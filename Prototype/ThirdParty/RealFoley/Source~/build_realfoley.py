"""Build Prototype/ThirdParty/RealFoley from downloaded CC0 / CC BY recordings.

Usage: python3 -I build_realfoley.py <download_root> <out_dir>
Needs numpy, scipy and ffmpeg (libvorbis). Writes the clips and build_log.json
(per-file processing record used to write sources.json).
"""
import json, os, subprocess, sys, hashlib
import numpy as np
from scipy.signal import lfilter, butter, sosfilt

SR = 48000
DL = sys.argv[1]
OUT = sys.argv[2]
os.makedirs(OUT, exist_ok=True)
LOG = []


def load(path, mono=True):
    ch = 1 if mono else 2
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", os.path.join(DL, path), "-f", "f32le",
                          "-ac", str(ch), "-ar", str(SR), "-"], capture_output=True, check=True).stdout
    x = np.frombuffer(raw, dtype=np.float32).astype(np.float64)
    return x if mono else x.reshape(-1, 2)


def kweight(x):
    # ITU-R BS.1770 K-weighting at 48 kHz
    b1 = [1.53512485958697, -2.69169618940638, 1.19839281085285]
    a1 = [1.0, -1.69065929318241, 0.73248077421585]
    b2 = [1.0, -2.0, 1.0]
    a2 = [1.0, -1.99004745483398, 0.99007225036621]
    return lfilter(b2, a2, lfilter(b1, a1, x, axis=0), axis=0)


def lufs(x, gated=True):
    y = kweight(x)
    if y.ndim == 1:
        y = y[:, None]
    if not gated or len(y) < int(0.4 * SR):
        ms = np.mean(np.sum(y ** 2, axis=1))
        return -0.691 + 10 * np.log10(ms + 1e-20)
    blk, hop = int(0.4 * SR), int(0.1 * SR)
    pw = np.array([np.mean(np.sum(y[i:i + blk] ** 2, axis=1)) for i in range(0, len(y) - blk + 1, hop)])
    l = -0.691 + 10 * np.log10(pw + 1e-20)
    pw = pw[l > -70]
    rel = -0.691 + 10 * np.log10(np.mean(pw) + 1e-20) - 10
    pw2 = pw[(-0.691 + 10 * np.log10(pw + 1e-20)) > rel]
    return -0.691 + 10 * np.log10(np.mean(pw2) + 1e-20)


def active_lufs(x):
    """Ungated K-weighted loudness of the part of a one-shot within 30 dB of its envelope peak."""
    m = x if x.ndim == 1 else x.mean(axis=1)
    env = np.sqrt(np.convolve(m ** 2, np.ones(480) / 480, mode="same"))
    keep = env > env.max() * 10 ** (-30 / 20)
    return lufs(x[keep], gated=False)


def hpf(x, fc=30):
    sos = butter(2, fc, "highpass", fs=SR, output="sos")
    return sosfilt(sos, x, axis=0)


def fade(x, fin=0.003, fout=0.02):
    x = x.copy()
    ni, no = int(fin * SR), int(fout * SR)
    w = np.linspace(0, 1, ni) if ni else None
    if ni:
        x[:ni] = (x[:ni].T * w).T
    if no:
        x[-no:] = (x[-no:].T * np.linspace(1, 0, no) ** 2).T
    return x


def trim(x, floor_db=-50, pre=0.004, post_floor=-55, tail=0.03):
    m = np.abs(x if x.ndim == 1 else x.max(axis=1))
    pk = m.max()
    idx = np.nonzero(m > pk * 10 ** (floor_db / 20))[0]
    s = max(0, idx[0] - int(pre * SR))
    idx2 = np.nonzero(m > pk * 10 ** (post_floor / 20))[0]
    e = min(len(x), idx2[-1] + int(tail * SR))
    return x[s:e]


def onsets(x, thresh_db=-24, gap=0.18):
    env = np.sqrt(np.convolve(x ** 2, np.ones(240) / 240, mode="same"))
    th = env.max() * 10 ** (thresh_db / 20)
    above = env > th
    starts, last = [], -1e9
    for i in np.nonzero(above[1:] & ~above[:-1])[0]:
        if i - last > gap * SR:
            starts.append(i)
            last = i
    return starts


def split(x, maxlen=0.7, thresh_db=-24, gap=0.18):
    st = onsets(x, thresh_db, gap)
    segs = []
    for k, s in enumerate(st):
        e = st[k + 1] - int(0.01 * SR) if k + 1 < len(st) else len(x)
        a = max(0, s - int(0.015 * SR))
        e = min(e, a + int(maxlen * SR))
        segs.append((a, e))
    return segs


def norm_oneshot(x, target=-18.0, ceiling_db=-1.0):
    g = target - active_lufs(x)
    y = x * 10 ** (g / 20)
    pk = np.max(np.abs(y))
    lim = 10 ** (ceiling_db / 20)
    capped = False
    if pk > lim:
        y *= lim / pk
        capped = True
    return y, capped


def loopify(x, length, xf=1.0, start=0.0):
    s, L, X = int(start * SR), int(length * SR), int(xf * SR)
    seg = x[s:s + L + X]
    if len(seg) < L + X:
        raise ValueError("source too short for loop")
    body = seg[:L].copy()
    t = np.linspace(0, np.pi / 2, X)
    fi, fo = np.sin(t), np.cos(t)
    if body.ndim == 2:
        fi, fo = fi[:, None], fo[:, None]
    body[:X] = body[:X] * fi + seg[L:L + X] * fo
    return body


def extend_loop(x, length, xf=0.5):
    """Make a longer loop from a short seamless-ish source by overlapping offset copies."""
    out = np.zeros((int(length * SR),) + x.shape[1:])
    X = int(xf * SR)
    pos, rng = 0, np.random.default_rng(7)
    while pos < len(out):
        off = int(rng.integers(0, max(1, len(x) - X - 1)))
        seg = x[off:]
        n = min(len(seg), len(out) - pos + X)
        seg = seg[:n].copy()
        w = np.ones(n)
        w[:min(X, n)] = np.sin(np.linspace(0, np.pi / 2, min(X, n))) if pos else 1
        if n > X:
            w[-X:] = np.cos(np.linspace(0, np.pi / 2, X))
        if seg.ndim == 2:
            w = w[:, None]
        end = min(len(out), pos + n)
        out[pos:end] += (seg * w)[:end - pos]
        pos += n - X
    return out


def write(name, y, kind, sources, processing, loop=False, extra=None):
    path = os.path.join(OUT, name)
    ch = 1 if y.ndim == 1 else 2
    data = np.clip(y, -1, 1).astype(np.float32).tobytes()
    if name.endswith(".wav"):
        args = ["-c:a", "pcm_s16le"]
    else:
        args = ["-c:a", "libvorbis", "-q:a", "6"]
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "f32le", "-ar", str(SR), "-ac", str(ch), "-i", "-"]
                   + args + [path], input=data, check=True)
    info = {
        "file": name, "kind": kind, "seconds": round(len(y) / SR, 3), "sample_rate": SR, "channels": ch,
        "format": "WAV PCM16" if name.endswith(".wav") else "Ogg Vorbis q6",
        "integrated_lufs": round(lufs(y), 1) if loop else None,
        "active_lufs": None if loop else round(active_lufs(y), 1),
        "sample_peak_dbfs": round(20 * np.log10(np.max(np.abs(y)) + 1e-12), 1),
        "bytes": os.path.getsize(path),
        "sha256": hashlib.sha256(open(path, "rb").read()).hexdigest(),
        "sources": sources, "processing": processing,
    }
    if loop:
        a = y[:int(0.02 * SR)]
        b = y[-int(0.02 * SR):]
        info["loop_wrap_step"] = round(float(np.max(np.abs(y[0] - y[-1]))), 4)
    if extra:
        info.update(extra)
    LOG.append(info)
    print(name, info["seconds"], info.get("integrated_lufs") or info.get("active_lufs"), info["sample_peak_dbfs"])


def oneshot(name, src, segment=None, proc_note="", hp=40, target=-18.0, pre_gain=None, lp=None, x=None):
    if x is None:
        x = load(src)
    if segment:
        x = x[int(segment[0] * SR):int(segment[1] * SR)]
    x = hpf(x, hp)
    if lp:
        x = sosfilt(butter(2, lp, "lowpass", fs=SR, output="sos"), x)
    x = trim(x)
    x = fade(x)
    y, capped = norm_oneshot(x, target)
    p = "mono 48 kHz; %d Hz high-pass; silence trimmed (-50 dB head, -55 dB tail); 3 ms/20 ms fades; active-region loudness to %.0f LUFS%s" % (
        hp, target, ", peak-capped at -1 dBFS" if capped else "")
    if lp:
        p += "; %d Hz low-pass" % lp
    if segment:
        p = "excerpt %.3f-%.3f s; " % segment + p
    if proc_note:
        p += "; " + proc_note
    write(name, y, "one-shot", [src], p)


def split_set(prefix, src, n_max, start_index=1, thresh_db=-24, gap=0.18, maxlen=0.7, hp=40, skip=()):
    x = hpf(load(src), hp)
    segs = split(x, maxlen, thresh_db, gap)
    k = start_index
    used = 0
    for i, (a, e) in enumerate(segs):
        if i in skip or used >= n_max:
            continue
        seg = x[a:e]
        if len(seg) < int(0.06 * SR):
            continue
        seg = fade(trim(seg))
        y, capped = norm_oneshot(seg)
        write("%s_%02d.wav" % (prefix, k), y, "one-shot", [src],
              "event %d cut at %.3f-%.3f s by onset detection; mono 48 kHz; %d Hz high-pass; trimmed; fades; active-region loudness to -18 LUFS%s"
              % (i + 1, a / SR, e / SR, hp, ", peak-capped at -1 dBFS" if capped else ""))
        k += 1
        used += 1
    return k


def layer(name, srcs, offsets, gains_db, note, hp=40, lp=None):
    parts = [hpf(load(s), hp) for s in srcs]
    L = max(int(o * SR) + len(p) for p, o in zip(parts, offsets))
    y = np.zeros(L)
    for p, o, g in zip(parts, offsets, gains_db):
        o = int(o * SR)
        q = trim(p, floor_db=-60)
        y[o:o + len(q)] += q / (np.max(np.abs(q)) + 1e-9) * 10 ** (g / 20)
    if lp:
        y = sosfilt(butter(2, lp, "lowpass", fs=SR, output="sos"), y)
    y = fade(trim(y))
    y, capped = norm_oneshot(y)
    write(name, y, "one-shot", srcs, "layer of recorded sources (%s), each trimmed and peak-matched, mixed at %s dB with offsets %s s; mono 48 kHz; %d Hz high-pass; active-region loudness to -18 LUFS%s"
          % (note, gains_db, offsets, hp, ", peak-capped at -1 dBFS" if capped else ""))


def loop(name, src, length, xf=1.0, start=0.0, mono=False, hp=30, target=-24.0, ext=False, lp=None, note=""):
    x = load(src, mono=mono)
    x = hpf(x, hp)
    if lp:
        x = sosfilt(butter(2, lp, "lowpass", fs=SR, output="sos"), x, axis=0)
    if ext:
        x = extend_loop(x, length + xf, xf=min(xf, len(x) / SR / 4))
        start = 0
    y = loopify(x, length, xf, start)
    y = y * 10 ** ((target - lufs(y)) / 20)
    pk = np.max(np.abs(y))
    if pk > 10 ** (-1 / 20):
        y *= 10 ** (-1 / 20) / pk
    p = "%s 48 kHz; %d Hz high-pass; %s%.2f s loop from %.2f s with a %.2f s equal-power wrap crossfade; integrated loudness to %.0f LUFS" % (
        "mono" if mono else "stereo", hp, "source extended by overlapping random-offset copies (0.5 s crossfades) then " if ext else "",
        length, start, xf, target)
    if lp:
        p += "; %d Hz low-pass" % lp
    if note:
        p += "; " + note
    write(name, y, "loop", [src], p, loop=True)


K = "kenney/kenney-impact-sounds/audio/Audio/"
R = "kenney/kenney-rpg-audio/audio/Audio/"
F = "kenney/kenney-foley-sounds/audio/Audio/"
V = "rd/Assets/Audio/SFX/sfx_100_v2/"
LP = "rd/Assets/Audio/Ambient/sfx_loops/"
ST = "st/sounds/"
HB = "hbf/public/assets/audio/"
M = "misc/"

# ---------- Footsteps: polished stone / tile (indoor hard shoe) ----------
for i in range(5):
    oneshot("Footstep_Stone_%02d.wav" % (i + 1), K + "footstep_concrete_%03d.ogg" % i)
oneshot("Footstep_Stone_06.wav", V + "sfx100v2_footstep_01.ogg")
oneshot("Footstep_Stone_07.wav", V + "sfx100v2_footstep_02.ogg")
split_set("Footstep_Stone", ST + "366111.mp3", 3, start_index=8, thresh_db=-20, gap=0.25, maxlen=0.5)

# ---------- Footsteps: concrete / pavement (outdoor) ----------
for i in range(6):
    oneshot("Footstep_Concrete_%02d.wav" % (i + 1), HB + "sfx/foot_hall_%d.mp3" % (i + 1))
for i in range(2):
    oneshot("Footstep_Concrete_%02d.wav" % (i + 7), R + "footstep0%d.ogg" % (i * 4))

# ---------- Footsteps: wet pavement ----------
for i in range(3):
    oneshot("Footstep_Wet_%02d.wav" % (i + 1), V + "sfx100v2_footstep_wet_%02d.ogg" % (i + 1))
for i, (c, w) in enumerate([(1, 1), (2, 2), (3, 3), (4, 1), (0, 2)]):
    layer("Footstep_Wet_%02d.wav" % (i + 4), [HB + "sfx/foot_hall_%d.mp3" % (c + 1), V + "sfx100v2_footstep_wet_%02d.ogg" % w],
          [0.0, 0.012], [0.0, -2.0], "concrete step + recorded wet-step splash")

# ---------- Footsteps: broken glass ----------
combo = [(0, "light", 0), (1, "light", 1), (2, "medium", 2), (3, "light", 3), (4, "medium", 4), (0, "light", 4), (2, "medium", 0), (3, "light", 2)]
for i, (c, g, n) in enumerate(combo):
    layer("Footstep_Glass_%02d.wav" % (i + 1), [K + "footstep_concrete_%03d.ogg" % c, K + "impactGlass_%s_%03d.ogg" % (g, n)],
          [0.0, 0.008], [0.0, -5.0], "Kenney hard-shoe step + Kenney recorded glass impact")

# ---------- Footsteps: carpet ----------
for i in range(5):
    oneshot("Footstep_Carpet_%02d.wav" % (i + 1), K + "footstep_carpet_%03d.ogg" % i)

# ---------- Footsteps: heels (optional NPC) ----------
for i in range(5):
    oneshot("Footstep_Heels_%02d.wav" % (i + 1), M + "ss14_heelsclack%d.ogg" % (i + 1))

# ---------- Glass debris ----------
for i in range(5):
    oneshot("GlassDebris_Light_%02d.wav" % (i + 1), K + "impactGlass_light_%03d.ogg" % i)
for i in range(5):
    oneshot("GlassDebris_Medium_%02d.wav" % (i + 1), K + "impactGlass_medium_%03d.ogg" % i)
for i in range(6):
    oneshot("GlassShard_%02d.wav" % (i + 1), V + "sfx100v2_glass_%02d.ogg" % (i + 1))
oneshot("GlassDebrisSettle_01.wav", HB + "sfx/glass_break_2.mp3", segment=(0.9, 3.4), proc_note="debris tail after the smash (smash itself cut away)")
oneshot("GlassBreak_01.wav", HB + "sfx/glass_break_1.mp3")

# ---------- Doors and bell ----------
for i in range(5):
    oneshot("DoorHandle_%02d.wav" % (i + 1), V + "sfx100v2_door_%02d.ogg" % (i + 1))
oneshot("DoorOpen_01.wav", ST + "398750.mp3")
oneshot("DoorClose_01.wav", ST + "117614.mp3")
oneshot("DoorClose_02.wav", ST + "266682.mp3")
oneshot("ShopDoorBell_01.wav", ST + "442280.mp3", hp=150)
oneshot("ShopDoorBell_02.wav", ST + "69385.mp3", hp=150)
oneshot("DoorUnlockKeys_01.wav", ST + "390328.mp3")

# ---------- Keys ----------
split_set("Keys", ST + "235614.mp3", 4, thresh_db=-18, gap=0.6, maxlen=1.6)
oneshot("Keys_05.wav", ST + "390324.mp3")
oneshot("Keys_06.wav", ST + "267711.mp3")

# ---------- Clothing ----------
for i in range(4):
    oneshot("ClothRustle_%02d.wav" % (i + 1), R + "cloth%d.ogg" % (i + 1))
oneshot("ClothRustle_05.wav", ST + "19291.mp3")
oneshot("ClothRustle_06.wav", ST + "334219.mp3")
oneshot("ClothRustle_07.wav", ST + "556711.mp3")
oneshot("ClothBelt_01.wav", R + "clothBelt.ogg")
oneshot("ClothBelt_02.wav", R + "clothBelt2.ogg")

# ---------- Evidence marker / plastic set-down ----------
oneshot("PlasticSetDown_01.wav", F + "Helmet/setDown1.ogg")
oneshot("PlasticSetDown_02.wav", F + "Helmet/setDown2.ogg")
oneshot("PlasticSetDown_03.wav", V + "sfx100v2_items_01.ogg")
oneshot("PlasticSetDown_04.wav", V + "sfx100v2_items_02.ogg")
for i in range(4):
    oneshot("PickUp_%02d.wav" % (i + 1), F + "Helmet/pickup%d.ogg" % (i + 1))
oneshot("PlasticBag_01.wav", ST + "142592.mp3", segment=(0.0, 2.5))

# ---------- Camera ----------
oneshot("CameraShutter_01.wav", M + "s47_shutter.wav")
oneshot("CameraShutter_02.wav", M + "ss14_camera_snap.ogg")
oneshot("CameraFocusShutter_01.wav", ST + "207865.mp3")

# ---------- Torch / switches ----------
oneshot("SwitchClick_01.wav", V + "sfx100v2_switch_01.ogg")
oneshot("SwitchClick_02.wav", V + "sfx100v2_switch_02.ogg")
split_set("SwitchClick", ST + "423512.mp3", 2, start_index=3, thresh_db=-20, gap=0.3, maxlen=0.4)
oneshot("SwitchClick_05.wav", ST + "278205.mp3")
oneshot("MetalClick_01.wav", R + "metalClick.ogg")

# ---------- Zippers, latches ----------
oneshot("Zipper_01.wav", ST + "263039.mp3")
oneshot("Zipper_02.wav", ST + "51176.mp3")
oneshot("Zipper_03.wav", ST + "234885.mp3")
oneshot("CaseLatch_01.wav", R + "metalLatch.ogg")
oneshot("CaseLatch_02.wav", V + "sfx100v2_lock_open_01.ogg")

# ---------- Tape ----------
oneshot("TapePeel_01.wav", M + "peel.mp3")

# ---------- Radio ----------
oneshot("RadioKeyBeep_01.wav", M + "dw_key.wav", hp=150)
oneshot("RadioSquelchTail_01.wav", M + "dw_release.wav", hp=150)
oneshot("RadioKnob_01.wav", M + "dw_knob1.wav", hp=150)
oneshot("RadioKnob_02.wav", M + "dw_knob2.wav", hp=150)
loop("RadioStatic_Near_Loop.ogg", M + "dw_static_near.wav", 3.2, xf=0.3, mono=True, hp=150, note="source is 16 kHz, so nothing above 8 kHz")
loop("RadioStatic_Far_Loop.ogg", M + "dw_static_far.wav", 4.8, xf=0.4, mono=True, hp=150, note="source is 16 kHz, so nothing above 8 kHz")

# ---------- Drips ----------
for i in range(4):
    oneshot("WaterDrip_%02d.wav" % (i + 1), F + "Water/drip%d.ogg" % (i + 1))

# ---------- Ambience beds and machine loops ----------
loop("RainCityStreet_Loop.ogg", HB + "amb/rain_heavy.mp3", 28.0, xf=2.0)
loop("RainSteadyCity_Loop.ogg", ST + "217236.mp3", 29.0, xf=2.5, mono=True)
loop("NightCityAtmos_Loop.ogg", ST + "237329.mp3", 29.0, xf=2.5, mono=True)
loop("NightCityAirTone_Loop.ogg", ST + "427841.mp3", 29.0, xf=2.5, mono=True)
loop("DistantHighway_Loop.ogg", V + "sfx100v2_loop_highway.ogg", 14.5, xf=1.5)
loop("WindSoft_Loop.ogg", ST + "525292.mp3", 29.0, xf=2.5, mono=True)
loop("WindWinter_Loop.ogg", ST + "117513.mp3", 29.0, xf=2.5, mono=True)
loop("OfficeRoomTone_Loop.ogg", ST + "203306.mp3", 29.0, xf=2.5, mono=True)
loop("HVACVentAir_Loop.ogg", M + "ep_AMB_FacilityAir_LP.wav", 22.0, xf=1.5)
loop("FluorescentBuzz_Loop.ogg", M + "ep_AMB_Fluorescent_LP.wav", 11.5, xf=1.0, mono=True, hp=60)
loop("CompressorHum_Loop.ogg", M + "ep_AMB_DistantMachinery_LP.wav", 12.5, xf=1.0, mono=True)
loop("ServerFans_Loop.ogg", M + "ss14_server_fans.ogg", 30.0, xf=3.0, mono=True)
loop("MachineHum_A_Loop.ogg", V + "sfx100v2_loop_machine_01.ogg", 6.5, xf=0.8, mono=True)
loop("MachineHum_B_Loop.ogg", V + "sfx100v2_loop_machine_02.ogg", 9.5, xf=0.8, mono=True)

json.dump(LOG, open(os.path.join(OUT, "..", "build_log.json"), "w"), indent=1)
print("files", len(LOG), "bytes", sum(i["bytes"] for i in LOG))
