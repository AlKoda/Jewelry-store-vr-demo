#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

// The robbery as found at night, kept apart from the store layout so a later case can
// swap it: the case state the fixture builders read (broken glass, drawers, safe
// doors) and the dressing pass. The story: under the closing procedure (all stock into
// the safes by 17:45, under dual control) the cases hold display furniture only at
// night. The burglars smashed cases they found empty, forced both safes and dropped
// some of the stock on their way out. So the decks keep bare busts, stands and trays;
// the floor shows the trail from the office past the right island and the central
// cluster to the entrance (dropped rings, opened boxes and trays); the pack jewellery
// left behind lies in the open safes, on the desk and on the floor (Stock); then the
// glass fans at the entrance, the shoe print and the discarded torch. StockInSafes
// false restores the October 11 stocked decks (for Koda). Loose pieces are static
// scenery the player cannot pick up; only the gas bottle and the toppled chair keep
// colliders, so the dressing never snags the walker. Counts stay modest: every loose
// piece is a probe-lit renderer.
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

    // The closing procedure: true (the default) puts the stock in the safes, so the decks
    // hold display furniture only; false restores the October 11 stocked decks (chains on
    // the busts, rings in the trays, an opened box on each robbed deck). Koda's choice.
    // Read-only static, not const, so a test of it is never flagged as unreachable code.
    public static readonly bool StockInSafes = true;

    // Toppled on its back between the desk and the safes, backrest towards the shelving.
    public static readonly Vector3 ChairPosition = V(-1.2f, 0, 10.45f);
    public const float ChairYaw = -100;

    // Groups whose children CombineLoose merges: each case's deck furniture, and the floor
    // scatter in LooseCell metre cells (each merged mesh takes one light probe sample at
    // its centre, so a cell must stay small enough for the headlight falloff to read).
    public const string DisplaysGroup = "Displays", LooseGroup = "Loose";
    public const float LooseCell = 2.5f;

    // The pack jewellery left behind, kept out of CombineLoose so every piece keeps its
    // LODGroup: Stock/SafeLarge and Stock/SafeTall (named after the safe they lie in),
    // Stock/Desk and Stock/Floor.
    public const string StockGroup = "Stock", StockDesk = "Desk", StockFloor = "Floor";
    public const string JewelryPack = "JewelryModels";

    // Renderers kept out of static batching so validators can read their meshes in Play mode.
    public static readonly string[] Unbatched = { "EntranceGlass_Outside", "EntranceGlass_Inside" };
    public const float GlassLift = 0.0012f;

    // ---------------------------------------------------------------- dressing pass

    private const float Deck = 0.71f; // Case deck top: 0.70 m carcass plus the 10 mm suede.

    public static void Apply(Transform dressing, Transform showroom, string meshFolder)
    {
        StoreKit.Palette();
        Transform displays = Group(DisplaysGroup, dressing);
        // One group per case, so CombineLoose merges each deck's furniture into its own meshes.
        foreach (NightStoreLayout.Fixture f in NightStoreLayout.Fixtures)
            if (f.Kit == "display_case" || f.Kit == "wall_case") Stock(Group(f.Name, displays), showroom.Find(f.Name), f);
        Crack(showroom.Find(CrackedTop));
        KnockedDrawer(dressing, showroom.Find("KnockedCase"));
        Transform drawer = showroom.Find("CentralCase_Middle/Drawer");
        if (drawer != null) Put(StoreKit.RingTray(dressing), drawer, V(-0.35f, -0.035f, 0.22f), 6).name = "DrawerTray";

        Transform loose = Group(LooseGroup, dressing);
        Scatter(loose);
        Cells(loose);
        // The safes stand in the office zone, beside the showroom under the store root.
        LeftBehind(Group(StockGroup, dressing), showroom.parent != null ? showroom.parent.Find("Office") : null);
        GameObject chair = StoreKit.OfficeChair(dressing, true);
        Put(chair, null, ChairPosition, ChairYaw);

        foreach (Fan fan in Fans) GlassFan(dressing, fan, meshFolder);

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

    // Deck furniture in the case's own space (front -z). Intact decks keep their bare
    // stands; robbed (broken or knocked) ones a tipped bust, the ring tray and a riser;
    // the cracked-top case keeps its stands but its bust went over. Only with
    // StockInSafes false are the intact decks stocked and an opened box left on each robbed one.
    private static void Stock(Transform parent, Transform fixture, NightStoreLayout.Fixture f)
    {
        if (fixture == null) return;
        float back = f.Kit == "wall_case" ? 0.05f : 0; // Clear of a wall case's back panel posts.
        StoreKit.CaseGlass glass = CaseGlass(f.Name);
        bool robbed = glass != StoreKit.CaseGlass.Intact || f.Name == "KnockedCase";
        bool tipped = robbed || f.Name == CrackedTop;
        bool stocked = !StockInSafes && !robbed, openedBox = !StockInSafes && robbed;
        GameObject bust = Put(StoreKit.NecklaceBust(parent, stocked), fixture, V(-0.6f, Deck, (tipped ? -0.12f : 0.03f) + back * 0.5f), tipped ? 25 : 0);
        if (tipped) Tip(bust, fixture);
        Put(StoreKit.RingTray(parent, stocked), fixture, V(-0.2f, Deck, back * 0.5f), robbed ? 11 : 0);
        if (robbed)
        {
            Put(StoreKit.DisplayRiser(parent, StoreKit.Riser.Small), fixture, V(0.15f, Deck, 0.06f + back * 0.5f), 17);
            if (openedBox) Put(StoreKit.JewelryBox(parent, StoreKit.JewelryBoxKind.Ring), fixture, V(0.45f, Deck, -0.05f + back * 0.5f), -28);
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
        DemoAssetLibrary.Seat(bust, fixture.position.y + Deck + 0.001f);
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

    // Dropped and abandoned pieces: a trail from the office past the right island and the
    // central cluster to the entrance, the emptied trays and boxes in front of both safes,
    // a tray left on the desk (right of its middle, clear of the desk equipment) and rings
    // near the large safe.
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
        Put(StoreKit.TransportTray(loose), null, NightStoreLayout.Find("Desk").Position + V(0.45f, 0.75f, 0), 8);
    }

    // ---------------------------------------------------------------- left-behind jewellery

    // The jewellery palette beside StoreKit's (saved in StoreKit.Folder): an opaque diamond
    // (smoothness held at 0.92 so its specular does not shimmer in VR), pearl and the ring
    // box's navy velvet, at JewelryModels materials.json's values otherwise.
    public static Material Gem { get; private set; }
    public static Material Pearl { get; private set; }
    public static Material VelvetNavy { get; private set; }

    private static void JewelPalette()
    {
        Gem = Metallic(Mat(StoreKit.Folder, "KitGem", new Color(0.92f, 0.94f, 0.97f), 0.92f), 0.9f);
        Pearl = Metallic(Mat(StoreKit.Folder, "KitPearl", new Color(0.97f, 0.95f, 0.92f), 0.8f), 0.1f);
        VelvetNavy = Metallic(Mat(StoreKit.Folder, "KitVelvetNavy", new Color(0.20f, 0.24f, 0.37f), 0.18f), 0);
    }

    private static Material Metallic(Material m, float metallic)
    {
        m.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(m);
        return m;
    }

    // JewelryModels materials by their MTL name -> the store's palette, so the pack's
    // jewellery shares the cases' gold, silver and suede; null lets PackMaterial build the
    // textured ones (GoldCoin, GoldBar1kg, GemPaper).
    public static Material JewelryRemap(string material)
    {
        switch (material)
        {
            case "Gold18k": return StoreKit.Gold;
            case "Platinum":
            case "Chrome": return StoreKit.Silver;
            case "BlackMetal": return StoreKit.DeviceBlack;
            case "Glass": return StoreKit.Glass;
            case "Cord": return StoreKit.Cord;
            case "VelvetCream": return StoreKit.Suede;
            case "Diamond": return Gem;
            case "Pearl": return Pearl;
            case "VelvetNavy": return VelvetNavy;
            default: return null;
        }
    }

    // LODGroup class per placed model: Tiny (the pack's _LOD1 file is the top level) for
    // the coin, band, closed ring box and loupe; Prop for the gem parcel and the pearl
    // strand; None for the 364-triangle gold bar, which has no LOD files.
    private static readonly Dictionary<string, DemoAssetLibrary.LodClass> JewelLods = new Dictionary<string, DemoAssetLibrary.LodClass>
    {
        ["GoldCoin"] = DemoAssetLibrary.LodClass.Tiny, ["RingGoldBand"] = DemoAssetLibrary.LodClass.Tiny,
        ["RingBoxClosed"] = DemoAssetLibrary.LodClass.Tiny, ["JewelersLoupe"] = DemoAssetLibrary.LodClass.Tiny,
        ["GemParcel"] = DemoAssetLibrary.LodClass.Prop, ["NecklacePearlDropped"] = DemoAssetLibrary.LodClass.Prop,
        ["GoldBar1kg"] = DemoAssetLibrary.LodClass.None,
    };

    public static DemoAssetLibrary.LodClass JewelLod(string model)
        => JewelLods.TryGetValue(model, out DemoAssetLibrary.LodClass lod) ? lod : DemoAssetLibrary.LodClass.None;

    // What the burglars left: in the large safe a gold bar on the floor, a gem parcel and
    // three closed ring boxes on the lower shelf and two coins on the upper one; a coin
    // on the tall safe's lower shelf; the loupe on the desk; a pearl strand, a coin and a
    // band dropped on the floor, each at least 0.3 m from the walk route and the UV
    // traces. Surfaces in safe space (StoreKit.Safe): lining floor 0.141 m, shelf tops a
    // third and two thirds up the lining (0.574 and 0.997 in the large safe, 0.607 the
    // tall safe's lower one).
    private static void LeftBehind(Transform stock, Transform office)
    {
        JewelPalette();
        Transform large = office != null ? office.Find("SafeLarge") : null, tall = office != null ? office.Find("SafeTall") : null;
        if (large != null)
        {
            Transform g = Group(large.name, stock);
            Jewel(g, "GoldBar1kg", large, V(-0.3f, 0.141f, 0.1f), Vector3.zero, 8);
            Jewel(g, "GemParcel", large, V(-0.15f, 0.574f, 0), Vector3.zero, 20);
            Vector4[] boxes = { new Vector4(0.15f, 0.574f, 0.12f, -6), new Vector4(0.24f, 0.574f, 0.10f, 4), new Vector4(0.33f, 0.574f, 0.14f, -12) };
            foreach (Vector4 b in boxes) Jewel(g, "RingBoxClosed", large, b, Vector3.zero, b.w);
            Jewel(g, "GoldCoin", large, V(0.30f, 0.997f, -0.05f), Vector3.zero, 37);
            Jewel(g, "GoldCoin", large, V(0.36f, 0.997f, -0.02f), Vector3.zero, 37);
        }
        if (tall != null) Jewel(Group(tall.name, stock), "GoldCoin", tall, V(0.05f, 0.607f, 0), Vector3.zero, 0);
        Jewel(Group(StockDesk, stock), "JewelersLoupe", null, NightStoreLayout.Find("Desk").Position + V(-0.55f, 0.75f, 0.15f), Vector3.zero, 30);
        Transform floor = Group(StockFloor, stock);
        Jewel(floor, "NecklacePearlDropped", null, V(-0.6f, 0, 7.9f), Vector3.zero, 25);
        Jewel(floor, "GoldCoin", null, V(2.05f, 0, 10.65f), Vector3.zero, 0);
        // The band lies flat: its finger axis (x in the pack) turned upright.
        Jewel(floor, "RingGoldBand", null, V(2.9f, 0, 6.15f), V(0, 0, 90), 0);
    }

    // One JewelryModels piece under parent at a point in frame's space (world when frame
    // is null) that is also the surface it rests on, turned by yaw; euler turns the model
    // inside its root (its lying pose). Shadowless and probe-lit, materials through
    // JewelryRemap. Without the pack: StoreKit's dropped ring for the band, its closed
    // ring box for the ring box, nothing for the rest. Returns what was placed, or null.
    private static GameObject Jewel(Transform parent, string model, Transform frame, Vector3 local, Vector3 euler, float yaw)
    {
        GameObject g = DemoAssetLibrary.PlacePack(JewelryPack, model, parent, Vector3.zero, 0, new DemoAssetLibrary.PackOptions
        {
            ModelEuler = euler, Shadows = false, Lod = JewelLod(model), Remap = JewelryRemap
        });
        if (g == null)
        {
            g = model == "RingGoldBand" ? StoreKit.DroppedRing(parent)
                : model == "RingBoxClosed" ? StoreKit.JewelryBox(parent, StoreKit.JewelryBoxKind.Ring, 0) : null;
            if (g == null) return null;
        }
        Put(g, frame, local, yaw);
        DemoAssetLibrary.Seat(g, g.transform.position.y);
        return g;
    }

    // ---------------------------------------------------------------- glass

    // The entrance fans (the Unbatched renderers): centre, side (-1 outside, +1 inside),
    // radius, shard count and seed.
    private struct Fan
    {
        public string Name;
        public Vector3 Centre;
        public float Side, Radius;
        public int Count, Seed;
    }

    private static readonly Fan[] Fans =
    {
        new Fan { Name = "EntranceGlass_Outside", Centre = V(0.1f, 0, -0.12f), Side = -1, Radius = 2.5f, Count = 70, Seed = 41 },
        new Fan { Name = "EntranceGlass_Inside", Centre = V(0.1f, 0, 0.12f), Side = 1, Radius = 1.5f, Count = 50, Seed = 43 },
    };

    // Fan shape, shared with GlassZones: shard centres from FanInner out to FanInner +
    // radius, within FanArc degrees of the fan's axis and at least FanSill off the
    // threshold line (z 0); the largest shard reaches ShardLargest x ShardSpread from its centre.
    private const float FanInner = 0.08f, FanArc = 75, FanSill = 0.16f, ShardLargest = 0.07f, ShardSpread = 1.3f;

    // Small flat shards in a fan from the threshold, denser near the doors, kept off the
    // sill line and the bollards. One mesh per fan, all with their bases GlassLift above
    // the floor or pavement (y 0).
    private static void GlassFan(Transform parent, Fan fan, string folder)
    {
        KitMesh k = new KitMesh { Lightmapped = false };
        System.Random rng = new System.Random(fan.Seed);
        float side = fan.Side, radius = fan.Radius;
        for (int made = 0, guard = 0; made < fan.Count && guard < fan.Count * 20; guard++)
        {
            float u = (float)rng.NextDouble(), r = FanInner + radius * u * u, phi = Range(rng, -FanArc, FanArc) * Mathf.Deg2Rad;
            Vector3 c = fan.Centre + V(Mathf.Sin(phi), 0, side * Mathf.Cos(phi)) * r;
            if (side * c.z < FanSill || NearBollard(c)) continue;
            float size = Mathf.Lerp(ShardLargest, 0.025f, r / radius) * Range(rng, 0.6f, ShardSpread), a0 = Range(rng, 0, Mathf.PI * 2);
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
        k.Emit(fan.Name, parent, Vector3.zero, Quaternion.identity, folder, "night_" + fan.Name.ToLowerInvariant());
    }

    private static bool NearBollard(Vector3 p)
    {
        foreach (float x in NightStoreLayout.BollardX)
            if (new Vector2(p.x - x, p.z - NightStoreLayout.BollardZ).sqrMagnitude < 0.16f * 0.16f) return true;
        return false;
    }

    // Where loose glass lies, as world x/z rects (Rect x = world x, y = world z, as
    // NightStoreLayout.RegionRect; the store root sits at the origin): each entrance fan's
    // envelope (its arc out to the full radius, from the sill line, widened by the largest
    // shard plus 1 cm, so it follows a retuned fan) and a 2.0 x 0.9 m strip in front of
    // each broken case, from 0.3 m off its centre line, holding the floor fragments
    // StoreKit's case spills 0.3 m out from its front.
    public static Rect[] GlassZones()
    {
        List<Rect> zones = new List<Rect>();
        float shard = ShardLargest * ShardSpread + 0.01f;
        foreach (Fan fan in Fans)
        {
            float reach = FanInner + fan.Radius, halfX = reach * Mathf.Sin(FanArc * Mathf.Deg2Rad) + shard;
            float near = fan.Side * (FanSill - shard), far = fan.Centre.z + fan.Side * (reach + shard);
            zones.Add(Rect.MinMaxRect(fan.Centre.x - halfX, Mathf.Min(near, far), fan.Centre.x + halfX, Mathf.Max(near, far)));
        }
        foreach (NightStoreLayout.Fixture f in NightStoreLayout.Fixtures)
        {
            if ((f.Kit != "display_case" && f.Kit != "wall_case") || CaseGlass(f.Name) == StoreKit.CaseGlass.Intact) continue;
            Quaternion turn = Quaternion.Euler(0, f.Yaw, 0);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 corner in new[] { V(-1, 0, -0.3f), V(1, 0, -0.3f), V(-1, 0, -1.2f), V(1, 0, -1.2f) })
            {
                Vector3 p = f.Position + turn * corner;
                min = Vector2.Min(min, new Vector2(p.x, p.z));
                max = Vector2.Max(max, new Vector2(p.x, p.z));
            }
            zones.Add(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
        }
        return zones.ToArray();
    }

    // ---------------------------------------------------------------- combining

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
    // the scatter); after the static flags are set, each case's furniture and each loose
    // cell become one mesh per material. They stay probe-lit, out of GI like the small
    // props they were, and batch statically. Returns the renderers left in those groups.
    // The Stock group is left alone: its pieces keep their LOD levels.
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
