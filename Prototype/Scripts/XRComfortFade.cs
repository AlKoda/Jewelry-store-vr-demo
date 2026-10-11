using UnityEngine;

// Brief blink in front of the eyes that masks teleports and snap turns. Lives on
// the XR head camera; DemoModeSwitch enables it only in VR, so the desktop view
// never shows the quad. Fade() is called in the same frame as the move, before it
// renders, and starts at full black, so the jump itself is never seen; the view
// then fades back in over Duration (unscaled). The quad is created lazily and
// disabled whenever the alpha is back at zero.
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
        SetAlpha(1);
    }

    private void Update()
    {
        if (!Fading) return;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= Duration) { Fading = false; SetAlpha(0); return; }
        SetAlpha(1f - elapsed / Duration);
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

    // A quad just past the near clip plane covers the view, so nothing in the scene can
    // sit in front of it; drawn last in the overlay queue. Transparent unlit, no collider.
    private void CreateQuad()
    {
        GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cover.name = "ComfortFade";
        Collider blocker = cover.GetComponent<Collider>();
        blocker.enabled = false; // Destroy only lands at frame end; never block rays meanwhile.
        Destroy(blocker);
        cover.transform.SetParent(transform, false);
        Camera head = GetComponent<Camera>();
        float z = (head != null ? head.nearClipPlane : 0.01f) + 0.01f;
        cover.transform.localPosition = new Vector3(0, 0, z);
        cover.transform.localScale = new Vector3(Mathf.Max(0.5f, z * 8f), Mathf.Max(0.5f, z * 8f), 1f);
        quad = cover.GetComponent<MeshRenderer>();
        quad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quad.receiveShadows = false;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
        material = new Material(shader);
        material.renderQueue = 5000;
        quad.sharedMaterial = material;
    }
}
