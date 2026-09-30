using System;
using System.Runtime.InteropServices;
using UnityEngine;

// The Unity object is the authoritative model. RealityKit only displays it.
public sealed class CubeLogic : MonoBehaviour
{
    private bool selected;
    private float lastSent;

#if UNITY_VISIONOS && !UNITY_EDITOR
    [DllImport("__Internal", EntryPoint = "MVP_SendCubeState")]
    private static extern void SendNative(string json);
#else
    private static void SendNative(string json) { Debug.Log("Bridge preview: " + json); }
#endif

    private void Start() { Publish(); }

    private void Update()
    {
        // A small motion proves that transforms originate in C#.
        transform.position = new Vector3(Mathf.Sin(Time.time) * 0.15f, 1.4f, -1.2f);
        transform.rotation = Quaternion.Euler(0, Time.time * 30f, 0);
        if (Time.time - lastSent >= 0.05f) Publish();
    }

    // Native -> Unity entry point. Called through UnityFramework sendMessageToGO.
    public void OnNativeSelect(string message)
    {
        if (message != "cube.select") return;
        selected = !selected;
        Publish();
    }

    private void Publish()
    {
        lastSent = Time.time;
        var p = transform.position;
        var q = transform.rotation;
        var s = transform.localScale;
        var state = new CubeState {
            version = 1, id = "cube", selected = selected,
            position = new V3(p), rotation = new Q4(q), scale = new V3(s)
        };
        SendNative(JsonUtility.ToJson(state));
    }

    [Serializable] private struct CubeState
    {
        public int version; public string id; public bool selected;
        public V3 position; public Q4 rotation; public V3 scale;
    }
    [Serializable] private struct V3
    {
        public float x, y, z;
        public V3(Vector3 value) { x = value.x; y = value.y; z = value.z; }
    }
    [Serializable] private struct Q4
    {
        public float x, y, z, w;
        public Q4(Quaternion value) { x = value.x; y = value.y; z = value.z; w = value.w; }
    }
}
