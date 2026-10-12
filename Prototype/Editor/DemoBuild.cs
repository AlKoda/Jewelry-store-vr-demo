#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Windows 64-bit player from the enabled build scenes into Builds/Windows next
// to the repository README. Run from the menu, or in batch mode through
// Build-Windows.cmd, which uses -executeMethod DemoBuild.BuildWindows.
public static class DemoBuild
{
    public const string Executable = "JewelryStoreDemo.exe";
    private static string OutputFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/Windows"));

    [MenuItem("Crime Scene Demo/Build Windows Demo")]
    public static void BuildWindowsFromMenu()
    {
        BuildReport report = Build();
        bool success = report != null && report.summary.result == BuildResult.Succeeded;
        EditorUtility.DisplayDialog(success ? "Build complete" : "Build failed",
            success ? Path.Combine(OutputFolder, Executable) : "See the Console for errors.", "OK");
    }

    public static void BuildWindows()
    {
        BuildReport report = Build();
        bool success = report != null && report.summary.result == BuildResult.Succeeded;
        Debug.Log(success ? "JEWELRY_BUILD_PASS " + OutputFolder : "JEWELRY_BUILD_FAIL");
        EditorApplication.Exit(success ? 0 : 1);
    }

    public const string LauncherScene = DemoValidation.ScenesFolder + "/DemoLauncher.unity";
    public const string BuildInfoAsset = "Assets/CrimeSceneDemo/Resources/DemoBuildInfo.txt";

    [MenuItem("Crime Scene Demo/Build Latest Demo (launcher + night store)")]
    public static void BuildLatestFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildReport report = BuildLatestReport(false);
        bool success = report != null && report.summary.result == BuildResult.Succeeded;
        EditorUtility.DisplayDialog(success ? "Build complete" : "Build failed",
            success ? Path.Combine(OutputFolder, Executable) : "See the Console for errors.", "OK");
    }

    // Start-Demo.cmd: regenerate the night store from the current source (add -demoBake
    // for baked lighting, minutes longer), make the start-menu scene, stamp the build,
    // register launcher + night store (+ the October 10 shop when its scene exists) and
    // build the Windows player. Exits 0 on success, 1 on failure.
    public static void BuildLatest()
    {
        bool success = false;
        try
        {
            BuildReport report = BuildLatestReport(Environment.GetCommandLineArgs().Contains("-demoBake"));
            success = report != null && report.summary.result == BuildResult.Succeeded;
        }
        catch (Exception exception) { Debug.LogException(exception); }
        Debug.Log(success ? "JEWELRY_BUILD_PASS " + OutputFolder : "JEWELRY_BUILD_FAIL");
        EditorApplication.Exit(success ? 0 : 1);
    }

    private static BuildReport BuildLatestReport(bool bake)
    {
        NightStoreBuilder.BuildScene(bake, false);
        string night = DemoValidation.ScenesFolder + "/" + NightStoreLayout.SceneName + ".unity";
        if (!File.Exists(night)) { Debug.LogError("Night store scene was not generated: " + night); return null; }
        CreateLauncherScene();
        WriteBuildInfo(bake);
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(LauncherScene, true),
            new EditorBuildSettingsScene(night, true),
        };
        string glass = DemoValidation.ScenesFolder + "/JewelryStoreGlass.unity";
        if (File.Exists(glass)) scenes.Add(new EditorBuildSettingsScene(glass, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        return Build();
    }

    // An empty scene with a dark camera and the DemoLauncher menu.
    private static void CreateLauncherScene()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject view = new GameObject("LauncherCamera");
        Camera camera = view.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
        view.AddComponent<AudioListener>();
        new GameObject("Launcher").AddComponent<DemoLauncher>();
        Directory.CreateDirectory(DemoValidation.ScenesFolder);
        EditorSceneManager.SaveScene(scene, LauncherScene);
    }

    // Build date, source commit and lighting mode, shown at the bottom of the start menu.
    private static void WriteBuildInfo(bool bake)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(BuildInfoAsset));
        string commit = Environment.GetEnvironmentVariable("JEWELRY_COMMIT");
        if (string.IsNullOrEmpty(commit)) commit = GitCommit();
        File.WriteAllText(BuildInfoAsset, "Built " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "   ·   source " + commit
            + "   ·   lighting " + (bake ? "baked" : "preview (run Start-Demo.cmd bake for baked lighting)"));
        AssetDatabase.ImportAsset(BuildInfoAsset);
    }

    // Short hash of the repository checkout two levels above Assets, or "unknown"
    // (a GitHub ZIP download has no .git folder).
    private static string GitCommit()
    {
        try
        {
            string git = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.git"));
            string head = File.ReadAllText(Path.Combine(git, "HEAD")).Trim();
            if (!head.StartsWith("ref: ")) return head.Substring(0, Math.Min(7, head.Length));
            string reference = head.Substring(5);
            string file = Path.Combine(git, reference.Replace('/', Path.DirectorySeparatorChar));
            string hash = File.Exists(file) ? File.ReadAllText(file).Trim()
                : File.ReadAllLines(Path.Combine(git, "packed-refs")).Where(l => l.EndsWith(" " + reference)).Select(l => l.Split(' ')[0]).FirstOrDefault();
            return string.IsNullOrEmpty(hash) ? "unknown" : hash.Substring(0, Math.Min(7, hash.Length));
        }
        catch (Exception) { return "unknown"; }
    }

    private static BuildReport Build()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("No enabled scenes in Build Settings. Run a scene validation or add the scene by hand.");
            return null;
        }
        Directory.CreateDirectory(OutputFolder);
        CopyLicenses();
        return BuildPipeline.BuildPlayer(scenes, Path.Combine(OutputFolder, Executable),
            BuildTarget.StandaloneWindows64, BuildOptions.None);
    }

    // CC BY attribution must travel with the distributed demo.
    private static void CopyLicenses()
    {
        string thirdParty = Path.Combine(Application.dataPath, "CrimeSceneDemo/ThirdParty");
        if (!Directory.Exists(thirdParty)) return;
        string target = Path.Combine(OutputFolder, "ThirdPartyLicenses");
        Directory.CreateDirectory(target);
        foreach (string license in Directory.GetFiles(thirdParty, "LICENSE.txt", SearchOption.AllDirectories))
            File.Copy(license, Path.Combine(target, Path.GetFileName(Path.GetDirectoryName(license)) + ".txt"), true);
    }
}
#endif
