#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
using static DemoGeometry;

public static partial class PresentationPolish {
    static Material fineLeather,edgePiping,stitchThread;
    static void Upholstery(Material m,string stem,Color tint){
        m.color=tint;m.mainTexture=DemoAssetLibrary.Texture("ArabicSceneSigns",stem+"_Color.png");
        m.SetTexture("_BumpMap",DemoAssetLibrary.NormalMap("ArabicSceneSigns",stem+"_Normal.png"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.35f);
        foreach(var t in new[]{m.mainTexture,m.GetTexture("_BumpMap")}){
            var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)) as TextureImporter;if(!importer)throw new Exception("Upholstery map missing: "+stem);
            importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=512;
            var android=importer.GetPlatformTextureSettings("Android");android.overridden=true;android.name="Android";android.maxTextureSize=512;android.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();
        }
        m.mainTextureScale=Vector2.one/.09f;m.SetFloat("_Glossiness",.10f);m.SetFloat("_Metallic",0);m.SetTexture("_MetallicGlossMap",null);m.DisableKeyword("_METALLICGLOSSMAP");EditorUtility.SetDirty(m);
    }
    static void RefinedPalette(Material jade,Material cream){
        Upholstery(jade,"FineSuede",new Color(.18f,.27f,.22f));Upholstery(cream,"FineSuede",new Color(.9f,.86f,.77f));
        fineLeather=Mat(Folder,"ShowcaseFineLeather",new Color(.19f,.13f,.085f),.16f);Upholstery(fineLeather,"FineLeather",fineLeather.color);
        edgePiping=Mat(Folder,"ShowcaseLeatherPiping",new Color(.13f,.085f,.055f),.14f);
        stitchThread=Mat(Folder,"ShowcaseLeatherStitch",new Color(.56f,.41f,.25f),.03f);stitchThread.mainTexture=DemoAssetLibrary.Texture("ArabicSceneSigns","LeatherStitch_Color.png");
        stitchThread.SetTexture("_BumpMap",DemoAssetLibrary.NormalMap("ArabicSceneSigns","LeatherStitch_Normal.png"));stitchThread.EnableKeyword("_NORMALMAP");stitchThread.SetFloat("_BumpScale",.12f);
    }
    static void Round(KitMesh k,Material m,Vector3 p,Vector3 size,float radius=.003f)=>k.RoundedBox(m,p,size,radius,Quaternion.identity);
    static void Pipe(KitMesh k,Material m,Vector3 a,Vector3 b,float radius=.0011f)=>k.Cylinder(m,a,b,radius,6,true);
    static void Stitch(KitMesh k,Vector3 a,Vector3 b,Vector3 normal){
        var side=Vector3.Cross(normal,b-a).normalized*.0012f;
        k.Quad(stitchThread,normal,a-side,a+side,b+side,b-side,new Rect(0,0,1,Vector3.Distance(a,b)/.006f));
    }
    static void Screw(KitMesh k,Material metal,Vector3 p,Vector3 normal,float radius=.0018f){
        k.Cylinder(metal,p-normal*.0005f,p+normal*.0005f,radius,8,true);
        var tangent=Vector3.Cross(normal,Mathf.Abs(normal.y)<.8f?Vector3.up:Vector3.forward).normalized;
        k.Stroke(StoreKit.DeviceBlack,p+normal*.0006f-tangent*radius*.65f,p+normal*.0006f+tangent*radius*.65f,.00035f,normal);
    }
    static void ArcShell(KitMesh k,Vector3 c,float halfLength,float radius){
        const int count=24;float wall=.0035f;
        for(int i=0;i<count;i++){
            float a=(90+i*180f/count)*Mathf.Deg2Rad,b=(90+(i+1)*180f/count)*Mathf.Deg2Rad;
            Vector3 A(float angle,float x,float r)=>c+new Vector3(x,Mathf.Cos(angle)*r,Mathf.Sin(angle)*r);
            var n=new Vector3(0,Mathf.Cos((a+b)*.5f),Mathf.Sin((a+b)*.5f));
            k.Face(fineLeather,n,A(a,-halfLength,radius),A(a,halfLength,radius),A(b,halfLength,radius),A(b,-halfLength,radius));
            k.Face(fineLeather,-n,A(a,-halfLength,radius-wall),A(b,-halfLength,radius-wall),A(b,halfLength,radius-wall),A(a,halfLength,radius-wall));
            foreach(float side in new[]{-1f,1f}){
                k.Face(edgePiping,Vector3.right*side,A(a,halfLength*side,radius),A(b,halfLength*side,radius),A(b,halfLength*side,radius-wall),A(a,halfLength*side,radius-wall));
                Pipe(k,edgePiping,A(a,halfLength*side,radius+.0005f),A(b,halfLength*side,radius+.0005f),.0014f);
            }
        }
        foreach(float side in new[]{-1f,1f})k.Box(edgePiping,c+new Vector3(0,0,side*(radius-wall*.5f)),new Vector3(halfLength*2,.0035f,wall));
    }
    static void RefinedInsert(KitMesh k,int index,float deck,Material soft,Material wood,Material metal){
        if(index==0){
            // One leather roll case with three removable bolsters; fully closed shell thickness.
            ArcShell(k,new Vector3(0,deck+.10f,0),.624f,.073f);
            Round(k,fineLeather,new Vector3(0,deck+.171f,.074f),new Vector3(1.248f,.142f,.010f),.004f);
            Round(k,soft,new Vector3(0,deck+.171f,.067f),new Vector3(1.218f,.116f,.004f),.0018f);
            for(int j=0;j<3;j++){
                float x=(j-1)*.40f;
                k.Lathe(soft,new Vector3(x-.17f,deck+.103f,0),Quaternion.Euler(0,0,-90),new[]{.044f,.056f,.058f,.058f,.056f,.044f},new[]{0f,.008f,.02f,.32f,.332f,.34f},24,true,true,true);
                foreach(float end in new[]{-.173f,.173f}){
                    k.Torus(edgePiping,new Vector3(x+end,deck+.103f,0),Quaternion.Euler(0,0,90),.045f,.045f,.0013f,24,5);
                    k.Cylinder(fineLeather,new Vector3(x+end-.001f,deck+.103f,0),new Vector3(x+end+.001f,deck+.103f,0),.044f,24,true);
                }
                Round(k,edgePiping,new Vector3(x,deck+.024f,0),new Vector3(.29f,.008f,.10f),.003f);
            }
            Pipe(k,edgePiping,new Vector3(-.62f,deck+.243f,.074f),new Vector3(.62f,deck+.243f,.074f),.0017f);
            Stitch(k,new Vector3(-.603f,deck+.234f,.0685f),new Vector3(.603f,deck+.234f,.0685f),Vector3.back);
            Round(k,fineLeather,new Vector3(0,deck+.216f,.062f),new Vector3(.034f,.048f,.007f),.003f);Screw(k,metal,new Vector3(0,deck+.207f,.0578f),Vector3.back,.0035f);
        }else if(index==1){
            for(int j=0;j<4;j++){
                float x=(j-1.5f)*.32f,h=.12f+j*.025f;
                Round(k,wood,new Vector3(x,deck+.033f,0),new Vector3(.215f,.026f,.18f),.006f);
                Round(k,soft,new Vector3(x,deck+.050f,0),new Vector3(.196f,.011f,.163f),.004f);
                foreach(float side in new[]{-1f,1f}){
                    k.Cylinder(metal,new Vector3(x+side*.07f,deck+.056f,0),new Vector3(x+side*.07f,deck+h-.012f,0),.0035f,10,true);
                    k.Lathe(metal,new Vector3(x+side*.07f,deck+.057f,0),Quaternion.identity,new[]{.007f,.007f,.004f},new[]{0f,.004f,.009f},10,true,true,true);
                    Pipe(k,metal,new Vector3(x+side*.07f,deck+h-.012f,0),new Vector3(x+side*.058f,deck+h,0),.0035f);
                    Pipe(k,metal,new Vector3(x+side*.045f,deck+h,0),new Vector3(x+side*.045f,deck+h-.017f,-.002f),.0018f);
                }
                Pipe(k,metal,new Vector3(x-.058f,deck+h,0),new Vector3(x+.058f,deck+h,0),.0035f);
            }
        }else if(index==2){
            for(int j=0;j<3;j++){
                float x=(j-1)*.43f;
                k.Loft(wood,new Vector3(x,deck+.021f,0),Quaternion.identity,new[]{.157f,.167f,.167f,.160f},new[]{.118f,.128f,.128f,.121f},new[]{0f,.007f,.042f,.049f},32,true,true);
                k.Loft(soft,new Vector3(x,deck+.071f,0),Quaternion.identity,new[]{.148f,.148f},new[]{.110f,.110f},new[]{-.004f,.002f},32,true,true);
                for(int roll=0;roll<5;roll++)k.Cylinder(soft,new Vector3(x-.122f,deck+.086f,(roll-2)*.037f),new Vector3(x+.122f,deck+.086f,(roll-2)*.037f),.017f,16,true);
                k.Torus(edgePiping,new Vector3(x,deck+.071f,0),Quaternion.identity,.153f,.115f,.0012f,40,5);
            }
        }else if(index==3){
            for(int j=0;j<5;j++){
                float x=(j-2)*.27f,h=.055f+.028f*(2-Mathf.Abs(j-2));
                Round(k,wood,new Vector3(x,deck+h*.5f,0),new Vector3(.23f,h,.25f),.004f);
                Round(k,metal,new Vector3(x,deck+h+.003f,0),new Vector3(.218f,.006f,.238f),.002f);
                Round(k,soft,new Vector3(x,deck+h+.013f,0),new Vector3(.21f,.014f,.23f),.006f);
            }
        }else if(index==4){
            for(int j=0;j<3;j++){
                float x=(j-1)*.42f;float z0=-.16f,depth=.32f;
                Vector3 Point(float u,float t)=>new Vector3(x+u*.15f,deck+.047f+.12f*t*t+.006f*(1-u*u)*Mathf.Sin(t*Mathf.PI),z0+t*depth);
                for(int segment=0;segment<12;segment++){
                    float a=segment/12f,b=(segment+1)/12f;
                    for(int across=0;across<6;across++){
                        float u=-1+across/3f,v=u+1/3f;var p=Point(u,a);var q=Point(v,a);var r=Point(v,b);var s=Point(u,b);
                        k.Quad(soft,Vector3.Cross(s-p,q-p).normalized,p,q,r,s,new Rect((u+1)*.15f,a*depth,.05f,depth/12));
                    }
                    foreach(float side in new[]{-1f,1f}){
                        var p=Point(side,a);var q=Point(side,b);k.Face(wood,Vector3.right*side,p,q,new Vector3(q.x,deck+.024f,q.z),new Vector3(p.x,deck+.024f,p.z));
                        Pipe(k,edgePiping,p+Vector3.up*.0007f,q+Vector3.up*.0007f,.0012f);
                    }
                }
                k.Face(wood,Vector3.down,new Vector3(x-.15f,deck+.024f,z0),new Vector3(x-.15f,deck+.024f,z0+depth),new Vector3(x+.15f,deck+.024f,z0+depth),new Vector3(x+.15f,deck+.024f,z0));
                foreach(float t in new[]{0f,1f}){var p=Point(-1,t);var q=Point(1,t);k.Face(wood,t==0?Vector3.back:Vector3.forward,p,q,new Vector3(q.x,deck+.024f,q.z),new Vector3(p.x,deck+.024f,p.z));}
                foreach(float side in new[]{-1f,1f}){
                    var p=Point(side*.72f,.82f);Round(k,fineLeather,p+Vector3.up*.003f,new Vector3(.023f,.006f,.041f),.0025f);
                    Screw(k,metal,p+Vector3.up*.0065f,Vector3.up,.0022f);
                }
            }
        }else if(index==5){
            for(int j=0;j<4;j++){
                float x=(j-1.5f)*.33f;
                Round(k,wood,new Vector3(x,deck+.035f,0),new Vector3(.26f,.03f,.25f),.005f);
                k.Loft(soft,new Vector3(x,deck+.05f,0),Quaternion.identity,new[]{.085f,.105f,.115f,.115f,.105f,.08f},new[]{.048f,.068f,.075f,.075f,.068f,.048f},new[]{0f,.01f,.025f,.045f,.06f,.07f},32,true,true);
                k.Torus(edgePiping,new Vector3(x,deck+.085f,0),Quaternion.identity,.115f,.075f,.0009f,40,5);
            }
        }else if(index==6){
            for(int j=0;j<3;j++){
                float x=(j-1)*.43f,h=.07f+j*.045f;
                k.Lathe(wood,new Vector3(x,deck+.021f,0),Quaternion.identity,new[]{.148f,.155f,.155f,.148f},new[]{0f,.007f,h-.007f,h},8,false,true,true,22.5f);
                k.Lathe(metal,new Vector3(x,deck+h+.022f,0),Quaternion.identity,new[]{.148f,.148f},new[]{0f,.003f},8,false,true,true,22.5f);
                k.Loft(soft,new Vector3(x,deck+h+.025f,0),Quaternion.identity,new[]{.134f,.142f,.142f,.134f},new[]{.134f,.142f,.142f,.134f},new[]{0f,.004f,.01f,.014f},32,true,true);
            }
        }else{
            for(int j=0;j<3;j++)RefinedPresentationBox(k,(j-1)*.43f,deck,soft,wood,metal);
        }
    }
    static void RefinedPresentationBox(KitMesh k,float x,float deck,Material soft,Material wood,Material metal){
        Round(k,wood,new Vector3(x,deck+.026f,-.015f),new Vector3(.30f,.014f,.24f),.004f);
        Round(k,soft,new Vector3(x,deck+.037f,-.015f),new Vector3(.274f,.008f,.212f),.003f);
        foreach(float side in new[]{-1f,1f}){
            Round(k,wood,new Vector3(x+side*.144f,deck+.055f,-.015f),new Vector3(.012f,.058f,.24f),.003f);
            Round(k,wood,new Vector3(x,deck+.055f,-.015f+side*.114f),new Vector3(.276f,.058f,.012f),.003f);
        }
        // Recessed split lining, four ring rolls and a separate empty compartment.
        Round(k,soft,new Vector3(x-.015f,deck+.055f,-.015f),new Vector3(.007f,.029f,.20f),.002f);
        for(int roll=0;roll<4;roll++)k.Cylinder(soft,new Vector3(x-.125f,deck+.057f,-.083f+roll*.045f),new Vector3(x-.025f,deck+.057f,-.083f+roll*.045f),.014f,12,true);
        var hinge=new Vector3(x,deck+.080f,.105f);var q=Quaternion.Euler(12,0,0);
        k.RoundedBox(wood,hinge+q*new Vector3(0,.078f,0),new Vector3(.30f,.156f,.013f),.004f,q);
        k.RoundedBox(soft,hinge+q*new Vector3(0,.078f,-.009f),new Vector3(.274f,.128f,.006f),.0025f,q);
        foreach(float side in new[]{-1f,1f}){
            float bx=x+side*.094f;
            for(int barrel=0;barrel<3;barrel++)k.Cylinder(metal,new Vector3(bx-.023f+barrel*.016f,deck+.080f,.106f),new Vector3(bx-.009f+barrel*.016f,deck+.080f,.106f),.0045f,10,true);
            Round(k,metal,new Vector3(bx,deck+.075f,.093f),new Vector3(.046f,.003f,.016f),.001f);
            foreach(float screw in new[]{-.013f,.013f})Screw(k,metal,new Vector3(bx+screw,deck+.077f,.091f),Vector3.up);
            var a=new Vector3(x+side*.132f,deck+.070f,.008f);var b=hinge+q*new Vector3(side*.132f,.080f,-.014f);
            Pipe(k,metal,a,b,.0018f);Screw(k,metal,a,Vector3.right*side,.0022f);
        }
        Round(k,metal,new Vector3(x,deck+.060f,-.138f),new Vector3(.023f,.018f,.004f),.002f);
        k.Stroke(StoreKit.DeviceBlack,new Vector3(x-.004f,deck+.060f,-.1402f),new Vector3(x+.004f,deck+.060f,-.1402f),.001f,Vector3.back);
    }
    static void PhotographicCollectionSigns(){
        string[] fixtures={"Sign_Left1","Sign_Left2","Sign_Right0"};string[] files={"CollectionEarrings_Photographic","Craftsmanship_Photographic","CollectionNecklaces_Photographic"};
        for(int i=0;i<fixtures.Length;i++){
            var frame=root.Find("Showroom/"+fixtures[i]);var group=Fresh("DistinctCollectionPrint",frame);var map=DemoAssetLibrary.Texture("ArabicSceneSigns",files[i]+".png");if(!map)throw new Exception("Photographic sign missing");TextureBudget(map);
            FramedPrint(group,files[i],map,new Vector3(0,.35f,-.055f),.8f,.8f*map.height/map.width);
        }
    }
    static void RepairSmallNormals(){
        var repaired=new System.Collections.Generic.Dictionary<Mesh,Mesh>();int count=0;
        foreach(var f in root.GetComponentsInChildren<MeshFilter>()){
            var r=f.GetComponent<Renderer>();if(!r||!InShop(r)||!f.sharedMesh)continue;
            if(r.sharedMaterials.All(m=>m&&m.shader.name!="Standard")){
                // Projected contacts intentionally have no lighting normals; keep their original meshes.
                string name=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(f.sharedMesh));
                if(name.StartsWith("RepairedMicroNormals_")){
                    var original=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(name.Substring("RepairedMicroNormals_".Length)));if(original)f.sharedMesh=original;
                }
                continue;
            }
            var source=f.sharedMesh;if(repaired.TryGetValue(source,out var saved)){f.sharedMesh=saved;continue;}
            var normals=source.normals;if(normals.Length!=source.vertexCount||!normals.Any(n=>n.sqrMagnitude<.01f))continue;
            var v=source.vertices;var sum=new Vector3[v.Length];var triangles=source.triangles;
            for(int i=0;i<triangles.Length;i+=3){
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];var area=Vector3.Cross(v[b]-v[a],v[c]-v[a]);sum[a]+=area;sum[b]+=area;sum[c]+=area;
            }
            for(int i=0;i<normals.Length;i++)if(normals[i].sqrMagnitude<.01f){normals[i]=sum[i].sqrMagnitude>1e-22f?sum[i]/Mathf.Sqrt(sum[i].sqrMagnitude):Vector3.up;count++;}
            Debug.Log("SMALL_NORMAL_MESH "+PathOf(f.transform)+" "+source.name);
            var mesh=Object.Instantiate(source);mesh.normals=normals;mesh.RecalculateTangents();
            string key=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));f.sharedMesh=Save(mesh,"RepairedMicroNormals_"+key);repaired[source]=f.sharedMesh;
        }
        Debug.Log("SMALL_SURFACE_NORMAL_REPAIR_PASS meshes="+repaired.Count+" vertices="+count+"; positions, UVs and topology preserved");
    }
    static void RefinementChecks(){
        var tiny=new ProceduralMesh();tiny.Box(null,Vector3.zero,Vector3.one*.001f);var tinyMesh=tiny.ToMesh("Millimetre fitting check");
        try{if(tinyMesh.normals.Any(n=>Mathf.Abs(n.magnitude-1)>.001f))throw new Exception("Millimetre-scale fitting normals lost precision");}finally{Object.DestroyImmediate(tinyMesh);}
        var test=new ProceduralMesh();test.RoundedBox(null,Vector3.zero,new Vector3(.3f,.02f,.24f),.004f,Quaternion.identity);var rounded=test.ToMesh("Rounded box check");
        try{
            var v=rounded.vertices;var normals=rounded.normals;var t=rounded.triangles;
            var edges=new System.Collections.Generic.Dictionary<string,int>();
            string Key(Vector3 p)=>$"{Mathf.RoundToInt(p.x*100000)},{Mathf.RoundToInt(p.y*100000)},{Mathf.RoundToInt(p.z*100000)}";
            for(int i=0;i<t.Length;i+=3){
                var face=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);
                if(face.sqrMagnitude<1e-16f||Vector3.Dot(face,normals[t[i]])<=0)throw new Exception("Rounded box has degenerate/inward geometry");
                for(int e=0;e<3;e++){
                    string a=Key(v[t[i+e]]),b=Key(v[t[i+(e+1)%3]]);string edge=string.CompareOrdinal(a,b)<0?a+"/"+b:b+"/"+a;
                    edges[edge]=edges.TryGetValue(edge,out int count)?count+1:1;
                }
            }
            if(edges.Values.Any(n=>n!=2))throw new Exception("Rounded box is not closed/manifold");
        }finally{Object.DestroyImmediate(rounded);}
        var group=root.Find("Dressing/BespokeShowcaseInserts");int triangles=0;
        foreach(var f in group.GetComponentsInChildren<MeshFilter>()){
            var mesh=f.sharedMesh;triangles+=mesh.triangles.Length/3;
            if(mesh.normals.Any(n=>Mathf.Abs(n.magnitude-1)>.02f))throw new Exception("Invalid refined surface normal: "+f.name+" bad="+mesh.normals.Count(n=>n.magnitude<.98f));
        }
        if(triangles>40000)throw new Exception("Refined display geometry budget exceeded: "+triangles);
        foreach(var r in group.GetComponentsInChildren<MeshRenderer>())foreach(var m in r.sharedMaterials.Where(m=>m&&m.name.Contains("FineLeather")))if(!m.mainTexture||!m.GetTexture("_BumpMap"))throw new Exception("Fine leather maps missing");
        Debug.Log("SHOWCASE_REFINEMENT_CHECKS_PASS eight inserts, triangles="+triangles+" / 40000; valid normals, leather maps, no new lights");
    }
    static void RefinementPreview(){
        var go=new GameObject("Refined accessory review");var camera=go.AddComponent<Camera>();NightQuality.Configure(camera);camera.stereoTargetEye=StereoTargetEyeMask.None;camera.fieldOfView=60;
        var target=new RenderTexture(1440,900,24);camera.targetTexture=target;var previous=RenderTexture.active;var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);var torch=lighting.FlashlightLight;
        var views=new[]{(new Vector3(.35f,1.3f,4.1f),new Vector3(0,.85f,5.2f)),(new Vector3(6.5f,1.18f,6.35f),new Vector3(7.6f,.83f,6.2f)),(new Vector3(-6.5f,1.15f,4.25f),new Vector3(-7.6f,.82f,4.4f)),(new Vector3(3.4f,1.15f,4.2f),new Vector3(4.6f,.83f,4.2f)),(new Vector3(-6.1f,2.05f,6.25f),new Vector3(-8,2.35f,6.4f)),(new Vector3(6.1f,2.05f,4.2f),new Vector3(8,2.35f,4.2f))};
        try{for(int i=0;i<views.Length;i++){
            camera.transform.position=views[i].Item1;camera.transform.LookAt(views[i].Item2);torch.transform.SetPositionAndRotation(camera.transform.position+camera.transform.right*.10f,camera.transform.rotation);camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes("Builds/refinement-"+i+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }}finally{RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
    }
    public static void ShowcaseReviewRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;
        RepairSmallNormals();ShopReviewChecks();RefinementChecks();NightStoreValidation.RepairChecks();NightStoreValidation.PresentationChecks();EditorSceneManager.SaveOpenScenes();
        RefinementPreview();EditableDelivery.Build();Debug.Log("SHOWCASE_REFINED_REVIEW_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void ShowcaseRefinementRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;StoreKit.Palette();
        ShopDisplayVariety();PhotographicCollectionSigns();RepairSmallNormals();ShopFinishes();ShopReviewChecks();RefinementChecks();
        if(TriangleAudit(false)!=0)throw new Exception("Refinement introduced duplicate faces");NightStoreValidation.RepairChecks();NightStoreValidation.PresentationChecks();
        AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
        if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();ShopReviewPreview();EditableDelivery.Build();
        Debug.Log("SHOWCASE_REFINEMENT_DELIVERY_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
