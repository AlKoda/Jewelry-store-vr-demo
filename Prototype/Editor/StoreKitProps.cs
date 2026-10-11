#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using static DemoGeometry;

// Small jewelry props for the night store, in the StoreKit conventions: pivot at the
// bottom centre, front towards -z, metric UVs, one renderer per rigid part and a
// user-model override through StoreKit.Instantiate. Display pieces that stay on the
// decks (bust, stands, ring tray, risers) are lightmapped; loose pieces the robbery
// dressing scatters (boxes, trays, bags, dropped rings) and the tiny jewelry on the
// displays ("Chain", "Rings") have no lightmap UVs and are lit by light probes.
public static partial class StoreKit
{
    public enum Riser { Small, Medium, Tall }
    public enum JewelryBoxKind { Ring, Bracelet, Necklace }

    public static readonly string[] PropNames =
    {
        "necklace_bust", "bracelet_stand", "earring_stand", "ring_tray", "display_riser",
        "jewelry_box", "transport_tray", "shopping_bag", "dropped_ring", "dropped_bangle"
    };

    public static readonly string[] PropVariants =
    {
        "necklace_bust__Bare", "ring_tray__Stocked", "display_riser__Small", "display_riser__Tall",
        "jewelry_box__Bracelet", "jewelry_box__Necklace", "jewelry_box__Closed", "shopping_bag__Tipped"
    };

    // Props without lightmap UVs at all (their variants share the base name).
    public static readonly string[] LooseProps = { "jewelry_box", "transport_tray", "shopping_bag", "dropped_ring", "dropped_bangle" };

    // Parts of lightmapped props that are still probe-lit (thin gold and silver).
    public static readonly string[] ProbeLitParts = { "Chain", "Rings" };

    public static bool IsLoose(string entry) => Array.IndexOf(LooseProps, entry.Split(new[] { "__" }, StringSplitOptions.None)[0]) >= 0;

    private static readonly Dictionary<string, Func<Transform, GameObject>> PropBuilders = new Dictionary<string, Func<Transform, GameObject>>
    {
        { "necklace_bust", p => NecklaceBust(p) }, { "bracelet_stand", BraceletStand }, { "earring_stand", EarringStand },
        { "ring_tray", p => RingTray(p) }, { "display_riser", p => DisplayRiser(p) }, { "jewelry_box", p => JewelryBox(p) },
        { "transport_tray", TransportTray }, { "shopping_bag", p => ShoppingBag(p) }, { "dropped_ring", DroppedRing },
        { "dropped_bangle", DroppedBangle }
    };

    // Loose items stay at or under 200 triangles so the dressing can scatter many.
    private static readonly Dictionary<string, int> PropBudgets = new Dictionary<string, int>
    {
        { "necklace_bust", 400 }, { "necklace_bust__Bare", 400 }, { "bracelet_stand", 150 }, { "earring_stand", 60 },
        { "ring_tray", 200 }, { "ring_tray__Stocked", 600 }, { "display_riser", 40 }, { "display_riser__Small", 40 },
        { "display_riser__Tall", 40 }, { "jewelry_box", 200 }, { "jewelry_box__Bracelet", 200 }, { "jewelry_box__Necklace", 200 },
        { "jewelry_box__Closed", 200 }, { "transport_tray", 60 }, { "shopping_bag", 120 }, { "shopping_bag__Tipped", 120 },
        { "dropped_ring", 200 }, { "dropped_bangle", 200 }
    };

    public static Material Gold { get; private set; }
    public static Material Silver { get; private set; }
    public static Material Leather { get; private set; }
    public static Material Leatherette { get; private set; }
    public static Material Felt { get; private set; }
    public static Material Kraft { get; private set; }
    public static Material Cord { get; private set; }

    private static void PropPalette()
    {
        // Polished metals: almost all their look comes from the baked reflection probes.
        // Yellow gold at its measured reflectance (linear F0 about 1.0, 0.71, 0.29), shared
        // with ShopDisplayFinish's JewelryGold so the cases and the scattered pieces match.
        Gold = Surface("KitGold", new Color(0.98f, 0.84f, 0.55f), 0.8f, 1, null, 1);
        Silver = Surface("KitSilver", new Color(0.82f, 0.82f, 0.84f), 0.8f, 1, null, 1);
        Leather = Surface("KitLeather", new Color(0.16f, 0.14f, 0.13f), 0.35f, 0, null, 1);
        Leatherette = Surface("KitLeatherette", new Color(0.07f, 0.07f, 0.075f), 0.3f, 0, null, 1);
        Felt = Surface("KitFelt", new Color(0.42f, 0.38f, 0.34f), 0.05f, 0, null, 1);
        Kraft = Surface("KitKraft", new Color(0.72f, 0.62f, 0.48f), 0.15f, 0, null, 1);
        Cord = Surface("KitCord", new Color(0.45f, 0.40f, 0.33f), 0.1f, 0, null, 1);
    }

    private static KitMesh Probed() => new KitMesh { Lightmapped = false };

    // ---------------------------------------------------------------- display pieces

    // Charcoal velvet bust (0.18 wide at the shoulders, 0.30 high) on a velvet plinth:
    // a 14-sided elliptical loft from the chest up to a capped neck. The optional chain
    // is a thin gold loop tilted 45 degrees, resting on the neck at the back and dipping
    // over the chest at the front (its sides pass inside the shoulders).
    public static GameObject NecklaceBust(Transform parent, bool chain = true)
    {
        GameObject root = Root("necklace_bust", parent);
        KitMesh k = new KitMesh();
        k.Box(Velvet, V(0, 0.0125f, 0), V(0.15f, 0.025f, 0.10f), false);
        k.Loft(Velvet, V(0, 0.025f, 0.005f), Quaternion.identity,
            new[] { 0.05f, 0.06f, 0.078f, 0.09f, 0.082f, 0.06f, 0.03f, 0.024f, 0.026f, 0.02f },
            new[] { 0.03f, 0.032f, 0.036f, 0.04f, 0.038f, 0.032f, 0.026f, 0.022f, 0.024f, 0.018f },
            new[] { 0f, 0.07f, 0.13f, 0.17f, 0.195f, 0.21f, 0.225f, 0.255f, 0.268f, 0.275f }, 14, false, true);
        Part(k, "Body", root, Vector3.zero, "necklace_bust");
        if (chain)
        {
            KitMesh loop = Probed();
            loop.Torus(Gold, V(0, 0.232f, -0.008f), Quaternion.Euler(-45, 0, 0), 0.045f, 0.05f, 0.0015f, 14, 4);
            Part(loop, "Chain", root, Vector3.zero, "necklace_chain");
        }
        Collide(root, V(0, 0.15f, 0), V(0.18f, 0.30f, 0.10f));
        return root;
    }

    // T-bar bracelet stand: charcoal velvet roll (0.24 m) on a bronze post on a walnut base.
    public static GameObject BraceletStand(Transform parent)
    {
        GameObject root = Root("bracelet_stand", parent);
        KitMesh k = new KitMesh();
        k.Box(Walnut, V(0, 0.011f, 0), V(0.20f, 0.022f, 0.09f), false);
        k.Cylinder(Bronze, V(0, 0.022f, 0), V(0, 0.102f, 0), 0.008f, 10, false);
        k.Cylinder(Velvet, V(-0.12f, 0.13f, 0), V(0.12f, 0.13f, 0), 0.03f, 16);
        Part(k, "Body", root, Vector3.zero, "bracelet_stand");
        Collide(root, V(0, 0.08f, 0), V(0.24f, 0.16f, 0.09f));
        return root;
    }

    // Earring board: charcoal velvet panel leaning back 8 degrees on a walnut wedge
    // (lower at the front), with two small piercings as dark marks.
    public static GameObject EarringStand(Transform parent)
    {
        const float w = 0.11f, d = 0.07f, front = 0.012f, back = 0.022f;
        GameObject root = Root("earring_stand", parent);
        KitMesh k = new KitMesh();
        Vector3 fl = V(-w * 0.5f, 0, -d * 0.5f), fr = V(w * 0.5f, 0, -d * 0.5f), bl = V(-w * 0.5f, 0, d * 0.5f), br = V(w * 0.5f, 0, d * 0.5f);
        Vector3 fh = V(0, front, 0), bh = V(0, back, 0);
        k.Face(Walnut, Vector3.up, fl + fh, fr + fh, br + bh, bl + bh);
        k.Face(Walnut, Vector3.back, fl, fr, fr + fh, fl + fh);
        k.Face(Walnut, Vector3.forward, bl, br, br + bh, bl + bh);
        k.Face(Walnut, Vector3.left, fl, bl, bl + bh, fl + fh);
        k.Face(Walnut, Vector3.right, fr, br, br + bh, fr + fh);
        Quaternion lean = Quaternion.Euler(8, 0, 0);
        Vector3 foot = V(0, (front + back) * 0.5f, 0.005f);
        k.Box(Velvet, foot + lean * V(0, 0.065f, 0), V(0.09f, 0.13f, 0.01f), lean, false);
        for (int s = -1; s <= 1; s += 2)
            k.Face(DeviceBlack, lean * Vector3.back, foot + lean * V(s * 0.02f - 0.0015f, 0.105f, -0.0051f), foot + lean * V(s * 0.02f + 0.0015f, 0.105f, -0.0051f),
                foot + lean * V(s * 0.02f + 0.0015f, 0.108f, -0.0051f), foot + lean * V(s * 0.02f - 0.0015f, 0.108f, -0.0051f));
        Part(k, "Body", root, Vector3.zero, "earring_stand");
        Collide(root, V(0, 0.075f, 0.01f), V(w, 0.15f, d));
        return root;
    }

    // Walnut ring tray (0.30 x 0.22) with four cream suede rolls; stocked seats nine
    // small gold and silver rings upright in the three grooves between the rolls.
    public static GameObject RingTray(Transform parent, bool stocked = false)
    {
        const float w = 0.30f, d = 0.22f, h = 0.045f, t = 0.012f, floor = 0.008f, roll = 0.0235f, pitch = 0.049f;
        GameObject root = Root("ring_tray", parent);
        KitMesh k = new KitMesh();
        k.Box(Walnut, V(0, floor * 0.5f, 0), V(w, floor, d), false);
        for (int s = -1; s <= 1; s += 2)
        {
            k.Box(Walnut, V(0, h * 0.5f, s * (d * 0.5f - t * 0.5f)), V(w, h, t), false);
            k.Box(Walnut, V(s * (w * 0.5f - t * 0.5f), h * 0.5f, 0), V(t, h, d - 2 * t), false);
        }
        // Open-ended rolls: their ends meet the side walls.
        for (int i = 0; i < 4; i++)
        {
            float z = (i - 1.5f) * pitch;
            k.Cylinder(Suede, V(-(w * 0.5f - t), floor + roll, z), V(w * 0.5f - t, floor + roll, z), roll, 12, false);
        }
        Part(k, "Body", root, Vector3.zero, "ring_tray");
        if (stocked)
        {
            KitMesh rings = Probed();
            Quaternion upright = Quaternion.Euler(90, 0, 0); // Ring plane xy, axis across the groove.
            for (int g = 0; g < 3; g++)
                for (int i = 0; i < 3; i++)
                    rings.Torus((g * 3 + i) % 2 == 0 ? Gold : Silver, V(-0.09f + i * 0.09f + (g - 1) * 0.025f, 0.05f, (g - 1) * pitch),
                        upright, 0.0095f, 0.0095f, 0.0016f, 8, 3);
            Part(rings, "Rings", root, Vector3.zero, "ring_tray_rings");
        }
        Collide(root, V(0, h * 0.5f, 0), V(w, h, d));
        return root;
    }

    // Cream suede block on a walnut base band; 9 cm square, 8, 12 or 18 cm high.
    public static GameObject DisplayRiser(Transform parent, Riser size = Riser.Medium)
    {
        const float w = 0.09f, band = 0.012f;
        float h = size == Riser.Small ? 0.08f : size == Riser.Medium ? 0.12f : 0.18f;
        GameObject root = Root("display_riser", parent);
        KitMesh k = new KitMesh();
        k.Box(Walnut, V(0, band * 0.5f, 0), V(w, band, w), false);
        k.Box(Suede, V(0, (band + h) * 0.5f, 0), V(w, h - band, w), false);
        Part(k, "Body", root, Vector3.zero, "display_riser_" + size);
        Collide(root, V(0, h * 0.5f, 0), V(w, h, w));
        return root;
    }

    // ---------------------------------------------------------------- loose pieces

    // Jewelry box in charcoal leather with a bronze trim line at the rim and a cream
    // insert (two slotted rolls for a ring, holding tabs for a bracelet or necklace).
    // "Lid" is a child pivoted on the rear top edge of the base: local x is the hinge
    // axis, the lid extends towards -z, and openAngle (degrees, 0 closed) swings it up.
    public static GameObject JewelryBox(Transform parent, JewelryBoxKind kind = JewelryBoxKind.Ring, float openAngle = 100)
    {
        const float t = 0.004f, trim = 0.002f;
        float w = kind == JewelryBoxKind.Ring ? 0.06f : kind == JewelryBoxKind.Bracelet ? 0.22f : 0.17f;
        float d = kind == JewelryBoxKind.Ring ? 0.06f : kind == JewelryBoxKind.Bracelet ? 0.065f : 0.17f;
        float baseH = kind == JewelryBoxKind.Ring ? 0.032f : 0.028f, lidH = kind == JewelryBoxKind.Ring ? 0.022f : 0.019f;
        float well = kind == JewelryBoxKind.Ring ? 0.012f : 0.008f, insert = baseH - well;
        GameObject root = Root("jewelry_box", parent);
        KitMesh k = Probed();
        Tray(k, Vector3.zero, w, d, baseH, t, insert, trim, true, Leather, Bronze, Suede, Suede);
        if (kind == JewelryBoxKind.Ring)
            for (int s = -1; s <= 1; s += 2)
                k.ChamferBox(Suede, V(0, insert + 0.0055f, s * 0.0128f), V(w - 2 * t - 0.001f, 0.011f, 0.025f), 0.005f, Quaternion.identity);
        else if (kind == JewelryBoxKind.Bracelet)
            for (int s = -1; s <= 1; s += 2)
                k.Box(Suede, V(s * (w * 0.5f - 0.035f), insert + 0.004f, 0), V(0.012f, 0.008f, 0.03f), false);
        else
            for (int s = -1; s <= 1; s += 2)
                k.Box(Suede, V(s * 0.03f, insert + 0.003f, d * 0.5f - 0.045f), V(0.008f, 0.006f, 0.022f), Quaternion.Euler(0, s * 20, 0), false);
        Part(k, "Body", root, Vector3.zero, "jewelry_box_" + kind);
        KitMesh lid = Probed();
        Tray(lid, V(0, 0, -d * 0.5f), w, d, lidH, t, 0.003f, trim, false, Leather, Bronze, Suede, Suede);
        lid.Emit("Lid", root.transform, V(0, baseH, d * 0.5f), Quaternion.Euler(openAngle, 0, 0), Folder, "jewelry_box_lid_" + kind);
        float high = openAngle == 0 ? baseH + lidH : baseH;
        Collide(root, V(0, high * 0.5f, 0), V(w, high, d));
        return root;
    }

    // Charcoal leatherette transport tray (0.40 x 0.30 x 0.05) with a taupe felt floor.
    public static GameObject TransportTray(Transform parent)
    {
        GameObject root = Root("transport_tray", parent);
        KitMesh k = Probed();
        Tray(k, Vector3.zero, 0.40f, 0.30f, 0.05f, 0.012f, 0.008f, 0, true, Leatherette, Leatherette, Leatherette, Felt);
        Part(k, "Body", root, Vector3.zero, "transport_tray");
        Collide(root, V(0, 0.025f, 0), V(0.40f, 0.05f, 0.30f));
        return root;
    }

    // Kraft paper carrier bag (0.22 x 0.10 x 0.28) with flat fabric handles on its front
    // and back. Tipped lies on its front face with the opening towards -z.
    public static GameObject ShoppingBag(Transform parent, bool tipped = false)
    {
        const float w = 0.22f, d = 0.10f, h = 0.28f;
        GameObject root = Root("shopping_bag", parent);
        KitMesh k = Probed();
        Tray(k, Vector3.zero, w, d, h, 0.0015f, 0.002f, 0, true, Kraft, Kraft, Kraft, Kraft);
        for (int s = -1; s <= 1; s += 2)
        {
            float z = s * (d * 0.5f + 0.002f);
            List<Vector3> path = new List<Vector3> { V(-0.045f, h - 0.015f, z) };
            for (int i = 0; i <= 5; i++)
            {
                float a = Mathf.PI * (1 - i / 5f);
                path.Add(V(Mathf.Cos(a) * 0.045f, h + Mathf.Sin(a) * 0.075f, z));
            }
            path.Add(V(0.045f, h - 0.015f, z));
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 side = Vector3.Cross(Vector3.forward, path[i + 1] - path[i]).normalized * 0.006f;
                k.TwoSided(Cord, path[i] - side, path[i + 1] - side, path[i + 1] + side, path[i] + side);
            }
        }
        // Rotated -90 about x: up becomes -z (opening forward), the front face lands on the floor.
        k.Emit("Bag", root.transform, tipped ? V(0, d * 0.5f + 0.003f, h * 0.5f) : Vector3.zero,
            tipped ? Quaternion.Euler(-90, 0, 0) : Quaternion.identity, Folder, "shopping_bag");
        DemoAssetLibrary.AddBoundsCollider(root);
        return root;
    }

    // Plain 2 cm gold band lying flat (12 x 6 facets).
    public static GameObject DroppedRing(Transform parent)
    {
        GameObject root = Root("dropped_ring", parent);
        KitMesh k = Probed();
        k.Torus(Gold, V(0, 0.0018f, 0), Quaternion.identity, 0.0095f, 0.0095f, 0.0018f, 12, 6);
        Part(k, "Body", root, Vector3.zero, "dropped_ring");
        Collide(root, V(0, 0.0018f, 0), V(0.0226f, 0.0036f, 0.0226f));
        return root;
    }

    // 7 cm gold bangle lying flat (24 x 4 facets).
    public static GameObject DroppedBangle(Transform parent)
    {
        GameObject root = Root("dropped_bangle", parent);
        KitMesh k = Probed();
        k.Torus(Gold, V(0, 0.0028f, 0), Quaternion.identity, 0.034f, 0.034f, 0.0028f, 24, 4);
        Part(k, "Body", root, Vector3.zero, "dropped_bangle");
        Collide(root, V(0, 0.0028f, 0), V(0.0736f, 0.0056f, 0.0736f));
        return root;
    }

    // Open box shell w x d x h with walls t thick: the closed end, the four outer sides
    // (their last `trim` metres before the opening in the rim material, a trim line),
    // the rim, the inner walls and a floor `floorAt` from the closed end. up opens
    // upwards (bases, trays, bags) from centre.y; otherwise downwards (lids), with the
    // closed end at centre.y + h. 28 triangles, 36 with a trim line.
    private static void Tray(KitMesh k, Vector3 centre, float w, float d, float h, float t, float floorAt, float trim, bool up,
        Material outer, Material rim, Material inner, Material floor)
    {
        float s = up ? 1 : -1;
        float yc = up ? centre.y : centre.y + h, yo = up ? centre.y + h : centre.y, yt = yo - s * trim, yf = yc + s * floorAt;
        float ox = w * 0.5f, oz = d * 0.5f, ix = ox - t, iz = oz - t;
        Vector3 c = V(centre.x, 0, centre.z), open = Vector3.up * s;
        k.Face(outer, -open, c + V(-ox, yc, -oz), c + V(ox, yc, -oz), c + V(ox, yc, oz), c + V(-ox, yc, oz));
        Walls(k, outer, c, ox, oz, yc, yt, false);
        Walls(k, rim, c, ox, oz, yt, yo, false);
        Walls(k, inner, c, ix, iz, yf, yo, true);
        k.Face(rim, open, c + V(-ox, yo, -oz), c + V(ox, yo, -oz), c + V(ix, yo, -iz), c + V(-ix, yo, -iz));
        k.Face(rim, open, c + V(-ox, yo, oz), c + V(ox, yo, oz), c + V(ix, yo, iz), c + V(-ix, yo, iz));
        k.Face(rim, open, c + V(-ox, yo, -oz), c + V(-ix, yo, -iz), c + V(-ix, yo, iz), c + V(-ox, yo, oz));
        k.Face(rim, open, c + V(ox, yo, -oz), c + V(ix, yo, -iz), c + V(ix, yo, iz), c + V(ox, yo, oz));
        k.Face(floor, open, c + V(-ix, yf, -iz), c + V(ix, yf, -iz), c + V(ix, yf, iz), c + V(-ix, yf, iz));
    }

    // Four vertical walls of a hx by hz rectangle between y0 and y1, facing out or in.
    private static void Walls(KitMesh k, Material m, Vector3 c, float hx, float hz, float y0, float y1, bool inward)
    {
        if (Mathf.Abs(y1 - y0) < 1e-5f) return;
        float f = inward ? -1 : 1;
        k.Face(m, Vector3.back * f, c + V(-hx, y0, -hz), c + V(hx, y0, -hz), c + V(hx, y1, -hz), c + V(-hx, y1, -hz));
        k.Face(m, Vector3.forward * f, c + V(-hx, y0, hz), c + V(hx, y0, hz), c + V(hx, y1, hz), c + V(-hx, y1, hz));
        k.Face(m, Vector3.left * f, c + V(-hx, y0, -hz), c + V(-hx, y0, hz), c + V(-hx, y1, hz), c + V(-hx, y1, -hz));
        k.Face(m, Vector3.right * f, c + V(hx, y0, -hz), c + V(hx, y0, hz), c + V(hx, y1, hz), c + V(hx, y1, -hz));
    }

    // ---------------------------------------------------------------- gallery

    // Every prop and prop variant in two rows of nine in front of the fixture gallery
    // (z -3 and -3.7), under "StoreKitProps", always procedural.
    public static Transform BuildPropGallery(Transform parent)
    {
        Palette();
        Transform gallery = Group("StoreKitProps", parent);
        List<string> entries = new List<string>(PropNames);
        entries.AddRange(PropVariants);
        for (int i = 0; i < entries.Count; i++)
        {
            Vector3 slot = V((i % 9 - 4) * 0.5f, 0, -3.0f - (i / 9) * 0.7f);
            GameObject g = BuildPropEntry(entries[i], gallery);
            g.name = entries[i];
            g.transform.localPosition = slot;
            TextMesh label = Text("Label_" + entries[i], gallery, entries[i].Replace("__", " "), slot + V(0, 0.002f, -0.22f), 0, 0.005f, Color.white);
            label.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
        return gallery;
    }

    private static GameObject BuildPropEntry(string entry, Transform parent)
    {
        switch (entry)
        {
            case "necklace_bust__Bare": return NecklaceBust(parent, false);
            case "ring_tray__Stocked": return RingTray(parent, true);
            case "display_riser__Small": return DisplayRiser(parent, Riser.Small);
            case "display_riser__Tall": return DisplayRiser(parent, Riser.Tall);
            case "jewelry_box__Bracelet": return JewelryBox(parent, JewelryBoxKind.Bracelet);
            case "jewelry_box__Necklace": return JewelryBox(parent, JewelryBoxKind.Necklace);
            case "jewelry_box__Closed": return JewelryBox(parent, JewelryBoxKind.Ring, 0);
            case "shopping_bag__Tipped": return ShoppingBag(parent, true);
            default: return Build(entry, parent);
        }
    }
}
#endif
