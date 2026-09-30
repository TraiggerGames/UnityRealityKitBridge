using UnityEngine;

// Optional example: anchors this VisionObject to the first large horizontal
// surface and keeps it there across launches. The object stays hidden until its
// anchor is known, so a saved anchor shows it again immediately on the next start.
[RequireComponent(typeof(VisionObject))]
public sealed class VisionAnchorOnSurface : MonoBehaviour
{
    [SerializeField] private string anchorId = "sculpture-anchor";
    [SerializeField] private float heightAboveSurface = 0.15f;
    private VisionObject target;
    private bool requested;

    private void OnEnable()
    {
        target = GetComponent<VisionObject>();
        target.SetAnchor(anchorId); // hidden until the anchor is restored or created
        VisionInput.SurfaceUpdated += OnSurface;
    }

    private void OnDisable() { VisionInput.SurfaceUpdated -= OnSurface; }

    private void OnSurface(VisionInput.SurfaceSample surface)
    {
        if (requested || surface.Phase == "removed" || surface.Alignment != "horizontal") return;
        if (surface.Width < 0.3f || surface.Height < 0.3f) return;
        Vector3 up = surface.Normal.y < 0f ? -surface.Normal : surface.Normal;
        // Ensure is a no-op when a saved anchor already exists.
        target.AttachToAnchor(anchorId, surface.Center + up * heightAboveSurface, Quaternion.identity);
        requested = true;
    }
}
