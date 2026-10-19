#if UNITY_EDITOR
using System;
using UnityEngine;
using Object=UnityEngine.Object;
public static class VRVisualValidation
{
    public static void Check(){
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
            Debug.Log("VR_VISUAL_VALIDATION_PASS rendered icon texture, smooth headset-following belt anchors and official rigged Meta hand assets");
        }finally{RenderTexture.active=previous;rt.Release();if(read)Object.DestroyImmediate(read);Object.DestroyImmediate(texture);Object.DestroyImmediate(mat);Object.DestroyImmediate(rt);Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(root);}
    }
}
#endif
