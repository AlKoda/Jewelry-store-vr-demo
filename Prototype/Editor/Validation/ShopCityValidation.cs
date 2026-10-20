#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Refined shop with city backdrop, optionally with the presentation expansion.
// Adds walking, locomotion, pointer, hand, backdrop and expansion checks before
// the shared tool checks. Invoke Run or RunExpanded with -executeMethod.
[InitializeOnLoad]
public static class ShopCityValidation
{
    private const string Key = "ShopCityValidationStage";
    private const string ExpandedKey = "ShopCityValidationExpanded";
    private const string FormsKey = "ShopCityValidationForms";
    private const string GlassKey = "ShopCityValidationGlass";
    private const string RobberyKey = "ShopCityValidationRobbery";
    private const string InteriorKey = "ShopCityValidationInterior";
    private const string TexturedKey = "ShopCityValidationTextured";
    private const string AssetsKey = "ShopCityValidationAssets";
    private const string TexturedCountKey = "ShopCityValidationTexturedCount";
    static ShopCityValidation() { EditorApplication.update += Tick; }

    public static void Run() { Generate(false); }
    public static void RunExpanded() { Generate(true); }
    public static void RunForms() { Generate(true, true); }
    public static void RunGlass() { Generate(true,true,true,true,true); }
    public static void RunRobbery() { Generate(true, true, true, true); }
    public static void RunInterior() { Generate(true, true, true); }
    // Full variant: finished procedural shop plus textures, imported models and HDR environment.
    public static void RunTextured() { Generate(true, true, true, true, true, true, true); }

    private static void Generate(bool expanded, bool forms = false, bool interior = false, bool robbery = false, bool glass = false,
        bool textured = false, bool? importedAssets = null)
    {
        // Historically the forms flag also meant "no imported models"; RunTextured brings them back.
        bool assets = importedAssets ?? !forms;
        try
        {
            SessionState.SetBool(AssetsKey, assets);
            SessionState.SetBool(TexturedKey, textured);
            SessionState.SetBool(GlassKey,glass);
            SessionState.SetBool(RobberyKey, robbery);
            SessionState.SetBool(ExpandedKey, expanded);
            SessionState.SetBool(FormsKey, forms);
            SessionState.SetBool(InteriorKey, interior);
            DemoValidation.Begin(textured ? "VerificationTextured" : glass ? "VerificationGlass" : robbery ? "VerificationRobbery" : interior ? "VerificationInterior" : forms ? "VerificationForms" : expanded ? "VerificationExpanded" : "VerificationCity");
            DemoValidation.LauncherChecks();
            JewelryStoreBuilder.CreateStore();
            DemoToolsBuilder.Create(assets);
            ShopCityRefinement.Apply(assets);
            if (expanded)
            {
                ShopPresentationExpansion.Apply();
                if (assets) ShopAssetDressing.Apply();
                IntactStateBuilder.Apply();
                if (forms) ShopFormRefinement.Apply();
            }
            Camera camera = DemoValidation.CreatePreviewCamera(150);
            if (interior) { ShopInteriorFinish.ApplyShell(); ShopDisplayFinish.Apply(); }
            if (robbery) ShopRobberyDressing.Apply();
            if (textured) SessionState.SetInt(TexturedCountKey, ShopTextureFinish.Apply());
            if(glass) ShopGlassRefinement.Apply();
            DemoValidation.SaveScene(textured ? "JewelryStoreTextured" : glass ? "JewelryStoreGlass" : robbery ? "JewelryStoreRobbery" : interior ? "JewelryStoreInterior" : forms ? "JewelryStoreForms" : expanded ? "JewelryStoreExpanded" : "JewelryStoreCity");
            DemoValidation.Capture(camera, "showroom.png", new Vector3(3.6f, 1.65f, 0.8f), new Vector3(-0.2f, 1, 4.5f));
            if (expanded) DemoValidation.Capture(camera, "display-details.png", new Vector3(-.4f, 1.6f, 2), new Vector3(-2, 1, 3.7f));
            DemoValidation.Capture(camera, "street-from-inside.png", new Vector3(0, 1.65f, 1.5f), new Vector3(0, 2, -18));
            DemoValidation.Capture(camera, "safe-room.png", new Vector3(3.4f, 1.65f, 8.8f), new Vector3(2.1f, 0.9f, 10.5f));
            if (interior) DemoValidation.Capture(camera, "entrance.png", new Vector3(-.2f, 1.65f, 2.1f), new Vector3(2.2f, 1.2f, -.05f));
            if (robbery)
            {
                var state=GameObject.Find("JewelryStore_Blockout").GetComponent<CrimeSceneState>();
                state.SetIntact(true);
                DemoValidation.Capture(camera,"intact-comparison.png",new Vector3(-.4f,1.6f,2),new Vector3(-2,1,3.7f));
                state.ShowRobbed();
            }
            DemoValidation.CaptureOverview(camera);
            if(glass) GlassToolValidation.Capture(camera);
            DemoToolsBuilder.CreatePlayer(camera);
            // CreatePlayer resets the camera to a solid clear; the textured shop keeps the HDR sky.
            if (textured && ShopTextureFinish.HasEnvironment && RenderSettings.skybox != null) camera.clearFlags = CameraClearFlags.Skybox;
            DemoValidation.SaveScene();
            DemoValidation.EnterPlayMode(Key, (textured ? "Textured scene generation and 10" : glass ? "Glass scene generation and 10" : robbery ? "Robbery scene generation and 7" : interior ? "Interior scene generation and 6" : expanded ? "Expanded scene generation and 5" : "City scene generation and 4")
                + " actual Unity rendered captures passed.");
        }
        catch (Exception ex) { DemoValidation.Finish(Key, false, ex.ToString()); }
    }

    private static void Tick()
    {
        if (!DemoValidation.Running(Key, out int stage)) return;
        try
        {
            if (stage == 1)
            {
                if (SessionState.GetBool(RobberyKey, false)) ShopRobberyDressing.Check();
                if(SessionState.GetBool(GlassKey,false)) GlassToolValidation.Check();
                if (SessionState.GetBool(GlassKey, false))
                {
                    // The shop's StyleSceneTape path passes no text: both warnings keep the default wording.
                    SceneTape shopTape = DemoValidation.Find<ToolStation>().TapePrefab;
                    DemoValidation.Check(shopTape != null && shopTape.FrontWarning != null && shopTape.BackWarning != null
                        && shopTape.FrontWarning.text == DemoToolsBuilder.DefaultTapeText && shopTape.BackWarning.text == DemoToolsBuilder.DefaultTapeText,
                        "Shop tape keeps the CRIME SCENE text");
                }
                WalkingChecks();
                LocomotionChecks();
                InteractionChecks();
                LightingChecks();
                PanelChecks();
                HandChecks();
                FreeHoldAndWheelChecks();
                VRComfortChecks();
                QuestStandaloneChecks();
                DemoValidation.InterfaceChecks();
                BackdropChecks();
                if (SessionState.GetBool(FormsKey, false)) ShopFormRefinement.CheckBudget();
                if (SessionState.GetBool(InteriorKey, false)) { ShopInteriorFinish.CheckBudget(); ShopDisplayFinish.CheckBudget(); }
                if (SessionState.GetBool(TexturedKey, false)) { ShopTextureFinish.CheckBudget(SessionState.GetInt(TexturedCountKey, 0)); GeneratedMeshChecks(); }
                if (SessionState.GetBool(ExpandedKey, false)) ExpansionChecks();
                DemoValidation.ToolStage();
                SessionState.SetInt(Key, 2);
            }
            else if (stage == 2 && DemoValidation.PhotoStage()) SessionState.SetInt(Key, 3);
            else if (stage == 3 && DemoValidation.ResetStage()) DemoValidation.Finish(Key, true, null);
        }
        catch (Exception ex) { DemoValidation.Finish(Key, false, ex.ToString()); }
    }

    private static void WalkingChecks()
    {
        ShopWalkController walker = DemoValidation.Find<ShopWalkController>();
        DemoValidation.Check(walker.transform.position.z > 0, "Player spawns inside");
        walker.ReadDesktopInput = false;
        Vector3 start = walker.transform.position;
        walker.MovePlanar(Vector3.forward * .5f);
        DemoValidation.Check(walker.transform.position.z > start.z + .3f, "Desktop walking moves player");
        Walk(walker, Vector3.back, 100);
        DemoValidation.Check(walker.transform.position.z >= .29f, "Entrance boundary keeps player inside");
        Walk(walker, Vector3.right, 80);
        DemoValidation.Check(walker.transform.position.x < 4.8f, "Side wall keeps player inside");
        walker.ResetPosition();
        for (int i = 0; i < 19; i++) walker.MovePlanar(Vector3.left * .05f);
        for (int i = 0; i < 190; i++) walker.MovePlanar(Vector3.forward * .05f);
        DemoValidation.Check(walker.transform.position.z > 8.3f, "Rear safe room remains accessible");
        walker.ResetPosition();
        GameObject boundary = GameObject.Find("IndoorBoundary_Front");
        DemoValidation.Check(boundary.GetComponent<BoxCollider>() != null && boundary.GetComponent<Renderer>() == null,
            "Front boundary blocks movement without blocking view");
        InteriorBounds bounds = walker.Bounds;
        DemoValidation.Check(bounds != null && bounds.Contains(new Vector3(0, 0, 4)) && !bounds.Contains(new Vector3(0, 0, -2))
            && Mathf.Abs(bounds.Clamp(new Vector3(-4, 0, 9.5f)).z - 8.25f) < 0.001f,
            "Interior bounds reject the street and clamp beside the safe room");
    }

    private static void Walk(ShopWalkController walker, Vector3 direction, int steps)
    {
        walker.ResetPosition();
        for (int i = 0; i < steps; i++) walker.MovePlanar(direction * .05f);
    }

    private static void LocomotionChecks()
    {
        ShopWalkController walker = DemoValidation.Find<ShopWalkController>();
        XRLocomotion locomotion = DemoValidation.Find<XRLocomotion>();
        Transform head = locomotion.Head;
        DemoValidation.Check(!locomotion.TryTeleport(new Vector3(0, 0, -3)), "Teleport refuses the street");
        DemoValidation.Check(locomotion.TryTeleport(new Vector3(-2, 0, 6))
            && Mathf.Abs(head.position.x + 2) < 0.01f && Mathf.Abs(head.position.z - 6) < 0.01f,
            "Teleport lands the head over the target");
        DemoValidation.Check(!locomotion.TryTeleport(new Vector3(-2, 1, 6)), "Teleport refuses surfaces above step height");
        // Offset the head as a headset would, so the pivot maths is exercised.
        Vector3 headLocal = head.localPosition;
        head.localPosition = new Vector3(0.4f, 1.6f, 0.3f);
        Vector3 headBefore = head.position;
        float yawBefore = locomotion.transform.eulerAngles.y;
        locomotion.SnapRight();
        DemoValidation.Check(Mathf.Abs(Mathf.DeltaAngle(locomotion.transform.eulerAngles.y, yawBefore + 45)) < 0.01f
            && Vector3.Distance(head.position, headBefore) < 0.001f, "Snap turn pivots around an offset head");
        head.localPosition = headLocal;

        // The arc shares FindDestination's validation: a parabola down the right-hand
        // aisle lands on open floor, one aimed out the entrance never lands inside.
        List<Vector3> arc = new List<Vector3>();
        bool lands = locomotion.FindArcDestination(new Vector3(1, 1.6f, 2), new Vector3(0, 0.3f, 1).normalized, 6, arc, out Vector3 landing);
        DemoValidation.Check(lands && locomotion.Bounds.Contains(landing) && arc.Count > 5, "Teleport arc lands inside the shop");
        bool street = locomotion.FindArcDestination(new Vector3(0, 1.6f, 1), new Vector3(0, 0.3f, -1).normalized, 6, arc, out Vector3 outside);
        DemoValidation.Check(!street, "Teleport arc refuses the street");

        // Smooth stick movement walks along the head's view and never leaves the interior.
        Quaternion rigTurn = locomotion.transform.rotation;
        Quaternion headTurn = head.localRotation;
        locomotion.transform.rotation = Quaternion.identity;
        head.localRotation = Quaternion.identity;
        locomotion.TryTeleport(new Vector3(1, 0, 2)); // the right-hand aisle the arc check uses
        Vector3 startHead = head.position;
        float moved = 0;
        for (int i = 0; i < 10; i++) moved += locomotion.SmoothMove(Vector2.up, 0.05f);
        Vector3 walked = head.position - startHead;
        DemoValidation.Check(moved > 0.5f && walked.z > 0.5f && Mathf.Abs(walked.x) < 0.05f && Mathf.Abs(walked.y) < 0.05f,
            "Smooth stick movement walks the head forward at floor level");
        locomotion.TryTeleport(new Vector3(0, 0, 1.2f));
        for (int i = 0; i < 200; i++) locomotion.SmoothMove(Vector2.down, 0.05f);
        DemoValidation.Check(locomotion.Bounds.Contains(new Vector3(head.position.x, locomotion.transform.position.y, head.position.z)),
            "Smooth stick movement stops at the interior bounds");
        DemoValidation.Check(locomotion.SmoothMove(new Vector2(0.1f, 0.1f), 0.05f) == 0, "Smooth stick movement ignores the dead zone");
        locomotion.transform.rotation = rigTurn;
        head.localRotation = headTurn;

        // Fade and hint toggling only exist while the VR component set is live.
        DemoModeSwitch mode = DemoValidation.Find<DemoModeSwitch>();
        XRComfortFade fade = head.GetComponent<XRComfortFade>();
        mode.Apply(true);
        bool faded = fade != null && locomotion.TryTeleport(new Vector3(-2, 0, 6)) && fade.Alpha >= 0.99f && fade.Fading;
        XRControllerInput[] hands = UnityEngine.Object.FindObjectsByType<XRControllerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool shown = hands.Length == 2 && hands[0].Label.GetComponent<Renderer>().enabled && hands[1].Label.GetComponent<Renderer>().enabled;
        XRControllerInput.ToggleHints();
        bool hidden = shown && !hands[0].Label.GetComponent<Renderer>().enabled && !hands[1].Label.GetComponent<Renderer>().enabled;
        XRControllerInput.ToggleHints();
        bool restored = hidden && hands[0].Label.GetComponent<Renderer>().enabled && hands[1].Label.GetComponent<Renderer>().enabled;
        mode.Apply(false);
        DemoValidation.Check(faded && fade != null && !fade.enabled, "Comfort fade runs on teleport");
        DemoValidation.Check(restored, "Hand hints toggle together");
        walker.ResetPosition();
    }

    private static void InteractionChecks()
    {
        DesktopInteractor interactor = DemoValidation.Find<DesktopInteractor>();
        interactor.ReadDesktopInput = false;
        ToolStation station = DemoValidation.Find<ToolStation>();
        // Stanchion: base, pole, yellow belt cassette with the tape leaving from inside it,
        // and every part keeps its own painted material (StyleTools skips those).
        Transform post = station.TapePostPrefab.transform;
        CapsuleCollider shaft = post.Find("Post") != null ? post.Find("Post").GetComponent<CapsuleCollider>() : null;
        Transform anchor = station.TapePostPrefab.TapeAnchor;
        bool toolParts = post.Find("Base") != null && post.Find("Head") != null && shaft != null
            && shaft.center.y + shaft.height * 0.5f >= 0.95f && anchor != null
            && anchor.localPosition.y >= 0.905f && anchor.localPosition.y <= 0.955f;
        foreach (MeshRenderer part in post.GetComponentsInChildren<MeshRenderer>())
            toolParts &= DemoToolGeometry.IsPainted(part.sharedMaterial);
        // The procedural cone (the imported one has no Shell) keeps its base, shell and band.
        Transform coneRoot = station.ConePrefab.transform;
        if (coneRoot.Find("Shell") != null)
            foreach (string name in new[] { "Base", "Shell", "Band" })
            {
                Transform part = coneRoot.Find(name);
                MeshRenderer renderer = part != null ? part.GetComponent<MeshRenderer>() : null;
                toolParts &= renderer != null && DemoToolGeometry.IsPainted(renderer.sharedMaterial);
            }
        DemoValidation.Check(toolParts, "Stanchion and cone tool visuals keep their parts and materials");
        // The scene lamp was appended to DemoToolKind: the original rack keeps a sample of
        // every original kind and no lamp sample, and each kind still spawns as itself
        // (markers only by prefab, so the shared stage's numbering still starts at 1).
        bool originalRack = station.transform.Find("Sample_" + DemoToolKind.Lamp) == null;
        foreach (DemoToolKind kind in new[] { DemoToolKind.Cone, DemoToolKind.Marker, DemoToolKind.TapePost, DemoToolKind.Scale, DemoToolKind.Measure })
        {
            Transform rackEntry = station.transform.Find("Sample_" + kind);
            ToolRackSample rackSample = rackEntry != null ? rackEntry.GetComponent<ToolRackSample>() : null;
            originalRack &= rackSample != null && rackSample.Kind == kind && station.Prefab(kind) != null && station.Prefab(kind).Kind == kind;
            if (kind == DemoToolKind.Marker) continue;
            DeployedTool spawned = station.Spawn(kind);
            originalRack &= spawned != null && spawned.Kind == kind && spawned.Lamp == null;
            if (spawned != null) station.RemoveTool(spawned);
        }
        DemoValidation.Check(originalRack, "Old tool rack still spawns every original kind");
        DeployedTool cone = interactor.SpawnIntoHand(DemoToolKind.Cone);
        DemoValidation.Check(cone != null && interactor.Held == cone, "Spawned tool is held");
        // A clear patch of showroom floor, pointed at from above.
        interactor.Carry(new Ray(new Vector3(-3.5f, 2, 5), Vector3.down));
        interactor.Place();
        DemoValidation.Check(interactor.Held == null && Vector3.Distance(cone.transform.position, new Vector3(-3.5f, 0, 5)) < 0.02f,
            "Pointer placement lands the tool on the floor");
        DemoValidation.Check(cone.Highlighted, "Held tool is highlighted");
        interactor.Hold(cone);
        Vector3 placed = cone.transform.position;
        interactor.Carry(new Ray(new Vector3(0, 1, 1), Vector3.back));
        interactor.Place();
        DemoValidation.Check(interactor.Held == cone && !interactor.HasPlacementTarget, "Invalid aim cannot place at a stale position");
        DemoValidation.Check(cone.transform.position == placed, "Front barrier prevents placement through the storefront");
        // Floor just inside the barrier but outside the bounds: clamped to the bounds edge.
        interactor.Carry(new Ray(new Vector3(0, 1, 1), new Vector3(0, -1, -0.9f)));
        DemoValidation.Check(Mathf.Abs(cone.transform.position.z - 0.3f) < 0.001f, "Placement is clamped inside the interior");
        interactor.Rotate(30);
        DemoValidation.Check(Mathf.Abs(Mathf.DeltaAngle(cone.transform.eulerAngles.y, 30)) < 0.01f, "Held tool rotates");
        DeployedTool scale = interactor.SpawnIntoHand(DemoToolKind.Scale);
        DemoValidation.Check(scale != null && scale.Kind == DemoToolKind.Scale && scale.GetComponentsInChildren<Collider>().Length == 2
            && scale.GetComponentsInChildren<MeshRenderer>().Length == 30, "Evidence L-scale spawns with two collider arms and 28 bands");
        interactor.Remove(scale);
        DeployedTool reel = interactor.SpawnIntoHand(DemoToolKind.Measure);
        DemoValidation.Check(reel != null && reel.Kind == DemoToolKind.Measure && reel.GetComponentsInChildren<Collider>().Length == 1
            && reel.GetComponentsInChildren<MeshRenderer>().Length == 3 && reel.TapeAnchor != null && reel.TapeAnchor.localPosition.y > 0.02f,
            "Measuring reel spawns with a collider and a tape anchor");
        interactor.Remove(reel);
        interactor.Hold(cone);
        interactor.Remove(cone);
        DemoValidation.Check(interactor.Held == null && interactor.Hovered == null, "Removing the held tool clears the hand and hover");
        // Desktop T: a scene lamp switches, a tape post is still selected as an endpoint.
        DeployedTool useLamp = station.Spawn(DemoToolKind.Lamp), usePost = station.Spawn(DemoToolKind.TapePost);
        station.CancelTapeSelection();
        bool lampWasLit = useLamp != null && useLamp.LampLit;
        interactor.Use(useLamp);
        bool lampSwitched = lampWasLit && !useLamp.LampLit && station.PendingPost == null;
        interactor.Use(usePost);
        DemoValidation.Check(lampSwitched && usePost != null && station.PendingPost == usePost, "Desktop use switches a lamp and selects a tape post");
        station.CancelTapeSelection();
        if (useLamp != null) station.RemoveTool(useLamp);
        if (usePost != null) station.RemoveTool(usePost);

        EvidenceCamera evidence = DemoValidation.Find<EvidenceCamera>();
        Transform rack = evidence.transform.parent;
        interactor.ToggleCamera();
        DemoValidation.Check(interactor.HoldingCamera && evidence.transform.parent == interactor.View.transform,
            "Camera held in front of the view");
        interactor.ToggleCamera();
        DemoValidation.Check(!interactor.HoldingCamera && evidence.transform.parent == rack
            && Vector3.Distance(evidence.transform.localPosition, new Vector3(-3.6f, 1.1f, 1)) < 0.001f,
            "Camera returns to its rack");
    }

    // Night and day are completed at once (the presenter sees a one-second fade), the
    // flashlight is checked in desktop mode here and on the hand in HandChecks, and
    // the beacon goes through the same map maths the panel's click uses.
    private static void LightingChecks()
    {
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        Light reference = null;
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.intensity > 0 && light != lighting.FlashlightLight) { reference = light; break; }
        Renderer fixture = GameObject.Find("JewelryStore_Blockout").transform.Find("ShopDetails/CeilingLight").GetComponent<Renderer>();
        float intensity = reference.intensity, ambient = RenderSettings.ambientIntensity;
        Color ambientColor = RenderSettings.ambientLight, equator = RenderSettings.ambientEquatorColor, fog = RenderSettings.fogColor;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        lighting.SetNight(true);
        lighting.Complete();
        if (lighting.NightSky != null)
            DemoValidation.Check(RenderSettings.skybox == lighting.NightSky, "Night preset swaps to the dusk sky");
        fixture.GetPropertyBlock(block);
        DemoValidation.Check(lighting.Night && reference.intensity < 0.5f * intensity
            && RenderSettings.ambientLight.maxColorComponent < 0.5f * ambientColor.maxColorComponent
            && RenderSettings.ambientIntensity <= 0.5f * ambient && RenderSettings.fogColor.maxColorComponent < fog.maxColorComponent
            && !block.isEmpty && block.GetColor("_EmissionColor").maxColorComponent < 0.01f,
            "Night preset dims lights and fixtures");
        lighting.SetNight(false);
        lighting.Complete();
        fixture.GetPropertyBlock(block);
        DemoValidation.Check(!lighting.Night && Mathf.Abs(reference.intensity - intensity) < 0.01f
            && Mathf.Abs(RenderSettings.ambientIntensity - ambient) < 0.01f && Close(RenderSettings.ambientLight, ambientColor)
            && Close(RenderSettings.ambientEquatorColor, equator) && Close(RenderSettings.fogColor, fog) && block.isEmpty,
            "Day restores the original lighting");

        lighting.Flashlight(true);
        Light spot = lighting.FlashlightLight;
        // Low-resolution hard shadows since the night-store batch: the one realtime shadow in the scene.
        bool lit = spot != null && spot.enabled && lighting.FlashlightOn && spot.type == LightType.Spot
            && spot.transform.parent == Camera.main.transform && spot.shadows == LightShadows.Soft;
        GameObject model = lighting.FlashlightModel;
        // Light and model both stay inside the player's 0.23 m capsule, so neither passes through a wall.
        bool shown = model != null && model.activeInHierarchy && model.transform.parent == Camera.main.transform
            && model.GetComponentsInChildren<MeshRenderer>().Length > 0
            && spot.transform.localPosition.magnitude < 0.23f && model.transform.localPosition.z < 0.23f
            && lighting.FlashlightCookie != null && spot.cookie == lighting.FlashlightCookie;
        // Any camera but the presenter's view (photo, viewfinder) renders without the desktop torch.
        MeshRenderer[] torchParts = model != null ? model.GetComponentsInChildren<MeshRenderer>() : new MeshRenderer[0];
        Camera photo = new GameObject("TorchCullProbe").AddComponent<Camera>();
        photo.enabled = false;
        bool hiddenDuring = torchParts.Length > 0, shownAfter = true;
        if (Camera.onPreCull != null) Camera.onPreCull(photo);
        foreach (MeshRenderer part in torchParts) hiddenDuring &= part.forceRenderingOff;
        if (Camera.onPostRender != null) Camera.onPostRender(photo);
        foreach (MeshRenderer part in torchParts) shownAfter &= !part.forceRenderingOff;
        UnityEngine.Object.DestroyImmediate(photo.gameObject);
        lighting.Flashlight(false);
        DemoValidation.Check(lit && !spot.enabled && !spot.gameObject.activeSelf && !lighting.FlashlightOn, "Flashlight toggles a spot light on the view");
        DemoValidation.Check(shown && !model.activeInHierarchy, "Flashlight shows its model and uses the cookie");
        DemoValidation.Check(hiddenDuring && shownAfter, "Desktop torch model is hidden from photo cameras");

        DemoOverviewMap map = DemoValidation.Find<DemoOverviewMap>();
        Vector3 point = new Vector3(2, 0, 5);
        DemoValidation.Check(Vector3.Distance(map.FromMap(map.ToMap(point)), point) < 0.05f, "Overview map maps a point there and back");

        DemoBeacon beacon = DemoValidation.Find<DemoBeacon>();
        InteriorBounds bounds = DemoValidation.Find<ShopWalkController>().Bounds;
        beacon.Place(new Vector3(9, 0, 4));
        bool inside = beacon.Active && bounds.Contains(beacon.Position) && Mathf.Abs(beacon.Position.x - 4.7f) < 0.001f;
        DemoValidation.Find<DemoSession>().ResetSession();
        DemoValidation.Check(inside && !beacon.Active, "Beacon lands inside the interior and clears on reset");
    }

    private static bool Close(Color a, Color b) => ((Vector4)a - (Vector4)b).sqrMagnitude < 0.0001f;

    // The presenter panel never draws in batch mode (OnGUI does not run), so its
    // layout state, scaled hit test and the camera's live view are exercised directly.
    private static void PanelChecks()
    {
        DemoDesktopPanel panel = DemoValidation.Find<DemoDesktopPanel>();
        // F2 lasts for one run: the HUD starts visible, and hiding it writes nothing to PlayerPrefs.
        bool startsVisible = panel.HudVisible;
        panel.HudVisible = false;
        bool unsaved = !PlayerPrefs.HasKey("DemoHudVisible");
        panel.HudVisible = true;
        DemoValidation.Check(startsVisible && unsaved && panel.HudVisible, "Desktop HUD starts visible each run and F2 is not remembered");
        EvidenceCamera evidence = DemoValidation.Find<EvidenceCamera>();
        panel.Layout = PanelLayout.Hidden;
        panel.CycleLayout();
        bool strip = panel.Layout == PanelLayout.Strip && panel.Visible && !panel.CapturesInput;
        panel.CycleLayout();
        bool full = panel.Layout == PanelLayout.Full && panel.Visible && panel.CapturesInput;
        panel.CycleLayout();
        DemoValidation.Check(strip && full && panel.Layout == PanelLayout.Hidden && !panel.Visible, "Panel layout cycles hidden, strip, full");

        // The strip is 56 GUI units tall whatever the screen size, so the hit test uses it with
        // points placed relative to Screen.height (which Contains subtracts): screen y = height - 80
        // is GUI y 40 at scale 2 (inside) and 80 at scale 1 (outside); height - 130 is GUI y 65 at
        // scale 2 (outside). Only Screen.width >= 2 is assumed. The preference is restored afterwards.
        float scale = panel.Scale;
        panel.Layout = PanelLayout.Strip;
        panel.Scale = 2;
        Vector2 inside = new Vector2(1, Screen.height - 80), outside = new Vector2(1, Screen.height - 130);
        bool scaled = panel.Contains(inside) && !panel.Contains(outside);
        panel.Scale = 1;
        DemoValidation.Check(scaled && !panel.Contains(inside), "Panel pointer test respects UI scale");
        panel.Scale = scale;
        panel.Layout = PanelLayout.Hidden;

        Camera photo = evidence.transform.Find("PhotoCamera").GetComponent<Camera>();
        bool racked = evidence.Viewfinder == null;
        // The flashlight owns the scene's one realtime shadow; the viewfinder render skips that shadow map
        // while it is on. StartViewfinder renders the first frame synchronously, so the probe sees it.
        DemoLighting torches = DemoValidation.Find<DemoLighting>();
        ShadowQuality shadowsBefore = QualitySettings.shadows, duringPreview = shadowsBefore;
        Camera.CameraCallback probe = c => { if (c == photo) duringPreview = QualitySettings.shadows; };
        torches.Flashlight(true);
        Camera.onPreCull += probe;
        evidence.HoldBy(Camera.main.transform, new Vector3(0.22f, -0.12f, 0.35f));
        Camera.onPreCull -= probe;
        bool live = evidence.Viewfinder != null && evidence.Viewfinder.width == 480 && evidence.Holder == Camera.main.transform
            && photo.targetTexture == null && !photo.enabled;
        // The rear screen is refreshed directly (LateUpdate has not run between these calls).
        CameraScreen screen = evidence.GetComponentInChildren<CameraScreen>(true);
        MeshRenderer screenRenderer = screen != null ? screen.GetComponent<MeshRenderer>() : null;
        MaterialPropertyBlock screenBlock = new MaterialPropertyBlock();
        bool screenLive = false;
        if (screen != null)
        {
            screen.Refresh();
            screenRenderer.GetPropertyBlock(screenBlock);
            screenLive = evidence.Viewfinder != null && screenBlock.GetTexture("_MainTex") == evidence.Viewfinder;
        }
        evidence.ReturnToRack();
        torches.Flashlight(false);
        DemoValidation.Check(duringPreview == ShadowQuality.Disable && QualitySettings.shadows == shadowsBefore && !photo.allowHDR,
            "Viewfinder skips the flashlight shadow map and restores the quality settings");
        DemoValidation.Check(racked && live && evidence.Viewfinder == null && evidence.Holder == null,
            "Viewfinder renders only while the camera is held");
        bool screenDark = false;
        if (screen != null)
        {
            screen.Refresh();
            screenRenderer.GetPropertyBlock(screenBlock);
            screenDark = screenBlock.GetTexture("_MainTex") == Texture2D.blackTexture;
        }
        DemoValidation.Check(screenLive && screenDark, "Camera screen shows the viewfinder while held");
    }

    // Held tools turn freely with the controller and stand upright on release; the left
    // controller's tool wheel hands the pointed item (a tool or the camera) to the right hand.
    private static void FreeHoldAndWheelChecks()
    {
        DesktopInteractor desktop = DemoValidation.Find<DesktopInteractor>();
        DemoValidation.Find<DemoModeSwitch>().Apply(true);
        HandInteractor hand = desktop.transform.Find("RightHand").GetComponent<HandInteractor>();
        HandInteractor left = desktop.transform.Find("LeftHand").GetComponent<HandInteractor>();
        Pose saved = new Pose(hand.transform.position, hand.transform.rotation);
        Pose savedLeft = new Pose(left.transform.position, left.transform.rotation);

        hand.transform.SetPositionAndRotation(new Vector3(-3, 1.2f, 5), Quaternion.Euler(30, 60, 70));
        DeployedTool cone = hand.SpawnIntoHand(DemoToolKind.Cone);
        bool turned = cone != null && Quaternion.Angle(cone.transform.rotation, hand.transform.rotation) < 0.5f;
        hand.Release();
        bool upright = cone != null && Vector3.Angle(cone.transform.up, Vector3.up) < 0.5f && cone.transform.position.y < 0.05f;
        if (cone != null) hand.Remove(cone);
        DemoValidation.Check(turned, "A held tool turns with the controller in every axis");
        DemoValidation.Check(upright, "A freely turned tool stands upright on the floor when released");

        DeployedTool lamp = hand.SpawnIntoHand(DemoToolKind.Lamp);
        bool lampUpright = lamp != null && Vector3.Angle(lamp.transform.up, Vector3.up) < 0.5f;
        hand.RemoveHeld();
        DemoValidation.Check(lampUpright, "A carried lamp still hangs upright under a rolled hand");

        XRControllerInput leftInput = left.GetComponent<XRControllerInput>();
        XRToolWheel wheel = leftInput != null ? leftInput.Wheel : null;
        DemoValidation.Check(wheel != null && hand.GetComponent<XRControllerInput>().Wheel == null, "Only the left controller has a tool wheel");
        if (wheel == null) return;
        left.transform.SetPositionAndRotation(new Vector3(-3, 1.1f, 5), Quaternion.identity);
        DemoValidation.Check(leftInput.WheelTrigger(true) && wheel.Open && XRToolWheel.Active == wheel && wheel.Target == hand,
            "Left trigger with an empty hand opens the tool wheel for the right hand");
        Vector3 n = wheel.Face.forward;
        System.Action<int> aim = slot => hand.transform.SetPositionAndRotation(
            (slot < 0 ? wheel.Face.position : wheel.SlotPosition(slot)) - n * 0.4f, Quaternion.LookRotation(n));
        bool slotsRight = true;
        for (int i = 0; i < XRToolWheel.SlotCount; i++)
            slotsRight &= wheel.SlotAt(new Ray(wheel.SlotPosition(i) - n * 0.4f, n)) == i;
        DemoValidation.Check(slotsRight && wheel.SlotAt(new Ray(wheel.Face.position - n * 0.4f, n)) == -1,
            "Each wheel slot is picked by a ray through it, the centre picks nothing");
        aim((int)DemoToolKind.Marker);
        wheel.Refresh();
        bool hovered = wheel.Hovered == (int)DemoToolKind.Marker;
        bool chosen = leftInput.WheelTrigger(false) && !wheel.Open && hand.Held != null && hand.Held.Kind == DemoToolKind.Marker;
        DemoValidation.Check(hovered && chosen, "Releasing the left trigger over a slot puts that tool in the right hand");

        leftInput.WheelTrigger(true);
        aim(XRToolWheel.CameraSlot);
        wheel.Refresh();
        bool camera = hand.GetComponent<XRControllerInput>().WheelTrigger(true) && !wheel.Open && hand.HoldingCamera && hand.Held == null;
        DemoValidation.Check(camera, "Right trigger on the camera slot puts the marker down and the camera in hand");
        leftInput.WheelTrigger(true);
        aim((int)DemoToolKind.Cone);
        wheel.Refresh();
        leftInput.WheelTrigger(false);
        DemoValidation.Check(!hand.HoldingCamera && hand.Held != null && hand.Held.Kind == DemoToolKind.Cone,
            "Choosing a tool while holding the camera returns the camera first");
        hand.RemoveHeld();
        leftInput.WheelTrigger(true);
        hand.transform.SetPositionAndRotation(new Vector3(0, 3, 0), Quaternion.identity);
        wheel.Refresh();
        leftInput.WheelTrigger(false);
        DemoValidation.Check(!wheel.Open && hand.Held == null && !hand.HoldingCamera, "Releasing the wheel with nothing pointed gives nothing");
        foreach (DeployedTool t in Object.FindObjectsByType<DeployedTool>(FindObjectsSortMode.None))
            if (t.Kind == DemoToolKind.Marker && t.Holder == null && Vector3.Distance(t.transform.position, new Vector3(-3, 0, 5)) < 1.5f) desktop.Station.RemoveTool(t);
        hand.transform.SetPositionAndRotation(saved.position, saved.rotation);
        left.transform.SetPositionAndRotation(savedLeft.position, savedLeft.rotation);
    }

    // October 19 batch: comfort vignette, wheel stick picking, camera controls,
    // distance grab, holsters and the see-through hands.
    // Quest 3 standalone app: the headset tier (DemoPlatform), the in-headset presenter menu and the
    // Android build settings, all exercised on the PC with DemoPlatform.ForceStandalone.
    private static void QuestStandaloneChecks()
    {
        DemoPlatform.ForceStandalone = true;
        bool tier = DemoPlatform.ShadowResolution(2048) == 1024 && DemoPlatform.ShadowResolution(300) == 256
            && DemoPlatform.ShadowType(LightShadows.Soft) == LightShadows.Hard && DemoPlatform.ShadowType(LightShadows.None) == LightShadows.None;
        NightQuality quality = DemoValidation.Find<NightQuality>();
        bool qualityTier = true;
        if (quality != null)
        {
            quality.Apply();
            qualityTier = QualitySettings.pixelLightCount == quality.QuestPixelLights && QualitySettings.shadows == ShadowQuality.HardOnly
                && Mathf.Abs(QualitySettings.shadowDistance - quality.QuestShadowDistance) < 0.001f;
        }
        DemoPlatform.ForceStandalone = false;
        if (quality != null) quality.Apply();
        bool pcBack = DemoPlatform.ShadowResolution(2048) == 2048 && DemoPlatform.ShadowType(LightShadows.Soft) == LightShadows.Soft
            && (quality == null || QualitySettings.shadows == ShadowQuality.All);
        DemoValidation.Check(tier && qualityTier && pcBack, "Quest standalone tier halves shadow maps, uses hard shadows and two pixel lights; the PC keeps its own");

        DesktopInteractor desktop = DemoValidation.Find<DesktopInteractor>();
        DemoValidation.Find<DemoModeSwitch>().Apply(true);
        HandInteractor hand = desktop.transform.Find("RightHand").GetComponent<HandInteractor>();
        HandInteractor left = desktop.transform.Find("LeftHand").GetComponent<HandInteractor>();
        XRControllerInput leftInput = left.GetComponent<XRControllerInput>();
        XRPresenterMenu menu = leftInput != null ? leftInput.Presenter : null;
        DemoValidation.Check(menu != null && hand.GetComponent<XRControllerInput>().Presenter == null, "Only the left controller carries the presenter menu");
        if (menu == null) return;
        Pose savedHand = new Pose(hand.transform.position, hand.transform.rotation);
        left.transform.SetPositionAndRotation(new Vector3(-3, 1.1f, 5), Quaternion.identity);
        leftInput.OpenPresenter();
        bool opened = menu.Open && XRPresenterMenu.Active == menu && menu.Count >= 8;
        Vector3 n = menu.Face.forward;
        bool rows = true;
        for (int i = 0; i < menu.Count; i++) rows &= menu.ButtonAt(new Ray(menu.ButtonPosition(i) - n * 0.4f, n)) == i;
        DemoValidation.Check(opened && rows, "Presenter menu opens on the left hand and each button is picked by a ray through it");
        bool smooth = XRControllerInput.SmoothMovement, vignette = XRComfortVignette.Enabled;
        float height = XRHeadTracking.HeightOffset;
        int move = menu.IndexOf("Movement");
        hand.transform.SetPositionAndRotation(menu.ButtonPosition(move) - n * 0.4f, Quaternion.LookRotation(n));
        menu.Refresh();
        bool picked = menu.Hovered == move && hand.GetComponent<XRControllerInput>().WheelTrigger(true) && XRControllerInput.SmoothMovement != smooth;
        menu.Choose(menu.IndexOf("Comfort vignette"));
        menu.Choose(menu.IndexOf("Height +5 cm"));
        bool toggled = XRComfortVignette.Enabled != vignette && Mathf.Abs(XRHeadTracking.HeightOffset - Mathf.Clamp(height + 0.05f, -.2f, .4f)) < 0.001f;
        XRControllerInput.SmoothMovement = smooth; XRComfortVignette.Enabled = vignette; XRHeadTracking.HeightOffset = height;
        menu.Choose(menu.IndexOf("Close"));
        DemoValidation.Check(picked && toggled && !menu.Open && XRPresenterMenu.Active == null,
            "Right trigger on a presenter button runs it (movement, vignette, height), and Close shuts the menu");
        hand.transform.SetPositionAndRotation(savedHand.position, savedHand.rotation);

        DemoValidation.Check(QuestBuild.PackageName == "com.alkoda.jewelrystorecrimescene" && PackImport.QuestMaxTextureSize == 1024,
            "Quest app identity and texture cap are defined (the Android build itself runs only through Build-Quest-App.cmd)");
    }

    private static void VRComfortChecks()
    {
        DesktopInteractor desktop = DemoValidation.Find<DesktopInteractor>();
        DemoValidation.Find<DemoModeSwitch>().Apply(true);
        XRLocomotion rig = DemoValidation.Find<XRLocomotion>();
        HandInteractor hand = desktop.transform.Find("RightHand").GetComponent<HandInteractor>();
        HandInteractor left = desktop.transform.Find("LeftHand").GetComponent<HandInteractor>();
        Pose savedRig = new Pose(rig.transform.position, rig.transform.rotation);
        Pose saved = new Pose(hand.transform.position, hand.transform.rotation);

        XRComfortVignette vignette = rig.Vignette;
        bool vignetteFades = false, jumpIgnored = false, offWorks = false;
        if (vignette != null)
        {
            vignette.Step(0.05f);
            for (int i = 0; i < 10; i++) { rig.transform.position += new Vector3(0, 0, 0.07f); vignette.Step(0.05f); }
            vignetteFades = vignette.Alpha > 0.2f && vignette.Alpha <= vignette.Strength + 1e-4f;
            for (int i = 0; i < 20; i++) vignette.Step(0.05f);
            float rest = vignette.Alpha;
            rig.transform.position += new Vector3(0, 0, 2f);
            vignette.Step(0.05f);
            jumpIgnored = rest < 1e-3f && vignette.Alpha < 1e-3f;
            XRComfortVignette.Enabled = false;
            for (int i = 0; i < 10; i++) { rig.transform.position += new Vector3(0, 0, 0.07f); vignette.Step(0.05f); }
            offWorks = vignette.Alpha < 1e-3f;
            XRComfortVignette.Enabled = true;
        }
        rig.transform.SetPositionAndRotation(savedRig.position, savedRig.rotation);
        DemoValidation.Check(vignetteFades, "Comfort vignette fades in during stick movement, no darker than its strength");
        DemoValidation.Check(jumpIgnored, "Comfort vignette clears at rest and ignores teleport jumps");
        DemoValidation.Check(offWorks, "Comfort vignette stays off when switched off (F6)");

        // Play-Keyboard/Play-VR pass -demo-mode; a validator run has none, so the mode stays automatic.
        DemoValidation.Check(DemoModeSwitch.CommandLineMode() == "" && DemoValidation.Find<DemoModeSwitch>().Mode != DemoModeSwitch.DemoMode.Desktop,
            "Without -demo-mode the desktop/VR choice stays automatic");
        DemoValidation.Check(XRToolWheel.SlotFromStick(Vector2.up) == 0 && XRToolWheel.SlotFromStick(Vector2.right) == 2
            && XRToolWheel.SlotFromStick(new Vector2(-0.43f, 0.9f)) == XRToolWheel.CameraSlot,
            "Left stick directions pick the matching wheel slots");

        EvidenceCamera evidence = DemoValidation.Find<EvidenceCamera>();
        hand.transform.SetPositionAndRotation(new Vector3(-3, 1.2f, 5), Quaternion.identity);
        hand.ToggleCamera();
        Camera lens = evidence.GetComponentInChildren<Camera>(true);
        float fov = lens != null ? lens.fieldOfView : 0;
        evidence.ZoomBy(2f);
        bool zoomed = lens != null && Mathf.Abs(evidence.Zoom - 2) < 1e-3f && Mathf.Abs(lens.fieldOfView - fov / 2) < 0.01f;
        evidence.ZoomBy(100f);
        bool capped = Mathf.Abs(evidence.Zoom - evidence.MaxZoom) < 1e-3f;
        bool flash = !evidence.FlashOn;
        evidence.ToggleFlash();
        flash &= evidence.FlashOn;
        evidence.ToggleFlash();
        evidence.ReviewStep(); // no photos this run: stays live
        bool live = !evidence.Reviewing;
        hand.ToggleCamera();
        bool reset = Mathf.Abs(evidence.Zoom - 1) < 1e-3f && lens != null && Mathf.Abs(lens.fieldOfView - fov) < 0.01f;
        DemoValidation.Check(zoomed && capped && reset, "Camera zooms up to MaxZoom and resets when returned to the rack");
        DemoValidation.Check(flash && live, "Camera flash toggles, and review with no photos stays on the live view");

        DeployedTool far = hand.SpawnIntoHand(DemoToolKind.Cone);
        hand.Release();
        hand.transform.SetPositionAndRotation(new Vector3(-3, 1.2f, 3.2f), Quaternion.LookRotation(far.transform.position + Vector3.up * 0.15f - new Vector3(-3, 1.2f, 3.2f)));
        Physics.SyncTransforms();
        bool pointed = hand.PointedTool() == far;
        bool pulled = hand.Grab() && hand.Held == far;
        HandInteractor.DistanceGrab = false;
        hand.Release();
        hand.transform.SetPositionAndRotation(new Vector3(-3, 1.2f, 3.2f), Quaternion.LookRotation(far.transform.position + Vector3.up * 0.15f - new Vector3(-3, 1.2f, 3.2f)));
        Physics.SyncTransforms();
        bool offRefuses = hand.PointedTool() == null;
        HandInteractor.DistanceGrab = true;
        hand.Remove(far);
        DemoValidation.Check(pointed && pulled, "Pointing at a tool on the floor and gripping pulls it into the hand");
        DemoValidation.Check(offRefuses, "Distance grab can be switched off");

        XRHolsters holsters = rig.Holsters;
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        bool holsterTorch = false, holsterBack = false, holsterCamera = false;
        if (holsters != null)
        {
            lighting.Flashlight(false);
            left.transform.position = holsters.Position(XRHolsters.Slot.Flashlight);
            holsterTorch = holsters.TryUse(left, UnityEngine.XR.XRNode.LeftHand) && lighting.FlashlightOn
                && lighting.FlashlightHand == UnityEngine.XR.XRNode.LeftHand && lighting.FlashlightLight.transform.parent == left.transform;
            holsterBack = holsters.TryUse(left, UnityEngine.XR.XRNode.LeftHand) && !lighting.FlashlightOn;
            lighting.MoveTorch(false, UnityEngine.XR.XRNode.RightHand);
            hand.transform.position = holsters.Position(XRHolsters.Slot.Camera);
            holsterCamera = holsters.TryUse(hand, UnityEngine.XR.XRNode.RightHand) && hand.HoldingCamera;
            hand.ToggleCamera();
        }
        DemoValidation.Check(holsterTorch && holsterBack, "Hip holster hands the flashlight to the gripping hand and takes it back");
        DemoValidation.Check(holsterCamera, "Chest holster puts the camera in the hand");

        var controllerInput=hand.GetComponent<XRControllerInput>();
        controllerInput.SetValidationTracking(true);
        XRGhostHand ghost = controllerInput.GhostHand;
        if (ghost != null && ghost.Root != null) ghost.Apply(false); // no frame loop in the validator
        bool ghostShown = ghost != null && ghost.Root != null && ghost.Root.gameObject.activeSelf;
        XRHandHud hud = hand.GetComponent<XRHandHud>();
        bool controllerHidden = hud != null && hud.Model != null && !hud.Model.gameObject.activeSelf;
        bool curls = false, opacity = false;
        if (ghost != null)
        {
            ghost.SetPose(1, 1, 1, 0);
            curls = ghost.Grip > 0.99f && ghost.Index > 0.99f;
            float before = XRGhostHand.Opacity;
            XRGhostHand.ChangeOpacity(0.2f);
            ghost.Apply(false);
            opacity = Mathf.Abs(ghost.Material.color.a - XRGhostHand.Opacity) < 1e-3f;
            XRGhostHand.Opacity = before;
            ghost.Apply(false);
            ghost.SetPose(0, 0, 0, 0);
        }
        controllerInput.SetValidationTracking(false);
        if(ghost!=null)ghost.Apply(false);
        DemoValidation.Check(ghost!=null&&!ghost.Root.gameObject.activeSelf&&!controllerInput.PoseTracked
            && !XRControllerInput.HasTrackedPose(default(UnityEngine.XR.InputDevice)),"Untracked controllers hide hands and reject invalid device poses");
        DemoValidation.Check(ghostShown && controllerHidden, "See-through hands show in VR in place of the controller models");
        DemoValidation.Check(curls && opacity, "Hands curl with grip and trigger and take the presenter's opacity");
        hand.transform.SetPositionAndRotation(saved.position, saved.rotation);
    }

    private static void HandChecks()
    {
        DesktopInteractor desktop = DemoValidation.Find<DesktopInteractor>();
        EvidenceCamera evidence = DemoValidation.Find<EvidenceCamera>();
        DemoModeSwitch mode = DemoValidation.Find<DemoModeSwitch>();
        mode.Apply(true);
        HandInteractor hand = desktop.transform.Find("RightHand").GetComponent<HandInteractor>();
        DemoValidation.Check(!desktop.enabled && hand.isActiveAndEnabled && hand.Station == desktop.Station,
            "VR mode enables wired hands and disables desktop input");
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        lighting.Flashlight(true);
        DemoValidation.Check(lighting.FlashlightLight.transform.parent == hand.transform && hand.GetComponent<XRControllerInput>().Lighting == lighting,
            "Flashlight rides on the right hand in VR mode");
        lighting.Flashlight(false);
        HandInteractor left = desktop.transform.Find("LeftHand").GetComponent<HandInteractor>();
        lighting.UVTorch(true);
        DemoValidation.Check(lighting.UVTorchLight.transform.parent == left.transform && left.GetComponent<XRControllerInput>().UVLighting == lighting
            && left.GetComponent<XRControllerInput>().Lighting == null && !lighting.FlashlightOn,
            "UV torch rides on the left hand in VR mode");
        // The shop's UV torch keeps the flashlight's generated cookie (UVTorchCookie and the
        // light bar are the night store's).
        bool uvCookie = lighting.UVTorchCookie == null && lighting.LightBar == null && lighting.FlashlightCookie != null
            && lighting.UVTorchLight.cookie == lighting.FlashlightCookie;
        lighting.UVTorch(false);
        DemoValidation.Check(uvCookie, "Shop torches keep the generated cookie");
        DemoValidation.Check(DemoValidation.Find<ToolStation>().LampCookie == null, "Shop lamps have no cookie");

        // The lamp hangs straight below a pitched hand and never sinks below the floor;
        // the trigger switches it rather than selecting a tape endpoint.
        Quaternion handRotation = hand.transform.rotation;
        hand.transform.SetPositionAndRotation(new Vector3(-3, 1.2f, 5), Quaternion.Euler(40, 30, 0));
        DeployedTool lamp = hand.SpawnIntoHand(DemoToolKind.Lamp);
        Vector3 lampBase = lamp != null ? lamp.transform.position : Vector3.zero;
        hand.transform.position = new Vector3(-3, 0.5f, 5);
        hand.FollowHand();
        float lowBase = lamp != null ? lamp.transform.position.y : -1;
        DemoValidation.Check(lamp != null && Vector3.Distance(lampBase, new Vector3(-3, 1.2f - Mathf.Abs(hand.LampHeldOffset.y), 5)) < 0.001f
            && Mathf.Abs(lowBase - hand.FloorY) < 0.001f,
            "Carried lamp hangs below a pitched hand and stays above the floor");
        bool lit = lamp != null && lamp.LampLit;
        hand.Trigger();
        DemoValidation.Check(lit && !lamp.LampLit && desktop.Station.PendingPost == null, "Trigger switches a held scene lamp");
        hand.RemoveHeld();
        hand.transform.rotation = handRotation;
        // An overhead release drops to the floor instead of landing on the ceiling slab's top (ToolHolder.DropCeilingClearance).
        hand.transform.position = new Vector3(-3, 2.4f, 5);
        DeployedTool overhead = hand.SpawnIntoHand(DemoToolKind.Cone);
        hand.Release();
        bool dropped = overhead != null && hand.Held == null && Mathf.Abs(overhead.transform.position.y) < 0.02f;
        if (overhead != null) hand.Remove(overhead); // through the hand, so its highlight of the cone clears too
        DemoValidation.Check(dropped, "Tool released overhead drops to the floor, not onto the roof");

        hand.transform.position = new Vector3(-3, 1, 5);
        DeployedTool marker = hand.SpawnIntoHand(DemoToolKind.Marker);
        DemoValidation.Check(marker != null && Vector3.Distance(marker.transform.position, new Vector3(-3, 0.85f, 5)) < 0.001f,
            "Hand carries the spawned tool below the hand");
        hand.Release();
        DemoValidation.Check(hand.Held == null && Vector3.Distance(marker.transform.position, new Vector3(-3, 0, 5)) < 0.02f,
            "Released tool drops onto the floor");
        hand.transform.position = marker.transform.position + Vector3.up * 0.1f;
        Physics.SyncTransforms(); // Test moves colliders repeatedly without a physics tick.
        DemoValidation.Check(hand.Grab() && hand.Held == marker, "Hand grabs the nearest tool");
        HandInteractor other = desktop.transform.Find("LeftHand").GetComponent<HandInteractor>();
        other.transform.position = hand.transform.position;
        Physics.SyncTransforms();
        DemoValidation.Check(!other.Grab() && other.Held == null && marker.Holder == hand, "A held tool cannot be taken by the other hand");
        // Highlights are counted per holder: the empty hand dropping its highlight of a tool
        // the other hand just grabbed must not untint the held tool.
        System.Reflection.MethodInfo handUpdate = typeof(HandInteractor).GetMethod("Update",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        hand.Release();
        hand.transform.position = marker.transform.position + Vector3.up * 0.1f;
        other.transform.position = hand.transform.position;
        Physics.SyncTransforms();
        handUpdate.Invoke(other, null);
        bool otherLit = marker.Highlighted;
        bool regrabbed = hand.Grab() && hand.Held == marker;
        handUpdate.Invoke(other, null);
        DemoValidation.Check(otherLit && regrabbed && marker.Highlighted, "A held tool stays highlighted when the other hand moves on");
        DemoValidation.Check(!desktop.GetComponent<CharacterController>().enabled, "VR mode disables the walking collider");
        hand.RemoveHeld();
        DemoValidation.Check(hand.Held == null, "Hand removes the held tool");
        // The trigger selects any held tool with a tape anchor, so a reel starts a measurement.
        DeployedTool reel = hand.SpawnIntoHand(DemoToolKind.Measure);
        hand.Trigger();
        DemoValidation.Check(reel != null && desktop.Station.PendingPost == reel && desktop.Station.PendingPrompt.Contains("second reel"),
            "Trigger selects the held reel for measuring");
        desktop.Station.CancelTapeSelection();
        hand.RemoveHeld();

        hand.transform.position = desktop.Station.transform.Find("Sample_Marker").position + Vector3.up * 0.05f;
        Physics.SyncTransforms(); // Test moves colliders repeatedly without a physics tick.
        DemoValidation.Check(hand.Grab() && hand.Held != null && hand.Held.Kind == DemoToolKind.Marker,
            "Grabbing a rack sample spawns that tool into the hand");
        hand.RemoveHeld();
        hand.transform.position = desktop.Station.transform.Find("Sample_Measure").position + Vector3.up * 0.05f;
        Physics.SyncTransforms(); // Test moves colliders repeatedly without a physics tick.
        DemoValidation.Check(hand.Grab() && hand.Held != null && hand.Held.Kind == DemoToolKind.Measure,
            "Grabbing the reel sample spawns a measuring reel");
        hand.RemoveHeld();
        // Twelve markers (Foot, Front and Back colliders each) beside a cone fill more than the first
        // 32-entry overlap buffer: the scan grows and still finds the cone the hand is on.
        Vector3 pile = new Vector3(-3, 0, 5);
        List<DeployedTool> stacked = new List<DeployedTool>();
        for (int i = 0; i < 12; i++)
        {
            DeployedTool m = desktop.Station.Spawn(DemoToolKind.Marker);
            m.PlaceAt(pile, 0);
            stacked.Add(m);
        }
        DeployedTool nearCone = desktop.Station.Spawn(DemoToolKind.Cone);
        nearCone.PlaceAt(pile + new Vector3(0.15f, 0, 0), 0);
        hand.transform.position = nearCone.transform.position + Vector3.up * 0.05f;
        Physics.SyncTransforms(); // Test moves colliders repeatedly without a physics tick.
        bool nearest = hand.Grab() && hand.Held == nearCone;
        if (hand.Held != null) hand.RemoveHeld();
        if (nearCone.gameObject.activeSelf) desktop.Station.RemoveTool(nearCone);
        foreach (DeployedTool m in stacked) if (m.gameObject.activeSelf) desktop.Station.RemoveTool(m);
        DemoValidation.Check(nearest, "Grab finds the nearest tool beside a pile of 36 colliders");
        // XRControllerInput.Awake wrote the label when VR mode activated the hand; A/X spawns a cone by default.
        DemoValidation.Check(hand.GetComponent<XRControllerInput>().Label.text.Contains("new cone"), "Hand label names the spawn kind");
        hand.transform.position = evidence.transform.position;
        Physics.SyncTransforms(); // Test moves colliders repeatedly without a physics tick.
        DemoValidation.Check(hand.Grab() && hand.HoldingCamera && evidence.Holder == hand.transform,
            "Hand grabs the camera from the rack");
        DemoValidation.Check(hand.SpawnIntoHand(DemoToolKind.Cone) == null, "No tool spawns into a hand holding the camera");
        mode.Apply(false);
        DemoValidation.Check(desktop.enabled && !hand.gameObject.activeSelf && evidence.Holder == null
            && evidence.gameObject.activeInHierarchy && evidence.transform.parent == desktop.Station.transform,
            "Desktop mode releases the hands and returns the camera");
        desktop.ToggleCamera();
        DemoDesktopPanel panel = DemoValidation.Find<DemoDesktopPanel>();
        DemoValidation.Check(!panel.RequestReset() && desktop.HoldingCamera, "First reset request only arms the button");
        DemoValidation.Check(panel.RequestReset(), "Second reset request resets the session");
        DemoValidation.Check(!desktop.HoldingCamera && evidence.Holder == null
            && Vector3.Distance(evidence.transform.localPosition, new Vector3(-3.6f, 1.1f, 1)) < 0.001f,
            "Reset returns the camera to its rack");
        DemoValidation.Check(DemoValidation.Find<DemoStatusBoard>().GetComponent<TextMesh>().text.Contains("next marker 1"),
            "Status board reflects the reset");
    }

    // Every mesh the generators saved (lofts, loops, combined city) must carry UVs and
    // tangents, or the textured materials show as flat colour with broken normal maps.
    private static void GeneratedMeshChecks()
    {
        int generated = 0, textured = 0;
        foreach (MeshFilter filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null || !AssetDatabase.GetAssetPath(mesh).StartsWith("Assets/CrimeSceneDemo/")) continue;
            generated++;
            if (mesh.uv.Length == mesh.vertexCount && mesh.tangents.Length == mesh.vertexCount) textured++;
        }
        DemoValidation.Check(generated > 0 && textured == generated, "Generated meshes carry UVs and tangents (" + textured + "/" + generated + ")");
    }

    private static void BackdropChecks()
    {
        DemoOverviewMap map = DemoValidation.Find<DemoOverviewMap>();
        map.Render();
        Vector2 uv = map.ToMap(DemoValidation.Find<ShopWalkController>().transform.position);
        DemoValidation.Check(map.Texture != null && uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1, "Overview map renders with the player inside it");
        DemoValidation.Check(map.MapShader != null && !ShaderUtil.ShaderHasError(map.MapShader), "Overview map draws albedo through its replacement shader");
        DemoValidation.Info("Third-party city models=" + (DemoAssetLibrary.Has("KenneyCityBuilder", "building-small-a") ? "present" : "absent"));
        GameObject city = GameObject.Find("CityBackdrop");
        DemoValidation.Check(city.GetComponentsInChildren<Collider>().Length == 0, "Backdrop has no physics colliders");
        int renderers = city.GetComponentsInChildren<MeshRenderer>().Length;
        DemoValidation.Check(renderers < 25, "Backdrop renderer budget");
        int triangles = 0;
        foreach (MeshFilter filter in city.GetComponentsInChildren<MeshFilter>())
            triangles += filter.sharedMesh.triangles.Length / 3;
        // Boxes and Kenney tiles stay under 20k; the KayKit kit (cars, towers, props) adds about 10k more.
        DemoValidation.Check(triangles < 40000, "Backdrop triangle budget");
        DemoValidation.Info("City renderers=" + renderers + "; triangles=" + triangles);
        if (SessionState.GetBool(AssetsKey, true) && DemoAssetLibrary.Has("KayKitCity", "car_police"))
        {
            Transform kit = city.transform.Find("City_citybits_texture");
            Mesh kitMesh = kit != null ? kit.GetComponent<MeshFilter>().sharedMesh : null;
            DemoValidation.Check(kitMesh != null && kitMesh.uv.Length == kitMesh.vertexCount && kitMesh.vertexCount > 5000
                && kit.GetComponent<MeshRenderer>().sharedMaterial.mainTexture != null, "KayKit street kit combines into one atlas-textured draw");
            DemoValidation.Check(city.transform.Find("StreetFurniture") != null && city.GetComponentsInChildren<Transform>().Count(t => t.name == "ParkedCar") == ShopCityRefinement.ParkedCars,
                "Street furniture and the five kit cars placed");
        }
        DemoValidation.Check(DemoSounds.Clip(DemoSound.Click).length > 0.02f && DemoSounds.Clip(DemoSound.Ambience).length > 3,
            "Generated sounds are available");
        Renderer fixture = GameObject.Find("JewelryStore_Blockout").transform.Find("ShopDetails/CeilingLight").GetComponent<Renderer>();
        DemoValidation.Check(fixture.sharedMaterial.IsKeywordEnabled("_EMISSION"), "Ceiling fixtures use an emissive material");
        // isPlaying is unreliable without an audio device in batch mode; check the setup instead.
        AudioSource ambience = DemoValidation.Find<AmbientSound>().GetComponent<AudioSource>();
        DemoValidation.Check(ambience.clip != null && ambience.loop, "Street ambience source is configured");
        DemoValidation.Check(DemoSounds.Bank == null && DemoSounds.SetDownClip(DemoToolKind.Cone) == DemoSounds.Generated(DemoSound.Thud)
            && DemoSounds.SetDownClip(DemoToolKind.Marker) == DemoSounds.Generated(DemoSound.Thud), "Without a sound bank, set-downs keep the generated thud");
        DemoValidation.Check(Mathf.Approximately(ambience.spatialBlend, 0.6f), "Shop ambience keeps spatial blend 0.6");
        // The night store's realism wiring stays out of the old shop (JewelryStoreGlass looks and
        // sounds as before): no soundscape zones or street events, no footsteps or underscore, no
        // glow cards, light bar or presenter sets, active or not.
        int Placed<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        int night = Placed<AmbientZones>() + Placed<AmbientEvents>() + Placed<Footsteps>() + Placed<DemoMusic>() + Placed<PresenterSet>()
            + Placed<GlowBillboard>() + Placed<LightBarBlink>();
        DemoValidation.Check(night == 0 && DemoMusic.Instance == null, "The old shop has no night-store soundscape, music, footsteps, glow or presenter sets");
    }

    private static void ExpansionChecks()
    {
        GameObject street = GameObject.Find("StreetDetails");
        DemoValidation.Check(street.GetComponentsInChildren<MeshRenderer>().Length <= 6, "Street additions stay within renderer budget");
        DemoValidation.Check(street.GetComponentsInChildren<Collider>().Length == 0, "Street additions have no physics colliders");
        Transform store = GameObject.Find("JewelryStore_Blockout").transform;
        DemoValidation.Check(store.Find("PresentationDetails") != null, "Presentation details present");
        Transform dressing = store.Find("ThirdPartyDressing");
        if (!SessionState.GetBool(AssetsKey, true))
            DemoValidation.Check(dressing == null, "Geometry-only scene skips imported dressing");
        else
        {
            int expected = ShopAssetDressing.ExpectedPresent();
            DemoValidation.Check(dressing != null && expected > 0 && dressing.childCount == expected, "Third-party dressing placed (" + expected + " of " + ShopAssetDressing.Count + " listed models present)");
            DemoValidation.Check(dressing.Find("GlamVelvetSofa").GetComponent<BoxCollider>() != null, "Furniture has a collider");
            if (DemoAssetLibrary.Has("KayKitFurniture", "cabinet_medium_decorated"))
            {
                Transform cabinet = dressing.Find("cabinet_medium_decorated"), lamp = dressing.Find("lamp_standing");
                DemoValidation.Check(cabinet != null && cabinet.GetComponent<BoxCollider>() != null && lamp != null && lamp.GetComponent<BoxCollider>() != null
                    && cabinet.GetComponentInChildren<MeshRenderer>().sharedMaterial == lamp.GetComponentInChildren<MeshRenderer>().sharedMaterial,
                    "KayKit furniture is solid and shares one atlas material");
                DemoValidation.Check(cabinet != null && cabinet.position.x > 1 && cabinet.position.x < 1.7f && cabinet.position.z > 8.25f && cabinet.position.z < 10.75f,
                    "Safe room cabinet stands against the back room's left wall");
            }
        }

        CrimeSceneState state = DemoValidation.Find<CrimeSceneState>();
        state.ReadDesktopInput = false;
        Transform evidenceGroup = store.Find("FixedEvidence");
        Transform hinge = store.Find("Furniture/OpenSafe/DoorHinge");
        state.SetIntact(true);
        DemoValidation.Check(state.Intact && !evidenceGroup.gameObject.activeSelf && store.Find("IntactOverlay").gameObject.activeSelf
            && Mathf.Abs(hinge.localEulerAngles.y) < 0.01f && store.Find("IntactOverlay").childCount >= 7,
            "Intact view hides evidence, closes the safe and restores glass and stock");
        DemoValidation.Find<DemoSession>().ResetSession();
        DemoValidation.Check(!state.Intact && evidenceGroup.gameObject.activeSelf && !store.Find("IntactOverlay").gameObject.activeSelf
            && Mathf.Abs(Mathf.DeltaAngle(hinge.localEulerAngles.y, 110)) < 0.01f, "Reset returns to the robbed view");
    }
}
#endif
