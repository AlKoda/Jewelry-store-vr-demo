"""Glass debris: GlassShardsScatter, TemperedGlassCrumbs, GlassShardsLarge, GlassCrackDecal (project-made)."""
import sys, math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
import meshkit as mk

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
GLASS = {
    'GlassFace': dict(Kd=(0.86, 0.90, 0.89), d=0.22, Ks=0.9, Ns=900),
    'GlassEdge': dict(Kd=(0.42, 0.62, 0.55), d=0.75, Ks=0.6, Ns=600),
}


def shard_polygon(rng, size, aspect, spikes=True):
    k = rng.integers(4, 8)
    ang = np.sort(rng.uniform(0, 2 * math.pi, k))
    rad = rng.uniform(0.45, 1.0, k)
    if spikes:
        i = rng.integers(0, k); rad[i] = rng.uniform(1.0, 1.6)  # one dagger point
    pts = np.stack([np.cos(ang) * rad * aspect, np.sin(ang) * rad], 1)
    pts *= size / (2 * np.max(np.linalg.norm(pts, axis=1)))
    return pts


def add_slab(mesh, poly, thick, M):
    top, bot, side = mk.extrude(poly, thick, uv_top=lambda x, z: (x * 2 + 0.5, z * 2 + 0.5), uv_side=(0, 0, 1, 1),
                                y0=-thick / 2)
    sub = mk.Mesh()
    sub.add('GlassFace', *top); sub.add('GlassFace', *bot); sub.add('GlassEdge', *side)
    mesh.merge(sub, M)


def scatter(seed=7):
    rng = np.random.default_rng(seed)
    mesh = mk.Mesh()
    n = 230
    for i in range(n):
        u = rng.random()
        size = 0.012 + 0.07 * u ** 2.5 + (0.10 * rng.random() if rng.random() < 0.07 else 0)  # mostly small
        poly = shard_polygon(rng, size, rng.uniform(1.0, 3.0))
        r = min(abs(rng.normal(0, 0.17)), 0.42); a = rng.uniform(0, 2 * math.pi)
        x, z = r * math.cos(a) * 1.25, r * math.sin(a) * 0.85
        thick = 0.006
        y = thick / 2 + rng.uniform(0, 0.0025) + (0.004 if rng.random() < 0.15 else 0)
        M = mk.chain(mk.T(x, y, z), mk.Ry(rng.uniform(0, 360)), mk.Rx(rng.normal(0, 4)), mk.Rz(rng.normal(0, 4)))
        add_slab(mesh, poly, thick, M)
    return mesh


def crumbs(seed=11):
    rng = np.random.default_rng(seed)
    mesh = mk.Mesh()
    n = 760
    for i in range(n):
        r = min(abs(rng.normal(0, 0.11)), 0.34); a = rng.uniform(0, 2 * math.pi)
        x, z = r * math.cos(a) * 1.2, r * math.sin(a)
        layer = 0 if rng.random() > math.exp(-(r / 0.09) ** 2) * 0.7 else 1
        sx, sz = rng.uniform(0.006, 0.013, 2); t = 0.008  # 8 mm toughened glass, dice-like fragments
        # distorted box: jitter corners
        P, UV, N = mk.box((sx, t, sz))
        jit = rng.normal(0, 0.0012, (8, 3)); jit[:, 1] *= 0.25
        corners = {}
        Pj = P.copy()
        for f in range(len(P)):
            for c in range(3):
                key = tuple(np.sign(P[f, c]).astype(int))
                if key not in corners:
                    corners[key] = jit[len(corners) % 8]
                Pj[f, c] = P[f, c] + corners[key]
        y = t / 2 + layer * t * 0.9 + rng.uniform(0, 0.001)
        M = mk.chain(mk.T(x, y, z), mk.Ry(rng.uniform(0, 360)), mk.Rx(rng.normal(0, 6 + 10 * layer)),
                     mk.Rz(rng.normal(0, 6 + 10 * layer)))
        sub = mk.Mesh()
        # top/bottom = original glass surfaces, the four fracture sides = edge material
        for f in range(len(Pj)):
            fn = np.cross(Pj[f, 1] - Pj[f, 0], Pj[f, 2] - Pj[f, 0])
            mat = 'GlassFace' if abs(fn[1]) > 0.7 * np.linalg.norm(fn) else 'GlassEdge'
            sub.add(mat, Pj[f:f + 1], UV[f:f + 1] * 0.05 + rng.random(2) * 0.9)
        mesh.merge(sub, M)
    return mesh


def fracture_cells(rng, w, h, impact, rays=10, rings=(0.05, 0.12, 0.22)):
    """Radial + concentric fracture of a w x h pane; returns list of polygons (x, z)."""
    ang = np.sort(rng.uniform(0, 2 * math.pi, rays))
    ang += np.linspace(0, 0, rays)
    cells = []
    hw, hh = w / 2, h / 2

    def ray_rect(a):
        d = np.array([math.cos(a), math.sin(a)])
        ts = []
        for t in ((hw - impact[0]) / d[0] if d[0] > 1e-9 else np.inf, (-hw - impact[0]) / d[0] if d[0] < -1e-9 else np.inf,
                  (hh - impact[1]) / d[1] if d[1] > 1e-9 else np.inf, (-hh - impact[1]) / d[1] if d[1] < -1e-9 else np.inf):
            ts.append(t)
        return np.array(impact) + d * min(ts), min(ts)

    ringj = [[r * rng.uniform(0.8, 1.25) for _ in range(rays)] for r in rings]
    corners = [(hw, hh), (-hw, hh), (-hw, -hh), (hw, -hh)]
    for i in range(rays):
        a0 = ang[i]; a1 = ang[(i + 1) % rays] + (2 * math.pi if i == rays - 1 else 0)
        prev0 = np.array(impact); prev1 = np.array(impact)
        for j, rr in enumerate(ringj):
            r0 = min(rr[i], ray_rect(a0)[1] * 0.95); r1 = min(rr[(i + 1) % rays], ray_rect(a1)[1] * 0.95)
            p0 = np.array(impact) + r0 * np.array([math.cos(a0), math.sin(a0)])
            p1 = np.array(impact) + r1 * np.array([math.cos(a1), math.sin(a1)])
            mid_a = (a0 + a1) / 2; mid_r = (r0 + r1) / 2 * rng.uniform(1.0, 1.12)
            pm = np.array(impact) + mid_r * np.array([math.cos(mid_a), math.sin(mid_a)])
            if j == 0:
                poly = [prev0, p0, pm, p1]
            else:
                poly = [prev0, p0, pm, p1, prev1, prev_mid]
            cells.append(np.array(poly))
            prev0, prev1, prev_mid = p0, p1, pm
        # outer cell to the rectangle border
        e0, _ = ray_rect(a0); e1, _ = ray_rect(a1)
        poly = [prev0, e0]
        cs = []
        for c in corners:
            ca = math.atan2(c[1] - impact[1], c[0] - impact[0])
            while ca < a0:
                ca += 2 * math.pi
            if ca < a1:
                cs.append((ca, c))
        poly += [np.array(c) for _, c in sorted(cs)]
        poly += [e1, prev1, prev_mid]
        cells.append(np.array(poly))
    return cells, ang, ringj


def jag(poly, rng, amp=0.004, step=0.03):
    """Add small irregularities along long edges."""
    out = []
    for i in range(len(poly)):
        a = poly[i]; b = poly[(i + 1) % len(poly)]
        out.append(a)
        L = np.linalg.norm(b - a)
        k = int(L / step)
        if k >= 1:
            nrm = np.array([-(b - a)[1], (b - a)[0]]) / (L + 1e-12)
            for t in np.linspace(0, 1, k + 2)[1:-1]:
                out.append(a + (b - a) * t + nrm * rng.normal(0, amp))
    out = np.array(out)
    # drop near-duplicates
    keep = [0]
    for i in range(1, len(out)):
        if np.linalg.norm(out[i] - out[keep[-1]]) > 1e-4:
            keep.append(i)
    return out[keep]


def large(seed=5):
    rng = np.random.default_rng(seed)
    cells, _, _ = fracture_cells(rng, 0.9, 0.6, (0.06, -0.03), rays=9, rings=(0.06, 0.16))
    mesh = mk.Mesh()
    # keep the larger outer and middle cells, scatter them as they would lie after falling into a case bed
    areas = [abs(mk.area2(c)) for c in cells]
    order = np.argsort(areas)[::-1]
    chosen = [cells[i] for i in order[:11]]
    for k, c in enumerate(chosen):
        c = jag(c, rng)
        cen = c.mean(0)
        poly = c - cen
        if mk.area2(poly) < 0:
            poly = poly[::-1]
        thick = 0.006
        lift = thick / 2 + (0.007 if k % 4 == 3 else 0.0) + rng.uniform(0, 0.002)
        M = mk.chain(mk.T(cen[0] * 1.15 + rng.normal(0, 0.03), lift, cen[1] * 1.15 + rng.normal(0, 0.03)),
                     mk.Ry(rng.normal(0, 12)), mk.Rx(rng.normal(0, 3)), mk.Rz(rng.normal(0, 3)))
        add_slab(mesh, poly, thick, M)
    return mesh


def crack_decal(seed=3, S=2048, out_px=1024):
    """RGBA crack overlay for intact-but-cracked panes: radial + concentric cracks around an impact point."""
    rng = np.random.default_rng(seed)
    img = Image.new('L', (S, S), 0)
    d = ImageDraw.Draw(img)
    c = np.array([S * 0.5, S * 0.5])
    rays = 23
    angs = np.linspace(0, 2 * math.pi, rays, endpoint=False) + rng.uniform(-0.12, 0.12, rays)
    ends = []
    for a in angs:
        L = S * rng.uniform(0.28, 0.5)
        p = c.copy(); dirv = np.array([math.cos(a), math.sin(a)])
        pts = [tuple(p)]
        steps = int(L / 14)
        for s in range(steps):
            dirv = mk.rotate_vec(np.array([dirv[0], dirv[1], 0.0]), np.array([0, 0, 1.0]), rng.normal(0, 0.012) + (rng.normal(0, 0.18) if rng.random() < 0.05 else 0))[:2]
            dirv /= np.linalg.norm(dirv)
            p = p + dirv * 14
            pts.append(tuple(p))
        w = 5
        for i in range(len(pts) - 1):
            ww = max(1, int(w * (1 - i / len(pts)) + 1))
            d.line([pts[i], pts[i + 1]], fill=int(235 - 120 * i / len(pts)), width=ww)
            # small side branches
            if rng.random() < 0.05:
                b = np.array(pts[i]); ba = math.atan2(pts[i + 1][1] - b[1], pts[i + 1][0] - b[0]) + rng.choice([-1, 1]) * rng.uniform(0.4, 1.0)
                q = b + np.array([math.cos(ba), math.sin(ba)]) * rng.uniform(30, 120)
                d.line([tuple(b), tuple(q)], fill=150, width=1)
        ends.append(pts)
    # concentric arcs between neighbouring rays
    for ring in (30, 55, 85, 120, 165, 220, 290, 370, 460):
        for i in range(rays):
            if rng.random() < 0.25 + (0.65 if ring < 180 else (0.3 if ring < 300 else 0)):
                A = ends[i]; B = ends[(i + 1) % rays]
                ka = min(len(A) - 1, int(ring * rng.uniform(0.8, 1.2) / 14)); kb = min(len(B) - 1, int(ring * rng.uniform(0.8, 1.2) / 14))
                pa = np.array(A[ka]); pb = np.array(B[kb])
                mid = (pa + pb) / 2; outv = mid - c; outv /= np.linalg.norm(outv) + 1e-9
                pm = mid - outv * np.linalg.norm(pa - pb) * rng.uniform(0.02, 0.1) + rng.normal(0, 3, 2)
                d.line([tuple(pa), tuple(pm), tuple(pb)], fill=int(rng.uniform(140, 210)), width=int(rng.integers(1, 4)), joint='curve')
    # crushed zone around the impact
    for k in range(260):
        r = abs(rng.normal(0, 26)); a = rng.uniform(0, 2 * math.pi)
        p = c + r * np.array([math.cos(a), math.sin(a)])
        q = p + rng.normal(0, 14, 2)
        d.line([tuple(p), tuple(q)], fill=int(rng.uniform(160, 255)), width=int(rng.integers(1, 3)))
    d.ellipse([c[0] - 9, c[1] - 9, c[0] + 9, c[1] + 9], fill=255)
    a = np.asarray(img.filter(ImageFilter.GaussianBlur(0.7)).resize((out_px, out_px), Image.LANCZOS), np.float32) / 255
    glow = np.asarray(img.filter(ImageFilter.GaussianBlur(6)).resize((out_px, out_px), Image.LANCZOS), np.float32) / 255
    alpha = np.clip(a * 1.15 + glow * 0.25, 0, 1)
    rgb = np.dstack([np.full_like(alpha, 0.93), np.full_like(alpha, 0.97), np.full_like(alpha, 0.96)])
    rgba = np.dstack([rgb, alpha])
    Image.fromarray((rgba * 255).astype(np.uint8), 'RGBA').save(os.path.join(OUT, 'GlassCrackDecal.png'), optimize=True)
    # matching normal map: cracks act as tiny ridges that catch reflections
    h = np.asarray(img.filter(ImageFilter.GaussianBlur(2.0)).resize((out_px, out_px), Image.LANCZOS), np.float32) / 255
    Image.fromarray(mk.normal_from_height(h, 3.0)).save(os.path.join(OUT, 'GlassCrackDecal_Normal.png'), optimize=True)


if __name__ == '__main__':
    stats = {}
    for name, fn in (('GlassShardsScatter', scatter), ('TemperedGlassCrumbs', crumbs), ('GlassShardsLarge', large)):
        m = fn().ground()
        t = m.write(os.path.join(OUT, name + '.obj'), GLASS)
        lo, hi = m.bounds()
        print(name, t, np.round(hi - lo, 3))
    crack_decal()
    print('decal done')
