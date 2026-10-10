using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// One rig, two ways to drive it. Desktop components (walking, pointer) run with
// mouse and keyboard; VR components (head and controller tracking, hands) run
// when a headset is active. Auto picks VR only when an XR device is running, so
// Play mode without a headset is always the desktop demo. F9 flips for testing.
public sealed class DemoModeSwitch : MonoBehaviour
{
    public enum DemoMode { Auto, Desktop, VR }

    public DemoMode Mode = DemoMode.Auto;
    public Behaviour[] DesktopOnly = new Behaviour[0];
    public Collider[] DesktopColliders = new Collider[0];
    public Behaviour[] VROnly = new Behaviour[0];
    public GameObject[] VRObjects = new GameObject[0];
    public bool VRActive { get; private set; }

    private bool applied;

    private void Start() { Sync(); }

    // Auto keeps following the runtime: Quest Link or Air Link may come up after Play
    // starts. F9 pins the mode by hand and stops the automatic choice.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9)) { Mode = VRActive ? DemoMode.Desktop : DemoMode.VR; Apply(!VRActive); return; }
        if (Mode == DemoMode.Auto) Sync();
    }

    private void Sync()
    {
        bool vr = Mode == DemoMode.VR || (Mode == DemoMode.Auto && XRSettings.isDeviceActive);
        if (!applied || vr != VRActive) Apply(vr);
    }

    public void Apply(bool vr)
    {
        // Release before disabling hand objects: Unity forbids reparenting a child
        // from inside its parent\'s SetActive/OnDisable traversal.
        foreach (ToolHolder holder in GetComponentsInChildren<ToolHolder>(true)) holder.ReleaseAll();
        VRActive = vr;
        applied = true;
        if (vr) SetFloorTrackingOrigin();
        foreach (Behaviour b in DesktopOnly) if (b != null) b.enabled = !vr;
        foreach (Collider c in DesktopColliders) if (c != null) c.enabled = !vr;
        foreach (Behaviour b in VROnly) if (b != null) b.enabled = vr;
        foreach (GameObject g in VRObjects) if (g != null) g.SetActive(vr);
    }

    // Poses are floor relative, so the rig root stands on the floor at the start position.
    private static void SetFloorTrackingOrigin()
    {
        List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        foreach (XRInputSubsystem subsystem in subsystems)
            if (subsystem.running) subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
    }
}
