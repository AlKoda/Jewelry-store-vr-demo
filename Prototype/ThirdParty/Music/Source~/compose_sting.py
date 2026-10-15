"""ResetSting: original 5.6 s cinematic sting (project-owned). Soft sub thud, felt-piano open-fifth chord on F,
detuned pad bloom with an opening/closing low-pass, a faint glass shimmer and a convolution reverb tail.
Usage: python3 -I compose_sting.py <out.wav>
"""
import sys, numpy as np
SR = 44100
DUR = 5.6
rng = np.random.default_rng(14102026)
N = int(DUR * SR); t = np.arange(N) / SR

def env_adsr(t0, att, tau, shape=1.0):
    e = np.zeros(N); tt = t - t0; m = tt >= 0
    a = np.clip(tt[m] / att, 0, 1) ** shape
    a = 0.5 - 0.5 * np.cos(np.pi * a)            # smooth attack
    e[m] = a * np.exp(-np.maximum(tt[m] - att, 0) / tau)
    return e

def butter(kind, f, order=2):
    """Butterworth magnitude response descriptor, applied zero-phase in the frequency domain (numpy only)."""
    return (kind, f, order)

def biquad(x, kind, f, order):
    n = x.shape[0]; M = 1 << int(np.ceil(np.log2(n + SR)))
    fr = np.fft.rfftfreq(M, 1 / SR) + 1e-9
    lp = lambda fc: 1 / np.sqrt(1 + (fr / fc) ** (2 * order))
    hp = lambda fc: 1 / np.sqrt(1 + (fc / fr) ** (2 * order))
    H = lp(f) if kind == 'low' else hp(f) if kind == 'high' else lp(f[1]) * hp(f[0])
    X = np.fft.rfft(x, M, axis=0)
    H = H[:, None] if x.ndim == 2 else H
    return np.fft.irfft(X * H, M, axis=0)[:n]

# ---------------- felt piano (additive, stretched partials, two detuned strings) ----------------
def piano(f0, t0, vel, decay=3.2, bright=4200):
    out = np.zeros(N); B = 0.00035
    for k in range(1, 18):
        fk = k * f0 * np.sqrt(1 + B * k * k)
        if fk > 9000: break
        amp = vel * k ** -1.15 * np.exp(-k * f0 / bright)
        tau = decay / (1 + 0.55 * (k - 1) * f0 / 260.0)
        e = env_adsr(t0, 0.006 + 0.002 * k, tau)
        for det in (-0.7, 0.7):                       # cents, string pair beating
            ph = rng.uniform(0, 2 * np.pi)
            out += 0.5 * amp * e * np.sin(2 * np.pi * fk * 2 ** (det / 1200) * t + ph)
    # felt hammer: short low-passed noise
    hn = rng.standard_normal(N) * env_adsr(t0, 0.002, 0.012) * vel * 0.05
    out += biquad(hn, *butter('low', min(2.5 * f0 + 400, 3000)))
    return out

# ---------------- pad: detuned band-limited saws with time-varying low-pass (applied per harmonic) -------------
def pad(f0, voices=6, spread=11.0, gain=1.0):
    fc = 300 + 2300 * np.exp(-((t - 0.9) / 0.8) ** 2) + 300 * np.exp(-t / 2.5)   # opens then closes
    out = np.zeros(N)
    for v in range(voices):
        det = (v / (voices - 1) - 0.5) * 2 * spread
        fv = f0 * 2 ** (det / 1200); ph0 = rng.uniform(0, 2 * np.pi)
        drift = 1 + 0.0009 * np.sin(2 * np.pi * rng.uniform(0.1, 0.25) * t + rng.uniform(0, 6.28))
        phase = 2 * np.pi * np.cumsum(fv * drift) / SR + ph0
        for k in range(1, 60):
            fk = k * fv
            if fk > 12000: break
            g = 1.0 / np.sqrt(1 + (fk / fc) ** 4)          # 2-pole style magnitude, per harmonic
            out += g * np.sin(k * phase) / k
    return out * gain / voices

# ---------------- build ----------------
L = np.zeros(N); R = np.zeros(N)
def add(sig, pan=0.0, g=1.0):
    global L, R
    L += sig * g * np.cos((pan + 1) * np.pi / 4); R += sig * g * np.sin((pan + 1) * np.pi / 4)

T0 = 0.06
F1, F2, C3, G3, C4, G4 = 43.65, 87.31, 130.81, 196.00, 261.63, 392.00

# sub thud: sine glide 64 -> F1 with soft saturation
fsub = F1 + (64 - F1) * np.exp(-np.maximum(t - T0, 0) / 0.09)
sub = np.sin(2 * np.pi * np.cumsum(fsub) / SR) * env_adsr(T0, 0.012, 0.6)
sub = np.tanh(1.6 * sub) / np.tanh(1.6)
add(sub, 0.0, 0.38)

# soft felt-piano chord, slightly strummed, open fifths on F
for f, dt, vel, pan in [(F2, 0.000, 0.70, -0.15), (C3, 0.018, 0.60, 0.10), (G3, 0.032, 0.52, -0.25),
                        (C4, 0.045, 0.48, 0.25), (G4, 0.060, 0.40, 0.0), (523.25, 0.075, 0.22, 0.35)]:
    add(piano(f, T0 + dt, vel), pan, 0.55)

# pad bloom (stereo: two independent renders)
padenv = env_adsr(T0, 0.35, 1.25, shape=1.0)
pl = (pad(F2, gain=0.8) + pad(C3, gain=0.8) + pad(G3, gain=0.7) + pad(C4, gain=0.55) + pad(G4, gain=0.3)) * padenv
pr = (pad(F2, gain=0.8) + pad(C3, gain=0.8) + pad(G3, gain=0.7) + pad(C4, gain=0.55) + pad(G4, gain=0.3)) * padenv
L += 0.22 * pl; R += 0.22 * pr

# glass shimmer: bell-like inharmonic partials on C6 with slow attack + tremolo
glass = np.zeros(N)
for ratio, a, tau in [(1.0, 1.0, 2.2), (2.756, 0.35, 1.2), (5.404, 0.12, 0.6), (8.933, 0.05, 0.35)]:
    glass += a * np.sin(2 * np.pi * 1046.5 * ratio * t + rng.uniform(0, 6.28)) * env_adsr(T0 + 0.12, 0.25, tau)
glass *= 1 + 0.25 * np.sin(2 * np.pi * 4.3 * t)
gl = glass * 0.10
L += gl * 0.8; R += np.roll(gl, int(0.011 * SR)) * 1.0

# air whoosh: band-passed noise swelling into the hit
noise = rng.standard_normal((N, 2))
air = biquad(noise, *butter('band', [1800, 7500], 2))
aenv = np.exp(-((t - T0 - 0.02) / 0.10) ** 2) * (t < T0 + 0.02) + env_adsr(T0 + 0.02, 0.001, 0.45) * (t >= T0 + 0.02)
air *= aenv[:, None] * 0.05
L += air[:, 0]; R += air[:, 1]

dry = np.stack([L, R], 1)

# ---------------- synthetic convolution reverb (stereo, frequency-dependent decay) ----------------
def make_ir(dur=3.6, rt_low=3.0, rt_high=1.4, predelay=0.022):
    n = int(dur * SR); tt = np.arange(n) / SR
    ir = np.zeros((n, 2))
    bands = [(None, 400, rt_low), (400, 1500, (rt_low + rt_high) / 2 + 0.3), (1500, 5000, rt_high + 0.3), (5000, None, rt_high * 0.7)]
    for lo, hi, rt in bands:
        nz = rng.standard_normal((n, 2))
        spec = butter('low', hi, 2) if lo is None else butter('high', lo, 2) if hi is None else butter('band', [lo, hi], 2)
        nz = biquad(nz, *spec)
        ir += nz * np.exp(-6.91 * tt / rt)[:, None]
    ir *= (1 - np.exp(-tt / 0.012))[:, None]          # soft onset of the late tail
    # early reflections
    for d, g in [(0.011, 0.5), (0.017, 0.38), (0.023, 0.3), (0.031, 0.26), (0.043, 0.2), (0.057, 0.15)]:
        ir[int(d * SR), 0] += g * rng.choice([-1, 1]); ir[int((d + 0.0037) * SR), 1] += g * rng.choice([-1, 1])
    pd = int(predelay * SR)
    ir = np.vstack([np.zeros((pd, 2)), ir])
    return ir / np.sqrt(np.sum(ir ** 2) / 2)

ir = make_ir()
def conv(x, h):
    n = len(x) + len(h) - 1; M = 1 << int(np.ceil(np.log2(n)))
    return np.fft.irfft(np.fft.rfft(x, M) * np.fft.rfft(h, M), M)[:len(x)]
# send: sub mostly dry; send everything else
send = dry.copy()
wet = np.stack([conv(send[:, 0], ir[:, 0]), conv(send[:, 1], ir[:, 1])], 1)
wet = biquad(wet, *butter('high', 120, 2))
mix = dry * 0.8 + wet * 0.38

# master: DC / rumble high-pass, gentle glue, fade the very end
mix = biquad(mix, *butter('high', 28, 2))
mix = np.tanh(1.2 * mix / np.max(np.abs(mix))) / np.tanh(1.2)
fade = np.ones(N); fl = int(0.9 * SR); fade[-fl:] = 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, fl))
mix *= fade[:, None]
mix *= 10 ** (-1.5 / 20) / np.max(np.abs(mix))
mix[:int(0.002 * SR)] *= np.linspace(0, 1, int(0.002 * SR))[:, None]

import subprocess
subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'f64le', '-ar', str(SR), '-ac', '2', '-i', '-', '-c:a', 'pcm_f32le', sys.argv[1]],
               input=np.ascontiguousarray(mix).tobytes(), check=True)
print('peak', np.max(np.abs(mix)), 'samples', N)
