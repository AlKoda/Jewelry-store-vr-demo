using UnityEngine;
using UnityEngine.XR;

// Body holsters in VR, placed from the head so they follow the trainee standing,
// sitting or bending: the white flashlight on the right hip, the UV torch on the left
// hip and the evidence camera on the chest. Grip with a hand inside a holster's
// TakeRadius takes its item into that hand (a torch comes on in that hand and stays
// there without holding grip); grip again at the same holster with that torch in the
// hand puts it back (off). The camera is held with grip like before: releasing grip
// returns it to its rack. A faint ring marks each holster and brightens when a hand is
// close. Desktop mode never shows them. XRControllerInput calls TryUse on every grip
// press before the normal grab; the validator calls the same methods with plain transforms.
public sealed class XRHolsters : MonoBehaviour
{
    public enum Slot { None = -1, Flashlight, UVTorch, Camera }

    public static XRHolsters Active { get; private set; }
    public Transform Head;
    public DemoLighting Lighting;
    public EvidenceCamera Camera;
    public float TakeRadius = 0.16f;
    // Offsets from the head in its heading frame (x right, y up, z forward).
    public Vector3 HipOffset = new Vector3(0.2f, -0.62f, 0.04f);
    public Vector3 ChestOffset = new Vector3(0, -0.36f, 0.13f);
    // Holsters never go lower than this above the rig's floor (a crouching trainee).
    public float MinHeight = 0.25f;

    private readonly Transform[] markers = new Transform[3];
    private readonly Material[] materials = new Material[3];
    private Transform root;
    private DemoModeSwitch mode;

    private void OnEnable() { Active = this; }

    private void OnDisable()
    {
        if (Active == this) Active = null;
        if (root != null) root.gameObject.SetActive(false);
    }

    public Vector3 Position(Slot slot)
    {
        Transform head = Head != null ? Head : transform;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 1e-4f) forward = Vector3.ProjectOnPlane(transform.up, Vector3.up);
        if (forward.sqrMagnitude < 1e-4f) forward = transform.forward;
        Quaternion heading = Quaternion.LookRotation(forward.normalized, Vector3.up);
        Vector3 offset = slot == Slot.Camera ? ChestOffset
            : new Vector3(slot == Slot.Flashlight ? HipOffset.x : -HipOffset.x, HipOffset.y, HipOffset.z);
        Vector3 p = head.position + heading * offset;
        p.y = Mathf.Max(p.y, transform.position.y + MinHeight);
        return p;
    }

    public Slot SlotNear(Vector3 hand)
    {
        Slot best = Slot.None;
        float bestDistance = TakeRadius;
        for (int i = 0; i < 3; i++)
        {
            float d = Vector3.Distance(hand, Position((Slot)i));
            if (d <= bestDistance) { bestDistance = d; best = (Slot)i; }
        }
        return best;
    }

    public bool TryUse(XRControllerInput input) => input != null && TryUse(input.Hand, input.Node);

    // Takes from or returns to the holster the hand is in; false when the hand is in none or nothing changed.
    public bool TryUse(HandInteractor hand, XRNode node)
    {
        if (hand == null || !VR) return false;
        Slot slot = SlotNear(hand.transform.position);
        DemoLighting lights = Lighting != null ? Lighting : FindFirstObjectByType<DemoLighting>();
        switch (slot)
        {
            case Slot.Flashlight:
            case Slot.UVTorch:
                if (lights == null || hand.Held != null || hand.HoldingCamera) return false;
                bool uv = slot == Slot.UVTorch;
                bool on = uv ? lights.UVTorchOn : lights.FlashlightOn;
                XRNode at = uv ? lights.UVTorchHand : lights.FlashlightHand;
                if (lights.TorchEquipped(uv) && at == node)
                {
                    lights.HolsterTorch(uv);
                    DemoFeedback.Post(uv ? "UV torch holstered" : "Flashlight holstered");
                    return true;
                }
                if(lights.TorchEquipped(!uv) && (uv?lights.FlashlightHand:lights.UVTorchHand)==node)return false;
                lights.MoveTorch(uv, node);
                if (!on) { if (uv) lights.ToggleUVTorch(); else lights.ToggleFlashlight(); }
                return true;
            case Slot.Camera:
                if (hand.Held != null || hand.HoldingCamera) return false;
                EvidenceCamera camera = Camera != null ? Camera : hand.EvidenceCamera;
                if (camera == null || (camera.Holder != null && camera.Holder != hand.transform)) return false;
                hand.ToggleCamera();
                return hand.HoldingCamera;
        }
        return false;
    }

    private bool VR
    {
        get
        {
            if (mode == null) mode = FindFirstObjectByType<DemoModeSwitch>();
            return mode == null || mode.VRActive;
        }
    }

    private void LateUpdate()
    {
        bool show = VR;
        if (!show) { if (root != null) root.gameObject.SetActive(false); return; }
        if (root == null) Build();
        root.gameObject.SetActive(true);
        HandInteractor[] hands = FindObjectsByType<HandInteractor>(FindObjectsSortMode.None);
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = Position((Slot)i);
            markers[i].position = p;
            if (Head != null) DemoUi.FaceViewer(markers[i], Head.position);
            bool near = false;
            foreach (HandInteractor h in hands) near |= Vector3.Distance(h.transform.position, p) <= TakeRadius;
            float proximity=float.PositiveInfinity;
            foreach(var h in hands)if(h.GetComponent<XRControllerInput>()?.PoseTracked==true)proximity=Mathf.Min(proximity,Vector3.Distance(h.transform.position,p));
            markers[i].gameObject.SetActive(proximity<.45f || XRControllerInput.HintsVisible);
            materials[i].color = DemoUi.WithAlpha(near ? DemoUi.Accent : DemoUi.Info, near ? 0.85f : 0.22f);
        }
    }

    private void Build()
    {
        root = new GameObject("Holsters").transform;
        root.SetParent(transform, false);
        UiIcon[] icons = { UiIcon.Flashlight, UiIcon.UVTorch, UiIcon.Camera };
        for (int i = 0; i < 3; i++)
        {
            GameObject marker = new GameObject("Holster_" + (Slot)i);
            marker.transform.SetParent(root, false);
            GameObject ring = new GameObject("Ring");
            ring.transform.SetParent(marker.transform, false);
            ring.transform.localRotation = Quaternion.Euler(90, 0, 0); // FlatRing lies flat; stand it up facing the eye
            ring.AddComponent<MeshFilter>().sharedMesh = DemoUi.FlatRing(0.05f, 0.006f);
            MeshRenderer r = ring.AddComponent<MeshRenderer>();
            materials[i] = DemoUi.UnlitMaterial(DemoUi.WithAlpha(DemoUi.Info, 0.22f));
            r.sharedMaterial = materials[i];
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(marker.transform, false);
            icon.AddComponent<MeshFilter>().sharedMesh = DemoUi.RoundedQuad(0.045f, 0.045f, 0, 1);
            MeshRenderer ir = icon.AddComponent<MeshRenderer>();
            ir.sharedMaterial = DemoUi.IconMaterial(icons[i], DemoUi.WithAlpha(DemoUi.Ink, 0.6f));
            ir.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            markers[i] = marker.transform;
        }
        DemoUi.ToHeadsetUiLayer(root);
    }

    private void OnDestroy()
    {
        foreach (Material m in materials) if (m != null) Destroy(m);
        if (root != null)
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
                if (r.sharedMaterial != null && System.Array.IndexOf(materials, r.sharedMaterial) < 0) Destroy(r.sharedMaterial);
    }
}
