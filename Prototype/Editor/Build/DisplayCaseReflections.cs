#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public static class DisplayCaseReflections
{
    public const string PreviewName = "DisplayCasePreviewReflection";

    public static void ConfigureDesktop()
    {
        foreach (GraphicsTier tier in Enum.GetValues(typeof(GraphicsTier)))
        {
            var settings = EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Standalone, tier);
            settings.reflectionProbeBoxProjection = true;
            settings.reflectionProbeBlending = true;
            EditorGraphicsSettings.SetTierSettings(BuildTargetGroup.Standalone, tier, settings);
        }
    }

    public static void RemovePreview(Transform root)
    {
        var old = root.Find(PreviewName);
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
    }
    // Unity's probe baker filters roughness mips and writes HDR decode metadata.
    // A raw camera cubemap is not a substitute for this on Standard-specular glass.
    public static ReflectionProbe Bake(string name,Vector3 position,Bounds bounds,string folder,int resolution=256)
    {
        ConfigureDesktop();
        DemoGeometry.EnsureFolder(folder);
        var probe=new GameObject(name).AddComponent<ReflectionProbe>();
        probe.transform.position=position;probe.center=bounds.center-position;probe.size=bounds.size;
        probe.boxProjection=true;probe.mode=ReflectionProbeMode.Baked;probe.resolution=resolution;
        probe.hdr=true;probe.nearClipPlane=.03f;probe.farClipPlane=50;probe.importance=5;probe.blendDistance=.25f;
        probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=RenderSettings.ambientEquatorColor;
        var flags=new Dictionary<GameObject,StaticEditorFlags>();
        var disabled=new List<Renderer>();
        try
        {
            foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                bool transparent=false;
                foreach(var m in r.sharedMaterials) transparent|=m!=null&&m.renderQueue>=3000;
                if(transparent){if(r.enabled){r.enabled=false;disabled.Add(r);}continue;}
                flags[r.gameObject]=GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                GameObjectUtility.SetStaticEditorFlags(r.gameObject,flags[r.gameObject]|StaticEditorFlags.ReflectionProbeStatic);
            }
            string path=folder+"/"+name+".exr";
            if(!Lightmapping.BakeReflectionProbe(probe,path)) throw new Exception("Reflection bake failed: "+name);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var cube=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if(cube==null) throw new Exception("Reflection cubemap failed to import: "+path);
            probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=cube;
            return probe;
        }
        catch
        {
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            throw;
        }
        finally
        {
            foreach(var r in disabled) if(r!=null)r.enabled=true;
            foreach(var pair in flags) if(pair.Key!=null)GameObjectUtility.SetStaticEditorFlags(pair.Key,pair.Value);
        }
    }

    public static void PreviewInterior(Transform root)
    {
        RemovePreview(root);
        var probe=Bake(PreviewName,new Vector3(0,1.45f,4.8f),
            new Bounds(new Vector3(0,1.5f,4.4f),new Vector3(15.5f,3,7.5f)),DisplayCaseFinish.Folder);
        probe.transform.SetParent(root,true);
    }
}
#endif
