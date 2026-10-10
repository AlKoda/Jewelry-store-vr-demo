#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

// Applies the generated tileable textures (Tools/make_textures.py, committed under
// Prototype/ThirdParty/GeneratedTextures) to the finish materials: stone floor,
// plaster walls, ceiling, marble counter, walnut cabinets, brass trims, asphalt
// and pavement outside. Photo-scanned cgbookcase maps (CC0) give the safe room a
// parquet floor and the jewellery a worn-gold surface when present, and the HDRI
// Haven panoramas (CC0) become the street sky and the reflection environment.
// Tiling is set per renderer from its world size so every surface shows the
// texture at the intended scale. Without the files nothing changes.
public static class ShopTextureFinish
{
    public const string Folder = DemoAssetLibrary.Root + "/GeneratedTextures";
    public const string ScanPack = "CgbookcaseTextures";
    public const string EnvironmentPack = "Environments";
    public const string SkyFile = "venetian_crossroads_2k.hdr", ReflectionFile = "lightroom_14b.hdr";
    public const int ExpectedTextured = 8;
    public static bool HasEnvironment => AssetDatabase.LoadAssetAtPath<Texture>(DemoAssetLibrary.Path(EnvironmentPack, SkyFile)) != null;
    public static bool HasScans => DemoAssetLibrary.HasTexture(ScanPack, "Parquet_flooring_05_Color.png");

    [MenuItem("Crime Scene Demo/Apply Generated Textures")]
    public static void ApplyFromMenu() { Apply(); }

    // Returns the number of materials that received a texture.
    public static int Apply()
    {
        if (!Has("StoneTiles")) { Debug.Log("Generated textures absent; flat finish kept."); return 0; }
        Transform store = GameObject.Find("JewelryStore_Blockout").transform;
        int textured = 0;

        // Floor: the architecture floor boxes carry the tiles; the per-tone tile skins are hidden.
        // The safe room gets its own material: scanned parquet when present, generated carpet otherwise.
        Material stone = Textured("InteriorGenerated", "StoneGrout", "StoneTiles", 0.9f);
        Mat("Assets/CrimeSceneDemo/InteriorGenerated", "SafeRoomFloor", new Color(.22f, .19f, .18f), HasScans ? .35f : .05f);
        Material safeFloor = HasScans
            ? Textured("InteriorGenerated", "SafeRoomFloor", Scan("Parquet_flooring_05_Color"), Scan("Parquet_flooring_05_Normal"), 0.8f)
            : Textured("InteriorGenerated", "SafeRoomFloor", "DarkCarpet", 0.6f);
        if (stone != null)
        {
            textured++;
            Transform tiles = store.Find("InteriorFinish/StoneTiles");
            if (tiles != null) tiles.gameObject.SetActive(false);
            foreach (MeshRenderer r in store.Find("Architecture").GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.name.Contains("Floor")) continue;
                bool safeRoom = r.name.StartsWith("SafeRoom");
                r.sharedMaterial = safeRoom && safeFloor != null ? safeFloor : stone;
                TileByWorldSize(r, safeRoom ? 1.5f : 2f);
            }
            if (safeFloor != null) textured++;
        }
        textured += Retexture(store, "InteriorGenerated", "WarmPlaster", "Plaster", 2.5f, 0.5f, "Architecture");
        textured += Retexture(store, "InteriorGenerated", "CeilingPaint", "Ceiling", 3f, 0.3f, "Architecture");
        textured += Retexture(store, "InteriorGenerated", "CounterStone", "Marble", 1.2f, 0.3f, null);
        textured += Retexture(store, "InteriorGenerated", "SmokedOak", "Walnut", 1f, 0.6f, null);
        textured += Retexture(store, "InteriorGenerated", "DisplayBronze", "BrushedBrass", 0.5f, 0.4f, null);
        textured += Retexture(store, "InteriorGenerated", "SatinBronze", "BrushedBrass", 0.5f, 0.4f, null);
        // Jewellery is centimetres across, so the scanned gold maps once per face, not by world size.
        if (HasScans && Textured("InteriorGenerated", "JewelryGold", Scan("Dirty_gold_01_Color"), Scan("Dirty_gold_01_Normal"), 0.5f) != null) textured++;

        // Street: the city meshes are combined by material, so only shared tiling applies.
        Transform city = GameObject.Find("CityBackdrop")?.transform;
        if (city != null)
        {
            Material asphalt = Textured("CityGenerated", "Asphalt", "Asphalt", 0.8f);
            Material pavement = Textured("CityGenerated", "Pavement", "Pavement", 0.8f);
            if (asphalt != null) { asphalt.SetTextureScale("_MainTex", new Vector2(35, 2)); textured++; }
            if (pavement != null) { pavement.SetTextureScale("_MainTex", new Vector2(70, 2)); textured++; }
        }
        textured += Environment();
        AssetDatabase.SaveAssets();
        Debug.Log("Textures applied to " + textured + " materials.");
        return textured;
    }

    // Street sky from the Venetian crossroads panorama and reflections from the light
    // room, so brass, marble and glass pick up a real environment instead of flat grey.
    // Cameras that cleared to a colour now show the sky through the shopfront.
    private static int Environment()
    {
        if (!HasEnvironment) return 0;
        Texture sky = ImportEnvironment(SkyFile, TextureImporterShape.Texture2D, 2048);
        Texture room = ImportEnvironment(ReflectionFile, TextureImporterShape.TextureCube, 256);
        Material skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/CrimeSceneDemo/CityGenerated/StreetSky.mat");
        if (skybox == null)
        {
            skybox = new Material(Shader.Find("Skybox/Panoramic"));
            AssetDatabase.CreateAsset(skybox, "Assets/CrimeSceneDemo/CityGenerated/StreetSky.mat");
        }
        skybox.SetTexture("_MainTex", sky);
        skybox.SetFloat("_Exposure", 1.1f);
        skybox.SetFloat("_Rotation", 200);
        RenderSettings.skybox = skybox;
        if (room != null)
        {
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = room;
            RenderSettings.reflectionIntensity = 0.7f;
        }
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (camera.targetTexture == null && camera.clearFlags == CameraClearFlags.SolidColor) camera.clearFlags = CameraClearFlags.Skybox;
        return 1;
    }

    private static Texture ImportEnvironment(string file, TextureImporterShape shape, int maxSize)
    {
        string path = DemoAssetLibrary.Path(EnvironmentPack, file);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        if (importer.textureShape != shape || importer.maxTextureSize != maxSize)
        {
            importer.textureShape = shape;
            importer.generateCubemap = shape == TextureImporterShape.TextureCube ? TextureImporterGenerateCubemap.AutoCubemap : TextureImporterGenerateCubemap.None;
            importer.maxTextureSize = maxSize;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture>(path);
    }

    private static string Scan(string name) => DemoAssetLibrary.Path(ScanPack, name + ".png");

    // Texture an existing finish material (if present) and tile every renderer using it.
    private static int Retexture(Transform store, string folder, string material, string texture, float metres, float bump, string under)
    {
        Material m = Textured(folder, material, texture, bump);
        if (m == null) return 0;
        Transform root = under != null ? store.Find(under) : store;
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            if (r.sharedMaterial == m) TileByWorldSize(r, metres);
        return 1;
    }

    // Generated textures: <name>.png with an optional <name>_Normal.png beside it.
    private static Material Textured(string folder, string material, string texture, float bump)
        => Textured(folder, material, Folder + "/" + texture + ".png", Folder + "/" + texture + "_Normal.png", bump);

    // Loads the saved material by name and gives it the albedo and normal textures at the given asset paths.
    private static Material Textured(string folder, string material, string albedoPath, string normalPath, float bump)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>("Assets/CrimeSceneDemo/" + folder + "/" + material + ".mat");
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
        if (m == null || albedo == null) return null;
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if (normal != null) MarkNormalMap(normalPath);
        m.mainTexture = albedo;
        // The texture carries the colour; keep a light tint so the saved colour does not darken it.
        m.color = Color.Lerp(m.color, Color.white, 0.75f);
        if (normal != null)
        {
            m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", bump);
        }
        return m;
    }

    public static bool Has(string texture) => Load(texture) != null;
    private static Texture2D Load(string texture) => AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/" + texture + ".png");

    private static void MarkNormalMap(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.NormalMap) return;
        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }

    // Per-renderer tiling so one material shows the texture at `metres` per repeat on
    // boxes of any size: the two largest world extents of the renderer's bounds count.
    public static void TileByWorldSize(Renderer renderer, float metres)
    {
        Vector3 size = renderer.bounds.size;
        float[] extents = { size.x, size.y, size.z };
        System.Array.Sort(extents);
        Vector2 tiling = new Vector2(Mathf.Max(1, extents[2] / metres), Mathf.Max(1, extents[1] / metres));
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetVector("_MainTex_ST", new Vector4(tiling.x, tiling.y, 0, 0));
        renderer.SetPropertyBlock(block);
    }

    public static void CheckBudget(int textured)
    {
        DemoValidation.Check(textured >= ExpectedTextured, "Generated textures applied to the finish materials (" + textured + ")");
        Material plaster = AssetDatabase.LoadAssetAtPath<Material>("Assets/CrimeSceneDemo/InteriorGenerated/WarmPlaster.mat");
        DemoValidation.Check(plaster != null && plaster.mainTexture != null && plaster.IsKeywordEnabled("_NORMALMAP"), "Walls use albedo and normal maps");
        Transform store = GameObject.Find("JewelryStore_Blockout").transform;
        Renderer floor = store.Find("Architecture/ShowroomFloor").GetComponent<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        floor.GetPropertyBlock(block);
        Vector4 st = block.GetVector("_MainTex_ST");
        DemoValidation.Check(floor.sharedMaterial.mainTexture != null && st.x > 4 && st.y > 3, "Floor texture tiles by world size");
        DemoValidation.Check(store.Find("InteriorFinish/StoneTiles") == null || !store.Find("InteriorFinish/StoneTiles").gameObject.activeSelf,
            "Tile skins hidden under the textured floor");
        Renderer safeFloor = store.Find("Architecture/SafeRoomFloor").GetComponent<Renderer>();
        DemoValidation.Check(safeFloor.sharedMaterial.name == "SafeRoomFloor" && safeFloor.sharedMaterial.mainTexture != null,
            "Safe room floor has its own textured material");
        DemoValidation.Info("Scanned textures=" + (HasScans ? "present" : "absent") + "; environment=" + (HasEnvironment ? "present" : "absent"));
        if (HasScans)
        {
            Material gold = AssetDatabase.LoadAssetAtPath<Material>("Assets/CrimeSceneDemo/InteriorGenerated/JewelryGold.mat");
            DemoValidation.Check(safeFloor.sharedMaterial.mainTexture.name.StartsWith("Parquet") && gold != null && gold.mainTexture != null,
                "Scanned parquet and gold maps applied");
        }
        if (HasEnvironment)
        {
            DemoValidation.Check(RenderSettings.skybox != null && RenderSettings.skybox.shader.name == "Skybox/Panoramic"
                && RenderSettings.skybox.GetTexture("_MainTex") != null, "Street sky uses the HDR panorama");
            DemoValidation.Check(RenderSettings.defaultReflectionMode == UnityEngine.Rendering.DefaultReflectionMode.Custom
                && RenderSettings.customReflectionTexture != null, "Reflections come from the HDR light room");
            DemoValidation.Check(Camera.main != null && Camera.main.clearFlags == CameraClearFlags.Skybox, "Player camera clears to the sky");
        }
    }
}
#endif
