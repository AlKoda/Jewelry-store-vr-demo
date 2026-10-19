#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;
using Object=UnityEngine.Object;

// Selected October 18 assets, attached architectural details, and authored UV cues.
public static class UrbanDetailPass
{
    const string Folder="Assets/CrimeSceneDemo/CombinedArt";
    static Transform root;
    static Material iron,brass,rubber;
    public static void Apply()
    {
        root=GameObject.Find("NightStore").transform;
        iron=Surface("WeatheredIron","PaintedMetalChipped",new Color(.19f,.21f,.22f),.3f,.22f);
        brass=Mat(Folder,"TorchBrass",new Color(.48f,.32f,.12f),.28f);brass.SetFloat("_Metallic",.85f);
        rubber=Mat(Folder,"TorchHoseRubber",new Color(.035f,.027f,.02f),.22f);
        Buildings();Architecture();Props();CuttingTorch();Traces();Audio();
        Physics.SyncTransforms();Validate();
        Debug.Log("URBAN_DETAIL_PASS selected PBR props, extended facades, fire escapes, window rails, rebuilt cutting torch, UV-only traces");
    }
    static Material Surface(string name,string set,Color tint,float metallic,float gloss)
    {
        var m=Mat(Folder,name,tint,gloss);m.SetFloat("_Metallic",metallic);
        m.mainTexture=DemoAssetLibrary.Texture("RealismTextures","Surfaces/"+set+"_Neutral.jpg")??DemoAssetLibrary.Texture("RealismTextures","Surfaces/"+set+"_Color.jpg");
        m.SetTexture("_BumpMap",DemoAssetLibrary.NormalMapAny("RealismTextures","Surfaces/"+set+"_Normal"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.4f);
        DemoAssetLibrary.ApplyMetallicSmoothness(m,DemoAssetLibrary.DataMap("RealismTextures","Surfaces/"+set+"_MetallicSmoothness.png"));
        m.SetFloat("_GlossMapScale",gloss);
        DemoAssetLibrary.ApplyOcclusion(m,DemoAssetLibrary.DataMap("RealismTextures","Surfaces/"+set+"_Occlusion.jpg"));return m;
    }
    static void Buildings()
    {
        var group=Group("StreetContinuationBuildings",root.Find("Exterior/Buildings"));
        for(int side=-1;side<=1;side+=2)for(int row=0;row<2;row++)for(int i=0;i<2;i++){
            float x0=side<0?-44+i*12:20.4f+i*12,x1=x0+11.6f;
            if(row==1){x0=side<0?-48+i*11:28.4f+i*11;x1=x0+10.6f;}
            float height=row==0?7.5f+i*3:10.5f-i*1.5f,z=row==0?0:NightStoreLayout.FarPavementZ,facing=row==0?-1:1;
            var k=new KitMesh{LightmapScale=NightLighting.FarScale};
            k.Box((i+row)%2==0?StoreKit.Brick:StoreKit.Render,new Vector3((x0+x1)*.5f,height*.5f,z-facing*5),new Vector3(x1-x0,height,10));
            NightStoreBuilder.Shopfront(k,x0,x1,z,facing);NightStoreBuilder.Windows(k,new System.Random(101+row*10+i),x0,x1,z,facing,height,.16f);
            k.Box(StoreKit.Concrete,new Vector3((x0+x1)*.5f,height,z-facing*.05f),new Vector3(x1-x0+.15f,.18f,.32f));
            k.Emit("Continuation_"+side+"_"+row+"_"+i,group,Vector3.zero,Quaternion.identity,Folder,"continuation_"+side+"_"+row+"_"+i);
        }
        StoreKit.MarkStatic(group.gameObject,true);
    }
    static void Balcony(KitMesh k,float x,float y,float z,float facing,float width=1.5f)
    {
        float front=z+facing*.88f;
        k.Box(iron,new Vector3(x,y-.055f,z+facing*.43f),new Vector3(width,.08f,.94f));
        for(float dx=-width*.5f;dx<=width*.5f+.001f;dx+=.15f)k.Cylinder(iron,new Vector3(x+dx,y,front),new Vector3(x+dx,y+.95f,front),.012f,6);
        k.Cylinder(iron,new Vector3(x-width*.5f,y+.95f,front),new Vector3(x+width*.5f,y+.95f,front),.022f,8);
        foreach(float end in new[]{-1f,1f}){
            k.Cylinder(iron,new Vector3(x+end*width*.5f,y+.95f,z),new Vector3(x+end*width*.5f,y+.95f,front),.018f,8);
            k.Cylinder(iron,new Vector3(x+end*width*.4f,y-.65f,z),new Vector3(x+end*width*.4f,y-.08f,front),.022f,8);
        }
    }
    static void Escape(Transform parent,float x,float z,float facing,int id)
    {
        var k=new KitMesh{LightmapScale=NightLighting.FarScale};
        foreach(float y in id==2?new[]{4.1f}:new[]{4.1f,7.1f}){
            Balcony(k,x,y,z,facing,2.8f);
            // Plate strips leave a genuinely open grated floor between the beams.
            for(float dx=-1.3f;dx<1.4f;dx+=.14f)k.Box(iron,new Vector3(x+dx,y+.005f,z+facing*.43f),new Vector3(.035f,.025f,.85f));
        }
        float lx=x+.95f,lz=z+facing*1.12f,top=id==2?5.25f:8.15f;
        foreach(float dx in new[]{-.23f,.23f})k.Cylinder(iron,new Vector3(lx+dx,2.1f,lz),new Vector3(lx+dx,top,lz),.026f,8);
        for(float y=2.25f;y<top;y+=.28f)k.Cylinder(iron,new Vector3(lx-.23f,y,lz),new Vector3(lx+.23f,y,lz),.018f,8);
        foreach(float y in id==2?new[]{2.4f,4.2f,5.2f}:new[]{2.4f,4.2f,6.1f,7.8f})foreach(float dx in new[]{-.23f,.23f})k.Cylinder(iron,new Vector3(lx+dx,y,z),new Vector3(lx+dx,y,lz),.02f,8);
        k.Emit("FireEscape_"+id,parent,Vector3.zero,Quaternion.identity,Folder,"fire_escape_"+id);
    }
    static void Architecture()
    {
        var group=Group("FacadeAttachments",root.Find("Exterior"));var k=new KitMesh{LightmapScale=NightLighting.FarScale};
        foreach(float x in new[]{-18.4f,-16f,11.6f,16.4f})Balcony(k,x,4.13f,0,-1);
        foreach(float x in new[]{-19.6f,13.91f})Balcony(k,x,4.13f,-17,1);
        // Window boxes and roller-blind housings add depth without opaque room cards.
        foreach(float x in new[]{-16f,14f,18.8f}){
            k.Box(iron,new Vector3(x,4.03f,-.22f),new Vector3(1.3f,.22f,.38f));
            k.Box(StoreKit.DeviceBlack,new Vector3(x,4.15f,-.22f),new Vector3(1.21f,.025f,.29f));
            k.Box(iron,new Vector3(x,5.78f,-.1f),new Vector3(1.38f,.16f,.19f));
        }
        k.Emit("WindowRailsAndBoxes",group,Vector3.zero,Quaternion.identity,Folder,"window_rails_boxes");
        Escape(group,-22.7f,-17,1,0);Escape(group,22.0f,-17,1,1);Escape(group,-11.8f,0,-1,2);
        StoreKit.MarkStatic(group.gameObject,true);
    }
    internal static GameObject Prop(string pack,string name,Transform parent,Vector3 position,float yaw=0,float scale=1)
    {
        var go=DemoAssetLibrary.PlacePack(pack,name,parent,position,yaw,new DemoAssetLibrary.PackOptions{Scale=scale,Shadows=true,Lod=DemoAssetLibrary.LodClass.Prop});
        if(!go)throw new Exception("Selected asset missing: "+pack+"/"+name);
        DemoAssetLibrary.Seat(go,position.y);
        // AmbientProps' colour-only materials take their RealismTextures/PropMaps set, matched per
        // material (pipes group 1/2, cable boxes, dirty spray cans), not per model.
        if(pack=="AmbientProps")foreach(var r in go.GetComponentsInChildren<MeshRenderer>())foreach(var m in r.sharedMaterials)if(m)DemoAssetLibrary.ApplyPropMaps(m);
        return go;
    }
    static void Props()
    {
        var group=Group("SelectedAmbientProps",root);
        var rack=Prop("AmbientProps2","WornMetalRack",group,new Vector3(6.9f,0,11.3f),180);
        Support(Prop("AmbientProps2","PlasticTub",group,new Vector3(6.9f,.44f,11.25f),0,.6f),rack);
        Support(Prop("AmbientProps","PlasticCrate",group,new Vector3(6.8f,.9f,11.25f),0,.65f),rack);
        Support(Prop("AmbientProps2","WaterBottleGallon",group,new Vector3(7.15f,1.45f,11.25f)),rack);
        Support(Prop("AmbientProps2","CleanerSprayBottle",group,new Vector3(6.7f,1.45f,11.25f)),rack);
        Prop("AmbientProps2","MetalStool",group,new Vector3(-6,0,10.05f));
        var shelf=Prop("AmbientProps2","DisplayShelvesWood",group,new Vector3(-6.8f,0,11.3f),90);
        Support(Prop("AmbientProps","PlasticThermos",group,new Vector3(-6.6f,.55f,11.3f)),shelf);
        Support(Prop("AmbientProps2","PlasticJug",group,new Vector3(-6.95f,.55f,11.3f)),shelf);
        Support(Prop("AmbientProps2","WickerBasket",group,new Vector3(-6.8f,1.05f,11.3f)),shelf);
        Prop("AmbientProps2","StreetPlanterBox",group,new Vector3(-9.1f,0,-.8f),0,1.2f);
        Prop("AmbientProps2","StreetPlanterBox",group,new Vector3(9.1f,0,-.8f),0,1.2f);
        Support(Prop("AmbientProps2","WickerBasket",group,new Vector3(7,.55f,1.1f),0,.8f),root.Find("Showroom/SideTable").gameObject);
        var leaves=new Material(Shader.Find("CrimeScene/SurfaceDecal"));leaves.mainTexture=DemoAssetLibrary.Texture("CutoutDecals","Leaves/LeafCluster_DryPile.png");leaves.SetFloat("_Glossiness",.12f);var leafOld=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/DryLeafPile.mat");if(leafOld){EditorUtility.CopySerialized(leaves,leafOld);Object.DestroyImmediate(leaves);leaves=leafOld;}else AssetDatabase.CreateAsset(leaves,Folder+"/DryLeafPile.mat");
        foreach(var point in NightStreet.TreePositions){var decal=GameObject.CreatePrimitive(PrimitiveType.Quad);decal.name="DryLeavesAtTree";decal.transform.SetParent(group,false);decal.transform.localPosition=point+new Vector3(.55f,.002f,.2f);decal.transform.localRotation=Quaternion.Euler(90,25,0);decal.transform.localScale=new Vector3(1.5f,.9f,1);Object.DestroyImmediate(decal.GetComponent<Collider>());var renderer=decal.GetComponent<MeshRenderer>();renderer.sharedMaterial=leaves;renderer.shadowCastingMode=ShadowCastingMode.Off;}
        // Nothing left floating: imported bounds supply the support/collision surfaces.
        foreach(var go in group.Cast<Transform>())if(go.localPosition.y<.05f&&go.name!="DryLeavesAtTree")DemoAssetLibrary.AddBoundsCollider(go.gameObject);
        StoreKit.MarkStatic(group.gameObject,true);
    }
    static void Support(GameObject prop,GameObject support)
    {
        float desired=prop.transform.position.y;float nearest=float.PositiveInfinity;
        var ray=new Ray(prop.transform.position+Vector3.up*.3f,Vector3.down);
        foreach(var mesh in support.GetComponentsInChildren<MeshFilter>())if(DemoValidation.TriangleRay(ray,.7f,DemoValidation.NewRayMesh(mesh),out float distance))nearest=Mathf.Min(nearest,distance);
        if(float.IsPositiveInfinity(nearest))throw new Exception("No physical shelf beneath "+prop.name+" at "+prop.transform.position);
        DemoAssetLibrary.Seat(prop,desired+.3f-nearest+.002f);
    }
    static void CuttingTorch()
    {
        var old=root.Find("Dressing/TorchSet");if(old)old.gameObject.SetActive(false);
        var group=Group("RebuiltCuttingTorch",root.Find("Dressing"));group.localPosition=new Vector3(-.55f,0,8.95f);
        var paint=Surface("CylinderPaint","PaintedMetalChipped",new Color(.36f,.40f,.43f),.2f,.25f);
        var k=new KitMesh();
        k.Lathe(paint,Vector3.zero,Quaternion.identity,new[]{.105f,.13f,.14f,.14f,.125f,.075f,.042f},new[]{.035f,.05f,.11f,.48f,.54f,.59f,.61f},32);
        k.Lathe(iron,Vector3.zero,Quaternion.identity,new[]{.115f,.115f},new[]{.008f,.05f},24);
        k.Cylinder(brass,new Vector3(0,.6f,0),new Vector3(0,.69f,0),.027f,16);
        k.Cylinder(brass,new Vector3(0,.655f,0),new Vector3(.10f,.655f,0),.017f,12);
        k.Cylinder(iron,new Vector3(0,.70f,0),new Vector3(0,.715f,0),.052f,12);
        foreach(float x in new[]{-.12f,.12f})k.Cylinder(paint,new Vector3(x,.54f,0),new Vector3(x,.76f,0),.016f,12);
        k.Cylinder(paint,new Vector3(-.12f,.76f,0),new Vector3(.12f,.76f,0),.022f,16);
        k.Cylinder(StoreKit.Stainless,new Vector3(.065f,.69f,-.015f),new Vector3(.065f,.69f,-.045f),.04f,20);
        k.Cylinder(Mat(Folder,"GaugePaper",new Color(.8f,.79f,.7f),.15f),new Vector3(.065f,.69f,-.046f),new Vector3(.065f,.69f,-.047f),.033f,20);
        k.Cylinder(iron,new Vector3(.065f,.69f,-.048f),new Vector3(.08f,.71f,-.048f),.002f,6);
        Vector3 previous=new Vector3(.10f,.65f,0);
        for(int i=1;i<=56;i++){
            float t=i/56f;Vector3 p;
            if(t<.23f){float f=t/.23f;p=Vector3.Lerp(new Vector3(.10f,.65f,0),new Vector3(.28f,.018f,.15f),f);}
            else{float f=(t-.23f)/.77f,a=f*Mathf.PI*3.4f;p=new Vector3(.42f+Mathf.Sin(a)*(.23f-f*.10f),.018f,.36f+Mathf.Cos(a)*.20f);}
            k.Cylinder(rubber,previous,p,.009f,8);previous=p;
        }
        var handle=new Vector3(.67f,.032f,.38f);k.Cylinder(rubber,previous,handle,.009f,8);
        k.Cylinder(brass,handle,handle+new Vector3(.21f,0,0),.022f,16);
        k.Cylinder(rubber,handle+new Vector3(.025f,0,0),handle+new Vector3(.14f,0,0),.026f,16);
        k.Cylinder(brass,handle+new Vector3(.21f,0,0),handle+new Vector3(.29f,0,-.07f),.012f,12);
        k.Cylinder(StoreKit.DeviceBlack,handle+new Vector3(.29f,0,-.07f),handle+new Vector3(.295f,0,-.075f),.008f,12);
        foreach(float x in new[]{.12f,.18f})k.Cylinder(iron,handle+new Vector3(x,0,0),handle+new Vector3(x,.038f,0),.014f,10);
        k.Emit("CylinderHoseAndTorch",group,Vector3.zero,Quaternion.identity,Folder,"rebuilt_cutting_torch");
        var collider=group.gameObject.AddComponent<CapsuleCollider>();collider.radius=.145f;collider.height=.78f;collider.center=new Vector3(0,.39f,0);
        StoreKit.MarkStatic(group.gameObject,true);
    }
    static Material UVMaterial(string name,Texture texture,float intensity)
    {
        var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(UVTraceBuilder.ShaderName));AssetDatabase.CreateAsset(m,path);}
        m.mainTexture=texture;m.SetColor("_GlowColor",new Color(.28f,1,.12f));m.SetFloat("_Intensity",intensity);m.SetFloat("_Visibility",0);m.renderQueue=3030;return m;
    }
    static UVTrace Mark(string name,Transform parent,Vector3 local,Vector3 normal,Vector2 size,Material material,Collider surface)
    {
        var trace=UVTraceBuilder.Add(name,parent,parent.TransformPoint(local),parent.TransformDirection(normal),size,UVTraceBuilder.Mask.Smear);
        trace.GetComponent<Renderer>().sharedMaterial=material;trace.Surface=surface;return trace;
    }
    static void Traces()
    {
        var atlas=DemoAssetLibrary.Texture("SurfaceDecals","Fingerprints_Atlas.png");if(!atlas)throw new Exception("Fingerprint atlas missing");
        var fingerprint=UVMaterial("EvidenceUVFingerprint",atlas,.5f);fingerprint.mainTextureScale=new Vector2(.25f,.5f);fingerprint.mainTextureOffset=new Vector2(.25f,.5f);
        var smear=UVMaterial("EvidenceUVWipedSmear",UVTraceBuilder.MaskTexture(UVTraceBuilder.Mask.Smear),.25f);
        var drops=UVMaterial("EvidenceUVDrop",UVTraceBuilder.MaskTexture(UVTraceBuilder.Mask.Drops),.28f);
        foreach(string side in new[]{"DoorLeft","DoorRight"}){
            var leaf=root.Find("Facade/EntranceDoors/"+side);float sign=side=="DoorLeft"?1:-1;var surface=leaf.GetComponent<Collider>();
            for(int i=0;i<3;i++)Mark("EvidenceUV_Fingerprint_"+i,leaf,new Vector3(sign*(.77f+i*.014f),1.36f+i*.034f,-.033f),Vector3.back,new Vector2(.028f,.042f),fingerprint,surface);
            if(sign>0){Mark("EvidenceUV_WipedGlassSmear",leaf,new Vector3(.76f,.84f,-.033f),Vector3.back,new Vector2(.055f,.16f),smear,surface);Mark("EvidenceUV_GlassDrop",leaf,new Vector3(.69f,.55f,-.033f),Vector3.back,new Vector2(.045f,.07f),drops,surface);}
        }
        var office=root.Find("Office/OfficeDoor/Door");Mark("EvidenceUV_StaffDoorPrint",office,new Vector3(.70f,1.02f,-.044f),Vector3.back,new Vector2(.03f,.045f),fingerprint,office.GetComponent<Collider>());
    }
    static void Audio()
    {
        var bank=Object.FindFirstObjectByType<DemoSoundBank>();
        if(bank){bank.CameraClick=DemoAssetLibrary.Audio("RealFoley","CameraShutter_01.wav");bank.CameraClickVolume=.35f;bank.FlashlightSwitch=DemoAssetLibrary.Audio("RealFoley2","TorchClick_01.wav");bank.FlashlightSwitchVolume=.35f;}
        var go=new GameObject("QuietOfficeClock");go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(0,2.2f,10.1f);
        var source=go.AddComponent<AudioSource>();source.clip=DemoAssetLibrary.Audio("RealFoley2","ClockTick_Quartz_Loop.ogg");source.volume=.035f;source.spatialBlend=1;source.minDistance=.4f;source.maxDistance=4;source.loop=true;source.playOnAwake=true;source.dopplerLevel=0;
    }
    static void Validate()
    {
        if(root.Find("Exterior/Buildings/StreetContinuationBuildings").childCount!=8)throw new Exception("Street continuation missing");
        if(root.Find("Dressing/TorchSet").gameObject.activeSelf)throw new Exception("Old cutting torch still active");
        if(root.GetComponentsInChildren<UVTrace>(true).Count(t=>t.name.StartsWith("EvidenceUV_"))!=9)throw new Exception("Authored UV traces missing");
        foreach(var r in root.Find("SelectedAmbientProps").GetComponentsInChildren<MeshRenderer>())if(r.bounds.size.y>2.4f)throw new Exception("Oversized selected prop "+r.name);
        Debug.Log("URBAN_EDIT_CHECKS_PASS eight facade extensions, three fire escapes, selected props bounded, rebuilt torch, nine attached UV marks");
    }
}
#endif
