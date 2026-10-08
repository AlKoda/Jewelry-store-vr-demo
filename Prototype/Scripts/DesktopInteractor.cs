using UnityEngine;

// Desktop pointer adapter: pick up, carry, rotate, place and remove deployed
// tools, hold the evidence camera and read hotkeys. The pointer is the mouse,
// or the screen centre while the right button locks the cursor for looking.
// XR controllers will replace this adapter; the station/session API stays.
public sealed class DesktopInteractor : MonoBehaviour
{
    public Camera View;
    public ShopWalkController Walker;
    public ToolStation Station;
    public DemoSession Session;
    public EvidenceCamera EvidenceCamera;
    public InteriorBounds Bounds;
    public DemoDesktopPanel Panel;
    public float Reach = 4f;
    public float RotateStep = 15f;
    public Vector3 CameraHoldOffset = new Vector3(0.22f, -0.12f, 0.35f);
    public bool ReadDesktopInput = true;

    public DeployedTool Held { get; private set; }
    public DeployedTool Hovered { get; private set; }
    public bool HoveringCamera { get; private set; }
    public bool HoldingCamera { get; private set; }

    private float heldYaw;
    private Transform cameraRack;
    private Vector3 rackPosition;
    private Quaternion rackRotation;
    private readonly RaycastHit[] hits = new RaycastHit[16];

    private void OnEnable()
    {
        if (Session != null) Session.Resetting.AddListener(ReleaseAll);
    }

    private void OnDisable()
    {
        if (Session != null) Session.Resetting.RemoveListener(ReleaseAll);
        ReleaseAll();
    }

    private void Update()
    {
        if (!ReadDesktopInput || View == null) return;
        Ray ray = PointerRay();
        if (Held != null)
        {
            Hovered = null;
            HoveringCamera = false;
            Carry(ray);
        }
        else
        {
            bool hit = Raycast(ray, null, out RaycastHit nearest);
            Hovered = hit ? nearest.collider.GetComponentInParent<DeployedTool>() : null;
            HoveringCamera = hit && Hovered == null && EvidenceCamera != null &&
                nearest.collider.GetComponentInParent<EvidenceCamera>() == EvidenceCamera;
        }

        bool overPanel = Panel != null && Panel.PointerOverPanel;
        if (Input.GetMouseButtonDown(0) && !overPanel)
        {
            if (Held != null) Place();
            else if (Hovered != null) Hold(Hovered);
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
        if (Input.GetKeyDown(KeyCode.T) && Station != null) Station.SelectTapePost(Target);
        if (Input.GetKeyDown(KeyCode.X) && Station != null) Station.CancelTapeSelection();
        if (Input.GetKeyDown(KeyCode.F)) ToggleCamera();
        if (Input.GetKeyDown(KeyCode.P) && EvidenceCamera != null) EvidenceCamera.CapturePhoto();
    }

    private DeployedTool Target => Held != null ? Held : Hovered;

    public Ray PointerRay()
    {
        bool centred = Walker != null && Walker.Looking;
        Vector3 point = centred ? new Vector3(Screen.width / 2f, Screen.height / 2f) : Input.mousePosition;
        return View.ScreenPointToRay(point);
    }

    public DeployedTool SpawnIntoHand(DemoToolKind kind)
    {
        if (Station == null) return null;
        DeployedTool tool = Station.Spawn(kind);
        if (tool == null) return null;
        Hold(tool);
        if (View != null) Carry(PointerRay());
        return tool;
    }

    public void Hold(DeployedTool tool)
    {
        if (tool == null) return;
        Held = tool;
        Hovered = null;
        heldYaw = tool.transform.eulerAngles.y;
    }

    // Move the held tool to the pointed surface. Only upward-facing surfaces
    // inside the interior bounds count; otherwise it stays where it was.
    public void Carry(Ray ray)
    {
        if (Held == null || !Raycast(ray, Held.transform, out RaycastHit hit) || hit.normal.y < 0.7f) return;
        Vector3 point = hit.point;
        if (Bounds != null) point = Bounds.Clamp(point);
        Held.PlaceAt(point, heldYaw);
    }

    public void Rotate(float degrees)
    {
        if (Held == null) return;
        heldYaw += degrees;
        Held.PlaceAt(Held.transform.position, heldYaw);
    }

    public void Place() { Held = null; }

    public void Remove(DeployedTool tool)
    {
        if (tool == null || Station == null) return;
        if (tool == Held) Held = null;
        if (tool == Hovered) Hovered = null;
        Station.RemoveTool(tool);
    }

    // Camera held in front of the view, or returned to its rack. Its colliders
    // are disabled while held so they neither block walking nor the pointer.
    public void ToggleCamera()
    {
        if (EvidenceCamera == null || View == null) return;
        Transform body = EvidenceCamera.transform;
        HoldingCamera = !HoldingCamera;
        if (HoldingCamera)
        {
            cameraRack = body.parent;
            rackPosition = body.localPosition;
            rackRotation = body.localRotation;
            body.SetParent(View.transform, false);
            body.localPosition = CameraHoldOffset;
            body.localRotation = Quaternion.identity;
        }
        else
        {
            body.SetParent(cameraRack, false);
            body.localPosition = rackPosition;
            body.localRotation = rackRotation;
            cameraRack = null;
        }
        foreach (Collider collider in body.GetComponentsInChildren<Collider>(true))
            collider.enabled = !HoldingCamera;
    }

    // Releases everything before a session reset so parents and poses restore.
    public void ReleaseAll()
    {
        Held = null;
        Hovered = null;
        if (HoldingCamera) ToggleCamera();
    }

    private bool Raycast(Ray ray, Transform ignore, out RaycastHit nearest)
    {
        nearest = default;
        bool found = false;
        int count = Physics.RaycastNonAlloc(ray, hits, Reach);
        for (int i = 0; i < count; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (Walker != null && hitTransform == Walker.transform) continue;
            if (ignore != null && hitTransform.IsChildOf(ignore)) continue;
            if (!found || hits[i].distance < nearest.distance) { nearest = hits[i]; found = true; }
        }
        return found;
    }
}
