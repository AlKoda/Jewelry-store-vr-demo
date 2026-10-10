#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;

public static class ShopDisplayFinish
{
    private const string Folder="Assets/CrimeSceneDemo/InteriorGenerated";
    public static void Apply()
    {
        Transform store=GameObject.Find("JewelryStore_Blockout").transform;
        if(store.Find("DisplayFinish")!=null) throw new System.InvalidOperationException("Displays already finished.");
        EnsureFolder(Folder);
        Material oak=Mat(Folder,"SmokedOak",new Color(.25f,.21f,.16f),.20f);
        Material inset=Mat(Folder,"CabinetInset",new Color(.34f,.30f,.23f),.16f);
        Material bronze=Mat(Folder,"DisplayBronze",new Color(.61f,.47f,.25f),.38f);
        bronze.SetFloat("_Metallic",.45f);
        Material linen=Mat(Folder,"CounterStone",new Color(.77f,.75f,.69f),.21f);
        Material pad=Mat(Folder,"DisplayVelvet",new Color(.16f,.21f,.23f),.04f);
        Material suede=Mat(Folder,"DisplaySuede",new Color(.55f,.58f,.53f),.06f);
        Material gold=Mat(Folder,"JewelryGold",new Color(.78f,.56f,.22f),.55f);
        gold.SetFloat("_Metallic",.65f);
        Material glass=Translucent(Mat(Folder,"ClearGlass",new Color(.78f,.88f,.90f,.07f),.55f));
        Material broken=Translucent(Mat(Folder,"BrokenGlass",new Color(.46f,.71f,.75f,.25f),.35f));
        Material shard=Mat(Folder,"GlassShardEdges",new Color(.40f,.60f,.64f),.35f);
        Material steel=Mat(Folder,"SafeSteel",new Color(.27f,.32f,.33f),.20f);
        steel.SetFloat("_Metallic",.25f);
        Mesh horizontal=Pane("GlassHorizontal",0),vertical=Pane("GlassVertical",1),side=Pane("GlassSide",2);
        foreach(MeshRenderer r in store.GetComponentsInChildren<MeshRenderer>(true))
        {
            string n=r.name;
            if(r.transform.IsChildOf(store.Find("FormDetails")) &&
                (r.transform.parent.name.StartsWith("Island") || r.transform.parent.name.StartsWith("WallDisplay")))
            {
                string materialName=r.sharedMaterial.name;
                if(materialName=="Graphite") r.sharedMaterial=oak;
                else if(materialName=="WarmGrey") r.sharedMaterial=inset;
                else if(materialName=="MutedBrass") r.sharedMaterial=bronze;
            }
            if(n=="FramePost" || n=="TopRail") r.sharedMaterial=bronze;
            else if(n.StartsWith("JewelryPad_") || n=="RingStand") r.sharedMaterial=pad;
            else if(n=="NecklaceBust") r.sharedMaterial=suede;
            else if(n=="DisplaySurface" || n=="CounterTop") r.sharedMaterial=linen;
            else if(n=="ServiceCounter") r.sharedMaterial=oak;
            else if(n=="IntactCaseTop" || n=="IntactCaseSide" || n=="DoorGlass" || n=="IntactStorefrontGlass")
            {
                // Two triangles per side instead of transparent cube faces; keep existing colliders.
                r.GetComponent<MeshFilter>().sharedMesh=n=="IntactCaseTop"?horizontal:n=="IntactCaseSide"?side:vertical;
                r.sharedMaterial=glass;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            }
            else if(n=="JaggedGlassRemnant")
            {r.sharedMaterial=broken;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
            else if(n.Contains("Shard") || n.StartsWith("SillGlassRemnant")) r.sharedMaterial=shard;
            if(r.transform.IsChildOf(store.Find("Furniture/OpenSafe")) && r.GetComponent<TextMesh>()==null && n!="DarkenedDoorEdge")
                r.sharedMaterial=n.Contains("Wheel") || n.Contains("Spoke") || n=="CombinationDial"?bronze:
                    n=="Shelf" || n=="Top" || n=="Bottom"?inset:steel;
        }
        RefineJewelry(store,gold);
        FinishLabels();
        Transform details=Group("DisplayFinish",store);
        Material led=Glowing(Mat(Folder,"DisplayDiffuser",new Color(.90f,.89f,.80f),.1f),new Color(1,.95f,.84f)*.4f);
        foreach(Transform display in store.Find("Furniture"))
        {
            if(!display.name.StartsWith("Island") && !display.name.StartsWith("WallDisplay")) continue;
            Transform basis=display.Find("Base");
            Vector3 p=basis.localPosition;p.y=1.17f;
            for(int sign=-1;sign<=1;sign+=2)
                Box("FrameDiffuser",details,p+V(sign*(basis.localScale.x*.5f-.045f),0,0),V(.009f,.009f,basis.localScale.z-.09f),led);
        }
        CombineByMaterial(details,Folder+"/DisplayFinish_","DisplayFinish_");
        AssetDatabase.SaveAssets();
    }

    private static void FinishLabels()
    {
        Shader shader=Shader.Find("CrimeScene/WorldText");
        if(shader==null) throw new System.InvalidOperationException("Import WorldText.shader before finishing the store.");
        Dictionary<Font,Material> materials=new Dictionary<Font,Material>();
        foreach(TextMesh text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(text.font==null) continue;
            if(!materials.TryGetValue(text.font,out Material material))
            {
                string path=Folder+"/Label_"+materials.Count+".mat";
                material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null) {material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
                else material.shader=shader;
                materials.Add(text.font,material);
            }
            DepthTestedLabel label=text.GetComponent<DepthTestedLabel>();
            if(label==null) label=text.gameObject.AddComponent<DepthTestedLabel>();
            label.LabelMaterial=material;label.Refresh();
            EditorUtility.SetDirty(material);
        }
    }

    private static Mesh Pane(string name,int axis)
    {
        Vector3[] front={V(-.5f,0,-.5f),V(-.5f,0,.5f),V(.5f,0,.5f),V(.5f,0,-.5f)};
        Vector3[] vertices=new Vector3[8];
        for(int i=0;i<4;i++)
        {
            Vector3 p=front[i];
            if(axis==1) p=V(p.x,p.z,0);
            else if(axis==2) p=V(0,p.x,p.z);
            vertices[i]=vertices[i+4]=p;
        }
        Mesh mesh=new Mesh {name=name,vertices=vertices,triangles=new int[]{0,1,2,0,2,3,4,6,5,4,7,6}};
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        SaveMesh(ref mesh,Folder+"/"+name+".asset");return mesh;
    }

    private static void RefineJewelry(Transform store,Material gold)
    {
        Mesh necklace=Loop("NecklaceLoop",.06f,.0045f),ring=Loop("RingLoop",.025f,.0035f);
        // Group the original twelve-segment loops by their fixed local z plane.
        foreach(Transform parent in store.GetComponentsInChildren<Transform>(true))
        {
            if(parent==null) continue;
            Dictionary<string,List<Transform>> groups=new Dictionary<string,List<Transform>>();
            foreach(Transform t in parent)
            {
                if(t.name!="Necklace_Segment" && t.name!="JewelryRing_Segment") continue;
                string key=t.name+"_"+Mathf.RoundToInt(t.localPosition.z*10000);
                if(!groups.ContainsKey(key)) groups[key]=new List<Transform>();
                groups[key].Add(t);
            }
            foreach(var group in groups)
            {
                // Do not replace custom/manual variants with an incomplete ring.
                if(group.Value.Count!=12) continue;
                Vector3 center=Vector3.zero;
                foreach(Transform t in group.Value) center+=t.localPosition;
                center/=group.Value.Count;
                bool isNecklace=group.Key.StartsWith("Necklace");
                ShopFormRefinement.Shape("JewelryLoop",parent,isNecklace?necklace:ring,center,Vector3.one,gold);
                foreach(Transform t in group.Value) Object.DestroyImmediate(t.gameObject);
            }
        }
    }

    private static Mesh Loop(string name,float radius,float tube)
    {
        const int steps=12,sides=4;
        Vector3[] vertices=new Vector3[steps*sides],normals=new Vector3[steps*sides];
        List<int> indices=new List<int>();
        for(int i=0;i<steps;i++) for(int j=0;j<sides;j++)
        {
            float a=i*Mathf.PI*2/steps,b=j*Mathf.PI*2/sides;
            Vector3 normal=V(Mathf.Cos(b)*Mathf.Cos(a),Mathf.Cos(b)*Mathf.Sin(a),Mathf.Sin(b));
            int index=i*sides+j;
            normals[index]=normal;
            vertices[index]=V(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0)+normal*tube;
            int next=((i+1)%steps)*sides+j,diagonal=((i+1)%steps)*sides+(j+1)%sides,last=i*sides+(j+1)%sides;
            indices.Add(index);indices.Add(next);indices.Add(diagonal);
            indices.Add(index);indices.Add(diagonal);indices.Add(last);
        }
        Mesh mesh=new Mesh {name=name,vertices=vertices,normals=normals,triangles=indices.ToArray()};
        mesh.RecalculateBounds();SaveMesh(ref mesh,Folder+"/"+name+".asset");return mesh;
    }

    public static void CheckBudget()
    {
        Transform store=GameObject.Find("JewelryStore_Blockout").transform;
        int loops=0,panes=0;
        foreach(MeshFilter f in store.GetComponentsInChildren<MeshFilter>(true))
        {
            if(f.name=="JewelryLoop")
            {
                loops++;
                if(f.sharedMesh.triangles.Length/3!=96) throw new System.InvalidOperationException("Jewelry loop budget exceeded.");
            }
            if(f.name=="IntactCaseTop" || f.name=="IntactCaseSide" || f.name=="DoorGlass" || f.name=="IntactStorefrontGlass")
            {
                panes++;
                if(f.sharedMesh.triangles.Length/3!=4) throw new System.InvalidOperationException("Glass must remain a thin pane.");
            }
        }
        DemoValidation.Check(loops==16,"All 16 jewelry loops refined across robbed and intact stock");
        DemoValidation.Check(panes==6,"Six glass surfaces use thin panes");
        Transform detail=store.Find("DisplayFinish");
        DemoValidation.Check(detail.GetComponentsInChildren<MeshRenderer>().Length==1 &&
            detail.GetComponentsInChildren<Collider>().Length==0 &&
            detail.GetComponentsInChildren<Light>().Length==0,"Display diffusers share one mesh and add no lights or colliders");
        DemoValidation.Check(GameObject.Find("JewelryStore_Blockout").transform.Find("ShopDetails/Brand")
            .GetComponent<MeshRenderer>().sharedMaterial.shader.name=="CrimeScene/WorldText","World signage uses depth-tested text");
        int sceneTriangles=0,sceneMeshes=0;
        foreach(MeshFilter f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            MeshRenderer r=f.GetComponent<MeshRenderer>();
            if(r==null || !r.enabled || f.sharedMesh==null) continue;
            sceneMeshes++;sceneTriangles+=f.sharedMesh.triangles.Length/3;
        }
        DemoValidation.Info("Active scene MeshFilter triangles="+sceneTriangles+"; mesh renderers="+sceneMeshes+" (excludes text meshes)");
        DemoValidation.Info("Jewelry loops="+loops+" at 96 triangles each; glass panes="+panes+" at 4 triangles each");
    }
}
#endif
