using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Drives one HandInteractor from a tracked controller through Unity's built-in
// XR input API (no package required in the project). Grip holds, trigger uses,
// the primary button (A/X) spawns the current tool kind, the secondary button
// (B/Y) removes the held tool or, with empty hands, cycles the kind. The
// thumbstick teleports (push forward: a ballistic arc with a landing ring;
// release: go) or snap turns (left/right), depending on which role this hand
// has; clicking it toggles the white flashlight on the hand that has Lighting
// wired (the right one) or the UV torch on the hand with UVLighting (the left
// one). The trigger also switches a held scene lamp. The left controller's menu
// button hides or shows the hints on both hands. Every action answers with a short
// vibration (Pulse) and the controller interface lives in XRHandHud. The pose is
// read in Update and again just before rendering (LatePose).
// Runs after the head and before every default-order consumer: HandInteractor places the held tool
// and the grab highlight from this frame's pose and from any rig move this Update made, and
// DemoLighting.Update aims the UV beam from it.
[RequireComponent(typeof(HandInteractor))]
[DefaultExecutionOrder(-100)]
public sealed class XRControllerInput : MonoBehaviour
{
    public enum HandEvent { Select, Grab, Teleport, Spawn, Place, Photo, Refused }

    public XRNode Node = XRNode.RightHand;
    public XRLocomotion Locomotion;
    public DemoLighting Lighting;
    public DemoLighting UVLighting;
    public bool Teleports = true;
    // Smooth movement with the stick instead of teleporting (when both are on, smooth wins).
    // Switch at runtime with SetSmoothMovement or the drawer/hint toggle.
    public bool SmoothMoves;
    public static bool SmoothMovement = true;
    public float MovedDistance { get; private set; }
    public bool SnapTurns;
    public float ArcSpeed = 6f;
    public float ZoomPerSecond = 2f;
    public DemoToolKind SpawnKind = DemoToolKind.Cone;
    public TextMesh Label;

    // Button states for the controller model's highlights.
    public bool GripHeld => grip;
    public bool TriggerHeld => trigger;
    public bool PrimaryHeld => primary;
    public bool SecondaryHeld => secondary;
    public bool StickHeld => stickClick;
    public bool MenuHeld => menu;
    // Analog grip and trigger (0..1) and whether the thumb rests on the face buttons or stick, for the ghost hand.
    public float GripValue { get; private set; }
    public float TriggerValue { get; private set; }
    public bool ThumbDown { get; private set; }
    public XRGhostHand GhostHand => ghost;
    public bool Left => Node == XRNode.LeftHand;
    public HandInteractor Hand => hand;
    // The left hand's radial tool menu (left trigger with an empty hand); null on the right.
    public XRToolWheel Wheel => wheel;
    public XRHandHud Hud => hud;
    public static bool HintsVisible => hintsVisible;

    private const int ArcPositions = 24;
    private static readonly int KindCount = Enum.GetValues(typeof(DemoToolKind)).Length;
    private static bool hintsVisible = false;

    private HandInteractor hand;
    private XRHandHud hud;
    private XRToolWheel wheel;
    private XRGhostHand ghost;
    private InputDevice device;
    private bool grip, trigger, primary, secondary, stickClick, menu, aiming, aimValid, turned;
    private Vector3 aimPoint;
    private Transform teleportMarker;
    private LineRenderer arcLine;
    private readonly List<Vector3> arcPoints = new List<Vector3>();

    private void Awake()
    {
        hand = GetComponent<HandInteractor>();
        hud = GetComponent<XRHandHud>();
        if (hud == null) hud = gameObject.AddComponent<XRHandHud>();
        hud.Build(this);
        ghost = GetComponent<XRGhostHand>();
        if (ghost == null) ghost = gameObject.AddComponent<XRGhostHand>();
        ghost.Build(this);
        if (Left)
        {
            wheel = GetComponent<XRToolWheel>();
            if (wheel == null) wheel = gameObject.AddComponent<XRToolWheel>();
        }
        RefreshLabel();
        ApplyHints();
    }

    private void OnEnable() { Application.onBeforeRender += LatePose; }

    private void OnDisable() { Application.onBeforeRender -= LatePose; LoseTracking(); }

    // Forget any destination shown so far, e.g. when tracking drops or the hand is disabled.
    private void CancelAim()
    {
        aiming = aimValid = false;
        if (teleportMarker != null) teleportMarker.gameObject.SetActive(false);
        if (arcLine != null) arcLine.enabled = false;
    }

    private void OnDestroy()
    {
        if (teleportMarker != null)
        {
            foreach (Renderer part in teleportMarker.GetComponentsInChildren<Renderer>(true)) Destroy(part.sharedMaterial);
            Destroy(teleportMarker.gameObject);
        }
        if (arcLine != null)
        {
            Destroy(arcLine.sharedMaterial);
            Destroy(arcLine.gameObject);
        }
    }

    public bool PoseTracked { get; private set; }
    bool needsNeutral=true;
    float quitChordSince=-1;
    public static bool HasTrackedPose(InputDevice d) {
        const InputTrackingState required=InputTrackingState.Position|InputTrackingState.Rotation;
        return d.isValid && d.TryGetFeatureValue(CommonUsages.isTracked,out bool tracked) && tracked
            && d.TryGetFeatureValue(CommonUsages.trackingState,out InputTrackingState state) && (state&required)==required
            && d.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 _) && d.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion _);
    }
    private void LoseTracking() {
        PoseTracked=false;CancelAim();if(wheel!=null)wheel.Hide();if(XRToolWheel.Active!=null)XRToolWheel.Active.Hide();
        grip=trigger=primary=secondary=stickClick=menu=turned=false;
        GripValue=TriggerValue=MovedDistance=0;ThumbDown=false;needsNeutral=true;quitChordSince=-1;
        // Keep a held object frozen; require neutral controls before re-arming so a
        // reconnect with grip pressed cannot drop/place/spawn anything unexpectedly.
    }
#if UNITY_EDITOR
    // Only the editor validator can inject tracking; player builds use device data.
    public void SetValidationTracking(bool tracked){if(tracked)PoseTracked=true;else LoseTracking();}
#endif
    private bool Neutral(InputDevice d) {
        foreach(var usage in new[]{CommonUsages.gripButton,CommonUsages.triggerButton,CommonUsages.primaryButton,CommonUsages.secondaryButton,CommonUsages.primary2DAxisClick,CommonUsages.menuButton})
            if(d.TryGetFeatureValue(usage,out bool down)&&down)return false;
        return !d.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 axis)||axis.sqrMagnitude<.04f;
    }
    private void Update()
    {
        device = InputDevices.GetDeviceAtXRNode(Node);
        if (!HasTrackedPose(device)) { LoseTracking(); return; }
        PoseTracked=true;
        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) transform.localPosition = position;
        if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation)) transform.localRotation = rotation;
        if(needsNeutral){if(!Neutral(device))return;needsNeutral=false;}

        // A held left-menu + Y chord quits without executing either button's normal
        // action. Photos are already saved. Keep this separate from tool removal.
        bool quitChord=Left && device.TryGetFeatureValue(CommonUsages.menuButton,out bool quitMenu)&&quitMenu
            && device.TryGetFeatureValue(CommonUsages.secondaryButton,out bool quitY)&&quitY;
        if(quitChord){
            if(quitChordSince<0){quitChordSince=Time.unscaledTime;DemoFeedback.Post("Hold Menu + Y for two seconds to quit VR");}
            if(Time.unscaledTime-quitChordSince>=2)Application.Quit();
            return;
        }
        quitChordSince=-1;
        GripValue = device.TryGetFeatureValue(CommonUsages.grip, out float gripAmount) ? gripAmount : grip ? 1 : 0;
        TriggerValue = device.TryGetFeatureValue(CommonUsages.trigger, out float triggerAmount) ? triggerAmount : trigger ? 1 : 0;
        ThumbDown = primary || secondary || stickClick || Touched(device, CommonUsages.primaryTouch)
            || Touched(device, CommonUsages.secondaryTouch) || Touched(device, CommonUsages.primary2DAxisTouch);
        if (Changed(device, CommonUsages.gripButton, ref grip))
        {
            if (grip)
            {
                XRHolsters holsters = XRHolsters.Active;
                if (holsters != null && holsters.TryUse(this)) Buzz(HandEvent.Grab);
                else if (hand.Grab()) Buzz(HandEvent.Grab);
            }
            else
            {
                bool holding = hand.Held != null || hand.HoldingCamera;
                hand.Release();
                if (holding) Buzz(HandEvent.Place);
            }
        }
        if (Changed(device, CommonUsages.triggerButton, ref trigger) && !WheelTrigger(trigger) && trigger)
        {
            bool camera = hand.HoldingCamera, held = hand.Held != null;
            hand.Trigger();
            if (camera) Buzz(HandEvent.Photo);
            else if (held) Buzz(HandEvent.Select);
        }
        if (Changed(device, CommonUsages.primaryButton, ref primary) && primary)
        {
            if (hand.HoldingCamera) { hand.EvidenceCamera.ToggleFlash(); Buzz(HandEvent.Select); }
            else Buzz(hand.SpawnIntoHand(SpawnKind) != null ? HandEvent.Spawn : HandEvent.Refused);
        }
        if (Changed(device, CommonUsages.secondaryButton, ref secondary) && secondary)
        {
            if (hand.HoldingCamera) { hand.EvidenceCamera.ReviewStep(); Buzz(HandEvent.Select); }
            else if (hand.Held != null) { hand.RemoveHeld(); Buzz(HandEvent.Select); }
            else CycleKind();
        }
        if ((Lighting != null || UVLighting != null) && Changed(device, CommonUsages.primary2DAxisClick, ref stickClick) && stickClick)
        {
            // The torch riding this hand (a holster may have swapped them), else this hand's own.
            DemoLighting torches = Lighting != null ? Lighting : UVLighting;
            if (torches.FlashlightOn && torches.FlashlightHand == Node) torches.ToggleFlashlight();
            else if (torches.UVTorchOn && torches.UVTorchHand == Node) torches.ToggleUVTorch();
            else if (Lighting != null) Lighting.ToggleFlashlight();
            else UVLighting.ToggleUVTorch();
            Buzz(HandEvent.Select);
        }
        if (Left && Changed(device, CommonUsages.menuButton, ref menu) && menu)
        {
            ToggleHints();
            DemoFeedback.Post(hintsVisible ? "Controller hints shown" : "Controller hints hidden");
            Buzz(HandEvent.Select);
        }
        if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick)) Stick(stick);
    }

    // Just before rendering: the refreshed controller pose, and the held tool moved with it (FollowHand only
    // places it). Re-fetches the device rather than trusting the one cached in Update.
    private void LatePose()
    {
        InputDevice late = InputDevices.GetDeviceAtXRNode(Node);
        if (!HasTrackedPose(late)) { LoseTracking();return; }
        if(!PoseTracked)return;
        if (late.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) transform.localPosition = position;
        if (late.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation)) transform.localRotation = rotation;
        if (!needsNeutral && hand != null && hand.Held != null) hand.FollowHand();
    }

    // The tool wheel takes the trigger first: the left trigger opens it with an empty left hand and,
    // released over a slot, chooses it; the right trigger chooses while any wheel is open.
    // True when the wheel used this press or release.
    public bool WheelTrigger(bool pressed)
    {
        if (Left && wheel != null)
        {
            if (pressed && hand.Held == null && !hand.HoldingCamera)
            {
                OpenWheel();
                Buzz(HandEvent.Select);
                return true;
            }
            if (!pressed && wheel.Open)
            {
                bool chose = wheel.Hovered >= 0;
                bool given = chose && wheel.Confirm();
                if (!chose) wheel.Hide();
                if (chose) Buzz(given ? HandEvent.Spawn : HandEvent.Refused);
                return true;
            }
            return false;
        }
        XRToolWheel open = XRToolWheel.Active;
        if (!Left && pressed && open != null && open.Open)
        {
            bool chose = open.Hovered >= 0;
            if (chose) Buzz(open.Confirm() ? HandEvent.Spawn : HandEvent.Refused);
            return true; // a right trigger never photographs or selects while the wheel is up
        }
        return false;
    }

    // Points the wheel at the other (right) controller, which receives the chosen item.
    public void OpenWheel()
    {
        if (wheel == null) return;
        foreach (XRControllerInput other in FindObjectsByType<XRControllerInput>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (other != this && !other.Left) { wheel.Pointer = other.transform; wheel.Target = other.Hand; }
        if (Locomotion != null) wheel.Head = Locomotion.Head;
        wheel.Show();
    }

    // B/Y with an empty hand: the next kind for A/X, shown on the carousel above the controller.
    public void CycleKind() { SetSpawnKind((DemoToolKind)(((int)SpawnKind + 1) % KindCount)); }

    public void SetSpawnKind(DemoToolKind kind)
    {
        SpawnKind = kind;
        RefreshLabel();
        if (hud != null) hud.ShowCarousel(kind);
        Buzz(HandEvent.Select);
    }

    // Amplitude (0..1) and seconds per event: a refusal is the strongest, so it is never
    // mistaken for a success; a photo has the shutter's longer thump.
    public static Vector2 Pulse(HandEvent e)
    {
        switch (e)
        {
            case HandEvent.Refused: return new Vector2(0.75f, 0.12f);
            case HandEvent.Photo: return new Vector2(0.6f, 0.08f);
            case HandEvent.Place: return new Vector2(0.45f, 0.05f);
            case HandEvent.Spawn: return new Vector2(0.35f, 0.05f);
            case HandEvent.Teleport: return new Vector2(0.3f, 0.06f);
            case HandEvent.Grab: return new Vector2(0.25f, 0.04f);
            default: return new Vector2(0.15f, 0.02f);
        }
    }

    private void Buzz(HandEvent e)
    {
        if (!device.isValid) return;
        Vector2 pulse = Pulse(e);
        device.SendHapticImpulse(0u, pulse.x, pulse.y);
    }

    // The A/X hint names the tool that button spawns ("new cone"); the other hints are
    // fixed and written by XRHandHud.
    private void RefreshLabel()
    {
        if (Label == null) return;
        Label.text = "new " + DeployedTool.KindName(SpawnKind);
        if (hud != null) hud.Refresh();
    }

    // One flag for every hand, so the presenter hides or shows all hints at once.
    public static void ToggleHints()
    {
        hintsVisible = !hintsVisible;
        foreach (XRControllerInput input in FindObjectsByType<XRControllerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            input.ApplyHints();
    }

    private void ApplyHints()
    {
        if (hud != null) hud.ShowHints(hintsVisible);
        else if (Label != null)
        {
            Renderer text = Label.GetComponent<Renderer>();
            if (text != null) text.enabled = hintsVisible;
        }
    }

    private void Stick(Vector2 stick)
    {
        MovedDistance = 0;
        // While the wheel is open the left stick picks a slot instead of walking.
        if (wheel != null && wheel.Open) { wheel.Stick = stick; if (aiming) CancelAim(); return; }
        if (SmoothMoves && SmoothMovement && Locomotion != null)
        {
            if (aiming) CancelAim();
            MovedDistance = Locomotion.SmoothMove(stick, Time.deltaTime);
        }
        else if (Teleports && Locomotion != null) Teleport(stick.y);
        // Holding the camera, the stick's up and down zoom (twice as close per second at full push).
        if (!Left && hand.HoldingCamera && Mathf.Abs(stick.y) > 0.3f && Mathf.Abs(stick.y) > Mathf.Abs(stick.x))
            hand.EvidenceCamera.ZoomBy(Mathf.Pow(ZoomPerSecond, stick.y * Time.deltaTime));
        if (SnapTurns && Locomotion != null)
        {
            if (!turned && stick.x > 0.7f) { Locomotion.SnapRight(); turned = true; Buzz(HandEvent.Select); }
            else if (!turned && stick.x < -0.7f) { Locomotion.SnapLeft(); turned = true; Buzz(HandEvent.Select); }
            else if (Mathf.Abs(stick.x) < 0.3f) turned = false;
        }
    }

    // While the stick is pushed forward the arc shows the trajectory and the ring the
    // landing; releasing the stick teleports to the last valid destination shown, and a
    // release with no valid landing answers with the refusal pulse.
    private void Teleport(float push)
    {
        bool pushed = push > 0.7f;
        if (pushed)
        {
            aimValid = Locomotion.FindArcDestination(transform.position, transform.forward, ArcSpeed, arcPoints,
                out Vector3 point, hand.Held != null ? hand.Held.transform : null);
            if (aimValid) { aimPoint = point; Marker().position = aimPoint + Vector3.up * 0.01f; }
            if (teleportMarker != null)
            {
                teleportMarker.gameObject.SetActive(aimValid);
                float pulse = 1 + 0.06f * Mathf.Sin(Time.unscaledTime * 9f);
                teleportMarker.localScale = new Vector3(pulse, 1, pulse);
            }
            DrawArc();
        }
        else if (aiming && push < 0.3f)
        {
            if (aimValid && Locomotion.TryTeleport(aimPoint)) Buzz(HandEvent.Teleport);
            else Buzz(HandEvent.Refused);
            CancelAim();
        }
        aiming = pushed || (aiming && push >= 0.3f);
    }

    // A flat ring with a faint disc, in the interface's info blue.
    private Transform Marker()
    {
        if (teleportMarker != null) return teleportMarker;
        GameObject marker = new GameObject("TeleportMarker");
        marker.AddComponent<MeshFilter>().sharedMesh = DemoUi.FlatRing(0.22f, 0.025f);
        MeshRenderer ring = marker.AddComponent<MeshRenderer>();
        ring.sharedMaterial = DemoUi.UnlitMaterial(DemoUi.Info);
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;
        GameObject disc = new GameObject("Disc");
        disc.transform.SetParent(marker.transform, false);
        disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
        disc.AddComponent<MeshFilter>().sharedMesh = DemoUi.RoundedQuad(0.4f, 0.4f, 0.2f, 12);
        MeshRenderer fill = disc.AddComponent<MeshRenderer>();
        fill.sharedMaterial = DemoUi.UnlitMaterial(DemoUi.WithAlpha(DemoUi.Info, 0.18f));
        fill.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fill.receiveShadows = false;
        DemoUi.ToHeadsetUiLayer(marker.transform); // ring and disc
        teleportMarker = marker.transform;
        return teleportMarker;
    }

    // Blue while the landing is valid, red otherwise. The samples are evenly thinned
    // to the renderer's position budget so the whole arc always shows.
    private void DrawArc()
    {
        LineRenderer line = Arc();
        int count = Mathf.Min(arcPoints.Count, ArcPositions);
        line.positionCount = count;
        for (int i = 0; i < count; i++)
            line.SetPosition(i, arcPoints[count < 2 ? 0 : i * (arcPoints.Count - 1) / (count - 1)]);
        Color color = aimValid ? DemoUi.Info : DemoUi.Warning;
        line.startColor = DemoUi.WithAlpha(color, 0.35f);
        line.endColor = color;
        line.enabled = count > 1;
    }

    private LineRenderer Arc()
    {
        if (arcLine != null) return arcLine;
        GameObject arc = new GameObject("TeleportArc");
        arc.layer = DemoUi.HeadsetUiLayer;
        arcLine = arc.AddComponent<LineRenderer>();
        arcLine.useWorldSpace = true;
        arcLine.startWidth = 0.006f;
        arcLine.endWidth = 0.012f;
        arcLine.numCapVertices = 4;
        arcLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        arcLine.receiveShadows = false;
        arcLine.sharedMaterial = DemoUi.UnlitMaterial(Color.white);
        return arcLine;
    }

    private static bool Touched(InputDevice device, InputFeatureUsage<bool> usage) => device.TryGetFeatureValue(usage, out bool on) && on;

    // True when the button state changed; state holds the new value.
    private static bool Changed(InputDevice device, InputFeatureUsage<bool> usage, ref bool state)
    {
        if (!device.TryGetFeatureValue(usage, out bool now) || now == state) return false;
        state = now;
        return true;
    }
}
