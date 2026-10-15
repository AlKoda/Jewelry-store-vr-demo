"""Synthesis building blocks (numpy/scipy) for original NightSoundscape sounds."""
import numpy as np
from scipy import signal

SR = 44100


def rng(seed):
    return np.random.default_rng(seed)


def t_axis(dur, sr=SR):
    return np.arange(int(round(dur * sr))) / sr


def bp(x, lo, hi, order=4, sr=SR):
    sos = signal.butter(order, [lo, hi], "bandpass", fs=sr, output="sos")
    return signal.sosfilt(sos, x, axis=0)


def lp(x, fc, order=4, sr=SR, zero_phase=False):
    sos = signal.butter(order, fc, "lowpass", fs=sr, output="sos")
    return signal.sosfiltfilt(sos, x, axis=0) if zero_phase else signal.sosfilt(sos, x, axis=0)


def hp(x, fc, order=4, sr=SR, zero_phase=False):
    sos = signal.butter(order, fc, "highpass", fs=sr, output="sos")
    return signal.sosfiltfilt(sos, x, axis=0) if zero_phase else signal.sosfilt(sos, x, axis=0)


def peak_eq(x, f0, gain_db, q=1.0, sr=SR):
    """RBJ peaking EQ biquad."""
    A = 10 ** (gain_db / 40)
    w0 = 2 * np.pi * f0 / sr
    alpha = np.sin(w0) / (2 * q)
    b = np.array([1 + alpha * A, -2 * np.cos(w0), 1 - alpha * A])
    a = np.array([1 + alpha / A, -2 * np.cos(w0), 1 - alpha / A])
    return signal.lfilter(b / a[0], a / a[0], x, axis=0)


def shelf(x, f0, gain_db, high=True, sr=SR):
    A = 10 ** (gain_db / 40)
    w0 = 2 * np.pi * f0 / sr
    alpha = np.sin(w0) / 2 * np.sqrt(2)
    c = np.cos(w0)
    if high:
        b = [A * ((A + 1) + (A - 1) * c + 2 * np.sqrt(A) * alpha), -2 * A * ((A - 1) + (A + 1) * c),
             A * ((A + 1) + (A - 1) * c - 2 * np.sqrt(A) * alpha)]
        a = [(A + 1) - (A - 1) * c + 2 * np.sqrt(A) * alpha, 2 * ((A - 1) - (A + 1) * c),
             (A + 1) - (A - 1) * c - 2 * np.sqrt(A) * alpha]
    else:
        b = [A * ((A + 1) - (A - 1) * c + 2 * np.sqrt(A) * alpha), 2 * A * ((A - 1) - (A + 1) * c),
             A * ((A + 1) - (A - 1) * c - 2 * np.sqrt(A) * alpha)]
        a = [(A + 1) + (A - 1) * c + 2 * np.sqrt(A) * alpha, -2 * ((A - 1) + (A + 1) * c),
             (A + 1) + (A - 1) * c - 2 * np.sqrt(A) * alpha]
    b = np.array(b) / a[0]
    a = np.array(a) / a[0]
    return signal.lfilter(b, a, x, axis=0)


def pink(n, r):
    """Pink-ish noise via FFT shaping."""
    w = r.standard_normal(n)
    W = np.fft.rfft(w)
    f = np.fft.rfftfreq(n, 1 / SR)
    f[0] = f[1]
    W /= np.sqrt(f)
    y = np.fft.irfft(W, n)
    return y / (np.std(y) + 1e-12)


def brown(n, r):
    w = r.standard_normal(n)
    W = np.fft.rfft(w)
    f = np.fft.rfftfreq(n, 1 / SR)
    f[0] = f[1]
    W /= f
    y = np.fft.irfft(W, n)
    return y / (np.std(y) + 1e-12)


def periodic_noise(n, r, shape=None):
    """Noise that is exactly periodic over n samples (circular), optional spectral shape fn(f)->gain."""
    W = np.fft.rfft(r.standard_normal(n))
    if shape is not None:
        f = np.fft.rfftfreq(n, 1 / SR)
        W *= shape(f)
    y = np.fft.irfft(W, n)
    return y / (np.std(y) + 1e-12)


def smooth_random(n, r, rate_hz, periodic=True):
    """Slow random modulation signal (zero mean, unit std), band-limited to rate_hz; circular if periodic."""
    W = np.fft.rfft(r.standard_normal(n))
    f = np.fft.rfftfreq(n, 1 / SR)
    W *= np.exp(-(f / rate_hz) ** 2)
    W[0] = 0
    y = np.fft.irfft(W, n)
    return y / (np.std(y) + 1e-12)


def env_adsr(n, a, d, s_level, r_t, sr=SR):
    e = np.ones(n) * s_level
    na, nd, nr = int(a * sr), int(d * sr), int(r_t * sr)
    na = min(na, n)
    e[:na] = np.linspace(0, 1, na)
    nd = min(nd, n - na)
    e[na:na + nd] = np.linspace(1, s_level, nd)
    if nr > 0:
        e[-nr:] *= np.linspace(1, 0, nr)
    return e


def exp_decay(n, t60, sr=SR):
    return np.exp(-6.907755 * np.arange(n) / (t60 * sr))


def modal_ir(freqs, t60s, amps, dur, sr=SR, phases=None):
    t = np.arange(int(dur * sr)) / sr
    y = np.zeros_like(t)
    for i, (f, T, a) in enumerate(zip(freqs, t60s, amps)):
        if f >= sr / 2 * 0.95:
            continue
        ph = 0.0 if phases is None else phases[i]
        y += a * np.exp(-6.907755 * t / T) * np.sin(2 * np.pi * f * t + ph)
    return y


def click(n_ms, r, lo=800, hi=12000, sr=SR, decay_ms=None):
    n = max(8, int(n_ms * sr / 1000))
    x = r.standard_normal(n)
    d = decay_ms if decay_ms else n_ms / 3
    x *= np.exp(-np.arange(n) / (d * sr / 1000))
    return bp(x, lo, min(hi, sr / 2 * 0.95), order=2)


def place(dst, src, start_s, gain=1.0, sr=SR):
    i = int(round(start_s * sr))
    if i >= len(dst):
        return dst
    j = min(len(dst), i + len(src))
    if i < 0:
        src = src[-i:]
        j = min(len(dst), len(src))
        i = 0
    dst[i:j] += gain * src[:j - i]
    return dst


def reverb_ir(rt60, dur=None, sr=SR, stereo=True, predelay=0.01, seed=0, bands=None,
              early=None, flutter=None, density_lp=None):
    """Synthetic room/street impulse response.

    bands: list of (lo, hi, rt60_scale). early: list of (time_s, gain). flutter: (period_s, gain_per_bounce, n).
    """
    r = rng(seed)
    if dur is None:
        dur = rt60 * 1.2 + predelay
    n = int(dur * sr)
    ch = 2 if stereo else 1
    out = np.zeros((n, ch))
    if bands is None:
        bands = [(20, 250, 1.15), (250, 1000, 1.0), (1000, 3000, 0.85), (3000, 7000, 0.6), (7000, 20000, 0.35)]
    t = np.arange(n) / sr
    for lo, hi, sc in bands:
        nz = r.standard_normal((n, ch))
        nz = bp(nz, lo, min(hi, sr / 2 * 0.95), order=3)
        env = np.exp(-6.907755 * np.maximum(t - predelay, 0) / (rt60 * sc))
        env[t < predelay] = 0
        # soft onset of the diffuse tail
        env *= 1 - np.exp(-np.maximum(t - predelay, 0) / 0.012)
        out += nz * env[:, None]
    out /= np.sqrt(np.sum(out ** 2) / ch) + 1e-12
    if early:
        for (tt, g) in early:
            k = int(tt * sr)
            if k < n:
                for c in range(ch):
                    jitter = int(r.integers(0, int(0.0015 * sr))) if ch > 1 else 0
                    if k + jitter < n:
                        out[k + jitter, c] += g
    if flutter:
        per, g, nb = flutter
        for b in range(1, nb + 1):
            k = int((predelay + per * b) * sr)
            if k >= n:
                break
            for c in range(ch):
                kk = k + int(r.integers(0, int(0.002 * sr)))
                if kk < n:
                    out[kk, c] += (g ** b) * (1 if (b % 2) else 0.8)
    if density_lp:
        out = lp(out, density_lp, order=2)
    return out if stereo else out[:, 0]


def convolve(x, ir, wet=0.3, dry=1.0):
    """x mono (n,) -> stereo if ir stereo. Returns dry*x + wet*conv (length n + len(ir) - 1)."""
    from scipy.signal import fftconvolve
    if ir.ndim == 1:
        y = fftconvolve(x, ir)
        out = np.zeros_like(y)
        out[:len(x)] += dry * x
        return out + wet * y / (np.sqrt(np.sum(ir ** 2)) + 1e-12)
    ys = [fftconvolve(x, ir[:, c]) for c in range(ir.shape[1])]
    y = np.stack(ys, axis=1)
    out = np.zeros_like(y)
    out[:len(x)] += dry * x[:, None]
    return out + wet * y / (np.sqrt(np.sum(ir ** 2) / ir.shape[1]) + 1e-12)


def convolve_circular(x, ir, wet=0.3, dry=1.0):
    """Circular convolution so a periodic loop stays seamless (tail wraps to the start)."""
    n = len(x)
    if ir.ndim == 1:
        ir = ir[:, None]
        mono_out = True
    else:
        mono_out = False
    m = ir.shape[0]
    L = n
    outs = []
    for c in range(ir.shape[1]):
        h = ir[:, c]
        # fold IR longer than the loop onto itself
        if m > L:
            hh = np.zeros(L)
            for k in range(0, m, L):
                seg = h[k:k + L]
                hh[:len(seg)] += seg
            h = hh
        H = np.fft.rfft(h, L)
        X = np.fft.rfft(x, L)
        y = np.fft.irfft(X * H, L)
        outs.append(y)
    y = np.stack(outs, axis=1)
    norm = np.sqrt(np.sum(ir ** 2) / ir.shape[1]) + 1e-12
    out = dry * x[:, None] + wet * y / norm
    return out[:, 0] if mono_out else out


def frac_delay_read(x, delay_samples):
    """Read x at time index (n - delay) with linear interpolation (for Doppler)."""
    n = np.arange(len(delay_samples))
    pos = n - delay_samples
    i0 = np.floor(pos).astype(int)
    fr = pos - i0
    valid = (i0 >= 0) & (i0 + 1 < len(x))
    y = np.zeros(len(delay_samples))
    y[valid] = x[i0[valid]] * (1 - fr[valid]) + x[i0[valid] + 1] * fr[valid]
    return y


def time_varying_lp(x, fc_curve, sr=SR, block=512):
    """One-pole-ish time varying lowpass (2nd order state variable) driven by a cutoff curve."""
    y = np.zeros_like(x)
    lowA = 0.0
    bandA = 0.0
    lowB = 0.0
    bandB = 0.0
    q = 0.75
    for s in range(0, len(x), block):
        fc = float(np.mean(fc_curve[s:s + block]))
        f = 2 * np.sin(np.pi * min(fc, sr * 0.2) / sr)
        seg = x[s:s + block]
        out = np.empty_like(seg)
        for i, v in enumerate(seg):
            lowA += f * bandA
            high = v - lowA - q * bandA
            bandA += f * high
            lowB += f * bandB
            highB = lowA - lowB - q * bandB
            bandB += f * highB
            out[i] = lowB
        y[s:s + block] = out
    return y


def circ(fn, x, reps=3):
    """Apply a (causal) processing function to a periodic loop without a start-up transient."""
    n = len(x)
    xx = np.concatenate([x] * reps, axis=0)
    y = fn(xx)
    return y[(reps - 1) * n:reps * n] if reps > 1 else y
