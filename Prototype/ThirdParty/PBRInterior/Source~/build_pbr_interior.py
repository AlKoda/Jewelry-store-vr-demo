#!/usr/bin/env python3
"""Build the PBRInterior material pack for the night jewellery-store scene.

Usage:  python3 -I build_pbr_interior.py <source_dir> <output_dir>

<source_dir> holds the unmodified ambientCG 2K maps, one folder per download
(for example Marble021_2K-JPG/Marble021_2K-JPG_Color.jpg). <output_dir> receives
the Unity-ready maps:

  <Name>_Color.jpg              sRGB albedo, JPEG quality 90
  <Name>_Normal.png             tangent-space normal, OpenGL convention (+Y up)
                                (not written for the polished sets StoneTop,
                                StoneTopDark, Chrome, BrassPolished and for
                                GlassSmudge: their relief is below 8-bit
                                precision, so a flat normal map would only
                                cost memory)
  <Name>_MetallicSmoothness.png RGBA, R = metallic, G = B = 0, A = 1 - roughness
  <Name>_AO.jpg                 optional ambient occlusion (linear)
  GlassSmudge_ColorAlpha.png    RGBA overlay (sRGB colour, linear alpha)

Every map is periodic (seamless). Resampling pads the source by wrapping, so
filters see the opposite edge. Procedural parts use FFT-filtered noise, which
is periodic by construction. Fixed seeds make the output reproducible.

Written for the Jewelry-store VR demo with Claude, October 14, 2026; revised
October 15, 2026 after review (StoneTopDark floor lift, PlasterWarm crack fill
from offset plaster in the colour map with the scanned normal untouched, stronger CeilingPaint and
SafeSteel relief, flat normal maps dropped).
Original project work (no third-party pixels): SafeSteel and CeilingPaint
(fully procedural), and the procedural parts of the derived sets: the FloorTile
layout, grout, bevel and lippage; the Wallpaper ogee print; the GlassSmudge
cluster mask, wipe arcs and dust; the fine pile normal of VelvetCharcoal and
SuedeCream. Everything else is resampled / recoloured ambientCG CC0 material.

Dependencies: Python 3, numpy, Pillow (no scipy). Run time about 4 minutes.
Optional extra arguments name single builders, e.g. build_walnut.
"""
import os
import sys

import numpy as np
from PIL import Image

Image.MAX_IMAGE_PIXELS = None

SRC = sys.argv[1]
OUT = sys.argv[2]
os.makedirs(OUT, exist_ok=True)


# ----------------------------------------------------------------- helpers
def src_path(set_name, channel):
    ext = "png" if "PNG" in set_name else "jpg"
    return os.path.join(SRC, set_name, f"{set_name}_{channel}.{ext}")


def load(set_name, channel):
    """Load a source map as float32 in 0..1 (RGB or single channel)."""
    im = Image.open(src_path(set_name, channel))
    if im.mode in ("I;16", "I"):
        a = np.asarray(im).astype(np.float32) / 65535.0
    elif channel in ("Color", "NormalGL"):
        a = np.asarray(im.convert("RGB")).astype(np.float32) / 255.0
    else:
        a = np.asarray(im.convert("L")).astype(np.float32) / 255.0
    return a


def srgb_to_lin(c):
    c = np.clip(c, 0.0, 1.0)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def lin_to_srgb(c):
    c = np.clip(c, 0.0, 1.0)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def lum(lin_rgb):
    return lin_rgb[..., 0] * 0.2126 + lin_rgb[..., 1] * 0.7152 + lin_rgb[..., 2] * 0.0722


def resize(a, tw, th, pad=64):
    """Periodic Lanczos resize: wrap-pad, then resample the inner box."""
    h, w = a.shape[:2]
    if (w, h) == (tw, th):
        return a.copy()
    pad = min(pad, w // 2, h // 2)
    widths = ((pad, pad), (pad, pad)) + ((0, 0),) * (a.ndim - 2)
    ap = np.pad(a, widths, mode="wrap").astype(np.float32)
    chans = [ap] if a.ndim == 2 else [ap[..., i] for i in range(a.shape[2])]
    out = []
    for c in chans:
        im = Image.fromarray(np.ascontiguousarray(c), "F")
        r = im.resize((tw, th), Image.LANCZOS, box=(pad, pad, pad + w, pad + h))
        out.append(np.asarray(r))
    return out[0] if a.ndim == 2 else np.stack(out, -1)


def decode_normal(rgb):
    n = rgb * 2.0 - 1.0
    return n / np.linalg.norm(n, axis=-1, keepdims=True).clip(1e-6)


def scale_normal(n, k):
    n = n.copy()
    n[..., 0] *= k
    n[..., 1] *= k
    n[..., 2] = np.maximum(n[..., 2], 1e-3)
    return n / np.linalg.norm(n, axis=-1, keepdims=True)


def resize_normal(n, tw, th):
    r = resize(n, tw, th)
    r[..., 2] = np.maximum(r[..., 2], 1e-3)
    return r / np.linalg.norm(r, axis=-1, keepdims=True)


def height_to_normal(h, pixel_m):
    """h in metres (periodic). OpenGL convention: +Y (green) = up in the texture."""
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) / (2 * pixel_m)
    drow = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) / (2 * pixel_m)
    n = np.stack([-dx, drow, np.ones_like(h)], -1)
    return n / np.linalg.norm(n, axis=-1, keepdims=True)


def blend_normals(a, b):
    """Reoriented-normal style detail blend (b on top of a)."""
    n = np.stack([a[..., 0] + b[..., 0], a[..., 1] + b[..., 1], a[..., 2] * b[..., 2]], -1)
    return n / np.linalg.norm(n, axis=-1, keepdims=True)


def pnoise(n, feature_px, seed, m=None):
    """Periodic band-limited noise, zero mean, unit std. Size n x m."""
    m = m or n
    rng = np.random.default_rng(seed)
    w = rng.standard_normal((n, m))
    fy = np.fft.fftfreq(n)[:, None]
    fx = np.fft.fftfreq(m)[None, :]
    r = np.sqrt(fx * fx + fy * fy)
    filt = np.exp(-((r * feature_px) ** 2))
    out = np.real(np.fft.ifft2(np.fft.fft2(w) * filt))
    out -= out.mean()
    return (out / (out.std() + 1e-9)).astype(np.float32)


def fbm(n, base_px, seed, octaves=4, gain=0.5, m=None):
    acc = np.zeros((n, m or n), np.float32)
    amp, tot = 1.0, 0.0
    for o in range(octaves):
        acc += amp * pnoise(n, base_px / (2 ** o), seed + 101 * o, m)
        tot += amp
        amp *= gain
    return acc / tot


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def gaussian_filter(a, sigma, mode="wrap"):
    """Periodic Gaussian blur (FFT), numpy only."""
    fy = np.fft.fftfreq(a.shape[0])[:, None]
    fx = np.fft.fftfreq(a.shape[1])[None, :]
    g = np.exp(-2 * (np.pi ** 2) * (sigma ** 2) * (fx * fx + fy * fy))
    return np.real(np.fft.ifft2(np.fft.fft2(a) * g)).astype(np.float32)


def maximum_filter(a, size, mode="wrap"):
    out = a.copy()
    r = size // 2
    for ax in (0, 1):
        base = out.copy()
        for k in range(-r, r + 1):
            out = np.maximum(out, np.roll(base, k, ax))
    return out


def median_filter(a, size, mode="wrap"):
    """Periodic sparse median (5 x 5 taps spread over size x size)."""
    offs = np.round(np.linspace(-(size // 2), size // 2, 5)).astype(int)
    stack = np.stack([np.roll(np.roll(a, dy, 0), dx, 1) for dy in offs for dx in offs], 0)
    return np.median(stack, axis=0).astype(np.float32)


WRITTEN = []


def save_color(name, srgb, q=90, subsampling=2):
    p = os.path.join(OUT, f"{name}_Color.jpg")
    Image.fromarray((np.clip(srgb, 0, 1) * 255 + 0.5).astype(np.uint8), "RGB").save(
        p, quality=q, optimize=True, subsampling=subsampling)
    WRITTEN.append(p)


def save_normal(name, n):
    p = os.path.join(OUT, f"{name}_Normal.png")
    img = (np.clip(n * 0.5 + 0.5, 0, 1) * 255 + 0.5).astype(np.uint8)
    Image.fromarray(img, "RGB").save(p, optimize=True, compress_level=9)
    WRITTEN.append(p)


def save_ms(name, metallic, rough, size=1024):
    """R = metallic, G = B = 0, A = smoothness = 1 - roughness. 1024 px (aspect kept)."""
    h, w = rough.shape
    tw, th = (size, int(round(size * h / w))) if w >= h else (int(round(size * w / h)), size)
    r = np.clip(resize(rough, tw, th), 0, 1)
    if np.isscalar(metallic):
        met = np.full((th, tw), float(metallic), np.float32)
    else:
        met = np.clip(resize(metallic, tw, th), 0, 1)
    rgba = np.zeros((th, tw, 4), np.uint8)
    rgba[..., 0] = (met * 255 + 0.5).astype(np.uint8)
    rgba[..., 3] = ((1.0 - r) * 255 + 0.5).astype(np.uint8)
    p = os.path.join(OUT, f"{name}_MetallicSmoothness.png")
    Image.fromarray(rgba, "RGBA").save(p, optimize=True, compress_level=9)
    WRITTEN.append(p)


def save_ao(name, ao, size=1024):
    h, w = ao.shape
    a = np.clip(resize(ao, size, int(round(size * h / w))), 0, 1)
    p = os.path.join(OUT, f"{name}_AO.jpg")
    Image.fromarray((a * 255 + 0.5).astype(np.uint8), "L").save(p, quality=90, optimize=True)
    WRITTEN.append(p)


def recolor(lin_src, target_srgb, contrast=1.0, keep_chroma=0.0):
    """Map the luminance variation of a source onto a new linear base colour."""
    L = lum(lin_src)
    rel = L / max(float(L.mean()), 1e-5)
    rel = np.power(np.clip(rel, 1e-4, None), contrast)
    base = srgb_to_lin(np.array(target_srgb, np.float32) / 255.0)
    out = rel[..., None] * base[None, None, :]
    if keep_chroma > 0:
        chroma = lin_src / np.clip(L[..., None], 1e-4, None)
        chroma /= chroma.reshape(-1, 3).mean(0)
        out *= (1 - keep_chroma) + keep_chroma * chroma
    return out


def scale_to_mean(lin, target_srgb):
    tgt = srgb_to_lin(np.array(target_srgb, np.float32) / 255.0)
    return lin * (tgt / lin.reshape(-1, 3).mean(0).clip(1e-5))[None, None, :]


def ambient(name, set_name, color_size, normal_size, rough_fn, metallic=0.0,
            normal_k=1.0, color_fn=None, ao=False, flip=None):
    """Standard path for an ambientCG set: optional colour/roughness remap and resize."""
    col = srgb_to_lin(load(set_name, "Color"))
    nrm = decode_normal(load(set_name, "NormalGL"))
    rgh = load(set_name, "Roughness")
    if flip:
        col, nrm, rgh = flip(col, nrm, rgh)
    if color_fn:
        col = color_fn(col)
    h, w = col.shape[:2]
    cw, ch = (color_size, int(round(color_size * h / w)))
    save_color(name, lin_to_srgb(resize(col, cw, ch)))
    if normal_size:      # None: the source normal is flat, ship no normal map
        nw, nh = (normal_size, int(round(normal_size * h / w)))
        save_normal(name, scale_normal(resize_normal(nrm, nw, nh), normal_k))
    met = metallic if np.isscalar(metallic) else metallic(load(set_name, "Metalness"))
    save_ms(name, met, rough_fn(rgh))
    if ao:
        save_ao(name, load(set_name, "AmbientOcclusion"))


# =============================================================== materials
def build_floor_tile():
    """Large-format polished marble tiles, 4 x 4 tiles of 60 cm (2.4 m repeat).

    Each tile is a native-resolution 1024 px window of ambientCG Marble021
    (rotated / mirrored, warm-tinted, per-tile tone shift). Layout, grout,
    bevel and lippage are procedural. Built at 4096 px (0.586 mm/px), output
    2048 px colour and normal (1.17 mm/px)."""
    S = "Marble021_2K-JPG"
    col = srgb_to_lin(load(S, "Color"))
    rgh = load(S, "Roughness")
    N, T = 4096, 1024                     # canvas, tile size in px (60 cm)
    px_m = 2.4 / N
    rng = np.random.default_rng(21)
    out_c = np.zeros((N, N, 3), np.float32)
    out_r = np.zeros((N, N), np.float32)
    height = np.zeros((N, N), np.float32)
    tint = srgb_to_lin(np.array([233, 224, 207], np.float32) / 255.0)
    src_mean = col.reshape(-1, 3).mean(0)
    yy, xx = np.mgrid[0:T, 0:T].astype(np.float32)
    for ty in range(4):
        for tx in range(4):
            ox, oy = rng.integers(0, 2048, 2)
            win_c = np.roll(np.roll(col, -oy, 0), -ox, 1)[:T, :T]
            win_r = np.roll(np.roll(rgh, -oy, 0), -ox, 1)[:T, :T]
            k = int(rng.integers(0, 4))
            win_c = np.rot90(win_c, k)
            win_r = np.rot90(win_r, k)
            if rng.random() < 0.5:
                win_c = win_c[:, ::-1]
                win_r = win_r[:, ::-1]
            tone = 1.0 + rng.normal(0, 0.025)
            warm = 1.0 + rng.normal(0, 0.012)
            c = win_c / src_mean * tint * tone
            c[..., 0] *= warm
            c[..., 2] /= warm
            # veins a touch stronger so they read at night
            L = lum(c)
            Lm = L.mean()
            c = c * (1.0 + 0.35 * (L / Lm - 1.0))[..., None]
            out_c[ty * T:(ty + 1) * T, tx * T:(tx + 1) * T] = c
            out_r[ty * T:(ty + 1) * T, tx * T:(tx + 1) * T] = win_r
            # lippage: tiny plane tilt per tile (max ~0.25 mm)
            gx, gy = rng.normal(0, 0.00012, 2)
            height[ty * T:(ty + 1) * T, tx * T:(tx + 1) * T] = (
                gx * (xx / T - 0.5) + gy * (yy / T - 0.5))
    # grout lines on tile borders: 2.5 mm joint, 1 mm micro-bevel, 1.5 mm deep
    coord = np.arange(N, dtype=np.float32) + 0.5
    d1 = np.minimum(coord % T, T - (coord % T))          # px to nearest joint centre
    dist = np.minimum(d1[None, :], d1[:, None]) * px_m * 1000.0   # mm
    half_joint = 1.25
    grout = 1.0 - smoothstep(half_joint - 0.3, half_joint + 0.3, dist)
    bevel = 1.0 - smoothstep(half_joint, half_joint + 1.2, dist)
    height = height - 0.0015 * grout - 0.0004 * bevel * (1 - grout)
    gcol = srgb_to_lin(np.array([156, 147, 133], np.float32) / 255.0)
    gnoise = 1.0 + 0.06 * pnoise(N, 6, 5)
    out_c = out_c * (1 - grout[..., None]) + (gcol[None, None, :] * gnoise[..., None]) * grout[..., None]
    # polished stone 0.06..0.2 + soft traffic wear haze
    wear = fbm(N, 600, 77, 3)
    r_tile = np.clip(0.05 + 0.55 * out_r + 0.03 * wear, 0.04, 0.3)
    r_final = r_tile * (1 - grout) + 0.85 * grout
    # source micro relief from roughness variation (polished: very subtle)
    micro = (out_r - out_r.mean()) * 0.00002
    n = height_to_normal(height + micro, px_m)
    save_color("FloorTile", lin_to_srgb(resize(out_c, 2048, 2048)))
    save_normal("FloorTile", resize_normal(n, 2048, 2048))
    save_ms("FloorTile", 0.0, r_final)
    ao = 1.0 - 0.45 * grout - 0.15 * bevel * (1 - grout)
    save_ao("FloorTile", ao)


def build_stone_top():
    # Marble019's NormalGL is flat (polished slab): no normal map is shipped.
    S = "Marble019_2K-JPG"
    ambient("StoneTop", S, 2048, None,
            rough_fn=lambda r: np.clip(0.04 + 0.3 * r, 0.04, 0.25),   # re-polished
            normal_k=1.0,
            color_fn=lambda c: scale_to_mean(c, [236, 235, 232]))


def equalize_low(lin, sigma_px, amount):
    """Flatten very low-frequency brightness drift (reduces visible tiling)."""
    L = lum(lin)
    low = gaussian_filter(L, sigma_px, mode="wrap")
    gain = np.power(low.mean() / np.clip(low, 1e-5, None), amount)
    return lin * gain[..., None]


def build_walnut():
    S = "Wood067_2K-JPG"

    def col(c):
        # pull the orange cast toward a neutral chocolate walnut, keep figure;
        # halve the broad dark/light bands that would repeat every metre
        L = lum(c)[..., None]
        c = L + (c - L) * 0.62
        c = equalize_low(c, 160, 0.5)
        return scale_to_mean(c, [74, 50, 36])
    ambient("Walnut", S, 2048, 1024,
            rough_fn=lambda r: np.clip(r * 1.05, 0.2, 0.5),   # satin lacquer
            normal_k=1.0, color_fn=col)


CRACK_SHIFTS = [(0, 173), (97, -211), (-151, 61), (233, 389), (-301, -97)]


def crack_mask(col):
    """Thin dark lines (hairline cracks) of a plaster colour map, 0..1."""
    L = lum(col)
    med = median_filter(L, size=11, mode="wrap")
    dark = smoothstep(0.03, 0.07, med - L)
    # keep only connected thin lines: isolated pits fade under the blur
    crack = smoothstep(0.16, 0.3, gaussian_filter(dark, 2.0, mode="wrap"))
    crack = maximum_filter(crack, size=7, mode="wrap")
    return np.clip(gaussian_filter(crack, 1.0, mode="wrap"), 0.0, 1.0)


def decrack(col, nrm, rgh):
    """Remove the hairline cracks of PaintedPlaster017 (new, sound wall finish).

    The cracks exist only in the source's colour map; its NormalGL has no
    crack relief, so the normal is returned untouched, and the roughness is
    patched with the same shifted copies to stay consistent. Each
    crack pixel is filled from a shifted copy of the same plaster (the first
    shift whose own pixel is crack-free), tone-matched to the surrounding
    plaster with a normalised local mean. The fill therefore keeps the plaster
    grain instead of leaving a smooth, darker channel."""
    crack = crack_mask(col)
    clean = 1.0 - crack
    fill_c = np.zeros_like(col)
    fill_r = np.zeros_like(rgh)
    left = np.ones_like(crack)
    for dy, dx in CRACK_SHIFTS:
        m_s = np.roll(np.roll(crack, dy, 0), dx, 1)
        w = left * (1.0 - m_s)
        fill_c += w[..., None] * np.roll(np.roll(col, dy, 0), dx, 1)
        fill_r += w * np.roll(np.roll(rgh, dy, 0), dx, 1)
        left = left * m_s
    # pixels cracked in every shifted copy (none expected): local median
    fill_c += left[..., None] * np.stack(
        [median_filter(col[..., i], size=11, mode="wrap") for i in range(3)], -1)
    fill_r += left * median_filter(rgh, size=11, mode="wrap")

    def local_mean(a, wgt, sigma=6.0):
        return gaussian_filter(a * wgt, sigma) / np.clip(gaussian_filter(wgt, sigma), 1e-3, None)
    for i in range(3):      # match the fill to the clean plaster around it
        fill_c[..., i] *= local_mean(col[..., i], clean) / np.clip(
            gaussian_filter(fill_c[..., i], 6.0), 1e-5, None)
    fill_r += local_mean(rgh, clean) - gaussian_filter(fill_r, 6.0)
    col = col * clean[..., None] + fill_c * crack[..., None]
    rgh = rgh * clean + fill_r * crack
    return col, nrm, rgh


def build_plaster_warm():
    # Normal: the scanned NormalGL untouched apart from resampling and the
    # strength 0.45 baked in (smooth wall). Cracks are removed from the colour
    # (roughness patched to match), see decrack.
    S = "PaintedPlaster017_2K-JPG"
    ambient("PlasterWarm", S, 2048, 1024,
            rough_fn=lambda r: np.clip(0.82 + (r - r.mean()) * 0.6, 0.65, 0.95),
            normal_k=0.45, flip=decrack,
            color_fn=lambda c: recolor(c, [214, 203, 186], contrast=0.6))


def build_ceiling_paint(stipple_m=90e-6):
    """Original procedural matte ceiling emulsion on plasterboard, 2 m x 2 m.

    Built on a 1024 px grid (1.95 mm/px): rounded roller stipple of 3-6 mm
    with about 0.09 mm relief (normal 99th-percentile tilt about 5 degrees),
    nine roller lanes of 22 cm (one per 2 m / 9) whose stipple strength, tone
    (about +/- 2 %) and sheen differ slightly, wavy lane boundaries with
    intermittent 'tram line' paint ridges, and a low-frequency tonal drift.
    Colour is saved at 512 px (3.9 mm/px: tone, lanes and drift only); normal
    and MetallicSmoothness at 1024 px."""
    N = 1024
    px_m = 2.0 / N
    rng = np.random.default_rng(57)
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    # roller lanes run along V (rows); lane index changes across U (columns)
    lanes = 9
    lane_w = N / lanes
    wobble = 7.0 * pnoise(N, 90, 58)[:, :1]           # +/- 14 mm boundary wander
    wobble = np.repeat(wobble, N, 1)
    u = (xx + wobble) % N
    lane_f = u / lane_w
    idx = np.floor(lane_f).astype(int) % lanes
    frac = lane_f - np.floor(lane_f)
    amp_l = rng.uniform(0.75, 1.25, lanes).astype(np.float32)
    tone_l = rng.normal(0.0, 0.018, lanes).astype(np.float32)
    rough_l = rng.normal(0.0, 0.02, lanes).astype(np.float32)
    # soft 3 cm overlap where one lane rolls over the next
    t = smoothstep(0.0, 0.07, frac) * (1 - smoothstep(0.93, 1.0, frac))
    nxt = (idx + 1) % lanes
    prv = (idx - 1) % lanes
    edge_mix = np.where(frac < 0.5, prv, nxt)

    def lane_val(v):
        return v[idx] * t + 0.5 * (v[idx] + v[edge_mix]) * (1 - t)
    amp = lane_val(amp_l)
    tone_lane = lane_val(tone_l)
    rough_lane = lane_val(rough_l)
    # stipple: rounded peaks, slightly sharper pits (paint pulled by the nap)
    s = 0.65 * pnoise(N, 1.6, 51) + 0.35 * pnoise(N, 3.2, 52)
    s = np.tanh(1.3 * s) - 0.15 * np.maximum(-s - 0.8, 0) ** 2
    s = (s - s.mean()) / s.std()
    # tram lines: thin intermittent ridges of excess paint at lane edges
    d_edge = np.minimum(frac, 1 - frac) * lane_w          # px to lane boundary
    ridge = np.exp(-(d_edge / 1.4) ** 2) * np.clip(pnoise(N, 40, 59) * 0.9 + 0.2, 0, 1)
    swell = 0.5 * pnoise(N, 60, 53)                       # broad paint thickness
    h = stipple_m * (amp * s + 0.9 * ridge + 0.6 * swell)
    n = height_to_normal(h, px_m)
    save_normal("CeilingPaint", n)
    # tone: lanes, low drift, slight darkening in the stipple pits (dirt/shade)
    base = srgb_to_lin(np.array([232, 230, 224], np.float32) / 255.0)
    drift = 0.02 * fbm(N, 300, 55, 3) + 0.008 * pnoise(N, 25, 56)
    tone = 1.0 + tone_lane + drift + 0.006 * np.clip(s, -3, 1) + 0.01 * ridge
    col = base[None, None, :] * tone[..., None]
    save_color("CeilingPaint", lin_to_srgb(resize(col, 512, 512)))
    rough = np.clip(0.9 + rough_lane - 0.035 * np.clip(s, -2, 2) - 0.05 * ridge
                    + 0.015 * fbm(N, 120, 60, 3), 0.8, 0.97)
    save_ms("CeilingPaint", 0.0, rough)


def pile_normal(n, feature_px, strength, seed):
    """Fine isotropic pile / nap relief (original procedural)."""
    h = 0.75 * pnoise(n, feature_px, seed) + 0.25 * pnoise(n, feature_px * 3, seed + 1)
    nn = np.stack([-strength * (np.roll(h, -1, 1) - np.roll(h, 1, 1)),
                   strength * (np.roll(h, -1, 0) - np.roll(h, 1, 0)),
                   np.ones_like(h)], -1)
    return nn / np.linalg.norm(nn, axis=-1, keepdims=True)


def soft_fabric(name, set_name, target_srgb, contrast, blur_px, rough_base,
                rough_var, pile_strength, src_normal_k, seed):
    """Fabric with the source's crush / mottle shading but without its weave
    lines (blurred away) and an original fine pile normal. 1024 px, 0.5 m."""
    c = srgb_to_lin(load(set_name, "Color"))
    c = np.stack([gaussian_filter(c[..., i], blur_px, mode="wrap") for i in range(3)], -1)
    c = recolor(c, target_srgb, contrast=contrast)
    r = gaussian_filter(load(set_name, "Roughness"), blur_px, mode="wrap")
    n_src = decode_normal(load(set_name, "NormalGL"))
    n_src = np.stack([gaussian_filter(n_src[..., i], blur_px * 2, mode="wrap") for i in range(3)], -1)
    n_src = scale_normal(resize_normal(n_src, 1024, 1024), src_normal_k)
    n = blend_normals(n_src, pile_normal(1024, 1.5, pile_strength, seed))
    save_color(name, lin_to_srgb(resize(c, 1024, 1024)))
    save_normal(name, n)
    save_ms(name, 0.0, np.clip(rough_base + (r - r.mean()) * rough_var, 0.5, 0.98))


def build_velvet():
    # Fabric023 crush shading, ribs removed; charcoal; velvet-like high roughness
    soft_fabric("VelvetCharcoal", "Fabric023_2K-JPG", [50, 50, 54], contrast=1.1,
                blur_px=2.5, rough_base=0.82, rough_var=0.6, pile_strength=0.09,
                src_normal_k=1.5, seed=61)


def build_suede():
    # Fabric028 mottle, stripes removed; cream; matte nap
    soft_fabric("SuedeCream", "Fabric028_2K-JPG", [214, 199, 172], contrast=0.55,
                blur_px=3.0, rough_base=0.9, rough_var=0.3, pile_strength=0.07,
                src_normal_k=1.2, seed=71)


def build_bronze():
    S = "Metal009_2K-JPG"
    ambient("BronzeBrushed", S, 1024, 1024,
            rough_fn=lambda r: np.clip(r * 0.82, 0.18, 0.62),
            metallic=1.0, normal_k=1.0,
            color_fn=lambda c: recolor(c, [203, 168, 118], contrast=1.0))


def build_brushed_steel():
    S = "Metal009_2K-JPG"
    ambient("BrushedSteel", S, 1024, 1024,
            rough_fn=lambda r: np.clip(r, 0.2, 0.75),
            metallic=1.0, normal_k=1.0,
            color_fn=lambda c: recolor(c, [192, 194, 196], contrast=1.0))


def build_chrome():
    # Metal049A's NormalGL is flat: no normal map is shipped.
    S = "Metal049A_2K-JPG"
    ambient("Chrome", S, 1024, None,
            rough_fn=lambda r: np.clip(r * 1.2, 0.02, 0.12),
            metallic=1.0, normal_k=1.0,
            color_fn=lambda c: recolor(c, [196, 197, 198], contrast=1.0))


def build_leather():
    S = "Leather027_2K-JPG"
    ambient("Leather", S, 1024, 1024,
            rough_fn=lambda r: np.clip(r, 0.3, 0.65),
            normal_k=1.0,
            color_fn=lambda c: scale_to_mean(c, [31, 30, 30]))


def build_carpet():
    S = "Carpet012_2K-JPG"

    def col(c):
        L = lum(c)[..., None]
        c = L + (c - L) * 0.45
        return scale_to_mean(c, [50, 53, 60])
    ambient("Carpet", S, 1024, 1024,
            rough_fn=lambda r: np.clip(r, 0.75, 1.0),
            normal_k=1.0, color_fn=col, ao=True)


def build_terrazzo():
    S = "Terrazzo005_2K-JPG"
    ambient("Terrazzo", S, 2048, 1024,
            rough_fn=lambda r: np.clip(0.03 + r * 0.55, 0.04, 0.32),
            normal_k=1.0)


def build_safe_steel(peel_m=19e-6):
    """Original procedural satin powder coat (dark graphite) on steel, 0.5 m.

    Built at 2048 px (0.24 mm/px), saved at 1024 px (0.49 mm/px). Orange peel
    of 2-4 mm with about 20 micrometre relief (normal 99th-percentile tilt
    about 4 degrees at 1024 px); seven clusters of near-parallel fine
    scratches plus nine isolated faint ones, intermittent along their length,
    lighter than the coat (towards the grey primer, about +22 sRGB levels at
    the 99.9th percentile and up to about +45 at the strongest centres) and
    rougher (smoothness down by 0.1-0.4 at the centres); two short gouges through to
    bare steel (metallic, bright); burnished patches where hands and keys rub
    (smoother, up to +0.1) and a faint low-frequency mottle."""
    N = 2048
    px_m = 0.5 / N
    rng = np.random.default_rng(13)
    peel = 0.6 * pnoise(N, 14, 11) + 0.4 * pnoise(N, 7, 12)
    peel = (peel - peel.mean()) / peel.std()
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)

    def segment(x0, y0, ang, length, width):
        dx, dy = np.cos(ang), np.sin(ang)
        px = (xx - x0 + N / 2) % N - N / 2          # periodic offsets
        py = (yy - y0 + N / 2) % N - N / 2
        t = np.clip(px * dx + py * dy, 0, length)
        d = np.hypot(px - t * dx, py - t * dy)
        taper = np.sin(np.pi * np.clip(t / length, 0, 1)) ** 0.35   # thin ends
        return np.exp(-(d / (width * (0.35 + 0.65 * taper))) ** 2)
    scratch = np.zeros((N, N), np.float32)            # fine scratches
    # clusters of near-parallel scratches (keys, tools, boxes sliding past)
    for _ in range(7):
        cx, cy = rng.uniform(0, N, 2)
        base_ang = rng.normal(0, 0.2) if rng.random() < 0.6 else rng.uniform(0, np.pi)
        for _ in range(int(rng.integers(3, 7))):
            off, along = rng.normal(0, 40), rng.normal(0, 60)
            x0 = cx - off * np.sin(base_ang) + along * np.cos(base_ang)
            y0 = cy + off * np.cos(base_ang) + along * np.sin(base_ang)
            s = segment(x0, y0, base_ang + rng.normal(0, 0.05), rng.uniform(60, 380),
                        rng.uniform(1.0, 2.0))
            scratch = np.maximum(scratch, s * rng.uniform(0.3, 1.0))
    for _ in range(9):                                # isolated faint ones
        x0, y0 = rng.uniform(0, N, 2)
        s = segment(x0, y0, rng.uniform(0, np.pi), rng.uniform(50, 300), rng.uniform(0.8, 1.4))
        scratch = np.maximum(scratch, s * rng.uniform(0.2, 0.5))
    # intermittent along their length, as real scratches skip
    scratch *= np.clip(0.55 + 0.6 * pnoise(N, 5, 14), 0.12, 1.0)
    gouge = np.zeros((N, N), np.float32)              # short, to bare metal
    for _ in range(2):
        x0, y0 = rng.uniform(0, N, 2)
        s = segment(x0, y0, rng.uniform(0, np.pi), rng.uniform(90, 180), rng.uniform(2.0, 3.0))
        gouge = np.maximum(gouge, s)
    gouge *= np.clip(0.7 + 0.5 * pnoise(N, 8, 15), 0.3, 1.0)
    burnish = smoothstep(0.55, 1.4, fbm(N, 260, 17, 3)) * 0.9
    mott = 0.02 * fbm(N, 400, 19, 3)
    h = peel_m * peel - 12e-6 * scratch - 40e-6 * gouge
    n = height_to_normal(h, px_m)
    base = srgb_to_lin(np.array([60, 62, 65], np.float32) / 255.0)
    primer = srgb_to_lin(np.array([112, 112, 108], np.float32) / 255.0)
    steel = srgb_to_lin(np.array([168, 170, 172], np.float32) / 255.0)
    col = base[None, None, :] * (1.0 + mott + 0.012 * peel)[..., None]
    sc = 0.85 * scratch
    col = col * (1 - sc[..., None]) + primer[None, None, :] * sc[..., None]
    col = col * (1 - gouge[..., None]) + steel[None, None, :] * gouge[..., None]
    rough = (0.44 + 0.025 * peel - 0.12 * burnish + 0.4 * scratch) * (1 - gouge) + 0.3 * gouge
    rough = np.clip(rough, 0.2, 0.85)
    met = np.clip(gouge * 1.15, 0, 1)
    save_color("SafeSteel", lin_to_srgb(resize(col, 1024, 1024)))
    save_normal("SafeSteel", resize_normal(n, 1024, 1024))
    save_ms("SafeSteel", met, rough)


def build_wallpaper():
    """Original tone-on-tone ogee wallpaper printed on ambientCG Wallpaper001A paper.
    Tile 0.5 m x 0.5 m (2 x 2 ogee repeats of 25 cm)."""
    S = "Wallpaper001A_2K-JPG"
    N = 4096                                     # supersampled canvas
    px_m = 0.5 / N
    u = (np.arange(N, dtype=np.float32) + 0.5) / N * 2.0     # 2 repeats
    U, V = np.meshgrid(u, u)
    fu, fv = U % 1.0, V % 1.0
    A = 0.25
    ink = np.zeros((N, N), np.float32)

    def curve_band(sign, shift, w_mm, offset=0.0):
        # distance (in cell units) to the curve family v = sign*A*sin(2*pi*u) + shift + k
        f = sign * A * np.sin(2 * np.pi * fu) + shift + offset
        slope = sign * A * 2 * np.pi * np.cos(2 * np.pi * fu)
        d = np.abs(((fv - f + 0.5) % 1.0) - 0.5) / np.sqrt(1 + slope ** 2)
        w = w_mm / 250.0                          # cell = 250 mm
        aa = 0.6 / (N / 2)                        # ~0.6 px antialias at canvas res
        return 1.0 - smoothstep(w / 2 - aa, w / 2 + aa, d)
    ink = np.maximum(ink, curve_band(1, 0.0, 2.6))
    ink = np.maximum(ink, curve_band(-1, 0.5, 2.6))

    def wrapped(cx, cy):
        dx = ((fu - cx + 0.5) % 1.0) - 0.5
        dy = ((fv - cy + 0.5) % 1.0) - 0.5
        return dx, dy
    aa = 1.0 / (N / 2)
    # junction medallions where the ogee curves kiss: lozenge outline + dot
    for cx, cy in ((0.25, 0.25), (0.75, 0.75)):
        dx, dy = wrapped(cx, cy)
        diamond = np.abs(dx) / 0.06 + np.abs(dy) / 0.042        # 1 = edge
        fill = 1.0 - smoothstep(1.0 - aa * 18, 1.0 + aa * 18, diamond)
        hole = 1.0 - smoothstep(0.78 - aa * 18, 0.78 + aa * 18, diamond)
        r = np.hypot(dx, dy)
        dot = 1.0 - smoothstep(0.012 - aa, 0.012 + aa, r)
        ink = np.maximum(ink * (1 - fill), np.maximum(fill - hole, dot))
    # four-petal flower with centre dot at the ogee centres
    for cx, cy in ((0.25, 0.75), (0.75, 0.25)):
        dx, dy = wrapped(cx, cy)
        r = np.hypot(dx, dy)
        th = np.arctan2(dy, dx)
        petal_r = 0.07 * np.abs(np.cos(2 * th)) ** 0.8
        petals = 1.0 - smoothstep(petal_r - 0.003, petal_r + 0.002, r)
        inner = 1.0 - smoothstep(petal_r * 0.62 - 0.002, petal_r * 0.62 + 0.002, r)
        petal_ring = np.clip(petals - 0.8 * inner, 0, 1)        # outlined petals
        dot = 1.0 - smoothstep(0.01 - aa, 0.01 + aa, r)
        ink = np.maximum(ink, np.maximum(petal_ring, dot))
    ink = resize(ink, 2048, 2048)
    ink = np.clip(ink, 0, 1)
    # paper from Wallpaper001A: luminance variation and fibre relief
    pc = srgb_to_lin(load(S, "Color"))
    pn = decode_normal(load(S, "NormalGL"))
    pr = load(S, "Roughness")
    paper_rel = lum(pc) / lum(pc).mean()
    paper_rel = 1.0 + (paper_rel - 1.0) * 0.6
    ground = srgb_to_lin(np.array([176, 164, 146], np.float32) / 255.0)
    inkc = srgb_to_lin(np.array([206, 193, 170], np.float32) / 255.0)
    col = ground[None, None, :] * (1 - ink[..., None]) + inkc[None, None, :] * ink[..., None]
    col = col * paper_rel[..., None]
    rough = np.clip(0.74 + (pr - pr.mean()) * 0.5, 0.6, 0.85) * (1 - ink) + 0.42 * ink
    # ink sits ~40 micrometres proud (screen print), on top of the paper relief
    ink_h = resize(ink, 1024, 1024) * 4e-5
    n_ink = height_to_normal(ink_h, 0.5 / 1024)
    n_paper = scale_normal(resize_normal(pn, 1024, 1024), 0.6)
    save_color("Wallpaper", lin_to_srgb(col), subsampling=0)
    save_normal("Wallpaper", blend_normals(n_paper, n_ink))
    save_ms("Wallpaper", 0.0, rough)


def build_glass_smudge():
    """Fingerprint / smudge overlay for display glass, 0.6 m x 0.6 m at 2048 px.
    Prints from ambientCG Fingerprints009 (4K) at 0.125 scale (15 cm source tile
    -> 512 px), gated by an original periodic cluster mask, plus original wipe
    arcs and dust."""
    S = "Fingerprints009_4K-JPG"
    N = 2048
    op = load(S, "Opacity")
    rg = load(S, "Roughness")
    op = resize(op, 512, 512)
    rg = resize(rg, 512, 512)
    prints = np.tile(op, (4, 4))
    prints_r = np.tile(rg, (4, 4))
    # cluster mask: where hands touched; mostly clean glass
    m = fbm(N, 260, 31, 3)
    mask = smoothstep(0.15, 0.95, m)
    prints_a = np.clip((prints - 0.08) * 1.25, 0, 1) * mask
    # cloth wipe arcs (very faint, broad)
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    rng = np.random.default_rng(37)
    wipe = np.zeros((N, N), np.float32)
    for _ in range(7):
        cx, cy = rng.uniform(0, N, 2)
        R = rng.uniform(250, 700)
        px = (xx - cx + N / 2) % N - N / 2
        py = (yy - cy + N / 2) % N - N / 2
        r = np.hypot(px, py)
        band = np.exp(-((r - R) / rng.uniform(70, 160)) ** 2)
        streak = 0.5 + 0.5 * np.sin(r * 0.9 + rng.uniform(0, 6.28))   # cloth fibre lines
        ang = np.arctan2(py, px)
        a0 = rng.uniform(-np.pi, np.pi)
        arc = smoothstep(0.0, 0.4, np.cos(ang - a0) - 0.2)
        wipe = np.maximum(wipe, band * arc * (0.75 + 0.25 * streak))
    wipe *= 0.06 * (0.5 + 0.5 * smoothstep(-0.5, 0.8, fbm(N, 180, 39, 3)))
    dust = np.clip(pnoise(N, 0.7, 41) - 3.2, 0, 1) * 0.9
    alpha = np.clip(0.55 * prints_a + wipe + dust, 0, 0.6)
    # colour: greasy, slightly warm haze
    rgb = np.ones((N, N, 3), np.float32) * np.array([0.86, 0.855, 0.83], np.float32)
    rgba = np.concatenate([rgb, alpha[..., None]], -1)
    p = os.path.join(OUT, "GlassSmudge_ColorAlpha.png")
    Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), "RGBA").save(p, optimize=True, compress_level=9)
    WRITTEN.append(p)
    # roughness: clean glass 0.03, grease up to ~0.55
    rough = np.clip(0.03 + 0.9 * alpha * (0.6 + 0.4 * prints_r), 0.03, 0.6)
    save_ms("GlassSmudge", 0.0, rough)
    # No normal map: grease films are sub-micron, below 8-bit normal precision.
    # The prints read through alpha and the smoothness drop.


def build_stone_top_dark():
    """Extra: black marble with white veins (Nero-Marquina-like) for plinths,
    counter fronts and accent tops."""
    S = "Marble006_2K-JPG"

    def col(c):
        # Floor lift in linear space, c' = FLOOR + k * c, with k chosen so the
        # median lands at sRGB ~38 (source median 16, p5 5, 71 % of pixels
        # below sRGB 30). The lift raises the black ground into the PBR range
        # (p5 >= sRGB 26) while the white veins keep their full contrast; a
        # soft shoulder stops the brightest veins from clipping.
        floor = 0.009
        med = float(np.median(lum(c)))
        k = (float(srgb_to_lin(np.float32(38 / 255))) - floor) / max(med, 1e-5)
        out = floor + k * c
        knee = 0.6
        over = np.clip(out - knee, 0.0, None)
        return np.minimum(out, knee + over / (1.0 + over / (0.92 - knee)))
    # Marble006's NormalGL is flat (polished slab): no normal map is shipped.
    ambient("StoneTopDark", S, 2048, None,
            rough_fn=lambda r: np.clip(0.03 + r * 0.8, 0.04, 0.25),
            color_fn=col)


def build_brass_polished():
    """Extra: polished brass for display hardware, handles and jewellery risers."""
    # Metal048A's NormalGL is flat: no normal map is shipped.
    S = "Metal048A_2K-JPG"
    ambient("BrassPolished", S, 1024, None,
            rough_fn=lambda r: np.clip(r, 0.05, 0.2),
            metallic=1.0, normal_k=1.0,
            color_fn=lambda c: recolor(c, [236, 208, 146], contrast=1.0))


BUILDERS = [build_floor_tile, build_stone_top, build_walnut, build_plaster_warm,
            build_ceiling_paint, build_velvet, build_suede, build_bronze,
            build_safe_steel, build_brushed_steel, build_chrome, build_leather,
            build_carpet, build_terrazzo, build_wallpaper, build_glass_smudge,
            build_stone_top_dark, build_brass_polished]

if __name__ == "__main__":
    only = set(sys.argv[3:])
    for b in BUILDERS:
        if only and b.__name__ not in only:
            continue
        print("building", b.__name__, flush=True)
        b()
    for p in WRITTEN:
        print(os.path.basename(p), os.path.getsize(p))
