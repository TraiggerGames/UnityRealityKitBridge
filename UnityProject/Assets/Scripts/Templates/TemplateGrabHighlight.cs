using UnityEngine;

// TEMPLATE / PLANTILLA: grab an object (look + pinch + move hand), it follows the
// hand and highlights while held. Coge un objeto (mira + pellizca + mueve la mano):
// te sigue y se resalta mientras lo sujetas.
//
// VisionInteractable does the grabbing; this file only reacts to its events.
// VisionInteractable gestiona el agarre; este archivo solo reacciona a sus eventos.
[RequireComponent(typeof(VisionInteractable))]
public sealed class TemplateGrabHighlight : MonoBehaviour
{
    [SerializeField] private Color heldColor = Color.yellow;
    private VisionInteractable interactable;
    private Color original;

    private void Awake() { interactable = GetComponent<VisionInteractable>(); }

    private void OnEnable()
    {
        interactable.GrabBegan += OnGrabBegan;
        interactable.GrabEnded += OnGrabEnded;
    }

    private void OnDisable()
    {
        interactable.GrabBegan -= OnGrabBegan;
        interactable.GrabEnded -= OnGrabEnded;
    }

    private void OnGrabBegan()
    {
        original = interactable.Object.BaseColor;
        interactable.Object.BaseColor = heldColor;
    }

    private void OnGrabEnded()
    {
        interactable.Object.BaseColor = original;
        Debug.Log("Dropped at / Soltado en " + transform.position);
    }
}
