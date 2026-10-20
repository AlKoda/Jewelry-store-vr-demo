using UnityEngine;
using UnityEngine.SceneManagement;

// One place that knows whether the demo runs on the Quest 3 by itself (an Android build,
// Builds/Quest/JewelryStoreDemo.apk) or on the PC (keyboard, or the headset over Link).
// The same scene and code serve both: on the headset alone the realtime lights use
// smaller, hard shadow maps, the headlights' specular companions and the PC-monitor views
// (presenter panel, overview map, language button) are switched off, and NightQuality applies its Quest tier.
// The baked lighting, materials and every interaction stay the same.
// ForceStandalone lets the Editor and the validators exercise the Quest tier on the PC.
public static class DemoPlatform
{
    public static bool ForceStandalone;

    public static bool Standalone => ForceStandalone || Application.platform == RuntimePlatform.Android;

    // Shadow map size: the PC value on the PC, half of it (at least 256) on the headset alone.
    public static int ShadowResolution(int pc) => Standalone ? Mathf.Max(256, pc / 2) : pc;

    // Soft shadows cost several taps per pixel on the mobile GPU; the headset alone uses hard ones.
    public static LightShadows ShadowType(LightShadows pc) => Standalone && pc == LightShadows.Soft ? LightShadows.Hard : pc;

    // Shapes a handheld or placed light for the platform: one call where the light is set up.
    public static void TuneShadows(Light light, int pcResolution)
    {
        if (light == null) return;
        light.shadows = ShadowType(light.shadows);
        light.shadowCustomResolution = ShadowResolution(pcResolution);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Standalone) Lighten(scene);
    }

    // Scene-wide changes for the headset alone; returns how many objects it changed (validators).
    public static int Lighten(Scene scene)
    {
        int changed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // The headlights' realtime specular companions (SceneReviewPass): two extra shadow maps
            // and per-pixel passes for a subtle highlight; the baked headlights stay.
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
                if (light.name.EndsWith("_Specular") && light.enabled) { light.enabled = false; changed++; }
            // Nobody sees the PC monitor: its panel and map would only spend the headset's frame time.
            foreach (DemoOverviewMap map in root.GetComponentsInChildren<DemoOverviewMap>(true))
                if (map.enabled) { map.enabled = false; changed++; }
            foreach (DemoDesktopPanel panel in root.GetComponentsInChildren<DemoDesktopPanel>(true))
                if (panel.enabled) { panel.enabled = false; changed++; }
            foreach (LanguageSwitcher switcher in root.GetComponentsInChildren<LanguageSwitcher>(true))
                if (switcher.enabled) { switcher.enabled = false; changed++; }
        }
        return changed;
    }
}
