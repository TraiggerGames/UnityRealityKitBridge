using UnityEngine;

// Optional example: attach to a VisionObject to place it on the first detected
// horizontal real-world surface. SurfaceUpdated arrives from ARKit via the host.
public sealed class VisionSurfacePlacer : MonoBehaviour
{
    [SerializeField] private float heightAboveSurface = 0.15f;
    private bool placed;

    private void OnEnable() { VisionInput.SurfaceUpdated += OnSurface; }
    private void OnDisable() { VisionInput.SurfaceUpdated -= OnSurface; }

    private void OnSurface(VisionInput.SurfaceSample surface)
    {
        if (placed || surface.Phase == "removed" || surface.Alignment != "horizontal") return;
        if (surface.Width < 0.3f || surface.Height < 0.3f) return;
        Vector3 up = surface.Normal.y < 0f ? -surface.Normal : surface.Normal;
        transform.position = surface.Center + up * heightAboveSurface;
        placed = true;
    }
}
