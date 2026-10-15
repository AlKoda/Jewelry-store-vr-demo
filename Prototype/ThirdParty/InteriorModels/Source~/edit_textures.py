"""Project edits of Poly Haven source textures (InteriorModels pack). Copies each model folder that needs
edits to WORK/src/<id>/ and edits the 2K textures there:
  wall_clock         dial maker-style wordmark removed (patch copied from clean dial area)
  fire_alarm         maker-style wordmark on the lower plate removed (colour + normal)
  korean_fire_extinguisher_01  Korean stencil on the base plaque and the "UMS" hose print removed from the
                     colour AND normal maps; product label replaced by an original English label; Korean inspection
                     tag (with handwritten names) replaced by an original English inspection tag.
edit_textures.py SRC_MODELS_ROOT WORK_DIR"""
import sys, os, shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

SRC, WORK = sys.argv[1], sys.argv[2]
FONT = '/usr/share/fonts/truetype/dejavu/'
def font(name, size): return ImageFont.truetype(name if name.startswith('/') else FONT + name, size)

def copy_model(mid):
    """Copy SRC/<mid> to WORK/src/<mid>; returns None (and the edits of that model are skipped) when SRC lacks it."""
    if not os.path.isdir(os.path.join(SRC, mid)):
        print('skip', mid, '(not in', SRC + ')'); return None
    dst = os.path.join(WORK, 'src', mid)
    if os.path.exists(dst): shutil.rmtree(dst)
    shutil.copytree(os.path.join(SRC, mid), dst, ignore=shutil.ignore_patterns('*.import'))
    return dst

def load(p): return np.asarray(Image.open(p).convert('RGB')).astype(np.float32)
def save(p, a): Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(p, quality=95)

def inpaint(img, mask, iters=60):
    """Fill masked pixels by repeated normalized blurring of the unmasked neighbourhood, then add back
    fine grain taken from a nearby unmasked offset so the patch is not smooth."""
    out = img.copy(); m = mask.astype(np.float32)
    known = 1 - m
    acc = out * known[..., None]; w = known.copy()
    for r in (2, 4, 8, 16, 32):
        a = np.asarray(Image.fromarray(np.clip(acc, 0, 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(r))).astype(np.float32)
        ww = np.asarray(Image.fromarray((w * 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(r))).astype(np.float32) / 255
        est = a / np.maximum(ww[..., None], 1e-3) * (np.asarray(Image.fromarray((known*255).astype(np.uint8)).filter(ImageFilter.BoxBlur(r))).astype(np.float32)[..., None] / 255 / np.maximum(ww[..., None], 1e-3))
        est = a / np.maximum(ww[..., None], 1e-3)
        fill = (m > 0) & (ww > 0.02)
        out[fill] = est[fill]; acc[fill] = est[fill]; w[fill] = 1; known[fill] = 1; m[fill] = 0
        if not (m > 0).any(): break
    return out

def grain_from(img, region_src, shape):
    y0, x0 = region_src
    patch = img[y0:y0 + shape[0], x0:x0 + shape[1]]
    blur = np.asarray(Image.fromarray(np.clip(patch, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(6))).astype(np.float32)
    return patch - blur

def feather(h, w, r):
    m = np.ones((h, w), np.float32)
    ramp = np.minimum(np.minimum(np.arange(w)[None, :], np.arange(w)[::-1][None, :]), np.minimum(np.arange(h)[:, None], np.arange(h)[::-1][:, None]))
    return np.clip(ramp / r, 0, 1)

# ------------------------------------------------------------------ wall clock
d = copy_model('wall_clock')
if d:
    p = os.path.join(d, 'textures', 'wall_clock_diff_2k.jpg'); a = load(p)
    y0, y1, x0, x1 = 1232, 1322, 500, 620
    src = a[y0 + 150:y1 + 150, x0:x1].copy()
    f = feather(y1 - y0, x1 - x0, 10)[..., None]
    a[y0:y1, x0:x1] = a[y0:y1, x0:x1] * (1 - f) + src * f
    save(p, a)

# ------------------------------------------------------------------ fire alarm call point
d = copy_model('fire_alarm')
if d:
    p = os.path.join(d, 'textures', 'fire_alarm_diff_2k.jpg'); a = load(p)
    y0, y1, x0, x1 = 515, 935, 1716, 1810
    reg = a[y0:y1, x0:x1]
    red = (reg[..., 0] - reg[..., 1] > 25)
    red = np.asarray(Image.fromarray((red * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))) > 0
    full = np.zeros(a.shape[:2], bool); full[y0:y1, x0:x1] = red
    a = inpaint(a, full)
    # regrain with the plate's own texture from its lower clean part
    g = grain_from(load(p), (y1 - 0, x0), (1, 1))
    save(p, a)
    pn = os.path.join(d, 'textures', 'fire_alarm_nor_gl_2k.jpg'); n = load(pn)
    sub = n[y0 + 6:y1 - 6, x0 + 6:x1 - 6]
    flat = np.asarray(Image.fromarray(sub.astype(np.uint8)).filter(ImageFilter.GaussianBlur(9))).astype(np.float32)
    n[y0 + 6:y1 - 6, x0 + 6:x1 - 6] = flat
    save(pn, n)

# ------------------------------------------------------------------ fire extinguisher
d = copy_model('korean_fire_extinguisher_01')
if d:
    p = os.path.join(d, 'textures', 'korean_fire_extinguisher_01_body_diff_2k.jpg'); a = load(p)
    # 1) stencil on the base plaque: replace the band between the two rule lines with plain painted metal
    #    copied from the clean red body panel below (feathered), so no glyph ghost remains
    y0, y1, x0, x1 = 80, 206, 34, 650
    src = a[760:760 + (y1 - y0), 200:200 + (x1 - x0)].copy()
    grain = src - np.asarray(Image.fromarray(np.clip(src, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(5))).astype(np.float32)
    top = a[y0:y0 + 6, x0:x1].mean(0); bot = a[y1 - 6:y1, x0:x1].mean(0)
    top = np.asarray(Image.fromarray(np.clip(top[None], 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(12))).astype(np.float32)[0]
    bot = np.asarray(Image.fromarray(np.clip(bot[None], 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(12))).astype(np.float32)[0]
    t = np.linspace(0, 1, y1 - y0)[:, None, None]
    src = top[None] * (1 - t) + bot[None] * t + grain * 0.45
    f = feather(y1 - y0, x1 - x0, 8)[..., None]
    a[y0:y1, x0:x1] = a[y0:y1, x0:x1] * (1 - f) + src * f
    # 2) print on the black hose ("UMS", grey glyphs at x 1915-1965, y 213-303 in the 2K maps): the hose runs along v,
    #    so every column of the box is rebuilt as a blend from the clean rows just above the box to the clean rows just
    #    below it, plus fine grain taken ONLY from glyph-free stretches of the same columns (the same is done in the
    #    normal map further down, where the print is embossed)
    HOSE = (198, 314, 1900, 1980)            # y0, y1, x0, x1 (2K)
    HOSE_CLEAN = [(120, 196), (316, 370)]   # glyph-free rows of the same hose (130 rows)
    def column_fill(arr, box, clean_rows, grain_k=0.8, fr=6):
        y0, y1, x0, x1 = box; h = y1 - y0
        top = np.median(arr[y0 - 16:y0 - 2, x0:x1], 0); bot = np.median(arr[y1 + 2:y1 + 16, x0:x1], 0)
        t = np.linspace(0, 1, h)[:, None, None]
        clean = np.concatenate([arr[r0:r1, x0:x1] for r0, r1 in clean_rows], 0)
        g = clean - np.median(clean, 0, keepdims=True)
        g = g[:h] if len(g) >= h else np.concatenate([g] * (h // len(g) + 1), 0)[:h]
        new = top[None] * (1 - t) + bot[None] * t + grain_k * g
        f = feather(h, x1 - x0, fr)[..., None]
        arr[y0:y1, x0:x1] = arr[y0:y1, x0:x1] * (1 - f) + new * f
        return arr
    a = column_fill(a, HOSE, HOSE_CLEAN)
    # 3) product label -> original English label, same placement
    y0, y1, x0, x1 = 1198, 1378, 388, 866
    W, H = x1 - x0, y1 - y0
    lab = Image.new('RGB', (W * 2, H * 2), (238, 236, 228)); dr = ImageDraw.Draw(lab)
    S2 = 2
    dr.rectangle([0, 0, W * 2 - 1, 58], fill=(196, 30, 36))
    dr.text((W, 30), 'FIRE EXTINGUISHER', font=font('DejaVuSans-Bold.ttf', 40), fill=(255, 255, 255), anchor='mm')
    dr.text((W, 92), 'ABC DRY POWDER  3 kg', font=font('DejaVuSans-Bold.ttf', 34), fill=(20, 32, 90), anchor='mm')
    # class pictogram boxes
    for i, (c, col) in enumerate((('A', (30, 120, 60)), ('B', (200, 40, 40)), ('C', (30, 70, 170)))):
        bx = 40 + i * 92; dr.rectangle([bx, 128, bx + 78, 206], outline=col, width=5)
        dr.text((bx + 39, 167), c, font=font('DejaVuSans-Bold.ttf', 50), fill=col, anchor='mm')
    steps = ['1  PULL THE PIN', '2  AIM AT THE BASE', '3  SQUEEZE THE LEVER', '4  SWEEP SIDE TO SIDE']
    for i, s in enumerate(steps):
        dr.text((330, 132 + i * 34), s, font=font('DejaVuSans-Bold.ttf', 24), fill=(25, 25, 25))
    for i in range(5):
        dr.line([40, 240 + i * 18, W * 2 - 40, 240 + i * 18], fill=(150, 150, 150), width=4)
    dr.text((40, 336), 'Service pressure 1.4 MPa   Test 2.5 MPa   Recharge after any use', font=font('DejaVuSans.ttf', 20), fill=(60, 60, 60))
    lab = lab.resize((W, H), Image.LANCZOS)
    la = np.asarray(lab).astype(np.float32)
    # keep the photographed wear: multiply by the source label's low-frequency luminance relative to its mean
    src = a[y0:y1, x0:x1]; lumi = src.mean(-1, keepdims=True)
    wear = np.asarray(Image.fromarray(np.clip(lumi[..., 0], 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(4))).astype(np.float32)[..., None]
    wear = np.clip(wear / max(np.percentile(wear, 90), 1), 0.75, 1.05)
    f = feather(H, W, 3)[..., None]
    a[y0:y1, x0:x1] = src * (1 - f) + la * wear * f
    save(p, a)
    # the same edits in the normal map: (1) the stencil plaque carries the glyphs as embossed relief -> flat painted
    # metal: the plaque's own median normal plus the fine relief of the clean body panel used for the colour patch;
    # (2) the embossed hose print -> column rebuild as in the colour map
    pn = os.path.join(d, 'textures', 'korean_fire_extinguisher_01_body_nor_gl_2k.jpg'); n = load(pn)
    y0, y1, x0, x1 = 80, 206, 34, 650
    src = n[760:760 + (y1 - y0), 200:200 + (x1 - x0)].copy()
    hp = src - np.asarray(Image.fromarray(np.clip(src, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(5))).astype(np.float32)
    base = np.median(np.concatenate([n[y0:y0 + 4, x0:x1], n[y1 - 4:y1, x0:x1]], 0).reshape(-1, 3), 0)
    f = feather(y1 - y0, x1 - x0, 8)[..., None]
    n[y0:y1, x0:x1] = n[y0:y1, x0:x1] * (1 - f) + (base[None, None] + hp * 0.6) * f
    n = column_fill(n, HOSE, HOSE_CLEAN, grain_k=0.5)
    save(pn, n)

    # 4) inspection tag: mirror the clean reverse side over the front and draw an English record
    p = os.path.join(d, 'textures', 'korean_fire_extinguisher_01_paper_diff_2k.jpg'); t = Image.open(p).convert('RGB')
    back = t.crop((1039, 705, 1982, 1998)).transpose(Image.FLIP_LEFT_RIGHT)
    fx0, fy0 = 32, 37
    front_mask = Image.new('L', t.size, 0)
    ta = np.asarray(t).astype(np.float32)
    region = ta[fy0:fy0 + back.size[1], fx0:fx0 + back.size[0]]
    inside = (region.mean(-1) > 40)
    ba = np.asarray(back).astype(np.float32)
    ok = (ba.mean(-1) > 40)
    region[ok] = ba[ok]
    ta[fy0:fy0 + back.size[1], fx0:fx0 + back.size[0]] = region
    tag = Image.fromarray(ta.astype(np.uint8)); dr = ImageDraw.Draw(tag)
    red = (205, 52, 50); ink = (35, 35, 40)
    X0, Y0, X1, Y1 = 112, 122, 886, 1262
    dr.rectangle([X0, Y0, X1, Y1], outline=red, width=18)
    dr.rectangle([X0, Y0, X1, Y0 + 150], fill=red)
    dr.text(((X0 + X1) // 2, Y0 + 75), 'INSPECTION RECORD', font=font('DejaVuSans-Bold.ttf', 56), fill=(250, 246, 240), anchor='mm')
    gx0, gx1 = X0 + 28, X1 - 28
    rows_y = [Y0 + 180, Y0 + 280, Y0 + 380]
    dr.line([gx0, rows_y[0], gx1, rows_y[0]], fill=ink, width=4)
    labels = [('Year', '2026'), ('Location', 'Rear office'), ('Asset', 'FE-07'), ('Type', 'ABC powder')]
    for i, (k, v) in enumerate(labels):
        r = i // 2; c = i % 2
        cx = gx0 + c * (gx1 - gx0) // 2
        dr.text((cx + 14, rows_y[r] + 50), k, font=font('DejaVuSans-Bold.ttf', 34), fill=ink, anchor='lm')
        dr.text((cx + 215, rows_y[r] + 50), v, font=font('/usr/share/fonts/truetype/freefont/FreeSansOblique.ttf', 38), fill=(30, 40, 110), anchor='lm')
        dr.line([gx0, rows_y[r] + 100, gx1, rows_y[r] + 100], fill=ink, width=4)
    dr.line([(gx0 + gx1) // 2, rows_y[0], (gx0 + gx1) // 2, rows_y[1] + 100], fill=ink, width=4)
    hy = rows_y[2] + 20
    cols = [gx0, gx0 + 150, gx0 + 380, gx0 + 540, gx1]
    heads = ['Date', 'Checked by', 'Result', 'Notes']
    for i, h in enumerate(heads):
        dr.text(((cols[i] + cols[i + 1]) // 2, hy + 40), h, font=font('DejaVuSans-Bold.ttf', 32), fill=ink, anchor='mm')
    dr.line([gx0, hy + 85, gx1, hy + 85], fill=ink, width=4)
    entries = [('12/01', 'M.R.', 'OK', ''), ('09/02', 'M.R.', 'OK', ''), ('11/03', 'J.T.', 'OK', 'pin tag'), ('10/04', 'J.T.', 'OK', ''),
               ('08/05', 'M.R.', 'OK', ''), ('12/06', 'A.K.', 'OK', 'gauge'), ('09/07', 'A.K.', 'OK', ''), ('11/08', 'M.R.', 'OK', ''),
               ('10/09', 'J.T.', 'OK', ''), ('', '', '', '')]
    rng = np.random.default_rng(4)
    for j, e in enumerate(entries):
        y = hy + 85 + j * 66
        for i, s in enumerate(e):
            if s:
                dx = int(rng.integers(-6, 7)); dy = int(rng.integers(-4, 5))
                dr.text(((cols[i] + cols[i + 1]) // 2 + dx, y + 34 + dy), s, font=font('/usr/share/fonts/truetype/freefont/FreeSansOblique.ttf', 36), fill=(30, 40, 110) if j % 3 else (25, 25, 30), anchor='mm')
        dr.line([gx0, y + 66, gx1, y + 66], fill=ink, width=3)
    for c in cols[1:-1]:
        dr.line([c, hy, c, hy + 85 + len(entries) * 66], fill=ink, width=3)
    # re-apply grime: multiply by the mirrored back's low-frequency variation
    ta2 = np.asarray(tag).astype(np.float32)
    g = np.asarray(back.convert('L').filter(ImageFilter.GaussianBlur(2))).astype(np.float32) / 235
    reg = ta2[fy0:fy0 + back.size[1], fx0:fx0 + back.size[0]]
    reg[ok] = reg[ok] * np.clip(g[ok], 0.7, 1.03)[:, None]
    ta2[fy0:fy0 + back.size[1], fx0:fx0 + back.size[0]] = reg
    save(p, ta2)
    print('edited textures in', os.path.join(WORK, 'src'))

# ------------------------------------------------------------------ security camera 02: blank the spec label
d = copy_model('security_camera_02')
if d:
    p = os.path.join(d, 'textures', 'security_camera_02_diff_2k.jpg'); a = load(p)
    y0, y1, x0, x1 = 1768, 1940, 1810, 1890
    reg = a[y0:y1, x0:x1]; lum = reg.mean(-1)
    paper = np.median(reg[lum > np.percentile(lum, 60)], axis=0)
    ink = lum < np.percentile(lum, 60) - 8
    ink = np.asarray(Image.fromarray((ink * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))) > 0
    full = np.zeros(a.shape[:2], bool); full[y0:y1, x0:x1] = ink
    a = inpaint(a, full)
    save(p, a)
    print('camera label blanked')
