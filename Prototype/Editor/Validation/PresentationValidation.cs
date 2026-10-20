#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static partial class NightStoreValidation
{
    public static void PresentationChecks()=>PresentationValidation.Check();
}

public static class PresentationValidation
{
    public static void Check(){ToolSafety();Shadows();Scene();}
    static void Require(bool condition,string text){if(!condition)throw new Exception(text);}
    static void ToolSafety(){
        var group=new GameObject("Temporary tool safety test");group.transform.position=Vector3.one*900;
        try{
            var player=new GameObject("Safety player");player.transform.SetParent(group.transform,false);var body=player.AddComponent<CharacterController>();body.height=1.7f;body.center=Vector3.up*.85f;body.radius=.23f;
            var holder=player.AddComponent<HandInteractor>();
            var traceObject=GameObject.CreatePrimitive(PrimitiveType.Quad);traceObject.transform.SetParent(group.transform,false);traceObject.transform.localRotation=Quaternion.Euler(90,0,0);traceObject.transform.localScale=Vector3.one*.02f;
            Object.DestroyImmediate(traceObject.GetComponent<Collider>());var trace=traceObject.AddComponent<UVTrace>();
            foreach(DemoToolKind kind in Enum.GetValues(typeof(DemoToolKind))){
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.transform.SetParent(group.transform,false);go.transform.localPosition=new Vector3(0,.5f,0);go.transform.localScale=new Vector3(.5f,1,.5f);
                var tool=go.AddComponent<DeployedTool>();tool.Kind=kind;tool.ApplyCollisionPolicy();
                Require(go.GetComponent<Rigidbody>().isKinematic&&!go.GetComponent<Rigidbody>().useGravity,"Tool can apply dynamic forces: "+kind);
                Require(go.GetComponent<Collider>().isTrigger==(kind!=DemoToolKind.Lamp),"Tool pass-through policy wrong: "+kind);
                Physics.SyncTransforms();
                Require(trace.LitFraction(group.transform.position+Vector3.up*2,Vector3.down,.9f,4)<.01f,"Non-solid tool no longer occludes UV evidence: "+kind);
                var ray=new Ray(group.transform.position+new Vector3(0,.5f,-2),Vector3.forward);
                Require(DemoPhysics.Nearest(ray,4,out var selected,player.transform,null,false,QueryTriggerInteraction.Collide)&&selected.collider.GetComponent<DeployedTool>()==tool,"Tool cannot be selected after collision fix: "+kind);
                XRLocomotion.Relocate(player.transform,group.transform.position+Vector3.back);
                body.Move(Vector3.forward*2);
                if(kind!=DemoToolKind.Lamp){
                    Require(Mathf.Abs(player.transform.position.y-group.transform.position.y)<.03f&&player.transform.localPosition.z>.8f,"Passing through tool displaces or blocks player: "+kind);
                }else Require(player.transform.localPosition.z<-.25f,"Placed scene lamp no longer blocks walking");
                tool.Holder=holder;Require(go.GetComponent<Collider>().isTrigger,"Held lamp/tool blocks its carrier");
                Require(trace.LitFraction(group.transform.position+Vector3.up*2,Vector3.down,.9f,4)>.99f,"Held tool blocks its own torch");
                tool.Holder=null;Require(go.GetComponent<Collider>().isTrigger==(kind!=DemoToolKind.Lamp),"Release fails to restore lamp collision");
                Object.DestroyImmediate(go);
            }
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(group.transform,false);wall.transform.localPosition=new Vector3(0,1,0);wall.transform.localScale=new Vector3(3,2,.2f);Physics.SyncTransforms();XRLocomotion.Relocate(player.transform,group.transform.position+Vector3.back);body.Move(Vector3.forward*2);
            Require(player.transform.localPosition.z<-.25f&&Mathf.Abs(player.transform.localPosition.y)<.03f,"Player can cross a solid wall or is launched upward");
            Debug.Log("TOOL_PLAYER_SAFETY_PASS all six kinds: kinematic, pass-through, ray selectable; placed lamp solid and held lamp non-solid");
            Debug.Log("TOOL_UV_OCCLUSION_AND_WALL_PASS trigger tools still hide UV evidence, held tools do not, solid wall still stops the player");
        }finally{Object.DestroyImmediate(group);}
    }
    static Color[] Render(Camera camera,RenderTexture target){
        camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
        var read=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);read.ReadPixels(new Rect(0,0,target.width,target.height),0,0);read.Apply();var pixels=read.GetPixels();
        Object.DestroyImmediate(read);RenderTexture.active=previous;return pixels;
    }
    static void Shadows(){
        var group=new GameObject("Temporary shadow render test");group.transform.position=Vector3.one*1000;
        var target=new RenderTexture(384,384,24);var mat=new Material(Shader.Find("Standard"));mat.color=new Color(.65f,.65f,.65f);mat.SetFloat("_Glossiness",.12f);
        bool fog=RenderSettings.fog,standalone=DemoPlatform.ForceStandalone;var ambient=RenderSettings.ambientLight;var quality=QualitySettings.shadows;float distance=QualitySettings.shadowDistance;int pixels=QualitySettings.pixelLightCount;
        try{
            RenderSettings.fog=false;RenderSettings.ambientLight=Color.black;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=14;QualitySettings.pixelLightCount=4;
            foreach(var item in new[]{("Floor",new Vector3(0,-.1f,0),new Vector3(4,.2f,4)),("Blocker",new Vector3(0,.5f,0),new Vector3(.4f,1,.4f))}){
                var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name=item.Item1;cube.transform.SetParent(group.transform,false);cube.transform.localPosition=item.Item2;cube.transform.localScale=item.Item3;cube.layer=31;cube.GetComponent<Renderer>().sharedMaterial=mat;
            }
            var go=new GameObject("Test camera");go.transform.SetParent(group.transform,false);var camera=go.AddComponent<Camera>();camera.transform.localPosition=new Vector3(3,4,-4);camera.transform.LookAt(group.transform.position);camera.orthographic=true;camera.orthographicSize=1.7f;camera.targetTexture=target;camera.stereoTargetEye=StereoTargetEyeMask.None;camera.cullingMask=1<<31;camera.backgroundColor=Color.black;camera.clearFlags=CameraClearFlags.SolidColor;camera.renderingPath=RenderingPath.Forward;
            var control=group.AddComponent<DemoLighting>();control.TorchesExclusive=true;
            for(int tier=0;tier<2;tier++)for(int uv=0;uv<2;uv++){
                // Recreate each torch so its real platform configuration is exercised.
                DemoPlatform.ForceStandalone=tier==1;
                var owner=new GameObject("Actual torch setup");owner.transform.SetParent(group.transform,false);var setup=owner.AddComponent<DemoLighting>();
                if(uv==1)setup.UVTorch(true);else setup.Flashlight(true);
                var light=uv==1?setup.UVTorchLight:setup.FlashlightLight;light.cullingMask=1<<31;
                light.transform.position=group.transform.position+new Vector3(0,3,-2);light.transform.LookAt(group.transform.position+Vector3.forward*.4f);
                var configured=light.shadows;Require(configured!=LightShadows.None,"Torch shadow disabled");
                light.shadows=LightShadows.None;var unshadowed=Render(camera,target);light.shadows=configured;Render(camera,target);var shadowed=Render(camera,target);
                int darkened=0;for(int i=0;i<shadowed.Length;i++)if(unshadowed[i].maxColorComponent-shadowed[i].maxColorComponent>.025f)darkened++;
                Require(darkened>80,"Torch produced no visible rendered occlusion: tier="+tier+" uv="+uv+" pixels="+darkened);
                Debug.Log("TORCH_RENDERED_SHADOW_PASS tier="+tier+" uv="+uv+" shadowPixels="+darkened+" map="+light.shadowCustomResolution);
                Directory.CreateDirectory("Builds");var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(384,384,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,384,384),0,0);image.Apply();File.WriteAllBytes("Builds/torch-shadow-"+tier+"-"+uv+".png",image.EncodeToPNG());Object.DestroyImmediate(image);RenderTexture.active=previous;
                setup.ReleaseTorchResources();Object.DestroyImmediate(owner);
            }
        }finally{RenderSettings.fog=fog;RenderSettings.ambientLight=ambient;QualitySettings.shadows=quality;QualitySettings.shadowDistance=distance;QualitySettings.pixelLightCount=pixels;DemoPlatform.ForceStandalone=standalone;Object.DestroyImmediate(group);Object.DestroyImmediate(mat);target.Release();Object.DestroyImmediate(target);}
    }
    static void Scene(){
        var root=GameObject.Find("NightStore").transform;
        Require(!root.Find("Exterior/Boundary").gameObject.activeSelf,"Old street barrier still blocks the investigation");
        Require(root.Find("CitySkyline360/ContinuousDistantBuildings"),"360 degree city missing");
        Require(root.Find("Exterior/LivingStreetCanopy").childCount>=10,"Leafy street trees missing");
        Require(root.Find("Exterior/MainStreetArchitecture/TerracedShopFrontDetails"),"Reference-based exterior architecture missing");
        Require(RenderSettings.fogDensity<.02f,"Fog still hides the distant city");
        foreach(var l in root.Find("Lighting").GetComponentsInChildren<Light>()){
            var p=root.InverseTransformPoint(l.transform.position);
            Require(!(l.enabled&&l.type!=LightType.Directional&&p.z>.25f&&p.z<8.25f&&Mathf.Abs(p.x)<7.7f),"Store interior fixture is powered on: "+l.name);
        }
        foreach(var b in Object.FindObjectsByType<InteriorBounds>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var p in new[]{new Vector3(-23,0,-8),new Vector3(23,0,-8),new Vector3(0,0,-15),new Vector3(-23,0,-2.99f),new Vector3(-23,0,-3.01f),new Vector3(-23,0,-13.99f),new Vector3(-23,0,-14.01f)})Require(b.Contains(p),"Road/kerb not available for investigation: "+p);
        var car=root.Find("Exterior/PoliceCar");Require(Mathf.Abs(Vector3.Dot(car.forward,Vector3.right))>.99f,"Car is not parked along the road");
        var model=car.Find("SuppliedModel");Require(model&&Mathf.Abs(model.localScale.x-model.localScale.y)<.0001f&&Mathf.Abs(model.localScale.y-model.localScale.z)<.0001f,"Supplied car is missing or flattened");
        foreach(var f in model.GetComponentsInChildren<MeshFilter>())Require(UnityEditor.AssetDatabase.GetAssetPath(f.sharedMesh).EndsWith("police+car+3d+model.fbx"),"Supplied car mesh was replaced/modified");
        foreach(var mark in root.GetComponentsInChildren<UVTrace>(true).Where(t=>t.name.StartsWith("EvidenceUV_HandlePrint"))){
            var p=mark.transform.localPosition;Require(Mathf.Abs(Mathf.Abs(p.x)-.815f)<.005f&&p.y>=1&&p.y<=1.15f,"Entrance print is not on the pull handle");
        }
        Require(root.GetComponentsInChildren<UVTrace>(true).Count(t=>t.name.StartsWith("EvidenceUV_HandlePrint"))==6,"Handle prints missing");
        foreach(var t in root.Find("Dressing/FloorBloodTraces").GetComponentsInChildren<UVTrace>())Require(Mathf.Max(t.transform.localScale.x,t.transform.localScale.y)<=.055f,"Blood trace too large for small injury");
        Debug.Log("PRESENTATION_SCENE_CHECKS_PASS road bounds, 360 skyline, foliage, sparse blood, handle prints, preserved non-flattened supplied FBX");
    }
}
#endif
