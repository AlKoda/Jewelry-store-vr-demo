#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;
using L = NightStoreLayout;

// Night lighting of the night store, cheap at runtime: every scene light is Baked
// (moonlight, four streetlights shaped by the NightSky street-lamp cookie, the police
// car's two headlights), so their direct light, soft shadows and two bounces live in
// lightmaps and light probes, and the glass and polished floor reflect four baked
// box-projected reflection probes. At runtime only the flashlight is a realtime light
// (and the diagnostic sun, off unless the instructor asks for it). Emission does the
// rest: headlamp lenses, streetlight lenses, the light bar (blinking by property
// block), and the exit sign, keypad and lit windows across the street, which also bake
// a faint spill; additive glow cards add halos round the lenses and the light bar's
// red and blue wash on the street and the shop floor, without a light.
// Store fixtures stay off: no lights and black lens emission. The streetlights follow
// NightStoreLayout.Lamp (the StreetModels lantern or the KayKit streetlight), the car's
// lenses and light bar the car the builder placed (PoliceLook).
//
// Apply after NightStoreBuilder.Build, Bake after the scene is saved; without a bake
// UseRealtimeFallback makes the same lights realtime and shadowless so the scene
// still reads. Intensities assume the lightmapper's inverse-square falloff (baked)
// and Unity's legacy falloff (realtime fallback, flashlight), in linear colour space
// with linear light intensities (UseLinearColorSpace sets both).
public static class NightLighting
{
    public const string GroupName = "Lighting";
    public const string SettingsPath = "Assets/CrimeSceneDemo/NightLighting.lighting";
    public const string MaterialFolder = NightStoreBuilder.Folder;

    // The headlights follow the car's aim, raised to case height.
    public static readonly Vector3 HeadlightTarget = L.CarAim + Vector3.up * 1.0f;
    public static readonly Color MoonColour = new Color(0.55f, 0.65f, 1.0f);
    public static readonly Color StreetColour = new Color(1.0f, 0.82f, 0.6f);
    public static readonly Color HeadlightColour = new Color(1.0f, 0.83f, 0.63f);
    // Linear values; RenderSettings take gamma-space colours, hence .gamma where applied.
    public static readonly Color NightAmbient = new Color(0.015f, 0.018f, 0.028f);
    public static readonly Color FallbackAmbient = new Color(0.045f, 0.05f, 0.065f);

    // Baked (inverse square): a headlight gives ~0.17 at the frontage 9 m away and
    // ~0.08 at the central cases. A streetlight's intensities and spot angles come from
    // the lamp profile (NightStoreLayout.Lamp): ~0.28 on the ground under the KayKit
    // head, the lower lantern scaled to the same pool.
    public const float MoonIntensity = 0.05f, HeadlightIntensity = 2.5f;
    // Realtime legacy falloff at the same distances needs far less.
    public const float HeadlightRealtime = 1.2f;
    public const float FlashlightIntensity = 2.8f;
    public const float StreetlightRange = 11f, HeadlightRange = 18f, HeadlightAngle = 55f;

    // The baked lights sit clear of their own static housings (the KayKit head, the
    // car's nose), which would otherwise occlude them in the bake; the lantern casts no
    // shadow, so its light sits at the bulb (NightStoreLayout.StreetlightLight).
    public const float HeadlightClearance = 0.15f;
    public static Vector3 LampPosition(int i) => L.StreetlightLight(i);

    // NightSky files, each optional (PackImport imports the cookies as spot cookies, the
    // sky without mipmaps and the glow sprite as an additive colour map).
    public const string SkyPack = "NightSky";
    public const string StreetCookieFile = "cookie_streetlight_512.png", FlashlightCookieFile = "cookie_flashlight_led_512.png",
        UVTorchCookieFile = "cookie_uv_torch_512.png", LampCookieFile = "cookie_scene_floodlight_512.png",
        GlowSpriteFile = "glow_soft_256.png", OvercastSkyFile = "city_night_sky_overcast_1k.hdr";

    // The street-lamp cookie throws long along the kerb and cuts off sharply at its top
    // edge, which faces the houses: the spot (pointing down, Euler(90, yaw, 0)) turns so
    // its up axis is +z on the shop's side of the street and -z on the far side.
    public static bool NearSide(int i) => L.Streetlights[i].z > L.FarKerbZ * 0.5f;
    public static float StreetCookieYaw(int i) => NearSide(i) ? 0 : 180;

    // Glow cards under Lighting/Glow: halos round the lenses (size in metres, tint with
    // the additive strength in alpha) and the light bar's washes.
    public const string GlowGroupName = "Glow";
    public const float StreetHaloSize = 1.0f, HeadlampHaloSize = 0.6f, SpawnEyeHeight = 1.6f;
    public static readonly Color StreetHaloTint = new Color(1f, 0.8f, 0.55f, 0.12f), HeadlampHaloTint = new Color(1f, 0.97f, 0.9f, 0.1f);
    public static readonly Vector2 StreetWashSize = new Vector2(5, 5), FloorWashSize = new Vector2(4, 3);
    public static readonly Vector3 ShopFloorWash = V(0, 0.004f, 1.8f);

    // Box-projected reflection probes: name, centre, size.
    public static readonly string[] ProbeNames = { "ReflectionProbe_Showroom", "ReflectionProbe_Office", "ReflectionProbe_Exterior", "ReflectionProbe_AcrossStreet" };
    // Boxes from the layout: the showroom and office volumes, the pavement and staging
    // bay out to the car (the box projection must match the walls), and the far pavement
    // in front of the row across the street, so its windows reflect the baked street
    // rather than the sky.
    private static readonly Bounds[] ProbeBoxes =
    {
        ProbeBox(V(L.MinX, 0, L.MinZ), V(L.MaxX, L.CeilingY, L.RearZ)),
        ProbeBox(V(-L.OfficeHalf, 0, L.RearZ), V(L.OfficeHalf, L.CeilingY, L.MaxZ)),
        ProbeBox(V(-L.PavementHalfX, -0.5f, L.StagingBay.MinZ - 3), V(L.PavementHalfX + L.CarPosition.x, 5.5f, L.MinZ + 1)),
        ProbeBox(V(-26, -0.5f, L.FarPavementZ - 0.5f), V(28, 14, L.FarKerbZ + 2)),
    };
    public static Bounds ProbeBoxOf(int i) => ProbeBoxes[i];
    private static Bounds ProbeBox(Vector3 min, Vector3 max) { Bounds b = new Bounds(); b.SetMinMax(min, max); return b; }
    public const int ReflectionResolution = 256; // the 0.95-smooth glass samples about mip 0.5, so the probe size sets its sharpness
    public const int SkyReflectionResolution = 128; // the near-black night sky needs no more

    // Project-wide: the night bake and its intensities assume linear lighting and
    // linear light intensities (Built-in defaults to intensities applied in gamma,
    // i.e. (colour * intensity)^2.2, which would blow the headlights out and black out
    // the moon). Older scenes stay valid but render in linear from now on, and their
    // lights above or below intensity 1 shift accordingly.
    public static void UseLinearColorSpace()
    {
        if (PlayerSettings.colorSpace != ColorSpace.Linear) PlayerSettings.colorSpace = ColorSpace.Linear;
        if (!GraphicsSettings.lightsUseLinearIntensity) GraphicsSettings.lightsUseLinearIntensity = true;
    }

    // Lightmap texel scale (scaleInLightmap) by zone; the builder's KitMeshes size their
    // chart margins from the same values (KitMesh.LightmapScale).
    public const float ShellScale = 1, FixtureScale = 0.8f, GroundScale = 0.75f, KitScale = 0.5f, StreetScale = 0.3f, FarScale = 0.1f;

    // Builds NightStore/Lighting and sets the environment and bake settings. Removes
    // the validator's preview sun (its grey ambient is replaced here).
    public static Transform Apply(Transform root, Camera camera)
    {
        GameObject preview = GameObject.Find("PreviewSun");
        if (preview != null) Object.DestroyImmediate(preview);
        Transform group = Group(GroupName, root);
        Light moon = Lights(root, group, out Light sun);
        Environment(moon);
        LightBarBlink bar = Emitters(root);
        Glow(root, group, bar);
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
            lighting.LightBar = bar;
            // NightSky's torch cookies replace DemoToolsBuilder's generated one when present;
            // without the UV cookie the UV torch keeps the flashlight's.
            Texture2D flashlight = DemoAssetLibrary.Texture(SkyPack, FlashlightCookieFile);
            if (flashlight != null) lighting.FlashlightCookie = flashlight;
            lighting.UVTorchCookie = DemoAssetLibrary.Texture(SkyPack, UVTorchCookieFile);
            EditorUtility.SetDirty(lighting);
        }
        // Lit scene lamps project the rectangular LED floodlight (null: a plain spot).
        ToolStation station = Object.FindFirstObjectByType<ToolStation>(FindObjectsInactive.Include);
        if (station != null)
        {
            station.LampCookie = DemoAssetLibrary.Texture(SkyPack, LampCookieFile);
            EditorUtility.SetDirty(station);
        }
        if (camera != null) ConfigureCamera(camera);
        return group;
    }

    // The car the builder placed (NightStoreBuilder.PlacedCar), else the one PoliceLook
    // would place now (a lighting pass without a build in this editor session).
    public static PoliceLook.Car PlacedCar => string.IsNullOrEmpty(NightStoreBuilder.PlacedCar.Model) ? PoliceLook.ActiveCar : NightStoreBuilder.PlacedCar;

    public static void ConfigureCamera(Camera camera) { NightQuality.Configure(camera); }

    // ---------------------------------------------------------------- lights

    private static Light Lights(Transform root, Transform group, out Light sun)
    {
        // Low moon from behind the street-side buildings: it rims the roofs and pavement;
        // the roof slab keeps it out of the store.
        Light moon = MakeLight("Moonlight", group, LightType.Directional, MoonColour, MoonIntensity, V(0, 12, -20), Quaternion.Euler(38, 25, 0));
        moon.shadowAngle = 2;

        // Streetlights: wide spots straight down from the lamps, soft baked pools in the
        // cookie's street-lamp distribution, at the lamp profile's intensity and angles.
        L.LampProfile profile = L.Lamp;
        Texture2D cookie = DemoAssetLibrary.Texture(SkyPack, StreetCookieFile);
        Transform posts = root.Find("Exterior/Streetlights");
        for (int i = 0; i < L.Streetlights.Length; i++)
        {
            Light lamp = MakeLight("Streetlight_" + i, group, LightType.Spot, StreetColour, profile.BakedIntensity, root.TransformPoint(LampPosition(i)), Quaternion.Euler(90, StreetCookieYaw(i), 0));
            lamp.spotAngle = profile.SpotAngle;
            lamp.innerSpotAngle = profile.InnerSpotAngle;
            lamp.range = StreetlightRange;
            lamp.shadowRadius = 0.15f;
            if (cookie != null) lamp.cookie = cookie;
            Transform post = posts != null ? posts.Find("Streetlight_" + i) : null;
            if (post == null) continue;
            // Exactly one lens per post: a flat box under the KayKit head (or its stand-in),
            // along the arm, or a box round the lantern's bulb that fits inside its glass.
            Material emitter = Emitter("NightStreetLens", StreetColour * 0.4f, StreetColour * 3f);
            GameObject lens;
            if (profile.Arm)
            {
                Vector3 head = root.TransformPoint(L.StreetlightHead(i));
                lens = Box("Lens", post, post.InverseTransformPoint(head + V(0, -0.1f, 0)), profile.LensSize, emitter);
                lens.transform.localRotation = Quaternion.Euler(0, L.StreetlightYaw(i), 0);
            }
            else lens = Box("Lens", post, post.InverseTransformPoint(lamp.transform.position), profile.LensSize, emitter);
            ProbeLens(lens);
        }

        // Headlights: aimed at the entrance, so the cases cast long shadows into the
        // store and the facade walls block the beam either side of the glass. The lens
        // quads take the placed car's lens size (PoliceLook.Car.LensSize).
        Vector2 lensSize = PlacedCar.LensSize;
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
            GameObject lens = Primitive("Lens", PrimitiveType.Quad, anchor, V(0, 0, 0.01f), V(lensSize.x, lensSize.y, 1), Emitter("NightHeadlampLens", HeadlightColour, HeadlightColour * .7f));
            lens.transform.localRotation = Quaternion.Euler(0, 180, 0);
            ProbeLens(lens);
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

    // Police light bar: red and blue halves on the roof at +-LightBarCentre, each
    // 2 x LightBarHalf (PoliceLook: the 0.38 x 0.07 x 0.16 boxes at x +-0.2 on the KayKit
    // car, boxes round the Lightbar_glass on the US sedan). LightBarBlink alternates them
    // at the preset's colours and rate.
    private static LightBarBlink LightBar(Transform root)
    {
        PoliceLook.Car car = PlacedCar;
        PoliceLook.Preset look = PoliceLook.Active;
        Transform anchor = root.Find("Exterior/PoliceCar/LightBar");
        Color red = new Color(0.5f, 0.03f, 0.02f), blue = new Color(0.04f, 0.1f, 0.5f);
        Vector3 size = car.LightBarHalf * 2;
        GameObject left = Box("Red", anchor, V(-car.LightBarCentre, 0, 0), size, Emitter("NightLightBarRed", red, red * 0.1f));
        GameObject right = Box("Blue", anchor, V(car.LightBarCentre, 0, 0), size, Emitter("NightLightBarBlue", blue, blue * 0.1f));
        Unlit(left);
        Unlit(right);
        LightBarBlink blink = anchor.gameObject.AddComponent<LightBarBlink>();
        blink.Red = left.GetComponent<Renderer>();
        blink.Blue = right.GetComponent<Renderer>();
        blink.RedOn = look.LightBarLeft;
        blink.BlueOn = look.LightBarRight;
        blink.Hertz = look.LightBarHertz;
        return blink;
    }

    // Emissive Standard material for the emitters (the lights do the lighting; lenses are
    // probe-static only, the light bar dynamic).
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

    // Emissive lenses the baked reflection probes capture (no ContributeGI, so lightmaps
    // and the GI policy are unchanged): headlamp glints in the showroom probe (case glass,
    // jewellery, floor), streetlight glows in the exterior probe. The light bar blinks by
    // property block and stays dynamic.
    private static void ProbeLens(GameObject lens)
    {
        Unlit(lens);
        GameObjectUtility.SetStaticEditorFlags(lens, StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.BatchingStatic);
    }

    // ---------------------------------------------------------------- emission

    // Exit sign and keypad are below StoreKit's small-prop size, so MarkStatic left them
    // out of GI; as GI contributors (still probe-lit) their emission bakes a faint spill.
    // Returns the light bar's blinker.
    private static LightBarBlink Emitters(Transform root)
    {
        LightBarBlink bar = LightBar(root);
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
        return bar;
    }

    // ---------------------------------------------------------------- glow

    // Glow without lights (no Light, no shadow, no static flag, nothing in the bake):
    // additive camera-facing halos round every streetlight and headlamp lens, and two
    // dim horizontal washes the light bar tints with its lit half, on the street round
    // the car and on the shop floor inside the entrance. All share one material with the
    // NightSky soft-glow sprite and take their tints by property block (GlowBillboard and
    // LightBarBlink apply them at runtime, since a scene does not save property blocks).
    // Six halos and two washes: 8 transparent draws of 2 triangles. Without the sprite
    // there is no glow at all.
    private static void Glow(Transform root, Transform group, LightBarBlink bar)
    {
        bar.Washes = new Renderer[0];
        Material glow = GlowMaterial();
        if (glow == null)
        {
            bar.Apply(RedPhase(bar));
            return;
        }
        Transform holder = Group(GlowGroupName, group);
        List<Transform> halos = new List<Transform>();
        List<Vector3> anchors = new List<Vector3>();
        List<Color> tints = new List<Color>();
        // The lantern glass glows all round; a headlamp only towards its beam (the nose),
        // so its halo is gone from beside or behind the car (the arrival view).
        List<Vector3> facing = new List<Vector3>();
        Transform posts = root.Find("Exterior/Streetlights");
        for (int i = 0; i < L.Streetlights.Length; i++)
        {
            Transform lens = posts != null ? posts.Find("Streetlight_" + i + "/Lens") : null;
            if (lens == null) continue;
            halos.Add(GlowQuad("Halo_Streetlight_" + i, holder, lens.position, new Vector2(StreetHaloSize, StreetHaloSize), glow).transform);
            anchors.Add(holder.InverseTransformPoint(lens.position));
            tints.Add(StreetHaloTint);
            facing.Add(Vector3.zero);
        }
        Transform car = root.Find("Exterior/PoliceCar");
        foreach (string side in new[] { "Left", "Right" })
        {
            Transform lens = car != null ? car.Find("Headlight" + side + "/Lens") : null;
            if (lens == null) continue;
            halos.Add(GlowQuad("Halo_Headlight_" + side, holder, lens.position, new Vector2(HeadlampHaloSize, HeadlampHaloSize), glow).transform);
            anchors.Add(holder.InverseTransformPoint(lens.position));
            tints.Add(HeadlampHaloTint);
            facing.Add(holder.InverseTransformDirection(lens.parent.forward));
        }
        GlowBillboard billboard = holder.gameObject.AddComponent<GlowBillboard>();
        billboard.Quads = halos.ToArray();
        billboard.Anchors = anchors.ToArray();
        billboard.Tints = tints.ToArray();
        billboard.Facing = facing.ToArray();
        // Saved facing the arrival view and tinted for it; at runtime they turn to the
        // camera every frame.
        billboard.Face(root.TransformPoint(L.PlayerSpawn) + Vector3.up * SpawnEyeHeight);

        // Washes 4 mm above the street at the car and above the shop floor (Cull Off: the
        // quads lie face up either way).
        Vector3 street = root.TransformPoint(new Vector3(L.CarPosition.x, L.StreetY + 0.004f, L.CarPosition.z));
        GameObject streetWash = GlowQuad("Wash_Street", holder, street, StreetWashSize, glow);
        GameObject floorWash = GlowQuad("Wash_ShopFloor", holder, root.TransformPoint(ShopFloorWash), FloorWashSize, glow);
        streetWash.transform.rotation = floorWash.transform.rotation = root.rotation * Quaternion.Euler(90, 0, 0);
        bar.Washes = new[] { streetWash.GetComponent<Renderer>(), floorWash.GetComponent<Renderer>() };
        bar.Apply(RedPhase(bar));
    }

    // Edit-mode captures show the red half fully lit (mid-way through it, clear of the
    // crossfades); property blocks are not saved, so play mode starts from LightBarBlink.
    private static float RedPhase(LightBarBlink bar) => bar.Hertz > 0 ? 0.25f / bar.Hertz : 0;

    // A glow card: a quad without collider, shadows or probe sampling (the additive
    // shader is unlit), never static.
    private static GameObject GlowQuad(string name, Transform holder, Vector3 world, Vector2 size, Material glow)
    {
        GameObject quad = Primitive(name, PrimitiveType.Quad, holder, holder.InverseTransformPoint(world), V(size.x, size.y, 1), glow);
        MeshRenderer r = quad.GetComponent<MeshRenderer>();
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        GameObjectUtility.SetStaticEditorFlags(quad, 0);
        return quad;
    }

    // Legacy Particles/Additive (no lighting, no depth write, both faces, fogs to black)
    // with the soft-glow sprite. Its own tint is clear, so a card without a property block
    // adds nothing. null without the sprite.
    private static Material GlowMaterial()
    {
        Texture2D sprite = DemoAssetLibrary.Texture(SkyPack, GlowSpriteFile);
        Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (sprite == null || shader == null) return null;
        string path = MaterialFolder + "/NightGlow.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else m.shader = shader;
        m.mainTexture = sprite;
        m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0));
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------------------------------------------------------- environment

    // Very dark blue flat ambient (baked into the lightmaps and probes), the night sky as
    // background and default reflection, exponential blue-grey fog.
    private static void Environment(Light moon)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = NightAmbient.gamma;
        RenderSettings.ambientIntensity = 1;
        RenderSettings.sun = moon;
        RenderSettings.skybox = NightSkybox();
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.defaultReflectionResolution = SkyReflectionResolution;
        RenderSettings.reflectionIntensity = 1;
        RenderSettings.reflectionBounces = 1;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.10f, 0.11f, 0.14f);
        RenderSettings.fogDensity = 0.038f;
    }

    // The generated overcast night panorama when NightSky has it (a stratocumulus deck lit
    // from below by the city: no lamps, skyline or moon to contradict the scene's own
    // lights), else the procedural sky. The flat ambient keeps either out of the bake's
    // lighting; the box probes and the default reflection see it.
    private static Material NightSkybox()
    {
        Texture2D panorama = DemoAssetLibrary.Texture(SkyPack, OvercastSkyFile);
        return panorama != null ? Panorama(panorama) : NightSky();
    }

    // Skybox/Panoramic, latitude-longitude layout over 360 degrees, unrotated (the sky's
    // city glow then lies behind the far buildings), at the pack's radiance scale
    // (exposure 1: zenith about 0.013).
    private static Material Panorama(Texture panorama)
    {
        string path = MaterialFolder + "/NightStorePanorama.mat";
        Shader shader = Shader.Find("Skybox/Panoramic");
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else m.shader = shader;
        m.SetTexture("_MainTex", panorama);
        m.SetFloat("_Exposure", 1);
        m.SetFloat("_Rotation", 0);
        m.SetFloat("_Mapping", 1);
        m.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
        m.DisableKeyword("_MAPPING_6_FRAMES_LAYOUT");
        m.SetFloat("_ImageType", 0);
        m.SetFloat("_MirrorOnBack", 0);
        EditorUtility.SetDirty(m);
        return m;
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
    // Replaces the saved scene's probe group with a fresh one (new rows were added October 24 for
    // the spectators across the street); the caller bakes afterwards, probes carry no light until then.
    public static void RebuildProbes(Transform root)
    {
        Transform group = root.Find(GroupName);
        if (group == null) throw new System.Exception("Night store lighting group missing");
        Transform old = group.Find("LightProbes");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        Probes(group);
    }

    private static void Probes(Transform group)
    {
        List<Vector3> points = new List<Vector3>();
        float[] heights = { 0.2f, 1.0f, 2.0f };
        ProbeGrid(points, L.MinX + 0.5f, L.MaxX - 0.5f, L.MinZ + 0.5f, L.RearZ - 0.4f, 1, heights);
        ProbeGrid(points, -L.OfficeHalf + 0.5f, L.OfficeHalf - 0.5f, L.RearZ + 0.5f, L.MaxZ - 0.4f, 1, heights);
        ProbeGrid(points, -L.PavementHalfX, L.PavementHalfX, L.StagingBay.MinZ, -1, 2, heights);
        ProbeGrid(points, -24, 24, L.FarPavementZ + 1.5f, L.FarPavementZ + 1.5f, 4, new[] { 0.5f, 3f, 7f });
        // The spectators stand on the far pavement by the buildings (z about -21.5): two rows at
        // body heights there, so skinned people take the streetlight from probes instead of the
        // hull's edge value (they looked unlit and out of place, October 24).
        ProbeGrid(points, -8, 9, -22.2f, -19.4f, 1.4f, new[] { 0.3f, 1.0f, 1.8f });
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
        // Add standing and overhead samples near each accessible region centre;
        // larger imported props can remove the old grid samples during collision filtering.
        foreach (InteriorBounds.Region region in L.Regions())
        {
            float cx=(region.MinX+region.MaxX)*.5f, cz=(region.MinZ+region.MaxZ)*.5f;
            ProbeGrid(points,cx-1,cx+1,cz-1,cz+1,1,new[]{1.6f,2.4f});
        }
        Physics.SyncTransforms();
        foreach(var hit in Physics.OverlapSphere(new Vector3(0,1.6f,0),.08f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) Debug.Log("PROBE_THRESHOLD_BLOCKER " + PathUnder(hit.transform,group.root) + " " + hit.bounds);
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
            t.position = ProbeBoxes[i].center;
            ReflectionProbe probe = t.gameObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.boxProjection = true;
            probe.center = Vector3.zero;
            probe.size = ProbeBoxes[i].size;
            probe.resolution = ReflectionResolution;
            probe.hdr = true;
            probe.intensity = 1;
            // The small office probe wins where it overlaps the showroom's.
            probe.importance = i == 1 ? 2 : 1;
            // Unity's default 1 m blend would fade the office probe over most of its small box.
            probe.blendDistance = i == 1 ? 0.5f : 0.75f;
        }
    }

    // ---------------------------------------------------------------- lightmaps

    // Texel density by zone at DemoGeometry.LightmapTexelsPerMetre (20 per metre), first
    // match wins: the interior shell 20 (5 cm, where the headlights' case shadows fall);
    // fixtures and facade 16; the near pavement and kerbs 15 (where the player walks, under
    // the bollard and car shadows; a 64 m strip needs 960 px, inside lightmapMaxSize 1024);
    // the car, streetlights, table, easel and bollards 10; the street, the neighbour
    // terrace and the apartment block opposite 6 (its import margins are padded for 6);
    // the far pavement and the row across the street 2.
    private static void LightmapScales(Transform root)
    {
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r.receiveGI != ReceiveGI.Lightmaps) continue;
            string path = PathUnder(r.transform, root);
            float scale = FixtureScale;
            if (path.StartsWith("Architecture/")) scale = ShellScale;
            else if (path.StartsWith("Exterior/Buildings/Neighbours")) scale = StreetScale;
            else if (path.StartsWith("Exterior/Buildings/ApartmentBlock")) scale = StreetScale;
            else if (path.StartsWith("Exterior/Ground/FarPavement") || path.StartsWith("Exterior/Buildings/")) scale = FarScale;
            else if (path.StartsWith("Exterior/Ground/Street")) scale = StreetScale;
            else if (path.StartsWith("Exterior/Ground/")) scale = GroundScale;
            else if (path.StartsWith("Exterior/")) scale = KitScale;
            r.scaleInLightmap = scale;
        }
    }

    // Saved LightingSettings asset, assigned to the scene. All lights are Baked, so the
    // mixed-lighting mode is irrelevant; combined-directional lightmaps let normal maps
    // respond to the baked light (one extra lightmap sample per pixel, about 1.3 MB per
    // 1024 map). The streetlights' cookies bake without a switch: Unity 6's Progressive
    // Lightmapper always honours cookies (EditorSettings.enableCookiesInLightmapper is
    // obsolete there and ignored).
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
        // BC6H/BC7-class blocks at the same size: NormalQuality bands the dark gradients.
        s.lightmapCompression = LightmapCompression.HighQuality;
        s.directionalityMode = LightmapsMode.CombinedDirectional;
        EditorUtility.SetDirty(s);
        AssetDatabase.SaveAssets();
        Lightmapping.lightingSettings = s;
    }

    // Duration of the last Bake in this editor session (seconds).
    public static double LastBakeSeconds { get; private set; }

    // Synchronous bake of the saved, open scene (lightmaps, light probes and the baked
    // reflection probes); works in batch mode. Returns false when the bake fails.
    public static bool Bake(out string report)
    {
        Lightmapping.Clear();
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        bool baked = Lightmapping.Bake();
        watch.Stop();
        LastBakeSeconds = watch.Elapsed.TotalSeconds;
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

    // RunNightQuick, or a build whose bake was skipped or failed (NightStoreBuilder.
    // BuildScene): no lightmaps, so baked lights would not render at all. They become
    // realtime at legacy-falloff intensities (hard low shadows on the near ones); the headlights and near
    // streetlights are Auto, so the flashlight, UV torch and lamps (ForcePixel) keep their
    // per-pixel slots, the far streetlights are vertex-lit, and the flat ambient is lifted
    // to stand in for the missing bounce. A preview only, never the shipped look.
    public static void UseRealtimeFallback(Transform root)
    {
        Transform group = root.Find(GroupName);
        L.LampProfile lamp = L.Lamp;
        foreach (Light light in group.GetComponentsInChildren<Light>(true))
        {
            if (light.name == "DiagnosticSun" || light.name.EndsWith("_Specular")) continue;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            // Hard low-resolution shadows on the near lights, so a preview build does not show street
            // light inside the store through its walls; the far, vertex-lit streetlights stay shadowless.
            bool near = light.name.StartsWith("Headlight_") || light.name.StartsWith("Streetlight_") && NearSide(int.Parse(light.name.Substring(12)));
            light.shadows = near ? LightShadows.Hard : LightShadows.None;
            light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
            light.shadowBias = .05f;
            light.shadowNormalBias = .4f;
            if (light.name.StartsWith("Headlight_"))
            {
                light.intensity = HeadlightRealtime;
                light.renderMode = LightRenderMode.Auto;
            }
            else if (light.name.StartsWith("Streetlight_"))
            {
                light.intensity = lamp.RealtimeIntensity;
                bool far = !NearSide(int.Parse(light.name.Substring(12)));
                light.renderMode = far ? LightRenderMode.ForceVertex : LightRenderMode.Auto;
            }
        }
        RenderSettings.ambientLight = FallbackAmbient.gamma;
        Debug.LogWarning("NightLighting: not baked (RunNightQuick or a preview build). Baked lights switched to realtime (near ones with hard low shadows) and the ambient lifted; run RunNight or Play-Keyboard.cmd bake for the real look.");
    }

    // The inverse of UseRealtimeFallback, before a bake of a quick build: every scene
    // light baked with soft shadows at the baked intensities, the night ambient back.
    public static void UseBaked(Transform root)
    {
        Transform group = root.Find(GroupName);
        if (group == null) return;
        L.LampProfile lamp = L.Lamp;
        foreach (Light light in group.GetComponentsInChildren<Light>(true))
        {
            if (light.name == "DiagnosticSun" || light.name.EndsWith("_Specular")) continue;
            light.lightmapBakeType = LightmapBakeType.Baked;
            light.shadows = LightShadows.Soft;
            light.renderMode = LightRenderMode.Auto;
            if (light.name.StartsWith("Headlight_")) light.intensity = HeadlightIntensity;
            else if (light.name.StartsWith("Streetlight_")) light.intensity = lamp.BakedIntensity;
            else if (light.name == "Moonlight") light.intensity = MoonIntensity;
        }
        RenderSettings.ambientLight = NightAmbient.gamma;
    }

    // The ambient the night uses with the lights as they are: lifted while the realtime
    // fallback stands in for the bake.
    public static Color NightAmbientFor(Transform root)
    {
        Transform group = root.Find(GroupName);
        if (group != null)
            foreach (Light light in group.GetComponentsInChildren<Light>(true))
                if (light.name != "DiagnosticSun" && light.lightmapBakeType != LightmapBakeType.Baked) return FallbackAmbient.gamma;
        return NightAmbient.gamma;
    }
}
#endif
