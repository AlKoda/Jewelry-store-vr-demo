"""Effect sprites, caustics and light cookies (all original procedural work)."""
import sys, os
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
from dutil import *

OUT = '.'


def grid(n):
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    u = (xx + 0.5) / n * 2 - 1; v = (yy + 0.5) / n * 2 - 1
    return u, v, np.sqrt(u * u + v * v), np.arctan2(v, u)


def save_sprite(name, rgb, a):
    save_rgba(os.path.join(OUT, name + '.png'), rgb, a, fill=(1, 1, 1))


def dust_motes(seed=101):
    n = 256
    r = rng(seed)
    u, v, rad, th = grid(n)
    cells = []
    # a) soft mote
    cells.append(np.exp(-(rad / 0.22) ** 2) * 0.9 + np.exp(-(rad / 0.06) ** 2) * 0.1)
    # b) defocused bokeh disc: 7-blade aperture, brighter rim, faint onion rings
    k = 7
    poly_r = np.cos(np.pi / k) / np.cos(((th + 0.3) % (2 * np.pi / k)) - np.pi / k)
    rr = rad / (0.82 * poly_r)
    disc = smoothstep(1.0, 0.94, rr)
    cells.append(disc * (0.38 + 0.35 * smoothstep(0.75, 0.99, rr)) * (1 + 0.06 * fbm(n, n, 12, 2, 0.5, seed + 9)))
    # c) fibre / hair
    im = Image.new('L', (n * 2, n * 2), 0); d = ImageDraw.Draw(im)
    pts = [(n * 2 * (0.15 + 0.7 * s), n * 2 * (0.5 + 0.18 * np.sin(s * 5.0 + 0.5) - 0.1 * s)) for s in np.linspace(0, 1, 40)]
    d.line(pts, fill=255, width=5)
    fib = ndi.gaussian_filter(np.asarray(im.resize((n, n), Image.LANCZOS), float) / 255, 1.2)
    cells.append(np.clip(fib * 1.6, 0, 1) * smoothstep(1.0, 0.8, rad))
    # d) irregular speck cluster
    sp = np.zeros((n, n))
    for i in range(14):
        disc_stamp(sp, n / 2 + r.normal(0, n * 0.12), n / 2 + r.normal(0, n * 0.12), r.uniform(2, 9), r.uniform(0.4, 1.0), soft=3)
    cells.append(np.clip(ndi.gaussian_filter(sp, 2.0) * 1.3, 0, 1) * smoothstep(1.0, 0.8, rad))
    A = np.zeros((2 * n, 2 * n))
    for i, c in enumerate(cells):
        A[(i // 2) * n:(i // 2 + 1) * n, (i % 2) * n:(i % 2 + 1) * n] = c
    save_sprite('DustMotes_Atlas', np.ones((2 * n, 2 * n, 3)), A)


def light_shaft(seed=102, H=1024, W=512):
    """Volumetric light shaft / beam card: source at the top, soft sides, faint dust striations."""
    r = rng(seed)
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    t = yy / H; x = (xx + 0.5) / W * 2 - 1
    spread = 0.55 + 0.45 * t  # beam widens away from the source
    side = np.cos(np.clip(x / spread, -1, 1) * np.pi / 2) ** 2 * (np.abs(x) < spread)
    along = np.exp(-t * 2.2) * smoothstep(0.0, 0.04, t) * smoothstep(1.0, 0.85, t)
    stri = ndi.gaussian_filter(r.standard_normal((H, W)), (120, 2.5)); stri /= stri.std()
    a = side * along * (0.85 + 0.15 * smoothstep(-2, 2, stri))
    a = np.clip(a / a.max(), 0, 1)
    save_sprite('LightShaft_Gradient', np.ones((H, W, 3)), a)


def fog_puffs(seed=103):
    n = 512
    A = np.zeros((2 * n, 2 * n))
    u, v, rad, th = grid(n)
    for i in range(4):
        g = fbm(n, n, 80, 4, 0.45, seed + i * 7)
        g = warp(g, fbm(n, n, 120, 2, 0.5, seed + i * 7 + 1) * 35, fbm(n, n, 120, 2, 0.5, seed + i * 7 + 2) * 35)
        detail = fbm(n, n, 14, 3, 0.5, seed + i * 7 + 3)
        shape = rad * (1 + 0.25 * fbm(n, n, 110, 2, 0.5, seed + i * 7 + 4))
        fall = smoothstep(0.95, 0.15, shape)
        dens = ndi.gaussian_filter(np.clip(fall * (0.6 + 0.22 * g + 0.06 * detail), 0, 1), 2.0) ** 1.6
        dens *= smoothstep(1.0, 0.85, rad)
        A[(i // 2) * n:(i // 2 + 1) * n, (i % 2) * n:(i % 2 + 1) * n] = dens / dens.max() * 0.9
    save_sprite('FogPuff_Atlas', np.ones((2 * n, 2 * n, 3)), A)


def lens_glow(seed=104):
    n = 512
    u, v, rad, th = grid(n)
    core = np.exp(-(rad / 0.035) ** 2)
    halo = np.exp(-rad / 0.12) * 0.55 + np.exp(-rad / 0.35) * 0.18
    ring_r = 0.72
    ring = np.exp(-((rad - ring_r) / 0.035) ** 2) * 0.10
    # chromatic: red outside, blue inside on the ring
    rgb = np.ones((n, n, 3))
    rgb[..., 0] += 0.25 * np.exp(-((rad - ring_r - 0.03) / 0.03) ** 2)
    rgb[..., 2] += 0.25 * np.exp(-((rad - ring_r + 0.03) / 0.03) ** 2)
    a = np.clip(core + halo + ring, 0, 1) * smoothstep(1.0, 0.9, rad)
    save_sprite('LensGlow', np.clip(rgb / rgb.max(-1, keepdims=True), 0, 1), a)


def lens_streak(seed=105, H=256, W=1024):
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    x = (xx + 0.5) / W * 2 - 1; y = (yy + 0.5) / H * 2 - 1
    a = np.exp(-np.abs(x) / 0.18) * np.exp(-(y / 0.04) ** 2) + 0.35 * np.exp(-np.abs(x) / 0.5) * np.exp(-(y / 0.015) ** 2) + 0.08 * np.exp(-np.abs(x) / 0.3) * np.exp(-(y / 0.15) ** 2)
    a = np.clip(a / a.max(), 0, 1) * smoothstep(1.0, 0.9, np.abs(x)) * smoothstep(1.0, 0.7, np.abs(y))
    rgb = np.ones((H, W, 3)) * np.array([0.75, 0.88, 1.0])[None, None]
    rgb = rgb * (1 - 0.3 * np.abs(x)[..., None]) + np.array([0.0, 0.05, 0.12]) * np.abs(x)[..., None]
    save_sprite('LensFlare_Streak', np.clip(rgb / rgb.max(-1, keepdims=True), 0, 1), a)


def lens_ghosts(seed=106):
    n = 256
    u, v, rad, th = grid(n)
    cells, cols = [], []
    k = 6
    poly_r = np.cos(np.pi / k) / np.cos(((th + 0.2) % (2 * np.pi / k)) - np.pi / k)
    rr = rad / (0.8 * poly_r)
    cells.append(smoothstep(1.0, 0.9, rr) * (0.25 + 0.5 * smoothstep(0.75, 1.0, rr))); cols.append((0.75, 0.9, 1.0))
    ring = np.exp(-((rad - 0.7) / 0.06) ** 2)
    cells.append(ring * 0.8); cols.append(None)
    cells.append(np.exp(-(rad / 0.45) ** 2) * 0.6); cols.append((1.0, 0.8, 0.55))
    cells.append(smoothstep(0.85, 0.75, rad) * (0.15 + 0.35 * smoothstep(0.55, 0.8, rad))); cols.append((0.7, 1.0, 0.8))
    A = np.zeros((n, 4 * n)); C = np.ones((n, 4 * n, 3))
    for i, (c, col) in enumerate(zip(cells, cols)):
        A[:, i * n:(i + 1) * n] = c
        if col is None:  # chromatic ring: red outer, blue inner
            rgb = np.ones((n, n, 3))
            rgb[..., 0] = smoothstep(0.62, 0.78, rad)
            rgb[..., 2] = smoothstep(0.78, 0.62, rad)
            rgb[..., 1] = 1 - 0.6 * np.abs(rad - 0.7) / 0.1
            C[:, i * n:(i + 1) * n] = np.clip(rgb / rgb.max(-1, keepdims=True), 0, 1)
        else:
            C[:, i * n:(i + 1) * n] = np.array(col)
    save_sprite('LensFlare_Ghosts', C, A)


def starburst(seed=107):
    n = 512
    r = rng(seed)
    u, v, rad, th = grid(n)
    a = np.exp(-(rad / 0.03) ** 2) + 0.35 * np.exp(-rad / 0.08)
    for k in range(6):  # 6-blade aperture -> 6 spikes
        ang = k * np.pi / 3 + 0.26
        d = np.abs(np.sin(th - ang)) * rad
        fwd = np.cos(th - ang) > 0
        a += fwd * np.exp(-(d / (0.004 + 0.01 * rad)) ** 2) * np.exp(-rad / 0.32) * 0.9
    streaks = ndi.gaussian_filter(r.standard_normal(720), 1.5)
    idx = ((th + np.pi) / (2 * np.pi) * 720).astype(int) % 720
    a += 0.12 * np.clip(streaks[idx], 0, None) * np.exp(-rad / 0.25)
    a = np.clip(a / np.percentile(a, 99.995), 0, 1) * smoothstep(1.0, 0.9, rad)
    save_sprite('LensStarburst', np.ones((n, n, 3)), a)


KCAUS = 9.0


def caustics(seed=108, n=1024, ss=3):
    """Tileable water caustics by photon mapping through a wavy surface."""
    N = n * ss
    h = spectral_noise(n, n, beta=4.0, seed=seed, max_feature_px=n / 5, min_feature_px=n / 18)
    h += 0.15 * spectral_noise(n, n, beta=3.0, seed=seed + 1, max_feature_px=n / 12, min_feature_px=n / 40)
    gy, gx = np.gradient(h)
    # upsample gradients (wrap) to the photon grid
    gx = ndi.zoom(np.pad(gx, 2, mode='wrap'), ss, order=1)[2 * ss:-2 * ss, 2 * ss:-2 * ss]
    gy = ndi.zoom(np.pad(gy, 2, mode='wrap'), ss, order=1)[2 * ss:-2 * ss, 2 * ss:-2 * ss]
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32) / ss
    k = KCAUS * n / 1024 / (np.abs(np.concatenate([gx.ravel(), gy.ravel()])).std() + 1e-9)
    px = (xx + k * gx) % n; py = (yy + k * gy) % n
    ix = px.astype(np.int64); iy = py.astype(np.int64)
    acc = np.bincount((iy * n + ix).ravel(), minlength=n * n).reshape(n, n).astype(float)
    acc = ndi.gaussian_filter(acc, 1.0, mode='wrap')
    acc /= np.percentile(acc, 99.5)
    img = np.clip(acc, 0, 1) ** 1.1
    save_gray(os.path.join(OUT, 'Caustics_Tile.png'), img)


def cookie_border(img, n):
    u, v, rad, th = grid(n)
    edge = np.maximum(np.abs(u), np.abs(v))
    img = img * smoothstep(1.0, 0.94, edge)
    img[:3, :] = 0; img[-3:, :] = 0; img[:, :3] = 0; img[:, -3:] = 0
    return img


def cookies(seed=109):
    n = 512
    r = rng(seed)
    u, v, rad, th = grid(n)
    # 1) LED torch with a smooth reflector: hotspot + spill, faint ring, reflector facets
    hot = np.exp(-(rad / 0.17) ** 2.4)
    spill = 0.22 * smoothstep(0.92, 0.5, rad) * (0.97 + 0.03 * np.cos(rad * 55 + 2 * fbm(n, n, 40, 2, 0.5, seed + 5)))
    ring = 0.12 * np.exp(-((rad - 0.21) / 0.025) ** 2)
    die = 0.06 * smoothstep(0.05, 0.02, np.maximum(np.abs(u), np.abs(v))) 
    blotch = 1 + 0.05 * fbm(n, n, 30, 3, 0.5, seed)
    led = np.clip((hot + spill + ring + die) * blotch, 0, 1)
    save_gray(os.path.join(OUT, 'Cookie_FlashlightLED.png'), cookie_border(led / led.max(), n))
    # 2) Incandescent reflector torch: dark filament hole, bright ring, rings of reflector artefacts
    ringb = np.exp(-((rad - 0.16) / 0.07) ** 2)
    hole = 1 - 0.55 * np.exp(-(rad / 0.07) ** 2)
    arte = 0.15 * np.exp(-((rad - 0.38) / 0.03) ** 2) + 0.08 * np.exp(-((rad - 0.55) / 0.05) ** 2)
    sp2 = 0.18 * smoothstep(0.95, 0.6, rad)
    inc = np.clip((ringb * hole + arte + sp2) * (1 + 0.08 * fbm(n, n, 20, 3, 0.5, seed + 1)) * (1 + 0.05 * np.sin(th * 3)), 0, None)
    save_gray(os.path.join(OUT, 'Cookie_FlashlightIncandescent.png'), cookie_border(np.clip(inc / inc.max(), 0, 1), n))
    # 3) UV torch, 9 LEDs (ring of 8 + centre): overlapping blotchy spots
    uv = np.zeros((n, n))
    pos = [(0, 0)] + [(0.075 * np.cos(a), 0.075 * np.sin(a)) for a in np.linspace(0, 2 * np.pi, 8, endpoint=False)]
    for (px, py) in pos:
        rr = np.sqrt((u - px * 3.5) ** 2 + (v - py * 3.5) ** 2)
        uv += np.exp(-(rr / (0.30 + r.uniform(-0.03, 0.03))) ** 2.2) * r.uniform(0.8, 1.0)
    uv = uv / uv.max()
    uv *= 1 + 0.07 * fbm(n, n, 15, 3, 0.5, seed + 2)
    uv = np.clip(uv * smoothstep(0.98, 0.6, rad), 0, 1)
    save_gray(os.path.join(OUT, 'Cookie_UVTorch.png'), cookie_border(uv / uv.max(), n))
    # 4) Gallery / display spot: framed circular beam, flat plateau, crisp-ish edge, faint spill
    gal = smoothstep(0.62, 0.56, rad) * (0.92 + 0.08 * smoothstep(0.56, 0.0, rad)) + 0.05 * smoothstep(0.95, 0.62, rad)
    save_gray(os.path.join(OUT, 'Cookie_GallerySpot.png'), cookie_border(np.clip(gal, 0, 1), n))
    # 5) Scene flood lamp (LED panel on a tripod): rectangular soft beam with faint LED grid
    rect = smoothstep(0.80, 0.55, np.abs(u)) * smoothstep(0.62, 0.40, np.abs(v))
    gridp = 0.015 * (np.cos(u * np.pi * 12) * np.cos(v * np.pi * 9)) * rect
    flood = np.clip(rect * (0.9 + 0.1 * smoothstep(0.8, 0.0, np.sqrt(u ** 2 + (v * 1.3) ** 2))) + gridp, 0, 1)
    save_gray(os.path.join(OUT, 'Cookie_SceneFlood.png'), cookie_border(flood, n))


def kenney_smoke(src_dir, names):
    for nm in names:
        im = Image.open(os.path.join(src_dir, nm + '.png')).convert('RGBA')
        arr = np.asarray(im, float) / 255
        out = 'SmokePuff_Kenney' + nm.split('_')[1] + '.png'
        save_rgba(os.path.join(OUT, out), arr[..., :3], arr[..., 3], fill=(1, 1, 1))


if __name__ == '__main__':
    OUT = sys.argv[1]
    which = sys.argv[2:] or ['motes', 'shaft', 'fog', 'glow', 'streak', 'ghosts', 'star', 'caustics', 'cookies']
    if 'motes' in which: dust_motes()
    if 'shaft' in which: light_shaft()
    if 'fog' in which: fog_puffs()
    if 'glow' in which: lens_glow()
    if 'streak' in which: lens_streak()
    if 'ghosts' in which: lens_ghosts()
    if 'star' in which: starburst()
    if 'caustics' in which: caustics()
    if 'cookies' in which: cookies()
    if 'kenney' in which:
        kenney_smoke(os.environ['KENNEY_DIR'], ['smoke_01', 'smoke_04', 'smoke_07', 'smoke_08'])
