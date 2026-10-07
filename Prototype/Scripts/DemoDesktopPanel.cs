using UnityEngine;

// Temporary desktop/debug panel. This is not the final headset HUD.
public sealed class DemoDesktopPanel : MonoBehaviour
{
    public ToolStation Station;
    public DemoSession Session;
    public EvidenceCamera EvidenceCamera;
    public bool Visible = true;
    private DeployedTool selected;
    private Vector2 scroll;

    private void OnGUI()
    {
        if (!Visible || Station == null) return;
        GUILayout.BeginArea(new Rect(12,12,300,Screen.height-24),GUI.skin.box);
        GUILayout.Label("Crime Scene Demo — desktop controls");
        if(GUILayout.Button("Spawn cone")) selected=Station.Spawn(DemoToolKind.Cone);
        if(GUILayout.Button("Spawn marker")) selected=Station.Spawn(DemoToolKind.Marker);
        if(GUILayout.Button("Spawn tape post")) selected=Station.Spawn(DemoToolKind.TapePost);
        GUILayout.Label("Select a deployed tool:");
        scroll=GUILayout.BeginScrollView(scroll,GUILayout.Height(130));
        if(Station.DeploymentRoot!=null)
            foreach(DeployedTool tool in Station.DeploymentRoot.GetComponentsInChildren<DeployedTool>())
                if(GUILayout.Button(tool.name)) selected=tool;
        GUILayout.EndScrollView();

        if(selected!=null)
        {
            GUILayout.Label("Selected: "+selected.name);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Left")) Move(Vector3.left);
            if(GUILayout.Button("Right")) Move(Vector3.right);
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
            if(GUILayout.Button("Take photograph")) EvidenceCamera.CapturePhoto();
            if(GUILayout.Button("Open photo folder")) EvidenceCamera.OpenPhotoFolder();
            GUILayout.Label(EvidenceCamera.Status);
        }
        if(Session!=null && GUILayout.Button("Reset placed tools / scene"))
        {
            selected=null;
            Session.ResetSession();
        }
        GUILayout.EndArea();
    }

    private void Move(Vector3 direction)
    {
        selected.PlaceAt(selected.transform.position+direction*0.1f,
            selected.transform.eulerAngles.y);
    }
}
