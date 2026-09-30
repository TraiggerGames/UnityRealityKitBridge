using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// The menu creates a fresh Xcode export. The build callbacks add NativeHost
// automatically, even if the user starts a visionOS build from Build Settings.
public static class BuildVisionExperience
{
    [MenuItem("MVP/Build immersive visionOS app")]
    public static void BuildVisionOS()
    {
        string destination = Environment.GetEnvironmentVariable("VISION_MVP_BUILD_PATH");
        if (string.IsNullOrEmpty(destination))
        {
            string parent = EditorUtility.OpenFolderPanel("Choose a folder for visionOS builds", "", "");
            if (string.IsNullOrEmpty(parent)) return;
            destination = Path.Combine(parent, "VisionOS-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        }
        if (Directory.Exists(destination) && Directory.GetFileSystemEntries(destination).Length != 0)
            throw new InvalidOperationException("Choose a fresh, empty build folder: " + destination);
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length != 1 || !scenes[0].enabled)
            throw new InvalidOperationException("Enable exactly one Unity scene in Build Settings");
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { scenes[0].path },
            locationPathName = destination,
            target = BuildTarget.VisionOS,
            options = BuildOptions.None
        });
        if (result.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("visionOS build failed: " + result.summary.result);
        UnityEngine.Debug.Log("Immersive host ready: " + destination);
        if (!Application.isBatchMode) EditorUtility.RevealInFinder(destination);
    }

    const string DebugPref = "MVP.DebugHostUI";
    // Debug host UI defaults on; Development Build also forces it on.
    static bool DebugHostUI
    {
        get => EditorPrefs.GetBool(DebugPref, true);
        set => EditorPrefs.SetBool(DebugPref, value);
    }

    [MenuItem("MVP/Debug host UI (window + diagnostics)")]
    static void ToggleDebugHostUI() => DebugHostUI = !DebugHostUI;

    [MenuItem("MVP/Debug host UI (window + diagnostics)", true)]
    static bool ToggleDebugHostUIValidate()
    {
        Menu.SetChecked("MVP/Debug host UI (window + diagnostics)", DebugHostUI);
        return true;
    }

    internal static bool WantsDebugUI(BuildReport report) =>
        DebugHostUI || (report.summary.options & BuildOptions.Development) != 0;

    internal static string HostTemplate => Path.Combine(Application.dataPath, "Editor/VisionHost");

    internal static void ExportBuildScene()
    {
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length != 1 || !scenes[0].enabled)
            throw new InvalidOperationException("Enable exactly one Unity scene in Build Settings");
        EditorSceneManager.OpenScene(scenes[0].path);
        ExportVisionModels.ExportActiveScene();
        ExportVisionAudio.ExportActiveScene();
    }

    internal static void Integrate(string destination, bool debugUI)
    {
        string sourceModels = Path.Combine(Application.dataPath, "VisionExport/Models");
        string targetModels = Path.Combine(destination, "VisionModels");
        Directory.CreateDirectory(targetModels);
        foreach (var file in Directory.GetFiles(sourceModels))
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension == ".usda" || extension == ".png" ||
                extension == ".jpg" || extension == ".jpeg")
                File.Copy(file, Path.Combine(targetModels, Path.GetFileName(file)), true);
        }
        string sourceAudio = Path.Combine(Application.dataPath, "VisionExport/Audio");
        string targetAudio = Path.Combine(destination, "VisionAudio");
        Directory.CreateDirectory(targetAudio);
        foreach (var file in Directory.GetFiles(sourceAudio))
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension == ".wav" || extension == ".aif" || extension == ".aiff")
                File.Copy(file, Path.Combine(targetAudio, Path.GetFileName(file)), true);
        }
        string script = Path.Combine(HostTemplate, "integrate_host.rb");
        string swiftSources = Path.Combine(HostTemplate, "Sources");
        if (!File.Exists(script) || !Directory.Exists(swiftSources))
            throw new InvalidOperationException("Missing Assets/Editor/VisionHost template");
        var start = new ProcessStartInfo("/usr/bin/ruby") {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        start.ArgumentList.Add(script);
        start.ArgumentList.Add(destination);
        start.ArgumentList.Add(swiftSources);
        start.ArgumentList.Add(debugUI ? "debug" : "production");
        // Player Settings are the single source of the version shown by the host.
        start.ArgumentList.Add(PlayerSettings.bundleVersion);
        string build = PlayerSettings.VisionOS.buildNumber;
        start.ArgumentList.Add(string.IsNullOrEmpty(build) ? "1" : build);
        using (var process = Process.Start(start))
        {
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException("NativeHost integration failed (Ruby xcodeproj required): " + stderr + stdout);
            UnityEngine.Debug.Log(stdout);
        }
    }
}

public sealed class VisionBuildHooks : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => 1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.VisionOS)
            BuildVisionExperience.ExportBuildScene();
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.VisionOS)
            BuildVisionExperience.Integrate(report.summary.outputPath, BuildVisionExperience.WantsDebugUI(report));
    }
}
