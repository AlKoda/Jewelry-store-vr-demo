#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Third-party models under Assets/CrimeSceneDemo/ThirdParty. Generators ask
// for a model by pack and name and get a placed instance, or null when the
// files are absent, so every generator keeps its primitive fallback.
// PlacePack is the realism packs' path: Standard materials built once per colour
// map, tint and role (PackMaterial), LOD levels from the packs' _LOD1/_LOD2 files
// and the CC BY / MIT credits of whatever was placed (Credit, WriteCredits).
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

    // A texture by stem whichever format the pack ships: PNG first (the cut-out, RGBA and
    // generated maps), then JPG (the photographic colour and normal maps); null when neither exists.
    public static Texture2D TextureAny(string pack, string stem)
    {
        Texture2D png = Texture(pack, stem + ".png");
        return png != null ? png : Texture(pack, stem + ".jpg");
    }

    // A normal map from a pack, re-imported as a normal map the first time so the
    // Standard shader unpacks it correctly; null when the file is absent.
    public static Texture2D NormalMap(string pack, string file) => NormalMapAt(Path(pack, file));

    // NormalMap by stem, in TextureAny's order: PNG first, then JPG.
    public static Texture2D NormalMapAny(string pack, string stem)
    {
        Texture2D png = NormalMap(pack, stem + ".png");
        return png != null ? png : NormalMap(pack, stem + ".jpg");
    }

    // Files the pack import rules govern already carry their settings; these loaders
    // then only load them, so a loader and the rules never fight over a setting
    // (each change would reimport the file).
    public static Texture2D NormalMapAt(string path)
    {
        if (PackImport.Governs(path)) return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        if (importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // A linear data texture from a pack (packed metallic/smoothness, roughness): imported
    // with sRGB off the first time; null when the file is absent.
    public static Texture2D DataMap(string pack, string file)
    {
        string path = Path(pack, file);
        if (PackImport.Governs(path)) return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        if (importer.textureType != TextureImporterType.Default || importer.sRGBTexture)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // A sign or label image (sRGB, mipmapped, clamped so the edges do not bleed);
    // null when the file is absent.
    public static Texture2D SignTexture(string pack, string file)
    {
        string path = Path(pack, file);
        if (PackImport.Governs(path)) return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        if (importer.textureType != TextureImporterType.Default || !importer.sRGBTexture || !importer.mipmapEnabled
            || importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // An audio clip from a pack as an asset, so a scene component can reference it and
    // it ships in a player build (outside the governed packs uncompressed PCM, decompressed
    // on load: these are a few short mono clips; the soundscape and music keep the import
    // rules' load types); null when the file is absent. A loaded clip records its
    // credit (Credit), so an attributable recording is credited wherever it is used.
    public static AudioClip Audio(string pack, string file)
    {
        string path = Path(pack, file);
        AudioClip clip;
        if (PackImport.Governs(path)) clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        else
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return null;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.compressionFormat != AudioCompressionFormat.PCM || settings.loadType != AudioClipLoadType.DecompressOnLoad)
            {
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        if (clip != null) Credit(pack, System.IO.Path.GetFileNameWithoutExtension(file));
        return clip;
    }

    // Built-in Standard metallic workflow with a packed map (R metallic, A smoothness):
    // smoothness from the metallic alpha at multiplier 1. null clears the map, so a
    // regenerated material falls back to its scalar metallic and smoothness.
    public static void ApplyMetallicSmoothness(Material m, Texture2D packed)
    {
        m.SetTexture("_MetallicGlossMap", packed);
        if (packed == null)
        {
            m.DisableKeyword("_METALLICGLOSSMAP");
            return;
        }
        m.EnableKeyword("_METALLICGLOSSMAP");
        m.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
        m.SetFloat("_SmoothnessTextureChannel", 0);
        m.SetFloat("_GlossMapScale", 1);
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
        Credit(pack, model);
        return instance;
    }

    // Lowers or raises the prop until its lowest renderer point rests on surface (world y).
    public static void Seat(GameObject prop, float surface)
    {
        Bounds bounds = new Bounds();
        bool first = true;
        foreach (Renderer r in prop.GetComponentsInChildren<Renderer>())
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }
        if (!first) prop.transform.position += Vector3.up * (surface - bounds.min.y);
    }

    // ---------------------------------------------------------------- pack materials

    // Saved Standard materials for the realism packs. Every OBJ import carries its own
    // read-only copies of its MTL's materials; PackMaterial maps each one to a saved
    // material shared by every instance and LOD level (and, without a finish entry, by
    // every model of the pack) with the same colour map, tint and role, and wires the
    // pack's map convention by the colour map's stem:
    // <stem>_Color -> <stem>_Normal (.png or .jpg) and <stem>_MetallicSmoothness.png.
    public const string PackMaterialFolder = "Assets/CrimeSceneDemo/PackMaterials";

    // What a material's maps cannot say: map-less metallic/smoothness, emission, glass and
    // alpha test. Emission and Cutoff above 0 switch those roles on; Glass above 0 marks a
    // glass material and is its alpha (the MTL d) when the import's colour has none.
    public struct Finish
    {
        public float Metallic, Smoothness, Emission, Cutoff, Glass;
        // The colour map doubles as the emission map (lit windows, screens, the lightbox).
        public bool EmissionFromMap;
        // Emission tint; left black it means white.
        public Color EmissionColor;
        // None keeps small emitters out of the bake; BakedEmissive lets large ones light it.
        public MaterialGlobalIlluminationFlags GI;
    }

    private static Finish Surface(float metallic, float smoothness) => new Finish { Metallic = metallic, Smoothness = smoothness };
    private static Finish GlassFinish(float alpha) => new Finish { Smoothness = 0.95f, Glass = alpha };
    private static Finish MapGlow(float smoothness, float emission, MaterialGlobalIlluminationFlags gi)
        => new Finish { Smoothness = smoothness, Emission = emission, EmissionFromMap = true, GI = gi };

    // Keyed pack/model/material, never by a material name alone: the packs reuse names
    // (Interior, Glass, Props) and colour maps (Shared_PlasticBlack, PeopleProps) for
    // different surfaces. Values from StreetModels sources.json (models.<Name>.obj.materials),
    // InteriorModels models.json, JewelryModels materials.json and the pack READMEs;
    // the emission intensities are the night look's choice within the READMEs' ranges.
    private static readonly Dictionary<string, Finish> Finishes = new Dictionary<string, Finish>
    {
        ["StreetModels/PoliceCar/Body"] = Surface(0, 0.891f),
        ["StreetModels/PoliceCar/Lights"] = Surface(0.036f, 0.893f),
        ["StreetModels/PoliceCar/Plate"] = Surface(0, 0.45f),
        ["StreetModels/PoliceCar/Glass"] = GlassFinish(0.35f),
        ["StreetModels/StreetLamp/LampGlass"] = GlassFinish(0.35f),
        ["StreetModels/StreetLamp/LampBulb"] = new Finish { Smoothness = 0.5f, Emission = 3, EmissionColor = new Color(1, 0.85f, 0.6f) },
        ["StreetModels/Bollard/Signs"] = Surface(0, 0.35f),
        ["StreetModels/BusShelter/Signs"] = Surface(0, 0.35f),
        ["StreetModels/BusShelter/Lightbox"] = MapGlow(0.6f, 1, MaterialGlobalIlluminationFlags.BakedEmissive),
        ["StreetModels/BusShelter/Glass"] = GlassFinish(0.22f),
        ["StreetModels/StormDrainGrate/Void"] = Surface(0, 0),
        ["StreetModels/ApartmentBlock/Glass"] = GlassFinish(0.35f),
        ["StreetModels/ApartmentBlock/Interior"] = MapGlow(0.1f, 0.8f, MaterialGlobalIlluminationFlags.BakedEmissive),
        ["InteriorModels/ComputerMonitor/Housing"] = Surface(0, 0.42f),
        ["InteriorModels/ComputerMonitor/Screen"] = MapGlow(0.9f, 0.45f, MaterialGlobalIlluminationFlags.None),
        ["InteriorModels/Keyboard/Keyboard"] = Surface(0, 0.35f),
        ["InteriorModels/Keyboard/Rubber"] = Surface(0, 0.25f),
        ["InteriorModels/CeilingDiffuser/Diffuser"] = Surface(0, 0.58f),
        ["InteriorModels/CeilingDiffuser/Duct"] = Surface(0, 0.1f),
        ["InteriorModels/SnakePlant/Pot"] = Surface(0, 0.25f),
        ["InteriorModels/SnakePlant/Soil"] = Surface(0, 0.05f),
        ["InteriorModels/SnakePlant/Leaf"] = Surface(0, 0.45f),
        ["InteriorModels/Rug/Rug"] = Surface(0, 0.05f),
        ["InteriorModels/FireExtinguisher/GaugeGlass"] = GlassFinish(0.25f),
        ["InteriorModels/WallClock/Glass"] = GlassFinish(0.18f),
        ["PeopleModels/PasserBy_Phone/Props"] = Surface(0, 0.4f),
        ["PeopleModels/PasserBy_Phone/PropScreen"] = MapGlow(0.9f, 0.5f, MaterialGlobalIlluminationFlags.None),
        ["ForensicModels/NitrileGloveBox/GloveBoxCard"] = Surface(0, 0.3f),
        ["ForensicModels/NitrileGloveBox/GloveNitrile"] = Surface(0, 0.45f),
        ["JewelryModels/GoldCoin/GoldCoin"] = Surface(1, 0.85f),
        ["JewelryModels/GoldBar1kg/GoldBar1kg"] = Surface(1, 0.78f),
        ["JewelryModels/GemParcel/GemPaper"] = Surface(0, 0.2f),
    };

    // Material roles, the last part of a pack material's name.
    private const char GlassRole = 'G', CutoutRole = 'C', EmissiveRole = 'E', OpaqueRole = 'O';

    private static readonly Dictionary<string, Material> packMaterials = new Dictionary<string, Material>();
    // Asset paths built (or rebuilt from an earlier session's asset) since the editor domain loaded.
    private static readonly HashSet<string> builtMaterials = new HashSet<string>();

    // The shared pack material for an OBJ's imported material, saved as
    // PackMaterialFolder/<pack>/<key>.mat. model is the base model (LOD suffixes are
    // stripped). Roles: Glass when the MTL d came through as alpha below 0.99 or the
    // finish says glass (never decided by a name), Cutout for a cut-out colour map (the
    // import rules' table, PackImport.CutoffOf, so the material alpha-tests at the cutoff
    // its mipmaps keep coverage for) or a finish cutoff, Emissive for a finish emission,
    // otherwise Opaque. A material with a finish is keyed model_material_role; any other
    // by colour map (or material name without one), RGBA tint and role, so a shared map
    // tinted two ways stays two materials.
    public static Material PackMaterial(string pack, string model, Material imported)
    {
        if (imported == null) return null;
        model = BaseModel(model);
        bool finished = Finishes.TryGetValue(pack + "/" + model + "/" + imported.name, out Finish finish);
        Texture albedo = imported.mainTexture;
        string albedoName = albedo != null ? albedo.name : null;
        float mask = albedo != null ? PackImport.CutoffOf(AssetDatabase.GetAssetPath(albedo)) : 0;
        float cutoff = mask > 0 ? mask : finish.Cutoff;
        char role = imported.color.a < 0.99f || finish.Glass > 0 ? GlassRole : cutoff > 0 ? CutoutRole : finish.Emission > 0 ? EmissiveRole : OpaqueRole;
        string key = finished ? model + "_" + imported.name + "_" + role
            : (albedoName ?? imported.name) + "_" + ColorUtility.ToHtmlStringRGBA(imported.color) + "_" + role;
        string path = PackMaterialFolder + "/" + pack + "/" + key + ".mat";
        if (packMaterials.TryGetValue(path, out Material cached) && cached != null) return cached;
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            EnsureFolders(PackMaterialFolder + "/" + pack);
            m = new Material(Shader.Find("Standard")) { name = key };
            AssetDatabase.CreateAsset(m, path);
            builtMaterials.Remove(path);
        }
        // Built once per domain, so a regenerated scene picks up changes to the tables.
        if (builtMaterials.Add(path)) BuildPackMaterial(m, pack, imported, finished, finish, role, cutoff);
        packMaterials[path] = m;
        return m;
    }

    private static void BuildPackMaterial(Material m, string pack, Material imported, bool finished, Finish finish, char role, float cutoff)
    {
        Texture albedo = imported.mainTexture;
        Color color = imported.color;
        color.a = role != GlassRole ? 1 : color.a < 0.99f ? color.a : finish.Glass;
        m.color = color;
        m.mainTexture = albedo;
        string stem = albedo == null ? null
            : albedo.name.EndsWith("_Color", System.StringComparison.Ordinal) ? albedo.name.Substring(0, albedo.name.Length - "_Color".Length) : albedo.name;
        Texture2D normal = stem != null ? NormalMapAny(pack, stem + "_Normal") : null;
        m.SetTexture("_BumpMap", normal);
        if (normal != null)
        {
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_BumpScale", 1);
        }
        else m.DisableKeyword("_NORMALMAP");
        // Glass takes scalars only; the others read the packed map when the pack ships one.
        ApplyMetallicSmoothness(m, stem != null && role != GlassRole ? DataMap(pack, stem + "_MetallicSmoothness.png") : null);
        Vector2 scalars = finished ? new Vector2(finish.Metallic, finish.Smoothness) : NamedSurface(imported.name);
        m.SetFloat("_Metallic", scalars.x);
        m.SetFloat("_Glossiness", scalars.y);
        if (role == GlassRole)
        {
            DemoGeometry.Translucent(m);
            m.SetFloat("_Metallic", 0);
            m.SetFloat("_Glossiness", finished && finish.Smoothness > 0 ? finish.Smoothness : 0.95f);
        }
        else if (role == CutoutRole) DemoGeometry.Cutout(m, cutoff);
        else Opaque(m);
        if (role == EmissiveRole)
        {
            Color tint = finish.EmissionColor.maxColorComponent > 0 ? finish.EmissionColor : Color.white;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(tint.r * finish.Emission, tint.g * finish.Emission, tint.b * finish.Emission, 1));
            m.SetTexture("_EmissionMap", finish.EmissionFromMap ? albedo : null);
            m.globalIlluminationFlags = finish.GI;
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            m.SetTexture("_EmissionMap", null);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
        EditorUtility.SetDirty(m);
    }

    // Last resort for a material with neither a packed map nor a finish: metallic and
    // smoothness guessed from its name (gold and pearl as JewelryModels materials.json
    // gives its shared untextured materials).
    private static Vector2 NamedSurface(string material)
    {
        string n = material.ToLowerInvariant();
        if (n.Contains("chrome")) return new Vector2(1, 0.92f);
        if (n.Contains("gold") || n.Contains("platinum")) return new Vector2(1, 0.9f);
        if (n.Contains("steel") || n.Contains("metal") || n.Contains("aluminium") || n.Contains("handle")) return new Vector2(1, 0.6f);
        if (n.Contains("pearl")) return new Vector2(0.1f, 0.8f);
        if (n.Contains("rubber") || n.Contains("tyre")) return new Vector2(0, 0.25f);
        if (n.Contains("plastic") || n.Contains("housing")) return new Vector2(0, 0.45f);
        if (n.Contains("void") || n.Contains("recess") || n.Contains("duct")) return new Vector2(0, 0.1f);
        return new Vector2(0, 0.3f);
    }

    // Standard Opaque mode, so a rebuilt asset never keeps an earlier blend state.
    private static void Opaque(Material m)
    {
        m.SetFloat("_Mode", 0);
        m.SetInt("_SrcBlend", (int)BlendMode.One);
        m.SetInt("_DstBlend", (int)BlendMode.Zero);
        m.SetInt("_ZWrite", 1);
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.SetOverrideTag("RenderType", "");
        m.renderQueue = -1;
    }

    // A LOD file's base model: StreetLamp_LOD1 -> StreetLamp.
    private static string BaseModel(string model)
    {
        bool lod = model.EndsWith("_LOD1", System.StringComparison.Ordinal) || model.EndsWith("_LOD2", System.StringComparison.Ordinal);
        return lod ? model.Substring(0, model.Length - "_LOD1".Length) : model;
    }

    // Every missing folder along an asset path (DemoGeometry.EnsureFolder makes one level).
    private static void EnsureFolders(string path)
    {
        string[] parts = path.Split('/');
        string folder = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            folder += "/" + parts[i];
            DemoGeometry.EnsureFolder(folder);
        }
    }

    // ---------------------------------------------------------------- pack placement

    // LODGroup size classes. Tiny: jewellery and coins (their LOD1 is the top level in the
    // night store); Prop: hand-held to waist-high props; Large: cars, cabinets, figures;
    // Post: the 3.87 m lamp posts, which a viewer passes within a few metres.
    public enum LodClass { None, Tiny, Prop, Large, Post }

    // Screen-relative transition heights (LOD Bias 1) for a class: three levels give
    // {first switch, second switch, cull}, two give {first switch, cull}, one {cull}.
    public static float[] Transitions(LodClass c, int levels)
    {
        float first, second, cull;
        switch (c)
        {
            case LodClass.Tiny: first = 0.02f; second = 0.006f; cull = 0.001f; break;
            case LodClass.Prop: first = 0.10f; second = 0.03f; cull = 0.004f; break;
            case LodClass.Large: first = 0.30f; second = 0.10f; cull = 0.004f; break;
            case LodClass.Post: first = 0.60f; second = 0.20f; cull = 0.005f; break;
            default: return new float[0];
        }
        if (levels >= 3) return new[] { first, second, cull };
        return levels == 2 ? new[] { first, cull } : levels == 1 ? new[] { cull } : new float[0];
    }

    public struct PackOptions
    {
        // Uniform scale of the root; 0 means 1.
        public float Scale;
        // Turns each level inside the root, for a model authored facing another axis.
        public Vector3 ModelEuler;
        // Every level receives shadows; this makes it cast them too.
        public bool Shadows;
        public LodClass Lod;
        // Replaces a material by its imported name (a scene palette material); null or a
        // null result keeps PackMaterial.
        public System.Func<string, Material> Remap;
    }

    // A realism-pack model as root <model> (at the position and yaw, scaled) holding one
    // unpacked level per LOD file, LOD0, LOD1, ...; null when the model is absent, so the
    // caller keeps its fallback. Levels: Lod None -> the model alone; Tiny -> its _LOD1 and
    // _LOD2 files when _LOD1 exists (else the model); the other classes -> the model, _LOD1
    // and _LOD2, whichever files exist. Two or more levels get an LODGroup with the class's
    // transitions. Colliders are removed; materials go through Remap, then PackMaterial, so
    // every level and instance shares one material per surface. Probe-lit until the caller
    // marks it static.
    public static GameObject PlacePack(string pack, string model, Transform parent, Vector3 localPosition, float yaw, PackOptions o)
    {
        if (Load(pack, model) == null) return null;
        List<string> levels = new List<string>();
        if (o.Lod == LodClass.None) levels.Add(model);
        else if (o.Lod == LodClass.Tiny && Load(pack, model + "_LOD1") != null)
        {
            levels.Add(model + "_LOD1");
            if (Load(pack, model + "_LOD2") != null) levels.Add(model + "_LOD2");
        }
        else if (o.Lod == LodClass.Tiny) levels.Add(model);
        else
        {
            levels.Add(model);
            foreach (string suffix in new[] { "_LOD1", "_LOD2" })
                if (Load(pack, model + suffix) != null) levels.Add(model + suffix);
        }

        GameObject root = new GameObject(model);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        root.transform.localScale = Vector3.one * (o.Scale == 0 ? 1 : o.Scale);
        float[] heights = Transitions(o.Lod, levels.Count);
        LOD[] lods = new LOD[levels.Count];
        for (int i = 0; i < levels.Count; i++)
        {
            GameObject level = (GameObject)PrefabUtility.InstantiatePrefab(Load(pack, levels[i]), root.transform);
            PrefabUtility.UnpackPrefabInstance(level, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            level.name = "LOD" + i;
            level.transform.localPosition = Vector3.zero;
            level.transform.localRotation = Quaternion.Euler(o.ModelEuler);
            foreach (Collider collider in level.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            MeshRenderer[] renderers = level.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer r in renderers)
            {
                Material[] materials = r.sharedMaterials;
                bool blended = materials.Length > 0;
                for (int k = 0; k < materials.Length; k++)
                {
                    if (materials[k] == null) { blended = false; continue; }
                    Material remapped = o.Remap != null ? o.Remap(materials[k].name) : null;
                    materials[k] = remapped != null ? remapped : PackMaterial(pack, model, materials[k]);
                    blended &= materials[k].renderQueue >= 3000;
                }
                r.sharedMaterials = materials;
                // Glass and other blended surfaces never cast (the Standard shader would dither them).
                r.shadowCastingMode = o.Shadows && !blended ? ShadowCastingMode.On : ShadowCastingMode.Off;
                r.receiveShadows = true;
                r.lightProbeUsage = LightProbeUsage.BlendProbes;
                r.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            }
            if (i < heights.Length) lods[i] = new LOD(heights[i], renderers);
        }
        if (levels.Count >= 2 && o.Lod != LodClass.None)
        {
            LODGroup group = root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.SetLODs(lods);
            group.RecalculateBounds();
        }
        Credit(pack, model);
        return root;
    }

    // ---------------------------------------------------------------- atlases

    // An atlas page's layout (<Set>.json beside the page, as JsonUtility reads it):
    // pixel rects with y counted from the bottom edge, Unity's UV origin.
    [System.Serializable]
    public class AtlasCell
    {
        public string name;
        public int x, y, w, h;
    }

    [System.Serializable]
    public class AtlasFile
    {
        public int width, height;
        public AtlasCell[] cells;
    }

    // UV rects by cell name for an atlas layout (jsonFile relative to the pack, e.g.
    // Atlas/Ambient.json); null with a warning when it is missing or unreadable.
    public static Dictionary<string, Rect> Atlas(string pack, string jsonFile)
    {
        TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(Path(pack, jsonFile));
        AtlasFile layout = null;
        if (json != null)
        {
            try { layout = JsonUtility.FromJson<AtlasFile>(json.text); }
            catch (System.ArgumentException) { layout = null; }
        }
        if (layout == null || layout.width <= 0 || layout.height <= 0 || layout.cells == null)
        {
            Debug.LogWarning("Atlas layout missing or unreadable: " + Path(pack, jsonFile));
            return null;
        }
        Dictionary<string, Rect> rects = new Dictionary<string, Rect>();
        foreach (AtlasCell cell in layout.cells)
            if (cell != null && !string.IsNullOrEmpty(cell.name))
                rects[cell.name] = new Rect((float)cell.x / layout.width, (float)cell.y / layout.height,
                    (float)cell.w / layout.width, (float)cell.h / layout.height);
        return rects;
    }

    // ---------------------------------------------------------------- credits

    // Attribution lines for what needs credit (CC BY, and the MIT avatars), copied from
    // each pack's LICENSE.txt (the dog barks: the README's credit line), keyed pack/model
    // (LOD suffixes stripped) or pack/* for a whole pack. CC0 assets need none.
    private static readonly Dictionary<string, string> Attributions = BuildAttributions();

    private static Dictionary<string, string> BuildAttributions()
    {
        const string zhabotinsky = "by Daniel Zhabotinsky (https://sketchfab.com/DanielZhabotinsky) licensed under CC-BY-4.0 (http://creativecommons.org/licenses/by/4.0/)";
        const string buildings = "This work is based on \"Downtown Buildings Set - Low Poly model\" (https://sketchfab.com/3d-models/downtown-buildings-set-low-poly-model-7378e7fb9c914c39880d9913a6f4e1d6) "
            + zhabotinsky + ". Changes: Converted the selected building from the set (node transforms baked, metres, Y up); shared 2K atlases resized to 1K JPG and renamed DT_*.";
        const string barks = "\"Dog Barking in Neighborhood\" by Kinoton (https://freesound.org/s/516904/), licensed CC BY 3.0 (https://creativecommons.org/licenses/by/3.0/); trimmed, filtered and normalised.";
        return new Dictionary<string, string>
        {
            ["KhronosSamples/TrafficCone"] = "TrafficCone — © 2024 hinndia. Creative Commons Attribution 4.0 International (CC BY 4.0). https://creativecommons.org/licenses/by/4.0/ — ground plane removed; used as the scene-cone visual.",
            ["KhronosSamples/GlamVelvetSofa"] = "GlamVelvetSofa — © 2021 Wayfair, LLC. CC BY 4.0.",
            ["KhronosSamples/SunglassesKhronos"] = "SunglassesKhronos — © 2024 Darmstadt Graphics Group GmbH. CC BY 4.0.",
            ["KhronosSamples/ChronographWatch"] = "ChronographWatch — © 2025 Darmstadt Graphics Group GmbH. CC BY 4.0. Based on \"Chronograph Watch Mudmaster\" by graphiccompressor (https://skfb.ly/oAsPA), CC BY 4.0. Decimated to about 15k triangles, scaled to metres.",
            ["KhronosSamples/DiffuseTransmissionPlant"] = "DiffuseTransmissionPlant — © 2024 Darmstadt Graphics Group GmbH. CC BY 4.0. Decimated to about 15k triangles.",
            ["KhronosSamples/GlassHurricaneCandleHolder"] = "GlassHurricaneCandleHolder — © 2021 Wayfair, LLC. CC BY 4.0.",
            ["KhronosSamples/IridescenceLamp"] = "IridescenceLamp — © 2022 Wayfair, LLC. CC BY 4.0. Decimated to about 15k triangles.",
            ["KhronosSamples/ChairDamaskPurplegold"] = "ChairDamaskPurplegold — © 2021 Wayfair. CC BY 4.0. Decimated to about 10k triangles.",
            ["StreetModels/PoliceCar"] = "This work is based on \"80 American Police Sedan - Low poly model\" (https://sketchfab.com/3d-models/80-american-police-sedan-low-poly-model-945fd09f513f4d1ab19b6388a7ea7323) "
                + zhabotinsky + ". Changes: trunk badges removed (parody marque atlas); number-plate texture replaced by a plain plate colour; a slogan painted out of the body livery (two places); decimated LOD variants PoliceCar_LOD1/_LOD2.",
            ["StreetModels/Hatchback"] = "This work is based on \"Ace '11 - Low Poly model\" (https://sketchfab.com/3d-models/ace-11-low-poly-model-055ff8a21b8d4d279debca089e2fafcd) "
                + zhabotinsky + ". Changes: badges removed (the badge atlas holds parody marques); plates replaced by plain colour; body paint set to grey metallic.",
            ["StreetModels/SedanClassic"] = "This work is based on \"88 American Sedan - Low poly model\" (https://sketchfab.com/3d-models/88-american-sedan-low-poly-model-8509a7f82665442592627940b7a1cfab) "
                + zhabotinsky + ". Changes: trunk badges removed; plates replaced by plain colour; paint set to dark red.",
            ["StreetModels/BuildingBrickCorner"] = buildings,
            ["StreetModels/BuildingBrickArcade"] = buildings,
            ["StreetModels/BuildingNarrowOffice"] = buildings,
            ["StreetModels/BuildingBrickResidential"] = buildings,
            ["JewelryModels/RingAntiqueTigerEye"] = "\"An Old Ring\" (https://sketchfab.com/3d-models/an-old-ring-ea5e0c11f2e046c993239ea1a3efcad7) by Qifan Zhang (https://sketchfab.com/qifanzhang), licensed under CC BY 4.0. Modified.",
            ["JewelryModels/EarringPearlDrop"] = "\"Earring Jade\" (https://sketchfab.com/3d-models/earring-jade-c91e552728fb4b149d52193fd3e03ac5) by Softmind Game Factory (https://sketchfab.com/softmind), licensed under CC BY 4.0. Modified.",
            ["JewelryModels/EarringGoldLeaf"] = "\"Gold leaf Earring\" (https://sketchfab.com/3d-models/gold-leaf-earring-64c4c78a861d45a28aa1b78873b02d14) by DineshThennarasan (https://sketchfab.com/dineshthennarasan), licensed under CC BY 4.0. Modified.",
            ["PeopleModels/*"] = "Human figures from the Microsoft Rocketbox Avatar Library, Copyright (c) 2020 Microsoft, MIT License (github.com/microsoft/Microsoft-Rocketbox). Posed with one frame of a Rocketbox motion-capture animation and baked to a static mesh.",
            ["NightSoundscape/DogBarkDistant_01"] = barks,
            ["NightSoundscape/DogBarkDistant_02"] = barks,
            ["NightSoundscape/DogBarkDistant_03"] = barks,
        };
    }

    // Attribution keys recorded since the last ResetCredits.
    private static readonly HashSet<string> usedCredits = new HashSet<string>();

    private static string AttributionKey(string pack, string model)
    {
        string key = pack + "/" + BaseModel(model);
        if (Attributions.ContainsKey(key)) return key;
        return Attributions.ContainsKey(pack + "/*") ? pack + "/*" : null;
    }

    // Records that a model or clip was used; only those with an attribution are kept.
    public static void Credit(string pack, string model)
    {
        string key = AttributionKey(pack, model);
        if (key != null) usedCredits.Add(key);
    }

    public static bool NeedsCredit(string pack, string model) => AttributionKey(pack, model) != null;

    // The attribution line, or null when the asset needs none.
    public static string CreditFor(string pack, string model)
    {
        string key = AttributionKey(pack, model);
        return key != null ? Attributions[key] : null;
    }

    // The distinct attribution lines of everything recorded, sorted.
    public static List<string> UsedCredits()
    {
        SortedSet<string> lines = new SortedSet<string>(System.StringComparer.Ordinal);
        foreach (string key in usedCredits) lines.Add(Attributions[key]);
        return new List<string>(lines);
    }

    // Call before building a scene, so its credits list only what it places.
    public static void ResetCredits() => usedCredits.Clear();

    // In Resources, so a player build carries it and the credits screen can load it.
    public const string CreditsAsset = "Assets/CrimeSceneDemo/Resources/DemoCredits.txt";

    // Writes baseLine and one line per used credit to CreditsAsset; the file is only
    // rewritten (and reimported) when the text changed. Returns whether it was written.
    public static bool WriteCredits(string baseLine)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder(baseLine).Append('\n');
        foreach (string line in UsedCredits()) text.Append(line).Append('\n');
        string content = text.ToString();
        if (System.IO.File.Exists(CreditsAsset) && System.IO.File.ReadAllText(CreditsAsset) == content) return false;
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CreditsAsset));
        System.IO.File.WriteAllText(CreditsAsset, content);
        AssetDatabase.ImportAsset(CreditsAsset);
        return true;
    }
}
#endif
