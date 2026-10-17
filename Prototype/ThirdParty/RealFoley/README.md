# RealFoley

Real recorded foley and room tones meant to replace the synthesized ("retro-game") clips in Prototype/ThirdParty/NightSoundscape and NightAudio. 136 clips, 12.4 MB. Every clip is a field or foley **recording**. A few are a mix of two recordings, marked *layered* below. Nothing here is synthesized. Prepared October 17, 2026. **Not wired and not auditioned** on headphones, in Unity or on the Quest, so all of it is unverified. Pick by ear before wiring.

## Licences

All clips are CC0 1.0 except two that need attribution:

- **ServerFans_Loop.ogg**, CC BY 3.0: "Server fans" by DeVern (https://freesound.org/people/DeVern/sounds/610761/), licensed CC BY 3.0 (https://creativecommons.org/licenses/by/3.0/); via Space Station 14; trimmed, looped and normalised.
- **CameraShutter_02.wav**, CC BY 4.0: "camera snap" by thecheeseman (https://freesound.org/people/thecheeseman/sounds/51360/), edited by ketufaispikinut (Space Station 14), licensed CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/); trimmed and normalised.

If the credits cannot show these lines, use CameraShutter_01 and MachineHum_A/B instead. Freesound pages could not be opened from the preparation environment. Licence status comes from the licence records of the GitHub mirrors (copies in Licenses/), so re-check the Freesound/OpenGameArt pages before a public release. Per-file author, title, original URL, mirror repository, commit, path, licence evidence, duration, loudness, hash and processing are in sources.json. Credits are in LICENSE.txt.

Sources: Kenney Impact Sounds, RPG Audio and Foley Sounds (CC0); rubberduck "100 CC0 SFX #2" (OpenGameArt, CC0); Freesound CC0 recordings via the mirrors hey8v0/ST-iPhonie, Aarymanv/hillbomb-sf, Tombonator3000/SIGNAL-47, lala11112/seodaemun-recycle-demo, Warexpor/DarkwoodMP-YokWare, LautiGR116/EchoProtocol and space-wizards/space-station-14.

## Format

- 48 kHz everywhere. One-shots are mono PCM16 WAV. Loops are Ogg Vorbis q6 and wrap seamlessly (equal-power wrap crossfade). Five beds are stereo; the rest are mono point sources.
- One-shots: silence trimmed, 3 ms/20 ms fades, 40 Hz high-pass (150 Hz for radio and bells). Set to **-18 LUFS** measured over the active part of the clip (BS.1770 K-weighting, ungated), then capped at -1 dBFS sample peak. Very percussive clips therefore read 1-9 dB under -18. Loops are **-24 LUFS integrated**.
- Quality caveat: the hey8v0/ST-iPhonie mirror holds 64 kbps MP3s (one-shots) and 32 kHz/48 kbps mono MP3 32 s excerpts (ambiences), so those clips have no content above about 14-16 kHz. The hillbomb clips are 64-112 kbps MP3s; the radio statics are 16 kHz. They are fine at a distance or under a mix, but replace them with the Freesound originals for a final release.
- Unity import: one-shots *Decompress On Load*, PCM or ADPCM, Preload on. Loops *Compressed In Memory* (Vorbis 60-70), 3D for mono sources, 2D for the stereo beds. Start loops at a random time. Play one-shots with a random pick (never the same twice), pitch 0.94-1.06 and volume 0.85-1.0.

## Replacement table (old → new)

The **Origin of old** column is taken from NightSoundscape/sources.json: *synth* means synthesized by Generator/ (these are the retro-sounding ones), *recorded* means already a recording. New file sets are listed as Name_01..NN.

| Old file(s) | Origin of old | New file(s) in RealFoley | Notes |
|---|---|---|---|
| NightSoundscape/FootstepHard_01..06 | recorded (florianreichelt, low-bitrate) | Footstep_Stone_01..10 (shop tile/polished stone), Footstep_Concrete_01..08 (pavement) | Stone 01-05 are very short Kenney steps; 08-10 are cut from an indoor recording, so check them for a second step |
| NightSoundscape/FootstepGlass_01..05 | synth + recorded | Footstep_Glass_01..08 | layered: Kenney hard-shoe step + Kenney recorded glass impact, glass 5 dB under the step |
| (none: wet street) | n/a | Footstep_Wet_01..08 | 01-03 are rubberduck wet steps; 04-08 are layered (RutgerMuller concrete step + rubberduck wet splash) |
| (none: carpet/rug) | n/a | Footstep_Carpet_01..05 | Kenney; only 5 variations |
| (none: NPC heels) | n/a | Footstep_Heels_01..05 | optional, for a manager/witness NPC |
| NightSoundscape/GlassCrunch_01..04 | synth | GlassDebris_Light_01..05 (small crunch), GlassDebris_Medium_01..05 | Kenney recorded glass impacts; not a shuffle under a sole (see gaps) |
| NightSoundscape/GlassShardsTinkle_01..04 | synth | GlassShard_01..06, GlassDebrisSettle_01 (1.3 s debris settling) | GlassBreak_01 is a full pane smash, for a replay or cut-scene only |
| NightSoundscape/DoorOpen_01..02, DoorClose_01..02, DoorCreak_01..03 | recorded (Kenney) | optional: DoorOpen_01, DoorClose_01..02, DoorHandle_01..05, DoorUnlockKeys_01 | the old ones are already real recordings; swap only if these suit the glass door better |
| (none: shop bell) | n/a | ShopDoorBell_01, ShopDoorBell_02 | these are recorded doorbells, possibly electric chimes, not a sprung shop bell; audition them (see gaps) |
| NightSoundscape/KeysJingle_01..03 | synth | Keys_01..06 | 01-04 are cut from one jingling take |
| (none: clothing) | n/a | ClothRustle_01..07, ClothBelt_01..02 | for the player body/officer NPC movement |
| NightSoundscape/EvidenceMarkerPlace_01..03 | synth | PlasticSetDown_03..04 (light), PlasticSetDown_01..02 (heavier plastic helmet set-down) | PickUp_01..04 for picking a marker back up |
| NightSoundscape/ConePlace_01..03 | synth | PlasticSetDown_01..02 (pitch about 0.85) | no recorded traffic cone (gap) |
| NightSoundscape/CameraShutterDSLR_01..02, NightAudio/CameraClick.wav | synth / Kenney UI click | CameraShutter_01 (CC0), CameraShutter_02 (CC BY 4.0), CameraFocusShutter_01 (7.7 s, focus + shutter; trim in use or skip) | |
| NightSoundscape/FlashlightClick_01..02, FlashlightClickHalf_01, NightAudio/FlashlightSwitch.wav | synth / Kenney UI | SwitchClick_01..05, MetalClick_01 | real switches (wall switch, small push switch); no recorded torch tail-cap |
| NightSoundscape/ZipperOpen_01..02, ZipperClose_01 | synth | Zipper_01..03 | Zipper_01 is 7.4 s long; use part of it, or loop a region while the zip is pulled |
| (none: kit case latches) | n/a | CaseLatch_01..02 | |
| NightAudio/ToolSetDown.wav | Kenney UI | PlasticSetDown_03..04, PickUp_01..04 | |
| NightSoundscape/TapeUnroll_01..02 | synth | TapePeel_01 (0.16 s duct-tape peel) | only one short clip; repeat it with pitch variation while tape is dragged; still a gap |
| NightSoundscape/TapeStretchSnap_01..02, BarrierTapeFlutter_Loop | synth | none | gap, keep the old ones |
| NightSoundscape/RadioSquelch_01..03 | synth | RadioSquelchTail_01, RadioKnob_01..02 | the mirror cut the bass and added a 0.3 s tail derived from the recording |
| NightSoundscape/RadioBeep_01..03 | synth | RadioKeyBeep_01 | a real walkie-talkie talk-permit beep |
| NightSoundscape/RadioChatter_01..05, PoliceCarRadio_Loop | synth (speech-like babble) | RadioStatic_Near_Loop, RadioStatic_Far_Loop as the bed under the old chatter | no CC0 recording of real police voice traffic was found (gap) |
| NightSoundscape/HVACVent_Loop | synth | HVACVentAir_Loop (stereo, real household floor vent) | HVACRoomTone_Loop (recorded) can stay |
| NightSoundscape/FluorescentBuzz_Loop | synth | FluorescentBuzz_Loop (real fluorescent tube, Zoom H6) | same name, different folder |
| NightSoundscape/FridgeCompressor_Loop | synth | CompressorHum_Loop (real compressor hum), MachineHum_A_Loop / MachineHum_B_Loop | no fridge recorded as such; these are machine/compressor hums |
| (none: server/computer fan) | n/a | ServerFans_Loop (CC BY 3.0) | for the back office / CCTV recorder |
| NightAudio/DistantAir_Loop.wav | synth | NightCityAirTone_Loop (quiet city night air tone) or DistantHighway_Loop | |
| NightSoundscape/NightCity_Loop, NightCityDistant_Loop | recorded | keep; alternatives NightCityAtmos_Loop, NightCityAirTone_Loop, OfficeRoomTone_Loop (interior) | the new ones are mono 32 kHz excerpts, so the old stereo beds stay the main beds |
| NightSoundscape/LightRainStreet_Loop | recorded | keep, or RainCityStreet_Loop (stereo, heavier, city street) / RainSteadyCity_Loop; WaterDrip_01..04 for drips | |
| NightSoundscape/StreetWind_Loop, WindWhistleGap_Loop | recorded | keep; alternatives WindSoft_Loop, WindWinter_Loop | |
| (none: distant traffic) | n/a | DistantHighway_Loop (stereo) | |
| NightSoundscape/Siren*, CarPass*, CarDoor*, PoliceCarIdle_Loop, DogBark* | recorded | keep | already real recordings |
| NightSoundscape/AlarmBell_* | synth | none | no CC0 burglar-bell recording found (gap) |
| NightAudio/PanelSelect.wav | Kenney UI | keep | interface sound, not foley |

## Gaps (nothing real and redistributable found from this environment)

- Gloves snapping on: the only CC0 candidate found (dannydandanshababaloo, Freesound 593196) is mixed into another project's sound sprite and cannot be cut out cleanly.
- Real police radio voice traffic, a recorded burglar alarm bell, a sprung shop-door bell (the two doorbells may be electric chimes), barrier tape stretch/snap/flutter, a traffic cone, a torch tail-cap click, a fridge as such.
- Glass shuffle/grind under a sole: Footstep_Glass is a layer of a step and a glass impact. A continuous grind needs a dedicated recording.
- Carpet has only 5 variations. Wet steps have 3 pure takes plus 5 layered ones.
- A distant siren was not re-sourced because NightSoundscape's sirens are already recordings.

## All files

| File | Type | Length | Channels | Level | Author(s) | Licence |
|---|---|---|---|---|---|---|
| Footstep_Stone_01.wav | one-shot | 0.09 s | mono | -18.0 LUFS act / -4.0 dBFS pk | Kenney | CC0 1.0 |
| Footstep_Stone_02.wav | one-shot | 0.10 s | mono | -18.0 LUFS act / -2.3 dBFS pk | Kenney | CC0 1.0 |
| Footstep_Stone_03.wav | one-shot | 0.10 s | mono | -18.0 LUFS act / -2.9 dBFS pk | Kenney | CC0 1.0 |
| Footstep_Stone_04.wav | one-shot | 0.09 s | mono | -18.0 LUFS act / -6.0 dBFS pk | Kenney | CC0 1.0 |
| Footstep_Stone_05.wav | one-shot | 0.09 s | mono | -18.0 LUFS act / -3.7 dBFS pk | Kenney | CC0 1.0 |
| Footstep_Stone_06.wav | one-shot | 0.30 s | mono | -18.0 LUFS act / -2.4 dBFS pk | rubberduck | CC0 1.0 |
| Footstep_Stone_07.wav | one-shot | 0.30 s | mono | -18.0 LUFS act / -2.1 dBFS pk | rubberduck | CC0 1.0 |
| Footstep_Stone_08.wav | one-shot | 0.50 s | mono | -18.0 LUFS act / -3.9 dBFS pk | WakuWakuWakuWaku | CC0 1.0 |
| Footstep_Stone_09.wav | one-shot | 0.50 s | mono | -18.0 LUFS act / -3.7 dBFS pk | WakuWakuWakuWaku | CC0 1.0 |
| Footstep_Stone_10.wav | one-shot | 0.50 s | mono | -18.0 LUFS act / -4.8 dBFS pk | WakuWakuWakuWaku | CC0 1.0 |
| Footstep_Concrete_01.wav | one-shot | 0.26 s | mono | -19.6 LUFS act / -1.0 dBFS pk | RutgerMuller | CC0 1.0 |
| Footstep_Concrete_02.wav | one-shot | 0.25 s | mono | -19.1 LUFS act / -1.0 dBFS pk | RutgerMuller | CC0 1.0 |
| Footstep_Concrete_03.wav | one-shot | 0.27 s | mono | -19.3 LUFS act / -1.0 dBFS pk | RutgerMuller | CC0 1.0 |
| Footstep_Concrete_04.wav | one-shot | 0.27 s | mono | -18.0 LUFS act / -1.5 dBFS pk | RutgerMuller | CC0 1.0 |
| Footstep_Concrete_05.wav | one-shot | 0.26 s | mono | -23.0 LUFS act / -1.0 dBFS pk | RutgerMuller | CC0 1.0 |
| Footstep_Concrete_06.wav | one-shot | 0.27 s | mono | -20.8 LUFS act / -1.0 dBFS pk | RutgerMuller | CC0 1.0 |
| Footstep_Concrete_07.wav | one-shot | 0.24 s | mono | -19.0 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| Footstep_Concrete_08.wav | one-shot | 0.31 s | mono | -19.1 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| Footstep_Wet_01.wav | one-shot | 0.27 s | mono | -19.4 LUFS act / -1.0 dBFS pk | rubberduck | CC0 1.0 |
| Footstep_Wet_02.wav | one-shot | 0.38 s | mono | -18.0 LUFS act / -1.4 dBFS pk | rubberduck | CC0 1.0 |
| Footstep_Wet_03.wav | one-shot | 0.37 s | mono | -19.5 LUFS act / -1.0 dBFS pk | rubberduck | CC0 1.0 |
| Footstep_Wet_04.wav | one-shot | 0.28 s | mono | -19.3 LUFS act / -1.0 dBFS pk | RutgerMuller + rubberduck | CC0 1.0 |
| Footstep_Wet_05.wav | one-shot | 0.39 s | mono | -18.5 LUFS act / -1.0 dBFS pk | RutgerMuller + rubberduck | CC0 1.0 |
| Footstep_Wet_06.wav | one-shot | 0.38 s | mono | -19.1 LUFS act / -1.0 dBFS pk | RutgerMuller + rubberduck | CC0 1.0 |
| Footstep_Wet_07.wav | one-shot | 0.29 s | mono | -18.2 LUFS act / -1.0 dBFS pk | RutgerMuller + rubberduck | CC0 1.0 |
| Footstep_Wet_08.wav | one-shot | 0.39 s | mono | -18.5 LUFS act / -1.0 dBFS pk | RutgerMuller + rubberduck | CC0 1.0 |
| Footstep_Glass_01.wav | one-shot | 0.18 s | mono | -18.0 LUFS act / -5.0 dBFS pk | Kenney + Kenney | CC0 1.0 |
| Footstep_Glass_02.wav | one-shot | 0.17 s | mono | -18.0 LUFS act / -4.7 dBFS pk | Kenney + Kenney | CC0 1.0 |
| Footstep_Glass_03.wav | one-shot | 0.37 s | mono | -18.8 LUFS act / -1.0 dBFS pk | Kenney + Kenney | CC0 1.0 |
| Footstep_Glass_04.wav | one-shot | 0.17 s | mono | -18.0 LUFS act / -5.8 dBFS pk | Kenney + Kenney | CC0 1.0 |
| Footstep_Glass_05.wav | one-shot | 0.39 s | mono | -18.7 LUFS act / -1.0 dBFS pk | Kenney + Kenney | CC0 1.0 |
| Footstep_Glass_06.wav | one-shot | 0.17 s | mono | -18.0 LUFS act / -4.4 dBFS pk | Kenney + Kenney | CC0 1.0 |
| Footstep_Glass_07.wav | one-shot | 0.39 s | mono | -18.8 LUFS act / -1.0 dBFS pk | Kenney + Kenney | CC0 1.0 |
| Footstep_Glass_08.wav | one-shot | 0.17 s | mono | -18.0 LUFS act / -6.1 dBFS pk | Kenney + Kenney | CC0 1.0 |
| Footstep_Carpet_01.wav | one-shot | 0.17 s | mono | -18.7 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| Footstep_Carpet_02.wav | one-shot | 0.11 s | mono | -18.0 LUFS act / -2.6 dBFS pk | Kenney | CC0 1.0 |
| Footstep_Carpet_03.wav | one-shot | 0.11 s | mono | -18.0 LUFS act / -2.6 dBFS pk | Kenney | CC0 1.0 |
| Footstep_Carpet_04.wav | one-shot | 0.11 s | mono | -18.0 LUFS act / -1.2 dBFS pk | Kenney | CC0 1.0 |
| Footstep_Carpet_05.wav | one-shot | 0.11 s | mono | -18.0 LUFS act / -1.2 dBFS pk | Kenney | CC0 1.0 |
| Footstep_Heels_01.wav | one-shot | 0.52 s | mono | -20.2 LUFS act / -1.0 dBFS pk | NachtmahrTV; clipped by Centronias | CC0 1.0 |
| Footstep_Heels_02.wav | one-shot | 0.41 s | mono | -21.9 LUFS act / -1.0 dBFS pk | NachtmahrTV; clipped by Centronias | CC0 1.0 |
| Footstep_Heels_03.wav | one-shot | 0.41 s | mono | -20.6 LUFS act / -1.0 dBFS pk | NachtmahrTV; clipped by Centronias | CC0 1.0 |
| Footstep_Heels_04.wav | one-shot | 0.40 s | mono | -19.9 LUFS act / -1.0 dBFS pk | NachtmahrTV; clipped by Centronias | CC0 1.0 |
| Footstep_Heels_05.wav | one-shot | 0.49 s | mono | -20.5 LUFS act / -1.0 dBFS pk | NachtmahrTV; clipped by Centronias | CC0 1.0 |
| GlassDebris_Light_01.wav | one-shot | 0.18 s | mono | -18.0 LUFS act / -5.4 dBFS pk | Kenney | CC0 1.0 |
| GlassDebris_Light_02.wav | one-shot | 0.18 s | mono | -18.0 LUFS act / -4.5 dBFS pk | Kenney | CC0 1.0 |
| GlassDebris_Light_03.wav | one-shot | 0.18 s | mono | -18.0 LUFS act / -5.9 dBFS pk | Kenney | CC0 1.0 |
| GlassDebris_Light_04.wav | one-shot | 0.18 s | mono | -18.0 LUFS act / -4.1 dBFS pk | Kenney | CC0 1.0 |
| GlassDebris_Light_05.wav | one-shot | 0.18 s | mono | -18.0 LUFS act / -5.4 dBFS pk | Kenney | CC0 1.0 |
| GlassDebris_Medium_01.wav | one-shot | 0.42 s | mono | -19.4 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| GlassDebris_Medium_02.wav | one-shot | 0.42 s | mono | -19.7 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| GlassDebris_Medium_03.wav | one-shot | 0.39 s | mono | -19.7 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| GlassDebris_Medium_04.wav | one-shot | 0.43 s | mono | -18.8 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| GlassDebris_Medium_05.wav | one-shot | 0.41 s | mono | -18.7 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| GlassShard_01.wav | one-shot | 0.24 s | mono | -18.0 LUFS act / -9.3 dBFS pk | rubberduck | CC0 1.0 |
| GlassShard_02.wav | one-shot | 0.42 s | mono | -18.0 LUFS act / -4.9 dBFS pk | rubberduck | CC0 1.0 |
| GlassShard_03.wav | one-shot | 0.45 s | mono | -18.0 LUFS act / -3.8 dBFS pk | rubberduck | CC0 1.0 |
| GlassShard_04.wav | one-shot | 0.17 s | mono | -18.0 LUFS act / -5.8 dBFS pk | rubberduck | CC0 1.0 |
| GlassShard_05.wav | one-shot | 0.52 s | mono | -18.0 LUFS act / -5.3 dBFS pk | rubberduck | CC0 1.0 |
| GlassShard_06.wav | one-shot | 0.40 s | mono | -18.0 LUFS act / -6.0 dBFS pk | rubberduck | CC0 1.0 |
| GlassDebrisSettle_01.wav | one-shot | 1.35 s | mono | -18.0 LUFS act / -2.2 dBFS pk | kyles | CC0 1.0 |
| GlassBreak_01.wav | one-shot | 0.52 s | mono | -18.0 LUFS act / -1.5 dBFS pk | unfa | CC0 1.0 |
| DoorHandle_01.wav | one-shot | 0.37 s | mono | -21.9 LUFS act / -1.0 dBFS pk | rubberduck | CC0 1.0 |
| DoorHandle_02.wav | one-shot | 0.24 s | mono | -22.1 LUFS act / -1.0 dBFS pk | rubberduck | CC0 1.0 |
| DoorHandle_03.wav | one-shot | 0.72 s | mono | -18.8 LUFS act / -1.0 dBFS pk | rubberduck | CC0 1.0 |
| DoorHandle_04.wav | one-shot | 0.20 s | mono | -18.0 LUFS act / -2.0 dBFS pk | rubberduck | CC0 1.0 |
| DoorHandle_05.wav | one-shot | 0.31 s | mono | -22.2 LUFS act / -1.0 dBFS pk | rubberduck | CC0 1.0 |
| DoorOpen_01.wav | one-shot | 1.12 s | mono | -27.5 LUFS act / -1.0 dBFS pk | Anthousai | CC0 1.0 |
| DoorClose_01.wav | one-shot | 1.23 s | mono | -19.9 LUFS act / -1.0 dBFS pk | soundmary | CC0 1.0 |
| DoorClose_02.wav | one-shot | 0.66 s | mono | -21.3 LUFS act / -1.0 dBFS pk | wjtaylor | CC0 1.0 |
| ShopDoorBell_01.wav | one-shot | 2.98 s | mono | -18.3 LUFS act / -1.0 dBFS pk | jwheeler91 | CC0 1.0 |
| ShopDoorBell_02.wav | one-shot | 9.21 s | mono | -20.6 LUFS act / -1.0 dBFS pk | guitarguy1985 | CC0 1.0 |
| DoorUnlockKeys_01.wav | one-shot | 8.74 s | mono | -23.2 LUFS act / -1.0 dBFS pk | Lightnessko | CC0 1.0 |
| Keys_01.wav | one-shot | 0.65 s | mono | -19.1 LUFS act / -1.0 dBFS pk | vmgraw | CC0 1.0 |
| Keys_02.wav | one-shot | 0.61 s | mono | -18.0 LUFS act / -3.8 dBFS pk | vmgraw | CC0 1.0 |
| Keys_03.wav | one-shot | 0.71 s | mono | -18.0 LUFS act / -1.9 dBFS pk | vmgraw | CC0 1.0 |
| Keys_04.wav | one-shot | 0.65 s | mono | -18.0 LUFS act / -3.8 dBFS pk | vmgraw | CC0 1.0 |
| Keys_05.wav | one-shot | 2.20 s | mono | -20.9 LUFS act / -1.0 dBFS pk | Lightnessko | CC0 1.0 |
| Keys_06.wav | one-shot | 1.12 s | mono | -20.0 LUFS act / -1.0 dBFS pk | WeeJee_vdH | CC0 1.0 |
| ClothRustle_01.wav | one-shot | 0.63 s | mono | -22.9 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| ClothRustle_02.wav | one-shot | 0.38 s | mono | -19.3 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| ClothRustle_03.wav | one-shot | 0.48 s | mono | -21.9 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| ClothRustle_04.wav | one-shot | 0.38 s | mono | -20.4 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| ClothRustle_05.wav | one-shot | 2.61 s | mono | -24.0 LUFS act / -1.0 dBFS pk | martian | CC0 1.0 |
| ClothRustle_06.wav | one-shot | 1.71 s | mono | -22.4 LUFS act / -1.0 dBFS pk | WasabiWielder | CC0 1.0 |
| ClothRustle_07.wav | one-shot | 2.85 s | mono | -25.3 LUFS act / -1.0 dBFS pk | NachtmahrTV | CC0 1.0 |
| ClothBelt_01.wav | one-shot | 0.72 s | mono | -23.7 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| ClothBelt_02.wav | one-shot | 0.68 s | mono | -20.8 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| PlasticSetDown_01.wav | one-shot | 0.91 s | mono | -18.4 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| PlasticSetDown_02.wav | one-shot | 1.06 s | mono | -20.5 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| PlasticSetDown_03.wav | one-shot | 0.42 s | mono | -18.0 LUFS act / -5.2 dBFS pk | rubberduck | CC0 1.0 |
| PlasticSetDown_04.wav | one-shot | 0.45 s | mono | -18.0 LUFS act / -3.6 dBFS pk | rubberduck | CC0 1.0 |
| PickUp_01.wav | one-shot | 1.84 s | mono | -18.0 LUFS act / -2.6 dBFS pk | Kenney | CC0 1.0 |
| PickUp_02.wav | one-shot | 1.26 s | mono | -18.0 LUFS act / -1.9 dBFS pk | Kenney | CC0 1.0 |
| PickUp_03.wav | one-shot | 0.65 s | mono | -19.9 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| PickUp_04.wav | one-shot | 0.99 s | mono | -20.1 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| PlasticBag_01.wav | one-shot | 2.50 s | mono | -22.3 LUFS act / -1.0 dBFS pk | timgormly | CC0 1.0 |
| CameraShutter_01.wav | one-shot | 0.29 s | mono | -20.3 LUFS act / -1.0 dBFS pk | roachpowder | CC0 1.0 |
| CameraShutter_02.wav | one-shot | 0.54 s | mono | -18.0 LUFS act / -3.9 dBFS pk | thecheeseman; edited by ketufaispikinut | CC BY 4.0 |
| CameraFocusShutter_01.wav | one-shot | 7.70 s | mono | -22.6 LUFS act / -1.0 dBFS pk | uEffects | CC0 1.0 |
| SwitchClick_01.wav | one-shot | 0.22 s | mono | -26.6 LUFS act / -1.0 dBFS pk | rubberduck | CC0 1.0 |
| SwitchClick_02.wav | one-shot | 0.22 s | mono | -23.5 LUFS act / -1.0 dBFS pk | rubberduck | CC0 1.0 |
| SwitchClick_03.wav | one-shot | 0.18 s | mono | -20.5 LUFS act / -1.0 dBFS pk | SomeoneCool15 | CC0 1.0 |
| SwitchClick_04.wav | one-shot | 0.19 s | mono | -19.0 LUFS act / -1.0 dBFS pk | SomeoneCool15 | CC0 1.0 |
| SwitchClick_05.wav | one-shot | 0.18 s | mono | -18.7 LUFS act / -1.0 dBFS pk | IanStarGem | CC0 1.0 |
| MetalClick_01.wav | one-shot | 0.41 s | mono | -21.7 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| Zipper_01.wav | one-shot | 7.43 s | mono | -21.4 LUFS act / -1.0 dBFS pk | Tee_Hee_Tummy_Tums | CC0 1.0 |
| Zipper_02.wav | one-shot | 1.64 s | mono | -18.0 LUFS act / -3.3 dBFS pk | RutgerMuller | CC0 1.0 |
| Zipper_03.wav | one-shot | 3.29 s | mono | -18.0 LUFS act / -3.0 dBFS pk | leosalom | CC0 1.0 |
| CaseLatch_01.wav | one-shot | 0.25 s | mono | -18.0 LUFS act / -3.9 dBFS pk | Kenney | CC0 1.0 |
| CaseLatch_02.wav | one-shot | 0.51 s | mono | -24.6 LUFS act / -1.0 dBFS pk | rubberduck | CC0 1.0 |
| TapePeel_01.wav | one-shot | 0.16 s | mono | -21.2 LUFS act / -1.0 dBFS pk | thefirecrowd | CC0 1.0 |
| RadioKeyBeep_01.wav | one-shot | 0.66 s | mono | -18.0 LUFS act / -2.2 dBFS pk | SEF7 | CC0 1.0 |
| RadioSquelchTail_01.wav | one-shot | 0.47 s | mono | -18.0 LUFS act / -1.9 dBFS pk | JovianSounds | CC0 1.0 |
| RadioKnob_01.wav | one-shot | 0.49 s | mono | -20.1 LUFS act / -1.0 dBFS pk | greatsoundstube | CC0 1.0 |
| RadioKnob_02.wav | one-shot | 0.48 s | mono | -18.0 LUFS act / -1.7 dBFS pk | greatsoundstube | CC0 1.0 |
| RadioStatic_Near_Loop.ogg | loop | 3.20 s | mono | -24.0 LUFS int | SEF7 | CC0 1.0 |
| RadioStatic_Far_Loop.ogg | loop | 4.80 s | mono | -24.0 LUFS int | LukaCafuka | CC0 1.0 |
| WaterDrip_01.wav | one-shot | 0.28 s | mono | -21.4 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| WaterDrip_02.wav | one-shot | 0.26 s | mono | -18.0 LUFS act / -3.4 dBFS pk | Kenney | CC0 1.0 |
| WaterDrip_03.wav | one-shot | 0.57 s | mono | -18.0 LUFS act / -4.0 dBFS pk | Kenney | CC0 1.0 |
| WaterDrip_04.wav | one-shot | 0.18 s | mono | -21.7 LUFS act / -1.0 dBFS pk | Kenney | CC0 1.0 |
| RainCityStreet_Loop.ogg | loop | 28.00 s | stereo | -24.0 LUFS int | Dominik_W | CC0 1.0 |
| RainSteadyCity_Loop.ogg | loop | 29.00 s | mono | -24.0 LUFS int | roofusj | CC0 1.0 |
| NightCityAtmos_Loop.ogg | loop | 29.00 s | mono | -24.0 LUFS int | Soundkrampf | CC0 1.0 |
| NightCityAirTone_Loop.ogg | loop | 29.00 s | mono | -24.0 LUFS int | leonelmail | CC0 1.0 |
| DistantHighway_Loop.ogg | loop | 14.50 s | stereo | -24.0 LUFS int | rubberduck | CC0 1.0 |
| WindSoft_Loop.ogg | loop | 29.00 s | mono | -24.0 LUFS int | nithala | CC0 1.0 |
| WindWinter_Loop.ogg | loop | 29.00 s | mono | -24.0 LUFS int | klangfabrik | CC0 1.0 |
| OfficeRoomTone_Loop.ogg | loop | 29.00 s | mono | -24.0 LUFS int | mzui | CC0 1.0 |
| HVACVentAir_Loop.ogg | loop | 22.00 s | stereo | -24.0 LUFS int | HECKFRICKER | CC0 1.0 |
| FluorescentBuzz_Loop.ogg | loop | 11.50 s | mono | -24.0 LUFS int | JoelMcDaniel | CC0 1.0 |
| CompressorHum_Loop.ogg | loop | 12.50 s | mono | -24.0 LUFS int | AlaskaRobotics | CC0 1.0 |
| ServerFans_Loop.ogg | loop | 30.00 s | mono | -24.0 LUFS int | DeVern | CC BY 3.0 |
| MachineHum_A_Loop.ogg | loop | 6.50 s | mono | -24.0 LUFS int | rubberduck | CC0 1.0 |
| MachineHum_B_Loop.ogg | loop | 9.50 s | mono | -24.0 LUFS int | rubberduck | CC0 1.0 |

Source~/build_realfoley.py rebuilds every clip from the downloaded mirrors (paths and commits in sources.json); Source~/make_sources.py writes sources.json. Unity ignores Source~.
