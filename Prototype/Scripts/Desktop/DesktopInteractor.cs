using UnityEngine;

// Desktop pointer adapter: the pointer is the mouse, or the screen centre while
// the right button locks the cursor for looking. Picks up, carries, rotates,
// places and removes tools, takes new ones from the rack samples, holds the
// camera, drops the instructor's beacon and reads hotkeys. Shared holding rules
// live in ToolHolder; XR hands use HandInteractor instead.
public sealed class DesktopInteractor : ToolHolder
{
    public Camera View;
    public ShopWalkController Walker;
    public DemoDesktopPanel Panel;
    public DemoLighting Lighting;
    public DemoBeacon Beacon;
    public float Reach = 4f;
    public float RotateStep = 15f;
    public bool ReadDesktopInput = true;

    public DeployedTool Hovered { get; private set; }
    public ToolRackSample HoveredSample { get; private set; }
    public bool HoveringCamera { get; private set; }
    // The surface the pointer last reached, whatever it was; the beacon key uses it.
    public bool PointerHit { get; private set; }
    public Vector3 PointedPoint { get; private set; }

    protected override Transform CameraAnchor => View != null ? View.transform : null;

    private float heldYaw;
    private bool preservePlaced, surfaceOffsetReady;
    private Vector3 surfaceOffset;
    private Quaternion placedOrientation;
    protected override bool PreserveReleasePose => preservePlaced;
    public bool HasPlacementTarget { get; private set; }
    private void OnGUI(){
        if(!ReadDesktopInput||!View||(Panel&&(!Panel.HudVisible||Panel.CapturesInput)))return;
        if(CarriedKit){DemoI18n.Label(new Rect(Screen.width*.25f,Screen.height-220,Screen.width*.5f,35),"Click: put kit on a clear floor or table   R: put down",GUI.skin.box);return;}
        if(!InvestigationInteraction.Target(PointerRay(),2,this,out var hit))return;
        string prompt=null;var clue=hit.collider.GetComponentInParent<EvidenceClue>();
        if(FieldTool&&FieldTool.Kind==FieldInvestigationTool.Method.Brush&&clue&&clue.GetComponent<DustablePrint>())prompt="Hold G / Click and sweep: develop ridge mark";
        else if(FieldTool&&clue&&clue.Available)prompt="G / Click: collect sample   R: return tool";
        else if(clue&&clue.Available)prompt="G / Click: inspect evidence";
        else if(hit.collider.GetComponentInParent<FieldInvestigationTool>())prompt="G / Click: take field tool";
        else if(hit.collider.GetComponentInParent<PortableFieldKit>()&&hit.collider.GetComponentInParent<PortableFieldKit>().GripCollider==hit.collider)prompt="G / Click: carry field kit";
        else if(hit.collider.GetComponentInParent<EvidenceContainer>())prompt="G / Click: open / close document box";
        else if(hit.collider.GetComponentInParent<EvidenceRegister>())prompt="G / Click: next register page";
        else if(hit.collider.GetComponentInParent<PushDoorLeaf>())prompt="G / Click: push the door open";
        if(prompt!=null&&FieldTool)prompt+="   V: how this tool works";
        if(prompt!=null)DemoI18n.Label(new Rect(Screen.width*.25f,Screen.height-220,Screen.width*.5f,35),prompt,GUI.skin.box);
    }

    private void Update()
    {
        if (!ReadDesktopInput || View == null || (Panel != null && Panel.CapturesInput)) return;
        Aim(PointerRay());

        bool overPanel = Panel != null && Panel.PointerOverPanel;
        if (Input.GetMouseButtonDown(0) && !overPanel)
        {
            if(CarriedKit)PlaceKit(PointerRay(),Reach);
            else if(FieldTool)InvestigationInteraction.Use(this,PointerRay(),2);
            else if (Held != null)
            {
                if (HasPlacementTarget) Place();
                else DemoFeedback.Post("Aim at the floor or a counter to place the " + DeployedTool.KindName(Held.Kind), FeedbackTone.Warning);
            }
            else if (Hovered != null) Hold(Hovered);
            else if (HoveredSample != null) TakeSample(HoveredSample);
            else if (HoveringCamera) ToggleCamera();
            else if (HoldingCamera) EvidenceCamera.CapturePhoto();
            else if(InvestigationInteraction.Use(this,PointerRay(),2)){}
            else if(ShoveDoor(PointerRay())){}
            else if (Lighting != null) { if (Lighting.UVTorchModel != null && Lighting.UVTorchModel.activeSelf) Lighting.ToggleUVTorch(); else if (Lighting.FlashlightModel != null && Lighting.FlashlightModel.activeSelf) Lighting.ToggleFlashlight(); }
        }
        // 1-6 on the main row or the number pad, in the hotbar's order.
        for (int i = 0; i < 6; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                SpawnIntoHand(DemoDesktopPanel.HotbarKind(i));
        if (Input.GetKeyDown(KeyCode.Q)) Rotate(-RotateStep);
        if (Input.GetKeyDown(KeyCode.E)) Rotate(RotateStep);
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (wheel != 0 && !overPanel) Rotate(Mathf.Sign(wheel) * RotateStep);
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) {if(CarriedKit)PutDownKit();else if(FieldTool)ReturnFieldTool();else Remove(Target);}
        if(Input.GetKeyDown(KeyCode.G)&&!overPanel&&!Held&&!HoldingCamera){if(CarriedKit)PlaceKit(PointerRay(),Reach);else if(!InvestigationInteraction.Use(this,PointerRay(),2))ShoveDoor(PointerRay());}
        if(Input.GetKeyDown(KeyCode.V)&&FieldTool&&!overPanel)FieldTool.ToggleHint();
        if (Input.GetKeyDown(KeyCode.T)) Use(Target);
        // + and - (main row or number pad) slide the hovered or held lamp's telescoping column by 10 cm.
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) AdjustLampHeight(Target, LampHeightStep);
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) AdjustLampHeight(Target, -LampHeightStep);
        if (Input.GetKeyDown(KeyCode.X) && Station != null) Station.CancelTapeSelection();
        if (Input.GetKeyDown(KeyCode.F)) ToggleCamera();
        if (Input.GetKeyDown(KeyCode.P)) Photograph();
        if (Input.GetKeyDown(KeyCode.L) && Lighting != null) Lighting.ToggleFlashlight();
        if (Input.GetKeyDown(KeyCode.U) && Lighting != null) Lighting.ToggleUVTorch();
        if (Input.GetKeyDown(KeyCode.N) && Lighting != null) Lighting.ToggleNight();
        if (Input.GetKeyDown(KeyCode.B) && Beacon != null && PointerHit) Beacon.Place(PointedPoint);
    }

    public void Aim(Ray ray)
    {
        if (Held != null)
        {
            Hovered = null;
            HoveredSample = null;
            HoveringCamera = false;
            Carry(ray);
        }
        else
        {
            bool hit = Point(ray, null, out RaycastHit nearest);
            Hovered = hit ? nearest.collider.GetComponentInParent<DeployedTool>() : null;
            if (Hovered != null && !Hovered.Free) Hovered = null;
            HoveredSample = hit && Hovered == null ? nearest.collider.GetComponentInParent<ToolRackSample>() : null;
            HoveringCamera = hit && Hovered == null && HoveredSample == null && EvidenceCamera != null &&
                nearest.collider.GetComponentInParent<EvidenceCamera>() == EvidenceCamera;
        }
        Highlight(Held != null ? Held : Hovered);

    }

    public DeployedTool Target => Held != null ? Held : Hovered;

    public const float LampHeightStep = 0.1f;
    // Slides a scene lamp's head up or down (DeployedTool.SetLampHeight clamps the column's travel).
    public bool AdjustLampHeight(DeployedTool lamp, float delta)
    {
        if (lamp == null || !lamp.IsLamp || !lamp.LampTelescopes) return false;
        float height = lamp.SetLampHeight(lamp.LampHeight + delta);
        DemoFeedback.Post("Lamp head at " + height.ToString("0.00") + " m");
        return true;
    }

    // T on a scene lamp switches it; on a tape post or reel it selects the endpoint.
    public void Use(DeployedTool tool)
    {
        if (tool != null && tool.Lamp != null && Station != null) Station.ToggleLamp(tool);
        else SelectTapePost(tool);
    }

    // Through the held camera's lens, or of the current view when it is on its rack.
    public void Photograph()
    {
        if (EvidenceCamera == null) return;
        if (HoldingCamera || View == null) EvidenceCamera.CapturePhoto();
        else EvidenceCamera.CaptureFromView(View.transform);
    }

    // A pointed door leaf within reach swings open away from the pointer (a shove, not a nudge).
    public bool ShoveDoor(Ray ray)
    {
        if (!Raycast(ray, Reach, out RaycastHit hit, null, Walker != null ? Walker.transform : null)) return false;
        var door = hit.collider.GetComponentInParent<PushDoorLeaf>(); if (door == null) return false;
        door.Shove(hit.point, ray.direction); return true;
    }

    public Ray PointerRay()
    {
        bool centred = Walker != null && Walker.Looking;
        Vector3 point = centred ? new Vector3(Screen.width / 2f, Screen.height / 2f) : Input.mousePosition;
        return View.ScreenPointToRay(point);
    }

    public override void Hold(DeployedTool tool)
    {
        if(tool==null||tool==Held||HoldingCamera||!tool.Free)return;
        bool keepPose=tool.HasBeenPlaced;
        base.Hold(tool);
        if (Held == null) return;
        preservePlaced=keepPose;surfaceOffsetReady=false;placedOrientation=Held.transform.rotation;
        if(preservePlaced&&View!=null&&Point(PointerRay(),Held.transform,out var support)&&support.normal.y>=.7f){surfaceOffset=Held.transform.position-support.point;surfaceOffsetReady=true;}
        Hovered = null;
        Highlight(Held);
        heldYaw = Held.transform.eulerAngles.y;
        HasPlacementTarget = false;
        // A tool taken from the full panel waits at the rack until the sidebar closes.
        bool panelOpen = Panel != null && Panel.CapturesInput;
        if (!preservePlaced && ReadDesktopInput && View != null && !panelOpen) Carry(PointerRay());
    }

    // Move the held tool to the pointed surface. Only upward-facing surfaces
    // inside the interior count; otherwise it stays where it was.
    public void Carry(Ray ray)
    {
        HasPlacementTarget = false;
        if (Held == null || !Point(ray, Held.transform, out RaycastHit hit) || hit.normal.y < 0.7f) return;
        if(preservePlaced){
            if(!surfaceOffsetReady){surfaceOffset=Held.transform.position-hit.point;surfaceOffsetReady=true;}
            Held.transform.SetPositionAndRotation(Confine(hit.point+surfaceOffset),placedOrientation);
        }else Held.PlaceAt(Confine(hit.point), heldYaw);
        HasPlacementTarget = true;
    }

    // The one pointer raycast: nearest surface within reach, skipping the rig and
    // the given tool, remembered for the beacon key.
    private bool Point(Ray ray, Transform ignore, out RaycastHit hit)
    {
        PointerHit = Raycast(ray, Reach, out hit, ignore, Walker != null ? Walker.transform : null);
        PointedPoint = PointerHit ? hit.point : ray.origin;
        return PointerHit;
    }

    public override void Place()
    {
        if (!HasPlacementTarget) return;
        base.Place();
        HasPlacementTarget = false;
    }

    public void Rotate(float degrees)
    {
        if (Held == null) return;
        heldYaw += degrees;
        if(preservePlaced){placedOrientation=Quaternion.AngleAxis(degrees,Vector3.up)*placedOrientation;Held.transform.rotation=placedOrientation;}
        else Held.PlaceAt(Held.transform.position, heldYaw);
    }

    public override void Remove(DeployedTool tool)
    {
        if (tool != null && tool == Hovered) Hovered = null;
        base.Remove(tool);
    }

    public override void ReleaseAll()
    {
        base.ReleaseAll();
        Hovered = null;
        HoveredSample = null;
        HoveringCamera = false;
        PointerHit = false;
    }
}
