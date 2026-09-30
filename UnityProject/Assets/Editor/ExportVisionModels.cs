using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Exports static MeshFilter geometry from objects marked VisionObject(Model).
// USD ASCII is an open interchange format that RealityKit can load. This MVP
// exports one base-color texture and UV set when the MeshRenderer has one.
public static class ExportVisionModels
{
    private const string ExportDirectory = "Assets/VisionExport/Models";
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    // Batch entry point: a fresh Editor session need not reopen the build scene.
    public static void ExportBuildScene()
    {
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length != 1 || !scenes[0].enabled)
            throw new InvalidOperationException("Expected one enabled build scene");
        EditorSceneManager.OpenScene(scenes[0].path);
        ExportActiveScene();
    }

    [MenuItem("MVP/Export static models")]
    public static void ExportActiveScene()
    {
        Directory.CreateDirectory(ExportDirectory);
        // This folder is generated output. Remove models from an earlier scene
        // so a later build never bundles unused assets by accident.
        foreach (var file in Directory.GetFiles(ExportDirectory))
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension == ".usda" || extension == ".png" ||
                extension == ".jpg" || extension == ".jpeg")
                File.Delete(file);
        }
        var exported = new HashSet<string>();
        foreach (var item in UnityEngine.Object.FindObjectsByType<VisionObject>(FindObjectsSortMode.None))
        {
            if (!item.isActiveAndEnabled || item.ShapeName != "model") continue;
            var key = item.ModelKey;
            if (string.IsNullOrEmpty(key) || !Regex.IsMatch(key, "^[A-Za-z0-9_-]+$"))
                throw new InvalidOperationException("Invalid model asset key on " + item.name);
            if (!exported.Add(key))
                throw new InvalidOperationException("Duplicate model asset key: " + key);
            var filter = item.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                throw new InvalidOperationException("Model needs MeshFilter: " + item.name);
            var mesh = filter.sharedMesh;
            if (!mesh.isReadable)
                throw new InvalidOperationException("Enable Read/Write on mesh import settings: " + item.name);
            var path = Path.Combine(ExportDirectory, key + ".usda");
            string textureFile = null;
            var renderer = item.GetComponent<MeshRenderer>();
            var texture = renderer != null && renderer.sharedMaterial != null
                ? renderer.sharedMaterial.mainTexture : null;
            if (texture != null)
            {
                string source = AssetDatabase.GetAssetPath(texture);
                if (mesh.uv.Length != mesh.vertexCount || !File.Exists(source))
                    throw new InvalidOperationException("Textured model needs UVs and a source image: " + item.name);
                textureFile = key + Path.GetExtension(source).ToLowerInvariant();
                File.Copy(source, Path.Combine(ExportDirectory, textureFile), true);
            }
            // USD ASCII requires '#usda' as the first bytes: no UTF-8 BOM.
            File.WriteAllText(path, ToUSDA(mesh, textureFile), new UTF8Encoding(false));
            Debug.Log("Exported static model " + key + " → " + path);
        }
        AssetDatabase.Refresh();
    }

    private static string Number(float value) => value.ToString("R", Invariant);
    private static string Tuple(Vector3 value) =>
        "(" + Number(value.x) + ", " + Number(value.y) + ", " + Number(-value.z) + ")";

    private static string ToUSDA(Mesh mesh, string textureFile)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        if (vertices.Length == 0 || triangles.Length == 0 || triangles.Length % 3 != 0)
            throw new InvalidOperationException("Mesh has no valid triangle geometry: " + mesh.name);
        var b = new StringBuilder();
        b.AppendLine("#usda 1.0");
        b.AppendLine("(");
        b.AppendLine("    defaultPrim = \"Model\"");
        b.AppendLine("    metersPerUnit = 1");
        b.AppendLine("    upAxis = \"Y\"");
        b.AppendLine(")");
        b.AppendLine("def Xform \"Model\" {");
        if (textureFile != null)
            b.AppendLine("    def Mesh \"Geometry\" (prepend apiSchemas = [\"MaterialBindingAPI\"]) {");
        else
            b.AppendLine("    def Mesh \"Geometry\" {");
        b.AppendLine("        uniform token subdivisionScheme = \"none\"");
        if (textureFile != null)
        {
            b.AppendLine("        rel material:binding = </Model/Material>");
        }
        b.Append("        point3f[] points = [");
        for (int i = 0; i < vertices.Length; i++)
        {
            if (i > 0) b.Append(", ");
            b.Append(Tuple(vertices[i]));
        }
        b.AppendLine("]");
        b.Append("        int[] faceVertexCounts = [");
        for (int i = 0; i < triangles.Length / 3; i++)
        {
            if (i > 0) b.Append(", ");
            b.Append('3');
        }
        b.AppendLine("]");
        b.Append("        int[] faceVertexIndices = [");
        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (i > 0) b.Append(", ");
            // Negating Z flips handedness; reverse winding to preserve faces.
            b.Append(triangles[i]).Append(", ").Append(triangles[i + 2]).Append(", ").Append(triangles[i + 1]);
        }
        b.AppendLine("]");
        var normals = mesh.normals;
        if (normals.Length == vertices.Length)
        {
            b.Append("        normal3f[] normals = [");
            for (int i = 0; i < normals.Length; i++)
            {
                if (i > 0) b.Append(", ");
                b.Append(Tuple(normals[i]));
            }
            b.AppendLine("] (interpolation = \"vertex\")");
        }
        if (textureFile != null)
        {
            var uvs = mesh.uv;
            b.Append("        texCoord2f[] primvars:st = [");
            for (int i = 0; i < uvs.Length; i++)
            {
                if (i > 0) b.Append(", ");
                b.Append('(').Append(Number(uvs[i].x)).Append(", ").Append(Number(uvs[i].y)).Append(')');
            }
            b.AppendLine("] (interpolation = \"vertex\")");
        }
        b.AppendLine("    }");
        if (textureFile != null)
        {
            b.AppendLine("    def Material \"Material\" {");
            b.AppendLine("        token outputs:surface.connect = </Model/Material/Surface.outputs:surface>");
            b.AppendLine("        def Shader \"Surface\" {");
            b.AppendLine("            uniform token info:id = \"UsdPreviewSurface\"");
            b.AppendLine("            color3f inputs:diffuseColor.connect = </Model/Material/Texture.outputs:rgb>");
            b.AppendLine("            token outputs:surface");
            b.AppendLine("        }");
            b.AppendLine("        def Shader \"UVReader\" {");
            b.AppendLine("            uniform token info:id = \"UsdPrimvarReader_float2\"");
            b.AppendLine("            string inputs:varname = \"st\"");
            b.AppendLine("            float2 outputs:result");
            b.AppendLine("        }");
            b.AppendLine("        def Shader \"Texture\" {");
            b.AppendLine("            uniform token info:id = \"UsdUVTexture\"");
            b.Append("            asset inputs:file = @").Append(textureFile).AppendLine("@");
            b.AppendLine("            float2 inputs:st.connect = </Model/Material/UVReader.outputs:result>");
            b.AppendLine("            token inputs:sourceColorSpace = \"sRGB\"");
            b.AppendLine("            float3 outputs:rgb");
            b.AppendLine("        }");
            b.AppendLine("    }");
        }
        b.AppendLine("}");
        return b.ToString();
    }
}
