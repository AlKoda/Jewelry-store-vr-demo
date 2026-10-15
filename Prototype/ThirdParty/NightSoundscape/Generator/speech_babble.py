"""Unintelligible speech-like babble for the synthetic police radio (original, project-owned).

A small Klatt-style source-filter synthesizer: glottal-pulse source (KLGLOTT88 flow derivative with jitter,
shimmer and a variable return phase), aspiration and frication noise, a cascade of five time-varying formant
resonators with a nasal pole/zero pair, and a parallel branch for fricatives and stop bursts. Random but
phonotactically plausible syllables (onset clusters, vowels/diphthongs, codas; no word list, no meaning) are
timed at 4.5-6.5 syllables/s with lexical stress, phrase declination, pitch accents, phrase-final lengthening and
pauses. Nothing is recorded; no real voice is imitated."""
import numpy as np
from scipy.signal import lfilter

FSS = 32000          # synthesis sample rate
NF = 32              # samples per 1 ms parameter frame

# vowel formants F1, F2, F3 (Hz), adult male averages (Hillenbrand et al. 1995, rounded)
VOW = {"i": (342, 2322, 3000), "I": (427, 2034, 2684), "e": (476, 2089, 2691), "E": (580, 1799, 2605),
       "ae": (588, 1952, 2601), "a": (768, 1333, 2522), "O": (652, 997, 2538), "o": (497, 910, 2459),
       "U": (469, 1122, 2434), "u": (378, 997, 2343), "V": (623, 1200, 2550), "@": (500, 1450, 2500),
       "3": (474, 1379, 1710)}
DIPH = {"ai": ("a", "I"), "ei": ("e", "i"), "au": ("a", "U"), "oi": ("O", "I"), "ou": ("o", "u")}
VW = {"i": 6, "I": 9, "e": 5, "E": 8, "ae": 6, "a": 8, "O": 4, "o": 4, "U": 2, "u": 4, "V": 7, "@": 12, "3": 3,
      "ai": 3, "ei": 3, "au": 1.5, "oi": 0.5, "ou": 2.5}

# consonants: manner, voiced, F2 locus, F3 locus, noise centre, noise bandwidth, noise level (dB re vowel rms)
CONS = {
    "p": ("stop", 0, 900, 2200, 900, 1800, -14), "t": ("stop", 0, 1800, 2700, 4200, 3000, -6),
    "k": ("stop", 0, None, None, None, 900, -6), "b": ("stop", 1, 900, 2200, 900, 1800, -18),
    "d": ("stop", 1, 1800, 2700, 3800, 3000, -12), "g": ("stop", 1, None, None, None, 900, -12),
    "s": ("fric", 0, 1750, 2700, 5600, 2600, -11), "S": ("fric", 0, 1900, 2450, 2900, 1600, -9),
    "f": ("fric", 0, 1000, 2300, 5000, 7000, -24), "T": ("fric", 0, 1500, 2600, 5200, 6000, -26),
    "z": ("fric", 1, 1750, 2700, 5600, 2600, -16), "v": ("fric", 1, 1000, 2300, 4000, 6000, -26),
    "h": ("h", 0, None, None, None, None, -14),
    "m": ("nasal", 1, 1000, 2250, 1000, None, None), "n": ("nasal", 1, 1650, 2600, 1700, None, None),
    "N": ("nasal", 1, None, None, 2600, None, None),
    "l": ("approx", 1, 1050, 2700, 360, None, None), "r": ("approx", 1, 1150, 1600, 350, None, None),
    "w": ("approx", 1, 750, 2200, 300, None, None), "j": ("approx", 1, 2200, 3000, 280, None, None),
}
ONS = {"t": 9, "k": 7, "s": 7, "n": 6, "m": 5, "d": 6, "l": 5, "r": 5, "p": 5, "b": 4, "g": 3, "w": 4, "h": 4,
       "f": 4, "v": 2, "S": 3, "j": 1.5, "z": 1, "T": 1,
       "st": 2, "sk": 1, "sp": 1, "tr": 1.5, "kr": 1, "pr": 1, "bl": 0.8, "kl": 0.8, "gr": 0.8, "dr": 0.8,
       "fl": 0.6, "sl": 0.5, "kw": 0.6}
CODA = {"n": 8, "t": 7, "s": 5, "k": 4, "d": 4, "m": 3, "l": 3, "r": 3, "N": 2, "z": 2, "p": 1.5, "v": 1,
        "f": 1, "st": 1.5, "nt": 1.5, "nd": 1.2, "ks": 1, "ts": 1, "ld": 0.6, "rt": 0.6}


def _pick(r, table):
    keys = list(table)
    w = np.array([table[k] for k in keys], float)
    return keys[r.choice(len(keys), p=w / w.sum())]


def _split(cl):
    out, i = [], 0
    while i < len(cl):
        out.append(cl[i])
        i += 1
    return out


def plan_utterance(r, n_phrases, syl_per_phrase, rate=1.0, final_rise=False):
    """Returns a list of phones: dict(kind, name, dur_ms, stress, phrase, final, syl)."""
    phones = []
    syl_id = 0
    for ph in range(n_phrases):
        nsyl = int(syl_per_phrase[ph])
        # words of 1-3 syllables, one stressed syllable each
        words = []
        left = nsyl
        while left > 0:
            k = min(left, int(r.choice([1, 1, 1, 2, 2, 3])))
            words.append(k)
            left -= k
        sidx = 0
        for wi, wl in enumerate(words):
            stress_pos = 0 if (wl == 1 or r.random() < 0.7) else 1
            for si in range(wl):
                stressed = (si == stress_pos) and (wl > 1 or r.random() < 0.65)
                final = (sidx == nsyl - 1)
                # onset
                u = r.random()
                onset = None if u < 0.12 else _pick(r, ONS)
                if onset and len(onset) > 1 and not stressed and r.random() < 0.5:
                    onset = onset[-1]
                nuc = _pick(r, VW)
                if not stressed and nuc not in ("@", "I", "i", "U") and r.random() < 0.45:
                    nuc = "@" if r.random() < 0.7 else "I"   # vowel reduction
                coda = None
                if r.random() < (0.5 if (stressed or final) else 0.3):
                    coda = _pick(r, CODA)
                    if len(coda) > 1 and not stressed:
                        coda = coda[0]
                syl = []
                if onset:
                    for c in _split(onset):
                        syl.append(dict(kind="C", name=c, pos="onset", cluster=len(onset) > 1))
                syl.append(dict(kind="V", name=nuc))
                if coda:
                    for c in _split(coda):
                        syl.append(dict(kind="C", name=c, pos="coda", cluster=len(coda) > 1))
                for p in syl:
                    p.update(stress=stressed, phrase=ph, final=final, syl=syl_id)
                phones.extend(syl)
                syl_id += 1
                sidx += 1
            # occasional micro-pause between words
            if wi < len(words) - 1 and r.random() < 0.06:
                phones.append(dict(kind="P", name="_", dur=r.uniform(30, 90), phrase=ph, stress=False, final=False, syl=-1))
        if ph < n_phrases - 1:
            phones.append(dict(kind="P", name="_", dur=r.uniform(110, 240), phrase=ph, stress=False, final=False,
                               syl=-1, breath=True))
    # durations (ms)
    for i, p in enumerate(phones):
        if p["kind"] == "P":
            continue
        st, fin = p["stress"], p["final"]
        if p["kind"] == "V":
            d = r.uniform(95, 140) if st else r.uniform(48, 75)
            if p["name"] in DIPH:
                d += 35
            if p["name"] in ("@", "I") and not st:
                d *= 0.85
        else:
            man = CONS[p["name"]][0]
            if man == "stop":
                d = r.uniform(55, 80) if not CONS[p["name"]][1] else r.uniform(45, 65)
            elif man == "fric":
                d = r.uniform(80, 115) if p["name"] in ("s", "S", "z") else r.uniform(60, 85)
            elif man == "nasal":
                d = r.uniform(50, 70) if p["pos"] == "onset" else r.uniform(55, 80)
            elif man == "approx":
                d = r.uniform(40, 60)
            else:
                d = r.uniform(50, 70)
            if p.get("cluster"):
                d *= 0.7
        if fin:
            d *= 1.45 if p["kind"] == "V" or p.get("pos") == "coda" else 1.1
        p["dur"] = d * rate
    return phones


def _formants_for(name, k):
    if name in DIPH:
        a, b = DIPH[name]
        return np.array(VOW[a], float) * k, np.array(VOW[b], float) * k
    v = np.array(VOW[name], float) * k
    return v, v


def _onset_formants(c, vowel, k):
    """Formant values at a consonant release into `vowel` (locus equations; velar pinch)."""
    man, vo, L2, L3 = CONS[c][:4]
    F1 = 250 * k if man in ("stop", "fric", "nasal") else CONS[c][4] * k if man == "approx" else vowel[0]
    if man == "h":
        return vowel.copy()
    if L2 is None:  # velar: F2 and F3 come together near the vowel's F2 (front) or above it (back)
        F2 = min(vowel[1] * 1.08 + 150 * k, 2350 * k)
        F3 = F2 + 350 * k
    else:
        F2 = L2 * k + 0.45 * (vowel[1] - L2 * k)
        F3 = L3 * k + 0.5 * (vowel[2] - L3 * k)
        if man == "approx":
            F2, F3 = L2 * k, L3 * k
    return np.array([F1, F2, F3])


def render_tracks(r, phones, k, f0_base, fem):
    """Frame (1 ms) parameter tracks from the phone plan."""
    total = int(sum(p["dur"] for p in phones)) + 300
    n = total
    F = np.zeros((n, 3))
    AV = np.zeros(n)
    AH = np.zeros(n)        # aspiration (through cascade), linear re vowel level
    FZ = np.full(n, 270.0)  # nasal zero (= pole -> no effect)
    FP = np.full(n, 270.0)
    B1 = np.full(n, 70.0)
    noise_ev = []           # (start_frame, n_frames, centre, bw, level_db, env_shape)
    vowel_centres = []      # (frame, stressed, phrase, final)
    t = 20
    # find next vowel for each phone
    vowel_after = [None] * len(phones)
    nxt = None
    for i in range(len(phones) - 1, -1, -1):
        if phones[i]["kind"] == "V":
            nxt = phones[i]["name"]
        if phones[i]["kind"] == "P":
            nxt = None
        vowel_after[i] = nxt
    prev_v = None
    cur = np.array(VOW["@"], float) * k
    for i, p in enumerate(phones):
        d = max(4, int(round(p["dur"])))
        s, e = t, min(n, t + d)
        m = e - s
        lv = 1.0 if p.get("stress") else 0.8
        if p["kind"] == "P":
            F[s:e] = cur
            t = e
            continue
        if p["kind"] == "V":
            a, b = _formants_for(p["name"], k)
            # look ahead: transition toward the following consonant's locus in the last ~35 ms
            j = i + 1
            nc = phones[j]["name"] if j < len(phones) and phones[j]["kind"] == "C" else None
            end_t = b.copy()
            if nc is not None and CONS[nc][0] != "h":
                end_t = _onset_formants(nc, b, k)
                end_t[0] = 0.5 * (end_t[0] + b[0])
            tr_in = min(int(0.4 * m), 40)
            tr_out = min(int(0.3 * m), 35) if nc else 0
            for q in range(m):
                if q < tr_in:
                    w = q / max(1, tr_in)
                    w = w * w * (3 - 2 * w)
                    tgt = a
                    val = cur * (1 - w) + tgt * w
                else:
                    w2 = (q - tr_in) / max(1, m - tr_in)
                    val = a * (1 - w2) + b * w2 if p["name"] in DIPH else a
                if tr_out and q >= m - tr_out:
                    w3 = (q - (m - tr_out)) / tr_out
                    val = val * (1 - 0.6 * w3) + end_t * 0.6 * w3
                F[s + q] = val
            env = np.ones(m) * lv
            if p.get("final"):
                env *= np.linspace(1.0, 0.6, m)
            AV[s:e] = np.maximum(AV[s:e], env)
            AH[s:e] += 0.035
            B1[s:e] = 60 + 30 * (not p.get("stress"))
            vowel_centres.append((s + m // 2, p.get("stress"), p["phrase"], p.get("final"), s, e))
            cur = F[e - 1].copy()
            prev_v = p["name"]
            t = e
            continue
        # consonant
        c = p["name"]
        man, vo, L2, L3, nc_f, nbw, nlev = CONS[c]
        nv = vowel_after[i] if p.get("pos") == "onset" else None
        vt = _formants_for(nv, k)[0] if nv else cur
        tgt = _onset_formants(c, vt, k) if nv else _onset_formants(c, cur, k)
        F[s:e] = cur * 0.3 + tgt * 0.7
        if man == "stop":
            burst = 10 if not vo else 6
            asp = 0
            if not vo and p.get("pos") == "onset" and not p.get("cluster"):
                asp = int(r.uniform(35, 60) if p.get("stress") else r.uniform(18, 30))
            elif not vo and p.get("pos") == "onset":
                asp = 12
            cl = max(15, m - burst - asp) if p.get("pos") == "onset" else m
            if vo:
                AV[s:s + cl] = 0.07          # voice bar
                F[s:s + cl, 0] = 180 * k
            centre = nc_f if nc_f else min(tgt[1] * 1.05, 2600 * k)
            released = p.get("pos") == "onset" or r.random() < 0.55
            if released:
                bs = s + cl
                noise_ev.append((bs, burst, centre, nbw if nbw else 900, nlev, "burst"))
                if asp:
                    AH[bs + burst:bs + burst + asp] += np.linspace(0.45, 0.12, len(AH[bs + burst:bs + burst + asp]))
                    F[bs + burst:bs + burst + asp, 0] = np.linspace(400 * k, vt[0], len(F[bs + burst:bs + burst + asp]))
                    F[bs + burst:bs + burst + asp, 1:] = np.linspace(tgt[1:], vt[1:], len(F[bs + burst:bs + burst + asp]))
            cur = tgt.copy()
        elif man == "fric":
            noise_ev.append((s, m, nc_f, nbw, nlev, "fric"))
            if vo:
                AV[s:e] = np.maximum(AV[s:e], 0.25)
                F[s:e, 0] = 220 * k
            AH[s:e] += 0.02
            cur = tgt.copy()
        elif man == "h":
            AH[s:e] += np.concatenate([np.linspace(0.1, 0.55, m // 2), np.linspace(0.55, 0.4, m - m // 2)])
            F[s:e] = vt
            cur = vt.copy()
        elif man == "nasal":
            AV[s:e] = np.maximum(AV[s:e], 0.42 * lv)
            F[s:e, 0] = 260 * k
            F[s:e, 1] = (L2 if L2 else 1900) * k
            F[s:e, 2] = 2400 * k
            FZ[s:e] = nc_f * k
            B1[s:e] = 110
            # nasalised edges of neighbouring vowels
            FZ[max(0, s - 30):s] = np.minimum(FZ[max(0, s - 30):s], 450)
            FZ[e:e + 30] = np.minimum(FZ[e:e + 30], 450)
            cur = tgt.copy()
            cur[0] = 300 * k
        elif man == "approx":
            AV[s:e] = np.maximum(AV[s:e], 0.62 * lv)
            F[s:e, 0] = nc_f * k
            F[s:e, 1] = L2 * k
            F[s:e, 2] = L3 * k
            cur = F[e - 1].copy()
        t = e
    n = min(len(F), t + 40)
    F[t:] = cur
    F, AV, AH, FZ, FP, B1 = F[:n], AV[:n], AH[:n], FZ[:n], FP[:n], B1[:n]
    # smooth tracks (coarticulation / articulator inertia)
    def sm(x, ms):
        w = np.hanning(ms * 2 + 1)
        w /= w.sum()
        pad = np.pad(x, ((ms, ms),) + ((0, 0),) * (x.ndim - 1), mode="edge")
        if x.ndim == 1:
            return np.convolve(pad, w, "valid")
        return np.stack([np.convolve(pad[:, j], w, "valid") for j in range(x.shape[1])], axis=1)
    F = sm(F, 12)
    AV = sm(AV, 4)
    AH = sm(AH, 4)
    FZ = sm(FZ, 8)
    B1 = sm(B1, 8)
    # F0 contour: phrase declination, accents on stressed vowels, final fall, micro-variation
    f0 = np.zeros(n)
    phr_start = {}
    phr_end = {}
    for (fr, st, ph, fin, s, e) in vowel_centres:
        phr_start.setdefault(ph, s)
        phr_end[ph] = e
    nph = len(phr_start)
    base = np.full(n, f0_base)
    for ph in phr_start:
        s0, e0 = phr_start[ph], phr_end[ph]
        L = max(1, e0 - s0)
        tt = np.clip((np.arange(n) - s0) / L, 0, 1)
        reset = 1.10 - 0.04 * ph
        seg = reset - 0.22 * tt
        lo = s0 - 40 if ph else 0
        hi = (phr_start.get(ph + 1, n))
        base[lo:hi] = f0_base * seg[lo:hi]
    acc = np.zeros(n)
    for (fr, st, ph, fin, s, e) in vowel_centres:
        if st:
            hgt = r.uniform(0.10, 0.24)
            wdt = (e - s) * 0.7 + 25
            acc += hgt * np.exp(-0.5 * ((np.arange(n) - (s + 0.35 * (e - s))) / wdt) ** 2)
        if fin:
            last = ph == nph - 1
            drop = -0.18 if (last or r.random() < 0.6) else +0.12  # final fall or continuation rise
            ramp = np.clip((np.arange(n) - s) / max(1, e - s), 0, 1)
            acc += drop * ramp * (np.arange(n) < e + 60)
    f0 = base * (1 + acc) * (1 + 0.025 * _smooth_noise(r, n, 4.0))
    f0 = np.clip(f0, 0.55 * f0_base, 1.7 * f0_base)
    return dict(F=F, AV=AV, AH=AH, FZ=FZ, FP=FP, B1=B1, f0=f0, noise=noise_ev, n=n)


def _smooth_noise(r, n, hz, fps=1000.0):
    w = r.standard_normal(n)
    W = np.fft.rfft(w)
    f = np.fft.rfftfreq(n, 1 / fps)
    W *= np.exp(-(f / hz) ** 2)
    y = np.fft.irfft(W, n)
    return y / (np.std(y) + 1e-12)


def glottal(r, f0_fr, av_fr, oq=0.62, jitter=0.012, shimmer=0.06, ta_ms=0.25):
    """KLGLOTT88-style flow-derivative pulse train, sample-accurate period boundaries."""
    n = len(f0_fr) * NF
    idx = np.arange(n) / NF
    f0 = np.interp(idx, np.arange(len(f0_fr)), f0_fr)
    av = np.interp(idx, np.arange(len(av_fr)), av_fr)
    out = np.zeros(n + 64)
    t = 0.0
    ta = ta_ms * 1e-3 * FSS
    while t < n - 1:
        i = int(t)
        T0 = FSS / f0[i] * (1 + jitter * r.standard_normal())
        amp = av[i] * (1 + shimmer * r.standard_normal())
        if amp > 1e-3:
            Te = oq * T0
            k0 = int(np.ceil(t))
            k1 = min(n, int(np.floor(t + Te)) + 1)
            tau = (np.arange(k0, k1) - t) / Te
            out[k0:k1] += amp * (2 * tau - 3 * tau ** 2) * 1.5
            # exponential return phase after the closure instant (softens the excitation: spectral tilt)
            kk = np.arange(k1, min(n, k1 + int(6 * ta) + 2))
            out[kk] += -1.5 * amp * np.exp(-(kk - (t + Te)) / ta)
        t += T0
    return out[:n]


def _res_coefs(f, bw):
    C = -np.exp(-2 * np.pi * bw / FSS)
    B = 2 * np.exp(-np.pi * bw / FSS) * np.cos(2 * np.pi * f / FSS)
    A = 1 - B - C
    return A, B, C


def tv_resonator(x, f_fr, bw_fr, anti=False):
    """Time-varying Klatt resonator (or anti-resonator), coefficients per 1 ms block, state carried."""
    y = np.zeros_like(x)
    zi = np.zeros(2)
    nb = len(x) // NF
    for b in range(nb):
        A, B, C = _res_coefs(f_fr[min(b, len(f_fr) - 1)], bw_fr[min(b, len(bw_fr) - 1)] if np.ndim(bw_fr) else bw_fr)
        seg = x[b * NF:(b + 1) * NF]
        if anti:
            bb, aa = np.array([1.0, -B, -C]) / A, np.array([1.0, 0.0, 0.0])
        else:
            bb, aa = np.array([A, 0.0, 0.0]), np.array([1.0, -B, -C])
        y[b * NF:(b + 1) * NF], zi = lfilter(bb, aa, seg, zi=zi)
    return y


def cascade(x, tr, k):
    n = len(x)
    nfr = tr["n"]
    F = tr["F"]
    y = tv_resonator(x, tr["FP"], np.full(nfr, 100.0))           # nasal pole
    y = tv_resonator(y, tr["FZ"], np.full(nfr, 100.0), anti=True)  # nasal zero
    y = tv_resonator(y, np.full(nfr, 4500 * k), np.full(nfr, 350.0))
    y = tv_resonator(y, np.full(nfr, 3500 * k), np.full(nfr, 250.0))
    y = tv_resonator(y, F[:, 2], np.full(nfr, 150.0))
    y = tv_resonator(y, F[:, 1], np.full(nfr, 95.0))
    y = tv_resonator(y, F[:, 0], tr["B1"])
    return y


def bpf(x, fc, bw):
    w0 = 2 * np.pi * fc / FSS
    q = fc / bw
    al = np.sin(w0) / (2 * q)
    b = np.array([al, 0, -al])
    a = np.array([1 + al, -2 * np.cos(w0), 1 - al])
    return lfilter(b / a[0], a / a[0], x)


def synth_utterance(r, dur_s, female=False, rate=None):
    """Returns float array at FSS. dur_s is a target; the result is close to it."""
    k = r.uniform(1.15, 1.21) if female else r.uniform(0.96, 1.04)
    f0b = r.uniform(185, 225) if female else r.uniform(98, 128)
    rate = rate or r.uniform(0.82, 0.95)     # radio talk is quick and clipped
    syl_rate = 5.3 / rate
    nsyl = max(4, int(round(dur_s * syl_rate * 0.9)))
    nph = 1 if dur_s < 2.6 else (2 if dur_s < 4.3 else 3)
    parts = r.dirichlet(np.ones(nph) * 4) * nsyl
    parts = np.maximum(3, np.round(parts)).astype(int)
    phones = plan_utterance(r, nph, parts, rate)
    tr = render_tracks(r, phones, k, f0b, female)
    nfr = tr["n"]
    n = nfr * NF
    src = glottal(r, tr["f0"], tr["AV"], oq=0.58 if female else 0.62, ta_ms=0.12 if female else 0.15)
    asp = np.diff(r.standard_normal(n + 1))
    ahs = np.interp(np.arange(n) / NF, np.arange(nfr), tr["AH"])
    v = cascade(src, tr, k)
    a = cascade(asp * ahs, tr, k)
    vowel_mask = np.repeat(tr["AV"] > 0.6, NF)[:n]
    vr = np.sqrt(np.mean(v[vowel_mask] ** 2)) if vowel_mask.any() else np.std(v)
    # calibrate aspiration so AH = 1 equals the vowel rms
    am = np.repeat(tr["AH"] > 0.3, NF)[:n]
    ar = np.sqrt(np.mean((a / np.maximum(ahs, 1e-3))[am] ** 2)) if am.any() else 1.0
    y = v / vr + a / (ar + 1e-12) * 0.5
    # parallel noise branch: fricatives and bursts
    for (s, m, fc, bw, lev, kind) in tr["noise"]:
        ln = m * NF
        nz = r.standard_normal(ln + 400)
        if fc - bw / 2 > 300:
            nz = bpf(nz, fc, bw)
        else:
            nz = bpf(nz, max(fc, 900), max(bw, 1500))
        nz = nz[400:]
        nz /= np.std(nz) + 1e-12
        if kind == "burst":
            env = np.exp(-np.arange(ln) / (0.35 * ln)) * np.minimum(1, np.arange(ln) / 16)
            g = 10 ** ((lev + 9) / 20)
        else:
            tt = np.linspace(0, 1, ln)
            env = np.minimum(1, np.minimum(tt / 0.25, (1 - tt) / 0.25)) ** 1.5
            g = 10 ** (lev / 20)
        i0 = s * NF
        j0 = min(n, i0 + ln)
        y[i0:j0] += (nz * env * g)[:j0 - i0]
    return y


def to44k(y):
    from scipy.signal import resample_poly
    return resample_poly(y, 441, 320)
