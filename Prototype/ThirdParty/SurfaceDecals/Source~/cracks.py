"""Concrete cracks (pavement, kerb, floor screed) and a bitumen crack-seal band for asphalt."""
import sys, os
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
from dutil import *


BOUNDS = None


def crack_paths(r, start, ang, length_mm, width_mm, ppm, depth=0, max_depth=3, branch_p=0.012):
    """Returns list of (points[N,2] px, widths[N] px). Large-scale direction stays persistent;
    small-scale jitter does not accumulate, which is how cracks in concrete look."""
    paths = []
    step = 1.2  # mm
    nsteps = max(4, int(length_mm / step))
    lf = ndi.gaussian_filter1d(r.standard_normal(nsteps + 50), 18)[25:25 + nsteps]
    lf = lf / (lf.std() + 1e-9)
    mf = ndi.gaussian_filter1d(r.standard_normal(nsteps + 20), 4)[10:10 + nsteps]
    mf = mf / (mf.std() + 1e-9)
    pts = [np.array(start, float)]
    widths = []
    for i in range(nsteps):
        t = i / max(nsteps - 1, 1)
        a = ang + 0.3 * lf[i] + 0.18 * mf[i] + r.normal(0, 0.3)
        p = pts[-1] + np.array([np.cos(a), np.sin(a)]) * step * ppm
        if BOUNDS is not None:
            mx, my = BOUNDS[0] * 0.03 + 12, BOUNDS[1] * 0.03 + 12
            if not (mx < p[0] < BOUNDS[0] - mx and my < p[1] < BOUNDS[1] - my):
                break
        pts.append(p)
        taper = np.sin(np.pi * min(1.0, 0.12 + t * 0.88)) ** 0.6 if depth == 0 else (1 - t) ** 0.8
        w = width_mm * (0.2 + 0.8 * taper) * (0.75 + 0.5 * r.random())
        widths.append(w * ppm)
        if depth < max_depth and r.random() < branch_p and 0.08 < t < 0.85:
            ba = a + r.choice([-1, 1]) * r.uniform(0.35, 0.9)
            paths += crack_paths(r, p, ba, length_mm * r.uniform(0.12, 0.35) * (1 - t * 0.5), max(w * 0.55, 0.4),
                                 ppm, depth + 1, max_depth, branch_p * 0.7)
    widths.append(widths[-1] if widths else width_mm * ppm)
    paths.insert(0, (np.array(pts), np.array(widths)))
    return paths


def draw_paths(N, paths, ss):
    im = Image.new('L', (N * ss, N * ss) if isinstance(N, int) else (N[1] * ss, N[0] * ss), 0)
    d = ImageDraw.Draw(im)
    for pts, ws in paths:
        m = len(ws)
        ramp = np.clip((m - 1 - np.arange(m)) / max(m * 0.12, 1), 0.15, 1.0)
        ws = ws * ramp
        for i in range(len(pts) - 1):
            w = max(1, int(round(ws[i] * ss)))
            d.line([tuple(pts[i] * ss), tuple(pts[i + 1] * ss)], fill=255, width=w)
            if w > 3:
                rr = w / 2
                x, y = pts[i + 1] * ss
                d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=255)
    return im


def render_crack(name, out, seed, shape, metres, mode='line'):
    global BOUNDS
    r = rng(seed)
    H, W = shape
    BOUNDS = (W, H)
    ss = 3
    ppm = W / (metres[0] * 1000.0)
    paths = []
    if mode == 'line':
        paths += crack_paths(r, (W * 0.04, H * 0.58), -0.06, metres[0] * 1000 * 0.9, 3.5, ppm, branch_p=0.006)
    else:  # radial network from a corner impact (e.g. a kerb or slab corner hit by a wheel)
        c = (W * 0.3, H * 0.62)
        for k in range(7):
            a = -1.4 + k * 0.42 + r.normal(0, 0.12)
            paths += crack_paths(r, c, a, r.uniform(250, 520), r.uniform(2.0, 3.5), ppm, branch_p=0.006, max_depth=2)
        # concentric connecting cracks
        for rad in (55, 120):
            a0 = r.uniform(-1.5, -1.0)
            pts = []
            for a in np.linspace(a0, a0 + r.uniform(1.8, 2.6), 40):
                rr = rad * ppm * (1 + 0.08 * r.normal())
                pts.append((c[0] + np.cos(a) * rr, c[1] + np.sin(a) * rr))
            pts = np.array(pts)
            paths.append((pts, np.full(len(pts), 1.2 * ppm)))
    im = draw_paths((H, W), paths, ss)
    # jagged edges: warp the supersampled mask with fine noise
    big = np.asarray(im, np.float32) / 255
    hb, wb = big.shape
    dx = blur_noise(hb, wb, 2.5 * ss, seed + 1, wrap=False) * 1.6 * ss
    dy = blur_noise(hb, wb, 2.5 * ss, seed + 2, wrap=False) * 1.6 * ss
    big = warp(big, dx, dy)
    crack = np.asarray(Image.fromarray((np.clip(big, 0, 1) * 255).astype(np.uint8)).resize((W, H), Image.LANCZOS), float) / 255
    # spalled chips along the crack edges
    dist = ndi.distance_transform_edt(crack < 0.5) / ppm  # mm from the crack
    chip_n = fbm(H, W, 3 * ppm, 3, 0.5, seed + 3)
    chips = (dist < 4.5) & (chip_n > 1.0) & (dist > 0)
    chips = ndi.binary_opening(chips, iterations=1)
    chips_f = ndi.gaussian_filter(chips.astype(float), 0.6)
    # dirt and moisture halo
    halo = np.exp(-dist / 7.0) * smoothstep(-1.0, 1.0, fbm(H, W, 10 * ppm, 4, 0.55, seed + 4) + 0.3)
    grit = (r.random((H, W)) > 0.985) & (dist < 12)
    # colour
    c_crack = np.array([0.075, 0.07, 0.065]); c_halo = np.array([0.26, 0.245, 0.225]); c_chip = np.array([0.58, 0.565, 0.54])
    a = np.zeros((H, W)); rgb = np.zeros((H, W, 3))
    a_halo = halo * 0.38
    a_chip = chips_f * 0.55
    a_crack = crack * 0.97
    a = np.maximum(np.maximum(a_halo, a_chip), a_crack)
    wsum = a_halo + a_chip + a_crack * 3 + 1e-6
    rgb = (c_halo * a_halo[..., None] + c_chip * a_chip[..., None] + c_crack * (a_crack * 3)[..., None]) / wsum[..., None]
    rgb[grit] = rgb[grit] * 0.5 + 0.25
    a[grit] = np.maximum(a[grit], 0.6)
    # height: V-groove in the crack, shallow chips, slight lip
    depth = ndi.gaussian_filter(crack, 1.0) * 2.0 + chips_f * 0.6
    height = -depth + 0.15 * fbm(H, W, 2, 3, 0.5, seed + 6) * np.maximum(chips_f, crack)
    nrm = normal_from_height(height, strength=2.2)
    save_rgba(os.path.join(out, name + '.png'), rgb, a)
    save_normal(os.path.join(out, name + '_Normal.png'), nrm)
    print(name)


def crack_seal(name, out, seed, H=2048, W=512, metres=(0.5, 2.0)):
    """Bitumen crack sealant band (overband) meandering along the road."""
    r = rng(seed)
    ppm = W / (metres[0] * 1000.0)
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    t = yy / H
    def n1(sig):
        v = ndi.gaussian_filter1d(r.standard_normal(H + 400), sig)[200:200 + H]
        return (v / (v.std() + 1e-9))[:, None]
    centre = W * 0.5 + n1(260) * 55 * ppm + n1(40) * 9 * ppm + n1(6) * 1.5 * ppm
    width = (24 + 2.5 * n1(80)) * ppm
    d = np.abs(xx - centre)
    edge_noise = fbm(H, W, 3, 4, 0.55, seed + 2) * 2.2 * ppm
    band = smoothstep(width + 1.5, width - 1.5, d + edge_noise)
    # thin feathered squeegee film either side
    film = smoothstep(width + 14 * ppm, width, d + edge_noise * 2) * 0.35 * smoothstep(-0.5, 1.0, fbm(H, W, 12 * ppm, 3, 0.5, seed + 3))
    # ends fade out
    ends = smoothstep(0.0, 0.06, t) * smoothstep(1.0, 0.94, t)
    band *= ends; film *= ends
    sand = (r.random((H, W)) > 0.93) * band
    sand_f = ndi.gaussian_filter(sand.astype(float), 0.5)
    a = np.clip(np.maximum(band * 0.97, film), 0, 1)
    col = np.array([0.035, 0.034, 0.033])[None, None] * np.ones((H, W, 3))
    col = col * (1 - sand_f[..., None]) + np.array([0.33, 0.31, 0.28]) * sand_f[..., None]
    # worn tyre-polished middle is slightly lighter grey
    wear = smoothstep(0.5, 1.6, fbm(H, W, 30 * ppm, 3, 0.5, seed + 5)) * band
    col = col + wear[..., None] * 0.05
    height = band * 1.2 + 0.25 * fbm(H, W, 1.5, 3, 0.5, seed + 6) * band + sand_f * 0.3
    nrm = normal_from_height(ndi.gaussian_filter(height, 0.8), strength=2.0)
    smooth = 0.25 + 0.5 * band * (1 - sand_f) - 0.1 * wear
    save_rgba(os.path.join(out, name + '.png'), col, a)
    save_normal(os.path.join(out, name + '_Normal.png'), nrm)
    print(name)


if __name__ == '__main__':
    out = sys.argv[1]
    which = sys.argv[2:] or ['c1', 'c2', 'seal']
    if 'c1' in which: render_crack('ConcreteCrack_Long', out, 51, (512, 2048), (2.0, 0.5), 'line')
    if 'c2' in which: render_crack('ConcreteCrack_Corner', out, 52, (1024, 1024), (1.0, 1.0), 'radial')
    if 'seal' in which: crack_seal('AsphaltCrackSeal', out, 53)
