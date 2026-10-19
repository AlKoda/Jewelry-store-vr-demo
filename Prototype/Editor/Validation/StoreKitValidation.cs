#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Edit-mode checks for the StoreKit fixture library. Check() builds the gallery in
// the current scene (the night validator calls it with keepGallery false); RunGallery
// is the -executeMethod entry that generates the StoreKitGallery scene, captures it
// and reports through DemoValidation like the other validators (no Play mode stage).
public static class StoreKitValidation
{
    private const string Key = "StoreKitValidationStage";
    public const int ExpectedFixtures = 31;

    public static void RunGallery()
    {
        try
        {
            DemoValidation.Begin("VerificationStoreKit");
            DemoValidation.WriteHeader("Store kit gallery generation and Edit mode checks; 3 actual Unity rendered captures.");
            Camera camera = DemoValidation.CreatePreviewCamera(150);
            Check(true);
            // Not deleted: the night store builds kit meshes the gallery does not (sidelights,
            // cracked tops, door variants), and deleting them would break a generated night scene.
            DemoValidation.Info("Store kit meshes not used by the gallery=" + KitMesh.CountUntouched(StoreKit.Folder));
            // Not registered as the build scene, so running this in a working project keeps its build list.
            DemoValidation.SaveScene("StoreKitGallery", false);
            DemoValidation.Capture(camera, "store-kit-gallery.png", new Vector3(0, 11, -10), new Vector3(0, 0.5f, 5.5f));
            DemoValidation.Capture(camera, "store-kit-closeup.png", new Vector3(-6.5f, 1.7f, -3.5f), new Vector3(-6.5f, 0.8f, 0));
            DemoValidation.Capture(camera, "store-kit-props.png", new Vector3(0, 1.3f, -5.4f), new Vector3(0, 0.05f, -3.35f));
            DemoValidation.Finish(Key, true, null);
        }
        catch (Exception ex) { DemoValidation.Finish(Key, false, ex.ToString()); }
    }

    // For other validators: builds, checks and removes the gallery.
    public static void Check() { Check(false); }

    // The fixture gallery is returned; the prop gallery ("StoreKitProps") is a separate
    // root beside it, kept or removed with it.
    public static Transform Check(bool keepGallery)
    {
        Transform gallery = StoreKit.BuildGallery(null);
        Transform props = StoreKit.BuildPropGallery(null);
        try
        {
            FixtureChecks(gallery);
            PaletteSetChecks();
            PaletteTilingChecks();
            PaletteFallbackChecks();
            HingeChecks(gallery);
            PaneChecks(gallery);
            ColliderChecks(gallery);
            CeilingChecks(gallery);
            EmissionChecks(gallery);
            CaseGlassChecks(gallery);
            FrameJointChecks(gallery);
            FixturePartChecks(gallery);
            SafeBoltChecks(gallery);
            CaseHardwareChecks(gallery);
            GlazingChecks(gallery);
            ContactChecks(gallery);
            DeviceChecks(gallery);
            PropChecks(props);
            LidChecks(props);
            CameraBodyChecks();
        }
        finally
        {
            if (!keepGallery)
            {
                UnityEngine.Object.DestroyImmediate(gallery.gameObject);
                UnityEngine.Object.DestroyImmediate(props.gameObject);
            }
        }
        return keepGallery ? gallery : null;
    }

    // Every prop and variant builds with UVs and normals inside its budget; loose props
    // carry no lightmap UVs, lightmapped ones do except their probe-lit jewelry parts.
    private static void PropChecks(Transform props)
    {
        bool ok = props.childCount > 0;
        int total = 0, renderers = 0, entries = 0;
        foreach (string name in StoreKit.PropNames) ok &= props.Find(name) != null;
        foreach (string name in StoreKit.PropVariants) ok &= props.Find(name) != null;
        foreach (Transform entry in props)
        {
            if (entry.name.StartsWith("Label_")) continue;
            entries++;
            int triangles = Triangles(entry), budget = StoreKit.Budget(entry.name);
            total += triangles;
            renderers += entry.GetComponentsInChildren<MeshRenderer>(true).Length;
            DemoValidation.Info(entry.name + " triangles=" + triangles + " budget=" + budget);
            ok &= budget > 0 && triangles > 0 && triangles <= budget;
            bool loose = StoreKit.IsLoose(entry.name);
            foreach (MeshFilter filter in entry.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                int n = mesh != null ? mesh.vertexCount : 0;
                bool probeLit = loose || Array.IndexOf(StoreKit.ProbeLitParts, filter.name) >= 0;
                ok &= n > 0 && mesh.uv.Length == n && mesh.normals.Length == n && (probeLit ? mesh.uv2.Length == 0 : mesh.uv2.Length == n);
            }
        }
        ok &= entries == StoreKit.PropNames.Length + StoreKit.PropVariants.Length;
        DemoValidation.Info("Store kit props triangles=" + total + "; renderers=" + renderers);
        DemoValidation.Check(ok, "Store kit jewelry props build and stay within budgets");
    }

    // A lid's pivot is the base's rear top edge and its mesh runs forward (-z) from it:
    // closed, its free edge rests on the base's front rim; open, it stands above the base.
    private static void LidChecks(Transform props)
    {
        bool hinged = true;
        foreach (string name in new[] { "jewelry_box", "jewelry_box__Bracelet", "jewelry_box__Necklace", "jewelry_box__Closed" })
        {
            Transform entry = props.Find(name);
            Transform body = entry != null ? entry.Find("Body") : null, lid = entry != null ? entry.Find("Lid") : null;
            if (body == null || lid == null || lid.GetComponent<MeshFilter>() == null) { hinged = false; continue; }
            Bounds b = body.GetComponent<MeshFilter>().sharedMesh.bounds, l = lid.GetComponent<MeshFilter>().sharedMesh.bounds;
            hinged &= Mathf.Abs(lid.localPosition.z - b.max.z) < 0.003f && Mathf.Abs(lid.localPosition.y - b.max.y) < 0.003f
                && l.max.z <= 0.001f && l.min.z < -0.03f && l.min.y > -0.001f;
            Vector3 free = entry.InverseTransformPoint(lid.TransformPoint(new Vector3(l.center.x, 0, l.min.z)));
            if (name.EndsWith("Closed")) hinged &= Mathf.Abs(free.z - b.min.z) < 0.003f && Mathf.Abs(free.y - b.max.y) < 0.003f;
            else hinged &= free.y > b.max.y + 0.03f;
        }
        DemoValidation.Check(hinged, "Jewelry box lids hinge at the rear");
    }

    // The handheld camera's procedural body on a scratch root: a DSLR (no box primitive)
    // within budget whose geometry stops short of the photo camera's pose at (0, 0, 0.1),
    // with the grab colliders on "Body" and "Lens" and the rear screen script. A scene
    // that already has the tool station's camera is checked for the same contract.
    private static void CameraBodyChecks()
    {
        GameObject scratch = new GameObject("CameraBodyProbe");
        try
        {
            DemoToolGeometry.CreateCameraBody(scratch.transform);
            Transform body = scratch.transform.Find("Body"), lens = scratch.transform.Find("Lens"), screen = scratch.transform.Find("Screen");
            Bounds bounds = DemoAssetLibrary.LocalBounds(scratch);
            int triangles = Triangles(scratch.transform);
            bool ok = body != null && lens != null && screen != null && body.GetComponent<BoxCollider>() != null
                && lens.GetComponent<CapsuleCollider>() != null && screen.GetComponent<CameraScreen>() != null
                && body.GetComponent<MeshFilter>().sharedMesh.name != "Cube"
                && triangles > 300 && triangles <= 2500 && bounds.max.z < 0.1f
                && bounds.size.x > 0.13f && bounds.size.x < 0.16f && bounds.size.y > 0.09f && bounds.size.y < 0.14f;
            GameObject placed = GameObject.Find("HandheldCamera");
            if (placed != null)
            {
                Transform photo = placed.transform.Find("PhotoCamera");
                ok &= photo != null && Vector3.Distance(photo.localPosition, new Vector3(0, 0, 0.1f)) < 0.001f
                    && placed.GetComponentInChildren<CameraScreen>() != null && placed.transform.Find("Body") != null
                    && DemoAssetLibrary.LocalBounds(placed).max.z < 0.1f;
            }
            DemoValidation.Info("DSLR camera body triangles=" + triangles + "; size=" + bounds.size.ToString("F3"));
            DemoValidation.Check(ok, "DSLR camera body replaces the box and keeps the photo camera pose");
        }
        finally
        {
            foreach (MeshFilter filter in scratch.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(filter.sharedMesh)))
                    UnityEngine.Object.DestroyImmediate(filter.sharedMesh);
            UnityEngine.Object.DestroyImmediate(scratch);
        }
    }

    private static void FixtureChecks(Transform gallery)
    {
        bool all = StoreKit.Names.Length == ExpectedFixtures;
        foreach (string name in StoreKit.Names)
        {
            Transform entry = gallery.Find(name);
            all &= entry != null && entry.GetComponentsInChildren<MeshFilter>().Length > 0;
        }
        DemoValidation.Check(all, "Store kit builds every fixture");

        bool meshes = true;
        int filters = 0;
        foreach (MeshFilter filter in gallery.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            int n = mesh != null ? mesh.vertexCount : 0;
            meshes &= n > 0 && mesh.uv.Length == n && mesh.uv2.Length == n && mesh.normals.Length == n;
            filters++;
        }
        DemoValidation.Check(meshes && filters > 0, "Store kit meshes carry metric UVs, normals and lightmap UVs");
        DemoValidation.Check(MetricUVs(), "Store kit UVs are metric (a 2 x 1 face spans 2 x 1 UV units)");
        DemoValidation.Check(LightmapUVs(gallery), "Store kit lightmap UVs lie inside [0,1] and are unwrapped");
        DemoValidation.Info("Store kit textured palette: " + string.Join(", ", StoreKit.TexturedPalette()));

        bool budgets = true;
        int total = 0, renderers = 0;
        foreach (Transform entry in gallery)
        {
            if (entry.name.StartsWith("Label_")) continue;
            int triangles = Triangles(entry);
            int budget = StoreKit.Budget(entry.name);
            total += triangles;
            renderers += entry.GetComponentsInChildren<MeshRenderer>(true).Length;
            int submeshes = 0;
            foreach (MeshFilter filter in entry.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null) submeshes += filter.sharedMesh.subMeshCount;
            DemoValidation.Info(entry.name + " triangles=" + triangles + " budget=" + budget + " submeshes=" + submeshes);
            budgets &= budget > 0 && triangles <= budget;
        }
        DemoValidation.Info("Store kit gallery triangles=" + total + "; renderers=" + renderers);
        DemoValidation.Check(budgets, "Store kit fixtures stay within their triangle budgets");
    }

    // Palette sets come from one pack each: every map a textured palette material carries is
    // the file its chosen set names (PavementSlab's wet colour and finish with the dry normal
    // and AO, all PBRExterior, is the only pairing), no higher-priority set's colour file
    // exists, and the ReferenceTextures and generated sets carry no packed map.
    private static void PaletteSetChecks()
    {
        bool ok = true;
        int sourced = 0, named = 0;
        string[] slots = { "_MainTex", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" };
        foreach (KeyValuePair<Material, string> entry in StoreKit.PaletteTextures)
        {
            Material m = entry.Key;
            if (m == null) continue;
            named++;
            if (!StoreKit.PaletteSet(m, out StoreKit.SurfaceSet set))
            {
                foreach (string slot in slots) ok &= m.GetTexture(slot) == null;
                foreach (StoreKit.SurfaceSet unused in StoreKit.PaletteSets(entry.Value)) ok &= !DemoAssetLibrary.HasTexture(unused.Pack, unused.Color);
                continue;
            }
            sourced++;
            string[] files = { set.Color, set.Normal, set.Ms, set.Ao };
            for (int i = 0; i < slots.Length; i++)
            {
                Texture t = m.GetTexture(slots[i]);
                bool present = files[i] != null && DemoAssetLibrary.HasTexture(set.Pack, files[i]);
                ok &= t == null ? !present : present && AssetDatabase.GetAssetPath(t) == DemoAssetLibrary.Path(set.Pack, files[i]);
            }
            ok &= m.mainTexture != null;
            foreach (StoreKit.SurfaceSet earlier in StoreKit.PaletteSets(entry.Value))
            {
                if (earlier.Pack == set.Pack && earlier.Color == set.Color) break;
                ok &= !DemoAssetLibrary.HasTexture(earlier.Pack, earlier.Color);
            }
            if (set.Pack == StoreKit.TexturePack || set.Pack == StoreKit.FallbackTexturePack)
                ok &= set.Ms == null && m.GetTexture("_MetallicGlossMap") == null && !m.IsKeywordEnabled("_METALLICGLOSSMAP");
        }
        DemoValidation.Info("Palette materials with a texture set: " + sourced + " of " + named);
        DemoValidation.Check(ok && named > 0, "Palette sets come from one pack each, in the documented priority");
    }

    // Palette tiling follows each chosen set's real-world tile (metric UVs: scale = 1 / tile),
    // with the documented sizes spot-checked when the PBR sets are present.
    private static void PaletteTilingChecks()
    {
        bool ok = true;
        foreach (KeyValuePair<Material, string> entry in StoreKit.PaletteTextures)
            if (StoreKit.PaletteSet(entry.Key, out StoreKit.SurfaceSet set))
                ok &= TileScale(entry.Key, 1 / set.Tile, 1 / (set.TileV > 0 ? set.TileV : set.Tile));
        const string interior = StoreKit.PbrInteriorPack, exterior = StoreKit.PbrExteriorPack;
        int spot = 0;
        if (DemoAssetLibrary.HasTexture(interior, "FloorTile_Color.jpg")) { ok &= TileScale(StoreKit.Floor, 1 / 2.4f, 1 / 2.4f); spot++; }
        if (DemoAssetLibrary.HasTexture(interior, "PlasterWarm_Color.jpg")) { ok &= TileScale(StoreKit.Plaster, 0.5f, 1); spot++; }
        if (DemoAssetLibrary.HasTexture(exterior, "AsphaltWet_Color.jpg")) { ok &= TileScale(StoreKit.Asphalt, 1 / 3f, 1 / 3f); spot++; }
        if (DemoAssetLibrary.HasTexture(exterior, "BrickFacade_Color.jpg")) { ok &= TileScale(StoreKit.Brick, 1 / 3f, 1 / 3f); spot++; }
        if (DemoAssetLibrary.HasTexture(exterior, "MetalShutter_Color.jpg")) { ok &= TileScale(StoreKit.Shutter, 1, 1); spot++; }
        DemoValidation.Info("Palette tile sizes spot-checked: " + spot + " of 5 (the rest need PBRInterior/PBRExterior)");
        DemoValidation.Check(ok, "Palette tiling follows each set's real-world tile size");
    }

    private static bool TileScale(Material m, float u, float v)
        => m != null && Mathf.Abs(m.mainTextureScale.x - u) < 1e-4f && Mathf.Abs(m.mainTextureScale.y - v) < 1e-4f;

    // Brick, shutter and stainless have one PBR set each: they take it when its colour file
    // exists and otherwise stay untextured at their scalar finish (no map of any kind).
    private static void PaletteFallbackChecks()
    {
        bool ok = true;
        Dictionary<Material, string> names = new Dictionary<Material, string>();
        foreach (KeyValuePair<Material, string> entry in StoreKit.PaletteTextures) names[entry.Key] = entry.Value;
        foreach (Material m in new[] { StoreKit.Brick, StoreKit.Shutter, StoreKit.Stainless })
        {
            string texture = null;
            StoreKit.SurfaceSet[] sets = m != null && names.TryGetValue(m, out texture) ? StoreKit.PaletteSets(texture) : new StoreKit.SurfaceSet[0];
            bool pbr = sets.Length == 1 && (sets[0].Pack == StoreKit.PbrInteriorPack || sets[0].Pack == StoreKit.PbrExteriorPack);
            bool present = pbr && DemoAssetLibrary.HasTexture(sets[0].Pack, sets[0].Color);
            bool textured = StoreKit.PaletteSet(m, out StoreKit.SurfaceSet set);
            ok &= pbr && textured == present;
            if (textured) ok &= sets.Length > 0 && set.Pack == sets[0].Pack && m.mainTexture != null;
            else ok &= m != null && m.mainTexture == null && m.GetTexture("_BumpMap") == null && m.GetTexture("_MetallicGlossMap") == null
                && m.GetTexture("_OcclusionMap") == null && !m.IsKeywordEnabled("_NORMALMAP") && !m.IsKeywordEnabled("_METALLICGLOSSMAP");
        }
        DemoValidation.Info("Brick/shutter/stainless sets: " + (StoreKit.PaletteSource(StoreKit.Brick) ?? "scalar") + ", "
            + (StoreKit.PaletteSource(StoreKit.Shutter) ?? "scalar") + ", " + (StoreKit.PaletteSource(StoreKit.Stainless) ?? "scalar"));
        DemoValidation.Check(ok, "Brick, shutter and stainless use their PBR sets and keep a scalar fallback");
    }

    // A 2 x 1 x 0.5 box: its UVs span 2 by 1 units, the largest face's world edges.
    private static bool MetricUVs()
    {
        GameObject box = StoreKit.MetricBox("UVProbe", null, Vector3.zero, new Vector3(2, 1, 0.5f), StoreKit.Walnut);
        try
        {
            Vector2[] uv = box.GetComponent<MeshFilter>().sharedMesh.uv;
            if (uv.Length == 0) return false;
            Vector2 min = uv[0], max = uv[0];
            foreach (Vector2 p in uv) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            Vector2 span = max - min;
            return Mathf.Abs(span.x - 2) < 0.001f && Mathf.Abs(span.y - 1) < 0.001f;
        }
        finally { UnityEngine.Object.DestroyImmediate(box); }
    }

    private static bool LightmapUVs(Transform gallery)
    {
        bool ok = true;
        foreach (MeshFilter filter in gallery.GetComponentsInChildren<MeshFilter>(true))
        {
            Vector2[] uv2 = filter.sharedMesh != null ? filter.sharedMesh.uv2 : new Vector2[0];
            bool nonZero = false;
            foreach (Vector2 p in uv2)
            {
                ok &= p.x >= -1e-4f && p.y >= -1e-4f && p.x <= 1.0001f && p.y <= 1.0001f;
                nonZero |= p.sqrMagnitude > 1e-8f;
            }
            ok &= uv2.Length > 0 && nonZero;
        }
        return ok;
    }

    public static int Triangles(Transform root)
    {
        int triangles = 0;
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue;
            for (int s = 0; s < mesh.subMeshCount; s++) triangles += (int)mesh.GetIndexCount(s) / 3;
        }
        return triangles;
    }

    // A leaf's mesh must start at its pivot: turning it 90 degrees about local y keeps
    // the hinge edge in place (within 1 cm) and swings the free edge.
    private static void HingeChecks(Transform gallery)
    {
        string[] hinged = { "safe_large", "safe_large__Closed", "safe_tall", "office_door", "service_door", "storefront_doors", "storefront_doors__BrokenOpen" };
        bool pivots = true;
        foreach (string name in hinged)
        {
            Transform entry = gallery.Find(name);
            List<Transform> leaves = new List<Transform>();
            if (entry != null)
                foreach (MeshFilter filter in entry.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.name.StartsWith("Door")) leaves.Add(filter.transform);
            pivots &= leaves.Count == (name.StartsWith("storefront") ? 2 : 1);
            foreach (Transform leaf in leaves)
            {
                Bounds b = leaf.GetComponent<MeshFilter>().sharedMesh.bounds;
                bool hingeAtMin = Mathf.Abs(b.min.x) < Mathf.Abs(b.max.x);
                Vector3 hinge = new Vector3(hingeAtMin ? b.min.x : b.max.x, b.center.y, 0);
                Vector3 free = new Vector3(hingeAtMin ? b.max.x : b.min.x, b.center.y, 0);
                Vector3 hinge0 = leaf.TransformPoint(hinge), free0 = leaf.TransformPoint(free);
                Quaternion saved = leaf.localRotation;
                leaf.localRotation = saved * Quaternion.Euler(0, 90, 0);
                float hingeMoved = Vector3.Distance(leaf.TransformPoint(hinge), hinge0);
                float freeMoved = Vector3.Distance(leaf.TransformPoint(free), free0);
                leaf.localRotation = saved;
                pivots &= hingeMoved <= 0.01f && freeMoved > 0.3f;
            }
        }
        DemoValidation.Check(pivots, "Safe and door leaves pivot at their hinges");
    }

    private static void PaneChecks(Transform gallery)
    {
        Transform intact = gallery.Find("display_case"), broken = gallery.Find("display_case__FrontBroken");
        Transform shards = broken != null ? broken.Find("Shards") : null;
        bool panes = intact != null && broken != null && Panes(intact) == 5 && Panes(broken) == 4
            && broken.Find("Glass_Front") == null && shards != null && Triangles(shards) > 0;
        DemoValidation.Check(panes, "Display case glass panes are separate children");
    }

    // Root colliders follow the open parts: the deck of a case with its glass gone, the
    // inside of an open safe, and facade frames and intact glass that stop rays.
    private static void ColliderChecks(Transform gallery)
    {
        Physics.SyncTransforms();
        bool reach = Hits(gallery, "display_case__AllBroken", new Vector3(0, 2, 0), Vector3.down, out Vector3 deck) && Mathf.Abs(deck.y - 0.70f) < 0.02f;
        reach &= Hits(gallery, "display_case__FrontBroken", new Vector3(0, 0.88f, -1.5f), Vector3.forward, out Vector3 back) && back.z > 0.2f;
        reach &= Hits(gallery, "safe_large", new Vector3(0, 0.35f, -1.5f), Vector3.forward, out Vector3 lining) && lining.z > 0.3f;
        reach &= Hits(gallery, "display_case", new Vector3(0, 0.88f, -1.5f), Vector3.forward, out Vector3 front) && front.z < -0.2f;
        DemoValidation.Check(reach, "Case decks and the open safe's interior are reachable; intact glass blocks");

        bool facade = true;
        foreach (string name in new[] { "window_module", "storefront_doors", "office_door", "service_door" })
        {
            Transform entry = gallery.Find(name);
            facade &= entry != null && entry.GetComponentsInChildren<Collider>().Length > 0;
        }
        facade &= Hits(gallery, "window_module", new Vector3(0, 1.5f, -1.5f), Vector3.forward, out Vector3 pane) && Mathf.Abs(pane.z) < 0.02f;
        facade &= !Hits(gallery, "window_module__Broken", new Vector3(0.1f, 1.345f, -1.5f), Vector3.forward, out _);
        DemoValidation.Check(facade, "Facade bays and door frames carry colliders; intact window glass blocks rays");
    }

    // Ray in the entry's local space; true when the nearest hit within 3 m belongs to it (point in its local space).
    private static bool Hits(Transform gallery, string name, Vector3 origin, Vector3 direction, out Vector3 local)
    {
        local = Vector3.zero;
        Transform entry = gallery.Find(name);
        if (entry == null) return false;
        Ray ray = new Ray(entry.TransformPoint(origin), entry.TransformDirection(direction));
        if (!Physics.Raycast(ray, out RaycastHit hit, 3, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
        if (!hit.transform.IsChildOf(entry)) return false;
        local = entry.InverseTransformPoint(hit.point);
        return true;
    }

    // Ceiling pieces hang entirely at or below their pivot (the ceiling plane).
    private static void CeilingChecks(Transform gallery)
    {
        bool below = true;
        foreach (string name in new[] { "track_light", "downlight" })
        {
            Transform entry = gallery.Find(name);
            below &= entry != null && DemoAssetLibrary.LocalBounds(entry.gameObject).max.y <= 0.001f;
        }
        DemoValidation.Check(below, "Ceiling lights stay at or below the ceiling plane");
    }

    // Wall devices (PIR, switch, socket) pivot at the bottom centre of their back: the back
    // lies on z = 0, the bottom on y = 0, and they stand at most 6 cm proud. Ceiling devices
    // (dome camera, smoke detector) touch the ceiling plane, hang at most 8 cm below it and
    // are centred on their pivot. None carries a collider (they are probe-lit wall dressing).
    private static void DeviceChecks(Transform gallery)
    {
        bool ok = true;
        foreach (string name in new[] { "pir_detector", "light_switch", "socket_outlet", "cctv_dome", "smoke_detector" })
        {
            Transform entry = gallery.Find(name);
            Transform body = entry != null ? entry.Find("Body") : null;
            if (body == null || body.GetComponent<MeshFilter>() == null) { ok = false; continue; }
            Bounds b = DemoAssetLibrary.LocalBounds(entry.gameObject);
            bool ceiling = name == "cctv_dome" || name == "smoke_detector";
            ok &= entry.GetComponentsInChildren<Collider>(true).Length == 0;
            if (ceiling)
                ok &= Mathf.Abs(b.max.y) <= 0.001f && b.size.y <= 0.08f && Mathf.Abs(b.center.x) <= 0.002f && Mathf.Abs(b.center.z) <= 0.002f;
            else
                ok &= Mathf.Abs(b.max.z) <= 0.001f && b.min.z >= -0.06f && Mathf.Abs(b.min.y) <= 0.001f && Mathf.Abs(b.center.x) <= 0.002f;
            DemoValidation.Info(name + " bounds min=" + b.min.ToString("F4") + " max=" + b.max.ToString("F4") + " triangles=" + Triangles(entry));
        }
        DemoValidation.Check(ok, "Wall devices pivot at the bottom centre of their back, at most 6 cm proud; ceiling devices hang at or below the ceiling plane");
    }

    private static int Panes(Transform fixture)
    {
        int panes = 0;
        foreach (Transform child in fixture)
            if (child.name.StartsWith("Glass_") && child.GetComponent<MeshRenderer>() != null) panes++;
        return panes;
    }

    // Every emitter is baked (no realtime emissive GI), and fixture lenses, shelf strips
    // and screens start dark so the night scene is lit only by what it switches on.
    private static void EmissionChecks(Transform gallery)
    {
        bool baked = true;
        foreach (MeshRenderer renderer in gallery.GetComponentsInChildren<MeshRenderer>(true))
            foreach (Material m in renderer.sharedMaterials)
                if (m != null && m.IsKeywordEnabled("_EMISSION"))
                    baked &= (m.globalIlluminationFlags & MaterialGlobalIlluminationFlags.BakedEmissive) != 0;
        foreach (Material m in new[] { StoreKit.LightLens, StoreKit.StripLight, StoreKit.DeviceScreen })
            baked &= m.GetColor("_EmissionColor").maxColorComponent == 0;
        baked &= StoreKit.ExitGreen.GetColor("_EmissionColor").maxColorComponent > 0;
        DemoValidation.Check(baked, "Store kit emitters are baked and fixture lights start off");
    }

    // Case panes are 10 mm and the top pane lies on the rails: its upper face is the case's
    // 1.05 m top, nothing bronze reaches above the rails' 1.0395, and an opaque green edge
    // band (StoreKit.GlassEdge) runs 0.1-1 mm outside all four of its sides between its faces.
    // The all-broken case has neither band nor panes.
    private static void CaseGlassChecks(Transform gallery)
    {
        bool ok = true;
        List<string> lines = new List<string>();
        foreach (string name in new[] { "display_case", "wall_case" })
        {
            Transform entry = gallery.Find(name), body = entry != null ? entry.Find("Body") : null, top = entry != null ? entry.Find("Glass_Top") : null;
            Mesh front = FixtureMesh(entry, "Glass_Front"), left = FixtureMesh(entry, "Glass_Left"), lid = FixtureMesh(entry, "Glass_Top");
            if (body == null || front == null || left == null || lid == null)
            {
                ok = false;
                lines.Add(name + " lacks its Body or panes");
                continue;
            }
            float topY = top.localPosition.y + lid.bounds.max.y, bronze = 0;
            ok &= FixtureWithin(front.bounds.size.z, 0.0095f, 0.0105f) && FixtureWithin(left.bounds.size.x, 0.0095f, 0.0105f)
                && FixtureWithin(lid.bounds.size.y, 0.0095f, 0.0105f) && FixtureWithin(topY, 1.049f, 1.0505f);
            foreach (Vector3 v in FixtureVertices(body, StoreKit.Bronze)) bronze = Mathf.Max(bronze, v.y);
            ok &= bronze > 0 && bronze <= 1.0396f;
            // The band in entry space against the top pane's box in entry space.
            Bounds pane = lid.bounds;
            pane.center = entry.InverseTransformPoint(top.TransformPoint(pane.center));
            List<Vector3> band = FixtureVertices(body, StoreKit.GlassEdge);
            float near = float.MaxValue, far = 0;
            foreach (Vector3 p in band)
            {
                Vector3 v = entry.InverseTransformPoint(body.TransformPoint(p));
                float outside = Mathf.Max(Mathf.Abs(v.x - pane.center.x) - pane.extents.x, Mathf.Abs(v.z - pane.center.z) - pane.extents.z);
                near = Mathf.Min(near, outside);
                far = Mathf.Max(far, outside);
                ok &= FixtureWithin(v.y, 1.040f, 1.050f);
            }
            ok &= FixtureTriangles(body, StoreKit.GlassEdge).Count >= 8 && band.Count > 0 && near >= 0.0001f && far <= 0.001f;
            lines.Add(name + ": front " + front.bounds.size.z.ToString("0.0000") + ", side " + left.bounds.size.x.ToString("0.0000") + ", top "
                + lid.bounds.size.y.ToString("0.0000") + " to " + topY.ToString("0.0000") + ", bronze up to " + bronze.ToString("0.0000") + ", band "
                + (near * 1000).ToString("0.00") + "-" + (far * 1000).ToString("0.00") + " mm outside");
        }
        Material edge = StoreKit.GlassEdge;
        ok &= edge != null && edge.renderQueue < 2500 && !edge.IsKeywordEnabled("_ALPHABLEND_ON") && !edge.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON")
            && edge.color.g > edge.color.r && edge.color.g >= edge.color.b;
        Transform broken = gallery.Find("display_case__AllBroken");
        ok &= broken != null && broken.Find("Body") != null && FixtureVertices(broken.Find("Body"), StoreKit.GlassEdge).Count == 0;
        if (broken != null) foreach (Transform child in broken) ok &= !child.name.StartsWith("Glass_");
        DemoValidation.Info("Case glass: " + string.Join("; ", lines));
        DemoValidation.Check(ok, "Case panes are 10 mm and the top pane rests on the rails, its green edge band opaque on all four sides");
    }

    // Every case and counter Body is free of coplanar overlapping faces (DemoValidation.
    // CoplanarOverlap over all its submeshes, in mesh space): before the detail round the
    // posts and rails shared outer faces and tops at the case corners, and the framed panels'
    // stile tops overlapped the carcass top.
    private static void FrameJointChecks(Transform gallery)
    {
        bool ok = true;
        List<string> lines = new List<string>();
        foreach (string name in new[] { "display_case", "display_case__FrontBroken", "display_case__AllBroken", "display_case__Drawer", "wall_case", "wall_case__AllBroken", "counter" })
        {
            Mesh mesh = FixtureMesh(gallery.Find(name), "Body");
            if (mesh == null)
            {
                ok = false;
                lines.Add(name + " has no Body");
                continue;
            }
            bool overlap = DemoValidation.CoplanarOverlap(new List<Vector3>(mesh.vertices), new List<int>(mesh.triangles), 0.00005f, 0.0002f, out int a, out int b);
            if(overlap) {var v=mesh.vertices;var t=mesh.triangles;DemoValidation.Info("Overlap coordinates "+name+" A="+v[t[a*3]]+","+v[t[a*3+1]]+","+v[t[a*3+2]]+" B="+v[t[b*3]]+","+v[t[b*3+1]]+","+v[t[b*3+2]]);} ok &= !overlap;
            lines.Add(name + (overlap ? ": triangles " + a + " / " + b : ": none"));
        }
        DemoValidation.Info("Frame joints (coplanar overlaps): " + string.Join("; ", lines));
        DemoValidation.Check(ok, "Case and counter bodies have no coplanar overlapping faces, so their frame joints cannot z-fight");
    }

    // The detail lives in the parts' own meshes: each entry keeps exactly these renderers.
    private static readonly Dictionary<string, string> FixtureParts = new Dictionary<string, string>
    {
        { "display_case", "Body,Glass_Back,Glass_Front,Glass_Left,Glass_Right,Glass_Top" },
        { "display_case__FrontBroken", "Body,Glass_Back,Glass_Left,Glass_Right,Glass_Top,Shards" },
        { "display_case__AllBroken", "Body,Shards" },
        { "display_case__Drawer", "Body,Drawer,Glass_Back,Glass_Front,Glass_Left,Glass_Right,Glass_Top" },
        { "wall_case", "Body,Glass_Front,Glass_Left,Glass_Right,Glass_Top" },
        { "wall_case__AllBroken", "Body,Shards" },
        { "counter", "Body" }, { "safe_large", "Body,Door" }, { "safe_large__Closed", "Body,Door" }, { "safe_tall", "Body,Door" },
        { "office_door", "Door,Frame" }, { "service_door", "Door,Frame" },
        { "storefront_doors", "DoorLeft,DoorRight,Frame,Glass,Glass" }, { "storefront_doors__BrokenOpen", "DoorLeft,DoorRight,Frame,Glass,Glass,Shards" },
        { "window_module", "Frame,Glass" }, { "window_module__Cracked", "Cracks,Frame,Glass" }, { "window_module__Broken", "Frame,Glass,Shards" },
        { "security_camera", "Body" }
    };

    private static void FixturePartChecks(Transform gallery)
    {
        List<string> wrong = new List<string>();
        foreach (KeyValuePair<string, string> part in FixtureParts)
        {
            Transform entry = gallery.Find(part.Key);
            List<string> names = new List<string>();
            if (entry != null) foreach (MeshRenderer r in entry.GetComponentsInChildren<MeshRenderer>(true)) names.Add(r.name);
            names.Sort(StringComparer.Ordinal);
            string got = string.Join(",", names);
            if (entry == null || got != part.Value) wrong.Add(part.Key + " has " + (entry == null ? "no entry" : got));
        }
        DemoValidation.Info("Fixture parts: " + (wrong.Count == 0 ? "every entry as listed" : string.Join("; ", wrong)));
        DemoValidation.Check(wrong.Count == 0, "Fixture detail stays merged: cases, safes, doors, bays and the counter keep their renderers and part names");
    }

    // Each safe's left jamb carries three 30 mm dark receivers (Body DeviceBlack, 6 triangles,
    // facing +x, 0.5 mm off the jamb's inner face at -(w/2 - 0.09)), one at the height of each
    // of the door's three bolts: the door's bronze vertices within 46 mm of its free end. Both
    // are grouped by height (no triangle order is assumed), each group's middle being its
    // (min + max) / 2; the door shares the body's y.
    private static void SafeBoltChecks(Transform gallery)
    {
        bool ok = true;
        List<string> lines = new List<string>();
        foreach (string name in new[] { "safe_large", "safe_large__Closed", "safe_tall" })
        {
            Transform entry = gallery.Find(name), body = entry != null ? entry.Find("Body") : null, door = entry != null ? entry.Find("Door") : null;
            Mesh shell = FixtureMesh(entry, "Body"), leaf = FixtureMesh(entry, "Door");
            if (shell == null || leaf == null)
            {
                ok = false;
                lines.Add(name + " lacks its Body or Door");
                continue;
            }
            float jamb = -(shell.bounds.size.x * 0.5f - 0.09f) + 0.0005f;
            List<KeyValuePair<Vector3, Vector3>> receivers = FixtureTriangles(body, StoreKit.DeviceBlack);
            foreach (KeyValuePair<Vector3, Vector3> t in receivers) ok &= t.Value.x > 0.99f;
            List<float> dark = new List<float>(), heights = new List<float>();
            foreach (Vector3 v in FixtureVertices(body, StoreKit.DeviceBlack))
            {
                ok &= Mathf.Abs(v.x - jamb) <= 0.001f;
                dark.Add(v.y);
            }
            foreach (Vector3 v in FixtureVertices(door, StoreKit.Bronze)) if (v.x < leaf.bounds.min.x + 0.046f) heights.Add(v.y);
            List<float> centres = FixtureGroups(dark, 0.05f), bolts = FixtureGroups(heights, 0.1f);
            ok &= receivers.Count == 6 && centres.Count == 3 && bolts.Count == 3;
            for (int i = 0; i < Mathf.Min(centres.Count, bolts.Count); i++) ok &= Mathf.Abs(centres[i] - bolts[i]) <= 0.003f;
            lines.Add(name + ": receivers " + string.Join("/", centres.ConvertAll(y => y.ToString("0.000"))) + ", bolts " + string.Join("/", bolts.ConvertAll(y => y.ToString("0.000"))));
        }
        DemoValidation.Info("Safe bolts: " + string.Join("; ", lines));
        DemoValidation.Check(ok, "Safe bolt receivers on the jamb line up with the door's three bolts");
    }

    // Case locks face the staff side (+z on the island case, -z on the wall case): 8+ dark
    // Body triangles facing along z (three keyways and the cupboard's meeting line), all
    // facing and lying on that side. The LED diffusers (Body LightLens: 4 on the island case,
    // 2 on the wall case) face down inside the glass, under the rails.
    private static void CaseHardwareChecks(Transform gallery)
    {
        bool ok = true;
        List<string> lines = new List<string>();
        foreach (string name in new[] { "display_case", "wall_case" })
        {
            Transform entry = gallery.Find(name), body = entry != null ? entry.Find("Body") : null;
            int staff = name == "display_case" ? 1 : -1, locks = 0;
            float w = 1.8f, d = name == "display_case" ? 0.65f : 0.6f;
            foreach (KeyValuePair<Vector3, Vector3> t in FixtureTriangles(body, StoreKit.DeviceBlack))
            {
                if (Mathf.Abs(t.Value.z) <= 0.99f) continue;
                locks++;
                ok &= Mathf.Sign(t.Value.z) == staff && Mathf.Sign(t.Key.z) == staff;
            }
            List<KeyValuePair<Vector3, Vector3>> lenses = FixtureTriangles(body, StoreKit.LightLens);
            ok &= body != null && locks >= 8 && lenses.Count == (staff > 0 ? 4 : 2);
            List<string> at = new List<string>();
            foreach (KeyValuePair<Vector3, Vector3> t in lenses)
            {
                Vector3 c = t.Key;
                ok &= t.Value.y < -0.99f && Mathf.Abs(c.x) < (w - 0.11f) * 0.5f && Mathf.Abs(c.z) < d * 0.5f - 0.0475f && c.y > 0.71f && c.y < 1.025f;
                at.Add(c.ToString("F3"));
            }
            lines.Add(name + ": " + locks + " staff-side lock triangles, " + lenses.Count + " diffuser triangles at " + string.Join(" ", at));
        }
        DemoValidation.Info("Case hardware: " + string.Join("; ", lines));
        DemoValidation.Check(ok, "Case locks face the staff side and the LED diffusers face down inside the glass");
    }

    // Each intact pane (a "Glass" child with 12 triangles; broken panes are skipped) of the
    // windows and doors: no vertex of the part holding it (the Frame for a window, its leaf
    // for a door) lies inside the pane shrunk by 0.5 mm. Within the pane's outline grown by
    // 1 mm, measured outward from each face in the pane's space (+z is the shop): a window
    // Frame has 16+ frame-metal vertices 0-16 mm off the shop face (the beads) and 8+ 0-1 mm
    // in front of the street face (the gasket); a door leaf has 16+ vertices 0-13 mm off each
    // face (the beads).
    private static void GlazingChecks(Transform gallery)
    {
        bool ok = true;
        List<string> lines = new List<string>();
        foreach (string name in new[] { "window_module", "window_module__Cracked", "storefront_doors", "storefront_doors__BrokenOpen" })
        {
            Transform entry = gallery.Find(name);
            bool window = name.StartsWith("window");
            int intact = 0;
            if (entry == null) { ok = false; continue; }
            foreach (MeshFilter glass in entry.GetComponentsInChildren<MeshFilter>(true))
            {
                if (glass.name != "Glass" || glass.sharedMesh == null || glass.sharedMesh.triangles.Length != 36) continue;
                intact++;
                Transform pane = glass.transform, holder = window ? entry.Find("Frame") : pane.parent;
                MeshFilter filter = holder != null ? holder.GetComponent<MeshFilter>() : null;
                MeshRenderer renderer = holder != null ? holder.GetComponent<MeshRenderer>() : null;
                if (filter == null || renderer == null || filter.sharedMesh == null) { ok = false; continue; }
                Bounds g = glass.sharedMesh.bounds;
                Mesh mesh = filter.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                Material[] materials = renderer.sharedMaterials;
                int inside = 0, shop = 0, street = 0;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    bool metal = s < materials.Length && materials[s] == StoreKit.FrameMetal;
                    foreach (int i in new HashSet<int>(mesh.GetTriangles(s)))
                    {
                        Vector3 v = pane.InverseTransformPoint(holder.TransformPoint(vertices[i]));
                        if (v.x > g.min.x + 0.0005f && v.x < g.max.x - 0.0005f && v.y > g.min.y + 0.0005f && v.y < g.max.y - 0.0005f
                            && v.z > g.min.z + 0.0005f && v.z < g.max.z - 0.0005f) inside++;
                        if (v.x < g.min.x - 0.001f || v.x > g.max.x + 0.001f || v.y < g.min.y - 0.001f || v.y > g.max.y + 0.001f) continue;
                        float shopSide = v.z - g.max.z, streetSide = g.min.z - v.z;
                        if (window)
                        {
                            if (metal && FixtureWithin(shopSide, -0.0005f, 0.016f)) shop++;
                            if (metal && FixtureWithin(streetSide, 0, 0.001f)) street++;
                        }
                        else
                        {
                            if (FixtureWithin(shopSide, -0.0005f, 0.013f)) shop++;
                            if (FixtureWithin(streetSide, -0.0005f, 0.013f)) street++;
                        }
                    }
                }
                ok &= inside == 0 && shop >= 16 && street >= (window ? 8 : 16);
                lines.Add(name + "/" + (window ? "" : holder.name + "/") + "Glass: inside " + inside + ", shop side " + shop + ", street side " + street);
            }
            ok &= intact == (name == "storefront_doors" ? 2 : 1);
        }
        DemoValidation.Info("Glazing: " + string.Join("; ", lines));
        DemoValidation.Check(ok, "Glazing beads, caps and gaskets frame the door and window glass without entering it");
    }

    // Door contacts: the centroid of each part's DeviceGrey vertices (the storefront frame's
    // split into its two contacts by the side of the entry they are on). A closed leaf's
    // magnet lies within 5 cm of the nearest frame contact; the forced-open left leaf of
    // storefront_doors__BrokenOpen (35 degrees) has moved its magnet at least 0.25 m away.
    private static void ContactChecks(Transform gallery)
    {
        bool ok = true;
        List<string> lines = new List<string>();
        foreach (string name in new[] { "office_door", "service_door", "storefront_doors", "storefront_doors__BrokenOpen" })
        {
            Transform entry = gallery.Find(name), frame = entry != null ? entry.Find("Frame") : null;
            List<Vector3> grey = FixtureVertices(frame, StoreKit.DeviceGrey, true), contacts = new List<Vector3>();
            if (name.StartsWith("storefront"))
            {
                List<Vector3> left = grey.FindAll(v => entry.InverseTransformPoint(v).x < 0), right = grey.FindAll(v => entry.InverseTransformPoint(v).x > 0);
                if (left.Count > 0) contacts.Add(FixtureCentroid(left));
                if (right.Count > 0) contacts.Add(FixtureCentroid(right));
                ok &= contacts.Count == 2;
            }
            else if (grey.Count > 0) contacts.Add(FixtureCentroid(grey));
            int leaves = 0;
            if (entry != null)
                foreach (Transform leaf in entry)
                {
                    if (!leaf.name.StartsWith("Door")) continue;
                    leaves++;
                    List<Vector3> magnet = FixtureVertices(leaf, StoreKit.DeviceGrey, true);
                    float gap = float.MaxValue;
                    foreach (Vector3 c in contacts) gap = Mathf.Min(gap, Vector3.Distance(c, FixtureCentroid(magnet)));
                    bool forced = name == "storefront_doors__BrokenOpen" && leaf.name == "DoorLeft";
                    ok &= magnet.Count > 0 && (forced ? gap >= 0.25f : gap <= 0.05f);
                    lines.Add(name + "/" + leaf.name + " " + gap.ToString("0.000") + " m");
                }
            ok &= contacts.Count > 0 && leaves == (name.StartsWith("storefront") ? 2 : 1);
        }
        DemoValidation.Info("Door contacts (magnet to nearest frame contact): " + string.Join(", ", lines));
        DemoValidation.Check(ok, "Door contacts pair up on closed leaves and part when a leaf is forced open");
    }

    // ---------------------------------------------------------------- fixture detail

    // The mesh of entry's direct child `part`, or null.
    private static Mesh FixtureMesh(Transform entry, string part)
    {
        Transform t = entry != null ? entry.Find(part) : null;
        MeshFilter filter = t != null ? t.GetComponent<MeshFilter>() : null;
        return filter != null ? filter.sharedMesh : null;
    }

    // The part's vertices in material m's submesh (each once), in the part's mesh space or,
    // with world, in world space; empty when the part does not use m.
    private static List<Vector3> FixtureVertices(Transform part, Material m, bool world = false)
    {
        List<Vector3> list = new List<Vector3>();
        MeshFilter filter = part != null ? part.GetComponent<MeshFilter>() : null;
        MeshRenderer renderer = part != null ? part.GetComponent<MeshRenderer>() : null;
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        int s = renderer != null ? Array.IndexOf(renderer.sharedMaterials, m) : -1;
        if (mesh == null || s < 0 || s >= mesh.subMeshCount) return list;
        Vector3[] vertices = mesh.vertices;
        foreach (int i in new HashSet<int>(mesh.GetTriangles(s))) list.Add(world ? part.TransformPoint(vertices[i]) : vertices[i]);
        return list;
    }

    // The part's triangles in material m's submesh as (centroid, unit normal) in mesh space,
    // in submesh order (a Face's two triangles are consecutive); the normal follows the winding.
    private static List<KeyValuePair<Vector3, Vector3>> FixtureTriangles(Transform part, Material m)
    {
        List<KeyValuePair<Vector3, Vector3>> list = new List<KeyValuePair<Vector3, Vector3>>();
        MeshFilter filter = part != null ? part.GetComponent<MeshFilter>() : null;
        MeshRenderer renderer = part != null ? part.GetComponent<MeshRenderer>() : null;
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        int s = renderer != null ? Array.IndexOf(renderer.sharedMaterials, m) : -1;
        if (mesh == null || s < 0 || s >= mesh.subMeshCount) return list;
        Vector3[] v = mesh.vertices;
        int[] t = mesh.GetTriangles(s);
        for (int i = 0; i + 2 < t.Length; i += 3)
        {
            Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
            list.Add(new KeyValuePair<Vector3, Vector3>((a + b + c) / 3, Vector3.Cross(b - a, c - a).normalized));
        }
        return list;
    }

    private static Vector3 FixtureCentroid(List<Vector3> points)
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 p in points) sum += p;
        return points.Count > 0 ? sum / points.Count : Vector3.zero;
    }

    private static bool FixtureWithin(float value, float min, float max) => value >= min && value <= max;

    // The middles ((min + max) / 2) of values' groups, in ascending order, splitting wherever
    // two neighbouring values differ by more than gap.
    private static List<float> FixtureGroups(List<float> values, float gap)
    {
        List<float> sorted = new List<float>(values), middles = new List<float>();
        sorted.Sort();
        for (int i = 0, start = 0; i < sorted.Count; i++)
        {
            if (i + 1 < sorted.Count && sorted[i + 1] - sorted[i] <= gap) continue;
            middles.Add((sorted[start] + sorted[i]) * 0.5f);
            start = i + 1;
        }
        return middles;
    }
}
#endif
