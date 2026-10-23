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
 static void VisibleGlass(){
  foreach(var m in root.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader.name=="CrimeScene/DisplayGlass").Distinct()){
   float alpha=m.name.Contains("FreshCutEdges")?.24f:m.name.Contains("DebrisFaces")?.095f:m.name.Contains("Edges")?.19f:.13f;
   m.color=new Color(.94f,.97f,1,alpha);m.SetFloat("_MicroVariation",.018f);m.SetFloat("_Smoothness",m.name.Contains("Edges")?.83f:.94f);EditorUtility.SetDirty(m);
  }
 }
 static void AdditionalEvidence(){
  var blood=root.Find("Dressing/FloorBloodTraces");if(!blood)throw new Exception("Existing blood trail missing");blood.gameObject.SetActive(true);
  for(int i=0;i<6;i++){
   var visible=blood.Find("SmallDriedBloodDrops_"+i);var uv=blood.Find("EvidenceUV_FloorDrops_"+i);if(!visible||!uv)throw new Exception("Blood trace pair missing");
   float size=new[]{.05f,.054f,.045f,.052f,.048f,.04f}[i];visible.localScale=new Vector3(size,size,1);uv.localScale=visible.localScale;
   var mask=DropMask(i);uv.GetComponent<Renderer>().sharedMaterial.mainTexture=mask;var m=visible.GetComponent<Renderer>().sharedMaterial;m.mainTexture=mask;m.color=new Color(.28f,.041f,.024f,.94f);m.SetFloat("_Glossiness",.21f);EditorUtility.SetDirty(m);
  }
  var group=Fresh("AdditionalPhysicalEvidence",root.Find("Dressing"));
  var dust=Mat(Folder,"PartialDustTransfer",new Color(.56f,.49f,.37f,.19f),.025f);dust.shader=Shader.Find("CrimeScene/SurfaceDecal");dust.mainTexture=UVTraceBuilder.MaskTexture(UVTraceBuilder.Mask.SolePrint);dust.renderQueue=2460;
  var steps=new[]{new Vector3(-.27f,.001f,1.04f),new Vector3(.11f,.001f,1.67f),new Vector3(-.18f,.001f,2.32f)};
  for(int i=0;i<steps.Length;i++){
   var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name="PartialDustFootwear_"+i;g.transform.SetParent(group,false);g.transform.localPosition=steps[i];g.transform.localRotation=Quaternion.Euler(90,new[]{-9,7,-14}[i],0);g.transform.localScale=new Vector3(.115f,.27f,1);Object.DestroyImmediate(g.GetComponent<Collider>());
   var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=dust;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;
  }
  var fabric=Mat(Folder,"BlueThreadFibres",new Color(.055f,.10f,.20f),.08f);var fiber=new KitMesh{Lightmapped=false};
  for(int i=0;i<4;i++){
   var a=new Vector3(.67f+i*.006f,.002f,1.19f+i*.003f);var b=a+new Vector3(.006f,.001f,.008f);fiber.Cylinder(fabric,a,b,.0007f,5);fiber.Cylinder(fabric,b,b+new Vector3(.007f,-.0005f,-.003f),.0006f,5);
  }
  fiber.Emit("CaughtBlueFabricFibres",group,Vector3.zero,Quaternion.identity,Folder,"blue_fibres");
  var gold=Mat(Folder,"BrokenClaspGold",new Color(.56f,.36f,.11f),.52f);gold.SetFloat("_Metallic",.9f);var clasp=new KitMesh{Lightmapped=false};
  for(int i=0;i<12;i++){
   float a=(i*24-60)*Mathf.Deg2Rad,b=((i+1)*24-60)*Mathf.Deg2Rad;clasp.Cylinder(gold,new Vector3(-1.03f+Mathf.Cos(a)*.012f,.003f,4.06f+Mathf.Sin(a)*.008f),new Vector3(-1.03f+Mathf.Cos(b)*.012f,.003f,4.06f+Mathf.Sin(b)*.008f),.0013f,6);
  }
  clasp.Emit("BentBrokenJewelleryClasp",group,Vector3.zero,Quaternion.identity,Folder,"broken_clasp");
  var state=Object.FindAnyObjectByType<CrimeSceneState>();if(state)state.RobbedOnly=state.RobbedOnly.Where(g=>g&&g.name!="AdditionalPhysicalEvidence").Concat(new[]{group.gameObject}).ToArray();
 }
 static void DistantStreetLife(){
  CrowdGathering();
 }
 static void InvestigationChecks(){
  ShopReviewChecks();ArchitecturalGlassChecks();RawCarChecks();GeneralSceneReview();
  var blood=root.Find("Dressing/FloorBloodTraces");if(!blood.gameObject.activeInHierarchy)throw new Exception("Blood trail hidden");
  if(blood.GetComponentsInChildren<MeshRenderer>().Count(r=>r.name.StartsWith("SmallDriedBloodDrops"))!=6)throw new Exception("Six sparse visible blood transfers required");
  foreach(var m in root.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader.name=="CrimeScene/DisplayGlass").Distinct())if(m.color.a<.085f||m.color.a>.30f)throw new Exception("Glass is invisible or excessively opaque: "+m.name);
  var life=root.Find("Exterior/DistantStreetLife");if(life.GetComponentsInChildren<Light>().Length!=0)throw new Exception("Street dressing must not add light cost");
  CrowdStructureChecks();
  Debug.Log("INVESTIGATION_REFINEMENT_CHECKS_PASS floor-seated VR/desktop release; stronger clear glass; six small visible blood clusters; dust, fibres, broken clasp; five animated spectators; rejected civilian cars excluded; no extra lights");
 }
 static void InvestigationPreview(){
  var go=new GameObject("Temporary investigation review camera");var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.fieldOfView=56;c.farClipPlane=220;
  var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);var torch=lighting.FlashlightLight;foreach(var r in lighting.FlashlightModel.GetComponentsInChildren<Renderer>())r.enabled=false;var rt=new RenderTexture(1440,900,24);c.targetTexture=rt;var previous=RenderTexture.active;
  var views=new[]{(new Vector3(-1.4f,1.5f,-2.4f),new Vector3(.2f,.8f,.4f)),(new Vector3(.7f,.48f,.45f),new Vector3(.62f,.0f,1.5f)),(new Vector3(-.4f,.38f,3.3f),new Vector3(-1,.01f,4.05f)),(new Vector3(.75f,.16f,1.0f),new Vector3(.75f,.003f,1.35f)),(new Vector3(-.3f,.30f,.7f),new Vector3(-.27f,.001f,1.04f)),(new Vector3(-24,1.65f,-15),new Vector3(-32.5f,1.2f,-21.7f)),(new Vector3(24,1.65f,-15),new Vector3(32.5f,1.2f,-21.5f)),(new Vector3(2.6f,1.2f,3.5f),new Vector3(4.5f,1.04f,4.7f))};
  try{for(int i=0;i<views.Length;i++){
   c.transform.position=views[i].Item1;c.transform.LookAt(views[i].Item2);torch.transform.SetPositionAndRotation(c.transform.position+c.transform.right*.1f,c.transform.rotation);c.Render();RenderTexture.active=rt;var im=new Texture2D(1440,900,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1440,900),0,0);im.Apply();File.WriteAllBytes("Builds/investigation-"+i+".png",im.EncodeToPNG());Object.DestroyImmediate(im);
  }}finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
 }
 static void EvidenceRenderCheck(){
  var mark=root.Find("Dressing/FloorBloodTraces/SmallDriedBloodDrops_0");var r=mark.GetComponent<Renderer>();
  var go=new GameObject("Temporary evidence visibility camera");var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.orthographic=true;c.orthographicSize=.07f;
  c.transform.position=mark.position+new Vector3(0,.35f,-.035f);c.transform.LookAt(mark.position);
  var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=2;light.intensity=1.3f;light.shadows=LightShadows.None;
  var target=new RenderTexture(512,512,24);c.targetTexture=target;var previous=RenderTexture.active;Texture2D a=null,b=null;
  Texture2D Capture(){c.Render();RenderTexture.active=target;var t=new Texture2D(512,512,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,512,512),0,0);t.Apply();return t;}
  try{
   a=Capture();r.enabled=false;b=Capture();r.enabled=true;var ap=a.GetPixels();var bp=b.GetPixels();int changed=0;for(int i=0;i<ap.Length;i++)if(Mathf.Max(Mathf.Abs(ap[i].r-bp[i].r),Mathf.Abs(ap[i].g-bp[i].g),Mathf.Abs(ap[i].b-bp[i].b))>.015f)changed++;
   var texture=(Texture2D)r.sharedMaterial.mainTexture;Debug.Log("BLOOD_DIAGNOSTIC position="+mark.position+" normal="+(-mark.forward)+" scale="+mark.lossyScale+" alpha="+texture.GetPixels().Max(p=>p.a)+" changed="+changed);
   File.WriteAllBytes("Builds/blood-visibility.png",a.EncodeToPNG());if(changed<400||texture.GetPixels().Max(p=>p.a)<.5f)throw new Exception("Blood decal remains too transparent to observe");
   Debug.Log("EVIDENCE_RENDER_PASS sparse floor blood differs from clean floor by "+changed+" pixels");
  }finally{r.enabled=true;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);if(a)Object.DestroyImmediate(a);if(b)Object.DestroyImmediate(b);Object.DestroyImmediate(go);}
 }
 public static void InvestigationVisualRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;EvidenceRenderCheck();InvestigationPreview();EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void InvestigationRefinementRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");
  string signature=CarGeometrySignature();VisibleGlass();AdditionalEvidence();DistantStreetLife();Physics.SyncTransforms();
  if(root.Find("InvestigationFieldwork"))EvidenceGameplay();
  if(signature!=CarGeometrySignature())throw new Exception("Police car triangles changed");InvestigationChecks();NightStoreValidation.PresentationChecks();
  EditorSceneManager.MarkSceneDirty(root.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();
  EvidenceRenderCheck();InvestigationPreview();ArchitecturalGlassPreview("visible");Debug.Log("INVESTIGATION_REFINEMENT_REVIEW_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void InvestigationDeliveryRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");InvestigationChecks();EvidenceRenderCheck();NightStoreValidation.PresentationChecks();EditableDelivery.Build();Debug.Log("INVESTIGATION_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
