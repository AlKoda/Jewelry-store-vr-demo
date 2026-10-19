using System;
using System.Collections.Generic;
using UnityEngine;

public enum PanelLayout { Hidden, Strip, Full }
public enum PanelTab { Tools, Camera, Map, Scene, Settings }

// The monitor's interface, on IMGUI so it needs no packages, drawn through a scaled
// GUI.matrix so it reads from the back of a room. Three layers, in the DemoUi look
// shared with the start menu and the VR controller hints:
//  - the HUD: status chips, toasts (DemoFeedback) and the camera's live view; for the
//    mouse-and-keyboard visitor also a clickable tool hotbar, a crosshair while mouse
//    look is on, a tooltip on what the pointer is over and key prompts for the moment.
//    In VR mode the monitor is the audience's view, so only chips, toasts and the live view;
//  - the presenter layouts Tab cycles: nothing more, a status strip along the top, or a
//    drawer on the right with Tools, Camera, Map, Scene and Settings tabs;
//  - the controls overlay (F1 or H). F2 hides the HUD for a clean view (not saved: each map load starts with it on).
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
    public DemoLighting Lighting;
    public DemoBeacon Beacon;
    public Transform Player;

    public PanelLayout Layout { get => layout; set { layout = value; SyncLayoutFlag(); } }
    // Older callers only know open or closed; open means the full drawer.
    public bool Visible
    {
        get => Layout != PanelLayout.Hidden;
        set => Layout = value ? PanelLayout.Full : PanelLayout.Hidden;
    }
    public PanelTab Tab { get; set; } = PanelTab.Tools;
    // The controls overlay; like the drawer it pauses walking and the tool keys.
    public bool HelpOpen { get => helpOpen; set { helpOpen = value; SyncLayoutFlag(); } }
    // The drawer and the overlay pause walking and the tool hotkeys; the strip and the
    // HUD leave the visitor's mouse and keyboard alone and only swallow clicks on themselves.
    public bool CapturesInput => Layout == PanelLayout.Full || HelpOpen;

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

    // F2: chips, hotbar, prompts and toasts off, for a clean view of the scene; not saved, so each map load starts with it on.
    public bool HudVisible
    {
        get => hudVisible;
        set => hudVisible = value;
    }

    // The key prompts above the hotbar; experienced visitors can switch them off.
    public bool ShowPrompts
    {
        get => showPrompts;
        set { showPrompts = value; PlayerPrefs.SetInt(PromptsKey, value ? 1 : 0); }
    }

    // Evaluated on demand so the interactor's Update sees this frame's pointer.
    public bool PointerOverPanel => Contains(Input.mousePosition);

    // Hit test in screen pixels (origin bottom left, as Input.mousePosition) against
    // the active layout's rectangle and the hotbar, in scaled GUI space.
    public bool Contains(Vector2 screenPoint)
    {
        if (!isActiveAndEnabled || Station == null || Looking) return false;
        if (HelpOpen) return true;
        Vector2 p = new Vector2(screenPoint.x, Screen.height - screenPoint.y) / EffectiveScale;
        if (Layout != PanelLayout.Hidden && Area.Contains(p)) return true;
        if (!HotbarShown) return false;
        Rect bar = HotbarRect;
        return new Rect(bar.x - 8, bar.y - 8, bar.width + 16, bar.height + 16).Contains(p);
    }

    public void CycleLayout() { Layout = (PanelLayout)(((int)Layout + 1) % 3); }

    // Keys 1-6 and the first six hotbar slots, in this order.
    public static DemoToolKind HotbarKind(int index) => (DemoToolKind)Mathf.Clamp(index, 0, HotbarTools - 1);

    private bool Looking => Interactor != null && Interactor.Walker != null && Interactor.Walker.Looking;
    private bool DesktopActive => Interactor != null && Interactor.isActiveAndEnabled;
    private ShopWalkController Walker => Interactor != null ? Interactor.Walker : null;

    // Layout rectangles in GUI units (screen pixels divided by Scale).
    private float EffectiveScale => Mathf.Min(scale, Mathf.Min(Screen.width / 960f, Screen.height / 640f));
    private float GuiWidth => Screen.width / EffectiveScale;
    private float GuiHeight => Screen.height / EffectiveScale;
    private Rect Area
    {
        get
        {
            if (Layout == PanelLayout.Strip) return new Rect(0, 0, GuiWidth, StripHeight);
            float width = Mathf.Min(DrawerWidth, GuiWidth - 24);
            return new Rect(GuiWidth - width - 12, 12, width, GuiHeight - 24);
        }
    }

    private const float DrawerWidth = 510;
    private const float StripHeight = 56;
    private const int RecentTools = 8;
    private const int HotbarTools = 6, HotbarSlots = 9;
    private const float SlotGap = 6, SlotGroupGap = 18;
    private float SlotWidth=>90;
    private float SlotHeight=>94;
    private const float FlashSeconds = 0.25f;
    private const string ScaleKey = "DemoPanelScale", HudKey = "DemoHudVisible", PromptsKey = "DemoShowPrompts", TabKey = "DemoPanelTab";
    private static readonly string[] SlotNames = { "Cone", "Marker", "Tape", "Scale", "Reel", "Lamp", "Camera", "Light", "UV" };
    private static readonly string[] SlotKeys = { "1", "2", "3", "4", "5", "6", "F", "L", "U" };
    private static readonly float[] ScalePresets = { 1f, 1.25f, 1.5f, 1.75f, 2f };
    private static readonly int KindCount = Enum.GetValues(typeof(DemoToolKind)).Length;
    // Fixed text made once, so the HUD draws without allocating (static initializers run in
    // textual order: these follow ScalePresets and KindCount). TabNames follows PanelTab.
    private static readonly string[] ViewfinderKeysVR = { "Trigger", "Photograph" }, ViewfinderKeysDesktop = { "P", "Photo", "F", "Return" };
    private static readonly string[] ChipHintsVR = { "Tab", "Presenter panel", "F1", "Controls", "F9", "Desktop" }, ChipHintsDesktop = { "F1", "Controls", "Tab", "Panel" };
    private static readonly string[] DrawerHints = { "Tab/Esc", "Close", "F1", "Controls", "[/]", "Size" }, HelpHints = { "F1/H", "Close", "Esc", "Close" };
    private static readonly string[] TabNames = { "Tools", "Camera", "Map", "Scene", "Settings" };
    private static readonly string[] ScaleLabels = MakeScaleLabels();
    private static readonly string[] Numbers = MakeNumbers(100);
    private static readonly string[] TakeLabels = MakeKindLabels("Take a ");
    private static readonly string[] RackLabels = MakeKindLabels("Rack: new ");
    private static readonly string[] AimLabels = MakeKindLabels("Aim at the floor or a counter to place the ");
    private static string[] MakeScaleLabels() { var a = new string[ScalePresets.Length]; for (int i = 0; i < a.Length; i++) a[i] = "x" + ScalePresets[i].ToString("0.##"); return a; }
    private static string[] MakeNumbers(int n) { var a = new string[n]; for (int i = 0; i < n; i++) a[i] = i.ToString(); return a; }
    private static string[] MakeKindLabels(string prefix) { var a = new string[KindCount]; for (int i = 0; i < a.Length; i++) a[i] = prefix + DeployedTool.KindName((DemoToolKind)i); return a; }
    private static string Count(int n) => n >= 0 && n < Numbers.Length ? Numbers[n] : n.ToString(); // ToolStation has no cap

    private float scale = 1.25f;
    private bool hudVisible = true, showPrompts = true;
    private bool helpForVR;
    private int deployed;
    private float resetArmedUntil, flashUntil;
    private Vector2 scroll, helpScroll;
    private Action deferred;
    private readonly int[] kindCounts = new int[KindCount];
    private readonly List<string> pairs = new List<string>(12);
    // The active tools under the deployment root and their per-kind chips, refreshed on ToolsChanged.
    private readonly List<DeployedTool> placed = new List<DeployedTool>(32);
    private readonly string[] kindChips = new string[KindCount];
    // Count text, rebuilt only when a count changes.
    private int shownDeployed = -1, shownMarker = -1, shownPhotos = -2;
    private string toolChip, toolsText, markerText, photoText, statusLine;
    // The strip's button actions, made once instead of on every event.
    private Action openDrawer, requestReset, toggleScene, photograph;
    // The Scene tab's presenter sets sorted by label, rebuilt only when PresenterSet.All changes,
    // with each row's label and note copied beside it at that Layout event.
    private readonly List<PresenterSet> dressing = new List<PresenterSet>(8);
    private readonly List<string> dressingLabels = new List<string>(8), dressingNotes = new List<string>(8);
    private int dressingRevision = -1;
    private static readonly Comparison<PresenterSet> ByLabel = (a, b) => string.CompareOrdinal(a.Label, b.Label);

    private PanelLayout layout = PanelLayout.Hidden;
    private bool helpOpen;
    // IMGUI sends a Layout event before every event only while useGUILayout is on. Only the
    // drawer and the controls overlay use GUILayout; the HUD, strip, toasts and banner do not.
    private void SyncLayoutFlag() { useGUILayout = layout == PanelLayout.Full || helpOpen; }

    private void Awake()
    {
        scale = Mathf.Clamp(PlayerPrefs.GetFloat(ScaleKey, 1.25f), 1, 2);
        PlayerPrefs.DeleteKey(HudKey); // F2 used to be remembered: clear what older builds saved
        showPrompts = PlayerPrefs.GetInt(PromptsKey, 1) == 1;
        Tab = (PanelTab)Mathf.Clamp(PlayerPrefs.GetInt(TabKey, 0), 0, 4);
        openDrawer = () => Layout = PanelLayout.Full;
        requestReset = () => RequestReset();
        toggleScene = () => SceneState.Toggle();
        photograph = Photograph;
        SyncLayoutFlag();
    }

    private void OnEnable()
    {
        if (EvidenceCamera != null) EvidenceCamera.Shutter.AddListener(OnShutter);
        if (Station == null) return;
        Station.ToolsChanged.AddListener(Recount);
        Recount();
    }

    private void OnDisable()
    {
        if (EvidenceCamera != null) EvidenceCamera.Shutter.RemoveListener(OnShutter);
        if (Station != null) Station.ToolsChanged.RemoveListener(Recount);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) { HelpOpen = false; CycleLayout(); }
        if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.H)) ToggleHelp();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (HelpOpen) HelpOpen = false;
            else if (Layout == PanelLayout.Full) Layout = PanelLayout.Hidden;
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            HudVisible = !HudVisible;
            if (HudVisible) DemoFeedback.Post("HUD on  (F2 hides it)", FeedbackTone.Info, true);
        }
        if (Input.GetKeyDown(KeyCode.F8))
        {
            XRControllerInput.SmoothMovement = !XRControllerInput.SmoothMovement;
            DemoFeedback.Post(XRControllerInput.SmoothMovement ? "VR movement: smooth walking  (F8 for teleport)" : "VR movement: teleport  (F8 for smooth walking)", FeedbackTone.Info, true);
        }
        if(Input.GetKeyDown(KeyCode.F11))XRHeadTracking.HeightOffset-=.05f;
        if(Input.GetKeyDown(KeyCode.F12))XRHeadTracking.HeightOffset+=.05f;
        if (Input.GetKeyDown(KeyCode.F3)) XRGhostHand.ChangeOpacity(-0.1f);
        if (Input.GetKeyDown(KeyCode.F4)) XRGhostHand.ChangeOpacity(0.1f);
        if (Input.GetKeyDown(KeyCode.F6))
        {
            XRComfortVignette.Enabled = !XRComfortVignette.Enabled;
            DemoFeedback.Post(XRComfortVignette.Enabled ? "Comfort vignette on  (F6 turns it off)" : "Comfort vignette off  (F6 turns it on)", FeedbackTone.Info, true);
        }
        if (Input.GetKeyDown(KeyCode.LeftBracket)) ChangeScale(-0.25f);
        if (Input.GetKeyDown(KeyCode.RightBracket)) ChangeScale(0.25f);
    }

    public void ToggleHelp()
    {
        HelpOpen = !HelpOpen;
        if (HelpOpen) helpForVR = !DesktopActive && Interactor != null;
    }

    private void ChangeScale(float delta)
    {
        Scale += delta;
        DemoFeedback.Post("Interface size x" + scale.ToString("0.00"), FeedbackTone.Info, true);
    }

    private void OnGUI()
    {
        DemoUi.Skin s = DemoUi.Styles;
        // A button that takes the mouse release uses the event: answer it with the
        // recorded panel click (DemoSoundBank; silent without one).
        bool release = Event.current.type == EventType.MouseUp;
        float now = DemoFeedback.Now;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(EffectiveScale, EffectiveScale, 1));
        bool vr = Interactor != null && !Interactor.isActiveAndEnabled;
        if (Station != null)
        {
            RefreshCountText();
            // Under the controls overlay everything else is inert, so its buttons get the clicks.
            GUI.enabled = !HelpOpen;
            if (hudVisible) DrawHud(s, vr);
            else if (Looking) DrawCrosshair(s);
            if (Layout == PanelLayout.Strip) DrawStrip(s, vr);
            else if (Layout == PanelLayout.Full) DrawDrawer(s, vr);
            if (hudVisible || Layout != PanelLayout.Hidden) DrawToasts(s, now);
            DrawResetBanner(s);
            GUI.enabled = true;
            if (HelpOpen) DrawHelp(s);
            DrawFlash();
        }
        GUI.matrix = Matrix4x4.identity;
        if (release && Event.current.type == EventType.Used) DemoSounds.PlayInterface(DemoSound.Select);
        // Clicks act after the whole pass, so the controls drawn in this event match its layout.
        Action run = deferred;
        deferred = null;
        run?.Invoke();
    }

    private void Defer(Action action) { deferred += action; }

    // ---------------------------------------------------------------- HUD

    private void DrawHud(DemoUi.Skin s, bool vr)
    {
        if (Layout == PanelLayout.Hidden) DrawChips(s, vr);
        bool open = Layout == PanelLayout.Full || HelpOpen;
        if (DesktopActive && !open)
        {
            if (Looking) DrawCrosshair(s);
            else DrawPointerTip(s);
            if (showPrompts) DrawPrompt(s);
            DrawHotbar(s);
        }
        if (EvidenceCamera != null && EvidenceCamera.Viewfinder != null && !open)
            DrawViewfinder(s, ViewfinderRect(), vr ? ViewfinderKeysVR : ViewfinderKeysDesktop);
    }

    // Mode, light and tool state top left, wrapping before the toast column.
    private void DrawChips(DemoUi.Skin s, bool vr)
    {
        float x = 14, y = Layout == PanelLayout.Strip ? StripHeight + 12 : 14, limit = Layout == PanelLayout.Full ? Mathf.Min(GuiWidth * 0.62f, Area.x - 20) : GuiWidth * 0.62f;
        Chip(s, ref x, ref y, limit, vr ? "VR" : "DESKTOP", s.ChipAccent);
        if (Lighting != null && Lighting.Night) Chip(s, ref x, ref y, limit, "NIGHT", s.Chip);
        if (Lighting != null && Lighting.FlashlightOn) Chip(s, ref x, ref y, limit, "FLASHLIGHT", s.Chip);
        if (Lighting != null && Lighting.UVTorchOn) Chip(s, ref x, ref y, limit, "UV TORCH", s.Chip);
        ShopWalkController walker = Walker;
        if (!vr && walker != null && walker.Crouching) Chip(s, ref x, ref y, limit, "CROUCHING", s.Chip);
        if (!vr && walker != null && walker.Zooming) Chip(s, ref x, ref y, limit, "ZOOM", s.Chip);
        Chip(s, ref x, ref y, limit, toolChip, s.Chip);
        if (photoText != null && shownPhotos > 0) Chip(s, ref x, ref y, limit, photoText, s.Chip);
        y += 36;
        if (Station.PendingPrompt != null)
        {
            x = 14;
            Chip(s, ref x, ref y, limit, Station.PendingPrompt + (vr ? "" : "   (T)"), s.ChipAccent);
            y += 36;
        }
        if (showPrompts)
            DemoUi.DrawHints(new Rect(16, y, limit, 26), vr ? ChipHintsVR : ChipHintsDesktop);
    }

    private static void Chip(DemoUi.Skin s, ref float x, ref float y, float limit, string text, GUIStyle style)
    {
        GUIContent content = DemoUi.Content(text);
        Vector2 size = style.CalcSize(content);if(DemoLanguage.Arabic)size.x+=10;
        if (x > 14 && x + size.x > limit) { x = 14; y += 34; }
        DemoI18n.Label(new Rect(x, y, size.x, size.y), content, style);
        x += size.x + 6;
    }

    private string TargetName()
    {
        if (!DesktopActive || Interactor.Held != null) return null;
        if (Interactor.Hovered != null) return Interactor.Hovered.DisplayName;
        if (Interactor.HoveredSample != null) return RackLabels[(int)Interactor.HoveredSample.Kind];
        if (Interactor.HoveringCamera) return "Evidence camera";
        return null;
    }

    // Ring and dot at the screen centre while mouse look is on; brass when it has a target.
    private void DrawCrosshair(DemoUi.Skin s)
    {
        string target = TargetName();
        bool active = target != null || (DesktopActive && Interactor.Held != null && Interactor.HasPlacementTarget);
        Vector2 c = new Vector2(GuiWidth / 2, GuiHeight / 2);
        float r = active ? 12 : 9;
        Color previous = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.45f);
        GUI.DrawTexture(new Rect(c.x - r - 1.5f, c.y - r - 1.5f, 2 * r + 3, 2 * r + 3), DemoUi.Ring(16, 5, Color.white));
        GUI.color = active ? DemoUi.Accent : DemoUi.WithAlpha(DemoUi.Ink, 0.8f);
        GUI.DrawTexture(new Rect(c.x - r, c.y - r, 2 * r, 2 * r), DemoUi.Ring(16, 3, Color.white));
        GUI.DrawTexture(new Rect(c.x - 2, c.y - 2, 4, 4), DemoUi.Disc(4, Color.white));
        GUI.color = previous;
        if (target == null) return;
        GUIContent content = DemoUi.Content(target);
        Vector2 size = s.Chip.CalcSize(content);
        DemoI18n.Label(new Rect(c.x - size.x / 2, c.y + r + 10, size.x, size.y), content, s.Chip);
    }

    // With a free cursor: the name of what is under it, beside the pointer.
    private void DrawPointerTip(DemoUi.Skin s)
    {
        string target = TargetName();
        if (target == null) return;
        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / EffectiveScale;
        GUIContent content = DemoUi.Content(target);
        Vector2 size = s.Chip.CalcSize(content);
        float x = Mathf.Min(mouse.x + 18, GuiWidth - size.x - 6), y = Mathf.Min(mouse.y + 16, GuiHeight - size.y - 6);
        DemoI18n.Label(new Rect(x, y, size.x, size.y), content, s.Chip);
    }

    // What the keys do right now, as key-and-action pairs ("LMB", "Place", ...).
    public string[] ContextPrompt() { FillPrompt(); return pairs.ToArray(); }

    // Fills `pairs` in place, so the HUD's prompt reuses one list on every event.
    private void FillPrompt()
    {
        pairs.Clear();
        if (!DesktopActive) return;
        DeployedTool held = Interactor.Held;
        if (Interactor.HoldingCamera) { Add("P", "Photograph"); Add("F", "Return camera"); }
        else if (held != null)
        {
            if (Interactor.HasPlacementTarget) Add("LMB", "Place");
            Add("Q/E", "Rotate");
            if (held.Lamp != null) Add("T", "Lamp on/off");
            else if (held.TapeAnchor != null) Add("T", "Select end");
            Add("R", "Remove");
        }
        else if (Interactor.Hovered != null)
        {
            Add("LMB", "Pick up");
            if (Interactor.Hovered.Lamp != null) Add("T", "Lamp on/off");
            else if (Interactor.Hovered.TapeAnchor != null) Add("T", "Select end");
            Add("R", "Remove");
        }
        else if (Interactor.HoveredSample != null) Add("LMB", TakeLabels[(int)Interactor.HoveredSample.Kind]);
        else if (Interactor.HoveringCamera) { Add("LMB", "Take camera"); Add("P", "Photo from here"); }
        else { Add("WASD", "Walk"); Add("RMB", Looking ? "Free cursor" : "Mouse look"); Add("1-6", "Tools"); Add("F1", "Help"); }
    }

    private void Add(string key, string action) { pairs.Add(key); pairs.Add(action); }

    // A line above the key prompts when something needs doing first.
    public string ContextMessage()
    {
        if (!DesktopActive) return null;
        if (Interactor.Held != null && !Interactor.HasPlacementTarget)
            return AimLabels[(int)Interactor.Held.Kind];
        if (Station != null && Station.PendingPrompt != null) return Station.PendingPrompt + "   (T, X cancels)";
        return null;
    }

    private void DrawPrompt(DemoUi.Skin s)
    {
        FillPrompt();
        string message = ContextMessage();
        float bottom = HotbarRect.y - 14;
        if (pairs.Count > 0)
        {
            float w = DemoUi.HintWidth(pairs) + 28;
            Rect pill = new Rect((GuiWidth - w) / 2, bottom - 36, w, 36);
            DemoUi.Box(pill, 12, DemoUi.Glass, DemoUi.Line);
            DemoUi.DrawHints(new Rect(pill.x + 14, pill.y, w - 28, 36), pairs);
            bottom = pill.y - 8;
        }
        if (message == null) return;
        GUIContent content = DemoUi.Content(message);
        Vector2 size = s.Chip.CalcSize(content);
        DemoI18n.Label(new Rect((GuiWidth - size.x) / 2, bottom - size.y, size.x, size.y), content, s.ChipAccent);
    }

    // ---------------------------------------------------------------- hotbar

    private bool HotbarShown => DesktopActive && hudVisible && Layout != PanelLayout.Full && !HelpOpen;
    private float HotbarNaturalWidth => HotbarSlots * SlotWidth + (HotbarSlots - 1) * SlotGap + SlotGroupGap;
    private float SlotScale => Mathf.Min(1, (GuiWidth - 40) / HotbarNaturalWidth);

    private Rect HotbarRect
    {
        get
        {
            float k = SlotScale, w = HotbarNaturalWidth * k, h = SlotHeight * k;
            return new Rect((GuiWidth - w) / 2, GuiHeight - h - 18, w, h);
        }
    }

    private Rect SlotRect(int i)
    {
        Rect bar = HotbarRect;
        float k = SlotScale;
        float x = bar.x + (i * (SlotWidth + SlotGap) + (i >= HotbarTools ? SlotGroupGap : 0)) * k;
        return new Rect(x, bar.y, SlotWidth * k, SlotHeight * k);
    }

    private void DrawHotbar(DemoUi.Skin s)
    {
        Rect bar = HotbarRect;
        float k = SlotScale;
        DemoUi.Box(new Rect(bar.x - 8, bar.y - 8, bar.width + 16, bar.height + 16), 16, new Color(0.02f, 0.025f, 0.035f, 0.55f), DemoUi.Line);
        for (int i = 0; i < HotbarSlots; i++)
        {
            Rect slot = SlotRect(i);
            bool available = SlotAvailable(i);
            if (DemoI18n.Button(slot, GUIContent.none, SlotOn(i) ? s.SlotSelected : s.Slot) && available)
            {
                int index = i;
                Defer(() => UseSlot(index));
            }
            Color tint = SlotColor(i);
            if (!available) tint.a = 0.25f;
            float icon = Mathf.Min(slot.width*.45f,slot.height*.42f);
            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(new Rect(slot.center.x - icon / 2, slot.y + slot.height * 0.14f, icon, icon), DemoUi.Icon(SlotIcon(i)));
            GUI.color = previous;
            if (k > 0.7f) DemoI18n.Label(new Rect(slot.x + 6, slot.y + 3, 20, 28), DemoUi.Content(SlotKeys[i]), s.SlotKey);
            if (k > 0.70f) DemoI18n.Label(new Rect(slot.x+3, slot.yMax-43, slot.width-6, 40), DemoUi.Content(SlotNames[i]), s.SlotName);
            if (i < HotbarTools && kindCounts[i] > 0 && k > 0.7f)
                DemoI18n.Label(new Rect(slot.xMax - 24, slot.y + 3, 18, 28), DemoUi.Content(Count(kindCounts[i])), s.SlotCount);
        }
    }

    private bool SlotOn(int i)
    {
        if (i < HotbarTools) return DesktopActive && Interactor.Held != null && Interactor.Held.Kind == HotbarKind(i);
        if (i == 6) return DesktopActive && Interactor.HoldingCamera;
        if (Lighting == null) return false;
        return i == 7 ? Lighting.FlashlightOn : Lighting.UVTorchOn;
    }

    private bool SlotAvailable(int i) => i < HotbarTools || (i == 6 ? EvidenceCamera != null : Lighting != null);

    private static UiIcon SlotIcon(int i) => i < HotbarTools ? DemoUi.IconFor(HotbarKind(i)) : i == 6 ? UiIcon.Camera : i == 7 ? UiIcon.Flashlight : UiIcon.UVTorch;

    private static Color SlotColor(int i)
    {
        if (i < HotbarTools) return KindColor(HotbarKind(i));
        return i == 6 ? new Color(0.45f, 0.9f, 1f) : i == 7 ? new Color(1f, 0.93f, 0.78f) : new Color(0.72f, 0.55f, 1f);
    }

    private void UseSlot(int i)
    {
        if (!DesktopActive) return;
        if (i < HotbarTools) Interactor.SpawnIntoHand(HotbarKind(i));
        else if (i == 6) Interactor.ToggleCamera();
        else if (i == 7) Lighting.ToggleFlashlight();
        else Lighting.ToggleUVTorch();
    }

    // ---------------------------------------------------------------- live view, toasts, banners

    private Rect ViewfinderRect()
    {
        Rect r = new Rect(GuiWidth - 334, GuiHeight - 180 - 46, 320, 180);
        if (!HotbarShown) return r;
        Rect hotbar = HotbarRect;
        Rect band = new Rect(hotbar.x - 8, hotbar.y - 96, hotbar.width + 16, hotbar.height + 104);
        if (r.Overlaps(band)) r.y = band.y - r.height - 40;
        return r;
    }

    // 16:9 live view with thirds, a LIVE tag and its keys underneath.
    private void DrawViewfinder(DemoUi.Skin s, Rect rect, string[] keys)
    {
        DemoUi.Box(new Rect(rect.x - 5, rect.y - 5, rect.width + 10, rect.height + (keys != null ? 40 : 10)), 12, DemoUi.Glass, DemoUi.Line);
        GUI.DrawTexture(rect, EvidenceCamera.Viewfinder, ScaleMode.StretchToFill);
        Color guide = new Color(1, 1, 1, 0.18f);
        for (int i = 1; i < 3; i++)
        {
            DemoUi.Fill(new Rect(rect.x + rect.width * i / 3f, rect.y, 1, rect.height), guide);
            DemoUi.Fill(new Rect(rect.x, rect.y + rect.height * i / 3f, rect.width, 1), guide);
        }
        DemoI18n.Label(new Rect(rect.x + 8, rect.y + 8, 40, 18), DemoUi.Content("LIVE"), s.Badge);
        if (keys != null) DemoUi.DrawHints(new Rect(rect.x + 2, rect.yMax + 4, rect.width, 28), keys);
    }

    // Newest at the top, sliding in from the right; left of the drawer when it is open.
    private void DrawToasts(DemoUi.Skin s, float now)
    {
        DemoFeedback.Prune(now);
        IReadOnlyList<DemoFeedback.Message> recent = DemoFeedback.Recent;
        float right = Layout == PanelLayout.Full ? Area.x - 12 : GuiWidth - 14;
        float y = Layout == PanelLayout.Strip ? StripHeight + 10 : 14;
        for (int i = recent.Count - 1; i >= 0; i--)
        {
            DemoFeedback.Message message = recent[i];
            float alpha = DemoFeedback.Opacity(message, now);
            if (alpha <= 0) continue;
            GUIContent content = DemoUi.Content(message.Text);
            float textWidth = Mathf.Min(s.Body.CalcSize(content).x, 360);
            float h = Mathf.Max(38, s.Body.CalcHeight(content, textWidth) + 18);
            float w = textWidth + 42;
            Rect r = new Rect(right - w + (1 - alpha) * 30, y, w, h);
            Color previous = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            DemoUi.Box(r, 10, new Color(0.06f, 0.07f, 0.09f, 0.94f), DemoUi.Line);
            Color tone = DemoFeedback.ToneColor(message.Tone);
            DemoUi.Box(new Rect(r.x + 10, r.y + 10, 4, h - 20), 2, tone, tone);
            DemoI18n.Label(new Rect(r.x + 24, r.y + 9, textWidth + 4, h - 18), content, s.Body);
            GUI.color = previous;
            y += h + 8;
        }
    }

    private void DrawResetBanner(DemoUi.Skin s)
    {
        if (!ResetArmed || Session == null) return;
        GUIContent text = DemoUi.Content("Reset armed: press Reset again within 4 seconds to clear every placed tool");
        float w = Mathf.Min(s.Body.CalcSize(text).x + 44, GuiWidth - 24);
        float y = Layout == PanelLayout.Strip ? StripHeight + 10 : GuiHeight * 0.18f;
        Rect r = new Rect((GuiWidth - w) / 2, y, w, 42);
        DemoUi.Box(r, 12, new Color(0.32f, 0.08f, 0.07f, 0.96f), DemoUi.WithAlpha(DemoUi.Warning, 0.7f));
        DemoI18n.Label(r, text, s.Center);
    }

    private void OnShutter() { flashUntil = Time.unscaledTime + FlashSeconds; }

    private void DrawFlash()
    {
        float left = flashUntil - Time.unscaledTime;
        if (left > 0) DemoUi.Fill(new Rect(0, 0, GuiWidth, GuiHeight), new Color(1, 1, 1, 0.3f * left / FlashSeconds));
    }

    // ---------------------------------------------------------------- strip

    private void DrawStrip(DemoUi.Skin s, bool vr)
    {
        Rect area = Area;
        DemoUi.Fill(area, DemoUi.Glass);
        DemoUi.Fill(new Rect(0, area.yMax - 1, area.width, 1), DemoUi.Line);
        DemoUi.Fill(new Rect(0, 0, 3, area.height), DemoUi.Accent);
        float right = area.width - 10;
        if (EvidenceCamera != null && EvidenceCamera.Viewfinder != null)
        {
            right -= 72;
            DrawLiveThumb(s, new Rect(right, 8, 72, 40));
            right -= 8;
        }
        if (EvidenceCamera != null && EvidenceCamera.LastPhoto != null)
        {
            right -= 72;
            DemoUi.Box(new Rect(right - 2, 6, 76, 44), 6, DemoUi.Card, DemoUi.Line);
            GUI.DrawTexture(new Rect(right, 8, 72, 40), EvidenceCamera.LastPhotoDisplay, ScaleMode.ScaleToFit); // the mipmapped copy
            right -= 10;
        }
        right = StripButton(s, right, "Panel", s.ButtonGhost, openDrawer);
        if (Session != null) right = StripButton(s, right, ResetArmed ? "Confirm reset" : "Reset", ResetArmed ? s.ButtonDanger : s.Button, requestReset);
        if (SceneState != null) right = StripButton(s, right, SceneState.Intact ? "Show robbed" : "Show intact", s.Button, toggleScene);
        if (EvidenceCamera != null) right = StripButton(s, right, "Photo", s.ButtonAccent, photograph);

        float x = 16;
        GUIContent title = DemoUi.Content("CRIME SCENE");
        float titleWidth = s.Heading.CalcSize(title).x;
        DemoI18n.Label(new Rect(x, 0, titleWidth, StripHeight), title, s.Heading);
        x += titleWidth + 14;
        StripChip(s, ref x, right, vr ? "VR" : "DESKTOP", s.ChipAccent);
        if (Station.PendingPrompt != null) StripChip(s, ref x, right, Station.PendingPrompt + (vr ? "" : "  (T)"), s.ChipAccent);
        StripChip(s, ref x, right, toolsText, s.Chip);
        StripChip(s, ref x, right, markerText, s.Chip);
        if (photoText != null) StripChip(s, ref x, right, photoText, s.Chip);
        if (Lighting != null && Lighting.Night) StripChip(s, ref x, right, "Night", s.Chip);
        if (Lighting != null && Lighting.FlashlightOn) StripChip(s, ref x, right, "Flashlight", s.Chip);
        if (Lighting != null && Lighting.UVTorchOn) StripChip(s, ref x, right, "UV torch", s.Chip);
    }

    private static void StripChip(DemoUi.Skin s, ref float x, float right, string text, GUIStyle style)
    {
        GUIContent content = DemoUi.Content(text);
        Vector2 size = style.CalcSize(content);
        if (x + size.x > right - 10) return;
        DemoI18n.Label(new Rect(x, (StripHeight - size.y) / 2, size.x, size.y), content, style);
        x += size.x + 6;
    }

    private float StripButton(DemoUi.Skin s, float right, string label, GUIStyle style, Action action)
    {
        GUIContent content = DemoUi.Content(label);
        float w = style.CalcSize(content).x;
        Rect r = new Rect(right - w, (StripHeight - 36) / 2, w, 36);
        if (DemoI18n.Button(r, content, style)) Defer(action);
        return r.x - 6;
    }

    private void DrawLiveThumb(DemoUi.Skin s, Rect rect)
    {
        DemoUi.Box(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), 6, DemoUi.Card, DemoUi.WithAlpha(DemoUi.Warning, 0.7f));
        GUI.DrawTexture(rect, EvidenceCamera.Viewfinder, ScaleMode.StretchToFill);
    }

    // ---------------------------------------------------------------- drawer

    private void DrawDrawer(DemoUi.Skin s, bool vr)
    {
        GUILayout.BeginArea(Area, s.Panel);
        GUILayout.BeginHorizontal();
        DemoI18n.LayoutLabel(DemoUi.Content("Crime Scene Demo"), s.Title,GUILayout.ExpandWidth(true));
        GUILayout.FlexibleSpace();
        DemoI18n.LayoutLabel(DemoUi.Content(vr ? "VR" : "DESKTOP"), s.ChipAccent);
        GUILayout.Space(6);
        if (DemoI18n.LayoutButton("x", s.ButtonGhost, GUILayout.Width(38))) Defer(() => Layout = PanelLayout.Hidden);
        GUILayout.EndHorizontal();
        DemoI18n.LayoutLabel(DemoUi.Content(StatusLine()), s.Dim);
        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        // The loop variable is captured only inside the click branch, so no closure per tab and event.
        for (int i = 0; i < TabNames.Length; i++)
            if (DemoI18n.LayoutButton(DemoUi.Content(TabNames[i]), Tab == (PanelTab)i ? s.TabActive : s.Tab)) { PanelTab chosen = (PanelTab)i; Defer(() => SelectTab(chosen)); }
        GUILayout.EndHorizontal();
        GUILayout.Space(6);
        DemoUi.Fill(GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true)), DemoUi.Line);
        scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
        switch (Tab)
        {
            case PanelTab.Tools: ToolsTab(s, vr); break;
            case PanelTab.Camera: CameraTab(s, vr); break;
            case PanelTab.Map: MapTab(s); break;
            case PanelTab.Scene: SceneTab(s); break;
            default: SettingsTab(s); break;
        }
        GUILayout.Space(8);
        GUILayout.EndScrollView();
        DemoUi.Fill(GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true)), DemoUi.Line);
        DemoUi.DrawHints(GUILayoutUtility.GetRect(10, 34, GUILayout.ExpandWidth(true)), DrawerHints);
        GUILayout.EndArea();
    }

    private void SelectTab(PanelTab tab)
    {
        Tab = tab;
        scroll = Vector2.zero;
        PlayerPrefs.SetInt(TabKey, (int)tab);
    }

    private static void Heading(DemoUi.Skin s, string text)
    {
        GUILayout.Space(6);
        DemoI18n.LayoutLabel(text, s.Heading);
    }

    private string StatusLine() => statusLine;

    // The chip, strip and drawer count text, rebuilt only when a count changes (OnGUI calls it first).
    private void RefreshCountText()
    {
        int marker = Station.NextMarkerNumber, photos = EvidenceCamera != null ? EvidenceCamera.PhotoCount : -1;
        if (deployed == shownDeployed && marker == shownMarker && photos == shownPhotos) return;
        shownDeployed = deployed;
        shownMarker = marker;
        shownPhotos = photos;
        toolsText = deployed + (deployed == 1 ? " tool" : " tools");
        toolChip = toolsText + "   Â·   marker " + marker + " next";
        markerText = "Marker " + marker + " next";
        photoText = photos < 0 ? null : photos + (photos == 1 ? " photo" : " photos");
        statusLine = toolsText + " in the scene   Â·   marker " + marker + " next" + (photoText != null ? "   Â·   " + photoText : "");
    }

    private void ToolsTab(DemoUi.Skin s, bool vr)
    {
        Heading(s, vr ? "NEW TOOL  (appears at the rack)" : "NEW TOOL  (straight into your hand)");
        for (int row = 0; row < 2; row++)
        {
            GUILayout.BeginHorizontal();
            for (int col = 0; col < 3; col++)
            {
                int i = row * 3 + col;
                if (col > 0) GUILayout.Space(8);
                Rect tile = GUILayoutUtility.GetRect(90, 78, GUILayout.ExpandWidth(true));
                ToolTile(s, tile, HotbarKind(i), i);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
        }

        Heading(s, "TAPE AND MEASURING");
        bool auto = DemoUi.Switch("Join new posts and reels automatically", Station.AutoConnectTape);
        if (auto != Station.AutoConnectTape) { bool chosen = auto; Defer(() => Station.AutoConnectTape = chosen); }
        GUILayout.BeginHorizontal();
        if (DemoI18n.LayoutButton("Start a separate run", s.Button)) Defer(Station.StartNewTapeRun);
        if (Station.PendingPost != null && DemoI18n.LayoutButton("Cancel selection  (X)", s.Button)) Defer(Station.CancelTapeSelection);
        GUILayout.EndHorizontal();
        if (Station.PendingPrompt != null) DemoI18n.LayoutLabel("<color=" + DemoUi.Hex(DemoUi.Accent) + ">" + Station.PendingPrompt + "</color>", s.Body);
        if (Station.LitLampCount > 0) DemoI18n.LayoutLabel("Scene lamps lit: " + Station.LitLampCount + " of " + ToolStation.MaxLitLamps, s.Dim);

        Heading(s, "PLACED");
        PlacedTools(s);
    }

    private void ToolTile(DemoUi.Skin s, Rect tile, DemoToolKind kind, int index)
    {
        // A captured parameter would allocate the closure on every call: copy it inside the click branch.
        if (DemoI18n.Button(tile, GUIContent.none, s.Slot)) { DemoToolKind chosen = kind; Defer(() => SpawnFromPanel(chosen)); }
        Color previous = GUI.color;
        GUI.color = KindColor(kind);
        GUI.DrawTexture(new Rect(tile.center.x - 17, tile.y + 10, 34, 34), DemoUi.Icon(DemoUi.IconFor(kind)));
        GUI.color = previous;
        DemoI18n.Label(new Rect(tile.x, tile.yMax - 32, tile.width, 28), DemoUi.Content(DeployedTool.Title(kind)), s.SlotName);
        DemoI18n.Label(new Rect(tile.x + 7, tile.y + 4, 20, 28), DemoUi.Content(SlotKeys[index]), s.SlotKey);
        if (kindCounts[(int)kind] > 0) DemoI18n.Label(new Rect(tile.xMax - 26, tile.y + 4, 20, 28), DemoUi.Content(Count(kindCounts[(int)kind])), s.SlotCount);
    }

    // Desktop: into the visitor's hand, and the drawer closes so it can be placed at once.
    // VR: at the rack for the headset visitor to pick up.
    private void SpawnFromPanel(DemoToolKind kind)
    {
        if (DesktopActive)
        {
            if (Interactor.SpawnIntoHand(kind) != null) Layout = PanelLayout.Hidden;
        }
        else
        {
            DeployedTool tool = Station.Spawn(kind);
            if (tool != null) DemoFeedback.Post(tool.DisplayName + " waiting at the rack", FeedbackTone.Success);
        }
    }

    // Counts per kind as chips, then only the most recent few with Remove buttons, so a
    // busy scene does not push the rest of the drawer away.
    private void PlacedTools(DemoUi.Skin s)
    {
        // `placed` and the chips come from Recount (ToolsChanged), not a hierarchy walk per event.
        if (placed.Count == 0) { DemoI18n.LayoutLabel("Nothing placed yet. Press 1-6 or click a tile above.", s.Dim); return; }
        GUILayout.BeginHorizontal();
        for (int i = 0; i < KindCount; i++)
            if (kindChips[i] != null) DemoI18n.LayoutLabel(DemoUi.Content(kindChips[i]), s.Chip);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        int first = Mathf.Max(0, placed.Count - RecentTools);
        if (first > 0) DemoI18n.LayoutLabel("+" + first + " earlier", s.Small);
        for (int i = placed.Count - 1; i >= first; i--)
        {
            DeployedTool tool = placed[i];
            if (tool == null) continue;
            GUILayout.BeginHorizontal(s.Card);
            Rect dot = GUILayoutUtility.GetRect(14, 22, GUILayout.Width(14));
            Color previous = GUI.color;
            GUI.color = KindColor(tool.Kind);
            GUI.DrawTexture(new Rect(dot.x, dot.center.y - 5, 10, 10), DemoUi.Disc(5, Color.white));
            GUI.color = previous;
            DemoI18n.LayoutLabel(tool.DisplayName + (tool.Holder != null ? "  Â·  in hand" : ""), s.Body);
            GUILayout.FlexibleSpace();
            if (DemoI18n.LayoutButton("Remove", s.ButtonGhost, GUILayout.Width(84))) { DeployedTool target = tool; Defer(() => RemoveTool(target)); }
            GUILayout.EndHorizontal();
        }
    }

    private void RemoveTool(DeployedTool tool)
    {
        if (tool == null) return;
        if (tool.Holder != null) tool.Holder.Remove(tool);
        else if (Interactor != null) Interactor.Remove(tool);
        else Station.RemoveTool(tool);
    }

    private static string KindName(DemoToolKind kind, int count)
    {
        string name = DeployedTool.KindName(kind);
        return count == 1 ? name : name + "s";
    }

    private void CameraTab(DemoUi.Skin s, bool vr)
    {
        if (EvidenceCamera == null) { DemoI18n.LayoutLabel("No evidence camera in this scene.", s.Dim); return; }
        Heading(s, "EVIDENCE CAMERA");
        GUILayout.BeginVertical(s.Card);
        DemoI18n.LayoutLabel(EvidenceCamera.Status, s.Body);
        DemoI18n.LayoutLabel(EvidenceCamera.PhotoCount + (EvidenceCamera.PhotoCount == 1 ? " photo" : " photos") + " this session"
            + (EvidenceCamera.Holder != null ? "   Â·   camera in hand" : "   Â·   camera on its rack"), s.Dim);
        GUILayout.EndVertical();
        GUILayout.BeginHorizontal();
        if (DemoI18n.LayoutButton("Take photo  (P)", s.ButtonAccent)) Defer(Photograph);
        if (DesktopActive && DemoI18n.LayoutButton(Interactor.HoldingCamera ? "Return camera  (F)" : "Hold camera  (F)", s.Button))
            Defer(Interactor.ToggleCamera);
        GUILayout.EndHorizontal();
        if (DemoI18n.LayoutButton("Open photo folder", s.ButtonGhost)) Defer(EvidenceCamera.OpenPhotoFolder);
        // Panel padding, the scrollbar and its margin.
        float width = Area.width - 52;
        if (EvidenceCamera.Viewfinder != null)
        {
            Heading(s, "LIVE VIEW");
            Rect live = GUILayoutUtility.GetRect(width, width * 9 / 16 + 8);
            DrawViewfinder(s, new Rect(live.x + 4, live.y + 4, live.width - 8, live.height - 8), null);
        }
        if (EvidenceCamera.LastPhoto != null)
        {
            Heading(s, "LAST PHOTO");
            Rect photo = GUILayoutUtility.GetRect(width, width * 9 / 16 + 8);
            DemoUi.Box(photo, 10, DemoUi.Card, DemoUi.Line);
            GUI.DrawTexture(new Rect(photo.x + 4, photo.y + 4, photo.width - 8, photo.height - 8), EvidenceCamera.LastPhotoDisplay, ScaleMode.ScaleToFit);
        }
        if (Review != null)
        {
            Heading(s, "INSTRUCTOR REVIEW");
            GUILayout.BeginHorizontal();
            if (DemoI18n.LayoutButton("Save snapshot", s.Button)) Defer(Review.SaveSnapshot);
            if (DemoI18n.LayoutButton("Open review", s.Button)) Defer(Review.OpenReview);
            GUILayout.EndHorizontal();
            DemoI18n.LayoutLabel(Review.Status, s.Dim);
        }
    }

    private void MapTab(DemoUi.Skin s)
    {
        if (Map == null || Map.Texture == null) { DemoI18n.LayoutLabel("No overview map in this scene.", s.Dim); return; }
        Heading(s, "OVERVIEW");
        float size = Area.width - 52;
        Rect frame = GUILayoutUtility.GetRect(size, size);
        DemoUi.Box(frame, 10, DemoUi.Card, DemoUi.Line);
        Rect mapRect = new Rect(frame.x + 4, frame.y + 4, frame.width - 8, frame.height - 8);
        GUI.DrawTexture(mapRect, Map.Texture, ScaleMode.ScaleToFit);
        for (int i = 0; i < placed.Count; i++)
        {
            DeployedTool tool = placed[i];
            if (tool != null) Glyph(mapRect, Map.ToMap(tool.transform.position), KindColor(tool.Kind), 9);
        }
        if (EvidenceCamera != null && EvidenceCamera.Holder != null) Glyph(mapRect, Map.ToMap(EvidenceCamera.transform.position), new Color(0.45f, 0.9f, 1f), 8);
        if (Player != null) Glyph(mapRect, Map.ToMap(Player.position), DemoUi.Success, 12);
        if (Beacon != null)
        {
            // A click on the map sends the beacon there: the square texture fills the
            // square rectangle, so the inverse of Glyph gives the map coordinate.
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && !HelpOpen && mapRect.Contains(e.mousePosition))
            {
                Vector2 uv = new Vector2((e.mousePosition.x - mapRect.x) / mapRect.width, 1 - (e.mousePosition.y - mapRect.y) / mapRect.height);
                Vector3 point = Map.FromMap(uv);
                Defer(() => Beacon.Place(point));
                e.Use();
            }
            if (Beacon.Active) Glyph(mapRect, Map.ToMap(Beacon.Position), new Color(1f, 0.65f, 0.15f), 12);
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            DemoI18n.LayoutLabel(Beacon.Active ? "Beacon placed. Click the map to move it." : "Click the map to place the beacon (B at the pointer).", s.Dim);
            if (Beacon.Active && DemoI18n.LayoutButton("Clear", s.ButtonGhost, GUILayout.Width(70))) Defer(Beacon.Clear);
            GUILayout.EndHorizontal();
        }
        Heading(s, "LEGEND");
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        Legend(s, DemoUi.Success, "Visitor");
        Legend(s, KindColor(DemoToolKind.Cone), "Cones");
        Legend(s, KindColor(DemoToolKind.Marker), "Markers");
        Legend(s, KindColor(DemoToolKind.TapePost), "Tape posts");
        Legend(s, KindColor(DemoToolKind.Scale), "Scales");
        GUILayout.EndVertical();
        GUILayout.BeginVertical();
        Legend(s, KindColor(DemoToolKind.Measure), "Measuring reels");
        Legend(s, KindColor(DemoToolKind.Lamp), "Scene lamps");
        Legend(s, new Color(0.45f, 0.9f, 1f), "Camera in hand");
        Legend(s, new Color(1f, 0.65f, 0.15f), "Beacon");
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private static void Legend(DemoUi.Skin s, Color color, string label)
    {
        GUILayout.BeginHorizontal();
        Rect dot = GUILayoutUtility.GetRect(16, 22, GUILayout.Width(16));
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(dot.x + 2, dot.center.y - 5, 10, 10), DemoUi.Disc(5, Color.white));
        GUI.color = previous;
        DemoI18n.LayoutLabel(label, s.Dim);
        GUILayout.EndHorizontal();
    }

    // Map glyph centred on a 0..1 map coordinate (v up), with a dark outline.
    private static void Glyph(Rect mapRect, Vector2 uv, Color color, float size)
    {
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return;
        float x = mapRect.x + uv.x * mapRect.width, y = mapRect.y + (1 - uv.y) * mapRect.height;
        Color previous = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.75f);
        GUI.DrawTexture(new Rect(x - size / 2 - 1.5f, y - size / 2 - 1.5f, size + 3, size + 3), DemoUi.Disc(8, Color.white));
        GUI.color = color;
        GUI.DrawTexture(new Rect(x - size / 2, y - size / 2, size, size), DemoUi.Disc(8, Color.white));
        GUI.color = previous;
    }

    private void SceneTab(DemoUi.Skin s)
    {
        if (SceneState != null)
        {
            Heading(s, "STORE");
            GUILayout.BeginHorizontal();
            if (DemoI18n.LayoutButton("Robbed", SceneState.Intact ? s.Tab : s.TabActive) && SceneState.Intact) Defer(SceneState.Toggle);
            if (DemoI18n.LayoutButton("Intact", SceneState.Intact ? s.TabActive : s.Tab) && !SceneState.Intact) Defer(SceneState.Toggle);
            GUILayout.EndHorizontal();
            DemoI18n.LayoutLabel("I switches between them.", s.Small);
        }
        if (Lighting != null)
        {
            Heading(s, "LIGHTING");
            // A baked night scene's day is the diagnostic daylight for comparing the layout.
            GUILayout.BeginHorizontal();
            if (DemoI18n.LayoutButton("Night", Lighting.Night ? s.TabActive : s.Tab) && !Lighting.Night) Defer(Lighting.ToggleNight);
            if (DemoI18n.LayoutButton(Lighting.BakedNight ? "Diagnostic day" : "Day", Lighting.Night ? s.Tab : s.TabActive) && Lighting.Night) Defer(Lighting.ToggleNight);
            GUILayout.EndHorizontal();
            DemoI18n.LayoutLabel("N switches between them.", s.Small);
            bool torch = DemoUi.Switch("Flashlight  (L)", Lighting.FlashlightOn);
            if (torch != Lighting.FlashlightOn) Defer(Lighting.ToggleFlashlight);
            bool uv = DemoUi.Switch("UV torch  (U)", Lighting.UVTorchOn);
            if (uv != Lighting.UVTorchOn) Defer(Lighting.ToggleUVTorch);
            if (Lighting.UVExamples != null)
            {
                bool examples = DemoUi.Switch("UV example traces", Lighting.UVExamplesVisible);
                if (examples != Lighting.UVExamplesVisible) Defer(Lighting.ToggleUVExamples);
                DemoI18n.LayoutLabel("Placeholder traces: what they represent is the instructor's choice.", s.Small);
            }
            if (Lighting.LightBar != null)
            {
                bool flashing = DemoUi.Switch("Light bar flashing", Lighting.LightBarFlashing);
                if (flashing != Lighting.LightBarFlashing) Defer(Lighting.ToggleLightBarFlashing);
                DemoI18n.LayoutLabel("Switch it off to hold the bar steady for visitors sensitive to flashing light. A reset makes it flash again.", s.Small);
            }
            if (Station.LitLampCount > 0) DemoI18n.LayoutLabel("Scene lamps lit: " + Station.LitLampCount + " of " + ToolStation.MaxLitLamps, s.Dim);
        }
        DressingSection(s);
        MusicSection(s);
        if (Beacon != null)
        {
            Heading(s, "BEACON");
            GUILayout.BeginHorizontal();
            DemoI18n.LayoutLabel(Beacon.Active ? "Showing on the floor." : "Not placed. Use the Map tab or B.", s.Dim);
            GUILayout.FlexibleSpace();
            if (Beacon.Active && DemoI18n.LayoutButton("Clear", s.ButtonGhost, GUILayout.Width(70))) Defer(Beacon.Clear);
            GUILayout.EndHorizontal();
        }
        if (Session != null)
        {
            Heading(s, "SESSION");
            DemoI18n.LayoutLabel("Removes every placed tool and restores the store. Photos are kept.", s.Dim);
            if (DemoI18n.LayoutButton(ResetArmed ? "Click again to confirm the reset" : "Reset placed tools and scene", ResetArmed ? s.ButtonDanger : s.Button))
                Defer(() => RequestReset());
        }
    }

    // One switch per presenter set (PresenterSet.All), sorted by label. The sorted copy and its
    // labels and notes are rebuilt only on a Layout event, so the event that follows it draws
    // the same rows with the same GUILayout controls.
    private void DressingSection(DemoUi.Skin s)
    {
        if (dressingRevision != PresenterSet.Revision && Event.current.type == EventType.Layout)
        {
            dressingRevision = PresenterSet.Revision;
            dressing.Clear();
            for (int i = 0; i < PresenterSet.All.Count; i++) dressing.Add(PresenterSet.All[i]);
            dressing.Sort(ByLabel);
            dressingLabels.Clear();
            dressingNotes.Clear();
            for (int i = 0; i < dressing.Count; i++) { dressingLabels.Add(dressing[i].Label); dressingNotes.Add(dressing[i].Note); }
        }
        if (dressing.Count == 0) return;
        Heading(s, "SCENE DRESSING");
        for (int i = 0; i < dressing.Count; i++)
        {
            PresenterSet set = dressing[i];
            if (set == null)
            {
                // Destroyed since the Layout event: the same row, inert, until the next one rebuilds the list.
                bool was = GUI.enabled;
                GUI.enabled = false;
                DemoUi.Switch(dressingLabels[i], false);
                GUI.enabled = was;
            }
            else
            {
                bool shown = set.Shown;
                // The delegate is made only inside the click branch.
                if (DemoUi.Switch(dressingLabels[i], shown) != shown) Defer(set.Toggle);
            }
            if (!string.IsNullOrEmpty(dressingNotes[i])) DemoI18n.LayoutLabel(dressingNotes[i], s.Small);
        }
    }

    // The underscore bed (DemoMusic), off at arrival; nothing without a bed clip.
    private void MusicSection(DemoUi.Skin s)
    {
        DemoMusic music = DemoMusic.Instance;
        if (music == null || music.Bed == null) return;
        Heading(s, "MUSIC");
        bool on = DemoUi.Switch("Underscore music", music.BedOn);
        if (on != music.BedOn) Defer(music.ToggleBed);
        DemoI18n.LayoutLabel("Off at arrival. It sets a mood only and never answers what the visitor does.", s.Small);
    }

    private void SettingsTab(DemoUi.Skin s)
    {
        Heading(s,"LANGUAGE");
        GUILayout.BeginHorizontal();
        if(DemoI18n.LayoutButton(DemoLanguage.Shape("Ø§Ù„Ø¹Ø±Ø¨ÙŠØ©"),DemoLanguage.Arabic?s.TabActive:s.Tab))Defer(()=>DemoLanguage.Arabic=true);
        if(DemoI18n.LayoutButton("English",!DemoLanguage.Arabic?s.TabActive:s.Tab))Defer(()=>DemoLanguage.Arabic=false);
        GUILayout.EndHorizontal();DemoI18n.LayoutLabel("Store signs stay Arabic in both UI languages.",s.Small);
        Heading(s, "INTERFACE SIZE  ([ ])");
        GUILayout.BeginHorizontal();
        // Captured values are copied inside the click branches, so no closure is made per event.
        for (int i = 0; i < ScalePresets.Length; i++)
            if (DemoI18n.LayoutButton(DemoUi.Content(ScaleLabels[i]), Mathf.Abs(scale - ScalePresets[i]) < 0.01f ? s.TabActive : s.Tab))
            {
                float value = ScalePresets[i];
                Defer(() => Scale = value);
            }
        GUILayout.EndHorizontal();
        Heading(s, "HEADS-UP DISPLAY");
        bool hud = DemoUi.Switch("Show the HUD  (F2)", hudVisible);
        if (hud != hudVisible) { bool chosen = hud; Defer(() => HudVisible = chosen); }
        bool prompts = DemoUi.Switch("Key prompts above the hotbar", showPrompts);
        if (prompts != showPrompts) { bool chosen = prompts; Defer(() => ShowPrompts = chosen); }
        ShopWalkController walker = Walker;
        if (walker != null)
        {
            Heading(s, "MOUSE AND VIEW");
            float sensitivity = SliderRow(s, "Look sensitivity", walker.LookSensitivity, 0.5f, 5f, "0.0");
            if (!Mathf.Approximately(sensitivity, walker.LookSensitivity)) { walker.LookSensitivity = sensitivity; walker.SaveSettings(); }
            float fov = SliderRow(s, "Field of view", walker.FieldOfView, 50, 90, "0");
            if (!Mathf.Approximately(fov, walker.FieldOfView)) walker.SetFieldOfView(Mathf.Round(fov));
            bool invert = DemoUi.Switch("Invert mouse Y", walker.InvertY);
            if (invert != walker.InvertY)
            {
                ShopWalkController target = walker;
                bool chosen = invert;
                Defer(() => { target.InvertY = chosen; target.SaveSettings(); });
            }
            DemoI18n.LayoutLabel("C crouches, hold Z to zoom, Shift to walk faster.", s.Small);
        }
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (DemoI18n.LayoutButton("Controls  (F1)", s.Button)) Defer(ToggleHelp);
        if (DemoI18n.LayoutButton("Restore defaults", s.ButtonGhost)) Defer(RestoreDefaults);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        // The one credits text, kept by DemoUi (no copy here to drift from the licences).
        DemoI18n.LayoutLabel(DemoUi.Credits, s.Small);
    }

    private static float SliderRow(DemoUi.Skin s, string label, float value, float min, float max, string format)
    {
        GUILayout.BeginHorizontal();
        DemoI18n.LayoutLabel(label, s.Body, GUILayout.Width(140));
        GUILayout.BeginVertical();
        GUILayout.Space(7);
        float result = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
        GUILayout.EndVertical();
        DemoI18n.LayoutLabel(result.ToString(format), s.Dim, GUILayout.Width(40));
        GUILayout.EndHorizontal();
        return result;
    }

    private void RestoreDefaults()
    {
        Scale = 1.25f;
        HudVisible = true;
        ShowPrompts = true;
        if (Walker != null) Walker.ResetSettings();
        DemoFeedback.Post("Settings restored to their defaults", FeedbackTone.Info, true);
    }

    // ---------------------------------------------------------------- controls overlay

    private static readonly string[][] DesktopHelp =
    {
        new[] { "MOVE AND LOOK", "W/A/S/D", "Walk (arrows too)", "Shift", "Walk faster", "RMB", "Mouse look on / off", "Esc", "Free the cursor",
            "C", "Crouch", "Z", "Zoom while held", "Home", "Back to the start" },
        new[] { "TOOLS", "1-6", "New tool into your hand", "LMB", "Pick up, place, take from the rack", "Q/E", "Rotate (or the wheel)",
            "R", "Remove (Delete too)", "T", "Select tape end, switch a lamp", "X", "Cancel the tape selection" },
        new[] { "CAMERA AND LIGHT", "F", "Hold / return the camera", "P", "Photograph", "L", "Flashlight beam (click while held)", "U", "UV beam",
            "N", "Night / day", "I", "Robbed / intact store", "B", "Beacon where you point" },
        new[] { "INTERFACE", "Tab", "Panel: none, strip, drawer", "[/]", "Interface size", "F1/H", "This overlay", "F2", "Hide the HUD",
            "F9", "Desktop / VR", "F10", "Start menu" },
    };

    private static readonly string[][] VRHelp =
    {
        new[] { "BOTH CONTROLLERS", "Grip", "Grab, hold, drop (rack, camera)", "Trigger", "Use: photo, torch, tape, lamp",
            "A/X", "New tool into the hand", "B/Y", "Remove held tool, else next tool" },
        new[] { "STICKS", "Left", "Push to aim, release to teleport", "Left click", "UV torch", "Right", "Snap turn",
            "Right click", "Flashlight", "Menu", "Hide or show the hints" },
        new[] { "IN THE HEADSET", "Look", "At a controller to see its hints", "Wrist", "Left wrist shows the scene status",
            "Toasts", "Below your view after each action" },
        new[] { "AT THE PC", "Tab", "Presenter panel", "F1/H", "This overlay", "F9", "Desktop / VR", "F10", "Start menu" },
    };

    private void DrawHelp(DemoUi.Skin s)
    {
        DemoUi.Fill(new Rect(0, 0, GuiWidth, GuiHeight), new Color(0, 0, 0, 0.55f));
        float w = Mathf.Min(860, GuiWidth - 32), h = Mathf.Min(600, GuiHeight - 32);
        GUILayout.BeginArea(new Rect((GuiWidth - w) / 2, (GuiHeight - h) / 2, w, h), s.Panel);
        GUILayout.BeginHorizontal();
        DemoI18n.LayoutLabel("Controls", s.Title);
        GUILayout.FlexibleSpace();
        if (DemoI18n.LayoutButton("Mouse and keyboard", helpForVR ? s.Tab : s.TabActive)) Defer(() => helpForVR = false);
        if (DemoI18n.LayoutButton("Quest 2 controllers", helpForVR ? s.TabActive : s.Tab)) Defer(() => helpForVR = true);
        GUILayout.Space(6);
        if (DemoI18n.LayoutButton("x", s.ButtonGhost, GUILayout.Width(38))) Defer(() => HelpOpen = false);
        GUILayout.EndHorizontal();
        GUILayout.Space(6);
        DemoUi.Fill(GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true)), DemoUi.Line);
        helpScroll = GUILayout.BeginScrollView(helpScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
        string[][] groups = helpForVR ? VRHelp : DesktopHelp;
        // Reserve the scroll bar before splitting into two columns. RTL labels
        // align to the right edge, making a clipped column especially apparent.
        float column = (w - s.Panel.padding.horizontal - 28 - 20) / 2;
        GUILayout.BeginHorizontal();
        for (int c = 0; c < 2; c++)
        {
            if (c > 0) GUILayout.Space(28);
            GUILayout.BeginVertical(GUILayout.Width(column));
            for (int g = c * 2; g < c * 2 + 2; g++)
            {
                Heading(s, groups[g][0]);
                for (int i = 1; i + 1 < groups[g].Length; i += 2) DemoUi.HintRow(groups[g][i], groups[g][i + 1], 112);
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        DemoUi.DrawHints(GUILayoutUtility.GetRect(10, 32, GUILayout.ExpandWidth(true)), HelpHints);
        GUILayout.EndArea();
    }

    // ---------------------------------------------------------------- actions

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

    // Desktop: through the interactor (held lens or view). VR: the held lens, else the headset view.
    public void Photograph()
    {
        if (EvidenceCamera == null) return;
        if (Interactor != null && Interactor.isActiveAndEnabled) Interactor.Photograph();
        else if (EvidenceCamera.Holder != null || Camera.main == null) EvidenceCamera.CapturePhoto();
        else EvidenceCamera.CaptureFromView(Camera.main.transform);
    }

    // One hierarchy walk per ToolsChanged: the counts, the drawer's rows, the map glyphs and the
    // kind chips all read what it collects. Every removal deactivates the tool before the event.
    private void Recount()
    {
        for (int i = 0; i < kindCounts.Length; i++) kindCounts[i] = 0;
        placed.Clear();
        if (Station != null && Station.DeploymentRoot != null) Station.DeploymentRoot.GetComponentsInChildren(placed); // active only, like before
        for (int i = 0; i < placed.Count; i++) kindCounts[(int)placed[i].Kind]++;
        deployed = placed.Count;
        for (int i = 0; i < KindCount; i++) kindChips[i] = kindCounts[i] > 0 ? kindCounts[i] + " " + KindName((DemoToolKind)i, kindCounts[i]) : null;
    }

    // The one palette for tool kinds; SessionReviewRecorder's sketches and the VR carousel use it too.
    public static Color KindColor(DemoToolKind kind)
    {
        switch (kind)
        {
            case DemoToolKind.Cone: return new Color(1f, 0.55f, 0.1f);
            case DemoToolKind.Marker: return new Color(1f, 0.9f, 0.2f);
            case DemoToolKind.TapePost: return new Color(1f, 0.25f, 0.2f);
            case DemoToolKind.Scale: return Color.white;
            case DemoToolKind.Lamp: return new Color(1f, 0.5f, 0.78f);
            default: return new Color(0.75f, 0.55f, 1f);
        }
    }
}
