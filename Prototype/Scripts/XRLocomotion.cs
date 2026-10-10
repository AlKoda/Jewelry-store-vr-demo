using System.Collections.Generic;
using UnityEngine;

// Rig-level teleport and snap turn without toolkit dependencies. Put it on the
// rig root (the Player), point Head at the camera and bind controller actions
// to these methods. Teleports land the head over the target, are refused
// outside the interior bounds and refused onto surfaces above floor level.
// Aiming can use a straight ray (FindDestination) or a ballistic arc
// (FindArcDestination); both go through the same destination validation.
public sealed class XRLocomotion : MonoBehaviour
{
    public Transform Head;
    public InteriorBounds Bounds;
    public XRComfortFade ComfortFade;
    public float SnapTurnDegrees = 45f;
    public float MaxTeleportDistance = 8f;
    public float MaxStepHeight = 0.35f;

    private const float ArcGravity = 9.81f;
    private const float ArcStep = 0.05f;
    private const float ArcDuration = 2f;

    private Transform HeadOrRoot => Head != null ? Head : transform;

    private void Awake()
    {
        if (Bounds == null) Bounds = InteriorBounds.On(gameObject);
    }

    // Floor point the pointer ray lands on, or false when it is not a valid destination.
    public bool FindDestination(Ray pointer, out Vector3 floorPoint, Transform ignore = null)
    {
        floorPoint = default;
        if (!DemoPhysics.Nearest(pointer, MaxTeleportDistance, out RaycastHit hit, transform, ignore)
            || !IsDestination(hit.point, hit.normal)) return false;
        floorPoint = hit.point;
        return true;
    }

    // Ballistic arc from the controller: gravity pulls the sample points down until a
    // collider stops them. points (when given) receives the whole arc for drawing;
    // destination is the hit point, validated exactly like a straight-ray destination.
    public bool FindArcDestination(Vector3 origin, Vector3 direction, float speed, List<Vector3> points,
        out Vector3 destination, Transform ignore = null)
    {
        destination = default;
        if (points != null) { points.Clear(); points.Add(origin); }
        Vector3 velocity = direction.normalized * speed;
        Vector3 position = origin;
        for (float time = 0; time < ArcDuration; time += ArcStep)
        {
            velocity += Vector3.down * (ArcGravity * ArcStep);
            Vector3 next = position + velocity * ArcStep;
            Vector3 segment = next - position;
            if (DemoPhysics.Nearest(new Ray(position, segment.normalized), segment.magnitude, out RaycastHit hit, transform, ignore))
            {
                destination = hit.point;
                if (points != null) points.Add(hit.point);
                return IsDestination(hit.point, hit.normal);
            }
            position = next;
            if (points != null) points.Add(position);
        }
        return false;
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
        if (ComfortFade != null) ComfortFade.Fade();
        return true;
    }

    // Turns around the head: the root moves, so it goes through Relocate.
    public void SnapTurn(float degrees)
    {
        Vector3 pivot = HeadOrRoot.position;
        Quaternion turn = Quaternion.AngleAxis(degrees, Vector3.up);
        transform.rotation = turn * transform.rotation;
        Relocate(transform, pivot + turn * (transform.position - pivot));
        if (ComfortFade != null) ComfortFade.Fade();
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
