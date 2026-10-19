#if UNITY_EDITOR
using UnityEngine;
using static DemoGeometry;

// The security and electrical devices, in the StoreKit conventions: wall pieces
// (pir_detector, light_switch, socket_outlet) pivot at the bottom centre of their back,
// which sits on z = 0, and face -z; ceiling pieces (cctv_dome, smoke_detector) pivot at
// the ceiling contact and stay entirely at or below y = 0.
// They are country-neutral: single 82 mm square plates with blank faces. Plate size, pin
// pattern and switch style follow Koda's decision 1. No call point is placed (decision 10).
// Each device is one KitMesh "Body" with lightmap UVs, like every gallery mesh. In the
// night store MarkStatic makes them probe-lit, since all are under 0.3 m (SmallProp).
// They carry no collider. InteriorModels' CCTVDome (1,440 triangles, alpha-0.55 dome) and
// SmokeDetector (556) are deliberately not used.
public static partial class StoreKit
{
    // Ceiling dome camera, 14 cm across (206 triangles): a cream base ring and a smoked
    // dome drawn opaque in KitWindowDark, so it adds no transparent renderer; at night a
    // smoked dome hides its lens anyway. The dome is a loft on a half-ellipse (62 by 47 mm)
    // with a 6 mm cap, so its normals run smoothly down the profile: a lathe's bands would
    // show as rings in the reflections of the glossy dome.
    public static GameObject CctvDome(Transform parent)
    {
        GameObject root = Root("cctv_dome", parent);
        KitMesh k = new KitMesh();
        Quaternion down = Quaternion.Euler(180, 0, 0); // Local lathe and loft heights run downwards from the ceiling.
        k.Lathe(Lacquer, Vector3.zero, down, new[] { 0.07f, 0.07f, 0.062f }, new[] { 0f, 0.012f, 0.016f }, 16, true, false, false);
        float[] dome = { 0.062f, 0.0537f, 0.031f, 0.011f, 0.003f };
        k.Loft(WindowDark, Vector3.zero, down, dome, dome, new[] { 0.016f, 0.0395f, 0.0567f, 0.0623f, 0.063f }, 16, false, true);
        Part(k, "Body", root, Vector3.zero, "cctv_dome");
        return root;
    }

    // Ceiling optical smoke detector, 11.6 cm across and 46 mm deep (86 triangles), with a
    // dark status LED off centre. The cone under the LED is about 44.6 mm deep at its inner
    // edge, so the flat LED sits 0.3 mm below that (the apex is at 46 mm).
    public static GameObject SmokeDetector(Transform parent)
    {
        GameObject root = Root("smoke_detector", parent);
        KitMesh k = new KitMesh();
        Quaternion down = Quaternion.Euler(180, 0, 0);
        k.Lathe(Lacquer, Vector3.zero, down, new[] { 0.058f, 0.058f, 0.052f, 0.036f, 0f }, new[] { 0f, 0.028f, 0.038f, 0.044f, 0.046f }, 12, true, false, false);
        const float led = 0.003f, y = -0.0449f;
        k.Face(DeviceBlack, Vector3.down, V(0.028f - led, y, -led), V(0.028f + led, y, -led), V(0.028f + led, y, led), V(0.028f - led, y, led));
        Part(k, "Body", root, Vector3.zero, "smoke_detector");
        return root;
    }

    // Wall PIR motion detector, 4.6 cm proud (44 triangles): back plate, a body with its
    // four vertical edges bevelled, a flat grey Fresnel lens across the top (where real
    // detectors carry it) and a dark LED under it, both 0.3 mm proud of the body's flat
    // front (x +-0.025 between the bevels).
    public static GameObject PirDetector(Transform parent)
    {
        GameObject root = Root("pir_detector", parent);
        KitMesh k = new KitMesh();
        k.Box(Lacquer, V(0, 0.05f, -0.003f), V(0.055f, 0.10f, 0.006f));
        k.Bar(Lacquer, V(0, 0.055f, -0.026f), V(0.062f, 0.105f, 0.04f), 0.006f);
        const float z = -0.0463f;
        k.Face(DeviceGrey, Vector3.back, V(-0.024f, 0.06f, z), V(0.024f, 0.06f, z), V(0.024f, 0.098f, z), V(-0.024f, 0.098f, z));
        k.Face(DeviceBlack, Vector3.back, V(0.016f, 0.048f, z), V(0.02f, 0.048f, z), V(0.02f, 0.052f, z), V(0.016f, 0.052f, z));
        Part(k, "Body", root, Vector3.zero, "pir_detector");
        return root;
    }

    // Light switch (40 triangles): an 82 mm square plate, 9 mm deep, with one wide rocker.
    public static GameObject LightSwitch(Transform parent)
    {
        GameObject root = Root("light_switch", parent);
        KitMesh k = new KitMesh();
        k.Bar(Lacquer, V(0, 0.041f, -0.0045f), V(0.082f, 0.082f, 0.009f), 0.002f);
        k.Box(Lacquer, V(0, 0.041f, -0.0105f), V(0.03f, 0.05f, 0.003f));
        Part(k, "Body", root, Vector3.zero, "light_switch");
        return root;
    }

    // Single socket outlet (30 triangles): the switch's plate with a blank 46 mm grey
    // insert 0.3 mm proud. No pin pattern or switch: those name a country.
    public static GameObject SocketOutlet(Transform parent)
    {
        GameObject root = Root("socket_outlet", parent);
        KitMesh k = new KitMesh();
        k.Bar(Lacquer, V(0, 0.041f, -0.0045f), V(0.082f, 0.082f, 0.009f), 0.002f);
        const float half = 0.023f, z = -0.0093f;
        k.Face(DeviceGrey, Vector3.back, V(-half, 0.041f - half, z), V(half, 0.041f - half, z), V(half, 0.041f + half, z), V(-half, 0.041f + half, z));
        Part(k, "Body", root, Vector3.zero, "socket_outlet");
        return root;
    }
}
#endif
