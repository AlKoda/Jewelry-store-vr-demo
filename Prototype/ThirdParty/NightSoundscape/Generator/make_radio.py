"""Original synthesized police-radio sounds: squelch bursts, generic beeps and unintelligible speech-like
chatter (Klatt-style source-filter synthesis of random syllables, see speech_babble.py: no words, no real voices),
plus a 60 s car-radio loop assembled from them. Everything is generated here; nothing is recorded.

Revision October 15, 2026: the chatter was rebuilt after review (the first version droned: 0.3-1.5 Hz envelope
rhythm, 8-10 dB envelope range, weak 1-3 kHz). Squelch and beep files are unchanged (same seed and code path)."""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
import au  # noqa: E402
import speech_babble as SB  # noqa: E402
import synth as S  # noqa: E402

SR = 44100
OUT = sys.argv[1]
SKIP_WRITE_CHATTER = False
SPK_SHELF_DB = 10.0

def radio_chain(x, r, drive=2.5, lo=320, hi=3000, noise=0.03):
    """Handheld/car radio: narrow band, hard compression, a little distortion and hiss."""
    y = S.bp(x, lo, hi, order=4)
    y = y / (np.max(np.abs(y)) + 1e-9)
    y = np.tanh(drive * y) / np.tanh(drive)
    y += noise * S.bp(r.standard_normal(len(y)), 400, 4000, order=2)
    # small loudspeaker: resonance and harshness
    y = S.peak_eq(y, 1800, 5, q=2)
    y = S.peak_eq(y, 900, 2, q=1.5)
    y = S.bp(y, 280, 3800, order=2)
    return y


def squelch_tail(r, dur=None):
    dur = dur or r.uniform(0.12, 0.32)
    m = int(dur * SR)
    nz = r.standard_normal(m)
    nz = S.bp(nz, 500, 3600, order=2)
    env = np.minimum(1, np.arange(m) / (0.004 * SR)) * (1 - np.arange(m) / m) ** 0.5
    y = nz * env
    # the carrier-drop click at the end
    y[-int(0.002 * SR):] *= np.linspace(1, 0, int(0.002 * SR))
    return y


def key_click(r):
    m = int(0.006 * SR)
    return S.bp(r.standard_normal(m), 600, 5000, order=2) * np.exp(-np.arange(m) / (0.0012 * SR))


def beep(freqs, durs, gaps, level=0.5):
    out = []
    for f, d, g in zip(freqs, durs, gaps):
        tt = np.arange(int(d * SR)) / SR
        b = np.sin(2 * np.pi * f * tt) * level
        b = au.fade(b, 0.003, 0.004)
        out.append(b)
        out.append(np.zeros(int(g * SR)))
    return np.concatenate(out)


def comp(x, thr_db=-24.0, ratio=4.0, att=0.002, rel=0.12, sr=SR):
    """Feed-forward RMS compressor (the radio's mic AGC)."""
    env = np.zeros(len(x))
    p = 0.0
    aa, ar = np.exp(-1 / (att * sr)), np.exp(-1 / (rel * sr))
    sq = x * x
    for i in range(0, len(x), 32):  # 32-sample blocks: fast enough, smooth enough
        v = float(np.mean(sq[i:i + 32]))
        c = aa ** 32 if v > p else ar ** 32
        p = c * p + (1 - c) * v
        env[i:i + 32] = p
    lev = 10 * np.log10(env + 1e-12)
    over = np.maximum(0.0, lev - thr_db)
    g = 10 ** (-(over * (1 - 1 / ratio)) / 20)
    return x * g


def preemph(x, tau=750e-6, fmax=6000.0):
    """FM pre-emphasis 1 + s*tau (bilinear), limited by a pole at fmax."""
    from scipy.signal import bilinear, lfilter
    b, a = bilinear([tau, 1.0], [1 / (2 * np.pi * fmax), 1.0], SR)
    return lfilter(b, a, x)


def deemph(x, tau=750e-6):
    from scipy.signal import bilinear, lfilter
    b, a = bilinear([1.0], [tau, 1.0], SR)
    return lfilter(b, a, x)


def radio_voice(speech, r, console=False, noise_db=-46.0, drive=1.6, bg=None):
    """Narrow-band FM voice path: mic + AGC, pre-emphasis, deviation limiter (clipping), 300-3000 Hz channel
    filter, FM hiss, de-emphasis, receiver audio filter and a small loudspeaker."""
    x = speech / (np.max(np.abs(speech)) + 1e-9)
    if bg is not None:
        x = x + bg
    x = S.hp(x, 180 if not console else 120, order=2)
    x = S.peak_eq(x, 2600, 3.0 if not console else 1.5, q=1.0)      # mic presence
    x = comp(x / (np.sqrt(np.mean(x ** 2)) + 1e-9) * 10 ** (-18 / 20), thr_db=-26, ratio=4.0)
    x = x / (np.percentile(np.abs(x), 99.7) + 1e-9)
    x = preemph(x)
    x = x / (np.percentile(np.abs(x), 99.5) + 1e-9)
    x = np.tanh(drive * x) / np.tanh(drive)                                 # deviation limiter
    x = S.lp(x, 3000, order=6)
    x = S.hp(x, 300, order=4)                                               # sub-audible tone filter
    hiss = S.hp(r.standard_normal(len(x)), 300, order=2)                    # FM noise rises with frequency
    hiss = preemph(S.lp(hiss, 3400, order=4)) * 10 ** (noise_db / 20)
    x = x + hiss
    x = deemph(x) * 4.0
    x = S.bp(x, 320, 3000, order=4)
    # loudspeaker in the car / handset: a little cone break-up, no bass, cone resonance, bright top
    x = x / (np.max(np.abs(x)) + 1e-9)
    x = np.tanh(1.3 * x) / np.tanh(1.3)
    x = S.hp(x, 450, order=2)
    x = S.shelf(x, 1300, SPK_SHELF_DB, high=True)
    x = S.peak_eq(x, 1250, 4.0, q=1.4)
    x = S.peak_eq(x, 2500, 3.0, q=1.8)
    x = S.lp(x, 3400, order=2)
    return x


def transmission(r, dur, female=False, with_beep=False, console=False):
    """One over: key-up click, a beat of open carrier, speech, squelch tail (+ optional end beep)."""
    sp = SB.to44k(SB.synth_utterance(r, dur, female))
    lead = np.zeros(int(r.uniform(0.08, 0.16) * SR))
    body = np.concatenate([lead, sp, np.zeros(int(0.04 * SR))])
    # transmitting side background: patrol-car cabin rumble/road noise or a quiet dispatch room
    nb = len(body)
    if console:
        bg = S.lp(r.standard_normal(nb), 900, order=2) * 0.004
    else:
        bg = (S.lp(r.standard_normal(nb), 500, order=2) * 0.013 + S.bp(r.standard_normal(nb), 600, 3000, order=2) * 0.004)
        bg *= 1 + 0.3 * S.smooth_random(nb, r, 1.0, periodic=False)
    tx = radio_voice(body, r, console=console, noise_db=-52 if console else -48, drive=1.4 if console else 1.7, bg=bg)
    tx = np.concatenate([key_click(r) * 0.6, tx, squelch_tail(r) * 0.55])
    if with_beep:
        tx = np.concatenate([tx, np.zeros(int(0.06 * SR)), beep([1450], [0.09], [0.0], level=0.35)])
    return tx


def finish(x, peak=-3.0):
    x = S.hp(x, 150, order=2)
    return au.normalize_peak(au.fade(x, 0.002, 0.01), peak)


def main():
    os.makedirs(OUT, exist_ok=True)
    r = S.rng(4242)
    # squelch bursts (open/close noise without speech)
    for i in range(3):
        sq = np.concatenate([key_click(r) * 0.6, squelch_tail(r, dur=r.uniform(0.18, 0.45))])
        x = finish(sq)
        au.write_wav(os.path.join(OUT, f"RadioSquelch_{i + 1:02d}.wav"), x)
    # beeps: generic talk-permit chirp, acknowledge double-beep, end-of-call tone (not any vendor's tones)
    b1 = beep([1200, 1600], [0.06, 0.08], [0.02, 0.0])
    b2 = beep([1450, 1450], [0.07, 0.07], [0.07, 0.0])
    b3 = beep([980], [0.22], [0.0])
    for i, b in enumerate([b1, b2, b3]):
        y = radio_chain(np.concatenate([b, np.zeros(int(0.03 * SR))]), r, drive=1.5, noise=0.01)
        au.write_wav(os.path.join(OUT, f"RadioBeep_{i + 1:02d}.wav"), finish(y, -6.0))
    # chatter: own seed, so the squelch and beep files above stay bit-identical to the first release
    rc = S.rng(15102026)
    # (seconds of speech, female, end beep, console = dispatcher at a fixed console; else officer in a car)
    plan = [(2.3, False, False, True), (3.3, True, False, True), (1.7, False, True, False), (4.0, False, False, False),
            (2.7, True, True, False)]
    txs = []
    for i, (d, fem, bp_, con) in enumerate(plan):
        tx = transmission(rc, d, fem, bp_, con)
        txs.append(tx)
        if SKIP_WRITE_CHATTER:
            continue
        au.write_wav(os.path.join(OUT, f"RadioChatter_{i + 1:02d}.wav"), finish(tx))
    # extra overs used only inside the loop
    extra = [transmission(rc, d, fem, False, con) for d, fem, con in
             [(2.0, True, True), (2.9, False, False), (1.3, False, False), (2.5, True, True), (1.9, False, False)]]
    disp = [txs[0], txs[1], extra[0], extra[3]]
    unit = [txs[2], txs[3], txs[4], extra[1], extra[2], extra[4]]
    # 60 s loop for the parked car's radio: call/answer exchanges, a few lone overs and kerchunks on a hiss bed
    L = 60.0
    n = int(L * SR)
    bed = 0.004 * S.periodic_noise(n, rc, lambda f: np.exp(-((f - 1500) / 1200) ** 2))
    y = bed.copy()
    t = 1.2
    i_d = i_u = 0
    while True:
        seq = []
        if rc.random() < 0.75:
            seq.append(disp[i_d % len(disp)] * rc.uniform(0.75, 1.0))
            i_d += 1
            if rc.random() < 0.8:
                seq.append(None)  # answer gap
                seq.append(unit[i_u % len(unit)] * rc.uniform(0.6, 0.95))
                i_u += 1
        else:
            seq.append(unit[i_u % len(unit)] * rc.uniform(0.6, 0.95))
            i_u += 1
        need = sum(len(z) / SR + 0.0 for z in seq if z is not None) + 1.2
        if t + need > L - 0.4:
            break
        for z in seq:
            if z is None:
                t += rc.uniform(0.45, 1.1)
                continue
            S.place(y, z, t)
            t += len(z) / SR
        gap = rc.uniform(3.5, 8.5)
        if rc.random() < 0.3 and gap > 3:  # someone keys up without talking (kerchunk)
            sq = np.concatenate([key_click(rc) * 0.6, squelch_tail(rc, dur=rc.uniform(0.15, 0.3))])
            tk = t + gap * rc.uniform(0.3, 0.6)
            if tk + len(sq) / SR < L - 0.3:  # never let an event straddle the loop point
                S.place(y, sq * 0.5, tk)
        t += gap
    y = S.circ(lambda z: S.hp(z, 150, order=2), y)
    y = au.gain_to_lufs(y, -20.0, ceiling_db=-2.0)
    au.write_ogg(os.path.join(OUT, "PoliceCarRadio_Loop.ogg"), y, q=5)
    print("radio loop", au.lufs(y), round(au.seam_jump(y), 2))


if __name__ == "__main__":
    main()
