using UnityEngine;

// Walkable and placeable interior in metres. Shared by desktop walking, tool
// placement and, later, XR teleport destination filtering.
public sealed class InteriorBounds : MonoBehaviour
{
    [System.Serializable]
    public struct Region
    {
        public float MinX, MaxX, MinZ, MaxZ;

        public Region(float minX, float maxX, float minZ, float maxZ)
        {
            MinX = minX; MaxX = maxX; MinZ = minZ; MaxZ = maxZ;
        }

        public bool Contains(Vector3 p) =>
            p.x >= MinX && p.x <= MaxX && p.z >= MinZ && p.z <= MaxZ;

        public Vector3 Clamp(Vector3 p) =>
            new Vector3(Mathf.Clamp(p.x, MinX, MaxX), p.y, Mathf.Clamp(p.z, MinZ, MaxZ));
    }

    // Showroom, then the safe room reached through the rear doorway.
    public Region[] Regions =
    {
        new Region(-4.7f, 4.7f, 0.3f, 8.25f),
        new Region(1.3f, 3.7f, 8.25f, 10.75f)
    };

    // The object's own bounds, adding the default shop regions when none exist,
    // so walking and teleporting are never left unconstrained.
    public static InteriorBounds On(GameObject owner)
    {
        InteriorBounds bounds = owner.GetComponent<InteriorBounds>();
        return bounds != null ? bounds : owner.AddComponent<InteriorBounds>();
    }

    public bool Contains(Vector3 p)
    {
        foreach (Region region in Regions)
            if (region.Contains(p)) return true;
        return false;
    }

    // Nearest point inside any region; height is preserved.
    public Vector3 Clamp(Vector3 p)
    {
        Vector3 best = p;
        float bestDistance = float.MaxValue;
        foreach (Region region in Regions)
        {
            Vector3 candidate = region.Clamp(p);
            float distance = (candidate - p).sqrMagnitude;
            if (distance < bestDistance) { best = candidate; bestDistance = distance; }
        }
        return best;
    }
}
