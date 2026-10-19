"""Build the PBRExterior texture pack (Unity Built-in Standard, metallic workflow).

Usage:  python -I build_pbr_exterior.py <raw_dir> <out_dir>

<raw_dir> holds the untouched CC0 source maps, one folder per source asset, exactly as
downloaded from the GitHub mirrors listed in sources.json (ambientCG / Poly Haven 2K JPG).
<out_dir> receives <Name>_Color.jpg, <Name>_Normal.png (OpenGL +Y), <Name>_MetallicSmoothness.png
(R = metallic, G = B = 0, A = 1 - roughness) and optional <Name>_AO.jpg.

Derived maps (AsphaltWet, PavementSlabWet), the procedural MetalShutter and the PuddleMask are
original project work. Requires numpy and Pillow only. Deterministic (fixed seeds).
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

RAW = Path(sys.argv[1])
OUT = Path(sys.argv[2])
OUT.mkdir(parents=True, exist_ok=True)
LZ = Image.Resampling.LANCZOS


# ----------------------------------------------------------------------------- helpers
def load_rgb(p):
    return np.asarray(Image.open(p).convert("RGB")).astype(np.float32) / 255.0


def load_l(p):
    return np.asarray(Image.open(p).convert("L")).astype(np.float32) / 255.0


def to_lin(c):
    return np.power(np.clip(c, 0, 1), 2.2)


def to_srgb(c):
    return np.power(np.clip(c, 0, 1), 1 / 2.2)


def resize_f(a, size):
    """Resize a float image (H,W) or (H,W,C) to size=(w,h) with Lanczos in 32-bit float."""
    if a.ndim == 2:
        if (a.shape[1], a.shape[0]) == tuple(size):
            return a.astype(np.float32)
        return np.asarray(Image.fromarray(a.astype(np.float32), mode="F").resize(size, LZ))
    return np.stack([resize_f(a[..., i], size) for i in range(a.shape[2])], axis=2)


def u8(a):
    return np.clip(np.round(a * 255), 0, 255).astype(np.uint8)


def save_color(rgb, name, size, q=87):
    Image.fromarray(u8(resize_f(rgb, size))).save(OUT / f"{name}_Color.jpg", quality=q, optimize=True)


def save_normal(n, name, size):
    """n: float (H,W,3) in -1..1, OpenGL convention. Resized, renormalised, saved as RGB PNG."""
    r = resize_f(n * 0.5 + 0.5, size) * 2 - 1
    r /= np.maximum(np.linalg.norm(r, axis=2, keepdims=True), 1e-6)
    Image.fromarray(u8(r * 0.5 + 0.5)).save(OUT / f"{name}_Normal.png", optimize=True)


def save_ms(rough, metal, name, size):
    r = resize_f(np.clip(rough, 0, 1), size)
    m = resize_f(np.clip(metal, 0, 1), size) if np.ndim(metal) else np.full_like(r, float(metal))
    a = np.zeros((*r.shape, 4), np.uint8)
    a[..., 0] = u8(m)
    a[..., 3] = u8(1 - r)
    Image.fromarray(a, "RGBA").save(OUT / f"{name}_MetallicSmoothness.png", optimize=True)


def save_ao(ao, name, size, q=90):
    Image.fromarray(u8(resize_f(ao, size)), "L").save(OUT / f"{name}_AO.jpg", quality=q, optimize=True)


def read_normal(p):
    return load_rgb(p) * 2 - 1


def height_to_normal(h, mm_per_px, height_mm):
    """OpenGL normal from a periodic height field (0..1 scaled to height_mm)."""
    hm = h * height_mm
    dx = (np.roll(hm, -1, 1) - np.roll(hm, 1, 1)) / (2 * mm_per_px)
    dy = (np.roll(hm, -1, 0) - np.roll(hm, 1, 0)) / (2 * mm_per_px)
    n = np.stack([-dx, dy, np.ones_like(hm)], axis=2)   # row index points down, V points up
    return n / np.linalg.norm(n, axis=2, keepdims=True)


def blur_periodic(a, sigma):
    """Gaussian blur with wrap-around (keeps tiling) via FFT."""
    h, w = a.shape
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    g = np.exp(-2 * (np.pi * sigma) ** 2 * (fx ** 2 + fy ** 2))
    return np.real(np.fft.ifft2(np.fft.fft2(a) * g)).astype(np.float32)


def periodic_noise(shape, rng, beta, fmin=0.0, fmax=0.5):
    """Tileable 1/f^beta noise, normalised to 0..1, band-limited to [fmin, fmax] cycles/px."""
    h, w = shape
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    f = np.sqrt(fx ** 2 + fy ** 2)
    amp = np.where((f > fmin) & (f <= fmax), 1.0 / np.maximum(f, 1e-6) ** (beta / 2), 0.0)
    spec = (rng.standard_normal(shape) + 1j * rng.standard_normal(shape)) * amp
    n = np.real(np.fft.ifft2(spec)).astype(np.float32)
    n -= n.min()
    return n / max(n.max(), 1e-6)


def periodic_noise_aniso(shape, rng, beta, sx, sy, fmin=0.0, fmax=0.5):
    """Like periodic_noise but frequencies scaled per axis (sy > 1 stretches features along y)."""
    h, w = shape
    fy = np.fft.fftfreq(h)[:, None] * sy
    fx = np.fft.fftfreq(w)[None, :] * sx
    f = np.sqrt(fx ** 2 + fy ** 2)
    amp = np.where((f > fmin) & (f <= fmax), 1.0 / np.maximum(f, 1e-6) ** (beta / 2), 0.0)
    spec = (rng.standard_normal(shape) + 1j * rng.standard_normal(shape)) * amp
    n = np.real(np.fft.ifft2(spec)).astype(np.float32)
    n -= n.min()
    return n / max(n.max(), 1e-6)


def warp_periodic(a, du, dv):
    """Bilinear sample of a periodic image at (x+du, y+dv) with wrap."""
    h, w = a.shape
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    x = (xx + du) % w
    y = (yy + dv) % h
    x0 = np.floor(x).astype(int); y0 = np.floor(y).astype(int)
    fx = x - x0; fy = y - y0
    x1 = (x0 + 1) % w; y1 = (y0 + 1) % h
    return (a[y0, x0] * (1 - fx) * (1 - fy) + a[y0, x1] * fx * (1 - fy)
            + a[y1, x0] * (1 - fx) * fy + a[y1, x1] * fx * fy)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def saturate_rgb(c, s):
    lum = (c * np.array([0.2126, 0.7152, 0.0722], np.float32)).sum(axis=2, keepdims=True)
    return np.clip(lum + (c - lum) * s, 0, 1)


C2K, C1K, C512 = (2048, 2048), (1024, 1024), (512, 512)

# ----------------------------------------------------------------------------- Asphalt (Poly Haven asphalt_02, 3 x 3 m)
d = RAW / "asphalt_02"
col = load_rgb(d / "asphalt_02_diffuse_2k.jpg")
arm = load_rgb(d / "asphalt_02_arm_2k.jpg")
ao, rough0, metal0 = arm[..., 0], arm[..., 1], arm[..., 2]
nrm = read_normal(d / "asphalt_02_nor_gl_2k.jpg")
disp = load_l(d / "asphalt_02_displacement_2k.jpg")

save_color(col, "AsphaltDry", C2K)
save_normal(nrm, "AsphaltDry", C1K)
save_ms(rough0, 0.0, "AsphaltDry", C1K)
save_ao(ao, "AsphaltDry", C512)

# Wet: water settles in cracks and aggregate pits (high-pass of the scanned height; ~6 cm scale
# so the 3 m tile does not repeat a big puddle - large puddles come from PuddleMask.png).
hp = disp - blur_periodic(disp, 40)
lo = np.percentile(hp, 3.5)
hi = np.percentile(hp, 11.0)
water = smoothstep(hi, lo, hp)                       # 1 = water-filled cavity
water = np.clip(blur_periodic(water, 1.2), 0, 1)
lin = to_lin(col)
lin = lin * (0.60 - 0.18 * water[..., None])         # porous darkening, deeper where filled
wet_col = saturate_rgb(to_srgb(lin), 1.12)
r_film = np.clip(0.30 + (rough0 - rough0.mean()) * 0.55, 0.14, 0.55)   # thin water film everywhere
wet_rough = r_film * (1 - water) + 0.04 * water
flat = np.zeros_like(nrm); flat[..., 2] = 1
k = (0.18 + 0.77 * water)[..., None]                 # film smooths micro relief, pools are flat
wet_n = nrm * (1 - k) + flat * k
save_color(wet_col, "AsphaltWet", C2K)
save_normal(wet_n, "AsphaltWet", C1K)
save_ms(wet_rough, 0.0, "AsphaltWet", C1K)
save_ao(ao * (1 - water) + np.maximum(ao, 0.85) * water, "AsphaltWet", C512)

# ----------------------------------------------------------------------------- Pavement (ambientCG PavingStones136, 2 x 2 m)
d = RAW / "PavingStones136"
col = load_rgb(d / "PavingStones136_2K-JPG_Color.jpg")
col = saturate_rgb(col, 0.5)                          # olive-yellow slabs -> neutral grey concrete
col = to_srgb(to_lin(col) * np.array([0.97, 0.99, 1.03], np.float32))
rough = load_l(d / "PavingStones136_2K-JPG_Roughness.jpg")
ao = load_l(d / "PavingStones136_2K-JPG_AmbientOcclusion.jpg")
nrm = read_normal(d / "PavingStones136_2K-JPG_NormalGL.jpg")
save_color(col, "PavementSlab", C2K)
save_normal(nrm, "PavementSlab", C1K)
save_ms(rough, 0.0, "PavementSlab", C1K)
save_ao(ao, "PavementSlab", C1K)
# Damp variant (rain-soaked slabs, joints hold water). Shares PavementSlab_Normal.png / _AO.jpg.
joint = smoothstep(0.80, 0.45, ao)                    # joints are the occluded grooves
lin = to_lin(col) * (0.70 - 0.15 * joint[..., None])
save_color(saturate_rgb(to_srgb(lin), 1.08), "PavementSlabWet", C2K)
save_ms(np.clip(0.36 + (rough - rough.mean()) * 0.5, 0.15, 0.6) * (1 - joint) + 0.08 * joint,
        0.0, "PavementSlabWet", C1K)

# ----------------------------------------------------------------------------- Concrete (ambientCG Concrete034, 2 x 1 m)
d = RAW / "Concrete034"
col = load_rgb(d / "Concrete034_2K-JPG_Color.jpg")
col = to_srgb(to_lin(col) * 0.80)                     # very pale source; bring kerb concrete to ~0.45 albedo
save_color(col, "Concrete", (2048, 1024))
save_normal(read_normal(d / "Concrete034_2K-JPG_NormalGL.jpg"), "Concrete", (1024, 512))
save_ms(load_l(d / "Concrete034_2K-JPG_Roughness.jpg"), 0.0, "Concrete", (1024, 512))

# ----------------------------------------------------------------------------- FacadeStone (ambientCG Travertine001, 1.2 x 1.2 m)
d = RAW / "Travertine001"
col = load_rgb(d / "Travertine001_2K-JPG_Color.jpg")
save_color(col, "FacadeStone", C2K)
# Source is a mirror-polished procedural stone (roughness ~0.02, flat normal). Shop-front cladding
# reads more real as polished-but-worn: roughness ~0.12 and a faint vein/pore relief from the albedo.
lum = (col * np.array([0.2126, 0.7152, 0.0722], np.float32)).sum(axis=2)
rel = lum - blur_periodic(lum, 3.0)
rel = (rel - rel.min()) / (rel.max() - rel.min())
nrm = read_normal(d / "Travertine001_2K-JPG_NormalGL.jpg") + height_to_normal(rel, 1200 / 2048, 0.12) - [0, 0, 1]
save_normal(nrm, "FacadeStone", C1K)
r = load_l(d / "Travertine001_2K-JPG_Roughness.jpg")
save_ms(np.clip(0.12 + (r - r.mean()) * 1.4 + (0.5 - rel) * 0.04, 0.07, 0.25), 0.0, "FacadeStone", C1K)

# ----------------------------------------------------------------------------- BrickFacade (Poly Haven brick_wall_006, 3 x 3 m)
d = RAW / "brick_wall_006"
arm = load_rgb(d / "brick_wall_006_arm_2k.jpg")
save_color(load_rgb(d / "brick_wall_006_diffuse_2k.jpg"), "BrickFacade", C2K, q=85)
save_normal(read_normal(d / "brick_wall_006_nor_gl_2k.jpg"), "BrickFacade", C1K)
save_ms(arm[..., 1], 0.0, "BrickFacade", C512)
save_ao(arm[..., 0], "BrickFacade", C1K)

# ----------------------------------------------------------------------------- Render (Poly Haven plastered_wall, 2 x 2 m)
d = RAW / "plastered_wall"
arm = load_rgb(d / "plastered_wall_arm_2k.jpg")
save_color(load_rgb(d / "plastered_wall_diffuse_2k.jpg"), "Render", C1K)
save_normal(read_normal(d / "plastered_wall_nor_gl_2k.jpg"), "Render", C512)
save_ms(arm[..., 1], 0.0, "Render", C512)
save_ao(arm[..., 0], "Render", C512)

# ----------------------------------------------------------------------------- Granite (ambientCG Granite002A, ~0.6 m)
d = RAW / "Granite002A"
col = load_rgb(d / "Granite002A_2K-JPG_Color.jpg")
save_color(col, "Granite", C1K)
# Source is polished (roughness ~0.02, flat normal). Kerbs and steps are flamed/bush-hammered:
# crystal-scale relief (light feldspar/quartz proud, dark minerals pitted), roughness ~0.6 and
# sparse mica/quartz facets that glint under a torch.
rng = np.random.default_rng(6020)
lum = (col * np.array([0.2126, 0.7152, 0.0722], np.float32)).sum(axis=2)
g = lum - blur_periodic(lum, 6.0); g /= g.std()
pit = periodic_noise((2048, 2048), rng, 1.0, 1 / 16, 1 / 2) - 0.5; pit /= pit.std()
h = 0.6 * g + 0.4 * pit
h = (h - h.min()) / (h.max() - h.min())
nrm = read_normal(d / "Granite002A_2K-JPG_NormalGL.jpg") + height_to_normal(h, 600 / 2048, 0.9) - [0, 0, 1]
save_normal(nrm, "Granite", C1K)
rough = np.clip(0.62 + 0.10 * (0.5 - h) * 2, 0.45, 0.8)
rough = resize_f(rough, C512)
glint = periodic_noise((512, 512), rng, 0.0, 0.25, 0.5)
rough = np.where(glint > np.percentile(glint, 98.5), 0.22, rough)
save_ms(rough, 0.0, "Granite", C512)

# ----------------------------------------------------------------------------- RustyMetal (ambientCG Metal053C, ~1 m)
d = RAW / "Metal053C"
save_color(load_rgb(d / "Metal053C_2K-JPG_Color.jpg"), "RustyMetal", C1K)
save_normal(read_normal(d / "Metal053C_2K-JPG_NormalGL.jpg"), "RustyMetal", C1K)
save_ms(load_l(d / "Metal053C_2K-JPG_Roughness.jpg"), load_l(d / "Metal053C_2K-JPG_Metalness.jpg"),
        "RustyMetal", C512)

# ----------------------------------------------------------------------------- MetalShutter (procedural, 1 x 1 m, 13 slats of 77 mm)
rng = np.random.default_rng(20261014)
N = 2048
mm = 1000.0 / N
yy = np.arange(N, dtype=np.float32)[:, None] * np.ones((1, N), np.float32)
t = (yy / (N / 13.0)) % 1.0                           # 0 at top of each slat, 1 at bottom
face = np.sin(np.clip((t - 0.10) / 0.80, 0, 1) * np.pi) * 2.6          # 2.6 mm convex face
flute = -0.9 * np.exp(-((t - 0.5) / 0.035) ** 2)                       # central stiffening flute
hook = 3.4 * np.exp(-((t - 0.955) / 0.03) ** 2) + 2.2 * np.exp(-((t - 0.04) / 0.025) ** 2)
gap = -5.0 * np.exp(-((np.minimum(t, 1 - t)) / 0.008) ** 2)            # interlock seam
height_mm = face + flute + hook + gap
# slight dents / waviness, periodic
dent = (periodic_noise((N, N), rng, 3.2, 1 / 512, 1 / 16) - 0.5) * 0.9
height_mm = height_mm + dent
hn = (height_mm - height_mm.min()) / (height_mm.max() - height_mm.min())
sh_n = height_to_normal(hn, mm, float(height_mm.max() - height_mm.min()))
# zinc spangle (periodic voronoi-ish cells) + streaky grime from rain runs
cells = periodic_noise((N, N), rng, 0.0, 1 / 40, 1 / 20)
spangle = (np.floor(cells * 9) / 9 - 0.5) * 0.06
streak = periodic_noise_aniso((N, N), rng, 1.4, 1.0, 40.0, 1 / 300, 1 / 2)   # thin vertical rain runs
streak = smoothstep(0.58, 0.85, streak * 0.85 + periodic_noise((N, N), rng, 2.0, 1 / 1024, 1 / 64) * 0.3)
groove = smoothstep(0.35, 0.05, hn)                                     # grime collects in seams
grime = np.clip(groove * 0.75 + streak * 0.2 + periodic_noise((N, N), rng, 1.8, 1 / 1024, 1 / 8) * 0.18 - 0.06, 0, 1)
base = np.array([0.62, 0.635, 0.65], np.float32)                        # weathered galvanised zinc (sRGB)
dirt = np.array([0.20, 0.19, 0.17], np.float32)
sh_col = (base[None, None, :] * (1 + spangle[..., None])) * (1 - grime[..., None]) + dirt * grime[..., None]
sh_col *= (0.92 + 0.08 * periodic_noise((N, N), rng, 2.2, 1 / 2048, 1 / 4))[..., None]
sh_metal = 0.9 * (1 - grime) + 0.15 * grime
sh_rough = np.clip(0.46 + spangle * 1.5 + grime * 0.38 + (periodic_noise((N, N), rng, 1.0, 1 / 256, 1 / 8) - 0.5) * 0.08, 0.2, 0.95)
sh_ao = np.clip(1 - groove * 0.55, 0, 1)
save_color(np.clip(sh_col, 0, 1), "MetalShutter", C1K)
save_normal(sh_n, "MetalShutter", C1K)
save_ms(sh_rough, sh_metal, "MetalShutter", C512)
save_ao(sh_ao, "MetalShutter", C512)

# ----------------------------------------------------------------------------- PuddleMask (procedural, 8 x 8 m, tileable)
rng = np.random.default_rng(1410)
M = 2048
big = periodic_noise((M, M), rng, 3.0, 1 / 1024, 1 / 64)              # puddle layout (0.25-8 m)
wu = (periodic_noise((M, M), rng, 2.5, 1 / 512, 1 / 32) - 0.5) * 40
wv = (periodic_noise((M, M), rng, 2.5, 1 / 512, 1 / 32) - 0.5) * 40
field = warp_periodic(big, wu, wv)
field += (periodic_noise((M, M), rng, 1.6, 1 / 128, 1 / 6) - 0.5) * 0.025  # slightly ragged waterline
thr = np.percentile(field, 85)                                         # ~15 % standing water
pond = smoothstep(thr - 0.003, thr + 0.008, field)                     # waterline ~2-4 cm wide
halo = smoothstep(thr - 0.10, thr - 0.003, field) ** 1.5 * 0.45        # wet rim fading over ~0.2-0.6 m
mask = np.maximum(pond, halo)
Image.fromarray(u8(resize_f(mask, (1024, 1024))), "L").save(OUT / "PuddleMask.png", optimize=True)

# ----------------------------------------------------------------------------- LeakStain decal (ambientCG Leaking006, 0.5 x 2 m)
d = RAW / "Leaking006"
lc = load_rgb(d / "Leaking006_2K-JPG_Color.jpg")
lo_ = load_l(d / "Leaking006_2K-JPG_Opacity.jpg")
rgba = np.concatenate([resize_f(lc, (256, 1024)), resize_f(lo_, (256, 1024))[..., None]], axis=2)
Image.fromarray(u8(rgba), "RGBA").save(OUT / "LeakStain_ColorAlpha.png", optimize=True)

print("built", sorted(p.name for p in OUT.iterdir() if p.is_file()))
