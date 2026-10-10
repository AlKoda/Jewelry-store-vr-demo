#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

// Geometry-only pass. Keeps evidence/state roots and existing simple colliders intact.
public static class ShopFormRefinement
{
    private const string Folder="Assets/CrimeSceneDemo/FormGenerated";
    private static Material chalk, dark, brass, stone;
    private static Mesh cabinet, plinth, bust, tray;
    private static int serial;

    [MenuItem("Crime Scene Demo/Refine Store Forms")]
    public static void Apply()
    {
        GameObject store=GameObject.Find("JewelryStore_Blockout");
        if(store==null || store.transform.Find("PresentationDetails")==null)
            throw new InvalidOperationException("Generate the expanded shop first.");
        if(store.transform.Find("FormDetails")!=null)
            throw new InvalidOperationException("Forms already refined; regenerate to apply changes.");
        EnsureFolder(Folder);
        chalk=Mat(Folder,"Chalk",new Color(.81f,.79f,.71f),.08f);
        dark=Mat(Folder,"Graphite",new Color(.16f,.19f,.20f),.08f);
        brass=Mat(Folder,"MutedBrass",new Color(.52f,.42f,.27f),.12f);
        stone=Mat(Folder,"WarmGrey",new Color(.51f,.50f,.46f),.06f);
        cabinet=Loft("Cabinet",new float[]{-.5f,-.43f,.43f,.5f},new float[]{.94f,1,1,.96f},new float[]{.94f,1,1,.96f},.09f);
        plinth=Loft("Plinth",new float[]{-.5f,.25f,.5f},new float[]{.94f,.94f,1},new float[]{.94f,.94f,1},.1f);
        bust=Loft("Bust",new float[]{-.5f,-.3f,.05f,.27f,.5f},new float[]{.60f,1,.80f,.37f,.33f},new float[]{.80f,1,.90f,.58f,.58f},.18f);
        tray=Loft("Tray",new float[]{-.5f,.28f,.5f},new float[]{.95f,1,.95f},new float[]{.95f,1,.95f},.1f);
        Transform root=Group("FormDetails",store.transform);serial=0;
        foreach(Transform display in store.transform.Find("Furniture"))
        {
            if(!display.name.StartsWith("Island") && !display.name.StartsWith("WallDisplay")) continue;
            Transform original=display.Find("Base");
            Vector3 p=original.localPosition;p.y=0;
            float width=original.localScale.x,depth=original.localScale.z;
            original.GetComponent<MeshRenderer>().enabled=false; // Keep collider.
            Transform zone=Group(display.name,root);
            Shape("ChamferedCabinet",zone,cabinet,p+V(0,.44f,0),V(width,.72f,depth),dark);
            Shape("RecessedToeKick",zone,plinth,p+V(0,.07f,0),V(width*.94f,.14f,depth*.96f),stone);
            Shape("DisplayLip",zone,tray,p+V(0,.797f,0),V(width,.035f,depth),brass);
            for(int side=-1;side<=1;side+=2)
            {
                Box("InsetEndPanel",zone,p+V(0,.43f,side*(depth*.5f+.001f)),V(width*.76f,.46f,.012f),stone);
                for(int row=0;row<2;row++)
                {
                    Box("DrawerReveal",zone,p+V(side*(width*.5f+.001f),.33f+row*.25f,0),V(.012f,.012f,depth*.83f),brass);
                    Box("Pull",zone,p+V(side*(width*.5f+.008f),.49f+row*.25f,0),V(.016f,.025f,.23f),brass);
                }
            }
            Batch(zone);
        }
        // Earlier full-width drawer blocks hide the new cabinet silhouette.
        foreach(Transform t in store.transform.Find("PresentationDetails").GetComponentsInChildren<Transform>())
            if(t.name=="WalnutPlinth" || t.name=="DrawerFront" || t.name=="DrawerHandle")
            {
                MeshRenderer r=t.GetComponent<MeshRenderer>();if(r!=null) r.enabled=false;
            }
        // Includes inactive intact-state stock; do not merge across state-toggle roots.
        foreach(Transform t in store.GetComponentsInChildren<Transform>(true))
        {
            MeshFilter f=t.GetComponent<MeshFilter>();if(f==null) continue;
            if(t.name=="NecklaceBust")
            {
                f.sharedMesh=bust;t.localScale=V(.17f,.18f,.105f);
                t.GetComponent<MeshRenderer>().sharedMaterial=chalk;
            }
            else if(t.name.StartsWith("JewelryPad_"))
            {
                f.sharedMesh=tray;t.GetComponent<MeshRenderer>().sharedMaterial=chalk;
            }
        }
        Architecture(root);
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(store.scene);
    }

    private static void Architecture(Transform root)
    {
        for(int side=-1;side<=1;side+=2)
        {
            Transform wall=Group(side<0?"WestWall":"EastWall",root);
            for(int bay=0;bay<2;bay++)
            {
                // West niches start beyond the existing photo frame (z=1.8..3.4).
                float z=(side<0?4.5f:3.05f)+bay*1.9f;
                Box("NicheBack",wall,V(side*4.975f,1.88f,z),V(.025f,.97f,1.4f),dark);
                for(int edge=-1;edge<=1;edge+=2)
                {
                    Box("NicheJamb",wall,V(side*4.91f,1.88f,z+edge*.71f),V(.15f,1.07f,.055f),chalk);
                    Box("NicheSill",wall,V(side*4.91f,1.88f+edge*.51f,z),V(.15f,.055f,1.47f),chalk);
                }
                Box("NicheMount",wall,V(side*4.90f,1.61f,z),V(.16f,.32f,.30f),stone);
                Box("NicheCrown",wall,V(side*4.86f,2.34f,z),V(.018f,.02f,1.25f),brass);
            }
            Batch(wall);
        }
        Transform rear=Group("ServiceWall",root);
        for(int side=-1;side<=1;side+=2)
        {
            Box("FeatureBorder",rear,V(-1.5f+side*2.24f,1.8f,7.94f),V(.045f,1.66f,.075f),brass);
            for(int rib=0;rib<3;rib++)
                Box("FeatureFlute",rear,V(-1.5f+side*(2.35f+rib*.105f),1.8f,7.95f),V(.035f,1.65f,.065f),stone);
        }
        Box("StaffJambLeft",rear,V(1.77f,1.15f,7.97f),V(.06f,2.3f,.10f),stone);
        Box("StaffJambRight",rear,V(3.23f,1.15f,7.97f),V(.06f,2.3f,.10f),stone);
        Box("StaffLintel",rear,V(2.5f,2.32f,7.97f),V(1.52f,.06f,.10f),stone);
        Batch(rear);
        Transform front=Group("StorefrontReveal",root);
        Box("DeepWindowSill",front,V(-1,.495f,.005f),V(6.90f,.035f,.24f),stone);
        Box("WindowHeadReveal",front,V(-1,2.62f,.005f),V(6.90f,.035f,.24f),stone);
        Box("DoorThreshold",front,V(3.8f,.006f,.03f),V(1.50f,.012f,.22f),brass);
        Batch(front);
    }

    // Combine only new, opaque, collision-free decoration, in spatially small groups.
    private static void Batch(Transform zone)
        => CombineByMaterial(zone,Folder+"/Batch_"+serial+++"_",zone.name+"_");

    public static GameObject Shape(string name,Transform parent,Mesh mesh,Vector3 position,Vector3 size,Material material)
    {
        Transform t=Group(name,parent);t.localPosition=position;t.localScale=size;
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t.gameObject;
    }

    // Closed eight-sided rings. Separate face vertices keep the bevels crisp.
    public static Mesh Loft(string name,float[] heights,float[] widths,float[] depths,float cut)
    {
        EnsureFolder(Folder);
        List<Vector3> vertices=new List<Vector3>();List<int> indices=new List<int>();
        Vector2[] p={new Vector2(-.5f+cut,-.5f),new Vector2(.5f-cut,-.5f),
            new Vector2(.5f,-.5f+cut),new Vector2(.5f,.5f-cut),new Vector2(.5f-cut,.5f),
            new Vector2(-.5f+cut,.5f),new Vector2(-.5f,.5f-cut),new Vector2(-.5f,-.5f+cut)};
        for(int level=0;level<heights.Length-1;level++)
            for(int i=0;i<8;i++)
            {
                int j=(i+1)%8;
                Vector3 a=Point(p[i],level,heights,widths,depths),b=Point(p[j],level,heights,widths,depths);
                Vector3 c=Point(p[j],level+1,heights,widths,depths),d=Point(p[i],level+1,heights,widths,depths);
                Triangle(vertices,indices,a,c,b);Triangle(vertices,indices,a,d,c);
            }
        for(int i=0;i<8;i++)
        {
            int j=(i+1)%8,top=heights.Length-1;
            Triangle(vertices,indices,V(0,heights[0],0),Point(p[i],0,heights,widths,depths),Point(p[j],0,heights,widths,depths));
            Triangle(vertices,indices,V(0,heights[top],0),Point(p[j],top,heights,widths,depths),Point(p[i],top,heights,widths,depths));
        }
        Mesh mesh=new Mesh {name=name,vertices=vertices.ToArray(),triangles=indices.ToArray()};
        mesh.RecalculateNormals();mesh.RecalculateBounds();SaveMesh(ref mesh,Folder+"/"+name+".asset");return mesh;
    }
    private static Vector3 Point(Vector2 p,int level,float[] y,float[] x,float[] z) => V(p.x*x[level],y[level],p.y*z[level]);
    private static void Triangle(List<Vector3> v,List<int> indices,Vector3 a,Vector3 b,Vector3 c)
    {
        int n=v.Count;v.Add(a);v.Add(b);v.Add(c);indices.Add(n);indices.Add(n+1);indices.Add(n+2);
    }

    public static void CheckBudget()
    {
        Transform root=GameObject.Find("JewelryStore_Blockout").transform.Find("FormDetails");
        if(root==null) throw new InvalidOperationException("Form refinement missing.");
        int triangles=0,renderers=0;
        foreach(MeshFilter f in root.GetComponentsInChildren<MeshFilter>())
        {triangles+=f.sharedMesh.triangles.Length/3;renderers++;}
        DemoValidation.Check(triangles<=5000 && renderers<=32,"Form detail fits 5000-triangle / 32-renderer budget");
        DemoValidation.Check(root.GetComponentsInChildren<Collider>().Length==0 &&
            root.GetComponentsInChildren<Light>().Length==0,"Form detail adds no physics or realtime lights");
        bool opaque=true;
        foreach(MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>())
            opaque &= r.sharedMaterial.mainTexture==null && r.sharedMaterial.renderQueue<3000;
        DemoValidation.Check(opaque,"Form detail uses opaque untextured materials");
        DemoValidation.Info("Form detail triangles="+triangles+"; renderers="+renderers);
    }
}
#endif
