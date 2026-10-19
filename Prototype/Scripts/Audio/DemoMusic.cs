using UnityEngine;

// Music (Prototype/ThirdParty/Music): an optional looping Bed and a one-shot Sting. The
// night store's bed is the investigation underscore, off at arrival with a presenter
// switch (ToggleBed); the launcher's is the menu theme, played at start and faded out
// before a scene loads (FadeOut). A session reset plays the sting and ducks the bed by
// DuckLevel (about 6 dB) for DuckSeconds. Two 2D sources created at Awake, never culled
// (priority 0) and outside the reverb zones, as the pack README asks: music stays 2D in VR
// too. Music never signals a right or wrong action. Fades run on unscaled time, so they
// also finish while a scene is loading. Instance is the scene's one DemoMusic, or null.
public sealed class DemoMusic : MonoBehaviour
{
    public static DemoMusic Instance { get; private set; }

    public AudioClip Bed, Sting;
    [Range(0, 1)] public float BedVolume = 0.18f, StingVolume = 0.5f, DuckLevel = 0.5f;
    public float FadeSeconds = 2.5f, DuckSeconds = 3f;
    public bool PlayBedAtStart;
    // Found in Awake when unassigned; the launcher has none.
    public DemoSession Session;

    public bool BedOn { get; private set; }
    // Stings played since Awake (the reset check reads it; batch mode may have no audio device).
    public int StingsPlayed { get; private set; }
    public AudioSource BedSource { get; private set; }
    public AudioSource StingSource { get; private set; }

    private float level, duck = 1, master = 1, masterTarget = 1, masterRate = 1, duckUntil = -1;

    private void Awake()
    {
        Instance = this;
        BedSource = CreateSource(true);
        BedSource.clip = Bed;
        BedSource.volume = 0;
        StingSource = CreateSource(false);
        if (Session == null) Session = FindFirstObjectByType<DemoSession>();
    }

    private AudioSource CreateSource(bool loop)
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0;
        s.priority = 0;
        s.dopplerLevel = 0;
        s.bypassReverbZones = true;
        return s;
    }

    private void OnEnable() { if (Session != null) Session.SessionReset.AddListener(PlaySting); }
    private void OnDisable() { if (Session != null) Session.SessionReset.RemoveListener(PlaySting); }

    private void Start() { if (PlayBedAtStart) SetBed(true); }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        level = Mathf.MoveTowards(level, BedOn ? 1 : 0, FadeSeconds > 0 ? dt / FadeSeconds : 1);
        duck = Mathf.MoveTowards(duck, Time.unscaledTime < duckUntil ? DuckLevel : 1, dt / 0.3f);
        master = Mathf.MoveTowards(master, masterTarget, masterRate * dt);
        BedSource.volume = BedVolume * level * duck * master;
        StingSource.volume = master;
        if (!BedOn && level <= 0 && BedSource.isPlaying) BedSource.Stop();
    }

    // Fades the bed in (from the top of the loop when it was stopped) or out; no bed, no music.
    public void SetBed(bool on)
    {
        BedOn = on && Bed != null;
        if (!BedOn || BedSource == null) return;
        if (BedSource.clip != Bed) BedSource.clip = Bed;
        if (!BedSource.isPlaying) BedSource.Play();
    }

    // The presenter switch; the confirmation shows on the desktop only.
    public void ToggleBed()
    {
        SetBed(!BedOn);
        DemoFeedback.Post(BedOn ? "Music on" : "Music off", FeedbackTone.Info, true);
    }

    public void PlaySting()
    {
        if (Sting == null || StingSource == null) return;
        StingSource.PlayOneShot(Sting, StingVolume);
        StingsPlayed++;
        duckUntil = Time.unscaledTime + DuckSeconds;
    }

    // Fades everything out over seconds (before leaving the scene); the bed keeps its switch.
    public void FadeOut(float seconds)
    {
        masterTarget = 0;
        masterRate = seconds > 0 ? 1 / seconds : 1e6f;
    }
}
