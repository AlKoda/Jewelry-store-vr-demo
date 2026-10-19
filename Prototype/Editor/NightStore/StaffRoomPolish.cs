#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;
using Object=UnityEngine.Object;

// Retail work/security room and a modest staff break room; no car edits.
public static class StaffRoomPolish
{
    const string Folder="Assets/CrimeSceneDemo/CombinedArt";
    static Transform root,staff;
    static GameObject fridge;
    public const int TraceCount=44;
    public static void Apply()
    {
        root=GameObject.Find("NightStore").transform;staff=Group("StaffRoomFurnishings",root);
        var steel=Mat(Folder,"StaffPaintedSteel",new Color(.19f,.20f,.21f),.22f);steel.SetFloat("_Metallic",.2f);
        var bench=Table("StaffToolBench",new Vector3(-6,0,9.3f),1.85f,.65f,.78f,steel);
        ClearOutside(bench.transform);
        Prop("AmbientProps","NetworkServerRackRow",new Vector3(-4.8f,0,11.25f),90);
        fridge=Prop("InteriorModels","MiniFridge",new Vector3(7.53f,0,9.48f),90);
        Prop("InteriorModels","CoffeeMachine",new Vector3(7.53f,.84f,9.48f),90);
        Prop("InteriorModels","WaterCooler",new Vector3(7.55f,0,10.43f),90);
        var table=Table("StaffBreakTable",new Vector3(5.5f,0,10.4f),.95f,.65f,.74f,steel);
        Prop("AmbientProps2","MonoblocChair",new Vector3(4.65f,0,10.4f),-90);
        var chair=Prop("AmbientProps2","MonoblocChair",new Vector3(5.5f,0,9.55f),180);chair.name="MonoblocChair_Second";
        Prop("InteriorModels","OfficeChair",new Vector3(-1.6f,0,10.15f),0);
        Prop("AmbientProps2","FoodPackets",new Vector3(5.5f,.74f,10.43f),0,.72f);
        // Painted and worn metal keeps broad reflections, without polished highlights.
        foreach(string name in new[]{"WornMetalRack","MetalStool","StreetPlanterBox"})
            foreach(Transform t in root.Find("SelectedAmbientProps"))if(t.name.StartsWith(name))Dull(t.gameObject);
        Dull(root.Find("Dressing/RebuiltCuttingTorch").gameObject);
        MorePrints();FloorDrops();Physics.SyncTransforms();Validate();
    }
    static GameObject Table(string name,Vector3 position,float width,float depth,float height,Material steel)
    {
        var group=Group(name,staff);group.localPosition=position;var k=new KitMesh();
        k.Box(StoreKit.Walnut,new Vector3(0,height-.02f,0),new Vector3(width,.04f,depth));
        foreach(float x in new[]{-width*.5f+.08f,width*.5f-.08f})foreach(float z in new[]{-depth*.5f+.07f,depth*.5f-.07f})
            k.Box(steel,new Vector3(x,(height-.04f)*.5f,z),new Vector3(.045f,height-.04f,.045f));
        k.Box(steel,new Vector3(0,.20f,depth*.5f-.07f),new Vector3(width-.12f,.03f,.035f));
        k.Emit("Body",group,Vector3.zero,Quaternion.identity,Folder,name.ToLowerInvariant());
        var collider=group.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,height*.5f,0);collider.size=new Vector3(width,height,depth);
        StoreKit.MarkStatic(group.gameObject,true);return group.gameObject;
    }
    static GameObject Prop(string pack,string name,Vector3 p,float yaw,float scale=1)
    {
        var go=UrbanDetailPass.Prop(pack,name,staff,p,yaw,scale);
        if(p.y<.1f)DemoAssetLibrary.AddBoundsCollider(go);
        foreach(var r in go.GetComponentsInChildren<MeshRenderer>())GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ReflectionProbeStatic);
        if(name=="NetworkServerRackRow"||name=="OfficeChair"||name=="MiniFridge"||name=="CoffeeMachine")Dull(go);
        return go;
    }
    static void Dull(GameObject go)
    {
        foreach(var r in go.GetComponentsInChildren<MeshRenderer>()){
            var materials=r.sharedMaterials;
            for(int i=0;i<materials.Length;i++){
                var source=materials[i];if(!source||source.shader.name.Contains("Glass"))continue;
                string name=source.name.StartsWith("Matte_")?source.name:"Matte_"+source.name;
                string path=Folder+"/"+name+".mat";
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!m){m=new Material(source){name=name};AssetDatabase.CreateAsset(m,path);}
                else if(m!=source)EditorUtility.CopySerialized(source,m);
                if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",Mathf.Min(m.GetFloat("_Glossiness"),.27f));
                if(m.HasProperty("_GlossMapScale"))m.SetFloat("_GlossMapScale",Mathf.Min(m.GetFloat("_GlossMapScale"),.27f));
                m.name=name;EditorUtility.SetDirty(m);materials[i]=m;
            }
            r.sharedMaterials=materials;
        }
    }
    static void ClearOutside(Transform bench)
    {
        root.Find("Exterior/Equipment").gameObject.SetActive(false);
        foreach(string name in new[]{"NitrileGloveBox","ForensicCase"}){
            var leftover=root.Find("Exterior/"+NightStreet.GroupName+"/"+name);
            if(leftover)leftover.gameObject.SetActive(false);
        }
        var tools=root.Find("ToolSystem");
        tools.Find("PhotoFrame").gameObject.SetActive(false);tools.Find("StatusBoard").gameObject.SetActive(false);
        Vector3 delta=new Vector3(-6,.04f,9.3f)-NightStoreLayout.Table;
        foreach(var sample in tools.GetComponentsInChildren<ToolRackSample>()){
            sample.transform.position+=delta;
            if(sample.Kind==DemoToolKind.Lamp){var p=sample.transform.position;p.y=0;sample.transform.position=p;}
        }
        tools.Find("HandheldCamera").localPosition=new Vector3(-6,.98f,9.53f);
        tools.Find("SpawnPoint").localPosition=new Vector3(-6,.85f,9.3f);
    }
    static Material PrintMaterial(int index)
    {
        var path=Folder+"/BackgroundUVPrint_"+index+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find(UVTraceBuilder.ShaderName));AssetDatabase.CreateAsset(m,path);}
        m.mainTexture=DemoAssetLibrary.Texture("SurfaceDecals","Fingerprints_Atlas.png");m.mainTextureScale=new Vector2(.25f,.5f);m.mainTextureOffset=new Vector2(index*.25f,.5f);
        m.SetColor("_GlowColor",new Color(.28f,1,.12f));m.SetFloat("_Intensity",.32f+index*.045f);m.SetFloat("_Visibility",0);m.renderQueue=3030;return m;
    }
    static UVTrace Print(Transform target,Vector3 local,Vector3 localNormal,int index)
    {
        Vector3 n=target.TransformDirection(localNormal).normalized,guess=target.TransformPoint(local);
        var ray=new Ray(guess+n*.16f,-n);float nearest=float.PositiveInfinity;
        foreach(var f in target.GetComponentsInChildren<MeshFilter>()){
            var r=f.GetComponent<Renderer>();if(!r||!r.enabled||!r.gameObject.activeInHierarchy||r.GetComponent<UVTrace>())continue;
            if(DemoValidation.TriangleRay(ray,.4f,DemoValidation.NewRayMesh(f),out float hit))nearest=Mathf.Min(nearest,hit);
        }
        if(float.IsPositiveInfinity(nearest))throw new Exception("No actual touch surface for print on "+target.name+" at "+local);
        var point=ray.GetPoint(nearest);var t=UVTraceBuilder.Add("EvidenceUV_BackgroundPrint_"+index,target,point,n,new Vector2(.025f+(index%3)*.002f,.037f+(index%2)*.003f),UVTraceBuilder.Mask.Smear,(index*31)%80-40);
        t.GetComponent<Renderer>().sharedMaterial=PrintMaterial(index%4);t.Surface=target.GetComponent<Collider>();return t;
    }
    static void MorePrints()
    {
        int index=0;
        foreach(string name in new[]{"CentralCase_Left","CentralCase_Middle","CentralCase_Right","LeftIsland_Front","LeftIsland_Rear","RightIsland_Rear"}){
            var target=root.Find("Showroom/"+name);
            for(int i=0;i<3;i++)Print(target,new Vector3((index%2==0?-.4f:.14f)+i*.026f,1.05f,-.18f),Vector3.up,index++);
        }
        for(int i=0;i<4;i++)Print(root.Find("Showroom/Counter"),new Vector3(-.55f+i*.035f,.95f,-.18f),Vector3.up,index++);
        for(int i=0;i<3;i++)Print(root.Find("Office/Desk"),new Vector3(.32f+i*.028f,.76f,-.12f),Vector3.up,index++);
        for(int i=0;i<2;i++)Print(fridge.transform,new Vector3(.12f,.61f+i*.043f,-.28f),Vector3.back,index++);
        for(int i=0;i<2;i++)Print(root.Find("Office/ServiceDoor/Door"),new Vector3(.65f+i*.029f,1.08f,-.045f),Vector3.back,index++);
        if(index!=29)throw new Exception("Distributed print authoring count incorrect");
    }
    static void FloorDrops()
    {
        var group=Group("FloorBloodTraces",root.Find("Dressing"));
        var mask=UVTraceBuilder.MaskTexture(UVTraceBuilder.Mask.Drops);
        var path=Folder+"/DryBloodFloor.mat";var red=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!red){red=new Material(Shader.Find("CrimeScene/SurfaceDecal"));AssetDatabase.CreateAsset(red,path);}
        red.mainTexture=mask;red.color=new Color(.21f,.032f,.018f,.88f);red.SetFloat("_Glossiness",.12f);red.SetFloat("_Metallic",0);red.renderQueue=2455;
        var uv=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/EvidenceUVDrop.mat");
        Vector3[] positions={new Vector3(.75f,.003f,1.35f),new Vector3(.95f,.003f,2.4f),new Vector3(.8f,.003f,3.65f),new Vector3(.2f,.003f,6.55f),new Vector3(-.35f,.003f,8.7f),new Vector3(-.45f,.003f,9.65f)};
        for(int i=0;i<positions.Length;i++){
            float size=.09f+(i%3)*.026f;var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name="SmallDriedBloodDrops_"+i;g.transform.SetParent(group,false);g.transform.localPosition=positions[i];g.transform.localRotation=Quaternion.Euler(90,i*61,0);g.transform.localScale=new Vector3(size,size,1);Object.DestroyImmediate(g.GetComponent<Collider>());
            var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=red;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;
            var t=UVTraceBuilder.Add("EvidenceUV_FloorDrops_"+i,group,positions[i]+Vector3.up*.001f,Vector3.up,new Vector2(size,size),UVTraceBuilder.Mask.Drops,i*61);t.GetComponent<Renderer>().sharedMaterial=uv;g.transform.rotation=t.transform.rotation;
        }
        var state=Object.FindFirstObjectByType<CrimeSceneState>();if(state)state.RobbedOnly=state.RobbedOnly.Concat(new[]{group.gameObject}).ToArray();
    }
    static void Validate()
    {
        if(root.Find("Exterior/Equipment").gameObject.activeSelf||root.Find("ToolSystem/PhotoFrame").gameObject.activeSelf||root.Find("ToolSystem/StatusBoard").gameObject.activeSelf)throw new Exception("Outdoor presentation equipment still visible");
        if(root.GetComponentsInChildren<UVTrace>(true).Count(t=>t.name.StartsWith("EvidenceUV_"))!=TraceCount)throw new Exception("Distributed evidence count incorrect");
        foreach(string name in new[]{"NitrileGloveBox","ForensicCase"}){
            var leftover=root.Find("Exterior/"+NightStreet.GroupName+"/"+name);
            if(leftover&&leftover.gameObject.activeInHierarchy)throw new Exception("Outdoor table remnant still active: "+name);
        }
        foreach(var sample in root.GetComponentsInChildren<ToolRackSample>())if(sample.transform.position.z<8.5f)throw new Exception("Tool sample left outside");
        foreach(var collider in staff.GetComponentsInChildren<Collider>())if(collider.bounds.min.x< -7.95f||collider.bounds.max.x>7.95f||collider.bounds.max.z>11.97f)throw new Exception("Staff furnishing intersects exterior wall: "+collider.name+" "+collider.bounds);
        Debug.Log("STAFF_ROOM_POLISH_PASS outdoor equipment removed, tools moved indoors, rougher metal, furnished staff rooms, 44 UV marks and six small floor blood clusters");
    }
}
#endif
