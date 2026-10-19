#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;

// Repeatable, actual Unity renders of the same production case builders. The
// lighting room is isolated from the night scene; no exterior assets are edited.
public static class DisplayCaseReview
{
    const string Key="DisplayCaseReview";
    static readonly string Output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Docs/VerificationDisplayCases"));

    [MenuItem("Crime Scene Demo/Review Display Cases")]
    public static void Run()
    {
        try
        {
            NightLighting.UseLinearColorSpace();
            ShaderUtil.allowAsyncCompilation=false;
            DemoValidation.Begin("VerificationDisplayCases");
            DemoValidation.WriteHeader("Display case material review; actual Unity renders, controlled interior lighting.");
            QualitySettings.antiAliasing=8;
            QualitySettings.pixelLightCount=8;
            QualitySettings.shadows=ShadowQuality.All;
            QualitySettings.shadowResolution=ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance=20;
            foreach(GraphicsTier tier in Enum.GetValues(typeof(GraphicsTier)))
            {
                var settings=EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Standalone,tier);
                settings.reflectionProbeBoxProjection=true;
                settings.reflectionProbeBlending=true;
                EditorGraphicsSettings.SetTierSettings(BuildTargetGroup.Standalone,tier,settings);
            }
            StoreKit.Palette(); DisplayCaseFinish.Prepare();
            Transform room=Group("MaterialReviewRoom",null);
            Material floor=Mat(DisplayCaseFinish.Folder,"ReviewFloor",new Color(.32f,.34f,.35f),.22f);
            Material wall=Mat(DisplayCaseFinish.Folder,"ReviewWall",new Color(.60f,.58f,.54f),.08f);
            Box("Floor",room,V(0,-.06f,0),V(12,.12f,12),floor);
            Box("RearWall",room,V(0,2.0f,3),V(12,4,.12f),wall);
            Box("LeftWall",room,V(-4,2,0),V(.12f,4,8),wall);
            RenderSettings.skybox=null;
            RenderSettings.fog=false;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.50f,.53f,.58f);
            RenderSettings.ambientEquatorColor=new Color(.30f,.31f,.33f);
            RenderSettings.ambientGroundColor=new Color(.14f,.13f,.12f);
            Light key=Light("Key",V(-2,3,-2),V(0,.6f,0),1.1f,new Color(1,.95f,.88f));
            key.type=LightType.Directional;key.shadows=LightShadows.Soft;key.shadowBias=.015f;key.shadowNormalBias=.015f;
            Light("Fill",V(2,2,-1),V(0,.8f,0),0.8f,new Color(.81f,.89f,1));
            Light("Rim",V(0,2.8f,2),V(0,.7f,0),1.2f,new Color(1,.94f,.85f));
            Material luminous=Mat(DisplayCaseFinish.Folder,"ReviewSoftbox",Color.white,.5f);
            luminous.EnableKeyword("_EMISSION");luminous.SetColor("_EmissionColor",Color.white*3);
            Box("OverheadSoftbox",room,V(-.3f,2.7f,1.1f),V(2.6f,.03f,1.1f),luminous);
            Box("NarrowSoftbox",room,V(1.8f,2.4f,-.2f),V(.15f,1.5f,.03f),luminous);
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>()) if(r.name.Contains("Softbox") || r.name=="LeftWall") r.shadowCastingMode=ShadowCastingMode.Off;
            var ambient=new SphericalHarmonicsL2(); ambient.AddAmbientLight(new Color(.22f,.23f,.25f)); RenderSettings.ambientProbe=ambient;
            Box("FrontReflectionCard",room,V(-2.6f,2.0f,-3.8f),V(3.2f,1.5f,.02f),luminous).gameObject.layer=30;
            GameObject intact=StoreKit.DisplayCase(null);
            Dress(intact.transform);
            Camera camera=new GameObject("ReviewCamera").AddComponent<Camera>();
            camera.nearClipPlane=.02f;camera.farClipPlane=30;camera.fieldOfView=37;
            camera.cullingMask=~(1<<30); camera.allowHDR=true;camera.allowMSAA=true;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.28f,.30f,.32f);
            camera.gameObject.AddComponent<DisplayReviewOrbit>();
            camera.gameObject.AddComponent<AudioListener>();
            BakeReflection();
            int frames=0;
            EditorApplication.CallbackFunction ready=null;
            ready=()=>
            {
                EditorApplication.QueuePlayerLoopUpdate();
                if(++frames<20) return;
                EditorApplication.update-=ready;
                try
                {
            Validate(intact);
            Capture(camera,"01-display-case.png",V(2.3f,1.65f,-2.85f),V(0,.58f,0));
            Capture(camera,"02-glass-closeup.png",V(1.15f,1.36f,-1.25f),V(.08f,.88f,0));
            Capture(camera,"03-glass-grazing.png",V(1.65f,1.10f,-.65f),V(0,.91f,0));
            DemoValidation.SaveScene("DisplayCaseMaterialReview",false);
            UnityEngine.Object.DestroyImmediate(intact);
            GameObject broken=StoreKit.DisplayCase(null,StoreKit.CaseGlass.FrontBroken);
            Dress(broken.transform);
            Capture(camera,"04-broken-glass.png",V(1.35f,1.38f,-1.75f),V(0,.74f,0));
            DemoValidation.Check(broken.transform.Find("Glass_Front")==null,"Broken front leaves the opening accessible");
            DemoValidation.Check(broken.transform.Find("Shards").GetComponent<MeshRenderer>().sharedMaterials.Length==2,"Fragments have clear faces and separate cut-edge material");
            UnityEngine.Object.DestroyImmediate(broken);
            GameObject wallCase=StoreKit.WallCase(null);
            Dress(wallCase.transform);
            Capture(camera,"05-wall-case.png",V(2.3f,1.65f,-2.85f),V(0,.58f,0));
            UnityEngine.Object.DestroyImmediate(wallCase);
            Transform table=UserPropDressing.Table(null);
            UserPropDressing.Validate(table);
            Capture(camera,"06-user-props.png",V(2.0f,1.8f,-2.7f),V(0,.55f,0));
            DemoValidation.SaveScene("UserPropsReview",false);
            DemoValidation.Check(GameObject.Find("camera")!=null && GameObject.Find("gloves")!=null,"Prepared user props instantiate in Unity");
            UnityEngine.Object.DestroyImmediate(table.gameObject);
            UserPropDressing.Place("sofa",null,Vector3.zero,180);
            Capture(camera,"07-user-sofa.png",V(2.3f,1.65f,-2.85f),V(0,.5f,0));
            DemoValidation.Finish(Key,true,null);
                }
                catch(Exception ex){DemoValidation.Finish(Key,false,ex.ToString());}
            };
            EditorApplication.update+=ready;
        }
        catch(Exception ex){DemoValidation.Finish(Key,false,ex.ToString());}
    }

    static Light Light(string name,Vector3 position,Vector3 target,float intensity,Color color)
    {
        var light=new GameObject(name).AddComponent<Light>();
        light.type=LightType.Spot;light.transform.position=position;light.transform.LookAt(target);
        light.spotAngle=95;light.range=8;light.intensity=intensity;light.color=color;
        light.renderMode=LightRenderMode.ForcePixel;
        return light;
    }

    static void Dress(Transform parent)
    {
        StoreKit.NecklaceBust(parent).transform.localPosition=V(0,.711f,.015f);
        StoreKit.RingTray(parent,true).transform.localPosition=V(-.51f,.711f,-.04f);
        StoreKit.BraceletStand(parent).transform.localPosition=V(.49f,.711f,.01f);
        StoreKit.JewelryBox(parent).transform.localPosition=V(.69f,.711f,-.12f);
    }

    static void BakeReflection()
    {
        var probe=DisplayCaseReflections.Bake("ReviewReflection",V(0,1.4f,0),
            new Bounds(V(0,1.4f,0),V(8,2.8f,8)),DisplayCaseFinish.Folder,512);
        RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture=probe.customBakedTexture;
    }
    public static void Validate(GameObject intact)
    {
        var panes=intact.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Glass_")).ToArray();
        DemoValidation.Check(panes.Length==5,"Intact display case retains five independent panes");
        DemoValidation.Check(panes.All(r=>r.sharedMaterials.Length==2 && r.sharedMaterials[0].shader.name=="CrimeScene/DisplayGlass"),"All panes use dielectric glass and polished cut edges");
        DemoValidation.Check(panes.All(r=>r.shadowCastingMode==ShadowCastingMode.Off),"Clear panes do not cast opaque shadows");
        Shader shader=DisplayCaseFinish.Pane.shader;
        DemoValidation.Check(shader.isSupported && !ShaderUtil.ShaderHasError(shader),"Display glass shader compiles on this graphics device");
        int triangles=intact.GetComponentsInChildren<MeshFilter>().Where(f=>f.transform.parent==intact.transform).Sum(f=>f.sharedMesh.triangles.Length/3);
        DemoValidation.Check(triangles<=StoreKit.Budget("display_case"),"Bevelled case stays within its triangle budget");
        DemoValidation.Info("Intact case triangles="+triangles+"; GPU="+SystemInfo.graphicsDeviceName);
    }

    static void Capture(Camera camera,string name,Vector3 position,Vector3 target)
    {
        camera.transform.position=position;camera.transform.LookAt(target);
        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=8};
        var previous=RenderTexture.active;var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;
            texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
            File.WriteAllBytes(Path.Combine(Output,name),texture.EncodeToPNG());
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
#endif


