#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using static DemoGeometry;

public static partial class PresentationPolish {
    const string RawCarPath="Assets/CrimeSceneDemo/ThirdParty/SuppliedPoliceCar/police+car+3d+model.fbx";
    static string CarGeometrySignature(){
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(RawCarPath);var faces=new List<string>();
        foreach(var f in asset.GetComponentsInChildren<MeshFilter>(true)){
            var v=f.sharedMesh.vertices;var t=f.sharedMesh.triangles;
            string Key(int i){var p=f.transform.TransformPoint(v[i]);return $"{Mathf.RoundToInt(p.x*100000)},{Mathf.RoundToInt(p.y*100000)},{Mathf.RoundToInt(p.z*100000)}";}
            for(int i=0;i<t.Length;i+=3)faces.Add(string.Join("/",new[]{Key(t[i]),Key(t[i+1]),Key(t[i+2])}.OrderBy(k=>k)));
        }
        using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n",faces.OrderBy(f=>f))))).Replace("-","");
    }
    static void ApplyRawCarMaterialOnly(Transform model){
        if(IsRefinedPatrol(model))return;
        if(model.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.sharedMaterials.Any(m=>m&&m.shader.name=="CrimeScene/OmanPoliceCar"))){ApplyPoliceLivery(model);return;}
        var m=Mat(Folder,"SuppliedCarUntexturedClay",new Color(.42f,.43f,.44f),0);
        m.shader=Shader.Find("CrimeScene/RawCarClay");if(!m.shader)throw new Exception("Raw car shader missing");m.SetFloat("_DebugGeometryNormals",0);
        foreach(string p in m.GetTexturePropertyNames())m.SetTexture(p,null);
        foreach(string keyword in m.shaderKeywords)m.DisableKeyword(keyword);
        m.SetFloat("_Metallic",0);m.SetFloat("_Glossiness",0);m.SetColor("_EmissionColor",Color.black);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        foreach(var r in model.GetComponentsInChildren<MeshRenderer>(true)){r.sharedMaterials=Enumerable.Repeat(m,r.sharedMaterials.Length).ToArray();r.receiveGI=ReceiveGI.LightProbes;r.lightmapIndex=-1;r.lightProbeUsage=LightProbeUsage.BlendProbes;}
        EditorUtility.SetDirty(m);
    }
    static void RawCarImport(){
        string before=CarGeometrySignature();
        var model=root.Find("Exterior/PoliceCar/SuppliedModel");ApplyRawCarMaterialOnly(model);
        foreach(var r in root.Find("Exterior/PoliceCar").GetComponentsInChildren<Renderer>(true))if(!r.transform.IsChildOf(model))r.enabled=false;
        if(CarGeometrySignature()!=before)throw new Exception("Car surface change altered triangle geometry");
        Debug.Log("RAW_CAR_GEOMETRY_PRESERVED "+before);
    }
    static void RawCarChecks(){
        if(IsRefinedPatrol(root.Find("Exterior/PoliceCar/SuppliedModel"))){PatrolCarChecks();return;}
        var model=root.Find("Exterior/PoliceCar/SuppliedModel");int total=0;
        foreach(var f in model.GetComponentsInChildren<MeshFilter>()){
            if(AssetDatabase.GetAssetPath(f.sharedMesh)!=RawCarPath)throw new Exception("Car no longer uses the supplied FBX directly");
            total+=f.sharedMesh.triangles.Length/3;
        }
        FlatCarNormalsRenderCheck();
        foreach(var r in model.GetComponentsInChildren<MeshRenderer>())foreach(var m in r.sharedMaterials){
            bool clay=m&&m.shader.name=="CrimeScene/RawCarClay"&&!m.GetTexturePropertyNames().Any(p=>m.GetTexture(p))&&m.GetFloat("_Metallic")==0;
            bool paint=m&&m.shader.name=="CrimeScene/OmanPoliceCar"&&AssetDatabase.GetAssetPath(m.mainTexture)==LiveryFolder+"/PoliceAuthoredPaint.png"&&m.GetTexture("_SurfaceMap");
            if((!clay&&!paint)||m.GetFloat("_DebugGeometryNormals")!=0||m.IsKeywordEnabled("_EMISSION"))throw new Exception("Car must use original face shading with plain clay or approved authored livery");
        }
        using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(RawCarPath))).Replace("-","")!="BEF2445C622AC69BAFB734D90BE5F7173EC9046F03790B48DD18D6761CE1445A")throw new Exception("Supplied FBX bytes changed");
        Debug.Log($"RAW_CAR_VALIDATION_PASS rendered face shading for {total} triangles; original FBX bytes/triangle geometry, uniform placement and collision retained; plain clay or authored paint only");
    }
    static void GeneralSceneReview(){
        int renderers=0,slots=0,missing=0,badMeshes=0,zeroNormals=0,duplicateObjects=0;
        var report=new List<string>();var visible=new Dictionary<string,string>();
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){
            if(!r.enabled)continue;renderers++;string path=PathOf(r.transform);
            foreach(var m in r.sharedMaterials){slots++;if(!m||!m.shader||!m.shader.isSupported||m.shader.name=="Hidden/InternalErrorShader"){missing++;report.Add("MISSING MATERIAL/SHADER "+path);}}
            var f=r.GetComponent<MeshFilter>();if(!f){if(!r.GetComponent<TextMesh>()){badMeshes++;report.Add("MISSING FILTER "+path);}continue;}
            var mesh=f.sharedMesh;if(!mesh){badMeshes++;report.Add("MISSING MESH "+path);continue;}
            if(r.sharedMaterials.Any(m=>m&&m.shader.name=="Standard")){
                int bad=mesh.normals.Count(n=>n.sqrMagnitude<.01f);if(bad>0){zeroNormals+=bad;report.Add("ZERO LIT NORMALS "+path+" "+bad);}
            }
            // Exact renderer copies only; intentional alternate LODs and decorative overlays stay separate.
            if(path.Contains("/LOD1/")||path.Contains("/LOD2/"))continue;
            var matrix=f.transform.localToWorldMatrix;string key=AssetDatabase.GetAssetPath(mesh)+"#"+mesh.name+"#"+matrix.ToString("F5")+"#"+string.Join(",",r.sharedMaterials.Select(m=>m?AssetDatabase.GetAssetPath(m):"missing"));
            if(visible.TryGetValue(key,out var other)){duplicateObjects++;report.Add("DUPLICATE RENDERER "+path+" == "+other);}else visible[key]=path;
        }
        foreach(var t in root.GetComponentsInChildren<Transform>()){
            int count=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);if(count>0){missing+=count;report.Add("MISSING SCRIPT "+PathOf(t));}
        }
        report.Insert(0,$"Visible renderers={renderers}; material slots={slots}; missing references/shaders/scripts={missing}; missing meshes={badMeshes}; zero lit normals={zeroNormals}; exact duplicate renderer candidates={duplicateObjects}");
        Directory.CreateDirectory("Builds");File.WriteAllLines("Builds/current-scene-review.txt",report);
        if(missing>0||badMeshes>0)throw new Exception("Scene reference/mesh review failed; see current-scene-review.txt");
        Debug.Log("GENERAL_SCENE_REVIEW_PASS "+report[0]);
    }
    static void FlatCarNormalsRenderCheck(string shaderName="CrimeScene/RawCarClay"){
        var stage=new GameObject("Temporary flat car shading check");stage.transform.position=Vector3.one*1000;
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.SetParent(stage.transform,false);cube.layer=31;
        var mesh=Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);mesh.normals=mesh.vertices.Select(v=>v.normalized).ToArray();cube.GetComponent<MeshFilter>().sharedMesh=mesh;
        var material=new Material(Shader.Find(shaderName));material.SetFloat("_DebugGeometryNormals",1);cube.GetComponent<Renderer>().sharedMaterial=material;
        var go=new GameObject("Verification camera");go.transform.SetParent(stage.transform,false);var camera=go.AddComponent<Camera>();camera.transform.localPosition=new Vector3(3,2.5f,-4);camera.transform.LookAt(stage.transform.position);camera.orthographic=true;camera.orthographicSize=1;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.stereoTargetEye=StereoTargetEyeMask.None;camera.renderingPath=RenderingPath.Forward;
        var target=new RenderTexture(256,256,24);camera.targetTexture=target;var previous=RenderTexture.active;bool fog=RenderSettings.fog;Texture2D pixels=null;
        try{
            RenderSettings.fog=false;camera.Render();RenderTexture.active=target;pixels=new Texture2D(256,256,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,256,256),0,0);pixels.Apply();
            Color Sample(Vector3 p){var screen=camera.WorldToScreenPoint(stage.transform.position+p);return pixels.GetPixel(Mathf.RoundToInt(screen.x),Mathf.RoundToInt(screen.y));}
            float Difference(Color a,Color b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b));
            var front=Sample(new Vector3(-.15f,-.15f,-.5f));var frontOther=Sample(new Vector3(.15f,.15f,-.5f));
            var top=Sample(new Vector3(.15f,.5f,.15f));var topOther=Sample(new Vector3(-.15f,.5f,-.15f));
            if(Difference(front,frontOther)>.025f||Difference(top,topOther)>.025f||Difference(front,top)<.15f||front.maxColorComponent<.2f)throw new Exception($"Flat car GPU shading failed: front {front}/{frontOther}, top {top}/{topOther}");
            File.WriteAllBytes("Builds/raw-car-normal-check.png",pixels.EncodeToPNG());Debug.Log("RAW_CAR_GPU_NORMAL_PASS smoothed input normals produce constant distinct face shading");
        }finally{RenderSettings.fog=fog;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);if(pixels)Object.DestroyImmediate(pixels);Object.DestroyImmediate(material);Object.DestroyImmediate(mesh);Object.DestroyImmediate(stage);}
    }
    static void RawCarPreview(string suffix){
        var go=new GameObject("Car material review");var camera=go.AddComponent<Camera>();NightQuality.Configure(camera);camera.stereoTargetEye=StereoTargetEyeMask.None;camera.fieldOfView=55;camera.farClipPlane=220;
        var target=new RenderTexture(1440,900,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);var torch=lighting.FlashlightLight;
        var views=new[]{(new Vector3(8.7f,1.5f,-7.6f),new Vector3(5,.7f,-4.65f)),(new Vector3(.7f,1.5f,-7.5f),new Vector3(5,.7f,-4.65f)),(new Vector3(5.1f,1.8f,-9),new Vector3(5,.6f,-4.65f))};
        try{for(int i=0;i<views.Length;i++){
            camera.transform.position=views[i].Item1;camera.transform.LookAt(views[i].Item2);torch.transform.SetPositionAndRotation(camera.transform.position+camera.transform.right*.12f,camera.transform.rotation);camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes("Builds/car-"+suffix+"-"+i+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }}finally{RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
    }
    public static void CurrentSceneAuditRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;
        GeneralSceneReview();RawCarPreview("before");RepairPreview();RefinementPreview();Debug.Log("CURRENT_SCENE_AUDIT_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void RawCarReviewRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;
        var middle=root.Find("Dressing/BespokeShowcaseInserts/WatchRollTravelCase");var matrix=middle.localToWorldMatrix;
        RawCarImport();RawCarPreview("plain-check");RawCarChecks();GeneralSceneReview();ShopReviewChecks();RefinementChecks();NightStoreValidation.RepairChecks();NightStoreValidation.PresentationChecks();
        if(middle.localToWorldMatrix!=matrix)throw new Exception("Middle display was changed");if(TriangleAudit(false)!=0)throw new Exception("Shop has duplicate faces");
        AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);
        ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();RawCarPreview("plain");EditableDelivery.Build();Debug.Log("RAW_CAR_REVIEW_DELIVERY_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
