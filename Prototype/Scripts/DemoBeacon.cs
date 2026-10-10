using UnityEngine;
using UnityEngine.Rendering;

// Instructor's beacon: a pulsing amber disc with a translucent two-metre column
// that marks a spot on the shop floor for the visitor ("go there", "look at
// this"). Built on first use from the built-in cylinder and runtime Standard
// materials, so it needs no asset; a session reset clears it.
public sealed class DemoBeacon : MonoBehaviour
{
    public InteriorBounds Bounds;
    public DemoSession Session;
    public float FloorY = 0f;
    public float ColumnHeight = 2f;
    public float PulseSeconds = 1.4f;
    public Color Amber = new Color(1f, 0.65f, 0.15f);

    public bool Active => marker != null && marker.gameObject.activeSelf;
    public Vector3 Position => marker != null ? marker.position : Vector3.zero;

    private Transform marker, ring;
    private Material ringMaterial, columnMaterial;

    private void OnEnable()
    {
        if (Session != null) Session.SessionReset.AddListener(Clear);
    }

    private void OnDisable()
    {
        if (Session != null) Session.SessionReset.RemoveListener(Clear);
    }

    // Marks the floor under the point, inside the interior; any height is accepted
    // so a pointer hit on a counter still lands the beacon on the floor beside it.
    public void Place(Vector3 worldPoint)
    {
        if (marker == null) Build();
        if (Bounds == null) Bounds = FindFirstObjectByType<InteriorBounds>();
        Vector3 p = Bounds != null ? Bounds.Clamp(worldPoint) : worldPoint;
        p.y = FloorY;
        marker.position = p;
        marker.gameObject.SetActive(true);
    }

    public void Clear()
    {
        if (marker != null) marker.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!Active) return;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 2 * Mathf.PI / PulseSeconds);
        float width = Mathf.Lerp(0.5f, 0.7f, pulse);
        ring.localScale = new Vector3(width, 0.01f, width);
        ringMaterial.SetColor("_EmissionColor", Amber * Mathf.Lerp(0.6f, 2.2f, pulse));
        columnMaterial.color = new Color(Amber.r, Amber.g, Amber.b, Mathf.Lerp(0.10f, 0.28f, pulse));
    }

    private void Build()
    {
        marker = new GameObject("Beacon").transform;
        marker.SetParent(transform, false);
        ringMaterial = new Material(Shader.Find("Standard")) { color = Amber };
        ringMaterial.EnableKeyword("_EMISSION");
        ringMaterial.SetColor("_EmissionColor", Amber);
        ring = Cylinder("Ring", new Vector3(0, 0.005f, 0), new Vector3(0.6f, 0.01f, 0.6f), ringMaterial);
        // Premultiplied transparent Standard, as DemoGeometry.Translucent builds it in the Editor.
        columnMaterial = new Material(Shader.Find("Standard")) { color = new Color(Amber.r, Amber.g, Amber.b, 0.2f) };
        columnMaterial.SetFloat("_Mode", 3);
        columnMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
        columnMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        columnMaterial.SetInt("_ZWrite", 0);
        columnMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        columnMaterial.renderQueue = 3000;
        // The built-in cylinder is two units tall, so half the height scales it.
        Cylinder("Column", new Vector3(0, ColumnHeight / 2, 0), new Vector3(0.3f, ColumnHeight / 2, 0.3f), columnMaterial);
    }

    // Collider-free cylinder, so it never blocks the pointer or the walker;
    // CreatePrimitive guarantees the built-in mesh exists in player builds.
    private Transform Cylinder(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = name;
        // Immediate: a deferred Destroy would leave the collider live for the
        // rest of the frame, catching the pointer ray or nudging the walker.
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(marker, false);
        g.transform.localPosition = position;
        g.transform.localScale = scale;
        MeshRenderer renderer = g.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return g.transform;
    }

    private void OnDestroy()
    {
        if (ringMaterial != null) Destroy(ringMaterial);
        if (columnMaterial != null) Destroy(columnMaterial);
    }
}
