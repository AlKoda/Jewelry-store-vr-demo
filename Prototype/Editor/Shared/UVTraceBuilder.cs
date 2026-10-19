#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;

// Authoring for the UV trace demonstration: procedural 256 px alpha masks (a drop
// cluster, a wipe smear, a partial shoe sole) saved under UVGenerated, one
// CrimeScene/UVFluorescence material per mask, and Add, which lays a trace quad on a
// surface and records that surface for UVTrace's line-of-sight test. The night store's
// three examples are placeholders: what a trace means is the instructor's decision
// (Docs/NIGHT_ARRIVAL_PLAN.md), so they start hidden and nothing scores them.
public static class UVTraceBuilder
{
    public const string Folder = "Assets/CrimeSceneDemo/UVGenerated";
    public const string ShaderName = "CrimeScene/UVFluorescence";
    public const string ExamplesName = "UVTraceExamples";
    private const int Size = 256;
    public const float GlowIntensity = 0.9f;

    public enum Mask { Drops, Smear, SolePrint }

    // A trace quad lying on the surface at position (facing along normal), size in
    // metres (width, height) and spin in degrees about the normal. Not static: it
    // never takes part in the bake or static batching.
    public static UVTrace Add(string name, Transform parent, Vector3 position, Vector3 normal, Vector2 size, Mask mask, float spin = 0)
    {
        Material material = TraceMaterial(mask);
        if (material == null) return null;
        normal = normal.normalized;
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(parent, true);
        Vector3 up = Mathf.Abs(normal.y) > 0.9f ? Vector3.forward : Vector3.up;
        // Unity's quad faces its local -z: look along -normal so the face points out.
        quad.transform.SetPositionAndRotation(position + normal * 0.002f, Quaternion.LookRotation(-normal, up) * Quaternion.Euler(0, 0, spin));
        quad.transform.localScale = new Vector3(size.x, size.y, 1);
        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        GameObjectUtility.SetStaticEditorFlags(quad, 0);
        UVTrace trace = quad.AddComponent<UVTrace>();
        Physics.SyncTransforms();
        if (Physics.Raycast(position + normal * 0.05f, -normal, out RaycastHit hit, 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            trace.Surface = hit.collider;
        return trace;
    }

    // The night store's placeholder examples under root/UVTraceExamples, hidden:
    // a smear on the right door's push side, drops in front of the large safe and a
    // partial sole near the office opening. Returns the group (null without the shader).
    public static GameObject NightExamples(Transform root)
    {
        if (Shader.Find(ShaderName) == null)
        {
            Debug.LogError("UV traces need " + ShaderName + ": sync Prototype/Shaders into Assets/CrimeSceneDemo/Shaders.");
            return null;
        }
        Transform group = Group(ExamplesName, root);
        Transform leaf = root.Find("Facade/EntranceDoors/DoorRight");
        if (leaf != null)
            Add("UVTrace_DoorSmear", group, leaf.TransformPoint(NightStoreLayout.UVDoorSmearLocal), leaf.TransformDirection(Vector3.forward), new Vector2(0.07f, 0.22f), Mask.Smear);
        Add("UVTrace_SafeDrops", group, root.TransformPoint(NightStoreLayout.UVDrops), Vector3.up, new Vector2(0.3f, 0.3f), Mask.Drops, 20);
        Add("UVTrace_PartialSole", group, root.TransformPoint(NightStoreLayout.UVSole), Vector3.up, new Vector2(0.12f, 0.3f), Mask.SolePrint, -8);
        group.gameObject.SetActive(false);
        return group.gameObject;
    }

    public static Material TraceMaterial(Mask mask)
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null) return null;
        EnsureFolder("Assets/CrimeSceneDemo");
        EnsureFolder(Folder);
        string path = Folder + "/UVTrace_" + mask + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader) { name = "UVTrace_" + mask };
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = shader;
        m.mainTexture = MaskTexture(mask);
        // Kept below the LDR clip (max glow colour * intensity <= 1 in linear) so the
        // mask's rims and streaks stay distinct instead of flattening to one value.
        m.SetFloat("_Intensity", GlowIntensity);
        EditorUtility.SetDirty(m);
        return m;
    }

    // White RGB, the trace shape in alpha, mipmapped and clamped. Regenerated in place
    // so materials keep their reference.
    public static Texture2D MaskTexture(Mask mask)
    {
        EnsureFolder("Assets/CrimeSceneDemo");
        EnsureFolder(Folder);
        string path = Folder + "/UVMask_" + mask + ".asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        bool create = texture == null || !texture.isReadable || texture.width != Size || texture.height != Size;
        if (create) texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true, true) { name = "UVMask_" + mask };
        Color32[] pixels = new Color32[Size * Size];
        System.Random rng = new System.Random(31 + (int)mask);
        float[] alpha = mask == Mask.Drops ? Drops(rng) : mask == Mask.Smear ? Smear(rng) : Sole(rng);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha[i]) * 255));
        texture.SetPixels32(pixels);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Trilinear;
        texture.Apply(true, false);
        if (create)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(texture, path);
        }
        else EditorUtility.SetDirty(texture);
        AssetDatabase.SaveAssets();
        return texture;
    }

    // Drop cluster: a few round drops with slightly stronger (drying) rims and trailing
    // satellite droplets, kept inside the central 80 % of the quad.
    private static float[] Drops(System.Random rng)
    {
        float[] a = new float[Size * Size];
        for (int d = 0; d < 7; d++)
        {
            float cx = Range(rng, 0.32f, 0.68f), cy = Range(rng, 0.32f, 0.68f), r = Range(rng, 0.03f, 0.07f);
            Blob(a, cx, cy, r, r * Range(rng, 0.85f, 1.1f), 1f);
            float angle = Range(rng, 0, Mathf.PI * 2);
            for (int s = 0; s < 3; s++)
            {
                float k = r * (1.4f + s * 0.7f);
                Blob(a, cx + Mathf.Cos(angle) * k, cy + Mathf.Sin(angle) * k, r * 0.25f / (1 + s * 0.4f), r * 0.25f / (1 + s * 0.4f), 0.9f);
            }
        }
        return a;
    }

    // Wipe smear along v: a broad band whose streaks (finger or cloth drag) thin out
    // towards the far end, with a ragged edge.
    private static float[] Smear(System.Random rng)
    {
        float[] a = new float[Size * Size];
        float[] streak = new float[Size];
        for (int x = 0; x < Size; x++) streak[x] = 0.55f + 0.45f * Mathf.PerlinNoise(x * 0.09f, 3.7f);
        float seed = Range(rng, 0, 100);
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f) / Size, v = (y + 0.5f) / Size;
                float width = Mathf.Lerp(0.36f, 0.18f, v) + 0.05f * (Mathf.PerlinNoise(seed + v * 9, 1.3f) - 0.5f);
                float centre = 0.5f + 0.06f * Mathf.Sin(v * 4.5f);
                float across = 1 - Mathf.SmoothStep(0.75f, 1f, Mathf.Abs(u - centre) / width);
                float along = Mathf.SmoothStep(0.06f, 0.16f, v) * (1 - Mathf.SmoothStep(0.62f, 0.94f, v + 0.1f * Mathf.PerlinNoise(u * 12, seed)));
                a[y * Size + x] = across * along * Mathf.Lerp(streak[x], 1, 0.3f);
            }
        return a;
    }

    // Partial shoe sole along v (toe at the top): an outline of forefoot and heel with
    // a bar tread, transferred only partly (patchy, fading towards the heel).
    private static float[] Sole(System.Random rng)
    {
        float[] a = new float[Size * Size];
        float seed = Range(rng, 0, 100);
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f) / Size - 0.5f, v = (y + 0.5f) / Size;
                // Half-width of the sole outline at this height: forefoot, waist, heel.
                float half = v > 0.55f ? 0.4f * Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((v - 0.72f) / 0.24f, 2)))
                    : v > 0.35f ? 0.27f : 0.33f * Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((v - 0.2f) / 0.16f, 2)));
                if (Mathf.Abs(u) > half) continue;
                bool tread = Mathf.Repeat(v * 30 + Mathf.Abs(u) * 6, 1) < 0.55f;
                float edge = 1 - Mathf.SmoothStep(0.8f, 1f, Mathf.Abs(u) / Mathf.Max(half, 1e-3f));
                float transfer = Mathf.SmoothStep(0.25f, 0.6f, Mathf.PerlinNoise(seed + x * 0.035f, y * 0.035f) + v * 0.35f);
                a[y * Size + x] = (tread ? 1f : 0.25f) * edge * transfer;
            }
        return a;
    }

    // Soft elliptical drop with a slightly stronger rim, added with max.
    private static void Blob(float[] a, float cx, float cy, float rx, float ry, float strength)
    {
        int x0 = Mathf.Max(0, (int)((cx - rx) * Size) - 1), x1 = Mathf.Min(Size - 1, (int)((cx + rx) * Size) + 1);
        int y0 = Mathf.Max(0, (int)((cy - ry) * Size) - 1), y1 = Mathf.Min(Size - 1, (int)((cy + ry) * Size) + 1);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = ((x + 0.5f) / Size - cx) / rx, dy = ((y + 0.5f) / Size - cy) / ry;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r >= 1) continue;
                float value = strength * (1 - Mathf.SmoothStep(0.85f, 1f, r)) * (0.75f + 0.25f * Mathf.SmoothStep(0.55f, 0.9f, r));
                int i = y * Size + x;
                a[i] = Mathf.Max(a[i], value);
            }
    }

    private static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
}
#endif
