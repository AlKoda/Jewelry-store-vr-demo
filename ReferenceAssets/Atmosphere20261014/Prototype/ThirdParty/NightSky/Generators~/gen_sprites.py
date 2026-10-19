"""Light cookies, glow/flare sprites and lens dirt for the night store (project-made, CC0).

All images are white-on-black. Cookies (cookie_*.png) store LINEAR intensity (pixel/255) as 8-bit
grayscale with +-0.5 LSB dither; glow/flare sprites are sRGB-encoded grayscale; lens dirt is sRGB RGB.
usage: gen_sprites.py <out_dir>
"""
import sys, os, math, zlib
import numpy as np
from PIL import Image, ImageFilter

OUT = sys.argv[1]
SEED = 20261014


def coords(n, m=None):
    m = m or n
    y, x = np.mgrid[0:m, 0:n].astype(np.float64)
    x = (x + 0.5) / n * 2 - 1
    y = (y + 0.5) / m * 2 - 1
    return x, y


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def value_noise(shape, scale, seed):
    """Smooth value noise (bicubic-upsampled random grid), mean 0, roughly unit range."""
    rng = np.random.default_rng(seed)
    gh, gw = max(2, int(shape[0] / scale) + 3), max(2, int(shape[1] / scale) + 3)
    g = rng.uniform(-1, 1, (gh, gw)).astype(np.float32)
    im = Image.fromarray(g, 'F').resize((int(gw * scale), int(gh * scale)), Image.BICUBIC)
    a = np.asarray(im)[:shape[0], :shape[1]]
    return a / (np.abs(a).max() + 1e-6)


def fbm_noise(shape, base_scale, octaves, seed):
    s = np.zeros(shape); amp = 1.0; tot = 0
    for o in range(octaves):
        s += amp * value_noise(shape, max(base_scale / 2 ** o, 1.5), seed + o)
        tot += amp; amp *= 0.5
    return s / tot


def black_border(a, px=3):
    a = a.copy(); a[:px] = 0; a[-px:] = 0; a[:, :px] = 0; a[:, -px:] = 0
    return a


def save_gray(a, name, srgb=False):
    """Cookies: linear values with +-0.5 LSB dither (alpha is sampled linearly).
    Sprites: sRGB-encoded so 8 bits are spent on the dark tail (import with sRGB on)."""
    a = np.clip(a, 0, 1)
    if srgb:
        a = np.where(a <= 0.0031308, a * 12.92, 1.055 * a ** (1 / 2.4) - 0.055)
        q = a * 255 + 0.5
    else:
        rng = np.random.default_rng(zlib.crc32(name.encode()))
        q = a * 255 + rng.uniform(0.0, 1.0, a.shape) * (a > 0)
    Image.fromarray(np.clip(q, 0, 255).astype(np.uint8), 'L').save(os.path.join(OUT, name), optimize=True)
    print(name, a.shape, 'max %.3f mean %.4f' % (a.max(), a.mean()))


def ss(fn, n, m=None, k=3):
    """Supersample a procedural function for clean anti-aliased edges."""
    m = m or n
    x, y = coords(n * k, m * k)
    big = fn(x, y)
    return big.reshape(m, k, n, k).mean(axis=(1, 3))


# ------------------------------------------------------------------ cookies
def flashlight_led(n=512):
    def f(x, y):
        r = np.hypot(x, y)
        th = np.arctan2(y, x)
        # LED die image: slightly square hotspot (superellipse) blended with a round core
        hot = np.exp(-0.5 * (r / 0.10) ** 2) + 0.35 * np.exp(-0.5 * (r / 0.19) ** 2)   # reflector hotspot + corona
        spill = 0.11 * (1 - smoothstep(0.55, 0.95, r)) + 0.03 * (1 - smoothstep(0.85, 0.97, r))
        gap = -0.035 * np.exp(-0.5 * ((r - 0.30) / 0.04) ** 2)      # slightly darker band outside the corona
        ring = 0.045 * np.exp(-0.5 * ((r - 0.38) / 0.035) ** 2)     # reflector-rim ring
        ripple = 0.006 * np.sin(r * 2 * np.pi * 6.0) * smoothstep(0.42, 0.55, r) * (1 - smoothstep(0.8, 0.9, r))
        ang = 1 + (0.03 * np.sin(3 * th + 0.7) + 0.015 * np.sin(7 * th + 1.9)) * smoothstep(0.08, 0.3, r)
        v = (hot + np.clip(spill + gap + ring + ripple, 0, None)) * ang
        return v
    a = ss(f, n)
    a = a * (1 + 0.015 * fbm_noise(a.shape, 40, 4, SEED))     # lens dust / reflector orange peel
    a = a / a.max()
    x, y = coords(n)
    a = a * (1 - smoothstep(0.93, 0.985, np.hypot(x, y)))
    save_gray(black_border(a), 'cookie_flashlight_led_512.png')


def uv_torch(n=512):
    def f(x, y):
        r = np.hypot(x, y)
        # projected square LED die (aspheric lens), soft edges, slightly brighter centre
        sq = (np.abs(x) ** 5 + np.abs(y) ** 5) ** (1 / 5)
        die = 1 - smoothstep(0.30, 0.40, sq)
        die *= 0.88 + 0.12 * np.exp(-0.5 * (r / 0.22) ** 2)
        halo = 0.10 * np.exp(-0.5 * (r / 0.42) ** 2)
        ring = 0.035 * np.exp(-0.5 * ((r - 0.66) / 0.04) ** 2)  # faint lens-rim ring
        return die + halo + ring
    a = ss(f, n)
    a = a * (1 + 0.03 * fbm_noise(a.shape, 24, 4, SEED + 5))
    a = a / a.max()
    x, y = coords(n)
    a = a * (1 - smoothstep(0.80, 0.95, np.hypot(x, y)))
    save_gray(black_border(a), 'cookie_uv_torch_512.png')


def streetlight(n=512):
    def f(x, y):
        # +x/-x along the street (long throw, batwing), +y towards the road, -y towards the house fronts
        ax, ay_road, ay_house = 0.95, 0.62, 0.30
        ay = np.where(y >= 0, ay_road, ay_house)
        e = (x / ax) ** 2 + (y / ay) ** 2
        env = 1 - smoothstep(0.55, 1.0, e)
        bat = 0.62 + 0.38 * smoothstep(0.05, 0.75, np.abs(x)) * (1 - smoothstep(0.78, 0.95, np.abs(x)))
        house_cut = np.where(y < 0, 1 - smoothstep(0.10, 0.32, -y) * 0.85, 1.0)
        # LED module: many small lenses give faint multiple images -> fine streak texture along x
        streak = 1 + 0.008 * np.sin(y * 2 * np.pi * 14) * smoothstep(0.2, 0.6, np.abs(x))
        return env * bat * house_cut * streak
    a = ss(f, n)
    a = a * (1 + 0.02 * fbm_noise(a.shape, 30, 3, SEED + 9))
    a = a / a.max()
    save_gray(black_border(a), 'cookie_streetlight_512.png')


def scene_floodlight(n=512):
    def f(x, y):
        # rectangular LED flood (about 16:10), soft edges, slightly brighter centre
        hx, hy = 0.82, 0.52
        bx = 1 - smoothstep(hx - 0.16, hx + 0.08, np.abs(x))
        by = 1 - smoothstep(hy - 0.12, hy + 0.08, np.abs(y))
        centre = 0.82 + 0.18 * np.exp(-0.5 * ((x / 0.45) ** 2 + (y / 0.3) ** 2))
        # 6 x 4 LED array -> very faint overlapping hotspots
        cells = 1 + 0.012 * np.cos(x * np.pi * 6 / (2 * hx) * 2) * np.cos(y * np.pi * 4 / (2 * hy) * 2)
        return bx * by * centre * cells
    a = ss(f, n)
    a = a * (1 + 0.02 * fbm_noise(a.shape, 30, 3, SEED + 13))
    a = a / a.max()
    save_gray(black_border(a), 'cookie_scene_floodlight_512.png')


# ------------------------------------------------------------------ sprites
def glow_soft(n=256):
    def f(x, y):
        r = np.hypot(x, y)
        core = np.exp(-0.5 * (r / 0.05) ** 2)
        mid = 0.35 * np.exp(-0.5 * (r / 0.16) ** 2)
        tail = np.clip(0.18 / (1 + (r / 0.10) ** 2) - 0.18 / (1 + 100.0), 0, None)
        win = 1 - smoothstep(0.5, 1.0, r)
        return (core + mid + tail) * win
    a = ss(f, n)
    save_gray(a / a.max(), 'glow_soft_256.png', srgb=True)


def glow_starburst(n=512):
    rng = np.random.default_rng(SEED + 21)
    def f(x, y):
        r = np.hypot(x, y) + 1e-6
        th = np.arctan2(y, x)
        core = np.exp(-0.5 * (r / 0.025) ** 2) + 0.25 * np.exp(-0.5 * (r / 0.08) ** 2)
        spikes = np.zeros_like(r)
        for k in range(6):                       # six-blade aperture -> six spikes
            a0 = np.pi / 6 + k * np.pi / 3
            d = np.angle(np.exp(1j * (th - a0)))
            width = 0.004 / r + 0.004
            spikes += np.exp(-0.5 * (d / width) ** 2) * np.exp(-r / 0.30)
        rays = np.zeros_like(r)
        for _ in range(90):                      # fine irregular rays (eyelash / wet lens look)
            a0 = rng.uniform(-np.pi, np.pi)
            L = rng.uniform(0.15, 0.85); w = rng.uniform(0.6, 1.6) * 0.0025
            d = np.angle(np.exp(1j * (th - a0)))
            rays += rng.uniform(0.15, 0.5) * np.exp(-0.5 * (d / (w / r + 0.002)) ** 2) * np.exp(-r / (L * 0.35))
        halo = 0.06 * np.clip(np.exp(-0.5 * (r / 0.25) ** 2) - np.exp(-0.5 * (1 / 0.25) ** 2), 0, None)
        win = 1 - smoothstep(0.6, 1.0, r)
        return (core + 0.55 * spikes + 0.22 * rays + halo) * win
    a = ss(f, n, k=2)
    save_gray(np.clip(a / np.percentile(a, 99.98), 0, 1), 'glow_starburst_512.png', srgb=True)


def flare_streak(w=512, h=128):
    def f(x, y):
        core = np.exp(-0.5 * ((x / 0.03) ** 2 + (y / 0.12) ** 2))
        streak = np.exp(-np.abs(x) / 0.22) * np.exp(-0.5 * (y / 0.07) ** 2)
        thin = 0.6 * np.exp(-np.abs(x) / 0.5) * np.exp(-0.5 * (y / 0.025) ** 2)
        win = (1 - smoothstep(0.8, 1.0, np.abs(x))) * (1 - smoothstep(0.7, 1.0, np.abs(y)))
        return (core + 0.7 * streak + thin) * win
    a = ss(f, w, h)
    save_gray(a / a.max(), 'flare_streak_512x128.png', srgb=True)


def flare_halo_ring(n=256):
    def f(x, y):
        r = np.hypot(x, y)
        ring = np.exp(-0.5 * ((r - 0.72) / 0.045) ** 2)
        inner = 0.03 * (1 - smoothstep(0.5, 0.72, r))
        win = 1 - smoothstep(0.9, 1.0, r)
        return (ring + inner) * win
    a = ss(f, n)
    save_gray(a / a.max(), 'flare_halo_ring_256.png', srgb=True)


def lens_dirt(n=1024):
    rng = np.random.default_rng(SEED + 33)
    H = W = n
    img = np.zeros((H, W, 3))
    y, x = np.mgrid[0:H, 0:W].astype(np.float64)
    xc, yc = (x / W) * 2 - 1, (y / H) * 2 - 1
    edge = 0.35 + 0.65 * smoothstep(0.2, 1.2, np.hypot(xc, yc))  # more dirt towards the edges
    # out-of-focus dust: soft discs with a slightly brighter rim, a few faint colour casts
    for i in range(170):
        cx, cy = rng.uniform(0, W), rng.uniform(0, H)
        rad = rng.choice([rng.uniform(2, 6), rng.uniform(6, 18), rng.uniform(18, 44)], p=[0.55, 0.35, 0.10])
        inten = rng.uniform(0.03, 0.25) * (1.0 if rad < 18 else 0.4)
        x0, x1 = int(max(cx - rad - 3, 0)), int(min(cx + rad + 4, W))
        y0, y1 = int(max(cy - rad - 3, 0)), int(min(cy + rad + 4, H))
        if x1 <= x0 or y1 <= y0:
            continue
        d = np.hypot(x[y0:y1, x0:x1] - cx, y[y0:y1, x0:x1] - cy) / rad
        disc = (1 - smoothstep(0.82, 1.0, d)) * (0.75 + 0.25 * smoothstep(0.5, 0.95, d))
        tint = np.array([1.0, 1.0, 1.0]) + rng.normal(0, 0.06, 3)
        img[y0:y1, x0:x1] += (inten * disc)[..., None] * tint
    # smudges: soft elongated streaks (finger wipes)
    sm = np.zeros((H, W))
    for i in range(6):
        cx, cy = rng.uniform(0, W), rng.uniform(0, H)
        ang = rng.uniform(0, np.pi); L = rng.uniform(80, 320); Wd = rng.uniform(8, 30)
        u = (x - cx) * np.cos(ang) + (y - cy) * np.sin(ang)
        v = -(x - cx) * np.sin(ang) + (y - cy) * np.cos(ang)
        bend = 0.0015 * rng.uniform(-1, 1) * u ** 2
        sm += rng.uniform(0.02, 0.06) * np.exp(-0.5 * (u / L) ** 2) * np.exp(-0.5 * ((v - bend) / Wd) ** 2)
    streaks = 0.5 + 0.5 * fbm_noise((H, W), 6, 3, SEED + 41)           # fine wipe striations
    img += (sm * (0.6 + 0.8 * streaks))[..., None] * np.array([1.0, 0.98, 0.95])
    # fine dust specks and a faint overall haze
    specks = (rng.uniform(0, 1, (H, W)) > 0.9993) * rng.uniform(0.2, 0.7, (H, W))
    specks = np.asarray(Image.fromarray((specks * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8))) / 255.0 * 3
    img += specks[..., None]
    haze = 0.008 * (0.5 + 0.5 * fbm_noise((H, W), 160, 4, SEED + 51))
    img += haze[..., None]
    img *= edge[..., None]
    img = np.clip(img / np.percentile(img, 99.95) * 0.85, 0, 1)
    img = np.where(img <= 0.0031308, img * 12.92, 1.055 * img ** (1 / 2.4) - 0.055)   # sRGB-encoded
    Image.fromarray((img * 255 + 0.5).astype(np.uint8), 'RGB').save(os.path.join(OUT, 'lens_dirt_1024.png'), optimize=True)
    print('lens_dirt_1024.png', img.shape, 'mean %.4f' % img.mean())


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    flashlight_led(); uv_torch(); streetlight(); scene_floodlight()
    glow_soft(); glow_starburst(); flare_streak(); flare_halo_ring(); lens_dirt()
