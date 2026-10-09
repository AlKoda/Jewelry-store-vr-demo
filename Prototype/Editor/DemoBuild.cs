#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
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
