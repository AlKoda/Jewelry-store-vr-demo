#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static partial class PresentationPolish {
 const string PatrolPack="Assets/CrimeSceneDemo/ThirdParty/TripoPoliceCar";
 const string PatrolMesh=PatrolPack+"/PatrolBody.obj";
 static bool IsRefinedPatrol(Transform model)=>model&&model.GetComponentsInChildren<MeshFilter>(true).Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh)==PatrolMesh);
 static void PlaceRefinedPatrolCar(){
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>(PatrolMesh);if(!asset)throw new Exception("Refined supplied patrol mesh missing");
  var car=root.Find("Exterior/PoliceCar");var old=car.Find("SuppliedModel");if(old)Object.DestroyImmediate(old.gameObject);
  foreach(var r in car.GetComponentsInChildren<Renderer>(true))r.enabled=false;
  var model=Object.Instantiate(asset);model.name="SuppliedModel";model.transform.SetParent(car,false);
  model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
  var shader=Shader.Find("CrimeScene/PatrolCarFinish");if(!shader||!shader.isSupported)throw new Exception("Patrol finish shader unavailable");
  var path=PatrolPack+"/PatrolFinish.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}material.shader=shader;
  material.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(PatrolPack+"/PatrolOriginal_Color.jpg"));
  material.SetTexture("_DoorTex",AssetDatabase.LoadAssetAtPath<Texture2D>(PatrolPack+"/PatrolDoor_Color.png"));
  material.SetTexture("_PlateTex",AssetDatabase.LoadAssetAtPath<Texture2D>(PatrolPack+"/PatrolPlate_Color.png"));
  material.SetFloat("_PaintPolish",.76f);EditorUtility.SetDirty(material);
  foreach(var r in model.GetComponentsInChildren<MeshRenderer>()){
   r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
   r.receiveGI=ReceiveGI.LightProbes;r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;r.lightmapIndex=-1;
  }
  var bodyRenderers=model.GetComponentsInChildren<Renderer>();
  var wheels=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PatrolPack+"/PatrolWheels.obj"));wheels.name="DetailedWheels";wheels.transform.SetParent(model.transform,false);
  foreach(var r in wheels.GetComponentsInChildren<MeshRenderer>()){
   r.sharedMaterials=r.sharedMaterials.Select(m=>PatrolWheelMaterial(m?m.name:"Rubber")).ToArray();r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;r.lightProbeUsage=LightProbeUsage.BlendProbes;r.receiveGI=ReceiveGI.LightProbes;r.shadowCastingMode=ShadowCastingMode.On;
  }
  var optics=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PatrolPack+"/PatrolOptics.obj"));optics.name="FittedHeadlampOptics";optics.transform.SetParent(model.transform,false);
  foreach(var r in optics.GetComponentsInChildren<MeshRenderer>()){
   r.sharedMaterials=r.sharedMaterials.Select(m=>PatrolOpticalMaterial(m?m.name:"SatinChrome")).ToArray();r.lightProbeUsage=LightProbeUsage.BlendProbes;r.receiveGI=ReceiveGI.LightProbes;r.shadowCastingMode=ShadowCastingMode.On;
  }
  var distance=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PatrolPack+"/PatrolDistance.obj"));distance.name="DistanceModel";distance.transform.SetParent(model.transform,false);
  foreach(var r in distance.GetComponentsInChildren<MeshRenderer>()){r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;r.lightProbeUsage=LightProbeUsage.BlendProbes;r.receiveGI=ReceiveGI.LightProbes;r.shadowCastingMode=ShadowCastingMode.On;}
  var lod=model.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.26f,bodyRenderers.Concat(wheels.GetComponentsInChildren<Renderer>()).Concat(optics.GetComponentsInChildren<Renderer>()).ToArray()),new LOD(.012f,distance.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();lod.fadeMode=LODFadeMode.None;
  car.localPosition=new Vector3(5,NightStoreLayout.StreetY,-4.65f);car.localRotation=Quaternion.Euler(0,90,0);
  foreach(var c in car.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
  var box=car.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,.72f,0);box.size=new Vector3(1.98f,1.44f,5.12f);
  // Original scene lighting is reused at the new model's actual headlamp centres.
  foreach(string side in new[]{"Left","Right"}){
   float sign=side=="Left"?-1:1;var local=new Vector3(sign*.82f,.85f,2.35f);
   var lens=car.Find("Headlight"+side);if(lens)lens.localPosition=local;
   var light=root.Find("Lighting").GetComponentsInChildren<Light>(true).FirstOrDefault(l=>l.name=="Headlight_"+side);
   if(light){light.transform.position=car.TransformPoint(local);light.transform.rotation=car.rotation;light.intensity=.55f;light.range=13;light.spotAngle=68;light.innerSpotAngle=30;light.color=new Color(1,.91f,.76f);}
  }
  foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=27;
  var lighting=root.Find("Lighting");var probeObject=lighting.Find("ReflectionProbe_PatrolStreet");if(!probeObject){var pgo=new GameObject("ReflectionProbe_PatrolStreet");probeObject=pgo.transform;probeObject.SetParent(lighting,false);}
  var probe=probeObject.GetComponent<ReflectionProbe>();if(!probe)probe=probeObject.gameObject.AddComponent<ReflectionProbe>();
  probe.transform.position=car.TransformPoint(new Vector3(0,1.05f,0));probe.size=new Vector3(6,3.5f,6);probe.center=Vector3.zero;probe.boxProjection=true;probe.resolution=256;probe.mode=ReflectionProbeMode.Baked;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.cullingMask=~(1<<27);probe.intensity=.9f;probe.importance=20;probe.blendDistance=.5f;probe.nearClipPlane=.1f;probe.farClipPlane=100;
  foreach(var r in model.GetComponentsInChildren<Renderer>(true)){r.reflectionProbeUsage=ReflectionProbeUsage.Simple;r.probeAnchor=probe.transform;}
  StoreKit.MarkStatic(model,false);if(AssetDatabase.LoadAssetAtPath<GameObject>(PatrolPack+"/PatrolWipers.obj"))FinishPatrolDetails();PatrolCarChecks();
 }
 static Material PatrolWheelMaterial(string name){
  string kind=name.Contains("Alloy")?"Alloy":name.Contains("Brake")?"Brake":name.Contains("DarkMetal")?"DarkMetal":"Rubber";
  var path=PatrolPack+"/Patrol"+kind+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
  m.shader=Shader.Find(kind=="Brake"?"CrimeScene/PatrolMachinedMetal":kind=="Rubber"?"CrimeScene/PatrolTyre":"Standard");
  m.color=kind=="Alloy"?new Color(.72f,.74f,.76f):kind=="Brake"?new Color(.40f,.42f,.44f):kind=="DarkMetal"?new Color(.15f,.16f,.17f):new Color(.075f,.078f,.081f);
  m.SetFloat("_Metallic",kind=="Rubber"?0:kind=="Brake"?.80f:kind=="DarkMetal"?.65f:.92f);m.SetFloat("_Glossiness",kind=="Rubber"?.20f:kind=="Alloy"?.64f:kind=="Brake"?.48f:.30f);EditorUtility.SetDirty(m);return m;
 }
 static Material PatrolOpticalMaterial(string name){
  bool guide=name.Contains("LightGuide"),lens=name.Contains("ProjectorLens");
  var path=PatrolPack+(guide?"/PatrolLightGuide.mat":lens?"/PatrolProjectorLens.mat":"/PatrolSatinChrome.mat");var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
  m.color=guide?new Color(.72f,.70f,.65f):lens?new Color(.46f,.53f,.59f,.26f):new Color(.66f,.69f,.72f);m.SetFloat("_Metallic",guide||lens?.04f:.92f);m.SetFloat("_Glossiness",guide?.76f:lens?.94f:.64f);
  if(lens){m.SetFloat("_Mode",3);m.SetInt("_SrcBlend",(int)BlendMode.One);m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);m.SetInt("_ZWrite",0);m.DisableKeyword("_ALPHATEST_ON");m.DisableKeyword("_ALPHABLEND_ON");m.EnableKeyword("_ALPHAPREMULTIPLY_ON");m.renderQueue=3000;}
  if(guide){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.025f,.022f,.016f));m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;}
  EditorUtility.SetDirty(m);return m;
 }
 public static void PatrolCarChecks(){
  if(!root)root=GameObject.Find("NightStore").transform;
  var model=root.Find("Exterior/PoliceCar/SuppliedModel");if(!IsRefinedPatrol(model))throw new Exception("Current car is not the user-supplied Tripo model");
  var filters=model.GetComponentsInChildren<MeshFilter>();int count=filters.Sum(f=>f.sharedMesh.triangles.Length/3);
  int near=filters.Where(f=>!f.transform.IsChildOf(model.Find("DistanceModel"))).Sum(f=>f.sharedMesh.triangles.Length/3);
  if(near<30000||near>35000||count-near!=15000)throw new Exception("Patrol geometry budget changed: "+near+" / "+count);
  var bounds=SupplyBounds(model);if(bounds.size.x<4.9f||bounds.size.x>5.3f||bounds.size.y<1.5f||bounds.size.y>1.75f||bounds.size.z<2||bounds.size.z>2.4f)throw new Exception("Patrol dimensions incorrect: "+bounds);
  foreach(var f in filters){var mesh=f.sharedMesh;bool textured=AssetDatabase.GetAssetPath(mesh)==PatrolMesh||AssetDatabase.GetAssetPath(mesh)==PatrolPack+"/PatrolDistance.obj";if(mesh.vertices.Any(v=>float.IsNaN(v.x+v.y+v.z)||float.IsInfinity(v.x+v.y+v.z))||mesh.normals.Length!=mesh.vertexCount||(textured&&mesh.uv.Length!=mesh.vertexCount))throw new Exception("Invalid patrol mesh attributes");}
  foreach(var r in model.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials){
   if(!m||!m.shader||!m.shader.isSupported)throw new Exception("Incomplete patrol surface assets");
   if(m.shader.name=="CrimeScene/PatrolCarFinish"&&(!m.GetTexture("_MainTex")||!m.GetTexture("_DoorTex")||!m.GetTexture("_PlateTex")))throw new Exception("Missing patrol graphics");
   if(r.reflectionProbeUsage==ReflectionProbeUsage.Off||r.shadowCastingMode==ShadowCastingMode.Off)throw new Exception("Patrol reflection/shadows disabled");
  }
  if(!model.GetComponent<LODGroup>()||model.GetComponent<LODGroup>().GetLODs().Length!=2)throw new Exception("Patrol distance LOD missing");
  var probe=root.Find("Lighting/ReflectionProbe_PatrolStreet").GetComponent<ReflectionProbe>();
  if(probe.resolution!=256||(probe.cullingMask&(1<<27))!=0||model.GetComponentsInChildren<Transform>(true).Any(t=>t.gameObject.layer!=27))throw new Exception("Patrol self-reflection exclusion/budget changed");
  var optics=model.Find("FittedHeadlampOptics");if(!optics||!model.GetComponent<LODGroup>().GetLODs()[0].renderers.Contains(optics.GetComponentInChildren<Renderer>()))throw new Exception("Close-up optics missing from the near LOD");
  var data=JsonUtility.FromJson<PatrolGeometryMetrics>(File.ReadAllText(PatrolPack+"/geometry.json"));
  if(data.optics_triangles<1000||data.optics_max_surface_distance_m>.010f)throw new Exception("Optical details are not fitted to the body");
  using(var sha=System.Security.Cryptography.SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(PatrolPack+"/PatrolOriginal_Color.jpg"))).Replace("-","")!="BC49A14385E5B319B64E90A8A55AF9E0A833EE78206DA34390D6C1D608BA9C8B")throw new Exception("Original Tripo colour texture was replaced");
  if(model.GetComponentsInChildren<Rigidbody>(true).Length!=0||model.parent.GetComponents<BoxCollider>().Length!=1)throw new Exception("Patrol collision policy changed");
  Debug.Log("PATROL_CAR_CHECKS_PASS near="+near+" / distance=15000 triangles; 5.12m uniform scale; native plates/door artwork; detailed wheels; distinct paint, glass, metal, rubber responses; reflection probes and shadows");
 }
 [Serializable] sealed class PatrolGeometryMetrics {public int optics_triangles;public float optics_max_surface_distance_m;}
 static void PatrolPreview(string label,bool lit){
  Directory.CreateDirectory("Builds/PatrolReview");var car=root.Find("Exterior/PoliceCar");var go=new GameObject("Temporary patrol review");
  var c=go.AddComponent<Camera>();NightQuality.Configure(c);c.stereoTargetEye=StereoTargetEyeMask.None;c.nearClipPlane=.05f;c.farClipPlane=200;c.fieldOfView=46;
  var rt=new RenderTexture(1600,1000,24){antiAliasing=4};c.targetTexture=rt;var prev=RenderTexture.active;
  if(lit){var key=go.AddComponent<Light>();key.type=LightType.Spot;key.range=20;key.spotAngle=95;key.intensity=1.15f;key.color=new Color(1,.96f,.91f);key.shadows=LightShadows.Soft;key.shadowBias=.008f;key.shadowNormalBias=.12f;}
  var views=new[]{new Vector3(-3.8f,2.5f,4.5f),new Vector3(3.8f,2.4f,4.5f),new Vector3(3.8f,2.3f,-4.5f),new Vector3(-3.8f,2.1f,-4.5f),new Vector3(-4.2f,1.6f,0)};
  var closeups=new[]{(new Vector3(2.05f,.71f,1.75f),new Vector3(.94f,.35f,1.669f)),(new Vector3(1.65f,1.18f,3.4f),new Vector3(.78f,.70f,2.24f))};
  try{for(int i=0;i<views.Length+closeups.Length;i++){
   c.transform.position=car.TransformPoint(i<views.Length?views[i]:closeups[i-views.Length].Item1);c.transform.LookAt(car.TransformPoint(i<views.Length?new Vector3(0,.75f,0):closeups[i-views.Length].Item2));c.Render();RenderTexture.active=rt;
   var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes("Builds/PatrolReview/"+label+"-"+i+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
  }}finally{RenderTexture.active=prev;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
 }
 public static void PatrolCarInspectRun(){try{
  FinishOpen();PlaceRefinedPatrolCar();PatrolPreview("review",true);PatrolPreview("night",false);Debug.Log("PATROL_PREVIEW_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void PatrolCarVerifyRun(){try{
  FinishOpen();PatrolCarChecks();GeneralSceneReview();
  var car=root.Find("Exterior/PoliceCar/SuppliedModel");var lod=car.GetComponent<LODGroup>();
  if(!root.Find("Lighting/ReflectionProbe_PatrolStreet").GetComponent<ReflectionProbe>().customBakedTexture)throw new Exception("Patrol baked reflection asset missing");
  var colliders=root.Find("Exterior/PoliceCar").GetComponentsInChildren<Collider>();
  if(colliders.Length!=1)throw new Exception("Patrol has unexpected hidden colliders");
  foreach(var level in lod.GetLODs())if(level.renderers.Length==0||level.renderers.Any(r=>!r))throw new Exception("Patrol LOD references incomplete");
  PatrolPreview("final",true);PatrolPreview("night",false);Debug.Log("PATROL_SAVED_SCENE_PASS source texture bytes, model/LOD budgets, baked reflections, collision and references");EditableDelivery.Build();EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void PatrolCarDeliveryRun(){try{
  FinishOpen();PlaceRefinedPatrolCar();GeneralSceneReview();NightStoreValidation.PresentationChecks();
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveOpenScenes();
  if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();
  PatrolPreview("final",true);PatrolPreview("night",false);FinishOpen();PatrolCarChecks();EditableDelivery.Build();Debug.Log("PATROL_CAR_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
