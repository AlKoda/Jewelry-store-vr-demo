#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Glass is authored directly in Unity metres, with baked vertices and identity
// transforms. Convex fracture cells and bevelled closed shards share two materials.
public static class RecordingGlass
{
    const string Folder="Assets/CrimeSceneDemo/CombinedArt";
    sealed class Geometry
    {
        public readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Vector2> uv=new List<Vector2>();
        readonly List<int>[] triangles={new List<int>(),new List<int>()};
        void Face(List<Vector3> points,int material)
        {
            int start=vertices.Count;
            foreach(var p in points){vertices.Add(p);uv.Add(new Vector2(p.x,p.z+p.y));}
            for(int i=1;i<points.Count-1;i++){triangles[material].Add(start);triangles[material].Add(start+i);triangles[material].Add(start+i+1);}
        }
        public void Prism(List<Vector2> polygon,float thickness,Vector3 position,Quaternion rotation,float bevel)
        {
            Vector2 centre=Vector2.zero;foreach(var p in polygon)centre+=p;centre/=polygon.Count;
            var rings=new Vector3[4][];
            for(int r=0;r<4;r++){
                rings[r]=new Vector3[polygon.Count];
                for(int i=0;i<polygon.Count;i++){
                    var v=polygon[i];if(r==0||r==3)v=Vector2.MoveTowards(v,centre,bevel);
                    float z=r==0?-thickness*.5f:r==1?-thickness*.5f+bevel:r==2?thickness*.5f-bevel:thickness*.5f;
                    rings[r][i]=position+rotation*new Vector3(v.x,v.y,z);
                }
            }
            var bottom=new List<Vector3>(rings[0]);bottom.Reverse();Face(bottom,0);Face(new List<Vector3>(rings[3]),0);
            for(int r=0;r<3;r++)for(int i=0;i<polygon.Count;i++){
                int j=(i+1)%polygon.Count;
                Face(new List<Vector3>{rings[r][i],rings[r][j],rings[r+1][j],rings[r+1][i]},1);
            }
        }
        public GameObject Emit(string name,Transform parent,Vector3 position,Vector3 scale,Material face,Material edge)
        {
            var mesh=new Mesh{name=name,vertices=vertices.ToArray(),uv=uv.ToArray(),subMeshCount=2};
            mesh.SetTriangles(triangles[0],0);mesh.SetTriangles(triangles[1],1);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Folder+"/Recording_"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=new[]{face,edge};
            r.shadowCastingMode=ShadowCastingMode.Off;r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
            return go;
        }
    }
    static float Range(System.Random random,float a,float b)=>a+(b-a)*(float)random.NextDouble();
    static List<Vector2> Clip(List<Vector2> input,Vector2 normal,float offset)
    {
        var output=new List<Vector2>();if(input.Count==0)return output;
        var a=input[input.Count-1];float da=Vector2.Dot(a,normal)-offset;
        foreach(var b in input){float db=Vector2.Dot(b,normal)-offset;if((da<=0)!=(db<=0))output.Add(Vector2.Lerp(a,b,da/(da-db)));if(db<=0)output.Add(b);a=b;da=db;}
        return output;
    }
    static Geometry Pane(float width,float height,int seed,bool broken)
    {
        var random=new System.Random(seed);var impact=new Vector2(width*.08f,height*.48f);var sites=new List<Vector2>();
        // Irregular cells become finer near the impact, instead of identical radial wedges.
        for(int i=0;i<70;i++)sites.Add(new Vector2(Range(random,-width*.5f,width*.5f),Range(random,0,height)));
        for(int i=0;i<45;i++){
            float angle=Range(random,0,Mathf.PI*2),radius=Range(random,.05f,.43f);
            sites.Add(impact+new Vector2(Mathf.Cos(angle)*width*radius,Mathf.Sin(angle)*height*radius));
        }
        var geometry=new Geometry();
        for(int i=0;i<sites.Count;i++){
            var cell=new List<Vector2>{new Vector2(-width*.5f,0),new Vector2(width*.5f,0),new Vector2(width*.5f,height),new Vector2(-width*.5f,height)};
            for(int j=0;j<sites.Count&&cell.Count>2;j++)if(i!=j){var n=sites[j]-sites[i];cell=Clip(cell,n,(sites[j].sqrMagnitude-sites[i].sqrMagnitude)*.5f);}
            if(cell.Count<3)continue;Vector2 centre=Vector2.zero;foreach(var p in cell)centre+=p;centre/=cell.Count;
            float x=(centre.x-impact.x)/(width*.28f),y=(centre.y-impact.y)/(height*.23f);
            float irregular=1+.18f*Mathf.Sin(Mathf.Atan2(y,x)*5+seed);
            if(broken&&(x*x+y*y)<irregular)continue;
            for(int k=0;k<cell.Count;k++)cell[k]=Vector2.MoveTowards(cell[k],centre,.00032f);
            geometry.Prism(cell,.008f,Vector3.zero,Quaternion.identity,.0006f);
        }
        return geometry;
    }
    static Geometry Scatter(int seed,int count,float rx,float rz)
    {
        var random=new System.Random(seed);var geometry=new Geometry();
        var placed=new List<Vector3>();var radii=new List<float>();
        for(int i=0;i<count;i++){
            float a=Range(random,0,Mathf.PI*2),radius=Mathf.Pow((float)random.NextDouble(),1.65f);
            float x=Mathf.Cos(a)*rx*radius,z=Mathf.Sin(a)*rz*radius;
            float size=i<14?Range(random,.04f,.105f):Range(random,.008f,.04f)*(1.25f-radius*.4f);
            float y=.004f,thickness=Range(random,.0035f,.007f);
            // Accumulate overlapping fragments rather than intersecting them or leaving
            // a uniformly flat scatter. Largest piles remain only a few centimetres high.
            for(int j=0;j<placed.Count;j++)if(Vector2.Distance(new Vector2(x,z),new Vector2(placed[j].x,placed[j].z))<(size+radii[j])*.62f)y=Mathf.Max(y,placed[j].y+.0045f);
            y=Mathf.Min(y,.038f);var polygon=new List<Vector2>();int sides=random.Next(3,7);float stretch=Range(random,.35f,1),turn=Range(random,0,6.28f);
            for(int k=0;k<sides;k++){float angle=turn+(k+Range(random,-.17f,.17f))*Mathf.PI*2/sides;polygon.Add(new Vector2(Mathf.Cos(angle)*size,Mathf.Sin(angle)*size*stretch));}
            var rotation=Quaternion.Euler(90+Range(random,-17,17),Range(random,0,360),Range(random,-17,17));
            float bottom=0;foreach(var p in polygon)bottom=Mathf.Min(bottom,(rotation*new Vector3(p.x,p.y,-thickness*.5f)).y,(rotation*new Vector3(p.x,p.y,thickness*.5f)).y);
            geometry.Prism(polygon,thickness,new Vector3(x,y-bottom,z),rotation,.00055f);placed.Add(new Vector3(x,y,z));radii.Add(size);
        }
        return geometry;
    }
    public static GameObject Create(string name,Transform parent,Vector3 position,Vector3 scale,Material face,Material edge)
    {
        Geometry geometry;
        if(name.StartsWith("GroundGlass"))geometry=name.EndsWith("Sparse")?Scatter(91,95,1.1f,.8f):name.EndsWith("Threshold")?Scatter(41,290,1.2f,.85f):Scatter(52,220,.95f,.55f);
        else if(name.StartsWith("DoorGlass"))geometry=Pane(.775f,2.05f,name.Contains("Left")?15:21,true);
        else geometry=Pane(1.92f,2.13f,31,name.EndsWith("Broken"));
        if(name.StartsWith("GroundGlass")){
            face=GroundMaterial("GroundGlassFaces",face,new Color(.55f,.60f,.65f,.22f));
            edge=GroundMaterial("GroundGlassCutEdges",edge,new Color(.78f,.84f,.89f,.38f));
        }
        var go=geometry.Emit(name,parent,position,scale,face,edge);
        if(name.StartsWith("GroundGlass"))Contact(go,face);
        return go;
    }
    static Material GroundMaterial(string name,Material source,Color color)
    {
        string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(source);AssetDatabase.CreateAsset(material,path);}
        material.color=color;material.SetFloat("_Smoothness",.95f);return material;
    }
    static void Contact(GameObject scatter,Material unused)
    {
        // A restrained feathered contact patch beneath each dense pile substitutes
        // for transmitted glass contact shading unavailable in this raster renderer.
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/GlassPileContact.mat");
        if(!material){material=new Material(Shader.Find("CrimeScene/GlassContact"));AssetDatabase.CreateAsset(material,Folder+"/GlassPileContact.mat");}
        var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="PileContact";go.transform.SetParent(scatter.transform,false);
        go.transform.localPosition=new Vector3(0,-.001f,0);go.transform.localRotation=Quaternion.Euler(90,0,0);go.transform.localScale=new Vector3(1.25f,.72f,1);
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;
    }
}
#endif
