# Render the four hero product shots used by the Aurel & Finch posters.
import sys, time, numpy as np
from scipy.signal import fftconvolve
from scipy import ndimage
from scenes import *

OUT = sys.argv[1]
SS = int(sys.argv[2]) if len(sys.argv) > 2 else 3
only = sys.argv[3].split(',') if len(sys.argv) > 3 else None


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


def bgzero(u, v):
    return np.zeros((u.size, 3), F)


jobs = {}
env = studio()


def job_solitaire():
    sc, C = solitaire_scene(env, metal='gold18', yaw=-0.42)
    return render(sc, cam_pos=(0.0, 45.06, 94.55), target=(0, 12.5, 0), fov_deg=30, W=1024, H=1536, ss=SS, bg=bgzero, shift=(0.0, 0.22)), 1.0


def job_solitaire_wide():
    sc, C = solitaire_scene(env, metal='whitegold', yaw=0.62)
    return render(sc, cam_pos=(-10.63, 31.21, 60.27), target=(0, 12.5, 0), fov_deg=30, W=2048, H=1024, ss=SS, bg=bgzero, shift=(0.56, -0.04)), 1.0


def job_pearls():
    sc = pearl_scene(env)
    return render(sc, cam_pos=(35.49, 247.2, 201.3), target=(0, 3.6, 0), fov_deg=30, W=1024, H=1536, ss=SS, bg=bgzero, shift=(0.0, 0.31)), 0.0


def job_bands():
    sc = bands_scene(env)
    return render(sc, cam_pos=(0.0, 52.02, 119.14), target=(0, 11, 0), fov_deg=30, W=1024, H=1536, ss=SS, bg=bgzero, shift=(0.0, 0.26)), 0.5


def job_pendant():
    sc = pendant_scene(env, chain_top=90.0, spread=88.0)
    return render(sc, cam_pos=(0.0, 7.18, 102.75), target=(0, 0, 0), fov_deg=36, W=1024, H=1536, ss=SS, bg=bgzero, shift=(0.0, 0.25)), 0.9


for name, fn in [('solitaire', job_solitaire), ('solitaire_wide', job_solitaire_wide), ('pearls', job_pearls),
                 ('bands', job_bands), ('pendant', job_pendant)]:
    if only and name not in only:
        continue
    t0 = time.time()
    (img, al), gl = fn()
    np.save(f'{OUT}/{name}_lin.npy', img.astype(np.float16))
    np.save(f'{OUT}/{name}_alpha.npy', al.astype(np.float16))
    disp, a = finish(img, al, glint=gl)
    from PIL import Image
    Image.fromarray((np.clip(disp, 0, 1) * 255).astype(np.uint8)).save(f'{OUT}/{name}_prev.png')
    print(name, img.shape, round(time.time() - t0, 1), 's', flush=True)
