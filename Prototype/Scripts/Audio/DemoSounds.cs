using UnityEngine;

// Shutter, Switch and Select have no generated version: they sound only when a
// DemoSoundBank supplies a recorded clip. New values go at the end (the index is stored).
public enum DemoSound { Click, Thud, Ambience, Shutter, Switch, Select }

// Small generated sounds, so the demo has audio feedback without audio assets:
// a click when a tool is taken, a thud when it is set down, and a quiet
// street-ambience loop. Clips are built once per session. A DemoSoundBank in the
// scene (the night store's, filled from Prototype/ThirdParty/NightAudio and
// NightSoundscape by NightAudioBuilder) replaces them with recorded clips at its own
// volumes; an empty slot keeps the generated sound. Its cone and marker variations
// give those tools their own set-down (PlaySetDown); without them every tool thuds.
public static class DemoSounds
{
    private const int SampleRate = 22050;
    private static readonly AudioClip[] clips = new AudioClip[3];
    private static DemoSoundBank bank;

    public static DemoSoundBank Bank => bank;

    public static void Use(DemoSoundBank soundBank) { bank = soundBank; }
    public static void Release(DemoSoundBank soundBank) { if (bank == soundBank) bank = null; }

    // The bank's recorded clip for the sound, or null.
    public static AudioClip Staged(DemoSound sound) => bank != null ? bank.ClipFor(sound) : null;

    // The recorded clip when a bank supplies one, else the generated clip (null for
    // the sounds that have none).
    public static AudioClip Clip(DemoSound sound)
    {
        AudioClip staged = Staged(sound);
        return staged != null ? staged : Generated(sound);
    }

    public static AudioClip Generated(DemoSound sound)
    {
        int index = (int)sound;
        if (index >= clips.Length) return null;
        if (clips[index] == null) clips[index] = Build(sound);
        return clips[index];
    }

    // volume applies to the generated clip; a recorded one plays at the bank's level.
    public static float Volume(DemoSound sound, float generatedVolume)
        => Staged(sound) != null ? bank.VolumeFor(sound) : generatedVolume;

    public static void Play(DemoSound sound, Vector3 position, float volume = 0.6f)
    {
        AudioClip clip = Clip(sound);
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, Volume(sound, volume));
    }

    // The clip a tool of this kind is set down with: the bank's own cone or marker
    // variation, else the Thud (the bank's ToolSetDown or the generated one).
    public static AudioClip SetDownClip(DemoToolKind kind)
    {
        AudioClip staged = bank != null ? bank.SetDownClip(kind) : null;
        return staged != null ? staged : Clip(DemoSound.Thud);
    }

    // volume applies to the Thud (as in Play); a cone or marker variation plays at the bank's level.
    public static void PlaySetDown(DemoToolKind kind, Vector3 position, float volume)
    {
        AudioClip staged = bank != null ? bank.SetDownClip(kind) : null;
        if (staged != null) AudioSource.PlayClipAtPoint(staged, position, bank.SetDownVolume(kind));
        else Play(DemoSound.Thud, position, volume);
    }

    // Non-spatial feedback (presenter panel buttons) through the bank's 2D source;
    // silent without a bank or a recorded clip.
    public static void PlayInterface(DemoSound sound)
    {
        AudioClip clip = Staged(sound);
        if (clip != null && bank.Interface != null) bank.Interface.PlayOneShot(clip, bank.VolumeFor(sound));
    }

    private static AudioClip Build(DemoSound sound)
    {
        float[] samples;
        switch (sound)
        {
            case DemoSound.Click:
                samples = Tone(0.04f, 2400, 0.4f);
                break;
            case DemoSound.Thud:
                samples = Tone(0.14f, 110, 0.7f);
                break;
            default:
                samples = Noise(4f, 0.12f);
                break;
        }
        AudioClip clip = AudioClip.Create(sound.ToString(), samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Sine burst with a linear fade-out.
    private static float[] Tone(float seconds, float frequency, float amplitude)
    {
        float[] samples = new float[(int)(SampleRate * seconds)];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = Mathf.Sin(i * 2 * Mathf.PI * frequency / SampleRate) * amplitude * (1f - (float)i / samples.Length);
        return samples;
    }

    // Low-passed noise with matched ends so it loops without a click.
    private static float[] Noise(float seconds, float amplitude)
    {
        System.Random random = new System.Random(7);
        float[] samples = new float[(int)(SampleRate * seconds)];
        float value = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            value += ((float)random.NextDouble() * 2 - 1 - value) * 0.04f;
            samples[i] = value * amplitude;
        }
        int fade = SampleRate / 10;
        for (int i = 0; i < fade; i++)
        {
            float t = (float)i / fade;
            samples[i] *= t;
            samples[samples.Length - 1 - i] *= t;
        }
        return samples;
    }
}
