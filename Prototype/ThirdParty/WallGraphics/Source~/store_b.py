# Store notices, part B: framed gemmology diploma, staff notice board and sheets, wall calendar,
# fire evacuation plan. Project-original artwork; all names, numbers and organisations are fictional.
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw
from gfx import *

OUT = sys.argv[1]
CAL_PICTURE = sys.argv[2]   # public-domain painting used for the calendar picture
os.makedirs(OUT, exist_ok=True)
SS = 3


class Canvas:
    """Supersampled RGB drawing surface on top of a float background."""

    def __init__(self, w, h, bg):
        self.w, self.h = w, h
        self.bg = bg.astype(np.float32)
        self.layers = []

    def mask(self, fn):
        im = Image.new('L', (self.w * SS, self.h * SS), 0)
        fn(ImageDraw.Draw(im), SS)
        return mask_from(im.resize((self.w, self.h), Image.LANCZOS))

    def paint(self, color, fn, opacity=1.0):
        self.bg = composite(self.bg, np.array(color, np.float32), self.mask(fn) * opacity)

    def text(self, color, items, opacity=1.0):
        def fn(d, s):
            for it in items:
                x, y, t, fn_, size = it[:5]
                wg = it[5] if len(it) > 5 else None
                tr = it[6] if len(it) > 6 else 0.0
                an = it[7] if len(it) > 7 else 'l'
                draw_text(d, (x * s, y * s), t, font(fn_, size * s, wg), 255, tr, an)
        self.paint(color, fn, opacity)


INK = (0.10, 0.10, 0.12)
BLUE_PEN = (0.10, 0.16, 0.50)
RED_PEN = (0.70, 0.10, 0.10)


# ------------------------------------------------------------------ framed diploma
def guilloche(d, s, x0, y0, x1, y1, color, n=7, amp=7, period=26, width=1):
    for k in range(n):
        ph = k * 2 * math.pi / n
        for (ax, ay, bx, by) in ((x0, y0, x1, y0), (x0, y1, x1, y1)):
            pts = [((ax + t) * s, (ay + amp * math.sin(t / period * 2 * math.pi + ph)) * s) for t in np.arange(0, bx - ax, 1.5)]
            d.line(pts, fill=color, width=width * s)
        for (ax, ay, bx, by) in ((x0, y0, x0, y1), (x1, y0, x1, y1)):
            pts = [((ax + amp * math.sin(t / period * 2 * math.pi + ph)) * s, (ay + t) * s) for t in np.arange(0, by - ay, 1.5)]
            d.line(pts, fill=color, width=width * s)


def seal(c, cx, cy, r):
    y, x = np.mgrid[0:c.h, 0:c.w].astype(np.float32)
    a = np.arctan2(y - cy, x - cx)
    rr = np.sqrt((x - cx) ** 2 + (y - cy) ** 2)
    edge = r * (1 + 0.06 * np.cos(a * 36))
    m = np.clip((edge - rr) / 1.2 + 0.5, 0, 1)
    # ribbons
    rib = c.mask(lambda d, s: (d.polygon([((cx - 10) * s, cy * s), ((cx - 44) * s, (cy + r + 60) * s), ((cx - 22) * s, (cy + r + 48) * s), ((cx - 8) * s, (cy + r + 66) * s), ((cx + 10) * s, (cy + 10) * s)], fill=255),
                               d.polygon([((cx + 10) * s, cy * s), ((cx + 46) * s, (cy + r + 56) * s), ((cx + 26) * s, (cy + r + 46) * s), ((cx + 12) * s, (cy + r + 64) * s), ((cx - 8) * s, (cy + 10) * s)], fill=255)))
    c.bg = composite(c.bg, np.array([0.55, 0.08, 0.10], np.float32), rib)
    gf = gold_fill(c.h, c.w, seed=5, light=1.0)
    sh = emboss(m, 2.0, light=(0.6, 1.0))
    ring = np.clip((r * 0.78 - rr) / 1.0 + 0.5, 0, 1) * (1 - np.clip((r * 0.72 - rr) / 1.0 + 0.5, 0, 1))
    gf2 = np.clip(gf * (1 + sh[..., None] * 6) - ring[..., None] * 0.18, 0, 1)
    c.bg = composite(c.bg, gf2, m)
    c.text((0.45, 0.33, 0.12), [(cx, cy + 8, 'N S G', 'cinzel', r * 0.42, 700, 0.05, 'm')], 0.8)


def diploma():
    W, H = 1024, 768
    # paper
    pw, ph = W, H
    bg = paper(H, W, base=(0.96, 0.94, 0.88), seed=11, fiber=0.018, mottle=0.03)
    c = Canvas(W, H, bg)
    # certificate area inside the mat: x 150..874, y 140..628
    X0, Y0, X1, Y1 = 150, 140, 874, 628
    c.paint((0.32, 0.44, 0.38), lambda d, s: guilloche(d, s, X0 + 18, Y0 + 18, X1 - 18, Y1 - 18, 255, n=6, amp=5, period=22), 0.75)
    c.paint((0.32, 0.44, 0.38), lambda d, s: d.rectangle(((X0 + 32) * s, (Y0 + 32) * s, (X1 - 32) * s, (Y1 - 32) * s), outline=255, width=2 * s), 0.8)
    cx = (X0 + X1) / 2
    c.text((0.18, 0.26, 0.22), [(cx, Y0 + 86, 'NORTHBRIDGE SCHOOL OF GEMMOLOGY', 'cinzel', 25, 700, 0.10, 'm')])
    c.text((0.20, 0.18, 0.15), [(cx, Y0 + 152, 'Diploma in Gemmology', 'pinyon', 54, None, 0.0, 'm')])
    c.text((0.22, 0.20, 0.18), [(cx, Y0 + 196, 'This is to certify that', 'baskerville_i', 16, 400, 0.0, 'm')])
    c.text((0.12, 0.10, 0.10), [(cx, Y0 + 252, 'Eleanor M. Finch', 'vibes', 56, None, 0.0, 'm')])
    lines = ['having satisfied the Board of Examiners in the theory and practice of',
             'Gemmology, is hereby awarded the Diploma of the School with Distinction.',
             'Given under the seal of the School this twelfth day of June, 1998.']
    y = Y0 + 300
    for ln in lines:
        c.text((0.22, 0.20, 0.18), [(cx, y, ln, 'baskerville', 13.5, 400, 0.0, 'm')])
        y += 24
    # signatures
    for sx, name, role, sig in ((X0 + 190, 'H. J. Calloway', 'Principal', 'H.J. Calloway'),
                                (X1 - 190, 'R. M. Osei', 'Chief Examiner', 'R Osei')):
        c.text((0.08, 0.10, 0.30), [(sx, Y1 - 92, sig, 'homemade', 21, None, 0.0, 'm')], 0.92)
        c.paint((0.25, 0.23, 0.2), lambda d, s, sx=sx: d.line([((sx - 95) * s, (Y1 - 80) * s), ((sx + 95) * s, (Y1 - 80) * s)], fill=255, width=s))
        c.text((0.22, 0.20, 0.18), [(sx, Y1 - 60, name, 'baskerville', 12.5, 700, 0.0, 'm'),
                                    (sx, Y1 - 42, role, 'baskerville_i', 12, 400, 0.0, 'm')])
    c.text((0.30, 0.28, 0.25), [(X1 - 44, Y0 + 52, 'No. 04417', 'baskerville', 11, 400, 0.0, 'r')])
    seal(c, cx, Y1 - 92, 40)
    col = c.bg
    # mat board with bevelled window
    y, x = np.mgrid[0:H, 0:W].astype(np.float32)
    inside = ((x >= X0) & (x < X1) & (y >= Y0) & (y < Y1)).astype(np.float32)
    mat = paper(H, W, base=(0.95, 0.95, 0.93), seed=12, fiber=0.01, mottle=0.01)
    bev = (((x >= X0 - 7) & (x < X1 + 7) & (y >= Y0 - 7) & (y < Y1 + 7)).astype(np.float32)) * (1 - inside)
    mat = mat * (1 - bev[..., None] * 0.0) + bev[..., None] * 0.0
    bevcol = np.where(((y < Y0) | (x < X0))[..., None], np.array([0.99, 0.98, 0.96]), np.array([0.84, 0.83, 0.80])).astype(np.float32)
    col = col * inside[..., None] + mat * (1 - inside[..., None])
    col = composite(col, bevcol, bev)
    # shadow of the mat onto the certificate (light from top-left)
    sh = np.clip(1 - inside, 0, 1)
    shadow = ndimage.gaussian_filter(np.roll(np.roll(sh, 4, 0), 4, 1), 3) * inside
    col = col * (1 - 0.25 * shadow[..., None])
    # black satin frame (64 px) with a gold fillet
    F = 64
    fr = ((x < F) | (x >= W - F) | (y < F) | (y >= H - F)).astype(np.float32)
    # moulding profile: rounded, lit from top-left
    dist = np.minimum(np.minimum(x, W - 1 - x), np.minimum(y, H - 1 - y))
    prof = np.sin(np.clip(dist / F, 0, 1) * np.pi)
    side_light = np.where(dist == x, 1.0, np.where(dist == y, 1.15, np.where(dist == W - 1 - x, 0.7, 0.6))).astype(np.float32)
    wood = 0.05 + 0.10 * prof ** 3 * side_light + 0.02 * fbm(H, W, 40, 3, 13)
    frame = np.stack([wood * 1.02, wood, wood * 0.98], -1)
    col = col * (1 - fr[..., None]) + frame * fr[..., None]
    fil = ((dist >= F - 6) & (dist < F)).astype(np.float32)
    col = composite(col, gold_fill(H, W, seed=14, light=0.85), fil)
    # glass: faint diagonal reflection and dust
    glass = 1 - fr
    streak = np.exp(-(((x - y * 0.9) - 520) / 140) ** 2) * 0.05 + np.exp(-(((x - y * 0.9) - 300) / 30) ** 2) * 0.03
    col = col + (streak * glass)[..., None]
    dust = (np.random.default_rng(15).random((H, W)) > 0.9992).astype(np.float32)
    col = np.clip(col + ndimage.gaussian_filter(dust, 0.6)[..., None] * 0.4 * glass[..., None], 0, 1)
    save_jpg(col, os.path.join(OUT, 'Certificate_GemmologyDiplomaFramed.jpg'), 90)


# ------------------------------------------------------------------ A4 sheets
def a4(seed, base=(0.97, 0.97, 0.95)):
    W, H = 724, 1024
    return Canvas(W, H, paper(H, W, base=base, seed=seed, fiber=0.012, mottle=0.02))


def sheet_rota():
    c = a4(21)
    W = c.w
    c.text(INK, [(60, 92, 'STAFF ROTA', 'barlow_b', 40, None, 0.04),
                 (60, 128, 'Week commencing Monday 12 October 2026', 'barlow', 20)])
    c.text(INK, [(W - 60, 92, 'AUREL & FINCH', 'cinzel', 18, 600, 0.16, 'r')])
    days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
    staff = ['Eleanor', 'Marcus', 'Priya', 'Tom', 'Grace (Sat)']
    rows = [['9.00–5.30'] * 5 + ['OFF', 'OFF'],
            ['9.30–5.30', '9.30–5.30', 'OFF', '9.30–5.30', '9.30–5.30', '9.30–5.00', 'OFF'],
            ['OFF', '9.00–5.30', '9.00–5.30', '9.00–5.30', '12.00–5.30', '9.30–5.00', 'OFF'],
            ['12.00–5.30', 'OFF', '9.30–5.30', 'OFF', '9.30–5.30', '10.00–5.00', 'OFF'],
            ['OFF'] * 5 + ['9.30–5.00', 'OFF']]
    x0, y0 = 60, 178
    colw = [118] + [70] * 7
    rowh = 62
    tw = sum(colw)

    def grid(d, s):
        for i in range(len(staff) + 2):
            d.line([(x0 * s, (y0 + i * rowh) * s), ((x0 + tw) * s, (y0 + i * rowh) * s)], fill=255, width=(2 if i in (0, 1) else 1) * s)
        xx = x0
        for w_ in colw + [0]:
            d.line([(xx * s, y0 * s), (xx * s, (y0 + (len(staff) + 1) * rowh) * s)], fill=255, width=s)
            xx += w_
    c.paint(INK, grid)
    c.paint((0.85, 0.85, 0.85), lambda d, s: d.rectangle(((x0 + 1) * s, (y0 + 1) * s, (x0 + tw - 1) * s, (y0 + rowh - 1) * s), fill=255), 0.6)
    items = [(x0 + 10, y0 + 40, 'Name', 'barlow_sb', 19)]
    xx = x0 + colw[0]
    for dname in days:
        items.append((xx + 35, y0 + 40, dname, 'barlow_sb', 19, None, 0, 'm'))
        xx += 70
    for r, (nm, row) in enumerate(zip(staff, rows)):
        yy = y0 + (r + 1) * rowh + 38
        items.append((x0 + 10, yy, nm, 'barlow_m', 18))
        xx = x0 + colw[0]
        for v in row:
            items.append((xx + 35, yy, v, 'robotoc', 13 if v != 'OFF' else 15, 500, 0, 'm'))
            xx += 70
    c.text(INK, items)
    yb = y0 + (len(staff) + 1) * rowh + 60
    notes = ['Opening: two keyholders present at all times (dual control).',
             'Closing: last person out sets the alarm and checks the rear door.',
             'Lunch breaks staggered – never leave the showroom with one person.',
             'Holiday requests to Eleanor by the 25th of the month.']
    for n_ in notes:
        c.text(INK, [(60, yb, '•  ' + n_, 'barlow', 18)])
        yb += 34
    # handwritten edits
    c.paint(RED_PEN, lambda d, s: d.line([((x0 + colw[0] + 4 * 70 + 6) * s, (y0 + 4 * rowh + 30) * s), ((x0 + colw[0] + 5 * 70 - 6) * s, (y0 + 4 * rowh + 30) * s)], fill=255, width=2 * s))
    c.text(RED_PEN, [(x0 + colw[0] + 4 * 70 - 8, y0 + 4 * rowh + 8, 'swap w/ Priya', 'caveat', 24, 600)])
    c.text(BLUE_PEN, [(70, 940, 'Safe engineer coming Thurs – Marcus to sign him in', 'caveat', 30, 500),
                      (70, 980, 'E.F.', 'caveat', 30, 700)])
    return c


def sheet_closing():
    c = a4(31)
    W = c.w
    c.paint((0.08, 0.10, 0.12), lambda d, s: d.rectangle((0, 0, W * s, 150 * s), fill=255))
    c.text((0.96, 0.95, 0.92), [(60, 84, 'CLOSING PROCEDURE', 'barlow_xb', 42, None, 0.04),
                                (60, 122, 'To be completed by the two closing keyholders', 'barlow', 20)])
    steps = [('17.15', 'Turn the door sign to Closed. Last customers by appointment only.'),
             ('17.30', 'Window displays emptied. All window trays carried to the safe room.'),
             ('', 'Every display case emptied into the numbered trays. Nothing left out.'),
             ('', 'Cases locked; keys counted back into the key cabinet (14 keys).'),
             ('17.45', 'Both safes closed under dual control. Spin the dials. Initial the log.'),
             ('', 'Till cashed up, float counted. Card terminal ends the day.'),
             ('', 'Rear service door: bolts top and bottom, bar across.'),
             ('', 'Check the CCTV recorder is recording (red light on).'),
             ('18.00', 'Set the intruder alarm. Leave by the front door within 45 s.'),
             ('', 'Shutters down and locked. Report any fault to Corvane (24 h).')]
    y = 214
    for t, line in steps:
        c.paint(INK, lambda d, s, y=y: d.rectangle((60 * s, (y - 22) * s, 84 * s, (y + 2) * s), outline=255, width=2 * s))
        if t:
            c.text(INK, [(100, y, t, 'barlow_b', 20)])
        for i, ln in enumerate(wrap(line, font('barlow', 20), 520)):
            c.text(INK, [(170, y + i * 26, ln, 'barlow', 20)])
        y += 26 * len(wrap(line, font('barlow', 20), 520)) + 30
    c.text(INK, [(60, y + 30, 'Never write alarm codes or safe combinations anywhere.', 'barlow_b', 21),
                 (60, y + 64, 'If anything looks wrong on arrival: do not enter – call 999 and Eleanor.', 'barlow_sb', 20)])
    # some ticks in pen, a coffee ring
    for k in range(4):
        yy = 214 + k * 82
        c.paint(BLUE_PEN, lambda d, s, yy=yy: d.line([(62 * s, (yy - 10) * s), (71 * s, yy * s), (88 * s, (yy - 28) * s)], fill=255, width=3 * s), 0.85)
    yy_, xx_ = np.mgrid[0:c.h, 0:c.w].astype(np.float32)
    r = np.sqrt((xx_ - 560) ** 2 + (yy_ - 900) ** 2)
    ring = np.exp(-((r - 62) / 4.0) ** 2) * 0.55 + np.clip((62 - r) / 62, 0, 1) * 0.08
    c.bg = composite(c.bg, np.array([0.55, 0.38, 0.22], np.float32), ring * (0.7 + 0.3 * fbm(c.h, c.w, 30, 3, 32)))
    return c


def sheet_fire_action():
    W, H = 512, 724
    c = Canvas(W, H, paper(H, W, base=(0.98, 0.98, 0.97), seed=41, fiber=0.01, mottle=0.015))
    c.paint((0.80, 0.08, 0.08), lambda d, s: d.rectangle((0, 0, W * s, 120 * s), fill=255))
    c.text((1, 1, 1), [(W / 2, 82, 'FIRE ACTION', 'barlow_blk', 56, None, 0.04, 'm')])
    blocks = [('If you discover a fire:', ['Raise the alarm – call point by the front door', 'or in the office.']),
              ('On hearing the alarm:', ['Leave by the front entrance.', 'Do not stop to collect belongings.',
                                         'Do not re-enter the building.']),
              ('Call the fire service:', ['Dial 999']),
              ('Assembly point:', ['Opposite pavement, by the bus stop.'])]
    y = 176
    for head, lines in blocks:
        c.text((0.80, 0.08, 0.08), [(40, y, head, 'barlow_b', 26)])
        y += 36
        for ln in lines:
            c.text(INK, [(40, y, ln, 'barlow_m', 22)])
            y += 30
        y += 22
    c.text(INK, [(40, H - 40, 'Fire marshal: Marcus / Eleanor', 'barlow', 18)])
    return c


def postit(text_lines, color, seed, w=300, h=300, fontsize=40):
    c = Canvas(w, h, paper(h, w, base=color, seed=seed, fiber=0.01, mottle=0.04))
    y = 80
    for ln in text_lines:
        c.text(BLUE_PEN if seed % 2 else (0.1, 0.1, 0.12), [(24, y, ln, 'caveat', fontsize, 600)])
        y += fontsize + 6
    # glued top strip is slightly darker
    c.bg[:50] *= 0.97
    return c


def business_card():
    W, H = 340, 200
    c = Canvas(W, H, paper(H, W, base=(0.98, 0.98, 0.97), seed=51, fiber=0.006, mottle=0.01))
    c.paint((0.10, 0.25, 0.45), lambda d, s: d.rectangle((0, 0, 14 * s, H * s), fill=255))
    c.text((0.10, 0.25, 0.45), [(34, 54, 'QUELLBROOK GLAZING', 'barlow_xb', 25, None, 0.03),
                                (34, 82, 'Shopfronts · Boarding up · Laminated glass', 'barlow', 14)])
    c.text(INK, [(34, 130, '24 h emergency', 'barlow_sb', 18), (34, 160, '07700 900 618', 'barlow_b', 26)])
    return c


def pinned_sheet_rgba(cv, angle, shadow=True):
    """Return RGBA float image of the sheet rotated by angle degrees (expand)."""
    rgb = to_img(cv.bg)
    a = Image.new('L', rgb.size, 255)
    rgba = rgb.convert('RGBA')
    rgba.putalpha(a)
    r = rgba.rotate(angle, resample=Image.BICUBIC, expand=True)
    return np.asarray(r).astype(np.float32) / 255


def cork(H, W, seed):
    rng = np.random.default_rng(seed)
    base = np.array([0.66, 0.48, 0.30], np.float32)
    n1 = ndimage.gaussian_filter(rng.random((H, W)).astype(np.float32), 0.8)
    n2 = ndimage.gaussian_filter(rng.random((H, W)).astype(np.float32), 2.5)
    n3 = fbm(H, W, scale=120, octaves=4, seed=seed)
    v = (n1 - 0.5) * 0.9 + (n2 - 0.5) * 1.6 + (n3 - 0.5) * 0.3
    col = base[None, None] * (1 + 0.55 * v[..., None])
    dark = (n1 < 0.38).astype(np.float32) * 0.25
    col = col * (1 - dark[..., None])
    # old pin holes
    holes = (rng.random((H, W)) > 0.9994).astype(np.float32)
    col = col * (1 - 0.6 * ndimage.gaussian_filter(holes, 0.8)[..., None] * 3)
    return np.clip(col, 0, 1)


def pin(col, x, y, color, r=13):
    H, W = col.shape[:2]
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    sh = np.exp(-(((xx - x - 8) ** 2 + (yy - y - 10) ** 2) / (2 * (r * 0.9) ** 2))) * 0.55
    col = col * (1 - sh[..., None])
    rr = np.sqrt((xx - x) ** 2 + (yy - y) ** 2)
    m = np.clip(r - rr + 0.5, 0, 1)
    shade = np.clip(1.25 - ((xx - x + r * 0.35) ** 2 + (yy - y + r * 0.35) ** 2) / (r * r * 1.2), 0.45, 1.4)
    pc = np.array(color, np.float32)[None, None] * shade[..., None]
    spec = np.exp(-(((xx - x + r * 0.35) ** 2 + (yy - y + r * 0.4) ** 2) / (2 * (r * 0.18) ** 2))) * 0.7
    pc = np.clip(pc + spec[..., None], 0, 1)
    return composite(col, pc, m)


def place(col, rgba, x, y):
    h, w = rgba.shape[:2]
    H, W = col.shape[:2]
    a = rgba[..., 3]
    # drop shadow
    sha = ndimage.gaussian_filter(np.pad(a, 12), 6)
    sx, sy = x + 6 - 12, y + 9 - 12
    ys, xs = slice(max(0, sy), min(H, sy + sha.shape[0])), slice(max(0, sx), min(W, sx + sha.shape[1]))
    sub = sha[ys.start - sy:ys.stop - sy, xs.start - sx:xs.stop - sx]
    col[ys, xs] *= (1 - 0.45 * sub[..., None])
    ys, xs = slice(max(0, y), min(H, y + h)), slice(max(0, x), min(W, x + w))
    sub = rgba[ys.start - y:ys.stop - y, xs.start - x:xs.stop - x]
    col[ys, xs] = composite(col[ys, xs], sub[..., :3], sub[..., 3])
    return col


def memo():
    W, H = 512, 362
    c = Canvas(W, H, paper(H, W, base=(0.97, 0.97, 0.95), seed=55, fiber=0.01, mottle=0.015))
    c.text(INK, [(30, 52, 'MEMO', 'barlow_xb', 34, None, 0.1), (W - 30, 52, '6 Oct', 'barlow', 20, None, 0, 'r')])
    c.paint(INK, lambda d, s: d.line([(30 * s, 68 * s), ((W - 30) * s, 68 * s)], fill=255, width=2 * s))
    y = 110
    for ln in wrap('Please keep personal bags and phones in the staff lockers, not behind the counter. '
                   'The new insurer requires all stock to be in the safes by 17.45 every day - no exceptions, '
                   'including Saturdays.', font('barlow', 21), W - 60):
        c.text(INK, [(30, y, ln, 'barlow', 21)])
        y += 30
    c.text(INK, [(30, y + 26, 'Thank you - Eleanor', 'barlow_i', 21)])
    return c


def sheet_holiday():
    W, H = 362, 512
    c = Canvas(W, H, paper(H, W, base=(0.97, 0.97, 0.95), seed=57, fiber=0.01, mottle=0.015))
    c.text(INK, [(24, 46, 'HOLIDAY PLANNER  Q4', 'barlow_b', 22, None, 0.04)])
    months = ['OCT', 'NOV', 'DEC']
    for i, m in enumerate(months):
        y0 = 80 + i * 140
        c.text(INK, [(24, y0 + 20, m, 'barlow_sb', 18)])
        c.paint((0.6, 0.6, 0.6), lambda d, s, y0=y0: [d.rectangle(((24 + k * 10.5) * s, (y0 + 32) * s, (24 + k * 10.5 + 9) * s, (y0 + 50) * s), outline=255, width=s) for k in range(31)])
    c.paint((0.2, 0.5, 0.9), lambda d, s: d.rectangle(((24 + 19 * 10.5) * s, 112 * s, (24 + 24 * 10.5 + 9) * s, 130 * s), fill=255), 0.6)
    c.paint((0.9, 0.4, 0.2), lambda d, s: d.rectangle(((24 + 9 * 10.5) * s, 252 * s, (24 + 13 * 10.5 + 9) * s, 270 * s), fill=255), 0.6)
    c.paint((0.3, 0.7, 0.3), lambda d, s: d.rectangle(((24 + 21 * 10.5) * s, 392 * s, (24 + 30 * 10.5 + 9) * s, 410 * s), fill=255), 0.6)
    c.text(BLUE_PEN, [(24, 160, 'Tom 20-25', 'caveat', 24, 600), (24, 300, 'Priya 10-14', 'caveat', 24, 600),
                      (24, 440, 'SHOP CLOSED 25-26 + 1 Jan', 'caveat', 22, 600)])
    return c


def notice_board(rota, closing, fire):
    W, H = 1536, 1024
    col = cork(H, W, 61)
    rng = np.random.default_rng(62)
    # sheets scaled to board scale: board 0.9 m wide -> 1536 px; A4 0.21 m -> ~358 px
    def scaled(cv, wpx):
        im = to_img(cv.bg)
        hpx = int(round(im.height * wpx / im.width))
        return Canvas(wpx, hpx, from_img(im.resize((wpx, hpx), Image.LANCZOS)))
    items = [(scaled(rota, 358), -1.6, 70, 70), (scaled(closing, 358), 1.2, 470, 58), (scaled(fire, 250), -2.2, 880, 64),
             (scaled(postit(['Safe engineer', 'THURS 2pm!'], (0.98, 0.90, 0.42), 71, 210, 210, 34), 140), 4.0, 1210, 80),
             (scaled(postit(['Window 3 latch', 'sticking -', 'reported 9/10'], (0.98, 0.70, 0.78), 72, 210, 210, 30), 140), -5.0, 1330, 300),
             (scaled(postit(['order more', 'gift boxes', '(navy, small)'], (0.70, 0.92, 0.72), 73, 210, 210, 30), 140), 3.0, 1180, 470),
             (scaled(business_card(), 160), -3.0, 1250, 700, (132, 64)),   # pin in the blank corner so the name reads
             (scaled(memo(), 300), 1.8, 120, 560),
             (scaled(sheet_holiday(), 250), -1.0, 880, 560)]
    pins = []
    for it in items:
        cv, ang, x, y = it[:4]
        rgba = pinned_sheet_rgba(cv, ang)
        col = place(col, rgba, x, y)
        jx, jy = rng.uniform(-10, 10), rng.uniform(-4, 4)
        pins.append((x + it[4][0], y + it[4][1]) if len(it) > 4 else (x + rgba.shape[1] / 2 + jx, y + 24 + jy))
    pin_cols = [(0.80, 0.10, 0.10), (0.10, 0.30, 0.75), (0.95, 0.75, 0.10), (0.10, 0.55, 0.25), (0.85, 0.85, 0.85)]
    for i, (px, py) in enumerate(pins):
        col = pin(col, px, py, pin_cols[i % len(pin_cols)])
    # spare pins stuck in the cork
    for k in range(5):
        col = pin(col, rng.uniform(80, 1450), rng.uniform(880, 960), pin_cols[k % 5], 11)
    # aluminium frame
    F = 34
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    dist = np.minimum(np.minimum(xx, W - 1 - xx), np.minimum(yy, H - 1 - yy))
    fr = (dist < F).astype(np.float32)
    prof = 0.62 + 0.25 * np.cos(dist / F * np.pi) * np.where(dist == yy, 1, np.where(dist == xx, 0.7, -0.4))
    al = np.stack([prof, prof * 1.01, prof * 1.03], -1) * (1 + 0.04 * (fbm(H, W, 8, 2, 63)[..., None] - 0.5))
    col = col * (1 - fr[..., None]) + np.clip(al, 0, 1) * fr[..., None]
    inner_sh = np.clip(1 - (dist - F) / 10, 0, 1) * (dist >= F)
    col = col * (1 - 0.35 * inner_sh[..., None])
    col = grain(col, 0.006, 64)
    save_jpg(col, os.path.join(OUT, 'NoticeBoard_StaffCork.jpg'), 88)


# ------------------------------------------------------------------ wall calendar
def calendar():
    W, H = 1024, 1448
    c = Canvas(W, H, paper(H, W, base=(0.98, 0.98, 0.97), seed=81, fiber=0.006, mottle=0.012))
    # picture (top), from a public-domain painting
    pic = Image.open(CAL_PICTURE).convert('RGB')
    PX0, PY0, PX1, PY1 = 60, 92, W - 60, 660
    tw, th = PX1 - PX0, PY1 - PY0
    s = max(tw / pic.width, th / pic.height)
    pic = pic.resize((int(pic.width * s) + 1, int(pic.height * s) + 1), Image.LANCZOS)
    ox, oy = (pic.width - tw) // 2, (pic.height - th) // 2
    pic = from_img(pic.crop((ox, oy, ox + tw, oy + th)))
    c.bg[PY0:PY1, PX0:PX1] = pic * 0.97 + 0.01
    c.text((0.45, 0.45, 0.45), [(PX1, PY1 + 26, 'Claude Monet, Poppy Field (detail)', 'cormorant_i', 18, 500, 0.0, 'r')])
    c.text(INK, [(PX0, 768, 'OCTOBER', 'barlow_l', 76, None, 0.10), (PX1, 768, '2026', 'barlow_l', 76, None, 0.04, 'r')])
    days = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN']
    gx0, gy0 = PX0, 800
    cw = (PX1 - PX0) / 7
    rh = 112
    c.paint((0.75, 0.75, 0.75), lambda d, s: [d.line([(gx0 * s, (gy0 + 40 + i * rh) * s), (PX1 * s, (gy0 + 40 + i * rh) * s)], fill=255, width=s) for i in range(6)])
    c.paint((0.75, 0.75, 0.75), lambda d, s: [d.line([((gx0 + i * cw) * s, (gy0 + 40) * s), ((gx0 + i * cw) * s, (gy0 + 40 + 5 * rh) * s)], fill=255, width=s) for i in range(8)])
    for i, dn in enumerate(days):
        c.text((0.70, 0.12, 0.12) if i == 6 else INK, [(gx0 + i * cw + 8, gy0 + 28, dn, 'barlow_sb', 18, None, 0.12)])
    import calendar as calmod
    first, nd = calmod.monthrange(2026, 10)
    pos = {}
    for dday in range(1, nd + 1):
        idx = first + dday - 1
        col_, row = idx % 7, idx // 7
        x = gx0 + col_ * cw + cw - 10
        y = gy0 + 40 + row * rh + 34
        pos[dday] = (gx0 + col_ * cw, gy0 + 40 + row * rh)
        c.text((0.70, 0.12, 0.12) if col_ == 6 else INK, [(x, y, str(dday), 'barlow_m', 28, None, 0.0, 'r')])
    x, y = pos[25]
    c.text((0.45, 0.45, 0.45), [(x + 6, y + 96, 'British Summer', 'barlow', 11), (x + 6, y + 108, 'Time ends', 'barlow', 11)])
    notes = {2: ('Insurer', '9.30'), 9: ('Stocktake', ''), 15: ('Safe service', '2pm'), 16: ('Window', 'change'),
             21: ('Valuation', 'day'), 25: ('Clocks back!', ''), 28: ('Engraver', 'collects')}
    for dd, (a, b) in notes.items():
        x, y = pos[dd]
        c.text(BLUE_PEN, [(x + 8, y + 62, a, 'caveat', 26, 600)])
        if b:
            c.text(BLUE_PEN, [(x + 8, y + 88, b, 'caveat', 26, 600)])
    # days crossed off up to the 13th
    for dd in range(1, 14):
        x, y = pos[dd]
        c.paint(RED_PEN, lambda d, s, x=x, y=y: (d.line([((x + 12) * s, (y + 14) * s), ((x + cw - 14) * s, (y + rh - 12) * s)], fill=255, width=2 * s),
                                                  d.line([((x + cw - 16) * s, (y + 12) * s), ((x + 14) * s, (y + rh - 16) * s)], fill=255, width=2 * s)), 0.55)
    c.text((0.40, 0.40, 0.40), [(W / 2, H - 34, 'With the compliments of Marrowby Bullion & Findings  ·  Trade counter 0161 496 0702', 'barlow', 15, None, 0.02, 'm')])
    col = c.bg
    # wire-o binding and hanger
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    for k in range(28):
        hx = 120 + k * 28.5
        hole = smooth(np.sqrt((xx - hx) ** 2 + (yy - 40) ** 2), 6)
        col = composite(col, np.array([0.25, 0.25, 0.27], np.float32), hole)
        wire = np.exp(-((np.abs(xx - hx - 4) - 4) / 1.6) ** 2) * (yy < 40) * (yy > 10)
        col = composite(col, np.array([0.75, 0.76, 0.78], np.float32), np.clip(wire * 1.2, 0, 1))
    alpha = np.ones((H, W), np.float32)
    hang = (((xx - W / 2) / 70) ** 2 + ((yy - 18) / 26) ** 2 < 1) & (yy < 30)
    alpha[hang] = 0
    # slight paper wave shading
    wave = (fbm(H, W, 300, 2, 83) - 0.5) * 0.05
    col = np.clip(col * (1 + wave[..., None]), 0, 1)
    save_png(col, os.path.join(OUT, 'Calendar_October2026.png'), alpha)


def smooth(r, rad):
    return np.clip((rad - r) + 0.5, 0, 1)


# ------------------------------------------------------------------ fire evacuation plan
def evac_plan():
    W, H = 2048, 1448
    c = Canvas(W, H, paper(H, W, base=(0.985, 0.985, 0.98), seed=91, fiber=0.004, mottle=0.008))
    GREEN = (0.0, 0.52, 0.30)
    RED = (0.80, 0.08, 0.08)
    c.paint(GREEN, lambda d, s: d.rectangle((0, 0, W * s, 150 * s), fill=255))
    c.text((1, 1, 1), [(60, 100, 'FIRE EVACUATION PLAN', 'barlow_blk', 78, None, 0.03),
                       (W - 60, 70, 'Aurel & Finch  ·  Ground floor', 'barlow_sb', 34, None, 0.0, 'r'),
                       (W - 60, 116, 'Shop, office and rear rooms', 'barlow', 28, None, 0.0, 'r')])
    # plan transform: store x -8..8 -> px, z 0..12 (front at the bottom)
    S = 84.0
    OX, OY = 110 + 8 * S, 200 + 12 * S  # x=0 at OX, z=0 at OY (bottom)

    def P(x, z):
        return (OX + x * S, OY - z * S)

    def L(d, s, pts, w):
        d.line([(P(*p)[0] * s, P(*p)[1] * s) for p in pts], fill=255, width=int(w * s), joint='curve')

    def R(d, s, x0, z0, x1, z1, fill=None, outline=255, w=2):
        a, b = P(x0, z1), P(x1, z0)
        d.rectangle((a[0] * s, a[1] * s, b[0] * s, b[1] * s), fill=fill, outline=outline, width=int(w * s))

    # furniture (light grey)
    def furniture(d, s):
        for (x, z, rot) in [(-1.9, 5.2, 0), (0, 5.2, 0), (1.9, 5.2, 0), (-4.6, 4.0, 90), (-4.6, 5.8, 90), (-5.6, 2.3, 98),
                            (4.6, 4.2, 90), (4.6, 6.0, 90)]:
            hw, hd = (0.8, 0.4) if rot == 0 else (0.4, 0.8)
            R(d, s, x - hw, z - hd, x + hw, z + hd, fill=None, w=2)
        for z in (2.4, 4.4, 6.4):
            R(d, s, -8, z - 0.8, -7.4, z + 0.8, w=2)
        for z in (4.2, 6.2):
            R(d, s, 7.4, z - 0.8, 8, z + 0.8, w=2)
        R(d, s, -6.6, 7.6, -4.4, 8.3, w=2)          # rear wall case
        R(d, s, 4.3, 7.4, 6.7, 8.2, w=2)            # counter
        R(d, s, 4.6, 0.7, 6.6, 1.5, w=2)            # sofa
        R(d, s, 6.7, 0.8, 7.3, 1.4, w=2)            # side table
        R(d, s, 0.8, 10.9, 1.8, 11.8, w=2)          # safes
        R(d, s, -1.4, 11.0, -0.6, 11.8, w=2)
        R(d, s, -2.4, 9.0, -1.0, 9.8, w=2)          # desk
        R(d, s, -2.95, 9.8, -2.5, 11.0, w=2)        # shelving
    c.paint((0.62, 0.62, 0.62), furniture)

    def walls(d, s):
        # outer shell with the glazed front
        L(d, s, [(-8, 0), (-8, 12), (8, 12), (8, 0)], 12)
        # rear partition with openings: side-room doors at x=-3.9 and 3.9 (0.9 m), office opening |x|<0.8
        L(d, s, [(-8, 8.5), (-4.35, 8.5)], 6)
        L(d, s, [(-3.45, 8.5), (-0.8, 8.5)], 6)
        L(d, s, [(0.8, 8.5), (3.45, 8.5)], 6)
        L(d, s, [(4.35, 8.5), (8, 8.5)], 6)
        L(d, s, [(-3, 8.5), (-3, 12)], 6)
        L(d, s, [(3, 8.5), (3, 12)], 6)
        # facade: piers between glazed bays
        for x in (-8, -6, -4, -2, 2, 4, 6, 8):
            L(d, s, [(x - 0.12, 0), (x + 0.12, 0)], 14)
    c.paint(INK, walls)

    def glazing(d, s):
        for x0 in (-8, -6, -4, 2, 4, 6):
            L(d, s, [(x0 + 0.15, -0.05), (x0 + 1.85, -0.05)], 2)
            L(d, s, [(x0 + 0.15, 0.07), (x0 + 1.85, 0.07)], 2)
        # entrance doors (double leaf swing arcs)
        for sx in (-1, 1):
            hinge = P(sx * 1.0, 0)
            r = 1.0 * S
            a0, a1 = (270, 360) if sx < 0 else (180, 270)
            d.arc(((hinge[0] - r) * s, (hinge[1] - r) * s, (hinge[0] + r) * s, (hinge[1] + r) * s), a0, a1, fill=255, width=2 * s)
            L(d, s, [(sx * 1.0, 0), (sx * 1.0, 1.0)], 3)
        for (x, sgn) in ((-3.9, 1), (3.9, -1)):
            hinge = P(x - 0.45 * sgn, 8.5)
            r = 0.9 * S
            d.arc(((hinge[0] - r) * s, (hinge[1] - r) * s, (hinge[0] + r) * s, (hinge[1] + r) * s), 0 if sgn > 0 else 90, 90 if sgn > 0 else 180, fill=255, width=2 * s)
    c.paint(INK, glazing)
    # room labels
    c.text((0.35, 0.35, 0.35), [(OX, OY - 3.75 * S, 'SHOWROOM', 'barlow_sb', 40, None, 0.2, 'm'),
                                (OX, OY - 10.85 * S, 'OFFICE', 'barlow_sb', 30, None, 0.2, 'm'),
                                (OX - 5.5 * S, OY - 10.6 * S, 'STOCK ROOM', 'barlow_sb', 28, None, 0.15, 'm'),
                                (OX + 5.5 * S, OY - 10.6 * S, 'WORKSHOP', 'barlow_sb', 28, None, 0.15, 'm'),
                                (OX, OY + 62, 'PAVEMENT  –  HIGH STREET', 'barlow_m', 26, None, 0.25, 'm')])

    # escape routes: green arrows
    def arrow_path(d, s, pts, w=10):
        L(d, s, pts, w)
        (x1, z1), (x2, z2) = pts[-2], pts[-1]
        ax, ay = P(x2, z2)
        bx, by = P(x1, z1)
        ang = math.atan2(ay - by, ax - bx)
        hl = 34
        d.polygon([((ax + math.cos(ang) * 6) * s, (ay + math.sin(ang) * 6) * s),
                   ((ax - math.cos(ang - 0.5) * hl) * s, (ay - math.sin(ang - 0.5) * hl) * s),
                   ((ax - math.cos(ang + 0.5) * hl) * s, (ay - math.sin(ang + 0.5) * hl) * s)], fill=255)
    c.paint(GREEN, lambda d, s: (arrow_path(d, s, [(0, 9.9), (0, 7.2), (-3.35, 6.5), (-3.35, 3.2), (-0.5, 1.6), (-0.5, 1.05)]),
                                 arrow_path(d, s, [(-5.2, 9.7), (-3.9, 9.2), (-3.9, 7.7), (-3.55, 7.0)]),
                                 arrow_path(d, s, [(5.2, 9.7), (3.9, 9.2), (3.9, 7.6), (3.35, 6.6), (3.35, 3.2), (0.5, 1.6), (0.5, 1.05)]),
                                 arrow_path(d, s, [(-6.7, 7.4), (-6.7, 3.6), (-3.6, 2.6)], 8),
                                 arrow_path(d, s, [(6.7, 3.0), (4.6, 2.4), (3.6, 2.4)], 8)))
    # symbols
    def symbol_ext(x, z):
        cx, cy = P(x, z)
        c.paint(RED, lambda d, s: d.rectangle(((cx - 22) * s, (cy - 22) * s, (cx + 22) * s, (cy + 22) * s), fill=255))
        c.paint((1, 1, 1), lambda d, s: (d.rounded_rectangle(((cx - 6) * s, (cy - 12) * s, (cx + 6) * s, (cy + 16) * s), 4 * s, fill=255),
                                         d.line([((cx) * s, (cy - 12) * s), ((cx + 4) * s, (cy - 17) * s), ((cx + 13) * s, (cy - 14) * s)], fill=255, width=2 * s)))

    def symbol_cp(x, z):
        cx, cy = P(x, z)
        c.paint(RED, lambda d, s: d.rectangle(((cx - 18) * s, (cy - 18) * s, (cx + 18) * s, (cy + 18) * s), fill=255))
        c.paint((1, 1, 1), lambda d, s: d.rectangle(((cx - 9) * s, (cy - 9) * s, (cx + 9) * s, (cy + 9) * s), fill=255))
        c.paint(RED, lambda d, s: d.ellipse(((cx - 4) * s, (cy - 4) * s, (cx + 4) * s, (cy + 4) * s), fill=255))

    def symbol_fa(x, z):
        cx, cy = P(x, z)
        c.paint(GREEN, lambda d, s: d.rectangle(((cx - 20) * s, (cy - 20) * s, (cx + 20) * s, (cy + 20) * s), fill=255))
        c.paint((1, 1, 1), lambda d, s: (d.rectangle(((cx - 4) * s, (cy - 13) * s, (cx + 4) * s, (cy + 13) * s), fill=255),
                                         d.rectangle(((cx - 13) * s, (cy - 4) * s, (cx + 13) * s, (cy + 4) * s), fill=255)))

    def symbol_exit(x, z):
        cx, cy = P(x, z)
        c.paint(GREEN, lambda d, s: d.rectangle(((cx - 46) * s, (cy - 20) * s, (cx + 46) * s, (cy + 20) * s), fill=255))
        c.text((1, 1, 1), [(cx, cy + 10, 'EXIT', 'barlow_xb', 28, None, 0.1, 'm')])
    symbol_ext(1.8, 0.45); symbol_ext(2.7, 8.85); symbol_ext(-7.6, 8.1)
    symbol_cp(-2.4, 0.45); symbol_cp(-2.6, 8.85)
    symbol_fa(1.9, 9.6)
    symbol_exit(0, 0.45)
    # you are here
    yx, yz = 1.8, 8.0
    cx, cy = P(yx, yz)
    c.paint(RED, lambda d, s: d.ellipse(((cx - 20) * s, (cy - 20) * s, (cx + 20) * s, (cy + 20) * s), fill=255))
    c.paint((1, 1, 1), lambda d, s: d.ellipse(((cx - 8) * s, (cy - 8) * s, (cx + 8) * s, (cy + 8) * s), fill=255))
    c.text(RED, [(cx, cy + 58, 'YOU ARE HERE', 'barlow_xb', 26, None, 0.04, 'm')])
    # legend
    LX = 1580
    c.text(INK, [(LX, 250, 'KEY', 'barlow_xb', 40, None, 0.1)])
    entries = [('route', 'Escape route'), ('exit', 'Fire exit'), ('ext', 'Fire extinguisher'), ('cp', 'Fire alarm call point'),
               ('fa', 'First aid kit'), ('yah', 'You are here')]
    y = 330
    for kind, label in entries:
        sx, sy = LX + 40, y - 12
        if kind == 'route':
            c.paint(GREEN, lambda d, s: (d.line([((sx - 34) * s, sy * s), ((sx + 20) * s, sy * s)], fill=255, width=10 * s),
                                         d.polygon([((sx + 40) * s, sy * s), ((sx + 14) * s, (sy - 16) * s), ((sx + 14) * s, (sy + 16) * s)], fill=255)))
        elif kind == 'exit':
            c.paint(GREEN, lambda d, s: d.rectangle(((sx - 40) * s, (sy - 18) * s, (sx + 40) * s, (sy + 18) * s), fill=255))
            c.text((1, 1, 1), [(sx, sy + 9, 'EXIT', 'barlow_xb', 24, None, 0.1, 'm')])
        else:
            # reuse plan symbols by placing them in legend coordinates
            px = (sx - OX) / S
            pz = (OY - sy) / S
            {'ext': symbol_ext, 'cp': symbol_cp, 'fa': symbol_fa}.get(kind, lambda a, b: None)(px, pz)
            if kind == 'yah':
                c.paint(RED, lambda d, s: d.ellipse(((sx - 20) * s, (sy - 20) * s, (sx + 20) * s, (sy + 20) * s), fill=255))
                c.paint((1, 1, 1), lambda d, s: d.ellipse(((sx - 8) * s, (sy - 8) * s, (sx + 8) * s, (sy + 8) * s), fill=255))
        c.text(INK, [(LX + 110, y, label, 'barlow_m', 30)])
        y += 82
    y += 30
    c.text(RED, [(LX, y, 'IN CASE OF FIRE', 'barlow_xb', 34, None, 0.05)])
    y += 52
    for ln in ['Raise the alarm.', 'Leave by the front entrance.', 'Close doors behind you.', 'Do not collect belongings.',
               'Call 999.', 'Assemble on the opposite', 'pavement by the bus stop.', 'Do not re-enter until told', 'it is safe.']:
        c.text(INK, [(LX, y, ln, 'barlow', 28)])
        y += 38
    c.text((0.4, 0.4, 0.4), [(60, H - 36, 'Plan not to scale.  Prepared for Aurel & Finch Ltd., reviewed March 2026.  Keep this plan clear and visible.', 'barlow', 22)])
    c.paint((0.2, 0.2, 0.2), lambda d, s: d.line([((LX - 40) * s, 200 * s), ((LX - 40) * s, (H - 90) * s)], fill=255, width=2 * s), 0.5)
    # north arrow-free orientation hint
    col = c.bg
    # laminated: very soft sheen and slight edge yellowing
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    sheen = np.exp(-(((xx * 0.7 + yy) - 1500) / 380) ** 2) * 0.035
    col = np.clip(col + sheen[..., None], 0, 1)
    col = composite(col, np.array([0.85, 0.80, 0.62], np.float32), grime(H, W, seed=92, amount=0.06, edge=0.18) * 0.5)
    save_jpg(col, os.path.join(OUT, 'FireEvacuationPlan.jpg'), 88)


diploma(); print('diploma', flush=True)
rota = sheet_rota(); save_jpg(rota.bg, os.path.join(OUT, 'Sheet_StaffRota.jpg'), 88)
closing = sheet_closing(); save_jpg(closing.bg, os.path.join(OUT, 'Sheet_ClosingProcedure.jpg'), 88)
fire = sheet_fire_action()
print('sheets', flush=True)
notice_board(rota, closing, fire); print('board', flush=True)
calendar(); print('calendar', flush=True)
evac_plan(); print('evac', flush=True)
print('FONTS', sorted(USED_FONTS))
