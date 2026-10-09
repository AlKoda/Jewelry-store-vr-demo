#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

public static class ShopCityRefinement
{
    private const string Folder="Assets/CrimeSceneDemo/CityGenerated";
    private const string Pack="KenneyCityBuilder";
    private static Material stone, dark, metal, cream, road, glass, green, brick, white, glow;
    private static Transform city;

    [MenuItem("Crime Scene Demo/Refine Shop And Add Street")]
    public static void Apply()
    {
        GameObject store=GameObject.Find("JewelryStore_Blockout");
        if(store==null) throw new System.InvalidOperationException("Generate the store first.");
        if(GameObject.Find("CityBackdrop")!=null)
            throw new System.InvalidOperationException("City already exists. Generate into a fresh scene.");
        EnsureFolder(Folder);
        stone=Mat("WarmStone",new Color(.64f,.61f,.54f));
        cream=Mat("Ivory",new Color(.84f,.81f,.72f));
        dark=Mat("CharcoalWood",new Color(.16f,.19f,.19f));
        metal=Mat("BrushedBrass",new Color(.48f,.36f,.19f));
        road=Mat("Asphalt",new Color(.18f,.20f,.22f));
        glass=Mat("OpaqueWindow",new Color(.19f,.30f,.35f));
        green=Mat("Foliage",new Color(.21f,.30f,.23f));
        brick=Mat("Terracotta",new Color(.48f,.29f,.22f));
        white=Mat("RoadMarking",new Color(.83f,.84f,.80f));
        glow=Glowing(Mat("LampGlow",new Color(.95f,.92f,.82f)),new Color(1f,.93f,.75f)*1.4f);
        foreach(Renderer r in store.GetComponentsInChildren<Renderer>())
        {
            string n=r.name.ToLowerInvariant();
            Material m=cream;
            if(n.Contains("floor")) m=stone;
            if(n=="base" || n.Contains("counter") || n.Contains("safe") || n=="door" ||
                n=="back" || n=="left" || n=="right" || n.Contains("toolrack")) m=dark;
            if(n.Contains("rail") || n.Contains("post") || n.Contains("trim") ||
                n.Contains("wheel") || n.Contains("dial") || n.Contains("segment")) m=metal;
            if(n.Contains("shard") || n.Contains("glass")) m=glass;
            if(n.Contains("tread") || n.Contains("heel")) m=dark;
            if(r is MeshRenderer && r.GetComponent<TextMesh>()==null) r.sharedMaterial=m;
        }
        Transform oldPavement=store.transform.Find("Architecture/Pavement");
        if(oldPavement!=null) Object.DestroyImmediate(oldPavement.gameObject);
        Transform detail=Group("ShopDetails",store.transform);
        // Thin geometric joints provide scale without texture maps.
        for(int x=-4;x<5;x++)
            Box("FloorJoint",detail,V(x,.002f,4),V(.008f,.002f,8),dark);
        for(int z=1;z<8;z++)
            Box("FloorJoint",detail,V(0,.002f,z),V(10,.002f,.008f),dark);
        Box("BackFeaturePanel",detail,V(-1.5f,1.8f,7.98f),V(4.4f,1.6f,.035f),dark);
        Text("Brand",detail,"ATELIER",V(-1.5f,2.12f,7.95f),0,.11f,cream.color);
        Text("BrandSubtitle",detail,"F I N E   J E W E L L E R Y",V(-1.5f,1.58f,7.94f),0,.036f,cream.color);
        for(int i=0;i<3;i++)
        {
            Box("CeilingTrack",detail,V(-3+i*3,2.92f,4),V(.07f,.08f,6),dark);
            for(int j=0;j<3;j++)
                Box("CeilingLight",detail,V(-3+i*3,2.84f,1.8f+j*2),V(.26f,.08f,.32f),glow);
        }
        for(int side=-1;side<=1;side+=2)
        {
            Box("Skirting",detail,V(side*4.98f,.10f,4),V(.025f,.2f,8),dark);
            Box("WallCornice",detail,V(side*4.96f,2.65f,4),V(.06f,.08f,8),metal);
        }
        // Collider spans ALL glazing and entrance; backdrop is view-only.
        GameObject barrier=new GameObject("IndoorBoundary_Front");
        barrier.transform.SetParent(store.transform,false);
        barrier.transform.localPosition=V(0,1.5f,-.05f);
        barrier.AddComponent<BoxCollider>().size=V(10.4f,3,.15f);

        city=Group("CityBackdrop",null);
        Box("DistantGround",city,V(0,-.22f,-22),V(180,.2f,160),stone);
        Box("Street",city,V(0,-.09f,-7),V(140,.1f,8),road);
        Box("NearSidewalk",city,V(0,-.10f,-1.5f),V(140,.2f,3),stone);
        Box("FarSidewalk",city,V(0,-.1f,-13),V(140,.2f,4),stone);
        Box("NearKerb",city,V(0,-.015f,-3),V(140,.15f,.16f),cream);
        Box("FarKerb",city,V(0,-.015f,-11),V(140,.15f,.16f),cream);
        for(int i=-16;i<=16;i++)
            Box("LaneDash",city,V(i*4,-.033f,-7),V(1.8f,.005f,.10f),white);
        for(int i=0;i<7;i++)
            Box("Crosswalk",city,V(12,-.03f,-10.3f+i),V(2.4f,.006f,.42f),white);
        string[] signs={"BOOKS","CAFE","STUDIO","BAKERY","FLORIST","DESIGN","MARKET"};
        for(int i=-3;i<=3;i++)
            Building("Across_"+i,V(i*9,0,-19),8,9+(i+3)%3*2,8,
                i%2==0?stone:brick,signs[i+3],0);
        // Side buildings form the continuation of the shop's street frontage.
        Building("Neighbor_Left",V(-12,0,2.7f),12,9,9,brick,"GALLERY",180);
        Building("Neighbor_Right",V(12,0,2.7f),12,12,9,stone,"PHARMACY",180);
        string[] blocks={"building-small-a","building-small-b","building-small-c","building-small-d","building-garage"};
        for(int i=-4;i<=4;i++)
        {
            // Kenney CC0 buildings when the pack is present (1 m tiles scaled up), boxes otherwise.
            if(DemoAssetLibrary.Place(Pack,blocks[(i+4)%blocks.Length],city,V(i*13,0,-40),(i%2==0)?0:180,12)==null)
                Box("DistantBlock",city,V(i*13,10+(i+4)%3*2,-40),V(10,20+(i+4)%3*4,12),stone);
            if(i%2==0) Lamp(V(i*8,0,-11.8f));
        }
        if(DemoAssetLibrary.Place(Pack,"pavement-fountain",city,V(2,-.02f,-13),0,4)!=null)
            Box("FountainPlinth",city,V(2,-.015f,-13),V(4.2f,.03f,4.2f),cream);
        Car(V(-7,-.02f,-9.4f),dark);
        Car(V(10,-.02f,-4.5f),brick);
        Bench(V(-3,0,-12.3f));
        Bench(V(8,0,-12.3f));
        for(int i=0;i<3;i++)
            if(DemoAssetLibrary.Place(Pack,"grass-trees-tall",city,V(-13+i*13,0,-13.2f),i*90,3.5f)==null)
                Planter(V(-13+i*13,0,-13.2f));
        // Consolidate static backdrop by material into persistent mesh assets.
        CombineByMaterial(city,Folder+"/CityMesh_","City_");
        RenderSettings.fog=true;
        RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogColor=new Color(.65f,.72f,.75f);
        RenderSettings.fogStartDistance=35;
        RenderSettings.fogEndDistance=110;
        AssetDatabase.SaveAssets();
    }

    private static void Building(string name,Vector3 p,float width,float height,float depth,
        Material wall,string sign,float facing)
    {
        Transform g=Group(name,city);
        g.localPosition=p; g.localRotation=Quaternion.Euler(0,facing,0);
        Box("Shell",g,V(0,height/2,0),V(width,height,depth),wall);
        float face=depth/2+.04f;
        Box("Plinth",g,V(0,.4f,face),V(width,.8f,.1f),dark);
        Box("Cornice",g,V(0,height-.3f,face),V(width+.25f,.3f,.25f),cream);
        for(int row=0;row<3;row++)
        for(int col=0;col<3;col++)
        {
            float y=4.5f+row*2.7f;
            if(y+1>height) continue;
            float x=(col-1)*width*.28f;
            Box("WindowFrame",g,V(x,y,face+.04f),V(1.55f,1.8f,.1f),cream);
            Box("Window",g,V(x,y,face+.1f),V(1.33f,1.58f,.03f),glass);
            Box("Mullion",g,V(x,y,face+.13f),V(.04f,1.6f,.03f),dark);
        }
        Box("Shopfront",g,V(0,1.4f,face+.05f),V(width-.8f,2.1f,.12f),glass);
        for(int i=-1;i<=1;i++)
            Box("ShopfrontPillar",g,V(i*width*.29f,1.4f,face+.13f),V(.14f,2.3f,.12f),dark);
        Box("Signboard",g,V(0,2.95f,face+.10f),V(width-.4f,.72f,.18f),dark);
        Text("ShopSign",g,sign,V(0,2.98f,face+.21f),180,.09f,cream.color);
        Box("Awning",g,V(0,2.56f,face+.48f),V(width-.3f,.08f,1),wall);
    }

    private static void Car(Vector3 p,Material paint)
    {
        Transform g=Group("ParkedCar",city); g.localPosition=p;
        Box("Body",g,V(0,.55f,0),V(4,.65f,1.65f),paint);
        Box("Cabin",g,V(0,1.05f,0),V(2.1f,.55f,1.45f),glass);
        for(int x=-1;x<=1;x+=2)
        for(int z=-1;z<=1;z+=2)
            Box("Wheel",g,V(x*1.3f,.32f,z*.77f),V(.62f,.62f,.25f),dark);
        Box("FrontLamp",g,V(2.02f,.65f,0),V(.03f,.17f,1.3f),cream);
    }
    private static void Lamp(Vector3 p)
    {
        Transform g=Group("StreetLamp",city); g.localPosition=p;
        Box("Post",g,V(0,2.2f,0),V(.09f,4.4f,.09f),dark);
        Box("Arm",g,V(0,4.35f,.45f),V(.09f,.09f,.9f),dark);
        Box("Head",g,V(0,4.28f,.9f),V(.3f,.10f,.5f),glow);
    }
    private static void Bench(Vector3 p)
    {
        Transform g=Group("Bench",city);g.localPosition=p;
        Box("Seat",g,V(0,.48f,0),V(1.8f,.10f,.45f),dark);
        Box("Back",g,V(0,.78f,-.2f),V(1.8f,.5f,.08f),dark);
        for(int x=-1;x<=1;x+=2) Box("Leg",g,V(x*.65f,.24f,0),V(.08f,.48f,.4f),metal);
    }
    private static void Planter(Vector3 p)
    {
        Transform g=Group("Planter",city);g.localPosition=p;
        Box("Pot",g,V(0,.35f,0),V(.9f,.7f,.9f),dark);
        Box("Trunk",g,V(0,1.35f,0),V(.13f,2,.13f),metal);
        for(int i=0;i<3;i++)
        {
            GameObject crown=Box("Canopy",g,V(0,2.5f+i*.35f,0),
                V(1.6f-i*.3f,.65f,1.5f-i*.25f),green);
            crown.transform.localRotation=Quaternion.Euler(0,i*30,0);
        }
    }

    private static Material Mat(string name,Color color) => DemoGeometry.Mat(Folder,name,color,.12f);
}
#endif
