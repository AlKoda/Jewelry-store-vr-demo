#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using L = NightStoreLayout;

// Night store generation and checks, in the ShopCityValidation pattern: generate the
// JewelryStoreNight scene, capture it, enter Play mode and run staged checks, then the
// shared tool, photo and reset stages with the night store's test points. RunNight
// bakes the night lighting (NightLighting) before the captures; RunNightQuick skips
// the bake and previews the same lights as shadowless realtime lights instead.
// Output goes to Docs/VerificationNight. Invoke either with -executeMethod.
[InitializeOnLoad]
public static class NightStoreValidation
{
    private const string Key = "NightStoreValidationStage";
    private const string BakeKey = "NightStoreValidationBake";
    private const string StatsKey = "NightStoreValidationStats";
    private const string BakedKey = "NightStoreValidationBaked";
    private const string BakeSecondsKey = "NightStoreValidationBakeSeconds";
    private const string TrianglesKey = "NightStoreValidationTriangles";
    private const string RenderersKey = "NightStoreValidationRenderers";

    // VR budgets for a PC-tethered Quest (see Budgets). Each holds the measured scene
    // with headroom for the instructor's tools (materials: 96, +6 over the old 90, October 11,
    // unverified; it must cover both the five staged NightGraphics sign materials and up to five
    // styled tool materials from DemoToolsBuilder.StyleStation, so set it from the logged
    // "Budget: materials=" value after the first RunNightQuick); the transparency budget became
    // reachable once each case's panes were merged (NightStoreBuilder.MergeCaseGlass).
    public const int TriangleBudget = 220000, RendererBudget = 900, MaterialBudget = 96, TextureBudget = 2048;
    public const int TransparentBudget = 70, FacadePaneBudget = 10;
    public const int LightmapBudget = 8, LightmapSizeBudget = 1024;
    // Realtime lights at once with a torch and two lit lamps; one of them may shadow.
    public const int RealtimeLightBudget = 4, ShadowLightBudget = 1;
    static NightStoreValidation() { EditorApplication.update += Tick; }

    public static void RunNightQuick() { Generate(false); }
    public static void RunNight() { Generate(true); }

    private static void Generate(bool bake)
    {
        try
        {
            SessionState.SetBool(BakeKey, bake);
            SessionState.SetBool(BakedKey, false);
            SessionState.SetFloat(BakeSecondsKey, 0);
            // Before any content: textures, materials and the bake all assume linear.
            NightLighting.UseLinearColorSpace();
            int reimported = ThirdPartyModelImport.GenerateLightmapUVs();
            DemoValidation.Begin("VerificationNight");
            if (reimported > 0) DemoValidation.Info("Third-party models reimported with lightmap UVs: " + reimported);
            // Fixture library first (it builds and removes its galleries at the origin).
            StoreKitValidation.Check();
            GameObject root = NightStoreBuilder.Build(true);
            StaticChecks(root.transform);
            DressingChecks(root.transform);
            StagedAssetChecks(root.transform);
            Camera camera = DemoValidation.CreatePreviewCamera(150);
            NightLighting.Apply(root.transform, camera);
            DemoValidation.SaveScene(L.SceneName);
            if (bake)
            {
                bool baked = NightLighting.Bake(out string report);
                DemoValidation.Info(report);
                DemoValidation.Check(baked && LightmapSettings.lightmaps.Length > 0, "Night lighting bakes");
                SessionState.SetBool(BakedKey, true);
                SessionState.SetFloat(BakeSecondsKey, (float)NightLighting.LastBakeSeconds);
                DemoValidation.SaveScene();
            }
            else NightLighting.UseRealtimeFallback(root.transform);
            LightmapChecks(bake);
            Captures(camera, root.transform);
            NightStoreBuilder.CreatePlayer(camera);
            NightLighting.ConfigureCamera(camera);
            Budgets(root.transform);
            SessionState.SetString(StatsKey, Stats(root.transform));
            DemoValidation.SaveScene();
            DemoValidation.EnterPlayMode(Key, "Night store generation" + (bake ? " with baked lighting" : " (no bake, realtime fallback)")
                + " and 11 actual Unity rendered captures passed.");
        }
        catch (Exception ex) { DemoValidation.Finish(Key, false, ex.ToString()); }
    }

    // The eight concept views of Docs/LayoutReferences/NightStore (manifest.json) at
    // night (night_*), then three in the instructor's diagnostic daylight (day_*) for
    // comparing the layout; the ceiling and roof are hidden for the overview only.
    private static void Captures(Camera camera, Transform root)
    {
        Overview(camera, root, "night_01-cutaway-overview.png");
        DemoValidation.Capture(camera, "night_02-exterior-wide.png", new Vector3(0, 1.6f, -10), new Vector3(0, 1.5f, 3));
        DemoValidation.Capture(camera, "night_03-entrance-interior.png", new Vector3(0, 1.6f, -0.5f), new Vector3(0, 1.1f, 11));
        DemoValidation.Capture(camera, "night_04-left-aisle.png", new Vector3(-6.1f, 1.6f, 0.8f), new Vector3(-5.6f, 0.9f, 8));
        DemoValidation.Capture(camera, "night_05-right-aisle-seating.png", new Vector3(7.3f, 1.7f, 0.5f), new Vector3(1.5f, 0.8f, 6.5f));
        DemoValidation.Capture(camera, "night_06-central-display.png", new Vector3(0.3f, 1.4f, 3.2f), new Vector3(0, 0.75f, 5.2f));
        DemoValidation.Capture(camera, "night_07-office.png", new Vector3(0, 1.6f, 8.0f), new Vector3(0, 0.9f, 11.6f));
        DemoValidation.Capture(camera, "night_08-entrance-closeup.png", new Vector3(0.3f, 1.5f, -2.4f), new Vector3(0, 0.6f, 0.6f));
        DemoLighting lighting = UnityEngine.Object.FindFirstObjectByType<DemoLighting>();
        lighting.SetNight(false);
        Overview(camera, root, "day_01-cutaway-overview.png");
        DemoValidation.Capture(camera, "day_02-exterior-wide.png", new Vector3(0, 1.6f, -10), new Vector3(0, 1.5f, 3));
        DemoValidation.Capture(camera, "day_03-entrance-interior.png", new Vector3(0, 1.6f, -0.5f), new Vector3(0, 1.1f, 11));
        lighting.SetNight(true);
    }

    private static void Overview(Camera camera, Transform root, string file)
    {
        Transform ceiling = root.Find(L.CeilingPath);
        ceiling.gameObject.SetActive(false);
        // Baked occlusion data ignores hidden objects, so the cutaway renders without it.
        bool occlusion = camera.useOcclusionCulling;
        camera.useOcclusionCulling = false;
        DemoValidation.Capture(camera, file, new Vector3(-13, 17, -13), new Vector3(0.5f, 0, 4.5f));
        camera.useOcclusionCulling = occlusion;
        ceiling.gameObject.SetActive(true);
    }

    // The bake policy of StoreKit.MarkStatic as the builder applies it, in edit mode.
    private static void StaticChecks(Transform root)
    {
        // The Windows build opens on DemoLauncher; the night store must head its list.
        var menu = DemoLauncher.Entries(new[] { DemoBuild.LauncherScene, DemoValidation.ScenesFolder + "/JewelryStoreGlass.unity",
            DemoValidation.ScenesFolder + "/" + NightStoreLayout.SceneName + ".unity" }, 0);
        DemoValidation.Check(menu.Count == 2 && menu[0].Scene == NightStoreLayout.SceneName && menu[0].BuildIndex == 2,
            "Start menu lists the night store first and leaves itself out");
        StaticEditorFlags baked = StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic;
        bool shell = true;
        foreach (string path in new[] { "Architecture/Walls", "Architecture/Floor" })
        {
            MeshRenderer r = root.Find(path).GetComponent<MeshRenderer>();
            shell &= (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & baked) == baked && r.receiveGI == ReceiveGI.Lightmaps;
        }
        foreach (MeshRenderer r in root.Find(L.CeilingPath).GetComponentsInChildren<MeshRenderer>(true))
            shell &= (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.OccluderStatic) == 0;
        // Small props stay out of GI; GI contributors without a second UV set take probes.
        int small = 0, wrong = 0;
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            if ((flags & StaticEditorFlags.BatchingStatic) == 0) continue;
            Vector3 size = r.bounds.size;
            bool gi = (flags & StaticEditorFlags.ContributeGI) != 0;
            MeshFilter filter = r.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            bool uv2 = mesh != null && mesh.vertexCount > 0 && mesh.uv2.Length == mesh.vertexCount;
            if (size.x < StoreKit.SmallProp && size.y < StoreKit.SmallProp && size.z < StoreKit.SmallProp)
            {
                small++;
                if (gi) wrong++;
            }
            else if (gi && r.receiveGI == ReceiveGI.Lightmaps && !uv2) wrong++;
        }
        // Nothing moves at night: the open safe doors bake like the rest of the office.
        bool doors = true;
        foreach (string safe in new[] { "Office/SafeLarge/Door", "Office/SafeTall/Door" })
        {
            Transform door = root.Find(safe);
            bool any = false;
            if (door != null)
                foreach (MeshRenderer r in door.GetComponentsInChildren<MeshRenderer>(true))
                    any |= (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0;
            doors &= any;
        }
        bool fans = true;
        foreach (string name in NightStoreDressing.Unbatched)
        {
            Transform fan = root.Find("Dressing/" + name);
            fans &= fan != null && GameObjectUtility.GetStaticEditorFlags(fan.gameObject) == 0;
        }
        DemoValidation.Info("Static policy: small probe-lit props=" + small + "; violations=" + wrong
            + "; textured shell=" + (StoreKit.Floor.mainTexture != null) + "/" + (StoreKit.Plaster.mainTexture != null) + "/" + (StoreKit.Asphalt.mainTexture != null));
        DemoValidation.Check(shell && small > 0 && wrong == 0 && doors && fans, "Night store static flags follow the bake policy");
    }

    // Every static zone batches; each case's deck stock and each loose floor cell is one
    // probe-lit renderer per material, out of GI like the small props it replaced.
    private static void DressingChecks(Transform root)
    {
        int unbatched = 0;
        foreach (string path in new[] { "Architecture", "Facade", "Showroom", "Office", "Exterior/Ground", "Exterior/Bollards",
            "Exterior/Buildings/Neighbours", "Exterior/Buildings/AcrossStreet", "Dressing/" + NightStoreDressing.DisplaysGroup, "Dressing/" + NightStoreDressing.LooseGroup })
        {
            Transform zone = root.Find(path);
            if (zone == null) { unbatched++; continue; }
            foreach (MeshRenderer r in zone.GetComponentsInChildren<MeshRenderer>(true))
                if ((GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.BatchingStatic) == 0) unbatched++;
        }
        Transform dressing = root.Find("Dressing");
        int groups = 0, combined = 0;
        bool merged = true;
        foreach (string name in new[] { NightStoreDressing.DisplaysGroup, NightStoreDressing.LooseGroup })
            foreach (Transform part in dressing.Find(name))
            {
                groups++;
                HashSet<Material> seen = new HashSet<Material>();
                foreach (MeshRenderer r in part.GetComponentsInChildren<MeshRenderer>(true))
                {
                    combined++;
                    StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                    merged &= r.transform.parent == part && r.sharedMaterials.Length == 1 && seen.Add(r.sharedMaterial)
                        && (flags & StaticEditorFlags.ContributeGI) == 0 && r.receiveGI == ReceiveGI.LightProbes;
                }
            }
        DemoValidation.Info("Dressing groups=" + groups + "; combined renderers=" + combined + "; unbatched static-zone renderers=" + unbatched);
        DemoValidation.Check(merged && groups >= 14 && combined >= groups && unbatched == 0, "Night dressing is combined by material");
    }

    // ---------------------------------------------------------------- staged night assets

    // Edit mode: the NightMaterials packed maps and finishes and the NightGraphics signs,
    // each checked only when its files were synced into the project (otherwise the
    // builder keeps the previous scalar finishes and places no sign, and this reports it).
    private static void StagedAssetChecks(Transform root)
    {
        const string pack = StoreKit.NightMaterialPack;
        int packed = 0, expected = 0;
        bool maps = true;
        foreach (KeyValuePair<Material, string> entry in StoreKit.PaletteTextures)
        {
            if (!DemoAssetLibrary.HasTexture(pack, entry.Value + "_MetallicSmoothness.png")) continue;
            expected++;
            Texture map = entry.Key.GetTexture("_MetallicGlossMap");
            bool ok = map != null && map.name == entry.Value + "_MetallicSmoothness" && entry.Key.IsKeywordEnabled("_METALLICGLOSSMAP")
                && entry.Key.GetFloat("_SmoothnessTextureChannel") == 0 && Mathf.Approximately(entry.Key.GetFloat("_GlossMapScale"), 1);
            TextureImporter importer = map != null ? AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(map)) as TextureImporter : null;
            ok &= importer != null && !importer.sRGBTexture;
            if (ok) packed++;
            maps &= ok;
        }
        DemoValidation.Info("Night materials with packed metallic-smoothness maps: " + packed + " of " + expected + " staged");
        if (expected > 0) DemoValidation.Check(maps, "Night materials use packed metallic-smoothness maps");

        bool ceiling = DemoAssetLibrary.HasTexture(pack, "CeilingPaint_Color.png") && !DemoAssetLibrary.HasTexture(StoreKit.TexturePack, "CeilingPaint_Color.png");
        bool facade = DemoAssetLibrary.HasTexture(pack, "FacadeStone_Color.png") && !DemoAssetLibrary.HasTexture(StoreKit.TexturePack, "FacadeStone_Color.png");
        if (ceiling || facade)
        {
            bool ok = true;
            if (ceiling) ok &= Textured(root.Find(L.CeilingPath + "/CeilingSlab"), "CeilingPaint_Color");
            if (facade) ok &= Textured(root.Find("Facade/Cladding"), "FacadeStone_Color");
            DemoValidation.Check(ok, "Night ceiling and facade use the staged plaster and marble");
        }
        else DemoValidation.Info("Staged ceiling/facade finishes not present (or overridden by ReferenceTextures): skipped");

        const string graphics = NightStoreBuilder.GraphicsPack;
        if (DemoAssetLibrary.HasTexture(graphics, "ShopFascia.png"))
        {
            Transform sign = root.Find("Facade/" + NightStoreBuilder.FasciaSign);
            bool ok = SignShows(sign, "ShopFascia") && sign.position.y > L.CeilingY && sign.position.y < L.ParapetY
                && Mathf.Abs(sign.position.x) < 0.01f && sign.position.z < -0.2f && Vector3.Dot(sign.forward, Vector3.forward) > 0.99f;
            DemoValidation.Check(ok, "Fascia shows the shop sign");
        }
        else DemoValidation.Info("NightGraphics fascia not present: skipped");
        if (DemoAssetLibrary.HasTexture(graphics, "StaffOnly.png") && DemoAssetLibrary.HasTexture(graphics, "Closed.png"))
        {
            bool ok = SignShows(root.Find("Facade/EntranceDoors/DoorLeft/" + NightStoreBuilder.ClosedSign), "Closed");
            foreach (string door in NightStoreBuilder.StaffDoors)
                ok &= SignShows(root.Find("Office/" + door + "/Door/" + NightStoreBuilder.StaffSign), "StaffOnly");
            DemoValidation.Check(ok, "Closed sign on the entrance door and Staff Only on the side-room doors");
        }
        else DemoValidation.Info("NightGraphics door signs not present: skipped");
        if (DemoAssetLibrary.HasTexture(graphics, "CollectionRings.png") && DemoAssetLibrary.HasTexture(graphics, "CollectionNecklaces.png")
            && !DemoAssetLibrary.Has(StoreKit.OverridePack, "sign_frame"))
        {
            bool ok = true;
            foreach (string frame in NightStoreBuilder.RingSigns) ok &= SignShows(root.Find("Showroom/" + frame + "/" + NightStoreBuilder.CollectionSign), "CollectionRings");
            foreach (string frame in NightStoreBuilder.NecklaceSigns) ok &= SignShows(root.Find("Showroom/" + frame + "/" + NightStoreBuilder.CollectionSign), "CollectionNecklaces");
            DemoValidation.Check(ok, "Collection signs sit in the wall sign frames");
        }
        else DemoValidation.Info("NightGraphics collection signs not placed: skipped");
        // Case decision pending (for Koda.md): no decal from the pack may be placed yet.
        bool decals = false;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            foreach (Material m in r.sharedMaterials)
                if (m != null && m.mainTexture != null && (m.mainTexture.name.StartsWith("ShoeTrace") || m.mainTexture.name.StartsWith("Scuff") || m.mainTexture.name.StartsWith("DustSmear")))
                    decals = true;
        DemoValidation.Check(!decals, "Staged floor decals stay unplaced");
    }

    private static bool Textured(Transform t, string texture)
    {
        MeshRenderer r = t != null ? t.GetComponent<MeshRenderer>() : null;
        if (r == null) return false;
        foreach (Material m in r.sharedMaterials)
            if (m != null && m.mainTexture != null && m.mainTexture.name == texture) return true;
        return false;
    }

    // An opaque, non-emissive sign quad without a collider showing the named image.
    private static bool SignShows(Transform t, string texture)
    {
        MeshRenderer r = t != null ? t.GetComponent<MeshRenderer>() : null;
        Material m = r != null ? r.sharedMaterial : null;
        return m != null && m.mainTexture != null && m.mainTexture.name == texture && m.renderQueue < 3000
            && !m.IsKeywordEnabled("_EMISSION") && t.GetComponent<Collider>() == null;
    }

    // Play mode: with NightAudio synced the builder's DemoSoundBank supplies the recorded
    // clips (as scene-referenced assets, so a player build carries them) in place of the
    // generated ones; the generated clips stay available as the fallback.
    private static void StagedSoundChecks()
    {
        DemoSoundBank bank = UnityEngine.Object.FindFirstObjectByType<DemoSoundBank>();
        string[] files = { "CameraClick.wav", "FlashlightSwitch.wav", "ToolSetDown.wav", "PanelSelect.wav", "DistantAir_Loop.wav" };
        bool any = false;
        foreach (string file in files) any |= AssetDatabase.LoadAssetAtPath<AudioClip>(DemoAssetLibrary.Path(NightStoreBuilder.AudioPack, file)) != null;
        bool generated = DemoSounds.Generated(DemoSound.Click) != null && DemoSounds.Generated(DemoSound.Thud) != null
            && DemoSounds.Generated(DemoSound.Ambience) != null && DemoSounds.Generated(DemoSound.Ambience).length > 3;
        if (!any)
        {
            DemoValidation.Info("NightAudio not present: generated sounds only");
            DemoValidation.Check(bank == null && generated && DemoSounds.Clip(DemoSound.Thud) == DemoSounds.Generated(DemoSound.Thud),
                "Generated sounds stay without staged audio");
            return;
        }
        bool ok = bank != null && DemoSounds.Bank == bank && generated;
        if (ok)
        {
            foreach (DemoSound sound in new[] { DemoSound.Shutter, DemoSound.Switch, DemoSound.Thud, DemoSound.Select, DemoSound.Ambience })
            {
                AudioClip clip = bank.ClipFor(sound);
                if (clip == null) continue;
                ok &= DemoSounds.Clip(sound) == clip && AssetDatabase.Contains(clip);
            }
            ok &= DemoSounds.Clip(DemoSound.Click) == DemoSounds.Generated(DemoSound.Click);
        }
        AmbientSound ambience = UnityEngine.Object.FindFirstObjectByType<AmbientSound>();
        AudioSource source = ambience != null ? ambience.GetComponent<AudioSource>() : null;
        bool bed = bank == null || bank.Ambience == null || (source != null && source.loop && source.clip == bank.Ambience);
        DemoValidation.Info("Staged sounds: shutter=" + (bank != null && bank.CameraClick != null) + ", switch=" + (bank != null && bank.FlashlightSwitch != null)
            + ", set-down=" + (bank != null && bank.ToolSetDown != null) + ", panel=" + (bank != null && bank.PanelSelect != null) + ", ambience=" + (bank != null && bank.Ambience != null));
        DemoValidation.Check(ok && bed, "Staged sounds replace generated clips");
    }

    // Edit mode, right after the bake (or without one): the lightmaps the scene ships.
    private static void LightmapChecks(bool bake)
    {
        LightmapData[] maps = LightmapSettings.lightmaps;
        int largest = 0;
        bool present = true;
        foreach (LightmapData map in maps)
        {
            present &= map.lightmapColor != null;
            if (map.lightmapColor != null) largest = Mathf.Max(largest, Mathf.Max(map.lightmapColor.width, map.lightmapColor.height));
        }
        int maxSize = Lightmapping.lightingSettings.lightmapMaxSize;
        DemoValidation.Info("Lightmaps=" + maps.Length + " (largest " + largest + " px; max size " + maxSize + ")");
        bool count = bake ? maps.Length >= 1 && maps.Length <= LightmapBudget : maps.Length == 0;
        DemoValidation.Check(count && present && largest <= LightmapSizeBudget && maxSize <= LightmapSizeBudget, "Night store lightmap budget");
    }

    // The scene as the arrival view can draw it, in edit mode (static batching merges the
    // meshes in Play mode): active, enabled renderers only, whatever their owner.
    private static void Budgets(Transform root)
    {
        int[] triangles = new int[4];
        string[] categories = { "store", "exterior", "dressing", "tools and player" };
        int renderers = 0, transparent = 0, facadePanes = 0, largestTexture = 0;
        HashSet<Material> materials = new HashSet<Material>();
        HashSet<Texture> textures = new HashSet<Texture>();
        Transform facade = root.Find("Facade");
        foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            renderers++;
            bool clear = false;
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null) continue;
                materials.Add(m);
                clear |= m.renderQueue >= 3000;
            }
            MeshFilter filter = r.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            // A facade pane fills much of the view from the pavement: one face of a square metre or more.
            if (clear) transparent++;
            if (clear && mesh != null && r.transform.IsChildOf(facade) && DemoGeometry.SurfaceArea(mesh) * 0.5f >= 1) facadePanes++;
            if (mesh == null) continue;
            int count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++) count += (int)mesh.GetIndexCount(i) / 3;
            triangles[Category(r.transform, root)] += count;
        }
        foreach (Material m in materials)
            foreach (string property in m.GetTexturePropertyNames())
            {
                Texture texture = m.GetTexture(property);
                if (texture != null && textures.Add(texture)) largestTexture = Mathf.Max(largestTexture, Mathf.Max(texture.width, texture.height));
            }
        int total = 0;
        string split = "";
        for (int i = 0; i < triangles.Length; i++)
        {
            total += triangles[i];
            split += (i > 0 ? ", " : "") + categories[i] + " " + triangles[i];
        }
        SessionState.SetInt(TrianglesKey, total);
        SessionState.SetInt(RenderersKey, renderers);
        DemoValidation.Info("Budget: triangles=" + total + " (" + split + ") of " + TriangleBudget);
        DemoValidation.Check(total <= TriangleBudget, "Night store triangle budget");
        DemoValidation.Info("Budget: renderers before static batching=" + renderers + " of " + RendererBudget);
        DemoValidation.Check(renderers <= RendererBudget, "Night store renderer budget");
        DemoValidation.Info("Budget: materials=" + materials.Count + " of " + MaterialBudget + "; textures=" + textures.Count + ", largest " + largestTexture + " px of " + TextureBudget);
        DemoValidation.Check(materials.Count <= MaterialBudget && largestTexture <= TextureBudget, "Night store material budget");
        DemoValidation.Info("Budget: transparent renderers=" + transparent + " of " + TransparentBudget + "; facade panes of 1 m2 or more=" + facadePanes + " of " + FacadePaneBudget);
        DemoValidation.Check(transparent <= TransparentBudget && facadePanes <= FacadePaneBudget, "Night store transparency budget");
    }

    // Budget category of a renderer: 0 store, 1 exterior, 2 dressing (and the UV examples), 3 tools and the player rig.
    private static int Category(Transform t, Transform root)
    {
        if (t == root) return 0;
        if (!t.IsChildOf(root)) return 3;
        while (t.parent != root) t = t.parent;
        if (t.name == "Exterior") return 1;
        if (t.name == "Dressing" || t.name == UVTraceBuilder.ExamplesName) return 2;
        if (t.name == "ToolSystem" || t.name == "DeployedTools") return 3;
        return 0;
    }

    // Edit mode numbers (static batching merges meshes in Play mode, so count here).
    private static string Stats(Transform root)
    {
        int triangles = 0, renderers = 0, statics = 0, lightmapped = 0, baked = 0, realtime = 0;
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter filter = r.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                for (int i = 0; i < filter.sharedMesh.subMeshCount; i++) triangles += (int)filter.sharedMesh.GetIndexCount(i) / 3;
            renderers++;
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            if ((flags & StaticEditorFlags.BatchingStatic) != 0) statics++;
            if ((flags & StaticEditorFlags.ContributeGI) != 0 && r.receiveGI == ReceiveGI.Lightmaps) lightmapped++;
        }
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (light.lightmapBakeType == LightmapBakeType.Baked) baked++;
            else realtime++;
        LightProbeGroup probes = root.GetComponentInChildren<LightProbeGroup>();
        return "Night store triangles=" + triangles + "; renderers=" + renderers + "; static=" + statics + "; lightmapped=" + lightmapped
            + "; lights baked=" + baked + ", realtime=" + realtime + " (diagnostic sun, off; the flashlight is created at runtime)"
            + "; light probes placed=" + (probes != null ? probes.probePositions.Length : 0);
    }

    private static void Tick()
    {
        if (!DemoValidation.Running(Key, out int stage)) return;
        try
        {
            if (stage == 1)
            {
                DemoValidation.Info(SessionState.GetString(StatsKey, "stats missing"));
                DemoValidation.TestPoints = L.TestPoints();
                DemoValidation.BeforeReset = DisturbBeforeReset;
                LightingChecks();
                ZoneChecks();
                EntranceChecks();
                SpawnChecks();
                WalkChecks();
                TeleportChecks();
                PlacementChecks();
                ToolStylingChecks();
                CarriedLampChecks();
                GlassChecks();
                MapChecks();
                InspectionChecks();
                RealtimeLightChecks();
                StagedSoundChecks();
                DemoValidation.InterfaceChecks();
                DemoValidation.ToolStage();
                SessionState.SetInt(Key, 2);
            }
            else if (stage == 2 && DemoValidation.PhotoStage()) SessionState.SetInt(Key, 3);
            else if (stage == 3 && DemoValidation.ResetStage())
            {
                ResetChecks();
                // The night's QualitySettings are project settings: put them back before exit.
                DemoValidation.Check(NightQuality.Restore(), "Night quality settings restored after the run");
                Summary();
                DemoValidation.Finish(Key, true, null);
            }
        }
        catch (Exception ex) { NightQuality.Restore(); DemoValidation.Finish(Key, false, ex.ToString()); }
    }

    private static Transform Root()
    {
        GameObject root = GameObject.Find(L.Root);
        if (root == null) throw new Exception("FAILED: " + L.Root + " missing from scene");
        return root.transform;
    }

    // ---------------------------------------------------------------- lighting

    // Runs first in Play mode: the arrival state (night, fixtures off, flashlight off)
    // is checked before anything toggles it. RunNightQuick has no bake: the baked
    // lights are then the realtime shadowless fallback and the bake-only checks skip.
    private static void LightingChecks()
    {
        bool baked = SessionState.GetBool(BakedKey, false);
        Transform root = Root();
        Transform group = root.Find(NightLighting.GroupName);
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        // The arrival state, before anything is switched: realtime lights and the night ambient.
        RealtimeLights(group, out arrivalRealtime, out arrivalFallback, out _);
        arrivalAmbient = RenderSettings.ambientLight;
        DemoValidation.Info("Night lighting " + (baked ? "baked" : "not baked: realtime shadowless fallback (RunNightQuick)"));
        DemoValidation.Check(PlayerSettings.colorSpace == ColorSpace.Linear && QualitySettings.activeColorSpace == ColorSpace.Linear
            && UnityEngine.Rendering.GraphicsSettings.lightsUseLinearIntensity, "Night scene renders in linear colour space");

        Bounds store = new Bounds(new Vector3((L.MinX + L.MaxX) * 0.5f, L.CeilingY * 0.5f, (L.MinZ + L.MaxZ) * 0.5f),
            new Vector3(L.MaxX - L.MinX, L.CeilingY, L.MaxZ - L.MinZ));
        int inside = 0, lenses = 0;
        bool dark = true;
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.isActiveAndEnabled && light.type != LightType.Directional && store.Contains(light.transform.position)) inside++;
        foreach (Renderer r in root.Find(L.CeilingPath).GetComponentsInChildren<Renderer>(true))
            foreach (Material m in r.sharedMaterials)
                if (m != null && (m.name.StartsWith("KitLightLens") || m.name.StartsWith("KitStripLight")))
                {
                    lenses++;
                    dark &= m.GetColor("_EmissionColor").maxColorComponent < 0.01f;
                }
        DemoValidation.Check(lighting.Night && lighting.BakedNight && !lighting.FlashlightOn && inside == 0 && lenses > 0 && dark,
            "Store fixtures are off at arrival");

        LightmapBakeType expected = baked ? LightmapBakeType.Baked : LightmapBakeType.Realtime;
        bool aimed = true;
        foreach (string side in new[] { "Left", "Right" })
        {
            Light beam = LightAt(group, "Headlight_" + side);
            aimed &= beam != null && beam.type == LightType.Spot && beam.lightmapBakeType == expected && beam.isActiveAndEnabled
                && Vector3.Dot(beam.transform.forward, (NightLighting.HeadlightTarget - beam.transform.position).normalized) > 0.95f;
        }
        DemoValidation.Check(aimed, "Headlights are baked spots aimed at the entrance");

        Light moon = LightAt(group, "Moonlight");
        bool outside = moon != null && moon.type == LightType.Directional && moon.lightmapBakeType == expected && RenderSettings.sun == moon;
        for (int i = 0; i < L.Streetlights.Length; i++)
        {
            Light lamp = LightAt(group, "Streetlight_" + i);
            outside &= lamp != null && lamp.lightmapBakeType == expected && Vector3.Distance(lamp.transform.position, root.TransformPoint(NightLighting.LampPosition(i))) < 0.01f;
        }
        DemoValidation.Check(outside, "Streetlights and moonlight are baked");

        lighting.Flashlight(true);
        int casters = 0;
        bool torch = false;
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.isActiveAndEnabled && light.shadows != LightShadows.None && light.lightmapBakeType != LightmapBakeType.Baked)
            {
                casters++;
                torch |= light == lighting.FlashlightLight;
            }
        Light spot = lighting.FlashlightLight;
        bool tuned = spot.shadows == LightShadows.Hard && spot.intensity >= 2.5f && spot.intensity <= 3f && Mathf.Abs(spot.range - 14) < 0.01f;
        lighting.Flashlight(false);
        DemoValidation.Check(casters == 1 && torch && tuned, "Only the flashlight casts realtime shadows");

        Camera view = Camera.main;
        DemoValidation.Check(QualitySettings.pixelLightCount == 4 && QualitySettings.antiAliasing == 4 && !QualitySettings.realtimeReflectionProbes
            && QualitySettings.shadows == ShadowQuality.HardOnly && view != null && !view.allowHDR && view.allowMSAA
            && view.clearFlags == CameraClearFlags.Skybox && view.renderingPath == RenderingPath.Forward, "Night quality settings applied");

        LightProbeGroup probeGroup = group.GetComponentInChildren<LightProbeGroup>();
        List<Vector3> probes = new List<Vector3>();
        if (probeGroup != null) foreach (Vector3 p in probeGroup.probePositions) probes.Add(probeGroup.transform.TransformPoint(p));
        int count = baked ? (LightmapSettings.lightProbes != null ? LightmapSettings.lightProbes.count : 0) : probes.Count;
        bool covered = true;
        foreach (InteriorBounds.Region region in L.Regions())
        {
            Vector2 centre = new Vector2((region.MinX + region.MaxX) * 0.5f, (region.MinZ + region.MaxZ) * 0.5f);
            float nearest = float.MaxValue;
            foreach (Vector3 p in probes) nearest = Mathf.Min(nearest, Vector2.Distance(centre, new Vector2(p.x, p.z)));
            covered &= nearest < 2.5f;
        }
        DemoValidation.Info("Light probes=" + count + " (placed " + probes.Count + ")");
        DemoValidation.Check(count > 150 && covered, "Light probes cover the accessible areas");

        bool reflections = true;
        foreach (string name in NightLighting.ProbeNames)
        {
            Transform t = group.Find(name);
            ReflectionProbe probe = t != null ? t.GetComponent<ReflectionProbe>() : null;
            reflections &= probe != null && probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Baked && probe.boxProjection
                && probe.resolution == NightLighting.ReflectionResolution && (!baked || probe.bakedTexture != null);
        }
        DemoValidation.Check(reflections, "Reflection probes are baked for showroom, office and street");

        Color night = RenderSettings.ambientLight;
        lighting.SetNight(false);
        Light sun = lighting.DiagnosticSun;
        bool day = !lighting.Night && sun != null && sun.isActiveAndEnabled && sun.shadows == LightShadows.None
            && sun.lightmapBakeType == LightmapBakeType.Realtime && RenderSettings.ambientLight.grayscale > 0.2f;
        lighting.SetNight(true);
        bool restored = lighting.Night && !sun.isActiveAndEnabled && Close(RenderSettings.ambientLight, night);
        DemoValidation.Check(day && restored, "Diagnostic daylight toggles and restores the night");

        LightBarBlink bar = DemoValidation.Find<LightBarBlink>();
        bar.Apply(0.1f / bar.Hertz);
        bool red = bar.RedLit && bar.Emission(bar.Red).maxColorComponent > 5 * bar.Emission(bar.Blue).maxColorComponent;
        bar.Apply(0.6f / bar.Hertz);
        bool blue = !bar.RedLit && bar.Emission(bar.Blue).maxColorComponent > 5 * bar.Emission(bar.Red).maxColorComponent;
        Transform car = root.Find("Exterior/PoliceCar");
        DemoValidation.Check(red && blue && bar.transform.IsChildOf(car) && car.GetComponentsInChildren<Light>(true).Length == 0,
            "Light bar blinks without realtime lights");
    }

    // Arrival state captured by LightingChecks (Play mode statics live until the run ends).
    private static int arrivalRealtime, arrivalFallback;
    private static Color arrivalAmbient;

    // Enabled realtime lights: those outside the scene's lighting group (torches, lamps,
    // anything stray), those inside it (RunNightQuick's realtime stand-ins for the
    // baked lights) and how many of all of them cast shadows.
    private static void RealtimeLights(Transform group, out int outside, out int fallback, out int shadows)
    {
        outside = fallback = shadows = 0;
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (!light.isActiveAndEnabled || light.lightmapBakeType == LightmapBakeType.Baked) continue;
            if (group != null && light.transform.IsChildOf(group)) fallback++;
            else outside++;
            if (light.shadows != LightShadows.None) shadows++;
        }
    }

    // Arrival has no realtime light at all once baked (RunNightQuick: only the seven
    // shadowless stand-ins). The flashlight or the UV torch with the two lit lamps the
    // station allows stay within RealtimeLightBudget, and only one of them shadows:
    // the flashlight while it is on, else the lamp lit last (ToolStation.UpdateLampShadows).
    private static void RealtimeLightChecks()
    {
        bool baked = SessionState.GetBool(BakedKey, false);
        Transform group = Root().Find(NightLighting.GroupName);
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        ToolStation station = DemoValidation.Find<ToolStation>();
        int stand = 0;
        foreach (Light light in group.GetComponentsInChildren<Light>(true)) if (light != lighting.DiagnosticSun) stand++;
        bool arrival = arrivalRealtime == 0 && (baked ? arrivalFallback == 0 : arrivalFallback == stand);

        int before = station.DeployedCount;
        DeployedTool first = station.Spawn(DemoToolKind.Lamp), second = station.Spawn(DemoToolKind.Lamp);
        RealtimeLights(group, out int lamps, out _, out int lampShadows);
        bool newest = second != null && second.Lamp.shadows != LightShadows.None && first != null && first.Lamp.shadows == LightShadows.None
            && Mathf.Abs(second.Lamp.range - DemoToolGeometry.LampRange) < 0.01f && Mathf.Abs(first.Lamp.range - DemoToolGeometry.LampUnshadowedRange) < 0.01f;
        lighting.Flashlight(true);
        RealtimeLights(group, out int withTorch, out _, out int torchShadows);
        bool torchOwns = lighting.FlashlightLight.shadows != LightShadows.None && second.Lamp.shadows == LightShadows.None
            && Mathf.Abs(second.Lamp.range - DemoToolGeometry.LampUnshadowedRange) < 0.01f;
        lighting.UVTorch(true);
        RealtimeLights(group, out int withUV, out _, out int uvShadows);
        bool handedBack = second.Lamp.shadows != LightShadows.None && Mathf.Abs(second.Lamp.range - DemoToolGeometry.LampRange) < 0.01f;
        lighting.UVTorch(false);
        station.RemoveTool(second);
        bool remaining = first.Lamp.shadows != LightShadows.None && Mathf.Abs(first.Lamp.range - DemoToolGeometry.LampRange) < 0.01f;
        station.RemoveTool(first);
        RealtimeLights(group, out int after, out _, out _);
        DemoValidation.Info("Realtime lights: arrival " + arrivalRealtime + " (+" + arrivalFallback + " quick-preview stand-ins); two lamps " + lamps
            + "; with flashlight " + withTorch + " (" + torchShadows + " shadowed); with UV torch " + withUV + " (" + uvShadows + " shadowed)");
        DemoValidation.Check(arrival && lamps == 2 && newest && withTorch == 3 && withUV == 3 && Mathf.Max(withTorch, withUV) <= RealtimeLightBudget
            && lampShadows == ShadowLightBudget && torchShadows == ShadowLightBudget && uvShadows == ShadowLightBudget
            && torchOwns && handedBack && remaining && after == 0 && station.DeployedCount == before,
            "Night store realtime light budget");
    }

    private static Light LightAt(Transform group, string name)
    {
        Transform t = group != null ? group.Find(name) : null;
        return t != null ? t.GetComponent<Light>() : null;
    }

    private static bool Close(Color a, Color b) => Mathf.Abs(a.r - b.r) < 0.002f && Mathf.Abs(a.g - b.g) < 0.002f && Mathf.Abs(a.b - b.b) < 0.002f;

    private static void ZoneChecks()
    {
        Transform root = Root();
        bool zones = true;
        foreach (string zone in new[] { "Architecture", "Facade", "Exterior", "Showroom", "Office", "Dressing" })
            zones &= root.Find(zone) != null && root.Find(zone).GetComponentsInChildren<Renderer>().Length > 0;
        DemoValidation.Check(zones && root.Find("Anchors/PlayerSpawn") != null && root.Find("Exterior/PoliceCar/HeadlightLeft") != null,
            "Night store builds every zone");
        int safes = 0, open = 0;
        foreach (Transform t in root.Find("Office"))
        {
            if (!t.name.StartsWith("Safe")) continue;
            safes++;
            Transform door = t.Find("Door");
            if (door != null && Mathf.Abs(Mathf.DeltaAngle(door.localEulerAngles.y, 0)) > 45) open++;
        }
        DemoValidation.Check(safes == 2 && open == 2, "Office holds exactly two open safes");
    }

    // A walker-sized capsule passes the forced entrance; a ray at the middle of an intact
    // bay (clear of its mullions) is stopped by the pane's own thin collider.
    private static void EntranceChecks()
    {
        DemoValidation.Check(!Cast(0) && PaneStops(6), "Entrance is open between the forced door leaves");
    }

    private static bool PaneStops(int bay)
    {
        if (!Physics.Raycast(new Vector3(L.BayCentre(bay), 1.5f, -1), Vector3.forward, out RaycastHit hit, 1.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;
        BoxCollider box = hit.collider as BoxCollider;
        return box != null && hit.collider.transform == Root().Find("Facade/Bay_" + bay) && box.size.z < 0.02f && Mathf.Abs(hit.point.z) < 0.02f;
    }

    private static bool Cast(float x)
    {
        Vector3 bottom = new Vector3(x, 0.35f, -1), top = new Vector3(x, 1.6f, -1);
        return Physics.CapsuleCast(bottom, top, 0.23f, Vector3.forward, 2, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    // Desktop: the walker's reset pose is the spawn anchor, beside the car and facing the
    // entrance. VR: entering VR with the headset off-centre in its play area (head 0.5 m
    // aside, turned 60 degrees) recentres the head over the spawn, facing the shop.
    private static void SpawnChecks()
    {
        Transform root = Root();
        ShopWalkController walker = DemoValidation.Find<ShopWalkController>();
        DemoModeSwitch mode = DemoValidation.Find<DemoModeSwitch>();
        XRLocomotion locomotion = DemoValidation.Find<XRLocomotion>();
        Vector3 spawn = root.TransformPoint(L.PlayerSpawn), entrance = root.TransformPoint(L.Anchor("EntranceCentre"));
        Vector3 toShop = entrance - spawn;
        toShop.y = 0;
        walker.ResetPosition();
        Transform anchor = root.Find("Anchors/PlayerSpawn");
        bool desktop = Flat(walker.transform.position - spawn) < 0.01f && anchor != null && Flat(anchor.position - spawn) < 0.001f
            && Vector3.Dot(walker.View.forward, toShop.normalized) > 0.9f;
        Collider car = root.Find("Exterior/PoliceCar").GetComponent<Collider>();
        Vector3 nearest = car != null ? car.ClosestPoint(spawn + Vector3.up * 0.8f) : spawn + Vector3.one * 99;
        float gap = Flat(nearest - spawn);
        bool beside = gap > 0.3f && gap < 1.5f;

        Transform head = locomotion.Head;
        mode.Apply(true);
        head.localPosition = new Vector3(0.5f, 1.6f, -0.4f);
        head.localRotation = Quaternion.Euler(0, 60, 0);
        bool pending = mode.RecentrePending;
        mode.RecentreNow();
        Vector3 forward = head.forward;
        forward.y = 0;
        bool vr = pending && !mode.RecentrePending && Flat(head.position - spawn) < 0.01f && Mathf.Abs(locomotion.transform.position.y - spawn.y) < 0.001f
            && Vector3.Angle(forward, Quaternion.Euler(0, L.PlayerYaw, 0) * Vector3.forward) < 0.5f && Vector3.Dot(forward.normalized, toShop.normalized) > 0.9f;
        mode.Apply(false);
        walker.ResetPosition();
        DemoValidation.Info("Spawn " + spawn + "; car body " + gap.ToString("0.00") + " m away");
        DemoValidation.Check(desktop && beside && vr && !mode.VRActive, "Visitor starts beside the police car facing the shop");
    }

    private static void WalkChecks()
    {
        ShopWalkController walker = DemoValidation.Find<ShopWalkController>();
        walker.ReadDesktopInput = false;
        XRLocomotion locomotion = DemoValidation.Find<XRLocomotion>();
        walker.ResetPosition();
        DemoValidation.Check(Flat(walker.transform.position - L.PlayerSpawn) < 0.01f && walker.Bounds.Contains(L.PlayerSpawn),
            "Player spawns beside the police car");
        // The kerb is the only step on the route; the walker's capsule and the teleport both take it.
        CharacterController body = walker.GetComponent<CharacterController>();
        bool reached = body.stepOffset > -L.StreetY && locomotion.MaxStepHeight > -L.StreetY;
        string missed = null;
        foreach (Vector3 waypoint in L.Route)
        {
            WalkTo(walker, waypoint);
            if (Flat(walker.transform.position - waypoint) > 0.3f && missed == null) missed = waypoint.ToString();
        }
        reached &= missed == null;
        DemoValidation.Info("Walk route " + (missed == null ? "complete" : "stopped before " + missed + " at " + walker.transform.position));
        DemoValidation.Check(reached, "Visitor walks from the car to the safe");
        walker.ResetPosition();
        for (int i = 0; i < 200; i++) walker.MovePlanar(Vector3.back * 0.05f);
        DemoValidation.Check(walker.transform.position.z > L.StagingBay.MinZ - 0.05f && walker.Bounds.Contains(walker.transform.position),
            "Street beyond the staging bay stays out of reach");
        walker.ResetPosition();
    }

    private static void WalkTo(ShopWalkController walker, Vector3 target)
    {
        for (int i = 0; i < 400; i++)
        {
            Vector3 d = target - walker.transform.position;
            d.y = 0;
            if (d.magnitude < 0.03f) return;
            walker.MovePlanar(d.normalized * Mathf.Min(0.05f, d.magnitude));
        }
    }

    private static float Flat(Vector3 d) => new Vector2(d.x, d.z).magnitude;

    private static void TeleportChecks()
    {
        ShopWalkController walker = DemoValidation.Find<ShopWalkController>();
        XRLocomotion locomotion = DemoValidation.Find<XRLocomotion>();
        walker.ResetPosition();
        Vector3 head = locomotion.Head.position, pavement = new Vector3(0, 0, -1.5f), street = new Vector3(0, L.StreetY, -15);
        bool accepts = locomotion.FindDestination(new Ray(head, pavement - head), out Vector3 landing)
            && Flat(landing - pavement) < 0.05f && locomotion.TryTeleport(landing);
        walker.ResetPosition();
        bool refuses = !locomotion.FindDestination(new Ray(head, street - head), out _) && !locomotion.TryTeleport(street);
        walker.ResetPosition();
        DemoValidation.Check(accepts && refuses, "Teleport accepts the pavement and refuses the far street");
    }

    // The desktop pointer's carry-and-place path, pointed straight down.
    private static void PlacementChecks()
    {
        DesktopInteractor interactor = DemoValidation.Find<DesktopInteractor>();
        interactor.ReadDesktopInput = false;
        DemoValidation.Check(Place(interactor, new Vector3(3, 0, -1.5f)), "Tools can be placed on the pavement");
        DemoValidation.Check(Place(interactor, new Vector3(6.5f, L.StreetY, -4.0f)), "Tools can be placed in the staging bay");
    }

    // The night build styles its own tool prefabs (DemoToolsBuilder.StyleStation) in its
    // own folder, so it never overwrites the shop's GeneratedTools prefabs.
    private static void ToolStylingChecks()
    {
        ToolStation station = DemoValidation.Find<ToolStation>();
        Material black = null, white = null;
        foreach (MeshRenderer r in station.ScalePrefab.GetComponentsInChildren<MeshRenderer>())
        {
            if (r.name == "BandBlack") black = r.sharedMaterial;
            if (r.name == "BandWhite") white = r.sharedMaterial;
        }
        bool bands = black != null && white != null && black != white && black.name != "Default-Material" && white.name != "Default-Material";
        TextMesh number = station.MarkerPrefab.NumberLabel;
        MeshRenderer numberRenderer = number != null ? number.GetComponent<MeshRenderer>() : null;
        bool label = number != null && number.GetComponent<DepthTestedLabel>() != null && numberRenderer.sharedMaterial != null
            && numberRenderer.sharedMaterial.shader.name == "CrimeScene/WorldText";
        MeshRenderer ribbon = station.TapePrefab != null && station.TapePrefab.Ribbon != null ? station.TapePrefab.Ribbon.GetComponent<MeshRenderer>() : null;
        bool tape = ribbon != null && ribbon.sharedMaterial != null && ribbon.sharedMaterial.shader.name == "CrimeScene/Tape"
            && station.TapePrefab.FrontWarning != null && station.TapePrefab.BackWarning != null;
        DemoValidation.Check(bands && label && tape, "Night tool prefabs are styled (scale bands, depth-tested numbers, police tape)");
        bool own = true;
        foreach (UnityEngine.Object prefab in new UnityEngine.Object[] { station.ConePrefab, station.MarkerPrefab, station.TapePostPrefab,
            station.ScalePrefab, station.MeasurePrefab, station.LampPrefab, station.TapePrefab, station.MeasureTapePrefab })
            own &= prefab != null && AssetDatabase.GetAssetPath(prefab).StartsWith(NightStoreBuilder.Folder + "/");
        DemoValidation.Check(own, "Night tool prefabs live in the night store's own folder");
    }

    // A lamp carried low over the staging bay rests its feet on the street, not at the
    // showroom floor height (HandInteractor clamps to Bounds.FloorAt).
    private static void CarriedLampChecks()
    {
        DesktopInteractor desktop = DemoValidation.Find<DesktopInteractor>();
        HandInteractor hand = desktop.transform.Find("RightHand").GetComponent<HandInteractor>();
        Vector3 position = hand.transform.position;
        hand.transform.position = new Vector3(6.5f, 0.3f, -4.0f);
        DeployedTool lamp = hand.SpawnIntoHand(DemoToolKind.Lamp);
        float lampBase = lamp != null ? lamp.transform.position.y : 99;
        if (lamp != null) hand.RemoveHeld();
        hand.transform.position = position;
        DemoValidation.Check(lamp != null && Mathf.Abs(lampBase - L.StreetY) < 0.001f, "Carried lamp rests on the street in the staging bay");
    }

    private static bool Place(DesktopInteractor interactor, Vector3 floor)
    {
        DeployedTool cone = interactor.SpawnIntoHand(DemoToolKind.Cone);
        if (cone == null) return false;
        interactor.Carry(new Ray(floor + Vector3.up * 2, Vector3.down));
        interactor.Place();
        bool placed = interactor.Held == null && Vector3.Distance(cone.transform.position, floor) < 0.02f;
        interactor.Remove(cone);
        return placed;
    }

    // Every vertex of the entrance glass fans rests within 2 mm above the surface under it.
    private static void GlassChecks()
    {
        List<Vector3> points = NightStoreDressing.GlassVertices(Root().Find("Dressing"));
        int flush = 0;
        foreach (Vector3 p in points)
            if (Physics.Raycast(p + Vector3.up * 0.05f, Vector3.down, out RaycastHit hit, 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && p.y - hit.point.y >= -0.0005f && p.y - hit.point.y <= 0.002f) flush++;
        DemoValidation.Info("Entrance glass vertices=" + points.Count + "; flush=" + flush);
        DemoValidation.Check(points.Count > 300 && flush == points.Count, "Loose glass sits flush on its surfaces");
    }

    // ---------------------------------------------------------------- inspection equipment

    // UV torch, the hidden placeholder traces and the scene lamp. The torch is posed
    // directly (it rides the desktop view) and the traces settled at once, so every
    // check is synchronous; everything is switched off and removed afterwards.
    private static void InspectionChecks()
    {
        Transform root = Root();
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        Transform examples = root.Find(UVTraceBuilder.ExamplesName);
        UVTrace[] traces = examples != null ? examples.GetComponentsInChildren<UVTrace>(true) : new UVTrace[0];
        DemoValidation.Check(examples != null && lighting.UVExamples == examples.gameObject && !lighting.UVExamplesVisible
            && traces.Length == 3 && UVTrace.ActiveCount == 0, "UV example traces start hidden");

        lighting.Flashlight(true);
        lighting.UVTorch(true);
        bool uvOnly = lighting.UVTorchOn && !lighting.FlashlightOn;
        lighting.Flashlight(true);
        bool whiteOnly = lighting.FlashlightOn && !lighting.UVTorchOn;
        lighting.Flashlight(false);
        Light uv = lighting.UVTorchLight;
        bool tuned = uv != null && uv.type == LightType.Spot && uv.shadows == LightShadows.None && Mathf.Abs(uv.range - 6) < 0.01f
            && Mathf.Abs(uv.spotAngle - 32) < 0.01f && lighting.UVTorchModel != null;
        DemoValidation.Check(uvOnly && whiteOnly && tuned && !lighting.UVTorchOn && !lighting.FlashlightOn, "UV torch and white flashlight are exclusive");

        lighting.SetUVExamples(true);
        lighting.UVTorch(true);
        Physics.SyncTransforms();
        Transform dropsObject = examples.Find("UVTrace_SafeDrops");
        UVTrace drops = dropsObject != null ? dropsObject.GetComponent<UVTrace>() : null;
        if (drops == null) throw new Exception("FAILED: UVTrace_SafeDrops missing");
        // Placed from the layout: on the office floor, clear of the large safe's front face.
        L.Fixture safe = L.Find("SafeLarge");
        // (Safe 0.9 m deep; the drop quad's centre at least 0.15 m clear of its front face.)
        Vector3 dropsLocal = root.InverseTransformPoint(drops.transform.position);
        DemoValidation.Check(L.Office.Contains(dropsLocal) && dropsLocal.z < safe.Position.z - 0.45f - 0.15f,
            "UV drops lie in the office in front of the large safe");
        // Peak glow (linear colour * intensity) stays at or below 1 in the LDR night view.
        Material traceMaterial = drops.GetComponent<MeshRenderer>().sharedMaterial;
        Color glowLinear = traceMaterial.GetColor("_GlowColor").linear;
        DemoValidation.Check(glowLinear.maxColorComponent * traceMaterial.GetFloat("_Intensity") <= 1.0f,
            "UV trace glow keeps its mask detail below the LDR clip");
        Vector3 target = drops.transform.position, above = target + new Vector3(0, 1.5f, -0.8f);
        float inside = AimUV(lighting, above, target, drops);
        DemoValidation.Check(inside > 0.9f && UVTrace.ActiveCount == 3 && Shader.GetGlobalVector(DemoLighting.UVParamsId).z > 0.99f,
            "UV trace glows inside the beam");
        DemoValidation.Check(AimUV(lighting, above, above + Vector3.forward, drops) < 0.01f, "UV trace stays dark outside the cone");
        // In the side room beyond the office's right partition, aimed straight at the drops:
        // in range and in the cone, so only the wall can keep the trace dark.
        Vector3 beyond = new Vector3(L.OfficeHalf + 1.5f, 1.5f, target.z);
        bool wall = Vector3.Distance(beyond, target) < uv.range && beyond.x > L.OfficeHalf + L.Partition;
        DemoValidation.Check(wall && AimUV(lighting, beyond, target, drops) < 0.01f, "UV trace stays dark behind a wall");
        lighting.UVTorch(false);
        lighting.SetUVExamples(false);

        ToolStation station = DemoValidation.Find<ToolStation>();
        int before = station.DeployedCount;
        Transform sample = station.transform.Find("Sample_" + DemoToolKind.Lamp);
        DeployedTool first = station.Spawn(DemoToolKind.Lamp), second = station.Spawn(DemoToolKind.Lamp), third = station.Spawn(DemoToolKind.Lamp);
        Light spot = third != null ? third.Lamp : null;
        float tilt = spot != null ? Vector3.Angle(spot.transform.forward, third.transform.forward) : 0;
        bool lamp = spot != null && spot.type == LightType.Spot && spot.shadows == LightShadows.Hard
            && spot.shadowResolution == UnityEngine.Rendering.LightShadowResolution.Low && Mathf.Abs(spot.range - 8) < 0.01f
            && Mathf.Abs(spot.spotAngle - 75) < 0.01f && tilt > 14 && tilt < 16 && spot.transform.forward.y < 0;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        bool glow = third != null && third.LampFace != null && first.LampFace != null;
        if (glow)
        {
            third.LampFace.GetPropertyBlock(block);
            glow = block.GetColor("_EmissionColor").maxColorComponent > 1;
            first.LampFace.GetPropertyBlock(block);
            glow &= block.GetColor("_EmissionColor").maxColorComponent < 0.01f;
        }
        DemoValidation.Check(sample != null && sample.GetComponentsInChildren<Light>(true).Length == 0 && lamp && glow
            && !first.LampLit && second.LampLit && third.LampLit && station.LitLampCount == ToolStation.MaxLitLamps,
            "Scene lamp lights its spot and limits lit lamps to two");
        station.RemoveTool(second);
        bool remaining = station.LitLampCount == 1 && third.LampLit && !first.LampLit;
        station.RemoveTool(first);
        station.RemoveTool(third);
        int orphans = 0;
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.isActiveAndEnabled && light.GetComponentInParent<DeployedTool>() == null && light.name == "LampLight") orphans++;
        int lampLights = station.DeploymentRoot.GetComponentsInChildren<Light>().Length;
        DemoValidation.Check(remaining && orphans == 0 && lampLights == 0 && station.LitLampCount == 0 && station.DeployedCount == before,
            "Scene lamp removes cleanly");
    }

    // Poses the UV torch at from, aimed at at, settles the traces and returns the trace's visibility.
    private static float AimUV(DemoLighting lighting, Vector3 from, Vector3 at, UVTrace trace)
    {
        lighting.UVTorchLight.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
        lighting.SettleUVTraces();
        return trace.Visibility;
    }

    private static void MapChecks()
    {
        DemoOverviewMap map = DemoValidation.Find<DemoOverviewMap>();
        bool inside = true;
        foreach (Vector3 p in new[] { L.PlayerSpawn, new Vector3(0, 0, 11.5f), L.CarPosition, new Vector3(L.StagingBay.MinX, 0, L.StagingBay.MinZ) })
        {
            Vector2 uv = map.ToMap(p);
            inside &= uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1;
        }
        // Layout-driven framing: every movement region's corners are on the map.
        foreach (InteriorBounds.Region r in L.Regions())
            foreach (Vector3 corner in new[] { new Vector3(r.MinX, 0, r.MinZ), new Vector3(r.MaxX, 0, r.MaxZ) })
            {
                Vector2 uv = map.ToMap(corner);
                inside &= uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1;
            }
        inside &= Vector2.Distance(map.Centre, L.MapCentre) < 0.001f && Mathf.Abs(map.HalfSize - L.MapHalfSize) < 0.001f;
        map.Render();
        DemoValidation.Check(inside && map.Texture != null, "Overview map frames the store and the staging bay");

        // The beacon shares the player's night regions: a click on the far street lands
        // on the staging bay's street surface, one beyond the right wall on the showroom
        // edge, one in the office stays put.
        ShopWalkController walker = DemoValidation.Find<ShopWalkController>();
        DemoBeacon beacon = DemoValidation.Find<DemoBeacon>();
        bool shared = beacon.Bounds == walker.Bounds && SameRegions(walker.Bounds.Regions);
        beacon.Place(new Vector3(0, 0, -15));
        bool street = Vector3.Distance(beacon.Position, new Vector3(0, L.StreetY, L.StagingBay.MinZ)) < 0.001f;
        // The side probe is built from the layout and asserted to be nearest the showroom,
        // so a later retune of the pavement or bay cannot silently move the target.
        Vector3 side = new Vector3(L.Showroom.MaxX + 1, 1, (L.Showroom.MinZ + L.Showroom.MaxZ) * 0.5f);
        float showroomDistance = (L.Showroom.Clamp(side) - side).sqrMagnitude;
        bool wall = true;
        foreach (InteriorBounds.Region r in L.Regions())
            if (!r.Equals(L.Showroom)) wall &= (r.Clamp(side) - side).sqrMagnitude > showroomDistance;
        beacon.Place(side);
        wall &= Vector3.Distance(beacon.Position, new Vector3(L.Showroom.MaxX, 0, side.z)) < 0.001f;
        Vector3 office = new Vector3(0.5f, 0, 10.5f);
        beacon.Place(office);
        bool kept = Vector3.Distance(beacon.Position, office) < 0.001f && L.Office.Contains(office);
        beacon.Clear();
        DemoValidation.Check(shared && street && wall && kept && !beacon.Active, "Beacon clamps to the night store regions");

        // The review sketch draws the same regions: its view box is theirs (plus its
        // margin) and holds the spawn, the car, the safes and the staging bay's far corners.
        SessionReviewRecorder review = DemoValidation.Find<SessionReviewRecorder>();
        Rect box = review.SketchViewBox();
        bool framed = review.Bounds == walker.Bounds && box == SessionReviewRecorder.SketchViewBox(L.Regions());
        foreach (Vector3 p in new[] { L.PlayerSpawn, L.CarPosition, L.Find("SafeLarge").Position, L.Find("SafeTall").Position,
            new Vector3(L.StagingBay.MinX, 0, L.StagingBay.MinZ), new Vector3(L.StagingBay.MaxX, 0, L.StagingBay.MinZ), new Vector3(L.MinX, 0, L.MaxZ) })
            framed &= box.Contains(SessionReviewRecorder.SketchPoint(p, box));
        DemoValidation.Check(framed, "Review sketch frames the night store");
    }

    private static bool SameRegions(InteriorBounds.Region[] regions)
    {
        InteriorBounds.Region[] night = L.Regions();
        if (regions == null || regions.Length != night.Length) return false;
        for (int i = 0; i < night.Length; i++)
            if (!regions[i].Equals(night[i])) return false;
        return true;
    }

    // ---------------------------------------------------------------- reset

    // Called by the shared photo stage just before it resets: the instructor left the
    // diagnostic daylight on, the UV examples showing, the flashlight on, a lit lamp in
    // the showroom and a beacon on the floor.
    private static void DisturbBeforeReset()
    {
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        lighting.SetNight(false);
        lighting.SetUVExamples(true);
        lighting.Flashlight(true);
        DeployedTool lamp = DemoValidation.Find<ToolStation>().Spawn(DemoToolKind.Lamp);
        if (lamp != null) lamp.PlaceAt(new Vector3(-2, 0, 3), 30);
        DemoValidation.Find<DemoBeacon>().Place(new Vector3(1, 0, 3));
    }

    // After the shared reset stage: the arrival again, photographs kept, the camera on the table.
    private static void ResetChecks()
    {
        Transform group = Root().Find(NightLighting.GroupName);
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        ToolStation station = DemoValidation.Find<ToolStation>();
        EvidenceCamera evidence = DemoValidation.Find<EvidenceCamera>();
        RealtimeLights(group, out int realtime, out int fallback, out _);
        bool night = lighting.Night && lighting.DiagnosticSun != null && !lighting.DiagnosticSun.isActiveAndEnabled
            && Close(RenderSettings.ambientLight, arrivalAmbient) && realtime == 0 && fallback == arrivalFallback;
        bool off = !lighting.FlashlightOn && !lighting.UVTorchOn && !lighting.UVExamplesVisible && station.LitLampCount == 0
            && station.DeployedCount == 0 && !DemoValidation.Find<DemoBeacon>().Active;
        bool kept = evidence.PhotoCount >= 1 && System.IO.File.Exists(evidence.LastPhotoPath);
        bool table = Vector3.Distance(evidence.transform.localPosition, L.Tools.Camera) < 0.001f
            && Vector3.Distance(evidence.transform.position, Root().TransformPoint(L.Tools.Camera)) < 0.01f;
        DemoValidation.Check(night && off && kept && table, "Reset restores the night arrival");
    }

    // The closing line, like the other validators' totals: checks, bake, lightmaps, budget totals.
    private static void Summary()
    {
        string line = "Night store summary: checks passed=" + DemoValidation.ChecksPassed
            + "; bake=" + SessionState.GetFloat(BakeSecondsKey, 0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s"
            + (SessionState.GetBool(BakedKey, false) ? "" : " (not baked)")
            + "; lightmaps=" + LightmapSettings.lightmaps.Length
            + "; triangles=" + SessionState.GetInt(TrianglesKey, 0) + "; renderers=" + SessionState.GetInt(RenderersKey, 0);
        DemoValidation.Info(line);
        Debug.Log(line);
    }
}
#endif
