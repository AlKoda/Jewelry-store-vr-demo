using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Interface pictograms; the first six follow DemoToolKind.
public enum UiIcon { Cone, Marker, TapePost, Scale, Measure, Lamp, Camera, Flashlight, UVTorch }

// The one visual language of the demo, shared by the desktop HUD and instructor
// panel (IMGUI), the start menu and the VR controller interface (world meshes and
// text): a dark glass palette with a brass accent that matches the store, the
// Barlow font family, anti-aliased rounded panels and keycap-style key hints.
// Everything is generated at runtime; no package, no texture asset is needed.
public static class DemoUi
{
    // ---------------------------------------------------------------- palette
    public static readonly Color Ink = new Color(0.94f, 0.95f, 0.97f);
    public static readonly Color InkDim = new Color(0.63f, 0.67f, 0.74f);
    public static readonly Color InkFaint = new Color(0.45f, 0.49f, 0.56f);
    public static readonly Color Glass = new Color(0.05f, 0.06f, 0.08f, 0.88f);
    public static readonly Color Card = new Color(0.11f, 0.125f, 0.155f, 0.96f);
    public static readonly Color CardHover = new Color(0.16f, 0.18f, 0.22f, 0.98f);
    public static readonly Color Line = new Color(1f, 1f, 1f, 0.09f);
    public static readonly Color Accent = new Color(0.97f, 0.74f, 0.30f);
    public static readonly Color AccentDark = new Color(0.30f, 0.21f, 0.07f, 0.98f);
    public static readonly Color Info = new Color(0.40f, 0.70f, 1f);
    public static readonly Color Success = new Color(0.40f, 0.86f, 0.58f);
    public static readonly Color Warning = new Color(1f, 0.42f, 0.38f);

    public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

    // ---------------------------------------------------------------- credits
    // The built-in credits line. NightStoreBuilder.Build writes Resources/DemoCredits from it:
    // this line, the night store's CC0 sources, then one attribution per CC BY or MIT asset
    // the build placed (DemoAssetLibrary.WriteCredits).
    public const string DefaultCredits =
        "Models: Kenney, KayKit (CC0); Khronos glTF samples (CC0 / CC BY 4.0); textures cgbookcase, HDRIs HDRI Haven (CC0); "
        + "Barlow font (SIL OFL 1.1). See ThirdParty licenses.";

    private static string credits;

    // The one credits text (the instructor panel's Settings tab, the start menu): Resources/
    // DemoCredits, loaded on first use and kept, or DefaultCredits when the project has no
    // night store build yet (or the file is empty).
    public static string Credits
    {
        get
        {
            if (credits != null) return credits;
            TextAsset file = Resources.Load<TextAsset>("DemoCredits");
            string text = file != null ? file.text.Trim() : "";
            credits = text.Length > 0 ? text : DefaultCredits;
            return credits;
        }
    }

    // ---------------------------------------------------------------- fonts
    private static DemoUiTheme theme;
    private static int themeSearchFrame = -1000;
    private static Font builtin;

    private static DemoUiTheme Theme
    {
        get
        {
            // Re-searched at once when the cached theme was destroyed (a scene load), and at most
            // once a second while no theme exists.
            if (theme == null && ((object)theme != null || Time.frameCount - themeSearchFrame > 60))
            {
                themeSearchFrame = Time.frameCount;
                theme = Object.FindFirstObjectByType<DemoUiTheme>();
            }
            return theme;
        }
    }

    private static Font Builtin
    {
        get
        {
            if (builtin == null) builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return builtin;
        }
    }

    public static Font Regular => Theme != null && Theme.Regular != null ? Theme.Regular : Builtin;
    public static Font Medium => Theme != null && Theme.Medium != null ? Theme.Medium : Regular;
    public static Font SemiBold => Theme != null && Theme.SemiBold != null ? Theme.SemiBold : Medium;

    // ---------------------------------------------------------------- textures
    // Keyed by value: Color is IEquatable<Color>, so lookups neither box nor allocate
    // (string keys formatted every colour on every IMGUI event).
    private static readonly Dictionary<(int, Color, Color, int), Texture2D> rounded = new Dictionary<(int, Color, Color, int), Texture2D>();
    private static readonly Dictionary<(int, int, Color), Texture2D> rings = new Dictionary<(int, int, Color), Texture2D>();
    private static readonly Texture2D[] icons = new Texture2D[System.Enum.GetValues(typeof(UiIcon)).Length];

    // Anti-aliased rounded rectangle, 9-sliced by GUIStyle.border = radius.
    public static Texture2D Rounded(int radius, Color fill, Color border, int borderWidth = 1)
    {
        var key = (radius, fill, border, borderWidth);
        if (rounded.TryGetValue(key, out Texture2D cached) && cached != null) return cached;
        int size = radius * 2 + 2;
        Texture2D texture = NewTexture(size, size);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float outside = Outside(x + 0.5f, y + 0.5f, size, size, radius);
            float shape = Mathf.Clamp01(0.5f - outside);
            float inner = borderWidth > 0 ? Mathf.Clamp01(0.5f - (outside + borderWidth)) : 1;
            Color c = Color.Lerp(border, fill, inner);
            c.a = Mathf.Lerp(border.a, fill.a, inner) * shape;
            pixels[y * size + x] = c;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        rounded[key] = texture;
        return texture;
    }

    // A filled disc with an optional ring (crosshair, map glyphs, status dots).
    public static Texture2D Disc(int radius, Color fill) => Rounded(radius, fill, fill, 0);

    public static Texture2D Ring(int radius, int thickness, Color color)
    {
        var key = (radius, thickness, color);
        if (rings.TryGetValue(key, out Texture2D cached) && cached != null) return cached;
        int size = radius * 2 + 2;
        Texture2D texture = NewTexture(size, size);
        Color[] pixels = new Color[size * size];
        float c = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
            float a = Mathf.Clamp01(0.5f - Mathf.Abs(d - (radius - thickness / 2f)) + thickness / 2f);
            Color p = color;
            p.a *= a;
            pixels[y * size + x] = p;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        rings[key] = texture;
        return texture;
    }

    // ---------------------------------------------------------------- icons
    // White 64 px pictograms, tinted with GUI.color or a material colour: the six tool
    // kinds first (DemoToolKind order), then the camera and the two torches. Drawn
    // from distance functions, so they stay crisp at any size up to 64 px.
    public static Texture2D Icon(UiIcon icon)
    {
        int slot = (int)icon;
        bool cacheable = slot >= 0 && slot < icons.Length;
        if (cacheable && icons[slot] != null) return icons[slot];
        const int size = 64;
        Texture2D texture = NewTexture(size, size);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = IconDistance(icon, new Vector2((x + 0.5f) / size, (y + 0.5f) / size)) * size;
            pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - d));
        }
        texture.SetPixels(pixels);
        texture.Apply();
        if (cacheable) icons[slot] = texture;
        return texture;
    }

    public static UiIcon IconFor(DemoToolKind kind) => (UiIcon)(int)kind;

    // Signed distance in icon units (0..1, y up); negative inside.
    private static float IconDistance(UiIcon icon, Vector2 p)
    {
        switch (icon)
        {
            case UiIcon.Cone:
                return Mathf.Max(Mathf.Min(Poly(p, V(0.26f, 0.2f), V(0.74f, 0.2f), V(0.5f, 0.88f)), RoundBox(p, V(0.5f, 0.17f), V(0.33f, 0.05f), 0.02f)),
                    -RoundBox(p, V(0.5f, 0.5f), V(0.3f, 0.045f), 0));
            case UiIcon.Marker:
                return Mathf.Max(Poly(p, V(0.18f, 0.18f), V(0.82f, 0.18f), V(0.64f, 0.8f), V(0.36f, 0.8f)),
                    -RoundBox(p, V(0.5f, 0.47f), V(0.04f, 0.15f), 0.01f));
            case UiIcon.TapePost:
                return Mathf.Min(Mathf.Min(RoundBox(p, V(0.36f, 0.15f), V(0.22f, 0.05f), 0.03f), RoundBox(p, V(0.36f, 0.5f), V(0.045f, 0.33f), 0.02f)),
                    Mathf.Min(Circle(p, V(0.36f, 0.84f), 0.075f), RoundBox(p, V(0.66f, 0.72f), V(0.3f, 0.04f), 0.01f)));
            case UiIcon.Scale:
            {
                float d = Mathf.Min(RoundBox(p, V(0.28f, 0.52f), V(0.08f, 0.34f), 0.02f), RoundBox(p, V(0.55f, 0.26f), V(0.35f, 0.08f), 0.02f));
                for (int i = 0; i < 3; i++)
                {
                    d = Mathf.Max(d, -RoundBox(p, V(0.35f, 0.46f + i * 0.13f), V(0.035f, 0.014f), 0));
                    d = Mathf.Max(d, -RoundBox(p, V(0.5f + i * 0.13f, 0.33f), V(0.014f, 0.035f), 0));
                }
                return d;
            }
            case UiIcon.Measure:
                return Mathf.Min(Mathf.Min(Ring(p, V(0.4f, 0.58f), 0.22f, 0.055f), Circle(p, V(0.4f, 0.58f), 0.07f)),
                    Mathf.Min(RoundBox(p, V(0.64f, 0.35f), V(0.26f, 0.032f), 0.01f), RoundBox(p, V(0.89f, 0.39f), V(0.028f, 0.075f), 0.01f)));
            case UiIcon.Lamp:
            {
                float legs = Mathf.Min(Segment(p, V(0.5f, 0.6f), V(0.2f, 0.1f), 0.035f),
                    Mathf.Min(Segment(p, V(0.5f, 0.6f), V(0.8f, 0.1f), 0.035f), Segment(p, V(0.5f, 0.6f), V(0.5f, 0.1f), 0.035f)));
                return Mathf.Min(legs, Mathf.Max(RoundBox(p, V(0.5f, 0.74f), V(0.22f, 0.13f), 0.05f), -Circle(p, V(0.5f, 0.74f), 0.07f)));
            }
            case UiIcon.Camera:
                return Mathf.Min(Mathf.Max(Mathf.Min(RoundBox(p, V(0.5f, 0.44f), V(0.38f, 0.25f), 0.07f), RoundBox(p, V(0.34f, 0.72f), V(0.12f, 0.07f), 0.03f)),
                    -Circle(p, V(0.5f, 0.44f), 0.16f)), Circle(p, V(0.5f, 0.44f), 0.085f));
            case UiIcon.Flashlight:
            case UiIcon.UVTorch:
            {
                float torch = Mathf.Min(RoundBox(p, V(0.36f, 0.5f), V(0.24f, 0.085f), 0.03f), Poly(p, V(0.58f, 0.36f), V(0.76f, 0.28f), V(0.76f, 0.72f), V(0.58f, 0.64f)));
                if (icon == UiIcon.UVTorch)
                    return Mathf.Min(torch, Mathf.Min(Circle(p, V(0.88f, 0.5f), 0.045f), Mathf.Min(Circle(p, V(0.86f, 0.74f), 0.038f), Circle(p, V(0.86f, 0.26f), 0.038f))));
                return Mathf.Min(torch, Mathf.Min(Segment(p, V(0.84f, 0.5f), V(0.96f, 0.5f), 0.026f),
                    Mathf.Min(Segment(p, V(0.83f, 0.66f), V(0.93f, 0.78f), 0.026f), Segment(p, V(0.83f, 0.34f), V(0.93f, 0.22f), 0.026f))));
            }
            default:
                return Circle(p, V(0.5f, 0.5f), 0.3f);
        }
    }

    private static Vector2 V(float x, float y) => new Vector2(x, y);
    private static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
    private static float Ring(Vector2 p, Vector2 c, float r, float halfWidth) => Mathf.Abs((p - c).magnitude - r) - halfWidth;

    private static float Segment(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude - r;
    }

    private static float RoundBox(Vector2 p, Vector2 c, Vector2 half, float round)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(round, round);
        return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - round;
    }

    // Convex polygon, counter-clockwise: the largest distance past any edge.
    private static float Poly(Vector2 p, params Vector2[] points)
    {
        float d = float.MinValue;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a = points[i], b = points[(i + 1) % points.Length];
            Vector2 normal = new Vector2(b.y - a.y, a.x - b.x).normalized;
            d = Mathf.Max(d, Vector2.Dot(p - a, normal));
        }
        return d;
    }

    // Signed distance (pixels) outside a rounded rectangle covering 0..w x 0..h.
    private static float Outside(float px, float py, float w, float h, float r)
    {
        float dx = Mathf.Max(Mathf.Max(r - px, px - (w - r)), 0);
        float dy = Mathf.Max(Mathf.Max(r - py, py - (h - r)), 0);
        return Mathf.Sqrt(dx * dx + dy * dy) - r;
    }

    private static Texture2D NewTexture(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };
    }

    public static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    public static void Box(Rect rect, int radius, Color fill, Color border)
    {
        // Drawn on Repaint only, so Layout and input events skip the style lookup.
        if (Event.current.type != EventType.Repaint) return;
        BoxStyle(radius, fill, border).Draw(rect, false, false, false, false);
    }

    private static readonly Dictionary<(int, Color, Color), GUIStyle> boxes = new Dictionary<(int, Color, Color), GUIStyle>();
    private static GUIStyle BoxStyle(int radius, Color fill, Color border)
    {
        var key = (radius, fill, border);
        if (boxes.TryGetValue(key, out GUIStyle style) && style.normal.background != null) return style;
        style = new GUIStyle { border = new RectOffset(radius, radius, radius, radius) };
        style.normal.background = Rounded(radius, fill, border);
        boxes[key] = style;
        return style;
    }

    // ---------------------------------------------------------------- IMGUI styles
    public sealed class Skin
    {
        public GUIStyle Panel, Card, Title, Heading, Body, Dim, Small, Button, ButtonAccent, ButtonDanger, ButtonGhost,
            Tab, TabActive, Keycap, HintText, Chip, ChipAccent, Slot, SlotSelected, SlotKey, SlotName, SlotCount, Toast, Badge, Center;
    }

    private static Skin skin;
    private static Font skinFont;

    // Built inside OnGUI (GUI.skin exists only there); rebuilt if the theme font changes.
    public static Skin Styles
    {
        get
        {
            if (skin != null && skinFont == Regular && skin.Panel.normal.background != null) return skin;
            skinFont = Regular;
            skin = Build();
            return skin;
        }
    }

    private static GUIStyle Text(Font font, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool wrap = false)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label) { font = font, fontSize = size, alignment = anchor, wordWrap = wrap, richText = true };
        s.normal.textColor = color;
        s.padding = new RectOffset(0, 0, 1, 1);
        s.margin = new RectOffset(0, 0, 2, 2);
        return s;
    }

    private static GUIStyle Button(Color fill, Color hover, Color press, Color text, Color border)
    {
        GUIStyle s = new GUIStyle(GUI.skin.button)
        {
            font = Medium, fontSize = 15, alignment = TextAnchor.MiddleCenter, richText = true,
            border = new RectOffset(8, 8, 8, 8), padding = new RectOffset(14, 14, 9, 9), margin = new RectOffset(3, 3, 3, 3),
        };
        s.normal.background = Rounded(8, fill, border);
        s.hover.background = Rounded(8, hover, border);
        s.active.background = Rounded(8, press, border);
        s.focused.background = s.normal.background;
        s.onNormal.background = s.active.background;
        s.normal.textColor = s.hover.textColor = s.active.textColor = s.focused.textColor = s.onNormal.textColor = text;
        return s;
    }

    private static Skin Build()
    {
        Skin s = new Skin();
        s.Panel = new GUIStyle { border = new RectOffset(14, 14, 14, 14), padding = new RectOffset(18, 18, 16, 16) };
        s.Panel.normal.background = Rounded(14, Glass, Line);
        s.Card = new GUIStyle { border = new RectOffset(10, 10, 10, 10), padding = new RectOffset(14, 14, 12, 12), margin = new RectOffset(0, 0, 6, 6) };
        s.Card.normal.background = Rounded(10, Card, Line);
        s.Title = Text(SemiBold, 22, Ink);
        s.Heading = Text(SemiBold, 13, Accent);
        s.Heading.margin = new RectOffset(0, 0, 10, 4);
        s.Body = Text(Regular, 15, Ink, TextAnchor.MiddleLeft, true);
        s.Dim = Text(Regular, 13, InkDim, TextAnchor.MiddleLeft, true);
        s.Small = Text(Regular, 12, InkFaint, TextAnchor.MiddleLeft, true);
        s.Center = Text(Medium, 15, Ink, TextAnchor.MiddleCenter, true);
        s.Button = Button(new Color(0.17f, 0.19f, 0.23f, 0.98f), new Color(0.23f, 0.25f, 0.30f, 1f), AccentDark, Ink, Line);
        s.ButtonAccent = Button(Accent, Color.Lerp(Accent, Color.white, 0.18f), Color.Lerp(Accent, Color.black, 0.25f), new Color(0.08f, 0.06f, 0.02f), new Color(0, 0, 0, 0));
        s.ButtonDanger = Button(new Color(0.42f, 0.12f, 0.11f, 0.98f), new Color(0.55f, 0.16f, 0.14f, 1f), new Color(0.30f, 0.08f, 0.07f, 1f), Ink, new Color(1f, 0.42f, 0.38f, 0.5f));
        s.ButtonGhost = Button(new Color(1, 1, 1, 0.0f), new Color(1, 1, 1, 0.08f), new Color(1, 1, 1, 0.14f), InkDim, new Color(0, 0, 0, 0));
        s.Tab = Button(new Color(1, 1, 1, 0.0f), new Color(1, 1, 1, 0.07f), new Color(1, 1, 1, 0.12f), InkDim, new Color(0, 0, 0, 0));
        s.Tab.fontSize = 14;
        s.Tab.padding = new RectOffset(8, 8, 8, 8);
        s.TabActive = Button(AccentDark, AccentDark, AccentDark, Accent, new Color(Accent.r, Accent.g, Accent.b, 0.45f));
        s.TabActive.fontSize = 14;
        s.TabActive.padding = s.Tab.padding;
        s.Keycap = new GUIStyle(GUI.skin.label)
        {
            font = SemiBold, fontSize = 13, alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(5, 5, 5, 5), padding = new RectOffset(7, 7, 3, 4), margin = new RectOffset(2, 2, 0, 0),
        };
        s.Keycap.normal.background = Rounded(5, new Color(0.22f, 0.24f, 0.29f, 0.98f), new Color(1, 1, 1, 0.28f));
        s.Keycap.normal.textColor = Ink;
        s.HintText = Text(Regular, 14, InkDim);
        s.Chip = new GUIStyle(GUI.skin.label)
        {
            font = Medium, fontSize = 13, alignment = TextAnchor.MiddleCenter, richText = true,
            border = new RectOffset(10, 10, 10, 10), padding = new RectOffset(11, 11, 5, 6), margin = new RectOffset(0, 6, 0, 0),
        };
        s.Chip.normal.background = Rounded(10, new Color(0.05f, 0.06f, 0.08f, 0.78f), Line);
        s.Chip.normal.textColor = Ink;
        s.ChipAccent = new GUIStyle(s.Chip);
        s.ChipAccent.normal.background = Rounded(10, AccentDark, new Color(Accent.r, Accent.g, Accent.b, 0.6f));
        s.ChipAccent.normal.textColor = Accent;
        s.Slot = new GUIStyle(GUI.skin.button) { border = new RectOffset(10, 10, 10, 10), padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0) };
        s.Slot.normal.background = Rounded(10, new Color(0.06f, 0.07f, 0.09f, 0.82f), Line);
        s.Slot.hover.background = Rounded(10, new Color(0.13f, 0.14f, 0.18f, 0.92f), new Color(1, 1, 1, 0.22f));
        s.Slot.active.background = Rounded(10, AccentDark, Accent);
        s.SlotSelected = new GUIStyle(s.Slot);
        s.SlotSelected.normal.background = s.SlotSelected.hover.background = Rounded(10, AccentDark, Accent, 2);
        s.SlotKey = Text(SemiBold, 12, InkDim, TextAnchor.UpperLeft);
        s.SlotName = Text(Medium, 13, Ink, TextAnchor.LowerCenter);
        s.SlotCount = Text(SemiBold, 12, Accent, TextAnchor.UpperRight);
        s.Toast = new GUIStyle { border = new RectOffset(10, 10, 10, 10), padding = new RectOffset(16, 14, 9, 10) };
        s.Toast.normal.background = Rounded(10, new Color(0.06f, 0.07f, 0.09f, 0.94f), Line);
        s.Badge = new GUIStyle(s.Chip) { fontSize = 11, padding = new RectOffset(7, 7, 2, 3) };
        s.Badge.normal.background = Rounded(8, Warning, Warning);
        s.Badge.normal.textColor = Color.white;

        // The shared default skin's scrollbar and slider, restyled once for every panel.
        GUI.skin.verticalScrollbar.normal.background = Rounded(3, new Color(1, 1, 1, 0.05f), new Color(0, 0, 0, 0), 0);
        GUI.skin.verticalScrollbar.border = new RectOffset(3, 3, 3, 3);
        GUI.skin.verticalScrollbar.fixedWidth = 6;
        GUI.skin.verticalScrollbar.margin = new RectOffset(6, 0, 0, 0);
        GUI.skin.verticalScrollbarThumb.normal.background = Rounded(3, new Color(1, 1, 1, 0.28f), new Color(0, 0, 0, 0), 0);
        GUI.skin.verticalScrollbarThumb.border = new RectOffset(3, 3, 3, 3);
        GUI.skin.verticalScrollbarThumb.fixedWidth = 6;
        GUI.skin.horizontalSlider.normal.background = SliderTrack();
        GUI.skin.horizontalSlider.border = new RectOffset(6, 6, 0, 0);
        GUI.skin.horizontalSlider.fixedHeight = 16;
        GUI.skin.horizontalSliderThumb.normal.background = GUI.skin.horizontalSliderThumb.hover.background =
            GUI.skin.horizontalSliderThumb.active.background = Disc(8, Accent);
        GUI.skin.horizontalSliderThumb.fixedWidth = GUI.skin.horizontalSliderThumb.fixedHeight = 16;
        return s;
    }

    // A 6 px bar centred in a 16 px tall texture, so the slider thumb sits on its middle.
    // Made once: the skin is rebuilt whenever the theme font changes.
    private static Texture2D sliderTrack;
    private static Texture2D SliderTrack()
    {
        if (sliderTrack != null) return sliderTrack;
        Texture2D texture = NewTexture(14, 16);
        Color[] pixels = new Color[14 * 16];
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 14; x++)
        {
            float outside = Outside(x + 0.5f, y - 5 + 0.5f, 14, 6, 3);
            Color c = new Color(1, 1, 1, 0.16f);
            c.a *= y >= 5 && y < 11 ? Mathf.Clamp01(0.5f - outside) : 0;
            pixels[y * 14 + x] = c;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        sliderTrack = texture;
        return texture;
    }

    // ---------------------------------------------------------------- cached text
    // One immutable GUIContent per distinct string: stable labels measure and draw without allocating.
    private static readonly Dictionary<string, GUIContent> contents = new Dictionary<string, GUIContent>(256);
    public static GUIContent Content(string text)
    {
        if (text == null) text = string.Empty;
        if (contents.TryGetValue(text, out GUIContent content)) return content;
        if (contents.Count >= 512) contents.Clear(); // toasts and counters stay few; bounded anyway
        contents[text] = content = new GUIContent(text);
        return content;
    }

    private static readonly Dictionary<string, string[]> keyParts = new Dictionary<string, string[]>();
    private static string[] KeyParts(string keys)
    {
        if (!keyParts.TryGetValue(keys, out string[] parts)) keyParts[keys] = parts = keys.Split('/');
        return parts;
    }

    // ---------------------------------------------------------------- key hints
    // Pairs of key and action, e.g. ("LMB", "Place", "Q/E", "Rotate"); "Q/E" draws two caps.
    public static float HintWidth(IReadOnlyList<string> pairs)
    {
        Skin s = Styles;
        float width = 0;
        for (int i = 0; i + 1 < pairs.Count; i += 2)
        {
            string[] keys = KeyParts(pairs[i]);
            for (int k = 0; k < keys.Length; k++) width += Mathf.Max(s.Keycap.CalcSize(Content(keys[k])).x, 24) + 4;
            width += s.HintText.CalcSize(Content(pairs[i + 1])).x + 18;
        }
        return Mathf.Max(0, width - 12);
    }

    // Draws the hints left to right from rect.x, vertically centred in rect.
    public static void DrawHints(Rect rect, IReadOnlyList<string> pairs)
    {
        Skin s = Styles;
        float x = rect.x;
        for (int i = 0; i + 1 < pairs.Count; i += 2)
        {
            string[] keys = KeyParts(pairs[i]);
            for (int k = 0; k < keys.Length; k++)
            {
                GUIContent key = Content(keys[k]);
                Vector2 size = s.Keycap.CalcSize(key);
                size.x = Mathf.Max(size.x, 24);
                GUI.Label(new Rect(x, rect.y + (rect.height - size.y) / 2, size.x, size.y), key, s.Keycap);
                x += size.x + 4;
            }
            GUIContent action = Content(pairs[i + 1]);
            Vector2 label = s.HintText.CalcSize(action);
            GUI.Label(new Rect(x + 2, rect.y, label.x, rect.height), action, s.HintText);
            x += label.x + 18;
        }
    }

    // GUILayout row of key hints (help screen, settings).
    public static void HintRow(string keys, string action, float keyColumn = 150)
    {
        Skin s = Styles;
        GUILayout.BeginHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(keyColumn));
        foreach (string key in KeyParts(keys)) GUILayout.Label(Content(key), s.Keycap, GUILayout.MinWidth(24));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Label(Content(action), s.Body);
        GUILayout.EndHorizontal();
    }

    // On/off switch drawn as a pill; returns the new value.
    public static bool Switch(string label, bool value)
    {
        Skin s = Styles;
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, s.Body);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(value ? "ON" : "OFF", value ? s.ButtonAccent : s.Button, GUILayout.Width(64))) value = !value;
        GUILayout.EndHorizontal();
        return value;
    }

    // ---------------------------------------------------------------- world (VR) elements
    private static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    private static readonly Dictionary<Font, Material> textMaterials = new Dictionary<Font, Material>();

    // Flat rounded rectangle in the XY plane centred on the origin, facing -Z like a TextMesh.
    public static Mesh RoundedQuad(float width, float height, float radius, int cornerSegments = 5)
    {
        // Millimetre steps, so panels fitted to changing text share a bounded set of meshes.
        width = Mathf.Max(0.001f, Mathf.Round(width * 1000) / 1000);
        height = Mathf.Max(0.001f, Mathf.Round(height * 1000) / 1000);
        radius = Mathf.Round(radius * 1000) / 1000;
        string key = "q" + width + "x" + height + "r" + radius + "s" + cornerSegments;
        if (meshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;
        radius = Mathf.Min(radius, Mathf.Min(width, height) / 2);
        List<Vector3> outline = new List<Vector3>();
        Vector2[] centres =
        {
            new Vector2(width / 2 - radius, height / 2 - radius), new Vector2(width / 2 - radius, -height / 2 + radius),
            new Vector2(-width / 2 + radius, -height / 2 + radius), new Vector2(-width / 2 + radius, height / 2 - radius),
        };
        // Clockwise as seen from -Z: top-right corner first, angles decreasing.
        for (int c = 0; c < 4; c++)
            for (int i = 0; i <= cornerSegments; i++)
            {
                float a = Mathf.Deg2Rad * (90 - c * 90 - i * 90f / cornerSegments);
                outline.Add(new Vector3(centres[c].x + Mathf.Cos(a) * radius, centres[c].y + Mathf.Sin(a) * radius, 0));
            }
        Vector3[] vertices = new Vector3[outline.Count + 1];
        vertices[0] = Vector3.zero;
        outline.CopyTo(vertices, 1);
        int[] triangles = new int[outline.Count * 3];
        for (int i = 0; i < outline.Count; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % outline.Count + 1;
        }
        Mesh mesh = new Mesh { name = "UiRoundedQuad", vertices = vertices, triangles = triangles, hideFlags = HideFlags.DontSave };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshes[key] = mesh;
        return mesh;
    }

    // Flat ring in the XZ plane (teleport marker) or, rotated, a controller tracking ring.
    public static Mesh FlatRing(float radius, float width, int segments = 48)
    {
        string key = "ring" + radius + "w" + width + "s" + segments;
        if (meshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;
        Vector3[] vertices = new Vector3[segments * 2];
        int[] triangles = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2 / segments;
            Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            vertices[i * 2] = d * (radius - width / 2);
            vertices[i * 2 + 1] = d * (radius + width / 2);
            int n = (i + 1) % segments;
            triangles[i * 6] = i * 2; triangles[i * 6 + 1] = n * 2; triangles[i * 6 + 2] = i * 2 + 1;
            triangles[i * 6 + 3] = i * 2 + 1; triangles[i * 6 + 4] = n * 2; triangles[i * 6 + 5] = n * 2 + 1;
        }
        Mesh mesh = new Mesh { name = "UiRing", vertices = vertices, triangles = triangles, hideFlags = HideFlags.DontSave };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshes[key] = mesh;
        return mesh;
    }

    // Layer 5 (Unity's built-in "UI" layer, unused here: no uGUI) carries the headset interface: controller
    // models, hints, wrist card, toast, carousel, teleport arc and ring, comfort fade. The head and desktop
    // cameras draw it; the photo camera (photos and viewfinder) and the overview map cull it.
    public const int HeadsetUiLayer = 5;
    public static void ToHeadsetUiLayer(Transform root)
    {
        if (root == null) return;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = HeadsetUiLayer;
    }

    // Unlit, alpha-blended, double-sided material for world panels, lines and markers.
    public static Material UnlitMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        return new Material(shader) { color = color, hideFlags = HideFlags.DontSave };
    }

    public static MeshRenderer WorldPanel(string name, Transform parent, Vector3 localPosition, float width, float height, float radius, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        panel.transform.localPosition = localPosition;
        panel.AddComponent<MeshFilter>().sharedMesh = RoundedQuad(width, height, radius);
        MeshRenderer renderer = panel.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = UnlitMaterial(color);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    // TextMesh in the theme font on the depth-tested world-text shader (labels do not
    // show through hands or furniture), drawn after the panels behind it.
    public static TextMesh WorldText(string name, Transform parent, Vector3 localPosition, string text, float lineHeight,
        Color color, TextAnchor anchor, Font font = null)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        TextMesh mesh = go.AddComponent<TextMesh>();
        Configure(mesh, font != null ? font : Medium, lineHeight, color, anchor);
        mesh.text = text;
        return mesh;
    }

    public static void Configure(TextMesh mesh, Font font, float lineHeight, Color color, TextAnchor anchor)
    {
        mesh.font = font;
        mesh.fontSize = 64;
        // A TextMesh line is about fontSize * characterSize / 10 world units tall.
        mesh.characterSize = lineHeight / 6.4f;
        mesh.anchor = anchor;
        mesh.alignment = anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.UpperLeft || anchor == TextAnchor.LowerLeft
            ? TextAlignment.Left : anchor == TextAnchor.MiddleRight || anchor == TextAnchor.UpperRight || anchor == TextAnchor.LowerRight
            ? TextAlignment.Right : TextAlignment.Center;
        mesh.richText = true;
        mesh.color = color;
        MeshRenderer renderer = mesh.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        Material material = TextMaterial(font);
        if (material == null) { renderer.sharedMaterial = font.material; return; }
        DepthTestedLabel label = mesh.GetComponent<DepthTestedLabel>();
        if (label == null) label = mesh.gameObject.AddComponent<DepthTestedLabel>();
        label.LabelMaterial = material;
        label.Refresh();
    }

    private static Material TextMaterial(Font font)
    {
        if (font == null) return null;
        if (textMaterials.TryGetValue(font, out Material cached) && cached != null) return cached;
        Shader shader = Shader.Find("CrimeScene/WorldText");
        if (shader == null) return null;
        Material material = new Material(shader) { hideFlags = HideFlags.DontSave, renderQueue = 3100 };
        material.mainTexture = font.material != null ? font.material.mainTexture : null;
        textMaterials[font] = material;
        return material;
    }

    // Width in world units of a TextMesh set up by Configure with this line height (rich
    // text tags ignored), so panels can be sized to their text before it is rendered.
    public static float TextWidth(Font font, string text, float lineHeight)
    {
        if (font == null || string.IsNullOrEmpty(text)) return 0;
        string plain = StripTags(text);
        font.RequestCharactersInTexture(plain, 64);
        float widest = 0, line = 0;
        foreach (char c in plain)
        {
            if (c == '\n') { widest = Mathf.Max(widest, line); line = 0; continue; }
            line += font.GetCharacterInfo(c, out CharacterInfo info, 64) ? info.advance : 32;
        }
        // One font pixel at size 64 is characterSize / 10 = lineHeight / 64 world units.
        return Mathf.Max(widest, line) * lineHeight / 64f;
    }

    public static string StripTags(string text)
    {
        if (text.IndexOf('<') < 0) return text;
        StringBuilder plain = new StringBuilder(text.Length);
        bool tag = false;
        foreach (char c in text)
        {
            if (c == '<') tag = true;
            else if (c == '>' && tag) tag = false;
            else if (!tag) plain.Append(c);
        }
        return plain.ToString();
    }

    // Unlit panel material drawn in a fixed order among the interface (panels 3000,
    // chips and icons above them, text at 3100).
    public static Material UnlitMaterial(Color color, int renderQueue)
    {
        Material material = UnlitMaterial(color);
        material.renderQueue = renderQueue;
        return material;
    }

    public static Material IconMaterial(UiIcon icon, Color color)
    {
        Material material = UnlitMaterial(color, 3050);
        material.mainTexture = Icon(icon);
        return material;
    }

    // Opaque, softly shaded material for the controller models: CrimeScene/UiShaded (kept in
    // the build by DemoUiTheme), else the always-included lit diffuse shader.
    public static Material ShadedMaterial(Color color)
    {
        Shader shader = Theme != null && Theme.Shaded != null ? Theme.Shaded : Shader.Find("CrimeScene/UiShaded");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null) return UnlitMaterial(color);
        return new Material(shader) { color = color, hideFlags = HideFlags.DontSave };
    }

    // Faces the transform's readable side (+Z away from the viewer, as TextMesh expects) towards the eye.
    public static void FaceViewer(Transform t, Vector3 eye)
    {
        Vector3 away = t.position - eye;
        if (away.sqrMagnitude > 1e-6f) t.rotation = Quaternion.LookRotation(away, Vector3.up);
    }

    public static Color WithAlpha(Color c, float a) { c.a = a; return c; }
}
