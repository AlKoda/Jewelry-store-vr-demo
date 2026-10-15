"""Writes README.md for the NightSoundscape pack from sources.json."""
import json
import os
import sys

PACK = sys.argv[1]
IDLE_HP120 = float(sys.argv[2]) if len(sys.argv) > 2 else -21.0  # measured by the QA step (LUFS after a 120 Hz high-pass)
d = json.load(open(os.path.join(PACK, "sources.json")))
F = {e["file"]: e for e in d["files"]}

SECTIONS = [
    ("Exterior beds (loops)", ["NightCity_Loop.ogg", "NightCityDistant_Loop.ogg", "StreetWind_Loop.ogg", "WindWhistleGap_Loop.ogg",
                               "LightRainStreet_Loop.ogg"]),
    ("Burglar alarm bell (loops, original)", ["AlarmBell_Close_Loop.wav", "AlarmBell_Street_Loop.ogg", "AlarmBell_Interior_Loop.wav"]),
    ("Interior machines (loops)", ["HVACRoomTone_Loop.ogg", "HVACVent_Loop.wav", "FluorescentBuzz_Loop.wav", "FridgeCompressor_Loop.wav"]),
    ("Police car and radio", ["PoliceCarIdle_Loop.wav", "PoliceCarRadio_Loop.ogg"] + [f"RadioChatter_{i:02d}.wav" for i in range(1, 6)]
     + [f"RadioSquelch_{i:02d}.wav" for i in range(1, 4)] + [f"RadioBeep_{i:02d}.wav" for i in range(1, 4)]
     + ["CarDoorOpen_01.wav", "CarDoorSlam_01.wav"]),
    ("Distant city events (one-shots)", ["SirenWailPassDistant_01.wav", "SirenWailYelpPass_02.wav", "SirenDistantCity_03.wav",
                                         "SirenDistantPass_04.wav", "CarPassWet_01.wav", "CarPass_02.wav", "CarPass_03.wav"]
     + [f"DogBarkDistant_{i:02d}.wav" for i in range(1, 5)]),
    ("Footsteps and glass (one-shots)", [f"FootstepHard_{i:02d}.wav" for i in range(1, 7)] + [f"FootstepGlass_{i:02d}.wav" for i in range(1, 6)]
     + [f"GlassCrunch_{i:02d}.wav" for i in range(1, 5)] + [f"GlassShardsTinkle_{i:02d}.wav" for i in range(1, 5)]),
    ("Tools and handling (one-shots)", [f"EvidenceMarkerPlace_{i:02d}.wav" for i in range(1, 4)] + [f"ConePlace_{i:02d}.wav" for i in range(1, 4)]
     + [f"TapeUnroll_{i:02d}.wav" for i in range(1, 3)] + [f"TapeStretchSnap_{i:02d}.wav" for i in range(1, 3)]
     + ["BarrierTapeFlutter_Loop.wav", "CameraShutterDSLR_01.wav", "CameraShutterDSLR_02.wav", "FlashlightClick_01.wav",
        "FlashlightClick_02.wav", "FlashlightClickHalf_01.wav"] + [f"KeysJingle_{i:02d}.wav" for i in range(1, 4)]
     + ["ZipperOpen_01.wav", "ZipperOpen_02.wav", "ZipperClose_01.wav"]),
    ("Interior doors (one-shots)", [f"DoorCreak_{i:02d}.wav" for i in range(1, 4)] + ["DoorOpen_01.wav", "DoorOpen_02.wav",
                                                                                     "DoorClose_01.wav", "DoorClose_02.wav"]),
]
listed = [f for _, fs in SECTIONS for f in fs]
assert sorted(listed) == sorted(F), (set(F) - set(listed), set(listed) - set(F))


def origin(e):
    s = e["source"]
    if s["type"] == "original":
        return "original"
    if s["type"] == "hybrid":
        return "recorded + original"
    return "recorded"


def row(e):
    ch = "mono" if e["channels"] == 1 else "stereo"
    fmt = "WAV" if e["format"].startswith("WAV") else "OGG"
    lvl = f"{e['integrated_lufs']:.0f} LUFS" if e["kind"] == "loop" else f"{e['sample_peak_dbfs']:.0f} dBFS pk"
    return (f"| {e['file']} | {e['kind']} | {e['seconds']:.2f} s | {fmt} {ch} | {lvl} | {origin(e)} | {e['description']} | "
            f"{e['placement']} | {e['suggested_volume']} | {e['spatial']} |")


total = sum(e["bytes"] for e in d["files"])
out = []
out.append("# NightSoundscape\n")
out.append("Sound for the night burglary scene: exterior city/wind/rain beds, the burglar alarm bell, interior machine room "
           "tones, the parked police car (idle, radio traffic, doors), distant sirens, cars and dogs, footsteps on tile and "
           "on broken glass, glass debris, and the handling sounds of the investigator's tools (markers, cones, barrier "
           "tape, camera, torch, keys, kit-bag zips). 86 clips, %.1f MB. Prepared October 14, 2026, revised October 15, 2026 "
           "after review; **not wired into the scene and not auditioned in Unity or on headphones** (unverified).\n" % (total / 1e6))
out.append("It complements Prototype/ThirdParty/NightAudio (CameraClick, FlashlightSwitch, PanelSelect, ToolSetDown, "
           "DistantAir_Loop): CameraShutterDSLR_0x and FlashlightClick_0x are realistic alternatives, not duplicates; keep "
           "only one set active per action.\n")
out.append("## Revision October 15, 2026\n")
out.append("- **PoliceCarIdle_Loop.wav** replaced: the first loop (seth-m 'car idle') was 82 %% sub-bass below 40 Hz and "
           "almost inaudible on Quest 2 speakers. The new 14.4 s loop is a Ford 5.4 V8 idling at about 1000 rpm (Therancher01, CC0, "
           "via Jxstrr0/350Chev) with a 45 Hz 4th-order high-pass; it measures -20 LUFS full range and %.1f LUFS through a 120 Hz "
           "high-pass (small-speaker proxy).\n"
           "- **RadioChatter_01..05.wav and PoliceCarRadio_Loop.ogg** rebuilt with a source-filter speech synthesizer "
           "(Generator/speech_babble.py): 4-6 syllables/s, stop gaps and bursts, fricatives, pitch accents and phrase pauses, "
           "then a narrow-band FM radio chain. Measured: envelope modulation peak 2.4-5.8 Hz, envelope range (P90-P10 of 10 ms "
           "frames) 22-30 dB, 1-3 kHz within 3 dB of 250 Hz-1 kHz (first version: 0.3-1.1 Hz, 9-18 dB, -2 to -12 dB). "
           "RadioSquelch and RadioBeep are bit-identical to the first release.\n"
           "- **FootstepGlass_01..05.wav** remixed: glass layers 8 dB lower with their rare large spikes soft-limited, so the "
           "heel thud (60 Hz-1.5 kHz) is 2-6 dB above the crunch (2-20 kHz) and 55-77 %% of the energy is below 1 kHz "
           "(first version 5-10 %%).\n"
           "- **DogBarkDistant_01..03.wav** are now recorded as CC BY 3.0 (attribution required) because the licence evidence "
           "for Kinoton's recording conflicts; see Licences below.\n"
           "- Every other file is unchanged.\n" % IDLE_HP120)
out.append("## Licences and attribution\n")
out.append("All recordings are CC0 1.0 except **DogBarkDistant_01..03.wav**, which are treated as **CC BY 3.0** until the "
           "Freesound page is checked. Put this line in the in-app credits and keep it in Docs/ASSET_REGISTER.md:\n\n"
           "> \"Dog Barking in Neighborhood\" by Kinoton (https://freesound.org/s/516904/), licensed CC BY 3.0 "
           "(https://creativecommons.org/licenses/by/3.0/); trimmed, filtered and normalised.\n\n"
           "If the attribution cannot be shown, use DogBarkDistant_04.wav (rvandemark, CC0) alone. The synthesized files are "
           "project-owned. Full provenance is in LICENSE.txt and sources.json.\n")
out.append("## What is recorded and what is synthesized\n")
out.append("- **Recorded (Freesound field recordings via GitHub mirrors, CC0 except the CC BY 3.0 dog clips "
           "DogBarkDistant_01..03, plus Kenney RPG Audio, CC0):** city "
           "beds, wind, rain, recorded HVAC room tone, car idle, car doors, car pass-bys, sirens (two of them re-spatialised "
           "here with a moving-source distance model), dogs, hard footsteps, interior door creaks/open/close.\n"
           "- **Original synthesis (project-owned, reproducible with Generator/):** the alarm bell (physical modal model whose "
           "mode ratios were measured from a CC0 bell recording), HVAC vent, fluorescent buzz, fridge, all police-radio "
           "sounds (formant-synthesized speech-like babble without words), glass shards/crunch, the glass layer of FootstepGlass, marker, cone, "
           "tape, camera shutter, torch click, keys and zips.\n"
           "- Every licence and source is in LICENSE.txt; per-file hashes, cut points and processing in sources.json.\n")
out.append("## Format and import notes (Unity 6000.6, Built-in, no packages)\n")
out.append("- All files are 44.1 kHz. One-shots are mono 16-bit PCM WAV normalised to about -3 dBFS sample peak (radio "
           "beeps -6 dBFS). Loops are seamless and normalised to -20 LUFS integrated; stereo loops and loops longer than 30 s "
           "are Ogg Vorbis (q5-q6), the remaining mono loops (12-24 s) are WAV. Integrated LUFS of clips under 0.4 s reads -70 "
           "(gating) and is meaningless.\n"
           "- Suggested import: one-shots < 2 s: Load Type *Decompress On Load*, Compression *PCM* or *ADPCM*, Preload on. "
           "Loops and long one-shots (sirens, dog sequence): *Compressed In Memory*, *Vorbis* quality 60-70; the 60 s beds "
           "may use *Streaming*. Force To Mono for every 3D source (the stereo beds are 2D only). Leave Ambisonic off.\n"
           "- Loop clips: set AudioSource.loop = true and start each at a random time (`source.time = Random.Range(0, "
           "clip.length)`) so layers never line up. The loops were rendered/cross-faded to wrap without a click; check the "
           "wrap once in Unity because Vorbis re-encoding can change the boundary slightly.\n"
           "- One-shots: play through a small pool of AudioSources with PlayOneShot, random pick among the numbered "
           "variations (never the same twice in a row), pitch 0.94-1.06 and volume 0.85-1.0 per play.\n"
           "- 3D settings that suit a 16 x 12 m shop: Spatial Blend 1, Logarithmic rolloff, min distance 0.5-2 m (2 m for "
           "the bell), max distance 15-30 m (60 m for the bell), Doppler 0 for static emitters. Distant events (sirens, "
           "dogs, far cars) are best as 2D with Spatial Blend 0-0.3 and baked distance character.\n"
           "- Mains hum in the synthesized machine loops is 50 Hz (UK/EU); regenerate with `MAINS=60` for 60 Hz regions.\n")
out.append("## Suggested mix for the night store\n")
out.append("| Zone | Always-on layers (volume) | Random events |\n|---|---|---|\n"
           "| Street / pavement | NightCity_Loop (0.45), StreetWind_Loop (0.35), AlarmBell_Street_Loop (0.6, crossfade to the 3D AlarmBell_Close_Loop on the facade by distance), PoliceCarIdle_Loop and PoliceCarRadio_Loop at the car, BarrierTapeFlutter_Loop on each tape span, optional LightRainStreet_Loop | Siren one-shots every 2-6 min, CarPass/CarPassWet every 20-60 s, DogBarkDistant every 1-4 min |\n"
           "| Shop floor | NightCity_Loop low-passed (~1.5 kHz, 0.25), AlarmBell_Interior_Loop (0.6), HVACRoomTone_Loop (0.25) or HVACVent_Loop at the vents, WindWhistleGap_Loop at the forced doors (0.2) | same events, quieter and low-passed; GlassShardsTinkle rarely near broken bays |\n"
           "| Rear office / safe room | HVACRoomTone_Loop, FluorescentBuzz_Loop at the fitting, FridgeCompressor_Loop in the kitchenette, AlarmBell_Interior_Loop (0.35) | KeysJingle, DoorCreak/Open/Close |\n")
out.append("Interaction mapping: footsteps FootstepHard (tile, pavement) or FootstepGlass (inside the glass-debris area; "
           "the step carries the footfall, so GlassCrunch at about -6 dB on every other step is optional extra debris); marker placed EvidenceMarkerPlace; cone ConePlace; barrier "
           "tape TapeUnroll while dragging and TapeStretchSnap when a span is finished; photo CameraShutterDSLR; torch/UV "
           "torch FlashlightClick_01/_02; kit bag ZipperOpen/Close; scene lamp set-down can reuse ConePlace at 0.6 volume or "
           "NightAudio/ToolSetDown.\n")
out.append("The alarm bell is the key atmosphere element: it is designed to ring continuously; a training scenario "
           "might let the instructor silence it (fade over 0.3 s, then a short metallic decay is natural). Nothing here "
           "signals a right or wrong action.\n")
for title, fs in SECTIONS:
    out.append(f"## {title}\n")
    out.append("| File | Type | Length | Format | Level | Origin | What it is | Suggested placement | Volume | 2D/3D |\n"
               "|---|---|---|---|---|---|---|---|---|---|")
    for f in fs:
        out.append(row(F[f]))
    out.append("")
out.append("## Regenerating\n")
out.append("Generator/ holds the scripts used (Python 3, numpy, scipy, ffmpeg): make_alarm.py, make_room.py, make_radio.py "
           "(with speech_babble.py, the formant speech synthesizer), make_foley.py and make_sirens.py write the original sounds; "
           "process_recorded.py prepares the recordings from the decoded sources listed in sources.json (download them from the "
           "repositories and commits given there; `process_recorded.py SRC OUT idle` re-renders only the police-car idle). Seeds are "
           "fixed, so the synthesized files are reproducible bit-for-bit given the same numpy/scipy/ffmpeg versions. "
           "Unity imports the .py files as plain assets; they are not compiled.\n")
out.append("## Known limits\n")
out.append("- Several recordings come from 128 kbps Freesound previews re-encoded by the mirror projects, so their top "
           "octave is soft; acceptable for distant/background use. For a final release, replace them with the original "
           "WAV downloads from the Freesound pages listed (same CC0 licence).\n"
           "- No CC0 recording of a burglar alarm bell at good quality was reachable, so the bell is synthesized; a recorded "
           "bell can replace AlarmBell_Close_Loop later without code changes.\n"
           "- The radio chatter is deliberately unintelligible synthetic speech (no words, no real dispatcher). Its rhythm, "
           "dynamics and spectrum now measure like radio speech, but it has not been auditioned; it is meant to be heard as "
           "radio traffic from a few metres away, not as close-up dialogue. If it still sounds artificial on the headset, lower "
           "it to 0.25 and keep the squelch/beep clips, or replace it with a recorded CC0 take that identifies no real person.\n"
           "- PoliceCarIdle_Loop.wav comes from a 96 kbps MP3 made from a Freesound HQ preview, so its top octave is soft; "
           "fine at a few metres outdoors.\n"
           "- Nothing here has been heard in Unity or VR yet; levels and rolloff need a listening pass with the Quest 2.\n")
open(os.path.join(PACK, "README.md"), "w").write("\n".join(out))
print("README written", len("\n".join(out)))
