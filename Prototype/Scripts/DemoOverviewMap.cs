using UnityEngine;

// Top-down picture of the shop for the presenter panel, so the instructor can
// see where the visitor and the tools are without the headset. A hidden
// orthographic camera renders into a small texture a few times a second with
// the ceilings switched off for that one render.
public sealed class DemoOverviewMap : MonoBehaviour
{
    public Transform Ceiling;
    public Vector2 Centre = new Vector2(0, 5.5f);
    public float HalfSize = 6f;
    public int Resolution = 256;
    public float Interval = 0.25f;
    public RenderTexture Texture { get; private set; }

    private Camera overview;
    private float nextRender;

    private void Awake()
    {
        Texture = new RenderTexture(Resolution, Resolution, 16) { name = "OverviewMap" };
        GameObject rig = new GameObject("OverviewCamera");
        rig.transform.SetParent(transform, false);
        rig.transform.position = new Vector3(Centre.x, 20, Centre.y);
        rig.transform.rotation = Quaternion.Euler(90, 0, 0);
        overview = rig.AddComponent<Camera>();
        overview.enabled = false;
        overview.orthographic = true;
        overview.orthographicSize = HalfSize;
        overview.nearClipPlane = 1;
        overview.farClipPlane = 40;
        overview.clearFlags = CameraClearFlags.SolidColor;
        overview.backgroundColor = new Color(0.12f, 0.13f, 0.15f);
        overview.targetTexture = Texture;
        overview.stereoTargetEye = StereoTargetEyeMask.None;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRender) return;
        nextRender = Time.unscaledTime + Interval;
        Render();
    }

    public void Render()
    {
        bool ceilingShown = Ceiling != null && Ceiling.gameObject.activeSelf;
        if (ceilingShown) Ceiling.gameObject.SetActive(false);
        overview.Render();
        if (ceilingShown) Ceiling.gameObject.SetActive(true);
    }

    // World position to 0..1 texture coordinates (u right, v up = +z).
    public Vector2 ToMap(Vector3 world)
    {
        return new Vector2((world.x - Centre.x) / (2 * HalfSize) + 0.5f, (world.z - Centre.y) / (2 * HalfSize) + 0.5f);
    }

    private void OnDestroy()
    {
        if (Texture != null) Texture.Release();
    }
}
