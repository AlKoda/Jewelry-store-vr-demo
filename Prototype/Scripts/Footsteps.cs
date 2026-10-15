using UnityEngine;

// The walker's footsteps on the rig root (the Player): one step every Stride metres of
// horizontal travel, a Hard step on tile and pavement or a Glass step inside GlassZones
// (world x/z rects, Rect y = world z: NightStoreDressing.GlassZones, the entrance fans
// and the strips before the broken cases). A move longer than MaxStep, or faster than
// MaxSpeed, in one frame (a teleport, a reset to the spawn, the VR recentre) and a snap
// turn (the root swings round the head) are jumps: they are not walked and start the
// stride again. It follows the root, not the head, so physical room-scale walking in VR,
// where the visitor hears their own steps, never plays it. One 2D source, created at
// Awake; a step with no clip is still counted.
public sealed class Footsteps : MonoBehaviour
{
    public AudioClip[] Hard = new AudioClip[0], Glass = new AudioClip[0];
    public Rect[] GlassZones = new Rect[0];
    public float Stride = 0.7f, MaxStep = 1.5f;
    // Sprinting on the desktop is about 3 m/s; anything much faster is a relocation.
    public float MaxSpeed = 6f;
    // A root turn larger than this in one frame is a snap turn (mouse look turns far less).
    public float SnapTurnDegrees = 20;
    [Range(0, 1)] public float Volume = 0.35f, GlassVolume = 0.3f;
    public float PitchJitter = 0.05f;

    public int Steps { get; private set; }
    public bool LastOnGlass { get; private set; }
    public AudioSource Source { get; private set; }

    private Vector3 last;
    private bool anchored;
    private float travelled, lastYaw;
    private int lastHard = -1, lastGlass = -1;

    private void Awake()
    {
        Source = gameObject.AddComponent<AudioSource>();
        Source.playOnAwake = false;
        Source.spatialBlend = 0;
        Source.dopplerLevel = 0;
        Source.priority = 96;
    }

    private void OnEnable() { anchored = false; }

    private void Update()
    {
        Vector3 p = transform.position;
        float yaw = transform.eulerAngles.y;
        bool turned = Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) > SnapTurnDegrees;
        bool fast = Horizontal(p - last) > MaxSpeed * Mathf.Max(Time.deltaTime, 0.02f);
        if (anchored && (turned || fast)) Anchor(p);
        else Advance(p);
        lastYaw = yaw;
    }

    // Starts a new stride at position (after a jump, or for validation).
    public void Anchor(Vector3 position)
    {
        last = position;
        anchored = true;
        travelled = 0;
    }

    // Walks to position: true when the move completed a stride and played a step. The
    // first call only anchors; a move longer than MaxStep is a jump.
    public bool Advance(Vector3 position)
    {
        if (!anchored) { Anchor(position); return false; }
        float distance = Horizontal(position - last);
        if (distance > MaxStep) { Anchor(position); return false; }
        last = position;
        travelled += distance;
        if (travelled < Stride) return false;
        travelled = Mathf.Repeat(travelled, Stride);
        Step(position);
        return true;
    }

    public bool OnGlass(Vector3 position)
    {
        if (GlassZones == null) return false;
        Vector2 p = new Vector2(position.x, position.z);
        foreach (Rect r in GlassZones)
            if (r.Contains(p)) return true;
        return false;
    }

    private void Step(Vector3 position)
    {
        Steps++;
        LastOnGlass = OnGlass(position);
        bool glass = LastOnGlass && DemoSoundBank.Has(Glass);
        AudioClip clip = glass ? DemoSoundBank.Pick(Glass, ref lastGlass) : DemoSoundBank.Pick(Hard, ref lastHard);
        if (clip == null || Source == null) return;
        Source.pitch = 1 + Random.Range(-PitchJitter, PitchJitter);
        Source.PlayOneShot(clip, (glass ? GlassVolume : Volume) * Random.Range(0.85f, 1f));
    }

    private static float Horizontal(Vector3 d) => new Vector2(d.x, d.z).magnitude;
}
