using UnityEngine;

// Optional example: detect a simple thumb-index pinch entirely in Unity/C#.
// This is only a distance threshold, not Apple's system gesture recognizer.
public sealed class VisionHandPinch : MonoBehaviour
{
    [SerializeField] private string hand = "right";
    [SerializeField] private float pinchDistanceMeters = 0.03f;
    [SerializeField] private float selectedScale = 1.25f;
    private Vector3 initialScale;
    private bool pinching;

    private void Awake() { initialScale = transform.localScale; }
    private void OnEnable() { VisionInput.HandUpdated += OnHand; }
    private void OnDisable() { VisionInput.HandUpdated -= OnHand; }

    private void OnHand(VisionInput.HandSample sample)
    {
        if (sample.Hand != hand) return;
        pinching = sample.Tracked &&
            Vector3.Distance(sample.IndexTip, sample.ThumbTip) < pinchDistanceMeters;
    }

    private void Update()
    {
        Vector3 target = initialScale * (pinching ? selectedScale : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * 8f);
    }
}
