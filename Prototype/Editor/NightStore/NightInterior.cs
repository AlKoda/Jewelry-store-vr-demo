#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;
using L = NightStoreLayout;

// Interior props and wall graphics of the night store, placed after the StoreKit fixtures
// and the staged signs and before the static flags, so they bake with their zones.
// InteriorModels through DemoAssetLibrary.PlacePack (one material per surface, the finish
// table, LOD levels): snake plants in the three planters, the rug under the seating corner,
// the monitor and keyboard on the office desk, the extinguisher and clock on the rear
// partition and four air diffusers on the showroom ceiling. WallGraphics' two atlas pages,
// three KitMeshes in all: the framed paintings, the Aurel & Finch lightbox, the fire plan,
// the door stickers and the CCTV sign (Showroom), the staff notices (Office), and the
// graffiti, fly posters and lost-cat flyer (Exterior/Street). Every model and page is
// optional: without it the StoreKit fixture (planter, sign frame) stays and nothing else is
// placed, so a project unpacked from the ZIP builds today's interior. Nothing here is
// police-specific (the police notice in WallGraphics is not placed).
public static class NightInterior
{
    public const string InteriorPack = "InteriorModels", GraphicsPack = "WallGraphics";

    // ---------------------------------------------------------------- props

    // Root names (the pack model names, the planters keep their fixture names). The pack
    // models face +z at yaw 0 (see NightStreet); floor models pivot at their lowest point,
    // wall models at their back, ceiling models at their top (InteriorModels README).
    public const string PlantModel = "SnakePlant", RugName = "Rug", MonitorName = "ComputerMonitor", KeyboardName = "Keyboard",
        ExtinguisherName = "FireExtinguisher", ClockName = "WallClock", DiffuserModel = "CeilingDiffuser", DiffuserPrefix = "CeilingDiffuser_";
    public static readonly string[] Planters = { "Planter_Front", "Planter_RearLeft", "Planter_Office" };

    // The rear partition's two faces.
    public const float ShowroomFace = L.RearZ - L.Partition * 0.5f, OfficeFace = L.RearZ + L.Partition * 0.5f;

    // Seating corner: the 2.0 x 1.4 m rug under the sofa and side table, 2 mm above the floor.
    public static readonly Vector3 RugPosition = V(6.2f, 0.002f, 1.2f);

    // Office desk (StoreKit office_desk, top at 0.75, turned so its chair side is +z): the
    // monitor at the back with its screen to the chair, the keyboard in front of it. The
    // dressing's transport tray on the desk has to stay clear of both.
    public const float DeskTop = 0.75f;
    public static readonly Vector3 MonitorPosition = V(-1.85f, DeskTop, 9.2f), KeyboardPosition = V(-1.85f, DeskTop, 9.5f);

    // On the rear partition's showroom face, turned to the shop (yaw 180): the extinguisher
    // on the floor right of the office opening, and the clock over the opening on the lintel,
    // its back 1 mm off it. The back edge of the extinguisher's floor stand (y 0-0.115) sits
    // ExtinguisherGap off the walnut skirting (NightStoreBuilder: 15 mm proud of the face,
    // 0.1 m high), so it ends at ExtinguisherBack, not inside the skirting.
    public const float ExtinguisherGap = 0.006f, SkirtingDepth = 0.015f, ExtinguisherBack = ShowroomFace - SkirtingDepth - ExtinguisherGap;
    public static readonly Vector3 ExtinguisherPosition = V(1.05f, 0, 8.235f), ClockPosition = V(0, 2.62f, ShowroomFace - 0.001f);

    // Showroom ceiling diffusers between the downlights (NightStoreLayout.Downlights).
    public static readonly Vector3[] Diffusers = { V(-2.5f, L.CeilingY, 2.5f), V(2.5f, L.CeilingY, 2.5f), V(-2.5f, L.CeilingY, 5.0f), V(2.5f, L.CeilingY, 5.0f) };

    // Paths under the store root of every prop the last Apply placed (for the validators).
    public static readonly List<string> Props = new List<string>();

    // ---------------------------------------------------------------- wall graphics

    // Atlas pages (<name>_Color.png) and their JsonUtility layouts (<name>.json), the gilt
    // moulding, the three materials they add (saved in NightStoreBuilder.Folder) and the
    // meshes' names (WallGraphics under Showroom and Office, StreetGraphics under Exterior/Street).
    public const string InteriorAtlas = "Atlas/NightStoreInterior", StreetAtlas = "Atlas/NightStoreStreet";
    public const string GiltColorFile = "Artwork/FrameMoulding_Gilt_Color.jpg", GiltNormalFile = "Artwork/FrameMoulding_Gilt_Normal.png";
    public const string InteriorMaterialName = "NightWallInterior", StreetMaterialName = "NightWallStreet", GiltMaterialName = "NightGiltFrame";
    public const string GraphicsName = "WallGraphics", StreetGraphicsName = "StreetGraphics", StreetZone = "Exterior/" + NightStreet.GroupName;
    public const string PosterCell = "StreetPosters_Weathered";

    // How a graphic is mounted: a flat sheet, a gilt-framed painting, a lightbox, a clip
    // frame, or a sticker on glass.
    public enum GraphicFrame { None, Gilt, Box, Clip, Sticker }

    // Gilt moulding (README): 0.08 m bars whose face stands 0.03 m off the wall, the pattern
    // repeating every 0.32 m along them; EdgeBand is the strip of the moulding image the
    // sides show. The lightbox face stands 0.06 m proud; clip-frame strips are 1 cm wide, 2 mm
    // over the sheet.
    public const float GiltBar = 0.08f, GiltDepth = 0.03f, GiltRepeat = 0.32f, EdgeBand = 0.04f;
    public const float BoxDepth = 0.06f, ClipStrip = 0.01f, ClipLift = 0.002f;

    // How far the picture stands off its mounting surface by mount (a sheet's placement can
    // say otherwise).
    public static float ProudOf(GraphicFrame frame)
    {
        switch (frame)
        {
            case GraphicFrame.Box: return BoxDepth;
            case GraphicFrame.Clip: return 0.008f;
            case GraphicFrame.Sticker: return 0.001f;
            default: return 0.003f;
        }
    }

    // One placed graphic: its atlas cell, the zone its mesh is under, the picture's centre
    // and facing (store-root space), its size (README width, height from the cell's aspect),
    // the cell's UV rect, its mount and how far the picture stands off its surface.
    public struct PlacedGraphic
    {
        public string Name, Zone;
        public Vector3 Centre, Normal;
        public Vector2 Size;
        public Rect Uv;
        public GraphicFrame Frame;
        public float Proud;
    }

    // What the last Apply placed.
    public static readonly List<PlacedGraphic> Graphics = new List<PlacedGraphic>();

    // A graphic to place: cell, zone, README width, mount, a point on the mounting surface
    // and that surface's outward normal. Proud 0 takes ProudOf(Frame); Replaces names the
    // sign frame whose procedural Body the graphic takes the place of.
    private struct Spec
    {
        public string Cell, Zone, Replaces;
        public float Width, Proud;
        public GraphicFrame Frame;
        public Vector3 Surface, Normal;

        public Spec(string cell, string zone, float width, GraphicFrame frame, Vector3 surface, Vector3 normal)
        {
            Cell = cell; Zone = zone; Width = width; Frame = frame; Surface = surface; Normal = normal; Replaces = null; Proud = 0;
        }
    }

    // The builder's facade measures: the piers' street face (NightStoreBuilder.Facade) and
    // the neighbours' half-closed shutters, 15 mm in front of the terrace facade (Shopfront).
    private const float PierFace = -0.2f, ShutterFace = L.MinZ - 0.015f;

    // ---------------------------------------------------------------- build

    // Props, then the wall graphics; with imported false (no third-party assets) nothing.
    public static void Apply(Transform root, bool imported = true)
    {
        Graphics.Clear();
        Props.Clear();
        if (!imported || root == null) return;
        StoreKit.Palette();
        Transform showroom = root.Find("Showroom"), office = root.Find("Office"), ceiling = root.Find(L.CeilingPath);
        if (showroom != null)
        {
            Plants(showroom);
            Seating(showroom);
            PartitionFittings(showroom);
        }
        if (office != null) Desk(office);
        if (ceiling != null) Ceiling(ceiling);
        InteriorGraphics(root, showroom, office);
        StreetGraphics(root);
    }

    // The three planters become snake plants (shadows on) when the model loads: the
    // procedural Body and the root's box go, one bounds collider replaces them and the root
    // keeps its fixture name. A user-converted planter (no Body) stays as it is.
    private static void Plants(Transform showroom)
    {
        foreach (string name in Planters)
        {
            Transform planter = showroom.Find(name);
            Transform body = planter != null ? planter.Find("Body") : null;
            if (body == null) continue;
            if (Place(PlantModel, PlantModel, planter, Vector3.zero, 0, DemoAssetLibrary.LodClass.None, true, false) == null) return;
            Object.DestroyImmediate(body.gameObject);
            foreach (BoxCollider box in planter.GetComponents<BoxCollider>()) Object.DestroyImmediate(box);
            DemoAssetLibrary.AddBoundsCollider(planter.gameObject);
            Props.Add("Showroom/" + name + "/" + PlantModel);
        }
    }

    // The rug: no shadow and no collider (the walker and the tools pass over it); its second
    // UV set (ThirdPartyModelImport) lightmaps it with the showroom at FixtureScale. The
    // StoreKit sofa and side table stay.
    private static void Seating(Transform showroom)
    {
        if (Place(RugName, RugName, showroom, RugPosition, 0, DemoAssetLibrary.LodClass.None, false, false) != null)
            Props.Add("Showroom/" + RugName);
    }

    // Monitor and keyboard with a collider each, so tools settle on them. The screen glows
    // from the pack's finish table (0.45, GI None): no light and nothing in the bake.
    private static void Desk(Transform office)
    {
        if (Place(MonitorName, MonitorName, office, MonitorPosition, 0, DemoAssetLibrary.LodClass.None, true, true) != null)
            Props.Add("Office/" + MonitorName);
        if (Place(KeyboardName, KeyboardName, office, KeyboardPosition, 0, DemoAssetLibrary.LodClass.None, true, true) != null)
            Props.Add("Office/" + KeyboardName);
    }

    // The extinguisher (Prop LODs, its LOD0 within about 5.7 m) set back to ExtinguisherBack
    // whatever the import measured, with a collider; the wall clock with its LOD1 as the top
    // level (Tiny), which keeps its hands and a 32-sided case, and no collider (2.5 m up).
    // No fire-alarm call point: the pack's is a US pull station.
    private static void PartitionFittings(Transform showroom)
    {
        GameObject extinguisher = Place(ExtinguisherName, ExtinguisherName, showroom, ExtinguisherPosition, 180, DemoAssetLibrary.LodClass.Prop, true, false);
        if (extinguisher != null)
        {
            DemoAssetLibrary.WorldBounds(extinguisher, out Bounds b);
            extinguisher.transform.position += Vector3.forward * (ExtinguisherBack - b.max.z);
            DemoAssetLibrary.AddBoundsCollider(extinguisher);
            Props.Add("Showroom/" + ExtinguisherName);
        }
        if (Place(ClockName, ClockName, showroom, ClockPosition, 180, DemoAssetLibrary.LodClass.Tiny, true, false) != null)
            Props.Add("Showroom/" + ClockName);
    }

    // Diffusers under Architecture/Ceiling, so the overview and the map hide them with the
    // slab and the builder clears their occluder flag; flush fittings, no shadows.
    private static void Ceiling(Transform ceiling)
    {
        for (int i = 0; i < Diffusers.Length; i++)
            if (Place(DiffuserModel, DiffuserPrefix + i, ceiling, Diffusers[i], 0, DemoAssetLibrary.LodClass.None, false, false) != null)
                Props.Add(L.CeilingPath + "/" + DiffuserPrefix + i);
    }

    // An InteriorModels model under parent, renamed; null when the model is missing. PlacePack
    // keeps glass and other blended surfaces from casting; collider adds one bounds box on the root.
    private static GameObject Place(string model, string name, Transform parent, Vector3 position, float yaw, DemoAssetLibrary.LodClass lod, bool shadows, bool collider)
    {
        GameObject g = DemoAssetLibrary.PlacePack(InteriorPack, model, parent, position, yaw, new DemoAssetLibrary.PackOptions { Lod = lod, Shadows = shadows });
        if (g == null) return null;
        g.name = name;
        if (collider) DemoAssetLibrary.AddBoundsCollider(g);
        return g;
    }

    // ---------------------------------------------------------------- wall graphics

    // The interior page's graphics, in store-root coordinates (the zones sit at its origin).
    private static List<Spec> InteriorSpecs(Transform root)
    {
        List<Spec> specs = new List<Spec>
        {
            // Rear partition, showroom face: the still life in place of the rear sign frame,
            // the shop's lightbox (unlit: the shop is shut) over the counter, the fire plan
            // right of the office opening.
            new Spec("Art_Heda_BreakfastWithLobster", "Showroom", 0.80f, GraphicFrame.Gilt, V(-5.5f, 2.3f, ShowroomFace), Vector3.back) { Replaces = "Sign_Rear" },
            new Spec("Lightbox_AurelFinchWide", "Showroom", 2.00f, GraphicFrame.Box, V(5.5f, 2.25f, ShowroomFace), Vector3.back) { Replaces = "Sign_Counter" },
            new Spec("FireEvacuationPlan", "Showroom", 0.42f, GraphicFrame.Clip, V(1.6f, 1.5f, ShowroomFace), Vector3.back),
            // Right wall over the seating corner.
            new Spec("Art_Pissarro_BoulevardMontmartre", "Showroom", 0.89f, GraphicFrame.Gilt, V(L.MaxX, 1.65f, 1.1f), Vector3.left),
            // The CCTV warning on the left pier's street face.
            new Spec("Sign_CCTV", "Showroom", 0.30f, GraphicFrame.None, V(L.MinX - L.Pier * 0.5f, 1.6f, PierFace), Vector3.back),
            // Office face of the partition.
            new Spec("NoticeBoard_StaffCork", "Office", 0.90f, GraphicFrame.None, V(1.9f, 1.5f, OfficeFace), Vector3.forward),
            new Spec("Calendar_October2026", "Office", 0.30f, GraphicFrame.None, V(2.6f, 1.6f, OfficeFace), Vector3.forward),
            new Spec("Certificate_GemmologyDiplomaFramed", "Office", 0.48f, GraphicFrame.None, V(-2.0f, 1.6f, OfficeFace), Vector3.forward),
        };
        // Stickers on the street face of the left sidelight's pane (intact at night).
        Transform pane = root.Find("Facade/Sidelight_Left/Glass");
        Renderer glass = pane != null ? pane.GetComponent<Renderer>() : null;
        if (glass != null && L.LeftSidelight != StoreKit.Pane.Broken)
        {
            float x = -(L.DoorHalf + L.SidelightWidth * 0.5f), z = glass.bounds.min.z;
            specs.Add(new Spec("Sticker_AlarmMonitored", "Showroom", 0.10f, GraphicFrame.Sticker, V(x, 1.95f, z), Vector3.back));
            specs.Add(new Spec("Sticker_InsurerApproved", "Showroom", 0.12f, GraphicFrame.Sticker, V(x, 1.82f, z), Vector3.back));
            specs.Add(new Spec("Sticker_CardsAccepted", "Showroom", 0.12f, GraphicFrame.Sticker, V(x, 1.2f, z), Vector3.back));
        }
        return specs;
    }

    // The street page's graphics.
    private static List<Spec> StreetSpecs(Transform root)
    {
        // PosterWall's centre is 12 mm proud of the blank ground floor across the street.
        const float posterProud = 0.012f;
        NightStreet.PosterWall(root, out Vector3 poster, out Vector3 normal);
        return new List<Spec>
        {
            // Tags on the left neighbour's shutter, 2 mm off it.
            new Spec("Graffiti_Tags", StreetZone, 3.0f, GraphicFrame.None, V(-12.2f, 1.25f, ShutterFace), Vector3.back) { Proud = 0.002f },
            new Spec(PosterCell, StreetZone, 3.0f, GraphicFrame.None, poster - normal * posterProud, normal) { Proud = posterProud },
            // The lost-cat flyer at 1.5 m, taped flat to the right pier's street face a sheet's
            // 3 mm off the stone (the left pier carries the CCTV sign). The README tapes it to a
            // lamp post, but every post here is about 0.1 m across (the lantern's round 88 mm, the
            // KayKit streetlight's 105 mm octagon, the stand-in's 0.12 m box), so the flat 0.23 m
            // card would leave about 6 cm of it in the air either side; the pier stands in every build.
            new Spec("Flyer_LostCat", StreetZone, 0.23f, GraphicFrame.None, V(L.MaxX + L.Pier * 0.5f, 1.5f, PierFace), Vector3.back),
        };
    }

    // The interior page: the showroom's graphics in one KitMesh (picture, gilt and FrameMetal
    // submeshes), the office's in another, both lightmapped at FixtureScale. Without the page
    // or its layout nothing is placed and the rear and counter sign frames keep their emblem.
    private static void InteriorGraphics(Transform root, Transform showroom, Transform office)
    {
        if (showroom == null || office == null) return;
        Texture2D page = DemoAssetLibrary.Texture(GraphicsPack, InteriorAtlas + "_Color.png");
        if (page == null) return;
        Dictionary<string, Rect> cells = DemoAssetLibrary.Atlas(GraphicsPack, InteriorAtlas + ".json");
        if (cells == null) return;
        Material picture = AtlasMaterial(InteriorMaterialName, page, 0.5f, 0.25f), gilt = GiltMaterial();
        KitMesh room = new KitMesh { LightmapScale = NightLighting.FixtureScale }, staff = new KitMesh { LightmapScale = NightLighting.FixtureScale };
        foreach (Spec s in InteriorSpecs(root))
        {
            if (!cells.TryGetValue(s.Cell, out Rect uv) || uv.width <= 0 || uv.height <= 0) continue;
            if (s.Replaces != null)
            {
                // Only a procedural sign frame gives way; a user-converted one keeps its face.
                Transform frame = showroom.Find(s.Replaces);
                Transform body = frame != null ? frame.Find("Body") : null;
                if (body == null) continue;
                Object.DestroyImmediate(body.gameObject);
            }
            Add(s.Zone == "Office" ? staff : room, picture, gilt, s, uv, page);
        }
        Emit(room, showroom, GraphicsName, "night_wall_graphics_showroom");
        Emit(staff, office, GraphicsName, "night_wall_graphics_office");
    }

    // The street page in one KitMesh under Exterior/Street, lightmapped at KitScale like the
    // rest of the street kit (NightLighting.LightmapScales).
    private static void StreetGraphics(Transform root)
    {
        Texture2D page = DemoAssetLibrary.Texture(GraphicsPack, StreetAtlas + "_Color.png");
        if (page == null) return;
        Dictionary<string, Rect> cells = DemoAssetLibrary.Atlas(GraphicsPack, StreetAtlas + ".json");
        Transform street = root.Find(StreetZone);
        if (cells == null || street == null) return;
        Material picture = AtlasMaterial(StreetMaterialName, page, 0.3f, 0.2f);
        KitMesh k = new KitMesh { LightmapScale = NightLighting.KitScale };
        foreach (Spec s in StreetSpecs(root))
            if (cells.TryGetValue(s.Cell, out Rect uv) && uv.width > 0 && uv.height > 0) Add(k, picture, null, s, uv, page);
        Emit(k, street, StreetGraphicsName, "night_street_graphics");
    }

    // One graphic into k: the picture quad on the cell's rect (unmirrored seen from the
    // front), its mount, and its record in Graphics.
    private static void Add(KitMesh k, Material picture, Material gilt, Spec s, Rect uv, Texture page)
    {
        Vector2 size = new Vector2(s.Width, s.Width * uv.height * page.height / (uv.width * page.width));
        Vector3 n = s.Normal.normalized, r = Vector3.Cross(Vector3.up, -n).normalized, u = Vector3.up;
        float proud = s.Proud > 0 ? s.Proud : ProudOf(s.Frame), hw = size.x * 0.5f, hh = size.y * 0.5f;
        Vector3 c = s.Surface + n * proud;
        k.Quad(picture, n, c - r * hw - u * hh, c + r * hw - u * hh, c + r * hw + u * hh, c - r * hw + u * hh, uv);
        if (s.Frame == GraphicFrame.Gilt && gilt != null) GiltFrame(k, gilt, c, n, r, u, hw, hh, proud);
        else if (s.Frame == GraphicFrame.Box) BoxSides(k, c, n, r, u, hw, hh, proud);
        else if (s.Frame == GraphicFrame.Clip) ClipFrame(k, c, n, r, u, hw, hh, proud);
        Graphics.Add(new PlacedGraphic { Name = s.Cell, Zone = s.Zone, Centre = c, Normal = n, Size = size, Uv = uv, Frame = s.Frame, Proud = proud });
    }

    // The corners of a 2hw x 2hh rectangle round c, clockwise from the top left seen from
    // the front (r is the viewer's right); edge i runs from corner i to i + 1 and faces
    // Outward(i): top, right, bottom, left.
    private static Vector3[] Corners(Vector3 c, Vector3 r, Vector3 u, float hw, float hh)
        => new[] { c - r * hw + u * hh, c + r * hw + u * hh, c + r * hw - u * hh, c - r * hw - u * hh };

    private static Vector3 Outward(int edge, Vector3 r, Vector3 u) => edge == 0 ? u : edge == 1 ? r : edge == 2 ? -u : -r;

    // Four mitred bars round the picture, their face GiltDepth off the wall and GiltBar wide,
    // with the outer sides down to the wall and the rebate down to the picture. The moulding
    // runs clockwise round the frame, so each bar is the strip turned, never mirrored: u along
    // the bar (one repeat per GiltRepeat, from the bar's outer corner), v 0 at the sight edge
    // and 1 at the outer edge, the moulding image's bottom and top rows. One UV rect on the
    // whole trapezoid would shear the beads and the twist 45 degrees either side of its
    // diagonal, so each face is the rectangle over the sight edge plus its two mitre
    // triangles, all mapped by position. Quad maps a rect, so a mitre is a quad whose fourth
    // corner sits Sliver behind the face at the sight corner: the extra triangle is a hidden
    // sliver inside the frame (never zero-area), and its tangent lies along the normal.
    private const float Sliver = 0.0001f;

    private static void GiltFrame(KitMesh k, Material gilt, Vector3 c, Vector3 n, Vector3 r, Vector3 u, float hw, float hh, float proud)
    {
        Vector3 face = c + n * (GiltDepth - proud), toWall = -n * GiltDepth, toPicture = -n * (GiltDepth - proud), hidden = -n * Sliver;
        Vector3[] inner = Corners(face, r, u, hw, hh), outer = Corners(face, r, u, hw + GiltBar, hh + GiltBar);
        float g = GiltBar / GiltRepeat;
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            Vector3 outward = Outward(i, r, u), across = outward * GiltBar;
            float s = (i % 2 == 0 ? 2 * hw : 2 * hh) / GiltRepeat;
            k.Quad(gilt, n, inner[i], inner[j], inner[j] + across, inner[i] + across, new Rect(g, 0, s, 1));
            // The start mitre takes its rect upside down, so the real triangle is the quad's first.
            k.Quad(gilt, n, outer[i], inner[i] + across, inner[i], inner[i] + hidden, Rect.MinMaxRect(0, 1, g, 0));
            k.Quad(gilt, n, inner[j], inner[j] + hidden, outer[j], inner[j] + across, new Rect(g + s, 0, g, 1));
            k.Quad(gilt, outward, outer[i] + toWall, outer[j] + toWall, outer[j], outer[i], new Rect(0, 1 - EdgeBand, s + 2 * g, EdgeBand));
            k.Quad(gilt, -outward, inner[i], inner[j], inner[j] + toPicture, inner[i] + toPicture, new Rect(g, 0, s, EdgeBand));
        }
    }

    // A lightbox: the picture is its face, BoxDepth off the wall, on FrameMetal sides.
    private static void BoxSides(KitMesh k, Vector3 c, Vector3 n, Vector3 r, Vector3 u, float hw, float hh, float proud)
    {
        Vector3[] edge = Corners(c, r, u, hw, hh);
        Vector3 toWall = -n * proud;
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            k.Face(StoreKit.FrameMetal, Outward(i, r, u), edge[i] + toWall, edge[j] + toWall, edge[j], edge[i]);
        }
    }

    // A clip frame: ClipStrip-wide mitred FrameMetal strips round the sheet, ClipLift over it,
    // with their outer sides down to the wall and a lip down to the sheet.
    private static void ClipFrame(KitMesh k, Vector3 c, Vector3 n, Vector3 r, Vector3 u, float hw, float hh, float proud)
    {
        Vector3 front = c + n * ClipLift, toWall = -n * (proud + ClipLift), toSheet = -n * ClipLift;
        Vector3[] inner = Corners(front, r, u, hw, hh), outer = Corners(front, r, u, hw + ClipStrip, hh + ClipStrip);
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            Vector3 outward = Outward(i, r, u);
            k.Face(StoreKit.FrameMetal, n, inner[i], inner[j], outer[j], outer[i]);
            k.Face(StoreKit.FrameMetal, outward, outer[i] + toWall, outer[j] + toWall, outer[j], outer[i]);
            k.Face(StoreKit.FrameMetal, -outward, inner[i], inner[j], inner[j] + toSheet, inner[i] + toSheet);
        }
    }

    // The part under parent, casting no shadow (sheets a few millimetres off the wall);
    // nothing when no graphic went in.
    private static void Emit(KitMesh k, Transform parent, string name, string asset)
    {
        if (k.IsEmpty) return;
        GameObject g = k.Emit(name, parent, Vector3.zero, Quaternion.identity, NightStoreBuilder.Folder, asset);
        MeshRenderer renderer = g.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    // Standard Cutout on an atlas page at the cutoff its import keeps coverage for
    // (PackImport.CutoffOf, else the README's), no normal map, no emission: the opaque cells
    // pass whole, the stickers', calendar's and graffiti's clear texels cut.
    private static Material AtlasMaterial(string name, Texture2D page, float cutoff, float smoothness)
    {
        float imported = PackImport.CutoffOf(AssetDatabase.GetAssetPath(page));
        Material m = Mat(NightStoreBuilder.Folder, name, Color.white, smoothness);
        m.mainTexture = page;
        m.mainTextureScale = Vector2.one;
        m.mainTextureOffset = Vector2.zero;
        m.SetFloat("_Metallic", 0);
        m.SetTexture("_BumpMap", null);
        m.DisableKeyword("_NORMALMAP");
        DemoAssetLibrary.ApplyMetallicSmoothness(m, null);
        NoEmission(m);
        Cutout(m, imported > 0 ? imported : cutoff);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Gilt moulding (README: metallic 0.8, smoothness 0.5) with its colour and normal map,
    // repeating along the bars (the import wraps it in U); a plain gilt tint without the map.
    private static readonly Color GiltTint = new Color(0.72f, 0.56f, 0.30f);

    private static Material GiltMaterial()
    {
        Texture2D color = DemoAssetLibrary.Texture(GraphicsPack, GiltColorFile);
        Texture2D normal = color != null ? DemoAssetLibrary.NormalMap(GraphicsPack, GiltNormalFile) : null;
        Material m = Mat(NightStoreBuilder.Folder, GiltMaterialName, color != null ? Color.white : GiltTint, 0.5f);
        m.mainTexture = color;
        m.mainTextureScale = Vector2.one;
        m.mainTextureOffset = Vector2.zero;
        m.SetFloat("_Metallic", 0.8f);
        m.SetTexture("_BumpMap", normal);
        if (normal != null)
        {
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_BumpScale", 1);
        }
        else m.DisableKeyword("_NORMALMAP");
        DemoAssetLibrary.ApplyMetallicSmoothness(m, null);
        NoEmission(m);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void NoEmission(Material m)
    {
        m.DisableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", Color.black);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
    }
}
#endif
