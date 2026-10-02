using UnityEngine;
using System;
using System.Collections.Generic;

// Add this to every Unity object that should appear in RealityKit.
// The ID must be unique and stable within the scene.
public sealed class VisionObject : MonoBehaviour
{
    public enum PrimitiveShape { Box, Sphere, Model, Panel }

    [SerializeField] private string objectId = "piece";
    [SerializeField] private PrimitiveShape shape = PrimitiveShape.Box;
    [SerializeField] private string modelKey = "";
    [SerializeField] private Color baseColor = Color.cyan;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private string anchorId = "";
    // Only used when shape is Panel: a floating text card. Its size in meters is
    // the transform's scale X (width) and Y (height).
    [SerializeField] private string panelTitle = "";
    [SerializeField, TextArea] private string panelText = "";
    [SerializeField] private bool panelFaceUser = true;

    // Enabled objects, so the bridge never searches the scene. Version changes
    // whenever an object appears or disappears.
    internal static readonly List<VisionObject> Active = new List<VisionObject>();
    internal static int Version { get; private set; }
    private void OnEnable() { Active.Add(this); Version++; sentOnce = false; }
    private void OnDisable() { Active.Remove(this); Version++; }

    // What the host last received, to publish only when something changed.
    private bool sentOnce, sentSelected;
    private Color sentColor;
    private Vector3 sentPosition, sentScale;
    private Quaternion sentRotation;
    private int sentAudioSequence;
    private string sentAnchor, sentPanel;

    private string PanelSignature => panelTitle + "\u0001" + panelText + panelFaceUser;

    internal bool ChangedSinceSent()
    {
        var t = transform;
        return !sentOnce || sentSelected != selected || sentAudioSequence != audioSequence ||
               sentAnchor != anchorId || sentColor != DisplayColor || sentPanel != PanelSignature || t.position != sentPosition ||
               t.rotation != sentRotation || t.lossyScale != sentScale;
    }

    internal void MarkSent()
    {
        var t = transform;
        sentOnce = true; sentSelected = selected; sentAudioSequence = audioSequence;
        sentAnchor = anchorId; sentColor = DisplayColor; sentPanel = PanelSignature; sentPosition = t.position;
        sentRotation = t.rotation; sentScale = t.lossyScale;
    }

    private bool selected;
    private string audioKey = "";
    private int audioSequence;
    // Native taps arrive at VisionSceneBridge, which calls Select(). Game
    // scripts can subscribe without changing Swift or the bridge protocol.
    public event Action<bool> SelectionChanged;
    public string Id => objectId;
    public bool IsSelected => selected;
    public string ShapeName => shape == PrimitiveShape.Model ? "model" :
                               shape == PrimitiveShape.Sphere ? "sphere" :
                               shape == PrimitiveShape.Panel ? "panel" : "box";
    public string PanelTitle { get => panelTitle; set => panelTitle = value ?? ""; }
    public string PanelText { get => panelText; set => panelText = value ?? ""; }
    public bool PanelFaceUser { get => panelFaceUser; set => panelFaceUser = value; }
    public void SetPanelText(string title, string text) { PanelTitle = title; PanelText = text; }
    public string ModelKey => shape == PrimitiveShape.Model ? modelKey : "";
    public Color DisplayColor => selected ? selectedColor : baseColor;
    // Colors you can change from code (they reach the headset on the next frame).
    public Color BaseColor { get => baseColor; set => baseColor = value; }
    public Color SelectedColor { get => selectedColor; set => selectedColor = value; }
    public string AudioKey => audioKey;
    public int AudioSequence => audioSequence;
    // When set, position and rotation are relative to this anchor (VisionAnchors)
    // and the object stays hidden until the anchor is located.
    public string AnchorId => anchorId;

    public void SetAnchor(string id) { anchorId = id ?? ""; }

    // Anchors the object at a world pose (only if the anchor does not exist yet)
    // and resets its transform, which from now on is local to the anchor.
    public void AttachToAnchor(string id, Vector3 worldPosition, Quaternion worldRotation)
    {
        VisionAnchors.Ensure(id, worldPosition, worldRotation);
        anchorId = id;
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    // Like AttachToAnchor, but replaces an existing anchor of that name.
    public void PlaceAnchor(string id, Vector3 worldPosition, Quaternion worldRotation)
    {
        VisionAnchors.Place(id, worldPosition, worldRotation);
        anchorId = id;
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    // Stops following the anchor and keeps the object where it is in the world,
    // so it can be moved freely again. The anchor itself is not deleted.
    public void DetachFromAnchor()
    {
        if (string.IsNullOrEmpty(anchorId)) return;
        if (VisionAnchors.TryGet(anchorId, out var anchor))
            transform.SetPositionAndRotation(
                anchor.Position + anchor.Rotation * transform.localPosition,
                anchor.Rotation * transform.localRotation);
        anchorId = "";
    }

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
