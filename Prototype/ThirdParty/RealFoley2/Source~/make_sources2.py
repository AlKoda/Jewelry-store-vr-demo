"""Compose sources.json for RealFoley2 from build_log.json.
Usage: python3 make_sources2.py build_log.json esc_rows.json out.json"""
import json, sys

log = json.load(open(sys.argv[1]))
esc = {r[1][:-4]: r for r in json.load(open(sys.argv[2]))}
CC0 = ("CC0 1.0", "https://creativecommons.org/publicdomain/zero/1.0/")
BY4 = ("CC BY 4.0", "https://creativecommons.org/licenses/by/4.0/")

R = {
    "esc": ("https://github.com/karolpiczak/ESC-50", "33c8ce9eb2cf0b1c2f8bcf322eb349b6be34dbb6"),
    "fed": ("https://github.com/FEDCHER67/Game", "0957711d57455375f8dbced90eae02e81a185dde"),
    "ss14": ("https://github.com/space-wizards/space-station-14", "d469c989fbd1ec2ab86410d2c9cac7cc9579913e"),
    "ve": ("https://github.com/victorrentea/victor-effects", "753149cae0da148e7c59954b86898226197c2d48"),
    "hp": ("https://github.com/huynguyen03dev/godot-ai-playground", "61d39903dbf92a62b5e98c9f8af52dda98dd7474"),
    "dv": ("https://github.com/DeltaV-Station/Delta-v", "a042876d0050d13a01b874fbb683d38e4d15e9d7"),
    "cp": ("https://github.com/crystallpunk-14/crystall-punk-14", "5b6108377e40235c768be3ac6ffadb37a085f441"),
    "jra": ("https://github.com/series-ai/jam-ready-assets", "2d7c41049c1db4d670b1ded88a7aeec0d7c2f38c"),
}
EV = {
    "esc": "ESC-50 LICENSE file (copy in Licenses/ESC-50_LICENSE_excerpt.txt) lists every clip with its Freesound original and that original's licence; only clips whose original is marked [CC0] are used. The ESC-50 compilation as a whole is CC BY-NC 3.0; the individual 5 s clips are plain excerpts of the CC0 originals. 44.1 kHz PCM WAV.",
    "fed": "Assets/OnlyVolunteers/Audio/Library_v1/LICENSES.md of the mirror (copy in Licenses/): NOX Sound Essentials Series is 'All these sounds are under CC0 license' (Essentials_Series_README.pdf), BigSoundBank (Joseph Sardin) CC0. Files are the mirror's own 48 kHz mono WAV cuts (Git LFS, fetched from media.githubusercontent.com at the pinned commit).",
    "ss14": "attributions.yml next to the file in the mirror (per-file licence, copyright and source; copy in Licenses/)",
    "ve": "starter-pack/credits.json and CREDITS.md of the mirror ('Every sound ... may be redistributed'; per-file Freesound page and CC0-1.0; copy in Licenses/). 44.1 kHz 160 kbps stereo MP3 cuts.",
    "hp": "assets/horror/audio/sfx/ATTRIBUTION_freesound.tsv of the mirror (per-file Freesound id and CC0; copy in Licenses/). 24 kHz MP3, low quality.",
    "dv": "attributions.yml next to the file in the mirror (copy in Licenses/)",
    "cp": "attributions.yml next to the file in the mirror (copy in Licenses/)",
    "jra": "License.txt shipped in the mirrored pack folder (Kenney, 'License: (Creative Commons Zero, CC0)'); Git LFS objects fetched from media.githubusercontent.com at the pinned commit",
}
FS = "https://freesound.org/s/%s/"
# path -> (repo, mirror_path, title, author, original, licence)
NOX = "https://nox-sound-design.itch.io/essentials-series-sfx-nox-sound"
FIX = {
    "fedraw/police_siren_01.wav": ("Library_v1/police/police_siren_01.wav", "Siren, French Gendarmerie (0886)", "Joseph Sardin (BigSoundBank)", "https://bigsoundbank.com/siren-french-gendarmerie-s0886.html", CC0),
    "fedraw/police_engine_idle_01.wav": ("Library_v1/vehicle/police_engine_idle_01.wav", "Vehicle_Car_Engine_Idle_Exterior_Loop_Mono_02.wav (1.06-7.36 s)", "NOX Sound", NOX, CC0),
    "fedraw/police_engine_start_01.wav": ("Library_v1/vehicle/police_engine_start_01.wav", "Vehicle_Car_Start_Engine_Exterior_Mono.wav (0.00-0.87 s)", "NOX Sound", NOX, CC0),
    "ss14raw/Effects_Vehicle_policesiren.ogg": ("Resources/Audio/Effects/Vehicle/policesiren.ogg", "policesiren", "Piotr Zaczek (blukotek)", "https://freesound.org/people/blukotek/sounds/431651/", CC0),
    "ss14raw/Effects_Vehicle_vehicleengineidle.ogg": ("Resources/Audio/Effects/Vehicle/vehicleengineidle.ogg", "vehicleengineidle.ogg (cut from Freesound 338954)", "InspectorJ (www.jshaw.co.uk)", "https://freesound.org/people/InspectorJ/sounds/338954/", BY4),
    "ss14raw/Effects_Vehicle_vehiclestartup.ogg": ("Resources/Audio/Effects/Vehicle/vehiclestartup.ogg", "vehiclestartup.ogg (cut from Freesound 338954)", "InspectorJ (www.jshaw.co.uk)", "https://freesound.org/people/InspectorJ/sounds/338954/", BY4),
    "ss14raw/Items_pen_click.ogg": ("Resources/Audio/Items/pen_click.ogg", "pen click", "dslrguide (converted by Themias)", "https://freesound.org/people/dslrguide/sounds/321484", CC0),
    "ss14raw/Items_toolbox_open.ogg": ("Resources/Audio/Items/toolbox_open.ogg", "toolbox open", "Nox_Sound (edited by themias)", "https://freesound.org/people/Nox_Sound/sounds/556648/", CC0),
    "ss14raw/Items_toolbox_close.ogg": ("Resources/Audio/Items/toolbox_close.ogg", "toolbox close", "Nox_Sound (edited by themias)", "https://freesound.org/people/Nox_Sound/sounds/556648/", CC0),
    "ve/02_siren.mp3": ("starter-pack/02_siren.mp3", "Police Siren", "TitanKaempfer", "https://freesound.org/people/TitanKaempfer/sounds/746302/", CC0),
    "ve/12_cricket.mp3": ("starter-pack/12_cricket.mp3", "cherping summer night crickets.wav", "Dashman71", "https://freesound.org/people/Dashman71/sounds/477863/", CC0),
    "ve/65_school_bell.mp3": ("starter-pack/65_school_bell.mp3", "School bell.wav", "deleted_user_7020630", "https://freesound.org/people/deleted_user_7020630/sounds/378394/", CC0),
    "ve/86_doorbell.mp3": ("starter-pack/86_doorbell.mp3", "DoorBell Shortened", "jwheeler91", "https://freesound.org/people/jwheeler91/sounds/442280/", CC0),
    "ve/90_breaking-glass.mp3": ("starter-pack/90_breaking-glass.mp3", "Glass Break", "unfa", "https://freesound.org/people/unfa/sounds/221528/", CC0),
    "hp/flashlight_click.mp3": ("assets/horror/audio/sfx/flashlight_click.mp3", "flashlight click (Freesound 502506)", "Rudmer_Rotteveel", "https://freesound.org/people/Rudmer_Rotteveel/sounds/502506/", CC0),
    "hp/note_rustle.mp3": ("assets/horror/audio/sfx/note_rustle.mp3", "paper/note rustle (Freesound 731484)", "Spaghetto87", "https://freesound.org/people/Spaghetto87/sounds/731484/", CC0),
    "forkraw/dv_electrical_hum.ogg": ("Resources/Audio/_FarHorizons/Ambience/Objects/electrical_hum.ogg", "electrical substation_hum.wav", "Zabuhailo (clipped by Jhrushbe)", "https://freesound.org/people/Zabuhailo/sounds/167673/", BY4),
    "forkraw/dv_relay_click.ogg": ("Resources/Audio/_FarHorizons/Machines/relay_click.ogg", "relay click", "bassmosphere (remixed by Jhrushbe)", "https://freesound.org/people/bassmosphere/sounds/384698/", CC0),
    "forkraw/cp_bell.ogg": ("Resources/Audio/_CP14/Effects/bell.ogg", "bell", "Fenodyrie", "https://freesound.org/people/Fenodyrie/sounds/583950/", CC0),
    "forkraw/cp_bell_short.ogg": ("Resources/Audio/_CP14/Effects/bell_short.ogg", "bell (short)", "Fenodyrie", "https://freesound.org/people/Fenodyrie/sounds/583950/", CC0),
    "forkraw/cp_cash.ogg": ("Resources/Audio/_CP14/Effects/cash.ogg", "cash register", "Zott820", "https://freesound.org/people/Zott820/sounds/209578/", CC0),
    "forkraw/cp_doorknob_2.ogg": ("Resources/Audio/_CP14/Structures/doorknob_2.ogg", "doorknob", "BenjaminNelan", "https://freesound.org/people/BenjaminNelan/sounds/321087/", CC0),
    "ss14raw/Items_Paper_paper_scribble1.ogg": ("Resources/Audio/Items/Paper/paper_scribble1.ogg", "Pen Clicks and Writing on Paper (paper_scribble1 cut)", "PDaefaul (cut by Krunklehorn)", "https://freesound.org/people/PDaefaul/sounds/631971/", CC0),
    "ss14raw/Items_Paper_paper_scribble2.ogg": ("Resources/Audio/Items/Paper/paper_scribble2.ogg", "Pen Clicks and Writing on Paper (paper_scribble2 cut)", "PDaefaul (cut by Krunklehorn)", "https://freesound.org/people/PDaefaul/sounds/631971/", CC0),
    "ss14raw/Items_Paper_paper_scribble3.ogg": ("Resources/Audio/Items/Paper/paper_scribble3.ogg", "Pen Clicks and Writing on Paper (paper_scribble3 cut)", "PDaefaul (cut by Krunklehorn)", "https://freesound.org/people/PDaefaul/sounds/631971/", CC0),
    "ss14raw/Items_Paper_paper_scribble4.ogg": ("Resources/Audio/Items/Paper/paper_scribble4.ogg", "Pen Clicks and Writing on Paper (paper_scribble4 cut)", "PDaefaul (cut by Krunklehorn)", "https://freesound.org/people/PDaefaul/sounds/631971/", CC0),
    "ss14raw/Items_Paper_paper_scribble5.ogg": ("Resources/Audio/Items/Paper/paper_scribble5.ogg", "Pen Clicks and Writing on Paper (paper_scribble5 cut)", "PDaefaul (cut by Krunklehorn)", "https://freesound.org/people/PDaefaul/sounds/631971/", CC0),
}


def src_for(p):
    f = p.split("/")[-1]
    if p.startswith("esc/"):
        cid = f.rsplit("-", 1)[0]
        r = esc[cid]
        return dict(title=r[2], author=r[4], original=r[3].replace("http://www.", "https://"), license=CC0[0], license_url=CC0[1],
                    attribution_required=False, mirror=R["esc"][0], mirror_commit=R["esc"][1], mirror_path="audio/" + f,
                    license_evidence=EV["esc"], recorded=True)
    if p.startswith("kenney/"):
        return dict(title="RPG Audio 1.0 - " + f, author="Kenney (www.kenney.nl)", original="https://kenney.nl/assets/rpg-audio", license=CC0[0],
                    license_url=CC0[1], attribution_required=False, mirror=R["jra"][0], mirror_commit=R["jra"][1],
                    mirror_path="kenney-rpg-audio/audio/Audio/" + f, license_evidence=EV["jra"], recorded=True)
    if p.startswith("fedraw/door") or p.startswith("fedraw/step"):
        nox = {"door_front_close_01": "Vehicle_Car_Door_Closing_Exterior_Mono_01.wav", "door_front_close_02": "Vehicle_Car_Door_Closing_Exterior_Mono_02.wav",
               "door_front_close_03": "Vehicle_Car_Door_Closing_Exterior_Mono_03.wav", "door_front_open_01": "Vehicle_Truck_Door_Opening_Exterior_Mono_01.wav",
               "door_front_open_02": "Vehicle_Truck_Door_Opening_Exterior_Mono_02.wav", "door_front_open_03": "Vehicle_Truck_Door_Opening_Exterior_Mono_03.wav",
               "step_asphalt_01": "Footsteps_Rock_Walk_03.wav", "step_asphalt_02": "Footsteps_Rock_Walk_05.wav", "step_asphalt_03": "Footsteps_Rock_Walk_07.wav",
               "step_asphalt_04": "Footsteps_Rock_Walk_08.wav", "step_asphalt_05": "Footsteps_Rock_Walk_09.wav", "step_asphalt_06": "Footsteps_Rock_Walk_04.wav"}[f[:-4]]
        sub = "van" if f.startswith("door") else "footsteps"
        return dict(title="NOX Sound Essentials Series - " + nox, author="NOX Sound", original=NOX, license=CC0[0], license_url=CC0[1],
                    attribution_required=False, mirror=R["fed"][0], mirror_commit=R["fed"][1],
                    mirror_path="Assets/OnlyVolunteers/Audio/Library_v1/%s/%s" % (sub, f), license_evidence=EV["fed"], recorded=True)
    mp, t, a, o, lic = FIX[p]
    key = {"fedraw": "fed", "ss14raw": "ss14", "ve": "ve", "hp": "hp"}.get(p.split("/")[0]) or ("dv" if "/dv_" in p else "cp")
    if key == "fed":
        mp = "Assets/OnlyVolunteers/Audio/" + mp
    return dict(title=t, author=a, original=o, license=lic[0], license_url=lic[1], attribution_required=lic is BY4,
                mirror=R[key][0], mirror_commit=R[key][1], mirror_path=mp, license_evidence=EV[key], recorded=True)


ATTR = {
    "EngineIdle_InspectorJ_Loop.ogg": 'Freesound sound 338954 by InspectorJ (www.jshaw.co.uk) of Freesound.org (https://freesound.org/people/InspectorJ/sounds/338954/), licensed CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/); via Space Station 14; trimmed, looped and normalised.',
    "EngineStart_02.wav": 'Freesound sound 338954 by InspectorJ (www.jshaw.co.uk) of Freesound.org (https://freesound.org/people/InspectorJ/sounds/338954/), licensed CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/); via Space Station 14; trimmed and normalised.',
    "ElectricalHum_Loop.ogg": '"electrical substation_hum.wav" by Zabuhailo (https://freesound.org/people/Zabuhailo/sounds/167673/), licensed CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/); clipped by Jhrushbe (Delta-V); looped and normalised.',
}
files = []
for e in log:
    e = dict(e)
    srcs = [src_for(p) for p in e.pop("sources")]
    e["source"] = srcs[0] if len(srcs) == 1 else None
    if len(srcs) > 1:
        e["sources"] = srcs
    if e["file"] in ATTR:
        e["attribution"] = ATTR[e["file"]]
    e["origin"] = "recorded"
    files.append(e)
out = {
    "pack": "RealFoley2",
    "prepared": "2026-10-17",
    "prepared_by": "Claude (Anthropic) for the Jewelry-store-vr-demo project",
    "status": "Fetched and processed October 17, 2026; not wired; not auditioned on headphones, in Unity or on Quest (unverified)",
    "notes": [
        "Every clip is a real recording; nothing here is synthesized or layered.",
        "Freesound/BigSoundBank/itch.io pages could not be opened from the preparation environment; licence status comes from the licence records of the GitHub mirrors named in license_evidence (copies in Licenses/). Re-check the 'original' pages before a public release.",
        "Attribution required for: ElectricalHum_Loop.ogg, EngineIdle_InspectorJ_Loop.ogg, EngineStart_02.wav (CC BY 4.0); see 'attribution'.",
        "Processing script: Source~/build_realfoley2.py with Source~/lib.py (verbatim helpers of RealFoley/Source~/build_realfoley.py). Same targets as RealFoley: one-shots -18 LUFS active-region (ungated BS.1770), capped at -1 dBFS sample peak, so transients can sit up to ~10 dB under -18; loops -24 LUFS integrated with an equal-power wrap crossfade.",
    ],
    "files": files,
}
json.dump(out, open(sys.argv[3], "w"), indent=1, ensure_ascii=False)
print(len(files))
