"""Scene and burglary items for the ForensicModels pack (project-made geometry and textures, CC0):
PoliceBarrier, BrickHalf, ShoePrintCast, CameraTripod, PoliceCap, DuffelBag, Balaclava, EvidenceMarkerTriFold.
proc_scene.py OUT_DIR [name ...]"""
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
import meshkit as mk

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)


def report(name, m):
    lo, hi = m.bounds(); print(name, m.tris(), np.round(hi - lo, 4))


def rect(x0, y0, x1, y1, W=1024, H=1024):
    """Pixel rectangle (image rows down) -> (u0, v0, u1, v1) with OBJ v up."""
    return (x0 / W, 1 - y1 / H, x1 / W, 1 - y0 / H)


def blur(a, r):
    from scipy.ndimage import gaussian_filter
    a = np.asarray(a, np.float32)
    if a.ndim == 2:
        return gaussian_filter(a, r, mode='wrap')
    return np.dstack([gaussian_filter(a[..., k], r, mode='wrap') for k in range(a.shape[2])])


def grime(h, w, seed, scale=60):
    n = mk.noise(h, w, scale, seed, 5)
    return np.clip((n - 0.45) * 2.2, 0, 1)


def write_maps(name, col, height=None, strength=2.0, metal=None, smooth=None, ms_px=512, q=90, n_px=None):
    mk.save_color(os.path.join(OUT, name + '_Color.jpg'), col, q)
    if height is not None:
        nm = Image.fromarray(mk.normal_from_height(height, strength))
        if n_px:
            nm = nm.resize((n_px, n_px * nm.size[1] // nm.size[0]), Image.LANCZOS)
        nm.save(os.path.join(OUT, name + '_Normal.jpg'), quality=92, optimize=True)
    if smooth is not None:
        m = np.zeros_like(smooth) if metal is None else metal
        H, W = np.asarray(smooth).shape[:2]
        s = Image.fromarray((np.clip(smooth, 0, 1) * 255).astype(np.uint8)).resize((ms_px, ms_px * H // W), Image.LANCZOS)
        mm = Image.fromarray((np.clip(m, 0, 1) * 255).astype(np.uint8)).resize((ms_px, ms_px * H // W), Image.LANCZOS)
        mk.save_ms(os.path.join(OUT, name + '_MetallicSmoothness.png'), np.asarray(mm, np.float32) / 255, np.asarray(s, np.float32) / 255)


def set_uv(P, UV, region):
    """Planar-fill each triangle UV into a constant region centre (for small parts)."""
    u0, v0, u1, v1 = region
    UV = np.array(UV, float); UV[..., 0] = (u0 + u1) / 2; UV[..., 1] = (v0 + v1) / 2
    return UV


def box_part(m, mat, size, M, region, world_uv=None):
    P, UV, N = mk.box(size)
    u0, v0, u1, v1 = region
    if world_uv:
        # metric mapping inside the region: world_uv = metres per region width
        P0 = P.copy()
        for f in range(len(P)):
            n = np.abs(N[f, 0]); ax = [i for i in range(3) if n[i] < 0.5]
            q = P0[f][:, ax] - P0[f][:, ax].min(0) * 0 + np.array(size)[ax] / 2
            UV[f, :, 0] = u0 + (u1 - u0) * np.clip(q[:, 0] / world_uv, 0, 1)
            UV[f, :, 1] = v0 + (v1 - v0) * np.clip(q[:, 1] / world_uv, 0, 1)
    else:
        UV[..., 0] = u0 + (u1 - u0) * UV[..., 0]; UV[..., 1] = v0 + (v1 - v0) * UV[..., 1]
    sub = mk.Mesh(); sub.add(mat, P, UV, N); m.merge(sub, M)


# ======================================================================================= police barrier
def police_barrier():
    S = 1024
    col = np.zeros((S, S, 3), np.float32); hgt = np.zeros((S, S), np.float32)
    met = np.zeros((S, S), np.float32); smo = np.zeros((S, S), np.float32)
    # --- plank art: rows 0..170 front, 180..350 back (1.2 x 0.2 m -> 1024 x 170 px)
    def plank_art(y0, seed):
        im = Image.new('RGB', (S, 170), (238, 240, 236)); d = ImageDraw.Draw(im)
        blue = (18, 52, 132)
        for k in range(-6, 40):
            x = k * 64
            d.polygon([(x, 170), (x + 32, 170), (x + 32 + 170, 0), (x + 170, 0)], fill=blue)
        # centre panel
        d.rectangle([292, 22, 732, 148], fill=(240, 242, 238)); d.rectangle([292, 22, 732, 148], outline=blue, width=6)
        f = mk.font('sans-bold', 78); fs = mk.font('sans-bold', 25)
        for txt, fnt, cy in (('POLICE', f, 70), ('DO NOT CROSS', fs, 128)):
            bb = d.textbbox((0, 0), txt, font=fnt)
            d.text((512 - (bb[2] - bb[0]) / 2 - bb[0], cy - (bb[3] - bb[1]) / 2 - bb[1]), txt, font=fnt, fill=blue)
        a = np.asarray(im, np.float32) / 255
        # retroreflective sheeting: fine prism cells + dirt, scuffs along the bottom edge
        yy, xx = np.mgrid[0:170, 0:S]
        cells = (np.sin(xx * 0.9) * np.sin((yy + xx * 0.5) * 0.9)) * 0.5 + 0.5
        g = grime(170, S, seed, 40)
        a = a * (1 - 0.18 * g[..., None] * np.array([0.6, 0.7, 0.9]))
        bottom = np.clip((yy - 120) / 50, 0, 1) * mk.noise(170, S, 12, seed + 1, 4)
        a = a * (1 - 0.35 * bottom[..., None]) + np.array([0.20, 0.17, 0.12]) * 0.35 * bottom[..., None]
        scratches = np.zeros((170, S), np.float32)
        rng = np.random.default_rng(seed)
        sc = Image.new('L', (S, 170), 0); ds = ImageDraw.Draw(sc)
        for _ in range(70):
            x, y = rng.uniform(0, S), rng.uniform(0, 170); L = rng.uniform(8, 60); an = rng.normal(0, 0.25)
            ds.line([(x, y), (x + L * math.cos(an), y + L * math.sin(an))], fill=int(rng.uniform(60, 200)), width=1)
        scratches = np.asarray(sc, np.float32) / 255
        a = a * (1 - 0.25 * scratches[..., None]) + 0.25 * scratches[..., None] * 0.85
        col[y0:y0 + 170] = a
        hgt[y0:y0 + 170] = cells * 0.15 + g * 0.3 - scratches * 0.6
        smo[y0:y0 + 170] = 0.72 - 0.3 * g - 0.3 * bottom
    plank_art(0, 3); plank_art(180, 4)
    # plank edges (white plastic): rows 360..400
    e = mk.noise(40, S, 20, 9, 3)
    col[360:400] = (0.86 + 0.06 * e)[..., None] * np.array([1, 1, 0.98]); smo[360:400] = 0.45
    # painted galvanised steel: rows 420..1024 (left: white powder coat, right: zinc)
    n = mk.noise(604, S, 50, 12, 5); g = grime(604, S, 13, 80); r = mk.noise(604, S, 8, 14, 3)
    paint = np.array([0.80, 0.80, 0.77]) * (0.94 + 0.08 * n[..., None])
    paint = paint * (1 - 0.35 * g[..., None]) + np.array([0.25, 0.22, 0.18]) * 0.35 * g[..., None]
    chips = (r > 0.78) & (n > 0.55)
    zinc = np.array([0.58, 0.60, 0.61]) * (0.9 + 0.15 * r[..., None])
    left = paint.copy(); left[chips] = zinc[chips]
    col[420:, :512] = left[:, :512]; col[420:, 512:] = zinc[:, 512:] * (1 - 0.3 * g[:, 512:, None])
    met[420:, 512:] = 0.9; met[420:, :512][chips[:, :512]] = 0.9
    smo[420:, :512] = 0.42 - 0.2 * g[:, :512]; smo[420:, 512:] = 0.55 - 0.25 * g[:, 512:]
    hgt[420:] = n * 0.4 + g * 0.3 - chips * 0.6
    write_maps('PoliceBarrier', col, hgt, 2.0, met, smo, 512)
    FRONT = rect(0, 0, 1024, 170); BACK = rect(0, 180, 1024, 350); EDGE = rect(0, 360, 1024, 400)
    PAINT = rect(0, 420, 512, 1024); ZINC = rect(512, 420, 1024, 1024)
    m = mk.Mesh()
    L, Hp, T = 1.20, 0.20, 0.022
    top = 1.02
    P, UV, N = mk.box((L, Hp, T))
    for f in range(len(P)):
        nz, ny = N[f, 0, 2], N[f, 0, 1]
        u = (P[f, :, 0] + L / 2) / L; v = (P[f, :, 1] + Hp / 2) / Hp
        if nz > 0.5:
            r_ = FRONT; UV[f, :, 0] = r_[0] + (r_[2] - r_[0]) * u; UV[f, :, 1] = r_[1] + (r_[3] - r_[1]) * v
        elif nz < -0.5:
            r_ = BACK; UV[f, :, 0] = r_[0] + (r_[2] - r_[0]) * (1 - u); UV[f, :, 1] = r_[1] + (r_[3] - r_[1]) * v
        else:
            r_ = EDGE; UV[f, :, 0] = r_[0] + (r_[2] - r_[0]) * u; UV[f, :, 1] = r_[1] + (r_[3] - r_[1]) * 0.5
    m.add('Barrier', P + np.array([0, top - Hp / 2, 0]), UV, N)
    # A-frame ends (square steel tube 30 mm), legs splay to +-0.30 m at the floor
    for xe in (-0.50, 0.50):
        for zs in (1, -1):
            dz = 0.30 * zs; Lg = math.hypot(dz, top + 0.01)
            ang = math.degrees(math.atan2(dz, top + 0.01))
            M = mk.chain(mk.T(xe, 0, 0), mk.T(0, top + 0.01, 0), mk.Rx(ang), mk.T(0, -Lg / 2, 0))
            box_part(m, 'Barrier', (0.030, Lg, 0.030), M, PAINT)
            # rubber foot pad
            box_part(m, 'Barrier', (0.07, 0.012, 0.09), mk.T(xe, 0.006, dz), ZINC)
        # top bracket and lower spreader bar
        box_part(m, 'Barrier', (0.06, 0.05, 0.07), mk.T(xe, top + 0.005, 0), ZINC)
        sp = 0.30 * (1 - 0.40 / (top + 0.01))
        box_part(m, 'Barrier', (0.022, 0.022, 2 * sp + 0.02), mk.T(xe, 0.40, 0), ZINC)
    # lower rail (round tube) between the ends at the front legs
    zr = 0.30 * (1 - 0.55 / (top + 0.01))
    for zs in (1, -1):
        P, UV, N = mk.tube([(-0.5, 0.55, zr * zs), (0.5, 0.55, zr * zs)], 0.013, 10)
        UV = set_uv(P, UV, ZINC); m.add('Barrier', P, UV, N)
    m.ground()
    m.write(os.path.join(OUT, 'PoliceBarrier.obj'), {'Barrier': dict(map='PoliceBarrier_Color.jpg', Ks=0.15)})
    report('PoliceBarrier', m)


# ======================================================================================= half brick
def displaced_box(size, res, disp, region_of_face):
    """Box whose faces are res-subdivided grids, vertices pushed along their normal by disp(p, n)."""
    h = np.array(size) / 2
    out = mk.Mesh()
    faces = {'px': (0, 1, 2, 1), 'nx': (0, -1, 2, 1), 'py': (1, 1, 0, 2), 'ny': (1, -1, 0, 2), 'pz': (2, 1, 0, 1), 'nz': (2, -1, 0, 1)}
    for name, (ax, sg, ua, va) in faces.items():
        nu = max(2, int(res * size[ua] / max(size))); nv = max(2, int(res * size[va] / max(size)))
        A = np.linspace(-h[ua], h[ua], nu + 1); B = np.linspace(-h[va], h[va], nv + 1)
        G = np.zeros((nv + 1, nu + 1, 3)); G[..., ax] = sg * h[ax]; G[..., ua] = A[None]; G[..., va] = B[:, None]
        n = np.zeros(3); n[ax] = sg
        G = G + disp(G, n)[..., None] * n
        u0, v0, u1, v1 = region_of_face[name]
        UVg = np.stack([u0 + (u1 - u0) * (G[..., ua] + h[ua]) / (2 * h[ua]), v0 + (v1 - v0) * (G[..., va] + h[va]) / (2 * h[va])], -1)
        P, UV, _ = mk.grid_surface(G, UVg)
        fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
        if np.mean(fn @ n) < 0:
            P = P[:, ::-1]; UV = UV[:, ::-1]
        out.add('m', P, UV)
    return out


def brick_half():
    S = 1024
    rng = np.random.default_rng(21)
    n1 = mk.noise(S, S, 120, 21, 5); n2 = mk.noise(S, S, 14, 22, 4); n3 = mk.noise(S, S, 4, 23, 3)
    base = np.array([0.50, 0.25, 0.18])
    col = base * (0.80 + 0.35 * n1[..., None]) * (0.92 + 0.12 * n2[..., None])
    # firing flash (darker purple-brown patches) and sand grains
    flash = np.clip((mk.noise(S, S, 200, 24, 4) - 0.55) * 3, 0, 1)
    col = col * (1 - 0.45 * flash[..., None]) + np.array([0.22, 0.12, 0.10]) * 0.45 * flash[..., None]
    grains = (n3 > 0.72).astype(np.float32) * rng.uniform(0.6, 1.0, (S, S))
    col = col * (1 - 0.25 * grains[..., None]) + np.array([0.70, 0.55, 0.40]) * 0.25 * grains[..., None]
    pits = (mk.noise(S, S, 6, 25, 2) < 0.18).astype(np.float32)
    col *= (1 - 0.35 * pits[..., None])
    hgt = n2 * 0.4 + n3 * 0.3 - pits * 0.8
    # mortar residue on the bed faces region (rows 512..1024, left half): grey-white crusts
    mort = np.clip((mk.noise(S, S, 50, 26, 5) - 0.62) * 3, 0, 1) * 0.8; mort[:512] = 0; mort[:, 512:] = 0
    col = col * (1 - mort[..., None]) + (np.array([0.52, 0.50, 0.47]) * (0.85 + 0.2 * n3[..., None])) * mort[..., None]
    hgt += mort * 0.8
    # fracture face (right-bottom quadrant): rougher, lighter fresh clay
    fr = np.zeros((S, S), bool); fr[512:, 512:] = True
    fresh = np.array([0.66, 0.33, 0.20]) * (0.85 + 0.3 * n2[..., None])
    col[fr] = fresh[fr]; hgt[fr] = (n2 * 0.6 + n3 * 0.5)[fr]
    smo = 0.18 + 0.1 * n3 - 0.1 * mort
    write_maps('BrickHalf', col, hgt, 3.0, None, smo, 256)
    L, W, H = 0.112, 0.1025, 0.065
    FACE = rect(0, 0, 512, 512); SIDE = rect(512, 0, 1024, 512); BED = rect(0, 512, 512, 1024); FRAC = rect(512, 512, 1024, 1024)
    regions = {'px': FRAC, 'nx': SIDE, 'py': BED, 'ny': BED, 'pz': FACE, 'nz': FACE}
    sub = displaced_box((L, H, W), 26, lambda G, n: np.zeros(G.shape[:2]), regions)
    P, UV, _ = sub.parts['m']
    V = P.reshape(-1, 3).copy(); V0 = V.copy()
    # 1. broken end: set back the +x end by an uneven conchoidal surface, blended over the last 25 mm
    y, z = V[:, 1] / (H / 2), V[:, 2] / (W / 2)
    from scipy.ndimage import map_coordinates
    fr_rng = np.random.default_rng(11)
    field = blur(fr_rng.normal(0, 1, (48, 48)), 3.5) * 1.6 + blur(fr_rng.normal(0, 1, (48, 48)), 1.2) * 0.35
    gy = (y * 0.5 + 0.5) * 47; gz = (z * 0.5 + 0.5) * 47
    rough = map_coordinates(field, [gy, gz], order=1, mode='nearest')
    f = -0.006 - 0.004 * rough - 0.006 * np.clip(y * z * 0.8 + 0.3 * y, -1, 1)
    s_ = np.clip((V[:, 0] - (L / 2 - 0.025)) / 0.025, 0, 1) ** 2
    V[:, 0] += f * s_
    # 2. chips: spherical bites centred on edges and corners
    rng = np.random.default_rng(7)
    hx, hy, hz = L / 2, H / 2, W / 2
    for _ in range(26):
        ax = rng.integers(0, 3)
        c = np.array([rng.choice([-hx, hx]), rng.choice([-hy, hy]), rng.choice([-hz, hz])])
        c[ax] = rng.uniform(-1, 1) * [hx, hy, hz][ax]
        if c[0] > hx - 0.03:
            continue
        r = rng.uniform(0.002, 0.007) if rng.random() < 0.8 else rng.uniform(0.008, 0.013)
        c = c + rng.normal(0, r * 0.25, 3)
        d = V - c; dist = np.linalg.norm(d, axis=1)
        hit = dist < r
        # inward direction from the box faces the vertex lies on (position based, so welded edges move together)
        nv = np.zeros((hit.sum(), 3)); Vh = V0[hit]
        for k, hk in enumerate((hx, hy, hz)):
            on = np.abs(np.abs(Vh[:, k]) - hk) < 1e-6
            nv[on, k] = np.sign(Vh[on, k])
        nv /= np.linalg.norm(nv, axis=1, keepdims=True) + 1e-12
        dh = d[hit]; dn = np.sum(dh * nv, 1)
        t = dn + np.sqrt(np.maximum(dn ** 2 - np.sum(dh * dh, 1) + r * r, 0))
        V[hit] = V[hit] - nv * t[:, None] * (0.85 + 0.15 * rng.random(hit.sum())[:, None])
    P = V.reshape(-1, 3, 3)
    m = mk.Mesh(); m.add('Brick', P, UV, mk.smooth_normals(P, 35))
    m.ground()
    m.write(os.path.join(OUT, 'BrickHalf.obj'), {'Brick': dict(map='BrickHalf_Color.jpg', Ks=0.03)})
    report('BrickHalf', m)


# ======================================================================================= shoe-print cast
def sole_masks(W, H):
    """Outsole height field (0..1) in a W x H image, toe to the right."""
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    u = xx / W; v = (yy - H / 2) / (H / 2)
    # outline: forefoot wide, waist narrow, heel round
    half = np.interp(u, [0.0, 0.04, 0.12, 0.22, 0.38, 0.48, 0.62, 0.78, 0.90, 0.97, 1.0],
                     [0.0, 0.55, 0.78, 0.80, 0.62, 0.58, 0.82, 0.97, 0.88, 0.55, 0.0])
    shift = 0.10 * np.sin(u * math.pi) - 0.05
    inside = np.abs(v - shift) < half
    tread = np.zeros((H, W), np.float32)
    # heel: horizontal bars, waist: plain, forefoot: chevrons + flex grooves
    heel = u < 0.30; fore = u > 0.47
    tread[heel] = ((np.sin(xx[heel] / W * 2 * math.pi * 22) > -0.2)).astype(np.float32)
    chev = np.sin((xx / W + 0.6 * np.abs(v - shift) * 0.5) * 2 * math.pi * 30)
    tread[fore] = (chev[fore] > -0.1).astype(np.float32)
    grooves = np.abs(np.sin(xx / W * 2 * math.pi * 5.5)) < 0.08
    tread[fore & grooves] = 0
    rim = inside & ~(np.abs(v - shift) < half - 0.10)
    tread[rim] = 1.0
    waist = (~heel) & (~fore)
    tread[waist] = 0.6
    tread[~inside] = 0
    out = np.where(inside, 0.55 + 0.45 * tread, 0.0)
    return out.astype(np.float32), inside


def shoeprint_cast():
    Wt, Ht = 1024, 512   # top atlas: 0.36 x 0.18 m
    n1 = mk.noise(Ht, Wt, 80, 31, 5); n2 = mk.noise(Ht, Wt, 10, 32, 4); n3 = mk.noise(Ht, Wt, 3, 33, 3)
    so, inside = sole_masks(820, 300)
    sole = np.zeros((Ht, Wt), np.float32); ins = np.zeros((Ht, Wt), bool)
    sole[106:406, 102:922] = so; ins[106:406, 102:922] = inside
    sole = blur(sole, 1.5)
    stone = np.array([0.80, 0.76, 0.68])  # dental stone, pale buff
    col = stone * (0.90 + 0.12 * n1[..., None]) * (0.95 + 0.08 * n2[..., None])
    # soil picked up from the impression: brown specks, denser outside the print and in grooves
    soil = np.clip((n2 - 0.55) * 3, 0, 1) * (0.6 + 0.4 * (sole < 0.8)) + (n3 > 0.8) * 0.6
    col = col * (1 - 0.6 * soil[..., None]) + np.array([0.30, 0.24, 0.17]) * 0.6 * soil[..., None]
    # cavity shading on the tread (ambient occlusion baked lightly)
    cav = np.clip(blur(sole, 4) - sole, 0, 1)
    col *= (1 - 1.2 * cav[..., None])
    im = Image.fromarray((np.clip(col, 0, 1) * 255).astype(np.uint8)); d = ImageDraw.Draw(im)
    f = mk.font('sans', 26)
    d.text((40, 440), '14.10  #7  JM', font=f, fill=(120, 112, 100))  # scratched case notes
    col = np.asarray(im, np.float32) / 255
    hgt = sole * 1.0 + n2 * 0.15 + n3 * 0.1 - soil * 0.1
    # side atlas below (rows 512..640 in a 1024x1024 map): rough edges
    C = np.zeros((1024, 1024, 3), np.float32); Hh = np.zeros((1024, 1024), np.float32)
    C[:512] = col; Hh[:512] = hgt
    e1 = mk.noise(512, 1024, 30, 34, 5); e2 = mk.noise(512, 1024, 5, 35, 3)
    C[512:] = stone * (0.82 + 0.15 * e1[..., None]) * (0.9 + 0.1 * e2[..., None]) * (1 - 0.3 * (e2 > 0.75))[..., None]
    Hh[512:] = e1 * 0.5 + e2 * 0.5
    smo = 0.12 + 0.06 * np.ones((1024, 1024))
    write_maps('ShoePrintCast', C, Hh, 4.0, None, smo, 256)
    # geometry: irregular slab 0.36 x 0.18, 25 mm thick, top displaced by the sole field (positive relief 6 mm)
    nu, nv = 80, 40
    Lx, Lz, T = 0.36, 0.18, 0.025
    rng = np.random.default_rng(36)
    U = np.linspace(0, 1, nu + 1); V = np.linspace(0, 1, nv + 1)
    uu, vv = np.meshgrid(U, V)
    # outline: rounded rectangle with lumpy edge -> radial squash of the grid border
    cx, cz = (uu - 0.5) * 2, (vv - 0.5) * 2
    ang = np.arctan2(cz, cx); lump = 1 + 0.05 * np.sin(ang * 3 + 1) + 0.04 * np.sin(ang * 7 + 2) + 0.03 * np.sin(ang * 13)
    sq = np.maximum(np.abs(cx), np.abs(cz)) ** 6
    shrink = 1 - 0.10 * sq * (2 - lump)
    X = cx * Lx / 2 * shrink; Z = cz * Lz / 2 * shrink
    img_h = np.asarray(Image.fromarray(np.clip(hgt * 255, 0, 255).astype(np.uint8)).resize((nu + 1, nv + 1), Image.BILINEAR), np.float32) / 255
    s_h = np.asarray(Image.fromarray(np.clip(sole * 255, 0, 255).astype(np.uint8)).resize((nu + 1, nv + 1), Image.BILINEAR), np.float32) / 255
    s_h = s_h[::-1]  # image row 0 = v 1
    img_h = img_h[::-1]
    border = np.clip(1 - sq, 0, 1)
    Y = T + 0.006 * s_h * border + 0.0015 * img_h * border - 0.004 * sq
    G = np.stack([X, Y, -Z], -1)
    UVt = np.stack([uu, 0.5 + 0.5 * vv], -1)
    P, UV, _ = mk.grid_surface(G, UVt)
    fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
    if np.mean(fn[:, 1]) < 0:
        P = P[:, ::-1]; UV = UV[:, ::-1]
    m = mk.Mesh(); m.add('Cast', P, UV, mk.smooth_normals(P, 50))
    # side wall: follow the grid border ring down to the floor, bottom flat
    ring = np.concatenate([G[0, :], G[1:, -1], G[-1, -2::-1], G[-2:0:-1, 0]])
    perim = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(np.vstack([ring, ring[:1]]), axis=0), axis=1))])
    Ps, UVs = [], []
    for i in range(len(ring)):
        a = ring[i]; b = ring[(i + 1) % len(ring)]
        a0 = a.copy(); a0[1] = 0; b0 = b.copy(); b0[1] = 0
        ua, ub = perim[i] / perim[-1], perim[i + 1] / perim[-1]
        q = np.array([a0, b0, b, a]); uq = np.array([(ua, 0.38), (ub, 0.38), (ub, 0.48), (ua, 0.48)])
        for t in ((0, 1, 2), (0, 2, 3)):
            Ps.append(q[list(t)]); UVs.append(uq[list(t)])
    Ps = np.array(Ps); UVs = np.array(UVs)
    c = np.array([0, 0.01, 0])
    fn = np.cross(Ps[:, 1] - Ps[:, 0], Ps[:, 2] - Ps[:, 0]); out = Ps.mean(1) - c
    flip = np.sum(fn * out, 1) < 0
    Ps[flip] = Ps[flip][:, ::-1]; UVs[flip] = UVs[flip][:, ::-1]
    m.add('Cast', Ps, UVs, mk.smooth_normals(Ps, 60))
    # bottom
    bot = [(p[0], p[2]) for p in ring]
    tris = mk.triangulate(np.array([(x, -z) for x, z in bot]))
    Pb = np.array([[ring[k] * np.array([1, 0, 1]) for k in t] for t in tris])
    fn = np.cross(Pb[:, 1] - Pb[:, 0], Pb[:, 2] - Pb[:, 0]); fl = fn[:, 1] > 0
    Pb[fl] = Pb[fl][:, ::-1]
    m.add('Cast', Pb, np.full((len(Pb), 3, 2), (0.5, 0.25)))
    m.ground()
    m.write(os.path.join(OUT, 'ShoePrintCast.obj'), {'Cast': dict(map='ShoePrintCast_Color.jpg', Ks=0.02)})
    report('ShoePrintCast', m)



# ======================================================================================= fabric helpers
def weave(H, W, period, seed, kind='plain'):
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    if kind == 'twill':
        h = 0.5 + 0.5 * np.sin((xx + yy) * 2 * math.pi / period)
    elif kind == 'rib':
        h = np.abs(np.sin(xx * math.pi / period)) * (0.8 + 0.2 * np.abs(np.sin(yy * math.pi / (period * 0.7) + (np.floor(xx / period) % 2) * 1.5)))
    else:
        h = 0.5 + 0.25 * (np.sin(xx * 2 * math.pi / period) * np.sin(yy * 2 * math.pi / period)) + 0.25 * np.sin((xx - yy) * math.pi / period) ** 2
    fuzz = mk.noise(H, W, 3, seed, 2)
    return h * 0.8 + fuzz * 0.2


# ======================================================================================= camera on tripod
def camera_tripod():
    S = 1024
    col = np.zeros((S, S, 3), np.float32); hgt = np.zeros((S, S), np.float32)
    met = np.zeros((S, S), np.float32); smo = np.zeros((S, S), np.float32)
    # A: leatherette camera body (0..512, 0..512)
    peb = mk.noise(512, 512, 2.5, 41, 2); peb2 = mk.noise(512, 512, 30, 42, 4)
    col[:512, :512] = (0.035 + 0.02 * peb[..., None] + 0.015 * peb2[..., None]); hgt[:512, :512] = peb
    smo[:512, :512] = 0.30 + 0.1 * peb
    # B: lens barrel (512..1024, 0..512): rows = along the lens, columns = around
    yy, xx = np.mgrid[0:512, 0:512].astype(np.float32)
    lens = np.full((512, 512), 0.04, np.float32); lh = np.zeros((512, 512), np.float32); ls = np.full((512, 512), 0.35, np.float32)
    zoom = (yy > 140) & (yy < 300); focus = (yy > 340) & (yy < 420); mount = yy > 470
    lh[zoom] = (np.sin(xx[zoom] * 2 * math.pi / 6) > 0).astype(np.float32); lens[zoom] = 0.03 + 0.02 * lh[zoom]; ls[zoom] = 0.2
    lh[focus] = (np.sin(xx[focus] * 2 * math.pi / 3) > 0).astype(np.float32); lens[focus] = 0.03; ls[focus] = 0.25
    lens[mount] = 0.62; ls[mount] = 0.7
    ring = (np.abs(yy - 60) < 2); lens[ring] = 0.75  # thin white index ring at the front
    col[:512, 512:] = lens[..., None]; hgt[:512, 512:] = lh; smo[:512, 512:] = ls; met[:512, 512:][mount] = 1
    # C: anodised black aluminium legs (0..512, 512..1024), light scratches
    sc = Image.new('L', (512, 512), 0); d = ImageDraw.Draw(sc); rng = np.random.default_rng(43)
    for _ in range(140):
        x, y = rng.uniform(0, 512, 2); Lh = rng.uniform(5, 40)
        d.line([(x, y), (x + rng.normal(0, 3), y + Lh)], fill=int(rng.uniform(60, 220)), width=1)
    scr = np.asarray(sc, np.float32) / 255; an = mk.noise(512, 512, 20, 44, 3)
    col[512:, :512] = (0.045 + 0.02 * an[..., None]) * (1 - scr[..., None]) + 0.45 * scr[..., None]
    met[512:, :512] = 0.6 + 0.4 * scr; smo[512:, :512] = 0.55 - 0.1 * an; hgt[512:, :512] = -scr
    # D: rubber and grey plastic (512..1024, 512..1024): left rubber, right plastic, a strip of bare steel
    rb = mk.noise(512, 512, 4, 45, 3)
    col[512:, 512:768] = 0.03 + 0.015 * rb[:, :256, None]; smo[512:, 512:768] = 0.15; hgt[512:, 512:768] = rb[:, :256]
    col[512:, 768:] = 0.12 + 0.02 * rb[:, 256:, None]; smo[512:, 768:] = 0.35
    col[960:, 768:] = 0.6; met[960:, 768:] = 1; smo[960:, 768:] = 0.6
    write_maps('CameraTripod', col, hgt, 1.5, met, smo, 512)
    BODY = rect(0, 0, 512, 512); LENS = rect(512, 0, 1024, 512); LEG = rect(0, 512, 512, 1024)
    RUB = rect(512, 512, 768, 1024); PLA = rect(768, 512, 1024, 950); STEEL = rect(768, 960, 1024, 1024)
    m = mk.Mesh()
    hub_y = 1.18; splay = 23.0
    Lleg = (hub_y - 0.0) / math.cos(math.radians(splay))
    for k, ang in enumerate((90, 210, 330)):
        a = math.radians(ang)
        # leg built along +y from the foot (y=0) to the hinge (y=Lleg), then tilted outwards
        prof = [(0.0, 0.0), (0.012, 0.002), (0.014, 0.030), (0.0092, 0.034), (0.0092, Lleg * 0.36), (0.0128, Lleg * 0.36),
                (0.0128, Lleg * 0.40), (0.0108, Lleg * 0.40), (0.0108, Lleg * 0.68), (0.0145, Lleg * 0.68), (0.0145, Lleg * 0.72),
                (0.0126, Lleg * 0.72), (0.0126, Lleg - 0.01), (0.0, Lleg)]
        P, UV, N = mk.lathe(prof, 12, uv=LEG, smooth=False)
        N = mk.smooth_normals(P, 40)
        # rubber foot and lock collars get the rubber region
        cy = P.mean(1)[:, 1]
        lock = (cy < 0.034) | (np.abs(cy - Lleg * 0.38) < Lleg * 0.021) | (np.abs(cy - Lleg * 0.70) < Lleg * 0.021)
        UV[lock] = set_uv(P[lock], UV[lock], RUB)
        r = 0.05
        M = mk.chain(mk.T(r * math.cos(a), hub_y, -r * math.sin(a)), mk.Ry(ang - 90), mk.Rx(-(180 - splay)))
        sub = mk.Mesh(); sub.add('Tripod', P, UV, N); m.merge(sub, M)
    # hub, centre column, pan head
    P, UV, N = mk.lathe([(0.0, hub_y - 0.03), (0.055, hub_y - 0.03), (0.06, hub_y - 0.01), (0.06, hub_y + 0.02), (0.045, hub_y + 0.035), (0.0, hub_y + 0.035)], 18, uv=PLA)
    m.add('Tripod', P, UV, N)
    P, UV, N = mk.lathe([(0.0, 0.93), (0.016, 0.93), (0.016, 1.30), (0.0, 1.30)], 14, uv=LEG); m.add('Tripod', P, UV, N)
    P, UV, N = mk.lathe([(0.0, 0.905), (0.006, 0.905), (0.01, 0.93), (0.0, 0.93)], 10, uv=STEEL); m.add('Tripod', P, UV, N)
    P, UV, N = mk.lathe([(0.0, 1.30), (0.032, 1.30), (0.032, 1.325), (0.0, 1.325)], 18, uv=PLA); m.add('Tripod', P, UV, N)
    box_part(m, 'Tripod', (0.052, 0.055, 0.050), mk.T(0, 1.352, 0), PLA)
    box_part(m, 'Tripod', (0.064, 0.012, 0.075), mk.chain(mk.T(0, 1.386, 0), mk.Rx(-4)), PLA)
    P, UV, N = mk.tube([(0.03, 1.36, -0.01), (0.034, 1.345, -0.10), (0.04, 1.32, -0.20), (0.045, 1.30, -0.27)], 0.008, 10)
    UV = set_uv(P, UV, LEG); m.add('Tripod', P, UV, N)
    P, UV, N = mk.tube([(0.042, 1.312, -0.225), (0.045, 1.30, -0.27), (0.046, 1.295, -0.29)], 0.0125, 12)
    UV = set_uv(P, UV, RUB); m.add('Tripod', P, UV, N)
    # camera body (lens toward +z), grip on the photographer's right (-x)
    cam_y = 1.392
    P, UV, N = mk.rounded_box((0.136, 0.092, 0.072), 0.012, 3, uv_rects={k: BODY for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')}, center=(0, cam_y + 0.046, 0.0))
    m.add('Camera', P, UV, N)
    P, UV, N = mk.rounded_box((0.036, 0.086, 0.088), 0.014, 3, uv_rects={k: BODY for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')}, center=(-0.052, cam_y + 0.044, 0.010))
    m.add('Camera', P, UV, N)
    P, UV, N = mk.rounded_box((0.056, 0.036, 0.060), 0.008, 2, uv_rects={k: BODY for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')}, center=(0.0, cam_y + 0.100, -0.004))
    m.add('Camera', P, UV, N)
    box_part(m, 'Camera', (0.046, 0.034, 0.012), mk.T(0.0, cam_y + 0.040, -0.040), PLA)  # rear screen frame
    # lens along +z: lathe along y then rotate
    lprof = [(0.0, 0.0), (0.0315, 0.0), (0.0315, 0.006), (0.0345, 0.008), (0.0345, 0.040), (0.036, 0.042), (0.036, 0.074),
             (0.0335, 0.076), (0.0335, 0.090), (0.037, 0.092), (0.037, 0.104), (0.031, 0.105), (0.0, 0.103)]
    P, UV, N = mk.lathe(lprof, 28, uv=(LENS[0], LENS[3], LENS[2], LENS[1]))
    glass = P.mean(1)[:, 1] > 0.1046
    sub = mk.Mesh(); sub.add('Camera', P[~glass], UV[~glass], N[~glass]); sub.add('LensGlass', P[glass], UV[glass], N[glass])
    m.merge(sub, mk.chain(mk.T(0, cam_y + 0.046, 0.036), mk.Rx(90)))
    m.ground()
    m.write(os.path.join(OUT, 'CameraTripod.obj'), {'Tripod': dict(map='CameraTripod_Color.jpg', Ks=0.2),
                                                     'Camera': dict(map='CameraTripod_Color.jpg', Ks=0.2),
                                                     'LensGlass': dict(Kd=(0.02, 0.03, 0.05), Ks=0.9, Ns=300)})
    report('CameraTripod', m)


# ======================================================================================= police peaked cap
def police_cap():
    S = 1024
    col = np.zeros((S, S, 3), np.float32); hgt = np.zeros((S, S), np.float32)
    met = np.zeros((S, S), np.float32); smo = np.zeros((S, S), np.float32)
    tw = weave(512, 1024, 5, 51, 'twill'); n = mk.noise(512, 1024, 60, 52, 4)
    col[:512] = np.array([0.035, 0.045, 0.085]) * (0.85 + 0.25 * tw[..., None]) * (0.9 + 0.2 * n[..., None])
    lint = (mk.noise(512, 1024, 2, 53, 1) > 0.86).astype(np.float32) * 0.5
    col[:512] += lint[..., None] * 0.04
    hgt[:512] = tw; smo[:512] = 0.22
    # band (rows 512..640): black mohair band with a fine rib
    yy, xx = np.mgrid[0:128, 0:1024].astype(np.float32)
    rib = 0.5 + 0.5 * np.sin(yy * 2 * math.pi / 4)
    col[512:640] = 0.02 + 0.015 * rib[..., None]; hgt[512:640] = rib; smo[512:640] = 0.35
    # lining (rows 640..768): pale grey satin
    ln = mk.noise(128, 1024, 30, 54, 3)
    col[640:768] = np.array([0.55, 0.56, 0.58]) * (0.85 + 0.2 * ln[..., None]); smo[640:768] = 0.6
    # patent visor and strap (rows 768..896, left), silver badge metal (right) and enamel
    pv = mk.noise(128, 512, 40, 55, 3)
    col[768:896, :512] = 0.012 + 0.01 * pv[..., None]; smo[768:896, :512] = 0.86 - 0.1 * pv
    col[768:896, 512:768] = np.array([0.78, 0.78, 0.80]); met[768:896, 512:768] = 1; smo[768:896, 512:768] = 0.78
    col[768:896, 768:] = np.array([0.04, 0.07, 0.20]); smo[768:896, 768:] = 0.85
    col[896:] = np.array([0.08, 0.09, 0.10]); smo[896:] = 0.4   # sweatband (dark leather)
    write_maps('PoliceCap', col, hgt, 1.2, met, smo, 256)
    CROWN = rect(0, 0, 1024, 512); BAND = rect(0, 512, 1024, 640); LINING = rect(0, 640, 1024, 768)
    PATENT = rect(0, 768, 512, 896); SILVER = rect(512, 768, 768, 896); ENAMEL = rect(768, 768, 1024, 896); SWEAT = rect(0, 896, 1024, 1024)
    m = mk.Mesh()
    sx, sz = 0.94, 1.13
    crown = [(0.0868, 0.040), (0.0880, 0.050), (0.0990, 0.066), (0.1130, 0.083), (0.1250, 0.096), (0.1290, 0.103),
             (0.1260, 0.1085), (0.1100, 0.1110), (0.0700, 0.1122), (0.0, 0.1125)]
    def saddle(P):
        P = P.copy()
        zf = np.clip(P[..., 2] / (0.13 * sz), -1, 1); xf = np.clip(P[..., 0] / (0.13 * sx), -1, 1)
        up = np.clip((P[..., 1] - 0.045) / 0.065, 0, 1)
        P[..., 1] += up * (0.020 * np.clip(zf, 0, 1) ** 2 - 0.006 * xf ** 2 - 0.004 * np.clip(-zf, 0, 1))
        return P
    P, UV, N = mk.lathe(crown, 48, uv=CROWN, sx=sx, sz=sz); P = saddle(P); m.add('Cap', P, UV, mk.smooth_normals(P, 50))
    P, UV, N = mk.lathe([(0.0870, 0.0), (0.0872, 0.040), (0.0868, 0.046)], 48, uv=BAND, sx=sx, sz=sz); m.add('Cap', P, UV, N)
    # inside: lining and sweatband (flipped)
    inner = [(r * 0.975, y - 0.002) for r, y in crown]
    P, UV, N = mk.lathe(inner, 40, uv=LINING, sx=sx, sz=sz, flip=True); P = saddle(P); m.add('Cap', P, UV, N)
    P, UV, N = mk.lathe([(0.0850, 0.0), (0.0850, 0.040)], 40, uv=SWEAT, sx=sx, sz=sz, flip=True); m.add('Cap', P, UV, N)
    P, UV, N = mk.lathe([(0.0850, 0.0), (0.0870, 0.0)], 40, uv=SWEAT, sx=sx, sz=sz); m.add('Cap', P, UV, N)
    # visor: grid over angle (front = +z) and outward distance, sloping down
    na, nr = 24, 6
    angs = np.radians(np.linspace(-78, 78, na + 1)); tt = np.linspace(0, 1, nr + 1)
    G = np.zeros((nr + 1, na + 1, 3))
    for i, t in enumerate(tt):
        for j, a in enumerate(angs):
            ex, ez = 0.0872 * sx * math.sin(a), 0.0872 * sz * math.cos(a)
            reach = 0.062 * math.cos(a) ** 1.5 * t
            dirv = np.array([math.sin(a) * 0.4, 0, math.cos(a)]); dirv /= np.linalg.norm(dirv)
            G[i, j] = (ex + dirv[0] * reach * 0.6, 0.004 - 0.42 * reach - 0.010 * t * t * math.cos(a), ez + dirv[2] * reach)
    UVg = np.stack(np.meshgrid(PATENT[0] + (PATENT[2] - PATENT[0]) * np.linspace(0, 1, na + 1), PATENT[1] + (PATENT[3] - PATENT[1]) * tt), -1)
    Pt, UVt, _ = mk.grid_surface(G, UVg); Pb, UVb, _ = mk.grid_surface(G - np.array([0, 0.0028, 0]), UVg, flip=True)
    fn = np.cross(Pt[:, 1] - Pt[:, 0], Pt[:, 2] - Pt[:, 0])
    if np.mean(fn[:, 1]) < 0:
        Pt, UVt, Pb, UVb = Pt[:, ::-1], UVt[:, ::-1], Pb[:, ::-1], UVb[:, ::-1]
    m.add('Cap', Pt, UVt, mk.smooth_normals(Pt, 40)); m.add('Cap', Pb, UVb, mk.smooth_normals(Pb, 40))
    rim = G[-1]; Pr = []
    for j in range(na):
        a, b = rim[j], rim[j + 1]; a2, b2 = a - (0, 0.0028, 0), b - (0, 0.0028, 0)
        Pr += [[a, a2, b2], [a, b2, b]]
    Pr = np.array(Pr); fn = np.cross(Pr[:, 1] - Pr[:, 0], Pr[:, 2] - Pr[:, 0]); outv = Pr.mean(1) * np.array([1, 0, 1])
    fl = np.sum(fn * outv, 1) < 0; Pr[fl] = Pr[fl][:, ::-1]
    m.add('Cap', Pr, np.full((len(Pr), 3, 2), ((PATENT[0] + PATENT[2]) / 2, (PATENT[1] + PATENT[3]) / 2)))
    # chin strap across the front of the band with two side buttons
    P, UV, N = mk.lathe([(0.0885, 0.008), (0.0885, 0.019)], 30, uv=PATENT, sx=sx, sz=sz, a0=180 + 12, a1=360 - 12)
    m.add('Cap', P, UV, N)
    for side in (-1, 1):
        a = math.radians(90 + side * 78)
        bx, bz = 0.0885 * sx * math.cos(a) * 1.0, -0.0885 * sz * math.sin(a)
        Pd, UVd, Nd = mk.lathe([(0.0, 0.0), (0.0055, 0.0), (0.0045, 0.0025), (0.0, 0.0035)], 10, uv=SILVER)
        sub = mk.Mesh(); sub.add('Cap', Pd, UVd, Nd)
        yaw = math.degrees(math.atan2(bx, bz))
        m.merge(sub, mk.chain(mk.T(bx, 0.0135, bz), mk.Ry(yaw), mk.Rx(90)))
    # generic badge: eight-point star with an enamel centre (no lettering)
    star = []
    for k in range(16):
        r = 0.026 if k % 2 == 0 else 0.013
        a = math.pi / 2 + k * math.pi / 8
        star.append((r * math.cos(a), r * math.sin(a)))
    top, bot, side = mk.extrude(star, 0.003, uv_top=lambda x, z: ((SILVER[0] + SILVER[2]) / 2, (SILVER[1] + SILVER[3]) / 2))
    badge = mk.Mesh()
    for part in (top, side):
        Pp, UVp, Np = part; badge.add('Cap', Pp, np.full((len(Pp), 3, 2), ((SILVER[0] + SILVER[2]) / 2, (SILVER[1] + SILVER[3]) / 2)), Np)
    Pd, UVd, Nd = mk.disk(0.0085, 0.0035, 16); badge.add('Cap', Pd, np.full((len(Pd), 3, 2), ((ENAMEL[0] + ENAMEL[2]) / 2, (ENAMEL[1] + ENAMEL[3]) / 2)), Nd)
    Pd, UVd, Nd = mk.lathe([(0.0085, 0.0035), (0.0105, 0.0035), (0.0105, 0.0045), (0.0085, 0.0045)], 16, uv=SILVER); badge.add('Cap', Pd, UVd, Nd)
    zf = 0.0995 * sz
    m.merge(badge, mk.chain(mk.T(0, 0.066, zf - 0.005), mk.Rx(90 - 28)))
    m.ground()
    m.write(os.path.join(OUT, 'PoliceCap.obj'), {'Cap': dict(map='PoliceCap_Color.jpg', Ks=0.15)})
    report('PoliceCap', m)


# ======================================================================================= duffel bag
def duffel_bag():
    S = 1024
    col = np.zeros((S, S, 3), np.float32); hgt = np.zeros((S, S), np.float32); smo = np.zeros((S, S), np.float32)
    wv = weave(768, 1024, 4, 61); n = mk.noise(768, 1024, 70, 62, 5); dust = grime(768, 1024, 63, 50)
    body = (0.028 + 0.018 * wv[..., None] + 0.01 * n[..., None]) * np.array([1.0, 1.0, 1.08])
    body = body * (1 - 0.2 * dust[..., None]) + np.array([0.13, 0.12, 0.11]) * 0.2 * dust[..., None]
    col[:768] = body; hgt[:768] = wv * 0.6 + n * 0.4; smo[:768] = 0.38 - 0.15 * dust
    # zip along u = 0.5 (top centre) and piping at u = 0, the end seams at v edges
    zc = 512; xx = np.arange(1024)
    teeth = (np.arange(768) // 3) % 2
    for c0, c1, val, s_ in ((zc - 16, zc - 5, 0.05, 0.3), (zc + 5, zc + 16, 0.05, 0.3)):
        col[:768, c0:c1] = val; smo[:768, c0:c1] = s_; hgt[:768, c0:c1] = 0.5
    col[:768, zc - 5:zc + 5] = np.where(teeth[:, None, None] > 0, 0.09, 0.02); hgt[:768, zc - 5:zc + 5] = teeth[:, None] * 0.8 + 0.6
    smo[:768, zc - 5:zc + 5] = 0.6
    for c in (0, 1, 2, 1021, 1022, 1023):
        col[:768, c] = 0.06; hgt[:768, c] = 1.0
    for r0 in (0, 765):
        col[r0:r0 + 3] = 0.07; hgt[r0:r0 + 3] = 1.0
    # stitch lines 8 px in from the seams
    st = ((np.arange(768) // 6) % 2 == 0)
    for c in (10, 1013):
        col[:768, c][st] = 0.12; hgt[:768, c][st] = 0.9
    # end panels (rows 768..1024, left half) and webbing (right half)
    ep = weave(256, 512, 4, 64); col[768:, :512] = 0.03 + 0.018 * ep[..., None]; hgt[768:, :512] = ep; smo[768:, :512] = 0.35
    yy, xw = np.mgrid[0:256, 0:512].astype(np.float32)
    web = 0.5 + 0.5 * np.sin(xw * 2 * math.pi / 3) * np.sin(yy * 2 * math.pi / 6)
    col[768:, 512:] = 0.035 + 0.025 * web[..., None]; hgt[768:, 512:] = web; smo[768:, 512:] = 0.3
    write_maps('DuffelBag', col, hgt, 1.6, None, smo, 256)
    BODY = rect(0, 0, 1024, 768); END = rect(0, 768, 512, 1024); WEB = rect(512, 768, 1024, 1024)
    from scipy.ndimage import map_coordinates
    rng = np.random.default_rng(65)
    Lb, w, h = 0.60, 0.15, 0.30
    # cross-section: flat bottom, straight sides, round top; parameter t (0..1) starts at bottom centre
    def section(nt):
        pts = []
        seg_b = w; seg_s = h - w; seg_t = math.pi * w; tot = 2 * seg_b + 2 * seg_s + seg_t
        for t in np.linspace(0, 1, nt + 1)[:-1]:
            s_ = t * tot
            if s_ < seg_b: pts.append((s_, 0))
            elif s_ < seg_b + seg_s: pts.append((w, s_ - seg_b))
            elif s_ < seg_b + seg_s + seg_t:
                a = (s_ - seg_b - seg_s) / w; pts.append((w * math.cos(a), (h - w) + w * math.sin(a)))
            elif s_ < seg_b + 2 * seg_s + seg_t: pts.append((-w, (h - w) - (s_ - seg_b - seg_s - seg_t)))
            else: pts.append((-w + (s_ - seg_b - 2 * seg_s - seg_t), 0))
        return np.array(pts)
    nt, nx = 48, 34
    sec = section(nt)
    field = blur(rng.normal(0, 1, (nx + 1, nt + 1)), 1.6)
    G = np.zeros((nx + 1, nt + 1, 3))
    xs = np.linspace(-Lb / 2, Lb / 2, nx + 1)
    for i, x in enumerate(xs):
        e = abs(x) / (Lb / 2)
        sc = 1 - 0.16 * e ** 5
        for j in range(nt + 1):
            z, y = sec[j % nt]
            y2 = y * (sc * (0.93 - 0.05 * (y / h)))   # slumped: top lower than the frame
            z2 = z * sc * (1 + 0.10 * (1 - (y / h - 0.45) ** 2 * 3) if y > 0 else sc)
            bump = 0.008 * field[i, j] * (y > 0.01)
            rad = np.array([0, y2 - h * 0.4, z2]); rad /= np.linalg.norm(rad) + 1e-9
            G[i, j] = (x, y2, z2) + bump * rad * np.array([0, 1, 1])
    G[..., 1] = np.maximum(G[..., 1], 0)
    UVg = np.stack(np.meshgrid(np.linspace(0, 1, nt + 1), np.linspace(0, 1, nx + 1)), -1)
    UVg[..., 0] = BODY[0] + (BODY[2] - BODY[0]) * UVg[..., 0]; UVg[..., 1] = BODY[1] + (BODY[3] - BODY[1]) * UVg[..., 1]
    P, UV, _ = mk.grid_surface(G, UVg)
    fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0]); outv = P.mean(1) - np.array([0, 0.13, 0]); outv[:, 0] = 0
    fl = np.sum(fn * outv, 1) < 0; P[fl] = P[fl][:, ::-1]; UV[fl] = UV[fl][:, ::-1]
    m = mk.Mesh(); m.add('Bag', P, UV, mk.smooth_normals(P, 55))
    # end panels: fan from a slightly bulged centre
    for i, sgn in ((0, -1), (nx, 1)):
        ring = G[i, :-1]; cen = ring.mean(0) + np.array([sgn * 0.012, 0, 0])
        Pe = []; UVe = []
        for j in range(nt):
            a, b = ring[j], ring[(j + 1) % nt]
            tri = np.array([cen, a, b]); fnn = np.cross(tri[1] - tri[0], tri[2] - tri[0])
            if fnn[0] * sgn < 0: tri = tri[::-1]
            Pe.append(tri)
            uvt = [((END[0] + END[2]) / 2 + (END[2] - END[0]) * 0.45 * (p[2] / w / 1.2), END[1] + (END[3] - END[1]) * (p[1] / h)) for p in tri]
            UVe.append(uvt)
        m.add('Bag', np.array(Pe), np.array(UVe), mk.smooth_normals(np.array(Pe), 40))
    # two carry handles (round, foam-wrapped webbing) meeting over the zip, and a zip puller
    top_y = G[nx // 2, nt // 2, 1]
    for zs in (1, -1):
        path = [(-0.14, top_y - 0.07, zs * 0.115), (-0.12, top_y + 0.02, zs * 0.07), (-0.06, top_y + 0.075, zs * 0.025),
                (0.06, top_y + 0.075, zs * 0.025), (0.12, top_y + 0.02, zs * 0.07), (0.14, top_y - 0.07, zs * 0.115)]
        pts = []
        for k in range(len(path) - 1):
            for t in np.linspace(0, 1, 5)[:-1]:
                pts.append(np.array(path[k]) * (1 - t) + np.array(path[k + 1]) * t)
        pts.append(np.array(path[-1]))
        P, UV, N = mk.tube(pts, 0.009, 10, uv=WEB)
        m.add('Bag', P, UV, N)
        for xa in (-0.14, 0.14):
            box_part(m, 'Bag', (0.05, 0.075, 0.004), mk.chain(mk.T(xa, top_y - 0.085, zs * (w + 0.004)), mk.Rx(zs * 18)), WEB)
    box_part(m, 'Bag', (0.012, 0.004, 0.030), mk.T(0.17, top_y + 0.002, 0.012), rect(512, 768, 520, 776))
    m.ground()
    m.write(os.path.join(OUT, 'DuffelBag.obj'), {'Bag': dict(map='DuffelBag_Color.jpg', Ks=0.1)})
    report('DuffelBag', m)


# ======================================================================================= balaclava
def balaclava():
    S = 1024
    knit = weave(S, S, 5, 71, 'rib'); n = mk.noise(S, S, 50, 72, 4)
    col = (0.022 + 0.03 * knit[..., None] + 0.01 * n[..., None]) * np.array([1, 1, 1.05])
    hgt = knit.copy(); smo = 0.12 + 0.05 * knit
    # eye opening (one-hole style) on the upper face: dark interior, rib hem around it (texture u 0.5 centre, v 0.66)
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    ex = (xx - 512) / 175; ey = (yy - 330) / 52
    rr = np.maximum(np.abs(ex) ** 4 + np.abs(ey) ** 4, 0) ** 0.25
    hole = rr < 1.0; hem = (rr >= 1.0) & (rr < 1.35)
    col[hole] = 0.006; hgt[hole] = 0.0
    hemrib = 0.5 + 0.5 * np.sin(np.arctan2(ey, ex) * 90)
    col[hem] = (0.03 + 0.03 * hemrib[hem])[:, None]; hgt[hem] = 0.8 + 0.2 * hemrib[hem]
    write_maps('Balaclava', col, hgt, 2.2, None, smo, 256)
    # outline: rounded crown at -z (head top), straight sides, flared neck at +z
    nu, nv = 34, 64
    L, wh, wn = 0.40, 0.105, 0.125
    def half_width(t):  # t 0 crown .. 1 neck
        if t < 0.18: return wh * math.sqrt(max(0, 1 - ((0.18 - t) / 0.18) ** 2))
        return wh + (wn - wh) * max(0, (t - 0.72) / 0.28) ** 1.5
    rng = np.random.default_rng(73)
    field = blur(rng.normal(0, 1, (nv + 1, nu + 1)), 2.0)
    wob = blur(rng.normal(0, 1, (nv + 1, 1)), 3.0)[:, 0] * 2.5
    # crease ridges: (centre t, centre s, direction angle, amplitude, width)
    creases = [(0.55, 0.1, 0.4, 0.016, 0.05), (0.80, -0.3, -0.9, 0.012, 0.04), (0.25, 0.5, 1.3, 0.010, 0.035), (0.68, 0.45, 0.2, -0.006, 0.03)]
    G = np.zeros((nv + 1, nu + 1, 3)); UVg = np.zeros((nv + 1, nu + 1, 2))
    for i in range(nv + 1):
        t = i / nv; hw = max(half_width(t) * (1 + 0.07 * wob[i]), 0.004)
        cx = 0.025 * math.sin(t * math.pi * 1.3 + 0.4)
        for j in range(nu + 1):
            s_ = j / nu * 2 - 1
            x = cx + s_ * hw
            edge = max(0.0, 1 - abs(s_)) ** 0.45 * min(1.0, t / 0.05 + 0.2) ** 0.5 * min(1.0, (1 - t) / 0.03 + 0.15) ** 0.5
            y = 0.003 + 0.016 * edge + 0.007 * field[i, j] * edge
            for ct, cs, ca, amp, wd in creases:
                dx, dz = s_ * 0.12 - cs * 0.12, (t - ct) * L
                dist = abs(-math.sin(ca) * dx + math.cos(ca) * dz)
                y += amp * math.exp(-(dist / wd) ** 2) * edge
            ex_, ey_ = (x - cx) / 0.055, (t - 0.32) / 0.045
            if ex_ ** 4 + ey_ ** 4 < 1: y -= 0.010 * edge
            G[i, j] = (x, max(y, 0.0015), -L / 2 + t * L)
            UVg[i, j] = (0.5 + s_ * hw / (2 * 0.15), 1 - t * 0.96 - 0.02)
    P, UV, _ = mk.grid_surface(G, UVg)
    fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
    if np.mean(fn[:, 1]) < 0: P, UV = P[:, ::-1], UV[:, ::-1]
    m = mk.Mesh(); m.add('Knit', P, UV, mk.smooth_normals(P, 60))
    Gb = G.copy(); Gb[..., 1] = 0.0
    Pb, UVb, _ = mk.grid_surface(Gb[::4, ::4], UVg[::4, ::4])
    fn = np.cross(Pb[:, 1] - Pb[:, 0], Pb[:, 2] - Pb[:, 0])
    if np.mean(fn[:, 1]) > 0: Pb, UVb = Pb[:, ::-1], UVb[:, ::-1]
    m.add('Knit', Pb, UVb)
    m.ground()
    m.write(os.path.join(OUT, 'Balaclava.obj'), {'Knit': dict(map='Balaclava_Color.jpg', Ks=0.02)})
    report('Balaclava', m)


# ======================================================================================= numbered evidence cone
def evidence_cone():
    S = 1024; C = 256
    rng = np.random.default_rng(81)
    base = np.array([0.98, 0.76, 0.04]) * (0.95 + 0.06 * mk.noise(S, S, 40, 82, 4)[..., None])
    im = Image.fromarray((np.clip(base, 0, 1) * 255).astype(np.uint8)); d = ImageDraw.Draw(im)
    f = mk.font('sans-bold', 120)
    for k in range(16):
        x0, y0 = (k % 4) * C, (k // 4) * C
        txt = str(k + 1); bb = d.textbbox((0, 0), txt, font=f)
        d.text((x0 + C / 2 - (bb[2] - bb[0]) / 2 - bb[0], y0 + 150 - (bb[3] - bb[1]) / 2 - bb[1]), txt, font=f, fill=(12, 12, 12))
        d.rectangle([x0 + 20, y0 + 236, x0 + C - 20, y0 + 246], fill=(12, 12, 12))
    im = im.filter(ImageFilter.GaussianBlur(0.4))
    mk.save_color(os.path.join(OUT, 'EvidenceMarkerCone_Atlas.png'), np.asarray(im))
    # three-sided cone (frustum), 12 cm tall, 11 cm base edge; every face shows the number of cell 1 (top-left)
    H, b, t = 0.12, 0.11, 0.022
    rb, rt = b / math.sqrt(3), t / math.sqrt(3)
    m = mk.Mesh()
    cell = (0.0, 0.75, 0.25, 1.0)
    for k in range(3):
        a0 = math.radians(90 + k * 120); a1 = math.radians(90 + (k + 1) * 120)
        p0 = (rb * math.cos(a0), 0, -rb * math.sin(a0)); p1 = (rb * math.cos(a1), 0, -rb * math.sin(a1))
        q1 = (rt * math.cos(a1), H, -rt * math.sin(a1)); q0 = (rt * math.cos(a0), H, -rt * math.sin(a0))
        quad = np.array([p0, p1, q1, q0])
        # the face is a trapezoid; map the full cell width at the base, narrowing towards the top
        u = [cell[0] + 0.02 * 0.25, cell[2] - 0.02 * 0.25, 0.125 + (t / b) * 0.115, 0.125 - (t / b) * 0.115]
        v = [cell[1], cell[1], cell[3], cell[3]]
        uvq = np.array(list(zip(u, v)))
        for tri in ((0, 1, 2), (0, 2, 3)):
            Pq = quad[list(tri)]; fnn = np.cross(Pq[1] - Pq[0], Pq[2] - Pq[0])
            Uq = uvq[list(tri)]
            if np.dot(fnn, Pq.mean(0) * np.array([1, 0, 1])) < 0: Pq, Uq = Pq[::-1], Uq[::-1]
            m.add('MarkerPlastic', Pq[None], Uq[None])
    plain = (0.125, 0.76)
    Pd, UVd, Nd = mk.disk(rt, H, 3, up=True); m.add('MarkerPlastic', mk.xform(Pd, Nd, mk.Ry(90))[0], np.full_like(UVd, 0) + plain, Nd)
    Pd, UVd, Nd = mk.disk(rb, 0.0, 3, up=False); m.add('MarkerPlastic', mk.xform(Pd, Nd, mk.Ry(90))[0], np.full_like(UVd, 0) + plain, Nd)
    m.ground()
    m.write(os.path.join(OUT, 'EvidenceMarkerCone.obj'), {'MarkerPlastic': dict(map='EvidenceMarkerCone_Atlas.png', Ks=0.3)})
    report('EvidenceMarkerCone', m)


if __name__ == '__main__':
    names = sys.argv[2:] or ['police_barrier', 'brick_half', 'shoeprint_cast']
    for n in names:
        globals()[n]()
