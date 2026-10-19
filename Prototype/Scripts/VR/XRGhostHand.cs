using UnityEngine;

// A see-through hand where the controller is (VR only). It is built from primitives
// (palm, four three-joint fingers and a thumb) on the GhostHand material: the middle,
// ring and little fingers close with the grip, the index finger with the trigger and the
// thumb rests on the face buttons when one is touched or pressed. Opacity is shared by
// both hands (static Opacity, F3 lower / F4 higher on the desktop) and is the first
// thing to tune from the headset test. While the hands show, the controller model is
// hidden (HideControllers); the button hints stay. The pose offsets are guesses for the
// Quest 3 Touch Plus controllers, to be tuned in the headset.
[DisallowMultipleComponent]
public sealed class XRGhostHand : MonoBehaviour
{
    public static float Opacity = 0.35f;
    public static bool Visible = true;
    public static bool HideControllers = true;
    public static readonly Color Tint = new Color(0.86f, 0.9f, 1f);

    // Palm centre and turn relative to the controller (right hand; mirrored on the left).
    public Vector3 PalmOffset = new Vector3(0.012f, -0.03f, -0.065f);
    public Vector3 PalmEuler = new Vector3(-50f, 0f, -75f);
    public float CurlSpeed = 14f;

    public float Grip { get; private set; }
    public float Index { get; private set; }
    public float Thumb { get; private set; }
    public Transform Root => root;
    public Material Material => material;

    private XRControllerInput input;
    private Transform root;
    private Material material;
    private Mesh capsule, sphere;
    private float side = 1;
    private DemoModeSwitch mode;
    // [finger][joint]; 0 index, 1 middle, 2 ring, 3 little, 4 thumb.
    private readonly Transform[][] joints = new Transform[5][];
    private static readonly float[] FingerX = { 0.03f, 0.01f, -0.01f, -0.028f };
    private static readonly float[] FingerLength = { 0.026f, 0.029f, 0.027f, 0.021f };
    private static readonly float[] FingerWidth = { 0.019f, 0.02f, 0.019f, 0.016f };

    public void Build(XRControllerInput owner)
    {
        if (root != null) return;
        input = owner;
        side = owner.Left ? -1 : 1;
        material = DemoUi.GhostMaterial(DemoUi.WithAlpha(Tint, Opacity));
        material.renderQueue = 3020; // after the scene's glass, before the interface panels
        GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule = probe.GetComponent<MeshFilter>().sharedMesh;
        Destroy(probe);
        probe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere = probe.GetComponent<MeshFilter>().sharedMesh;
        Destroy(probe);

        root = new GameObject("GhostHand").transform;
        root.SetParent(transform, false);
        root.localPosition = new Vector3(PalmOffset.x * side, PalmOffset.y, PalmOffset.z);
        root.localRotation = Quaternion.Euler(PalmEuler.x, PalmEuler.y * side, PalmEuler.z * side);
        // Hand frame: x across the knuckles (towards the thumb is +x on the right hand), y the back of the hand, z the fingers.
        Piece("Palm", root, sphere, new Vector3(0, 0, 0), new Vector3(0.082f, 0.026f, 0.09f));
        Piece("Wrist", root, sphere, new Vector3(0, -0.002f, -0.055f), new Vector3(0.06f, 0.03f, 0.05f));
        for (int f = 0; f < 4; f++)
        {
            Transform knuckle = Joint("Finger" + f, root, new Vector3(FingerX[f] * side, 0.002f, 0.042f));
            joints[f] = Chain(knuckle, FingerLength[f], FingerWidth[f]);
        }
        Transform thumbBase = Joint("Thumb", root, new Vector3(0.04f * side, -0.004f, -0.012f));
        thumbBase.localRotation = Quaternion.Euler(0, 45 * side, 20 * side);
        joints[4] = Chain(thumbBase, 0.028f, 0.021f);
        DemoUi.ToHeadsetUiLayer(root); // never in evidence photos
        Apply(true);
    }

    private Transform[] Chain(Transform start, float length, float width)
    {
        Transform[] chain = new Transform[3];
        Transform parent = start;
        for (int j = 0; j < 3; j++)
        {
            float l = length * (j == 0 ? 1f : j == 1 ? 0.75f : 0.62f);
            Transform joint = j == 0 ? start : Joint("J" + j, parent, new Vector3(0, 0, parentLength));
            Piece("Bone", joint, capsule, new Vector3(0, 0, l / 2), new Vector3(width * (1 - j * 0.08f), l / 2 + width / 2, width * (1 - j * 0.08f)), new Vector3(90, 0, 0));
            chain[j] = joint;
            parent = joint;
            parentLength = l;
        }
        return chain;
    }

    private float parentLength;

    private static Transform Joint(string name, Transform parent, Vector3 local)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = local;
        return t;
    }

    private void Piece(string name, Transform parent, Mesh mesh, Vector3 local, Vector3 scale, Vector3 euler = default)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    // Grip, trigger and thumb (0..1) from the controller; the validator sets them directly.
    public void SetPose(float grip, float index, float thumb, float deltaTime)
    {
        float k = deltaTime <= 0 ? 1 : 1 - Mathf.Exp(-CurlSpeed * deltaTime);
        Grip = Mathf.Lerp(Grip, Mathf.Clamp01(grip), k);
        Index = Mathf.Lerp(Index, Mathf.Clamp01(index), k);
        Thumb = Mathf.Lerp(Thumb, Mathf.Clamp01(thumb), k);
        for (int f = 0; f < 4; f++)
        {
            // A relaxed hand is slightly curled; a full grip wraps the handle.
            float curl = Mathf.Lerp(0.12f, 1f, f == 0 ? Index : Grip);
            for (int j = 0; j < 3; j++)
                joints[f][j].localRotation = Quaternion.Euler(curl * (j == 0 ? 70 : j == 1 ? 90 : 60), 0, 0);
        }
        Transform[] thumbJoints = joints[4];
        Quaternion baseTurn = Quaternion.Euler(0, 45 * side, 20 * side);
        thumbJoints[0].localRotation = baseTurn * Quaternion.Euler(Thumb * 25, -Thumb * 30 * side, 0);
        thumbJoints[1].localRotation = Quaternion.Euler(Thumb * 35, 0, 0);
        thumbJoints[2].localRotation = Quaternion.Euler(Thumb * 30, 0, 0);
    }

    private void LateUpdate()
    {
        if (root == null) return;
        Apply(false);
        if (input != null && root.gameObject.activeSelf)
            SetPose(input.GripValue, input.TriggerValue, input.ThumbDown ? 1 : 0, Time.deltaTime);
    }

    // Shows the hand only in VR, applies the shared opacity and hides or shows the controller model.
    public void Apply(bool force)
    {
        if (mode == null) mode = FindFirstObjectByType<DemoModeSwitch>();
        bool show = Visible && (mode == null || mode.VRActive);
        if (force || root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
        Color c = DemoUi.WithAlpha(Tint, Mathf.Clamp01(Opacity));
        if (material.color != c) material.color = c;
        XRHandHud hud = input != null ? input.Hud : null;
        if (hud != null && hud.Model != null)
        {
            bool controller = !(show && HideControllers);
            if (hud.Model.gameObject.activeSelf != controller) hud.Model.gameObject.SetActive(controller);
        }
    }

    // Presenter keys: opacity in steps of 0.1 between 0.1 and 1.
    public static void ChangeOpacity(float delta)
    {
        Opacity = Mathf.Clamp(Mathf.Round((Opacity + delta) * 10) / 10, 0.1f, 1f);
        DemoFeedback.Post("Hand opacity " + Mathf.RoundToInt(Opacity * 100) + "%  (F3 lower, F4 higher)", FeedbackTone.Info, true);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (root != null) Destroy(root.gameObject);
    }
}
