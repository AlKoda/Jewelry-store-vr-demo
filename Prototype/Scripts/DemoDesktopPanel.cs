using UnityEngine;

public enum PanelLayout { Hidden, Strip, Full }

// Presenter panel on IMGUI, so it needs no packages. It is the projector view the
// audience watches while one visitor uses the headset or the mouse: a slim status
// strip along the top during the demo, a full sidebar with tools, photos, map
// and controls between scenes, or nothing but a hint bar. Everything is drawn
// through a scaled GUI.matrix so it stays readable from the back of the room.
// Not a headset HUD.
[DefaultExecutionOrder(-200)]
public sealed class DemoDesktopPanel : MonoBehaviour
{
    public ToolStation Station;
    public DemoSession Session;
    public EvidenceCamera EvidenceCamera;
    public DesktopInteractor Interactor;
    public SessionReviewRecorder Review;
    public DemoOverviewMap Map;
    public CrimeSceneState SceneState;
    public Transform Player;

    public PanelLayout Layout { get; set; } = PanelLayout.Hidden;
    // Older callers only know open or closed; open means the full sidebar.
    public bool Visible
    {
        get => Layout != PanelLayout.Hidden;
        set => Layout = value ? PanelLayout.Full : PanelLayout.Hidden;
    }
    // Only the full sidebar pauses walking and the tool hotkeys; the strip leaves
    // the visitor's mouse and keyboard alone and merely swallows clicks on itself.
    public bool CapturesInput => Layout == PanelLayout.Full;

    // Everything drawn is multiplied by this (1 to 2); remembered between runs.
    public float Scale
    {
        get => scale;
        set
        {
            scale = Mathf.Clamp(value, 1, 2);
            PlayerPrefs.SetFloat(ScaleKey, scale);
        }
    }

    // Evaluated on demand so the interactor's Update sees this frame's pointer.
    public bool PointerOverPanel => Contains(Input.mousePosition);

    // Hit test in screen pixels (origin bottom left, as Input.mousePosition),
    // against the active layout's rectangle in scaled GUI space.
    public bool Contains(Vector2 screenPoint)
    {
        if (!isActiveAndEnabled || Layout == PanelLayout.Hidden || Station == null || Looking) return false;
        return Area.Contains(new Vector2(screenPoint.x, Screen.height - screenPoint.y) / scale);
    }

    public void CycleLayout() { Layout = (PanelLayout)(((int)Layout + 1) % 3); }

    private bool Looking => Interactor != null && Interactor.Walker != null && Interactor.Walker.Looking;

    // Layout rectangles in GUI units (screen pixels divided by Scale).
    private float GuiWidth => Screen.width / scale;
    private float GuiHeight => Screen.height / scale;
    private Rect Area => Layout == PanelLayout.Strip
        ? new Rect(0, 0, GuiWidth, StripHeight)
        : new Rect(12, 12, Width, GuiHeight - 24);

    private const float Width = 330;
    private const float StripHeight = 56;
    private const int RecentTools = 8;
    private const string ScaleKey = "DemoPanelScale";
    private const string DesktopGuide =
        "WASD / arrows: walk (Shift: faster)   Right click: toggle mouse look; Esc: release\n" +
        "Left click: pick up / place a tool, or take one from the rack\n" +
        "1-5: new cone / marker / tape post / scale / measuring tape   Q / E, wheel: rotate\n" +
        "T: select tape post or reel   X: cancel tape   R / Delete: remove\n" +
        "F or click the camera: hold / return it   P: photograph\n" +
        "I: intact / robbed store   Home: return to start\n" +
        "F9: VR mode   Tab: hidden / strip / full panel   [ ]: panel size";
    private const string VRGuide =
        "Grip: hold tool / rack sample / camera   Trigger: photo, tape post or reel\n" +
        "A / X: new tool   B / Y: remove or next kind\n" +
        "Left stick: teleport   Right stick: snap turn   F9: desktop mode\n" +
        "Tab: hidden / strip / full panel   [ ]: panel size";
    private const string Credits =
        "Models: Kenney, KayKit (CC0); Khronos glTF samples (CC0 / CC BY 4.0); textures cgbookcase, HDRIs HDRI Haven (CC0). See ThirdParty licenses.";
    private static readonly int KindCount = System.Enum.GetValues(typeof(DemoToolKind)).Length;

    private float scale = 1.25f;
    private int deployed;
    private float resetArmedUntil;
    private bool showControls;
    private Vector2 scroll;
    private GUIStyle panel, header, body, button, stripLabel, liveTag;
    private Texture2D background, dot, cameraDot, border, tagBackground;
    private readonly Texture2D[] kindDots = new Texture2D[KindCount];
    private readonly int[] kindCounts = new int[KindCount];
    private GUILayoutOption tall, narrow;

    private void Awake() { scale = Mathf.Clamp(PlayerPrefs.GetFloat(ScaleKey, 1.25f), 1, 2); }

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
        if (Input.GetKeyDown(KeyCode.Tab)) CycleLayout();
        if (Input.GetKeyDown(KeyCode.LeftBracket)) Scale -= 0.25f;
        if (Input.GetKeyDown(KeyCode.RightBracket)) Scale += 0.25f;
    }

    private void OnGUI()
    {
        if (panel == null) BuildStyles();
        if (Looking) GUI.Box(new Rect(Screen.width / 2f - 3, Screen.height / 2f - 3, 6, 6), GUIContent.none);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
        bool vr = Interactor != null && !Interactor.isActiveAndEnabled;
        if (Layout == PanelLayout.Hidden || Station == null) DrawHidden();
        else if (Layout == PanelLayout.Strip) DrawStrip(vr);
        else DrawFull(vr);
        GUI.matrix = Matrix4x4.identity;
    }

    // Hint bar for the desktop visitor and, while the camera is carried, the live
    // view in the corner so shots can be framed with the panel out of the way.
    private void DrawHidden()
    {
        if (Interactor != null && Interactor.isActiveAndEnabled)
        {
            string hint = "WASD: walk   Right click: mouse look   Tab: tools / settings   1-5: tools   P: photo";
            if (Interactor.Hovered != null) hint = Interactor.Hovered.name + " — Click: move   R / Delete: remove";
            if (Interactor.Held != null) hint = Interactor.HasPlacementTarget
                ? "Left click: place   Q/E or wheel: rotate   R: remove   Tab: tools"
                : "Aim at a nearby floor or counter to place the held tool";
            GUI.Box(new Rect(12, GuiHeight - 42, Mathf.Min(760, GuiWidth - 24), 30), hint);
        }
        if (EvidenceCamera != null && EvidenceCamera.Viewfinder != null)
            DrawViewfinder(new Rect(GuiWidth - 332, GuiHeight - 234, 320, 180));
    }

    private void DrawStrip(bool vr)
    {
        Rect area = Area;
        GUILayout.BeginArea(area, panel);
        GUILayout.BeginHorizontal(tall);
        GUILayout.Label(vr ? "VR" : "DESKTOP", header, narrow, tall);
        string status = "Tools: " + deployed + "   Next marker: " + Station.NextMarkerNumber;
        if (Station.PendingPrompt != null) status += "   " + Station.PendingPrompt + " (T)";
        GUILayout.Label(status, stripLabel, narrow, tall);
        if (EvidenceCamera != null)
        {
            GUILayout.Label(CameraStatus(), stripLabel, narrow, tall);
            if (EvidenceCamera.LastPhoto != null)
                GUI.DrawTexture(GUILayoutUtility.GetRect(64, 36, tall), EvidenceCamera.LastPhoto, ScaleMode.ScaleToFit);
        }
        GUILayout.FlexibleSpace();
        if (EvidenceCamera != null && GUILayout.Button("Photo", button, narrow, tall)) Photograph();
        if (SceneState != null && GUILayout.Button(SceneState.Intact ? "Show robbed" : "Show intact", button, narrow, tall))
            SceneState.Toggle();
        ResetButton(true, narrow, tall);
        if (GUILayout.Button("Full panel", button, narrow, tall)) Layout = PanelLayout.Full;
        // Reserved for the live view drawn below, at the strip's right end.
        if (EvidenceCamera != null && EvidenceCamera.Viewfinder != null) GUILayout.Space(84);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        if (EvidenceCamera != null && EvidenceCamera.Viewfinder != null)
            DrawViewfinder(new Rect(area.width - 80, 8, 72, 40));
    }

    private void DrawFull(bool vr)
    {
        GUILayout.BeginArea(Area, panel);
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label("CRIME SCENE DEMO  —  " + (vr ? "VR mode" : "desktop mode"), header);
        GUILayout.Label(Status(), body);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Compact strip", button)) Layout = PanelLayout.Strip;
        if (GUILayout.Button("Hide panel (Tab)", button)) Layout = PanelLayout.Hidden;
        GUILayout.EndHorizontal();
        DeployedTool[] tools = Station.DeploymentRoot.GetComponentsInChildren<DeployedTool>();

        Section("TOOLS");
        GUILayout.BeginHorizontal();
        SpawnButton("Cone", DemoToolKind.Cone);
        SpawnButton("Marker", DemoToolKind.Marker);
        SpawnButton("Tape post", DemoToolKind.TapePost);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        SpawnButton("Scale", DemoToolKind.Scale);
        SpawnButton("Measure", DemoToolKind.Measure);
        GUILayout.EndHorizontal();
        Station.AutoConnectTape = GUILayout.Toggle(Station.AutoConnectTape, "Connect new tape posts and reels automatically");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start a separate tape run", button)) Station.StartNewTapeRun();
        if (Station.PendingPost != null && GUILayout.Button("Cancel tape selection (X)", button)) Station.CancelTapeSelection();
        GUILayout.EndHorizontal();

        Section("PLACED TOOLS");
        PlacedTools(tools);

        if (EvidenceCamera != null)
        {
            Section("PHOTOGRAPHS");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Take photograph (P)", button)) Photograph();
            if (GUILayout.Button("Open photo folder", button)) EvidenceCamera.OpenPhotoFolder();
            GUILayout.EndHorizontal();
            if (Interactor != null && Interactor.isActiveAndEnabled
                && GUILayout.Button(Interactor.HoldingCamera ? "Return camera to rack (F)" : "Hold camera (F)", button))
                Interactor.ToggleCamera();
            GUILayout.Label(CameraStatus(), body);
            if (EvidenceCamera.Viewfinder != null)
            {
                Rect live = GUILayoutUtility.GetRect(Width - 24, (Width - 24) * 9 / 16 + 4);
                DrawViewfinder(new Rect(live.x + 2, live.y + 2, live.width - 4, live.height - 4));
            }
            if (EvidenceCamera.LastPhoto != null)
                GUI.DrawTexture(GUILayoutUtility.GetRect(Width - 24, (Width - 24) * 9 / 16), EvidenceCamera.LastPhoto, ScaleMode.ScaleToFit);
        }
        if (Review != null)
        {
            Section("INSTRUCTOR REVIEW");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save snapshot", button)) Review.SaveSnapshot();
            if (GUILayout.Button("Open review", button)) Review.OpenReview();
            GUILayout.EndHorizontal();
            GUILayout.Label(Review.Status, body);
        }
        if (Map != null && Map.Texture != null)
        {
            Section("OVERVIEW");
            Rect mapRect = GUILayoutUtility.GetRect(Width - 24, Width - 24);
            GUI.DrawTexture(mapRect, Map.Texture, ScaleMode.ScaleToFit);
            foreach (DeployedTool tool in tools) DrawGlyph(mapRect, Map.ToMap(tool.transform.position), kindDots[(int)tool.Kind], 6);
            if (EvidenceCamera != null && EvidenceCamera.Holder != null)
                DrawGlyph(mapRect, Map.ToMap(EvidenceCamera.transform.position), cameraDot, 5);
            if (Player != null) DrawGlyph(mapRect, Map.ToMap(Player.position), dot, 8);
            GUILayout.Label("Green: visitor.  Orange cones, yellow markers, red tape posts, white scales, violet reels, cyan camera.", body);
        }

        Section("SESSION");
        if (SceneState != null && GUILayout.Button(SceneState.Intact ? "Show robbed store (I)" : "Show intact store (I)", button))
            SceneState.Toggle();
        ResetButton(false);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Panel size x" + scale.ToString("0.00") + "  ([ ])", body);
        if (GUILayout.Button("-", button, GUILayout.Width(36))) Scale -= 0.25f;
        if (GUILayout.Button("+", button, GUILayout.Width(36))) Scale += 0.25f;
        GUILayout.EndHorizontal();

        Section("CONTROLS");
        showControls = GUILayout.Toggle(showControls, "Show controls");
        if (showControls) GUILayout.Label(vr ? VRGuide : DesktopGuide, body);
        GUILayout.Space(8);
        GUILayout.Label(Credits, body);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void Section(string title)
    {
        GUILayout.Space(8);
        GUILayout.Label(title, header);
    }

    // Counts per kind on one line, then only the most recent few with their
    // Remove buttons, so a busy scene does not push the rest of the panel away.
    private void PlacedTools(DeployedTool[] tools)
    {
        if (tools.Length == 0) { GUILayout.Label("None yet.", body); return; }
        for (int i = 0; i < kindCounts.Length; i++) kindCounts[i] = 0;
        foreach (DeployedTool tool in tools) kindCounts[(int)tool.Kind]++;
        string summary = "";
        for (int i = 0; i < kindCounts.Length; i++)
        {
            if (kindCounts[i] == 0) continue;
            summary += (summary.Length > 0 ? ", " : "") + kindCounts[i] + " " + KindName((DemoToolKind)i, kindCounts[i]);
        }
        GUILayout.Label(summary, body);
        int first = Mathf.Max(0, tools.Length - RecentTools);
        if (first > 0) GUILayout.Label("+" + first + " more", body);
        for (int i = first; i < tools.Length; i++)
        {
            DeployedTool tool = tools[i];
            GUILayout.BeginHorizontal();
            GUILayout.Label(tool.name, body);
            if (GUILayout.Button("Remove", button, GUILayout.Width(80)))
            {
                if (tool.Holder != null) tool.Holder.Remove(tool);
                else if (Interactor != null) Interactor.Remove(tool);
                else Station.RemoveTool(tool);
            }
            GUILayout.EndHorizontal();
        }
    }

    private static string KindName(DemoToolKind kind, int count)
    {
        string name = DeployedTool.KindName(kind);
        return count == 1 ? name : name + "s";
    }

    // Map glyph centred on a 0..1 map coordinate; the texture's v axis points up (+z).
    private void DrawGlyph(Rect mapRect, Vector2 uv, Texture2D glyph, float size)
    {
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return;
        float x = mapRect.x + uv.x * mapRect.width;
        float y = mapRect.y + (1 - uv.y) * mapRect.height;
        GUI.DrawTexture(new Rect(x - size / 2, y - size / 2, size, size), glyph);
    }

    // 16:9 live view with a 2 px frame and a LIVE tag.
    private void DrawViewfinder(Rect rect)
    {
        GUI.DrawTexture(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), border);
        GUI.DrawTexture(rect, EvidenceCamera.Viewfinder, ScaleMode.StretchToFill);
        GUI.Label(new Rect(rect.x + 3, rect.y + 3, 34, 16), "LIVE", liveTag);
    }

    private void ResetButton(bool compact, params GUILayoutOption[] options)
    {
        if (Session == null) return;
        string label = ResetArmed ? "Click again to confirm reset" : compact ? "Reset" : "Reset placed tools / scene";
        if (GUILayout.Button(label, button, options)) RequestReset();
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

    // Desktop: through the interactor (held lens or view). VR: the held lens, else the headset view.
    public void Photograph()
    {
        if (EvidenceCamera == null) return;
        if (Interactor != null && Interactor.isActiveAndEnabled) Interactor.Photograph();
        else if (EvidenceCamera.Holder != null || Camera.main == null) EvidenceCamera.CapturePhoto();
        else EvidenceCamera.CaptureFromView(Camera.main.transform);
    }

    private void Recount() { deployed = Station.DeployedCount; }

    private string CameraStatus()
    {
        string text = EvidenceCamera.Status + "   Photos: " + EvidenceCamera.PhotoCount;
        if (EvidenceCamera.Holder != null) text += "   Camera in hand";
        return text;
    }

    private string Status()
    {
        string text = "Deployed tools: " + deployed + "   Next marker: " + Station.NextMarkerNumber;
        if (Interactor != null && Interactor.isActiveAndEnabled)
        {
            if (Interactor.Held != null) text += "\nHolding: " + Interactor.Held.name;
            else if (Interactor.Hovered != null) text += "\nPointing at: " + Interactor.Hovered.name;
            else if (Interactor.HoveredSample != null) text += "\nPointing at: rack (" + Interactor.HoveredSample.Kind + ")";
            else if (Interactor.HoveringCamera) text += "\nPointing at: camera";
        }
        if (Station.PendingPrompt != null) text += "\n" + Station.PendingPrompt + " (T)";
        return text;
    }

    private static Color KindColor(DemoToolKind kind)
    {
        switch (kind)
        {
            case DemoToolKind.Cone: return new Color(1f, 0.55f, 0.1f);
            case DemoToolKind.Marker: return new Color(1f, 0.9f, 0.2f);
            case DemoToolKind.TapePost: return new Color(1f, 0.25f, 0.2f);
            case DemoToolKind.Scale: return Color.white;
            default: return new Color(0.75f, 0.55f, 1f);
        }
    }

    private void BuildStyles()
    {
        background = Solid(new Color(0.08f, 0.09f, 0.11f, 0.88f));
        dot = Solid(new Color(0.4f, 1f, 0.45f));
        cameraDot = Solid(Color.cyan);
        border = Solid(new Color(0.9f, 0.9f, 0.9f));
        tagBackground = Solid(new Color(0.75f, 0.1f, 0.1f, 0.9f));
        for (int i = 0; i < kindDots.Length; i++) kindDots[i] = Solid(KindColor((DemoToolKind)i));
        panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 8, 8) };
        panel.normal.background = background;
        header = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, margin = new RectOffset(0, 8, 4, 2), alignment = TextAnchor.MiddleLeft };
        header.normal.textColor = new Color(1f, 0.85f, 0.45f);
        body = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        body.normal.textColor = Color.white;
        stripLabel = new GUIStyle(body) { fontSize = 14, wordWrap = false, alignment = TextAnchor.MiddleLeft, margin = new RectOffset(0, 10, 0, 0) };
        button = new GUIStyle(GUI.skin.button) { fontSize = 13, padding = new RectOffset(8, 8, 6, 6) };
        liveTag = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        liveTag.normal.textColor = Color.white;
        liveTag.normal.background = tagBackground;
        tall = GUILayout.ExpandHeight(true);
        narrow = GUILayout.ExpandWidth(false);
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
        if (cameraDot != null) Destroy(cameraDot);
        if (border != null) Destroy(border);
        if (tagBackground != null) Destroy(tagBackground);
        foreach (Texture2D glyph in kindDots) if (glyph != null) Destroy(glyph);
    }
}
