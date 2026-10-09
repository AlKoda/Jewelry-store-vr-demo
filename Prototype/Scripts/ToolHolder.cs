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

    public DeployedTool Held { get; private set; }
    public bool HoldingCamera => EvidenceCamera != null && EvidenceCamera.Holder == CameraAnchor;

    // Where the camera sits while this holder has it.
    protected abstract Transform CameraAnchor { get; }

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

    public virtual void Hold(DeployedTool tool)
    {
        if (tool != null) Held = tool;
    }

    public virtual void Place() { Held = null; }

    public void Remove(DeployedTool tool)
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
        Held = null;
        if (HoldingCamera) EvidenceCamera.ReturnToRack();
    }

    // Nearest point inside the interior, when bounds are assigned.
    protected Vector3 Confine(Vector3 point) => Bounds != null ? Bounds.Clamp(point) : point;

    private readonly RaycastHit[] hits = new RaycastHit[16];

    // Nearest hit along the ray, skipping colliders under the ignored transforms
    // (the held tool, the player) so they never block their own placement.
    protected bool Raycast(Ray ray, float distance, out RaycastHit nearest, params Transform[] ignore)
    {
        nearest = default;
        bool found = false;
        int count = Physics.RaycastNonAlloc(ray, hits, distance);
        for (int i = 0; i < count; i++)
        {
            if (Ignored(hits[i].transform, ignore)) continue;
            if (!found || hits[i].distance < nearest.distance) { nearest = hits[i]; found = true; }
        }
        return found;
    }

    private static bool Ignored(Transform hit, Transform[] ignore)
    {
        foreach (Transform root in ignore)
            if (root != null && hit.IsChildOf(root)) return true;
        return false;
    }
}
