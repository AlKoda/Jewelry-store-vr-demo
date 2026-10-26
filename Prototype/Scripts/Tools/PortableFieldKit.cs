using UnityEngine;

// One upright carry case, shared between desktop and tracked hands. Contents use
// local dock transforms, so a released tool returns to the case wherever it is.
// A case has no dynamic rigidbody and can never propel its carrier.
public sealed class PortableFieldKit : MonoBehaviour {
 public Transform Grip;
 public Collider GripCollider;
 public InteriorBounds Bounds;
 public Vector2 Footprint=new Vector2(.88f,.48f);
 // The clear volume above the base that a rest position must offer (local centre and half size).
 public Vector3 VolumeCentre=new Vector3(0,.31f,.065f), VolumeHalf=new Vector3(.46f,.285f,.325f);
 // A cart (the exterior field table, October 24) is pulled by its push bar and carries whatever
 // other kit rests on its top along with it, so the case rides on the tray.
 public bool Cart;
 [Tooltip("Roll on the ground when the handle is held. Disable to restore the previous hand-follow movement.")]
 public bool RollOnGround=true;
 public float RollSpeed=.85f,RollAcceleration=1.6f,RollBrake=2.2f,HandleResponse=5f,RollTurnSpeed=70f;
 public Transform[] WheelSwivels=new Transform[0],WheelSpins=new Transform[0];
 public float WheelRadius=.04f;
 Vector3 rollVelocity,grabPoint,lastAnchor;float yawVelocity;bool rollGrab;
 static PortableFieldKit rollingCart;
 public static float LimitWalkingSpeed(Transform mover,float requested){
  if(!rollingCart||!rollingCart.RollOnGround||!rollingCart.Holder||!rollingCart.Holder.transform.IsChildOf(mover))return requested;
  return Mathf.Min(requested,rollingCart.RollSpeed*.8f);
 }
 public Vector3 RollingVelocity=>rollVelocity;
 public float PassengerTop=1.2f;
 public ToolHolder Holder {get;private set;}
 Vector3 home;Quaternion homeRotation;bool recorded;DemoLighting lighting;
 public void RecordHome(){if(recorded)return;recorded=true;home=transform.position;homeRotation=transform.rotation;}
 void Awake(){RecordHome();}
 public bool Take(ToolHolder holder,Transform anchor){
  if(Holder||!holder||!anchor)return false;RecordHome();Holder=holder;lighting=FindFirstObjectByType<DemoLighting>();
  if(Cart&&RollOnGround){rollingCart=this;grabPoint=transform.InverseTransformPoint(anchor.position);lastAnchor=anchor.position;rollVelocity=Vector3.zero;yawVelocity=0;rollGrab=true;}
  else Follow(anchor);return true;
 }
 public void Follow(Transform anchor){
  if(!anchor)return;
  if(Cart&&RollOnGround){RollStep(anchor,Time.deltaTime);return;}
  float heading=ToolHolder.Heading(anchor);var rotation=Quaternion.Euler(0,heading,0);
  var point=Holder is DesktopInteractor?anchor.TransformPoint(new Vector3(0,-.36f,Cart?1.0f:.72f)):anchor.TransformPoint(new Vector3(0,-.045f,.005f))-rotation*Grip.localPosition;
  MoveTo(point,rotation);
 }
 // Moves the kit and, for a cart, every other kit resting on its top (same local pose kept).
 public void MoveTo(Vector3 position,Quaternion rotation){
  if(!Cart){transform.SetPositionAndRotation(position,rotation);return;}
  var riders=Passengers();var local=new Vector3[riders.Length];var turn=new Quaternion[riders.Length];
  for(int i=0;i<riders.Length;i++){local[i]=transform.InverseTransformPoint(riders[i].transform.position);turn[i]=Quaternion.Inverse(transform.rotation)*riders[i].transform.rotation;}
  transform.SetPositionAndRotation(position,rotation);
  for(int i=0;i<riders.Length;i++)riders[i].transform.SetPositionAndRotation(transform.TransformPoint(local[i]),transform.rotation*turn[i]);
 }
 public PortableFieldKit[] Passengers(){
  if(!Cart)return new PortableFieldKit[0];var list=new System.Collections.Generic.List<PortableFieldKit>();
  foreach(var other in FindObjectsByType<PortableFieldKit>(FindObjectsSortMode.None)){
   if(other==this||other.Holder||other.Cart)continue;var l=transform.InverseTransformPoint(other.transform.position);
   if(Mathf.Abs(l.x)<=Footprint.x*.5f&&Mathf.Abs(l.z)<=Footprint.y*.5f&&l.y>0&&l.y<PassengerTop)list.Add(other);
  }return list.ToArray();
 }
 void LateUpdate(){
  if(!Holder){if(Cart&&RollOnGround)RollStep(null,Time.deltaTime);return;}
  if(Holder is HandInteractor&&lighting&&((lighting.TorchEquipped(false)&&lighting.FlashlightLight.transform.parent==Holder.transform)||(lighting.TorchEquipped(true)&&lighting.UVTorchLight.transform.parent==Holder.transform))){Holder.PutDownKit();return;}
  Follow(Holder is HandInteractor?Holder.transform:((DesktopInteractor)Holder).View.transform);
 }
 // Grounded, acceleration-limited handle forces. No dynamic rigidbody can push
 // the visitor, and neither taking the handle nor changing hand height lifts or
 // teleports the trolley. MoveTo remains the explicit authoring/reset operation.
 public void RollStep(Transform anchor,float deltaTime){
  if(!Cart||!RollOnGround)return;float dt=Mathf.Clamp(deltaTime,0,.05f);if(dt<=0)return;
  Vector3 desired=Vector3.zero;float desiredYaw=0;
  if(anchor&&Holder&&rollGrab){
   if(Vector3.Distance(anchor.position,lastAnchor)>.65f){rollVelocity=Vector3.zero;yawVelocity=0;Holder.PutDownKit();return;}
   lastAnchor=anchor.position;
   var error=Vector3.ProjectOnPlane(anchor.position-transform.TransformPoint(grabPoint),Vector3.up);
   if(error.magnitude>.75f){rollVelocity=Vector3.zero;yawVelocity=0;Holder.PutDownKit();return;}
   if(error.magnitude>.012f)desired=Vector3.ClampMagnitude(error*HandleResponse,RollSpeed);
   var lever=Vector3.ProjectOnPlane(transform.TransformPoint(grabPoint)-transform.position,Vector3.up);
   desiredYaw=Mathf.Clamp(Vector3.Cross(lever,error).y*230,-RollTurnSpeed,RollTurnSpeed);
  }
  rollVelocity=Vector3.MoveTowards(rollVelocity,desired,(anchor?RollAcceleration:RollBrake)*dt);
  yawVelocity=Mathf.MoveTowards(yawVelocity,desiredYaw,160*dt);
  var delta=rollVelocity*dt;var rotation=Quaternion.Euler(0,transform.eulerAngles.y+yawVelocity*dt,0);
  if(delta.sqrMagnitude<.00000001f&&Mathf.Abs(yawVelocity)<.01f)return;
  var riders=Passengers();bool Ignore(Collider c){
   if(c.transform.IsChildOf(transform)||c.GetComponentInParent<CharacterController>()||(Holder&&c.transform.IsChildOf(Holder.transform)))return true;
   foreach(var rider in riders)if(c.transform.IsChildOf(rider.transform))return true;return false;
  }
  float distance=delta.magnitude;
  if(distance>.00001f){
   foreach(var hit in Physics.BoxCastAll(transform.position+transform.rotation*VolumeCentre,VolumeHalf,delta/distance,transform.rotation,distance+.012f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){
    if(Ignore(hit.collider))continue;float allowed=Mathf.Max(0,hit.distance-.012f);if(allowed<distance){distance=allowed;rollVelocity=Vector3.zero;}
   }
   delta=delta.normalized*distance;
  }
  var position=transform.position+delta;
  if(Bounds&&Vector3.Distance(Bounds.Clamp(position),position)>.001f){rollVelocity=Vector3.zero;yawVelocity=0;return;}
  float floor=float.NegativeInfinity;
  foreach(float x in new[]{-.40f,.40f})foreach(float z in new[]{-.18f,.18f}){
   var origin=position+rotation*new Vector3(x,.22f,z);
   if(!Support(new Ray(origin,Vector3.down),.50f,out var hit)||hit.point.y-transform.position.y>.045f){rollVelocity=Vector3.zero;yawVelocity=0;return;}
   floor=Mathf.Max(floor,hit.point.y);
  }
  position.y=floor+.002f;
  foreach(var c in Physics.OverlapBox(position+rotation*VolumeCentre,VolumeHalf,rotation,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
   if(!Ignore(c)){rollVelocity=Vector3.zero;yawVelocity=0;return;}
  var before=transform.position;var beforeRotation=transform.rotation;MoveTo(position,rotation);
  for(int i=0;i<WheelSwivels.Length;i++){
   var swivel=WheelSwivels[i];if(!swivel)continue;
   var local=swivel.localPosition;var movement=position+rotation*local-(before+beforeRotation*local);movement.y=0;
   if(movement.sqrMagnitude<.0000001f)continue;
   float angle=Mathf.Atan2(transform.InverseTransformDirection(movement).x,transform.InverseTransformDirection(movement).z)*Mathf.Rad2Deg;
   swivel.localRotation=Quaternion.RotateTowards(swivel.localRotation,Quaternion.Euler(0,angle,0),240*dt);
   if(i<WheelSpins.Length&&WheelSpins[i])WheelSpins[i].Rotate(Vector3.right,movement.magnitude/Mathf.Max(.01f,WheelRadius)*Mathf.Rad2Deg,Space.Self);
  }
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
  var riders=Passengers();
  foreach(var c in Physics.OverlapBox(point+rotation*VolumeCentre,VolumeHalf,rotation,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){
   if(c.transform.IsChildOf(transform)||c.GetComponentInParent<CharacterController>()||(Holder&&c.transform.IsChildOf(Holder.transform)))continue;
   bool rider=false;foreach(var r in riders)if(c.transform.IsChildOf(r.transform))rider=true;if(rider)continue;
   return false;
  }return true;
 }
 public bool PlaceByRay(Ray ray,float reach){
  if(Cart&&RollOnGround){rollVelocity=Vector3.zero;yawVelocity=0;return true;}
  var rotation=Quaternion.Euler(0,transform.eulerAngles.y,0);
  if(!DemoPhysics.Nearest(ray,reach,out var hit,transform,Holder?Holder.transform:null)||hit.normal.y<.7f||!CanRestAt(hit.point,rotation)){DemoFeedback.Post("Choose a clear floor or table for the kit",FeedbackTone.Warning);return false;}
  MoveTo(hit.point+Vector3.up*.002f,rotation);return true;
 }
 public bool PutDown(){
  if(Cart&&RollOnGround)return true;
  var rotation=Quaternion.Euler(0,transform.eulerAngles.y,0);var center=Bounds?Bounds.Clamp(transform.position):transform.position;float floor=Bounds?Bounds.FloorAt(center):0;
  foreach(var offset in new[]{Vector3.zero,Vector3.left*.3f,Vector3.right*.3f,Vector3.forward*.3f,Vector3.back*.3f,Vector3.left*.65f,Vector3.right*.65f,Vector3.forward*.65f,Vector3.back*.65f}){
   var p=center+offset;p.y=Mathf.Clamp(center.y+.5f,floor+.15f,floor+2.65f);
   if(Support(new Ray(p,Vector3.down),3,out var hit)&&CanRestAt(hit.point,rotation)){MoveTo(hit.point+Vector3.up*.002f,rotation);return true;}
  }return false;
 }
 public void Release(bool restoreHome=false,bool alreadyPlaced=false,bool audible=true){
  if(!restoreHome&&!alreadyPlaced&&!PutDown()){restoreHome=true;DemoFeedback.Post(Cart?"No clear ground for the cart here; it rolled back to its place":"No clear support nearby; kit returned to the field table",FeedbackTone.Warning);}
  Holder=null;rollGrab=false;if(rollingCart==this)rollingCart=null;if(restoreHome){RecordHome();rollVelocity=Vector3.zero;yawVelocity=0;MoveTo(home,homeRotation);}if(audible)DemoSounds.Play(DemoSound.Thud,transform.position,.15f);
 }
 // A reset teleports the case home silently (ReleaseAll may already have set it down).
 public void ResetToHome(){if(Holder)Holder.PutDownKit(true,false);else Release(true,false,false);}
}
