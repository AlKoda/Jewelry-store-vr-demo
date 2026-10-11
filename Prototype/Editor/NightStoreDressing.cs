#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

// The robbery as found at night, kept apart from the store layout so a later case can
// swap it: the case state the fixture builders read (broken glass, drawers, safe
// doors) and the dressing pass (deck stock, dropped jewelry along the route from the
// entrance past the central cluster and the right island to the office, the glass
// fans at the entrance, shoe print and discarded torch). Loose pieces are static
// scenery the player cannot pick up; only the gas bottle and the toppled chair keep
// colliders, so the dressing never snags the walker. Counts stay modest: every
// loose piece is a probe-lit renderer.
public static class NightStoreDressing
{
    // ---------------------------------------------------------------- case state

    public static StoreKit.CaseGlass CaseGlass(string fixture)
    {
        switch (fixture)
        {
            case "CentralCase_Left":
            case "CentralCase_Right": return StoreKit.CaseGlass.FrontBroken;
            case "RightIsland_Front": return StoreKit.CaseGlass.AllBroken;
            default: return StoreKit.CaseGlass.Intact;
        }
    }

    // Cases built with a drawer, and how far it is pulled out (the knocked case's lies on the floor).
    public static bool HasDrawer(string fixture) => fixture == "CentralCase_Middle" || fixture == "KnockedCase";
    public static float DrawerOut(string fixture) => fixture == "CentralCase_Middle" ? 0.3f : 0;

    // Only the top pane of the left island's front case is cracked: tried and abandoned.
    public const string CrackedTop = "LeftIsland_Front";

    public static float SafeOpen(string fixture) => fixture == "SafeLarge" ? 100 : fixture == "SafeTall" ? 80 : 0;

    // Toppled on its back between the desk and the safes, backrest towards the shelving.
    public static readonly Vector3 ChairPosition = V(-1.2f, 0, 10.45f);
    public const float ChairYaw = -100;

    // Groups whose children CombineLoose merges: each case's deck stock, and the floor
    // scatter in LooseCell metre cells (each merged mesh takes one light probe sample at
    // its centre, so a cell must stay small enough for the headlight falloff to read).
    public const string DisplaysGroup = "Displays", LooseGroup = "Loose";
    public const float LooseCell = 2.5f;

    // Renderers kept out of static batching so validators can read their meshes in Play mode.
    public static readonly string[] Unbatched = { "EntranceGlass_Outside", "EntranceGlass_Inside" };
    public const float GlassLift = 0.0012f;

    // ---------------------------------------------------------------- dressing pass

    private const float Deck = 0.71f; // Case deck top: 0.70 m carcass plus the 10 mm suede.

    public static void Apply(Transform dressing, Transform showroom, string meshFolder)
    {
        StoreKit.Palette();
        Transform displays = Group(DisplaysGroup, dressing);
        // One group per case, so CombineLoose merges each deck's stock into its own meshes.
        foreach (NightStoreLayout.Fixture f in NightStoreLayout.Fixtures)
            if (f.Kit == "display_case" || f.Kit == "wall_case") Stock(Group(f.Name, displays), showroom.Find(f.Name), f);
        Crack(showroom.Find(CrackedTop));
        KnockedDrawer(dressing, showroom.Find("KnockedCase"));
        Transform drawer = showroom.Find("CentralCase_Middle/Drawer");
        if (drawer != null) Put(StoreKit.RingTray(dressing), drawer, V(-0.35f, -0.035f, 0.22f), 6).name = "DrawerTray";

        Transform loose = Group(LooseGroup, dressing);
        Scatter(loose);
        Cells(loose);
        GameObject chair = StoreKit.OfficeChair(dressing, true);
        Put(chair, null, ChairPosition, ChairYaw);

        GlassFan(dressing, "EntranceGlass_Outside", V(0.1f, 0, -0.12f), -1, 2.5f, 70, 41, meshFolder);
        GlassFan(dressing, "EntranceGlass_Inside", V(0.1f, 0, 0.12f), 1, 1.5f, 50, 43, meshFolder);

        // The small shop's shoe print and cutting torch, by the office opening.
        Transform print = JewelryStoreBuilder.ShoePrint(dressing, V(0.35f, 0.0015f, 7.75f), 8);
        Transform torchPivot = Group("TorchSet", dressing);
        torchPivot.localPosition = V(-0.55f, 0, 8.95f);
        torchPivot.localRotation = Quaternion.Euler(0, 90, 0);
        Transform torch = JewelryStoreBuilder.Torch(torchPivot);
        torch.localPosition = V(-JewelryStoreBuilder.TorchCylinderX, 0, -JewelryStoreBuilder.TorchCylinderZ);
        Paint(print, StoreKit.Soil);
        Paint(torch, StoreKit.DeviceBlack);
        foreach (Renderer r in torch.GetComponentsInChildren<Renderer>())
        {
            if (r.name.StartsWith("Cylinder")) r.sharedMaterial = StoreKit.PaintedGrey;
            else if (r.name.StartsWith("Valve") || r.name == "PressureGauge") r.sharedMaterial = StoreKit.Stainless;
            else if (r.name == "TorchNozzle") r.sharedMaterial = StoreKit.Bronze;
        }

        foreach (Collider c in dressing.GetComponentsInChildren<Collider>(true))
            if (c.name != "Cylinder" && c.gameObject != chair) Object.DestroyImmediate(c);
    }

    // Deck stock in the case's own space (front -z). Intact cases are fully dressed;
    // broken ones are emptied with a tipped bust and an opened box left behind; the
    // cracked-top case keeps its stock but its bust went over.
    private static void Stock(Transform parent, Transform fixture, NightStoreLayout.Fixture f)
    {
        if (fixture == null) return;
        float back = f.Kit == "wall_case" ? 0.05f : 0; // Clear of a wall case's back panel posts.
        StoreKit.CaseGlass glass = CaseGlass(f.Name);
        bool robbed = glass != StoreKit.CaseGlass.Intact || f.Name == "KnockedCase";
        bool tipped = robbed || f.Name == CrackedTop;
        GameObject bust = Put(StoreKit.NecklaceBust(parent, !robbed), fixture, V(-0.6f, Deck, (tipped ? -0.12f : 0.03f) + back * 0.5f), tipped ? 25 : 0);
        if (tipped) Tip(bust, fixture);
        Put(StoreKit.RingTray(parent, !robbed), fixture, V(-0.2f, Deck, back * 0.5f), robbed ? 11 : 0);
        if (robbed)
        {
            Put(StoreKit.DisplayRiser(parent, StoreKit.Riser.Small), fixture, V(0.15f, Deck, 0.06f + back * 0.5f), 17);
            Put(StoreKit.JewelryBox(parent, StoreKit.JewelryBoxKind.Ring), fixture, V(0.45f, Deck, -0.05f + back * 0.5f), -28);
        }
        else
        {
            Put(StoreKit.EarringStand(parent), fixture, V(0.15f, Deck, 0.05f + back * 0.5f), 0);
            Put(StoreKit.BraceletStand(parent), fixture, V(0.5f, Deck, back * 0.5f), 0);
        }
    }

    // A bust knocked onto its back: turned about its own x so the head points rearwards, then seated.
    private static void Tip(GameObject bust, Transform fixture)
    {
        bust.transform.rotation = bust.transform.rotation * Quaternion.Euler(90, 0, 0);
        Seat(bust, fixture.position.y + Deck + 0.001f);
    }

    private static void Crack(Transform fixture)
    {
        Transform top = fixture != null ? fixture.Find("Glass_Top") : null;
        if (top == null) return;
        // Cracks draws in a pane's local xy plane; this child lays that plane on the top pane.
        Transform plane = Group("CrackPlane", top);
        plane.localRotation = Quaternion.Euler(90, 0, 0);
        StoreKit.Cracks(plane, 1.8f - 0.11f, 0.65f - 0.11f);
    }

    // The knocked case's drawer, pulled right out and dropped flat on the floor beside
    // it with its open side away from the case (renamed so it is static scenery).
    private static void KnockedDrawer(Transform dressing, Transform fixture)
    {
        Transform drawer = fixture != null ? fixture.Find("Drawer") : null;
        if (drawer == null) return;
        drawer.SetParent(dressing, true);
        drawer.name = "KnockedDrawer";
        drawer.position = V(-6.35f, 0.055f, 2.2f);
        drawer.rotation = Quaternion.Euler(0, NightStoreLayout.Find("KnockedCase").Yaw + 192, 0);
    }

    // Dropped and abandoned pieces: a trail from the entrance past the central cluster
    // and the right island to the office, then the emptied trays and boxes in front of
    // both safes, a tray left on the desk and rings near the large safe.
    private static void Scatter(Transform loose)
    {
        Vector4[] rings =
        {
            new Vector4(0.55f, 0, 1.3f, 0), new Vector4(1.7f, 0, 4.45f, 40), new Vector4(3.9f, 0, 5.3f, 0), new Vector4(0.4f, 0, 8.0f, 0),
            new Vector4(1.75f, 0, 10.2f, 0), new Vector4(1.3f, 0, 10.05f, 0), new Vector4(2.45f, 0, 10.3f, 0)
        };
        foreach (Vector4 r in rings) Put(StoreKit.DroppedRing(loose), null, r, r.w);
        Put(StoreKit.DroppedBangle(loose), null, V(2.3f, 0, 4.35f), 0);
        Put(StoreKit.JewelryBox(loose, StoreKit.JewelryBoxKind.Ring), null, V(1.1f, 0, 2.7f), 35);
        Put(StoreKit.JewelryBox(loose, StoreKit.JewelryBoxKind.Bracelet), null, V(3.6f, 0, 3.7f), -20);
        Put(StoreKit.JewelryBox(loose, StoreKit.JewelryBoxKind.Necklace), null, V(3.1f, 0, 6.6f), 70);
        Put(StoreKit.ShoppingBag(loose, true), null, V(2.0f, 0, 7.2f), 15);
        Put(StoreKit.TransportTray(loose), null, V(1.0f, 0, 10.35f), 18);
        Put(StoreKit.TransportTray(loose), null, V(-0.35f, 0, 10.75f), -12);
        Put(StoreKit.JewelryBox(loose, StoreKit.JewelryBoxKind.Ring), null, V(1.55f, 0, 10.5f), 60);
        Put(StoreKit.JewelryBox(loose, StoreKit.JewelryBoxKind.Necklace), null, V(0.6f, 0, 10.0f), -35);
        Put(StoreKit.JewelryBox(loose, StoreKit.JewelryBoxKind.Bracelet), null, V(-0.45f, 0, 9.95f), -30);
        Put(StoreKit.TransportTray(loose), null, NightStoreLayout.Find("Desk").Position + V(-0.05f, 0.75f, -0.05f), 8);
    }

    // Small flat shards in a fan from the threshold (side -1 outside, +1 inside), denser
    // near the doors, kept off the sill line and the bollards. One mesh per fan, all
    // with their bases GlassLift above the floor or pavement (y 0).
    private static void GlassFan(Transform parent, string name, Vector3 centre, float side, float radius, int count, int seed, string folder)
    {
        KitMesh k = new KitMesh { Lightmapped = false };
        System.Random rng = new System.Random(seed);
        for (int made = 0, guard = 0; made < count && guard < count * 20; guard++)
        {
            float u = (float)rng.NextDouble(), r = 0.08f + radius * u * u, phi = Range(rng, -75, 75) * Mathf.Deg2Rad;
            Vector3 c = centre + V(Mathf.Sin(phi), 0, side * Mathf.Cos(phi)) * r;
            if (side * c.z < 0.16f || NearBollard(c)) continue;
            float size = Mathf.Lerp(0.07f, 0.025f, r / radius) * Range(rng, 0.6f, 1.3f), a0 = Range(rng, 0, Mathf.PI * 2);
            // A 3-6 mm thick sliver: its top tilts a few degrees so each one catches the
            // headlights and streetlights on its own; the base lies flat at GlassLift.
            Vector3[] points = new Vector3[3], tops = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                float a = a0 + i * Mathf.PI * 2 / 3 + Range(rng, -0.35f, 0.35f);
                points[i] = c + V(Mathf.Cos(a), 0, Mathf.Sin(a)) * size * Range(rng, 0.45f, 1f);
                points[i].y = GlassLift;
                tops[i] = points[i] + Vector3.up * Range(rng, 0.003f, 0.006f);
            }
            k.Face(StoreKit.ShardGlass, Vector3.up, tops);
            Vector3 mid = (points[0] + points[1] + points[2]) / 3;
            for (int i = 0; i < 3; i++)
            {
                int j = (i + 1) % 3;
                Vector3 outward = (points[i] + points[j]) * 0.5f - mid;
                outward.y = 0;
                k.Face(StoreKit.ShardGlass, outward, points[i], points[j], tops[j], tops[i]);
            }
            made++;
        }
        k.Emit(name, parent, Vector3.zero, Quaternion.identity, folder, "night_" + name.ToLowerInvariant());
    }

    private static bool NearBollard(Vector3 p)
    {
        foreach (float x in NightStoreLayout.BollardX)
            if (new Vector2(p.x - x, p.z - NightStoreLayout.BollardZ).sqrMagnitude < 0.16f * 0.16f) return true;
        return false;
    }

    // Sorts the scattered pieces into Cell_<i>_<j> groups by their floor position.
    private static void Cells(Transform loose)
    {
        List<Transform> pieces = new List<Transform>();
        foreach (Transform t in loose) pieces.Add(t);
        foreach (Transform t in pieces)
        {
            string name = "Cell_" + Mathf.FloorToInt(t.position.x / LooseCell) + "_" + Mathf.FloorToInt(t.position.z / LooseCell);
            Transform cell = loose.Find(name);
            if (cell == null) cell = Group(name, loose);
            t.SetParent(cell, true);
        }
    }

    // Loose props cost one renderer per part and material (about 150 for the decks and
    // the scatter); after the static flags are set, each case's stock and each loose cell
    // become one mesh per material. They stay probe-lit, out of GI like the small props
    // they were, and batch statically. Returns the renderers left in those groups.
    public static int CombineLoose(Transform dressing, string meshFolder)
    {
        int renderers = 0;
        foreach (string groupName in new[] { DisplaysGroup, LooseGroup })
        {
            Transform group = dressing.Find(groupName);
            if (group == null) continue;
            foreach (Transform part in group)
            {
                string prefix = "night_dressing_" + groupName.ToLowerInvariant() + "_" + part.name.ToLowerInvariant() + "_";
                foreach (MeshRenderer r in DemoGeometry.CombineSubmeshesByMaterial(part, meshFolder + "/" + prefix, part.name + "_"))
                {
                    bool transparent = r.sharedMaterial.renderQueue >= 3000;
                    StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
                    if (!transparent) flags |= StaticEditorFlags.ReflectionProbeStatic;
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);
                    r.receiveGI = ReceiveGI.LightProbes;
                    renderers++;
                }
            }
        }
        return renderers;
    }

    // Places a built prop at a point in frame's space (world when frame is null), turned by yaw.
    private static GameObject Put(GameObject prop, Transform frame, Vector3 local, float yaw)
    {
        prop.transform.position = frame != null ? frame.TransformPoint(local) : local;
        prop.transform.rotation = (frame != null ? frame.rotation : Quaternion.identity) * Quaternion.Euler(0, yaw, 0);
        return prop;
    }

    // Lowers or raises the prop until its lowest renderer point rests on surface.
    private static void Seat(GameObject prop, float surface)
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

    private static void Paint(Transform root, Material material)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>()) r.sharedMaterial = material;
    }

    private static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

    // The resting (base) vertices of the fans' shards for the flush check, in world space.
    public static List<Vector3> GlassVertices(Transform dressing)
    {
        List<Vector3> points = new List<Vector3>();
        foreach (string name in Unbatched)
        {
            Transform fan = dressing.Find(name);
            MeshFilter filter = fan != null ? fan.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null) continue;
            foreach (Vector3 v in filter.sharedMesh.vertices)
                if (v.y <= GlassLift + 0.0001f) points.Add(fan.TransformPoint(v));
        }
        return points;
    }
}
#endif
