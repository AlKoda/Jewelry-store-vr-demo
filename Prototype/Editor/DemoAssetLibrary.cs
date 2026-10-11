#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Third-party models under Assets/CrimeSceneDemo/ThirdParty. Generators ask
// for a model by pack and name and get a placed instance, or null when the
// files are absent, so every generator keeps its primitive fallback.
public static class DemoAssetLibrary
{
    public const string Root = "Assets/CrimeSceneDemo/ThirdParty";

    public static bool Has(string pack, string model) => Load(pack, model) != null;

    // One box collider on the root covering every renderer, in the root's local space.
    public static BoxCollider AddBoundsCollider(GameObject instance)
    {
        Bounds bounds = LocalBounds(instance);
        BoxCollider collider = instance.AddComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = bounds.size;
        return collider;
    }

    public static Bounds LocalBounds(GameObject instance)
    {
        Bounds bounds = new Bounds();
        bool first = true;
        foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            Matrix4x4 toRoot = instance.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            Bounds mesh = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mesh.center + Vector3.Scale(mesh.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = toRoot.MultiplyPoint3x4(corner);
                if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                else bounds.Encapsulate(p);
            }
        }
        return bounds;
    }

    // OBJ first (what the package-free project imports from glTF via Tools/glb_to_obj.py),
    // then FBX for models converted elsewhere. GLB needs a package and is not tried.
    public static GameObject Load(string pack, string model)
    {
        GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + pack + "/" + model + ".obj");
        return obj != null ? obj : AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + pack + "/" + model + ".fbx");
    }

    public static string Path(string pack, string file) => Root + "/" + pack + "/" + file;
    public static bool HasTexture(string pack, string file) => Texture(pack, file) != null;
    public static Texture2D Texture(string pack, string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(Path(pack, file));

    // A normal map from a pack, re-imported as a normal map the first time so the
    // Standard shader unpacks it correctly; null when the file is absent.
    public static Texture2D NormalMap(string pack, string file) => NormalMapAt(Path(pack, file));

    public static Texture2D NormalMapAt(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        if (importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // Gives a saved material an albedo (the texture carries the colour, a light tint of
    // the saved colour is kept so it does not darken it) and an optional normal map.
    public static void ApplyTextures(Material m, Texture2D albedo, Texture2D normal, float bump)
    {
        m.mainTexture = albedo;
        m.color = Color.Lerp(m.color, Color.white, 0.75f);
        if (normal == null) return;
        m.EnableKeyword("_NORMALMAP");
        m.SetTexture("_BumpMap", normal);
        m.SetFloat("_BumpScale", bump);
    }

    // Each imported OBJ carries its own copy of the pack's atlas material. Models of
    // one pack share the first copy seen, so a kit combines into one draw per atlas.
    private static readonly Dictionary<string, Material> atlases = new Dictionary<string, Material>();
    private static Material Shared(string pack, Material material)
    {
        if (material == null) return null;
        string key = pack + "/" + material.name;
        if (!atlases.TryGetValue(key, out Material shared) || shared == null) atlases[key] = shared = material;
        return shared;
    }

    // Unpacked instance (meshes still reference the asset), uniformly scaled, without colliders.
    // Kit props (the default) drop shadows and share the pack's atlas material by name; a
    // full-size model (kitProp false: StoreKit overrides) keeps its own materials, whose
    // names image-to-3D exports repeat across models, and casts and receives shadows.
    public static GameObject Place(string pack, string model, Transform parent, Vector3 localPosition, float yaw, float scale, bool kitProp = true)
    {
        GameObject asset = Load(pack, model);
        if (asset == null) return null;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.name = model;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        instance.transform.localScale = Vector3.one * scale;
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
        foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.shadowCastingMode = kitProp ? ShadowCastingMode.Off : ShadowCastingMode.On;
            renderer.receiveShadows = !kitProp;
            if (kitProp && renderer.sharedMaterials.Length == 1) renderer.sharedMaterial = Shared(pack, renderer.sharedMaterial);
        }
        return instance;
    }
}
#endif
