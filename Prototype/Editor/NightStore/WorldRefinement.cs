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
 static void WorldSurfaceRefinement(){
  int count=0;
  foreach(var m in root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m&&(m.shader.name=="Standard"||m.shader.name=="CrimeScene/SatinWood")).Distinct()){
   string texture=AssetDatabase.GetAssetPath(m.mainTexture),path=AssetDatabase.GetAssetPath(m);bool changed=false;
   void Finish(float gloss,float bump){m.SetFloat("_Glossiness",gloss);m.SetFloat("_GlossMapScale",gloss);m.SetFloat("_BumpScale",bump);changed=true;}
   // These authored normal maps were previously all at full strength: wood
   // looked like bark and suede like crumpled paper in the hand torch.
   if(texture.Contains("/PBRInterior/Walnut_")){m.shader=Shader.Find("CrimeScene/SatinWood");m.SetFloat("_GrainContrast",.60f);Finish(.29f,.19f);}
   if(texture.Contains("/PBRInterior/BronzeBrushed_"))Finish(.42f,.20f);
   if(texture.Contains("SuedeCream_")||texture.Contains("FineSuede_"))Finish(.09f,.14f);
   if(texture.Contains("FineLeather_"))Finish(.23f,.19f);
   if(m.name=="ShowcaseSatinNickel")Finish(.37f,.16f);
   if(m.name=="RefinedRetailFloor")Finish(.34f,.13f);
   if(m.name.StartsWith("CeilingDiffuser_Diffuser"))Finish(.20f,.13f);
   if(path.Contains("/OmanStreet/")){
    if(new[]{"Ivory","Sand","Chalk","WarmRender","ConcreteTrim"}.Contains(m.name))Finish(.055f,.15f);
    if(m.name=="DustyPaintedSteel"){
     m.SetTexture("_BumpMap",DemoAssetLibrary.NormalMap("RealismTextures","Surfaces/PaintedMetalChipped_Normal.png"));m.EnableKeyword("_NORMALMAP");Finish(.24f,.12f);
    }
    if(m.name.StartsWith("ShutterCondition_")||m.name=="FadedShutter")Finish(m.name.EndsWith("_0")?.23f:.14f,.16f);
   }
   if(changed){EditorUtility.SetDirty(m);count++;}
  }
  Debug.Log("WORLD_SURFACES_REFINED "+count+" wood, fabric, hardware, floor, plaster, shutters and municipal steel materials");
 }
 static void WorldShopDisarray(){
  var lighter=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Lighter");
  lighter.localRotation=Quaternion.Euler(90,-30,0);lighter.position=new Vector3(-.62f,0,3.42f);
  if(!DemoAssetLibrary.WorldBounds(lighter.gameObject,out var b,false))throw new Exception("Lighter has no visible model");
  lighter.position+=Vector3.up*(.001f-b.min.y);
  var clue=root.GetComponentInChildren<EvidenceRegister>().Clues.First(c=>c&&c.Id=="E25");
  clue.transform.position=new Vector3(lighter.position.x,.016f,lighter.position.z);
  // The stock itself remains on its shelves. Move only the loose tray inserts,
  // by a few centimetres, to break the pristine alignment inside disturbed cases.
  var inserts=root.Find("Dressing/BespokeShowcaseInserts");
  float[] angles={-2.4f,1.8f,-3.1f,2.5f,-1.4f,3.0f,-2.0f,1.2f};int i=0;
  foreach(Transform t in inserts){
   // Existing insert groups use the source cabinet's world origin/orientation.
   // Keep their established pivot; only the mesh child carries the small offset.
   var mesh=t.GetChild(0);mesh.localRotation=Quaternion.Euler(0,angles[i],0);mesh.localPosition=new Vector3(i%2==0?.012f:-.016f,0,i%3==0?.009f:-.006f);i++;
  }
  Debug.Log("WORLD_SHOP_DISARRAY lighter seated on its side; eight loose display arrangements offset within their cases");
 }
 static void WorldCrowdHeadChecks(){
  foreach(var original in root.GetComponentsInChildren<CrowdBystander>()){
   var clone=Object.Instantiate(original.gameObject,original.transform.parent);clone.hideFlags=HideFlags.DontSave;
   try{
    var c=clone.GetComponent<CrowdBystander>();c.CharacterAnimation.Stop();var rest=c.Head.localRotation;
    for(int j=0;j<1200;j++)c.LookTowardsScene(1f/60);
    float limit=c.Role==CrowdBystander.Behaviour.Phone?12:28;
    if(Quaternion.Angle(rest,c.Head.localRotation)>limit+.1f)throw new Exception("Culled animation accumulated an excessive head turn: "+c.name);
    var stopped=c.Head.localRotation;for(int j=0;j<600;j++)c.LookTowardsScene(1f/30);
    if(Quaternion.Angle(stopped,c.Head.localRotation)>.05f)throw new Exception("Head aim drifts when the animation is stopped");
    // A fresh animation sample must replace the baseline instead of being erased.
    c.CharacterAnimation.Play("Motion");c.CharacterAnimation["Motion"].normalizedTime=.65f;c.CharacterAnimation.Sample();var sampled=c.Head.localRotation;c.LookTowardsScene(.016f);
    if(Quaternion.Angle(sampled,c.Head.localRotation)>limit+.1f)throw new Exception("Head aim exceeded the new authored pose");
   }finally{Object.DestroyImmediate(clone);}
  }
  Debug.Log("CROWD_HEAD_PASS stopped/culling regression, bounded turns, fresh authored-pose preservation");
 }
 static void WorldRefinementChecks(){
  CrowdStructureChecks();CrowdPoseAudit();CrowdBehaviourChecks();WorldCrowdHeadChecks();PatrolCarChecks();ShopReviewChecks();OmanChecks();GeneralSceneReview();
  var lighter=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Lighter");
  if(!DemoAssetLibrary.WorldBounds(lighter.gameObject,out var b,false)||Mathf.Abs(b.min.y-.001f)>.002f||b.size.y>.04f)throw new Exception("Dropped lighter is upright or floating: "+b);
  foreach(Transform t in root.Find("Dressing/BespokeShowcaseInserts")){
   var mesh=t.GetChild(0).GetComponent<MeshFilter>();
   foreach(var v in mesh.sharedMesh.vertices){var p=t.InverseTransformPoint(mesh.transform.TransformPoint(v));if(Mathf.Abs(p.x)>.85f||Mathf.Abs(p.z)>.30f)throw new Exception("Disturbed insert extends beyond cabinet: "+t.name);}
  }
 }
 static void WorldRefinementApply(){
  FinishOpen();CrowdGathering();WorldSurfaceRefinement();WorldShopDisarray();Physics.SyncTransforms();WorldRefinementChecks();
  // Checks sample clips. Recreate only the spectators before saving the scene.
  CrowdGathering();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveOpenScenes();
 }
 static void WorldMaterialReport(string label){
  Directory.CreateDirectory("Builds/WorldRefinement/"+label);
  var report=new List<string>();
  foreach(var m in root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().OrderBy(m=>m.name)){
   var fields=new[]{"_Metallic","_Glossiness","_GlossMapScale","_BumpScale","_OcclusionStrength"}.Where(m.HasProperty).Select(p=>p+"="+m.GetFloat(p));
   report.Add(m.name+" | "+m.shader.name+" | "+m.color+" | "+string.Join(" ",fields)+" | "+AssetDatabase.GetAssetPath(m)+" | texture="+AssetDatabase.GetAssetPath(m.mainTexture));
  }
  File.WriteAllLines("Builds/WorldRefinement/"+label+"/materials.txt",report);
 }
 static void WorldReviewCapture(string label){
  string folder="Builds/WorldRefinement/"+label;Directory.CreateDirectory(folder);
  CrowdPreview();RoadPreview();ShopDetailPreview("world-"+label);PatrolPreview("world-"+label,true);
  foreach(string pattern in new[]{"crowd-review-*.png","road-sidewalk-*.png","shop-detail-world-"+label+"-*.png"})
   foreach(var f in Directory.GetFiles("Builds",pattern))File.Copy(f,folder+"/"+Path.GetFileName(f),true);
  WorldLighterPreview(folder);
 }
 static void WorldLighterPreview(string folder){
  var go=new GameObject("Temporary dropped lighter review");var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.nearClipPlane=.025f;c.fieldOfView=48;
  var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);foreach(var r in lighting.FlashlightModel.GetComponentsInChildren<Renderer>())r.enabled=false;
  var rt=new RenderTexture(1280,900,24){antiAliasing=4};c.targetTexture=rt;var previous=RenderTexture.active;
  try{
   c.transform.position=new Vector3(-.40f,.38f,2.95f);c.transform.LookAt(new Vector3(-.62f,.018f,3.42f));lighting.FlashlightLight.transform.SetPositionAndRotation(c.transform.position,c.transform.rotation);lighting.FlashlightLight.intensity*=.25f;
   c.Render();RenderTexture.active=rt;var t=new Texture2D(1280,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1280,900),0,0);t.Apply();File.WriteAllBytes(folder+"/dropped-lighter.png",t.EncodeToPNG());Object.DestroyImmediate(t);
  }finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
 }
 public static void WorldRefinementAuditRun(){try{
  FinishOpen();GeneralSceneReview();WorldMaterialReport("before");WorldReviewCapture("before");Debug.Log("WORLD_VISUAL_AUDIT_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void WorldRefinementPreviewRun(){try{
  WorldRefinementApply();WorldMaterialReport("after");WorldReviewCapture("after");Debug.Log("WORLD_REFINEMENT_PREVIEW_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void WorldRefinementDeliveryRun(){try{
  WorldRefinementApply();NightStoreValidation.PresentationChecks();
  if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();
  WorldMaterialReport("after");WorldReviewCapture("after");FinishOpen();WorldRefinementChecks();FinishActualGroundCheck();
  // Regression checks instantiate and sample objects; strict build reloads the
  // saved scene, containing only the final authored poses and objects.
  FinishOpen();EditableDelivery.Build();Debug.Log("WORLD_REFINEMENT_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
