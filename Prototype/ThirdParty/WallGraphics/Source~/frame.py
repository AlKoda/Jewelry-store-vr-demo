# Gilt picture-frame moulding strip (colour + OpenGL normal map), tileable along U. Project-original.
import sys, os
import numpy as np
from PIL import Image
from gfx import *

OUT = sys.argv[1]
W, H = 1024, 256
u = np.arange(W, dtype=np.float32)[None, :] / W          # along the moulding (tiles)
v = np.arange(H, dtype=np.float32)[:, None] / H          # across: 0 = outer edge (row 0), 1 = sight edge


def band(a, b):
    return ((v >= a) & (v < b)).astype(np.float32)


def bump(a, b):
    t = np.clip((v - a) / (b - a), 0, 1)
    return np.sin(t * np.pi) * band(a, b)


h = np.zeros((H, W), np.float32)
h += 0.55 * bump(0.00, 0.12)                                     # outer rounded lip
h += (0.35 - 0.30 * bump(0.12, 0.34)) * band(0.12, 0.34)          # cove (concave)
# bead row: round beads, 32 per tile
bu = (u * 32) % 1.0
bv = np.clip((v - 0.34) / 0.10, 0, 1)
bead = np.sqrt(np.clip(1 - ((bu - 0.5) / 0.45) ** 2 - ((bv - 0.5) / 0.5) ** 2, 0, 1))
h += (0.40 + 0.35 * bead) * band(0.34, 0.44)
h += 0.42 * band(0.44, 0.56)                                       # flat frieze
# leaf ornament on the frieze (stylised, periodic)
lu = (u * 12) % 1.0
lv = np.clip((v - 0.44) / 0.12, 0, 1)
leaf = np.clip(np.cos((lu - 0.5) * np.pi * 2) * 0.5 + 0.5 - np.abs(lv - 0.5) * 1.6, 0, 1) ** 1.5
vein = np.exp(-((lu - 0.5) / 0.02) ** 2) * (np.abs(lv - 0.5) < 0.38)
h += (0.26 * leaf - 0.08 * vein * leaf) * band(0.44, 0.56)
# gadrooned torus: diagonal ridges, 24 per tile
tv = np.clip((v - 0.56) / 0.28, 0, 1)
gad = 0.5 + 0.5 * np.cos(((u * 24) + tv * 0.8) * 2 * np.pi)
h += (0.45 + 0.35 * np.sin(tv * np.pi) + 0.24 * gad * np.sin(tv * np.pi)) * band(0.56, 0.84)
h += (0.30 + 0.10 * bump(0.84, 0.92)) * band(0.84, 0.92)          # small sight bead
h += 0.18 * band(0.92, 1.01)                                       # sight step (rebate side)
h = ndimage.gaussian_filter(h, (1.0, 0.6), mode=('nearest', 'wrap'))
hp = h * 26.0                                                       # height in pixel units for normals
gy, gx = np.gradient(hp)
gx = (np.roll(hp, -1, 1) - np.roll(hp, 1, 1)) * 0.5                 # wrap along U
n = np.stack([-gx, gy, np.ones_like(hp)], -1)                      # OpenGL: green = up (image row decreasing)
n /= np.linalg.norm(n, axis=-1, keepdims=True)
nrm = (n * 0.5 + 0.5)
# colour: gold leaf, burnished on high points, toned in recesses, bole showing through wear
ao = np.clip(1 - (ndimage.gaussian_filter(h, 6, mode=('nearest', 'wrap')) - h) * 3.0, 0.45, 1)
gold = np.array([0.80, 0.62, 0.30], np.float32)
dark = np.array([0.35, 0.24, 0.10], np.float32)
bole = np.array([0.55, 0.22, 0.12], np.float32)
def periodic(n):
    a = np.abs(np.sin(np.pi * u))
    return n * a + np.roll(n, W // 2, 1) * (1 - a)


noise = periodic(fbm(H, W, 40, 4, 7))
burn = np.clip((h - 0.45) * 2.5, 0, 1)
col = dark + (gold - dark) * (ao[..., None] ** 1.5)
col = col * (0.9 + 0.2 * noise[..., None]) + burn[..., None] * 0.08
L = np.array([-0.25, 0.55, 0.80], np.float32); L /= np.linalg.norm(L)
lit = np.clip(n @ L, 0, 1)
col = col * (0.62 + 0.50 * lit[..., None])
rub = np.clip((burn * 1.4 - 1.0) + (periodic(fbm(H, W, 12, 3, 8)) - 0.5) * 0.9, 0, 1) * 0.30
col = composite(col, bole, rub)
dust = np.clip(1 - ao, 0, 1) * 0.5
col = composite(col, np.array([0.45, 0.40, 0.32], np.float32), dust * 0.4)
save_jpg(np.clip(col, 0, 1), os.path.join(OUT, 'FrameMoulding_Gilt_Color.jpg'), 90)
Image.fromarray((np.clip(nrm, 0, 1) * 255 + 0.5).astype(np.uint8), 'RGB').save(os.path.join(OUT, 'FrameMoulding_Gilt_Normal.png'), optimize=True)
print('frame')
