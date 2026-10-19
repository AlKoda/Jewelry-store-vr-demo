using UnityEngine;
[RequireComponent(typeof(Rigidbody),typeof(HingeJoint))]
public sealed class PushDoorLeaf : MonoBehaviour {
 public float PushImpulse=2.4f;
 Rigidbody body;float nextPush;Quaternion closed;
 public float OpeningDegrees=>Mathf.Abs(Mathf.DeltaAngle(0,transform.localEulerAngles.y));
 void Awake(){body=GetComponent<Rigidbody>();closed=transform.localRotation;}
 public void Push(Vector3 point,Vector3 direction){if(Time.time<nextPush)return;nextPush=Time.time+.06f;direction.y=0;if(direction.sqrMagnitude<.01f)return;body.WakeUp();body.AddForceAtPosition(direction.normalized*PushImpulse,point,ForceMode.Impulse);}
 public void Close(){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.rotation=transform.parent.rotation*closed;transform.localRotation=closed;Physics.SyncTransforms();body.Sleep();}
}
