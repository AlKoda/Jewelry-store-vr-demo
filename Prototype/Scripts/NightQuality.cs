using UnityEngine;

// Runtime quality for the baked night scene in VR. All scene lighting is baked, so
// the only realtime light is the flashlight: four pixel lights leave room for it and
// the diagnostic sun, hard low-cost shadows over the flashlight's 14 m reach, 4x
// MSAA instead of post-process anti-aliasing, and no realtime reflection probes (the
// three box-projected probes are baked). Applied in Awake and not restored: these
// are project QualitySettings, so another scene loaded afterwards keeps them (a
// scene that needs others sets its own).
public sealed class NightQuality : MonoBehaviour
{
    public int PixelLights = 4;
    public int AntiAliasing = 4;
    public float ShadowDistance = 14;

    private void Awake() { Apply(); }

    public void Apply()
    {
        QualitySettings.pixelLightCount = PixelLights;
        QualitySettings.shadows = ShadowQuality.HardOnly;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.shadowDistance = ShadowDistance;
        QualitySettings.antiAliasing = AntiAliasing;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.softParticles = false;
        QualitySettings.lodBias = 1;
        if (Camera.main != null) Configure(Camera.main);
    }

    // Forward rendering keeps MSAA; LDR skips the HDR buffer the night does not need
    // (nothing is post-processed); the dark sky is the background and the reflection.
    public static void Configure(Camera camera)
    {
        camera.renderingPath = RenderingPath.Forward;
        camera.allowHDR = false;
        camera.allowMSAA = true;
        camera.clearFlags = CameraClearFlags.Skybox;
    }
}
