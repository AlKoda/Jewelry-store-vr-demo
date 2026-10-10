#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Third-party props placed over the generated shop: seating, counter decor and
// display items from the Khronos sample set (ThirdParty/KhronosSamples) and
// KayKit furniture (ThirdParty/KayKitFurniture, one atlas texture, 0.66 scale).
// Every item is optional; missing files leave the generated geometry as it is.
public static class ShopAssetDressing
{
    private const string Pack = "KhronosSamples";
    private const string Furniture = "KayKitFurniture";
    private const string Storage = "KayKitPrototype";
    private const float FurnitureScale = 0.66f;
    public const int Count = 22;

    [MenuItem("Crime Scene Demo/Add Third-Party Dressing")]
    public static void Apply()
    {
        GameObject store = GameObject.Find("JewelryStore_Blockout");
        if (store == null) throw new System.InvalidOperationException("Generate the store first.");
        if (store.transform.Find("ThirdPartyDressing") != null)
            throw new System.InvalidOperationException("Dressing already added.");
        Transform group = DemoGeometry.Group("ThirdPartyDressing", store.transform);
        int placed = 0;
        // Customer seating: armchair by the window left of the rack, sofa along the right wall.
        placed += Place("SheenChair", group, new Vector3(-4.35f, 0, 1.9f), 90, 1, true);
        placed += Place("GlamVelvetSofa", group, new Vector3(4.4f, 0, 6.8f), -90, 1, true);
        placed += Place("ChairDamaskPurplegold", group, new Vector3(1.4f, 0, 10.3f), 60, 1, true);
        placed += Place("DiffuseTransmissionPlant", group, new Vector3(4.5f, 0, 1.0f), 0, 1, false);
        // Counter decor on the counter top (top surface at y = 1.165).
        placed += Place("GlassVaseFlowers", group, new Vector3(-1.3f, 1.165f, 6.5f), 20, 1, false);
        placed += Place("GlassHurricaneCandleHolder", group, new Vector3(-2.1f, 1.165f, 6.45f), 0, 1, false);
        placed += Place("IridescenceLamp", group, new Vector3(-0.3f, 1.165f, 6.65f), 0, 1, false);
        placed += Place("WaterBottle", group, new Vector3(0.05f, 1.295f, 6.35f), 0, 1, false);
        // Stock in the intact display case (display surface at y = 0.84).
        placed += Place("SunglassesKhronos", group, new Vector3(2.1f, 0.84f, 3.15f), 100, 1, false);
        placed += Place("ChronographWatch", group, new Vector3(2.1f, 0.84f, 3.9f), 30, 1, false);
        // KayKit furniture: a reading lamp in the rear left corner, a rug in front of the
        // sofa, pictures on both side walls, a shelf on the rear wall, and a cabinet and
        // cactus in the safe room so the back room reads as a used office, not a box.
        placed += Place(Furniture, "lamp_standing", group, new Vector3(-4.5f, 0, 7.2f), 0, FurnitureScale, true);
        GameObject rug = DemoAssetLibrary.Place(Furniture, "rug_rectangle_A", group, new Vector3(3.3f, 0.001f, 6.8f), 90, FurnitureScale);
        if (rug != null) { rug.transform.localScale = new Vector3(FurnitureScale, 0.12f, FurnitureScale); placed++; }
        placed += Place(Furniture, "pictureframe_large_A", group, new Vector3(4.99f, 1.6f, 1.5f), -90, FurnitureScale, false);
        placed += Place(Furniture, "pictureframe_large_A", group, new Vector3(-4.99f, 1.75f, 6.3f), 90, FurnitureScale, false);
        placed += Place(Furniture, "shelf_B_large_decorated", group, new Vector3(4.1f, 1.3f, 7.97f), 180, FurnitureScale, false);
        placed += Place(Furniture, "cabinet_medium_decorated", group, new Vector3(1.35f, 0, 9.6f), 90, FurnitureScale, true);
        placed += Place(Furniture, "cactus_medium_A", group, new Vector3(3.7f, 0, 8.55f), 0, FurnitureScale, false);
        // A low table with books on the rug completes the seating corner; a standing
        // frame joins the counter decor.
        placed += Place(Furniture, "table_low", group, new Vector3(3.3f, 0, 6.8f), 90, 0.5f, true);
        placed += Place(Furniture, "book_set", group, new Vector3(3.3f, 0.38f, 6.8f), 70, 0.5f, false);
        placed += Place(Furniture, "pictureframe_standing_A", group, new Vector3(-2.75f, 1.165f, 6.6f), -15, FurnitureScale, false);
        // Robbery staging in the safe room: shipment boxes and a barrel still waiting
        // to be unpacked (KayKit Prototype Bits, their own atlas).
        placed += Place(Storage, "Box_A", group, new Vector3(3.45f, 0, 10.35f), 25, 1, false);
        placed += Place(Storage, "Box_B", group, new Vector3(3.45f, 0.51f, 10.35f), 70, 1, false);
        placed += Place(Storage, "Barrel_A", group, new Vector3(1.6f, 0.26f, 8.6f), 0, 0.52f, false);
        Debug.Log("Third-party dressing placed: " + placed + " of " + Count + " models.");
    }

    private static int Place(string model, Transform parent, Vector3 position, float yaw, float scale, bool solid)
        => Place(Pack, model, parent, position, yaw, scale, solid);

    // Furniture gets a box collider from its bounds so the player cannot walk through it.
    private static int Place(string pack, string model, Transform parent, Vector3 position, float yaw, float scale, bool solid)
    {
        GameObject instance = DemoAssetLibrary.Place(pack, model, parent, position, yaw, scale);
        if (instance == null) return 0;
        if (solid) DemoAssetLibrary.AddBoundsCollider(instance);
        return 1;
    }
}
#endif
