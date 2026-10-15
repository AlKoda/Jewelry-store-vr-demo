"""Software preview renderer for OBJ/MTL (+map_Kd) files: render.py out.png model.obj [yaw_deg] [pitch_deg] [size]
Lambert + ambient, textured, z-buffered. Front = +Z. Used only to eyeball the pack (no GPU here)."""
import sys, os, math
import numpy as np
from PIL import Image

def load_obj(path):
    d = os.path.dirname(path)
    V, T, N = [], [], []
    groups = []  # (mtl, faces[(vi,ti,ni)*3])
    cur = None; mtllib = None
    for line in open(path):
        p = line.split()
        if not p: continue
        if p[0] == 'v': V.append([float(x) for x in p[1:4]])
        elif p[0] == 'vt': T.append([float(x) for x in p[1:3]])
        elif p[0] == 'vn': N.append([float(x) for x in p[1:4]])
        elif p[0] == 'mtllib': mtllib = p[1]
        elif p[0] == 'usemtl':
            cur = [p[1], []]; groups.append(cur)
        elif p[0] == 'f':
            if cur is None:
                cur = ['default', []]; groups.append(cur)
            cs = []
            for c in p[1:]:
                a = c.split('/')
                vi = int(a[0]) - 1; ti = int(a[1]) - 1 if len(a) > 1 and a[1] else -1; ni = int(a[2]) - 1 if len(a) > 2 and a[2] else -1
                cs.append((vi, ti, ni))
            for k in range(1, len(cs) - 1):
                cur[1].append((cs[0], cs[k], cs[k + 1]))
    mats = {}
    if mtllib and os.path.exists(os.path.join(d, mtllib)):
        m = None
        for line in open(os.path.join(d, mtllib)):
            p = line.split()
            if not p: continue
            if p[0] == 'newmtl': m = mats.setdefault(p[1], {'Kd': (1, 1, 1), 'd': 1.0, 'map': None})
            elif p[0] == 'Kd': m['Kd'] = tuple(float(x) for x in p[1:4])
            elif p[0] == 'd': m['d'] = float(p[1])
            elif p[0] == 'map_Kd': m['map'] = os.path.join(d, p[1])
    return np.array(V), np.array(T) if T else np.zeros((1, 2)), np.array(N) if N else None, groups, mats

def render(path, out, yaw=35, pitch=20, size=640, bg=(48, 52, 58)):
    V, T, N, groups, mats = load_obj(path)
    lo, hi = V.min(0), V.max(0); c = (lo + hi) / 2; r = np.linalg.norm(hi - lo) / 2 or 1
    ya, pa = math.radians(yaw), math.radians(pitch)
    # camera looking at c from direction (sin ya cos pa, sin pa, cos ya cos pa)
    fwd = -np.array([math.sin(ya) * math.cos(pa), math.sin(pa), math.cos(ya) * math.cos(pa)])
    right = np.cross(fwd, [0, 1, 0]); right /= np.linalg.norm(right); up = np.cross(right, fwd)
    dist = r / math.tan(math.radians(14)) * 1.05
    eye = c - fwd * dist
    img = np.zeros((size, size, 3), np.float32); img[:] = np.array(bg) / 255
    zb = np.full((size, size), np.inf, np.float32)
    f = 1 / math.tan(math.radians(15))
    light = np.array([0.4, 0.8, 0.6]); light /= np.linalg.norm(light)
    texcache = {}
    order = sorted(groups, key=lambda g: mats.get(g[0], {}).get('d', 1) < 0.99)
    for mname, faces in order:
        m = mats.get(mname, {'Kd': (0.8, 0.8, 0.8), 'd': 1, 'map': None})
        tex = None
        if m['map'] and os.path.exists(m['map']):
            if m['map'] not in texcache:
                texcache[m['map']] = np.asarray(Image.open(m['map']).convert('RGBA'), np.float32) / 255
            tex = texcache[m['map']]
        kd = np.array(m['Kd']); alpha = m['d']
        F = np.array(faces)  # (n,3,3)
        P = V[F[:, :, 0]]
        rel = P - eye
        cx = rel @ right; cy = rel @ up; cz = rel @ fwd
        sx = (cx / cz * f * 0.5 + 0.5) * size; sy = (0.5 - cy / cz * f * 0.5) * size
        fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0]); fn /= np.linalg.norm(fn, axis=1, keepdims=True) + 1e-12
        if N is not None and (F[:, :, 2] >= 0).all():
            NN = N[F[:, :, 2]]
        else:
            NN = np.repeat(fn[:, None], 3, 1)
        UV = T[np.clip(F[:, :, 1], 0, None)]
        for i in range(len(F)):
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
            z = 1.0 / (w0 / cz[i, 0] + w1 / cz[i, 1] + w2 / cz[i, 2])
            sub = zb[y0:y1 + 1, x0:x1 + 1]
            mask = inside & (z < sub)
            if not mask.any(): continue
            n = w0[..., None] * NN[i, 0] + w1[..., None] * NN[i, 1] + w2[..., None] * NN[i, 2]
            n /= np.linalg.norm(n, axis=-1, keepdims=True) + 1e-12
            # two-sided lighting
            vd = -fwd
            flip = (n @ vd) < 0
            n[flip] *= -1
            col = np.broadcast_to(kd, n.shape).copy()
            a = np.ones(n.shape[:2]) * alpha
            if tex is not None:
                u = w0 * UV[i, 0, 0] + w1 * UV[i, 1, 0] + w2 * UV[i, 2, 0]
                v = w0 * UV[i, 0, 1] + w1 * UV[i, 1, 1] + w2 * UV[i, 2, 1]
                th, tw = tex.shape[:2]
                tx = (np.mod(u, 1) * tw).astype(int).clip(0, tw - 1); ty = ((1 - np.mod(v, 1)) * th).astype(int).clip(0, th - 1)
                s = tex[ty, tx]
                col = col * s[..., :3]; a = a * s[..., 3]
                mask = mask & (s[..., 3] > 0.4) if alpha >= 0.99 else mask
            lam = np.clip(n @ light, 0, 1) * 0.75 + 0.35 + 0.15 * np.clip(n @ np.array([-0.5, 0.2, -0.6]), 0, 1)
            shaded = col * lam[..., None]
            simg = img[y0:y1 + 1, x0:x1 + 1]
            if alpha < 0.99:
                simg[mask] = simg[mask] * (1 - a[mask][:, None]) + shaded[mask] * a[mask][:, None]
            else:
                simg[mask] = shaded[mask]
                sub[mask] = z[mask]
    Image.fromarray((np.clip(img, 0, 1) ** (1 / 1.0) * 255).astype(np.uint8)).save(out)

if __name__ == '__main__':
    a = sys.argv
    render(a[2], a[1], float(a[3]) if len(a) > 3 else 35, float(a[4]) if len(a) > 4 else 20, int(a[5]) if len(a) > 5 else 640)
