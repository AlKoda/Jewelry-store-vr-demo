using UnityEngine;

// Rig-level teleport and snap turn without toolkit dependencies. Put it on the
// rig root (the Player), point Head at the camera and bind controller actions
// to these methods. Teleports land the head over the target, are refused
// outside the interior bounds and refused onto surfaces above floor level.
public sealed class XRLocomotion : MonoBehaviour
{
    public Transform Head;
    public InteriorBounds Bounds;
    public float SnapTurnDegrees = 45f;
    public float MaxTeleportDistance = 8f;
    public float MaxStepHeight = 0.35f;

    private Transform HeadOrRoot => Head != null ? Head : transform;

    private void Awake()
    {
        if (Bounds == null) Bounds = InteriorBounds.On(gameObject);
    }

    // Floor point the pointer ray lands on, or false when it is not a valid destination.
    public bool FindDestination(Ray pointer, out Vector3 floorPoint)
    {
        floorPoint = default;
        if (!Physics.Raycast(pointer, out RaycastHit hit, MaxTeleportDistance) || !IsDestination(hit.point, hit.normal)) return false;
        floorPoint = hit.point;
        return true;
    }

    public bool IsDestination(Vector3 point, Vector3 normal)
    {
        return normal.y >= 0.7f && Mathf.Abs(point.y - transform.position.y) <= MaxStepHeight
            && (Bounds == null || Bounds.Contains(point));
    }

    public bool TryTeleport(Ray pointer)
    {
        return FindDestination(pointer, out Vector3 floorPoint) && TryTeleport(floorPoint);
    }

    public bool TryTeleport(Vector3 floorPoint)
    {
        if (!IsDestination(floorPoint, Vector3.up)) return false;
        Vector3 head = HeadOrRoot.position;
        Relocate(transform, transform.position + new Vector3(floorPoint.x - head.x, floorPoint.y - transform.position.y, floorPoint.z - head.z));
        return true;
    }

    // Turns around the head: the root moves, so it goes through Relocate.
    public void SnapTurn(float degrees)
    {
        Vector3 pivot = HeadOrRoot.position;
        Quaternion turn = Quaternion.AngleAxis(degrees, Vector3.up);
        transform.rotation = turn * transform.rotation;
        Relocate(transform, pivot + turn * (transform.position - pivot));
    }

    public void SnapLeft() { SnapTurn(-SnapTurnDegrees); }
    public void SnapRight() { SnapTurn(SnapTurnDegrees); }

    // Sets a position directly. A CharacterController on the object must be off
    // meanwhile or it keeps its old position; the walker uses this too.
    public static void Relocate(Transform target, Vector3 position)
    {
        CharacterController body = target.GetComponent<CharacterController>();
        bool active = body != null && body.enabled;
        if (active) body.enabled = false;
        target.position = position;
        if (active) body.enabled = true;
    }
}
