#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static DemoGeometry;

public static class ShopGlassRefinement
{
    const string Folder="Assets/CrimeSceneDemo/GlassGenerated";
    public static void Apply()
    {
        EnsureFolder(Folder);
        Transform store=GameObject.Find("JewelryStore_Blockout").transform;
        Material glass=Glass("ArchitecturalGlass",new Color(.80f,.93f,.90f,.018f),.50f);
        Material fractured=Glass("FracturedPane",new Color(.78f,.91f,.86f,.035f),.65f);
        Material shard=Glass("FragmentGlass",new Color(.72f,.86f,.80f,.075f),.42f);
        Material edge=Mat(Folder,"GlassEdges",new Color(.36f,.50f,.44f),.65f);
        var glassRenderers=new List<MeshRenderer>();
        var shards=new List<Transform>();
        foreach(MeshRenderer r in store.GetComponentsInChildren<MeshRenderer>(true))
        {
            string n=r.name;
            if(n.Contains("DisplayShard") || n.StartsWith("WindowShard")) {shards.Add(r.transform);continue;}
            if(n.StartsWith("SillGlassRemnant")) {Object.DestroyImmediate(r.gameObject);continue;}
            if(n=="IntactCaseTop" || n=="IntactCaseSide" || n=="IntactStorefrontGlass" || n=="DoorGlass" || n=="JaggedGlassRemnant")
            {
                r.sharedMaterial=n=="JaggedGlassRemnant"?fractured:glass; r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
                glassRenderers.Add(r);
            }
        }
        Mesh fragment=Fragment();
        int serial=0;
        foreach(Transform t in shards)
        {
            bool display=t.name.Contains("DisplayShard");
            if(display)
            {
                bool left=t.name.StartsWith("Island_Left");
                int i=int.Parse(t.name.Substring(t.name.LastIndexOf('_')+1));
                // A narrow scatter beside the pads; fragments rest on the .84 m case surface.
                t.position=V((left?-1.7f:4.45f)+(i%2==0?1:-1)*(left?.40f:.25f),.841f,(left?3.7f:4f)-.85f+i*.39f);
            }
            else t.position=new Vector3(t.position.x,.005f,t.position.z);
            if(display) t.position+=new Vector3(Mathf.Sin(serial*7)*.02f,0,Mathf.Cos(serial*13)*.07f);
            float size=display?.065f+(serial%3)*.017f:.06f+(serial%5)*.025f;
            t.localScale=V(size,1,size);t.localRotation=Quaternion.Euler(0,serial*137.5f,0);
            t.GetComponent<MeshFilter>().sharedMesh=fragment;
            var renderer=t.GetComponent<MeshRenderer>();renderer.sharedMaterials=new[]{shard,edge};
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            glassRenderers.Add(renderer);serial++;
        }
        int edgeSerial=0;
        foreach(MeshRenderer r in glassRenderers)
            if(r.name=="JaggedGlassRemnant") BrokenEdges(r,edge,edgeSerial++);
        foreach(MeshRenderer r in glassRenderers)
        {
            if(r.name!="IntactCaseTop") continue;
            // Child edge strips inherit the pane's intact-state visibility.
            Transform parent=Group("PolishedEdges",r.transform.parent);
            parent.position=r.transform.position;
            float width=r.transform.localScale.x,depth=r.transform.localScale.z;
            for(int sign=-1;sign<=1;sign+=2)
            {
                Box("GlassEdge",parent,V(sign*width*.5f,0,0),V(.0025f,.003f,depth),edge);
                Box("GlassEdge",parent,V(0,0,sign*depth*.5f),V(width,.003f,.0025f),edge);
            }
        }
        // One static, low-resolution cubemap. No per-frame reflection camera.
        foreach(var r in glassRenderers) r.enabled=false;
        GameObject cameraObject=new GameObject("GlassBakeCamera");
        Camera camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
        camera.transform.position=V(0,1.5f,3.8f);camera.nearClipPlane=.05f;camera.farClipPlane=80;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.fogColor;
        Cubemap reflection=new Cubemap(256,TextureFormat.RGB24,false);
        if(!camera.RenderToCubemap(reflection)) throw new System.Exception("Glass reflection bake failed");
        Object.DestroyImmediate(cameraObject);
        string path=Folder+"/RoomReflection.asset";
        Cubemap prior=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
        if(prior==null) AssetDatabase.CreateAsset(reflection,path);
        else {EditorUtility.CopySerialized(reflection,prior);Object.DestroyImmediate(reflection);reflection=prior;}
        fractured.SetTexture("_Reflection",reflection);EditorUtility.SetDirty(fractured);
        glass.SetTexture("_Reflection",reflection);shard.SetTexture("_Reflection",reflection);
        foreach(var r in glassRenderers) r.enabled=true;
        EditorUtility.SetDirty(glass);EditorUtility.SetDirty(shard);
        StyleTape();AssetDatabase.SaveAssets();
    }
    static void BrokenEdges(MeshRenderer renderer,Material material,int serial)
    {
        Mesh source=renderer.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] sourceVertices=source.vertices;int[] triangles=source.triangles;
        var unique=new List<Vector3>();var ids=new Dictionary<Vector3,int>();var counts=new Dictionary<string,int>();var pairs=new Dictionary<string,int[]>();
        foreach(Vector3 vertex in sourceVertices) if(!ids.ContainsKey(vertex)){ids[vertex]=unique.Count;unique.Add(vertex);}
        for(int i=0;i<triangles.Length;i+=3) for(int j=0;j<3;j++)
        {
            int a=ids[sourceVertices[triangles[i+j]]],b=ids[sourceVertices[triangles[i+(j+1)%3]]];
            string key=Mathf.Min(a,b)+":"+Mathf.Max(a,b);
            if(!counts.ContainsKey(key)){counts[key]=0;pairs[key]=new[]{a,b};}counts[key]++;
        }
        var vertices=new List<Vector3>();var indices=new List<int>();
        foreach(var pair in counts)
        {
            if(pair.Value!=2)continue; // Both front and back copies, no internal fan edges.
            Vector3 a=renderer.transform.TransformPoint(unique[pairs[pair.Key][0]]),b=renderer.transform.TransformPoint(unique[pairs[pair.Key][1]]);
            Vector3 direction=(b-a).normalized,normal=renderer.transform.up;
            Vector3 side=Vector3.Cross(direction,normal).normalized*.0012f,depth=normal*.0012f;
            int start=vertices.Count;
            foreach(Vector3 end in new[]{a,b})
                foreach(Vector3 offset in new[]{-side-depth,side-depth,side+depth,-side+depth})
                    vertices.Add(renderer.transform.InverseTransformPoint(end+offset));
            int[] box={0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};
            foreach(int index in box)indices.Add(start+index);
        }
        Mesh mesh=new Mesh{name="FractureEdges",vertices=vertices.ToArray(),triangles=indices.ToArray()};
        mesh.RecalculateNormals();mesh.RecalculateBounds();SaveMesh(ref mesh,Folder+"/FractureEdges_"+serial+".asset");
        Transform edges=Group("FractureEdges",renderer.transform);
        edges.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        edges.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    static Material Glass(string name,Color tint,float reflection)
    {
        string path=Folder+"/"+name+".mat";
        Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null) {material=new Material(Shader.Find("CrimeScene/ArchitecturalGlass"));AssetDatabase.CreateAsset(material,path);}
        material.color=tint;material.SetFloat("_Reflectivity",reflection);return material;
    }
    static Mesh Fragment()
    {
        // Convex irregular pentagon, with a real 2 mm green edge.
        Vector3[] outline={V(-.5f,0,-.28f),V(-.12f,0,.50f),V(.35f,0,.28f),V(.5f,0,-.12f),V(.1f,0,-.4f)};
        var vertices=new List<Vector3>();var faces=new List<int>();var sides=new List<int>();
        foreach(Vector3 p in outline)vertices.Add(p+Vector3.up*.002f);
        foreach(Vector3 p in outline)vertices.Add(p);
        for(int i=1;i<4;i++) {faces.Add(0);faces.Add(i);faces.Add(i+1);faces.Add(5);faces.Add(5+i+1);faces.Add(5+i);}
        for(int i=0;i<5;i++)
        {
            int j=(i+1)%5,k=vertices.Count;
            vertices.Add(outline[i]);vertices.Add(outline[j]);vertices.Add(outline[j]+Vector3.up*.002f);vertices.Add(outline[i]+Vector3.up*.002f);
            sides.Add(k);sides.Add(k+1);sides.Add(k+2);sides.Add(k);sides.Add(k+2);sides.Add(k+3);
        }
        Mesh mesh=new Mesh{name="GlassFragment",vertices=vertices.ToArray(),subMeshCount=2};
        mesh.SetTriangles(faces,0);mesh.SetTriangles(sides,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
        SaveMesh(ref mesh,Folder+"/GlassFragment.asset");return mesh;
    }
    static void StyleTape()
    {
        ToolStation station=Object.FindFirstObjectByType<ToolStation>();
        string path=AssetDatabase.GetAssetPath(station.TapePrefab);
        GameObject contents=PrefabUtility.LoadPrefabContents(path);
        Material tape=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/PoliceTape.mat");
        if(tape==null) {tape=new Material(Shader.Find("CrimeScene/Tape"));AssetDatabase.CreateAsset(tape,Folder+"/PoliceTape.mat");}
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int i=0;i<=8;i++)
        {
            float z=-.5f+i/8f;
            vertices.Add(V(-.5f,-.5f,z));vertices.Add(V(.5f,-.5f,z));
            vertices.Add(V(.5f,.5f,z));vertices.Add(V(-.5f,.5f,z));
        }
        for(int i=0;i<8;i++)for(int j=0;j<4;j++)
        {
            int a=i*4+j,b=i*4+(j+1)%4,c=b+4,d=a+4;
            triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(a);triangles.Add(c);triangles.Add(d);
        }
        triangles.AddRange(new[]{0,2,1,0,3,2,32,33,34,32,34,35});
        Mesh ribbonMesh=new Mesh{name="FlexibleRibbon",vertices=vertices.ToArray(),triangles=triangles.ToArray()};
        ribbonMesh.RecalculateNormals();ribbonMesh.RecalculateBounds();
        ribbonMesh.bounds=new Bounds(Vector3.zero,new Vector3(1,2,1));
        SaveMesh(ref ribbonMesh,Folder+"/FlexibleRibbon.asset");
        contents.GetComponentInChildren<MeshFilter>().sharedMesh=ribbonMesh;
        contents.GetComponentInChildren<MeshRenderer>().sharedMaterial=tape;
        SceneTape component=contents.GetComponent<SceneTape>();
        Material ink=AssetDatabase.LoadAssetAtPath<Material>("Assets/CrimeSceneDemo/PresentationGenerated/ToolNumber.mat");
        TextMesh front=Text("FrontWarning",contents.transform,"CRIME SCENE - DO NOT CROSS",Vector3.zero,90,.012f,Color.black);
        TextMesh back=Text("BackWarning",contents.transform,"CRIME SCENE - DO NOT CROSS",Vector3.zero,-90,.012f,Color.black);
        foreach(TextMesh text in new[]{front,back})
        {var label=text.gameObject.AddComponent<DepthTestedLabel>();label.LabelMaterial=ink;label.Refresh();}
        component.FrontWarning=front;component.BackWarning=back;
        PrefabUtility.SaveAsPrefabAsset(contents,path);PrefabUtility.UnloadPrefabContents(contents);
    }
}
#endif
