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
//  - the controls overlay (F1 or H). F2 hides the HUD for a clean view.
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

    public PanelLayout Layout { get; set; } = PanelLayout.Hidden;
    // Older callers only know open or closed; open means the full drawer.
    public bool Visible
    {
        get => Layout != PanelLayout.Hidden;
        set => Layout = value ? PanelLayout.Full : PanelLayout.Hidden;
    }
    public PanelTab Tab { get; set; } = PanelTab.Tools;
    // The controls overlay; like the drawer it pauses walking and the tool keys.
    public bool HelpOpen { get; set; }
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

    // F2: chips, hotbar, prompts and toasts off, for a clean view of the scene.
    public bool HudVisible
    {
        get => hudVisible;
        set { hudVisible = value; PlayerPrefs.SetInt(HudKey, value ? 1 : 0); }
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
        Vector2 p = new Vector2(screenPoint.x, Screen.height - screenPoint.y) / scale;
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
    private float GuiWidth => Screen.width / scale;
    private float GuiHeight => Screen.height / scale;
    private Rect Area
    {
        get
        {
            if (Layout == PanelLayout.Strip) return new Rect(0, 0, GuiWidth, StripHeight);
            float width = Mathf.Min(DrawerWidth, GuiWidth - 24);
            return new Rect(GuiWidth - width - 12, 12, width, GuiHeight - 24);
        }
    }

    private const float DrawerWidth = 420;
    private const float StripHeight = 56;
    private const int RecentTools = 8;
    private const int HotbarTools = 6, HotbarSlots = 9;
    private const float SlotSize = 58, SlotGap = 6, SlotGroupGap = 18;
    private const float FlashSeconds = 0.25f;
    private const string ScaleKey = "DemoPanelScale", HudKey = "DemoHudVisible", PromptsKey = "DemoShowPrompts", TabKey = "DemoPanelTab";
    private const string Credits =
        "Models: Kenney, KayKit (CC0); Khronos glTF samples (CC0 / CC BY 4.0); textures cgbookcase, HDRIs HDRI Haven (CC0); "
        + "Barlow font (SIL OFL 1.1). See ThirdParty licenses.";
    private static readonly string[] SlotNames = { "Cone", "Marker", "Tape", "Scale", "Reel", "Lamp", "Camera", "Light", "UV" };
    private static readonly string[] SlotKeys = { "1", "2", "3", "4", "5", "6", "F", "L", "U" };
    private static readonly float[] ScalePresets = { 1f, 1.25f, 1.5f, 1.75f, 2f };
    private static readonly int KindCount = Enum.GetValues(typeof(DemoToolKind)).Length;

    private float scale = 1.25f;
    private bool hudVisible = true, showPrompts = true;
    private bool helpForVR;
    private int deployed;
    private float resetArmedUntil, flashUntil;
    private Vector2 scroll, helpScroll;
    private Action deferred;
    private readonly int[] kindCounts = new int[KindCount];
    private readonly List<string> pairs = new List<string>(12);

    private void Awake()
    {
        scale = Mathf.Clamp(PlayerPrefs.GetFloat(ScaleKey, 1.25f), 1, 2);
        hudVisible = PlayerPrefs.GetInt(HudKey, 1) == 1;
        showPrompts = PlayerPrefs.GetInt(PromptsKey, 1) == 1;
        Tab = (PanelTab)Mathf.Clamp(PlayerPrefs.GetInt(TabKey, 0), 0, 4);
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
            if (HudVisible) DemoFeedback.Post("HUD on  (F2 hides it)");
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
        DemoFeedback.Post("Interface size x" + scale.ToString("0.00"));
    }

    private void OnGUI()
    {
        DemoUi.Skin s = DemoUi.Styles;
        // A button that takes the mouse release uses the event: answer it with the
        // recorded panel click (DemoSoundBank; silent without one).
        bool release = Event.current.type == EventType.MouseUp;
        float now = DemoFeedback.Now;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
        bool vr = Interactor != null && !Interactor.isActiveAndEnabled;
        if (Station != null)
        {
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
            DrawViewfinder(s, ViewfinderRect(), vr ? new[] { "Trigger", "Photograph" } : new[] { "P", "Photo", "F", "Return" });
    }

    // Mode, light and tool state top left, wrapping before the toast column.
    private void DrawChips(DemoUi.Skin s, bool vr)
    {
        float x = 14, y = 14, limit = GuiWidth * 0.62f;
        Chip(s, ref x, ref y, limit, vr ? "VR" : "DESKTOP", s.ChipAccent);
        if (Lighting != null && Lighting.Night) Chip(s, ref x, ref y, limit, "NIGHT", s.Chip);
        if (Lighting != null && Lighting.FlashlightOn) Chip(s, ref x, ref y, limit, "FLASHLIGHT", s.Chip);
        if (Lighting != null && Lighting.UVTorchOn) Chip(s, ref x, ref y, limit, "UV TORCH", s.Chip);
        ShopWalkController walker = Walker;
        if (!vr && walker != null && walker.Crouching) Chip(s, ref x, ref y, limit, "CROUCHING", s.Chip);
        if (!vr && walker != null && walker.Zooming) Chip(s, ref x, ref y, limit, "ZOOM", s.Chip);
        Chip(s, ref x, ref y, limit, deployed + (deployed == 1 ? " tool" : " tools") + "   ·   marker " + Station.NextMarkerNumber + " next", s.Chip);
        if (EvidenceCamera != null && EvidenceCamera.PhotoCount > 0)
            Chip(s, ref x, ref y, limit, EvidenceCamera.PhotoCount + (EvidenceCamera.PhotoCount == 1 ? " photo" : " photos"), s.Chip);
        y += 36;
        if (Station.PendingPrompt != null)
        {
            x = 14;
            Chip(s, ref x, ref y, limit, Station.PendingPrompt + (vr ? "" : "   (T)"), s.ChipAccent);
            y += 36;
        }
        if (showPrompts)
            DemoUi.DrawHints(new Rect(16, y, limit, 26), vr ? new[] { "Tab", "Presenter panel", "F1", "Controls", "F9", "Desktop" } : new[] { "F1", "Controls", "Tab", "Panel" });
    }

    private static void Chip(DemoUi.Skin s, ref float x, ref float y, float limit, string text, GUIStyle style)
    {
        Vector2 size = style.CalcSize(new GUIContent(text));
        if (x > 14 && x + size.x > limit) { x = 14; y += 34; }
        GUI.Label(new Rect(x, y, size.x, size.y), text, style);
        x += size.x + 6;
    }

    private string TargetName()
    {
        if (!DesktopActive || Interactor.Held != null) return null;
        if (Interactor.Hovered != null) return Interactor.Hovered.DisplayName;
        if (Interactor.HoveredSample != null) return "Rack: new " + DeployedTool.KindName(Interactor.HoveredSample.Kind);
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
        Vector2 size = s.Chip.CalcSize(new GUIContent(target));
        GUI.Label(new Rect(c.x - size.x / 2, c.y + r + 10, size.x, size.y), target, s.Chip);
    }

    // With a free cursor: the name of what is under it, beside the pointer.
    private void DrawPointerTip(DemoUi.Skin s)
    {
        string target = TargetName();
        if (target == null) return;
        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) / scale;
        Vector2 size = s.Chip.CalcSize(new GUIContent(target));
        float x = Mathf.Min(mouse.x + 18, GuiWidth - size.x - 6), y = Mathf.Min(mouse.y + 16, GuiHeight - size.y - 6);
        GUI.Label(new Rect(x, y, size.x, size.y), target, s.Chip);
    }

    // What the keys do right now, as key-and-action pairs ("LMB", "Place", ...).
    public string[] ContextPrompt()
    {
        pairs.Clear();
        if (!DesktopActive) return pairs.ToArray();
        DeployedTool held = Interactor.Held;
        if (Interactor.HoldingCamera) Add("P", "Photograph", "F", "Return camera");
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
        else if (Interactor.HoveredSample != null) Add("LMB", "Take a " + DeployedTool.KindName(Interactor.HoveredSample.Kind));
        else if (Interactor.HoveringCamera) Add("LMB", "Take camera", "P", "Photo from here");
        else Add("WASD", "Walk", "RMB", Looking ? "Free cursor" : "Mouse look", "1-6", "Tools", "F1", "Help");
        return pairs.ToArray();
    }

    private void Add(params string[] items) { pairs.AddRange(items); }

    // A line above the key prompts when something needs doing first.
    public string ContextMessage()
    {
        if (!DesktopActive) return null;
        if (Interactor.Held != null && !Interactor.HasPlacementTarget)
            return "Aim at the floor or a counter to place the " + DeployedTool.KindName(Interactor.Held.Kind);
        if (Station != null && Station.PendingPrompt != null) return Station.PendingPrompt + "   (T, X cancels)";
        return null;
    }

    private void DrawPrompt(DemoUi.Skin s)
    {
        string[] hints = ContextPrompt();
        string message = ContextMessage();
        float bottom = HotbarRect.y - 14;
        if (hints.Length > 0)
        {
            float w = DemoUi.HintWidth(hints) + 28;
            Rect pill = new Rect((GuiWidth - w) / 2, bottom - 36, w, 36);
            DemoUi.Box(pill, 12, DemoUi.Glass, DemoUi.Line);
            DemoUi.DrawHints(new Rect(pill.x + 14, pill.y, w - 28, 36), hints);
            bottom = pill.y - 8;
        }
        if (message == null) return;
        GUIContent content = new GUIContent(message);
        Vector2 size = s.Chip.CalcSize(content);
        GUI.Label(new Rect((GuiWidth - size.x) / 2, bottom - size.y, size.x, size.y), content, s.ChipAccent);
    }

    // ---------------------------------------------------------------- hotbar

    private bool HotbarShown => DesktopActive && hudVisible && Layout != PanelLayout.Full && !HelpOpen;
    private float HotbarNaturalWidth => HotbarSlots * SlotSize + (HotbarSlots - 1) * SlotGap + SlotGroupGap;
    private float SlotScale => Mathf.Min(1, (GuiWidth - 40) / HotbarNaturalWidth);

    private Rect HotbarRect
    {
        get
        {
            float k = SlotScale, w = HotbarNaturalWidth * k, h = SlotSize * k;
            return new Rect((GuiWidth - w) / 2, GuiHeight - h - 18, w, h);
        }
    }

    private Rect SlotRect(int i)
    {
        Rect bar = HotbarRect;
        float k = SlotScale;
        float x = bar.x + (i * (SlotSize + SlotGap) + (i >= HotbarTools ? SlotGroupGap : 0)) * k;
        return new Rect(x, bar.y, SlotSize * k, SlotSize * k);
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
            if (GUI.Button(slot, GUIContent.none, SlotOn(i) ? s.SlotSelected : s.Slot) && available)
            {
                int index = i;
                Defer(() => UseSlot(index));
            }
            Color tint = SlotColor(i);
            if (!available) tint.a = 0.25f;
            float icon = slot.height * 0.5f;
            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(new Rect(slot.center.x - icon / 2, slot.y + slot.height * 0.14f, icon, icon), DemoUi.Icon(SlotIcon(i)));
            GUI.color = previous;
            if (k > 0.7f) GUI.Label(new Rect(slot.x + 6, slot.y + 3, 20, 16), SlotKeys[i], s.SlotKey);
            if (k > 0.85f) GUI.Label(new Rect(slot.x, slot.yMax - 19, slot.width, 16), SlotNames[i], s.SlotName);
            if (i < HotbarTools && kindCounts[i] > 0 && k > 0.7f)
                GUI.Label(new Rect(slot.xMax - 24, slot.y + 3, 18, 16), kindCounts[i].ToString(), s.SlotCount);
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
        GUI.Label(new Rect(rect.x + 8, rect.y + 8, 40, 18), "LIVE", s.Badge);
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
            GUIContent content = new GUIContent(message.Text);
            float textWidth = Mathf.Min(s.Body.CalcSize(content).x, 360);
            float h = Mathf.Max(38, s.Body.CalcHeight(content, textWidth) + 18);
            float w = textWidth + 42;
            Rect r = new Rect(right - w + (1 - alpha) * 30, y, w, h);
            Color previous = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            DemoUi.Box(r, 10, new Color(0.06f, 0.07f, 0.09f, 0.94f), DemoUi.Line);
            Color tone = DemoFeedback.ToneColor(message.Tone);
            DemoUi.Box(new Rect(r.x + 10, r.y + 10, 4, h - 20), 2, tone, tone);
            GUI.Label(new Rect(r.x + 24, r.y + 9, textWidth + 4, h - 18), content, s.Body);
            GUI.color = previous;
            y += h + 8;
        }
    }

    private void DrawResetBanner(DemoUi.Skin s)
    {
        if (!ResetArmed || Session == null) return;
        GUIContent text = new GUIContent("Reset armed: press Reset again within 4 seconds to clear every placed tool");
        float w = Mathf.Min(s.Body.CalcSize(text).x + 44, GuiWidth - 24);
        float y = Layout == PanelLayout.Strip ? StripHeight + 10 : GuiHeight * 0.18f;
        Rect r = new Rect((GuiWidth - w) / 2, y, w, 42);
        DemoUi.Box(r, 12, new Color(0.32f, 0.08f, 0.07f, 0.96f), DemoUi.WithAlpha(DemoUi.Warning, 0.7f));
        GUI.Label(r, text, s.Center);
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
            GUI.DrawTexture(new Rect(right, 8, 72, 40), EvidenceCamera.LastPhoto, ScaleMode.ScaleToFit);
            right -= 10;
        }
        right = StripButton(s, right, "Panel", s.ButtonGhost, () => Layout = PanelLayout.Full);
        if (Session != null) right = StripButton(s, right, ResetArmed ? "Confirm reset" : "Reset", ResetArmed ? s.ButtonDanger : s.Button, () => RequestReset());
        if (SceneState != null) right = StripButton(s, right, SceneState.Intact ? "Show robbed" : "Show intact", s.Button, () => SceneState.Toggle());
        if (EvidenceCamera != null) right = StripButton(s, right, "Photo", s.ButtonAccent, Photograph);

        float x = 16;
        GUIContent title = new GUIContent("CRIME SCENE");
        float titleWidth = s.Heading.CalcSize(title).x;
        GUI.Label(new Rect(x, 0, titleWidth, StripHeight), title, s.Heading);
        x += titleWidth + 14;
        StripChip(s, ref x, right, vr ? "VR" : "DESKTOP", s.ChipAccent);
        if (Station.PendingPrompt != null) StripChip(s, ref x, right, Station.PendingPrompt + (vr ? "" : "  (T)"), s.ChipAccent);
        StripChip(s, ref x, right, deployed + (deployed == 1 ? " tool" : " tools"), s.Chip);
        StripChip(s, ref x, right, "Marker " + Station.NextMarkerNumber + " next", s.Chip);
        if (EvidenceCamera != null) StripChip(s, ref x, right, EvidenceCamera.PhotoCount + (EvidenceCamera.PhotoCount == 1 ? " photo" : " photos"), s.Chip);
        if (Lighting != null && Lighting.Night) StripChip(s, ref x, right, "Night", s.Chip);
        if (Lighting != null && Lighting.FlashlightOn) StripChip(s, ref x, right, "Flashlight", s.Chip);
        if (Lighting != null && Lighting.UVTorchOn) StripChip(s, ref x, right, "UV torch", s.Chip);
    }

    private static void StripChip(DemoUi.Skin s, ref float x, float right, string text, GUIStyle style)
    {
        Vector2 size = style.CalcSize(new GUIContent(text));
        if (x + size.x > right - 10) return;
        GUI.Label(new Rect(x, (StripHeight - size.y) / 2, size.x, size.y), text, style);
        x += size.x + 6;
    }

    private float StripButton(DemoUi.Skin s, float right, string label, GUIStyle style, Action action)
    {
        float w = style.CalcSize(new GUIContent(label)).x;
        Rect r = new Rect(right - w, (StripHeight - 36) / 2, w, 36);
        if (GUI.Button(r, label, style)) Defer(action);
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
        GUILayout.Label("Crime Scene Demo", s.Title);
        GUILayout.FlexibleSpace();
        GUILayout.Label(vr ? "VR" : "DESKTOP", s.ChipAccent);
        GUILayout.Space(6);
        if (GUILayout.Button("x", s.ButtonGhost, GUILayout.Width(38))) Defer(() => Layout = PanelLayout.Hidden);
        GUILayout.EndHorizontal();
        GUILayout.Label(StatusLine(), s.Dim);
        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        for (int i = 0; i < 5; i++)
        {
            PanelTab tab = (PanelTab)i;
            if (GUILayout.Button(tab.ToString(), Tab == tab ? s.TabActive : s.Tab)) Defer(() => SelectTab(tab));
        }
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
        DemoUi.DrawHints(GUILayoutUtility.GetRect(10, 34, GUILayout.ExpandWidth(true)), new[] { "Tab/Esc", "Close", "F1", "Controls", "[/]", "Size" });
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
        GUILayout.Label(text, s.Heading);
    }

    private string StatusLine()
    {
        string text = deployed + (deployed == 1 ? " tool" : " tools") + " in the scene   ·   marker " + Station.NextMarkerNumber + " next";
        if (EvidenceCamera != null) text += "   ·   " + EvidenceCamera.PhotoCount + (EvidenceCamera.PhotoCount == 1 ? " photo" : " photos");
        return text;
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
        if (auto != Station.AutoConnectTape) Defer(() => Station.AutoConnectTape = auto);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start a separate run", s.Button)) Defer(Station.StartNewTapeRun);
        if (Station.PendingPost != null && GUILayout.Button("Cancel selection  (X)", s.Button)) Defer(Station.CancelTapeSelection);
        GUILayout.EndHorizontal();
        if (Station.PendingPrompt != null) GUILayout.Label("<color=" + DemoUi.Hex(DemoUi.Accent) + ">" + Station.PendingPrompt + "</color>", s.Body);
        if (Station.LitLampCount > 0) GUILayout.Label("Scene lamps lit: " + Station.LitLampCount + " of " + ToolStation.MaxLitLamps, s.Dim);

        Heading(s, "PLACED");
        PlacedTools(s);
    }

    private void ToolTile(DemoUi.Skin s, Rect tile, DemoToolKind kind, int index)
    {
        if (GUI.Button(tile, GUIContent.none, s.Slot)) Defer(() => SpawnFromPanel(kind));
        Color previous = GUI.color;
        GUI.color = KindColor(kind);
        GUI.DrawTexture(new Rect(tile.center.x - 17, tile.y + 10, 34, 34), DemoUi.Icon(DemoUi.IconFor(kind)));
        GUI.color = previous;
        GUI.Label(new Rect(tile.x, tile.yMax - 26, tile.width, 20), DeployedTool.Title(kind), s.SlotName);
        GUI.Label(new Rect(tile.x + 7, tile.y + 4, 20, 16), (index + 1).ToString(), s.SlotKey);
        if (kindCounts[(int)kind] > 0) GUI.Label(new Rect(tile.xMax - 26, tile.y + 4, 20, 16), kindCounts[(int)kind].ToString(), s.SlotCount);
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
        DeployedTool[] tools = Station.DeploymentRoot != null ? Station.DeploymentRoot.GetComponentsInChildren<DeployedTool>() : new DeployedTool[0];
        if (tools.Length == 0) { GUILayout.Label("Nothing placed yet. Press 1-6 or click a tile above.", s.Dim); return; }
        GUILayout.BeginHorizontal();
        for (int i = 0; i < KindCount; i++)
            if (kindCounts[i] > 0) GUILayout.Label(kindCounts[i] + " " + KindName((DemoToolKind)i, kindCounts[i]), s.Chip);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        int first = Mathf.Max(0, tools.Length - RecentTools);
        if (first > 0) GUILayout.Label("+" + first + " earlier", s.Small);
        for (int i = tools.Length - 1; i >= first; i--)
        {
            DeployedTool tool = tools[i];
            GUILayout.BeginHorizontal(s.Card);
            Rect dot = GUILayoutUtility.GetRect(14, 22, GUILayout.Width(14));
            Color previous = GUI.color;
            GUI.color = KindColor(tool.Kind);
            GUI.DrawTexture(new Rect(dot.x, dot.center.y - 5, 10, 10), DemoUi.Disc(5, Color.white));
            GUI.color = previous;
            GUILayout.Label(tool.DisplayName + (tool.Holder != null ? "  ·  in hand" : ""), s.Body);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove", s.ButtonGhost, GUILayout.Width(84))) Defer(() => RemoveTool(tool));
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
        if (EvidenceCamera == null) { GUILayout.Label("No evidence camera in this scene.", s.Dim); return; }
        Heading(s, "EVIDENCE CAMERA");
        GUILayout.BeginVertical(s.Card);
        GUILayout.Label(EvidenceCamera.Status, s.Body);
        GUILayout.Label(EvidenceCamera.PhotoCount + (EvidenceCamera.PhotoCount == 1 ? " photo" : " photos") + " this session"
            + (EvidenceCamera.Holder != null ? "   ·   camera in hand" : "   ·   camera on its rack"), s.Dim);
        GUILayout.EndVertical();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Take photo  (P)", s.ButtonAccent)) Defer(Photograph);
        if (DesktopActive && GUILayout.Button(Interactor.HoldingCamera ? "Return camera  (F)" : "Hold camera  (F)", s.Button))
            Defer(Interactor.ToggleCamera);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Open photo folder", s.ButtonGhost)) Defer(EvidenceCamera.OpenPhotoFolder);
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
            GUI.DrawTexture(new Rect(photo.x + 4, photo.y + 4, photo.width - 8, photo.height - 8), EvidenceCamera.LastPhoto, ScaleMode.ScaleToFit);
        }
        if (Review != null)
        {
            Heading(s, "INSTRUCTOR REVIEW");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save snapshot", s.Button)) Defer(Review.SaveSnapshot);
            if (GUILayout.Button("Open review", s.Button)) Defer(Review.OpenReview);
            GUILayout.EndHorizontal();
            GUILayout.Label(Review.Status, s.Dim);
        }
    }

    private void MapTab(DemoUi.Skin s)
    {
        if (Map == null || Map.Texture == null) { GUILayout.Label("No overview map in this scene.", s.Dim); return; }
        Heading(s, "OVERVIEW");
        float size = Area.width - 52;
        Rect frame = GUILayoutUtility.GetRect(size, size);
        DemoUi.Box(frame, 10, DemoUi.Card, DemoUi.Line);
        Rect mapRect = new Rect(frame.x + 4, frame.y + 4, frame.width - 8, frame.height - 8);
        GUI.DrawTexture(mapRect, Map.Texture, ScaleMode.ScaleToFit);
        if (Station.DeploymentRoot != null)
            foreach (DeployedTool tool in Station.DeploymentRoot.GetComponentsInChildren<DeployedTool>())
                Glyph(mapRect, Map.ToMap(tool.transform.position), KindColor(tool.Kind), 9);
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
            GUILayout.Label(Beacon.Active ? "Beacon placed. Click the map to move it." : "Click the map to place the beacon (B at the pointer).", s.Dim);
            if (Beacon.Active && GUILayout.Button("Clear", s.ButtonGhost, GUILayout.Width(70))) Defer(Beacon.Clear);
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
        GUILayout.Label(label, s.Dim);
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
            if (GUILayout.Button("Robbed", SceneState.Intact ? s.Tab : s.TabActive) && SceneState.Intact) Defer(SceneState.Toggle);
            if (GUILayout.Button("Intact", SceneState.Intact ? s.TabActive : s.Tab) && !SceneState.Intact) Defer(SceneState.Toggle);
            GUILayout.EndHorizontal();
            GUILayout.Label("I switches between them.", s.Small);
        }
        if (Lighting != null)
        {
            Heading(s, "LIGHTING");
            // A baked night scene's day is the diagnostic daylight for comparing the layout.
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Night", Lighting.Night ? s.TabActive : s.Tab) && !Lighting.Night) Defer(Lighting.ToggleNight);
            if (GUILayout.Button(Lighting.BakedNight ? "Diagnostic day" : "Day", Lighting.Night ? s.Tab : s.TabActive) && Lighting.Night) Defer(Lighting.ToggleNight);
            GUILayout.EndHorizontal();
            GUILayout.Label("N switches between them.", s.Small);
            bool torch = DemoUi.Switch("Flashlight  (L)", Lighting.FlashlightOn);
            if (torch != Lighting.FlashlightOn) Defer(Lighting.ToggleFlashlight);
            bool uv = DemoUi.Switch("UV torch  (U)", Lighting.UVTorchOn);
            if (uv != Lighting.UVTorchOn) Defer(Lighting.ToggleUVTorch);
            if (Lighting.UVExamples != null)
            {
                bool examples = DemoUi.Switch("UV example traces", Lighting.UVExamplesVisible);
                if (examples != Lighting.UVExamplesVisible) Defer(Lighting.ToggleUVExamples);
                GUILayout.Label("Placeholder traces: what they represent is the instructor's choice.", s.Small);
            }
            if (Station.LitLampCount > 0) GUILayout.Label("Scene lamps lit: " + Station.LitLampCount + " of " + ToolStation.MaxLitLamps, s.Dim);
        }
        if (Beacon != null)
        {
            Heading(s, "BEACON");
            GUILayout.BeginHorizontal();
            GUILayout.Label(Beacon.Active ? "Showing on the floor." : "Not placed. Use the Map tab or B.", s.Dim);
            GUILayout.FlexibleSpace();
            if (Beacon.Active && GUILayout.Button("Clear", s.ButtonGhost, GUILayout.Width(70))) Defer(Beacon.Clear);
            GUILayout.EndHorizontal();
        }
        if (Session != null)
        {
            Heading(s, "SESSION");
            GUILayout.Label("Removes every placed tool and restores the store. Photos are kept.", s.Dim);
            if (GUILayout.Button(ResetArmed ? "Click again to confirm the reset" : "Reset placed tools and scene", ResetArmed ? s.ButtonDanger : s.Button))
                Defer(() => RequestReset());
        }
    }

    private void SettingsTab(DemoUi.Skin s)
    {
        Heading(s, "INTERFACE SIZE  ([ ])");
        GUILayout.BeginHorizontal();
        foreach (float preset in ScalePresets)
        {
            float value = preset;
            if (GUILayout.Button("x" + preset.ToString("0.##"), Mathf.Abs(scale - preset) < 0.01f ? s.TabActive : s.Tab)) Defer(() => Scale = value);
        }
        GUILayout.EndHorizontal();
        Heading(s, "HEADS-UP DISPLAY");
        bool hud = DemoUi.Switch("Show the HUD  (F2)", hudVisible);
        if (hud != hudVisible) Defer(() => HudVisible = hud);
        bool prompts = DemoUi.Switch("Key prompts above the hotbar", showPrompts);
        if (prompts != showPrompts) Defer(() => ShowPrompts = prompts);
        ShopWalkController walker = Walker;
        if (walker != null)
        {
            Heading(s, "MOUSE AND VIEW");
            float sensitivity = SliderRow(s, "Look sensitivity", walker.LookSensitivity, 0.5f, 5f, "0.0");
            if (!Mathf.Approximately(sensitivity, walker.LookSensitivity)) { walker.LookSensitivity = sensitivity; walker.SaveSettings(); }
            float fov = SliderRow(s, "Field of view", walker.FieldOfView, 50, 90, "0");
            if (!Mathf.Approximately(fov, walker.FieldOfView)) walker.SetFieldOfView(Mathf.Round(fov));
            bool invert = DemoUi.Switch("Invert mouse Y", walker.InvertY);
            if (invert != walker.InvertY) Defer(() => { walker.InvertY = invert; walker.SaveSettings(); });
            GUILayout.Label("C crouches, hold Z to zoom, Shift to walk faster.", s.Small);
        }
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Controls  (F1)", s.Button)) Defer(ToggleHelp);
        if (GUILayout.Button("Restore defaults", s.ButtonGhost)) Defer(RestoreDefaults);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.Label(Credits, s.Small);
    }

    private static float SliderRow(DemoUi.Skin s, string label, float value, float min, float max, string format)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, s.Body, GUILayout.Width(140));
        GUILayout.BeginVertical();
        GUILayout.Space(7);
        float result = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
        GUILayout.EndVertical();
        GUILayout.Label(result.ToString(format), s.Dim, GUILayout.Width(40));
        GUILayout.EndHorizontal();
        return result;
    }

    private void RestoreDefaults()
    {
        Scale = 1.25f;
        HudVisible = true;
        ShowPrompts = true;
        if (Walker != null) Walker.ResetSettings();
        DemoFeedback.Post("Settings restored to their defaults");
    }

    // ---------------------------------------------------------------- controls overlay

    private static readonly string[][] DesktopHelp =
    {
        new[] { "MOVE AND LOOK", "W/A/S/D", "Walk (arrows too)", "Shift", "Walk faster", "RMB", "Mouse look on / off", "Esc", "Free the cursor",
            "C", "Crouch", "Z", "Zoom while held", "Home", "Back to the start" },
        new[] { "TOOLS", "1-6", "New tool into your hand", "LMB", "Pick up, place, take from the rack", "Q/E", "Rotate (or the wheel)",
            "R", "Remove (Delete too)", "T", "Select tape end, switch a lamp", "X", "Cancel the tape selection" },
        new[] { "CAMERA AND LIGHT", "F", "Hold / return the camera", "P", "Photograph", "L", "Flashlight", "U", "UV torch",
            "N", "Night / day", "I", "Robbed / intact store", "B", "Beacon where you point" },
        new[] { "INTERFACE", "Tab", "Panel: none, strip, drawer", "[/]", "Interface size", "F1/H", "This overlay", "F2", "Hide the HUD",
            "F9", "Desktop / VR", "F10", "Start menu" },
    };

    private static readonly string[][] VRHelp =
    {
        new[] { "BOTH CONTROLLERS", "Grip", "Grab, hold, drop (rack, camera)", "Trigger", "Use: photo, tape end, lamp",
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
        GUILayout.Label("Controls", s.Title);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Mouse and keyboard", helpForVR ? s.Tab : s.TabActive)) Defer(() => helpForVR = false);
        if (GUILayout.Button("Quest 2 controllers", helpForVR ? s.TabActive : s.Tab)) Defer(() => helpForVR = true);
        GUILayout.Space(6);
        if (GUILayout.Button("x", s.ButtonGhost, GUILayout.Width(38))) Defer(() => HelpOpen = false);
        GUILayout.EndHorizontal();
        GUILayout.Space(6);
        DemoUi.Fill(GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true)), DemoUi.Line);
        helpScroll = GUILayout.BeginScrollView(helpScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
        string[][] groups = helpForVR ? VRHelp : DesktopHelp;
        float column = (w - 36 - 28) / 2;
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
        DemoUi.DrawHints(GUILayoutUtility.GetRect(10, 32, GUILayout.ExpandWidth(true)), new[] { "F1/H", "Close", "Esc", "Close" });
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

    private void Recount()
    {
        for (int i = 0; i < kindCounts.Length; i++) kindCounts[i] = 0;
        deployed = 0;
        if (Station == null || Station.DeploymentRoot == null) return;
        foreach (DeployedTool tool in Station.DeploymentRoot.GetComponentsInChildren<DeployedTool>())
        {
            kindCounts[(int)tool.Kind]++;
            deployed++;
        }
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
