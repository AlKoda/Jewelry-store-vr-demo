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

    // The path of t under root, names joined by '/'.
    public static string PathUnder(Transform t, Transform root)
    {
        string path = t.name;
        for (t = t.parent; t != null && t != root; t = t.parent) path = t.name + "/" + path;
        return path;
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

    // Standard-shader fade mode (straight alpha: diffuse and reflections both fade with the
    // colour or texture alpha), for decals and glow cards; queue orders stacked layers.
    public static Material Fade(Material material, int queue)
    {
        material.SetFloat("_Mode", 2);
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = queue;
        return material;
    }

    // Standard-shader cutout mode: texels with alpha below cutoff are discarded, the rest
    // draw opaque with depth (grilles, leaf cards, hair cards).
    public static Material Cutout(Material material, float cutoff)
    {
        material.SetFloat("_Mode", 1);
        material.SetInt("_SrcBlend", (int)BlendMode.One);
        material.SetInt("_DstBlend", (int)BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.EnableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.SetFloat("_Cutoff", cutoff);
        material.SetOverrideTag("RenderType", "TransparentCutout");
        material.renderQueue = 2450;
        return material;
    }

    // Standard-shader opaque mode, so a rebuilt material asset never keeps an earlier blend state.
    public static Material Opaque(Material material)
    {
        material.SetFloat("_Mode", 0);
        material.SetInt("_SrcBlend", (int)BlendMode.One);
        material.SetInt("_DstBlend", (int)BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.SetOverrideTag("RenderType", "");
        material.renderQueue = -1;
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
    // lightmapUVs is opt-in so the historical generators keep their meshes unchanged.
    // lightmapScale is the renderer's planned scaleInLightmap (sizes the chart margin).
    public static void SaveMesh(ref Mesh mesh, string path, bool lightmapUVs = false, float lightmapScale = 1)
    {
        PrepareForTextures(mesh);
        if (lightmapUVs) PrepareForLightmaps(mesh, lightmapScale);
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

    // Texel density of the night bake (NightLighting's lightmapResolution, scaled per zone
    // by NightLighting.LightmapScales): 20 per metre, 5 cm on the interior shell where the
    // headlights' case shadows fall. Sets the lightmap chart margin, so the two stay in step.
    public const float LightmapTexelsPerMetre = 20;

    // Non-overlapping second UV set so the mesh can receive baked lightmaps. The pack
    // margin is about two lightmap texels at the planned density, i.e. the bake's
    // texels per metre times the renderer's scaleInLightmap (lightmapScale); Unity's
    // default 1/256 is a fraction of a texel on a small mesh, so neighbouring charts
    // bleed. The unwrap may split vertices along seams, so tangents are rebuilt afterwards.
    public static void PrepareForLightmaps(Mesh mesh, float lightmapScale = 1)
    {
        if (mesh.vertexCount == 0) return;
        UnwrapParam.SetDefaults(out UnwrapParam settings);
        float side = Mathf.Sqrt(Mathf.Max(SurfaceArea(mesh), 1e-6f)) * LightmapTexelsPerMetre * Mathf.Max(lightmapScale, 0.01f);
        settings.packMargin = Mathf.Clamp(2f / side, 1f / 256, 0.05f);
        Unwrapping.GenerateSecondaryUVSet(mesh, settings);
        mesh.RecalculateTangents();
    }

    public static float SurfaceArea(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        float area = 0;
        for (int i = 0; i + 2 < triangles.Length; i += 3)
            area += Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]).magnitude * 0.5f;
        return area;
    }

    // Replaces every mesh under root with one combined, shadowless mesh per material.
    // Reads each renderer's first material and submesh only (the small shop's parts are
    // single-material primitives); CombineSubmeshesByMaterial handles multi-material parts.
    public static void CombineByMaterial(Transform root, string assetPrefix, string namePrefix)
    {
        Combine(root, assetPrefix, namePrefix, false);
    }

    // For multi-material parts (KitMesh props): every submesh joins its material's mesh,
    // and the parts' whole hierarchies under root (root itself carries no mesh) are
    // replaced by one renderer per material. Opaque batches keep their shadows, so
    // props still shadow under the flashlight; transparent ones cast and receive none.
    public static List<MeshRenderer> CombineSubmeshesByMaterial(Transform root, string assetPrefix, string namePrefix)
    {
        return Combine(root, assetPrefix, namePrefix, true);
    }

    private static List<MeshRenderer> Combine(Transform root, string assetPrefix, string namePrefix, bool submeshes)
    {
        Dictionary<Material, List<CombineInstance>> batches = new Dictionary<Material, List<CombineInstance>>();
        List<GameObject> originals = new List<GameObject>();
        List<GameObject> parts = new List<GameObject>();
        foreach (Transform child in root) parts.Add(child.gameObject);
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
        {
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || filter.sharedMesh == null) continue;
            Material[] materials = renderer.sharedMaterials;
            int count = submeshes ? Mathf.Min(filter.sharedMesh.subMeshCount, materials.Length) : 1;
            for (int i = 0; i < count; i++)
            {
                Material material = submeshes ? materials[i] : renderer.sharedMaterial;
                if (submeshes && material == null) continue;
                if (!batches.ContainsKey(material)) batches[material] = new List<CombineInstance>();
                batches[material].Add(new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    subMeshIndex = i,
                    transform = root.worldToLocalMatrix * filter.transform.localToWorldMatrix
                });
            }
            originals.Add(filter.gameObject);
        }
        List<MeshRenderer> made = new List<MeshRenderer>();
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
            bool shadows = submeshes && batch.Key.renderQueue < 3000;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = shadows;
            made.Add(r);
        }
        if (submeshes) foreach (GameObject g in parts) Object.DestroyImmediate(g);
        else foreach (GameObject g in originals) Object.DestroyImmediate(g);
        return made;
    }
}
#endif
