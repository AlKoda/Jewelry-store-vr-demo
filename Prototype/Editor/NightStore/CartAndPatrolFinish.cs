#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
using static DemoGeometry;

public static partial class PresentationPolish {
 static void FinishRollingCart(){
  var cart=root.GetComponentsInChildren<PortableFieldKit>().Single(k=>k.Cart);cart.RollOnGround=true;
  var old=cart.transform.Find("CartFrameAndCasters");if(old)old.gameObject.SetActive(false);
  var group=Fresh("RollingFrameAndCasters",cart.transform);var steel=Mat(Folder,"FieldCartSatinSteel",new Color(.22f,.25f,.26f),.24f);steel.SetFloat("_Metallic",.6f);
  var rubber=Mat(Folder,"RollingCasterRubber",new Color(.045f,.05f,.052f),.16f);var hub=Mat(Folder,"RollingCasterHub",new Color(.29f,.31f,.32f),.35f);hub.SetFloat("_Metallic",.65f);
  var frame=new KitMesh{Lightmapped=false};frame.Bar(steel,V(0,.875f,0),V(1,.03f,.54f),.008f);frame.Bar(steel,V(0,.52f,0),V(.88f,.02f,.42f),.004f);
  foreach(float x in new[]{-.43f,.43f})foreach(float z in new[]{-.21f,.21f})frame.Cylinder(steel,V(x,.105f,z),V(x,.86f,z),.014f,12);
  frame.Emit("SupportedCartFrame",group,Vector3.zero,Quaternion.identity,Folder,"rolling_cart_frame");
  var swivels=new System.Collections.Generic.List<Transform>();var spins=new System.Collections.Generic.List<Transform>();int index=0;
  foreach(float x in new[]{-.43f,.43f})foreach(float z in new[]{-.21f,.21f}){
   var swivel=Group("CasterSwivel_"+index,group);swivel.localPosition=V(x,.042f,z);var fork=new KitMesh{Lightmapped=false};
   foreach(float side in new[]{-1f,1f})fork.Bar(steel,V(side*.022f,.028f,0),V(.006f,.060f,.026f),.003f);
   fork.Cylinder(steel,V(0,.047f,0),V(0,.069f,0),.014f,12);fork.Emit("Fork",swivel,Vector3.zero,Quaternion.identity,Folder,"rolling_cart_fork_"+index);
   var spin=Group("WheelSpin",swivel);var wheel=new KitMesh{Lightmapped=false};wheel.Cylinder(rubber,V(-.017f,0,0),V(.017f,0,0),.04f,24);
   foreach(float side in new[]{-1f,1f}){
    wheel.Cylinder(hub,V(side*.018f,0,0),V(side*.019f,0,0),.023f,20);
    for(int spoke=0;spoke<4;spoke++){float a=spoke*Mathf.PI/2;wheel.Cylinder(rubber,V(side*.0195f,Mathf.Cos(a)*.008f,Mathf.Sin(a)*.008f),V(side*.0195f,Mathf.Cos(a)*.019f,Mathf.Sin(a)*.019f),.002f,6);}
   }wheel.Emit("RubberWheelWithHub",spin,Vector3.zero,Quaternion.identity,Folder,"rolling_cart_wheel_"+index);swivels.Add(swivel);spins.Add(spin);index++;
  }
  cart.WheelSwivels=swivels.ToArray();cart.WheelSpins=spins.ToArray();foreach(var t in group.GetComponentsInChildren<Transform>()){GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);t.gameObject.layer=cart.gameObject.layer;}
 }
 static void FinishPatrolDetails(){
  var model=root.Find("Exterior/PoliceCar/SuppliedModel");var prior=model.Find("ParkedWindshieldWipers");if(prior)Object.DestroyImmediate(prior.gameObject);
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>(PatrolPack+"/PatrolWipers.obj");if(!asset)throw new Exception("Native fitted wipers missing");
  var go=Object.Instantiate(asset,model);go.name="ParkedWindshieldWipers";go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;
  var mat=Mat(PatrolPack,"PatrolWiperRubber",new Color(.025f,.028f,.03f),.27f);
  foreach(var r in go.GetComponentsInChildren<MeshRenderer>()){r.sharedMaterials=r.sharedMaterials.Select(m=>mat).ToArray();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.BlendProbes;r.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Simple;r.probeAnchor=root.Find("Lighting/ReflectionProbe_PatrolStreet");}
  foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=27;
  var lod=model.GetComponent<LODGroup>();var levels=lod.GetLODs();levels[0].renderers=levels[0].renderers.Where(r=>r).Concat(go.GetComponentsInChildren<Renderer>()).ToArray();lod.SetLODs(levels);lod.RecalculateBounds();
  PatrolWheelMaterial("Rubber");
 }
 static void RollingCartChecks(){
  var stage=new GameObject("Temporary rolling-cart regression");stage.hideFlags=HideFlags.DontSave;PortableFieldKit cart=null;HandInteractor hand=null;
  try{
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(stage.transform);floor.transform.position=V(0,99.95f,0);floor.transform.localScale=V(12,.1f,12);
   var g=new GameObject("Cart test");g.transform.SetParent(stage.transform);g.transform.position=V(0,100,0);cart=g.AddComponent<PortableFieldKit>();cart.Cart=true;cart.VolumeCentre=V(0,.5f,0);cart.VolumeHalf=V(.5f,.44f,.27f);cart.Grip=Group("Handle",g.transform);cart.Grip.localPosition=V(0,.9f,-.31f);
   var owner=new GameObject("Test hand");owner.transform.SetParent(stage.transform);owner.transform.position=cart.Grip.position;hand=owner.AddComponent<HandInteractor>();
   Physics.SyncTransforms();var start=cart.transform.position;if(!hand.TakeKit(cart)||Vector3.Distance(start,cart.transform.position)>.00001f)throw new Exception("Cart jumps when its handle is grabbed");
   if(PortableFieldKit.LimitWalkingSpeed(owner.transform,1.7f)>cart.RollSpeed*.81f)throw new Exception("Walking outruns the held trolley");
   var handStart=owner.transform.position;float maxStep=0;for(int i=0;i<100;i++){owner.transform.position=handStart+V(0,0,Mathf.Min(i*.012f,1));var before=cart.transform.position;cart.RollStep(owner.transform,.02f);maxStep=Mathf.Max(maxStep,Vector3.Distance(before,cart.transform.position));Physics.SyncTransforms();}
   if(cart.transform.position.z<.7f||maxStep>.018f||Mathf.Abs(cart.transform.position.y-100.002f)>.003f)throw new Exception("Cart does not roll smoothly on the ground: "+cart.transform.position+" step="+maxStep);
   owner.transform.position+=Vector3.up*.25f;for(int i=0;i<20;i++)cart.RollStep(owner.transform,.02f);if(Mathf.Abs(cart.transform.position.y-100.002f)>.003f)throw new Exception("Hand lifts cart off ground");
   hand.PutDownKit();for(int i=0;i<100;i++)cart.RollStep(null,.02f);if(cart.RollingVelocity.magnitude>.001f||PortableFieldKit.LimitWalkingSpeed(owner.transform,1.7f)!=1.7f)throw new Exception("Released trolley does not brake or restore walking speed");
   cart.ResetToHome();var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(stage.transform);wall.transform.position=V(0,100.5f,.9f);wall.transform.localScale=V(3,1,.08f);Physics.SyncTransforms();owner.transform.position=cart.Grip.position;hand.TakeKit(cart);handStart=owner.transform.position;
   for(int i=0;i<160;i++){owner.transform.position=handStart+V(0,0,Mathf.Min(i*.01f,.9f));cart.RollStep(owner.transform,.02f);Physics.SyncTransforms();}
   if(cart.transform.position.z>.60f)throw new Exception("Trolley passes through obstruction");
   hand.PutDownKit();cart.ResetToHome();owner.transform.position=cart.Grip.position;hand.TakeKit(cart);owner.transform.position+=Vector3.forward*3;cart.RollStep(owner.transform,.02f);if(cart.Holder||Vector3.Distance(cart.transform.position,start)>.01f)throw new Exception("Tracking jump drags/teleports trolley");
   cart.RollOnGround=false;owner.transform.position=cart.Grip.position;hand.TakeKit(cart);owner.transform.position+=Vector3.forward*.3f;cart.Follow(owner.transform);if(Vector3.Distance(cart.transform.position,start)<.2f)throw new Exception("Original hand-follow fallback unavailable");
   foreach(bool left in new[]{false,true}){var pose=EvidenceCamera.ControllerGrip(left);float cameraRelativeX=(Quaternion.Inverse(pose.rotation)*-pose.position).x;if((left?-cameraRelativeX:cameraRelativeX)<.07f||Quaternion.Angle(pose.rotation,EvidenceCamera.HandHold)>.001f)throw new Exception("Camera side grip incorrect");}
   Debug.Log("CART_AND_CAMERA_PASS no grab snap, bounded rolling, ground lock, braking, obstacle sweep, tracking-jump release, original fallback and left/right camera grips");
  }finally{if(hand)hand.ReleaseAll();Object.DestroyImmediate(stage);Physics.SyncTransforms();}
 }
 public static void CartPatrolFinishRun(){try{
  FinishOpen();FinishRollingCart();FinishPatrolDetails();RollingCartChecks();PatrolCarChecks();GeneralSceneReview();
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveOpenScenes();
  if(!NightLighting.Bake(out string report))throw new Exception(report);Debug.Log(report);ReflectionCapture.Refresh();EditorSceneManager.SaveOpenScenes();
  NightStoreValidation.CameraGripRender();PatrolPreview("polished",true);PortablePreview();FinishOpen();FieldworkChecks();FinishOpen();NightStoreValidation.PresentationChecks();EditableDelivery.Build();Debug.Log("CART_PATROL_FINISH_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void CartPatrolVerifyRun(){try{FinishOpen();RollingCartChecks();FieldworkChecks();FinishOpen();PatrolCarChecks();NightStoreValidation.PresentationChecks();EditableDelivery.Build();Debug.Log("CART_PATROL_FINAL_VERIFY_PASS");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
#endif
