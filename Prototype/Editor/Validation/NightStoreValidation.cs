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
// Output goes to Docs/Verification/Night. Invoke either with -executeMethod. The realism
// packs' checks live in the area files of this partial class (NightStoreValidationStreet,
// -Interior, -Dressing, -Lighting, -Sound and -Presenter), the fixture-detail round's in
// -Detail, called from Generate and Tick; their shared geometry helpers are DemoValidation's.
[InitializeOnLoad]
public static partial class NightStoreValidation
{
    private const string Key = "NightStoreValidationStage";
    private const string BakeKey = "NightStoreValidationBake";
    private const string StatsKey = "NightStoreValidationStats";
    private const string BakedKey = "NightStoreValidationBaked";
    private const string BakeSecondsKey = "NightStoreValidationBakeSeconds";
    private const string TrianglesKey = "NightStoreValidationTriangles";
    private const string RenderersKey = "NightStoreValidationRenderers";

    // VR budgets for a PC-tethered Quest (see Budgets). Each holds the measured scene
    // with headroom for the instructor's tools (materials: 136, October 15, unverified: the
    // realism wiring adds +52, counted from the newmtl and map_Kd lines of the placed models'
    // MTLs after PackMaterial's sharing rules, on an estimated ~75 before it, about 127 in all,
    // plus headroom for the 5 hidden presenter-set materials; set it to the logged
    // "Budget: materials=" value + 8 after the first RunNightQuick); the transparency budget became
    // reachable once each case's panes were merged (NightStoreBuilder.MergeCaseGlass).
    public const int TriangleBudget = 220000, RendererBudget = 900, MaterialBudget = 136, TextureBudget = 2048;
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
            if (reimported > 0) DemoValidation.Info("Third-party assets reimported to the import rules (models, then pack textures and audio): " + reimported);
            DemoValidation.LauncherChecks();
            // Fixture library first (it builds and removes its galleries at the origin).
            StoreKitValidation.Check();
            AssetLibraryChecks();
            GameObject root = NightStoreBuilder.Build(true);
            StaticChecks(root.transform);
            PackImportChecks();
            DressingChecks(root.transform);
            StagedAssetChecks(root.transform);
            Camera camera = DemoValidation.CreatePreviewCamera(150);
            NightLighting.Apply(root.transform, camera);
            PoliceLookChecks(root.transform);
            // The realism packs as built, before the first save: lights, lenses, glow cards and
            // lightmap scales final, no bake, no realtime fallback and no player rig yet, the
            // builders' placement records still valid (StreetPackChecks syncs physics first).
            StreetPackChecks(root.transform);
            InteriorPackChecks(root.transform);
            ClosingProcedureChecks(root.transform);
            DecalChecks(root.transform);
            LightingPackChecks(root.transform);
            PresenterBuildChecks(root.transform);
            CreditChecks(root.transform);
            DetailChecks(root.transform);
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
            LightmapChecks(bake, root.transform);
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

    // The eight concept views of Docs/References/LayoutReferences/NightStore (manifest.json) at
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

    // Edit mode, after the build's import sync (ThirdPartyModelImport.GenerateLightmapUVs, which
    // ends with PackImport.Apply): the realism packs import by their folder and suffix rules,
    // the older packs keep their own settings, and pack props carry no lightmap UVs except the
    // buildings and the rug. Each sample is reported, and checked only when its pack was synced.
    private static void PackImportChecks()
    {
        string root = DemoAssetLibrary.Root + "/";
        int sampled = 0;
        bool textures = true;
        foreach (string file in new[] { "PBRInterior/FloorTile_Normal.png", "PBRInterior/FloorTile_MetallicSmoothness.png",
            "PBRExterior/AsphaltWet_AO.jpg", "WallGraphics/Store/Sheet_StaffRota.jpg", "NightSky/cookie_streetlight_512.png",
            "StreetModels/DT_MaskedDetails_Color.png", "SurfaceDecals/GlassShards_Scatter.png", "NightSky/city_night_sky_overcast_1k.hdr" })
        {
            TextureImporter t = AssetImporter.GetAtPath(root + file) as TextureImporter;
            if (t == null) { DemoValidation.Info("Pack import sample absent, not checked: " + file); continue; }
            sampled++;
            // The rule table, plus the planned values themselves, so a table that drifted fails too.
            bool ok = PackImport.Expected(t, root + file, false);
            switch (file)
            {
                case "PBRInterior/FloorTile_Normal.png": ok &= t.textureType == TextureImporterType.NormalMap && t.maxTextureSize == 2048; break;
                case "PBRInterior/FloorTile_MetallicSmoothness.png":
                case "PBRExterior/AsphaltWet_AO.jpg": ok &= t.textureType == TextureImporterType.Default && !t.sRGBTexture; break;
                case "WallGraphics/Store/Sheet_StaffRota.jpg":
                    ok &= t.npotScale == TextureImporterNPOTScale.None && t.wrapModeU == TextureWrapMode.Clamp && t.wrapModeV == TextureWrapMode.Clamp; break;
                case "NightSky/cookie_streetlight_512.png":
                    ok &= t.textureType == TextureImporterType.Cookie && t.wrapModeU == TextureWrapMode.Clamp && t.wrapModeV == TextureWrapMode.Clamp && t.borderMipmap; break;
                case "StreetModels/DT_MaskedDetails_Color.png":
                    ok &= t.alphaIsTransparency && t.mipMapsPreserveCoverage && Mathf.Abs(t.alphaTestReferenceValue - 0.5f) < 0.001f; break;
                case "SurfaceDecals/GlassShards_Scatter.png":
                    ok &= t.maxTextureSize == 1024 && t.wrapModeU == TextureWrapMode.Clamp && t.wrapModeV == TextureWrapMode.Clamp && t.alphaIsTransparency; break;
                default: ok &= t.textureShape == TextureImporterShape.Texture2D && !t.mipmapEnabled; break;
            }
            DemoValidation.Info("Pack import " + file + ": " + t.textureType + ", sRGB " + t.sRGBTexture + ", wrap " + t.wrapModeU + "/" + t.wrapModeV
                + ", max " + t.maxTextureSize + ", " + t.textureCompression + (ok ? "" : " (differs from the rule)"));
            textures &= ok;
        }
        if (sampled > 0) DemoValidation.Check(textures, "Pack textures follow the folder and suffix import rules");

        int again = PackImport.Apply();
        if (again > 0) DemoValidation.Info("Pack textures and audio reimported again on the second run: " + again);
        DemoValidation.Check(again == 0, "PackImport.Apply leaves nothing to reimport on a second run");

        bool alone = PackImport.Governs(root + "PBRInterior/FloorTile_Color.jpg")
            && PackImport.PackOf(root + "WallGraphics/Atlas/NightStoreInterior_Color.png") == "WallGraphics";
        foreach (string pack in new[] { "ReferenceTextures", "NightMaterials", "NightAudio", "KayKitCity", "StoreKit" })
            alone &= !PackImport.Governs(root + pack + "/Sample_Color.png");
        alone &= !PackImport.Governs(root + "StreetModels/Source~/Preview_LODs.jpg") && !PackImport.Governs(root + "NightSky/Generators~/gen_sprites.py")
            && !PackImport.Governs(root + "MusicArchive/MenuTheme.ogg") && !PackImport.Governs("Assets/Elsewhere/PBRInterior/FloorTile_Color.jpg");
        DemoValidation.Check(alone, "Import rules leave the older packs alone");

        sampled = 0;
        bool models = true;
        foreach (string file in new[] { "StreetModels/PoliceCar.obj", "StreetModels/StreetLamp.obj", "StreetModels/ApartmentBlock.obj",
            "InteriorModels/Rug.obj", "KayKitCity/car_police.obj" })
        {
            ModelImporter m = AssetImporter.GetAtPath(root + file) as ModelImporter;
            if (m == null) { DemoValidation.Info("Model import sample absent, not checked: " + file); continue; }
            sampled++;
            bool wanted = !file.StartsWith("StreetModels/PoliceCar", StringComparison.Ordinal) && !file.StartsWith("StreetModels/StreetLamp", StringComparison.Ordinal);
            float planned = file.StartsWith("StreetModels/ApartmentBlock", StringComparison.Ordinal) ? 6 : file.StartsWith("InteriorModels/Rug", StringComparison.Ordinal) ? 16 : 0;
            bool ok = ThirdPartyModelImport.Expected(m, root + file, false) && m.generateSecondaryUV == wanted && MeshesHaveUV2(root + file, wanted);
            if (planned > 0)
                ok &= m.secondaryUVMarginMethod == ModelImporterSecondaryUVMarginMethod.Calculate && Mathf.Abs(m.secondaryUVMinLightmapResolution - planned) < 0.01f;
            DemoValidation.Info("Model import " + file + ": uv2 " + m.generateSecondaryUV + ", margin " + m.secondaryUVMarginMethod
                + " at " + m.secondaryUVMinLightmapResolution + " texels per unit" + (ok ? "" : " (differs from the rule)"));
            models &= ok;
        }
        if (sampled > 0) DemoValidation.Check(models, "Prop packs import without lightmap UVs; buildings, the rug and KayKit keep them with their planned chart margins");

        sampled = 0;
        bool audio = true;
        foreach (string file in new[] { "NightSoundscape/NightCity_Loop.ogg", "NightSoundscape/FootstepHard_01.wav", "Music/MenuTheme.ogg" })
        {
            AudioImporter a = AssetImporter.GetAtPath(root + file) as AudioImporter;
            if (a == null) { DemoValidation.Info("Pack import sample absent, not checked: " + file); continue; }
            sampled++;
            AudioImporterSampleSettings s = a.defaultSampleSettings;
            bool ok = PackImport.Expected(a, root + file, false);
            if (file.EndsWith("_Loop.ogg", StringComparison.Ordinal))
                ok &= s.loadType == AudioClipLoadType.CompressedInMemory && s.compressionFormat == AudioCompressionFormat.Vorbis && a.loadInBackground;
            else if (file.EndsWith(".wav", StringComparison.Ordinal))
                ok &= s.loadType == AudioClipLoadType.DecompressOnLoad && s.compressionFormat == AudioCompressionFormat.PCM;
            else ok &= s.loadType == AudioClipLoadType.Streaming && s.compressionFormat == AudioCompressionFormat.Vorbis && a.loadInBackground;
            DemoValidation.Info("Pack import " + file + ": " + s.loadType + ", " + s.compressionFormat + " " + s.quality + ", background " + a.loadInBackground);
            audio &= ok;
        }
        if (sampled > 0) DemoValidation.Check(audio, "Pack audio imports: loops compressed in memory, one-shots PCM, music streaming");
    }

    // Edit mode, after the lighting pass: Koda's decision 1 (country and police service, for
    // Koda.md) stays open, so every police choice comes from PoliceLook and the build keeps
    // today's provisional look: the KayKit car (or its stand-in) with its 0.38 x 0.07 x 0.16
    // light-bar halves at x +-0.2 and 0.3 x 0.12 lenses, 1.5 Hz, the CRIME SCENE tape, no
    // siren or radio events, no radio loop and no police figure. Fails on purpose while
    // another PoliceLook.Service is selected for review.
    private static void PoliceLookChecks(Transform root)
    {
        PoliceLook.Preset look = PoliceLook.Active;
        Transform car = root.Find("Exterior/PoliceCar");
        Transform red = car != null ? car.Find("LightBar/Red") : null, blue = car != null ? car.Find("LightBar/Blue") : null;
        Transform lens = car != null ? car.Find("HeadlightLeft/Lens") : null;
        LightBarBlink bar = car != null ? car.GetComponentInChildren<LightBarBlink>(true) : null;
        AmbientEvents events = root.GetComponentInChildren<AmbientEvents>(true);
        ToolStation station = UnityEngine.Object.FindFirstObjectByType<ToolStation>();
        SceneTape tape = station != null ? station.TapePrefab : null;
        bool provisional = PoliceLook.Current == PoliceLook.Service.Provisional && NightStoreBuilder.PlacedCar.Model == PoliceLook.KayKitCar.Model
            && look.Sirens.Length == 0 && look.Radio.Length == 0 && string.IsNullOrEmpty(look.GuardFigure) && L.Tools.TapeText == DemoToolsBuilder.DefaultTapeText;
        bool built = red != null && blue != null && lens != null && bar != null
            && Vector3.Distance(red.localPosition, new Vector3(-0.2f, 0, 0)) < 0.001f && Vector3.Distance(blue.localPosition, new Vector3(0.2f, 0, 0)) < 0.001f
            && Vector3.Distance(red.localScale, new Vector3(0.38f, 0.07f, 0.16f)) < 0.001f && Vector3.Distance(lens.localScale, new Vector3(0.3f, 0.12f, 1)) < 0.001f
            && Mathf.Abs(bar.Hertz - 1.5f) < 0.001f
            && (events == null || events.IndexOf(NightAudioBuilder.SirenGroup) < 0 && events.IndexOf(NightAudioBuilder.RadioGroup) < 0)
            && root.Find(NightAudioBuilder.SoundscapeName + "/" + NightAudioBuilder.RadioLoopName) == null
            && root.Find("Exterior/" + NightStreet.PeopleGroup + "/" + PoliceLook.RealisticUS.GuardFigure) == null
            && tape != null && tape.FrontWarning != null && tape.FrontWarning.text == DemoToolsBuilder.DefaultTapeText;
        DemoValidation.Info("Police look: " + look.Name + "; car placed " + NightStoreBuilder.PlacedCar.Model);
        DemoValidation.Check(provisional && built, "Police choices come from the PoliceLook setting, Provisional by default");
    }

    // Every mesh of the model has (wanted) or lacks a second UV set; false without a mesh.
    private static bool MeshesHaveUV2(string path, bool wanted)
    {
        bool any = false, ok = true;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
        {
            Mesh mesh = asset as Mesh;
            if (mesh == null || mesh.vertexCount == 0) continue;
            any = true;
            ok &= mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1) == wanted;
        }
        return any && ok;
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

    // Edit mode: the palette's texture sets (packed maps, ceiling and facade) and the
    // NightGraphics signs, each checked only when its files were synced into the project
    // (otherwise the builder keeps the scalar finishes and places no sign, and this reports it).
    private static void StagedAssetChecks(Transform root)
    {
        // Every palette material's packed map is the one its chosen set names (sRGB off,
        // Metallic Alpha at multiplier 1), or none: the ReferenceTextures and generated sets
        // and the untextured materials keep their scalar finish.
        int packed = 0, sourced = 0;
        bool maps = true;
        foreach (KeyValuePair<Material, string> entry in StoreKit.PaletteTextures)
        {
            Material material = entry.Key;
            if (material == null) continue;
            Texture map = material.GetTexture("_MetallicGlossMap");
            bool chosen = StoreKit.PaletteSet(material, out StoreKit.SurfaceSet set);
            if (chosen) sourced++;
            if (!chosen || set.Ms == null || !DemoAssetLibrary.HasTexture(set.Pack, set.Ms))
            {
                maps &= map == null && !material.IsKeywordEnabled("_METALLICGLOSSMAP");
                continue;
            }
            TextureImporter importer = map != null ? AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(map)) as TextureImporter : null;
            bool ok = map != null && AssetDatabase.GetAssetPath(map) == DemoAssetLibrary.Path(set.Pack, set.Ms) && material.IsKeywordEnabled("_METALLICGLOSSMAP")
                && material.GetFloat("_SmoothnessTextureChannel") == 0 && Mathf.Approximately(material.GetFloat("_GlossMapScale"), 1)
                && importer != null && !importer.sRGBTexture;
            if (ok) packed++;
            maps &= ok;
        }
        DemoValidation.Info("Night palette materials with a texture set: " + sourced + "; with the set's packed metallic-smoothness map: " + packed);
        DemoValidation.Check(maps, "Night palette maps all come from the selected set");

        // The ceiling slab shows the colour map of the set KitCeiling took (PBRInterior's
        // plasterboard when present, else the NightMaterials plaster); the facade cladding
        // shows the NightMaterials dark marble.
        bool ShowsSet(Transform t, StoreKit.SurfaceSet s)
        {
            MeshRenderer renderer = t != null ? t.GetComponent<MeshRenderer>() : null;
            if (renderer == null) return false;
            foreach (Material shown in renderer.sharedMaterials)
                if (shown != null && shown.mainTexture != null && AssetDatabase.GetAssetPath(shown.mainTexture) == DemoAssetLibrary.Path(s.Pack, s.Color)) return true;
            return false;
        }
        bool ceiling = StoreKit.PaletteSet(StoreKit.CeilingPaint, out StoreKit.SurfaceSet ceilingSet);
        bool facade = StoreKit.PaletteSet(StoreKit.Cladding, out StoreKit.SurfaceSet facadeSet);
        DemoValidation.Info("Ceiling set=" + (StoreKit.PaletteSource(StoreKit.CeilingPaint) ?? "none") + "; facade set=" + (StoreKit.PaletteSource(StoreKit.Cladding) ?? "none"));
        if (ceiling || facade)
        {
            bool ok = true;
            if (ceiling) ok &= ShowsSet(root.Find(L.CeilingPath + "/CeilingSlab"), ceilingSet)
                && (ceilingSet.Pack == StoreKit.PbrInteriorPack || !DemoAssetLibrary.HasTexture(StoreKit.PbrInteriorPack, "CeilingPaint_Color.jpg"));
            if (facade) ok &= ShowsSet(root.Find("Facade/Cladding"), facadeSet)
                && facadeSet.Pack == StoreKit.NightMaterialPack && facadeSet.Color == "FacadeStone_Color.png";
            DemoValidation.Check(ok, "Ceiling and facade use the selected sets");
        }
        else DemoValidation.Info("No ceiling or facade texture set present: skipped");

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
        // Case decision pending (Docs/DECISIONS.md): no decal from the pack may be placed yet.
        bool decals = false;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            foreach (Material m in r.sharedMaterials)
                if (m != null && m.mainTexture != null && (m.mainTexture.name.StartsWith("ShoeTrace") || m.mainTexture.name.StartsWith("Scuff") || m.mainTexture.name.StartsWith("DustSmear")))
                    decals = true;
        DemoValidation.Check(!decals, "Staged floor decals stay unplaced");
    }

    // Edit mode, before the build: DemoAssetLibrary's pack path (shared pack materials,
    // roles and finishes, map wiring, LOD levels, atlases, the blend and quad helpers,
    // credits). A check that needs a pack is skipped (Info) without it. The test objects
    // and every credit recorded here are cleared again, so the build credits only what it places.
    private static void AssetLibraryChecks()
    {
        const string street = "StreetModels", interior = "InteriorModels", people = "PeopleModels", jewelry = "JewelryModels";
        Transform test = new GameObject("AssetLibraryTest").transform;
        test.position = new Vector3(0, -50, 0);
        try
        {
            if (DemoAssetLibrary.Has(street, "Bollard") && DemoAssetLibrary.Has(street, "StreetLamp"))
            {
                DemoAssetLibrary.PackOptions prop = new DemoAssetLibrary.PackOptions { Lod = DemoAssetLibrary.LodClass.Prop };
                GameObject a = DemoAssetLibrary.PlacePack(street, "Bollard", test, Vector3.zero, 0, prop);
                GameObject b = DemoAssetLibrary.PlacePack(street, "Bollard", test, new Vector3(1.4f, 0, 0), 90, prop);
                GameObject lamp = DemoAssetLibrary.PlacePack(street, "StreetLamp", test, new Vector3(4, 0, 0), 0,
                    new DemoAssetLibrary.PackOptions { Lod = DemoAssetLibrary.LodClass.Post });
                List<Material> first = MaterialsUnder(a.transform), second = MaterialsUnder(b.transform);
                bool shared = first.Count > 0 && first.Count == second.Count;
                for (int i = 0; shared && i < first.Count; i++)
                    shared = first[i] != null && first[i] == second[i]
                        && AssetDatabase.GetAssetPath(first[i]).StartsWith(DemoAssetLibrary.PackMaterialFolder + "/", StringComparison.Ordinal);
                bool lods = DemoAssetLibrary.Has(street, "StreetLamp_LOD1") && DemoAssetLibrary.Has(street, "StreetLamp_LOD2");
                Transform lod0 = lamp.transform.Find("LOD0"), lod1 = lamp.transform.Find("LOD1");
                if (lods) shared &= lod0 != null && lod1 != null && new HashSet<Material>(MaterialsUnder(lod0)).IsSupersetOf(MaterialsUnder(lod1));
                DemoValidation.Check(shared, "Pack placement shares one material per pack, colour map, tint and role across instances and LOD levels");
                if (lods)
                {
                    LODGroup group = lamp.GetComponent<LODGroup>();
                    float[] post = DemoAssetLibrary.Transitions(DemoAssetLibrary.LodClass.Post, 3);
                    bool grouped = group != null && group.lodCount == 3 && group.fadeMode == LODFadeMode.None && a.GetComponent<LODGroup>() == null;
                    if (grouped)
                    {
                        LOD[] levels = group.GetLODs();
                        for (int i = 0; i < 3; i++)
                            grouped &= Mathf.Abs(levels[i].screenRelativeTransitionHeight - post[i]) < 1e-4f && levels[i].renderers.Length > 0;
                    }
                    DemoValidation.Check(grouped, "LOD groups use the prep variants with the class transitions");
                }
                else DemoValidation.Info("StreetLamp_LOD1/_LOD2 not present: LOD group check skipped");
            }
            else DemoValidation.Info("StreetModels not present: pack placement and LOD checks skipped");

            Material yellow = PackMaterialOf(street, "TrafficLight", "PaintYellow"), black = PackMaterialOf(street, "TrafficLight", "PaintBlack");
            Material housing = PackMaterialOf(interior, "ComputerMonitor", "Housing"), pot = PackMaterialOf(interior, "SnakePlant", "Pot");
            Material carInterior = PackMaterialOf(street, "PoliceCar", "Interior"), rooms = PackMaterialOf(street, "ApartmentBlock", "Interior");
            Material props = PackMaterialOf(people, "PasserBy_Phone", "Props"), screen = PackMaterialOf(people, "PasserBy_Phone", "PropScreen");
            if (yellow != null && black != null && housing != null && pot != null && carInterior != null && rooms != null && props != null && screen != null)
                DemoValidation.Check(yellow != black && housing != pot && props != screen && !carInterior.IsKeywordEnabled("_EMISSION")
                    && rooms.IsKeywordEnabled("_EMISSION") && (rooms.globalIlluminationFlags & MaterialGlobalIlluminationFlags.BakedEmissive) != 0
                    && !props.IsKeywordEnabled("_EMISSION") && screen.IsKeywordEnabled("_EMISSION"),
                    "Shared colour maps with different tints or roles stay separate");
            else DemoValidation.Info("StreetModels, InteriorModels or PeopleModels not present: shared-map separation check skipped");

            Material frames = PackMaterialOf(street, "ApartmentBlock", "WindowFrames"), facadeGlass = PackMaterialOf(street, "ApartmentBlock", "Glass");
            Material body = PackMaterialOf(street, "PoliceCar", "Body");
            if (frames != null && facadeGlass != null && body != null)
                DemoValidation.Check(frames.renderQueue < 2500 && facadeGlass.renderQueue == 3000 && facadeGlass.color.a < 0.99f
                    && body.GetFloat("_Glossiness") >= 0.8f && !body.IsKeywordEnabled("_METALLICGLOSSMAP"),
                    "Transparency comes from the MTL alpha, finishes from the pack data");
            else DemoValidation.Info("StreetModels not present: transparency and finish check skipped");
            // GemParcel's Diamond has no finish entry: only the imported MTL d can make it glass.
            Material diamond = ImportedMaterial(jewelry, "GemParcel", "Diamond");
            if (diamond != null)
            {
                DemoValidation.Info("Imported MTL alpha of GemParcel Diamond (d 0.35): " + diamond.color.a.ToString("0.00"));
                DemoValidation.Check(PackMaterialOf(jewelry, "GemParcel", "Diamond").renderQueue == 3000,
                    "The OBJ import carries the MTL d as colour alpha (a glass material without a finish entry)");
            }
            else DemoValidation.Info("JewelryModels not present: MTL alpha check skipped");

            Material extinguisher = PackMaterialOf(interior, "FireExtinguisher", "Body");
            if (extinguisher != null)
            {
                Texture bump = extinguisher.GetTexture("_BumpMap"), gloss = extinguisher.GetTexture("_MetallicGlossMap");
                TextureImporter bumpImporter = bump != null ? AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(bump)) as TextureImporter : null;
                TextureImporter glossImporter = gloss != null ? AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(gloss)) as TextureImporter : null;
                DemoValidation.Check(bump != null && bump.name == "FireExtinguisher_Body_Normal" && gloss != null && gloss.name == "FireExtinguisher_Body_MetallicSmoothness"
                    && extinguisher.IsKeywordEnabled("_NORMALMAP") && extinguisher.IsKeywordEnabled("_METALLICGLOSSMAP")
                    && bumpImporter != null && bumpImporter.textureType == TextureImporterType.NormalMap && glossImporter != null && !glossImporter.sRGBTexture,
                    "Pack material convention wires Normal and MetallicSmoothness by the colour map stem");
            }
            else DemoValidation.Info("InteriorModels not present: map convention check skipped");
            Material grille = PackMaterialOf(street, "AirconUnit", "AirconGrille");
            if (grille != null)
                DemoValidation.Check(grille.mainTexture != null && grille.IsKeywordEnabled("_ALPHATEST_ON") && grille.renderQueue == 2450 && grille.GetFloat("_Cutoff") > 0
                    && Mathf.Approximately(grille.GetFloat("_Cutoff"), PackImport.CutoffOf(AssetDatabase.GetAssetPath(grille.mainTexture))),
                    "Cut-out colour maps alpha-test at the import rules' cutoff");
            else DemoValidation.Info("StreetModels AirconUnit not present: cut-out check skipped");

            bool threw = false;
            GameObject missing = null;
            try
            {
                missing = DemoAssetLibrary.PlacePack(street, "NoSuchModel", test, Vector3.zero, 0,
                    new DemoAssetLibrary.PackOptions { Lod = DemoAssetLibrary.LodClass.Prop });
            }
            catch (Exception) { threw = true; }
            DemoValidation.Check(!threw && missing == null && test.Find("NoSuchModel") == null, "Missing pack models return null");

            foreach (string[] atlas in new[] { new[] { "SurfaceDecals", "Atlas/Ambient.json" }, new[] { "WallGraphics", "Atlas/NightStoreInterior.json" } })
            {
                if (AssetDatabase.LoadAssetAtPath<TextAsset>(DemoAssetLibrary.Path(atlas[0], atlas[1])) == null)
                {
                    DemoValidation.Info(atlas[0] + "/" + atlas[1] + " not present: atlas check skipped");
                    continue;
                }
                Dictionary<string, Rect> cells = DemoAssetLibrary.Atlas(atlas[0], atlas[1]);
                bool inside = cells != null && cells.Count > 0;
                if (inside)
                    foreach (Rect r in cells.Values)
                        inside &= r.xMin >= 0 && r.yMin >= 0 && r.xMax <= 1 && r.yMax <= 1 && r.width > 0 && r.height > 0;
                // A cell from the WallGraphics README table: y counts from the page's bottom edge.
                if (inside && atlas[0] == "WallGraphics")
                {
                    inside = cells.TryGetValue("Lightbox_AurelFinchWide", out Rect box) && Mathf.Abs(box.xMin - 8 / 2048f) < 1e-5f
                        && Mathf.Abs(box.yMin - 788 / 2048f) < 1e-5f && Mathf.Abs(box.width - 0.5f) < 1e-5f && Mathf.Abs(box.height - 0.25f) < 1e-5f;
                }
                DemoValidation.Check(inside, "Atlas cells load as UV rects inside [0,1] (" + atlas[0] + "/" + atlas[1] + ")");
            }

            Material fade = DemoGeometry.Fade(new Material(Shader.Find("Standard")), 3010);
            Material cut = DemoGeometry.Cutout(new Material(Shader.Find("Standard")), 0.4f);
            bool blend = fade.IsKeywordEnabled("_ALPHABLEND_ON") && !fade.IsKeywordEnabled("_ALPHATEST_ON") && !fade.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON")
                && fade.renderQueue == 3010 && fade.GetTag("RenderType", false) == "Transparent" && fade.GetFloat("_ZWrite") == 0 && fade.GetFloat("_Mode") == 2
                && cut.IsKeywordEnabled("_ALPHATEST_ON") && !cut.IsKeywordEnabled("_ALPHABLEND_ON") && !cut.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON")
                && cut.renderQueue == 2450 && cut.GetTag("RenderType", false) == "TransparentCutout" && cut.GetFloat("_ZWrite") == 1
                && cut.GetFloat("_Mode") == 1 && Mathf.Approximately(cut.GetFloat("_Cutoff"), 0.4f);
            UnityEngine.Object.DestroyImmediate(fade);
            UnityEngine.Object.DestroyImmediate(cut);
            DemoValidation.Check(blend, "Fade and Cutout helpers set the Standard blend state");

            ProceduralMesh quad = new ProceduralMesh();
            quad.Quad(null, Vector3.back, Vector3.zero, Vector3.right, new Vector3(1, 1, 0), Vector3.up, new Rect(0.25f, 0.5f, 0.25f, 0.125f));
            quad.Quad(null, Vector3.back, Vector3.zero, Vector3.right, Vector3.right * 2, Vector3.right * 3, new Rect(0, 0, 1, 1));
            Mesh mesh = quad.ToMesh("QuadCheck");
            int[] triangles = mesh.triangles;
            Vector3[] corners = mesh.vertices;
            Vector2[] uv = mesh.uv;
            bool quadOk = mesh.vertexCount == 4 && triangles.Length == 6 && mesh.normals[0].z < -0.99f
                && Vector3.Dot(Vector3.Cross(corners[triangles[1]] - corners[triangles[0]], corners[triangles[2]] - corners[triangles[0]]), Vector3.back) > 0
                && (uv[0] - new Vector2(0.25f, 0.5f)).sqrMagnitude < 1e-10f && (uv[2] - new Vector2(0.5f, 0.625f)).sqrMagnitude < 1e-10f;
            UnityEngine.Object.DestroyImmediate(mesh);
            DemoValidation.Check(quadOk, "ProceduralMesh.Quad maps the UV rect to its corners, faces outward and skips degenerate quads");

            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.SetParent(test, false);
            block.transform.position = new Vector3(0, 7, 0);
            DemoAssetLibrary.Seat(block, 0.5f);
            DemoValidation.Check(Mathf.Abs(block.GetComponent<Renderer>().bounds.min.y - 0.5f) < 1e-4f, "Seat rests a prop's lowest renderer point on the surface");

            DemoAssetLibrary.ResetCredits();
            bool credited = DemoAssetLibrary.UsedCredits().Count == 0 && !DemoAssetLibrary.NeedsCredit(street, "StreetLamp")
                && DemoAssetLibrary.CreditFor(people, "PasserBy_Phone_LOD1") != null
                && DemoAssetLibrary.CreditFor(street, "PoliceCar_LOD2") == DemoAssetLibrary.CreditFor(street, "PoliceCar");
            if (DemoAssetLibrary.Has("KhronosSamples", "TrafficCone"))
            {
                GameObject cone = DemoAssetLibrary.Place("KhronosSamples", "TrafficCone", test, Vector3.zero, 0, 1);
                string line = DemoAssetLibrary.CreditFor("KhronosSamples", "TrafficCone");
                DemoValidation.Check(credited && cone != null && line != null && DemoAssetLibrary.UsedCredits().Contains(line),
                    "Placing an attributable model records its credit");
            }
            else DemoValidation.Info("KhronosSamples TrafficCone not present: credit check skipped");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(test.gameObject);
            DemoAssetLibrary.ResetCredits();
        }
    }

    // An OBJ's imported (read-only) material by its MTL name; null when the model is absent.
    private static Material ImportedMaterial(string pack, string model, string material)
    {
        GameObject asset = DemoAssetLibrary.Load(pack, model);
        if (asset == null) return null;
        foreach (Renderer r in asset.GetComponentsInChildren<Renderer>(true))
            foreach (Material m in r.sharedMaterials)
                if (m != null && m.name == material) return m;
        return null;
    }

    // The pack material DemoAssetLibrary.PlacePack gives that imported material, without placing the model.
    private static Material PackMaterialOf(string pack, string model, string material)
        => DemoAssetLibrary.PackMaterial(pack, model, ImportedMaterial(pack, model, material));

    private static List<Material> MaterialsUnder(Transform t)
    {
        List<Material> list = new List<Material>();
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true)) list.AddRange(r.sharedMaterials);
        return list;
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
        // NightAudioBuilder also stages NightSoundscape's takes (shutter, torch click, city bed,
        // set-downs, footsteps), so a project with that pack has a bank without NightAudio too.
        foreach (string file in new[] { "CameraShutterDSLR_01.wav", "FlashlightClick_01.wav", "NightCity_Loop.ogg", "ConePlace_01.wav", "EvidenceMarkerPlace_01.wav", "FootstepHard_01.wav", "FootstepGlass_01.wav" })
            any |= AssetDatabase.LoadAssetAtPath<AudioClip>(DemoAssetLibrary.Path(NightAudioBuilder.SoundPack, file)) != null;
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
    private static void LightmapChecks(bool bake, Transform root)
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

        // Combined-directional lightmaps (and, once baked, a direction map beside every colour map).
        LightingSettings settings = Lightmapping.lightingSettings;
        bool directional = settings.directionalityMode == LightmapsMode.CombinedDirectional;
        if (bake)
        {
            directional &= LightmapSettings.lightmapsMode == LightmapsMode.CombinedDirectional;
            foreach (LightmapData map in maps) directional &= map.lightmapDir != null;
        }
        DemoValidation.Check(directional, "Night lightmaps are directional (normal maps respond to baked light)");

        // NightLighting.LightmapScales at DemoGeometry's density: shell, near pavement, neighbour terrace.
        bool zones = Mathf.Abs(settings.lightmapResolution - DemoGeometry.LightmapTexelsPerMetre) < 0.01f
            && Mathf.Abs(ScaleAt(root, "Architecture/Floor") - NightLighting.ShellScale) < 0.001f
            && Mathf.Abs(ScaleAt(root, "Exterior/Ground/Pavement") - NightLighting.GroundScale) < 0.001f
            && Mathf.Abs(ScaleAt(root, "Exterior/Buildings/Neighbours") - NightLighting.StreetScale) < 0.001f;
        DemoValidation.Check(zones, "Lightmap density by zone");

        // Every baked renderer fits one lightmap at its density (Unity would shrink it), and the
        // imported KayKit car and streetlights beside the spawn get at least the 40 texels per
        // model unit their importer's default Calculate chart margin assumes.
        Transform car = root.Find("Exterior/PoliceCar"), posts = root.Find("Exterior/Streetlights");
        int oversized = 0, kit = 0, thin = 0;
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r.receiveGI != ReceiveGI.Lightmaps || (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) == 0) continue;
            Vector3 size = r.bounds.size;
            float density = settings.lightmapResolution * r.scaleInLightmap;
            if (Mathf.Max(size.x, Mathf.Max(size.y, size.z)) * density > settings.lightmapMaxSize) oversized++;
            bool beside = (car != null && r.transform.IsChildOf(car)) || (posts != null && r.transform.IsChildOf(posts));
            MeshFilter filter = r.GetComponent<MeshFilter>();
            string asset = filter != null && filter.sharedMesh != null ? AssetDatabase.GetAssetPath(filter.sharedMesh) : "";
            if (!beside || !asset.StartsWith(DemoAssetLibrary.Root + "/")) continue;
            kit++;
            if (density * Mathf.Abs(r.transform.lossyScale.x) < 39.99f) thin++;
        }
        DemoValidation.Info("Lightmap density " + settings.lightmapResolution + " texels/m; imported kit renderers " + kit);
        DemoValidation.Check(oversized == 0, "No lightmapped renderer outgrows one lightmap");
        DemoValidation.Check(thin == 0, "Kit models beside the spawn bake at their import margin density");

        // Streetlight and headlamp lenses are probe-static and out of GI; the blinking light bar stays dynamic.
        bool lensesStatic = true;
        int lensCount = 0;
        foreach (string zone in new[] { "Exterior/Streetlights", "Exterior/PoliceCar" })
        {
            Transform t = root.Find(zone);
            if (t == null) { lensesStatic = false; continue; }
            foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.name != "Lens") continue;
                lensCount++;
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                lensesStatic &= (flags & StaticEditorFlags.ReflectionProbeStatic) != 0 && (flags & StaticEditorFlags.ContributeGI) == 0;
            }
        }
        Transform bar = root.Find("Exterior/PoliceCar/LightBar");
        if (bar == null) lensesStatic = false;
        else foreach (MeshRenderer r in bar.GetComponentsInChildren<MeshRenderer>(true)) lensesStatic &= GameObjectUtility.GetStaticEditorFlags(r.gameObject) == 0;
        DemoValidation.Check(lensesStatic && lensCount == L.Streetlights.Length + 2, "Light lenses are captured by the baked reflection probes");

        // The row across the street takes lightmaps (uv2) at the far density, so the far streetlights' pools reach it.
        Transform acrossStreet = root.Find("Exterior/Buildings/AcrossStreet");
        MeshRenderer acrossRenderer = acrossStreet != null ? acrossStreet.GetComponent<MeshRenderer>() : null;
        MeshFilter acrossFilter = acrossRenderer != null ? acrossRenderer.GetComponent<MeshFilter>() : null;
        Mesh acrossMesh = acrossFilter != null ? acrossFilter.sharedMesh : null;
        DemoValidation.Check(acrossRenderer != null && acrossRenderer.receiveGI == ReceiveGI.Lightmaps && acrossMesh != null && acrossMesh.uv2.Length == acrossMesh.vertexCount
            && Mathf.Abs(acrossRenderer.scaleInLightmap - NightLighting.FarScale) < 0.001f, "Buildings across the street are lightmapped at the far density");

        DemoValidation.Check(NightLighting.ReflectionResolution >= 256 && RenderSettings.defaultReflectionResolution == NightLighting.SkyReflectionResolution,
            "Glass reflection probes bake at 256 px, the night sky at 128 px");
    }

    // scaleInLightmap of the renderer at root/path, or -1 when there is none.
    private static float ScaleAt(Transform root, string path)
    {
        Transform t = root.Find(path);
        MeshRenderer r = t != null ? t.GetComponent<MeshRenderer>() : null;
        return r != null ? r.scaleInLightmap : -1;
    }

    // Where the budgets are seen from: the spawn, every walk-route point and the office
    // opening, at standing eye height, with the desktop view's field of view and the LOD bias
    // NightQuality.Apply sets at runtime (VR's ~95 degrees selects the same or lower levels).
    public const float BudgetEyeHeight = 1.6f, BudgetFieldOfView = 60, BudgetLodBias = 1;

    private static List<Vector3> BudgetEyes(Transform root, out List<string> names)
    {
        List<Vector3> eyes = new List<Vector3> { L.PlayerSpawn };
        names = new List<string> { "spawn" };
        for (int i = 0; i < L.Route.Length; i++) { eyes.Add(L.Route[i]); names.Add("route " + i); }
        eyes.Add(L.Anchor("OfficeOpening"));
        names.Add("office opening");
        for (int i = 0; i < eyes.Count; i++) eyes[i] = root.TransformPoint(eyes[i]) + Vector3.up * BudgetEyeHeight;
        return eyes;
    }

    // The level a LODGroup draws for a perspective eye, by the LODGroup inspector's formula:
    // half the group's world size over (distance x tan of half the field of view), times the
    // LOD bias, against each level's transition height; lods.Length when it is culled.
    public static int SelectedLod(LODGroup group, Vector3 eye, float fieldOfView, float lodBias)
    {
        LOD[] lods = group.GetLODs();
        Vector3 scale = group.transform.lossyScale;
        float size = group.size * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        float distance = Mathf.Max(Vector3.Distance(group.transform.TransformPoint(group.localReferencePoint), eye), 1e-4f);
        float height = size * 0.5f / (distance * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad)) * lodBias;
        for (int i = 0; i < lods.Length; i++)
            if (height >= lods[i].screenRelativeTransitionHeight) return i;
        return lods.Length;
    }

    // The scene as the arrival view can draw it, in edit mode (static batching merges the
    // meshes in Play mode): active, enabled renderers only, whatever their owner. Triangles,
    // renderers and transparent renderers count only the level each LODGroup selects from the
    // worst eye (BudgetEyes; each budget at its own worst eye); every level together and the
    // hidden presenter sets are logged as Info. Materials and textures count every level.
    private static void Budgets(Transform root)
    {
        string[] categories = { "store", "exterior", "dressing", "tools and player" };
        int facadePanes = 0, largestTexture = 0, allRenderers = 0, allTransparent = 0, allTriangles = 0, allSubmeshes = 0;
        HashSet<Material> materials = new HashSet<Material>();
        HashSet<Texture> textures = new HashSet<Texture>();
        Transform facade = root.Find("Facade");
        // The levels of every active LODGroup that hold each renderer (a bit per level).
        Dictionary<Renderer, KeyValuePair<LODGroup, int>> levels = new Dictionary<Renderer, KeyValuePair<LODGroup, int>>();
        LODGroup[] groups = UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
        foreach (LODGroup group in groups)
        {
            LOD[] lods = group.GetLODs();
            for (int i = 0; i < lods.Length; i++)
                foreach (Renderer r in lods[i].renderers)
                    if (r != null) levels[r] = new KeyValuePair<LODGroup, int>(group, (levels.TryGetValue(r, out KeyValuePair<LODGroup, int> held) ? held.Value : 0) | 1 << i);
        }
        List<Renderer> drawn = new List<Renderer>();
        List<int> counts = new List<int>(), kinds = new List<int>();
        List<bool> blends = new List<bool>();
        foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
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
            if (clear && mesh != null && r.transform.IsChildOf(facade) && DemoGeometry.SurfaceArea(mesh) * 0.5f >= 1) facadePanes++;
            int count = 0;
            if (mesh != null)
                for (int i = 0; i < mesh.subMeshCount; i++) count += (int)mesh.GetIndexCount(i) / 3;
            if (mesh != null) allSubmeshes += mesh.subMeshCount;
            drawn.Add(r);
            counts.Add(count);
            kinds.Add(Category(r.transform, root));
            blends.Add(clear);
            allRenderers++;
            allTriangles += count;
            if (clear) allTransparent++;
        }
        foreach (Material m in materials)
            foreach (string property in m.GetTexturePropertyNames())
            {
                Texture texture = m.GetTexture(property);
                if (texture != null && textures.Add(texture)) largestTexture = Mathf.Max(largestTexture, Mathf.Max(texture.width, texture.height));
            }

        List<Vector3> eyes = BudgetEyes(root, out List<string> eyeNames);
        int[] triangles = new int[categories.Length];
        int total = -1, renderers = 0, transparent = 0;
        string worst = "", worstRenderers = "", worstTransparent = "";
        bool helper = true;
        foreach (LODGroup group in groups)
        {
            Vector3 at = group.transform.TransformPoint(group.localReferencePoint);
            helper &= SelectedLod(group, at, BudgetFieldOfView, BudgetLodBias) == 0
                && SelectedLod(group, at + Vector3.up * 10000, BudgetFieldOfView, BudgetLodBias) == group.lodCount;
        }
        for (int e = 0; e < eyes.Count; e++)
        {
            Dictionary<LODGroup, int> selected = new Dictionary<LODGroup, int>();
            foreach (LODGroup group in groups) selected[group] = SelectedLod(group, eyes[e], BudgetFieldOfView, BudgetLodBias);
            int[] split = new int[categories.Length];
            int sum = 0, shown = 0, clear = 0;
            for (int i = 0; i < drawn.Count; i++)
            {
                if (levels.TryGetValue(drawn[i], out KeyValuePair<LODGroup, int> held) && (held.Value & 1 << selected[held.Key]) == 0) continue;
                split[kinds[i]] += counts[i];
                sum += counts[i];
                shown++;
                if (blends[i]) clear++;
            }
            if (sum > total) { total = sum; triangles = split; worst = eyeNames[e]; }
            if (shown > renderers) { renderers = shown; worstRenderers = eyeNames[e]; }
            if (clear > transparent) { transparent = clear; worstTransparent = eyeNames[e]; }
        }
        total = Mathf.Max(total, 0);
        string splitText = "";
        for (int i = 0; i < triangles.Length; i++) splitText += (i > 0 ? ", " : "") + categories[i] + " " + triangles[i];

        // Hidden presenter sets (their content inactive at arrival), every level, for when they are shown.
        int hiddenTriangles = 0, hiddenRenderers = 0;
        foreach (PresenterSet set in UnityEngine.Object.FindObjectsByType<PresenterSet>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (set.Content == null || set.Content.activeInHierarchy) continue;
            foreach (MeshFilter filter in set.Content.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                hiddenRenderers++;
                for (int i = 0; i < filter.sharedMesh.subMeshCount; i++) hiddenTriangles += (int)filter.sharedMesh.GetIndexCount(i) / 3;
            }
        }

        SessionState.SetInt(TrianglesKey, total);
        SessionState.SetInt(RenderersKey, renderers);
        DemoValidation.Info("Budget: LOD groups=" + groups.Length + "; all levels drawn at once: triangles=" + allTriangles + ", renderers=" + allRenderers
            + ", transparent=" + allTransparent + "; hidden presenter sets (all levels): triangles=" + hiddenTriangles + ", renderers=" + hiddenRenderers);
        DemoValidation.Check(helper && total <= allTriangles && renderers <= allRenderers && transparent <= allTransparent,
            "Budgets count the LOD level selected from the worst eye");
        DemoValidation.Info("Budget: triangles=" + total + " (" + splitText + ") of " + TriangleBudget + ", worst eye " + worst);
        DemoValidation.Check(total <= TriangleBudget, "Night store triangle budget");
        DemoValidation.Info("Budget: renderers before static batching=" + renderers + " of " + RendererBudget + ", worst eye " + worstRenderers);
        DemoValidation.Check(renderers <= RendererBudget, "Night store renderer budget");
        // Reported only (no check): one draw per submesh until static batching merges them.
        DemoValidation.Info("Budget: submeshes (draw calls before static batching, all active renderers)=" + allSubmeshes);
        DemoValidation.Info("Budget: materials=" + materials.Count + " of " + MaterialBudget + "; textures=" + textures.Count + ", largest " + largestTexture + " px of " + TextureBudget);
        DemoValidation.Check(materials.Count <= MaterialBudget && largestTexture <= TextureBudget, "Night store material budget");
        DemoValidation.Info("Budget: transparent renderers=" + transparent + " of " + TransparentBudget + ", worst eye " + worstTransparent
            + "; facade panes of 1 m2 or more=" + facadePanes + " of " + FacadePaneBudget);
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
                // Arrival as PresenterSet.Start left it, before any set is switched.
                ArrivalPrintChecks();
                PresenterArrivalChecks();
                ZoneChecks();
                EntranceChecks();
                SpawnChecks();
                WalkChecks();
                TeleportChecks();
                PlacementChecks();
                StreetPlayChecks();
                ToolStylingChecks();
                CarriedLampChecks();
                GlassChecks();
                MapChecks();
                InspectionChecks();
                RealtimeLightChecks();
                LightingPlayChecks();
                StagedSoundChecks();
                SoundPlayChecks();
                DemoValidation.InterfaceChecks();
                DemoValidation.ToolStage();
                SessionState.SetInt(Key, 2);
            }
            else if (stage == 2 && DemoValidation.PhotoStage()) SessionState.SetInt(Key, 3);
            else if (stage == 3 && DemoValidation.ResetStage())
            {
                ResetChecks();
                PresenterResetChecks();
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
        // Every camera renders Forward, so player builds drop the DEFERRED pass and keep every other one.
        DemoValidation.Check(ForwardOnlyShaderStripping.ForwardOnly()
            && ForwardOnlyShaderStripping.Strips(UnityEngine.Rendering.PassType.Deferred, true)
            && !ForwardOnlyShaderStripping.Strips(UnityEngine.Rendering.PassType.ForwardBase, true)
            && !ForwardOnlyShaderStripping.Strips(UnityEngine.Rendering.PassType.ForwardAdd, true)
            && !ForwardOnlyShaderStripping.Strips(UnityEngine.Rendering.PassType.ShadowCaster, true)
            && !ForwardOnlyShaderStripping.Strips(UnityEngine.Rendering.PassType.Meta, true)
            && !ForwardOnlyShaderStripping.Strips(UnityEngine.Rendering.PassType.Deferred, false),
            "Forward-only player builds strip the deferred shader pass");

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
        // Torches and lamps are ForcePixel; the preview's headlights are Auto, so they never take a torch's pixel slot.
        bool pixel = lighting.FlashlightLight.renderMode == LightRenderMode.ForcePixel && lighting.UVTorchLight.renderMode == LightRenderMode.ForcePixel
            && first.Lamp.renderMode == LightRenderMode.ForcePixel && second.Lamp.renderMode == LightRenderMode.ForcePixel;
        bool headlights = true;
        if (!baked) foreach (Light l in group.GetComponentsInChildren<Light>(true)) if (l.name.StartsWith("Headlight_")) headlights &= l.renderMode == LightRenderMode.Auto;
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
        DemoValidation.Check(pixel && headlights, "Handheld lights keep a per-pixel slot");
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
        // Apply(true) enabled the fade (VROnly); the start-up turn and jump blink like a teleport.
        bool blinked = locomotion.ComfortFade != null && locomotion.ComfortFade.Fading && locomotion.ComfortFade.Alpha >= 0.99f;
        Vector3 forward = head.forward;
        forward.y = 0;
        bool vr = pending && !mode.RecentrePending && Flat(head.position - spawn) < 0.01f && Mathf.Abs(locomotion.transform.position.y - spawn.y) < 0.001f
            && Vector3.Angle(forward, Quaternion.Euler(0, L.PlayerYaw, 0) * Vector3.forward) < 0.5f && Vector3.Dot(forward.normalized, toShop.normalized) > 0.9f;
        mode.Apply(false);
        walker.ResetPosition();
        DemoValidation.Info("Spawn " + spawn + "; car body " + gap.ToString("0.00") + " m away");
        DemoValidation.Check(desktop && beside && vr && !mode.VRActive, "Visitor starts beside the police car facing the shop");
        DemoValidation.Check(blinked, "VR recentre blinks through the comfort fade");
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
        // The tape is a surface shader: a ForwardAdd pass for the torches and lamps, and its own shadow caster.
        bool litTape = false, castsTape = false;
        if (ribbon != null && ribbon.sharedMaterial != null)
        {
            Shader shader = ribbon.sharedMaterial.shader;
            var mode = new UnityEngine.Rendering.ShaderTagId("LightMode");
            for (int i = 0; i < shader.passCount; i++)
            {
                string m = shader.FindPassTagValue(i, mode).name;
                litTape |= m == "ForwardAdd"; castsTape |= m == "ShadowCaster";
            }
        }
        DemoValidation.Check(litTape && castsTape, "Police tape is lit by the torches and lamps and casts shadows");
        // Fallback if Shader.passCount / FindPassTagValue fail to compile on 6000.6: replace the loop with
        // litTape = ribbon.sharedMaterial.FindPass("FORWARD_DELTA") >= 0; castsTape = ribbon.sharedMaterial.FindPass("ShadowCaster") >= 0;
        bool own = true;
        foreach (UnityEngine.Object prefab in new UnityEngine.Object[] { station.ConePrefab, station.MarkerPrefab, station.TapePostPrefab,
            station.ScalePrefab, station.MeasurePrefab, station.LampPrefab, station.TapePrefab, station.MeasureTapePrefab })
            own &= prefab != null && AssetDatabase.GetAssetPath(prefab).StartsWith(NightStoreBuilder.Folder + "/");
        DemoValidation.Check(own, "Night tool prefabs live in the night store's own folder");
    }

    // A lamp carried low over the staging bay rests its feet on the street, not at the
    // showroom floor height (HandInteractor clamps to Bounds.FloorAt). A tool released
    // overhead there drops to the street too (the bay has no ceiling; ToolHolder.Settle
    // casts from DropCeilingClearance above the region's floor at most). PlacementChecks
    // removed its cone from the same spot, so nothing else stands there.
    private static void CarriedLampChecks()
    {
        DesktopInteractor desktop = DemoValidation.Find<DesktopInteractor>();
        HandInteractor hand = desktop.transform.Find("RightHand").GetComponent<HandInteractor>();
        Vector3 position = hand.transform.position;
        hand.transform.position = new Vector3(6.5f, 0.3f, -4.0f);
        DeployedTool lamp = hand.SpawnIntoHand(DemoToolKind.Lamp);
        float lampBase = lamp != null ? lamp.transform.position.y : 99;
        if (lamp != null) hand.RemoveHeld();
        hand.transform.position = new Vector3(6.5f, 2.3f, -4.0f);
        DeployedTool high = hand.SpawnIntoHand(DemoToolKind.Cone);
        hand.Release();
        float highBase = high != null ? high.transform.position.y : 99;
        // Remove (not Station.RemoveTool) also clears the hand's highlight of the cone.
        if (high != null) hand.Remove(high);
        hand.transform.position = position;
        DemoValidation.Check(lamp != null && Mathf.Abs(lampBase - L.StreetY) < 0.001f, "Carried lamp rests on the street in the staging bay");
        DemoValidation.Check(high != null && Mathf.Abs(highBase - L.StreetY) < 0.02f,
            "Tool released overhead in the staging bay drops to the street");
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

        // The hidden camera renders only while the full drawer shows the Map tab; layout and tab are put back.
        DemoDesktopPanel panel = DemoValidation.Find<DemoDesktopPanel>();
        PanelLayout layout = panel.Layout; PanelTab tab = panel.Tab;
        panel.Layout = PanelLayout.Full; panel.Tab = PanelTab.Tools; bool other = !map.ShouldRender;
        panel.Tab = PanelTab.Map; bool shown = map.ShouldRender;
        panel.Layout = PanelLayout.Strip; bool strip = !map.ShouldRender;
        panel.Layout = layout; panel.Tab = tab;
        DemoValidation.Check(other && shown && strip, "Overview renders only on the drawer's Map tab");
        DemoValidation.Check(map.MapShader != null && !ShaderUtil.ShaderHasError(map.MapShader), "Overview map draws albedo through its replacement shader");

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
    // the showroom and a beacon on the floor; the presenter showed every set, held the
    // light bar steady and turned the underscore on (PresenterDisturb).
    private static void DisturbBeforeReset()
    {
        DemoLighting lighting = DemoValidation.Find<DemoLighting>();
        lighting.SetNight(false);
        lighting.SetUVExamples(true);
        lighting.Flashlight(true);
        DeployedTool lamp = DemoValidation.Find<ToolStation>().Spawn(DemoToolKind.Lamp);
        if (lamp != null) lamp.PlaceAt(new Vector3(-2, 0, 3), 30);
        DemoValidation.Find<DemoBeacon>().Place(new Vector3(1, 0, 3));
        PresenterDisturb();
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
