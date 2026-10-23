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
 static Texture2D BroadLightCookie(){
  string path=OmanFolder+"/BroadSoftBeam.png";const int n=256;var t=new Texture2D(n,n,TextureFormat.RGBA32,false,true);var pixels=new Color[n*n];
  for(int y=0;y<n;y++)for(int x=0;x<n;x++){float r=new Vector2((x+.5f)/n*2-1,(y+.5f)/n*2-1).magnitude;float feather=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.50f,1,r));float value=(.42f+.58f*Mathf.Exp(-r*r/.20f))*feather;pixels[y*n+x]=new Color(value,value,value,value);}
  t.SetPixels(pixels);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
  var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Cookie;imp.sRGBTexture=false;imp.wrapMode=TextureWrapMode.Clamp;imp.mipmapEnabled=true;imp.borderMipmap=true;imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 }
 static void BroaderPracticalLights(){
  var cookie=BroadLightCookie();foreach(var control in root.GetComponentsInChildren<DemoLighting>(true)){control.FlashlightCookie=cookie;control.UVTorchCookie=cookie;control.UVSpotAngle=68;EditorUtility.SetDirty(control);}
  foreach(var l in oman.GetComponentsInChildren<Light>()){
   if(l.name=="OmanStreetLamp"){l.spotAngle=150;l.innerSpotAngle=85;l.range=23;l.intensity=4.4f;l.shadowRadius=.28f;l.cookie=cookie;l.bounceIntensity=1.15f;}
   if(l.name=="ClosedShopExteriorLight"){l.spotAngle=130;l.innerSpotAngle=65;l.range=9;l.intensity=1.4f;l.shadowRadius=.18f;l.cookie=cookie;}
   EditorUtility.SetDirty(l);
  }
 }
 static void RemoveDuplicateDoorGlass(){
  foreach(string side in new[]{"Left","Right"}){var t=root.Find("Facade/EntranceDoors/Door"+side);var intact=t.Find("Glass");if(intact)Object.DestroyImmediate(intact.gameObject);var old=t.Find("DoorGlass_"+side+"_Broken");if(old)old.gameObject.SetActive(false);}
  var shards=root.Find("Facade/EntranceDoors/Shards");if(shards)shards.gameObject.SetActive(false);
 }
 static void FineGlassPiles(){
  var group=Fresh("FineGlassAccumulations",root.Find("Dressing"));var face=Mat(Folder,"FineGranuleFaces",new Color(.78f,.82f,.83f,.14f),.62f);face.shader=Shader.Find("CrimeScene/FractureGlint");face.SetFloat("_Smoothness",.62f);var edge=Mat(Folder,"FineGranuleEdges",new Color(.80f,.83f,.84f,.24f),.70f);edge.shader=Shader.Find("CrimeScene/FractureGlint");edge.SetFloat("_Smoothness",.70f);
  var bed=Mat(Folder,"DenseFineGlassContact",Color.white,.65f);bed.shader=Shader.Find("CrimeScene/GlassCrumbBed");bed.mainTexture=DemoAssetLibrary.Texture("OmanExterior","SafetyGlassFineOriginal.png");bed.SetFloat("_Density",.48f);bed.SetFloat("_Cutoff",.68f);var contact=new KitMesh{Lightmapped=false};
  var specs=new[]{(V(.02f,.001f,-.56f),.73f,.35f,1800),(V(-1.88f,.001f,4.60f),.46f,.30f,900),(V(5.15f,.001f,4.18f),.28f,.44f,900)};
  for(int i=0;i<specs.Length;i++){var s=specs[i];RecordingGlass.ArchitecturalDebris("FineGranulePile_"+i,group,s.Item1,2200+i,s.Item4,s.Item2,s.Item3,face,edge,true);var p=s.Item1;p.y=.0007f;float x=s.Item2*1.25f,z=s.Item3*1.3f;contact.Quad(bed,Vector3.up,p+V(-x,0,-z),p+V(x,0,-z),p+V(x,0,z),p+V(-x,0,z),new Rect(0,0,1,1));}
  var g=contact.Emit("FineGrainContact",group,Vector3.zero,Quaternion.identity,Folder,"fine_grain_accumulation_contact");g.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
 }
 static Material RefinedSurface(string name,string pack,string color,string normal,Color tint,float gloss,float relief,float metal=0){
  var m=Mat(Folder,name,tint,gloss);m.shader=Shader.Find("Standard");m.mainTexture=DemoAssetLibrary.Texture(pack,color);m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",gloss);m.SetTexture("_MetallicGlossMap",null);m.DisableKeyword("_METALLICGLOSSMAP");m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
  if(normal!=null){m.SetTexture("_BumpMap",DemoAssetLibrary.NormalMap(pack,normal));m.SetFloat("_BumpScale",relief);m.EnableKeyword("_NORMALMAP");}EditorUtility.SetDirty(m);return m;
 }
 static void ShopSurfaceMaterials(){
  var floor=RefinedSurface("RefinedRetailFloor","PBRInterior","FloorTile_Color.jpg","FloorTile_Normal.png",new Color(.76f,.75f,.70f),.30f,.16f);floor.mainTextureScale=Vector2.one*1.2f;root.Find("Architecture/Floor").GetComponent<MeshRenderer>().sharedMaterial=floor;
  var ceiling=RefinedSurface("RefinedMatteCeiling","PBRInterior","CeilingPaint_Color.jpg","CeilingPaint_Normal.png",new Color(.79f,.78f,.74f),.055f,.13f);ceiling.mainTextureScale=Vector2.one*1.5f;root.Find("Architecture/Ceiling/CeilingSlab").GetComponent<MeshRenderer>().sharedMaterial=ceiling;
  foreach(var r in root.GetComponentsInChildren<MeshRenderer>().Where(r=>PathOf(r.transform).Contains("VaseCeramic"))){
   var name=PathOf(r.transform).Contains("Tall")?"VaseCeramicTall":"VaseCeramicBottle";var m=RefinedSurface("Refined_"+name,"InteriorModels",name+"_Color.jpg",name+"_Normal.jpg",new Color(.77f,.78f,.76f),.34f,.15f);r.sharedMaterials=r.sharedMaterials.Select(s=>m).ToArray();
  }
  var hatch=Fresh("StaffCeilingServiceHatch",root.Find("Architecture/Ceiling"));var k=new KitMesh{LightmapScale=.5f};var trim=Mat(Folder,"CeilingHatchTrim",new Color(.48f,.48f,.45f),.12f);var screw=Mat(Folder,"CeilingHatchFastener",new Color(.27f,.28f,.29f),.25f);screw.SetFloat("_Metallic",.6f);
  var p=V(-5.7f,2.994f,10.0f);k.Bar(trim,p,V(.52f,.008f,.52f),.004f);k.Box(ceiling,p+Vector3.down*.005f,V(.49f,.004f,.49f));foreach(float x in new[]{-.22f,.22f})foreach(float z in new[]{-.22f,.22f})k.Cylinder(screw,p+V(x,-.008f,z),p+V(x,-.010f,z),.003f,8);var panel=k.Emit("SeatedServicePanel",hatch,Vector3.zero,Quaternion.identity,Folder,"staff_ceiling_service_panel");StoreKit.MarkStatic(panel,true);
 }
 static void RefineServerRack(){
  var rack=root.Find("StaffRoomFurnishings/NetworkServerRackRow");rack.localRotation=Quaternion.Euler(0,270,0);
  foreach(var r in rack.GetComponentsInChildren<MeshRenderer>()){
   var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){var m=new Material(mats[i]){name="RefinedDarkServer_"+i};m.color=new Color(.32f,.35f,.37f);m.SetFloat("_Glossiness",.18f);m.SetFloat("_Metallic",.15f);m.SetTexture("_MetallicGlossMap",null);m.DisableKeyword("_METALLICGLOSSMAP");m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);mats[i]=Save(m,m.name);}r.sharedMaterials=mats;
  }
  // The source scan has open wiring on both broad faces. Close the room-facing
  // face with rack-mounted equipment fronts; retain the scan behind the covers.
  var covers=Fresh("ServerFrontEnclosures",root.Find("StaffRoomFurnishings"));var front=new KitMesh{LightmapScale=.35f};
  var steel=RefinedSurface("ServerPowderCoatFront","PBRInterior","BrushedSteel_Color.jpg","BrushedSteel_Normal.png",new Color(.095f,.112f,.12f),.16f,.08f,.15f);var slots=Mat(Folder,"ServerVentRecess",new Color(.022f,.028f,.031f),.08f);var screw=Mat(Folder,"ServerFasteners",new Color(.20f,.23f,.25f),.27f);screw.SetFloat("_Metallic",.65f);var indicator=Mat(Folder,"ServerMutedIndicator",new Color(.06f,.15f,.11f),.16f);
  foreach(float x in new[]{-5.49f,-4.79f,-4.09f}){
   front.Bar(steel,V(x,.91f,10.50f),V(.66f,1.81f,.055f),.012f);
   foreach(float side in new[]{-1f,1f})front.Bar(slots,V(x+side*.295f,.91f,10.468f),V(.018f,1.69f,.012f),.004f);
   for(int unit=0;unit<10;unit++){
    float y=.145f+unit*.159f;front.Bar(steel,V(x,y,10.459f),V(.555f,.138f,.027f),.005f);
    for(int vent=0;vent<8;vent++)front.Box(slots,V(x-.18f+vent*.038f,y,10.4448f),V(.021f,.058f,.0015f));
    foreach(float side in new[]{-1f,1f})front.Cylinder(screw,V(x+side*.26f,y,10.445f),V(x+side*.26f,y,10.441f),.0045f,8);
    front.Box(indicator,V(x+.22f,y-.025f,10.444f),V(.012f,.006f,.002f));
   }
  }
  var body=front.Emit("ClosedRackEquipmentFronts",covers,Vector3.zero,Quaternion.identity,Folder,"server_closed_fronts");StoreKit.MarkStatic(body,true);
 }
 static void CasualFoodPlacement(){
  var original=root.Find("StaffRoomFurnishings/FoodPackets");var filter=original.GetComponentInChildren<MeshFilter>(true);var source=filter.sharedMesh;var verts=source.vertices;var tris=source.triangles;var intervals=new List<Vector2>();
  for(int i=0;i<tris.Length;i+=3){float a=Mathf.Min(verts[tris[i]].x,Mathf.Min(verts[tris[i+1]].x,verts[tris[i+2]].x)),b=Mathf.Max(verts[tris[i]].x,Mathf.Max(verts[tris[i+1]].x,verts[tris[i+2]].x));intervals.Add(new Vector2(a,b));}
  var ranges=new List<Vector2>();foreach(var span in intervals.OrderBy(v=>v.x)){if(ranges.Count>0&&span.x<=ranges[ranges.Count-1].y+.002f){var v=ranges[ranges.Count-1];v.y=Mathf.Max(v.y,span.y);ranges[ranges.Count-1]=v;}else ranges.Add(span);}
  if(ranges.Count!=4)throw new Exception("Expected four independent food packages, got "+ranges.Count);
  var parent=Fresh("CasualBreakTableItems",root.Find("StaffRoomFurnishings"));parent.localPosition=V(5.5f,.74f,10.4f);var normals=source.normals;var uv=source.uv;
  var placements=new[]{V(-.28f,0,.12f),V(.06f,0,-.16f),V(-.06f,0,.13f),V(-.28f,0,-.17f)};var angles=new[]{-19f,33f,-8f,102f};
  for(int part=0;part<4;part++){
   var index=new Dictionary<int,int>();var vv=new List<Vector3>();var nn=new List<Vector3>();var tt=new List<Vector2>();var faceIndices=new List<int>();
   for(int i=0;i<tris.Length;i+=3){float x=(verts[tris[i]].x+verts[tris[i+1]].x+verts[tris[i+2]].x)/3;if(x<ranges[part].x-.001f||x>ranges[part].y+.001f)continue;for(int j=0;j<3;j++){int a=tris[i+j];if(!index.TryGetValue(a,out int b)){b=vv.Count;index[a]=b;vv.Add(verts[a]);nn.Add(normals[a]);tt.Add(uv[a]);}faceIndices.Add(b);}}
   var mesh=new Mesh{name="SeparateSnack_"+part,vertices=vv.ToArray(),normals=nn.ToArray(),uv=tt.ToArray(),triangles=faceIndices.ToArray()};mesh.RecalculateBounds();var center=mesh.bounds.center;center.y=mesh.bounds.min.y;mesh.vertices=mesh.vertices.Select(v=>v-center).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();
   string path=Folder+"/SeparateSnack_"+part+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
   var g=new GameObject("Snack_"+part);g.transform.SetParent(parent,false);g.transform.localPosition=placements[part];g.transform.localRotation=Quaternion.Euler(0,angles[part],0);g.transform.localScale=Vector3.one*.72f;g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterials=filter.GetComponent<MeshRenderer>().sharedMaterials;r.lightProbeUsage=LightProbeUsage.BlendProbes;
  }
  original.gameObject.SetActive(false);
 }
 static void CleaningSupplies(){
  var group=Fresh("CleaningSupplies",root.Find("StaffRoomFurnishings"));
  foreach(var s in new[]{("Broom",V(-7.65f,0,10.15f),12f),("Dustpan",V(-7.35f,0,10.3f),-35f),("CleanerSprayBottle",V(-6.65f,.78f,9.38f),18f)}){
   var prop=UrbanDetailPass.Prop("AmbientProps2",s.Item1,group,s.Item2,s.Item3,1);if(!prop)throw new Exception("Cleaning asset missing "+s.Item1);
  }
  var plastic=Mat(Folder,"CleaningBucketPolymer",new Color(.13f,.22f,.26f),.16f);plastic.SetFloat("_Metallic",0);var steel=Mat(Folder,"CleaningHandleSteel",new Color(.25f,.27f,.28f),.28f);steel.SetFloat("_Metallic",.65f);var cloth=Mat(Folder,"CleaningMicrofiber",new Color(.23f,.34f,.33f),.07f);
  var bucket=Fresh("OpenCleaningBucket",group);bucket.localPosition=V(-7.43f,0,9.85f);var k=new KitMesh{LightmapScale=.5f};
  k.Lathe(plastic,Vector3.zero,Quaternion.identity,new[]{0f,.115f,.15f,.153f,.143f,.137f,.106f,0f},new[]{.012f,.012f,.275f,.295f,.300f,.275f,.035f,.035f},24,true,false,false);
  for(int i=0;i<16;i++){float a=Mathf.PI*i/16,b=Mathf.PI*(i+1)/16;k.Cylinder(steel,V(Mathf.Cos(a)*.148f,.25f+Mathf.Sin(a)*.18f,0),V(Mathf.Cos(b)*.148f,.25f+Mathf.Sin(b)*.18f,0),.004f,6);}
  k.Bar(cloth,V(.10f,.29f,.07f),V(.13f,.028f,.11f),.009f);var g=k.Emit("BucketAndFoldedCloth",bucket,Vector3.zero,Quaternion.identity,Folder,"cleaning_bucket_cloth");StoreKit.MarkStatic(g,true);
 }
 static void RefineSafes(){
  var shell=RefinedSurface("RefinedPowderCoatSafe","PBRInterior","SafeSteel_Color.jpg","SafeSteel_Normal.png",new Color(.47f,.49f,.50f),.21f,.22f,.30f);shell.mainTextureScale=Vector2.one*1.7f;
  var lining=RefinedSurface("RefinedSafeInterior","PBRInterior","BrushedSteel_Color.jpg","BrushedSteel_Normal.png",new Color(.22f,.25f,.27f),.16f,.09f,.3f);lining.mainTextureScale=Vector2.one*2;
  var hardware=Mat(Folder,"RefinedSafeLockSteel",new Color(.40f,.43f,.45f),.32f);hardware.SetFloat("_Metallic",.72f);var rubber=Mat(Folder,"SafeFrameGasket",new Color(.034f,.039f,.041f),.06f);var label=Mat(Folder,"SafeLockPlate",new Color(.14f,.16f,.17f),.24f);label.SetFloat("_Metallic",.5f);
  foreach(string name in new[]{"SafeLarge","SafeTall"}){
   var safe=root.Find("Office/"+name);bool large=name=="SafeLarge";float w=large?1.3f:.75f,h=large?1.5f:1.6f,d=large?.9f:.65f;float W=w-.188f,H=h-.238f,dy=.144f+H*.5f;
   foreach(var r in safe.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m.name.Contains("SafeSteel")||m.name=="RefinedPowderCoatSafe"?shell:m.name.Contains("SafeLining")||m.name=="RefinedSafeInterior"?lining:m.name.Contains("Bronze")||m.name.Contains("Stainless")?hardware:m).ToArray();
   var details=Fresh("RefinedFrameDetails",safe);var frame=new KitMesh{LightmapScale=.6f};
   foreach(float s in new[]{-1f,1f}){
    frame.Bar(rubber,V(s*(w*.5f-.085f),h*.5f+.025f,-d*.5f-.004f),V(.010f,h-.18f,.008f),.003f);
    frame.Bar(rubber,V(0,s<0?.137f:h-.087f,-d*.5f-.004f),V(w-.17f,.010f,.008f),.003f);
    foreach(float yy in new[]{.18f,h*.5f,h-.12f}){var c=V(s*(w*.5f-.035f),yy,-d*.5f-.002f);frame.Cylinder(hardware,c,c+Vector3.back*.003f,.0055f,8);frame.Stroke(rubber,c+V(-.003f,0,-.0035f),c+V(.003f,0,-.0035f),.001f,Vector3.back);}
   }
   frame.Emit("GasketAndFrameFasteners",details,Vector3.zero,Quaternion.identity,Folder,"safe_gasket_"+name);
   var door=safe.Find("Door");var panel=Fresh("RefinedDoorHardware",door);var metal=new KitMesh{Lightmapped=false};
   foreach(float xx in new[]{-W+.085f,-.085f})foreach(float yy in new[]{dy-H*.5f+.085f,dy,dy+H*.5f-.085f}){var c=V(xx,yy,.105f);metal.Cylinder(hardware,c,c+Vector3.forward*.004f,.006f,8);metal.Stroke(rubber,c+V(-.003f,0,.0045f),c+V(.003f,0,.0045f),.001f,Vector3.forward);}
   metal.Bar(label,V(-W*.5f,dy-H*.26f,-.008f),V(large?.21f:.15f,.045f,.006f),.003f);
   for(int i=0;i<5;i++)metal.Box(hardware,V(-W*.5f-.04f+i*.018f,dy-H*.26f,-.0118f),V(i%2==0?.008f:.012f,.0015f,.0004f));
   metal.Emit("LockPlateAndServiceFasteners",panel,Vector3.zero,Quaternion.identity,Folder,"safe_service_plate_"+name);
  }
 }
 static void RefineCarFinish(){
  var model=root.Find("Exterior/PoliceCar/SuppliedModel");ApplyRawCarMaterialOnly(model);foreach(var r in model.GetComponentsInChildren<MeshRenderer>()){r.receiveGI=ReceiveGI.LightProbes;r.lightmapIndex=-1;r.lightProbeUsage=LightProbeUsage.BlendProbes;foreach(var m in r.sharedMaterials){if(m.shader.name=="CrimeScene/RawCarClay")m.SetFloat("_Glossiness",0);EditorUtility.SetDirty(m);}}
 }
 static void ShopAtmosphereChecks(){
  foreach(string side in new[]{"Left","Right"}){var t=root.Find("Facade/EntranceDoors/Door"+side);if(t.Find("Glass"))throw new Exception("Duplicate door pane remains");if(t.GetComponentsInChildren<Transform>().Count(t=>t.name=="ArchitecturalFracturedPane")!=1)throw new Exception("Door needs exactly one fractured assembly");}
  var fine=root.Find("Dressing/FineGlassAccumulations");if(!fine||fine.GetComponentsInChildren<Collider>().Length!=0||fine.GetComponentsInChildren<MonoBehaviour>().Length!=0)throw new Exception("Fine glass must be combined non-physical scenery");
  foreach(var f in fine.GetComponentsInChildren<MeshFilter>()){if(!f.name.Contains("PileContact")&&f.sharedMesh.normals.Any(n=>n.sqrMagnitude<.5f))throw new Exception("Fine fragment normals invalid "+f.name);var b=f.GetComponent<MeshRenderer>().bounds;if(b.min.y<-.0001f||b.max.y>.09f)throw new Exception("Fine glass floats or sinks "+f.name+" "+b);}
  var staff=root.Find("StaffRoomFurnishings");if(staff.Find("FoodPackets").gameObject.activeSelf||staff.Find("CasualBreakTableItems").childCount!=4)throw new Exception("Straight food row remains");
  if(Mathf.Abs(Mathf.DeltaAngle(staff.Find("NetworkServerRackRow").localEulerAngles.y,270))>.1f)throw new Exception("Server rear faces room");
  if(staff.Find("CleaningSupplies").childCount!=4)throw new Exception("Cleaning tools incomplete");
  foreach(var control in root.GetComponentsInChildren<DemoLighting>(true))if(control.UVSpotAngle<65||!control.FlashlightCookie)throw new Exception("Narrow torch cone remains");
  foreach(var r in root.Find("Exterior/PoliceCar/SuppliedModel").GetComponentsInChildren<MeshRenderer>())if(r.receiveGI!=ReceiveGI.LightProbes||r.sharedMaterials.Any(m=>m.shader.name=="CrimeScene/RawCarClay"&&m.GetFloat("_Glossiness")!=0))throw new Exception("Car must use matte face shading and probe GI");
  foreach(string safe in new[]{"SafeLarge","SafeTall"})if(!root.Find("Office/"+safe+"/Body").GetComponent<MeshRenderer>().sharedMaterials.Any(m=>m.name=="RefinedSafeInterior"))throw new Exception("Safe interior finish not applied");
  if(!staff.Find("ServerFrontEnclosures/ClosedRackEquipmentFronts"))throw new Exception("Rack fronts remain exposed");
  Debug.Log("SHOP_ATMOSPHERE_CHECKS_PASS single door panes; 3600 fine grains in 3 batched piles; four casually placed food items; dark enclosed server; cleaning supplies; safe details; matte original car");
 }
 static void FineGlassVisibilityCheck(){
  var fine=root.Find("Dressing/FineGlassAccumulations");var go=new GameObject("Temporary fine glass visibility camera");var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.fieldOfView=58;c.transform.position=V(.3f,.45f,-1.4f);c.transform.LookAt(V(.02f,.02f,-.5f));var l=go.AddComponent<DemoLighting>();l.Flashlight(true);l.FlashlightLight.transform.SetPositionAndRotation(c.transform.position,c.transform.rotation);
  var rt=new RenderTexture(800,600,24){antiAliasing=4};c.targetTexture=rt;var previous=RenderTexture.active;
  Texture2D Capture(){c.Render();RenderTexture.active=rt;var t=new Texture2D(800,600,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,800,600),0,0);t.Apply();return t;}
  Texture2D a=null,b=null;try{a=Capture();fine.gameObject.SetActive(false);b=Capture();var av=a.GetPixels();var bv=b.GetPixels();int changed=0;for(int i=0;i<av.Length;i++)if(Mathf.Max(Mathf.Abs(av[i].r-bv[i].r),Mathf.Abs(av[i].g-bv[i].g),Mathf.Abs(av[i].b-bv[i].b))>.008f)changed++;if(changed<500)throw new Exception("Fine glass is too visually absent: "+changed);File.WriteAllBytes("Builds/fine-glass-visibility.png",a.EncodeToPNG());Debug.Log("FINE_GLASS_VISIBILITY_PASS changedPixels="+changed);}
  finally{fine.gameObject.SetActive(true);if(a)Object.DestroyImmediate(a);if(b)Object.DestroyImmediate(b);RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);l.ReleaseTorchResources();Object.DestroyImmediate(go);}
 }
 public static void ShopAtmosphereDeliveryRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");ShopAtmosphereChecks();FineGlassVisibilityCheck();RawCarChecks();GeneralSceneReview();NightStoreValidation.PresentationChecks();EditableDelivery.Build();Debug.Log("SHOP_ATMOSPHERE_LOCAL_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void ShopAtmosphereRefinementRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");string car=CarGeometrySignature();
  BroaderPracticalLights();RemoveDuplicateDoorGlass();FineGlassPiles();ShopSurfaceMaterials();RefineServerRack();CasualFoodPlacement();CleaningSupplies();RefineSafes();RefineCarFinish();OmanLegacyLeafCleanup();OmanBakeQuality();
  if(car!=CarGeometrySignature())throw new Exception("Original car geometry changed");ShopAtmosphereChecks();ArchitecturalGlassChecks();LightingSurfaceChecks();OmanChecks();GeneralSceneReview();ShopReviewChecks();RefinementChecks();RawCarChecks();NightStoreValidation.PresentationChecks();GrazingTorchShadows();
  AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();ShopAtmospherePreview("after");ArchitecturalGlassPreview("refined-shop");LightingSeamPreview("wider");OmanPreview();Debug.Log("SHOP_ATMOSPHERE_REVIEW_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void ShopAtmosphereInspectRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");var lines=new List<string>();
  foreach(var t in root.GetComponentsInChildren<Transform>(true)){
   string path=PathOf(t);if(!new[]{"EntranceDoors","StaffRoomFurnishings","SafeLarge","SafeTall","Vase","Architecture/Ceiling","Architecture/Floor","PoliceCar/Supplied"}.Any(path.Contains))continue;
   var r=t.GetComponent<MeshRenderer>();lines.Add(path+" active="+t.gameObject.activeInHierarchy+" pos="+t.position+" yaw="+t.eulerAngles+" scale="+t.lossyScale+(r?" bounds="+r.bounds+" GI="+r.receiveGI+" lm="+r.lightmapIndex+" mat="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name+"|"+m.shader.name+"|"+m.color+"|"+AssetDatabase.GetAssetPath(m):"NULL")):""));
  }
  File.WriteAllLines("Builds/shop-atmosphere-inspect.txt",lines);ShopAtmospherePreview("before");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void ShopAtmospherePreview(string label){
  var go=new GameObject("Temporary shop refinement camera");var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.fieldOfView=67;c.farClipPlane=100;
  var control=go.AddComponent<DemoLighting>();var sceneLighting=root.GetComponentsInChildren<DemoLighting>(true).FirstOrDefault();if(sceneLighting)control.FlashlightCookie=sceneLighting.FlashlightCookie;control.Flashlight(true);var torch=control.FlashlightLight;
  var rt=new RenderTexture(1400,950,24){antiAliasing=4};c.targetTexture=rt;var previous=RenderTexture.active;
  var views=new[]{(V(5.4f,1.65f,8.65f),V(5.6f,.9f,10.5f)),(V(-4.5f,1.65f,8.8f),V(-4.9f,1.1f,11.2f)),(V(0,1.65f,9),V(.4f,.8f,11.3f)),(V(-.4f,1.4f,-2),V(.05f,1.1f,0)),(V(.3f,.4f,-1.1f),V(.2f,.01f,-.1f)),(V(-3,1.5f,3),V(-1.9f,.4f,4.7f)),(V(0,1.6f,2.5f),V(1.2f,2.9f,4.5f)),(V(5.9f,1.6f,1.7f),V(7,.9f,1.1f)),(V(1,1.8f,-8),V(5,.8f,-4.65f)),(V(-6.2f,1.4f,8.7f),V(-7.45f,.65f,10.0f))};
  try{for(int i=0;i<views.Length;i++){c.transform.position=views[i].Item1;c.transform.LookAt(views[i].Item2);torch.transform.SetPositionAndRotation(c.transform.position,c.transform.rotation);c.Render();RenderTexture.active=rt;var image=new Texture2D(1400,950,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,950),0,0);image.Apply();File.WriteAllBytes("Builds/shop-refinement-"+label+"-"+i+".png",image.EncodeToPNG());Object.DestroyImmediate(image);}}
  finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);control.ReleaseTorchResources();Object.DestroyImmediate(go);}
 }
}
#endif
