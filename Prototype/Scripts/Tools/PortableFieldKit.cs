using UnityEngine;

// One upright carry case, shared between desktop and tracked hands. Contents use
// local dock transforms, so a released tool returns to the case wherever it is.
// A case has no dynamic rigidbody and can never propel its carrier.
public sealed class PortableFieldKit : MonoBehaviour {
 public Transform Grip;
 public Collider GripCollider;
 public InteriorBounds Bounds;
 public Vector2 Footprint=new Vector2(.88f,.48f);
 public ToolHolder Holder {get;private set;}
 Vector3 home;Quaternion homeRotation;bool recorded;DemoLighting lighting;
 public void RecordHome(){if(recorded)return;recorded=true;home=transform.position;homeRotation=transform.rotation;}
 void Awake(){RecordHome();}
 public bool Take(ToolHolder holder,Transform anchor){
  if(Holder||!holder||!anchor)return false;RecordHome();Holder=holder;lighting=FindFirstObjectByType<DemoLighting>();Follow(anchor);return true;
 }
 public void Follow(Transform anchor){
  if(!anchor)return;float heading=ToolHolder.Heading(anchor);var rotation=Quaternion.Euler(0,heading,0);
  var point=Holder is DesktopInteractor?anchor.TransformPoint(new Vector3(0,-.36f,.72f)):anchor.TransformPoint(new Vector3(0,-.045f,.005f))-rotation*Grip.localPosition;
  transform.SetPositionAndRotation(point,rotation);
 }
 void LateUpdate(){
  if(!Holder)return;
  if(Holder is HandInteractor&&lighting&&((lighting.TorchEquipped(false)&&lighting.FlashlightLight.transform.parent==Holder.transform)||(lighting.TorchEquipped(true)&&lighting.UVTorchLight.transform.parent==Holder.transform))){Holder.PutDownKit();return;}
  Follow(Holder is HandInteractor?Holder.transform:((DesktopInteractor)Holder).View.transform);
 }
 bool Support(Ray ray,float reach,out RaycastHit hit)=>DemoPhysics.Nearest(ray,reach,out hit,transform,Holder?Holder.transform:null,true);
 // A stable rest: the centre and the four points at 60% of the footprint are supported within
 // 4 cm of the chosen point, so a display-case top narrower than the case (it may overhang 20%
 // a side, like a real case on a counter) and a table both qualify; a wall or a prop in the
 // case's own volume refuses it.
 public float RestTolerance=.04f, RestFootprint=.30f;
 public bool CanRestAt(Vector3 point,Quaternion rotation){
  if(Bounds&&Vector3.Distance(Bounds.Clamp(point),point)>.015f)return false;
  foreach(float x in new[]{0f,-RestFootprint,RestFootprint})foreach(float z in new[]{0f,-RestFootprint,RestFootprint}){
   if(x!=0&&z==0||x==0&&z!=0)continue; // centre and four corners
   var corner=point+rotation*new Vector3(Footprint.x*x,.09f,Footprint.y*z);
   if(!Support(new Ray(corner,Vector3.down),.09f+RestTolerance+.15f,out var ground)||Mathf.Abs(ground.point.y-point.y)>RestTolerance)return false;
  }
  // A table/floor below the bottom is fine; walls and objects above it are not.
  foreach(var c in Physics.OverlapBox(point+rotation*new Vector3(0,.31f,.065f),new Vector3(.46f,.285f,.325f),rotation,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){
   if(c.transform.IsChildOf(transform)||c.GetComponentInParent<CharacterController>()||(Holder&&c.transform.IsChildOf(Holder.transform)))continue;
   return false;
  }return true;
 }
 public bool PlaceByRay(Ray ray,float reach){
  var rotation=Quaternion.Euler(0,transform.eulerAngles.y,0);
  if(!DemoPhysics.Nearest(ray,reach,out var hit,transform,Holder?Holder.transform:null)||hit.normal.y<.7f||!CanRestAt(hit.point,rotation)){DemoFeedback.Post("Choose a clear floor or table for the kit",FeedbackTone.Warning);return false;}
  transform.SetPositionAndRotation(hit.point+Vector3.up*.002f,rotation);return true;
 }
 public bool PutDown(){
  var rotation=Quaternion.Euler(0,transform.eulerAngles.y,0);var center=Bounds?Bounds.Clamp(transform.position):transform.position;float floor=Bounds?Bounds.FloorAt(center):0;
  foreach(var offset in new[]{Vector3.zero,Vector3.left*.3f,Vector3.right*.3f,Vector3.forward*.3f,Vector3.back*.3f,Vector3.left*.65f,Vector3.right*.65f,Vector3.forward*.65f,Vector3.back*.65f}){
   var p=center+offset;p.y=Mathf.Clamp(center.y+.5f,floor+.15f,floor+2.65f);
   if(Support(new Ray(p,Vector3.down),3,out var hit)&&CanRestAt(hit.point,rotation)){transform.SetPositionAndRotation(hit.point+Vector3.up*.002f,rotation);return true;}
  }return false;
 }
 public void Release(bool restoreHome=false,bool alreadyPlaced=false,bool audible=true){
  if(!restoreHome&&!alreadyPlaced&&!PutDown()){restoreHome=true;DemoFeedback.Post("No clear support nearby; kit returned to the field table",FeedbackTone.Warning);}
  Holder=null;if(restoreHome){RecordHome();transform.SetPositionAndRotation(home,homeRotation);}if(audible)DemoSounds.Play(DemoSound.Thud,transform.position,.15f);
 }
 // A reset teleports the case home silently (ReleaseAll may already have set it down).
 public void ResetToHome(){if(Holder)Holder.PutDownKit(true,false);else Release(true,false,false);}
}
