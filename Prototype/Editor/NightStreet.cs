#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;
using L = NightStoreLayout;

// Street furniture, the street-side figures and the across-street layout of the night
// store's exterior, from the realism packs (StreetModels, ForensicModels, PeopleModels)
// through DemoAssetLibrary.PlacePack, so every level of a model shares one material per
// surface and its CC BY / MIT credit is recorded. Every model is optional: a missing one
// is skipped (the bollards fall back to StoreKit's procedural bollard), so a project
// unpacked from the ZIP builds today's street. Nothing here picks a country: no road
// signs, hydrants or name plates (Koda's decision #1); the only police item, the guard
// figure, comes from PoliceLook.Active and is absent by default.
//
// Props whose footprint reaches a movement region get one bounds collider (the walker,
// teleport arcs and tool placement stop on them); the rest stand outside the play area
// and need none. Figures stand at least 2.5 m outside it, where nobody walks up to them.
public static class NightStreet
{
    public const string StreetPack = "StreetModels", ForensicPack = "ForensicModels", PeoplePack = PoliceLook.FigurePack;
    public const string GroupName = "Street", PeopleGroup = "People";
    public const string Apartment = "ApartmentBlock", ApartmentPath = "Exterior/Buildings/" + Apartment;

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

    // Exterior/Street (furniture and kit by the kerb, at the table and across the street)
    // and Exterior/People; both groups exist even when every model is missing.
    public static void Apply(Transform exterior, bool imported = true)
    {
        Transform street = Group(GroupName, exterior), people = Group(PeopleGroup, exterior);
        if (!imported) return;
        foreach (Item item in Furniture) Place(item, street);
        foreach (Item item in Figures) Place(item, people);
        if (Guard(out Item guard)) Place(guard, people);
    }

    // One pack model under parent; null (nothing placed) when the model is missing.
    private static GameObject Place(Item item, Transform parent)
    {
        GameObject g = DemoAssetLibrary.PlacePack(item.Pack, item.Model, parent, item.Position, item.Yaw,
            new DemoAssetLibrary.PackOptions { Lod = item.Lod, Shadows = item.Shadows });
        if (g == null) return null;
        g.name = item.Name;
        NoTransparentShadows(g);
        if (InPlayArea(g)) DemoAssetLibrary.AddBoundsCollider(g);
        return g;
    }

    // Glass and other blended surfaces cast no shadows (the Standard shader would dither them).
    public static void NoTransparentShadows(GameObject g)
    {
        foreach (MeshRenderer r in g.GetComponentsInChildren<MeshRenderer>(true))
            if (r.sharedMaterial != null && r.sharedMaterial.renderQueue >= 3000) r.shadowCastingMode = ShadowCastingMode.Off;
    }

    // Whether the prop's footprint (its renderers' x/z extent) touches a movement region.
    // The store root sits at the world origin, as the builder always places it.
    public static bool InPlayArea(GameObject g)
    {
        bool first = true;
        Bounds b = new Bounds();
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
        {
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        if (first) return false;
        foreach (InteriorBounds.Region region in L.Regions())
            if (b.max.x >= region.MinX && b.min.x <= region.MaxX && b.max.z >= region.MinZ && b.min.z <= region.MaxZ) return true;
        return false;
    }
}
#endif
