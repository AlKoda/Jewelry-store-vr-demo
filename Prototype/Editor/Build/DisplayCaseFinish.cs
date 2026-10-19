#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static DemoGeometry;

// Dedicated case palette: changing this never recolours the facade or street.
public static class DisplayCaseFinish
{
    public const string Folder = "Assets/CrimeSceneDemo/DisplayCaseGenerated";
    public static Material Pane, Edge, Walnut, Metal, Lining, Seal;

    public static void Prepare()
    {
        if (Pane != null) return;
        DisplayCaseReflections.ConfigureDesktop();
        EnsureFolder(Folder);
        Pane = Glass("LowIronGlass", new Color(.92f,.99f,.97f,.008f), .97f);
        Edge = Glass("PolishedGlassEdge", new Color(.30f,.62f,.49f,.22f), .94f);
        Metal = Mat(Folder,"SatinChampagne",new Color(.66f,.52f,.32f),.73f);
        Metal.SetFloat("_Metallic",.88f);
        Walnut = new Material(StoreKit.Walnut);
        Walnut.name = "CaseWalnut";
        string path = Folder + "/CaseWalnut.mat";
        var old = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (old != null) { EditorUtility.CopySerialized(Walnut,old); Object.DestroyImmediate(Walnut); Walnut=old; }
        else AssetDatabase.CreateAsset(Walnut,path);
        Walnut.color = new Color(.83f,.74f,.64f);
        Walnut.SetFloat("_Glossiness",.36f);
        Walnut.SetFloat("_BumpScale",.16f);
        Walnut.SetTexture("_MetallicGlossMap",null);
        Walnut.DisableKeyword("_METALLICGLOSSMAP");
        Lining = Mat(Folder,"IvoryMicrosuede",new Color(.69f,.63f,.52f),.12f);
        Seal = Mat(Folder,"GraphiteGasket",new Color(.028f,.031f,.032f),.28f);
        foreach (var m in new[]{Pane,Edge,Walnut,Metal,Lining,Seal}) EditorUtility.SetDirty(m);
    }

    static Material Glass(string name, Color color, float smoothness)
    {
        Shader shader=Shader.Find("CrimeScene/DisplayGlass");
        if(shader==null) throw new System.InvalidOperationException("DisplayGlass shader is missing");
        string path=Folder+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}
        m.shader=shader;m.color=color;m.SetFloat("_IOR",1.52f);
        m.SetFloat("_Smoothness",smoothness);m.SetFloat("_MicroVariation",.008f);
        m.renderQueue=3000;
        return m;
    }

    // Closed 26-face bevelled solid. Six faces, twelve edge quads, eight corner
    // triangles. Dimensions remain exact; the bevel takes material off the corners.
    // A thinAxis >= 0 puts the green material only on the pane's cut edges.
    public static void Bevel(KitMesh mesh, Material face, Vector3 c, Vector3 size,
        float radius, Material edge=null, int thinAxis=-1)
    {
        edge=edge??face;
        Vector3 h=size*.5f;
        float b=Mathf.Min(radius,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.8f);
        Vector3 inset=h-Vector3.one*b;
        Vector3[] axes={Vector3.right,Vector3.up,Vector3.forward};
        for(int a=0;a<3;a++)
        {
            int u=(a+1)%3,v=(a+2)%3;
            foreach(int s in new[]{-1,1})
            {
                Vector3 n=axes[a]*s, centre=c+n*h[a];
                Vector3 x=axes[u]*inset[u], y=axes[v]*inset[v];
                mesh.Face(thinAxis<0||a==thinAxis?face:edge,n,centre-x-y,centre+x-y,centre+x+y,centre-x+y);
            }
        }
        for(int a=0;a<3;a++) for(int d=a+1;d<3;d++)
        {
            int k=3-a-d;
            foreach(int s in new[]{-1,1}) foreach(int t in new[]{-1,1})
            {
                Vector3 p=c+axes[a]*s*h[a]+axes[d]*t*inset[d];
                Vector3 q=c+axes[a]*s*inset[a]+axes[d]*t*h[d];
                Vector3 z=axes[k]*inset[k];
                mesh.Face(edge,axes[a]*s+axes[d]*t,p-z,q-z,q+z,p+z);
            }
        }
        foreach(int x in new[]{-1,1}) foreach(int y in new[]{-1,1}) foreach(int z in new[]{-1,1})
        {
            Vector3 p=c+Vector3.Scale(inset,new Vector3(x,y,z));
            mesh.Face(edge,new Vector3(x,y,z),p+Vector3.right*x*b,p+Vector3.up*y*b,p+Vector3.forward*z*b);
        }
    }

    public static void Fragment(KitMesh k,Vector3 a,Vector3 b,Vector3 c,float thickness=.004f)
    {
        Vector3 n=Vector3.Cross(b-a,c-a).normalized, offset=n*thickness*.5f;
        k.Face(Pane,n,a+offset,b+offset,c+offset);
        k.Face(Pane,-n,c-offset,b-offset,a-offset);
        Vector3[] p={a,b,c};
        for(int i=0;i<3;i++)
        {
            Vector3 v=p[i],w=p[(i+1)%3];
            k.Face(Edge,Vector3.Cross(w-v,n),v-offset,w-offset,w+offset,v+offset);
        }
    }
}
#endif
