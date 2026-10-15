"""Software preview of OBJ (+MTL map_Kd): z-buffer rasteriser with textures and lambert shading.
usage: preview.py OUT.png [--size N] model.obj [model2.obj ...]  (each model: two views side by side)"""
import sys, os
import numpy as np
from PIL import Image, ImageDraw

Image.MAX_IMAGE_PIXELS = None
TEX = 512


def load_obj(path):
    d = os.path.dirname(path)
    v, vt = [], []
    tris = []  # (vi3, ti3, mat)
    mats = {}
    cur = None
    for line in open(path):
        if line.startswith('v '):
            v.append([float(x) for x in line.split()[1:4]])
        elif line.startswith('vt '):
            vt.append([float(x) for x in line.split()[1:3]])
        elif line.startswith('usemtl'):
            cur = line.split()[1]
        elif line.startswith('mtllib'):
            name = None
            for l2 in open(os.path.join(d, line.split()[1])):
                p = l2.split()
                if not p:
                    continue
                if p[0] == 'newmtl':
                    name = p[1]; mats[name] = {'kd': np.array([0.8, 0.8, 0.8]), 'img': None, 'd': 1.0}
                elif p[0] == 'Kd':
                    mats[name]['kd'] = np.array([float(x) for x in p[1:4]])
                elif p[0] == 'd':
                    mats[name]['d'] = float(p[1])
                elif p[0] == 'map_Kd':
                    im = Image.open(os.path.join(d, p[1]))
                    rgba = np.asarray(im.convert('RGBA').resize((TEX, TEX))).astype(np.float32) / 255.0
                    mats[name]['img'] = rgba
        elif line.startswith('f '):
            idx = [x.split('/') for x in line.split()[1:]]
            vi = [int(x[0]) - 1 for x in idx]
            ti = [int(x[1]) - 1 if len(x) > 1 and x[1] else -1 for x in idx]
            for k in range(1, len(vi) - 1):
                tris.append(([vi[0], vi[k], vi[k + 1]], [ti[0], ti[k], ti[k + 1]], cur))
    return np.array(v, float), (np.array(vt, float) if vt else np.zeros((1, 2))), tris, mats


def render(path, size=520, yaw=35, pitch=18, bg=(58, 62, 70)):
    v, vt, tris, mats = load_obj(path)
    c = (v.max(0) + v.min(0)) / 2
    r = np.linalg.norm(v.max(0) - v.min(0)) / 2 + 1e-6
    y, p = np.radians(yaw), np.radians(-pitch)
    R1 = np.array([[np.cos(y), 0, np.sin(y)], [0, 1, 0], [-np.sin(y), 0, np.cos(y)]])
    R2 = np.array([[1, 0, 0], [0, np.cos(p), -np.sin(p)], [0, np.sin(p), np.cos(p)]])
    q = (v - c) @ R1.T @ R2.T
    dist = r * 3.0
    zc = q[:, 2] + dist
    f = size * 1.4
    sx = size / 2 - f * q[:, 0] / zc
    sy = size / 2 - f * q[:, 1] / zc
    L = np.array([0.45, 0.8, -0.4]); L /= np.linalg.norm(L)
    zbuf = np.full((size, size), np.inf)
    img = np.zeros((size, size, 3)); img[:] = np.array(bg) / 255.0
    for vi, ti, m in tris:
        md = mats.get(m)
        if md is None:
            md = {'kd': np.array([0.8, 0.8, 0.8]), 'img': None, 'd': 1}
        P = q[vi]
        n = np.cross(P[1] - P[0], P[2] - P[0]); ln = np.linalg.norm(n)
        if ln == 0:
            continue
        n /= ln
        shade = 0.30 + 0.70 * abs(float(np.dot(n, L)))
        xs, ys, zs = sx[vi], sy[vi], zc[vi]
        x0, x1 = int(max(0, np.floor(xs.min()))), int(min(size - 1, np.ceil(xs.max())))
        y0, y1 = int(max(0, np.floor(ys.min()))), int(min(size - 1, np.ceil(ys.max())))
        if x1 < x0 or y1 < y0:
            continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        d = (ys[1] - ys[2]) * (xs[0] - xs[2]) + (xs[2] - xs[1]) * (ys[0] - ys[2])
        if abs(d) < 1e-12:
            continue
        w0 = ((ys[1] - ys[2]) * (gx - xs[2]) + (xs[2] - xs[1]) * (gy - ys[2])) / d
        w1 = ((ys[2] - ys[0]) * (gx - xs[2]) + (xs[0] - xs[2]) * (gy - ys[2])) / d
        w2 = 1 - w0 - w1
        mask = (w0 >= -1e-6) & (w1 >= -1e-6) & (w2 >= -1e-6)
        if not mask.any():
            continue
        # perspective-correct
        iz = w0 / zs[0] + w1 / zs[1] + w2 / zs[2]
        z = 1.0 / iz
        sub = zbuf[y0:y1 + 1, x0:x1 + 1]
        mask &= z < sub
        if not mask.any():
            continue
        col = np.broadcast_to(md['kd'], mask.shape + (3,)).copy()
        alpha_ok = mask
        if md['img'] is not None and ti[0] >= 0:
            T = vt[ti]
            u = (w0 * T[0, 0] / zs[0] + w1 * T[1, 0] / zs[1] + w2 * T[2, 0] / zs[2]) * z
            w = (w0 * T[0, 1] / zs[0] + w1 * T[1, 1] / zs[1] + w2 * T[2, 1] / zs[2]) * z
            ui = (np.mod(u, 1.0) * (TEX - 1)).astype(int)
            wi = ((1 - np.mod(w, 1.0)) * (TEX - 1)).astype(int)
            tx = md['img'][wi, ui]
            col = col * tx[..., :3]
            alpha_ok = mask & (tx[..., 3] > 0.4)
        if md.get('d', 1) < 0.5:
            continue
        sub[alpha_ok] = z[alpha_ok]
        tgt = img[y0:y1 + 1, x0:x1 + 1]
        tgt[alpha_ok] = (col * shade)[alpha_ok]
    im = Image.fromarray((np.clip(img, 0, 1) ** (1 / 1.1) * 255).astype(np.uint8))
    dr = ImageDraw.Draw(im)
    dr.text((6, 6), os.path.basename(path) + '  %.2f x %.2f x %.2f m  %d tris' % (tuple(v.max(0) - v.min(0)) + (len(tris),)),
            fill=(255, 255, 255))
    return im


if __name__ == '__main__':
    args = sys.argv[1:]
    out = args.pop(0)
    size = 520
    if args and args[0] == '--size':
        args.pop(0); size = int(args.pop(0))
    rows = []
    for pth in args:
        a = render(pth, size, yaw=30)
        b = render(pth, size, yaw=-150)
        row = Image.new('RGB', (size * 2, size)); row.paste(a, (0, 0)); row.paste(b, (size, 0))
        rows.append(row)
    sheet = Image.new('RGB', (size * 2, size * len(rows)))
    for i, r in enumerate(rows):
        sheet.paste(r, (0, size * i))
    sheet.save(out)
