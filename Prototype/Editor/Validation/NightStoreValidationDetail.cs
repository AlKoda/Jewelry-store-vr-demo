#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using L = NightStoreLayout;

// The fixture-detail round as built: the small-metal finishes of the dressing, the case tops'
// glass edge, the walnut architraves and the skirting that butts into them, the shadow gap
// under the ceiling, the wall and ceiling devices, the corner camera square to its partition,
// the hidden cut cable and the street trees. Edit mode, from Generate after CreditChecks:
// NightStoreBuilder.Build has dressed, merged (MergeCaseGlass, CombineLoose) and flagged
// everything, NightLighting.Apply has set the lightmap scales, NightInterior.Graphics still
// holds this build's placements, and nothing is baked, batched or saved yet. Every measurement
// comes from the placed renderers, meshes, flags and materials. A check that needs a pack
// (InteriorModels' clock, extinguisher and diffusers, WallGraphics, StreetModels' shelter and
// racks) measures against it only when it was placed and says so in its Info line. File-local
// members carry the Detail prefix, so the other area files of this partial class never collide.
public static partial class NightStoreValidation
{
    private static void DetailChecks(Transform root)
    {
        DemoValidation.ClearRayMeshes();
        try
        {
            DetailHelpers();
            DetailMetals(root);
            DetailCaseEdges(root);
            DetailTrim(root);
            DetailShadowGap(root);
            DetailWallDevices(root);
            DetailCeilingDevices(root);
            DetailPirs(root);
            DetailCamera(root);
            DetailCutCable(root);
            DetailTrees(root);
        }
        finally { DemoValidation.ClearRayMeshes(); }
    }

    // ---------------------------------------------------------------- shared helpers

    // The DemoValidation geometry helpers these and the other area checks rely on, on shapes
    // with known answers: a 1 m quad on a temporary hidden object (turned and moved off the
    // store, destroyed again before anything is saved) hit square-on, missed beside it and
    // short of it, eager and lazy; the surface height over it laid flat; a box collider's world
    // box; footprint, point and segment distances; AppendWorldMesh's world corners and index
    // offset; and CoplanarOverlap on two quads in one plane near the partition's coordinates
    // that overlap by 10 mm (the only pair that counts), butt, overlap by 0.1 mm (under the
    // 0.2 mm minimum), lie 1 mm apart, or face opposite ways.
    private static void DetailHelpers()
    {
        GameObject probe = new GameObject("DetailHelperProbe") { hideFlags = HideFlags.HideAndDontSave };
        Mesh quad = new Mesh { name = "DetailHelperQuad", hideFlags = HideFlags.HideAndDontSave };
        List<string> wrong = new List<string>();
        try
        {
            quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quad.RecalculateBounds();
            MeshFilter filter = probe.AddComponent<MeshFilter>();
            filter.sharedMesh = quad;
            probe.AddComponent<MeshRenderer>();
            // Turned 90 degrees about y, the quad lies in the world plane x = 100 (y 49.5-50.5, z 99.5-100.5).
            probe.transform.SetPositionAndRotation(new Vector3(100, 50, 100), Quaternion.Euler(0, 90, 0));
            foreach (bool lazy in new[] { false, true })
            {
                string kind = lazy ? "lazy" : "eager";
                DemoValidation.RayMesh mesh = DemoValidation.NewRayMesh(filter, lazy);
                List<DemoValidation.RayMesh> list = new List<DemoValidation.RayMesh> { null, mesh };
                Ray square = new Ray(new Vector3(99, 50.2f, 100.1f), Vector3.right);
                if (!DemoValidation.TriangleRay(square, 2, list, out float d, out Vector3 n) || Mathf.Abs(d - 1) > 1e-4f || Mathf.Abs(Mathf.Abs(n.x) - 1) > 1e-4f)
                    wrong.Add(kind + " ray to the quad " + d.ToString("0.0000") + " m, normal " + n);
                if (DemoValidation.TriangleRay(new Ray(new Vector3(99, 50.6f, 100.1f), Vector3.right), 2, mesh, out d) || !float.IsNaN(d))
                    wrong.Add(kind + " ray beside the quad hits");
                if (DemoValidation.TriangleRay(square, 0.9f, mesh, out d)) wrong.Add(kind + " ray short of the quad hits");
            }
            BoxCollider box = probe.AddComponent<BoxCollider>();
            box.center = new Vector3(0.5f, 0, 0);
            box.size = new Vector3(1, 2, 4);
            Bounds held = DemoValidation.BoxColliderBounds(box);
            if ((held.center - new Vector3(100, 50, 99.5f)).magnitude > 1e-4f || (held.size - new Vector3(4, 2, 1)).magnitude > 1e-4f)
                wrong.Add("box collider bounds " + held);
            List<Vector3> corners = new List<Vector3> { Vector3.zero };
            List<int> indices = new List<int>();
            DemoValidation.AppendWorldMesh(filter, corners, indices);
            if (corners.Count != 5 || indices.Count != 6 || indices[0] != 1 || indices[2] != 2 || (corners[1] - probe.transform.TransformPoint(quad.vertices[0])).magnitude > 1e-5f)
                wrong.Add("AppendWorldMesh gave " + corners.Count + " corners, " + indices.Count + " indices");
            // Laid flat (turned 90 degrees about x), the quad lies in the world plane y = 50.
            probe.transform.rotation = Quaternion.Euler(90, 0, 0);
            float y = DemoValidation.SurfaceY(filter, new Vector3(100.2f, 50.1f, 99.8f)), off = DemoValidation.SurfaceY(filter, new Vector3(101, 50.1f, 100));
            if (Mathf.Abs(y - 50) > 1e-4f || !float.IsNaN(off)) wrong.Add("surface height " + y.ToString("0.0000") + ", beside it " + off);
            Bounds a = new Bounds(Vector3.zero, Vector3.one), b = new Bounds(new Vector3(3, 5, 4), Vector3.one);
            float gap = DemoValidation.FootprintGap(a, b), rectGap = DemoValidation.FootprintGap(DemoValidation.Footprint(a), DemoValidation.Footprint(b));
            if (Mathf.Abs(gap - Mathf.Sqrt(13)) > 1e-4f || Mathf.Abs(rectGap - gap) > 1e-4f || DemoValidation.FootprintGap(a, new Bounds(new Vector3(0.5f, 9, 0.5f), Vector3.one)) != 0)
                wrong.Add("footprint gaps " + gap.ToString("0.0000") + " / " + rectGap.ToString("0.0000"));
            Rect r = Rect.MinMaxRect(0, 0, 1, 1);
            if (DemoValidation.RectSegmentDistance(r, new Vector2(-1, 0.5f), new Vector2(2, 0.5f)) != 0
                || Mathf.Abs(DemoValidation.RectSegmentDistance(r, new Vector2(2, 0), new Vector2(2, 1)) - 1) > 1e-5f
                || Mathf.Abs(DemoValidation.RectSegmentDistance(r, new Vector2(2, 2), new Vector2(3, 3)) - Mathf.Sqrt(2)) > 1e-5f
                || Mathf.Abs(DemoValidation.PointSegmentDistance(new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0)) - 1) > 1e-5f
                || Mathf.Abs(DemoValidation.RectPointDistance(r, new Vector2(1.5f, 0.5f)) - 0.5f) > 1e-5f)
                wrong.Add("rect, segment or point distances");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probe);
            UnityEngine.Object.DestroyImmediate(quad);
        }
        // Quad B against the 1 m quad A, both in the plane of the partition's showroom face (z 8.44):
        // B's left edge 10 mm inside A, on A's edge or 0.1 mm inside it, or a 0.5 m overlap 1 mm
        // off the plane or facing the other way.
        string[] cases = { "10 mm overlap", "butt joint", "0.1 mm overlap", "1 mm apart", "facing away" };
        float[] starts = { 0.99f, 1, 0.9999f, 0.5f, 0.5f };
        List<string> found = new List<string>();
        for (int i = 0; i < cases.Length; i++)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            const float z = 8.44f;
            DetailQuad(vertices, triangles, new Vector3(-2, 0.5f, z), 1, 1, false);
            DetailQuad(vertices, triangles, new Vector3(-2 + starts[i], 0.5f, z - (i == 3 ? 0.001f : 0)), 1, 1, i == 4);
            bool overlap = DemoValidation.CoplanarOverlap(vertices, triangles, 0.00005f, 0.0002f, out int ta, out int tb);
            if (overlap) found.Add(cases[i] + " (triangles " + ta + " and " + tb + ")");
            if (overlap != (i == 0)) wrong.Add(cases[i] + (overlap ? " counted" : " missed"));
        }
        DemoValidation.Info("Geometry helpers: coplanar overlap found in " + (found.Count > 0 ? string.Join(", ", found) : "no case")
            + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(wrong.Count == 0, "Shared triangle-ray, footprint and coplanar helpers measure known shapes exactly");
    }

    // A w x h quad in the plane z = corner.z from its lower left corner, facing +z (-z when reversed).
    private static void DetailQuad(List<Vector3> vertices, List<int> triangles, Vector3 corner, float w, float h, bool reversed)
    {
        int start = vertices.Count;
        vertices.Add(corner);
        vertices.Add(corner + new Vector3(w, 0, 0));
        vertices.Add(corner + new Vector3(w, h, 0));
        vertices.Add(corner + new Vector3(0, h, 0));
        int[] order = reversed ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 };
        foreach (int k in order) triangles.Add(start + k);
    }

    // ---------------------------------------------------------------- finishes

    // Small metal and gloss parts keep a highlight twice as wide and a quarter as bright at its
    // peak as at 0.8, so it flickers far less on Quest 2 (the small-metal rule in
    // StoreKit.PropPalette): every material on the left-behind jewellery, the floor
    // scatter, the deck furniture and the drawer's tray (inactive renderers included) stays
    // at or below 0.72 smoothness, NightStoreDressing.Gem at or below 0.85. Blended materials
    // (the loupe lens, KitGlass) are exempt, and one with a packed metallic-smoothness map is
    // listed with its _GlossMapScale instead (the map carries its smoothness). The presenter
    // sets and the glass fans are not scanned. Runs with and without JewelryModels: StoreKit's
    // stand-ins carry KitGold.
    private static void DetailMetals(Transform root)
    {
        const float cap = 0.72f, gemCap = 0.85f;
        Transform dressing = root.Find("Dressing");
        string[] paths = { NightStoreDressing.StockGroup, NightStoreDressing.LooseGroup, NightStoreDressing.DisplaysGroup, "DrawerTray" };
        HashSet<Material> seen = new HashSet<Material>();
        List<string> scanned = new List<string>(), mapped = new List<string>(), wrong = new List<string>(), missing = new List<string>();
        foreach (string path in paths)
        {
            Transform group = dressing != null ? dressing.Find(path) : null;
            if (group == null)
            {
                // The drawer's tray exists only while CentralCase_Middle keeps its drawer.
                if (path != "DrawerTray") missing.Add("Dressing/" + path);
                continue;
            }
            foreach (Renderer r in group.GetComponentsInChildren<Renderer>(true))
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null || !seen.Add(m) || m.renderQueue >= 3000 || !m.HasProperty("_Glossiness")) continue;
                    if (m.IsKeywordEnabled("_METALLICGLOSSMAP"))
                    {
                        mapped.Add(m.name + " (_GlossMapScale " + (m.HasProperty("_GlossMapScale") ? m.GetFloat("_GlossMapScale").ToString("0.00") : "none") + ")");
                        continue;
                    }
                    float smoothness = m.GetFloat("_Glossiness"), limit = m == NightStoreDressing.Gem ? gemCap : cap;
                    scanned.Add(m.name + " " + smoothness.ToString("0.00"));
                    if (smoothness > limit + 1e-4f) wrong.Add(m.name + " " + smoothness.ToString("0.000") + " > " + limit.ToString("0.00"));
                }
        }
        scanned.Sort(StringComparer.Ordinal);
        mapped.Sort(StringComparer.Ordinal);
        DemoValidation.Info("Dressing finishes (smoothness): " + (scanned.Count > 0 ? string.Join(", ", scanned) : "none")
            + "; mapped, skipped: " + (mapped.Count > 0 ? string.Join(", ", mapped) : "none")
            + (missing.Count > 0 ? "; missing: " + string.Join(", ", missing) : "") + (wrong.Count > 0 ? "; too smooth: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(missing.Count == 0 && scanned.Count > 0 && wrong.Count == 0,
            "Dressing and jewellery finishes stay at or below 0.72 smoothness (gems 0.85), glass and mapped surfaces aside");
    }

    // ---------------------------------------------------------------- cases

    // Case tops show their green glass edge exactly where the top pane is intact. A case's top
    // is intact when its merged "Glass" (NightStoreBuilder.MergeCaseGlass) reaches 1.045 m
    // above the case; its Body then carries StoreKit.GlassEdge, and only then (the all-broken
    // RightIsland_Front has neither). Every band vertex lies at 1.040-1.050 m in the case's own
    // space (the knocked case is turned 98 degrees), and nothing on a case rises above
    // 1.052 m: the top pane ends at 1.05, the cracked top's strokes at 1.0505.
    private static void DetailCaseEdges(Transform root)
    {
        List<string> banded = new List<string>(), bare = new List<string>(), wrong = new List<string>();
        foreach (L.Fixture f in L.Fixtures)
        {
            if (f.Kit != "display_case" && f.Kit != "wall_case") continue;
            Transform fixture = root.Find("Showroom/" + f.Name);
            if (fixture == null) { wrong.Add(f.Name + " missing"); continue; }
            Transform glass = fixture.Find("Glass"), body = fixture.Find("Body");
            Renderer pane = glass != null ? glass.GetComponent<Renderer>() : null;
            MeshRenderer renderer = body != null ? body.GetComponent<MeshRenderer>() : null;
            MeshFilter filter = body != null ? body.GetComponent<MeshFilter>() : null;
            bool intact = pane != null && pane.bounds.max.y >= fixture.position.y + 1.045f;
            int s = renderer != null ? Array.IndexOf(renderer.sharedMaterials, StoreKit.GlassEdge) : -1;
            (s >= 0 ? banded : bare).Add(f.Name);
            if ((s >= 0) != intact) wrong.Add(f.Name + (intact ? " has an intact top but no band" : " has a band but no intact top"));
            if (s >= 0 && filter != null && filter.sharedMesh != null && s < filter.sharedMesh.subMeshCount)
            {
                Vector3[] vertices = filter.sharedMesh.vertices;
                float low = float.MaxValue, high = float.MinValue;
                foreach (int i in filter.sharedMesh.GetTriangles(s))
                {
                    float y = fixture.InverseTransformPoint(body.TransformPoint(vertices[i])).y;
                    low = Mathf.Min(low, y);
                    high = Mathf.Max(high, y);
                }
                if (low < 1.040f || high > 1.050f) wrong.Add(f.Name + " band at " + low.ToString("0.0000") + "-" + high.ToString("0.0000"));
            }
            if (!DemoAssetLibrary.WorldBounds(fixture.gameObject, out Bounds b)) wrong.Add(f.Name + " has no renderer");
            else if (b.max.y > fixture.position.y + 1.052f) wrong.Add(f.Name + " rises to " + (b.max.y - fixture.position.y).ToString("0.0000"));
        }
        DemoValidation.Info("Case edges: band on " + string.Join(", ", banded) + "; none on " + (bare.Count > 0 ? string.Join(", ", bare) : "no case")
            + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(banded.Count > 0 && wrong.Count == 0, "Case tops show their green glass edge exactly where the top pane is intact, at the cases' 1.05 m height");
    }

    // ---------------------------------------------------------------- trim

    // The walnut architraves frame the office opening on both faces of the partition (backs on
    // it, ArchitraveProud deep, lightmapped at the shell's density, no collider), never reach
    // into the clear opening, stay under the wall clock and short of the extinguisher; the
    // skirting stops at the legs' outer faces on the showroom face and has its own run on the
    // office face; the two meshes together have no coplanar overlap (z-fighting).
    private static void DetailTrim(Transform root)
    {
        const string label = "Walnut architraves frame the office opening on both faces and the skirting butts into them, with no coplanar overlap";
        Transform trim = root.Find("Architecture/" + NightStoreBuilder.ArchitravesName), skirting = root.Find("Architecture/Skirting");
        MeshFilter trimFilter = trim != null ? trim.GetComponent<MeshFilter>() : null, skirtingFilter = skirting != null ? skirting.GetComponent<MeshFilter>() : null;
        MeshRenderer trimRenderer = trim != null ? trim.GetComponent<MeshRenderer>() : null;
        if (trimFilter == null || trimFilter.sharedMesh == null || trimRenderer == null || skirtingFilter == null || skirtingFilter.sharedMesh == null)
        {
            DemoValidation.Info("Architraves or skirting missing under Architecture");
            DemoValidation.Check(false, label);
            return;
        }
        float half = L.OfficeOpeningHalf, legs = half + NightStoreBuilder.ArchitraveWidth, head = L.OfficeOpeningHeight + NightStoreBuilder.ArchitraveWidth;
        float proud = NightStoreBuilder.ArchitraveProud, showroom = NightInterior.ShowroomFace, office = NightInterior.OfficeFace;
        bool ok = trimRenderer.receiveGI == ReceiveGI.Lightmaps && Mathf.Abs(trimRenderer.scaleInLightmap - NightLighting.ShellScale) < 1e-4f
            && trim.GetComponentsInChildren<Collider>(true).Length == 0;
        float s0 = float.MaxValue, s1 = float.MinValue, o0 = float.MaxValue, o1 = float.MinValue;
        int opening = 0, outside = 0;
        Bounds box = new Bounds();
        bool first = true;
        Matrix4x4 toWorld = trim.localToWorldMatrix;
        foreach (Vector3 v in trimFilter.sharedMesh.vertices)
        {
            Vector3 p = toWorld.MultiplyPoint3x4(v);
            if (first) { box = new Bounds(p, Vector3.zero); first = false; }
            else box.Encapsulate(p);
            if (p.z < L.RearZ) { s0 = Mathf.Min(s0, p.z); s1 = Mathf.Max(s1, p.z); }
            else { o0 = Mathf.Min(o0, p.z); o1 = Mathf.Max(o1, p.z); }
            if (Mathf.Abs(p.x) < half - 0.0005f && p.y < L.OfficeOpeningHeight - 0.0005f) opening++;
            if (Mathf.Abs(p.x) > legs + 0.0005f || p.y < -0.0005f || p.y > head + 0.0005f) outside++;
        }
        bool faces = s0 <= s1 && o0 <= o1 && Mathf.Abs(s0 - (showroom - proud)) < 0.0005f && Mathf.Abs(s1 - showroom) < 0.0005f
            && Mathf.Abs(o0 - office) < 0.0005f && Mathf.Abs(o1 - (office + proud)) < 0.0005f;
        // The skirting beside the opening (the partition's runs, not the office side walls).
        float nearest = float.MaxValue;
        bool officeRun = false;
        Matrix4x4 skirtingToWorld = skirting.localToWorldMatrix;
        foreach (Vector3 v in skirtingFilter.sharedMesh.vertices)
        {
            Vector3 p = skirtingToWorld.MultiplyPoint3x4(v);
            if (Mathf.Abs(p.z - L.RearZ) >= 0.1f || Mathf.Abs(p.x) >= 2.9f) continue;
            nearest = Mathf.Min(nearest, Mathf.Abs(p.x));
            officeRun |= p.z >= office + 0.0145f;
        }
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        DemoValidation.AppendWorldMesh(trimFilter, vertices, triangles);
        DemoValidation.AppendWorldMesh(skirtingFilter, vertices, triangles);
        bool overlap = DemoValidation.CoplanarOverlap(vertices, triangles, 0.00005f, 0.0002f, out int a, out int b);
        // The clock and the extinguisher are InteriorModels props, measured when placed.
        string clear = "";
        Transform clock = root.Find("Showroom/" + NightInterior.ClockName), extinguisher = root.Find("Showroom/" + NightInterior.ExtinguisherName);
        if (clock != null && DemoAssetLibrary.WorldBounds(clock.gameObject, out Bounds c))
        {
            ok &= c.min.y >= box.max.y + 0.005f;
            clear += "; clock " + ((c.min.y - box.max.y) * 1000).ToString("0.0") + " mm above the head";
        }
        else clear += "; no wall clock placed";
        if (extinguisher != null && DemoAssetLibrary.WorldBounds(extinguisher.gameObject, out Bounds e))
        {
            ok &= e.min.x >= box.max.x + 0.02f;
            clear += "; extinguisher " + ((e.min.x - box.max.x) * 1000).ToString("0.0") + " mm beside the leg";
        }
        else clear += "; no extinguisher placed";
        DemoValidation.Info("Architraves: showroom face z " + s0.ToString("0.0000") + ".." + s1.ToString("0.0000") + ", office face z " + o0.ToString("0.0000") + ".."
            + o1.ToString("0.0000") + ", top " + box.max.y.ToString("0.000") + ", " + opening + " vertices in the opening, " + outside + " outside the frame; skirting by the opening from |x| "
            + nearest.ToString("0.000") + (officeRun ? ", office run present" : ", no office run") + (overlap ? "; coplanar overlap between triangles " + a + " and " + b : "") + clear);
        DemoValidation.Check(ok && faces && opening == 0 && outside == 0 && nearest >= legs - 0.0005f && officeRun && !overlap, label);
    }

    // The dark strip under the ceiling: DeviceBlack only, shadowless, probe-lit, batched but
    // never an occluder (it is under the ceiling the map looks through), every vertex in the
    // 15 mm under the slab and 2-4 mm on the room side of a showroom or office wall, and the
    // strips reach both side walls, the facade end and the office back wall.
    private static void DetailShadowGap(Transform root)
    {
        const string label = "A dark shadow gap runs round the showroom and office walls under the ceiling, probe-lit and casting two-sided";
        Transform gap = root.Find(L.CeilingPath + "/" + NightStoreBuilder.ShadowGapName);
        MeshRenderer r = gap != null ? gap.GetComponent<MeshRenderer>() : null;
        MeshFilter f = gap != null ? gap.GetComponent<MeshFilter>() : null;
        if (r == null || f == null || f.sharedMesh == null)
        {
            DemoValidation.Info("Shadow gap missing under " + L.CeilingPath);
            DemoValidation.Check(false, label);
            return;
        }
        StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(gap.gameObject);
        Material[] materials = r.sharedMaterials;
        bool ok = materials.Length == 1 && materials[0] == StoreKit.DeviceBlack && r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.TwoSided
            && r.receiveGI == ReceiveGI.LightProbes && (flags & StaticEditorFlags.BatchingStatic) != 0 && (flags & StaticEditorFlags.OccluderStatic) == 0;
        // Wall planes as (axis: 0 x, 2 z; position; the room side's sign).
        float ox = L.OfficeHalf - L.Partition * 0.5f;
        Vector3[] planes =
        {
            new Vector3(0, L.MinX, 1), new Vector3(0, L.MaxX, -1), new Vector3(2, NightInterior.ShowroomFace, -1),
            new Vector3(0, -ox, 1), new Vector3(0, ox, -1), new Vector3(2, L.MaxZ, -1), new Vector3(2, NightInterior.OfficeFace, 1)
        };
        Vector3[] vertices = f.sharedMesh.vertices;
        Matrix4x4 toWorld = gap.localToWorldMatrix;
        Bounds b = new Bounds();
        int stray = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = toWorld.MultiplyPoint3x4(vertices[i]);
            if (i == 0) b = new Bounds(p, Vector3.zero);
            else b.Encapsulate(p);
            bool high = p.y >= L.CeilingY - 0.016f && p.y <= L.CeilingY + 0.0001f, wall = false;
            foreach (Vector3 w in planes)
            {
                float d = w.z * (p[(int)w.x] - w.y);
                wall |= d >= 0.002f && d <= 0.004f;
            }
            if (!high || !wall) stray++;
        }
        bool reach = vertices.Length > 0 && b.min.x <= -7.99f && b.max.x >= 7.99f && b.min.z <= 0.08f && b.max.z >= 11.99f;
        DemoValidation.Info("Shadow gap: " + vertices.Length + " vertices, bounds x " + b.min.x.ToString("0.000") + ".." + b.max.x.ToString("0.000") + ", y "
            + b.min.y.ToString("0.000") + ".." + b.max.y.ToString("0.000") + ", z " + b.min.z.ToString("0.000") + ".." + b.max.z.ToString("0.000") + "; off the walls " + stray);
        DemoValidation.Check(ok && reach && stray == 0, label);
    }

    // ---------------------------------------------------------------- devices

    // Each wall device (the PIRs, switches, sockets and the office keypad): its back within
    // 3 mm of the wall behind it (a ray from 1 cm in front of the back plane along the device's
    // back), sockets above the skirting, clear of every placed wall graphic (its picture box
    // grown by the gilt bar for framed paintings) and of every other layout fixture by 1 cm,
    // and no collider on a procedural device.
    private static void DetailWallDevices(Transform root)
    {
        const string label = "Switches, sockets, PIR detectors and the office keypad sit on their walls, above the skirting and clear of the wall graphics and fixtures";
        Transform walls = root.Find("Architecture/Walls");
        MeshFilter wallFilter = walls != null ? walls.GetComponent<MeshFilter>() : null;
        if (wallFilter == null || wallFilter.sharedMesh == null)
        {
            DemoValidation.Info("Architecture/Walls has no mesh");
            DemoValidation.Check(false, label);
            return;
        }
        var wallMesh = DemoValidation.RayMeshOf(wallFilter);
        Dictionary<string, Bounds> fixtures = new Dictionary<string, Bounds>();
        foreach (L.Fixture f in L.Fixtures)
        {
            Transform t = root.Find(f.Zone + "/" + f.Name);
            if (t != null && DemoAssetLibrary.WorldBounds(t.gameObject, out Bounds fb)) fixtures[f.Name] = fb;
        }
        List<string> wrong = new List<string>(), gaps = new List<string>();
        int devices = 0;
        foreach (L.Fixture f in L.Fixtures)
        {
            if (f.Kit != "pir_detector" && f.Kit != "light_switch" && f.Kit != "socket_outlet" && f.Name != "AlarmKeypad_Office") continue;
            devices++;
            Transform device = root.Find(f.Zone + "/" + f.Name);
            if (device == null || !fixtures.TryGetValue(f.Name, out Bounds b))
            {
                wrong.Add(f.Name + " missing");
                continue;
            }
            Vector3 p = device.TransformPoint(0, 0.04f, 0), n = device.TransformDirection(Vector3.back);
            bool hit = DemoValidation.TriangleRay(new Ray(p + n * 0.01f, -n), 0.03f, wallMesh, out float d);
            gaps.Add(f.Name + " " + (hit ? ((d - 0.01f) * 1000).ToString("0.0") + " mm" : "no wall"));
            if (!hit || d < 0.007f || d > 0.013f) wrong.Add(f.Name + " off its wall");
            if (f.Kit == "socket_outlet" && b.min.y < 0.15f) wrong.Add(f.Name + " low");
            foreach (NightInterior.PlacedGraphic g in NightInterior.Graphics)
            {
                Vector3 normal = g.Normal.normalized;
                Vector3 size = Mathf.Abs(normal.x) > 0.5f ? new Vector3(0.1f, g.Size.y, g.Size.x)
                    : Mathf.Abs(normal.z) > 0.5f ? new Vector3(g.Size.x, g.Size.y, 0.1f) : new Vector3(g.Size.x, 0.1f, g.Size.y);
                Bounds picture = new Bounds(g.Centre, size);
                picture.Expand(2 * (0.02f + (g.Frame == NightInterior.GraphicFrame.Gilt ? NightInterior.GiltBar : 0)));
                if (picture.Intersects(b)) wrong.Add(f.Name + " over " + g.Name);
            }
            Bounds grown = b;
            grown.Expand(0.02f);
            foreach (KeyValuePair<string, Bounds> other in fixtures)
                if (other.Key != f.Name && grown.Intersects(other.Value)) wrong.Add(f.Name + " against " + other.Key);
            if (device.Find("Body") != null && device.GetComponentsInChildren<Collider>(true).Length > 0) wrong.Add(f.Name + " has a collider");
        }
        DemoValidation.Info("Wall devices: back gaps " + string.Join(", ", gaps) + "; " + NightInterior.Graphics.Count + " wall graphics placed (WallGraphics)"
            + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(devices == 11 && wrong.Count == 0, label);
    }

    // Each CCTV dome and smoke detector under the ceiling: its top on the slab's underside (a
    // ray up from its lowest point's height at its centre), at least 0.3 m in plan from every
    // downlight, track rail and ceiling diffuser (InteriorModels; the downlights and tracks
    // alone without it), no collider and nothing in GI.
    private static void DetailCeilingDevices(Transform root)
    {
        const string label = "CCTV domes and smoke detectors hang flush under the ceiling, clear of the downlights, tracks and diffusers";
        Transform ceiling = root.Find(L.CeilingPath);
        Transform slab = ceiling != null ? ceiling.Find("CeilingSlab") : null;
        MeshFilter slabFilter = slab != null ? slab.GetComponent<MeshFilter>() : null;
        if (slabFilter == null || slabFilter.sharedMesh == null)
        {
            DemoValidation.Info("Ceiling slab missing under " + L.CeilingPath);
            DemoValidation.Check(false, label);
            return;
        }
        var slabMesh = DemoValidation.RayMeshOf(slabFilter);
        List<Bounds> fittings = new List<Bounds>();
        List<string> names = new List<string>();
        int diffusers = 0;
        foreach (Transform t in ceiling)
            if ((t.name.StartsWith("Downlight_", StringComparison.Ordinal) || t.name.StartsWith("TrackLight_", StringComparison.Ordinal)
                || t.name.StartsWith(NightInterior.DiffuserPrefix, StringComparison.Ordinal)) && DemoAssetLibrary.WorldBounds(t.gameObject, out Bounds fb))
            {
                fittings.Add(fb);
                names.Add(t.name);
                if (t.name.StartsWith(NightInterior.DiffuserPrefix, StringComparison.Ordinal)) diffusers++;
            }
        List<string> wrong = new List<string>(), lines = new List<string>();
        foreach (string prefix in new[] { "CctvDome_", "SmokeDetector_" })
        {
            int count = prefix == "CctvDome_" ? L.Domes.Length : L.SmokeDetectors.Length;
            for (int i = 0; i < count; i++)
            {
                Transform device = ceiling.Find(prefix + i);
                if (device == null || !DemoAssetLibrary.WorldBounds(device.gameObject, out Bounds b))
                {
                    wrong.Add(prefix + i + " missing");
                    continue;
                }
                bool hit = DemoValidation.TriangleRay(new Ray(new Vector3(b.center.x, b.min.y, b.center.z), Vector3.up), 0.2f, slabMesh, out float d);
                float underside = b.min.y + d;
                if (!hit || Mathf.Abs(underside - b.max.y) > 0.003f || Mathf.Abs(underside - L.CeilingY) > 0.003f) wrong.Add(prefix + i + " not flush");
                float nearest = float.MaxValue;
                string near = "none";
                for (int j = 0; j < fittings.Count; j++)
                {
                    float g = DemoValidation.FootprintGap(b, fittings[j]);
                    if (g < nearest) { nearest = g; near = names[j]; }
                }
                if (nearest < 0.3f) wrong.Add(prefix + i + " beside " + near);
                if (device.GetComponentsInChildren<Collider>(true).Length > 0) wrong.Add(prefix + i + " has a collider");
                foreach (MeshRenderer r in device.GetComponentsInChildren<MeshRenderer>(true))
                    if ((GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0) wrong.Add(prefix + i + " in GI");
                lines.Add(prefix + i + " nearest " + near + " " + (nearest < float.MaxValue ? nearest.ToString("0.00") + " m" : "-"));
            }
        }
        DemoValidation.Info("Ceiling devices: " + string.Join(", ", lines) + " (" + diffusers + " diffusers placed)"
            + (wrong.Count > 0 ? "; wrong: " + string.Join(", ", wrong) : ""));
        DemoValidation.Check(lines.Count == L.Domes.Length + L.SmokeDetectors.Length && wrong.Count == 0, label);
    }

    // Each PIR detector looks into its room (towards its centre), and the showroom ones never
    // towards the shop windows (-z); the office PIR on the back wall faces -z by design.
    private static void DetailPirs(Transform root)
    {
        const string label = "PIR detectors face into the showroom and the office, and the showroom ones never towards the shop windows";
        List<string> lines = new List<string>();
        bool ok = true;
        int count = 0;
        foreach (L.Fixture f in L.Fixtures)
        {
            if (f.Kit != "pir_detector") continue;
            count++;
            Transform device = root.Find(f.Zone + "/" + f.Name);
            if (device == null)
            {
                ok = false;
                lines.Add(f.Name + " missing");
                continue;
            }
            Vector3 facing = device.TransformDirection(Vector3.back);
            facing.y = 0;
            facing.Normalize();
            Vector3 centre = root.TransformPoint(f.Zone == "Office" ? new Vector3(0, 0, 10.25f) : new Vector3(0, 0, 4.2f)), to = centre - device.position;
            to.y = 0;
            to.Normalize();
            float room = Vector3.Dot(facing, to), street = Vector3.Dot(facing, root.TransformDirection(Vector3.back));
            ok &= room >= 0.3f && (f.Zone == "Office" || street <= 0.5f);
            lines.Add(f.Name + " room " + room.ToString("0.00") + ", street " + street.ToString("0.00"));
        }
        DemoValidation.Info("PIR detectors: " + string.Join(", ", lines));
        DemoValidation.Check(ok && count == 3, label);
    }

    // The corner camera stands square to the partition (yaw 0, pivot on the showroom face) with
    // nothing of its Body behind that face, and its lens (the DeviceBlack submesh) at least
    // 0.2 m out from the wall, looking at the entrance in plan.
    private static void DetailCamera(Transform root)
    {
        const string label = "The corner camera's plate sits flat on the partition and only its housing turns towards the entrance";
        Transform camera = root.Find("Showroom/SecurityCamera"), entrance = root.Find("Anchors/EntranceCentre");
        Transform body = camera != null ? camera.Find("Body") : null;
        MeshFilter f = body != null ? body.GetComponent<MeshFilter>() : null;
        MeshRenderer r = body != null ? body.GetComponent<MeshRenderer>() : null;
        if (f == null || f.sharedMesh == null || r == null || entrance == null)
        {
            DemoValidation.Info("Security camera Body or the EntranceCentre anchor missing");
            DemoValidation.Check(false, label);
            return;
        }
        float yaw = Mathf.DeltaAngle(camera.localEulerAngles.y, 0);
        bool ok = Mathf.Abs(yaw) <= 0.5f && Mathf.Abs(camera.position.z - NightInterior.ShowroomFace) <= 0.0005f;
        Mesh mesh = f.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Matrix4x4 toWorld = body.localToWorldMatrix;
        float deepest = float.MinValue;
        foreach (Vector3 v in vertices) deepest = Mathf.Max(deepest, toWorld.MultiplyPoint3x4(v).z);
        ok &= deepest <= NightInterior.ShowroomFace + 0.0005f;
        int lens = Array.IndexOf(r.sharedMaterials, StoreKit.DeviceBlack);
        float aim = float.NaN, offWall = float.NaN;
        if (lens >= 0 && lens < mesh.subMeshCount)
        {
            HashSet<int> used = new HashSet<int>(mesh.GetTriangles(lens));
            Vector3 c = Vector3.zero;
            foreach (int i in used) c += toWorld.MultiplyPoint3x4(vertices[i]);
            c /= Mathf.Max(1, used.Count);
            Vector3 toLens = c - camera.position, toEntrance = entrance.position - camera.position;
            toLens.y = 0;
            toEntrance.y = 0;
            aim = Vector3.Dot(toLens.normalized, toEntrance.normalized);
            offWall = NightInterior.ShowroomFace - c.z;
            ok &= used.Count > 0 && offWall >= 0.2f && aim >= 0.95f;
        }
        else ok = false;
        DemoValidation.Info("Security camera: yaw " + yaw.ToString("0.0") + ", deepest Body vertex " + ((deepest - NightInterior.ShowroomFace) * 1000).ToString("0.0")
            + " mm past the partition face, lens " + offWall.ToString("0.000") + " m out, aim dot " + aim.ToString("0.000"));
        DemoValidation.Check(ok, label);
    }

    // The one cut cable is in the hidden Case evidence set's Content (never static, probe-lit),
    // in front of the partition, its top inside the camera's plate.
    private static void DetailCutCable(Transform root)
    {
        const string label = "The only cut cable hangs under the security camera in the hidden Case evidence set, in front of the wall";
        List<Transform> cables = new List<Transform>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == NightDecals.CutCableName) cables.Add(t);
        Transform camera = root.Find("Showroom/SecurityCamera");
        Transform content = root.Find(NightDecals.SetsPath + "/" + NightDecals.EvidenceSet + "/" + NightDecals.ContentName);
        Transform cable = cables.Count == 1 ? cables[0] : null;
        MeshRenderer r = cable != null ? cable.GetComponent<MeshRenderer>() : null;
        MeshFilter f = cable != null ? cable.GetComponent<MeshFilter>() : null;
        bool ok = cable != null && content != null && cable.parent == content && camera != null && r != null && f != null && f.sharedMesh != null
            && GameObjectUtility.GetStaticEditorFlags(cable.gameObject) == 0 && r.receiveGI == ReceiveGI.LightProbes;
        float wall = float.NaN, reach = float.NaN;
        if (ok)
        {
            Matrix4x4 toWorld = cable.localToWorldMatrix;
            float front = float.MinValue, high = float.MinValue;
            Vector3 top = Vector3.zero;
            foreach (Vector3 v in f.sharedMesh.vertices)
            {
                Vector3 p = toWorld.MultiplyPoint3x4(v);
                front = Mathf.Max(front, p.z);
                if (p.y > high) { high = p.y; top = p; }
            }
            wall = NightInterior.ShowroomFace - front;
            reach = Vector3.Distance(top, camera.position);
            ok &= front <= NightInterior.ShowroomFace - 0.0005f && reach <= 0.02f && high < camera.position.y + 0.015f;
        }
        DemoValidation.Info("Cut cable: " + cables.Count + " found, " + (wall * 1000).ToString("0.0") + " mm off the partition, top " + (reach * 1000).ToString("0.0") + " mm from the camera's pivot");
        DemoValidation.Check(ok, label);
    }

    // ---------------------------------------------------------------- street

    // The three bare street trees (NightStreet.Trees; procedural, so this always runs) stand on
    // the far pavement: Exterior/Trees holds one Tree_i per NightStreet.TreePositions entry and
    // no collider anywhere (they stand outside the play area). Each is one opaque, shadowless
    // renderer, a GI contributor lit by the probes, of at most 800 triangles. Its box starts on
    // the paving, stays under 8 m and between 0.3 m in front of the facades and 0.3 m past the
    // far kerb line, and every vertex below 0.5 m (the trunk and its grate) lies on the far
    // pavement behind the kerb stone. Its x/z footprint keeps 1 m from the far lanterns
    // (Streetlight_2 and _3) and from the bus shelter and the cycle stands, those placed
    // (StreetModels). Measured on the placed renderers and their mesh vertices in world space.
    private static void DetailTrees(Transform root)
    {
        Transform group = root.Find("Exterior/" + NightStreet.TreeGroup);
        bool ok = group != null && group.childCount == NightStreet.TreePositions.Length && group.GetComponentsInChildren<Collider>(true).Length == 0;
        List<KeyValuePair<string, Bounds>> neighbours = new List<KeyValuePair<string, Bounds>>();
        foreach (string path in new[] { "Exterior/Streetlights/Streetlight_2", "Exterior/Streetlights/Streetlight_3" })
        {
            Transform t = root.Find(path);
            if (t != null && DemoAssetLibrary.WorldBounds(t.gameObject, out Bounds b)) neighbours.Add(new KeyValuePair<string, Bounds>(t.name, b));
            else ok = false;
        }
        List<string> absent = new List<string>();
        foreach (string name in new[] { "BusShelter", "BikeRack_0", "BikeRack_1" })
        {
            Transform t = root.Find("Exterior/" + NightStreet.GroupName + "/" + name);
            if (t != null && DemoAssetLibrary.WorldBounds(t.gameObject, out Bounds b)) neighbours.Add(new KeyValuePair<string, Bounds>(name, b));
            else absent.Add(name);
        }
        List<string> lines = new List<string>();
        if (group != null)
            foreach (Transform tree in group)
            {
                MeshRenderer r = tree.GetComponent<MeshRenderer>();
                MeshFilter filter = tree.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (r == null || mesh == null)
                {
                    ok = false;
                    lines.Add(tree.name + " not drawn");
                    continue;
                }
                bool opaque = r.sharedMaterials.Length > 0;
                foreach (Material m in r.sharedMaterials) opaque &= m != null && m.renderQueue < 2500;
                bool lit = r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off && r.receiveGI == ReceiveGI.LightProbes
                    && (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0;
                int triangles = 0;
                for (int i = 0; i < mesh.subMeshCount; i++) triangles += (int)mesh.GetIndexCount(i) / 3;
                Bounds b = r.bounds;
                bool placed = Mathf.Abs(b.min.y) <= 0.005f && b.max.y <= 8 && b.min.z >= L.FarPavementZ + 0.3f && b.max.z <= L.FarKerbZ + 0.3f;
                bool grounded = true;
                Matrix4x4 world = tree.localToWorldMatrix;
                foreach (Vector3 v in mesh.vertices)
                {
                    Vector3 p = world.MultiplyPoint3x4(v);
                    if (p.y < 0.5f) grounded &= p.z >= L.FarPavementZ && p.z <= L.FarKerbZ - 0.18f;
                }
                float nearest = float.MaxValue;
                string nearestName = "nothing";
                foreach (KeyValuePair<string, Bounds> n in neighbours)
                {
                    float gap = DemoValidation.FootprintGap(b, n.Value);
                    if (gap < nearest) { nearest = gap; nearestName = n.Key; }
                }
                ok &= opaque && lit && triangles <= 800 && placed && grounded && nearest >= 1.0f;
                lines.Add(tree.name + " x " + b.min.x.ToString("0.00") + ".." + b.max.x.ToString("0.00") + ", y " + b.min.y.ToString("0.000") + ".."
                    + b.max.y.ToString("0.00") + ", z " + b.min.z.ToString("0.00") + ".." + b.max.z.ToString("0.00") + ", " + triangles + " triangles, nearest "
                    + nearestName + " " + nearest.ToString("0.00") + " m" + (opaque ? "" : ", not opaque") + (lit ? "" : ", wrong shadow or GI flags")
                    + (grounded ? "" : ", base off the far pavement"));
            }
        DemoValidation.Info("Street trees: " + (lines.Count > 0 ? string.Join("; ", lines) : "none built")
            + (absent.Count > 0 ? " (not placed, so not measured: " + string.Join(", ", absent) + ")" : ""));
        DemoValidation.Check(ok, "Street trees stand on the far pavement clear of the lamps, the shelter, the racks and the facades, probe-lit, shadowless and without colliders");
    }
}
#endif
