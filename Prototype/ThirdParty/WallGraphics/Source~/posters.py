# Aurel & Finch posters, lightbox panel and window vinyl (project-original artwork).
import sys, os
import numpy as np
from PIL import Image, ImageDraw
from gfx import *
from post import finish

R = sys.argv[1]          # renders dir
OUT = sys.argv[2]        # pack Brand dir

GOLD_MUTED = (0.74, 0.62, 0.42)
IVORY = (0.91, 0.87, 0.79)


def load(name, glint):
    img = np.load(f'{R}/{name}_lin.npy').astype(np.float32)
    al = np.load(f'{R}/{name}_alpha.npy').astype(np.float32)
    disp, a = finish(img, al, glint=glint)
    return disp.astype(np.float32), a.astype(np.float32)


def background(h, w, cx=0.5, cy=0.35, glow=(0.16, 0.12, 0.085), base=(0.035, 0.030, 0.026), seed=2, rad=0.55):
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    dx = (x / w - cx) * (w / h)
    dy = (y / h - cy)
    r = np.sqrt(dx * dx + dy * dy) / rad
    g = np.exp(-r * r * 1.6)[..., None]
    bg = np.array(base, np.float32) + (np.array(glow, np.float32) - np.array(base, np.float32)) * g
    n = fbm(h, w, scale=max(h, w) / 4, octaves=4, seed=seed) - 0.5
    bg = bg * (1 + 0.10 * n[..., None])
    bg *= vignette(h, w, 0.35, 2.0)[..., None]
    return bg


def put_gold(canvas, mask, seed=0, light=1.0):
    h, w = mask.shape
    ys, xs = np.nonzero(mask > 0.01)
    if ys.size == 0:
        return canvas
    y0, y1 = ys.min(), ys.max() + 1
    gf = np.zeros((h, w, 3), np.float32)
    # gradient per text block so every line gets the full sheen
    gf[y0:y1] = gold_fill(y1 - y0, w, seed=seed, light=light)
    return composite(canvas, gf, mask)


def tmask(size, xy, text, f, tracking=0.0, anchor='m'):
    return mask_from(text_mask(size, xy, text, f, tracking, anchor))


def line_mask(size, x0, x1, y, thick=1.5):
    W, H = size
    ss = 4
    im = Image.new('L', (W * ss, H * ss), 0)
    ImageDraw.Draw(im).rectangle((x0 * ss, (y - thick / 2) * ss, x1 * ss, (y + thick / 2) * ss), fill=255)
    return mask_from(im.resize((W, H), Image.LANCZOS))


def monogram(size, cx, cy, r):
    W, H = size
    ss = 4
    im = Image.new('L', (W * ss, H * ss), 0)
    d = ImageDraw.Draw(im)
    d.ellipse(((cx - r) * ss, (cy - r) * ss, (cx + r) * ss, (cy + r) * ss), outline=255, width=int(1.6 * ss))
    d.ellipse(((cx - r + 6) * ss, (cy - r + 6) * ss, (cx + r - 6) * ss, (cy + r - 6) * ss), outline=255, width=int(0.8 * ss))
    f = font('cormorant_i', r * 1.05 * ss, 500)
    d.text((cx * ss, (cy + r * 0.05) * ss), 'A&F', font=f, fill=255, anchor='mm')
    return mask_from(im.resize((W, H), Image.LANCZOS))


def brand_header(c, W, y, scale=1.0):
    m = monogram((W, c.shape[0]), W / 2, y, 34 * scale)
    c = put_gold(c, m, seed=11)
    f = font('cinzel', 30 * scale, 500)
    m = tmask((W, c.shape[0]), (W / 2, y + 78 * scale), 'AUREL & FINCH', f, 0.32)
    c = put_gold(c, m, seed=12)
    return c


def fade_rows(disp, a, y0, y1):
    H = disp.shape[0]
    y = np.arange(H, dtype=np.float32)[:, None] / H
    f = 1 - smooth(y0, y1, y)
    return disp * f[..., None], a * f


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def poster(render, glint, title1, title2, sub, small, out, seed, glow_cy=0.33, text_top=1040, fade=(0.62, 0.68)):
    W, H = 1024, 1536
    disp, a = load(render, glint)
    disp, a = fade_rows(disp, a, *fade)
    c = background(H, W, cy=glow_cy, seed=seed)
    c = disp + c * (1 - a[..., None])
    c = brand_header(c, W, 92)
    size = (W, H)
    # headline
    f1 = font('cormorant', 104, 400)
    f2 = font('cormorant_i', 104, 400)
    y = text_top
    m = tmask(size, (W / 2, y), title1, f1, 0.01)
    if title2:
        m = np.maximum(m, tmask(size, (W / 2, y + 98), title2, f2, 0.0))
        y += 98
    c = put_gold(c, m, seed=seed + 1)
    # sub line
    fs = font('cormorant_i', 40, 500)
    y += 78
    c = composite(c, IVORY, tmask(size, (W / 2, y), sub, fs, 0.01) * 0.92)
    # rule + footer
    y += 52
    c = put_gold(c, line_mask(size, W / 2 - 46, W / 2 + 46, y, 1.4), seed=3)
    ff = font('tenor', 19)
    y += 50
    c = composite(c, GOLD_MUTED, tmask(size, (W / 2, y), 'FINE JEWELLERY  ·  EST. 1962', ff, 0.30))
    fsm = font('barlow_l', 15)
    c = composite(c, (0.55, 0.51, 0.45), tmask(size, (W / 2, H - 40), small, fsm, 0.08))
    c = grain(c, 0.010, seed=seed)
    save_jpg(c, os.path.join(OUT, out), 90)
    print(out)


poster('solitaire', 1.0, 'The Solitaire', 'Collection', 'One stone. One promise.',
       '18ct yellow gold  ·  round brilliant diamond  ·  from £2,450', 'Poster_SolitaireCollection.jpg', 21)
poster('pearls', 0.0, 'Timeless', 'since 1962', 'Hand-knotted pearl strands from the Atelier.',
       'Cultured pearls, 7 mm  ·  18ct gold clasp  ·  restringing while you wait', 'Poster_TimelessSince1962.jpg', 31,
       glow_cy=0.30)
poster('bands', 0.5, 'Forever,', 'engraved.', 'Wedding bands, hand-finished in our workshop.',
       'Complimentary engraving on every wedding band  ·  ask in store', 'Poster_ForeverEngraved.jpg', 41)
poster('pendant', 0.9, 'Midnight', 'Sapphire', 'The Evening Collection.',
       'Sapphire and diamond halo pendant  ·  18ct white gold', 'Poster_MidnightSapphire.jpg', 51, glow_cy=0.36)


# ------------------------------------------------------------------ wide lightbox 2:1
def lightbox_wide():
    W, H = 2048, 1024
    disp, a = load('solitaire_wide', 1.0)
    c = background(H, W, cx=0.70, cy=0.45, rad=0.45, seed=61)
    c = disp + c * (1 - a[..., None])
    size = (W, H)
    x = 190
    m = monogram(size, x + 44, 250, 44)
    c = put_gold(c, m, seed=62)
    f = font('cinzel', 104, 500)
    m = tmask(size, (x, 450), 'AUREL & FINCH', f, 0.12, 'l')
    c = put_gold(c, m, seed=63)
    fs = font('cormorant_i', 54, 500)
    c = composite(c, IVORY, tmask(size, (x + 4, 540), 'Fine jewellery since 1962', fs, 0.0, 'l') * 0.93)
    c = put_gold(c, line_mask(size, x + 4, x + 120, 600, 2.0), seed=64)
    ff = font('tenor', 27)
    c = composite(c, GOLD_MUTED, tmask(size, (x + 4, 670), 'BESPOKE  ·  BRIDAL  ·  PEARLS  ·  WATCHES  ·  REPAIRS', ff, 0.22, 'l'))
    fsm = font('barlow_l', 22)
    c = composite(c, (0.58, 0.54, 0.47), tmask(size, (x + 4, 880), 'Private appointments available  ·  Valuations for insurance', fsm, 0.06, 'l'))
    c = grain(c, 0.009, seed=65)
    save_jpg(c, os.path.join(OUT, 'Lightbox_AurelFinchWide.jpg'), 90)
    print('Lightbox_AurelFinchWide.jpg')


lightbox_wide()


# ------------------------------------------------------------------ window vinyl (gilded glass sign)
def window_vinyl():
    W, H = 2048, 512
    size = (W, H)
    f = font('cinzel', 176, 600)
    m_main = tmask(size, (W / 2, 250), 'AUREL & FINCH', f, 0.10)
    f2 = font('cinzel', 54, 500)
    m_sub = tmask(size, (W / 2, 372), 'FINE JEWELLERY  ·  EST. 1962', f2, 0.22)
    tw = text_width('FINE JEWELLERY  \u00b7  EST. 1962', f2, 0.22)
    xl = W / 2 - tw / 2 - 36
    xr = W / 2 + tw / 2 + 36
    rule = np.maximum(line_mask(size, xl - 230, xl, 352, 3), line_mask(size, xr, xr + 230, 352, 3))
    m_sub = np.maximum(m_sub, rule)
    f3 = font('cormorant_i', 46, 500)
    m_s3 = tmask(size, (W / 2, 456), 'Bespoke · Bridal · Watches · Repairs', f3, 0.02)
    gold = np.maximum(np.maximum(m_main, m_sub), m_s3)
    # black keyline outline + shadow, typical of gilded shop glass
    disk = np.hypot(*np.mgrid[-2:3, -2:3]) <= 2.2
    outline = np.clip(ndimage.grey_dilation(gold, footprint=disk), 0, 1)
    outline = np.maximum(outline, gold)
    shadow = np.clip(np.roll(np.roll(ndimage.grey_dilation(m_main, footprint=disk), 3, 0), 3, 1), 0, 1)
    alpha = np.clip(np.maximum(outline, shadow * 0.75), 0, 1)
    col = np.zeros((H, W, 3), np.float32) + np.array([0.04, 0.035, 0.03], np.float32)
    # gold leaf: burnished streaks + gradient
    gl = gold_fill(H, W, seed=71, light=1.05)
    streak = fbm(H, W, scale=40, octaves=3, seed=72)
    streak = ndimage.gaussian_filter(streak, (0.5, 18))
    gl = gl * (0.88 + 0.24 * streak[..., None])
    sh = emboss(gold, 1.5)
    gl = np.clip(gl + sh[..., None] * 1.2, 0, 1)
    col = composite(col, gl, gold)
    # tiny wear: missing flecks in the leaf
    rng = np.random.default_rng(73)
    fleck = (ndimage.gaussian_filter(rng.random((H, W)).astype(np.float32), 1.0) > 0.535).astype(np.float32) * 0.6
    alpha = np.clip(alpha - fleck * gold * 0.7, 0, 1)
    save_png(col, os.path.join(OUT, 'WindowVinyl_AurelFinch.png'), alpha)
    print('WindowVinyl_AurelFinch.png')


window_vinyl()
print('FONTS', sorted(USED_FONTS))
