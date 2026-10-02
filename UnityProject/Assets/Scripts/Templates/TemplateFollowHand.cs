using UnityEngine;

// TEMPLATE / PLANTILLA: the object follows your pinch point while you pinch.
// El objeto sigue el punto de pellizco mientras pellizcas.
//
// Needs hand-tracking permission. Do not use together with a grab on the same object.
// Requiere el permiso de manos. No lo combines con un agarre sobre el mismo objeto.
public sealed class TemplateFollowHand : MonoBehaviour
{
    [SerializeField] private VisionHand hand = VisionHand.Right;
    [SerializeField] private float smoothing = 20f; // higher = snappier / más alto = más directo

    private void Update()
    {
        if (!VisionControls.GetPinch(hand)) return;
        Vector3 goal = VisionControls.Hand(hand).PinchPoint;
        transform.position = Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
    }
}
