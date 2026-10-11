using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Tool visuals shared by the Editor prefab builder and Play mode (DemoLighting builds
// its flashlight and UV torch at runtime). Primitive parts (markers, scale, reel) get
// no material here and are styled by the presentation pass; the procedural parts (cone
// shell, stanchion, scene lamp, flashlights, DSLR) carry their own "Tool..." materials
// from Paint(), which the presentation pass leaves alone. DemoToolsBuilder saves the procedural
// meshes and materials as assets before they go into a prefab or scene.
public static class DemoToolGeometry
{
    public static DeployedTool Create(DemoToolKind kind)
    {
        GameObject root = new GameObject(kind.ToString());
        DeployedTool tool = root.AddComponent<DeployedTool>();
        tool.Kind = kind;
        Rigidbody body = root.GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        if (kind == DemoToolKind.Cone)
        {
            // 45 cm traffic cone on a square black base with one reflective white band
            // (the imported Khronos cone replaces all of this when it is present).
            ProceduralMesh basis = new ProceduralMesh();
            basis.ChamferBox(null, new Vector3(0, 0.0175f, 0), new Vector3(0.36f, 0.035f, 0.36f), 0.01f, Quaternion.identity);
            BoxCollider foot = Part("Base", root.transform, basis, Paint("ToolConeBase", new Color(0.05f, 0.05f, 0.05f), 0.25f))
                .AddComponent<BoxCollider>();
            foot.center = new Vector3(0, 0.0175f, 0);
            foot.size = new Vector3(0.36f, 0.035f, 0.36f);
            ProceduralMesh shell = new ProceduralMesh();
            shell.Lathe(null, Vector3.zero, Quaternion.identity, new[] { ConeBase, ConeTop }, new[] { ConeFoot, ConeHeight }, 20, true, false, true);
            CapsuleCollider cone = Part("Shell", root.transform, shell, Paint("ToolConeOrange", new Color(0.95f, 0.30f, 0.04f), 0.35f))
                .AddComponent<CapsuleCollider>();
            cone.center = new Vector3(0, 0.25f, 0);
            cone.radius = 0.1f;
            cone.height = 0.42f;
            ProceduralMesh band = new ProceduralMesh();
            band.Lathe(null, Vector3.zero, Quaternion.identity, new[] { ConeRadius(0.22f) + 0.0015f, ConeRadius(0.31f) + 0.0015f },
                new[] { 0.22f, 0.31f }, 20, true, false, false);
            Part("Band", root.transform, band, Paint("ToolReflectiveWhite", new Color(0.92f, 0.92f, 0.9f), 0.7f));
        }
        else if (kind == DemoToolKind.Marker)
        {
            Cube("Foot",root.transform,new Vector3(0,0.012f,0),new Vector3(0.18f,0.024f,0.16f));
            GameObject front = Cube("Front",root.transform,new Vector3(0,0.105f,-0.034f),
                new Vector3(0.16f,0.2f,0.008f));
            front.transform.localRotation=Quaternion.Euler(20,0,0);
            GameObject back = Cube("Back",root.transform,new Vector3(0,0.105f,0.034f),
                new Vector3(0.16f,0.2f,0.008f));
            back.transform.localRotation=Quaternion.Euler(-20,0,0);
            tool.NumberLabel = Label(front.transform, false);
            tool.BackNumberLabel = Label(back.transform, true);
            tool.SetMarkerNumber(1);
        }
        else if (kind == DemoToolKind.Scale)
        {
            // Forensic L-scale: two 15 cm arms with 1 cm black/white bands, laid flat
            // beside evidence so photographs carry a size reference. Arms collide; bands do not.
            Cube("ArmX",root.transform,new Vector3(0.075f,0.0015f,0.01f),new Vector3(0.15f,0.003f,0.02f));
            Cube("ArmZ",root.transform,new Vector3(0.01f,0.0015f,0.085f),new Vector3(0.02f,0.003f,0.15f));
            for (int i=0;i<15;i++)
            {
                string band = i%2==0 ? "BandBlack" : "BandWhite";
                Visual(band,root.transform,new Vector3(0.005f+i*0.01f,0.0033f,0.01f),new Vector3(0.0095f,0.0006f,0.016f));
                if (i>=2) Visual(band,root.transform,new Vector3(0.01f,0.0033f,0.005f+i*0.01f),new Vector3(0.016f,0.0006f,0.0095f));
            }
        }
        else if (kind == DemoToolKind.Measure)
        {
            // Measuring-tape reel, 8 cm across and lying flat: the body collides, the
            // label plate and hook are decoration. Measuring tape stretches between the
            // anchors on top of two reels, exactly as barrier tape does between posts.
            Primitive("Body",PrimitiveType.Cylinder,root.transform,
                new Vector3(0,0.0125f,0),new Vector3(0.08f,0.0125f,0.08f));
            Visual("Plate",root.transform,new Vector3(0,0.027f,0),new Vector3(0.045f,0.004f,0.03f));
            Visual("Hook",root.transform,new Vector3(0.045f,0.006f,0),new Vector3(0.012f,0.012f,0.02f));
            Anchor(tool,0.03f);
        }
        else if (kind == DemoToolKind.Lamp) Lamp(tool);
        else
        {
            // Retractable-belt stanchion: weighted round base (0.32 m), black pole and
            // a yellow belt cassette with its black slot; the tape leaves from the slot.
            Material black = Paint("ToolStanchionBlack", new Color(0.04f, 0.04f, 0.045f), 0.45f);
            ProceduralMesh basis = new ProceduralMesh();
            basis.Lathe(null, Vector3.zero, Quaternion.identity, new[] { 0.16f, 0.16f, 0.14f, 0.045f, 0 },
                new[] { 0f, 0.018f, 0.032f, 0.045f, 0.045f }, 24, true, true, false);
            BoxCollider foot = Part("Base", root.transform, basis, black).AddComponent<BoxCollider>();
            foot.center = new Vector3(0, 0.0225f, 0);
            foot.size = new Vector3(0.32f, 0.045f, 0.32f);
            ProceduralMesh pole = new ProceduralMesh();
            pole.Cylinder(null, new Vector3(0, 0.045f, 0), new Vector3(0, 0.89f, 0), 0.019f, 12, false);
            pole.Lathe(null, Vector3.zero, Quaternion.identity, new[] { 0.036f, 0.036f }, new[] { 0.885f, 0.905f }, 20, true, true, false);
            pole.Lathe(null, Vector3.zero, Quaternion.identity, new[] { 0.036f, 0.036f, 0.03f }, new[] { 0.955f, 0.975f, 0.98f }, 20, true, false, true);
            pole.Box(null, new Vector3(0.036f, 0.93f, 0), new Vector3(0.014f, 0.045f, 0.032f));
            CapsuleCollider shaft = Part("Post", root.transform, pole, black).AddComponent<CapsuleCollider>();
            shaft.center = new Vector3(0, 0.51f, 0);
            shaft.radius = 0.036f;
            shaft.height = 0.94f;
            ProceduralMesh head = new ProceduralMesh();
            head.Lathe(null, Vector3.zero, Quaternion.identity, new[] { 0.035f, 0.035f }, new[] { 0.905f, 0.955f }, 20, true, false, false);
            Part("Head", root.transform, head, Paint("ToolStanchionYellow", new Color(0.95f, 0.78f, 0.06f), 0.45f));
            Anchor(tool, 0.93f);
        }
        return tool;
    }

    // ------------------------------------------------------------------ scene lamp

    // Portable LED panel on a light stand (02-store-fixtures-sheet), about 1.6 m tall:
    // three splayed legs on a collar with spreader braces, a two-section centre column,
    // a yoke with tilt knobs and a 0.30 x 0.20 panel tilted 15 degrees down along the
    // tool's forward (+z). The face ("Face") glows only while lit (DeployedTool.LampGlow);
    // the hard-shadowed spot ("LampLight", off in the prefab) sits just in front of it.
    // About 370 triangles; a box round the legs, a capsule round the column and a box
    // round the panel are the colliders (grabbing, pointing and settling use them).
    private static void Lamp(DeployedTool tool)
    {
        Transform root = tool.transform;
        Material black = Paint("ToolStandBlack", new Color(0.035f, 0.035f, 0.04f), 0.45f, 0.3f);
        ProceduralMesh stand = new ProceduralMesh();
        Vector3 collar = new Vector3(0, LampCollar, 0);
        for (int i = 0; i < 3; i++)
        {
            // One leg straight back, two forward-sides: the panel's weight sits over the base.
            float a = (90 + 120 * i) * Mathf.Deg2Rad;
            Vector3 out1 = new Vector3(Mathf.Cos(a), 0, -Mathf.Sin(a));
            Vector3 foot = out1 * LampSpread + new Vector3(0, 0.02f, 0);
            stand.Cylinder(null, collar + out1 * 0.025f, foot, 0.011f, 6, false);
            stand.Cylinder(null, foot + new Vector3(0, -0.02f, 0), foot, 0.016f, 6);
            stand.Cylinder(null, new Vector3(0, 0.32f, 0), Vector3.Lerp(collar, foot, 0.55f), 0.006f, 4, false);
        }
        stand.Lathe(null, Vector3.zero, Quaternion.identity, new[] { 0.026f, 0.026f }, new[] { LampCollar - 0.03f, LampCollar + 0.03f }, 10, true, true, true);
        stand.Cylinder(null, new Vector3(0, 0.3f, 0), new Vector3(0, 1.02f, 0), 0.017f, 8);
        stand.Lathe(null, Vector3.zero, Quaternion.identity, new[] { 0.022f, 0.022f }, new[] { 1.0f, 1.05f }, 10, true, true, true);
        stand.Cylinder(null, new Vector3(0, 1.05f, 0), new Vector3(0, LampPanelY - 0.13f, 0), 0.012f, 8, false);
        // Yoke: a cross bar under the panel and two arms up to the tilt knobs.
        float arm = LampPanelWidth * 0.5f + 0.02f;
        stand.Box(null, new Vector3(0, LampPanelY - 0.13f, 0), new Vector3(2 * arm + 0.016f, 0.016f, 0.02f));
        for (int s = -1; s <= 1; s += 2)
        {
            stand.Box(null, new Vector3(s * arm, LampPanelY - 0.065f, 0), new Vector3(0.016f, 0.13f, 0.02f));
            stand.Cylinder(null, new Vector3(s * arm, LampPanelY, 0), new Vector3(s * (arm + 0.025f), LampPanelY, 0), 0.016f, 8);
        }
        BoxCollider legs = Part("Stand", root, stand, black).AddComponent<BoxCollider>();
        legs.center = new Vector3(0, 0.15f, 0);
        legs.size = new Vector3(LampSpread * 1.8f, 0.3f, LampSpread * 1.6f);
        CapsuleCollider column = legs.gameObject.AddComponent<CapsuleCollider>();
        column.center = new Vector3(0, 0.95f, 0);
        column.radius = 0.04f;
        column.height = 1.3f;

        // Panel housing and its face, in a head pivot tilted down about the knob axis.
        Transform head = new GameObject("Head").transform;
        head.SetParent(root, false);
        head.localPosition = new Vector3(0, LampPanelY, 0);
        head.localRotation = Quaternion.Euler(LampTilt, 0, 0);
        ProceduralMesh housing = new ProceduralMesh();
        housing.ChamferBox(null, new Vector3(0, 0, -0.012f), new Vector3(LampPanelWidth, LampPanelHeight, 0.03f), 0.006f, Quaternion.identity);
        housing.Box(null, new Vector3(0, 0, -0.03f), new Vector3(LampPanelWidth * 0.7f, LampPanelHeight * 0.6f, 0.012f));
        BoxCollider panel = Part("Housing", head, housing, black).AddComponent<BoxCollider>();
        panel.center = new Vector3(0, 0, -0.015f);
        panel.size = new Vector3(LampPanelWidth, LampPanelHeight, 0.04f);
        ProceduralMesh face = new ProceduralMesh();
        float fx = LampPanelWidth * 0.5f - 0.015f, fy = LampPanelHeight * 0.5f - 0.015f;
        face.Face(null, Vector3.forward, new Vector3(-fx, -fy, 0.0035f), new Vector3(fx, -fy, 0.0035f), new Vector3(fx, fy, 0.0035f), new Vector3(-fx, fy, 0.0035f));
        GameObject faceObject = Part("Face", head, face, Glow(Paint("ToolLampPanel", new Color(0.92f, 0.92f, 0.9f), 0.6f), Color.black));
        tool.LampFace = faceObject.GetComponent<MeshRenderer>();
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
            renderer.shadowCastingMode = ShadowCastingMode.Off; // The lamp never shadows its own beam.

        GameObject lightObject = new GameObject("LampLight");
        lightObject.transform.SetParent(head, false);
        lightObject.transform.localPosition = new Vector3(0, 0, 0.02f);
        Light spot = lightObject.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(1f, 0.97f, 0.92f);
        spot.intensity = 1.6f;
        spot.range = LampRange;
        spot.spotAngle = 75;
        // Low-resolution hard shadows so the 8 m beam stops at walls, partitions and
        // counters; ToolStation.UpdateLampShadows keeps it on one lamp at a time, and
        // only while the flashlight (the scene's one realtime shadow) is off. A lamp
        // without its shadow shrinks to LampUnshadowedRange so it cannot light through walls.
        spot.shadows = LightShadows.Hard;
        spot.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
#if UNITY_EDITOR
        spot.lightmapBakeType = LightmapBakeType.Realtime; // Placed at runtime: never part of the bake.
#endif
        spot.enabled = false;
        tool.Lamp = spot;
    }

    public const float LampRange = 8, LampUnshadowedRange = 3.5f;
    public const float LampPanelY = 1.5f, LampPanelWidth = 0.3f, LampPanelHeight = 0.2f, LampTilt = 15;
    private const float LampCollar = 0.62f, LampSpread = 0.36f;

    private const float ConeBase = 0.13f, ConeTop = 0.022f, ConeFoot = 0.035f, ConeHeight = 0.46f;
    private static float ConeRadius(float y) => Mathf.Lerp(ConeBase, ConeTop, (y - ConeFoot) / (ConeHeight - ConeFoot));

    // ------------------------------------------------------------------ flashlights

    // Handheld torch, 3 cm across the body and 16 cm long, with its pivot at the lens
    // centre and the beam along local +z, so a spot light at the same pose sits at the
    // lens. White: polished reflector around a warm LED; UV: purple bezel and a purple
    // front glass. The lens glows (emission) because the model is only shown while the
    // torch is on. It casts no shadows, so it never blocks or speckles its own beam.
    public static GameObject CreateFlashlight(bool uv)
    {
        GameObject root = new GameObject(uv ? "UVFlashlightModel" : "FlashlightModel");
        Quaternion forward = Quaternion.Euler(90, 0, 0); // Lathe heights run along +z.
        Vector3 tail = new Vector3(0, 0, -FlashlightLength);
        Material anodised = Paint("ToolAnodisedBlack", new Color(0.05f, 0.05f, 0.055f), 0.55f, 0.6f);

        ProceduralMesh body = new ProceduralMesh();
        body.Lathe(null, tail, forward, new[] { 0.011f, 0.0135f, 0.0135f, 0.0125f, 0.0125f, 0.0135f, 0.0165f, 0.0175f, 0.0175f },
            new[] { 0.004f, 0.006f, 0.03f, 0.032f, 0.098f, 0.104f, 0.122f, 0.126f, 0.148f }, 16, true, true, false);
        Part("Body", root.transform, body, anodised);

        ProceduralMesh button = new ProceduralMesh();
        button.Lathe(null, tail, forward, new[] { 0.008f, 0.008f }, new[] { 0f, 0.004f }, 12, true, true, false);
        Part("Switch", root.transform, button, Paint("ToolRubber", new Color(0.03f, 0.03f, 0.03f), 0.15f));

        ProceduralMesh bezel = new ProceduralMesh();
        bezel.Lathe(null, tail, forward, new[] { 0.0175f, 0.0185f, 0.0185f, 0.0165f }, new[] { 0.148f, 0.150f, 0.16f, 0.16f }, 16, true, false, false);
        Part("Bezel", root.transform, bezel, uv ? Paint("ToolAnodisedPurple", new Color(0.30f, 0.07f, 0.52f), 0.6f, 0.6f) : anodised);

        // Reflector cone from the bezel's inner edge back to the LED, facing forwards.
        ProceduralMesh reflector = new ProceduralMesh();
        reflector.Lathe(null, tail, forward, new[] { 0.0165f, 0.005f }, new[] { 0.157f, 0.148f }, 16, true, false, false);
        Part("Reflector", root.transform, reflector, Paint("ToolChrome", new Color(0.9f, 0.9f, 0.9f), 0.9f, 1));

        ProceduralMesh lens = new ProceduralMesh();
        float radius = uv ? 0.0166f : 0.005f, depth = uv ? -0.003f : -0.0118f;
        lens.Face(null, Vector3.forward, ProceduralMesh.Ring(new Vector3(0, 0, depth), forward, radius, 0, 16, 0));
        Part("Lens", root.transform, lens, uv
            ? Glow(Paint("ToolUVGlass", new Color(0.25f, 0.05f, 0.45f), 0.9f), new Color(0.45f, 0.1f, 0.9f))
            : Glow(Paint("ToolLED", new Color(1f, 0.95f, 0.8f), 0.9f), new Color(1f, 0.92f, 0.75f) * 1.5f));

        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        return root;
    }

    public const float FlashlightLength = 0.16f;

    // ------------------------------------------------------------------ handheld camera

    // Unbranded DSLR (body about 14 x 10 x 9 cm with a 9 cm lens, about 1000 triangles)
    // built under an existing camera root: grip on the right (+x seen from behind),
    // pentaprism hump with hot shoe, mode and command dials, shutter button, metal
    // lens mount, two-step lens barrel with a recessed dark front element, and a rear
    // screen ("Screen", 16:9, UVs 0..1) showing the viewfinder through CameraScreen.
    // The lens faces local +z and its front stops at z = 0.093, short of the photo
    // camera at (0, 0, 0.1), so the body never appears in its own photographs. "Body"
    // and "Lens" carry the colliders that grabbing and pointing use.
    public static void CreateCameraBody(Transform root)
    {
        Material shell = Paint("ToolCameraBody", new Color(0.045f, 0.045f, 0.05f), 0.3f);
        Material rubber = Paint("ToolRubber", new Color(0.03f, 0.03f, 0.03f), 0.15f);
        Material metal = Paint("ToolMetalDark", new Color(0.35f, 0.35f, 0.36f), 0.55f, 0.8f);
        Quaternion forward = Quaternion.Euler(90, 0, 0);

        ProceduralMesh body = new ProceduralMesh();
        body.ChamferBox(null, new Vector3(0, 0.0025f, -0.035f), new Vector3(0.14f, 0.095f, 0.07f), 0.01f, Quaternion.identity);
        // Rotated so the bevelled edges are the grip's vertical ones (local x is world y).
        body.ChamferBox(null, new Vector3(0.05f, -0.003f, -0.0075f), new Vector3(0.087f, 0.04f, 0.055f), 0.012f, Quaternion.Euler(0, 0, 90));
        body.Lathe(null, new Vector3(0, 0, -0.038f), Quaternion.identity, new[] { 0.032f * 1.4142f, 0.019f * 1.4142f }, new[] { 0.05f, 0.079f },
            4, false, false, true, 45);
        body.Cylinder(null, new Vector3(-0.045f, 0.05f, -0.035f), new Vector3(-0.045f, 0.062f, -0.035f), 0.012f, 16);
        body.Cylinder(null, new Vector3(0.045f, 0.05f, -0.045f), new Vector3(0.045f, 0.057f, -0.045f), 0.01f, 16);
        body.Cylinder(null, new Vector3(0.05f, 0.0405f, 0.008f), new Vector3(0.05f, 0.0465f, 0.008f), 0.006f, 12);
        body.Box(null, new Vector3(0, 0.062f, -0.076f), new Vector3(0.034f, 0.024f, 0.012f));
        for (int i = 0; i < 4; i++) body.Box(null, new Vector3(0.05f, 0.028f - i * 0.016f, -0.0715f), new Vector3(0.008f, 0.008f, 0.003f));
        for (int s = -1; s <= 1; s += 2) body.Box(null, new Vector3(s * 0.0715f, 0.035f, -0.035f), new Vector3(0.006f, 0.012f, 0.008f));
        BoxCollider grip = Part("Body", root, body, shell).AddComponent<BoxCollider>();
        grip.center = new Vector3(0, 0.012f, -0.03f);
        grip.size = new Vector3(0.145f, 0.115f, 0.09f);

        ProceduralMesh trim = new ProceduralMesh();
        trim.Box(null, new Vector3(0, 0.0825f, -0.038f), new Vector3(0.022f, 0.007f, 0.02f));
        trim.Lathe(null, Vector3.zero, forward, new[] { 0.036f, 0.036f }, new[] { 0f, 0.005f }, 24, true, false, true);
        Part("Mount", root, trim, metal);

        ProceduralMesh barrel = new ProceduralMesh();
        barrel.Lathe(null, new Vector3(0, 0, 0.005f), forward, new[] { 0.031f, 0.031f, 0.034f, 0.034f, 0.0365f, 0.0365f, 0.029f, 0.029f },
            new[] { 0f, 0.034f, 0.036f, 0.068f, 0.07f, 0.088f, 0.088f, 0.082f }, 24, true, false, false);
        CapsuleCollider lensCollider = Part("Lens", root, barrel, rubber).AddComponent<CapsuleCollider>();
        lensCollider.direction = 2;
        lensCollider.center = new Vector3(0, 0, 0.05f);
        lensCollider.radius = 0.037f;
        lensCollider.height = 0.09f;

        ProceduralMesh glass = new ProceduralMesh();
        glass.Lathe(null, new Vector3(0, 0, 0.005f), forward, new[] { 0.029f, 0.02f, 0 }, new[] { 0.082f, 0.0845f, 0.0855f }, 24, true, false, false);
        Part("LensGlass", root, glass, Paint("ToolLensGlass", new Color(0.02f, 0.04f, 0.05f), 0.95f, 0.5f));

        // Rear screen: its own quad with 0..1 UVs so a render texture fills it, facing -z.
        const float x0 = -0.052f, x1 = 0.028f, y0 = -0.036f, y1 = 0.009f, z = -0.0702f;
        Mesh quad = new Mesh { name = "CameraScreen" };
        quad.vertices = new[] { new Vector3(x0, y0, z), new Vector3(x0, y1, z), new Vector3(x1, y1, z), new Vector3(x1, y0, z) };
        quad.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        quad.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        quad.RecalculateBounds();
        quad.RecalculateTangents();
        GameObject screen = new GameObject("Screen");
        screen.transform.SetParent(root, false);
        screen.AddComponent<MeshFilter>().sharedMesh = quad;
        MeshRenderer screenRenderer = screen.AddComponent<MeshRenderer>();
        screenRenderer.sharedMaterial = Paint("ToolCameraScreen", Color.white, 0, 0, "Unlit/Texture");
        screenRenderer.shadowCastingMode = ShadowCastingMode.Off;
        screen.AddComponent<CameraScreen>();
    }

    // ------------------------------------------------------------------ shared

    private static readonly Dictionary<string, Material> paints = new Dictionary<string, Material>();

    // Shared tool material, created once per name (Standard unless another shader is
    // named). Materials made in Play mode work in a build because the scene's own
    // Standard materials keep the shader in it.
    public static Material Paint(string name, Color color, float smoothness, float metallic = 0, string shader = "Standard")
    {
        if (paints.TryGetValue(name, out Material cached) && cached != null) return cached;
        Shader found = Shader.Find(shader);
        if (found == null) found = Shader.Find("Standard");
        if (found == null) return null;
        Material m = new Material(found) { name = name };
        if (m.HasProperty("_Color")) m.color = color;
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        paints[name] = m;
        return m;
    }

    // True for the materials Paint() makes (or their saved copies), which restyling passes keep.
    public static bool IsPainted(Material material) => material != null && material.name.StartsWith("Tool") && material.name != "ToolNumber";

    private static Material Glow(Material m, Color emission)
    {
        if (m == null) return null;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emission);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; // A handheld glow, not a light source for GI.
        return m;
    }

    // One renderer for one procedural part.
    private static GameObject Part(string name, Transform parent, ProceduralMesh mesh, Material material)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh.ToMesh(name);
        g.AddComponent<MeshRenderer>().sharedMaterial = material;
        return g;
    }

    public static SceneTape CreateTape()
    {
        GameObject root=new GameObject("SceneTape");
        SceneTape tape=root.AddComponent<SceneTape>();
        tape.Ribbon=Visual("Ribbon",root.transform,Vector3.zero,Vector3.one).transform;
        return tape;
    }

    // Measuring tape: a flat 2.5 cm ribbon with end caps and a distance label, read
    // by SceneTape.Refresh; materials come from the presentation pass like the tools'.
    public static SceneTape CreateMeasureTape()
    {
        SceneTape tape=CreateTape();
        tape.name="MeasureTape";
        tape.Flat=true;
        tape.RibbonHeight=0.025f;
        tape.StartCap=Visual("StartCap",tape.transform,Vector3.zero,new Vector3(0.03f,0.008f,0.02f)).transform;
        tape.EndCap=Visual("EndCap",tape.transform,Vector3.zero,new Vector3(0.03f,0.008f,0.02f)).transform;
        GameObject label=new GameObject("Distance");
        label.transform.SetParent(tape.transform,false);
        TextMesh text=label.AddComponent<TextMesh>();
        DeployedTool.EnsureFont(text);
        text.anchor=TextAnchor.LowerCenter;
        text.alignment=TextAlignment.Center;
        text.fontSize=64;
        text.characterSize=0.006f;
        text.color=Color.white;
        tape.DistanceLabel=text;
        return tape;
    }

    private static void Anchor(DeployedTool tool,float height)
    {
        GameObject anchor=new GameObject("TapeAnchor");
        anchor.transform.SetParent(tool.transform,false);
        anchor.transform.localPosition=new Vector3(0,height,0);
        tool.TapeAnchor=anchor.transform;
    }

    private static TextMesh Label(Transform panel, bool reverse)
    {
        GameObject label=new GameObject("Number");
        label.transform.SetParent(panel,false);
        // Panel's unit cube face is at +/-0.5 along local z.
        label.transform.localPosition=new Vector3(0,0,reverse?0.6f:-0.6f);
        label.transform.localRotation=Quaternion.Euler(0,reverse?180:0,0);
        // Counteract the thin, nonuniform panel scale.
        label.transform.localScale=new Vector3(1/0.16f,1/0.2f,1/0.008f);
        TextMesh text=label.AddComponent<TextMesh>();
        DeployedTool.EnsureFont(text);
        text.anchor=TextAnchor.MiddleCenter;
        text.alignment=TextAlignment.Center;
        text.fontSize=64;
        text.characterSize=0.019f;
        text.color=Color.black;
        return text;
    }

    private static GameObject Cube(string name,Transform parent,Vector3 p,Vector3 size)
        => Primitive(name,PrimitiveType.Cube,parent,p,size);
    // Decoration without a collider, so grabbing and pointing go by the arms.
    private static GameObject Visual(string name,Transform parent,Vector3 p,Vector3 size)
    {
        GameObject g=Cube(name,parent,p,size);
        Collider collider=g.GetComponent<Collider>();
        if (Application.isPlaying) Object.Destroy(collider);
        else Object.DestroyImmediate(collider);
        return g;
    }
    private static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 size)
    {
        GameObject g=GameObject.CreatePrimitive(type);
        g.name=name;
        g.transform.SetParent(parent,false);
        g.transform.localPosition=p;
        g.transform.localScale=size;
        return g;
    }
}
