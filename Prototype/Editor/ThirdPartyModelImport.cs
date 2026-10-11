#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

// Third-party models (KayKit, Kenney, Khronos OBJs under Assets/CrimeSceneDemo/ThirdParty)
// import with a generated second UV set, so the static ones (police car, streetlights,
// towers) can take baked lightmaps in the night store. Every other import setting is
// left as it is; the extra channel changes nothing for scenes that are not baked.
public sealed class ThirdPartyModelImport : AssetPostprocessor
{
    private static bool Applies(string path) => path.StartsWith(DemoAssetLibrary.Root + "/", StringComparison.Ordinal);

    private void OnPreprocessModel()
    {
        ModelImporter importer = assetImporter as ModelImporter;
        if (importer != null && Applies(assetPath) && !importer.generateSecondaryUV) importer.generateSecondaryUV = true;
    }

    // Reimports models imported before this postprocessor existed whose meshes still
    // lack the second UV set; returns how many. Cheap when everything is current.
    public static int GenerateLightmapUVs()
    {
        if (!AssetDatabase.IsValidFolder(DemoAssetLibrary.Root)) return 0;
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { DemoAssetLibrary.Root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null || mesh.vertexCount == 0 || mesh.uv2.Length == mesh.vertexCount) continue;
            importer.generateSecondaryUV = true;
            importer.SaveAndReimport();
            count++;
        }
        return count;
    }
}
#endif
