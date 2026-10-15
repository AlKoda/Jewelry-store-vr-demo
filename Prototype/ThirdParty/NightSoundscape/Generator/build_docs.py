"""Writes sources.json and README.md for the NightSoundscape pack from the shipped files + the table below."""
import hashlib
import json
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(__file__))
import au  # noqa: E402

PACK = sys.argv[1]
DATE = "2026-10-14"
REVISED = "2026-10-15"

CC0 = ("CC0-1.0", "https://creativecommons.org/publicdomain/zero/1.0/")
CCBY3 = ("CC-BY-3.0", "https://creativecommons.org/licenses/by/3.0/")
PROJ = ("Project-owned original (LicenseRef-project-original)", None)

REPOS = {
    "atmo": ("https://github.com/timmywheels/atmospherics", "743012245d9d567d9397295758fb87f069370a0d",
             "per-sound list design/sounds/SOURCES.md ('Every recording is public domain or CC0'); repository code MIT"),
    "hill": ("https://github.com/Aarymanv/hillbomb-sf", "937d362243a56fd7aed001e36dc5051e1959a04d",
             "public/assets/ASSET_LICENSES.md and dev/audio.pipeline/fragments/*.json (license 'CC0 1.0' per file; pipeline accepts only Freesound pages stating Creative Commons 0)"),
    "lul": ("https://github.com/scenesua/Lullaby-scene", "74fce68956b5d03a2582a4d76798dfc8b77ca7ed",
            "app/src/main/assets/ambience/manifest/external_licenses.json (license 'CC0-1.0', license_status 'verified', per asset with sha256)"),
    "upp": ("https://github.com/dominik-wojciak/Unity-Physics-Project", "d39bca7ad994f34baa813624cd420f9c9bfd943f",
            "file only (unmodified Freesound download name); licence statement taken from https://github.com/Dhruv-Mishra/VoxelGame/blob/1d0730642b44cd24182980a8da32b93b40dfc8ff/assets/sfx/CREDITS.txt ('car idle' by seth-m - CC0)"),
    "chev": ("https://github.com/Jxstrr0/350Chev", "b80c73b490962d4de5d47cf49e99e68a21c968dc",
             "sounds/CREDITS.md: per file 'Ford F350 5.4 V8 startup and idle | Therancher01 | CC0 1.0 | Freesound 610772', "
             "'Raw license fields as read on 2026-10-10: fs_610772 ... Freesound page license link: \"Creative Commons 0\"', and "
             "'License of the derived files in this folder: each .mp3 keeps the license of its source - CC0 / public-domain sources stay CC0'"),
    "jra": ("https://github.com/series-ai/jam-ready-assets", "bdce326b2d7eee85524c08c751faf4804d453f3d",
            "kenney-rpg-audio/audio/License.txt (Kenney RPG Audio 1.0, CC0); Git LFS objects fetched from media.githubusercontent.com and checked against the LFS pointer sha256"),
}

FS = lambda i: f"https://freesound.org/s/{i}/"  # noqa: E731

# file -> (kind, group, desc, placement, volume, spatial, origin dict)
G = {}


def add(files, kind, desc, place, vol, spatial, origin):
    for f in files:
        G[f] = dict(kind=kind, desc=desc, place=place, vol=vol, spatial=spatial, origin=origin)


def rec(repo, path, src_sha, fs_id, title, author, changes, extra=None):
    o = dict(type="recording", repo=REPOS[repo][0], commit=REPOS[repo][1], repo_path=path, repo_file_sha256=src_sha,
             license_evidence=REPOS[repo][2], original=FS(fs_id) if fs_id else None, title=title, author=author,
             license=CC0[0], license_url=CC0[1], changes=changes)
    if extra:
        o.update(extra)
    return o


def proj(script, changes, ref=None, extra=None):
    gen = " + ".join(f"Generator/{p.strip()}" for p in script.split("+"))
    o = dict(type="original", author="Jewelry-store-vr-demo project (generated with Claude, Anthropic)",
             license=PROJ[0], license_url=None, generator=gen, changes=changes)
    if ref:
        o["reference"] = ref
    if extra:
        o.update(extra)
    return o


# ------------------------------------------------------------------ beds
add(["NightCity_Loop.ogg"], "loop",
    "Night city bed: semi-distant downtown traffic hum at 11 pm heard from a 4th-floor window, occasional far car.",
    "2D/stereo street bed outside and (low-passed by the shopfront) inside; main exterior ambience.", 0.45, "2D",
    rec("atmo", "Atmospherics/Sounds/cityNight.m4a", "1d6fd6dff49b98422f3fef85646c83d0155b85400f8233da1172559d9f9f56f4",
        330427, "Downtown LA, Little Tokyo, late night semi distant traffic.wav", "janbezouska",
        "AAC decoded; 60 s from 22 s with a 3 s equal-power seam crossfade; 30 Hz high-pass; -20 LUFS; Ogg Vorbis q6 44.1 kHz stereo",
        {"also_listed_cc0_by": "https://github.com/Aarymanv/hillbomb-sf (ASSET_LICENSES.md, amb_city_night)"}))
add(["NightCityDistant_Loop.ogg"], "loop",
    "Alternative city bed: distant evening traffic wash from a 5th-floor balcony; steadier, no close events.",
    "2D bed for the rear office/side rooms or layered very low under NightCity_Loop.", 0.3, "2D",
    rec("atmo", "Atmospherics/Sounds/cityHum.m4a", "f3ca223dbd3482b1f2e5b59a089c1acbec171201a7be1d67e5ecc31205b52299",
        371250, "Distant city evening traffic, 5th floor balcony", "risto_alcinov",
        "AAC decoded; 60 s from 12 s, 3 s seam crossfade; 30 Hz high-pass; -20 LUFS; Ogg Vorbis q6 stereo",
        {"also_listed_cc0_by": "https://github.com/scenesua/Lullaby-scene (external_licenses.json, hood_journey_bed_001)"}))
add(["StreetWind_Loop.ogg"], "loop", "Quiet night wind: soft broadband wind with slow gusts, no birds or traffic.",
    "2D exterior layer under the city bed (pavement, kerb, police car).", 0.35, "2D",
    rec("atmo", "Atmospherics/Sounds/wind.m4a", "18f824d6fa7565f0bd2a3ff87ba283264ef728f8471a41a6857360a261377efb",
        521736, "wind (title as listed in SOURCES.md)", "Fission9",
        "AAC decoded; 48 s from 2 s, 3 s seam crossfade; 30 Hz high-pass; -20 LUFS; Ogg Vorbis q6 stereo"))
add(["WindWhistleGap_Loop.ogg"], "loop", "Wind howling/whistling through a gap at a window: resonant, moaning.",
    "3D source (mono-folded by Unity) at the forced entrance doors / broken glass bay; small radius, low volume.", 0.2, "3D",
    rec("atmo", "Atmospherics/Sounds/howl.m4a", "927c06a8ee0de23f4cd460ec0dc81459aca1113a0c1a2215f066029710528fda",
        117610, "wind howling at a window (description in SOURCES.md)", "swiftoid",
        "AAC decoded; 45 s from 2 s, 3 s seam crossfade; 30 Hz high-pass; -20 LUFS; Ogg Vorbis q6 stereo"))
add(["LightRainStreet_Loop.ogg"], "loop", "Optional light rain on a city street at night, recorded from an open window.",
    "2D exterior bed for a wet-night variant; inside the shop play it low and low-passed.", 0.4, "2D",
    rec("hill", "public/assets/audio/amb/rain_light.mp3", "6d565ce0a36357a499397b6b908244fb79190f7ea893da3a63b783ba7aca3ff4",
        380651, "Light rain on street.wav", "BonnyOrbit",
        "MP3 decoded; existing 30 s loop region (0.25-30.25 s) re-closed as a 29 s loop with a 1 s seam crossfade; 60 Hz high-pass; -20 LUFS; Ogg Vorbis q6 stereo",
        {"also_listed_cc0_by": "https://github.com/openOMSI-org/openOMSI (tools/ambience/CREDITS.md)"}))
add(["HVACRoomTone_Loop.ogg"], "loop", "Recorded air-conditioning unit: steady broadband air rush with a low machine hum.",
    "Interior room tone for the whole shop (2D, low) or a 3D source in the rear office ceiling.", 0.25, "2D or 3D",
    rec("lul", "app/src/main/assets/ambience/submarine_journey/continuous/submarine_journey_arrival_001.ogg",
        "f657b2d2bb6777dd751bb48486a57bcaf7a99b9f7b3994307205e5d7d5b19dee", 438724, "G45-22 Submarine Air Conditioner", "craigsmith",
        "Opus decoded and folded to mono (the source was mono duplicated); 45 s from 2.5 s, 3 s seam crossfade; 35 Hz high-pass; -20 LUFS; Ogg Vorbis q5 mono"))
add(["PoliceCarIdle_Loop.wav"], "loop",
    "V8 petrol engine idling at about 1000 rpm (a police car's raised idle), exterior: broadband exhaust burble and engine mechanics. 14.4 s, seamless, no audible 3.6 s repeat.",
    "3D source at the police car's bonnet/exhaust, min distance 1 m, max 25 m.", 0.5, "3D",
    rec("chev", "sounds/v8x54_idle.mp3", "e2fe4ed9baaef8752b19f0963041825a46b71eb3f5e2a5704db7b57108101610",
        610772, "Ford F350 5.4 V8 startup and idle", "Therancher01",
        "Upstream (350Chev): Freesound HQ preview, 5-28 s window time-warped to a steady 999 rpm, cut to whole engine cycles and looped, 96 kbps mono MP3. "
        "Here: MP3 decoded; DC removed; 45 Hz 4th-order high-pass (circular); the 3.60 s loop tiled 4x and read through a periodic time warp "
        "(+-0.6 % idle-speed hunting) with a +-0.7 dB periodic level drift, cubic interpolation; -20 LUFS (peak about -4.5 dBFS); mono 16-bit WAV. "
        "Replaces the first-release loop made from 'car idle' by seth-m (freesound 269771), which was 82 % sub-bass below 40 Hz.",
        {"revised": REVISED}))
# ------------------------------------------------------------------ sirens
add(["SirenWailPassDistant_01.wav"], "one-shot",
    "Police siren (electronic wail) passing two blocks away behind buildings: Doppler, distance, occlusion, street echo. 26 s.",
    "2D ambient one-shot every 2-6 minutes, random start; or a 3D emitter 150+ m away.", 0.35, "2D",
    rec("hill", "public/assets/audio/loops/siren_wail.mp3", "3a8a38261c47be23673524d699958b043bfededbecad8574bc2605ee9f318807",
        157867, "Whelen Wail.wav (electronic police siren, wail tone)", "Jefflix",
        "Loop region tiled; moving-source model (210 m perpendicular, 17 m/s): time-varying delay (Doppler), 1/r spreading, air absorption, building occlusion low-pass, synthetic street-canyon reverb; 180 Hz high-pass; fades; -3 dBFS peak; mono 16-bit WAV"))
add(["SirenWailYelpPass_02.wav"], "one-shot",
    "Police siren on a nearer street (~90 m) switching wail -> yelp -> wail as it clears a junction. 22 s.",
    "2D ambient one-shot; rarer than _01.", 0.35, "2D",
    rec("hill", "public/assets/audio/loops/siren_wail.mp3 + loops/siren_yelp.mp3",
        "3a8a38261c47be23673524d699958b043bfededbecad8574bc2605ee9f318807 / 0c4bc4515914777ea7f9ee85c43a1542870240828048d65d4c86dd7d3b24423f",
        "157867", "Whelen Wail.wav + Whelen Yelp.wav (freesound 157867, 157866)", "Jefflix",
        "Wail and yelp loops crossfaded (8-13 s yelp); moving-source distance model as _01 (90 m, 13 m/s); -3 dBFS peak; mono 16-bit WAV",
        {"original_2": FS(157866)}))
add(["SirenDistantCity_03.wav"], "one-shot",
    "Real distant North American police siren echoing between city buildings. 18 s.",
    "2D ambient one-shot.", 0.35, "2D",
    rec("hill", "public/assets/audio/amb/distant_siren.mp3", "ba3b9c3ceeb9f1b94a40f45f53cc842edb0c90f151b2755cebe795501c4ddd46",
        635741, "siren police north american distant city echo changes behind buildings natural nice +bassy skyline lowcut to taste.flac", "kyles",
        "MP3 decoded; 40 Hz high-pass; 0.5 s/1 s fades; -3 dBFS peak; mono 16-bit WAV"))
add(["SirenDistantPass_04.wav"], "one-shot", "Police siren wail passing at a distance with fade-out (32 kHz band-limited source). 15 s. Dense tone: about 8 dB louder than the other sirens at the same peak.",
    "2D ambient one-shot.", 0.15, "2D",
    rec("lul", "app/src/main/assets/ambience/hood_journey/events/hood_siren_event_001.ogg",
        "c349e9aa0a745c0a618e18ac1cd51a90dd8930f4a1093253dacc35d87d7cf65a", 746302, "Police Siren", "TitanKaempfer",
        "Vorbis decoded and resampled 32 -> 44.1 kHz; 40 Hz high-pass; fades; -3 dBFS peak; mono 16-bit WAV"))
# ------------------------------------------------------------------ alarm bell (original)
BELL_REF = ("Mode ratios and levels measured from 'fire alarm bell' by msx2plus (freesound 678345, CC0) as mirrored in "
            "https://github.com/Dhruv-Mishra/VoxelGame/blob/1d0730642b44cd24182980a8da32b93b40dfc8ff/assets/sfx/alarm_loop.mp3 "
            "(sha256 2e3a37454e07172dc5acbb3ceb8eabf8c3918ab5150a23edcf5a9422295b5d86); no recorded audio used")
add(["AlarmBell_Close_Loop.wav"], "loop",
    "Burglar alarm bell (6-inch trembler gong in an outdoor bell box), heard a few metres away; nearly dry.",
    "3D source on the shop facade above the fascia, min distance 2 m, max 60 m, Logarithmic rolloff; the key atmosphere element.", 0.8, "3D",
    proj("make_alarm.py", "Modal synthesis: 12 split (doublet) gong modes + 22 weak inharmonic modes, trembler hammer at 18.6 strikes/s with timing/force jitter, contact damping, hammer bounce, armature buzz and rattle; bell-box cover EQ; short early reflections; rendered periodic (seamless); -20 LUFS; mono 16-bit WAV", BELL_REF))
add(["AlarmBell_Street_Loop.ogg"], "loop",
    "Same bell heard from across the street: air absorption, facade flutter echoes and street-canyon reverb (stereo).",
    "2D layer while the player is outside, crossfaded with the 3D close source by distance.", 0.6, "2D",
    proj("make_alarm.py", "As Close plus 7.5 kHz air-absorption low-pass and circular convolution with a synthetic 1.9 s street IR; -20 LUFS; Ogg Vorbis q6 stereo", BELL_REF))
add(["AlarmBell_Interior_Loop.wav"], "loop",
    "Same bell heard inside the shop through the shopfront: muffled (2.6 kHz low-pass) with interior room reverb.",
    "2D/3D source inside near the entrance while the player is in the shop.", 0.6, "2D or 3D",
    proj("make_alarm.py", "As Close plus transmission low-pass/EQ and a synthetic 0.75 s room IR (circular); -20 LUFS; mono 16-bit WAV", BELL_REF))
# ------------------------------------------------------------------ interior machines (original)
add(["HVACVent_Loop.wav"], "loop", "Ceiling supply diffuser: airflow hiss with turbulence, blower blade-pass tone and faint 100 Hz hum.",
    "3D sources at 2-3 ceiling vents, min 0.5 m, max 8 m.", 0.3, "3D",
    proj("make_room.py", "Periodic shaped noise + blade-pass (24 Hz x 38 blades) and mains-hum partials, slow turbulence modulation; 24 s seamless; -20 LUFS; mono 16-bit WAV (50 Hz mains)"))
add(["FluorescentBuzz_Loop.wav"], "loop", "Magnetic-ballast fluorescent fitting buzz (100 Hz and harmonics) with lamination rattle and flicker.",
    "3D source on the rear-office or side-room fluorescent fittings, min 0.3 m, max 5 m.", 0.25, "3D",
    proj("make_room.py", "Saturated 100 Hz waveform + half-cycle rattle ticks + fitting resonances + faint discharge hiss; 16 s seamless; -20 LUFS; mono 16-bit WAV"))
add(["FridgeCompressor_Loop.wav"], "loop", "Small fridge compressor running: 50/100 Hz motor hum, piston knocks through the shell, refrigerant gurgle.",
    "3D source in the staff kitchenette/office, min 0.5 m, max 6 m.", 0.3, "3D",
    proj("make_room.py", "Mains-hum partials + 48.3 Hz piston impulse train through shell resonances + gurgle bursts + condenser fan noise; 20 s seamless; -20 LUFS; mono 16-bit WAV"))
# ------------------------------------------------------------------ radio (original)
add([f"RadioChatter_{i:02d}.wav" for i in range(1, 6)], "one-shot",
    "Police-radio transmission: unintelligible synthetic speech (no words, no real voice) at 4-6 syllables/s through a narrow-band FM radio chain, key-up click and squelch tail. _01/_02 dispatcher at a console (male/female), _03-_05 officers in cars (_03/_05 end with a beep).",
    "3D source at the police car / officer radio; random 5-20 s gaps.", 0.4, "3D",
    proj("make_radio.py + speech_babble.py", "Klatt-style source-filter synthesis (glottal pulses with jitter/shimmer, 5 time-varying formants, nasal pole/zero, fricative and burst noise) of random syllables with stress, pitch accents, declination and phrase pauses; radio chain: mic presence, AGC compressor, 750 us pre-emphasis, deviation limiter, 300-3000 Hz channel, FM hiss, de-emphasis, small-speaker EQ; squelch tail; -3 dBFS peak; mono 16-bit WAV. Rebuilt October 15, 2026 (the first version droned: 0.3-1.5 Hz envelope rhythm, 8-10 dB envelope range).",
         extra={"revised": REVISED}))
add([f"RadioSquelch_{i:02d}.wav" for i in range(1, 4)], "one-shot", "Radio squelch burst (carrier noise tail) with key click.",
    "Police car / radio, between transmissions.", 0.35, "3D", proj("make_radio.py", "Band-passed noise burst with click; -3 dBFS peak; mono WAV"))
add([f"RadioBeep_{i:02d}.wav" for i in range(1, 4)], "one-shot",
    "Generic radio tones: _01 rising talk-permit chirp, _02 double acknowledge beep, _03 single long tone (not any manufacturer's tones).",
    "Police car / radio.", 0.3, "3D", proj("make_radio.py", "Sine tones through the radio chain; -6 dBFS peak; mono WAV"))
add(["PoliceCarRadio_Loop.ogg"], "loop", "60 s loop of radio traffic: dispatcher calls with officer answers, a few lone overs and kerchunks over a faint hiss (the 5 chatter takes plus 5 loop-only takes).",
    "3D source inside the parked police car (door open) or on an officer; simplest wiring.", 0.35, "3D",
    proj("make_radio.py + speech_babble.py", "Call/answer exchanges (0.45-1.1 s answer gaps, 3.5-8.5 s between exchanges) on a periodic hiss bed, nothing straddles the loop point; 150 Hz high-pass; -20 LUFS, peak ceiling -2 dBFS; Ogg Vorbis q5 mono. Rebuilt October 15, 2026 with the new chatter.",
         extra={"revised": REVISED}))
# ------------------------------------------------------------------ vehicles, dog
add(["CarDoorOpen_01.wav"], "one-shot", "Car door handle, latch and door opening (recorded in a car park; some reverb).",
    "Police car door, 3D.", 0.6, "3D",
    rec("lul", "app/src/main/assets/ambience/hood_journey/events/hood_car_door_event_001.ogg",
        "99e3416ac00e53ae13e67973c5c2dcd855c324e0a019db13985a0683582d61e2", 721619, "Opening and Closing car door in parkade", "SduggySounds",
        "Opus decoded; cut 2.80-4.90 s; 40 Hz high-pass; fades; -3 dBFS peak; mono WAV"))
add(["CarDoorSlam_01.wav"], "one-shot", "Car door closed firmly (slam) with a short car-park tail.", "Police car door, 3D.", 0.7, "3D",
    rec("lul", "app/src/main/assets/ambience/hood_journey/events/hood_car_door_event_001.ogg",
        "99e3416ac00e53ae13e67973c5c2dcd855c324e0a019db13985a0683582d61e2", 721619, "Opening and Closing car door in parkade", "SduggySounds",
        "Opus decoded; cut 6.62-8.10 s; 40 Hz high-pass; fades; -3 dBFS peak; mono WAV"))
add(["CarPassWet_01.wav"], "one-shot", "Single car passing on a wet road (tyre spray).", "3D on the street lane or 2D, every 20-60 s.", 0.4, "3D",
    rec("hill", "public/assets/audio/sfx/passby_wet_1.mp3", "bdadda909fbfd1c59de1f0db9f33abeff31897f1084b048556cd31653bd38a37",
        462862, "Passing Car (Wet road)", "Breviceps", "MP3 decoded; fades; -3 dBFS peak; mono WAV"))
add(["CarPass_02.wav"], "one-shot", "Car passing by at moderate speed.", "3D on the street lane or 2D.", 0.35, "3D",
    rec("lul", "app/src/main/assets/ambience/hood_journey/events/hood_car_pass_event_001.ogg",
        "54ee5a5f9a3b6d6001322ae61043d0051b895da94a95ccf9c8abaae69f347f2e", 538687, "Car passing by", "colorsCrimsonTears",
        "Opus decoded; fades; -3 dBFS peak; mono WAV"))
add(["CarPass_03.wav"], "one-shot", "Two cars passing slowly.", "3D on the street lane or 2D.", 0.35, "3D",
    rec("hill", "public/assets/audio/sfx/passby_2.mp3", "42596196fc3cd0c97931bece2392ca14e81222f20c71549a19b7f5d9b08a4a19",
        237338, "Car by slow Civic and Volvo DonFX.wav", "Donmann", "MP3 decoded; fades; -3 dBFS peak; mono WAV"))
KIN = "Dog Barking in Neighborhood"
for f, a, b in [("DogBarkDistant_01.wav", 0.50, 1.45), ("DogBarkDistant_02.wav", 1.98, 3.50), ("DogBarkDistant_03.wav", 0.45, 9.20)]:
    add([f], "one-shot", {"DogBarkDistant_01.wav": "Single distant dog bark in a neighbourhood.",
                          "DogBarkDistant_02.wav": "Distant dog barking twice.",
                          "DogBarkDistant_03.wav": "Distant dog barking six times over 8.7 s."}[f],
        "2D ambient one-shot every 1-4 minutes (or 3D far behind the buildings across the street).", 0.25, "2D",
        rec("lul", "app/src/main/assets/ambience/hood_journey/events/hood_dog_event_001.ogg",
            "7d1df05dfb415b43444fe15fdc91cda3c6b2d0d2a2526152cebed9fef5dd4b23", 516904, KIN, "Kinoton",
            f"Changes: trimmed, filtered, normalised (Opus decoded; cut {a:.2f}-{b:.2f} s; 40 Hz high-pass; fades; -3 dBFS peak; mono WAV)",
            {"license": CCBY3[0], "license_url": CCBY3[1],
             "attribution": f"\"{KIN}\" by Kinoton (https://freesound.org/s/516904/), CC BY 3.0 (https://creativecommons.org/licenses/by/3.0/); trimmed, filtered and normalised",
             "attribution_required": True,
             "license_note": "Licence evidence conflicts and the Freesound page could not be opened: CC0 per scenesua/Lullaby-scene external_licenses.json "
                             "(license_status 'verified') and rustemd02/URMAN game/assets/audio/act1/sound_mood/credits.json; CC BY 3.0 per "
                             "NickJVaccaro/valleyofshadow-site src/assets/data/asset-credits.json (attributionRequired 'y'). Treated as CC BY 3.0 "
                             "(the stricter of the two) until the Freesound page is checked; the attribution must appear in the in-app credits.",
             "revised": REVISED}))
add(["DogBarkDistant_04.wav"], "one-shot", "A different small dog barking twice far away (rural).", "2D ambient one-shot.", 0.25, "2D",
    rec("hill", "public/assets/audio/amb/dog_bark.mp3", "8cb48e0c840e96b7796c22cf5a3c09f66e3213d0518950f20a7de096fd6f7893",
        581478, "Dogs Barking in Distance_Rural.wav", "rvandemark", "MP3 decoded; fades; -3 dBFS peak; mono WAV"))
# ------------------------------------------------------------------ footsteps and glass
for k in range(1, 7):
    add([f"FootstepHard_{k:02d}.wav"], "one-shot", "Hard-soled footstep on concrete/tile (variation %d of 6)." % k,
        "Player/officer footsteps on the shop tile and pavement; random pick, pitch 0.95-1.05.", 0.5, "3D (attached to feet) or 2D",
        rec("hill", f"public/assets/audio/sfx/foot_concrete_{k}.mp3",
            ["05586f879a580a32d8b091de20388acc4662fa6d12d45cd8b5ffe7a66260573a", "8af01d45eb9638095e389cdd63a4179f618700ca84d6b6cfbf9bde3c95b843a8",
             "f3050188d67b4ca023f743ee8f7592096553f5777b9bf348033dc5acffcac8f6", "557ca43d531bc5f25bbbff15644c9d428ce3dc57cb82ad13dc18eb7cba76464e",
             "e96a66a17e0602226723d85adfabb894b3afb14c7aff39a23ca3e9c743ed0ad4", "627312fba5bbfed237119421fc20c8717947a26f064b0d9c901cb631f834342e"][k - 1],
            459964, "Footsteps on concrete", "florianreichelt", "MP3 decoded; 50 Hz high-pass; 30 ms fade; -3 dBFS peak; mono WAV"))
for k in range(1, 6):
    add([f"FootstepGlass_{k:02d}.wav"], "one-shot", "Footstep on broken glass: hard step with glass fracturing and grinding under heel and toe, shards scattering (variation %d of 5)." % k,
        "Footsteps when the player stands on the glass-debris area near the forced doors and broken bays (about 2.5 dB louder than FootstepHard at the same volume, hence 0.4).", 0.4, "3D or 2D",
        dict(type="hybrid", author="Recorded step: florianreichelt (CC0); glass layers: project synthesis",
             license="CC0-1.0 (recorded layer) + project-owned synthesis", license_url=CC0[1], generator="Generator/make_foley.py",
             repo=REPOS["hill"][0], commit=REPOS["hill"][1], repo_path=f"public/assets/audio/sfx/foot_concrete_{(k - 1) % 6 + 1}.mp3",
             original=FS(459964), title="Footsteps on concrete", license_evidence=REPOS["hill"][2],
             changes="Recorded footstep at full level + synthesized fracture crackle/grit scrape aligned to heel and toe + small shard tinkles, glass layers 8 dB below the first release with their rare large spikes soft-limited, so the heel thud (60 Hz-1.5 kHz) sits 2-6 dB above the crunch (2-20 kHz); -3 dBFS peak; mono WAV. Remixed October 15, 2026 (first release: glass about 10 dB above the step).",
             revised=REVISED))
add([f"GlassShardsTinkle_{i:02d}.wav" for i in range(1, 5)], "one-shot",
    "Small glass shards falling and bouncing on a hard floor, settling with a few slides.",
    "When a tool or foot disturbs debris; also a rare random 'settling' tinkle near broken bays.", 0.4, "3D",
    proj("make_foley.py", "Modal synthesis of 6-16 shards (2-19 kHz split modes), ballistic bounces (restitution 0.3-0.5), slides; -3 dBFS peak; mono WAV"))
add([f"GlassCrunch_{i:02d}.wav" for i in range(1, 5)], "one-shot", "Glass grit crunching under a shoe (no step body).",
    "Layer on top of FootstepHard when on glass, or when a kneeling pad/tool is set onto debris.", 0.45, "3D",
    proj("make_foley.py", "Stochastic fracture impulses with a weight envelope, grit grinding noise, ringing fragments, low thump; -3 dBFS peak; mono WAV"))
# ------------------------------------------------------------------ tools
add([f"EvidenceMarkerPlace_{i:02d}.wav" for i in range(1, 4)], "one-shot",
    "Folded plastic evidence marker (tent) set down on tile: two edges touching down.", "On marker placement (3D at the marker).", 0.5, "3D",
    proj("make_foley.py", "Damped plastic modes 0.8-4.7 kHz, two contacts 6-20 ms apart, floor knock; -3 dBFS peak; mono WAV"))
add([f"ConePlace_{i:02d}.wav" for i in range(1, 4)], "one-shot",
    "PVC traffic cone with rubber base set down: dull slap, hollow body, one or two wobbles.", "On cone placement (3D).", 0.55, "3D",
    proj("make_foley.py", "Rubber-base slap + low body modes + hollow cone modes, settling wobbles; -3 dBFS peak; mono WAV"))
add([f"CameraShutterDSLR_{i:02d}.wav" for i in range(1, 3)], "one-shot",
    "Mirror-slap DSLR exposure: mirror up, two curtains at 1/125 s, mirror return and re-cock motor.",
    "Alternative to NightAudio/CameraClick.wav for the evidence camera (2D for the player).", 0.5, "2D",
    proj("make_foley.py", "Modal mechanical clacks + low body knocks + geared motor whirr; -3 dBFS peak; mono WAV"))
add(["FlashlightClick_01.wav", "FlashlightClick_02.wav"], "one-shot",
    "Tactile tail-cap switch of a metal torch: press and release (two variations).",
    "Alternative to NightAudio/FlashlightSwitch.wav for the torch and UV torch (use _01 for torch, _02 for UV).", 0.4, "2D",
    proj("make_foley.py", "Metal modal click + thumb knock, release click 90-160 ms later; -3 dBFS peak; mono WAV"))
add(["FlashlightClickHalf_01.wav"], "one-shot", "Single switch click (press only), e.g. momentary/half press.", "Torch momentary mode.", 0.4, "2D",
    proj("make_foley.py", "As FlashlightClick without the release; -3 dBFS peak; mono WAV"))
add([f"KeysJingle_{i:02d}.wav" for i in range(1, 4)], "one-shot", "Bunch of 4-6 keys on a ring shaken/handled.",
    "Officer or manager handling keys at the safe room / office door.", 0.4, "3D",
    proj("make_foley.py", "Per-key modal metal plates, clustered collisions in 2-3 shake gestures, ring rattle; -3 dBFS peak; mono WAV"))
add(["ZipperOpen_01.wav", "ZipperOpen_02.wav", "ZipperClose_01.wav"], "one-shot", "Kit-bag zip opened (long/short) or closed.",
    "Forensic kit bag; opening the tool panel.", 0.4, "3D",
    proj("make_foley.py", "Tooth clicks at 2.5 mm pitch driven by a speed profile, slider friction, bag resonance, end stop; -3 dBFS peak; mono WAV"))
add([f"TapeUnroll_{i:02d}.wav" for i in range(1, 3)], "one-shot", "Barrier tape pulled off a hand-held roll: roll rattle, film crackle, flutter.",
    "While the player drags barrier tape (start/loop the middle while dragging).", 0.45, "3D",
    proj("make_foley.py", "Rotation-locked core knocks + speed-dependent film crackle + flutter; -3 dBFS peak; mono WAV"))
add([f"TapeStretchSnap_{i:02d}.wav" for i in range(1, 3)], "one-shot", "Polyethylene barrier tape stretched until it snaps, ends whipping back.",
    "When a tape segment is finished/cut.", 0.45, "3D",
    proj("make_foley.py", "Rising stick-slip crackle and creak, broadband snap, whip flutter; -3 dBFS peak; mono WAV"))
add(["BarrierTapeFlutter_Loop.wav"], "loop", "Barrier tape stretched between posts fluttering in a light night breeze.",
    "3D source on each outdoor tape span (small radius, low volume).", 0.25, "3D",
    proj("make_foley.py", "Aeroelastic flap train (9-23 Hz with gusts) + rustle + wind rumble, periodic; 20 s; -20 LUFS; mono WAV"))
# ------------------------------------------------------------------ doors (Kenney)
KEN = dict(title_prefix="Kenney RPG Audio 1.0")
for f, src, sha in [("DoorCreak_01.wav", "creak1.ogg", "fa75b1d17d327d7e0001d249d468493b59f3865b00a2582c21f7da2287728728"),
                    ("DoorCreak_02.wav", "creak2.ogg", "6b243b41df9a3752aeccae987cb8fadd92ce6b29fdc38f781e809c2c661ff92b"),
                    ("DoorCreak_03.wav", "creak3.ogg", "be677e6970e830fa4a8e3b20de596c3818466c4d341cd341186cff77616d9399"),
                    ("DoorOpen_01.wav", "doorOpen_1.ogg", "a3f9deb5ac1b6e1cbb8f6a5de93f1940d4d00eb4a4424ae4f2f3f7a9170dd9b0"),
                    ("DoorOpen_02.wav", "doorOpen_2.ogg", "378471f21245119d19d55b5c78bfffc72cf614da8781915d6903f13527cd5f50"),
                    ("DoorClose_01.wav", "doorClose_1.ogg", "68eec543408b4c296fed6820df9b8e986ee9708289ce60145c4934aff514e4ee"),
                    ("DoorClose_02.wav", "doorClose_4.ogg", "a0b40a11ffd7fc8c521085bcda1fafd7bf10d8e058beb72815754a3bcebb7319")]:
    desc = {"DoorCreak": "Wooden door hinge creak.", "DoorOpen_": "Wooden interior door: handle and opening.",
            "DoorClose": "Wooden interior door closing with latch."}[f[:9]]
    add([f], "one-shot", desc, "Closed side-room doors, rear office door (3D at the hinge/latch).", 0.5, "3D",
        dict(type="recording", repo=REPOS["jra"][0], commit=REPOS["jra"][1], repo_path=f"kenney-rpg-audio/audio/Audio/{src}",
             repo_file_sha256=sha, license_evidence=REPOS["jra"][2], original="https://kenney.nl/assets/rpg-audio",
             title=f"RPG Audio 1.0 - {src}", author="Kenney (www.kenney.nl)", license=CC0[0], license_url=CC0[1],
             changes="Vorbis decoded; mono fold-down; 40 Hz high-pass; edge fades; -3 dBFS peak; mono 16-bit WAV"))


def sha(p):
    h = hashlib.sha256()
    with open(p, "rb") as f:
        h.update(f.read())
    return h.hexdigest()


def probe(p):
    out = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "stream=codec_name,sample_rate,channels,duration",
                          "-of", "json", p], capture_output=True, text=True, check=True).stdout
    s = json.loads(out)["streams"][0]
    return s["codec_name"], int(s["sample_rate"]), int(s["channels"]), float(s["duration"])


def main():
    files = sorted(f for f in os.listdir(PACK) if f.endswith((".wav", ".ogg")))
    missing = [f for f in files if f not in G]
    extra = [f for f in G if f not in files]
    assert not missing and not extra, (missing, extra)
    entries = []
    for f in files:
        p = os.path.join(PACK, f)
        codec, sr, ch, dur = probe(p)
        x = au.read(p)
        i, tp = au.lufs(x)
        m = G[f]
        e = dict(file=f, kind=m["kind"], format=("WAV PCM16" if codec.startswith("pcm") else "Ogg Vorbis"), sample_rate=sr,
                 channels=ch, seconds=round(dur, 3), integrated_lufs=round(i, 1), sample_peak_dbfs=round(au.peak_db(x), 1),
                 bytes=os.path.getsize(p), sha256=sha(p), description=m["desc"], placement=m["place"],
                 suggested_volume=m["vol"], spatial=m["spatial"], source=m["origin"])
        entries.append(e)
    doc = dict(pack="NightSoundscape", prepared=DATE, revised=REVISED, prepared_by="Claude (Anthropic) for the Jewelry-store-vr-demo project",
               status="Fetched/generated October 14, 2026, revised October 15, 2026 after review; not yet wired; unverified in Unity and not auditioned",
               notes=["Freesound pages could not be opened from the preparation environment; the CC0 status of each recording is "
                      "taken from the per-asset licence metadata of the mirror repository named in 'license_evidence' (and a second "
                      "repository where noted). Re-check the 'original' pages before a public release.",
                      "Original (synthesized) files are reproducible with the scripts in Generator/ (numpy, scipy, ffmpeg).",
                      "Revision 2026-10-15: PoliceCarIdle_Loop.wav replaced (new CC0 source, broadband); RadioChatter_01..05.wav and "
                      "PoliceCarRadio_Loop.ogg rebuilt with a speech-like synthesizer; FootstepGlass_01..05.wav remixed; DogBarkDistant_01..03.wav "
                      "relicensed in these records as CC BY 3.0 with attribution (conflicting evidence). Every other file is unchanged.",
                      "Attribution required for: DogBarkDistant_01..03.wav (see 'attribution' in those entries)."],
               files=entries)
    with open(os.path.join(PACK, "sources.json"), "w") as fh:
        json.dump(doc, fh, indent=2)
    print(len(entries), "entries")


if __name__ == "__main__":
    main()
