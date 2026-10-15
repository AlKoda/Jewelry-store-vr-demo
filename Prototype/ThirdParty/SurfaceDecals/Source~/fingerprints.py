"""Synthetic fingerprints (no real person's print): orientation field from a zero-pole model,
ridges grown by iterative oriented Gabor filtering (SFinGe-style)."""
import sys, os
import numpy as np
from scipy import ndimage as ndi
from dutil import *


def orientation_field(n, kind, seed, rot=0.0):
    r = rng(seed)
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    z = (xx - n / 2) + 1j * (n / 2 - yy)
    s = n / 256.0
    if kind == 'loopR':
        cores = [complex(-8, 18) * s]; deltas = [complex(52, -48) * s]
    elif kind == 'loopL':
        cores = [complex(8, 18) * s]; deltas = [complex(-52, -48) * s]
    elif kind == 'whorl':
        cores = [complex(-6, 22) * s, complex(6, -2) * s]; deltas = [complex(-62, -50) * s, complex(60, -54) * s]
    else:
        cores = []; deltas = []
    th = np.zeros((n, n))
    for c in cores:
        th += 0.5 * np.angle(z - c)
    for d in deltas:
        th -= 0.5 * np.angle(z - d)
    if kind == 'arch':
        # plain arch: ridges flow across with a hump in the middle
        xn = (xx - n / 2) / (n * 0.25)
        yn = (n * 0.62 - yy) / (n * 0.5)
        th = np.arctan(1.4 * np.exp(-xn ** 2) * (-2 * xn) * np.clip(1.2 - yn * 0.6, 0.2, 1.5) * 0.5)
    th += rot + 0.06 * blur_noise(n, n, n / 6, seed + 3, wrap=False)
    return th


def gabor_ridges(theta, period, seed, iters=22, nbins=16):
    from scipy.signal import fftconvolve
    n = theta.shape[0]
    r = rng(seed)
    img = r.standard_normal(theta.shape)
    s_par, s_perp = period * 1.1, period * 0.5
    k = int(np.ceil(s_par * 2.5))
    yy, xx = np.mgrid[-k:k + 1, -k:k + 1].astype(float)
    kernels = []
    for b in range(nbins):
        a = np.pi * b / nbins
        # theta is measured with y up; image rows go down
        along = xx * np.cos(a) - yy * np.sin(a)
        perp = xx * np.sin(a) + yy * np.cos(a)
        g = np.exp(-along ** 2 / (2 * s_par ** 2) - perp ** 2 / (2 * s_perp ** 2)) * np.cos(2 * np.pi * perp / period)
        g -= g.mean()
        kernels.append(g / np.abs(g).sum())
    tb = (theta % np.pi) / np.pi * nbins
    b0 = np.floor(tb).astype(int) % nbins
    b1 = (b0 + 1) % nbins
    f = tb - np.floor(tb)
    for it in range(iters):
        pad = np.pad(img, k, mode='reflect')
        outs = np.stack([fftconvolve(pad, kern, mode='same')[k:-k, k:-k] for kern in kernels])
        sel = np.take_along_axis(outs, b0[None], 0)[0] * (1 - f) + np.take_along_axis(outs, b1[None], 0)[0] * f
        img = np.tanh(sel / (sel.std() + 1e-9) * 2.0)
    return img  # -1..1, ridges positive


def tip_mask(n, seed, w=0.30, h=0.40, cx=0.5, cy=0.5, partial=True):
    r = rng(seed)
    yy, xx = np.mgrid[0:n, 0:n].astype(float) / n
    a = r.uniform(-0.35, 0.35)
    x = (xx - cx) * np.cos(a) - (yy - cy) * np.sin(a)
    y = (xx - cx) * np.sin(a) + (yy - cy) * np.cos(a)
    # fingertip: rounded top, flatter bottom (crease)
    e = (x / w) ** 2 + (y / (h * np.where(y < 0, 1.0, 0.8))) ** 2
    base = smoothstep(1.0, 0.55, e + 0.12 * blur_noise(n, n, n / 14, seed + 1, wrap=False))
    if partial:
        g = np.cos(r.uniform(0, 2 * np.pi)) * x + np.sin(r.uniform(0, 2 * np.pi)) * y
        base *= smoothstep(-0.25, 0.15, g * r.uniform(1.0, 2.5) + 0.25)
    pres = 0.55 + 0.45 * smoothstep(1.0, 0.0, e) + 0.25 * blur_noise(n, n, n / 10, seed + 2, wrap=False)
    return np.clip(base * pres, 0, 1)


def print_cell(n, kind, seed, developed):
    th = orientation_field(n, kind, seed, rot=rng(seed).uniform(-0.3, 0.3))
    rid = gabor_ridges(th, period=5.4 * n / 256, seed=seed)
    m = tip_mask(n, seed + 10)
    r = rng(seed + 20)
    pores = (r.random((n, n)) > 0.985).astype(float)
    pores = ndi.gaussian_filter(pores, 0.6) * 4
    ridge = np.clip(smoothstep(-0.1, 0.6, rid) - 0.6 * pores, 0, 1)
    # breaks and smudge
    brk = smoothstep(-1.7, -0.5, blur_noise(n, n, 3, seed + 21, wrap=False))
    smear = smoothstep(0.6, 1.6, blur_noise(n, n, 12, seed + 22, wrap=False)) * 0.6
    if developed:
        grain = r.random((n, n))
        a = m * (ridge * brk * (0.55 + 0.45 * (grain > 0.25)) + smear * 0.5)
        # powder haze from brushing: soft swirl around the print
        yy, xx = np.mgrid[0:n, 0:n].astype(float) / n - 0.5
        rad = np.sqrt(xx ** 2 + yy ** 2)
        ang = np.arctan2(yy, xx)
        swirl = 0.5 + 0.5 * np.sin(ang * 3 + rad * 40 + blur_noise(n, n, 6, seed + 23, wrap=False))
        haze = smoothstep(0.48, 0.15, rad) * (0.06 + 0.06 * swirl) * smoothstep(-1, 1, blur_noise(n, n, 10, seed + 24, wrap=False))
        specks = ndi.gaussian_filter((r.random((n, n)) > 0.996).astype(float), 0.5) * 3 * smoothstep(0.5, 0.2, rad)
        a = np.clip(np.maximum(a * 0.92, haze) + specks * 0.6, 0, 1)
        col = np.array([0.06, 0.06, 0.065])
    else:
        a = m * (ridge * brk * 0.30 + smear * 0.12)
        a = np.clip(a, 0, 0.36)
        col = np.array([0.86, 0.85, 0.82])
    rgb = np.broadcast_to(col, (n, n, 3)).copy()
    return rgb, a


def atlas(out):
    n = 256
    kinds = ['loopR', 'whorl', 'loopL', 'arch']
    A = np.zeros((2 * n, 4 * n)); C = np.zeros((2 * n, 4 * n, 3))
    for row, dev in enumerate([False, True]):
        for i, k in enumerate(kinds):
            # same seed for both rows: the developed cell is the same print (same ridges, mask,
            # position); only the powder grain, brush haze and specks differ
            rgb, a = print_cell(n, k, 100 + i * 7, dev)
            A[row * n:(row + 1) * n, i * n:(i + 1) * n] = a
            C[row * n:(row + 1) * n, i * n:(i + 1) * n] = rgb
    # each row has its own constant colour so we save rows with their own fill
    save_rgba(os.path.join(out, 'Fingerprints_Atlas.png'), C, A, fill=(0.5, 0.5, 0.5))


def _ellipse(xx, yy, cx, cy, ax, ay, ang):
    c, s = np.cos(ang), np.sin(ang)
    x = (xx - cx) * c + (yy - cy) * s
    y = -(xx - cx) * s + (yy - cy) * c
    return (x / ax) ** 2 + (y / ay) ** 2


def handprint(out, seed=300):
    """Left hand pressed flat on glass (palm side), seen from the side the hand was on.
    1024 px = 0.19 m (5.4 px/mm). Coordinates below in mm, origin top-left."""
    n = 1024
    ppm = n / 190.0
    r = rng(seed)
    yy, xx = np.mgrid[0:n, 0:n].astype(float) / ppm
    # fingers: base (x, y) at the palm, direction angle (0 = straight up), segment lengths
    fingers = [  # little, ring, middle, index
        (52, 112, -0.30, [20, 22, 24], 15.5),
        (75, 103, -0.10, [24, 27, 25], 17.0),
        (98, 100, 0.04, [26, 29, 26], 17.5),
        (121, 106, 0.20, [24, 26, 24], 17.0),
    ]
    pads = []      # (cx, cy, ax, ay, ang, weight, is_tip)
    cvec = np.zeros((n, n)); svec = np.zeros((n, n)); wsum = np.zeros((n, n)) + 1e-6
    def add_orient(th, wgt):
        nonlocal cvec, svec, wsum
        cvec += np.cos(2 * th) * wgt; svec += np.sin(2 * th) * wgt; wsum += wgt
    for i, (bx, by, ang, segs, wid) in enumerate(fingers):
        dx, dy = np.sin(ang), -np.cos(ang)
        d = 4.0
        for j, L in enumerate(reversed(segs)):  # proximal, middle, distal
            cx, cy = bx + dx * (d + L / 2), by + dy * (d + L / 2)
            is_tip = (j == 2)
            ax_, ay_ = wid / 2, L / 2 * (0.95 if is_tip else 0.78)
            pads.append((cx, cy, ax_, ay_, ang, 1.0 if is_tip else 0.75, is_tip))
            e = _ellipse(xx, yy, cx, cy, ax_ * 1.3, ay_ * 1.3, ang)
            wgt = np.exp(-e * 1.5)
            if is_tip:
                # loop / whorl around the tip centre
                z = (xx - cx) + 1j * (cy - yy)
                core = complex(r.normal(0, 1.5), -2)
                delt = complex(r.choice([-1, 1]) * wid * 0.55, -L * 0.45)
                th = 0.5 * np.angle(z - core) - 0.5 * np.angle(z - delt) - ang
            else:
                th = np.full((n, n), np.pi / 2 - ang) + 0.15 * np.sin((xx - cx) * 0.2)
            add_orient(th, wgt)
            d += L + 2.5
    # palm: interdigital pad band, thenar (thumb side = right for a left palm print seen from outside),
    # hypothenar (little-finger side), and the thumb tip off to the side
    palm = [(64, 118, 15, 9, -0.25, 0.8), (87, 112, 13, 9, -0.05, 0.85), (110, 113, 14, 9, 0.12, 0.85),
            (132, 120, 12, 9, 0.3, 0.7), (128, 150, 22, 28, 0.45, 0.85), (66, 152, 18, 30, -0.15, 0.75),
            (97, 172, 26, 14, 0.0, 0.6), (165, 128, 12, 16, 0.75, 0.9)]
    for cx, cy, ax_, ay_, ang, wgt0 in palm:
        pads.append((cx, cy, ax_, ay_, ang, wgt0, cx > 160))
        e = _ellipse(xx, yy, cx, cy, ax_ * 1.3, ay_ * 1.3, ang)
        th = np.full((n, n), 0.35 + 0.6 * np.sin(yy * 0.02 + xx * 0.015))
        if cx > 160:
            z = (xx - cx) + 1j * (cy - yy)
            th = 0.5 * np.angle(z - complex(0, -1)) - 0.5 * np.angle(z - complex(-8, -10)) - ang
        add_orient(th, np.exp(-e * 1.5) * 0.8)
    add_orient(np.full((n, n), 0.4), 0.02)
    theta = 0.5 * np.arctan2(svec / wsum, cvec / wsum)
    theta += 0.05 * blur_noise(n, n, 40, seed + 5)
    rid = gabor_ridges(theta, period=2.9, seed=seed, iters=16)
    ridge = smoothstep(-0.15, 0.65, rid)
    # contact mask from pads, eroded with noise; pressure highest in each pad centre
    contact = np.zeros((n, n))
    nz = blur_noise(n, n, 6, seed + 7)
    for k, (cx, cy, ax_, ay_, ang, wgt0, is_tip) in enumerate(pads):
        e = _ellipse(xx, yy, cx, cy, ax_, ay_, ang)
        pres = wgt0 * (0.6 + 0.4 * smoothstep(1.0, 0.0, e)) * r.uniform(0.75, 1.0)
        cm = smoothstep(1.05, 0.75, e + 0.12 * nz)
        contact = np.maximum(contact, cm * pres)
    # flexion creases (no contact) across the palm
    for (a0, b0, c0) in [(0.09, 128, 0.0), (-0.12, 138, 1.0), (0.5, 160, 2.0)]:
        line = np.abs(yy - (b0 + a0 * (xx - 95) + 3 * np.sin(xx * 0.05 + c0)))
        contact *= 1 - 0.85 * smoothstep(1.4, 0.4, line)
    grain = smoothstep(-1.4, -0.2, blur_noise(n, n, 2.0, seed + 8))
    a = contact * ridge * grain * 0.40
    # slight smear where the hand slid when lifted (downward, few mm)
    sm = ndi.uniform_filter1d(contact * 0.5, size=int(7 * ppm), axis=0, origin=-int(3 * ppm))
    a = np.maximum(a, sm * 0.10 * smoothstep(0.2, 1.2, blur_noise(n, n, 25, seed + 9)))
    a = np.clip(a, 0, 1)
    rgb = np.broadcast_to(np.array([0.86, 0.85, 0.82]), (n, n, 3)).copy()
    save_rgba(os.path.join(out, 'GlassHandprint_Latent.png'), rgb, a)


if __name__ == '__main__':
    out = sys.argv[1]
    which = sys.argv[2:] or ['atlas', 'hand']
    if 'atlas' in which: atlas(out)
    if 'hand' in which: handprint(out)
