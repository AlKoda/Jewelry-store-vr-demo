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
 static void AddShopSearchActivities(EvidenceRegister register){
  foreach(var old in root.GetComponentsInChildren<UVDiscoverableClue>(true).Where(c=>c.name.StartsWith("UVSearch_")))Object.DestroyImmediate(old.gameObject);
  var group=Fresh("ShopSearchDetails",register.transform);var existing=register.Clues.Where(c=>c&&int.Parse(c.Id.Substring(1))<15).ToList();
  var office=register.Containers.First(c=>c&&c.name=="OfficeDocumentBox");
  var shell=RefinedSurface("RepairBoxFabricCover","PBRInterior","VelvetCharcoal_Color.jpg","VelvetCharcoal_Normal.png",new Color(.24f,.115f,.075f),.12f,.13f);
  var lining=RefinedSurface("RepairBoxLining","PBRInterior","VelvetCharcoal_Color.jpg","VelvetCharcoal_Normal.png",new Color(.24f,.27f,.23f),.04f,.18f);
  var box=FieldBox(group,"WatchRepairBox",V(4.78f,.952f,7.96f),V(.29f,.10f,.25f),shell,lining);
  var details=new KitMesh{Lightmapped=false};var brass=Mat(Folder,"RepairBoxSatinBrass",new Color(.40f,.30f,.13f),.29f);brass.SetFloat("_Metallic",.68f);
  foreach(float x in new[]{-.105f,.105f}){
   details.Cylinder(brass,V(x-.015f,.10f,.125f),V(x+.015f,.10f,.125f),.004f,10);
   foreach(float z in new[]{-.09f,.09f})details.Cylinder(brass,V(x,.001f,z),V(x,-.001f,z),.003f,8);
  }
  details.Bar(lining,V(.073f,.020f,0),V(.004f,.014f,.20f),.001f);details.Emit("HingesFastenersAndInteriorDivider",box.transform,Vector3.zero,Quaternion.identity,Folder,"repair_box_hardware");
  var label=PaperSheet(box.Lid,"RepairIntake",V(-.02f,-.006f,-.13f),new Vector2(.06f,.08f),0);label.transform.localRotation=Quaternion.Euler(180,0,0);
  var pin=Group("LooseWatchSpringBar",box.transform);pin.localPosition=V(-.025f,.0165f,-.012f);pin.localRotation=Quaternion.Euler(0,32,0);var steel=Mat(Folder,"WatchSpringBarSteel",new Color(.39f,.42f,.44f),.36f);steel.SetFloat("_Metallic",.82f);var p=new KitMesh{Lightmapped=false};p.Cylinder(steel,V(-.012f,0,0),V(.012f,0,0),.0013f,10);foreach(float side in new[]{-1f,1f})p.Cylinder(steel,V(side*.011f,0,0),V(side*.016f,0,0),.00065f,8);p.Emit("TelescopingSteelPin",pin,Vector3.zero,Quaternion.identity,Folder,"loose_watch_spring_bar");
  var pinClue=Clue(group,register,"E17","Loose watch spring bar","دبوس ساعة منفصل","A loose spring bar in the repair box. Its presence may be routine servicing; record it before collecting.","دبوس منفصل في علبة الصيانة. قد يكون من عمل صيانة معتاد؛ سجل موضعه قبل جمعه.",pin.position,V(.065f,.035f,.065f),pin.gameObject,true);pinClue.Container=box;existing.Add(pinClue);
  var thread=Group("ThreadAtDocumentBoxEdge",group);thread.position=office.transform.TransformPoint(V(.078f,.0165f,-.036f));var fabric=Mat(Folder,"RepairBoxThread",new Color(.07f,.13f,.20f),.035f);var k=new KitMesh{Lightmapped=false};for(int i=0;i<3;i++)k.Cylinder(fabric,V(i*.0015f,0,0),V(.01f+i*.0015f,.0007f,.008f+i*.002f),.0005f,5);k.Emit("ShortBlueThreads",thread,Vector3.zero,Quaternion.identity,Folder,"document_box_thread");
  var fibre=Clue(group,register,"E18","Thread inside document box","خيط داخل صندوق الأوراق","A short blue thread beside the paperwork. Similar colour alone does not establish a common source.","خيط أزرق قصير بجوار الأوراق. تشابه اللون وحده لا يثبت وحدة المصدر.",thread.position,V(.055f,.025f,.055f),thread.gameObject,true);fibre.Container=office;existing.Add(fibre);
  foreach(var spec in new[]{("E15","EvidenceUV_StaffDoorPrint","Staff door fluorescent mark","أثر فلوري على باب الموظفين"),("E16","EvidenceUV_BackgroundPrint_18","Counter fluorescent mark","أثر فلوري على المنضدة")}){
   var trace=root.GetComponentsInChildren<UVTrace>(true).First(t=>t.name==spec.Item2);var target=Clue(group,register,spec.Item1,spec.Item3,spec.Item4,"A fluorescence cue on a frequently touched surface. It can guide examination; it does not establish a substance or identity.","إشارة فلورية على سطح كثير اللمس. توجه الفحص ولا تثبت مادة معينة أو هوية.",trace.transform.position,V(.075f,.075f,.04f));target.name="UVSearch_"+spec.Item1;target.transform.SetParent(trace.transform.parent,true);target.transform.rotation=trace.transform.rotation;
   var discovery=target.gameObject.AddComponent<UVDiscoverableClue>();discovery.Trace=trace;existing.Add(target);
  }
  register.Clues=existing.OrderBy(c=>c.Id).ToArray();register.Containers=new[]{office,box};
  var packets=register.SealedPackets.Where(p=>p).ToList();while(packets.Count<10){var packet=Object.Instantiate(packets[0],packets[0].transform.parent);packet.name="SealedPacket_"+(packets.Count+1);packet.transform.localPosition=V(0,packets.Count*.008f,0);packet.SetActive(false);packets.Add(packet);}register.SealedPackets=packets.ToArray();
  foreach(var r in group.GetComponentsInChildren<MeshRenderer>(true)){r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;r.gameObject.layer=CrowdLayer;}
  var state=Object.FindFirstObjectByType<CrimeSceneState>();if(state)state.RobbedOnly=state.RobbedOnly.Where(g=>g).Concat(new[]{pinClue.gameObject,pin.gameObject,fibre.gameObject,thread.gameObject}).Distinct().ToArray();register.RefreshPage();
 }
 static void ShopDoorAndSurfaceDetails(){
  RemoveDuplicateDoorGlass();
  foreach(var spec in new[]{("Left",1f),("Right",-1f)}){
   var leaf=root.Find("Facade/EntranceDoors/Door"+spec.Item1);var group=Fresh("RepairedPullMountsAndHangingSign",leaf);var mesh=new KitMesh{Lightmapped=false};var bronze=Mat(Folder,"DoorSatinMounts",new Color(.27f,.22f,.14f),.34f);bronze.SetFloat("_Metallic",.72f);
   foreach(float y in new[]{.80f,1.30f})foreach(float side in new[]{-1f,1f})mesh.Cylinder(bronze,V(spec.Item2*.88f,y,-.025f),V(spec.Item2*.815f,y,-.025f+side*.06f),.0055f,10);
   if(spec.Item1=="Left"){
    var front=leaf.Find("ClosedSign");var back=leaf.Find("ClosedSignBack");var p=front.localPosition;p.z=-.031f;front.localPosition=p;var q=back.localPosition;q.z=-.019f;back.localPosition=q;
    var board=Mat(Folder,"DoorHangingSignBacking",new Color(.035f,.042f,.038f),.16f);mesh.Bar(board,V(p.x,p.y,-.025f),V(.412f,.212f,.009f),.003f);
    foreach(float x in new[]{-.145f,.145f})mesh.Cylinder(bronze,V(p.x+x,p.y+.108f,-.025f),V(p.x+x,2.285f,-.025f),.0012f,6);
   }mesh.Emit("ThroughMountedHardware",group,Vector3.zero,Quaternion.identity,Folder,"door_remnant_mount_"+spec.Item1);
   if(spec.Item1=="Left")foreach(string name in new[]{"EvidenceUV_WipedGlassSmear","EvidenceUV_GlassDrop"}){var trace=leaf.Find(name);if(trace)trace.localPosition=V(.88f,name.Contains("Drop")?.55f:.90f,-.052f);}
  }
  foreach(var m in root.Find("Showroom").GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader.name=="Standard"&&m.name=="KitFrameMetal").Distinct()){
   m.SetFloat("_Glossiness",.26f);m.SetFloat("_Metallic",.46f);EditorUtility.SetDirty(m);
  }
  var crumbs=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/DenseFineGlassContact.mat");if(crumbs){crumbs.SetFloat("_Density",.32f);EditorUtility.SetDirty(crumbs);}
  // Soften static practical shadows without adding realtime light sources or
  // powering the intentionally dark shop fixtures.
  foreach(var light in oman.GetComponentsInChildren<Light>()){
   if(light.name=="OmanStreetLamp"){light.shadowRadius=.42f;light.bounceIntensity=1.25f;}
   if(light.name=="ClosedShopExteriorLight"){light.shadowRadius=.24f;light.bounceIntensity=1.15f;}EditorUtility.SetDirty(light);
  }
 }
 static void ShopSearchChecks(EvidenceRegister register){
  void Require(bool value,string message){if(!value)throw new Exception("Shop search: "+message);}
  bool persist=register.PersistRecords;register.PersistRecords=false;var input=new GameObject("Temporary shop search hand");var hand=input.AddComponent<HandInteractor>();GameObject block=null;
  try{
   register.ResetInvestigation();var forceps=register.Tools.First(t=>t.Kind==FieldInvestigationTool.Method.Forceps);input.transform.position=forceps.transform.position+Vector3.up*.04f;Require(hand.TakeFieldTool(forceps),"forceps unavailable");
   foreach(string id in new[]{"E17","E18"}){
    var clue=register.Clues.First(c=>c.Id==id);input.transform.position=clue.transform.position+Vector3.up*.25f;input.transform.LookAt(clue.transform.position);forceps.Follow(input.transform);Physics.SyncTransforms();
    Require(!clue.Sample(forceps),"contents collectible through a closed box");clue.Container.Toggle();clue.Container.Lid.localRotation=Quaternion.Euler(clue.Container.OpenDegrees,0,0);Physics.SyncTransforms();
    Require(InvestigationInteraction.Target(new Ray(input.transform.position,input.transform.forward),.65f,hand,out var hit)&&hit.collider.GetComponentInParent<EvidenceClue>()==clue,"open-box specimen cannot be reached");
    Require(clue.Sample(forceps)&&!clue.Specimen.activeSelf,"opened-box specimen not sealed");clue.Container.Close();
   }Require(register.SampleCount==2,"box samples not counted");hand.Release();
   foreach(var clue in register.Clues.Where(c=>c.GetComponent<UVDiscoverableClue>())){
    var gate=clue.GetComponent<UVDiscoverableClue>();gate.Trace.SendMessage("OnEnable");UVTrace.HideAll();Require(!clue.Available&&!clue.Inspect(),"UV search can be completed with UV off");
    var position=gate.Trace.transform.position;var origin=position+gate.Trace.Normal*.35f;var direction=-gate.Trace.Normal;
    block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.position=Vector3.Lerp(origin,position,.5f);block.transform.localScale=Vector3.one*.09f;Physics.SyncTransforms();UVTrace.UpdateAll(origin,direction,Mathf.Cos(34*Mathf.Deg2Rad),2,1);Require(!clue.Available,"UV search sees through opaque obstruction");Object.DestroyImmediate(block);block=null;Physics.SyncTransforms();
    UVTrace.UpdateAll(origin,direction,Mathf.Cos(34*Mathf.Deg2Rad),2,1);Require(gate.Trace.Visibility>.08f&&clue.Inspect()&&gate.Discovered,"actual UV cone does not unlock observation");UVTrace.HideAll();Require(clue.Available,"a recorded UV finding disappears when torch switches off");
   }
   bool ar=DemoLanguage.Arabic;try{DemoLanguage.Arabic=false;for(int i=0;i<register.ObservedCount+1;i++)register.NextPage();bool overview=false;for(int i=0;i<register.ObservedCount+1;i++){if(register.PageText.text.Contains("Sealed samples:"))overview=true;register.NextPage();}Require(overview,"field register has no survey overview");var latest=register.Clues.First(c=>c.Id=="E17");register.Record(latest,false);Require(register.PageText.text.Contains("E17")&&register.PageText.characterSize<=.0027f,"register does not select / fit latest entry");}finally{DemoLanguage.Arabic=ar;}
   register.ResetInvestigation();UVTrace.HideAll();Require(register.Clues.Where(c=>c.GetComponent<UVDiscoverableClue>()).All(c=>!c.Available&&!c.GetComponent<UVDiscoverableClue>().Discovered),"reset retains discovered UV targets");Require(register.Containers.Length==2&&register.Containers.All(c=>!c.IsOpen),"search lids not restored");
   Debug.Log("SHOP_SEARCH_BEHAVIOUR_PASS closed/open container reach; two forceps samples; UV-off and wall rejection; actual UV cone unlock; remembered observation; survey overview; complete reset");
  }finally{if(block)Object.DestroyImmediate(block);hand.ReleaseAll();Object.DestroyImmediate(input);register.ResetInvestigation();UVTrace.HideAll();register.PersistRecords=persist;}
 }
 public static void ShopDetailRefinementRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");string car=CarGeometrySignature();
  RefineArchitecturalGlass();VisibleGlass();ShopDoorAndSurfaceDetails();RefineShopGlassFamily();AddShopSearchActivities(root.Find("InvestigationFieldwork").GetComponent<EvidenceRegister>());Physics.SyncTransforms();
  FieldworkChecks();GlassFamilyChecks();InvestigationChecks();NightStoreValidation.PresentationChecks();if(car!=CarGeometrySignature())throw new Exception("Protected car changed");
  EditorSceneManager.MarkSceneDirty(root.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();ShopDetailPreview("after");Debug.Log("SHOP_DETAIL_REFINEMENT_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void ShopDetailDeliveryRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");GlassFamilyChecks();FineGlassVisibilityCheck();FieldworkChecks();InvestigationChecks();NightStoreValidation.PresentationChecks();EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");EditableDelivery.Build();Debug.Log("SHOP_DETAIL_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void ShopDetailInspectRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");
  var lines=new List<string>();foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){
   if(r.sharedMaterials.Any(m=>m&&m.shader.name=="CrimeScene/DisplayGlass")){var f=r.GetComponent<MeshFilter>();lines.Add("GLASS_AUDIT "+PathOf(r.transform)+" mesh="+(f?f.sharedMesh.name:"none")+" triangles="+(f?f.sharedMesh.triangles.Length/3:0)+" bounds="+(f?f.sharedMesh.bounds.ToString():"none")+" mats="+string.Join(",",r.sharedMaterials.Select(m=>m.name)));}
   var path=PathOf(r.transform);if(path.Contains("EntranceDoors")||path.Contains("Showroom/Counter")||r.bounds.Intersects(new Bounds(V(5.5f,1.1f,7.85f),V(2.8f,.7f,1.4f))))lines.Add(path+" bounds="+r.bounds+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m.name+"|"+m.shader.name+"|"+m.color)));
  }foreach(var l in root.GetComponentsInChildren<Light>(true))lines.Add("LIGHT "+PathOf(l.transform)+" enabled="+l.enabled+" mode="+l.lightmapBakeType+" type="+l.type+" intensity="+l.intensity+" range="+l.range+" shadows="+l.shadows+" pos="+l.transform.position);
  File.WriteAllLines("Builds/shop-detail-inspect.txt",lines);ShopDetailPreview("before");Debug.Log("SHOP_DETAIL_INSPECT_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void ShopDetailPreview(string suffix){
  var go=new GameObject("Temporary shop detail camera");var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.nearClipPlane=.035f;c.farClipPlane=220;c.fieldOfView=58;
  var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);foreach(var r in lighting.FlashlightModel.GetComponentsInChildren<Renderer>())r.enabled=false;
  var target=new RenderTexture(1440,960,24){antiAliasing=4};c.targetTexture=target;var previous=RenderTexture.active;
  var repair=root.Find("InvestigationFieldwork/ShopSearchDetails/WatchRepairBox");
  var views=new[]{(V(-.45f,1.5f,-2.3f),V(.0f,1.15f,0)),(V(.3f,.25f,-1.25f),V(.05f,.025f,-.15f)),(V(-.1f,1.55f,3.1f),V(-1.9f,.95f,4.7f)),(V(5.5f,1.6f,6.9f),V(5.5f,.95f,7.85f)),(V(4.70f,1.29f,7.55f),V(4.78f,1.01f,7.96f)),(V(-3,1.55f,-1.3f),V(-3,1.4f,0)),(V(.15f,1.6f,1.7f),V(0,1.2f,0)),(V(-4,1.6f,-9),V(0,1.4f,-1)),(V(-1.9f,1.12f,4.1f),V(-1.9f,.87f,4.9f)),(V(-1.2f,.23f,4.25f),V(-1.8f,.035f,4.6f))};
  try{for(int i=0;i<views.Length;i++){
   if(repair&&(i==3||i==4))repair.GetComponent<EvidenceContainer>().Lid.localRotation=Quaternion.Euler(105,0,0);lighting.Flashlight(i!=7);
   c.transform.position=views[i].Item1;c.transform.LookAt(views[i].Item2);lighting.FlashlightLight.transform.SetPositionAndRotation(c.transform.position+c.transform.right*.18f,c.transform.rotation);c.Render();RenderTexture.active=target;var image=new Texture2D(1440,960,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,960),0,0);image.Apply();File.WriteAllBytes("Builds/shop-detail-"+suffix+"-"+i+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
  }}finally{if(repair)repair.GetComponent<EvidenceContainer>().Close();RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
 }
}
#endif
