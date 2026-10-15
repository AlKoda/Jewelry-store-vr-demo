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
    public const int ExpectedFixtures = 26;

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
            DemoValidation.Info(entry.name + " triangles=" + triangles + " budget=" + budget);
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
}
#endif
