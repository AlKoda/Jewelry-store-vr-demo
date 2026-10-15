"""LOD contact sheet: python3 -I render_lods.py PACKDIR OUT.jpg [--size 300]
From the repository root: python3 -I Prototype/ThirdParty/JewelryModels/Source~/render_lods.py
Prototype/ThirdParty/JewelryModels Prototype/ThirdParty/JewelryModels/Source~/Preview_LODs.jpg
One row per model with LOD0 | LOD1 | LOD2 side by side, each in two views: the whole model, and a close-up (a second
view rendered CLOSE[0] times larger and cropped around a model point: the coin rim, the band, the box seam, the loupe
barrel and lens, the stones on the paper, the clasp end of the strand). Rendered with Source~/render.py's rasterizer
(unchanged), but each OBJ is read here per corner, every (v, vt, vn) its own vertex as Unity's importer splits them,
with render.py's material rules (MTL Kd and map_Kd, metal by material name): trimesh's loader, which render.py uses,
merged the closed ring box's seam corners once the box had no enclosed faces left, and hid its seam. Faceted stones
and paper show flat, metal smooth. Triangles facing away from the view are dropped first, as Unity's Standard shader
culls back faces (render.py alone draws both sides, which shows e.g. the gem paper's downward bottom sheet through the
top sheet)."""
import sys, os, site, argparse
try:
    import trimesh  # noqa: F401  (render.py imports it)
except ImportError:
    sys.path.append(site.getusersitepackages())   # python3 -I hides it; add only this directory
import numpy as np
from PIL import Image, ImageDraw
sys.dont_write_bytecode = True                    # no __pycache__ next to the pack's scripts
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render as RD   # Source~/render.py

MODELS = ['GoldCoin', 'RingGoldBand', 'RingBoxClosed', 'JewelersLoupe', 'GemParcel', 'NecklacePearlDropped']
VIEWS = {   # (yaw, pitch) of the whole-model view and of the close-up (radians, as in render.py)
    'GoldCoin': ((0.8, 0.5), (0.6, 0.25)),
    'RingGoldBand': ((1.1, 0.35), (1.35, 0.15)),
    'RingBoxClosed': ((0.8, 0.45), (0.5, 0.25)),
    'JewelersLoupe': ((0.8, 0.6), (0.5, 0.9)),
    'GemParcel': ((0.6, 0.7), (0.5, 0.6)),
    'NecklacePearlDropped': ((0.0, 1.45), (0.4, 0.8)),
}
CLOSE = {   # close-up: zoom factor and the model point (metres) at its centre
    'GoldCoin': (2.6, (0.011, 0.0029, 0.011)),
    'RingGoldBand': (2.2, (0.0, 0.002, 0.0)),
    'RingBoxClosed': (2.2, (0.016, 0.026, 0.024)),
    'JewelersLoupe': (2.2, (-0.0208, 0.011, -0.0037)),
    'GemParcel': (3.0, (0.0, 0.002, -0.004)),
    'NecklacePearlDropped': (5.0, (0.104, 0.004, -0.024)),
}


def load_exact(path):
    """parts per usemtl as render.py's load_parts makes them, but one vertex per face corner (exact v/vt/vn)"""
    V, T, N, G, mtl, lib = [], [], [], {}, None, None
    for line in open(path, encoding='utf-8'):
        p = line.split()
        if not p:
            continue
        if p[0] == 'v': V.append([float(x) for x in p[1:4]])
        elif p[0] == 'vt': T.append([float(x) for x in p[1:3]])
        elif p[0] == 'vn': N.append([float(x) for x in p[1:4]])
        elif p[0] == 'mtllib': lib = line.split(None, 1)[1].strip()
        elif p[0] == 'usemtl': mtl = p[1]; G.setdefault(mtl, [])
        elif p[0] == 'f':
            cs = [[int(i) - 1 if i else -1 for i in (c.split('/') + ['', ''])[:3]] for c in p[1:]]
            G[mtl] += [[cs[0], cs[j], cs[j + 1]] for j in range(1, len(cs) - 1)]
    mats, cur = {}, None
    for line in open(os.path.join(os.path.dirname(path), lib), encoding='utf-8'):
        p = line.split()
        if p and p[0] == 'newmtl':
            cur = mats[p[1]] = {}
        elif p and cur is not None and p[0] in ('Kd', 'map_Kd'):
            cur[p[0]] = p[1:]
    V, T, N = np.array(V), np.array(T) if T else None, np.array(N)
    parts = []
    for name, F in G.items():
        F = np.array(F)                                       # (tris, 3 corners, v/vt/vn)
        m = mats.get(name, {})
        col = np.array([float(x) for x in m.get('Kd', [0.8, 0.8, 0.8])] + [1.0])
        col[:3] = col[:3] ** 2.2
        tex = uv = None
        if 'map_Kd' in m and T is not None and (F[:, :, 1] >= 0).all():
            im = Image.open(os.path.join(os.path.dirname(path), m['map_Kd'][0])).convert('RGBA')
            if max(im.size) > 512:
                im = im.resize((min(512, im.size[0]), min(512, im.size[1])))
            tex = np.asarray(im).astype(np.float32) / 255.0; tex[..., :3] = tex[..., :3] ** 2.2
            uv = T[F[:, :, 1]].reshape(-1, 2)
        metal = 1.0 if any(k in name.lower() for k in ('gold', 'silver', 'metal', 'platinum', 'chrome', 'steel')) else 0.0
        parts.append(dict(v=V[F[:, :, 0]].reshape(-1, 3), f=np.arange(3 * len(F)).reshape(-1, 3),
                          n=N[F[:, :, 2]].reshape(-1, 3), uv=uv, tex=tex, col=col, metal=metal, rough=0.5))
    return parts


def cull(parts, yaw, pitch):
    """drop the triangles facing away from the (orthographic) view, as Unity's Standard shader culls back faces"""
    d = np.array([np.sin(yaw) * np.cos(pitch), np.sin(pitch), np.cos(yaw) * np.cos(pitch)])
    out = []
    for p in parts:
        v = p['v'][p['f']]
        keep = np.cross(v[:, 1] - v[:, 0], v[:, 2] - v[:, 0]) @ d > 0
        q = dict(p); q['f'] = p['f'][keep]
        out.append(q)
    return out


def view(parts, size, yaw, pitch, z=1.0, focus=None):
    """render.render of the front faces, z times larger and cropped to size x size around focus (a model point)"""
    big = RD.render(cull(parts, yaw, pitch), int(size * z), yaw, pitch)
    if focus is None:
        return big
    allv = np.vstack([p['v'] for p in parts]); lo, hi = allv.min(0), allv.max(0)
    c = (lo + hi) / 2; rad = np.linalg.norm(hi - lo) / 2 + 1e-9          # render.render's framing
    d = np.array([np.sin(yaw) * np.cos(pitch), np.sin(pitch), np.cos(yaw) * np.cos(pitch)])
    v = (np.asarray(focus, float) - c) @ RD.look(d * 3 * rad, np.zeros(3)).T
    B = int(size * z)
    cx, cy = int((v[0] / rad * 0.92 + 1) * B / 2), int((1 - v[1] / rad * 0.92) * B / 2)
    x0 = min(max(cx - size // 2, 0), B - size); y0 = min(max(cy - size // 2, 0), B - size)
    return big[y0:y0 + size, x0:x0 + size]


def tile(img, text):
    im = Image.fromarray((img * 255).astype(np.uint8))
    d = ImageDraw.Draw(im); d.rectangle([0, 0, im.size[0], 13], fill=(0, 0, 0)); d.text((3, 1), text, fill=(255, 255, 0))
    return im


def main():
    ap = argparse.ArgumentParser(); ap.add_argument('pack'); ap.add_argument('out'); ap.add_argument('--size', type=int, default=300)
    a = ap.parse_args()
    S = a.size
    rows = []
    for name in MODELS:
        row = Image.new('RGB', (S * 6, S))
        for li, suf in enumerate(('', '_LOD1', '_LOD2')):
            parts = load_exact(os.path.join(a.pack, name + suf + '.obj'))
            tris = sum(len(p['f']) for p in parts)
            (y0, p0), (y1, p1) = VIEWS[name]
            z, focus = CLOSE[name]
            row.paste(tile(view(parts, S, y0, p0), '%s%s  %d tris' % (name, suf or ' LOD0', tris)), (li * 2 * S, 0))
            row.paste(tile(view(parts, S, y1, p1, z, focus), 'close-up x%.1f %s' % (z, suf[1:] or 'LOD0')), (li * 2 * S + S, 0))
            print(name + suf, tris, flush=True)
        rows.append(row)
    sheet = Image.new('RGB', (S * 6, S * len(rows)))
    for i, r in enumerate(rows):
        sheet.paste(r, (0, i * S))
    sheet.save(a.out, quality=88)
    print('wrote', a.out, sheet.size)


if __name__ == '__main__':
    main()
