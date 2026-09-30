using System;
using UnityEngine;

// Optional example: lets a VisionObject react when a fingertip touches it, with
// no pinch. The system tap/drag gestures always need look + pinch; this uses the
// ARKit index fingertip instead. The touch volume is the same unit box (scaled by
// the object) that the host uses for gaze targeting.
[RequireComponent(typeof(VisionObject))]
public sealed class VisionTouchable : MonoBehaviour
{
    [SerializeField] private float margin = 0.02f; // meters around the box
    [SerializeField] private bool selectOnTouch = true;
    private VisionObject target;
    private bool leftInside, rightInside;

    public bool IsTouched => leftInside || rightInside;
    public event Action<string> TouchBegan; // hand: "left" or "right"
    public event Action<string> TouchEnded;

    private void OnEnable()
    {
        target = GetComponent<VisionObject>();
        VisionInput.HandUpdated += OnHand;
    }

    private void OnDisable()
    {
        VisionInput.HandUpdated -= OnHand;
        leftInside = rightInside = false;
    }

    private void OnHand(VisionInput.HandSample sample)
    {
        bool inside = sample.Tracked && Contains(sample.IndexTip);
        ref bool was = ref (sample.Hand == "left" ? ref leftInside : ref rightInside);
        if (inside == was) return;
        was = inside;
        if (inside)
        {
            TouchBegan?.Invoke(sample.Hand);
            if (selectOnTouch) target.Select();
        }
        else TouchEnded?.Invoke(sample.Hand);
    }

    private bool Contains(Vector3 worldPoint)
    {
        // An anchored object's transform is local to its anchor.
        Matrix4x4 world = transform.localToWorldMatrix;
        if (!string.IsNullOrEmpty(target.AnchorId))
        {
            if (!VisionAnchors.TryGet(target.AnchorId, out var anchor)) return false;
            world = Matrix4x4.TRS(anchor.Position, anchor.Rotation, Vector3.one) *
                    Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        }
        Vector3 local = world.inverse.MultiplyPoint3x4(worldPoint);
        Vector3 scale = transform.lossyScale;
        return Mathf.Abs(local.x) <= 0.5f + margin / Mathf.Max(Mathf.Abs(scale.x), 1e-4f) &&
               Mathf.Abs(local.y) <= 0.5f + margin / Mathf.Max(Mathf.Abs(scale.y), 1e-4f) &&
               Mathf.Abs(local.z) <= 0.5f + margin / Mathf.Max(Mathf.Abs(scale.z), 1e-4f);
    }
}
