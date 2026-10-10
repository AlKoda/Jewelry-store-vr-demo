#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

// Applies the generated tileable textures (Tools/make_textures.py, committed under
// Prototype/ThirdParty/GeneratedTextures) to the finish materials: stone floor,
// plaster walls, ceiling, marble counter, walnut cabinets, brass trims, carpet in
// the safe room, asphalt and pavement outside. Tiling is set per renderer from
// its world size so every surface shows the texture at the intended scale.
// Without the texture files nothing changes: the flat finish stays.
public static class ShopTextureFinish
{
    public const string Folder = DemoAssetLibrary.Root + "/GeneratedTextures";
    public const int ExpectedTextured = 8;

    [MenuItem("Crime Scene Demo/Apply Generated Textures")]
    public static void ApplyFromMenu() { Apply(); }

    // Returns the number of materials that received a texture.
    public static int Apply()
    {
        if (!Has("StoneTiles")) { Debug.Log("Generated textures absent; flat finish kept."); return 0; }
        Transform store = GameObject.Find("JewelryStore_Blockout").transform;
        int textured = 0;

        // Floor: the architecture floor boxes carry the tiles; the per-tone tile skins are hidden.
        Material stone = Textured("InteriorGenerated", "StoneGrout", "StoneTiles", 2f, 0.9f);
        Material carpet = Textured("InteriorGenerated", "SafeRoomCarpet", "DarkCarpet", 1.5f, 0.6f);
        if (stone != null)
        {
            textured++;
            Transform tiles = store.Find("InteriorFinish/StoneTiles");
            if (tiles != null) tiles.gameObject.SetActive(false);
            foreach (MeshRenderer r in store.Find("Architecture").GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.name.Contains("Floor")) continue;
                bool safeRoom = r.name.StartsWith("SafeRoom");
                r.sharedMaterial = safeRoom && carpet != null ? carpet : stone;
                TileByWorldSize(r, safeRoom ? 1.5f : 2f);
            }
            if (carpet != null) textured++;
        }
        textured += Retexture(store, "InteriorGenerated", "WarmPlaster", "Plaster", 2.5f, 0.5f, "Architecture");
        textured += Retexture(store, "InteriorGenerated", "CeilingPaint", "Ceiling", 3f, 0.3f, "Architecture");
        textured += Retexture(store, "InteriorGenerated", "CounterStone", "Marble", 1.2f, 0.3f, null);
        textured += Retexture(store, "InteriorGenerated", "SmokedOak", "Walnut", 1f, 0.6f, null);
        textured += Retexture(store, "InteriorGenerated", "DisplayBronze", "BrushedBrass", 0.5f, 0.4f, null);
        textured += Retexture(store, "InteriorGenerated", "SatinBronze", "BrushedBrass", 0.5f, 0.4f, null);

        // Street: the city meshes are combined by material, so only shared tiling applies.
        Transform city = GameObject.Find("CityBackdrop")?.transform;
        if (city != null)
        {
            Material asphalt = Textured("CityGenerated", "Asphalt", "Asphalt", 4f, 0.8f);
            Material pavement = Textured("CityGenerated", "Pavement", "Pavement", 2f, 0.8f);
            if (asphalt != null) { asphalt.SetTextureScale("_MainTex", new Vector2(35, 2)); textured++; }
            if (pavement != null) { pavement.SetTextureScale("_MainTex", new Vector2(70, 2)); textured++; }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Generated textures applied to " + textured + " materials.");
        return textured;
    }

    // Texture an existing finish material (if present) and tile every renderer using it.
    private static int Retexture(Transform store, string folder, string material, string texture, float metres, float bump, string under)
    {
        Material m = Textured(folder, material, texture, metres, bump);
        if (m == null) return 0;
        Transform root = under != null ? store.Find(under) : store;
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            if (r.sharedMaterial == m) TileByWorldSize(r, metres);
        return 1;
    }

    // Loads the saved material by name and gives it the albedo and normal textures.
    private static Material Textured(string folder, string material, string texture, float metres, float bump)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>("Assets/CrimeSceneDemo/" + folder + "/" + material + ".mat");
        if (m == null || !Has(texture)) return null;
        Texture2D albedo = Load(texture);
        Texture2D normal = Load(texture + "_Normal");
        if (normal != null) MarkNormalMap(texture + "_Normal");
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

    private static void MarkNormalMap(string texture)
    {
        TextureImporter importer = AssetImporter.GetAtPath(Folder + "/" + texture + ".png") as TextureImporter;
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
    }
}
#endif
