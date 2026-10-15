import numpy as np
from scipy.signal import fftconvolve
from scipy import ndimage
from jrt import tonemap


def star_kernel(size=121, arms=(np.radians(28), np.radians(118)), length=26.0, width=0.7):
    c = size // 2
    y, x = np.mgrid[-c:c + 1, -c:c + 1].astype(np.float32)
    k = np.zeros_like(x)
    for a in arms:
        u = x * np.cos(a) + y * np.sin(a)
        v = -x * np.sin(a) + y * np.cos(a)
        k += np.exp(-np.abs(u) / length) * np.exp(-(v / width) ** 2)
    k[c, c] = 0
    return k / k.sum()


def finish(img, alpha, glint=0.0, bloom=1.0):
    lum = img @ np.array([0.2126, 0.7152, 0.0722], np.float32)
    bright = np.clip(img - 1.0, 0, 8)
    b = sum(np.stack([ndimage.gaussian_filter(bright[..., c], s) for c in range(3)], -1) * w
            for s, w in ((2.0, 0.22), (8.0, 0.12), (30.0, 0.06)))
    out = img + b * bloom
    if glint > 0:
        spikes = np.clip(lum - 5.0, 0, 30)
        spikes = ndimage.maximum_filter(spikes, 3) * (spikes > 0)
        k = star_kernel()
        st = fftconvolve(spikes, k, mode='same').astype(np.float32)
        out = out + st[..., None] * glint * np.array([1.0, 0.97, 0.92], np.float32)
    a = np.clip(alpha + np.clip((b * bloom).max(-1), 0, 1), 0, 1)
    return tonemap(out, 1.0), a


