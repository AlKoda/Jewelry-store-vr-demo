using UnityEngine;

// Comfort vignette for smooth stick movement: a soft dark ring at the edge of the view
// that fades in only while the rig itself glides (stick walking), never for walking in
// the room, which does not move the rig. The middle of the view always stays fully clear;
// at full stick speed the very edge darkens to Strength (0.35 by default, so the room
// stays visible through it). Teleports (a jump of more than JumpDistance in one frame)
// are ignored: they have their own blink. F7 on the desktop switches it on or off.
// Lives on the XR rig; the ring is a child of the head on the headset interface layer,
// so evidence photos never show it.
public sealed class XRComfortVignette : MonoBehaviour
{
    public static bool Enabled = true;
    public Transform Head;
    [Range(0, 1)] public float Strength = 0.35f;
    // Rig speed (m/s) that gives the full Strength.
    public float FullSpeed = 1.4f;
    public float FadeSeconds = 0.18f;
    public float JumpDistance = 0.5f;
    // Ring geometry at Distance in front of the eyes: fully clear inside ClearRadius,
    // reaching Strength at EdgeRadius (beyond the Quest 3's field of view).
    public float Distance = 0.3f;
    public float ClearRadius = 0.16f;
    public float EdgeRadius = 0.42f;

    public float Alpha { get; private set; }

    private DemoModeSwitch mode;
    private Vector3 lastPosition;
    private bool hasLast;
    private MeshRenderer ring;
    private Material material;
    private Mesh mesh;

    private void OnEnable() { hasLast = false; Alpha = 0; Apply(); }

    private void LateUpdate() { Step(Time.deltaTime); }

    // One frame of the fade; public so the validator can drive it without a headset.
    public void Step(float deltaTime)
    {
        Vector3 p = transform.position;
        float speed = 0;
        if (hasLast && deltaTime > 0)
        {
            Vector3 d = p - lastPosition; d.y = 0;
            if (d.magnitude < JumpDistance) speed = d.magnitude / deltaTime;
        }
        lastPosition = p;
        hasLast = true;
        if (mode == null) mode = FindFirstObjectByType<DemoModeSwitch>();
        bool vr = mode == null || mode.VRActive;
        float target = Enabled && vr ? Strength * Mathf.Clamp01(speed / Mathf.Max(0.01f, FullSpeed)) : 0;
        Alpha = Mathf.MoveTowards(Alpha, target, deltaTime * Strength / Mathf.Max(0.01f, FadeSeconds));
        Apply();
    }

    private void Apply()
    {
        if (Alpha <= 0.001f) { if (ring != null) ring.enabled = false; return; }
        if (ring == null) Build();
        if (ring == null) return;
        ring.enabled = true;
        material.color = new Color(0, 0, 0, Alpha / Mathf.Max(0.01f, Strength));
    }

    private void Build()
    {
        if (Head == null) return;
        GameObject go = new GameObject("ComfortVignette");
        go.transform.SetParent(Head, false);
        go.transform.localPosition = new Vector3(0, 0, Distance);
        mesh = Annulus(ClearRadius, EdgeRadius, 48, Strength);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        ring = go.AddComponent<MeshRenderer>();
        material = DemoUi.UnlitMaterial(Color.black);
        material.renderQueue = 4000; // after everything else, before nothing
        ring.sharedMaterial = material;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;
        go.layer = DemoUi.HeadsetUiLayer;
    }

    // Flat ring facing the eye: vertex alpha 0 on the inner edge, edgeAlpha on the outer,
    // then a solid outer band so the corners of the lens never show a hard line.
    private static Mesh Annulus(float inner, float outer, int segments, float edgeAlpha)
    {
        Vector3[] v = new Vector3[segments * 3];
        Color[] c = new Color[v.Length];
        int[] t = new int[segments * 12];
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2 / segments;
            Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            v[i] = dir * inner; c[i] = new Color(0, 0, 0, 0);
            v[segments + i] = dir * outer; c[segments + i] = new Color(0, 0, 0, edgeAlpha);
            v[segments * 2 + i] = dir * outer * 3; c[segments * 2 + i] = new Color(0, 0, 0, edgeAlpha);
            int n = (i + 1) % segments, k = i * 12;
            for (int ringIndex = 0; ringIndex < 2; ringIndex++, k += 6)
            {
                int a0 = ringIndex * segments + i, a1 = ringIndex * segments + n, b0 = a0 + segments, b1 = a1 + segments;
                t[k] = a0; t[k + 1] = b0; t[k + 2] = a1; t[k + 3] = a1; t[k + 4] = b0; t[k + 5] = b1;
            }
        }
        Mesh mesh = new Mesh { name = "ComfortVignette", vertices = v, colors = c, triangles = t, hideFlags = HideFlags.DontSave };
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10); // never culled while the head turns
        return mesh;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (mesh != null) Destroy(mesh);
        if (ring != null) Destroy(ring.gameObject);
    }
}
