using UnityEngine;

// Top-down picture of the shop for the presenter panel, so the instructor can
// see where the visitor and the tools are without the headset. A hidden
// orthographic camera renders into a small texture about once a second while the
// drawer's Map tab is open, in flat albedo through a replacement shader (MapShader),
// and culls the ceiling layer and the headset interface: the panel draws the
// visitor, the tools, the camera and the beacon as glyphs itself.
public sealed class DemoOverviewMap : MonoBehaviour
{
    // Objects on this (unnamed) layer are left out of the overview: the ceilings. The
    // headset interface (DemoUi.HeadsetUiLayer) is left out too.
    public int HiddenLayer = 31;
    public Vector2 Centre = new Vector2(0, 5.5f);
    public float HalfSize = 6f;
    public int Resolution = 256;
    public float Interval = 1f; // tools, camera, visitor and beacon are live IMGUI glyphs; the texture only carries the scene
    // Replacement shader (DemoToolsBuilder assigns Hidden/CrimeScene/MapAlbedo, which also keeps it in the
    // build); without it the map renders lit.
    public Shader MapShader;
    public RenderTexture Texture { get; private set; }

    private Camera overview;
    private DemoDesktopPanel panel;
    // Only the full drawer's Map tab shows the map; every other layout and tab skips the render.
    public bool ShouldRender => panel != null && panel.isActiveAndEnabled && panel.Layout == PanelLayout.Full && panel.Tab == PanelTab.Map;
    private float nextRender;

    private void Awake()
    {
        panel = GetComponent<DemoDesktopPanel>();
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
        overview.cullingMask = ~((1 << HiddenLayer) | (1 << DemoUi.HeadsetUiLayer));
        // Baked occlusion ignores the culling mask: the hidden roof would still hide the interior.
        overview.useOcclusionCulling = false;
        overview.stereoTargetEye = StereoTargetEyeMask.None;
        overview.allowHDR = false; // LDR 256 px target: no FP16 intermediate or resolve
        overview.allowMSAA = false;
    }

    private void Update()
    {
        // Reopening the Map tab renders at once instead of showing the old picture for up to Interval.
        if (!ShouldRender) { nextRender = 0; return; }
        if (Time.unscaledTime < nextRender) return;
        nextRender = Time.unscaledTime + Interval;
        Render();
    }

    public void Render()
    {
        if (MapShader != null) overview.RenderWithShader(MapShader, "RenderType");
        else overview.Render();
    }

    // World position to 0..1 texture coordinates (u right, v up = +z).
    public Vector2 ToMap(Vector3 world)
    {
        return new Vector2((world.x - Centre.x) / (2 * HalfSize) + 0.5f, (world.z - Centre.y) / (2 * HalfSize) + 0.5f);
    }

    // The inverse: 0..1 texture coordinates to the point on the shop floor (y 0).
    public Vector3 FromMap(Vector2 uv)
    {
        return new Vector3((uv.x - 0.5f) * 2 * HalfSize + Centre.x, 0, (uv.y - 0.5f) * 2 * HalfSize + Centre.y);
    }

    private void OnDestroy()
    {
        if (Texture != null) { Texture.Release(); Destroy(Texture); }
    }
}
