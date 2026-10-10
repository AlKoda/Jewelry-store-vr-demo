using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Lighting presets for the instructor: the shop as found by day, or the same
// shop at night with the lights dimmed, the ceiling fixtures dark and the fog
// closer, plus a handheld flashlight on the desktop view or the right hand. The
// originals are collected once from the scene and restored exactly; a session
// reset returns to day with the flashlight off.
public sealed class DemoLighting : MonoBehaviour
{
    public DemoSession Session;
    public float TransitionSeconds = 1f;
    public float NightLightFactor = 0.25f;
    public float NightAmbientFactor = 0.3f;
    public float NightFogFactor = 0.35f;
    public Color FlashlightColor = new Color(1f, 0.95f, 0.85f);

    public bool Night { get; private set; }
    public Light FlashlightLight { get; private set; }
    public bool FlashlightOn => FlashlightLight != null && FlashlightLight.enabled && FlashlightLight.gameObject.activeSelf;

    private const string FixtureName = "CeilingLight";
    private const string EmissionProperty = "_EmissionColor";

    private Light[] lights = new Light[0];
    private float[] intensities = new float[0];
    private Renderer[] fixtures = new Renderer[0];
    private Color[] emissions = new Color[0];
    private Color sky, equator, ground, fogColor, background;
    private float ambientIntensity, fogStart, fogEnd, fogDensity;
    private Camera view;
    private DemoModeSwitch mode;
    private bool collected, flashlightInVR;
    // 0 is day, 1 is night; the transition moves it between the two.
    private float blend;
    private Coroutine transition;
    private static MaterialPropertyBlock block;

    private void Start() { Collect(); }

    private void OnEnable()
    {
        if (Session != null) Session.SessionReset.AddListener(OnReset);
    }

    private void OnDisable()
    {
        if (Session != null) Session.SessionReset.RemoveListener(OnReset);
    }

    private void OnReset()
    {
        if (Night) SetNight(false);
        Flashlight(false);
    }

    // The flashlight follows the active mode: F9 may switch it while the light is on.
    private void Update()
    {
        if (FlashlightOn && mode != null && mode.VRActive != flashlightInVR) Attach();
    }

    public void ToggleNight() { SetNight(!Night); }

    public void SetNight(bool night)
    {
        Collect();
        Night = night;
        if (transition != null) StopCoroutine(transition);
        transition = null;
        if (TransitionSeconds > 0 && isActiveAndEnabled) transition = StartCoroutine(Transition(night ? 1 : 0));
        else Apply(night ? 1 : 0);
    }

    // Finishes the running transition at once; validation uses it, a presenter never needs to.
    public void Complete()
    {
        if (transition != null) StopCoroutine(transition);
        transition = null;
        Apply(Night ? 1 : 0);
    }

    private IEnumerator Transition(float target)
    {
        float from = blend;
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / TransitionSeconds)
        {
            Apply(Mathf.Lerp(from, target, t));
            yield return null;
        }
        Apply(target);
        transition = null;
    }

    private void Apply(float k)
    {
        blend = k;
        float light = Mathf.Lerp(1, NightLightFactor, k), ambient = Mathf.Lerp(1, NightAmbientFactor, k), fog = Mathf.Lerp(1, NightFogFactor, k);
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].intensity = intensities[i] * light;
        if (block == null) block = new MaterialPropertyBlock();
        for (int i = 0; i < fixtures.Length; i++)
        {
            if (fixtures[i] == null) continue;
            block.Clear();
            // By day the block stays empty so the saved material shows untouched.
            if (k > 0) block.SetColor(EmissionProperty, Color.Lerp(emissions[i], Color.black, k));
            fixtures[i].SetPropertyBlock(block);
        }
        RenderSettings.ambientSkyColor = sky * ambient;
        RenderSettings.ambientLight = sky * ambient;
        RenderSettings.ambientEquatorColor = equator * ambient;
        RenderSettings.ambientGroundColor = ground * ambient;
        RenderSettings.ambientIntensity = ambientIntensity * ambient;
        // Night fog: darker and closer, so the street fades a few metres beyond the window.
        RenderSettings.fogColor = fogColor * fog;
        RenderSettings.fogStartDistance = fogStart * Mathf.Lerp(1, 0.6f, k);
        RenderSettings.fogEndDistance = fogEnd * Mathf.Lerp(1, 0.6f, k);
        RenderSettings.fogDensity = fogDensity * Mathf.Lerp(1, 1.6f, k);
        if (view != null) view.backgroundColor = background * fog;
    }

    // Originals, taken once: every scene light except the flashlight, the ceiling
    // fixtures' emission, the ambient trilight, the fog and the view's clear colour.
    private void Collect()
    {
        if (collected) return;
        collected = true;
        List<Light> found = new List<Light>();
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l != FlashlightLight) found.Add(l);
        lights = found.ToArray();
        intensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) intensities[i] = lights[i].intensity;
        List<Renderer> lit = new List<Renderer>();
        foreach (MeshRenderer r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if (r.name == FixtureName && r.sharedMaterial != null && r.sharedMaterial.HasProperty(EmissionProperty)) lit.Add(r);
        fixtures = lit.ToArray();
        emissions = new Color[fixtures.Length];
        for (int i = 0; i < fixtures.Length; i++) emissions[i] = fixtures[i].sharedMaterial.GetColor(EmissionProperty);
        sky = RenderSettings.ambientSkyColor;
        equator = RenderSettings.ambientEquatorColor;
        ground = RenderSettings.ambientGroundColor;
        ambientIntensity = RenderSettings.ambientIntensity;
        fogColor = RenderSettings.fogColor;
        fogStart = RenderSettings.fogStartDistance;
        fogEnd = RenderSettings.fogEndDistance;
        fogDensity = RenderSettings.fogDensity;
        // Only a view clearing to a colour is dimmed; a skybox is left as it is.
        view = Camera.main != null && Camera.main.clearFlags == CameraClearFlags.SolidColor ? Camera.main : null;
        if (view != null) background = view.backgroundColor;
        mode = FindFirstObjectByType<DemoModeSwitch>();
    }

    public void ToggleFlashlight() { Flashlight(!FlashlightOn); }

    // Warm shadowless spot light, created on first use and re-attached whenever it
    // is switched on, since the holder depends on the mode at that moment.
    public void Flashlight(bool on)
    {
        if (FlashlightLight == null)
        {
            if (!on) return;
            GameObject torch = new GameObject("Flashlight");
            FlashlightLight = torch.AddComponent<Light>();
            FlashlightLight.type = LightType.Spot;
            FlashlightLight.range = 12;
            FlashlightLight.spotAngle = 40;
            FlashlightLight.intensity = 2.5f;
            FlashlightLight.color = FlashlightColor;
            FlashlightLight.shadows = LightShadows.None;
        }
        if (on) Attach();
        FlashlightLight.enabled = on;
        // Off means inactive too, so the scene's light budget checks do not count it.
        FlashlightLight.gameObject.SetActive(on);
    }

    // The main camera on the desktop; the right hand while VR drives the rig.
    private void Attach()
    {
        if (mode == null) mode = FindFirstObjectByType<DemoModeSwitch>();
        flashlightInVR = mode != null && mode.VRActive;
        Transform holder = null;
        if (flashlightInVR)
            foreach (XRControllerInput hand in FindObjectsByType<XRControllerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (hand.Node == XRNode.RightHand) holder = hand.transform;
        if (holder == null) holder = Camera.main != null ? Camera.main.transform : transform;
        FlashlightLight.transform.SetParent(holder, false);
        FlashlightLight.transform.localPosition = Vector3.zero;
        FlashlightLight.transform.localRotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        if (FlashlightLight != null) Destroy(FlashlightLight.gameObject);
    }
}
