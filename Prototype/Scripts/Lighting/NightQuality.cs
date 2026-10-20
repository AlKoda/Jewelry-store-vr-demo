using UnityEngine;

// Runtime quality for the baked night scene in VR. All scene lighting is baked, so
// the only realtime light is the flashlight: four pixel lights leave room for it and
// the diagnostic sun, filtered 2K flashlight shadows over its 14 m reach, 4x
// MSAA instead of post-process anti-aliasing, and no realtime reflection probes (the
// three box-projected probes are baked). Applied in Awake; the previous values are
// saved first and restored when the component is destroyed: in a build when another
// map is loaded (F10 back to the start menu, then the daytime shop keeps its own
// settings), in the Editor when Play mode ends (they are project QualitySettings), or
// when the validator calls Restore.
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
        Apply();
    }

    private void OnDestroy() { Restore(); }

    // Puts back the QualitySettings saved in Awake. True when something was restored
    // and the settings now match it.
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

    // Quest 3 standalone tier (DemoPlatform.Standalone): two per-pixel lights (a torch and a lamp),
    // hard shadows only, out to 8 m, 4x MSAA kept (cheap on the tiled mobile GPU), nearer LOD switches.
    public int QuestPixelLights = 2;
    public float QuestShadowDistance = 8;
    public float QuestLodBias = 0.7f;

    public void Apply()
    {
        bool quest = DemoPlatform.Standalone;
        QualitySettings.pixelLightCount = quest ? QuestPixelLights : PixelLights;
        QualitySettings.shadows = quest ? ShadowQuality.HardOnly : ShadowQuality.All;
        QualitySettings.shadowResolution = quest ? ShadowResolution.Medium : ShadowResolution.VeryHigh;
        QualitySettings.shadowDistance = quest ? QuestShadowDistance : ShadowDistance;
        QualitySettings.antiAliasing = AntiAliasing;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.softParticles = false;
        QualitySettings.lodBias = quest ? QuestLodBias : 1;
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
