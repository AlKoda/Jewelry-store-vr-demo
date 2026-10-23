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
    // Drop casts start no higher than this above the region's floor: below every collidable ceiling
    // slab (3.0 m in both stores), above every case (2.1 m at most).
    public float DropCeilingClearance = 2.7f;

    public DeployedTool Held { get; private set; }
    public FieldInvestigationTool FieldTool { get; private set; }
    public PortableFieldKit CarriedKit {get;private set;}
    public bool TakeKit(PortableFieldKit kit){
        if(!kit||Held||HoldingCamera||FieldTool||CarriedKit)return false;
        if(this is HandInteractor&&Vector3.Distance(transform.position,kit.Grip.position)>.32f)return false;
        if(!kit.Take(this,CameraAnchor))return false;CarriedKit=kit;var lighting=FindFirstObjectByType<DemoLighting>();if(this is HandInteractor&&lighting)lighting.ClearHandTorch(CameraAnchor);
        DemoSounds.Play(DemoSound.Click,kit.Grip.position,.18f);DemoFeedback.Post("Field kit in hand");return true;
    }
    public void PutDownKit(bool home=false,bool audible=true){if(!CarriedKit)return;var kit=CarriedKit;CarriedKit=null;kit.Release(home,false,audible);}
    public bool PlaceKit(Ray ray,float reach){if(!CarriedKit||!CarriedKit.PlaceByRay(ray,reach))return false;var kit=CarriedKit;CarriedKit=null;kit.Release(false,true);return true;}
    public bool TakeFieldTool(FieldInvestigationTool tool){
        if(this is HandInteractor&&tool&&Vector3.Distance(transform.position,tool.transform.position)>.32f){DemoFeedback.Post("Move closer to take the field tool",FeedbackTone.Warning);return false;}
        if(!tool||Held||HoldingCamera||FieldTool||CarriedKit||!tool.Take(this,CameraAnchor))return false;
        FieldTool=tool;var lighting=FindFirstObjectByType<DemoLighting>();if(this is HandInteractor&&lighting)lighting.ClearHandTorch(CameraAnchor);
        DemoSounds.Play(DemoSound.Click,tool.transform.position,.2f);DemoFeedback.Post(tool.ToolName+" in hand");return true;
    }
    public void ReturnFieldTool(){if(!FieldTool)return;var tool=FieldTool;FieldTool=null;tool.ReturnToRack();DemoFeedback.Post("Field tool returned to its tray");}
    public bool HoldingCamera =>
        EvidenceCamera != null && CameraAnchor != null && EvidenceCamera.Holder == CameraAnchor;

    // Where the camera sits while this holder has it.
    protected abstract Transform CameraAnchor { get; }
    protected virtual bool PreserveReleasePose => false;

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
        PutDownKit();
        ReturnFieldTool();
        if (Station == null) return null;
        if (HoldingCamera)
        {
            DemoFeedback.Post("Put the camera back before taking a tool", FeedbackTone.Warning);
            return null;
        }
        DeployedTool tool = Station.Spawn(kind);
        Hold(tool);
        if (tool != null && Held == tool) DemoFeedback.Post(tool.DisplayName + " in hand");
        return tool;
    }

    public DeployedTool TakeSample(ToolRackSample sample)
        => sample != null ? SpawnIntoHand(sample.Kind) : null;

    // Taking a second tool first puts the current one down. A tool another
    // holder has, or anything while the camera is held, is refused.
    public virtual void Hold(DeployedTool tool)
    {
        if (tool == null || tool == Held || HoldingCamera || !tool.Free) return;
        PutDownKit();
        ReturnFieldTool();
        if (Held != null) Settle(true);
        Held = tool;
        tool.Holder = this;
        DemoSounds.Play(DemoSound.Click, tool.transform.position, 0.4f);
    }

    public virtual void Place()
    {
        DeployedTool placed = Held;
        bool first = placed != null && !placed.HasBeenPlaced; // read before Settle marks it placed
        Settle(true);
        if (placed != null) DemoFeedback.Post(placed.DisplayName + " placed", FeedbackTone.Success);
        if (Station != null) Station.NotifyPlaced(placed, first, this);
    }

    // Lets go of the held tool and sets it on the nearest upward surface beneath it,
    // inside the interior. The cast always reaches the floor of the region under the
    // tool, so a tool released overhead drops rather than floating or landing on the roof.
    private void Settle(bool audible)
    {
        DeployedTool tool = Held;
        Held = null;
        if (tool == null) return;
        tool.Holder = null;
        Vector3 p = Confine(tool.transform.position);
        float floor = Bounds != null ? Bounds.FloorAt(p) : 0;
        // Cast from 1 m above the tool, which clears any case it is inside, but never from inside or above the
        // ceiling slab; a miss lands on the region's floor (the street in the staging bay), not at the hand.
        Vector3 origin = new Vector3(p.x, Mathf.Min(p.y + 1, floor + DropCeilingClearance), p.z);
        float depth = Mathf.Max(DropSearchDepth + 1, origin.y - floor + 0.25f);
        Vector3 landing = DemoPhysics.Nearest(new Ray(origin, Vector3.down), depth, out RaycastHit hit, tool.transform, null, true)
            ? hit.point : new Vector3(p.x, floor, p.z);
        if (PreserveReleasePose) {
            // A re-grab keeps its full orientation and grip offset in the hand, then
            // settles onto a real supporting surface rather than hanging in mid-air.
            Vector3 confined = tool.transform.position;
            confined.x=p.x; confined.z=p.z; tool.transform.position=confined;
            float lowest=float.PositiveInfinity;
            foreach (var renderer in tool.GetComponentsInChildren<MeshRenderer>()) {
                if (!renderer.enabled || renderer.GetComponent<TextMesh>()!=null) continue;
                var filter=renderer.GetComponent<MeshFilter>();
                if (filter!=null && filter.sharedMesh!=null && filter.sharedMesh.isReadable) {
                    foreach (var vertex in filter.sharedMesh.vertices)
                        lowest=Mathf.Min(lowest,filter.transform.TransformPoint(vertex).y);
                } else if (filter!=null && filter.sharedMesh!=null) {
                    // Imported meshes can be unreadable on the headset. Transform
                    // their local bounds now; renderer bounds can lag the last pose.
                    var bounds=filter.sharedMesh.bounds;
                    for(int corner=0;corner<8;corner++) {
                        var v=bounds.center+Vector3.Scale(bounds.extents,new Vector3(
                            (corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                        lowest=Mathf.Min(lowest,filter.transform.TransformPoint(v).y);
                    }
                }
            }
            if (float.IsInfinity(lowest)) lowest=tool.transform.position.y;
            tool.transform.position+=Vector3.up*(landing.y-lowest);
        } else tool.PlaceAt(landing, Heading(tool.transform));
        tool.HasBeenPlaced=true;
        // A cone or marker sounds as itself when the bank has its takes, any other tool thuds.
        if (audible) DemoSounds.PlaySetDown(tool.Kind, landing, 0.5f);
    }

    public virtual void Remove(DeployedTool tool)
    {
        if (tool == null || Station == null) return;
        if (tool == Held) { Held = null; tool.Holder = null; }
        if (tool == highlighted) highlighted = null;
        string name = tool.DisplayName;
        Station.RemoveTool(tool);
        DemoFeedback.Post(name + " removed");
    }

    public void SelectTapePost(DeployedTool post)
    {
        if (Station != null) Station.SelectTapePost(post);
    }

    public void ToggleCamera()
    {
        if (EvidenceCamera == null || CameraAnchor == null) return;
        PutDownKit();
        ReturnFieldTool();
        if (HoldingCamera) EvidenceCamera.ReturnToRack();
        else if (Held == null) EvidenceCamera.HoldBy(CameraAnchor, this is HandInteractor ? new Vector3(-.045f,-.015f,.025f) : CameraHoldOffset);
    }

    // Quietly lets go of everything: before a reset, or when the holder is switched off.
    public virtual void ReleaseAll()
    {
        PutDownKit();
        ReturnFieldTool();
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

    // Compass heading of a freely turned transform: its forward flattened, or its up when it points straight down or up.
    public static float Heading(Transform t)
    {
        Vector3 f = Vector3.ProjectOnPlane(t.forward, Vector3.up);
        if (f.sqrMagnitude < 0.01f) f = Vector3.ProjectOnPlane(-t.up * Mathf.Sign(t.forward.y), Vector3.up);
        return f.sqrMagnitude < 1e-6f ? t.eulerAngles.y : Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
    }

    // Nearest point inside the interior, when bounds are assigned.
    protected Vector3 Confine(Vector3 point) => Bounds != null ? Bounds.Clamp(point) : point;

    protected static bool Raycast(Ray ray, float distance, out RaycastHit nearest, Transform ignore = null, Transform alsoIgnore = null)
        => DemoPhysics.Nearest(ray, distance, out nearest, ignore, alsoIgnore, false, QueryTriggerInteraction.Collide);
}
