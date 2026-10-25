#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
using static DemoGeometry;

public static partial class PresentationPolish {
 // Visual authoring is shared by existing-scene polish and fresh fieldwork builds.
 // Grip anchors, docks and the working tip stay in their accepted local positions.
 static void RefineFieldTool(FieldInvestigationTool tool){
  var kind=tool.Kind;if(kind!=FieldInvestigationTool.Method.Forceps&&kind!=FieldInvestigationTool.Method.Swab&&kind!=FieldInvestigationTool.Method.Brush&&kind!=FieldInvestigationTool.Method.MagneticWand&&kind!=FieldInvestigationTool.Method.LiftCard)return;
  foreach(var child in tool.transform.Cast<Transform>().Where(t=>t!=tool.Tip).ToArray())Object.DestroyImmediate(child.gameObject);
  var steel=Mat(Folder,"FinishBrushedInstrumentSteel",new Color(.57f,.59f,.60f),.43f);steel.SetFloat("_Metallic",.88f);
  var black=Mat(Folder,"FinishMouldedToolGrip",new Color(.031f,.037f,.042f),.21f);
  var cotton=Mat(Folder,"FinishCottonTip",new Color(.77f,.76f,.70f),.015f);
  var shaft=Mat(Folder,"FinishSwabShaft",new Color(.57f,.55f,.47f),.12f);
  var bristle=Mat(Folder,"FinishBrushFibres",new Color(.16f,.145f,.12f),.06f);
  var accent=Mat(Folder,"FinishWandIdentificationBand",new Color(.12f,.18f,.23f),.27f);
  var mesh=new KitMesh{Lightmapped=false};var along=Quaternion.Euler(90,0,0);
  if(kind==FieldInvestigationTool.Method.Forceps){
   // Two continuous flat spring leaves, tapered tips, rounded heel and grip flutes.
   float[] z={-.055f,-.046f,-.025f,.005f,.045f,.085f,.112f,.125f};
   float[] spread={0,.0018f,.0045f,.0065f,.007f,.0055f,.0022f,.001f};
   for(int side=-1;side<=1;side+=2){
    for(int i=1;i<z.Length;i++){
     Vector3 a=V(side*spread[i-1],0,z[i-1]),b=V(side*spread[i],0,z[i]);
     float width=Mathf.Lerp(.007f,.002f,Mathf.InverseLerp(.04f,.125f,(z[i]+z[i-1])*.5f));
     mesh.ChamferBox(steel,(a+b)*.5f,V(Vector3.Distance(a,b)+.0006f,width,.0011f),.00035f,Quaternion.FromToRotation(Vector3.right,b-a));
    }
    for(int i=0;i<14;i++)mesh.Bar(steel,V(side*.007f,.0038f,-.018f+i*.0034f),V(.0023f,.00055f,.001f),.00022f);
    for(int i=0;i<6;i++)mesh.Bar(steel,V(side*.002f,.0014f,.109f+i*.002f),V(.001f,.003f,.00045f),.00015f);
   }mesh.Cylinder(steel,V(-.001f,0,-.055f),V(.001f,0,-.055f),.0033f,16);
  }else if(kind==FieldInvestigationTool.Method.Swab){
   mesh.Lathe(shaft,V(0,0,-.059f),along,new[]{.0013f,.0018f,.0018f,.0014f},new[]{0f,.003f,.162f,.166f},20);
   mesh.Lathe(cotton,V(0,0,.102f),along,new[]{.0015f,.0036f,.0047f,.0048f,.0042f,.0031f,.0016f,.0003f},new[]{0f,.003f,.009f,.015f,.020f,.023f,.025f,.026f},28);
   mesh.Lathe(shaft,V(0,0,-.05f),along,new[]{.0018f,.002f,.0018f},new[]{0f,.001f,.002f},16);
  }else if(kind==FieldInvestigationTool.Method.Brush){
   mesh.Lathe(black,V(0,0,-.055f),along,new[]{.001f,.004f,.0045f,.005f,.0048f,.0035f},new[]{0f,.004f,.025f,.073f,.103f,.109f},28);
   mesh.Lathe(steel,V(0,0,.052f),along,new[]{.004f,.0048f,.0048f,.0042f},new[]{0f,.002f,.011f,.014f},28);
   for(int i=0;i<3;i++)mesh.Lathe(steel,V(0,0,.054f+i*.0035f),along,new[]{.0049f,.0051f,.0049f},new[]{0f,.0006f,.0012f},24);
   mesh.Lathe(bristle,V(0,0,.064f),along,new[]{.004f,.0065f,.010f,.012f,.010f,.006f,.001f},new[]{0f,.010f,.025f,.040f,.051f,.058f,.062f},32);
   var fibre=Mat(Folder,"FinishBrushFibreHighlights",new Color(.19f,.17f,.14f),.05f);
   for(int i=0;i<48;i++){
    float a=i*Mathf.PI*2/48;Vector3 direction=V(Mathf.Cos(a),Mathf.Sin(a),0);
    Vector3 bottom=V(0,0,.068f)+direction*.0043f,middle=V(0,0,.102f)+direction*.0122f,tipEnd=V(0,0,.123f+(i%3)*.0007f)+direction*.004f;
    mesh.Cylinder(i%3==0?fibre:bristle,bottom,middle,.00028f,5,false);mesh.Cylinder(i%3==0?fibre:bristle,middle,tipEnd,.00022f,5,false);
   }
  }else if(kind==FieldInvestigationTool.Method.MagneticWand){
   mesh.Lathe(black,V(0,0,-.054f),along,new[]{.004f,.007f,.0075f,.0072f,.005f,.0045f},new[]{0f,.004f,.012f,.080f,.090f,.145f},28);
   for(int i=0;i<7;i++)mesh.Lathe(black,V(0,0,-.029f+i*.007f),along,new[]{.0073f,.0079f,.0073f},new[]{0f,.0008f,.0016f},24);
   mesh.Lathe(accent,V(0,0,.028f),along,new[]{.0075f,.0076f,.0075f},new[]{0f,.003f,.006f},28);
   mesh.Cylinder(steel,V(0,0,-.066f),V(0,0,-.053f),.0021f,16);
   mesh.Lathe(black,V(0,0,-.070f),along,new[]{.004f,.0065f,.0065f,.004f},new[]{0f,.001f,.004f,.005f},24);
   mesh.Lathe(steel,V(0,0,.090f),along,new[]{.0046f,.006f,.006f,.0057f},new[]{0f,.003f,.021f,.024f},24);
   mesh.Lathe(bristle,V(0,0,.112f),along,new[]{.0058f,.0083f,.0089f,.0065f,.001f},new[]{0f,.003f,.007f,.011f,.014f},24);
  }else{
   var card=Mat(Folder,"FinishLiftBackingCard",new Color(.72f,.71f,.66f),.07f);
   var border=Mat(Folder,"FinishLiftCardBorder",new Color(.22f,.25f,.26f),.14f);
   mesh.Bar(card,V(0,0,.021f),V(.076f,.0013f,.127f),.0005f);
   foreach(float x in new[]{-.033f,.033f})mesh.Bar(border,V(x,.00085f,.021f),V(.00065f,.0003f,.112f),.0001f);
   foreach(float z in new[]{-.035f,.077f})mesh.Bar(border,V(0,.00085f,z),V(.066f,.0003f,.00065f),.0001f);
   var film=GlassFinish("FinishLiftCoverFilm",.19f,.82f,0);
   mesh.Bar(film,V(0,.0012f,.033f),V(.059f,.0005f,.079f),.0002f);
   mesh.Bar(card,V(.025f,.0015f,.077f),V(.013f,.0006f,.012f),.0005f);
  }
  var g=mesh.Emit("RefinedInstrument",tool.transform,Vector3.zero,Quaternion.identity,Folder,"finish_instrument_"+kind);g.layer=CrowdLayer;
  foreach(var r in g.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.receiveShadows=true;r.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.BlendProbes;r.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;}
  tool.Tip.localPosition=V(0,0,.125f);
  FitInstrumentPickup(tool);
 }
 static void FitInstrumentPickup(FieldInvestigationTool tool){
  var box=tool.GetComponent<BoxCollider>();if(!box)return;
  switch(tool.Kind){
   case FieldInvestigationTool.Method.Forceps:box.center=V(0,0,.035f);box.size=V(.027f,.022f,.20f);break;
   case FieldInvestigationTool.Method.Swab:box.center=V(0,0,.035f);box.size=V(.024f,.022f,.20f);break;
   case FieldInvestigationTool.Method.Brush:box.center=V(0,0,.035f);box.size=V(.033f,.032f,.20f);break;
   case FieldInvestigationTool.Method.MagneticWand:box.center=V(0,0,.028f);box.size=V(.032f,.031f,.215f);break;
   case FieldInvestigationTool.Method.LiftCard:box.center=V(0,0,.021f);box.size=V(.085f,.023f,.137f);break;
  }box.isTrigger=true;
 }
 static void FinishMaterials(){
  var register=root.GetComponentInChildren<EvidenceRegister>();
  foreach(var material in register.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader.name=="Standard").Distinct()){
   string path=AssetDatabase.GetAssetPath(material),name=material.name.ToLowerInvariant();
   if(path.Contains("/ForensicModels/")){
    bool film=material.renderQueue>=3000;
    if(!film){
     if(material.GetTexture("_MetallicGlossMap"))material.SetFloat("_GlossMapScale",name.Contains("steel")||name.Contains("metal")?.72f:.55f);
     else material.SetFloat("_Glossiness",Mathf.Min(material.GetFloat("_Glossiness"),name.Contains("steel")?.43f:.27f));
     if(material.GetTexture("_BumpMap"))material.SetFloat("_BumpScale",.5f);
    }
   }
   if(name.StartsWith("fieldpaper")){material.color=new Color(.88f,.87f,.82f);material.SetFloat("_Glossiness",.035f);}
   if(name.Contains("portablecase")||name.Contains("fieldlidstrap"))material.SetFloat("_Glossiness",name.Contains("steel")?.32f:.16f);
   material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(material);
  }
  // Smoothly varying paint and dust remain in the single road surface; no overlay planes.
  foreach(string name in new[]{"DryRoad","DustyPaving"}){
   var m=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/"+name+".mat");
   m.SetFloat("_AggregateContrast",name=="DryRoad"?.68f:.83f);m.SetFloat("_DustStrength",.18f);m.SetFloat("_BumpScale",name=="DryRoad"?.18f:.14f);EditorUtility.SetDirty(m);
  }
 }
 static void FinishChecks(){
  var register=root.GetComponentInChildren<EvidenceRegister>();var tools=register.Tools;
  foreach(var tool in tools){
   if(!tool.Tip||!tool.Dock||tool.GetComponentsInChildren<Collider>().Any(c=>!c.isTrigger))throw new Exception("Invalid tool tip/dock/collision: "+tool.Kind);
   foreach(var f in tool.GetComponentsInChildren<MeshFilter>()){
    var m=f.sharedMesh;if(!m||m.triangles.Length==0)throw new Exception("Missing instrument mesh");
    if(m.vertices.Any(v=>float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)))throw new Exception("Invalid tool vertex");
   }
  }
  var tray=register.Kit.transform.Find("FieldKit");var trayTools=tools.Where(t=>t.Dock&&t.Dock.parent==tray).ToArray();
  foreach(var t in trayTools.Where(t=>t.Kind==FieldInvestigationTool.Method.Forceps||t.Kind==FieldInvestigationTool.Method.Swab||t.IsApplicator)){
   foreach(var filter in t.GetComponentsInChildren<MeshFilter>())foreach(var vertex in filter.sharedMesh.vertices){
    Vector3 local=tray.InverseTransformPoint(filter.transform.TransformPoint(vertex));
    if(local.x<-.228f||local.x>-.006f||Mathf.Abs(local.z)>.141f)throw new Exception("Instrument clips tray wall: "+t.Kind+" at "+local);
   }
  }
  for(int a=0;a<trayTools.Length;a++)for(int b=a+1;b<trayTools.Length;b++){
   var ca=trayTools[a].GetComponent<BoxCollider>();var cb=trayTools[b].GetComponent<BoxCollider>();
   if(ca.bounds.Intersects(cb.bounds))throw new Exception("Overlapping tray pickup volumes: "+trayTools[a].Kind+" / "+trayTools[b].Kind);
  }
  foreach(string file in new[]{"GroundFracturesPacked","ParkingLayoutMask","RoadWeatherOriginal"}){
   var i=(TextureImporter)AssetImporter.GetAtPath(RoadArt+file+".png");if(i.sRGBTexture||!PackImport.Expected(i,i.assetPath,false))throw new Exception("Ground data import mismatch: "+file);
  }
  foreach(string file in new[]{"RoadAggregateNormal","PavingAggregateNormal"}){
   var i=(TextureImporter)AssetImporter.GetAtPath(RoadArt+file+".png");if(i.textureType!=TextureImporterType.NormalMap||!i.convertToNormalmap)throw new Exception("Ground normal import mismatch");
  }
  RoadLayerGpuCheck();Debug.Log("PRESENTATION_FINISH_CHECKS_PASS fitted nonoverlapping tray pickups; complete tool meshes; ground import rules and paint/crack GPU layering");
 }
 static void FinishActualGroundCheck(){
  var stage=new GameObject("Temporary actual road map verification");var surface=GameObject.CreatePrimitive(PrimitiveType.Plane);surface.transform.SetParent(stage.transform);surface.layer=31;surface.transform.position=V(-14,300,-5);
  var road=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/DryRoad.mat");var material=new Material(road);surface.GetComponent<Renderer>().sharedMaterial=material;
  var eye=new GameObject("Road check camera");eye.transform.SetParent(stage.transform);var camera=eye.AddComponent<Camera>();camera.transform.position=V(-14,307,-5);camera.transform.rotation=Quaternion.Euler(90,0,0);camera.orthographic=true;camera.orthographicSize=4;camera.cullingMask=1<<31;camera.stereoTargetEye=StereoTargetEyeMask.None;
  var source=new GameObject("Road check light");source.transform.SetParent(stage.transform);var light=source.AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(60,25,0);light.cullingMask=1<<31;light.intensity=1;
  var rt=new RenderTexture(384,256,24);camera.targetTexture=rt;var previous=RenderTexture.active;var image=new Texture2D(384,256,TextureFormat.RGB24,false);bool fog=RenderSettings.fog;RenderSettings.fog=false;
  Color32[] Pixels(){camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,384,256),0,0);image.Apply();return image.GetPixels32();}
  try{
   var baseline=Pixels();foreach(string property in new[]{"_PaintMask","_CrackMasks"}){
    var original=material.GetTexture(property);material.SetTexture(property,Texture2D.blackTexture);var disabled=Pixels();material.SetTexture(property,original);int changed=0;
    for(int i=0;i<baseline.Length;i++)if(Mathf.Abs(baseline[i].r-disabled[i].r)+Mathf.Abs(baseline[i].g-disabled[i].g)+Mathf.Abs(baseline[i].b-disabled[i].b)>9)changed++;
    if(changed<120)throw new Exception("Actual road layer is invisible: "+property+" pixels="+changed);Debug.Log("ACTUAL_GROUND_VISIBILITY_PASS "+property+" pixels="+changed);
   }
  }finally{RenderSettings.fog=fog;RenderTexture.active=previous;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(material);Object.DestroyImmediate(stage);}
 }
 static void FinishOpen(){EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");}
 static void FinishCapture(string label){
  var folder="Builds/PresentationFinish/"+label;Directory.CreateDirectory(folder);
  SandboxPreview();RoadPreview();ShopDetailPreview("finish-"+label);
  foreach(var pattern in new[]{"sandbox-view-*.png","road-sidewalk-*.png","shop-detail-finish-"+label+"-*.png"})foreach(var file in Directory.GetFiles("Builds",pattern))File.Copy(file,folder+"/"+Path.GetFileName(file),true);
  FinishInstrumentPreview(folder);
 }
 static void FinishInstrumentPreview(string folder){
  var go=new GameObject("Temporary instrument detail camera");var camera=go.AddComponent<Camera>();NightQuality.Configure(camera);camera.stereoTargetEye=StereoTargetEyeMask.None;camera.nearClipPlane=.015f;camera.fieldOfView=44;
  var light=go.AddComponent<Light>();light.type=LightType.Spot;light.range=3;light.spotAngle=82;light.intensity=1.8f;light.color=new Color(1,.95f,.87f);light.shadows=LightShadows.Soft;
  var rt=new RenderTexture(1200,900,24){antiAliasing=4};camera.targetTexture=rt;var previous=RenderTexture.active;
  try{foreach(var t in root.GetComponentInChildren<EvidenceRegister>().Tools.Where(t=>t.Kind<=FieldInvestigationTool.Method.MagneticWand)){
   camera.transform.position=t.transform.TransformPoint(V(.11f,.26f,.12f));camera.transform.LookAt(t.transform.TransformPoint(V(0,0,.03f)),t.transform.forward);
   camera.Render();RenderTexture.active=rt;var image=new Texture2D(1200,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,900),0,0);image.Apply();File.WriteAllBytes(folder+"/instrument-"+t.Kind+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
  }}finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
 }
 static void FinishReport(string label){
  Directory.CreateDirectory("Builds/PresentationFinish");var lines=new List<string>();
  foreach(var m in root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct()){
   string asset=AssetDatabase.GetAssetPath(m);if(!asset.Contains("ForensicModels")&&!asset.Contains("OmanStreet")&&!m.name.StartsWith("Field"))continue;
   string props=string.Join(" ",new[]{"_Metallic","_Glossiness","_GlossMapScale","_BumpScale","_OcclusionStrength"}.Where(m.HasProperty).Select(k=>k+"="+m.GetFloat(k)));
   lines.Add(asset+" | "+m.name+" | "+m.shader.name+" | "+m.color+" | "+props);
  }File.WriteAllLines("Builds/PresentationFinish/materials-"+label+".txt",lines);
 }
 public static void PresentationFinishAuditRun(){try{FinishOpen();FinishReport("before");FinishCapture("before");Debug.Log("PRESENTATION_AUDIT_PASS");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void PresentationFinishRun(){try{
  FinishOpen();string car=CarGeometrySignature();var register=root.GetComponentInChildren<EvidenceRegister>();
  register.Kit.Grip.localPosition=V(0,.08f,-.38f);register.Kit.Grip.localRotation=Quaternion.Euler(-90,0,0);
  foreach(var field in root.GetComponentsInChildren<LatentMarkField>(true))foreach(var child in field.transform.Cast<Transform>().Where(c=>c.name.StartsWith("LatentMark_")).ToArray())Object.DestroyImmediate(child.gameObject);
  foreach(var tool in register.Tools){
   RefineFieldTool(tool);
   if(tool.Dock&&tool.Dock.parent.name=="FieldKit"&&(tool.IsApplicator||tool.Kind==FieldInvestigationTool.Method.Forceps||tool.Kind==FieldInvestigationTool.Method.Swab)){
    var p=tool.transform.localPosition;p.x=-.145f;tool.transform.localPosition=p;tool.Dock.localPosition=p;tool.Dock.localRotation=tool.transform.localRotation;
   }
  }
  FinishMaterials();Physics.SyncTransforms();FinishChecks();FieldworkChecks();
  // Test-created field marks are transient and must never be serialized into the scene.
  register.ResetInvestigation();
  if(car!=CarGeometrySignature())throw new Exception("Protected car changed");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveOpenScenes();
  FinishReport("after");FinishCapture("after");
  FinishOpen();FinishChecks();FinishActualGroundCheck();
  if(root.GetComponentsInChildren<LatentMarkField>(true).Any(f=>f.GetComponentsInChildren<DustablePrint>(true).Length>0))throw new Exception("Runtime fingerprints leaked into saved scene");
  EditableDelivery.Build();Debug.Log("PRESENTATION_FINISH_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
