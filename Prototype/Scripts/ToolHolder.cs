using UnityEngine;

// Everything an interaction adapter shares, whatever drives it: one held tool
// at a time, spawning into the hand, removal, tape selection, holding the
// evidence camera, and releasing it all before a session reset so parents and
// poses restore. Tools stay under the station's DeploymentRoot while held, and
// a tool held by one holder cannot be taken by another.
public abstract class ToolHolder : MonoBehaviour
{
    public ToolStation Station;
    public DemoSession Session;
    public EvidenceCamera EvidenceCamera;
    public InteriorBounds Bounds;
    public Vector3 CameraHoldOffset = new Vector3(0.22f, -0.12f, 0.35f);
    public float DropSearchDepth = 2f;

    public DeployedTool Held { get; private set; }
    public bool HoldingCamera =>
        EvidenceCamera != null && CameraAnchor != null && EvidenceCamera.Holder == CameraAnchor;

    // Where the camera sits while this holder has it.
    protected abstract Transform CameraAnchor { get; }

    private DeployedTool highlighted;

    protected virtual void OnEnable()
    {
        if (Session != null) Session.Resetting.AddListener(ReleaseAll);
    }

    protected virtual void OnDisable()
    {
        if (Session != null) Session.Resetting.RemoveListener(ReleaseAll);
        ReleaseAll();
    }

    // A new tool straight into the hand; refused while the camera is held.
    public DeployedTool SpawnIntoHand(DemoToolKind kind)
    {
        if (Station == null || HoldingCamera) return null;
        DeployedTool tool = Station.Spawn(kind);
        Hold(tool);
        return tool;
    }

    public DeployedTool TakeSample(ToolRackSample sample)
        => sample != null ? SpawnIntoHand(sample.Kind) : null;

    // Taking a second tool first puts the current one down. A tool another
    // holder has, or anything while the camera is held, is refused.
    public virtual void Hold(DeployedTool tool)
    {
        if (tool == null || tool == Held || HoldingCamera || !tool.Free) return;
        if (Held != null) Settle(true);
        Held = tool;
        tool.Holder = this;
        DemoSounds.Play(DemoSound.Click, tool.transform.position, 0.4f);
    }

    public virtual void Place() { Settle(true); }

    // Lets go of the held tool and sets it on the nearest upward surface beneath it,
    // inside the interior; casting from above the tool clears any case it is inside.
    private void Settle(bool audible)
    {
        DeployedTool tool = Held;
        Held = null;
        if (tool == null) return;
        tool.Holder = null;
        Vector3 origin = Confine(tool.transform.position) + Vector3.up;
        Vector3 landing = DemoPhysics.Nearest(new Ray(origin, Vector3.down), DropSearchDepth + 1, out RaycastHit hit, tool.transform, null, true)
            ? hit.point : new Vector3(origin.x, Mathf.Max(0, origin.y - 1), origin.z);
        tool.PlaceAt(landing, tool.transform.eulerAngles.y);
        if (audible) DemoSounds.Play(DemoSound.Thud, landing, 0.5f);
    }

    public virtual void Remove(DeployedTool tool)
    {
        if (tool == null || Station == null) return;
        if (tool == Held) { Held = null; tool.Holder = null; }
        if (tool == highlighted) highlighted = null;
        Station.RemoveTool(tool);
    }

    public void SelectTapePost(DeployedTool post)
    {
        if (Station != null) Station.SelectTapePost(post);
    }

    public void ToggleCamera()
    {
        if (EvidenceCamera == null || CameraAnchor == null) return;
        if (HoldingCamera) EvidenceCamera.ReturnToRack();
        else if (Held == null) EvidenceCamera.HoldBy(CameraAnchor, CameraHoldOffset);
    }

    // Quietly lets go of everything: before a reset, or when the holder is switched off.
    public virtual void ReleaseAll()
    {
        Highlight(null);
        Settle(false);
        if (HoldingCamera) EvidenceCamera.ReturnToRack();
    }

    // One highlighted tool per holder: the pointed, nearest or held one.
    protected void Highlight(DeployedTool tool)
    {
        if (tool == highlighted) return;
        if (highlighted != null) highlighted.SetHighlight(false);
        highlighted = tool;
        if (highlighted != null) highlighted.SetHighlight(true);
    }

    // Nearest point inside the interior, when bounds are assigned.
    protected Vector3 Confine(Vector3 point) => Bounds != null ? Bounds.Clamp(point) : point;

    protected static bool Raycast(Ray ray, float distance, out RaycastHit nearest, Transform ignore = null, Transform alsoIgnore = null)
        => DemoPhysics.Nearest(ray, distance, out nearest, ignore, alsoIgnore);
}
