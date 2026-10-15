"""Remove the woven "YOKI'S" tag from the garden_gloves_01 roughness/metal (ARM) and normal maps.
delabel.py already covered the tag in the colour map; this does the same for the other two maps,
then exports them exactly as build_ph.py + assemble.py do:
  WorkGloves_MetallicSmoothness.png  512 px RGBA, R = metallic, G = B = 0, A = 1 - roughness
  WorkGloves_Normal.jpg              1024 px, LANCZOS, JPG q92 (build_ph) then re-encoded q90 (assemble)
Method: one tag mask on the 2K maps (polygon of the tag and its raised right edge, +10 px, feathered).
Low frequencies inside it (tag outline, lettering) are diffused in from the mask boundary (same
inpaint as delabel.py, then harmonic smoothing); high frequencies (the knit) are cloned from the same
glove back 140 px lower and 100 px left, which stays above the ribbed cuff. The normal map is not
renormalised, because the source map is not unit length on the knit.
usage: python3 -I delabel_glove_maps.py <dir with garden_gloves_01_arm_2k.jpg and _nor_gl_2k.jpg> <out dir>"""
import sys, os, io
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

POLY = [(358, 1393), (532, 1438), (562, 1446), (596, 1484), (591, 1506), (538, 1530), (352, 1478)]
DONOR = (140, -100)   # (rows, columns) on the 2K maps


def box(x, r):
    k = 2 * r + 1
    p = np.pad(x, r, mode='edge')
    c = np.cumsum(p, 0); c = np.vstack([np.zeros((1, c.shape[1])), c]); y = (c[k:] - c[:-k]) / k
    c = np.cumsum(y, 1); c = np.hstack([np.zeros((c.shape[0], 1)), c]); return (c[:, k:] - c[:, :-k]) / k


def gauss(x, r):
    for _ in range(3):
        x = box(x, r)
    return x


def inpaint(a, mask, iters=600):
    a = a.astype(np.float32).copy(); m = mask.copy(); known = ~m
    for _ in range(iters):
        if not m.any():
            break
        s = np.zeros_like(a); c = np.zeros(a.shape[:2], np.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (-1, -1), (1, 1), (1, -1), (-1, 1), (0, -1)):
            sh = np.roll(np.roll(a, dy, 0), dx, 1); kn = np.roll(np.roll(known, dy, 0), dx, 1)
            s += sh * kn[..., None]; c += kn
        fill = m & (c > 0)
        a[fill] = s[fill] / c[fill][:, None]
        known = known | fill; m = m & ~fill
    return a


def smooth_fill(lp, mask, passes=200):
    a = lp.copy()
    for _ in range(passes):
        avg = (np.roll(a, 1, 0) + np.roll(a, -1, 0) + np.roll(a, 1, 1) + np.roll(a, -1, 1)) / 4
        a[mask] = avg[mask]
    return a


def make_mask():
    m = Image.new('L', (2048, 2048), 0); ImageDraw.Draw(m).polygon(POLY, fill=255)
    hard = np.asarray(m.filter(ImageFilter.MaxFilter(21))) > 0
    soft = np.asarray(Image.fromarray(hard.astype(np.uint8) * 255).filter(ImageFilter.GaussianBlur(4)), np.float32) / 255
    alpha = np.maximum(soft, (np.asarray(m.filter(ImageFilter.MaxFilter(13))) > 0).astype(np.float32))
    return alpha > 0.01, alpha


def fix(img, mask, alpha, dy, dx, r_lp=6):
    x0, y0, x1, y1 = 250, 1300, 700, 1800
    full = img.astype(np.float32)
    w = full[y0:y1, x0:x1]; m = mask[y0:y1, x0:x1]; al = alpha[y0:y1, x0:x1, None]
    lp = np.dstack([gauss(w[..., i], r_lp) for i in range(w.shape[2])])
    big = np.asarray(Image.fromarray(m.astype(np.uint8) * 255).filter(ImageFilter.MaxFilter(2 * (3 * r_lp) + 1))) > 0
    lpf = smooth_fill(inpaint(lp, big), big)
    donor = full[y0 + dy:y1 + dy, x0 + dx:x1 + dx]
    dlp = np.dstack([gauss(donor[..., i], r_lp) for i in range(donor.shape[2])])
    res = full.copy(); res[y0:y1, x0:x1] = w * (1 - al) + (lpf + donor - dlp) * al
    return res.clip(0, 255).round().astype(np.uint8)


def main():
    src, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    region, alpha = make_mask()
    arm = np.asarray(Image.open(os.path.join(src, 'garden_gloves_01_arm_2k.jpg')).convert('RGB'))
    nor = np.asarray(Image.open(os.path.join(src, 'garden_gloves_01_nor_gl_2k.jpg')).convert('RGB'))
    arm2 = fix(arm, region, alpha, *DONOR)
    nor2 = fix(nor, region, alpha, *DONOR)
    a = np.asarray(Image.fromarray(arm2).resize((512, 512), Image.LANCZOS)).astype(np.float32)
    ms = np.zeros((512, 512, 4), np.uint8)
    ms[..., 0] = a[..., 2].clip(0, 255).astype(np.uint8); ms[..., 3] = (255 - a[..., 1]).clip(0, 255).astype(np.uint8)
    Image.fromarray(ms, 'RGBA').save(os.path.join(out, 'WorkGloves_MetallicSmoothness.png'), optimize=True)
    b = io.BytesIO(); Image.fromarray(nor2).resize((1024, 1024), Image.LANCZOS).save(b, 'JPEG', quality=92, optimize=True)
    Image.open(io.BytesIO(b.getvalue())).convert('RGB').save(os.path.join(out, 'WorkGloves_Normal.jpg'), quality=90, optimize=True)
    print('written to', out)


if __name__ == '__main__':
    main()
