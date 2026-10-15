"""Original synthesized interior machine loops: HVAC supply vent, fluorescent ballast buzz, fridge compressor.

All components are periodic over the loop length (integer cycles, circular noise), so every loop is seamless.
50 Hz mains (UK/EU) is assumed; for 60 Hz regions set MAINS=60.
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
import au  # noqa: E402
import synth as S  # noqa: E402

SR = 44100
OUT = sys.argv[1]
MAINS = float(os.environ.get("MAINS", "50"))


def snap(f, L):
    """Round a frequency to an integer number of cycles over L seconds."""
    return max(1, round(f * L)) / L


def tone(f, L, phase=0.0, fm_depth=0.0, fm_sig=None):
    t = np.arange(int(L * SR)) / SR
    ph = 2 * np.pi * snap(f, L) * t + phase
    if fm_sig is not None:
        # integrate a zero-mean periodic modulation (keeps periodicity)
        ph += 2 * np.pi * fm_depth * np.cumsum(fm_sig) / SR
    return np.sin(ph)


def hvac(L=24.0, seed=31):
    r = S.rng(seed)
    n = int(L * SR)

    def air_shape(f):
        f = np.maximum(f, 1)
        g = 1 / np.sqrt(f / 100 + 0.2)            # pink-ish
        g *= 1 / (1 + (f / 2500) ** 2)              # diffuser grille softens the top
        g *= 1 + 1.8 * np.exp(-((np.log(f) - np.log(450)) ** 2) / 0.6)   # broad airflow hump
        g *= 1 / (1 + (35 / f) ** 4)
        return g
    air = S.periodic_noise(n, r, air_shape)
    # turbulence: slow random level breathing (+-1.2 dB) and a faster flutter
    mod = 1 + 0.07 * S.smooth_random(n, r, 0.35) + 0.025 * S.smooth_random(n, r, 4.0)
    air *= mod
    rumble = S.periodic_noise(n, r, lambda f: np.exp(-((f - 45) / 25) ** 2)) * 0.5
    # blower: 4-pole motor ~1440 rpm (24 Hz), 38-blade forward-curved wheel -> blade pass ~ 912 Hz
    rot = 24.0
    wob = S.smooth_random(n, r, 0.2)
    bpf = tone(rot * 38, L, fm_depth=1.5, fm_sig=wob) * 0.035
    bpf += tone(rot * 76, L, fm_depth=3.0, fm_sig=wob) * 0.012
    shaft = tone(rot, L) * 0.04 + tone(rot * 2, L) * 0.02
    hum = tone(2 * MAINS, L) * 0.02 + tone(4 * MAINS, L) * 0.006
    y = air + rumble + bpf * 4 + shaft + hum * 3
    y = S.circ(lambda z: S.hp(z, 25, order=2), y)
    return y


def fluorescent(L=16.0, seed=32):
    r = S.rng(seed)
    n = int(L * SR)
    t = np.arange(n) / SR
    f0 = 2 * MAINS  # magnetostriction at twice the mains frequency
    # magnetic ballast: laminations buzz -> asymmetric, slightly clipped waveform rich in harmonics
    base = np.sin(2 * np.pi * f0 * t) + 0.35 * np.sin(2 * np.pi * 2 * f0 * t + 0.6) + 0.12 * np.sin(2 * np.pi * 3 * f0 * t + 1.1)
    buzz = np.tanh(2.2 * base) + 0.15 * np.tanh(6 * np.sin(2 * np.pi * f0 * t + 0.3)) ** 3
    # loose lamination rattle: sharp tick each half-cycle with tiny random amplitude
    tick_amp = 0.10 * (1 + 0.3 * S.smooth_random(n, r, 3.0))
    rattle_src = (np.sin(2 * np.pi * f0 * t) > 0.995).astype(float)
    rattle = S.circ(lambda z: S.bp(z, 1800, 7000, order=2), rattle_src) * tick_amp * 8
    # amplitude flicker of the hum (supply variations)
    amp = 1 + 0.04 * S.smooth_random(n, r, 1.5)
    y = buzz * amp * 0.35 + rattle
    # fitting/diffuser resonances and the ceiling cavity
    y = S.circ(lambda z: S.peak_eq(S.peak_eq(z, 600, 4, q=2.5), 1700, 3, q=3), y)
    y = S.circ(lambda z: S.lp(S.hp(z, 80, order=2), 9000, order=2), y)
    # faint broadband hiss of the tube discharge
    y += 0.01 * S.periodic_noise(n, r, lambda f: 1 / (1 + (f / 6000) ** 2))
    return y


def fridge(L=20.0, seed=33):
    r = S.rng(seed)
    n = int(L * SR)
    t = np.arange(n) / SR
    # reciprocating compressor ~2900 rpm -> 48.3 Hz piston strokes; motor hum at mains and 2x
    stroke_f = snap(48.3, L)
    hum = 0.5 * np.sin(2 * np.pi * snap(MAINS, L) * t) + 0.35 * np.sin(2 * np.pi * snap(2 * MAINS, L) * t + 0.4)
    hum += 0.12 * np.sin(2 * np.pi * snap(3 * MAINS, L) * t + 1.0) + 0.08 * np.sin(2 * np.pi * snap(4 * MAINS, L) * t)
    # piston knocks: impulse per revolution through the shell resonances
    k = np.zeros(n)
    period = int(round(SR / stroke_f))
    jitter = r.normal(0, 0.03, n // period + 2)
    for j, i in enumerate(range(0, n, period)):
        k[i] = 1.0 + jitter[j]
    shell = S.circ(lambda z: S.peak_eq(S.peak_eq(S.bp(z, 120, 2500, order=2), 380, 8, q=6), 820, 6, q=8), k)
    shell *= 0.9
    # refrigerant flow: soft hiss with slow gurgle bubbles
    hiss = S.periodic_noise(n, r, lambda f: np.exp(-((np.log(np.maximum(f, 1)) - np.log(1600)) ** 2) / 0.5)) * 0.05
    gur = np.zeros(n)
    for _ in range(26):
        c = r.uniform(0, L)
        d = r.uniform(0.05, 0.4)
        g = np.exp(-0.5 * ((t - c) / d) ** 2) + np.exp(-0.5 * ((t - c + L) / d) ** 2) + np.exp(-0.5 * ((t - c - L) / d) ** 2)
        gur += g * r.uniform(0.3, 1.0)
    bub = S.periodic_noise(n, r, lambda f: np.exp(-((f - 450) / 220) ** 2)) * gur * 0.08
    # condenser fan (small, low)
    fan = S.periodic_noise(n, r, lambda f: 1 / (1 + (f / 900) ** 2) / np.sqrt(np.maximum(f, 20) / 50)) * 0.05
    y = hum * 0.6 + shell + hiss + bub + fan
    # cabinet housing: mid lowpass
    y = S.circ(lambda z: S.lp(S.hp(z, 30, order=2), 5000, order=2), y)
    y *= 1 + 0.03 * S.smooth_random(n, r, 0.4)
    return y


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, fn, target in [("HVACVent_Loop.wav", hvac, -20.0),
                             ("FluorescentBuzz_Loop.wav", fluorescent, -20.0),
                             ("FridgeCompressor_Loop.wav", fridge, -20.0)]:
        x = fn()
        x = au.gain_to_lufs(x, target, ceiling_db=-1.0)
        au.write_wav(os.path.join(OUT, name), x)
        print(name, au.lufs(x), "seam", round(au.seam_jump(x), 2), len(x) / SR)


if __name__ == "__main__":
    main()
