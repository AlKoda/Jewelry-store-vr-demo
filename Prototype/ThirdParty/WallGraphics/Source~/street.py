# Street-level graphics: fly-posted wall, no-parking sign, graffiti tags, newspaper front page,
# lost-cat flyer, police cordon notice. Project-original artwork; every name and number is fictional.
# Photographs: scikit-image sample images "chelsea" and "coffee" (CC0); painting crops: public domain.
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw
from gfx import *

OUT = sys.argv[1]
ART = sys.argv[2]        # prepared public-domain paintings (pack Artwork dir)
SK = sys.argv[3]         # scikit-image CC0 photos dir
os.makedirs(OUT, exist_ok=True)
SS = 3
rng_global = np.random.default_rng(2026)


def mask_draw(w, h, fn, ss=SS):
    im = Image.new('L', (w * ss, h * ss), 0)
    fn(ImageDraw.Draw(im), ss)
    return mask_from(im.resize((w, h), Image.LANCZOS))


def tmask(w, h, items, ss=SS):
    def fn(d, s):
        for it in items:
            x, y, t, fn_, size = it[:5]
            wg = it[5] if len(it) > 5 else None
            tr = it[6] if len(it) > 6 else 0.0
            an = it[7] if len(it) > 7 else 'l'
            draw_text(d, (x * s, y * s), t, font(fn_, size * s, wg), 255, tr, an)
    return mask_draw(w, h, fn, ss)


def fit_crop(path, w, h, cx=0.5, cy=0.5, zoom=1.0):
    im = Image.open(path).convert('RGB')
    s = max(w / im.width, h / im.height) * zoom
    im = im.resize((max(w, int(im.width * s) + 1), max(h, int(im.height * s) + 1)), Image.LANCZOS)
    ox = int((im.width - w) * cx)
    oy = int((im.height - h) * cy)
    return from_img(im.crop((ox, oy, ox + w, oy + h)))


def halftone(gray, cell=6, angle=45, gain=1.0):
    """Amplitude-modulated halftone of a grey image (0 white..1 black ink coverage)."""
    h, w = gray.shape
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    a = math.radians(angle)
    u = (x * math.cos(a) + y * math.sin(a)) / cell
    v = (-x * math.sin(a) + y * math.cos(a)) / cell
    du = u - np.floor(u) - 0.5
    dv = v - np.floor(v) - 0.5
    r = np.sqrt(du * du + dv * dv)
    cov = np.clip(gray * gain, 0, 1)
    rad = np.sqrt(cov / np.pi)
    return np.clip((rad - r) * cell * 0.9 + 0.5, 0, 1)


def lum(rgb):
    return rgb @ np.array([0.299, 0.587, 0.114], np.float32)


# ------------------------------------------------------------------ spray paint
def spray(canvas, alpha, mask, color, seed, halo=0.35, speck=0.5, drips=10, drip_len=(20, 90)):
    rng = np.random.default_rng(seed)
    h, w = mask.shape
    core = ndimage.gaussian_filter(mask, 0.8)
    hb = ndimage.gaussian_filter(mask, 5.0)
    hb2 = ndimage.gaussian_filter(mask, 12.0)
    noise = rng.random((h, w)).astype(np.float32)
    specks = ((noise < hb2 * speck * 0.25) & (hb2 > 0.02)).astype(np.float32)
    specks = ndimage.gaussian_filter(specks, 0.5) * 1.5
    a = np.clip(core + hb * halo * 0.6 + specks, 0, 1)
    # drips from the lower edges of strokes
    if drips:
        ys, xs = np.nonzero((mask > 0.5) & (np.roll(mask, -3, 0) < 0.2))
        if ys.size:
            idx = rng.choice(ys.size, size=min(drips, ys.size), replace=False)
            dm = Image.new('L', (w * 2, h * 2), 0)
            dd = ImageDraw.Draw(dm)
            for i in idx:
                x0, y0 = xs[i] * 2, ys[i] * 2
                L = rng.uniform(*drip_len) * 2
                wid = rng.uniform(2.0, 4.5) * 2
                dd.line([(x0, y0), (x0 + rng.normal(0, 1.5), y0 + L)], fill=255, width=int(wid))
                dd.ellipse((x0 - wid * 0.7, y0 + L - wid * 0.7, x0 + wid * 0.7, y0 + L + wid * 0.9), fill=255)
            dmk = mask_from(dm.resize((w, h), Image.LANCZOS))
            a = np.maximum(a, dmk * 0.95)
    col = np.array(color, np.float32)
    canvas = composite(canvas, col, a)
    alpha = np.maximum(alpha, a)
    return canvas, alpha


def tag_mask(w, h, text, fn, size, x, y, shear=-0.35, rot=-6, wght=None):
    m = Image.new('L', (w, h), 0)
    d = ImageDraw.Draw(m)
    d.text((x, y), text, font=font(fn, size, wght), fill=255, anchor='mm')
    m = m.transform((w, h), Image.AFFINE, (1, shear, -shear * y, 0, 1, 0), resample=Image.BICUBIC)
    m = m.rotate(rot, resample=Image.BICUBIC, center=(x, y))
    return mask_from(m)


def bubble_mask(w, h, text, size, x, y, grow=12, rot=-4):
    m = Image.new('L', (w, h), 0)
    f = font('anton', size)
    draw_text(ImageDraw.Draw(m), (x, y + size * 0.36), text, f, 255, 0.22, 'm')
    m = m.rotate(rot, resample=Image.BICUBIC, center=(x, y))
    a = mask_from(m)
    a = ndimage.gaussian_filter(a, grow * 0.7)
    fill = np.clip((a - 0.30) * 10, 0, 1)
    return fill


def graffiti_layer(w, h, seed, scale=1.0):
    """Returns RGB, alpha of a sheet of original tags and one throw-up."""
    col = np.zeros((h, w, 3), np.float32)
    al = np.zeros((h, w), np.float32)
    s = scale
    # throw-up: silver fill, black outline, white highlights
    fill = bubble_mask(w, h, 'OSKA', int(250 * s), int(w * 0.30), int(h * 0.50), grow=int(9 * s))
    outline = np.clip(ndimage.grey_dilation(fill, size=(int(15 * s), int(15 * s))) - fill, 0, 1)
    shadow = np.roll(np.roll(ndimage.grey_dilation(fill, size=(int(15 * s), int(15 * s))), int(10 * s), 0), int(12 * s), 1)
    col, al = spray(col, al, np.clip(shadow - ndimage.grey_dilation(fill, size=(int(15 * s), int(15 * s))), 0, 1),
                    (0.05, 0.05, 0.06), seed, halo=0.25, drips=0)
    yy = np.linspace(0, 1, h, dtype=np.float32)[:, None, None]
    chrome = np.clip(0.55 + 0.35 * np.cos(yy * 9.0) + 0.05 * fbm(h, w, 40, 3, seed)[..., None], 0, 1) * np.array([0.92, 0.93, 0.95], np.float32)
    al_f = np.clip(ndimage.gaussian_filter(fill, 1.0) + ndimage.gaussian_filter(fill, 4) * 0.2, 0, 1)
    col = composite(col, chrome, al_f)
    al = np.maximum(al, al_f)
    col, al = spray(col, al, outline, (0.04, 0.04, 0.05), seed + 1, halo=0.3, drips=8, drip_len=(15, 60))
    # tags (handstyles)
    tags = [('Zephr', 'sedgwick', int(170 * s), 0.70, 0.30, (0.06, 0.06, 0.07), -0.35, -8),
            ('NYTE', 'marker', int(120 * s), 0.78, 0.74, (0.75, 0.08, 0.10), -0.25, 4),
            ('krom', 'sedgwick', int(120 * s), 0.10, 0.20, (0.10, 0.25, 0.70), -0.4, -12),
            ('vapo 26', 'rocksalt', int(70 * s), 0.55, 0.88, (0.06, 0.06, 0.07), -0.2, -3)]
    for i, (t, fn_, size, fx, fy, c_, sh, rot) in enumerate(tags):
        m = tag_mask(w, h, t, fn_, size, int(w * fx), int(h * fy), sh, rot)
        col, al = spray(col, al, m, c_, seed + 10 + i, halo=0.4, drips=6 + 3 * i)
    return col, al


# ------------------------------------------------------------------ brick wall
def brick_wall(h, w, seed, ppm=683):
    rng = np.random.default_rng(seed)
    bw, bh, mo = int(0.215 * ppm), int(0.065 * ppm), max(4, int(0.010 * ppm))
    mortar = np.array([0.40, 0.38, 0.35], np.float32)
    col = mortar[None, None] * (0.75 + 0.35 * fbm(h, w, 6, 3, seed)[..., None])
    noise = fbm(h, w, 5, 3, seed + 1)
    noise2 = fbm(h, w, 30, 3, seed + 3)
    big = fbm(h, w, 260, 3, seed + 2)
    shade = np.zeros((h, w), np.float32)
    row = 0
    for y0 in range(0, h, bh + mo):
        off = (bw + mo) // 2 if row % 2 else 0
        for x0 in range(-off, w, bw + mo):
            tint = rng.uniform(0.0, 1.0)
            base = (np.array([0.47, 0.25, 0.17], np.float32) * (1 - tint) + np.array([0.40, 0.27, 0.20], np.float32) * tint) \
                * rng.uniform(0.78, 1.12) + rng.normal(0, 0.02, 3).astype(np.float32)
            ys, xs = slice(max(0, y0), min(h, y0 + bh)), slice(max(0, x0), min(w, x0 + bw))
            if ys.stop <= ys.start or xs.stop <= xs.start:
                continue
            yy, xx = np.mgrid[ys, xs].astype(np.float32)
            ed = np.minimum(np.minimum(yy - y0, y0 + bh - 1 - yy), np.minimum(xx - x0, x0 + bw - 1 - xx))
            shade[ys, xs] = np.clip(ed / 4.0, 0, 1)
            n = noise[ys, xs][..., None]
            col[ys, xs] = base * (0.72 + 0.5 * n) * (0.85 + 0.3 * noise2[ys, xs][..., None]) * (0.9 + 0.2 * big[ys, xs][..., None])
        row += 1
    # rounded arrises and recessed joints
    col = col * (0.82 + 0.18 * shade[..., None])
    sp = (rng.random((h, w)) > 0.985).astype(np.float32)
    col = col * (1 - 0.25 * ndimage.gaussian_filter(sp, 0.6)[..., None])
    return np.clip(col, 0, 1)


# ------------------------------------------------------------------ poster designs (A1 405x574 / A2 203x287 at 683 px/m)
def poster_gig(w, h, seed, c1=(0.95, 0.15, 0.55), c2=(0.10, 0.85, 0.90)):
    col = np.zeros((h, w, 3), np.float32) + np.array([0.06, 0.05, 0.08], np.float32)
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    r = np.sqrt((x - w * 0.5) ** 2 + (y - h * 0.42) ** 2) / w
    dots = halftone(np.clip(1.2 - r * 2.2, 0, 1), cell=max(4, w // 40), angle=30)
    col = composite(col, c1, dots * 0.9)
    s = w / 405
    col = composite(col, (0.97, 0.95, 0.90), tmask(w, h, [(w / 2, 90 * s, 'THE', 'anton', 44 * s, None, 0.3, 'm'),
                                                           (w / 2, 170 * s, 'VELVET', 'anton', 92 * s, None, 0.02, 'm'),
                                                           (w / 2, 262 * s, 'STATIC', 'anton', 92 * s, None, 0.02, 'm')]))
    col = composite(col, c2, tmask(w, h, [(w / 2, 330 * s, 'LIVE  +  SPECIAL GUESTS', 'oswald', 24 * s, 600, 0.12, 'm')]))
    col = composite(col, (0.97, 0.95, 0.90), tmask(w, h, [(w / 2, 470 * s, 'THE FOUNDRY', 'bebas', 50 * s, None, 0.06, 'm'),
                                                           (w / 2, 512 * s, 'FRI 23 OCT  ·  DOORS 7PM', 'oswald', 22 * s, 500, 0.08, 'm'),
                                                           (w / 2, 548 * s, 'tickets on the door', 'oswald', 16 * s, 400, 0.1, 'm')]))
    return col


def poster_market(w, h, seed):
    col = np.zeros((h, w, 3), np.float32) + np.array([0.96, 0.91, 0.80], np.float32)
    s = w / 405
    stripes = mask_draw(w, h, lambda d, ss: [d.rectangle((0, (k * 26 * s) * ss, w * ss, (k * 26 * s + 13 * s) * ss), fill=255) for k in range(0, 6)])
    col = composite(col, (0.93, 0.42, 0.12), stripes)
    col = composite(col, (0.12, 0.16, 0.30), tmask(w, h, [(w / 2, 270 * s, 'NIGHT', 'bebas', 130 * s, None, 0.02, 'm'),
                                                           (w / 2, 380 * s, 'MARKET', 'bebas', 110 * s, None, 0.02, 'm')]))
    col = composite(col, (0.93, 0.42, 0.12), tmask(w, h, [(w / 2, 432 * s, 'STREET FOOD  ·  VINYL  ·  MAKERS', 'oswald', 20 * s, 600, 0.06, 'm')]))
    col = composite(col, (0.12, 0.16, 0.30), tmask(w, h, [(w / 2, 500 * s, 'EVERY SATURDAY 5–11PM', 'oswald', 26 * s, 700, 0.04, 'm'),
                                                           (w / 2, 540 * s, 'Canal Yard, under the arches', 'oswald', 19 * s, 400, 0.02, 'm')]))
    return col


def poster_gallery(w, h, seed):
    s = w / 405
    col = np.zeros((h, w, 3), np.float32) + np.array([0.94, 0.93, 0.90], np.float32)
    pic = fit_crop(os.path.join(ART, 'Art_Lorrain_MorningInTheHarbour.jpg'), w - int(40 * s), int(330 * s), 0.35, 0.45, 1.15)
    col[int(20 * s):int(20 * s) + pic.shape[0], int(20 * s):int(20 * s) + pic.shape[1]] = pic
    col = composite(col, (0.10, 0.10, 0.12), tmask(w, h, [(24 * s, 400 * s, 'IMPRESSIONS', 'cormorant', 58 * s, 600, 0.04, 'l'),
                                                           (24 * s, 440 * s, 'Harbours, light and the golden hour', 'cormorant_i', 24 * s, 500, 0.0, 'l'),
                                                           (24 * s, 500 * s, 'CITY GALLERY', 'barlow_b', 20 * s, None, 0.2, 'l'),
                                                           (24 * s, 528 * s, 'Late opening Thursdays until 9pm  ·  Free entry', 'barlow', 14 * s, None, 0.02, 'l')]))
    return col


def poster_club(w, h, seed):
    s = w / 405
    pic = fit_crop(os.path.join(ART, 'Art_Aivazovsky_NinthWave.jpg'), w, h, 0.35, 0.8, 1.0)
    g = lum(pic)
    duo = np.array([0.02, 0.10, 0.08], np.float32) + (np.array([0.30, 1.0, 0.65], np.float32) - np.array([0.02, 0.10, 0.08], np.float32)) * (g[..., None] ** 1.3)
    col = duo
    col = composite(col, (0.96, 0.98, 0.95), tmask(w, h, [(w / 2, 120 * s, 'DEEP', 'anton', 120 * s, None, 0.04, 'm'),
                                                           (w / 2, 240 * s, 'CURRENT', 'anton', 92 * s, None, 0.02, 'm')]))
    col = composite(col, (0.96, 0.98, 0.95), tmask(w, h, [(w / 2, 470 * s, 'DRUM & BASS ALL NIGHT', 'oswald', 26 * s, 700, 0.06, 'm'),
                                                           (w / 2, 510 * s, 'BASEMENT 21  ·  SAT 24 OCT  ·  10PM–4AM', 'oswald', 18 * s, 500, 0.06, 'm')]))
    return col


def poster_comedy(w, h, seed):
    s = w / 203
    col = np.zeros((h, w, 3), np.float32) + np.array([0.98, 0.84, 0.10], np.float32)
    col = composite(col, (0.06, 0.06, 0.06), tmask(w, h, [(w / 2, 60 * s, 'LAUGH', 'anton', 54 * s, None, 0.02, 'm'),
                                                           (w / 2, 118 * s, 'TRACK', 'anton', 54 * s, None, 0.02, 'm'),
                                                           (w / 2, 160 * s, 'STAND-UP COMEDY', 'oswald', 15 * s, 700, 0.1, 'm'),
                                                           (w / 2, 230 * s, 'THE CROWN ROOMS', 'oswald', 17 * s, 600, 0.04, 'm'),
                                                           (w / 2, 256 * s, 'every Thursday 8pm', 'oswald', 13 * s, 400, 0.04, 'm')]))
    return col


def poster_circus(w, h, seed):
    s = w / 203
    col = np.zeros((h, w, 3), np.float32) + np.array([0.08, 0.10, 0.30], np.float32)
    rng = np.random.default_rng(seed)
    stars = mask_draw(w, h, lambda d, ss: [d.ellipse(((x - r) * ss, (y - r) * ss, (x + r) * ss, (y + r) * ss), fill=255)
                                           for x, y, r in zip(rng.uniform(0, w, 60), rng.uniform(0, h * 0.6, 60), rng.uniform(0.6, 2.2, 60) * s)])
    col = composite(col, (1.0, 0.9, 0.5), stars)
    col = composite(col, (0.98, 0.82, 0.30), tmask(w, h, [(w / 2, 150 * s, 'CIRQUE', 'cinzel', 34 * s, 700, 0.08, 'm'),
                                                           (w / 2, 190 * s, 'LUMEN', 'cinzel', 34 * s, 700, 0.08, 'm'),
                                                           (w / 2, 222 * s, 'a circus of light', 'cormorant_i', 15 * s, 500, 0.0, 'm'),
                                                           (w / 2, 262 * s, 'Riverside Park · 30 Oct – 8 Nov', 'barlow_m', 10.5 * s, None, 0.0, 'm')]))
    return col


def torn(w, h, seed, amount=0.4, bottom_bias=0.6):
    """Alpha of a pasted poster after tearing (1 = paper present)."""
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    n = fbm(h, w, scale=max(w, h) / 3.0, octaves=6, seed=seed)
    nv = np.asarray(Image.fromarray((fbm(h, max(8, w // 4), scale=max(w, h) / 6.0, octaves=5, seed=seed + 3) * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)).astype(np.float32) / 255
    n = 0.6 * n + 0.4 * nv
    ex = np.minimum(x, w - 1 - x) / w
    ey = np.minimum(y, h - 1 - y) / h
    edge = np.exp(-np.minimum(ex, ey) * 9)
    bot = (y / h) ** 2 * bottom_bias
    thr = 1.0 - amount * (0.4 + edge * 0.9 + bot)
    keep = (n < thr).astype(np.float32)
    keep = ndimage.binary_opening(keep > 0.5, iterations=2).astype(np.float32)
    # rough fibrous edge
    rough = fbm(h, w, scale=3, octaves=2, seed=seed + 7)
    keep = np.clip(ndimage.gaussian_filter(keep, 1.2) + (rough - 0.5) * 0.6, 0, 1)
    keep = (keep > 0.5).astype(np.float32)
    return keep


def paste(base, poster, x, y, keep, seed, fade=0.0, wrinkle=1.0):
    h, w = poster.shape[:2]
    H, W = base.shape[:2]
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(W, x + w), min(H, y + h)
    if x1 <= x0 or y1 <= y0:
        return base
    p = poster[y0 - y:y1 - y, x0 - x:x1 - x]
    k = keep[y0 - y:y1 - y, x0 - x:x1 - x]
    # paper-white fibres along torn edges
    ring = np.clip(ndimage.grey_dilation(k, size=(4, 4)) - k, 0, 1)
    # wheat-paste wrinkles and bubbles
    hgt = 0.5 * fbm(y1 - y0, x1 - x0, scale=40, octaves=3, seed=seed) + fbm(y1 - y0, x1 - x0, scale=120, octaves=2, seed=seed + 1)
    sh = emboss(hgt, 1.5, light=(0.5, 1.0)) * 2.2 * wrinkle
    p = np.clip(p * (1 + sh[..., None]), 0, 1)
    if fade:
        g = lum(p)[..., None]
        p = p * (1 - fade) + (g * 0.6 + 0.4 * np.array([0.92, 0.90, 0.84], np.float32)) * fade
    region = base[y0:y1, x0:x1]
    region = composite(region, np.array([0.93, 0.92, 0.88], np.float32), ring * 0.9)
    region = composite(region, p, k)
    base[y0:y1, x0:x1] = region
    return base


def street_posters():
    W, H = 2048, 1024
    col = brick_wall(H, W, 101)
    rng = np.random.default_rng(102)
    A1 = (405, 574)
    A2 = (203, 287)
    # old, faded underlayer
    old = [(poster_market, -60, 120), (poster_gig, 330, 110), (poster_club, 720, 130), (poster_market, 1110, 115),
           (poster_gig, 1500, 120), (poster_club, 1880, 125)]
    for i, (fn, x, y) in enumerate(old):
        p = fn(A1[0], A1[1], 200 + i) if fn is not poster_gig else fn(A1[0], A1[1], 200 + i, (0.2, 0.6, 0.9), (0.9, 0.8, 0.2))
        col = paste(col, p, x, y, torn(A1[0], A1[1], 300 + i, amount=0.75), 400 + i, fade=0.55)
    # current layer: repeated fly-posters
    cur = [(poster_gig, 40, 150), (poster_gig, 430, 158), (poster_market, 840, 140), (poster_market, 1235, 146),
           (poster_gallery, 1630, 150)]
    for i, (fn, x, y) in enumerate(cur):
        p = fn(A1[0], A1[1], 500 + i)
        col = paste(col, p, x + int(rng.integers(-6, 6)), y, torn(A1[0], A1[1], 600 + i, amount=0.30), 700 + i, fade=0.12)
    small = [(poster_comedy, 90, 732), (poster_comedy, 300, 740), (poster_circus, 1010, 735), (poster_circus, 1220, 728),
             (poster_club, 1680, 720)]
    for i, (fn, x, y) in enumerate(small):
        wh = A2 if fn is not poster_club else (230, 326)
        p = fn(wh[0], wh[1], 800 + i)
        col = paste(col, p, x, y, torn(wh[0], wh[1], 900 + i, amount=0.5, bottom_bias=1.2), 1000 + i, fade=0.18)
    # graffiti over the posters
    gcol, gal = graffiti_layer(W, H, 1100, scale=0.8)
    col = composite(col, gcol, gal * 0.92)
    # weathering: sun fade high up, rain streaks, soot and splash at the bottom
    y = np.linspace(0, 1, H, dtype=np.float32)[:, None, None]
    g = lum(col)[..., None]
    col = col * (1 - 0.18 * (1 - y)) + (g * 0.5 + 0.45) * 0.18 * (1 - y)
    streak = ndimage.gaussian_filter(rng.random((H, W)).astype(np.float32), (60, 1.2))
    streak = np.clip((streak - 0.5) * 9, 0, 1) * (0.3 + 0.7 * fbm(H, W, 150, 3, 103))
    col = composite(col, np.array([0.18, 0.16, 0.14], np.float32), streak * 0.35)
    splash = np.clip(grime(H, W, seed=104, amount=0.6, edge=0.0) * (y[..., 0] ** 4) * 2.5, 0, 1)
    col = composite(col, np.array([0.20, 0.18, 0.15], np.float32), splash * 0.6)
    col = grain(col, 0.01, 105)
    save_jpg(col, os.path.join(OUT, 'StreetPosters_Weathered.jpg'), 86)


# ------------------------------------------------------------------ graffiti tags (alpha)
def graffiti_tags():
    W, H = 2048, 1024
    col, al = graffiti_layer(W, H, 1200, scale=1.25)
    save_png(col, os.path.join(OUT, 'Graffiti_Tags.png'), al)


# ------------------------------------------------------------------ no parking sign (Vienna-convention style, generic)
def no_parking():
    W, H = 1024, 1536
    cx, cy, R = 512, 470, 440
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    r = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
    disc = np.clip(R - r + 0.5, 0, 1)
    red = np.array([0.78, 0.10, 0.12], np.float32)
    blue = np.array([0.07, 0.24, 0.58], np.float32)
    white = np.array([0.94, 0.94, 0.92], np.float32)
    col = np.zeros((H, W, 3), np.float32) + white
    ring_o, ring_i = R - 10, R - 10 - 0.16 * R
    col = composite(col, red, np.clip(ring_o - r + 0.5, 0, 1))
    col = composite(col, blue, np.clip(ring_i - r + 0.5, 0, 1))
    # diagonal bar (top-left to bottom-right) inside the blue field
    u = ((xx - cx) - (yy - cy)) / math.sqrt(2)
    bar = np.clip(0.08 * R - np.abs(u) + 0.5, 0, 1) * np.clip(ring_i - r + 0.5, 0, 1)
    col = composite(col, red, bar)
    # plate
    px0, py0, px1, py1 = 152, 990, 872, 1420
    plate = mask_draw(W, H, lambda d, s: d.rounded_rectangle((px0 * s, py0 * s, px1 * s, py1 * s), 22 * s, fill=255))
    col = composite(col, white, plate)
    border = mask_draw(W, H, lambda d, s: d.rounded_rectangle(((px0 + 16) * s, (py0 + 16) * s, (px1 - 16) * s, (py1 - 16) * s), 14 * s, outline=255, width=8 * s))
    blk = np.array([0.06, 0.06, 0.07], np.float32)
    col = composite(col, blk, border)
    col = composite(col, blk, tmask(W, H, [(512, 1110, 'Mon – Sat', 'barlow_sb', 76, None, 0.0, 'm'),
                                            (512, 1210, '8 am – 6.30 pm', 'barlow_sb', 76, None, 0.0, 'm'),
                                            (512, 1330, 'Loading 20 mins', 'barlow_m', 62, None, 0.0, 'm')]))
    alpha = np.maximum(disc, plate)
    # retro-reflective sheeting micro texture + fade + dirt + scratches + sticker residue + rivets
    hexn = (np.sin(xx * 1.9) * np.sin(yy * 1.9 + xx * 0.95)) * 0.012
    col = col * (1 + hexn[..., None])
    fade = 0.10 + 0.10 * fbm(H, W, 220, 3, 121)[..., None]
    col = col + (np.array([0.90, 0.88, 0.86], np.float32) - col) * fade * (0.5 + 0.5 * (col[..., 0:1] > 0.5))
    col = composite(col, np.array([0.30, 0.28, 0.24], np.float32), grime(H, W, seed=122, amount=0.35, edge=0.0) * 0.35)
    rng = np.random.default_rng(123)
    sm = Image.new('L', (W, H), 0)
    dsm = ImageDraw.Draw(sm)
    for _ in range(80):
        x, y = rng.uniform(0, W), rng.uniform(0, H)
        a = rng.normal(0.4, 0.8)
        L = rng.uniform(10, 70)
        dsm.line([(x, y), (x + math.cos(a) * L, y + math.sin(a) * L)], fill=int(rng.uniform(80, 220)), width=1)
    col = np.clip(col + ndimage.gaussian_filter(mask_from(sm), 0.4)[..., None] * 0.22, 0, 1)
    # sticker residue (white paper fibres where a sticker was scraped off)
    st = np.clip(fbm(H, W, 10, 3, 124) - 0.55, 0, 1) * 2 * (((xx - 330) / 60) ** 2 + ((yy - 650) / 34) ** 2 < 1)
    col = composite(col, np.array([0.85, 0.86, 0.88], np.float32), np.clip(st, 0, 1) * 0.6)
    # marker tag on the plate
    m = tag_mask(W, H, 'krom', 'marker', 70, 760, 1390, -0.2, -6)
    col = composite(col, np.array([0.05, 0.05, 0.06], np.float32), m * 0.85)
    for (rx, ry) in ((cx, cy - R + 50), (cx, cy + R - 50), (512, py0 + 34), (512, py1 - 34)):
        rr = np.sqrt((xx - rx) ** 2 + (yy - ry) ** 2)
        col = composite(col, np.array([0.70, 0.70, 0.72], np.float32), np.clip(13 - rr + 0.5, 0, 1))
        rust = np.exp(-((xx - rx) ** 2) / 40) * np.exp(-np.maximum(yy - ry - 10, 0) / 22) * (yy > ry + 10)
        col = composite(col, np.array([0.45, 0.26, 0.12], np.float32), np.clip(rust * 0.5, 0, 0.35))
    save_png(col, os.path.join(OUT, 'Sign_NoParking.png'), alpha)


# ------------------------------------------------------------------ newspaper front page (fictional)
BODY = ("Shop owners on the High Street reported a third consecutive month of rising visitor numbers, according to figures "
        "published by the town centre partnership on Tuesday. Footfall was up eleven per cent on the same period last year, "
        "with evening trade showing the strongest growth since the pedestrian scheme was introduced. "
        "Independent retailers said longer opening hours and the weekly night market had brought new customers into the area. "
        "A spokesperson for the partnership said the results were encouraging but warned that rising rents remained a concern for "
        "smaller businesses. Plans for improved lighting and additional cycle parking will go before the planning committee next month. "
        "Residents are invited to comment on the proposals at the library until the end of October. "
        "The council said it would continue to monitor parking and deliveries along the route, after complaints from traders about "
        "loading restrictions during the busiest part of the day. ")


def justify_lines(d, s, x, y, width, text, f, lh, color=255, max_lines=999):
    words = text.split()
    lines, cur = [], []
    for w_ in words:
        t = ' '.join(cur + [w_])
        if f.getlength(t) <= width * s or not cur:
            cur.append(w_)
        else:
            lines.append(cur)
            cur = [w_]
    if cur:
        lines.append(cur)
    for i, ln in enumerate(lines[:max_lines]):
        yy = y + i * lh
        if i == len(lines) - 1 or len(ln) == 1:
            d.text((x * s, yy * s), ' '.join(ln), font=f, fill=color, anchor='ls')
            continue
        total = sum(f.getlength(w_) for w_ in ln)
        gap = (width * s - total) / (len(ln) - 1)
        xx = x * s
        for w_ in ln:
            d.text((xx, yy * s), w_, font=f, fill=color, anchor='ls')
            xx += f.getlength(w_) + gap
    return min(len(lines), max_lines)


def newspaper():
    W, H = 1024, 1536
    col = paper(H, W, base=(0.90, 0.89, 0.84), seed=131, fiber=0.02, mottle=0.03)
    ink = np.array([0.10, 0.10, 0.11], np.float32)
    M = 46
    # masthead
    col = composite(col, ink, tmask(W, H, [(W / 2, 172, 'The Evening Lamplighter', 'fraktur', 90, None, 0.0, 'm'),
                                            (W / 2, 50, 'YOUR CITY, EVERY EVENING SINCE 1871', 'oldstd', 16, None, 0.2, 'm')]))
    col = composite(col, ink, mask_draw(W, H, lambda d, s: (d.line([(M * s, 196 * s), ((W - M) * s, 196 * s)], fill=255, width=3 * s),
                                                             d.line([(M * s, 230 * s), ((W - M) * s, 230 * s)], fill=255, width=1 * s))))
    col = composite(col, ink, tmask(W, H, [(M, 220, 'WEDNESDAY 14 OCTOBER 2026', 'oldstd_b', 16, None, 0.08, 'l'),
                                            (W / 2, 220, 'No. 47,218', 'oldstd', 16, None, 0.08, 'm'),
                                            (W - M, 220, '90p', 'oldstd_b', 18, None, 0.0, 'r')]))
    # promo strip
    col = composite(col, np.array([0.70, 0.12, 0.12], np.float32), mask_draw(W, H, lambda d, s: d.rectangle((M * s, 26 * s, 250 * s, 58 * s), fill=255)))
    col = composite(col, np.array([0.96, 0.95, 0.92], np.float32), tmask(W, H, [(148, 49, 'FREE INSIDE: HOMES GUIDE', 'oswald', 15, 600, 0.04, 'm')]))
    # splash headline
    col = composite(col, ink, tmask(W, H, [(M, 320, 'HIGH STREET FOOTFALL', 'oswald', 76, 700, 0.0, 'l'),
                                            (M, 404, 'UP FOR THIRD MONTH', 'oswald', 76, 700, 0.0, 'l')]))
    col = composite(col, ink, tmask(W, H, [(M, 448, 'Evening trade leads the revival as traders welcome night market', 'ptserif_i', 25, None, 0.0, 'l')]))
    # photo: halftoned crop of a public-domain street painting
    PX0, PY0, PW, PH = M, 476, W - 2 * M, 470
    pic = fit_crop(os.path.join(ART, 'Art_Pissarro_BoulevardMontmartre.jpg'), PW, PH, 0.5, 0.55, 1.35)
    g = 1 - lum(pic)
    g = np.clip((g - 0.15) * 1.35, 0, 1)
    ht = halftone(g, cell=4.2, angle=45)
    region = col[PY0:PY0 + PH, PX0:PX0 + PW]
    col[PY0:PY0 + PH, PX0:PX0 + PW] = composite(region, ink, ht * 0.95)
    col = composite(col, ink, tmask(W, H, [(M, PY0 + PH + 26, 'Busy pavements on the boulevard last weekend. Picture: Lamplighter archive', 'ptserif_i', 16, None, 0.0, 'l')]))
    # body columns
    y0 = PY0 + PH + 66
    colw = (W - 2 * M - 2 * 24) / 3
    body_f = font('ptserif', 15 * SS)

    def body(d, s):
        txt = BODY + BODY
        x = M
        for k in range(2):
            justify_lines(d, s, x + k * (colw + 24), y0 + 18, colw, txt[k * 700:], body_f, 21, max_lines=19)
    col = composite(col, ink, mask_draw(W, H, body))
    col = composite(col, ink, tmask(W, H, [(M, y0 - 8, 'By Staff Reporter', 'ptserif_b', 15, None, 0.0, 'l')]))
    # third column: second story with coffee photo
    x3 = M + 2 * (colw + 24)
    col = composite(col, ink, tmask(W, H, [(x3, y0 + 16, 'Cafés toast later', 'oswald', 30, 700, 0.0, 'l'),
                                            (x3, y0 + 50, 'licence hours', 'oswald', 30, 700, 0.0, 'l')]))
    cof = fit_crop(os.path.join(SK, 'coffee.png'), int(colw), 150, 0.5, 0.5, 1.0)
    cg = np.clip((1 - lum(cof) - 0.1) * 1.3, 0, 1)
    cy0 = y0 + 66
    region = col[cy0:cy0 + 150, int(x3):int(x3) + int(colw)]
    col[cy0:cy0 + 150, int(x3):int(x3) + int(colw)] = composite(region, ink, halftone(cg, cell=3.8, angle=45) * 0.95)
    col = composite(col, ink, mask_draw(W, H, lambda d, s: justify_lines(d, s, x3, cy0 + 176, colw,
                                                                          'Late-night coffee is back on the menu after councillors agreed to extend opening hours for '
                                                                          'cafes on three streets near the station. Full story, page 7.', body_f, 21, max_lines=6)))
    # index bar
    col = composite(col, ink, mask_draw(W, H, lambda d, s: d.line([(M * s, (H - 64) * s), ((W - M) * s, (H - 64) * s)], fill=255, width=2 * s)))
    col = composite(col, ink, tmask(W, H, [(M, H - 34, 'INSIDE:  Weather 2  ·  Letters 14  ·  TV 30  ·  Puzzles 38  ·  Sport 44', 'oswald', 20, 500, 0.04, 'l'),
                                            (W - M, H - 34, 'Showers, 12°C', 'oswald', 20, 600, 0.0, 'r')]))
    # newsprint: ink spread, centre fold, edge yellowing, slight grime
    col = ndimage.gaussian_filter(col, (0.45, 0.45, 0))
    yy = np.arange(H, dtype=np.float32)[:, None]
    fold = np.exp(-((yy - H / 2) / 6) ** 2) * 0.10 - np.exp(-((yy - H / 2 - 9) / 10) ** 2) * 0.05
    col = np.clip(col * (1 - fold[..., None]), 0, 1)
    col = composite(col, np.array([0.80, 0.72, 0.52], np.float32), grime(H, W, seed=132, amount=0.10, edge=0.35) * 0.45)
    col = grain(col, 0.012, 133)
    save_jpg(col, os.path.join(OUT, 'Newspaper_FrontPage.jpg'), 87)


# ------------------------------------------------------------------ lost cat flyer (photocopy, tear-off tabs, tape)
def lost_cat():
    W, H = 768, 1024
    PX0, PY0, PX1, PY1 = 44, 30, 724, 990    # paper (A4 ratio-ish) inside the canvas, tape extends beyond
    pw, ph = PX1 - PX0, PY1 - PY0
    pap = paper(ph, pw, base=(0.95, 0.95, 0.93), seed=141, fiber=0.015, mottle=0.03)
    ink = np.array([0.08, 0.08, 0.09], np.float32)
    c = pap
    c = composite(c, ink, tmask(pw, ph, [(pw / 2, 120, 'LOST CAT', 'anton', 128, None, 0.04, 'm')]))
    cat = fit_crop(os.path.join(SK, 'chelsea.png'), 520, 346, 0.5, 0.5, 1.0)
    g = lum(cat)
    g = np.clip((g - 0.12) * 1.45, 0, 1)                     # photocopier contrast
    g = ndimage.gaussian_filter(g, 0.6)
    tone = 1 - g
    rngp = np.random.default_rng(142)
    tone = np.clip(tone + rngp.normal(0, 0.06, tone.shape).astype(np.float32), 0, 1)
    cx0, cy0 = (pw - 520) // 2, 160
    region = c[cy0:cy0 + 346, cx0:cx0 + 520]
    c[cy0:cy0 + 346, cx0:cx0 + 520] = composite(region, ink, np.clip(tone, 0, 1) * 0.92)
    lines = [(pw / 2, 580, 'Have you seen Mabel?', 'barlow_xb', 52, None, 0.0, 'm'),
             (pw / 2, 636, 'Tabby, green eyes, pink nose, 6 years old.', 'barlow_m', 28, None, 0.0, 'm'),
             (pw / 2, 674, 'Very friendly but nervous of dogs.', 'barlow_m', 28, None, 0.0, 'm'),
             (pw / 2, 712, 'Missing since Sunday 11 October', 'barlow_m', 28, None, 0.0, 'm'),
             (pw / 2, 750, 'near Canal Street / the High Street.', 'barlow_m', 28, None, 0.0, 'm'),
             (pw / 2, 800, 'Please check sheds and garages!', 'barlow_b', 30, None, 0.0, 'm'),
             (pw / 2, 846, 'REWARD', 'anton', 40, None, 0.08, 'm')]
    c = composite(c, ink, tmask(pw, ph, lines))
    # tear-off tabs
    tab_top = 872
    ntab = 10
    tw = pw / ntab
    cuts = mask_draw(pw, ph, lambda d, s: [d.line([((k * tw) * s, tab_top * s), ((k * tw) * s, ph * s)], fill=255, width=2 * s) for k in range(1, ntab)])
    dash = mask_draw(pw, ph, lambda d, s: [d.line([(x * s, tab_top * s), ((x + 8) * s, tab_top * s)], fill=255, width=s) for x in range(0, pw, 16)])
    c = composite(c, ink, np.maximum(cuts, dash) * 0.8)
    tabtxt = Image.new('L', (int(ph - tab_top) * SS, int(tw) * SS), 0)
    dt = ImageDraw.Draw(tabtxt)
    dt.text((6 * SS, tw * 0.42 * SS), 'Mabel', font=font('barlow_b', 15 * SS), fill=255, anchor='lm')
    # size 12 so all 11 digits fit between the paper edge and the perforation (tab is 88 px long)
    dt.text((5 * SS, tw * 0.78 * SS), '07700 900 482', font=font('barlow_m', 12 * SS), fill=255, anchor='lm')
    tabtxt = tabtxt.rotate(90, expand=True).resize((int(tw), int(ph - tab_top)), Image.LANCZOS)
    tm = mask_from(tabtxt)
    tabink = np.zeros((ph, pw), np.float32)
    for k in range(ntab):
        x = int(k * tw)
        sub = c[tab_top:tab_top + tm.shape[0], x:x + tm.shape[1]]
        c[tab_top:tab_top + tm.shape[0], x:x + tm.shape[1]] = composite(sub, ink, tm[:sub.shape[0], :sub.shape[1]])
        tabink[tab_top:tab_top + sub.shape[0], x:x + sub.shape[1]] = np.maximum(tabink[tab_top:tab_top + sub.shape[0], x:x + sub.shape[1]], tm[:sub.shape[0], :sub.shape[1]])
    # photocopy toner speckle and streak; no speck within 4 px of the tab text, so none reads as a dot in the number
    sp = (rngp.random((ph, pw)) > 0.9985).astype(np.float32)
    sp *= (ndimage.maximum_filter(tabink, size=9) < 0.05)
    c = composite(c, ink, ndimage.gaussian_filter(sp, 0.6) * 2.0)
    streak = np.exp(-((np.arange(pw, dtype=np.float32) - pw * 0.71) / 3.0) ** 2)[None, :] * 0.10
    c = c * (1 - streak[..., None])
    alpha_p = np.ones((ph, pw), np.float32)
    torn = (2, 5, 6)  # torn-off tabs; neighbouring torn tabs leave no sliver between them
    for k in torn:
        x0 = int(k * tw)
        xs = x0 - 1 if (k - 1) in torn else x0 + 1
        alpha_p[tab_top + 4:, xs:int((k + 1) * tw)] = 0
    # rain damage: wrinkles + slight tide marks
    hgt = fbm(ph, pw, 70, 4, 143)
    sh = emboss(hgt, 2.0, light=(0.4, 1.0)) * 5
    c = np.clip(c * (1 + sh[..., None]), 0, 1)
    tide = np.clip(np.abs(fbm(ph, pw, 160, 3, 144) - 0.5) < 0.012, 0, 1).astype(np.float32)
    c = composite(c, np.array([0.70, 0.66, 0.55], np.float32), ndimage.gaussian_filter(tide, 2.0) * 0.22)
    c = composite(c, np.array([0.75, 0.72, 0.62], np.float32), grime(ph, pw, seed=145, amount=0.15, edge=0.3) * 0.4)
    # place on the canvas with a slight rotation
    canvas = np.zeros((H, W, 3), np.float32)
    alpha = np.zeros((H, W), np.float32)
    canvas[PY0:PY1, PX0:PX1] = c
    alpha[PY0:PY1, PX0:PX1] = alpha_p
    rgba = np.concatenate([canvas, alpha[..., None]], -1)
    im = Image.fromarray((rgba * 255).astype(np.uint8), 'RGBA').rotate(-1.4, resample=Image.BICUBIC)
    rgba = np.asarray(im).astype(np.float32) / 255
    canvas, alpha = rgba[..., :3], rgba[..., 3]
    # clear tape strips across the top corners and the top centre
    for (tx, ty, ang) in ((70, 40, -35), (W - 70, 40, 35), (W / 2, 22, 2)):
        tape = Image.new('L', (W, H), 0)
        dtp = ImageDraw.Draw(tape)
        L, Wd = 130, 40
        a = math.radians(ang)
        pts = [(tx + math.cos(a) * dx - math.sin(a) * dy, ty + math.sin(a) * dx + math.cos(a) * dy)
               for dx, dy in ((-L / 2, -Wd / 2), (L / 2, -Wd / 2), (L / 2, Wd / 2), (-L / 2, Wd / 2))]
        dtp.polygon(pts, fill=255)
        tm_ = mask_from(tape)
        canvas = composite(canvas, np.array([0.93, 0.92, 0.85], np.float32), tm_ * 0.35)
        alpha = np.maximum(alpha, tm_ * 0.45)
        alpha = np.where(tm_ > 0.5, np.maximum(alpha, 0.45), alpha)
    save_png(canvas, os.path.join(OUT, 'Flyer_LostCat.png'), alpha)


# ------------------------------------------------------------------ police cordon notice (laminated, generic)
def police_notice():
    W, H = 1024, 724
    lam = rounded_rect_mask(W, H, 26)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    M = 18
    card = ((xx >= M) & (xx < W - M) & (yy >= M) & (yy < H - M)).astype(np.float32)
    yellow = np.array([0.98, 0.84, 0.06], np.float32)
    blk = np.array([0.05, 0.05, 0.05], np.float32)
    col = paper(H, W, base=tuple(yellow), seed=151, fiber=0.006, mottle=0.015)
    # hazard stripes border
    stripes = (((xx + yy) // 34) % 2 == 0).astype(np.float32)
    inner = ((xx >= M + 50) & (xx < W - M - 50) & (yy >= M + 50) & (yy < H - M - 50)).astype(np.float32)
    col = composite(col, blk, stripes * card * (1 - inner))
    col = composite(col, blk, tmask(W, H, [(W / 2, 250, 'POLICE LINE', 'barlow_blk', 150, None, 0.02, 'm'),
                                            (W / 2, 360, 'DO NOT CROSS', 'barlow_blk', 96, None, 0.04, 'm'),
                                            (W / 2, 440, 'Crime scene – access restricted to authorised personnel.', 'barlow_sb', 30, None, 0.0, 'm'),
                                            (W / 2, 482, 'Everyone entering must sign the scene log.', 'barlow_sb', 30, None, 0.0, 'm'),
                                            (130, 590, 'Scene log ref:', 'barlow_m', 30, None, 0.0, 'l')]))
    col = composite(col, blk, mask_draw(W, H, lambda d, s: d.line([(330 * s, 596 * s), (720 * s, 596 * s)], fill=255, width=2 * s)))
    col = composite(col, np.array([0.08, 0.08, 0.12], np.float32), tmask(W, H, [(350, 586, '14/10  JS-0217', 'marker', 40, None, 0.02, 'l')]) * 0.92)
    # lamination: clear margin, glare, punched holes
    margin = lam * (1 - card)
    col = composite(col, np.array([0.85, 0.86, 0.86], np.float32), margin)
    glare = np.exp(-(((xx * 0.8 + yy) - 700) / 90) ** 2) * 0.10 + np.exp(-(((xx * 0.8 + yy) - 860) / 25) ** 2) * 0.06
    col = np.clip(col + glare[..., None] * lam[..., None], 0, 1)
    alpha = lam * (0.45 + 0.55 * card)
    for hx in (60, W - 60):
        r = np.sqrt((xx - hx) ** 2 + (yy - 60) ** 2)
        alpha = alpha * np.clip(r - 11 + 0.5, 0, 1)
    col = composite(col, np.array([0.4, 0.38, 0.30], np.float32), grime(H, W, seed=152, amount=0.12, edge=0.2) * 0.3)
    save_png(col, os.path.join(OUT, 'Notice_PoliceLine.png'), alpha)


for fn in (street_posters, graffiti_tags, no_parking, newspaper, lost_cat, police_notice):
    fn()
    print(fn.__name__, flush=True)
print('FONTS', sorted(USED_FONTS))
