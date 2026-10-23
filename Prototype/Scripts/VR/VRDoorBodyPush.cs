using UnityEngine;
// A tracked torso sweep makes real room-scale head movement push the same door
// bodies as desktop contact. Teleports and recentres do not kick the doors.
public sealed class VRDoorBodyPush:MonoBehaviour {
 public Transform Head;public DemoModeSwitch Mode;
 Vector3 previous;bool anchored;
 static readonly RaycastHit[] sweepHits=new RaycastHit[16];static readonly Collider[] overlapHits=new Collider[16];
 void FixedUpdate(){ProcessPose(Head?Head.position:transform.position,transform.position.y,Mode&&Mode.VRActive);}
 public void ProcessPose(Vector3 head,float floor,bool active){Vector3 now=new Vector3(head.x,floor,head.z);if(!active||!anchored){previous=now;anchored=active;return;}var delta=now-previous;var from=previous;previous=now;float distance=delta.magnitude;if(distance<.002f||distance>.6f)return;
 int sweeps=Physics.CapsuleCastNonAlloc(from+Vector3.up*.32f,from+Vector3.up*1.3f,.22f,delta/distance,sweepHits,distance+.04f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
 for(int i=0;i<sweeps;i++){var door=sweepHits[i].collider.GetComponentInParent<PushDoorLeaf>();if(door)door.Push(sweepHits[i].point,delta/distance);}
 int overlaps=Physics.OverlapCapsuleNonAlloc(now+Vector3.up*.32f,now+Vector3.up*1.3f,.22f,overlapHits,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
 for(int i=0;i<overlaps;i++){var door=overlapHits[i].GetComponentInParent<PushDoorLeaf>();if(door)door.Push(overlapHits[i].ClosestPoint(now+Vector3.up*.9f),delta/distance);}
 }
}
