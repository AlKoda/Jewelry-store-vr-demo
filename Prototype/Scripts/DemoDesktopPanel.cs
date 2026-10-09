using UnityEngine;

// Presenter panel on IMGUI, so it needs no packages: status, tool buttons,
// photo and review buttons, a top-down map and the controls guide. It is the
// monitor view in both desktop and VR modes; Tab hides it. Not a headset HUD.
public sealed class DemoDesktopPanel : MonoBehaviour
{
    public ToolStation Station;
    public DemoSession Session;
    public EvidenceCamera EvidenceCamera;
    public DesktopInteractor Interactor;
    public SessionReviewRecorder Review;
    public DemoOverviewMap Map;
    public PhotoFrame Frame;
    public Transform Player;
    public bool Visible = true;

    // Evaluated on demand so the interactor's Update sees this frame's pointer.
    public bool PointerOverPanel
    {
        get
        {
            if (!Visible || Station == null || (Interactor != null && Interactor.Walker != null && Interactor.Walker.Looking)) return false;
            return Area.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        }
    }

    private static Rect Area => new Rect(12, 12, Width, Screen.height - 24);

    private const float Width = 330;
    private const string DesktopGuide =
        "WASD / arrows: walk (Shift: faster)   Hold right mouse: look\n" +
        "Left click: pick up / place a tool, or take one from the rack\n" +
        "1 / 2 / 3: new cone / marker / tape post   Q / E, wheel: rotate\n" +
        "T: select tape post   X: cancel tape   Delete: remove\n" +
        "F or click the camera: hold / return it   P: photograph\n" +
        "Home: return to start   F9: VR mode   Tab: hide this panel";
    private const string VRGuide =
        "Grip: hold tool / rack sample / camera   Trigger: photo, tape post\n" +
        "A / X: new tool   B / Y: remove or next kind\n" +
        "Left stick: teleport   Right stick: snap turn   F9: desktop mode";

    private int deployed;
    private float resetArmedUntil;
    private Vector2 scroll;
    private GUIStyle panel, header, body, button;
    private Texture2D background, dot;

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
        if (panel == null) BuildStyles();
        bool looking = Interactor != null && Interactor.Walker != null && Interactor.Walker.Looking;
        if (looking) GUI.Box(new Rect(Screen.width / 2f - 3, Screen.height / 2f - 3, 6, 6), GUIContent.none);
        if (!Visible || Station == null) return;

        GUILayout.BeginArea(Area, panel);
        scroll = GUILayout.BeginScrollView(scroll);
        bool vr = Interactor != null && !Interactor.isActiveAndEnabled;
        GUILayout.Label("CRIME SCENE DEMO  —  " + (vr ? "VR mode" : "desktop mode"), header);
        GUILayout.Label(Status(), body);

        GUILayout.Label("TOOLS", header);
        GUILayout.BeginHorizontal();
        SpawnButton("Cone", DemoToolKind.Cone);
        SpawnButton("Marker", DemoToolKind.Marker);
        SpawnButton("Tape post", DemoToolKind.TapePost);
        GUILayout.EndHorizontal();
        if (Station.PendingPost != null && GUILayout.Button("Cancel tape selection", button))
            Station.CancelTapeSelection();

        if (EvidenceCamera != null)
        {
            GUILayout.Label("PHOTOGRAPHS", header);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Take photograph (P)", button))
            {
                if (Interactor != null && Interactor.isActiveAndEnabled) Interactor.Photograph();
                else EvidenceCamera.CapturePhoto();
            }
            if (GUILayout.Button("Open photo folder", button)) EvidenceCamera.OpenPhotoFolder();
            GUILayout.EndHorizontal();
            if (Interactor != null && Interactor.isActiveAndEnabled
                && GUILayout.Button(Interactor.HoldingCamera ? "Return camera to rack (F)" : "Hold camera (F)", button))
                Interactor.ToggleCamera();
            GUILayout.Label(EvidenceCamera.Status, body);
            if (Frame != null && Frame.Current != null)
                GUI.DrawTexture(GUILayoutUtility.GetRect(Width - 24, (Width - 24) * 9 / 16), Frame.Current, ScaleMode.ScaleToFit);
        }
        if (Review != null)
        {
            GUILayout.Label("INSTRUCTOR REVIEW", header);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save snapshot", button)) Review.SaveSnapshot();
            if (GUILayout.Button("Open review", button)) Review.OpenReview();
            GUILayout.EndHorizontal();
            GUILayout.Label(Review.Status, body);
        }
        if (Map != null && Map.Texture != null)
        {
            GUILayout.Label("OVERVIEW", header);
            Rect mapRect = GUILayoutUtility.GetRect(Width - 24, Width - 24);
            GUI.DrawTexture(mapRect, Map.Texture, ScaleMode.ScaleToFit);
            if (Player != null) DrawDot(mapRect, Map.ToMap(Player.position));
        }
        GUILayout.Label("SESSION", header);
        ResetButton();
        GUILayout.Label("CONTROLS", header);
        GUILayout.Label(vr ? VRGuide : DesktopGuide, body);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // Player position as a dot on the map; the texture's v axis points up (+z).
    private void DrawDot(Rect mapRect, Vector2 uv)
    {
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return;
        float x = mapRect.x + uv.x * mapRect.width;
        float y = mapRect.y + (1 - uv.y) * mapRect.height;
        GUI.DrawTexture(new Rect(x - 4, y - 4, 8, 8), dot);
    }

    private void ResetButton()
    {
        if (Session == null) return;
        if (GUILayout.Button(ResetArmed ? "Click again to confirm reset" : "Reset placed tools / scene", button)) RequestReset();
    }

    public bool ResetArmed => Time.unscaledTime < resetArmedUntil;

    // Two requests within a few seconds, so a stray click cannot wipe the scene mid-demo.
    public bool RequestReset()
    {
        if (Session == null) return false;
        if (!ResetArmed) { resetArmedUntil = Time.unscaledTime + 4f; return false; }
        resetArmedUntil = 0;
        Session.ResetSession();
        return true;
    }

    private void SpawnButton(string label, DemoToolKind kind)
    {
        if (!GUILayout.Button(label, button)) return;
        if (Interactor != null && Interactor.isActiveAndEnabled) Interactor.SpawnIntoHand(kind);
        else Station.Spawn(kind);
    }

    private void Recount() { deployed = Station.DeployedCount; }

    private string Status()
    {
        string text = "Deployed tools: " + deployed + "   Next marker: " + Station.NextMarkerNumber;
        if (Interactor != null && Interactor.isActiveAndEnabled)
        {
            if (Interactor.Held != null) text += "\nHolding: " + Interactor.Held.name;
            else if (Interactor.Hovered != null) text += "\nPointing at: " + Interactor.Hovered.name;
            else if (Interactor.HoveredSample != null) text += "\nPointing at: rack (" + Interactor.HoveredSample.Kind + ")";
            else if (Interactor.HoveringCamera) text += "\nPointing at: camera";
            if (Interactor.HoldingCamera) text += "\nCamera in hand";
        }
        if (Station.PendingPost != null) text += "\nTape: choose the second post (T)";
        return text;
    }

    private void BuildStyles()
    {
        background = Solid(new Color(0.08f, 0.09f, 0.11f, 0.88f));
        dot = Solid(new Color(1f, 0.85f, 0.2f));
        panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 10, 10) };
        panel.normal.background = background;
        header = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, margin = new RectOffset(0, 0, 10, 2) };
        header.normal.textColor = new Color(1f, 0.85f, 0.45f);
        body = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        body.normal.textColor = Color.white;
        button = new GUIStyle(GUI.skin.button) { fontSize = 13, padding = new RectOffset(8, 8, 6, 6) };
    }

    private static Texture2D Solid(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnDestroy()
    {
        if (background != null) Destroy(background);
        if (dot != null) Destroy(dot);
    }
}
