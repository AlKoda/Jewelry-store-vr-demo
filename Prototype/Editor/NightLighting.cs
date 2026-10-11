#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;
using L = NightStoreLayout;

// Night lighting of the night store, cheap at runtime: every scene light is Baked
// (moonlight, four streetlights, the police car's two headlights), so their direct
// light, soft shadows and two bounces live in lightmaps and light probes, and the
// glass and polished floor reflect three baked box-projected reflection probes. At
// runtime only the flashlight is a realtime light (and the diagnostic sun, off
// unless the instructor asks for it). Emission does the rest: headlamp lenses,
// streetlight lenses, the light bar (blinking by property block), and the exit
// sign, keypad and lit windows across the street, which also bake a faint spill.
// Store fixtures stay off: no lights and black lens emission.
//
// Apply after NightStoreBuilder.Build, Bake after the scene is saved; without a bake
// UseRealtimeFallback makes the same lights realtime and shadowless so the scene
// still reads. Intensities assume the lightmapper's inverse-square falloff (baked)
// and Unity's legacy falloff (realtime fallback, flashlight), in linear colour space.
public static class NightLighting
{
    public const string GroupName = "Lighting";
    public const string SettingsPath = "Assets/CrimeSceneDemo/NightLighting.lighting";
    public const string MaterialFolder = NightStoreBuilder.Folder;

    public static readonly Vector3 HeadlightTarget = V(0, 1.0f, 2.0f);
    public static readonly Color MoonColour = new Color(0.55f, 0.65f, 1.0f);
    public static readonly Color StreetColour = new Color(1.0f, 0.82f, 0.6f);
    public static readonly Color HeadlightColour = new Color(1.0f, 0.97f, 0.9f);
    // Linear values; RenderSettings take gamma-space colours, hence .gamma where applied.
    public static readonly Color NightAmbient = new Color(0.015f, 0.018f, 0.028f);
    public static readonly Color FallbackAmbient = new Color(0.045f, 0.05f, 0.065f);

    // Baked (inverse square): a headlight gives ~0.17 at the frontage 9 m away and
    // ~0.08 at the central cases; a streetlight ~0.28 on the ground 4.2 m below.
    public const float MoonIntensity = 0.05f, StreetlightIntensity = 5f, HeadlightIntensity = 14f;
    // Realtime legacy falloff at the same distances needs far less.
    public const float StreetlightRealtime = 1.2f, HeadlightRealtime = 1.2f;
    public const float FlashlightIntensity = 2.8f;
    public const float StreetlightRange = 11f, HeadlightRange = 18f, HeadlightAngle = 55f;

    // The baked lights sit clear of their own static housings (the lamp head, the
    // car's nose), which would otherwise occlude them in the bake.
    public const float HeadlightClearance = 0.15f;
    public static Vector3 LampPosition(int i) => L.StreetlightHead(i) + V(0, -0.3f, 0);

    // Box-projected reflection probes: name, centre, size.
    public static readonly string[] ProbeNames = { "ReflectionProbe_Showroom", "ReflectionProbe_Office", "ReflectionProbe_Exterior" };
    private static readonly Vector3[] ProbeCentres = { V(0, 1.5f, 4.25f), V(0, 1.5f, 10.25f), V(2, 2.5f, -5) };
    private static readonly Vector3[] ProbeSizes = { V(16, 3, 8.5f), V(6, 3, 3.5f), V(24, 6, 12) };
    public const int ReflectionResolution = 128;

    // Project-wide: the night bake and its intensities assume linear lighting. Older
    // scenes stay valid but render in linear from now on.
    public static void UseLinearColorSpace()
    {
        if (PlayerSettings.colorSpace != ColorSpace.Linear) PlayerSettings.colorSpace = ColorSpace.Linear;
    }

    // Builds NightStore/Lighting and sets the environment and bake settings. Removes
    // the validator's preview sun (its grey ambient is replaced here).
    public static Transform Apply(Transform root, Camera camera)
    {
        GameObject preview = GameObject.Find("PreviewSun");
        if (preview != null) Object.DestroyImmediate(preview);
        Transform group = Group(GroupName, root);
        Light moon = Lights(root, group, out Light sun);
        Environment(moon);
        Emitters(root);
        Probes(group);
        ReflectionProbes(group);
        LightmapScales(root);
        Settings();
        group.gameObject.AddComponent<NightQuality>();
        DemoLighting lighting = Object.FindFirstObjectByType<DemoLighting>(FindObjectsInactive.Include);
        if (lighting != null)
        {
            lighting.BakedNight = true;
            lighting.DiagnosticSun = sun;
            lighting.FlashlightIntensity = FlashlightIntensity;
            lighting.NightSky = null;
            EditorUtility.SetDirty(lighting);
        }
        if (camera != null) ConfigureCamera(camera);
        return group;
    }

    public static void ConfigureCamera(Camera camera) { NightQuality.Configure(camera); }

    // ---------------------------------------------------------------- lights

    private static Light Lights(Transform root, Transform group, out Light sun)
    {
        // Low moon from behind the street-side buildings: it rims the roofs and pavement;
        // the roof slab keeps it out of the store.
        Light moon = MakeLight("Moonlight", group, LightType.Directional, MoonColour, MoonIntensity, V(0, 12, -20), Quaternion.Euler(38, 25, 0));
        moon.shadowAngle = 2;

        // Streetlights: wide spots straight down from the lamp heads, soft baked pools.
        Transform posts = root.Find("Exterior/Streetlights");
        for (int i = 0; i < L.Streetlights.Length; i++)
        {
            Vector3 head = root.TransformPoint(L.StreetlightHead(i));
            Light lamp = MakeLight("Streetlight_" + i, group, LightType.Spot, StreetColour, StreetlightIntensity, root.TransformPoint(LampPosition(i)), Quaternion.Euler(90, 0, 0));
            lamp.spotAngle = 120;
            lamp.innerSpotAngle = 70;
            lamp.range = StreetlightRange;
            lamp.shadowRadius = 0.15f;
            Transform post = posts != null ? posts.Find("Streetlight_" + i) : null;
            if (post != null)
            {
                GameObject lens = Box("Lens", post, post.InverseTransformPoint(head + V(0, -0.1f, 0)), V(0.4f, 0.03f, 0.2f), Emitter("NightStreetLens", StreetColour * 0.4f, StreetColour * 3f));
                lens.transform.localRotation = Quaternion.Euler(0, L.StreetlightYaw(i), 0);
                Unlit(lens);
            }
        }

        // Headlights: aimed at the entrance, so the cases cast long shadows into the
        // store and the facade walls block the beam either side of the glass.
        Transform car = root.Find("Exterior/PoliceCar");
        foreach (string side in new[] { "Left", "Right" })
        {
            Transform anchor = car.Find("Headlight" + side);
            Vector3 p = anchor.position + anchor.forward * HeadlightClearance;
            Light beam = MakeLight("Headlight_" + side, group, LightType.Spot, HeadlightColour, HeadlightIntensity, p, Quaternion.LookRotation(HeadlightTarget - p));
            beam.spotAngle = HeadlightAngle;
            beam.innerSpotAngle = 30;
            beam.range = HeadlightRange;
            beam.shadowRadius = 0.05f;
            // Lens quad on the car's nose (a quad faces -z; the nose is the anchor's +z).
            GameObject lens = Primitive("Lens", PrimitiveType.Quad, anchor, V(0, 0, 0.01f), V(0.3f, 0.12f, 1), Emitter("NightHeadlampLens", HeadlightColour, HeadlightColour * 3f));
            lens.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Unlit(lens);
        }

        // Instructor's diagnostic daylight (DemoLighting.SetNight(false)): realtime, no shadows, off.
        sun = MakeLight("DiagnosticSun", group, LightType.Directional, new Color(1f, 0.96f, 0.9f), 1.0f, V(0, 15, 0), Quaternion.Euler(50, 150, 0));
        sun.lightmapBakeType = LightmapBakeType.Realtime;
        sun.shadows = LightShadows.None;
        sun.enabled = false;
        return moon;
    }

    private static Light MakeLight(string name, Transform group, LightType type, Color colour, float intensity, Vector3 position, Quaternion rotation)
    {
        Transform t = Group(name, group);
        t.SetPositionAndRotation(position, rotation);
        Light light = t.gameObject.AddComponent<Light>();
        light.type = type;
        light.color = colour;
        light.intensity = intensity;
        light.lightmapBakeType = LightmapBakeType.Baked;
        light.shadows = LightShadows.Soft;
        return light;
    }

    // Police light bar (red and blue halves on the roof, LightBarBlink alternates them).
    private static void LightBar(Transform root)
    {
        Transform anchor = root.Find("Exterior/PoliceCar/LightBar");
        Color red = new Color(0.5f, 0.03f, 0.02f), blue = new Color(0.04f, 0.1f, 0.5f);
        GameObject left = Box("Red", anchor, V(-0.2f, 0, 0), V(0.38f, 0.07f, 0.16f), Emitter("NightLightBarRed", red, red * 0.1f));
        GameObject right = Box("Blue", anchor, V(0.2f, 0, 0), V(0.38f, 0.07f, 0.16f), Emitter("NightLightBarBlue", blue, blue * 0.1f));
        Unlit(left);
        Unlit(right);
        LightBarBlink blink = anchor.gameObject.AddComponent<LightBarBlink>();
        blink.Red = left.GetComponent<Renderer>();
        blink.Blue = right.GetComponent<Renderer>();
        blink.Apply(0); // Edit-mode captures show the red half lit (property blocks are not saved).
    }

    // Emissive Standard material for the non-static emitters (the lights do the lighting).
    private static Material Emitter(string name, Color albedo, Color emission)
    {
        Material m = Mat(MaterialFolder, name, albedo, 0.8f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emission);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void Unlit(GameObject g)
    {
        Renderer r = g.GetComponent<Renderer>();
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    // ---------------------------------------------------------------- emission and environment

    // Exit sign and keypad are below StoreKit's small-prop size, so MarkStatic left them
    // out of GI; as GI contributors (still probe-lit) their emission bakes a faint spill.
    private static void Emitters(Transform root)
    {
        LightBar(root);
        foreach (string path in new[] { "Showroom/ExitSign", "Showroom/AlarmKeypad" })
        {
            Transform t = root.Find(path);
            if (t == null) continue;
            foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, GameObjectUtility.GetStaticEditorFlags(r.gameObject) | StaticEditorFlags.ContributeGI);
                r.receiveGI = ReceiveGI.LightProbes;
            }
        }
    }

    // Very dark blue flat ambient (baked into the lightmaps and probes), a dark
    // procedural sky as background and default reflection, exponential blue-grey fog.
    private static void Environment(Light moon)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = NightAmbient.gamma;
        RenderSettings.ambientIntensity = 1;
        RenderSettings.sun = moon;
        RenderSettings.skybox = NightSky();
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.defaultReflectionResolution = ReflectionResolution;
        RenderSettings.reflectionIntensity = 1;
        RenderSettings.reflectionBounces = 1;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.10f, 0.11f, 0.14f);
        RenderSettings.fogDensity = 0.01f;
    }

    // Skybox/Procedural lit by the moon, no sun disc, exposed down to a deep night blue.
    private static Material NightSky()
    {
        string path = MaterialFolder + "/NightStoreSky.mat";
        Shader shader = Shader.Find("Skybox/Procedural");
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else m.shader = shader;
        m.SetFloat("_SunDisk", 0);
        m.EnableKeyword("_SUNDISK_NONE");
        m.DisableKeyword("_SUNDISK_SIMPLE");
        m.DisableKeyword("_SUNDISK_HIGH_QUALITY");
        m.SetFloat("_AtmosphereThickness", 0.6f);
        m.SetColor("_SkyTint", new Color(0.35f, 0.4f, 0.6f));
        m.SetColor("_GroundColor", new Color(0.05f, 0.05f, 0.06f));
        m.SetFloat("_Exposure", 0.06f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------------------------------------------------------- probes

    // Light probes for everything probe-lit (props, the player's tools, dynamic
    // objects): 1 m inside, 2 m on the accessible exterior, a row along the buildings
    // across the street, and extra probes along the headlight beams' edges where the
    // light changes fastest. Probes inside solid fixtures are skipped.
    private static void Probes(Transform group)
    {
        List<Vector3> points = new List<Vector3>();
        float[] heights = { 0.2f, 1.0f, 2.0f };
        ProbeGrid(points, L.MinX + 0.5f, L.MaxX - 0.5f, L.MinZ + 0.5f, L.RearZ - 0.4f, 1, heights);
        ProbeGrid(points, -L.OfficeHalf + 0.5f, L.OfficeHalf - 0.5f, L.RearZ + 0.5f, L.MaxZ - 0.4f, 1, heights);
        ProbeGrid(points, -L.PavementHalfX, L.PavementHalfX, L.StagingBay.MinZ, -1, 2, heights);
        ProbeGrid(points, -24, 24, L.FarPavementZ + 1.5f, L.FarPavementZ + 1.5f, 4, new[] { 0.5f, 3f, 7f });
        foreach (string side in new[] { "Left", "Right" })
        {
            Transform beam = group.Find("Headlight_" + side);
            if (beam == null) continue;
            foreach (float yaw in new[] { -HeadlightAngle * 0.5f, HeadlightAngle * 0.5f })
            {
                Vector3 edge = Quaternion.AngleAxis(yaw, Vector3.up) * beam.forward;
                for (float d = 2; d <= 12; d += 2)
                {
                    Vector3 p = beam.position + edge * d;
                    foreach (float y in heights) points.Add(new Vector3(p.x, Floor(p.z) + y, p.z));
                }
            }
        }
        Physics.SyncTransforms();
        Vector3[] local = new Vector3[points.Count];
        int kept = 0;
        Transform holder = Group("LightProbes", group);
        foreach (Vector3 p in points)
            if (!Physics.CheckSphere(p, 0.08f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) local[kept++] = holder.InverseTransformPoint(p);
        System.Array.Resize(ref local, kept);
        holder.gameObject.AddComponent<LightProbeGroup>().probePositions = local;
    }

    // Ground height: the street between the kerbs is 0.12 m below the pavements.
    private static float Floor(float z) => z < L.KerbZ && z > L.FarKerbZ ? L.StreetY : 0;

    private static void ProbeGrid(List<Vector3> points, float x0, float x1, float z0, float z1, float step, float[] heights)
    {
        for (float x = x0; x <= x1 + 0.01f; x += step)
            for (float z = z0; z <= z1 + 0.01f; z += step)
                foreach (float y in heights) points.Add(new Vector3(x, Floor(z) + y, z));
    }

    private static void ReflectionProbes(Transform group)
    {
        for (int i = 0; i < ProbeNames.Length; i++)
        {
            Transform t = Group(ProbeNames[i], group);
            t.position = ProbeCentres[i];
            ReflectionProbe probe = t.gameObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.boxProjection = true;
            probe.center = Vector3.zero;
            probe.size = ProbeSizes[i];
            probe.resolution = ReflectionResolution;
            probe.hdr = true;
            probe.intensity = 1;
            // The small office probe wins where it overlaps the showroom's.
            probe.importance = i == 1 ? 2 : 1;
        }
    }

    // ---------------------------------------------------------------- lightmaps

    // Texel density by zone: the interior shell full, fixtures 0.8, kit models 0.5, the
    // street and pavement 0.3 and what is mostly out of view (far pavement, buildings) 0.2.
    private static void LightmapScales(Transform root)
    {
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r.receiveGI != ReceiveGI.Lightmaps) continue;
            string path = PathUnder(r.transform, root);
            float scale = 0.8f;
            if (path.StartsWith("Architecture/")) scale = 1;
            else if (path.StartsWith("Exterior/Ground/FarPavement") || path.StartsWith("Exterior/Buildings/")) scale = 0.2f;
            else if (path.StartsWith("Exterior/Ground/")) scale = 0.3f;
            else if (path.StartsWith("Exterior/")) scale = 0.5f;
            r.scaleInLightmap = scale;
        }
    }

    private static string PathUnder(Transform t, Transform root)
    {
        string path = t.name;
        for (t = t.parent; t != null && t != root; t = t.parent) path = t.name + "/" + path;
        return path;
    }

    // Saved LightingSettings asset, assigned to the scene. All lights are Baked, so the
    // mixed-lighting mode is irrelevant; non-directional lightmaps halve the memory.
    private static void Settings()
    {
        LightingSettings s = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
        if (s == null)
        {
            s = new LightingSettings();
            s.name = "NightLighting";
            AssetDatabase.CreateAsset(s, SettingsPath);
        }
        s.autoGenerate = false;
        s.bakedGI = true;
        s.realtimeGI = false;
        // Falls back to the CPU lightmapper where no supported GPU is present.
        s.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        s.lightmapResolution = LightmapTexelsPerMetre;
        s.lightmapMaxSize = 1024;
        s.lightmapPadding = 2;
        s.directSampleCount = 32;
        s.indirectSampleCount = 256;
        s.environmentSampleCount = 128;
        s.maxBounces = 2;
        s.ao = true;
        s.aoMaxDistance = 0.6f;
        s.lightmapCompression = LightmapCompression.NormalQuality;
        s.directionalityMode = LightmapsMode.NonDirectional;
        EditorUtility.SetDirty(s);
        AssetDatabase.SaveAssets();
        Lightmapping.lightingSettings = s;
    }

    // Synchronous bake of the saved, open scene (lightmaps, light probes and the baked
    // reflection probes); works in batch mode. Returns false when the bake fails.
    public static bool Bake(out string report)
    {
        Lightmapping.Clear();
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        bool baked = Lightmapping.Bake();
        watch.Stop();
        LightmapData[] maps = LightmapSettings.lightmaps;
        int largest = 0;
        foreach (LightmapData map in maps)
            if (map.lightmapColor != null) largest = Mathf.Max(largest, Mathf.Max(map.lightmapColor.width, map.lightmapColor.height));
        int probes = LightmapSettings.lightProbes != null ? LightmapSettings.lightProbes.count : 0;
        report = "Night bake " + (baked ? "completed" : "FAILED") + " in " + watch.Elapsed.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
            + " s; lightmaps=" + maps.Length + " (largest " + largest + " px); light probes=" + probes
            + "; lightmapper=" + Lightmapping.lightingSettings.lightmapper;
        return baked;
    }

    // RunNightQuick: no bake, so baked lights would not render at all. They become
    // realtime and shadowless at legacy-falloff intensities; the headlights take pixel
    // lights, the far streetlights are vertex-lit, and the flat ambient is lifted to
    // stand in for the missing bounce. A preview only, never the shipped look.
    public static void UseRealtimeFallback(Transform root)
    {
        Transform group = root.Find(GroupName);
        foreach (Light light in group.GetComponentsInChildren<Light>(true))
        {
            if (light.name == "DiagnosticSun") continue;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.shadows = LightShadows.None;
            if (light.name.StartsWith("Headlight_"))
            {
                light.intensity = HeadlightRealtime;
                light.renderMode = LightRenderMode.ForcePixel;
            }
            else if (light.name.StartsWith("Streetlight_"))
            {
                light.intensity = StreetlightRealtime;
                bool far = L.Streetlights[int.Parse(light.name.Substring(12))].z < L.FarKerbZ * 0.5f;
                light.renderMode = far ? LightRenderMode.ForceVertex : LightRenderMode.Auto;
            }
        }
        RenderSettings.ambientLight = FallbackAmbient.gamma;
        Debug.LogWarning("NightLighting: not baked (RunNightQuick). Baked lights switched to realtime without shadows and the ambient lifted; run RunNight for the real look.");
    }
}
#endif
