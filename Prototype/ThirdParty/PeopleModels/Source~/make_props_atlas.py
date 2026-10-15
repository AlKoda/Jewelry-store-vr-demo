"""Project-made 512 px atlas for the figures' hand props (notebook, pen, phone, camera).

    python3 -I make_props_atlas.py OUT_DIR

Writes PeopleProps_Color.jpg (sRGB) and PeopleProps_MetallicSmoothness.png (R metallic, A smoothness).
Region layout matches props.REG. All content is procedural; no text or marks of real products.
"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

S = 512
out = sys.argv[1]
rng = np.random.default_rng(7)
col = np.zeros((S, S, 3), np.float32)
ms = np.zeros((S, S, 4), np.float32)


def px(region):
    """UV region -> pixel box (x0, y0, x1, y1) in image coordinates (v up -> rows down)."""
    u0, v0, u1, v1 = region
    return int(u0 * S), int((1 - v1) * S), int(u1 * S), int((1 - v0) * S)


def noise(h, w, scale, octaves=4, seed=0):
    r = np.random.default_rng(seed)
    acc = np.zeros((h, w), np.float32)
    amp, tot = 1.0, 0.0
    for o in range(octaves):
        s = max(1, int(scale / (2 ** o)))
        small = r.random((h // s + 2, w // s + 2)).astype(np.float32)
        big = np.asarray(Image.fromarray((small * 255).astype(np.uint8)).resize(((w // s + 2) * s, (h // s + 2) * s), Image.BICUBIC), np.float32)[:h, :w] / 255
        acc += amp * big
        tot += amp
        amp *= 0.5
    return acc / tot


REG = {
    'cover': (0.0, 0.5, 0.5, 1.0), 'page': (0.5, 0.5, 1.0, 1.0), 'phone_body': (0.0, 0.25, 0.25, 0.5),
    'phone_screen': (0.25, 0.25, 0.5, 0.5), 'cam_body': (0.5, 0.25, 0.75, 0.5), 'lens': (0.75, 0.25, 1.0, 0.5),
    'glass': (0.0, 0.0, 0.125, 0.125), 'pen': (0.125, 0.0, 0.25, 0.125), 'metal': (0.25, 0.0, 0.375, 0.125),
    'flash': (0.375, 0.0, 0.5, 0.125), 'page_edge': (0.5, 0.0, 0.625, 0.125), 'strap': (0.625, 0.0, 0.75, 0.125),
}


def fill(name, rgb, metallic, smooth):
    x0, y0, x1, y1 = px(REG[name])
    h, w = y1 - y0, x1 - x0
    col[y0:y1, x0:x1] = rgb(h, w) if callable(rgb) else np.array(rgb, np.float32) / 255
    ms[y0:y1, x0:x1, 0] = metallic
    ms[y0:y1, x0:x1, 3] = smooth(h, w) if callable(smooth) else smooth


# Black leatherette cover: pebbled grain.
def leather(h, w):
    g = noise(h, w, 6, 3, 1) * 0.6 + noise(h, w, 40, 3, 2) * 0.4
    v = 0.035 + 0.03 * g
    return np.dstack([v, v, v * 1.05])


fill('cover', leather, 0.0, lambda h, w: 0.35 + 0.15 * noise(h, w, 6, 2, 3))


# Ruled notebook page with handwritten-looking notes.
def page(h, w):
    img = Image.new('RGB', (w, h), (238, 236, 226))
    d = ImageDraw.Draw(img)
    for y in range(22, h, 11):
        d.line([(0, y), (w, y)], fill=(170, 190, 215), width=1)
    d.line([(26, 0), (26, h)], fill=(215, 140, 140), width=1)
    r = np.random.default_rng(11)
    for y in range(31, h - 40, 11):
        x = 32 + r.integers(0, 6)
        end = w - 10 - r.integers(0, 90) if r.random() > 0.15 else 32 + r.integers(20, 60)
        while x < end:
            wl = r.integers(8, 30)
            pts = []
            for t in np.linspace(0, 1, wl * 2):
                xx = x + t * wl
                yy = y - 2 - 3.0 * abs(np.sin(t * wl * 0.9 + r.random())) * (0.6 + 0.4 * r.random())
                pts.append((xx, yy))
            d.line(pts, fill=(30, 40, 95), width=1)
            x += wl + r.integers(4, 8)
    img = img.filter(ImageFilter.GaussianBlur(0.35))
    a = np.asarray(img, np.float32) / 255
    a *= (0.96 + 0.04 * noise(h, w, 30, 2, 5))[..., None]
    return a


fill('page', page, 0.0, 0.18)


def edge(h, w):
    rows = 0.80 + 0.12 * np.sin(np.arange(h)[:, None] * 2.1) * np.ones((1, w))
    rows += 0.05 * noise(h, w, 3, 2, 9)
    return np.dstack([rows * 0.93, rows * 0.92, rows * 0.86])


fill('page_edge', edge, 0.0, 0.15)


def phone_body(h, w):
    v = 0.05 + 0.015 * noise(h, w, 20, 3, 12)
    return np.dstack([v, v * 1.02, v * 1.08])


fill('phone_body', phone_body, 0.2, 0.6)


def screen(h, w):
    # Camera viewfinder at night: dark street, blurred lamps and blue/red flashes, recording dot and timer.
    img = Image.new('RGB', (w, h), (8, 10, 16))
    d = ImageDraw.Draw(img)
    for y in range(h):
        t = y / h
        d.line([(0, y), (w, y)], fill=(int(10 + 25 * t), int(12 + 22 * t), int(20 + 18 * t)))
    d.rectangle([0, int(h * 0.62), w, h], fill=(26, 24, 26))
    lamps = Image.new('RGB', (w, h), (0, 0, 0))
    ld = ImageDraw.Draw(lamps)
    r = np.random.default_rng(4)
    for _ in range(9):
        x, y = r.integers(0, w), r.integers(int(h * 0.25), int(h * 0.6))
        rad = r.integers(3, 9)
        ld.ellipse([x - rad, y - rad, x + rad, y + rad], fill=(255, 190, 110))
    ld.ellipse([w * 0.18, h * 0.42, w * 0.38, h * 0.52], fill=(40, 80, 255))
    ld.ellipse([w * 0.55, h * 0.43, w * 0.72, h * 0.52], fill=(255, 40, 50))
    lamps = lamps.filter(ImageFilter.GaussianBlur(5))
    a = np.asarray(img, np.float32) + np.asarray(lamps, np.float32) * 0.9
    img = Image.fromarray(a.clip(0, 255).astype(np.uint8))
    d = ImageDraw.Draw(img)
    d.ellipse([w * 0.40, h * 0.035, w * 0.47, h * 0.07], fill=(230, 30, 30))
    d.text((w * 0.50, h * 0.025), '00:42', fill=(240, 240, 240))
    d.ellipse([w * 0.42, h * 0.88, w * 0.58, h * 0.96], outline=(240, 240, 240), width=2)
    d.ellipse([w * 0.455, h * 0.9, w * 0.545, h * 0.94], fill=(230, 30, 30))
    return np.asarray(img, np.float32) / 255


fill('phone_screen', screen, 0.0, 0.85)


def plastic(h, w):
    v = 0.045 + 0.02 * noise(h, w, 2, 2, 21) + 0.01 * noise(h, w, 25, 2, 22)
    return np.dstack([v, v, v])


fill('cam_body', plastic, 0.0, 0.32)


def lens(h, w):
    u = np.arange(w)[None, :] * np.ones((h, 1))
    v = np.arange(h)[:, None] * np.ones((1, w))
    base = 0.05 + 0.01 * noise(h, w, 4, 2, 31)
    ribs = (np.sin(u * 1.4) > 0) & (((v > h * 0.15) & (v < h * 0.45)) | ((v > h * 0.6) & (v < h * 0.75)))
    base = np.where(ribs, base * 0.6, base)
    ring = (v > h * 0.08) & (v < h * 0.1)
    rgb = np.dstack([base, base, base])
    rgb[ring] = [0.42, 0.42, 0.44]  # thin grey index ring (no maker's colour code)
    return rgb


fill('lens', lens, 0.0, lambda h, w: 0.35)


def glass(h, w):
    yy, xx = np.mgrid[0:h, 0:w] / max(h, w)
    g = np.dstack([0.02 + 0.03 * xx, 0.04 + 0.05 * yy, 0.07 + 0.08 * (1 - yy)])
    spot = np.exp(-(((xx - 0.35) ** 2 + (yy - 0.3) ** 2) / 0.01))
    return g + spot[..., None] * 0.5


fill('glass', glass, 0.0, 0.92)
fill('pen', lambda h, w: np.dstack([np.full((h, w), 0.03), np.full((h, w), 0.05), np.full((h, w), 0.12)]), 0.0, 0.7)


def brushed(h, w):
    from numpy.lib.stride_tricks import sliding_window_view
    r = np.random.default_rng(41).random((h, w)).astype(np.float32)
    v = sliding_window_view(np.pad(r, ((0, 0), (6, 6)), mode='wrap'), 13, axis=1).mean(-1)
    v = 0.58 + 0.45 * (v - 0.5)
    return np.dstack([v, v, v * 1.02])


fill('metal', brushed, 1.0, 0.6)


def flash(h, w):
    v = np.arange(h)[:, None] * np.ones((1, w))
    s = 0.82 + 0.08 * np.sin(v * 1.6)
    return np.dstack([s, s, s * 0.98])


fill('flash', flash, 0.0, 0.7)
fill('strap', lambda h, w: np.dstack([0.04 + 0.02 * noise(h, w, 2, 2, 51)] * 3), 0.0, 0.25)

os.makedirs(out, exist_ok=True)
Image.fromarray((col.clip(0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(out, 'PeopleProps_Color.jpg'), quality=92)
m = (ms.clip(0, 1) * 255 + 0.5).astype(np.uint8)
Image.fromarray(m, 'RGBA').resize((256, 256), Image.LANCZOS).save(os.path.join(out, 'PeopleProps_MetallicSmoothness.png'), optimize=True)
print('ok')
