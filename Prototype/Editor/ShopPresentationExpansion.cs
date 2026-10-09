#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;

public static class ShopPresentationExpansion
{
    private const string Folder="Assets/CrimeSceneDemo/PresentationGenerated";
    private static Material wood, brass, ivory, charcoal, glass, red, orange, yellow, darkGlass;
    private static Transform details;
    private static int serial;

    public static void Apply()
    {
        EnsureFolder(Folder);
        wood=Mat("Walnut",new Color(.22f,.13f,.08f));
        brass=Mat("SatinGold",new Color(.62f,.45f,.20f));
        ivory=Mat("Linen",new Color(.82f,.77f,.63f));
        charcoal=Mat("Graphite",new Color(.10f,.12f,.13f));
        red=Mat("Red",new Color(.55f,.08f,.05f));
        orange=Mat("ConeOrange",new Color(.95f,.29f,.03f));
        yellow=Mat("EvidenceYellow",new Color(.95f,.75f,.08f));
        darkGlass=Mat("Screen",new Color(.05f,.11f,.14f));
        glass=Mat("RemainingGlass",new Color(.62f,.83f,.87f,.19f));
        glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",(int)BlendMode.One);
        glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);
        glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.renderQueue=3000;
        Transform store=GameObject.Find("JewelryStore_Blockout").transform;
        if(store.Find("PresentationDetails")!=null) throw new System.InvalidOperationException("Already expanded.");
        details=Group("PresentationDetails",store);
        serial=0;

        foreach(Transform furniture in store.Find("Furniture"))
        {
            if(!furniture.name.Contains("Island") && !furniture.name.Contains("WallDisplay")) continue;
            bool island=furniture.name.Contains("Island");
            Vector3 p=furniture.Find("Base").localPosition; p.y=0;
            float width=island?1.2f:.8f,depth=island?2.6f:3.2f;
            Box("WalnutPlinth",details,p+V(0,.075f,0),V(width+.04f,.15f,depth+.04f),wood);
            for(int i=-1;i<=1;i++)
            {
                Box("DrawerFront",details,p+V(0,.46f,i*depth*.29f),V(width+.02f,.39f,depth*.27f),wood);
                Box("DrawerHandle",details,p+V(width*.5f+.025f,.5f,i*depth*.29f),V(.035f,.025f,.24f),brass);
            }
            if(furniture.name=="Island_Right")
            {
                Box("IntactCaseTop",details,p+V(0,1.21f,0),V(width-.06f,.018f,depth-.06f),glass).AddComponent<BoxCollider>();
                Box("IntactCaseSide",details,p+V(-width*.5f+.04f,1.02f,0),V(.014f,.34f,depth-.08f),glass);
            }
            if(furniture.name=="Island_Left")
            {
                // Small lid fragments around a large missing section.
                JaggedPanel(p+V(-.49f,1.21f,0),V(.18f,1,1.08f),Quaternion.identity);
                JaggedPanel(p+V(.49f,1.21f,.4f),V(.18f,1,.55f),Quaternion.Euler(0,180,0));
                Box("FallenCaseTrim",details,p+V(.72f,.035f,.3f),V(.035f,.025f,.75f),brass)
                    .transform.localRotation=Quaternion.Euler(0,24,0);
            }
        }

        // Jagged remaining glazing mounted in the storefront plane.
        JaggedPanel(V(-3.95f,1.55f,-.08f),V(.9f,1,1.0f),Quaternion.Euler(90,0,0));
        JaggedPanel(V(2.1f,1.55f,-.08f),V(.7f,1,.95f),Quaternion.Euler(90,0,180));
        // Front door frame reads as a closed presentation boundary.
        Box("DoorLeftFrame",details,V(3.03f,1.25f,-.05f),V(.05f,2.5f,.08f),charcoal);
        Box("DoorRightFrame",details,V(4.57f,1.25f,-.05f),V(.05f,2.5f,.08f),charcoal);
        Box("DoorTopFrame",details,V(3.8f,2.48f,-.05f),V(1.6f,.05f,.08f),charcoal);
        Box("DoorPushBar",details,V(3.8f,1.02f,.01f),V(1.3f,.05f,.05f),brass);
        Box("DoorGlass",details,V(3.8f,1.24f,-.04f),V(1.48f,2.39f,.014f),glass);

        // Counter detail and office equipment.
        Box("CounterTop",details,V(-1.8f,1.13f,6.5f),V(3.95f,.065f,.88f),ivory).AddComponent<BoxCollider>();
        Box("POSBase",details,V(-2.8f,1.20f,6.4f),V(.26f,.08f,.20f),charcoal);
        Box("POSStem",details,V(-2.8f,1.36f,6.44f),V(.05f,.3f,.05f),charcoal);
        Box("POSScreen",details,V(-2.8f,1.52f,6.43f),V(.42f,.28f,.045f),darkGlass)
            .transform.localRotation=Quaternion.Euler(12,0,0);
        Box("ReceiptPrinter",details,V(-2.32f,1.24f,6.47f),V(.18f,.18f,.22f),charcoal);
        Box("OpenLedger",details,V(-.7f,1.18f,6.42f),V(.42f,.02f,.28f),ivory);
        for(int i=0;i<4;i++)
            Box("GiftBox",details,V(-3.4f+i*.24f,1.22f,6.68f),V(.18f,.12f,.13f),i%2==0?wood:ivory);
        Box("SafeRoomSign",details,V(3.78f,2.05f,7.98f),V(.6f,.25f,.02f),charcoal);
        Label("STAFF",V(3.78f,2.05f,7.96f),0,.035f,ivory.color);

        // Closed circuit camera and small alarm hardware, static props.
        Box("CCTVBracket",details,V(-4.83f,2.55f,7.7f),V(.25f,.06f,.08f),charcoal);
        Box("CCTVBody",details,V(-4.66f,2.49f,7.65f),V(.14f,.13f,.28f),ivory)
            .transform.localRotation=Quaternion.Euler(20,-30,0);
        Box("AlarmBox",details,V(4.97f,2.35f,1),V(.04f,.18f,.25f),red);

        // Environmental evidence remains stationary; no automatic interpretation.
        Box("DroppedJewelryTray",details,V(-.50f,.04f,5.35f),V(.32f,.06f,.28f),wood)
            .transform.localRotation=Quaternion.Euler(0,25,0);
        Box("TrayInsert",details,V(-.50f,.075f,5.35f),V(.27f,.015f,.23f),ivory)
            .transform.localRotation=Quaternion.Euler(0,25,0);
        Transform hinge=store.Find("Furniture/OpenSafe/DoorHinge");
        for(int i=0;i<7;i++)
            Box("DarkenedDoorEdge",hinge,V(1.26f,.35f+i*.14f,-.075f),V(.055f,.09f,.007f),charcoal);
        Box("SafeRoomShelf",details,V(3.97f,1.65f,10),V(.18f,.08f,1.4f),wood);
        for(int i=0;i<3;i++) Box("RecordsBox",details,V(3.93f,1.84f,9.6f+i*.34f),V(.18f,.3f,.25f),ivory);

        for(int i=0;i<3;i++)
        {
            GameObject light=new GameObject("WarmDisplayLight");
            light.transform.SetParent(details,false);light.transform.localPosition=V(-3+i*3,2.65f,4);
            Light l=light.AddComponent<Light>();l.type=LightType.Point;
            l.color=new Color(1,.88f,.69f);l.intensity=.55f;l.range=5;
            l.shadows=LightShadows.None;
        }
        StyleTools();
        StreetDetails();
        QualitySettings.antiAliasing=4;
        AssetDatabase.SaveAssets();
    }

    private static void StyleTools()
    {
        ToolStation station=Object.FindFirstObjectByType<ToolStation>();
        foreach(DeployedTool prefab in new []{station.ConePrefab,station.MarkerPrefab,station.TapePostPrefab})
        {
            string path=AssetDatabase.GetAssetPath(prefab);
            GameObject contents=PrefabUtility.LoadPrefabContents(path);
            foreach(MeshRenderer r in contents.GetComponentsInChildren<MeshRenderer>())
            {
                TextMesh text=r.GetComponent<TextMesh>();
                if(text!=null)
                {
                    text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    r.sharedMaterial=text.font.material;text.color=Color.black;
                }
                else if(r.sharedMaterial!=null && r.sharedMaterial.mainTexture!=null) continue;
                else r.sharedMaterial=prefab.Kind==DemoToolKind.Cone?orange:
                    prefab.Kind==DemoToolKind.Marker?yellow:charcoal;
            }
            PrefabUtility.SaveAsPrefabAsset(contents,path);PrefabUtility.UnloadPrefabContents(contents);
        }
        string tapePath=AssetDatabase.GetAssetPath(station.TapePrefab);
        GameObject tape=PrefabUtility.LoadPrefabContents(tapePath);
        tape.GetComponentInChildren<MeshRenderer>().sharedMaterial=yellow;
        PrefabUtility.SaveAsPrefabAsset(tape,tapePath);PrefabUtility.UnloadPrefabContents(tape);
    }

    private static void StreetDetails()
    {
        Transform street=Group("StreetDetails",null);
        Box("BusStopRoof",street,V(-11,2.4f,-12.6f),V(3,.12f,1.3f),charcoal);
        Box("BusStopBack",street,V(-11,1.3f,-13.15f),V(2.9f,2.2f,.08f),darkGlass);
        for(int i=-1;i<=1;i+=2)
            Box("BusStopPost",street,V(-11+i*1.4f,1.2f,-12.2f),V(.06f,2.4f,.06f),charcoal);
        Box("BusStopSeat",street,V(-11,.48f,-12.8f),V(2.5f,.08f,.4f),wood);
        for(int i=-1;i<=1;i+=2)
        {
            Box("TrafficSignalPost",street,V(12+i*2,1.7f,-11.4f),V(.09f,3.4f,.09f),charcoal);
            Box("TrafficSignal",street,V(12+i*2,3,-11.4f),V(.22f,.6f,.18f),charcoal);
            Box("RedSignal",street,V(12+i*2,3.18f,-11.29f),V(.12f,.12f,.015f),red);
        }
        for(int i=0;i<3;i++)
        {
            Box("LitterBin",street,V(-17+i*15,.45f,-12),V(.45f,.9f,.45f),charcoal);
            for(int j=0;j<5;j++)
                Box("DrainGrille",street,V(-16+i*15+j*.07f,-.025f,-10.8f),V(.03f,.009f,.3f),charcoal);
        }
        // Small additions use a handful of combined meshes, not individual draw calls.
        CombineByMaterial(street,Folder+"/StreetDetail_","StreetDetail_");
    }

    private static void JaggedPanel(Vector3 position,Vector3 scale,Quaternion rotation)
    {
        // A narrow edge strip with an irregular torn boundary, visible from both sides.
        Vector3[] outline={V(-.5f,0,-1),V(.25f,0,-1),V(.1f,0,-.65f),V(.43f,0,-.38f),
            V(.08f,0,-.1f),V(.37f,0,.18f),V(.05f,0,.5f),V(.28f,0,.75f),V(-.05f,0,1),V(-.5f,0,1)};
        List<Vector3> vertices=new List<Vector3>();List<int> triangles=new List<int>();
        // Fan from the solid left edge; reverse faces use separate vertices.
        for(int i=1;i<outline.Length-1;i++)
        {
            int start=vertices.Count;
            vertices.Add(outline[0]);vertices.Add(outline[i]);vertices.Add(outline[i+1]);
            vertices.Add(outline[0]);vertices.Add(outline[i+1]);vertices.Add(outline[i]);
            for(int j=0;j<6;j++) triangles.Add(start+j);
        }
        Mesh mesh=new Mesh {vertices=vertices.ToArray(),triangles=triangles.ToArray()};
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        SaveMesh(ref mesh,Folder+"/BrokenPanel_"+serial+++".asset");
        Transform g=Group("JaggedGlassRemnant",details);g.localPosition=position;
        g.localScale=scale;g.localRotation=rotation;
        g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=glass;
    }
    private static Material Mat(string name,Color color) => DemoGeometry.Mat(Folder,name,color,.18f);
    private static void Label(string text,Vector3 p,float yaw,float size,Color color)
        => Text(text,details,text,p,yaw,size,color);
}
#endif
