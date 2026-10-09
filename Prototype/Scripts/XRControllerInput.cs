using System;
using UnityEngine;
using UnityEngine.XR;

// Drives one HandInteractor from a tracked controller through Unity's built-in
// XR input API (no package required in the project). Grip holds, trigger uses,
// the primary button (A/X) spawns the current tool kind, the secondary button
// (B/Y) removes the held tool or, with empty hands, cycles the kind. The
// thumbstick teleports (push forward, release) or snap turns (left/right),
// depending on which role this hand has.
[RequireComponent(typeof(HandInteractor))]
public sealed class XRControllerInput : MonoBehaviour
{
    public XRNode Node = XRNode.RightHand;
    public XRLocomotion Locomotion;
    public bool Teleports = true;
    public bool SnapTurns;
    public DemoToolKind SpawnKind = DemoToolKind.Cone;
    public TextMesh Label;

    private static readonly int KindCount = Enum.GetValues(typeof(DemoToolKind)).Length;
    private HandInteractor hand;
    private bool grip, trigger, primary, secondary, aiming, aimValid, turned;
    private Vector3 aimPoint;
    private Transform teleportMarker;

    private void Awake()
    {
        hand = GetComponent<HandInteractor>();
        RefreshLabel();
    }

    private void OnDisable() { CancelAim(); }

    // Forget any destination shown so far, e.g. when tracking drops or the hand is disabled.
    private void CancelAim()
    {
        aiming = aimValid = false;
        if (teleportMarker != null) teleportMarker.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (teleportMarker != null) Destroy(teleportMarker.gameObject);
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
        if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick)) Stick(stick);
    }

    // Small text on the back of the hand: which tool A/X spawns, and the B/Y hint.
    private void RefreshLabel()
    {
        if (Label == null) return;
        bool left = Node == XRNode.LeftHand;
        Label.text = (left ? "X" : "A") + ": new " + SpawnKind.ToString().ToLowerInvariant()
            + "\n" + (left ? "Y" : "B") + ": remove / next kind";
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

    // While the stick is pushed forward the marker shows the destination; releasing
    // the stick teleports to the last destination shown, not to a fresh ray.
    private void Teleport(float push)
    {
        bool pushed = push > 0.7f;
        if (pushed)
        {
            aimValid = Locomotion.FindDestination(new Ray(transform.position, transform.forward), out Vector3 point);
            if (aimValid) { aimPoint = point; Marker().position = aimPoint + Vector3.up * 0.005f; }
            if (teleportMarker != null) teleportMarker.gameObject.SetActive(aimValid);
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

    // True when the button state changed; state holds the new value.
    private static bool Changed(InputDevice device, InputFeatureUsage<bool> usage, ref bool state)
    {
        if (!device.TryGetFeatureValue(usage, out bool now) || now == state) return false;
        state = now;
        return true;
    }
}
