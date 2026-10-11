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
            DemoValidation.WriteHeader("Store kit gallery generation and Edit mode checks; 2 actual Unity rendered captures.");
            Camera camera = DemoValidation.CreatePreviewCamera(150);
            Check(true);
            // Not registered as the build scene, so running this in a working project keeps its build list.
            DemoValidation.SaveScene("StoreKitGallery", false);
            DemoValidation.Capture(camera, "store-kit-gallery.png", new Vector3(0, 11, -10), new Vector3(0, 0.5f, 5.5f));
            DemoValidation.Capture(camera, "store-kit-closeup.png", new Vector3(-6.5f, 1.7f, -3.5f), new Vector3(-6.5f, 0.8f, 0));
            DemoValidation.Finish(Key, true, null);
        }
        catch (Exception ex) { DemoValidation.Finish(Key, false, ex.ToString()); }
    }

    // For other validators: builds, checks and removes the gallery.
    public static void Check() { Check(false); }

    public static Transform Check(bool keepGallery)
    {
        Transform gallery = StoreKit.BuildGallery(null);
        try
        {
            FixtureChecks(gallery);
            HingeChecks(gallery);
            PaneChecks(gallery);
            EmissionChecks(gallery);
        }
        finally
        {
            if (!keepGallery) UnityEngine.Object.DestroyImmediate(gallery.gameObject);
        }
        return keepGallery ? gallery : null;
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
