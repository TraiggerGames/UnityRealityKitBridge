using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

// Sends a complete, small scene snapshot 10 times per second. Full snapshots
// make create/delete deterministic for this prototype; a future library can
// send only changes. This component must be on GameObject "VisionSceneBridge".
[DefaultExecutionOrder(100)]
public sealed class VisionSceneBridge : MonoBehaviour
{
    private const float PublishInterval = 0.1f;
    private float nextPublish;
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
        if (Time.time < nextPublish) return;
        Publish();
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
        nextPublish = Time.time + PublishInterval;
        objectsById.Clear();
        var found = FindObjectsByType<VisionObject>(FindObjectsSortMode.None);
        var states = new List<ObjectState>(found.Length);
        foreach (var item in found)
        {
            if (!item.isActiveAndEnabled || string.IsNullOrEmpty(item.Id)) continue;
            if (objectsById.ContainsKey(item.Id))
            {
                Debug.LogWarning("Duplicate VisionObject ID: " + item.Id);
                continue;
            }
            objectsById.Add(item.Id, item);
            var transform = item.transform;
            var color = item.DisplayColor;
            states.Add(new ObjectState {
                id = item.Id, shape = item.ShapeName, asset = item.ModelKey,
                audio = item.AudioKey, audioSequence = item.AudioSequence,
                anchor = item.AnchorId,
                position = V3.WorldPosition(transform.position),
                rotation = new Q4(transform.rotation),
                scale = new V3(transform.lossyScale),
                color = new RGB(color)
            });
        }
        // Sort by ID so the protocol and logs are deterministic.
        states.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        var commands = new AnchorCommand[VisionAnchors.Pending.Count];
        for (int i = 0; i < commands.Length; i++)
        {
            var c = VisionAnchors.Pending[i];
            commands[i] = new AnchorCommand {
                id = c.Id, op = c.Op, sequence = c.Sequence,
                position = V3.WorldPosition(c.Position), rotation = new Q4(c.Rotation)
            };
        }
        SendNative(JsonUtility.ToJson(new SceneFrame {
            version = 2, objects = states.ToArray(),
            anchors = commands, map = VisionMap.ModeName
        }));
    }

    [Serializable] private struct SceneFrame
    {
        public int version;
        public ObjectState[] objects;
        public AnchorCommand[] anchors;
        public string map;
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
        public string id, shape, asset, audio, anchor;
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
