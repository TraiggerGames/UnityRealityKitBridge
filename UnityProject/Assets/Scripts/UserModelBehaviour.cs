using UnityEngine;

// Example of programming a RealityKit model entirely from Unity/C#.
// Add this alongside VisionObject on any model. RealityKit receives the
// resulting transform through VisionSceneBridge; it does not run this script.
[RequireComponent(typeof(VisionObject))]
public sealed class UserModelBehaviour : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 18f;
    [SerializeField] private float selectedScaleMultiplier = 1.2f;

    private VisionObject visionObject;
    private VisionAudioSource audioSource;
    private Vector3 originalScale;
    private bool selected;

    private void Awake()
    {
        visionObject = GetComponent<VisionObject>();
        audioSource = GetComponent<VisionAudioSource>();
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (visionObject == null) visionObject = GetComponent<VisionObject>();
        visionObject.SelectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        if (visionObject != null) visionObject.SelectionChanged -= OnSelectionChanged;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
        // Smoothly enlarge the model when the user taps it in RealityKit.
        float multiplier = selected ? selectedScaleMultiplier : 1f;
        transform.localScale = Vector3.Lerp(
            transform.localScale, originalScale * multiplier,
            Mathf.Min(1f, 6f * Time.deltaTime));
    }

    private void OnSelectionChanged(bool value)
    {
        selected = value;
        if (value && audioSource != null) audioSource.Play();
    }
}
