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
// vibration (Pulse) and the controller interface lives in XRHandHud.
[RequireComponent(typeof(HandInteractor))]
public sealed class XRControllerInput : MonoBehaviour
{
    public enum HandEvent { Select, Grab, Teleport, Spawn, Place, Photo, Refused }

    public XRNode Node = XRNode.RightHand;
    public XRLocomotion Locomotion;
    public DemoLighting Lighting;
    public DemoLighting UVLighting;
    public bool Teleports = true;
    public bool SnapTurns;
    public float ArcSpeed = 6f;
    public DemoToolKind SpawnKind = DemoToolKind.Cone;
    public TextMesh Label;

    // Button states for the controller model's highlights.
    public bool GripHeld => grip;
    public bool TriggerHeld => trigger;
    public bool PrimaryHeld => primary;
    public bool SecondaryHeld => secondary;
    public bool StickHeld => stickClick;
    public bool MenuHeld => menu;
    public bool Left => Node == XRNode.LeftHand;
    public HandInteractor Hand => hand;
    public XRHandHud Hud => hud;
    public static bool HintsVisible => hintsVisible;

    private const int ArcPositions = 24;
    private static readonly int KindCount = Enum.GetValues(typeof(DemoToolKind)).Length;
    private static bool hintsVisible = true;

    private HandInteractor hand;
    private XRHandHud hud;
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
        RefreshLabel();
        ApplyHints();
    }

    private void OnDisable() { CancelAim(); }

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

    private void Update()
    {
        device = InputDevices.GetDeviceAtXRNode(Node);
        if (!device.isValid) { CancelAim(); return; }
        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) transform.localPosition = position;
        if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation)) transform.localRotation = rotation;

        if (Changed(device, CommonUsages.gripButton, ref grip))
        {
            if (grip) { if (hand.Grab()) Buzz(HandEvent.Grab); }
            else
            {
                bool holding = hand.Held != null || hand.HoldingCamera;
                hand.Release();
                if (holding) Buzz(HandEvent.Place);
            }
        }
        if (Changed(device, CommonUsages.triggerButton, ref trigger) && trigger)
        {
            bool camera = hand.HoldingCamera, held = hand.Held != null;
            hand.Trigger();
            if (camera) Buzz(HandEvent.Photo);
            else if (held) Buzz(HandEvent.Select);
        }
        if (Changed(device, CommonUsages.primaryButton, ref primary) && primary)
            Buzz(hand.SpawnIntoHand(SpawnKind) != null ? HandEvent.Spawn : HandEvent.Refused);
        if (Changed(device, CommonUsages.secondaryButton, ref secondary) && secondary)
        {
            if (hand.Held != null) { hand.RemoveHeld(); Buzz(HandEvent.Select); }
            else CycleKind();
        }
        if ((Lighting != null || UVLighting != null) && Changed(device, CommonUsages.primary2DAxisClick, ref stickClick) && stickClick)
        {
            if (Lighting != null) Lighting.ToggleFlashlight();
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
        if (Teleports && Locomotion != null) Teleport(stick.y);
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

    // True when the button state changed; state holds the new value.
    private static bool Changed(InputDevice device, InputFeatureUsage<bool> usage, ref bool state)
    {
        if (!device.TryGetFeatureValue(usage, out bool now) || now == state) return false;
        state = now;
        return true;
    }
}
