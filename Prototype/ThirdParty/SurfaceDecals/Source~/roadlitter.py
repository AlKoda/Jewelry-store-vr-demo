"""Street decals: tyre skid mark, muddy tyre tread track, wet leaf litter, small litter scatter."""
import sys, os
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
from dutil import *

OUT = '.'


def tyre_skid(seed=91, H=2048, W=512):
    """Braking skid of one car tyre (205 mm wide). 0.3 x 2.4 m, travel direction = +V (towards the top):
    the mark starts faint at the bottom, darkens, and ends with a heavier deposit near the top."""
    r = rng(seed)
    ppm = W / 300.0
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    t = 1 - yy / H  # 0 at the bottom (start) .. 1 at the top (stop)
    cx = W / 2 + 6 * ppm * fbm(H, 1, 400, 2, 0.5, seed)[:, 0][:, None] + (t * 10 * ppm)
    dx = (xx - cx) / ppm  # mm
    band = smoothstep(104, 96, np.abs(dx))
    grooves = np.zeros((H, W))
    for gx in (-56, -19, 19, 56):
        grooves = np.maximum(grooves, smoothstep(5.5, 3.0, np.abs(dx - gx)))
    stri = ndi.gaussian_filter(r.standard_normal((H, W)), (80, 0.8)); stri = stri / stri.std()
    inten = smoothstep(0.02, 0.35, t) * (0.55 + 0.45 * t) * smoothstep(0.985, 0.955, t)
    inten *= 0.85 + 0.15 * smoothstep(-1, 1, fbm(H, 1, 60, 3, 0.5, seed + 1)[:, 0][:, None])
    end = smoothstep(0.02, 0.0, np.abs(t - 0.95)) * 0.25
    edge = 0.7 + 0.3 * smoothstep(60, 95, np.abs(dx))  # shoulders deposit more rubber
    a = band * (inten * edge * (1 - 0.65 * grooves) * (0.8 + 0.2 * smoothstep(-1.5, 1.5, stri)) + end)
    a = np.clip(a, 0, 0.9)
    col = np.broadcast_to(np.array([0.035, 0.034, 0.033]), (H, W, 3)).copy()
    save_rgba(os.path.join(OUT, 'TyreSkid.png'), col, a)


def tyre_tread_mud(seed=92, H=2048, W=512):
    """Muddy tread print of a car tyre that drove off a verge or through mud: 0.3 x 2.4 m, the mud
    load fades along +V. Generic tread (no brand): 4 circumferential grooves, shoulder blocks, sipes."""
    r = rng(seed)
    ppm = W / 300.0
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    t = yy / H
    cx = W / 2 + 4 * ppm * fbm(H, 1, 500, 2, 0.5, seed)[:, 0][:, None]
    x = (xx - cx) / ppm; y = yy / ppm
    band = smoothstep(104, 98, np.abs(x))
    pitch = 31.0
    contact = np.ones((H, W), bool)
    for gx in (-56, -19, 19, 56):
        contact &= np.abs(x - gx) > 4.5
    # lateral grooves in the shoulders (angled), and sipes in the ribs
    shoulder = np.abs(x) > 60
    lat = (((y + 0.35 * np.abs(x)) % pitch) < 5.5) & shoulder
    sipe = (((y - 0.5 * x) % (pitch / 2)) < 1.2) & (np.abs(x) < 52) & ~shoulder
    notch = (((y + 8 + 0.2 * x) % pitch) < 4) & (np.abs(np.abs(x) - 37) < 9)
    contact &= ~lat & ~sipe & ~notch
    contact &= band > 0.5
    cf = ndi.gaussian_filter(contact.astype(float), 0.7)
    load = smoothstep(1.0, 0.05, t) ** 1.3
    mud = fbm(H, W, 12, 4, 0.55, seed + 1)
    cover = cf * smoothstep(0.15, 0.6, load + 0.25 * mud)
    clumps = smoothstep(1.1, 1.6, mud + 0.6 * load) * band * 0.9
    thick = np.clip(np.maximum(cover, clumps), 0, 1)
    a = np.clip(thick * 0.88, 0, 1) * smoothstep(0.0, 0.02, t) * smoothstep(1.0, 0.97, t)
    wet = smoothstep(0.2, 0.9, load)
    col = np.array([0.38, 0.31, 0.23])[None, None] * (1 - wet[..., None]) + np.array([0.19, 0.145, 0.10])[None, None] * wet[..., None]
    col = col * (0.9 + 0.1 * mud[..., None])
    hgt = thick * 0.8 + 0.1 * mud * thick
    save_rgba(os.path.join(OUT, 'TyreTread_Mud.png'), col, a)
    save_normal(os.path.join(OUT, 'TyreTread_Mud_Normal.png'), normal_from_height(ndi.gaussian_filter(hgt, 0.7), 3.0))


def leaf_shape(r, kind, size_px):
    """Return (mask, vein, curl) arrays for one leaf drawn in a square patch, petiole pointing down."""
    n = int(size_px * 1.5) | 1
    c = n / 2
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    x = (xx - c) / (size_px / 2); y = (c - yy) / (size_px / 2)
    ang = np.arctan2(x, y)  # 0 = up along the midrib
    rad = np.sqrt(x ** 2 + y ** 2)
    if kind == 'lobed':  # plane / maple-like, 5 lobes
        lobes = np.abs(np.cos(ang * 2.5)) ** 1.15
        R = (0.50 + 0.47 * lobes) * (1 - 0.28 * np.abs(ang) / np.pi)
        R *= 1 - 0.55 * smoothstep(2.3, 3.14, np.abs(ang))  # notch at the petiole
        teeth = 0.035 * np.abs(np.sin(ang * 22))
        mask = smoothstep(R - teeth + 0.02, R - teeth - 0.02, rad)
        veins = np.zeros_like(mask)
        for va in (-2.5, -1.25, 0.0, 1.25, 2.5):
            va2 = va / 2.5 * 2.5 / 2.5 * (np.pi / 2.5) * 1.0
            d = np.abs(np.sin(ang - va2 * 1.0)) * rad
            veins = np.maximum(veins, smoothstep(0.025, 0.0, d) * (np.cos(ang - va2) > 0))
    else:  # ovate, serrated (lime / birch / poplar-like)
        s = (y + 1) / 2  # 0 at the base .. 1 at the tip
        half = 0.62 * np.sin(np.pi * np.clip(s, 0, 1)) ** 0.75 * (1 - 0.35 * s)
        half *= 1 + 0.06 * np.abs(np.sin(s * 40))  # serration
        mask = smoothstep(half + 0.02, half - 0.02, np.abs(x)) * (s > 0.0) * (s < 1.0)
        veins = smoothstep(0.022, 0.0, np.abs(x)) * (s > 0) * (s < 0.97)
        for k in range(1, 7):
            s0 = k / 7.5
            d = np.abs(y - (s0 * 2 - 1) - 0.7 * np.abs(x))
            veins = np.maximum(veins, smoothstep(0.02, 0.0, d) * (np.abs(x) < half * 0.92) * 0.7)
    # petiole
    pet = smoothstep(0.03, 0.0, np.abs(x)) * (y < -0.6) * (y > -1.0 - 0.25)
    mask = np.maximum(mask, pet)
    curl = (1 - rad ** 2) * 0.6
    return mask, veins * mask, curl * mask


def leaf_litter(seed=93, N=1024, metres=1.0, count=70, name='LeafLitter_Wet'):
    r = rng(seed)
    ppm = N / (metres * 1000)
    A = np.zeros((N, N)); C = np.zeros((N, N, 3)); Hh = np.zeros((N, N))
    palette = [(0.34, 0.21, 0.09), (0.46, 0.30, 0.10), (0.22, 0.14, 0.07), (0.52, 0.39, 0.13), (0.38, 0.34, 0.13), (0.28, 0.17, 0.09)]
    for i in range(count):
        kind = 'lobed' if r.random() < 0.45 else 'ovate'
        size = (r.uniform(70, 140) if kind == 'lobed' else r.uniform(45, 95)) * ppm
        mask, vein, curl = leaf_shape(r, kind, size)
        rot = r.uniform(0, 360)
        flip = r.random() < 0.5
        def tr(a):
            im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
            if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT)
            return np.asarray(im.rotate(rot, Image.BICUBIC), float) / 255
        m, v, cu = tr(mask), tr(vein), tr(curl)
        # decay holes and darker wet patches
        n = m.shape[0]
        hole = smoothstep(1.3, 1.6, fbm(n, n, max(2, size / 14), 3, 0.5, seed * 100 + i)) * (r.random() < 0.4)
        m = m * (1 - hole)
        base = np.array(palette[r.integers(len(palette))]) * r.uniform(0.75, 1.05)
        mott = fbm(n, n, max(2, size / 10), 4, 0.55, seed * 200 + i)
        col = base[None, None] * (0.8 + 0.2 * mott[..., None])
        col = col * (1 - 0.35 * smoothstep(0.4, 1.6, mott)[..., None])  # dark wet blotches
        col = col * (1 + 0.25 * v[..., None])
        # place
        cx, cy = int(r.uniform(0.08, 0.92) * N), int(r.uniform(0.08, 0.92) * N)
        y0, x0 = cy - n // 2, cx - n // 2
        ys, xs = slice(max(0, y0), min(N, y0 + n)), slice(max(0, x0), min(N, x0 + n))
        sm = m[ys.start - y0:ys.stop - y0, xs.start - x0:xs.stop - x0]
        sc = col[ys.start - y0:ys.stop - y0, xs.start - x0:xs.stop - x0]
        sh = (cu + v * 0.15)[ys.start - y0:ys.stop - y0, xs.start - x0:xs.stop - x0]
        C[ys, xs] = C[ys, xs] * (1 - sm[..., None]) + sc * sm[..., None]
        Hh[ys, xs] = Hh[ys, xs] * (1 - sm) + (sh + i * 0.002) * sm
        A[ys, xs] = A[ys, xs] + (1 - A[ys, xs]) * sm
    A *= 0.96
    save_rgba(os.path.join(OUT, name + '.png'), C / np.maximum(A, 1e-4)[..., None] * 0.96, A)
    save_normal(os.path.join(OUT, name + '_Normal.png'), normal_from_height(ndi.gaussian_filter(Hh, 0.8), 6.0))


def litter(seed=94, N=1024):
    """Cigarette butts, a burnt match, paper receipt scraps, crown caps, foil and grit. 1.0 m."""
    r = rng(seed)
    ppm = N / 1000.0
    A = np.zeros((N, N)); C = np.zeros((N, N, 3)); Hh = np.zeros((N, N))
    yy, xx = np.mgrid[0:N, 0:N].astype(float)

    def put(mask, col, hgt):
        nonlocal A, C, Hh
        C = C * (1 - mask[..., None]) + col * mask[..., None]
        Hh = Hh * (1 - mask) + hgt * mask
        A = A + (1 - A) * mask

    def local(cx, cy, ang):
        c, s = np.cos(ang), np.sin(ang)
        return ((xx - cx) * c + (yy - cy) * s) / ppm, (-(xx - cx) * s + (yy - cy) * c) / ppm

    for i in range(16):  # cigarette butts: 8 mm diameter, filter 15-21 mm, paper/ash 3-12 mm
        cx, cy = r.uniform(0.06, 0.94) * N, r.uniform(0.06, 0.94) * N
        ang = r.uniform(0, 2 * np.pi)
        lx, ly = local(cx, cy, ang)
        fl, pl = r.uniform(15, 21), r.uniform(3, 12)
        body = smoothstep(4.2, 3.6, np.abs(ly)) * smoothstep(-0.5, 0.5, lx) * smoothstep(fl + pl + 0.5, fl + pl - 0.5, lx)
        squash = r.random() < 0.5
        if squash:
            body *= smoothstep(4.8, 3.8, np.abs(ly) * (1 + 0.3 * np.sin(lx * 0.6)))
        filt = lx < fl
        speck = (r.random((N, N)) > 0.7) * 0.08
        aged = r.uniform(0.6, 1.0)
        col = np.where(filt[..., None], np.array([0.80, 0.58, 0.32]) * aged - speck[..., None], np.array([0.86, 0.85, 0.82]) * aged)
        ash = smoothstep(fl + pl - 2.5, fl + pl, lx)
        col = col * (1 - ash[..., None]) + np.array([0.18, 0.17, 0.16]) * ash[..., None]
        band = smoothstep(0.6, 0.0, np.abs(lx - fl)) * 0.3
        col = col * (1 - band[..., None])
        hgt = np.sqrt(np.clip(1 - (ly / 4.0) ** 2, 0, 1)) * (0.5 if squash else 1.0)
        put(body, col, hgt)
    for i in range(5):  # paper scraps (receipt): crumpled, faint print lines
        cx, cy = r.uniform(0.1, 0.9) * N, r.uniform(0.1, 0.9) * N
        lx, ly = local(cx, cy, r.uniform(0, np.pi))
        w, h = r.uniform(25, 60), r.uniform(30, 80)
        edge = fbm(N, N, 4, 3, 0.5, seed + 10 + i) * 2.5
        m = smoothstep(w / 2 + 1, w / 2 - 1, np.abs(lx) + edge) * smoothstep(h / 2 + 1, h / 2 - 1, np.abs(ly) + edge)
        crumple = fbm(N, N, 6 * ppm + 2, 3, 0.6, seed + 20 + i)
        lines = (np.abs(((ly + h / 2) % 4.2) - 2.1) < 0.45) * (np.abs(lx) < w / 2 - 4) * smoothstep(0.2, 0.8, fbm(N, N, 3, 2, 0.5, seed + 30 + i))
        wet = r.uniform(0.75, 1.0)
        col = np.array([0.88, 0.87, 0.84]) * wet * (0.92 + 0.08 * crumple[..., None]) - lines[..., None] * 0.35
        put(m, col, 0.3 + 0.25 * crumple)
    for i in range(4):  # crown caps 26 mm, crimped edge, generic metal
        cx, cy = r.uniform(0.08, 0.92) * N, r.uniform(0.08, 0.92) * N
        lx, ly = local(cx, cy, r.uniform(0, 6.3))
        rr = np.sqrt(lx ** 2 + ly ** 2); th = np.arctan2(ly, lx)
        edge = 13.2 + 0.6 * np.cos(th * 21)
        m = smoothstep(edge + 0.6, edge - 0.6, rr)
        if r.random() < 0.5:  # bent cap
            m *= smoothstep(-6, -4, lx + 2 * np.sin(ly * 0.2))
        metal = np.array([0.62, 0.60, 0.55]) if r.random() < 0.6 else np.array([0.70, 0.56, 0.25])
        rust = smoothstep(0.8, 1.6, fbm(N, N, 3, 3, 0.5, seed + 40 + i))
        col = metal * (0.85 + 0.15 * (rr < 10.5))[..., None] * (1 - 0.5 * rust[..., None]) + np.array([0.35, 0.2, 0.1]) * 0.5 * rust[..., None]
        hgt = 0.6 * smoothstep(13, 10, rr) + 0.9 * smoothstep(10, 12, rr) * smoothstep(13.5, 12, rr) + 0.15 * np.cos(th * 21) * (rr > 11)
        put(m, col, hgt)
    for i in range(3):  # burnt matches 45 mm
        cx, cy = r.uniform(0.1, 0.9) * N, r.uniform(0.1, 0.9) * N
        lx, ly = local(cx, cy, r.uniform(0, 6.3))
        m = smoothstep(1.4, 1.0, np.abs(ly)) * smoothstep(-0.5, 0.5, lx) * smoothstep(45.5, 44.5, lx)
        head = smoothstep(2.6, 1.6, np.sqrt((lx - 44) ** 2 + (ly * 1.2) ** 2))
        m = np.maximum(m, head)
        burnt = smoothstep(30, 40, lx)
        col = np.array([0.72, 0.6, 0.42]) * (1 - burnt[..., None]) + np.array([0.1, 0.09, 0.08]) * burnt[..., None]
        put(m, col, 0.5 + 0.3 * head)
    for i in range(6):  # foil / wrapper bits
        cx, cy = r.uniform(0.08, 0.92) * N, r.uniform(0.08, 0.92) * N
        lx, ly = local(cx, cy, r.uniform(0, 6.3))
        s = r.uniform(8, 20)
        edge = fbm(N, N, 3, 2, 0.5, seed + 60 + i) * 2.0
        m = smoothstep(s / 2 + 1, s / 2 - 1, np.maximum(np.abs(lx), np.abs(ly) * 1.4) + edge)
        cr = fbm(N, N, 2, 3, 0.6, seed + 70 + i)
        col = np.array([0.72, 0.72, 0.74]) * (0.7 + 0.3 * smoothstep(-1, 1, cr))[..., None]
        put(m, col, 0.2 + 0.2 * cr)
    grit = (r.random((N, N)) > 0.9993).astype(float) * smoothstep(-0.5, 1.5, fbm(N, N, 60, 3, 0.5, seed + 90))
    g = np.clip(ndi.gaussian_filter(grit, r.uniform(0.6, 1.2)) * 5, 0, 1)
    put(g * 0.9, np.array([0.38, 0.36, 0.33]), g * 0.8)
    save_rgba(os.path.join(OUT, 'LitterScatter.png'), C, border_fade(A, 0.03))
    save_normal(os.path.join(OUT, 'LitterScatter_Normal.png'), normal_from_height(ndi.gaussian_filter(Hh, 0.7), 5.0))


if __name__ == '__main__':
    OUT = sys.argv[1]
    which = sys.argv[2:] or ['skid', 'tread', 'litter']  # leaves: see leaves_photo.py
    if 'skid' in which: tyre_skid()
    if 'tread' in which: tyre_tread_mud()
    if 'leaves' in which: leaf_litter()
    if 'litter' in which: litter()
