#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using L = NightStoreLayout;

// The street the realism wiring added (NightStreet, NightStoreBuilder's exterior and facade),
// checked on the built scene: in Edit mode right after the lighting pass (StreetPackChecks,
// from Generate) and in Play mode at arrival (StreetPlayChecks, from Tick stage 1). Every
// measurement comes from the placed objects (renderer bounds through DemoAssetLibrary.WorldBounds,
// mesh vertices and triangles, colliders, static flags, materials), never from the builder's
// layout or placement helpers, so a wrong pivot, mirror, collider or flag shows. A check that
// needs StreetModels, ForensicModels or PeopleModels logs an Info line and skips without that
// pack, as in a project unpacked from the ZIP alone; a model its pack has but the build left
// out fails. File-local members carry the Street prefix, so the other area files of this
// partial class never collide with them.
public static partial class NightStoreValidation
{
    private static readonly string[] StreetPacks = { NightStreet.StreetPack, NightStreet.ForensicPack, NightStreet.PeoplePack };

    // Country-specific StreetModels (Koda's decision 1 stays open): never placed outside the shop.
    private static readonly string[] StreetCountryModels = { "FireHydrant", "ParkingMeter", "TrafficLight", "SignNoStopping", "SignOneWayStreetName", "StreetNamePlate" };

    // Edit mode, after NightLighting.Apply (lights, lenses and lightmap scales set) and before
    // the bake and the player rig: placement, colliders, figures, bollards, the buildings across
    // the street, the facades, the baked emitters, the alarm bell box and the neutral kerb.
    private static void StreetPackChecks(Transform root)
    {
        // The builder moved and added colliders without a physics step.
        Physics.SyncTransforms();
        List<string> packs = new List<string>();
        foreach (string pack in StreetPacks)
            packs.Add(pack + (AssetDatabase.IsValidFolder(DemoAssetLibrary.Root + "/" + pack) ? " present" : " absent"));
        DemoValidation.Info("Street packs: " + string.Join(", ", packs));
        StreetFurnitureChecks(root);
        StreetColliderChecks(root);
        StreetShadowChecks(root);
        StreetFigureChecks(root);
        StreetBollardChecks(root);
        StreetApartmentChecks(root);
        StreetSkylineChecks(root);
        StreetFacadeChecks(root);
        StreetEmitterChecks(root);
        StreetBellBoxChecks(root);
        StreetNeutralityChecks(root);
    }

    // Play mode at arrival, after PlacementChecks (the rig at the spawn, desktop input off, no
    // tools out, VR off): the walk route stays clear and the rack samples stay reachable.
    private static void StreetPlayChecks()
    {
        Physics.SyncTransforms();
        StreetRouteChecks();
        StreetRackSampleChecks();
    }

    // ---------------------------------------------------------------- furniture and figures

    // NightStreet.Furniture stands on the surface its Position names: the gully grates 12 mm
    // proud in the wet gutter, the cabinet, the shelter and the case on the pavements, the
    // cycle stands with their legs 0.12 m in the paving, the glove box on the table top. The
    // surface is the highest ground or equipment collider under the model's centre.
    private static void StreetFurnitureChecks(Transform root)
    {
        Transform ground = root.Find("Exterior/Ground"), equipment = root.Find("Exterior/Equipment");
        List<string> lines = new List<string>(), absent = new List<string>();
        bool ok = ground != null && equipment != null;
        foreach (NightStreet.Item item in NightStreet.Furniture)
        {
            Transform t = root.Find("Exterior/" + NightStreet.GroupName + "/" + item.Name);
            if (t == null || !DemoAssetLibrary.WorldBounds(t.gameObject, out Bounds b))
            {
                // Without its model the builder skips the item; with it, nothing may be missing.
                if (DemoAssetLibrary.Has(item.Pack, item.Model)) { ok = false; lines.Add(item.Name + " not placed"); }
                else absent.Add(item.Name);
                continue;
            }
            float surface = float.NegativeInfinity;
            foreach (RaycastHit hit in Physics.RaycastAll(new Ray(new Vector3(b.center.x, b.max.y + 1, b.center.z), Vector3.down), b.max.y + 3,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if ((ground != null && hit.collider.transform.IsChildOf(ground)) || (equipment != null && hit.collider.transform.IsChildOf(equipment)))
                    surface = Mathf.Max(surface, hit.point.y);
            bool found = !float.IsNegativeInfinity(surface);
            ok &= found && b.min.y >= surface - 0.2f && b.min.y <= surface + 0.01f && b.max.y >= surface + 0.005f;
            lines.Add(item.Name + " " + b.min.y.ToString("0.000") + " on " + (found ? surface.ToString("0.000") : "nothing"));
        }
        if (absent.Count > 0) DemoValidation.Info("Street furniture without its model, not placed: " + string.Join(", ", absent));
        if (lines.Count == 0)
        {
            DemoValidation.Info("StreetModels and ForensicModels not present: street furniture placement check skipped");
            return;
        }
        DemoValidation.Info("Street furniture base y on its surface: " + string.Join("; ", lines));
        DemoValidation.Check(ok, "Street furniture rests on the pavement, the street or the table under it");
    }

    // A prop whose renderers' x/z footprint reaches a movement region carries one BoxCollider,
    // on its root, enclosing its renderers (the walker, teleport arcs and tool placement stop
    // on it); props and figures that stand outside carry none. The store root sits at the
    // world origin, as the builder places it, so world x/z is layout x/z.
    private static void StreetColliderChecks(Transform root)
    {
        List<Transform> props = new List<Transform>();
        foreach (NightStreet.Item item in NightStreet.Furniture)
        {
            Transform t = root.Find("Exterior/" + NightStreet.GroupName + "/" + item.Name);
            if (t != null) props.Add(t);
        }
        Transform people = root.Find("Exterior/" + NightStreet.PeopleGroup);
        if (people != null) foreach (Transform figure in people) props.Add(figure);
        if (props.Count == 0)
        {
            DemoValidation.Info("StreetModels, ForensicModels and PeopleModels not present: street prop collider check skipped");
            return;
        }
        bool ok = true;
        List<string> reaching = new List<string>();
        foreach (Transform t in props)
        {
            Collider[] colliders = t.GetComponentsInChildren<Collider>(true);
            bool reaches = false;
            bool drawn = DemoAssetLibrary.WorldBounds(t.gameObject, out Bounds b);
            if (drawn)
                foreach (InteriorBounds.Region r in L.Regions())
                    reaches |= b.max.x >= r.MinX && b.min.x <= r.MaxX && b.max.z >= r.MinZ && b.min.z <= r.MaxZ;
            if (!reaches) { ok &= colliders.Length == 0; continue; }
            reaching.Add(t.name);
            BoxCollider box = colliders.Length == 1 ? colliders[0] as BoxCollider : null;
            if (box == null || box.transform != t) { ok = false; continue; }
            Bounds held = DemoValidation.BoxColliderBounds(box);
            Vector3 inset = Vector3.one * 0.01f;
            ok &= held.Contains(b.min + inset) && held.Contains(b.max - inset);
        }
        DemoValidation.Info("Street props reaching the play area, one bounds collider each: " + (reaching.Count > 0 ? string.Join(", ", reaching) : "none"));
        DemoValidation.Check(ok, "Street props that reach the play area carry one bounds collider; the rest carry none");
    }

    // Each placed street prop and figure casts shadows as NightStreet lists it (the cabinet,
    // shelter, case and figures in the beams and pools; the flat grates, thin stands and glove
    // box not), except its blended surfaces (the shelter's glass), which never cast. A prop with
    // an LOD class keeps one level per LOD file in its LODGroup (the cabinet and the case have
    // three); one without a class has none.
    private static void StreetShadowChecks(Transform root)
    {
        bool ok = true;
        int placed = 0, casting = 0, blended = 0;
        List<string> levels = new List<string>();
        // A missing model is StreetFurnitureChecks' concern; the figures' LODs are StreetFigureChecks'.
        void Prop(NightStreet.Item item, string group, bool lods)
        {
            Transform t = root.Find("Exterior/" + group + "/" + item.Name);
            if (t == null) return;
            placed++;
            foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                bool glass = DemoAssetLibrary.Blended(r);
                if (glass) blended++;
                else if (item.Shadows) casting++;
                ok &= r.shadowCastingMode == (item.Shadows && !glass ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off);
            }
            if (!lods) return;
            int files = 1;
            foreach (string suffix in new[] { "_LOD1", "_LOD2" })
                if (DemoAssetLibrary.Has(item.Pack, item.Model + suffix)) files++;
            LODGroup lod = t.GetComponent<LODGroup>();
            ok &= item.Lod != DemoAssetLibrary.LodClass.None && files >= 2 ? lod != null && lod.lodCount == files : lod == null;
            if (lod != null) levels.Add(item.Name + " " + lod.lodCount);
        }
        foreach (NightStreet.Item item in NightStreet.Furniture) Prop(item, NightStreet.GroupName, true);
        foreach (NightStreet.Item item in NightStreet.Figures) Prop(item, NightStreet.PeopleGroup, false);
        if (NightStreet.Guard(out NightStreet.Item guard)) Prop(guard, NightStreet.PeopleGroup, false);
        if (placed == 0)
        {
            DemoValidation.Info("StreetModels, ForensicModels and PeopleModels not present: street prop shadow check skipped");
            return;
        }
        DemoValidation.Info("Street props and figures: " + placed + " placed, " + casting + " renderers casting, " + blended
            + " blended (no shadow); LOD groups: " + (levels.Count > 0 ? string.Join(", ", levels) : "none"));
        DemoValidation.Check(ok, "Street props cast shadows as listed, their glass never, and keep their LOD levels");
    }

    // The passer-by (and a PoliceLook guard when the active preset has one) stands at least
    // 2.5 m outside every movement region, measured on its mesh vertices (the renderers' boxes,
    // turned with the figure, overstate its footprint: about 2.4 m against 2.8 m for the
    // passer-by), with no collider; with the pack's two LOD files it has a three-level LODGroup.
    private static void StreetFigureChecks(Transform root)
    {
        Transform people = root.Find("Exterior/" + NightStreet.PeopleGroup);
        if (people == null || people.childCount == 0)
        {
            DemoValidation.Info("PeopleModels not present: street figure check skipped");
            return;
        }
        bool ok = true;
        List<string> gaps = new List<string>();
        foreach (Transform figure in people)
        {
            List<Vector3> points = new List<Vector3>();
            if (!StreetVertices(figure, points))
            {
                DemoValidation.Info("Mesh data of " + figure.name + " is not readable: its gap to the play area is not measured");
                continue;
            }
            float gap = 0;
            if (points.Count > 0)
            {
                float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
                foreach (Vector3 p in points)
                {
                    x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x);
                    z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z);
                }
                gap = float.MaxValue;
                foreach (InteriorBounds.Region r in L.Regions())
                {
                    float dx = Mathf.Max(0, Mathf.Max(r.MinX - x1, x0 - r.MaxX)), dz = Mathf.Max(0, Mathf.Max(r.MinZ - z1, z0 - r.MaxZ));
                    gap = Mathf.Min(gap, Mathf.Sqrt(dx * dx + dz * dz));
                }
            }
            bool lods = true;
            if (DemoAssetLibrary.Has(NightStreet.PeoplePack, figure.name + "_LOD1") && DemoAssetLibrary.Has(NightStreet.PeoplePack, figure.name + "_LOD2"))
            {
                LODGroup group = figure.GetComponent<LODGroup>();
                lods = group != null && group.lodCount == 3;
            }
            ok &= points.Count > 0 && gap >= 2.5f && figure.GetComponentsInChildren<Collider>(true).Length == 0 && lods;
            gaps.Add(figure.name + " " + (points.Count > 0 ? gap.ToString("0.00") + " m" : "without a mesh") + (lods ? "" : " (LOD levels missing)"));
        }
        DemoValidation.Info("Street figures, gap to the play area: " + string.Join(", ", gaps));
        DemoValidation.Check(ok, "Figures stand at least 2.5 m outside the play area with no collider");
    }

    // The anti-ram row: Bollard_0..n at the layout's row, each solid from the paving to at
    // least 0.8 m (the walker stops on it) with a walkable gap between neighbours. StreetModels'
    // bollard carries one bounds BoxCollider on its root, casts shadows in the headlight beams
    // and uses the shared pack materials; StoreKit's procedural fallback is checked for its
    // collider only.
    private static void StreetBollardChecks(Transform root)
    {
        Transform group = root.Find("Exterior/Bollards");
        bool pack = DemoAssetLibrary.Has(NightStreet.StreetPack, "Bollard");
        bool ok = group != null && group.childCount == L.BollardX.Length;
        List<Bounds> solid = new List<Bounds>();
        for (int i = 0; ok && i < L.BollardX.Length; i++)
        {
            Transform bollard = group.Find("Bollard_" + i);
            Collider[] colliders = bollard != null ? bollard.GetComponentsInChildren<Collider>(true) : new Collider[0];
            if (colliders.Length == 0) { ok = false; break; }
            Bounds cb = StreetColliderBounds(colliders[0]);
            for (int k = 1; k < colliders.Length; k++) cb.Encapsulate(StreetColliderBounds(colliders[k]));
            // In the row (a pivot off the post's axis moves the collider) and standing on the paving.
            ok &= Mathf.Abs(cb.center.x - L.BollardX[i]) < 0.05f && Mathf.Abs(cb.center.z - L.BollardZ) < 0.05f
                && Mathf.Abs(cb.min.y) <= 0.02f && cb.size.y >= 0.8f;
            solid.Add(cb);
            if (!pack) continue;
            ok &= colliders.Length == 1 && colliders[0] is BoxCollider && colliders[0].transform == bollard;
            // Every opaque part casts (glass never would, DemoAssetLibrary.Blended); at least one does.
            int casting = 0;
            foreach (MeshRenderer r in bollard.GetComponentsInChildren<MeshRenderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                    ok &= m != null && AssetDatabase.GetAssetPath(m).StartsWith(DemoAssetLibrary.PackMaterialFolder + "/", StringComparison.Ordinal);
                if (DemoAssetLibrary.Blended(r)) continue;
                casting++;
                ok &= r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On;
            }
            ok &= casting > 0;
        }
        solid.Sort((a, b) => a.center.x.CompareTo(b.center.x));
        float narrowest = float.MaxValue;
        for (int i = 0; i + 1 < solid.Count; i++) narrowest = Mathf.Min(narrowest, solid[i + 1].min.x - solid[i].max.x);
        ok &= solid.Count < 2 || narrowest >= 0.6f;
        string gap = solid.Count >= 2 ? narrowest.ToString("0.00") + " m" : "none";
        DemoValidation.Info(pack ? "Bollards: StreetModels Bollard with shadows; narrowest gap " + gap
            : "StreetModels Bollard not present: procedural bollards checked for colliders only; narrowest gap " + gap);
        DemoValidation.Check(ok, "Bollards stand in the anti-ram row, solid and grounded, the pack's casting shadows");
    }

    // ---------------------------------------------------------------- across the street

    // ApartmentBlock (StreetModels) directly opposite: Unity mirrors the OBJ's x on import, so
    // the block must span ApartmentX0..ApartmentX0 + ApartmentWidth with its facade on the far
    // pavement line, the plinth trim reaching 0.425 m towards the road and its back 9 m deep.
    // Its facade faces +z, the shop: in the slab along its front, triangles wound to face +z
    // outweigh those facing -z about 19 to 1 (a mirror without the winding flip inverts that).
    // The row across the street leaves the slot to it (and fills it without the block); the
    // shelter, the cycle stands and the far lamps stand clear of it. It bakes like the terrace:
    // lightmapped through its own uv2 at the terrace's scale, an occluder casting shadows,
    // its glass excepted.
    private static void StreetApartmentChecks(Transform root)
    {
        Transform block = root.Find(NightStreet.ApartmentPath);
        Transform across = root.Find("Exterior/Buildings/AcrossStreet");
        float x0 = NightStreet.ApartmentX0 + 0.05f, x1 = NightStreet.ApartmentX0 + NightStreet.ApartmentWidth - 0.05f;
        List<Vector3> row = new List<Vector3>();
        bool readable = across != null && StreetVertices(across, row);
        int inSlot = 0;
        foreach (Vector3 p in row) if (p.x > x0 && p.x < x1) inSlot++;
        bool expected = DemoAssetLibrary.Has(NightStreet.StreetPack, NightStreet.Apartment);
        DemoValidation.Info("Row across the street: " + inSlot + " vertices inside the apartment block's slot; block "
            + (block != null ? "placed" : expected ? "missing" : "not in the project"));
        DemoValidation.Check(readable && row.Count > 0 && (block != null ? inSlot == 0 : inSlot > 0 && !expected),
            "The row across the street leaves the apartment block's slot free exactly when the block stands in it");
        if (block == null)
        {
            DemoValidation.Info("StreetModels ApartmentBlock not present: the row across the street keeps its fifth box");
            return;
        }

        bool placed = DemoAssetLibrary.WorldBounds(block.gameObject, out Bounds b) && Mathf.Abs(b.min.x - NightStreet.ApartmentX0) < 0.05f
            && Mathf.Abs(b.size.x - NightStreet.ApartmentWidth) < 0.05f && b.max.z >= L.FarPavementZ && b.max.z <= L.FarPavementZ + 0.5f
            && b.min.z <= L.FarPavementZ - 8.9f;
        float front = 0, back = 0;
        bool meshes = true;
        foreach (MeshFilter f in block.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = f.sharedMesh;
            if (mesh == null) continue;
            Vector3[] v = mesh.vertices;
            int[] triangles = mesh.triangles;
            if (v.Length == 0 && mesh.vertexCount > 0) { meshes = false; continue; }
            Matrix4x4 world = f.transform.localToWorldMatrix;
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                Vector3 a = world.MultiplyPoint3x4(v[triangles[i]]), c1 = world.MultiplyPoint3x4(v[triangles[i + 1]]), c2 = world.MultiplyPoint3x4(v[triangles[i + 2]]);
                if ((a.z + c1.z + c2.z) / 3 <= L.FarPavementZ - 0.6f) continue;
                // Unity's front face: Cross(b - a, c - a) points out of it (as ProceduralMesh winds).
                Vector3 n = Vector3.Cross(c1 - a, c2 - a);
                float twice = n.magnitude;
                if (twice < 1e-8f) continue;
                if (n.z > 0.9f * twice) front += twice * 0.5f;
                else if (n.z < -0.9f * twice) back += twice * 0.5f;
            }
        }
        if (!meshes) DemoValidation.Info("ApartmentBlock mesh data not readable: its facade winding is not measured");
        bool facing = !meshes || (front > 0 && front >= 3 * back);
        bool clear = true;
        List<Transform> neighbours = new List<Transform>();
        Transform street = root.Find("Exterior/" + NightStreet.GroupName), lamps = root.Find("Exterior/Streetlights");
        if (street != null)
            foreach (Transform t in street)
                if (t.name == "BusShelter" || t.name.StartsWith("BikeRack_", StringComparison.Ordinal)) neighbours.Add(t);
        if (lamps != null)
            foreach (Transform t in lamps)
                if (t.name.StartsWith("Streetlight_", StringComparison.Ordinal)) neighbours.Add(t);
        foreach (Transform t in neighbours)
            if (DemoAssetLibrary.WorldBounds(t.gameObject, out Bounds nb) && nb.Intersects(b)) clear = false;
        DemoValidation.Info("Apartment block x " + b.min.x.ToString("0.00") + ".." + b.max.x.ToString("0.00") + ", z " + b.min.z.ToString("0.00") + ".."
            + b.max.z.ToString("0.00") + "; front slab facing the street " + front.ToString("0") + " m2, facing away " + back.ToString("0") + " m2");
        DemoValidation.Check(placed && facing && clear, "The apartment block stands in its slot across the street, its facade to the shop");

        float terrace = ScaleAt(root, "Exterior/Buildings/Neighbours");
        StaticEditorFlags baked = StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic;
        int opaque = 0, glass = 0;
        bool bakes = terrace > 0;
        foreach (MeshRenderer r in block.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (DemoAssetLibrary.Blended(r))
            {
                glass++;
                bakes &= r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off;
                continue;
            }
            opaque++;
            MeshFilter filter = r.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            bakes &= (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & baked) == baked && r.receiveGI == ReceiveGI.Lightmaps
                && mesh != null && mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1)
                && Mathf.Abs(r.scaleInLightmap - terrace) < 0.001f && r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On;
        }
        DemoValidation.Info("Apartment block renderers: " + opaque + " lightmapped and shadowing (terrace scale " + terrace + "), " + glass + " glass");
        DemoValidation.Check(bakes && opaque > 0, "The apartment block bakes into the lightmaps at the terrace's density and casts shadows");
    }

    // The KayKit building_*_withoutBase towers that stood 16 m behind the far pavement line are
    // no longer placed: no building_ object under the exterior, no KayKitCity mesh among the
    // buildings, and nothing drawn behind the far pavement's end (FarPavementZ - 10, where the
    // row across the street and the slab stop). KayKitCity ships in the ZIP, so this always runs.
    private static void StreetSkylineChecks(Transform root)
    {
        Transform exterior = root.Find("Exterior");
        Transform buildings = exterior != null ? exterior.Find("Buildings") : null;
        bool gone = exterior != null && buildings != null;
        float deepest = float.MaxValue;
        if (gone)
        {
            foreach (Transform t in exterior.GetComponentsInChildren<Transform>(true))
                gone &= !t.name.StartsWith("building_", StringComparison.Ordinal);
            foreach (MeshFilter f in buildings.GetComponentsInChildren<MeshFilter>(true))
                gone &= f.sharedMesh == null || !AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(DemoAssetLibrary.Root + "/KayKitCity/", StringComparison.Ordinal);
            foreach (Renderer r in exterior.GetComponentsInChildren<Renderer>(true)) deepest = Mathf.Min(deepest, r.bounds.min.z);
        }
        DemoValidation.Info("Exterior drawn back to z " + deepest.ToString("0.00") + " (far pavement ends at " + (L.FarPavementZ - 10).ToString("0.00") + ")");
        DemoValidation.Check(gone && deepest >= L.FarPavementZ - 10.05f, "The KayKit skyline towers are gone; nothing stands beyond the far pavement");
    }

    // The terrace: KitBrick only on the left neighbour (x <= MinX - Pier), KitRender only on
    // the right (x >= MaxX + Pier), and a KitShutter face on both shopfronts just in front of
    // their glass (z in [MinZ - 0.03, MinZ)). Across the street: render, shutters just proud of
    // the far facade line (FarPavementZ, facing +z) and lit windows. Measured on the submesh
    // each palette material draws, in world space.
    private static void StreetFacadeChecks(Transform root)
    {
        Dictionary<Material, List<Vector3>> near = StreetSubmeshes(root.Find("Exterior/Buildings/Neighbours"));
        Dictionary<Material, List<Vector3>> far = StreetSubmeshes(root.Find("Exterior/Buildings/AcrossStreet"));
        List<Vector3> Of(Dictionary<Material, List<Vector3>> parts, Material m) => m != null && parts.TryGetValue(m, out List<Vector3> v) ? v : new List<Vector3>();
        float left = L.MinX - L.Pier, right = L.MaxX + L.Pier;
        List<Vector3> brick = Of(near, StoreKit.Brick), render = Of(near, StoreKit.Render), shutter = Of(near, StoreKit.Shutter);
        bool brickLeft = brick.Count > 0, renderRight = render.Count > 0, shutterFront = shutter.Count > 0, shutterLeft = false, shutterRight = false;
        foreach (Vector3 p in brick) brickLeft &= p.x <= left + 0.01f;
        foreach (Vector3 p in render) renderRight &= p.x >= right - 0.01f;
        foreach (Vector3 p in shutter)
        {
            shutterLeft |= p.x < left;
            shutterRight |= p.x > right;
            shutterFront &= p.z >= L.MinZ - 0.03f && p.z < L.MinZ;
        }
        List<Vector3> farShutter = Of(far, StoreKit.Shutter);
        bool row = Of(far, StoreKit.Render).Count > 0 && Of(far, StoreKit.WindowLit).Count > 0 && farShutter.Count > 0;
        foreach (Vector3 p in farShutter) row &= p.z > L.FarPavementZ && p.z <= L.FarPavementZ + 0.03f;
        DemoValidation.Info("Street facades: brick " + (StoreKit.PaletteSource(StoreKit.Brick) ?? "scalar finish") + ", render "
            + (StoreKit.PaletteSource(StoreKit.Render) ?? "scalar finish") + ", shutters " + (StoreKit.PaletteSource(StoreKit.Shutter) ?? "scalar finish"));
        DemoValidation.Check(brickLeft && renderRight && shutterLeft && shutterRight && shutterFront && row,
            "Brick on the left, render on the right and half-closed shutters on both neighbours and across the street");
    }

    // The street's night light is baked emission: the lit rooms across the street (KitWindowLit)
    // and the apartment block's rooms and the bus shelter's lightbox (its colour map as the
    // emission map), each on a GI contributor. No Light component sits under the exterior: the
    // headlights and streetlights live in the lighting group, the halos are cards.
    private static void StreetEmitterChecks(Transform root)
    {
        bool Glows(Material m) => m != null && m.IsKeywordEnabled("_EMISSION") && (m.globalIlluminationFlags & MaterialGlobalIlluminationFlags.BakedEmissive) != 0;
        bool Contributes(Renderer r) => (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0;
        bool Bakes(Transform t, bool mapped)
        {
            foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>(true))
                foreach (Material m in r.sharedMaterials)
                    if (Glows(m) && (!mapped || m.GetTexture("_EmissionMap") != null) && Contributes(r)) return true;
            return false;
        }
        Transform across = root.Find("Exterior/Buildings/AcrossStreet"), block = root.Find(NightStreet.ApartmentPath);
        Transform shelter = root.Find("Exterior/" + NightStreet.GroupName + "/BusShelter"), exterior = root.Find("Exterior");
        MeshRenderer row = across != null ? across.GetComponent<MeshRenderer>() : null;
        bool windows = Glows(StoreKit.WindowLit) && row != null && Array.IndexOf(row.sharedMaterials, StoreKit.WindowLit) >= 0 && Contributes(row);
        bool rooms = block == null || Bakes(block, false), lightbox = shelter == null || Bakes(shelter, true);
        int lights = exterior != null ? exterior.GetComponentsInChildren<Light>(true).Length : -1;
        DemoValidation.Info("Baked street emitters: lit windows across the street" + (block != null ? ", apartment rooms" : " (no apartment block)")
            + (shelter != null ? ", bus shelter lightbox" : " (no bus shelter)") + "; Light components under the exterior: " + lights);
        DemoValidation.Check(windows && rooms && lightbox && lights == 0,
            "Lit windows, the apartment's rooms and the bus shelter's lightbox are baked emitters, and the street adds no light");
    }

    // ---------------------------------------------------------------- facade and kerb

    // The project-made bell box hangs on the fascia band right of the entrance: a ray from 1 cm
    // in front of its back (towards the shop) meets the cladding's street face at 1 cm, so its
    // back neither floats nor sinks in; it stays between the ceiling line and the coping and
    // clear of the fascia sign and the doors.
    private static void StreetBellBoxChecks(Transform root)
    {
        Transform bell = root.Find("Facade/BellBox"), cladding = root.Find("Facade/Cladding"), sign = root.Find("Facade/" + NightStoreBuilder.FasciaSign);
        MeshRenderer box = bell != null ? bell.GetComponent<MeshRenderer>() : null;
        MeshRenderer signRenderer = sign != null ? sign.GetComponent<MeshRenderer>() : null;
        bool ok = box != null && cladding != null;
        float distance = -1;
        if (ok)
        {
            Bounds bb = box.bounds;
            List<DemoValidation.RayMesh> stone = new List<DemoValidation.RayMesh>();
            foreach (MeshFilter f in cladding.GetComponentsInChildren<MeshFilter>(true)) stone.Add(DemoValidation.NewRayMesh(f));
            bool hit = DemoValidation.TriangleRay(new Ray(new Vector3(bb.center.x, bb.center.y, bb.max.z - 0.01f), Vector3.forward), 0.05f,
                stone, out distance, out Vector3 normal);
            ok = hit && Mathf.Abs(distance - 0.01f) <= 0.003f && normal.z < -0.99f && bb.min.y > L.CeilingY + 0.02f && bb.max.y < L.ParapetY - 0.02f
                && Mathf.Abs(bb.center.x) > L.DoorHalf && (signRenderer == null || !signRenderer.bounds.Intersects(bb));
            DemoValidation.Info("Alarm bell box at x " + bb.center.x.ToString("0.00") + ", y " + bb.min.y.ToString("0.00") + ".." + bb.max.y.ToString("0.00")
                + "; cladding " + (hit ? (distance - 0.01f).ToString("0.000") + " m behind its back" : "not found behind it"));
        }
        DemoValidation.Check(ok, "The alarm bell box sits on the fascia cladding, clear of the shop sign and the coping");
    }

    // The country and the police service are Koda's open decision 1: no hydrant, parking
    // machine, traffic light, road sign or street-name plate stands outside the shop, by object
    // name or by StreetModels mesh.
    private static void StreetNeutralityChecks(Transform root)
    {
        Transform exterior = root.Find("Exterior");
        List<string> found = new List<string>();
        string packFolder = DemoAssetLibrary.Root + "/" + NightStreet.StreetPack + "/";
        bool Country(string name)
        {
            foreach (string model in StreetCountryModels)
                if (name.StartsWith(model, StringComparison.Ordinal)) return true;
            return false;
        }
        if (exterior != null)
        {
            foreach (Transform t in exterior.GetComponentsInChildren<Transform>(true))
                if (Country(t.name)) found.Add(t.name);
            foreach (MeshFilter f in exterior.GetComponentsInChildren<MeshFilter>(true))
            {
                string path = f.sharedMesh != null ? AssetDatabase.GetAssetPath(f.sharedMesh) : "";
                if (path.StartsWith(packFolder, StringComparison.Ordinal) && Country(System.IO.Path.GetFileNameWithoutExtension(path))) found.Add(path);
            }
        }
        if (!AssetDatabase.IsValidFolder(DemoAssetLibrary.Root + "/" + NightStreet.StreetPack))
            DemoValidation.Info("StreetModels not present: no country-specific street model could be placed");
        else if (found.Count > 0) DemoValidation.Info("Country-specific street models placed: " + string.Join(", ", found));
        DemoValidation.Check(exterior != null && found.Count == 0,
            "No hydrant, parking machine, traffic light, road sign or name plate stands outside the shop (country left open)");
    }

    // ---------------------------------------------------------------- Play mode

    // Nothing the street wiring added blocks the walk: every enabled solid collider of the
    // street furniture, figures, bollards, lamp posts and equipment keeps the walker's radius
    // plus 5 cm from the spawn and from every segment of the route (spawn, then L.Route; the
    // points are up to 4 m apart). A low prop resting on the ground, under the step offset (a
    // gully grate), is stepped over and skipped.
    private static void StreetRouteChecks()
    {
        Transform root = Root();
        ShopWalkController walker = DemoValidation.Find<ShopWalkController>();
        CharacterController body = walker.GetComponent<CharacterController>();
        float clearance = body.radius + 0.05f;
        List<Vector2> route = new List<Vector2>();
        Vector3 spawn = root.TransformPoint(L.PlayerSpawn);
        route.Add(new Vector2(spawn.x, spawn.z));
        foreach (Vector3 point in L.Route)
        {
            Vector3 p = root.TransformPoint(point);
            route.Add(new Vector2(p.x, p.z));
        }
        float nearest = float.MaxValue;
        string nearestName = "none";
        int measured = 0;
        foreach (string zone in new[] { "Exterior/" + NightStreet.GroupName, "Exterior/" + NightStreet.PeopleGroup, "Exterior/Bollards", "Exterior/Streetlights", "Exterior/Equipment" })
        {
            Transform t = root.Find(zone);
            if (t == null) continue;
            foreach (Collider c in t.GetComponentsInChildren<Collider>())
            {
                if (!c.enabled || c.isTrigger) continue;
                Bounds b = c.bounds;
                if (b.size.y < body.stepOffset && b.min.y < 0.02f) continue;
                Rect r = DemoValidation.Footprint(b);
                float d = float.MaxValue;
                for (int i = 0; i + 1 < route.Count; i++) d = Mathf.Min(d, DemoValidation.RectSegmentDistance(r, route[i], route[i + 1]));
                measured++;
                if (d < nearest) { nearest = d; nearestName = c.name; }
            }
        }
        DemoValidation.Info("Walk route clearance: nearest street collider " + nearestName + " at " + nearest.ToString("0.00") + " m of " + measured
            + " measured (needs " + clearance.ToString("0.00") + " m)");
        DemoValidation.Check(measured > 0 && nearest >= clearance, "Street furniture and bollards keep the walk route and the spawn clear");
    }

    // The rack samples on the equipment table, a few centimetres from the NitrileGloveBox
    // (which carries a bounds collider, as it stands in the pavement region) and under the
    // evidence camera: a desktop pointer aimed at each sample from the street side and from
    // the shop side of the table hovers that sample, not the glove box or the camera, and a
    // hand 3 cm above it picks that sample as its grab target (HandInteractor.Grab's own
    // choice, read without spawning a tool, so the marker numbering stays untouched).
    private static void StreetRackSampleChecks()
    {
        Transform root = Root();
        ToolStation station = DemoValidation.Find<ToolStation>();
        DesktopInteractor desktop = DemoValidation.Find<DesktopInteractor>();
        HandInteractor hand = desktop.transform.Find("RightHand").GetComponent<HandInteractor>();
        System.Reflection.BindingFlags own = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        System.Reflection.MethodInfo nearestTool = typeof(HandInteractor).GetMethod("NearestTool", own);
        System.Reflection.FieldInfo nearSample = typeof(HandInteractor).GetField("nearSample", own), nearCamera = typeof(HandInteractor).GetField("nearCamera", own);
        if (nearestTool == null || nearSample == null || nearCamera == null)
            DemoValidation.Info("HandInteractor.NearestTool, nearSample or nearCamera not found: the grab choice cannot be read");
        Vector3 table = root.TransformPoint(L.Table), handAt = hand.transform.position;
        bool idle = desktop.Held == null, clicks = idle, grabs = nearestTool != null && nearSample != null && nearCamera != null;
        List<Bounds> samples = new List<Bounds>();
        List<string> missed = new List<string>();
        if (!idle) missed.Add("every click (the desktop visitor holds a tool)");
        foreach (DemoToolKind kind in new[] { DemoToolKind.Cone, DemoToolKind.Marker, DemoToolKind.TapePost, DemoToolKind.Scale, DemoToolKind.Measure })
        {
            Transform sample = station.transform.Find("Sample_" + kind);
            ToolRackSample rack = sample != null ? sample.GetComponent<ToolRackSample>() : null;
            if (rack == null || rack.Kind != kind || !StreetSampleTarget(sample, out Vector3 target, out Bounds all))
            {
                clicks = grabs = false;
                missed.Add(kind + " sample");
                continue;
            }
            samples.Add(all);
            // Aiming while a tool is held would carry it to the sample (arrival holds none).
            foreach (float side in idle ? new[] { -0.9f, 0.9f } : new float[0])
            {
                Vector3 eye = new Vector3(target.x, table.y + 1.6f, table.z + side);
                desktop.Aim(new Ray(eye, target - eye));
                if (desktop.HoveredSample == rack && desktop.Hovered == null && !desktop.HoveringCamera) continue;
                clicks = false;
                missed.Add(kind + " click from the " + (side < 0 ? "street" : "shop") + " side");
            }
            if (nearestTool == null || nearSample == null || nearCamera == null) continue;
            hand.transform.position = target + Vector3.up * 0.03f;
            DeployedTool tool = (DeployedTool)nearestTool.Invoke(hand, null);
            if (tool == null && (ToolRackSample)nearSample.GetValue(hand) == rack && !(bool)nearCamera.GetValue(hand)) continue;
            grabs = false;
            missed.Add(kind + " grab");
        }
        // The hand back where it was, its grab scan redone there (the inactive VR hand keeps
        // nearSample and nearCamera until its next scan), and the pointer idle again (ReleaseAll
        // clears the hover and the pointer hit), so the later checks start from the arrival state.
        hand.transform.position = handAt;
        if (nearestTool != null) nearestTool.Invoke(hand, null);
        desktop.ReleaseAll();

        Transform gloves = root.Find("Exterior/" + NightStreet.GroupName + "/NitrileGloveBox");
        bool apart = true;
        float gap = float.MaxValue;
        Collider[] glove = gloves != null ? gloves.GetComponentsInChildren<Collider>() : new Collider[0];
        if (glove.Length > 0)
        {
            Bounds gb = glove[0].bounds;
            foreach (Collider c in glove) gb.Encapsulate(c.bounds);
            foreach (Bounds sb in samples)
            {
                apart &= !gb.Intersects(sb);
                gap = Mathf.Min(gap, StreetGap(gb, sb));
            }
        }
        DemoValidation.Info("Rack samples: " + (missed.Count > 0 ? "missed " + string.Join(", ", missed) : "every click and grab reaches its sample")
            + (glove.Length > 0 ? "; glove box " + gap.ToString("0.000") + " m from the nearest sample" : "; no glove box on the table"));
        DemoValidation.Check(clicks && apart, "Rack samples on the equipment table stay clickable from both sides, clear of the glove box");
        DemoValidation.Check(grabs, "Rack samples on the equipment table stay grabbable beside the glove box and under the camera");
    }

    // ---------------------------------------------------------------- helpers

    // World positions of every mesh vertex under t (all LOD levels); false when a mesh's
    // data cannot be read (Edit mode reads imported meshes whatever their Read/Write setting).
    private static bool StreetVertices(Transform t, List<Vector3> points)
    {
        foreach (MeshFilter f in t.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = f.sharedMesh;
            if (mesh == null) continue;
            Vector3[] vertices = mesh.vertices;
            if (vertices.Length == 0 && mesh.vertexCount > 0) return false;
            Matrix4x4 world = f.transform.localToWorldMatrix;
            foreach (Vector3 v in vertices) points.Add(world.MultiplyPoint3x4(v));
        }
        return true;
    }

    // World positions of the vertices each material's submesh uses, for the MeshFilter and
    // MeshRenderer at t (a KitMesh part: one submesh per material); empty without them.
    private static Dictionary<Material, List<Vector3>> StreetSubmeshes(Transform t)
    {
        Dictionary<Material, List<Vector3>> parts = new Dictionary<Material, List<Vector3>>();
        MeshFilter filter = t != null ? t.GetComponent<MeshFilter>() : null;
        MeshRenderer renderer = t != null ? t.GetComponent<MeshRenderer>() : null;
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null || renderer == null) return parts;
        Vector3[] vertices = mesh.vertices;
        Material[] materials = renderer.sharedMaterials;
        for (int s = 0; s < mesh.subMeshCount && s < materials.Length; s++)
        {
            if (materials[s] == null) continue;
            if (!parts.TryGetValue(materials[s], out List<Vector3> points)) parts[materials[s]] = points = new List<Vector3>();
            foreach (int i in mesh.GetIndices(s))
                if (i < vertices.Length) points.Add(t.TransformPoint(vertices[i]));
        }
        return parts;
    }

    // A collider's world bounds: a box's from its own corners (DemoValidation.BoxColliderBounds,
    // no physics sync), any other's from the physics bounds.
    private static Bounds StreetColliderBounds(Collider c) => c is BoxCollider box ? DemoValidation.BoxColliderBounds(box) : c.bounds;

    // Gap between two boxes in 3D (0 when they touch or overlap); local, since the shared
    // DemoValidation.FootprintGap measures x/z footprints only.
    private static float StreetGap(Bounds a, Bounds b) => Vector3.Max(Vector3.zero, Vector3.Max(a.min - b.max, b.min - a.max)).magnitude;

    // Where a pointer or a hand aims at a rack sample: the centre of its solid collider nearest
    // the centre of all of them (the L-scale's overall centre lies off both arms), with their
    // union; false without a solid collider.
    private static bool StreetSampleTarget(Transform sample, out Vector3 target, out Bounds all)
    {
        target = Vector3.zero;
        all = new Bounds();
        List<Collider> solid = new List<Collider>();
        foreach (Collider c in sample.GetComponentsInChildren<Collider>())
            if (c.enabled && !c.isTrigger) solid.Add(c);
        if (solid.Count == 0) return false;
        all = solid[0].bounds;
        foreach (Collider c in solid) all.Encapsulate(c.bounds);
        float best = float.MaxValue;
        foreach (Collider c in solid)
        {
            float d = (c.ClosestPoint(all.center) - all.center).sqrMagnitude;
            if (d < best) { best = d; target = c.bounds.center; }
        }
        return true;
    }
}
#endif
