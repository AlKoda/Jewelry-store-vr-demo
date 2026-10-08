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
    public bool PointerOverPanel { get; private set; }

    private static readonly Rect Area = new Rect(12, 12, 310, 430);
    private const string Guide =
        "WASD / arrows: walk (Shift: faster)\n" +
        "Hold right mouse: look\n" +
        "Left click: pick up / place a tool\n" +
        "1 / 2 / 3: new cone / marker / tape post\n" +
        "Q / E or wheel: rotate held tool\n" +
        "T: select tape post   X: cancel tape\n" +
        "Delete: remove tool under pointer\n" +
        "F or click camera: hold / return it\n" +
        "P: photograph   Home: return to start\n" +
        "Tab: hide this panel";

    private int deployed;

    private void OnEnable()
    {
        if (Station == null) return;
        Station.ToolsChanged.AddListener(Recount);
        Recount();
    }

    private void OnDisable()
    {
        if (Station != null) Station.ToolsChanged.RemoveListener(Recount);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) Visible = !Visible;
    }

    private void OnGUI()
    {
        bool looking = Interactor != null && Interactor.Walker != null && Interactor.Walker.Looking;
        if (looking) GUI.Box(new Rect(Screen.width / 2f - 3, Screen.height / 2f - 3, 6, 6), GUIContent.none);
        PointerOverPanel = Visible && !looking && Area.Contains(Event.current.mousePosition);
        if (!Visible || Station == null) return;

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
            GUILayout.Label(EvidenceCamera.Status);
        }
        if (Session != null && GUILayout.Button("Reset placed tools / scene")) Session.ResetSession();
        GUILayout.Space(8);
        GUILayout.Label(Guide);
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
