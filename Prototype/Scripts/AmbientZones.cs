using UnityEngine;

// The night store's beds, blended by where the listener stands. Inside one of the Inside
// rects (world x/z, Rect y = world z, as NightStoreLayout.RegionRect) the Exterior sources
// (the street, the car, the alarm bell, the street events) are low-passed to InsideCutoff
// and turned down to InsideExteriorVolume, as heard through the shopfront, and the
// Interior beds (the room tone) come up; on the pavement the street is full range and the
// room tone silent. The blend takes Smoothing seconds, on a log-frequency scale.
//
// Start records each source's base volume, gives each exterior source an
// AudioLowPassFilter (switched off while it would be transparent, its distance curve held
// flat at the cutoff) and starts every looped bed at a random time once its clip has
// loaded, so the layers never line up. It runs after AmbientSound (order 50), whose source
// is one of the exterior beds. A source on an inactive object (a presenter set's) is
// blended too and plays from its own playOnAwake.
[DefaultExecutionOrder(50)]
public sealed class AmbientZones : MonoBehaviour
{
    public AudioSource[] Exterior = new AudioSource[0], Interior = new AudioSource[0];
    public Rect[] Inside = new Rect[0];
    public float InsideCutoff = 1500, OutsideCutoff = 22000;
    [Range(0, 1)] public float InsideExteriorVolume = 0.55f;
    public float Smoothing = 1.5f;

    // 0 on the street, 1 inside; the cutoff the exterior sources have now.
    public float Blend { get; private set; }
    public float Cutoff { get; private set; }

    private float[] exteriorBase, interiorBase;
    private AudioLowPassFilter[] filters;
    // A filter beside an AudioSource has a distance curve (the source's Low-Pass curve in the
    // 3D graph, the cutoff as a share of CurveHz) that Unity may evaluate by listener distance
    // and write over cutoffFrequency; one key at Cutoff keeps it flat at the zones' setting.
    // One curve serves every filter (the setter copies it), so a blend allocates nothing.
    private AnimationCurve flatCurve;
    private const float CurveHz = 22000;
    // Random start offset per source (exterior then interior; -1 for no looped bed), and
    // which ones still wait for their clip to load.
    private float[] offsets;
    private bool[] pending;
    private bool prepared, applied;
    private float appliedBlend, giveUpAt;
    private const float LoadTimeout = 5;
    private AudioListener listener;
    private float nextSearch;

    private void Start()
    {
        Prepare();
        giveUpAt = Time.unscaledTime + LoadTimeout;
        for (int i = 0; i < offsets.Length; i++)
        {
            AudioSource s = SourceAt(i);
            if (s == null || s.clip == null || !s.loop || !s.isActiveAndEnabled) continue;
            offsets[i] = Random.Range(0, s.clip.length * 0.99f);
            // AmbientSound has already started its bed from 0; it restarts at the offset.
            s.Stop();
            pending[i] = true;
            StartWhenLoaded(i);
        }
        // The arrival takes the listener's zone at once rather than fading into it.
        if (FindListener()) Step(Mathf.Max(Smoothing, 0.01f), listener.transform.position);
    }

    private void Update()
    {
        for (int i = 0; i < pending.Length; i++)
            if (pending[i]) StartWhenLoaded(i);
        if (FindListener()) Step(Time.deltaTime, listener.transform.position);
    }

    // Records the base volumes and adds the filters, once (Start, or a validator's first Step).
    private void Prepare()
    {
        if (prepared) return;
        prepared = true;
        if (Exterior == null) Exterior = new AudioSource[0];
        if (Interior == null) Interior = new AudioSource[0];
        exteriorBase = new float[Exterior.Length];
        interiorBase = new float[Interior.Length];
        filters = new AudioLowPassFilter[Exterior.Length];
        offsets = new float[Exterior.Length + Interior.Length];
        pending = new bool[offsets.Length];
        for (int i = 0; i < offsets.Length; i++) offsets[i] = -1;
        flatCurve = new AnimationCurve(new Keyframe(0, 1));
        FlattenCurve(OutsideCutoff);
        for (int i = 0; i < Exterior.Length; i++)
        {
            AudioSource s = Exterior[i];
            if (s == null) continue;
            exteriorBase[i] = s.volume;
            AudioLowPassFilter filter = s.GetComponent<AudioLowPassFilter>();
            if (filter == null) filter = s.gameObject.AddComponent<AudioLowPassFilter>();
            filter.lowpassResonanceQ = 1;
            filter.customCutoffCurve = flatCurve;
            filter.cutoffFrequency = OutsideCutoff;
            filter.enabled = false;
            filters[i] = filter;
        }
        for (int i = 0; i < Interior.Length; i++)
            if (Interior[i] != null) interiorBase[i] = Interior[i].volume;
        Cutoff = OutsideCutoff;
    }

    private AudioSource SourceAt(int i) => i < Exterior.Length ? Exterior[i] : Interior[i - Exterior.Length];

    // Plays the bed from its offset once the clip's data is in (a clip imported with
    // Load In Background may still be loading at Start). A clip that cannot load, or is
    // still loading after LoadTimeout seconds, plays from where Unity starts it.
    private void StartWhenLoaded(int i)
    {
        AudioSource s = SourceAt(i);
        if (s == null || s.clip == null || !s.isActiveAndEnabled) { pending[i] = false; return; }
        AudioClip clip = s.clip;
        bool loadable = clip.loadState != AudioDataLoadState.Unloaded || clip.LoadAudioData();
        bool loading = clip.loadState != AudioDataLoadState.Loaded && clip.loadState != AudioDataLoadState.Failed;
        if (loadable && loading && Time.unscaledTime < giveUpAt) return;
        pending[i] = false;
        s.Play();
        if (clip.loadState == AudioDataLoadState.Loaded) s.time = offsets[i];
    }

    // The random start offset chosen for a source in either list, or -1.
    public float StartOffset(AudioSource source)
    {
        if (!prepared || source == null) return -1;
        for (int i = 0; i < offsets.Length; i++)
            if (SourceAt(i) == source) return offsets[i];
        return -1;
    }

    // The volume a source had at Start (before any blend), or -1 when it is in neither list.
    public float BaseVolume(AudioSource source)
    {
        Prepare();
        for (int i = 0; i < Exterior.Length; i++)
            if (Exterior[i] == source) return exteriorBase[i];
        for (int i = 0; i < Interior.Length; i++)
            if (Interior[i] == source) return interiorBase[i];
        return -1;
    }

    public bool IsInside(Vector3 position)
    {
        Vector2 p = new Vector2(position.x, position.z);
        foreach (Rect r in Inside)
            if (r.Contains(p)) return true;
        return false;
    }

    // Moves the blend deltaTime seconds towards the listener's zone and applies it.
    public void Step(float deltaTime, Vector3 listenerPosition)
    {
        Prepare();
        float target = IsInside(listenerPosition) ? 1 : 0;
        Blend = Smoothing > 0 ? Mathf.MoveTowards(Blend, target, deltaTime / Smoothing) : target;
        if (applied && Blend == appliedBlend) return;
        applied = true;
        appliedBlend = Blend;
        Cutoff = Mathf.Exp(Mathf.Lerp(Mathf.Log(Mathf.Max(10, OutsideCutoff)), Mathf.Log(Mathf.Max(10, InsideCutoff)), Blend));
        float outside = Mathf.Lerp(1, InsideExteriorVolume, Blend);
        FlattenCurve(Cutoff);
        for (int i = 0; i < Exterior.Length; i++)
        {
            if (Exterior[i] == null) continue;
            Exterior[i].volume = exteriorBase[i] * outside;
            AudioLowPassFilter filter = filters[i];
            if (filter == null) continue;
            filter.customCutoffCurve = flatCurve;
            filter.cutoffFrequency = Cutoff;
            filter.enabled = Blend > 0.001f;
        }
        for (int i = 0; i < Interior.Length; i++)
            if (Interior[i] != null) Interior[i].volume = interiorBase[i] * Blend;
    }

    // Puts the shared curve's one key at hz (as a share of CurveHz, at every distance).
    private void FlattenCurve(float hz) => flatCurve.MoveKey(0, new Keyframe(0, Mathf.Clamp01(hz / CurveHz)));

    // The scene's listener (the head camera's), looked up at most once a second while missing.
    private bool FindListener()
    {
        if (listener != null && listener.isActiveAndEnabled) return true;
        if (Time.unscaledTime < nextSearch) return false;
        nextSearch = Time.unscaledTime + 1;
        listener = FindFirstObjectByType<AudioListener>();
        return listener != null && listener.isActiveAndEnabled;
    }
}
