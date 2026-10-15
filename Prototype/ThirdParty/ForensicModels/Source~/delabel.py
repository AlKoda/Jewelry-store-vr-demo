"""Remove product-style wordmarks from Poly Haven colour maps (in place on staged copies)."""
import sys, os
import numpy as np
from PIL import Image, ImageFilter
R = sys.argv[1]

def inpaint(a, mask, iters=400):
    a = a.astype(np.float32).copy(); m = mask.copy()
    known = ~m
    for _ in range(iters):
        if not m.any(): break
        s = np.zeros_like(a); c = np.zeros(a.shape[:2], np.float32)
        for dy, dx in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(-1,-1),(1,-1),(-1,1)):
            sh = np.roll(np.roll(a, dy, 0), dx, 1); kn = np.roll(np.roll(known, dy, 0), dx, 1)
            s += sh * kn[..., None]; c += kn
        fill = m & (c > 0)
        a[fill] = s[fill] / c[fill][:, None]
        known = known | fill; m = m & ~fill
    return a

def grain(shape, amp, seed):
    rng = np.random.default_rng(seed)
    n = rng.normal(0, 1, shape[:2]).astype(np.float32)
    n = np.asarray(Image.fromarray(((n * 40) + 128).clip(0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8)), np.float32) - 128
    return n[..., None] / 40 * amp

def edit(path, fn):
    im = np.asarray(Image.open(path).convert('RGB')).astype(np.float32)
    out = fn(im)
    Image.fromarray(out.clip(0, 255).astype(np.uint8)).save(path, quality=95)
    print('edited', path)

# 1. measuring tape: "Q-TECH" wordmark in the yellow label box -> plain yellow (keeps "3m" length and "QC-PASSED")
def tape(im):
    x0, y0, x1, y1 = 574, 354, 713, 399
    sub = im[y0:y1, x0:x1]
    lum = sub.mean(2); yellow = np.median(sub[lum > np.percentile(lum, 60)], axis=0)
    mask = np.zeros(im.shape[:2], bool)
    dark = (lum < 0.62 * yellow.mean())
    dark = np.asarray(Image.fromarray(dark.astype(np.uint8) * 255).filter(ImageFilter.MaxFilter(5))) > 0
    mask[y0:y1, x0:x1] = dark
    out = inpaint(im, mask)
    out[mask] += grain(out.shape, 6, 1)[mask]
    return out

# 2. security camera: "FocusSpy" badge -> housing paint
def camera(im):
    mask = np.zeros(im.shape[:2], bool); mask[983:1012, 15:113] = True
    out = inpaint(im, mask, 800)
    out[mask] += grain(out.shape, 7, 2)[mask]
    return out

# 3. work gloves: "YOKI'S" woven tag -> knit cloned from the same glove back
def glove(im):
    x0, y0, x1, y1 = 345, 1390, 552, 1528
    sub = im[y0:y1, x0:x1]
    tag = (sub.min(2) > 120) & (sub[..., 0] > 140)
    tag = np.asarray(Image.fromarray(tag.astype(np.uint8) * 255).filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.MinFilter(3))) > 0
    # fill holes of the tag (letters are green) using a convex fill per row
    full = np.zeros_like(tag)
    for r in range(tag.shape[0]):
        xs = np.where(tag[r])[0]
        if len(xs) > 2: full[r, xs.min():xs.max() + 1] = True
    alpha = np.asarray(Image.fromarray(full.astype(np.uint8) * 255).filter(ImageFilter.GaussianBlur(3)), np.float32)[..., None] / 255
    alpha = np.maximum(alpha, full[..., None].astype(np.float32))
    donor = im[y0 + 190:y1 + 190, x0 - 150:x1 - 150]   # knit lower on the same glove back
    out = im.copy(); out[y0:y1, x0:x1] = sub * (1 - alpha) + donor * alpha
    return out

# 4. searchlight: "SENTINEL IQ-4" plate text -> plain plate
def searchlight(im):
    mask = np.zeros(im.shape[:2], bool); mask[131:143, 519:567] = True
    out = inpaint(im, mask)
    out[mask] += grain(out.shape, 5, 3)[mask]
    return out

edit(os.path.join(R, 'MeasuringTape/textures/measuring_tape_01_diff_1k.jpg'), tape)
edit(os.path.join(R, 'SecurityCamera/textures/security_camera_01_diff_1k.jpg'), camera)
edit(os.path.join(R, 'WorkGloves/textures/garden_gloves_01_diff_2k.jpg'), glove)
edit(os.path.join(R, 'PortableSearchlight/textures/portable_searchlight_diff_1k.jpg'), searchlight)
