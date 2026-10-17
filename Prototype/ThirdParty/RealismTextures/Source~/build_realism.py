#!/usr/bin/env python3
"""Build Prototype/ThirdParty/RealismTextures.

usage: python3 -I build_realism.py <download_root> <ambientprops_dir> <out_dir>

<download_root> holds sparse, no-checkout clones (see sources.json) of
  VoxogoBrusch/the-levels, TheMarco/liminal, Dadaskis/TextureArchive,
  EliteGamer007/6dof-drone-sim, Geadas11/Claude_steam_game.
Everything procedural is seeded and deterministic.
"""
import hashlib, json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage as ndi

DL, PROPS, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
REC = []  # file records for sources.json


# ------------------------------------------------------------------ helpers
def load(p, mode="RGB"):
    return np.asarray(Image.open(p).convert(mode), dtype=np.float32) / 255.0


def to_img(a):
    a = np.clip(a * 255.0 + 0.5, 0, 255).astype(np.uint8)
    return Image.fromarray(a)


def save(rel, arr_or_img, kind, src, licence="CC0-1.0", note="", quality=92):
    path = os.path.join(OUT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im = arr_or_img if isinstance(arr_or_img, Image.Image) else to_img(arr_or_img)
    if rel.endswith(".jpg"):
        im.convert("RGB").save(path, quality=quality, optimize=True)
    else:
        im.save(path, optimize=True)
    REC.append({"file": rel, "kind": kind, "size": list(im.size), "source": src,
                "licence": licence, "note": note,
                "sha256": hashlib.sha256(open(path, "rb").read()).hexdigest()})


def bleed(rgba, thr=0.9):
    """Straight alpha; RGB under alpha<thr replaced by nearest opaque colour."""
    a = rgba[..., 3]
    solid = a >= thr
    if not solid.any():
        solid = a >= a.max() * 0.5
    _, (iy, ix) = ndi.distance_transform_edt(~solid, return_indices=True)
    out = rgba.copy()
    out[..., :3] = rgba[iy, ix, :3]
    out[..., :3] = np.where(solid[..., None], rgba[..., :3], out[..., :3])
    return out


def normal_from_height(h, strength, wrap=False):
    mode = "wrap" if wrap else "nearest"
    dx = ndi.sobel(h, axis=1, mode=mode) / 8.0
    dy = ndi.sobel(h, axis=0, mode=mode) / 8.0
    nx, ny, nz = -dx * strength, dy * strength, np.ones_like(h)  # OpenGL (+Y up)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.dstack([nx / l, ny / l, nz / l]) * 0.5 + 0.5


def fbm(shape, rng, octaves=5, base=4, wrap=True, persistence=0.55):
    h, w = shape
    acc = np.zeros(shape, np.float32)
    amp, tot = 1.0, 0.0
    for o in range(octaves):
        n = base * 2 ** o
        g = rng.random((n, n)).astype(np.float32)
        if wrap:
            g = np.pad(g, 2, mode="wrap")
            z = ndi.zoom(g, ((h + 4 * h / n) / (n + 4), (w + 4 * w / n) / (n + 4)), order=3, mode="grid-wrap")
            oy, ox = int(round(2 * h / n)), int(round(2 * w / n))
            z = z[oy:oy + h, ox:ox + w]
            if z.shape != shape:
                z = np.asarray(Image.fromarray(z).resize((w, h), Image.BICUBIC))
        else:
            z = np.asarray(Image.fromarray(g).resize((w, h), Image.BICUBIC))
        acc += amp * z
        tot += amp
        amp *= persistence
    acc /= tot
    return (acc - acc.min()) / (acc.max() - acc.min() + 1e-6)


def ms_map(metal, smooth):
    """Unity Standard _MetallicGlossMap: R = metallic, A = smoothness."""
    m = np.clip(metal, 0, 1)
    return np.dstack([m, m, m, np.clip(smooth, 0, 1)])


def resize(a, size):
    if a.ndim == 2:
        return np.asarray(Image.fromarray(a.astype(np.float32)).resize(size, Image.LANCZOS))
    return np.dstack([resize(a[..., c], size) for c in range(a.shape[2])])


def seamless(a, blend=0.25):
    """Make a texture tileable by cross-fading with its half-offset copy."""
    h, w = a.shape[:2]
    sh = np.roll(np.roll(a, h // 2, 0), w // 2, 1)
    y = np.minimum(np.arange(h), h - 1 - np.arange(h)) / (h * blend)
    x = np.minimum(np.arange(w), w - 1 - np.arange(w)) / (w * blend)
    m = np.clip(np.minimum.outer(y, x), 0, 1)
    m = m * m * (3 - 2 * m)
    if a.ndim == 3:
        m = m[..., None]
    return a * m + sh * (1 - m)


def renorm(n):
    v = n * 2 - 1
    v /= np.linalg.norm(v, axis=2, keepdims=True) + 1e-6
    return v * 0.5 + 0.5


# ------------------------------------------------- 1. model-matched prop maps
LEV = os.path.join(DL, "the-levels/public/assets/models")
LIM = os.path.join(DL, "liminal/models/cc0")
PH_PROPS = [
    # (AmbientProps name, colour png in AmbientProps, mirror dir, texture stem, PH id, authors)
    ("PlasticCrate", "PlasticCrate_plastic_crate_01.png", LEV, "plastic_crate_01", "plastic_crate_01", "PierreB3D"),
    ("PlasticThermos", "PlasticThermos_plastic_thermos.png", LEV, "plastic_thermos", "plastic_thermos", "PierreB3D"),
    ("UtilityBoxSmall", "UtilityBoxSmall_utility_box_02.png", LEV, "utility_box_02", "utility_box_02", "James Ray Cock"),
    ("SprayPaintBottles", "SprayPaintBottles_spray_paint_bottles.png", LEV + "/spray_paint_bottles", "spray_paint_bottles", "spray_paint_bottles", "James Ray Cock"),
    ("SprayPaintBottles_Dirty", "SprayPaintBottles_spray_paint_bottles_dirty.png", LEV + "/spray_paint_bottles", "spray_paint_bottles_dirty", "spray_paint_bottles", "James Ray Cock"),
    ("ElectricCables", "ElectricCables_modular_electric_cables.png", LEV + "/modular_electric_cables", "modular_electric_cables", "modular_electric_cables", "Kuutti Siitonen"),
    ("ElectricCables_Boxes", "ElectricCables_modular_electric_cables_boxes.png", LEV + "/modular_electric_cables", "modular_electric_cables_boxes", "modular_electric_cables", "Kuutti Siitonen"),
    ("IndustrialPipes_Group01", "IndustrialPipes_modular_industrial_pipes_01_group01.png", LEV + "/modular_industrial_pipes_01", "modular_industrial_pipes_01_group01", "modular_industrial_pipes_01", "Jorge Camacho"),
    ("IndustrialPipes_Group02", "IndustrialPipes_modular_industrial_pipes_01_group02.png", LEV + "/modular_industrial_pipes_01", "modular_industrial_pipes_01_group02", "modular_industrial_pipes_01", "Jorge Camacho"),
    ("RustedCan", "RustedCan_can_rusted.png", LIM, "can_rusted", "can_rusted", "Rahul Chaudhary"),
    ("PowerBox", "PowerBox_power_box_01.png", LIM, "power_box_01", "power_box_01", "Rico Cilliers, Yann Kervran"),
    ("WetFloorSign", "WetFloorSign_WetFloorSign_01.png", LIM, "WetFloorSign_01", "WetFloorSign_01", "Fran Calvente"),
]


def texdir(base, stem, phid):
    for d in (os.path.join(base, phid, "textures"), os.path.join(base, "textures")):
        if os.path.exists(os.path.join(d, stem + "_nor_gl_1k.jpg")):
            return d
    raise FileNotFoundError(stem)


for name, colour, base, stem, phid, authors in PH_PROPS:
    d = texdir(base, stem, phid)
    nor = load(os.path.join(d, stem + "_nor_gl_1k.jpg"))
    arm = load(os.path.join(d, stem + "_arm_1k.jpg"))
    diff = load(os.path.join(d, stem + "_diff_1k.jpg"))
    col = load(os.path.join(PROPS, colour))
    # UV-match check: colour in AmbientProps must be the same image as the mirror's diffuse
    a, b = resize(col, (128, 128)).mean(2), resize(diff, (128, 128)).mean(2)
    corr = float(np.corrcoef(a.ravel(), b.ravel())[0, 1])
    src = {"provider": "Poly Haven", "page": f"https://polyhaven.com/a/{phid}", "authors": authors,
           "mirror": os.path.relpath(d, DL), "uv_match_correlation": round(corr, 4)}
    save(f"PropMaps/{name}_Normal.png", renorm(nor), "normal (OpenGL, Unity)", src)
    save(f"PropMaps/{name}_MetallicSmoothness.png", ms_map(arm[..., 2], 1 - arm[..., 1]), "R metallic, A smoothness", src)
    save(f"PropMaps/{name}_Occlusion.jpg", np.dstack([arm[..., 0]] * 3), "ambient occlusion", src)
    print(name, "uv corr", round(corr, 3))


# ---------------------- 1b. approximations for colour-only Sketchfab props
SK_PROPS = [
    # name, colour file, base smoothness, metal, bump strength, author, page
    ("CardboardBoxSet", "CardboardBoxSet_Cardboard_Box_Mat.png", 0.18, 0.0, 5.0, "NotAnotherApocalypticCo.", "https://sketchfab.com/3d-models/set-of-cardboard-boxes-8986ba512f704ac5b253286a0d1ad8bb"),
    ("ACCondenserSet", "ACCondenserSet_conditioner.png", 0.38, 0.0, 4.0, "Lora", "https://sketchfab.com/3d-models/air-conditioners-65417369a2724c9ebf06e7698238a856"),
    ("IndoorACUnit", "IndoorACUnit_mIndoor.png", 0.55, 0.0, 2.0, "Rylae Shylna", "https://sketchfab.com/3d-models/indoor-air-conditioner-unit-d93c5557a9ba46afbb00e35f48343077"),
    ("NetworkServerRackRow", "NetworkServerRackRow_Material_001.png", 0.42, 0.55, 3.5, "Khoa Nguyen", "https://sketchfab.com/3d-models/network-server-rack-3b15fb8779274844b54e63a40194d450"),
    ("LightSwitch", "LightSwitch_lightswitch.png", 0.6, 0.0, 2.0, "Avot", "https://sketchfab.com/3d-models/light-switch-808c3de4dd874fc1b67ff15cd2645555"),
    ("PowerOutlet", "PowerOutlet_Material_001.png", 0.6, 0.0, 2.0, "Drake", "https://sketchfab.com/3d-models/outlet-192d0b1de17f4fcdbfeca3fe873c7bdf"),
]
for name, colour, sm, metal, k, author, page in SK_PROPS:
    col = load(os.path.join(PROPS, colour))
    lum = col @ np.array([0.299, 0.587, 0.114], np.float32)
    hp = lum - ndi.gaussian_filter(lum, 6)  # fine relief only, broad shading removed
    h = ndi.gaussian_filter(hp, 0.8)
    n = normal_from_height(h, k * 8)
    lo = ndi.gaussian_filter(lum, 12)
    ao = np.clip(1 - np.clip((lo - lum) * 2.5, 0, 0.6), 0.4, 1)  # crevices darker than surroundings
    smooth = np.clip(sm + (lum - lum.mean()) * 0.25 - np.abs(hp) * 1.5, 0.05, 0.9)
    src = {"provider": "derived by this project from the AmbientProps colour map", "page": page,
           "authors": author + " (colour map); derivation: this project"}
    note = "APPROXIMATION from colour (high-pass luminance relief); not a scanned map"
    save(f"PropMaps/{name}_Normal.png", n, "normal (OpenGL, Unity), derived", src, "CC-BY-4.0", note)
    save(f"PropMaps/{name}_MetallicSmoothness.png", ms_map(np.full_like(lum, metal), smooth), "R metallic, A smoothness, derived", src, "CC-BY-4.0", note)
    save(f"PropMaps/{name}_Occlusion.jpg", np.dstack([ao] * 3), "cavity occlusion, derived", src, "CC-BY-4.0", note)


# --------------------------------------------------- 2. tileable surface sets
TA = os.path.join(DL, "TextureArchive/PBR")


def ambient_set(name, folder, asset, size=1024, metal_const=None):
    p = os.path.join(TA, folder, asset + "_2K-JPG", asset + "_2K-JPG_")
    src = {"provider": "ambientCG", "page": f"https://ambientcg.com/view?id={asset}", "authors": "Lennart Demes (ambientCG)",
           "mirror": "github.com/Dadaskis/TextureArchive @ 461af934 " + os.path.relpath(p, DL)}
    col = resize(load(p + "Color.jpg"), (size, size))
    nor = renorm(resize(load(p + "NormalGL.jpg"), (size, size)))
    rough = resize(load(p + "Roughness.jpg", "L"), (size, size))
    met = resize(load(p + "Metalness.jpg", "L"), (size, size)) if os.path.exists(p + "Metalness.jpg") else np.full_like(rough, metal_const or 0.0)
    save(f"Surfaces/{name}_Color.jpg", col, "colour (tileable)", src)
    save(f"Surfaces/{name}_Normal.png", nor, "normal (OpenGL, tileable)", src)
    save(f"Surfaces/{name}_MetallicSmoothness.png", ms_map(met, 1 - rough), "R metallic, A smoothness", src)
    disp = resize(load(p + "Displacement.jpg", "L"), (size, size))
    ao = np.clip(1 - (ndi.gaussian_filter(disp, 4, mode="wrap") - disp) * 3, 0.5, 1)
    save(f"Surfaces/{name}_Occlusion.jpg", np.dstack([ao] * 3), "occlusion from displacement cavity", src)
    return p


pm = ambient_set("PaintedMetalChipped", "Metal", "PaintedMetal004")
# paint mask so code can retint the paint (red in the source)
op = resize(load(pm + "Opacity.jpg", "L"), (1024, 1024))
col = load(os.path.join(OUT, "Surfaces/PaintedMetalChipped_Color.jpg"))
g = np.repeat((col @ np.array([0.3, 0.59, 0.11], np.float32))[..., None], 3, 2)
neutral = np.where(op[..., None] > 0.5, g * 0.9, np.clip(g * 1.9, 0, 1))  # paint -> light grey (tint in shader), chips -> grey metal
save("Surfaces/PaintedMetalChipped_Neutral.jpg", np.clip(neutral, 0, 1), "colour with paint desaturated (multiply by tint in shader)",
     {"provider": "ambientCG", "page": "https://ambientcg.com/view?id=PaintedMetal004", "authors": "Lennart Demes (ambientCG)"})
save("Surfaces/PaintedMetalChipped_PaintMask.png", np.dstack([1 - op] * 3), "white = paint, black = bare metal chip",
     {"provider": "ambientCG", "page": "https://ambientcg.com/view?id=PaintedMetal004", "authors": "Lennart Demes (ambientCG)"})
ambient_set("PlasticTextured", "Carpet", "Rubber004", metal_const=0.0)

# Poly Haven rust_coarse_01
rp = os.path.join(DL, "EliteGamer007_6dof-drone-sim/assets/polyhaven/textures/rust_coarse_01/rust_coarse_01_")
src = {"provider": "Poly Haven", "page": "https://polyhaven.com/a/rust_coarse_01", "authors": "Dimitrios Savva, Rico Cilliers",
       "mirror": "github.com/EliteGamer007/6dof-drone-sim @ 3b970771 assets/polyhaven/textures/rust_coarse_01"}
arm = load(rp + "arm_1k.jpg")
save("Surfaces/RustCoarse_Color.jpg", load(rp + "diff_1k.jpg"), "colour (tileable)", src)
save("Surfaces/RustCoarse_Normal.png", renorm(load(rp + "nor_gl_1k.jpg")), "normal (OpenGL, tileable)", src)
save("Surfaces/RustCoarse_MetallicSmoothness.png", ms_map(arm[..., 2], 1 - arm[..., 1]), "R metallic, A smoothness", src)
save("Surfaces/RustCoarse_Occlusion.jpg", np.dstack([arm[..., 0]] * 3), "occlusion", src)

# Poly Haven cardboard_box_01: flat side panel cut out of the 2k atlas, rotated 45 deg, made tileable
cp = os.path.join(DL, "Geadas11_Claude_steam_game/assets/3d/models/cardboard_box_01/textures/cardboard_box_01_")
src = {"provider": "Poly Haven", "page": "https://polyhaven.com/a/cardboard_box_01", "authors": "Rahul Chaudhary",
       "mirror": "github.com/Geadas11/Claude_steam_game @ 8b6aa56f assets/3d/models/cardboard_box_01/textures (2k)"}
box = (1600, 790, 1792, 1334)  # x0,y0,x1,y1 in the rotated 2k image: one undamaged side panel


def rot_crop(path, normal=False):
    im = Image.open(path).convert("RGB").rotate(45, resample=Image.BICUBIC)
    a = np.asarray(im.crop(box), np.float32) / 255.0
    if normal:  # rotate the tangent-space vectors with the image (+45 deg CCW)
        v = a * 2 - 1
        c, s = math.cos(math.radians(45)), math.sin(math.radians(45))
        x, y = v[..., 0] * c - v[..., 1] * s, v[..., 0] * s + v[..., 1] * c
        a = np.dstack([x, y, v[..., 2]]) * 0.5 + 0.5
    return resize(a, (256, 512))


cc, cn, ca = rot_crop(cp + "diff_2k.jpg"), rot_crop(cp + "nor_gl_2k.jpg", True), rot_crop(cp + "arm_2k.jpg")
save("Surfaces/CardboardPanel_Color.jpg", seamless(cc), "colour (tileable, 0.5 x 1 aspect)", src)
save("Surfaces/CardboardPanel_Normal.png", renorm(seamless(cn)), "normal (OpenGL, tileable)", src)
save("Surfaces/CardboardPanel_MetallicSmoothness.png", ms_map(np.zeros(cc.shape[:2]), seamless(1 - ca[..., 1])), "R metallic, A smoothness", src)
save("Surfaces/CardboardPanel_Occlusion.jpg", np.dstack([seamless(ca[..., 0])] * 3), "occlusion", src)


# ---------------------------------------------------------- 3. decals
PROC = {"provider": "this project (procedural, Source~/build_realism.py)", "page": "", "authors": "this project"}


def decal(rel, rgb, alpha, kind, src=PROC, note=""):
    save(rel, bleed(np.dstack([rgb, alpha])), kind, src, note=note)


# 3a peeling paint from ambientCG PaintedMetal004 chip mask
pmsrc = {"provider": "ambientCG", "page": "https://ambientcg.com/view?id=PaintedMetal004", "authors": "Lennart Demes (ambientCG)"}
opfull = load(pm + "Opacity.jpg", "L")
dispf = load(pm + "Displacement.jpg", "L")
rng = np.random.default_rng(11)
for i, (y0, x0) in enumerate([(0, 0), (1024, 1024)]):
    chips = opfull[y0:y0 + 1024, x0:x0 + 1024]
    yy, xx = np.mgrid[0:1024, 0:1024] / 1023.0 - 0.5
    blob = fbm((1024, 1024), rng, 5, 3, wrap=False) * 0.55 + (0.5 - np.sqrt(xx ** 2 + yy ** 2)) * 1.6
    blob = np.clip((blob - 0.35) * 4, 0, 1)
    chips = np.clip(ndi.grey_dilation(chips, size=3) * 1.4, 0, 1) * blob
    edge = np.clip(ndi.grey_dilation(chips, size=5) - chips, 0, 1)  # lifted paint lip around each chip
    base = np.array([0.55, 0.53, 0.5]) * (0.8 + 0.4 * fbm((1024, 1024), rng, 4, 8, wrap=False))[..., None]
    rgb = base * (1 - 0.35 * edge[..., None]) + 0.0
    alpha = np.clip(chips + edge * 0.6, 0, 1)
    decal(f"Decals/PeelingPaint_Chips{i + 1:02d}.png", rgb, alpha,
          "decal: bare substrate showing through chipped paint (multiply/alpha blend over a painted wall)", pmsrc,
          "chip shapes from PaintedMetal004 opacity; substrate colour and radial falloff procedural")
    h = dispf[y0:y0 + 1024, x0:x0 + 1024] * blob - edge * 0.2
    save(f"Decals/PeelingPaint_Chips{i + 1:02d}_Normal.png", normal_from_height(h, 6), "normal (OpenGL)", pmsrc)


# 3b cobwebs (procedural strands; no CC0 photographic cobweb was reachable)
def strand_canvas(S):
    return Image.new("L", (S, S), 0)


def cobweb_corner(seed, S=1024):
    rng = np.random.default_rng(seed)
    ss = 2
    im = Image.new("L", (S * ss, S * ss), 0)
    d = ImageDraw.Draw(im)
    n_sp = rng.integers(11, 16)
    angs = np.sort(rng.uniform(0.03, math.pi / 2 - 0.03, n_sp))
    lens = rng.uniform(0.75, 1.0, n_sp) * S * ss * 0.98
    for a, L in zip(angs, lens):
        pts = [(math.cos(a) * L * t + rng.normal(0, 2), math.sin(a) * L * t + rng.normal(0, 2)) for t in np.linspace(0, 1, 12)]
        d.line(pts, fill=int(rng.uniform(150, 230)), width=2)
    r = rng.uniform(20, 40) * ss
    while r < S * ss * 0.95:
        for i in range(n_sp - 1):
            if rng.random() < 0.12:
                continue  # broken thread
            a0, a1 = angs[i], angs[i + 1]
            if r > min(lens[i], lens[i + 1]):
                continue
            p0 = np.array([math.cos(a0), math.sin(a0)]) * r
            p1 = np.array([math.cos(a1), math.sin(a1)]) * r
            mid = (p0 + p1) / 2 * (1 - 0.05 - rng.uniform(0, 0.06))  # sag toward the corner
            pts = [tuple((1 - t) ** 2 * p0 + 2 * (1 - t) * t * mid + t ** 2 * p1) for t in np.linspace(0, 1, 8)]
            d.line(pts, fill=int(rng.uniform(110, 210)), width=1 if rng.random() < 0.6 else 2)
        r *= rng.uniform(1.06, 1.12)
        r += rng.uniform(4, 14) * ss
    # torn loose strands and dust clumps
    for _ in range(25):
        x, y = rng.uniform(0, S * ss * 0.8, 2)
        pts = [(x, y)]
        for _ in range(6):
            x += rng.normal(0, 25); y += abs(rng.normal(20, 15)); pts.append((x, y))
        d.line(pts, fill=int(rng.uniform(70, 150)), width=1)
    im = im.resize((S, S), Image.LANCZOS).filter(ImageFilter.GaussianBlur(0.4))
    a = np.asarray(im, np.float32) / 255.0
    dust = (fbm((S, S), rng, 5, 16, wrap=False) > 0.72) * ndi.grey_dilation(a, size=5) * 0.35
    a = np.clip(a * 1.1 + ndi.gaussian_filter(dust, 1.5), 0, 1)
    # web is densest near the corner
    yy, xx = np.mgrid[0:S, 0:S] / S
    a *= np.clip(1.25 - np.sqrt(xx ** 2 + yy ** 2) * 0.6, 0, 1)
    tint = np.array([0.82, 0.8, 0.76]) - 0.25 * dust[..., None]
    return np.broadcast_to(tint, (S, S, 3)).astype(np.float32), a


for s in (1, 2):
    rgb, a = cobweb_corner(100 + s)
    decal(f"Decals/Cobweb_Corner{s:02d}.png", rgb, a, "decal/quad: cobweb anchored at the top-left corner (texel 0,0)",
          note="procedural, not photographic")


def cobweb_hanging(seed, W=512, H=1024):
    rng = np.random.default_rng(seed)
    ss = 2
    im = Image.new("L", (W * ss, H * ss), 0)
    d = ImageDraw.Draw(im)
    for _ in range(40):
        x0, x1 = sorted(rng.uniform(0, W * ss, 2))
        sag = rng.uniform(0.1, 0.9) * H * ss
        pts = [(x0 + (x1 - x0) * t, 4 * t * (1 - t) * sag + rng.normal(0, 3)) for t in np.linspace(0, 1, 20)]
        d.line(pts, fill=int(rng.uniform(90, 220)), width=1 if rng.random() < 0.7 else 2)
    for _ in range(14):  # dangling threads with dust beads
        x = rng.uniform(0.1, 0.9) * W * ss
        L = rng.uniform(0.3, 1.0) * H * ss
        pts = [(x + math.sin(t * 3) * rng.uniform(2, 10), t * L) for t in np.linspace(0, 1, 15)]
        d.line(pts, fill=int(rng.uniform(100, 200)), width=1)
        bx, by = pts[-1]
        rr = rng.uniform(3, 9)
        d.ellipse([bx - rr, by - rr, bx + rr, by + rr], fill=int(rng.uniform(80, 160)))
    im = im.resize((W, H), Image.LANCZOS).filter(ImageFilter.GaussianBlur(0.4))
    a = np.clip(np.asarray(im, np.float32) / 255.0 * 1.1, 0, 1)
    return np.broadcast_to(np.array([0.8, 0.78, 0.74], np.float32), (H, W, 3)), a


rgb, a = cobweb_hanging(7)
decal("Decals/Cobweb_Hanging01.png", rgb, a, "quad: sagging cobweb, top edge anchored to ceiling/shelf", note="procedural, not photographic")


# 3c rain on glass: drops + run-down trails, with normal
def rain_glass(seed, S=1024):
    rng = np.random.default_rng(seed)
    h = np.zeros((S, S), np.float32)
    yy, xx = np.mgrid[0:S, 0:S]

    def drop(cx, cy, r, sq=1.0):
        x0, x1 = int(max(cx - r - 2, 0)), int(min(cx + r + 2, S))
        y0, y1 = int(max(cy - r * sq - 2, 0)), int(min(cy + r * sq + 2, S))
        if x1 <= x0 or y1 <= y0:
            return
        dx = (xx[y0:y1, x0:x1] - cx) / r
        dy = (yy[y0:y1, x0:x1] - cy) / (r * sq)
        v = np.sqrt(np.clip(1 - dx * dx - dy * dy, 0, 1))
        h[y0:y1, x0:x1] = np.maximum(h[y0:y1, x0:x1], v * r / 12)

    for _ in range(2600):  # fine mist
        drop(rng.uniform(0, S), rng.uniform(0, S), rng.uniform(1.2, 3.5))
    for _ in range(140):
        drop(rng.uniform(0, S), rng.uniform(0, S), rng.uniform(4, 9), rng.uniform(1.0, 1.3))
    for _ in range(22):  # trails: wobble downward, thin out, leave beads
        x, y = rng.uniform(0, S), rng.uniform(-50, S * 0.5)
        w = rng.uniform(3, 6)
        while y < S and w > 0.8:
            drop(x, y, w * 0.7, 1.0)
            y += rng.uniform(1.5, 3)
            x += rng.normal(0, 0.6)
            w -= rng.uniform(0, 0.02)
            if rng.random() < 0.015:
                drop(x, y + 8, w * 1.3, 1.2)
        drop(x, y, w * 1.6, 1.3)
    a = np.clip(h * 2.5, 0, 1) * 0.35
    col = np.broadcast_to(np.array([0.85, 0.9, 0.95], np.float32), (S, S, 3))
    return col, a, h


col, a, hh = rain_glass(5)
decal("Decals/RainOnGlass.png", col, a, "glass overlay: drops and run-down trails (low alpha; use the normal for refraction/specular)", note="procedural; tileable horizontally only")
save("Decals/RainOnGlass_Normal.png", normal_from_height(hh, 10), "normal (OpenGL)", PROC)
save("Decals/RainOnGlass_MetallicSmoothness.png", ms_map(np.zeros_like(hh), np.where(hh > 0.01, 0.97, 0.0)), "R metallic, A smoothness (wet = 0.97)", PROC)


# 3d drink spills
def spill(seed, liquid, opacity, S=512, dried=False):
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:S, 0:S] / (S - 1) - 0.5
    rad = np.sqrt(xx ** 2 + (yy * rng.uniform(0.8, 1.2)) ** 2)
    f = fbm((S, S), rng, 5, 3, wrap=False)
    m = (0.33 - rad) + (f - 0.5) * 0.35
    shape = np.clip(m * 40, 0, 1)
    for _ in range(rng.integers(8, 20)):  # splash droplets
        cx, cy = rng.uniform(0.1, 0.9, 2) * S
        r = rng.uniform(2, 7)
        shape = np.maximum(shape, np.clip((r - np.hypot(xx * S + S / 2 - cx, yy * S + S / 2 - cy)) * 0.8, 0, 1))
    shape = ndi.gaussian_filter(shape, 0.8)
    rim = np.clip(shape - ndi.grey_erosion(shape, size=7), 0, 1)
    depth = ndi.gaussian_filter(shape, 8)
    c = np.array(liquid, np.float32)
    rgb = c[None, None, :] * (0.85 + 0.3 * f[..., None]) * (1 - 0.35 * rim[..., None])
    alpha = shape * (opacity * (0.55 + 0.45 * depth)) + rim * 0.25
    if dried:
        alpha = rim * 0.85 + shape * opacity * 0.25
    sm = np.where(shape > 0.05, 0.6 if dried else 0.96, 0.0)
    return rgb, np.clip(alpha, 0, 1), sm


for name, liquid, op, seed, dried in [("Cola", (0.20, 0.09, 0.04), 0.85, 21, False), ("Coffee", (0.32, 0.18, 0.08), 0.8, 22, False),
                                      ("Beer", (0.78, 0.55, 0.18), 0.5, 23, False), ("SodaDriedRing", (0.35, 0.2, 0.08), 0.6, 24, True)]:
    rgb, a, sm = spill(seed, liquid, op, dried=dried)
    decal(f"Decals/DrinkSpill_{name}.png", rgb, a, "floor decal: drink spill", note="procedural")
    save(f"Decals/DrinkSpill_{name}_MetallicSmoothness.png", ms_map(np.zeros_like(sm), sm), "R metallic, A smoothness", PROC)


# 3e bottle caps atlas (crown caps, unbranded)
def cap_tile(rng, colour, metal, bent, rusty, T=256):
    yy, xx = np.mgrid[0:T, 0:T] / (T - 1) - 0.5
    sx = 1.0 + (rng.uniform(0.25, 0.45) if bent else 0)
    ang = rng.uniform(0, math.pi)
    xr = (xx * math.cos(ang) + yy * math.sin(ang)) * sx
    yr = -xx * math.sin(ang) + yy * math.cos(ang)
    r = np.hypot(xr, yr)
    th = np.arctan2(yr, xr)
    R = 0.36 + 0.025 * np.cos(21 * th)  # 21 crimp flutes
    inside = np.clip((R - r) * T, 0, 1)
    rim = np.clip((r - 0.27) / (R - 0.27 + 1e-6), 0, 1) * inside
    light = 0.75 + 0.35 * (-(xr + yr) / 0.5) * rim - 0.25 * rim * (0.5 + 0.5 * np.cos(21 * th))
    top = (r < 0.27)
    light = np.where(top, 0.9 + 0.15 * (-(xx + yy)), light)
    base = np.array(colour, np.float32)
    rgb = base[None, None, :] * light[..., None]
    if rusty:
        f = fbm((T, T), rng, 4, 6, wrap=False)
        rs = np.clip((f - 0.55) * 5, 0, 1)[..., None]
        rgb = rgb * (1 - rs) + np.array([0.42, 0.22, 0.1]) * rs
    shadow = ndi.gaussian_filter(np.roll(np.roll(inside, 6, 0), 6, 1), 5) * 0.5
    alpha = np.maximum(inside, shadow)
    rgb = np.where(inside[..., None] > 0.5, rgb, 0.0)
    sm = np.where(inside > 0.5, 0.75 if metal > 0.5 else 0.6, 0.0)
    return np.clip(rgb, 0, 1), alpha, np.where(inside > 0.5, metal, 0.0), sm


rng = np.random.default_rng(31)
atlas = np.zeros((1024, 1024, 4), np.float32)
msat = np.zeros((1024, 1024, 4), np.float32)
palette = [((0.85, 0.7, 0.3), 1), ((0.8, 0.8, 0.82), 1), ((0.7, 0.08, 0.06), 0), ((0.1, 0.35, 0.15), 0),
           ((0.1, 0.2, 0.55), 0), ((0.9, 0.88, 0.85), 0), ((0.12, 0.12, 0.12), 0), ((0.85, 0.5, 0.1), 0)]
for i in range(16):
    c, m = palette[i % 8]
    rgb, a, met, sm = cap_tile(rng, c, m, bent=i >= 8 and i % 2 == 0, rusty=i >= 12)
    y, x = (i // 4) * 256, (i % 4) * 256
    atlas[y:y + 256, x:x + 256] = bleed(np.dstack([rgb, a]))
    msat[y:y + 256, x:x + 256] = ms_map(met, sm)
save("Decals/BottleCaps_Atlas.png", atlas, "4x4 atlas (256 px cells): crown caps, top view, flat + bent + rusty, with contact shadow", PROC, note="procedural, unbranded")
save("Decals/BottleCaps_Atlas_MetallicSmoothness.png", msat, "R metallic, A smoothness", PROC)


# 3f pigeon droppings atlas
def dropping(rng, T=256):
    yy, xx = np.mgrid[0:T, 0:T] / (T - 1) - 0.5
    f = fbm((T, T), rng, 4, 4, wrap=False)
    r = np.hypot(xx * rng.uniform(0.8, 1.3), yy)
    body = np.clip(((0.2 * rng.uniform(0.6, 1.1)) - r + (f - 0.5) * 0.18) * 60, 0, 1)
    for _ in range(rng.integers(4, 14)):  # splatter dots
        cx, cy = rng.normal(0, 0.2, 2)
        rr = rng.uniform(0.008, 0.03)
        body = np.maximum(body, np.clip((rr - np.hypot(xx - cx, yy - cy)) * 300, 0, 1))
    core = np.clip(((0.09 * rng.uniform(0.5, 1.2)) - np.hypot((xx - rng.normal(0, 0.04)) * 0.7, yy - rng.normal(0, 0.04)) + (f - 0.5) * 0.2) * 25, 0, 1)
    core *= np.clip((fbm((T, T), rng, 3, 8, wrap=False) - 0.3) * 3, 0, 1) * 0.8
    white = np.array([0.88, 0.87, 0.82]) * (0.85 + 0.2 * f[..., None])
    dark = np.array([0.33, 0.34, 0.26]) * (0.8 + 0.4 * f[..., None])
    rgb = white * (1 - core[..., None]) + dark * core[..., None]
    h = ndi.gaussian_filter(body * 0.6 + core * 0.4, 2)
    return rgb, ndi.gaussian_filter(body, 0.7), h


rng = np.random.default_rng(41)
atlas = np.zeros((1024, 1024, 4), np.float32)
hat = np.zeros((1024, 1024), np.float32)
for i in range(16):
    rgb, a, h = dropping(rng)
    y, x = (i // 4) * 256, (i % 4) * 256
    atlas[y:y + 256, x:x + 256] = bleed(np.dstack([rgb, a]))
    hat[y:y + 256, x:x + 256] = h
save("Decals/PigeonDroppings_Atlas.png", atlas, "4x4 atlas (256 px cells): bird droppings for ledges, kerbs, bench tops", PROC, note="procedural")
save("Decals/PigeonDroppings_Atlas_Normal.png", normal_from_height(hat, 8), "normal (OpenGL)", PROC)

# ---------------------------------------------------------------- manifest
tot = sum(os.path.getsize(os.path.join(OUT, r["file"])) for r in REC)
json.dump({"files": REC, "total_bytes": tot}, open(os.path.join(OUT, "_build_manifest.json"), "w"), indent=1)
print("files", len(REC), "bytes", tot)
