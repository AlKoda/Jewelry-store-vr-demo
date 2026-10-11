# Prepared audio

Four CC0 interaction clips and one original quiet synthetic ambience loop.
No audio has been wired into the scene or auditioned in Unity/headphones in this batch.

| File | Proposed use | Initial source volume |
|---|---|---:|
| CameraClick.wav | Generic shutter feedback | 0.35 |
| FlashlightSwitch.wav | Torch switch | 0.25 |
| ToolSetDown.wav | Generic tool contact, not a material-specific impact | 0.25 |
| PanelSelect.wav | Instructor menu selection | 0.2 |
| DistantAir_Loop.wav | Optional neutral exterior background, 16-second loop | 0.15 |

All WAVs are mono PCM16; short clips 44.1 kHz, ambience 24 kHz. PCM / Decompress
On Load is inexpensive for this small selection. Keep Preload Audio Data on for
one-shots. Audition at low volume and adjust against the instructor's voice.
One-shot loop off. Spatial blend 1 for placed world tools with short distance
rolloff; panel click 2D. Bed loop on; one exterior source only, no Doppler, start
with a restrained radius. Check the wrap in the actual Unity audio path.

DemoSounds currently generates interaction sounds and ambience; EvidenceCamera
generates its own shutter click. AmbientSound.Start assigns DemoSounds.Ambience,
so dragging a clip onto that AudioSource alone is overwritten at runtime.
Integration needs an explicit optional clip reference with the existing generated
sound as fallback. Avoid double-playing both old and new shutter/tool sounds.
Keep mute/volume available to the instructor. Nothing here signifies a right or
wrong action, advances a timer, or adds sirens, dialogue or scenario conclusions.

Originals/ is provenance/reproduction material; use the prepared WAVs in runtime.
