#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// One rigid part (a cabinet body, a door leaf, a pane) of the fixture library: the
// runtime ProceduralMesh faces plus asset saving. One submesh per material, so a
// fixture costs one renderer per moving part instead of one per box. Saved meshes
// get a lightmap UV set (DemoGeometry.SaveMesh with lightmapUVs) unless the part is
// loose (Lightmapped false: dropped jewellery, scattered boxes), which only takes
// light probes and so needs no second UV set.
public sealed class KitMesh : ProceduralMesh
{
    public bool Lightmapped = true;
    // Planned scaleInLightmap of the emitted renderer: sizes the lightmap chart margin.
    public float LightmapScale = 1;

    // Stable fingerprint of the geometry: identical parts resolve to one saved asset.
    private int Signature()
    {
        unchecked
        {
            int h = vertices.Count * 397 ^ TriangleCount;
            foreach (Vector3 p in vertices)
                h = h * 31 + Mathf.RoundToInt(p.x * 1000) * 7 + Mathf.RoundToInt(p.y * 1000) * 13 + Mathf.RoundToInt(p.z * 1000);
            // Atlas rects (rect Quad: decals, wall graphics) are part of the geometry, so a
            // changed atlas JSON or cell gives a new asset instead of the session's cached mesh
            // with stale UVs. Position-derived UVs add nothing, so other parts keep their names.
            if (explicitUVs)
                foreach (Vector2 uv in uvs) h = h * 31 + Mathf.RoundToInt(uv.x * 100000) * 7 + Mathf.RoundToInt(uv.y * 100000);
            foreach (Material m in materials) h = h * 31 + (m != null ? m.name.GetHashCode() : 0);
            if (!Lightmapped) h = h * 31 + 1; // Same geometry without uv2 is a different asset.
            else if (LightmapScale != 1) h = h * 31 + Mathf.RoundToInt(LightmapScale * 1000); // Different chart margins.
            return h;
        }
    }

    private static readonly Dictionary<string, Mesh> saved = new Dictionary<string, Mesh>();

    // Every mesh asset path saved or reused since the editor domain loaded.
    private static readonly HashSet<string> touched = new HashSet<string>();

    // Counts mesh assets in folder that nothing saved or reused in this editor session,
    // i.e. the fingerprints left behind by earlier geometry (reported, not removed). Only
    // meaningful after building everything that should keep its meshes; the gallery run reports it.
    public static int CountUntouched(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return 0;
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".asset") && !touched.Contains(path)) count++;
        }
        return count;
    }

    // Deletes those untouched mesh assets. Call it only after building everything that
    // should keep its meshes (the gallery run builds every fixture), or it removes meshes
    // used by scenes this session did not rebuild. Not called automatically; run it by hand.
    public static int DeleteUntouched(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return 0;
        int deleted = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".asset") || touched.Contains(path)) continue;
            if (AssetDatabase.DeleteAsset(path)) deleted++;
        }
        return deleted;
    }

    // Saves (or reuses) the mesh as <folder>/<name>_<fingerprint>.asset. Parts with
    // identical geometry share one asset, so repeated fixtures do not re-unwrap.
    public Mesh Save(string folder, string name)
    {
        string path = folder + "/" + name + "_" + Signature().ToString("x8") + ".asset";
        touched.Add(path);
        if (saved.TryGetValue(path, out Mesh cached) && cached != null) return cached;
        Mesh mesh = BuildMesh(name);
        DemoGeometry.SaveMesh(ref mesh, path, Lightmapped, LightmapScale);
        saved[path] = mesh;
        return mesh;
    }

    // Child object carrying this part; glass-only parts cast no shadows.
    public GameObject Emit(string name, Transform parent, Vector3 position, Quaternion rotation, string folder, string assetName)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        g.transform.localRotation = rotation;
        if (IsEmpty) return g;
        g.AddComponent<MeshFilter>().sharedMesh = Save(folder, assetName);
        MeshRenderer renderer = g.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = materials.ToArray();
        bool transparent = true;
        foreach (Material m in materials) transparent &= m != null && m.renderQueue >= 3000;
        if (transparent) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; }
        return g;
    }
}
#endif
