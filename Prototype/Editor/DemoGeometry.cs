#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Primitive, material, text and mesh helpers shared by the scene generators.
// Generators import it with `using static DemoGeometry;`.
public static class DemoGeometry
{
    public static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

    public static Transform Group(string name, Transform parent)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    public static GameObject Box(string name, Transform parent, Vector3 p, Vector3 size, Material material = null, bool collision = false)
        => Primitive(name, PrimitiveType.Cube, parent, p, size, material, collision);

    public static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 p, Vector3 size,
        Material material = null, bool collision = false)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = p;
        g.transform.localScale = size;
        if (material != null) g.GetComponent<Renderer>().sharedMaterial = material;
        if (!collision) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    // Built-in font text; readable from a camera looking along the text's forward.
    public static TextMesh Text(string name, Transform parent, string value, Vector3 p, float yaw, float size, Color color)
    {
        Transform g = Group(name, parent);
        g.localPosition = p;
        g.localRotation = Quaternion.Euler(0, yaw, 0);
        TextMesh text = g.gameObject.AddComponent<TextMesh>();
        DeployedTool.EnsureFont(text);
        text.text = value;
        text.fontSize = 64;
        text.characterSize = size;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;
        return text;
    }

    // Flat Standard material saved as an asset so scene references survive reopening.
    public static Material Mat(string folder, string name, Color color, float glossiness)
    {
        string path = folder + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = color;
        m.SetFloat("_Glossiness", glossiness);
        return m;
    }

    // Standard-shader transparent (premultiplied) mode; alpha comes from the colour.
    public static Material Translucent(Material material)
    {
        material.SetFloat("_Mode", 3);
        material.SetInt("_SrcBlend", (int)BlendMode.One);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }

    // Standard-shader emission on a saved material, so light fixtures read as lit.
    public static Material Glowing(Material material, Color emission)
    {
        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        material.SetColor("_EmissionColor", emission);
        return material;
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    // Persists a generated mesh; an existing asset at the path is updated and reused.
    public static void SaveMesh(ref Mesh mesh, string path)
    {
        PrepareForTextures(mesh);
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return; }
        EditorUtility.CopySerialized(mesh, existing);
        Object.DestroyImmediate(mesh);
        mesh = existing;
    }

    // Generated meshes carry positions only; textured materials need UVs and normal
    // maps need tangents. Box projection (one metre per repeat) from the dominant
    // normal axis suits the boxy lofts, tiles and loops the generators produce.
    public static void PrepareForTextures(Mesh mesh)
    {
        if (mesh.vertexCount == 0) return;
        if (mesh.normals == null || mesh.normals.Length != mesh.vertexCount) mesh.RecalculateNormals();
        if (mesh.uv == null || mesh.uv.Length != mesh.vertexCount)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 n = normals[i], v = vertices[i];
                float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
                uv[i] = ay >= ax && ay >= az ? new Vector2(v.x, v.z) : ax >= az ? new Vector2(v.z, v.y) : new Vector2(v.x, v.y);
            }
            mesh.uv = uv;
        }
        mesh.RecalculateTangents();
    }

    // Replaces every mesh under root with one combined, shadowless mesh per material.
    public static void CombineByMaterial(Transform root, string assetPrefix, string namePrefix)
    {
        Dictionary<Material, List<CombineInstance>> batches = new Dictionary<Material, List<CombineInstance>>();
        List<GameObject> originals = new List<GameObject>();
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
        {
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || filter.sharedMesh == null) continue;
            Material material = renderer.sharedMaterial;
            if (!batches.ContainsKey(material)) batches[material] = new List<CombineInstance>();
            batches[material].Add(new CombineInstance
            {
                mesh = filter.sharedMesh,
                transform = root.worldToLocalMatrix * filter.transform.localToWorldMatrix
            });
            originals.Add(filter.gameObject);
        }
        int serial = 0;
        foreach (KeyValuePair<Material, List<CombineInstance>> batch in batches)
        {
            Mesh mesh = new Mesh { name = namePrefix + batch.Key.name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(batch.Value.ToArray(), true, true);
            SaveMesh(ref mesh, assetPrefix + serial++ + ".asset");
            GameObject g = new GameObject(mesh.name);
            g.transform.SetParent(root, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = batch.Key;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        foreach (GameObject g in originals) Object.DestroyImmediate(g);
    }
}
#endif
