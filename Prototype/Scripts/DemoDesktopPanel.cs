using UnityEngine;

// One presenter layout, with one input adapter active at a time.
public sealed class DemoDesktopPanel : MonoBehaviour
{
    public ToolStation Station;
    public DemoSession Session;
    public EvidenceCamera EvidenceCamera;
    public DesktopInteractor Interactor;
    public DesktopToolPlacement Placement;
    public SessionReviewRecorder Review;
    public bool Visible=true;
    public bool UseCarryControls=false;
    private DeployedTool fallbackSelected;
    private Vector2 panelScroll,toolScroll;
    private int deployed;

    public Rect Area => new Rect(12,12,300,Mathf.Max(80,Screen.height-24));
    public bool PointerOverPanel
    {
        get
        {
            bool looking=Interactor!=null && Interactor.Walker!=null && Interactor.Walker.Looking;
            return Visible && !looking && Area.Contains(
                new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y));
        }
    }
    private DeployedTool Selected
    {
        get
        {
            if(UseCarryControls && Interactor!=null)
                return Interactor.Held!=null?Interactor.Held:Interactor.Hovered!=null?Interactor.Hovered:fallbackSelected;
            return Placement!=null?Placement.Selected:fallbackSelected;
        }
        set {fallbackSelected=value;if(Placement!=null)Placement.Select(value);}
    }

    private void OnEnable()
    {
        if(Station!=null) Station.ToolsChanged.AddListener(Recount);
        Recount();
        ApplyInputMode();
    }
    private void OnDisable()
    {
        if(Station!=null) Station.ToolsChanged.RemoveListener(Recount);
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab)) Visible=!Visible;
    }
    public void SetCarryControls(bool carry)
    {
        if(Interactor!=null) Interactor.ReleaseAll();
        if(Placement!=null) Placement.CancelPlacement();
        UseCarryControls=carry;
        ApplyInputMode();
    }
    private void ApplyInputMode()
    {
        if(Interactor!=null) Interactor.ReadDesktopInput=UseCarryControls;
        if(Placement!=null) Placement.ReadDesktopInput=!UseCarryControls;
    }
    private void Recount()
    {
        deployed=Station!=null && Station.DeploymentRoot!=null?
            Station.DeploymentRoot.GetComponentsInChildren<DeployedTool>().Length:0;
    }
    private void OnGUI()
    {
        if(!Visible || Station==null) return;
        GUILayout.BeginArea(Area,GUI.skin.box);
        panelScroll=GUILayout.BeginScrollView(panelScroll);
        GUILayout.Label("Crime Scene Demo — presenter controls");
        bool carry=GUILayout.Toggle(UseCarryControls,"Carry controls (1/2/3 spawn, click pick/place)");
        if(carry!=UseCarryControls) SetCarryControls(carry);
        GUILayout.BeginHorizontal();
        SpawnButton("Cone",DemoToolKind.Cone);
        SpawnButton("Marker",DemoToolKind.Marker);
        SpawnButton("Tape post",DemoToolKind.TapePost);
        GUILayout.EndHorizontal();
        GUILayout.Label("Deployed: "+deployed+"   Next marker: "+Station.NextMarkerNumber);
        if(Placement!=null && !UseCarryControls) GUILayout.Label(Placement.Status);
        GUILayout.Label("Select a deployed tool:");
        toolScroll=GUILayout.BeginScrollView(toolScroll,GUILayout.Height(110));
        if(Station.DeploymentRoot!=null)
            foreach(DeployedTool tool in Station.DeploymentRoot.GetComponentsInChildren<DeployedTool>())
                if(GUILayout.Button(tool.name)) Selected=tool;
        GUILayout.EndScrollView();
        DeployedTool selected=Selected;
        if(selected!=null)
        {
            GUILayout.Label("Selected: "+selected.name);
            if(!UseCarryControls && Placement!=null && GUILayout.Button("Place selected with mouse"))
                Placement.BeginPlacement(selected);
            if(UseCarryControls && Interactor!=null && GUILayout.Button("Pick up selected"))
                Interactor.Hold(selected);
            if(GUILayout.Button("Rotate 15 degrees"))
            {
                if(UseCarryControls && Interactor!=null && Interactor.Held==selected) Interactor.Rotate(15);
                else selected.PlaceAt(selected.transform.position,selected.transform.eulerAngles.y+15);
            }
            if(selected.Kind==DemoToolKind.TapePost && GUILayout.Button("Select post for tape"))
                Station.SelectTapePost(selected);
            if(GUILayout.Button("Remove selected"))
            {
                if(Interactor!=null) Interactor.Remove(selected); else Station.RemoveTool(selected);
                Selected=null;
            }
        }
        if(Placement!=null && Placement.IsPlacing && GUILayout.Button("Cancel placement")) Placement.CancelPlacement();
        GUILayout.Label(Station.PendingPost!=null?"Tape: select second post":"Tape: select first post");
        if(GUILayout.Button("Cancel tape selection")) Station.CancelTapeSelection();
        if(EvidenceCamera!=null)
        {
            if(GUILayout.Button("Photograph current view"))
            {
                if(Placement!=null) Placement.PhotographView();
                else EvidenceCamera.CapturePhoto();
            }
            if(Interactor!=null && GUILayout.Button(Interactor.HoldingCamera?"Return camera to rack":"Hold camera"))
                Interactor.ToggleCamera();
            if(GUILayout.Button("Open photo folder")) EvidenceCamera.OpenPhotoFolder();
            GUILayout.Label(EvidenceCamera.Status);
        }
        if(Review!=null)
        {
            if(GUILayout.Button("Save review snapshot")) Review.SaveSnapshot();
            if(GUILayout.Button("Open instructor review")) Review.OpenReview();
            GUILayout.Label(Review.Status);
        }
        if(Session!=null && GUILayout.Button("Reset scene / placed tools"))
        {
            if(Interactor!=null) Interactor.ReleaseAll();
            if(Placement!=null) {Placement.CancelPlacement();Placement.Select(null);}
            fallbackSelected=null;
            Session.ResetSession();
        }
        GUILayout.Label("WASD: walk | right mouse: look | Home: inside start | Tab: panel");
        GUILayout.Label(UseCarryControls?"F: hold/return camera | P: shutter | T: tape endpoint | X: cancel tape":
            "F: current-view photograph | Q/E: rotate placement | Escape: cancel");
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    private void SpawnButton(string label,DemoToolKind kind)
    {
        if(!GUILayout.Button(label)) return;
        Selected=UseCarryControls && Interactor!=null?Interactor.SpawnIntoHand(kind):Station.Spawn(kind);
    }
}
