# RealFoley2

Second batch of real recorded foley and beds for the night jewelry-store crime scene, filling gaps left by Prototype/ThirdParty/RealFoley. It does not repeat anything in RealFoley or NightSoundscape. 112 clips, 8.5 MB. Every clip is a field or foley **recording**. Nothing is synthesized and nothing is layered. Prepared October 17, 2026. **Not wired and not auditioned** on headphones, in Unity or on the Quest, so all of it is unverified. Pick by ear before wiring.

## Licences

All clips are CC0 1.0 except three, which need attribution (CC BY 4.0):

- **ElectricalHum_Loop.ogg**: "electrical substation_hum.wav" by Zabuhailo (https://freesound.org/people/Zabuhailo/sounds/167673/), licensed CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/); clipped by Jhrushbe (Delta-V); looped and normalised.
- **EngineIdle_InspectorJ_Loop.ogg** and **EngineStart_02.wav**: Freesound sound 338954 by InspectorJ (www.jshaw.co.uk) of Freesound.org (https://freesound.org/people/InspectorJ/sounds/338954/), licensed CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/); via Space Station 14; trimmed, looped and normalised.

If the credits cannot show these lines, use EngineIdle_Car/Police_Loop and EngineStart_01 instead, and drop ElectricalHum_Loop (RealFoley/FluorescentBuzz_Loop is CC0).

Freesound, BigSoundBank and itch.io pages could not be opened from the preparation environment. Licence status comes from the licence records of the GitHub mirrors (copies in Licenses/), so re-check the original pages before a public release. Per-file author, title, original URL, mirror repository, commit, path, licence evidence, duration, loudness, hash and processing are in sources.json. Credits are in LICENSE.txt.

Mirrors used: karolpiczak/ESC-50 (44.1 kHz PCM 5 s excerpts; only clips whose Freesound original is CC0), FEDCHER67/Game (NOX Sound Essentials and BigSoundBank, CC0), space-wizards/space-station-14, DeltaV-Station/Delta-v, crystallpunk-14/crystall-punk-14 (attributions.yml), victorrentea/victor-effects (160 kbps CC0 Freesound cuts), huynguyen03dev/godot-ai-playground (24 kHz CC0 Freesound cuts), series-ai/jam-ready-assets (Kenney RPG Audio).

ESC-50 note: the ESC-50 dataset as a compilation is CC BY-NC 3.0. Each clip is a plain 5 s excerpt of one Freesound recording, and only clips whose original is marked CC0 in the dataset's LICENSE are used here. They are treated as CC0, so check this before a commercial release.

## Format

Same as RealFoley: 48 kHz everywhere. One-shots are mono PCM16 WAV, silence-trimmed, 3 ms/20 ms fades, 40 Hz high-pass (60-150 Hz for clicks, bells and sirens). Each one is set to **-18 LUFS** measured over its active part (BS.1770 K-weighting, ungated) and then capped at -1 dBFS sample peak, so very percussive clips read up to about 10 dB under -18. Loops are Ogg Vorbis q6 at **-24 LUFS integrated**, with an equal-power wrap crossfade. NightCrickets_Loop is stereo; the other loops are mono point sources. The clock loops are a whole number of tick periods long, so the tick spacing stays even across the wrap (checked by onset detection).

Unity import: one-shots *Decompress On Load*; loops *Compressed In Memory* and started at a random time. Play one-shots with a random pick (never the same twice), pitch 0.94-1.06 and volume 0.85-1.0.

Quality caveats:
- ESC-50 sources are 5 s long, so their loops are short (3-4.2 s). Crickets, insects and idles will repeat audibly up close. Layer two of them at different lengths, or keep them distant.
- TorchClick_01 and PaperRustle_* come from 24 kHz MP3s (nothing above about 11 kHz).
- The sirens are close recordings of a siren that never moves, not drive-bys. For a distant pass, use a 3D source with a low-pass and doppler.

## Use for → file

| Use for | File(s) | Notes |
|---|---|---|
| Burglar alarm bell (ringing) | AlarmBell_Loop (electric school bell: hammer on a gong, the same mechanism as a burglar bell) | stop with AlarmBell_Stop_01 (last strokes and ring-out) |
| Alarm bell, smaller or other options | AlarmBellSmall_Loop (twin-bell wind-up clock), AlarmBellClock_Loop (old alarm clock) | brighter and smaller than a wall bell |
| Mechanical shop door bell | ShopBell_01, ShopBell_02 (a struck bell, Freesound 583950) | the type of bell is not confirmed, so audition it; ShopDoorBell_01_HQ is an electric doorbell |
| Police officer boots on pavement | Footstep_Boot_01..06 (NOX "rock walk"), Footstep_BootLeather_01..08 (Kenney leather boots), Footstep_Pavement_01..18 (walking on pavement and asphalt) | Pavement 01-08 JohnsonBrandEditing, 09-14 Ryding (asphalt), 15-18 hintringer (street) |
| Carpet footsteps (more variety) | Footstep_Carpet_06..13 | continues RealFoley Footstep_Carpet_01..05; 06 and 13 are 0.6 s long and may hold two steps |
| Distant police siren | Siren_US_Wail_01..02 (Whelen), Siren_Police_01..04, Siren_EuroTwoTone_01 (French gendarmerie) | close and steady, so add distance in the mixer |
| Car door | CarDoorClose_01..03 (car), CarDoorOpen_01..03 (truck door) | NOX Sound |
| Engine idle | EngineIdle_Police_Loop, EngineIdle_Car_Loop, EngineIdle_CarB_Loop, EngineIdle_Diesel_Loop, EngineIdle_InspectorJ_Loop (CC BY) | EngineStart_01 (CC0), EngineStart_02 (CC BY) |
| Night insects (urban edge) | NightCrickets_Loop (stereo, 7 s), NightCrickets_Soft_Loop, NightCrickets_Far_Loop, NightInsects_Loop | keep them low under the city bed; they suit a back lot or park, not the street |
| Buzzing streetlight or neon | ElectricalHum_Loop (CC BY, substation hum) | for a fluorescent tube, see RealFoley/FluorescentBuzz_Loop |
| Ticking clock (back office) | ClockTick_Wall_Loop, ClockTick_WallB_Loop, ClockTick_Close_Loop, ClockTick_Quartz_Loop (1 s ticks), ClockTick_Fast_Loop (fast ticks) | seamless whole-period loops |
| Torch / flashlight click | TorchClick_01 (a real flashlight), RelayClick_01 | 24 kHz source for TorchClick |
| Pages / paper rustle | PageTurn_01..03, PaperRustle_01..06 | |
| Clipboard / pen writing | PenWriting_01..05, PenClick_01, NotebookOpen_01, NotebookClose_01, NotebookSetDown_01..03 | NotebookSetDown is a book set down; it also works for a clipboard on a counter |
| Plastic kit case open/close | KitCaseOpen_01, KitCaseClose_01 (toolbox) | |
| Evidence pouch / bag handling | PouchHandle_01..02 (small leather pouch) | not a plastic evidence bag; RealFoley/PlasticBag_01 is still the only plastic bag |
| Door knob | DoorKnob_01 | |
| Shop till (optional) | CashRegister_01 | |
| HQ replacement for RealFoley/Footstep_Concrete_01..06 | Footstep_Concrete_01_HQ..06_HQ | same Freesound recording (RutgerMuller 155858), from the PCM ESC-50 excerpts; different steps of it |
| HQ replacement for RealFoley/ShopDoorBell_01 | ShopDoorBell_01_HQ | same recording (jwheeler91 442280), 160 kbps instead of 64 kbps |
| HQ replacement for RealFoley/GlassBreak_01 | GlassBreak_01_HQ | same recording (unfa 221528), 160 kbps |

RealFoley2 adds no new siren drive-bys: NightSoundscape already has recorded ones (SirenDistantPass_04, SirenWailPassDistant_01 and others), and its Freesound 746302 was left out here.

## Gaps (nothing real and redistributable found from this environment)

- **Latex/nitrile gloves snapping on**: the only CC0 recording found is still mixed into a sound sprite (as in RealFoley).
- **Police radio voice traffic or dispatch**: none found with a redistributable licence. Keep RealFoley/RadioStatic_* and the key/squelch clips under the old chatter.
- **Barrier tape stretch, snap or flutter**, **traffic cone set-down**, **camera flash charge whine**, **evidence-bag rustle and seal**, **glass crunched under a shoe (continuous grind)**, **fridge hum**: none found.
- **Distant voices / murmur (walla)** and **real distant siren drive-bys**: the openOMSI ambience pack (github.com/openOMSI-org/openOMSI, release ambience-1, 297 MB, its pack.json says CC0 for most takes) holds both: "City Centre_Distant Sirens" by LewisEmmott5, "Distant ambulance & fire truck sirens" by Breviceps, walla by Profispiesser and LudwigMueller, "Uptown neighborhood night streetlight buzz" by fi2lcallpeace. The release download goes to release-assets.githubusercontent.com, which this session was not allowed to fetch. Allow that host to add them in a later batch.
- **HQ originals for the other low-bitrate RealFoley files** (ST-iPhonie clips, the radio statics): no better copy found on GitHub.
- Short loops: the ESC-50 beds are only 3-4 s long (see the quality caveats above).

## All files

| File | Type | Length | Channels | Level | Author(s) | Licence |
|---|---|---|---|---|---|---|
| Footstep_Carpet_06.wav | one-shot | 0.60 s | mono | -19.8 LUFS act / -1.0 dBFS pk | mlsulli | CC0 1.0 |
| Footstep_Carpet_07.wav | one-shot | 0.39 s | mono | -18.0 LUFS act / -3.9 dBFS pk | mlsulli | CC0 1.0 |
| Footstep_Carpet_08.wav | one-shot | 0.27 s | mono | -18.0 LUFS act / -3.3 dBFS pk | mlsulli | CC0 1.0 |
| Footstep_Carpet_09.wav | one-shot | 0.26 s | mono | -18.0 LUFS act / -5.1 dBFS pk | mlsulli | CC0 1.0 |
| Footstep_Carpet_10.wav | one-shot | 0.49 s | mono | -21.8 LUFS act / -1.0 dBFS pk | mlsulli | CC0 1.0 |
| Footstep_Carpet_11.wav | one-shot | 0.37 s | mono | -22.2 LUFS act / -1.0 dBFS pk | mlsulli | CC0 1.0 |
| Footstep_Carpet_12.wav | one-shot | 0.36 s | mono | -21.2 LUFS act / -1.0 dBFS pk | mlsulli | CC0 1.0 |
| Footstep_Carpet_13.wav | one-shot | 0.60 s | mono | -20.7 LUFS act / -1.0 dBFS pk | mlsulli | CC0 1.0 |
| Footstep_Pavement_01.wav | one-shot | 0.28 s | mono | -18.1 LUFS act / -1.0 dBFS pk | JohnsonBrandEditing | CC0 1.0 |
| Footstep_Pavement_02.wav | one-shot | 0.30 s | mono | -18.0 LUFS act / -1.2 dBFS pk | JohnsonBrandEditing | CC0 1.0 |
| Footstep_Pavement_03.wav | one-shot | 0.36 s | mono | -18.0 LUFS act / -4.9 dBFS pk | JohnsonBrandEditing | CC0 1.0 |
| Footstep_Pavement_04.wav | one-shot | 0.28 s | mono | -23.6 LUFS act / -1.0 dBFS pk | JohnsonBrandEditing | CC0 1.0 |
| Footstep_Pavement_05.wav | one-shot | 0.30 s | mono | -18.0 LUFS act / -2.2 dBFS pk | JohnsonBrandEditing | CC0 1.0 |
| Footstep_Pavement_06.wav | one-shot | 0.26 s | mono | -19.1 LUFS act / -1.0 dBFS pk | JohnsonBrandEditing | CC0 1.0 |
| Footstep_Pavement_07.wav | one-shot | 0.26 s | mono | -18.0 LUFS act / -1.8 dBFS pk | JohnsonBrandEditing | CC0 1.0 |
| Footstep_Pavement_08.wav | one-shot | 0.26 s | mono | -18.0 LUFS act / -4.0 dBFS pk | JohnsonBrandEditing | CC0 1.0 |
| Footstep_Pavement_09.wav | one-shot | 0.28 s | mono | -23.5 LUFS act / -1.0 dBFS pk | Ryding | CC0 1.0 |
| Footstep_Pavement_10.wav | one-shot | 0.27 s | mono | -23.3 LUFS act / -1.0 dBFS pk | Ryding | CC0 1.0 |
| Footstep_Pavement_11.wav | one-shot | 0.32 s | mono | -20.8 LUFS act / -1.0 dBFS pk | Ryding | CC0 1.0 |
| Footstep_Pavement_12.wav | one-shot | 0.27 s | mono | -21.6 LUFS act / -1.0 dBFS pk | Ryding | CC0 1.0 |
| Footstep_Pavement_13.wav | one-shot | 0.31 s | mono | -19.7 LUFS act / -1.0 dBFS pk | Ryding | CC0 1.0 |
| Footstep_Pavement_14.wav | one-shot | 0.38 s | mono | -20.6 LUFS act / -1.0 dBFS pk | Ryding | CC0 1.0 |
| Footstep_Pavement_15.wav | one-shot | 0.30 s | mono | -18.0 LUFS act / -3.4 dBFS pk | hintringer | CC0 1.0 |
| Footstep_Pavement_16.wav | one-shot | 0.31 s | mono | -18.0 LUFS act / -3.6 dBFS pk | hintringer | CC0 1.0 |
| Footstep_Pavement_17.wav | one-shot | 0.31 s | mono | -20.4 LUFS act / -1.0 dBFS pk | hintringer | CC0 1.0 |
| Footstep_Pavement_18.wav | one-shot | 0.32 s | mono | -18.0 LUFS act / -3.1 dBFS pk | hintringer | CC0 1.0 |
| Footstep_Boot_01.wav | one-shot | 0.28 s | mono | -18.0 LUFS act / -2.3 dBFS pk | NOX Sound | CC0 1.0 |
| Footstep_Boot_02.wav | one-shot | 0.30 s | mono | -19.5 LUFS act / -1.0 dBFS pk | NOX Sound | CC0 1.0 |
| Footstep_Boot_03.wav | one-shot | 0.26 s | mono | -19.3 LUFS act / -1.0 dBFS pk | NOX Sound | CC0 1.0 |
| Footstep_Boot_04.wav | one-shot | 0.28 s | mono | -22.0 LUFS act / -1.0 dBFS pk | NOX Sound | CC0 1.0 |
| Footstep_Boot_05.wav | one-shot | 0.24 s | mono | -20.6 LUFS act / -1.0 dBFS pk | NOX Sound | CC0 1.0 |
| Footstep_Boot_06.wav | one-shot | 0.34 s | mono | -21.6 LUFS act / -1.0 dBFS pk | NOX Sound | CC0 1.0 |
| Footstep_BootLeather_01.wav | one-shot | 0.28 s | mono | -18.6 LUFS act / -1.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| Footstep_BootLeather_02.wav | one-shot | 0.24 s | mono | -21.0 LUFS act / -1.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| Footstep_BootLeather_03.wav | one-shot | 0.27 s | mono | -18.5 LUFS act / -1.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| Footstep_BootLeather_04.wav | one-shot | 0.27 s | mono | -21.5 LUFS act / -1.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| Footstep_BootLeather_05.wav | one-shot | 0.24 s | mono | -19.5 LUFS act / -1.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| Footstep_BootLeather_06.wav | one-shot | 0.24 s | mono | -19.7 LUFS act / -1.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| Footstep_BootLeather_07.wav | one-shot | 0.26 s | mono | -18.0 LUFS act / -2.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| Footstep_BootLeather_08.wav | one-shot | 0.21 s | mono | -18.0 LUFS act / -1.8 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| Footstep_Concrete_01_HQ.wav | one-shot | 0.55 s | mono | -27.6 LUFS act / -1.0 dBFS pk | RutgerMuller | CC0 1.0 |
| Footstep_Concrete_02_HQ.wav | one-shot | 0.55 s | mono | -26.5 LUFS act / -1.0 dBFS pk | RutgerMuller | CC0 1.0 |
| Footstep_Concrete_03_HQ.wav | one-shot | 0.55 s | mono | -23.9 LUFS act / -1.0 dBFS pk | RutgerMuller | CC0 1.0 |
| Footstep_Concrete_04_HQ.wav | one-shot | 0.54 s | mono | -25.6 LUFS act / -1.0 dBFS pk | RutgerMuller | CC0 1.0 |
| Footstep_Concrete_05_HQ.wav | one-shot | 0.53 s | mono | -24.6 LUFS act / -1.0 dBFS pk | RutgerMuller | CC0 1.0 |
| Footstep_Concrete_06_HQ.wav | one-shot | 0.54 s | mono | -20.0 LUFS act / -1.0 dBFS pk | RutgerMuller | CC0 1.0 |
| AlarmBell_Loop.ogg | loop | 2.00 s | mono | -24.0 LUFS int / -13.9 dBFS pk | deleted_user_7020630 | CC0 1.0 |
| AlarmBellSmall_Loop.ogg | loop | 4.00 s | mono | -24.0 LUFS int / -18.4 dBFS pk | AngryAdam | CC0 1.0 |
| AlarmBellClock_Loop.ogg | loop | 1.80 s | mono | -24.0 LUFS int / -9.0 dBFS pk | tuberatanka | CC0 1.0 |
| AlarmBell_Stop_01.wav | one-shot | 1.57 s | mono | -18.0 LUFS act / -3.7 dBFS pk | deleted_user_7020630 | CC0 1.0 |
| ShopBell_01.wav | one-shot | 1.54 s | mono | -18.0 LUFS act / -6.6 dBFS pk | Fenodyrie | CC0 1.0 |
| ShopBell_02.wav | one-shot | 0.92 s | mono | -18.0 LUFS act / -5.3 dBFS pk | Fenodyrie | CC0 1.0 |
| ShopDoorBell_01_HQ.wav | one-shot | 2.91 s | mono | -18.0 LUFS act / -1.5 dBFS pk | jwheeler91 | CC0 1.0 |
| Siren_US_Wail_01.wav | one-shot | 5.00 s | mono | -18.0 LUFS act / -9.4 dBFS pk | 2887679652 | CC0 1.0 |
| Siren_US_Wail_02.wav | one-shot | 5.00 s | mono | -18.0 LUFS act / -9.3 dBFS pk | 2887679652 | CC0 1.0 |
| Siren_Police_01.wav | one-shot | 5.00 s | mono | -18.0 LUFS act / -9.8 dBFS pk | guitarguy1985 | CC0 1.0 |
| Siren_Police_02.wav | one-shot | 3.75 s | mono | -18.0 LUFS act / -9.1 dBFS pk | guitarguy1985 | CC0 1.0 |
| Siren_Police_03.wav | one-shot | 5.00 s | mono | -18.0 LUFS act / -5.2 dBFS pk | radiopassiveboy | CC0 1.0 |
| Siren_Police_04.wav | one-shot | 2.84 s | mono | -18.0 LUFS act / -8.6 dBFS pk | Piotr Zaczek (blukotek) | CC0 1.0 |
| Siren_EuroTwoTone_01.wav | one-shot | 7.63 s | mono | -18.0 LUFS act / -9.6 dBFS pk | Joseph Sardin (BigSoundBank) | CC0 1.0 |
| EngineIdle_Car_Loop.ogg | loop | 4.00 s | mono | -24.0 LUFS int / -11.0 dBFS pk | jpkweli | CC0 1.0 |
| EngineIdle_CarB_Loop.ogg | loop | 3.20 s | mono | -24.0 LUFS int / -7.8 dBFS pk | ReadeOnly | CC0 1.0 |
| EngineIdle_Police_Loop.ogg | loop | 5.00 s | mono | -24.0 LUFS int / -11.1 dBFS pk | NOX Sound | CC0 1.0 |
| EngineIdle_Diesel_Loop.ogg | loop | 3.20 s | mono | -24.0 LUFS int / -10.9 dBFS pk | ReadeOnly | CC0 1.0 |
| EngineStart_01.wav | one-shot | 0.87 s | mono | -18.0 LUFS act / -2.1 dBFS pk | NOX Sound | CC0 1.0 |
| CarDoorClose_01.wav | one-shot | 0.39 s | mono | -19.5 LUFS act / -1.0 dBFS pk | NOX Sound | CC0 1.0 |
| CarDoorOpen_01.wav | one-shot | 0.45 s | mono | -22.4 LUFS act / -1.0 dBFS pk | NOX Sound | CC0 1.0 |
| CarDoorClose_02.wav | one-shot | 0.33 s | mono | -19.2 LUFS act / -1.0 dBFS pk | NOX Sound | CC0 1.0 |
| CarDoorOpen_02.wav | one-shot | 0.42 s | mono | -20.9 LUFS act / -1.0 dBFS pk | NOX Sound | CC0 1.0 |
| CarDoorClose_03.wav | one-shot | 0.34 s | mono | -18.9 LUFS act / -1.0 dBFS pk | NOX Sound | CC0 1.0 |
| CarDoorOpen_03.wav | one-shot | 0.50 s | mono | -22.6 LUFS act / -1.0 dBFS pk | NOX Sound | CC0 1.0 |
| NightCrickets_Loop.ogg | loop | 7.00 s | stereo | -24.0 LUFS int / -10.6 dBFS pk | Dashman71 | CC0 1.0 |
| NightCrickets_Soft_Loop.ogg | loop | 4.20 s | mono | -24.0 LUFS int / -11.1 dBFS pk | rayjensen | CC0 1.0 |
| NightCrickets_Far_Loop.ogg | loop | 4.20 s | mono | -24.0 LUFS int / -7.8 dBFS pk | icuttv | CC0 1.0 |
| NightInsects_Loop.ogg | loop | 4.20 s | mono | -24.0 LUFS int / -12.9 dBFS pk | kslagerman | CC0 1.0 |
| ElectricalHum_Loop.ogg | loop | 8.00 s | mono | -24.0 LUFS int / -12.1 dBFS pk | Zabuhailo (clipped by Jhrushbe) | CC BY 4.0 |
| ClockTick_Wall_Loop.ogg | loop | 3.00 s | mono | -24.0 LUFS int / -1.6 dBFS pk | felix.blume | CC0 1.0 |
| ClockTick_Close_Loop.ogg | loop | 3.00 s | mono | -24.4 LUFS int / -1.0 dBFS pk | life_interconnected | CC0 1.0 |
| ClockTick_Quartz_Loop.ogg | loop | 3.00 s | mono | -24.4 LUFS int / -1.0 dBFS pk | MentalSanityOff | CC0 1.0 |
| ClockTick_WallB_Loop.ogg | loop | 2.99 s | mono | -27.6 LUFS int / -1.0 dBFS pk | Sclolex | CC0 1.0 |
| ClockTick_Fast_Loop.ogg | loop | 2.90 s | mono | -27.0 LUFS int / -1.0 dBFS pk | Adam_N | CC0 1.0 |
| TorchClick_01.wav | one-shot | 0.13 s | mono | -19.0 LUFS act / -1.0 dBFS pk | Rudmer_Rotteveel | CC0 1.0 |
| RelayClick_01.wav | one-shot | 0.19 s | mono | -18.0 LUFS act / -1.8 dBFS pk | bassmosphere (remixed by Jhrushbe) | CC0 1.0 |
| PenClick_01.wav | one-shot | 0.61 s | mono | -24.3 LUFS act / -1.0 dBFS pk | dslrguide (converted by Themias) | CC0 1.0 |
| PenWriting_01.wav | one-shot | 0.86 s | mono | -23.5 LUFS act / -1.0 dBFS pk | PDaefaul (cut by Krunklehorn) | CC0 1.0 |
| PenWriting_02.wav | one-shot | 0.68 s | mono | -21.3 LUFS act / -1.0 dBFS pk | PDaefaul (cut by Krunklehorn) | CC0 1.0 |
| PenWriting_03.wav | one-shot | 0.52 s | mono | -18.7 LUFS act / -1.0 dBFS pk | PDaefaul (cut by Krunklehorn) | CC0 1.0 |
| PenWriting_04.wav | one-shot | 0.50 s | mono | -18.0 LUFS act / -1.3 dBFS pk | PDaefaul (cut by Krunklehorn) | CC0 1.0 |
| PenWriting_05.wav | one-shot | 0.92 s | mono | -18.9 LUFS act / -1.0 dBFS pk | PDaefaul (cut by Krunklehorn) | CC0 1.0 |
| PageTurn_01.wav | one-shot | 0.70 s | mono | -23.8 LUFS act / -1.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| NotebookSetDown_01.wav | one-shot | 0.28 s | mono | -18.0 LUFS act / -3.7 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| PageTurn_02.wav | one-shot | 0.43 s | mono | -24.8 LUFS act / -1.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| NotebookSetDown_02.wav | one-shot | 0.31 s | mono | -18.0 LUFS act / -2.7 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| PageTurn_03.wav | one-shot | 0.23 s | mono | -23.4 LUFS act / -1.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| NotebookSetDown_03.wav | one-shot | 0.25 s | mono | -18.0 LUFS act / -1.2 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| NotebookOpen_01.wav | one-shot | 0.15 s | mono | -18.0 LUFS act / -1.2 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| NotebookClose_01.wav | one-shot | 0.23 s | mono | -20.0 LUFS act / -1.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| PaperRustle_01.wav | one-shot | 0.74 s | mono | -25.7 LUFS act / -1.0 dBFS pk | Spaghetto87 | CC0 1.0 |
| PaperRustle_02.wav | one-shot | 0.62 s | mono | -20.0 LUFS act / -1.0 dBFS pk | Spaghetto87 | CC0 1.0 |
| PaperRustle_03.wav | one-shot | 0.72 s | mono | -30.0 LUFS act / -1.0 dBFS pk | Spaghetto87 | CC0 1.0 |
| PaperRustle_04.wav | one-shot | 0.76 s | mono | -21.9 LUFS act / -1.0 dBFS pk | Spaghetto87 | CC0 1.0 |
| PaperRustle_05.wav | one-shot | 0.61 s | mono | -26.5 LUFS act / -1.0 dBFS pk | Spaghetto87 | CC0 1.0 |
| PaperRustle_06.wav | one-shot | 0.62 s | mono | -19.8 LUFS act / -1.0 dBFS pk | Spaghetto87 | CC0 1.0 |
| KitCaseOpen_01.wav | one-shot | 1.07 s | mono | -21.6 LUFS act / -1.0 dBFS pk | Nox_Sound (edited by themias) | CC0 1.0 |
| KitCaseClose_01.wav | one-shot | 0.57 s | mono | -18.5 LUFS act / -1.0 dBFS pk | Nox_Sound (edited by themias) | CC0 1.0 |
| PouchHandle_01.wav | one-shot | 0.27 s | mono | -21.2 LUFS act / -1.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| PouchHandle_02.wav | one-shot | 0.29 s | mono | -20.2 LUFS act / -1.0 dBFS pk | Kenney (www.kenney.nl) | CC0 1.0 |
| DoorKnob_01.wav | one-shot | 0.63 s | mono | -20.9 LUFS act / -1.0 dBFS pk | BenjaminNelan | CC0 1.0 |
| CashRegister_01.wav | one-shot | 1.94 s | mono | -18.9 LUFS act / -1.0 dBFS pk | Zott820 | CC0 1.0 |
| GlassBreak_01_HQ.wav | one-shot | 0.95 s | mono | -21.0 LUFS act / -1.0 dBFS pk | unfa | CC0 1.0 |
| EngineIdle_InspectorJ_Loop.ogg | loop | 2.20 s | mono | -24.0 LUFS int / -9.5 dBFS pk | InspectorJ (www.jshaw.co.uk) | CC BY 4.0 |
| EngineStart_02.wav | one-shot | 1.62 s | mono | -22.0 LUFS act / -1.0 dBFS pk | InspectorJ (www.jshaw.co.uk) | CC BY 4.0 |
