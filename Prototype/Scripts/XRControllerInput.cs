using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Drives one HandInteractor from a tracked controller through Unity's built-in
// XR input API (no package required in the project). Grip holds, trigger uses,
// the primary button (A/X) spawns the current tool kind, the secondary button
// (B/Y) removes the held tool or, with empty hands, cycles the kind. The
// thumbstick teleports (push forward: a ballistic arc with a landing marker;
// release: go) or snap turns (left/right), depending on which role this hand
// has; clicking it toggles the flashlight on the hand that has Lighting wired
// (the right one). The left controller's menu button hides or shows the hint
// labels on both hands.
[RequireComponent(typeof(HandInteractor))]
public sealed class XRControllerInput : MonoBehaviour
{
    public XRNode Node = XRNode.RightHand;
    public XRLocomotion Locomotion;
    public DemoLighting Lighting;
    public bool Teleports = true;
    public bool SnapTurns;
    public float ArcSpeed = 6f;
    public DemoToolKind SpawnKind = DemoToolKind.Cone;
    public TextMesh Label;

    private const int ArcPositions = 24;
    private static readonly int KindCount = Enum.GetValues(typeof(DemoToolKind)).Length;
    private static bool hintsVisible = true;

    private HandInteractor hand;
    private bool grip, trigger, primary, secondary, stickClick, menu, aiming, aimValid, turned;
    private Vector3 aimPoint;
    private Transform teleportMarker;
    private Renderer backing;
    private LineRenderer arcLine;
    private readonly List<Vector3> arcPoints = new List<Vector3>();

    private void Awake()
    {
        hand = GetComponent<HandInteractor>();
        CreateBacking();
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
        if (teleportMarker != null) Destroy(teleportMarker.gameObject);
        if (arcLine != null) Destroy(arcLine.gameObject);
    }

    private void Update()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(Node);
        if (!device.isValid) { CancelAim(); return; }
        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) transform.localPosition = position;
        if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation)) transform.localRotation = rotation;

        if (Changed(device, CommonUsages.gripButton, ref grip))
        {
            if (grip) hand.Grab(); else hand.Release();
        }
        if (Changed(device, CommonUsages.triggerButton, ref trigger) && trigger) hand.Trigger();
        if (Changed(device, CommonUsages.primaryButton, ref primary) && primary) hand.SpawnIntoHand(SpawnKind);
        if (Changed(device, CommonUsages.secondaryButton, ref secondary) && secondary)
        {
            if (hand.Held != null) hand.RemoveHeld();
            else { SpawnKind = (DemoToolKind)(((int)SpawnKind + 1) % KindCount); RefreshLabel(); }
        }
        if (Lighting != null && Changed(device, CommonUsages.primary2DAxisClick, ref stickClick) && stickClick) Lighting.ToggleFlashlight();
        if (Node == XRNode.LeftHand && Changed(device, CommonUsages.menuButton, ref menu) && menu) ToggleHints();
        if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick)) Stick(stick);
    }

    // Small text on the back of the hand: shared bindings in two lines, then which
    // tool A/X spawns and the B/Y hint.
    private void RefreshLabel()
    {
        if (Label == null) return;
        bool left = Node == XRNode.LeftHand;
        Label.text = "grip: grab   trigger: use"
            + "\nstick: " + (Teleports ? "teleport" : "turn") + (Lighting != null ? "   click: light" : left ? "   menu: hints" : "")
            + "\n" + (left ? "X" : "A") + ": new " + DeployedTool.KindName(SpawnKind)
            + "   " + (left ? "Y" : "B") + ": remove / next kind";
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
        if (Label != null)
        {
            Renderer text = Label.GetComponent<Renderer>();
            if (text != null) text.enabled = hintsVisible;
        }
        if (backing != null) backing.enabled = hintsVisible;
    }

    private void Stick(Vector2 stick)
    {
        if (Teleports && Locomotion != null) Teleport(stick.y);
        if (SnapTurns && Locomotion != null)
        {
            if (!turned && stick.x > 0.7f) { Locomotion.SnapRight(); turned = true; }
            else if (!turned && stick.x < -0.7f) { Locomotion.SnapLeft(); turned = true; }
            else if (Mathf.Abs(stick.x) < 0.3f) turned = false;
        }
    }

    // While the stick is pushed forward the arc shows the trajectory and the marker the
    // landing; releasing the stick teleports to the last valid destination shown.
    private void Teleport(float push)
    {
        bool pushed = push > 0.7f;
        if (pushed)
        {
            aimValid = Locomotion.FindArcDestination(transform.position, transform.forward, ArcSpeed, arcPoints,
                out Vector3 point, hand.Held != null ? hand.Held.transform : null);
            if (aimValid) { aimPoint = point; Marker().position = aimPoint + Vector3.up * 0.005f; }
            if (teleportMarker != null) teleportMarker.gameObject.SetActive(aimValid);
            DrawArc();
        }
        else if (aiming && push < 0.3f)
        {
            if (aimValid) Locomotion.TryTeleport(aimPoint);
            CancelAim();
        }
        aiming = pushed || (aiming && push >= 0.3f);
    }

    private Transform Marker()
    {
        if (teleportMarker != null) return teleportMarker;
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "TeleportMarker";
        Destroy(marker.GetComponent<Collider>());
        marker.transform.localScale = new Vector3(0.4f, 0.01f, 0.4f);
        teleportMarker = marker.transform;
        return teleportMarker;
    }

    // Green while the landing is valid, red otherwise. The samples are evenly thinned
    // to the renderer's position budget so the whole arc always shows.
    private void DrawArc()
    {
        LineRenderer line = Arc();
        int count = Mathf.Min(arcPoints.Count, ArcPositions);
        line.positionCount = count;
        for (int i = 0; i < count; i++)
            line.SetPosition(i, arcPoints[count < 2 ? 0 : i * (arcPoints.Count - 1) / (count - 1)]);
        line.startColor = line.endColor = aimValid ? Color.green : Color.red;
        line.enabled = count > 1;
    }

    private LineRenderer Arc()
    {
        if (arcLine != null) return arcLine;
        GameObject arc = new GameObject("TeleportArc");
        arcLine = arc.AddComponent<LineRenderer>();
        arcLine.useWorldSpace = true;
        arcLine.startWidth = arcLine.endWidth = 0.01f;
        arcLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        arcLine.receiveShadows = false;
        arcLine.sharedMaterial = UnlitMaterial(Color.white);
        return arcLine;
    }

    // Dark quad just behind the label text so the hints stay readable on any background.
    private void CreateBacking()
    {
        if (Label == null || backing != null) return;
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "LabelBacking";
        Collider blocker = quad.GetComponent<Collider>();
        blocker.enabled = false; // Destroy only lands at frame end; keep grabs unobstructed now.
        Destroy(blocker);
        quad.transform.SetParent(Label.transform, false);
        quad.transform.localPosition = new Vector3(0, 0.028f, 0.004f);
        quad.transform.localScale = new Vector3(0.14f, 0.06f, 1f);
        backing = quad.GetComponent<MeshRenderer>();
        backing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        backing.receiveShadows = false;
        backing.sharedMaterial = UnlitMaterial(new Color(0, 0, 0, 0.75f));
    }

    private static Material UnlitMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    // True when the button state changed; state holds the new value.
    private static bool Changed(InputDevice device, InputFeatureUsage<bool> usage, ref bool state)
    {
        if (!device.TryGetFeatureValue(usage, out bool now) || now == state) return false;
        state = now;
        return true;
    }
}
