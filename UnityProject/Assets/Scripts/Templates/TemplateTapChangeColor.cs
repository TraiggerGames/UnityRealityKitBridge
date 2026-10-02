using UnityEngine;

// TEMPLATE / PLANTILLA: look at the object and pinch -> it changes color.
// Mira el objeto y pellizca -> cambia de color.
//
// Use: copy this file, rename the class, add it to a GameObject with VisionObject.
// Uso: copia este archivo, cambia el nombre de la clase y añádelo a un objeto con VisionObject.
[RequireComponent(typeof(VisionObject))]
public sealed class TemplateTapChangeColor : MonoBehaviour
{
    [SerializeField] private Color[] colors = { Color.cyan, Color.yellow, Color.magenta, Color.green };
    private VisionObject target;
    private int index;

    private void Awake() { target = GetComponent<VisionObject>(); }

    private void OnEnable()
    {
        target.SelectionChanged += OnTap; // the system tap: look + pinch / el tap del sistema
        Apply();
    }

    private void OnDisable() { target.SelectionChanged -= OnTap; }

    private void OnTap(bool selected)
    {
        index = (index + 1) % colors.Length;
        Apply();
    }

    private void Apply()
    {
        // Same color in both states so the tap's own on/off toggle does not matter.
        target.BaseColor = colors[index];
        target.SelectedColor = colors[index];
    }
}
