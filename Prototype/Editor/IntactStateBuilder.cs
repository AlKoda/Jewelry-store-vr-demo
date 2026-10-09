#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

// Builds the "intact shop" overlay (whole storefront glass, intact case tops,
// stock on the emptied pads) and the CrimeSceneState that swaps it with the
// damage and evidence. Run after the store, tools, refinement and expansion.
public static class IntactStateBuilder
{
    private const string Folder = "Assets/CrimeSceneDemo/PresentationGenerated";
    private static readonly string[] DamagePrefixes =
        { "JaggedGlassRemnant", "DroppedJewelryTray", "TrayInsert", "FallenCaseTrim", "DarkenedDoorEdge" };

    [MenuItem("Crime Scene Demo/Add Intact State Toggle")]
    public static void ApplyFromMenu() { Apply(); }

    public static CrimeSceneState Apply()
    {
        GameObject store = GameObject.Find("JewelryStore_Blockout");
        if (store == null) throw new System.InvalidOperationException("Generate the store first.");
        if (store.transform.Find("IntactOverlay") != null)
            throw new System.InvalidOperationException("Intact overlay already exists.");
        EnsureFolder("Assets/CrimeSceneDemo");
        EnsureFolder(Folder);
        Material glass = Translucent(Mat(Folder, "RemainingGlass", new Color(.62f, .83f, .87f, .19f), .18f));

        Transform overlay = Group("IntactOverlay", store.transform);
        // Whole storefront pane, where the remnants and shards are in the robbed state.
        Box("IntactStorefrontGlass", overlay, V(-1, 1.55f, -.08f), V(6.9f, 2.05f, .02f), glass);
        // Damaged displays: Island_Left and WallDisplay_Right (see JewelryStoreBuilder.Display).
        RestoreDisplay(overlay, glass, V(-1.7f, 0, 3.7f), V(1.2f, 1, 2.6f));
        RestoreDisplay(overlay, glass, V(4.45f, 0, 4), V(0.8f, 1, 3.2f));
        overlay.gameObject.SetActive(false);

        List<GameObject> robbed = new List<GameObject> { store.transform.Find("FixedEvidence").gameObject };
        foreach (Transform t in store.GetComponentsInChildren<Transform>(true))
            foreach (string prefix in DamagePrefixes)
                if (t.name.StartsWith(prefix)) { robbed.Add(t.gameObject); break; }

        CrimeSceneState state = store.AddComponent<CrimeSceneState>();
        state.RobbedOnly = robbed.ToArray();
        state.IntactOnly = new[] { overlay.gameObject };
        state.SafeDoorHinge = store.transform.Find("Furniture/OpenSafe/DoorHinge");
        state.Session = Object.FindFirstObjectByType<DemoSession>();
        DemoDesktopPanel panel = Object.FindFirstObjectByType<DemoDesktopPanel>();
        if (panel != null) panel.SceneState = state;
        return state;
    }

    // Intact case top plus stock on the two pads the robbery emptied.
    private static void RestoreDisplay(Transform overlay, Material glass, Vector3 p, Vector3 size)
    {
        Box("IntactCaseTop", overlay, p + V(0, 1.21f, 0), V(size.x - .06f, .018f, size.z - .06f), glass);
        for (int i = 2; i < 4; i++)
            JewelryStoreBuilder.Stock(overlay, p, -size.z * 0.32f + i * size.z * 0.21f, i % 2 == 0);
    }
}
#endif
