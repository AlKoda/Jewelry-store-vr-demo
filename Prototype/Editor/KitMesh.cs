#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Accumulates faces for one rigid part (a cabinet body, a door leaf, a pane) into
// a single mesh with one submesh per material, so a fixture costs one renderer per
// moving part instead of one per box. Every face gets flat or lathe normals and
// metric UVs (1 UV unit = 1 m in the part's local space), so a material's
// mainTextureScale = 1 / metres tiles identically on every object. Saved meshes
// get a lightmap UV set (DemoGeometry.SaveMesh with lightmapUVs).
public sealed class KitMesh
{
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Vector3> normals = new List<Vector3>();
    private readonly List<Vector2> uvs = new List<Vector2>();
    private readonly List<Material> materials = new List<Material>();
    private readonly List<List<int>> triangles = new List<List<int>>();

    public bool IsEmpty => vertices.Count == 0;

    public int TriangleCount
    {
        get { int n = 0; foreach (List<int> t in triangles) n += t.Count; return n / 3; }
    }

    private List<int> Sub(Material material)
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

    // Visible from both sides (glass shards, leaves, crack lines).
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

    private static Vector3 Dir(Quaternion rotation, float angle) => rotation * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

    private static void Tri(List<int> t, int a, int b, int c, bool flip)
    {
        t.Add(a);
        t.Add(flip ? c : b);
        t.Add(flip ? b : c);
    }

    // Stable fingerprint of the geometry: identical parts resolve to one saved asset.
    private int Signature()
    {
        unchecked
        {
            int h = vertices.Count * 397 ^ TriangleCount;
            foreach (Vector3 p in vertices)
                h = h * 31 + Mathf.RoundToInt(p.x * 1000) * 7 + Mathf.RoundToInt(p.y * 1000) * 13 + Mathf.RoundToInt(p.z * 1000);
            foreach (Material m in materials) h = h * 31 + (m != null ? m.name.GetHashCode() : 0);
            return h;
        }
    }

    private static readonly Dictionary<string, Mesh> saved = new Dictionary<string, Mesh>();

    // Every mesh asset path saved or reused since the editor domain loaded.
    private static readonly HashSet<string> touched = new HashSet<string>();

    // Deletes mesh assets in folder that nothing saved or reused in this editor session,
    // i.e. the fingerprints left behind by earlier geometry. Call it only after building
    // everything that should keep its meshes (the gallery run builds every fixture).
    public static int DeleteUntouched(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return 0;
        int deleted = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".asset") || touched.Contains(path)) continue;
            if (AssetDatabase.DeleteAsset(path)) deleted++;
        }
        return deleted;
    }

    // Saves (or reuses) the mesh as <folder>/<name>_<fingerprint>.asset. Parts with
    // identical geometry share one asset, so repeated fixtures do not re-unwrap.
    public Mesh Save(string folder, string name)
    {
        string path = folder + "/" + name + "_" + Signature().ToString("x8") + ".asset";
        touched.Add(path);
        if (saved.TryGetValue(path, out Mesh cached) && cached != null) return cached;
        Mesh mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = triangles.Count;
        for (int i = 0; i < triangles.Count; i++) mesh.SetTriangles(triangles[i], i);
        mesh.RecalculateBounds();
        DemoGeometry.SaveMesh(ref mesh, path, true);
        saved[path] = mesh;
        return mesh;
    }

    // Child object carrying this part; glass-only parts cast no shadows.
    public GameObject Emit(string name, Transform parent, Vector3 position, Quaternion rotation, string folder, string assetName)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        g.transform.localRotation = rotation;
        if (IsEmpty) return g;
        g.AddComponent<MeshFilter>().sharedMesh = Save(folder, assetName);
        MeshRenderer renderer = g.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = materials.ToArray();
        bool transparent = true;
        foreach (Material m in materials) transparent &= m != null && m.renderQueue >= 3000;
        if (transparent) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; }
        return g;
    }
}
#endif
