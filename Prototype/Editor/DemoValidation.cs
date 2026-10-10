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
    private const string ScenesFolder = "Assets/CrimeSceneDemo/Scenes";
    private static readonly List<string> results = new List<string>();
    private static float started;
    private static int frame;
    private static EvidenceCamera evidence;
    private static string failure;

    private static string Output =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/" + SessionState.GetString(OutputKey, "Verification")));

    // --- Edit mode: scene generation and captures ---

    public static void Begin(string outputFolder)
    {
        SessionState.SetString(OutputKey, outputFolder);
        Directory.CreateDirectory(Output);
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
    public static void SaveScene(string sceneName = null)
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
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
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

    public static void EnterPlayMode(string stageKey, string header)
    {
        File.WriteAllText(Path.Combine(Output, "runtime-results.txt"),
            "Unity " + Application.unityVersion + "; Built-in pipeline\n" + header + "\n");
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

    public static T Find<T>() where T : UnityEngine.Object
    {
        T found = UnityEngine.Object.FindFirstObjectByType<T>();
        if (found == null) throw new Exception("FAILED: " + typeof(T).Name + " missing from scene");
        return found;
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
        cone.PlaceAt(new Vector3(-2, 0, 2), 45);
        Check(Vector3.Distance(cone.transform.position, new Vector3(-2, 0, 2)) < 0.001f, "Tool placement");
        DeployedTool post1 = station.Spawn(DemoToolKind.TapePost);
        DeployedTool post2 = station.Spawn(DemoToolKind.TapePost);
        post1.PlaceAt(new Vector3(0, 0, 1), 0);
        post2.PlaceAt(new Vector3(2, 0, 1), 0);
        station.SelectTapePost(post1);
        Check(Find<DemoStatusBoard>().GetComponent<TextMesh>().text.Contains("second post"), "Status board shows the pending tape selection");
        station.SelectTapePost(post2);
        SceneTape tape = station.DeploymentRoot.GetComponentInChildren<SceneTape>();
        Check(tape != null, "Tape connection");
        tape.Refresh();
        Check(Mathf.Abs(tape.Ribbon.localScale.z - 2) < 0.01f, "Tape length");
        post2.PlaceAt(new Vector3(3, 0, 1), 0);
        tape.Refresh();
        Check(Mathf.Abs(tape.Ribbon.localScale.z - 3) < 0.01f, "Tape follows moved post");
        station.RemoveTool(post1);
        tape.Refresh();
        // Measuring reels: placed like posts, they join each other with a labelled
        // ribbon and never join barrier posts, whether placed or selected by hand.
        DeployedTool reel1 = station.Spawn(DemoToolKind.Measure);
        DeployedTool reel2 = station.Spawn(DemoToolKind.Measure);
        reel1.PlaceAt(new Vector3(-1, 0, 2), 0);
        station.NotifyPlaced(reel1);
        reel2.PlaceAt(new Vector3(-1, 0, 3.5f), 0);
        station.NotifyPlaced(reel2);
        SceneTape measure = null;
        foreach (SceneTape ribbon in station.DeploymentRoot.GetComponentsInChildren<SceneTape>())
            if (ribbon.StartAnchor == reel1.TapeAnchor && ribbon.EndAnchor == reel2.TapeAnchor) measure = ribbon;
        Check(measure != null && measure.ShowsDistance && measure.Flat && measure.DistanceLabel.text == "1.50 m",
            "Measuring tape connects two reels and reads the distance");
        reel2.PlaceAt(new Vector3(-1, 0, 4), 0);
        measure.Refresh();
        Check(measure.DistanceLabel.text == "2.00 m" && Mathf.Abs(measure.Ribbon.localScale.z - 2) < 0.01f,
            "Measuring tape label follows the moved reel");
        station.SelectTapePost(reel1);
        Check(Find<DemoStatusBoard>().GetComponent<TextMesh>().text.Contains("second reel"), "Status board asks for the second reel");
        station.CancelTapeSelection();
        DeployedTool post3 = station.Spawn(DemoToolKind.TapePost);
        post3.PlaceAt(new Vector3(-1, 0, 5), 0);
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
        ToolStation station = Find<ToolStation>();
        Check(station.DeploymentRoot.GetComponentInChildren<SceneTape>() == null, "Tape removed after endpoint removal");
        Check(File.Exists(evidence.LastPhotoPath), "Runtime photograph saved");
        Check(evidence.PhotoCount == 1, "Photo count follows saved photographs");
        PhotoFrame photoFrame = UnityEngine.Object.FindFirstObjectByType<PhotoFrame>();
        Check(photoFrame != null && photoFrame.Current != null && photoFrame.Current.width == 1920, "Photo frame shows the saved photograph");
        Check(Vector3.Distance(evidence.transform.Find("PhotoCamera").localPosition, new Vector3(0, 0, 0.1f)) < 0.001f,
            "View photograph restores dedicated camera pose");
        File.Copy(evidence.LastPhotoPath, Path.Combine(Output, "runtime-photo.png"), true);

        SessionReviewRecorder review = Find<SessionReviewRecorder>();
        SessionReviewRecorder.ReviewDocument document = ReadReview(review);
        SessionReviewRecorder.Entry photo = document.entries[document.entries.Count - 1];
        Check(photo.reason == "Photograph" && photo.tools.Count >= 3, "Photograph records deployed tools in the review");
        Check(File.Exists(Path.Combine(review.ReviewFolder, photo.photo)), "Review contains the copied photograph");
        string html = File.ReadAllText(Path.Combine(review.ReviewFolder, "review.html"));
        Check(html.Contains(photo.photo), "HTML review references the photograph");
        Check(html.Contains("class=\"contact-sheet\"") && html.Contains("<table class=\"shot-list\""), "HTML review contains the contact sheet and shot list");
        Check(html.Contains("<svg") && html.Contains(">1</text>"), "HTML review sketches the scene with tool glyphs");
        Rect sketch = review.SketchViewBox();
        Check(sketch.Contains(SessionReviewRecorder.SketchPoint(new Vector3(0, 0, 4), sketch)), "Sketch maps a shop position inside the drawing");
        // A connected pair of posts goes into the snapshot so the review records the ribbon.
        DeployedTool postA = station.Spawn(DemoToolKind.TapePost);
        DeployedTool postB = station.Spawn(DemoToolKind.TapePost);
        postA.PlaceAt(new Vector3(2, 0, 3), 0);
        postB.PlaceAt(new Vector3(3, 0, 3), 0);
        station.SelectTapePost(postA);
        station.SelectTapePost(postB);
        review.SaveSnapshot();
        evidence.transform.position = new Vector3(0, 1.5f, 4);
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
        Check(Vector3.Distance(evidence.transform.localPosition, new Vector3(-3.6f, 1.1f, 1)) < 0.001f, "Reset restores handheld camera");
        SessionReviewRecorder review = Find<SessionReviewRecorder>();
        SessionReviewRecorder.ReviewDocument document = ReadReview(review);
        int last = document.entries.Count - 1;
        Check(last >= 1 && document.entries[last - 1].reason == "Manual snapshot" && document.entries[last].reason == "Before scene reset"
            && document.entries[last].tools.Count >= 3, "Snapshot and reset append review entries with the tools");
        SessionReviewRecorder.Entry snapshot = document.entries[last - 1];
        Check(snapshot.connections.Count == 1 && snapshot.tools[snapshot.connections[0].from].kind == "TapePost"
            && File.ReadAllText(Path.Combine(review.ReviewFolder, "review.html")).Contains("<line "),
            "Snapshot records the tape connection and the sketch draws it");
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
}
#endif
