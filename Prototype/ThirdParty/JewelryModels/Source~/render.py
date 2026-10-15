"""Tiny numpy rasterizer for previews. render.py OUT.png MODEL [MODEL...] [--size 320] [--label text]
MODEL may be .gltf/.glb/.obj. Renders 3 views per model (front, 3/4 from above, top) in one row; models stacked."""
import sys, os, argparse
import numpy as np, trimesh
from PIL import Image, ImageDraw

def env(r):
    # fake studio environment: bright soft top, dark floor, two side softboxes
    y = r[..., 1]; x = r[..., 0]; z = r[..., 2]
    sky = np.clip(0.35 + 0.65 * y, 0, 1) ** 1.5
    box = np.exp(-((x - 0.8) ** 2 + (y - 0.3) ** 2) * 12) * 1.6 + np.exp(-((x + 0.7) ** 2 + (z - 0.4) ** 2) * 10) * 1.0
    return np.clip(sky * 0.9 + box, 0.03, 2.5)

def load_parts(path):
    sc = trimesh.load(path, force='scene')
    parts = []
    for g in sc.dump(concatenate=False):
        if not isinstance(g, trimesh.Trimesh) or len(g.faces) == 0: continue
        mat = getattr(g.visual, 'material', None)
        uv = getattr(g.visual, 'uv', None)
        tex = None; col = np.array([0.8, 0.8, 0.8, 1.0]); metal = 0.0; rough = 0.5; kind = 'pbr'
        if mat is not None:
            if hasattr(mat, 'baseColorFactor') and mat.baseColorFactor is not None:
                f = np.array(mat.baseColorFactor, float); col = f / 255.0 if f.max() > 1 else f
            elif hasattr(mat, 'diffuse') and mat.diffuse is not None:
                f = np.array(mat.diffuse, float); col = f / 255.0 if f.max() > 1 else f
            img = getattr(mat, 'baseColorTexture', None)
            if img is None: img = getattr(mat, 'image', None)
            if img is not None and uv is not None and len(uv) == len(g.vertices):
                im = img.convert('RGBA')
                if max(im.size) > 512: im = im.resize((min(512, im.size[0]), min(512, im.size[1])))
                tex = np.asarray(im).astype(np.float32) / 255.0
                tex[..., :3] = tex[..., :3] ** 2.2
            m = getattr(mat, 'metallicFactor', None); metal = float(m) if m is not None else 0.0
            r = getattr(mat, 'roughnessFactor', None); rough = float(r) if r is not None else 0.5
            name = (getattr(mat, 'name', '') or '').lower()
            if any(k in name for k in ('gold', 'silver', 'metal', 'platinum', 'chrome', 'steel')) and metal == 0: metal = 1.0
        col = np.array(col, float)
        if len(col) == 3: col = np.append(col, 1)
        col[:3] = col[:3] ** 2.2 if col[:3].max() <= 1 else col[:3]
        parts.append(dict(v=np.asarray(g.vertices, float), f=np.asarray(g.faces), n=np.asarray(g.vertex_normals, float),
                          uv=np.asarray(uv, float) if (tex is not None) else None, tex=tex, col=col, metal=metal, rough=rough))
    return parts

def look(eye, target=np.zeros(3), up=np.array([0, 1, 0.0])):
    f = target - eye; f /= np.linalg.norm(f)
    r = np.cross(f, up); 
    if np.linalg.norm(r) < 1e-6: r = np.array([1, 0, 0.0])
    r /= np.linalg.norm(r); u = np.cross(r, f)
    return np.stack([r, u, -f])

def render(parts, size, yaw, pitch):
    allv = np.vstack([p['v'] for p in parts]); lo, hi = allv.min(0), allv.max(0)
    c = (lo + hi) / 2; rad = np.linalg.norm(hi - lo) / 2 + 1e-9
    d = np.array([np.sin(yaw) * np.cos(pitch), np.sin(pitch), np.cos(yaw) * np.cos(pitch)])
    R = look(d * 3 * rad, np.zeros(3))
    img = np.zeros((size, size, 3)); img[:] = np.array([0.20, 0.21, 0.23])
    zb = np.full((size, size), np.inf)
    L = np.array([0.4, 0.8, 0.5]); L /= np.linalg.norm(L)
    for p in parts:
        v = (p['v'] - c) @ R.T; n = p['n'] @ R.T
        sx = (v[:, 0] / rad * 0.92 + 1) * size / 2; sy = (1 - v[:, 1] / rad * 0.92) * size / 2; z = -v[:, 2]
        F = p['f']
        if len(F) > 250000:
            F = F[np.random.default_rng(0).choice(len(F), 250000, replace=False)]
        X = sx[F]; Y = sy[F]; Z = z[F]
        x0 = np.clip(np.floor(X.min(1)), 0, size - 1).astype(int); x1 = np.clip(np.ceil(X.max(1)), 0, size - 1).astype(int)
        y0 = np.clip(np.floor(Y.min(1)), 0, size - 1).astype(int); y1 = np.clip(np.ceil(Y.max(1)), 0, size - 1).astype(int)
        Rinv = R.T  # view->world for normals
        for t in range(len(F)):
            if x1[t] < x0[t] or y1[t] < y0[t]: continue
            xs = np.arange(x0[t], x1[t] + 1) + 0.5; ys = np.arange(y0[t], y1[t] + 1) + 0.5
            gx, gy = np.meshgrid(xs, ys)
            (ax, bx, cx), (ay, by, cy) = X[t], Y[t]
            den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
            if abs(den) < 1e-12: continue
            w0 = ((by - cy) * (gx - cx) + (cx - bx) * (gy - cy)) / den
            w1 = ((cy - ay) * (gx - cx) + (ax - cx) * (gy - cy)) / den
            w2 = 1 - w0 - w1
            m = (w0 >= -1e-4) & (w1 >= -1e-4) & (w2 >= -1e-4)
            if not m.any(): continue
            zz = w0 * Z[t, 0] + w1 * Z[t, 1] + w2 * Z[t, 2]
            sub = zb[y0[t]:y1[t] + 1, x0[t]:x1[t] + 1]
            m &= zz < sub
            if not m.any(): continue
            sub[m] = zz[m]
            i0, i1, i2 = F[t]
            nn = (w0[m, None] * n[i0] + w1[m, None] * n[i1] + w2[m, None] * n[i2])
            nn /= np.linalg.norm(nn, axis=1, keepdims=True) + 1e-9
            if p['tex'] is not None:
                uv = w0[m, None] * p['uv'][i0] + w1[m, None] * p['uv'][i1] + w2[m, None] * p['uv'][i2]
                th, tw = p['tex'].shape[:2]
                tx = (np.mod(uv[:, 0], 1) * (tw - 1)).astype(int); ty = ((1 - np.mod(uv[:, 1], 1)) * (th - 1)).astype(int)
                base = p['tex'][ty, tx, :3] * p['col'][:3]
            else:
                base = np.broadcast_to(p['col'][:3], (m.sum(), 3))
            nw = nn @ R  # normal in world
            vdir = np.array([0, 0, 1.0]) @ R  # view dir (towards camera) in world
            ndl = np.clip(nw @ L, 0, 1)[:, None]
            rr = 2 * (nw @ vdir)[:, None] * nw - vdir
            e = env(rr)[:, None]
            h = L + vdir; h /= np.linalg.norm(h)
            spec = np.clip(nw @ h, 0, 1)[:, None] ** (8 + 200 * (1 - p['rough']) ** 2)
            met = p['metal']
            diff = base * (0.18 + 0.82 * ndl)
            metl = base * (0.15 + 0.85 * e) + spec * base * 1.5
            colr = diff * (1 - met) + metl * met + spec * 0.25 * (1 - met)
            sub2 = img[y0[t]:y1[t] + 1, x0[t]:x1[t] + 1]
            sub2[m] = colr
    return np.clip(img, 0, 1) ** (1 / 2.2)

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('out'); ap.add_argument('models', nargs='+'); ap.add_argument('--size', type=int, default=300)
    a = ap.parse_args()
    rows = []
    for mp in a.models:
        try:
            parts = load_parts(mp)
            allv = np.vstack([p['v'] for p in parts]); ext = allv.max(0) - allv.min(0)
            tris = sum(len(p['f']) for p in parts)
            views = [render(parts, a.size, 0.0, 0.05), render(parts, a.size, 0.8, 0.5), render(parts, a.size, 0.0, 1.45)]
            row = Image.fromarray((np.hstack(views) * 255).astype(np.uint8))
            label = '%s  ext=%.4f x %.4f x %.4f  tris=%d' % (os.path.basename(os.path.dirname(mp)) + '/' + os.path.basename(mp), ext[0], ext[1], ext[2], tris)
        except Exception as e:
            row = Image.new('RGB', (a.size * 3, a.size), (60, 0, 0)); label = mp + ' ERR ' + str(e)[:80]
        dr = ImageDraw.Draw(row); dr.rectangle([0, 0, row.size[0], 14], fill=(0, 0, 0)); dr.text((3, 1), label, fill=(255, 255, 0))
        rows.append(row)
        print(label, flush=True)
    W = max(r.size[0] for r in rows); H = sum(r.size[1] for r in rows)
    sheet = Image.new('RGB', (W, H)); y = 0
    for r in rows: sheet.paste(r, (0, y)); y += r.size[1]
    sheet.save(a.out)

if __name__ == '__main__':
    main()
