#if UNITY_EDITOR
using UnityEngine;
using static DemoGeometry;
using L = NightStoreLayout;

// Street furniture, the street-side figures and the across-street layout of the night
// store's exterior, from the realism packs (StreetModels, ForensicModels, PeopleModels)
// through DemoAssetLibrary.PlacePack, so every level of a model shares one material per
// surface and its CC BY / MIT credit is recorded. Every model is optional: a missing one
// is skipped (the bollards fall back to StoreKit's procedural bollard), so a project
// unpacked from the ZIP builds today's street. The one procedural street item is the row
// of bare trees on the far pavement (Trees), built with or without the packs. Nothing here
// picks a country: no road signs, hydrants or name plates (Koda's decision #1), and the
// trees name no species and are not pollarded; the only police item, the guard figure,
// comes from PoliceLook.Active and is absent by default.
//
// Props whose footprint reaches a movement region get one bounds collider (the walker,
// teleport arcs and tool placement stop on them); the rest stand outside the play area
// and need none. Figures stand at least 2.5 m outside it, where nobody walks up to them.
public static class NightStreet
{
    public const string StreetPack = "StreetModels", ForensicPack = "ForensicModels", PeoplePack = PoliceLook.FigurePack;
    public const string GroupName = "Street", PeopleGroup = "People", TreeGroup = "Trees";
    public const string Apartment = "ApartmentBlock", ApartmentPath = "Exterior/Buildings/" + Apartment;

    // Exterior/Trees/Tree_i: 1.2 m behind the far kerb (FarKerbZ -14) and midway between the
    // far probe row's probes (every 4 m at z -15.5), so a probe never sits in a crown. With a
    // crown reach of at most 1.29 m they stay over 3 m from the far lanterns (x -6 and 7),
    // 1.2 m from the bus shelter (x -11.45..-7.55), clear of the cycle stands (x -6.6 and
    // -5.6) and off the facades, the posters' wall included.
    public static readonly Vector3[] TreePositions = { V(-14.0f, 0, -15.2f), V(2.0f, 0, -15.2f), V(18.0f, 0, -15.2f) };

    // ApartmentBlock's OBJ spans x -0.355..15.355 with its facade plane at z 0 facing +z.
    // Unity mirrors x on import, so a root at x0 + PivotX puts the block on x0..x0 + Width.
    public const float ApartmentX0 = -3.8f, ApartmentWidth = 15.71f, ApartmentPivotX = 15.355f;

    // The row across the street: slot i spans x Edges[2i]..Edges[2i + 1] at Heights[i]; the
    // Blank slot has a plain ground floor (no shopfront) for the street posters. With the
    // apartment block its slot (ApartmentX0..ApartmentX0 + ApartmentWidth) has no box.
    public struct Across
    {
        public float[] Edges, Heights;
        public int Blank;
        public float ApartmentX0;
        public bool Apartment;
    }

    public static Across AcrossLayout(bool apartment)
    {
        if (!apartment)
            return new Across
            {
                Edges = new[] { -26, -15.2f, -14.8f, -4.2f, -3.8f, 7.2f, 7.6f, 18.2f, 18.6f, 28 },
                Heights = new[] { 9, 12, 10.5f, 13.5f, 9 },
                Blank = 3
            };
        // 0.4 m gaps either side of the apartment block, as between the boxes.
        return new Across
        {
            Edges = new[] { -26, -15.2f, -14.8f, -4.2f, ApartmentX0 + ApartmentWidth + 0.4f, 18.2f, 18.6f, 28 },
            Heights = new[] { 9, 12, 13.5f, 9 },
            Blank = 2, ApartmentX0 = ApartmentX0, Apartment = true
        };
    }

    // The builder places the apartment block only with imported assets and the model present.
    public static bool HasApartment(bool imported) => imported && DemoAssetLibrary.Has(StreetPack, Apartment);

    // Where the street posters go: the middle of the Blank slot's ground floor, 12 mm proud
    // of its facade (the blank wall has no shopfront faces, so the poster is the outermost
    // layer), facing the street (+z).
    public static void PosterWall(bool apartment, out Vector3 centre, out Vector3 normal)
    {
        Across a = AcrossLayout(apartment);
        centre = V((a.Edges[2 * a.Blank] + a.Edges[2 * a.Blank + 1]) * 0.5f, 0.95f, L.FarPavementZ + 0.012f);
        normal = Vector3.forward;
    }

    // The same for a built night store (whether its build placed the apartment block).
    public static void PosterWall(Transform root, out Vector3 centre, out Vector3 normal)
        => PosterWall(root != null && root.Find(ApartmentPath) != null, out centre, out normal);

    // Bollard_i of the anti-ram row: StreetModels' anthracite bollard, which keeps its
    // shadows in the headlight beams, or StoreKit's procedural one (its own collider).
    public static GameObject Bollard(Transform parent, int i, bool imported = true)
    {
        Vector3 p = V(L.BollardX[i], 0, L.BollardZ);
        GameObject g = imported ? DemoAssetLibrary.PlacePack(StreetPack, "Bollard", parent, p, 0, new DemoAssetLibrary.PackOptions { Shadows = true }) : null;
        if (g != null) DemoAssetLibrary.AddBoundsCollider(g);
        else g = StoreKit.Instantiate("bollard", parent, p, 0);
        g.name = "Bollard_" + i;
        return g;
    }

    // One placed pack model: its object name, where it stands (Position.y is the surface it
    // rests on), its LODGroup class and whether it casts shadows.
    public struct Item
    {
        public string Pack, Model, Name;
        public Vector3 Position;
        public float Yaw;
        public DemoAssetLibrary.LodClass Lod;
        public bool Shadows;

        public Item(string pack, string model, string name, Vector3 position, float yaw, DemoAssetLibrary.LodClass lod, bool shadows)
        {
            Pack = pack; Model = model; Name = name; Position = position; Yaw = yaw; Lod = lod; Shadows = shadows;
        }
    }

    // Exterior/Street. At yaw 0 the pack models face +z: towards the shop on the near
    // pavement, towards the road across the street.
    public static readonly Item[] Furniture =
    {
        // Gully grates in the wet gutter against the near kerb (12 mm proud of the street),
        // one by the police car; long side along the kerb.
        new Item(StreetPack, "StormDrainGrate", "StormDrainGrate_0", V(2.0f, L.StreetY, -3.25f), 0, DemoAssetLibrary.LodClass.None, false),
        new Item(StreetPack, "StormDrainGrate", "StormDrainGrate_1", V(-6.5f, L.StreetY, -3.25f), 0, DemoAssetLibrary.LodClass.None, false),
        // Electrical cabinet against the left neighbour, its door to the street.
        new Item(StreetPack, "UtilityBox", "UtilityBox", V(-15.2f, 0, -0.3f), 180, DemoAssetLibrary.LodClass.Prop, true),
        // Across the street: the bus shelter (open side to the road) and two cycle stands
        // beside it, side by side 1 m apart and square to the kerb, so bikes park across the
        // pavement (end to end at 1 m the bikes would overlap); their legs sit 0.12 m in the
        // paving, as the pack models them.
        new Item(StreetPack, "BusShelter", "BusShelter", V(-9.5f, 0, -15.9f), 0, DemoAssetLibrary.LodClass.None, true),
        new Item(StreetPack, "BikeRack", "BikeRack_0", V(-6.6f, 0, -15.6f), 90, DemoAssetLibrary.LodClass.None, false),
        new Item(StreetPack, "BikeRack", "BikeRack_1", V(-5.6f, 0, -15.6f), 90, DemoAssetLibrary.LodClass.None, false),
        // Scene kit: the closed forensic case on the pavement beside the table, the glove
        // box on the table top (clear of the rack samples).
        new Item(ForensicPack, "ForensicCase", "ForensicCase", V(6.2f, 0, -1.25f), 15, DemoAssetLibrary.LodClass.Prop, true),
        new Item(ForensicPack, "NitrileGloveBox", "NitrileGloveBox", L.Table + V(0.3f, L.TableTop, 0.2f), 10, DemoAssetLibrary.LodClass.None, false),
    };

    // Exterior/People: a bystander on his phone on the pavement left of the shop (3 m outside
    // the play area). Baked shadows ground the figures in the streetlight pools.
    public static readonly Item[] Figures =
    {
        new Item(PeoplePack, "PasserBy_Phone", "PasserBy_Phone", V(-12.5f, 0, -1.2f), 120, DemoAssetLibrary.LodClass.Large, true),
    };

    // The active police preset's guard figure, when it has one (the default has none).
    public static bool Guard(out Item guard)
    {
        PoliceLook.Preset look = PoliceLook.Active;
        bool any = !string.IsNullOrEmpty(look.GuardFigure);
        guard = any ? new Item(PoliceLook.FigurePack, look.GuardFigure, look.GuardFigure, look.GuardPosition, look.GuardYaw, DemoAssetLibrary.LodClass.Large, true) : default;
        return any;
    }

    // Exterior/Street (furniture and kit by the kerb, at the table and across the street),
    // Exterior/People and Exterior/Trees; the first two exist even when every model is
    // missing, and the procedural trees stand in every build.
    public static void Apply(Transform exterior, bool imported = true)
    {
        Transform street = Group(GroupName, exterior), people = Group(PeopleGroup, exterior);
        Trees(exterior);
        if (!imported) return;
        foreach (Item item in Furniture) Place(item, street);
        foreach (Item item in Figures) Place(item, people);
        if (Guard(out Item guard)) Place(guard, people);
    }

    // One pack model under parent; null (nothing placed) when the model is missing. PlacePack
    // keeps glass and other blended surfaces from casting (DemoAssetLibrary.Blended).
    private static GameObject Place(Item item, Transform parent)
    {
        GameObject g = DemoAssetLibrary.PlacePack(item.Pack, item.Model, parent, item.Position, item.Yaw,
            new DemoAssetLibrary.PackOptions { Lod = item.Lod, Shadows = item.Shadows });
        if (g == null) return null;
        g.name = item.Name;
        if (InPlayArea(g)) DemoAssetLibrary.AddBoundsCollider(g);
        return g;
    }

    // Exterior/Trees: three bare deciduous trees across the street, a late-autumn
    // simplification (the wet leaves in the gutter are theirs; foliage cards are deferred).
    public static void Trees(Transform exterior)
    {
        Transform group = Group(TreeGroup, exterior);
        for (int i = 0; i < TreePositions.Length; i++) Tree(group, i);
    }

    // Tree_i, 592 triangles and about 6.2 m tall: a tapering trunk (54) in a 1 m grate (2),
    // four diagonal limbs (no limb points at a facade) that bend upwards into closed cones
    // (34 each), and five two-segment branches from inside each cone (20 each). A branch
    // stays within 60 degrees of its limb's heading, so the crown reaches at most 1.29 m from
    // the trunk and 1.15 m in z (z about -16.35..-14.05): it never overhangs the road and keeps
    // 0.4 m from the apartment block's projections. The tips stay 2.4 cm across, about 2 px at
    // 12 m, so they do not flicker. Loose (no lightmap UVs): StoreKit.MarkStatic makes it a GI
    // contributor lit by the probes. It casts no shadow (the far pavement bakes at 2 texels per
    // metre, too coarse for branch shadows, which would only smudge the lantern pools), has no
    // collider (outside the play area) and no LODGroup (every budget eye sees it above LOD0 height).
    private static void Tree(Transform parent, int i)
    {
        System.Random rng = new System.Random(41 + i);
        float TreeRange(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        KitMesh k = new KitMesh { Lightmapped = false };
        // Bark: KitConcrete, already in the scene (a mottled light grey; the untextured 0.56 grey without PBRExterior).
        Material bark = StoreKit.Concrete;
        k.Lathe(bark, Vector3.zero, Quaternion.identity, new[] { 0.17f, 0.15f, 0.13f, 0.11f }, new[] { 0f, 0.9f, 2.4f, 3.5f }, 8, true, false, true);
        // The grate, GroundLift (3 mm) proud of the paving like the far-pavement decals.
        const float grate = 0.5f, lift = 0.003f;
        k.Face(StoreKit.FrameMetal, Vector3.up, V(-grate, lift, -grate), V(grate, lift, -grate), V(grate, lift, grate), V(-grate, lift, grate));
        Vector3 a0 = V(0, 3.35f, 0);
        for (int j = 0; j < 4; j++)
        {
            float yaw = 45 + 90 * j + TreeRange(-10, 10), tilt = TreeRange(30, 40);
            Vector3 dir = Quaternion.Euler(0, yaw, 0) * (Quaternion.Euler(tilt, 0, 0) * Vector3.up);
            Vector3 a1 = a0 + dir * 0.8f;
            // The elbow (14-18 degrees) is closed: the limb runs on 3 cm past a1 into the cone (same
            // taper), which shuts the wedge of up to 23 mm on its outer side, and the cone's base is
            // capped (+4), where its hexagon's corners stand out of the limb's flats (the two
            // hexagons are turned differently). Open, both showed the sky or a lit window through.
            Branch(k, bark, a0, a1, new[] { 0.09f, 0.0744f }, new[] { 0f, 0.83f }, 6);
            Vector3 dir2 = Vector3.Slerp(dir, Vector3.up, 0.45f).normalized, a2 = a1 + dir2 * 0.7f;
            k.Lathe(bark, a1, Quaternion.FromToRotation(Vector3.up, dir2), new[] { 0.075f, 0.06f, 0f }, new[] { 0f, 0.7f, 0.8f }, 6, true, true, false);
            // The branches start inside the cone, 4 cm past its 6 cm ring.
            Vector3 s0 = a2 + dir2 * 0.04f;
            for (int s = 0; s < 5; s++)
            {
                float azimuth = yaw + TreeRange(-60, 60), tiltS = TreeRange(6, 15), length = TreeRange(1.0f, 1.5f);
                Vector3 sdir = Quaternion.Euler(0, azimuth, 0) * (Quaternion.Euler(tiltS, 0, 0) * Vector3.up);
                Vector3 s1 = s0 + sdir * 0.55f * length;
                // 5 mm past s1 for the same reason (the droop bends it up to 10 degrees).
                Branch(k, bark, s0, s1, new[] { 0.025f, 0.018f }, new[] { 0f, 0.55f * length + 0.005f }, 5);
                // The outer half droops slightly towards the horizontal.
                Vector3 sdir2 = Vector3.Slerp(sdir, new Vector3(sdir.x, 0, sdir.z).normalized, 0.12f).normalized;
                Branch(k, bark, s1, s1 + sdir2 * 0.45f * length, new[] { 0.018f, 0.012f }, new[] { 0f, 0.45f * length }, 5);
            }
        }
        GameObject tree = k.Emit("Tree_" + i, parent, TreePositions[i], Quaternion.identity, NightStoreBuilder.Folder, "night_tree");
        // Casts in the bake, so the trees stand on the wet street instead of floating under the lanterns.
        tree.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    // An open smooth lathe from a towards b (radii at heights along it; b only sets the direction).
    private static void Branch(KitMesh k, Material m, Vector3 a, Vector3 b, float[] radii, float[] heights, int sides)
        => k.Lathe(m, a, Quaternion.FromToRotation(Vector3.up, b - a), radii, heights, sides, true, false, false);

    // Whether the prop's footprint (its renderers' x/z extent) touches a movement region.
    // The store root sits at the world origin, as the builder always places it.
    public static bool InPlayArea(GameObject g)
    {
        if (!DemoAssetLibrary.WorldBounds(g, out Bounds b)) return false;
        foreach (InteriorBounds.Region region in L.Regions())
            if (b.max.x >= region.MinX && b.min.x <= region.MaxX && b.max.z >= region.MinZ && b.min.z <= region.MaxZ) return true;
        return false;
    }
}
#endif
