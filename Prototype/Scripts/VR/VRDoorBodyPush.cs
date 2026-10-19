using UnityEngine;
// A tracked torso sweep makes real room-scale head movement push the same door
// bodies as desktop contact. Teleports and recentres do not kick the doors.
public sealed class VRDoorBodyPush:MonoBehaviour {
 public Transform Head;public DemoModeSwitch Mode;
 Vector3 previous;bool anchored;
 void FixedUpdate(){ProcessPose(Head?Head.position:transform.position,transform.position.y,Mode&&Mode.VRActive);}
 public void ProcessPose(Vector3 head,float floor,bool active){Vector3 now=new Vector3(head.x,floor,head.z);if(!active||!anchored){previous=now;anchored=active;return;}var delta=now-previous;var from=previous;previous=now;float distance=delta.magnitude;if(distance<.002f||distance>.6f)return;
 foreach(var hit in Physics.CapsuleCastAll(from+Vector3.up*.32f,from+Vector3.up*1.3f,.22f,delta/distance,distance+.04f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){var door=hit.collider.GetComponentInParent<PushDoorLeaf>();if(door)door.Push(hit.point,delta/distance);}
 foreach(var collider in Physics.OverlapCapsule(now+Vector3.up*.32f,now+Vector3.up*1.3f,.22f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){var door=collider.GetComponentInParent<PushDoorLeaf>();if(door)door.Push(collider.ClosestPoint(now+Vector3.up*.9f),delta/distance);}
 }
}
