#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using static DemoGeometry;

public static partial class PresentationPolish
{
    static void ConsistentPropMaterials(){
        int count=0;
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){
            string path=PathOf(renderer.transform);
            if(!path.Contains("StaffRoomFurnishings")&&!path.Contains("SelectedAmbientProps")&&!path.Contains("LivedInDetails")&&!path.Contains("SelectedStoreDetails")&&!path.Contains("Office/"))continue;
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++){
                var source=materials[i];if(!source||source.shader.name!="Standard"||source.renderQueue>=2450)continue;
                string name=source.name.StartsWith("Consistent_")?source.name:"Consistent_"+source.name;
                var material=source.name==name?source:new Material(source){name=name};
                string lower=name.ToLowerInvariant();bool screen=lower.Contains("screen")||lower.Contains("indicator");
                if(screen){material.SetColor("_EmissionColor",new Color(.025f,.03f,.025f));material.SetFloat("_Glossiness",.3f);}
                else{material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);material.SetTexture("_EmissionMap",null);}
                bool dielectric=new[]{"plastic","wood","paper","cardboard","leather","food","rubber","fabric","ceramic"}.Any(lower.Contains);
                if(dielectric){material.SetTexture("_MetallicGlossMap",null);material.DisableKeyword("_METALLICGLOSSMAP");material.SetFloat("_Metallic",0);}
                material.SetFloat("_Glossiness",Mathf.Min(material.GetFloat("_Glossiness"),dielectric?.24f:.32f));
                material.SetFloat("_GlossMapScale",Mathf.Min(material.GetFloat("_GlossMapScale"),.32f));
                material.globalIlluminationFlags=screen?MaterialGlobalIlluminationFlags.None:MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                materials[i]=material==source?source:Save(material,name);EditorUtility.SetDirty(materials[i]);count++;
            }
            renderer.sharedMaterials=materials;renderer.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
        }
        // Local room captures prevent a storage-room object reflecting the brighter showroom/sky.
        var probes=Fresh("StaffRoomReflectionProbes",root);
        foreach(var item in new[]{("Storage",-5.5f,5f),("Office",0f,5.7f),("BreakRoom",5.5f,4.7f)}){
            var go=new GameObject(item.Item1+"Reflection");go.transform.SetParent(probes,false);go.transform.localPosition=new Vector3(item.Item2,1.35f,10.25f);
            var probe=go.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Baked;probe.boxProjection=true;probe.size=new Vector3(item.Item3,2.7f,3.25f);probe.resolution=128;probe.hdr=true;probe.importance=7;probe.blendDistance=.08f;probe.intensity=.7f;
        }
        Debug.Log("PROP_FINISH_REPAIR_PASS slots="+count+" local rear-room probes=3");
    }
    static void StreetSurfaceRepair(){
        foreach(var entry in new[]{(StoreKit.Asphalt,"AsphaltDry",3f,.30f),(StoreKit.Pavement,"PavementSlab",2f,.22f)}){
            var m=entry.Item1;m.color=Color.white;m.mainTexture=DemoAssetLibrary.Texture("PBRExterior",entry.Item2+"_Color.jpg");
            m.SetTexture("_BumpMap",DemoAssetLibrary.NormalMap("PBRExterior",entry.Item2+"_Normal.png"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.45f);
            DemoAssetLibrary.ApplyMetallicSmoothness(m,DemoAssetLibrary.DataMap("PBRExterior",entry.Item2+"_MetallicSmoothness.png"));
            m.SetFloat("_Glossiness",entry.Item4);m.SetFloat("_GlossMapScale",entry.Item4);m.mainTextureScale=Vector2.one/entry.Item3;EditorUtility.SetDirty(m);
        }
        foreach(var filter in root.Find("Exterior/Ground").GetComponentsInChildren<MeshFilter>()){
            if(filter.name!="Pavement"&&filter.name!="FarPavement"&&filter.name!="Street"&&filter.name!="Kerbs")continue;
            var mesh=Object.Instantiate(filter.sharedMesh);var uv=mesh.uv;var vertices=mesh.vertices;var normals=mesh.normals;
            // Metric projection fixes accumulated UV stretching from both ground extensions.
            for(int i=0;i<vertices.Length;i++){var p=vertices[i];var n=normals[i];uv[i]=Mathf.Abs(n.y)>.5f?new Vector2(p.x,p.z):Mathf.Abs(n.x)>.5f?new Vector2(p.z,p.y):new Vector2(p.x,p.y);}
            mesh.uv=uv;mesh.RecalculateTangents();filter.sharedMesh=Save(mesh,"RepairedMetric_"+filter.name);
        }
        Debug.Log("STREET_SURFACE_REPAIR_PASS dry PBR sets, restrained roughness, metric UVs");
    }
    static void NaturalTrees(){
        var old=root.Find("Exterior/Trees");if(old)old.gameObject.SetActive(false);
        var group=Fresh("LivingStreetCanopy",root.Find("Exterior"));var rng=new System.Random(819);
        var texture=DemoAssetLibrary.Texture("GeneratedTextures","ElmTwig_Designed.png");if(!texture)throw new Exception("Photographic twig missing");
        TextureBudget(texture);var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        importer.alphaIsTransparency=true;importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.4f;importer.SaveAndReimport();
        var m=new Material(Shader.Find("CrimeScene/StreetFoliage")){name="NaturalElmLeaves",mainTexture=texture};m.color=new Color(.78f,.86f,.69f);m.SetFloat("_Wind",.009f);m.SetFloat("_Cutoff",.4f);var leaves=Save(m,m.name);
        var bark=Mat(Folder,"StreetTreeBark",new Color(.29f,.24f,.18f),.1f);bark.mainTexture=DemoAssetLibrary.Texture("AmbientProps2","StreetTreeYoung_NormalTree_Bark.png");
        var positions=NightStreet.TreePositions.Concat(new[]{new Vector3(-20,0,-1.65f),new Vector3(21,0,-1.65f)}).ToArray();
        for(int tree=0;tree<positions.Length;tree++){
            var wood=new KitMesh{Lightmapped=false};var foliage=new KitMesh{Lightmapped=false};float height=4.9f+(float)rng.NextDouble()*.6f;
            wood.Lathe(bark,Vector3.zero,Quaternion.identity,new[]{.125f,.095f,.06f,.018f},new[]{0f,1.7f,3.25f,height},10,true,true,true);
            for(int branch=0;branch<11;branch++){
                float a=branch*2.39996f+tree*.6f,y=2.55f+branch*.15f;var start=new Vector3(0,y,0);float reach=1.25f+(float)rng.NextDouble()*.65f;
                var end=start+new Vector3(Mathf.Sin(a)*reach,.65f+(float)rng.NextDouble()*.5f,Mathf.Cos(a)*reach);
                var axis=end-start;wood.Lathe(bark,start,Quaternion.FromToRotation(Vector3.up,axis),new[]{.041f,.021f,.004f},new[]{0f,axis.magnitude*.6f,axis.magnitude},7,true,true,true);
                for(int twig=0;twig<3;twig++){
                    float t=.4f+twig*.27f;var origin=Vector3.Lerp(start,end,t);var tip=origin+new Vector3(Mathf.Sin(a+twig-1)*.65f,.45f,Mathf.Cos(a+twig-1)*.65f);
                    var direction=tip-origin;wood.Lathe(bark,origin,Quaternion.FromToRotation(Vector3.up,direction),new[]{.012f,.002f},new[]{0f,direction.magnitude},5,true,true,true);
                    for(int spray=0;spray<10;spray++){
                        var centre=Vector3.Lerp(origin,tip,.2f+spray*.08f)+new Vector3(((float)rng.NextDouble()-.5f)*.6f,((float)rng.NextDouble()-.5f)*.45f,((float)rng.NextDouble()-.5f)*.6f);
                        float size=.34f+(float)rng.NextDouble()*.21f;var q=Quaternion.Euler(25+(float)rng.NextDouble()*110,(float)rng.NextDouble()*360,(float)rng.NextDouble()*180);
                        var x=q*Vector3.right*size*.5f;var up=q*Vector3.up*size*.5f;foliage.Quad(leaves,q*Vector3.back,centre-x-up,centre+x-up,centre+x+up,centre-x+up,new Rect(0,0,1,1));
                    }
                }
            }
            var trunk=wood.Emit("NaturalTrunk_"+tree,group,positions[tree],Quaternion.identity,Folder,"natural_trunk_"+tree);trunk.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.On;
            var capsule=trunk.AddComponent<CapsuleCollider>();capsule.center=Vector3.up*1.3f;capsule.height=2.6f;capsule.radius=.12f;
            var canopy=foliage.Emit("NaturalCanopy_"+tree,group,positions[tree],Quaternion.identity,Folder,"natural_canopy_"+tree);canopy.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.TwoSided;
            var grate=new KitMesh{Lightmapped=false};for(float x=-.5f;x<=.5f;x+=.12f)grate.Box(StoreKit.FrameMetal,new Vector3(x,.008f,0),new Vector3(.018f,.016f,1.1f));grate.Emit("TreeGrate_"+tree,group,positions[tree],Quaternion.identity,Folder,"natural_tree_grate_"+tree);
        }
        Debug.Log("NATURAL_TREES_PASS five branched trees, small photographic sprays, subtle wind");
    }
    static void CarLightingRepair(){
        var car=root.Find("Exterior/PoliceCar");
        var old=car.Find("LightBar");if(old)old.gameObject.SetActive(false);
        foreach(var blink in root.GetComponentsInChildren<LightBarBlink>(true)){blink.Washes=Array.Empty<Renderer>();blink.enabled=false;}
        foreach(var lighting in Object.FindObjectsByType<DemoLighting>(FindObjectsInactive.Include,FindObjectsSortMode.None)){lighting.LightBar=null;EditorUtility.SetDirty(lighting);}
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))if(r.name.StartsWith("Halo_Headlight")||r.name=="Wash_Street"||r.name=="Wash_ShopFloor")r.gameObject.SetActive(false);
        foreach(var light in root.Find("Lighting").GetComponentsInChildren<Light>(true))if(light.name.StartsWith("Headlight_")){light.enabled=false;light.gameObject.SetActive(false);}
        var group=Fresh("AlignedCarLighting",car);
        foreach(float side in new[]{-1f,1f}){
            // The actual supplied FBX has its nose at car-local -Z (verified from the rear plate/lamps).
            var holder=Group(side<0?"LeftHeadlamp":"RightHeadlamp",group);holder.localPosition=new Vector3(side*.70f,.85f,-2.15f);
            // No added rectangles over the supplied car's original headlamp mesh/texture.
            // The emitter sits just ahead of the bumper to avoid self-occluding its broad spill.
            var go=new GameObject("WarmHeadlight");go.transform.SetParent(holder,false);go.transform.localPosition=Vector3.back*.24f;
            go.transform.rotation=Quaternion.LookRotation((-car.forward+Vector3.forward*.65f-Vector3.up*.12f).normalized);
            var light=go.AddComponent<Light>();light.type=LightType.Spot;light.lightmapBakeType=LightmapBakeType.Baked;light.color=new Color(1,.86f,.65f);light.intensity=.95f;light.range=14;light.spotAngle=105;light.innerSpotAngle=45;light.shadows=LightShadows.Soft;light.shadowRadius=.08f;
        }
        Debug.Log("CAR_LIGHTING_REPAIR_PASS legacy bar/halos/washes and headlights disabled; two aligned dim warm lamps; source car mesh preserved");
    }
    static void ClearShardMaterials(){
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))foreach(var m in renderer.sharedMaterials.Where(m=>m)){
            if(m.name=="GroundGlassFaces"){m.color=new Color(.92f,.97f,1,.045f);m.SetFloat("_Smoothness",.97f);}
            else if(m.name=="GroundGlassCutEdges"){m.color=new Color(.66f,.77f,.78f,.10f);m.SetFloat("_Smoothness",.91f);}
            else if(m.name=="NeutralGlassEdges"){m.color=new Color(.76f,.85f,.86f,.055f);m.SetFloat("_Smoothness",.97f);}
            else continue;EditorUtility.SetDirty(m);
        }
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>().Where(f=>f&&f.name.StartsWith("GroundGlass_")).ToArray())RecordingGlass.RefreshContact(filter.gameObject);
        Debug.Log("CLEAR_SHARDS_PASS transmissive faces 0.045, cut edges 0.10; no diffuse white");
    }
    public static void RepairsFinalizeRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;
        CarLightingRepair();ClearShardMaterials();NightStoreValidation.RepairChecks();AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
        if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();Preview();RepairPreview();EditableDelivery.Build();Debug.Log("REPAIRS_DELIVERY_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void RepairsReviewRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;
        NightStoreValidation.RepairChecks();NightStoreValidation.PresentationChecks();NightStoreValidation.CameraGripRender();NightStoreValidation.GlassRenderCheck();RepairPreview();EditableDelivery.Build();Debug.Log("REPAIR_REVIEW_DELIVERY_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void HandAndPreviewFixRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");
        int removed=VRVisualValidation.PruneCameraPreviewTorches();VRVisualValidation.CheckPreviewTorchOwnership();NightStoreValidation.CameraGripRender();NightStoreValidation.RepairChecks();
        EditorSceneManager.SaveOpenScenes();if(removed>0)ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();
        EditableDelivery.Build();
        if(VRVisualValidation.PruneCameraPreviewTorches()!=0)throw new Exception("Build validation leaked torch models");
        Debug.Log("CAMERA_HAND_AND_PREVIEW_FIX_PASS saved scene cleaned, normal authored camera grip, strict Windows build");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void RepairsRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;StoreKit.Palette();
        ConsistentPropMaterials();StreetSurfaceRepair();NaturalTrees();CarLightingRepair();ClearShardMaterials();Physics.SyncTransforms();
        NightStoreValidation.RepairChecks();AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
        if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();Preview();RepairPreview();
        EditableDelivery.Build();Debug.Log("REPAIRS_DELIVERY_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void RepairPreview(){
        var go=new GameObject("Repair review camera");var camera=go.AddComponent<Camera>();NightQuality.Configure(camera);camera.stereoTargetEye=StereoTargetEyeMask.None;
        var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;var previous=RenderTexture.active;var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);var torch=lighting.FlashlightLight;
        var views=new[]{(new Vector3(0,1.5f,10.8f),new Vector3(-1.9f,.8f,9.3f)),(new Vector3(4,1.5f,9),new Vector3(7,1,10.7f)),(new Vector3(-4.3f,1.5f,9.5f),new Vector3(-6.8f,1,11.2f)),(new Vector3(-1,.48f,-1.5f),new Vector3(0,.03f,-.65f)),(new Vector3(9,1.4f,-1.4f),new Vector3(6,.6f,-4.65f)),(new Vector3(0,1.65f,-10),new Vector3(2,3.9f,-15.2f)),(new Vector3(0,.22f,-1.15f),new Vector3(0,.035f,-.65f)),(new Vector3(.4f,1.1f,-3.7f),new Vector3(3,.5f,-4.65f))};
        try{for(int i=0;i<views.Length;i++){camera.transform.position=views[i].Item1;camera.transform.LookAt(views[i].Item2);torch.enabled=i<4||i==6;torch.transform.SetPositionAndRotation(camera.transform.position+camera.transform.right*.06f,camera.transform.rotation);camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Builds/repair-"+i+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }}finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
    }
}
#endif
