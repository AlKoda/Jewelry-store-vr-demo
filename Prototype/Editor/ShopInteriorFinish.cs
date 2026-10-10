#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;

// A restrained, texture-free first material pass; generated assets are persistent.
public static class ShopInteriorFinish
{
    private const string Folder="Assets/CrimeSceneDemo/InteriorGenerated";
    private static Material plaster, ceiling, grout, trim, metal, timber, lamp;
    private static Transform root;

    public static void ApplyShell()
    {
        Transform store=GameObject.Find("JewelryStore_Blockout").transform;
        if(store.Find("InteriorFinish")!=null) throw new InvalidOperationException("Interior finish already applied.");
        EnsureFolder(Folder);
        plaster=Mat(Folder,"WarmPlaster",new Color(.77f,.75f,.69f),.07f);
        ceiling=Mat(Folder,"CeilingPaint",new Color(.86f,.85f,.80f),.04f);
        grout=Mat(Folder,"StoneGrout",new Color(.47f,.46f,.42f),.05f);
        trim=Mat(Folder,"PaintedMetal",new Color(.22f,.25f,.25f),.22f);
        metal=Mat(Folder,"SatinBronze",new Color(.58f,.46f,.27f),.36f);
        metal.SetFloat("_Metallic",.45f);
        timber=Mat(Folder,"DoorTimber",new Color(.31f,.24f,.18f),.18f);
        lamp=Glowing(Mat(Folder,"OpalDiffuser",new Color(.90f,.87f,.78f),.12f),new Color(1,.94f,.82f)*.65f);
        root=Group("InteriorFinish",store);
        foreach(MeshRenderer r in store.Find("Architecture").GetComponentsInChildren<MeshRenderer>())
        {
            if(r.name.Contains("Ceiling")) r.sharedMaterial=ceiling;
            else if(r.name.Contains("Floor")) r.sharedMaterial=grout;
            else if(r.name.Contains("Trim")) r.sharedMaterial=metal;
            else r.sharedMaterial=plaster;
        }
        foreach(MeshRenderer r in store.Find("ShopDetails").GetComponentsInChildren<MeshRenderer>())
        {
            if(r.name=="FloorJoint") r.enabled=false;
            else if(r.name=="CeilingLight") r.sharedMaterial=lamp;
            else if(r.name=="Skirting" || r.name=="CeilingTrack") r.sharedMaterial=trim;
            else if(r.name=="WallCornice") r.sharedMaterial=metal;
        }
        foreach(MeshRenderer r in store.Find("PresentationDetails").GetComponentsInChildren<MeshRenderer>())
            if(r.name.StartsWith("Door") && r.name!="DoorGlass") r.sharedMaterial=r.name=="DoorPushBar"?metal:trim;
        Floor();
        Doors();
        ConfigureLighting();
        AssetDatabase.SaveAssets();
    }

    private static void Floor()
    {
        Transform floor=Group("StoneTiles",root);
        for(int tone=0;tone<4;tone++)
        {
            float delta=(tone-1.5f)*.012f;
            Material tile=Mat(Folder,"StoneTile_"+tone,new Color(.73f+delta,.71f+delta,.65f+delta),.18f);
            List<Vector3> vertices=new List<Vector3>();List<int> indices=new List<int>();
            for(int x=0;x<10;x++) for(int z=0;z<8;z++)
                if((x*13+z*7)%4==tone) Tile(vertices,indices,x-5,z,1,1);
            for(int x=0;x<3;x++) for(int z=0;z<3;z++)
                if((x+z*3)%4==tone) Tile(vertices,indices,x+1,z+8.1f,1,1);
            Mesh mesh=new Mesh {name="StoneTiles_"+tone,vertices=vertices.ToArray(),triangles=indices.ToArray()};
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            SaveMesh(ref mesh,Folder+"/StoneTiles_"+tone+".asset");
            ShopFormRefinement.Shape(mesh.name,floor,mesh,Vector3.zero,Vector3.one,tile);
        }
    }

    private static void Tile(List<Vector3> vertices,List<int> indices,float x,float z,float width,float depth)
    {
        // Thin visual skin; original floor collider stays at y=0. Evidence starts at y=.009.
        const float gap=.003f,y=.004f;
        int n=vertices.Count;
        vertices.Add(V(x+gap,y,z+gap));vertices.Add(V(x+gap,y,z+depth-gap));
        vertices.Add(V(x+width-gap,y,z+depth-gap));vertices.Add(V(x+width-gap,y,z+gap));
        indices.Add(n);indices.Add(n+1);indices.Add(n+2);
        indices.Add(n);indices.Add(n+2);indices.Add(n+3);
    }

    private static void Doors()
    {
        Transform hardware=Group("EntranceHardware",root);
        Box("DoorKickPlate",hardware,V(3.8f,.14f,.004f),V(1.43f,.20f,.025f),trim);
        Box("CloserBody",hardware,V(4.20f,2.37f,.02f),V(.25f,.065f,.06f),trim);
        Box("CloserArm",hardware,V(4.04f,2.40f,.06f),V(.25f,.018f,.018f),metal);
        for(int i=0;i<3;i++)
            Box("EntranceHinge",hardware,V(4.56f,.42f+i*.78f,.014f),V(.055f,.105f,.075f),metal);
        CombineByMaterial(hardware,Folder+"/EntranceHardware_","EntranceHardware_");

        // Hinge sits at the left edge of the 1.4 m opening; leaf parks 90 degrees into the safe room.
        Transform door=Group("StaffDoorOpen",root);
        door.localPosition=V(1.80f,0,8.10f);
        Box("Leaf",door,V(0,1.10f,.65f),V(.045f,2.20f,1.30f),timber,true);
        Transform details=Group("Hardware",door);
        for(int side=-1;side<=1;side+=2)
        {
            Box("InsetPanel",details,V(side*.025f,1.18f,.65f),V(.009f,1.75f,1.06f),plaster);
            Box("HandlePlate",details,V(side*.035f,1.04f,1.14f),V(.018f,.18f,.055f),trim);
            Box("Lever",details,V(side*.075f,1.06f,1.06f),V(.075f,.024f,.17f),metal);
            Box("KickPlate",details,V(side*.027f,.16f,.65f),V(.012f,.20f,1.15f),trim);
        }
        CombineByMaterial(details,Folder+"/StaffHardware_","StaffHardware_");
    }

    public static void ConfigureLighting()
    {
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.64f,.68f,.72f);
        RenderSettings.ambientEquatorColor=new Color(.50f,.49f,.45f);
        RenderSettings.ambientGroundColor=new Color(.29f,.27f,.23f);
        foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            l.shadows=LightShadows.None;
            if(l.type==LightType.Directional) {l.intensity=.7f;l.color=new Color(1,.97f,.91f);}
            else if(l.name=="WarmDisplayLight")
            {
                l.intensity=.8f;l.range=6;l.color=new Color(1,.94f,.83f);
                l.renderMode=LightRenderMode.ForceVertex;
            }
        }
    }

    public static void CheckBudget()
    {
        Transform finish=GameObject.Find("JewelryStore_Blockout").transform.Find("InteriorFinish");
        int triangles=0;
        MeshFilter[] meshes=finish.GetComponentsInChildren<MeshFilter>();
        foreach(MeshFilter f in meshes) triangles+=f.sharedMesh.triangles.Length/3;
        DemoValidation.Check(triangles<1500 && meshes.Length<=16,"Interior shell fits 1500-triangle / 16-renderer budget");
        DemoValidation.Check(finish.GetComponentsInChildren<Collider>().Length==1,"Only the fixed-open staff door adds collision");
        DemoValidation.Check(finish.GetComponentsInChildren<Light>().Length==0,"Interior finish adds no realtime lights");
        DemoValidation.Check(finish.Find("StaffDoorOpen/Leaf").GetComponent<BoxCollider>()!=null,"Staff door is solid and fixed open");
        bool cheapLights=true;int lights=0;
        foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {lights++;cheapLights &= l.shadows==LightShadows.None;}
        DemoValidation.Check(cheapLights && lights<=4,"Lighting uses at most four shadowless lights");
        DemoValidation.Info("Interior finish triangles="+triangles+"; renderers="+meshes.Length+"; lights="+lights);
    }
}
#endif
