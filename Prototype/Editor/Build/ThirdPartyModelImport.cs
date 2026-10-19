#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Third-party models (KayKit, Kenney, Khronos, NightModelReuse and the StoreKit overrides under
// Assets/CrimeSceneDemo/ThirdParty) import with a generated second UV set, so the static ones
// (police car, streetlights, towers) can take baked lightmaps in the night store. The twelve
// realism packs (PackImport) are probe-lit props instead (critic B3), without that UV set,
// except the buildings across the street and the office rug: their charts get margins padded
// for the texel density they bake at (6 and 16 per metre, not the 40 per unit the importer
// assumes by default). Pack models also skip animation, cameras and lights, and those whose
// OBJ has no normals get calculated ones at a fixed smoothing angle. Every other import
// setting is left as it is; the extra channel changes nothing for scenes that are not baked.
public sealed class ThirdPartyModelImport : AssetPostprocessor
{
    private static bool Applies(string path) => path.StartsWith(DemoAssetLibrary.Root + "/", StringComparison.Ordinal);

    private static bool Starts(string text, string prefix) => text.StartsWith(prefix, StringComparison.Ordinal);

    // Second UV set: every model outside the packs (today's behaviour); in the packs only the
    // StreetModels buildings and the InteriorModels rug, _LOD files included.
    public static bool WantsLightmapUVs(string path)
    {
        string pack = PackImport.PackOf(path);
        if (pack == null) return true;
        string stem = Path.GetFileNameWithoutExtension(path);
        return pack == "StreetModels" && (Starts(stem, "ApartmentBlock") || Starts(stem, "Building"))
            || pack == "InteriorModels" && Starts(stem, "Rug");
    }

    // Texels per metre the chart margins are padded for (NightLighting's zone scales at the
    // bake's DemoGeometry.LightmapTexelsPerMetre, rounded so the stored value compares exactly):
    // 6 for the buildings (the street zone), 16 for the rug (fixtures). 0 leaves the margin
    // settings alone (the importer's default assumes 40 texels per unit, which the KayKit car
    // and lamps exceed).
    public static float LightmapResolution(string path)
    {
        string pack = PackImport.PackOf(path);
        if (pack == null || !WantsLightmapUVs(path)) return 0;
        float scale = pack == "StreetModels" ? NightLighting.StreetScale : NightLighting.FixtureScale;
        return Mathf.Round(DemoGeometry.LightmapTexelsPerMetre * scale);
    }

    // Pack models whose OBJ has no vn lines (measured; their _LOD files too) by name prefix:
    // hard-edged street furniture and buildings at 30 degrees, the organic jewellery and the
    // forged tools at 60. 0 for every other model, which keeps importing its own normals.
    private static readonly string[] HardEdged =
    {
        "ApartmentBlock", "Building", "TrafficLight", "Bollard", "BikeRack", "ParkingMeter", "StormDrainGrate",
        "WasteContainer1100L", "BusShelter", "SignNoStopping", "SignOneWayStreetName", "StreetNamePlate", "PoliceLightBar",
    };
    private static readonly string[] SmoothJewellery = { "RingAntiqueTigerEye", "EarringPearlDrop", "EarringGoldLeaf", "PocketWatchVintage" };
    private static readonly string[] SmoothTools = { "Crowbar", "Sledgehammer", "FlatheadScrewdriver" };

    public static float SmoothingAngle(string path)
    {
        string pack = PackImport.PackOf(path);
        string[] names = pack == "StreetModels" ? HardEdged : pack == "JewelryModels" ? SmoothJewellery : pack == "ForensicModels" ? SmoothTools : null;
        if (names == null) return 0;
        string stem = Path.GetFileNameWithoutExtension(path);
        foreach (string name in names)
            if (Starts(stem, name)) return pack == "StreetModels" ? 30 : 60;
        return 0;
    }

    // True when the importer already follows the rules above; with write it also takes them
    // (the caller saves and reimports). Models outside ThirdParty match trivially.
    public static bool Expected(ModelImporter m, string path, bool write)
    {
        if (m == null || !Applies(path)) return true;
        bool ok = true;
        void Want<T>(T current, T wanted, Action<T> set)
        {
            if (EqualityComparer<T>.Default.Equals(current, wanted)) return;
            ok = false;
            if (write) set(wanted);
        }

        bool uv2 = WantsLightmapUVs(path);
        Want(m.generateSecondaryUV, uv2, x => m.generateSecondaryUV = x);
        float resolution = LightmapResolution(path);
        if (uv2 && resolution > 0)
        {
            Want(m.secondaryUVMarginMethod, ModelImporterSecondaryUVMarginMethod.Calculate, x => m.secondaryUVMarginMethod = x);
            Want(m.secondaryUVMinLightmapResolution, resolution, x => m.secondaryUVMinLightmapResolution = x);
            Want(m.secondaryUVMinObjectScale, 1f, x => m.secondaryUVMinObjectScale = x);
        }
        if (PackImport.Governs(path))
        {
            Want(m.importAnimation, false, x => m.importAnimation = x);
            Want(m.importCameras, false, x => m.importCameras = x);
            Want(m.importLights, false, x => m.importLights = x);
            float angle = SmoothingAngle(path);
            if (angle > 0)
            {
                Want(m.importNormals, ModelImporterNormals.Calculate, x => m.importNormals = x);
                Want(m.normalSmoothingAngle, angle, x => m.normalSmoothingAngle = x);
            }
        }
        return ok;
    }

    private void OnPreprocessModel()
    {
        if (Applies(assetPath)) Expected(assetImporter as ModelImporter, assetPath, true);
    }

    // The imported meshes carry a second UV set exactly when one is wanted (the first mesh
    // stands for the model: the importer setting covers all of them).
    private static bool HasExpectedUV2(string path)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        return mesh == null || mesh.vertexCount == 0 || mesh.HasVertexAttribute(VertexAttribute.TexCoord1) == WantsLightmapUVs(path);
    }

    // Reimports, in one batch, the models imported before this postprocessor or its current
    // rules existed: a second UV set missing or no longer wanted (the pack props), chart margins
    // for the wrong density (buildings, rug), or other pack settings that differ. Then brings
    // the pack textures and audio in line (PackImport.Apply). Returns how many assets were
    // reimported; cheap when everything is current.
    public static int GenerateLightmapUVs()
    {
        if (!AssetDatabase.IsValidFolder(DemoAssetLibrary.Root)) return 0;
        int count = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { DemoAssetLibrary.Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                bool settings = Expected(importer, path, false);
                if (settings && HasExpectedUV2(path)) continue;
                if (!settings)
                {
                    Expected(importer, path, true);
                    importer.SaveAndReimport();
                }
                // Settings already right over meshes imported before them: nothing is dirty,
                // so SaveAndReimport could keep the cached import; force it.
                else AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                count++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        return count + PackImport.Apply();
    }
}
#endif
