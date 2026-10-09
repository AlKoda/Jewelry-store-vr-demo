#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Third-party props placed over the generated shop: seating, counter decor and
// display items from the Khronos sample set (see ThirdParty/KhronosSamples).
// Every item is optional; missing files leave the generated geometry as it is.
public static class ShopAssetDressing
{
    private const string Pack = "KhronosSamples";
    public const int Count = 9;

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
        Debug.Log("Third-party dressing placed: " + placed + " of " + Count + " models.");
    }

    // Furniture gets a box collider from its bounds so the player cannot walk through it.
    private static int Place(string model, Transform parent, Vector3 position, float yaw, float scale, bool solid)
    {
        GameObject instance = DemoAssetLibrary.Place(Pack, model, parent, position, yaw, scale);
        if (instance == null) return 0;
        if (solid) DemoAssetLibrary.AddBoundsCollider(instance);
        return 1;
    }
}
#endif
