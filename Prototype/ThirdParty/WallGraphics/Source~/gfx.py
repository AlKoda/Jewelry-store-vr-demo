# Shared 2D helpers for the WallGraphics pack (project-original code).
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
FONTROOT = os.path.join(HERE, '..', 'fonts', 'gfonts')
BARLOW = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Fonts')

FONTS = {
    'cormorant': 'ofl/cormorantgaramond/CormorantGaramond[wght].ttf',
    'cormorant_i': 'ofl/cormorantgaramond/CormorantGaramond-Italic[wght].ttf',
    'cinzel': 'ofl/cinzel/Cinzel[wght].ttf',
    'italiana': 'ofl/italiana/Italiana-Regular.ttf',
    'marcellus': 'ofl/marcellus/Marcellus-Regular.ttf',
    'tenor': 'ofl/tenorsans/TenorSans-Regular.ttf',
    'barlow': 'ofl/barlow/Barlow-Regular.ttf',
    'barlow_m': 'ofl/barlow/Barlow-Medium.ttf',
    'barlow_sb': 'ofl/barlow/Barlow-SemiBold.ttf',
    'barlow_b': 'ofl/barlow/Barlow-Bold.ttf',
    'barlow_xb': 'ofl/barlow/Barlow-ExtraBold.ttf',
    'barlow_blk': 'ofl/barlow/Barlow-Black.ttf',
    'barlow_l': 'ofl/barlow/Barlow-Light.ttf',
    'barlow_i': 'ofl/barlow/Barlow-Italic.ttf',
    'bc': 'ofl/barlowcondensed/BarlowCondensed-Regular.ttf',
    'bc_m': 'ofl/barlowcondensed/BarlowCondensed-Medium.ttf',
    'bc_sb': 'ofl/barlowcondensed/BarlowCondensed-SemiBold.ttf',
    'bc_b': 'ofl/barlowcondensed/BarlowCondensed-Bold.ttf',
    'bc_xb': 'ofl/barlowcondensed/BarlowCondensed-ExtraBold.ttf',
    'bc_blk': 'ofl/barlowcondensed/BarlowCondensed-Black.ttf',
    'oswald': 'ofl/oswald/Oswald[wght].ttf',
    'anton': 'ofl/anton/Anton-Regular.ttf',
    'bebas': 'ofl/bebasneue/BebasNeue-Regular.ttf',
    'fraktur': 'ofl/unifrakturmaguntia/UnifrakturMaguntia-Book.ttf',
    'oldstd': 'ofl/oldstandardtt/OldStandard-Regular.ttf',
    'oldstd_b': 'ofl/oldstandardtt/OldStandard-Bold.ttf',
    'oldstd_i': 'ofl/oldstandardtt/OldStandard-Italic.ttf',
    'ptserif': 'ofl/ptserif/PT_Serif-Web-Regular.ttf',
    'ptserif_b': 'ofl/ptserif/PT_Serif-Web-Bold.ttf',
    'ptserif_i': 'ofl/ptserif/PT_Serif-Web-Italic.ttf',
    'caveat': 'ofl/caveat/Caveat[wght].ttf',
    'patrick': 'ofl/patrickhand/PatrickHand-Regular.ttf',
    'kalam': 'ofl/kalam/Kalam-Regular.ttf',
    'kalam_b': 'ofl/kalam/Kalam-Bold.ttf',
    'reenie': 'ofl/reeniebeanie/ReenieBeanie.ttf',
    'homemade': 'apache/homemadeapple/HomemadeApple-Regular.ttf',
    'marker': 'apache/permanentmarker/PermanentMarker-Regular.ttf',
    'sedgwick': 'ofl/sedgwickavedisplay/SedgwickAveDisplay-Regular.ttf',
    'rocksalt': 'apache/rocksalt/RockSalt-Regular.ttf',
    'elite': 'apache/specialelite/SpecialElite-Regular.ttf',
    'courier': 'ofl/courierprime/CourierPrime-Regular.ttf',
    'courier_b': 'ofl/courierprime/CourierPrime-Bold.ttf',
    'vibes': 'ofl/greatvibes/GreatVibes-Regular.ttf',
    'pinyon': 'ofl/pinyonscript/PinyonScript-Regular.ttf',
    'baskerville': 'ofl/librebaskerville/LibreBaskerville[wght].ttf',
    'baskerville_i': 'ofl/librebaskerville/LibreBaskerville-Italic[wght].ttf',
    'montserrat': 'ofl/montserrat/Montserrat[wght].ttf',
    'robotoc': 'ofl/robotocondensed/RobotoCondensed[wght].ttf',
}
USED_FONTS = set()


def font(name, size, wght=None):
    path = os.path.join(FONTROOT, FONTS[name])
    f = ImageFont.truetype(path, int(round(size)))
    USED_FONTS.add(FONTS[name])
    if wght is not None:
        try:
            f.set_variation_by_axes([wght])
        except Exception:
            pass
    return f


def text_width(text, f, tracking=0.0):
    if not text:
        return 0
    return f.getlength(text) + tracking * f.size * (len(text) - 1)


def draw_text(draw, xy, text, f, fill, tracking=0.0, anchor='l', baseline=True):
    """Draw text with letter-spacing (tracking in em). anchor: l, m, r (horizontal);
    y is the baseline when baseline=True."""
    x, y = xy
    w = text_width(text, f, tracking)
    if anchor == 'm':
        x -= w / 2
    elif anchor == 'r':
        x -= w
    a = 'ls' if baseline else 'la'
    if tracking == 0:
        draw.text((x, y), text, font=f, fill=fill, anchor=a)
        return w
    for i, ch in enumerate(text):
        draw.text((x, y), ch, font=f, fill=fill, anchor=a)
        # advance with kerning to next char
        if i + 1 < len(text):
            pair = f.getlength(text[i:i + 2]) - f.getlength(text[i + 1])
            x += pair + tracking * f.size
    return w


def text_mask(size, xy, text, f, tracking=0.0, anchor='l'):
    m = Image.new('L', size, 0)
    draw_text(ImageDraw.Draw(m), xy, text, f, 255, tracking, anchor)
    return m


def wrap(text, f, width, tracking=0.0):
    words = text.split()
    lines, cur = [], ''
    for w_ in words:
        t = (cur + ' ' + w_).strip()
        if text_width(t, f, tracking) <= width:
            cur = t
        else:
            if cur:
                lines.append(cur)
            cur = w_
    if cur:
        lines.append(cur)
    return lines


# ------------------------------------------------------------------ noise
def fbm(h, w, scale=64.0, octaves=5, seed=0, persistence=0.5):
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32)
    amp, tot = 1.0, 0.0
    s = scale
    for _ in range(octaves):
        gh, gw = max(2, int(h / s) + 3), max(2, int(w / s) + 3)
        g = rng.random((gh, gw)).astype(np.float32)
        z = ndimage.zoom(g, (s, s), order=3, mode='wrap', grid_mode=True)
        oy, ox = rng.integers(0, max(1, z.shape[0] - h)), rng.integers(0, max(1, z.shape[1] - w))
        z = z[oy:oy + h, ox:ox + w]
        if z.shape != (h, w):
            z = np.pad(z, ((0, h - z.shape[0]), (0, w - z.shape[1])), mode='edge')
        out += amp * z
        tot += amp
        amp *= persistence
        s = max(1.0, s / 2)
    out /= tot
    out = (out - out.min()) / max(1e-6, out.max() - out.min())
    return out


def blur(a, sigma):
    if a.ndim == 3:
        return np.stack([ndimage.gaussian_filter(a[..., c], sigma) for c in range(a.shape[2])], -1)
    return ndimage.gaussian_filter(a, sigma)


def paper(h, w, base=(0.95, 0.94, 0.90), seed=1, fiber=0.025, mottle=0.035):
    rng = np.random.default_rng(seed)
    b = np.array(base, np.float32)
    m = fbm(h, w, scale=max(h, w) / 6, octaves=6, seed=seed) - 0.5
    n = rng.normal(0, 1, (h, w)).astype(np.float32)
    fib = ndimage.gaussian_filter(n, (0.6, 2.5)) * 1.4 + ndimage.gaussian_filter(n, (2.5, 0.6)) * 1.0
    img = b[None, None, :] * (1 + mottle * m[..., None] + fiber * fib[..., None])
    return np.clip(img, 0, 1)


def to_img(a, mode=None):
    a = np.clip(a, 0, 1)
    if mode is None:
        mode = 'L' if a.ndim == 2 else ('RGBA' if a.shape[2] == 4 else 'RGB')
    return Image.fromarray((a * 255 + 0.5).astype(np.uint8), mode)


def from_img(im):
    return np.asarray(im).astype(np.float32) / 255.0


def grain(a, amount=0.012, seed=3):
    rng = np.random.default_rng(seed)
    n = rng.normal(0, amount, a.shape[:2]).astype(np.float32)
    n = ndimage.gaussian_filter(n, 0.5)
    return np.clip(a + n[..., None], 0, 1)


def vignette(h, w, strength=0.4, power=2.2, cx=0.5, cy=0.5):
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    dx = (x / w - cx) * 2
    dy = (y / h - cy) * 2
    r = np.sqrt(dx * dx + dy * dy) / np.sqrt(2)
    return 1 - strength * r ** power


def grime(h, w, seed=5, amount=0.25, scale=None, edge=0.3):
    """Dirt layer in [0,1]: blotchy plus more at edges/bottom."""
    scale = scale or max(h, w) / 5
    g = fbm(h, w, scale=scale, octaves=6, seed=seed)
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    ex = np.minimum(x, w - 1 - x) / w
    ey = np.minimum(y, h - 1 - y) / h
    e = np.exp(-np.minimum(ex, ey) * 18) * edge
    bottom = (y / h) ** 3 * 0.25
    d = np.clip((g - 0.45) * 2.2, 0, 1) * amount + e + bottom * amount
    return np.clip(d, 0, 1)


def gold_fill(h, w, seed=0, light=1.0):
    """Printed metallic-gold gradient (display space RGB)."""
    y = np.linspace(0, 1, h, dtype=np.float32)[:, None]
    stops = np.array([[0.0, 0.55, 0.40, 0.18], [0.30, 0.86, 0.72, 0.44], [0.48, 0.98, 0.90, 0.64],
                      [0.62, 0.80, 0.64, 0.34], [1.0, 0.62, 0.46, 0.22]], np.float32)
    cols = np.stack([np.interp(y[:, 0], stops[:, 0], stops[:, i]) for i in (1, 2, 3)], -1)
    g = np.broadcast_to(cols[:, None, :], (h, w, 3)).copy()
    n = fbm(h, w, scale=max(8, w / 30), octaves=3, seed=seed) - 0.5
    g *= (1 + 0.06 * n[..., None]) * light
    return np.clip(g, 0, 1)


def composite(base, color, mask):
    """base (H,W,3) float, color (H,W,3) or (3,), mask (H,W) float 0..1"""
    m = mask[..., None]
    return base * (1 - m) + np.asarray(color, np.float32) * m


def mask_from(im):
    return np.asarray(im).astype(np.float32) / 255.0


def drop_shadow(mask, offset=(3, 3), sigma=3, strength=0.5):
    s = np.roll(np.roll(mask, offset[1], 0), offset[0], 1)
    return ndimage.gaussian_filter(s, sigma) * strength


def emboss(mask, sigma=1.2, light=(-1, -1)):
    m = ndimage.gaussian_filter(mask, sigma)
    gy, gx = np.gradient(m)
    shade = -(gx * light[0] + gy * light[1])
    return shade


def rounded_rect_mask(w, h, r, ss=4):
    m = Image.new('L', (w * ss, h * ss), 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, w * ss - 1, h * ss - 1), r * ss, fill=255)
    return np.asarray(m.resize((w, h), Image.LANCZOS)).astype(np.float32) / 255


def save_jpg(a, path, q=88):
    to_img(a[..., :3]).save(path, 'JPEG', quality=q, optimize=True)


def save_png(a, path, alpha=None):
    if alpha is not None:
        rgba = np.concatenate([np.clip(a[..., :3], 0, 1), np.clip(alpha, 0, 1)[..., None]], -1)
        Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), 'RGBA').save(path, optimize=True)
    else:
        to_img(a).save(path, optimize=True)


def ss_draw(w, h, ss=3, mode='L', bg=0):
    im = Image.new(mode, (w * ss, h * ss), bg)
    return im, ImageDraw.Draw(im)


def down(im, w, h):
    return im.resize((w, h), Image.LANCZOS)


def warp(a, amp=2.0, scale=80, seed=9):
    """Small paper-wave displacement of an image array."""
    h, w = a.shape[:2]
    dx = (fbm(h, w, scale=scale, octaves=3, seed=seed) - 0.5) * 2 * amp
    dy = (fbm(h, w, scale=scale, octaves=3, seed=seed + 1) - 0.5) * 2 * amp
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    coords = [y + dy, x + dx]
    if a.ndim == 2:
        return ndimage.map_coordinates(a, coords, order=1, mode='nearest')
    return np.stack([ndimage.map_coordinates(a[..., c], coords, order=1, mode='nearest') for c in range(a.shape[2])], -1)
