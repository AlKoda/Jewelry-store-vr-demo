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
