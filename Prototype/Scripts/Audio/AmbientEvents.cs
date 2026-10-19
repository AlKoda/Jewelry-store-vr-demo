using UnityEngine;

// Random one-shots of the night street: passing cars and a distant dog, plus sirens and
// radio traffic when PoliceLook's preset has them (the default has neither). Each group
// plays a random clip of its own (never the same twice in a row, pitch within
// PitchJitter, level 85-100 % of Volume times the clip's gain) through its own source,
// MinSeconds to MaxSeconds after the previous one has ended; the first one comes that
// long after Start. A 2D group is a distant event with its distance baked in; a Spatial
// one plays at Position (this object's space) with a logarithmic rolloff to MaxDistance.
// The builder lists the sources among AmbientZones' exterior beds, so inside the shop
// they are muffled like the street.
public sealed class AmbientEvents : MonoBehaviour
{
    [System.Serializable]
    public class Group
    {
        public string Name;
        public AudioClip[] Clips = new AudioClip[0];
        // Per clip, relative to Volume (1 where missing): takes of different loudness in one group.
        public float[] ClipGains = new float[0];
        public float MinSeconds = 20, MaxSeconds = 60;
        [Range(0, 1)] public float Volume = 0.2f;
        public float PitchJitter = 0.04f;
        public bool Spatial;
        public Vector3 Position;
        public float MaxDistance = 30;
        // The group's own source, a child of this object (EnsureSource).
        public AudioSource Source;
    }

    public Group[] Groups = new Group[0];

    // Events played since Start, over all groups.
    public int Played { get; private set; }

    private float[] next = new float[0];
    private int[] last = new int[0];

    private void Awake()
    {
        if (Groups == null) Groups = new Group[0];
        for (int i = 0; i < Groups.Length; i++) EnsureSource(i);
    }

    private void Start() { Schedule(); }

    // Every group's first event, MinSeconds to MaxSeconds from now.
    private void Schedule()
    {
        next = new float[Groups.Length];
        last = new int[Groups.Length];
        for (int i = 0; i < Groups.Length; i++)
        {
            last[i] = -1;
            next[i] = Time.time + (Groups[i] != null ? Gap(Groups[i]) : 60);
        }
    }

    private void Update()
    {
        float now = Time.time;
        for (int i = 0; i < next.Length; i++)
            if (now >= next[i]) Fire(i);
    }

    // When group i plays next (Time.time), or -1 before Start or for an unknown group.
    public float NextTime(int i) => i >= 0 && i < next.Length ? next[i] : -1;

    public int IndexOf(string name)
    {
        for (int i = 0; i < Groups.Length; i++)
            if (Groups[i] != null && Groups[i].Name == name) return i;
        return -1;
    }

    // Group i's source (made as a child named after the group when missing), set up from
    // the group; the builder calls it in the editor, so the sources are serialized.
    public AudioSource EnsureSource(int i)
    {
        Group g = Groups[i];
        if (g == null) return null;
        if (g.Source == null)
        {
            GameObject holder = new GameObject(string.IsNullOrEmpty(g.Name) ? "Event_" + i : g.Name);
            holder.transform.SetParent(transform, false);
            g.Source = holder.AddComponent<AudioSource>();
        }
        AudioSource s = g.Source;
        s.transform.localPosition = g.Spatial ? g.Position : Vector3.zero;
        s.playOnAwake = false;
        s.loop = false;
        s.spatialBlend = g.Spatial ? 1 : 0;
        s.rolloffMode = AudioRolloffMode.Logarithmic;
        s.minDistance = 1;
        s.maxDistance = Mathf.Max(1.5f, g.MaxDistance);
        s.dopplerLevel = 0;
        s.volume = g.Volume;
        return s;
    }

    // Plays group i now and schedules the next one after this clip has ended.
    public void Fire(int i)
    {
        if (next.Length != Groups.Length) Schedule();
        Group g = Groups[i];
        float now = Time.time;
        int pick = g != null ? DemoSoundBank.PickIndex(g.Clips != null ? g.Clips.Length : 0, ref last[i]) : -1;
        AudioClip clip = pick >= 0 ? g.Clips[pick] : null;
        float length = 0;
        if (clip != null && g.Source != null)
        {
            float pitch = 1 + Random.Range(-g.PitchJitter, g.PitchJitter);
            float gain = g.ClipGains != null && pick < g.ClipGains.Length ? g.ClipGains[pick] : 1;
            g.Source.pitch = pitch;
            g.Source.PlayOneShot(clip, gain * Random.Range(0.85f, 1f));
            length = clip.length / Mathf.Max(0.5f, pitch);
            Played++;
        }
        next[i] = now + length + (g != null ? Gap(g) : 60);
    }

    private static float Gap(Group g) => Random.Range(Mathf.Max(0.5f, g.MinSeconds), Mathf.Max(g.MinSeconds, g.MaxSeconds));
}
