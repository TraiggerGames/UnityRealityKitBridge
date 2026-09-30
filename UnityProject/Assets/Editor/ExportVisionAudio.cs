using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Copies original audio files for native spatial playback. Unity's imported
// AudioClip binary is not a format that RealityKit can load from the app bundle.
public static class ExportVisionAudio
{
    private const string DirectoryName = "Assets/VisionExport/Audio";

    public static void ExportActiveScene()
    {
        Directory.CreateDirectory(DirectoryName);
        foreach (var file in Directory.GetFiles(DirectoryName))
            if (IsSupported(Path.GetExtension(file))) File.Delete(file);
        var used = new HashSet<string>();
        foreach (var source in UnityEngine.Object.FindObjectsByType<VisionAudioSource>(FindObjectsSortMode.None))
        {
            if (!source.isActiveAndEnabled || source.Clip == null) continue;
            if (!Regex.IsMatch(source.AssetKey ?? "", "^[A-Za-z0-9_-]+$"))
                throw new InvalidOperationException("Invalid audio key on " + source.name);
            if (!used.Add(source.AssetKey))
                throw new InvalidOperationException("Duplicate audio key: " + source.AssetKey);
            string path = AssetDatabase.GetAssetPath(source.Clip);
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (!IsSupported(extension) || !File.Exists(path))
                throw new InvalidOperationException("Use an imported WAV or AIFF file: " + source.name);
            File.Copy(path, Path.Combine(DirectoryName, source.AssetKey + extension), true);
            Debug.Log("Exported spatial audio " + source.AssetKey);
        }
        AssetDatabase.Refresh();
    }

    private static bool IsSupported(string extension) =>
        extension == ".wav" || extension == ".aif" || extension == ".aiff";
}
