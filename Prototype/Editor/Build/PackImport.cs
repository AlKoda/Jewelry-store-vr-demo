#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Import settings of the twelve realism packs under Assets/CrimeSceneDemo/ThirdParty, from one
// rule table keyed on the pack folder and the file's suffix (critic B5): colour maps sRGB,
// normal maps as normal maps, packed metallic-smoothness, AO and masks linear, the light
// cookies and night skies with their own shape, wrap and mipmaps, cut-out maps with
// coverage-preserving mipmaps, size, filtering and compression per pack, and load types for
// the soundscape and the music. Applied on every import of a governed file; Apply catches up
// files imported before these rules existed. Everything else (ReferenceTextures, NightMaterials,
// NightAudio, KayKit, StoreKit, ...) and the folders Unity ignores (Source~, Generators~) keep
// the settings they have, and a pack that was not synced does nothing.
public sealed class PackImport : AssetPostprocessor
{
    public static readonly string[] Packs =
    {
        "PBRInterior", "PBRExterior", "NightSky", "JewelryModels", "ForensicModels", "StreetModels",
        "InteriorModels", "WallGraphics", "SurfaceDecals", "NightSoundscape", "Music", "PeopleModels",
        "AmbientProps", "AmbientProps2", "CutoutDecals", "RealFoley", "RealFoley2", "RealismTextures",
    };

    // The pack whose folder holds the path; null outside the twelve and under a trailing-~ folder.
    public static string PackOf(string path)
    {
        if (string.IsNullOrEmpty(path) || path.Contains("~/")) return null;
        foreach (string pack in Packs)
            if (path.StartsWith(DemoAssetLibrary.Root + "/" + pack + "/", StringComparison.Ordinal)) return pack;
        return null;
    }

    public static bool Governs(string path) => PackOf(path) != null;

    // Bump on every rule change. Unity then reimports every texture and audio clip of the
    // project once (it cannot tell which files a postprocessor governs).
    public override uint GetVersion() => 1;

    private void OnPreprocessTexture()
    {
        if (Governs(assetPath)) Expected(assetImporter as TextureImporter, assetPath, true);
    }

    private void OnPreprocessAudio()
    {
        if (Governs(assetPath)) Expected(assetImporter as AudioImporter, assetPath, true);
    }

    // ---------------------------------------------------------------- textures

    // Cut-out colour maps (Standard Cutout) by pack/path inside the pack, with their alpha
    // cutoff: the mipmaps keep the coverage at that cutoff, so grilles, fire escapes, leaves,
    // hair cards and stickers do not thin out with distance.
    private static readonly Dictionary<string, float> Cutoffs = new Dictionary<string, float>
    {
        { "StreetModels/AirconUnit_AirconGrille_Color.png", 0.5f },
        { "StreetModels/DT_MaskedDetails_Color.png", 0.5f },
        { "InteriorModels/PottedPlantTall_Leaves_Color.png", 0.5f },
        { "PeopleModels/DetectiveFemale_Hair_Color.png", 0.4f },
        { "WallGraphics/Atlas/NightStoreInterior_Color.png", 0.5f },
        { "WallGraphics/Atlas/NightStoreStreet_Color.png", 0.3f },
    };

    // Alpha-blended overlays outside the decal and sign folders (Standard Fade). The pack
    // READMEs also give Fade for the glass-crack decal and the moon billboard.
    private static readonly HashSet<string> Overlays = new HashSet<string>
    {
        "PBRInterior/GlassSmudge_ColorAlpha.png", "PBRExterior/LeakStain_ColorAlpha.png", "ForensicModels/EvidenceBagPlastic_Color.png",
        "ForensicModels/GlassCrackDecal_ColorAlpha.png", "NightSky/moon_disc_512.png",
    };

    private enum Map { Colour, Normal, Data, Occlusion, Mask }

    // The path inside its pack (Atlas/NightStoreInterior_Color.png, FloorTile_Normal.png).
    private static string Inside(string path, string pack) => path.Substring(DemoAssetLibrary.Root.Length + pack.Length + 2);

    // The alpha cutoff a cut-out colour map imports with (its mipmaps keep the coverage at
    // that value), or 0: a material that alpha-tests the map should use the same cutoff.
    public static float CutoffOf(string path)
    {
        string pack = PackOf(path);
        return pack != null && Cutoffs.TryGetValue(pack + "/" + Inside(path, pack), out float cutoff) ? cutoff : 0;
    }

    // True when the importer already follows the rules; with write it also takes them (the
    // caller saves and reimports). Files of the packs without a rule match trivially.
    public static bool Expected(TextureImporter t, string path, bool write)
    {
        string pack = PackOf(path);
        if (t == null || pack == null) return true;
        string sub = Inside(path, pack);
        string file = Path.GetFileName(path), stem = Path.GetFileNameWithoutExtension(path);
        bool ok = true;
        void Want<T>(T current, T wanted, Action<T> set)
        {
            if (EqualityComparer<T>.Default.Equals(current, wanted)) return;
            ok = false;
            if (write) set(wanted);
        }

        // Type, first match wins. The type is set before everything else, so nothing it
        // implies can overwrite the settings below.
        bool cookie = pack == "NightSky" && Starts(stem, "cookie_") || pack == "SurfaceDecals" && Starts(stem, "Cookie_");
        bool sky = pack == "NightSky" && Path.GetExtension(path).Equals(".hdr", StringComparison.OrdinalIgnoreCase);
        bool sprite = pack == "NightSky" && (Starts(stem, "glow_") || Starts(stem, "flare_") || Starts(stem, "lens_dirt_") || Starts(stem, "moon_disc_"));
        Map map = Map.Colour;
        if (cookie)
        {
            // Spot-light cookies (a 2D cookie is a spotlight's; the light type is left alone).
            Want(t.textureType, TextureImporterType.Cookie, x => t.textureType = x);
            Want(t.textureShape, TextureImporterShape.Texture2D, x => t.textureShape = x);
            Want(t.borderMipmap, true, x => t.borderMipmap = x);
            Want(t.mipmapEnabled, true, x => t.mipmapEnabled = x);
            Want(t.alphaSource, TextureImporterAlphaSource.FromGrayScale, x => t.alphaSource = x);
        }
        else if (sky)
        {
            // Skybox/Panoramic: no mipmaps, which would draw a seam line where u wraps.
            Want(t.textureType, TextureImporterType.Default, x => t.textureType = x);
            Want(t.textureShape, TextureImporterShape.Texture2D, x => t.textureShape = x);
            Want(t.mipmapEnabled, false, x => t.mipmapEnabled = x);
        }
        else if (stem.EndsWith("_Normal", StringComparison.Ordinal))
        {
            map = Map.Normal;
            Want(t.textureType, TextureImporterType.NormalMap, x => t.textureType = x);
        }
        else if (stem.EndsWith("_MetallicSmoothness", StringComparison.Ordinal))
        {
            map = Map.Data;
            Want(t.textureType, TextureImporterType.Default, x => t.textureType = x);
            Want(t.sRGBTexture, false, x => t.sRGBTexture = x);
            Want(t.alphaSource, TextureImporterAlphaSource.FromInput, x => t.alphaSource = x);
        }
        else if ((stem.EndsWith("_AO", StringComparison.Ordinal) || stem.EndsWith("_Occlusion", StringComparison.Ordinal) || stem.EndsWith("_PaintMask", StringComparison.Ordinal)) || pack == "PBRExterior" && file == "PuddleMask.png"
            || pack == "SurfaceDecals" && file == "Caustics_Tile.png")
        {
            map = stem.EndsWith("_AO", StringComparison.Ordinal) || stem.EndsWith("_Occlusion", StringComparison.Ordinal) ? Map.Occlusion : Map.Mask;
            Want(t.textureType, TextureImporterType.Default, x => t.textureType = x);
            Want(t.sRGBTexture, false, x => t.sRGBTexture = x);
        }
        else
        {
            // Colour maps. The night sky's glows, flares and lens dirt are additive (no alpha).
            Want(t.textureType, TextureImporterType.Default, x => t.textureType = x);
            Want(t.sRGBTexture, true, x => t.sRGBTexture = x);
            bool additive = sprite && !Starts(stem, "moon_disc_");
            Want(t.alphaSource, additive ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput, x => t.alphaSource = x);
            bool cutout = Cutoffs.TryGetValue(pack + "/" + sub, out float cutoff);
            Want(t.alphaIsTransparency, cutout || Blended(pack, sub, file), x => t.alphaIsTransparency = x);
            Want(t.mipMapsPreserveCoverage, cutout, x => t.mipMapsPreserveCoverage = x);
            if (cutout) Want(t.alphaTestReferenceValue, cutoff, x => t.alphaTestReferenceValue = x);
        }

        // Wrap: signs, decals, cookies and sprites clamp (the gilt moulding repeats along its
        // length); the skies repeat around and clamp at the poles; tiling sets repeat.
        TextureWrapMode wrapU = TextureWrapMode.Repeat, wrapV = TextureWrapMode.Repeat;
        if (cookie || sprite) wrapU = wrapV = TextureWrapMode.Clamp;
        else if (sky) wrapV = TextureWrapMode.Clamp;
        else if (pack == "WallGraphics")
        {
            wrapU = Starts(stem, "FrameMoulding_Gilt_") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            wrapV = TextureWrapMode.Clamp;
        }
        else if (pack == "CutoutDecals" || pack == "RealismTextures" && Starts(sub,"Decals/") || pack == "SurfaceDecals" && file != "Caustics_Tile.png") wrapU = wrapV = TextureWrapMode.Clamp;
        Want(t.wrapModeU, wrapU, x => t.wrapModeU = x);
        Want(t.wrapModeV, wrapV, x => t.wrapModeV = x);

        // The A4 sheets, plan, calendar and paintings keep their exact size (every side is a
        // multiple of 4, so block compression still applies); ToNearest would cut 25-30 %.
        // Four Artwork paintings (Heda, Pissarro, Bellotto, Fantin-Latour) halve at the 1024
        // cap to a side that is not, so they may import uncompressed: only atlas sources, the
        // night store places them from Atlas/NightStoreInterior_Color.png.
        if (pack == "WallGraphics") Want(t.npotScale, TextureImporterNPOTScale.None, x => t.npotScale = x);
        Want(t.anisoLevel, Grazing(pack, stem) ? 8 : 4, x => t.anisoLevel = x);

        // BC7-class for small text and the skies, BC1/BC3 (BC5 for normals) elsewhere; the
        // cookies stay uncompressed (their gradients band).
        TextureImporterCompression compression = cookie ? TextureImporterCompression.Uncompressed
            : sky || pack == "WallGraphics" && (Starts(sub, "Store/") || sub == "Atlas/NightStoreInterior_Color.png")
                ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Compressed;
        Want(t.textureCompression, compression, x => t.textureCompression = x);
        int max = cookie ? 512 : sky ? SkySize(stem) : MaxSize(pack, sub, stem, map);
        Want(t.maxTextureSize, max, x => t.maxTextureSize = x);
        return ok;
    }

    private static bool Starts(string text, string prefix) => text.StartsWith(prefix, StringComparison.Ordinal);

    private static bool In(string value, params string[] names) => Array.IndexOf(names, value) >= 0;

    // The material a map belongs to: its stem without the map suffix (FloorTile_Normal ->
    // FloorTile, GlassSmudge_ColorAlpha -> GlassSmudge, AirconUnit_AirconGrille_Color -> AirconUnit_AirconGrille).
    private static string MaterialOf(string stem)
    {
        int cut = stem.LastIndexOf('_');
        return cut > 0 ? stem.Substring(0, cut) : stem;
    }

    // Alpha-blended colour maps (Standard Fade): every decal colour map, the PNGs of the
    // store, street and brand signs, and the overlays above. Colour bleeds into the clear
    // texels so the mipmaps get no dark fringe.
    private static bool Blended(string pack, string sub, string file)
    {
        if (!file.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return false;
        if (pack == "SurfaceDecals" || pack == "CutoutDecals" || pack == "RealismTextures" && Starts(sub,"Decals/")) return true;
        if (pack == "WallGraphics") return Starts(sub, "Store/") || Starts(sub, "Street/") || Starts(sub, "Brand/");
        return Overlays.Contains(pack + "/" + sub);
    }

    // Anisotropic 8 where surfaces are seen at grazing angles in VR (floors, the street, wall
    // text and decals), 4 elsewhere.
    private static bool Grazing(string pack, string stem)
    {
        if (pack == "WallGraphics" || pack == "SurfaceDecals") return true;
        if (pack == "PBRInterior") return Starts(stem, "FloorTile") || Starts(stem, "Terrazzo");
        if (pack == "PBRExterior") return Starts(stem, "Asphalt") || Starts(stem, "Pavement") || Starts(stem, "Concrete") || Starts(stem, "Granite");
        return false;
    }

    // The default overcast sky and the clear alternative; the four photographic HDRIs are unused.
    private static int SkySize(string stem) => stem == "city_night_sky_overcast_1k" ? 1024 : stem == "city_night_sky_clear_2k" ? 2048 : 256;

    // Max Size per pack and map (colour / normal / metallic-smoothness / AO): 2048 only for
    // the hero floors, counters and street, the wall-text pages and the decal atlases. Packs
    // without AO maps would give AO the metallic-smoothness size.
    private static int MaxSize(string pack, string sub, string stem, Map map)
    {
        string material = MaterialOf(stem);
        switch (pack)
        {
            case "PBRInterior":
                if (map == Map.Normal) return material == "FloorTile" ? 2048 : In(material, "VelvetCharcoal", "SuedeCream", "Leather", "Carpet") ? 512 : 1024;
                if (map == Map.Data) return 1024;
                if (map == Map.Occlusion) return 512;
                return In(material, "FloorTile", "StoneTop", "StoneTopDark", "Walnut", "PlasterWarm", "Terrazzo") ? 2048 : 1024;
            case "PBRExterior":
                if (map == Map.Occlusion) return 512;
                if (map != Map.Colour) return 1024;
                return In(material, "AsphaltWet", "AsphaltDry", "PavementSlab", "PavementSlabWet", "Concrete", "BrickFacade") ? 2048 : 1024;
            case "JewelryModels":
                return map == Map.Colour && (In(material, "PriceTags", "GemPaper", "PaymentTerminal") || stem == "PosTablet_Screen") ? 1024 : 512;
            case "ForensicModels":
                return map == Map.Colour || map == Map.Mask ? 1024 : map == Map.Normal ? 512 : 256;
            case "StreetModels":
                return Starts(stem, "Proc_") ? 512 : 1024;
            case "InteriorModels":
                return map == Map.Colour || map == Map.Mask ? (Starts(stem, "Shared_") ? 512 : 1024) : 512;
            case "PeopleModels":
                return map == Map.Colour || map == Map.Mask ? (Starts(stem, "PeopleProps_") ? 512 : 1024) : map == Map.Normal ? 512 : 256;
            case "WallGraphics":
                return Starts(sub, "Store/") || Starts(sub, "Atlas/") ? 2048 : 1024;
            case "SurfaceDecals":
                if (Starts(sub, "Atlas/")) return map == Map.Data ? 1024 : 2048;
                return 1024;
            default:
                // NightSky sprites and moon map (the cookies and skies have their own sizes).
                return 1024;
        }
    }

    // ---------------------------------------------------------------- audio

    // The same contract for audio. Loops and long one-shots stay compressed in memory, short
    // one-shots decompress on load, the music beds stream. Preload settings are never touched.
    public static bool Expected(AudioImporter a, string path, bool write)
    {
        string pack = PackOf(path);
        if (a == null || pack == null) return true;
        string file = Path.GetFileName(path), stem = Path.GetFileNameWithoutExtension(path);
        AudioImporterSampleSettings s = a.defaultSampleSettings;
        bool ok = true;
        void Want<T>(T current, T wanted, Action<T> set)
        {
            if (EqualityComparer<T>.Default.Equals(current, wanted)) return;
            ok = false;
            if (write) set(wanted);
        }

        if (pack == "NightSoundscape" || pack == "RealFoley" || pack == "RealFoley2")
        {
            if (stem.EndsWith("_Loop", StringComparison.Ordinal))
            {
                Want(s.loadType, AudioClipLoadType.CompressedInMemory, x => s.loadType = x);
                Want(s.compressionFormat, AudioCompressionFormat.Vorbis, x => s.compressionFormat = x);
                Want(s.quality, 0.6f, x => s.quality = x);
                Want(s.sampleRateSetting, AudioSampleRateSetting.PreserveSampleRate, x => s.sampleRateSetting = x);
                Want(a.loadInBackground, true, x => a.loadInBackground = x);
                // The one stereo loop played as a 3D source (at the forced doors); the other
                // stereo beds are 2D and keep their width.
                Want(a.forceToMono, file == "WindWhistleGap_Loop.ogg", x => a.forceToMono = x);
            }
            else if (Starts(stem, "Siren") || Starts(stem, "CarPass") || stem == "DogBarkDistant_03")
            {
                // 5-26 s one-shots.
                Want(s.loadType, AudioClipLoadType.CompressedInMemory, x => s.loadType = x);
                Want(s.compressionFormat, AudioCompressionFormat.Vorbis, x => s.compressionFormat = x);
                Want(s.quality, 0.6f, x => s.quality = x);
            }
            else if (file.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                Want(s.loadType, AudioClipLoadType.DecompressOnLoad, x => s.loadType = x);
                Want(s.compressionFormat, AudioCompressionFormat.PCM, x => s.compressionFormat = x);
            }
        }
        else if (pack == "Music")
        {
            if (stem == "MenuTheme" || Starts(stem, "InvestigationLoop"))
            {
                Want(s.loadType, AudioClipLoadType.Streaming, x => s.loadType = x);
                Want(s.compressionFormat, AudioCompressionFormat.Vorbis, x => s.compressionFormat = x);
                Want(s.quality, 0.7f, x => s.quality = x);
                Want(a.loadInBackground, true, x => a.loadInBackground = x);
            }
            else if (stem == "ResetSting")
            {
                Want(s.loadType, AudioClipLoadType.DecompressOnLoad, x => s.loadType = x);
                Want(s.compressionFormat, AudioCompressionFormat.Vorbis, x => s.compressionFormat = x);
                Want(s.quality, 0.7f, x => s.quality = x);
            }
        }
        if (write && !s.Equals(a.defaultSampleSettings)) a.defaultSampleSettings = s;
        return ok;
    }

    // ---------------------------------------------------------------- catch-up

    // Safety net for projects that imported the packs before these rules existed: reimports
    // every pack texture and audio clip whose importer does not follow them, in one batch;
    // returns how many. Cheap when everything is current (ThirdPartyModelImport.GenerateLightmapUVs
    // runs it before each build of the night store).
    public static int Apply()
    {
        List<string> folders = new List<string>();
        foreach (string pack in Packs)
            if (AssetDatabase.IsValidFolder(DemoAssetLibrary.Root + "/" + pack)) folders.Add(DemoAssetLibrary.Root + "/" + pack);
        if (folders.Count == 0) return 0;
        HashSet<string> paths = new HashSet<string>();
        foreach (string filter in new[] { "t:Texture", "t:AudioClip" })
            foreach (string guid in AssetDatabase.FindAssets(filter, folders.ToArray()))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        int count = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string path in paths)
            {
                AssetImporter importer = AssetImporter.GetAtPath(path);
                TextureImporter texture = importer as TextureImporter;
                AudioImporter audio = importer as AudioImporter;
                if (texture != null ? Expected(texture, path, false) : audio == null || Expected(audio, path, false)) continue;
                if (texture != null) Expected(texture, path, true);
                else Expected(audio, path, true);
                importer.SaveAndReimport();
                count++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        return count;
    }
}
#endif
