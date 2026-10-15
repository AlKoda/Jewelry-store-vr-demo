# Music: start-menu theme, investigation underscore and reset sting

Music for the night jewellery-store demo. Prepared 2026-10-14. **Not imported, wired or auditioned in Unity or on headphones yet (unverified).**

- Three looping beds cut from tracks by **John Bartmann** (CC0 1.0, album *100 Ambient Atmospheric Soundtracks: Straylight Drones Collection*). These are human-produced cinematic drone/pad pieces with no drums and no melodic hook, so they don't get annoying on repeat. Each file is exactly one musical period and loops seamlessly as a whole file.
- One **original** sting (`ResetSting.ogg`), synthesized for the project and owned by it.
- Every file is 44.1 kHz stereo OGG Vorbis (quality 6). Static gain only, with no limiter or compressor.
- Total size **4.8 MB** (about 4,785,000 bytes, everything in this folder including `Source~`; 4.6 MiB by `du -sh`). The largest file is 2.4 MB.

LICENSE.txt has the licence evidence and a suggested voluntary credit line. sources.json has the source URLs, mirror commit, SHA-256 of each source and shipped file, loop points and loudness. `Source~/` is ignored by Unity and holds the scripts that rebuild every file (see the end of this README).

## Files

| File | What it is | Length | Loop / one-shot | Loudness (integrated, true peak) | Suggested use in the night store | Start volume |
|---|---|---|---|---|---|---|
| **MenuTheme.ogg** | "profound-mystery" (John Bartmann). Slow progression of eight sustained chords, one every 15 s, around F/C/B♭. Low granular string-like pads under long high tones. Calm and cinematic with a hint of mystery. No drums. | 2:00.000 (5,292,000 samples) | **Loop**. The file starts on a chord change; the 6 s crossfade sits inside the file, not at the wrap. | −18.0 LUFS, −5.4 dBTP | Start menu / launcher (DemoLauncher) and the instructor's pre-session screen. Fade in over 2–3 s and out over 1.5 s when the scene loads. | 0.5 |
| **InvestigationLoop.ogg** | "a-wall-of-glass" (John Bartmann). Dark, static drone on C, F♯ and G♯ (an uneasy tritone colour). Soft glassy shimmer swells about every 20 s and one slow low "breath" per cycle. No melody, no rhythm. | 1:44.000 (4,586,400 samples) | **Loop**, sample-exact. The source repeats itself every 104 s, so there is no crossfade. | −20.0 LUFS, −8.1 dBTP, LRA 11 LU | Optional in-scene underscore inside the store. Off by default, with an instructor toggle. Keep it under the street ambience and well under the instructor's voice. | 0.15–0.2 |
| **InvestigationLoop_Calm.ogg** | "background-scan" (John Bartmann). Airy, wide granular texture centred on G♯/E with slow "scanning" swells. More neutral and less ominous than InvestigationLoop. | 2:08.000 (5,644,800 samples) | **Loop**, sample-exact (the source repeats every 128 s). | −20.0 LUFS, −2.4 dBTP, LRA 7 LU | Alternative to InvestigationLoop if the instructor finds the tritone too dark, or for the review/debrief screen. Use only one of the two at a time. | 0.15–0.2 |
| **ResetSting.ogg** | Original sting: a soft sub thud on F1, a strummed felt-piano open-fifth chord (F2 C3 G3 C4 G4 C5, Fsus2), a detuned pad that blooms and closes, faint glass partials on C6, a short air swell and a 3 s synthetic hall reverb. Neutral (neither major nor minor) and cinematic. | 5.6 s; the hit lands at 0.06 s and the tail is faded out by 5.6 s | **One-shot** | −13.0 LUFS (short-file measure), −1.3 dBTP | Plays on DemoSession.SessionReset ("session reset") and/or when a session starts from the launcher. Duck the music bed by about 6 dB for 3 s while it plays. | 0.5 |

Musical keys, if you ever layer them: MenuTheme sits around F major/modal, and ResetSting is Fsus2, so the sting fits over the menu. InvestigationLoop (C/F♯/G♯) and _Calm (G♯/E) are tonally static drones. Crossfade them 3–4 s against the menu rather than cutting.

## Unity import settings (Unity 6000.6.2f1, Built-in, no packages)

| Setting | Beds (MenuTheme, InvestigationLoop, InvestigationLoop_Calm) | ResetSting |
|---|---|---|
| Force To Mono | Off (stereo is part of the sound; the mono sum loses only 0.3 / 1.3 / 3.0 dB) | Off |
| Load In Background | On | Off |
| Load Type | **Streaming** (or Compressed In Memory) | **Decompress On Load** |
| Preload Audio Data | Off for Streaming | On |
| Compression Format | Vorbis, Quality 70 | Vorbis, Quality 70 (or PCM; it is only 5.6 s) |
| Sample Rate Setting | Preserve Sample Rate | Preserve Sample Rate |

AudioSource for music: **spatialBlend = 0 (2D)**, `loop = true` for the beds and false for the sting, dopplerLevel 0, priority 0–32 (music should never be culled), `bypassReverbZones = true`, playOnAwake false (start from code with a fade). Music must stay 2D in VR as well. Do not attach it to the head or to world objects.

Seamless looping: the beds are built so that the last sample flows straight into the first. This was checked with a spectral-change and sample-step metric on the WAV and on the decoded OGG (the wrap step is within the normal step-to-step range). Unity re-encodes on import, so **check the wrap by ear in the Unity player once**. If a click appears at the wrap with Vorbis Streaming, try Compressed In Memory or ADPCM for that clip.

## Wiring ideas (nothing is wired yet)

- Follow the DemoSoundBank pattern (clips assigned by the Editor builder, nothing loaded from disk at runtime). Add `MenuTheme`, `InvestigationLoop` and `ResetSting` slots with volumes, either to DemoSoundBank or to a small `DemoMusic` component that owns one 2D looping source plus one 2D one-shot source. Empty slots mean silence; there is no generated fallback music.
- DemoLauncher: play MenuTheme with a 2.5 s fade-in and fade it out when a scene loads.
- Night store: InvestigationLoop is **optional**, off by default. Expose a toggle and a volume next to the existing mute/volume controls in the instructor panel (DemoDesktopPanel settings). Never make music signal right or wrong actions.
- DemoSession.SessionReset: play ResetSting through a 2D source and duck the bed by about 6 dB for 3 s.
- Validation (per CLAUDE.md): add a DemoValidation/NightStoreValidation check that the clips are assigned, `loop` is set on the bed source, `spatialBlend == 0`, and the clip lengths are 120.0 / 104.0 / 128.0 / 5.6 s (±0.01 s).

## Rebuilding (Source~/)

- `build_loops.py <source_dir> <out_dir>` takes the three source MP3s from the CC0 mirror (the command is in its header; SHA-256 values are in sources.json). It finds the exact repeat period by cross-correlation, cuts the loop on a 320-sample grid (an integer length at both 48 and 44.1 kHz), crossfades only where the repeat is not sample-exact, resamples circularly with soxr, applies static gain to the target loudness and encodes OGG q6. It writes `loops_report.json` with the loop points, gains and seam metrics.
- `compose_sting.py <out.wav>` synthesizes the sting with numpy only (seed 14102026). Encode it with `ffmpeg -i ResetSting.wav -c:a libvorbis -q:a 6 ResetSting.ogg`.
- `audio_util.py` holds the shared helpers. All scripts need numpy and ffmpeg (with libvorbis and soxr). Run them with `python3 -I` from a copy outside the Unity project.
