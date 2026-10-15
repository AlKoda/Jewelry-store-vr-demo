"""Dirt, grime, water stains, scuffs, scratches, drag and pry marks, oil, gum, dust film."""
import sys, os
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
from dutil import *

OUT = '.'


def grime_streaks(seed=71, H=1024, W=512):
    """Soot/dirt run-off under a ledge (window sill, fascia). 0.6 x 1.2 m, ledge at the top."""
    r = rng(seed)
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    t = yy / H
    srcv = smoothstep(-0.6, 1.6, fbm(1, W, 5, 4, 0.6, seed)[0] + 0.5 * fbm(1, W, 30, 2, 0.5, seed + 7)[0])[None, :]
    length = (0.2 + 0.75 * smoothstep(-1.2, 1.6, fbm(1, W, 4, 3, 0.6, seed + 1)[0]))[None, :]
    fine = ndi.gaussian_filter(r.standard_normal((H, W)), (60, 0.6))
    fine = fine / fine.std()
    run = np.exp(-t / (length * 0.5)) * smoothstep(length * 1.02, length * 0.75, t)
    streak = srcv * run * (0.6 + 0.4 * smoothstep(-1.5, 1.5, fine))
    ends = smoothstep(0.025, 0.0, np.abs(t - length * 0.92)) * srcv * 0.25 * (length > 0.4)
    band_h = 0.035 + 0.02 * smoothstep(-1, 1, fbm(1, W, 40, 2, 0.5, seed + 2)[0])[None, :]
    top = smoothstep(band_h, band_h * 0.2, t) * (0.8 + 0.2 * smoothstep(-1, 1, fbm(H, W, 14, 3, 0.5, seed + 3)))
    a = np.clip(np.maximum(streak * 0.55 + ends, top * 0.75), 0, 1)
    a *= smoothstep(0.0, 0.03, xx / W) * smoothstep(1.0, 0.97, xx / W)
    a = ndi.gaussian_filter(a, (0.6, 0.5))
    tint = fbm(H, W, 40, 3, 0.5, seed + 4)
    rgb = np.array([0.12, 0.11, 0.098])[None, None] * (1 + 0.1 * tint[..., None])
    rgb = rgb + np.array([0.07, 0.04, 0.0]) * (smoothstep(0.6, 1.8, tint) * smoothstep(0.1, 0.4, t))[..., None]
    save_rgba(os.path.join(OUT, 'GrimeStreaks_Wall.png'), rgb, a)


def dirt_entrance(seed=72, N=1024):
    """Wet street dirt tracked in through the forced door: faint partial shoe prints walking in
    (toward the top of the image), grit and a dirty haze near the threshold (bottom edge). 1.5 m."""
    import footprints as fp
    from PIL import Image as _I
    r = rng(seed)
    ppm = N / 1500.0
    yy, xx = np.mgrid[0:N, 0:N].astype(float) / N
    haze = smoothstep(-0.5, 1.5, fbm(N, N, 60, 4, 0.5, seed)) * smoothstep(0.55, 0.0, 1 - yy) ** 1.5
    a = haze * 0.28
    col = np.zeros((N, N, 3)) + np.array([0.30, 0.26, 0.21])
    acc_a = a.copy(); acc_c = col * a[..., None]
    pw, ph = int(200 * ppm), int(400 * ppm)
    tracks = [(0.36, -0.06, 'Boot', 0), (0.66, 0.08, 'Sneaker', 1)]
    for tx, ang, kind, ti in tracks:
        for k in range(5):
            foot = 'L' if (k + ti) % 2 == 0 else 'R'
            load = 0.9 - k * 0.15
            rgb, al, nrm, sm, mask = fp.make_print(kind, foot, 'Mud', seed * 10 + k + ti * 50, load)
            al = al * (0.55 - k * 0.07)
            rot = np.degrees(ang) + r.normal(0, 3)
            Ai = _I.fromarray((np.clip(al, 0, 1) * 255).astype(np.uint8)).resize((pw, ph), _I.LANCZOS).rotate(rot, _I.BICUBIC, expand=True)
            Ci = _I.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8)).resize((pw, ph), _I.LANCZOS).rotate(rot, _I.BICUBIC, expand=True)
            A = np.asarray(Ai, float) / 255; C = np.asarray(Ci, float) / 255
            side = (-1 if foot == 'L' else 1) * 0.045
            cx = int((tx + side) * N); cy = int((0.86 - k * 0.19) * N)
            h2, w2 = A.shape
            y0, x0 = cy - h2 // 2, cx - w2 // 2
            ys, xs = slice(max(0, y0), min(N, y0 + h2)), slice(max(0, x0), min(N, x0 + w2))
            sub = A[ys.start - y0:ys.stop - y0, xs.start - x0:xs.stop - x0]
            subc = C[ys.start - y0:ys.stop - y0, xs.start - x0:xs.stop - x0]
            acc_c[ys, xs] = acc_c[ys, xs] * (1 - sub[..., None]) + subc * sub[..., None]
            acc_a[ys, xs] = acc_a[ys, xs] * (1 - sub) + sub
    grit = (r.random((N, N)) < 0.0025 * (haze + 0.15)).astype(float)
    g = np.clip(ndi.gaussian_filter(grit, 0.6) * 2.5, 0, 1)
    acc_c = acc_c * (1 - g[..., None]) + np.array([0.18, 0.16, 0.13]) * g[..., None]
    acc_a = np.maximum(acc_a, g * 0.8)
    acc_a *= smoothstep(0.0, 0.04, xx) * smoothstep(1.0, 0.96, xx) * smoothstep(0.0, 0.03, yy)
    rgb = acc_c / np.maximum(acc_a, 1e-4)[..., None]
    save_rgba(os.path.join(OUT, 'DirtTracked_Entrance.png'), np.clip(rgb, 0, 1), acc_a)


def dirt_edge(seed=73, H=256, W=1024):
    """Dust and grit collected along a wall base or kerb face. 2.0 x 0.25 m, wall side = top edge."""
    r = rng(seed)
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    t = yy / H
    prof = np.exp(-t / (0.18 + 0.12 * smoothstep(-1, 1, fbm(1, W, 60, 3, 0.5, seed)[0])[None, :]))
    a = prof * (0.55 + 0.45 * smoothstep(-1, 1.2, fbm(H, W, 6, 4, 0.55, seed + 1)))
    grit = ndi.gaussian_filter((r.random((H, W)) < 0.02 * prof).astype(float), 0.6) * 2.5
    fluff = ndi.gaussian_filter((r.random((H, W)) < 0.0008 * prof).astype(float), 1.6) * 8
    a = np.clip(a * 0.62 + grit * 0.7 + fluff * 0.3, 0, 1)
    a *= smoothstep(0, 0.03, xx / W) * smoothstep(1, 0.97, xx / W)
    col = np.array([0.30, 0.28, 0.25])[None, None] * (1 + 0.1 * fbm(H, W, 20, 3, 0.5, seed + 2)[..., None])
    col = col * (1 - 0.45 * np.clip(grit, 0, 1)[..., None]) + 0.25 * np.clip(fluff, 0, 1)[..., None]
    save_rgba(os.path.join(OUT, 'DirtEdge_WallBase.png'), col, a)


def tide_stain(name, seed, N, ring_col, fill_col, fill_a, ring_a, n_rings):
    """Water stain with tide lines (successive drying fronts): smooth lobed outline, thin dark
    rings with a sharp outer edge and soft inner side (coffee-ring effect)."""
    r = rng(seed)
    yy, xx = np.mgrid[0:N, 0:N].astype(float)
    u = xx / N * 2 - 1; v = yy / N * 2 - 1
    g = fbm(N, N, 140, 3, 0.45, seed)
    g = warp(g, fbm(N, N, 220, 2, 0.5, seed + 1) * 25, fbm(N, N, 220, 2, 0.5, seed + 2) * 25)
    small = fbm(N, N, 14, 2, 0.5, seed + 9)
    field = np.sqrt(u ** 2 + v ** 2) + 0.16 * g + 0.006 * small
    levels = np.sort(r.uniform(0.38, 0.80, n_rings))[::-1]
    outer = levels[0]
    inside = smoothstep(outer + 0.003, outer - 0.003, field)
    fill = inside * fill_a * (0.7 + 0.3 * smoothstep(-1.5, 1.5, fbm(N, N, 50, 3, 0.5, seed + 3)))
    fill *= 0.6 + 0.4 * smoothstep(outer - 0.25, outer, field)
    rings = np.zeros((N, N))
    for k, lv in enumerate(levels):
        d = lv - field
        ring = smoothstep(-0.0025, 0.001, d) * np.exp(-np.clip(d, 0, None) / 0.008)
        rings = np.maximum(rings, ring * (1.0 if k == 0 else r.uniform(0.4, 0.75)))
    rings *= 0.8 + 0.2 * smoothstep(-1, 1, fbm(N, N, 25, 3, 0.5, seed + 4))
    a = np.clip(np.maximum(fill, rings * ring_a), 0, 1)
    w_ring = rings * ring_a
    col = (np.array(fill_col) * fill[..., None] + np.array(ring_col) * w_ring[..., None]) / (fill + w_ring + 1e-6)[..., None]
    save_rgba(os.path.join(OUT, name + '.png'), col, a)


def heel_scuffs(seed=75, N=1024):
    """Black rubber heel marks and sole scuffs on a pale stone floor. 1.0 m.
    Each mark: dense start (heel strike), striated rubber smear, tapering tail."""
    r = rng(seed)
    ss = 2
    acc = np.zeros((N * ss, N * ss))
    ppm = N * ss / 1000.0
    for i in range(30):
        im = Image.new('L', (N * ss, N * ss), 0)
        d = ImageDraw.Draw(im)
        cx, cy = r.uniform(0.1, 0.9) * N * ss, r.uniform(0.1, 0.9) * N * ss
        L = r.uniform(20, 110) * ppm
        a0 = r.uniform(0, 2 * np.pi)
        curv = r.normal(0, 0.0015)
        w = r.uniform(5, 15) * ppm
        nlines = max(3, int(w / (0.8 * ppm)))
        for j in range(nlines):
            off = (j / (nlines - 1) - 0.5) * w
            strength = r.uniform(0.3, 1.0)
            lenf = r.uniform(0.7, 1.0)
            pts = []
            for s in np.linspace(0, lenf, 24):
                ang = a0 + curv * s * L
                px = cx + np.cos(ang) * s * L - np.sin(a0) * off * (1 - 0.6 * s)
                py = cy + np.sin(ang) * s * L + np.cos(a0) * off * (1 - 0.6 * s)
                pts.append((px, py))
            for k in range(len(pts) - 1):
                s = k / (len(pts) - 1)
                val = int(255 * strength * (1 - s) ** 0.7)
                d.line([pts[k], pts[k + 1]], fill=val, width=max(1, int(1.6 * ppm)))
        m = np.asarray(im, float) / 255
        acc = np.maximum(acc, ndi.gaussian_filter(m, 0.6 * ss))
    m = acc.reshape(N, ss, N, ss).mean((1, 3))
    a = np.clip(m * 1.3, 0, 0.92)
    col = np.broadcast_to(np.array([0.05, 0.048, 0.046]), (N, N, 3)).copy()
    save_rgba(os.path.join(OUT, 'FloorScuffs_Heel.png'), col, a)


def fine_scratches(seed=76, N=1024):
    """Fine wear scratches on polished stone or a glass case top (use as a smoothness breaker)."""
    r = rng(seed)
    im = Image.new('L', (N * 2, N * 2), 0)
    d = ImageDraw.Draw(im)
    dom = r.uniform(0, np.pi)
    for i in range(420):
        cx, cy = r.uniform(0, 1) * N * 2, r.uniform(0, 1) * N * 2
        L = (r.pareto(1.5) * 40 + 15) * 2
        a0 = dom + r.normal(0, 0.35) if r.random() < 0.6 else r.uniform(0, np.pi)
        curv = r.normal(0, 0.0015)
        val = int(r.uniform(60, 255))
        pts = [(cx + np.cos(a0 + curv * s) * s, cy + np.sin(a0 + curv * s) * s) for s in np.linspace(-L / 2, L / 2, 12)]
        d.line(pts, fill=val, width=1 if r.random() < 0.8 else 2)
    m = np.asarray(im.resize((N, N), Image.LANCZOS), float) / 255
    # broken, intermittent scratches
    m *= smoothstep(-0.8, 0.6, blur_noise(N, N, 2.5, seed + 1))
    m *= 0.6 + 0.4 * smoothstep(-1, 1, fbm(N, N, 150, 3, 0.5, seed + 2))
    a = np.clip(m * 0.55, 0, 1)
    col = np.broadcast_to(np.array([0.85, 0.85, 0.84]), (N, N, 3)).copy()
    save_rgba(os.path.join(OUT, 'FloorScratches_Fine.png'), col, border_fade(a, 0.06))


def drag_marks(seed=77, H=2048, W=512):
    """Heavy object dragged across a stone floor: two parallel bands of scratches and gouges with
    grit ploughed to the sides. 0.6 x 2.4 m, drag direction along V."""
    r = rng(seed)
    ppm = W / 600.0
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    t = yy / H
    wob = W * 0.5 + 12 * ppm * fbm(H, 1, 300, 2, 0.5, seed)[:, 0][:, None]
    a = np.zeros((H, W)); hgt = np.zeros((H, W))
    for side in (-1, 1):
        cx = wob + side * 210 * ppm + 4 * ppm * fbm(H, 1, 120, 2, 0.5, seed + side)[:, 0][:, None]
        dx = xx - cx
        band_w = 22 * ppm
        inband = smoothstep(band_w, band_w * 0.75, np.abs(dx))
        # many parallel scratches: noise varying across, almost constant along
        cross = ndi.gaussian_filter(r.standard_normal((H, W)), (60, 0.6))
        cross = cross / cross.std()
        scr = smoothstep(0.6, 1.8, cross) * inband
        gouge = smoothstep(1.6, 2.6, ndi.gaussian_filter(r.standard_normal((H, W)), (200, 1.2)) * 3.2) * inband
        plough = smoothstep(band_w + 10 * ppm, band_w, np.abs(dx)) * smoothstep(band_w * 0.8, band_w, np.abs(dx))
        grit = ndi.gaussian_filter((r.random((H, W)) < 0.03 * plough).astype(float), 0.6) * 2.5
        intensity = smoothstep(0.0, 0.08, t) * smoothstep(1.0, 0.82, t) * (0.6 + 0.4 * smoothstep(-1, 1, fbm(H, 1, 80, 2, 0.5, seed + 5)[:, 0][:, None]))
        a = np.maximum(a, (scr * 0.8 + gouge * 0.9) * intensity)
        a = np.maximum(a, np.clip(grit, 0, 1) * 0.7 * intensity)
        hgt -= (scr * 0.3 + gouge * 1.0) * intensity
        hgt += np.clip(grit, 0, 1) * 0.4 * intensity
    col = np.broadcast_to(np.array([0.88, 0.87, 0.85]), (H, W, 3)).copy()
    save_rgba(os.path.join(OUT, 'DragMarks_Floor.png'), col, a)
    save_normal(os.path.join(OUT, 'DragMarks_Floor_Normal.png'), normal_from_height(hgt, 3.0))


def pry_marks(seed=78, N=512):
    """Flat-bladed tool (crowbar / large screwdriver) pry marks on an anodised or painted metal
    door frame beside the lock. 0.12 x 0.12 m; the frame edge (door gap) is the right edge."""
    r = rng(seed)
    ppm = N / 120.0
    yy, xx = np.mgrid[0:N, 0:N].astype(float)
    lx = (N - xx) / ppm  # mm from the frame edge
    hgt = np.zeros((N, N)); expo = np.zeros((N, N)); scr = np.zeros((N, N))
    marks = [(30, 23, 11), (60, 21, 8), (92, 25, 14)]
    nz = fbm(N, N, 5, 3, 0.5, seed)
    for k, (ymm, wmm, Lmm) in enumerate(marks):
        ly = (yy / ppm - ymm - r.normal(0, 1))
        hw = wmm / 2
        qx = np.abs(lx - Lmm / 2) - Lmm / 2 + 2.5
        qy = np.abs(ly) - hw + 2.5
        sd = np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2) + np.minimum(np.maximum(qx, qy), 0) - 2.5
        inside = smoothstep(0.6, -0.6, sd)
        depth = inside * (1 - np.clip(lx / Lmm, 0, 1)) ** 1.3 * (1.6 + 0.3 * r.normal())
        hgt -= depth
        hgt += 0.35 * smoothstep(-0.2, 0.6, sd) * smoothstep(1.6, 0.6, sd) * (np.abs(ly) > hw - 1.5)
        expo = np.maximum(expo, smoothstep(0.75, 1.25, depth + 0.35 * nz) * inside)
        expo = np.maximum(expo, 0.8 * smoothstep(-0.3, 0.3, sd) * smoothstep(0.9, 0.3, sd) * smoothstep(0.2, 0.9, nz + 0.5))
        flakes = smoothstep(2.0, 0.0, sd) * smoothstep(1.3, 1.8, nz + 0.5 * smoothstep(1.5, 0, sd)) * (sd > 0)
        expo = np.maximum(expo, flakes)
        for j in range(r.integers(3, 7)):
            yo = r.uniform(-hw, hw)
            Ls = r.uniform(8, 28)
            line = smoothstep(0.35, 0.0, np.abs(ly - yo - 0.04 * (lx - Lmm))) * smoothstep(Lmm + Ls, Lmm, lx) * smoothstep(Lmm - 2, Lmm + 1, lx)
            scr = np.maximum(scr, line * r.uniform(0.5, 1.0))
    smudge = smoothstep(14, 0, lx) * smoothstep(0.5, 1.5, fbm(N, N, 20, 3, 0.5, seed + 5)) * 0.15 + 0.25 * smoothstep(0.05, 0.4, -hgt)
    a = np.clip(np.maximum(np.maximum(expo, scr * 0.8), smudge), 0, 1)
    metal = np.array([0.66, 0.66, 0.64]); grime = np.array([0.12, 0.115, 0.11])
    wm = np.maximum(expo, scr)
    col = metal[None, None] * wm[..., None] + grime[None, None] * (1 - wm[..., None])
    brushed = ndi.gaussian_filter(r.standard_normal((N, N)), (0.6, 14))
    brushed = brushed / brushed.std()
    col = col * (0.88 + 0.12 * smoothstep(-2, 2, brushed)[..., None])
    hgt -= scr * 0.15 + expo * 0.05
    save_rgba(os.path.join(OUT, 'PryMarks_DoorFrame.png'), col, a)
    save_normal(os.path.join(OUT, 'PryMarks_DoorFrame_Normal.png'), normal_from_height(ndi.gaussian_filter(hgt, 0.7), 4.0))
    save_ms(os.path.join(OUT, 'PryMarks_DoorFrame_MetallicSmoothness.png'), 0.3 + 0.3 * wm, metal=wm * 0.95)


def oil_stain(seed=79, N=1024):
    """Engine oil soaked into asphalt or concrete under a parked car. 1.0 m."""
    r = rng(seed)
    yy, xx = np.mgrid[0:N, 0:N].astype(float)
    u = xx / N * 2 - 1; v = yy / N * 2 - 1
    g = fbm(N, N, 110, 3, 0.45, seed)
    g = warp(g, fbm(N, N, 200, 2, 0.5, seed + 1) * 20, fbm(N, N, 200, 2, 0.5, seed + 2) * 20)
    field = np.sqrt((u * 1.25) ** 2 + (v * 0.95) ** 2) + 0.2 * g
    core = smoothstep(0.42, 0.18, field)
    soak = np.exp(-np.clip(field - 0.18, 0, None) / 0.16) * (0.85 + 0.15 * smoothstep(-1.2, 1.2, fbm(N, N, 40, 3, 0.5, seed + 3)))
    drips = np.zeros((N, N))
    for i in range(22):
        ang = r.uniform(0, 2 * np.pi); rad = r.uniform(0.3, 0.8)
        cx, cy = N / 2 + np.cos(ang) * rad * N / 2, N / 2 + np.sin(ang) * rad * N / 2
        rr = r.uniform(4, 13)
        disc_stamp(drips, cx, cy, rr * 1.6, r.uniform(0.35, 0.7), soft=rr * 0.9)
    a = np.clip(np.maximum(core * 0.35 + soak * 0.5, drips * 0.6), 0, 0.82)
    a *= smoothstep(1.0, 0.88, np.maximum(np.abs(u), np.abs(v)))
    col = np.array([0.05, 0.045, 0.04])[None, None] * (1 + 0.06 * fbm(N, N, 30, 3, 0.5, seed + 4)[..., None])
    col = col + np.array([0.03, 0.022, 0.01]) * (1 - core)[..., None]
    save_rgba(os.path.join(OUT, 'OilStain.png'), col, a)
    save_ms(os.path.join(OUT, 'OilStain_MetallicSmoothness.png'), np.clip(0.25 + 0.55 * core + 0.25 * drips + 0.15 * soak, 0, 0.9))


def chewing_gum(seed=80, N=1024):
    """Flattened, blackened chewing-gum spots on pavement. 1.0 m."""
    r = rng(seed)
    ppm = N / 1000.0
    a = np.zeros((N, N)); col = np.zeros((N, N, 3)); hgt = np.zeros((N, N))
    yy, xx = np.mgrid[0:N, 0:N].astype(float)
    for i in range(34):
        cx, cy = r.uniform(0.06, 0.94) * N, r.uniform(0.06, 0.94) * N
        rad = r.uniform(7, 16) * ppm
        x0, x1 = int(max(0, cx - rad * 2)), int(min(N, cx + rad * 2))
        y0, y1 = int(max(0, cy - rad * 2)), int(min(N, cy + rad * 2))
        sx, sy = xx[y0:y1, x0:x1], yy[y0:y1, x0:x1]
        ang = np.arctan2(sy - cy, sx - cx)
        rr = np.sqrt((sx - cx) ** 2 + ((sy - cy) * r.uniform(0.85, 1.15)) ** 2)
        edge = rad * (1 + 0.05 * np.sin(ang * r.integers(2, 5) + r.uniform(0, 6)) + 0.025 * np.sin(ang * 7 + r.uniform(0, 6)))
        m = smoothstep(edge + 0.8, edge - 0.8, rr)
        age = r.random()
        base = (np.array([0.58, 0.55, 0.53]) if r.random() < 0.8 else np.array([0.62, 0.50, 0.50])) * (1 - age) + np.array([0.09, 0.085, 0.08]) * age
        tex = 0.85 + 0.15 * fbm(y1 - y0, x1 - x0, 1.2, 2, 0.5, seed + i, )
        c = base[None, None] * tex[..., None]
        rim = smoothstep(edge * 0.75, edge, rr) * m
        c = c * (1 - 0.3 * rim[..., None])
        a_sub = m * (0.9 - 0.2 * (1 - age))
        sel = a_sub > a[y0:y1, x0:x1]
        a[y0:y1, x0:x1] = np.where(sel, a_sub, a[y0:y1, x0:x1])
        col[y0:y1, x0:x1] = np.where(sel[..., None], c, col[y0:y1, x0:x1])
        hgt[y0:y1, x0:x1] = np.maximum(hgt[y0:y1, x0:x1], m * (0.6 + 0.3 * smoothstep(edge, 0, rr)) + 0.1 * tex * m)
    save_rgba(os.path.join(OUT, 'ChewingGum_Scatter.png'), col, a)
    save_normal(os.path.join(OUT, 'ChewingGum_Scatter_Normal.png'), normal_from_height(ndi.gaussian_filter(hgt, 0.8), 3.0))


def dust_shelf(seed=81, H=1024, W=1024):
    """Even dust film on a display shelf / case floor with crisp clean outlines where items were
    lifted, and four-finger glove drag marks. 0.6 x 0.6 m."""
    r = rng(seed)
    ppm = W / 600.0
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    film = 0.85 + 0.15 * smoothstep(-1.5, 1.5, fbm(H, W, 80, 4, 0.5, seed))
    film += 0.35 * smoothstep(0.12, 0.0, yy / H)
    grains = (r.random((H, W)) > 0.988).astype(float) * 0.5
    im = Image.new('L', (W, H), 0); d = ImageDraw.Draw(im)
    for i in range(60):
        x, y = r.uniform(0, W), r.uniform(0, H); L = r.uniform(3, 12) * ppm; a0 = r.uniform(0, 6.3)
        pts = [(x + np.cos(a0 + 0.25 * np.sin(s * 0.3)) * s, y + np.sin(a0 + 0.25 * np.sin(s * 0.3)) * s) for s in np.linspace(0, L, 10)]
        d.line(pts, fill=int(r.uniform(90, 200)), width=1)
    fibres = np.asarray(im, float) / 255
    clean = np.zeros((H, W))
    shapes = [('rect', 0.22, 0.27, 60, 60, 0.1), ('rect', 0.47, 0.25, 60, 60, -0.05), ('oval', 0.76, 0.32, 95, 70, 0.0),
              ('rect', 0.30, 0.66, 150, 55, 0.04), ('circ', 0.64, 0.60, 24, 24, 0), ('circ', 0.72, 0.68, 22, 22, 0), ('circ', 0.82, 0.60, 24, 24, 0)]
    for kind, fx, fy, wmm, hmm, ang in shapes:
        cx, cy = fx * W, fy * H
        c, s = np.cos(ang), np.sin(ang)
        lx = ((xx - cx) * c + (yy - cy) * s) / (wmm * ppm / 2)
        ly = (-(xx - cx) * s + (yy - cy) * c) / (hmm * ppm / 2)
        dd = np.maximum(np.abs(lx), np.abs(ly)) if kind == 'rect' else np.sqrt(lx ** 2 + ly ** 2)
        clean = np.maximum(clean, smoothstep(1.015, 0.985, dd))
        film = film + 0.3 * smoothstep(1.0, 1.03, dd) * smoothstep(1.1, 1.03, dd)
    sw = np.zeros((H, W))
    for (sx, sy, ang, L) in [(0.50, 0.92, -1.75, 210), (0.86, 0.90, -1.95, 170)]:
        for f in range(4):
            off = (f - 1.5) * 19 * ppm
            cx = sx * W + np.cos(ang + np.pi / 2) * off
            cy = sy * H + np.sin(ang + np.pi / 2) * off
            lx = (xx - cx) * np.cos(ang) + (yy - cy) * np.sin(ang)
            ly = -(xx - cx) * np.sin(ang) + (yy - cy) * np.cos(ang)
            Lf = L * ppm * r.uniform(0.75, 1.0)
            stroke = smoothstep(8 * ppm, 5 * ppm, np.abs(ly)) * smoothstep(-2, 6 * ppm, lx) * smoothstep(Lf, Lf * 0.8, lx)
            sw = np.maximum(sw, stroke * r.uniform(0.7, 1.0))
    streaks = smoothstep(-0.8, 1.0, ndi.gaussian_filter(r.standard_normal((H, W)), 1.2) * 2.5)
    a = film * 0.30 * (1 - clean * 0.94) * (1 - sw * (0.55 + 0.35 * streaks)) + grains * (1 - clean) * (1 - sw) + fibres * 0.45
    a = np.clip(a, 0, 1) * smoothstep(0, 0.015, xx / W) * smoothstep(1, 0.985, xx / W) * smoothstep(0, 0.015, yy / H) * smoothstep(1, 0.985, yy / H)
    col = np.broadcast_to(np.array([0.70, 0.68, 0.64]), (H, W, 3)).copy()
    save_rgba(os.path.join(OUT, 'DustFilm_MissingItems.png'), col, a)


if __name__ == '__main__':
    OUT = sys.argv[1]
    which = sys.argv[2:] or ['grime', 'entr', 'edge', 'wsc', 'wsf', 'scuff', 'scr', 'drag', 'pry', 'oil', 'gum', 'dust']
    if 'grime' in which: grime_streaks()
    if 'entr' in which: dirt_entrance()
    if 'edge' in which: dirt_edge()
    if 'wsc' in which: tide_stain('WaterStain_Ceiling', 74, 1024, (0.42, 0.30, 0.17), (0.62, 0.52, 0.36), 0.35, 0.75, 4)
    if 'wsf' in which: tide_stain('WaterStain_FloorDried', 82, 1024, (0.86, 0.85, 0.82), (0.80, 0.79, 0.76), 0.10, 0.45, 5)
    if 'scuff' in which: heel_scuffs()
    if 'scr' in which: fine_scratches()
    if 'drag' in which: drag_marks()
    if 'pry' in which: pry_marks()
    if 'oil' in which: oil_stain()
    if 'gum' in which: chewing_gum()
    if 'dust' in which: dust_shelf()
