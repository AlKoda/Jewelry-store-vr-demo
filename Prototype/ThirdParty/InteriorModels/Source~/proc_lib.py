"""Shared helpers for the project-made InteriorModels (procedural geometry + procedural textures).
Builds on meshkit.py (adapted from ForensicModels/Source~/meshkit.py, project-owned). Units: metres, Y up, front +Z."""
import os, sys, json, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from meshkit import *  # noqa

OUT = os.environ.get('PACK_OUT', '')
REPORT = os.environ.get('PACK_REPORT', '')
FONTS = {'sans': '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 'sans-bold': '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',
         'mono': '/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf', 'mono-bold': '/usr/share/fonts/truetype/dejavu/DejaVuSansMono-Bold.ttf',
         'cond': '/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf', 'cond-bold': '/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf'}
def F(name, size): return ImageFont.truetype(FONTS.get(name, name), size)

# ------------------------------------------------------------------ texture primitives
def fbm(h, w, scale, seed, octaves=4, persistence=0.5):
    return noise(h, w, scale, seed, octaves, persistence)

def tileable_noise(px, cells, seed, octaves=4, persistence=0.5):
    """Seamless value noise: random lattice wrapped, bicubic upsample of a 3x tiled grid then cropped."""
    rng = np.random.default_rng(seed); acc = np.zeros((px, px), np.float32); amp = 1.0; tot = 0
    for o in range(octaves):
        n = max(2, int(cells * 2 ** o))
        g = rng.random((n, n)).astype(np.float32)
        big = np.tile(g, (3, 3))
        im = Image.fromarray((big * 255).astype(np.uint8)).resize((px * 3, px * 3), Image.BICUBIC)
        acc += np.asarray(im, np.float32)[px:2 * px, px:2 * px] / 255 * amp; tot += amp; amp *= persistence
    return acc / tot

def blur(a, r):
    from scipy.ndimage import gaussian_filter
    return gaussian_filter(np.asarray(a, np.float32), r, mode='wrap')

def nmap(height, strength):
    return normal_from_height(height, strength)

def save_set(prefix, color, height=None, nstrength=2.0, metal=0.0, smooth=0.5, cpx=None, npx=None, mpx=None, png=False):
    """Write <prefix>_Color.jpg, <prefix>_Normal.jpg, <prefix>_MetallicSmoothness.png; return dict of file names."""
    out = {}
    c = np.clip(np.asarray(color, np.float32), 0, 1)
    im = Image.fromarray((c * 255).astype(np.uint8))
    if cpx: im = im.resize((cpx, cpx) if isinstance(cpx, int) else cpx, Image.LANCZOS)
    fn = prefix + ('_Color.png' if png else '_Color.jpg')
    (im.save(os.path.join(OUT, fn), optimize=True) if png else im.convert('RGB').save(os.path.join(OUT, fn), quality=88, optimize=True))
    out['Color'] = fn
    if height is not None:
        n = Image.fromarray(nmap(height, nstrength))
        if npx: n = n.resize((npx, npx) if isinstance(npx, int) else npx, Image.LANCZOS)
        fn = prefix + '_Normal.jpg'; n.save(os.path.join(OUT, fn), quality=90, optimize=True); out['Normal'] = fn
    if not (np.isscalar(metal) and np.isscalar(smooth)) or mpx:
        shp = c.shape[:2]
        m = np.broadcast_to(np.asarray(metal, np.float32), shp); s = np.broadcast_to(np.asarray(smooth, np.float32), shp)
        m8 = (np.clip(m, 0, 1) * 255).astype(np.uint8); s8 = (np.clip(s, 0, 1) * 255).astype(np.uint8)
        ms = Image.fromarray(np.dstack([m8, m8, m8, s8]), 'RGBA')
        if mpx: ms = ms.resize((mpx, mpx) if isinstance(mpx, int) else mpx, Image.LANCZOS)
        fn = prefix + '_MetallicSmoothness.png'; ms.save(os.path.join(OUT, fn), optimize=True); out['MetallicSmoothness'] = fn
    else:
        out['metallic'] = float(metal); out['smoothness'] = float(smooth)
    return out

# ------------------------------------------------------------------ OBJ writer (compact, welded)
def _fmt(x, d):
    s = ('%.' + str(d) + 'f') % x
    s = s.rstrip('0').rstrip('.') if '.' in s else s
    return '0' if s in ('-0', '') else s

def write_model(name, mesh, materials, info, pivot='floor'):
    """materials: {mat: {'Kd':(r,g,b), 'd':1, 'map': file or None, 'maps': {...}, 'metallic':.., 'smoothness':.., 'emission':..}}"""
    lo, hi = mesh.bounds()
    if pivot == 'floor': off = np.array([(lo[0] + hi[0]) / 2, lo[1], (lo[2] + hi[2]) / 2])
    elif pivot == 'wall': off = np.array([(lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, lo[2]])
    elif pivot == 'ceiling': off = np.array([(lo[0] + hi[0]) / 2, hi[1], (lo[2] + hi[2]) / 2])
    else: off = np.zeros(3)
    V, T, Nn = {}, {}, {}; vl, tl, nl = [], [], []
    def idx(tab, lst, key):
        i = tab.get(key)
        if i is None: i = len(lst) + 1; tab[key] = i; lst.append(key)
        return i
    body = []; tris = 0
    for mat, (P, UV, N) in mesh.parts.items():
        P = P - off
        body.append('g ' + mat); body.append('usemtl ' + mat)
        Pr = np.round(P, 4) + 0.0; Ur = np.round(UV, 4) + 0.0
        Nr = N / (np.linalg.norm(N, axis=2, keepdims=True) + 1e-12); Nr = np.round(Nr, 3) + 0.0
        for f in range(len(P)):
            cs = ['%d/%d/%d' % (idx(V, vl, tuple(Pr[f, c])), idx(T, tl, tuple(Ur[f, c])), idx(Nn, nl, tuple(Nr[f, c]))) for c in range(3)]
            body.append('f ' + ' '.join(cs)); tris += 1
    lines = ['# %s - InteriorModels pack, project-made (procedural). Units: metres, Y up, front +Z.' % name, 'mtllib %s.mtl' % name]
    lines += ['v %s %s %s' % tuple(_fmt(c, 4) for c in v) for v in vl]
    lines += ['vt %s %s' % tuple(_fmt(c, 4) for c in t) for t in tl]
    lines += ['vn %s %s %s' % tuple(_fmt(c, 3) for c in n) for n in nl]
    open(os.path.join(OUT, name + '.obj'), 'w').write('\n'.join(lines + body) + '\n')
    mtl = []; minfo = {}
    for mat in mesh.parts:
        m = materials.get(mat, {})
        kd = m.get('Kd', (1, 1, 1)); d = m.get('d', 1.0)
        s = 'newmtl %s\nKd %.3f %.3f %.3f\nKs %.3f %.3f %.3f\nd %s' % (mat, kd[0], kd[1], kd[2], m.get('Ks', 0.05), m.get('Ks', 0.05), m.get('Ks', 0.05), ('%.2f' % d) if d < 1 else '1.0')
        if m.get('Ns'): s += '\nNs %d' % m['Ns']
        maps = dict(m.get('maps', {}))
        if maps.get('Color'): s += '\nmap_Kd ' + maps['Color']
        for k in ('Normal', 'MetallicSmoothness'):
            if maps.get(k): s += '\n# %s %s' % (k, maps[k])
        mtl.append(s)
        e = {k: v for k, v in maps.items()}
        for k in ('metallic', 'smoothness', 'emission', 'glass', 'note', 'tiling', 'normal_scale', 'cutout'):
            if k in m: e[k] = m[k]
        if d < 1: e['glass'] = True; e['Kd'] = list(kd); e['d'] = d
        if not maps.get('Color'): e['Kd'] = [round(x, 3) for x in kd]
        minfo[mat] = e
    open(os.path.join(OUT, name + '.mtl'), 'w').write('\n\n'.join(mtl) + '\n')
    allp = np.vstack([p[0].reshape(-1, 3) for p in mesh.parts.values()]) - off
    rep = {'tris': tris, 'size_m': np.round(allp.max(0) - allp.min(0), 3).tolist(), 'min': np.round(allp.min(0), 3).tolist(),
           'max': np.round(allp.max(0), 3).tolist(), 'pivot': pivot, 'materials': minfo}
    rep.update(info)
    if REPORT:
        r = json.load(open(REPORT)) if os.path.exists(REPORT) else {}
        r[name] = rep; json.dump(r, open(REPORT, 'w'), indent=1)
    print(name, tris, 'tris', rep['size_m'])
    return rep

# ------------------------------------------------------------------ geometry helpers
def add(mesh, mat, geo, M=None):
    P, UV, N = geo
    if N is None: N = flat_normals(P)
    if M is not None: P, N = xform(P, N, M)
    mesh.add(mat, P, UV, N)

def box_uv_world(P, N, scale):
    """Planar box projection by dominant normal axis -> UV in metres/scale (for tiling materials)."""
    n = np.abs(N.mean(1)) if N.ndim == 3 else np.abs(N)
    ax = np.argmax(n, axis=1)
    UV = np.zeros(P.shape[:2] + (2,))
    for a, (u, v) in enumerate(((2, 1), (0, 2), (0, 1))):
        sel = ax == a
        UV[sel, :, 0] = P[sel][:, :, u] / scale; UV[sel, :, 1] = P[sel][:, :, v] / scale
    return UV

def retile(mesh, mat, scale):
    P, UV, N = mesh.parts[mat]
    mesh.parts[mat][1] = box_uv_world(P, N, scale)

def cyl(r, h, segs=24, y0=0.0, caps=True, uv=(0, 0, 1, 1), r_top=None, smooth=True):
    rt = r if r_top is None else r_top
    P, UV, N = lathe([(r, y0), (rt, y0 + h)], segs, uv=uv, smooth=smooth)
    parts = [(P, UV, N)]
    if caps:
        parts.append(disk(rt, y0 + h, segs, up=True)); parts.append(disk(r, y0, segs, up=False))
    return tuple(np.vstack([p[i] for p in parts]) for i in range(3))

def rbox(size, r, center=(0, 0, 0), seg=2, uv_rects=None):
    return rounded_box(size, r, seg=seg, uv_rects=uv_rects, center=center)

def quad(p0, p1, p2, p3, uv=(0, 0, 1, 1), n=None):
    """Quad p0..p3 counter-clockwise seen from the front; uv rect u0,v0 (p0) to u1,v1 (p2)."""
    u0, v0, u1, v1 = uv
    P = np.array([[p0, p1, p2], [p0, p2, p3]], float)
    UV = np.array([[(u0, v0), (u1, v0), (u1, v1)], [(u0, v0), (u1, v1), (u0, v1)]], float)
    N = flat_normals(P) if n is None else np.tile(np.array(n, float), (2, 3, 1))
    return P, UV, N

def merge(*geos):
    return tuple(np.vstack([g[i] for g in geos]) for i in range(3))

def rect_uv(geo, rect):
    """Remap a geometry's UVs (0..1) into an atlas rect (u0,v0,u1,v1)."""
    P, UV, N = geo; u0, v0, u1, v1 = rect
    return P, np.stack([u0 + (u1 - u0) * UV[..., 0], v0 + (v1 - v0) * UV[..., 1]], -1), N
