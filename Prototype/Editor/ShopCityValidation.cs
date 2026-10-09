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
    static ShopCityValidation() { EditorApplication.update += Tick; }

    public static void Run() { Generate(false); }
    public static void RunExpanded() { Generate(true); }

    private static void Generate(bool expanded)
    {
        try
        {
            SessionState.SetBool(ExpandedKey, expanded);
            DemoValidation.Begin(expanded ? "VerificationExpanded" : "VerificationCity");
            JewelryStoreBuilder.CreateStore();
            DemoToolsBuilder.Create();
            ShopCityRefinement.Apply();
            if (expanded) { ShopPresentationExpansion.Apply(); ShopAssetDressing.Apply(); }
            Camera camera = DemoValidation.CreatePreviewCamera(150);
            DemoValidation.SaveScene(expanded ? "JewelryStoreExpanded" : "JewelryStoreCity");
            DemoValidation.Capture(camera, "showroom.png", new Vector3(3.6f, 1.65f, 0.8f), new Vector3(-0.2f, 1, 4.5f));
            if (expanded) DemoValidation.Capture(camera, "display-details.png", new Vector3(-.4f, 1.6f, 2), new Vector3(-2, 1, 3.7f));
            DemoValidation.Capture(camera, "street-from-inside.png", new Vector3(0, 1.65f, 1.5f), new Vector3(0, 2, -18));
            DemoValidation.Capture(camera, "safe-room.png", new Vector3(3.4f, 1.65f, 8.8f), new Vector3(2.1f, 0.9f, 10.5f));
            DemoValidation.CaptureOverview(camera);
            DemoToolsBuilder.CreatePlayer(camera);
            DemoValidation.SaveScene();
            DemoValidation.EnterPlayMode(Key, (expanded ? "Expanded scene generation and 5" : "City scene generation and 4")
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
                WalkingChecks();
                LocomotionChecks();
                InteractionChecks();
                HandChecks();
                BackdropChecks();
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
        Vector3 headBefore = head.position;
        float yawBefore = locomotion.transform.eulerAngles.y;
        locomotion.SnapRight();
        DemoValidation.Check(Mathf.Abs(Mathf.DeltaAngle(locomotion.transform.eulerAngles.y, yawBefore + 45)) < 0.01f
            && Vector3.Distance(head.position, headBefore) < 0.001f, "Snap turn pivots around the head");
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
        DemoValidation.Check(cone.transform.position == placed, "Front barrier prevents placement through the storefront");
        // Floor just inside the barrier but outside the bounds: clamped to the bounds edge.
        interactor.Carry(new Ray(new Vector3(0, 1, 1), new Vector3(0, -1, -0.9f)));
        DemoValidation.Check(Mathf.Abs(cone.transform.position.z - 0.3f) < 0.001f, "Placement is clamped inside the interior");
        interactor.Rotate(30);
        DemoValidation.Check(Mathf.Abs(Mathf.DeltaAngle(cone.transform.eulerAngles.y, 30)) < 0.01f, "Held tool rotates");
        interactor.Remove(cone);
        DemoValidation.Check(interactor.Held == null, "Removing the held tool clears the hand");

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
        DemoValidation.Check(hand.Grab() && hand.Held == marker, "Hand grabs the nearest tool");
        hand.RemoveHeld();
        DemoValidation.Check(hand.Held == null, "Hand removes the held tool");

        hand.transform.position = desktop.Station.transform.Find("Sample_Marker").position + Vector3.up * 0.05f;
        DemoValidation.Check(hand.Grab() && hand.Held != null && hand.Held.Kind == DemoToolKind.Marker,
            "Grabbing a rack sample spawns that tool into the hand");
        hand.RemoveHeld();
        hand.transform.position = evidence.transform.position;
        DemoValidation.Check(hand.Grab() && hand.HoldingCamera && evidence.Holder == hand.transform,
            "Hand grabs the camera from the rack");
        mode.Apply(false);
        DemoValidation.Check(desktop.enabled && !hand.gameObject.activeSelf && evidence.Holder == null,
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
        DemoValidation.Check(dressing != null && dressing.childCount == 5, "Third-party dressing placed (5 models)");
        DemoValidation.Check(dressing.Find("GlamVelvetSofa").GetComponent<BoxCollider>() != null, "Furniture has a collider");
    }
}
#endif
