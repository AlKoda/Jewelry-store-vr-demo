#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

// Refined shop with city backdrop, optionally with the presentation expansion.
// Adds walking, locomotion, pointer, hand, backdrop and expansion checks before
// the shared tool checks. Invoke Run or RunExpanded with -executeMethod.
[InitializeOnLoad]
public static class ShopCityValidation
{
    private const string Key = "ShopCityValidationStage";
    private const string ExpandedKey = "ShopCityValidationExpanded";
    private const string FormsKey = "ShopCityValidationForms";
    private const string RobberyKey = "ShopCityValidationRobbery";
    private const string InteriorKey = "ShopCityValidationInterior";
    static ShopCityValidation() { EditorApplication.update += Tick; }

    public static void Run() { Generate(false); }
    public static void RunExpanded() { Generate(true); }
    public static void RunForms() { Generate(true, true); }
    public static void RunRobbery() { Generate(true, true, true, true); }
    public static void RunInterior() { Generate(true, true, true); }

    private static void Generate(bool expanded, bool forms = false, bool interior = false, bool robbery = false)
    {
        try
        {
            SessionState.SetBool(RobberyKey, robbery);
            SessionState.SetBool(ExpandedKey, expanded);
            SessionState.SetBool(FormsKey, forms);
            SessionState.SetBool(InteriorKey, interior);
            DemoValidation.Begin(robbery ? "VerificationRobbery" : interior ? "VerificationInterior" : forms ? "VerificationForms" : expanded ? "VerificationExpanded" : "VerificationCity");
            JewelryStoreBuilder.CreateStore();
            DemoToolsBuilder.Create(!forms);
            ShopCityRefinement.Apply(!forms);
            if (expanded)
            {
                ShopPresentationExpansion.Apply();
                if (!forms) ShopAssetDressing.Apply();
                IntactStateBuilder.Apply();
                if (forms) ShopFormRefinement.Apply();
            }
            Camera camera = DemoValidation.CreatePreviewCamera(150);
            if (interior) { ShopInteriorFinish.ApplyShell(); ShopDisplayFinish.Apply(); }
            if (robbery) ShopRobberyDressing.Apply();
            DemoValidation.SaveScene(robbery ? "JewelryStoreRobbery" : interior ? "JewelryStoreInterior" : forms ? "JewelryStoreForms" : expanded ? "JewelryStoreExpanded" : "JewelryStoreCity");
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
            DemoToolsBuilder.CreatePlayer(camera);
            DemoValidation.SaveScene();
            DemoValidation.EnterPlayMode(Key, (robbery ? "Robbery scene generation and 7" : interior ? "Interior scene generation and 6" : expanded ? "Expanded scene generation and 5" : "City scene generation and 4")
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
                WalkingChecks();
                LocomotionChecks();
                InteractionChecks();
                HandChecks();
                BackdropChecks();
                if (SessionState.GetBool(FormsKey, false)) ShopFormRefinement.CheckBudget();
                if (SessionState.GetBool(InteriorKey, false)) { ShopInteriorFinish.CheckBudget(); ShopDisplayFinish.CheckBudget(); }
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
        walker.ResetPosition();
    }

    private static void InteractionChecks()
    {
        DesktopInteractor interactor = DemoValidation.Find<DesktopInteractor>();
        interactor.ReadDesktopInput = false;
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
        interactor.Remove(cone);
        DemoValidation.Check(interactor.Held == null && interactor.Hovered == null, "Removing the held tool clears the hand and hover");

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

    private static void HandChecks()
    {
        DesktopInteractor desktop = DemoValidation.Find<DesktopInteractor>();
        EvidenceCamera evidence = DemoValidation.Find<EvidenceCamera>();
        DemoModeSwitch mode = DemoValidation.Find<DemoModeSwitch>();
        mode.Apply(true);
        HandInteractor hand = desktop.transform.Find("RightHand").GetComponent<HandInteractor>();
        DemoValidation.Check(!desktop.enabled && hand.isActiveAndEnabled && hand.Station == desktop.Station,
            "VR mode enables wired hands and disables desktop input");

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
        DemoValidation.Check(!desktop.GetComponent<CharacterController>().enabled, "VR mode disables the walking collider");
        hand.RemoveHeld();
        DemoValidation.Check(hand.Held == null, "Hand removes the held tool");

        hand.transform.position = desktop.Station.transform.Find("Sample_Marker").position + Vector3.up * 0.05f;
        Physics.SyncTransforms(); // Test moves colliders repeatedly without a physics tick.
        DemoValidation.Check(hand.Grab() && hand.Held != null && hand.Held.Kind == DemoToolKind.Marker,
            "Grabbing a rack sample spawns that tool into the hand");
        hand.RemoveHeld();
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

    private static void BackdropChecks()
    {
        DemoOverviewMap map = DemoValidation.Find<DemoOverviewMap>();
        map.Render();
        Vector2 uv = map.ToMap(DemoValidation.Find<ShopWalkController>().transform.position);
        DemoValidation.Check(map.Texture != null && uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1, "Overview map renders with the player inside it");
        DemoValidation.Info("Third-party city models=" + (DemoAssetLibrary.Has("KenneyCityBuilder", "building-small-a") ? "present" : "absent"));
        GameObject city = GameObject.Find("CityBackdrop");
        DemoValidation.Check(city.GetComponentsInChildren<Collider>().Length == 0, "Backdrop has no physics colliders");
        int renderers = city.GetComponentsInChildren<MeshRenderer>().Length;
        DemoValidation.Check(renderers < 25, "Backdrop renderer budget");
        int triangles = 0;
        foreach (MeshFilter filter in city.GetComponentsInChildren<MeshFilter>())
            triangles += filter.sharedMesh.triangles.Length / 3;
        DemoValidation.Check(triangles < 20000, "Backdrop triangle budget");
        DemoValidation.Info("City renderers=" + renderers + "; triangles=" + triangles);
        DemoValidation.Check(DemoSounds.Clip(DemoSound.Click).length > 0.02f && DemoSounds.Clip(DemoSound.Ambience).length > 3,
            "Generated sounds are available");
        Renderer fixture = GameObject.Find("JewelryStore_Blockout").transform.Find("ShopDetails/CeilingLight").GetComponent<Renderer>();
        DemoValidation.Check(fixture.sharedMaterial.IsKeywordEnabled("_EMISSION"), "Ceiling fixtures use an emissive material");
        // isPlaying is unreliable without an audio device in batch mode; check the setup instead.
        AudioSource ambience = DemoValidation.Find<AmbientSound>().GetComponent<AudioSource>();
        DemoValidation.Check(ambience.clip != null && ambience.loop, "Street ambience source is configured");
    }

    private static void ExpansionChecks()
    {
        GameObject street = GameObject.Find("StreetDetails");
        DemoValidation.Check(street.GetComponentsInChildren<MeshRenderer>().Length <= 6, "Street additions stay within renderer budget");
        DemoValidation.Check(street.GetComponentsInChildren<Collider>().Length == 0, "Street additions have no physics colliders");
        Transform store = GameObject.Find("JewelryStore_Blockout").transform;
        DemoValidation.Check(store.Find("PresentationDetails") != null, "Presentation details present");
        Transform dressing = store.Find("ThirdPartyDressing");
        if (SessionState.GetBool(FormsKey, false))
            DemoValidation.Check(dressing == null, "Geometry-only scene skips imported dressing");
        else
        {
            DemoValidation.Check(dressing != null && dressing.childCount == ShopAssetDressing.Count, "Third-party dressing placed (" + ShopAssetDressing.Count + " models)");
            DemoValidation.Check(dressing.Find("GlamVelvetSofa").GetComponent<BoxCollider>() != null, "Furniture has a collider");
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
