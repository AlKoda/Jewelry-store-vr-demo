"""CSI consumables and evidence items (October 24, 2026): MagneticWand, GelLifterBlack/White, BackingCardWhite/Black,
LiftCardWithPrint, HingeLifterBlack/White, SwabTube, SwabOpen, SwabBox, DistilledWaterAmpoule, KMTestKit, MikrosilTube,
MikrosilCatalyst, MixingSpatula, ToolMarkCast, MixingBowlRubber, CastingFrame, EvidenceTag, EvidenceLabelRoll,
BiohazardBinSmall, SteppingPlate, OrangeGoggles, FaceMask, ShoeCoverPair, HairFibreSpecimen, FibreTuft,
ElectrostaticLiftFilm, plus the FootwearDustMark and PryMark decal textures.

Project-made geometry and textures (numpy + Pillow, meshkit.py alongside). Dimensions follow the research notes
(research_notes/Crime scene forensic procedures/equipment_training.md section 1 and fingerprints.md sections 3, 5, 8):
hinge lifters 2 x 4 in, backing cards 3 x 5 in, BVDA gel lifter 13 x 18 cm (0.5-0.6 mm gel on 0.10 mm PET), Mikrosil
7 oz, stepping plate 350 x 350 mm, Sirchie casting frame 7.25 x 18 in. Printed text is fictional and brand-free; the
small Arabic lines use the project's Noto Sans Arabic (Prototype/ThirdParty/Fonts/Arabic) shaped by Pillow/raqm and are
skipped when that font or raqm is missing. The ridge art on LiftCardWithPrint is a cell of the project's own
SurfaceDecals/Fingerprints_Atlas.png (synthetic, no real person).

Usage: python3 -I proc_csi.py OUT_DIR   (then assemble_csi.py OUT_DIR PACK_DIR)
Units: metres, Y up, base on y = 0, centred in x/z. UV convention as the rest of the pack (meshkit.box faces, lathe u
around): readable after Unity's OBJ x-flip.
"""
import sys, os, math, json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont, features
import meshkit as mk

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
HERE = os.path.dirname(os.path.abspath(__file__))
FONT_AR = os.path.normpath(os.path.join(HERE, '..', '..', 'Fonts', 'Arabic', 'NotoSansArabic.ttf'))
AR_OK = os.path.exists(FONT_AR) and features.check('raqm')
FP_ATLAS = os.path.normpath(os.path.join(HERE, '..', '..', 'SurfaceDecals', 'Fingerprints_Atlas.png'))
STATS = {}
INCH = 0.0254

# =========================================================================================== helpers
def img(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))


def arr(im):
    return np.asarray(im, np.float32) / 255


def blur(im_or_a, r):
    if isinstance(im_or_a, Image.Image):
        return im_or_a.filter(ImageFilter.GaussianBlur(r))
    a = np.asarray(im_or_a, np.float32)
    if a.ndim == 3:
        return np.dstack([blur(a[..., k], r) for k in range(a.shape[2])])
    # separable Gaussian with reflected edges (float arrays; PIL's blur does not take mode F everywhere)
    k = max(1, int(3 * r)); x = np.arange(-k, k + 1); g = np.exp(-0.5 * (x / r) ** 2); g /= g.sum()
    p = np.pad(a, ((k, k), (k, k)), mode='reflect')
    p = np.apply_along_axis(lambda row: np.convolve(row, g, mode='same'), 1, p)
    p = np.apply_along_axis(lambda col: np.convolve(col, g, mode='same'), 0, p)
    return p[k:-k, k:-k].astype(np.float32)


def R(x0, y0, x1, y1, W=1024, H=1024):
    """Pixel rectangle (rows down) -> (u0, v0, u1, v1) with OBJ v up."""
    return (x0 / W, 1 - y1 / H, x1 / W, 1 - y0 / H)


def mid(region):
    u0, v0, u1, v1 = region
    return ((u0 + u1) / 2, (v0 + v1) / 2)


def const_uv(UV, region):
    UV = np.array(UV, float); UV[..., 0], UV[..., 1] = mid(region); return UV


def grain(h, w, base, seed, fibre=0.03, blotch=0.04):
    n1 = mk.noise(h, w, 40, seed, 5) - 0.5
    n2 = mk.noise(h, w, 4, seed + 1, 2) - 0.5
    out = np.ones((h, w, 3), np.float32) * np.array(base, np.float32)
    return out + (n1 * blotch + n2 * fibre)[..., None]


def font(name, size):
    return mk.font(name, size)


def ar(d, xy, text, size, fill, anchor='rm'):
    """One short Arabic line in the project's Noto Sans Arabic (needs raqm for shaping); skipped if unavailable."""
    if not AR_OK:
        return
    f = ImageFont.truetype(FONT_AR, size)
    d.text(xy, text, font=f, fill=fill, anchor=anchor, direction='rtl', language='ar')


def save_maps(name, col, height=None, strength=2.0, metal=None, smooth=None, n_px=None, ms_px=256, q=90, png=False):
    """Writes <name>_Color.jpg|png, optional <name>_Normal.jpg and <name>_MetallicSmoothness.png into OUT."""
    if isinstance(col, np.ndarray):
        col = img(col)
    if png:
        col.save(os.path.join(OUT, name + '_Color.png'), optimize=True)
    else:
        col.convert('RGB').save(os.path.join(OUT, name + '_Color.jpg'), quality=q, optimize=True)
    if height is not None:
        nm = Image.fromarray(mk.normal_from_height(np.asarray(height, np.float32), strength))
        if n_px and max(nm.size) > n_px:
            nm = nm.resize((n_px, n_px * nm.size[1] // nm.size[0]), Image.LANCZOS)
        nm.save(os.path.join(OUT, name + '_Normal.jpg'), quality=92, optimize=True)
    if smooth is not None:
        s = np.asarray(smooth, np.float32)
        if s.ndim == 0:
            s = np.full((ms_px, ms_px), float(s), np.float32)
        H, W = s.shape
        m = np.zeros_like(s) if metal is None else (np.full_like(s, float(metal)) if np.ndim(metal) == 0 else np.asarray(metal, np.float32))
        si = Image.fromarray((np.clip(s, 0, 1) * 255).astype(np.uint8)).resize((ms_px, ms_px * H // W), Image.LANCZOS)
        mi = Image.fromarray((np.clip(m, 0, 1) * 255).astype(np.uint8)).resize((ms_px, ms_px * H // W), Image.LANCZOS)
        mk.save_ms(os.path.join(OUT, name + '_MetallicSmoothness.png'), arr(mi), arr(si))


def finish(name, m, mats, real_size, note=''):
    """Ground, write OBJ/MTL, record stats (size in m, tris)."""
    m.ground()
    t = m.write(os.path.join(OUT, name + '.obj'), mats)
    lo, hi = m.bounds()
    size = [float(round(v, 4)) for v in (hi - lo)]
    maps = sorted({v['map'] for v in mats.values() if v.get('map')})
    STATS[name] = {'kind': 'model', 'tris': int(t), 'size_m': size, 'min_y': float(round(lo[1], 5)),
                   'materials': list(m.parts.keys()), 'maps': maps, 'nominal_size_m': list(real_size), 'note': note}
    print('%-22s %6d tris  %s' % (name, t, np.round(hi - lo, 4)))


def orient(P, UV, ref):
    """Flip triangles whose face normal points against ref (a vector or per-face array)."""
    P = np.array(P); UV = np.array(UV)
    fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
    ref = np.asarray(ref, float)
    dots = (fn * ref).sum(1) if ref.ndim == 2 else fn @ ref
    flip = dots < 0
    P[flip] = P[flip, ::-1]; UV[flip] = UV[flip, ::-1]
    return P, UV


def slab(W, D, nx, nz, top, bottom=None, uv_top=(0, 0, 1, 1), uv_bot=None, uv_side=None, thickness=0.001):
    """Thin plate between two height fields y = top(x, z) and y = bottom(x, z) (default top - thickness),
    with closed sides. Top UV: u -> +x, v -> -z; bottom: u -> +x, v -> +z (same as meshkit.box)."""
    xs = np.linspace(-W / 2, W / 2, nx + 1); zs = np.linspace(-D / 2, D / 2, nz + 1)
    X, Z = np.meshgrid(xs, zs)
    Yt = top(X, Z); Yb = bottom(X, Z) if bottom else Yt - thickness
    Pt = np.stack([X, Yt, Z], -1); Pb = np.stack([X, Yb, Z], -1)
    u0, v0, u1, v1 = uv_top
    UVt = np.stack([u0 + (u1 - u0) * (X + W / 2) / W, v0 + (v1 - v0) * (D / 2 - Z) / D], -1)
    b0, c0, b1, c1 = uv_bot or uv_top
    UVb = np.stack([b0 + (b1 - b0) * (X + W / 2) / W, c0 + (c1 - c0) * (Z + D / 2) / D], -1)
    P1, U1, _ = mk.grid_surface(Pt, UVt); P1, U1 = orient(P1, U1, (0, 1, 0))
    P2, U2, _ = mk.grid_surface(Pb, UVb); P2, U2 = orient(P2, U2, (0, -1, 0))
    # sides: walk the boundary
    ring = [(0, j) for j in range(nx + 1)] + [(i, nx) for i in range(1, nz + 1)] + \
           [(nz, j) for j in range(nx - 1, -1, -1)] + [(i, 0) for i in range(nz - 1, 0, -1)]
    Ps, Us = [], []
    su = mid(uv_side or uv_top)
    for k in range(len(ring)):
        a = ring[k]; b = ring[(k + 1) % len(ring)]
        q = np.array([Pt[a], Pt[b], Pb[b], Pb[a]])
        c = q.mean(0); out = np.array([c[0], 0, c[2]])
        for tri in ((0, 1, 2), (0, 2, 3)):
            t = q[list(tri)]
            if np.dot(np.cross(t[1] - t[0], t[2] - t[0]), out) < 0:
                t = t[::-1]
            Ps.append(t); Us.append([su] * 3)
    P3 = np.array(Ps); U3 = np.array(Us)
    P = np.vstack([P1, P2, P3]); UV = np.vstack([U1, U2, U3])
    return P, UV, mk.smooth_normals(P, 40)


def loft(rings, uv=(0, 0, 1, 1), close=True, smooth=60, outward=True):
    """rings (nr, n, 3) -> side surface. u around, v along the rings (by index)."""
    G = np.asarray(rings, float)
    if close:
        G = np.concatenate([G, G[:, :1]], axis=1)
    nr, n = G.shape[:2]
    u0, v0, u1, v1 = uv
    U = np.linspace(u0, u1, n)[None].repeat(nr, 0); V = np.linspace(v0, v1, nr)[:, None].repeat(n, 1)
    P, UV, _ = mk.grid_surface(G, np.stack([U, V], -1))
    cen = G.reshape(-1, 3).mean(0)
    fc = P.mean(1); ref = fc - cen; ref[:, 1] = 0
    if not outward:
        ref = -ref
    P, UV = orient(P, UV, ref)
    return P, UV, mk.smooth_normals(P, smooth)


def rrect(w, d, r, nc=4, y=0.0):
    """Rounded rectangle in x/z (CCW seen from +Y), starting at the middle of the +z (front) edge."""
    pts = []
    cs = [(w / 2 - r, d / 2 - r, 0), (-w / 2 + r, d / 2 - r, 90), (-w / 2 + r, -d / 2 + r, 180), (w / 2 - r, -d / 2 + r, 270)]
    pts.append((0, y, d / 2))
    for cx, cz, a0 in cs:
        for k in range(nc + 1):
            a = math.radians(a0 + 90 * k / nc)
            pts.append((cx + r * math.cos(a), y, cz + r * math.sin(a)))
    return np.array(pts)


def cap_poly(ring_xz, y, up, uv_region):
    tris = mk.triangulate(ring_xz)
    P = []
    for a, b, c in tris:
        t = np.array([(ring_xz[k][0], y, ring_xz[k][1]) for k in (a, b, c)])
        if (np.cross(t[1] - t[0], t[2] - t[0])[1] < 0) == up:
            t = t[::-1]
        P.append(t)
    P = np.array(P); UV = np.full((len(P), 3, 2), 0.0); UV[..., 0], UV[..., 1] = mid(uv_region)
    N = np.zeros_like(P); N[..., 1] = 1 if up else -1
    return P, UV, N


def add_box(m, mat, size, center, region, M=None):
    P, UV, N = mk.box(size, uvs={k: region for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')})
    P = P + np.array(center)
    if M is not None:
        P, N = mk.xform(P, N, M)
    m.add(mat, P, UV, N)


def biohazard(d, cx, cy, s, fill):
    """Generic three-ring biohazard pictogram drawn from primitives (public-domain symbol, own drawing)."""
    mask = Image.new('L', d._image.size, 0); md = ImageDraw.Draw(mask)
    ro, ri = 0.46 * s, 0.30 * s; off = 0.27 * s
    for k in range(3):
        a = math.radians(90 + 120 * k)
        x, y = cx + off * math.cos(a), cy - off * math.sin(a)
        md.ellipse([x - ro, y - ro, x + ro, y + ro], fill=255)
    for k in range(3):
        a = math.radians(90 + 120 * k)
        x, y = cx + off * math.cos(a), cy - off * math.sin(a)
        md.ellipse([x - ri, y - ri, x + ri, y + ri], fill=0)
    # centre ring and the three gaps toward it
    md.ellipse([cx - 0.19 * s, cy - 0.19 * s, cx + 0.19 * s, cy + 0.19 * s], fill=255)
    md.ellipse([cx - 0.11 * s, cy - 0.11 * s, cx + 0.11 * s, cy + 0.11 * s], fill=0)
    for k in range(3):
        a = math.radians(90 + 120 * k)
        for t in np.linspace(0.12, 0.40, 12) * s:
            x, y = cx + t * math.cos(a), cy - t * math.sin(a)
            md.ellipse([x - 0.03 * s, y - 0.03 * s, x + 0.03 * s, y + 0.03 * s], fill=0)
        a2 = math.radians(270 + 120 * k)
        for t in np.linspace(0.58, 0.80, 8) * s:
            x, y = cx + t * math.cos(a2), cy - t * math.sin(a2)
            md.ellipse([x - 0.025 * s, y - 0.025 * s, x + 0.025 * s, y + 0.025 * s], fill=0)
    # small arcs ("feet") outside each ring
    for k in range(3):
        a = math.radians(90 + 120 * k)
        x, y = cx + off * math.cos(a), cy - off * math.sin(a)
        md.arc([x - 0.56 * s, y - 0.56 * s, x + 0.56 * s, y + 0.56 * s], math.degrees(-a) - 35 + 180, math.degrees(-a) + 35 + 180,
               fill=255, width=int(0.045 * s))
    d._image.paste(fill, (0, 0), mask)


SIX = ('px', 'nx', 'py', 'ny', 'pz', 'nz')


# ============================================================================== 1. magnetic wand
def magnetic_wand():
    S = 512
    col = np.ones((S, S, 3), np.float32) * 0.07
    n = mk.noise(S, S, 20, 101, 3); col += (n[..., None] - 0.5) * 0.02
    hgt = np.zeros((S, S), np.float32); sm = np.full((S, S), 0.55, np.float32); mm = np.full((S, S), 0.8, np.float32)
    # v along the body (rows: v=1 top). Grip knurl rows for y 0.05..0.11 of the 0.14 body -> v 0.36..0.79
    r0, r1 = int((1 - 0.79) * S), int((1 - 0.36) * S)
    yy, xx = np.mgrid[r0:r1, 0:S]
    hgt[r0:r1] = (np.abs(((xx + yy) % 10) - 5) + np.abs(((xx - yy) % 10) - 5)) / 10; sm[r0:r1] = 0.4
    # anodised red band near the tip and white print above the grip
    b0, b1 = int((1 - 0.12) * S), int((1 - 0.06) * S)
    col[b0:b1] = np.array([0.55, 0.07, 0.06]); mm[b0:b1] = 0.6
    im = img(col); d = ImageDraw.Draw(im)
    d.text((20, int((1 - 0.30) * S) - 12), 'MAGNETIC APPLICATOR', font=font('sans-bold', 20), fill=(215, 215, 215))
    d.text((20, int((1 - 0.24) * S) - 10), 'pull plunger to release powder', font=font('sans', 14), fill=(160, 160, 160))
    save_maps('MagneticWand', im, hgt, 2.5, mm, sm, ms_px=256)
    m = mk.Mesh()
    # body (lathe, y 0.016..0.156, v mapped 0..1 over its length)
    body = [(0.0, 0.016), (0.0032, 0.016), (0.0040, 0.030), (0.0048, 0.055), (0.0062, 0.075), (0.0065, 0.110),
            (0.0062, 0.140), (0.0058, 0.154), (0.0040, 0.156), (0.0025, 0.156)]
    P, UV, N = mk.lathe(body, 24, uv=(0, 0, 1, 1)); m.add('WandBody', P, UV, N)
    # plunger rod and button
    rod = [(0.0, 0.156), (0.0022, 0.156), (0.0022, 0.170), (0.0050, 0.170), (0.0050, 0.175), (0.0, 0.1755)]
    P, UV, N = mk.lathe(rod, 20, uv=(0, 0.97, 1, 0.99)); m.add('WandBody', P, UV, N)
    # magnet pin and powder ball (18 mm) at the tip
    P, UV, N = mk.lathe([(0.0, 0.010), (0.0018, 0.010), (0.0018, 0.018), (0.0, 0.018)], 10, uv=(0, 0.97, 1, 0.99)); m.add('WandBody', P, UV, N)
    prof = [(0.009 * math.sin(t), 0.009 - 0.009 * math.cos(t)) for t in np.linspace(0, math.pi, 14)]
    P, UV, N = mk.lathe(prof, 24); P = np.array(P)
    rng = np.random.default_rng(7)
    ang = np.arctan2(-P[..., 2], P[..., 0]); el = P[..., 1]
    bump = 1 + 0.07 * np.sin(ang * 6 + el * 900) + 0.05 * np.sin(ang * 13 + 1.3) + 0.04 * np.sin(el * 1400)
    c = np.array([0, 0.009, 0]); P = c + (P - c) * bump[..., None]
    m.add('PowderBall', P, UV, mk.smooth_normals(P, 80))
    finish('MagneticWand', m, {'WandBody': dict(map='MagneticWand_Color.jpg', Ks=0.3), 'PowderBall': dict(Kd=(0.03, 0.03, 0.03), Ks=0.02)},
           (0.018, 0.176, 0.018), 'Upright, powder ball at the base; pivot at the tip. Rotate to hold like a pen.')


# ============================================================================== 2. gel lifters
def gel_lifter(name, black):
    W, D = 0.13, 0.18
    S = 512
    gel = 0.06 if black else 0.93
    col = np.ones((S, S * 2, 3), np.float32) * gel
    n = mk.noise(S, S * 2, 60, 111 if black else 112, 4); col += (n[..., None] - 0.5) * 0.02
    sm = np.full((S, S * 2), 0.80 if black else 0.75, np.float32)
    im = img(col); d = ImageDraw.Draw(im)
    # left half (0..512): gel face with a 2 mm carrier margin; right half: back of the carrier with print
    carrier = (28, 28, 30) if black else (236, 236, 232)
    d.rectangle([0, 0, 511, 511], outline=carrier, width=8)
    d.rectangle([512, 0, 1023, 511], fill=carrier)
    ink = (205, 205, 205) if black else (40, 40, 40)
    d.text((540, 30), 'GELATIN LIFTER', font=font('sans-bold', 34), fill=ink)
    d.text((540, 76), '13 x 18 cm   %s' % ('BLACK' if black else 'WHITE'), font=font('sans', 24), fill=ink)
    d.text((540, 112), 'Peel the cover sheet, lay one edge on the mark,', font=font('sans', 17), fill=ink)
    d.text((540, 136), 'roll down without bubbles, press, lift, replace cover.', font=font('sans', 17), fill=ink)
    d.text((540, 170), 'Lot 2609-33   Keep flat, store cool', font=font('sans', 17), fill=ink)
    ar(d, (1000, 215), 'رافع جيلاتيني للآثار', 24, ink)
    sm[:, 512:] = 0.35
    save_maps(name, im, None, metal=0.0, smooth=sm, ms_px=256)
    m = mk.Mesh()
    GEL = (0, 0, 0.5, 1); BACK = (0.5, 0, 1, 1); EDGE = (0.5, 0.5, 0.51, 0.51)
    P, UV, N = mk.rounded_box((W, 0.0013, D), 0.003, 2, uv_rects={'py': GEL, 'ny': BACK, 'px': EDGE, 'nx': EDGE, 'pz': EDGE, 'nz': EDGE})
    m.add('GelLifter', P + [0, 0.00065, 0], UV, N)
    # clear cover sheet, lifted at the +x/+z corner
    def top(X, Z):
        s = ((X / W + 0.5) + (Z / D + 0.5)) / 2
        return 0.0014 + 0.028 * np.clip((s - 0.60) / 0.40, 0, 1) ** 2.2
    P, UV, N = slab(W - 0.002, D - 0.002, 14, 18, top, thickness=0.0001, uv_top=(0, 0, 0.5, 1))
    m.add('CoverSheetPET', P, UV, N)
    finish(name, m, {'GelLifter': dict(map=name + '_Color.jpg', Ks=0.4),
                     'CoverSheetPET': dict(Kd=(0.9, 0.92, 0.95), d=0.22, Ks=0.9, Ns=900)}, (W, 0.03, D),
           'Lies flat, gel face up; cover sheet (CoverSheetPET, glass material) curls up at the +x/+z corner.')


# ============================================================================== 3. backing cards and lift card
def card_form(d, x0, y0, w, h, black, title='LATENT LIFT CARD'):
    ink = (225, 225, 225) if black else (30, 30, 30)
    red = (200, 60, 50) if not black else (235, 120, 110)
    px = w / 127.0  # px per mm
    fT = font('sans-bold', int(5.2 * px)); fL = font('sans', int(2.6 * px)); fS = font('sans', int(2.0 * px))
    d.rectangle([x0, y0, x0 + w - 1, y0 + h - 1], outline=ink, width=max(1, int(0.4 * px)))
    d.text((x0 + 4 * px, y0 + 3 * px), title, font=fT, fill=ink)
    ar(d, (x0 + w - 4 * px, y0 + 6.5 * px), 'بطاقة رفع أثر بصمة', int(3.6 * px), ink)
    d.line([(x0, y0 + 11 * px), (x0 + w, y0 + 11 * px)], fill=ink, width=max(1, int(0.3 * px)))
    rows = [('Case no.', 'Lift no.'), ('Location of lift', None), ('Surface / object', None), ('Date', 'Time'), ('Officer', 'ID')]
    y = y0 + 13 * px
    for a, b in rows:
        d.text((x0 + 4 * px, y), a, font=fL, fill=ink)
        if b:
            d.text((x0 + 48 * px, y), b, font=fL, fill=ink)
            d.line([(x0 + 4 * px, y + 7 * px), (x0 + 44 * px, y + 7 * px)], fill=ink, width=1)
            d.line([(x0 + 48 * px, y + 7 * px), (x0 + 80 * px, y + 7 * px)], fill=ink, width=1)
        else:
            d.line([(x0 + 4 * px, y + 7 * px), (x0 + 80 * px, y + 7 * px)], fill=ink, width=1)
        y += 9.5 * px
    # orientation box with arrow and sketch area
    bx0, by0, bx1, by1 = x0 + 86 * px, y0 + 13 * px, x0 + 123 * px, y0 + 50 * px
    d.rectangle([bx0, by0, bx1, by1], outline=ink, width=1)
    d.text((bx0 + 2 * px, by0 + 1 * px), 'orientation / sketch', font=fS, fill=ink)
    cx = (bx0 + bx1) / 2
    d.line([(cx, by1 - 4 * px), (cx, by0 + 9 * px)], fill=red, width=max(1, int(0.8 * px)))
    d.polygon([(cx, by0 + 6 * px), (cx - 3 * px, by0 + 12 * px), (cx + 3 * px, by0 + 12 * px)], fill=red)
    d.text((x0 + 86 * px, y0 + 53 * px), 'Photographed before lift:  [ ] yes', font=fS, fill=ink)
    d.text((x0 + 86 * px, y0 + 58 * px), 'Powder:  [ ] black  [ ] alu  [ ] mag', font=fS, fill=ink)
    d.text((x0 + 4 * px, y0 + 66 * px), 'Place the lift tape on the plain side. Fill in every field before the next lift.', font=fS, fill=ink)
    d.text((x0 + 4 * px, y0 + 70 * px), 'Form LP-3  (fictional)', font=fS, fill=ink)


def backing_card(name, black, with_print=False):
    Lx, Lz = 5 * INCH, 3 * INCH  # 0.127 x 0.0762
    S = 1024
    base = 0.10 if black else 0.94
    col = grain(S, S, (base, base, base * 1.0), 121 if black else 122, fibre=0.01, blotch=0.015)
    im = img(col); d = ImageDraw.Draw(im)
    card_form(d, 0, 0, 1024, 614, black)
    sm = np.full((S, S), 0.45 if black else 0.5, np.float32)
    TAPE = None
    if with_print:
        # tape strip region 540 x 300 px = 90 x 50 mm (6 px/mm), slightly greyer and glossier than the card
        x0, y0, x1, y1 = 20, 660, 560, 960
        d.rectangle([x0, y0, x1, y1], fill=(238, 238, 236))
        sm[y0:y1, x0:x1] = 0.85
        if os.path.exists(FP_ATLAS):
            atlas = Image.open(FP_ATLAS).convert('RGBA')
            cell = atlas.crop((0, 256, 256, 512)).resize((150, 150), Image.LANCZOS)  # developed loop, 25 mm
            cell2 = atlas.crop((512, 256, 768, 512)).resize((150, 150), Image.LANCZOS).rotate(28, resample=Image.BICUBIC)
            a2 = cell2.split()[3].point(lambda v: int(v * 0.55)); cell2.putalpha(a2)
            im.paste(cell, (x0 + 200, y0 + 75), cell)
            im.paste(cell2, (x0 + 320, y0 + 110), cell2)
        else:
            # generic non-identifying concentric ridge pattern
            cx, cy = x0 + 275, y0 + 150
            for r in range(8, 70, 5):
                d.ellipse([cx - r * 0.8, cy - r, cx + r * 0.8, cy + r], outline=(25, 25, 25), width=2)
        # powder haze along the tape and a felt-pen lift number
        haze = (mk.noise(y1 - y0, x1 - x0, 12, 123, 3) > 0.62).astype(np.float32) * 0.25
        a = arr(im); a[y0:y1, x0:x1] *= (1 - haze[..., None]); im = img(a); d = ImageDraw.Draw(im)
        d.text((x0 + 12, y0 + 10), 'L-4', font=font('sans-bold', 40), fill=(30, 30, 110))
        TAPE = R(x0, y0, x1, y1)
    save_maps(name, im, None, metal=0.0, smooth=sm, ms_px=256)
    m = mk.Mesh()
    BACK = R(0, 0, 1024, 614); FRONT = R(600, 660, 1000, 960); EDGE = R(600, 980, 620, 1000)
    P, UV, N = mk.box((Lx, 0.0005, Lz), uvs={'py': FRONT, 'ny': BACK, 'px': EDGE, 'nx': EDGE, 'pz': EDGE, 'nz': EDGE})
    m.add('Card', P + [0, 0.00025, 0], UV, N)
    mats = {'Card': dict(map=name + '_Color.jpg', Ks=0.1)}
    if with_print:
        P, UV, N = mk.box((0.09, 0.00015, 0.05), uvs={'py': TAPE, 'ny': FRONT, 'px': EDGE, 'nx': EDGE, 'pz': EDGE, 'nz': EDGE})
        m.add('LiftTape', P + [0.004, 0.0005 + 0.000075, -0.004], UV, N)
        mats['LiftTape'] = dict(map=name + '_Color.jpg', Ks=0.6, Ns=400)
    finish(name, m, mats, (Lx, 0.0005, Lz),
           'Lies flat, plain front up; the printed form is the underside (readable when the card is turned over).'
           + (' LiftTape is the clear tape strip with a black-powder ridge lift on the front.' if with_print else ''))


# ============================================================================== 4. hinge lifters
def hinge_lifter(name, black):
    W, L = 2 * INCH, 4 * INCH
    S = 512
    base = 0.10 if black else 0.94
    im = img(grain(S, S, (base, base, base), 131 if black else 132, 0.01, 0.015)); d = ImageDraw.Draw(im)
    ink = (225, 225, 225) if black else (30, 30, 30)
    # rows 0..256: back of the card (form), rows 256..512: front (plain, hinge tape strip at the top 10 mm)
    px = 512 / 101.6
    d.rectangle([0, 0, 511, 255], outline=ink, width=2)
    d.text((10, 8), 'HINGE LIFTER  2 x 4 in', font=font('sans-bold', 22), fill=ink)
    ar(d, (500, 20), 'رافع مفصلي', 20, ink)
    yy = 44
    for lab in ('Case no.', 'Lift no.', 'Location', 'Date / officer'):
        d.text((10, yy), lab, font=font('sans', 16), fill=ink); d.line([(130, yy + 18), (500, yy + 18)], fill=ink, width=1); yy += 30
    d.text((10, 190), 'orientation', font=font('sans', 14), fill=ink)
    d.line([(60, 240), (60, 205)], fill=ink, width=3); d.polygon([(60, 200), (52, 212), (68, 212)], fill=ink)
    d.text((110, 215), 'Form HL-2 (fictional)', font=font('sans', 13), fill=ink)
    hinge_rgb = (60, 60, 62) if black else (250, 250, 248)
    d.rectangle([0, 256, 511, 256 + int(10 * px)], fill=hinge_rgb)
    sm = np.full((S, S), 0.45, np.float32); sm[256:] = 0.5
    save_maps(name, im, None, metal=0.0, smooth=sm, ms_px=128)
    m = mk.Mesh()
    BACK = R(0, 0, 512, 256, 512, 512); FRONT = R(0, 256, 512, 512, 512, 512); EDGE = R(300, 300, 310, 310, 512, 512)
    # card: hinge edge at -z (short edge); the FRONT image's top (v = 1) is at -z, so the hinge strip is at the hinge
    P, UV, N = mk.box((W, 0.0006, L), uvs={'py': FRONT, 'ny': BACK, 'px': EDGE, 'nx': EDGE, 'pz': EDGE, 'nz': EDGE})
    m.add('LifterCard', P + [0, 0.0003, 0], UV, N)
    # cover flap (clear acetate with adhesive) hinged at z = -L/2 + 5 mm, open 30 degrees
    P, UV, N = mk.box((W - 0.001, 0.0002, L - 0.006))
    P = P + [0, 0.0001, (L - 0.006) / 2]
    Mh = mk.chain(mk.T(0, 0.0006, -L / 2 + 0.005), mk.Rx(-30))
    P, N = mk.xform(P, N, Mh)
    m.add('LifterFilm', P, UV, N)
    finish(name, m, {'LifterCard': dict(map=name + '_Color.jpg', Ks=0.1),
                     'LifterFilm': dict(Kd=(0.92, 0.94, 0.96), d=0.3, Ks=0.8, Ns=600)}, (W, 0.05, L),
           'Card flat, hinge at the -z short edge; the clear flap (LifterFilm, glass material) stands open at 30 degrees.')


# ============================================================================== 5. swabs
def swab_atlas(name, S=512, with_cap=True):
    col = np.ones((S, S, 3), np.float32) * 0.93
    n = mk.noise(S, S, 25, 141, 4)
    hgt = np.zeros((S, S), np.float32); sm = np.full((S, S), 0.55, np.float32); mm = np.zeros((S, S), np.float32)
    # rows 0..128 cotton (fibrous), rows 128..256 shaft (white polystyrene), rows 256..384 label, rows 384..512 cap (blue, ribbed)
    fib = blur(np.repeat(np.random.default_rng(142).random((1, S)), 128, 0), 0.8)
    col[:128] = (0.88 + 0.12 * fib)[..., None] * np.array([1.0, 0.99, 0.96]); hgt[:128] = fib; sm[:128] = 0.2
    col[128:256] = 0.95; sm[128:256] = 0.6
    im = img(col); d = ImageDraw.Draw(im)
    d.rectangle([0, 256, 511, 383], fill=(248, 248, 244))
    d.rectangle([0, 256, 511, 274], fill=(30, 90, 170))
    d.text((8, 257), 'STERILE  DNA-FREE  SWAB', font=font('sans-bold', 15), fill=(255, 255, 255))
    d.text((8, 284), 'Cotton tip, polystyrene shaft 150 mm', font=font('sans', 14), fill=(40, 40, 40))
    d.text((8, 304), 'Single use. Break the shaft at the score line.', font=font('sans', 13), fill=(40, 40, 40))
    d.text((8, 324), 'Lot 2609-07   Exp 2028-09', font=font('sans', 13), fill=(40, 40, 40))
    ar(d, (500, 356), 'مسحة معقمة', 18, (40, 40, 40))
    if with_cap:
        d.rectangle([0, 384, 511, 511], fill=(28, 88, 175))
        cols = np.arange(S)[None, :]
        hgt[384:] = (0.5 + 0.5 * np.sin(cols / S * 2 * math.pi * 48)) * np.ones((128, 1)); sm[384:] = 0.45
    save_maps(name, im, hgt, 2.0, mm, sm, ms_px=256)


def swab_tube():
    swab_atlas('SwabTube')
    m = mk.Mesh()
    COT = (0, 0.75, 1, 1); SHAFT = (0, 0.5, 1, 0.75); LABEL = (0, 0.25, 1, 0.5); CAP = (0, 0, 1, 0.25)
    # tube: hemispherical bottom, r 6.5 mm, open top under the cap
    prof = [(0.0065 * math.sin(t), 0.0065 - 0.0065 * math.cos(t)) for t in np.linspace(0, math.pi / 2, 6)] + [(0.0065, 0.132)]
    P, UV, N = mk.lathe(prof, 24); m.add('TubeClear', P, UV, N)
    P, UV, N = mk.lathe([(0.0068, 0.062), (0.0068, 0.092)], 24, uv=(0, 0.26, 1, 0.49)); m.add('SwabAtlas', P, UV, N)
    cap = [(0.0, 0.150), (0.0070, 0.150), (0.0078, 0.148), (0.0078, 0.132), (0.0070, 0.130), (0.0062, 0.130), (0.0062, 0.142), (0.0, 0.142)]
    P, UV, N = mk.lathe(cap[::-1], 24, uv=CAP, smooth=False); m.add('SwabAtlas', P, UV, mk.smooth_normals(P, 40))
    # swab inside: cotton tip at the bottom, shaft up into the cap
    tip = [(0.0, 0.012), (0.0018, 0.0125), (0.0026, 0.016), (0.0027, 0.024), (0.0022, 0.030), (0.0012, 0.0325)]
    P, UV, N = mk.lathe(tip, 14, uv=COT); m.add('SwabAtlas', P, UV, N)
    P, UV, N = mk.lathe([(0.0012, 0.0325), (0.0012, 0.142)], 10, uv=SHAFT); m.add('SwabAtlas', P, UV, N)
    finish('SwabTube', m, {'TubeClear': dict(Kd=(0.9, 0.93, 0.95), d=0.28, Ks=0.9, Ns=900), 'SwabAtlas': dict(map='SwabTube_Color.jpg', Ks=0.2)},
           (0.0156, 0.150, 0.0156), 'Upright, cap at the top (y = 0.15). TubeClear needs the glass material.')


def swab_open():
    swab_atlas('SwabOpen', 256, with_cap=False)
    m = mk.Mesh()
    COT = (0, 0.75, 1, 1); SHAFT = (0, 0.5, 1, 0.75)
    P, UV, N = mk.lathe([(0.0, 0.0), (0.0012, 0.0)], 10, uv=SHAFT); m.add('Swab', P, UV, N)
    P, UV, N = mk.lathe([(0.0012, 0.0), (0.0012, 0.130)], 10, uv=SHAFT); m.add('Swab', P, UV, N)
    tip = [(0.0012, 0.130), (0.0022, 0.1325), (0.0027, 0.138), (0.0026, 0.146), (0.0018, 0.1495), (0.0, 0.150)]
    P, UV, N = mk.lathe(tip, 14, uv=COT); m.add('Swab', P, UV, N)
    finish('SwabOpen', m, {'Swab': dict(map='SwabOpen_Color.jpg', Ks=0.2)}, (0.0054, 0.150, 0.0054),
           'Upright, cotton tip at the top; pivot at the shaft end (hold like a pen, tip forward).')


def swab_box():
    Lx, Hy, Dz = 0.175, 0.032, 0.032
    W, H = 1024, 512
    col = grain(H, W, (0.86, 0.80, 0.66), 151, 0.02, 0.03)  # kraft board
    im = img(col); d = ImageDraw.Draw(im)
    hgt = np.zeros((H, W), np.float32)
    # rows 0..187 top (175 x 32 mm at 5.85 px/mm), 187..374 front, 374..440 ends (two cells), rest plain
    px = 1024 / 175.0
    d.rectangle([0, 0, 1023, 186], outline=(70, 60, 45), width=2)
    d.text((16, 14), 'SWAB DRYING BOX', font=font('sans-bold', 42), fill=(40, 35, 30))
    d.text((16, 66), 'Biological sample  -  keep upright  -  do not seal until dry', font=font('sans', 22), fill=(50, 45, 40))
    d.text((16, 100), 'Case ________  Item ____  Date ______  Officer ______', font=font('sans', 22), fill=(50, 45, 40))
    ar(d, (1000, 150), 'عينة بيولوجية - يحفظ جافاً', 26, (50, 45, 40))
    # ventilation holes on the top and the front
    for row0 in (150, 300):
        for k in range(12):
            x = 560 + k * 36
            d.ellipse([x - 9, row0 - 9, x + 9, row0 + 9], fill=(45, 38, 30))
            hgt[row0 - 9:row0 + 9, x - 9:x + 9] = 1
    d.rectangle([0, 187, 1023, 373], outline=(70, 60, 45), width=2)
    d.text((16, 200), 'DNA  -  TRACE EVIDENCE', font=font('sans-bold', 36), fill=(170, 40, 35))
    d.text((16, 250), 'Air-dry swabs in the box before packaging. Fictional Lab Supply, lot 2609-11', font=font('sans', 20), fill=(50, 45, 40))
    biohazard(d, 940, 280, 70, (170, 40, 35))
    d.rectangle([0, 374, 186, 560], outline=(70, 60, 45), width=2)
    d.text((20, 395), 'OPEN', font=font('sans-bold', 24), fill=(50, 45, 40))
    d.line([(20, 430), (166, 430)], fill=(70, 60, 45), width=2)
    save_maps('SwabBox', im, blur(hgt, 1.2), 2.5, 0.0, 0.25, n_px=512, ms_px=128)
    m = mk.Mesh()
    TOP = R(0, 0, 1024, 187, W, H); FRONT = R(0, 187, 1024, 374, W, H); END = R(0, 374, 187, 560, W, H); PLAIN = R(600, 420, 700, 500, W, H)
    P, UV, N = mk.rounded_box((Lx, Hy, Dz), 0.0012, 1, uv_rects={'py': TOP, 'pz': FRONT, 'nz': FRONT, 'px': END, 'nx': END, 'ny': PLAIN})
    m.add('SwabBoxCard', P + [0, Hy / 2, 0], UV, N)
    finish('SwabBox', m, {'SwabBoxCard': dict(map='SwabBox_Color.jpg', Ks=0.05)}, (Lx, Hy, Dz), 'Lies flat, label up, long axis X.')


# ============================================================================== 6. water ampoule and presumptive test kit
def water_ampoule():
    S = 256
    col = np.ones((S, S, 3), np.float32) * 0.96
    im = img(col); d = ImageDraw.Draw(im)
    d.text((10, 60), 'STERILE WATER', font=font('sans-bold', 22), fill=(40, 90, 170))
    d.text((10, 90), '10 ml  single use', font=font('sans', 17), fill=(40, 90, 170))
    d.text((10, 115), 'for swab moistening', font=font('sans', 14), fill=(90, 90, 100))
    d.text((10, 140), 'Lot 2609-02', font=font('sans', 14), fill=(90, 90, 100))
    save_maps('DistilledWaterAmpoule', im, None, 0.0, 0.5, ms_px=64)
    m = mk.Mesh()
    body = [(0.0, 0.0), (0.0075, 0.0), (0.0090, 0.003), (0.0092, 0.030), (0.0088, 0.046), (0.0060, 0.052), (0.0040, 0.056), (0.0040, 0.062)]
    P, UV, N = mk.lathe(body, 24, uv=(0, 0, 1, 0.8)); m.add('Ampoule', P, UV, N)
    # twist-off tab
    add_box(m, 'Ampoule', (0.012, 0.012, 0.003), (0, 0.068, 0), (0.1, 0.9, 0.12, 0.92))
    P, UV, N = mk.lathe([(0.0040, 0.062), (0.0048, 0.0625), (0.0048, 0.074), (0.0, 0.074)], 16, uv=(0.1, 0.9, 0.12, 0.92)); m.add('Ampoule', P, UV, N)
    finish('DistilledWaterAmpoule', m, {'Ampoule': dict(map='DistilledWaterAmpoule_Color.jpg', d=0.85, Ks=0.4, Ns=200)},
           (0.0184, 0.074, 0.0184), 'Upright. Translucent LDPE: Standard Fade with the colour alpha ~0.85.')


def km_kit():
    S = 1024
    col = np.ones((S, S, 3), np.float32) * 0.80
    n = mk.noise(S, S, 8, 161, 3); col += (n[..., None] - 0.5) * 0.03
    sm = np.full((S, S), 0.35, np.float32)
    im = img(col); d = ImageDraw.Draw(im)
    # rows 0..256 case plastic (grey); rows 256..512 lid inside label (120 x 80 mm -> 768 x 512? use 768 x 256 strip at 6.4 px/mm)
    d.rectangle([0, 256, 767, 511], fill=(246, 246, 240))
    d.rectangle([0, 256, 767, 300], fill=(150, 30, 30))
    d.text((14, 262), 'PRESUMPTIVE BLOOD TEST KIT', font=font('sans-bold', 30), fill=(255, 255, 255))
    f = font('sans', 19)
    for i, line in enumerate(['1  Moisten a swab, touch the stain.', '2  One drop ETHANOL, one drop REAGENT: no colour change.',
                              '3  One drop PEROXIDE: an immediate pink = presumptive positive.', 'Record the result, then package the swab. Not a confirmatory test.']):
        d.text((14, 310 + i * 28), line, font=f, fill=(40, 40, 40))
    ar(d, (750, 440), 'اختبار أولي للدم - فينولفثالين', 22, (40, 40, 40))
    d.text((14, 470), 'Fictional Lab Supply  lot 2609-19', font=font('sans', 16), fill=(90, 90, 90))
    # bottle labels: three cells 256 px wide, rows 512..768
    names = [('1  ETHANOL', (40, 100, 60)), ('2  REAGENT', (150, 30, 30)), ('3  PEROXIDE', (30, 70, 150))]
    for k, (lab, rgb) in enumerate(names):
        x0 = k * 256
        d.rectangle([x0, 512, x0 + 255, 767], fill=(250, 250, 246))
        d.rectangle([x0, 512, x0 + 255, 560], fill=rgb)
        d.text((x0 + 10, 518), lab, font=font('sans-bold', 28), fill=(255, 255, 255))
        d.text((x0 + 10, 580), '15 ml dropper', font=font('sans', 20), fill=(40, 40, 40))
        d.text((x0 + 10, 610), ('70 %', 'phenolphthalein', '3 % H2O2')[k], font=font('sans', 20), fill=(40, 40, 40))
        d.text((x0 + 10, 650), 'Keep closed. Store cool.', font=font('sans', 16), fill=(80, 80, 80))
        d.text((x0 + 10, 720), 'Lot 2609-19', font=font('sans', 16), fill=(80, 80, 80))
    # cap colours row 768..832 (three cells), filter paper pad row 832..1024
    for k, (lab, rgb) in enumerate(names):
        d.rectangle([k * 256, 768, k * 256 + 255, 831], fill=rgb)
    d.rectangle([0, 832, 1023, 1023], fill=(242, 242, 236))
    d.text((20, 850), 'FILTER PAPER  50 sheets', font=font('sans-bold', 24), fill=(60, 60, 60))
    sm[256:512, :768] = 0.3; sm[512:768] = 0.4; sm[768:832] = 0.5
    save_maps('KMTestKit', im, None, 0.0, sm, ms_px=256)
    m = mk.Mesh()
    CASE = R(0, 0, 1024, 256); LID = R(0, 256, 768, 512); PAD = R(0, 832, 1024, 1024)
    LAB = [R(k * 256, 512, k * 256 + 256, 768) for k in range(3)]; CAP = [R(k * 256, 768, k * 256 + 256, 832) for k in range(3)]
    Lx, Hy, Dz, t = 0.12, 0.03, 0.08, 0.002
    add_box(m, 'KitAtlas', (Lx, t, Dz), (0, t / 2, 0), CASE)
    add_box(m, 'KitAtlas', (Lx, Hy - t, t), (0, (Hy + t) / 2, Dz / 2 - t / 2), CASE)
    add_box(m, 'KitAtlas', (Lx, Hy - t, t), (0, (Hy + t) / 2, -Dz / 2 + t / 2), CASE)
    add_box(m, 'KitAtlas', (t, Hy - t, Dz - 2 * t), (Lx / 2 - t / 2, (Hy + t) / 2, 0), CASE)
    add_box(m, 'KitAtlas', (t, Hy - t, Dz - 2 * t), (-Lx / 2 + t / 2, (Hy + t) / 2, 0), CASE)
    # lid hinged at the back (z = -Dz/2), open 105 degrees: inside face (-y before rotation) carries the label
    P, UV, N = mk.box((Lx, t, Dz), uvs={'ny': LID, 'py': CASE, 'px': CASE, 'nx': CASE, 'pz': CASE, 'nz': CASE})
    P = P + [0, t / 2, Dz / 2]
    P, N = mk.xform(P, N, mk.chain(mk.T(0, Hy, -Dz / 2), mk.Rx(105)))
    m.add('KitAtlas', P, UV, N)
    # three dropper bottles lying along x, caps toward +x
    prof = [(0.0, 0.0), (0.0095, 0.0), (0.0115, 0.002), (0.0115, 0.038), (0.0100, 0.044), (0.0055, 0.047), (0.0055, 0.050)]
    cap = [(0.0055, 0.050), (0.0075, 0.050), (0.0080, 0.052), (0.0080, 0.064), (0.0070, 0.066), (0.0, 0.066)]
    for k in range(3):
        z = -0.025 + k * 0.025
        Mb = mk.chain(mk.T(-0.045, t + 0.0115, z), mk.Rz(-90))
        sub = mk.Mesh()
        P, UV, N = mk.lathe(prof, 20, uv=(0, 0, 1, 1)); UV = UV.copy()
        ys = P[..., 1]; lab = (ys > 0.004) & (ys < 0.036)
        u0, v0, u1, v1 = LAB[k]
        UV[..., 1] = np.where(lab, v0 + (v1 - v0) * (ys - 0.004) / 0.032, (v0 + v1) / 2); UV[..., 0] = u0 + (u1 - u0) * UV[..., 0]
        sub.add('KitAtlas', P, UV, N)
        P, UV, N = mk.lathe(cap, 20, uv=CAP[k]); sub.add('KitAtlas', P, UV, N)
        m.merge(sub, Mb)
    add_box(m, 'KitAtlas', (0.028, 0.006, 0.04), (0.040, t + 0.003, 0.0), PAD)
    finish('KMTestKit', m, {'KitAtlas': dict(map='KMTestKit_Color.jpg', Ks=0.2)}, (0.12, 0.08, 0.08),
           'Open case, lid hinged at -z standing at 105 degrees; three dropper bottles lie along X with caps to -x; filter pad at +x.')


# ============================================================================== 7. silicone casting
def crimp_tube(m, mat, L, r, uv_body, uv_crimp, uv_cap, segs=24, rows=16):
    """Toothpaste-style tube along +Y (cap at the top), round at the shoulder, flat crimp at the base."""
    rings = []
    for i in range(rows + 1):
        t = i / rows  # 0 crimp -> 1 shoulder
        k = (1 - t) ** 1.6
        a = r * (1 + 0.57 * k); b = r * (1 - 0.97 * k)
        ang = np.linspace(0, 2 * math.pi, segs, endpoint=False)
        rings.append(np.stack([a * np.cos(ang), np.full(segs, 0.012 + t * (L - 0.012)), -b * np.sin(ang)], -1))
    P, UV, N = loft(rings, uv=uv_body); m.add(mat, P, UV, N)
    # crimp seam block at the base (flat fold)
    add_box(m, mat, (2 * r * 1.57, 0.012, 0.0025), (0, 0.006, 0), uv_crimp)
    # shoulder, neck, cap
    y0 = L
    sh = [(r, y0), (r * 0.6, y0 + 0.010), (r * 0.30, y0 + 0.014), (r * 0.30, y0 + 0.020)]
    P, UV, N = mk.lathe(sh, segs, uv=uv_crimp); m.add(mat, P, UV, N)
    cp = [(r * 0.30, y0 + 0.020), (r * 0.52, y0 + 0.020), (r * 0.55, y0 + 0.023), (r * 0.55, y0 + 0.040), (r * 0.48, y0 + 0.042), (0, y0 + 0.042)]
    P, UV, N = mk.lathe(cp, segs, uv=uv_cap); m.add(mat, P, UV, N)
    return y0 + 0.042


def mikrosil_tube(name, L, r, title, lines, base_rgb, S=1024):
    H = S // 2
    q = lambda v: int(v * S / 1024)  # layout authored at 1024 px
    col = np.ones((H, S, 3), np.float32) * np.array(base_rgb)
    n = mk.noise(H, S, 30, 171, 3); col += (n[..., None] - 0.5) * 0.03
    hgt = np.zeros((H, S), np.float32); sm = np.full((H, S), 0.5, np.float32); mm = np.zeros((H, S), np.float32)
    im = img(col); d = ImageDraw.Draw(im)
    # rows 0..384 body wrap (u around, v along: v=1 at the shoulder); label panel in the middle
    d.rectangle([q(60), q(70), q(980), q(330)], fill=(248, 246, 240))
    d.text((q(80), q(84)), title, font=font('sans-bold', q(46)), fill=(50, 32, 20))
    y = q(140)
    for ln in lines:
        d.text((q(80), y), ln, font=font('sans', q(24)), fill=(50, 40, 35)); y += q(32)
    ar(d, (q(960), q(300)), 'معجون صب السيليكون' if 'PUTTY' in title else 'مادة مصلّبة', q(26), (50, 40, 35))
    # rows 384..448 crimp (foil), rows 448..512 cap (white ribbed)
    c0, c1 = q(384), q(448)
    d.rectangle([0, c0, S - 1, c1 - 1], fill=(190, 190, 195)); mm[c0:c1] = 1; sm[c0:c1] = 0.6
    d.rectangle([0, c1, S - 1, H - 1], fill=(240, 240, 238)); sm[c1:] = 0.55
    cols = np.arange(S)[None, :]
    hgt[c1:] = (0.5 + 0.5 * np.sin(cols / S * 2 * math.pi * 36)) * np.ones((H - c1, 1))
    hgt[c0:c1] = (mk.noise(c1 - c0, S, 6, 172, 2) > 0.5).astype(np.float32) * 0.6
    save_maps(name, im, hgt, 2.0, mm, sm, n_px=512, ms_px=256)
    m = mk.Mesh()
    BODY = R(0, 0, S, c0, S, H); CRIMP = R(0, c0, S, c1, S, H); CAP = R(0, c1, S, H, S, H)
    crimp_tube(m, 'Tube', L, r, BODY, CRIMP, CAP)
    # lay it down: axis along +z (cap toward +z), flat crimp horizontal
    out = m.transformed(mk.Rx(90))
    finish(name, out, {'Tube': dict(map=name + '_Color.jpg', Ks=0.3)}, (2 * r * 1.57, 2 * r, L + 0.042),
           'Lies on its side, cap toward +z, flat crimp at -z.')


def mixing_spatula():
    S = 256
    base = np.array([0.86, 0.74, 0.55])
    n1 = mk.noise(S, S, 60, 181, 4); streak = blur(np.repeat(np.random.default_rng(182).random((1, S)), S, 0), 1.0)
    col = base[None, None] * (0.9 + 0.12 * n1[..., None]) * (0.92 + 0.12 * streak[..., None])
    col = np.transpose(col, (1, 0, 2))  # grain along u (the long axis)
    save_maps('MixingSpatula', col, None, 0.0, 0.25, ms_px=64)
    L, W, t = 0.150, 0.018, 0.0016
    poly = []
    for a in np.linspace(-90, 90, 9):
        poly.append((L / 2 - W / 2 + W / 2 * math.cos(math.radians(a)), W / 2 * math.sin(math.radians(a))))
    for a in np.linspace(90, 270, 9):
        poly.append((-L / 2 + W / 2 + W / 2 * math.cos(math.radians(a)), W / 2 * math.sin(math.radians(a))))
    top, bot, side = mk.extrude(poly, t, uv_top=lambda x, z: ((x + L / 2) / L, (-z + W / 2) / W))
    m = mk.Mesh(); m.add('Wood', *top); m.add('Wood', *bot); m.add('Wood', *side)
    finish('MixingSpatula', m, {'Wood': dict(map='MixingSpatula_Color.jpg', Ks=0.05)}, (L, t, W), 'Lies flat, long axis X.')


def toolmark_cast():
    S = 512
    rng = np.random.default_rng(191)
    base = np.array([0.42, 0.25, 0.14])
    n = mk.noise(S, S, 40, 192, 3)
    col = base[None, None] * (0.92 + 0.16 * n[..., None])
    # striations: fine lines along u (the 60 mm axis) with random depth
    rows = np.arange(S)
    prof = np.zeros(S, np.float32)
    for _ in range(90):
        c = rng.integers(60, S - 60); w = rng.uniform(0.8, 3.0); a = rng.uniform(0.2, 1.0)
        prof += a * np.exp(-0.5 * ((rows - c) / w) ** 2)
    prof /= prof.max()
    stri = np.repeat(prof[:, None], S, 1) * (0.8 + 0.2 * mk.noise(S, S, 100, 193, 2))
    # a wider blade-edge step across the middle and tiny bubbles
    step = 1 / (1 + np.exp(-(rows - 200) / 6.0)); stri += 0.5 * np.repeat(step[:, None], S, 1)
    bub = (mk.noise(S, S, 5, 194, 2) > 0.84).astype(np.float32)
    hgt = np.clip(stri - 0.3 * bub, 0, 2)
    col *= (1 - 0.25 * (hgt[..., None] - hgt.mean())); col -= 0.15 * bub[..., None]
    save_maps('ToolMarkCast', col, hgt, 3.0, 0.0, 0.45 + 0.1 * (n - 0.5), n_px=512, ms_px=128)
    L, W, t = 0.060, 0.025, 0.004
    def top(X, Z):
        curl = 0.006 * np.clip((Z - 0.006) / 0.0065, 0, 1) ** 2
        return t + curl + 0.0003 * np.sin(X * 400) * np.sin(Z * 700)
    def bottom(X, Z):
        return 0.0 + 0.006 * np.clip((Z - 0.006) / 0.0065, 0, 1) ** 2
    P, UV, N = slab(L, W, 24, 12, top, bottom)
    m = mk.Mesh(); m.add('Silicone', P, UV, N)
    finish('ToolMarkCast', m, {'Silicone': dict(map='ToolMarkCast_Color.jpg', Ks=0.2)}, (L, 0.010, W),
           'Lies flat, striated face up, long axis X; the +z edge curls up.')


# ============================================================================== 8. dental stone: bowl and frame
def mixing_bowl():
    S = 512
    col = np.ones((S, S, 3), np.float32) * 0.05
    n = mk.noise(S, S, 12, 201, 3); col += (n[..., None] - 0.5) * 0.02
    sm = np.full((S, S), 0.3, np.float32); hgt = n * 0.3
    prof_out = [(0.052, 0.0), (0.060, 0.002), (0.074, 0.030), (0.080, 0.058), (0.082, 0.070), (0.079, 0.071)]
    prof_in = [(0.079, 0.071), (0.076, 0.058), (0.070, 0.030), (0.056, 0.007), (0.0, 0.006)]
    prof = prof_out + prof_in[1:]
    pp = np.asarray(prof); Lc = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(pp, axis=0), axis=1))]); Lc /= Lc[-1]
    v_rim = Lc[len(prof_out) - 1]
    # dental-stone residue on the inside (v above the rim), heavier toward the bottom (v -> 1)
    vv = 1 - np.arange(S)[:, None] / S
    inside = vv > v_rim
    res = np.clip((mk.noise(S, S, 40, 202, 4) - 0.35) * 1.6, 0, 1) * np.clip((vv - v_rim) / (1 - v_rim), 0, 1) ** 0.7
    res = res * inside
    stone = np.array([0.78, 0.74, 0.66])
    col = col * (1 - res[..., None]) + stone * res[..., None]; sm = sm * (1 - res) + 0.15 * res; hgt += res * 0.8
    save_maps('MixingBowlRubber', col, hgt, 2.0, 0.0, sm, n_px=512, ms_px=256)
    m = mk.Mesh()
    P, UV, N = mk.lathe(prof, 40, uv=(0, 0, 1, 1), sx=1.0, sz=0.96); m.add('Rubber', P, UV, N)
    P, UV, N = mk.disk(0.052, 0.0, 40, (0.5, 0.02), 0.0, up=False, sz=0.96); m.add('Rubber', P, UV, N)
    finish('MixingBowlRubber', m, {'Rubber': dict(map='MixingBowlRubber_Color.jpg', Ks=0.1)}, (0.164, 0.071, 0.158),
           'Upright; dried dental-stone residue inside.')


def casting_frame():
    S = 256
    streak = blur(np.repeat(np.random.default_rng(211).random((1, S)), S, 0), 0.8)
    col = (0.70 + 0.08 * (streak - 0.5))[..., None] * np.array([1.0, 1.0, 1.02])
    save_maps('CastingFrame', col, streak * 0.3, 1.0, 1.0, 0.55 + 0.1 * (streak - 0.5), n_px=256, ms_px=128)
    Lx, Lz, h, t = 18 * INCH, 7.25 * INCH, 0.025, 0.0025
    m = mk.Mesh(); A = (0, 0, 1, 1)
    add_box(m, 'Alu', (Lx, h, t), (0, h / 2, Lz / 2 - t / 2), A)
    add_box(m, 'Alu', (Lx, h, t), (0, h / 2, -Lz / 2 + t / 2), A)
    add_box(m, 'Alu', (t, h, Lz - 2 * t), (Lx / 2 - t / 2, h / 2, 0), A)
    add_box(m, 'Alu', (t, h, Lz - 2 * t), (-Lx / 2 + t / 2, h / 2, 0), A)
    # corner hinge knuckles (two per corner) and a slide lock on one long bar
    for sx in (-1, 1):
        for sz in (-1, 1):
            for y0 in (0.003, 0.014):
                P, UV, N = mk.lathe([(0.0, y0), (0.004, y0), (0.004, y0 + 0.008), (0.0, y0 + 0.008)], 12)
                m.add('Alu', P + [sx * (Lx / 2 - 0.004), 0, sz * (Lz / 2 - 0.004)], UV, N)
    add_box(m, 'Alu', (0.03, 0.012, 0.004), (0.10, 0.012, Lz / 2 + 0.001), A)
    finish('CastingFrame', m, {'Alu': dict(map='CastingFrame_Color.jpg', Ks=0.6, Ns=300)}, (Lx, h, Lz),
           'Open rectangle on the floor, long axis X; stands around a shoe impression.')


# ============================================================================== 9. evidence tag and label roll
def evidence_tag():
    S = 512
    col = grain(S, S, (0.84, 0.72, 0.50), 221, 0.02, 0.03)  # manila
    im = img(col); d = ImageDraw.Draw(im)
    # left 256 px: front (60 x 120 mm at 4.27 px/mm, v along 120 mm = 512 rows), right: back
    px = 256 / 60.0
    ink = (35, 30, 25); red = (175, 35, 30)
    d.rectangle([0, 0, 255, 511], outline=ink, width=2)
    d.polygon([(0, 0), (40, 0), (0, 40)], fill=(120, 120, 120)); d.polygon([(255, 0), (215, 0), (255, 40)], fill=(120, 120, 120))
    d.ellipse([128 - 16, 34, 128 + 16, 66], fill=(50, 50, 50)); d.ellipse([128 - 22, 28, 128 + 22, 72], outline=(150, 150, 150), width=5)
    d.rectangle([14, 84, 241, 124], fill=red)
    d.text((128, 104), 'EVIDENCE', font=font('sans-bold', 34), fill=(255, 255, 255), anchor='mm')
    ar(d, (236, 142), 'دليل', 24, ink)
    y = 160
    for lab in ('Case no.', 'Item no.', 'Description', '', 'Recovered at', 'Date / time', 'Recovered by', 'Witness'):
        if lab:
            d.text((16, y), lab, font=font('sans', 15), fill=ink)
        d.line([(16, y + 30), (240, y + 30)], fill=ink, width=1); y += 42
    d.text((16, 494), 'Tag ET-60 (fictional)', font=font('sans', 11), fill=(90, 80, 70))
    # back: chain of custody
    d.rectangle([256, 0, 511, 511], outline=ink, width=2)
    d.ellipse([384 - 16, 34, 384 + 16, 66], fill=(50, 50, 50)); d.ellipse([384 - 22, 28, 384 + 22, 72], outline=(150, 150, 150), width=5)
    d.text((272, 90), 'CHAIN OF CUSTODY', font=font('sans-bold', 20), fill=ink)
    d.text((272, 118), 'from            to             date', font=font('sans', 13), fill=ink)
    for k in range(9):
        d.line([(272, 150 + k * 38), (496, 150 + k * 38)], fill=ink, width=1)
    save_maps('EvidenceTag', im, None, 0.0, 0.3, ms_px=128)
    m = mk.Mesh()
    FRONT = R(0, 0, 256, 512, S, S); BACK = R(256, 0, 512, 512, S, S); EDGE = R(300, 300, 310, 310, S, S)
    W, L = 0.060, 0.120
    # tag outline with clipped top corners (top = -z), as a polygon extruded 0.5 mm
    c = 0.008
    poly = [(-W / 2, L / 2), (W / 2, L / 2), (W / 2, -L / 2 + c), (W / 2 - c, -L / 2), (-W / 2 + c, -L / 2), (-W / 2, -L / 2 + c)]
    top, bot, side = mk.extrude(poly, 0.0005, uv_top=lambda x, z: (FRONT[0] + (FRONT[2] - FRONT[0]) * (x + W / 2) / W, FRONT[1] + (FRONT[3] - FRONT[1]) * (L / 2 - z) / L))
    m.add('Tag', *top)
    bP, bUV, bN = bot
    bUV = np.array(bUV); xs = bP[..., 0]; zs = bP[..., 2]
    bUV[..., 0] = BACK[0] + (BACK[2] - BACK[0]) * (xs + W / 2) / W; bUV[..., 1] = BACK[1] + (BACK[3] - BACK[1]) * (L / 2 - zs) / L
    m.add('Tag', bP, bUV, bN)
    sP, sUV, sN = side; m.add('Tag', sP, const_uv(sUV, EDGE), sN)
    # metal eyelet ring at the hole (hole centre 11 mm from the top edge)
    hz = -L / 2 + 0.011
    P, UV, N = mk.lathe([(0.0032, 0.0), (0.0048, 0.0), (0.0052, 0.0004), (0.0052, 0.0009), (0.0048, 0.0012), (0.0032, 0.0012)], 16)
    m.add('Eyelet', P + [0, 0.0, hz], const_uv(UV, EDGE), N)
    # cotton string: through the eyelet and lying in a loose loop on the floor
    tt = np.linspace(0, 1, 40)
    path = np.stack([0.012 * np.sin(tt * 2.2 * math.pi) * (1 - tt * 0.3) + 0.03 * tt, 0.0008 + 0.0005 * np.sin(tt * 9),
                     hz - 0.02 * tt - 0.045 * np.sin(tt * math.pi) ** 2 * (1 - tt)], -1)
    path[0] = [0, 0.001, hz]
    P, UV, N = mk.tube(path, 0.0007, 6, uv=EDGE); m.add('String', P, UV, N)
    finish('EvidenceTag', m, {'Tag': dict(map='EvidenceTag_Color.jpg', Ks=0.05), 'Eyelet': dict(Kd=(0.75, 0.75, 0.72), Ks=0.8, Ns=400),
                              'String': dict(Kd=(0.92, 0.90, 0.84), Ks=0.02)}, (0.060, 0.0015, 0.120),
           'Lies flat, printed front up, eyelet end toward -z; the string trails from the eyelet.')


def label_roll():
    S = 512
    col = np.ones((S, S, 3), np.float32) * 0.95
    im = img(col); d = ImageDraw.Draw(im)
    # rows 0..200: outer wrap (circumference 251 mm -> 512 px, label 50 mm tall -> 200 rows): three "EVIDENCE" labels + a part
    ink = (35, 35, 35); red = (190, 35, 30)
    for k in range(4):
        x0 = k * 153
        d.rectangle([x0 + 3, 4, x0 + 150, 196], outline=(170, 170, 170), width=1)
        d.rectangle([x0 + 10, 12, x0 + 143, 52], fill=red)
        d.text((x0 + 76, 32), 'EVIDENCE', font=font('sans-bold', 24), fill=(255, 255, 255), anchor='mm')
        yy = 64
        for lab in ('Case', 'Item', 'Date', 'By'):
            d.text((x0 + 12, yy), lab, font=font('sans', 13), fill=ink); d.line([(x0 + 48, yy + 15), (x0 + 140, yy + 15)], fill=ink, width=1); yy += 26
        ar(d, (x0 + 140, 180), 'دليل', 14, ink)
    # rows 256..512: top/bottom face: concentric paper rings around the core (polar texture in a 256 px disc)
    yy, xx = np.mgrid[256:512, 0:256]
    rr = np.sqrt((xx - 128) ** 2 + (yy - 384) ** 2) / 128
    rings = 0.90 + 0.08 * np.sin(rr * 2 * math.pi * 60) * (rr > 0.47)
    a = arr(im); a[256:512, 0:256] = rings[..., None]; a[256:512, 0:256][rr < 0.47] = (0.62, 0.52, 0.38)  # cardboard core face
    im = img(a); d = ImageDraw.Draw(im)
    d.rectangle([300, 300, 500, 500], fill=(158, 132, 96))  # core inside
    save_maps('EvidenceLabelRoll', im, None, 0.0, 0.4, ms_px=128)
    m = mk.Mesh()
    ro, ri, h = 0.040, 0.019, 0.050
    WRAP = R(0, 0, 512, 200, S, S); FACE = (0.0, 0.0, 0.5, 0.5); CORE = R(300, 300, 500, 500, S, S)
    P, UV, N = mk.lathe([(ro, 0), (ro, h)], 36, uv=WRAP); m.add('Roll', P, UV, N)
    P, UV, N = mk.lathe([(ri, h), (ri, 0)], 24, uv=CORE); m.add('Roll', P, UV, N)
    for yv, up in ((h, True), (0.0, False)):
        ang = np.linspace(0, 2 * math.pi, 37)
        Pf, Uf = [], []
        for i in range(36):
            a0, a1 = ang[i], ang[i + 1]
            quad = [(ri * math.cos(a0), yv, -ri * math.sin(a0)), (ro * math.cos(a0), yv, -ro * math.sin(a0)),
                    (ro * math.cos(a1), yv, -ro * math.sin(a1)), (ri * math.cos(a1), yv, -ri * math.sin(a1))]
            uvq = [(0.25 + 0.25 * (ri / ro) * math.cos(a0), 0.25 + 0.25 * (ri / ro) * math.sin(a0)), (0.25 + 0.25 * math.cos(a0), 0.25 + 0.25 * math.sin(a0)),
                   (0.25 + 0.25 * math.cos(a1), 0.25 + 0.25 * math.sin(a1)), (0.25 + 0.25 * (ri / ro) * math.cos(a1), 0.25 + 0.25 * (ri / ro) * math.sin(a1))]
            for tri in ((0, 1, 2), (0, 2, 3)):
                Pf.append([quad[j] for j in tri]); Uf.append([uvq[j] for j in tri])
        Pf = np.array(Pf); Uf = np.array(Uf)
        Pf, Uf = orient(Pf, Uf, (0, 1 if up else -1, 0))
        Nf = np.zeros_like(Pf); Nf[..., 1] = 1 if up else -1
        m.add('Roll', Pf, Uf, Nf)
    # loose leading label, peeling tangentially at the front
    P, UV, N = mk.box((0.0003, h - 0.004, 0.030), uvs={k: WRAP for k in SIX})
    P = P + [0, h / 2, 0.015]
    P, N = mk.xform(P, N, mk.chain(mk.T(ro + 0.0002, 0, 0.0), mk.Ry(-8)))
    m.add('Roll', P, UV, N)
    finish('EvidenceLabelRoll', m, {'Roll': dict(map='EvidenceLabelRoll_Color.jpg', Ks=0.1)}, (0.080, 0.050, 0.080),
           'Stands on its flat face (axis Y); the outer wrap shows the next labels, a leading label peels at +x.')


# ============================================================================== 10. biohazard bin
def biohazard_bin():
    W, H = 1024, 512
    yellow = np.array([0.95, 0.78, 0.08]); red = np.array([0.75, 0.08, 0.06])
    col = np.ones((H, W, 3), np.float32) * yellow
    n = mk.noise(H, W, 10, 231, 3); col += (n[..., None] - 0.5) * 0.03
    sm = np.full((H, W), 0.5, np.float32)
    col[:, 768:] = red; col[:, 768:] += (n[:, 768:, None] - 0.5) * 0.03
    im = img(col); d = ImageDraw.Draw(im)
    # cols 0..640, rows 0..512: front face (170 x 220 mm at 3.76 px/mm... use 640 x 512 for 0.17 x 0.22 -> 3.76 / 2.33; fine for flat colour)
    ink = (20, 20, 20)
    biohazard(d, 320, 150, 170, ink)
    d.text((320, 262), 'BIOHAZARD', font=font('sans-bold', 54), fill=ink, anchor='mm')
    d.text((320, 306), 'SHARPS AND INFECTIOUS WASTE', font=font('sans-bold', 24), fill=ink, anchor='mm')
    ar(d, (560, 345), 'خطر بيولوجي - نفايات حادة', 26, ink)
    d.text((320, 395), '5 L   -   DO NOT FILL ABOVE THE LINE', font=font('sans', 22), fill=ink, anchor='mm')
    d.text((320, 430), 'Close the lid and lock it before transport. Dispose of as clinical waste.', font=font('sans', 16), fill=ink, anchor='mm')
    d.text((320, 460), 'Unit 7  -  opened: ______  closed: ______', font=font('sans', 16), fill=ink, anchor='mm')
    # fill line on the plain side region (cols 640..768) at 75 % of the height
    d.line([(640, 128), (767, 128)], fill=ink, width=4); d.text((650, 100), 'MAX', font=font('sans-bold', 20), fill=ink)
    # lid region: slot and lock text
    d.text((896, 40), 'PUSH TO CLOSE', font=font('sans-bold', 18), fill=(255, 230, 230), anchor='mm')
    d.rectangle([800, 300, 990, 500], fill=(30, 10, 10))  # aperture interior
    save_maps('BiohazardBinSmall', im, None, 0.0, sm, ms_px=256)
    m = mk.Mesh()
    FRONT = R(0, 0, 640, 512, W, H); SIDE = R(640, 0, 768, 512, W, H); LID = R(768, 0, 1024, 280, W, H); HOLE = R(800, 300, 990, 500, W, H)
    # body: loft of rounded rectangles, base 0.14 x 0.10 -> top 0.17 x 0.125 at 0.22
    rings = []
    for (w, dd, y, r) in ((0.136, 0.096, 0.0, 0.014), (0.140, 0.100, 0.004, 0.015), (0.150, 0.108, 0.07, 0.016), (0.160, 0.117, 0.14, 0.017),
                          (0.170, 0.125, 0.21, 0.018), (0.170, 0.125, 0.22, 0.018), (0.165, 0.121, 0.226, 0.016)):
        rings.append(rrect(w, dd, r, 4, y))
    P, UV, N = loft(rings, close=True, smooth=50)
    UV = np.array(UV)
    fc = P.mean(1); fn = N.mean(1)
    front = (fn[:, 2] > 0.75)
    for f in range(len(P)):
        if front[f]:
            UV[f, :, 0] = FRONT[0] + (FRONT[2] - FRONT[0]) * np.clip((P[f, :, 0] + 0.085) / 0.17, 0, 1)
            UV[f, :, 1] = FRONT[1] + (FRONT[3] - FRONT[1]) * np.clip(P[f, :, 1] / 0.22, 0, 1)
        else:
            UV[f, :, 0] = SIDE[0] + (SIDE[2] - SIDE[0]) * 0.5; UV[f, :, 1] = SIDE[1] + (SIDE[3] - SIDE[1]) * np.clip(P[f, :, 1] / 0.22, 0, 1)
    m.add('BinAtlas', P, UV, N)
    ring0 = [(p[0], p[2]) for p in rings[0]]
    P, UV, N = cap_poly(ring0, 0.0, False, SIDE); m.add('BinAtlas', P, UV, N)
    # red lid with a dark aperture and a hinged flap
    P, UV, N = mk.rounded_box((0.180, 0.030, 0.135), 0.010, 2, uv_rects={k: LID for k in SIX}); m.add('BinAtlas', P + [0, 0.226 + 0.015, 0], UV, N)
    P, UV, N = mk.box((0.070, 0.004, 0.036), uvs={k: HOLE for k in SIX}); m.add('BinAtlas', P + [0, 0.256 + 0.001, 0.010], UV, N)
    P, UV, N = mk.box((0.074, 0.003, 0.040)); P = P + [0, 0.0015, 0.020]
    P, N = mk.xform(P, N, mk.chain(mk.T(0, 0.258, -0.010), mk.Rx(-40)))
    m.add('BinAtlas', P, const_uv(UV, LID), N)
    finish('BiohazardBinSmall', m, {'BinAtlas': dict(map='BiohazardBinSmall_Color.jpg', Ks=0.3)}, (0.18, 0.26, 0.135),
           'Upright, label on the +z face; red lid with an open flap over the aperture.')


# ============================================================================== 11. stepping plate
def stepping_plate():
    m = mk.Mesh()
    add_box(m, 'Polycarbonate', (0.35, 0.006, 0.35), (0, 0.020 + 0.003, 0), (0, 0, 1, 1))
    for sx in (-1, 1):
        for sz in (-1, 1):
            P, UV, N = mk.lathe([(0.0, 0.0), (0.016, 0.0), (0.016, 0.018), (0.013, 0.020), (0.0, 0.020)], 16)
            m.add('RubberFoot', P + [sx * 0.135, 0, sz * 0.135], UV, N)
    finish('SteppingPlate', m, {'Polycarbonate': dict(Kd=(0.86, 0.90, 0.92), d=0.30, Ks=0.9, Ns=900), 'RubberFoot': dict(Kd=(0.06, 0.06, 0.06), Ks=0.05)},
           (0.35, 0.026, 0.35), 'Flat plate on four 20 mm feet. Polycarbonate needs the glass material (Fade, alpha 0.3).')


# ============================================================================== 12. ALS goggles
def goggles():
    S = 512
    col = np.ones((S, S, 3), np.float32) * 0.05
    n = mk.noise(S, S, 10, 241, 3); col += (n[..., None] - 0.5) * 0.02
    hgt = n * 0.2; sm = np.full((S, S), 0.3, np.float32)
    # vent slots in the frame band (rows 0..256), elastic weave in the strap band (rows 256..512)
    for k in range(18):
        x = 20 + k * 27
        hgt[80:176, x:x + 8] = 1.0; col[80:176, x:x + 8] = 0.02
    weave = (np.sin(np.arange(S)[None, :] / S * 2 * math.pi * 60) * np.sin(np.arange(S)[:, None] / S * 2 * math.pi * 24)) * 0.5 + 0.5
    hgt[256:] = weave[256:]; col[256:] = 0.08 + 0.05 * weave[256:, :, None]; sm[256:] = 0.15
    save_maps('OrangeGoggles', col, hgt, 2.0, 0.0, sm, n_px=256, ms_px=128)
    FRAME = (0, 0.5, 1, 1); STRAP = (0, 0, 1, 0.5)
    m = mk.Mesh()
    W, Hh, rc = 0.150, 0.065, 0.022
    # frame: tube swept along a rounded rectangle in x/y, bent back toward -z at the sides
    pts = rrect(W, Hh, rc, 6)[:, [0, 2, 1]]  # x, y(from z), 0
    pts = np.vstack([pts, pts[:1]])
    pts[:, 2] = -0.020 * (pts[:, 0] / (W / 2)) ** 2
    P, UV, N = mk.tube(pts, 0.0065, 10, uv=FRAME, closed_ends=False); m.add('Frame', P, UV, N)
    # nose bridge block and the lens (bent plate inside the frame)
    add_box(m, 'Frame', (0.030, 0.012, 0.012), (0, -0.018, -0.004), FRAME)
    def lens_y(X, Z):
        return X * 0  # placeholder (slab works in x/z); built below
    xs = np.linspace(-W / 2 + 0.004, W / 2 - 0.004, 25); ys = np.linspace(-Hh / 2 + 0.004, Hh / 2 - 0.004, 9)
    X, Y = np.meshgrid(xs, ys)
    # trim to the rounded rectangle: clamp corners inward
    cx = np.clip(np.abs(X) - (W / 2 - rc - 0.004), 0, None); cy = np.clip(np.abs(Y) - (Hh / 2 - rc - 0.004), 0, None)
    rr = np.sqrt(cx ** 2 + cy ** 2); over = rr > (rc - 0.001)
    sc = np.where(over, (rc - 0.001) / (rr + 1e-9), 1.0)
    X = np.sign(X) * ((W / 2 - rc - 0.004) + cx * sc) * (np.abs(X) > (W / 2 - rc - 0.004)) + X * (np.abs(X) <= (W / 2 - rc - 0.004))
    Y = np.sign(Y) * ((Hh / 2 - rc - 0.004) + cy * sc) * (np.abs(Y) > (Hh / 2 - rc - 0.004)) + Y * (np.abs(Y) <= (Hh / 2 - rc - 0.004))
    Z = -0.020 * (X / (W / 2)) ** 2 + 0.003
    G = np.stack([X, Y, Z], -1)
    UVg = np.stack([(X + W / 2) / W, (Y + Hh / 2) / Hh], -1)
    P, UV, _ = mk.grid_surface(G, UVg); P, UV = orient(P, UV, (0, 0, 1))
    Pb = P[:, ::-1].copy(); UVb = UV[:, ::-1].copy()
    m.add('LensOrange', P, UV, mk.smooth_normals(P, 80)); m.add('LensOrange', Pb, UVb, mk.smooth_normals(Pb, 80))
    # elastic strap: 25 mm band around the back (semi-ellipse in x/z)
    tt = np.linspace(0, math.pi, 25)
    sx = (W / 2 + 0.004) * np.cos(tt); sz = -0.020 - 0.085 * np.sin(tt)
    top = np.stack([sx, np.full_like(sx, 0.0125), sz], -1); bot = np.stack([sx, np.full_like(sx, -0.0125), sz], -1)
    G = np.stack([bot, top], 0); UVg = np.stack([np.stack([tt / math.pi, np.zeros_like(tt)], -1), np.stack([tt / math.pi, np.ones_like(tt) * 0.5], -1)], 0)
    P, UV, _ = mk.grid_surface(G, UVg)
    Pb = P[:, ::-1].copy(); UVb = UV[:, ::-1].copy()
    m.add('Frame', P, UV, mk.smooth_normals(P, 80)); m.add('Frame', Pb, UVb, mk.smooth_normals(Pb, 80))
    out = m.transformed(mk.T(0, Hh / 2 + 0.0065, 0))
    finish('OrangeGoggles', out, {'Frame': dict(map='OrangeGoggles_Color.jpg', Ks=0.2), 'LensOrange': dict(Kd=(1.0, 0.42, 0.04), d=0.55, Ks=0.9, Ns=900)},
           (0.163, 0.078, 0.108), 'Stands upright on the lower rim, lens facing +z, strap behind. LensOrange: Fade/Transparent, orange tint.')


# ============================================================================== 13. PPE: mask and shoe covers
def face_mask():
    S = 512
    blue = np.array([0.60, 0.76, 0.88])
    n = mk.noise(S, S, 3, 251, 2); fibre = mk.noise(S, S, 50, 252, 4)
    col = blue[None, None] * (0.94 + 0.10 * fibre[..., None]) * (0.97 + 0.05 * n[..., None])
    # three pleats across the mask (v): shading bands
    vv = np.arange(S)[:, None] / S
    pleat = np.abs(((vv * 3.6 + 0.2) % 1) - 0.5) * 2
    col[:256] *= (0.90 + 0.12 * pleat[:256])[..., None]  # rows 0..256 top (blue), 256..512 underside (white)
    col[256:] = np.array([0.95, 0.95, 0.94]) * (0.95 + 0.08 * fibre[256:, :, None])
    hgt = np.zeros((S, S), np.float32); hgt[:256] = pleat[:256] * np.ones((1, S)) * 0.6 + fibre[:256] * 0.2; hgt[256:] = fibre[256:] * 0.2
    save_maps('FaceMask', col, hgt, 2.0, 0.0, 0.12, n_px=256, ms_px=64)
    W, D = 0.175, 0.095
    def top(X, Z):
        vz = (Z + D / 2) / D
        pl = np.abs(((vz * 3.6 + 0.2) % 1) - 0.5) * 2
        return 0.004 + 0.003 * pl + 0.004 * (1 - (X / (W / 2)) ** 2) * (1 - (Z / (D / 2)) ** 2)
    P, UV, N = slab(W, D, 24, 16, top, lambda X, Z: X * 0 + 0.0005, uv_top=(0, 0.5, 1, 1), uv_bot=(0, 0, 1, 0.5), thickness=0.0015)
    m = mk.Mesh(); m.add('Mask', P, UV, N)
    # nose strip and ear loops
    add_box(m, 'Mask', (0.06, 0.0015, 0.004), (0, 0.0105, -D / 2 + 0.006), (0.5, 0.75, 0.52, 0.77))
    for s in (-1, 1):
        tt = np.linspace(0, 2 * math.pi, 28)
        path = np.stack([s * (W / 2 - 0.003 + 0.030 * (1 - np.cos(tt)) / 2 * 1.4), 0.0012 + 0.0004 * np.sin(tt * 3), 0.035 * np.sin(tt) * 0.9], -1)
        P, UV, N = mk.tube(path, 0.0012, 6, uv=(0.5, 0.25, 0.52, 0.27), closed_ends=False); m.add('EarLoop', P, UV, N)
    finish('FaceMask', m, {'Mask': dict(map='FaceMask_Color.jpg', Ks=0.02), 'EarLoop': dict(Kd=(0.95, 0.95, 0.95), Ks=0.05)},
           (0.26, 0.012, 0.095), 'Lies flat, pleated blue side up, nose strip at -z; ear loops spread to the sides.')


def shoe_covers():
    S = 512
    blue = np.array([0.18, 0.40, 0.86])
    n = mk.noise(S, S, 14, 261, 4); n2 = mk.noise(S, S, 3, 262, 2)
    col = blue[None, None] * (0.85 + 0.25 * n[..., None]) * (0.95 + 0.08 * n2[..., None])
    # elastic opening band at the top rows (v near 1): darker, gathered
    col[:40] *= 0.6
    hgt = n * 0.8 + n2 * 0.3; hgt[:40] = (np.sin(np.arange(S)[None, :] / S * 2 * math.pi * 70) * 0.5 + 0.5) * np.ones((40, 1))
    save_maps('ShoeCoverPair', col, hgt, 3.0, 0.0, 0.62 + 0.1 * (n - 0.5), n_px=512, ms_px=128)
    m = mk.Mesh()
    rng = np.random.default_rng(263)
    for k, (dx, dz, rot) in enumerate(((-0.075, 0.0, 12), (0.085, 0.01, -20))):
        prof = [(0.0, 0.0)] + [(0.065 * math.sin(t), 0.055 * (1 - math.cos(t)) / 2) for t in np.linspace(0.15, math.pi - 0.1, 11)] + [(0.0, 0.055)]
        P, UV, N = mk.lathe(prof, 28, uv=(0, 0, 1, 1), sx=2.3, sz=1.0); P = np.array(P)
        # crumple: low-frequency displacement, flattened to lie on the floor
        ang = np.arctan2(-P[..., 2], P[..., 0]); el = P[..., 1] / 0.055
        wob = 1 + 0.10 * np.sin(ang * 3 + k) + 0.06 * np.sin(ang * 7 + el * 9) + 0.05 * np.sin(el * 14 + ang * 2)
        P[..., 0] *= wob; P[..., 2] *= wob
        P[..., 1] *= (0.75 + 0.25 * np.cos(ang * 2 + 0.5)) * (1 - 0.35 * el ** 2)
        sub = mk.Mesh(); sub.add('Cover', P, UV, mk.smooth_normals(P, 80))
        m.merge(sub, mk.chain(mk.T(dx, 0, dz), mk.Ry(rot)))
    finish('ShoeCoverPair', m, {'Cover': dict(map='ShoeCoverPair_Color.jpg', Ks=0.4, Ns=150)}, (0.33, 0.055, 0.17),
           'Two crumpled overshoes on the floor; the gathered elastic opening is the darker band at the top.')


# ============================================================================== 14. trace specimens
def hair_specimen():
    S = 512
    a = np.zeros((S, S, 4), np.float32)
    # rows 0..512 x cols 0..154: tape strip (12 x 40 mm at 12.8 px/mm) semi-transparent; hairs drawn as dark curves under it
    a[:, :154] = (0.92, 0.92, 0.90, 0.30)
    im = Image.fromarray((a * 255).astype(np.uint8), 'RGBA'); d = ImageDraw.Draw(im)
    rng = np.random.default_rng(271)
    for k in range(5):
        x = 20 + rng.uniform(0, 110); y = rng.uniform(20, 120)
        pts = [(x, y)]
        for s in range(14):
            x += rng.uniform(-6, 8); y += rng.uniform(14, 28) * (0.6 + 0.4 * rng.random()); pts.append((x, y))
        colr = (40, 28, 20, 255) if k < 3 else (120, 90, 60, 255)
        d.line(pts, fill=colr, width=2 if k != 4 else 1, joint='curve')
    # a red fibre and a blue fibre
    d.line([(60, 60), (75, 120), (70, 190), (90, 260)], fill=(190, 30, 30, 255), width=1)
    d.line([(120, 300), (100, 360), (118, 420)], fill=(30, 60, 170, 255), width=1)
    # label sticker (opaque) cols 200..500, rows 20..180: "TRACE  H-3"
    d.rectangle([200, 20, 500, 180], fill=(250, 250, 245, 255))
    d.text((214, 30), 'TRACE  H-3', font=font('sans-bold', 36), fill=(30, 30, 30, 255))
    d.text((214, 80), 'hairs / fibres  -  door edge', font=font('sans', 20), fill=(30, 30, 30, 255))
    d.text((214, 112), 'Case ______  Date ______', font=font('sans', 20), fill=(30, 30, 30, 255))
    ar(d, (490, 160), 'شعر وألياف', 20, (30, 30, 30, 255))
    im.save(os.path.join(OUT, 'HairFibreSpecimen_Color.png'), optimize=True)
    sm = np.full((128, 128), 0.85, np.float32); mk.save_ms(os.path.join(OUT, 'HairFibreSpecimen_MetallicSmoothness.png'), np.zeros((128, 128)), sm)
    m = mk.Mesh()
    TAPE = R(0, 0, 154, 512, S, S); LABEL = R(200, 20, 500, 180, S, S); EDGE = R(190, 400, 192, 402, S, S)
    add_box(m, 'Acetate', (0.040, 0.0003, 0.040), (0, 0.00015, 0), EDGE)
    P, UV, N = mk.box((0.012, 0.0001, 0.040), uvs={'py': TAPE, 'ny': TAPE, 'px': EDGE, 'nx': EDGE, 'pz': EDGE, 'nz': EDGE})
    m.add('SpecimenTape', P + [-0.006, 0.00035, 0], UV, N)
    P, UV, N = mk.box((0.018, 0.0002, 0.0096), uvs={k: LABEL for k in SIX})
    m.add('SpecimenTape', P + [0.009, 0.0004, -0.013], UV, N)
    finish('HairFibreSpecimen', m, {'Acetate': dict(Kd=(0.92, 0.94, 0.96), d=0.35, Ks=0.8, Ns=600),
                                    'SpecimenTape': dict(map='HairFibreSpecimen_Color.png', Ks=0.5, Ns=300)}, (0.040, 0.0005, 0.040),
           'Nearly flat. Acetate: glass material. SpecimenTape: Standard Fade with the PNG alpha (tape 0.3, hairs opaque).')


def fibre_tuft():
    m = mk.Mesh()
    rng = np.random.default_rng(281)
    for k in range(16):
        a = rng.uniform(0, 2 * math.pi); L = rng.uniform(0.004, 0.0085)
        n = 7
        tt = np.linspace(0, 1, n)
        dirx, dirz = math.cos(a), math.sin(a)
        wig = rng.uniform(0.0006, 0.0015)
        path = np.stack([tt * L * dirx + wig * np.sin(tt * 6 + k) * dirz, 0.00015 + 0.0012 * np.sin(tt * math.pi) * rng.uniform(0.2, 1.0) + 0.0003 * tt,
                         tt * L * dirz - wig * np.sin(tt * 6 + k) * dirx], -1)
        P, UV, N = mk.tube(path, 0.00015, 4, closed_ends=False); m.add('FibreRed', P, UV, N)
    finish('FibreTuft', m, {'FibreRed': dict(Kd=(0.78, 0.08, 0.10), Ks=0.1)}, (0.012, 0.002, 0.012),
           'Tiny tuft of red textile fibres; place on a glass edge or a snag point.')


# ============================================================================== 15. decal textures
def tread_mask(W, H, seed):
    """Outsole dust pattern (0..1), toe to the right. Hexagonal lugs in the forefoot, bars in the heel; different design
    from ShoePrintCast."""
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    u = xx / W; v = (yy - H / 2) / (H / 2)
    half = np.interp(u, [0.0, 0.05, 0.14, 0.24, 0.40, 0.50, 0.64, 0.80, 0.91, 0.97, 1.0],
                     [0.0, 0.52, 0.76, 0.78, 0.60, 0.56, 0.84, 0.98, 0.86, 0.5, 0.0])
    shift = 0.08 * np.sin(u * math.pi) - 0.04
    inside = np.abs(v - shift) < half
    tread = np.zeros((H, W), np.float32)
    heel = u < 0.32; fore = u > 0.46
    tread[heel] = (np.abs(np.sin(yy[heel] / H * 2 * math.pi * 9 + xx[heel] / W * 6)) > 0.35).astype(np.float32)
    hx = (xx / W * 26) % 1; hy = ((yy / H * 7.5) + 0.5 * np.floor(xx / W * 26)) % 1
    hexlug = ((np.abs(hx - 0.5) + 0.58 * np.abs(hy - 0.5)) < 0.42).astype(np.float32)
    tread[fore] = hexlug[fore]
    wave = np.abs(np.sin((xx / W + 0.08 * np.sin(v * 3)) * 2 * math.pi * 4.5)) < 0.05
    tread[fore & wave] = 0
    rim = inside & ~(np.abs(v - shift) < half - 0.09)
    tread[rim] = 1
    waist = ~heel & ~fore
    tread[waist] = (np.abs(np.sin(xx[waist] / W * 2 * math.pi * 30)) > 0.6).astype(np.float32) * 0.6
    tread[~inside] = 0
    return tread, inside


def footwear_decal():
    S = 1024
    tread, inside = tread_mask(900, 360, 291)
    full = np.zeros((S, S), np.float32); full[332:692, 62:962] = tread
    full = blur(full, 1.2)
    n = mk.noise(S, S, 25, 292, 4); n2 = mk.noise(S, S, 6, 293, 3)
    # dust transfer: uneven loading, missing toe area, fading heel edge
    xx = np.arange(S)[None, :] / S
    load = np.clip(0.55 + 0.9 * (n - 0.5), 0, 1) * np.clip((0.86 - xx) / 0.12, 0, 1) * np.clip((xx - 0.05) / 0.10, 0, 1)
    alpha = np.clip(full * load * (0.6 + 0.8 * n2), 0, 1) * 0.85
    # scuff streaks across the mark
    scuff = np.zeros((S, S), np.float32)
    rng = np.random.default_rng(294)
    im = Image.fromarray(scuff); d = ImageDraw.Draw(im)
    for k in range(5):
        x0, y0 = rng.uniform(150, 850), rng.uniform(380, 650); ln = rng.uniform(80, 220); ang = rng.uniform(-0.5, 0.5)
        d.line([(x0, y0), (x0 + ln * math.cos(ang), y0 + ln * math.sin(ang))], fill=1.0, width=int(rng.uniform(3, 9)))
    scuff = blur(np.asarray(im, np.float32), 1.5)
    alpha = np.clip(alpha * (1 - 0.6 * scuff) + 0.25 * scuff * (0.5 + 0.5 * n2), 0, 1)
    grey = np.array([0.72, 0.70, 0.66])
    rgb = grey[None, None] * (0.85 + 0.3 * n[..., None])
    out = np.dstack([rgb, alpha[..., None]])
    Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8), 'RGBA').save(os.path.join(OUT, 'FootwearDustMark_ColorAlpha.png'), optimize=True)
    Image.fromarray(mk.normal_from_height(blur(alpha, 1.0) * 0.4, 1.0)).save(os.path.join(OUT, 'FootwearDustMark_Normal.png'), optimize=True)
    STATS['FootwearDustMark'] = {'kind': 'decal', 'files': ['FootwearDustMark_ColorAlpha.png', 'FootwearDustMark_Normal.png'],
                                 'suggested_size_m': [0.32, 0.32], 'note': 'Partial trainer sole in grey dust, toe toward +u; mark spans 0.28 x 0.11 m at 0.32 m quad.'}
    print('%-22s decal 1024 px' % 'FootwearDustMark')


def pry_decal():
    S = 1024  # suggested quad 0.12 m -> 8.5 px/mm
    n = mk.noise(S, S, 20, 301, 4); n2 = mk.noise(S, S, 4, 302, 2)
    dep = np.zeros((S, S), np.float32); paint = np.zeros((S, S), np.float32)
    dm = Image.fromarray(dep); dd = ImageDraw.Draw(dm); pm = Image.fromarray(paint); pd = ImageDraw.Draw(pm)
    # two flat-blade impressions ~25 x 6 mm (212 x 50 px), overlapping at different angles; paint loss halo around each
    for (cx, cy, ang, w, h) in ((470, 520, -8, 212, 52), (560, 470, 22, 205, 48)):
        blade = Image.new('F', (S, S), 0.0); bd = ImageDraw.Draw(blade)
        bd.rounded_rectangle([cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2], 10, fill=1.0)
        blade = blade.rotate(ang, center=(cx, cy), resample=Image.BILINEAR)
        halo = Image.new('F', (S, S), 0.0); hd = ImageDraw.Draw(halo)
        hd.rounded_rectangle([cx - w / 2 - 28, cy - h / 2 - 34, cx + w / 2 + 28, cy + h / 2 + 34], 30, fill=1.0)
        halo = halo.rotate(ang, center=(cx, cy), resample=Image.BILINEAR)
        dep = np.maximum(dep, np.asarray(blade, np.float32)); paint = np.maximum(paint, np.asarray(halo, np.float32))
    # chipped irregular paint edge: threshold the halo with noise
    chip = np.clip((paint - 0.2) - 0.9 * (n - 0.45) * 1.6, 0, 1); chip = (blur(chip, 1.0) > 0.35).astype(np.float32)
    chip = np.maximum(chip, dep)
    # scratch streaks dragged from the blades
    sc = Image.new('F', (S, S), 0.0); sd = ImageDraw.Draw(sc)
    rng = np.random.default_rng(303)
    for k in range(14):
        x0, y0 = rng.uniform(380, 660), rng.uniform(460, 560); ln = rng.uniform(40, 160); a = math.radians(rng.uniform(-30, 30) + 90 * (k % 2))
        sd.line([(x0, y0), (x0 + ln * math.cos(a), y0 + ln * math.sin(a))], fill=rng.uniform(0.4, 1.0), width=int(rng.uniform(1, 3)))
    sc = blur(np.asarray(sc, np.float32), 0.6)
    alpha = np.clip(chip + 0.7 * sc, 0, 1)
    # colour: bare substrate inside the chip (primer grey / timber tan mix), darker crushed metal in the blade dents
    bare = np.array([0.62, 0.58, 0.50])[None, None] * (0.85 + 0.3 * n[..., None]) * (0.9 + 0.2 * n2[..., None])
    dent = np.array([0.35, 0.33, 0.30])
    rgb = bare * (1 - dep[..., None]) + dent * dep[..., None]
    rgb = rgb * (1 - 0.5 * sc[..., None]) + np.array([0.80, 0.78, 0.74]) * 0.5 * sc[..., None]
    out = np.dstack([rgb, alpha[..., None]])
    Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8), 'RGBA').save(os.path.join(OUT, 'PryMark_ColorAlpha.png'), optimize=True)
    height = -blur(dep, 2.0) * 1.0 - blur(chip, 1.0) * 0.25 - sc * 0.15
    Image.fromarray(mk.normal_from_height(height, 3.0)).save(os.path.join(OUT, 'PryMark_Normal.png'), optimize=True)
    STATS['PryMark'] = {'kind': 'decal', 'files': ['PryMark_ColorAlpha.png', 'PryMark_Normal.png'], 'suggested_size_m': [0.12, 0.12],
                        'note': 'Two overlapping flat-blade pry impressions with chipped paint and drag scratches; centre of the 1024 px texture.'}
    print('%-22s decal 1024 px' % 'PryMark')


# ============================================================================== 16. electrostatic lift film
def lift_film():
    S = 1024
    col = np.ones((S, S, 3), np.float32) * 0.03
    n = mk.noise(S, S, 60, 311, 4); col += (n[..., None] - 0.5) * 0.015
    tread, inside = tread_mask(640, 260, 312)
    full = np.zeros((S, S), np.float32); full[300:560, 230:870] = tread
    full = blur(full, 1.0)
    n2 = mk.noise(S, S, 20, 313, 4)
    xx = np.arange(S)[None, :] / S
    dust = np.clip(full * (0.35 + 0.9 * (n2 - 0.4)) * np.clip((0.80 - xx) / 0.15, 0, 1), 0, 1) * 0.55
    col = col * (1 - dust[..., None]) + np.array([0.70, 0.68, 0.64]) * dust[..., None]
    sm = 0.62 - 0.4 * dust
    # the film is 30 x 40 cm; texture: u along x (0.30), v along z (0.40): rotate the mark to run along the long axis
    col = np.transpose(col, (1, 0, 2))[::-1]; sm = sm.T[::-1]
    save_maps('ElectrostaticLiftFilm', col, None, 0.0, sm, ms_px=256)
    W, D = 0.30, 0.40
    def top(X, Z):
        return 0.0006 + 0.0006 * np.sin(X * 60 + 1) * np.sin(Z * 45) + 0.0012 * np.clip((np.abs(Z) - 0.17) / 0.03, 0, 1) ** 2
    P, UV, N = slab(W, D, 12, 16, top, thickness=0.0002)
    m = mk.Mesh(); m.add('LiftFilm', P, UV, N)
    finish('ElectrostaticLiftFilm', m, {'LiftFilm': dict(map='ElectrostaticLiftFilm_Color.jpg', Ks=0.4, Ns=200)}, (W, 0.003, D),
           'Nearly flat black film, dust mark up, long axis Z; place on the floor beside a scale.')


if __name__ == '__main__':
    magnetic_wand()
    gel_lifter('GelLifterBlack', True); gel_lifter('GelLifterWhite', False)
    backing_card('BackingCardWhite', False); backing_card('BackingCardBlack', True); backing_card('LiftCardWithPrint', False, with_print=True)
    hinge_lifter('HingeLifterBlack', True); hinge_lifter('HingeLifterWhite', False)
    swab_tube(); swab_open(); swab_box()
    water_ampoule(); km_kit()
    mikrosil_tube('MikrosilTube', 0.150, 0.021, 'SILICONE CASTING PUTTY', ['BROWN  -  7 oz (198 g)', 'Tool-mark and lifted-print casting',
                  'Mix 1:1 with the catalyst on the pad;', 'apply within 2 min, cure 5-8 min.', 'Fictional Lab Supply  lot 2609-21'], (0.40, 0.23, 0.13))
    mikrosil_tube('MikrosilCatalyst', 0.075, 0.0095, 'CATALYST', ['Hardener for silicone putty', '15 g  -  use 1:1', 'Lot 2609-21'], (0.92, 0.92, 0.90), S=512)
    mixing_spatula(); toolmark_cast()
    mixing_bowl(); casting_frame()
    evidence_tag(); label_roll()
    biohazard_bin()
    stepping_plate()
    goggles()
    face_mask(); shoe_covers()
    hair_specimen(); fibre_tuft()
    footwear_decal(); pry_decal()
    lift_film()
    json.dump(STATS, open(os.path.join(OUT, 'stats_csi.json'), 'w'), indent=1)
    print('arabic lines:', 'rendered' if AR_OK else 'skipped (font or raqm missing)')
