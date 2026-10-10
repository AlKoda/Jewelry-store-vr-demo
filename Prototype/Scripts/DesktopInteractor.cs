using UnityEngine;

// Desktop pointer adapter: the pointer is the mouse, or the screen centre while
// the right button locks the cursor for looking. Picks up, carries, rotates,
// places and removes tools, takes new ones from the rack samples, holds the
// camera, drops the instructor's beacon and reads hotkeys. Shared holding rules
// live in ToolHolder; XR hands use HandInteractor instead.
public sealed class DesktopInteractor : ToolHolder
{
    public Camera View;
    public ShopWalkController Walker;
    public DemoDesktopPanel Panel;
    public DemoLighting Lighting;
    public DemoBeacon Beacon;
    public float Reach = 4f;
    public float RotateStep = 15f;
    public bool ReadDesktopInput = true;

    public DeployedTool Hovered { get; private set; }
    public ToolRackSample HoveredSample { get; private set; }
    public bool HoveringCamera { get; private set; }
    // The surface the pointer last reached, whatever it was; the beacon key uses it.
    public bool PointerHit { get; private set; }
    public Vector3 PointedPoint { get; private set; }

    protected override Transform CameraAnchor => View != null ? View.transform : null;

    private float heldYaw;
    public bool HasPlacementTarget { get; private set; }

    private void Update()
    {
        if (!ReadDesktopInput || View == null || (Panel != null && Panel.CapturesInput)) return;
        Aim(PointerRay());

        bool overPanel = Panel != null && Panel.PointerOverPanel;
        if (Input.GetMouseButtonDown(0) && !overPanel)
        {
            if (Held != null) Place();
            else if (Hovered != null) Hold(Hovered);
            else if (HoveredSample != null) TakeSample(HoveredSample);
            else if (HoveringCamera) ToggleCamera();
        }
        if (Input.GetKeyDown(KeyCode.Alpha1)) SpawnIntoHand(DemoToolKind.Cone);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SpawnIntoHand(DemoToolKind.Marker);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SpawnIntoHand(DemoToolKind.TapePost);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SpawnIntoHand(DemoToolKind.Scale);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SpawnIntoHand(DemoToolKind.Measure);
        if (Input.GetKeyDown(KeyCode.Q)) Rotate(-RotateStep);
        if (Input.GetKeyDown(KeyCode.E)) Rotate(RotateStep);
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (wheel != 0 && !overPanel) Rotate(Mathf.Sign(wheel) * RotateStep);
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) Remove(Target);
        if (Input.GetKeyDown(KeyCode.T)) SelectTapePost(Target);
        if (Input.GetKeyDown(KeyCode.X) && Station != null) Station.CancelTapeSelection();
        if (Input.GetKeyDown(KeyCode.F)) ToggleCamera();
        if (Input.GetKeyDown(KeyCode.P)) Photograph();
        if (Input.GetKeyDown(KeyCode.L) && Lighting != null) Lighting.ToggleFlashlight();
        if (Input.GetKeyDown(KeyCode.N) && Lighting != null) Lighting.ToggleNight();
        if (Input.GetKeyDown(KeyCode.B) && Beacon != null && PointerHit) Beacon.Place(PointedPoint);
    }

    public void Aim(Ray ray)
    {
        if (Held != null)
        {
            Hovered = null;
            HoveredSample = null;
            HoveringCamera = false;
            Carry(ray);
        }
        else
        {
            bool hit = Point(ray, null, out RaycastHit nearest);
            Hovered = hit ? nearest.collider.GetComponentInParent<DeployedTool>() : null;
            if (Hovered != null && !Hovered.Free) Hovered = null;
            HoveredSample = hit && Hovered == null ? nearest.collider.GetComponentInParent<ToolRackSample>() : null;
            HoveringCamera = hit && Hovered == null && HoveredSample == null && EvidenceCamera != null &&
                nearest.collider.GetComponentInParent<EvidenceCamera>() == EvidenceCamera;
        }
        Highlight(Held != null ? Held : Hovered);

    }

    public DeployedTool Target => Held != null ? Held : Hovered;

    // Through the held camera's lens, or of the current view when it is on its rack.
    public void Photograph()
    {
        if (EvidenceCamera == null) return;
        if (HoldingCamera || View == null) EvidenceCamera.CapturePhoto();
        else EvidenceCamera.CaptureFromView(View.transform);
    }

    public Ray PointerRay()
    {
        bool centred = Walker != null && Walker.Looking;
        Vector3 point = centred ? new Vector3(Screen.width / 2f, Screen.height / 2f) : Input.mousePosition;
        return View.ScreenPointToRay(point);
    }

    public override void Hold(DeployedTool tool)
    {
        base.Hold(tool);
        if (Held == null) return;
        Hovered = null;
        Highlight(Held);
        heldYaw = Held.transform.eulerAngles.y;
        HasPlacementTarget = false;
        // A tool taken from the full panel waits at the rack until the sidebar closes.
        bool panelOpen = Panel != null && Panel.CapturesInput;
        if (ReadDesktopInput && View != null && !panelOpen) Carry(PointerRay());
    }

    // Move the held tool to the pointed surface. Only upward-facing surfaces
    // inside the interior count; otherwise it stays where it was.
    public void Carry(Ray ray)
    {
        HasPlacementTarget = false;
        if (Held == null || !Point(ray, Held.transform, out RaycastHit hit) || hit.normal.y < 0.7f) return;
        Held.PlaceAt(Confine(hit.point), heldYaw);
        HasPlacementTarget = true;
    }

    // The one pointer raycast: nearest surface within reach, skipping the rig and
    // the given tool, remembered for the beacon key.
    private bool Point(Ray ray, Transform ignore, out RaycastHit hit)
    {
        PointerHit = Raycast(ray, Reach, out hit, ignore, Walker != null ? Walker.transform : null);
        PointedPoint = PointerHit ? hit.point : ray.origin;
        return PointerHit;
    }

    public override void Place()
    {
        if (!HasPlacementTarget) return;
        base.Place();
        HasPlacementTarget = false;
    }

    public void Rotate(float degrees)
    {
        if (Held == null) return;
        heldYaw += degrees;
        Held.PlaceAt(Held.transform.position, heldYaw);
    }

    public override void Remove(DeployedTool tool)
    {
        if (tool != null && tool == Hovered) Hovered = null;
        base.Remove(tool);
    }

    public override void ReleaseAll()
    {
        base.ReleaseAll();
        Hovered = null;
        HoveredSample = null;
        HoveringCamera = false;
        PointerHit = false;
    }
}
