"""Wet autumn leaf litter built from the photoscanned ambientCG LeafSet024 (CC0) beech leaves:
leaves cut from the 3x3 atlas, recoloured from summer green to autumn browns/ochres, darkened
as wet, rotated (normal vectors rotated with them) and scattered. 1024 px = 1.0 m."""
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
from dutil import *


def rgb_to_hsv(c):
    import colorsys
    return np.vectorize(colorsys.rgb_to_hsv)


def hsv_shift(rgb, hue_target, amount, sat_mul, val_mul):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = rgb.max(-1); mn = rgb.min(-1); d = mx - mn + 1e-9
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) / 6.0
    s = np.where(mx > 0, d / (mx + 1e-9), 0); v = mx
    # move hue toward the target along the short way round
    dh = ((hue_target - h + 0.5) % 1.0) - 0.5
    h = (h + dh * amount) % 1.0
    s = np.clip(s * sat_mul, 0, 1); v = np.clip(v * val_mul, 0, 1)
    i = np.floor(h * 6).astype(int) % 6; f = h * 6 - np.floor(h * 6)
    p = v * (1 - s); q = v * (1 - f * s); t = v * (1 - (1 - f) * s)
    out = np.zeros_like(rgb)
    for k, (a, b_, c) in enumerate([(v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q)]):
        m = i == k
        out[..., 0][m] = a[m]; out[..., 1][m] = b_[m]; out[..., 2][m] = c[m]
    return out


def main(src_dir, out_dir, seed=95, N=1024, metres=1.0, count=58):
    r = rng(seed)
    C = np.asarray(Image.open(os.path.join(src_dir, 'LeafSet024_2K-JPG_Color.jpg')).convert('RGB'), float) / 255
    O = np.asarray(Image.open(os.path.join(src_dir, 'LeafSet024_2K-JPG_Opacity.jpg')).convert('L'), float) / 255
    Nm = np.asarray(Image.open(os.path.join(src_dir, 'LeafSet024_2K-JPG_NormalGL.jpg')).convert('RGB'), float) / 127.5 - 1
    R = np.asarray(Image.open(os.path.join(src_dir, 'LeafSet024_2K-JPG_Roughness.jpg')).convert('L'), float) / 255
    lab, nl = ndi.label(O > 0.5)
    objs = ndi.find_objects(lab)
    leaves = []
    for k, sl in enumerate(objs):
        hgt = sl[0].stop - sl[0].start
        if hgt < 200:
            continue
        pad = 6
        ys = slice(max(0, sl[0].start - pad), min(O.shape[0], sl[0].stop + pad))
        xs = slice(max(0, sl[1].start - pad), min(O.shape[1], sl[1].stop + pad))
        m = O[ys, xs] * (ndi.binary_dilation(lab[ys, xs] == k + 1, iterations=3))
        leaves.append((C[ys, xs], m, Nm[ys, xs], R[ys, xs]))
    print(len(leaves), 'leaves in the atlas')
    ppm = N / (metres * 1000)
    A = np.zeros((N, N)); Cc = np.zeros((N, N, 3)); Nn = np.zeros((N, N, 3)); Nn[..., 2] = 1; S = np.zeros((N, N))
    hues = [0.10, 0.075, 0.055, 0.12, 0.085, 0.045]
    for i in range(count):
        col, m, nrm, rough = leaves[r.integers(len(leaves))]
        L_mm = r.uniform(62, 98)
        scale = L_mm * ppm / m.shape[0]
        h2, w2 = max(4, int(m.shape[0] * scale)), max(4, int(m.shape[1] * scale))
        ht = hues[r.integers(len(hues))]
        amount = r.uniform(0.75, 1.0) if r.random() < 0.85 else r.uniform(0.3, 0.55)
        wetv = r.uniform(0.42, 0.7)
        # vary the recolour across the leaf: browner toward the edge
        hh, ww = m.shape
        yy, xx = np.mgrid[0:hh, 0:ww]
        edge = ndi.distance_transform_edt(m > 0.5)
        edge = 1 - np.clip(edge / (0.18 * ww), 0, 1)
        c1 = hsv_shift(col, ht, amount, r.uniform(0.8, 1.05), wetv)
        c2 = hsv_shift(col, 0.05, 1.0, 0.75, wetv * 0.75)
        cc = c1 * (1 - 0.6 * edge[..., None]) + c2 * (0.6 * edge[..., None])
        rot = r.uniform(0, 360); flip = r.random() < 0.5
        def tr(a, resample=Image.BICUBIC):
            im = Image.fromarray(np.clip(a * 255, 0, 255).astype(np.uint8))
            im = im.resize((w2, h2), Image.LANCZOS)
            if flip:
                im = im.transpose(Image.FLIP_LEFT_RIGHT)
            return np.asarray(im.rotate(rot, resample, expand=True), float) / 255
        mm = tr(m)
        ccc = tr(cc)
        nn = tr(nrm * 0.5 + 0.5) * 2 - 1
        rr = tr(rough)
        nx, ny = nn[..., 0], nn[..., 1]
        if flip:
            nx = -nx
        th = np.radians(rot)  # PIL rotates counter-clockwise; image y is up in the normal map convention
        nx2 = nx * np.cos(th) - ny * np.sin(th)
        ny2 = nx * np.sin(th) + ny * np.cos(th)
        # the leaf lies slightly curled: tilt the whole leaf a little
        nz = nn[..., 2]
        nvec = np.stack([nx2, ny2, nz], -1)
        nvec /= np.linalg.norm(nvec, axis=-1, keepdims=True) + 1e-9
        hh2, ww2 = mm.shape
        cx, cy = int(r.uniform(0.07, 0.93) * N), int(r.uniform(0.07, 0.93) * N)
        y0, x0 = cy - hh2 // 2, cx - ww2 // 2
        ys = slice(max(0, y0), min(N, y0 + hh2)); xs = slice(max(0, x0), min(N, x0 + ww2))
        sub = lambda a: a[ys.start - y0:ys.stop - y0, xs.start - x0:xs.stop - x0]
        a_ = sub(mm)
        Cc[ys, xs] = Cc[ys, xs] * (1 - a_[..., None]) + sub(ccc) * a_[..., None]
        Nn[ys, xs] = Nn[ys, xs] * (1 - a_[..., None]) + sub(nvec) * a_[..., None]
        S[ys, xs] = S[ys, xs] * (1 - a_) + np.clip(0.55 + 0.35 * (1 - sub(rr)), 0, 0.92) * a_
        A[ys, xs] = A[ys, xs] + (1 - A[ys, xs]) * a_
    rgb = Cc / np.maximum(A, 1e-4)[..., None]
    Nn /= np.linalg.norm(Nn, axis=-1, keepdims=True) + 1e-9
    Nn = Nn * A[..., None] + np.array([0, 0, 1.0]) * (1 - A[..., None])
    Nn /= np.linalg.norm(Nn, axis=-1, keepdims=True)
    save_rgba(os.path.join(out_dir, 'LeafLitter_Wet.png'), np.clip(rgb, 0, 1), A * 0.98)
    save_normal(os.path.join(out_dir, 'LeafLitter_Wet_Normal.png'), Nn)
    save_ms(os.path.join(out_dir, 'LeafLitter_Wet_MetallicSmoothness.png'), S / np.maximum(A, 1e-4) * (A > 0.004))


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
