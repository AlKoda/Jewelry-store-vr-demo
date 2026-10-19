using UnityEngine;

// Recorded clips (Prototype/ThirdParty/NightAudio, and the NightSoundscape takes that
// replace some of them) that replace DemoSounds' generated ones while this component is
// in the scene. The Editor builder (NightAudioBuilder) assigns the AudioClip assets, so
// they ship with the scene in a player build; nothing is loaded from disk at runtime. An
// empty slot keeps the generated sound (or silence for the sounds without one). Volumes
// are the packs' suggested starting levels. The arrays hold numbered variations: cone and
// marker set-downs (DemoSounds.PlaySetDown) and the footsteps the builder hands to the
// player's Footsteps; Pick never plays the same variation twice in a row.
[DefaultExecutionOrder(-1000)]
public sealed class DemoSoundBank : MonoBehaviour
{
    public AudioClip CameraClick, FlashlightSwitch, ToolSetDown, PanelSelect, Ambience;
    [Range(0, 1)] public float CameraClickVolume = 0.35f, FlashlightSwitchVolume = 0.25f,
        ToolSetDownVolume = 0.25f, PanelSelectVolume = 0.2f, AmbienceVolume = 0.15f;
    // AmbientSound's spatial blend for the recorded bed: 0.6 for the mono DistantAir loop,
    // 0 (2D) for a stereo city bed.
    [Range(0, 1)] public float AmbienceSpatialBlend = 0.6f;
    public AudioClip[] ConeSetDown = new AudioClip[0], MarkerSetDown = new AudioClip[0],
        FootstepsHard = new AudioClip[0], FootstepsGlass = new AudioClip[0];
    [Range(0, 1)] public float ConeSetDownVolume = 0.55f, MarkerSetDownVolume = 0.5f;

    // 2D source for panel feedback, created at Awake; outside the night store's reverb
    // zones, like the music (interface clicks are not in the room).
    public AudioSource Interface { get; private set; }

    public bool Any => CameraClick != null || FlashlightSwitch != null || ToolSetDown != null || PanelSelect != null || Ambience != null
        || Has(ConeSetDown) || Has(MarkerSetDown) || Has(FootstepsHard) || Has(FootstepsGlass);

    private int lastCone = -1, lastMarker = -1;

    private void Awake()
    {
        Interface = gameObject.AddComponent<AudioSource>();
        Interface.playOnAwake = false;
        Interface.spatialBlend = 0;
        Interface.bypassReverbZones = true;
        DemoSounds.Use(this);
    }

    private void OnDestroy() { DemoSounds.Release(this); }

    public AudioClip ClipFor(DemoSound sound)
    {
        switch (sound)
        {
            case DemoSound.Shutter: return CameraClick;
            case DemoSound.Switch: return FlashlightSwitch;
            case DemoSound.Thud: return ToolSetDown;
            case DemoSound.Select: return PanelSelect;
            case DemoSound.Ambience: return Ambience;
            default: return null;
        }
    }

    public float VolumeFor(DemoSound sound)
    {
        switch (sound)
        {
            case DemoSound.Shutter: return CameraClickVolume;
            case DemoSound.Switch: return FlashlightSwitchVolume;
            case DemoSound.Thud: return ToolSetDownVolume;
            case DemoSound.Select: return PanelSelectVolume;
            case DemoSound.Ambience: return AmbienceVolume;
            default: return 0.5f;
        }
    }

    // A recorded set-down of the tool's own (a cone or marker variation), or null: the
    // other tools keep the Thud.
    public AudioClip SetDownClip(DemoToolKind kind)
    {
        switch (kind)
        {
            case DemoToolKind.Cone: return Pick(ConeSetDown, ref lastCone);
            case DemoToolKind.Marker: return Pick(MarkerSetDown, ref lastMarker);
            default: return null;
        }
    }

    public float SetDownVolume(DemoToolKind kind)
        => kind == DemoToolKind.Cone ? ConeSetDownVolume : kind == DemoToolKind.Marker ? MarkerSetDownVolume : ToolSetDownVolume;

    public static bool Has(AudioClip[] clips)
    {
        if (clips == null) return false;
        foreach (AudioClip clip in clips)
            if (clip != null) return true;
        return false;
    }

    // A random index below count that differs from last (the caller's memory of its
    // previous pick, -1 at first), or -1 when count is 0. Shared by the bank, Footsteps
    // and AmbientEvents.
    public static int PickIndex(int count, ref int last)
    {
        if (count <= 0) return -1;
        if (count == 1) { last = 0; return 0; }
        int i;
        if (last < 0 || last >= count) i = Random.Range(0, count);
        else
        {
            i = Random.Range(0, count - 1);
            if (i >= last) i++;
        }
        last = i;
        return i;
    }

    public static AudioClip Pick(AudioClip[] clips, ref int last)
    {
        int i = PickIndex(clips != null ? clips.Length : 0, ref last);
        return i >= 0 ? clips[i] : null;
    }
}
