using System.Collections.Generic;
using UnityEngine;

// Rig-level teleport, smooth thumbstick movement and snap turn without toolkit dependencies. Put it on the
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
    // Smooth movement: walking pace, the stick dead zone and the body radius kept clear of walls.
    public float MoveSpeed = 1.4f;
    public float MoveDeadZone = 0.2f;
    public float BodyRadius = 0.25f;

    private const float ArcGravity = 9.81f;
    private const float ArcStep = 0.05f;
    private const float ArcDuration = 2f;

    private Transform HeadOrRoot => Head != null ? Head : transform;

    private void Awake()
    {
        if (Bounds == null) Bounds = InteriorBounds.On(gameObject);
        Vignette = GetComponent<XRComfortVignette>();
        if (Vignette == null) Vignette = gameObject.AddComponent<XRComfortVignette>();
        if (Vignette.Head == null) Vignette.Head = Head;
        Holsters = GetComponent<XRHolsters>();
        if (Holsters == null) Holsters = gameObject.AddComponent<XRHolsters>();
        if (Holsters.Head == null) Holsters.Head = Head;
    }

    // Flashlight, UV torch and camera at the hips and chest.
    public XRHolsters Holsters { get; private set; }

    // Edge darkening while the stick glides the rig (F7 switches it).
    public XRComfortVignette Vignette { get; private set; }

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

    // Smooth movement from a thumbstick: forward is where the head looks (flattened), the
    // speed ramps with the stick past the dead zone, and the step is refused or slid along
    // a wall when knee or chest rays hit something within BodyRadius, or when the floor
    // under the new head position is not a valid destination (outside the bounds, a step
    // too high, no floor). Returns the distance moved, for footsteps.
    public float SmoothMove(Vector2 stick, float deltaTime)
    {
        float amount = stick.magnitude;
        if (amount < MoveDeadZone || deltaTime <= 0) return 0;
        Transform head = HeadOrRoot;
        Vector3 forward = head.forward; forward.y = 0;
        if (forward.sqrMagnitude < 1e-4f) forward = transform.forward;
        forward.Normalize();
        Vector3 right = new Vector3(forward.z, 0, -forward.x);
        float speed = MoveSpeed * Mathf.InverseLerp(MoveDeadZone, 1f, Mathf.Min(amount, 1f));
        Vector3 step = (forward * stick.y + right * stick.x).normalized * speed * deltaTime;
        if (TryStep(step)) return step.magnitude;
        // Slide along a wall: try each horizontal component on its own.
        Vector3 alongX = new Vector3(step.x, 0, 0), alongZ = new Vector3(0, 0, step.z);
        if (Mathf.Abs(step.x) >= Mathf.Abs(step.z)) { if (TryStep(alongX)) return alongX.magnitude; if (TryStep(alongZ)) return alongZ.magnitude; }
        else { if (TryStep(alongZ)) return alongZ.magnitude; if (TryStep(alongX)) return alongX.magnitude; }
        return 0;
    }

    private bool TryStep(Vector3 step)
    {
        float length = step.magnitude;
        if (length < 1e-5f) return false;
        Vector3 direction = step / length;
        Vector3 head = HeadOrRoot.position;
        float floorY = transform.position.y;
        foreach (float height in new[] { 0.45f, 1.1f })
        {
            Vector3 origin = new Vector3(head.x, floorY + height, head.z);
            if (DemoPhysics.Nearest(new Ray(origin, direction), length + BodyRadius, out RaycastHit _, transform)) return false;
        }
        Vector3 target = new Vector3(head.x, floorY, head.z) + step;
        if (!DemoPhysics.Nearest(new Ray(target + Vector3.up * (MaxStepHeight + 0.2f), Vector3.down), MaxStepHeight * 2 + 0.4f,
                out RaycastHit floor, transform, null, true)
            || !IsDestination(floor.point, floor.normal)) return false;
        Relocate(transform, transform.position + new Vector3(step.x, floor.point.y - floorY, step.z));
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

    // Turns and moves the rig so the tracked head stands over floorPoint looking along
    // yaw. Floor-relative tracking puts the head wherever the visitor stands in the
    // play area, facing wherever they face, so without this a VR session would not
    // start at the spawn facing the shop. The root goes to floorPoint's height.
    public void Recentre(Vector3 floorPoint, float yaw)
    {
        Transform head = HeadOrRoot;
        Vector3 forward = head.forward;
        forward.y = 0;
        float current = forward.sqrMagnitude > 1e-4f ? Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg : transform.eulerAngles.y;
        transform.rotation = Quaternion.AngleAxis(Mathf.DeltaAngle(current, yaw), Vector3.up) * transform.rotation;
        Vector3 at = head.position;
        Relocate(transform, transform.position + new Vector3(floorPoint.x - at.x, floorPoint.y - transform.position.y, floorPoint.z - at.z));
        if (ComfortFade != null) ComfortFade.Fade(); // the start-up turn and jump blink like a teleport
    }

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
