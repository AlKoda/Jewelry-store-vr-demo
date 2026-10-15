"""Original synthesized burglar-alarm bell (trembler / vibrating gong bell) loops.

Physical model: a steel gong with split (doublet) modes measured from a CC0 fire-alarm-bell
recording (msx2plus, freesound 678345, via Dhruv-Mishra/VoxelGame) is struck by a trembler hammer at
~18-19 strikes/s. Between strikes the modes ring freely; each strike partly damps the ringing
(hammer contact) and re-excites every mode with a slightly random strength. Rendered three times over and
the middle period kept, so the loop is periodic and seamless. Only analysis numbers (mode ratios) come from
the recording; no recorded audio is used.
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
import au  # noqa: E402
import synth as S  # noqa: E402

SR = 44100
OUT = sys.argv[1]
LOOP = 20.0
T60_SCALE = float(os.environ.get("T60_SCALE", "2.5"))
RATTLE = float(os.environ.get("RATTLE", "0.12"))

# modes: base frequency (Hz), level (dB), T60 (s); doublet split in Hz
MODES = [
    (598.0, -17, 2.4, 0.9),
    (985.0, -22, 1.2, 1.2),
    (1190.0, -24, 1.0, 1.4),
    (1521.0, -10, 1.9, 1.7),
    (2641.0, -7, 1.5, 2.6),
    (3932.0, -6, 1.15, 3.1),
    (5360.0, 0, 0.9, 4.4),
    (6928.0, -2, 0.7, 5.2),
    (8630.0, -10, 0.5, 6.0),
    (10230.0, -17, 0.35, 7.0),
    (11890.0, -23, 0.25, 8.0),
    (13650.0, -30, 0.18, 9.0),
]


def strike_times(r, loop, rate=18.6):
    """Strike instants over one loop with jitter; the count is chosen so the pattern tiles exactly."""
    n = int(round(loop * rate))
    base = np.arange(n) * (loop / n)
    jit = r.normal(0, 0.0022, n)          # ~2 ms timing jitter of the armature
    t = np.sort((base + jit) % loop)
    amp = np.clip(r.normal(1.0, 0.12, n), 0.6, 1.3)
    # occasional weak strike (armature bounce)
    weak = r.random(n) < 0.05
    amp[weak] *= 0.45
    return t, amp


def render_bell(seed, loop=LOOP, scale=1.0, damp=0.85):
    r = S.rng(seed)
    t_str, a_str = strike_times(r, loop)
    # three periods of the same pattern
    times = np.concatenate([t_str, t_str + loop, t_str + 2 * loop])
    amps = np.concatenate([a_str, a_str, a_str])
    total = 3 * loop
    n = int(total * SR)
    y = np.zeros(n)
    # mode table (doublets)
    fr, lv, t60 = [], [], []
    for f, l, T, split in MODES:
        for k, s in enumerate((-split / 2, split / 2)):
            fr.append((f + s) * scale)
            lv.append(10 ** ((l - (0 if k == 0 else 1.5)) / 20))
            t60.append(T * (1.0 if k == 0 else 0.85))
    # weaker inharmonic gong modes filling the spectrum (fixed per bell)
    rr = S.rng(seed + 99)
    for f in np.sort(rr.uniform(1800, 15000, 22)):
        fr.append(f * scale)
        lv.append(10 ** (rr.uniform(-34, -20) / 20))
        t60.append(float(np.clip(0.9 * (3000 / f) ** 0.8, 0.08, 1.2)))
    fr = np.array(fr)
    lv = np.array(lv)
    dampv = np.where(fr < 3000, min(0.97, damp + 0.08), damp)
    alpha = 6.907755 / (np.array(t60) * T60_SCALE)
    w = 2 * np.pi * fr
    st = np.zeros(len(fr), dtype=complex)  # complex modal amplitudes
    exc_rand = S.rng(seed + 7)
    # per-mode excitation varies a little per strike (contact point / force)
    for i, t0 in enumerate(times):
        t1 = times[i + 1] if i + 1 < len(times) else total
        i0, i1 = int(t0 * SR), int(t1 * SR)
        st *= dampv  # hammer contact damping (higher modes damped more)
        jitter_db = exc_rand.normal(0, 1.5, len(fr))
        st += amps[i] * lv * 10 ** (jitter_db / 20) * np.exp(1j * exc_rand.normal(0, 0.15, len(fr)))
        tt = np.arange(i1 - i0) / SR
        seg = (st[None, :] * np.exp((-alpha[None, :] + 1j * w[None, :]) * tt[:, None])).imag.sum(axis=1)
        y[i0:i1] += seg
        st = st * np.exp((-alpha + 1j * w) * (i1 - i0) / SR)
        # hammer contact click: short broadband tick, plus armature buzz
        clk = S.click(0.8, exc_rand, lo=2500, hi=12000) * 0.06 * amps[i]
        S.place(y, clk, t0)
        # hammer bounce: a weaker second micro-contact 1-3 ms later re-excites the modes slightly
        if exc_rand.random() < 0.6:
            tb = t0 + exc_rand.uniform(0.0012, 0.0032)
            ib = int(tb * SR)
            if ib < i1:
                tb_rel = (ib - i0) / SR
                st_b = 0.18 * amps[i] * lv * np.exp(1j * exc_rand.uniform(0, 2 * np.pi, len(fr)))
                tt2 = np.arange(i1 - ib) / SR
                y[ib:i1] += (st_b[None, :] * np.exp((-alpha[None, :] + 1j * w[None, :]) * tt2[:, None])).imag.sum(axis=1)
                st = st + st_b * np.exp((-alpha + 1j * w) * (i1 - ib) / SR)
    # armature / coil buzz (strike-rate harmonics, very low level)
    buzz = np.zeros(n)
    pulse = np.exp(-np.arange(int(0.012 * SR)) / (0.0025 * SR))
    for i, t0 in enumerate(times):
        S.place(buzz, pulse * amps[i], t0 - 0.004)
    buzz = S.bp(buzz, 70, 600, order=2) * 0.6
    y = y + buzz * np.max(np.abs(y)) * 0.02
    # mechanical rattle of armature, hammer and cover: short noise bursts per strike filling the
    # spectrum between the bell partials (as heard in real trembler bells)
    rat = np.zeros(n)
    rr2 = S.rng(seed + 3)
    m = int(0.018 * SR)
    for i, t0 in enumerate(times):
        b = rr2.standard_normal(m) * np.exp(-np.arange(m) / (0.004 * SR)) * amps[i]
        S.place(rat, b, t0 + rr2.uniform(0, 0.002))
    rat = S.peak_eq(S.bp(rat, 700, 11000, order=2), 3500, 4, q=0.8)
    rat *= RATTLE * np.sqrt(np.mean(y ** 2)) / (np.sqrt(np.mean(rat ** 2)) + 1e-12)
    y = y + rat
    mid = y[int(loop * SR):int(2 * loop * SR)]
    return mid


def housing(x):
    """Bell under a steel cover inside a bell box: a couple of cover resonances, softened top end."""
    y = S.peak_eq(x, 1150, 3.0, q=3.0)
    y = S.peak_eq(y, 3300, 2.0, q=4.0)
    y = S.shelf(y, 9000, -6.0, high=True)
    return y


def main():
    os.makedirs(OUT, exist_ok=True)
    bell = render_bell(seed=6611)
    bell = S.circ(lambda z: S.hp(housing(z), 120, order=2), bell)

    # 1) Close: as heard a few metres from the bell box on the facade; dry with a short early reflection
    near_ir = S.reverb_ir(0.35, dur=0.6, stereo=False, predelay=0.004, seed=11,
                          early=[(0.009, 0.45), (0.017, 0.25), (0.031, 0.15)])
    close = S.convolve_circular(bell, near_ir, wet=0.18, dry=1.0)
    close = au.gain_to_lufs(close, -20.0, ceiling_db=-1.0)
    au.write_wav(os.path.join(OUT, "AlarmBell_Close_Loop.wav"), close)

    # 2) Street: across the street (~15 m) with facade flutter echoes and street-canyon reverb (stereo)
    street_ir = S.reverb_ir(1.9, dur=2.4, stereo=True, predelay=0.03, seed=12,
                            early=[(0.044, 0.5), (0.071, 0.35), (0.118, 0.25)],
                            flutter=(0.088, 0.62, 14),
                            bands=[(20, 250, 1.1), (250, 1000, 1.0), (1000, 3000, 0.8),
                                   (3000, 7000, 0.5), (7000, 20000, 0.25)])
    far = S.circ(lambda z: S.lp(z, 7500, order=2), bell)  # air absorption over ~15-20 m
    street = S.convolve_circular(far, street_ir, wet=0.55, dry=0.8)
    # small inter-channel delay of the direct sound so it is not dead centre
    street[:, 1] = np.roll(street[:, 1], int(0.0004 * SR))
    street = au.gain_to_lufs(street, -20.0, ceiling_db=-1.0)
    au.write_ogg(os.path.join(OUT, "AlarmBell_Street_Loop.ogg"), street, q=6)

    # 3) Interior: heard inside the shop through the shopfront/door gap; muffled, room reverb
    thru = S.circ(lambda z: S.peak_eq(S.lp(z, 2600, order=4), 900, 3.0, q=1.0), bell)
    room_ir = S.reverb_ir(0.75, dur=1.2, stereo=False, predelay=0.012, seed=13,
                          early=[(0.018, 0.35), (0.029, 0.3), (0.047, 0.2)],
                          bands=[(20, 250, 1.2), (250, 1000, 1.0), (1000, 3000, 0.85),
                                 (3000, 7000, 0.6), (7000, 20000, 0.4)])
    inside = S.convolve_circular(thru, room_ir, wet=0.6, dry=0.7)
    inside = au.gain_to_lufs(inside, -20.0, ceiling_db=-1.0)
    au.write_wav(os.path.join(OUT, "AlarmBell_Interior_Loop.wav"), inside)

    # QA
    for nme, x in [("close", close), ("street", street), ("inside", inside)]:
        print(nme, "LUFS/TP", au.lufs(x), "seam", round(au.seam_jump(x), 2), "len", len(x) / SR)


if __name__ == "__main__":
    main()
