#if UNITY_EDITOR
using System;
using UnityEngine;
using Object=UnityEngine.Object;
public static class VRVisualValidation
{
    // Runtime torch models must never be authored into a saved player camera.
    // Their DemoLighting owner recreates them when equipped and attaches them to a hand.
    public static int PruneCameraPreviewTorches(){
        if(Application.isPlaying)return 0;int removed=0;
        foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            if(camera.transform.root.name!="Player")continue;
            var children=new System.Collections.Generic.List<GameObject>();
            foreach(Transform t in camera.transform)if(t.name=="Flashlight"||t.name=="UVTorch"||t.name=="FlashlightModel"||t.name=="UVFlashlightModel")children.Add(t.gameObject);
            foreach(var child in children){Object.DestroyImmediate(child);removed++;}
        }
        if(removed>0)Debug.Log("SAVED_PREVIEW_TORCH_CLEANUP removed="+removed);return removed;
    }
    public static void CheckPreviewTorchOwnership(){
        var owner=new GameObject("Temporary torch ownership validation");owner.hideFlags=HideFlags.DontSave;
        var lighting=owner.AddComponent<DemoLighting>();lighting.TorchesExclusive=false;
        try{
            lighting.Flashlight(true);lighting.UVTorch(true);
            var white=lighting.FlashlightLight;var uv=lighting.UVTorchLight;
            foreach(var light in new[]{white,uv})if((light.gameObject.hideFlags&HideFlags.DontSave)!=HideFlags.DontSave)throw new Exception("Editor torch light can be saved");
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(Transform t in camera.transform)
                if((t.name=="FlashlightModel"||t.name=="UVFlashlightModel")&&(t.gameObject.hideFlags&HideFlags.DontSave)!=HideFlags.DontSave)throw new Exception("Editor torch model can be saved");
            lighting.ReleaseTorchResources();
            if(white||uv||lighting.TorchEquipped(false)||lighting.TorchEquipped(true))throw new Exception("Preview torch resource cleanup failed");
            if(PruneCameraPreviewTorches()!=0)throw new Exception("Preview destruction left orphaned camera models");
            Debug.Log("PREVIEW_TORCH_OWNERSHIP_PASS both torches excluded from saves and explicitly destroyed");
        }finally{lighting.ReleaseTorchResources();Object.DestroyImmediate(owner);}
    }
    public static void HandsRun(){
        try{CheckHands();UnityEditor.EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);UnityEditor.EditorApplication.Exit(1);}
    }
    public static void CheckHands(){
        var stage=new GameObject("Hand render validation");
        var cameraObject=new GameObject("Hand validation camera");
        var rt=new RenderTexture(960,900,24);var previous=RenderTexture.active;
        var previewLight=new GameObject("Grip preview light").AddComponent<Light>();previewLight.transform.SetParent(stage.transform);previewLight.type=LightType.Directional;previewLight.transform.rotation=Quaternion.Euler(45,-30,0);previewLight.intensity=1.5f;previewLight.cullingMask=1<<31;
        var camera=cameraObject.AddComponent<Camera>();camera.targetTexture=rt;camera.stereoTargetEye=StereoTargetEyeMask.None;camera.cullingMask=1<<31;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.09f,.12f);camera.orthographic=true;camera.orthographicSize=.29f;camera.nearClipPlane=.01f;
        string gripFailure=null;
        try{
            for(int side=0;side<2;side++)for(int pose=0;pose<3;pose++){
                var owner=new GameObject("Hand validation owner");owner.transform.SetParent(stage.transform,false);
                owner.transform.localPosition=new Vector3(side*.24f-.12f,0,pose*.21f-.21f);
                var input=owner.AddComponent<XRControllerInput>();input.Node=side==0?UnityEngine.XR.XRNode.LeftHand:UnityEngine.XR.XRNode.RightHand;
                var ghost=owner.AddComponent<XRGhostHand>();ghost.Build(input);ghost.Root.gameObject.SetActive(true);
                ghost.SetTorchGrip(pose>0);ghost.SetPose(pose==0?0:pose==1?.8f:1,pose==2?1:0,1,0);
                var animator=ghost.Root.GetComponentInChildren<Animator>();
                if(!animator)throw new Exception("Official hand animator missing");
                var expected=Resources.Load<RuntimeAnimatorController>("MetaHands/Animations/OpenXR_HandController_"+(input.Left?"Left":"Right"));
                if(animator.runtimeAnimatorController!=expected)throw new Exception("Hand animation does not match controller side");
                animator.Update(.1f);
                if(pose>0 && Vector3.Dot(ghost.Root.localRotation*Vector3.right,Vector3.right)<.9f)throw new Exception("Held hand flips its palm to the opposite side");
                if(pose==0){
                    Transform wrist=null,middle=null;
                    foreach(var bone in ghost.Root.GetComponentsInChildren<Transform>()){
                        if(bone.name=="XRHand_Wrist")wrist=bone;
                        if(bone.name=="XRHand_MiddleTip")middle=bone;
                    }
                    if(Vector3.Dot((middle.position-wrist.position).normalized,owner.transform.TransformDirection(XRGhostHand.GripPitch*Vector3.forward))<.85f)throw new Exception("Hand fingers do not align with controller forward: "+side);
                    Debug.Log("HAND_CONTROLLER_FORWARD_PASS side="+side);
                }
                foreach(var t in owner.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
                if(pose>0){
                    var torch=DemoToolGeometry.CreateFlashlight(side==0);torch.transform.SetParent(owner.transform,false);
                    var torchPose=XRGhostHand.TorchPose(side==0);torch.transform.localPosition=torchPose.position;torch.transform.localRotation=torchPose.rotation;
                    foreach(var t in torch.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
                }
                var renderer=ghost.Root.GetComponentInChildren<SkinnedMeshRenderer>();
                var baked=new Mesh();renderer.BakeMesh(baked);var bounds=new Bounds(renderer.transform.TransformPoint(baked.vertices[0]),Vector3.zero);
                foreach(var v in baked.vertices)bounds.Encapsulate(renderer.transform.TransformPoint(v));Object.DestroyImmediate(baked);
                if(bounds.size.magnitude<.08f||bounds.size.magnitude>.35f||Mathf.Min(bounds.size.x,bounds.size.y,bounds.size.z)<.012f)throw new Exception("Hand has invalid scale or collapsed mesh: "+bounds.size);
                if(pose>0){
                    var bakedGrip=new Mesh();renderer.BakeMesh(bakedGrip);
                    var torchPose=XRGhostHand.TorchPose(side==0);var inverse=Quaternion.Inverse(torchPose.rotation);
                    var points=bakedGrip.vertices;var triangles=bakedGrip.triangles;float penetration=0;
                    Action<Vector3> inspect=v=>{
                        var local=inverse*(owner.transform.InverseTransformPoint(renderer.transform.TransformPoint(v))-torchPose.position);
                        if(local.z<=-.154f||local.z>=0)return;
                        float radius=local.z>-.012f?.0185f:local.z>-.034f?.0175f:.0135f;
                        penetration=Mathf.Max(penetration,radius-new Vector2(local.x,local.y).magnitude);
                    };
                    foreach(var v in points)inspect(v);
                    for(int t=0;t<triangles.Length;t+=3)inspect((points[triangles[t]]+points[triangles[t+1]]+points[triangles[t+2]])/3);
                    Object.DestroyImmediate(bakedGrip);
                    if(penetration>.0015f)gripFailure="Flashlight penetrates hand by "+penetration+" side="+side+" pose="+pose;
                    if(penetration<=.0015f)Debug.Log("TORCH_GRIP_CLEARANCE_PASS side="+side+" pose="+pose+" maximumPenetration="+penetration);
                }
                Debug.Log("HAND_MESH_SHAPE_PASS side="+side+" pose="+pose+" size="+bounds.size);
            }
            camera.transform.position=new Vector3(.12f,.65f,-.38f);camera.transform.LookAt(Vector3.zero);camera.Render();
            RenderTexture.active=rt;var read=new Texture2D(960,900,TextureFormat.RGB24,false);read.ReadPixels(new Rect(0,0,960,900),0,0);read.Apply();
            System.IO.Directory.CreateDirectory("Builds");System.IO.File.WriteAllBytes("Builds/hand-rig-validation.png",read.EncodeToPNG());Object.DestroyImmediate(read);
            if(gripFailure!=null)throw new Exception(gripFailure);
        }finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(stage);Object.DestroyImmediate(cameraObject);}
    }
    public static void Check(){
        CheckHands();
        var mesh=DemoUi.RoundedQuad(1,1,0,1);
        if(mesh.uv.Length!=mesh.vertexCount)throw new Exception("VR icon panel lacks UVs");
        var root=new GameObject("Temporary VR icon validation");var cameraObject=new GameObject("Temporary icon camera");
        var texture=new Texture2D(4,4,TextureFormat.RGBA32,false);var pixels=new Color[16];for(int i=0;i<16;i++)pixels[i]=Color.red;pixels[0]=Color.clear;texture.SetPixels(pixels);texture.Apply();
        var mat=DemoUi.UnlitMaterial(Color.white);mat.mainTexture=texture;
        var rt=new RenderTexture(32,32,24);var previous=RenderTexture.active;Texture2D read=null;
        try{
            root.layer=31;root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=mat;
            var camera=cameraObject.AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-1);camera.orthographic=true;camera.orthographicSize=.6f;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.stereoTargetEye=StereoTargetEyeMask.None;camera.targetTexture=rt;camera.Render();
            RenderTexture.active=rt;read=new Texture2D(32,32,TextureFormat.RGB24,false);read.ReadPixels(new Rect(0,0,32,32),0,0);read.Apply();var color=read.GetPixel(16,16);
            if(color.r<.5f||color.g>.2f)throw new Exception("VR icon panel texture failed rendered-pixel test: "+color);
            var head=new GameObject("Temporary head");head.transform.SetParent(root.transform);head.transform.localPosition=Vector3.up*1.6f;
            var holsters=root.AddComponent<XRHolsters>();holsters.Head=head.transform;holsters.UpdateHeading(.02f);
            var before=holsters.Position(XRHolsters.Slot.Flashlight);head.transform.rotation=Quaternion.Euler(0,90,0);holsters.UpdateHeading(.02f);
            if(Vector3.Distance(before,holsters.Position(XRHolsters.Slot.Flashlight))>.04f)throw new Exception("Belt yaw snaps instead of easing");
            for(int i=0;i<40;i++)holsters.UpdateHeading(.02f);
            var after=holsters.Position(XRHolsters.Slot.Flashlight);
            if(Vector3.Distance(before,after)<.1f||Mathf.Abs(after.y-before.y)>.001f)throw new Exception("Belt does not follow head yaw at stable height");
            foreach(string hand in new[]{"OpenXRLeftHand","OpenXRRightHand"}){
                var model=Resources.Load<GameObject>("MetaHands/"+hand);
                if(!model||model.GetComponentsInChildren<SkinnedMeshRenderer>().Length==0)throw new Exception("Official rigged Meta hand missing: "+hand);
            }
            foreach(string side in new[]{"Left","Right"}){
                var controller=Resources.Load<RuntimeAnimatorController>("MetaHands/Animations/OpenXR_HandController_"+side);
                if(!controller||controller.animationClips.Length<4)throw new Exception("Meta authored controller animations missing: "+side);
                var instance=Object.Instantiate(Resources.Load<GameObject>("MetaHands/OpenXR"+side+"Hand"));
                instance.transform.SetParent(root.transform);
                Transform wrist=null,tip=null;
                foreach(var bone in instance.GetComponentsInChildren<Transform>()){
                    if(bone.name=="XRHand_Wrist")wrist=bone;
                    if(bone.name=="XRHand_MiddleTip")tip=bone;
                }
                var animator=wrist.parent.GetComponent<Animator>();if(!animator)animator=wrist.parent.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController=controller;animator.Rebind();animator.Update(0);
                animator.SetFloat("Flex",0);animator.Update(.1f);var open=tip.position;
                animator.SetFloat("Flex",1);animator.SetFloat("Pinch",1);animator.Update(.1f);
                if(Vector3.Distance(open,tip.position)<.025f)throw new Exception("Meta hand animation does not deform fingers: "+side);
            }
            Debug.Log("VR_VISUAL_VALIDATION_PASS rendered icon texture, smooth headset-following belt anchors and official rigged Meta hand assets");
        }finally{RenderTexture.active=previous;rt.Release();if(read)Object.DestroyImmediate(read);Object.DestroyImmediate(texture);Object.DestroyImmediate(mat);Object.DestroyImmediate(rt);Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(root);}
    }
}
[UnityEditor.InitializeOnLoad]
static class PreventSavedTorchPreviews
{
    static PreventSavedTorchPreviews(){UnityEditor.SceneManagement.EditorSceneManager.sceneSaving+=(scene,path)=>VRVisualValidation.PruneCameraPreviewTorches();}
}
#endif
