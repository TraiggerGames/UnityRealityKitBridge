using System;
using UnityEngine;

// Reusable native -> Unity events. Scripts subscribe to these C# events; they
// never call ARKit or edit the Xcode host for a particular experience.
public static class VisionInput
{
    public struct HandSample
    {
        public string Hand; // "left" or "right"
        public bool Tracked;
        public Vector3 IndexTip;
        public Vector3 ThumbTip;
    }
    public struct SurfaceSample
    {
        public string Id;
        public string Phase; // added, updated, removed
        public string Alignment; // horizontal or vertical
        public string Classification;
        public Vector3 Center;
        public Vector3 Normal;
        public float Width, Height;
    }
    public struct DragSample
    {
        public string ObjectId;
        public string Phase; // changed or ended
        public Vector3 Position;
    }

    public static event Action<HandSample> HandUpdated;
    public static event Action<SurfaceSample> SurfaceUpdated;
    public static event Action<DragSample> ObjectDragged;

    [Serializable] private struct NativeEvent
    {
        public string type, id, phase, alignment, classification;
        public bool tracked;
        public V3 position, secondary, normal;
        public Q4 rotation;
        public float width, height;
    }
    [Serializable] private struct V3
    {
        public float x, y, z;
        // Native RealityKit uses -Z in front; Unity uses +Z in front.
        public Vector3 Unity => new Vector3(x, y, -z);
    }

    [Serializable] private struct Q4
    {
        public float x, y, z, w;
        // The reflection across Z is its own inverse (see VisionSceneBridge.Q4).
        public Quaternion Unity => new Quaternion(-x, -y, z, w);
    }

    internal static void Dispatch(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        NativeEvent value;
        try { value = JsonUtility.FromJson<NativeEvent>(json); }
        catch (Exception error) { Debug.LogWarning("Invalid VisionInput event: " + error.Message); return; }
        switch (value.type)
        {
            case "hand":
                HandUpdated?.Invoke(new HandSample {
                    Hand = value.id, Tracked = value.tracked,
                    IndexTip = value.position.Unity, ThumbTip = value.secondary.Unity
                });
                break;
            case "surface":
                SurfaceUpdated?.Invoke(new SurfaceSample {
                    Id = value.id, Phase = value.phase,
                    Alignment = value.alignment, Classification = value.classification,
                    Center = value.position.Unity, Normal = value.normal.Unity,
                    Width = value.width, Height = value.height
                });
                break;
            case "anchor":
                VisionAnchors.Receive(value.id, value.phase, value.tracked,
                    value.position.Unity,
                    value.rotation.w == 0f ? Quaternion.identity : value.rotation.Unity);
                break;
            case "drag":
                ObjectDragged?.Invoke(new DragSample {
                    ObjectId = value.id, Phase = value.phase,
                    Position = value.position.Unity
                });
                break;
        }
    }
}
