#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using static DemoGeometry;

public static partial class PresentationPolish {
 static void ConfigurePortableFieldwork(EvidenceRegister register){
  var parent=register.transform;var table=parent.Find("PortableFieldworkCart");table.name="ExteriorFieldTable";table.localPosition=V(-4.3f,0,-1.3f);
  var caseRoot=Group("PortableEvidenceCase",parent);caseRoot.localPosition=V(-4.3f,.892f,-1.3f);
  var kit=caseRoot.gameObject.AddComponent<PortableFieldKit>();kit.Bounds=Object.FindFirstObjectByType<InteriorBounds>();register.Kit=kit;
  var polymer=Mat(Folder,"PortableCaseGraphite",new Color(.055f,.065f,.067f),.19f);polymer.mainTexture=DemoAssetLibrary.Texture("RealismTextures","Surfaces/PaintedMetalChipped_Neutral.jpg");polymer.mainTextureScale=Vector2.one*2;
  var rubber=Mat(Folder,"PortableCaseRubber",new Color(.022f,.026f,.027f),.10f);var metal=Mat(Folder,"PortableCaseLatchSteel",new Color(.35f,.38f,.39f),.28f);metal.SetFloat("_Metallic",.7f);
  var shell=new KitMesh{Lightmapped=false};shell.Bar(polymer,V(0,.012f,0),V(.90f,.024f,.50f),.009f);
  foreach(float side in new[]{-1f,1f}){
   shell.Bar(polymer,V(side*.441f,.057f,0),V(.018f,.09f,.50f),.006f);shell.Bar(polymer,V(0,.057f,side*.241f),V(.864f,.09f,.018f),.006f);
   foreach(float z in new[]{-.22f,.22f})shell.Bar(rubber,V(side*.425f,.038f,z),V(.045f,.071f,.045f),.008f);
   shell.Bar(metal,V(side*.28f,.052f,-.255f),V(.045f,.045f,.012f),.004f);
   for(int i=0;i<5;i++)shell.Bar(polymer,V(side*.451f,.05f,-.15f+i*.075f),V(.008f,.055f,.013f),.003f);
  }
  foreach(float x in new[]{-.33f,.33f})foreach(float z in new[]{-.17f,.17f})shell.Bar(rubber,V(x,.003f,z),V(.07f,.006f,.05f),.002f);
  shell.Emit("ClosedBaseWithRecessAndCornerBumpers",caseRoot,Vector3.zero,Quaternion.identity,Folder,"portable_case_base");
  var lid=Group("RaisedLid",caseRoot);lid.localPosition=V(0,.10f,.247f);lid.localRotation=Quaternion.Euler(105,0,0);
  var cover=new KitMesh{Lightmapped=false};cover.Bar(polymer,V(0,.007f,-.24f),V(.91f,.018f,.49f),.007f);cover.Bar(rubber,V(0,-.004f,-.24f),V(.865f,.007f,.445f),.002f);
  foreach(float x in new[]{-.30f,.30f})cover.Cylinder(metal,V(x-.035f,0,0),V(x+.035f,0,0),.011f,10);
  cover.Emit("LidShellAndHinges",lid,Vector3.zero,Quaternion.identity,Folder,"portable_case_lid");
  var handle=Group("CarryHandle",caseRoot);handle.localPosition=V(0,.23f,-.18f);var bail=new KitMesh{Lightmapped=false};
  foreach(float x in new[]{-.105f,.105f})bail.Cylinder(polymer,V(x,-.13f,0),V(x,0,0),.009f,10);
  bail.Cylinder(polymer,V(-.105f,0,0),V(.105f,0,0),.012f,12);bail.Cylinder(rubber,V(-.065f,0,0),V(.065f,0,0),.015f,12);bail.Emit("MoldedCarryHandle",handle,Vector3.zero,Quaternion.identity,Folder,"portable_case_handle");
  var grip=handle.gameObject.AddComponent<BoxCollider>();grip.isTrigger=true;grip.size=V(.24f,.055f,.055f);kit.Grip=handle;kit.GripCollider=grip;
  var tray=parent.Find("FieldKit");tray.SetParent(caseRoot,false);tray.localPosition=V(-.19f,.031f,-.025f);tray.localScale=Vector3.one;
  var guide=tray.Find("FieldGuide");guide.SetParent(lid,false);guide.localPosition=V(-.25f,-.013f,-.24f);guide.localRotation=Quaternion.Euler(180,0,0);
  var pad=parent.Find("EvidenceRegisterClipboard");pad.SetParent(lid,false);pad.localPosition=V(.21f,-.014f,-.24f);pad.localRotation=Quaternion.Euler(180,0,0);pad.localScale=Vector3.one;
  var brush=FieldTool(tray,register,FieldInvestigationTool.Method.Brush,V(.105f,.035f,-.065f));var card=FieldTool(tray,register,FieldInvestigationTool.Method.LiftCard,V(.105f,.035f,.065f));
  card.transform.localRotation=Quaternion.Euler(0,90,0);
  register.Tools=register.Tools.Concat(new[]{brush,card}).ToArray();
  foreach(var tool in register.Tools){var dock=Group("Dock_"+tool.Kind,caseRoot);dock.SetPositionAndRotation(tool.transform.position,tool.transform.rotation);tool.Dock=dock;}
  var powder=DemoAssetLibrary.PlacePack("ForensicModels","FingerprintPowderJar",caseRoot,V(.24f,.025f,.11f),0,new DemoAssetLibrary.PackOptions{Shadows=true,Lod=DemoAssetLibrary.LodClass.None});
  if(!powder){var fallback=new KitMesh{Lightmapped=false};fallback.Cylinder(rubber,V(.24f,.025f,.11f),V(.24f,.085f,.11f),.028f,16);fallback.Emit("PowderJarFallback",caseRoot,Vector3.zero,Quaternion.identity,Folder,"portable_powder_fallback");}
  var packets=parent.Find("SealedSamplePackets");packets.SetParent(caseRoot,false);packets.localPosition=V(.23f,.027f,-.10f);
  var originals=register.SealedPackets;register.SealedPackets=new GameObject[8];for(int i=0;i<8;i++){
   GameObject packet;if(i<originals.Length)packet=originals[i];else {packet=Object.Instantiate(originals[0],packets);packet.name="SealedPacket_"+(i+1);packet.transform.localPosition=V(0,i*.008f,0);}
   register.SealedPackets[i]=packet;packet.SetActive(false);
  }
  var caseLabel=PaperSheet(caseRoot,"CaseLabel",V(0,.103f,-.226f),new Vector2(.22f,.035f),0);caseLabel.transform.localRotation=Quaternion.Euler(-90,0,0);caseLabel.transform.localPosition=V(0,.053f,-.251f);
  foreach(var t in caseRoot.GetComponentsInChildren<Transform>(true))t.gameObject.layer=CrowdLayer;
  foreach(var renderer in caseRoot.GetComponentsInChildren<Renderer>(true))GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,0);
  AddDustingPrints(register);
 }
 static void AddDustingPrints(EvidenceRegister register){
  // Reuse two real handle positions and the licensed/project-created ridge atlas.
  // Keep all six old UV handle traces and their evidence shaders unchanged.
  var old=root.GetComponentsInChildren<UVTrace>(true).Where(t=>t.name=="EvidenceUV_HandlePrint_1").OrderBy(t=>PathOf(t.transform)).ToArray();if(old.Length!=2)throw new Exception("Two entrance handle anchors required");
  var clues=register.Clues.ToList();
  for(int i=0;i<2;i++){
   var leaf=old[i].transform.parent;var previous=leaf.Find("DevelopableHandleRidge");if(previous)Object.DestroyImmediate(previous.gameObject);
   var g=Group("DevelopableHandleRidge",leaf);g.localPosition=V(old[i].transform.localPosition.x,old[i].transform.localPosition.y,-.085f);
   var clue=g.gameObject.AddComponent<EvidenceClue>();clue.Id="E"+(13+i);clue.TitleEn=i==0?"Developed left handle mark":"Developed right handle mark";clue.TitleAr=i==0?"أثر مطور على المقبض الأيسر":"أثر مطور على المقبض الأيمن";
   clue.ObservationEn="A developed ridge pattern on the handle. Comparison is still required; it does not identify a person.";clue.ObservationAr="نقشة حواف مطورة على المقبض. تحتاج إلى مقارنة ولا تحدد شخصًا بمفردها.";clue.Register=register;clue.CanSample=true;clue.RequiredTool=FieldInvestigationTool.Method.LiftCard;
   var hit=g.gameObject.AddComponent<BoxCollider>();hit.isTrigger=true;hit.size=V(.075f,.075f,.05f);hit.center=V(0,0,-.013f);
   var m=Mat(Folder,"DevelopedPowderRidge_"+i,new Color(.075f,.068f,.058f,.76f),.025f);m.shader=Shader.Find("CrimeScene/SurfaceDecal");m.mainTexture=DemoAssetLibrary.Texture("SurfaceDecals","Fingerprints_Atlas.png");m.mainTextureScale=new Vector2(.25f,.5f);m.mainTextureOffset=new Vector2((i+1)*.25f,.5f);m.renderQueue=2465;
   // A fine cylindrical patch follows the real 25 mm pull bar instead of hovering
   // as a flat fingerprint across the surrounding glass.
   var ridges=new KitMesh{Lightmapped=false};for(int j=0;j<6;j++){
    float a=-.70f+j*1.4f/6,b=-.70f+(j+1)*1.4f/6;
    Vector3 P(float angle,float y)=>V(Mathf.Sin(angle)*.013f,y,-Mathf.Cos(angle)*.013f);
    ridges.Quad(m,V(Mathf.Sin((a+b)*.5f),0,-Mathf.Cos((a+b)*.5f)),P(a,-.013f),P(b,-.013f),P(b,.013f),P(a,.013f),new Rect(j/6f,0,1/6f,1));
   }
   var quad=ridges.Emit("PowderRidgePattern",g,Vector3.zero,Quaternion.identity,Folder,"portable_handle_ridge_"+i);
   var r=quad.GetComponent<MeshRenderer>();r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;r.enabled=false;r.lightProbeUsage=LightProbeUsage.BlendProbes;
   var print=g.gameObject.AddComponent<DustablePrint>();print.Clue=clue;print.Powder=r;print.ResetPrint();clues.Add(clue);
  }register.Clues=clues.OrderBy(c=>c.Id).ToArray();
 }
 static void PortableFieldworkChecks(EvidenceRegister register){
  void Require(bool value,string message){if(!value)throw new Exception("Portable fieldwork: "+message);}
  bool persist=register.PersistRecords;register.PersistRecords=false;
  var kit=register.Kit;Require(kit&&kit.Grip&&kit.GripCollider&&kit.GripCollider.isTrigger,"case/handle missing");
  Require(register.SealedPackets.Length==10&&register.Tools.All(t=>t.Dock&&t.Dock.IsChildOf(kit.transform)),"packets or moving tool docks missing");
  Require(kit.transform.position.z<0&&Mathf.Abs(kit.transform.position.x+4.3f)<.01f,"kit/table must start outside the shop");
  Require(root.GetComponentsInChildren<UVTrace>(true).Count(t=>t.name.StartsWith("EvidenceUV_HandlePrint_"))==6,"existing UV handle traces changed");
  var a=new GameObject("Temporary portable kit hand");var b=new GameObject("Temporary kit work hand");var hand=a.AddComponent<HandInteractor>();var other=b.AddComponent<HandInteractor>();
  GameObject block=null;var home=kit.transform.position;var homeRotation=kit.transform.rotation;
  try{
   Physics.SyncTransforms();Require(kit.CanRestAt(home,homeRotation),"case does not fit its exterior table");
   a.transform.position=kit.Grip.position;Physics.SyncTransforms();Require(hand.Grab()&&hand.CarriedKit==kit,"handle cannot be gripped from inside its trigger");b.transform.position=a.transform.position;Require(!other.TakeKit(kit),"two hands can carry one case");
   a.transform.SetPositionAndRotation(V(-4.3f,1.4f,-2.2f),Quaternion.Euler(35,23,18));kit.Follow(a.transform);Require(Vector3.Dot(kit.transform.up,Vector3.up)>.999f,"carried case tilts contents");
   var tool=register.Tools.First(t=>t.Kind==FieldInvestigationTool.Method.Forceps);b.transform.position=tool.transform.position+Vector3.up*.07f;Require(other.TakeFieldTool(tool),"free hand cannot take a case tool");
   b.transform.SetPositionAndRotation(V(-3.7f,1.2f,-2.1f),Quaternion.identity);tool.Follow(b.transform);var held=tool.transform.position;
   a.transform.position+=Vector3.left*.25f;kit.Follow(a.transform);Require(Vector3.Distance(tool.transform.position,held)<.001f,"carrying case drags a tool out of the other hand");
   other.Release();Require(Vector3.Distance(tool.transform.position,tool.Dock.position)<.001f,"tool returns to old fixed table instead of current case dock");
   Physics.SyncTransforms();Require(hand.PlaceKit(new Ray(V(-4.3f,1.8f,-2.45f),Vector3.down),3),"case cannot be set on pavement");Require(Mathf.Abs(kit.transform.position.y-.002f)<.012f&&!kit.Holder&&!hand.CarriedKit,"released case floats");
   a.transform.position=kit.Grip.position+Vector3.up*.04f;Require(hand.TakeKit(kit),"seated case cannot be regripped");
   block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.SetPositionAndRotation(V(-4.3f,1,-2.45f),Quaternion.identity);block.transform.localScale=V(1,1,.05f);Physics.SyncTransforms();
   Require(!kit.PlaceByRay(new Ray(V(-4.3f,1,-3.2f),V(0,-.2f,1)),3),"case can be placed through a wall");Object.DestroyImmediate(block);block=null;
   Require(!kit.CanRestAt(V(-4.3f,1.7f,-2.45f),Quaternion.identity),"unsupported floating case accepted");
   Require(!kit.CanRestAt(V(-3.6f,.89f,-1.3f),Quaternion.identity),"case can balance off a table edge");
   block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.position=V(-4.3f,.54f,-2.4f);block.transform.localScale=V(1,.06f,.65f);Physics.SyncTransforms();Require(!kit.CanRestAt(V(-4.3f,0,-2.45f),Quaternion.identity),"raised lid can clip into a low shelf");Object.DestroyImmediate(block);block=null;
   register.ResetInvestigation();Require(!kit.Holder&&!hand.CarriedKit&&Vector3.Distance(kit.transform.position,home)<.001f,"session reset does not restore carried case");
   a.transform.position=kit.Grip.position+Vector3.up*.04f;a.transform.rotation=homeRotation;hand.TakeKit(kit);Physics.SyncTransforms();Require(hand.PlaceKit(new Ray(home+Vector3.up,Vector3.down),2),"case cannot return to tabletop");Require(Mathf.Abs(kit.transform.position.y-home.y)<.004f,"case sinks into table");
   hand.ReleaseAll();register.ResetInvestigation();
   var brush=register.Tools.First(t=>t.Kind==FieldInvestigationTool.Method.Brush);var card=register.Tools.First(t=>t.Kind==FieldInvestigationTool.Method.LiftCard);var clue=register.Clues.First(c=>c.Id=="E13");var print=clue.GetComponent<DustablePrint>();
   Require(print&&!print.Powder.enabled&&!clue.Inspect(),"latent ridge is visible or inspectable before development");
   a.transform.position=card.transform.position+Vector3.up*.04f;Require(hand.TakeFieldTool(card),"lift card cannot be held");a.transform.position=clue.transform.position-clue.transform.forward*.13f;Require(!clue.Sample(card),"undeveloped print can be lifted");hand.Release();
   a.transform.position=brush.transform.position+Vector3.up*.04f;Require(hand.TakeFieldTool(brush),"brush cannot be held");a.transform.position=clue.transform.position-clue.transform.forward*.19f;a.transform.LookAt(clue.transform.position);brush.Follow(a.transform);Physics.SyncTransforms();
   Require(brush.Use(clue),"brush cannot start at handle");var ray=new Ray(a.transform.position,a.transform.forward);Require(!brush.BrushStroke(ray,.05f,false)&&print.Coverage==0,"brush develops without pressed input");
   for(int i=0;i<20;i++)brush.BrushStroke(ray,.05f,true);Require(print.Coverage>0&&print.Coverage<1&&!clue.Available,"partial brush pass bypasses development");
   block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.position=Vector3.Lerp(a.transform.position,clue.transform.position,.5f);block.transform.localScale=Vector3.one*.06f;Physics.SyncTransforms();var coverage=print.Coverage;Require(!brush.BrushStroke(ray,.05f,true)&&print.Coverage==coverage,"brush develops through solid geometry");Object.DestroyImmediate(block);block=null;Physics.SyncTransforms();
   for(int i=0;i<80;i++)brush.BrushStroke(ray,.05f,true);Require(print.Developed&&print.Powder.enabled&&clue.Observed,"held brush input does not develop and record print");Require(!clue.Sample(brush),"brush wrongly collects a ridge lift");hand.Release();
   a.transform.position=card.transform.position+Vector3.up*.04f;Require(hand.TakeFieldTool(card),"lift card unavailable after brushing");a.transform.position=clue.transform.position-clue.transform.forward*.13f;Require(clue.Sample(card)&&register.SampleCount==1,"developed ridge cannot be lifted");int count=register.Entries.Count;Require(!clue.Sample(card)&&register.Entries.Count==count,"ridge lift duplicates records");
   register.ResetInvestigation();Require(!print.Developed&&!print.Powder.enabled&&!clue.Observed&&!clue.Sampled&&!hand.FieldTool,"reset leaves developed print or held tool");
   var desktopObject=new GameObject("Temporary desktop portable case test");try{
    var view=desktopObject.AddComponent<Camera>();var desktop=desktopObject.AddComponent<DesktopInteractor>();desktop.View=view;desktop.ReadDesktopInput=false;desktopObject.transform.position=home+V(0,.75f,-1);desktopObject.transform.LookAt(kit.Grip.position);Physics.SyncTransforms();
    Require(InvestigationInteraction.Use(desktop,new Ray(view.transform.position,kit.Grip.position-view.transform.position),2)&&desktop.CarriedKit==kit,"desktop pointer cannot pick up case handle");
    Require(desktop.PlaceKit(new Ray(V(-4.3f,1.8f,-2.45f),Vector3.down),3)&&Mathf.Abs(kit.transform.position.y-.002f)<.012f,"desktop case floats instead of seating on pavement");desktop.ReleaseAll();
   }finally{Object.DestroyImmediate(desktopObject);register.ResetInvestigation();}
   Debug.Log("PORTABLE_FIELDWORK_PASS outside table; actual grip from inside trigger; desktop pointer carry; upright single-holder carry; free-hand tool use; moving docks; supported pavement/table placement; walls/edges/air rejected; reset; hidden/partial/developed/lifted ridge workflow with hold input and occlusion");
  }finally{if(block)Object.DestroyImmediate(block);hand.ReleaseAll();other.ReleaseAll();Object.DestroyImmediate(a);Object.DestroyImmediate(b);register.ResetInvestigation();register.PersistRecords=persist;Physics.SyncTransforms();}
 }
 public static void PortableInspectRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;EvidenceGameplay();PortablePreview();Debug.Log("PORTABLE_INSPECTION_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void PortablePreview(){
  var cameraObject=new GameObject("Temporary portable case camera");var camera=cameraObject.AddComponent<Camera>();NightQuality.Configure(camera);camera.stereoTargetEye=StereoTargetEyeMask.None;camera.fieldOfView=50;camera.nearClipPlane=.03f;camera.farClipPlane=220;
  var lighting=cameraObject.AddComponent<DemoLighting>();lighting.Flashlight(true);foreach(var r in lighting.FlashlightModel.GetComponentsInChildren<Renderer>())r.enabled=false;
  var rt=new RenderTexture(1440,1000,24);camera.targetTexture=rt;var previous=RenderTexture.active;var register=root.Find("InvestigationFieldwork").GetComponent<EvidenceRegister>();
  var print=register.Clues.First(c=>c.Id=="E13").GetComponent<DustablePrint>();
  var views=new[]{(V(-4.3f,1.55f,-2.25f),V(-4.3f,1.08f,-1.3f)),(V(-6.5f,1.65f,-4),V(-4.3f,1,-1.3f)),(print.transform.position-print.transform.forward*.17f+Vector3.up*.012f,print.transform.position)};
  try{for(int i=0;i<views.Length;i++){
   camera.transform.position=views[i].Item1;camera.transform.LookAt(views[i].Item2);lighting.FlashlightLight.transform.SetPositionAndRotation(camera.transform.position+camera.transform.right*(i==2 ? .25f : .05f),camera.transform.rotation);if(i==2)lighting.FlashlightLight.transform.LookAt(print.transform.position);
   Color32[] before=null;if(i==2){camera.Render();RenderTexture.active=rt;var clean=new Texture2D(1440,1000,TextureFormat.RGB24,false);clean.ReadPixels(new Rect(0,0,1440,1000),0,0);clean.Apply();before=clean.GetPixels32();File.WriteAllBytes("Builds/portable-ridge-before.png",clean.EncodeToPNG());Object.DestroyImmediate(clean);print.Brush(1);}
   camera.Render();RenderTexture.active=rt;var image=new Texture2D(1440,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,1000),0,0);image.Apply();File.WriteAllBytes("Builds/portable-"+i+".png",image.EncodeToPNG());
   if(before!=null){var after=image.GetPixels32();int changed=0;for(int p=0;p<after.Length;p++)if(Mathf.Abs(after[p].r-before[p].r)+Mathf.Abs(after[p].g-before[p].g)+Mathf.Abs(after[p].b-before[p].b)>12)changed++;if(changed<50)throw new Exception("Developed ridge pattern is not visibly rendered");Debug.Log("PORTABLE_RIDGE_RENDER_PASS pixels="+changed+" enabled="+print.Powder.enabled+" bounds="+print.Powder.bounds);}
   Object.DestroyImmediate(image);
  }}finally{print.ResetPrint();RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);lighting.ReleaseTorchResources();Object.DestroyImmediate(cameraObject);}
 }
}
#endif
