using UnityEngine;

// Rig-level teleport and snap turn without toolkit dependencies. Put it on the
// rig root (XR Origin later, the desktop player now), point Head at the camera
// and bind the toolkit's or Input System's actions to these methods. Teleports
// land the head over the target and are refused outside the interior bounds.
public sealed class XRLocomotion : MonoBehaviour
{
    public Transform Head;
    public InteriorBounds Bounds;
    public float SnapTurnDegrees = 45f;
    public float MaxTeleportDistance = 8f;

    private Transform HeadOrRoot => Head != null ? Head : transform;

    public bool TryTeleport(Ray pointer)
    {
        return Physics.Raycast(pointer, out RaycastHit hit, MaxTeleportDistance) && hit.normal.y >= 0.7f
            && TryTeleport(hit.point);
    }

    public bool TryTeleport(Vector3 floorPoint)
    {
        if (Bounds != null && !Bounds.Contains(floorPoint)) return false;
        Vector3 head = HeadOrRoot.position;
        Move(transform.position + new Vector3(floorPoint.x - head.x, floorPoint.y - transform.position.y, floorPoint.z - head.z));
        return true;
    }

    public void SnapTurn(float degrees) { transform.RotateAround(HeadOrRoot.position, Vector3.up, degrees); }
    public void SnapLeft() { SnapTurn(-SnapTurnDegrees); }
    public void SnapRight() { SnapTurn(SnapTurnDegrees); }

    // A CharacterController on the same object must be off while its transform is set directly.
    private void Move(Vector3 position)
    {
        CharacterController body = GetComponent<CharacterController>();
        bool active = body != null && body.enabled;
        if (active) body.enabled = false;
        transform.position = position;
        if (active) body.enabled = true;
    }
}
