#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;
using L = NightStoreLayout;

// SurfaceDecals in the night store as thin Standard Fade quads on KitMeshes (the pack
// README's Built-in recipe: no projector decals without packages). Two atlas pages
// (Atlas/Ambient and Atlas/Evidence, cell rects from their JSON) and the glass-shard scatter,
// four materials in all. Always on, at arrival: shards on the floor in front of the three
// broken cases, puddles, oil, litter, leaves and gum on the street and both pavements, grime
// under a neighbour's sill; one renderer per group, lightmapped at its zone's density
// (DecalsLightmapped), never a shadow caster, occluder or reflection-probe static. Hidden, as
// presenter sets under Dressing/PresenterSets (off at arrival, probe-lit, never static):
// AmbientWear (floor wear that could read as evidence) and CaseEvidence (a placeholder
// footwear trail, pry marks, latent prints, the security camera's cut cable and the
// discarded tools; Koda's decisions 2, 5 and 8). PBRInterior's GlassSmudge is not placed:
// its fingerprint clusters read as evidence too (listed for Koda). A missing atlas skips its
// decals, so a project unpacked from the ZIP builds today's floors; the tools fall back to
// procedural stand-ins. A build without imported assets (Apply's imported false) reads no
// SurfaceDecals or ForensicModels file at all: no decal and no wear set, only the evidence
// set with the cut cable and the stand-in tools. Runs after
// NightStoreDressing.CombineLoose and StoreKit.MarkStatic and sets its own static flags.
public static class NightDecals
{
    public const string Pack = "SurfaceDecals";
    // Atlas pages: <name>_Color.png, _Normal.png, _MetallicSmoothness.png and <name>.json.
    public const string AmbientAtlas = "Atlas/Ambient", EvidenceAtlas = "Atlas/Evidence", ShardsFile = "GlassShards_Scatter";
    public const string AmbientMaterial = "NightDecalAmbient", ShardsMaterial = "NightDecalGlassShards",
        EvidenceMaterial = "NightDecalEvidence", EvidenceGlassMaterial = "NightDecalEvidenceGlass";

    // true: the always-on decals take lightmaps at their zone's density (NightLighting.
    // LightmapScales gives the same scale by path); false: they are probe-lit like the hidden
    // sets, split into LooseCell cells. Read-only static, not const, so a test of it is never
    // flagged as unreachable code.
    public static readonly bool DecalsLightmapped = true;

    // Render queues: floor and wall decals before every transparent (README: 2450-2500), the
    // prints on the case glass one above the glass (KitGlass is 3000).
    public const int FloorQueue = 2460, GlassQueue = 3001;

    // Always-on groups, as paths under the store root.
    public const string FloorGroup = "Architecture/FloorDecals", StreetGroup = "Exterior/Ground/StreetDecals",
        PavementGroup = "Exterior/Ground/PavementDecals", FarPavementGroup = "Exterior/Ground/FarPavementDecals",
        NeighboursGroup = "Exterior/Buildings/NeighboursDecals";
    public static readonly string[] AlwaysOnGroups = { FloorGroup, StreetGroup, PavementGroup, FarPavementGroup, NeighboursGroup };

    // Hidden sets: Dressing/PresenterSets/<set> carries the PresenterSet, its Content child
    // (inactive at arrival) the renderers: Decals_<i>_<j> and GlassPrints_<i>_<j> per LooseCell
    // cell, and the evidence set's Tools.
    public const string SetsGroup = "PresenterSets", SetsPath = "Dressing/" + SetsGroup;
    public const string AmbientSet = "AmbientWear", EvidenceSet = "CaseEvidence", ContentName = "Content", ToolsGroup = "Tools";
    public const string DecalsName = "Decals", GlassPrintsName = "GlassPrints";

    // How far a quad stands off its surface (1-4 mm): the outdoor ground decals 3 mm (they are
    // seen at grazing angles from further away), the shop floor and the hidden marks 1.5 mm
    // (the tracked dirt 0.1 mm over that, where it meets the trail), the neighbour's wall 2 mm.
    public const float GroundLift = 0.003f, SurfaceLift = 0.0015f, TrackedLift = 0.0016f, WallLift = 0.002f;

    // Ambient cells that read as wear (hidden at arrival): the arrival check finds none in use.
    public static readonly string[] AmbientWearCells = { "WaterStain_FloorDried", "FloorScuffs_Heel", "FloorScratches_Fine", "DirtEdge_WallBase" };

    // Real sizes in metres (width along the image's U, height along V), from the SurfaceDecals
    // README; the atlas keeps each cell's aspect within 1 %, so the quads keep them.
    private static readonly Dictionary<string, Vector2> Sizes = new Dictionary<string, Vector2>
    {
        ["Puddle_Kerbside"] = new Vector2(2.0f, 1.0f), ["Puddle_Round"] = new Vector2(1.2f, 1.2f), ["Puddle_Cluster"] = new Vector2(1.5f, 1.5f),
        ["OilStain"] = new Vector2(1, 1), ["LitterScatter"] = new Vector2(1, 1), ["LeafLitter_Wet"] = new Vector2(1, 1),
        ["ChewingGum_Scatter"] = new Vector2(1, 1), ["GrimeStreaks_Wall"] = new Vector2(0.6f, 1.2f),
        ["WaterStain_FloorDried"] = new Vector2(1, 1), ["FloorScuffs_Heel"] = new Vector2(1, 1), ["FloorScratches_Fine"] = new Vector2(1, 1),
        ["DirtEdge_WallBase"] = new Vector2(2.0f, 0.25f), ["DirtTracked_Entrance"] = new Vector2(1.5f, 1.5f),
        ["ShoePrint_Sneaker_Mud_L"] = new Vector2(0.2f, 0.4f), ["ShoePrint_Sneaker_Mud_R"] = new Vector2(0.2f, 0.4f),
        ["ShoePrint_Sneaker_Dust_L"] = new Vector2(0.2f, 0.4f), ["ShoePrint_Sneaker_Dust_R"] = new Vector2(0.2f, 0.4f),
        ["GlassHandprint_Latent"] = new Vector2(0.19f, 0.19f), [ShardsFile] = new Vector2(1.2f, 1.2f),
    };

    // One placed decal, for the validators: its name, the atlas cell (or the shards file) it
    // samples, the path of its group under the store root (the renderer's object, or the group
    // or set Content holding its cell renderers), its centre, surface normal and image +V in
    // world space (the store root sits at the origin), its quad size, the UV rect it samples
    // (inside the cell) and whether it is in a hidden presenter set.
    public struct Placed
    {
        public string Name, Cell, Group;
        public Vector3 Centre, Normal, Up;
        public Vector2 Size;
        public Rect Uv;
        public bool Hidden;
    }

    // What the last Apply placed, in placing order (the footwear trail in walking order).
    public static readonly List<Placed> Decals = new List<Placed>();

    // The atlas layouts the last Apply found (null: that atlas was missing and its decals were
    // skipped), and whether it found the shard scatter.
    public static Dictionary<string, Rect> AmbientCells { get; private set; }
    public static Dictionary<string, Rect> EvidenceCells { get; private set; }
    public static bool HasAmbient => AmbientCells != null;
    public static bool HasEvidence => EvidenceCells != null;
    public static bool HasShards { get; private set; }

    // A decal before it becomes a quad (world space; Mirror flips the image along U).
    private struct Decal
    {
        public string Name, Cell;
        public Vector3 Centre, Normal, Up;
        public Vector2 Size;
        public Rect Uv;
        public bool Mirror;
    }

    // ---------------------------------------------------------------- build

    // imported false (a build without third-party assets): no atlas or scatter is loaded, so
    // no decal is placed, and the evidence set holds the stand-in tools only.
    public static void Apply(Transform root, string folder, bool imported = true)
    {
        Decals.Clear();
        AmbientCells = EvidenceCells = null;
        HasShards = false;
        if (root == null) return;
        StoreKit.Palette();
        Texture2D shards = null;
        if (imported)
        {
            AmbientCells = Cells(AmbientAtlas);
            EvidenceCells = Cells(EvidenceAtlas);
            shards = DemoAssetLibrary.Texture(Pack, ShardsFile + ".png");
        }
        HasShards = shards != null;

        // Always on: the shard scatter on the shop floor, then the ambient page outdoors.
        if (HasShards)
        {
            Material glass = DecalMaterial(folder, ShardsMaterial, FloorQueue, shards, DemoAssetLibrary.NormalMap(Pack, ShardsFile + "_Normal.png"), null, ShardSmoothness);
            EmitBaked(root, FloorGroup, ShardDecals(root), glass, folder, "night_decals_floor");
        }
        Material ambient = HasAmbient ? AtlasMaterial(folder, AmbientMaterial, AmbientAtlas, FloorQueue) : null;
        if (ambient != null)
        {
            EmitBaked(root, StreetGroup, StreetDecals(root), ambient, folder, "night_decals_street");
            EmitBaked(root, PavementGroup, PavementDecals(root), ambient, folder, "night_decals_pavement");
            EmitBaked(root, FarPavementGroup, FarPavementDecals(root), ambient, folder, "night_decals_far_pavement");
            EmitBaked(root, NeighboursGroup, NeighbourDecals(root), ambient, folder, "night_decals_neighbours");
        }

        // Hidden presenter sets beside the rest of the dressing; the wear shares the ambient page.
        Transform dressing = root.Find("Dressing");
        if (dressing == null) return;
        Transform sets = Group(SetsGroup, dressing);
        if (ambient != null) AmbientWear(root, sets, ambient, folder);
        CaseEvidence(root, sets, folder, imported);
    }

    // UV rects of an atlas page, or null (skipping its decals) when the page or its layout is
    // missing; the layout warns only when the page is there without it.
    private static Dictionary<string, Rect> Cells(string atlas)
        => DemoAssetLibrary.HasTexture(Pack, atlas + "_Color.png") ? DemoAssetLibrary.Atlas(Pack, atlas + ".json") : null;

    // ---------------------------------------------------------------- materials

    // The shard scatter has no packed map: the README's smoothness for glass.
    private const float ShardSmoothness = 0.9f, AtlasSmoothness = 0.3f;

    // An atlas page's colour, normal and packed metallic-smoothness maps (the constant cells
    // carry their README smoothness in the map's alpha), at the queue.
    private static Material AtlasMaterial(string folder, string name, string atlas, int queue)
        => DecalMaterial(folder, name, queue, DemoAssetLibrary.Texture(Pack, atlas + "_Color.png"), DemoAssetLibrary.NormalMap(Pack, atlas + "_Normal.png"),
            DemoAssetLibrary.DataMap(Pack, atlas + "_MetallicSmoothness.png"), AtlasSmoothness);

    // Standard Fade (diffuse and reflections both fade with the texture alpha, so the clear
    // part of a quad shows nothing), white tint, no emission or occlusion; smoothness from the
    // packed map when there is one, else the scalar. Every property is set, so a material
    // asset saved by an earlier build never keeps a stale map.
    private static Material DecalMaterial(string folder, string name, int queue, Texture2D color, Texture2D normal, Texture2D packed, float smoothness)
    {
        Material m = Mat(folder, name, Color.white, smoothness);
        m.mainTexture = color;
        m.mainTextureScale = Vector2.one;
        m.mainTextureOffset = Vector2.zero;
        m.SetFloat("_Metallic", 0);
        m.SetTexture("_BumpMap", normal);
        if (normal != null)
        {
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_BumpScale", 1);
        }
        else m.DisableKeyword("_NORMALMAP");
        m.SetTexture("_OcclusionMap", null);
        DemoAssetLibrary.ApplyMetallicSmoothness(m, packed);
        m.DisableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", Color.black);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        Fade(m, queue);
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------------------------------------------------------- always on

    // One GlassShards_Scatter (1.2 m) in front of each broken case, centred on the middle of
    // the dressing's glass strip (NightStoreDressing.GlassZones: 0.3-1.2 m off the case's
    // centre line), so the footsteps crunch where the shards show; its top towards the case,
    // whose front spill overlaps the scatter's impact point (UV 0.50, 0.58). Turned and
    // mirrored per case so neighbouring cases do not repeat.
    private const float ShardsOut = 0.75f;
    private static readonly float[] ShardTurns = { 0, 8, -6 };

    private static List<Decal> ShardDecals(Transform root)
    {
        List<Decal> list = new List<Decal>();
        Rect whole = new Rect(0, 0, 1, 1);
        foreach (L.Fixture f in L.Fixtures)
        {
            if ((f.Kit != "display_case" && f.Kit != "wall_case") || NightStoreDressing.CaseGlass(f.Name) == StoreKit.CaseGlass.Intact) continue;
            int n = list.Count;
            Vector3 p = f.Position + Quaternion.Euler(0, f.Yaw, 0) * V(0, 0, -ShardsOut) + Vector3.up * SurfaceLift;
            AddFlat(list, root, ShardsFile, "GlassShards_" + f.Name, whole, p, f.Yaw + ShardTurns[n % ShardTurns.Length], n % 2 == 1);
        }
        return list;
    }

    // The wet street (StreetScale): gutter puddles against the kerb (top edge = kerb face)
    // round both gully grates, two round puddles in the carriageway, oil where cars park on the
    // far side and wet leaves in the gutter beside the car.
    private static List<Decal> StreetDecals(Transform root)
    {
        List<Decal> list = new List<Decal>();
        float y = L.StreetY + GroundLift;
        AddAmbient(list, root, "Puddle_Kerbside", V(2.6f, y, L.KerbZ - 0.5f), 0);
        AddAmbient(list, root, "Puddle_Kerbside", V(-6.0f, y, L.KerbZ - 0.5f), 0, true);
        AddAmbient(list, root, "Puddle_Round", V(6.8f, y, -9.0f), 0);
        AddAmbient(list, root, "Puddle_Round", V(-3.0f, y, -11.0f), 140);
        AddAmbient(list, root, "OilStain", V(-9.0f, y, -12.8f), 25);
        AddAmbient(list, root, "LeafLitter_Wet", V(5.4f, y, L.KerbZ - 0.45f), 200);
        return list;
    }

    // The near pavement (GroundScale), each quad axis-aligned so none overhangs the kerb face:
    // a puddle cluster by the left lamp, litter by the easel, leaves and gum near the door.
    private static List<Decal> PavementDecals(Transform root)
    {
        List<Decal> list = new List<Decal>();
        AddAmbient(list, root, "Puddle_Cluster", V(-7.2f, GroundLift, -1.0f), 0);
        AddAmbient(list, root, "LitterScatter", V(7.8f, GroundLift, -2.45f), 0);
        AddAmbient(list, root, "LeafLitter_Wet", V(-3.6f, GroundLift, -2.5f), 90);
        AddAmbient(list, root, "ChewingGum_Scatter", V(3.6f, GroundLift, -1.0f), 0);
        return list;
    }

    // The far pavement (FarScale) round the bus stop.
    private static List<Decal> FarPavementDecals(Transform root)
    {
        List<Decal> list = new List<Decal>();
        AddAmbient(list, root, "LitterScatter", V(-9.5f, GroundLift, -14.7f), 160);
        AddAmbient(list, root, "ChewingGum_Scatter", V(-8.0f, GroundLift, -15.3f), 110);
        return list;
    }

    // Soot streaks (0.6 x 1.2 m, top = ledge) on the right neighbour's render, their top at the
    // first-floor sill line (4.2 m) and running down behind the fascia board, WallLift off the
    // facade (z L.MinZ, which faces -z); the second one mirrored.
    private static List<Decal> NeighbourDecals(Transform root)
    {
        List<Decal> list = new List<Decal>();
        if (!AmbientCells.TryGetValue("GrimeStreaks_Wall", out Rect uv)) return list;
        float z = L.MinZ - WallLift;
        foreach (float x in new[] { 9.2f, 9.95f })
            list.Add(new Decal
            {
                Name = "GrimeStreaks_Wall_" + list.Count, Cell = "GrimeStreaks_Wall", Uv = uv, Size = Sizes["GrimeStreaks_Wall"],
                Centre = root.TransformPoint(V(x, 3.6f, z)), Normal = root.TransformDirection(Vector3.back), Up = root.up, Mirror = list.Count == 1
            });
        return list;
    }

    // ---------------------------------------------------------------- hidden sets

    // Ambient wear, probe-lit on the shop floor: a dried water stain inside the entrance, heel
    // scuffs in the right aisle and at the counter, fine scratches along the walkways and dust
    // along the rear partitions' skirting (top edge = wall).
    private static void AmbientWear(Transform root, Transform sets, Material ambient, string folder)
    {
        List<Decal> wear = new List<Decal>();
        float wall = L.RearZ - L.Partition * 0.5f - Sizes["DirtEdge_WallBase"].y * 0.5f;
        AddAmbient(wear, root, "WaterStain_FloorDried", V(-2.5f, SurfaceLift, 1.2f), 0);
        AddAmbient(wear, root, "FloorScuffs_Heel", V(3.4f, SurfaceLift, 3.0f), 30);
        AddAmbient(wear, root, "FloorScuffs_Heel", V(5.5f, SurfaceLift, 7.1f), 0);
        AddAmbient(wear, root, "FloorScratches_Fine", V(0.1f, SurfaceLift, 2.6f), 30);
        AddAmbient(wear, root, "FloorScratches_Fine", V(-3.2f, SurfaceLift, 6.8f), 90);
        AddAmbient(wear, root, "DirtEdge_WallBase", V(-6.0f, SurfaceLift, wall), 0);
        AddAmbient(wear, root, "DirtEdge_WallBase", V(6.0f, SurfaceLift, wall), 0, true);
        if (wear.Count == 0) return;
        Transform content = NewSet(sets, AmbientSet, "Ambient wear",
            "Water stain, heel scuffs, fine scratches and wall-base dust. Everyday wear can read as evidence; Koda decides whether it stays.");
        EmitProbed(root, content, DecalsName, wear, ambient, folder, "night_decals_wear", false, true);
        Close(content);
    }

    // The case evidence, a placeholder until Koda decides the entry (decision 2), the tools
    // (decision 5) and what the set holds (decision 8): a mud trail from the street to the
    // forced door, the tracked dirt inside it, a dust trail along the walk route to the office
    // opening, pry marks on the right leaf's lock stile, latent prints on the central case's
    // top and a handprint on the cracked top, the corner camera's cut cable hanging under its
    // plate (at arrival the cable is concealed), and the crowbar, sledgehammer and
    // screwdriver. Without the Evidence atlas only the cable and the tools (the stand-ins with
    // imported false).
    private static void CaseEvidence(Transform root, Transform sets, string folder, bool imported)
    {
        Transform content = NewSet(sets, EvidenceSet, "Case evidence",
            "Placeholder trail, pry marks, prints, a cut camera cable and tools for the instructor; Koda's decisions 2 (entry), 5 (tools) and 8 (contents).");
        if (HasEvidence)
        {
            List<Decal> marks = new List<Decal>(), prints = new List<Decal>();
            MudPrints(marks, root);
            if (EvidenceCells.TryGetValue("DirtTracked_Entrance", out Rect dirt))
                AddFlat(marks, root, "DirtTracked_Entrance", "DirtTracked_Entrance", dirt, V(0.1f, TrackedLift, 0.85f), 0);
            DustTrail(marks, root);
            PryMarks(marks, root);
            GlassPrints(prints, root);
            if (marks.Count > 0) EmitProbed(root, content, DecalsName, marks, AtlasMaterial(folder, EvidenceMaterial, EvidenceAtlas, FloorQueue), folder, "night_decals_evidence", false, true);
            if (prints.Count > 0) EmitProbed(root, content, GlassPrintsName, prints, AtlasMaterial(folder, EvidenceGlassMaterial, EvidenceAtlas, GlassQueue), folder, "night_decals_evidence_glass", false, true);
        }
        PlaceTools(root, Group(ToolsGroup, content), folder, imported);
        CutCable(root, content, folder);
        Close(content);
    }

    // The corner camera's cable, cut: 0.3 m of 7 mm black cable hanging from inside the
    // camera's 12 mm wall plate (its open top 1 cm up inside it), drifting a little off the
    // partition, with the copper core showing at the cut end. Offsets in the camera's own
    // frame from its pivot (the bottom centre of its plate, on the partition; -z is off the
    // wall), so the cable stays on the plate if the layout moves or turns the camera; the
    // axis stays 6-8 mm in front of the wall, so the 3.5 mm radius keeps at least 2.5 mm off
    // it. Probe-lit and never static (Close); 40 triangles. Its placement is decision 8.
    public const string CutCableName = "CutCable";
    private static readonly Vector3[] CutCablePoints = { V(0, 0.010f, -0.006f), V(0.004f, -0.10f, -0.0065f), V(0.010f, -0.20f, -0.0075f), V(0.014f, -0.29f, -0.008f) };

    private static void CutCable(Transform root, Transform content, string folder)
    {
        Transform camera = root.Find("Showroom/SecurityCamera");
        if (camera == null) return;
        Vector3[] p = new Vector3[CutCablePoints.Length];
        for (int i = 0; i < p.Length; i++) p[i] = content.InverseTransformPoint(camera.TransformPoint(CutCablePoints[i]));
        KitMesh k = new KitMesh { Lightmapped = false };
        for (int i = 0; i + 1 < p.Length; i++) k.Cylinder(StoreKit.DeviceBlack, p[i], p[i + 1], 0.0035f, 6, false);
        Vector3 end = p[p.Length - 1], dir = (end - p[p.Length - 2]).normalized;
        k.Face(StoreKit.Bronze, dir, KitMesh.Ring(end, Quaternion.FromToRotation(Vector3.up, dir), 0.0035f, 0, 6, 0));
        k.Emit(CutCableName, content, Vector3.zero, Quaternion.identity, folder, "night_evidence_cut_cable");
    }

    // Shoe-print walk (README): heel-to-heel steps of 1.3-1.5 m, so prints Stride apart, Gait
    // either side of the line, toes turned out ToeOut degrees, the L and R files in turn.
    private const float Stride = 0.7f, Gait = 0.1f, ToeOut = 6;
    // The dust trail starts where the route first passes this z inside the shop.
    private const float TrailStartZ = 1.7f;

    // Sneaker mud prints L/R/L on the pavement in front of the forced doors, toes into the shop.
    private static void MudPrints(List<Decal> list, Transform root)
    {
        Vector3[] steps = { V(-0.15f, SurfaceLift, -2.25f), V(0.08f, SurfaceLift, -1.55f), V(-0.12f, SurfaceLift, -0.85f) };
        for (int i = 0; i < steps.Length; i++) Print(list, root, "Mud", i % 2 == 0, steps[i], Vector3.forward, "MudPrint_" + i);
    }

    // Dust prints every Stride along NightStoreLayout.Route, from where it crosses TrailStartZ
    // to its last showroom point before the office opening, left and right in turn, each toe
    // along its segment of the route.
    private static void DustTrail(List<Decal> list, Transform root)
    {
        Vector3[] route = L.Route;
        int last = -1;
        for (int i = 0; i < route.Length; i++) if (route[i].z < L.RearZ - 0.5f) last = i;
        List<Vector3> path = new List<Vector3>();
        for (int i = 0; i < last; i++)
        {
            Vector3 a = route[i], b = route[i + 1];
            if (path.Count > 0) path.Add(b);
            else if (a.z <= TrailStartZ && b.z > TrailStartZ)
            {
                path.Add(Vector3.Lerp(a, b, (TrailStartZ - a.z) / (b.z - a.z)));
                path.Add(b);
            }
        }
        float along = 0, start = 0;
        int step = 0;
        for (int i = 0; i + 1 < path.Count; i++)
        {
            Vector3 dir = path[i + 1] - path[i];
            dir.y = 0;
            float length = dir.magnitude;
            if (length < 1e-4f) continue;
            dir /= length;
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            for (; along <= start + length; along += Stride, step++)
            {
                bool left = step % 2 == 0;
                Vector3 p = path[i] + dir * (along - start) + right * (left ? -Gait : Gait);
                p.y = SurfaceLift;
                Print(list, root, "Dust", left, p, dir, "DustPrint_" + step);
            }
            start += length;
        }
    }

    // One ShoePrint_Sneaker_<kind>_L/R (0.20 x 0.40 m, toe = +V) at p (store-root space), its
    // toe along forward turned out ToeOut degrees.
    private static void Print(List<Decal> list, Transform root, string kind, bool left, Vector3 p, Vector3 forward, string name)
    {
        string cell = "ShoePrint_Sneaker_" + kind + (left ? "_L" : "_R");
        if (!EvidenceCells.TryGetValue(cell, out Rect uv)) return;
        Vector3 toe = Quaternion.AngleAxis(left ? -ToeOut : ToeOut, Vector3.up) * forward;
        list.Add(new Decal { Name = name, Cell = cell, Uv = uv, Size = Sizes[cell], Centre = root.TransformPoint(p), Normal = root.up, Up = root.TransformDirection(toe) });
    }

    // Pry marks (0.12 m, right edge = the frame edge at the door gap) on the street face of the
    // right entrance leaf's lock stile, PryHeight up. The stile is StileWidth wide at the leaf's
    // free edge (StoreKit.StorefrontDoors): the quad keeps the real scale and takes only the
    // cell's StileWidth strip at the gap edge (all three dents lie in it), mirrored because
    // the right leaf's gap edge is on the viewer's left. The leaf's collider gives the face.
    public const string RightLeafPath = "Facade/EntranceDoors/DoorRight";
    private const float PrySize = 0.12f, StileWidth = 0.07f, PryHeight = 1.05f;

    private static void PryMarks(List<Decal> list, Transform root)
    {
        Transform leaf = root.Find(RightLeafPath);
        BoxCollider box = leaf != null ? leaf.GetComponent<BoxCollider>() : null;
        if (box == null || !EvidenceCells.TryGetValue("PryMarks_DoorFrame", out Rect cell)) return;
        // The right leaf extends -x from its hinge; its street face is the collider's -z side.
        float edge = box.center.x - box.size.x * 0.5f, face = box.center.z - box.size.z * 0.5f, keep = StileWidth / PrySize;
        list.Add(new Decal
        {
            Name = "PryMarks", Cell = "PryMarks_DoorFrame", Uv = new Rect(cell.xMax - cell.width * keep, cell.y, cell.width * keep, cell.height),
            Centre = leaf.TransformPoint(V(edge + StileWidth * 0.5f, PryHeight, face - SurfaceLift)), Normal = leaf.TransformDirection(Vector3.back),
            Up = leaf.TransformDirection(Vector3.up), Size = new Vector2(StileWidth, PrySize), Mirror = true
        });
    }

    // Four 25 mm latent prints (the atlas's top row, one column each: loop, whorl, loop, arch)
    // as a right hand's fingertips near the front edge of the intact central case's top
    // (fingers into the case), and a latent left handprint (0.19 m, thumb on the right seen
    // from where the person stood: the case's front) on the cracked top of the left island's
    // front case. SurfaceLift over the top pane, whose face is the top of the case's glass.
    public const string PrintCase = "CentralCase_Middle", HandprintCase = NightStoreDressing.CrackedTop;
    private const float PrintSize = 0.025f;
    // Case space x, z, turn (degrees) and atlas column of each fingertip, index to little.
    private static readonly Vector4[] FingerTips =
    {
        new Vector4(0.100f, -0.175f, -10, 0), new Vector4(0.122f, -0.165f, -3, 1),
        new Vector4(0.143f, -0.172f, 4, 2), new Vector4(0.161f, -0.190f, 12, 3)
    };
    private static readonly Vector3 HandprintAt = V(0.35f, 0, -0.08f);

    private static void GlassPrints(List<Decal> list, Transform root)
    {
        Transform tips = root.Find("Showroom/" + PrintCase);
        if (GlassTop(tips, out float top) && EvidenceCells.TryGetValue("Fingerprints_Atlas", out Rect atlas))
            foreach (Vector4 tip in FingerTips)
            {
                Vector3 p = tips.TransformPoint(V(tip.x, 0, tip.y));
                p.y = top + SurfaceLift;
                list.Add(new Decal
                {
                    Name = "LatentPrint_" + (int)tip.w, Cell = "Fingerprints_Atlas", Size = new Vector2(PrintSize, PrintSize),
                    Uv = new Rect(atlas.x + atlas.width * 0.25f * tip.w, atlas.y + atlas.height * 0.5f, atlas.width * 0.25f, atlas.height * 0.5f),
                    Centre = p, Normal = tips.up, Up = tips.TransformDirection(Quaternion.Euler(0, tip.z, 0) * Vector3.forward)
                });
            }
        Transform hand = root.Find("Showroom/" + HandprintCase);
        if (GlassTop(hand, out top) && EvidenceCells.TryGetValue("GlassHandprint_Latent", out Rect palm))
        {
            Vector3 p = hand.TransformPoint(HandprintAt);
            p.y = top + SurfaceLift;
            list.Add(new Decal
            {
                Name = "Handprint", Cell = "GlassHandprint_Latent", Uv = palm, Size = Sizes["GlassHandprint_Latent"],
                Centre = p, Normal = hand.up, Up = hand.forward
            });
        }
    }

    // World height of a case's top glass face: the merged Glass (NightStoreBuilder.
    // MergeCaseGlass), or the Glass_Top pane before a merge, is the case's highest glass, so
    // its bounds top is the top pane's upper face. False without either.
    private static bool GlassTop(Transform fixture, out float top)
    {
        top = 0;
        if (fixture == null) return false;
        foreach (string name in new[] { "Glass", "Glass_Top" })
        {
            Transform t = fixture.Find(name);
            Renderer r = t != null ? t.GetComponent<Renderer>() : null;
            if (r == null) continue;
            top = r.bounds.max.y;
            return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- tools

    // The discarded tools (ForensicModels, Poly Haven CC0, Prop LOD levels, shadows on),
    // modelled upright (long axis y) and laid down by Euler inside their root, then centred on
    // their point and seated on the floor. The crowbar lies on its side, claw flat, on the
    // office floor between the tall safe and the locker (clear of the dropped transport tray at
    // -0.35, 10.75); the sledgehammer on the broad side of its head (turned about x: about z it
    // would stand on a striking face with the handle 8 cm up) in the glass beside the right
    // island; the screwdriver on the pavement outside the forced doors. The handle ends stay
    // 1-2 cm off the floor (a rigid pose on the head or handle, measured on the OBJs).
    private struct Tool
    {
        public string Model;
        public Vector3 Position, Euler;
        public float Yaw;
    }

    private static readonly Tool[] ToolTable =
    {
        new Tool { Model = "Crowbar", Position = V(-0.25f, 0, 11.13f), Yaw = 20, Euler = V(0, 0, 90) },
        new Tool { Model = "Sledgehammer", Position = V(5.6f, 0, 3.6f), Yaw = 70, Euler = V(90, 0, 0) },
        new Tool { Model = "FlatheadScrewdriver", Position = V(0.7f, 0, -0.7f), Yaw = -30, Euler = V(0, 0, 90) },
    };

    public static string[] ToolModels => System.Array.ConvertAll(ToolTable, t => t.Model);

    private static void PlaceTools(Transform root, Transform parent, string folder, bool imported)
    {
        foreach (Tool t in ToolTable)
        {
            GameObject g = imported ? DemoAssetLibrary.PlacePack(NightStreet.ForensicPack, t.Model, parent, Vector3.zero, t.Yaw, new DemoAssetLibrary.PackOptions
            {
                ModelEuler = t.Euler, Shadows = true, Lod = DemoAssetLibrary.LodClass.Prop
            }) : null;
            if (g == null) g = StandIn(t, parent, folder);
            g.name = t.Model;
            Vector3 point = root.TransformPoint(t.Position), centre = g.transform.TransformPoint(DemoAssetLibrary.LocalBounds(g).center);
            g.transform.position += new Vector3(point.x - centre.x, 0, point.z - centre.z);
            DemoAssetLibrary.Seat(g, point.y);
        }
    }

    // Without ForensicModels: a procedural tool in the pack's frame (upright, base at y 0, the
    // pack's measured sizes), so the same pose lays it down; palette materials, no new ones.
    private static GameObject StandIn(Tool t, Transform parent, string folder)
    {
        KitMesh k = new KitMesh { Lightmapped = false };
        switch (t.Model)
        {
            case "Crowbar":
                // 0.555 m bar with a flat chisel end and the claw bent over at the top.
                k.Box(StoreKit.FrameMetal, V(0, 0.26f, -0.03f), V(0.02f, 0.5f, 0.018f));
                k.Box(StoreKit.FrameMetal, V(0, 0.01f, -0.035f), V(0.032f, 0.02f, 0.03f));
                k.Box(StoreKit.FrameMetal, V(0, 0.53f, 0.005f), V(0.02f, 0.02f, 0.09f));
                k.Box(StoreKit.FrameMetal, V(0, 0.545f, 0.05f), V(0.04f, 0.02f, 0.03f));
                break;
            case "Sledgehammer":
                // Ash handle into a 0.21 x 0.146 x 0.09 m head at the top.
                k.Cylinder(StoreKit.Walnut, V(0, 0, 0), V(0, 0.9f, 0), 0.02f, 8);
                k.Box(StoreKit.SafeSteel, V(0, 0.933f, 0), V(0.21f, 0.146f, 0.09f));
                break;
            default:
                // 0.13 m handle, steel shaft and the flat blade.
                k.Cylinder(StoreKit.DeviceBlack, V(0, 0, 0), V(0, 0.13f, 0), 0.015f, 10);
                k.Cylinder(StoreKit.Stainless, V(0, 0.13f, 0), V(0, 0.255f, 0), 0.003f, 6);
                k.Box(StoreKit.Stainless, V(0, 0.258f, 0), V(0.007f, 0.006f, 0.0015f));
                break;
        }
        GameObject g = new GameObject(t.Model);
        g.transform.SetParent(parent, false);
        g.transform.localRotation = Quaternion.Euler(0, t.Yaw, 0);
        k.Emit("StandIn", g.transform, Vector3.zero, Quaternion.Euler(t.Euler), folder, "night_evidence_" + t.Model.ToLowerInvariant());
        return g;
    }

    // ---------------------------------------------------------------- sets

    // Dressing/PresenterSets/<name> with its PresenterSet (hidden at arrival and after every
    // session reset) and the Content it shows; returns the content.
    private static Transform NewSet(Transform sets, string name, string label, string note)
    {
        Transform holder = Group(name, sets);
        Transform content = Group(ContentName, holder);
        PresenterSet set = holder.gameObject.AddComponent<PresenterSet>();
        set.Label = label;
        set.Note = note;
        set.Content = content.gameObject;
        set.ShownAtStart = false;
        return content;
    }

    // After building (renderer bounds need it active): nothing in a set is static or in GI,
    // and the content starts inactive.
    private static void Close(Transform content)
    {
        foreach (MeshRenderer r in content.GetComponentsInChildren<MeshRenderer>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, 0);
            r.receiveGI = ReceiveGI.LightProbes;
        }
        content.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- emitting

    // The lightmap scale of an always-on group: the zone NightLighting.LightmapScales gives
    // its path (shell, street, near pavement, far pavement, neighbour terrace).
    public static float GroupScale(string path)
    {
        switch (path)
        {
            case FloorGroup: return NightLighting.ShellScale;
            case PavementGroup: return NightLighting.GroundScale;
            case FarPavementGroup: return NightLighting.FarScale;
            default: return NightLighting.StreetScale;
        }
    }

    // An always-on group at path: one lightmapped renderer on the group object at the zone's
    // scale (its chart margins sized for it), or with DecalsLightmapped false a group of
    // probe-lit cell renderers; batched either way, never an occluder or probe static.
    private static void EmitBaked(Transform root, string path, List<Decal> decals, Material m, string folder, string asset)
    {
        int slash = path.LastIndexOf('/');
        Transform parent = root.Find(path.Substring(0, slash));
        if (parent == null || decals.Count == 0) return;
        string name = path.Substring(slash + 1);
        if (!DecalsLightmapped)
        {
            EmitProbed(root, Group(name, parent), name, decals, m, folder, asset, true, false);
            return;
        }
        float scale = GroupScale(path);
        KitMesh k = new KitMesh { LightmapScale = scale };
        foreach (Decal d in decals) Add(k, m, parent, d);
        GameObject g = k.Emit(name, parent, Vector3.zero, Quaternion.identity, folder, asset);
        Setup(g, true, true, scale);
        Record(decals, path, false);
    }

    // Probe-lit decals under parent, one renderer <name>_<i>_<j> per LooseCell cell of the store
    // floor: a probe-lit mesh takes one light-probe sample at its bounds centre, so a cell keeps
    // the headlight falloff readable across the shop (as NightStoreDressing's loose cells).
    // batched: the always-on fallback (static batching, out of GI); otherwise never static.
    private static void EmitProbed(Transform root, Transform parent, string name, List<Decal> decals, Material m, string folder, string asset, bool batched, bool hidden)
    {
        SortedDictionary<string, List<Decal>> cells = new SortedDictionary<string, List<Decal>>(System.StringComparer.Ordinal);
        foreach (Decal d in decals)
        {
            Vector3 p = root.InverseTransformPoint(d.Centre);
            string key = Mathf.FloorToInt(p.x / NightStoreDressing.LooseCell) + "_" + Mathf.FloorToInt(p.z / NightStoreDressing.LooseCell);
            if (!cells.TryGetValue(key, out List<Decal> list)) cells[key] = list = new List<Decal>();
            list.Add(d);
        }
        foreach (KeyValuePair<string, List<Decal>> cell in cells)
        {
            KitMesh k = new KitMesh { Lightmapped = false };
            foreach (Decal d in cell.Value) Add(k, m, parent, d);
            Setup(k.Emit(name + "_" + cell.Key, parent, Vector3.zero, Quaternion.identity, folder, asset + "_" + cell.Key), batched, false, 0);
        }
        Record(decals, PathUnder(parent, root), hidden);
    }

    // Shadowless (a quad millimetres off its surface), receiving the torch's shadow, lightmapped
    // at the scale or probe-lit; batched decals are BatchingStatic and OccludeeStatic (plus
    // ContributeGI when lightmapped), never OccluderStatic or ReflectionProbeStatic.
    private static void Setup(GameObject g, bool batched, bool lightmapped, float scale)
    {
        MeshRenderer r = g.GetComponent<MeshRenderer>();
        if (r == null) return;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = true;
        r.receiveGI = lightmapped ? ReceiveGI.Lightmaps : ReceiveGI.LightProbes;
        if (lightmapped) r.scaleInLightmap = scale;
        StaticEditorFlags flags = 0;
        if (batched)
        {
            flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
            if (lightmapped) flags |= StaticEditorFlags.ContributeGI;
        }
        GameObjectUtility.SetStaticEditorFlags(g, flags);
    }

    // One decal into k as a quad in space's local frame (the emitted object sits at space's
    // origin): the image's +V along Up (made perpendicular to the normal), +U to the viewer's
    // right looking at the surface (flipped when mirrored).
    private static void Add(KitMesh k, Material m, Transform space, Decal d)
    {
        Vector3 n = d.Normal.normalized, u = Vector3.ProjectOnPlane(d.Up, n).normalized;
        Vector3 r = Vector3.Cross(n, u) * (d.Mirror ? -1 : 1);
        float hw = d.Size.x * 0.5f, hh = d.Size.y * 0.5f;
        Vector3 c = d.Centre;
        k.Quad(m, space.InverseTransformDirection(n), space.InverseTransformPoint(c - r * hw - u * hh), space.InverseTransformPoint(c + r * hw - u * hh),
            space.InverseTransformPoint(c + r * hw + u * hh), space.InverseTransformPoint(c - r * hw + u * hh), d.Uv);
    }

    private static void Record(List<Decal> decals, string group, bool hidden)
    {
        foreach (Decal d in decals)
            Decals.Add(new Placed
            {
                Name = d.Name, Cell = d.Cell, Group = group, Centre = d.Centre, Normal = d.Normal.normalized,
                Up = Vector3.ProjectOnPlane(d.Up, d.Normal).normalized, Size = d.Size, Uv = d.Uv, Hidden = hidden
            });
    }

    // A whole ambient cell lying flat at p (store-root space), its top turned yaw degrees from +z.
    private static void AddAmbient(List<Decal> list, Transform root, string cell, Vector3 p, float yaw, bool mirror = false)
    {
        if (AmbientCells.TryGetValue(cell, out Rect uv)) AddFlat(list, root, cell, cell + "_" + list.Count, uv, p, yaw, mirror);
    }

    private static void AddFlat(List<Decal> list, Transform root, string cell, string name, Rect uv, Vector3 p, float yaw, bool mirror = false)
        => list.Add(new Decal
        {
            Name = name, Cell = cell, Uv = uv, Size = Sizes[cell], Centre = root.TransformPoint(p), Normal = root.up,
            Up = root.TransformDirection(Quaternion.Euler(0, yaw, 0) * Vector3.forward), Mirror = mirror
        });

    // ---------------------------------------------------------------- for the validators

    // The UV rect of a cell from the last Apply's atlases (the whole texture for the shards).
    public static bool CellRect(string cell, out Rect rect)
    {
        rect = new Rect(0, 0, 1, 1);
        if (cell == ShardsFile) return HasShards;
        if (AmbientCells != null && AmbientCells.TryGetValue(cell, out rect)) return true;
        return EvidenceCells != null && EvidenceCells.TryGetValue(cell, out rect);
    }

    // The rects a decal material may sample: its atlas's cells, the whole shard texture; empty
    // for any other material.
    public static List<Rect> CellsOf(Material m)
    {
        List<Rect> rects = new List<Rect>();
        if (m == null) return rects;
        if (m.name == ShardsMaterial) rects.Add(new Rect(0, 0, 1, 1));
        else if (m.name == AmbientMaterial && AmbientCells != null) rects.AddRange(AmbientCells.Values);
        else if ((m.name == EvidenceMaterial || m.name == EvidenceGlassMaterial) && EvidenceCells != null) rects.AddRange(EvidenceCells.Values);
        return rects;
    }

    // Texture names that show a print, a footwear mark or a wipe, none of which may show at
    // arrival: the Evidence atlas, the SurfaceDecals print files, PBRInterior's GlassSmudge and
    // NightGraphics' illustrative ShoeTrace, Scuff and DustSmear.
    private static readonly string[] PrintPrefixes =
        { "Evidence_", "ShoePrint_", "Fingerprints_", "GlassHandprint", "DirtTracked", "GlassSmudge", "ShoeTrace", "Scuff", "DustSmear" };

    public static bool PrintLike(string texture)
    {
        if (string.IsNullOrEmpty(texture)) return false;
        foreach (string prefix in PrintPrefixes)
            if (texture.StartsWith(prefix, System.StringComparison.Ordinal)) return true;
        return false;
    }
}
#endif
