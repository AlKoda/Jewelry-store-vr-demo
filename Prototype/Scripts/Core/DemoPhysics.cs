using UnityEngine;

// Nearest-hit raycast that can skip whole hierarchies (the player rig, a held
// tool) and optionally accept only upward-facing surfaces. Shared by the
// pointer, the hands, tool settling and teleport aiming.
public static class DemoPhysics
{
    private static readonly RaycastHit[] hits = new RaycastHit[64];

    public static bool Nearest(Ray ray, float distance, out RaycastHit nearest,
        Transform ignore = null, Transform alsoIgnore = null, bool upwardOnly = false)
    {
        nearest = default;
        bool found = false;
        int count = Physics.RaycastNonAlloc(ray, hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (ignore != null && hitTransform.IsChildOf(ignore)) continue;
            if (alsoIgnore != null && hitTransform.IsChildOf(alsoIgnore)) continue;
            if (upwardOnly && hits[i].normal.y < 0.7f) continue;
            if (!found || hits[i].distance < nearest.distance) { nearest = hits[i]; found = true; }
        }
        return found;
    }
}
