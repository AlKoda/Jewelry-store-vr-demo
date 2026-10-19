using UnityEngine;

// Quiet street ambience looping from the storefront: the generated loop, or the
// recorded one (at the bank's level and spatial blend: 2D for the night store's stereo
// city bed) when a DemoSoundBank supplies it. In the night store AmbientZones then
// starts it at a random time and muffles it inside the shop.
[RequireComponent(typeof(AudioSource))]
public sealed class AmbientSound : MonoBehaviour
{
    [Range(0, 1)] public float Volume = 0.25f;

    private void Start()
    {
        AudioSource source = GetComponent<AudioSource>();
        bool staged = DemoSounds.Staged(DemoSound.Ambience) != null;
        source.clip = DemoSounds.Clip(DemoSound.Ambience);
        source.loop = true;
        source.spatialBlend = staged ? DemoSounds.Bank.AmbienceSpatialBlend : 0.6f;
        source.minDistance = 3;
        source.maxDistance = 25;
        if (staged) source.dopplerLevel = 0;
        source.volume = DemoSounds.Volume(DemoSound.Ambience, Volume);
        source.Play();
    }
}
