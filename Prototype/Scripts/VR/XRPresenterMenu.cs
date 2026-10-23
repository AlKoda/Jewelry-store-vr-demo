using System;
using System.Collections.Generic;
using UnityEngine;

// The presenter's controls inside the headset, for the Quest 3 app where no PC keyboard
// is at hand (it works over Link too). Hold the left controller's menu button for
// HoldSeconds: a panel of buttons opens above the left controller, facing the head.
// Point the right controller at a button (a thin ray shows the aim) and pull the right
// trigger. A short press of the menu button still shows or hides the controller hints;
// a short press while the panel is open closes it. Each button does what a presenter
// key does on the PC: reset the scene (F10's session reset), smooth walking or teleport
// (F8), comfort vignette (F6), Arabic or English (F7), standing height (F11/F12), light
// bar flashing, controller hints, close. Quitting stays on the existing Menu + Y hold.
// Labels follow the interface language. No XR dependency: XRControllerInput calls
// Show/Hide/Confirm and Update reads the pointer transform, so validators drive it with transforms.
public sealed class XRPresenterMenu : MonoBehaviour
{
    public const float HoldSeconds = 0.8f;
    public Vector3 Offset = new Vector3(0, 0.2f, 0.05f);
    public float ButtonWidth = 0.17f, ButtonHeight = 0.034f, Gap = 0.007f;
    public float PointerReach = 1.5f;

    public static XRPresenterMenu Active { get; private set; }
    public Transform Pointer;
    public Transform Head;
    public bool Open => root != null && root.gameObject.activeSelf;
    public int Hovered { get; private set; } = -1;
    public Transform Face => root;
    public int Count => items.Count;

    private sealed class Item
    {
        public string En, Ar;
        public Func<string> State;   // the current setting, shown after the label (null: none)
        public Action Run;
        public Transform Tile;
        public TextMesh Text;
    }

    private readonly List<Item> items = new List<Item>();
    private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
    private Transform root;
    private TextMesh title;
    private LineRenderer ray;
    private bool built;

    // ---------------------------------------------------------------- actions

    private void Define()
    {
        items.Clear();
        Add("Reset scene", "إعادة ضبط المشهد", null, () =>
        {
            DemoSession session = FindFirstObjectByType<DemoSession>();
            if (session != null) session.ResetSession();
        });
        Add("Movement", "الحركة", () => XRControllerInput.SmoothMovement ? L("smooth", "مشي") : L("teleport", "انتقال"),
            () => XRControllerInput.SmoothMovement = !XRControllerInput.SmoothMovement);
        Add("Comfort vignette", "تعتيم الأطراف", () => XRComfortVignette.Enabled ? L("on", "مفعّل") : L("off", "متوقف"),
            () => XRComfortVignette.Enabled = !XRComfortVignette.Enabled);
        Add("Language", "اللغة", () => DemoLanguage.Arabic ? "العربية" : "English",
            () => DemoLanguage.Arabic = !DemoLanguage.Arabic);
        Add("Height +5 cm", "رفع الطول 5 سم", () => Mathf.RoundToInt(XRHeadTracking.HeightOffset * 100) + " cm",
            () => XRHeadTracking.HeightOffset += 0.05f);
        Add("Height -5 cm", "خفض الطول 5 سم", () => Mathf.RoundToInt(XRHeadTracking.HeightOffset * 100) + " cm",
            () => XRHeadTracking.HeightOffset -= 0.05f);
        Add("Calibrate floor", "معايرة الأرضية", () => Mathf.RoundToInt(XRHeadTracking.FloorCorrection * 100) + " cm", CalibrateFloor);
        Add("Light bar", "أضواء الدورية", () =>
        {
            DemoLighting lighting = Lighting;
            return lighting != null && lighting.LightBar != null && !lighting.LightBar.Flashing ? L("steady", "ثابتة") : L("flashing", "وامضة");
        }, () =>
        {
            DemoLighting lighting = FindFirstObjectByType<DemoLighting>();
            if (lighting != null) lighting.ToggleLightBarFlashing();
        });
        Add("Controller hints", "تلميحات الأزرار", () => XRControllerInput.HintsVisible ? L("shown", "ظاهرة") : L("hidden", "مخفية"),
            XRControllerInput.ToggleHints);
        Add("Close", "إغلاق", null, Hide);
    }

    // Rest one controller on the real floor, then choose this with the other: the lowest tracked controller
    // sets the floor correction for head and hands together (XRHeadTracking.CalibrateFloor).
    private void CalibrateFloor()
    {
        float lowest = float.PositiveInfinity;
        foreach (XRControllerInput input in FindObjectsByType<XRControllerInput>(FindObjectsSortMode.None))
            if (input.PoseTracked) lowest = Mathf.Min(lowest, input.RawTrackedY);
        if (float.IsInfinity(lowest)) { DemoFeedback.Post(L("No tracked controller to measure the floor with", "لا توجد وحدة تحكم متتبعة لقياس الأرضية"), FeedbackTone.Warning); return; }
        XRHeadTracking.CalibrateFloor(lowest);
        DemoFeedback.Post(L("Floor calibrated: ", "تمت معايرة الأرضية: ") + Mathf.RoundToInt(XRHeadTracking.FloorCorrection * 100) + " cm", FeedbackTone.Success);
    }

    private DemoLighting lightingCache;
    private DemoLighting Lighting => lightingCache != null ? lightingCache : lightingCache = FindFirstObjectByType<DemoLighting>();

    private void Add(string en, string ar, Func<string> state, Action run) => items.Add(new Item { En = en, Ar = ar, State = state, Run = run });

    private static string L(string en, string ar) => DemoLanguage.Arabic ? ar : en;

    // Runs one button by index (validators call this directly); true when it existed.
    public bool Choose(int index)
    {
        if (!built) Build();
        if (index < 0 || index >= items.Count) return false;
        Item item = items[index];
        item.Run();
        DemoFeedback.Post(LabelOf(item, false));
        if (Open) Refresh();
        return true;
    }

    public int IndexOf(string english) => items.FindIndex(i => i.En == english);

    // ---------------------------------------------------------------- open, point, confirm

    public void Show()
    {
        if (!built) Build();
        root.gameObject.SetActive(true);
        Active = this;
        Hovered = -1;
        Refresh();
    }

    public void Hide()
    {
        if (root != null) root.gameObject.SetActive(false);
        if (ray != null) ray.enabled = false;
        if (Active == this) Active = null;
        Hovered = -1;
    }

    // Runs the button under the pointer; the panel stays open for the next choice (Close shuts it).
    public bool Confirm()
    {
        if (!Open || Hovered < 0) return false;
        return Choose(Hovered);
    }

    // Button under a ray, or -1.
    public int ButtonAt(Ray pointer)
    {
        if (!Open) return -1;
        Plane plane = new Plane(root.forward, root.position);
        if (!plane.Raycast(pointer, out float distance) || distance > PointerReach) return -1;
        Vector3 local = root.InverseTransformPoint(pointer.GetPoint(distance));
        if (Mathf.Abs(local.x) > ButtonWidth / 2) return -1;
        for (int i = 0; i < items.Count; i++)
            if (Mathf.Abs(local.y - RowY(i)) <= ButtonHeight / 2) return i;
        return -1;
    }

    public Vector3 ButtonPosition(int index) => root != null ? root.TransformPoint(new Vector3(0, RowY(index), 0)) : transform.position;

    private float RowY(int index) => (items.Count - 1) * (ButtonHeight + Gap) / 2 - index * (ButtonHeight + Gap);

    private void LateUpdate()
    {
        if (Open) Refresh();
    }

    public void Refresh()
    {
        root.localPosition = Offset;
        if (Head != null) DemoUi.FaceViewer(root, Head.position);
        Hovered = Pointer != null ? ButtonAt(new Ray(Pointer.position, Pointer.forward)) : -1;
        Font font = DemoLanguage.Arabic && DemoLanguage.Font != null ? DemoLanguage.Font : null;
        for (int i = 0; i < items.Count; i++)
        {
            items[i].Tile.localScale = i == Hovered ? new Vector3(1.04f, 1.12f, 1) : Vector3.one;
            string text = LabelOf(items[i], true);
            if (items[i].Text.text != text) items[i].Text.text = text;
            if (font != null && items[i].Text.font != font) { items[i].Text.font = font; items[i].Text.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
        }
        string heading = DemoLanguage.Arabic ? DemoLanguage.Text("قائمة المدرب") : "Presenter menu";
        if (title.text != heading) title.text = heading;
        if (Pointer == null) return;
        LineRenderer line = Ray();
        line.enabled = true;
        line.SetPosition(0, Pointer.position);
        line.SetPosition(1, Hovered >= 0 ? ButtonPosition(Hovered) : Pointer.position + Pointer.forward * 0.4f);
        line.startColor = line.endColor = Hovered >= 0 ? DemoUi.Accent : DemoUi.WithAlpha(DemoUi.Info, 0.5f);
    }

    private static string LabelOf(Item item, bool shaped)
    {
        string state = item.State != null ? item.State() : null;
        string text = DemoLanguage.Arabic ? item.Ar + (state != null ? ": " + state : "") : item.En + (state != null ? ": " + state : "");
        return shaped && DemoLanguage.Arabic ? DemoLanguage.Text(text) : text;
    }

    // ---------------------------------------------------------------- build

    private void Build()
    {
        built = true;
        Define();
        root = new GameObject("PresenterMenu").transform;
        root.SetParent(transform, false);
        root.localPosition = Offset;
        float height = items.Count * (ButtonHeight + Gap) + 0.04f;
        Own(DemoUi.WorldPanel("Back", root, new Vector3(0, 0.012f, 0.002f), ButtonWidth + 0.02f, height + 0.02f, 0.012f,
            DemoUi.WithAlpha(DemoUi.Card, 0.85f)).sharedMaterial);
        title = DemoUi.WorldText("Title", root, new Vector3(0, RowY(0) + ButtonHeight * 0.95f, -0.001f), "", 0.011f, DemoUi.Ink, TextAnchor.MiddleCenter, DemoUi.SemiBold);
        for (int i = 0; i < items.Count; i++)
        {
            MeshRenderer tile = DemoUi.WorldPanel("Button_" + i, root, new Vector3(0, RowY(i), 0.0006f), ButtonWidth, ButtonHeight, 0.008f,
                i == items.Count - 1 ? DemoUi.WithAlpha(DemoUi.Warning, 0.35f) : DemoUi.Card);
            tile.sharedMaterial.renderQueue = 3010;
            Own(tile.sharedMaterial);
            items[i].Tile = tile.transform;
            items[i].Text = DemoUi.WorldText("Label", tile.transform, new Vector3(0, 0, -0.0008f), "", 0.0095f, DemoUi.Ink, TextAnchor.MiddleCenter);
        }
        DemoUi.ToHeadsetUiLayer(root);
        root.gameObject.SetActive(false);
    }

    private LineRenderer Ray()
    {
        if (ray != null) return ray;
        GameObject line = new GameObject("PresenterMenuPointer");
        line.layer = DemoUi.HeadsetUiLayer;
        ray = line.AddComponent<LineRenderer>();
        ray.useWorldSpace = true;
        ray.positionCount = 2;
        ray.startWidth = ray.endWidth = 0.003f;
        ray.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ray.receiveShadows = false;
        ray.sharedMaterial = Own(DemoUi.UnlitMaterial(Color.white));
        return ray;
    }

    private T Own<T>(T thing) where T : UnityEngine.Object { owned.Add(thing); return thing; }

    private void OnDisable() { Hide(); }

    private void OnDestroy()
    {
        foreach (UnityEngine.Object thing in owned) if (thing != null) Destroy(thing);
        if (ray != null) Destroy(ray.gameObject);
    }
}
