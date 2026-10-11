#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using L = NightStoreLayout;

// Night store generation and checks, in the ShopCityValidation pattern: generate the
// JewelryStoreNight scene, capture it, enter Play mode and run staged checks, then the
// shared tool, photo and reset stages with the night store's test points. RunNightQuick
// skips baking; RunNight is where the lighting batch adds the bake (same flow for now).
// Output goes to Docs/VerificationNight. Invoke either with -executeMethod.
[InitializeOnLoad]
public static class NightStoreValidation
{
    private const string Key = "NightStoreValidationStage";
    private const string BakeKey = "NightStoreValidationBake";
    private const string StatsKey = "NightStoreValidationStats";
    static NightStoreValidation() { EditorApplication.update += Tick; }

    public static void RunNightQuick() { Generate(false); }
    public static void RunNight() { Generate(true); }

    private static void Generate(bool bake)
    {
        try
        {
            SessionState.SetBool(BakeKey, bake);
            DemoValidation.Begin("VerificationNight");
            // Fixture library first (it builds and removes its galleries at the origin).
            StoreKitValidation.Check();
            GameObject root = NightStoreBuilder.Build(true);
            StaticChecks(root.transform);
            Camera camera = DemoValidation.CreatePreviewCamera(150);
            DemoValidation.SaveScene(L.SceneName);
            Captures(camera, root.transform);
            NightStoreBuilder.CreatePlayer(camera);
            SessionState.SetString(StatsKey, Stats(root.transform));
            DemoValidation.SaveScene();
            DemoValidation.EnterPlayMode(Key, "Night store generation" + (bake ? "" : " (no bake)") + " and 8 actual Unity rendered captures passed.");
        }
        catch (Exception ex) { DemoValidation.Finish(Key, false, ex.ToString()); }
    }

    // The eight concept views of Docs/LayoutReferences/NightStore (manifest.json), with
    // the temporary preview light; the ceiling and roof are hidden for the overview only.
    private static void Captures(Camera camera, Transform root)
    {
        Transform ceiling = root.Find(L.CeilingPath);
        ceiling.gameObject.SetActive(false);
        // Baked occlusion data ignores hidden objects, so the cutaway renders without it.
        bool occlusion = camera.useOcclusionCulling;
        camera.useOcclusionCulling = false;
        DemoValidation.Capture(camera, "night-01-cutaway-overview.png", new Vector3(-13, 17, -13), new Vector3(0.5f, 0, 4.5f));
        camera.useOcclusionCulling = occlusion;
        ceiling.gameObject.SetActive(true);
        DemoValidation.Capture(camera, "night-02-exterior-wide.png", new Vector3(0, 1.6f, -10), new Vector3(0, 1.5f, 3));
        DemoValidation.Capture(camera, "night-03-entrance-interior.png", new Vector3(0, 1.6f, -0.5f), new Vector3(0, 1.1f, 11));
        DemoValidation.Capture(camera, "night-04-left-aisle.png", new Vector3(-6.1f, 1.6f, 0.8f), new Vector3(-5.6f, 0.9f, 8));
        DemoValidation.Capture(camera, "night-05-right-aisle-seating.png", new Vector3(7.3f, 1.7f, 0.5f), new Vector3(1.5f, 0.8f, 6.5f));
        DemoValidation.Capture(camera, "night-06-central-display.png", new Vector3(0.3f, 1.4f, 3.2f), new Vector3(0, 0.75f, 5.2f));
        DemoValidation.Capture(camera, "night-07-office.png", new Vector3(0, 1.6f, 8.0f), new Vector3(0, 0.9f, 11.6f));
        DemoValidation.Capture(camera, "night-08-entrance-closeup.png", new Vector3(0.3f, 1.5f, -2.4f), new Vector3(0, 0.6f, 0.6f));
    }

    // The bake policy of StoreKit.MarkStatic as the builder applies it, in edit mode.
    private static void StaticChecks(Transform root)
    {
        StaticEditorFlags baked = StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic;
        bool shell = true;
        foreach (string path in new[] { "Architecture/Walls", "Architecture/Floor" })
        {
            MeshRenderer r = root.Find(path).GetComponent<MeshRenderer>();
            shell &= (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & baked) == baked && r.receiveGI == ReceiveGI.Lightmaps;
        }
        foreach (MeshRenderer r in root.Find(L.CeilingPath).GetComponentsInChildren<MeshRenderer>(true))
            shell &= (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.OccluderStatic) == 0;
        // Small props stay out of GI; GI contributors without a second UV set take probes.
        int small = 0, wrong = 0;
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            if ((flags & StaticEditorFlags.BatchingStatic) == 0) continue;
            Vector3 size = r.bounds.size;
            bool gi = (flags & StaticEditorFlags.ContributeGI) != 0;
            MeshFilter filter = r.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            bool uv2 = mesh != null && mesh.vertexCount > 0 && mesh.uv2.Length == mesh.vertexCount;
            if (size.x < StoreKit.SmallProp && size.y < StoreKit.SmallProp && size.z < StoreKit.SmallProp)
            {
                small++;
                if (gi) wrong++;
            }
            else if (gi && r.receiveGI == ReceiveGI.Lightmaps && !uv2) wrong++;
        }
        // Nothing moves at night: the open safe doors bake like the rest of the office.
        bool doors = true;
        foreach (string safe in new[] { "Office/SafeLarge/Door", "Office/SafeTall/Door" })
        {
            Transform door = root.Find(safe);
            bool any = false;
            if (door != null)
                foreach (MeshRenderer r in door.GetComponentsInChildren<MeshRenderer>(true))
                    any |= (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0;
            doors &= any;
        }
        bool fans = true;
        foreach (string name in NightStoreDressing.Unbatched)
        {
            Transform fan = root.Find("Dressing/" + name);
            fans &= fan != null && GameObjectUtility.GetStaticEditorFlags(fan.gameObject) == 0;
        }
        DemoValidation.Info("Static policy: small probe-lit props=" + small + "; violations=" + wrong
            + "; textured shell=" + (StoreKit.Floor.mainTexture != null) + "/" + (StoreKit.Plaster.mainTexture != null) + "/" + (StoreKit.Asphalt.mainTexture != null));
        DemoValidation.Check(shell && small > 0 && wrong == 0 && doors && fans, "Night store static flags follow the bake policy");
    }

    // Edit mode numbers (static batching merges meshes in Play mode, so count here).
    private static string Stats(Transform root)
    {
        int triangles = 0, renderers = 0, statics = 0, lightmapped = 0, lights = 0;
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter filter = r.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                for (int i = 0; i < filter.sharedMesh.subMeshCount; i++) triangles += (int)filter.sharedMesh.GetIndexCount(i) / 3;
            renderers++;
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            if ((flags & StaticEditorFlags.BatchingStatic) != 0) statics++;
            if ((flags & StaticEditorFlags.ContributeGI) != 0 && r.receiveGI == ReceiveGI.Lightmaps) lightmapped++;
        }
        lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        return "Night store triangles=" + triangles + "; renderers=" + renderers + "; static=" + statics + "; lightmapped=" + lightmapped
            + "; lights in scene=" + lights + " (temporary preview sun included)";
    }

    private static void Tick()
    {
        if (!DemoValidation.Running(Key, out int stage)) return;
        try
        {
            if (stage == 1)
            {
                DemoValidation.Info(SessionState.GetString(StatsKey, "stats missing"));
                DemoValidation.TestPoints = L.TestPoints();
                ZoneChecks();
                EntranceChecks();
                WalkChecks();
                TeleportChecks();
                PlacementChecks();
                GlassChecks();
                MapChecks();
                DemoValidation.ToolStage();
                SessionState.SetInt(Key, 2);
            }
            else if (stage == 2 && DemoValidation.PhotoStage()) SessionState.SetInt(Key, 3);
            else if (stage == 3 && DemoValidation.ResetStage()) DemoValidation.Finish(Key, true, null);
        }
        catch (Exception ex) { DemoValidation.Finish(Key, false, ex.ToString()); }
    }

    private static Transform Root()
    {
        GameObject root = GameObject.Find(L.Root);
        if (root == null) throw new Exception("FAILED: " + L.Root + " missing from scene");
        return root.transform;
    }

    private static void ZoneChecks()
    {
        Transform root = Root();
        bool zones = true;
        foreach (string zone in new[] { "Architecture", "Facade", "Exterior", "Showroom", "Office", "Dressing" })
            zones &= root.Find(zone) != null && root.Find(zone).GetComponentsInChildren<Renderer>().Length > 0;
        DemoValidation.Check(zones && root.Find("Anchors/PlayerSpawn") != null && root.Find("Exterior/PoliceCar/HeadlightLeft") != null,
            "Night store builds every zone");
        int safes = 0, open = 0;
        foreach (Transform t in root.Find("Office"))
        {
            if (!t.name.StartsWith("Safe")) continue;
            safes++;
            Transform door = t.Find("Door");
            if (door != null && Mathf.Abs(Mathf.DeltaAngle(door.localEulerAngles.y, 0)) > 45) open++;
        }
        DemoValidation.Check(safes == 2 && open == 2, "Office holds exactly two open safes");
    }

    // A walker-sized capsule passes the forced entrance; a ray at the middle of an intact
    // bay (clear of its mullions) is stopped by the pane's own thin collider.
    private static void EntranceChecks()
    {
        DemoValidation.Check(!Cast(0) && PaneStops(6), "Entrance is open between the forced door leaves");
    }

    private static bool PaneStops(int bay)
    {
        if (!Physics.Raycast(new Vector3(L.BayCentre(bay), 1.5f, -1), Vector3.forward, out RaycastHit hit, 1.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;
        BoxCollider box = hit.collider as BoxCollider;
        return box != null && hit.collider.transform == Root().Find("Facade/Bay_" + bay) && box.size.z < 0.02f && Mathf.Abs(hit.point.z) < 0.02f;
    }

    private static bool Cast(float x)
    {
        Vector3 bottom = new Vector3(x, 0.35f, -1), top = new Vector3(x, 1.6f, -1);
        return Physics.CapsuleCast(bottom, top, 0.23f, Vector3.forward, 2, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    private static void WalkChecks()
    {
        ShopWalkController walker = DemoValidation.Find<ShopWalkController>();
        walker.ReadDesktopInput = false;
        XRLocomotion locomotion = DemoValidation.Find<XRLocomotion>();
        walker.ResetPosition();
        DemoValidation.Check(Flat(walker.transform.position - L.PlayerSpawn) < 0.01f && walker.Bounds.Contains(L.PlayerSpawn),
            "Player spawns beside the police car");
        // The kerb is the only step on the route; the walker's capsule and the teleport both take it.
        CharacterController body = walker.GetComponent<CharacterController>();
        bool reached = body.stepOffset > -L.StreetY && locomotion.MaxStepHeight > -L.StreetY;
        string missed = null;
        foreach (Vector3 waypoint in L.Route)
        {
            WalkTo(walker, waypoint);
            if (Flat(walker.transform.position - waypoint) > 0.3f && missed == null) missed = waypoint.ToString();
        }
        reached &= missed == null;
        DemoValidation.Info("Walk route " + (missed == null ? "complete" : "stopped before " + missed + " at " + walker.transform.position));
        DemoValidation.Check(reached, "Visitor walks from the car to the safe");
        walker.ResetPosition();
        for (int i = 0; i < 200; i++) walker.MovePlanar(Vector3.back * 0.05f);
        DemoValidation.Check(walker.transform.position.z > L.StagingBay.MinZ - 0.05f && walker.Bounds.Contains(walker.transform.position),
            "Street beyond the staging bay stays out of reach");
        walker.ResetPosition();
    }

    private static void WalkTo(ShopWalkController walker, Vector3 target)
    {
        for (int i = 0; i < 400; i++)
        {
            Vector3 d = target - walker.transform.position;
            d.y = 0;
            if (d.magnitude < 0.03f) return;
            walker.MovePlanar(d.normalized * Mathf.Min(0.05f, d.magnitude));
        }
    }

    private static float Flat(Vector3 d) => new Vector2(d.x, d.z).magnitude;

    private static void TeleportChecks()
    {
        ShopWalkController walker = DemoValidation.Find<ShopWalkController>();
        XRLocomotion locomotion = DemoValidation.Find<XRLocomotion>();
        walker.ResetPosition();
        Vector3 head = locomotion.Head.position, pavement = new Vector3(0, 0, -1.5f), street = new Vector3(0, L.StreetY, -15);
        bool accepts = locomotion.FindDestination(new Ray(head, pavement - head), out Vector3 landing)
            && Flat(landing - pavement) < 0.05f && locomotion.TryTeleport(landing);
        walker.ResetPosition();
        bool refuses = !locomotion.FindDestination(new Ray(head, street - head), out _) && !locomotion.TryTeleport(street);
        walker.ResetPosition();
        DemoValidation.Check(accepts && refuses, "Teleport accepts the pavement and refuses the far street");
    }

    // The desktop pointer's carry-and-place path, pointed straight down.
    private static void PlacementChecks()
    {
        DesktopInteractor interactor = DemoValidation.Find<DesktopInteractor>();
        interactor.ReadDesktopInput = false;
        DemoValidation.Check(Place(interactor, new Vector3(3, 0, -1.5f)), "Tools can be placed on the pavement");
        DemoValidation.Check(Place(interactor, new Vector3(6.5f, L.StreetY, -4.0f)), "Tools can be placed in the staging bay");
    }

    private static bool Place(DesktopInteractor interactor, Vector3 floor)
    {
        DeployedTool cone = interactor.SpawnIntoHand(DemoToolKind.Cone);
        if (cone == null) return false;
        interactor.Carry(new Ray(floor + Vector3.up * 2, Vector3.down));
        interactor.Place();
        bool placed = interactor.Held == null && Vector3.Distance(cone.transform.position, floor) < 0.02f;
        interactor.Remove(cone);
        return placed;
    }

    // Every vertex of the entrance glass fans rests within 2 mm above the surface under it.
    private static void GlassChecks()
    {
        List<Vector3> points = NightStoreDressing.GlassVertices(Root().Find("Dressing"));
        int flush = 0;
        foreach (Vector3 p in points)
            if (Physics.Raycast(p + Vector3.up * 0.05f, Vector3.down, out RaycastHit hit, 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && p.y - hit.point.y >= -0.0005f && p.y - hit.point.y <= 0.002f) flush++;
        DemoValidation.Info("Entrance glass vertices=" + points.Count + "; flush=" + flush);
        DemoValidation.Check(points.Count > 300 && flush == points.Count, "Loose glass sits flush on its surfaces");
    }

    private static void MapChecks()
    {
        DemoOverviewMap map = DemoValidation.Find<DemoOverviewMap>();
        bool inside = true;
        foreach (Vector3 p in new[] { L.PlayerSpawn, new Vector3(0, 0, 11.5f), L.CarPosition, new Vector3(L.StagingBay.MinX, 0, L.StagingBay.MinZ) })
        {
            Vector2 uv = map.ToMap(p);
            inside &= uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1;
        }
        map.Render();
        DemoValidation.Check(inside && map.Texture != null, "Overview map frames the store and the staging bay");
    }
}
#endif
