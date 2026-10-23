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
        readonly List<Vector3> normals=new List<Vector3>();
        readonly List<int>[] triangles={new List<int>(),new List<int>()};
        void Face(List<Vector3> points,int material)
        {
            int start=vertices.Count;Vector3 area=Vector3.zero;for(int i=1;i<points.Count-1;i++)area+=Vector3.Cross(points[i]-points[0],points[i+1]-points[0]);
            if(area.sqrMagnitude<1e-20f)return;var normal=area/Mathf.Sqrt(area.sqrMagnitude);
            foreach(var p in points){vertices.Add(p);normals.Add(normal);if(Mathf.Abs(normal.y)>.7f)uv.Add(new Vector2(p.x,p.z));else{var up=(Vector3.up-normal*normal.y).normalized;uv.Add(new Vector2(Vector3.Dot(p,Vector3.Cross(normal,up)),Vector3.Dot(p,up)));}}
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
        public Mesh Save(string name)
        {
            var mesh=new Mesh{name=name,vertices=vertices.ToArray(),uv=uv.ToArray(),normals=normals.ToArray(),subMeshCount=2};
            mesh.SetTriangles(triangles[0],0);mesh.SetTriangles(triangles[1],1);mesh.RecalculateBounds();
            string path=Folder+"/Recording_"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }
        public GameObject Emit(string name,Transform parent,Vector3 position,Vector3 scale,Material face,Material edge)
        {
            var mesh=Save(name);var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=new[]{face,edge};
            r.shadowCastingMode=ShadowCastingMode.Off;r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
            // Static batching would weld a pane into one scene mesh, so glass left in a door leaf
            // (a Rigidbody/HingeJoint, or the authored door objects before Doors() adds them) stays
            // behind when the door swings: anything under a moving parent keeps no static flag.
            GameObjectUtility.SetStaticEditorFlags(go,MovesAtRuntime(parent)?0:StaticEditorFlags.BatchingStatic);
            return go;
        }
        public static bool MovesAtRuntime(Transform parent)
        {
            if(parent==null)return false;
            if(parent.GetComponentInParent<Rigidbody>()||parent.GetComponentInParent<HingeJoint>()||parent.GetComponentInParent<PushDoorLeaf>())return true;
            for(var t=parent;t!=null;t=t.parent)
                if(t.name=="DoorLeft"||t.name=="DoorRight"||(t.name=="Door"&&t.parent&&(t.parent.name=="OfficeDoor"||t.parent.name=="ServiceDoor")))return true;
            return false;
        }
    }
    static void PolishedPanePart(Geometry geometry,Bounds bounds){
        var s=bounds.size;float width,height,thickness;Quaternion rotation;
        if(s.y<=s.x&&s.y<=s.z){width=s.x;height=s.z;thickness=s.y;rotation=Quaternion.Euler(90,0,0);}
        else if(s.x<=s.z){width=s.z;height=s.y;thickness=s.x;rotation=Quaternion.Euler(0,90,0);}
        else{width=s.x;height=s.y;thickness=s.z;rotation=Quaternion.identity;}
        var poly=new List<Vector2>{new Vector2(-width*.5f,-height*.5f),new Vector2(width*.5f,-height*.5f),new Vector2(width*.5f,height*.5f),new Vector2(-width*.5f,height*.5f)};
        geometry.Prism(poly,thickness,bounds.center,rotation,Mathf.Min(.00045f,thickness*.12f));
    }
    public static Mesh PolishedPaneAssembly(string name,Mesh source,out int panes){
        if(source.name.StartsWith("PolishedRetailPane_")&&source.subMeshCount==2){panes=source.GetTriangles(0).Length/12;return source;}
        var vertices=source.vertices;var triangles=source.triangles;
        // The saved case combines four/five closed box panes in one renderer:
        // each original Box contributes twelve contiguous triangles. Retain that
        // batching and each individual pane's bounds, rather than filling the
        // combined bounds (which would wrongly put a solid pane across the case).
        if(triangles.Length%36!=0)throw new Exception("Unexpected original intact glass assembly: "+source.name);
        var geometry=new Geometry();panes=0;for(int i=0;i<triangles.Length;i+=36){
            var bounds=new Bounds(vertices[triangles[i]],Vector3.zero);for(int j=1;j<36;j++)bounds.Encapsulate(vertices[triangles[i+j]]);PolishedPanePart(geometry,bounds);panes++;
        }return geometry.Save(name);
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
            face=GroundMaterial("GroundGlassFaces",face,new Color(.92f,.97f,1,.045f));
            edge=GroundMaterial("GroundGlassCutEdges",edge,new Color(.66f,.77f,.78f,.10f));
        }
        var go=geometry.Emit(name,parent,position,scale,face,edge);
        if(name.StartsWith("GroundGlass"))Contact(go,face);
        return go;
    }

    // Architectural repair: circular stress rings and branching rays, clipped to the real frame.
    // Shared ring vertices keep crack cells connected; physical missing cells create the breach.
    public static GameObject ArchitecturalPane(string name,Transform parent,Vector3 position,float width,float height,int seed,int damage,Material face,Material edge,Material crack){
        var random=new System.Random(seed);int rays=30;float[] radii={0,.018f,.036f,.065f,.11f,.18f,.29f,.46f,.73f,1.20f,2.9f};
        var centre=new Vector2(width*(damage==1?.08f:damage==2?-.07f:-.12f),height*(damage==1?.43f:damage==2?.51f:.51f));
        var points=new Vector2[radii.Length,rays];var angles=new float[rays];for(int a=0;a<rays;a++)angles[a]=(a+Range(random,-.22f,.22f))*Mathf.PI*2/rays;
        for(int ring=0;ring<radii.Length;ring++)for(int a=0;a<rays;a++){
            float angle=angles[a]+(ring>1?Range(random,-.035f,.035f):0),radius=radii[ring]*Range(random,.88f,1.12f);points[ring,a]=centre+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
        }
        var geometry=new Geometry();var fracture=new KitMesh{Lightmapped=false};var edges=new HashSet<string>();float removedArea=0,keptArea=0;
        string Key(Vector2 p)=>Mathf.RoundToInt(p.x*100000)+","+Mathf.RoundToInt(p.y*100000);
        void Line(Vector2 a,Vector2 b,float lineWidth){
            if((b-a).sqrMagnitude<.0000001f)return;string aa=Key(a),bb=Key(b);string key=string.CompareOrdinal(aa,bb)<0?aa+"/"+bb:bb+"/"+aa;if(!edges.Add(key))return;
            var d=b-a;var tangent=new Vector2(-d.y,d.x);tangent*=lineWidth*.5f/Mathf.Sqrt(tangent.sqrMagnitude);
            foreach(float side in new[]{-1f,1f}){float z=side*.00315f;fracture.Face(crack,Vector3.forward*side,new Vector3(a.x+tangent.x,a.y+tangent.y,z),new Vector3(b.x+tangent.x,b.y+tangent.y,z),new Vector3(b.x-tangent.x,b.y-tangent.y,z),new Vector3(a.x-tangent.x,a.y-tangent.y,z));}
        }
        for(int ring=0;ring<radii.Length-1;ring++)for(int a=0;a<rays;a++){
            int b=(a+1)%rays;var cell=new List<Vector2>{points[ring,a],points[ring+1,a],points[ring+1,b],points[ring,b]};
            cell=Clip(cell,Vector2.left,width*.5f);cell=Clip(cell,Vector2.right,width*.5f);cell=Clip(cell,Vector2.down,0);cell=Clip(cell,Vector2.up,height);
            for(int k=cell.Count-1;k>=0&&cell.Count>2;k--)if((cell[k]-cell[(k+1)%cell.Count]).sqrMagnitude<1e-10f)cell.RemoveAt(k);
            if(cell.Count<3)continue;Vector2 mid=Vector2.zero;float area=0;foreach(var p in cell)mid+=p;mid/=cell.Count;for(int k=0;k<cell.Count;k++)area+=cell[k].x*cell[(k+1)%cell.Count].y-cell[(k+1)%cell.Count].x*cell[k].y;area=Mathf.Abs(area)*.5f;if(area<1e-8f)continue;
            float rx=damage==1?width*.34f:damage==2?width*.18f:width*.30f,ry=damage==1?height*.24f:damage==2?height*.10f:height*.25f;
            float xx=(mid.x-centre.x)/rx,yy=(mid.y-centre.y)/ry;float ragged=1+.19f*Mathf.Sin(Mathf.Atan2(yy,xx)*7+seed);
            if(damage>0&&xx*xx+yy*yy<ragged){removedArea+=area;continue;}keptArea+=area;

            geometry.Prism(cell,.006f,Vector3.zero,Quaternion.identity,.00010f);
            // Secondary branching remains wholly inside each retained convex cell.
            // It adds photographic crack density without multiplying full-thickness glass cells.
            if(area>.0008f){
                int branches=area>.025f?5:area>.006f?3:1;
                for(int branch=0;branch<branches;branch++){
                    int e=branch%cell.Count;var a0=Vector2.Lerp(cell[e],cell[(e+1)%cell.Count],Range(random,.2f,.8f));
                    var target=Vector2.Lerp(mid,cell[(e+2)%cell.Count],Range(random,.12f,.66f));Line(a0,target,.00095f);Line(target,Vector2.Lerp(cell[(e+2)%cell.Count],cell[(e+3)%cell.Count],Range(random,.2f,.8f)),.0008f);
                }
            }

            for(int k=0;k<cell.Count;k++){
                var p=cell[k];var q=cell[(k+1)%cell.Count];
                // Never paint bright fracture stripes over the frame gasket.
                bool border=(Mathf.Abs(p.x-q.x)<.00001f&&Mathf.Abs(Mathf.Abs(p.x)-width/2)<.0001f)||(Mathf.Abs(p.y-q.y)<.00001f&&(Mathf.Abs(p.y)<.0001f||Mathf.Abs(p.y-height)<.0001f));
                if(!border)Line(p,q,ring<4?.00085f:.00145f);
            }
        }
        var go=geometry.Emit(name+"_"+seed,parent,position,Vector3.one,face,edge);go.name=name;var cr=fracture.Emit("FineFractureNetwork",go.transform,Vector3.zero,Quaternion.identity,Folder,"architectural_cracks_"+seed);cr.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        Debug.Log($"ARCH_GLASS_PANE {name} retained={keptArea:F3}m2 removed={removedArea:F3}m2 lines={edges.Count}");return go;
    }
    public static GameObject DoorRemnants(string name,Transform parent,Vector3 position,float width,float height,int seed,Material face,Material edge,Material crack){
        var random=new System.Random(seed);var geometry=new Geometry();var lines=new KitMesh{Lightmapped=false};
        void Fragment(List<Vector2> poly){
            geometry.Prism(poly,.006f,Vector3.zero,Quaternion.identity,.00012f);
            if(random.NextDouble()<.33){var a=Vector2.Lerp(poly[0],poly[1],.45f);var b=Vector2.Lerp(poly[2],poly[3],.55f);foreach(float side in new[]{-1f,1f})lines.Stroke(crack,new Vector3(a.x,a.y,side*.00315f),new Vector3(b.x,b.y,side*.00315f),.00065f,Vector3.forward*side);}
        }
        foreach(float side in new[]{-1f,1f})for(float y0=.002f;y0<height-.025f;){
            float len=Mathf.Min(height-y0,Range(random,.045f,.15f)),depth=Range(random,.019f,.078f),x=side*width*.5f;
            var poly=new List<Vector2>{new Vector2(x,y0),new Vector2(x-side*depth*Range(random,.35f,.72f),y0+len*.14f),new Vector2(x-side*depth,y0+len*Range(random,.42f,.80f)),new Vector2(x,y0+len)};if(side>0)poly.Reverse();if(random.NextDouble()>.08)Fragment(poly);y0+=len+Range(random,.003f,.023f);
        }
        foreach(bool top in new[]{false,true})for(int i=0;i<9;i++){
            float x=-width*.5f+.105f+i*(width-.21f)/9,w=(width-.21f)/9*.90f,d=Range(random,.022f,.061f),y=top?height:0,sign=top?-1:1;
            var poly=new List<Vector2>{new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w*.72f,y+sign*d),new Vector2(x+w*.18f,y+sign*d*.65f)};if(top)poly.Reverse();Fragment(poly);
        }
        var go=geometry.Emit("DoorFrameRemnants_"+seed,parent,position,Vector3.one,face,edge);go.name=name;
        var network=lines.Emit("FineFractureNetwork",go.transform,Vector3.zero,Quaternion.identity,Folder,"door_remnant_seams_"+seed);var renderer=network.GetComponent<MeshRenderer>();if(renderer)renderer.shadowCastingMode=ShadowCastingMode.Off;
        return go;
    }
    public static GameObject ArchitecturalDebris(string name,Transform parent,Vector3 position,int seed,int count,float rx,float rz,Material face,Material edge,bool fine=false,int style=0){
        var random=new System.Random(seed);var geometry=new Geometry();var placed=new List<Vector3>();var sizes=new List<float>();
        for(int i=0;i<count;i++){
            float angle=Range(random,0,Mathf.PI*2),radius=Mathf.Pow((float)random.NextDouble(),1.8f);float x=Mathf.Cos(angle)*rx*radius,z=Mathf.Sin(angle)*rz*radius;
            float size=fine?Range(random,.0025f,.009f):style==2?(i%11==0?Range(random,.018f,.035f):Range(random,.004f,.015f)):i%27==0?Range(random,.026f,.055f):i%5==0?Range(random,.012f,.023f):Range(random,.0035f,.012f);float thickness=fine?Range(random,.0015f,.003f):Range(random,.003f,.0055f),y=0;
            for(int j=0;j<placed.Count;j++)if(Vector2.Distance(new Vector2(x,z),new Vector2(placed[j].x,placed[j].z))<(size+sizes[j])*.56f)y=Mathf.Max(y,placed[j].y+.0030f);
            y=Mathf.Min(y,.025f);int n=fine?(i%3==0?4:3):random.Next(3,6);var poly=new List<Vector2>();float stretch=style==1?Range(random,.22f,.65f):Range(random,.5f,1),turn=Range(random,0,6.28f);
            for(int k=0;k<n;k++){float a=turn+k*6.283185f/n;poly.Add(new Vector2(Mathf.Cos(a)*size,Mathf.Sin(a)*size*stretch));}
            float tilt=i%4==0?32f:12f;var rot=Quaternion.Euler(90+Range(random,-tilt,tilt),Range(random,0,360),Range(random,-tilt,tilt));float minY=float.PositiveInfinity;
            foreach(var p in poly)foreach(float side in new[]{-.5f,.5f})minY=Mathf.Min(minY,(rot*new Vector3(p.x,p.y,thickness*side)).y);
            geometry.Prism(poly,thickness,new Vector3(x,y-minY+.0008f,z),rot,fine?0:.00012f);placed.Add(new Vector3(x,y,z));sizes.Add(size);
        }
        var go=geometry.Emit(name,parent,position,Vector3.one,face,edge);RefreshContact(go);return go;
    }
    static Material GroundMaterial(string name,Material source,Color color)
    {
        string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(source);AssetDatabase.CreateAsset(material,path);}
        material.color=color;material.SetFloat("_Smoothness",.95f);return material;
    }
    static void Contact(GameObject scatter,Material unused)
    {
        RefreshContact(scatter);
    }
    public static void RefreshContact(GameObject scatter){
        var old=scatter.transform.Find("PileContact");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var source=scatter.GetComponent<MeshFilter>().sharedMesh;var vertices=source.vertices;var normals=source.normals;var triangles=source.GetTriangles(0);var projected=new List<int>();
        // Only the upper planar faces contribute: no double-darkening from underside/edge triangles.
        for(int i=0;i<triangles.Length;i+=3)if(normals[triangles[i]].y>.1f){projected.Add(triangles[i]);projected.Add(triangles[i+1]);projected.Add(triangles[i+2]);}
        for(int i=0;i<vertices.Length;i++)vertices[i].y=-.001f;
        var mesh=new Mesh{name=scatter.name+"_Contact",vertices=vertices,uv=source.uv,triangles=projected.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Folder+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,path);
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/GlassShardContact.mat");if(!material){material=new Material(Shader.Find("CrimeScene/GlassContact"));AssetDatabase.CreateAsset(material,Folder+"/GlassShardContact.mat");}
        material.SetFloat("_Strength",0);material.SetFloat("_UniformStrength",.035f);EditorUtility.SetDirty(material);
        var go=new GameObject("PileContact");go.transform.SetParent(scatter.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
    }
}
#endif
