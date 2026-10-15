"""Shared audio helpers for the NightSoundscape pack (numpy + ffmpeg only)."""
import json
import os
import re
import subprocess
import wave

import numpy as np

SR = 44100


def read(path, sr=SR, mono=False):
    """Decode any file with ffmpeg to float32 at `sr`. Returns (n, ch) array."""
    cmd = ["ffmpeg", "-nostdin", "-v", "error", "-i", path, "-f", "f32le", "-acodec", "pcm_f32le", "-ar", str(sr)]
    if mono:
        cmd += ["-ac", "1"]
    cmd += ["-"]
    raw = subprocess.run(cmd, check=True, capture_output=True).stdout
    ch = 1 if mono else channels(path)
    x = np.frombuffer(raw, dtype=np.float32).reshape(-1, ch).astype(np.float64)
    return x


def channels(path):
    out = subprocess.run(["ffprobe", "-v", "error", "-select_streams", "a:0", "-show_entries", "stream=channels",
                          "-of", "csv=p=0", path], check=True, capture_output=True, text=True).stdout.strip()
    return int(out)


def write_wav(path, x, sr=SR):
    """16-bit PCM WAV with TPDF dither. x: (n,) or (n, ch) float in [-1, 1]."""
    x = np.asarray(x, dtype=np.float64)
    if x.ndim == 1:
        x = x[:, None]
    rng = np.random.default_rng(12345)
    d = (rng.random(x.shape) - rng.random(x.shape)) / 32768.0
    y = np.clip(np.round((x + d) * 32767.0), -32768, 32767).astype("<i2")
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(x.shape[1])
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(y.tobytes())


def write_ogg(path, x, sr=SR, q=6):
    xx = np.asarray(x, dtype=np.float64)
    if xx.ndim == 1:
        xx = xx[:, None]
    subprocess.run(["ffmpeg", "-nostdin", "-v", "error", "-y", "-f", "f32le", "-ar", str(sr), "-ac", str(xx.shape[1]),
                    "-i", "-", "-c:a", "libvorbis", "-q:a", str(q), path],
                   input=xx.astype("<f4").tobytes(), check=True)


def lufs(x, sr=SR):
    """Integrated loudness (EBU R128) via ffmpeg ebur128."""
    xx = np.asarray(x, dtype=np.float64)
    if xx.ndim == 1:
        xx = xx[:, None]
    p = subprocess.run(["ffmpeg", "-nostdin", "-hide_banner", "-f", "f32le", "-ar", str(sr), "-ac", str(xx.shape[1]),
                        "-i", "-", "-af", "ebur128=peak=true", "-f", "null", "-"],
                       input=xx.astype("<f4").tobytes(), capture_output=True)
    err = p.stderr.decode("utf8", "replace")
    m = re.findall(r"I:\s+(-?[\d.]+|-inf) LUFS", err)
    tp = re.findall(r"Peak:\s+(-?[\d.]+|-inf) dBFS", err)
    i = float(m[-1]) if m and m[-1] != "-inf" else -99.0
    t = float(tp[-1]) if tp and tp[-1] != "-inf" else -99.0
    return i, t


def peak_db(x):
    p = float(np.max(np.abs(x))) if len(x) else 0.0
    return 20 * np.log10(max(p, 1e-12))


def db(v):
    return 20 * np.log10(max(float(v), 1e-12))


def gain_to_lufs(x, target, sr=SR, ceiling_db=-1.0):
    y = x * (0.5 / max(np.max(np.abs(x)), 1e-12))
    lim = 10 ** (ceiling_db / 20)
    for _ in range(3):
        i, _ = lufs(y, sr)
        if abs(i - target) < 0.1:
            break
        y = y * 10 ** ((target - i) / 20)
        if np.max(np.abs(y)) > lim:
            y = soft_limit(y, lim)
    return y


def soft_limit(x, ceiling=0.89, release_s=0.08, sr=SR):
    """Simple look-ahead-free peak limiter with smooth gain (for rare peaks only)."""
    a = np.max(np.abs(x if x.ndim == 1 else x), axis=-1) if x.ndim > 1 else np.abs(x)
    need = np.minimum(1.0, ceiling / np.maximum(a, 1e-12))
    # smooth: instant attack via running min over 2 ms window, exponential release
    w = max(1, int(0.002 * sr))
    from numpy.lib.stride_tricks import sliding_window_view
    pad = np.concatenate([need, np.ones(w - 1)])
    mins = sliding_window_view(pad, w).min(axis=1)
    g = np.empty_like(mins)
    r = np.exp(-1.0 / (release_s * sr))
    cur = 1.0
    for i, m in enumerate(mins):
        cur = m if m < cur else (r * cur + (1 - r) * m)
        g[i] = cur
    return x * (g[:, None] if x.ndim > 1 else g)


def normalize_peak(x, target_db=-3.0):
    pk = np.max(np.abs(x))
    return x * (10 ** (target_db / 20) / max(pk, 1e-12))


def fade(x, fin=0.003, fout=0.01, sr=SR):
    x = np.array(x, dtype=np.float64, copy=True)
    n = len(x)
    a = min(n, int(fin * sr))
    b = min(n, int(fout * sr))
    sh = (slice(None),) + ((None,) if x.ndim > 1 else ())
    if a > 0:
        x[:a] *= (np.sin(np.linspace(0, np.pi / 2, a)) ** 2)[sh]
    if b > 0:
        x[n - b:] *= (np.cos(np.linspace(0, np.pi / 2, b)) ** 2)[sh]
    return x


def loop_crossfade(x, length_s, xf_s, start_s=0.0, sr=SR):
    """Seamless loop: take [start, start+L+XF); blend the XF after the loop into the first XF (equal power)."""
    s = int(start_s * sr)
    L = int(length_s * sr)
    F = int(xf_s * sr)
    seg = x[s:s + L + F]
    assert len(seg) == L + F, (len(seg), L + F, len(x))
    head = seg[:F]
    tail = seg[L:L + F]
    t = np.linspace(0, 1, F)
    fi = np.sin(t * np.pi / 2)
    fo = np.cos(t * np.pi / 2)
    sh = (slice(None),) + ((None,) if seg.ndim > 1 else ())
    out = seg[:L].copy()
    out[:F] = head * fi[sh] + tail * fo[sh]
    return out


def seam_jump(x):
    """Sample-level discontinuity at the wrap relative to typical sample deltas (for loop QA)."""
    m = x if x.ndim == 1 else x.mean(axis=1)
    d = np.abs(np.diff(m))
    typical = np.percentile(d, 99) + 1e-12
    return float(abs(m[0] - m[-1]) / typical)


def spectrogram_png(path, out_png, sr=SR, title=None):
    subprocess.run(["ffmpeg", "-nostdin", "-v", "error", "-y", "-i", path, "-lavfi",
                    "showspectrumpic=s=1024x384:legend=1:scale=log:fscale=lin:color=intensity", out_png], check=True)


def env_db(x, sr=SR, hop_s=0.25):
    m = x if x.ndim == 1 else np.sqrt(np.mean(x ** 2, axis=1))
    h = int(hop_s * sr)
    n = len(m) // h
    r = np.sqrt(np.mean(m[:n * h].reshape(n, h) ** 2, axis=1) + 1e-20)
    return 20 * np.log10(r)
