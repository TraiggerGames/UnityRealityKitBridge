using UnityEngine;

// Optional example: move one VisionObject to the 3D position reported by a
// RealityKit drag gesture. Replace this with your own constraints or physics.
[RequireComponent(typeof(VisionObject))]
public sealed class VisionDragMover : MonoBehaviour
{
    private VisionObject visionObject;
    private bool dragging;
    private Vector3 grabOffset;
    private void Awake() { visionObject = GetComponent<VisionObject>(); }
    private void OnEnable() { VisionInput.ObjectDragged += OnDrag; }
    private void OnDisable() { VisionInput.ObjectDragged -= OnDrag; }

    private void OnDrag(VisionInput.DragSample drag)
    {
        if (drag.ObjectId != visionObject.Id) return;
        if (drag.Phase == "ended") { dragging = false; return; }
        if (drag.Phase != "changed") return;
        if (!dragging)
        {
            grabOffset = transform.position - drag.Position;
            dragging = true;
        }
        transform.position = drag.Position + grabOffset;
    }
}
