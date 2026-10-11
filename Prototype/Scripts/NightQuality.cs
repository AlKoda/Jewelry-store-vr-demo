using UnityEngine;

// Runtime quality for the baked night scene in VR. All scene lighting is baked, so
// the only realtime light is the flashlight: four pixel lights leave room for it and
// the diagnostic sun, hard low-cost shadows over the flashlight's 14 m reach, 4x
// MSAA instead of post-process anti-aliasing, and no realtime reflection probes (the
// three box-projected probes are baked). Applied in Awake. In a build they are not
// restored (another scene loaded afterwards keeps them; a scene that needs others
// sets its own). In the Editor they are project QualitySettings that Play mode would
// leave changed, so the previous values are restored when the component is destroyed
// (leaving Play mode) or when the validator calls Restore.
public sealed class NightQuality : MonoBehaviour
{
    public int PixelLights = 4;
    public int AntiAliasing = 4;
    public float ShadowDistance = 14;

    private struct Snapshot
    {
        public int PixelLights, AntiAliasing;
        public ShadowQuality Shadows;
        public ShadowResolution Resolution;
        public float ShadowDistance, LodBias;
        public bool RealtimeProbes, SoftParticles;
    }

    private static Snapshot? previous;

    private void Awake()
    {
#if UNITY_EDITOR
        if (!previous.HasValue)
            previous = new Snapshot
            {
                PixelLights = QualitySettings.pixelLightCount,
                AntiAliasing = QualitySettings.antiAliasing,
                Shadows = QualitySettings.shadows,
                Resolution = QualitySettings.shadowResolution,
                ShadowDistance = QualitySettings.shadowDistance,
                LodBias = QualitySettings.lodBias,
                RealtimeProbes = QualitySettings.realtimeReflectionProbes,
                SoftParticles = QualitySettings.softParticles,
            };
#endif
        Apply();
    }

#if UNITY_EDITOR
    private void OnDestroy() { Restore(); }
#endif

    // Puts back the QualitySettings saved in Awake (Editor only; nothing is saved in a
    // build). True when something was restored and the settings now match it.
    public static bool Restore()
    {
        if (!previous.HasValue) return false;
        Snapshot s = previous.Value;
        previous = null;
        QualitySettings.pixelLightCount = s.PixelLights;
        QualitySettings.antiAliasing = s.AntiAliasing;
        QualitySettings.shadows = s.Shadows;
        QualitySettings.shadowResolution = s.Resolution;
        QualitySettings.shadowDistance = s.ShadowDistance;
        QualitySettings.lodBias = s.LodBias;
        QualitySettings.realtimeReflectionProbes = s.RealtimeProbes;
        QualitySettings.softParticles = s.SoftParticles;
        return QualitySettings.pixelLightCount == s.PixelLights && Mathf.Abs(QualitySettings.shadowDistance - s.ShadowDistance) < 0.001f
            && QualitySettings.shadows == s.Shadows;
    }

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
