using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: MVP > Create demo scene. Safe to rerun: creates a fresh scene.
public static class CreateDemo
{
    [MenuItem("MVP/Create demo scene")]
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // Keep this name aligned with UnityRuntime.sendMessageToGO.
        cube.name = "Cube";
        cube.transform.position = new Vector3(0, 1.4f, -1.2f);
        cube.transform.localScale = Vector3.one * 0.2f;
        cube.AddComponent<CubeLogic>();
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Demo.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Demo.unity", true) };
    }
}
