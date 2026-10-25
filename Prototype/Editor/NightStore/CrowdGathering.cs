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
 const string CrowdFolder="Assets/CrimeSceneDemo/ThirdParty/PeopleModels/Animated";
 const int CrowdLayer=24;
 static Transform CrowdBone(GameObject go,string name){string Key(string s)=>s.Replace(" ","").Replace("_","").ToLowerInvariant();return go.GetComponentsInChildren<Transform>().FirstOrDefault(t=>Key(t.name)==Key("Bip01 "+name));}
 static AnimationClip CrowdClip(string role)=>AssetDatabase.LoadAllAssetsAtPath(CrowdFolder+"/"+role+".fbx").OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
 static void PrepareCrowdImports(){
  foreach(string role in new[]{"Walker","Walker_Idle","Talker","Talker_Idle","Listener","Listener_Idle","Phone","Observer"}){
   var path=CrowdFolder+"/"+role+".fbx";var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(!importer)throw new Exception("Animated figure missing: "+role);
   importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;importer.importBlendShapes=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
   importer.animationCompression=ModelImporterAnimationCompression.KeyframeReduction;importer.animationPositionError=.1f;importer.animationRotationError=.15f;importer.animationScaleError=.1f;
   importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
   importer.SaveAndReimport();
  }
  foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{CrowdFolder})){
   var path=AssetDatabase.GUIDToAssetPath(guid);var i=AssetImporter.GetAtPath(path) as TextureImporter;if(!i)continue;
   bool normal=path.Contains("_Normal");i.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;i.sRGBTexture=!normal;i.mipmapEnabled=true;i.alphaIsTransparency=path.Contains("Cutout");i.maxTextureSize=normal?512:1024;
   var android=i.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;android.maxTextureSize=i.maxTextureSize;android.format=TextureImporterFormat.ASTC_6x6;i.SetPlatformTextureSettings(android);i.SaveAndReimport();
  }
 }
 static Material CrowdMaterial(string role,string imported){
  if(imported.StartsWith("Omani"))return OmaniCrowdMaterial(imported);
  bool cutout=imported.ToLowerInvariant().Contains("opacity"),head=imported.ToLowerInvariant().Contains("head");string part=cutout?"Cutout":head?"Head":"Body";
  string prefix=role=="Phone"?"PasserBy":role=="Observer"?"DetectiveFemale":role;
  string path=(role=="Phone"||role=="Observer")?"Assets/CrimeSceneDemo/ThirdParty/PeopleModels/"+prefix+"_"+(cutout?"Hair":part):CrowdFolder+"/"+prefix+"_"+part;
  var color=AssetDatabase.LoadAssetAtPath<Texture2D>(path+"_Color.png")??AssetDatabase.LoadAssetAtPath<Texture2D>(path+"_Color.jpg");if(!color)throw new Exception("Crowd albedo missing: "+path);
  var mat=Mat(Folder,"Crowd_"+prefix+"_"+part,Color.white,cutout?.15f:head?.28f:.19f);
  mat.shader=Shader.Find(cutout?"CrimeScene/CrowdCutout":"Standard");mat.mainTexture=color;mat.DisableKeyword("_EMISSION");if(mat.HasProperty("_EmissionColor"))mat.SetColor("_EmissionColor",Color.black);
  if(!cutout){var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(path+"_Normal.png")??AssetDatabase.LoadAssetAtPath<Texture2D>(path+"_Normal.jpg");mat.SetTexture("_BumpMap",normal);mat.SetFloat("_BumpScale",head?.22f:.25f);if(normal)mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_Metallic",0);}
  EditorUtility.SetDirty(mat);return mat;
 }
 static CrowdBystander CrowdPerson(Transform parent,string role,Vector3 point,float yaw,CrowdBystander.Behaviour behaviour,float phase){
  var actor=Group("Spectator_"+role,parent);actor.localPosition=point;actor.localRotation=Quaternion.Euler(0,yaw,0);
  var pivot=Group("FacingCalibration",actor);var asset=AssetDatabase.LoadAssetAtPath<GameObject>((role=="Talker"||role=="Phone"?OmaniCrowdFolder:CrowdFolder)+"/"+role+".fbx");if(!asset)throw new Exception("Crowd rig missing");
  var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,pivot);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);go.name="CharacterSkin";
  foreach(var a in go.GetComponentsInChildren<Animation>())Object.DestroyImmediate(a);
  var animation=go.AddComponent<Animation>();animation.playAutomatically=false;animation.cullingType=AnimationCullingType.BasedOnRenderers;animation.AddClip(CrowdClip(role),"Motion");
  var idle=CrowdClip(role+"_Idle");if(idle)animation.AddClip(idle,"Idle");else animation.AddClip(CrowdClip(role),"Idle");
  foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()){
   r.sharedMaterials=r.sharedMaterials.Select(m=>CrowdMaterial(role,m.name)).ToArray();r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;r.updateWhenOffscreen=false;r.skinnedMotionVectors=false;r.quality=SkinQuality.Bone4;
  }
  foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=CrowdLayer;
  animation.Play("Motion");animation["Motion"].time=phase%animation["Motion"].length;animation.Sample();animation.Stop();
  var head=CrowdBone(go,"Head");var nose=CrowdBone(go,"MNose");if(!head||!nose)throw new Exception("Crowd head markers missing");var direction=actor.InverseTransformDirection(nose.position-head.position);pivot.localRotation=Quaternion.Euler(0,-Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,0);
  var c=actor.gameObject.AddComponent<CrowdBystander>();c.Role=behaviour;c.CharacterAnimation=animation;c.Head=head;c.Nose=nose;c.Storefront=root.Find("Facade");c.PhaseSeconds=phase;c.PlaybackSpeed=role=="Observer"?.92f:behaviour==CrowdBystander.Behaviour.Conversation?.86f:1;
  var capsule=actor.gameObject.AddComponent<CapsuleCollider>();capsule.center=new Vector3(0,.88f,0);capsule.height=1.72f;capsule.radius=.23f;
  if(behaviour==CrowdBystander.Behaviour.Phone)CrowdPhone(c,go);
  return c;
 }
 static void CrowdPhone(CrowdBystander c,GameObject skin){
  var left=CrowdBone(skin,"L Hand");var right=CrowdBone(skin,"R Hand");bool useLeft=(left.position-c.Head.position).sqrMagnitude<(right.position-c.Head.position).sqrMagnitude;string side=useLeft?"L":"R";var hand=useLeft?left:right;
  var f1=CrowdBone(skin,side+" Finger1");var f2=CrowdBone(skin,side+" Finger2");var f4=CrowdBone(skin,side+" Finger4");
  var along=(f2.position-hand.position).normalized;var across=Vector3.ProjectOnPlane(f1.position-f4.position,along).normalized;var normal=Vector3.Cross(across,along).normalized;
  var center=(hand.position+(f1.position+f2.position+f4.position)/3)*.5f+along*.016f+normal*.012f;
  var rubber=Mat(Folder,"CrowdPhoneBody",new Color(.018f,.021f,.025f),.28f);var glass=Mat(Folder,"CrowdPhoneScreen",new Color(.025f,.032f,.038f),.60f);
  var k=new KitMesh{Lightmapped=false};k.Bar(rubber,Vector3.zero,new Vector3(.067f,.132f,.009f),.004f);k.Box(glass,new Vector3(0,0,-.0048f),new Vector3(.058f,.117f,.001f));
  var phone=k.Emit("PhoneHeldAtEar",hand,Vector3.zero,Quaternion.identity,Folder,"crowd_phone");phone.transform.SetPositionAndRotation(center,Quaternion.LookRotation(normal,along));phone.transform.localScale=Vector3.one/Mathf.Max(.0001f,hand.lossyScale.x);phone.layer=CrowdLayer;
 }
 static void CrowdBarriers(){
  var group=Fresh("CrowdBoundary",root.Find("Exterior"));var mat=Mat(Folder,"CrowdWeatheredBarrierSteel",new Color(.16f,.18f,.18f),.24f);mat.SetFloat("_Metallic",.55f);
  mat.mainTexture=DemoAssetLibrary.Texture("RealismTextures","Surfaces/PaintedMetalChipped_Neutral.jpg");var k=new KitMesh{LightmapScale=.18f};
  foreach(float x in new[]{-3.1f,0,3.1f}){
   const float z=-20.45f;
   foreach(float side in new[]{-1f,1f}){float sx=x+side*1.05f;k.Cylinder(mat,new Vector3(sx,.07f,z),new Vector3(sx,1.02f,z),.022f,8);k.Cylinder(mat,new Vector3(sx,.025f,z-.31f),new Vector3(sx,.025f,z+.31f),.025f,8);}
   k.Cylinder(mat,new Vector3(x-1.05f,1.02f,z),new Vector3(x+1.05f,1.02f,z),.025f,8);k.Cylinder(mat,new Vector3(x-1.05f,.19f,z),new Vector3(x+1.05f,.19f,z),.020f,8);
   for(int i=0;i<11;i++){float px=x-.9f+i*.18f;k.Cylinder(mat,new Vector3(px,.19f,z),new Vector3(px,1.02f,z),.012f,6);}
   var collision=new GameObject("BarrierCollision");collision.transform.SetParent(group,false);collision.transform.localPosition=new Vector3(x,.52f,z);collision.AddComponent<BoxCollider>().size=new Vector3(2.15f,1.04f,.075f);
  }
  k.Emit("WeatheredPedestrianBarriers",group,Vector3.zero,Quaternion.identity,Folder,"crowd_boundary");StoreKit.MarkStatic(group.gameObject,true);
 }
 static void CrowdGathering(){
  var group=Fresh("DistantStreetLife",root.Find("Exterior"));
  var walker=CrowdPerson(group,"Walker",new Vector3(6.3f,0,-21.55f),90,CrowdBystander.Behaviour.Walker,0);walker.WalkOffset=new Vector3(5.0f,0,0);
  var man=CrowdPerson(group,"Talker",new Vector3(-1.1f,0,-21.65f),48,CrowdBystander.Behaviour.Conversation,.3f);
  var woman=CrowdPerson(group,"Listener",new Vector3(.30f,0,-21.70f),-48,CrowdBystander.Behaviour.Conversation,9.3f);man.Partner=woman;woman.Partner=man;
  CrowdPerson(group,"Phone",new Vector3(-4.6f,0,-21.8f),22,CrowdBystander.Behaviour.Phone,5.5f);
  CrowdPerson(group,"Observer",new Vector3(3.8f,0,-21.85f),-15,CrowdBystander.Behaviour.Observer,3.8f);
  var bank=Object.FindAnyObjectByType<DemoSoundBank>();if(bank)walker.FootstepClips=bank.FootstepsHard;
  var contacts=Group("SoftFootContacts",group).gameObject.AddComponent<CrowdGroundContacts>();var actors=group.GetComponentsInChildren<CrowdBystander>();
  contacts.Feet=actors.SelectMany(c=>new[]{CrowdBone(c.gameObject,"L Foot"),CrowdBone(c.gameObject,"R Foot")}).ToArray();contacts.Roots=actors.SelectMany(c=>new[]{c.transform,c.transform}).ToArray();
  contacts.ContactMaterial=Mat(Folder,"CrowdFootContact",Color.white,0);contacts.ContactMaterial.shader=Shader.Find("CrimeScene/CrowdGroundContact");
  CrowdBarriers();
  // Dynamic spectators must not be frozen into the scene's baked reflection maps.
  foreach(var probe in root.GetComponentsInChildren<ReflectionProbe>())probe.cullingMask&=~(1<<CrowdLayer);
 }
 static void CrowdStructureChecks(){
  var group=root.Find("Exterior/DistantStreetLife");var actors=group.GetComponentsInChildren<CrowdBystander>();if(actors.Length!=5)throw new Exception("Five animated spectators required");
  int triangles=0;
  foreach(var c in actors){
   if(!c.CharacterAnimation||!c.CharacterAnimation.GetClip("Motion")||!c.CharacterAnimation.GetClip("Idle")||!c.Head||!c.Nose)throw new Exception("Crowd clip/bone binding missing");
   if(c.transform.position.z< -22.5f||c.transform.position.z> -20.8f||c.GetComponent<Rigidbody>())throw new Exception("Spectator outside pavement or force-driven");
   foreach(var r in c.GetComponentsInChildren<SkinnedMeshRenderer>()){
    triangles+=r.sharedMesh.triangles.Length/3;if(r.bones.Any(b=>!b)||r.bones.Length!=r.sharedMesh.bindposes.Length||r.updateWhenOffscreen)throw new Exception("Skinned actor has invalid binding or offscreen cost");
    if(r.sharedMaterials.Any(m=>!m||!m.shader||!m.shader.isSupported||m.IsKeywordEnabled("_EMISSION")))throw new Exception("Crowd material unavailable or emissive");
   }
  }
  if(triangles>55000||group.GetComponentsInChildren<Light>().Length!=0)throw new Exception("Crowd exceeds geometry/light budget");
  if(actors.Count(c=>c.Role==CrowdBystander.Behaviour.Conversation)!=2||actors.Count(c=>c.Role==CrowdBystander.Behaviour.Walker)!=1||!group.GetComponentInChildren<CrowdGroundContacts>())throw new Exception("Requested crowd behaviours missing");
  Debug.Log("CROWD_STRUCTURE_PASS five different skinned characters, walking/conversation/phone/watch, bounded pavement, no extra lights or rigidbodies, triangles="+triangles);
 }
 public static void CrowdInspectRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;PrepareCrowdImports();CrowdGathering();CrowdStructureChecks();CrowdPoseAudit();CrowdPreview();Debug.Log("CROWD_INSPECTION_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void CrowdPreview(){
  var go=new GameObject("Temporary crowd review camera");var camera=go.AddComponent<Camera>();NightQuality.Configure(camera);camera.stereoTargetEye=StereoTargetEyeMask.None;camera.fieldOfView=51;camera.farClipPlane=220;
  var lighting=go.AddComponent<DemoLighting>();lighting.Flashlight(true);foreach(var r in lighting.FlashlightModel.GetComponentsInChildren<Renderer>())r.enabled=false;var torch=lighting.FlashlightLight;
  var target=new RenderTexture(1440,900,24);camera.targetTexture=target;var previous=RenderTexture.active;
  var views=new[]{(new Vector3(-1,1.65f,-16),new Vector3(-.4f,1,-21.65f)),(new Vector3(-7,1.6f,-17.3f),new Vector3(-4.6f,1,-21.8f)),(new Vector3(7,1.6f,-17),new Vector3(6.3f,.95f,-21.55f)),(new Vector3(1,1.65f,-4.5f),new Vector3(0,1.2f,-21.6f)),(new Vector3(-1,1.65f,-16),new Vector3(-.4f,1,-21.65f)),(new Vector3(7,1.6f,-17),new Vector3(6.3f,.95f,-21.55f))};
  try{foreach(var c in root.Find("Exterior/DistantStreetLife").GetComponentsInChildren<CrowdBystander>()){c.Initialize();c.Tick(.02f);c.CharacterAnimation.Sample();}root.GetComponentInChildren<CrowdGroundContacts>().RefreshContacts();
   for(int i=0;i<views.Length;i++){
    foreach(var actor in root.Find("Exterior/DistantStreetLife").GetComponentsInChildren<CrowdBystander>()){
     string clip=actor.Role==CrowdBystander.Behaviour.Walker?"Motion":actor.Role==CrowdBystander.Behaviour.Conversation?((actor.name.EndsWith("Listener")== (i==4))?"Motion":"Idle"):"Motion";
     var a=actor.CharacterAnimation;a.Stop();a.Play(clip);a[clip].normalizedTime=i==5?.72f:i==4?.55f:.28f;a.Sample();actor.LookTowardsScene(.02f);
    }
    root.GetComponentInChildren<CrowdGroundContacts>().RefreshContacts();
    camera.transform.position=views[i].Item1;camera.transform.LookAt(views[i].Item2);torch.transform.SetPositionAndRotation(camera.transform.position+camera.transform.right*.12f,camera.transform.rotation);camera.Render();RenderTexture.active=target;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes("Builds/crowd-review-"+i+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
   }
  }finally{RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);lighting.ReleaseTorchResources();Object.DestroyImmediate(go);}
 }
 public static void CrowdVisualRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;CrowdStructureChecks();CrowdPreview();EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void CrowdPoseAudit(){
  foreach(var actor in root.Find("Exterior/DistantStreetLife").GetComponentsInChildren<CrowdBystander>()){
   foreach(string clip in new[]{"Motion","Idle"})foreach(float t in new[]{0f,.125f,.25f,.375f,.5f,.625f,.75f,.875f,.999f}){
    actor.CharacterAnimation.Play(clip);actor.CharacterAnimation[clip].normalizedTime=t;actor.CharacterAnimation.Sample();
    foreach(var r in actor.GetComponentsInChildren<SkinnedMeshRenderer>()){
     var mesh=r.sharedMesh;var source=mesh.vertices;var weights=mesh.boneWeights;var matrices=r.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();var vertices=new Vector3[source.Length];
     for(int i=0;i<source.Length;i++){var w=weights[i];vertices[i]=matrices[w.boneIndex0].MultiplyPoint3x4(source[i])*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(source[i])*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(source[i])*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(source[i])*w.weight3;}
     var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var v in vertices)bounds.Encapsulate(v);
     float worst=0;int stretched=0;var triangles=mesh.triangles;float meshScale=r.transform.lossyScale.x;
     for(int i=0;i<triangles.Length;i+=3)for(int e=0;e<3;e++){int a=triangles[i+e],b=triangles[i+(e+1)%3];float original=Vector3.Distance(source[a],source[b])*meshScale,current=Vector3.Distance(vertices[a],vertices[b]);if(original>.002f){float ratio=current/original;worst=Mathf.Max(worst,ratio);if(current>original*4+.025f)stretched++;}}
     Debug.Log("CROWD_POSE "+actor.name+" "+clip+" "+t+" bounds="+bounds+" stretched="+stretched+" ratio="+worst);
     if(bounds.size.y<1.5f||bounds.size.y>2.1f||stretched>10)throw new Exception("Crowd skin deforms outside safe bounds: "+actor.name+" "+clip+" "+t);
    }
   }
  }
  Debug.Log("CROWD_SKIN_PASS all roles and clips at nine phases, weighted skin bounds and stretched-edge checks");
 }
 static void CrowdBehaviourChecks(){
  var actors=root.Find("Exterior/DistantStreetLife").GetComponentsInChildren<CrowdBystander>();var original=actors.First(c=>c.Role==CrowdBystander.Behaviour.Walker);
  foreach(var actor in actors)foreach(string clip in new[]{"Motion","Idle"}){
   var a=actor.CharacterAnimation;var bones=actor.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r=>r.bones).Distinct().ToArray();a.Stop();a.Play(clip);a[clip].normalizedTime=0;a.Sample();var first=bones.Select(b=>b.position).ToArray();a[clip].normalizedTime=.999f;a.Sample();float seam=bones.Select((b,i)=>Vector3.Distance(b.position,first[i])).Max();
   if(seam>.06f)throw new Exception("Crowd loop jumps at wrap: "+actor.name+" "+clip+" "+seam);Debug.Log("CROWD_LOOP_PASS "+actor.name+" "+clip+" maxBoneJump="+seam);
  }
  var clone=Object.Instantiate(original.gameObject,original.transform.parent);clone.name="Temporary walker verification";clone.hideFlags=HideFlags.DontSave;
  try{
   var walker=clone.GetComponent<CrowdBystander>();walker.FootstepClips=Array.Empty<AudioClip>();var home=walker.transform.localPosition;float min=home.x,max=home.x;bool moved=false,paused=false,returned=false;
   walker.Initialize();for(int i=0;i<1100;i++){walker.Tick(.1f);var p=walker.transform.localPosition;min=Mathf.Min(min,p.x);max=Mathf.Max(max,p.x);moved|=walker.IsWalking;paused|=!walker.IsWalking&&p.x>home.x+4.9f;returned|=max>home.x+4.9f&&p.x<home.x+.1f;if(Mathf.Abs(p.z-home.z)>.001f||p.x<home.x-.01f||p.x>home.x+5.01f)throw new Exception("Walker left clear pavement segment");}
   if(!moved||!paused||!returned||max-min<4.9f)throw new Exception("Walker failed travel / watch / return cycle");
   var camera=Camera.main;if(camera){var old=camera.transform.position;try{camera.transform.position=walker.transform.position+Vector3.up*1.6f;var point=walker.transform.position;for(int i=0;i<20;i++)walker.Tick(.1f);if(Vector3.Distance(point,walker.transform.position)>.001f||walker.IsWalking)throw new Exception("Walker did not yield to nearby visitor");}finally{camera.transform.position=old;}}
  }finally{Object.DestroyImmediate(clone);}
  var pair=actors.Where(c=>c.Role==CrowdBystander.Behaviour.Conversation).Select(c=>Object.Instantiate(c.gameObject,c.transform.parent)).ToArray();
  try{bool a=false,b=false;for(int i=0;i<200;i++){var x=pair[0].GetComponent<CrowdBystander>();var y=pair[1].GetComponent<CrowdBystander>();x.Tick(.1f);y.Tick(.1f);if(x.IsSpeaking&&y.IsSpeaking)throw new Exception("Conversation gestures overlap continuously");a|=x.IsSpeaking;b|=y.IsSpeaking;}if(!a||!b)throw new Exception("Conversation does not alternate gestures");}
  finally{foreach(var go in pair)Object.DestroyImmediate(go);}
  Debug.Log("CROWD_BEHAVIOUR_PASS walking/watch/return, bounded route, visitor yielding, alternating conversation, no physics forces");
 }
 public static void CrowdRefinementRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");string car=CarGeometrySignature();
  PrepareCrowdImports();CrowdGathering();CrowdStructureChecks();CrowdPoseAudit();CrowdBehaviourChecks();
  // Reset the sampled authoring poses before serializing the editable scene.
  CrowdGathering();Physics.SyncTransforms();if(car!=CarGeometrySignature())throw new Exception("Protected car changed");InvestigationChecks();NightStoreValidation.PresentationChecks();
  EditorSceneManager.MarkSceneDirty(root.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
  if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();CrowdPreview();Debug.Log("CROWD_REFINEMENT_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void CrowdDeliveryRun(){try{
  EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");root=GameObject.Find("NightStore").transform;oman=root.Find("Exterior/OmanStreet");InvestigationChecks();CrowdBehaviourChecks();NightStoreValidation.PresentationChecks();EditorSceneManager.OpenScene(DemoValidation.ScenesFolder+"/JewelryStoreNight.unity");EditableDelivery.Build();Debug.Log("CROWD_DELIVERY_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
// The general static-model importer disables animations for static packs.
// This narrow later pass enables only the new animated derivatives; no changes
// to Claude's shared importer or to any existing asset pack are required.
public sealed class CrowdAnimationImport : AssetPostprocessor {
 public override int GetPostprocessOrder()=>10000;
 void OnPreprocessModel(){
  if(!assetPath.StartsWith("Assets/CrimeSceneDemo/ThirdParty/PeopleModels/Animated/",StringComparison.Ordinal)&&!assetPath.StartsWith("Assets/CrimeSceneDemo/ThirdParty/PeopleModels/OmaniCrowd/",StringComparison.Ordinal))return;
  var importer=(ModelImporter)assetImporter;importer.importAnimation=true;importer.animationType=ModelImporterAnimationType.Legacy;importer.importBlendShapes=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.isReadable=true;
 }
}
#endif
