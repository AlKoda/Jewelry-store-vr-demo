"""Shared helpers for the SurfaceDecals generators (project-owned code)."""
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi


def rng(seed):
    return np.random.default_rng(seed)


def spectral_noise(h, w, beta=2.0, seed=0, max_feature_px=None, min_feature_px=None):
    """Tileable fractal (1/f^beta power spectrum) noise, zero mean, unit std."""
    r = rng(seed)
    white = r.standard_normal((h, w))
    F = np.fft.fft2(white)
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    f = np.sqrt(fx * fx + fy * fy)
    fmin = 1.0 / max_feature_px if max_feature_px else 1.0 / max(h, w)
    ff = np.maximum(f, fmin)
    amp = ff ** (-beta / 2.0)
    if min_feature_px:
        fc = 1.0 / min_feature_px
        amp *= np.exp(-(f / fc) ** 2)
    amp[0, 0] = 0
    n = np.real(np.fft.ifft2(F * amp))
    return (n - n.mean()) / (n.std() + 1e-9)


def blur_noise(h, w, sigma, seed=0, wrap=True):
    r = rng(seed)
    n = ndi.gaussian_filter(r.standard_normal((h, w)), sigma, mode='wrap' if wrap else 'reflect')
    return (n - n.mean()) / (n.std() + 1e-9)


def fbm(h, w, base_sigma, octaves=5, persistence=0.5, seed=0, lacunarity=2.0):
    out = np.zeros((h, w))
    amp = 1.0
    s = base_sigma
    tot = 0
    for o in range(octaves):
        out += amp * blur_noise(h, w, max(s, 0.5), seed=seed * 101 + o)
        tot += amp
        amp *= persistence
        s /= lacunarity
    out /= tot
    return (out - out.mean()) / (out.std() + 1e-9)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def warp(img, dx, dy, order=1, mode='reflect'):
    h, w = img.shape
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float64)
    return ndi.map_coordinates(img, [yy + dy, xx + dx], order=order, mode=mode)


def normal_from_height(hgt, strength=1.0, wrap=False):
    """OpenGL / Unity convention: R = +U (right), G = +V (up in the image)."""
    mode = 'wrap' if wrap else 'nearest'
    dx = ndi.sobel(hgt, axis=1, mode=mode) / 8.0
    dy = ndi.sobel(hgt, axis=0, mode=mode) / 8.0
    nx = -dx * strength
    ny = dy * strength
    nz = np.ones_like(hgt)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.stack([nx / l, ny / l, nz / l], -1)
    return n


def save_normal(path, n):
    rgb = np.clip(np.round((n * 0.5 + 0.5) * 255), 0, 255).astype(np.uint8)
    Image.fromarray(rgb, 'RGB').save(path, optimize=True)


def save_rgba(path, rgb, a, fill=None):
    """rgb float (h,w,3) 0..1 sRGB; a float (h,w). Transparent pixels get a constant fill colour
    (alpha-weighted mean) so filtering/mips do not pull dark fringes and PNG compresses well."""
    a = np.clip(a, 0, 1)
    rgb = np.clip(rgb, 0, 1)
    if fill is None:
        wsum = a.sum()
        fill = (rgb * a[..., None]).reshape(-1, 3).sum(0) / max(wsum, 1e-6)
    a8 = np.round(a * 255).astype(np.uint8)
    rgb8 = np.round(rgb * 255).astype(np.uint8)
    rgb8[a8 == 0] = np.round(np.asarray(fill) * 255).astype(np.uint8)
    Image.fromarray(np.dstack([rgb8, a8]), 'RGBA').save(path, optimize=True)


def save_gray(path, g):
    Image.fromarray(np.clip(np.round(g * 255), 0, 255).astype(np.uint8), 'L').save(path, optimize=True)


def save_rgb(path, rgb):
    Image.fromarray(np.clip(np.round(rgb * 255), 0, 255).astype(np.uint8), 'RGB').save(path, optimize=True)


def save_ms(path, smooth, metal=None):
    """Unity Standard metallic map: R = metallic, A = smoothness."""
    h, w = smooth.shape
    m = np.zeros((h, w)) if metal is None else metal
    out = np.zeros((h, w, 4), np.uint8)
    out[..., 0] = np.round(np.clip(m, 0, 1) * 255)
    out[..., 3] = np.round(np.clip(smooth, 0, 1) * 255)
    Image.fromarray(out, 'RGBA').save(path, optimize=True)


def poly_mask(h, w, polys, ss=4, value=255):
    """Antialiased polygon fill. polys: list of arrays (N,2) in pixel coords (x, y)."""
    im = Image.new('L', (w * ss, h * ss), 0)
    d = ImageDraw.Draw(im)
    for p in polys:
        d.polygon([(float(x) * ss, float(y) * ss) for x, y in p], fill=value)
    im = im.resize((w, h), Image.LANCZOS)
    return np.asarray(im, np.float64) / 255.0


def disc_stamp(canvas, cx, cy, r, val=1.0, soft=1.0, op='max'):
    """Stamp a soft disc into canvas (in place)."""
    h, w = canvas.shape
    x0, x1 = int(max(0, np.floor(cx - r - 2 * soft - 1))), int(min(w, np.ceil(cx + r + 2 * soft + 2)))
    y0, y1 = int(max(0, np.floor(cy - r - 2 * soft - 1))), int(min(h, np.ceil(cy + r + 2 * soft + 2)))
    if x1 <= x0 or y1 <= y0:
        return
    yy, xx = np.mgrid[y0:y1, x0:x1]
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
    v = val * np.clip((r - d) / max(soft, 1e-3) + 0.5, 0, 1)
    if op == 'max':
        canvas[y0:y1, x0:x1] = np.maximum(canvas[y0:y1, x0:x1], v)
    else:
        canvas[y0:y1, x0:x1] += v


def border_fade(a, frac=0.02):
    """Fade alpha to zero over the outer `frac` of the image so nothing is cut at the quad edge."""
    h, w = a.shape
    y = np.minimum(np.arange(h), np.arange(h)[::-1])[:, None] / (h * frac)
    x = np.minimum(np.arange(w), np.arange(w)[::-1])[None, :] / (w * frac)
    return a * np.clip(np.minimum(x, y), 0, 1) ** 1.5
