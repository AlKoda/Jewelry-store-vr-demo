#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class ReviewPlayerBuild {
 public static void Run(){try{string output=Environment.GetEnvironmentVariable("JEWELRY_COMBINED_OUTPUT");if(string.IsNullOrEmpty(output))output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/Review20261017/JewelryStoreDemo.exe"));var result=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),output,BuildTarget.StandaloneWindows64,BuildOptions.StrictMode);if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Player rebuild failed");Debug.Log("REVIEW_PLAYER_BUILD_PASS");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
