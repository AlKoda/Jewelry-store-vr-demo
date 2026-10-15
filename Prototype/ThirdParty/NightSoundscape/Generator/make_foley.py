"""Original synthesized foley one-shots for the night crime-scene demo (physically motivated modal/noise
synthesis; no recorded material except where noted: the glass-footstep layers use the CC0 'Footsteps on
concrete' takes by florianreichelt that also ship as FootstepHard_0x).

Revision October 15, 2026: FootstepGlass_0x remixed (glass layers -8 dB with spike limiting); every other file
written by this script is bit-identical to the first release."""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
import au  # noqa: E402
import synth as S  # noqa: E402

SR = 44100
SRC = sys.argv[1]
OUT = sys.argv[2]


def finish(x, peak=-3.0, fin=0.001, fout=0.02, hp_hz=40):
    x = S.hp(x, hp_hz, order=2)
    x = au.fade(x, fin, fout)
    # trim trailing near-silence
    a = np.abs(x)
    thr = np.max(a) * 10 ** (-70 / 20)
    idx = np.where(a > thr)[0]
    if len(idx):
        x = x[:min(len(x), idx[-1] + int(0.03 * SR))]
        x = au.fade(x, 0.0, min(fout, len(x) / SR / 4))
    return au.normalize_peak(x, peak)


def modal_hit(freqs, t60s, amps, dur, r, exc_ms=0.3, bright=1.0):
    """One impact: short noise/impulse excitation through a modal bank."""
    n = int(dur * SR)
    ir = S.modal_ir(freqs, t60s, amps, dur, phases=r.uniform(0, 2 * np.pi, len(freqs)))
    m = max(2, int(exc_ms * SR / 1000))
    exc = r.standard_normal(m) * np.hanning(m)
    if bright < 1.0:
        exc = S.lp(exc, 2000 + 14000 * bright, order=2)
    from scipy.signal import fftconvolve
    y = fftconvolve(exc, ir)[:n]
    return y


# ----------------------------------------------------------------- glass
def shard_modes(r, size):
    """size 0..1 (0 = crumb, 1 = palm-sized shard)."""
    base = 6200 * (1 - 0.68 * size) * r.uniform(0.8, 1.2)
    ratios = np.array([1.0, 1.52, 2.11, 2.73, 3.38, 4.2])[:r.integers(3, 7)]
    fr = base * ratios * r.uniform(0.97, 1.03, len(ratios))
    fr = fr[fr < 19000]
    t60 = np.clip(0.25 * (3000 / fr) ** 0.7 * (0.5 + size), 0.02, 0.5)
    amps = 10 ** (-r.uniform(0, 14, len(fr)) / 20)
    return fr, t60, amps


def glass_tinkle(r, n_shards=10, spread=0.6, drop_h=0.6):
    """Small pieces of glass falling on hard tile and settling."""
    dur = spread + 1.2
    y = np.zeros(int(dur * SR))
    for _ in range(n_shards):
        size = r.beta(1.4, 3.0)
        fr, t60, amps = shard_modes(r, size)
        t0 = r.uniform(0, spread) ** 1.4
        v = np.sqrt(2 * 9.81 * drop_h) * r.uniform(0.6, 1.1)
        e = r.uniform(0.3, 0.5)           # coefficient of restitution
        amp = (0.25 + size) * r.uniform(0.5, 1.0)
        t = t0
        for b in range(r.integers(2, 6)):
            hit = modal_hit(fr, t60, amps, 0.5, r, exc_ms=0.15 + 0.3 * size)
            click = S.click(0.6, r, lo=2500, hi=16000) * 0.5
            S.place(y, hit * amp, t)
            S.place(y, click * amp, t)
            dt = 2 * v / 9.81
            v *= e
            amp *= e * r.uniform(0.8, 1.3)
            t += dt
            if dt < 0.012:
                break
        # final slide/scrape of the bigger ones
        if size > 0.45 and r.random() < 0.6:
            m = int(r.uniform(0.04, 0.12) * SR)
            sc = S.bp(r.standard_normal(m), 3000, 12000, order=2) * np.hanning(m) * 0.05
            S.place(y, sc, t)
    return y


def crunch_core(r, dur=0.22, density=900, grit=0.35):
    """Fractures of glass under a sole: dense impulsive crackle with a grinding scrape."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    # density envelope: quick rise as weight goes on, tail as the foot rolls
    dens = density * (np.minimum(1, t / (0.25 * dur)) ** 1.5) * np.exp(-np.maximum(0, t - 0.35 * dur) / (0.3 * dur))
    y = np.zeros(n)
    p = dens / SR
    hits = np.where(r.random(n) < p)[0]
    for h in hits:
        m = int(r.uniform(0.0003, 0.002) * SR) + 2
        imp = r.standard_normal(m) * np.exp(-np.arange(m) / (m / 4))
        a = r.pareto(2.2) * 0.3 + 0.05
        S.place(y, imp * a, h / SR)
        if r.random() < 0.15:  # a fragment that rings briefly
            fr, t60, amps = shard_modes(r, r.uniform(0, 0.4))
            S.place(y, modal_hit(fr, t60 * 0.4, amps, 0.15, r) * a * 0.25, h / SR)
    y = S.hp(y, 1200, order=2)
    y = S.peak_eq(y, 4500, 4, q=1.2)
    # grit grinding between sole and tile
    sc = S.bp(r.standard_normal(n), 1500, 9000, order=2) * grit * 0.15
    sc *= np.sin(np.pi * np.clip(t / dur, 0, 1)) ** 2 * (1 + 0.6 * S.smooth_random(n, r, 25, periodic=False))
    return y + sc


def glass_crunch(r):
    y = crunch_core(r, dur=r.uniform(0.18, 0.32), density=r.uniform(700, 1400))
    tail = glass_tinkle(r, n_shards=r.integers(3, 6), spread=0.15, drop_h=0.02) * 0.25
    out = np.zeros(len(y) + int(0.6 * SR))
    S.place(out, y, 0)
    S.place(out, tail, 0.05)
    # floor body: a soft low thump from the shoe weight
    m = int(0.08 * SR)
    th = S.lp(r.standard_normal(m), 250, order=2) * np.exp(-np.arange(m) / (0.02 * SR)) * 0.4
    S.place(out, th, 0.0)
    return out


GLASS_STEP_DB = -8.0    # glass layers re the first release, spikes soft-limited (revised Oct 15, 2026; see footstep_glass)


def footstep_glass(r, step, glass_db=GLASS_STEP_DB):
    """A hard-soled step (recorded) with glass breaking and grinding under heel and toe. The glass layers sit
    `glass_db` below their first-release level and their rare large fracture spikes are soft-limited, so the
    footfall (60 Hz-1.5 kHz) carries the step 2-6 dB above the crunch (2-20 kHz). The first release had the glass
    about 10 dB above the step body, which read as crunch without a foot."""
    g = 10 ** (glass_db / 20)
    st = step.copy()
    a = np.abs(st)
    heel = int(np.argmax(a > 0.3 * a.max()))
    out = np.zeros(len(st) + int(0.6 * SR))
    out[:len(st)] += st * 1.0
    gl = np.zeros_like(out)
    c1 = crunch_core(r, dur=r.uniform(0.08, 0.14), density=r.uniform(900, 1600), grit=0.3)
    S.place(gl, c1 * 0.9 * g, heel / SR + 0.002)
    c2 = crunch_core(r, dur=r.uniform(0.10, 0.18), density=r.uniform(600, 1100), grit=0.5)
    S.place(gl, c2 * 0.7 * g, heel / SR + r.uniform(0.06, 0.11))
    tk = glass_tinkle(r, n_shards=r.integers(2, 5), spread=0.12, drop_h=0.015) * 0.22 * g
    S.place(gl, tk, heel / SR + 0.03)
    # tame the rare very large fracture spikes (they set the peak normalisation and pushed the step down)
    act = np.abs(gl) > 1e-6
    c = 4.0 * np.sqrt(np.mean(gl[act] ** 2)) if act.any() else 1.0
    gl = c * np.tanh(gl / c)
    return out + gl


# ----------------------------------------------------------------- plastic and street furniture
def marker_place(r):
    """Folded plastic evidence tent (ABS/PP, ~10 cm) set down on a tile floor: two edges touching down."""
    fr = np.array([780, 1340, 1910, 2650, 3480, 4700]) * r.uniform(0.9, 1.1)
    t60 = np.array([0.06, 0.05, 0.04, 0.035, 0.03, 0.02]) * r.uniform(0.8, 1.2)
    amps = np.array([1.0, 0.7, 0.55, 0.4, 0.3, 0.2]) * r.uniform(0.7, 1.0, 6)
    y = np.zeros(int(0.35 * SR))
    first = modal_hit(fr, t60, amps, 0.2, r, exc_ms=0.25)
    S.place(y, first, 0.0)
    S.place(y, S.click(0.5, r, lo=2000, hi=14000) * 0.6, 0.0)
    dt = r.uniform(0.006, 0.02)  # second edge
    second = modal_hit(fr * r.uniform(0.98, 1.02), t60, amps, 0.2, r, exc_ms=0.3) * r.uniform(0.45, 0.8)
    S.place(y, second, dt)
    S.place(y, S.click(0.4, r, lo=2500, hi=12000) * 0.35, dt)
    if r.random() < 0.5:  # a tiny rock back onto the first edge
        S.place(y, modal_hit(fr, t60 * 0.7, amps, 0.12, r) * 0.15, dt + r.uniform(0.03, 0.06))
    # floor (tile) knock
    m = int(0.03 * SR)
    S.place(y, S.bp(r.standard_normal(m), 300, 1500, order=2) * np.exp(-np.arange(m) / (0.004 * SR)) * 0.3, 0.0)
    return y


def cone_place(r, on_tile=True):
    """PVC traffic cone with a rubber base (~70 cm, 1.5 kg) set down; base slap, hollow body, wobble."""
    y = np.zeros(int(0.8 * SR))
    # base contact: dull rubber slap
    m = int(0.06 * SR)
    slap = S.lp(r.standard_normal(m), 900, order=2) * np.exp(-np.arange(m) / (0.008 * SR))
    S.place(y, slap * 1.0, 0.0)
    # body thump (low) and hollow cone resonance
    th = S.modal_ir([95, 170, 260], [0.08, 0.06, 0.05], [1.0, 0.6, 0.3], 0.3, phases=[0, 0, 0])
    S.place(y, th * 0.8, 0.0)
    hollow = modal_hit(np.array([420, 690, 1050, 1530, 2200]) * r.uniform(0.92, 1.08),
                       np.array([0.12, 0.09, 0.07, 0.05, 0.03]), np.array([1, 0.6, 0.4, 0.25, 0.1]), 0.4, r,
                       exc_ms=1.5, bright=0.2)
    S.place(y, hollow * 0.35, 0.002)
    # wobble: the base rocks once or twice before settling
    t = r.uniform(0.07, 0.12)
    g = 0.35
    for _ in range(r.integers(1, 3)):
        S.place(y, slap[:int(0.03 * SR)] * g, t)
        S.place(y, hollow * g * 0.4, t)
        t += r.uniform(0.05, 0.09)
        g *= 0.45
    if on_tile:
        y += 0  # tile: same; pavement variants could add grit
    return y


# ----------------------------------------------------------------- devices
def shutter_dslr(r):
    """Mirror-slap DSLR: mirror up, first/second curtain, mirror return, shutter re-cock motor."""
    y = np.zeros(int(0.3 * SR))
    body = np.array([2100, 3300, 4700, 6200, 8100]) * r.uniform(0.95, 1.05)
    t60 = np.array([0.03, 0.025, 0.02, 0.015, 0.01])
    amps = np.array([1, 0.8, 0.6, 0.4, 0.25])
    def clack(t, g, exc=0.2):
        S.place(y, modal_hit(body * r.uniform(0.97, 1.03), t60, amps, 0.08, r, exc_ms=exc) * g, t)
        S.place(y, S.click(0.4, r, lo=1500, hi=15000) * g * 0.8, t)
        m = int(0.02 * SR)
        S.place(y, S.bp(r.standard_normal(m), 150, 800, order=2) * np.exp(-np.arange(m) / (0.003 * SR)) * g * 0.6, t)
    clack(0.0, 1.0)                      # mirror up
    clack(0.004, 0.45, exc=0.1)          # mirror bounce/stop
    clack(0.016, 0.55, exc=0.08)         # 1st curtain
    clack(0.016 + 1 / 125, 0.6, exc=0.08)  # 2nd curtain at 1/125 s
    clack(0.045, 0.85)                   # mirror down
    # re-cock motor: short geared whirr
    m = int(0.065 * SR)
    tt = np.arange(m) / SR
    whirr = np.sin(2 * np.pi * (1650 + 300 * tt / tt[-1]) * tt) * 0.15 + S.bp(r.standard_normal(m), 1500, 6000, order=2) * 0.12
    gear = (np.sin(2 * np.pi * 380 * tt) > 0.7).astype(float)
    whirr += S.bp(gear, 2000, 8000, order=2) * 0.2
    whirr *= np.sin(np.pi * tt / tt[-1]) ** 0.5
    S.place(y, whirr, 0.055)
    clack(0.122, 0.35, exc=0.1)          # charge complete
    return y


def flashlight_click(r, release=True):
    """Tactile tail-cap switch of a metal torch: press click and (optionally) release click."""
    y = np.zeros(int(0.35 * SR))
    fr = np.array([2900, 4300, 6100, 8800]) * r.uniform(0.95, 1.05)
    t60 = np.array([0.03, 0.02, 0.015, 0.01])
    amps = np.array([1.0, 0.7, 0.5, 0.3])
    S.place(y, modal_hit(fr, t60, amps, 0.06, r, exc_ms=0.08), 0.0)
    S.place(y, S.click(0.3, r, lo=2000, hi=16000) * 0.9, 0.0)
    m = int(0.012 * SR)  # thumb/body low knock
    S.place(y, S.bp(r.standard_normal(m), 200, 1200, order=2) * np.exp(-np.arange(m) / (0.002 * SR)) * 0.35, 0.0)
    if release:
        t = r.uniform(0.09, 0.16)
        S.place(y, modal_hit(fr * 1.04, t60 * 0.8, amps, 0.05, r, exc_ms=0.06) * 0.45, t)
        S.place(y, S.click(0.25, r, lo=2500, hi=16000) * 0.4, t)
    return y


# ----------------------------------------------------------------- keys and zipper
def keys_jingle(r, dur=0.9, n_keys=5):
    y = np.zeros(int((dur + 0.6) * SR))
    keys = []
    for k in range(n_keys):
        base = r.uniform(2300, 5200)
        ratios = np.array([1, 2.76, 5.40, 8.93])
        fr = base * ratios * r.uniform(0.98, 1.02, 4)
        keys.append((fr[fr < 19500], r.uniform(0.15, 0.45)))
    # shake gesture: two or three bursts of collisions
    bursts = r.integers(2, 4)
    for b in range(bursts):
        c = b * dur / bursts + r.uniform(0, 0.05)
        nhit = r.integers(6, 14)
        for _ in range(nhit):
            t = c + abs(r.normal(0, 0.06))
            fr, T = keys[r.integers(0, n_keys)]
            t60 = np.full(len(fr), T) * (2500 / fr) ** 0.5
            amps = 10 ** (-r.uniform(0, 10, len(fr)) / 20)
            g = r.uniform(0.2, 1.0)
            S.place(y, modal_hit(fr, t60, amps, 0.5, r, exc_ms=0.1) * g, t)
            S.place(y, S.click(0.3, r, lo=3000, hi=16000) * g * 0.4, t)
    # ring/fob rattle
    ring = np.zeros_like(y)
    for _ in range(r.integers(10, 20)):
        S.place(ring, S.click(0.5, r, lo=1500, hi=9000), r.uniform(0, dur))
    return y + ring * 0.25


def zipper(r, dur=0.75, opening=True, length_cm=None):
    """Bag zip: slider over the teeth; speed profile accelerates then slows; 2.5 mm tooth pitch."""
    pitch = 0.0025
    n = int((dur + 0.15) * SR)
    t = np.arange(int(dur * SR)) / SR
    v = np.sin(np.pi * t / dur) ** 0.7 * r.uniform(0.25, 0.4) * (1.1 if opening else 0.9)
    v *= 1 + 0.25 * S.smooth_random(len(t), r, 6, periodic=False)
    v = np.maximum(v, 0.01)
    pos = np.cumsum(v) / SR
    teeth = np.floor(pos / pitch)
    hits = np.where(np.diff(teeth) > 0)[0]
    y = np.zeros(n)
    for h in hits:
        m = int(0.0012 * SR)
        g = 0.4 + 0.6 * v[h] / v.max()
        imp = r.standard_normal(m) * np.exp(-np.arange(m) / (0.00025 * SR)) * g * r.uniform(0.7, 1.0)
        S.place(y, imp, h / SR)
    y = S.bp(y, 900, 12000, order=2)
    y = S.peak_eq(y, 3200, 6, q=1.5)
    # fabric/bag body resonance and slider friction hiss
    fric = S.bp(r.standard_normal(len(t)), 2000, 9000, order=2) * (v / v.max()) * 0.08
    S.place(y, fric, 0.0)
    bag = S.lp(y, 500, order=2) * 2.0
    y = y + bag
    # end stop click
    S.place(y, S.click(0.8, r, lo=1000, hi=10000) * 0.6, dur)
    return y


# ----------------------------------------------------------------- tape
def tape_unroll(r, dur=1.4):
    """Barrier tape pulled off a hand-held roll: spinning roll rattle, film crackle and flutter."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    pull = np.sin(np.pi * np.clip(t / dur, 0, 1)) ** 0.8
    rot_hz = 4 + 9 * pull                       # roll revolutions per second
    ph = np.cumsum(rot_hz) / SR
    core = np.zeros(n)
    rev = np.where(np.diff(np.floor(ph * 2)) > 0)[0]  # two knocks per revolution (loose core)
    for h in rev:
        m = int(0.01 * SR)
        S.place(core, S.bp(r.standard_normal(m), 400, 2500, order=2) * np.exp(-np.arange(m) / (0.0015 * SR)) * r.uniform(0.4, 0.8), h / SR)
    # film peeling off the roll: dense small crackles whose rate follows the pull speed
    crackle = np.zeros(n)
    rate = 2500 * pull
    hits = np.where(r.random(n) < rate / SR)[0]
    for h in hits:
        m = int(r.uniform(0.0002, 0.0008) * SR) + 2
        S.place(crackle, r.standard_normal(m) * r.uniform(0.1, 0.5), h / SR)
    crackle = S.bp(crackle, 2500, 14000, order=2)
    # loose film flutter in the air
    fl = S.bp(r.standard_normal(n), 300, 3000, order=2) * (0.5 + 0.5 * np.sign(np.sin(2 * np.pi * 22 * t + 3 * S.smooth_random(n, r, 4, periodic=False)))) * 0.06
    y = core * 0.8 + crackle * 0.9 + fl * pull
    return y


def tape_tear(r):
    """Polyethylene barrier tape stretched until it snaps."""
    st = r.uniform(0.25, 0.5)
    n = int((st + 0.5) * SR)
    t = np.arange(n) / SR
    y = np.zeros(n)
    # stretch: rising creak (stick-slip) and crinkle
    m = int(st * SR)
    tt = t[:m]
    rate = 150 + 1600 * (tt / st) ** 2
    hits = np.where(r.random(m) < rate / SR)[0]
    for h in hits:
        k = int(0.0006 * SR) + 2
        S.place(y, r.standard_normal(k) * (0.15 + 0.6 * tt[h] / st), h / SR)
    y[:m] = S.bp(y[:m], 1800, 12000, order=2)
    creak = np.sin(2 * np.pi * np.cumsum(500 + 900 * (tt / st)) / SR) * (tt / st) ** 2 * 0.05
    y[:m] += S.bp(creak * (1 + np.sign(np.sin(2 * np.pi * 45 * tt))), 400, 4000, order=2)
    # snap: sharp broadband crack and the two ends whipping back
    snap = S.click(1.5, r, lo=600, hi=16000) * 2.2
    S.place(y, snap, st)
    w = int(0.12 * SR)
    whip = S.bp(r.standard_normal(w), 400, 5000, order=2) * np.exp(-np.arange(w) / (0.03 * SR)) * 0.25
    whip *= 0.6 + 0.4 * np.sign(np.sin(2 * np.pi * 35 * np.arange(w) / SR))
    S.place(y, whip, st + 0.004)
    return y


def tape_flutter_loop(r, L=20.0):
    """Barrier tape stretched between posts fluttering in a night breeze (periodic loop)."""
    n = int(L * SR)
    gust = 0.62 + 0.38 * np.tanh(1.3 * S.smooth_random(n, r, 0.6))
    # flutter frequency rises with wind speed
    f = 9 + 14 * gust
    ph = np.cumsum(f) / SR
    # make the phase periodic over the loop: scale to an integer number of cycles
    ph *= np.round(ph[-1]) / ph[-1]
    flap = np.zeros(n + int(0.02 * SR))
    edges = np.where(np.diff(np.floor(ph)) > 0)[0]
    for h in edges:
        m = int(0.006 * SR)
        g = gust[h] ** 1.5 * r.uniform(0.5, 1.0)
        S.place(flap, r.standard_normal(m) * np.exp(-np.arange(m) / (0.0012 * SR)) * g, h / SR)
    flap[:len(flap) - n] += flap[n:]  # wrap events that straddle the loop end
    flap = flap[:n]
    flap = S.circ(lambda z: S.peak_eq(S.bp(z, 250, 6000, order=2), 1200, 5, q=1.2), flap)
    rustle = S.periodic_noise(n, r, lambda fq: np.exp(-((np.log(np.maximum(fq, 1)) - np.log(2500)) ** 2) / 0.8)) * 0.04 * gust
    hum = S.periodic_noise(n, r, lambda fq: 1 / (1 + (fq / 400) ** 2)) * 0.05 * gust  # wind on the tape/microphone
    return flap + rustle + hum


def main():
    os.makedirs(OUT, exist_ok=True)
    r = S.rng(777)
    written = []

    def w(name, x, peak=-3.0, **kw):
        y = finish(x, peak, **kw)
        au.write_wav(os.path.join(OUT, name), y)
        written.append((name, len(y) / SR))

    for i in range(4):
        w(f"GlassShardsTinkle_{i + 1:02d}.wav", glass_tinkle(r, n_shards=int(r.integers(6, 16)), spread=r.uniform(0.3, 0.9)))
    for i in range(4):
        w(f"GlassCrunch_{i + 1:02d}.wav", glass_crunch(r))
    steps = [au.read(os.path.join(SRC, f"hill_sfx_foot_concrete_{k}.wav"), mono=True)[:, 0] for k in range(1, 7)]
    for i in range(5):
        w(f"FootstepGlass_{i + 1:02d}.wav", footstep_glass(r, steps[i % 6]))
    for i in range(3):
        w(f"EvidenceMarkerPlace_{i + 1:02d}.wav", marker_place(r))
    for i in range(3):
        w(f"ConePlace_{i + 1:02d}.wav", cone_place(r), hp_hz=30)
    for i in range(2):
        w(f"CameraShutterDSLR_{i + 1:02d}.wav", shutter_dslr(r))
    w("FlashlightClick_01.wav", flashlight_click(r, release=True))
    w("FlashlightClick_02.wav", flashlight_click(r, release=True))
    w("FlashlightClickHalf_01.wav", flashlight_click(r, release=False))
    for i in range(3):
        w(f"KeysJingle_{i + 1:02d}.wav", keys_jingle(r, dur=r.uniform(0.6, 1.2), n_keys=int(r.integers(4, 7))))
    w("ZipperOpen_01.wav", zipper(r, dur=0.8, opening=True))
    w("ZipperClose_01.wav", zipper(r, dur=0.7, opening=False))
    w("ZipperOpen_02.wav", zipper(r, dur=0.45, opening=True))
    for i in range(2):
        w(f"TapeUnroll_{i + 1:02d}.wav", tape_unroll(r, dur=r.uniform(1.0, 1.8)))
    for i in range(2):
        w(f"TapeStretchSnap_{i + 1:02d}.wav", tape_tear(r))
    fl = tape_flutter_loop(r)
    fl = au.gain_to_lufs(fl, -20.0, ceiling_db=-1.0)
    au.write_wav(os.path.join(OUT, "BarrierTapeFlutter_Loop.wav"), fl)
    written.append(("BarrierTapeFlutter_Loop.wav", len(fl) / SR))
    for nme, d in written:
        print(f"{nme:32s} {d:5.2f}s")


if __name__ == "__main__":
    main()
