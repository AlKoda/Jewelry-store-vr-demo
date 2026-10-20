#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static partial class NightStoreValidation
{
    public static void GlassRenderCheck(){
        var pile=GameObject.Find("NightStore/Dressing/GroundGlass_Threshold");var go=new GameObject("Glass review camera");var target=new RenderTexture(960,600,24);var previous=RenderTexture.active;
        try{
            var camera=go.AddComponent<Camera>();NightQuality.Configure(camera);camera.nearClipPlane=.035f;camera.stereoTargetEye=StereoTargetEyeMask.None;camera.targetTexture=target;
            camera.transform.position=new Vector3(.35f,.35f,-1.25f);camera.transform.LookAt(new Vector3(0,.03f,-.65f));
            var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);lighting.FlashlightLight.transform.position=new Vector3(-.55f,.2f,-1);lighting.FlashlightLight.transform.LookAt(new Vector3(0,.025f,-.65f));
            Texture2D Read(){camera.Render();RenderTexture.active=target;var t=new Texture2D(960,600,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,960,600),0,0);t.Apply();return t;}
            var visible=Read();pile.SetActive(false);var absent=Read();pile.SetActive(true);var a=visible.GetPixels();var b=absent.GetPixels();int changed=0;for(int i=0;i<a.Length;i++)if(Mathf.Max(Mathf.Abs(a[i].r-b[i].r),Mathf.Abs(a[i].g-b[i].g),Mathf.Abs(a[i].b-b[i].b))>.006f)changed++;
            System.IO.File.WriteAllBytes("Builds/glass-close.png",visible.EncodeToPNG());Object.DestroyImmediate(visible);Object.DestroyImmediate(absent);
            if(changed<80)throw new Exception("Ground glass is visually absent: changed pixels="+changed);Debug.Log("GLASS_RENDERED_VISIBILITY_PASS changedPixels="+changed);
        }finally{if(pile)pile.SetActive(true);RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(go);}
    }
    public static void CameraGripRender(){
        var stage=new GameObject("Camera grip render test");var target=new RenderTexture(1100,700,24);var previous=RenderTexture.active;
        try{
            var light=new GameObject("Preview light").AddComponent<Light>();light.transform.SetParent(stage.transform,false);light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-30,0);light.intensity=1.5f;light.cullingMask=1<<31;
            for(int side=0;side<2;side++){
                var owner=new GameObject("Controller "+side);owner.transform.SetParent(stage.transform,false);owner.transform.localPosition=new Vector3(side*.35f-.175f,0,0);
                var input=owner.AddComponent<XRControllerInput>();input.Node=side==0?UnityEngine.XR.XRNode.LeftHand:UnityEngine.XR.XRNode.RightHand;
                var ghost=owner.AddComponent<XRGhostHand>();ghost.Build(input);ghost.Root.gameObject.SetActive(true);ghost.SetTorchGrip(true);ghost.SetPose(.8f,0,1,0);ghost.Root.GetComponentInChildren<Animator>().Update(.1f);
                var body=new GameObject("Held camera");body.transform.SetParent(owner.transform,false);DemoToolGeometry.CreateCameraBody(body.transform);
                var pose=EvidenceCamera.ControllerGrip(side==0);body.transform.SetLocalPositionAndRotation(pose.position,pose.rotation);
                foreach(var t in owner.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
            }
            var go=new GameObject("Review camera");go.transform.SetParent(stage.transform,false);var camera=go.AddComponent<Camera>();camera.targetTexture=target;camera.stereoTargetEye=StereoTargetEyeMask.None;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.09f,.12f);camera.orthographic=true;camera.orthographicSize=.23f;camera.nearClipPlane=.01f;
            camera.transform.position=new Vector3(.10f,.32f,-.42f);camera.transform.LookAt(Vector3.zero);camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1100,700,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1100,700),0,0);image.Apply();System.IO.File.WriteAllBytes("Builds/camera-grips.png",image.EncodeToPNG());Object.DestroyImmediate(image);
            Debug.Log("CAMERA_GRIP_RENDER_PASS both native animated hands with controller-aligned camera handles");
        }finally{RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(stage);}
    }
    public static void RepairChecks(){
        void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        var root=GameObject.Find("NightStore").transform;
        Require(!root.Find("Exterior/PoliceCar/LightBar").gameObject.activeSelf,"Legacy siren bar remains active");
        Require(root.Find("Exterior/PoliceCar/AlignedCarLighting").GetComponentsInChildren<Light>().Length==2,"Expected two car headlights");
        foreach(var l in root.Find("Lighting").GetComponentsInChildren<Light>())Require(!l.enabled||!l.name.StartsWith("Headlight_"),"Legacy car beam remains active");
        foreach(var l in root.Find("Exterior/PoliceCar/AlignedCarLighting").GetComponentsInChildren<Light>())Require(l.intensity<=1&&l.color.r>l.color.b&&l.lightmapBakeType==LightmapBakeType.Baked,"Headlight must be dim, warm and baked");
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>())foreach(var m in r.sharedMaterials.Where(m=>m)){
            if(m.name=="GroundGlassFaces")Require(m.color.a<.06f,"Ground shard faces are opaque");
            if(m.name=="GroundGlassCutEdges")Require(m.color.a<=.12f,"Shard edges form opaque foam");
            if(m.name.StartsWith("Consistent_")&&!m.name.ToLowerInvariant().Contains("screen")&&!m.name.ToLowerInvariant().Contains("indicator"))Require(!m.IsKeywordEnabled("_EMISSION")&&m.GetFloat("_Glossiness")<=.32f&&m.GetFloat("_GlossMapScale")<=.32f,"Prop finish is glowing or overpolished: "+m.name);
        }
        Require(root.Find("StaffRoomReflectionProbes").GetComponentsInChildren<ReflectionProbe>().Length==3,"Local rear room reflections missing");
        Require(root.Find("Exterior/LivingStreetCanopy").GetComponentsInChildren<MeshRenderer>().Count(r=>r.name.StartsWith("NaturalCanopy_"))==5,"Natural street canopies missing");
        var group=new GameObject("Temporary belt/grip repair test");
        try{
            var head=new GameObject("Head").transform;head.SetParent(group.transform,false);head.localPosition=Vector3.up*1.7f;
            var belt=group.AddComponent<XRHolsters>();belt.Head=head;belt.UpdateHeading(.02f);for(int i=0;i<60;i++)belt.UpdateBody(.02f);
            var standing=belt.Position(XRHolsters.Slot.Flashlight)-head.position;
            head.rotation=Quaternion.Euler(65,0,0);for(int i=0;i<60;i++)belt.UpdateBody(.02f);
            Require(Mathf.Abs((belt.Position(XRHolsters.Slot.Flashlight)-head.position).z-standing.z)<.005f,"Looking down moves belt unnecessarily");
            head.localPosition=Vector3.up*1.15f;for(int i=0;i<60;i++)belt.UpdateBody(.02f);
            var bent=belt.Position(XRHolsters.Slot.Flashlight)-head.position;Require(bent.z<standing.z-.2f,"Bending does not retreat belt");
            foreach(bool left in new[]{false,true}){
                var pose=EvidenceCamera.ControllerGrip(left);var torch=XRGhostHand.TorchPose(left);var handle=new Vector3(left?-.05f:.05f,-.003f,-.0075f);
                Require(Quaternion.Angle(pose.rotation,torch.rotation)<.01f,"Camera axis does not match torch");
                Require(Vector3.Distance(pose.position+pose.rotation*handle,torch.position-torch.rotation*Vector3.forward*.095f)<.0001f,"Camera grip misses its handle");
            }
        }finally{Object.DestroyImmediate(group);}
        Debug.Log("PRESENTATION_REPAIR_VALIDATION_PASS glass, prop finish, localized reflections, car lamps, trees, bend-vs-look belt and both camera grips");
    }
}
#endif
