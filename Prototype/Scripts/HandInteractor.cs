using UnityEngine;

// Tracked-hand adapter without any toolkit dependency. Something else moves this
// transform (a TrackedPoseDriver once XR packages are installed) and input
// bindings call Grab, Release, Trigger, SpawnIntoHand and RemoveHeld. Held
// tools hang below the hand with the hand's heading; releasing drops them onto
// the surface beneath (ToolHolder.Place).
public sealed class HandInteractor : ToolHolder
{
    public float GrabRadius = 0.25f;
    public Vector3 HeldOffset = new Vector3(0, -0.15f, 0);

    protected override Transform CameraAnchor => transform;

    private readonly Collider[] overlaps = new Collider[32];

    private void Update() { FollowHand(); }

    public void FollowHand()
    {
        if (Held != null) Held.PlaceAt(transform.TransformPoint(HeldOffset), transform.eulerAngles.y);
    }

    // Grab the nearest tool within reach, else the camera if it is within reach.
    public bool Grab()
    {
        if (Held != null || HoldingCamera) return false;
        DeployedTool tool = NearestTool(out bool cameraNear);
        if (tool != null) { Hold(tool); return true; }
        if (cameraNear) { ToggleCamera(); return true; }
        return false;
    }

    public void Release()
    {
        if (Held != null) Place();
        else if (HoldingCamera) ToggleCamera();
    }

    // Use the held object: photograph with the camera, select a held tape post.
    public void Trigger()
    {
        if (HoldingCamera) EvidenceCamera.CapturePhoto();
        else if (Held != null && Held.Kind == DemoToolKind.TapePost) SelectTapePost(Held);
    }

    public void RemoveHeld() { Remove(Held); }

    public override void Hold(DeployedTool tool)
    {
        base.Hold(tool);
        FollowHand();
    }

    private DeployedTool NearestTool(out bool cameraNear)
    {
        cameraNear = false;
        DeployedTool best = null;
        float bestDistance = float.MaxValue;
        int count = Physics.OverlapSphereNonAlloc(transform.position, GrabRadius, overlaps);
        for (int i = 0; i < count; i++)
        {
            DeployedTool tool = overlaps[i].GetComponentInParent<DeployedTool>();
            if (tool == null)
            {
                cameraNear |= EvidenceCamera != null && overlaps[i].GetComponentInParent<EvidenceCamera>() == EvidenceCamera;
                continue;
            }
            float distance = (overlaps[i].ClosestPoint(transform.position) - transform.position).sqrMagnitude;
            if (distance < bestDistance) { best = tool; bestDistance = distance; }
        }
        return best;
    }
}
