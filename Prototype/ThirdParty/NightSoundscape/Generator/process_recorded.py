"""Prepare the CC0 field recordings for the pack: trim, seamless loops (equal-power crossfade of the audio that
follows the loop into its start), loudness (-20 LUFS for loops, -3 dBFS peak for one-shots), mono where the sound
is meant for a 3D emitter. Inputs are the decoded copies in work/src (see sources in sources.json).

Revision October 15, 2026: car_idle() now builds PoliceCarIdle_Loop.wav from 'Ford F350 5.4 V8 startup and idle'
(Therancher01, CC0) as prepared by Jxstrr0/350Chev (decoded to chev_v8x54_idle.wav); the first release used 'car idle' by
seth-m, which was almost all sub-bass."""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
import au  # noqa: E402
import synth as S  # noqa: E402

SR = 44100
SRC = sys.argv[1]
OUT = sys.argv[2]
ONLY = sys.argv[3] if len(sys.argv) > 3 else None  # "idle" re-renders only PoliceCarIdle_Loop.wav


def src(name, mono=False):
    return au.read(os.path.join(SRC, name + ".wav"), mono=mono)


def bed(name, out, start, length, xf, mono=False, hp=30, q=6):
    x = src(name, mono=mono)
    if mono:
        x = x[:, 0]
    y = au.loop_crossfade(x, length, xf, start)
    y = S.circ(lambda z: S.hp(z, hp, order=2), y)
    y = au.gain_to_lufs(y, -20.0, ceiling_db=-1.0)
    au.write_ogg(os.path.join(OUT, out), y, q=q)
    return y


def oneshot(name, out, a, b, fin=0.01, fout=0.08, peak=-3.0, hp=40, mono=True):
    x = src(name, mono=mono)[:, 0] if mono else src(name)
    y = x[int(a * SR):int(b * SR)]
    y = S.hp(y, hp, order=2)
    y = au.fade(y, fin, fout)
    y = au.normalize_peak(y, peak)
    au.write_wav(os.path.join(OUT, out), y)
    return y


def catmull(x, pos):
    """Cubic (Catmull-Rom) read of a periodic signal x at fractional positions pos."""
    n = len(x)
    i = np.floor(pos).astype(np.int64)
    f = pos - i
    xm1, x0, x1, x2 = x[(i - 1) % n], x[i % n], x[(i + 1) % n], x[(i + 2) % n]
    return x0 + 0.5 * f * (x1 - xm1 + f * (2 * xm1 - 5 * x0 + 4 * x1 - x2 + f * (3 * (x0 - x1) + x2 - xm1)))


def car_idle(out, tiles=4, seed=11):
    """Police-car idle loop from 'Ford F350 5.4 V8 startup and idle' (Therancher01, CC0) as prepared by
    Jxstrr0/350Chev (v8x54_idle.mp3: 999 rpm section, time-warped to a steady speed and cut to whole engine
    cycles, so it already wraps seamlessly). Here: 45 Hz 4th-order high-pass (circular), the loop tiled `tiles`
    times and read through a periodic time warp (+-0.6 % idle-speed hunting, two slow components) with a
    +-0.7 dB periodic level drift, so the result does not repeat audibly every 3.6 s and still wraps seamlessly;
    then -20 LUFS with a peak ceiling of -1 dBFS (the 4x-oversampled true peak is checked in the QA print)."""
    r = S.rng(seed)
    x = src("chev_v8x54_idle", mono=True)[:, 0]
    x = x - np.mean(x)
    x = S.circ(lambda z: S.hp(z, 45, order=4), x)
    P = len(x)
    T = P * tiles
    t = np.arange(T, dtype=np.float64)
    ph1, ph2, ph3 = r.uniform(0, 2 * np.pi, 3)
    # rate deviation d(t) = sum a_k cos(2 pi k t / T + ph): its integral over the loop is zero -> seamless
    a1, a2, a3 = 0.006, 0.003, 0.0015
    w = 2 * np.pi / T
    pos = t + (a1 / w) * (np.sin(w * t + ph1) - np.sin(ph1)) + (a2 / (2 * w)) * (np.sin(2 * w * t + ph2) - np.sin(ph2)) \
        + (a3 / (5 * w)) * (np.sin(5 * w * t + ph3) - np.sin(ph3))
    y = catmull(x, pos)
    lvl = 10 ** ((0.5 * np.cos(w * t + ph2) + 0.25 * np.cos(3 * w * t + ph1)) / 20)
    y = y * lvl
    y = au.gain_to_lufs(y, -20.0, ceiling_db=-1.0)
    au.write_wav(os.path.join(OUT, out), y)
    return y


def main():
    os.makedirs(OUT, exist_ok=True)
    res = {}
    if ONLY == "idle":
        y = car_idle("PoliceCarIdle_Loop.wav")
        print(f"PoliceCarIdle_Loop.wav {len(y) / SR:6.2f}s LUFS {au.lufs(y)[0]:6.1f} seam {au.seam_jump(y):.2f}")
        return
    # ---------------- beds (stereo OGG, seamless)
    res["NightCity_Loop.ogg"] = bed("atmo_cityNight", "NightCity_Loop.ogg", 22.0, 60.0, 3.0)
    res["NightCityDistant_Loop.ogg"] = bed("atmo_cityHum", "NightCityDistant_Loop.ogg", 12.0, 60.0, 3.0)
    res["StreetWind_Loop.ogg"] = bed("atmo_wind", "StreetWind_Loop.ogg", 2.0, 48.0, 3.0)
    res["WindWhistleGap_Loop.ogg"] = bed("atmo_howl", "WindWhistleGap_Loop.ogg", 2.0, 45.0, 3.0)
    # rain bed is already a 30.0 s loop between 0.25 and 30.25 s (hillbomb-sf); re-close with 1 s overlap
    res["LightRainStreet_Loop.ogg"] = bed("hill_amb_rain_light", "LightRainStreet_Loop.ogg", 0.25, 29.0, 1.0, hp=60)
    res["HVACRoomTone_Loop.ogg"] = bed("lul_subAC", "HVACRoomTone_Loop.ogg", 2.5, 45.0, 3.0, mono=True, hp=35, q=5)
    res["PoliceCarIdle_Loop.wav"] = car_idle("PoliceCarIdle_Loop.wav")
    # ---------------- one-shots
    oneshot("hill_amb_distant_siren", "SirenDistantCity_03.wav", 0.0, 18.0, fin=0.5, fout=1.0)
    oneshot("lul_siren", "SirenDistantPass_04.wav", 0.0, 15.0, fin=0.3, fout=0.5)
    oneshot("lul_dog", "DogBarkDistant_01.wav", 0.50, 1.45, fin=0.02, fout=0.15)
    oneshot("lul_dog", "DogBarkDistant_02.wav", 1.98, 3.50, fin=0.02, fout=0.2)
    oneshot("lul_dog", "DogBarkDistant_03.wav", 0.45, 9.20, fin=0.02, fout=0.3)
    oneshot("hill_amb_dog_bark", "DogBarkDistant_04.wav", 0.0, 1.10, fin=0.01, fout=0.15)
    oneshot("lul_car_door", "CarDoorOpen_01.wav", 2.80, 4.90, fin=0.02, fout=0.2)
    oneshot("lul_car_door", "CarDoorSlam_01.wav", 6.62, 8.10, fin=0.005, fout=0.3)
    oneshot("hill_sfx_passby_wet_1", "CarPassWet_01.wav", 0.0, 4.80, fin=0.2, fout=0.4)
    oneshot("lul_car_pass", "CarPass_02.wav", 0.0, 6.45, fin=0.2, fout=0.5)
    oneshot("hill_sfx_passby_2", "CarPass_03.wav", 0.0, 5.0, fin=0.2, fout=0.4)
    for k in range(1, 7):
        oneshot(f"hill_sfx_foot_concrete_{k}", f"FootstepHard_{k:02d}.wav", 0.0, 0.32, fin=0.001, fout=0.03, hp=50)
    for k in range(1, 4):
        x = src(f"kr_creak{k}", mono=True)[:, 0]
        oneshot(f"kr_creak{k}", f"DoorCreak_{k:02d}.wav", 0.0, len(x) / SR, fin=0.005, fout=0.05)
    for k, nm in enumerate(["kr_doorOpen_1", "kr_doorOpen_2"], 1):
        x = src(nm, mono=True)[:, 0]
        oneshot(nm, f"DoorOpen_{k:02d}.wav", 0.0, len(x) / SR, fin=0.002, fout=0.05)
    for k, nm in enumerate(["kr_doorClose_1", "kr_doorClose_4"], 1):
        x = src(nm, mono=True)[:, 0]
        oneshot(nm, f"DoorClose_{k:02d}.wav", 0.0, len(x) / SR, fin=0.002, fout=0.05)
    for k, v in res.items():
        print(f"{k:28s} {len(v) / SR:6.2f}s LUFS {au.lufs(v)[0]:6.1f} seam {au.seam_jump(v):.2f}")


if __name__ == "__main__":
    main()
