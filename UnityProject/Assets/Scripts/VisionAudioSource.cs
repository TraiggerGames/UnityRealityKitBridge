using UnityEngine;

// Attach a mono WAV/AIFF clip to a VisionObject. Calling Play() from any Unity
// script increments a command number; the RealityKit entity plays that asset
// from its current 3D position once, even though scene frames repeat at 10 Hz.
[RequireComponent(typeof(VisionObject))]
public sealed class VisionAudioSource : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField] private string assetKey = "chime";
    public AudioClip Clip => clip;
    public string AssetKey => assetKey;

    public void Configure(AudioClip source, string key)
    {
        clip = source;
        assetKey = key;
    }

    public void Play()
    {
        if (clip == null || string.IsNullOrEmpty(assetKey)) return;
        GetComponent<VisionObject>().PlayAudio(assetKey);
    }
}
