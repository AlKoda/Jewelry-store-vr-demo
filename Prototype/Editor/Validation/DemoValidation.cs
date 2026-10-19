#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Shared by the scene validators: preview camera, captures, assertion
// bookkeeping and the tool/tape/camera/reset checks both scenes must pass.
// Statics reset on the Play mode domain reload; anything needed across it
// lives in SessionState.
public static class DemoValidation
{
    private const string OutputKey = "DemoValidationOutput";
    public const string ScenesFolder = "Assets/CrimeSceneDemo/Scenes";
    private static readonly List<string> results = new List<string>();
    private static float started;
    private static int frame;
    private static EvidenceCamera evidence;
    // InterfaceChecks destroys a probe status board; PhotoStage, frames later, checks its DontSave panel material was freed.
    private static Material boardProbe;
    private static bool boardProbed;
    private static string failure;

    // Floor points the shared tool, photo and reset stages use. The defaults are the
    // small shop's open floor; another scene's validator assigns its own set in Play
    // mode before ToolStage (statics reset on the domain reload).
    public sealed class ToolTestPoints
    {
        public Vector3 Cone = new Vector3(-2, 0, 2);
        // Barrier posts at Tape and 2 m along +x; the second then moves to 3 m.
        public Vector3 Tape = new Vector3(0, 0, 1);
        // Measuring reels at Reels and 1.5 m along +z (then 2 m), a post 3 m along.
        public Vector3 Reels = new Vector3(-1, 0, 2);
        // Two connected posts for the review snapshot, 1 m apart along +x.
        public Vector3 Pair = new Vector3(2, 0, 3);
        // Where the photo stage leaves the handheld camera before the reset.
        public Vector3 CameraDrop = new Vector3(0, 1.5f, 4);
        // A floor point the review sketch must draw inside its view box.
        public Vector3 Sketch = new Vector3(0, 0, 4);
    }

    public static ToolTestPoints TestPoints = new ToolTestPoints();

    // Runs just before the photo stage resets the session, so a scene's validator can
    // put things in a state the reset must undo (null for the small shop; assigned in
    // Play mode like TestPoints).
    public static Action BeforeReset;

    private const string EditChecksKey = "DemoValidationEditChecks";

    // Checks passed so far in this run: those written before entering Play mode plus
    // those since.
    public static int ChecksPassed
    {
        get
        {
            int passed = SessionState.GetInt(EditChecksKey, 0);
            foreach (string line in results) if (line.StartsWith("PASS: ", StringComparison.Ordinal)) passed++;
            return passed;
        }
    }

    // Docs/Verification/<run>: "VerificationNight" writes to Docs/Verification/Night, the plain "Verification" to Original.
    private static string Output
    {
        get
        {
            string name = SessionState.GetString(OutputKey, "Verification");
            if (name.StartsWith("Verification", StringComparison.Ordinal)) name = name.Substring("Verification".Length);
            if (name.Length == 0) name = "Original";
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Verification/" + name));
        }
    }

    // --- Edit mode: scene generation and captures ---

    public static void Begin(string outputFolder)
    {
        SessionState.SetString(OutputKey, outputFolder);
        SessionState.SetInt(EditChecksKey, 0);
        Directory.CreateDirectory(Output);
        // Start a fresh results file so a generation failure is not appended to the previous run.
        WriteHeader("Scene generation started (Edit mode).");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    public static Camera CreatePreviewCamera(float sunYaw)
    {
        GameObject lightObject = new GameObject("PreviewSun");
        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.6f;
        lightObject.transform.rotation = Quaternion.Euler(55, sunYaw, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.22f, 0.22f);
        GameObject viewing = new GameObject("DesktopPreviewCamera");
        Camera camera = viewing.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 100;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.2f, 0.22f, 0.25f);
        viewing.AddComponent<AudioListener>();
        return camera;
    }

    // First call saves under the name and registers it as the build scene;
    // later calls re-save the same scene.
    public static void SaveScene(string sceneName = null, bool registerBuildScene = true)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (sceneName == null)
        {
            EditorSceneManager.SaveScene(scene);
        }
        else
        {
            Directory.CreateDirectory(ScenesFolder);
            string path = ScenesFolder + "/" + sceneName + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            if (registerBuildScene) EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        }
        AssetDatabase.SaveAssets();
    }

    public static void Capture(Camera camera, string filename, Vector3 position, Vector3 target)
    {
        camera.transform.position = position;
        camera.transform.LookAt(target);
        RenderTexture rt = RenderTexture.GetTemporary(1280, 720, 24);
        RenderTexture previous = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        Texture2D image = null;
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(Output, filename), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
        }
    }

    // Ceilings are hidden solely for this capture, then restored.
    public static void CaptureOverview(Camera camera)
    {
        Transform ceiling = GameObject.Find("JewelryStore_Blockout").transform.Find("Architecture/Ceiling_Optional");
        ceiling.gameObject.SetActive(false);
        Capture(camera, "layout-overview.png", new Vector3(12, 15, -12), new Vector3(0, 0, 4));
        ceiling.gameObject.SetActive(true);
    }

    // Starts runtime-results.txt; Finish appends the results to it.
    public static void WriteHeader(string header)
    {
        File.WriteAllText(Path.Combine(Output, "runtime-results.txt"),
            "Unity " + Application.unityVersion + "; Built-in pipeline\n" + header + "\n");
    }

    public static void EnterPlayMode(string stageKey, string header)
    {
        WriteHeader(header);
        // Edit mode checks would be lost with the Play mode domain reload; write them first.
        SessionState.SetInt(EditChecksKey, ChecksPassed);
        if (results.Count > 0) File.AppendAllText(Path.Combine(Output, "runtime-results.txt"), string.Join("\n", results) + "\n");
        results.Clear();
        SessionState.SetInt(stageKey, 1);
        EditorApplication.isPlaying = true;
    }

    // --- Play mode: staged checks driven from EditorApplication.update ---

    public static bool Running(string stageKey, out int stage)
    {
        stage = SessionState.GetInt(stageKey, 0);
        if (stage != 0) EditorApplication.QueuePlayerLoopUpdate();
        return stage != 0 && EditorApplication.isPlaying && Time.frameCount >= 10;
    }

    public static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAILED: " + label);
        results.Add("PASS: " + label);
    }

    public static void Info(string text) { results.Add("INFO: " + text); }

    private static int Count(string text, string part)
    {
        int count = 0;
        for (int at = text.IndexOf(part, StringComparison.Ordinal); at >= 0; at = text.IndexOf(part, at + part.Length, StringComparison.Ordinal)) count++;
        return count;
    }

    public static T Find<T>() where T : UnityEngine.Object
    {
        T found = UnityEngine.Object.FindFirstObjectByType<T>();
        if (found == null) throw new Exception("FAILED: " + typeof(T).Name + " missing from scene");
        return found;
    }

    // The interface: theme assets, the shared toast feed, the hotbar order, the controls
    // overlay pausing input, context prompts, crouch and zoom, and the VR controller
    // interface. IMGUI never draws in batch mode, so the state behind each element is
    // checked. Leaves desktop mode, no tools placed and the marker sequence untouched.
    public static void InterfaceChecks()
    {
        DemoUiTheme theme = UnityEngine.Object.FindFirstObjectByType<DemoUiTheme>();
        Check(theme != null && theme.Regular != null && theme.Medium != null && theme.SemiBold != null && theme.Shaded != null,
            "Interface theme carries the Barlow fonts and the controller shader");
        // A destroyed cached theme (a scene load) is searched again at once, not after the one-second throttle.
        GameObject staleTheme = new GameObject("StaleTheme");
        DemoUiTheme stale = staleTheme.AddComponent<DemoUiTheme>();
        System.Reflection.BindingFlags uiStatics = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        typeof(DemoUi).GetField("theme", uiStatics).SetValue(null, stale);
        typeof(DemoUi).GetField("themeSearchFrame", uiStatics).SetValue(null, Time.frameCount);
        UnityEngine.Object.DestroyImmediate(staleTheme);
        Check(theme != null && DemoUi.Medium == (DemoLanguage.Font?DemoLanguage.Font:theme.Medium), "UI uses the Arabic glyph font, with the replacement theme as its fallback");
        // The drawer's Settings tab draws DemoUi.Credits; the panel keeps no copy of its own to drift from the licences.
        Check(!string.IsNullOrEmpty(DemoUi.Credits) && typeof(DemoDesktopPanel).GetField("Credits",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) == null,
            "Panel credits come from DemoUi.Credits");
        // Credits: the cached text is Resources/DemoCredits when a build wrote it, else the built-in
        // line; it is loaded once (the second read returns the same string).
        typeof(DemoUi).GetField("credits", uiStatics).SetValue(null, null);
        bool creditsFile = Resources.Load<TextAsset>("DemoCredits") != null;
        string creditsText = DemoUi.Credits;
        Check(DemoUi.DefaultCredits.Contains("Barlow") && !string.IsNullOrEmpty(creditsText)
            && (creditsFile || creditsText == DemoUi.DefaultCredits) && ReferenceEquals(creditsText, DemoUi.Credits),
            "Credits fall back to the built-in line without a credits file");
        // The start menu shows one line of them (DemoLauncher.FirstLine): the first, trimmed, which
        // carries the Barlow font's OFL notice, never the attribution lines below it.
        System.Reflection.MethodInfo firstLine = typeof(DemoLauncher).GetMethod("FirstLine", uiStatics);
        string Summary(string text) => (string)firstLine.Invoke(null, new object[] { text });
        Check(firstLine != null && Summary("Line one \nLine two") == "Line one" && Summary("") == "" && Summary(null) == ""
            && Summary(creditsText) == creditsText.Split('\n')[0].Trim() && Summary(creditsText).Contains("Barlow"),
            "The start menu's credits summary is the first credits line");

        DemoFeedback.Clear();
        for (int i = 1; i <= 5; i++) DemoFeedback.Post("message " + i);
        DemoFeedback.Post("message 5");
        bool kept = DemoFeedback.Recent.Count == DemoFeedback.Capacity && DemoFeedback.Recent[0].Text == "message 2"
            && DemoFeedback.Recent[DemoFeedback.Capacity - 1].Text == "message 5";
        DemoFeedback.Prune(DemoFeedback.Now + DemoFeedback.Lifetime + 1);
        Check(kept && DemoFeedback.Recent.Count == 0, "Feedback feed keeps the newest four messages and expires them");
        DemoFeedback.Post("visitor action");
        DemoFeedback.Post("presenter only", FeedbackTone.Info, true);
        bool skipped = DemoFeedback.TryNewestForHeadset(out DemoFeedback.Message shownToVisitor) && shownToVisitor.Text == "visitor action"
            && DemoFeedback.Recent[DemoFeedback.Recent.Count - 1].Text == "presenter only";
        DemoFeedback.Clear();
        Check(skipped, "Headset toast skips presenter-only messages; the monitor still shows them");

        DemoToolKind[] order = { DemoToolKind.Cone, DemoToolKind.Marker, DemoToolKind.TapePost, DemoToolKind.Scale, DemoToolKind.Measure, DemoToolKind.Lamp };
        bool ordered = true;
        for (int i = 0; i < order.Length; i++) ordered &= DemoDesktopPanel.HotbarKind(i) == order[i];
        Check(ordered, "Hotbar maps keys 1-6 to the tool kinds in order");

        DemoDesktopPanel panel = Find<DemoDesktopPanel>();
        DesktopInteractor desktop = Find<DesktopInteractor>();
        desktop.ReadDesktopInput = false;
        // Forget hover state from frames that still read the mouse.
        desktop.ReleaseAll();
        panel.Layout = PanelLayout.Hidden;
        panel.HelpOpen = true;
        bool paused = panel.CapturesInput && panel.Contains(new Vector2(1, 1));
        panel.HelpOpen = false;
        Check(paused && !panel.CapturesInput, "Help overlay pauses desktop input");

        DeployedTool cone = desktop.SpawnIntoHand(DemoToolKind.Cone);
        bool holding = cone != null && Array.IndexOf(panel.ContextPrompt(), "Rotate") >= 0 && panel.ContextMessage() != null;
        desktop.Remove(cone);
        bool idle = Array.IndexOf(panel.ContextPrompt(), "Walk") >= 0 && panel.ContextMessage() == null;
        Check(holding && idle, "Context prompt follows what the desktop visitor holds");

        ShopWalkController walker = Find<ShopWalkController>();
        walker.SetCrouch(true, true);
        float low = walker.View.localPosition.y;
        walker.SetCrouch(false, true);
        Check(!walker.Crouching && Mathf.Abs(low - walker.CrouchEyeHeight) < 0.001f && Mathf.Abs(walker.View.localPosition.y - walker.EyeHeight.y) < 0.001f,
            "Crouch lowers the desktop eye height");
        Camera view = walker.View.GetComponent<Camera>();
        walker.SetZoom(true, true);
        float zoomed = view.fieldOfView;
        walker.SetZoom(false, true);
        Check(walker.ZoomFieldOfView < walker.FieldOfView && Mathf.Abs(zoomed - walker.ZoomFieldOfView) < 0.01f
            && Mathf.Abs(view.fieldOfView - walker.FieldOfView) < 0.01f, "Zoom narrows the view");

        // DestroyImmediate, so ToolStage's Find<DemoStatusBoard>() later in this tick cannot return the probe; the
        // board's own Destroy of the material lands at the end of the frame.
        GameObject probeBoard = new GameObject("ProbeStatusBoard");
        probeBoard.AddComponent<DemoStatusBoard>();
        boardProbe = probeBoard.transform.Find("BoardPanel").GetComponent<MeshRenderer>().sharedMaterial;
        boardProbed = boardProbe != null;
        UnityEngine.Object.DestroyImmediate(probeBoard);

        DemoModeSwitch mode = Find<DemoModeSwitch>();
        mode.Apply(true);
        XRControllerInput left = null, right = null;
        foreach (XRControllerInput hand in UnityEngine.Object.FindObjectsByType<XRControllerInput>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (hand.Left) left = hand; else right = hand;
        bool built = left != null && right != null && left.Hud != null && right.Hud != null
            && left.Hud.Model != null && right.Hud.Model != null && right.Hud.TooltipCount >= 5 && left.Hud.TooltipCount >= 6
            && left.Hud.StatusText != null && left.Hud.StatusText.text.Contains("Next marker") && right.Hud.StatusText == null;
        Check(built, "Controller tooltips and status card build on both hands");
        bool carousel = false;
        if (right != null && right.Hud != null)
        {
            right.SetSpawnKind(DemoToolKind.Cone);
            right.CycleKind();
            carousel = right.SpawnKind == DemoToolKind.Marker && right.Hud.CarouselVisible
                && right.Hud.CarouselLabel == DeployedTool.Title(DemoToolKind.Marker) && right.Label.text.Contains("new marker");
            right.SetSpawnKind(DemoToolKind.Cone);
        }
        // One frame of the controller interface (fades, highlights, wrist card, toast),
        // which otherwise only runs in the headset: it must not throw and must count a new tool.
        bool updated = false, faded = false, lateNoop = false;
        if (built)
        {
            System.Reflection.MethodInfo hudFrame = typeof(XRHandHud).GetMethod("LateUpdate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            ToolStation station = Find<ToolStation>();
            DeployedTool probe = station.Spawn(DemoToolKind.Cone);
            hudFrame.Invoke(left.Hud, null);
            hudFrame.Invoke(right.Hud, null);
            updated = probe != null && left.Hud.StatusText.text.Contains("1 tool");
            station.RemoveTool(probe);

            // Faded hints stop rendering: past onboarding and not looked at, the tips' alpha is 0 (forced, so
            // short batch-mode frames need no fade loop) and every hint renderer switches off.
            System.Reflection.FieldInfo tipAlphaField = typeof(XRHandHud).GetField("tipAlpha",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            float onboard = right.Hud.OnboardingSeconds, look = right.Hud.LookDistance;
            object tipWas = tipAlphaField.GetValue(right.Hud);
            right.Hud.OnboardingSeconds = 0;
            right.Hud.LookDistance = 0;
            tipAlphaField.SetValue(right.Hud, 0f);
            hudFrame.Invoke(right.Hud, null);
            Renderer hint = right.Label.GetComponent<Renderer>();
            faded = !hint.enabled;
            right.Hud.OnboardingSeconds = onboard;
            right.Hud.LookDistance = look;
            tipAlphaField.SetValue(right.Hud, tipWas);
            // ShowHints switches the renderers back at once, on both hands (the frame above may have faded the left).
            right.Hud.ShowHints(XRControllerInput.HintsVisible);
            left.Hud.ShowHints(XRControllerInput.HintsVisible);
            faded &= hint.enabled == XRControllerInput.HintsVisible;

            // Without a headset the before-render refresh finds no device and leaves the hand where Update put it.
            System.Reflection.MethodInfo latePose = typeof(XRControllerInput).GetMethod("LatePose",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Vector3 handAt = right.transform.localPosition;
            Quaternion handTurn = right.transform.localRotation;
            lateNoop = latePose != null;
            if (lateNoop)
            {
                latePose.Invoke(right, null);
                lateNoop = right.transform.localPosition == handAt && right.transform.localRotation == handTurn;
            }
        }
        // The headset interface has its own layer: the head (= desktop) camera draws it, the photo lens and the
        // map cull it. Not every renderer under the hands: a torch held in VR is deliberately on Default.
        int uiBit = 1 << DemoUi.HeadsetUiLayer;
        Camera lens = Find<EvidenceCamera>().transform.Find("PhotoCamera").GetComponent<Camera>();
        Camera mapCamera = typeof(DemoOverviewMap).GetField("overview",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(Find<DemoOverviewMap>()) as Camera;
        Camera headCamera = right != null && right.Locomotion != null && right.Locomotion.Head != null ? right.Locomotion.Head.GetComponent<Camera>() : null;
        Transform carouselRoot = right != null ? right.transform.Find("ToolCarousel") : null;
        Transform toast = right != null && right.transform.parent != null ? right.transform.parent.Find("XRToast") : null;
        bool headsetOnly = built && right.Hud.Model.gameObject.layer == DemoUi.HeadsetUiLayer
            && right.Label.gameObject.layer == DemoUi.HeadsetUiLayer && left.Hud.StatusText.gameObject.layer == DemoUi.HeadsetUiLayer
            && carouselRoot != null && carouselRoot.gameObject.layer == DemoUi.HeadsetUiLayer
            && toast != null && toast.gameObject.layer == DemoUi.HeadsetUiLayer
            && (lens.cullingMask & uiBit) == 0 && mapCamera != null && (mapCamera.cullingMask & uiBit) == 0
            && (view.cullingMask & uiBit) != 0 && (headCamera == null || (headCamera.cullingMask & uiBit) != 0);
        mode.Apply(false);
        Check(carousel && right.SpawnKind == DemoToolKind.Cone, "Tool carousel shows the newly selected kind");
        Check(updated, "Controller interface updates a frame and counts a new tool");
        Check(faded, "Faded controller hints stop rendering");
        Check(lateNoop, "Before-render pose refresh leaves the controllers alone without a headset");
        Check(headsetOnly, "Headset interface stays out of evidence photos, the viewfinder and the map");

        Vector2 refused = XRControllerInput.Pulse(XRControllerInput.HandEvent.Refused);
        Vector2 placed = XRControllerInput.Pulse(XRControllerInput.HandEvent.Place);
        Vector2 grabbed = XRControllerInput.Pulse(XRControllerInput.HandEvent.Grab);
        Check(refused.x > placed.x && placed.x > grabbed.x && grabbed.x > 0 && refused.y >= placed.y,
            "Haptic pulses rank refusal above placement above grab");
        DefaultExecutionOrder headOrder = (DefaultExecutionOrder)Attribute.GetCustomAttribute(typeof(XRHeadTracking), typeof(DefaultExecutionOrder));
        DefaultExecutionOrder handOrder = (DefaultExecutionOrder)Attribute.GetCustomAttribute(typeof(XRControllerInput), typeof(DefaultExecutionOrder));
        Check(headOrder != null && handOrder != null && headOrder.order < handOrder.order && handOrder.order < 0,
            "Head and controller poses are applied before HandInteractor places the held tool");
    }

    // Edit mode, no scene needed. With the project in the repository (UnityProject and
    // UnityProject_Demo beside the README): Open-In-Unity.cmd and Tools/build-demo.cmd (behind
    // Play-Keyboard.cmd and Play-VR.cmd) call Tools/sync-project.cmd before Unity starts. It mirrors
    // Scripts, Editor and Shaders with their committed .meta files, and ThirdParty keeping the
    // project's own .meta files, with robocopy /MIR into a checked project. Then the twelve
    // realism packs, which reach a project only by that mirror: none (a project from the ZIP alone)
    // is reported, some but not all fail. Then the licence notices a player build copies
    // (DemoBuild.LicenseFiles), on a probe folder for its naming and skip rules and on this project's ThirdParty.
    public static void LauncherChecks()
    {
        string repo = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        string sync = Path.Combine(repo, "Tools", "sync-project.cmd");
        if (!File.Exists(sync)) Info("Launchers not found beside this project: launcher checks skipped");
        else
        {
            string text = File.ReadAllText(sync);
            bool mirrors = true;
            foreach (string part in new[] { "robocopy", "/MIR", "call :mirror Scripts", "call :mirror Editor", "call :mirror Shaders",
                         "call :mirror ThirdParty /XF *.meta", "\\Assets\\", "\\ProjectSettings\\", "errorlevel 8" })
                mirrors &= text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
            bool editor = SyncsBefore(Path.Combine(repo, "Open-In-Unity.cmd"), "start \"\"");
            bool build = SyncsBefore(Path.Combine(repo, "Tools", "build-demo.cmd"), "-executeMethod DemoBuild.BuildLatest");
            bool play = true;
            foreach (string launcher in new[] { "Play-Keyboard.cmd", "Play-VR.cmd" })
            {
                string path = Path.Combine(repo, launcher);
                play &= File.Exists(path) && File.ReadAllText(path).IndexOf("Tools\\build-demo.cmd", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            Info("Launchers in " + repo + ": sync-project " + (mirrors ? "mirrors the four folders" : "INCOMPLETE") + "; Open-In-Unity "
                + (editor ? "syncs first" : "does NOT sync first") + "; build-demo " + (build ? "syncs first" : "does NOT sync first")
                + "; Play-Keyboard and Play-VR " + (play ? "use build-demo" : "do NOT both use build-demo"));
            Check(mirrors && editor && build && play, "The three launchers mirror the project through Tools/sync-project.cmd, metas included for code");
        }

        // The twelve realism packs travel only by the mirror (the ZIP leaves them out for size).
        List<string> present = new List<string>(), absent = new List<string>(), unlicensed = new List<string>();
        foreach (string pack in PackImport.Packs)
        {
            if (!AssetDatabase.IsValidFolder(DemoAssetLibrary.Root + "/" + pack)) { absent.Add(pack); continue; }
            present.Add(pack);
            if (!File.Exists(Path.Combine(DemoBuild.ThirdPartyFolder, pack, "LICENSE.txt"))) unlicensed.Add(pack);
        }
        if (present.Count == 0) Info("No realism pack in this project (unpacked from the ZIP alone; run Tools/sync-project.cmd): pack mirror check skipped");
        else
        {
            // A stale mirror shows as repository files the project lacks (Unity writes the .meta files).
            string source = Path.Combine(repo, "Prototype", "ThirdParty");
            if (Directory.Exists(source))
            {
                List<string> stale = new List<string>();
                foreach (string pack in PackImport.Packs)
                    if (Directory.Exists(Path.Combine(source, pack)))
                        stale.Add(pack + " " + FilesMissing(Path.Combine(source, pack), Path.Combine(DemoBuild.ThirdPartyFolder, pack)));
                Info("Realism pack files in the repository but not in this project: " + string.Join(", ", stale));
            }
            Info("Realism packs present: " + present.Count + " of " + PackImport.Packs.Length + (absent.Count > 0 ? "; absent: " + string.Join(", ", absent) : "")
                + (unlicensed.Count > 0 ? "; without LICENSE.txt: " + string.Join(", ", unlicensed) : ""));
            Check(absent.Count == 0 && unlicensed.Count == 0, "All twelve realism packs are mirrored together, each with its LICENSE.txt");
        }

        // The notices a player build copies to ThirdPartyLicenses. On a probe: a pack's LICENSE.txt
        // and its Licenses/*.txt ship under the pack's name, a licence deeper in a pack under its
        // folders' names; other text, other extensions and folders Unity ignores (a trailing ~, a
        // leading dot) do not, and the list is sorted by the published name.
        string probe = Path.Combine(Path.GetTempPath(), "JewelryLicenceProbe");
        List<string> names = new List<string>();
        try
        {
            if (Directory.Exists(probe)) Directory.Delete(probe, true);
            foreach (string file in new[] { "A/LICENSE.txt", "A/Licenses/Dog.txt", "A/Licenses/readme.md", "A/Licenses/Old.txt~", "A/Notes.txt",
                "A/Source~/LICENSE.txt", "B/Originals/License.txt", ".hidden/LICENSE.txt", "C~/LICENSE.txt" })
            {
                string path = Path.Combine(probe, file);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, "probe");
            }
            foreach (KeyValuePair<string, string> notice in DemoBuild.LicenseFiles(probe)) names.Add(notice.Value);
        }
        finally { if (Directory.Exists(probe)) Directory.Delete(probe, true); }
        bool rules = string.Join("|", names) == "A.txt|A_Dog.txt|B_Originals_License.txt";
        // This project: every pack folder's LICENSE.txt as <pack>.txt, every .txt of its Licenses folder as <pack>_<file>.
        HashSet<string> shipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> notice in DemoBuild.LicenseFiles(DemoBuild.ThirdPartyFolder)) shipped.Add(notice.Value);
        List<string> unshipped = new List<string>();
        if (Directory.Exists(DemoBuild.ThirdPartyFolder))
            foreach (string folder in Directory.GetDirectories(DemoBuild.ThirdPartyFolder))
            {
                string pack = Path.GetFileName(folder);
                if (pack.EndsWith("~", StringComparison.Ordinal) || pack.StartsWith(".", StringComparison.Ordinal)) continue;
                if (File.Exists(Path.Combine(folder, "LICENSE.txt")) && !shipped.Contains(pack + ".txt")) unshipped.Add(pack + ".txt");
                string licences = Path.Combine(folder, "Licenses");
                if (!Directory.Exists(licences)) continue;
                foreach (string file in Directory.GetFiles(licences, "*.txt"))
                    if (string.Equals(Path.GetExtension(file), ".txt", StringComparison.OrdinalIgnoreCase) && !shipped.Contains(pack + "_" + Path.GetFileName(file)))
                        unshipped.Add(pack + "_" + Path.GetFileName(file));
            }
        Info("Licence copy: the probe publishes " + (names.Count > 0 ? string.Join(", ", names) : "nothing") + "; this project ships " + shipped.Count + " notices"
            + (unshipped.Count > 0 ? "; not shipped: " + string.Join(", ", unshipped) : ""));
        Check(rules && unshipped.Count == 0, "The licence copy ships every pack LICENSE.txt and Licenses/*.txt and skips ignored folders");
    }

    // Whether the batch file runs "call ...Tools\sync-project.cmd" on a line before the first line
    // holding start (remarks and echoes aside); false without the file.
    private static bool SyncsBefore(string file, string start)
    {
        if (!File.Exists(file)) return false;
        bool synced = false;
        foreach (string raw in File.ReadAllLines(file))
        {
            string line = raw.Trim();
            if (line.StartsWith("rem ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("::", StringComparison.Ordinal)
                || line.StartsWith("echo", StringComparison.OrdinalIgnoreCase)) continue;
            synced |= line.StartsWith("call ", StringComparison.OrdinalIgnoreCase) && line.IndexOf("Tools\\sync-project.cmd", StringComparison.OrdinalIgnoreCase) >= 0;
            if (line.IndexOf(start, StringComparison.OrdinalIgnoreCase) >= 0) return synced;
        }
        return false;
    }

    // Files (not .meta) under from with no file at the same relative path under to.
    private static int FilesMissing(string from, string to)
    {
        string root = Path.GetFullPath(from);
        int missing = 0;
        foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
            string relative = Path.GetFullPath(file).Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!File.Exists(Path.Combine(to, relative))) missing++;
        }
        return missing;
    }

    // Spawning, numbering, placement, tape following and a photo capture start.
    public static void ToolStage()
    {
        started = Time.realtimeSinceStartup;
        ToolStation station = Find<ToolStation>();
        Check(station.DeploymentRoot != null && station.SpawnPoint != null, "Tool station exists");
        DeployedTool cone = station.Spawn(DemoToolKind.Cone);
        DeployedTool marker1 = station.Spawn(DemoToolKind.Marker);
        DeployedTool marker2 = station.Spawn(DemoToolKind.Marker);
        Check(marker1.MarkerNumber == 1 && marker2.MarkerNumber == 2, "Sequential marker numbers");
        Check(!string.IsNullOrEmpty(marker1.NumberLabel.text) && marker1.NumberLabel.font != null, "Marker label and font assigned");
        ToolTestPoints points = TestPoints;
        cone.PlaceAt(points.Cone, 45);
        Check(Vector3.Distance(cone.transform.position, points.Cone) < 0.001f, "Tool placement");
        DeployedTool post1 = station.Spawn(DemoToolKind.TapePost);
        DeployedTool post2 = station.Spawn(DemoToolKind.TapePost);
        post1.PlaceAt(points.Tape, 0);
        post2.PlaceAt(points.Tape + new Vector3(2, 0, 0), 0);
        station.SelectTapePost(post1);
        Check(Find<DemoStatusBoard>().GetComponent<TextMesh>().text.Contains("second post"), "Status board shows the pending tape selection");
        station.SelectTapePost(post2);
        SceneTape tape = station.DeploymentRoot.GetComponentInChildren<SceneTape>();
        Check(tape != null, "Tape connection");
        tape.Refresh();
        Check(Mathf.Abs(tape.Ribbon.localScale.z - 2) < 0.01f, "Tape length");
        post2.PlaceAt(points.Tape + new Vector3(3, 0, 0), 0);
        tape.Refresh();
        Check(Mathf.Abs(tape.Ribbon.localScale.z - 3) < 0.01f, "Tape follows moved post");
        station.RemoveTool(post1);
        tape.Refresh();
        // Measuring reels: placed like posts, they join each other with a labelled
        // ribbon and never join barrier posts, whether placed or selected by hand.
        DeployedTool reel1 = station.Spawn(DemoToolKind.Measure);
        DeployedTool reel2 = station.Spawn(DemoToolKind.Measure);
        reel1.PlaceAt(points.Reels, 0);
        station.NotifyPlaced(reel1);
        reel2.PlaceAt(points.Reels + new Vector3(0, 0, 1.5f), 0);
        station.NotifyPlaced(reel2);
        SceneTape measure = null;
        foreach (SceneTape ribbon in station.DeploymentRoot.GetComponentsInChildren<SceneTape>())
            if (ribbon.StartAnchor == reel1.TapeAnchor && ribbon.EndAnchor == reel2.TapeAnchor) measure = ribbon;
        Check(measure != null && measure.ShowsDistance && measure.Flat && measure.DistanceLabel.text == "1.50 m",
            "Measuring tape connects two reels and reads the distance");
        reel2.PlaceAt(points.Reels + new Vector3(0, 0, 2), 0);
        measure.Refresh();
        Check(measure.DistanceLabel.text == "2.00 m" && Mathf.Abs(measure.Ribbon.localScale.z - 2) < 0.01f,
            "Measuring tape label follows the moved reel");
        station.SelectTapePost(reel1);
        Check(Find<DemoStatusBoard>().GetComponent<TextMesh>().text.Contains("second reel"), "Status board asks for the second reel");
        station.CancelTapeSelection();
        DeployedTool post3 = station.Spawn(DemoToolKind.TapePost);
        post3.PlaceAt(points.Reels + new Vector3(0, 0, 3), 0);
        station.NotifyPlaced(post3);
        bool placedApart = station.PendingPost == null && station.LastPlacedReel == reel2 && station.LastPlacedPost == post3;
        station.SelectTapePost(reel2);
        station.SelectTapePost(post3);
        bool selectionSwitched = station.PendingPost == post3;
        station.CancelTapeSelection();
        int ribbons = 0;
        foreach (SceneTape ribbon in station.DeploymentRoot.GetComponentsInChildren<SceneTape>())
            if (ribbon.StartAnchor == post3.TapeAnchor || ribbon.EndAnchor == post3.TapeAnchor
                || ribbon.StartAnchor == reel2.TapeAnchor || ribbon.EndAnchor == reel2.TapeAnchor) ribbons++;
        Check(placedApart && selectionSwitched && ribbons == 1 && measure.gameObject.activeSelf,
            "Measuring reels and barrier posts never cross-connect");
        // Leave the deployed set as the photo and reset stages expect it (cone, two markers, one post).
        station.RemoveTool(reel1);
        station.RemoveTool(reel2);
        station.RemoveTool(post3);
        evidence = Find<EvidenceCamera>();
        evidence.CaptureFailed.AddListener(message => failure = message);
        evidence.CaptureFromView(Camera.main.transform);
        frame = Time.frameCount;
    }

    // Returns false while the photo is still being written.
    public static bool PhotoStage()
    {
        if (Time.realtimeSinceStartup - started > 30) throw new Exception("Photo capture timeout: " + evidence.Status);
        if (failure != null) throw new Exception(failure);
        if (Time.frameCount <= frame + 3 || evidence.IsCapturing || evidence.LastPhotoPath == null) return false;
        if (boardProbed)
        {
            Check(boardProbe == null, "Status board frees its panel material when destroyed");
            boardProbed = false;
        }
        ToolStation station = Find<ToolStation>();
        Check(station.DeploymentRoot.GetComponentInChildren<SceneTape>() == null, "Tape removed after endpoint removal");
        Check(File.Exists(evidence.LastPhotoPath), "Runtime photograph saved");
        Check(evidence.PhotoCount == 1, "Photo count follows saved photographs");
        PhotoFrame photoFrame = UnityEngine.Object.FindFirstObjectByType<PhotoFrame>();
        Check(photoFrame != null && photoFrame.Current != null && photoFrame.Current.width == 1920, "Photo frame shows the saved photograph");
        Check(photoFrame != null && evidence.LastPhotoDisplay is RenderTexture shownPhoto && shownPhoto.useMipMap
            && shownPhoto.width == evidence.LastPhoto.width / 2 && photoFrame.GetComponent<MeshRenderer>().sharedMaterial.mainTexture == shownPhoto,
            "Photo frame samples a mipmapped copy of the photograph");
        // The PNG is encoded from the reused pixel buffer that LastPhoto is loaded from afterwards; a mismatch means it changed in between.
        Texture2D saved = new Texture2D(2, 2);
        bool decoded = saved.LoadImage(File.ReadAllBytes(evidence.LastPhotoPath));
        Color onDisk = decoded ? saved.GetPixel(960, 540) : Color.clear, inMemory = evidence.LastPhoto.GetPixel(960, 540);
        UnityEngine.Object.Destroy(saved);
        Check(decoded && Mathf.Abs(onDisk.r - inMemory.r) < 0.01f && Mathf.Abs(onDisk.g - inMemory.g) < 0.01f && Mathf.Abs(onDisk.b - inMemory.b) < 0.01f,
            "Saved photograph matches the in-memory photo");
        Check(Vector3.Distance(evidence.transform.Find("PhotoCamera").localPosition, new Vector3(0, 0, 0.1f)) < 0.001f,
            "View photograph restores dedicated camera pose");
        Camera lens = evidence.transform.Find("PhotoCamera").GetComponent<Camera>();
        Check(!lens.allowHDR && lens.allowMSAA, "Photo camera renders LDR with anti-aliasing");
        File.Copy(evidence.LastPhotoPath, Path.Combine(Output, "runtime-photo.png"), true);

        SessionReviewRecorder review = Find<SessionReviewRecorder>();
        SessionReviewRecorder.ReviewDocument document = ReadReview(review);
        SessionReviewRecorder.Entry photo = document.entries[document.entries.Count - 1];
        Check(photo.reason == "Photograph" && photo.tools.Count >= 3, "Photograph records deployed tools in the review");
        Check(File.Exists(Path.Combine(review.ReviewFolder, photo.photo)), "Review contains the copied photograph");
        string html = File.ReadAllText(Path.Combine(review.ReviewFolder, "review.html"));
        Check(html.Contains(photo.photo), "HTML review references the photograph");
        int shots = 0;
        foreach (SessionReviewRecorder.Entry entry in document.entries) if (!string.IsNullOrEmpty(entry.photo)) shots++;
        Check(html.Contains("<figcaption>#" + shots + " \u00b7 " + photo.photo + " \u00b7 entry " + document.entries.Count + " \u00b7 ") && html.Contains(" m \u00b7 yaw ")
            && html.Contains("<a href=\"" + photo.photo + "\">" + photo.photo + "</a></td><td>" + photo.tools.Count + "</td><td>1, 2</td>"),
            "HTML review contact sheet tile and shot list row carry the shot, entry, camera, tools and markers");
        Check(html.Contains("<svg") && html.Contains(">1</text>") && html.Contains("<polygon class=\"camera\""), "HTML review sketches the scene with tool glyphs and the camera");
        Rect sketch = review.SketchViewBox();
        Check(sketch.Contains(SessionReviewRecorder.SketchPoint(TestPoints.Sketch, sketch)), "Sketch maps a shop position inside the drawing");
        // A connected pair of posts goes into the snapshot so the review records the ribbon.
        DeployedTool postA = station.Spawn(DemoToolKind.TapePost);
        DeployedTool postB = station.Spawn(DemoToolKind.TapePost);
        postA.PlaceAt(TestPoints.Pair, 0);
        postB.PlaceAt(TestPoints.Pair + new Vector3(1, 0, 0), 0);
        station.SelectTapePost(postA);
        station.SelectTapePost(postB);
        review.SaveSnapshot();
        evidence.transform.position = TestPoints.CameraDrop;
        if (BeforeReset != null) BeforeReset();
        Find<DemoSession>().ResetSession();
        frame = Time.frameCount;
        return true;
    }

    // Returns false until destroyed objects have been removed.
    public static bool ResetStage()
    {
        if (Time.frameCount <= frame + 2) return false;
        ToolStation station = Find<ToolStation>();
        Check(station.DeploymentRoot.childCount == 0, "Reset clears deployed objects");
        Check(station.NextMarkerNumber == 1, "Reset restarts marker numbering");
        Check(File.Exists(evidence.LastPhotoPath), "Reset preserves photographs");
        Check(Vector3.Distance(evidence.transform.localPosition, evidence.RackPosition) < 0.001f, "Reset restores handheld camera");
        SessionReviewRecorder review = Find<SessionReviewRecorder>();
        SessionReviewRecorder.ReviewDocument document = ReadReview(review);
        int last = document.entries.Count - 1;
        Check(last >= 1 && document.entries[last - 1].reason == "Manual snapshot" && document.entries[last].reason == "Before scene reset"
            && document.entries[last].tools.Count >= 3, "Snapshot and reset append review entries with the tools");
        SessionReviewRecorder.Entry snapshot = document.entries[last - 1];
        string html = File.ReadAllText(Path.Combine(review.ReviewFolder, "review.html"));
        Check(snapshot.connections.Count == 1 && snapshot.tools[snapshot.connections[0].from].kind == "TapePost" && html.Contains("<line ")
            && html.Contains("stroke=\"#" + ColorUtility.ToHtmlStringRGB(DemoDesktopPanel.KindColor(DemoToolKind.TapePost)) + "\""),
            "Snapshot records the tape connection and the sketch draws it in the panel's colour");
        // The stored camera pose is the last capture's, so the snapshot and reset entries neither draw nor caption a camera.
        Check(Count(html, "<svg ") == document.entries.Count && Count(html, "<polygon class=\"camera\"") == 1 && Count(html, " \u00b7 yaw ") == 2,
            "Camera glyph and caption appear only on the photograph entry");
        return true;
    }

    private static SessionReviewRecorder.ReviewDocument ReadReview(SessionReviewRecorder review)
    {
        string path = Path.Combine(review.ReviewFolder, "session.json");
        Check(File.Exists(path), "Review session JSON written");
        return JsonUtility.FromJson<SessionReviewRecorder.ReviewDocument>(File.ReadAllText(path));
    }

    public static void Finish(string stageKey, bool success, string error)
    {
        SessionState.SetInt(stageKey, 0);
        Directory.CreateDirectory(Output);
        File.AppendAllText(Path.Combine(Output, "runtime-results.txt"),
            string.Join("\n", results) + "\n" + (success ? "PASS: Validation completed" : "FAIL: " + error) + "\n");
        Debug.Log(success ? "JEWELRY_VALIDATION_PASS" : "JEWELRY_VALIDATION_FAIL " + error);
        EditorApplication.Exit(success ? 0 : 1);
    }

    // --- Geometry helpers for the night store's area validators (NightStoreValidation*) ---

    // A mesh the triangle rays read: the world box they test first, then the mesh's world-space
    // vertices and triangles. An eager one reads those at once and boxes its vertices; a lazy one
    // is boxed by its renderer and read on the first ray that reaches that box, so the meshes a
    // ray never nears are never read.
    public sealed class RayMesh
    {
        public Bounds Box;
        public Vector3[] World;
        public int[] Triangles;
        internal MeshFilter Unread;
    }

    private static readonly Dictionary<MeshFilter, RayMesh> rayMeshes = new Dictionary<MeshFilter, RayMesh>();

    // The filter's RayMesh, read once per check pass: a pass clears the cache when it starts and
    // ends (ClearRayMeshes), since a mesh that moves in between would keep its old place. Inactive
    // meshes are included; lazy needs the filter's renderer, and the first call in a pass decides.
    // Null without a mesh or with an empty one.
    public static RayMesh RayMeshOf(MeshFilter filter, bool lazy = false)
    {
        if (filter == null) return null;
        if (rayMeshes.TryGetValue(filter, out RayMesh m)) return m;
        m = NewRayMesh(filter, lazy);
        if (m != null) rayMeshes[filter] = m;
        return m;
    }

    // An uncached RayMesh, as RayMeshOf builds it.
    public static RayMesh NewRayMesh(MeshFilter filter, bool lazy = false)
    {
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null || mesh.vertexCount == 0) return null;
        Renderer renderer = lazy ? filter.GetComponent<Renderer>() : null;
        if (renderer != null) return new RayMesh { Box = renderer.bounds, Unread = filter };
        RayMesh m = new RayMesh();
        ReadRayMesh(m, filter);
        for (int i = 0; i < m.World.Length; i++)
            if (i == 0) m.Box = new Bounds(m.World[0], Vector3.zero);
            else m.Box.Encapsulate(m.World[i]);
        return m;
    }

    public static void ClearRayMeshes() => rayMeshes.Clear();

    private static void ReadRayMesh(RayMesh m, MeshFilter filter)
    {
        Mesh mesh = filter.sharedMesh;
        Matrix4x4 toWorld = filter.transform.localToWorldMatrix;
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++) vertices[i] = toWorld.MultiplyPoint3x4(vertices[i]);
        m.World = vertices;
        m.Triangles = mesh.triangles;
        m.Unread = null;
    }

    // Nearest hit of a world-space ray within maxDistance on either side of the meshes'
    // triangles (Moller-Trumbore), with that triangle's winding normal; false when nothing is
    // hit (distance NaN). Null entries are skipped, and boxes the ray misses are never read.
    public static bool TriangleRay(Ray ray, float maxDistance, List<RayMesh> meshes, out float distance, out Vector3 normal)
    {
        float best = float.MaxValue;
        normal = Vector3.zero;
        foreach (RayMesh m in meshes) TriangleRayInto(ray, maxDistance, m, ref best, ref normal);
        distance = best < float.MaxValue ? best : float.NaN;
        return best < float.MaxValue;
    }

    // The same against one mesh (null: no hit).
    public static bool TriangleRay(Ray ray, float maxDistance, RayMesh mesh, out float distance)
    {
        float best = float.MaxValue;
        Vector3 normal = Vector3.zero;
        TriangleRayInto(ray, maxDistance, mesh, ref best, ref normal);
        distance = best < float.MaxValue ? best : float.NaN;
        return best < float.MaxValue;
    }

    private static void TriangleRayInto(Ray ray, float maxDistance, RayMesh m, ref float best, ref Vector3 normal)
    {
        if (m == null) return;
        Bounds box = m.Box;
        box.Expand(0.004f);
        if (!box.IntersectRay(ray, out float enter) || enter > maxDistance) return;
        if (m.Unread != null) ReadRayMesh(m, m.Unread);
        Vector3[] w = m.World;
        int[] tri = m.Triangles;
        if (w == null || w.Length == 0 || tri == null) return;
        Vector3 origin = ray.origin, direction = ray.direction;
        for (int i = 0; i + 2 < tri.Length; i += 3)
        {
            Vector3 a = w[tri[i]], e1 = w[tri[i + 1]] - a, e2 = w[tri[i + 2]] - a, p = Vector3.Cross(direction, e2);
            float det = Vector3.Dot(e1, p);
            if (Mathf.Abs(det) < 1e-12f) continue;
            float inv = 1 / det;
            Vector3 s = origin - a;
            float u = Vector3.Dot(s, p) * inv;
            if (u < 0 || u > 1) continue;
            Vector3 q = Vector3.Cross(s, e1);
            float v = Vector3.Dot(direction, q) * inv;
            if (v < 0 || u + v > 1) continue;
            float t = Vector3.Dot(e2, q) * inv;
            if (t < 0 || t > maxDistance || t >= best) continue;
            best = t;
            normal = Vector3.Cross(e1, e2).normalized;
        }
    }

    // Height of the topmost triangle of the mesh straight below p (a ray down from 0.5 m above
    // it, either side facing); NaN when none lies there.
    public static float SurfaceY(MeshFilter filter, Vector3 p)
    {
        Vector3 origin = p + Vector3.up * 0.5f;
        return TriangleRay(new Ray(origin, Vector3.down), float.MaxValue, NewRayMesh(filter), out float d) ? origin.y - d : float.NaN;
    }

    // A box collider's world bounds round its eight corners, from its own centre and size (no
    // physics query, so no transform sync is needed).
    public static Bounds BoxColliderBounds(BoxCollider box)
    {
        Transform t = box.transform;
        Bounds b = new Bounds(t.TransformPoint(box.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
            b.Encapsulate(t.TransformPoint(box.center + Vector3.Scale(box.size * 0.5f,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
        return b;
    }

    // A box's x/z footprint as a Rect (x = world x, y = world z), as NightStoreLayout.RegionRect.
    public static Rect Footprint(Bounds b) => Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);

    // Shortest x/z distance between two footprints, 0 when they touch or overlap.
    public static float FootprintGap(Rect a, Rect b)
    {
        float dx = Mathf.Max(0, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax)), dz = Mathf.Max(0, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // Gap between two boxes' x/z footprints (0 when they touch or overlap).
    public static float FootprintGap(Bounds a, Bounds b)
    {
        float dx = Mathf.Max(0, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x)), dz = Mathf.Max(0, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // Distance in x/z (Rect x = world x, y = world z) from a point to a rectangle; 0 inside.
    public static float RectPointDistance(Rect r, Vector2 p)
        => Vector2.Distance(p, new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax)));

    public static float PointSegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude > 1e-12f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0;
        return Vector2.Distance(p, a + ab * t);
    }

    // Distance from a segment to a rectangle: 0 when the segment crosses it (slab clipping),
    // else the nearest of the segment's ends to the rectangle and of its corners to the segment.
    public static float RectSegmentDistance(Rect r, Vector2 a, Vector2 b)
    {
        float t0 = 0, t1 = 1;
        Vector2 d = b - a;
        bool crosses = true;
        for (int axis = 0; axis < 2 && crosses; axis++)
        {
            float start = a[axis], step = d[axis], min = axis == 0 ? r.xMin : r.yMin, max = axis == 0 ? r.xMax : r.yMax;
            if (Mathf.Abs(step) < 1e-9f) { crosses = start >= min && start <= max; continue; }
            float u0 = (min - start) / step, u1 = (max - start) / step;
            t0 = Mathf.Max(t0, Mathf.Min(u0, u1));
            t1 = Mathf.Min(t1, Mathf.Max(u0, u1));
            crosses = t0 <= t1;
        }
        if (crosses) return 0;
        float best = Mathf.Min(RectPointDistance(r, a), RectPointDistance(r, b));
        foreach (Vector2 corner in new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) })
            best = Mathf.Min(best, PointSegmentDistance(corner, a, b));
        return best;
    }

    public static string Mm(float metres) => (metres * 1000).ToString("0.0") + " mm";

    // Appends the filter's mesh in world space: its vertices, and its triangles (every
    // submesh) offset past the vertices already listed. Nothing without a mesh.
    public static void AppendWorldMesh(MeshFilter filter, List<Vector3> vertices, List<int> triangles)
    {
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null) return;
        int offset = vertices.Count;
        Matrix4x4 toWorld = filter.transform.localToWorldMatrix;
        foreach (Vector3 v in mesh.vertices) vertices.Add(toWorld.MultiplyPoint3x4(v));
        foreach (int i in mesh.triangles) triangles.Add(offset + i);
    }

    // The first pair of same-facing triangles (a and b, indices into triangles / 3) that lie in
    // one plane, every corner of each within planeTolerance of the other's plane, and overlap
    // more than minOverlap across: each triangle shrunk by minOverlap / 2 from its edges still
    // overlaps the other with positive area. Two such faces z-fight. Triangles that only share an
    // edge or touch at a corner (adjoining faces, a face's own two halves) do not count, and
    // neither do opposite-facing pairs (a butt joint's end faces, a panel's two sides) or
    // degenerate triangles. False (a and b -1) when no pair overlaps. All pairs: meant for meshes
    // of a few thousand triangles.
    public static bool CoplanarOverlap(List<Vector3> vertices, List<int> triangles, float planeTolerance, float minOverlap, out int a, out int b)
    {
        a = b = -1;
        int count = triangles.Count / 3;
        Vector3[] normals = new Vector3[count];
        Bounds[] boxes = new Bounds[count];
        bool[] valid = new bool[count];
        for (int i = 0; i < count; i++)
        {
            Vector3 p0 = vertices[triangles[3 * i]], p1 = vertices[triangles[3 * i + 1]], p2 = vertices[triangles[3 * i + 2]];
            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            valid[i] = n.sqrMagnitude > 1e-18f;
            normals[i] = valid[i] ? n.normalized : Vector3.zero;
            boxes[i] = new Bounds(p0, Vector3.zero);
            boxes[i].Encapsulate(p1);
            boxes[i].Encapsulate(p2);
            boxes[i].Expand(2 * planeTolerance);
        }
        Vector2[] first = new Vector2[3], second = new Vector2[3];
        for (int i = 0; i < count; i++)
        {
            if (!valid[i]) continue;
            for (int j = i + 1; j < count; j++)
            {
                if (!valid[j] || Vector3.Dot(normals[i], normals[j]) < 0.9999f || !boxes[i].Intersects(boxes[j])) continue;
                if (!CoplanarCorners(vertices, triangles, j, normals[i], vertices[triangles[3 * i]], planeTolerance)
                    || !CoplanarCorners(vertices, triangles, i, normals[j], vertices[triangles[3 * j]], planeTolerance)) continue;
                // Both triangles in the plane's own 2D frame, from the first one's first corner.
                Vector3 origin = vertices[triangles[3 * i]], u = (vertices[triangles[3 * i + 1]] - origin).normalized, v = Vector3.Cross(normals[i], u);
                for (int k = 0; k < 3; k++)
                {
                    Vector3 pi = vertices[triangles[3 * i + k]] - origin, pj = vertices[triangles[3 * j + k]] - origin;
                    first[k] = new Vector2(Vector3.Dot(pi, u), Vector3.Dot(pi, v));
                    second[k] = new Vector2(Vector3.Dot(pj, u), Vector3.Dot(pj, v));
                }
                float inset = minOverlap * 0.5f;
                if (PolygonArea(ShrunkOverlap(ShrunkOverlap(new List<Vector2>(first), first, inset), second, inset)) > 1e-12f)
                {
                    a = i;
                    b = j;
                    return true;
                }
            }
        }
        return false;
    }

    // Every corner of triangle t lies within tolerance of the plane through p with normal n.
    private static bool CoplanarCorners(List<Vector3> vertices, List<int> triangles, int t, Vector3 n, Vector3 p, float tolerance)
    {
        for (int k = 0; k < 3; k++)
            if (Mathf.Abs(Vector3.Dot(n, vertices[triangles[3 * t + k]] - p)) > tolerance) return false;
        return true;
    }

    // The part of polygon inside triangle tri shrunk by inset from each of its edges
    // (Sutherland-Hodgman against the three inset half-planes); tri's winding may be either way.
    private static List<Vector2> ShrunkOverlap(List<Vector2> polygon, Vector2[] tri, float inset)
    {
        float winding = Mathf.Sign((tri[1].x - tri[0].x) * (tri[2].y - tri[0].y) - (tri[1].y - tri[0].y) * (tri[2].x - tri[0].x));
        for (int e = 0; e < 3 && polygon.Count > 0; e++)
        {
            Vector2 p = tri[e], edge = tri[(e + 1) % 3] - p;
            float length = edge.magnitude;
            if (length < 1e-9f) return new List<Vector2>();
            List<Vector2> kept = new List<Vector2>();
            for (int k = 0; k < polygon.Count; k++)
            {
                Vector2 c = polygon[k], d = polygon[(k + 1) % polygon.Count];
                float fc = winding * (edge.x * (c.y - p.y) - edge.y * (c.x - p.x)) / length - inset;
                float fd = winding * (edge.x * (d.y - p.y) - edge.y * (d.x - p.x)) / length - inset;
                if (fc >= 0) kept.Add(c);
                if ((fc >= 0) != (fd >= 0)) kept.Add(c + (d - c) * (fc / (fc - fd)));
            }
            polygon = kept;
        }
        return polygon;
    }

    private static float PolygonArea(List<Vector2> polygon)
    {
        float twice = 0;
        for (int k = 0; k < polygon.Count; k++)
        {
            Vector2 c = polygon[k], d = polygon[(k + 1) % polygon.Count];
            twice += c.x * d.y - d.x * c.y;
        }
        return Mathf.Abs(twice) * 0.5f;
    }
}
#endif
