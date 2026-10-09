using UnityEngine;

// Everything an interaction adapter shares, whatever drives it: one held tool
// at a time, spawning into the hand, removal, tape selection, holding the
// evidence camera, and releasing it all before a session reset so parents and
// poses restore. Tools stay under the station's DeploymentRoot while held.
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

    private readonly RaycastHit[] hits = new RaycastHit[16];

    protected virtual void OnEnable()
    {
        if (Session != null) Session.Resetting.AddListener(ReleaseAll);
    }

    protected virtual void OnDisable()
    {
        if (Session != null) Session.Resetting.RemoveListener(ReleaseAll);
        ReleaseAll();
    }

    public DeployedTool SpawnIntoHand(DemoToolKind kind)
    {
        if (Station == null) return null;
        DeployedTool tool = Station.Spawn(kind);
        Hold(tool);
        return tool;
    }

    public DeployedTool TakeSample(ToolRackSample sample)
        => sample != null ? SpawnIntoHand(sample.Kind) : null;

    private DeployedTool highlighted;

    // One highlighted tool per holder: the pointed, nearest or held one.
    protected void Highlight(DeployedTool tool)
    {
        if (tool == highlighted) return;
        if (highlighted != null) highlighted.SetHighlight(false);
        highlighted = tool;
        if (highlighted != null) highlighted.SetHighlight(true);
    }

    // Taking a second tool first puts the current one down.
    public virtual void Hold(DeployedTool tool)
    {
        if (tool == null || tool == Held) return;
        if (Held != null) Place();
        Held = tool;
    }

    // Lets go of the held tool and settles it on the surface beneath, inside the
    // interior, so nothing is left floating at the rack or in mid-air.
    public virtual void Place()
    {
        DeployedTool tool = Held;
        Held = null;
        if (tool == null) return;
        Vector3 origin = Confine(tool.transform.position) + Vector3.up * 0.05f;
        Vector3 landing = Raycast(new Ray(origin, Vector3.down), DropSearchDepth, out RaycastHit hit, tool.transform)
            && hit.normal.y >= 0.7f ? hit.point : new Vector3(origin.x, Mathf.Max(0, origin.y - 0.05f), origin.z);
        tool.PlaceAt(landing, tool.transform.eulerAngles.y);
    }

    public virtual void Remove(DeployedTool tool)
    {
        if (tool == null || Station == null) return;
        if (tool == Held) Held = null;
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
        else EvidenceCamera.HoldBy(CameraAnchor, CameraHoldOffset);
    }

    public virtual void ReleaseAll()
    {
        Highlight(null);
        Place();
        if (HoldingCamera) EvidenceCamera.ReturnToRack();
    }

    // Nearest point inside the interior, when bounds are assigned.
    protected Vector3 Confine(Vector3 point) => Bounds != null ? Bounds.Clamp(point) : point;

    // Nearest hit along the ray, skipping colliders under the ignored transforms
    // (the held tool, the player) so they never block their own placement.
    protected bool Raycast(Ray ray, float distance, out RaycastHit nearest, Transform ignore = null, Transform alsoIgnore = null)
    {
        nearest = default;
        bool found = false;
        int count = Physics.RaycastNonAlloc(ray, hits, distance);
        for (int i = 0; i < count; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (ignore != null && hitTransform.IsChildOf(ignore)) continue;
            if (alsoIgnore != null && hitTransform.IsChildOf(alsoIgnore)) continue;
            if (!found || hits[i].distance < nearest.distance) { nearest = hits[i]; found = true; }
        }
        return found;
    }
}
