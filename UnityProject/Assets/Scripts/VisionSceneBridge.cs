using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

// Sends a complete, small scene snapshot publishRate times per second. Full snapshots
// make create/delete deterministic for this prototype; a future library can
// send only changes. This component must be on GameObject "VisionSceneBridge".
[DefaultExecutionOrder(100)]
public sealed class VisionSceneBridge : MonoBehaviour
{
    [Tooltip("Maximum snapshots per second while something moves. 0 = every Unity frame (the maximum).")]
    [SerializeField] private float publishRate = 0f;
    [Tooltip("Snapshots per second while nothing changes (keeps the host in sync).")]
    [SerializeField] private float idleRate = 2f;
    private float nextHeartbeat;
    private int sentVersion = -1;
    private int sentAnchorSequence = -1;
    private string sentMap = "";
    private float nextPublish;
    private int framesSincePublish;
    private float fpsWindowStart;
    private readonly Dictionary<string, VisionObject> objectsById = new Dictionary<string, VisionObject>();

#if UNITY_VISIONOS && !UNITY_EDITOR
    [DllImport("__Internal", EntryPoint = "MVP_SendCubeState")]
    private static extern void SendNative(string json);
#else
    private static void SendNative(string json) { Debug.Log("Vision scene preview: " + json); }
#endif

    private void Start() { Publish(); }

    private void Update()
    {
        framesSincePublish++;
        if (Time.time < nextPublish) return;
        if (Time.time >= nextHeartbeat || IsDirty()) Publish();
    }

    // True when the host would see something new: an object moved, appeared,
    // vanished or changed, an anchor command is new, or the map mode changed.
    private bool IsDirty()
    {
        if (sentVersion != VisionObject.Version || sentMap != VisionMap.ModeName) return true;
        var pending = VisionAnchors.Pending;
        if ((pending.Count > 0 ? pending[pending.Count - 1].Sequence : 0) != sentAnchorSequence) return true;
        foreach (var item in VisionObject.Active)
            if (item.ChangedSinceSent()) return true;
        return false;
    }

    // RealityKit -> Unity. The native side sends only the selected object ID.
    public void OnNativeSelect(string objectId)
    {
        if (!objectsById.TryGetValue(objectId, out var target)) return;
        if (target == null || !target.isActiveAndEnabled) return;
        target.Select();
        Publish();
    }

    // ARKit hand/surface data and targeted gestures share one documented
    // JSON event channel. Scripts subscribe through VisionInput.
    public void OnNativeEvent(string json) { VisionInput.Dispatch(json); }

    private void Publish()
    {
        nextPublish = publishRate > 0f ? Time.time + 1f / publishRate : 0f;
        nextHeartbeat = Time.time + (idleRate > 0f ? 1f / idleRate : 0.5f);
        objectsById.Clear();
        var found = VisionObject.Active;
        var states = new List<ObjectState>(found.Count);
        foreach (var item in found)
        {
            if (!item.isActiveAndEnabled || string.IsNullOrEmpty(item.Id)) continue;
            if (objectsById.ContainsKey(item.Id))
            {
                Debug.LogWarning("Duplicate VisionObject ID: " + item.Id);
                continue;
            }
            objectsById.Add(item.Id, item);
            item.MarkSent();
            var transform = item.transform;
            var color = item.DisplayColor;
            states.Add(new ObjectState {
                id = item.Id, shape = item.ShapeName, asset = item.ModelKey,
                audio = item.AudioKey, audioSequence = item.AudioSequence,
                anchor = item.AnchorId,
                title = item.PanelTitle, text = item.PanelText, faceUser = item.PanelFaceUser,
                position = V3.WorldPosition(transform.position),
                rotation = new Q4(transform.rotation),
                scale = new V3(transform.lossyScale),
                color = new RGB(color)
            });
        }
        // Sort by ID so the protocol and logs are deterministic.
        states.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        sentVersion = VisionObject.Version;
        sentMap = VisionMap.ModeName;
        sentAnchorSequence = VisionAnchors.Pending.Count > 0
            ? VisionAnchors.Pending[VisionAnchors.Pending.Count - 1].Sequence : 0;
        var commands = new AnchorCommand[VisionAnchors.Pending.Count];
        for (int i = 0; i < commands.Length; i++)
        {
            var c = VisionAnchors.Pending[i];
            commands[i] = new AnchorCommand {
                id = c.Id, op = c.Op, sequence = c.Sequence,
                position = V3.WorldPosition(c.Position), rotation = new Q4(c.Rotation)
            };
        }
        float elapsed = Time.unscaledTime - fpsWindowStart;
        float measuredFps = elapsed > 0.05f ? framesSincePublish / elapsed : 0f;
        framesSincePublish = 0;
        fpsWindowStart = Time.unscaledTime;
        SendNative(JsonUtility.ToJson(new SceneFrame {
            version = 2, objects = states.ToArray(),
            anchors = commands, map = VisionMap.ModeName,
            unityFps = measuredFps, unityMaxFps = MaxFps()
        }));
    }

    // Frame rate Unity is allowed to run at: the explicit target, else the
    // display refresh rate divided by the vSync interval.
    private static float MaxFps()
    {
        if (Application.targetFrameRate > 0) return Application.targetFrameRate;
        float refresh = (float)Screen.currentResolution.refreshRateRatio.value;
        int vsync = QualitySettings.vSyncCount;
        return vsync > 0 ? refresh / vsync : refresh;
    }

    [Serializable] private struct SceneFrame
    {
        public int version;
        public ObjectState[] objects;
        public AnchorCommand[] anchors;
        public string map;
        public float unityFps, unityMaxFps; // diagnostics for the debug window
    }
    [Serializable] private struct AnchorCommand
    {
        public string id, op;
        public int sequence;
        public V3 position;
        public Q4 rotation;
    }
    [Serializable] private struct ObjectState
    {
        public string id, shape, asset, audio, anchor, title, text;
        public bool faceUser;
        public int audioSequence;
        public V3 position, scale;
        public Q4 rotation;
        public RGB color;
    }
    [Serializable] private struct V3
    {
        public float x, y, z;
        public V3(Vector3 value) { x = value.x; y = value.y; z = value.z; }
        // Unity's +Z faces into its scene; RealityKit uses -Z in front.
        public static V3 WorldPosition(Vector3 value) =>
            new V3 { x = value.x, y = value.y, z = -value.z };
    }
    [Serializable] private struct Q4
    {
        public float x, y, z, w;
        // Reflection across Z converts Unity's left-handed rotation to
        // RealityKit's right-handed coordinates.
        public Q4(Quaternion value) { x = -value.x; y = -value.y; z = value.z; w = value.w; }
    }
    [Serializable] private struct RGB
    {
        public float r, g, b;
        public RGB(Color value) { r = value.r; g = value.g; b = value.b; }
    }
}
