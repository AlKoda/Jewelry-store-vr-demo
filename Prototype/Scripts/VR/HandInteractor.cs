using UnityEngine;

// Tracked-hand adapter without any toolkit dependency. Something else moves this
// transform (XRControllerInput now, a TrackedPoseDriver if ever needed) and
// input bindings call Grab, Release, Trigger, SpawnIntoHand and RemoveHeld.
// Held tools hang below the hand and turn freely with it (FreeRotation); releasing drops them
// onto the surface beneath (ToolHolder.Place). The nearest grabbable tool is
// highlighted while the hand is empty.
public sealed class HandInteractor : ToolHolder
{
    public float GrabRadius = 0.25f;
    public Vector3 HeldOffset = new Vector3(0, -0.15f, 0);
    // A scene lamp is carried by its column, the feet just above the floor.
    public Vector3 LampHeldOffset = new Vector3(0, -0.9f, 0);
    // Fallback floor height without bounds; a carried lamp is clamped to the floor of
    // the region under it (Bounds.FloorAt: the night staging bay's street is lower).
    public float FloorY = 0f;
    // Held tools turn with the controller in every axis (a lamp still hangs upright);
    // off, they keep only the hand's heading as before. Release always stands the tool upright.
    public static bool FreeRotation = true;
    // Distance grab: with nothing within GrabRadius, a tool the controller points at within
    // PullReach (a ray of PullRadius) is highlighted, and grip pulls it into the hand over PullSeconds.
    public static bool DistanceGrab = true;
    public float PullReach = 4f;
    public float PullRadius = 0.08f;
    public float PullSeconds = 0.18f;

    protected override Transform CameraAnchor => transform;

    // Grows when a pile of tools fills it, so the nearest tool is never cut from the scan.
    private Collider[] overlaps = new Collider[32];
    private ToolRackSample nearSample;
    private bool nearCamera;
    private Vector3 pullFrom;
    private Quaternion pullFromRotation;
    private float pullStarted = -1;

    // Runs after XRControllerInput (execution order -100), so FollowHand uses this frame's
    // controller pose; ShopCityValidation invokes it by name.
    private void Update()
    {
        var controller=GetComponent<XRControllerInput>();
        if(controller!=null&&Application.isPlaying&&!controller.PoseTracked){Highlight(null);return;}
        if (Held != null) FollowHand();
        else
        {
            DeployedTool near = NearestTool();
            Highlight(near != null || nearSample != null || nearCamera ? near : PointedTool());
        }
    }

    public void FollowHand()
    {
        if (Held == null) return;
        // The lamp column hangs straight down whatever the wrist pitch, and its feet never sink below the floor.
        Vector3 p = Held.Kind == DemoToolKind.Lamp
            ? transform.position + Vector3.down * Mathf.Abs(LampHeldOffset.y)
            : transform.TransformPoint(HeldOffset);
        if (Held.Kind == DemoToolKind.Lamp) p.y = Mathf.Max(p.y, Bounds != null ? Bounds.FloorAt(p) : FloorY);
        Quaternion r = Held.Kind != DemoToolKind.Lamp && FreeRotation ? transform.rotation : Quaternion.Euler(0, Heading(transform), 0);
        // A pulled tool flies from where it lay to the hand, easing in.
        float t = pullStarted < 0 || PullSeconds <= 0 ? 1 : Mathf.Clamp01((Time.time - pullStarted) / PullSeconds);
        if (t < 1) { float e = 1 - (1 - t) * (1 - t); p = Vector3.Lerp(pullFrom, p, e); r = Quaternion.Slerp(pullFromRotation, r, e); }
        else pullStarted = -1;
        Held.transform.SetPositionAndRotation(p, r);
    }

    // Grab the closest available tool, rack sample or camera within reach.
    public bool Grab()
    {
        if (Held != null || HoldingCamera) return false;
        DeployedTool tool = NearestTool();
        if (tool != null) { Hold(tool); return true; }
        if (nearSample != null) return TakeSample(nearSample) != null;
        if (nearCamera) { ToggleCamera(); return true; }
        DeployedTool far = PointedTool();
        if (far == null) return false;
        pullFrom = far.transform.position;
        pullFromRotation = far.transform.rotation;
        pullStarted = Time.time;
        Hold(far);
        if (Held != far) { pullStarted = -1; return false; }
        DemoFeedback.Post(far.DisplayName + " in hand");
        return true;
    }

    public void Release()
    {
        if (Held != null) Place();
        else if (HoldingCamera) ToggleCamera();
    }

    // Use the held object: photograph with the camera, select a held tape post or reel,
    // switch a held scene lamp on or off.
    public void Trigger()
    {
        if (HoldingCamera && EvidenceCamera.Reviewing) EvidenceCamera.EndReview();
        else if (HoldingCamera) EvidenceCamera.CapturePhoto();
        else if (Held != null && Held.Lamp != null && Station != null) Station.ToggleLamp(Held);
        else if (Held != null && Held.TapeAnchor != null) SelectTapePost(Held);
        else { var lighting = FindFirstObjectByType<DemoLighting>(); if (lighting != null) lighting.UseTorch(transform); }
    }

    public void RemoveHeld() { Remove(Held); }

    public override void Hold(DeployedTool tool)
    {
        if(tool!=null&&!HoldingCamera){var lighting=FindFirstObjectByType<DemoLighting>();if(lighting)lighting.ClearHandTorch(transform);}
        base.Hold(tool);
        Highlight(Held);
        FollowHand();
    }

    // The free tool the controller points at within PullReach, or null; anything solid in front of it blocks the pull.
    public DeployedTool PointedTool()
    {
        if (!DistanceGrab || PullReach <= 0) return null;
        Ray ray = new Ray(transform.position, transform.forward);
        DeployedTool tool = null;
        float distance = float.MaxValue;
        foreach (RaycastHit h in Physics.SphereCastAll(ray, PullRadius, PullReach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (h.distance >= distance || h.collider.transform.IsChildOf(transform)) continue;
            DeployedTool candidate = h.collider.GetComponentInParent<DeployedTool>();
            if (candidate == null || !candidate.Free) continue;
            tool = candidate;
            distance = h.distance;
        }
        if (tool == null) return null;
        // Nothing but the tool between the hand and it (a case's glass, a wall).
        if (DemoPhysics.Nearest(ray, Mathf.Max(0, distance - 0.05f), out RaycastHit first, transform) &&
            first.collider.GetComponentInParent<DeployedTool>() != tool && first.distance > 0.02f) return null;
        return tool;
    }

    // Compare all grabbable types by distance; a nearby rack sample must not mask the camera.
    private DeployedTool NearestTool()
    {
        nearSample = null;
        nearCamera = false;
        DeployedTool best = null;
        float bestDistance = float.MaxValue;
        int count;
        while ((count = Physics.OverlapSphereNonAlloc(transform.position, GrabRadius, overlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) == overlaps.Length)
            overlaps = new Collider[overlaps.Length * 2];
        for (int i = 0; i < count; i++)
        {
            Collider collider = overlaps[i];
            DeployedTool tool = collider.GetComponentInParent<DeployedTool>();
            if (tool != null && !tool.Free) continue;
            ToolRackSample sample=tool==null?collider.GetComponentInParent<ToolRackSample>():null;
            bool camera=tool==null && EvidenceCamera!=null &&
                collider.GetComponentInParent<EvidenceCamera>()==EvidenceCamera;
            if(tool==null && sample==null && !camera) continue;
            float distance=(collider.ClosestPoint(transform.position)-transform.position).sqrMagnitude;
            if(distance<bestDistance)
            {
                bestDistance=distance;
                best=tool;
                nearSample=sample;
                nearCamera=camera;
            }
        }
        return best;
    }
}
