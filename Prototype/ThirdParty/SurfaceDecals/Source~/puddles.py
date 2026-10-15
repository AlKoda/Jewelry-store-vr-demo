"""Rain puddles for the wet street and pavement: shape from a low-frequency height field
(water fills the dips up to a level, so edges follow contour lines), damp rim, sediment line."""
import sys, os
import numpy as np
from scipy import ndimage as ndi
from dutil import *


def puddle(name, out, seed, H, W, metres, level, mode='round'):
    r = rng(seed)
    ppm = W / (metres[0] * 1000.0)
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    u = (xx + 0.5) / W * 2 - 1; v = (yy + 0.5) / H * 2 - 1
    feat = (220 if mode != 'cluster' else 90) * ppm
    g = fbm(H, W, feat, 4, 0.42, seed)
    wdx = fbm(H, W, feat * 2, 2, 0.5, seed + 1) * feat * 0.18
    wdy = fbm(H, W, feat * 2, 2, 0.5, seed + 2) * feat * 0.18
    g = warp(g, wdx, wdy)
    rough = fbm(H, W, 4 * ppm, 3, 0.5, seed + 9)  # aggregate-scale relief: jaggy water line
    edge_fade = smoothstep(0.8, 1.0, np.maximum(np.abs(u), np.abs(v)))
    if mode == 'kerb':
        # gutter along the kerb (top edge of the image, v = -1): water lies against a straight line
        height = 0.45 * g + 1.1 * u ** 2 + 0.9 * (v + 1) + 3.0 * smoothstep(0.85, 1.0, np.abs(u))
        height += 3.0 * smoothstep(0.8, 1.0, v)
        kerb = smoothstep(-0.935, -0.945, v)  # 1 beyond the kerb face
    elif mode == 'cluster':
        height = 0.7 * g + 0.35 * (u ** 2 + v ** 2) + 3.0 * edge_fade
        kerb = 0
    else:
        height = 0.32 * g + 1.3 * (u ** 2 * 1.3 + v ** 2) + 3.0 * edge_fade
        kerb = 0
    height = height + 0.012 * rough
    depth = level - height  # >0 water
    water = smoothstep(0.0, 0.012, depth) * (1 - kerb)
    deep = smoothstep(0.0, 0.4, depth)
    damp = smoothstep(-0.12, 0.0, depth) * (0.75 + 0.25 * smoothstep(-1.0, 1.0, fbm(H, W, 15 * ppm, 3, 0.5, seed + 3)))
    damp = np.maximum(damp, water) * (1 - kerb)
    sed = smoothstep(0.018, 0.0, np.abs(depth + 0.025)) * 0.45 * smoothstep(-0.3, 1.0, fbm(H, W, 8 * ppm, 3, 0.5, seed + 4)) * (1 - kerb)
    a = np.clip(np.maximum(damp * 0.36, water * (0.66 + 0.28 * deep)), 0, 1)
    a = np.maximum(a, sed * 0.55)
    col_water = np.array([0.028, 0.031, 0.035]); col_damp = np.array([0.045, 0.045, 0.047]); col_sed = np.array([0.36, 0.33, 0.29])
    wsum = water + (damp - water) + sed + 1e-6
    rgb = (col_water * water[..., None] + col_damp * (damp - water)[..., None] + col_sed * sed[..., None]) / wsum[..., None]
    grit = (r.random((H, W)) > 0.9995) & (water > 0.5)
    gf = np.clip(ndi.gaussian_filter(grit.astype(float), 0.8) * 3, 0, 1)
    rgb = rgb * (1 - gf[..., None]) + np.array([0.22, 0.18, 0.12]) * gf[..., None]
    a = np.maximum(a, gf)
    ripple = fbm(H, W, 18 * ppm, 2, 0.5, seed + 5) * 0.04 * water
    men = smoothstep(0.02, 0.0, depth) * water * 0.12
    hgt = ripple + men + sed * 0.08 + gf * 0.3
    nrm = normal_from_height(ndi.gaussian_filter(hgt, 0.8), strength=8.0)
    smooth = np.clip(0.6 * damp + 0.37 * water - 0.3 * sed - 0.4 * gf, 0, 0.97)
    save_rgba(os.path.join(out, name + '.png'), rgb, a)
    save_normal(os.path.join(out, name + '_Normal.png'), nrm)
    save_ms(os.path.join(out, name + '_MetallicSmoothness.png'), smooth)
    print(name, 'water %.2f damp %.2f' % (water.mean(), damp.mean()))


if __name__ == '__main__':
    out = sys.argv[1]
    which = sys.argv[2:] or ['l', 'm', 'c']
    if 'l' in which: puddle('Puddle_Kerbside', out, 61, 512, 1024, (2.0, 1.0), 0.75, 'kerb')
    if 'm' in which: puddle('Puddle_Round', out, 66, 1024, 1024, (1.2, 1.2), 0.5)
    if 'c' in which: puddle('Puddle_Cluster', out, 63, 1024, 1024, (1.5, 1.5), -0.25, 'cluster')
