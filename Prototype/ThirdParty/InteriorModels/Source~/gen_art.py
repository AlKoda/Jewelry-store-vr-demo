"""Original artwork print for the black poster frame (InteriorModels, project-owned).
gen_art.py OUT.png W H : abstract 'light on dark' fine-art print with a warm off-white mount (passe-partout)."""
import sys, math
import numpy as np
from PIL import Image, ImageFilter

out, W, H = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
rng = np.random.default_rng(11)
y, x = np.mgrid[0:H, 0:W].astype(np.float32)
# mount
mount = np.dstack([np.full((H, W), 236.0), np.full((H, W), 231.0), np.full((H, W), 220.0)])
mount += (rng.standard_normal((H, W, 1)) * 2.2)
m = int(W * 0.13); mt = int(H * 0.11); mb = int(H * 0.14)
ix0, ix1, iy0, iy1 = m, W - m, mt, H - mb
iw, ih = ix1 - ix0, iy1 - iy0
u = (x[iy0:iy1, ix0:ix1] - ix0) / iw; v = (y[iy0:iy1, ix0:ix1] - iy0) / ih
# print: minimalist long-exposure seascape at dusk (muted colour photograph look)
hz = 0.6 + 0 * u
sky_t = np.clip(v / hz, 0, 1)
top = np.array([62, 74, 92]); mid = np.array([196, 176, 160]); low = np.array([238, 206, 170])
sky = np.where(sky_t[..., None] < 0.7, top + (mid - top) * (sky_t[..., None] / 0.7), mid + (low - mid) * ((sky_t[..., None] - 0.7) / 0.3))
sx, sy = 0.62, hz - 0.035
ds = np.sqrt(((u - sx) * iw / ih) ** 2 + (v - sy) ** 2)
sky = sky + np.exp(-(ds / 0.05) ** 2)[..., None] * np.array([60, 40, 10]) + np.exp(-(ds / 0.22) ** 2)[..., None] * np.array([30, 18, 0])
# faint cloud band
from PIL import Image as _I
def fbm(w, h, sc, seed, oct=5):
    r = np.random.default_rng(seed); acc = np.zeros((h, w), np.float32); amp = 1; tot = 0
    for o in range(oct):
        gw, gh = max(2, int(w / sc * 2 ** o)), max(2, int(h / sc * 2 ** o) // 4 + 2)
        g = r.random((gh, gw)).astype(np.float32)
        acc += np.asarray(_I.fromarray((g * 255).astype(np.uint8)).resize((w, h), _I.BICUBIC), np.float32) / 255 * amp; tot += amp; amp *= 0.5
    return acc / tot
cl = fbm(iw, ih, 220, 3)
cloud = np.clip((cl - 0.5) * 3, 0, 1) * np.exp(-((v - 0.3) / 0.12) ** 2)
sky = sky * (1 - 0.25 * cloud[..., None]) + cloud[..., None] * np.array([214, 190, 176]) * 0.25
# sea: long-exposure smooth water, darker towards the viewer, soft sun path
sea_t = np.clip((v - hz) / (1 - hz), 0, 1)
seac = np.array([150, 140, 136]) * (1 - sea_t[..., None]) + np.array([46, 54, 62]) * sea_t[..., None]
streak = fbm(iw, ih, 400, 5, 6)
streak = np.asarray(_I.fromarray((streak * 255).astype(np.uint8)).resize((iw // 40 + 1, ih), _I.BILINEAR).resize((iw, ih), _I.BICUBIC), np.float32) / 255
path = np.exp(-((u - sx) / (0.05 + 0.25 * sea_t)) ** 2) * (1 - sea_t) ** 1.5
seac = seac * (0.92 + 0.16 * streak[..., None]) + path[..., None] * np.array([120, 92, 50])
img = np.where((v > hz)[..., None], seac, sky)
# rocks: a low dark headland at the left edge of the horizon
prof = 0.05 * np.clip(1 - u / 0.3, 0, 1) ** 1.6 + 0.006 * (fbm(iw, ih, 25, 9)[0:1, :] - 0.5) * (u < 0.3)
rock = (v > hz - prof) & (u < 0.3) & (v <= hz + 0.002)
img[rock] = img[rock] * 0.25 + np.array([30, 30, 34]) * 0.75
vig = 1 - 0.28 * (((u - 0.5) / 0.7) ** 2 + ((v - 0.5) / 0.75) ** 2)
img = img * vig[..., None]
img = np.asarray(Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8))).astype(np.float32)
img += rng.standard_normal(img.shape) * 3.0  # print grain
mount[iy0:iy1, ix0:ix1] = img
# bevel of the mount window: thin light edge + soft inner shadow
mount[iy0 - 3:iy0, ix0 - 3:ix1 + 3] = 248; mount[iy1:iy1 + 3, ix0 - 3:ix1 + 3] = 210
mount[iy0 - 3:iy1 + 3, ix0 - 3:ix0] = 244; mount[iy0 - 3:iy1 + 3, ix1:ix1 + 3] = 222
sh = np.zeros((H, W), np.float32); sh[iy0:iy0 + 10, ix0:ix1] = np.linspace(0.35, 0, 10)[:, None]
mount *= (1 - sh[..., None])
Image.fromarray(np.clip(mount, 0, 255).astype(np.uint8)).save(out)
print(out, W, H)
