#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
public static class ReflectionCapture
{
    public static void Refresh()
    {
        VRVisualValidation.PruneCameraPreviewTorches();
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new Exception("Reflection capture requires an enabled graphics device; omit -nographics.");
        const string folder="Assets/CrimeSceneDemo/CombinedArt/Reflections";
        Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var probes=UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
        var intensities=probes.Select(p=>p.intensity).ToArray();float ambient=RenderSettings.reflectionIntensity;
        foreach(var p in probes)p.intensity=0;RenderSettings.reflectionIntensity=0;
        try{
            foreach(var p in probes){
                string path=folder+"/"+p.name+".exr";
                if(!Lightmapping.BakeReflectionProbe(p,path))throw new Exception("Reflection capture failed: "+p.name);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                p.bakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(path);p.customBakedTexture=p.bakedTexture;p.mode=ReflectionProbeMode.Custom;
                if(!p.bakedTexture)throw new Exception("Reflection cubemap missing: "+p.name);
                EditorUtility.SetDirty(p);
            }
        }finally{for(int i=0;i<probes.Length;i++)probes[i].intensity=intensities[i];RenderSettings.reflectionIntensity=ambient;}
        // Second pass with the probes back on: metals and glass seen in a cubemap now show the first
        // pass's reflections instead of black (one bounce, no feedback since the first pass was clean).
        foreach(var p in probes){
            string path=folder+"/"+p.name+".exr";
            if(!Lightmapping.BakeReflectionProbe(p,path))throw new Exception("Reflection bounce capture failed: "+p.name);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            p.bakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(path);p.customBakedTexture=p.bakedTexture;EditorUtility.SetDirty(p);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();Debug.Log("CLEAN_REFLECTION_CAPTURE_PASS probes="+probes.Length);
    }
    public static void RebuildSaved(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");Refresh();ReviewPlayerBuild.Run();
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif

