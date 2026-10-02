using UnityEngine;

// TEMPLATE / PLANTILLA: tap a painting -> an info card appears next to it. You can
// grab and move the card; tap the card to close it.
// Toca un cuadro -> aparece a su lado una tarjeta de información. Puedes cogerla y
// moverla; toca la tarjeta para cerrarla.
//
// Setup / Montaje:
//  1. Painting: a VisionObject (+ VisionInteractable) with this component.
//     Cuadro: un VisionObject (+ VisionInteractable) con este componente.
//  2. Card: another GameObject with VisionObject, Shape = Panel, and a
//     VisionInteractable (Grabbable on). Scale X/Y = width/height in meters
//     (e.g. 0.5 x 0.3). Leave it ACTIVE in the scene; this script hides it.
//     Tarjeta: otro GameObject con VisionObject, Shape = Panel y un
//     VisionInteractable (Grabbable activado). Scale X/Y = ancho/alto en metros
//     (p. ej. 0,5 x 0,3). Déjala ACTIVA en la escena; este script la oculta.
//  3. Drag the card into the "card" field below. / Arrastra la tarjeta al campo "card".
[RequireComponent(typeof(VisionObject))]
public sealed class TemplateMuseumInfo : MonoBehaviour
{
    [SerializeField] private VisionObject card;
    [SerializeField] private string title = "Las Meninas";
    [SerializeField, TextArea(3, 8)] private string text = "Diego Velázquez, 1656.\nÓleo sobre lienzo.";
    [Tooltip("Where the card appears, relative to the painting (meters). / Dónde aparece, respecto al cuadro (metros).")]
    [SerializeField] private Vector3 offset = new Vector3(0.6f, 0f, 0f);

    private VisionObject painting;
    private VisionInteractable cardTouch;

    private void Awake()
    {
        painting = GetComponent<VisionObject>();
        if (card != null) cardTouch = card.GetComponent<VisionInteractable>();
    }

    private void OnEnable()
    {
        painting.SelectionChanged += OnPaintingTap;
        if (cardTouch != null) cardTouch.Tapped += Hide;
        if (card != null) card.gameObject.SetActive(false); // starts hidden / empieza oculta
    }

    private void OnDisable()
    {
        painting.SelectionChanged -= OnPaintingTap;
        if (cardTouch != null) cardTouch.Tapped -= Hide;
    }

    private void OnPaintingTap(bool selected)
    {
        if (card == null) return;
        card.SetPanelText(title, text);
        card.transform.position = transform.position + offset;
        card.gameObject.SetActive(true);
    }

    private void Hide() { card.gameObject.SetActive(false); }
}
