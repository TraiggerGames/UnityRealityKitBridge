using UnityEngine;

// TEMPLATE / PLANTILLA: move an object by hand; where you let go, it is ANCHORED
// to the real world and stays there even after closing and reopening the app.
// Mueve un objeto con la mano; donde lo sueltes queda ANCLADO al mundo real y
// sigue ahí aunque cierres y vuelvas a abrir la app.
//
// Needs scene permission (anchors). Each object needs its own anchorId.
// Requiere permiso de entorno (anclas). Cada objeto necesita su propio anchorId.
// "Borrar anclas" in the debug window forgets all saved anchors.
[RequireComponent(typeof(VisionInteractable))]
public sealed class TemplateGrabThenAnchor : MonoBehaviour
{
    [SerializeField] private string anchorId = "my-object-anchor";
    private VisionInteractable interactable;

    private void Awake() { interactable = GetComponent<VisionInteractable>(); }

    private void OnEnable()
    {
        interactable.GrabEnded += OnDrop;
        // Start already attached: hidden until ARKit restores or creates the anchor.
        // Empieza ya anclado: oculto hasta que ARKit restaura o crea el ancla.
        // Use Ensure so a saved position from a previous session is kept.
        // Ensure conserva la posición guardada de sesiones anteriores.
        if (string.IsNullOrEmpty(interactable.Object.AnchorId))
            interactable.Object.AttachToAnchor(anchorId, transform.position, transform.rotation);
    }

    private void OnDisable() { interactable.GrabEnded -= OnDrop; }

    private void OnDrop()
    {
        // The grab released the anchor, so transform is in world space right now.
        // El agarre soltó el ancla, así que transform está ahora en coordenadas de mundo.
        interactable.Object.PlaceAnchor(anchorId, transform.position, transform.rotation);
    }
}
