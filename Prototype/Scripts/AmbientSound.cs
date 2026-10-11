using UnityEngine;

// Quiet street ambience looping from the storefront: the generated loop, or the
// recorded one (at the bank's level) when a DemoSoundBank supplies it.
[RequireComponent(typeof(AudioSource))]
public sealed class AmbientSound : MonoBehaviour
{
    [Range(0, 1)] public float Volume = 0.25f;

    private void Start()
    {
        AudioSource source = GetComponent<AudioSource>();
        source.clip = DemoSounds.Clip(DemoSound.Ambience);
        source.loop = true;
        source.spatialBlend = 0.6f;
        source.minDistance = 3;
        source.maxDistance = 25;
        if (DemoSounds.Staged(DemoSound.Ambience) != null) source.dopplerLevel = 0;
        source.volume = DemoSounds.Volume(DemoSound.Ambience, Volume);
        source.Play();
    }
}
