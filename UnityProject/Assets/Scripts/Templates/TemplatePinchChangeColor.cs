using UnityEngine;

// TEMPLATE / PLANTILLA: pinch with your hand NEAR the object (no need to look at it).
// Pellizca con la mano CERCA del objeto (sin necesidad de mirarlo).
//
// Uses VisionControls, the Input-like API. Needs hand-tracking permission.
// Usa VisionControls, la API tipo Input. Requiere el permiso de seguimiento de manos.
[RequireComponent(typeof(VisionObject))]
public sealed class TemplatePinchChangeColor : MonoBehaviour
{
    [SerializeField] private VisionHand hand = VisionHand.Right;
    [SerializeField] private float reach = 0.12f; // meters / metros
    [SerializeField] private Color pinchedColor = Color.red;
    private VisionObject target;
    private Color original;

    private void Awake() { target = GetComponent<VisionObject>(); original = target.BaseColor; }

    private void Update()
    {
        // Note: for an anchored object, transform.position is local to the anchor.
        // Nota: si el objeto está anclado, transform.position es local al ancla.
        bool near = VisionControls.IsTracked(hand) &&
                    Vector3.Distance(VisionControls.Hand(hand).PinchPoint, transform.position) < reach;
        if (near && VisionControls.GetPinchDown(hand)) target.BaseColor = pinchedColor;
        if (VisionControls.GetPinchUp(hand)) target.BaseColor = original;
    }
}
