#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Third-party props placed over the generated shop: seating, counter decor and
// display items from the Khronos sample set (ThirdParty/KhronosSamples) and
// KayKit furniture (ThirdParty/KayKitFurniture, one atlas texture, 0.66 scale).
// Every item is optional; missing files leave the generated geometry as it is.
public static class ShopAssetDressing
{
    private const string Khronos = "KhronosSamples";
    private const string Furniture = "KayKitFurniture";
    private const string Storage = "KayKitPrototype";
    private const float K = 0.66f; // KayKit furniture is modelled about 1.5x real size.

    private struct Item
    {
        public string Pack, Model;
        public Vector3 Position, Scale;
        public float Yaw;
        public bool Solid;
        public Item(string pack, string model, Vector3 position, float yaw, float scale, bool solid = false)
            : this(pack, model, position, yaw, Vector3.one * scale, solid) { }
        public Item(string pack, string model, Vector3 position, float yaw, Vector3 scale, bool solid = false)
        { Pack = pack; Model = model; Position = position; Yaw = yaw; Scale = scale; Solid = solid; }
    }

    // One table drives both placement and validation, so the expected count can never
    // drift from the code that places the models. Solid items get a bounds collider.
    private static readonly Item[] Items =
    {
        // Customer seating: armchair by the window left of the rack, sofa along the right wall.
        new Item(Khronos, "SheenChair", new Vector3(-4.35f, 0, 1.9f), 90, 1, true),
        new Item(Khronos, "GlamVelvetSofa", new Vector3(4.4f, 0, 6.8f), -90, 1, true),
        new Item(Khronos, "ChairDamaskPurplegold", new Vector3(1.4f, 0, 10.3f), 60, 1, true),
        new Item(Khronos, "DiffuseTransmissionPlant", new Vector3(4.5f, 0, 1.0f), 0, 1),
        // Counter decor on the counter top (top surface at y = 1.165).
        new Item(Khronos, "GlassVaseFlowers", new Vector3(-1.3f, 1.165f, 6.5f), 20, 1),
        new Item(Khronos, "GlassHurricaneCandleHolder", new Vector3(-2.1f, 1.165f, 6.45f), 0, 1),
        new Item(Khronos, "IridescenceLamp", new Vector3(-0.3f, 1.165f, 6.65f), 0, 1),
        new Item(Khronos, "WaterBottle", new Vector3(0.05f, 1.295f, 6.35f), 0, 1),
        // Stock in the intact display case (display surface at y = 0.84).
        new Item(Khronos, "SunglassesKhronos", new Vector3(2.1f, 0.84f, 3.15f), 100, 1),
        new Item(Khronos, "ChronographWatch", new Vector3(2.1f, 0.84f, 3.9f), 30, 1),
        // KayKit furniture: a reading lamp in the rear left corner, a flattened rug and a low
        // table with books in front of the sofa, pictures on both side walls, a shelf on the
        // rear wall, a standing frame on the counter, and a cabinet and cactus in the safe room.
        new Item(Furniture, "lamp_standing", new Vector3(-4.5f, 0, 7.2f), 0, K, true),
        new Item(Furniture, "rug_rectangle_A", new Vector3(3.3f, 0.001f, 6.8f), 90, new Vector3(K, 0.12f, K)),
        new Item(Furniture, "table_low", new Vector3(3.3f, 0, 6.8f), 90, 0.5f, true),
        new Item(Furniture, "book_set", new Vector3(3.3f, 0.38f, 6.8f), 70, 0.5f),
        new Item(Furniture, "pictureframe_large_A", new Vector3(4.99f, 1.6f, 1.5f), -90, K),
        new Item(Furniture, "pictureframe_large_A", new Vector3(-4.99f, 1.75f, 6.3f), 90, K),
        new Item(Furniture, "shelf_B_large_decorated", new Vector3(4.1f, 1.3f, 7.97f), 180, K),
        new Item(Furniture, "pictureframe_standing_A", new Vector3(-2.75f, 1.165f, 6.6f), -15, K),
        new Item(Furniture, "cabinet_medium_decorated", new Vector3(1.35f, 0, 9.6f), 90, K, true),
        new Item(Furniture, "cactus_medium_A", new Vector3(3.7f, 0, 8.55f), 0, K),
        // Safe-room storage: shipment boxes and a barrel (KayKit Prototype Bits).
        new Item(Storage, "Box_A", new Vector3(3.45f, 0, 10.35f), 25, 1),
        new Item(Storage, "Box_B", new Vector3(3.45f, 0.51f, 10.35f), 70, 1),
        new Item(Storage, "Barrel_A", new Vector3(1.6f, 0.26f, 8.6f), 0, 0.52f),
    };

    public static int Count => Items.Length;

    // Models whose files are present; the generated dressing must hold exactly these.
    public static int ExpectedPresent()
    {
        int present = 0;
        foreach (Item item in Items) if (DemoAssetLibrary.Has(item.Pack, item.Model)) present++;
        return present;
    }

    [MenuItem("Crime Scene Demo/Add Third-Party Dressing")]
    public static void Apply()
    {
        GameObject store = GameObject.Find("JewelryStore_Blockout");
        if (store == null) throw new System.InvalidOperationException("Generate the store first.");
        if (store.transform.Find("ThirdPartyDressing") != null)
            throw new System.InvalidOperationException("Dressing already added.");
        Transform group = DemoGeometry.Group("ThirdPartyDressing", store.transform);
        int placed = 0;
        foreach (Item item in Items)
        {
            GameObject instance = DemoAssetLibrary.Place(item.Pack, item.Model, group, item.Position, item.Yaw, 1);
            if (instance == null) continue;
            instance.transform.localScale = item.Scale;
            if (item.Solid) DemoAssetLibrary.AddBoundsCollider(instance);
            placed++;
        }
        Debug.Log("Third-party dressing placed: " + placed + " of " + Count + " models.");
    }
}
#endif
