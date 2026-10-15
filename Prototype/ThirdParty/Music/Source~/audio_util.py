"""Shared helpers: decode, circular resample, loudness, OGG export, loop checks."""
import subprocess, json, re, numpy as np, os, tempfile

def decode(path, sr=48000, ch=2):
    raw = subprocess.run(['ffmpeg', '-v', 'error', '-i', path, '-map', '0:a:0', '-ac', str(ch), '-ar', str(sr),
                          '-f', 'f64le', '-'], capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype=np.float64).reshape(-1, ch).copy()

def resample(x, sr_in, sr_out):
    ch = x.shape[1]
    p = subprocess.run(['ffmpeg', '-v', 'error', '-f', 'f64le', '-ar', str(sr_in), '-ac', str(ch), '-i', '-',
                        '-af', 'aresample=resampler=soxr:precision=28', '-ar', str(sr_out),
                        '-f', 'f64le', '-'], input=np.ascontiguousarray(x).tobytes(), capture_output=True, check=True)
    return np.frombuffer(p.stdout, dtype=np.float64).reshape(-1, ch).copy()

def circular_resample(loop, sr_in, sr_out):
    """Resample a seamless loop without edge effects: tile 3x, resample, keep the middle period."""
    n_in = len(loop)
    n_out = n_in * sr_out / sr_in
    assert abs(n_out - round(n_out)) < 1e-9, 'loop length must map to an integer number of output samples'
    n_out = int(round(n_out))
    y = resample(np.vstack([loop, loop, loop]), sr_in, sr_out)
    # soxr output is time-aligned with input (no added delay in ffmpeg's aresample)
    return y[n_out:2 * n_out].copy()

def write_wav(path, x, sr):
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'f64le', '-ar', str(sr), '-ac', str(x.shape[1]), '-i', '-',
                    '-c:a', 'pcm_f32le', path], input=np.ascontiguousarray(x).tobytes(), check=True)

def loudness(x, sr, tile=1):
    """Integrated loudness (LUFS) and true peak (dBTP) via ffmpeg ebur128."""
    with tempfile.NamedTemporaryFile(suffix='.wav', delete=False) as f:
        tmp = f.name
    write_wav(tmp, np.vstack([x] * tile), sr)
    r = subprocess.run(['ffmpeg', '-v', 'info', '-nostats', '-i', tmp, '-af', 'ebur128=peak=true', '-f', 'null', '-'],
                       capture_output=True, text=True)
    os.unlink(tmp)
    s = r.stderr[r.stderr.rfind('Summary:'):]
    I = float(re.search(r'I:\s+(-?[\d.]+|-inf) LUFS', s).group(1))
    lra = float(re.search(r'LRA:\s+(-?[\d.]+) LU', s).group(1))
    tp = float(re.search(r'Peak:\s+(-?[\d.]+|-inf) dBFS', s).group(1))
    return I, lra, tp

def normalize(x, sr, target_lufs, tp_ceiling=-1.0, tile=2):
    I, lra, tp = loudness(x, sr, tile)
    g = target_lufs - I
    if tp + g > tp_ceiling:
        g = tp_ceiling - tp
    y = x * 10 ** (g / 20)
    I2, lra2, tp2 = loudness(y, sr, tile)
    return y, dict(gain_db=round(g, 2), lufs=round(I2, 1), lra=round(lra2, 1), true_peak_dbtp=round(tp2, 1))

def encode_ogg(wav_or_x, out, sr=44100, q=6):
    if isinstance(wav_or_x, np.ndarray):
        with tempfile.NamedTemporaryFile(suffix='.wav', delete=False) as f:
            tmp = f.name
        write_wav(tmp, wav_or_x, sr)
        src = tmp
    else:
        src, tmp = wav_or_x, None
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', src, '-map_metadata', '-1', '-c:a', 'libvorbis', '-q:a', str(q),
                    '-ar', str(sr), out], check=True)
    if tmp: os.unlink(tmp)

def seam_report(x, sr):
    """Compare the wrap-around step with ordinary steps: sample jump and short-time spectral change."""
    m = x.mean(1)
    d = np.abs(np.diff(m)); wrap = abs(m[0] - m[-1])
    win = 2048
    def spec(seg): return np.log(np.abs(np.fft.rfft(seg * np.hanning(win))) + 1e-6)
    pos = np.linspace(win, len(m) - 2 * win, 200).astype(int)
    flux = [np.mean((spec(m[p - win:p]) - spec(m[p:p + win])) ** 2) for p in pos]
    wrapflux = np.mean((spec(m[-win:]) - spec(m[:win])) ** 2)
    return dict(wrap_jump=float(wrap), median_step=float(np.median(d)), p99_step=float(np.percentile(d, 99)),
                wrap_flux=float(wrapflux), median_flux=float(np.median(flux)), p95_flux=float(np.percentile(flux, 95)))
