# NightSoundscape

Sound for the night burglary scene: exterior city/wind/rain beds, the burglar alarm bell, interior machine room tones, the parked police car (idle, radio traffic, doors), distant sirens, cars and dogs, footsteps on tile and on broken glass, glass debris, and the handling sounds of the investigator's tools (markers, cones, barrier tape, camera, torch, keys, kit-bag zips). 86 clips, 31.6 MB. Prepared October 14, 2026, revised October 15, 2026 after review; **not wired into the scene and not auditioned in Unity or on headphones** (unverified).

It complements Prototype/ThirdParty/NightAudio (CameraClick, FlashlightSwitch, PanelSelect, ToolSetDown, DistantAir_Loop): CameraShutterDSLR_0x and FlashlightClick_0x are realistic alternatives, not duplicates; keep only one set active per action.

## Revision October 15, 2026

- **PoliceCarIdle_Loop.wav** replaced: the first loop (seth-m 'car idle') was 82 % sub-bass below 40 Hz and almost inaudible on Quest 2 speakers. The new 14.4 s loop is a Ford 5.4 V8 idling at about 1000 rpm (Therancher01, CC0, via Jxstrr0/350Chev) with a 45 Hz 4th-order high-pass; it measures -20 LUFS full range and -20.9 LUFS through a 120 Hz high-pass (small-speaker proxy).
- **RadioChatter_01..05.wav and PoliceCarRadio_Loop.ogg** rebuilt with a source-filter speech synthesizer (Generator/speech_babble.py): 4-6 syllables/s, stop gaps and bursts, fricatives, pitch accents and phrase pauses, then a narrow-band FM radio chain. Measured: envelope modulation peak 2.4-5.8 Hz, envelope range (P90-P10 of 10 ms frames) 22-30 dB, 1-3 kHz within 3 dB of 250 Hz-1 kHz (first version: 0.3-1.1 Hz, 9-18 dB, -2 to -12 dB). RadioSquelch and RadioBeep are bit-identical to the first release.
- **FootstepGlass_01..05.wav** remixed: glass layers 8 dB lower with their rare large spikes soft-limited, so the heel thud (60 Hz-1.5 kHz) is 2-6 dB above the crunch (2-20 kHz) and 55-77 % of the energy is below 1 kHz (first version 5-10 %).
- **DogBarkDistant_01..03.wav** are now recorded as CC BY 3.0 (attribution required) because the licence evidence for Kinoton's recording conflicts; see Licences below.
- Every other file is unchanged.

## Licences and attribution

All recordings are CC0 1.0 except **DogBarkDistant_01..03.wav**, which are treated as **CC BY 3.0** until the Freesound page is checked. Put this line in the in-app credits and keep it in Docs/ASSET_REGISTER.md:

> "Dog Barking in Neighborhood" by Kinoton (https://freesound.org/s/516904/), licensed CC BY 3.0 (https://creativecommons.org/licenses/by/3.0/); trimmed, filtered and normalised.

If the attribution cannot be shown, use DogBarkDistant_04.wav (rvandemark, CC0) alone. The synthesized files are project-owned. Full provenance is in LICENSE.txt and sources.json.

## What is recorded and what is synthesized

- **Recorded (Freesound field recordings via GitHub mirrors, CC0 except the CC BY 3.0 dog clips DogBarkDistant_01..03, plus Kenney RPG Audio, CC0):** city beds, wind, rain, recorded HVAC room tone, car idle, car doors, car pass-bys, sirens (two of them re-spatialised here with a moving-source distance model), dogs, hard footsteps, interior door creaks/open/close.
- **Original synthesis (project-owned, reproducible with Generator/):** the alarm bell (physical modal model whose mode ratios were measured from a CC0 bell recording), HVAC vent, fluorescent buzz, fridge, all police-radio sounds (formant-synthesized speech-like babble without words), glass shards/crunch, the glass layer of FootstepGlass, marker, cone, tape, camera shutter, torch click, keys and zips.
- Every licence and source is in LICENSE.txt; per-file hashes, cut points and processing in sources.json.

## Format and import notes (Unity 6000.6, Built-in, no packages)

- All files are 44.1 kHz. One-shots are mono 16-bit PCM WAV normalised to about -3 dBFS sample peak (radio beeps -6 dBFS). Loops are seamless and normalised to -20 LUFS integrated; stereo loops and loops longer than 30 s are Ogg Vorbis (q5-q6), the remaining mono loops (12-24 s) are WAV. Integrated LUFS of clips under 0.4 s reads -70 (gating) and is meaningless.
- Suggested import: one-shots < 2 s: Load Type *Decompress On Load*, Compression *PCM* or *ADPCM*, Preload on. Loops and long one-shots (sirens, dog sequence): *Compressed In Memory*, *Vorbis* quality 60-70; the 60 s beds may use *Streaming*. Force To Mono for every 3D source (the stereo beds are 2D only). Leave Ambisonic off.
- Loop clips: set AudioSource.loop = true and start each at a random time (`source.time = Random.Range(0, clip.length)`) so layers never line up. The loops were rendered/cross-faded to wrap without a click; check the wrap once in Unity because Vorbis re-encoding can change the boundary slightly.
- One-shots: play through a small pool of AudioSources with PlayOneShot, random pick among the numbered variations (never the same twice in a row), pitch 0.94-1.06 and volume 0.85-1.0 per play.
- 3D settings that suit a 16 x 12 m shop: Spatial Blend 1, Logarithmic rolloff, min distance 0.5-2 m (2 m for the bell), max distance 15-30 m (60 m for the bell), Doppler 0 for static emitters. Distant events (sirens, dogs, far cars) are best as 2D with Spatial Blend 0-0.3 and baked distance character.
- Mains hum in the synthesized machine loops is 50 Hz (UK/EU); regenerate with `MAINS=60` for 60 Hz regions.

## Suggested mix for the night store

| Zone | Always-on layers (volume) | Random events |
|---|---|---|
| Street / pavement | NightCity_Loop (0.45), StreetWind_Loop (0.35), AlarmBell_Street_Loop (0.6, crossfade to the 3D AlarmBell_Close_Loop on the facade by distance), PoliceCarIdle_Loop and PoliceCarRadio_Loop at the car, BarrierTapeFlutter_Loop on each tape span, optional LightRainStreet_Loop | Siren one-shots every 2-6 min, CarPass/CarPassWet every 20-60 s, DogBarkDistant every 1-4 min |
| Shop floor | NightCity_Loop low-passed (~1.5 kHz, 0.25), AlarmBell_Interior_Loop (0.6), HVACRoomTone_Loop (0.25) or HVACVent_Loop at the vents, WindWhistleGap_Loop at the forced doors (0.2) | same events, quieter and low-passed; GlassShardsTinkle rarely near broken bays |
| Rear office / safe room | HVACRoomTone_Loop, FluorescentBuzz_Loop at the fitting, FridgeCompressor_Loop in the kitchenette, AlarmBell_Interior_Loop (0.35) | KeysJingle, DoorCreak/Open/Close |

Interaction mapping: footsteps FootstepHard (tile, pavement) or FootstepGlass (inside the glass-debris area; the step carries the footfall, so GlassCrunch at about -6 dB on every other step is optional extra debris); marker placed EvidenceMarkerPlace; cone ConePlace; barrier tape TapeUnroll while dragging and TapeStretchSnap when a span is finished; photo CameraShutterDSLR; torch/UV torch FlashlightClick_01/_02; kit bag ZipperOpen/Close; scene lamp set-down can reuse ConePlace at 0.6 volume or NightAudio/ToolSetDown.

The alarm bell is the key atmosphere element: it is designed to ring continuously; a training scenario might let the instructor silence it (fade over 0.3 s, then a short metallic decay is natural). Nothing here signals a right or wrong action.

## Exterior beds (loops)

| File | Type | Length | Format | Level | Origin | What it is | Suggested placement | Volume | 2D/3D |
|---|---|---|---|---|---|---|---|---|---|
| NightCity_Loop.ogg | loop | 60.00 s | OGG stereo | -20 LUFS | recorded | Night city bed: semi-distant downtown traffic hum at 11 pm heard from a 4th-floor window, occasional far car. | 2D/stereo street bed outside and (low-passed by the shopfront) inside; main exterior ambience. | 0.45 | 2D |
| NightCityDistant_Loop.ogg | loop | 60.00 s | OGG stereo | -20 LUFS | recorded | Alternative city bed: distant evening traffic wash from a 5th-floor balcony; steadier, no close events. | 2D bed for the rear office/side rooms or layered very low under NightCity_Loop. | 0.3 | 2D |
| StreetWind_Loop.ogg | loop | 48.00 s | OGG stereo | -20 LUFS | recorded | Quiet night wind: soft broadband wind with slow gusts, no birds or traffic. | 2D exterior layer under the city bed (pavement, kerb, police car). | 0.35 | 2D |
| WindWhistleGap_Loop.ogg | loop | 45.00 s | OGG stereo | -20 LUFS | recorded | Wind howling/whistling through a gap at a window: resonant, moaning. | 3D source (mono-folded by Unity) at the forced entrance doors / broken glass bay; small radius, low volume. | 0.2 | 3D |
| LightRainStreet_Loop.ogg | loop | 29.00 s | OGG stereo | -20 LUFS | recorded | Optional light rain on a city street at night, recorded from an open window. | 2D exterior bed for a wet-night variant; inside the shop play it low and low-passed. | 0.4 | 2D |

## Burglar alarm bell (loops, original)

| File | Type | Length | Format | Level | Origin | What it is | Suggested placement | Volume | 2D/3D |
|---|---|---|---|---|---|---|---|---|---|
| AlarmBell_Close_Loop.wav | loop | 20.00 s | WAV mono | -20 LUFS | original | Burglar alarm bell (6-inch trembler gong in an outdoor bell box), heard a few metres away; nearly dry. | 3D source on the shop facade above the fascia, min distance 2 m, max 60 m, Logarithmic rolloff; the key atmosphere element. | 0.8 | 3D |
| AlarmBell_Street_Loop.ogg | loop | 20.00 s | OGG stereo | -20 LUFS | original | Same bell heard from across the street: air absorption, facade flutter echoes and street-canyon reverb (stereo). | 2D layer while the player is outside, crossfaded with the 3D close source by distance. | 0.6 | 2D |
| AlarmBell_Interior_Loop.wav | loop | 20.00 s | WAV mono | -20 LUFS | original | Same bell heard inside the shop through the shopfront: muffled (2.6 kHz low-pass) with interior room reverb. | 2D/3D source inside near the entrance while the player is in the shop. | 0.6 | 2D or 3D |

## Interior machines (loops)

| File | Type | Length | Format | Level | Origin | What it is | Suggested placement | Volume | 2D/3D |
|---|---|---|---|---|---|---|---|---|---|
| HVACRoomTone_Loop.ogg | loop | 45.00 s | OGG mono | -20 LUFS | recorded | Recorded air-conditioning unit: steady broadband air rush with a low machine hum. | Interior room tone for the whole shop (2D, low) or a 3D source in the rear office ceiling. | 0.25 | 2D or 3D |
| HVACVent_Loop.wav | loop | 24.00 s | WAV mono | -20 LUFS | original | Ceiling supply diffuser: airflow hiss with turbulence, blower blade-pass tone and faint 100 Hz hum. | 3D sources at 2-3 ceiling vents, min 0.5 m, max 8 m. | 0.3 | 3D |
| FluorescentBuzz_Loop.wav | loop | 16.00 s | WAV mono | -20 LUFS | original | Magnetic-ballast fluorescent fitting buzz (100 Hz and harmonics) with lamination rattle and flicker. | 3D source on the rear-office or side-room fluorescent fittings, min 0.3 m, max 5 m. | 0.25 | 3D |
| FridgeCompressor_Loop.wav | loop | 20.00 s | WAV mono | -20 LUFS | original | Small fridge compressor running: 50/100 Hz motor hum, piston knocks through the shell, refrigerant gurgle. | 3D source in the staff kitchenette/office, min 0.5 m, max 6 m. | 0.3 | 3D |

## Police car and radio

| File | Type | Length | Format | Level | Origin | What it is | Suggested placement | Volume | 2D/3D |
|---|---|---|---|---|---|---|---|---|---|
| PoliceCarIdle_Loop.wav | loop | 14.41 s | WAV mono | -20 LUFS | recorded | V8 petrol engine idling at about 1000 rpm (a police car's raised idle), exterior: broadband exhaust burble and engine mechanics. 14.4 s, seamless, no audible 3.6 s repeat. | 3D source at the police car's bonnet/exhaust, min distance 1 m, max 25 m. | 0.5 | 3D |
| PoliceCarRadio_Loop.ogg | loop | 60.00 s | OGG mono | -20 LUFS | original | 60 s loop of radio traffic: dispatcher calls with officer answers, a few lone overs and kerchunks over a faint hiss (the 5 chatter takes plus 5 loop-only takes). | 3D source inside the parked police car (door open) or on an officer; simplest wiring. | 0.35 | 3D |
| RadioChatter_01.wav | one-shot | 2.12 s | WAV mono | -3 dBFS pk | original | Police-radio transmission: unintelligible synthetic speech (no words, no real voice) at 4-6 syllables/s through a narrow-band FM radio chain, key-up click and squelch tail. _01/_02 dispatcher at a console (male/female), _03-_05 officers in cars (_03/_05 end with a beep). | 3D source at the police car / officer radio; random 5-20 s gaps. | 0.4 | 3D |
| RadioChatter_02.wav | one-shot | 3.63 s | WAV mono | -3 dBFS pk | original | Police-radio transmission: unintelligible synthetic speech (no words, no real voice) at 4-6 syllables/s through a narrow-band FM radio chain, key-up click and squelch tail. _01/_02 dispatcher at a console (male/female), _03-_05 officers in cars (_03/_05 end with a beep). | 3D source at the police car / officer radio; random 5-20 s gaps. | 0.4 | 3D |
| RadioChatter_03.wav | one-shot | 1.97 s | WAV mono | -3 dBFS pk | original | Police-radio transmission: unintelligible synthetic speech (no words, no real voice) at 4-6 syllables/s through a narrow-band FM radio chain, key-up click and squelch tail. _01/_02 dispatcher at a console (male/female), _03-_05 officers in cars (_03/_05 end with a beep). | 3D source at the police car / officer radio; random 5-20 s gaps. | 0.4 | 3D |
| RadioChatter_04.wav | one-shot | 4.36 s | WAV mono | -3 dBFS pk | original | Police-radio transmission: unintelligible synthetic speech (no words, no real voice) at 4-6 syllables/s through a narrow-band FM radio chain, key-up click and squelch tail. _01/_02 dispatcher at a console (male/female), _03-_05 officers in cars (_03/_05 end with a beep). | 3D source at the police car / officer radio; random 5-20 s gaps. | 0.4 | 3D |
| RadioChatter_05.wav | one-shot | 3.38 s | WAV mono | -3 dBFS pk | original | Police-radio transmission: unintelligible synthetic speech (no words, no real voice) at 4-6 syllables/s through a narrow-band FM radio chain, key-up click and squelch tail. _01/_02 dispatcher at a console (male/female), _03-_05 officers in cars (_03/_05 end with a beep). | 3D source at the police car / officer radio; random 5-20 s gaps. | 0.4 | 3D |
| RadioSquelch_01.wav | one-shot | 0.37 s | WAV mono | -3 dBFS pk | original | Radio squelch burst (carrier noise tail) with key click. | Police car / radio, between transmissions. | 0.35 | 3D |
| RadioSquelch_02.wav | one-shot | 0.35 s | WAV mono | -3 dBFS pk | original | Radio squelch burst (carrier noise tail) with key click. | Police car / radio, between transmissions. | 0.35 | 3D |
| RadioSquelch_03.wav | one-shot | 0.29 s | WAV mono | -3 dBFS pk | original | Radio squelch burst (carrier noise tail) with key click. | Police car / radio, between transmissions. | 0.35 | 3D |
| RadioBeep_01.wav | one-shot | 0.19 s | WAV mono | -6 dBFS pk | original | Generic radio tones: _01 rising talk-permit chirp, _02 double acknowledge beep, _03 single long tone (not any manufacturer's tones). | Police car / radio. | 0.3 | 3D |
| RadioBeep_02.wav | one-shot | 0.24 s | WAV mono | -6 dBFS pk | original | Generic radio tones: _01 rising talk-permit chirp, _02 double acknowledge beep, _03 single long tone (not any manufacturer's tones). | Police car / radio. | 0.3 | 3D |
| RadioBeep_03.wav | one-shot | 0.25 s | WAV mono | -6 dBFS pk | original | Generic radio tones: _01 rising talk-permit chirp, _02 double acknowledge beep, _03 single long tone (not any manufacturer's tones). | Police car / radio. | 0.3 | 3D |
| CarDoorOpen_01.wav | one-shot | 2.10 s | WAV mono | -3 dBFS pk | recorded | Car door handle, latch and door opening (recorded in a car park; some reverb). | Police car door, 3D. | 0.6 | 3D |
| CarDoorSlam_01.wav | one-shot | 1.48 s | WAV mono | -3 dBFS pk | recorded | Car door closed firmly (slam) with a short car-park tail. | Police car door, 3D. | 0.7 | 3D |

## Distant city events (one-shots)

| File | Type | Length | Format | Level | Origin | What it is | Suggested placement | Volume | 2D/3D |
|---|---|---|---|---|---|---|---|---|---|
| SirenWailPassDistant_01.wav | one-shot | 26.00 s | WAV mono | -3 dBFS pk | recorded | Police siren (electronic wail) passing two blocks away behind buildings: Doppler, distance, occlusion, street echo. 26 s. | 2D ambient one-shot every 2-6 minutes, random start; or a 3D emitter 150+ m away. | 0.35 | 2D |
| SirenWailYelpPass_02.wav | one-shot | 22.00 s | WAV mono | -3 dBFS pk | recorded | Police siren on a nearer street (~90 m) switching wail -> yelp -> wail as it clears a junction. 22 s. | 2D ambient one-shot; rarer than _01. | 0.35 | 2D |
| SirenDistantCity_03.wav | one-shot | 18.00 s | WAV mono | -3 dBFS pk | recorded | Real distant North American police siren echoing between city buildings. 18 s. | 2D ambient one-shot. | 0.35 | 2D |
| SirenDistantPass_04.wav | one-shot | 15.00 s | WAV mono | -3 dBFS pk | recorded | Police siren wail passing at a distance with fade-out (32 kHz band-limited source). 15 s. Dense tone: about 8 dB louder than the other sirens at the same peak. | 2D ambient one-shot. | 0.15 | 2D |
| CarPassWet_01.wav | one-shot | 4.79 s | WAV mono | -3 dBFS pk | recorded | Single car passing on a wet road (tyre spray). | 3D on the street lane or 2D, every 20-60 s. | 0.4 | 3D |
| CarPass_02.wav | one-shot | 6.45 s | WAV mono | -3 dBFS pk | recorded | Car passing by at moderate speed. | 3D on the street lane or 2D. | 0.35 | 3D |
| CarPass_03.wav | one-shot | 5.00 s | WAV mono | -3 dBFS pk | recorded | Two cars passing slowly. | 3D on the street lane or 2D. | 0.35 | 3D |
| DogBarkDistant_01.wav | one-shot | 0.95 s | WAV mono | -3 dBFS pk | recorded | Single distant dog bark in a neighbourhood. | 2D ambient one-shot every 1-4 minutes (or 3D far behind the buildings across the street). | 0.25 | 2D |
| DogBarkDistant_02.wav | one-shot | 1.52 s | WAV mono | -3 dBFS pk | recorded | Distant dog barking twice. | 2D ambient one-shot every 1-4 minutes (or 3D far behind the buildings across the street). | 0.25 | 2D |
| DogBarkDistant_03.wav | one-shot | 8.75 s | WAV mono | -3 dBFS pk | recorded | Distant dog barking six times over 8.7 s. | 2D ambient one-shot every 1-4 minutes (or 3D far behind the buildings across the street). | 0.25 | 2D |
| DogBarkDistant_04.wav | one-shot | 1.10 s | WAV mono | -3 dBFS pk | recorded | A different small dog barking twice far away (rural). | 2D ambient one-shot. | 0.25 | 2D |

## Footsteps and glass (one-shots)

| File | Type | Length | Format | Level | Origin | What it is | Suggested placement | Volume | 2D/3D |
|---|---|---|---|---|---|---|---|---|---|
| FootstepHard_01.wav | one-shot | 0.32 s | WAV mono | -3 dBFS pk | recorded | Hard-soled footstep on concrete/tile (variation 1 of 6). | Player/officer footsteps on the shop tile and pavement; random pick, pitch 0.95-1.05. | 0.5 | 3D (attached to feet) or 2D |
| FootstepHard_02.wav | one-shot | 0.32 s | WAV mono | -3 dBFS pk | recorded | Hard-soled footstep on concrete/tile (variation 2 of 6). | Player/officer footsteps on the shop tile and pavement; random pick, pitch 0.95-1.05. | 0.5 | 3D (attached to feet) or 2D |
| FootstepHard_03.wav | one-shot | 0.32 s | WAV mono | -3 dBFS pk | recorded | Hard-soled footstep on concrete/tile (variation 3 of 6). | Player/officer footsteps on the shop tile and pavement; random pick, pitch 0.95-1.05. | 0.5 | 3D (attached to feet) or 2D |
| FootstepHard_04.wav | one-shot | 0.32 s | WAV mono | -3 dBFS pk | recorded | Hard-soled footstep on concrete/tile (variation 4 of 6). | Player/officer footsteps on the shop tile and pavement; random pick, pitch 0.95-1.05. | 0.5 | 3D (attached to feet) or 2D |
| FootstepHard_05.wav | one-shot | 0.32 s | WAV mono | -3 dBFS pk | recorded | Hard-soled footstep on concrete/tile (variation 5 of 6). | Player/officer footsteps on the shop tile and pavement; random pick, pitch 0.95-1.05. | 0.5 | 3D (attached to feet) or 2D |
| FootstepHard_06.wav | one-shot | 0.32 s | WAV mono | -3 dBFS pk | recorded | Hard-soled footstep on concrete/tile (variation 6 of 6). | Player/officer footsteps on the shop tile and pavement; random pick, pitch 0.95-1.05. | 0.5 | 3D (attached to feet) or 2D |
| FootstepGlass_01.wav | one-shot | 0.36 s | WAV mono | -3 dBFS pk | recorded + original | Footstep on broken glass: hard step with glass fracturing and grinding under heel and toe, shards scattering (variation 1 of 5). | Footsteps when the player stands on the glass-debris area near the forced doors and broken bays (about 2.5 dB louder than FootstepHard at the same volume, hence 0.4). | 0.4 | 3D or 2D |
| FootstepGlass_02.wav | one-shot | 0.36 s | WAV mono | -3 dBFS pk | recorded + original | Footstep on broken glass: hard step with glass fracturing and grinding under heel and toe, shards scattering (variation 2 of 5). | Footsteps when the player stands on the glass-debris area near the forced doors and broken bays (about 2.5 dB louder than FootstepHard at the same volume, hence 0.4). | 0.4 | 3D or 2D |
| FootstepGlass_03.wav | one-shot | 0.44 s | WAV mono | -3 dBFS pk | recorded + original | Footstep on broken glass: hard step with glass fracturing and grinding under heel and toe, shards scattering (variation 3 of 5). | Footsteps when the player stands on the glass-debris area near the forced doors and broken bays (about 2.5 dB louder than FootstepHard at the same volume, hence 0.4). | 0.4 | 3D or 2D |
| FootstepGlass_04.wav | one-shot | 0.39 s | WAV mono | -3 dBFS pk | recorded + original | Footstep on broken glass: hard step with glass fracturing and grinding under heel and toe, shards scattering (variation 4 of 5). | Footsteps when the player stands on the glass-debris area near the forced doors and broken bays (about 2.5 dB louder than FootstepHard at the same volume, hence 0.4). | 0.4 | 3D or 2D |
| FootstepGlass_05.wav | one-shot | 0.38 s | WAV mono | -3 dBFS pk | recorded + original | Footstep on broken glass: hard step with glass fracturing and grinding under heel and toe, shards scattering (variation 5 of 5). | Footsteps when the player stands on the glass-debris area near the forced doors and broken bays (about 2.5 dB louder than FootstepHard at the same volume, hence 0.4). | 0.4 | 3D or 2D |
| GlassCrunch_01.wav | one-shot | 0.34 s | WAV mono | -3 dBFS pk | original | Glass grit crunching under a shoe (no step body). | Layer on top of FootstepHard when on glass, or when a kneeling pad/tool is set onto debris. | 0.45 | 3D |
| GlassCrunch_02.wav | one-shot | 0.33 s | WAV mono | -3 dBFS pk | original | Glass grit crunching under a shoe (no step body). | Layer on top of FootstepHard when on glass, or when a kneeling pad/tool is set onto debris. | 0.45 | 3D |
| GlassCrunch_03.wav | one-shot | 0.40 s | WAV mono | -3 dBFS pk | original | Glass grit crunching under a shoe (no step body). | Layer on top of FootstepHard when on glass, or when a kneeling pad/tool is set onto debris. | 0.45 | 3D |
| GlassCrunch_04.wav | one-shot | 0.33 s | WAV mono | -3 dBFS pk | original | Glass grit crunching under a shoe (no step body). | Layer on top of FootstepHard when on glass, or when a kneeling pad/tool is set onto debris. | 0.45 | 3D |
| GlassShardsTinkle_01.wav | one-shot | 1.59 s | WAV mono | -3 dBFS pk | original | Small glass shards falling and bouncing on a hard floor, settling with a few slides. | When a tool or foot disturbs debris; also a rare random 'settling' tinkle near broken bays. | 0.4 | 3D |
| GlassShardsTinkle_02.wav | one-shot | 1.35 s | WAV mono | -3 dBFS pk | original | Small glass shards falling and bouncing on a hard floor, settling with a few slides. | When a tool or foot disturbs debris; also a rare random 'settling' tinkle near broken bays. | 0.4 | 3D |
| GlassShardsTinkle_03.wav | one-shot | 1.47 s | WAV mono | -3 dBFS pk | original | Small glass shards falling and bouncing on a hard floor, settling with a few slides. | When a tool or foot disturbs debris; also a rare random 'settling' tinkle near broken bays. | 0.4 | 3D |
| GlassShardsTinkle_04.wav | one-shot | 1.53 s | WAV mono | -3 dBFS pk | original | Small glass shards falling and bouncing on a hard floor, settling with a few slides. | When a tool or foot disturbs debris; also a rare random 'settling' tinkle near broken bays. | 0.4 | 3D |

## Tools and handling (one-shots)

| File | Type | Length | Format | Level | Origin | What it is | Suggested placement | Volume | 2D/3D |
|---|---|---|---|---|---|---|---|---|---|
| EvidenceMarkerPlace_01.wav | one-shot | 0.12 s | WAV mono | -3 dBFS pk | original | Folded plastic evidence marker (tent) set down on tile: two edges touching down. | On marker placement (3D at the marker). | 0.5 | 3D |
| EvidenceMarkerPlace_02.wav | one-shot | 0.12 s | WAV mono | -3 dBFS pk | original | Folded plastic evidence marker (tent) set down on tile: two edges touching down. | On marker placement (3D at the marker). | 0.5 | 3D |
| EvidenceMarkerPlace_03.wav | one-shot | 0.13 s | WAV mono | -3 dBFS pk | original | Folded plastic evidence marker (tent) set down on tile: two edges touching down. | On marker placement (3D at the marker). | 0.5 | 3D |
| ConePlace_01.wav | one-shot | 0.26 s | WAV mono | -3 dBFS pk | original | PVC traffic cone with rubber base set down: dull slap, hollow body, one or two wobbles. | On cone placement (3D). | 0.55 | 3D |
| ConePlace_02.wav | one-shot | 0.33 s | WAV mono | -3 dBFS pk | original | PVC traffic cone with rubber base set down: dull slap, hollow body, one or two wobbles. | On cone placement (3D). | 0.55 | 3D |
| ConePlace_03.wav | one-shot | 0.24 s | WAV mono | -3 dBFS pk | original | PVC traffic cone with rubber base set down: dull slap, hollow body, one or two wobbles. | On cone placement (3D). | 0.55 | 3D |
| TapeUnroll_01.wav | one-shot | 1.56 s | WAV mono | -3 dBFS pk | original | Barrier tape pulled off a hand-held roll: roll rattle, film crackle, flutter. | While the player drags barrier tape (start/loop the middle while dragging). | 0.45 | 3D |
| TapeUnroll_02.wav | one-shot | 1.37 s | WAV mono | -3 dBFS pk | original | Barrier tape pulled off a hand-held roll: roll rattle, film crackle, flutter. | While the player drags barrier tape (start/loop the middle while dragging). | 0.45 | 3D |
| TapeStretchSnap_01.wav | one-shot | 0.51 s | WAV mono | -3 dBFS pk | original | Polyethylene barrier tape stretched until it snaps, ends whipping back. | When a tape segment is finished/cut. | 0.45 | 3D |
| TapeStretchSnap_02.wav | one-shot | 0.61 s | WAV mono | -3 dBFS pk | original | Polyethylene barrier tape stretched until it snaps, ends whipping back. | When a tape segment is finished/cut. | 0.45 | 3D |
| BarrierTapeFlutter_Loop.wav | loop | 20.00 s | WAV mono | -20 LUFS | original | Barrier tape stretched between posts fluttering in a light night breeze. | 3D source on each outdoor tape span (small radius, low volume). | 0.25 | 3D |
| CameraShutterDSLR_01.wav | one-shot | 0.18 s | WAV mono | -3 dBFS pk | original | Mirror-slap DSLR exposure: mirror up, two curtains at 1/125 s, mirror return and re-cock motor. | Alternative to NightAudio/CameraClick.wav for the evidence camera (2D for the player). | 0.5 | 2D |
| CameraShutterDSLR_02.wav | one-shot | 0.17 s | WAV mono | -3 dBFS pk | original | Mirror-slap DSLR exposure: mirror up, two curtains at 1/125 s, mirror return and re-cock motor. | Alternative to NightAudio/CameraClick.wav for the evidence camera (2D for the player). | 0.5 | 2D |
| FlashlightClick_01.wav | one-shot | 0.13 s | WAV mono | -3 dBFS pk | original | Tactile tail-cap switch of a metal torch: press and release (two variations). | Alternative to NightAudio/FlashlightSwitch.wav for the torch and UV torch (use _01 for torch, _02 for UV). | 0.4 | 2D |
| FlashlightClick_02.wav | one-shot | 0.13 s | WAV mono | -3 dBFS pk | original | Tactile tail-cap switch of a metal torch: press and release (two variations). | Alternative to NightAudio/FlashlightSwitch.wav for the torch and UV torch (use _01 for torch, _02 for UV). | 0.4 | 2D |
| FlashlightClickHalf_01.wav | one-shot | 0.06 s | WAV mono | -3 dBFS pk | original | Single switch click (press only), e.g. momentary/half press. | Torch momentary mode. | 0.4 | 2D |
| KeysJingle_01.wav | one-shot | 1.21 s | WAV mono | -3 dBFS pk | original | Bunch of 4-6 keys on a ring shaken/handled. | Officer or manager handling keys at the safe room / office door. | 0.4 | 3D |
| KeysJingle_02.wav | one-shot | 1.08 s | WAV mono | -3 dBFS pk | original | Bunch of 4-6 keys on a ring shaken/handled. | Officer or manager handling keys at the safe room / office door. | 0.4 | 3D |
| KeysJingle_03.wav | one-shot | 0.81 s | WAV mono | -3 dBFS pk | original | Bunch of 4-6 keys on a ring shaken/handled. | Officer or manager handling keys at the safe room / office door. | 0.4 | 3D |
| ZipperOpen_01.wav | one-shot | 0.83 s | WAV mono | -3 dBFS pk | original | Kit-bag zip opened (long/short) or closed. | Forensic kit bag; opening the tool panel. | 0.4 | 3D |
| ZipperOpen_02.wav | one-shot | 0.48 s | WAV mono | -3 dBFS pk | original | Kit-bag zip opened (long/short) or closed. | Forensic kit bag; opening the tool panel. | 0.4 | 3D |
| ZipperClose_01.wav | one-shot | 0.73 s | WAV mono | -3 dBFS pk | original | Kit-bag zip opened (long/short) or closed. | Forensic kit bag; opening the tool panel. | 0.4 | 3D |

## Interior doors (one-shots)

| File | Type | Length | Format | Level | Origin | What it is | Suggested placement | Volume | 2D/3D |
|---|---|---|---|---|---|---|---|---|---|
| DoorCreak_01.wav | one-shot | 0.66 s | WAV mono | -3 dBFS pk | recorded | Wooden door hinge creak. | Closed side-room doors, rear office door (3D at the hinge/latch). | 0.5 | 3D |
| DoorCreak_02.wav | one-shot | 0.83 s | WAV mono | -3 dBFS pk | recorded | Wooden door hinge creak. | Closed side-room doors, rear office door (3D at the hinge/latch). | 0.5 | 3D |
| DoorCreak_03.wav | one-shot | 0.34 s | WAV mono | -3 dBFS pk | recorded | Wooden door hinge creak. | Closed side-room doors, rear office door (3D at the hinge/latch). | 0.5 | 3D |
| DoorOpen_01.wav | one-shot | 0.92 s | WAV mono | -3 dBFS pk | recorded | Wooden interior door: handle and opening. | Closed side-room doors, rear office door (3D at the hinge/latch). | 0.5 | 3D |
| DoorOpen_02.wav | one-shot | 1.41 s | WAV mono | -3 dBFS pk | recorded | Wooden interior door: handle and opening. | Closed side-room doors, rear office door (3D at the hinge/latch). | 0.5 | 3D |
| DoorClose_01.wav | one-shot | 0.68 s | WAV mono | -3 dBFS pk | recorded | Wooden interior door closing with latch. | Closed side-room doors, rear office door (3D at the hinge/latch). | 0.5 | 3D |
| DoorClose_02.wav | one-shot | 0.60 s | WAV mono | -3 dBFS pk | recorded | Wooden interior door closing with latch. | Closed side-room doors, rear office door (3D at the hinge/latch). | 0.5 | 3D |

## Regenerating

Generator/ holds the scripts used (Python 3, numpy, scipy, ffmpeg): make_alarm.py, make_room.py, make_radio.py (with speech_babble.py, the formant speech synthesizer), make_foley.py and make_sirens.py write the original sounds; process_recorded.py prepares the recordings from the decoded sources listed in sources.json (download them from the repositories and commits given there; `process_recorded.py SRC OUT idle` re-renders only the police-car idle). Seeds are fixed, so the synthesized files are reproducible bit-for-bit given the same numpy/scipy/ffmpeg versions. Unity imports the .py files as plain assets; they are not compiled.

## Known limits

- Several recordings come from 128 kbps Freesound previews re-encoded by the mirror projects, so their top octave is soft; acceptable for distant/background use. For a final release, replace them with the original WAV downloads from the Freesound pages listed (same CC0 licence).
- No CC0 recording of a burglar alarm bell at good quality was reachable, so the bell is synthesized; a recorded bell can replace AlarmBell_Close_Loop later without code changes.
- The radio chatter is deliberately unintelligible synthetic speech (no words, no real dispatcher). Its rhythm, dynamics and spectrum now measure like radio speech, but it has not been auditioned; it is meant to be heard as radio traffic from a few metres away, not as close-up dialogue. If it still sounds artificial on the headset, lower it to 0.25 and keep the squelch/beep clips, or replace it with a recorded CC0 take that identifies no real person.
- PoliceCarIdle_Loop.wav comes from a 96 kbps MP3 made from a Freesound HQ preview, so its top octave is soft; fine at a few metres outdoors.
- Nothing here has been heard in Unity or VR yet; levels and rolloff need a listening pass with the Quest 2.
