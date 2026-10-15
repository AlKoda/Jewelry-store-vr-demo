"""Shoe-print decals: generic (non-branded) sneaker and lug-boot soles, mud and dust transfer.
Image 512 x 1024 px = 0.20 x 0.40 m (2.56 px/mm). Toe points up (+V). Left prints have the
medial (big-toe) side on the right of the image, as seen from above."""
import sys, os
import numpy as np
from scipy import ndimage as ndi
from scipy.interpolate import splprep, splev
from dutil import *

W, H = 512, 1024
PPM = 2.56  # px per mm
HEEL_ROW = H - 55 * PPM

OUTLINE_L = np.array([
    (0, 0), (-25, 6), (-35, 22), (-38, 45), (-37.5, 70), (-35.5, 100), (-35, 125), (-38, 150),
    (-44, 175), (-48, 200), (-48.5, 222), (-45, 243), (-37, 262), (-25, 278), (-9, 288), (6, 290),
    (20, 286), (33, 276), (45, 259), (52, 239), (55, 216), (53, 192), (46, 170), (37, 150),
    (31, 128), (30.5, 108), (33, 88), (36, 64), (37, 42), (33, 20), (24, 6)], float)


def outline_poly(scale_x=1.0, scale_y=1.0, n=400, square_toe=0.0):
    p = OUTLINE_L.copy()
    p[:, 0] *= scale_x
    p[:, 1] *= scale_y
    if square_toe:
        top = p[:, 1] > 250 * scale_y
        p[top, 1] += square_toe * (np.abs(p[top, 0]) / 50.0) * (p[top, 1] - 250 * scale_y) / 40.0
    pts = np.vstack([p, p[:1]])
    tck, _ = splprep([pts[:, 0], pts[:, 1]], s=0, per=True)
    u = np.linspace(0, 1, n, endpoint=False)
    x, y = splev(u, tck)
    return np.stack([x, y], 1)


def mm_grid():
    rows, cols = np.mgrid[0:H, 0:W].astype(float)
    x = (cols - W / 2 + 0.5) / PPM - 5.0  # shift a bit so the sole is centred (outline spans -48..55)
    y = (HEEL_ROW - rows) / PPM
    return x, y


def to_px(poly):
    return np.stack([(poly[:, 0] + 5.0) * PPM + W / 2 - 0.5, HEEL_ROW - poly[:, 1] * PPM], 1)


def sole_masks(poly):
    m = poly_mask(H, W, [to_px(poly)], ss=4)
    inside = m > 0.5
    d_in = ndi.distance_transform_edt(inside) / PPM  # mm from edge, inside
    d_out = ndi.distance_transform_edt(~inside) / PPM
    return m, d_in, d_out


def bevel(binary, mm=0.9):
    d = ndi.distance_transform_edt(binary) / PPM
    return np.clip(d / mm, 0, 1) ** 0.7


def tread_sneaker(x, y, d_in, seed):
    """Height of the sole's contact surface (1 = touches the ground)."""
    rim_w = 4.0
    rim = (d_in > 0.3) & (d_in < rim_w)
    # siping notches on the rim (transverse cuts every ~6 mm following the edge)
    ang = np.arctan2(y - 145, x - 3)
    per = (ang * 70 / np.pi) % 1.0
    rim &= ~((per > 0.0) & (per < 0.18))
    inner = d_in >= rim_w + 1.6
    # herringbone (zigzag chevrons): period 4.2 mm, chevron width 18 mm
    zz = np.abs(((x + 9.0) % 18.0) - 9.0) * 0.7
    s1 = ((y + zz) % 4.2) / 4.2
    herring = s1 > 0.36
    # heel: reversed chevrons, slightly coarser
    s2 = ((y - zz) % 5.0) / 5.0
    heel_pat = s2 > 0.38
    # pivot circle under the ball (medial)
    r = np.sqrt((x - 16.0) ** 2 + (y - 214.0) ** 2)
    pivot = (r < 17.0)
    rings = ((r % 3.0) / 3.0 > 0.40) | (r < 2.2)
    ring_border = (r > 17.0) & (r < 18.6)
    # midfoot bars
    bars = ((y % 7.0) / 7.0 > 0.45)
    # flex grooves
    flex = (np.abs(y - 176) < 1.3) | (np.abs(y - 247) < 1.2) | (np.abs(y - 92) < 1.1)
    pat = np.where(y < 95, heel_pat, np.where(y < 150, bars, herring))
    pat = np.where(pivot, rings, pat) & ~ring_border
    toe_band = (y > 268) & (((y % 3.4) / 3.4) > 0.42)
    pat = np.where(y > 268, toe_band, pat)
    contact = (inner & pat & ~flex) | rim
    hgt = bevel(contact, 0.8)
    return hgt


def tread_boot(x, y, d_in, seed):
    r = rng(seed)
    # perimeter lugs: blocks along the edge, 3.5..9 mm in from the edge
    ang = np.arctan2(y - 150, (x - 3) * 2.6)
    blk = ((ang * 26 / np.pi) % 1.0) < 0.62
    perim = (d_in > 0.2) & (d_in < 8.5) & blk
    edge = (d_in > 0.2) & (d_in < 2.2)
    inner = d_in >= 10.5
    # forefoot chevron lugs: rows every 15 mm, lugs 13 mm wide with 5.5 mm channels
    row_h = 15.0
    ry = np.floor((y - 3) / row_h)
    off = (ry % 2) * 9.0
    lx = (x + off) % 18.0 - 9.0
    ly = ((y - 3) % row_h) - 0.32 * np.abs(lx)
    fore = (np.abs(lx) < 6.3) & (ly > 2.2) & (ly < 10.3)
    # heel: horizontal bars with a centre notch
    hl = (((y - 2) % 11.0) > 3.6) & (np.abs(x - 1.5) > 2.6)
    heel = (y < 82) & hl
    shank = (y >= 86) & (y <= 122)
    pat = np.where(y < 84, heel, np.where(shank, False, fore))
    contact = (inner & pat) | ((perim | edge) & ~shank)
    # the heel breast and the forefoot leading edge of the shank
    contact |= (np.abs(y - 83) < 1.6) & (d_in > 0.2)
    contact |= (np.abs(y - 124) < 1.6) & (d_in > 0.2)
    contact &= ~(shank & (d_in > 0.2))
    hgt = bevel(contact, 1.3)
    return hgt


def add_wear_and_racs(tread, xs, y, d_in, seed, wear_amt):
    """Worn-flat zones (lateral heel, ball) and randomly acquired characteristics:
    cuts, stone holes and an embedded stone. These are what identify one shoe."""
    r = rng(seed + 500)
    inner = smoothstep(0.3, 1.5, d_in)
    wn = blur_noise(H, W, 6, seed + 501, wrap=False)
    heelw = np.exp(-(((xs + 22) / 18) ** 2 + ((y - 22) / 20) ** 2))
    ballw = np.exp(-(((xs - 6) / 22) ** 2 + ((y - 208) / 20) ** 2)) * 0.7
    wear = np.clip((heelw + ballw) * wear_amt + 0.15 * wn, 0, 1)
    tread = np.maximum(tread, smoothstep(0.45, 0.8, wear) * 0.9 * inner)
    cut = np.zeros_like(tread)
    from PIL import Image, ImageDraw
    im = Image.new('L', (W * 2, H * 2), 0)
    dr = ImageDraw.Draw(im)
    cand = np.argwhere(d_in > 6)
    for i in range(r.integers(3, 6)):
        rr, cc = cand[r.integers(len(cand))]
        L = r.uniform(4, 14) * PPM
        a = r.uniform(0, np.pi)
        x0, y0 = cc - np.cos(a) * L / 2, rr - np.sin(a) * L / 2
        x1, y1 = cc + np.cos(a) * L / 2, rr + np.sin(a) * L / 2
        xm, ym = (x0 + x1) / 2 + r.normal(0, 2), (y0 + y1) / 2 + r.normal(0, 2)
        dr.line([(x0 * 2, y0 * 2), (xm * 2, ym * 2), (x1 * 2, y1 * 2)], fill=255, width=max(2, int(r.uniform(0.45, 0.8) * PPM * 2)))
    for i in range(r.integers(2, 4)):
        rr, cc = cand[r.integers(len(cand))]
        rad = r.uniform(0.8, 1.6) * PPM * 2
        dr.ellipse([cc * 2 - rad, rr * 2 - rad, cc * 2 + rad, rr * 2 + rad], fill=255)
    cut = np.asarray(im.resize((W, H), Image.LANCZOS), float) / 255
    tread = tread * (1 - cut)
    # embedded stone: a small solid contact in a groove
    rr, cc = cand[r.integers(len(cand))]
    disc_stamp(tread, cc, rr, r.uniform(1.0, 1.8) * PPM, 1.0, soft=1.0)
    return tread


def pressure_map(x, y, foot_sign, seed, kind):
    """Relative contact pressure for a walking step. foot_sign=+1 left (medial on +x)."""
    xm = x * foot_sign
    heel = np.exp(-(((xm - 0) / 32) ** 2 + ((y - 38) / 34) ** 2))
    ball = np.exp(-(((xm - 8) / 40) ** 2 + ((y - 212) / 34) ** 2))
    toes = np.exp(-(((xm - 12) / 30) ** 2 + ((y - 272) / 16) ** 2))
    arch = np.exp(-(((xm - 26) / 18) ** 2 + ((y - 125) / 30) ** 2))
    lateral = np.clip(-xm / 45, -1, 1)
    p = 0.42 + 0.55 * heel + 0.5 * ball + 0.3 * toes - 0.65 * arch + 0.12 * lateral
    n = blur_noise(H, W, 18, seed=seed + 7, wrap=False)
    p += 0.13 * n
    return p


def make_print(kind, foot, medium, seed, load=1.0):
    x, y = mm_grid()
    foot_sign = 1 if foot == 'L' else -1
    if kind == 'Sneaker':
        poly = outline_poly(1.0, 1.0)
    else:
        poly = outline_poly(1.05, 1.04, square_toe=4.0)
    if foot == 'R':
        poly = poly * np.array([-1, 1]) + np.array([10.0 - 0.0, 0])  # mirror about x = 5 mm (image centre)
    mask, d_in, d_out = sole_masks(poly)
    xs = x if foot == 'L' else (10.0 - x)  # tread pattern defined for a left sole
    if kind == 'Sneaker':
        tread = tread_sneaker(xs, y, d_in, seed)
    else:
        tread = tread_boot(xs, y, d_in, seed)
    tread = add_wear_and_racs(tread, xs, y, d_in, seed, 0.9 if kind == 'Sneaker' else 0.45)
    p = pressure_map(x, y, foot_sign, seed, kind) * load
    r = rng(seed)
    if medium == 'Mud':
        # mud load on the sole: blotchy, with heavy cakes in places
        cake = fbm(H, W, 30, 5, 0.55, seed + 1)
        grain = fbm(H, W, 2.0, 3, 0.6, seed + 2)
        cover = p + 0.28 * cake + 0.12 * grain
        dep_face = tread * smoothstep(0.30, 0.56, cover)
        # grooves packed with mud in some places (prints show filled pattern there)
        packed = smoothstep(0.55, 0.95, cake + 0.4 * p - 0.15) * (mask > 0.5) * smoothstep(0.3, 1.2, d_in)
        dep = np.maximum(dep_face, 0.75 * packed * smoothstep(-0.2, 0.4, grain + 0.2))
        # thickness of the deposit
        thick = dep * (0.55 + 0.45 * smoothstep(-1, 1.5, cake)) * (0.7 + 0.3 * smoothstep(-1, 1, grain))
        # toe-off smear (backward) and heel-strike smear (backward at the heel)
        toe_zone = smoothstep(240, 285, y)
        sm = ndi.uniform_filter1d(thick, size=int(3.5 * PPM), axis=0, origin=-int(1.4 * PPM))
        thick = np.maximum(thick, 0.65 * sm * toe_zone * smoothstep(3.0, 0.0, d_out))
        heel_zone = smoothstep(25, 0, y) * (mask > 0.3)
        sm2 = ndi.uniform_filter1d(thick, size=int(4 * PPM), axis=0, origin=int(1.5 * PPM))
        thick = np.maximum(thick, 0.6 * sm2 * smoothstep(30, -5, y) * smoothstep(-1.5, 1.0, -d_out))
        # ragged deposit edges
        thick *= smoothstep(-0.1, 0.25, thick + 0.15 * grain)
        # crumbs of mud around the print, mostly ahead of the toe and at the sides
        crumbs = np.zeros((H, W))
        n_c = int(140 * load)
        for i in range(n_c):
            # sample a point near the outline
            k = r.integers(0, len(poly))
            bx, by = poly[k]
            dist = r.exponential(5.0) + 1.0
            if by > 230:
                dist *= 1.6
            nx_, ny_ = (bx - 3), (by - 150) * 0.4
            nn = np.hypot(nx_, ny_) + 1e-6
            px_ = bx + nx_ / nn * dist + r.normal(0, 2)
            py_ = by + ny_ / nn * dist + r.normal(0, 2)
            col = (px_ + 5.0) * PPM + W / 2 - 0.5
            row = HEEL_ROW - py_ * PPM
            rad = (r.pareto(3.0) * 0.5 + 0.35) * PPM
            disc_stamp(crumbs, col, row, min(rad, 3.5 * PPM), r.uniform(0.5, 1.0), soft=0.8)
        crumbs *= smoothstep(-0.6, 0.6, grain + 0.3)
        thick = np.maximum(thick, crumbs * 0.9)
        alpha = np.clip(smoothstep(0.02, 0.35, thick) * (0.82 + 0.18 * smoothstep(0, 1, thick)), 0, 1)
        # colour: wet dark brown where thick, lighter drying rims and silty film where thin
        wet = smoothstep(0.25, 0.85, thick)
        base_dry = np.array([0.40, 0.33, 0.25])
        base_wet = np.array([0.17, 0.125, 0.085])
        col = base_dry[None, None] * (1 - wet[..., None]) + base_wet[None, None] * wet[..., None]
        tint = 0.08 * fbm(H, W, 12, 4, 0.5, seed + 3)
        col = col * (1 + tint[..., None]) * (0.9 + 0.1 * grain[..., None])
        # sand grains (light specks)
        specks = (r.random((H, W)) > 0.993) & (thick > 0.15)
        col[specks] = col[specks] * 0.4 + np.array([0.55, 0.50, 0.42]) * 0.6
        rgb = col
        height = thick * 1.0 + 0.12 * grain * (thick > 0.05)
        normal = normal_from_height(ndi.gaussian_filter(height, 0.7), strength=5.0)
        smooth = 0.25 + 0.55 * wet
    elif medium == 'Wet':  # water film from the wet street on a dry polished floor, partly evaporated
        mottle = fbm(H, W, 10, 4, 0.55, seed + 4)
        cover = p + 0.25 * mottle
        film = ndi.gaussian_filter(tread, 0.9) * smoothstep(0.25, 0.6, cover)
        # surface tension: water beads into the tread grooves of the floor print and pulls back at the edges
        film = smoothstep(0.2, 0.55, film + 0.12 * fbm(H, W, 2.5, 3, 0.5, seed + 5)) * smoothstep(0.02, 0.15, film)
        drops = np.zeros((H, W))
        for i in range(int(60 * load)):
            k = r.integers(0, len(poly)); bx, by = poly[k]
            col_ = (bx + 5.0 + r.normal(0, 8)) * PPM + W / 2 - 0.5
            row_ = HEEL_ROW - (by + r.normal(0, 8)) * PPM
            disc_stamp(drops, col_, row_, r.uniform(0.5, 2.0) * PPM, 1.0, soft=0.8)
        thick = np.clip(np.maximum(film, drops), 0, 1)
        grit = (r.random((H, W)) > 0.997) & (thick > 0.3)
        alpha = thick * 0.42
        alpha[grit] = 0.85
        rgb = np.broadcast_to(np.array([0.03, 0.03, 0.032]), (H, W, 3)).copy()
        rgb[grit] = np.array([0.2, 0.17, 0.13])
        height = ndi.gaussian_filter(thick, 1.2) * 0.3
        normal = normal_from_height(height, strength=3.0)
        smooth = 0.25 + 0.67 * thick
    else:  # Dust: dry fine dust / glass powder picked up and deposited
        grain = r.random((H, W))
        mottle = fbm(H, W, 14, 4, 0.55, seed + 4)
        cover = p + 0.22 * mottle
        prob = smoothstep(0.30, 0.85, cover) * tread
        dep = (grain < prob * 0.95).astype(float)
        dep = ndi.gaussian_filter(dep, 0.55) * 1.4
        dep = np.clip(dep, 0, 1) * smoothstep(0.1, 0.5, tread)
        # soft film where the pressure is high
        film = 0.35 * smoothstep(0.55, 1.0, cover) * tread
        thick = np.clip(np.maximum(dep, film), 0, 1)
        # few scattered grains around
        stray = (r.random((H, W)) > 0.9985) & (d_out < 18) & (d_out > 0)
        thick = np.maximum(thick, ndi.gaussian_filter(stray.astype(float), 0.6) * 3.0)
        thick = np.clip(thick, 0, 1)
        alpha = thick * 0.52
        col = np.array([0.74, 0.72, 0.68])[None, None] * (1 + 0.05 * mottle[..., None])
        rgb = np.broadcast_to(col, (H, W, 3)).copy()
        height = ndi.gaussian_filter(thick, 0.8) * 0.25
        normal = normal_from_height(height, strength=2.0)
        smooth = 0.2 + 0 * thick
    return rgb, alpha, normal, smooth, mask


if __name__ == '__main__':
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    jobs = [
        ('Sneaker', 'L', 'Mud', 11, 1.0), ('Sneaker', 'R', 'Mud', 12, 0.92),
        ('Boot', 'L', 'Mud', 21, 1.0), ('Boot', 'R', 'Mud', 22, 0.97),
        ('Sneaker', 'L', 'Dust', 31, 1.0), ('Sneaker', 'R', 'Dust', 32, 0.85),
        ('Boot', 'L', 'Dust', 41, 1.0), ('Boot', 'R', 'Dust', 42, 0.85),
        ('Sneaker', 'L', 'Wet', 51, 1.0), ('Sneaker', 'R', 'Wet', 52, 0.9),
    ]
    sel = sys.argv[2:] if len(sys.argv) > 2 else None
    for kind, foot, med, seed, load in jobs:
        name = f'ShoePrint_{kind}_{med}_{foot}'
        if sel and name not in sel:
            continue
        rgb, a, n, s, mask = make_print(kind, foot, med, seed, load)
        save_rgba(os.path.join(out, name + '.png'), rgb, a)
        save_normal(os.path.join(out, name + '_Normal.png'), n)
        if med == 'Wet':
            save_ms(os.path.join(out, name + '_MetallicSmoothness.png'), s)
        print(name, 'alpha mean %.3f' % a.mean())
