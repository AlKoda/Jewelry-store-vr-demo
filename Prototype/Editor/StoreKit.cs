#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static DemoGeometry;

// Procedural low-poly fixture library for the night store. Every builder returns a
// root named after the fixture (snake_case, see Names) with its pivot at the bottom
// centre of the footprint and its front facing -z. Exceptions: wall-mounted pieces
// (sign_frame, exit_sign, alarm_keypad, security_camera) pivot at the bottom centre
// of their back, which sits on z = 0; ceiling pieces (track_light, downlight) pivot
// at the ceiling contact and stay entirely at or below y = 0. Door frames pivot at the bottom
// centre of the opening; every hinged leaf ("Door", "DoorLeft", "DoorRight") is a
// child whose pivot is on its hinge axis, so rotating it about local y swings it.
// Drawers ("Drawer", "Drawer0".."Drawer2") slide along local -z.
//
// Each rigid part is one KitMesh: one renderer with a submesh per material, metric
// UVs and lightmap UVs, saved under Folder. Materials come from Palette() and pick
// up tileable textures from the ReferenceTextures pack when present, else the
// GeneratedTextures stand-ins (walnut, brushed brass, marble).
//
// Instantiate(name, ...) first looks for a user-converted model at
// Prototype/ThirdParty/StoreKit/<name>.obj or .fbx (synced to
// Assets/CrimeSceneDemo/ThirdParty/StoreKit) and only builds the procedural fixture
// when there is none, so image-to-3D replacements need no code change.
public static partial class StoreKit
{
    public const string Folder = "Assets/CrimeSceneDemo/StoreKitGenerated";
    public const string OverridePack = "StoreKit";
    public const string TexturePack = "ReferenceTextures";

    public enum CaseGlass { Intact, FrontBroken, AllBroken }
    public enum Pane { Intact, Cracked, Broken }

    public static readonly string[] Names =
    {
        "display_case", "wall_case", "wall_display", "counter", "drawer_unit", "office_desk", "office_chair",
        "shelving_unit", "locker_cabinet", "safe_large", "safe_tall", "waiting_sofa", "side_table", "planter",
        "sign_frame", "storefront_doors", "office_door", "service_door", "window_module", "bollard", "exit_sign",
        "security_camera", "track_light", "downlight", "pos_terminal", "alarm_keypad"
    };

    private static readonly Dictionary<string, Func<Transform, GameObject>> Builders = new Dictionary<string, Func<Transform, GameObject>>
    {
        { "display_case", p => DisplayCase(p) }, { "wall_case", p => WallCase(p) }, { "wall_display", WallDisplay },
        { "counter", Counter }, { "drawer_unit", p => DrawerUnit(p) }, { "office_desk", p => OfficeDesk(p) },
        { "office_chair", p => OfficeChair(p) }, { "shelving_unit", ShelvingUnit }, { "locker_cabinet", LockerCabinet },
        { "safe_large", p => SafeLarge(p) }, { "safe_tall", p => SafeTall(p) }, { "waiting_sofa", WaitingSofa },
        { "side_table", SideTable }, { "planter", Planter }, { "sign_frame", SignFrame },
        { "storefront_doors", p => StorefrontDoors(p) }, { "office_door", p => OfficeDoor(p) },
        { "service_door", p => ServiceDoor(p) }, { "window_module", p => WindowModule(p) }, { "bollard", Bollard },
        { "exit_sign", ExitSign }, { "security_camera", SecurityCamera }, { "track_light", TrackLight },
        { "downlight", Downlight }, { "pos_terminal", PosTerminal }, { "alarm_keypad", AlarmKeypad }
    };

    // Triangle budgets for the procedural builds and the gallery variants (<name>__<variant>).
    private static readonly Dictionary<string, int> Budgets = new Dictionary<string, int>
    {
        { "display_case", 900 }, { "display_case__FrontBroken", 900 }, { "display_case__AllBroken", 900 },
        { "display_case__Drawer", 900 }, { "wall_case", 900 }, { "wall_case__AllBroken", 900 },
        { "wall_display", 400 }, { "counter", 400 }, { "drawer_unit", 450 }, { "office_desk", 400 },
        { "office_chair", 700 }, { "office_chair__Toppled", 700 }, { "shelving_unit", 400 }, { "locker_cabinet", 200 },
        { "safe_large", 900 }, { "safe_large__Closed", 900 }, { "safe_tall", 800 }, { "waiting_sofa", 400 },
        { "side_table", 300 }, { "planter", 300 }, { "sign_frame", 200 }, { "storefront_doors", 500 },
        { "storefront_doors__BrokenOpen", 700 }, { "office_door", 300 }, { "service_door", 300 },
        { "window_module", 300 }, { "window_module__Cracked", 500 }, { "window_module__Broken", 500 },
        { "bollard", 250 }, { "exit_sign", 100 }, { "security_camera", 250 }, { "track_light", 300 },
        { "downlight", 200 }, { "pos_terminal", 100 }, { "alarm_keypad", 200 }
    };

    public static int Budget(string entry) => Budgets.TryGetValue(entry, out int budget) ? budget
        : PropBudgets.TryGetValue(entry, out budget) ? budget : 0;

    // Gallery-only variants beside the defaults, so broken/open states are built and checked too.
    public static readonly string[] Variants =
    {
        "display_case__FrontBroken", "display_case__AllBroken", "display_case__Drawer", "wall_case__AllBroken",
        "office_chair__Toppled", "safe_large__Closed", "storefront_doors__BrokenOpen", "window_module__Cracked",
        "window_module__Broken"
    };

    // ---------------------------------------------------------------- palette

    public static Material Walnut { get; private set; }
    public static Material Suede { get; private set; }
    public static Material Velvet { get; private set; }
    public static Material Bronze { get; private set; }
    public static Material Stone { get; private set; }
    public static Material SafeSteel { get; private set; }
    public static Material FrameMetal { get; private set; }
    public static Material Lacquer { get; private set; }
    public static Material Fabric { get; private set; }
    public static Material Glass { get; private set; }
    public static Material SafeLining { get; private set; }
    public static Material PaintedGrey { get; private set; }
    public static Material Stainless { get; private set; }
    public static Material DeviceBlack { get; private set; }
    public static Material DeviceGrey { get; private set; }
    public static Material Foliage { get; private set; }
    public static Material Soil { get; private set; }
    public static Material GlassCrack { get; private set; }
    // Emitters: lenses, shelf strips and screens start off (black emission); the exit
    // sign and the keypad LED glow modestly. All are flagged for baked emission so the
    // lightmapper, not a realtime light, carries their light.
    public static Material LightLens { get; private set; }
    public static Material StripLight { get; private set; }
    public static Material DeviceScreen { get; private set; }
    public static Material ExitGreen { get; private set; }
    public static Material ExitLetters { get; private set; }
    public static Material AlarmLed { get; private set; }
    // Shell and street surfaces for the night store (NightStoreBuilder); metric UVs throughout.
    public static Material Floor { get; private set; }
    public static Material Plaster { get; private set; }
    public static Material CeilingPaint { get; private set; }
    public static Material Cladding { get; private set; }
    public static Material Pavement { get; private set; }
    public static Material Asphalt { get; private set; }
    public static Material Concrete { get; private set; }
    public static Material Render { get; private set; }
    public static Material RoadLine { get; private set; }
    public static Material WindowDark { get; private set; }
    // Neighbours' lit rooms: a baked emitter, so the street gets a little warm bounce for free.
    public static Material WindowLit { get; private set; }

    public static void Palette()
    {
        if (Walnut != null && Glass != null && Gold != null) return;
        EnsureFolder("Assets/CrimeSceneDemo");
        EnsureFolder(Folder);
        Walnut = Surface("KitWalnut", new Color(0.30f, 0.20f, 0.14f), 0.35f, 0, "Walnut", 0.6f);
        Suede = Surface("KitSuede", new Color(0.86f, 0.80f, 0.70f), 0.05f, 0, "SuedeCream", 0.25f);
        Velvet = Surface("KitVelvet", new Color(0.13f, 0.13f, 0.14f), 0.05f, 0, "VelvetCharcoal", 0.25f);
        Bronze = Surface("KitBronze", new Color(0.55f, 0.42f, 0.26f), 0.45f, 0.8f, "BronzeBrushed", 0.3f);
        Stone = Surface("KitStone", new Color(0.82f, 0.77f, 0.68f), 0.5f, 0, "StoneTop", 0.8f);
        // Dark finishes are powder-coated dielectrics: a dark albedo with high metallic
        // leaves almost no diffuse and a lower F0 than paint, so they read flat black at night.
        SafeSteel = Surface("KitSafeSteel", new Color(0.22f, 0.23f, 0.24f), 0.35f, 0.1f, "SafeSteel", 0.5f);
        FrameMetal = Surface("KitFrameMetal", new Color(0.16f, 0.16f, 0.17f), 0.45f, 0, null, 1);
        Lacquer = Surface("KitLacquer", new Color(0.84f, 0.81f, 0.74f), 0.4f, 0, null, 1);
        Fabric = Surface("KitFabric", new Color(0.16f, 0.16f, 0.17f), 0.1f, 0, null, 1);
        SafeLining = Surface("KitSafeLining", new Color(0.36f, 0.30f, 0.23f), 0.3f, 0.3f, null, 1);
        PaintedGrey = Surface("KitPaintedGrey", new Color(0.33f, 0.34f, 0.35f), 0.35f, 0.3f, null, 1);
        Stainless = Surface("KitStainless", new Color(0.62f, 0.62f, 0.60f), 0.55f, 0.8f, null, 1);
        DeviceBlack = Surface("KitDeviceBlack", new Color(0.10f, 0.10f, 0.11f), 0.4f, 0, null, 1);
        DeviceGrey = Surface("KitDeviceGrey", new Color(0.58f, 0.58f, 0.56f), 0.45f, 0.2f, null, 1);
        Foliage = Surface("KitFoliage", new Color(0.17f, 0.30f, 0.13f), 0.25f, 0, null, 1);
        Soil = Surface("KitSoil", new Color(0.12f, 0.09f, 0.07f), 0.05f, 0, null, 1);
        LightLens = Emissive(Surface("KitLightLens", new Color(0.85f, 0.85f, 0.82f), 0.8f, 0, null, 1), Color.black);
        StripLight = Emissive(Surface("KitStripLight", new Color(0.85f, 0.84f, 0.80f), 0.6f, 0, null, 1), Color.black);
        DeviceScreen = Emissive(Surface("KitScreen", new Color(0.02f, 0.02f, 0.025f), 0.9f, 0, null, 1), Color.black);
        ExitGreen = Emissive(Surface("KitExitGreen", new Color(0.05f, 0.45f, 0.20f), 0.5f, 0, null, 1), new Color(0.05f, 0.6f, 0.25f));
        ExitLetters = Emissive(Surface("KitExitLetters", new Color(0.92f, 0.97f, 0.94f), 0.5f, 0, null, 1), new Color(0.7f, 0.8f, 0.75f));
        AlarmLed = Emissive(Surface("KitAlarmLed", new Color(0.5f, 0.02f, 0.02f), 0.6f, 0, null, 1), new Color(0.9f, 0.05f, 0.04f));
        Floor = Surface("KitFloorTile", new Color(0.80f, 0.74f, 0.64f), 0.55f, 0, "FloorTile", 1.2f);
        Plaster = Surface("KitPlaster", new Color(0.80f, 0.76f, 0.69f), 0.12f, 0, "PlasterWarm", 2.0f);
        CeilingPaint = Surface("KitCeiling", new Color(0.86f, 0.85f, 0.82f), 0.08f, 0, "PlasterWarm", 2.0f);
        Cladding = Surface("KitCladding", new Color(0.33f, 0.32f, 0.31f), 0.35f, 0, null, 1);
        Pavement = Surface("KitPavement", new Color(0.46f, 0.45f, 0.43f), 0.35f, 0, "PavementSlab", 1.2f);
        // Wet asphalt: dark and glossy, so headlights and streetlights streak in the baked probes.
        Asphalt = Surface("KitAsphalt", new Color(0.09f, 0.09f, 0.10f), 0.7f, 0, "AsphaltWet", 3.0f);
        Concrete = Surface("KitConcrete", new Color(0.56f, 0.55f, 0.52f), 0.2f, 0, null, 1);
        Render = Surface("KitRender", new Color(0.44f, 0.41f, 0.37f), 0.1f, 0, null, 1);
        RoadLine = Surface("KitRoadLine", new Color(0.72f, 0.72f, 0.68f), 0.4f, 0, null, 1);
        WindowDark = Surface("KitWindowDark", new Color(0.04f, 0.05f, 0.06f), 0.85f, 0, null, 1);
        WindowLit = Emissive(Surface("KitWindowLit", new Color(0.85f, 0.75f, 0.55f), 0.5f, 0, null, 1), new Color(1.0f, 0.72f, 0.42f) * 0.9f);
        PropPalette();
        // One cheap Standard transparent material: no grab pass, reflections come from
        // whichever (baked, box-projected) reflection probe the pane sits in.
        Glass = Translucent(Mat(Folder, "KitGlass", new Color(0.80f, 0.90f, 0.88f, 0.10f), 0.95f));
        Glass.SetFloat("_Metallic", 0);
        EditorUtility.SetDirty(Glass);
        // Crack lines: faint translucent strips drawn after the pane, so they read as
        // glints rather than paint, cast no shadow and stay out of the lightmap.
        GlassCrack = Translucent(Mat(Folder, "KitGlassCrack", new Color(0.85f, 0.90f, 0.90f, 0.35f), 0.95f));
        GlassCrack.SetFloat("_Metallic", 0);
        GlassCrack.renderQueue = 3001;
        EditorUtility.SetDirty(GlassCrack);
        AssetDatabase.SaveAssets();
    }

    public const string FallbackTexturePack = "GeneratedTextures";

    // Generated tileable textures standing in for a palette texture until a reference one exists.
    private static readonly Dictionary<string, string> GeneratedFallback = new Dictionary<string, string>
    {
        { "Walnut", "Walnut" }, { "BronzeBrushed", "BrushedBrass" }, { "StoneTop", "Marble" }
    };

    // Standard material; with ReferenceTextures/<texture>_Color.png present (else the
    // GeneratedTextures stand-in) the texture carries the colour (light tint kept) and
    // tiles once per `metres` on metric UVs.
    private static Material Surface(string name, Color color, float smoothness, float metallic, string texture, float metres)
    {
        Material m = Mat(Folder, name, color, smoothness);
        m.SetFloat("_Metallic", metallic);
        m.mainTexture = null;
        m.SetTexture("_BumpMap", null);
        m.DisableKeyword("_NORMALMAP");
        m.mainTextureScale = Vector2.one / metres;
        if (texture != null)
        {
            Texture2D albedo = DemoAssetLibrary.Texture(TexturePack, texture + "_Color.png");
            Texture2D normal = albedo != null ? DemoAssetLibrary.NormalMap(TexturePack, texture + "_Normal.png") : null;
            if (albedo == null && GeneratedFallback.TryGetValue(texture, out string generated))
            {
                albedo = DemoAssetLibrary.Texture(FallbackTexturePack, generated + ".png");
                normal = albedo != null ? DemoAssetLibrary.NormalMap(FallbackTexturePack, generated + "_Normal.png") : null;
            }
            if (albedo != null) DemoAssetLibrary.ApplyTextures(m, albedo, normal, 0.8f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    // Palette materials that ended up with an albedo texture (for validator reports).
    public static List<string> TexturedPalette()
    {
        List<string> names = new List<string>();
        foreach (Material m in new[] { Walnut, Suede, Velvet, Bronze, Stone, SafeSteel })
            if (m != null && m.mainTexture != null) names.Add(m.name + "=" + m.mainTexture.name);
        return names;
    }

    private static Material Emissive(Material m, Color emission)
    {
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emission);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------------------------------------------------------- entry points

    // User-converted model when present (Prototype/ThirdParty/StoreKit/<name>.obj|.fbx), else the procedural build.
    public static GameObject Instantiate(string name, Transform parent, Vector3 position, float yaw)
    {
        GameObject model = DemoAssetLibrary.Place(OverridePack, name, parent, position, yaw, 1, false);
        if (model != null)
        {
            DemoAssetLibrary.AddBoundsCollider(model);
            return model;
        }
        GameObject built = Build(name, parent);
        built.transform.localPosition = position;
        built.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        return built;
    }

    // Procedural fixture or jewelry prop (StoreKitProps.cs) with default parameters, at the parent's origin.
    public static GameObject Build(string name, Transform parent)
    {
        if (!Builders.TryGetValue(name, out Func<Transform, GameObject> builder) && !PropBuilders.TryGetValue(name, out builder))
            throw new ArgumentException("Unknown store kit fixture: " + name);
        return builder(parent);
    }

    // A real box mesh with metric UVs; identical sizes and materials share one saved mesh.
    public static GameObject MetricBox(string name, Transform parent, Vector3 centre, Vector3 size, Material material)
    {
        Palette();
        KitMesh k = new KitMesh();
        k.Box(material, Vector3.zero, size);
        return k.Emit(name, parent, centre, Quaternion.identity, Folder, "MetricBox");
    }

    // Renderers whose bounds stay under this (metres) are probe-lit props: they take
    // light probes and stay out of the bake so they do not bloat the lightmaps.
    public const float SmallProp = 0.3f;

    // Static flags for baking. Opaque parts batch and go into the reflection probes;
    // larger ones also contribute to GI, lightmapped when their mesh has a second UV
    // set and probe-lit otherwise (imported kit models, loose props). Occluders (walls,
    // slabs, buildings) also hide what is behind them. Glass only batches. Hinged
    // leaves and drawers are skipped unless they never move at runtime.
    public static void MarkStatic(GameObject root, bool includeMovingParts, bool occluders = false)
    {
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!includeMovingParts && IsMovingPart(renderer.transform, root.transform)) continue;
            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
            if (!Transparent(renderer))
            {
                bool small = renderer.bounds.size.x < SmallProp && renderer.bounds.size.y < SmallProp && renderer.bounds.size.z < SmallProp;
                flags |= StaticEditorFlags.ReflectionProbeStatic;
                if (!small) flags |= StaticEditorFlags.ContributeGI;
                if (!small && occluders) flags |= StaticEditorFlags.OccluderStatic;
                renderer.receiveGI = !small && Lightmappable(renderer) ? ReceiveGI.Lightmaps : ReceiveGI.LightProbes;
            }
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);
        }
    }

    private static bool Transparent(Renderer renderer)
    {
        foreach (Material m in renderer.sharedMaterials)
            if (m == null || m.renderQueue < 3000) return false;
        return true;
    }

    // Meshes with a second UV set (saved KitMesh parts unless loose) can take lightmaps.
    private static bool Lightmappable(MeshRenderer renderer)
    {
        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        return mesh != null && mesh.vertexCount > 0 && mesh.uv2.Length == mesh.vertexCount;
    }

    private static bool IsMovingPart(Transform t, Transform root)
    {
        for (; t != null && t != root; t = t.parent)
            if (t.name.StartsWith("Door") || t.name.StartsWith("Drawer")) return true;
        return false;
    }

    // ---------------------------------------------------------------- shared helpers

    private static GameObject Root(string name, Transform parent)
    {
        Palette();
        GameObject g = new GameObject(name);
        if (parent != null) g.transform.SetParent(parent, false);
        return g;
    }

    private static GameObject Part(KitMesh k, string name, GameObject root, Vector3 position, string asset)
        => k.Emit(name, root.transform, position, Quaternion.identity, Folder, asset);

    private static BoxCollider Collide(GameObject g, Vector3 centre, Vector3 size)
    {
        BoxCollider c = g.AddComponent<BoxCollider>();
        c.center = centre;
        c.size = size;
        return c;
    }

    private static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

    // Framed walnut panels: 50 mm stiles and rails standing 15 mm proud of an inset
    // body w x d between y0 and y1, on the front, both ends and optionally the back.
    private static void FramedPanels(KitMesh k, Material m, float w, float d, float y0, float y1, bool back)
    {
        const float s = 0.05f, t = 0.02f, proud = 0.015f;
        float h = y1 - y0, cy = (y0 + y1) * 0.5f;
        for (int side = -1; side <= 1; side += 2)
        {
            if (side < 0 || back)
            {
                float z = side * (d * 0.5f + proud - t * 0.5f);
                k.Box(m, V(-(w * 0.5f - s * 0.5f), cy, z), V(s, h, t));
                k.Box(m, V(w * 0.5f - s * 0.5f, cy, z), V(s, h, t));
                k.Box(m, V(0, y1 - s * 0.5f, z), V(w - 2 * s, s, t));
                k.Box(m, V(0, y0 + s * 0.5f, z), V(w - 2 * s, s, t));
            }
            float x = side * (w * 0.5f + proud - t * 0.5f);
            k.Box(m, V(x, cy, -(d * 0.5f - s * 0.5f)), V(t, h, s));
            k.Box(m, V(x, cy, d * 0.5f - s * 0.5f), V(t, h, s));
            k.Box(m, V(x, y1 - s * 0.5f, 0), V(t, s, d - 2 * s));
            k.Box(m, V(x, y0 + s * 0.5f, 0), V(t, s, d - 2 * s));
        }
    }

    // Drawer in its own space: z = 0 is the carcass face, the 20 mm front stands in
    // front of it, the open-top tray runs `depth` back and a bronze bar pull sits on the front.
    private static void Drawer(KitMesh k, float w, float h, float depth, Material front, Material inside)
    {
        float ih = h * 0.7f, y = -h * 0.5f + 0.01f + ih * 0.5f;
        k.Box(front, V(0, 0, -0.01f), V(w, h, 0.02f));
        k.Box(inside, V(0, -h * 0.5f + 0.015f, depth * 0.5f), V(w - 0.04f, 0.01f, depth));
        k.Box(front, V(-(w * 0.5f - 0.026f), y, depth * 0.5f), V(0.012f, ih, depth));
        k.Box(front, V(w * 0.5f - 0.026f, y, depth * 0.5f), V(0.012f, ih, depth));
        k.Box(front, V(0, y, depth - 0.006f), V(w - 0.064f, ih, 0.012f));
        k.Box(Bronze, V(0, h * 0.15f, -0.03f), V(Mathf.Min(0.3f, w * 0.35f), 0.012f, 0.02f));
    }

    // Jagged crown of thin shards left in an empty frame opening: corner is the opening's
    // bottom-left, u runs along its width and v along its height (both unit vectors).
    private static void Crown(KitMesh k, System.Random rng, Vector3 corner, Vector3 u, Vector3 v, float width, float height, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int edge = i % 4; // Two of every four shards stand on the bottom edge, where glass survives longest.
            float span = edge == 3 ? height : width;
            float bw = Mathf.Min(Range(rng, 0.04f, 0.12f), span * 0.5f), s = Range(rng, 0, span - bw);
            float tip = Mathf.Min(Range(rng, 0.05f, 0.16f), (edge == 3 ? width : height) * 0.45f), lean = Range(rng, 0.2f, 0.8f);
            Vector3 a, b, c;
            if (edge <= 1) { a = corner + u * s; b = corner + u * (s + bw); c = corner + u * (s + bw * lean) + v * tip; }
            else if (edge == 2) { a = corner + v * height + u * s; b = a + u * bw; c = corner + u * (s + bw * lean) + v * (height - tip); }
            else
            {
                bool right = (i / 4) % 2 == 1;
                Vector3 side = corner + (right ? u * width : Vector3.zero);
                a = side + v * s; b = a + v * bw; c = side + v * (s + bw * lean) + u * (right ? -tip : tip);
            }
            k.TwoSided(Glass, a, b, c);
        }
    }

    // Flat fragments lying face up on a surface at height y, scattered in a rectangle.
    private static void Spill(KitMesh k, System.Random rng, Vector3 centre, float halfX, float halfZ, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 c = centre + V(Range(rng, -halfX, halfX), 0, Range(rng, -halfZ, halfZ));
            float a = Range(rng, 0, Mathf.PI * 2), r = Range(rng, 0.015f, 0.045f);
            Vector3 p0 = c + V(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
            Vector3 p1 = c + V(Mathf.Cos(a + 2.1f), 0, Mathf.Sin(a + 2.1f)) * r * Range(rng, 0.5f, 1);
            Vector3 p2 = c + V(Mathf.Cos(a + 4.1f), 0, Mathf.Sin(a + 4.1f)) * r * Range(rng, 0.5f, 1);
            k.Face(Glass, Vector3.up, p0, p1, p2);
        }
    }

    // A pane of w x h in the local xy plane (centred) with a jagged hole: the remaining
    // glass is a ring of convex pieces between the hole outline and the pane edge,
    // drawn on both faces, plus a few shards hanging into the hole.
    private static void BrokenPane(KitMesh k, System.Random rng, float w, float h, Vector2 centre, Vector2 radius)
    {
        const int n = 16;
        Vector2[] hole = new Vector2[n], outer = new Vector2[n], dir = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float a = i * 2 * Mathf.PI / n + Range(rng, -0.1f, 0.1f);
            float jag = (i % 2 == 0 ? 1 : 0.72f) * Range(rng, 0.85f, 1.1f);
            Vector2 d = new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y) * jag;
            dir[i] = d.normalized;
            hole[i] = centre + d;
            outer[i] = centre + dir[i] * RayToRect(centre, dir[i], w * 0.5f, h * 0.5f);
        }
        Vector2[] corners = { new Vector2(w * 0.5f, h * 0.5f), new Vector2(-w * 0.5f, h * 0.5f), new Vector2(-w * 0.5f, -h * 0.5f), new Vector2(w * 0.5f, -h * 0.5f) };
        for (int side = -1; side <= 1; side += 2)
        {
            float z = side * 0.004f;
            Vector3 outward = V(0, 0, side);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                k.Face(Glass, outward, P(hole[i], z), P(outer[i], z), P(outer[j], z), P(hole[j], z));
                foreach (Vector2 corner in corners)
                {
                    Vector2 c = corner - centre;
                    if (Cross(dir[i], c) > 0 && Cross(c, dir[j]) > 0) k.Face(Glass, outward, P(outer[i], z), P(corner, z), P(outer[j], z));
                }
            }
        }
        for (int s = 0; s < 5; s++)
        {
            int i = rng.Next(n), j = (i + 1) % n;
            Vector2 tip = Vector2.Lerp(hole[i], centre, Range(rng, 0.12f, 0.25f));
            k.TwoSided(Glass, P(hole[i], 0), P(hole[j], 0), P(tip, 0));
        }
    }

    private static Vector3 P(Vector2 p, float z) => V(p.x, p.y, z);
    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    private static float RayToRect(Vector2 origin, Vector2 d, float hw, float hh)
    {
        float t = float.MaxValue;
        if (d.x > 1e-5f) t = Mathf.Min(t, (hw - origin.x) / d.x);
        if (d.x < -1e-5f) t = Mathf.Min(t, (-hw - origin.x) / d.x);
        if (d.y > 1e-5f) t = Mathf.Min(t, (hh - origin.y) / d.y);
        if (d.y < -1e-5f) t = Mathf.Min(t, (-hh - origin.y) / d.y);
        return t;
    }

    // Glass child: intact pane box, or the broken ring; returns the child.
    private static GameObject PaneChild(string name, Transform parent, Vector3 centre, float w, float h, float thickness, bool broken, int seed, Vector2 holeCentre)
    {
        KitMesh k = new KitMesh();
        if (broken) BrokenPane(k, new System.Random(seed), w, h, holeCentre, new Vector2(w * 0.33f, h * 0.30f));
        else k.Box(Glass, Vector3.zero, V(w, h, thickness));
        return k.Emit(name, parent, centre, Quaternion.identity, Folder, broken ? "pane_broken" : "pane");
    }

    // ---------------------------------------------------------------- display fixtures

    public static GameObject DisplayCase(Transform parent, CaseGlass glass = CaseGlass.Intact, bool drawer = false, float drawerOut = 0)
        => Case("display_case", parent, 1.8f, 0.65f, false, glass, drawer, drawerOut);

    public static GameObject WallCase(Transform parent, CaseGlass glass = CaseGlass.Intact)
        => Case("wall_case", parent, 1.8f, 0.6f, true, glass, false, 0);

    // Walnut cabinet on a recessed plinth, framed panels, cream suede deck at 0.70 m,
    // 25 mm bronze posts and rails and five (four with a back panel) separate panes:
    // Glass_Front, Glass_Back, Glass_Left, Glass_Right, Glass_Top. A broken pane is
    // removed and leaves a crown of shards in its frame, plus fragments on the deck.
    private static GameObject Case(string kind, Transform parent, float w, float d, bool backPanel, CaseGlass glass, bool drawer, float drawerOut)
    {
        const float h = 1.05f, deck = 0.70f, post = 0.025f, rail = 0.0425f;
        GameObject root = Root(kind, parent);
        KitMesh body = new KitMesh();
        body.Box(Walnut, V(0, 0.04f, 0), V(w - 0.06f, 0.08f, d - 0.06f), false);
        body.Box(Walnut, V(0, (0.08f + deck) * 0.5f, 0), V(w - 0.02f, deck - 0.08f, d - 0.02f));
        FramedPanels(body, Walnut, w - 0.02f, d - 0.02f, 0.08f, deck, !backPanel);
        body.Box(Suede, V(0, deck + 0.005f, 0), V(w - 0.08f, 0.01f, d - 0.08f), false);
        for (int s = -1; s <= 1; s += 2)
        {
            body.Box(Bronze, V(0, deck + 0.0075f, s * (d * 0.5f - 0.034f)), V(w - 0.056f, 0.015f, 0.012f));
            body.Box(Bronze, V(s * (w * 0.5f - 0.034f), deck + 0.0075f, 0), V(0.012f, 0.015f, d - 0.08f));
            body.Box(Bronze, V(0, h - post * 0.5f, s * (d * 0.5f - rail)), V(w - 0.06f, post, post));
            body.Box(Bronze, V(s * (w * 0.5f - rail), h - post * 0.5f, 0), V(post, post, d - 0.06f));
            for (int t = -1; t <= 1; t += 2)
                body.Box(Bronze, V(s * (w * 0.5f - rail), (deck + 0.01f + h) * 0.5f, t * (d * 0.5f - rail)), V(post, h - deck - 0.01f, post));
        }
        float paneY0 = deck + 0.01f, paneH = h - post - paneY0, paneCy = paneY0 + paneH * 0.5f;
        if (backPanel) body.Box(Walnut, V(0, paneCy, d * 0.5f - rail), V(w - 0.11f, paneH, 0.012f));
        if (drawer) body.Box(Fabric, V(0, 0.59f, -(d * 0.5f - 0.01f) - 0.001f), V(w - 0.14f, 0.11f, 0.002f));
        Part(body, "Body", root, Vector3.zero, kind + "_body" + (drawer ? "_drawer" : ""));

        bool allBroken = glass == CaseGlass.AllBroken, frontBroken = glass != CaseGlass.Intact;
        float pw = w - 0.11f, pd = d - 0.11f;
        System.Random rng = new System.Random(kind.Length * 31 + (int)glass);
        KitMesh shards = new KitMesh();
        Vector3 frontCorner = V(-pw * 0.5f, paneY0, -(d * 0.5f - rail)), backCorner = V(-pw * 0.5f, paneY0, d * 0.5f - rail);
        if (frontBroken) Crown(shards, rng, frontCorner, Vector3.right, Vector3.up, pw, paneH, 8);
        else CasePane(root, "Glass_Front", V(0, paneCy, -(d * 0.5f - rail)), V(pw, paneH, 0.006f));
        if (!backPanel)
        {
            if (allBroken) Crown(shards, rng, backCorner, Vector3.right, Vector3.up, pw, paneH, 8);
            else CasePane(root, "Glass_Back", V(0, paneCy, d * 0.5f - rail), V(pw, paneH, 0.006f));
        }
        for (int s = -1; s <= 1; s += 2)
        {
            string name = s < 0 ? "Glass_Left" : "Glass_Right";
            if (allBroken) Crown(shards, rng, V(s * (w * 0.5f - rail), paneY0, -pd * 0.5f), Vector3.forward, Vector3.up, pd, paneH, 5);
            else CasePane(root, name, V(s * (w * 0.5f - rail), paneCy, 0), V(0.006f, paneH, pd));
        }
        if (allBroken) Crown(shards, rng, V(-pw * 0.5f, h - post * 0.5f, -pd * 0.5f), Vector3.right, Vector3.forward, pw, pd, 8);
        else CasePane(root, "Glass_Top", V(0, h - post * 0.5f, 0), V(pw, 0.006f, pd));
        if (frontBroken)
        {
            Spill(shards, rng, V(0, deck + 0.012f, 0), w * 0.5f - 0.1f, d * 0.5f - 0.1f, allBroken ? 16 : 10);
            Spill(shards, rng, V(0, 0.003f, -d * 0.5f - 0.3f), w * 0.4f, 0.22f, 6);
            Part(shards, "Shards", root, Vector3.zero, kind + "_shards_" + glass);
        }

        if (drawer)
        {
            KitMesh tray = new KitMesh();
            Drawer(tray, w - 0.14f, 0.11f, 0.45f, Walnut, Suede);
            Part(tray, "Drawer", root, V(0, 0.59f, -(d * 0.5f - 0.01f) - drawerOut), kind + "_drawer");
        }
        // Solid cabinet up to the deck, then one thin box per pane still standing, so the
        // deck stays reachable through a broken pane and tools settle on it, not on a lid.
        Collide(root, V(0, deck * 0.5f, 0), V(w, deck, d));
        const float slab = 0.02f;
        if (!frontBroken) Collide(root, V(0, paneCy, -(d * 0.5f - rail)), V(w, paneH, slab));
        if (backPanel || !allBroken) Collide(root, V(0, paneCy, d * 0.5f - rail), V(w, paneH, slab));
        if (!allBroken)
            for (int s = -1; s <= 1; s += 2) Collide(root, V(s * (w * 0.5f - rail), paneCy, 0), V(slab, paneH, d));
        if (!allBroken) Collide(root, V(0, h - post * 0.5f, 0), V(w, post, d));
        return root;
    }

    private static void CasePane(GameObject root, string name, Vector3 centre, Vector3 size)
    {
        KitMesh k = new KitMesh();
        k.Box(Glass, Vector3.zero, size);
        Part(k, name, root, centre, "case_pane");
    }

    // Tall open walnut cabinet with three shelves, bronze front trim and a shelf strip
    // light (KitStripLight, off) under the top and each shelf.
    public static GameObject WallDisplay(Transform parent)
    {
        const float w = 0.9f, d = 0.4f, h = 2.1f, t = 0.03f;
        GameObject root = Root("wall_display", parent);
        KitMesh k = new KitMesh();
        for (int s = -1; s <= 1; s += 2)
        {
            k.Box(Walnut, V(s * (w * 0.5f - t * 0.5f), h * 0.5f, 0), V(t, h, d), false);
            k.Box(Bronze, V(s * (w * 0.5f - t * 0.5f), h * 0.5f, -(d * 0.5f + 0.004f)), V(t, h, 0.008f));
        }
        k.Box(Walnut, V(0, h - t * 0.5f, 0), V(w - 2 * t, t, d));
        k.Box(Walnut, V(0, 0.04f, 0), V(w - 2 * t, 0.08f, d), false);
        k.Box(Walnut, V(0, (0.08f + h - t) * 0.5f, d * 0.5f - 0.01f), V(w - 2 * t, h - t - 0.08f, 0.02f));
        k.Box(Bronze, V(0, h - t * 0.5f, -(d * 0.5f + 0.004f)), V(w, t, 0.008f));
        k.Box(Bronze, V(0, 0.04f, -(d * 0.5f + 0.004f)), V(w - 2 * t, 0.08f, 0.008f));
        k.Box(StripLight, V(0, h - t - 0.004f, -(d * 0.5f - 0.06f)), V(w - 2 * t - 0.04f, 0.008f, 0.02f));
        foreach (float y in new[] { 0.6f, 1.1f, 1.6f })
        {
            k.Box(Walnut, V(0, y, -0.01f), V(w - 2 * t, 0.025f, d - 0.04f));
            k.Box(Bronze, V(0, y, -(d * 0.5f - 0.01f) - 0.003f), V(w - 2 * t, 0.025f, 0.006f));
            k.Box(StripLight, V(0, y - 0.0165f, -(d * 0.5f - 0.06f)), V(w - 2 * t - 0.04f, 0.008f, 0.02f));
        }
        Part(k, "Body", root, Vector3.zero, "wall_display");
        Collide(root, V(0, h * 0.5f, 0), V(w, h, d));
        return root;
    }

    // Checkout counter: framed walnut on a recessed plinth, 40 mm stone top with a 20 mm overhang.
    public static GameObject Counter(Transform parent)
    {
        const float w = 1.8f, d = 0.6f, h = 0.95f;
        GameObject root = Root("counter", parent);
        KitMesh k = new KitMesh();
        k.Box(Walnut, V(0, 0.04f, 0), V(w - 0.06f, 0.08f, d - 0.06f), false);
        k.Box(Walnut, V(0, (0.08f + h - 0.04f) * 0.5f, 0), V(w - 0.02f, h - 0.12f, d - 0.02f));
        FramedPanels(k, Walnut, w - 0.02f, d - 0.02f, 0.08f, h - 0.04f, true);
        k.Box(Stone, V(0, h - 0.02f, 0), V(w + 0.04f, 0.04f, d + 0.04f));
        Part(k, "Body", root, Vector3.zero, "counter");
        Collide(root, V(0, h * 0.5f, 0), V(w + 0.04f, h, d + 0.04f));
        return root;
    }

    // Walnut chest on a bronze plinth with three drawers (Drawer0 bottom .. Drawer2 top).
    public static GameObject DrawerUnit(Transform parent, float middleOpen = 0.25f)
    {
        const float w = 0.9f, d = 0.5f, h = 0.75f, dh = 0.21f;
        GameObject root = Root("drawer_unit", parent);
        KitMesh k = new KitMesh();
        k.Box(Bronze, V(0, 0.025f, 0), V(w - 0.04f, 0.05f, d - 0.04f), false);
        k.Box(Walnut, V(0, (0.05f + h) * 0.5f, 0), V(w, h - 0.05f, d));
        float[] centres = { 0.175f, 0.4f, 0.625f };
        foreach (float y in centres) k.Box(Fabric, V(0, y, -d * 0.5f - 0.001f), V(w - 0.04f, dh, 0.002f));
        Part(k, "Body", root, Vector3.zero, "drawer_unit");
        for (int i = 0; i < 3; i++)
        {
            KitMesh drawer = new KitMesh();
            Drawer(drawer, w - 0.04f, dh, 0.42f, Walnut, Suede);
            drawer.Box(Bronze, V(0.3f, 0.06f, -0.022f), V(0.012f, 0.012f, 0.004f));
            Part(drawer, "Drawer" + i, root, V(0, centres[i], -d * 0.5f - (i == 1 ? middleOpen : 0)), "drawer_unit_drawer");
        }
        Collide(root, V(0, h * 0.5f, 0), V(w, h, d));
        return root;
    }

    // ---------------------------------------------------------------- office

    // Walnut desk on tapered square legs with an apron and one drawer, right of centre.
    public static GameObject OfficeDesk(Transform parent, float drawerOpen = 0.2f)
    {
        const float w = 1.4f, d = 0.7f, h = 0.75f;
        GameObject root = Root("office_desk", parent);
        KitMesh k = new KitMesh();
        k.Box(Walnut, V(0, h - 0.015f, 0), V(w, 0.03f, d));
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                TaperedLeg(k, Walnut, V(sx * 0.65f, 0, sz * 0.30f), 0.02f, 0.032f, h - 0.03f);
        for (int s = -1; s <= 1; s += 2)
        {
            k.Box(Walnut, V(0, 0.66f, s * 0.31f), V(1.236f, 0.12f, 0.02f));
            k.Box(Walnut, V(s * 0.63f, 0.66f, 0), V(0.02f, 0.12f, 0.536f));
        }
        k.Box(Fabric, V(0.25f, 0.66f, -0.321f), V(0.5f, 0.1f, 0.002f));
        Part(k, "Body", root, Vector3.zero, "office_desk");
        KitMesh drawer = new KitMesh();
        Drawer(drawer, 0.5f, 0.1f, 0.5f, Walnut, Walnut);
        Part(drawer, "Drawer", root, V(0.25f, 0.66f, -0.32f - drawerOpen), "office_desk_drawer");
        Collide(root, V(0, h * 0.5f, 0), V(w, h, d));
        return root;
    }

    // Square leg tapering from half-width `bottom` to `top`: a four-sided flat lathe turned 45 degrees.
    private static void TaperedLeg(KitMesh k, Material m, Vector3 foot, float bottom, float top, float height)
        => k.Lathe(m, foot, Quaternion.identity, new[] { bottom * 1.4142f, top * 1.4142f }, new[] { 0f, height }, 4, false, true, false, 45);

    // Five-star swivel chair (about 450 triangles); toppled lies on its back.
    public static GameObject OfficeChair(Transform parent, bool toppled = false)
    {
        GameObject root = Root("office_chair", parent);
        KitMesh k = new KitMesh();
        for (int i = 0; i < 5; i++)
        {
            Quaternion yaw = Quaternion.Euler(0, i * 72, 0);
            k.Box(DeviceBlack, yaw * V(0, 0.08f, 0.15f), V(0.04f, 0.03f, 0.30f), yaw);
            Vector3 caster = yaw * V(0, 0.025f, 0.29f);
            k.Box(DeviceBlack, caster + V(0, 0.045f, 0), V(0.015f, 0.03f, 0.015f), yaw);
            k.Cylinder(DeviceBlack, caster + yaw * V(-0.015f, 0, 0), caster + yaw * V(0.015f, 0, 0), 0.025f, 8);
        }
        k.Lathe(DeviceBlack, V(0, 0.06f, 0), Quaternion.identity, new[] { 0.045f, 0.045f }, new[] { 0f, 0.05f }, 10);
        k.Cylinder(Stainless, V(0, 0.11f, 0), V(0, 0.40f, 0), 0.02f, 10, false);
        k.Box(DeviceBlack, V(0, 0.415f, 0), V(0.46f, 0.03f, 0.44f));
        k.ChamferBox(Fabric, V(0, 0.465f, 0), V(0.5f, 0.07f, 0.48f), 0.02f, Quaternion.identity);
        k.Box(DeviceBlack, V(0, 0.52f, 0.25f), V(0.05f, 0.2f, 0.03f));
        k.ChamferBox(Fabric, V(0, 0.80f, 0.25f), V(0.46f, 0.50f, 0.07f), 0.02f, Quaternion.Euler(8, 0, 0));
        for (int s = -1; s <= 1; s += 2)
        {
            k.Box(DeviceBlack, V(s * 0.27f, 0.55f, 0), V(0.03f, 0.2f, 0.03f));
            k.Box(DeviceBlack, V(s * 0.27f, 0.66f, 0), V(0.06f, 0.025f, 0.25f));
        }
        // Toppled: rotated onto its back about x, raised so the casters and backrest rest on the floor.
        k.Emit("Chair", root.transform, toppled ? V(0, 0.33f, 0) : Vector3.zero,
            toppled ? Quaternion.Euler(90, 0, 0) : Quaternion.identity, Folder, "office_chair");
        DemoAssetLibrary.AddBoundsCollider(root);
        return root;
    }

    // Open dark-metal shelving with three closed storage boxes on each of four shelves.
    public static GameObject ShelvingUnit(Transform parent)
    {
        const float w = 1.0f, d = 0.4f, h = 1.9f;
        GameObject root = Root("shelving_unit", parent);
        KitMesh k = new KitMesh();
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                k.Box(FrameMetal, V(sx * (w * 0.5f - 0.015f), h * 0.5f, sz * (d * 0.5f - 0.015f)), V(0.03f, h, 0.03f), false);
        float brace = Mathf.Atan2(1.74f, w - 0.03f) * Mathf.Rad2Deg, length = new Vector2(w - 0.03f, 1.74f).magnitude;
        for (int s = -1; s <= 1; s += 2)
            k.Box(FrameMetal, V(0, 0.95f, d * 0.5f - 0.01f), V(length, 0.015f, 0.006f), Quaternion.Euler(0, 0, s * brace));
        float[] shelves = { 0.08f, 0.66f, 1.24f, 1.82f };
        for (int i = 0; i < shelves.Length; i++)
        {
            k.Box(FrameMetal, V(0, shelves[i], 0), V(w - 0.01f, 0.02f, d - 0.01f));
            for (int j = 0; j < 3; j++)
            {
                float bh = 0.12f + 0.03f * ((i + j) % 2);
                k.Box(Lacquer, V(-0.31f + j * 0.31f, shelves[i] + 0.01f + bh * 0.5f, 0), V(0.26f, bh, 0.30f), false);
            }
        }
        Part(k, "Body", root, Vector3.zero, "shelving_unit");
        Collide(root, V(0, h * 0.5f, 0), V(w, h, d));
        return root;
    }

    // Cream lacquered steel locker with a proud door panel, vents and a bar handle.
    public static GameObject LockerCabinet(Transform parent)
    {
        const float w = 0.6f, d = 0.5f, h = 1.8f;
        GameObject root = Root("locker_cabinet", parent);
        KitMesh k = new KitMesh();
        k.Box(FrameMetal, V(0, 0.03f, 0), V(w - 0.04f, 0.06f, d - 0.04f), false);
        k.Box(Lacquer, V(0, (0.06f + h) * 0.5f, 0), V(w, h - 0.06f, d));
        k.Box(Lacquer, V(0, (0.06f + h) * 0.5f, -d * 0.5f - 0.004f), V(w - 0.04f, h - 0.1f, 0.008f));
        k.Box(Stainless, V(0.2f, 1.0f, -d * 0.5f - 0.03f), V(0.02f, 0.16f, 0.02f));
        for (int s = -1; s <= 1; s += 2) k.Box(Stainless, V(0.2f, 1.0f + s * 0.06f, -d * 0.5f - 0.015f), V(0.012f, 0.012f, 0.022f));
        for (int i = 0; i < 3; i++) k.Box(FrameMetal, V(0, 1.55f + i * 0.04f, -d * 0.5f - 0.0085f), V(0.3f, 0.012f, 0.002f));
        Part(k, "Body", root, Vector3.zero, "locker_cabinet");
        Collide(root, V(0, h * 0.5f, 0), V(w, h, d));
        return root;
    }

    public static GameObject SafeLarge(Transform parent, float openAngle = 100) => Safe("safe_large", parent, 1.3f, 0.9f, 1.5f, true, openAngle);
    public static GameObject SafeTall(Transform parent, float openAngle = 100) => Safe("safe_tall", parent, 0.75f, 0.65f, 1.6f, false, openAngle);

    // Steel safe on four feet with 90 mm walls, a bronze-brown lining and two shelves.
    // The inset door ("Door") hinges on the right seen from the front and swings out:
    // bronze bolts on its free edge, dial and spoked (or single lever) handle outside,
    // lined panel inside. openAngle 0 is closed.
    private static GameObject Safe(string kind, Transform parent, float w, float d, float h, bool spokes, float openAngle)
    {
        const float wall = 0.09f, foot = 0.05f, gap = 0.004f;
        GameObject root = Root(kind, parent);
        float hb = h - foot, cy = foot + hb * 0.5f;
        KitMesh k = new KitMesh();
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                k.Box(SafeSteel, V(sx * (w * 0.5f - 0.08f), foot * 0.5f, sz * (d * 0.5f - 0.08f)), V(0.09f, foot, 0.09f), false);
        k.Box(SafeSteel, V(0, h - wall * 0.5f, 0), V(w, wall, d));
        k.Box(SafeSteel, V(0, foot + wall * 0.5f, 0), V(w, wall, d));
        k.Box(SafeSteel, V(-(w * 0.5f - wall * 0.5f), cy, 0), V(wall, hb - 2 * wall, d));
        k.Box(SafeSteel, V(w * 0.5f - wall * 0.5f, cy, 0), V(wall, hb - 2 * wall, d));
        k.Box(SafeSteel, V(0, cy, d * 0.5f - wall * 0.5f), V(w - 2 * wall, hb - 2 * wall, wall));
        // Lining faces 1 mm inside the shell, behind the closed door's depth.
        float ix = w * 0.5f - wall - 0.001f, y0 = foot + wall + 0.001f, y1 = h - wall - 0.001f;
        float zf = -d * 0.5f + wall, zb = d * 0.5f - wall - 0.001f;
        k.Face(SafeLining, Vector3.right, V(-ix, y0, zf), V(-ix, y1, zf), V(-ix, y1, zb), V(-ix, y0, zb));
        k.Face(SafeLining, Vector3.left, V(ix, y0, zf), V(ix, y1, zf), V(ix, y1, zb), V(ix, y0, zb));
        k.Face(SafeLining, Vector3.up, V(-ix, y0, zf), V(ix, y0, zf), V(ix, y0, zb), V(-ix, y0, zb));
        k.Face(SafeLining, Vector3.down, V(-ix, y1, zf), V(ix, y1, zf), V(ix, y1, zb), V(-ix, y1, zb));
        k.Face(SafeLining, Vector3.back, V(-ix, y0, zb), V(ix, y0, zb), V(ix, y1, zb), V(-ix, y1, zb));
        for (int i = 1; i <= 2; i++)
            k.Box(SafeLining, V(0, y0 + (y1 - y0) * i / 3f, (zf + zb) * 0.5f), V(2 * ix, 0.02f, zb - zf));
        float hingeX = w * 0.5f - wall - gap, W = w - 2 * wall - 2 * gap, H = hb - 2 * wall - 2 * gap, dy = foot + wall + gap + H * 0.5f;
        for (int s = -1; s <= 1; s += 2)
            k.Cylinder(SafeSteel, V(hingeX + 0.02f, dy + s * H * 0.3f - 0.07f, -d * 0.5f - 0.012f),
                V(hingeX + 0.02f, dy + s * H * 0.3f + 0.07f, -d * 0.5f - 0.012f), 0.018f, 8);
        Part(k, "Body", root, Vector3.zero, kind + "_body");

        KitMesh door = new KitMesh();
        door.Box(SafeSteel, V(-W * 0.5f, dy, wall * 0.5f), V(W, H, wall));
        door.Box(SafeLining, V(-W * 0.5f, dy, wall + 0.006f), V(W - 0.12f, H - 0.12f, 0.012f));
        for (int i = -1; i <= 1; i++)
            door.Cylinder(Bronze, V(-W + 0.01f, dy + i * H * 0.3f, wall * 0.5f), V(-W - 0.035f, dy + i * H * 0.3f, wall * 0.5f), 0.022f, 12);
        Quaternion outward = Quaternion.FromToRotation(Vector3.up, Vector3.back);
        door.Lathe(Bronze, V(-W * 0.5f, dy + H * 0.22f, 0), outward, new[] { 0.07f, 0.07f, 0.05f }, new[] { 0f, 0.018f, 0.03f }, 20, true, false, true);
        Vector3 hub = V(-W * 0.5f, dy - H * 0.05f, 0), hubFront = hub + V(0, 0, -0.05f);
        door.Cylinder(Bronze, hub, hub + V(0, 0, -0.06f), 0.03f, 12);
        int arms = spokes ? 3 : 1;
        for (int i = 0; i < arms; i++)
        {
            float a = spokes ? (90 + 120 * i) * Mathf.Deg2Rad : Mathf.PI;
            Vector3 dir = V(Mathf.Cos(a), Mathf.Sin(a), 0);
            door.Cylinder(Bronze, hubFront, hubFront + dir * 0.17f, 0.012f, 8);
            door.Cylinder(Bronze, hubFront + dir * 0.17f, hubFront + dir * 0.2f, 0.02f, 8);
        }
        GameObject leaf = door.Emit("Door", root.transform, V(hingeX, 0, -d * 0.5f), Quaternion.Euler(0, -openAngle, 0), Folder, kind + "_door");
        Collide(leaf, V(-W * 0.5f, dy, wall * 0.5f), V(W, H, wall));
        // Shell walls and shelves, not one solid box, so the open safe's interior is reachable.
        Collide(root, V(0, h - wall * 0.5f, 0), V(w, wall, d));
        Collide(root, V(0, foot * 0.5f + wall * 0.5f, 0), V(w, foot + wall, d));
        for (int s = -1; s <= 1; s += 2) Collide(root, V(s * (w * 0.5f - wall * 0.5f), cy, 0), V(wall, hb - 2 * wall, d));
        Collide(root, V(0, cy, d * 0.5f - wall * 0.5f), V(w - 2 * wall, hb - 2 * wall, wall));
        for (int i = 1; i <= 2; i++) Collide(root, V(0, y0 + (y1 - y0) * i / 3f, (zf + zb) * 0.5f), V(2 * ix, 0.02f, zb - zf));
        return root;
    }

    // ---------------------------------------------------------------- seating and decor

    // Two-seat sofa: charcoal fabric base, arms and soft cushions on a walnut plinth and tapered legs.
    public static GameObject WaitingSofa(Transform parent)
    {
        const float w = 1.7f, d = 0.85f;
        GameObject root = Root("waiting_sofa", parent);
        KitMesh k = new KitMesh();
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                TaperedLeg(k, Walnut, V(sx * (w * 0.5f - 0.08f), 0, sz * (d * 0.5f - 0.08f)), 0.02f, 0.03f, 0.10f);
        k.Box(Walnut, V(0, 0.125f, 0), V(w - 0.04f, 0.05f, d - 0.04f));
        k.Box(Fabric, V(0, 0.275f, 0), V(w, 0.25f, d), false);
        k.Box(Fabric, V(0, 0.6f, d * 0.5f - 0.08f), V(w, 0.4f, 0.16f), false);
        for (int s = -1; s <= 1; s += 2)
        {
            k.ChamferBox(Fabric, V(s * (w * 0.5f - 0.08f), 0.52f, 0), V(d, 0.24f, 0.16f), 0.04f, Quaternion.Euler(0, 90, 0));
            k.ChamferBox(Fabric, V(s * 0.345f, 0.47f, -0.08f), V(0.68f, 0.14f, 0.69f), 0.03f, Quaternion.identity);
            k.ChamferBox(Fabric, V(s * 0.345f, 0.66f, 0.2f), V(0.68f, 0.30f, 0.14f), 0.04f, Quaternion.Euler(10, 0, 0));
        }
        Part(k, "Body", root, Vector3.zero, "waiting_sofa");
        Collide(root, V(0, 0.4f, 0), V(w, 0.8f, d));
        return root;
    }

    // Low table: stone top over a bronze band, walnut apron and legs, bronze feet and stretchers.
    public static GameObject SideTable(Transform parent)
    {
        const float w = 1.0f, d = 0.55f, h = 0.45f;
        GameObject root = Root("side_table", parent);
        KitMesh k = new KitMesh();
        k.Box(Stone, V(0, h - 0.02f, 0), V(w, 0.04f, d));
        k.Box(Bronze, V(0, h - 0.046f, 0), V(w - 0.04f, 0.012f, d - 0.04f));
        for (int s = -1; s <= 1; s += 2)
        {
            k.Box(Walnut, V(0, 0.365f, s * 0.235f), V(0.86f, 0.07f, 0.02f));
            k.Box(Walnut, V(s * 0.46f, 0.365f, 0), V(0.02f, 0.07f, 0.41f));
            k.Box(Bronze, V(0, 0.07f, s * 0.235f), V(0.86f, 0.015f, 0.015f));
            for (int t = -1; t <= 1; t += 2)
            {
                k.Box(Walnut, V(s * 0.46f, 0.215f, t * 0.235f), V(0.06f, 0.37f, 0.06f), false);
                k.Box(Bronze, V(s * 0.46f, 0.015f, t * 0.235f), V(0.064f, 0.03f, 0.064f), false);
            }
        }
        k.Box(Bronze, V(0, 0.07f, 0), V(0.015f, 0.015f, 0.41f));
        Part(k, "Body", root, Vector3.zero, "side_table");
        Collide(root, V(0, h * 0.5f, 0), V(w, h, d));
        return root;
    }

    // Square tapered cream pot (0.35 m) with soil and a low-poly cluster of folded leaves.
    public static GameObject Planter(Transform parent)
    {
        GameObject root = Root("planter", parent);
        KitMesh k = new KitMesh();
        k.Lathe(Lacquer, Vector3.zero, Quaternion.identity, new[] { 0.212f, 0.2475f, 0.22f, 0.22f }, new[] { 0f, 0.40f, 0.40f, 0.36f }, 4, false, true, false, 45);
        k.Face(Soil, Vector3.up, KitMesh.Ring(Vector3.zero, Quaternion.identity, 0.22f, 0.36f, 4, Mathf.PI * 0.25f));
        System.Random rng = new System.Random(7);
        for (int i = 0; i < 18; i++)
        {
            Quaternion yaw = Quaternion.Euler(0, i * 137.5f, 0), q = yaw * Quaternion.Euler(25 + (i % 6) * 9, 0, 0);
            float length = Range(rng, 0.28f, 0.45f), width = Range(rng, 0.05f, 0.08f);
            Vector3 stem = V(0, 0.36f, 0) + yaw * V(0, 0, 0.03f);
            Vector3 axis = q * Vector3.up, side = q * Vector3.right, fold = q * Vector3.forward * (width * 0.4f);
            Vector3 tip = stem + axis * length + Vector3.down * (length * 0.15f), mid = stem + axis * (length * 0.45f);
            k.TwoSided(Foliage, stem, mid - side * width + fold, tip);
            k.TwoSided(Foliage, stem, tip, mid + side * width + fold);
        }
        Part(k, "Body", root, Vector3.zero, "planter");
        Collide(root, V(0, 0.2f, 0), V(0.35f, 0.4f, 0.35f));
        return root;
    }

    // Walnut wall frame on charcoal velvet with a bronze line-drawn diamond emblem.
    public static GameObject SignFrame(Transform parent)
    {
        const float w = 0.9f, h = 0.7f, cy = 0.35f;
        GameObject root = Root("sign_frame", parent);
        KitMesh k = new KitMesh();
        k.Box(Velvet, V(0, cy, -0.01f), V(w - 0.08f, h - 0.08f, 0.02f));
        for (int s = -1; s <= 1; s += 2)
        {
            k.Box(Walnut, V(0, cy + s * (h * 0.5f - 0.02f), -0.02f), V(w, 0.04f, 0.04f));
            k.Box(Walnut, V(s * (w * 0.5f - 0.02f), cy, -0.02f), V(0.04f, h - 0.08f, 0.04f));
        }
        Vector2[] p =
        {
            new Vector2(-0.09f, 0.10f), new Vector2(0.09f, 0.10f), new Vector2(-0.18f, 0.03f), new Vector2(0.18f, 0.03f),
            new Vector2(0, -0.18f), new Vector2(-0.05f, 0.03f), new Vector2(0.05f, 0.03f)
        };
        int[] lines = { 0, 1, 0, 2, 1, 3, 2, 3, 2, 4, 3, 4, 0, 5, 1, 6, 5, 4, 6, 4 };
        for (int i = 0; i < lines.Length; i += 2)
            k.Stroke(Bronze, V(p[lines[i]].x, cy + p[lines[i]].y, -0.021f), V(p[lines[i + 1]].x, cy + p[lines[i + 1]].y, -0.021f), 0.008f, Vector3.back);
        Part(k, "Body", root, Vector3.zero, "sign_frame");
        return root;
    }

    // ---------------------------------------------------------------- doors and facade

    // Double glass doors in a dark metal frame (2.0 x 2.4). DoorLeft hinges at the left
    // jamb, DoorRight at the right; positive angles open both into the shop (+z). Each
    // hinge axis lies on the leaf's +z face, so an opened leaf swings clear of the jamb.
    public static GameObject StorefrontDoors(Transform parent, float openLeft = 0, float openRight = 0, bool brokenLeft = false, bool brokenRight = false)
    {
        const float half = 0.92f, leafW = 0.915f, leafH = 2.30f, hinge = half - 0.0025f;
        GameObject root = Root("storefront_doors", parent);
        KitMesh frame = new KitMesh();
        for (int s = -1; s <= 1; s += 2) frame.Box(FrameMetal, V(s * 0.96f, 1.2f, 0), V(0.08f, 2.4f, 0.12f));
        frame.Box(FrameMetal, V(0, 2.36f, 0), V(2 * half, 0.08f, 0.12f));
        frame.Box(Stainless, V(0, 0.005f, 0), V(2 * half, 0.01f, 0.14f), false);
        Part(frame, "Frame", root, Vector3.zero, "storefront_frame");
        for (int s = -1; s <= 1; s += 2) Collide(root, V(s * 0.96f, 1.2f, 0), V(0.08f, 2.4f, 0.12f));
        Collide(root, V(0, 2.36f, 0), V(2 * half, 0.08f, 0.12f));
        const float t = 0.05f, zc = -t * 0.5f;
        KitMesh shards = new KitMesh();
        System.Random rng = new System.Random(11);
        for (int s = -1; s <= 1; s += 2)
        {
            // s = -1 is the left leaf, which extends +x from its hinge; the right leaf mirrors it.
            float x = -s;
            bool broken = s < 0 ? brokenLeft : brokenRight;
            KitMesh leaf = new KitMesh();
            leaf.Box(FrameMetal, V(x * 0.035f, 0.01f + leafH * 0.5f, zc), V(0.07f, leafH, t));
            leaf.Box(FrameMetal, V(x * (leafW - 0.035f), 0.01f + leafH * 0.5f, zc), V(0.07f, leafH, t));
            leaf.Box(FrameMetal, V(x * leafW * 0.5f, 0.01f + leafH - 0.035f, zc), V(leafW - 0.14f, 0.07f, t));
            leaf.Box(FrameMetal, V(x * leafW * 0.5f, 0.01f + 0.09f, zc), V(leafW - 0.14f, 0.18f, t));
            for (int f = -1; f <= 1; f += 2)
            {
                leaf.Box(Bronze, V(x * (leafW - 0.1f), 1.05f, zc + f * 0.06f), V(0.025f, 0.6f, 0.025f));
                for (int e = -1; e <= 1; e += 2)
                    leaf.Box(Bronze, V(x * (leafW - 0.1f), 1.05f + e * 0.25f, zc + f * 0.0375f), V(0.015f, 0.015f, 0.025f));
            }
            float angle = s < 0 ? -openLeft : openRight;
            GameObject leafObject = leaf.Emit(s < 0 ? "DoorLeft" : "DoorRight", root.transform, V(s * hinge, 0, -zc),
                Quaternion.Euler(0, angle, 0), Folder, s < 0 ? "storefront_leaf_left" : "storefront_leaf_right");
            float gw = leafW - 0.14f, gh = leafH - 0.25f;
            PaneChild("Glass", leafObject.transform, V(x * leafW * 0.5f, 0.19f + gh * 0.5f, zc), gw, gh, 0.008f, broken, 20 + s, new Vector2(0.05f, -0.1f));
            Collide(leafObject, V(x * leafW * 0.5f, 0.01f + leafH * 0.5f, zc), V(leafW, leafH, t));
            if (broken) Spill(shards, rng, V(s * 0.45f, 0.003f, 0.45f), 0.45f, 0.4f, 12);
        }
        if (brokenLeft || brokenRight) Part(shards, "Shards", root, Vector3.zero, "storefront_shards");
        return root;
    }

    // Walnut office door in a walnut casing, bronze butt hinges and lever; opens into +z.
    public static GameObject OfficeDoor(Transform parent, float open = 0)
        => HingedDoor("office_door", parent, Walnut, Bronze, false, open);

    // Grey steel service door with a push bar and kick plate; opens into +z.
    public static GameObject ServiceDoor(Transform parent, float open = 0)
        => HingedDoor("service_door", parent, PaintedGrey, Stainless, true, open);

    // Single leaf in a 0.9 x 2.1 opening, hinged on the left seen from the front. The
    // hinge axis (and the butt hinges) lie on the leaf's +z face, the side it opens
    // towards, so the opened leaf's back corner swings into the opening, not the casing.
    private static GameObject HingedDoor(string kind, Transform parent, Material leafMaterial, Material hardware, bool service, float open)
    {
        const float half = 0.45f, leafW = 0.894f, leafH = 2.09f, casing = 0.07f, t = 0.04f, zc = -t * 0.5f;
        GameObject root = Root(kind, parent);
        KitMesh frame = new KitMesh();
        for (int s = -1; s <= 1; s += 2) frame.Box(leafMaterial, V(s * (half + casing * 0.5f), (2.1f + casing) * 0.5f, 0), V(casing, 2.1f + casing, 0.14f));
        frame.Box(leafMaterial, V(0, 2.1f + casing * 0.5f, 0), V(2 * half, casing, 0.14f));
        foreach (float y in new[] { 0.25f, 1.05f, 1.85f }) frame.Box(hardware, V(-half + 0.002f, y, -zc), V(0.012f, 0.1f, 0.025f));
        Part(frame, "Frame", root, Vector3.zero, kind + "_frame");
        for (int s = -1; s <= 1; s += 2) Collide(root, V(s * (half + casing * 0.5f), (2.1f + casing) * 0.5f, 0), V(casing, 2.1f + casing, 0.14f));
        Collide(root, V(0, 2.1f + casing * 0.5f, 0), V(2 * half, casing, 0.14f));
        KitMesh leaf = new KitMesh();
        float cx = leafW * 0.5f, cy = 0.005f + leafH * 0.5f;
        leaf.Box(leafMaterial, V(cx, cy, zc), V(leafW, leafH, t));
        if (service)
        {
            leaf.Box(hardware, V(cx, 1.0f, zc - 0.055f), V(0.66f, 0.035f, 0.03f));
            for (int e = -1; e <= 1; e += 2) leaf.Box(hardware, V(cx + e * 0.36f, 1.0f, zc - 0.045f), V(0.07f, 0.09f, 0.05f));
            leaf.Box(hardware, V(cx, 0.13f, zc - 0.0215f), V(leafW - 0.04f, 0.25f, 0.003f));
        }
        else
        {
            float hx = leafW - 0.07f;
            for (int f = -1; f <= 1; f += 2)
            {
                leaf.Cylinder(hardware, V(hx, 1.0f, zc + f * 0.02f), V(hx, 1.0f, zc + f * 0.03f), 0.026f, 10);
                leaf.Box(hardware, V(hx, 1.0f, zc + f * 0.037f), V(0.018f, 0.018f, 0.016f));
                leaf.Box(hardware, V(hx - 0.055f, 1.0f, zc + f * 0.045f), V(0.12f, 0.018f, 0.018f));
            }
        }
        GameObject leafObject = leaf.Emit("Door", root.transform, V(-half + 0.002f, 0, -zc), Quaternion.Euler(0, -open, 0), Folder, kind + "_leaf");
        Collide(leafObject, V(cx, cy, zc), V(leafW, leafH, t));
        return root;
    }

    // One 2.0 x 3.0 facade bay: 40 mm half mullions each side (adjacent bays form 80 mm),
    // kick panel to 0.35, sill, transom bar at 2.6, fascia and head, one "Glass" child.
    // Cracked adds radial crack lines ("Cracks" under Glass); Broken leaves a jagged
    // hole in the pane and fragments ("Shards") on the floor inside and out and on the sill.
    // width (default 2.0) narrows the bay for sidelights; the frame sections stay the same.
    public static GameObject WindowModule(Transform parent, Pane state = Pane.Intact, float width = 2.0f)
    {
        const float gy0 = 0.43f, gy1 = 2.56f;
        float gw = width - 0.08f, mullion = width * 0.5f - 0.02f;
        GameObject root = Root("window_module", parent);
        KitMesh k = new KitMesh();
        for (int s = -1; s <= 1; s += 2) k.Box(FrameMetal, V(s * mullion, 1.5f, 0), V(0.04f, 3.0f, 0.12f));
        k.Box(FrameMetal, V(0, 0.175f, 0), V(gw, 0.35f, 0.10f), false);
        k.Box(FrameMetal, V(0, 0.39f, 0), V(gw, 0.08f, 0.14f));
        k.Box(FrameMetal, V(0, 2.6f, 0), V(gw, 0.08f, 0.12f));
        k.Box(FrameMetal, V(0, 2.78f, 0), V(gw, 0.28f, 0.10f));
        k.Box(FrameMetal, V(0, 2.96f, 0), V(gw, 0.08f, 0.14f));
        foreach (float y in new[] { 0.05f, 0.30f }) k.Box(Bronze, V(0, y, -0.052f), V(gw - 0.12f, 0.008f, 0.004f));
        Part(k, "Frame", root, Vector3.zero, "window_frame");
        for (int s = -1; s <= 1; s += 2) Collide(root, V(s * mullion, 1.5f, 0), V(0.04f, 3.0f, 0.12f));
        Collide(root, V(0, gy0 * 0.5f, 0), V(gw, gy0, 0.14f));
        Collide(root, V(0, (gy1 + 3.0f) * 0.5f, 0), V(gw, 3.0f - gy1, 0.14f));
        float gh = gy1 - gy0;
        // An intact or cracked pane stops pointer, teleport and settling rays; a broken one leaves the hole open.
        if (state != Pane.Broken) Collide(root, V(0, (gy0 + gy1) * 0.5f, 0), V(gw, gh, 0.01f));
        GameObject glass = PaneChild("Glass", root.transform, V(0, (gy0 + gy1) * 0.5f, 0), gw, gh, 0.01f, state == Pane.Broken, 5, new Vector2(0.1f, -0.15f));
        if (state == Pane.Cracked) Cracks(glass.transform, gw, gh);
        if (state == Pane.Broken)
        {
            KitMesh shards = new KitMesh();
            System.Random rng = new System.Random(13);
            Spill(shards, rng, V(0, 0.003f, 0.65f), width * 0.45f, 0.55f, 11);
            Spill(shards, rng, V(0, 0.003f, -0.35f), width * 0.45f, 0.25f, 7);
            Spill(shards, rng, V(0, 0.432f, 0), width * 0.425f, 0.03f, 4);
            Part(shards, "Shards", root, Vector3.zero, "window_shards");
        }
        return root;
    }

    // Radial crack lines from an impact point plus two rings, drawn 5.5 mm off both faces
    // of a gw x gh pane in glass's local xy plane ("Cracks" child; the night dressing
    // also cracks a case's top pane through a rotated child).
    public static void Cracks(Transform glass, float gw, float gh)
    {
        KitMesh k = new KitMesh();
        System.Random rng = new System.Random(3);
        Vector2 impact = new Vector2(0.3f, -0.2f);
        const int rays = 12;
        Vector2[] dirs = new Vector2[rays];
        for (int i = 0; i < rays; i++)
        {
            float a = i * 2 * Mathf.PI / rays + Range(rng, -0.15f, 0.15f);
            dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 p = impact;
            float segment = Range(rng, 0.35f, 0.9f) / 3;
            for (int s = 0; s < 3; s++)
            {
                float turn = Range(rng, -0.35f, 0.35f);
                Vector2 d = new Vector2(dirs[i].x * Mathf.Cos(turn) - dirs[i].y * Mathf.Sin(turn), dirs[i].x * Mathf.Sin(turn) + dirs[i].y * Mathf.Cos(turn));
                Vector2 q = Clamp(p + d * segment, gw, gh);
                CrackLine(k, p, q, 0.006f - 0.001f * s); // 6 mm at the impact tapering to 4 mm, wide enough not to shimmer.
                p = q;
            }
        }
        foreach (float r in new[] { 0.12f, 0.28f })
            for (int i = 0; i < rays; i++)
                CrackLine(k, Clamp(impact + dirs[i] * r, gw, gh), Clamp(impact + dirs[(i + 1) % rays] * r, gw, gh), r < 0.2f ? 0.005f : 0.004f);
        k.Emit("Cracks", glass, Vector3.zero, Quaternion.identity, Folder, "window_cracks");
    }

    private static void CrackLine(KitMesh k, Vector2 a, Vector2 b, float width)
    {
        if ((b - a).sqrMagnitude < 1e-6f) return;
        k.Stroke(GlassCrack, P(a, -0.0055f), P(b, -0.0055f), width, Vector3.back);
        k.Stroke(GlassCrack, P(a, 0.0055f), P(b, 0.0055f), width, Vector3.forward);
    }

    private static Vector2 Clamp(Vector2 p, float w, float h)
        => new Vector2(Mathf.Clamp(p.x, -w * 0.5f + 0.01f, w * 0.5f - 0.01f), Mathf.Clamp(p.y, -h * 0.5f + 0.01f, h * 0.5f - 0.01f));

    // ---------------------------------------------------------------- street and fittings

    // 0.2 m dark steel bollard, 0.9 m high with a domed top (24 sides).
    public static GameObject Bollard(Transform parent)
    {
        GameObject root = Root("bollard", parent);
        KitMesh k = new KitMesh();
        k.Lathe(FrameMetal, Vector3.zero, Quaternion.identity, new[] { 0.1f, 0.1f, 0.094f, 0.07f, 0 }, new[] { 0f, 0.82f, 0.86f, 0.89f, 0.9f }, 24, true, false, false);
        Part(k, "Body", root, Vector3.zero, "bollard");
        Collide(root, V(0, 0.45f, 0), V(0.2f, 0.9f, 0.2f));
        return root;
    }

    // Wall exit sign: cream housing, green face and EXIT in strokes (both baked emitters).
    public static GameObject ExitSign(Transform parent)
    {
        GameObject root = Root("exit_sign", parent);
        KitMesh k = new KitMesh();
        k.Box(Lacquer, V(0, 0.075f, -0.025f), V(0.35f, 0.15f, 0.05f));
        k.Face(ExitGreen, Vector3.back, V(-0.165f, 0.01f, -0.0505f), V(-0.165f, 0.14f, -0.0505f), V(0.165f, 0.14f, -0.0505f), V(0.165f, 0.01f, -0.0505f));
        const float y0 = 0.04f, y1 = 0.11f, ym = 0.075f, sw = ExitStroke * 0.5f;
        float[] e = { -0.1035f, -0.0585f }, x = { -0.0385f, 0.0065f }, t = { 0.0585f, 0.1035f };
        Letter(k, e[0] + sw, y0, e[0] + sw, y1);
        Letter(k, e[0], y1 - sw, e[1], y1 - sw);
        Letter(k, e[0], ym, e[1] - 0.008f, ym);
        Letter(k, e[0], y0 + sw, e[1], y0 + sw);
        Letter(k, x[0], y0, x[1], y1);
        Letter(k, x[0], y1, x[1], y0);
        Letter(k, 0.0325f, y0, 0.0325f, y1);
        Letter(k, t[0], y1 - sw, t[1], y1 - sw);
        Letter(k, (t[0] + t[1]) * 0.5f, y0, (t[0] + t[1]) * 0.5f, y1 - sw);
        Part(k, "Body", root, Vector3.zero, "exit_sign");
        return root;
    }

    private const float ExitStroke = 0.012f;
    private static void Letter(KitMesh k, float ax, float ay, float bx, float by)
        => k.Stroke(ExitLetters, V(ax, ay, -0.051f), V(bx, by, -0.051f), ExitStroke, Vector3.back);

    // Wall-mounted bullet camera: plate, arm, housing tilted 15 degrees down, hood and lens.
    public static GameObject SecurityCamera(Transform parent)
    {
        GameObject root = Root("security_camera", parent);
        KitMesh k = new KitMesh();
        k.Box(DeviceGrey, V(0, 0.06f, -0.006f), V(0.08f, 0.12f, 0.012f));
        k.Box(DeviceGrey, V(0, 0.08f, -0.087f), V(0.03f, 0.03f, 0.15f));
        k.Box(DeviceGrey, V(0, 0.105f, -0.17f), V(0.035f, 0.05f, 0.035f));
        Vector3 c = V(0, 0.15f, -0.26f);
        Quaternion q = Quaternion.Euler(-15, 0, 0);
        k.Box(DeviceGrey, c, V(0.09f, 0.08f, 0.22f), q);
        k.Box(DeviceGrey, c + q * V(0, 0.048f, -0.02f), V(0.11f, 0.012f, 0.26f), q);
        k.Cylinder(DeviceBlack, c + q * V(0, 0, -0.11f), c + q * V(0, 0, -0.122f), 0.032f, 12);
        Part(k, "Body", root, Vector3.zero, "security_camera");
        return root;
    }

    // Ceiling track (pivot at the ceiling) with three cans aimed 30 degrees forward; lenses off.
    public static GameObject TrackLight(Transform parent)
    {
        GameObject root = Root("track_light", parent);
        KitMesh k = new KitMesh();
        k.Box(DeviceBlack, V(0, -0.0175f, 0), V(1.2f, 0.035f, 0.035f));
        Vector3 aim = V(0, -Mathf.Cos(30 * Mathf.Deg2Rad), -Mathf.Sin(30 * Mathf.Deg2Rad));
        Quaternion along = Quaternion.FromToRotation(Vector3.up, aim);
        foreach (float x in new[] { -0.4f, 0, 0.4f })
        {
            k.Cylinder(Bronze, V(x, -0.035f, 0), V(x, -0.075f, 0), 0.008f, 6, false);
            Vector3 back = V(x, -0.095f, 0) - aim * 0.045f;
            k.Lathe(DeviceBlack, back, along, new[] { 0.04f, 0.04f }, new[] { 0f, 0.12f }, 12, true, true, false);
            k.Lathe(DeviceBlack, back, along, new[] { 0.039f, 0.039f }, new[] { 0.12f, 0.10f }, 12, true, false, false);
            k.Face(LightLens, aim, KitMesh.Ring(back, along, 0.036f, 0.108f, 12, 0));
        }
        Part(k, "Body", root, Vector3.zero, "track_light");
        return root;
    }

    // Low ceiling downlight (pivot at the ceiling plane, everything at or below it, so no
    // ceiling opening is needed): 12 mm trim ring, short reflector stepping back up to
    // a lens 4 mm below the ceiling (off), visible from the floor.
    public static GameObject Downlight(Transform parent)
    {
        GameObject root = Root("downlight", parent);
        KitMesh k = new KitMesh();
        Quaternion down = Quaternion.Euler(180, 0, 0); // Local lathe heights run downwards from the ceiling.
        k.Lathe(Lacquer, Vector3.zero, down, new[] { 0.085f, 0.085f, 0.06f, 0.05f }, new[] { 0f, 0.012f, 0.012f, 0.004f }, 16, true, false, false);
        k.Face(LightLens, Vector3.down, KitMesh.Ring(Vector3.zero, down, 0.05f, 0.004f, 16, 0));
        Part(k, "Body", root, Vector3.zero, "downlight");
        return root;
    }

    // Counter-top card terminal: base, neck and a screen tilted back 15 degrees (screen off).
    public static GameObject PosTerminal(Transform parent)
    {
        GameObject root = Root("pos_terminal", parent);
        KitMesh k = new KitMesh();
        k.Box(DeviceBlack, V(0, 0.006f, 0), V(0.18f, 0.012f, 0.16f), false);
        k.Box(DeviceBlack, V(0, 0.07f, 0.02f), V(0.05f, 0.12f, 0.03f));
        Vector3 c = V(0, 0.2f, 0.03f);
        Quaternion q = Quaternion.Euler(15, 0, 0);
        k.Box(DeviceBlack, c, V(0.30f, 0.20f, 0.025f), q);
        k.Face(DeviceScreen, q * Vector3.back, c + q * V(-0.14f, -0.09f, -0.0126f), c + q * V(-0.14f, 0.09f, -0.0126f),
            c + q * V(0.14f, 0.09f, -0.0126f), c + q * V(0.14f, -0.09f, -0.0126f));
        Part(k, "Body", root, Vector3.zero, "pos_terminal");
        Collide(root, V(0, 0.15f, 0.02f), V(0.30f, 0.30f, 0.16f));
        return root;
    }

    // Wall alarm keypad: grey body, dark display, nine keys and a red status LED (baked emitter).
    public static GameObject AlarmKeypad(Transform parent)
    {
        GameObject root = Root("alarm_keypad", parent);
        KitMesh k = new KitMesh();
        k.Box(DeviceGrey, V(0, 0.08f, -0.0125f), V(0.12f, 0.16f, 0.025f));
        k.Face(DeviceScreen, Vector3.back, V(-0.045f, 0.10f, -0.0251f), V(-0.045f, 0.14f, -0.0251f), V(0.045f, 0.14f, -0.0251f), V(0.045f, 0.10f, -0.0251f));
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                k.Box(DeviceBlack, V(-0.03f + c * 0.03f, 0.03f + r * 0.025f, -0.027f), V(0.022f, 0.018f, 0.004f));
        k.Box(AlarmLed, V(0.05f, 0.15f, -0.027f), V(0.008f, 0.008f, 0.004f));
        Part(k, "Body", root, Vector3.zero, "alarm_keypad");
        return root;
    }

    // ---------------------------------------------------------------- gallery

    // Every fixture (default parameters) and every gallery variant laid out on a grid
    // of eight per row under "StoreKitGallery", always procedural (overrides are not
    // used) so the builders themselves are what gets checked. Entry children carry the
    // fixture or variant name; labels lie on the floor in front of each.
    public static Transform BuildGallery(Transform parent)
    {
        Palette();
        Transform gallery = Group("StoreKitGallery", parent);
        List<string> entries = new List<string>(Names);
        entries.AddRange(Variants);
        for (int i = 0; i < entries.Count; i++)
        {
            Vector3 slot = V((i % 8 - 3.5f) * 2.6f, 0, (i / 8) * 3.2f);
            GameObject g = BuildEntry(entries[i], gallery);
            g.name = entries[i];
            float lift = entries[i] == "track_light" || entries[i] == "downlight" ? 2.4f
                : entries[i] == "exit_sign" || entries[i] == "alarm_keypad" || entries[i] == "security_camera" ? 1.0f : 0;
            g.transform.localPosition = slot + V(0, lift, 0);
            TextMesh label = Text("Label_" + entries[i], gallery, entries[i].Replace("__", " "), slot + V(0, 0.01f, -1.0f), 0, 0.02f, Color.white);
            label.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
        return gallery;
    }

    private static GameObject BuildEntry(string entry, Transform parent)
    {
        switch (entry)
        {
            case "display_case__FrontBroken": return DisplayCase(parent, CaseGlass.FrontBroken);
            case "display_case__AllBroken": return DisplayCase(parent, CaseGlass.AllBroken);
            case "display_case__Drawer": return DisplayCase(parent, CaseGlass.Intact, true, 0.2f);
            case "wall_case__AllBroken": return WallCase(parent, CaseGlass.AllBroken);
            case "office_chair__Toppled": return OfficeChair(parent, true);
            case "safe_large__Closed": return SafeLarge(parent, 0);
            case "storefront_doors__BrokenOpen": return StorefrontDoors(parent, 35, 0, true, false);
            case "window_module__Cracked": return WindowModule(parent, Pane.Cracked);
            case "window_module__Broken": return WindowModule(parent, Pane.Broken);
            default: return Build(entry, parent);
        }
    }

    [MenuItem("Crime Scene Demo/Store Kit Gallery")]
    public static void GalleryMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        BuildGallery(null);
    }
}
#endif
