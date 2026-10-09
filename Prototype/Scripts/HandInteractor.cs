using UnityEngine;

// Tracked-hand adapter without any toolkit dependency. Something else moves this
// transform (XRControllerInput now, a TrackedPoseDriver if ever needed) and
// input bindings call Grab, Release, Trigger, SpawnIntoHand and RemoveHeld.
// Held tools hang below the hand with the hand's heading; releasing drops them
// onto the surface beneath (ToolHolder.Place). The nearest grabbable tool is
// highlighted while the hand is empty.
public sealed class HandInteractor : ToolHolder
{
    public float GrabRadius = 0.25f;
    public Vector3 HeldOffset = new Vector3(0, -0.15f, 0);

    protected override Transform CameraAnchor => transform;

    private readonly Collider[] overlaps = new Collider[32];
    private ToolRackSample nearSample;
    private bool nearCamera;

    private void Update()
    {
        if (Held != null) FollowHand();
        else Highlight(NearestTool());
    }

    public void FollowHand()
    {
        if (Held != null) Held.PlaceAt(transform.TransformPoint(HeldOffset), transform.eulerAngles.y);
    }

    // Grab the nearest tool within reach; otherwise a rack sample (new tool) or the camera.
    public bool Grab()
    {
        if (Held != null || HoldingCamera) return false;
        DeployedTool tool = NearestTool();
        if (tool != null) { Hold(tool); return true; }
        if (nearSample != null) return TakeSample(nearSample) != null;
        if (nearCamera) { ToggleCamera(); return true; }
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
        Highlight(Held);
        FollowHand();
    }

    // Nearest tool within GrabRadius; also notes a rack sample or the camera in reach.
    private DeployedTool NearestTool()
    {
        nearSample = null;
        nearCamera = false;
        DeployedTool best = null;
        float bestDistance = float.MaxValue;
        int count = Physics.OverlapSphereNonAlloc(transform.position, GrabRadius, overlaps);
        for (int i = 0; i < count; i++)
        {
            Collider collider = overlaps[i];
            DeployedTool tool = collider.GetComponentInParent<DeployedTool>();
            if (tool == null)
            {
                if (nearSample == null) nearSample = collider.GetComponentInParent<ToolRackSample>();
                nearCamera |= EvidenceCamera != null && collider.GetComponentInParent<EvidenceCamera>() == EvidenceCamera;
                continue;
            }
            float distance = (collider.ClosestPoint(transform.position) - transform.position).sqrMagnitude;
            if (distance < bestDistance) { best = tool; bestDistance = distance; }
        }
        return best;
    }
}
