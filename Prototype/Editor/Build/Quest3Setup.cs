#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
public static class Quest3Setup
{
    public static void Run(){try{
        Directory.CreateDirectory("Assets/XR/Settings");AssetDatabase.Refresh();
        XRGeneralSettingsPerBuildTarget per;
        if(!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out per)){
            per=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(per,"Assets/XR/Settings/XRGeneralSettings.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,per,true);
        }
        var group=BuildTargetGroup.Standalone;
        if(!per.HasSettingsForBuildTarget(group))per.CreateDefaultSettingsForBuildTarget(group);
        if(!per.HasManagerSettingsForBuildTarget(group))per.CreateDefaultManagerSettingsForBuildTarget(group);
        var settings=per.SettingsForBuildTarget(group);settings.InitManagerOnStart=false;
        if(!XRPackageMetadataStore.AssignLoader(settings.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",group))throw new Exception("OpenXR loader assignment failed");
        // 1.16 package uses an internal container; call its published factory through
        // reflection here only, keeping runtime scripts independent of editor internals.
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.XR.OpenXR.OpenXRPackageSettings")).First(t=>t!=null);
        type.GetMethod("GetOrCreateInstance",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
        UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(group);
        var xr=OpenXRSettings.GetSettingsForBuildTargetGroup(group);if(!xr)throw new Exception("OpenXR settings missing");
        xr.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;
        foreach(var feature in xr.GetFeatures<OpenXRFeature>()){
            string name=feature.GetType().Name;
            if(feature is OpenXRInteractionFeature)feature.enabled=name=="OculusTouchControllerProfile";
            EditorUtility.SetDirty(feature);
        }
        var player=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        player.FindProperty("activeInputHandler").intValue=2;player.ApplyModifiedPropertiesWithoutUndo();
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});
        EditorUtility.SetDirty(settings);EditorUtility.SetDirty(settings.Manager);EditorUtility.SetDirty(per);EditorUtility.SetDirty(xr);AssetDatabase.SaveAssets();
        Debug.Log("QUEST3_SETUP_PASS Windows OpenXR, Oculus Touch profile, SPI, DX11, Both input backends");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
