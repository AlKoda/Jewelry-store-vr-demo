#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static DemoGeometry;
using L = NightStoreLayout;

// Generates the expanded night store from NightStoreLayout: root "NightStore" with
// Architecture (floor, walls, partitions, ceiling and its fittings), Facade (window
// bays, forced entrance, cladding), Exterior (pavement, kerb, wet street, buildings,
// bollards, streetlights, police car, equipment table and easel, play-area walls),
// Showroom and Office (StoreKit fixtures), Dressing (NightStoreDressing) and Anchors,
// the hidden UV trace examples (UVTraceBuilder), then the tool station with the night
// ToolLayout. CreatePlayer puts the rig at the spawn beside the car with the layout's
// movement regions.
//
// Surfaces are KitMesh parts built in world coordinates, so the metric UVs (and the
// ReferenceTextures tiling) run continuously across walls and slabs, with lightmap
// UVs; collisions are plain BoxColliders on "Colliders" children. Everything that
// never moves is marked static for the bake (StoreKit.MarkStatic).
public static class NightStoreBuilder
{
    public const string Folder = "Assets/CrimeSceneDemo/NightStoreGenerated";

    [MenuItem("Crime Scene Demo/Night Store/Build Night Store In New Scene")]
    public static void BuildMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Build(true);
        if (Camera.main != null) CreatePlayer(Camera.main);
    }

    public static GameObject Build(bool useImportedAssets)
    {
        if (GameObject.Find(L.Root) != null) throw new InvalidOperationException("Night store already exists. Build into a fresh scene.");
        EnsureFolder("Assets/CrimeSceneDemo");
        EnsureFolder(Folder);
        StoreKit.Palette();
        GameObject root = new GameObject(L.Root);
        Dictionary<string, Transform> zone = new Dictionary<string, Transform>();
        foreach (string name in L.Zones) zone[name] = Group(name, root.transform);

        Architecture(zone["Architecture"]);
        Facade(zone["Facade"]);
        Exterior(zone["Exterior"], useImportedAssets);
        foreach (L.Fixture f in L.Fixtures) Place(f, zone[f.Zone]);
        NightStoreDressing.Apply(zone["Dressing"], zone["Showroom"], Folder);
        foreach (string name in L.AnchorNames)
        {
            Transform anchor = Group(name, zone["Anchors"]);
            anchor.localPosition = L.Anchor(name);
            if (name == "PlayerSpawn") anchor.localRotation = Quaternion.Euler(0, L.PlayerYaw, 0);
        }

        // Nothing in the night scene moves at runtime (forced leaves, open safe doors,
        // the pulled drawer and the closed side-room doors stay put), so door leaves and
        // drawers bake with everything else; small parts still go to the probes.
        StoreKit.MarkStatic(zone["Architecture"].gameObject, true, true);
        // The ceiling slab is also the roof: as an occluder it would hide the whole
        // interior from the top-down map and overview cameras once occlusion is baked.
        foreach (MeshRenderer r in root.transform.Find(L.CeilingPath).GetComponentsInChildren<MeshRenderer>(true))
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, GameObjectUtility.GetStaticEditorFlags(r.gameObject) & ~StaticEditorFlags.OccluderStatic);
        StoreKit.MarkStatic(zone["Facade"].gameObject, true);
        StoreKit.MarkStatic(zone["Exterior"].gameObject, false);
        StoreKit.MarkStatic(zone["Exterior"].Find("Buildings").gameObject, false, true);
        StoreKit.MarkStatic(zone["Showroom"].gameObject, true);
        StoreKit.MarkStatic(zone["Office"].gameObject, true);
        StoreKit.MarkStatic(zone["Dressing"].gameObject, true);
        foreach (string name in NightStoreDressing.Unbatched)
        {
            Transform fan = zone["Dressing"].Find(name);
            if (fan != null) GameObjectUtility.SetStaticEditorFlags(fan.gameObject, 0);
        }
        // Exterior surfaces mostly out of view (far pavement, neighbour roofs and backs)
        // take a coarser lightmap; the near pavement keeps full density, the street 0.6
        // (NightLighting.LightmapScales sets every zone; the meshes' chart margins match).
        foreach (string path in new[] { "Ground/FarPavement", "Buildings/Neighbours" })
        {
            Transform t = zone["Exterior"].Find(path);
            MeshRenderer r = t != null ? t.GetComponent<MeshRenderer>() : null;
            if (r != null) r.scaleInLightmap = NightLighting.FarScale;
        }

        // Placeholder UV traces, hidden; the presenter panel shows them through DemoLighting.
        GameObject examples = UVTraceBuilder.NightExamples(root.transform);
        DemoToolsBuilder.Create(useImportedAssets, L.Tools);
        DemoLighting lighting = UnityEngine.Object.FindFirstObjectByType<DemoLighting>();
        if (lighting != null) lighting.UVExamples = examples;
        AssetDatabase.SaveAssets();
        return root;
    }

    public static ShopWalkController CreatePlayer(Camera camera)
        => DemoToolsBuilder.CreatePlayer(camera, L.PlayerSpawn, L.PlayerYaw, L.Regions());

    // ---------------------------------------------------------------- architecture

    private static void Architecture(Transform parent)
    {
        GameObject solid = Group("Colliders", parent).gameObject;
        KitMesh floor = new KitMesh(), walls = new KitMesh(), trim = new KitMesh(), ceiling = new KitMesh();
        float w = L.Wall, top = L.CeilingY, half = L.Partition * 0.5f, rz = L.RearZ, oh = L.OfficeHalf;
        Slab(floor, solid, StoreKit.Floor, V(L.MinX - w, -L.Slab, L.MinZ), V(L.MaxX + w, 0, L.MaxZ + w), false);

        // Outer walls stand outside the interior and rise to the parapet.
        Slab(walls, solid, StoreKit.Plaster, V(L.MinX - w, 0, L.MinZ), V(L.MinX, L.ParapetY, L.MaxZ + w));
        Slab(walls, solid, StoreKit.Plaster, V(L.MaxX, 0, L.MinZ), V(L.MaxX + w, L.ParapetY, L.MaxZ + w));
        Slab(walls, solid, StoreKit.Plaster, V(L.MinX - w, 0, L.MaxZ), V(L.MaxX + w, L.ParapetY, L.MaxZ + w));

        // Rear partitions with the side-room doors, the office front with its flush
        // opening, and the office side walls, all full height.
        float officeDoor = L.Find("OfficeDoor").Position.x, serviceDoor = L.Find("ServiceDoor").Position.x;
        Opening(walls, solid, L.MinX, -oh - half, rz, officeDoor, 0.45f, 2.1f);
        Opening(walls, solid, oh + half, L.MaxX, rz, serviceDoor, 0.45f, 2.1f);
        Opening(walls, solid, -oh - half, oh + half, rz, 0, L.OfficeOpeningHalf, L.OfficeOpeningHeight);
        for (int s = -1; s <= 1; s += 2)
            Slab(walls, solid, StoreKit.Plaster, V(s * oh - half, 0, rz), V(s * oh + half, top, L.MaxZ));

        // Walnut skirting along the showroom and office walls (gaps at the doors and the opening).
        const float sk = 0.1f, st = 0.015f;
        Slab(trim, null, StoreKit.Walnut, V(L.MinX, 0, L.MinZ), V(L.MinX + st, sk, rz - half));
        Slab(trim, null, StoreKit.Walnut, V(L.MaxX - st, 0, L.MinZ), V(L.MaxX, sk, rz - half));
        float[] front = { L.MinX, officeDoor - 0.52f, officeDoor + 0.52f, -L.OfficeOpeningHalf, L.OfficeOpeningHalf, serviceDoor - 0.52f, serviceDoor + 0.52f, L.MaxX };
        for (int i = 0; i + 1 < front.Length; i += 2)
            Slab(trim, null, StoreKit.Walnut, V(front[i], 0, rz - half - st), V(front[i + 1], sk, rz - half));
        for (int s = -1; s <= 1; s += 2)
            Slab(trim, null, StoreKit.Walnut, V(s * (oh - half) - (s > 0 ? st : 0), 0, rz + half), V(s * (oh - half) + (s < 0 ? st : 0), sk, L.MaxZ));
        Slab(trim, null, StoreKit.Walnut, V(-oh + half, 0, L.MaxZ - st), V(oh - half, sk, L.MaxZ));

        floor.Emit("Floor", parent, Vector3.zero, Quaternion.identity, Folder, "night_floor");
        walls.Emit("Walls", parent, Vector3.zero, Quaternion.identity, Folder, "night_walls");
        trim.Emit("Skirting", parent, Vector3.zero, Quaternion.identity, Folder, "night_skirting");

        // Ceiling slab (its top is the roof) with the fittings, all off. The overview
        // capture and the presenter map hide this whole group.
        Transform ceilingGroup = Group("Ceiling", parent);
        GameObject ceilingSolid = Group("Colliders", ceilingGroup).gameObject;
        Slab(ceiling, ceilingSolid, StoreKit.CeilingPaint, V(L.MinX, top, L.MinZ), V(L.MaxX, top + L.Slab, L.MaxZ));
        ceiling.Emit("CeilingSlab", ceilingGroup, Vector3.zero, Quaternion.identity, Folder, "night_ceiling");
        Vector3[] downlights = L.Downlights();
        for (int i = 0; i < downlights.Length; i++)
            StoreKit.Instantiate("downlight", ceilingGroup, downlights[i], 0).name = "Downlight_" + i;
        for (int i = 0; i < L.Tracks.Length; i++)
            StoreKit.Instantiate("track_light", ceilingGroup, L.Tracks[i], L.Tracks[i].w).name = "TrackLight_" + i;
    }

    // A full-height partition along x at z with one opening (gapCentre +- gapHalf, up to gapTop).
    private static void Opening(KitMesh k, GameObject solid, float x0, float x1, float z, float gapCentre, float gapHalf, float gapTop)
    {
        float half = L.Partition * 0.5f, top = L.CeilingY;
        Slab(k, solid, StoreKit.Plaster, V(x0, 0, z - half), V(gapCentre - gapHalf, top, z + half));
        Slab(k, solid, StoreKit.Plaster, V(gapCentre + gapHalf, 0, z - half), V(x1, top, z + half));
        Slab(k, solid, StoreKit.Plaster, V(gapCentre - gapHalf, gapTop, z - half), V(gapCentre + gapHalf, top, z + half));
    }

    // Axis-aligned box from min to max into k, with a matching collider on solid (when given).
    private static void Slab(KitMesh k, GameObject solid, Material m, Vector3 min, Vector3 max, bool bottom = true)
    {
        Vector3 centre = (min + max) * 0.5f, size = max - min;
        k.Box(m, centre, size, bottom);
        if (solid == null) return;
        BoxCollider c = solid.AddComponent<BoxCollider>();
        c.center = centre;
        c.size = size;
    }

    // ---------------------------------------------------------------- facade

    private static void Facade(Transform parent)
    {
        for (int bay = 0; bay < L.Bays; bay++)
        {
            if (L.IsEntranceBay(bay)) continue;
            GameObject module = StoreKit.WindowModule(parent, L.BayState(bay));
            module.name = "Bay_" + bay;
            module.transform.localPosition = V(L.BayCentre(bay), 0, 0);
        }
        float sx = L.DoorHalf + L.SidelightWidth * 0.5f;
        GameObject left = StoreKit.WindowModule(parent, L.LeftSidelight, L.SidelightWidth);
        left.name = "Sidelight_Left";
        left.transform.localPosition = V(-sx, 0, 0);
        GameObject right = StoreKit.WindowModule(parent, L.RightSidelight, L.SidelightWidth);
        right.name = "Sidelight_Right";
        right.transform.localPosition = V(sx, 0, 0);
        GameObject doors = StoreKit.StorefrontDoors(parent, L.DoorLeftOpen, L.DoorRightOpen, true, true);
        doors.name = "EntranceDoors";

        // Transom panel over the doors, then the stone-clad end piers, fascia and coping.
        GameObject solid = Group("Colliders", parent).gameObject;
        KitMesh frame = new KitMesh { LightmapScale = NightLighting.FixtureScale }, stone = new KitMesh { LightmapScale = NightLighting.FixtureScale };
        Slab(frame, solid, StoreKit.FrameMetal, V(-L.DoorHalf, L.DoorHeight, -0.06f), V(L.DoorHalf, L.CeilingY, 0.06f));
        frame.Emit("Transom", parent, Vector3.zero, Quaternion.identity, Folder, "night_transom");
        const float front = -0.2f, back = 0.25f;
        float pier = L.Pier;
        for (int s = -1; s <= 1; s += 2)
            Slab(stone, solid, StoreKit.Cladding, V(s < 0 ? L.MinX - pier : L.MaxX, 0, front), V(s < 0 ? L.MinX : L.MaxX + pier, L.ParapetY, back));
        Slab(stone, solid, StoreKit.Cladding, V(L.MinX, L.CeilingY, front), V(L.MaxX, L.ParapetY, back));
        Slab(stone, null, StoreKit.Concrete, V(L.MinX - pier - 0.05f, L.ParapetY, front - 0.05f), V(L.MaxX + pier + 0.05f, L.ParapetY + 0.06f, back + 0.05f));
        stone.Emit("Cladding", parent, Vector3.zero, Quaternion.identity, Folder, "night_cladding");
    }

    // ---------------------------------------------------------------- exterior

    private static void Exterior(Transform parent, bool imported)
    {
        Transform ground = Group("Ground", parent);
        GameObject solid = Group("Colliders", ground).gameObject;
        KitMesh paving = new KitMesh(), kerbs = new KitMesh(), road = new KitMesh { LightmapScale = NightLighting.StreetScale }, far = new KitMesh { LightmapScale = NightLighting.FarScale };
        float hx = L.StreetHalfX, kerb = 0.18f;
        // Near pavement and kerb (the 0.12 m step down to the street), wet street, far side.
        Slab(paving, solid, StoreKit.Pavement, V(-hx, -0.3f, L.KerbZ + kerb), V(hx, 0, L.MinZ), false);
        Slab(kerbs, solid, StoreKit.Concrete, V(-hx, -0.3f, L.KerbZ), V(hx, 0, L.KerbZ + kerb), false);
        Slab(road, solid, StoreKit.Asphalt, V(-hx, L.StreetY - 0.3f, L.FarKerbZ), V(hx, L.StreetY, L.KerbZ), false);
        Slab(kerbs, solid, StoreKit.Concrete, V(-hx, -0.3f, L.FarKerbZ - kerb), V(hx, 0, L.FarKerbZ), false);
        // The far pavement runs on under the buildings across the street, ending at their
        // backs (the skyline towers stand on nothing anyone sees); it bakes coarsely.
        Slab(far, solid, StoreKit.Pavement, V(-hx, -0.3f, L.FarPavementZ - 10), V(hx, 0, L.FarKerbZ - kerb), false);
        // Dashed centre line, 2 mm proud of the asphalt.
        float lineZ = (L.KerbZ + L.FarKerbZ) * 0.5f, y = L.StreetY + 0.002f;
        for (float x = -hx + 1; x + 2 <= hx; x += 4)
            road.Face(StoreKit.RoadLine, Vector3.up, V(x, y, lineZ - 0.06f), V(x + 2, y, lineZ - 0.06f), V(x + 2, y, lineZ + 0.06f), V(x, y, lineZ + 0.06f));
        paving.Emit("Pavement", ground, Vector3.zero, Quaternion.identity, Folder, "night_pavement");
        kerbs.Emit("Kerbs", ground, Vector3.zero, Quaternion.identity, Folder, "night_kerbs");
        road.Emit("Street", ground, Vector3.zero, Quaternion.identity, Folder, "night_street");
        far.Emit("FarPavement", ground, Vector3.zero, Quaternion.identity, Folder, "night_far_pavement");

        Transform bollards = Group("Bollards", parent);
        for (int i = 0; i < L.BollardX.Length; i++)
            StoreKit.Instantiate("bollard", bollards, V(L.BollardX[i], 0, L.BollardZ), 0).name = "Bollard_" + i;
        Streetlights(Group("Streetlights", parent), imported);
        PoliceCar(parent, imported);
        Equipment(Group("Equipment", parent));
        Buildings(Group("Buildings", parent), imported);
        // Kit props share the atlas with shadows off; the car, lamp posts and towers need
        // their contact shadows on the wet street under the headlights and streetlights.
        foreach (string name in new[] { "PoliceCar", "Streetlights", "Buildings" })
            foreach (MeshRenderer r in parent.Find(name).GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.sharedMaterial != null && r.sharedMaterial.renderQueue >= 3000) continue;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }

        GameObject walls = Group("Boundary", parent).gameObject;
        for (int i = 0; i + 1 < L.BoundaryWalls.Length; i += 2)
        {
            BoxCollider c = walls.AddComponent<BoxCollider>();
            c.center = L.BoundaryWalls[i];
            c.size = L.BoundaryWalls[i + 1];
        }
    }

    // KayKit streetlights (light sources are added by the lighting pass at the
    // StreetlightHead anchors); a post, arm and head stand in without the pack.
    private static void Streetlights(Transform parent, bool imported)
    {
        for (int i = 0; i < L.Streetlights.Length; i++)
        {
            Transform lamp = Group("Streetlight_" + i, parent);
            lamp.localPosition = L.Streetlights[i];
            float yaw = L.StreetlightYaw(i);
            GameObject model = imported ? DemoAssetLibrary.Place(ShopCityRefinement.KitPack, "streetlight", lamp, Vector3.zero, yaw, ShopCityRefinement.KitScale) : null;
            if (model == null)
            {
                KitMesh k = new KitMesh { LightmapScale = NightLighting.KitScale };
                k.Box(StoreKit.FrameMetal, V(0, 2.15f, 0), V(0.12f, 4.3f, 0.12f));
                Quaternion turn = Quaternion.Euler(0, yaw, 0);
                k.Box(StoreKit.FrameMetal, turn * V(-0.5f, 4.3f, 0), V(1.0f, 0.08f, 0.08f), turn);
                k.Box(StoreKit.Lacquer, turn * V(-1.0f, 4.22f, 0), V(0.45f, 0.1f, 0.22f), turn);
                k.Emit("StreetlightStandIn", lamp, Vector3.zero, Quaternion.identity, Folder, "night_streetlight");
            }
            BoxCollider post = lamp.gameObject.AddComponent<BoxCollider>();
            post.center = V(0, 1.5f, 0);
            post.size = V(0.2f, 3f, 0.2f);
        }
    }

    // KayKit police car (nose +z, see NightStoreLayout) or ShopCityRefinement's stand-in,
    // whose nose is +x and so is turned -90 to match; one box collider round the body.
    private static void PoliceCar(Transform parent, bool imported)
    {
        Transform car = Group("PoliceCar", parent);
        car.localPosition = L.CarPosition;
        car.localRotation = Quaternion.Euler(0, L.CarYaw, 0);
        bool kit = imported && DemoAssetLibrary.Has(ShopCityRefinement.KitPack, "car_police");
        Transform model = ShopCityRefinement.Car(car, Vector3.zero, StoreKit.Lacquer, "car_police", 0, imported);
        if (!kit) model.localRotation = Quaternion.Euler(0, -90, 0);
        DemoAssetLibrary.AddBoundsCollider(car.gameObject);
        Group("HeadlightLeft", car).localPosition = L.HeadlightLeft;
        Group("HeadlightRight", car).localPosition = L.HeadlightRight;
        Group("LightBar", car).localPosition = L.LightBar;
    }

    // Folding table (top at TableTop) the tool samples and the camera rest on, and the
    // easel board carrying the photo frame and the status text.
    private static void Equipment(Transform parent)
    {
        Transform table = Group("FoldingTable", parent);
        table.localPosition = L.Table;
        KitMesh k = new KitMesh { LightmapScale = NightLighting.KitScale };
        float tw = L.TableWidth, td = L.TableDepth, th = L.TableTop;
        k.Box(StoreKit.PaintedGrey, V(0, th - 0.015f, 0), V(tw, 0.03f, td));
        for (int s = -1; s <= 1; s += 2)
        {
            for (int t = -1; t <= 1; t += 2)
                k.Box(StoreKit.FrameMetal, V(s * (tw * 0.5f - 0.12f), (th - 0.03f) * 0.5f, t * (td * 0.5f - 0.06f)), V(0.025f, th - 0.03f, 0.025f));
            k.Box(StoreKit.FrameMetal, V(s * (tw * 0.5f - 0.12f), 0.12f, 0), V(0.02f, 0.02f, td - 0.12f));
        }
        k.Emit("Body", table, Vector3.zero, Quaternion.identity, Folder, "night_table");
        BoxCollider top = table.gameObject.AddComponent<BoxCollider>();
        top.center = V(0, th * 0.5f, 0);
        top.size = V(tw, th, td);

        Transform easel = Group("Easel", parent);
        easel.localPosition = L.Easel;
        KitMesh e = new KitMesh { LightmapScale = NightLighting.KitScale };
        float bw = L.EaselWidth, b0 = L.EaselBottom, b1 = L.EaselTop;
        e.Box(StoreKit.DeviceBlack, V(0, (b0 + b1) * 0.5f, 0), V(bw, b1 - b0, 0.03f));
        for (int s = -1; s <= 1; s += 2)
        {
            e.Box(StoreKit.FrameMetal, V(s * (bw * 0.5f - 0.05f), b1 * 0.5f, 0.035f), V(0.04f, b1, 0.04f));
            e.Box(StoreKit.FrameMetal, V(s * (bw * 0.5f - 0.05f), 0.02f, 0.035f), V(0.05f, 0.04f, 0.6f));
        }
        e.Emit("Body", easel, Vector3.zero, Quaternion.identity, Folder, "night_easel");
        BoxCollider board = easel.gameObject.AddComponent<BoxCollider>();
        board.center = V(0, b1 * 0.5f, 0.02f);
        board.size = V(bw, b1, 0.1f);
    }

    // Terrace neighbours either side of the shop and a row across the street, simple
    // render-and-glass blocks with a few lit rooms (baked emitters); KayKit towers
    // behind them for the skyline when the pack is present.
    private static void Buildings(Transform parent, bool imported)
    {
        System.Random rng = new System.Random(5);
        KitMesh near = new KitMesh { LightmapScale = NightLighting.FarScale }, across = new KitMesh { Lightmapped = false };
        GameObject solid = Group("Colliders", parent).gameObject;
        foreach (float s in new[] { -1f, 1f })
        {
            float x0 = s < 0 ? -20 : L.MaxX + L.Pier, x1 = s < 0 ? L.MinX - L.Pier : 20;
            Slab(near, solid, StoreKit.Render, V(x0, 0, L.MinZ), V(x1, 7.5f, 14), false);
            Shopfront(near, x0, x1, L.MinZ, -1);
            Windows(near, rng, x0, x1, L.MinZ, -1, 7.5f, 0.25f);
        }
        float[] edges = { -26, -15.2f, -14.8f, -4.2f, -3.8f, 7.2f, 7.6f, 18.2f, 18.6f, 28 };
        float[] heights = { 9, 12, 10.5f, 13.5f, 9 };
        for (int i = 0; i < heights.Length; i++)
        {
            float x0 = edges[2 * i], x1 = edges[2 * i + 1];
            across.Box(StoreKit.Render, V((x0 + x1) * 0.5f, heights[i] * 0.5f, L.FarPavementZ - 5), V(x1 - x0, heights[i], 10), false);
            Shopfront(across, x0, x1, L.FarPavementZ, 1);
            Windows(across, rng, x0, x1, L.FarPavementZ, 1, heights[i], 0.2f);
        }
        near.Emit("Neighbours", parent, Vector3.zero, Quaternion.identity, Folder, "night_neighbours");
        across.Emit("AcrossStreet", parent, Vector3.zero, Quaternion.identity, Folder, "night_across");
        if (!imported) return;
        string[] towers = { "building_H_withoutBase", "building_C_withoutBase", "building_D_withoutBase", "building_F_withoutBase" };
        for (int i = 0; i < towers.Length; i++)
            DemoAssetLibrary.Place(ShopCityRefinement.KitPack, towers[i], parent, V(-20 + i * 14, -0.7f, L.FarPavementZ - 16), 0, 7);
    }

    // Ground-floor shop: dark glass band, a half-closed roller shutter and a fascia board.
    // facing is the facade normal's z (-1 towards the near street side, +1 across).
    private static void Shopfront(KitMesh k, float x0, float x1, float z, float facing)
    {
        float f = z + facing * 0.01f, mid = (x0 + x1) * 0.5f;
        Vector3 n = V(0, 0, facing);
        k.Face(StoreKit.WindowDark, n, V(x0 + 0.6f, 0.3f, f), V(x1 - 0.6f, 0.3f, f), V(x1 - 0.6f, 2.8f, f), V(x0 + 0.6f, 2.8f, f));
        f += facing * 0.005f;
        k.Face(StoreKit.PaintedGrey, n, V(mid, 0.3f, f), V(x1 - 0.6f, 0.3f, f), V(x1 - 0.6f, 2.8f, f), V(mid, 2.8f, f));
        k.Box(StoreKit.FrameMetal, V(mid, 3.2f, z + facing * 0.06f), V(x1 - x0 - 0.8f, 0.6f, 0.12f));
    }

    // Upper-floor window grid on a facade, about litChance of the rooms lit.
    private static void Windows(KitMesh k, System.Random rng, float x0, float x1, float z, float facing, float height, float litChance)
    {
        float f = z + facing * 0.01f;
        for (float y = 4.2f; y + 1.5f <= height - 0.8f; y += 3.0f)
            for (float x = x0 + 1.0f; x + 1.2f <= x1 - 0.8f; x += 2.4f)
            {
                Material m = rng.NextDouble() < litChance ? StoreKit.WindowLit : StoreKit.WindowDark;
                k.Face(m, V(0, 0, facing), V(x, y, f), V(x + 1.2f, y, f), V(x + 1.2f, y + 1.5f, f), V(x, y + 1.5f, f));
            }
    }

    // ---------------------------------------------------------------- fixtures

    // A layout fixture: case, safe and door states come from the dressing's case state,
    // everything else through StoreKit.Instantiate (user-converted model when present).
    private static void Place(L.Fixture f, Transform parent)
    {
        GameObject g;
        switch (f.Kit)
        {
            case "display_case":
                g = StoreKit.DisplayCase(parent, NightStoreDressing.CaseGlass(f.Name), NightStoreDressing.HasDrawer(f.Name), NightStoreDressing.DrawerOut(f.Name));
                break;
            case "wall_case": g = StoreKit.WallCase(parent, NightStoreDressing.CaseGlass(f.Name)); break;
            case "safe_large": g = StoreKit.SafeLarge(parent, NightStoreDressing.SafeOpen(f.Name)); break;
            case "safe_tall": g = StoreKit.SafeTall(parent, NightStoreDressing.SafeOpen(f.Name)); break;
            case "office_door": g = StoreKit.OfficeDoor(parent, 0); break;
            case "service_door": g = StoreKit.ServiceDoor(parent, 0); break;
            default: g = StoreKit.Instantiate(f.Kit, parent, f.Position, f.Yaw); break;
        }
        g.name = f.Name;
        g.transform.localPosition = f.Position;
        g.transform.localRotation = Quaternion.Euler(0, f.Yaw, 0);
    }
}
#endif
