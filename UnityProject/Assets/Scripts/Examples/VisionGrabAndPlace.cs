using System.Collections.Generic;
using UnityEngine;

// Optional example: pinch-drag a VisionObject, and when released over a detected
// horizontal surface (table, floor, seat...) let it settle on top of it.
// Surfaces are estimated rectangles, so "over" is a generous circle around the
// center. Release it elsewhere and it stays where you left it.
[RequireComponent(typeof(VisionObject))]
public sealed class VisionGrabAndPlace : MonoBehaviour
{
    [Tooltip("Distance from the object's pivot to its base. 0 if the pivot is at the bottom.")]
    [SerializeField] private float heightAboveSurface = 0f;
    [Tooltip("Only snap to a surface at most this far below the released object.")]
    [SerializeField] private float maxSnapDistance = 0.6f;
    [Tooltip("Extra horizontal reach around the estimated surface, in meters.")]
    [SerializeField] private float margin = 0.1f;
    [Tooltip("Empty = any. Otherwise e.g. Table, Floor, Seat (ARKit classification).")]
    [SerializeField] private string[] classifications = new string[0];
    [SerializeField] private float settleSpeed = 8f;

    private readonly Dictionary<string, VisionInput.SurfaceSample> surfaces =
        new Dictionary<string, VisionInput.SurfaceSample>();
    private VisionObject visionObject;
    private bool dragging;
    private Vector3 grabOffset;
    private Vector3? settleTarget;

    private void Awake() { visionObject = GetComponent<VisionObject>(); }

    private void OnEnable()
    {
        VisionInput.ObjectDragged += OnDrag;
        VisionInput.SurfaceUpdated += OnSurface;
    }

    private void OnDisable()
    {
        VisionInput.ObjectDragged -= OnDrag;
        VisionInput.SurfaceUpdated -= OnSurface;
    }

    private void OnSurface(VisionInput.SurfaceSample surface)
    {
        if (surface.Phase == "removed") surfaces.Remove(surface.Id);
        else if (surface.Alignment == "horizontal") surfaces[surface.Id] = surface;
    }

    private void OnDrag(VisionInput.DragSample drag)
    {
        if (drag.ObjectId != visionObject.Id) return;
        if (drag.Phase == "changed")
        {
            if (!dragging)
            {
                grabOffset = transform.position - drag.Position;
                dragging = true;
                settleTarget = null;
            }
            transform.position = drag.Position + grabOffset;
        }
        else if (drag.Phase == "ended" && dragging)
        {
            dragging = false;
            settleTarget = FindRestingPosition(transform.position);
        }
    }

    private Vector3? FindRestingPosition(Vector3 position)
    {
        bool found = false;
        float bestY = float.MinValue;
        foreach (var surface in surfaces.Values)
        {
            if (!Matches(surface.Classification)) continue;
            float drop = position.y - surface.Center.y;
            if (drop < -0.05f || drop > maxSnapDistance) continue; // above the object or too far below
            float reach = 0.5f * Mathf.Max(surface.Width, surface.Height) + margin;
            var flat = new Vector2(position.x - surface.Center.x, position.z - surface.Center.z);
            if (flat.magnitude > reach) continue;
            if (surface.Center.y > bestY) { bestY = surface.Center.y; found = true; }
        }
        return found ? new Vector3(position.x, bestY + heightAboveSurface, position.z) : (Vector3?)null;
    }

    private bool Matches(string classification)
    {
        if (classifications == null || classifications.Length == 0) return true;
        foreach (var wanted in classifications)
            if (string.Equals(wanted, classification, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private void Update()
    {
        if (dragging || !settleTarget.HasValue) return;
        transform.position = Vector3.Lerp(transform.position, settleTarget.Value, 1f - Mathf.Exp(-settleSpeed * Time.deltaTime));
        if ((transform.position - settleTarget.Value).sqrMagnitude < 1e-6f)
        {
            transform.position = settleTarget.Value;
            settleTarget = null;
        }
    }
}
