#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using L = NightStoreLayout;

// NightInterior's props and wall graphics as this build placed them (NightInterior.Props and
// NightInterior.Graphics), in Edit mode after the lighting pass (final lightmap scales, no bake)
// and before the scene is first saved: the snake plants, the rug, the desk equipment, the
// extinguisher, the clock and the ceiling diffusers (InteriorModels), then the two atlas pages'
// pictures with their mounts and the lost-cat flyer on its pier (WallGraphics). Mesh data is
// read here, before Play mode's static batching merges it, and the distance checks cast rays on
// the triangles themselves: most of what they measure against (skirting, glass, pictures) has
// no collider. A check whose pack was not synced is skipped with an Info line, or checks the
// fallback the builder keeps.
public static partial class NightStoreValidation
{
    private static void InteriorPackChecks(Transform root)
    {
        DemoValidation.ClearRayMeshes();
        try
        {
            InteriorPropList(root);
            InteriorPlanters(root);
            InteriorRug(root);
            InteriorDesk(root);
            InteriorScreen(root);
            InteriorExtinguisher(root);
            InteriorClock(root);
            InteriorDiffusers(root);
            List<InteriorSheet> sheets = InteriorSheets(root);
            InteriorGraphicsFlat(root, sheets);
            InteriorFlyer(root, sheets);
            InteriorGraphicsCells(sheets);
            InteriorGraphicsReplace(root, sheets);
            InteriorGraphicsMaterials(root, sheets);
            InteriorNeutral(root);
        }
        finally { DemoValidation.ClearRayMeshes(); }
    }

    // ---------------------------------------------------------------- props

    // Every path NightInterior.Props lists names one object of this build (other checks find
    // the props by it).
    private static void InteriorPropList(Transform root)
    {
        if (NightInterior.Props.Count == 0)
        {
            DemoValidation.Info("NightInterior placed no props (InteriorModels not present): the prop checks below skip or check the fallbacks");
            return;
        }
        HashSet<string> seen = new HashSet<string>();
        bool found = true;
        foreach (string path in NightInterior.Props) found &= seen.Add(path) && root.Find(path) != null;
        DemoValidation.Info("Interior props placed: " + string.Join(", ", NightInterior.Props));
        DemoValidation.Check(found, "NightInterior.Props names the props this build placed");
    }

    // With InteriorModels each planter's procedural Body and box give way to a snake plant
    // (shadows on) standing on the floor and one bounds collider round it, on the planter root
    // that keeps its fixture name. Without the model, or with a user-converted planter (which
    // has no Body), the StoreKit planter stays as it was.
    private static void InteriorPlanters(Transform root)
    {
        bool user = DemoAssetLibrary.Has(StoreKit.OverridePack, "planter");
        bool plants = DemoAssetLibrary.Has(NightInterior.InteriorPack, NightInterior.PlantModel) && !user;
        if (!plants)
            DemoValidation.Info(user ? "StoreKit planter override present: the planters keep the user model and get no snake plant"
                : "InteriorModels SnakePlant not present: the planters keep their StoreKit body");
        List<DemoValidation.RayMesh> floor = InteriorMeshes(root.Find("Architecture/Floor"));
        bool ok = true;
        foreach (string name in NightInterior.Planters)
        {
            Transform planter = root.Find("Showroom/" + name);
            if (planter == null)
            {
                ok = false;
                DemoValidation.Info("Planter missing: Showroom/" + name);
                continue;
            }
            Transform plant = planter.Find(NightInterior.PlantModel), body = planter.Find("Body");
            if (!plants)
            {
                ok &= plant == null && planter.GetComponent<BoxCollider>() != null && (user || body != null);
                continue;
            }
            Collider[] colliders = planter.GetComponentsInChildren<Collider>(true);
            BoxCollider box = colliders.Length == 1 && colliders[0].transform == planter ? colliders[0] as BoxCollider : null;
            Bounds b = default;
            if (plant == null || box == null || body != null || !InteriorBox(plant, out b)
                || !NightInterior.Props.Contains("Showroom/" + name + "/" + NightInterior.PlantModel))
            {
                ok = false;
                DemoValidation.Info("Planter " + name + ": snake plant " + (plant != null) + ", colliders " + colliders.Length + ", Body left " + (body != null));
                continue;
            }
            Bounds solid = DemoValidation.BoxColliderBounds(box);
            float floorY = InteriorFloorY(floor, b.center);
            int casters = 0;
            bool shadows = true;
            foreach (Renderer r in plant.GetComponentsInChildren<Renderer>(true))
            {
                if (!InteriorOpaque(r)) continue;
                casters++;
                shadows &= r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On;
            }
            DemoValidation.Info("Planter " + name + ": snake plant " + InteriorSize(b) + " m, " + DemoValidation.Mm(b.min.y - floorY)
                + " above the floor, collider " + InteriorSize(solid) + " m, shadow casters " + casters);
            // The box covers the plant (shrunk 1 cm) and is no more than 1 cm larger: the plant's own bounds, not the old pot box.
            ok &= !float.IsNaN(floorY) && Mathf.Abs(b.min.y - floorY) <= 0.003f && InteriorContains(solid, b, 0.01f)
                && InteriorContains(InteriorGrow(b, 0.01f), solid, 0) && casters > 0 && shadows;
        }
        DemoValidation.Check(ok, plants ? "Snake plants fill the three planters, each solid with one bounds collider"
            : user ? "User-converted planters stay as they are" : "Planters keep their procedural body without InteriorModels");
    }

    // The 2.0 x 1.4 m rug 2 mm over the floor under the sofa and the side table: flat, no
    // collider (the walker and the tools pass over it), no shadow, and lightmapped through
    // its second UV set at the same scale as the sofa it lies under.
    private static void InteriorRug(Transform root)
    {
        if (!DemoAssetLibrary.Has(NightInterior.InteriorPack, NightInterior.RugName))
        {
            DemoValidation.Info("InteriorModels Rug not present: rug check skipped");
            return;
        }
        Transform rug = root.Find("Showroom/" + NightInterior.RugName), sofa = root.Find("Showroom/Sofa"), table = root.Find("Showroom/SideTable");
        MeshRenderer seat = null;
        if (sofa != null)
            foreach (MeshRenderer r in sofa.GetComponentsInChildren<MeshRenderer>(true))
                if (r.receiveGI == ReceiveGI.Lightmaps) { seat = r; break; }
        Bounds b = default;
        bool ok = rug != null && table != null && seat != null && InteriorBox(rug, out b) && NightInterior.Props.Contains("Showroom/" + NightInterior.RugName);
        if (ok)
        {
            float lift = b.min.y - InteriorFloorY(InteriorMeshes(root.Find("Architecture/Floor")), b.center), scale = -1;
            Rect area = DemoValidation.Footprint(b);
            ok &= area.Contains(new Vector2(sofa.position.x, sofa.position.z)) && area.Contains(new Vector2(table.position.x, table.position.z))
                && rug.GetComponentsInChildren<Collider>(true).Length == 0 && b.size.y < 0.02f && lift >= 0 && lift <= 0.004f;
            int count = 0;
            foreach (MeshRenderer r in rug.GetComponentsInChildren<MeshRenderer>(true))
            {
                count++;
                scale = r.scaleInLightmap;
                MeshFilter filter = r.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                ok &= r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off && r.receiveGI == ReceiveGI.Lightmaps
                    && (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0
                    && mesh != null && mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1)
                    && Mathf.Abs(r.scaleInLightmap - seat.scaleInLightmap) < 0.001f;
            }
            ok &= count > 0;
            DemoValidation.Info("Rug: " + InteriorSize(b) + " m, " + DemoValidation.Mm(lift) + " above the floor, x " + b.min.x.ToString("0.00") + ".." + b.max.x.ToString("0.00")
                + ", z " + b.min.z.ToString("0.00") + ".." + b.max.z.ToString("0.00") + ", lightmap scale " + scale.ToString("0.00") + " (sofa " + seat.scaleInLightmap.ToString("0.00") + ")");
        }
        else DemoValidation.Info("Rug, sofa, side table or the sofa's lightmapped part missing");
        DemoValidation.Check(ok, "The rug lies flat under the sofa and side table, lightmapped, with no collider or shadow");
    }

    // The monitor and keyboard stand on the desk top (found by a ray, not assumed) with a gap
    // between them; the transport tray the dressing left on the desk (its Leatherette and Felt
    // above the top, merged into a loose cell) and the loupe beside the keyboard (JewelryModels)
    // keep clear of both, and everything stays inside the desk's footprint.
    private static void InteriorDesk(Transform root)
    {
        const string pack = NightInterior.InteriorPack, label = "Monitor and keyboard stand on the desk, clear of the transport tray, the loupe and each other";
        if (!DemoAssetLibrary.Has(pack, NightInterior.MonitorName) || !DemoAssetLibrary.Has(pack, NightInterior.KeyboardName))
        {
            DemoValidation.Info("InteriorModels ComputerMonitor or Keyboard not present: desk layout check skipped");
            return;
        }
        Transform desk = root.Find("Office/Desk"), monitor = root.Find("Office/" + NightInterior.MonitorName), keyboard = root.Find("Office/" + NightInterior.KeyboardName);
        Transform deskBody = desk != null ? desk.Find("Body") : null;
        Bounds m = default, k = default, d = default;
        bool found = monitor != null && keyboard != null && desk != null && InteriorBox(monitor, out m) && InteriorBox(keyboard, out k)
            && InteriorBox(deskBody != null ? deskBody : desk, out d);
        float deskY = float.NaN;
        if (found && DemoValidation.TriangleRay(new Ray(new Vector3(m.center.x, 1.5f, m.center.z), Vector3.down), 1.5f, InteriorMeshes(desk), out float down, out _))
            deskY = 1.5f - down;
        if (!found || float.IsNaN(deskY))
        {
            DemoValidation.Info("Office desk, monitor or keyboard missing, or no desk top under the monitor");
            DemoValidation.Check(false, label);
        }
        Rect surface = DemoValidation.Footprint(d), screen = DemoValidation.Footprint(m), keys = DemoValidation.Footprint(k);

        // The tray: the dressing's Leatherette and Felt above the desk top, inside its footprint.
        float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
        Rect over = InteriorGrowRect(surface, 0.005f);
        Transform loose = root.Find("Dressing/" + NightStoreDressing.LooseGroup);
        if (loose != null)
            foreach (MeshRenderer r in loose.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter filter = r.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || (r.sharedMaterial != StoreKit.Leatherette && r.sharedMaterial != StoreKit.Felt)) continue;
                Matrix4x4 toWorld = r.transform.localToWorldMatrix;
                foreach (Vector3 v in filter.sharedMesh.vertices)
                {
                    Vector3 p = toWorld.MultiplyPoint3x4(v);
                    if (p.y <= deskY + 0.001f || !over.Contains(new Vector2(p.x, p.z))) continue;
                    x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x);
                    z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z);
                }
            }
        bool tray = x0 <= x1;
        Rect held = tray ? Rect.MinMaxRect(x0, z0, x1, z1) : new Rect();
        Transform loupe = root.Find("Dressing/" + NightStoreDressing.StockGroup + "/" + NightStoreDressing.StockDesk + "/JewelersLoupe");
        Bounds l = default;
        bool lens = loupe != null && InteriorBox(loupe, out l);
        Rect glass = lens ? DemoValidation.Footprint(l) : new Rect();

        bool seated = Mathf.Abs(m.min.y - deskY) <= 0.003f && Mathf.Abs(k.min.y - deskY) <= 0.003f;
        bool apart = !InteriorGrowRect(screen, 0.005f).Overlaps(InteriorGrowRect(keys, 0.005f));
        bool clear = tray && !InteriorGrowRect(screen, 0.01f).Overlaps(held) && !InteriorGrowRect(keys, 0.01f).Overlaps(held);
        if (lens) clear &= !InteriorGrowRect(glass, 0.005f).Overlaps(screen) && !InteriorGrowRect(glass, 0.005f).Overlaps(keys) && !InteriorGrowRect(glass, 0.005f).Overlaps(held);
        bool inside = InteriorInside(screen, surface) && InteriorInside(keys, surface) && (!tray || InteriorInside(held, surface)) && (!lens || InteriorInside(glass, surface));
        DemoValidation.Info("Desk top " + deskY.ToString("0.000") + " m; monitor base " + DemoValidation.Mm(m.min.y - deskY) + ", keyboard base " + DemoValidation.Mm(k.min.y - deskY)
            + "; monitor-keyboard gap " + DemoValidation.Mm(DemoValidation.FootprintGap(screen, keys))
            + (tray ? "; tray gap to monitor " + DemoValidation.Mm(DemoValidation.FootprintGap(held, screen)) + ", to keyboard " + DemoValidation.Mm(DemoValidation.FootprintGap(held, keys)) : "; no tray found on the desk")
            + (lens ? "; loupe gap to keyboard " + DemoValidation.Mm(DemoValidation.FootprintGap(glass, keys)) + ", to monitor " + DemoValidation.Mm(DemoValidation.FootprintGap(glass, screen)) : "; no loupe on the desk (JewelryModels not present)"));
        DemoValidation.Check(seated && apart && clear && inside, label);
    }

    // The screen glows from its own colour map (the pack finish table: 0.45, GI None, so it
    // adds nothing to the bake) and the housing does not; no Light in the showroom or office
    // stands in for it. Monitor and keyboard each carry one bounds box (tools settle on them)
    // and, without a second UV set, take light probes.
    private static void InteriorScreen(Transform root)
    {
        const string pack = NightInterior.InteriorPack;
        if (!DemoAssetLibrary.Has(pack, NightInterior.MonitorName) || !DemoAssetLibrary.Has(pack, NightInterior.KeyboardName))
        {
            DemoValidation.Info("InteriorModels ComputerMonitor or Keyboard not present: screen glow check skipped");
            return;
        }
        Transform monitor = root.Find("Office/" + NightInterior.MonitorName), keyboard = root.Find("Office/" + NightInterior.KeyboardName);
        bool ok = monitor != null && keyboard != null;
        int glowing = 0, dark = 0, lights = 0;
        if (monitor != null)
            foreach (Renderer r in monitor.GetComponentsInChildren<Renderer>(true))
                foreach (Material material in r.sharedMaterials)
                {
                    if (material == null) { ok = false; continue; }
                    if (!material.IsKeywordEnabled("_EMISSION")) { dark++; continue; }
                    glowing++;
                    ok &= material.mainTexture != null && material.GetTexture("_EmissionMap") == material.mainTexture
                        && material.globalIlluminationFlags == MaterialGlobalIlluminationFlags.None && material.GetColor("_EmissionColor").maxColorComponent > 0.01f;
                }
        foreach (string zone in new[] { "Showroom", "Office" })
        {
            Transform t = root.Find(zone);
            if (t != null) lights += t.GetComponentsInChildren<Light>(true).Length;
        }
        bool probes = true;
        foreach (Transform t in new[] { monitor, keyboard })
            if (t != null)
                foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>(true)) probes &= r.receiveGI == ReceiveGI.LightProbes;
        DemoValidation.Info("Monitor: emissive materials " + glowing + ", others " + dark + "; Light components in the showroom and office " + lights);
        DemoValidation.Check(ok && glowing == 1 && dark >= 1 && lights == 0 && InteriorSolid(monitor) && InteriorSolid(keyboard) && probes,
            "The monitor screen glows without a light and stays out of the bake; monitor and keyboard are solid");
    }

    // The extinguisher (Prop LODs) stands on the floor right of the office opening, its back
    // a few millimetres off the walnut skirting's front (a ray at 5 cm, inside the skirting's
    // 0.1 m), with one bounds box.
    private static void InteriorExtinguisher(Transform root)
    {
        const string pack = NightInterior.InteriorPack, name = NightInterior.ExtinguisherName;
        if (!DemoAssetLibrary.Has(pack, name))
        {
            DemoValidation.Info("InteriorModels FireExtinguisher not present: extinguisher check skipped");
            return;
        }
        Transform extinguisher = root.Find("Showroom/" + name);
        Bounds b = default;
        bool ok = extinguisher != null && InteriorBox(extinguisher, out b) && NightInterior.Props.Contains("Showroom/" + name);
        float gap = float.NaN;
        Vector3 face = Vector3.zero;
        if (ok && DemoValidation.TriangleRay(new Ray(new Vector3(b.center.x, 0.05f, b.max.z - 0.01f), Vector3.forward), 0.05f, InteriorMeshes(root.Find("Architecture/Skirting")), out float d, out face))
            gap = d - 0.01f;
        int levels = 1;
        foreach (string suffix in new[] { "_LOD1", "_LOD2" })
            if (DemoAssetLibrary.Has(pack, name + suffix)) levels++;
        LODGroup group = extinguisher != null ? extinguisher.GetComponent<LODGroup>() : null;
        bool lods = levels < 2 ? group == null : group != null && group.lodCount == levels;
        if (lods && group != null)
        {
            float[] heights = DemoAssetLibrary.Transitions(DemoAssetLibrary.LodClass.Prop, levels);
            LOD[] placed = group.GetLODs();
            for (int i = 0; i < levels; i++) lods &= Mathf.Abs(placed[i].screenRelativeTransitionHeight - heights[i]) < 1e-4f && placed[i].renderers.Length > 0;
        }
        if (ok)
            DemoValidation.Info("Extinguisher: back " + DemoValidation.Mm(gap) + " off the skirting, base " + DemoValidation.Mm(b.min.y) + ", x " + b.min.x.ToString("0.000") + ".."
                + b.max.x.ToString("0.000") + " (opening half-width " + L.OfficeOpeningHalf.ToString("0.00") + "), " + levels + " LOD levels");
        else DemoValidation.Info("Extinguisher missing from Showroom or from NightInterior.Props");
        DemoValidation.Check(ok && gap >= 0.002f && gap <= 0.012f && Mathf.Abs(face.z) > 0.99f && b.min.y >= -0.002f && b.min.y <= 0.003f
            && (b.min.x >= L.OfficeOpeningHalf + 0.05f || b.max.x <= -L.OfficeOpeningHalf - 0.05f) && InteriorSolid(extinguisher) && lods,
            "The extinguisher stands on the floor a few millimetres off the skirting, clear of the office opening");
    }

    // The clock hangs on the lintel over the office opening, its back about 1 mm off the
    // partition's showroom face and its dial towards the shop, below the ceiling, with no
    // collider; its top level is the WallClock_LOD1 file (Tiny class) when that file exists.
    private static void InteriorClock(Transform root)
    {
        const string pack = NightInterior.InteriorPack, name = NightInterior.ClockName;
        if (!DemoAssetLibrary.Has(pack, name))
        {
            DemoValidation.Info("InteriorModels WallClock not present: clock check skipped");
            return;
        }
        Transform clock = root.Find("Showroom/" + name);
        Bounds b = default;
        bool ok = clock != null && InteriorBox(clock, out b) && NightInterior.Props.Contains("Showroom/" + name);
        float back = float.NaN;
        Vector3 wall = Vector3.zero;
        if (ok && DemoValidation.TriangleRay(new Ray(new Vector3(b.center.x, b.center.y, b.max.z), Vector3.forward), 0.02f, InteriorMeshes(root.Find("Architecture/Walls")), out float d, out wall))
            back = d;
        bool lod1 = true;
        string topFile = "none";
        if (ok && DemoAssetLibrary.Has(pack, name + "_LOD1"))
        {
            Transform top = clock.Find("LOD0");
            int meshes = 0;
            if (top != null)
                foreach (MeshFilter f in top.GetComponentsInChildren<MeshFilter>(true))
                {
                    meshes++;
                    string file = f.sharedMesh != null ? System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(f.sharedMesh)) : "";
                    topFile = file;
                    lod1 &= file == name + "_LOD1";
                }
            lod1 &= meshes > 0;
        }
        else if (ok) DemoValidation.Info("WallClock_LOD1 not present: the clock's top level is the full model");
        if (ok)
            DemoValidation.Info("Wall clock: back " + DemoValidation.Mm(back) + " off the partition, y " + b.min.y.ToString("0.000") + ".." + b.max.y.ToString("0.000")
                + " (opening " + L.OfficeOpeningHeight.ToString("0.00") + ", ceiling " + L.CeilingY.ToString("0.00") + "), top level " + topFile);
        else DemoValidation.Info("Wall clock missing from Showroom or from NightInterior.Props");
        DemoValidation.Check(ok && back <= 0.003f && Mathf.Abs(wall.z) > 0.99f && b.min.y >= L.OfficeOpeningHeight + 0.02f && b.max.y <= L.CeilingY - 0.02f
            && Mathf.Abs(b.center.x) < L.OfficeOpeningHalf && clock.GetComponentsInChildren<Collider>(true).Length == 0
            && Vector3.Dot(clock.forward, Vector3.back) > 0.99f && lod1,
            "The wall clock hangs flat on the lintel above the office opening, its top level the LOD1 file");
    }

    // The four diffusers hang under Architecture/Ceiling (hidden with it on the overview and
    // the map), flush with the slab's underside, clear of every downlight and track rail, cast
    // no shadow and are never occluders (the slab is also the roof the map looks through).
    private static void InteriorDiffusers(Transform root)
    {
        const string label = "Ceiling diffusers sit flush under the ceiling between the downlights and cast no shadow";
        if (!DemoAssetLibrary.Has(NightInterior.InteriorPack, NightInterior.DiffuserModel))
        {
            DemoValidation.Info("InteriorModels CeilingDiffuser not present: diffuser check skipped");
            return;
        }
        Transform ceiling = root.Find(L.CeilingPath);
        if (ceiling == null) DemoValidation.Check(false, label);
        List<DemoValidation.RayMesh> slab = InteriorMeshes(ceiling.Find("CeilingSlab"));
        List<Rect> downlights = new List<Rect>(), tracks = new List<Rect>();
        foreach (Transform t in ceiling)
        {
            Bounds fitting = default;
            bool downlight = t.name.StartsWith("Downlight_", StringComparison.Ordinal), track = t.name.StartsWith("TrackLight_", StringComparison.Ordinal);
            if ((downlight || track) && InteriorBox(t, out fitting)) (downlight ? downlights : tracks).Add(DemoValidation.Footprint(fitting));
        }
        bool ok = downlights.Count > 0 && tracks.Count > 0;
        float worst = 0, nearDownlight = float.MaxValue, nearTrack = float.MaxValue;
        for (int i = 0; i < NightInterior.Diffusers.Length; i++)
        {
            Transform diffuser = ceiling.Find(NightInterior.DiffuserPrefix + i);
            Bounds b = default;
            if (diffuser == null || !InteriorBox(diffuser, out b) || !NightInterior.Props.Contains(L.CeilingPath + "/" + NightInterior.DiffuserPrefix + i))
            {
                ok = false;
                DemoValidation.Info("Ceiling diffuser missing: " + NightInterior.DiffuserPrefix + i);
                continue;
            }
            float flush = float.NaN;
            if (DemoValidation.TriangleRay(new Ray(new Vector3(b.center.x, b.min.y, b.center.z), Vector3.up), 0.2f, slab, out float up, out _)) flush = b.max.y - (b.min.y + up);
            ok &= Mathf.Abs(flush) <= 0.003f;
            worst = float.IsNaN(flush) ? float.NaN : Mathf.Max(worst, Mathf.Abs(flush));
            Rect area = DemoValidation.Footprint(b);
            foreach (Rect r in downlights) nearDownlight = Mathf.Min(nearDownlight, DemoValidation.FootprintGap(area, r));
            foreach (Rect r in tracks) nearTrack = Mathf.Min(nearTrack, DemoValidation.FootprintGap(area, r));
            foreach (MeshRenderer r in diffuser.GetComponentsInChildren<MeshRenderer>(true))
                ok &= r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off
                    && (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.OccluderStatic) == 0;
        }
        DemoValidation.Info("Ceiling diffusers: largest offset from the slab underside " + DemoValidation.Mm(worst) + "; nearest downlight " + nearDownlight.ToString("0.00")
            + " m, nearest track rail " + nearTrack.ToString("0.00") + " m");
        DemoValidation.Check(ok && nearDownlight >= 0.4f && nearTrack >= 0.3f, label);
    }

    // ---------------------------------------------------------------- wall graphics

    // One of NightInterior's three wall-graphics meshes: the zone NightInterior.Graphics files
    // its pictures under, its renderer, the picture material and, in world space, the corners
    // (three per triangle) and UVs of its picture submesh and the corners of its gilt submesh.
    private sealed class InteriorSheet
    {
        public string Zone;
        public MeshRenderer Renderer;
        public Material Picture;
        public readonly List<Vector3> Corners = new List<Vector3>(), Gilt = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
    }

    // Showroom/WallGraphics, Office/WallGraphics and Exterior/Street/StreetGraphics, those built.
    private static List<InteriorSheet> InteriorSheets(Transform root)
    {
        List<InteriorSheet> sheets = new List<InteriorSheet>();
        string[] zones = { "Showroom", "Office", NightInterior.StreetZone };
        string[] names = { NightInterior.GraphicsName, NightInterior.GraphicsName, NightInterior.StreetGraphicsName };
        for (int i = 0; i < zones.Length; i++)
        {
            Transform t = root.Find(zones[i] + "/" + names[i]);
            MeshRenderer renderer = t != null ? t.GetComponent<MeshRenderer>() : null;
            MeshFilter filter = t != null ? t.GetComponent<MeshFilter>() : null;
            if (renderer == null || filter == null || filter.sharedMesh == null) continue;
            InteriorSheet sheet = new InteriorSheet { Zone = zones[i], Renderer = renderer };
            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;
            Matrix4x4 toWorld = t.localToWorldMatrix;
            Material[] materials = renderer.sharedMaterials;
            for (int sub = 0; sub < mesh.subMeshCount && sub < materials.Length; sub++)
            {
                Material m = materials[sub];
                bool picture = m != null && (m.name == NightInterior.InteriorMaterialName || m.name == NightInterior.StreetMaterialName);
                bool gilt = m != null && m.name == NightInterior.GiltMaterialName;
                if (!picture && !gilt) continue;
                if (picture) sheet.Picture = m;
                foreach (int index in mesh.GetTriangles(sub))
                {
                    Vector3 p = toWorld.MultiplyPoint3x4(vertices[index]);
                    if (gilt) { sheet.Gilt.Add(p); continue; }
                    sheet.Corners.Add(p);
                    sheet.Uvs.Add(index < uv.Length ? uv[index] : Vector2.zero);
                }
            }
            sheets.Add(sheet);
        }
        return sheets;
    }

    // What a picture's ray may land on: every rendered mesh of the store except the graphics
    // meshes themselves and the camera-facing glow cards.
    private static List<DemoValidation.RayMesh> InteriorSurfaces(Transform root, List<InteriorSheet> sheets)
    {
        Transform glow = root.Find(NightLighting.GroupName + "/" + NightLighting.GlowGroupName);
        HashSet<Transform> own = new HashSet<Transform>();
        foreach (InteriorSheet s in sheets) own.Add(s.Renderer.transform);
        return InteriorMeshes(root, f => !own.Contains(f.transform) && (glow == null || !f.transform.IsChildOf(glow)));
    }

    // Every picture lies parallel to the wall, glass, pier, shutter or facade behind it, 0.5 to
    // 70 mm in front (stickers 1 mm, sheets 2-12 mm, the lightbox face 60 mm), facing away from
    // it, with U running to the viewer's right and V up (unmirrored from the front). Each
    // triangle's centroid casts a ray back along its winding normal. No graphic is fixed to a
    // lamp post: the lost-cat flyer, which the README tapes to one, is a flat sheet on the right
    // pier in every build (InteriorFlyer), so it takes this test like the rest.
    private static void InteriorGraphicsFlat(Transform root, List<InteriorSheet> sheets)
    {
        if (sheets.Count == 0)
        {
            DemoValidation.Info("No wall graphics placed (WallGraphics not present): flatness check skipped");
            return;
        }
        List<DemoValidation.RayMesh> surfaces = InteriorSurfaces(root, sheets);
        bool ok = true;
        foreach (InteriorSheet s in sheets)
        {
            float near = float.MaxValue, far = 0;
            int orphans = 0;
            for (int t = 0; t + 2 < s.Corners.Count; t += 3)
            {
                Vector3 p0 = s.Corners[t], p1 = s.Corners[t + 1], p2 = s.Corners[t + 2];
                if (InteriorGraphicAt(s.Zone, (p0 + p1 + p2) / 3, 0.001f, 0.002f, false) < 0) { orphans++; continue; }
                Vector3 n = Vector3.Cross(p1 - p0, p2 - p0).normalized;
                if (Mathf.Abs(n.y) < 0.5f)
                {
                    Vector3 right = Vector3.Cross(Vector3.up, -n);
                    ok &= InteriorTangents(p0, p1, p2, s.Uvs[t], s.Uvs[t + 1], s.Uvs[t + 2], out Vector3 du, out Vector3 dv)
                        && Vector3.Dot(du, right) > 0 && Vector3.Dot(dv, Vector3.up) > 0;
                }
                bool hit = DemoValidation.TriangleRay(new Ray((p0 + p1 + p2) / 3, -n), 0.08f, surfaces, out float d, out Vector3 behind);
                ok &= hit && d >= 0.0005f && d <= 0.07f && Mathf.Abs(Vector3.Dot(behind, n)) > 0.98f;
                if (hit) { near = Mathf.Min(near, d); far = Mathf.Max(far, d); }
            }
            ok &= orphans == 0;
            DemoValidation.Info(s.Zone + " wall graphics: " + s.Corners.Count / 6 + " pictures, " + (near <= far ? DemoValidation.Mm(near) + " to " + DemoValidation.Mm(far) : "no surface hit")
                + " off their surfaces" + (orphans > 0 ? "; " + orphans + " picture triangles outside every placed graphic" : ""));
        }
        DemoValidation.Check(ok, "Wall graphics lie flat 0.5 to 70 mm in front of their surfaces, facing out and unmirrored");
    }

    // The lost-cat flyer (street page), which the README tapes to a lamp post, is a flat sheet
    // on the right pier's street face in every build: every post here is about 0.1 m across
    // (the lantern's round 88 mm, the KayKit octagon, the stand-in's box), so the 0.23 m card
    // would stand off it either side. Rays back from its centre and from each corner (5 mm in)
    // all land on the facade cladding a sheet's few millimetres behind and parallel to it, so
    // none of the card is in the air; it is right of the shopfront (the left pier carries the
    // CCTV sign) and more than 0.2 m from every built lamp post's axis.
    private static void InteriorFlyer(Transform root, List<InteriorSheet> sheets)
    {
        const string cell = "Flyer_LostCat", label = "The lost-cat flyer lies flat on the right pier's stone, clear of the lamp posts";
        Dictionary<string, Rect> cells = InteriorLayout(NightInterior.StreetAtlas, out _);
        Rect uv = default;
        if (cells == null || !cells.TryGetValue(cell, out uv) || uv.width <= 0 || uv.height <= 0 || root.Find(NightInterior.StreetZone) == null)
        {
            DemoValidation.Info(cells == null ? "WallGraphics street page or layout not present: lost-cat flyer check skipped"
                : uv.width <= 0 || uv.height <= 0 ? cell + " not in the street layout: lost-cat flyer check skipped"
                : NightInterior.StreetZone + " not built: lost-cat flyer check skipped");
            return;
        }
        int index = NightInterior.Graphics.FindIndex(x => x.Zone == NightInterior.StreetZone && x.Name == cell);
        InteriorSheet sheet = sheets.Find(x => x.Zone == NightInterior.StreetZone);
        List<Vector3> corners = new List<Vector3>();
        if (index >= 0 && sheet != null)
            for (int t = 0; t + 2 < sheet.Corners.Count; t += 3)
                if (InteriorGraphicAt(sheet.Zone, (sheet.Corners[t] + sheet.Corners[t + 1] + sheet.Corners[t + 2]) / 3, 0.001f, 0.002f, false) == index)
                    corners.AddRange(sheet.Corners.GetRange(t, 3));
        if (corners.Count != 6)
        {
            DemoValidation.Info("Lost-cat flyer: placed " + (index >= 0) + ", street graphics mesh " + (sheet != null) + ", picture triangles " + corners.Count / 3 + " (2 expected)");
            DemoValidation.Check(false, label);
        }
        Bounds card = new Bounds(corners[0], Vector3.zero);
        foreach (Vector3 p in corners) card.Encapsulate(p);
        Vector3 facing = Vector3.Cross(corners[1] - corners[0], corners[2] - corners[0]).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, -facing).normalized;

        // The centre and the quad's four corners, each pulled 5 mm in along the card's width and height.
        List<Vector3> origins = new List<Vector3> { card.center };
        foreach (Vector3 p in corners)
        {
            Vector3 local = p - card.center;
            Vector3 o = p - right * (Mathf.Sign(Vector3.Dot(local, right)) * 0.005f) - Vector3.up * (Mathf.Sign(local.y) * 0.005f);
            if (!origins.Exists(x => (x - o).sqrMagnitude < 1e-6f)) origins.Add(o);
        }
        List<DemoValidation.RayMesh> stone = InteriorMeshes(root.Find("Facade/Cladding"));
        int backed = 0;
        float near = float.MaxValue, far = 0;
        foreach (Vector3 o in origins)
        {
            if (!DemoValidation.TriangleRay(new Ray(o, -facing), 0.08f, stone, out float d, out Vector3 behind) || Mathf.Abs(Vector3.Dot(behind, facing)) <= 0.98f) continue;
            backed++;
            near = Mathf.Min(near, d); far = Mathf.Max(far, d);
        }
        float post = float.MaxValue;
        Transform posts = root.Find("Exterior/Streetlights");
        if (posts != null)
            foreach (Transform lamp in posts)
                post = Mathf.Min(post, new Vector2(card.center.x - lamp.position.x, card.center.z - lamp.position.z).magnitude);
        DemoValidation.Info("Lost-cat flyer: " + InteriorSize(card) + " m centred at x " + card.center.x.ToString("0.000") + ", y " + card.center.y.ToString("0.000")
            + ", z " + card.center.z.ToString("0.000") + "; " + backed + " of " + origins.Count + " rays on the facade cladding, "
            + (near <= far ? DemoValidation.Mm(near) + " to " + DemoValidation.Mm(far) : "none") + " behind; nearest lamp post axis "
            + (post < float.MaxValue ? post.ToString("0.00") + " m" : "none (no Exterior/Streetlights)"));
        DemoValidation.Check(backed == origins.Count && near >= 0.001f && far <= 0.012f && card.center.x > L.MaxX && post > 0.2f, label);
    }

    // Each picture quad maps exactly the cell of the graphic it shows, read from the layout as
    // it is on disk, and has as many page pixels per metre along U as along V: nothing is
    // stretched (the street page is twice as wide as it is high) or reaches into a neighbour cell.
    private static void InteriorGraphicsCells(List<InteriorSheet> sheets)
    {
        if (sheets.Count == 0)
        {
            DemoValidation.Info("No wall graphics placed (WallGraphics not present): atlas cell check skipped");
            return;
        }
        Dictionary<string, Rect> inside = InteriorLayout(NightInterior.InteriorAtlas, out Texture2D insidePage);
        Dictionary<string, Rect> outside = InteriorLayout(NightInterior.StreetAtlas, out Texture2D outsidePage);
        bool ok = true;
        float worst = 0;
        foreach (InteriorSheet s in sheets)
        {
            bool street = s.Zone == NightInterior.StreetZone;
            Dictionary<string, Rect> cells = street ? outside : inside;
            Texture2D page = street ? outsidePage : insidePage;
            if (cells == null || page == null) { ok = false; continue; }
            Dictionary<int, int> triangles = new Dictionary<int, int>();
            for (int t = 0; t + 2 < s.Corners.Count; t += 3)
            {
                int g = InteriorGraphicAt(s.Zone, (s.Corners[t] + s.Corners[t + 1] + s.Corners[t + 2]) / 3, 0.001f, 0.002f, false);
                if (g < 0 || !cells.TryGetValue(NightInterior.Graphics[g].Name, out Rect cell)) { ok = false; continue; }
                triangles[g] = (triangles.TryGetValue(g, out int counted) ? counted : 0) + 1;
                for (int k = 0; k < 3; k++)
                {
                    Vector2 uv = s.Uvs[t + k];
                    ok &= uv.x >= cell.xMin - 1e-4f && uv.x <= cell.xMax + 1e-4f && uv.y >= cell.yMin - 1e-4f && uv.y <= cell.yMax + 1e-4f;
                }
                if (!InteriorTangents(s.Corners[t], s.Corners[t + 1], s.Corners[t + 2], s.Uvs[t], s.Uvs[t + 1], s.Uvs[t + 2], out Vector3 du, out Vector3 dv)
                    || du.sqrMagnitude <= 0 || dv.sqrMagnitude <= 0)
                {
                    ok = false;
                    continue;
                }
                worst = Mathf.Max(worst, Mathf.Abs(page.width / du.magnitude / (page.height / dv.magnitude) - 1));
            }
            // Exactly one quad for every graphic the zone records.
            int records = 0;
            for (int i = 0; i < NightInterior.Graphics.Count; i++)
            {
                if (NightInterior.Graphics[i].Zone != s.Zone) continue;
                records++;
                ok &= triangles.TryGetValue(i, out int count) && count == 2;
            }
            ok &= triangles.Count == records;
        }
        DemoValidation.Info("Wall graphics: largest U/V pixel-density mismatch " + (worst * 100).ToString("0.00") + " %");
        DemoValidation.Check(ok && worst <= 0.02f, "Wall graphics sample only their own atlas cell at its aspect");
    }

    // With the interior page Heda's still life hangs where the rear sign frame's emblem was and
    // the Aurel & Finch lightbox where the counter frame's was: the frame's Body is gone and a
    // picture fills its place on the partition. Without the page, its layout or the cell, the
    // frame keeps its emblem; a user-converted sign frame (no Body to replace) is left alone.
    private static void InteriorGraphicsReplace(Transform root, List<InteriorSheet> sheets)
    {
        if (DemoAssetLibrary.Has(StoreKit.OverridePack, "sign_frame"))
        {
            DemoValidation.Info("StoreKit sign_frame override present: the rear and counter frames keep the user model; replacement check skipped");
            return;
        }
        Dictionary<string, Rect> cells = InteriorLayout(NightInterior.InteriorAtlas, out _);
        Transform showroom = root.Find("Showroom");
        InteriorSheet sheet = sheets.Find(x => x.Zone == "Showroom");
        string[] frames = { "Sign_Rear", "Sign_Counter" }, pictures = { "Art_Heda_BreakfastWithLobster", "Lightbox_AurelFinchWide" };
        bool ok = showroom != null;
        for (int i = 0; ok && i < frames.Length; i++)
        {
            Transform frame = showroom.Find(frames[i]);
            if (frame == null) { ok = false; break; }
            if (cells == null || !cells.TryGetValue(pictures[i], out Rect uv) || uv.width <= 0 || uv.height <= 0)
            {
                ok &= frame.Find("Body") != null;
                if (cells != null) DemoValidation.Info(pictures[i] + " not in the interior layout: " + frames[i] + " keeps its emblem");
                continue;
            }
            bool filled = false;
            float nearest = float.MaxValue;
            if (sheet != null)
                for (int t = 0; t + 2 < sheet.Corners.Count; t += 3)
                {
                    Vector3 c = (sheet.Corners[t] + sheet.Corners[t + 1] + sheet.Corners[t + 2]) / 3;
                    if (Mathf.Abs(c.z - NightInterior.ShowroomFace) > 0.07f) continue;
                    float away = new Vector2(c.x - frame.position.x, c.y - frame.position.y).magnitude;
                    nearest = Mathf.Min(nearest, away);
                    filled |= away <= 0.6f;
                }
            string picture = pictures[i];
            bool listed = NightInterior.Graphics.Exists(x => x.Zone == "Showroom" && x.Name == picture);
            DemoValidation.Info(frames[i] + ": Body " + (frame.Find("Body") != null ? "kept" : "removed") + ", nearest picture triangle on the partition "
                + (nearest < float.MaxValue ? nearest.ToString("0.00") + " m" : "none") + " from the frame");
            ok &= frame.Find("Body") == null && filled && listed;
        }
        if (cells != null)
        {
            ok &= sheet != null && (sheet.Gilt.Count > 0) == NightInterior.Graphics.Exists(x => x.Zone == "Showroom" && x.Frame == NightInterior.GraphicFrame.Gilt);
            DemoValidation.Check(ok, "The still life and the lightbox take the rear and counter frames' place");
        }
        else
        {
            DemoValidation.Info("WallGraphics interior page or layout not present: the rear and counter frames keep their emblem");
            ok &= showroom != null && showroom.Find(NightInterior.GraphicsName) == null;
            DemoValidation.Check(ok, "Rear and counter sign frames keep their emblem without the WallGraphics atlas");
        }
    }

    // The three meshes draw with the interior page, the street page, the gilt moulding and
    // FrameMetal (lightbox sides, clip frame) only; the pages alpha-test at the cutoff their
    // import keeps coverage for, with no normal map and no emission (the shop is shut), and
    // nothing casts a shadow. One picture quad per placed graphic and 40 gilt triangles per
    // framed painting (four mitred bars of five quads), whose face stands 3 cm off the wall.
    private static void InteriorGraphicsMaterials(Transform root, List<InteriorSheet> sheets)
    {
        Dictionary<string, Rect> inside = InteriorLayout(NightInterior.InteriorAtlas, out _), outside = InteriorLayout(NightInterior.StreetAtlas, out _);
        if (sheets.Count == 0 && inside == null && outside == null)
        {
            DemoValidation.Info("WallGraphics not present: wall-graphics material check skipped");
            return;
        }
        // A sheet exactly where graphics were recorded, and one wherever its page was synced.
        bool ok = true;
        foreach (string zone in new[] { "Showroom", "Office", NightInterior.StreetZone })
            ok &= sheets.Exists(x => x.Zone == zone) == NightInterior.Graphics.Exists(x => x.Zone == zone);
        ok &= (inside == null || sheets.Exists(x => x.Zone == "Showroom"))
            && (outside == null || root.Find(NightInterior.StreetZone) == null || sheets.Exists(x => x.Zone == NightInterior.StreetZone));
        List<DemoValidation.RayMesh> surfaces = InteriorSurfaces(root, sheets);
        int faces = 0;
        float near = float.MaxValue, far = 0;
        foreach (InteriorSheet s in sheets)
        {
            bool street = s.Zone == NightInterior.StreetZone;
            ok &= s.Renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (Material m in s.Renderer.sharedMaterials)
                ok &= m != null && (m.name == NightInterior.InteriorMaterialName || m.name == NightInterior.StreetMaterialName
                    || m.name == NightInterior.GiltMaterialName || m == StoreKit.FrameMetal);
            Material picture = s.Picture;
            string page = DemoAssetLibrary.Path(NightInterior.GraphicsPack, (street ? NightInterior.StreetAtlas : NightInterior.InteriorAtlas) + "_Color.png");
            string shown = picture != null && picture.mainTexture != null ? AssetDatabase.GetAssetPath(picture.mainTexture) : null;
            float cutoff = shown != null ? PackImport.CutoffOf(shown) : 0;
            ok &= picture != null && shown == page && picture.name == (street ? NightInterior.StreetMaterialName : NightInterior.InteriorMaterialName)
                && picture.IsKeywordEnabled("_ALPHATEST_ON") && picture.renderQueue == 2450 && !picture.IsKeywordEnabled("_NORMALMAP")
                && !picture.IsKeywordEnabled("_EMISSION") && picture.globalIlluminationFlags == MaterialGlobalIlluminationFlags.EmissiveIsBlack
                && (cutoff > 0 ? Mathf.Approximately(picture.GetFloat("_Cutoff"), cutoff) : picture.GetFloat("_Cutoff") > 0);
            int placed = 0, framed = 0, front = 0;
            foreach (NightInterior.PlacedGraphic g in NightInterior.Graphics)
            {
                if (g.Zone != s.Zone) continue;
                placed++;
                if (g.Frame == NightInterior.GraphicFrame.Gilt) framed++;
            }
            ok &= s.Corners.Count == placed * 6 && s.Gilt.Count == framed * 40 * 3;
            // The moulding's face (the triangles facing the way the painting does) stands GiltDepth off the wall.
            for (int t = 0; t + 2 < s.Gilt.Count; t += 3)
            {
                Vector3 p0 = s.Gilt[t], p1 = s.Gilt[t + 1], p2 = s.Gilt[t + 2], c = (p0 + p1 + p2) / 3;
                Vector3 n = Vector3.Cross(p1 - p0, p2 - p0).normalized;
                int g = InteriorGraphicAt(s.Zone, c, NightInterior.GiltBar + 0.005f, NightInterior.GiltDepth + 0.005f, true);
                if (g < 0) { ok = false; continue; }
                if (Vector3.Dot(n, NightInterior.Graphics[g].Normal.normalized) <= 0.99f) continue;
                front++;
                bool hit = DemoValidation.TriangleRay(new Ray(c, -n), 0.08f, surfaces, out float d, out _);
                ok &= hit && Mathf.Abs(d - NightInterior.GiltDepth) <= 0.002f;
                if (hit) { near = Mathf.Min(near, d); far = Mathf.Max(far, d); }
            }
            // Per bar the face rectangle (two triangles) and the real triangle of each mitre.
            ok &= front == framed * 16;
            faces += front;
        }
        List<string> names = new List<string>();
        foreach (NightInterior.PlacedGraphic g in NightInterior.Graphics) names.Add(g.Zone + ": " + g.Name + " (" + g.Frame + ")");
        DemoValidation.Info("Wall graphics placed: " + (names.Count > 0 ? string.Join(", ", names) : "none"));
        DemoValidation.Info("Gilt moulding: " + faces + " face triangles, " + (near <= far ? DemoValidation.Mm(near) + " to " + DemoValidation.Mm(far) : "none") + " off the wall");
        DemoValidation.Check(ok, "Wall graphics use the two atlas pages and the gilt moulding, alpha-tested, unlit and shadowless");
    }

    // Koda's decision 1 (country and police service) is open, so nothing NightInterior places is
    // police-specific or ties the shop to one country: WallGraphics' police notice and
    // InteriorModels' US pull-station call point stay unplaced.
    private static void InteriorNeutral(Transform root)
    {
        int police = 0, callPoints = 0;
        foreach (NightInterior.PlacedGraphic g in NightInterior.Graphics)
            if (g.Name.IndexOf("Police", StringComparison.OrdinalIgnoreCase) >= 0) police++;
        foreach (string zone in new[] { "Showroom", "Office", NightInterior.StreetZone })
        {
            Transform t = root.Find(zone);
            if (t == null) continue;
            foreach (Transform part in t.GetComponentsInChildren<Transform>(true))
                if (part.name.StartsWith("FireAlarmCallPoint", StringComparison.Ordinal)) callPoints++;
        }
        DemoValidation.Info("Country-neutral interior: " + NightInterior.Graphics.Count + " wall graphics, " + police + " police-specific; " + callPoints + " call points");
        DemoValidation.Check(police == 0 && callPoints == 0, "Interior graphics and fittings stay country-neutral: no police notice, no pull-station call point");
    }

    // ---------------------------------------------------------------- helpers

    // Index in NightInterior.Graphics of the zone's placed graphic (only gilt-framed ones when
    // giltOnly) whose picture rectangle, grown by margin and within depth of its plane, holds p;
    // -1 when none does.
    private static int InteriorGraphicAt(string zone, Vector3 p, float margin, float depth, bool giltOnly)
    {
        List<NightInterior.PlacedGraphic> placed = NightInterior.Graphics;
        for (int i = 0; i < placed.Count; i++)
        {
            NightInterior.PlacedGraphic g = placed[i];
            if (g.Zone != zone || (giltOnly && g.Frame != NightInterior.GraphicFrame.Gilt)) continue;
            Vector3 n = g.Normal.normalized, right = Vector3.Cross(Vector3.up, -n).normalized, local = p - g.Centre;
            if (Mathf.Abs(Vector3.Dot(local, n)) <= depth && Mathf.Abs(Vector3.Dot(local, right)) <= g.Size.x * 0.5f + margin
                && Mathf.Abs(local.y) <= g.Size.y * 0.5f + margin) return i;
        }
        return -1;
    }

    // An atlas page of WallGraphics and its cell rects when both files were synced; null otherwise.
    private static Dictionary<string, Rect> InteriorLayout(string atlas, out Texture2D page)
    {
        page = DemoAssetLibrary.Texture(NightInterior.GraphicsPack, atlas + "_Color.png");
        if (page == null || AssetDatabase.LoadAssetAtPath<TextAsset>(DemoAssetLibrary.Path(NightInterior.GraphicsPack, atlas + ".json")) == null) return null;
        return DemoAssetLibrary.Atlas(NightInterior.GraphicsPack, atlas + ".json");
    }

    // dP/du and dP/dv of a triangle (the standard tangent frame); false when its UVs are degenerate
    // (a zero tangent passes, unlike in DecalTangents, so neither is shared).
    private static bool InteriorTangents(Vector3 p0, Vector3 p1, Vector3 p2, Vector2 w0, Vector2 w1, Vector2 w2, out Vector3 du, out Vector3 dv)
    {
        Vector3 e1 = p1 - p0, e2 = p2 - p0;
        Vector2 d1 = w1 - w0, d2 = w2 - w0;
        float det = d1.x * d2.y - d2.x * d1.y;
        du = dv = Vector3.zero;
        if (Mathf.Abs(det) < 1e-12f) return false;
        du = (e1 * d2.y - e2 * d1.y) / det;
        dv = (e2 * d1.x - e1 * d2.x) / det;
        return true;
    }

    // One BoxCollider on t itself, no other collider below it, covering t's renderers (shrunk
    // 1 cm) and at most 1 cm larger: DemoAssetLibrary.AddBoundsCollider's box.
    private static bool InteriorSolid(Transform t)
    {
        Bounds b = default;
        if (t == null || !InteriorBox(t, out b)) return false;
        Collider[] colliders = t.GetComponentsInChildren<Collider>(true);
        BoxCollider box = colliders.Length == 1 && colliders[0].transform == t ? colliders[0] as BoxCollider : null;
        if (box == null) return false;
        Bounds solid = DemoValidation.BoxColliderBounds(box);
        return InteriorContains(solid, b, 0.01f) && InteriorContains(InteriorGrow(b, 0.01f), solid, 0);
    }

    // World box of every renderer under t, inactive LOD levels included (as the builder measures:
    // DemoAssetLibrary.WorldBounds); false without t or a renderer.
    private static bool InteriorBox(Transform t, out Bounds b)
    {
        b = default;
        return t != null && DemoAssetLibrary.WorldBounds(t.gameObject, out b);
    }

    // inner, shrunk by shrink on every side, lies inside outer (0.1 mm tolerance, for the
    // imported models' rounding; DressingContains is exact, so neither is shared).
    private static bool InteriorContains(Bounds outer, Bounds inner, float shrink)
    {
        const float e = 1e-4f;
        Vector3 lo = inner.min + Vector3.one * shrink, hi = inner.max - Vector3.one * shrink;
        return lo.x >= outer.min.x - e && lo.y >= outer.min.y - e && lo.z >= outer.min.z - e
            && hi.x <= outer.max.x + e && hi.y <= outer.max.y + e && hi.z <= outer.max.z + e;
    }

    private static Bounds InteriorGrow(Bounds b, float margin)
    {
        b.Expand(2 * margin);
        return b;
    }

    // Every material draws in the opaque or cut-out queues.
    private static bool InteriorOpaque(Renderer r)
    {
        Material[] materials = r.sharedMaterials;
        if (materials.Length == 0) return false;
        foreach (Material m in materials)
            if (m == null || m.renderQueue >= 3000) return false;
        return true;
    }

    private static Rect InteriorGrowRect(Rect r, float margin) => Rect.MinMaxRect(r.xMin - margin, r.yMin - margin, r.xMax + margin, r.yMax + margin);

    private static bool InteriorInside(Rect inner, Rect outer)
        => inner.xMin >= outer.xMin - 0.001f && inner.yMin >= outer.yMin - 0.001f && inner.xMax <= outer.xMax + 0.001f && inner.yMax <= outer.yMax + 0.001f;

    private static string InteriorSize(Bounds b) => b.size.x.ToString("0.000") + " x " + b.size.y.ToString("0.000") + " x " + b.size.z.ToString("0.000");

    // Height of the floor top under a point, by a ray down from 5 cm; NaN when it misses.
    private static float InteriorFloorY(List<DemoValidation.RayMesh> floor, Vector3 at)
        => DemoValidation.TriangleRay(new Ray(new Vector3(at.x, 0.05f, at.z), Vector3.down), 0.5f, floor, out float d, out _) ? 0.05f - d : float.NaN;

    // ---------------------------------------------------------------- triangle rays

    // The active, enabled meshes under t that keep passes (all of them without keep), as lazy
    // DemoValidation.RayMeshes: boxed by their renderers and read on the first ray that reaches
    // that box, once per check pass.
    private static List<DemoValidation.RayMesh> InteriorMeshes(Transform t, Func<MeshFilter, bool> keep = null)
    {
        List<DemoValidation.RayMesh> list = new List<DemoValidation.RayMesh>();
        if (t == null) return list;
        foreach (MeshFilter filter in t.GetComponentsInChildren<MeshFilter>(false))
        {
            MeshRenderer r = filter.GetComponent<MeshRenderer>();
            if (filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0 || r == null || !r.enabled || (keep != null && !keep(filter))) continue;
            list.Add(DemoValidation.RayMeshOf(filter, true));
        }
        return list;
    }
}
#endif
