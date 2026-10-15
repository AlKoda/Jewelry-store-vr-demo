using System.Collections.Generic;
using UnityEngine;

// Runtime-safe face accumulator shared by the Editor fixture library (KitMesh adds
// asset saving and lightmap UVs on top) and the tool visuals that are also built
// in Play mode (DemoToolGeometry: flashlights, camera body). Faces are grouped
// into one submesh per material; a null material is a valid group for parts whose
// renderer material is assigned later. Every face gets flat or smooth normals and
// metric UVs (1 UV unit = 1 m in the part's local space), so a material's
// mainTextureScale = 1 / metres tiles identically on every object.
public class ProceduralMesh
{
    protected readonly List<Vector3> vertices = new List<Vector3>();
    protected readonly List<Vector3> normals = new List<Vector3>();
    protected readonly List<Vector2> uvs = new List<Vector2>();
    protected readonly List<Material> materials = new List<Material>();
    protected readonly List<List<int>> triangles = new List<List<int>>();

    public bool IsEmpty => vertices.Count == 0;

    public int TriangleCount
    {
        get { int n = 0; foreach (List<int> t in triangles) n += t.Count; return n / 3; }
    }

    protected List<int> Sub(Material material)
    {
        int i = materials.IndexOf(material);
        if (i >= 0) return triangles[i];
        materials.Add(material);
        triangles.Add(new List<int>());
        return triangles[triangles.Count - 1];
    }

    // Planar metric UVs: horizontal faces map x/z, others map their own right/up so
    // wood grain and tiles stay upright and unmirrored when seen from the front.
    private static Vector2 PlanarUV(Vector3 p, Vector3 n)
    {
        if (Mathf.Abs(n.y) > 0.7f) return new Vector2(p.x, p.z);
        Vector3 t = (Vector3.up - n * n.y).normalized;
        Vector3 s = Vector3.Cross(n, t);
        return new Vector2(Vector3.Dot(p, s), Vector3.Dot(p, t));
    }

    // Convex planar polygon, fan-triangulated and wound so it faces `outward`.
    // Choosing the winding from the intended side keeps every caller order-agnostic.
    public void Face(Material material, Vector3 outward, params Vector3[] points)
    {
        Vector3 area = Vector3.zero;
        for (int i = 1; i < points.Length - 1; i++) area += Vector3.Cross(points[i] - points[0], points[i + 1] - points[0]);
        if (area.sqrMagnitude < 1e-14f) return; // Degenerate: nothing visible to draw.
        bool flip = Vector3.Dot(area, outward) < 0;
        Vector3 n = flip ? -area.normalized : area.normalized;
        int start = vertices.Count;
        foreach (Vector3 p in points)
        {
            vertices.Add(p);
            normals.Add(n);
            uvs.Add(PlanarUV(p, n));
        }
        List<int> t = Sub(material);
        for (int i = 1; i < points.Length - 1; i++)
        {
            t.Add(start);
            t.Add(start + (flip ? i + 1 : i));
            t.Add(start + (flip ? i : i + 1));
        }
    }

    // Flat quad with explicit UVs (an atlas cell, a decal, a sign): a, b, c and d take the
    // rect's (xMin, yMin), (xMax, yMin), (xMax, yMax) and (xMin, yMax) corners. Wound to
    // face `outward` like Face; a degenerate quad adds nothing.
    public void Quad(Material material, Vector3 outward, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Rect uv)
    {
        Vector3 area = Vector3.Cross(b - a, c - a) + Vector3.Cross(c - a, d - a);
        if (area.sqrMagnitude < 1e-14f) return;
        bool flip = Vector3.Dot(area, outward) < 0;
        Vector3 n = flip ? -area.normalized : area.normalized;
        int start = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
        for (int i = 0; i < 4; i++) normals.Add(n);
        uvs.Add(new Vector2(uv.xMin, uv.yMin));
        uvs.Add(new Vector2(uv.xMax, uv.yMin));
        uvs.Add(new Vector2(uv.xMax, uv.yMax));
        uvs.Add(new Vector2(uv.xMin, uv.yMax));
        List<int> t = Sub(material);
        Tri(t, start, start + 1, start + 2, flip);
        Tri(t, start, start + 2, start + 3, flip);
    }

    // Visible from both sides (glass shards, leaves, crack lines, bag handles).
    public void TwoSided(Material material, params Vector3[] points)
    {
        Vector3 n = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
        Face(material, n, points);
        Face(material, -n, points);
    }

    public void Box(Material material, Vector3 centre, Vector3 size, bool bottom = true)
        => Box(material, centre, size, Quaternion.identity, bottom);

    public void Box(Material material, Vector3 centre, Vector3 size, Quaternion rotation, bool bottom = true)
    {
        Vector3 x = rotation * new Vector3(size.x * 0.5f, 0, 0);
        Vector3 y = rotation * new Vector3(0, size.y * 0.5f, 0);
        Vector3 z = rotation * new Vector3(0, 0, size.z * 0.5f);
        Vector3 c = centre;
        Face(material, x, c + x - y - z, c + x + y - z, c + x + y + z, c + x - y + z);
        Face(material, -x, c - x - y - z, c - x - y + z, c - x + y + z, c - x + y - z);
        Face(material, y, c + y - x - z, c + y - x + z, c + y + x + z, c + y + x - z);
        if (bottom) Face(material, -y, c - y - x - z, c - y + x - z, c - y + x + z, c - y - x + z);
        Face(material, z, c + z - x - y, c + z + x - y, c + z + x + y, c + z - x + y);
        Face(material, -z, c - z - x - y, c - z - x + y, c - z + x + y, c - z + x - y);
    }

    // Box with its four edges along local x bevelled: soft cushions, rounded tops (28 triangles).
    public void ChamferBox(Material material, Vector3 centre, Vector3 size, float bevel, Quaternion rotation)
    {
        float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;
        float b = Mathf.Min(bevel, Mathf.Min(hy, hz) * 0.9f);
        Vector2[] p =
        {
            new Vector2(-hz + b, -hy), new Vector2(hz - b, -hy), new Vector2(hz, -hy + b), new Vector2(hz, hy - b),
            new Vector2(hz - b, hy), new Vector2(-hz + b, hy), new Vector2(-hz, hy - b), new Vector2(-hz, -hy + b)
        };
        Vector3[] left = new Vector3[8], right = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            left[i] = centre + rotation * new Vector3(-hx, p[i].y, p[i].x);
            right[i] = centre + rotation * new Vector3(hx, p[i].y, p[i].x);
        }
        for (int i = 0; i < 8; i++)
        {
            int j = (i + 1) % 8;
            Vector3 outward = rotation * new Vector3(0, p[i].y + p[j].y, p[i].x + p[j].x);
            Face(material, outward, left[i], left[j], right[j], right[i]);
        }
        Face(material, rotation * Vector3.left, left);
        Face(material, rotation * Vector3.right, right);
    }

    // Surface of revolution around the local up axis of `rotation`, placed at `origin`:
    // ring k has radius radii[k] at height heights[k]. Smooth lathes share normals
    // around the axis (round parts); flat ones give each side its own face (square
    // legs and pots with sides = 4 and phase = 45). UVs run arc length by profile length.
    public void Lathe(Material material, Vector3 origin, Quaternion rotation, float[] radii, float[] heights, int sides,
        bool smooth = true, bool capStart = true, bool capEnd = true, float phaseDegrees = 0)
    {
        float phase = phaseDegrees * Mathf.Deg2Rad, step = 2 * Mathf.PI / sides;
        Vector3 up = rotation * Vector3.up;
        float v = 0;
        for (int k = 0; k < radii.Length - 1; k++)
        {
            float r0 = radii[k], r1 = radii[k + 1], y0 = heights[k], y1 = heights[k + 1];
            float length = new Vector2(r1 - r0, y1 - y0).magnitude;
            if ((r0 <= 0 && r1 <= 0) || length < 1e-6f) { v += length; continue; }
            Vector2 slope = new Vector2(y1 - y0, -(r1 - r0)) / length; // Outward profile normal: (radial, axial).
            float arc = Mathf.Max(r0, r1);
            if (!smooth)
            {
                for (int i = 0; i < sides; i++)
                {
                    float a = phase + i * step, b = a + step, mid = a + step * 0.5f;
                    Vector3 da = Dir(rotation, a), db = Dir(rotation, b);
                    Vector3 outward = Dir(rotation, mid) * slope.x + up * slope.y;
                    Vector3 p0 = origin + da * r0 + up * y0, p1 = origin + db * r0 + up * y0;
                    Vector3 p2 = origin + db * r1 + up * y1, p3 = origin + da * r1 + up * y1;
                    if (r0 <= 0) Face(material, outward, p0, p2, p3);
                    else if (r1 <= 0) Face(material, outward, p0, p1, p2);
                    else Face(material, outward, p0, p1, p2, p3);
                }
            }
            else
            {
                int start = vertices.Count;
                for (int i = 0; i <= sides; i++)
                {
                    float a = phase + i * step;
                    Vector3 d = Dir(rotation, a);
                    Vector3 n = (d * slope.x + up * slope.y).normalized;
                    vertices.Add(origin + d * r0 + up * y0); normals.Add(n); uvs.Add(new Vector2(a * arc, v));
                    vertices.Add(origin + d * r1 + up * y1); normals.Add(n); uvs.Add(new Vector2(a * arc, v + length));
                }
                // Winding from the first quad's diagonals, which stay valid when one ring is a point.
                Vector3 q0 = vertices[start], q1 = vertices[start + 2], q2 = vertices[start + 3], q3 = vertices[start + 1];
                bool flip = Vector3.Dot(Vector3.Cross(q2 - q0, q3 - q1), normals[start]) < 0;
                List<int> t = Sub(material);
                for (int i = 0; i < sides; i++)
                {
                    int a = start + 2 * i, b = a + 2, c = a + 3, d = a + 1;
                    if (r0 > 0) Tri(t, a, b, c, flip);
                    if (r1 > 0) Tri(t, a, c, d, flip);
                }
            }
            v += length;
        }
        int last = radii.Length - 1;
        if (capStart && radii[0] > 0) Face(material, -up, Ring(origin, rotation, radii[0], heights[0], sides, phase));
        if (capEnd && radii[last] > 0) Face(material, up, Ring(origin, rotation, radii[last], heights[last], sides, phase));
    }

    // Smooth loft of elliptical rings (half-widths halfX by halfZ at heights[k]) around
    // the local up axis of `rotation`: busts and other soft organic forms. Rings share
    // vertices, and normals come from the surface's own tangents, so the silhouette
    // shades smoothly across profile changes. All half-widths must be positive.
    public void Loft(Material material, Vector3 origin, Quaternion rotation, float[] halfX, float[] halfZ, float[] heights, int sides,
        bool capStart = true, bool capEnd = true)
    {
        int rings = heights.Length, start = vertices.Count;
        float step = 2 * Mathf.PI / sides;
        for (int k = 0; k < rings; k++)
        {
            int prev = Mathf.Max(k - 1, 0), next = Mathf.Min(k + 1, rings - 1);
            for (int i = 0; i <= sides; i++)
            {
                float a = i * step, c = Mathf.Cos(a), s = Mathf.Sin(a);
                Vector3 along = new Vector3((halfX[next] - halfX[prev]) * c, heights[next] - heights[prev], (halfZ[next] - halfZ[prev]) * s);
                Vector3 around = new Vector3(-halfX[k] * s, 0, halfZ[k] * c);
                Vector3 n = Vector3.Cross(along, around).normalized;
                vertices.Add(origin + rotation * new Vector3(halfX[k] * c, heights[k], halfZ[k] * s));
                normals.Add(rotation * n);
                uvs.Add(new Vector2(a * (halfX[k] + halfZ[k]) * 0.5f, heights[k]));
            }
        }
        List<int> t = Sub(material);
        for (int k = 0; k < rings - 1; k++)
            for (int i = 0; i < sides; i++)
            {
                int a = start + k * (sides + 1) + i, b = a + 1, c = b + sides + 1, d = a + sides + 1;
                Quad(t, a, b, c, d);
            }
        Vector3 up = rotation * Vector3.up;
        if (capStart) Face(material, -up, Ellipse(origin, rotation, halfX[0], halfZ[0], heights[0], sides));
        if (capEnd) Face(material, up, Ellipse(origin, rotation, halfX[rings - 1], halfZ[rings - 1], heights[rings - 1], sides));
    }

    // Closed ring (rings, bangles, chains) lying in the local xz plane of `rotation`:
    // an elliptical centre line radiusX by radiusZ swept by a tube of `tube` radius.
    public void Torus(Material material, Vector3 centre, Quaternion rotation, float radiusX, float radiusZ, float tube, int segments, int sides)
    {
        int start = vertices.Count;
        for (int i = 0; i <= segments; i++)
        {
            float a = i * 2 * Mathf.PI / segments, c = Mathf.Cos(a), s = Mathf.Sin(a);
            Vector3 line = new Vector3(radiusX * c, 0, radiusZ * s);
            Vector3 outward = new Vector3(c / radiusX, 0, s / radiusZ).normalized;
            for (int j = 0; j <= sides; j++)
            {
                float b = j * 2 * Mathf.PI / sides;
                Vector3 n = outward * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                vertices.Add(centre + rotation * (line + n * tube));
                normals.Add(rotation * n);
                uvs.Add(new Vector2(a * (radiusX + radiusZ) * 0.5f, b * tube));
            }
        }
        List<int> t = Sub(material);
        for (int i = 0; i < segments; i++)
            for (int j = 0; j < sides; j++)
            {
                int a = start + i * (sides + 1) + j, b = a + sides + 1, c = b + 1, d = a + 1;
                Quad(t, a, b, c, d);
            }
    }

    // Straight cylinder from a to b (bolts, spokes, handles, columns).
    public void Cylinder(Material material, Vector3 a, Vector3 b, float radius, int sides, bool caps = true)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, b - a);
        float length = (b - a).magnitude;
        Lathe(material, a, rotation, new[] { radius, radius }, new[] { 0f, length }, sides, sides > 6, caps, caps);
    }

    // Flat strip of the given width from a to b, in the plane facing `outward` (line drawings, letters, cracks).
    public void Stroke(Material material, Vector3 a, Vector3 b, float width, Vector3 outward)
    {
        Vector3 side = Vector3.Cross(outward, b - a).normalized * (width * 0.5f);
        Face(material, outward, a - side, b - side, b + side, a + side);
    }

    public static Vector3[] Ring(Vector3 origin, Quaternion rotation, float radius, float height, int sides, float phase)
    {
        Vector3[] ring = new Vector3[sides];
        for (int i = 0; i < sides; i++)
            ring[i] = origin + rotation * new Vector3(Mathf.Cos(phase + i * 2 * Mathf.PI / sides) * radius, height,
                Mathf.Sin(phase + i * 2 * Mathf.PI / sides) * radius);
        return ring;
    }

    public static Vector3[] Ellipse(Vector3 origin, Quaternion rotation, float halfX, float halfZ, float height, int sides)
    {
        Vector3[] ring = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float a = i * 2 * Mathf.PI / sides;
            ring[i] = origin + rotation * new Vector3(Mathf.Cos(a) * halfX, height, Mathf.Sin(a) * halfZ);
        }
        return ring;
    }

    private static Vector3 Dir(Quaternion rotation, float angle) => rotation * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

    private static void Tri(List<int> t, int a, int b, int c, bool flip)
    {
        t.Add(a);
        t.Add(flip ? c : b);
        t.Add(flip ? b : c);
    }

    // Quad a-b-c-d over shared smooth vertices, wound to face along their normals.
    private void Quad(List<int> t, int a, int b, int c, int d)
    {
        Vector3 n = normals[a] + normals[b] + normals[c] + normals[d];
        bool flip = Vector3.Dot(Vector3.Cross(vertices[c] - vertices[a], vertices[d] - vertices[b]), n) < 0;
        Tri(t, a, b, c, flip);
        Tri(t, a, c, d, flip);
    }

    // Mesh with one submesh per material group, in the order the groups were first used.
    protected Mesh BuildMesh(string name)
    {
        Mesh mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = triangles.Count;
        for (int i = 0; i < triangles.Count; i++) mesh.SetTriangles(triangles[i], i);
        mesh.RecalculateBounds();
        return mesh;
    }

    // Runtime mesh (not saved): tangents included so normal-mapped materials work.
    public Mesh ToMesh(string name)
    {
        Mesh mesh = BuildMesh(name);
        mesh.RecalculateTangents();
        return mesh;
    }

    // Materials of the groups, in submesh order.
    public Material[] Materials => materials.ToArray();
}
