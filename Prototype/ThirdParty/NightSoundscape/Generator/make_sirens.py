"""Distant police-siren pass-bys built from real CC0 siren recordings (wail and yelp, Jefflix, freesound
157867 / 157866, loops prepared by Aarymanv/hillbomb-sf) with a physical distance model: moving source,
propagation delay (Doppler), 1/r spreading, air absorption, building occlusion and a street-canyon reverb.
Mono one-shots for a 2D/3D ambient emitter."""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
import au  # noqa: E402
import synth as S  # noqa: E402

SR = 44100
SRC = sys.argv[1]   # work/src
OUT = sys.argv[2]
C = 343.0


def loop_region(path, a, b, need_s):
    x = au.read(path, mono=True)[:, 0]
    seg = x[int(a * SR):int(b * SR)]
    reps = int(np.ceil(need_s * SR / len(seg))) + 1
    return np.tile(seg, reps)[:int(need_s * SR)]


def stft_gain(x, gain_fn, nfft=2048, hop=512):
    """Time-varying spectral gain: gain_fn(frame_time_s, freqs) -> gains."""
    win = np.hanning(nfft)
    n = len(x)
    pad = np.concatenate([np.zeros(nfft), x, np.zeros(nfft)])
    out = np.zeros(len(pad))
    wsum = np.zeros(len(pad))
    f = np.fft.rfftfreq(nfft, 1 / SR)
    for s in range(0, len(pad) - nfft, hop):
        fr = pad[s:s + nfft] * win
        t = (s + nfft / 2 - nfft) / SR
        X = np.fft.rfft(fr) * gain_fn(t, f)
        out[s:s + nfft] += np.fft.irfft(X, nfft) * win
        wsum[s:s + nfft] += win ** 2
    out /= np.maximum(wsum, 1e-6)
    return out[nfft:nfft + n]


def passby(src, dur, d_perp, speed, x_start, occl_hz, seed, wet=0.5, open_gap=None):
    """Render the received signal of a source moving along x at perpendicular distance d_perp."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    xs = x_start + speed * t
    r = np.sqrt(d_perp ** 2 + xs ** 2)
    delay = r / C * SR
    delay -= delay.min() - 1  # absolute latency is irrelevant; keep Doppler (its derivative)
    y = S.frac_delay_read(src, delay)
    y *= 30.0 / r  # spreading, referenced to 30 m

    def gain_fn(tt, f):
        i = int(np.clip(tt * SR, 0, n - 1))
        rr = r[i]
        # air absorption (approximate, 20 C / 60 % RH): ~0.1 dB/100 m at 1 kHz, ~1 dB/100 m at 4 kHz, more above
        a_db = rr / 100.0 * (0.1 * (f / 1000) ** 1.6)
        g = 10 ** (-a_db / 20)
        # building occlusion: stronger the further along the street the car is (line of sight blocked)
        xx = abs(xs[i])
        fc = occl_hz * (1 + 2.5 * np.exp(-(xx / 60.0) ** 2))
        if open_gap is not None and abs(xs[i] - open_gap[0]) < open_gap[1]:
            fc *= 2.2  # passing a side-street opening: brief line of sight
        g = g / np.sqrt(1 + (f / fc) ** 4)
        return g
    y = stft_gain(y, gain_fn)
    ir = S.reverb_ir(2.2, dur=2.8, stereo=False, predelay=0.035, seed=seed,
                     early=[(0.052, 0.4), (0.089, 0.3), (0.137, 0.2)], flutter=(0.094, 0.6, 16),
                     bands=[(20, 250, 1.1), (250, 1000, 1.0), (1000, 3000, 0.75), (3000, 7000, 0.45),
                            (7000, 20000, 0.2)])
    # direct-to-reverberant ratio falls with distance: reverb follows the source level but less steeply
    rev_in = y * np.sqrt(r / 30.0)
    rv = S.convolve(rev_in, ir, wet=1.0, dry=0.0)[:n]
    rv /= np.sqrt(np.mean(rv ** 2)) + 1e-12
    rv *= np.sqrt(np.mean(y ** 2)) * wet * 1.6
    out = y + rv
    out = S.hp(out, 180, order=2)
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    wail = loop_region(os.path.join(SRC, "hill_loops_siren_wail.wav"), 0.25, 10.343968, 40)
    yelp = loop_region(os.path.join(SRC, "hill_loops_siren_yelp.wav"), 0.25, 6.31585, 40)
    # 1) wail, far (two blocks away), crossing the listener's line of sight only briefly
    dur = 26.0
    y1 = passby(wail, dur, d_perp=210, speed=17.0, x_start=-221, occl_hz=900, seed=21, wet=0.75,
                open_gap=(0.0, 25.0))
    y1 = au.fade(y1, 2.5, 4.0)
    # 2) wail -> yelp -> wail on a nearer street (an intersection being cleared), ~90 m
    dur2 = 22.0
    n2 = int(dur2 * SR)
    sw = np.zeros(n2)
    tt = np.arange(n2) / SR
    m = ((tt > 8.0) & (tt < 13.0)).astype(float)
    m = np.convolve(m, np.hanning(int(0.12 * SR)) / np.sum(np.hanning(int(0.12 * SR))), mode="same")
    sw = wail[:n2] * (1 - m) + yelp[:n2] * m
    y2 = passby(sw, dur2, d_perp=90, speed=13.0, x_start=-140, occl_hz=1300, seed=22, wet=0.55)
    y2 = au.fade(y2, 2.0, 3.5)
    for name, y in [("SirenWailPassDistant_01.wav", y1), ("SirenWailYelpPass_02.wav", y2)]:
        y = au.normalize_peak(y, -3.0)
        au.write_wav(os.path.join(OUT, name), y)
        print(name, len(y) / SR, au.lufs(y))


if __name__ == "__main__":
    main()
