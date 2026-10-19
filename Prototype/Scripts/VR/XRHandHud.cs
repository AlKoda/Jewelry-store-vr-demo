using System.Collections.Generic;
using UnityEngine;

// The headset's interface on one controller, built at runtime from DemoUi pieces in
// the same dark glass and brass as the monitor (XRControllerInput adds it):
//  - a Quest 2 style controller model (grip, face, tracking ring, stick, buttons, trigger)
//    whose buttons light up in brass while pressed;
//  - hint tooltips beside the buttons with leader lines, shown for the first seconds and
//    afterwards whenever the visitor looks at the controller; the left menu button hides
//    them on both hands. The A/X hint is the builder's Label and names the tool it spawns;
//  - a tool carousel above the controller for a moment after B/Y picks the next kind;
//  - on the left hand, a wrist card with the scene status (tools, next marker, photos,
//    lights, the pending tape selection), shown when looked at;
//  - on the right hand, a toast that floats a little below the view and repeats the
//    monitor's messages (DemoFeedback) except presenter-only ones, so both screens say
//    the same thing about the scene.
// Every piece is on DemoUi.HeadsetUiLayer, so photos, the viewfinder and the overview
// map never show it; hints that have faded out stop rendering.
public sealed class XRHandHud : MonoBehaviour
{
    public enum Control { Trigger, Grip, Primary, Secondary, Stick, Menu }

    public float OnboardingSeconds = 20f;
    public float LookAngle = 30f;
    public float LookDistance = 0.8f;
    public float CarouselSeconds = 2.5f;

    public int TooltipCount => tips.Count;
    public bool CarouselVisible => carousel != null && carousel.gameObject.activeSelf;
    public string CarouselLabel => carouselName != null ? carouselName.text : "";
    public TextMesh StatusText => statusText;
    public Transform Model => model;

    private const float TextHeight = 0.0115f, PanelHeight = 0.022f, KeyHeight = 0.016f, Pad = 0.004f, Gap = 0.004f;
    private static readonly Color PanelColor = new Color(0.05f, 0.06f, 0.08f, 0.88f);
    private static readonly Color KeyInk = new Color(0.09f, 0.065f, 0.02f);
    private static readonly Color LineColor = new Color(1f, 1f, 1f, 0.5f);
    private static readonly Color BodyColor = new Color(0.15f, 0.16f, 0.18f);
    private static readonly Color FaceColor = new Color(0.09f, 0.095f, 0.11f);
    private static readonly Color ButtonColor = new Color(0.27f, 0.28f, 0.31f);
    private static Mesh torus;

    private sealed class Tip
    {
        public Control Control;
        public Transform Root, Anchor;
        public TextMesh Key, Action;
        public Renderer KeyRenderer, ActionRenderer;
        public MeshRenderer Panel, KeyPanel;
        public LineRenderer Line;
        public bool ExtendRight;
    }

    private XRControllerInput input;
    private bool left;
    private float side, startTime, tipAlpha = -1, appliedTipAlpha = -1, statusAlpha, appliedStatusAlpha = -1, carouselUntil, nextStatus;
    private bool hintsShown = true, tipsRendering = true;
    private Transform model;
    private readonly List<Tip> tips = new List<Tip>();
    private readonly Dictionary<Control, Renderer> buttons = new Dictionary<Control, Renderer>();
    private readonly Dictionary<Control, bool> lit = new Dictionary<Control, bool>();
    // Every material made here (text materials are DemoUi's, shared per font), freed in OnDestroy.
    private readonly List<Material> owned = new List<Material>();

    private Transform carousel, carouselRing;
    private TextMesh carouselName;
    private readonly List<Transform> carouselTiles = new List<Transform>();

    private Transform statusRoot;
    private MeshRenderer statusPanel;
    private TextMesh statusText;

    private Transform toastRoot;
    private MeshRenderer toastPanel, toastBar;
    private TextMesh toastText;
    private int toastSerial = -1;

    private Transform Head
    {
        get
        {
            if (input != null && input.Locomotion != null && input.Locomotion.Head != null) return input.Locomotion.Head;
            Camera main = Camera.main;
            return main != null ? main.transform : null;
        }
    }

    public void Build(XRControllerInput owner)
    {
        if (model != null) return;
        input = owner;
        left = owner.Left;
        side = left ? -1 : 1;
        startTime = Time.unscaledTime;
        // The builder's box stands in for a controller in the editor; the model replaces it.
        Transform box = transform.Find("Visual");
        if (box != null && box.GetComponent<Renderer>() != null) box.GetComponent<Renderer>().enabled = false;
        BuildModel();
        BuildTips();
        // Without a builder Label the A/X hint's own text takes its place.
        if (owner.Label == null)
            foreach (Tip tip in tips) if (tip.Control == Control.Primary) owner.Label = tip.Action;
        if (left) BuildStatusCard();
        else BuildToast();
        Refresh();
        // Headset only: photos, the viewfinder and the presenter's map never show the interface.
        // Not the hand itself: it carries held tools, the camera body and the torches.
        DemoUi.ToHeadsetUiLayer(model);
        foreach (Tip tip in tips) DemoUi.ToHeadsetUiLayer(tip.Root);
        DemoUi.ToHeadsetUiLayer(statusRoot);
        DemoUi.ToHeadsetUiLayer(toastRoot);
    }

    // ---------------------------------------------------------------- controller model

    private void BuildModel()
    {
        model = new GameObject("ControllerModel").transform;
        model.SetParent(transform, false);
        Material body = Own(DemoUi.ShadedMaterial(BodyColor)), face = Own(DemoUi.ShadedMaterial(FaceColor));
        float inner = -side;
        // The handle runs down and back from the head; the face plate tilts towards the thumb.
        Part("Grip", PrimitiveType.Capsule, model, new Vector3(0, -0.045f, -0.026f), new Vector3(29, 0, 0), new Vector3(0.034f, 0.05f, 0.038f), body);
        Part("Head", PrimitiveType.Cylinder, model, new Vector3(0, -0.002f, 0.004f), new Vector3(-12, 0, 0), new Vector3(0.058f, 0.011f, 0.052f), face);
        GameObject ring = new GameObject("TrackingRing");
        ring.transform.SetParent(model, false);
        ring.transform.localPosition = new Vector3(side * 0.01f, 0.02f, 0.014f);
        ring.transform.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(side * 0.3f, 0.5f, -0.8f).normalized);
        ring.AddComponent<MeshFilter>().sharedMesh = Torus();
        MeshRenderer ringRenderer = ring.AddComponent<MeshRenderer>();
        ringRenderer.sharedMaterial = body;
        ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Transform plate = new GameObject("Face").transform;
        plate.SetParent(model, false);
        plate.localPosition = new Vector3(0, 0.009f, 0.002f);
        plate.localRotation = Quaternion.Euler(-12, 0, 0);
        Part("StickBase", PrimitiveType.Cylinder, plate, new Vector3(inner * 0.012f, 0.001f, 0.006f), Vector3.zero, new Vector3(0.018f, 0.0015f, 0.018f), Own(DemoUi.ShadedMaterial(DemoUi.Accent * 0.7f)));
        Part("StickShaft", PrimitiveType.Cylinder, plate, new Vector3(inner * 0.012f, 0.005f, 0.006f), Vector3.zero, new Vector3(0.006f, 0.004f, 0.006f), face);
        buttons[Control.Stick] = Part("StickCap", PrimitiveType.Cylinder, plate, new Vector3(inner * 0.012f, 0.0095f, 0.006f), Vector3.zero, new Vector3(0.017f, 0.0022f, 0.017f), Button());
        buttons[Control.Primary] = Part(left ? "X" : "A", PrimitiveType.Cylinder, plate, new Vector3(side * 0.013f, 0.0015f, -0.007f), Vector3.zero, new Vector3(0.011f, 0.0018f, 0.011f), Button());
        buttons[Control.Secondary] = Part(left ? "Y" : "B", PrimitiveType.Cylinder, plate, new Vector3(side * 0.015f, 0.0015f, 0.01f), Vector3.zero, new Vector3(0.011f, 0.0018f, 0.011f), Button());
        Renderer system = Part(left ? "Menu" : "System", PrimitiveType.Cylinder, plate, new Vector3(side * 0.002f, 0.001f, -0.019f), Vector3.zero, new Vector3(0.006f, 0.0014f, 0.006f), Button());
        if (left) buttons[Control.Menu] = system;
        buttons[Control.Trigger] = Part("Trigger", PrimitiveType.Cube, model, new Vector3(0, -0.019f, 0.026f), new Vector3(25, 0, 0), new Vector3(0.012f, 0.007f, 0.02f), Button());
        buttons[Control.Grip] = Part("GripButton", PrimitiveType.Cube, model, new Vector3(inner * 0.016f, -0.042f, -0.024f), new Vector3(29, 0, 0), new Vector3(0.006f, 0.032f, 0.018f), Button());
    }

    private Material Button() => Own(DemoUi.ShadedMaterial(ButtonColor));

    private Material Own(Material material)
    {
        if (material != null) owned.Add(material);
        return material;
    }

    private MeshRenderer Own(MeshRenderer renderer)
    {
        Own(renderer.sharedMaterial);
        return renderer;
    }

    private static Renderer Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 euler, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Collider blocker = part.GetComponent<Collider>();
        if (blocker != null) { blocker.enabled = false; Destroy(blocker); } // Destroy lands at frame end; grabs stay unobstructed now.
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(euler);
        part.transform.localScale = scale;
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    // Tracking ring: a thin torus in the XZ plane.
    private static Mesh Torus()
    {
        if (torus != null) return torus;
        const int major = 36, minor = 8;
        const float radius = 0.04f, tube = 0.0045f;
        Vector3[] vertices = new Vector3[(major + 1) * (minor + 1)];
        Vector3[] normals = new Vector3[vertices.Length];
        int[] triangles = new int[major * minor * 6];
        for (int i = 0; i <= major; i++)
        {
            float a = i * Mathf.PI * 2 / major;
            Vector3 centre = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            for (int j = 0; j <= minor; j++)
            {
                float b = j * Mathf.PI * 2 / minor;
                Vector3 n = centre * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                vertices[i * (minor + 1) + j] = centre * radius + n * tube;
                normals[i * (minor + 1) + j] = n;
            }
        }
        int t = 0;
        for (int i = 0; i < major; i++)
        for (int j = 0; j < minor; j++)
        {
            int a = i * (minor + 1) + j, b = (i + 1) * (minor + 1) + j;
            triangles[t++] = a; triangles[t++] = a + 1; triangles[t++] = b;
            triangles[t++] = b; triangles[t++] = a + 1; triangles[t++] = b + 1;
        }
        torus = new Mesh { name = "ControllerRing", vertices = vertices, normals = normals, triangles = triangles, hideFlags = HideFlags.DontSave };
        torus.RecalculateBounds();
        return torus;
    }

    // ---------------------------------------------------------------- tooltips

    private void BuildTips()
    {
        string face = left ? "X" : "A", back = left ? "Y" : "B";
        string stick = (input.Teleports ? "Teleport" : input.SnapTurns ? "Turn" : "Stick")
            + (input.Lighting != null ? "  ·  click: flashlight" : input.UVLighting != null ? "  ·  click: UV torch" : "");
        // Face buttons and trigger in a column on the outer side, stick, menu and grip on the inner side.
        float outer = side, innerSide = -side;
        AddTip(Control.Secondary, back, "Remove / next tool", new Vector3(outer * 0.085f, 0.07f, 0.01f), outer > 0, null);
        AddTip(Control.Primary, face, null, new Vector3(outer * 0.085f, 0.044f, 0f), outer > 0, input.Label);
        AddTip(Control.Trigger, "Trigger", left ? "Hold: tool wheel (empty hand)" : "Use: photo, lamp, tape end", new Vector3(outer * 0.08f, -0.005f, 0.04f), outer > 0, null);
        AddTip(Control.Stick, "Stick", stick, new Vector3(innerSide * 0.085f, 0.06f, 0.01f), innerSide > 0, null);
        if (left) AddTip(Control.Menu, "Menu", "Hide hints", new Vector3(innerSide * 0.085f, 0.034f, -0.01f), innerSide > 0, null);
        AddTip(Control.Grip, "Grip", "Grab / drop", new Vector3(innerSide * 0.08f, -0.03f, -0.02f), innerSide > 0, null);
    }

    private void AddTip(Control control, string key, string action, Vector3 localPosition, bool extendRight, TextMesh existing)
    {
        Tip tip = new Tip { Control = control, ExtendRight = extendRight };
        tip.Anchor = buttons.TryGetValue(control, out Renderer button) ? button.transform : model;
        tip.Root = new GameObject("Hint_" + control).transform;
        tip.Root.SetParent(transform, false);
        tip.Root.localPosition = localPosition;
        tip.Panel = Own(DemoUi.WorldPanel("Panel", tip.Root, Vector3.zero, 0.05f, PanelHeight, PanelHeight / 2, PanelColor));
        tip.KeyPanel = Own(DemoUi.WorldPanel("KeyPanel", tip.Root, Vector3.zero, KeyHeight, KeyHeight, 0.003f, DemoUi.Accent));
        tip.KeyPanel.sharedMaterial.renderQueue = 3010;
        tip.Key = DemoUi.WorldText("Key", tip.Root, Vector3.zero, key, TextHeight * 0.85f, KeyInk, TextAnchor.MiddleCenter, DemoUi.SemiBold);
        if (existing != null)
        {
            existing.transform.SetParent(tip.Root, false);
            existing.transform.localRotation = Quaternion.identity;
            existing.transform.localScale = Vector3.one;
            DemoUi.Configure(existing, DemoUi.Medium, TextHeight, DemoUi.Ink, TextAnchor.MiddleLeft);
            tip.Action = existing;
        }
        else tip.Action = DemoUi.WorldText("Action", tip.Root, Vector3.zero, action ?? "", TextHeight, DemoUi.Ink, TextAnchor.MiddleLeft);
        tip.KeyRenderer = tip.Key.GetComponent<Renderer>();
        tip.ActionRenderer = tip.Action.GetComponent<Renderer>();
        GameObject leader = new GameObject("Leader");
        leader.transform.SetParent(tip.Root, false);
        tip.Line = leader.AddComponent<LineRenderer>();
        // In the tip's own space, so the line rides rigidly with the hand when the pose is
        // refreshed just before rendering (XRControllerInput.LatePose).
        tip.Line.useWorldSpace = false;
        tip.Line.positionCount = 2;
        tip.Line.startWidth = tip.Line.endWidth = 0.0012f;
        tip.Line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tip.Line.receiveShadows = false;
        tip.Line.sharedMaterial = Own(DemoUi.UnlitMaterial(Color.white));
        tips.Add(tip);
        LayoutTip(tip);
    }

    // Panel sized to its text: [key chip][action], growing away from the controller.
    private static void LayoutTip(Tip tip)
    {
        float keyWidth = Mathf.Max(KeyHeight, DemoUi.TextWidth(tip.Key.font, tip.Key.text, TextHeight * 0.85f) + 0.008f);
        float actionWidth = DemoUi.TextWidth(tip.Action.font, tip.Action.text, TextHeight);
        float width = Pad + keyWidth + Gap + actionWidth + Pad + 0.002f;
        float centre = (tip.ExtendRight ? 1 : -1) * width / 2;
        float leftEdge = centre - width / 2;
        tip.Panel.GetComponent<MeshFilter>().sharedMesh = DemoUi.RoundedQuad(width, PanelHeight, PanelHeight / 2);
        tip.Panel.transform.localPosition = new Vector3(centre, 0, 0.0006f);
        tip.KeyPanel.GetComponent<MeshFilter>().sharedMesh = DemoUi.RoundedQuad(keyWidth, KeyHeight, 0.004f);
        tip.KeyPanel.transform.localPosition = new Vector3(leftEdge + Pad + keyWidth / 2, 0, 0.0003f);
        tip.Key.transform.localPosition = new Vector3(leftEdge + Pad + keyWidth / 2, 0, 0);
        tip.Action.transform.localPosition = new Vector3(leftEdge + Pad + keyWidth + Gap, 0, 0);
    }

    // The A/X hint changed (XRControllerInput writes the Label): re-fit its panel.
    public void Refresh()
    {
        foreach (Tip tip in tips) if (tip.Control == Control.Primary) LayoutTip(tip);
    }

    // The menu button's switch for both hands; renderers follow it at once.
    public void ShowHints(bool visible)
    {
        hintsShown = visible;
        appliedTipAlpha = -1;        // the next LateUpdate re-applies alpha and visibility
        tipsRendering = !visible;    // force the write
        SetTipsRendering(visible);
    }

    // Faded-out hints stop rendering, as the wrist card does (55 transparent renderers otherwise).
    private void SetTipsRendering(bool on)
    {
        if (on == tipsRendering) return;
        tipsRendering = on;
        foreach (Tip tip in tips)
        {
            tip.Panel.enabled = tip.KeyPanel.enabled = tip.Line.enabled = on;
            tip.KeyRenderer.enabled = tip.ActionRenderer.enabled = on;
        }
    }

    private void ApplyTipAlpha(float alpha)
    {
        appliedTipAlpha = alpha;
        bool visible = hintsShown && alpha > 0.001f;
        SetTipsRendering(visible);
        if (!visible) return;
        foreach (Tip tip in tips)
        {
            bool pressed = Pressed(tip.Control);
            tip.Panel.sharedMaterial.color = DemoUi.WithAlpha(PanelColor, PanelColor.a * alpha);
            tip.KeyPanel.sharedMaterial.color = DemoUi.WithAlpha(pressed ? Color.white : DemoUi.Accent, alpha);
            tip.Key.color = DemoUi.WithAlpha(KeyInk, alpha);
            tip.Action.color = DemoUi.WithAlpha(DemoUi.Ink, alpha);
            tip.Line.startColor = tip.Line.endColor = DemoUi.WithAlpha(LineColor, LineColor.a * alpha);
        }
    }

    private bool Pressed(Control control)
    {
        if (input == null) return false;
        switch (control)
        {
            case Control.Trigger: return input.TriggerHeld;
            case Control.Grip: return input.GripHeld;
            case Control.Primary: return input.PrimaryHeld;
            case Control.Secondary: return input.SecondaryHeld;
            case Control.Stick: return input.StickHeld;
            default: return input.MenuHeld;
        }
    }

    // ---------------------------------------------------------------- carousel

    private void BuildCarousel()
    {
        carousel = new GameObject("ToolCarousel").transform;
        carousel.SetParent(transform, false);
        carousel.localPosition = new Vector3(0, 0.125f, 0.02f);
        Own(DemoUi.WorldPanel("Panel", carousel, new Vector3(0, -0.007f, 0.0012f), 0.205f, 0.08f, 0.016f, PanelColor));
        carouselRing = Own(DemoUi.WorldPanel("Selection", carousel, Vector3.zero, 0.037f, 0.037f, 0.0185f, DemoUi.Accent)).transform;
        carouselRing.GetComponent<MeshRenderer>().sharedMaterial.renderQueue = 3005;
        int kinds = System.Enum.GetValues(typeof(DemoToolKind)).Length;
        for (int i = 0; i < kinds; i++)
        {
            DemoToolKind kind = (DemoToolKind)i;
            Transform tile = Own(DemoUi.WorldPanel("Tile_" + kind, carousel, new Vector3((i - (kinds - 1) / 2f) * 0.032f, 0.012f, 0.0006f),
                0.026f, 0.026f, 0.013f, DemoUi.Card)).transform;
            tile.GetComponent<MeshRenderer>().sharedMaterial.renderQueue = 3010;
            GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Quad);
            icon.name = "Icon";
            Collider blocker = icon.GetComponent<Collider>();
            if (blocker != null) { blocker.enabled = false; Destroy(blocker); }
            icon.transform.SetParent(tile, false);
            icon.transform.localPosition = new Vector3(0, 0, -0.0003f);
            icon.transform.localScale = new Vector3(0.017f, 0.017f, 1);
            MeshRenderer iconRenderer = icon.GetComponent<MeshRenderer>();
            iconRenderer.sharedMaterial = Own(DemoUi.IconMaterial(DemoUi.IconFor(kind), DemoDesktopPanel.KindColor(kind)));
            iconRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            iconRenderer.receiveShadows = false;
            carouselTiles.Add(tile);
        }
        carouselName = DemoUi.WorldText("Name", carousel, new Vector3(0, -0.017f, 0), "", 0.013f, DemoUi.Ink, TextAnchor.MiddleCenter, DemoUi.SemiBold);
        DemoUi.WorldText("Keys", carousel, new Vector3(0, -0.033f, 0), (left ? "X" : "A") + ": take one     " + (left ? "Y" : "B") + ": next",
            0.0085f, DemoUi.InkDim, TextAnchor.MiddleCenter);
        DemoUi.ToHeadsetUiLayer(carousel); // built on the first B/Y press, after Build
    }

    public void ShowCarousel(DemoToolKind kind)
    {
        if (carousel == null) BuildCarousel();
        carousel.gameObject.SetActive(true);
        carouselUntil = Time.unscaledTime + CarouselSeconds;
        for (int i = 0; i < carouselTiles.Count; i++)
            carouselTiles[i].localScale = i == (int)kind ? Vector3.one * 1.18f : Vector3.one * 0.9f;
        Vector3 at = carouselTiles[(int)kind].localPosition;
        carouselRing.localPosition = new Vector3(at.x, at.y, 0.0009f);
        carouselName.text = DeployedTool.Title(kind);
        if (Head != null) DemoUi.FaceViewer(carousel, Head.position);
    }

    // ---------------------------------------------------------------- wrist status (left)

    private void BuildStatusCard()
    {
        statusRoot = new GameObject("WristStatus").transform;
        statusRoot.SetParent(transform, false);
        statusRoot.localPosition = new Vector3(0, 0.04f, -0.1f);
        statusPanel = Own(DemoUi.WorldPanel("Panel", statusRoot, new Vector3(0, 0, 0.001f), 0.12f, 0.06f, 0.01f, PanelColor));
        statusText = DemoUi.WorldText("Status", statusRoot, Vector3.zero, "", 0.0085f, DemoUi.Ink, TextAnchor.MiddleLeft);
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (statusText == null) return;
        ToolStation station = input != null && input.Hand != null ? input.Hand.Station : null;
        EvidenceCamera camera = input != null && input.Hand != null ? input.Hand.EvidenceCamera : null;
        DemoLighting lighting = input != null ? (input.Lighting != null ? input.Lighting : input.UVLighting) : null;
        string accent = DemoUi.Hex(DemoUi.Accent);
        string text = "<color=" + accent + ">SCENE STATUS</color>";
        if (station != null)
        {
            int tools = station.DeployedCount;
            text += "\n" + tools + (tools == 1 ? " tool" : " tools") + "   ·   Next marker " + station.NextMarkerNumber;
        }
        string lights = lighting == null ? "" : lighting.FlashlightOn ? "Flashlight on" : lighting.UVTorchOn ? "UV torch on" : "Torches off";
        if (camera != null || lights.Length > 0)
            text += "\n" + (camera != null ? camera.PhotoCount + (camera.PhotoCount == 1 ? " photo" : " photos") : "")
                + (camera != null && lights.Length > 0 ? "   ·   " : "") + lights;
        if (station != null && station.PendingPrompt != null) text += "\n<color=" + accent + ">" + station.PendingPrompt + "</color>";
        if (statusText.text == text) return;
        statusText.text = text;
        int lines = text.Split('\n').Length;
        float width = DemoUi.TextWidth(statusText.font, text, 0.0085f) + 0.016f;
        float height = lines * 0.0085f * 1.2f + 0.012f;
        statusPanel.GetComponent<MeshFilter>().sharedMesh = DemoUi.RoundedQuad(width, height, 0.008f);
        statusText.transform.localPosition = new Vector3(-width / 2 + 0.008f, 0, 0);
    }

    private void ApplyStatusAlpha(float alpha)
    {
        appliedStatusAlpha = alpha;
        statusPanel.sharedMaterial.color = DemoUi.WithAlpha(PanelColor, PanelColor.a * alpha);
        statusText.color = DemoUi.WithAlpha(DemoUi.Ink, alpha);
        bool visible = alpha > 0.001f;
        statusPanel.enabled = visible;
        statusText.GetComponent<Renderer>().enabled = visible;
    }

    // ---------------------------------------------------------------- toast (right)

    private void BuildToast()
    {
        // On the rig, beside the hands: a teleport or snap turn carries it along instead of
        // letting it sweep across the room to catch up.
        toastRoot = new GameObject("XRToast").transform;
        toastRoot.SetParent(transform.parent, false);
        toastPanel = Own(DemoUi.WorldPanel("Panel", toastRoot, new Vector3(0, 0, 0.002f), 0.3f, 0.06f, 0.016f, PanelColor));
        toastBar = Own(DemoUi.WorldPanel("Tone", toastRoot, Vector3.zero, 0.006f, 0.034f, 0.003f, DemoUi.Accent));
        toastBar.sharedMaterial.renderQueue = 3010;
        toastText = DemoUi.WorldText("Text", toastRoot, Vector3.zero, "", 0.026f, DemoUi.Ink, TextAnchor.MiddleLeft);
        toastRoot.gameObject.SetActive(false);
    }

    private void UpdateToast(Transform head, float now, float deltaTime)
    {
        if (toastRoot == null) return;
        DemoFeedback.Prune(now);
        float alpha = DemoFeedback.TryNewestForHeadset(out DemoFeedback.Message message) ? DemoFeedback.Opacity(message, now) : 0;
        if (alpha <= 0 || head == null) { toastRoot.gameObject.SetActive(false); return; }
        Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f) flat = Vector3.ProjectOnPlane(head.up, Vector3.up);
        Vector3 target = head.position + flat.normalized * 0.95f + Vector3.down * 0.3f;
        bool appearing = !toastRoot.gameObject.activeSelf;
        toastRoot.gameObject.SetActive(true);
        if (message.Serial != toastSerial)
        {
            toastSerial = message.Serial;
            toastText.text = message.Text;
            float width = DemoUi.TextWidth(toastText.font, message.Text, 0.026f) + 0.05f;
            toastPanel.GetComponent<MeshFilter>().sharedMesh = DemoUi.RoundedQuad(width, 0.06f, 0.016f);
            toastBar.transform.localPosition = new Vector3(-width / 2 + 0.016f, 0, 0.001f);
            toastText.transform.localPosition = new Vector3(-width / 2 + 0.03f, 0, 0);
            toastBar.sharedMaterial.color = DemoFeedback.ToneColor(message.Tone);
        }
        // Lazy follow: drifts after the view instead of being glued to it.
        toastRoot.position = appearing ? target : Vector3.Lerp(toastRoot.position, target, 1 - Mathf.Exp(-deltaTime * 4f));
        DemoUi.FaceViewer(toastRoot, head.position);
        toastPanel.sharedMaterial.color = DemoUi.WithAlpha(PanelColor, PanelColor.a * alpha);
        Color tone = toastBar.sharedMaterial.color;
        toastBar.sharedMaterial.color = DemoUi.WithAlpha(tone, alpha);
        // A TextMesh rebuilds on every colour change: only while fading.
        if (Mathf.Abs(toastText.color.a - alpha) > 0.02f || (alpha >= 1 && toastText.color.a < 1)) toastText.color = DemoUi.WithAlpha(DemoUi.Ink, alpha);
    }

    // ---------------------------------------------------------------- per frame

    private bool LookedAt(Vector3 point, Transform head)
    {
        if (head == null) return true;
        Vector3 to = point - head.position;
        return to.magnitude < LookDistance && Vector3.Angle(head.forward, to) < LookAngle;
    }

    private void LateUpdate()
    {
        if (input == null) return;
        Transform head = Head;
        float now = Time.unscaledTime, deltaTime = Time.unscaledDeltaTime;
        bool onboarding = now - startTime < OnboardingSeconds;

        foreach (KeyValuePair<Control, Renderer> button in buttons)
        {
            bool pressed = Pressed(button.Key);
            if (lit.TryGetValue(button.Key, out bool was) && was == pressed) continue;
            lit[button.Key] = pressed;
            button.Value.sharedMaterial.color = pressed ? DemoUi.Accent : ButtonColor;
            if (hintsShown && tipAlpha > 0) appliedTipAlpha = -1; // the key chips brighten with the button
        }

        float tipTarget = hintsShown && (onboarding || LookedAt(transform.position, head)) ? 1 : 0;
        tipAlpha = tipAlpha < 0 ? tipTarget : Mathf.MoveTowards(tipAlpha, tipTarget, deltaTime * 5f);
        if (Mathf.Abs(tipAlpha - appliedTipAlpha) > 0.02f || (tipAlpha != appliedTipAlpha && (tipAlpha == 0 || tipAlpha == 1)))
            ApplyTipAlpha(tipAlpha);
        if (head != null && hintsShown && tipAlpha > 0)
            foreach (Tip tip in tips)
            {
                DemoUi.FaceViewer(tip.Root, head.position);
                tip.Line.SetPosition(0, tip.Line.transform.InverseTransformPoint(tip.Anchor.position));
                tip.Line.SetPosition(1, tip.Line.transform.InverseTransformPoint(tip.Root.position));
            }

        if (carousel != null && carousel.gameObject.activeSelf)
        {
            if (now > carouselUntil) carousel.gameObject.SetActive(false);
            else if (head != null) DemoUi.FaceViewer(carousel, head.position);
        }

        if (statusRoot != null)
        {
            if (now >= nextStatus) { nextStatus = now + 0.25f; RefreshStatus(); }
            float target = onboarding || LookedAt(statusRoot.position, head) ? 1 : 0;
            statusAlpha = Mathf.MoveTowards(statusAlpha, target, deltaTime * 5f);
            if (Mathf.Abs(statusAlpha - appliedStatusAlpha) > 0.02f || (statusAlpha != appliedStatusAlpha && (statusAlpha == 0 || statusAlpha == 1)))
                ApplyStatusAlpha(statusAlpha);
            if (head != null && statusAlpha > 0) DemoUi.FaceViewer(statusRoot, head.position);
        }

        UpdateToast(head, now, deltaTime);
    }

    private void OnDisable()
    {
        if (toastRoot != null) toastRoot.gameObject.SetActive(false);
        if (carousel != null) carousel.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (toastRoot != null) Destroy(toastRoot.gameObject);
        foreach (Material material in owned) if (material != null) Destroy(material);
        owned.Clear();
    }
}
