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

    private HandInteractor hand;
    private bool grip, trigger, primary, secondary, aiming, turned;
    private Transform teleportMarker;

    private void Awake()
    {
        hand = GetComponent<HandInteractor>();
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "TeleportMarker";
        Destroy(marker.GetComponent<Collider>());
        marker.transform.localScale = new Vector3(0.4f, 0.01f, 0.4f);
        marker.SetActive(false);
        teleportMarker = marker.transform;
        RefreshLabel();
    }

    // Small text on the back of the hand: which tool A/X spawns, and the grip hint.
    private void RefreshLabel()
    {
        if (Label == null) return;
        Label.text = (Node == XRNode.LeftHand ? "X" : "A") + ": new " + SpawnKind.ToString().ToLowerInvariant()
            + "\n" + (Node == XRNode.LeftHand ? "Y" : "B") + ": remove / next kind";
    }

    private void OnDisable()
    {
        aiming = false;
        if (teleportMarker != null) teleportMarker.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (teleportMarker != null) Destroy(teleportMarker.gameObject);
    }

    private void Update()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(Node);
        if (!device.isValid) return;
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
            else { SpawnKind = (DemoToolKind)(((int)SpawnKind + 1) % 3); RefreshLabel(); }
        }
        if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick)) Stick(stick);
    }

    private void Stick(Vector2 stick)
    {
        if (Teleports && Locomotion != null)
        {
            bool pushed = stick.y > 0.7f;
            Ray ray = new Ray(transform.position, transform.forward);
            bool valid = pushed && Physics.Raycast(ray, out RaycastHit hit, Locomotion.MaxTeleportDistance)
                && hit.normal.y >= 0.7f && (Locomotion.Bounds == null || Locomotion.Bounds.Contains(hit.point));
            teleportMarker.gameObject.SetActive(valid);
            if (valid) teleportMarker.position = hit.point + Vector3.up * 0.005f;
            // Teleport when the stick is released after aiming at a valid spot.
            if (aiming && !pushed && stick.y < 0.3f) Locomotion.TryTeleport(ray);
            aiming = pushed;
        }
        if (SnapTurns && Locomotion != null)
        {
            if (!turned && stick.x > 0.7f) { Locomotion.SnapRight(); turned = true; }
            else if (!turned && stick.x < -0.7f) { Locomotion.SnapLeft(); turned = true; }
            else if (Mathf.Abs(stick.x) < 0.3f) turned = false;
        }
    }

    // True when the button state changed; state holds the new value.
    private static bool Changed(InputDevice device, InputFeatureUsage<bool> usage, ref bool state)
    {
        if (!device.TryGetFeatureValue(usage, out bool now) || now == state) return false;
        state = now;
        return true;
    }
}
