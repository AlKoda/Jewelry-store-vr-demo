#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
public static partial class PresentationPolish {
 public static void PoliceLiveryInspectRun(){try{
 EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;
 var car=root.Find("Exterior/PoliceCar");var model=car.Find("SuppliedModel");
 var lines=new System.Collections.Generic.List<string>();
 foreach(var f in model.GetComponentsInChildren<MeshFilter>()) {var m=f.sharedMesh;var v=m.vertices;var uv=m.uv;var t=m.triangles;
 for(int i=0;i<t.Length;i+=3)for(int k=0;k<3;k++){int j=t[i+k];var p=car.InverseTransformPoint(f.transform.TransformPoint(v[j]));lines.Add($"{p.x:R},{p.y:R},{p.z:R},{uv[j].x:R},{uv[j].y:R}");}}
 Directory.CreateDirectory("Builds");File.WriteAllLines("Builds/car-triangles.csv",lines);RawCarPreview("inspection");Debug.Log("CAR_INSPECTION_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 const string LiveryFolder="Assets/CrimeSceneDemo/ThirdParty/InvestigationDetail";
 static void ApplyPoliceLivery(Transform model){
  var shader=Shader.Find("CrimeScene/OmanPoliceCar");if(!shader||!shader.isSupported)throw new Exception("Authored car shader unavailable");
  var m=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/SuppliedOmanPolicePaint.mat");if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,Folder+"/SuppliedOmanPolicePaint.mat");}m.shader=shader;m.color=Color.white;
  m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(LiveryFolder+"/PoliceAuthoredPaint.png");m.SetTexture("_SurfaceMap",AssetDatabase.LoadAssetAtPath<Texture2D>(LiveryFolder+"/PoliceSurfaceResponse.png"));m.SetFloat("_DebugGeometryNormals",0);
  if(!m.mainTexture||!m.GetTexture("_SurfaceMap"))throw new Exception("Authored paint assets missing");
  foreach(var r in model.GetComponentsInChildren<MeshRenderer>(true)){r.sharedMaterials=Enumerable.Repeat(m,r.sharedMaterials.Length).ToArray();r.receiveGI=ReceiveGI.LightProbes;r.lightmapIndex=-1;r.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.BlendProbes;}EditorUtility.SetDirty(m);
 }
 static void PoliceLiveryChecks(){
  RawCarChecks();FlatCarNormalsRenderCheck("CrimeScene/OmanPoliceCar");var model=root.Find("Exterior/PoliceCar/SuppliedModel");
  foreach(var r in model.GetComponentsInChildren<MeshRenderer>())foreach(var m in r.sharedMaterials){
   if(m.shader.name!="CrimeScene/OmanPoliceCar"||AssetDatabase.GetAssetPath(m.mainTexture)!=LiveryFolder+"/PoliceAuthoredPaint.png"||AssetDatabase.GetAssetPath(m.GetTexture("_SurfaceMap"))!=LiveryFolder+"/PoliceSurfaceResponse.png")throw new Exception("Car uses a photographic or missing livery");
  }
  Debug.Log("POLICE_LIVERY_CHECKS_PASS clean authored paint/response, original 7317 triangle FBX; no photographic albedo, emission or added geometry");
 }
 public static void PoliceLiveryRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;
  string signature=CarGeometrySignature();
  foreach(string name in new[]{"PoliceAuthoredPaint","PoliceSurfaceResponse","PoliceDoorMarkings","PoliceArabicPlate"}){
   var path=LiveryFolder+"/"+name+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)throw new Exception("Missing livery image "+path);
   importer.sRGBTexture=name!="PoliceSurfaceResponse";importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;
   var android=importer.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;android.maxTextureSize=2048;android.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();
  }
  ApplyPoliceLivery(root.Find("Exterior/PoliceCar/SuppliedModel"));if(signature!=CarGeometrySignature())throw new Exception("Car geometry changed");
  PoliceLiveryChecks();GeneralSceneReview();ShopAtmosphereChecks();NightStoreValidation.PresentationChecks();
  AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();RawCarPreview("authored");Debug.Log("POLICE_LIVERY_REVIEW_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void PoliceLiveryDeliveryRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;PoliceLiveryChecks();GeneralSceneReview();EditableDelivery.Build();Debug.Log("POLICE_LIVERY_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}

}
#endif

