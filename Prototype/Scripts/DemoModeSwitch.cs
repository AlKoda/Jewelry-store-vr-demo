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

    private void Start()
    {
        Apply(Mode == DemoMode.VR || (Mode == DemoMode.Auto && XRSettings.isDeviceActive));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9)) Apply(!VRActive);
    }

    public void Apply(bool vr)
    {
        // Release before disabling hand objects: Unity forbids reparenting a child
        // from inside its parent\'s SetActive/OnDisable traversal.
        foreach (ToolHolder holder in GetComponentsInChildren<ToolHolder>(true)) holder.ReleaseAll();
        VRActive = vr;
        foreach (Behaviour b in DesktopOnly) if (b != null) b.enabled = !vr;
        foreach (Collider c in DesktopColliders) if (c != null) c.enabled = !vr;
        foreach (Behaviour b in VROnly) if (b != null) b.enabled = vr;
        foreach (GameObject g in VRObjects) if (g != null) g.SetActive(vr);
    }
}
