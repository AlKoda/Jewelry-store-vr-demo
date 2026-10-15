"""Texture conversions used by postprocess.py (project-owned code).
Unity Built-in Standard (metallic workflow) wants a metallic-smoothness map: R = metallic, A = smoothness."""
import numpy as np
from PIL import Image, ImageFilter

def _resize_l(ch, size):
    return ch if ch.size == (size, size) else ch.resize((size, size), Image.LANCZOS)

def _save_ms(metal, smooth, out):
    m = np.clip(metal, 0, 1); s = np.clip(np.broadcast_to(smooth, m.shape), 0, 1)
    rgba = (np.dstack([m, m, m, s]) * 255 + 0.5).astype(np.uint8)
    Image.fromarray(rgba, 'RGBA').save(out, optimize=True)

def ms_map(src, out, size, channels, metallic_factor=1.0, roughness_factor=1.0):
    """glTF metallicRoughness / Poly Haven ARM map -> metallic-smoothness. The channels are resized separately as L
    images (resizing an RGBA image premultiplies by alpha, which wipes RGB wherever smoothness is 0) and the glTF
    metallicFactor / roughnessFactor are applied (metallic = B * mf, smoothness = 1 - G * rf)."""
    ch = Image.open(src).convert('RGB').split()
    metal = np.asarray(_resize_l(ch[channels['metal']], size), np.float32) / 255 * metallic_factor
    rough = np.asarray(_resize_l(ch[channels['rough']], size), np.float32) / 255 * roughness_factor
    _save_ms(metal, 1 - rough, out)

def ms_from_color(src, out, size, smoothness, sat_lo=0.20, sat_hi=0.40, close_px=9):
    """metallic derived from a baked colour map: saturated (gold) texels -> metallic 1, near-white (pearl) texels -> 0.
    A morphological closing (max then min filter) removes the small white specks of baked highlights on the gold;
    a 1 px blur softens the gold/pearl boundary. Smoothness is a constant (the glTF roughnessFactor)."""
    im = Image.open(src).convert('RGB')
    sat = np.asarray(im.convert('HSV'), np.float32)[..., 1] / 255
    x = np.clip((sat - sat_lo) / (sat_hi - sat_lo), 0, 1); x = x * x * (3 - 2 * x)
    mask = Image.fromarray((x * 255 + 0.5).astype(np.uint8), 'L')
    mask = mask.filter(ImageFilter.MaxFilter(close_px)).filter(ImageFilter.MinFilter(close_px)).filter(ImageFilter.GaussianBlur(1.0))
    metal = np.asarray(_resize_l(mask, size), np.float32) / 255
    _save_ms(metal, smoothness, out)
