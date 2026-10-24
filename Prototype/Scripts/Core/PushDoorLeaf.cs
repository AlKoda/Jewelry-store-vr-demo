using UnityEngine;
// A hinged door leaf (SceneReviewPass.Doors): dynamic rigidbody, hinge with limits and a soft
// closing spring. Body and controller contact (DoorBodyPush, VRDoorBodyPush, VRDoorHandPush,
// XRLocomotion) call Push every frame they touch it. The push does not fire an impulse: it eases
// the leaf's swing speed towards OpenSpeed in the pushed direction (Acceleration per second), so
// a brush of the hand opens the door gently and a walk through it never flings it. The hinge's
// damped spring and the body's angular damping bring it back once the contact ends.
// A hand can also take hold of the leaf (HandInteractor.Grab with the hand on the door, Koda,
// October 24): while held, the leaf turns so the grabbed point follows the hand, and contact
// pushes are ignored until the grip opens.
[RequireComponent(typeof(Rigidbody),typeof(HingeJoint))]
public sealed class PushDoorLeaf : MonoBehaviour {
 [Tooltip("Legacy impulse strength; kept for saved scenes, unused by the eased swing.")] public float PushImpulse=2.4f;
 public float OpenSpeed=1.6f;      // rad/s, about 90 degrees a second at full push
 public float Acceleration=4f;     // rad/s^2 towards OpenSpeed while pushed
 public float StartFraction=.7f;   // a first touch starts the leaf at this share of OpenSpeed, then it eases up
 public float HoldSpeed=3f;        // rad/s cap while a hand holds the leaf
 public float HoldGain=12f;        // rad/s per radian of error between the leaf and the hand
 Rigidbody body;HingeJoint hinge;float lastPush=-1;Quaternion closed;bool held;float grabAngle;
 public float OpeningDegrees=>Mathf.Abs(Mathf.DeltaAngle(0,transform.localEulerAngles.y));
 public float SwingSpeed=>body?Mathf.Abs(body.angularVelocity.y):0;
 public bool Held=>held;
 void Awake(){body=GetComponent<Rigidbody>();hinge=GetComponent<HingeJoint>();closed=transform.localRotation;}
 public void Push(Vector3 point,Vector3 direction)=>Swing(point,direction,false);
 // A deliberate shove (desktop click or G on the leaf): the leaf goes straight to full swing speed.
 public void Shove(Vector3 point,Vector3 direction)=>Swing(point,direction,true);
 void Swing(Vector3 point,Vector3 direction,bool full){
  if(held)return;
  direction.y=0;if(direction.sqrMagnitude<.01f)return;
  Vector3 axis=transform.TransformPoint(hinge?hinge.anchor:Vector3.zero);
  float torque=Vector3.Cross(point-axis,direction.normalized).y; // which way this contact turns the leaf
  if(Mathf.Abs(torque)<.0005f)return;
  float dt=lastPush<0?Time.deltaTime:Mathf.Clamp(Time.time-lastPush,0,.1f);lastPush=Time.time;
  body.WakeUp();
  Vector3 w=body.angularVelocity;
  float sign=Mathf.Sign(torque),start=sign*OpenSpeed*StartFraction;
  if(full)w.y=sign*OpenSpeed;
  // Turning the other way, or barely moving: the contact takes the leaf to the gentle starting
  // speed at once (a nudge opens it a few degrees); held contact then eases it up to OpenSpeed.
  else if(Mathf.Sign(w.y)!=sign||Mathf.Abs(w.y)<Mathf.Abs(start))w.y=start;
  else w.y=Mathf.MoveTowards(w.y,sign*OpenSpeed,Acceleration*dt);
  body.angularVelocity=w;
 }
 // ---- holding: the angle of the hand around the hinge, measured in the frame the leaf swings in
 float LeafAngle()=>Mathf.DeltaAngle(0,transform.localEulerAngles.y);
 float AngleAround(Vector3 point){Vector3 axis=transform.TransformPoint(hinge?hinge.anchor:Vector3.zero);Vector3 d=transform.parent?transform.parent.InverseTransformDirection(point-axis):point-axis;d.y=0;return d.sqrMagnitude<1e-6f?LeafAngle()+grabAngle:Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;}
 public void BeginHold(Vector3 hand){held=true;grabAngle=Mathf.DeltaAngle(LeafAngle(),AngleAround(hand));body.WakeUp();}
 public void Hold(Vector3 hand){
  if(!held)return;float error=Mathf.DeltaAngle(LeafAngle(),AngleAround(hand)-grabAngle)*Mathf.Deg2Rad;
  Vector3 w=body.angularVelocity;w.y=Mathf.Clamp(error*HoldGain,-HoldSpeed,HoldSpeed);body.angularVelocity=w;body.WakeUp();
 }
 public void EndHold(){held=false;lastPush=-1;}
 public void Close(){held=false;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.rotation=transform.parent.rotation*closed;transform.localRotation=closed;Physics.SyncTransforms();body.Sleep();lastPush=-1;}
}
