#if UNITY_EDITOR
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

    public static GameObject Load(string pack, string model)
        => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + pack + "/" + model + ".obj");

    // Unpacked instance (meshes still reference the asset), uniformly scaled, without colliders or shadows.
    public static GameObject Place(string pack, string model, Transform parent, Vector3 localPosition, float yaw, float scale)
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
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        return instance;
    }
}
#endif
