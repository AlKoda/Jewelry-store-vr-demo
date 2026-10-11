using UnityEngine;

// Recorded clips (Prototype/ThirdParty/NightAudio) that replace DemoSounds' generated
// ones while this component is in the scene. The Editor builder assigns the AudioClip
// assets, so they ship with the scene in a player build; nothing is loaded from disk at
// runtime. An empty slot keeps the generated sound (or silence for the sounds without
// one). Volumes are the pack's suggested starting levels.
[DefaultExecutionOrder(-1000)]
public sealed class DemoSoundBank : MonoBehaviour
{
    public AudioClip CameraClick, FlashlightSwitch, ToolSetDown, PanelSelect, Ambience;
    [Range(0, 1)] public float CameraClickVolume = 0.35f, FlashlightSwitchVolume = 0.25f,
        ToolSetDownVolume = 0.25f, PanelSelectVolume = 0.2f, AmbienceVolume = 0.15f;

    // 2D source for panel feedback, created at Awake.
    public AudioSource Interface { get; private set; }

    public bool Any => CameraClick != null || FlashlightSwitch != null || ToolSetDown != null || PanelSelect != null || Ambience != null;

    private void Awake()
    {
        Interface = gameObject.AddComponent<AudioSource>();
        Interface.playOnAwake = false;
        Interface.spatialBlend = 0;
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
}
