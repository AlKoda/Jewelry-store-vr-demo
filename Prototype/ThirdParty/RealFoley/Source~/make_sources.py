"""Compose sources.json for RealFoley from build_log.json. Usage: python3 make_sources.py build_log.json stiph.md out.json"""
import json, re, sys

log = json.load(open(sys.argv[1]))
st_md = open(sys.argv[2], encoding="utf-8").read()
ST_INFO = {}
for line in st_md.splitlines():
    m = re.match(r"\|\s*[^|]*\|\s*([^|]*?)\s*\|\s*([^|]*?)\s*\|\s*https://freesound.org/s/(\d+)/", line)
    if m:
        ST_INFO[m.group(3)] = (m.group(1), m.group(2))

CC0 = ("CC0 1.0", "https://creativecommons.org/publicdomain/zero/1.0/")
REPOS = {
    "jra": ("https://github.com/series-ai/jam-ready-assets", "2d7c41049c1db4d670b1ded88a7aeec0d7c2f38c"),
    "bs": ("https://github.com/KasparPlaas/BiomeSurvivor", "f66f491319cdde6df9fc1da0c5dfa0570c015213"),
    "st": ("https://github.com/hey8v0/ST-iPhonie", "020eb9a251205b16c487fac1690c9dd99fa91441"),
    "hb": ("https://github.com/Aarymanv/hillbomb-sf", "937d362243a56fd7aed001e36dc5051e1959a04d"),
    "ss14": ("https://github.com/space-wizards/space-station-14", "d469c989fbd1ec2ab86410d2c9cac7cc9579913e"),
    "s47": ("https://github.com/Tombonator3000/SIGNAL-47", "726b2d965fdc08585cab9992ff82682a98ae5108"),
    "sd": ("https://github.com/lala11112/seodaemun-recycle-demo", "67bb760141ca8266e60928dab4f3861c6d3e669a"),
    "dw": ("https://github.com/Warexpor/DarkwoodMP-YokWare", "d9d2237e4071d5dd75fb809bdc90fc44cd5f89ac"),
    "ep": ("https://github.com/LautiGR116/EchoProtocol", "a93c6da6e5d183147423eb2685183a4d4a5968ab"),
}
KEN_EVID = "License.txt shipped in the mirrored pack folder (Kenney, 'License: (Creative Commons Zero, CC0)'); Git LFS objects fetched from media.githubusercontent.com at the pinned commit"
RD_EVID = ("No licence file inside sfx_100_v2.zip / sfx_loops.zip (rubberduck ships none); CC0 is stated on the OpenGameArt pages and recorded as CC0 1.0 by several independent projects "
           "(apholmes7-bot/hiddenharbours Assets/_Project/Audio/LICENSES.md, thisnameissoclever/terrilives docs/specs/2026-09-06-cc0-audio-intake.md, Lame34/jeu-medieval assets/CREDITS.md). Re-check the OGA page before release.")
ST_EVID = "sounds/README.md of the mirror: '304 sounds, all from Freesound, licence CC0 (public domain)', with per-sound author and freesound link. Mirror files are 64 kbps (one-shots) / 48 kbps 32 kHz (ambiences, cut to 32 s, mono, loudness-levelled) MP3 re-encodes. Re-check the Freesound page before release."
HB_EVID = "public/assets/ASSET_LICENSES.md of the mirror (each audio file listed with its Freesound page and 'CC0 1.0'; the project states it only accepted Freesound pages showing 'Creative Commons 0')"
SS14_EVID = "Resources/Audio/**/attributions.yml of the mirror (per-file licence, copyright and source)"


def src_for(path):
    f = path.split("/")[-1]
    if path.startswith("kenney/"):
        pack = {"impact-sounds": ("Impact Sounds 1.0", "https://kenney.nl/assets/impact-sounds"),
                "rpg-audio": ("RPG Audio 1.0", "https://kenney.nl/assets/rpg-audio"),
                "foley-sounds": ("Foley Sounds 1.0", "https://kenney.nl/assets/foley-sounds")}[path.split("/")[1][7:]]
        r = REPOS["jra"]
        return dict(title="%s - %s" % (pack[0], f), author="Kenney (www.kenney.nl)", original=pack[1], license=CC0[0], license_url=CC0[1],
                    attribution_required=False, mirror=r[0], mirror_commit=r[1], mirror_path=path.replace("kenney/", "", 1), license_evidence=KEN_EVID, recorded=True)
    if path.startswith("rd/"):
        r = REPOS["bs"]
        pk = ("100 CC0 SFX #2", "https://opengameart.org/content/100-cc0-sfx-2") if "sfx_100_v2" in path else ("30 CC0 SFX loops", "https://opengameart.org/content/30-cc0-sfx-loops")
        return dict(title="%s - %s" % (pk[0], f), author="rubberduck (OpenGameArt)", original=pk[1], license=CC0[0], license_url=CC0[1], attribution_required=False,
                    mirror=r[0], mirror_commit=r[1], mirror_path=path[3:], license_evidence=RD_EVID, recorded=True)
    if path.startswith("st/"):
        r = REPOS["st"]
        fid = f.split(".")[0]
        t, a = ST_INFO.get(fid, ("?", "?"))
        return dict(title=t, author=a, original="https://freesound.org/s/%s/" % fid, license=CC0[0], license_url=CC0[1], attribution_required=False,
                    mirror=r[0], mirror_commit=r[1], mirror_path=path[3:], license_evidence=ST_EVID, recorded=True)
    if path.startswith("hbf/"):
        r = REPOS["hb"]
        meta = {"foot_hall": ("Footsteps in Factory Hall on Wood and Concrete.wav", "RutgerMuller", "https://freesound.org/s/155858/"),
                "glass_break_1": ("Glass Break", "unfa", "https://freesound.org/s/221528/"),
                "glass_break_2": ("window small trailer rv glass smash with an axe debris fall metal grill.wav", "kyles", "https://freesound.org/s/452670/"),
                "rain_heavy": ("Rain (strong), city, street, distant traffic", "Dominik_W", "https://freesound.org/s/398593/")}
        key = next(k for k in meta if f.startswith(k))
        t, a, u = meta[key]
        return dict(title=t, author=a, original=u, license=CC0[0], license_url=CC0[1], attribution_required=False, mirror=r[0], mirror_commit=r[1],
                    mirror_path=path[4:], license_evidence=HB_EVID, recorded=True)
    m = {
        "ss14_heelsclack": ("heelsclack (clipped from a NachtmahrTV recording by Centronias)", "NachtmahrTV; clipped by Centronias", "https://freesound.org/people/NachtmahrTV/sounds/571801/", CC0, "ss14", "Resources/Audio/Effects/Footsteps/" + f[5:], SS14_EVID, None),
        "ss14_server_fans": ("server_fans", "DeVern", "https://freesound.org/people/DeVern/sounds/610761/", ("CC BY 3.0", "https://creativecommons.org/licenses/by/3.0/"), "ss14", "Resources/Audio/Ambience/Objects/server_fans.ogg", SS14_EVID,
                             '"Server fans" by DeVern (https://freesound.org/people/DeVern/sounds/610761/), licensed CC BY 3.0 (https://creativecommons.org/licenses/by/3.0/); via Space Station 14; trimmed, looped and normalised.'),
        "ss14_camera_snap": ("camera_snap (edited by ketufaispikinut)", "thecheeseman; edited by ketufaispikinut", "https://freesound.org/people/thecheeseman/sounds/51360/", ("CC BY 4.0", "https://creativecommons.org/licenses/by/4.0/"), "ss14", "Resources/Audio/Misc/camera_snap.ogg", SS14_EVID,
                             '"camera snap" by thecheeseman (https://freesound.org/people/thecheeseman/sounds/51360/), edited by ketufaispikinut (Space Station 14), licensed CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/); trimmed and normalised.'),
        "s47_shutter": ("Camera Shutter", "roachpowder", "https://freesound.org/people/roachpowder/sounds/170229/", CC0, "s47", "Unity/Assets/Signal47/Art/ThirdParty/PreparedAudio/CameraShutter.wav",
                        "Docs/THIRD_PARTY_NOTICES.md of the mirror ('Camera Shutter, roachpowder, Freesound 170229, CC0-1.0'; HQ preview, 100 Hz high-pass, limiter)", None),
        "peel": ("duct tape peels.wav", "thefirecrowd", "https://freesound.org/people/thefirecrowd/sounds/203107/", CC0, "sd", "assets/audio/sfx/peel.mp3", "assets/audio/SOURCES.md of the mirror (CC0 1.0 link)", None),
        "dw_key": ("Walkie Talkie famous beep", "SEF7", "https://freesound.org/people/SEF7/sounds/701326/", CC0, "dw", "DarkwoodMP.Mod/Resources/Radio/key.wav", "DarkwoodMP.Mod/Resources/Radio/SOURCES.md of the mirror", None),
        "dw_release": ("Radio Sign Off / Squelch", "JovianSounds", "https://freesound.org/people/JovianSounds/sounds/524205/", CC0, "dw", "DarkwoodMP.Mod/Resources/Radio/release.wav", "DarkwoodMP.Mod/Resources/Radio/SOURCES.md of the mirror", None),
        "dw_knob": ("Press radio knob.wav", "greatsoundstube", "https://freesound.org/people/greatsoundstube/sounds/629301/", CC0, "dw", "DarkwoodMP.Mod/Resources/Radio/" + f[3:], "DarkwoodMP.Mod/Resources/Radio/SOURCES.md of the mirror", None),
        "dw_static_near": ("Walkie Talkie radio static", "SEF7", "https://freesound.org/people/SEF7/sounds/701330/", CC0, "dw", "DarkwoodMP.Mod/Resources/Radio/static_near.wav", "DarkwoodMP.Mod/Resources/Radio/SOURCES.md of the mirror", None),
        "dw_static_far": ("Radio static", "LukaCafuka", "https://freesound.org/people/LukaCafuka/sounds/760335/", CC0, "dw", "DarkwoodMP.Mod/Resources/Radio/static_far.wav", "DarkwoodMP.Mod/Resources/Radio/SOURCES.md of the mirror", None),
        "ep_AMB_Fluorescent": ("Fluorescent Light Buzzing", "JoelMcDaniel", "https://freesound.org/people/JoelMcDaniel/sounds/830440/", CC0, "ep", "Assets/_Game/Audio/Clips/Ambience/AMB_Fluorescent_LP.wav", None, None),
        "ep_AMB_FacilityAir": ("House Vent Loop 03", "HECKFRICKER", "https://freesound.org/people/HECKFRICKER/sounds/753542/", CC0, "ep", "Assets/_Game/Audio/Clips/Ambience/AMB_FacilityAir_LP.wav", None, None),
        "ep_AMB_DistantMachinery": ("Machine Hum (power-washer compressor)", "AlaskaRobotics", "https://freesound.org/people/AlaskaRobotics/sounds/221566/", CC0, "ep", "Assets/_Game/Audio/Clips/Ambience/AMB_DistantMachinery_LP.wav", None, None),
    }
    key = next(k for k in m if f.startswith(k))
    t, a, u, lic, rk, mp, ev, attr = m[key]
    if rk == "ep":
        ev = ("Docs/AudioEvidence/2026-08-09/FREESOUND_CC0_SNAPSHOT.md and Docs/AUDIO_ASSET_LEDGER.md of the mirror (source page showed 'Creative Commons 0' on 2026-08-09; original hashed). "
              "The mirror repository has no project licence; the file is the mirror author's lightly edited loop (crossfade only) of the CC0 original, used here on the CC0 terms of the original.")
    r = REPOS[rk]
    d = dict(title=t, author=a, original=u, license=lic[0], license_url=lic[1], attribution_required=attr is not None, mirror=r[0], mirror_commit=r[1], mirror_path=mp,
             license_evidence=ev, recorded=True)
    if attr:
        d["attribution"] = attr
    if rk == "dw" and f.startswith(("dw_key", "dw_release", "dw_knob")):
        d["note"] = "The mirror cut the bass below 160-200 Hz and appended a ~0.3 s decaying tail derived from the recording itself (SOURCES.md)."
    return d


files = []
for e in log:
    srcs = [src_for(s) for s in e.pop("sources")]
    e["source"] = srcs[0] if len(srcs) == 1 else {"layered": srcs}
    e["origin"] = "recorded" if len(srcs) == 1 else "recorded (layer of two recordings)"
    files.append(e)

out = {
    "pack": "RealFoley",
    "prepared": "2026-10-17",
    "prepared_by": "Claude (Anthropic) for the Jewelry-store-vr-demo project",
    "status": "Fetched and processed October 17, 2026; not wired; not auditioned on headphones, in Unity or on Quest (unverified)",
    "notes": [
        "Every clip is a real recording (or a mix of two real recordings, marked 'layered'); nothing here is synthesized.",
        "Freesound pages could not be opened from the preparation environment; CC0/CC BY status comes from the licence records of the mirror repositories named in license_evidence. Re-check the 'original' pages before a public release.",
        "Attribution required for: ServerFans_Loop.ogg (CC BY 3.0) and CameraShutter_02.wav (CC BY 4.0); see 'attribution'.",
        "Processing script: Source~/build_realfoley.py (numpy, scipy, ffmpeg). Loudness: one-shots are set to -18 LUFS measured ungated over the part of the clip within 30 dB of its envelope peak (BS.1770 K-weighting), then capped at -1 dBFS sample peak, so short transients can sit 1-9 dB below -18; loops are -24 LUFS integrated (BS.1770 gated).",
    ],
    "files": files,
}
json.dump(out, open(sys.argv[3], "w"), indent=1, ensure_ascii=False)
print(len(files))
