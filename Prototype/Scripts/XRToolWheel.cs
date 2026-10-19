using System.Collections.Generic;
using UnityEngine;

// Radial tool menu on the left controller. Holding the left trigger with an empty
// left hand opens a ring of the six tool kinds and the evidence camera above the
// controller, turned towards the head. The right controller points at a slot (a
// thin ray shows where); the right trigger, or letting go of the left trigger while
// a slot is lit, puts that item in the right hand. Choosing a tool while the right
// hand has the camera returns the camera to its rack first; choosing the camera
// puts a held tool down, and choosing it again returns the camera.
// No XR dependency: XRControllerInput calls Show/Hide/Confirm, and Update reads the
// pointer transform, so the validator drives it with plain transforms.
public sealed class XRToolWheel : MonoBehaviour
{
    public const int CameraSlot = 6;
    public const int SlotCount = 7;
    public Vector3 Centre = new Vector3(0, 0.13f, 0.03f);
    public float Radius = 0.085f;
    public float TileSize = 0.042f;
    // A ray passing nearer the centre than this picks nothing, so the wheel can be dismissed.
    public float DeadRadius = 0.03f;
    public float PointerReach = 1.5f;

    public static XRToolWheel Active { get; private set; }
    // The ring itself (null until first shown): its forward is the wheel's normal, away from the head.
    public Transform Face => root;
    public bool Open => root != null && root.gameObject.activeSelf;
    public int Hovered { get; private set; } = -1;
    // Where Update reads the pointing ray (the right controller) and who receives the choice.
    public Transform Pointer;
    public HandInteractor Target;
    public Transform Head;

    private Transform root;
    private TextMesh title;
    private LineRenderer ray;
    private readonly List<Transform> tiles = new List<Transform>();
    private readonly List<Object> owned = new List<Object>();

    public static string SlotName(int slot) => slot == CameraSlot ? "Evidence camera" : DeployedTool.Title((DemoToolKind)slot);

    public void Show()
    {
        if (root == null) Build();
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

    // Gives the hovered item to the target hand and closes the wheel; false (wheel stays open)
    // when nothing is hovered.
    public bool Confirm()
    {
        if (!Open || Hovered < 0) return false;
        bool given = Choose(Hovered);
        Hide();
        return given;
    }

    public bool Choose(int slot)
    {
        HandInteractor hand = Target;
        if (hand == null || slot < 0 || slot >= SlotCount) return false;
        if (slot == CameraSlot)
        {
            if (hand.Held != null) hand.Place();
            hand.ToggleCamera();
            DemoFeedback.Post(hand.HoldingCamera ? "Camera in hand" : "Camera back on its rack");
            return true;
        }
        if (hand.HoldingCamera) hand.ToggleCamera();
        return hand.SpawnIntoHand((DemoToolKind)slot) != null;
    }

    // Slot under a ray, or -1: the ray meets the wheel's plane within the ring's reach, outside the dead centre.
    public int SlotAt(Ray pointer)
    {
        if (!Open) return -1;
        Plane plane = new Plane(root.forward, root.position);
        if (!plane.Raycast(pointer, out float distance) || distance > PointerReach) return -1;
        Vector3 local = root.InverseTransformPoint(pointer.GetPoint(distance));
        float r = new Vector2(local.x, local.y).magnitude;
        if (r < DeadRadius || r > Radius + TileSize) return -1;
        float angle = Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg; // 0 at the top, clockwise
        return Mathf.RoundToInt(Mathf.Repeat(angle, 360) / (360f / SlotCount)) % SlotCount;
    }

    public Vector3 SlotPosition(int slot) => root != null ? root.TransformPoint(SlotLocal(slot)) : transform.position;

    private Vector3 SlotLocal(int slot)
    {
        float a = slot * (360f / SlotCount) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0) * Radius;
    }

    private void LateUpdate()
    {
        if (!Open) return;
        Refresh();
    }

    // Faces the head, then lights the slot under the pointer and draws the pointer ray.
    public void Refresh()
    {
        root.localPosition = Centre;
        if (Head != null) DemoUi.FaceViewer(root, Head.position);
        int hovered = Pointer != null ? SlotAt(new Ray(Pointer.position, Pointer.forward)) : -1;
        if (hovered != Hovered && hovered >= 0) DemoSounds.Play(DemoSound.Click, root.position, 0.15f);
        Hovered = hovered;
        for (int i = 0; i < tiles.Count; i++) tiles[i].localScale = Vector3.one * (i == Hovered ? 1.25f : 1f);
        title.text = Hovered >= 0 ? SlotName(Hovered) : "Point with the right hand";
        if (Pointer == null) return;
        LineRenderer line = Ray();
        line.enabled = true;
        line.SetPosition(0, Pointer.position);
        line.SetPosition(1, Hovered >= 0 ? SlotPosition(Hovered) : Pointer.position + Pointer.forward * 0.4f);
        line.startColor = line.endColor = Hovered >= 0 ? DemoUi.Accent : DemoUi.WithAlpha(DemoUi.Info, 0.5f);
    }

    private void Build()
    {
        root = new GameObject("ToolWheel").transform;
        root.SetParent(transform, false);
        root.localPosition = Centre;
        Own(DemoUi.WorldPanel("Back", root, new Vector3(0, 0, 0.002f), (Radius + TileSize) * 2, (Radius + TileSize) * 2, Radius + TileSize,
            DemoUi.WithAlpha(DemoUi.Card, 0.55f)).sharedMaterial);
        for (int i = 0; i < SlotCount; i++)
        {
            MeshRenderer tile = DemoUi.WorldPanel("Slot_" + i, root, SlotLocal(i), TileSize, TileSize, TileSize / 2, DemoUi.Card);
            tile.sharedMaterial.renderQueue = 3010;
            Own(tile.sharedMaterial);
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(tile.transform, false);
            icon.transform.localPosition = new Vector3(0, 0, -0.0004f);
            icon.AddComponent<MeshFilter>().sharedMesh = DemoUi.RoundedQuad(TileSize * 0.65f, TileSize * 0.65f, 0, 1);
            MeshRenderer iconRenderer = icon.AddComponent<MeshRenderer>();
            UiIcon kind = i == CameraSlot ? UiIcon.Camera : DemoUi.IconFor((DemoToolKind)i);
            iconRenderer.sharedMaterial = Own(DemoUi.IconMaterial(kind, i == CameraSlot ? DemoUi.Ink : DemoDesktopPanel.KindColor((DemoToolKind)i)));
            iconRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            iconRenderer.receiveShadows = false;
            tiles.Add(tile.transform);
        }
        title = DemoUi.WorldText("Title", root, new Vector3(0, 0, -0.001f), "", 0.011f, DemoUi.Ink, TextAnchor.MiddleCenter, DemoUi.SemiBold);
        DemoUi.ToHeadsetUiLayer(root);
        root.gameObject.SetActive(false);
    }

    private LineRenderer Ray()
    {
        if (ray != null) return ray;
        GameObject line = new GameObject("WheelPointer");
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

    private T Own<T>(T thing) where T : Object { owned.Add(thing); return thing; }

    private void OnDisable() { Hide(); }

    private void OnDestroy()
    {
        foreach (Object thing in owned) if (thing != null) Destroy(thing);
        if (ray != null) Destroy(ray.gameObject);
    }
}
