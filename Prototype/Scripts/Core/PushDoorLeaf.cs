using UnityEngine;
// A hinged door leaf (SceneReviewPass.Doors): dynamic rigidbody, hinge with limits and a soft
// closing spring. Body and controller contact (DoorBodyPush, VRDoorBodyPush, VRDoorHandPush,
// XRLocomotion) call Push every frame they touch it. The push does not fire an impulse: it eases
// the leaf's swing speed towards OpenSpeed in the pushed direction (Acceleration per second), so
// a brush of the hand opens the door gently and a walk through it never flings it. The hinge's
// damped spring and the body's angular damping bring it back once the contact ends.
[RequireComponent(typeof(Rigidbody),typeof(HingeJoint))]
public sealed class PushDoorLeaf : MonoBehaviour {
 [Tooltip("Legacy impulse strength; kept for saved scenes, unused by the eased swing.")] public float PushImpulse=2.4f;
 public float OpenSpeed=1.1f;      // rad/s, about 63 degrees a second at full push
 public float Acceleration=3.5f;   // rad/s^2 towards OpenSpeed while pushed
 public float StartFraction=.5f;   // a first touch starts the leaf at this share of OpenSpeed, then it eases up
 Rigidbody body;HingeJoint hinge;float lastPush=-1;Quaternion closed;
 public float OpeningDegrees=>Mathf.Abs(Mathf.DeltaAngle(0,transform.localEulerAngles.y));
 public float SwingSpeed=>body?Mathf.Abs(body.angularVelocity.y):0;
 void Awake(){body=GetComponent<Rigidbody>();hinge=GetComponent<HingeJoint>();closed=transform.localRotation;}
 public void Push(Vector3 point,Vector3 direction){
  direction.y=0;if(direction.sqrMagnitude<.01f)return;
  Vector3 axis=transform.TransformPoint(hinge?hinge.anchor:Vector3.zero);
  float torque=Vector3.Cross(point-axis,direction.normalized).y; // which way this contact turns the leaf
  if(Mathf.Abs(torque)<.0005f)return;
  float dt=lastPush<0?Time.deltaTime:Mathf.Clamp(Time.time-lastPush,0,.1f);lastPush=Time.time;
  body.WakeUp();
  Vector3 w=body.angularVelocity;
  float sign=Mathf.Sign(torque),start=sign*OpenSpeed*StartFraction;
  // Turning the other way, or barely moving: the contact takes the leaf to the gentle starting
  // speed at once (a nudge opens it a few degrees); held contact then eases it up to OpenSpeed.
  if(Mathf.Sign(w.y)!=sign||Mathf.Abs(w.y)<Mathf.Abs(start))w.y=start;
  else w.y=Mathf.MoveTowards(w.y,sign*OpenSpeed,Acceleration*dt);
  body.angularVelocity=w;
 }
 public void Close(){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.rotation=transform.parent.rotation*closed;transform.localRotation=closed;Physics.SyncTransforms();body.Sleep();lastPush=-1;}
}
