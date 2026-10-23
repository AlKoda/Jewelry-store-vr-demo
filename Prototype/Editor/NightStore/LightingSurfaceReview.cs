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
 static void ContinuousRoad(Transform parent){
  // One rectangular UV chart: no chart or renderer boundary crosses the playable road.
  var mesh=new Mesh{name="ContinuousAsphalt"};mesh.vertices=new[]{V(-70,-.12f,-20),V(-70,-.12f,-3),V(70,-.12f,-3),V(70,-.12f,-20)};
  mesh.triangles=new[]{0,1,2,0,2,3};mesh.normals=Enumerable.Repeat(Vector3.up,4).ToArray();
  mesh.uv=new[]{new Vector2(-70,-20),new Vector2(-70,-3),new Vector2(70,-3),new Vector2(70,-20)};
  // Half-texel-safe chart border, consistent with the explicit chart aspect.
  mesh.uv2=new[]{new Vector2(.004f,.004f),new Vector2(.004f,.996f),new Vector2(.996f,.996f),new Vector2(.996f,.004f)};
  mesh.RecalculateTangents();mesh.RecalculateBounds();string path=OmanFolder+"/ContinuousAsphalt.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
  if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
  var go=new GameObject("ContinuousAsphalt");go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=omanRoad;StoreKit.MarkStatic(go,true);r.scaleInLightmap=.7f;EditorUtility.SetDirty(mesh);
 }
 static void LightingSurfaceRepair(){
  omanRoad=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/DryRoad.mat");omanPaving=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/DustyPaving.mat");omanWhite=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/KerbWhite.mat");omanBlack=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/KerbBlack.mat");omanWalls=new[]{omanWhite,AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/Sand.mat")};
  OmanGround();OmanLegacyLeafCleanup();
  OmanBakeQuality();
 }
 static void OmanBakeQuality(){var s=Lightmapping.lightingSettings;s.directSampleCount=128;s.indirectSampleCount=512;s.environmentSampleCount=256;s.lightmapPadding=4;s.lightmapCompression=LightmapCompression.HighQuality;EditorUtility.SetDirty(s);}
 static void LightingSurfaceChecks(){
  var ground=oman.Find("ParkingAndPavements");var road=ground.Find("ContinuousAsphalt");if(!road)throw new Exception("Missing continuous road");var mesh=road.GetComponent<MeshFilter>().sharedMesh;
  if(mesh.vertexCount!=4||mesh.triangles.Length!=6||mesh.uv2.Length!=4)throw new Exception("Road must be one continuous two-triangle lighting chart");
  if(ground.GetComponentsInChildren<MeshRenderer>().Count(r=>r.sharedMaterials.Any(m=>m&&m.name=="DryRoad"))!=2)throw new Exception("Segmented asphalt remains");
  if(root.GetComponentsInChildren<Transform>().Any(t=>t.name=="DryLeavesAtTree"))throw new Exception("Floating legacy leaves remain active");
  if(road.GetComponent<MeshRenderer>().scaleInLightmap<.35f||ground.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Surface_")).Any(r=>r.scaleInLightmap<.45f))throw new Exception("Low ground lighting density returned");
  Debug.Log("LIGHTING_SURFACE_CHECKS_PASS continuous 140m asphalt chart, higher ground density, zero active hovering tree decals");
 }
 static void GrazingTorchShadows(){
  var stage=new GameObject("Temporary grazing shadow regression");stage.transform.position=Vector3.one*1200;
  var ground=new GameObject("Actual road receiver");ground.layer=31;ground.transform.SetParent(stage.transform,false);ground.AddComponent<MeshFilter>().sharedMesh=oman.Find("ParkingAndPavements/ContinuousAsphalt").GetComponent<MeshFilter>().sharedMesh;var r=ground.AddComponent<MeshRenderer>();r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/DryRoad.mat");
  var go=new GameObject("Grazing camera");go.transform.SetParent(stage.transform,false);var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.cullingMask=1<<31;c.transform.localPosition=V(0,1.6f,-9);c.transform.LookAt(stage.transform.position+V(0,-.12f,-5));c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Color.black;
  var rt=new RenderTexture(512,384,24);c.targetTexture=rt;var previous=RenderTexture.active;var fog=RenderSettings.fog;bool tier=DemoPlatform.ForceStandalone;int lights=QualitySettings.pixelLightCount;var shadows=QualitySettings.shadows;float distance=QualitySettings.shadowDistance;
  Color[] Capture(){c.Render();RenderTexture.active=rt;var image=new Texture2D(512,384,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,512,384),0,0);image.Apply();var pixels=image.GetPixels();Object.DestroyImmediate(image);return pixels;}
  try{RenderSettings.fog=false;QualitySettings.pixelLightCount=4;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=14;
   for(int platform=0;platform<2;platform++)for(int uv=0;uv<2;uv++)for(int grazing=0;grazing<2;grazing++){
    DemoPlatform.ForceStandalone=platform==1;var owner=new GameObject("Torch regression owner");owner.transform.SetParent(stage.transform,false);var control=owner.AddComponent<DemoLighting>();
    try{if(uv==0)control.Flashlight(true);else control.UVTorch(true);var l=uv==0?control.FlashlightLight:control.UVTorchLight;l.cullingMask=1<<31;l.transform.position=stage.transform.position+V(0,grazing==0?1.5f:.25f,-9);l.transform.LookAt(stage.transform.position+V(0,-.12f,-5));
     var type=l.shadows;l.shadows=LightShadows.None;var clean=Capture();l.shadows=type;var shadowed=Capture();int lit=0,artifacts=0;for(int i=0;i<clean.Length;i++){if(clean[i].maxColorComponent>.06f){lit++;if(clean[i].maxColorComponent-shadowed[i].maxColorComponent>.035f)artifacts++;}}
     if(lit<300||artifacts>Mathf.Max(30,lit*.01f))throw new Exception("Ground self-shadow artifact: tier="+platform+" uv="+uv+" low="+grazing+" artifacts="+artifacts+" lit="+lit);
     Debug.Log("GRAZING_TORCH_SHADOW_PASS tier="+platform+" uv="+uv+" low="+grazing+" artifacts="+artifacts+" lit="+lit);
    }finally{control.ReleaseTorchResources();Object.DestroyImmediate(owner);}
   }
  }finally{DemoPlatform.ForceStandalone=tier;RenderSettings.fog=fog;QualitySettings.pixelLightCount=lights;QualitySettings.shadows=shadows;QualitySettings.shadowDistance=distance;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(stage);}
 }
 public static void LightingSurfaceDeliveryRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");LightingSurfaceChecks();GrazingTorchShadows();NightStoreValidation.PresentationChecks();GeneralSceneReview();EditableDelivery.Build();Debug.Log("LIGHTING_SURFACE_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void LightingSurfaceRepairRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");string signature=OmanInteriorSignature();var car=root.Find("Exterior/PoliceCar").localToWorldMatrix;
  LightingSurfaceRepair();LightingSurfaceChecks();ArchitecturalGlassChecks();OmanChecks();GeneralSceneReview();ShopReviewChecks();RefinementChecks();NightStoreValidation.PresentationChecks();RawCarChecks();
  if(signature!=OmanInteriorSignature()||car!=root.Find("Exterior/PoliceCar").localToWorldMatrix)throw new Exception("Protected store/car changed");
  AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();LightingSeamPreview("after");RoadPreview();ArchitecturalGlassPreview("lighting");OmanPreview();
  Debug.Log("LIGHTING_SURFACE_REVIEW_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void LightingSurfaceInspectRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");
  var report=new List<string>();var settings=Lightmapping.lightingSettings;report.Add("Bake resolution="+settings.lightmapResolution+" atlas="+settings.lightmapMaxSize+" direct="+settings.directSampleCount+" indirect="+settings.indirectSampleCount);
  foreach(var l in root.GetComponentsInChildren<Light>())if(l.enabled)report.Add("LIGHT "+PathOf(l.transform)+" "+l.lightmapBakeType+" "+l.type+" mode="+l.renderMode+" shadow="+l.shadows+" intensity="+l.intensity);
  foreach(var r in root.GetComponentsInChildren<MeshRenderer>())if(r.bounds.min.y<.15f&&r.bounds.min.z< -2.9f&&r.bounds.max.z> -23)report.Add("GROUND "+PathOf(r.transform)+" bounds="+r.bounds+" scale="+r.scaleInLightmap+" lm="+r.lightmapIndex+" uv="+r.lightmapScaleOffset+" mats="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"NULL")));
  File.WriteAllLines("Builds/lighting-surface-inspect.txt",report);
  LightingSeamPreview("before");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void LightingSeamPreview(string label){
  var go=new GameObject("Temporary lighting seam review");var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.fieldOfView=65;c.farClipPlane=150;
  var rt=new RenderTexture(1200,850,24){antiAliasing=4};c.targetTexture=rt;var old=RenderTexture.active;
  var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);var torch=lighting.FlashlightLight;
  var road=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/DryRoad.mat");float bump=road.GetFloat("_BumpScale");var lightmaps=LightmapSettings.lightmaps;
  var views=new[]{(V(-1,1.6f,-8),V(0,-.12f,-4)),(V(-11,1.6f,-8),V(-10,-.12f,-4)),(V(9,1.6f,-8),V(10,-.12f,-4)),(V(0,1.6f,-4),V(2,-.12f,-8))};
  try{for(int mode=0;mode<4;mode++){
   torch.enabled=mode==3;road.SetFloat("_BumpScale",mode==1?0:bump);LightmapSettings.lightmaps=mode==2?new LightmapData[0]:lightmaps;
   for(int i=0;i<views.Length;i++){c.transform.position=views[i].Item1;c.transform.LookAt(views[i].Item2);torch.transform.SetPositionAndRotation(c.transform.position,c.transform.rotation);c.Render();RenderTexture.active=rt;var img=new Texture2D(1200,850,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1200,850),0,0);img.Apply();File.WriteAllBytes("Builds/lighting-"+label+"-"+mode+"-"+i+".png",img.EncodeToPNG());Object.DestroyImmediate(img);}
  }}finally{road.SetFloat("_BumpScale",bump);LightmapSettings.lightmaps=lightmaps;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
 }
}
#endif
