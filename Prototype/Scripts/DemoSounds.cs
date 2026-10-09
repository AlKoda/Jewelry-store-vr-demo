using UnityEngine;

public enum DemoSound { Click, Thud, Ambience }

// Small generated sounds, so the demo has audio feedback without audio assets:
// a click when a tool is taken, a thud when it is set down, and a quiet
// street-ambience loop. Clips are built once per session.
public static class DemoSounds
{
    private const int SampleRate = 22050;
    private static readonly AudioClip[] clips = new AudioClip[3];

    public static AudioClip Clip(DemoSound sound)
    {
        int index = (int)sound;
        if (clips[index] == null) clips[index] = Build(sound);
        return clips[index];
    }

    public static void Play(DemoSound sound, Vector3 position, float volume = 0.6f)
    {
        AudioClip clip = Clip(sound);
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume);
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
