using UnityEngine;

// Presenter panel on IMGUI, so it needs no packages. Tab hides it.
// This is the desktop/monitor view, not the headset HUD.
public sealed class DemoDesktopPanel : MonoBehaviour
{
    public ToolStation Station;
    public DemoSession Session;
    public EvidenceCamera EvidenceCamera;
    public DesktopInteractor Interactor;
    public bool Visible = true;
    private DeployedTool fallbackSelected;
    public DesktopToolPlacement Placement;
    private DeployedTool selected
    {
        get { return Placement!=null ? Placement.Selected : fallbackSelected; }
        set { fallbackSelected=value; if(Placement!=null) Placement.Select(value); }
    }
    private Vector2 scroll;
    private Vector2 panelScroll;

    private void OnGUI()
    {
        bool looking = Interactor != null && Interactor.Walker != null && Interactor.Walker.Looking;
        if (looking) GUI.Box(new Rect(Screen.width / 2f - 3, Screen.height / 2f - 3, 6, 6), GUIContent.none);
        PointerOverPanel = Visible && !looking && Area.Contains(Event.current.mousePosition);
        if (!Visible || Station == null) return;
        GUILayout.BeginArea(new Rect(12,12,300,Screen.height-24),GUI.skin.box);
        panelScroll=GUILayout.BeginScrollView(panelScroll);
        GUILayout.Label("Crime Scene Demo — desktop controls");
        if(GUILayout.Button("Spawn cone")) selected=Station.Spawn(DemoToolKind.Cone);
        if(GUILayout.Button("Spawn marker")) selected=Station.Spawn(DemoToolKind.Marker);
        if(GUILayout.Button("Spawn tape post")) selected=Station.Spawn(DemoToolKind.TapePost);
        if(Placement!=null)
        {
            GUILayout.Label(Placement.Status);
            if(selected!=null && GUILayout.Button("Place selected with mouse")) Placement.BeginPlacement(selected);
            if(Placement.IsPlacing && GUILayout.Button("Cancel placement")) Placement.CancelPlacement();
        }
        GUILayout.Label("Select a deployed tool:");
        scroll=GUILayout.BeginScrollView(scroll,GUILayout.Height(130));
        if(Station.DeploymentRoot!=null)
            foreach(DeployedTool tool in Station.DeploymentRoot.GetComponentsInChildren<DeployedTool>())
                if(GUILayout.Button(tool.name)) selected=tool;
        GUILayout.EndScrollView();

        GUILayout.BeginArea(Area, GUI.skin.box);
        GUILayout.Label("Crime Scene Demo — presenter controls");
        GUILayout.BeginHorizontal();
        SpawnButton("Cone", DemoToolKind.Cone);
        SpawnButton("Marker", DemoToolKind.Marker);
        SpawnButton("Tape post", DemoToolKind.TapePost);
        GUILayout.EndHorizontal();
        GUILayout.Label(Status());
        if (Station.PendingPost != null && GUILayout.Button("Cancel tape selection"))
            Station.CancelTapeSelection();
        if (EvidenceCamera != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Take photograph")) EvidenceCamera.CapturePhoto();
            if (GUILayout.Button("Open photo folder")) EvidenceCamera.OpenPhotoFolder();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Forward")) Move(Vector3.forward);
            if(GUILayout.Button("Back")) Move(Vector3.back);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Up")) Move(Vector3.up);
            if(GUILayout.Button("Down")) Move(Vector3.down);
            GUILayout.EndHorizontal();
            if(GUILayout.Button("Rotate 15 degrees"))
                selected.PlaceAt(selected.transform.position,selected.transform.eulerAngles.y+15);
            if(selected.Kind==DemoToolKind.TapePost && GUILayout.Button("Select post for tape"))
                Station.SelectTapePost(selected);
            if(GUILayout.Button("Remove selected"))
            {
                Station.RemoveTool(selected);
                selected=null;
            }
        }
        GUILayout.Label(Station.PendingPost!=null ? "Tape: choose second post" : "Tape: choose first post");
        if(GUILayout.Button("Cancel tape selection")) Station.CancelTapeSelection();
        if(EvidenceCamera!=null)
        {
            if(GUILayout.Button("Photograph current view (F)"))
            {
                if(Placement!=null) Placement.PhotographView();
                else EvidenceCamera.CapturePhoto();
            }
            if(GUILayout.Button("Open photo folder")) EvidenceCamera.OpenPhotoFolder();
            GUILayout.Label(EvidenceCamera.Status);
        }
        if(Session!=null && GUILayout.Button("Reset placed tools / scene"))
        {
            selected=null;
            if(Placement!=null) {Placement.CancelPlacement();Placement.Select(null);}
            Session.ResetSession();
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void SpawnButton(string label, DemoToolKind kind)
    {
        if (!GUILayout.Button(label)) return;
        if (Interactor != null) Interactor.SpawnIntoHand(kind);
        else Station.Spawn(kind);
    }

    private void Recount()
    {
        deployed = Station.DeploymentRoot != null
            ? Station.DeploymentRoot.GetComponentsInChildren<DeployedTool>().Length : 0;
    }

    private string Status()
    {
        string text = "Deployed tools: " + deployed + "   Next marker: " + Station.NextMarkerNumber;
        if (Interactor != null)
        {
            if (Interactor.Held != null) text += "\nHolding: " + Interactor.Held.name;
            else if (Interactor.Hovered != null) text += "\nPointing at: " + Interactor.Hovered.name;
            else if (Interactor.HoveringCamera) text += "\nPointing at: camera";
            if (Interactor.HoldingCamera) text += "\nCamera in hand";
        }
        if (Station.PendingPost != null) text += "\nTape: choose the second post (T)";
        return text;
    }
}
