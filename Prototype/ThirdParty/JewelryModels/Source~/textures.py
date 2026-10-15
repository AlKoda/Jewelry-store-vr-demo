"""Original project-owned textures for the JewelryModels pack (Pillow + numpy).
Brand text uses the project's fictional shop name "Aurel & Finch" (see NightGraphics)."""
import os, numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
SERIF = '/usr/share/fonts/truetype/crosextra/Caladea-Regular.ttf'
SERIF_B = '/usr/share/fonts/truetype/crosextra/Caladea-Bold.ttf'
SANS = os.environ.get('BARLOW', os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Fonts', 'Barlow-Medium.ttf'))
SANS_SB = SANS.replace('Medium', 'SemiBold')
RNG = np.random.default_rng(20261014)

def font(path, size): return ImageFont.truetype(path, size)

def text_c(dr, xy, s, f, fill, spacing=0):
    if spacing:
        widths = [dr.textlength(ch, font=f) for ch in s]; total = sum(widths) + spacing * (len(s) - 1)
        x = xy[0] - total / 2
        for ch, w in zip(s, widths):
            dr.text((x, xy[1]), ch, font=f, fill=fill, anchor='lm'); x += w + spacing
    else:
        dr.text(xy, s, font=f, fill=fill, anchor='mm')

def noise(h, w, scale, octaves=4, seed=0):
    rng = np.random.default_rng(seed); out = np.zeros((h, w))
    amp = 1.0
    for o in range(octaves):
        sh = (max(2, int(h / scale * 2 ** o)), max(2, int(w / scale * 2 ** o)))
        g = rng.random(sh)
        im = Image.fromarray((g * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
        out += amp * (np.asarray(im) / 255.0 - 0.5); amp *= 0.5
    return out

def normal_from_height(hgt, strength=4.0):
    """OpenGL convention (green = +Y up), as Unity expects."""
    gy, gx = np.gradient(hgt)
    nx, ny, nz = -gx * strength, gy * strength, np.ones_like(hgt)
    n = np.stack([nx, ny, nz], -1); n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8))

def save(img, path, **kw):
    img.save(path, optimize=True, **kw); return os.path.basename(path)

# ---------------------------------------------------------------- watch dial
def watch_dial(path, S=1024, hour=10, minute=9, date='14'):
    y, x = np.mgrid[0:S, 0:S]; cx = cy = S / 2; dx, dy = (x - cx) / (S / 2), (y - cy) / (S / 2)
    r = np.hypot(dx, dy); th = np.arctan2(dy, dx)
    base = np.array([0.055, 0.095, 0.20])
    sun = 0.55 + 0.45 * np.cos(2 * (th - 0.6)) ** 2
    streak = noise(S, S, 3, 2, 3) * 0  # radial streaks below
    ang_noise = np.interp(th, np.linspace(-np.pi, np.pi, 2048), RNG.normal(0, 1, 2048))
    col = base[None, None, :] * (0.65 + 0.55 * sun[..., None] + 0.06 * ang_noise[..., None])
    col *= (1.0 - 0.25 * np.clip(r - 0.75, 0, 1)[..., None])
    img = Image.fromarray((np.clip(col, 0, 1) ** (1 / 2.2) * 255).astype(np.uint8))
    dr = ImageDraw.Draw(img)
    R = S / 2
    # minute track
    for k in range(60):
        a = np.radians(k * 6 - 90); r1, r2 = R * 0.93, R * (0.885 if k % 5 else 0.875)
        w = 3 if k % 5 else 4
        dr.line([(cx + r1 * np.cos(a), cy + r1 * np.sin(a)), (cx + r2 * np.cos(a), cy + r2 * np.sin(a))], fill=(225, 228, 235), width=w)
    # applied baton indices with a soft shadow
    shadow = Image.new('L', (S, S), 0); sd = ImageDraw.Draw(shadow)
    hl = Image.new('RGBA', (S, S), (0, 0, 0, 0)); hd = ImageDraw.Draw(hl)
    for k in range(12):
        if k == 3: continue  # date window at 3
        a = np.radians(k * 30 - 90); ca, sa = np.cos(a), np.sin(a)
        offs = (-0.022, 0.022) if k == 0 else (0.0,)
        for o in offs:
            px, py = -sa * o * R, ca * o * R
            r1, r2 = R * 0.84, R * 0.66
            pts = [(cx + px + r1 * ca, cy + py + r1 * sa), (cx + px + r2 * ca, cy + py + r2 * sa)]
            sd.line([(p[0] + 6, p[1] + 8) for p in pts], fill=150, width=22)
            hd.line(pts, fill=(205, 208, 214, 255), width=18)
            hd.line(pts, fill=(250, 250, 252, 255), width=6)
    shadow = shadow.filter(ImageFilter.GaussianBlur(6))
    img = Image.composite(Image.new('RGB', (S, S), (8, 14, 30)), img, shadow.point(lambda v: int(v * 0.6)))
    img.paste(hl, (0, 0), hl)
    dr = ImageDraw.Draw(img)
    # date window
    wx, wy = cx + R * 0.70, cy
    dr.rectangle([wx - 34, wy - 26, wx + 34, wy + 26], fill=(238, 238, 234), outline=(200, 202, 208), width=4)
    dr.text((wx, wy + 1), date, font=font(SANS_SB, 40), fill=(20, 20, 24), anchor='mm')
    text_c(dr, (cx, cy - R * 0.36), 'AUREL & FINCH', font(SERIF, 46), (236, 238, 242), spacing=4)
    text_c(dr, (cx, cy - R * 0.27), 'LONDON', font(SANS, 22), (200, 204, 212), spacing=6)
    text_c(dr, (cx, cy + R * 0.40), 'AUTOMATIC', font(SANS, 26), (215, 218, 226), spacing=7)
    text_c(dr, (cx, cy + R * 0.48), '25 JEWELS', font(SANS, 18), (170, 176, 190), spacing=5)
    # outside the dial disc: transparent black (texture is mapped to the disc only)
    mask = Image.fromarray(((r <= 1.0) * 255).astype(np.uint8))
    out = Image.new('RGB', (S, S), (10, 14, 24)); out.paste(img, (0, 0), mask)
    return save(out, path)

# ---------------------------------------------------------------- price tags atlas (4 x 2 cells)
TAGS = [('R-1042', '18K Diamond Solitaire', '£ 4,950'), ('R-2207', 'Platinum Eternity', '£ 3,200'),
        ('N-0318', 'Akoya Pearl Strand', '£ 1,850'), ('E-0731', 'Diamond Studs 1.0ct', '£ 2,600'),
        ('B-0907', 'Tennis Bracelet 3ct', '£ 7,400'), ('W-1210', 'Dress Watch 39mm', '£ 5,750'),
        ('R-3311', 'Sapphire Halo', '£ 3,900'), ('G-0001', 'Fine Gold 100 g', '£ 6,120')]

def price_tags(path, W=1024, H=512):
    img = Image.new('RGB', (W, H), (246, 244, 238)); dr = ImageDraw.Draw(img)
    cw, ch = W // 4, H // 2
    for i, (code, desc, price) in enumerate(TAGS):
        x0, y0 = (i % 4) * cw, (i // 4) * ch
        # paper grain
        dr.rectangle([x0 + 6, y0 + 6, x0 + cw - 7, y0 + ch - 7], outline=(176, 150, 96), width=2)
        dr.ellipse([x0 + 18, y0 + ch / 2 - 10, x0 + 38, y0 + ch / 2 + 10], fill=(214, 208, 196))  # string hole mark
        tx = x0 + cw / 2 + 14
        dr.text((tx, y0 + 40), 'AUREL & FINCH', font=font(SERIF, 22), fill=(120, 96, 52), anchor='mm')
        dr.line([(tx - 70, y0 + 58), (tx + 70, y0 + 58)], fill=(190, 166, 110), width=1)
        dr.text((tx, y0 + 98), price, font=font(SERIF_B, 46), fill=(28, 26, 24), anchor='mm')
        dr.text((tx, y0 + 150), desc, font=font(SANS, 19), fill=(60, 58, 56), anchor='mm')
        dr.text((tx, y0 + 186), code, font=font(SANS_SB, 20), fill=(90, 88, 84), anchor='mm')
        # barcode
        bx = tx - 70
        for k in range(46):
            w = RNG.integers(1, 4)
            if RNG.random() < 0.55: dr.rectangle([bx, y0 + 206, bx + w - 1, y0 + 232], fill=(30, 30, 30))
            bx += w + 1
    g = noise(H, W, 6, 3, 11)[..., None] * 10
    arr = np.clip(np.asarray(img).astype(float) + g, 0, 255).astype(np.uint8)
    return save(Image.fromarray(arr), path)

# ---------------------------------------------------------------- gold bar stamps (colour + normal)
def _stamp_height(W, H, lines, border=True, cast=False, seed=1):
    hm = Image.new('L', (W, H), 128); dr = ImageDraw.Draw(hm)
    if border:
        m = int(min(W, H) * 0.08)
        dr.rounded_rectangle([m, m, W - m, H - m], radius=m, outline=40 if cast else 200, width=max(3, W // 180))
    for (txt, fpath, size, yfrac, raised) in lines:
        f = font(fpath, int(size))
        val = 210 if raised else 40
        dr.text((W / 2, H * yfrac), txt, font=f, fill=val, anchor='mm')
    h = np.asarray(hm.filter(ImageFilter.GaussianBlur(1.6 if cast else 1.0))).astype(float) / 255.0 - 0.5
    if cast:
        h += noise(H, W, 14, 4, seed) * 0.35 + noise(H, W, 3, 2, seed + 1) * 0.08   # orange-peel cast surface
    else:
        h += noise(H, W, 40, 2, seed) * 0.02
    return h

def gold_bar(path_color, path_normal, kind='cast'):
    if kind == 'cast':
        W, H = 1024, 512
        lines = [('AUREL & FINCH', SERIF, 66, 0.25, False), ('ASSAYERS · LONDON', SANS, 34, 0.38, False),
                 ('1 KG', SERIF_B, 96, 0.56, False), ('FINE GOLD  999.9', SERIF, 58, 0.73, False), ('AF 041027', SANS_SB, 40, 0.86, False)]
        h = _stamp_height(W, H, lines, border=False, cast=True, seed=5)
    else:
        W, H = 512, 1024   # 100 g minted bar, portrait
        lines = [('AUREL', SERIF, 74, 0.17, True), ('& FINCH', SERIF, 74, 0.25, True), ('100 g', SERIF_B, 110, 0.45, True),
                 ('FINE GOLD', SANS_SB, 54, 0.60, True), ('999.9', SERIF, 74, 0.69, True), ('MELTER ASSAYER', SANS, 30, 0.80, True),
                 ('No. AF 2026 0614', SANS, 30, 0.86, True)]
        h = _stamp_height(W, H, lines, border=True, cast=False, seed=9)
    gold = np.array([1.00, 0.77, 0.34])
    gy_, gx_ = np.gradient(h); edge = np.clip(np.hypot(gx_, gy_) * (6 if kind == 'cast' else 10), 0, 1)
    if kind == 'cast':
        shade = 1.0 - 0.45 * np.clip(-h * 2.2, 0, 1) + 0.06 * noise(H, W, 30, 3, 7) - 0.35 * edge
    else:
        shade = 1.0 - 0.10 * np.clip(-h * 2.0, 0, 1) + 0.06 * np.clip(h * 2, 0, 1) - 0.55 * edge
    col = gold[None, None, :] * shade[..., None]
    Image.fromarray((np.clip(col, 0, 1) ** (1 / 2.2) * 255).astype(np.uint8)).save(path_color, optimize=True)
    normal_from_height(h, 9.0 if kind == 'cast' else 6.0).save(path_normal, optimize=True)

# ---------------------------------------------------------------- coin (left half obverse, right half reverse)
def coin(path_color, path_normal, S=512):
    W, H = 2 * S, S
    hm = Image.new('L', (W, H), 90); dr = ImageDraw.Draw(hm)
    for side in (0, 1):
        cx, cy, R = side * S + S / 2, S / 2, S / 2 - 6
        # beaded border
        for k in range(120):
            a = np.radians(k * 3); rr = R * 0.905
            dr.ellipse([cx + rr * np.cos(a) - 4, cy + rr * np.sin(a) - 4, cx + rr * np.cos(a) + 4, cy + rr * np.sin(a) + 4], fill=200)
        txt = 'FINE GOLD  ·  1 OZ  ·  999.9' if side == 0 else 'AUREL & FINCH  ·  LONDON'
        f = font(SERIF_B, 40)
        rr = R * 0.78
        widths = [ImageDraw.Draw(hm).textlength(c_, font=f) + 3 for c_ in txt]
        span = sum(widths) / rr                     # radians covered by the text
        acc = -np.pi / 2 - span / 2
        angles = []
        for w_ in widths:
            angles.append(acc + w_ / rr / 2); acc += w_ / rr
        for i, chh in enumerate(txt):
            a = angles[i]
            ch_img = Image.new('L', (60, 60), 0); ImageDraw.Draw(ch_img).text((30, 30), chh, font=f, fill=255, anchor='mm')
            ch_img = ch_img.rotate(-np.degrees(a) - 90, resample=Image.BICUBIC)
            px, py = int(cx + rr * np.cos(a) - 30), int(cy + rr * np.sin(a) - 30)
            hm.paste(Image.new('L', (60, 60), 205), (px, py), ch_img)
        if side == 0:
            # compass-rose sunburst motif
            for k in range(32):
                a = np.radians(k * 360 / 32); long = k % 2 == 0
                r1 = R * (0.62 if long else 0.48); w = R * (0.07 if long else 0.045)
                p0 = (cx + r1 * np.cos(a), cy + r1 * np.sin(a))
                pl = (cx + w * np.cos(a + np.pi / 2), cy + w * np.sin(a + np.pi / 2))
                pr = (cx + w * np.cos(a - np.pi / 2), cy + w * np.sin(a - np.pi / 2))
                dr.polygon([p0, pl, pr], fill=175 if long else 150)
            dr.ellipse([cx - R * 0.12, cy - R * 0.12, cx + R * 0.12, cy + R * 0.12], fill=215)
        else:
            # laurel wreath with year
            for sgn in (-1, 1):
                for k in range(11):
                    a = np.radians(110 + k * 14) if sgn > 0 else np.radians(70 - k * 14)
                    rr = R * 0.56; lx, ly = cx + rr * np.cos(a), cy + rr * np.sin(a)
                    leaf = Image.new('L', (80, 80), 0); ImageDraw.Draw(leaf).ellipse([28, 8, 52, 72], fill=255)
                    leaf = leaf.rotate(-np.degrees(a) + (35 * sgn), resample=Image.BICUBIC)
                    hm.paste(Image.new('L', (80, 80), 180), (int(lx - 40), int(ly - 40)), leaf)
            dr.text((cx, cy - R * 0.08), '2026', font=font(SERIF_B, 92), fill=210, anchor='mm')
            dr.text((cx, cy + R * 0.20), '1 TROY OUNCE', font=font(SANS_SB, 30), fill=190, anchor='mm')
    h = np.asarray(hm.filter(ImageFilter.GaussianBlur(1.4))).astype(float) / 255 - 0.35
    h += noise(H, W, 60, 2, 21) * 0.02
    gold = np.array([1.00, 0.77, 0.34])
    gy_, gx_ = np.gradient(h); edge = np.clip(np.hypot(gx_, gy_) * 8, 0, 1)
    shade = 0.80 + 0.26 * np.clip(h * 2.5, 0, 1) - 0.40 * edge   # frosted relief, mirror field, dark edges
    col = gold[None, None] * shade[..., None]
    Image.fromarray((np.clip(col, 0, 1) ** (1 / 2.2) * 255).astype(np.uint8)).save(path_color, optimize=True)
    normal_from_height(h, 7.0).save(path_normal, optimize=True)

# ---------------------------------------------------------------- gift bag (panel atlas)
def gift_bag(path, W=1024, H=1024):
    base = np.array([0.025, 0.025, 0.028])
    arr = np.ones((H, W, 3)) * base + noise(H, W, 8, 3, 31)[..., None] * 0.012
    img = Image.fromarray((np.clip(arr, 0, 1) ** (1 / 2.2) * 255).astype(np.uint8))
    dr = ImageDraw.Draw(img)
    pw = int(W * 0.75)
    gold = (196, 160, 92)
    dr.rectangle([24, 24, pw - 24, H - 24], outline=gold, width=3)
    text_c(dr, (pw / 2, H * 0.47), 'AUREL', font(SERIF, 112), gold, spacing=10)
    text_c(dr, (pw / 2, H * 0.57), '& FINCH', font(SERIF, 72), gold, spacing=8)
    text_c(dr, (pw / 2, H * 0.66), 'FINE JEWELLERY · LONDON', font(SANS, 26), (170, 140, 84), spacing=5)
    return save(img, path)

# ---------------------------------------------------------------- card terminal top (screen + keypad)
def terminal_top(path, W=512, H=1024):
    img = Image.new('RGB', (W, H), (32, 33, 36)); dr = ImageDraw.Draw(img)
    # screen
    sx0, sy0, sx1, sy1 = 46, 70, W - 46, 430
    dr.rounded_rectangle([sx0 - 10, sy0 - 10, sx1 + 10, sy1 + 10], radius=14, fill=(10, 10, 12))
    scr = Image.new('RGB', (sx1 - sx0, sy1 - sy0), (236, 240, 244)); sd = ImageDraw.Draw(scr)
    sd.rectangle([0, 0, scr.size[0], 46], fill=(28, 52, 92))
    sd.text((14, 23), 'AUREL & FINCH', font=font(SERIF, 24), fill=(235, 238, 242), anchor='lm')
    sd.text((scr.size[0] - 14, 23), '22:47', font=font(SANS, 22), fill=(200, 210, 225), anchor='rm')
    sd.text((scr.size[0] / 2, 110), 'AMOUNT', font=font(SANS, 26), fill=(90, 94, 100), anchor='mm')
    sd.text((scr.size[0] / 2, 165), '£ 4,950.00', font=font(SANS_SB, 58), fill=(20, 22, 26), anchor='mm')
    sd.text((scr.size[0] / 2, 245), 'Insert, tap or swipe card', font=font(SANS, 26), fill=(40, 44, 50), anchor='mm')
    for k in range(4):  # contactless arcs
        r = 14 + k * 9
        sd.arc([scr.size[0] / 2 - r, 300 - r, scr.size[0] / 2 + r, 300 + r], -45, 45, fill=(40, 90, 160), width=4)
    img.paste(scr, (sx0, sy0))
    # keypad 3 x 4 + function row
    kx0, ky0, kw, kh, gap = 60, 500, 118, 88, 22
    labels = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '*', '0', '#']
    for i, lab in enumerate(labels):
        x = kx0 + (i % 3) * (kw + gap); y = ky0 + (i // 3) * (kh + gap)
        dr.rounded_rectangle([x + 3, y + 5, x + kw + 3, y + kh + 5], radius=16, fill=(14, 14, 16))
        dr.rounded_rectangle([x, y, x + kw, y + kh], radius=16, fill=(64, 66, 70))
        dr.text((x + kw / 2, y + kh / 2), lab, font=font(SANS_SB, 44), fill=(236, 236, 236), anchor='mm')
    y = ky0 + 4 * (kh + gap)
    for j, (c, sym) in enumerate((((190, 40, 40), 'X'), ((220, 176, 40), '<'), ((40, 150, 70), 'O'))):
        x = kx0 + j * (kw + gap)
        dr.rounded_rectangle([x + 3, y + 5, x + kw + 3, y + kh + 5], radius=16, fill=(14, 14, 16))
        dr.rounded_rectangle([x, y, x + kw, y + kh], radius=16, fill=c)
        dr.text((x + kw / 2, y + kh / 2), sym, font=font(SANS_SB, 44), fill=(250, 250, 250), anchor='mm')
    return save(img, path)

# ---------------------------------------------------------------- POS tablet screen
def pos_screen(path, W=1024, H=683):
    img = Image.new('RGB', (W, H), (244, 245, 247)); dr = ImageDraw.Draw(img)
    dr.rectangle([0, 0, W, 64], fill=(22, 30, 46))
    dr.text((24, 32), 'AUREL & FINCH  ·  Till 1', font=font(SANS_SB, 28), fill=(230, 234, 240), anchor='lm')
    dr.text((W - 24, 32), '14 Oct 2026  17:58', font=font(SANS, 24), fill=(170, 180, 196), anchor='rm')
    items = [('18K Diamond Solitaire  R-1042', '4,950.00'), ('Gift wrapping', '0.00'), ('Engraving (6 letters)', '45.00')]
    y = 100
    dr.text((30, y), 'Current sale', font=font(SANS_SB, 30), fill=(30, 34, 40)); y += 56
    for name, pr in items:
        dr.text((30, y), name, font=font(SANS, 26), fill=(50, 54, 60)); dr.text((W * 0.58, y), '£ ' + pr, font=font(SANS, 26), fill=(50, 54, 60), anchor='ra')
        y += 46; dr.line([(30, y - 8), (W * 0.58, y - 8)], fill=(220, 222, 226))
    dr.text((30, y + 30), 'Total', font=font(SANS_SB, 34), fill=(20, 22, 26)); dr.text((W * 0.58, y + 30), '£ 4,995.00', font=font(SANS_SB, 34), fill=(20, 22, 26), anchor='ra')
    # tile buttons on the right
    tiles = ['Rings', 'Necklaces', 'Earrings', 'Watches', 'Bracelets', 'Repairs', 'Discount', 'Customer']
    for i, t in enumerate(tiles):
        x = W * 0.63 + (i % 2) * 180; yy = 100 + (i // 2) * 110
        dr.rounded_rectangle([x, yy, x + 168, yy + 96], radius=12, fill=(232, 236, 242), outline=(200, 206, 216))
        dr.text((x + 84, yy + 48), t, font=font(SANS, 24), fill=(40, 46, 56), anchor='mm')
    dr.rounded_rectangle([W * 0.63, H - 140, W - 30, H - 40], radius=14, fill=(38, 120, 72))
    dr.text(((W * 0.63 + W - 30) / 2, H - 90), 'PAY  £ 4,995.00', font=font(SANS_SB, 34), fill=(255, 255, 255), anchor='mm')
    return save(img.resize((1024, 1024), Image.LANCZOS), path)   # stored square (power of two); the UVs restore the 3:2 aspect

# ---------------------------------------------------------------- diamond paper print (inner blue liner + outer)
def gem_paper(path, W=1024, H=512):
    img = Image.new('RGB', (W, H), (236, 238, 240)); dr = ImageDraw.Draw(img)
    # left half: outside (white with print), right half: inside (blue liner)
    dr.rectangle([W // 2, 0, W, H], fill=(164, 196, 226))
    dr.text((W * 0.25, 90), 'AUREL & FINCH', font=font(SERIF, 40), fill=(52, 70, 120), anchor='mm')
    for i, lab in enumerate(['No.', 'Carats', 'Pcs', 'Colour', 'Clarity']):
        y = 170 + i * 58
        dr.text((60, y), lab, font=font(SANS, 30), fill=(52, 70, 120), anchor='lm')
        dr.line([(200, y + 14), (W * 0.5 - 60, y + 14)], fill=(52, 70, 120), width=2)
    for (txt, y) in (('A-1173', 170), ('3.42', 228), ('12', 286), ('F-G', 344), ('VS1', 402)):
        dr.text((230, y - 4), txt, font=font(SERIF, 34), fill=(30, 30, 60), anchor='lm')
    arr = np.asarray(img).astype(float) + noise(H, W, 10, 3, 41)[..., None] * 8
    return save(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)), path)

def velvet(path, rgb, S=256, seed=0):
    """tileable fine velvet colour variation (multiplied with the material colour in Unity: keep near white)"""
    n = noise(S, S, 4, 4, seed) * 0.18 + noise(S, S, 1.2, 2, seed + 1) * 0.10
    n = (n + np.roll(n, S // 2, 0) + np.roll(n, S // 2, 1) + np.roll(np.roll(n, S // 2, 0), S // 2, 1)) / 4  # soften seams
    col = np.array(rgb)[None, None] * (1 + n[..., None])
    return save(Image.fromarray((np.clip(col, 0, 1) ** (1 / 2.2) * 255).astype(np.uint8)), path)
