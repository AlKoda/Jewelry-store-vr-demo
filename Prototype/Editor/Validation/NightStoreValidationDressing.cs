#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using L = NightStoreLayout;

// The closing procedure (NightStoreDressing) and the decals (NightDecals) as this build placed
// them: in Edit mode after the lighting pass (final lightmap scales, no bake, presenter-set
// content inactive) and before the scene is first saved (ClosingProcedureChecks, then
// DecalChecks, from Generate), and in Play mode at arrival (ArrivalPrintChecks, from Tick
// stage 1, before anything is toggled). Supports and lifts are measured with rays on the
// triangles themselves (the dressing has no colliders and the hidden sets are inactive), atlas
// cells from a fresh read of the pack's JSON, footprints from mesh bounds (inactive meshes
// included), never from the builders' own tables. A check that needs JewelryModels or
// SurfaceDecals logs an Info line and skips without that pack, as in a project unpacked from
// the ZIP alone; a piece or decal the pack has but the build left out fails. Which presenter
// sets exist, that each is built hidden and how its switch behaves are the presenter file's
// (PresenterBuildChecks, PresenterArrivalChecks); here only what the hidden decals are. File-local
// members carry the Dressing or Decal prefix, so the other area files never collide with them.
public static partial class NightStoreValidation
{
    // ---------------------------------------------------------------- closing procedure

    private static void ClosingProcedureChecks(Transform root)
    {
        DemoValidation.ClearRayMeshes();
        try
        {
            DressingDecks(root);
            DressingSafes(root);
            DressingDeskAndFloor(root);
            DressingStockLook(root);
            DressingGlassZones(root);
        }
        finally { DemoValidation.ClearRayMeshes(); }
    }

    // Each case's deck group (one per case, merged by CombineLoose into one renderer per
    // material) holds display furniture only under the closing procedure: no gold, silver,
    // gem or pearl (chains, rings, loose jewellery) and no leather (an opened ring box), and
    // it still rests on its deck (a ray down from 1 cm over its lowest point meets the case
    // body 1 cm below). With StockInSafes false the intact decks are stocked again instead.
    private static void DressingDecks(Transform root)
    {
        Transform displays = root.Find("Dressing/" + NightStoreDressing.DisplaysGroup);
        HashSet<Material> jewellery = new HashSet<Material>();
        foreach (Material m in new[] { StoreKit.Gold, StoreKit.Silver, NightStoreDressing.Gem, NightStoreDressing.Pearl })
            if (m != null) jewellery.Add(m);
        bool safes = NightStoreDressing.StockInSafes;
        int cases = 0, intact = 0, stocked = 0, renderers = 0;
        List<string> wrong = new List<string>();
        foreach (L.Fixture f in L.Fixtures)
        {
            if (f.Kit != "display_case" && f.Kit != "wall_case") continue;
            cases++;
            Transform group = displays != null ? displays.Find(f.Name) : null, fixture = root.Find("Showroom/" + f.Name);
            Renderer[] parts = group != null ? group.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            if (fixture == null || parts.Length == 0) { DressingWrong(wrong, f.Name + " has no deck furniture"); continue; }
            renderers += parts.Length;
            bool metal = false, gilt = false, leather = false;
            foreach (Renderer r in parts)
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    metal |= jewellery.Contains(m);
                    gilt |= m == StoreKit.Gold || m == StoreKit.Silver;
                    leather |= m == StoreKit.Leather;
                }
            // Robbed: a broken case (its glass left Shards) or the knocked one.
            bool robbed = fixture.Find("Shards") != null || f.Name == "KnockedCase";
            if (!robbed) intact++;
            if (safes && (metal || leather)) DressingWrong(wrong, f.Name + (metal ? " shows jewellery" : " shows an opened box"));
            if (!safes && !robbed)
            {
                if (gilt) stocked++;
                else DressingWrong(wrong, f.Name + " intact but not stocked");
            }
            float gap = float.NaN;
            bool rests = DressingMeshBounds(group, out Bounds b) && DressingSupport(b, DressingSurfaces(root, null, "Showroom/" + f.Name + "/Body"), out gap);
            if (!rests) DressingWrong(wrong, f.Name + " furniture off its deck (" + DressingGapText(gap) + ")");
        }
        DemoValidation.Info("Closing procedure (StockInSafes=" + safes + "): case groups=" + cases + ", merged renderers=" + renderers + ", intact cases=" + intact
            + (safes ? "" : ", stocked=" + stocked) + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(cases > 0 && wrong.Count == 0,
            safes ? "Cases hold display furniture only under the closing procedure" : "Intact cases keep their stocked decks while StockInSafes is off");
    }

    // The jewellery left in the open safes (Dressing/Stock/<safe>): each piece inside the safe's
    // body (shell, lining and shelves; the open door swings clear and is left out) with 5 mm to
    // spare, and resting on the lining floor or a shelf, which a ray down at its centre from
    // 1 cm over its lowest point meets 1 cm below (4 mm either way). Without JewelryModels only
    // StoreKit's closed ring boxes stand in, seated the same way; the tall safe stays empty.
    private static void DressingSafes(Transform root)
    {
        bool pack = DemoAssetLibrary.Has(NightStoreDressing.JewelryPack, "GoldCoin");
        int pieces = 0;
        float worst = 0;
        List<string> counts = new List<string>(), wrong = new List<string>();
        foreach (string safe in new[] { "SafeLarge", "SafeTall" })
        {
            Transform group = root.Find("Dressing/" + NightStoreDressing.StockGroup + "/" + safe);
            if (group == null || group.childCount == 0)
            {
                // Both safes keep a coin when the pack is synced.
                if (pack) DressingWrong(wrong, safe + " holds nothing");
                counts.Add(safe + " none");
                continue;
            }
            List<DemoValidation.RayMesh> surfaces = DressingSurfaces(root, "Door", "Office/" + safe);
            if (!DressingSurfaceBox(surfaces, out Bounds body)) { DressingWrong(wrong, safe + " has no body mesh"); continue; }
            body.Expand(0.01f);
            foreach (Transform piece in group)
            {
                pieces++;
                if (!DressingMeshBounds(piece, out Bounds b)) { DressingWrong(wrong, safe + "/" + piece.name + " has no mesh"); continue; }
                bool inside = DressingContains(body, b);
                bool rests = DressingSupport(b, surfaces, out float gap);
                if (!float.IsNaN(gap)) worst = Mathf.Max(worst, Mathf.Abs(gap));
                if (!inside || !rests)
                    DressingWrong(wrong, safe + "/" + piece.name + (inside ? "" : " outside the safe") + (rests ? "" : " not on the lining or a shelf (" + DressingGapText(gap) + ")"));
            }
            counts.Add(safe + " " + group.childCount);
        }
        DemoValidation.Info("Left-behind jewellery in the safes: " + string.Join(", ", counts) + (pack ? "" : " (JewelryModels not synced: StoreKit ring boxes only)")
            + "; largest support gap " + DemoValidation.Mm(worst) + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        if (pieces == 0 && wrong.Count == 0)
        {
            DemoValidation.Info("No left-behind jewellery in either safe: safe stock check skipped");
            return;
        }
        DemoValidation.Check(wrong.Count == 0, "Left-behind jewellery lies on the open safes' lining and shelves");
    }

    // The loupe on the desk top (Office/Desk) and the pieces dropped on the floor (Stock/Floor),
    // each resting on its surface (as the safe stock), the floor ones at least 0.3 m in x/z
    // from the walk route (NightStoreLayout.Route) and from every UV trace's footprint. The
    // traces start hidden, so their footprints come from their meshes, not their renderers.
    private static void DressingDeskAndFloor(Transform root)
    {
        Transform stock = root.Find("Dressing/" + NightStoreDressing.StockGroup);
        Transform desk = stock != null ? stock.Find(NightStoreDressing.StockDesk) : null, floor = stock != null ? stock.Find(NightStoreDressing.StockFloor) : null;
        List<string> wrong = new List<string>();
        int onDesk = 0, onFloor = 0;
        if (desk != null)
        {
            List<DemoValidation.RayMesh> top = DressingSurfaces(root, null, "Office/Desk");
            foreach (Transform piece in desk)
            {
                onDesk++;
                float gap = float.NaN;
                bool rests = DressingMeshBounds(piece, out Bounds b) && DressingSupport(b, top, out gap);
                if (!rests) DressingWrong(wrong, piece.name + " not on the desk (" + DressingGapText(gap) + ")");
            }
        }
        if (onDesk == 0 && DemoAssetLibrary.Has(NightStoreDressing.JewelryPack, "JewelersLoupe")) DressingWrong(wrong, "no loupe on the desk");

        List<Rect> traces = new List<Rect>();
        Transform examples = root.Find(UVTraceBuilder.ExamplesName);
        if (examples != null)
            foreach (UVTrace trace in examples.GetComponentsInChildren<UVTrace>(true))
                if (DressingMeshBounds(trace.transform, out Bounds quad)) traces.Add(DemoValidation.Footprint(quad));
        float route = float.MaxValue, apart = float.MaxValue;
        if (floor != null)
        {
            List<DemoValidation.RayMesh> ground = DressingSurfaces(root, null, "Architecture/Floor");
            foreach (Transform piece in floor)
            {
                onFloor++;
                if (!DressingMeshBounds(piece, out Bounds b)) { DressingWrong(wrong, piece.name + " has no mesh"); continue; }
                if (!DressingSupport(b, ground, out float gap)) DressingWrong(wrong, piece.name + " not on the floor (" + DressingGapText(gap) + ")");
                Rect footprint = DemoValidation.Footprint(b);
                float fromRoute = DressingRouteGap(root, footprint);
                route = Mathf.Min(route, fromRoute);
                if (fromRoute < 0.3f) DressingWrong(wrong, piece.name + " " + fromRoute.ToString("0.00") + " m from the walk route");
                foreach (Rect t in traces)
                {
                    float fromTrace = DemoValidation.FootprintGap(footprint, t);
                    apart = Mathf.Min(apart, fromTrace);
                    if (fromTrace < 0.3f) DressingWrong(wrong, piece.name + " " + fromTrace.ToString("0.00") + " m from a UV trace");
                }
            }
        }
        DemoValidation.Info("Left-behind jewellery: desk=" + onDesk + ", floor=" + onFloor
            + (onFloor > 0 ? "; nearest walk route " + route.ToString("0.00") + " m" : "; Stock/Floor is empty")
            + (examples == null ? "; UV trace examples absent (no UV shader): trace clearance not measured"
                : traces.Count > 0 && onFloor > 0 ? "; nearest UV trace " + apart.ToString("0.00") + " m of " + traces.Count + " traces" : "")
            + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        if (onDesk + onFloor == 0 && wrong.Count == 0)
        {
            DemoValidation.Info("No left-behind jewellery on the desk or the floor: checks skipped");
            return;
        }
        DemoValidation.Check(wrong.Count == 0, "Jewellery on the desk and the floor rests on its surface, clear of the walk route and the UV traces");
    }

    // The pack jewellery left behind, kept out of CombineLoose: an LODGroup wherever the pack
    // ships the level files (Tiny classes from their own _LOD1 file, the others from the model
    // itself), every renderer shadowless, out of GI and probe-lit, every material opaque but the
    // loupe's StoreKit glass lens (whose renderer takes no lightmap either way). StoreKit stand-ins
    // (no pack) are counted, not checked.
    private static void DressingStockLook(Transform root)
    {
        Transform stock = root.Find("Dressing/" + NightStoreDressing.StockGroup);
        string pack = NightStoreDressing.JewelryPack;
        List<string> wrong = new List<string>();
        int pieces = 0, standIns = 0, groups = 0, lenses = 0;
        if (stock != null)
            foreach (Transform place in stock)
                foreach (Transform piece in place)
                {
                    string model = piece.name;
                    if (!DemoAssetLibrary.Has(pack, model) || piece.Find("LOD0") == null) { standIns++; continue; }
                    pieces++;
                    foreach (MeshRenderer r in piece.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        Material[] materials = r.sharedMaterials;
                        bool clear = materials.Length > 0, opaque = materials.Length > 0;
                        foreach (Material m in materials)
                        {
                            clear &= m != null && m.renderQueue >= 2500;
                            opaque &= m != null && m.renderQueue < 2500;
                        }
                        bool ok = r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off
                            && (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) == 0
                            && r.lightProbeUsage == UnityEngine.Rendering.LightProbeUsage.BlendProbes;
                        if (clear)
                        {
                            lenses++;
                            foreach (Material m in materials) ok &= m == StoreKit.Glass;
                        }
                        else ok &= opaque && r.receiveGI == ReceiveGI.LightProbes;
                        if (!ok) DressingWrong(wrong, DressingPath(r.transform) + (clear ? " (glass)" : opaque ? "" : " (blended)"));
                    }
                    DemoAssetLibrary.LodClass lod = NightStoreDressing.JewelLod(model);
                    bool lod1 = DemoAssetLibrary.Has(pack, model + "_LOD1"), lod2 = DemoAssetLibrary.Has(pack, model + "_LOD2");
                    bool expected = lod != DemoAssetLibrary.LodClass.None && (lod == DemoAssetLibrary.LodClass.Tiny ? lod1 && lod2 : lod1 || lod2);
                    LODGroup group = piece.GetComponent<LODGroup>();
                    bool levels = group != null && group.lodCount >= 2;
                    if (levels) groups++;
                    if (levels != expected) DressingWrong(wrong, model + (expected ? " lost its LOD levels" : " has an unexpected LODGroup"));
                    string top = lod == DemoAssetLibrary.LodClass.Tiny && lod1 ? model + "_LOD1" : model;
                    Renderer[] first = group != null && group.lodCount > 0 ? group.GetLODs()[0].renderers : piece.Find("LOD0").GetComponentsInChildren<Renderer>(true);
                    bool source = first.Length > 0;
                    foreach (Renderer r in first)
                    {
                        MeshFilter filter = r != null ? r.GetComponent<MeshFilter>() : null;
                        source &= filter != null && filter.sharedMesh != null
                            && System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(filter.sharedMesh)) == top;
                    }
                    if (!source) DressingWrong(wrong, model + " top level not from " + top);
                }
        if (pieces == 0)
        {
            DemoValidation.Info("JewelryModels not synced (" + standIns + " StoreKit stand-ins in Stock): left-behind LOD and look checks skipped");
            return;
        }
        DemoValidation.Info("Left-behind pack jewellery: pieces=" + pieces + ", LOD groups=" + groups + ", glass lens renderers=" + lenses + ", stand-ins=" + standIns
            + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(stock != null && wrong.Count == 0,
            "Left-behind jewellery keeps its LOD levels, casts no shadow and stays opaque and probe-lit (glass lenses aside)");
    }

    // NightStoreDressing.GlassZones (the footsteps' glass areas, root x/z rects) hold every
    // resting vertex of the entrance fans, every floor vertex (under 5 cm) of the broken cases'
    // Shards and, with SurfaceDecals, the shard scatter's impact point on each floor quad. The
    // entrance doors' and the right sidelight's own floor fragments are only reported: the
    // zones do not cover all of them (a known gap, the footsteps stay hard there).
    private static void DressingGlassZones(Transform root)
    {
        Rect[] zones = NightStoreDressing.GlassZones();
        Transform dressing = root.Find("Dressing");
        List<Vector3> fans = dressing != null ? NightStoreDressing.GlassVertices(dressing) : new List<Vector3>();
        int fanOut = DressingOutside(root, zones, fans);
        int cases = 0, spill = 0, spillOut = 0;
        foreach (L.Fixture f in L.Fixtures)
        {
            if (f.Kit != "display_case" && f.Kit != "wall_case") continue;
            Transform shards = root.Find("Showroom/" + f.Name + "/Shards");
            if (shards == null) continue;
            cases++;
            List<Vector3> floor = DressingFloorVertices(shards);
            spill += floor.Count;
            spillOut += DressingOutside(root, zones, floor);
        }
        bool scatter = root.Find(NightDecals.FloorGroup) != null;
        List<Vector3> impacts = scatter ? DecalImpactPoints(root) : new List<Vector3>();
        int impactOut = DressingOutside(root, zones, impacts);
        string facade = "";
        foreach (string path in new[] { "Facade/EntranceDoors/Shards", "Facade/Sidelight_Right/Shards" })
        {
            Transform t = root.Find(path);
            if (t == null) { facade += "; " + path + " absent"; continue; }
            List<Vector3> floor = DressingFloorVertices(t);
            facade += "; " + path + " floor vertices outside every zone " + DressingOutside(root, zones, floor) + " of " + floor.Count;
        }
        DemoValidation.Info("Glass zones=" + zones.Length + ": fan vertices outside " + fanOut + " of " + fans.Count + "; broken cases=" + cases
            + ", floor spill outside " + spillOut + " of " + spill
            + (scatter ? "; scatter impact points outside " + impactOut + " of " + impacts.Count : "; Architecture/FloorDecals absent (SurfaceDecals not synced): impact points not checked")
            + facade + " (known gap: footsteps stay hard there)");
        DemoValidation.Check(fans.Count > 0 && fanOut == 0 && cases > 0 && spill > 0 && spillOut == 0 && (!scatter || (impacts.Count > 0 && impactOut == 0)),
            "Glass zones cover the entrance fans, the broken cases' floor spill and the shard scatter's impact points");
    }

    // ---------------------------------------------------------------- decals

    private static void DecalChecks(Transform root)
    {
        DemoValidation.ClearRayMeshes();
        try
        {
            Dictionary<string, Rect> ambient = DecalLayout(NightDecals.AmbientAtlas, out Texture2D ambientPage);
            Dictionary<string, Rect> evidence = DecalLayout(NightDecals.EvidenceAtlas, out Texture2D evidencePage);
            Texture2D shards = DemoAssetLibrary.Texture(NightDecals.Pack, NightDecals.ShardsFile + ".png");
            DecalShardPlacement(root, shards);
            DecalFlags(root, shards, ambient);
            DecalCells(root, ambient, ambientPage, evidence, evidencePage, shards);
            DecalLift(root);
            DecalHiddenSets(root, ambient);
            DecalTrail(root, evidence);
        }
        finally { DemoValidation.ClearRayMeshes(); }
    }

    // One shard scatter in front of each broken case and none before an intact one: every
    // impact point lies in exactly one broken case's strip (case space |x| <= 1, z -1.2..-0.3,
    // as the glass zones), each broken case holds exactly one, and none lies within 1.3 m in
    // front of an intact case (|x| <= 1, z -1.3..0). Broken = the case left Shards.
    private static void DecalShardPlacement(Transform root, Texture2D shards)
    {
        if (shards == null)
        {
            DemoValidation.Info("SurfaceDecals " + NightDecals.ShardsFile + " not present: shard scatter placement not checked");
            return;
        }
        List<Transform> broken = new List<Transform>(), intact = new List<Transform>();
        foreach (L.Fixture f in L.Fixtures)
        {
            if (f.Kit != "display_case" && f.Kit != "wall_case") continue;
            Transform c = root.Find("Showroom/" + f.Name);
            if (c == null) continue;
            if (c.Find("Shards") != null) broken.Add(c);
            else intact.Add(c);
        }
        List<Vector3> impacts = DecalImpactPoints(root);
        int[] held = new int[broken.Count];
        List<string> wrong = new List<string>(), at = new List<string>();
        foreach (Vector3 p in impacts)
        {
            at.Add("(" + p.x.ToString("0.00") + ", " + p.z.ToString("0.00") + ")");
            int strips = 0;
            for (int i = 0; i < broken.Count; i++)
            {
                Vector3 l = broken[i].InverseTransformPoint(p);
                if (Mathf.Abs(l.x) <= 1 && l.z >= -1.2f && l.z <= -0.3f) { strips++; held[i]++; }
            }
            if (strips != 1) DressingWrong(wrong, "impact point at x " + p.x.ToString("0.00") + ", z " + p.z.ToString("0.00") + " in " + strips + " broken cases' strips");
            foreach (Transform c in intact)
            {
                Vector3 l = c.InverseTransformPoint(p);
                if (Mathf.Abs(l.x) <= 1 && l.z >= -1.3f && l.z <= 0) DressingWrong(wrong, "shards in front of intact " + c.name);
            }
        }
        for (int i = 0; i < broken.Count; i++)
            if (held[i] != 1) DressingWrong(wrong, broken[i].name + " has " + held[i] + " scatters");
        DemoValidation.Info("Shard scatters=" + impacts.Count + " for broken cases=" + broken.Count + ", impact points " + string.Join(" ", at)
            + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(broken.Count > 0 && impacts.Count == broken.Count && wrong.Count == 0, "Glass shards lie in front of the broken cases only");
    }

    // Each always-on group the packs allow (the floor scatter with GlassShards_Scatter, the
    // street, pavement, far pavement and neighbour groups with the Ambient atlas): shadowless,
    // batching and occludee static but never an occluder or reflection-probe static, and either
    // lightmapped (DecalsLightmapped: one renderer on the group with a second UV set, in GI, at
    // the lightmap scale of the surface under it) or probe-lit and out of GI; its materials
    // Standard Fade in the README's decal queues (before every other transparent) without depth
    // writes or emission.
    private static void DecalFlags(Transform root, Texture2D shards, Dictionary<string, Rect> ambient)
    {
        List<string> found = new List<string>(), wrong = new List<string>();
        int renderers = 0, quads = 0;
        foreach (string[] pair in DecalSurfacePaths)
        {
            string path = pair[0];
            bool floor = path == NightDecals.FloorGroup;
            bool expected = floor ? shards != null : ambient != null && (path != NightDecals.NeighboursGroup || ambient.ContainsKey("GrimeStreaks_Wall"));
            Transform group = root.Find(path), under = root.Find(pair[1]);
            if (group == null)
            {
                if (expected) DressingWrong(wrong, path + " missing");
                continue;
            }
            MeshRenderer surface = under != null ? under.GetComponent<MeshRenderer>() : null;
            MeshRenderer[] list = group.GetComponentsInChildren<MeshRenderer>(true);
            if (NightDecals.DecalsLightmapped ? list.Length != 1 || list[0].transform != group : list.Length == 0)
                DressingWrong(wrong, path + " has " + list.Length + " renderers");
            string material = floor ? NightDecals.ShardsMaterial : NightDecals.AmbientMaterial;
            float scale = 0;
            foreach (MeshRenderer r in list)
            {
                renderers++;
                scale = r.scaleInLightmap;
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                MeshFilter filter = r.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (mesh != null)
                    for (int s = 0; s < mesh.subMeshCount; s++) quads += (int)mesh.GetIndexCount(s) / 6;
                bool ok = r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off
                    && (flags & StaticEditorFlags.BatchingStatic) != 0 && (flags & StaticEditorFlags.OccludeeStatic) != 0
                    && (flags & (StaticEditorFlags.OccluderStatic | StaticEditorFlags.ReflectionProbeStatic)) == 0;
                if (NightDecals.DecalsLightmapped)
                    ok &= (flags & StaticEditorFlags.ContributeGI) != 0 && r.receiveGI == ReceiveGI.Lightmaps
                        && mesh != null && mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1)
                        && surface != null && Mathf.Abs(r.scaleInLightmap - surface.scaleInLightmap) < 0.001f;
                else ok &= (flags & StaticEditorFlags.ContributeGI) == 0 && r.receiveGI == ReceiveGI.LightProbes;
                foreach (Material m in r.sharedMaterials)
                    ok &= m != null && m.name == material && DecalQueued(m) && (m.shader.name == "CrimeScene/SurfaceDecal" || m.IsKeywordEnabled("_ALPHABLEND_ON"))
                        && m.GetFloat("_ZWrite") == 0 && !m.IsKeywordEnabled("_EMISSION");
                if (!ok) { DemoValidation.Info("Decal detail " + DressingPath(r.transform) + " flags=" + flags + " GI=" + r.receiveGI + " shadow=" + r.shadowCastingMode + " uv2=" + (mesh != null && mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1)) + " mats=" + string.Join(",", System.Array.ConvertAll(r.sharedMaterials, m => m.name + ":" + m.renderQueue + ":blend=" + (m.shader.name == "CrimeScene/SurfaceDecal" || m.IsKeywordEnabled("_ALPHABLEND_ON")) + ":z=" + m.GetFloat("_ZWrite") + ":em=" + m.IsKeywordEnabled("_EMISSION")))); DressingWrong(wrong, DressingPath(r.transform)); }
            }
            found.Add(path.Substring(path.LastIndexOf('/') + 1) + (NightDecals.DecalsLightmapped
                ? " x" + scale.ToString("0.00") + " on " + pair[1] + (surface != null ? " x" + surface.scaleInLightmap.ToString("0.00") : " (missing)") : ""));
        }
        if (found.Count == 0 && wrong.Count == 0)
        {
            DemoValidation.Info("SurfaceDecals not present: always-on decal checks skipped");
            return;
        }
        DemoValidation.Info("Always-on decals (" + (NightDecals.DecalsLightmapped ? "lightmapped" : "probe-lit") + "): " + string.Join(", ", found)
            + "; renderers=" + renderers + ", quads=" + quads + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(wrong.Count == 0, "Always-on decals cast no shadow, are never occluders or probe-static, and bake at their surface's density");
    }

    // SurfaceDecals README: decals draw at render queue 2450-2500, before every other transparent.
    private static bool DecalQueued(Material m) => m.renderQueue >= 2450 && m.renderQueue <= 2500;

    // SurfaceDecals README: DirtEdge_WallBase keeps its source's 4:1 pixels on the 8:1 real size,
    // 496 x 992 texels per metre (U per V = 0.5); every other cell is isotropic.
    private static readonly Dictionary<string, float> DecalAnisotropy = new Dictionary<string, float> { ["DirtEdge_WallBase"] = 0.5f };

    // Every decal triangle (the always-on groups' and the hidden sets' Decals_* and GlassPrints_*)
    // maps inside one cell of its material's page, read fresh from the pack's JSON (the shard
    // scatter: the whole texture), within 1e-4, and at the cell's aspect: texels per metre along
    // U and along V agree within 3 % (the README's anisotropic cells at their ratio). A mesh
    // kept from before an atlas change, or a quad sized off its cell, fails here.
    private static void DecalCells(Transform root, Dictionary<string, Rect> ambient, Texture2D ambientPage,
        Dictionary<string, Rect> evidence, Texture2D evidencePage, Texture2D shards)
    {
        List<MeshFilter> meshes = new List<MeshFilter>();
        foreach (string path in NightDecals.AlwaysOnGroups) meshes.AddRange(DecalMeshesAt(root, path));
        meshes.AddRange(DecalHiddenMeshes(root));
        if (meshes.Count == 0)
        {
            DemoValidation.Info("SurfaceDecals not present (no decal meshes): atlas cell checks skipped");
            return;
        }
        Dictionary<string, Rect> whole = new Dictionary<string, Rect> { [NightDecals.ShardsFile] = new Rect(0, 0, 1, 1) };
        int triangles = 0, outside = 0, skewed = 0;
        float worst = 0;
        string worstCell = "";
        HashSet<string> used = new HashSet<string>();
        List<string> wrong = new List<string>();
        foreach (MeshFilter f in meshes)
            foreach (DecalTri t in DecalTris(f))
            {
                triangles++;
                string name = t.Material != null ? t.Material.name : "(none)";
                Dictionary<string, Rect> cells = null;
                Texture2D page = null;
                if (name == NightDecals.AmbientMaterial) { cells = ambient; page = ambientPage; }
                else if (name == NightDecals.ShardsMaterial) { cells = shards != null ? whole : null; page = shards; }
                else if (name == NightDecals.EvidenceMaterial || name == NightDecals.EvidenceGlassMaterial) { cells = evidence; page = evidencePage; }
                if (cells == null || page == null || !DecalCellOf(t, cells, out string cell))
                {
                    outside++;
                    DressingWrong(wrong, t.Path + " (" + name + ") outside every cell");
                    continue;
                }
                used.Add(cell);
                if (!DecalTangents(t, out Vector3 du, out Vector3 dv)) { skewed++; DressingWrong(wrong, t.Path + " degenerate"); continue; }
                float ratio = (page.width / du.magnitude) / (page.height / dv.magnitude);
                float off = Mathf.Abs(ratio / (DecalAnisotropy.TryGetValue(cell, out float k) ? k : 1) - 1);
                if (off > worst) { worst = off; worstCell = cell; }
                if (off > 0.03f) { skewed++; DressingWrong(wrong, t.Path + " " + cell + " at " + (off * 100).ToString("0.0") + " % off its aspect"); }
            }
        DemoValidation.Info("Decal triangles=" + triangles + " in " + meshes.Count + " meshes over " + used.Count + " cells; outside a cell=" + outside
            + ", off aspect=" + skewed + "; worst aspect " + (worst * 100).ToString("0.0") + " % (" + worstCell + ")"
            + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(outside == 0 && skewed == 0, "Decal quads sample only their own atlas cells at the cells' aspect");
    }

    // Each always-on group's decals over the surface it was made for, and the hidden sets'
    // over the shop floor, the pavement, the right entrance leaf and the two print cases'
    // glass: a ray from each triangle's centroid against its winding normal meets that
    // surface 0.9 to 4.5 mm away (no z-fighting, no floating) on a face parallel to it.
    private static void DecalLift(Transform root)
    {
        List<string> lines = new List<string>(), wrong = new List<string>();
        int tested = 0;
        foreach (string[] pair in DecalSurfacePaths)
        {
            List<MeshFilter> meshes = DecalMeshesAt(root, pair[0]);
            if (meshes.Count > 0) tested += DecalLiftGroup(pair[0], meshes, DressingSurfaces(root, null, pair[1]), lines, wrong);
        }
        List<MeshFilter> hidden = DecalHiddenMeshes(root);
        if (hidden.Count > 0)
            tested += DecalLiftGroup(NightDecals.SetsPath, hidden, DressingSurfaces(root, null, "Architecture/Floor", "Exterior/Ground/Pavement", NightDecals.RightLeafPath,
                "Showroom/" + NightDecals.PrintCase + "/Glass", "Showroom/" + NightDecals.HandprintCase + "/Glass"), lines, wrong);
        if (tested == 0)
        {
            DemoValidation.Info("SurfaceDecals not present: decal lift not measured");
            return;
        }
        DemoValidation.Info("Decal lift over its surface (triangles " + tested + "): " + string.Join("; ", lines) + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(wrong.Count == 0, "Decals lie 1 to 4.5 mm above the surface beneath them, parallel to it");
    }

    // Lift of every triangle of meshes over surfaces, as "group min-max"; returns the triangles tested.
    private static int DecalLiftGroup(string group, List<MeshFilter> meshes, List<DemoValidation.RayMesh> surfaces, List<string> lines, List<string> wrong)
    {
        int tested = 0;
        float low = float.MaxValue, high = 0;
        foreach (MeshFilter f in meshes)
            foreach (DecalTri t in DecalTris(f))
            {
                tested++;
                Vector3 n = t.Normal;
                bool hit = DemoValidation.TriangleRay(new Ray(t.Centre, -n), 0.01f, surfaces, out float d, out Vector3 face);
                if (hit) { low = Mathf.Min(low, d); high = Mathf.Max(high, d); }
                if (!hit || d < 0.0009f || d > 0.0045f || Mathf.Abs(Vector3.Dot(face, n)) <= 0.99f)
                    DressingWrong(wrong, t.Path + (hit ? " " + DemoValidation.Mm(d) + " off a face at " + (Mathf.Acos(Mathf.Clamp01(Mathf.Abs(Vector3.Dot(face, n)))) * Mathf.Rad2Deg).ToString("0") + " deg" : " has no surface within 10 mm"));
            }
        lines.Add(group + " " + (low == float.MaxValue ? "no hit" : DemoValidation.Mm(low) + " to " + DemoValidation.Mm(high)));
        return tested;
    }

    // The hidden sets under Dressing/PresenterSets (which sets the packs call for, and that each is
    // built hidden with its Content a child, are PresenterBuildChecks'): every renderer in them
    // never static and probe-lit, the decal renderers shadowless, so nothing hidden enters the
    // bake or a static batch with what shows. The evidence set's Tools hold one object per tool
    // model, each resting on the floor or the pavement (a ray down at its centre; nearly given by
    // the seat, it shows a surface lies under it).
    private static void DecalHiddenSets(Transform root, Dictionary<string, Rect> ambient)
    {
        Transform holder = root.Find(NightDecals.SetsPath);
        List<string> wrong = new List<string>(), sets = new List<string>();
        PresenterSet evidence = null;
        bool wear = false;
        int renderers = 0;
        if (holder != null)
            foreach (PresenterSet set in holder.GetComponentsInChildren<PresenterSet>(true))
            {
                sets.Add(set.name);
                if (set.name == NightDecals.EvidenceSet) evidence = set;
                wear |= set.name == NightDecals.AmbientSet;
                if (set.Content == null)
                {
                    DressingWrong(wrong, set.name + " has no content");
                    continue;
                }
                foreach (MeshRenderer r in set.Content.GetComponentsInChildren<MeshRenderer>(true))
                {
                    renderers++;
                    bool decal = DecalNamed(r.name);
                    if (GameObjectUtility.GetStaticEditorFlags(r.gameObject) != 0 || r.receiveGI != ReceiveGI.LightProbes
                        || (decal && r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off))
                        DressingWrong(wrong, DressingPath(r.transform));
                }
            }
        if (evidence == null) DressingWrong(wrong, NightDecals.EvidenceSet + " missing");

        string[] models = NightDecals.ToolModels;
        Transform tools = evidence != null && evidence.Content != null ? evidence.Content.transform.Find(NightDecals.ToolsGroup) : null;
        List<DemoValidation.RayMesh> ground = DressingSurfaces(root, null, "Architecture/Floor", "Exterior/Ground/Pavement");
        int fromPack = 0, standIns = 0;
        float worst = 0;
        if (tools == null || tools.childCount != models.Length) DressingWrong(wrong, "Tools hold " + (tools != null ? tools.childCount : 0) + " of " + models.Length);
        else
            foreach (string model in models)
            {
                Transform tool = tools.Find(model);
                if (tool == null) { DressingWrong(wrong, model + " missing"); continue; }
                if (tool.Find("StandIn") != null) standIns++;
                else fromPack++;
                float gap = float.NaN;
                bool rests = DressingMeshBounds(tool, out Bounds b) && DressingSupport(b, ground, out gap);
                if (!float.IsNaN(gap)) worst = Mathf.Max(worst, Mathf.Abs(gap));
                if (!rests) DressingWrong(wrong, model + " not on the floor or pavement (" + DressingGapText(gap) + ")");
            }
        DemoValidation.Info("Presenter sets under " + NightDecals.SetsPath + ": " + string.Join(", ", sets) + "; renderers=" + renderers
            + (wear ? "" : "; " + NightDecals.AmbientSet + " not built" + (ambient != null ? "" : " (Ambient atlas not synced)"))
            + "; tools from " + NightStreet.ForensicPack + "=" + fromPack + ", stand-ins=" + standIns + ", largest support gap " + DemoValidation.Mm(worst)
            + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(holder != null && wrong.Count == 0, "Hidden decal sets stay out of the bake and the discarded tools rest on the floor or pavement");
        DecalHiddenSorting(root);
    }

    // The hidden decals sort as Standard Fade layers without depth writes: the floor and door
    // marks (Decals_*) in the README's decal queues, before every transparent, the latent prints
    // (GlassPrints_*) after the material of the case glass they lie on, so the glass never draws
    // over them.
    private static void DecalHiddenSorting(Transform root)
    {
        int glassQueue = 0;
        foreach (string fixture in new[] { NightDecals.PrintCase, NightDecals.HandprintCase })
        {
            Transform glass = root.Find("Showroom/" + fixture + "/Glass");
            Renderer r = glass != null ? glass.GetComponent<Renderer>() : null;
            if (r != null)
                foreach (Material m in r.sharedMaterials)
                    if (m != null) glassQueue = Mathf.Max(glassQueue, m.renderQueue);
        }
        int marks = 0, prints = 0;
        List<string> wrong = new List<string>();
        foreach (MeshFilter f in DecalHiddenMeshes(root))
        {
            Renderer r = f.GetComponent<Renderer>();
            if (r == null) continue;
            bool onGlass = f.name.StartsWith(NightDecals.GlassPrintsName + "_", StringComparison.Ordinal);
            if (onGlass) prints++;
            else marks++;
            foreach (Material m in r.sharedMaterials)
            {
                bool ok = m != null && (m.shader.name == "CrimeScene/SurfaceDecal" || m.IsKeywordEnabled("_ALPHABLEND_ON")) && m.GetFloat("_ZWrite") == 0 && !m.IsKeywordEnabled("_EMISSION")
                    && (onGlass ? glassQueue > 0 && m.renderQueue > glassQueue : DecalQueued(m));
                if (!ok) DressingWrong(wrong, DressingPath(f.transform) + (m != null ? " (" + m.name + " queue " + m.renderQueue + ")" : " (no material)"));
            }
        }
        if (marks + prints == 0)
        {
            DemoValidation.Info("No hidden decal renderers (SurfaceDecals not synced): hidden decal sorting not checked");
            return;
        }
        DemoValidation.Info("Hidden decal renderers: marks=" + marks + ", glass prints=" + prints + " over case glass at queue " + glassQueue
            + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(wrong.Count == 0, "Hidden floor marks draw before every transparent and the latent prints after the case glass");
    }

    // The case-evidence trail, read from the Evidence material's triangles of CaseEvidence's
    // Decals_* by the fresh Evidence cell holding their UVs: the mud prints on the pavement in
    // front of the forced doors (root z -2.6..-0.35, |x| under the doors' half width), at least
    // ten dust prints inside the showroom within 0.25 m of the walk route, the tracked dirt inside
    // the showroom within 1.5 m of the EntranceCentre anchor, and every shoe print at least 5 cm
    // clear of each piece dropped on the floor (Stock/Floor). The prints run up to 0.2 m either
    // side of the route and the pieces lie 0.3 m off it, so 5 cm is the margin the layout keeps.
    private static void DecalTrail(Transform root, Dictionary<string, Rect> evidence)
    {
        if (evidence == null)
        {
            DemoValidation.Info("SurfaceDecals Evidence atlas not present: the case-evidence trail is not checked");
            return;
        }
        Transform content = root.Find(NightDecals.SetsPath + "/" + NightDecals.EvidenceSet + "/" + NightDecals.ContentName);
        Transform entrance = root.Find("Anchors/EntranceCentre");
        Transform floor = root.Find("Dressing/" + NightStoreDressing.StockGroup + "/" + NightStoreDressing.StockFloor);
        List<Rect> pieces = new List<Rect>();
        if (floor != null)
            foreach (Transform piece in floor)
                if (DressingMeshBounds(piece, out Bounds b)) pieces.Add(DemoValidation.Footprint(b));
        List<string> wrong = new List<string>();
        int mud = 0, dust = 0, dirt = 0;
        float route = 0, door = float.MaxValue, clear = float.MaxValue;
        if (content != null)
            foreach (MeshFilter f in content.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!f.name.StartsWith(NightDecals.DecalsName + "_", StringComparison.Ordinal)) continue;
                foreach (DecalTri t in DecalTris(f))
                {
                    if (t.Material == null || t.Material.name != NightDecals.EvidenceMaterial || !DecalCellOf(t, evidence, out string cell)) continue;
                    Vector3 c = t.Centre, local = root.InverseTransformPoint(c);
                    bool shoe = cell.StartsWith("ShoePrint_", StringComparison.Ordinal);
                    if (cell.StartsWith("ShoePrint_Sneaker_Mud_", StringComparison.Ordinal))
                    {
                        mud++;
                        if (local.z < -2.6f || local.z > -0.35f || Mathf.Abs(local.x) >= L.DoorHalf)
                            DressingWrong(wrong, "mud print at x " + local.x.ToString("0.00") + ", z " + local.z.ToString("0.00"));
                    }
                    else if (cell.StartsWith("ShoePrint_Sneaker_Dust_", StringComparison.Ordinal))
                    {
                        dust++;
                        float off = DressingRouteGap(root, new Rect(c.x, c.z, 0, 0));
                        route = Mathf.Max(route, off);
                        if (!L.Showroom.Contains(local) || off > 0.25f)
                            DressingWrong(wrong, "dust print at x " + local.x.ToString("0.00") + ", z " + local.z.ToString("0.00") + " " + off.ToString("0.00") + " m off the route");
                    }
                    else if (cell == "DirtTracked_Entrance")
                    {
                        dirt++;
                        float fromDoor = entrance != null ? new Vector2(c.x - entrance.position.x, c.z - entrance.position.z).magnitude : float.MaxValue;
                        door = Mathf.Min(door, fromDoor);
                        if (!L.Showroom.Contains(local) || fromDoor > 1.5f)
                            DressingWrong(wrong, "tracked dirt " + (entrance != null ? fromDoor.ToString("0.00") + " m from the entrance" : "without the EntranceCentre anchor"));
                    }
                    if (!shoe) continue;
                    Vector2[] tri = { new Vector2(t.A.x, t.A.z), new Vector2(t.B.x, t.B.z), new Vector2(t.C.x, t.C.z) };
                    foreach (Rect p in pieces)
                    {
                        float gap = DecalTriangleRectGap(tri, p);
                        clear = Mathf.Min(clear, gap);
                        if (gap < 0.05f) DressingWrong(wrong, "shoe print " + gap.ToString("0.00") + " m from a dropped piece at x " + p.center.x.ToString("0.00") + ", z " + p.center.y.ToString("0.00"));
                    }
                }
            }
        if (mud == 0 || dust < 20 || dirt == 0) DressingWrong(wrong, "triangles mud=" + mud + ", dust=" + dust + ", tracked dirt=" + dirt);
        DemoValidation.Info("Case-evidence trail: mud prints=" + mud / 2 + ", dust prints=" + dust / 2 + " (furthest centroid " + route.ToString("0.00") + " m off the route), tracked dirt="
            + dirt / 2 + (dirt > 0 ? " (" + door.ToString("0.00") + " m from the entrance)" : "")
            + (pieces.Count > 0 ? "; nearest dropped piece " + clear.ToString("0.00") + " m from a shoe print" : "; no dropped floor pieces")
            + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(content != null && wrong.Count == 0, "The case-evidence trail runs from the pavement through the forced doors along the walk route");
    }

    // ---------------------------------------------------------------- arrival (Play mode)

    // At arrival (PresenterSet.Start has applied ShownAtStart, nothing toggled yet): no drawn
    // renderer samples a print-like texture (NightDecals.PrintLike: the Evidence page, the
    // print files, GlassSmudge, NightGraphics' ShoeTrace, Scuff and DustSmear), while the
    // evidence exists hidden whenever the Evidence page and its layout were synced, so the check
    // never passes on a build that placed none. The small shop's geometric shoe print
    // (JewelryStoreBuilder.ShoePrint: untextured treads, so no texture to find) is looked up by
    // name anywhere under the store root, as the night dressing builds none and one added again
    // could sit under Dressing or in the hidden evidence set; any drawn at arrival fails too.
    // Switches nothing (the sets' switches are PresenterArrivalChecks', which follows).
    private static void ArrivalPrintChecks()
    {
        Transform root = Root();
        List<string> drawn = new List<string>();
        int shown = 0, hidden = 0;
        foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string print = null;
            foreach (Material m in r.sharedMaterials)
            {
                Texture main = m != null && m.HasProperty("_MainTex") ? m.mainTexture : null;
                if (main != null && NightDecals.PrintLike(main.name)) print = main.name;
            }
            if (print == null) continue;
            if (r.enabled && r.gameObject.activeInHierarchy)
            {
                shown++;
                DressingWrong(drawn, DressingPath(r.transform) + " (" + print + ")");
            }
            else hidden++;
        }
        bool evidence = DecalLayout(NightDecals.EvidenceAtlas, out _) != null;
        // Drawn: the group active with an enabled tread renderer active under it.
        const string placeholderName = "ShoePrint_GeometryPlaceholder";
        List<string> placeholders = new List<string>();
        int treadsShown = 0;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != placeholderName) continue;
            bool drawnTread = false;
            if (t.gameObject.activeInHierarchy)
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                    drawnTread |= r.enabled && r.gameObject.activeInHierarchy;
            if (drawnTread) treadsShown++;
            DressingWrong(placeholders, DressingPath(t) + (drawnTread ? " VISIBLE at arrival" : " hidden"));
        }
        DemoValidation.Info("Print-like textures at arrival: drawn=" + shown + (shown > 0 ? " (" + string.Join(", ", drawn) + ")" : "") + ", hidden=" + hidden
            + (evidence ? "" : " (Evidence atlas or its layout not synced: no hidden prints expected)")
            + "; " + placeholderName + " (untextured footwear treads the texture scan cannot see): "
            + (placeholders.Count == 0 ? "absent" : string.Join(", ", placeholders)));
        DemoValidation.Check(shown == 0 && (!evidence || hidden > 0) && treadsShown == 0, "No print-like or footwear-like mark is drawn at arrival");
    }

    // ---------------------------------------------------------------- decal helpers

    // Each always-on group (NightDecals) and the surface it lies on.
    private static readonly string[][] DecalSurfacePaths =
    {
        new[] { NightDecals.FloorGroup, "Architecture/Floor" }, new[] { NightDecals.StreetGroup, "Exterior/Ground/Street" },
        new[] { NightDecals.PavementGroup, "Exterior/Ground/Pavement" }, new[] { NightDecals.FarPavementGroup, "Exterior/Ground/FarPavement" },
        new[] { NightDecals.NeighboursGroup, "Exterior/Buildings/Neighbours" },
    };

    // An atlas page of SurfaceDecals and its cell rects (DemoAssetLibrary.Atlas, read now) when
    // both files were synced; null otherwise.
    private static Dictionary<string, Rect> DecalLayout(string atlas, out Texture2D page)
    {
        page = DemoAssetLibrary.Texture(NightDecals.Pack, atlas + "_Color.png");
        if (page == null || AssetDatabase.LoadAssetAtPath<TextAsset>(DemoAssetLibrary.Path(NightDecals.Pack, atlas + ".json")) == null) return null;
        return DemoAssetLibrary.Atlas(NightDecals.Pack, atlas + ".json");
    }

    // The meshes of the always-on group at path (its own renderer, or its cells'), empty without it.
    private static List<MeshFilter> DecalMeshesAt(Transform root, string path)
    {
        Transform t = root.Find(path);
        return t != null ? new List<MeshFilter>(t.GetComponentsInChildren<MeshFilter>(true)) : new List<MeshFilter>();
    }

    private static bool DecalNamed(string name)
        => name.StartsWith(NightDecals.DecalsName + "_", StringComparison.Ordinal) || name.StartsWith(NightDecals.GlassPrintsName + "_", StringComparison.Ordinal);

    // The hidden sets' decal meshes (Decals_* and GlassPrints_* under each set's Content).
    private static List<MeshFilter> DecalHiddenMeshes(Transform root)
    {
        List<MeshFilter> list = new List<MeshFilter>();
        Transform sets = root.Find(NightDecals.SetsPath);
        if (sets == null) return list;
        foreach (PresenterSet set in sets.GetComponentsInChildren<PresenterSet>(true))
            if (set.Content != null)
                foreach (MeshFilter f in set.Content.GetComponentsInChildren<MeshFilter>(true))
                    if (DecalNamed(f.name)) list.Add(f);
        return list;
    }

    // One decal triangle in world space with its UVs, its submesh's material and its object's path.
    private struct DecalTri
    {
        public string Path;
        public Material Material;
        public Vector3 A, B, C;
        public Vector2 Ua, Ub, Uc;
        public Vector3 Centre => (A + B + C) / 3;
        public Vector3 Normal => Vector3.Cross(B - A, C - A).normalized;
    }

    private static List<DecalTri> DecalTris(MeshFilter f)
    {
        List<DecalTri> list = new List<DecalTri>();
        Mesh mesh = f.sharedMesh;
        MeshRenderer r = f.GetComponent<MeshRenderer>();
        if (mesh == null || r == null) return list;
        Vector3[] v = mesh.vertices;
        Vector2[] uv = mesh.uv;
        if (uv.Length != v.Length) uv = new Vector2[v.Length];
        Material[] materials = r.sharedMaterials;
        Matrix4x4 world = f.transform.localToWorldMatrix;
        string path = DressingPath(f.transform);
        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            int[] t = mesh.GetTriangles(s);
            Material m = s < materials.Length ? materials[s] : null;
            for (int i = 0; i + 2 < t.Length; i += 3)
                list.Add(new DecalTri
                {
                    Path = path, Material = m,
                    A = world.MultiplyPoint3x4(v[t[i]]), B = world.MultiplyPoint3x4(v[t[i + 1]]), C = world.MultiplyPoint3x4(v[t[i + 2]]),
                    Ua = uv[t[i]], Ub = uv[t[i + 1]], Uc = uv[t[i + 2]]
                });
        }
        return list;
    }

    // The cell whose rect holds all three UVs (1e-4 tolerance); false when none does.
    private static bool DecalCellOf(DecalTri t, Dictionary<string, Rect> cells, out string cell)
    {
        foreach (KeyValuePair<string, Rect> c in cells)
            if (DecalIn(c.Value, t.Ua) && DecalIn(c.Value, t.Ub) && DecalIn(c.Value, t.Uc))
            {
                cell = c.Key;
                return true;
            }
        cell = null;
        return false;
    }

    private static bool DecalIn(Rect r, Vector2 p)
        => p.x >= r.xMin - 1e-4f && p.x <= r.xMax + 1e-4f && p.y >= r.yMin - 1e-4f && p.y <= r.yMax + 1e-4f;

    // dP/du and dP/dv of a triangle (world metres per UV unit); false when its UVs are degenerate
    // or a tangent vanishes (InteriorTangents allows a zero one, so neither is shared).
    private static bool DecalTangents(DecalTri t, out Vector3 du, out Vector3 dv)
    {
        Vector3 e1 = t.B - t.A, e2 = t.C - t.A;
        Vector2 d1 = t.Ub - t.Ua, d2 = t.Uc - t.Ua;
        float det = d1.x * d2.y - d2.x * d1.y;
        du = dv = Vector3.zero;
        if (Mathf.Abs(det) < 1e-12f) return false;
        du = (e1 * d2.y - e2 * d1.y) / det;
        dv = (e2 * d1.x - e1 * d2.x) / det;
        return du.sqrMagnitude > 1e-12f && dv.sqrMagnitude > 1e-12f;
    }

    // The shard scatter's impact point (README: UV 0.50, 0.58) on every floor decal quad: the
    // world point of the one triangle of each quad whose UVs hold it (it lies off both diagonals).
    private static readonly Vector2 DecalImpactUv = new Vector2(0.5f, 0.58f);

    private static List<Vector3> DecalImpactPoints(Transform root)
    {
        List<Vector3> points = new List<Vector3>();
        foreach (MeshFilter f in DecalMeshesAt(root, NightDecals.FloorGroup))
            foreach (DecalTri t in DecalTris(f))
            {
                if (t.Material == null || t.Material.name != NightDecals.ShardsMaterial) continue;
                Vector2 e1 = t.Ub - t.Ua, e2 = t.Uc - t.Ua, p = DecalImpactUv - t.Ua;
                float det = e1.x * e2.y - e2.x * e1.y;
                if (Mathf.Abs(det) < 1e-12f) continue;
                float b = (p.x * e2.y - e2.x * p.y) / det, c = (e1.x * p.y - p.x * e1.y) / det, a = 1 - b - c;
                if (a >= -1e-6f && b >= -1e-6f && c >= -1e-6f) points.Add(t.A * a + t.B * b + t.C * c);
            }
        return points;
    }

    // Shortest x/z distance between a triangle and a rect (Rect y = world z): 0 when they touch
    // or overlap, else the nearest vertex of either to an edge of the other (both convex).
    private static float DecalTriangleRectGap(Vector2[] tri, Rect r)
    {
        Vector2[] box = { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };
        foreach (Vector2 p in tri)
            if (p.x >= r.xMin && p.x <= r.xMax && p.y >= r.yMin && p.y <= r.yMax) return 0;
        foreach (Vector2 p in box)
        {
            float s0 = DecalSide(tri[0], tri[1], p), s1 = DecalSide(tri[1], tri[2], p), s2 = DecalSide(tri[2], tri[0], p);
            if ((s0 >= 0 && s1 >= 0 && s2 >= 0) || (s0 <= 0 && s1 <= 0 && s2 <= 0)) return 0;
        }
        float best = float.MaxValue;
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 4; j++)
            {
                Vector2 a = tri[i], b = tri[(i + 1) % 3], c = box[j], d = box[(j + 1) % 4];
                if ((DecalSide(c, d, a) > 0) != (DecalSide(c, d, b) > 0) && (DecalSide(a, b, c) > 0) != (DecalSide(a, b, d) > 0)) return 0;
                best = Mathf.Min(best, Mathf.Min(DemoValidation.PointSegmentDistance(a, c, d), DemoValidation.PointSegmentDistance(c, a, b)));
            }
        return best;
    }

    // Which side of the line a-b the point p is on (the 2D cross product).
    private static float DecalSide(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);

    // ---------------------------------------------------------------- dressing helpers

    // Adds a reason to a check's failure list, at most eight of them (then an ellipsis).
    private static void DressingWrong(List<string> list, string note)
    {
        if (list.Count < 8) list.Add(note);
        else if (list.Count == 8) list.Add("...");
    }

    // The full scene path of t, for the logs.
    private static string DressingPath(Transform t) => DemoGeometry.PathUnder(t, null);

    private static string DressingGapText(float gap) => float.IsNaN(gap) ? "no surface within 2 cm below" : DemoValidation.Mm(gap) + " off";

    // World box of every mesh under t, inactive ones included: each mesh's bounds corners through
    // its object's matrix, as its renderer measures them when active. False without a mesh.
    private static bool DressingMeshBounds(Transform t, out Bounds bounds)
    {
        bounds = new Bounds();
        bool any = false;
        if (t == null) return false;
        foreach (MeshFilter f in t.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = f.sharedMesh;
            if (mesh == null || mesh.vertexCount == 0) continue;
            Matrix4x4 world = f.transform.localToWorldMatrix;
            Bounds local = mesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = world.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                else bounds.Encapsulate(p);
            }
        }
        return any;
    }

    // inner lies inside outer (both world boxes), exactly: the callers grow outer first, while
    // InteriorContains keeps a 0.1 mm tolerance, so neither is shared.
    private static bool DressingContains(Bounds outer, Bounds inner)
        => inner.min.x >= outer.min.x && inner.min.y >= outer.min.y && inner.min.z >= outer.min.z
            && inner.max.x <= outer.max.x && inner.max.y <= outer.max.y && inner.max.z <= outer.max.z;

    // A ray down at the box's centre from 1 cm over its lowest point meets the surfaces 1 cm
    // below, within 4 mm: the piece rests on them. gap is the measured offset, NaN without a hit.
    private static bool DressingSupport(Bounds b, List<DemoValidation.RayMesh> surfaces, out float gap)
    {
        gap = float.NaN;
        if (!DemoValidation.TriangleRay(new Ray(new Vector3(b.center.x, b.min.y + 0.01f, b.center.z), Vector3.down), 0.03f, surfaces, out float d, out _)) return false;
        gap = d - 0.01f;
        return Mathf.Abs(gap) <= 0.004f;
    }

    // x/z distance from a footprint to the walk route (NightStoreLayout.Route, in store-root space).
    private static float DressingRouteGap(Transform root, Rect r)
    {
        float best = float.MaxValue;
        Vector3[] route = L.Route;
        for (int i = 0; i + 1 < route.Length; i++)
        {
            Vector3 a = root.TransformPoint(route[i]), b = root.TransformPoint(route[i + 1]);
            best = Mathf.Min(best, DemoValidation.RectSegmentDistance(r, new Vector2(a.x, a.z), new Vector2(b.x, b.z)));
        }
        return best;
    }

    // World vertices under 5 cm (the floor fragments) of every mesh under t.
    private static List<Vector3> DressingFloorVertices(Transform t)
    {
        List<Vector3> points = new List<Vector3>();
        foreach (MeshFilter f in t.GetComponentsInChildren<MeshFilter>(true))
        {
            if (f.sharedMesh == null) continue;
            Matrix4x4 world = f.transform.localToWorldMatrix;
            foreach (Vector3 v in f.sharedMesh.vertices)
            {
                Vector3 p = world.MultiplyPoint3x4(v);
                if (p.y < 0.05f) points.Add(p);
            }
        }
        return points;
    }

    // How many points lie in no glass zone (zones in store-root x/z).
    private static int DressingOutside(Transform root, Rect[] zones, List<Vector3> points)
    {
        int outside = 0;
        foreach (Vector3 p in points)
        {
            Vector3 l = root.InverseTransformPoint(p);
            bool inside = false;
            foreach (Rect z in zones) inside |= z.Contains(new Vector2(l.x, l.z));
            if (!inside) outside++;
        }
        return outside;
    }

    // ---------------------------------------------------------------- triangle rays

    // The meshes under each path, skipping any below a child named skip (null: none skipped), as
    // DemoValidation.RayMeshes read once per check pass (inactive meshes included: hidden sets,
    // LOD levels, the UV traces), each boxed by its world-space vertices.
    private static List<DemoValidation.RayMesh> DressingSurfaces(Transform root, string skip, params string[] paths)
    {
        List<DemoValidation.RayMesh> list = new List<DemoValidation.RayMesh>();
        foreach (string path in paths)
        {
            Transform top = root.Find(path);
            if (top == null) continue;
            foreach (MeshFilter f in top.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = f.sharedMesh;
                if (mesh == null || mesh.vertexCount == 0 || (skip != null && DressingBelow(f.transform, top, skip))) continue;
                list.Add(DemoValidation.RayMeshOf(f));
            }
        }
        return list;
    }

    // t or one of its parents up to (not including) top is named name.
    private static bool DressingBelow(Transform t, Transform top, string name)
    {
        for (; t != null && t != top; t = t.parent)
            if (t.name == name) return true;
        return false;
    }

    private static bool DressingSurfaceBox(List<DemoValidation.RayMesh> surfaces, out Bounds box)
    {
        box = new Bounds();
        for (int i = 0; i < surfaces.Count; i++)
            if (i == 0) box = surfaces[0].Box;
            else box.Encapsulate(surfaces[i].Box);
        return surfaces.Count > 0;
    }
}
#endif
