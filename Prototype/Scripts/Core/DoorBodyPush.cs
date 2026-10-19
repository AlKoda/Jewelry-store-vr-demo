using UnityEngine;
// Shared by the desktop and XR CharacterControllers: contact pushes a physical leaf.
[RequireComponent(typeof(CharacterController))]
public sealed class DoorBodyPush : MonoBehaviour {
 void OnControllerColliderHit(ControllerColliderHit hit){if(hit.moveDirection.y<-.5f)return;var door=hit.collider.GetComponentInParent<PushDoorLeaf>();if(door)door.Push(hit.point,hit.moveDirection);}
}
