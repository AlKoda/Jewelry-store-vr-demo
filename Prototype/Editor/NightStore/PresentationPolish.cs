#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using static DemoGeometry;

// In-place art repairs for the saved demonstration scene; does not regenerate its layout.
public static partial class PresentationPolish
{
    const string Folder="Assets/CrimeSceneDemo/CombinedArt";
    static Transform root;
    public static void Apply(){
        // Finish texture reimports before editing scene instances and their transforms.
        foreach(string file in new[]{"ShopFascia_Designed.png","CollectionRings_Designed.png","CollectionNecklaces_Designed.png"})TextureBudget(DemoAssetLibrary.Texture("ArabicSceneSigns",file));
        TextureBudget(DemoAssetLibrary.Texture("SuppliedPoliceCar","police+car+3d+model.fbm/police+car+3d+model_basecolor.jpg"));
        root=GameObject.Find("NightStore").transform;StoreKit.Palette();
        Lighting();Jewelry();Traces();Street();Buildings();FacadeCharacter();Trees();Branding();EnvironmentSigns();InteriorDetails();LivedInDetails();
        ConsistentPropMaterials();StreetSurfaceRepair();NaturalTrees();CarLightingRepair();ClearShardMaterials();
        Physics.SyncTransforms();AssetDatabase.SaveAssets();
        Debug.Log("PRESENTATION_POLISH_APPLIED saved scene preserved, shadow casters, closed display bases, subtle evidence, full street, supplied car, leafy canopy and Arabic branding");
    }
    static Transform Fresh(string name,Transform parent){var old=parent.Find(name);if(old)Object.DestroyImmediate(old.gameObject);return Group(name,parent);}
    static T Save<T>(T asset,string name) where T:Object{
        string path=Folder+"/"+name+(asset is Material?".mat":".asset");var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old){EditorUtility.CopySerialized(asset,old);Object.DestroyImmediate(asset);return old;}
        AssetDatabase.CreateAsset(asset,path);return asset;
    }
    static void Lighting(){
        var lights=Fresh("SecurityAndSignLighting",root.Find("Lighting"));
        // The store is closed with its interior lights off; only outside spill and handheld lights illuminate it.
        foreach(float x in new[]{-.9f,.9f}){
            var go=new GameObject("FasciaDownlight_"+x);go.transform.SetParent(lights,false);go.transform.localPosition=new Vector3(x,3.81f,-.67f);go.transform.LookAt(root.TransformPoint(new Vector3(x*.6f,3.25f,-.22f)));
            var l=go.AddComponent<Light>();l.type=LightType.Spot;l.range=2;l.spotAngle=100;l.intensity=1.3f;l.color=new Color(1,.87f,.66f);l.lightmapBakeType=LightmapBakeType.Baked;l.shadows=LightShadows.Soft;
            var k=new KitMesh{Lightmapped=false};k.Cylinder(StoreKit.Bronze,new Vector3(x,3.85f,-.19f),new Vector3(x,3.85f,-.64f),.018f,8);k.Box(StoreKit.Bronze,new Vector3(x,3.86f,-.64f),new Vector3(.25f,.07f,.12f));k.Emit("FasciaLampHousing_"+x,lights,Vector3.zero,Quaternion.identity,Folder,"fascia_fixture_"+x);
        }
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){
            if(PathOf(r.transform).Contains("Architecture/Ceiling"))r.shadowCastingMode=ShadowCastingMode.TwoSided;
            foreach(var m in r.sharedMaterials.Where(m=>m)){
                if(m.name.StartsWith("Shared_")||m.name.Contains("Pipe")||m.name.Contains("Cable"))DemoAssetLibrary.ApplyPropMaps(m);
            }
        }
        foreach(var p in root.GetComponentsInChildren<ReflectionProbe>()){
            p.boxProjection=true;p.blendDistance=p.name.Contains("_Reflection")?.1f:p.name.Contains("Entrance")?.5f:.65f;
        }
        foreach(var l in root.GetComponentsInChildren<Light>())if(l.name.EndsWith("_Specular")){
            l.enabled=false; // A parked car needs no extra realtime light passes through the shop.
        }
    }
    // Seal small planar open rims in the combined display furniture. New faces keep the
    // same material, real UVs and outward normals; this repairs geometry rather than hiding it with Cull Off.
    static void Jewelry(){
        int faces=0;
        foreach(var f in root.Find("Dressing").GetComponentsInChildren<MeshFilter>()){
            var path=PathOf(f.transform);
            if(!path.Contains("/Displays/")&&!path.Contains("/Loose/")&&!path.Contains("DrawerTray"))continue;
            var source=f.sharedMesh;if(!source||source.name.EndsWith("_ClosedBases"))continue;
            var mesh=Object.Instantiate(source);var vertices=mesh.vertices.ToList();var normals=mesh.normals.ToList();var uv=mesh.uv.ToList();
            if(normals.Count!=vertices.Count||uv.Count!=vertices.Count){Object.DestroyImmediate(mesh);continue;}
            var weld=new Dictionary<Vector3Int,int>();var ids=new int[vertices.Count];
            for(int i=0;i<ids.Length;i++){var p=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));if(!weld.TryGetValue(key,out int id)){id=i;weld.Add(key,id);}ids[i]=id;}
            var edges=new Dictionary<(int,int),(int count,int a,int b,int sub)>();
            var tris=new List<int>[mesh.subMeshCount];
            for(int s=0;s<tris.Length;s++){
                tris[s]=mesh.GetTriangles(s).ToList();
                for(int i=0;i<tris[s].Count;i+=3)for(int e=0;e<3;e++){
                    int a=ids[tris[s][i+e]],b=ids[tris[s][i+(e+1)%3]];var key=(Math.Min(a,b),Math.Max(a,b));
                    if(edges.TryGetValue(key,out var edge))edges[key]=(edge.count+1,edge.a,edge.b,edge.sub);else edges[key]=(1,a,b,s);
                }
            }
            var pending=edges.Values.Where(e=>e.count==1).ToList();int added=0;
            while(pending.Count>0){
                var edge=pending[0];pending.RemoveAt(0);var ring=new List<int>{edge.a,edge.b};
                while(ring[ring.Count-1]!=ring[0]&&ring.Count<128){int at=pending.FindIndex(e=>e.a==ring[ring.Count-1]);if(at<0)break;var next=pending[at];pending.RemoveAt(at);ring.Add(next.b);}
                if(ring.Count<4||ring[ring.Count-1]!=ring[0])continue;ring.RemoveAt(ring.Count-1);
                var points=ring.Select(i=>vertices[i]).ToArray();var centre=points.Aggregate(Vector3.zero,(a,b)=>a+b)/points.Length;
                var n=Vector3.Cross(points[1]-points[0],points[2]-points[0]).normalized;
                if(n.sqrMagnitude<.5f)n=Vector3.up;
                if(points.Any(p=>Mathf.Abs(Vector3.Dot(p-centre,n))>.0005f)||points.Any(p=>(p-centre).magnitude>.45f))continue;
                // Existing rim winding faces into the hole; reverse it for the closing face.
                n=-n;int c=vertices.Count;vertices.Add(centre);normals.Add(n);uv.Add(new Vector2(centre.x,centre.z));
                int start=vertices.Count;
                foreach(var p in points){vertices.Add(p);normals.Add(n);uv.Add(Mathf.Abs(n.y)>.5f?new Vector2(p.x,p.z):new Vector2(p.x,p.y));}
                for(int i=0;i<points.Length;i++){tris[edge.sub].Add(c);tris[edge.sub].Add(start+(i+1)%points.Length);tris[edge.sub].Add(start+i);}added++;
            }
            if(added==0){Object.DestroyImmediate(mesh);continue;}
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);
            mesh.uv2=Array.Empty<Vector2>();for(int s=0;s<tris.Length;s++)mesh.SetTriangles(tris[s],s);mesh.RecalculateBounds();mesh.RecalculateTangents();
            mesh.name=source.name+"_ClosedBases";f.sharedMesh=Save(mesh,mesh.name);faces+=added;
            var r=f.GetComponent<MeshRenderer>();r.receiveGI=ReceiveGI.LightProbes;r.shadowCastingMode=ShadowCastingMode.On;
        }
        Debug.Log("DISPLAY_BASE_REPAIR_PASS closed rims="+faces);
    }
    static Material UV(string name,Texture texture,float intensity){
        var m=new Material(Shader.Find(UVTraceBuilder.ShaderName)){name=name,mainTexture=texture};
        m.SetColor("_GlowColor",new Color(.28f,1,.12f));m.SetFloat("_Intensity",intensity);m.SetFloat("_Visibility",0);m.renderQueue=3030;return Save(m,name);
    }
    static Texture2D DropMask(int variant){
        const int size=128;var t=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name="SmallInjuryDrop_"+variant};
        var pixels=new Color[size*size];var rng=new System.Random(441+variant);int count=variant%3==0?1:2;
        var drops=Enumerable.Range(0,count).Select(i=>new Vector4(.35f+(float)rng.NextDouble()*.3f,.3f+(float)rng.NextDouble()*.4f,.09f+(float)rng.NextDouble()*.035f,.10f+(float)rng.NextDouble()*.06f)).ToArray();
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){
            float u=(x+.5f)/size,v=(y+.5f)/size,a=0;
            foreach(var d in drops){float r=new Vector2((u-d.x)/d.z,(v-d.y)/d.w).magnitude;float ragged=r*(1+.08f*(Mathf.PerlinNoise(u*22+variant,v*24)-.5f));float body=1-Mathf.SmoothStep(.90f,1.02f,ragged);float rim=Mathf.SmoothStep(.60f,.92f,r);a=Mathf.Max(a,body*(.46f+.4f*rim)*(.85f+.15f*Mathf.PerlinNoise(u*35,v*35)));}
            pixels[y*size+x]=new Color(1,1,1,a);
        }
        t.SetPixels(pixels);t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Trilinear;t.Apply(true,false);return Save(t,t.name);
    }
    static void Traces(){
        foreach(string side in new[]{"DoorLeft","DoorRight"}){
            var leaf=root.Find("Facade/EntranceDoors/"+side);float sign=side=="DoorLeft"?1:-1;
            foreach(var old in leaf.GetComponentsInChildren<UVTrace>().Where(t=>t.name.StartsWith("EvidenceUV_Fingerprint_")||t.name.StartsWith("EvidenceUV_HandlePrint_")).ToArray())Object.DestroyImmediate(old.gameObject);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/EvidenceUVFingerprint.mat");
            // The real cylindrical pull is at x=+-0.815, y=.75..1.35, z=-.085.
            for(int i=0;i<3;i++){
                var p=new Vector3(sign*.815f,1.04f+i*.035f,-.0965f);
                var trace=UVTraceBuilder.Add("EvidenceUV_HandlePrint_"+i,leaf,leaf.TransformPoint(p),leaf.TransformDirection(Vector3.back),new Vector2(.017f,.026f),UVTraceBuilder.Mask.Smear,i*9-9);
                trace.GetComponent<Renderer>().sharedMaterial=material;trace.Surface=leaf.GetComponent<Collider>();
            }
        }
        var blood=root.Find("Dressing/FloorBloodTraces");
        if(blood)for(int i=0;i<6;i++){
            var texture=DropMask(i);var visible=blood.Find("SmallDriedBloodDrops_"+i);var trace=blood.Find("EvidenceUV_FloorDrops_"+i);
            float size=new[]{.035f,.047f,.029f,.054f,.038f,.025f}[i];
            if(visible){visible.localScale=new Vector3(size,size,1);var m=new Material(Shader.Find("CrimeScene/SurfaceDecal")){name="SparseDriedBlood_"+i,mainTexture=texture,color=new Color(.14f,.026f,.018f,.65f)};m.SetFloat("_Glossiness",.14f);m.renderQueue=2455;visible.GetComponent<Renderer>().sharedMaterial=Save(m,m.name);}
            if(trace){trace.localScale=new Vector3(size,size,1);trace.GetComponent<Renderer>().sharedMaterial=UV("SparseUVBlood_"+i,texture,.16f+i*.013f);}
        }
        foreach(var t in root.GetComponentsInChildren<UVTrace>(true).Where(t=>t.name.Contains("GlassDrop")||t.name.Contains("WipedGlassSmear"))){t.transform.localScale=t.name.Contains("GlassDrop")?new Vector3(.02475f,.0315f,1):new Vector3(.03025f,.072f,1);}
    }
    static void Street(){
        // Put the physical road/pavement ends beyond the camera's fogged far plane.
        foreach(var f in root.Find("Exterior/Ground").GetComponentsInChildren<MeshFilter>()){
            var source=f.sharedMesh;var v=source.vertices;var before=(Vector3[])v.Clone();var uv=source.uv;bool changed=false;
            for(int i=0;i<v.Length;i++)if(Mathf.Abs(v[i].x)>=89.9f&&Mathf.Abs(v[i].x)<299){
                float dx=Mathf.Sign(v[i].x)*300-v[i].x;v[i].x+=dx;changed=true;
                for(int j=0;j<before.Length;j++)if(Mathf.Abs(before[j].x-before[i].x)>1&&Mathf.Abs(before[j].y-before[i].y)<.001f&&Mathf.Abs(before[j].z-before[i].z)<.001f){uv[i]+=(source.uv[i]-source.uv[j])*(dx/(before[i].x-before[j].x));break;}
            }
            if(changed){var mesh=Object.Instantiate(source);mesh.vertices=v;mesh.uv=uv;mesh.RecalculateBounds();mesh.name=f.name+"_BeyondFog";f.sharedMesh=Save(mesh,mesh.name);}
        }
        var boundary=root.Find("Exterior/Boundary");if(boundary)boundary.gameObject.SetActive(false);
        RepairStreetRegions();
        var safety=Fresh("StreetSafetyBoundary",root.Find("Exterior"));
        foreach(float x in new[]{-29.5f,29.5f}){var b=safety.gameObject.AddComponent<BoxCollider>();b.center=new Vector3(x,1.4f,-8.5f);b.size=new Vector3(.2f,3.3f,17);}
        var back=safety.gameObject.AddComponent<BoxCollider>();back.center=new Vector3(0,1.4f,-16.9f);back.size=new Vector3(59,3.3f,.2f);
        ParkCar();
    }
    static void RepairStreetRegions(){
        var street=new InteriorBounds.Region(-29,29,-14,-3,NightStoreLayout.StreetY);
        var near=new InteriorBounds.Region(-29,29,-3,-.35f,0);
        var far=new InteriorBounds.Region(-29,29,-16.65f,-14,0);
        foreach(var b in Object.FindObjectsByType<InteriorBounds>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            b.Regions=b.Regions.Where(r=>!(r.MinZ<-.36f)).Concat(new[]{near,street,far}).ToArray();EditorUtility.SetDirty(b);
        }
    }
    static void ParkCar(){
        var car=root.Find("Exterior/PoliceCar");
        var existing=car.Find("SuppliedModel");if(existing)Object.DestroyImmediate(existing.gameObject);
        foreach(var r in car.GetComponentsInChildren<Renderer>())r.enabled=false;
        foreach(var c in car.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
        string path=DemoAssetLibrary.Root+"/SuppliedPoliceCar/police+car+3d+model.fbx";
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new Exception("Supplied unmodified FBX missing");
        var model=Object.Instantiate(asset);model.name="SuppliedModel";model.transform.SetParent(car,false);
        var bounds=DemoAssetLibrary.LocalBounds(model);if(bounds.size.x>bounds.size.z)model.transform.localRotation=Quaternion.Euler(0,90,0);
        model.transform.localRotation*=Quaternion.Euler(0,180,0); // Supplied FBX nose points opposite its long-axis forward.
        bounds=DemoAssetLibrary.LocalBounds(model);float length=Mathf.Max(bounds.size.x,bounds.size.z);model.transform.localScale=Vector3.one*(4.6f/length);
        var filters=model.GetComponentsInChildren<MeshFilter>();var local=new Bounds();bool first=true;
        foreach(var f in filters)foreach(var v in f.sharedMesh.vertices){var p=car.InverseTransformPoint(f.transform.TransformPoint(v));if(first){local=new Bounds(p,Vector3.zero);first=false;}else local.Encapsulate(p);}
        model.transform.localPosition=new Vector3(-local.center.x,-local.min.y,-local.center.z);
        var m=Mat(Folder,"SuppliedCarPaint",Color.white,.30f);m.mainTexture=DemoAssetLibrary.Texture("SuppliedPoliceCar","police+car+3d+model.fbm/police+car+3d+model_basecolor.jpg");m.SetFloat("_Metallic",.18f);
        TextureBudget(m.mainTexture);
        foreach(var r in model.GetComponentsInChildren<MeshRenderer>()){r.sharedMaterials=Enumerable.Repeat(m,r.sharedMaterials.Length).ToArray();r.shadowCastingMode=ShadowCastingMode.On;}
        car.localPosition=new Vector3(5,NightStoreLayout.StreetY,-4.65f);car.localRotation=Quaternion.Euler(0,90,0);
        var box=car.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,local.size.y*.5f,0);box.size=new Vector3(local.size.x,local.size.y,local.size.z);
        foreach(string side in new[]{"Left","Right"}){
            var lens=car.Find("Headlight"+side);if(!lens)continue;
            lens.localPosition=new Vector3(side=="Left"?-.68f:.68f,.68f,2.18f);
            foreach(var l in root.Find("Lighting").GetComponentsInChildren<Light>().Where(l=>l.name=="Headlight_"+side)){
                l.transform.SetPositionAndRotation(lens.position,car.rotation);l.intensity=.65f;l.range=13;l.spotAngle=52;l.color=new Color(1,.88f,.68f);
            }
        }
        StoreKit.MarkStatic(model,true);
        Debug.Log("SUPPLIED_CAR_PASS source FBX mesh unchanged; uniform scale="+model.transform.localScale.x+" triangles="+filters.Sum(f=>f.sharedMesh.triangles.Length/3));
    }
    static void Buildings(){
        var group=Fresh("NeighbourhoodDepth",root.Find("Exterior"));var rng=new System.Random(825);
        var k=new KitMesh{LightmapScale=.06f};
        for(int side=-1;side<=1;side+=2)for(int i=0;i<5;i++){
            float x=-56+i*24.5f+(side>0?4:0),z=side>0?26:-30;
            float h=11+(float)rng.NextDouble()*9,w=10+(float)rng.NextDouble()*7;
            k.Box(i%2==0?StoreKit.Brick:StoreKit.Render,new Vector3(x,h*.5f,z),new Vector3(w,h,8));
            NightStoreBuilder.Windows(k,rng,x-w*.5f,x+w*.5f,z-side*4,-side,h,.14f);
            k.Box(StoreKit.Concrete,new Vector3(x,h,z),new Vector3(w+.25f,.2f,8.25f));
        }
        k.Emit("MidDistanceBuildings",group,Vector3.zero,Quaternion.identity,Folder,"neighbourhood_depth");
        var continuation=new KitMesh{LightmapScale=.05f};
        for(int side=-1;side<=1;side+=2)for(int bank=0;bank<2;bank++)for(int i=0;i<2;i++){
            float a=side<0?-73+i*12:49+i*12,b=a+11.7f,z=bank==0?0:-17,facing=bank==0?-1:1,h=7.2f+(i+bank)%3*1.4f;
            continuation.Box((i+bank)%2==0?StoreKit.Brick:StoreKit.Render,new Vector3((a+b)*.5f,h*.5f,z-facing*4),new Vector3(b-a,h,8));
            NightStoreBuilder.Shopfront(continuation,a,b,z,facing);NightStoreBuilder.Windows(continuation,rng,a,b,z,facing,h,.14f);
            continuation.Box(StoreKit.Concrete,new Vector3((a+b)*.5f,h,z),new Vector3(b-a+.1f,.2f,.35f));
        }
        continuation.Emit("StreetVanishingRows",group,Vector3.zero,Quaternion.identity,Folder,"street_vanishing_rows");
        StoreKit.MarkStatic(group.gameObject,true);
        var old=root.Find("DistantCityBackdrop");if(old)old.gameObject.SetActive(false);
        var distant=Fresh("CitySkyline360",root);var skyline=new KitMesh{Lightmapped=false};
        var wall=Mat(Folder,"DistantCityStone",new Color(.23f,.25f,.29f),.16f);wall.mainTexture=StoreKit.Render.mainTexture;wall.mainTextureScale=new Vector2(.35f,.35f);
        var window=Mat(Folder,"DistantCityWindow",new Color(.17f,.14f,.10f),.15f);window.EnableKeyword("_EMISSION");window.SetColor("_EmissionColor",new Color(.10f,.075f,.035f));
        for(int i=0;i<54;i++){
            float angle=i*Mathf.PI*2/54,r=78+(float)rng.NextDouble()*16;var c=new Vector3(Mathf.Sin(angle)*r,0,Mathf.Cos(angle)*r);
            if(Mathf.Abs(c.z+8.5f)<20 && Mathf.Abs(c.x)>60)continue; // Keep the road's vanishing point open between the terraces.
            float h=8+(float)rng.NextDouble()*11,w=5+(float)rng.NextDouble()*5;
            skyline.Box(wall,c+Vector3.up*h*.5f,new Vector3(w,h,7));
            skyline.Box(wall,c+Vector3.up*h,new Vector3(w+.2f,.28f,7.2f));
            for(float y=2.8f;y<h-1;y+=2.8f)for(int face=0;face<4;face++)for(int column=-1;column<=1;column++){
                bool lit=rng.NextDouble()<.16f;var wm=lit?window:StoreKit.WindowDark;
                var offset=face%2==0?new Vector3(column*1.6f,y,face==0?-3.515f:3.515f):new Vector3(face==1?-w*.5f-.015f:w*.5f+.015f,y,column*1.6f);
                skyline.Box(wm,c+offset,face%2==0?new Vector3(.7f,1,.035f):new Vector3(.035f,1,.7f));
            }
        }
        skyline.Emit("ContinuousDistantBuildings",distant,Vector3.zero,Quaternion.identity,Folder,"city_skyline_360");
        var sky=RenderSettings.skybox;
        if(sky){sky.mainTexture=DemoAssetLibrary.Texture("NightSky","city_night_sky_overcast_1k.hdr");sky.SetFloat("_Mapping",1);sky.DisableKeyword("_MAPPING_6_FRAMES_LAYOUT");sky.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");sky.SetFloat("_ImageType",0);sky.SetFloat("_Layout",0);sky.SetFloat("_Exposure",1.1f);sky.SetColor("_Tint",new Color(.65f,.72f,.85f));EditorUtility.SetDirty(sky);}
        RestoreCityAtmosphere();
        foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))c.farClipPlane=Mathf.Max(c.farClipPlane,220);
    }
    static void Trees(){
        var group=Fresh("LivingStreetCanopy",root.Find("Exterior"));
        var material=new Material(Shader.Find("CrimeScene/StreetFoliage")){name="StreetLeaves",mainTexture=DemoAssetLibrary.Texture("InteriorModels","PottedPlantTall_Leaves_Color.png")};material.SetFloat("_Wind",.028f);material.SetFloat("_Cutoff",.45f);var leaves=Save(material,material.name);
        var rng=new System.Random(246);var positions=NightStreet.TreePositions.Concat(new[]{new Vector3(-20,0,-1.65f),new Vector3(21,0,-1.65f)}).ToArray();
        for(int tree=0;tree<positions.Length;tree++){
            var p=positions[tree];var k=new KitMesh{Lightmapped=false};
            var trunkCollider=new GameObject("TreeTrunkCollision_"+tree);trunkCollider.transform.SetParent(group,false);trunkCollider.transform.localPosition=p;
            var capsule=trunkCollider.AddComponent<CapsuleCollider>();capsule.center=new Vector3(0,2.1f,0);capsule.height=4.2f;capsule.radius=.13f;
            if(tree>=NightStreet.TreePositions.Length){var trunk=new KitMesh{Lightmapped=false};trunk.Cylinder(StoreKit.Walnut,Vector3.zero,Vector3.up*4.2f,.11f,9);trunk.Emit("SidewalkTreeTrunk_"+tree,group,p,Quaternion.identity,Folder,"street_trunk_"+tree);}
            for(int i=0;i<420;i++){
                float a=(float)rng.NextDouble()*Mathf.PI*2,h=(float)rng.NextDouble()*2-1,r=Mathf.Pow((float)rng.NextDouble(),.33f)*1.75f;
                var centre=new Vector3(Mathf.Cos(a)*r*Mathf.Sqrt(1-h*h),4.35f+h*1.8f,Mathf.Sin(a)*r*Mathf.Sqrt(1-h*h));
                float size=.34f+(float)rng.NextDouble()*.25f;var q=Quaternion.Euler((float)rng.NextDouble()*160,(float)rng.NextDouble()*360,(float)rng.NextDouble()*180);var x=q*Vector3.right*size*.5f;var y=q*Vector3.up*size*.5f;
                k.Quad(leaves,q*Vector3.back,centre-x-y,centre+x-y,centre+x+y,centre-x+y,new Rect(.02f,.57f,.42f,.40f));
            }
            var go=k.Emit("LeafCanopy_"+tree,group,p,Quaternion.identity,Folder,"leaf_canopy_"+tree);var renderer=go.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.TwoSided;renderer.lightProbeUsage=LightProbeUsage.BlendProbes;
            var grate=new KitMesh{Lightmapped=false};for(float x=-.6f;x<=.6f;x+=.15f)grate.Box(StoreKit.FrameMetal,new Vector3(x,.006f,0),new Vector3(.026f,.012f,1.3f));grate.Emit("TreeGrate_"+tree,group,p,Quaternion.identity,Folder,"tree_grate");
        }
    }
    static void FacadeCharacter(){
        var group=Fresh("MainStreetArchitecture",root.Find("Exterior"));var k=new KitMesh{LightmapScale=.22f};
        var trim=Mat(Folder,"AgedLimestoneTrim",new Color(.58f,.55f,.46f),.17f);trim.mainTexture=StoreKit.Render.mainTexture;
        var iron=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/WeatheredIron.mat")??StoreKit.FrameMetal;
        var green=Mat(Folder,"SageShopJoinery",new Color(.16f,.23f,.20f),.22f);
        var red=Mat(Folder,"OxideShopJoinery",new Color(.29f,.12f,.095f),.20f);
        var cream=Mat(Folder,"SandShopJoinery",new Color(.42f,.35f,.24f),.18f);
        // Exterior-only upper floors above the existing ceiling: no changes to the usable store.
        float[] seams={-7.98f,-2.65f,2.7f,7.98f},heights={7.7f,8.35f,7.95f};
        var rng=new System.Random(385);
        for(int i=0;i<3;i++){
            float x0=seams[i],x1=seams[i+1],h=heights[i];
            k.Box(StoreKit.Render,new Vector3((x0+x1)*.5f,(3.7f+h)*.5f,3.1f),new Vector3(x1-x0,h-3.7f,6.1f));
            NightStoreBuilder.Windows(k,rng,x0,x1,.045f,-1,h,.12f);
            Cornice(x0,x1,.045f,-1,h,i);
            // A modest roof rail and posts, visible against the sky from pavement level.
            if(i==0){for(float x=x0+.3f;x<x1-.2f;x+=.25f)k.Cylinder(iron,new Vector3(x,h+.12f,-.12f),new Vector3(x,h+.82f,-.12f),.013f,6);k.Cylinder(iron,new Vector3(x0+.3f,h+.82f,-.12f),new Vector3(x1-.2f,h+.82f,-.12f),.023f,8);}
        }
        int id=0;
        foreach(var b in new[]{(-20f,-14.25f,0f,-1f,7.5f),(-14.25f,-8.6f,0f,-1f,7.5f),(8.6f,14.25f,0f,-1f,7.5f),(14.25f,20f,0f,-1f,7.5f)})Decorate(b.Item1,b.Item2,b.Item3,b.Item4,b.Item5,id++,b.Item1<0?-20:8.6f);
        var across=NightStreet.AcrossLayout(NightStreet.HasApartment(true));
        for(int i=0;i<across.Heights.Length;i++){
            float x0=across.Edges[i*2],x1=across.Edges[i*2+1],mid=(x0+x1)*.5f;
            Decorate(x0,mid,-17,1,across.Heights[i],id++,x0);Decorate(mid,x1,-17,1,across.Heights[i],id++,x0);
        }
        k.Emit("TerracedShopFrontDetails",group,Vector3.zero,Quaternion.identity,Folder,"main_street_character");StoreKit.MarkStatic(group.gameObject,true);
        Debug.Log("MAIN_STREET_CHARACTER_PASS exterior upper storeys, stepped parapets, cornices, arches, painted joinery, awnings, gutters and roof rails");
        void Cornice(float x0,float x1,float z,float facing,float h,int variant){
            float width=x1-x0,mid=(x0+x1)*.5f,raise=new[]{.22f,.68f,.43f,.95f}[variant%4];
            k.Box(variant%3==0?StoreKit.Brick:StoreKit.Render,new Vector3(mid,h+raise*.5f,z-facing*.14f),new Vector3(width,raise,.32f));
            k.Box(trim,new Vector3(mid,h+raise,z+facing*.05f),new Vector3(width+.08f,.16f,.42f));
            k.Box(trim,new Vector3(mid,h-.17f,z+facing*.12f),new Vector3(width,.14f,.32f));
            for(float x=x0+.22f;x<x1-.15f;x+=.43f)k.Box(trim,new Vector3(x,h-.32f,z+facing*.09f),new Vector3(.13f,.20f,.18f));
            if(variant%3==1){
                // A shallow arched parapet rather than identical straight roof edges.
                for(int s=0;s<16;s++){float a=s*Mathf.PI/16,b=(s+1)*Mathf.PI/16;var p=new Vector3(mid+Mathf.Cos(a)*width*.28f,h+raise+Mathf.Sin(a)*.43f,z);var q=new Vector3(mid+Mathf.Cos(b)*width*.28f,h+raise+Mathf.Sin(b)*.43f,z);k.Cylinder(trim,p,q,.065f,6);}
            }
        }
        void Decorate(float x0,float x1,float z,float facing,float h,int variant,float windowOrigin){
            float width=x1-x0,mid=(x0+x1)*.5f;var paint=variant%3==0?green:variant%3==1?red:cream;
            Cornice(x0,x1,z,facing,h,variant);
            k.Box(paint,new Vector3(mid,3.35f,z+facing*.10f),new Vector3(width-.16f,.34f,.17f));
            foreach(float x in new[]{x0+.09f,x1-.09f}){
                k.Box(trim,new Vector3(x,1.65f,z+facing*.10f),new Vector3(.16f,3.3f,.20f));
                k.Box(trim,new Vector3(x,.25f,z+facing*.12f),new Vector3(.25f,.50f,.26f));
            }
            for(float y=4.2f;y+1.5f<h-.8f;y+=3)for(float x=windowOrigin+1;x+1.2f<x1-.2f;x+=2.4f){
                if(x<x0+.2f)continue;
                foreach(float dx in new[]{-.08f,1.28f})k.Box(trim,new Vector3(x+dx,y+.75f,z+facing*.09f),new Vector3(.11f,1.70f,.14f));
                k.Box(trim,new Vector3(x+.6f,y-.12f,z+facing*.14f),new Vector3(1.6f,.13f,.33f));
                if(variant%2==0)for(int s=0;s<12;s++){
                    float a=s*Mathf.PI/12,b=(s+1)*Mathf.PI/12;
                    k.Cylinder(trim,new Vector3(x+.6f+Mathf.Cos(a)*.69f,y+1.53f+Mathf.Sin(a)*.35f,z+facing*.07f),new Vector3(x+.6f+Mathf.Cos(b)*.69f,y+1.53f+Mathf.Sin(b)*.35f,z+facing*.07f),.045f,6);
                }
            }
            // Recessed shutter/canvas canopies are intentionally varied, with clear pavement beneath.
            if(variant%3!=2){
                float a=x0+.42f,b=x1-.42f,front=z+facing*.9f;
                k.Quad(paint,new Vector3(0,1,facing*.4f),new Vector3(a,3.06f,z),new Vector3(b,3.06f,z),new Vector3(b,2.84f,front),new Vector3(a,2.84f,front),new Rect(0,0,1,1));
                k.Box(paint,new Vector3(mid,2.77f,front),new Vector3(b-a,.16f,.035f));
                foreach(float x in new[]{a,b})k.Cylinder(iron,new Vector3(x,2.4f,z),new Vector3(x,2.82f,front),.012f,6);
            }
            float drain=x1-.2f,dz=z+facing*.13f;
            k.Cylinder(iron,new Vector3(drain,.16f,dz),new Vector3(drain,h-.3f,dz),.032f,8);
            foreach(float y in new[]{.7f,3.1f,5.9f})if(y<h)k.Box(iron,new Vector3(drain,y,dz-facing*.01f),new Vector3(.10f,.035f,.09f));
        }
    }
    static void Branding(){
        foreach(string file in new[]{"ShopFascia_Designed.png","CollectionRings_Designed.png","CollectionNecklaces_Designed.png"})TextureBudget(DemoAssetLibrary.Texture("ArabicSceneSigns",file));
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>())foreach(var m in r.sharedMaterials.Where(m=>m)){
            string texture=m.mainTexture?m.mainTexture.name:"";string file=texture=="ShopFascia"?"ShopFascia_Designed.png":texture=="CollectionRings"?"CollectionRings_Designed.png":texture=="CollectionNecklaces"?"CollectionNecklaces_Designed.png":null;
            if(file==null)continue;var map=DemoAssetLibrary.Texture("ArabicSceneSigns",file);if(!map)throw new Exception("Brand artwork missing: "+file);
            m.mainTexture=map;m.color=Color.white;m.SetFloat("_Glossiness",.28f);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",.10f);EditorUtility.SetDirty(m);
        }
        // Give the full Arabic wordmark its intended aspect ratio and a physical frame.
        var sign=root.Find("Facade/FasciaSign");sign.localScale=new Vector3(2.55f,.85f,1);
        var counter=Fresh("ArabicCounterBrand",root.Find("Showroom"));
        var background=Mat(Folder,"BrandEnamelBacking",new Color(.012f,.045f,.028f),.25f);
        var panel=new KitMesh{Lightmapped=false};panel.Box(background,new Vector3(5.5f,2.25f,8.375f),new Vector3(2,1,.004f));
        var face=Mat(Folder,"DesignedCounterBrand",Color.white,.28f);face.mainTexture=DemoAssetLibrary.Texture("ArabicSceneSigns","ShopFascia_Designed.png");
        panel.Quad(face,Vector3.back,new Vector3(4.5f,1.9167f,8.371f),new Vector3(6.5f,1.9167f,8.371f),new Vector3(6.5f,2.5833f,8.371f),new Vector3(4.5f,2.5833f,8.371f),new Rect(0,0,1,1));
        panel.Emit("PhysicalWordmark",counter,Vector3.zero,Quaternion.identity,Folder,"designed_counter_brand");StoreKit.MarkStatic(counter.gameObject,true);
    }
    static readonly string[] SignFiles={"Closed_Designed.png","StaffOnly_Designed.png","CCTV_Designed.png","StaffNotice_Designed.png","FireEvacuationPlan_Designed.png"};
    static void EnvironmentSigns(){
        foreach(var file in SignFiles){var t=DemoAssetLibrary.Texture("ArabicSceneSigns",file);if(!t)throw new Exception("Missing sign "+file);TextureBudget(t);}
        var closed=Mat(Folder,"DesignedClosedPlaque",Color.white,.24f);closed.mainTexture=DemoAssetLibrary.Texture("ArabicSceneSigns",SignFiles[0]);
        var staff=Mat(Folder,"DesignedStaffPlaque",Color.white,.24f);staff.mainTexture=DemoAssetLibrary.Texture("ArabicSceneSigns",SignFiles[1]);
        int plaques=0;
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){
            if(r.name=="ClosedSign"||r.name=="ClosedSignBack"){r.sharedMaterial=closed;plaques++;}
            if(r.name=="StaffOnlySign"){r.sharedMaterial=staff;plaques++;}
        }
        if(plaques<4)throw new Exception("Expected both closed faces and staff door plaques: "+plaques);
        var atlas=DemoAssetLibrary.Atlas("WallGraphics","Atlas/NightStoreInterior.json");
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="WallGraphics")){
            var renderer=filter.GetComponent<MeshRenderer>();var source=filter.sharedMesh;var mesh=Object.Instantiate(source);var uv=mesh.uv;
            for(int sub=0;sub<mesh.subMeshCount;sub++){
                var material=renderer.sharedMaterials[sub];if(!material||!material.mainTexture||material.mainTexture.name!="NightStoreInterior_Color")continue;
                var triangles=mesh.GetTriangles(sub);var keep=new List<int>();
                for(int i=0;i<triangles.Length;i+=3){bool remove=false;
                    foreach(var key in new[]{"FireEvacuationPlan","Sign_CCTV","NoticeBoard_StaffCork"}){var rect=atlas[key];rect.xMin-=.00001f;rect.yMin-=.00001f;rect.xMax+=.00001f;rect.yMax+=.00001f;if(rect.Contains(uv[triangles[i]])&&rect.Contains(uv[triangles[i+1]])&&rect.Contains(uv[triangles[i+2]]))remove=true;}
                    if(!remove){keep.Add(triangles[i]);keep.Add(triangles[i+1]);keep.Add(triangles[i+2]);}
                }mesh.SetTriangles(keep,sub);
            }
            filter.sharedMesh=Save(mesh,"DesignedSigns_"+filter.transform.parent.name+"_WallGraphics");
        }
        var group=Fresh("DesignedEnvironmentSigns",root);
        SignFace(group,"EvacuationPlan",SignFiles[4],new Vector3(1.6f,1.5f,8.431f),Vector3.back,.42f,.42f*724/1024);
        SignFace(group,"CCTVNotice",SignFiles[2],new Vector3(-8.3f,1.6f,-.204f),Vector3.back,.34f,.17f);
        var cork=Mat(Folder,"StaffBoardCork",new Color(.25f,.14f,.065f),.02f);var board=new KitMesh{Lightmapped=false};board.Box(cork,new Vector3(1.9f,1.5f,8.560f),new Vector3(.9f,.6f,.012f));board.Emit("StaffCorkBacking",group,Vector3.zero,Quaternion.identity,Folder,"designed_staff_cork");
        SignFace(group,"StaffReminder",SignFiles[3],new Vector3(1.9f,1.5f,8.567f),Vector3.forward,.35f,.525f);
        StoreKit.MarkStatic(group.gameObject,true);EditorUtility.SetDirty(closed);EditorUtility.SetDirty(staff);AssetDatabase.SaveAssets();
        Debug.Log("ENVIRONMENT_SIGNS_PASS plaques="+plaques+" plan/CCTV/reminder=3; lights unchanged");
    }
    static void SignFace(Transform parent,string name,string file,Vector3 centre,Vector3 normal,float width,float height){
        var m=Mat(Folder,"DesignedSign_"+name,Color.white,.14f);m.mainTexture=DemoAssetLibrary.Texture("ArabicSceneSigns",file);m.DisableKeyword("_EMISSION");
        var k=new KitMesh{Lightmapped=false};var r=Vector3.Cross(Vector3.up,-normal)*width*.5f;var u=Vector3.up*height*.5f;
        k.Quad(m,normal,centre-r-u,centre+r-u,centre+r+u,centre-r+u,new Rect(0,0,1,1));
        k.Emit(name,parent,Vector3.zero,Quaternion.identity,Folder,"designed_sign_"+name);EditorUtility.SetDirty(m);
    }
    public static void SignsRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;StoreKit.Palette();EnvironmentSigns();
        EditorSceneManager.SaveOpenScenes();ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();SignPreview();EditableDelivery.Build();Debug.Log("ENVIRONMENT_SIGNS_DELIVERY_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void SignPreview(){
        var go=new GameObject("Sign review camera");var camera=go.AddComponent<Camera>();NightQuality.Configure(camera);camera.stereoTargetEye=StereoTargetEyeMask.None;camera.fieldOfView=45;
        var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;var previous=RenderTexture.active;var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);var torch=lighting.FlashlightLight;
        var views=new[]{(new Vector3(1.6f,1.5f,7.75f),new Vector3(1.6f,1.5f,8.431f)),(new Vector3(1.9f,1.5f,9.3f),new Vector3(1.9f,1.5f,8.567f)),(new Vector3(-8.3f,1.6f,-.9f),new Vector3(-8.3f,1.6f,-.204f)),(new Vector3(-.5f,2.02f,-.7f),new Vector3(-.5f,2.02f,0))};
        try{Directory.CreateDirectory("Builds");for(int i=0;i<views.Length;i++){
            camera.transform.position=views[i].Item1;camera.transform.LookAt(views[i].Item2);torch.transform.SetPositionAndRotation(camera.transform.position+camera.transform.right*.08f,camera.transform.rotation);camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Builds/sign-"+i+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }}finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
    }
    static void TextureBudget(Texture texture){
        if(!texture)return;var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;if(importer==null)return;
        var android=importer.GetPlatformTextureSettings("Android");bool change=importer.maxTextureSize!=2048||!importer.mipmapEnabled||!android.overridden||android.maxTextureSize!=1024;
        if(!change)return;importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.anisoLevel=4;
        android.name="Android";android.overridden=true;android.maxTextureSize=1024;android.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();
    }
    static void InteriorDetails(){
        var group=Fresh("SelectedStoreDetails",root);
        foreach(var item in new[]{("SnakePlant",new Vector3(-7.1f,0,.68f)),("PottedPlantTall",new Vector3(7.1f,0,.68f)),("PottedSucculent",new Vector3(6.65f,1,7.78f))}){
            var go=DemoAssetLibrary.PlacePack("InteriorModels",item.Item1,group,item.Item2,0,new DemoAssetLibrary.PackOptions{Shadows=true,Lod=DemoAssetLibrary.LodClass.Prop});
            if(!go)continue;DemoAssetLibrary.Seat(go,item.Item2.y);
            // A prop is accepted only if its visible bounds clear the existing solid fixtures.
            if(!DemoAssetLibrary.WorldBounds(go,out var box,false))continue;
            bool overlap=false;foreach(var c in root.GetComponentsInChildren<Collider>()){
                if(c.isTrigger||c.transform.IsChildOf(go.transform)||c.bounds.max.y<=box.min.y+.012f)continue;
                var inset=box;inset.Expand(-.025f);if(inset.Intersects(c.bounds)){overlap=true;break;}
            }
            if(overlap){Object.DestroyImmediate(go);continue;}DemoAssetLibrary.AddBoundsCollider(go);
        }
        StoreKit.MarkStatic(group.gameObject,true);
    }
    static void LivedInDetails(){
        var group=Fresh("LivedInDetails",root);int accepted=0;var report=new List<string>();
        var items=new[]{
            ("InteriorModels","DeskPhone",new Vector3(-2.32f,.75f,9.28f),20f,.9f),
            ("InteriorModels","Mouse",new Vector3(-1.38f,.75f,9.5f),0f,1f),
            ("InteriorModels","LeverArchFiles",new Vector3(1.9f,0,11.25f),180f,1f),
            ("InteriorModels","FilingCabinet",new Vector3(2.45f,0,10.65f),-90f,1f),
            ("InteriorModels","CardboardBox",new Vector3(-7.3f,0,11.3f),13f,.85f),
            ("InteriorModels","CardboardBox",new Vector3(-6.5f,0,11.45f),-8f,.65f),
            ("InteriorModels","PottedSucculent",new Vector3(5.78f,.74f,10.4f),0f,.7f),
            ("InteriorModels","VaseCeramicBottle",new Vector3(7f,.55f,1.1f),0f,.7f),
            ("InteriorModels","PottedPlantArrowhead",new Vector3(3.6f,0,1.2f),24f,.85f),
            ("InteriorModels","PottedPlantTall",new Vector3(-2.5f,0,7.75f),-17f,.8f),
            ("StreetModels","StreetBench",new Vector3(-19f,0,-1.15f),180f,1f),
            ("StreetModels","MetalTrashCan",new Vector3(-21f,0,-.85f),10f,1f),
            ("StreetModels","PottedPlant",new Vector3(-24f,0,-.9f),0f,1f),
            ("StreetModels","StreetBench",new Vector3(20f,0,-15.75f),0f,1f),
            ("StreetModels","MetalTrashCan",new Vector3(22f,0,-16f),0f,1f),
            ("StreetModels","PottedPlant",new Vector3(25f,0,-15.9f),17f,1f),
            ("StreetModels","BikeRack",new Vector3(14f,0,-15.8f),90f,1f),
            ("StreetModels","BikeRack",new Vector3(15f,0,-15.8f),90f,1f)
        };
        foreach(var item in items){
            var go=DemoAssetLibrary.PlacePack(item.Item1,item.Item2,group,item.Item3,item.Item4,new DemoAssetLibrary.PackOptions{Shadows=true,Lod=DemoAssetLibrary.LodClass.Prop});
            if(!go){report.Add("missing "+item.Item2);continue;}go.transform.localScale*=item.Item5;DemoAssetLibrary.Seat(go,item.Item3.y);Physics.SyncTransforms();
            if(!DemoAssetLibrary.WorldBounds(go,out var box,false)){Object.DestroyImmediate(go);continue;}
            var inset=box;inset.Expand(-.025f);bool overlap=false;
            foreach(var collider in root.GetComponentsInChildren<Collider>()){
                if(collider.isTrigger||collider.transform.IsChildOf(go.transform)||collider.bounds.max.y<=box.min.y+.015f)continue;
                if(inset.Intersects(collider.bounds)){overlap=true;break;}
            }
            // Preserve the entrance and the plan's central evacuation aisle.
            if(box.center.z>0&&box.center.z<8.5f&&Mathf.Abs(box.center.x)<1.25f)overlap=true;
            if(overlap){report.Add("rejected overlap "+item.Item2+" "+item.Item3);Object.DestroyImmediate(go);continue;}
            DemoAssetLibrary.AddBoundsCollider(go);go.name=item.Item2+"_Detail_"+accepted;accepted++;report.Add("placed "+go.name+" "+box);
        }
        StoreKit.MarkStatic(group.gameObject,true);Directory.CreateDirectory("Builds");File.WriteAllLines("Builds/prop-dressing.txt",report);
        if(accepted<8)throw new Exception("Too few safe detail props: "+accepted);Debug.Log("LIVED_IN_DETAILS_PASS accepted="+accepted+"; rejected overlaps excluded, entrance aisle clear");
    }
    public static void PropsRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;StoreKit.Palette();LivedInDetails();ConsistentPropMaterials();StreetSurfaceRepair();
        EditorSceneManager.SaveOpenScenes();ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();Preview();EditableDelivery.Build();Debug.Log("PROP_DRESSING_DELIVERY_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Run(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");Apply();
        NightStoreValidation.PresentationChecks();
        EditorSceneManager.SaveOpenScenes();
        if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);
        ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();Preview();
        EditableDelivery.Build();Debug.Log("PRESENTATION_DELIVERY_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void ValidateAndBuildRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");
        RepairStreetRegions();EditorSceneManager.SaveOpenScenes();
        NightStoreValidation.PresentationChecks();Preview();EditableDelivery.Build();Debug.Log("PRESENTATION_FINAL_CHECK_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void Preview(){
        var go=new GameObject("Art review camera");var camera=go.AddComponent<Camera>();NightQuality.Configure(camera);camera.stereoTargetEye=StereoTargetEyeMask.None;camera.farClipPlane=220;
        var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;var previous=RenderTexture.active;
        var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);var torch=lighting.FlashlightLight;torch.enabled=false;
        try{
            var positions=new[]{(new Vector3(0,1.65f,-8.5f),new Vector3(0,1.8f,1)),(new Vector3(-18,1.65f,-8),new Vector3(10,2,-8)),(new Vector3(18,1.65f,-8),new Vector3(-12,2,-8)),(new Vector3(0,1.65f,1.6f),new Vector3(1,1.1f,6)),(new Vector3(-2.2f,.9f,3),new Vector3(0,.75f,4.5f))};
            Directory.CreateDirectory("Builds");for(int i=0;i<positions.Length;i++){
                camera.transform.position=positions[i].Item1;camera.transform.LookAt(positions[i].Item2);torch.enabled=i>=3;torch.transform.SetPositionAndRotation(camera.transform.position+camera.transform.right*.06f-camera.transform.up*.08f,camera.transform.rotation);camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Builds/presentation-"+i+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
            }
        }finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
    }
    public static void Inspect(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");
        var lines=new List<string>();
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            var path=PathOf(t);
            if(t.name.Contains("Handle")||t.name.Contains("Boundary")||t.name.Contains("bust")||t.name.Contains("stand")||t.name.Contains("Jewelry")||t.name.Contains("Riser")||t.name.Contains("Tray")||t.name.Contains("Collection")||t.name.Contains("Fascia")||t.name.Contains("Logo")||t.name.Contains("Ceiling"))
                lines.Add(path+" p="+t.position+" local="+t.localPosition+" scale="+t.localScale+" mats="+string.Join(",",t.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name).Distinct()));
        }
        foreach(var b in Object.FindObjectsByType<InteriorBounds>(FindObjectsInactive.Include,FindObjectsSortMode.None))lines.Add("BOUNDS "+PathOf(b.transform)+" "+string.Join(";",b.Regions.Select(r=>$"{r.MinX},{r.MaxX},{r.MinZ},{r.MaxZ},{r.FloorY}")));
        Directory.CreateDirectory("Builds");File.WriteAllLines("Builds/presentation-inspection.txt",lines);
        Debug.Log("PRESENTATION_INSPECTION_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    public static void RepairInspectRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;var lines=new List<string>();
        foreach(var r in root.GetComponentsInChildren<Renderer>()){
            string path=PathOf(r.transform);if(!path.Contains("Staff")&&!path.Contains("Office")&&!path.Contains("PoliceCar")&&!path.Contains("Ground")&&!path.Contains("SelectedAmbient")&&!path.Contains("Glass"))continue;
            lines.Add(path+" bounds="+r.bounds+" probe="+r.lightProbeUsage+" lightmap="+(r is MeshRenderer mr?mr.lightmapIndex:-1));
            foreach(var m in r.sharedMaterials.Where(m=>m))lines.Add("  "+m.name+" shader="+m.shader.name+" color="+m.color+" emission="+(m.HasProperty("_EmissionColor")?m.GetColor("_EmissionColor"):Color.black)+" gloss="+(m.HasProperty("_Glossiness")?m.GetFloat("_Glossiness"):0)+" glossMap="+(m.HasProperty("_GlossMapScale")?m.GetFloat("_GlossMapScale"):0)+" maps="+string.Join(",",m.GetTexturePropertyNames().Where(p=>m.GetTexture(p)).Select(p=>p+":"+m.GetTexture(p).name)));
        }
        File.WriteAllLines("Builds/repair-audit.txt",lines);EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
