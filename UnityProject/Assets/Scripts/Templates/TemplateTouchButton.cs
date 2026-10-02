using UnityEngine;

// TEMPLATE / PLANTILLA: a "button" you press by touching it with a fingertip,
// no pinch needed. Un "botón" que se pulsa tocándolo con la yema, sin pellizco.
//
// Needs hand-tracking permission. / Requiere el permiso de seguimiento de manos.
[RequireComponent(typeof(VisionObject), typeof(VisionTouchable))]
public sealed class TemplateTouchButton : MonoBehaviour
{
    [SerializeField] private Color pressedColor = Color.green;
    private VisionObject target;
    private VisionTouchable touch;
    private Color original;

    private void Awake()
    {
        target = GetComponent<VisionObject>();
        touch = GetComponent<VisionTouchable>();
        touch.SelectOnTouch = false; // we handle the reaction ourselves / reaccionamos nosotros
    }

    private void OnEnable()
    {
        touch.TouchBegan += OnPress;
        touch.TouchEnded += OnRelease;
    }

    private void OnDisable()
    {
        touch.TouchBegan -= OnPress;
        touch.TouchEnded -= OnRelease;
    }

    private void OnPress(string hand)
    {
        original = target.BaseColor;
        target.BaseColor = pressedColor;
        // TODO: do your action here / haz tu acción aquí.
    }

    private void OnRelease(string hand) { target.BaseColor = original; }
}
