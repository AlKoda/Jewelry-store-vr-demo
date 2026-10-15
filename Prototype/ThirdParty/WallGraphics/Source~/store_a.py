# Store notices, part A: hours plate, CCTV sign, ring-bell plaque, stickers, back-in-5 sign.
# Project-original artwork; every name, number and company is fictional.
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw
from gfx import *

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)


def scratches(h, w, n=120, seed=0, length=(10, 80), width=1, ss=2):
    rng = np.random.default_rng(seed)
    im = Image.new('L', (w * ss, h * ss), 0)
    d = ImageDraw.Draw(im)
    for _ in range(n):
        x, y = rng.random() * w * ss, rng.random() * h * ss
        a = rng.normal(0.2, 0.9)
        L = rng.uniform(*length) * ss
        pts = []
        for t in np.linspace(0, 1, 6):
            pts.append((x + math.cos(a) * L * t + rng.normal(0, 0.6 * ss), y + math.sin(a) * L * t + rng.normal(0, 0.6 * ss)))
        d.line(pts, fill=int(rng.uniform(60, 200)), width=max(1, int(width * ss * rng.uniform(0.5, 1.2))))
    return mask_from(im.resize((w, h), Image.LANCZOS))


def ss_mask(w, h, fn, ss=3):
    im = Image.new('L', (w * ss, h * ss), 0)
    fn(ImageDraw.Draw(im), ss)
    return mask_from(im.resize((w, h), Image.LANCZOS))


def text_ss(w, h, items, ss=3):
    """items: list of (x, y, text, fontname, size, wght, tracking, anchor)"""
    def fn(d, s):
        for (x, y, t, fn_, size, wg, tr, an) in items:
            draw_text(d, (x * s, y * s), t, font(fn_, size * s, wg), 255, tr, an)
    return ss_mask(w, h, fn, ss)


# ------------------------------------------------------------------ brass opening-hours plate
def hours_plate():
    W, H = 768, 1024
    rng = np.random.default_rng(1)
    shape = rounded_rect_mask(W, H, 26)
    base = np.array([0.80, 0.64, 0.36], np.float32)
    streak = ndimage.gaussian_filter(rng.normal(0, 1, (H, W)).astype(np.float32), (0.4, 40)) * 2.2
    streak2 = ndimage.gaussian_filter(rng.normal(0, 1, (H, W)).astype(np.float32), (0.3, 6)) * 0.9
    tone = fbm(H, W, scale=300, octaves=4, seed=2) - 0.5
    col = base[None, None] * (1 + 0.06 * streak[..., None] + 0.05 * streak2[..., None] + 0.10 * tone[..., None])
    # tarnish: darker, browner towards the edges and in blotches
    tarn = np.clip(grime(H, W, seed=3, amount=0.35, edge=0.5), 0, 1)
    col = composite(col, np.array([0.42, 0.30, 0.16], np.float32), tarn * 0.55)
    # finger smudges near the lower right (where hands push the door)
    sm = np.zeros((H, W), np.float32)
    for k in range(5):
        cx, cy = rng.uniform(0.55, 0.9) * W, rng.uniform(0.70, 0.95) * H
        y, x = np.mgrid[0:H, 0:W]
        sm += np.exp(-(((x - cx) / 38) ** 2 + ((y - cy) / 50) ** 2)) * rng.uniform(0.3, 0.6)
    col = col * (1 - 0.10 * np.clip(sm, 0, 1)[..., None])
    # bevelled edge highlight
    inner = rounded_rect_mask(W - 28, H - 28, 16)
    inner = np.pad(inner, ((14, 14), (14, 14)))
    bevel = np.clip(shape - inner, 0, 1)
    edge_shade = emboss(shape, 5.0, light=(0.45, 1.0))
    col = col * (1 + np.clip(edge_shade * 9.0, -0.35, 0.35)[..., None] * bevel[..., None] + bevel[..., None] * 0.06)
    # engraved lettering, filled with black wax
    items = [
        (W / 2, 160, 'AUREL & FINCH', 'cinzel', 56, 600, 0.12, 'm'),
        (W / 2, 214, 'FINE JEWELLERY  ·  EST. 1962', 'cinzel', 22, 500, 0.25, 'm'),
        (W / 2, 330, 'Opening Hours', 'cormorant_i', 60, 600, 0.0, 'm'),
    ]
    rows = [('Monday – Friday', '9.30 – 17.30'), ('Saturday', '10.00 – 17.00'), ('Sunday', 'Closed'),
            ('Bank Holidays', 'Closed')]
    yy = 440
    for a, b in rows:
        items.append((118, yy, a, 'cormorant', 40, 600, 0.0, 'l'))
        items.append((W - 118, yy, b, 'cormorant', 40, 600, 0.0, 'r'))
        yy += 72
    items += [
        (W / 2, 780, 'Viewing outside these hours by appointment', 'cormorant_i', 31, 500, 0.0, 'm'),
        (W / 2, 840, 'Please ring for admittance', 'cormorant_i', 31, 500, 0.0, 'm'),
    ]
    engr = text_ss(W, H, items)

    def rules(d, s):
        d.line([(W / 2 - 60) * s, 258 * s, (W / 2 + 60) * s, 258 * s], fill=255, width=2 * s)
        d.line([118 * s, 380 * s, (W - 118) * s, 380 * s], fill=255, width=1 * s)
        d.line([118 * s, 700 * s, (W - 118) * s, 700 * s], fill=255, width=1 * s)
    engr = np.maximum(engr, ss_mask(W, H, rules))
    # engraving: lit lower lip, shadowed upper lip, black fill slightly worn
    sh = emboss(engr, 1.0, light=(0.4, 1.0))
    col = col * (1 + np.clip(sh, -0.5, 0.5)[..., None] * 1.4)
    wear = fbm(H, W, scale=60, octaves=3, seed=4)
    fill = 0.92 - 0.25 * np.clip((wear - 0.6) * 3, 0, 1)
    col = composite(col, np.array([0.06, 0.05, 0.04], np.float32), engr * fill)
    # screws (dome heads, slotted)
    for (sx, sy) in ((54, 54), (W - 54, 54), (54, H - 54), (W - 54, H - 54)):
        y, x = np.mgrid[0:H, 0:W].astype(np.float32)
        r = np.sqrt((x - sx) ** 2 + (y - sy) ** 2)
        head = smooth_disc(r, 17)
        shade = np.clip(1.15 - ((x - sx + 6) ** 2 + (y - sy + 6) ** 2) / 600, 0.45, 1.25)
        hc = np.array([0.74, 0.60, 0.36], np.float32)[None, None] * shade[..., None]
        col = composite(col, col * 0.55, smooth_disc(r, 20) * (1 - head))
        col = composite(col, hc, head)
        ang = rng.uniform(0, np.pi)
        u = (x - sx) * math.cos(ang) + (y - sy) * math.sin(ang)
        v = -(x - sx) * math.sin(ang) + (y - sy) * math.cos(ang)
        slot = (np.abs(v) < 2.2) & (np.abs(u) < 14)
        col = composite(col, np.array([0.12, 0.09, 0.05], np.float32), slot.astype(np.float32) * head)
    col = np.clip(col + scratches(H, W, 160, seed=5, length=(8, 50))[..., None] * 0.10, 0, 1)
    save_png(col, os.path.join(OUT, 'Plate_OpeningHours.png'), shape)


def smooth_disc(r, rad, soft=1.0):
    return np.clip((rad - r) / soft + 0.5, 0, 1)


# ------------------------------------------------------------------ CCTV sign
def camera_pictogram(d, s, cx, cy, sc, fill):
    """Original CCTV camera pictogram: wall plate, arm, housing with sun hood and lens."""
    def P(x, y):
        return ((cx + x * sc) * s, (cy + y * sc) * s)
    ang = math.radians(16)
    ca, sa = math.cos(ang), math.sin(ang)

    def R(x, y):
        x, y = x + 10, y - 20
        return P(x * ca - y * sa, x * sa + y * ca)
    # wall plate and arm
    d.rounded_rectangle([P(-178, -70), P(-152, 70)], 4 * s * sc, fill=fill)
    d.polygon([P(-152, -6), P(-60, -6), P(-40, 18), P(-40, 34), P(-152, 14)], fill=fill)
    # swivel joint
    d.ellipse([P(-62, -6), P(-26, 30)], fill=fill)
    # housing
    d.polygon([R(-110, -44), R(90, -44), R(104, -30), R(104, 30), R(-110, 30)], fill=fill)
    # sun hood (overhangs the front)
    d.polygon([R(-126, -66), R(126, -66), R(146, -52), R(-126, -52)], fill=fill)
    # lens ring
    d.polygon([R(104, -20), R(128, -26), R(128, 26), R(104, 20)], fill=fill)


def cctv_sign():
    W, H = 768, 1024
    s = 3
    yellow = np.array([0.98, 0.80, 0.08], np.float32)
    black = np.array([0.05, 0.05, 0.05], np.float32)
    shape = rounded_rect_mask(W, H, 18)
    col = np.zeros((H, W, 3), np.float32) + yellow
    # printed border
    def border(d, s):
        d.rounded_rectangle((22 * s, 22 * s, (W - 22) * s, (H - 22) * s), 16 * s, outline=255, width=12 * s)
    m = ss_mask(W, H, border)
    col = composite(col, black, m)
    pict = ss_mask(W, H, lambda d, s: camera_pictogram(d, s, W / 2 + 20, 280, 1.45, 255))
    col = composite(col, black, pict)
    items = [(W / 2, 600, 'CCTV', 'barlow_blk', 150, None, 0.02, 'm'),
             (W / 2, 668, 'IN OPERATION', 'barlow_xb', 52, None, 0.04, 'm')]
    body = ['Images are being monitored and recorded', 'for the purposes of crime prevention', 'and public safety.']
    y = 744
    for line in body:
        items.append((W / 2, y, line, 'barlow_m', 30, None, 0.0, 'm'))
        y += 38
    items += [(W / 2, 880, 'This scheme is controlled by Aurel & Finch Ltd.', 'barlow_sb', 26, None, 0.0, 'm'),
              (W / 2, 922, 'For more information call 020 7946 0321', 'barlow_sb', 26, None, 0.0, 'm')]
    col = composite(col, black, text_ss(W, H, items))
    # weathering: sun fade towards the top, grime, scratches, screw holes
    y = np.linspace(0, 1, H, dtype=np.float32)[:, None, None]
    fade = (1 - y) * 0.12 + 0.04 * fbm(H, W, 200, 4, 7)[..., None]
    col = col + (np.array([0.96, 0.95, 0.88], np.float32) - col) * fade
    g = grime(H, W, seed=8, amount=0.28, edge=0.35)
    col = composite(col, np.array([0.30, 0.26, 0.18], np.float32), g * 0.45)
    col = np.clip(col + scratches(H, W, 90, seed=9, length=(10, 70))[..., None] * 0.18, 0, 1)
    for (sx, sy) in ((46, 46), (W - 46, 46), (46, H - 46), (W - 46, H - 46)):
        yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
        r = np.sqrt((xx - sx) ** 2 + (yy - sy) ** 2)
        col = composite(col, np.array([0.62, 0.62, 0.64], np.float32), smooth_disc(r, 9))
        col = composite(col, np.array([0.2, 0.2, 0.2], np.float32), smooth_disc(r, 3))
        col = composite(col, np.array([0.35, 0.28, 0.12], np.float32), np.clip(np.exp(-((xx - sx) ** 2 + (yy - sy - 30) ** 2 / 3) / 60) * 0.3, 0, 1) * (yy > sy))
    col = grain(col, 0.008, 10)
    save_png(col, os.path.join(OUT, 'Sign_CCTV.png'), shape)


# ------------------------------------------------------------------ ring-the-bell plaque (black acrylic)
def bell_icon(d, s, cx, cy, sc, fill):
    pts = []
    for t in np.linspace(0, np.pi, 40):
        pass
    # bell silhouette from a profile curve
    prof = [(0, -60), (10, -58), (26, -48), (34, -28), (36, 0), (40, 22), (52, 38), (56, 46), (-56, 46), (-52, 38), (-40, 22),
            (-36, 0), (-34, -28), (-26, -48), (-10, -58)]
    d.polygon([((cx + x * sc) * s, (cy + y * sc) * s) for x, y in prof], fill=fill)
    d.ellipse(((cx - 12 * sc) * s, (cy + 46 * sc) * s, (cx + 12 * sc) * s, (cy + 66 * sc) * s), fill=fill)
    d.ellipse(((cx - 7 * sc) * s, (cy - 72 * sc) * s, (cx + 7 * sc) * s, (cy - 58 * sc) * s), fill=fill)


def ring_bell():
    W, H = 1024, 512
    shape = rounded_rect_mask(W, H, 22)
    y = np.linspace(0, 1, H, dtype=np.float32)[:, None, None]
    col = np.zeros((H, W, 3), np.float32) + np.array([0.025, 0.024, 0.026], np.float32)
    col = col * (1 + 0.6 * (1 - y))  # glossy acrylic: faint top-lit gradient
    # polished edge
    inner = np.pad(rounded_rect_mask(W - 12, H - 12, 16), 6)
    col = col + (shape - inner)[..., None] * 0.18
    gold = np.array([0.80, 0.66, 0.40], np.float32)
    icon = ss_mask(W, H, lambda d, s: bell_icon(d, s, 150, 238, 1.55, 255))
    items = [(270, 222, 'Please ring the bell', 'cormorant', 86, 600, 0.0, 'l'),
             (274, 296, 'A member of our team will be with you shortly', 'cormorant_i', 36, 500, 0.0, 'l'),
             (274, 378, 'AUREL & FINCH', 'cinzel', 26, 500, 0.30, 'l')]
    txt = text_ss(W, H, items)
    arrow = ss_mask(W, H, lambda d, s: d.polygon([(870 * s, 350 * s), (930 * s, 370 * s), (870 * s, 390 * s)], fill=255))
    def arrline(d, s):
        d.line([(720 * s, 370 * s), (880 * s, 370 * s)], fill=255, width=3 * s)
    arrow = np.maximum(arrow, ss_mask(W, H, arrline))
    m = np.maximum(np.maximum(icon, txt), arrow)
    gf = gold_fill(H, W, seed=21, light=0.95)
    col = composite(col, gf, m)
    # stand-off caps
    for (sx, sy) in ((44, 44), (W - 44, 44), (44, H - 44), (W - 44, H - 44)):
        yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
        r = np.sqrt((xx - sx) ** 2 + (yy - sy) ** 2)
        shade = np.clip(1.25 - ((xx - sx + 5) ** 2 + (yy - sy + 6) ** 2) / 260, 0.35, 1.3)
        col = composite(col, np.array([0.78, 0.78, 0.80], np.float32)[None, None] * shade[..., None], smooth_disc(r, 16))
    # fine dust and hairline scratches on the gloss
    col = np.clip(col + scratches(H, W, 70, seed=22, length=(6, 40))[..., None] * 0.06, 0, 1)
    dust = (np.random.default_rng(23).random((H, W)) > 0.9985).astype(np.float32)
    col = np.clip(col + ndimage.gaussian_filter(dust, 0.7)[..., None] * 0.5, 0, 1)
    save_png(col, os.path.join(OUT, 'Sign_PleaseRingBell.png'), shape)


# ------------------------------------------------------------------ card acceptance sticker (generic, no logos)
def card(d, s, x, y, w, h, colr, stripe, label):
    d.rounded_rectangle((x * s, y * s, (x + w) * s, (y + h) * s), 10 * s, fill=colr)
    d.rectangle((x * s, (y + h * 0.62) * s, (x + w) * s, (y + h * 0.70) * s), fill=stripe)
    # chip
    d.rounded_rectangle(((x + 16) * s, (y + 22) * s, (x + 50) * s, (y + 48) * s), 5 * s, fill=(222, 190, 110))
    d.line([((x + 16) * s, (y + 35) * s), ((x + 50) * s, (y + 35) * s)], fill=(160, 130, 70), width=s)
    d.line([((x + 33) * s, (y + 22) * s), ((x + 33) * s, (y + 48) * s)], fill=(160, 130, 70), width=s)
    f = font('barlow_b', 15 * s)
    d.text(((x + w - 12) * s, (y + h - 12) * s), label, font=f, fill=(255, 255, 255), anchor='rs')


def cards_sticker():
    W, H = 768, 512
    ss = 3
    im = Image.new('RGB', (W * ss, H * ss), (250, 250, 248))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((10 * ss, 10 * ss, (W - 10) * ss, (H - 10) * ss), 34 * ss, outline=(30, 40, 60), width=4 * ss)
    f = font('barlow_sb', 46 * ss)
    d.text((W / 2 * ss, 92 * ss), 'We welcome', font=f, fill=(30, 40, 60), anchor='ms')
    cols = [((26, 58, 140), (16, 36, 96), 'DEBIT'), ((178, 34, 40), (120, 20, 26), 'CREDIT'),
            ((22, 112, 86), (12, 74, 56), 'DEBIT'), ((28, 28, 30), (60, 60, 64), 'CREDIT')]
    for i, (c1, c2, lab) in enumerate(cols):
        card(d, ss, 64 + i * 164, 140, 148, 94, c1, c2, lab)
    f2 = font('barlow_m', 34 * ss)
    d.text((W / 2 * ss, 318 * ss), 'Chip & PIN  ·  Tap to pay  ·  Mobile', font=f2, fill=(30, 40, 60), anchor='ms')
    f3 = font('barlow', 24 * ss)
    d.text((W / 2 * ss, 380 * ss), 'Card payments over £100 may require ID.', font=f3, fill=(70, 76, 90), anchor='ms')
    d.text((W / 2 * ss, 420 * ss), 'Sorry, no cheques.', font=f3, fill=(70, 76, 90), anchor='ms')
    col = from_img(im.resize((W, H), Image.LANCZOS))
    shape = rounded_rect_mask(W, H, 40)
    # sun fade + yellowing of the vinyl, dirt, a corner starting to peel
    y = np.linspace(0, 1, H, dtype=np.float32)[:, None, None]
    col = col + (np.array([0.93, 0.91, 0.82], np.float32) - col) * (0.12 + 0.1 * (1 - y))
    col = composite(col, np.array([0.45, 0.42, 0.36], np.float32), grime(H, W, seed=31, amount=0.15, edge=0.25) * 0.4)
    col = np.clip(col - scratches(H, W, 50, seed=32)[..., None] * 0.08, 0, 1)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    peel = np.clip(((xx - (W - 90)) + (yy - (H - 90))) / 40, 0, 1)
    col = col * (1 - 0.18 * peel[..., None])
    alpha = shape * (1 - 0.08 * peel)
    save_png(col, os.path.join(OUT, 'Sticker_CardsAccepted.png'), alpha)


# ------------------------------------------------------------------ alarm company sticker (fictional company)
def alarm_sticker():
    W = H = 512
    ss = 3
    im = Image.new('L', (W * ss, H * ss), 0)
    d = ImageDraw.Draw(im)
    shield = [(256, 18), (470, 80), (452, 300), (256, 494), (60, 300), (42, 80)]
    d.polygon([(x * ss, y * ss) for x, y in shield], fill=255)
    alpha = mask_from(im.resize((W, H), Image.LANCZOS))
    navy = np.array([0.06, 0.12, 0.30], np.float32)
    col = np.zeros((H, W, 3), np.float32) + navy
    col *= (1 + 0.25 * np.linspace(1, -1, H, dtype=np.float32)[:, None, None])
    inner = Image.new('L', (W * ss, H * ss), 0)
    di = ImageDraw.Draw(inner)
    sh2 = [(256, 40), (448, 96), (432, 292), (256, 466), (80, 292), (64, 96)]
    di.polygon([(x * ss, y * ss) for x, y in sh2], outline=255, width=6 * ss)
    ring = mask_from(inner.resize((W, H), Image.LANCZOS))
    col = composite(col, np.array([0.86, 0.10, 0.12], np.float32), ring)
    items = [(256, 150, 'CORVANE', 'barlow_blk', 74, None, 0.06, 'm'),
             (256, 196, 'ALARM SYSTEMS', 'barlow_sb', 30, None, 0.16, 'm'),
             (256, 268, 'MONITORED', 'barlow_xb', 44, None, 0.04, 'm'),
             (256, 312, '24 HOURS', 'barlow_xb', 44, None, 0.04, 'm'),
             (256, 366, 'POLICE RESPONSE', 'barlow_sb', 24, None, 0.10, 'm'),
             (256, 400, 'Grade 3 Intruder Alarm', 'barlow_m', 20, None, 0.0, 'm')]
    col = composite(col, np.array([0.97, 0.97, 0.95], np.float32), text_ss(W, H, items))
    rule = ss_mask(W, H, lambda d, s: d.rectangle(((256 - 70) * s, 218 * s, (256 + 70) * s, 224 * s), fill=255))
    col = composite(col, np.array([0.86, 0.10, 0.12], np.float32), rule)
    col = col + (np.array([0.70, 0.75, 0.85], np.float32) - col) * 0.10 * fbm(H, W, 120, 3, 41)[..., None]
    col = np.clip(col + scratches(H, W, 40, seed=42)[..., None] * 0.12, 0, 1)
    save_png(col, os.path.join(OUT, 'Sticker_AlarmMonitored.png'), alpha)


# ------------------------------------------------------------------ insurer-approved sticker (fictional)
def insurance_sticker():
    W, H = 512, 256
    ss = 3
    shape = rounded_rect_mask(W, H, 18)
    col = np.zeros((H, W, 3), np.float32) + np.array([0.94, 0.93, 0.89], np.float32)
    band = np.zeros((H, W), np.float32)
    band[:74] = 1
    col = composite(col, np.array([0.10, 0.22, 0.20], np.float32), band)
    items = [(W / 2, 50, 'SECURITY APPROVED', 'barlow_xb', 36, None, 0.10, 'm')]
    col = composite(col, np.array([0.95, 0.93, 0.86], np.float32), text_ss(W, H, items))
    items2 = [(W / 2, 118, 'Ashcombe & Vey Underwriting', 'baskerville', 26, 700, 0.0, 'm'),
              (W / 2, 156, 'Fine Jewellery & Specie Scheme', 'baskerville_i', 21, 400, 0.0, 'm'),
              (W / 2, 204, 'Premises Ref. AV-1962-0417', 'barlow_m', 19, None, 0.04, 'm'),
              (W / 2, 232, 'Valid to 31.12.2026', 'barlow_m', 17, None, 0.04, 'm')]
    col = composite(col, np.array([0.10, 0.20, 0.18], np.float32), text_ss(W, H, items2))
    col = composite(col, np.array([0.5, 0.48, 0.42], np.float32), grime(H, W, seed=51, amount=0.18, edge=0.3) * 0.35)
    save_png(col, os.path.join(OUT, 'Sticker_InsurerApproved.png'), shape)


# ------------------------------------------------------------------ back in 5 minutes (hanging card)
def back_in_five():
    W, H = 1024, 768
    ss = 3
    # card occupies the lower 78%; cord and suction hook above
    cx0, cy0, cx1, cy1 = 112, 214, 912, 734
    im = Image.new('L', (W * ss, H * ss), 0)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((cx0 * ss, cy0 * ss, cx1 * ss, cy1 * ss), 26 * ss, fill=255)
    card_m = mask_from(im.resize((W, H), Image.LANCZOS))
    cord = ss_mask(W, H, lambda d, s: d.line([(300 * s, 250 * s), (512 * s, 70 * s), (724 * s, 250 * s)], fill=255, width=5 * s, joint='curve'))
    hook = ss_mask(W, H, lambda d, s: d.ellipse(((512 - 46) * s, 22 * s, (512 + 46) * s, 92 * s), fill=255))
    holes = ss_mask(W, H, lambda d, s: [d.ellipse(((x - 11) * s, (250 - 11) * s, (x + 11) * s, (250 + 11) * s), fill=255) for x in (300, 724)])
    pap = paper(H, W, base=(0.97, 0.96, 0.93), seed=61, fiber=0.02, mottle=0.03)
    col = pap.copy()
    red = np.array([0.72, 0.10, 0.12], np.float32)
    navy = np.array([0.08, 0.10, 0.18], np.float32)
    # border
    col = composite(col, red, ss_mask(W, H, lambda d, s: d.rounded_rectangle(((cx0 + 18) * s, (cy0 + 18) * s, (cx1 - 18) * s, (cy1 - 18) * s), 16 * s, outline=255, width=8 * s)))
    items = [(370, 382, 'Back in', 'barlow_xb', 104, None, 0.0, 'l'),
             (370, 520, '5 minutes', 'barlow_xb', 104, None, 0.0, 'l'),
             (370, 600, 'Sorry for any inconvenience', 'barlow_m', 38, None, 0.0, 'l')]
    col = composite(col, navy, text_ss(W, H, items))
    # clock face with hands at 5 past
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    ccx, ccy = 245, 470
    r = np.sqrt((xx - ccx) ** 2 + (yy - ccy) ** 2)
    col = composite(col, red, smooth_disc(r, 104) * (1 - smooth_disc(r, 92)))
    ticks = ss_mask(W, H, lambda d, s: [d.line([((ccx + 78 * math.sin(a)) * s, (ccy - 78 * math.cos(a)) * s),
                                                ((ccx + 88 * math.sin(a)) * s, (ccy - 88 * math.cos(a)) * s)], fill=255, width=4 * s)
                                        for a in np.radians(np.arange(0, 360, 30))])
    hands = ss_mask(W, H, lambda d, s: (d.line([(ccx * s, ccy * s), ((ccx + 70 * math.sin(math.radians(30))) * s, (ccy - 70 * math.cos(math.radians(30))) * s)], fill=255, width=6 * s),
                                        d.line([(ccx * s, ccy * s), ((ccx + 46 * math.sin(math.radians(2.5 * 30 + 2.5))) * s, (ccy - 46 * math.cos(math.radians(2.5 * 30 + 2.5))) * s)], fill=255, width=9 * s)))
    col = composite(col, navy, np.maximum(ticks, hands))
    col = composite(col, navy, smooth_disc(r, 9))
    # handwriting: someone added a note
    hw = text_ss(W, H, [(370, 680, '(staff at the rear – knock!)', 'caveat', 46, 600, 0.0, 'l')])
    col = composite(col, np.array([0.10, 0.12, 0.45], np.float32), hw * 0.9)
    # age: card darker at edges, slight sun fade, creases
    col = composite(col, np.array([0.55, 0.5, 0.42], np.float32), grime(H, W, seed=62, amount=0.15, edge=0.25) * 0.4)
    crease = ss_mask(W, H, lambda d, s: d.line([(cx0 * s, 600 * s), (cx1 * s, 560 * s)], fill=255, width=2 * s))
    col = col * (1 - 0.10 * ndimage.gaussian_filter(crease, 1.5)[..., None])
    holes_m = holes
    alpha = np.clip(card_m - holes_m, 0, 1)
    # cord (white string) and clear suction hook (light grey translucent)
    col = composite(col, np.array([0.90, 0.88, 0.84], np.float32), cord)
    alpha = np.maximum(alpha, cord)
    col = composite(col, np.array([0.80, 0.84, 0.86], np.float32), hook)
    alpha = np.maximum(alpha, hook * 0.55)
    save_png(col, os.path.join(OUT, 'Sign_BackIn5Minutes.png'), alpha)


for fn in (hours_plate, cctv_sign, ring_bell, cards_sticker, alarm_sticker, insurance_sticker, back_in_five):
    fn()
    print(fn.__name__, flush=True)
print('FONTS', sorted(USED_FONTS))
