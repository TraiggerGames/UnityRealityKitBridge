using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: MVP > Create interactive sculpture. Saves a separate scene, leaving
// the user's existing Sample.unity untouched.
public static class CreateExperience
{
    // Leaves the user's current scene untouched. The sensor demo is a saved
    // copy with the imported model and the spatial chime already attached.
    [MenuItem("MVP/Use sensor demo scene")]
    public static void UseSensorDemoScene()
    {
        const string path = "Assets/Scenes/InteractiveSculptureSensors.unity";
        if (!System.IO.File.Exists(path))
            throw new System.IO.FileNotFoundException("Sensor demo scene is missing", path);
        EditorSceneManager.OpenScene(path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        Debug.Log("Sensor demo is now the only visionOS build scene: " + path);
    }

    [MenuItem("MVP/Create interactive sculpture")]
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var bridge = new GameObject("VisionSceneBridge");
        bridge.AddComponent<VisionSceneBridge>();

        Add("base", PrimitiveType.Cube, VisionObject.PrimitiveShape.Box,
            new Vector3(0f, 1.05f, 1.4f), new Vector3(0.9f, 0.08f, 0.65f),
            new Color(0.25f, 0.28f, 0.38f), Color.white);
        var core = Add("core", PrimitiveType.Cube, VisionObject.PrimitiveShape.Box,
            new Vector3(0f, 1.45f, 1.4f), Vector3.one * 0.22f,
            new Color(0.18f, 0.65f, 1f), new Color(1f, 0.45f, 0.12f));
        core.AddComponent<OrbitMotion>().Configure(new Vector3(0f, 1.45f, 1.4f), 0f, 0.7f, 0f);
        var left = Add("orbit-a", PrimitiveType.Sphere, VisionObject.PrimitiveShape.Sphere,
            new Vector3(-0.45f, 1.45f, 1.4f), Vector3.one * 0.15f,
            new Color(0.2f, 0.95f, 0.65f), new Color(1f, 0.8f, 0.2f));
        left.AddComponent<OrbitMotion>().Configure(new Vector3(0f, 1.45f, 1.4f), 0.45f, 0.7f, Mathf.PI);
        var right = Add("orbit-b", PrimitiveType.Sphere, VisionObject.PrimitiveShape.Sphere,
            new Vector3(0.45f, 1.45f, 1.4f), Vector3.one * 0.15f,
            new Color(0.95f, 0.3f, 0.65f), new Color(1f, 0.8f, 0.2f));
        right.AddComponent<OrbitMotion>().Configure(new Vector3(0f, 1.45f, 1.4f), 0.45f, -0.7f, 0f);

        // This is a real Mesh asset, not a Unity primitive. The exporter writes
        // its vertices and triangles to USDA for RealityKit.
        var ring = new GameObject("portal-ring");
        ring.transform.position = new Vector3(0f, 1.45f, 1.65f);
        ring.transform.localScale = Vector3.one;
        ring.AddComponent<MeshFilter>().sharedMesh = GetOrCreateRingMesh();
        ring.AddComponent<MeshRenderer>();
        ring.AddComponent<VisionObject>().Configure(
            "portal-ring", VisionObject.PrimitiveShape.Model,
            new Color(0.65f, 0.42f, 1f), new Color(1f, 0.85f, 0.3f), "portal-ring");

        // Imported OBJ is only the Unity authoring copy. Its mesh and assigned
        // texture are exported as USD for the native RealityKit presentation.
        const string modelPath = "Assets/Models/UserModel/result.obj";
        var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
        var modelMesh = System.Array.Find(AssetDatabase.LoadAllAssetsAtPath(modelPath),
            asset => asset is Mesh) as Mesh;
        var modelTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Models/UserModel/material_0.jpeg");
        if (modelMesh != null && modelTexture != null)
        {
            var userModel = new GameObject("user-model");
            userModel.transform.position = new Vector3(1.15f, 1.55f, 2.15f);
            userModel.transform.localScale = Vector3.one * 0.45f;
            userModel.AddComponent<MeshFilter>().sharedMesh = modelMesh;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { mainTexture = modelTexture };
            System.IO.Directory.CreateDirectory("Assets/Generated");
            const string materialPath = "Assets/Generated/UserModel.mat";
            var oldMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (oldMaterial == null) AssetDatabase.CreateAsset(material, materialPath);
            else { oldMaterial.mainTexture = modelTexture; material = oldMaterial; }
            userModel.AddComponent<MeshRenderer>().sharedMaterial = material;
            userModel.AddComponent<VisionObject>().Configure(
                "user-model", VisionObject.PrimitiveShape.Model,
                Color.white, Color.yellow, "user-model");
            userModel.AddComponent<UserModelBehaviour>();
            var chime = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/chime.wav");
            if (chime != null) userModel.AddComponent<VisionAudioSource>().Configure(chime, "chime");
        }

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        const string path = "Assets/Scenes/InteractiveSculpture.unity";
        EditorSceneManager.SaveScene(scene, path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        Debug.Log("Saved interactive sculpture and made it the only build scene: " + path);
    }

    private static Mesh GetOrCreateRingMesh()
    {
        System.IO.Directory.CreateDirectory("Assets/Generated");
        const string path = "Assets/Generated/PortalRing.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        const int majorSegments = 32;
        const int minorSegments = 10;
        var vertices = new Vector3[majorSegments * minorSegments];
        var triangles = new int[majorSegments * minorSegments * 6];
        for (int major = 0; major < majorSegments; major++)
        {
            float a = major * Mathf.PI * 2f / majorSegments;
            var radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            for (int minor = 0; minor < minorSegments; minor++)
            {
                float b = minor * Mathf.PI * 2f / minorSegments;
                vertices[major * minorSegments + minor] =
                    radial * (0.55f + Mathf.Cos(b) * 0.055f) +
                    Vector3.forward * (Mathf.Sin(b) * 0.055f);
                int nextMajor = ((major + 1) % majorSegments) * minorSegments;
                int nextMinor = (minor + 1) % minorSegments;
                int i = (major * minorSegments + minor) * 6;
                triangles[i] = major * minorSegments + minor;
                triangles[i + 1] = nextMajor + minor;
                triangles[i + 2] = major * minorSegments + nextMinor;
                triangles[i + 3] = major * minorSegments + nextMinor;
                triangles[i + 4] = nextMajor + minor;
                triangles[i + 5] = nextMajor + nextMinor;
            }
        }
        var mesh = new Mesh { name = "PortalRing" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static GameObject Add(string id, PrimitiveType primitive, VisionObject.PrimitiveShape shape,
        Vector3 position, Vector3 scale, Color normal, Color selected)
    {
        var gameObject = GameObject.CreatePrimitive(primitive);
        gameObject.name = id;
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.AddComponent<VisionObject>().Configure(id, shape, normal, selected);
        return gameObject;
    }
}
