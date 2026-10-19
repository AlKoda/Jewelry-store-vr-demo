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
    // The start menu's music, in the Music pack (NightAudioBuilder.MusicPack).
    public const string MenuTheme = "MenuTheme.ogg";
    // The project's third-party packs on disk (DemoAssetLibrary.Root as a full path).
    public static string ThirdPartyFolder => Path.Combine(Application.dataPath, "CrimeSceneDemo/ThirdParty");

    [MenuItem("Crime Scene Demo/Build Latest Demo (launcher + night store)")]
    public static void BuildLatestFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildReport report = BuildLatestReport(false);
        bool success = report != null && report.summary.result == BuildResult.Succeeded;
        EditorUtility.DisplayDialog(success ? "Build complete" : "Build failed",
            success ? Path.Combine(OutputFolder, Executable) : "See the Console for errors.", "OK");
    }

    // Play-Keyboard.cmd: regenerate the night store from the current source (add -demoBake
    // for baked lighting, minutes longer; a failed bake builds with the realtime preview
    // and logs JEWELRY_BUILD_LIGHTING), make the start-menu scene, stamp the build,
    // register launcher + night store (+ the October 10 shop when its scene exists) and
    // build the Windows player. Exits 0 on success, 1 on failure. The credits the player
    // shows (Resources/DemoCredits) are written by NightStoreBuilder.Build, so they list
    // what this night store places.
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
        bool baked = NightStoreBuilder.BuildScene(bake, false);
        string night = DemoValidation.ScenesFolder + "/" + NightStoreLayout.SceneName + ".unity";
        if (!File.Exists(night)) { Debug.LogError("Night store scene was not generated: " + night); return null; }
        CreateLauncherScene();
        WriteBuildInfo(bake, baked);
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

    // An empty scene with a dark camera, the DemoLauncher menu and, when the Music pack is
    // present, the menu theme: a DemoMusic bed faded in over 2.5 s at start and out before a
    // map loads (DemoLauncher.Open). Without the pack the menu is silent.
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
        AddMenuMusic();
        DemoToolsBuilder.AddTheme(new GameObject("UiTheme"));
        Directory.CreateDirectory(DemoValidation.ScenesFolder);
        EditorSceneManager.SaveScene(scene, LauncherScene);
    }

    // The launcher's MenuMusic object in the active scene, as CreateLauncherScene saves it
    // (the menu-theme check builds one too), or null without the Music pack.
    public static DemoMusic AddMenuMusic()
    {
        AudioClip theme = MenuThemeClip();
        if (theme == null) return null;
        DemoMusic music = new GameObject("MenuMusic").AddComponent<DemoMusic>();
        music.Bed = theme;
        music.PlayBedAtStart = true;
        music.BedVolume = 0.25f;
        music.FadeSeconds = 2.5f;
        return music;
    }

    // The menu theme as an asset (PackImport imports it as a streaming Vorbis bed), or null
    // in a project without the Music pack.
    public static AudioClip MenuThemeClip() => DemoAssetLibrary.Audio(NightAudioBuilder.MusicPack, MenuTheme);

    // Build date, source commit and lighting mode, shown at the bottom of the start menu.
    // The lighting is what the scene really ships: a bake that failed reads as preview.
    private static void WriteBuildInfo(bool bake, bool baked)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(BuildInfoAsset));
        string commit = Environment.GetEnvironmentVariable("JEWELRY_COMMIT");
        if (string.IsNullOrEmpty(commit) || commit == "unknown") commit = GitCommit();
        string lighting = baked ? "baked" : bake ? "preview (the bake failed, see build-latest.log)" : "preview (run Play-Keyboard.cmd bake for baked lighting)";
        File.WriteAllText(BuildInfoAsset, "Built " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "   ·   source " + commit + "   ·   lighting " + lighting);
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

    // CC BY attribution must travel with the distributed demo: every notice LicenseFiles
    // lists goes to ThirdPartyLicenses beside the executable. Notices an earlier build left
    // there that no longer match a pack are removed, so the folder lists only what ships.
    private static void CopyLicenses()
    {
        string thirdParty = ThirdPartyFolder;
        if (!Directory.Exists(thirdParty)) return;
        string target = Path.Combine(OutputFolder, "ThirdPartyLicenses");
        Directory.CreateDirectory(target);
        HashSet<string> copied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> notice in LicenseFiles(thirdParty))
        {
            File.Copy(notice.Key, Path.Combine(target, notice.Value), true);
            copied.Add(notice.Value);
        }
        foreach (string stale in Directory.GetFiles(target, "*.txt"))
            if (!copied.Contains(Path.GetFileName(stale))) File.Delete(stale);
    }

    // The licence notices a build ships, as (source file, file name in ThirdPartyLicenses),
    // sorted by that name. A pack is a folder directly under thirdParty:
    // - its LICENSE.txt -> <pack>.txt;
    // - every .txt in a Licenses folder of the pack (NightSoundscape/Licenses) -> <pack>_<file>;
    // - a LICENSE.txt deeper in the pack (NightAudio/Originals/License.txt, the author's own
    //   notice) -> <pack>_<folders>_<file>.
    // File names match case-insensitively on every platform (the Windows file system does),
    // and folders Unity ignores (a trailing ~, a leading dot) are skipped. Two notices that
    // would share a name keep the first and log a warning.
    public static List<KeyValuePair<string, string>> LicenseFiles(string thirdParty)
    {
        SortedDictionary<string, string> byName = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(thirdParty) || !Directory.Exists(thirdParty)) return new List<KeyValuePair<string, string>>();
        char[] separators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        string[] packs = Directory.GetDirectories(thirdParty);
        Array.Sort(packs, StringComparer.Ordinal);
        foreach (string packFolder in packs)
        {
            string pack = Path.GetFileName(packFolder);
            if (IgnoredFolder(pack)) continue;
            string root = Path.GetFullPath(packFolder);
            string[] files = Directory.GetFiles(root, "*.txt", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);
            foreach (string file in files)
            {
                // "*.txt" also matches longer extensions on Windows (".txt~").
                if (!string.Equals(Path.GetExtension(file), ".txt", StringComparison.OrdinalIgnoreCase)) continue;
                string[] parts = Path.GetFullPath(file).Substring(root.Length).Split(separators, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0 || parts.Take(parts.Length - 1).Any(IgnoredFolder)) continue;
                string name = parts[parts.Length - 1], published = null;
                if (parts.Length >= 2 && string.Equals(parts[parts.Length - 2], "Licenses", StringComparison.OrdinalIgnoreCase))
                    published = pack + "_" + name;
                else if (string.Equals(name, "LICENSE.txt", StringComparison.OrdinalIgnoreCase))
                    published = parts.Length == 1 ? pack + ".txt" : pack + "_" + string.Join("_", parts);
                if (published == null) continue;
                if (byName.TryGetValue(published, out string first))
                {
                    Debug.LogWarning("Licence notice " + file + " would overwrite " + first + " as " + published + "; not copied.");
                    continue;
                }
                byName[published] = file;
            }
        }
        return byName.Select(entry => new KeyValuePair<string, string>(entry.Value, entry.Key)).ToList();
    }

    private static bool IgnoredFolder(string name) => name.EndsWith("~", StringComparison.Ordinal) || name.StartsWith(".", StringComparison.Ordinal);
}
#endif
