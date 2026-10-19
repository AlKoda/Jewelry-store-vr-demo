#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class EditableDelivery
{
    [MenuItem("Crime Scene/Delivery/Build current night scene without regeneration")]
    public static void Build(){
        // Avoid unloading shader objects between XR startup and the store scene.
        // A captured player crash occurred in ShaderVariantMonitor::FrameFinished.
        var playerSettings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var keepShaders=playerSettings.FindProperty("keepLoadedShadersAlive");
        if(keepShaders==null)throw new Exception("Shader lifetime setting unavailable");
        keepShaders.boolValue=true;playerSettings.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
        string scene=DemoValidation.ScenesFolder+"/JewelryStoreNight.unity";
        if(!File.Exists(scene))throw new Exception("Night scene missing; generate it once with CombinedBuild.Run.");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/Windows/JewelryStoreDemo.exe"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));DemoBuild.CopyLicenses(Path.GetDirectoryName(output));
        VRVisualValidation.Check();
        var report=BuildPipeline.BuildPlayer(new[]{DemoBuild.LauncherScene,scene},output,BuildTarget.StandaloneWindows64,BuildOptions.StrictMode);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Existing scene player build failed.");
        Debug.Log("EXISTING_SCENE_DELIVERY_PASS "+output);
    }
    public static void Run(){try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
