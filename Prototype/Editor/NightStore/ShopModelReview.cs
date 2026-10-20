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

public static partial class PresentationPolish {
    static bool InShop(Renderer r)=>r.bounds.center.z>=-.3f&&r.bounds.center.z<=12.2f&&Mathf.Abs(r.bounds.center.x)<=9&&!PathOf(r.transform).Contains("/Exterior/");
    static void ShopFinishes(){
        var materials=new HashSet<Material>();int slots=0;
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>().Where(InShop)){
            if(!r.GetComponent<TextMesh>()&&(!r.GetComponent<MeshFilter>()||!r.GetComponent<MeshFilter>().sharedMesh))throw new Exception("Missing shop mesh: "+PathOf(r.transform));
            foreach(var m in r.sharedMaterials){
                if(!m||!m.shader||!m.shader.isSupported)throw new Exception("Missing/unsupported shop material: "+PathOf(r.transform));slots++;
                if(!materials.Add(m)||m.shader.name!="Standard")continue;
                string n=m.name.ToLowerInvariant();
                bool luminous=new[]{"screen","indicator","exit","led","lampface","litface","luminous"}.Any(n.Contains);
                if(!luminous){m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);m.SetTexture("_EmissionMap",null);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;}
                bool rough=new[]{"paper","kraft","cord","felt","fabric","suede","velvet","cardboard","rubber","cork","wicker"}.Any(n.Contains);
                if(rough){
                    m.SetFloat("_Metallic",0);m.SetTexture("_MetallicGlossMap",null);m.DisableKeyword("_METALLICGLOSSMAP");
                    m.SetFloat("_Glossiness",Mathf.Min(m.GetFloat("_Glossiness"),.12f));m.SetFloat("_GlossMapScale",.12f);
                    m.SetFloat("_SpecularHighlights",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                    m.SetFloat("_GlossyReflections",0);m.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
                }
                if(n.Contains("kraft")){m.mainTexture=DemoAssetLibrary.Texture("ArabicSceneSigns","KraftFiber_Color.png");m.color=Color.white;m.SetFloat("_Glossiness",.04f);}
                EditorUtility.SetDirty(m);
            }
            if(r.sharedMaterials.All(m=>m&&m.renderQueue<2450)){
                r.receiveShadows=true;r.lightProbeUsage=LightProbeUsage.BlendProbes;
                // Combined dressing meshes are at the store origin; sample lighting at their actual location.
                if(!r.probeAnchor&&!GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject,StaticEditorFlags.ContributeGI)){
                    var anchor=new GameObject("Surface lighting anchor").transform;anchor.SetParent(r.transform,false);anchor.position=r.bounds.center+Vector3.up*.025f;r.probeAnchor=anchor;
                }
                if(r.sharedMaterials.All(m=>m.name.Contains("Kraft")||m.name.Contains("Cord")))r.reflectionProbeUsage=ReflectionProbeUsage.Off;
            }
        }
        Debug.Log("SHOP_MATERIAL_AUDIT_PASS render slots="+slots+" unique materials="+materials.Count+"; rough paper/textile finishes, no accidental emission");
    }
    static void OfficeClearance(){
        // Preserve the original overturned evidence chair; the later furnishing occupied its footprint.
        var chair=root.Find("StaffRoomFurnishings/OfficeChair");if(chair)chair.gameObject.SetActive(false);
        var cabinet=root.Find("LivedInDetails/FilingCabinet_Detail_2");if(cabinet)cabinet.gameObject.SetActive(false);
        // Keep desk props on their support, away from the open safe doors and drawers.
        var phone=root.Find("LivedInDetails/DeskPhone_Detail_0");if(phone)phone.position=new Vector3(-2.22f,.75f,9.20f);
        var mouse=root.Find("LivedInDetails/Mouse_Detail_1");if(mouse)mouse.position=new Vector3(-1.42f,.75f,9.53f);
    }
    static void FramedPrint(Transform parent,string name,Texture texture,Vector3 centre,float width,float height){
        var board=Mat(Folder,"ShopPrintBacking",new Color(.018f,.03f,.025f),.1f);
        var edge=Mat(Folder,"ShopPrintBronze",new Color(.28f,.22f,.13f),.3f);edge.SetFloat("_Metallic",.65f);
        var face=Mat(Folder,"ShopPrint_"+name,Color.white,.09f);face.mainTexture=texture;face.DisableKeyword("_EMISSION");
        var k=new KitMesh{Lightmapped=false};
        k.Box(board,centre+Vector3.forward*.011f,new Vector3(width+.04f,height+.04f,.02f));
        for(int side=-1;side<=1;side+=2){
            k.Box(edge,centre+new Vector3(0,side*(height+.024f)*.5f,-.001f),new Vector3(width+.048f,.012f,.012f));
            k.Box(edge,centre+new Vector3(side*(width+.024f)*.5f,0,-.001f),new Vector3(.012f,height+.024f,.012f));
        }
        var x=Vector3.right*width*.5f;var y=Vector3.up*height*.5f;
        k.Quad(face,Vector3.back,centre-x-y,centre+x-y,centre+x+y,centre-x+y,new Rect(0,0,1,1));
        k.Emit(name,parent,Vector3.zero,Quaternion.identity,Folder,"shop_print_"+name);
    }
    static void ShopSigns(){
        var sign=root.Find("Facade/FasciaSign");sign.localPosition=new Vector3(0,3.22f,-.29f);sign.localScale=new Vector3(2.16f,.72f,1);
        var surround=Fresh("FasciaSignSurround",root.Find("Facade"));
        FramedPrint(surround,"FacadeWordmark",sign.GetComponent<Renderer>().sharedMaterial.mainTexture,sign.localPosition,2.16f,.72f);sign.gameObject.SetActive(false);
        // Remove the old lightbox's face AND deep box sides, rather than covering an incompatible frame.
        var filter=root.Find("Showroom/WallGraphics").GetComponent<MeshFilter>();var source=filter.sharedMesh;var vertices=source.vertices;var mesh=Object.Instantiate(source);
        for(int sub=0;sub<mesh.subMeshCount;sub++){
            var triangles=mesh.GetTriangles(sub);var keep=new List<int>();
            for(int i=0;i<triangles.Length;i+=3){
                var a=filter.transform.TransformPoint(vertices[triangles[i]]);var b=filter.transform.TransformPoint(vertices[triangles[i+1]]);var c=filter.transform.TransformPoint(vertices[triangles[i+2]]);
                bool Inside(Vector3 p)=>p.x>4.35f&&p.x<6.65f&&p.y>1.45f&&p.y<2.95f&&p.z>8.30f&&p.z<8.45f;
                if(Inside(a)&&Inside(b)&&Inside(c))continue;keep.Add(triangles[i]);keep.Add(triangles[i+1]);keep.Add(triangles[i+2]);
            }mesh.SetTriangles(keep,sub);
        }filter.sharedMesh=Save(mesh,"ShopWallGraphicsWithoutOldWordmark");
        var counter=Fresh("ArabicCounterBrand",root.Find("Showroom"));
        FramedPrint(counter,"InteriorWordmark",DemoAssetLibrary.Texture("ArabicSceneSigns","ShopFascia_Designed.png"),new Vector3(5.5f,2.25f,8.36f),2,.6666667f);
        string[] fixtures={"Sign_Left0","Sign_Left1","Sign_Left2","Sign_Right0","Sign_Right1"};
        string[] prints={"CollectionRings_Print","CollectionEarrings_Print","Craftsmanship_Print","CollectionNecklaces_Print","CollectionWatches_Print"};
        for(int i=0;i<fixtures.Length;i++){
            var frame=root.Find("Showroom/"+fixtures[i]);foreach(Transform child in frame)child.gameObject.SetActive(false);
            var display=Fresh("DistinctCollectionPrint",frame);var map=DemoAssetLibrary.Texture("ArabicSceneSigns",prints[i]+".png");if(!map)throw new Exception("Print texture failed to load: "+prints[i]);TextureBudget(map);
            FramedPrint(display,prints[i],map,new Vector3(0,.35f,-.055f),.80f,.52f);
        }
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true)){
            bool front=r.name=="ClosedSign",back=r.name=="ClosedSignBack";if(!front&&!back)continue;
            var m=Mat(Folder,front?"DoorClosedPrint":"DoorOpenPrint",Color.white,.08f);m.mainTexture=DemoAssetLibrary.Texture("ArabicSceneSigns",front?"Closed_Print.png":"Open_Print.png");TextureBudget(m.mainTexture);
            r.sharedMaterial=m;var p=r.transform.localPosition;p.z=front?-.032f:.032f;r.transform.localPosition=p;
            r.transform.localRotation=Quaternion.Euler(0,back?180:0,0);
        }
    }
    static void SmallMarkers(){
        var done=new HashSet<string>();
        foreach(var station in Object.FindObjectsByType<ToolStation>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            var prefab=station.MarkerPrefab;if(!prefab)continue;
            string path=AssetDatabase.GetAssetPath(prefab);if(!done.Add(path))continue;var loaded=PrefabUtility.LoadPrefabContents(path);
            try{if(DemoAssetLibrary.WorldBounds(loaded,out var bounds,false)&&bounds.size.y>.16f)foreach(Transform c in loaded.transform){c.localPosition*=.5f;c.localScale*=.5f;}PrefabUtility.SaveAsPrefabAsset(loaded,path);}finally{PrefabUtility.UnloadPrefabContents(loaded);}
        }
        foreach(var sample in root.GetComponentsInChildren<ToolRackSample>(true).Where(s=>s.Kind==DemoToolKind.Marker))if(DemoAssetLibrary.WorldBounds(sample.gameObject,out var b,false)&&b.size.y>.08f)foreach(Transform c in sample.transform){c.localPosition*=.5f;c.localScale*=.5f;}
    }
    static void ShopDisplayVariety(){
        var group=Fresh("BespokeShowcaseInserts",root.Find("Dressing"));
        var jade=new Material(StoreKit.Velvet){name="ShowcaseJadeVelvet"};jade.color=new Color(.22f,.34f,.27f);jade=Save(jade,jade.name);
        var cream=new Material(StoreKit.Suede){name="ShowcaseChampagneSuede"};cream.color=new Color(.75f,.68f,.53f);cream=Save(cream,cream.name);
        var smoke=new Material(StoreKit.Walnut){name="ShowcaseSmokedWalnut"};smoke.color=new Color(.50f,.47f,.43f);smoke.SetFloat("_Glossiness",.24f);smoke=Save(smoke,smoke.name);
        var nickel=new Material(StoreKit.Bronze){name="ShowcaseSatinNickel"};nickel.color=new Color(.46f,.48f,.47f);nickel.SetFloat("_Glossiness",.32f);nickel=Save(nickel,nickel.name);
        string[] cases={"CentralCase_Middle","RightIsland_Front","RightIsland_Rear","WallCase_Left0","WallCase_Left1","WallCase_Left2","WallCase_Right0","WallCase_Right1"};
        string[] designs={"WatchRollTravelCase","GraduatedEarringArches","OvalRingCabinet","TieredGemPedestals","NecklacePresentationRamp","BraceletPillows","OctagonalDisplayPlinths","HingedPresentationBoxes"};
        for(int index=0;index<cases.Length;index++){
            var fixture=root.Find("Showroom/"+cases[index]);var old=root.Find("Dressing/Displays/"+cases[index]);if(old)old.gameObject.SetActive(false);
            var parent=new GameObject(designs[index]).transform;parent.SetParent(group,false);parent.SetPositionAndRotation(fixture.position,fixture.rotation);
            var soft=index%2==0?jade:cream;var wood=index%3==0?smoke:StoreKit.Walnut;var metal=index%2==0?nickel:StoreKit.Bronze;
            var k=new KitMesh{Lightmapped=false};float deck=.712f;
            // Every insert is contained inside the existing case's usable 1.69 x .54 m deck.
            k.Bar(wood,new Vector3(0,deck+.01f,0),new Vector3(1.48f,.02f,.40f),.008f);
            if(index==0){
                for(int j=0;j<3;j++){
                    float x=(j-1)*.38f;k.Box(wood,new Vector3(x,deck+.07f,0),new Vector3(.30f,.10f,.28f));
                    k.Cylinder(soft,new Vector3(x-.12f,deck+.115f,0),new Vector3(x+.12f,deck+.115f,0),.055f,20,true);
                    for(int side=-1;side<=1;side+=2)k.Box(metal,new Vector3(x+side*.143f,deck+.105f,0),new Vector3(.008f,.12f,.16f));
                }
            }else if(index==1){
                for(int j=0;j<4;j++){
                    float x=(j-1.5f)*.32f,h=.12f+j*.025f;
                    k.Bar(soft,new Vector3(x,deck+.036f,0),new Vector3(.20f,.03f,.19f),.009f);
                    foreach(float side in new[]{-1f,1f})k.Cylinder(metal,new Vector3(x+side*.07f,deck+.05f,0),new Vector3(x+side*.07f,deck+h,0),.004f,8);
                    k.Cylinder(metal,new Vector3(x-.07f,deck+h,0),new Vector3(x+.07f,deck+h,0),.005f,8);
                    foreach(float side in new[]{-1f,1f})k.Cylinder(metal,new Vector3(x+side*.045f,deck+h,0),new Vector3(x+side*.045f,deck+h-.025f,-.004f),.0025f,6);
                }
            }else if(index==2){
                for(int j=0;j<3;j++){
                    float x=(j-1)*.43f;k.Lathe(wood,new Vector3(x,deck+.02f,0),Quaternion.identity,new[]{.17f,.17f},new[]{0f,.028f},24,true,true,true);
                    for(int roll=0;roll<5;roll++)k.Cylinder(soft,new Vector3(x-.12f,deck+.064f,(roll-2)*.045f),new Vector3(x+.12f,deck+.064f,(roll-2)*.045f),.021f,12,true);
                }
            }else if(index==3){
                for(int j=0;j<5;j++){
                    float x=(j-2)*.27f,h=.055f+.028f*(2-Mathf.Abs(j-2));
                    k.Bar(wood,new Vector3(x,deck+h*.5f,0),new Vector3(.23f,h,.25f),.006f);
                    k.Bar(soft,new Vector3(x,deck+h+.009f,0),new Vector3(.21f,.018f,.23f),.008f);
                }
            }else if(index==4){
                for(int j=0;j<3;j++){
                    float x=(j-1)*.42f;var q=Quaternion.Euler(-22,0,0);
                    k.Box(wood,new Vector3(x,deck+.068f,.055f),new Vector3(.26f,.10f,.12f));
                    k.ChamferBox(soft,new Vector3(x,deck+.10f,0),new Vector3(.30f,.022f,.33f),.009f,q);
                    foreach(float side in new[]{-1f,1f})k.Box(metal,new Vector3(x+side*.10f,deck+.167f,.135f),new Vector3(.018f,.006f,.018f));
                }
            }else if(index==5){
                for(int j=0;j<4;j++){
                    float x=(j-1.5f)*.33f;k.Bar(wood,new Vector3(x,deck+.035f,0),new Vector3(.26f,.03f,.25f),.008f);
                    k.Loft(soft,new Vector3(x,deck+.05f,0),Quaternion.identity,new[]{.085f,.105f,.115f,.115f,.105f,.08f},new[]{.048f,.068f,.075f,.075f,.068f,.048f},new[]{0f,.01f,.025f,.045f,.06f,.07f},24,true,true);
                }
            }else if(index==6){
                for(int j=0;j<3;j++){
                    float x=(j-1)*.43f,h=.07f+j*.045f;
                    k.Lathe(wood,new Vector3(x,deck+.02f,0),Quaternion.identity,new[]{.155f,.155f},new[]{0f,h},8,true,true,true);
                    k.Lathe(soft,new Vector3(x,deck+h+.02f,0),Quaternion.identity,new[]{.15f,.15f},new[]{0f,.012f},8,true,true,true);
                }
            }else{
                for(int j=0;j<3;j++){
                    float x=(j-1)*.43f;k.Box(wood,new Vector3(x,deck+.047f,-.02f),new Vector3(.29f,.055f,.23f));
                    k.Box(soft,new Vector3(x,deck+.079f,-.02f),new Vector3(.26f,.009f,.20f));
                    k.Box(wood,new Vector3(x,deck+.125f,.10f),new Vector3(.29f,.16f,.018f));
                    k.Box(soft,new Vector3(x,deck+.125f,.088f),new Vector3(.26f,.13f,.008f));
                    for(int side=-1;side<=1;side+=2)k.Cylinder(metal,new Vector3(x+side*.09f-.02f,deck+.068f,.10f),new Vector3(x+side*.09f+.02f,deck+.068f,.10f),.009f,10,true);
                }
            }
            k.Emit(designs[index],parent,Vector3.zero,Quaternion.identity,Folder,"bespoke_insert_"+designs[index]);
            // Cabinet finishes vary with their collection; hardware, glass and drawers keep their operation.
            foreach(var r in fixture.GetComponentsInChildren<MeshRenderer>()){
                var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i]&&mats[i].name=="KitWalnut")mats[i]=wood;r.sharedMaterials=mats;
            }
        }
        Debug.Log("BESPOKE_DISPLAYS_PASS eight original closed-base designs in existing case footprints; no extra lights or aisle obstructions");
    }
    static void ShopReviewChecks(){
        void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        Check(!root.Find("StaffRoomFurnishings/OfficeChair").gameObject.activeSelf,"Office chairs still overlap");
        Check(!root.Find("LivedInDetails/FilingCabinet_Detail_2").gameObject.activeSelf,"Cabinet occupies safe door clearance");
        var names=new[]{"Sign_Left0","Sign_Left1","Sign_Left2","Sign_Right0","Sign_Right1"};
        Check(names.Select(n=>root.Find("Showroom/"+n+"/DistinctCollectionPrint").GetComponentInChildren<MeshRenderer>().sharedMaterials.First(m=>m.name.StartsWith("ShopPrint_")).mainTexture).Distinct().Count()==5,"Repeated collection prints");
        Check(root.Find("Dressing/BespokeShowcaseInserts").childCount==8,"Display variety missing");
        var fascia=root.Find("Facade/FasciaSignSurround").GetComponentInChildren<MeshRenderer>();Check(fascia.bounds.max.y<3.7f&&fascia.bounds.max.z<-.25f,"Exterior wordmark touches upper facade decoration");
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="ClosedSign"||r.name=="ClosedSignBack")){
            Check(r.sharedMaterial.mainTexture.name==(r.name=="ClosedSign"?"Closed_Print":"Open_Print"),"Wrong door-card face");
            Check(Mathf.Abs(r.transform.localPosition.z)>.03f,"Door-card faces are coplanar");
        }
        foreach(Transform t in root.Find("Dressing/BespokeShowcaseInserts")){
            foreach(var f in t.GetComponentsInChildren<MeshFilter>())foreach(var v in f.sharedMesh.vertices)Check(Mathf.Abs(v.x)<=.8f&&Mathf.Abs(v.z)<=.255f&&v.y>=.70f&&v.y<=1.015f,"Showcase insert clips its glass/deck: "+t.name);
        }
        var station=Object.FindAnyObjectByType<ToolStation>();
        Check(DemoAssetLibrary.WorldBounds(station.MarkerPrefab.gameObject,out var b,false)&&b.size.y<.13f,"Marker remains oversized");
        var group=new GameObject("Temporary offset pickup check");var hand=group.AddComponent<HandInteractor>();
        var item=DemoToolGeometry.Create(DemoToolKind.Marker);
        try{
            hand.transform.SetPositionAndRotation(new Vector3(30,1,30),Quaternion.Euler(10,20,5));
            var p=new Vector3(30.12f,.91f,30.07f);var q=Quaternion.Euler(14,63,-8);item.transform.SetPositionAndRotation(p,q);item.HasBeenPlaced=true;
            var offset=hand.transform.InverseTransformPoint(p);var rotation=Quaternion.Inverse(hand.transform.rotation)*q;
            Physics.SyncTransforms();Check(hand.Grab(),"Placed marker cannot be selected at its real size");Check(Vector3.Distance(item.transform.position,p)<.0001f&&Quaternion.Angle(item.transform.rotation,q)<.01f,"Pickup snaps placed item");
            hand.transform.SetPositionAndRotation(new Vector3(31,.65f,29),Quaternion.Euler(-12,85,22));hand.FollowHand();
            p=hand.transform.TransformPoint(offset);q=hand.transform.rotation*rotation;
            Check(Vector3.Distance(item.transform.position,p)<.0001f&&Quaternion.Angle(item.transform.rotation,q)<.01f,"Grip offset does not follow controller delta");
            hand.Release();Check(item.Free&&Vector3.Distance(item.transform.position,p)<.0001f&&Quaternion.Angle(item.transform.rotation,q)<.01f,"Release changes placed item pose");
            var desktop=group.AddComponent<DesktopInteractor>();desktop.ReadDesktopInput=false;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(group.transform,false);floor.transform.position=new Vector3(31,-.05f,29);floor.transform.rotation=Quaternion.identity;floor.transform.localScale=new Vector3(6,.1f,6);Physics.SyncTransforms();
            desktop.Hold(item);desktop.Carry(new Ray(new Vector3(p.x,3,p.z),Vector3.down));
            Check(Vector3.Distance(item.transform.position,p)<.0001f&&Quaternion.Angle(item.transform.rotation,q)<.01f,"Desktop pickup snaps placed item");
            desktop.Carry(new Ray(new Vector3(p.x+.5f,3,p.z),Vector3.down));p+=Vector3.right*.5f;
            desktop.Place();Check(item.Free&&Vector3.Distance(item.transform.position,p)<.0001f&&Quaternion.Angle(item.transform.rotation,q)<.01f,"Desktop movement/release loses placement offset");
        }finally{hand.ReleaseAll();Object.DestroyImmediate(item.gameObject);Object.DestroyImmediate(group);}
        VRVisualValidation.CheckPreviewTorchOwnership();Debug.Log("SHOP_REVIEW_CHECKS_PASS five unique Arabic signs, office clearance, eight insert bounds, half-size markers, exact offset pickup/move/release");
    }
    public static void ShopFinalizeRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;StoreKit.Palette();
        ShopDisplayVariety();ShopFinishes();ShopReviewChecks();
        if(TriangleAudit(false)!=0)throw new Exception("Duplicate opaque faces remain");
        NightStoreValidation.RepairChecks();NightStoreValidation.PresentationChecks();AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
        ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();ShopReviewPreview();SignPreview();EditableDelivery.Build();
        Debug.Log("SHOP_FINAL_DELIVERY_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void ShopReviewPreview(){
        var go=new GameObject("Shop review camera");var camera=go.AddComponent<Camera>();NightQuality.Configure(camera);camera.stereoTargetEye=StereoTargetEyeMask.None;camera.fieldOfView=62;camera.farClipPlane=220;
        var rt=new RenderTexture(1440,900,24);camera.targetTexture=rt;var previous=RenderTexture.active;var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);var torch=lighting.FlashlightLight;
        var views=new[]{(new Vector3(.3f,1.65f,9.35f),new Vector3(.2f,.9f,11.2f)),(new Vector3(0,1.65f,-4.3f),new Vector3(0,3.15f,0)),(new Vector3(4.8f,1.7f,6.5f),new Vector3(5.5f,2.25f,8.36f)),(new Vector3(-6.1f,1.8f,4.2f),new Vector3(-8,2.35f,4.4f)),(new Vector3(2.3f,.8f,6.35f),new Vector3(2,.05f,7.2f)),(new Vector3(.25f,1.45f,4.15f),new Vector3(0,.85f,5.2f)),(new Vector3(-6.3f,1.25f,5.8f),new Vector3(-7.6f,.85f,6.4f)),(new Vector3(6.2f,1.2f,5.5f),new Vector3(7.6f,.85f,6.2f))};
        try{Directory.CreateDirectory("Builds");for(int i=0;i<views.Length;i++){camera.transform.position=views[i].Item1;camera.transform.LookAt(views[i].Item2);torch.enabled=true;torch.transform.SetPositionAndRotation(camera.transform.position+camera.transform.right*.08f,camera.transform.rotation);camera.Render();RenderTexture.active=rt;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes("Builds/shop-review-"+i+".png",image.EncodeToPNG());Object.DestroyImmediate(image);}}
        finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
    }
    public static void ShopRepairRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;StoreKit.Palette();
        OfficeClearance();ShopSigns();SmallMarkers();ShopDisplayVariety();TriangleAudit(true);ShopFinishes();Physics.SyncTransforms();
        ShopReviewChecks();NightStoreValidation.RepairChecks();NightStoreValidation.PresentationChecks();
        AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
        if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();
        ShopReviewPreview();EditableDelivery.Build();Debug.Log("SHOP_REPAIR_DELIVERY_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void ShopAuditRun(){try{
        EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;
        var lines=new List<string>();
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){
            if(r.bounds.center.z<-.3f||r.bounds.center.z>12.2f||Mathf.Abs(r.bounds.center.x)>9)continue;
            lines.Add(PathOf(r.transform)+" position="+r.transform.position+" scale="+r.transform.lossyScale+" bounds="+r.bounds+" lightmap="+r.lightmapIndex);
            foreach(var m in r.sharedMaterials)lines.Add(m?"  "+m.name+" shader="+m.shader.name+" tex="+(m.mainTexture?m.mainTexture.name:"none")+" color="+m.color+" gloss="+(m.HasProperty("_Glossiness")?m.GetFloat("_Glossiness"):0)+" metal="+(m.HasProperty("_Metallic")?m.GetFloat("_Metallic"):0)+" emission="+(m.HasProperty("_EmissionColor")?m.GetColor("_EmissionColor"):Color.black):"  MISSING MATERIAL");
        }
        Directory.CreateDirectory("Builds");File.WriteAllLines("Builds/shop-model-audit.txt",lines);TriangleAudit(false);RepairPreview();SignPreview();
        Debug.Log("SHOP_AUDIT_PASS");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static int TriangleAudit(bool repair){
        var seen=new Dictionary<string,(string path,Vector3 normal)>();var lines=new List<string>();int removed=0;
        foreach(var f in root.GetComponentsInChildren<MeshFilter>()){
            var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled||!f.sharedMesh)continue;
            string path=PathOf(f.transform);if(path.Contains("/LOD1/")||path.Contains("/LOD2/")||path.Contains("/Exterior/"))continue;
            var source=f.sharedMesh;var v=source.vertices;Mesh copy=null;
            for(int sub=0;sub<source.subMeshCount;sub++){
                if(sub>=r.sharedMaterials.Length)continue;var m=r.sharedMaterials[sub];if(!m||m.renderQueue>=2450)continue;
                var keep=new List<int>();var tri=source.GetTriangles(sub);
                for(int i=0;i<tri.Length;i+=3){
                    var p=new[]{f.transform.TransformPoint(v[tri[i]]),f.transform.TransformPoint(v[tri[i+1]]),f.transform.TransformPoint(v[tri[i+2]])};
                    var n=Vector3.Cross(p[1]-p[0],p[2]-p[0]).normalized;
                    string key=m.GetEntityId()+":"+string.Join("/",p.Select(a=>$"{Mathf.RoundToInt(a.x*10000)},{Mathf.RoundToInt(a.y*10000)},{Mathf.RoundToInt(a.z*10000)}").OrderBy(a=>a));
                    bool duplicate=seen.TryGetValue(key,out var first)&&Vector3.Dot(n,first.normal)>.99f;
                    if(duplicate){removed++;lines.Add(first.path+" == "+path+" material="+m.name);}else seen[key]=(path,n);
                    if(!duplicate||!repair){keep.Add(tri[i]);keep.Add(tri[i+1]);keep.Add(tri[i+2]);}
                }
                if(repair&&keep.Count!=tri.Length){if(!copy)copy=Object.Instantiate(source);copy.SetTriangles(keep,sub);}
            }
            if(copy){copy.RecalculateBounds();f.sharedMesh=Save(copy,"CleanSurface_"+source.name+"_"+Animator.StringToHash(path));}
        }
        File.WriteAllLines("Builds/shop-duplicate-surfaces.txt",lines);Debug.Log("SHOP_DUPLICATE_SURFACES "+removed+" repair="+repair);return removed;
    }
}
#endif
