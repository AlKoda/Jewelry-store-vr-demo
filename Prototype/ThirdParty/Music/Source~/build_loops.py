"""Cut seamless loops from three CC0 John Bartmann tracks and export 44.1 kHz stereo OGG Vorbis (q6).

Sources (CC0 1.0, album "100 Ambient Atmospheric Soundtracks: Straylight Drones Collection", Free Music Archive),
fetched from the CC0 mirror https://github.com/SoundSafari/CC0-1.0-Music @ 655388f987eacf6d08d73080fcc3e727f267bfde:
  git clone --filter=blob:none --depth 1 --no-checkout https://github.com/SoundSafari/CC0-1.0-Music ss
  git -C ss show "HEAD:freemusicarchive.org/John Bartmann - profound-mystery-master.mp3" > "src/John Bartmann - profound-mystery-master.mp3"
  (same for a-wall-of-glass-master and background-scan-master; SHA-256 of each source is in ../sources.json)
Usage: python3 -I build_loops.py <source_dir> <out_dir>      (needs numpy and ffmpeg with libvorbis and soxr)
Run from a copy outside the Unity project; Unity ignores this Source~ folder.
"""
import sys, json, numpy as np
sys.path.insert(0, __file__.rsplit('/', 1)[0])
import audio_util as au

SR_IN, SR_OUT = 48000, 44100
GRAIN = 320  # 320 samples @48k == 294 samples @44.1k -> integer lengths after resampling

def best_lag(m, a, nominal, search=0.05, win_s=4.0):
    n = int(win_s * SR_IN); seg = m[a:a + n]
    lo = a + int((nominal - search) * SR_IN); hi = a + int((nominal + search) * SR_IN)
    best = (-2, None)
    ref = m[lo:hi + n]
    N = 1 << int(np.ceil(np.log2(len(ref) + n)))
    c = np.fft.irfft(np.fft.rfft(ref, N) * np.conj(np.fft.rfft(seg, N)))[:hi - lo]
    e = np.sqrt(np.convolve(ref ** 2, np.ones(n), 'valid')[:hi - lo] * np.sum(seg ** 2))
    r = c / (e + 1e-12); k = int(np.argmax(r))
    return lo + k - a, float(r[k])

def make_loop(x, A, nominal, xfade_s, rotate_to=None):
    m = x.mean(1); a = int(round(A * SR_IN))
    lag, corr = best_lag(m, a, nominal)
    L = int(round(lag / GRAIN)) * GRAIN
    y = x[a:a + L].copy()
    F = int(xfade_s * SR_IN)
    if F > 0:
        t = (np.arange(F) + 0.5) / F
        if corr > 0.8:   # correlated material: equal-gain raised cosine keeps level constant
            fin = 0.5 - 0.5 * np.cos(np.pi * t); fout = 1 - fin
        else:            # uncorrelated: equal power
            fin = np.sin(0.5 * np.pi * t); fout = np.cos(0.5 * np.pi * t)
        y[:F] = x[a:a + F] * fin[:, None] + x[a + L:a + L + F] * fout[:, None]
    info = dict(loop_start_s=round(A, 4), source_lag_s=round(lag / SR_IN, 6), loop_len_samples_48k=L,
                loop_len_s=round(L / SR_IN, 6), match_corr=round(corr, 4), crossfade_s=xfade_s,
                crossfade_shape=('raised-cosine equal-gain' if corr > 0.8 else 'equal-power') if F else 'none (sample-exact repeat)')
    if rotate_to is not None:
        r = int(round((rotate_to - A) * SR_IN)) % L
        y = np.roll(y, -r, axis=0)
        info['file_starts_at_source_s'] = round(A + r / SR_IN, 4)
    return y, info

def main(src, out):
    jobs = [
        # name, source file, loop start (s), nominal period (s), crossfade (s), rotate start to (s), target LUFS
        ('MenuTheme', 'John Bartmann - profound-mystery-master.mp3', 67.5, 119.999, 6.0, 119.90, -18.0),
        ('InvestigationLoop', 'John Bartmann - a-wall-of-glass-master.mp3', 30.0, 104.0, 0.0, None, -20.0),
        ('InvestigationLoop_Calm', 'John Bartmann - background-scan-master.mp3', 80.0, 128.0, 0.0, None, -20.0),
    ]
    report = {}
    for name, fn, A, P, xf, rot, lufs in jobs:
        x = au.decode(f'{src}/{fn}', SR_IN)
        y, info = make_loop(x, A, P, xf, rot)
        if rot is None:
            info['file_starts_at_source_s'] = A
        y44 = au.circular_resample(y, SR_IN, SR_OUT)
        y44, ln = au.normalize(y44, SR_OUT, lufs, tp_ceiling=-1.5)
        info.update(ln); info['seam'] = au.seam_report(y44, SR_OUT)
        info['samples_44k1'] = len(y44); info['seconds'] = round(len(y44) / SR_OUT, 4)
        au.write_wav(f'{out}/{name}.wav', y44, SR_OUT)
        au.encode_ogg(f'{out}/{name}.wav', f'{out}/{name}.ogg', SR_OUT, q=6)
        report[name] = info
        print(name, json.dumps(info))
    json.dump(report, open(f'{out}/loops_report.json', 'w'), indent=1)

if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
