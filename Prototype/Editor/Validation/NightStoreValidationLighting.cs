#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using L = NightStoreLayout;

// The night lighting the realism wiring added (NightLighting's glow cards, sky, probes and
// cookies, GlowBillboard, LightBarBlink's crossfade and steady mode), checked on the built
// scene: in Edit mode right after the lighting pass, before the scene is saved and baked
// (LightingPackChecks, from Generate), and in Play mode at arrival (LightingPlayChecks, from
// Tick stage 1). Every measurement comes from the placed objects (lights, renderers, meshes,
// probe boxes, property blocks, the skybox, the cookie image), never from NightLighting's own
// helpers, so a halo off its lens, a wash sunk into the street, a cookie turned the wrong way
// or a probe box short of the facades shows. A check that needs NightSky or StreetModels logs
// an Info line and checks the fallback instead, as in a project unpacked from the ZIP alone.
// File-local members carry the Glow or Lantern prefix, so the other area files of this
// partial class never collide with them.
public static partial class NightStoreValidation
{
    private const string GlowShader = "Legacy Shaders/Particles/Additive", GlowTint = "_TintColor";

    // Edit mode, after NightLighting.Apply (lights with their cookies, lenses, Lighting/Glow saved
    // facing the arrival eye, the reflection probes and the skybox), before the bake and before
    // UseRealtimeFallback.
    private static void LightingPackChecks(Transform root)
    {
        // A local list: a static field would run NightStoreLayout's initialiser at every editor load.
        List<string> packs = new List<string>();
        foreach (string pack in new[] { NightLighting.SkyPack, L.Lantern.Pack })
            packs.Add(pack + (AssetDatabase.IsValidFolder(DemoAssetLibrary.Root + "/" + pack) ? " present" : " absent"));
        DemoValidation.Info("Lighting packs: " + string.Join(", ", packs));
        GlowCardChecks(root);
        GlowPlacementChecks(root);
        GlowSkyChecks();
        GlowProbeChecks(root);
        LanternCookieChecks(root);
        LanternLensChecks(root);
    }

    // Play mode at arrival, after RealtimeLightChecks (torches off, no lamp deployed, the light
    // bar flashing); leaves that state as it found it.
    private static void LightingPlayChecks()
    {
        Transform root = Root();
        GlowTorchCookieChecks();
        GlowLightBarChecks();
        GlowSteadyChecks();
        GlowFacingChecks(root);
        LanternFallbackChecks(root);
    }

    // ---------------------------------------------------------------- glow cards

    // Six halos (one per streetlight and headlamp lens) and two washes, all additive cards on
    // the one NightGlow material with a clear tint of its own (a card without its property block
    // adds nothing), quads without collider, shadows, probe sampling or static flags; no Light
    // under the glow, and the only lights under the store are the lighting group's moon,
    // streetlights, headlights and diagnostic sun. Without the sprite no card is built at all.
    private static void GlowCardChecks(Transform root)
    {
        Transform group = root.Find(NightLighting.GroupName);
        Transform glow = group != null ? group.Find(NightLighting.GlowGroupName) : null;
        Texture2D sprite = DemoAssetLibrary.Texture(NightLighting.SkyPack, NightLighting.GlowSpriteFile);
        bool built = sprite != null && Shader.Find(GlowShader) != null;
        LightBarBlink bar = GlowBar(root);
        Light[] lights = root.GetComponentsInChildren<Light>(true);
        bool grouped = group != null && lights.Length == 1 + L.Streetlights.Length + 2 + 1;
        foreach (Light light in lights) grouped &= group != null && light.transform.IsChildOf(group);
        if (!built)
        {
            DemoValidation.Info("NightSky glow sprite not present: no glow cards");
            DemoValidation.Check(glow == null && bar != null && bar.Washes.Length == 0 && grouped,
                "Without the glow sprite no glow card is built and no light is added");
            return;
        }

        int lenses = 0;
        Transform posts = root.Find("Exterior/Streetlights");
        if (posts != null) foreach (Transform post in posts) if (post.Find("Lens") != null) lenses++;
        foreach (string side in new[] { "Left", "Right" }) if (root.Find("Exterior/PoliceCar/Headlight" + side + "/Lens") != null) lenses++;
        int halos = 0, washes = 0, triangles = 0;
        HashSet<Material> materials = new HashSet<Material>();
        bool cards = glow != null && glow.GetComponentsInChildren<Light>(true).Length == 0;
        if (glow != null)
            foreach (Transform card in glow)
            {
                if (card.name.StartsWith("Halo_", StringComparison.Ordinal)) halos++;
                else if (card.name.StartsWith("Wash_", StringComparison.Ordinal)) washes++;
                else { cards = false; continue; }
                MeshRenderer r = card.GetComponent<MeshRenderer>();
                MeshFilter f = card.GetComponent<MeshFilter>();
                Material m = r != null ? r.sharedMaterial : null;
                if (m == null || f == null || f.sharedMesh == null) { cards = false; continue; }
                materials.Add(m);
                triangles += f.sharedMesh.triangles.Length / 3;
                cards &= m.shader.name == GlowShader && m.mainTexture == sprite && m.HasProperty(GlowTint) && m.GetColor(GlowTint).a < 0.001f
                    && r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off && !r.receiveShadows
                    && r.lightProbeUsage == UnityEngine.Rendering.LightProbeUsage.Off && r.reflectionProbeUsage == UnityEngine.Rendering.ReflectionProbeUsage.Off
                    && GameObjectUtility.GetStaticEditorFlags(card.gameObject) == 0 && card.GetComponent<Collider>() == null;
            }
        DemoValidation.Info("Glow cards: " + halos + " halos for " + lenses + " lenses, " + washes + " washes, " + triangles + " triangles, "
            + materials.Count + " material(s); lights under the store " + lights.Length);
        DemoValidation.Check(cards && halos == lenses && lenses == L.Streetlights.Length + 2 && washes == 2 && materials.Count == 1
            && triangles == 2 * (halos + washes) && grouped, "Glow halos and washes add no light: additive cards without collider, shadow or probes");
    }

    // The halos as saved: every Halo_ card is one of the billboard's quads, each with a tint and
    // an anchor, sitting on the line from its own lens (looked up by the card's name) towards the
    // arrival eye and facing away from it. The washes are exactly the light bar's wash renderers
    // and lie flat 2-6 mm over the visible surface under their centre and corners (mesh rays on
    // the street and floor meshes, so the police car's bounds collider above the street wash
    // cannot get in the way): the street one round the car, the floor one inside the showroom.
    private static void GlowPlacementChecks(Transform root)
    {
        Transform glow = root.Find(NightLighting.GroupName + "/" + NightLighting.GlowGroupName);
        if (glow == null)
        {
            DemoValidation.Info("NightSky glow sprite not present: halo and wash placement not checked");
            return;
        }
        GlowBillboard billboard = glow.GetComponent<GlowBillboard>();
        bool halos = billboard != null && billboard.Quads.Length > 0
            && billboard.Tints.Length == billboard.Quads.Length && billboard.Anchors.Length == billboard.Quads.Length;
        int count = 0;
        foreach (Transform card in glow)
            if (card.name.StartsWith("Halo_", StringComparison.Ordinal))
            {
                count++;
                halos &= billboard != null && Array.IndexOf(billboard.Quads, card) >= 0;
            }
        Vector3 eye = root.TransformPoint(L.PlayerSpawn) + Vector3.up * NightLighting.SpawnEyeHeight;
        halos &= billboard != null && count == billboard.Quads.Length && GlowFaces(billboard, root, eye);
        DemoValidation.Check(halos, "Each glow halo is saved on its lens, facing the arrival view");

        LightBarBlink bar = GlowBar(root);
        Transform car = root.Find("Exterior/PoliceCar");
        Transform street = glow.Find("Wash_Street"), floor = glow.Find("Wash_ShopFloor");
        Renderer streetWash = street != null ? street.GetComponent<Renderer>() : null, floorWash = floor != null ? floor.GetComponent<Renderer>() : null;
        bool washes = bar != null && streetWash != null && floorWash != null && bar.Washes.Length == 2
            && Array.IndexOf(bar.Washes, streetWash) >= 0 && Array.IndexOf(bar.Washes, floorWash) >= 0;
        float streetLow = 0, streetHigh = 0, floorLow = 0, floorHigh = 0;
        washes &= GlowWashGap(street, root.Find("Exterior/Ground/Street"), out streetLow, out streetHigh)
            && GlowWashGap(floor, root.Find("Architecture/Floor"), out floorLow, out floorHigh);
        washes &= streetLow >= 0.002f && streetHigh <= 0.006f && floorLow >= 0.002f && floorHigh <= 0.006f;
        washes &= car != null && street != null && Flat(street.position - car.position) < 1;
        if (floor != null)
            for (int i = 0; i < 4; i++)
                washes &= L.Showroom.Contains(floor.TransformPoint((i & 1) == 0 ? -0.5f : 0.5f, (i & 2) == 0 ? -0.5f : 0.5f, 0));
        DemoValidation.Info("Light-bar washes over their surface: street " + (streetLow * 1000).ToString("0.0") + "-" + (streetHigh * 1000).ToString("0.0")
            + " mm, shop floor " + (floorLow * 1000).ToString("0.0") + "-" + (floorHigh * 1000).ToString("0.0") + " mm");
        DemoValidation.Check(washes, "The light bar's washes lie flat just over the street at the car and over the shop floor inside the entrance");
    }

    // ---------------------------------------------------------------- sky and probes

    // The overcast panorama through Skybox/Panoramic when NightSky has it, else the procedural
    // sky; either way the default reflection is the sky (no custom HDRI cubemap), the baked box
    // probes doing the rest.
    private static void GlowSkyChecks()
    {
        Texture2D panorama = DemoAssetLibrary.Texture(NightLighting.SkyPack, NightLighting.OvercastSkyFile);
        Material sky = RenderSettings.skybox;
        bool reflections = RenderSettings.defaultReflectionMode == UnityEngine.Rendering.DefaultReflectionMode.Skybox && RenderSettings.customReflectionTexture == null;
        DemoValidation.Info("Night sky: " + (sky != null ? sky.name + " (" + sky.shader.name + ")" : "none")
            + "; default reflection " + RenderSettings.defaultReflectionMode);
        if (panorama != null)
        {
            DemoValidation.Check(sky != null && sky.shader.name == "Skybox/Panoramic" && sky.GetTexture("_MainTex") == panorama && reflections,
                "The night sky is the overcast panorama; reflections come from the sky and the baked probes");
            return;
        }
        DemoValidation.Info("NightSky overcast panorama not present: procedural night sky");
        DemoValidation.Check(sky != null && sky.shader.name == "Skybox/Procedural" && reflections,
            "The night sky is the procedural sky without the panorama; reflections come from the sky and the baked probes");
    }

    // The across-street box probe encloses the row across the street from end to end and from
    // its shopfronts to its top windows (in front of its frontmost fascia and 15 cm behind it,
    // past the window planes), the apartment block's facade corners and the furniture placed on
    // the far pavement, and stays clear of the near pavement and of the exterior probe's box.
    private static void GlowProbeChecks(Transform root)
    {
        Transform group = root.Find(NightLighting.GroupName);
        ReflectionProbe across = GlowProbe(group, "ReflectionProbe_AcrossStreet"), exterior = GlowProbe(group, "ReflectionProbe_Exterior");
        Renderer row = GlowRenderer(root, "Exterior/Buildings/AcrossStreet"), pavement = GlowRenderer(root, "Exterior/Ground/Pavement");
        bool ok = across != null && exterior != null && row != null && pavement != null;
        List<string> covered = new List<string>(), absent = new List<string>();
        if (ok)
        {
            Bounds box = GlowProbeBox(across), near = GlowProbeBox(exterior), a = row.bounds;
            foreach (float z in new[] { a.max.z + 0.05f, a.max.z - 0.15f })
                ok &= box.Contains(new Vector3(a.min.x + 0.1f, 0.5f, z)) && box.Contains(new Vector3(a.max.x - 0.1f, 0.5f, z))
                    && box.Contains(new Vector3(a.center.x, a.max.y - 0.1f, z));
            covered.Add("row x " + a.min.x.ToString("0.0") + ".." + a.max.x.ToString("0.0") + " to " + a.max.y.ToString("0.0") + " m");
            Transform block = root.Find(NightStreet.ApartmentPath);
            if (block != null && DemoAssetLibrary.WorldBounds(block.gameObject, out Bounds b))
            {
                foreach (float x in new[] { b.min.x + 0.1f, b.max.x - 0.1f })
                    ok &= box.Contains(new Vector3(x, b.max.y - 0.1f, b.max.z + 0.05f)) && box.Contains(new Vector3(x, 0.5f, b.max.z + 0.05f));
                covered.Add(NightStreet.Apartment);
            }
            foreach (NightStreet.Item item in NightStreet.Furniture)
            {
                if (item.Position.z > L.FarKerbZ) continue;
                Transform t = root.Find("Exterior/" + NightStreet.GroupName + "/" + item.Name);
                if (t == null || !DemoAssetLibrary.WorldBounds(t.gameObject, out Bounds f))
                {
                    // Without its model the builder skips the item; with it, it must be there.
                    if (DemoAssetLibrary.Has(item.Pack, item.Model)) ok = false;
                    else absent.Add(item.Name);
                    continue;
                }
                ok &= box.Contains(f.min) && box.Contains(f.max);
                covered.Add(item.Name);
            }
            ok &= box.max.z < pavement.bounds.min.z && !box.Intersects(near);
            DemoValidation.Info("Across-street probe box " + box.min + ".." + box.max + " covers " + string.Join(", ", covered)
                + (absent.Count > 0 ? "; not placed without their model: " + string.Join(", ", absent) : ""));
        }
        DemoValidation.Check(ok, "The across-street reflection probe encloses the far facades and the far pavement");
    }

    // ---------------------------------------------------------------- streetlights

    // Each streetlight is a downward spot whose up axis (the cookie's top edge) points at the
    // nearer facade, measured on the built terrace and the row across the street: +z on the
    // shop's side, -z across. With NightSky it carries the street-lamp cookie, whose short side
    // (the sharp cut-off) is the image's top edge (read from the file); without it, no cookie.
    private static void LanternCookieChecks(Transform root)
    {
        Transform group = root.Find(NightLighting.GroupName);
        Texture2D cookie = DemoAssetLibrary.Texture(NightLighting.SkyPack, NightLighting.StreetCookieFile);
        bool ok = true;
        List<string> lines = new List<string>();
        for (int i = 0; i < L.Streetlights.Length; i++)
        {
            Light light = LightAt(group, "Streetlight_" + i);
            if (light == null || !LanternNearShop(root, light.transform.position, out bool near)) { ok = false; continue; }
            float want = near ? 1 : -1;
            ok &= light.type == LightType.Spot && Vector3.Dot(light.transform.forward, Vector3.down) > 0.999f && light.transform.up.z * want > 0.99f
                && (cookie != null ? light.cookie == cookie : light.cookie == null);
            lines.Add(i + (near ? " shop side" : " far side") + " up z " + light.transform.up.z.ToString("0.00"));
        }
        string reach = "";
        if (cookie != null)
        {
            bool read = LanternCookieReach(DemoAssetLibrary.Path(NightLighting.SkyPack, NightLighting.StreetCookieFile), out int up, out int down);
            ok &= read && up > 0 && up * 1.5f < down;
            reach = "; cookie reaches " + up + " px towards its top edge, " + down + " px towards its bottom" + (read ? "" : " (unreadable)");
        }
        else DemoValidation.Info("NightSky streetlight cookie not present: the streetlights are plain spots");
        DemoValidation.Info("Streetlight spots: " + string.Join("; ", lines) + reach);
        DemoValidation.Check(ok, cookie != null ? "Streetlight cookies throw their short sharp edge towards the nearer facade"
            : "Streetlights point straight down, turned to the nearer facade, without a cookie");
    }

    // With the StreetModels lantern, each post keeps exactly one direct Lens box, which fits
    // inside the lantern's glass and encloses its bulb (LOD0's submeshes: the blended glass and
    // the bulb drawn in StoreKit.LightLens), the post's light sits inside the lens, and nothing
    // on the post casts a shadow.
    private static void LanternLensChecks(Transform root)
    {
        if (!L.UsesLantern)
        {
            DemoValidation.Info("StreetModels StreetLamp not present: KayKit lamps keep the existing lens checks");
            return;
        }
        Transform group = root.Find(NightLighting.GroupName), posts = root.Find("Exterior/Streetlights");
        Material bulbMaterial = StoreKit.LightLens;
        bool ok = posts != null && bulbMaterial != null;
        string sample = "";
        for (int i = 0; i < L.Streetlights.Length && posts != null; i++)
        {
            Transform post = posts.Find("Streetlight_" + i);
            Light light = LightAt(group, "Streetlight_" + i);
            Transform model = post != null ? post.Find(L.Lantern.Model + "/LOD0") : null;
            int lenses = 0;
            Transform lens = null;
            bool shadowless = post != null;
            if (post != null)
            {
                foreach (Transform child in post) if (child.name == "Lens") { lenses++; lens = child; }
                foreach (MeshRenderer r in post.GetComponentsInChildren<MeshRenderer>(true))
                    shadowless &= r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Renderer lensRenderer = lens != null ? lens.GetComponent<Renderer>() : null;
            if (light == null || model == null || lenses != 1 || lensRenderer == null || !shadowless
                || !LanternBounds(model, m => m.renderQueue >= 3000, out Bounds glass) || !LanternBounds(model, m => m == bulbMaterial, out Bounds bulb))
            {
                ok = false;
                continue;
            }
            Bounds box = lensRenderer.bounds;
            ok &= LanternInside(box, glass, 0.002f) && LanternInside(bulb, box, 0.002f) && box.Contains(light.transform.position);
            if (i == 0)
                sample = "glass y " + glass.min.y.ToString("0.000") + "-" + glass.max.y.ToString("0.000") + " (half-width " + glass.extents.x.ToString("0.000")
                    + "), bulb y " + bulb.min.y.ToString("0.000") + "-" + bulb.max.y.ToString("0.000") + ", lens y " + box.min.y.ToString("0.000") + "-"
                    + box.max.y.ToString("0.000") + " (half-width " + box.extents.x.ToString("0.000") + "), light y " + light.transform.position.y.ToString("0.000");
        }
        DemoValidation.Info("Lantern 0: " + (sample.Length > 0 ? sample : "not measured"));
        DemoValidation.Check(ok, "Each lantern holds its light and one lens at the bulb inside its glass, and casts no shadow");
    }

    // ---------------------------------------------------------------- play mode

    // The flashlight projects DemoLighting's flashlight cookie, the UV torch its own (else the
    // flashlight's), a lamp lit on deployment the station's lamp cookie (none without one); with
    // NightSky each is that pack's file. Torches go off and the lamp is removed again.
    private static void GlowTorchCookieChecks()
    {
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        ToolStation station = DemoValidation.Find<ToolStation>();
        int before = station.DeployedCount;
        string flashlightFile = GlowSkyFile(NightLighting.FlashlightCookieFile), uvFile = GlowSkyFile(NightLighting.UVTorchCookieFile),
            lampFile = GlowSkyFile(NightLighting.LampCookieFile);
        lighting.Flashlight(true);
        Texture flashlight = lighting.FlashlightLight != null ? lighting.FlashlightLight.cookie : null;
        lighting.UVTorch(true);
        Texture uv = lighting.UVTorchLight != null ? lighting.UVTorchLight.cookie : null;
        lighting.UVTorch(false);
        DeployedTool lamp = station.Spawn(DemoToolKind.Lamp);
        bool lit = lamp != null && lamp.LampLit;
        Texture lampCookie = lamp != null && lamp.Lamp != null ? lamp.Lamp.cookie : null;
        station.RemoveTool(lamp);
        Texture uvWanted = lighting.UVTorchCookie != null ? lighting.UVTorchCookie : lighting.FlashlightCookie;
        bool ok = flashlight != null && flashlight == lighting.FlashlightCookie && uv == uvWanted && lit && lampCookie == station.LampCookie
            && (flashlightFile == null || GlowAssetPath(flashlight) == flashlightFile) && (uvFile == null || GlowAssetPath(uv) == uvFile)
            && (lampFile == null || GlowAssetPath(lampCookie) == lampFile)
            && !lighting.FlashlightOn && !lighting.UVTorchOn && station.LitLampCount == 0 && station.DeployedCount == before;
        DemoValidation.Info("Cookies: flashlight " + GlowAssetPath(flashlight) + ", UV torch " + GlowAssetPath(uv) + ", scene lamp "
            + (lampCookie != null ? GlowAssetPath(lampCookie) : "none"));
        bool sky = flashlightFile != null || uvFile != null || lampFile != null;
        if (!sky) DemoValidation.Info("NightSky cookies not present: the torches keep the generated cookie and lamps project none");
        DemoValidation.Check(ok, sky ? "Torches and lit scene lamps project the NightSky cookies"
            : "Torches keep the generated cookie and scene lamps project none without NightSky");
    }

    // One full flash cycle in 400 steps: the red share (Redness) never jumps by more than 0.05 a
    // step, reaches full red and full blue and passes through mid-fade; at every step each half
    // shows On x Lerp(OffFactor, 1, its share) and every wash is on with the lit half's tint, as
    // the renderers' property blocks hand them back.
    private static void GlowLightBarChecks()
    {
        LightBarBlink bar = DemoValidation.Find<LightBarBlink>();
        MaterialPropertyBlock scratch = new MaterialPropertyBlock();
        const int steps = 400;
        bool flashing = bar.Flashing && bar.Hertz > 0 && bar.Red != null && bar.Blue != null, follows = flashing, fading = false;
        float period = bar.Hertz > 0 ? 1 / bar.Hertz : 0, previous = 0, largest = 0, highest = 0, lowest = 1;
        for (int k = 0; k <= steps && flashing; k++)
        {
            bar.Apply(k * period / steps);
            float r = bar.Redness;
            if (k > 0) largest = Mathf.Max(largest, Mathf.Abs(r - previous));
            previous = r;
            highest = Mathf.Max(highest, r);
            lowest = Mathf.Min(lowest, r);
            fading |= r > 0.3f && r < 0.7f;
            follows &= GlowNear(bar.Emission(bar.Red), GlowStored(scratch, bar.RedOn * Mathf.Lerp(bar.OffFactor, 1, r)))
                && GlowNear(bar.Emission(bar.Blue), GlowStored(scratch, bar.BlueOn * Mathf.Lerp(bar.OffFactor, 1, 1 - r)));
            foreach (Renderer wash in bar.Washes)
                follows &= wash != null && wash.enabled && GlowNear(bar.Tint(wash), GlowStored(scratch, Color.Lerp(bar.WashBlue, bar.WashRed, r)));
        }
        bar.Apply(Time.time);
        DemoValidation.Info("Light bar: " + bar.Hertz.ToString("0.00") + " Hz, largest step of the red share " + largest.ToString("0.000")
            + " per 1/" + steps + " cycle, " + bar.Washes.Length + " washes");
        DemoValidation.Check(follows && fading && largest <= 0.05f && highest >= 0.999f && lowest <= 0.001f,
            "The light bar crossfades between red and blue without a jump, and its washes follow");
    }

    // The presenter's switch (desktop-only toast) holds both halves at On x SteadyFactor with the
    // washes off and clear, at any time; switching again flashes with the washes back on; a
    // session reset (DemoLighting.OnReset, invoked alone so nothing else is reset) flashes it again.
    private static void GlowSteadyChecks()
    {
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        LightBarBlink bar = DemoValidation.Find<LightBarBlink>();
        MaterialPropertyBlock scratch = new MaterialPropertyBlock();
        bool wired = lighting.LightBar == bar && bar.Flashing && lighting.LightBarFlashing;
        lighting.ToggleLightBarFlashing();
        bool steady = GlowSteady(lighting, bar, scratch) && GlowToast("Light bar steady");
        bar.Apply(Time.time + 0.37f);
        steady &= GlowSteady(lighting, bar, scratch);
        lighting.ToggleLightBarFlashing();
        bool again = bar.Flashing && GlowToast("Light bar flashing") && GlowWashesOn(bar);
        lighting.ToggleLightBarFlashing();
        System.Reflection.MethodInfo reset = typeof(DemoLighting).GetMethod("OnReset",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        bool examples = lighting.UVExamplesVisible, held = !bar.Flashing;
        if (reset != null) reset.Invoke(lighting, null);
        lighting.SetUVExamples(examples);
        bool restored = held && reset != null && bar.Flashing && lighting.LightBarFlashing && GlowWashesOn(bar);
        if (!bar.Flashing) bar.SetFlashing(true);
        bar.Apply(Time.time);
        DemoValidation.Check(wired && steady && again && restored,
            "Light bar steady switch holds both halves dimmed with the washes off (desktop-only toast); a reset flashes it again");
    }

    // The halos from three eyes about the police car (8 m ahead of its nose, 8 m to its side and
    // 8 m behind it), each a disabled probe camera handed to Camera.onPreCull as a render would
    // (the evidence photo or the viewfinder): every halo turns to that camera, PullToEye towards
    // it on the line from its lens and facing away from it; a streetlight halo glows alike from
    // every side, a headlamp's only from ahead. The main camera's callback turns them back.
    // Nothing else moves them, so a billboard that faced Camera.main alone fails here.
    private static void GlowFacingChecks(Transform root)
    {
        Transform glow = root.Find(NightLighting.GroupName + "/" + NightLighting.GlowGroupName);
        GlowBillboard billboard = glow != null ? glow.GetComponent<GlowBillboard>() : null;
        if (billboard == null)
        {
            DemoValidation.Info("NightSky glow sprite not present: halo facing not checked");
            return;
        }
        Transform car = root.Find("Exterior/PoliceCar");
        Camera main = Camera.main;
        Camera probe = new GameObject("GlowProbeCamera").AddComponent<Camera>();
        probe.enabled = false;
        bool faced = car != null && main != null && billboard.Quads.Length > 0 && billboard.Tints.Length == billboard.Quads.Length, lit = faced;
        Vector3[] eyes = car != null ? new[] { car.TransformPoint(0, 1.2f, 8), car.TransformPoint(8, 1.2f, 0), car.TransformPoint(0, 1.2f, -8) } : new Vector3[0];
        List<string> views = new List<string>();
        for (int e = 0; e < eyes.Length && faced; e++)
        {
            probe.transform.position = eyes[e];
            if (Camera.onPreCull != null) Camera.onPreCull(probe);
            bool turned = GlowFaces(billboard, root, eyes[e]);
            faced &= turned;
            for (int i = 0; i < billboard.Quads.Length && i < billboard.Tints.Length; i++)
            {
                Transform quad = billboard.Quads[i];
                if (quad == null) { lit = false; continue; }
                float full = billboard.Tints[i].a, alpha = GlowAlpha(quad);
                bool headlamp = quad.name.StartsWith("Halo_Headlight_", StringComparison.Ordinal);
                lit &= full > 0 && ((!headlamp || e == 0) ? alpha >= 0.99f * full : alpha <= 0.01f * full);
            }
            if (Camera.onPostRender != null) Camera.onPostRender(probe);
            views.Add((e == 0 ? "ahead" : e == 1 ? "beside" : "behind") + (turned ? " faced" : " not faced"));
        }
        bool follows = false;
        if (main != null)
        {
            if (Camera.onPreCull != null) Camera.onPreCull(main);
            follows = GlowFaces(billboard, root, main.transform.position);
            if (Camera.onPostRender != null) Camera.onPostRender(main);
        }
        UnityEngine.Object.DestroyImmediate(probe.gameObject);
        DemoValidation.Info("Glow halos per camera: " + string.Join(", ", views) + "; main camera " + (follows ? "faced" : "not faced"));
        DemoValidation.Check(faced && lit && follows,
            "Glow halos turn to whichever camera renders them; a headlamp's halo shows only ahead of the car");
    }

    // RunNightQuick's realtime stand-ins follow the lamp profile: shadowless streetlights at its
    // realtime intensity, Auto on the shop's side (a per-pixel slot when near) and vertex-lit
    // across the street, the side measured on the built facades. Baked runs keep baked lights.
    private static void LanternFallbackChecks(Transform root)
    {
        if (SessionState.GetBool(BakedKey, false))
        {
            DemoValidation.Info("Night lighting baked: the quick preview's streetlight profile is not in use");
            return;
        }
        Transform group = root.Find(NightLighting.GroupName);
        L.LampProfile profile = L.Lamp;
        bool ok = true;
        List<string> modes = new List<string>();
        for (int i = 0; i < L.Streetlights.Length; i++)
        {
            Light light = LightAt(group, "Streetlight_" + i);
            if (light == null || !LanternNearShop(root, light.transform.position, out bool near)) { ok = false; continue; }
            ok &= light.lightmapBakeType == LightmapBakeType.Realtime && light.shadows == (near ? LightShadows.Hard : LightShadows.None)
                && Mathf.Abs(light.intensity - profile.RealtimeIntensity) < 0.001f
                && light.renderMode == (near ? LightRenderMode.Auto : LightRenderMode.ForceVertex);
            modes.Add(i + " " + light.renderMode + " at " + light.intensity.ToString("0.00"));
        }
        DemoValidation.Info("Quick-preview streetlights (" + profile.Model + "): " + string.Join("; ", modes));
        DemoValidation.Check(ok, "Quick-preview streetlights follow the lamp profile: near ones per-pixel with hard shadows, far ones vertex-lit and shadowless");
    }

    // ---------------------------------------------------------------- helpers

    private static LightBarBlink GlowBar(Transform root)
    {
        Transform t = root.Find("Exterior/PoliceCar/LightBar");
        return t != null ? t.GetComponent<LightBarBlink>() : null;
    }

    private static Renderer GlowRenderer(Transform root, string path)
    {
        Transform t = root.Find(path);
        return t != null ? t.GetComponent<Renderer>() : null;
    }

    private static ReflectionProbe GlowProbe(Transform group, string name)
    {
        Transform t = group != null ? group.Find(name) : null;
        return t != null ? t.GetComponent<ReflectionProbe>() : null;
    }

    // A box probe's world box from its own centre and size (the probes are never rotated).
    private static Bounds GlowProbeBox(ReflectionProbe p) => new Bounds(p.transform.position + p.center, p.size);

    // The lens a halo card belongs to, by the card's name: Halo_Streetlight_<i> on the post's
    // Lens, Halo_Headlight_<Side> on the car's Headlight<Side>/Lens.
    private static Transform GlowLens(Transform root, string halo)
    {
        if (halo.StartsWith("Halo_Streetlight_", StringComparison.Ordinal)) return root.Find("Exterior/Streetlights/" + halo.Substring(5) + "/Lens");
        if (halo.StartsWith("Halo_Headlight_", StringComparison.Ordinal)) return root.Find("Exterior/PoliceCar/Headlight" + halo.Substring(15) + "/Lens");
        return null;
    }

    // Every billboard quad on the line from its lens towards the eye, PullToEye out (at most half
    // way), its forward pointing away from the eye.
    private static bool GlowFaces(GlowBillboard billboard, Transform root, Vector3 eye)
    {
        bool ok = billboard.Quads.Length > 0;
        foreach (Transform quad in billboard.Quads)
        {
            Transform lens = quad != null ? GlowLens(root, quad.name) : null;
            if (lens == null) { ok = false; continue; }
            Vector3 to = eye - lens.position;
            float distance = to.magnitude;
            if (distance < 0.01f) { ok = false; continue; }
            to /= distance;
            ok &= Vector3.Distance(quad.position, lens.position + to * Mathf.Min(billboard.PullToEye, distance * 0.5f)) < 0.001f
                && Vector3.Dot(quad.forward, -to) > 0.999f;
        }
        return ok;
    }

    // A card's additive strength as its property block holds it (alpha is never colour-converted).
    private static float GlowAlpha(Transform quad)
    {
        Renderer r = quad.GetComponent<Renderer>();
        if (r == null) return -1;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        return block.GetColor(GlowTint).a;
    }

    // A wash card's lowest and highest gap over the surface's mesh, under its centre and its
    // four corners (1 cm in from the edges); false when it is not flat or a point has no surface
    // under it.
    private static bool GlowWashGap(Transform wash, Transform surface, out float low, out float high)
    {
        low = float.MaxValue;
        high = float.MinValue;
        MeshFilter filter = surface != null ? surface.GetComponent<MeshFilter>() : null;
        if (wash == null || filter == null || Mathf.Abs(wash.forward.y) < 0.999f) return false;
        Vector3[] points = { wash.position, wash.TransformPoint(-0.49f, -0.49f, 0), wash.TransformPoint(0.49f, -0.49f, 0),
            wash.TransformPoint(-0.49f, 0.49f, 0), wash.TransformPoint(0.49f, 0.49f, 0) };
        foreach (Vector3 p in points)
        {
            float y = DemoValidation.SurfaceY(filter, p);
            if (float.IsNaN(y)) return false;
            low = Mathf.Min(low, p.y - y);
            high = Mathf.Max(high, p.y - y);
        }
        return true;
    }

    // A colour as a property block hands it back (SetColor may convert it to the active colour
    // space), so it compares exactly with what LightBarBlink set on a renderer.
    private static Color GlowStored(MaterialPropertyBlock scratch, Color c)
    {
        scratch.SetColor("_GlowCheck", c);
        return scratch.GetColor("_GlowCheck");
    }

    // Colours equal within 0.2 % (of 1, or of the channel when it is brighter), alpha included.
    private static bool GlowNear(Color a, Color b)
    {
        for (int c = 0; c < 4; c++)
            if (Mathf.Abs(a[c] - b[c]) > 0.002f * Mathf.Max(1, Mathf.Abs(b[c]))) return false;
        return true;
    }

    // Steady: both halves at On x SteadyFactor and every wash off and clear.
    private static bool GlowSteady(DemoLighting lighting, LightBarBlink bar, MaterialPropertyBlock scratch)
    {
        bool ok = !bar.Flashing && !lighting.LightBarFlashing
            && GlowNear(bar.Emission(bar.Red), GlowStored(scratch, bar.RedOn * bar.SteadyFactor))
            && GlowNear(bar.Emission(bar.Blue), GlowStored(scratch, bar.BlueOn * bar.SteadyFactor));
        foreach (Renderer wash in bar.Washes) ok &= wash != null && !wash.enabled && GlowNear(bar.Tint(wash), Color.clear);
        return ok;
    }

    private static bool GlowWashesOn(LightBarBlink bar)
    {
        bool on = true;
        foreach (Renderer wash in bar.Washes) on &= wash != null && wash.enabled;
        return on;
    }

    // The newest feed message is this presenter-only text (the headset toast skips it).
    private static bool GlowToast(string text)
    {
        int n = DemoFeedback.Recent.Count;
        return n > 0 && DemoFeedback.Recent[n - 1].Text == text && DemoFeedback.Recent[n - 1].DesktopOnly;
    }

    // The NightSky file's asset path when the pack has it, else null.
    private static string GlowSkyFile(string file) =>
        DemoAssetLibrary.Texture(NightLighting.SkyPack, file) != null ? DemoAssetLibrary.Path(NightLighting.SkyPack, file) : null;

    private static string GlowAssetPath(Texture t) => t != null ? AssetDatabase.GetAssetPath(t) : "";

    // Whether a light at p is nearer the shop's terrace (the Neighbours' street face) than the
    // row across the street (its street face); false when either building is missing.
    private static bool LanternNearShop(Transform root, Vector3 p, out bool near)
    {
        near = false;
        Renderer shop = GlowRenderer(root, "Exterior/Buildings/Neighbours"), across = GlowRenderer(root, "Exterior/Buildings/AcrossStreet");
        if (shop == null || across == null) return false;
        near = Mathf.Abs(shop.bounds.min.z - p.z) < Mathf.Abs(across.bounds.max.z - p.z);
        return true;
    }

    // World bounds of the vertices of every submesh under t whose material keep accepts (each
    // OBJ group of a pack model is its own child mesh); false without one.
    private static bool LanternBounds(Transform t, Func<Material, bool> keep, out Bounds bounds)
    {
        bounds = new Bounds();
        bool any = false;
        foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter f = r.GetComponent<MeshFilter>();
            Mesh mesh = f != null ? f.sharedMesh : null;
            if (mesh == null) continue;
            Vector3[] vertices = mesh.vertices;
            Material[] materials = r.sharedMaterials;
            Matrix4x4 world = r.transform.localToWorldMatrix;
            for (int s = 0; s < mesh.subMeshCount && s < materials.Length; s++)
            {
                if (materials[s] == null || !keep(materials[s])) continue;
                foreach (int i in mesh.GetIndices(s))
                {
                    if (i >= vertices.Length) continue;
                    Vector3 p = world.MultiplyPoint3x4(vertices[i]);
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                    else bounds.Encapsulate(p);
                }
            }
        }
        return any;
    }

    // inner lies inside outer, within tolerance on every side.
    private static bool LanternInside(Bounds inner, Bounds outer, float tolerance)
    {
        for (int axis = 0; axis < 3; axis++)
            if (inner.min[axis] < outer.min[axis] - tolerance || inner.max[axis] > outer.max[axis] + tolerance) return false;
        return true;
    }

    // Rows from the middle of a cookie image's centre column to where it falls below a quarter of
    // that column's peak, towards the image's top edge (a spot's local up, the edge
    // NightSky's README turns towards the houses) and towards its bottom edge. Read from the PNG
    // (the imported cookie is not CPU-readable); false when it cannot be read.
    private static bool LanternCookieReach(string assetPath, out int up, out int down)
    {
        up = down = 0;
        string file = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), assetPath);
        if (!System.IO.File.Exists(file)) return false;
        Texture2D image = new Texture2D(2, 2);
        try
        {
            if (!image.LoadImage(System.IO.File.ReadAllBytes(file))) return false;
            int x = image.width / 2, centre = image.height / 2;
            bool alpha = image.format == TextureFormat.Alpha8;
            float[] column = new float[image.height];
            float peak = 0;
            for (int y = 0; y < image.height; y++)
            {
                Color c = image.GetPixel(x, y);
                column[y] = alpha ? c.a : c.r;
                peak = Mathf.Max(peak, column[y]);
            }
            float edge = peak * 0.25f;
            if (peak <= 0 || column[centre] < edge) return false;
            while (centre + up + 1 < image.height && column[centre + up + 1] >= edge) up++;
            while (centre - down - 1 >= 0 && column[centre - down - 1] >= edge) down++;
            return true;
        }
        finally { UnityEngine.Object.DestroyImmediate(image); }
    }
}
#endif
