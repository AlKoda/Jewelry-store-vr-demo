using UnityEngine;

// Brief blink in front of the eyes that masks teleports and snap turns. Lives on
// the XR head camera; DemoModeSwitch enables it only in VR, so the desktop view
// never shows the quad. Fade() runs alpha 0 -> 1 -> 0 over Duration (unscaled);
// the quad is created lazily and disabled whenever the alpha is back at zero.
public sealed class XRComfortFade : MonoBehaviour
{
    public bool Enabled = true;
    public float Duration = 0.25f;
    public float Alpha { get; private set; }
    public bool Fading { get; private set; }

    private float elapsed;
    private Renderer quad;
    private Material material;

    public void Fade()
    {
        if (!Enabled || !isActiveAndEnabled) return;
        elapsed = 0;
        Fading = true;
    }

    private void Update()
    {
        if (!Fading) return;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= Duration) { Fading = false; SetAlpha(0); return; }
        float half = Duration * 0.5f;
        SetAlpha(1f - Mathf.Abs(elapsed - half) / half);
    }

    private void OnDisable() { Fading = false; SetAlpha(0); }

    private void OnDestroy() { if (material != null) Destroy(material); }

    private void SetAlpha(float alpha)
    {
        Alpha = alpha;
        if (quad == null && alpha > 0) CreateQuad();
        if (quad == null) return;
        quad.enabled = alpha > 0;
        material.color = new Color(0, 0, 0, alpha);
    }

    // A quad half a metre ahead covers the view; transparent unlit shader, no collider.
    private void CreateQuad()
    {
        GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cover.name = "ComfortFade";
        Collider blocker = cover.GetComponent<Collider>();
        blocker.enabled = false; // Destroy only lands at frame end; never block rays meanwhile.
        Destroy(blocker);
        cover.transform.SetParent(transform, false);
        cover.transform.localPosition = new Vector3(0, 0, 0.5f);
        cover.transform.localScale = new Vector3(2f, 2f, 1f);
        quad = cover.GetComponent<MeshRenderer>();
        quad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quad.receiveShadows = false;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
        material = new Material(shader);
        quad.sharedMaterial = material;
    }
}
