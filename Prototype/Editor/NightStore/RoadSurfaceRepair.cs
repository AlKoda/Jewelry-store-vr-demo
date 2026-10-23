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
 const string RoadArt="Assets/CrimeSceneDemo/ThirdParty/OmanExterior/";
 static Texture2D RoadTexture(string name,bool data=false,bool readable=false,bool normal=false,int size=1024){
  string path=RoadArt+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
  var t=(TextureImporter)AssetImporter.GetAtPath(path);if(!t)throw new Exception("Missing road texture "+path);
  t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
  t.sRGBTexture=!data&&!normal;t.isReadable=readable;t.mipmapEnabled=true;t.maxTextureSize=size;
  t.npotScale=TextureImporterNPOTScale.None;t.alphaIsTransparency=false;t.wrapMode=TextureWrapMode.Repeat;t.anisoLevel=4;
  t.textureCompression=TextureImporterCompression.CompressedHQ;
  if(normal){t.convertToNormalmap=true;t.heightmapScale=.025f;}
  t.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=size,format=TextureImporterFormat.ASTC_6x6});
  t.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 }
 static void RoadWrite(string name,Texture2D image){File.WriteAllBytes(RoadArt+name+".png",image.EncodeToPNG());Object.DestroyImmediate(image);AssetDatabase.ImportAsset(RoadArt+name+".png");}
 static void RoadPrepareMaps(){
  var masks=new[]{"RoadNetworkOriginal","RoadStressOriginal","RoadFatigueOriginal","PavingFracturesOriginal"}.Select(n=>RoadTexture(n,true,true)).ToArray();
  const int n=1024;var packed=new Texture2D(n,n,TextureFormat.RGBA32,false,true);var pixels=new Color32[n*n];
  for(int y=0;y<n;y++)for(int x=0;x<n;x++){
   float u=(x+.5f)/n,v=(y+.5f)/n;pixels[y*n+x]=new Color(masks[0].GetPixelBilinear(u,v).r,masks[1].GetPixelBilinear(u,v).r,masks[2].GetPixelBilinear(u,v).r,masks[3].GetPixelBilinear(u,v).r);
  }
  packed.SetPixels32(pixels);packed.Apply();RoadWrite("GroundFracturesPacked",packed);
  foreach(var m in masks){var t=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m));t.isReadable=false;t.SaveAndReimport();}
  // A single flush material layer follows exact scene-space bay dimensions.
  const int w=2048,h=1024;var mask=new Texture2D(w,h,TextureFormat.RGB24,false,true);var paint=new Color32[w*h];
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   float px=-26+(x+.5f)/w*52,pz=-20+(y+.5f)/h*17;float d=100;
   for(int side=0;side<2;side++)for(float bx=-22.5f;bx<=22.5f;bx+=2.5f){
    if(side==0&&bx>-8&&bx<9)continue;float front=side==0?-3.08f:-19.92f,back=side==0?-8.03f:-14.97f;
    if(pz>=Mathf.Min(front,back)&&pz<=Mathf.Max(front,back))d=Mathf.Min(d,Mathf.Abs(px-bx));
    if(px>=bx&&px<=bx+2.5f)d=Mathf.Min(d,Mathf.Abs(pz-back));
   }
   byte value=(byte)(255*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.027f,.060f,d))));paint[y*w+x]=new Color32(value,value,value,255);
  }
  int coverage=paint.Count(c=>c.r>128);if(coverage<w*h*.008f||coverage>w*h*.08f)throw new Exception("Parking mask covers wrong road area: "+coverage/(float)(w*h));
  mask.SetPixels32(paint);mask.Apply();RoadWrite("ParkingLayoutMask",mask);
  foreach(string b in new[]{"RoadAggregate","PavingAggregate"}){
   File.Copy(RoadArt+b+"Original.png",RoadArt+b+"Normal.png",true);AssetDatabase.ImportAsset(RoadArt+b+"Normal.png");RoadTexture(b+"Normal",true,false,true);
  }
 }
 static void RoadMaterials(){
  var shader=Shader.Find("CrimeScene/OmanGround");if(!shader)throw new Exception("Ground shader absent");
  var cracks=RoadTexture("GroundFracturesPacked",true);var age=RoadTexture("RoadWeatherOriginal",true);var layout=RoadTexture("ParkingLayoutMask",true,false,false,2048);
  for(int kind=0;kind<2;kind++){
   var mat=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/"+(kind==0?"DryRoad":"DustyPaving")+".mat");if(!mat)throw new Exception("Existing ground material absent");
   mat.shader=shader;mat.shaderKeywords=new string[0];mat.renderQueue=-1;mat.color=kind==0?new Color(.69f,.68f,.65f):new Color(.77f,.76f,.72f);
   mat.mainTexture=RoadTexture(kind==0?"RoadAggregateOriginal":"PavingAggregateOriginal");mat.mainTextureScale=Vector2.one;
   mat.SetTexture("_AgeMap",age);mat.SetTexture("_CrackMasks",cracks);mat.SetTexture("_PaintMask",layout);
   mat.SetTexture("_BumpMap",RoadTexture(kind==0?"RoadAggregateNormal":"PavingAggregateNormal",true,false,true));
   mat.SetFloat("_BumpScale",kind==0?.23f:.18f);mat.SetFloat("_SurfaceType",kind);mat.SetFloat("_Glossiness",kind==0?.10f:.08f);EditorUtility.SetDirty(mat);
  }
 }
 static readonly List<Bounds> roadSeals=new List<Bounds>();
 static void RoadSealWalls(){
  var group=Fresh("PartyWallSeals",oman);var mat=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/DarkRecess.mat");var k=new KitMesh{LightmapScale=.12f};roadSeals.Clear();
  foreach(string rowName in new[]{"NearShopRow","OppositeShopRow"}){
   var row=oman.Find(rowName);var shops=row.Cast<Transform>().OrderBy(t=>t.localPosition.x).ToArray();
   for(int i=1;i<shops.Length;i++){
    var a=shops[i-1];var b=shops[i];if(b.localPosition.x-a.localPosition.x>8)continue;
    float x=(a.localPosition.x+b.localPosition.x)*.5f;
    float roof=Mathf.Min(a.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Facade_")).Max(r=>r.bounds.max.y),b.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Facade_")).Max(r=>r.bounds.max.y))-.03f;
    var center=row.TransformPoint(V(x,roof*.5f,4.55f));var bounds=new Bounds(center,V(.19f,roof,8.06f));
    k.Box(mat,center,bounds.size);roadSeals.Add(bounds);
   }
  }
  // Solid recessed service piers close the junction to the original store shell.
  var cream=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/Sand.mat");
  foreach(float s in new[]{-1f,1f}){var b=new Bounds(V(s*8.54f,3.55f,4.3f),V(1.22f,7.1f,8.12f));k.Box(cream,b.center,b.size);roadSeals.Add(b);}
  OmanEmit(k,"ClosedPartyWallJunctions",group,.12f);
 }
 static void RoadChecks(){
  var road=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/DryRoad.mat");var paving=AssetDatabase.LoadAssetAtPath<Material>(OmanFolder+"/DustyPaving.mat");
  if(road.shader.name!="CrimeScene/OmanGround"||paving.shader!=road.shader||road.renderQueue!=2000)throw new Exception("Ground must use single opaque material layers");
  if(oman.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("EmptyParkingBays_")))throw new Exception("Raised parking geometry remains");
  if(roadSeals.Count!=28)throw new Exception("Missing party-wall junction: "+roadSeals.Count);
  var sealFilter=oman.Find("PartyWallSeals/ClosedPartyWallJunctions").GetComponent<MeshFilter>();
  var vertices=sealFilter.sharedMesh.vertices.Select(sealFilter.transform.TransformPoint).ToArray();var triangles=sealFilter.sharedMesh.triangles;
  foreach(var b in roadSeals)foreach(float height in new[]{.2f,.5f,.9f}){
   var origin=b.center+V(0,b.size.y*(height-.5f),b.center.z<-10?8:-8);var ray=new Ray(origin,b.center.z<-10?Vector3.back:Vector3.forward);
   bool hit=false;for(int i=0;i<triangles.Length;i+=3){
    var a=vertices[triangles[i]];var e1=vertices[triangles[i+1]]-a;var e2=vertices[triangles[i+2]]-a;var cross=Vector3.Cross(ray.direction,e2);float det=Vector3.Dot(e1,cross);if(Mathf.Abs(det)<.000001f)continue;
    var delta=ray.origin-a;float u=Vector3.Dot(delta,cross)/det;if(u<0||u>1)continue;var q=Vector3.Cross(delta,e1);float v=Vector3.Dot(ray.direction,q)/det;if(v<0||u+v>1)continue;
    if(Vector3.Dot(e2,q)/det>0){hit=true;break;}
   }
   if(!hit)throw new Exception("Rendered seam mesh misses sightline");
  }
  var importer=(TextureImporter)AssetImporter.GetAtPath(RoadArt+"GroundFracturesPacked.png");if(importer.sRGBTexture||!importer.mipmapEnabled)throw new Exception("Mask import must be linear and filtered");
  Debug.Log("ROAD_SURFACE_CHECKS_PASS: three road fracture masks + concrete fractures, flush worn parking paint, 28 opaque seam backings, zero added lights/physics");
 }
 static void RoadLayerGpuCheck(){
  var stage=new GameObject("Temporary opaque layer test");var plane=GameObject.CreatePrimitive(PrimitiveType.Plane);plane.transform.SetParent(stage.transform);plane.transform.position=V(-12,500,-5);plane.layer=31;
  var mat=new Material(Shader.Find("CrimeScene/OmanGround"));plane.GetComponent<Renderer>().sharedMaterial=mat;
  mat.mainTexture=Texture2D.grayTexture;mat.color=Color.white;mat.SetTexture("_AgeMap",Texture2D.whiteTexture);mat.SetTexture("_PaintMask",Texture2D.whiteTexture);mat.SetFloat("_Glossiness",0);
  var camGo=new GameObject("Test camera");camGo.transform.SetParent(stage.transform);var c=camGo.AddComponent<Camera>();c.transform.position=plane.transform.position+Vector3.up*5;c.transform.LookAt(plane.transform.position);c.cullingMask=1<<31;c.orthographic=true;c.orthographicSize=1;c.stereoTargetEye=StereoTargetEyeMask.None;c.clearFlags=CameraClearFlags.SolidColor;
  var lightGo=new GameObject("Test directional");lightGo.transform.SetParent(stage.transform);var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(90,0,0);light.cullingMask=1<<31;light.intensity=1;
  var rt=new RenderTexture(64,64,24);c.targetTexture=rt;var previous=RenderTexture.active;bool fog=RenderSettings.fog;RenderSettings.fog=false;
  var data=new Texture2D(64,64,TextureFormat.RGB24,false);var crack=new Texture2D(1,1,TextureFormat.RGBA32,false,true);float paint=0;
  try{for(int channel=-1;channel<3;channel++){
   crack.SetPixel(0,0,channel<0?new Color(0,0,0,0):channel==0?new Color(1,0,0,0):channel==1?new Color(0,1,0,0):new Color(0,0,1,0));crack.Apply();mat.SetTexture("_CrackMasks",crack);c.Render();RenderTexture.active=rt;data.ReadPixels(new Rect(0,0,64,64),0,0);data.Apply();float value=data.GetPixel(32,32).grayscale;
   if(channel<0)paint=value;else if(value>paint*.65f)throw new Exception("Crack channel did not cut through paint: "+channel+" "+value+" / "+paint);
  }Debug.Log("ROAD_GPU_LAYERING_PASS: network, stress and fatigue cracks each darken painted surface after paint compositing");}
  finally{RenderSettings.fog=fog;RenderTexture.active=previous;Object.DestroyImmediate(data);Object.DestroyImmediate(crack);Object.DestroyImmediate(mat);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(stage);}
 }
 static void RoadPreview(){
  var go=new GameObject("Temporary ground inspection");var camera=go.AddComponent<Camera>();NightQuality.Configure(camera);camera.stereoTargetEye=StereoTargetEyeMask.None;camera.fieldOfView=65;camera.farClipPlane=150;
  var torch=go.AddComponent<Light>();torch.type=LightType.Spot;torch.range=14;torch.spotAngle=78;torch.intensity=2.2f;torch.color=new Color(1,.91f,.77f);torch.shadows=LightShadows.Soft;
  var rt=new RenderTexture(1600,1000,24){antiAliasing=4};camera.targetTexture=rt;var old=RenderTexture.active;
  var views=new[]{(V(-16,1.7f,-7),V(-14,-.12f,-5)),(V(17,1.7f,-11),V(20,-.12f,-16)),(V(-12,1.5f,-2.65f),V(-14,0,-1.3f)),(V(18,1.5f,-20.5f),V(21,0,-21.3f)),(V(-16,.35f,-8),V(-12,-.12f,-5)),(V(-16.04f,.35f,-8.02f),V(-12,-.12f,-5)),(V(-12,1.7f,-11),V(-15,4,.5f)),(V(10,1.7f,-16),V(12,4,-23))};
  try{for(int i=0;i<views.Length;i++){camera.transform.position=views[i].Item1;camera.transform.LookAt(views[i].Item2);camera.Render();RenderTexture.active=rt;var img=new Texture2D(1600,1000,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1600,1000),0,0);img.Apply();File.WriteAllBytes("Builds/road-sidewalk-"+i+".png",img.EncodeToPNG());Object.DestroyImmediate(img);}}
  finally{RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
 }
 public static void RoadSurfaceRepairRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");
  string interior=OmanInteriorSignature();var car=root.Find("Exterior/PoliceCar").localToWorldMatrix;
  RoadPrepareMaps();RoadMaterials();var ground=oman.Find("ParkingAndPavements");foreach(var t in ground.Cast<Transform>().Where(t=>t.name.StartsWith("EmptyParkingBays_")).ToArray())Object.DestroyImmediate(t.gameObject);
  RoadSealWalls();OmanLegacyLeafCleanup();RoadChecks();RoadLayerGpuCheck();ArchitecturalGlassChecks();OmanChecks();GeneralSceneReview();ShopReviewChecks();RefinementChecks();RawCarChecks();
  if(interior!=OmanInteriorSignature()||car!=root.Find("Exterior/PoliceCar").localToWorldMatrix)throw new Exception("Protected content moved");
  AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();
  RoadPreview();OmanPreview();EditableDelivery.Build();Debug.Log("ROAD_SIDEWALK_LOCAL_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
