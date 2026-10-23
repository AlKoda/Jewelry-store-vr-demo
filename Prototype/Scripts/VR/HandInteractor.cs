using UnityEngine;

// Tracked-hand adapter without any toolkit dependency. Something else moves this
// transform (XRControllerInput now, a TrackedPoseDriver if ever needed) and
// input bindings call Grab, Release, Trigger, SpawnIntoHand and RemoveHeld.
// New tools use their authored hand grip; placed tools retain their pickup offset and
// full rotation through pickup, movement and release. The nearest grabbable tool is
// highlighted while the hand is empty.
public sealed class HandInteractor : ToolHolder
{
    public float GrabRadius = 0.18f;
    public Vector3 HeldOffset = new Vector3(0, -0.15f, 0);
    // A scene lamp is carried by its column, the feet just above the floor.
    public Vector3 LampHeldOffset = new Vector3(0, -0.9f, 0);
    // Fallback floor height without bounds; a carried lamp is clamped to the floor of
    // the region under it (Bounds.FloorAt: the night staging bay's street is lower).
    public float FloorY = 0f;
    // Held tools turn with the controller in every axis (a lamp still hangs upright);
    // off, fresh tools keep only the hand's heading. Repositioned tools always retain their full grip offset.
    public static bool FreeRotation = true;
    // Distance grab: with nothing within GrabRadius, a tool the controller points at within
    // PullReach (a ray of PullRadius) is highlighted; gripping preserves its existing offset too.
    public static bool DistanceGrab = true;
    public float PullReach = 4f;
    public float PullRadius = 0.08f;
    [HideInInspector] public float PullSeconds = 0.18f; // Legacy serialized setting; placed items no longer snap/pull.

    protected override Transform CameraAnchor => transform;

    // Grows when a pile of tools fills it, so the nearest tool is never cut from the scan.
    private Collider[] overlaps = new Collider[32];
    private ToolRackSample nearSample;
    private bool nearCamera;
    private bool preserveGrip, takingPlaced;
    private Vector3 placedOffset;
    private Quaternion placedRotation;
    protected override bool PreserveReleasePose => preserveGrip;

    // Runs after XRControllerInput (execution order -100), so FollowHand uses this frame's
    // controller pose; ShopCityValidation invokes it by name.
    void Start(){GrabRadius=Mathf.Min(GrabRadius,.18f);}
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
        if(preserveGrip){
            Held.transform.SetPositionAndRotation(transform.TransformPoint(placedOffset),transform.rotation*placedRotation);
            return;
        }
        Vector3 grip=GripPoint(Held.Kind);
        Quaternion r=Held.Kind!=DemoToolKind.Lamp&&FreeRotation?transform.rotation:Quaternion.Euler(0,Heading(transform),0);
        Vector3 p=transform.TransformPoint(new Vector3(0,-.045f,.005f))-r*grip;
        if(Held.Kind==DemoToolKind.Lamp)p.y=Mathf.Max(p.y,Bounds!=null?Bounds.FloorAt(p):FloorY);
        Held.transform.SetPositionAndRotation(p, r);
    }

    public static Vector3 GripPoint(DemoToolKind kind){switch(kind){
        case DemoToolKind.Cone:return new Vector3(0,.37f,0);
        case DemoToolKind.Marker:return new Vector3(0,.09f,0);
        case DemoToolKind.TapePost:return new Vector3(0,.78f,0);
        case DemoToolKind.Scale:return new Vector3(.01f,.003f,.065f);
        case DemoToolKind.Measure:return new Vector3(.025f,.015f,0);
        default:return new Vector3(0,.9f,0);
    }}
    // Grab the closest available tool, rack sample or camera within reach.
    public bool Grab()
    {
        if (Held != null || HoldingCamera || FieldTool || CarriedKit) return false;
        DeployedTool tool = NearestTool();
        if (tool != null) { takingPlaced=true;Hold(tool);takingPlaced=false;return Held==tool; }
        if (nearSample != null) return TakeSample(nearSample) != null;
        if (nearCamera) { ToggleCamera(); return true; }
        var kit=InvestigationInteraction.NearKit(this);if(kit)return TakeKit(kit);
        var fieldTool=InvestigationInteraction.Near(this);if(fieldTool)return TakeFieldTool(fieldTool);
        DeployedTool far = PointedTool();
        if (far == null) return false;
        takingPlaced=true;Hold(far);takingPlaced=false;
        if (Held != far) return false;
        DemoFeedback.Post(far.DisplayName + " in hand");
        return true;
    }

    public void Release()
    {
        if(CarriedKit)PutDownKit();
        else if(FieldTool)ReturnFieldTool();
        else if (Held != null) Place();
        else if (HoldingCamera) ToggleCamera();
    }

    // Use the held object: photograph with the camera, select a held tape post or reel,
    // switch a held scene lamp on or off.
    public void Trigger()
    {
        if(CarriedKit){}
        else if(FieldTool)InvestigationInteraction.Use(this,new Ray(transform.position,transform.forward),.65f);
        else if (HoldingCamera && EvidenceCamera.Reviewing) EvidenceCamera.EndReview();
        else if (HoldingCamera) EvidenceCamera.CapturePhoto();
        else if (Held != null && Held.Lamp != null && Station != null) Station.ToggleLamp(Held);
        else if (Held != null && Held.TapeAnchor != null) SelectTapePost(Held);
        else if(Held==null&&InvestigationInteraction.Use(this,new Ray(transform.position,transform.forward),2)){}
        else { var lighting = FindFirstObjectByType<DemoLighting>(); if (lighting != null) lighting.UseTorch(transform); }
    }

    public void RemoveHeld() { Remove(Held); }

    public override void Hold(DeployedTool tool)
    {
        if(tool==null||tool==Held||HoldingCamera||!tool.Free)return;
        var offset=transform.InverseTransformPoint(tool.transform.position);
        var rotation=Quaternion.Inverse(transform.rotation)*tool.transform.rotation;
        if(tool!=null&&!HoldingCamera){var lighting=FindFirstObjectByType<DemoLighting>();if(lighting)lighting.ClearHandTorch(transform);}
        base.Hold(tool);
        preserveGrip=takingPlaced||tool.HasBeenPlaced;
        placedOffset=offset;placedRotation=rotation;
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
        foreach (RaycastHit h in Physics.SphereCastAll(ray, PullRadius, PullReach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
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
        while ((count = Physics.OverlapSphereNonAlloc(transform.position, GrabRadius, overlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)) == overlaps.Length)
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
