using UnityEngine;

// Desktop pointer adapter: the pointer is the mouse, or the screen centre while
// the right button locks the cursor for looking. Picks up, carries, rotates,
// places and removes tools, takes new ones from the rack samples, holds the
// camera and reads hotkeys. Shared holding rules live in ToolHolder; XR hands
// use HandInteractor instead.
public sealed class DesktopInteractor : ToolHolder
{
    public Camera View;
    public ShopWalkController Walker;
    public DemoDesktopPanel Panel;
    public float Reach = 4f;
    public float RotateStep = 15f;
    public bool ReadDesktopInput = true;

    public DeployedTool Hovered { get; private set; }
    public ToolRackSample HoveredSample { get; private set; }
    public bool HoveringCamera { get; private set; }

    protected override Transform CameraAnchor => View != null ? View.transform : null;

    private float heldYaw;

    private void Update()
    {
        if (!ReadDesktopInput || View == null) return;
        Ray ray = PointerRay();
        if (Held != null)
        {
            Hovered = null;
            HoveredSample = null;
            HoveringCamera = false;
            Carry(ray);
        }
        else
        {
            bool hit = Raycast(ray, Reach, out RaycastHit nearest, Walker != null ? Walker.transform : null);
            Hovered = hit ? nearest.collider.GetComponentInParent<DeployedTool>() : null;
            HoveredSample = hit && Hovered == null ? nearest.collider.GetComponentInParent<ToolRackSample>() : null;
            HoveringCamera = hit && Hovered == null && HoveredSample == null && EvidenceCamera != null &&
                nearest.collider.GetComponentInParent<EvidenceCamera>() == EvidenceCamera;
        }
        Highlight(Held != null ? Held : Hovered);

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
        if (Input.GetKeyDown(KeyCode.Q)) Rotate(-RotateStep);
        if (Input.GetKeyDown(KeyCode.E)) Rotate(RotateStep);
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (wheel != 0) Rotate(Mathf.Sign(wheel) * RotateStep);
        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) Remove(Target);
        if (Input.GetKeyDown(KeyCode.T)) SelectTapePost(Target);
        if (Input.GetKeyDown(KeyCode.X) && Station != null) Station.CancelTapeSelection();
        if (Input.GetKeyDown(KeyCode.F)) ToggleCamera();
        if (Input.GetKeyDown(KeyCode.P)) Photograph();
    }

    private DeployedTool Target => Held != null ? Held : Hovered;

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
        if (ReadDesktopInput && View != null) Carry(PointerRay());
    }

    // Move the held tool to the pointed surface. Only upward-facing surfaces
    // inside the interior count; otherwise it stays where it was.
    public void Carry(Ray ray)
    {
        if (Held == null || !Raycast(ray, Reach, out RaycastHit hit, Held.transform,
            Walker != null ? Walker.transform : null) || hit.normal.y < 0.7f) return;
        Held.PlaceAt(Confine(hit.point), heldYaw);
    }

    public void Rotate(float degrees)
    {
        if (Held == null) return;
        heldYaw += degrees;
        Held.PlaceAt(Held.transform.position, heldYaw);
    }

    public override void ReleaseAll()
    {
        base.ReleaseAll();
        Hovered = null;
        HoveredSample = null;
        HoveringCamera = false;
    }
}
