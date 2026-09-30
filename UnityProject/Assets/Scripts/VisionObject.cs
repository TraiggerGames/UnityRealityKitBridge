using UnityEngine;
using System;

// Add this to every Unity object that should appear in RealityKit.
// The ID must be unique and stable within the scene.
public sealed class VisionObject : MonoBehaviour
{
    public enum PrimitiveShape { Box, Sphere, Model }

    [SerializeField] private string objectId = "piece";
    [SerializeField] private PrimitiveShape shape = PrimitiveShape.Box;
    [SerializeField] private string modelKey = "";
    [SerializeField] private Color baseColor = Color.cyan;
    [SerializeField] private Color selectedColor = Color.yellow;

    private bool selected;
    private string audioKey = "";
    private int audioSequence;
    // Native taps arrive at VisionSceneBridge, which calls Select(). Game
    // scripts can subscribe without changing Swift or the bridge protocol.
    public event Action<bool> SelectionChanged;
    public string Id => objectId;
    public bool IsSelected => selected;
    public string ShapeName => shape == PrimitiveShape.Model ? "model" :
                               shape == PrimitiveShape.Sphere ? "sphere" : "box";
    public string ModelKey => shape == PrimitiveShape.Model ? modelKey : "";
    public Color DisplayColor => selected ? selectedColor : baseColor;
    public string AudioKey => audioKey;
    public int AudioSequence => audioSequence;

    public void PlayAudio(string key)
    {
        audioKey = key;
        audioSequence++;
    }

    public void Configure(string id, PrimitiveShape primitive, Color normal, Color active, string assetKey = "")
    {
        objectId = id;
        shape = primitive;
        modelKey = assetKey;
        baseColor = normal;
        selectedColor = active;
    }

    public void Select()
    {
        selected = !selected;
        var motion = GetComponent<OrbitMotion>();
        if (motion != null) motion.SetBoosted(selected);
        SelectionChanged?.Invoke(selected);
    }
}
