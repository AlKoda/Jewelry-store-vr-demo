"""Inspection renderer for OBJ/MTL: back-face culling like Unity's Standard shader, normal maps (from the MTL
'# Normal' comment), optional close-up framing.
render2.py out.png model.obj --yaw 35 --pitch 20 --size 640 [--target x y z --radius r] [--nocull] [--nonormal]"""
import sys, os, math, argparse
import numpy as np
from PIL import Image

def load_obj(path):
    d = os.path.dirname(path); V, T, N = [], [], []; groups = []; cur = None; mtllib = None
    for line in open(path):
        p = line.split()
        if not p: continue
        if p[0] == 'v': V.append([float(x) for x in p[1:4]])
        elif p[0] == 'vt': T.append([float(x) for x in p[1:3]])
        elif p[0] == 'vn': N.append([float(x) for x in p[1:4]])
        elif p[0] == 'mtllib': mtllib = p[1]
        elif p[0] == 'usemtl': cur = [p[1], []]; groups.append(cur)
        elif p[0] == 'f':
            if cur is None: cur = ['default', []]; groups.append(cur)
            cs = [tuple(int(x) - 1 if x else -1 for x in (c.split('/') + ['', ''])[:3]) for c in p[1:]]
            for k in range(1, len(cs) - 1): cur[1].append((cs[0], cs[k], cs[k + 1]))
    mats = {}
    if mtllib and os.path.exists(os.path.join(d, mtllib)):
        m = None
        for line in open(os.path.join(d, mtllib)):
            p = line.split()
            if not p: continue
            if p[0] == 'newmtl': m = mats.setdefault(p[1], {'Kd': (1, 1, 1), 'd': 1.0, 'map': None, 'nmap': None})
            elif p[0] == 'Kd': m['Kd'] = tuple(float(x) for x in p[1:4])
            elif p[0] == 'd': m['d'] = float(p[1])
            elif p[0] == 'map_Kd': m['map'] = os.path.join(d, p[1])
            elif p[0] == '#' and len(p) > 2 and p[1] == 'Normal': m['nmap'] = os.path.join(d, p[2])
    return np.array(V), np.array(T) if T else np.zeros((1, 2)), np.array(N) if N else None, groups, mats

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('out'); ap.add_argument('obj')
    ap.add_argument('--yaw', type=float, default=35); ap.add_argument('--pitch', type=float, default=20); ap.add_argument('--size', type=int, default=640)
    ap.add_argument('--target', type=float, nargs=3); ap.add_argument('--radius', type=float)
    ap.add_argument('--nocull', action='store_true'); ap.add_argument('--nonormal', action='store_true')
    ap.add_argument('--light', type=float, nargs=3, default=[0.4, 0.8, 0.6])
    a = ap.parse_args()
    V, T, N, groups, mats = load_obj(a.obj); size = a.size
    lo, hi = V.min(0), V.max(0)
    c = np.array(a.target) if a.target else (lo + hi) / 2
    r = a.radius or np.linalg.norm(hi - lo) / 2
    ya, pa = math.radians(a.yaw), math.radians(a.pitch)
    fwd = -np.array([math.sin(ya) * math.cos(pa), math.sin(pa), math.cos(ya) * math.cos(pa)])
    right = np.cross(fwd, [0, 1, 0]); right /= np.linalg.norm(right); up = np.cross(right, fwd)
    dist = r / math.tan(math.radians(14)) * 1.05; eye = c - fwd * dist
    img = np.zeros((size, size, 3), np.float32); img[:] = np.array([48, 52, 58]) / 255
    zb = np.full((size, size), np.inf, np.float32); f = 1 / math.tan(math.radians(15))
    L = np.array(a.light, float); L /= np.linalg.norm(L)
    cache = {}
    def tex(p):
        if p and os.path.exists(p):
            if p not in cache: cache[p] = np.asarray(Image.open(p).convert('RGBA'), np.float32) / 255
            return cache[p]
    for mname, faces in sorted(groups, key=lambda g: mats.get(g[0], {}).get('d', 1) < 0.99):
        m = mats.get(mname, {'Kd': (0.8, 0.8, 0.8), 'd': 1, 'map': None, 'nmap': None})
        tc, tn = tex(m['map']), (None if a.nonormal else tex(m['nmap']))
        kd = np.array(m['Kd']); alpha = m['d']; F = np.array(faces)
        P = V[F[:, :, 0]]; rel = P - eye
        cx = rel @ right; cy = rel @ up; cz = rel @ fwd
        sx = (cx / cz * f * 0.5 + 0.5) * size; sy = (0.5 - cy / cz * f * 0.5) * size
        fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0]); fn /= np.linalg.norm(fn, axis=1, keepdims=True) + 1e-12
        NN = N[F[:, :, 2]] if N is not None and (F[:, :, 2] >= 0).all() else np.repeat(fn[:, None], 3, 1)
        UV = T[np.clip(F[:, :, 1], 0, None)]
        e1, e2 = P[:, 1] - P[:, 0], P[:, 2] - P[:, 0]; d1, d2 = UV[:, 1] - UV[:, 0], UV[:, 2] - UV[:, 0]
        det = d1[:, 0] * d2[:, 1] - d2[:, 0] * d1[:, 1]; det = np.where(np.abs(det) < 1e-14, 1e-14, det)
        TG = (e1 * d2[:, 1:2] - e2 * d1[:, 1:2]) / det[:, None]; BG = (e2 * d1[:, 0:1] - e1 * d2[:, 0:1]) / det[:, None]
        for i in range(len(F)):
            if not a.nocull and np.dot(fn[i], P[i, 0] - eye) >= 0: continue
            xs, ys = sx[i], sy[i]
            x0, x1 = int(max(0, math.floor(xs.min()))), int(min(size - 1, math.ceil(xs.max())))
            y0, y1 = int(max(0, math.floor(ys.min()))), int(min(size - 1, math.ceil(ys.max())))
            if x1 < x0 or y1 < y0 or (cz[i] <= 0).any(): continue
            gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
            d = (ys[1] - ys[2]) * (xs[0] - xs[2]) + (xs[2] - xs[1]) * (ys[0] - ys[2])
            if abs(d) < 1e-12: continue
            w0 = ((ys[1] - ys[2]) * (gx - xs[2]) + (xs[2] - xs[1]) * (gy - ys[2])) / d
            w1 = ((ys[2] - ys[0]) * (gx - xs[2]) + (xs[0] - xs[2]) * (gy - ys[2])) / d
            w2 = 1 - w0 - w1
            inside = (w0 >= 0) & (w1 >= 0) & (w2 >= 0)
            if not inside.any(): continue
            # perspective-correct weights
            q0, q1, q2 = w0 / cz[i, 0], w1 / cz[i, 1], w2 / cz[i, 2]; qs = q0 + q1 + q2
            z = 1.0 / qs; p0, p1, p2 = q0 * z, q1 * z, q2 * z
            sub = zb[y0:y1 + 1, x0:x1 + 1]; mask = inside & (z < sub)
            if not mask.any(): continue
            n = p0[..., None] * NN[i, 0] + p1[..., None] * NN[i, 1] + p2[..., None] * NN[i, 2]
            n /= np.linalg.norm(n, axis=-1, keepdims=True) + 1e-12
            if a.nocull:
                flip = (n @ -fwd) < 0; n[flip] *= -1
            u = p0 * UV[i, 0, 0] + p1 * UV[i, 1, 0] + p2 * UV[i, 2, 0]; v = p0 * UV[i, 0, 1] + p1 * UV[i, 1, 1] + p2 * UV[i, 2, 1]
            col = np.broadcast_to(kd, n.shape).copy(); al = np.ones(n.shape[:2]) * alpha
            if tc is not None:
                th, tw = tc.shape[:2]
                s = tc[((1 - np.mod(v, 1)) * th).astype(int).clip(0, th - 1), (np.mod(u, 1) * tw).astype(int).clip(0, tw - 1)]
                col = col * s[..., :3]; al = al * s[..., 3]
                if alpha >= 0.99: mask = mask & (s[..., 3] >= 0.5)   # alpha test (cut-out)
            if tn is not None:
                th, tw = tn.shape[:2]
                s = tn[((1 - np.mod(v, 1)) * th).astype(int).clip(0, th - 1), (np.mod(u, 1) * tw).astype(int).clip(0, tw - 1)][..., :3] * 2 - 1
                t_ = TG[i] - n * (n @ TG[i])[..., None]; t_ /= np.linalg.norm(t_, axis=-1, keepdims=True) + 1e-12
                sg = 1.0 if np.dot(np.cross(fn[i], TG[i]), BG[i]) >= 0 else -1.0
                b_ = np.cross(n, t_) * sg
                n = t_ * s[..., 0:1] + b_ * s[..., 1:2] + n * s[..., 2:3]; n /= np.linalg.norm(n, axis=-1, keepdims=True) + 1e-12
            vd = -fwd; h = (L + vd) / np.linalg.norm(L + vd)
            lam = np.clip(n @ L, 0, 1) * 0.75 + 0.3 + 0.12 * np.clip(n @ np.array([-0.5, 0.2, -0.6]), 0, 1)
            spec = 0.18 * np.clip(n @ h, 0, 1) ** 40
            shaded = col * lam[..., None] + spec[..., None]
            simg = img[y0:y1 + 1, x0:x1 + 1]
            if alpha < 0.99:
                simg[mask] = simg[mask] * (1 - al[mask][:, None]) + shaded[mask] * al[mask][:, None]
            else:
                simg[mask] = shaded[mask]; sub[mask] = z[mask]
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(a.out)

main()
