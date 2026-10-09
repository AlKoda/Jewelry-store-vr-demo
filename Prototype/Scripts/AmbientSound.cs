using UnityEngine;

// Quiet generated street ambience looping from the storefront.
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
        source.volume = Volume;
        source.Play();
    }
}
