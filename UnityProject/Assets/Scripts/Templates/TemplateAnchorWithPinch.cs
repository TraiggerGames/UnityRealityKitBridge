using UnityEngine;

// TEMPLATE / PLANTILLA: pinch with the LEFT hand to drop an anchor, and a marker
// object appears there for good. Pellizca con la mano IZQUIERDA para dejar un
// ancla; el objeto aparece ahí de forma permanente.
//
// Shows the raw anchor API: VisionAnchors.Place / Remove / AnchorUpdated.
// Muestra la API de anclas en bruto: VisionAnchors.Place / Remove / AnchorUpdated.
[RequireComponent(typeof(VisionObject))]
public sealed class TemplateAnchorWithPinch : MonoBehaviour
{
    [SerializeField] private string anchorId = "pinch-anchor";
    [SerializeField] private VisionHand hand = VisionHand.Left;
    private VisionObject target;

    private void Awake() { target = GetComponent<VisionObject>(); }

    private void OnEnable()
    {
        target.SetAnchor(anchorId); // hidden until the anchor exists / oculto hasta que exista
        VisionAnchors.AnchorUpdated += OnAnchor;
    }

    private void OnDisable() { VisionAnchors.AnchorUpdated -= OnAnchor; }

    private void Update()
    {
        if (VisionControls.GetPinchDown(hand))
            target.PlaceAnchor(anchorId, VisionControls.Hand(hand).PinchPoint, Quaternion.identity);
    }

    private void OnAnchor(VisionAnchors.AnchorSample anchor)
    {
        if (anchor.Id == anchorId) Debug.Log($"Anchor {anchor.Phase}, tracked={anchor.Tracked}");
    }
}
