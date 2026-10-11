#!/usr/bin/env python3
"""Tileable textures taken from the project's own reference renders.

The user's reference sheets (Docs/LayoutReferences) show the store palette on clean,
evenly lit surfaces. Woven and grained materials (walnut, velvet, suede, safe steel)
are cut from those images, flattened (the render's lighting gradient is
divided out) and made seamless with a variance-preserving blend so the tile keeps its
contrast. Materials that no render shows flat or evenly lit (polished floor tiles, stone
tops, brushed bronze, wall plaster, pavement, asphalt) are synthesised with colours sampled from the renders.
Every output gets a matching tangent-space normal map from its high-passed luminance.

    python3 -P Tools/reference_textures.py [--size 1024] [--out Prototype/ThirdParty/ReferenceTextures]

Crop boxes are in original-image pixels; the RECIPES table is the provenance record.
Requires numpy and pillow.
"""
import argparse, math, os, sys
import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from make_textures import periodic_noise, stretch, speckle, to_image, mix, normal_map  # noqa: E402

SHEETS = 'Docs/LayoutReferences/StoreAssets/'
VIEWS = 'Docs/LayoutReferences/NightStore/'

# name: (image, crop box x0 y0 x1 y1, metres per repeat (documentation), normal strength)
RECIPES = {
    'Walnut':         (SHEETS + '02-store-fixtures-sheet.png', (740, 190, 1020, 300), 0.6, 2.5),
    'VelvetCharcoal': (SHEETS + 'Expansion/09-display-inserts.png', (1060, 130, 1250, 290), 0.25, 1.5),
    'SuedeCream':     (SHEETS + 'Expansion/09-display-inserts.png', (960, 700, 1080, 820), 0.25, 1.2),
    'SafeSteel':      (SHEETS + 'Expansion/07-safe-variants.png', (175, 100, 240, 330), 0.5, 1.5),
}
# Palette samples for the synthesised materials: (image, box) averaged after flattening.
SAMPLES = {
    'floor':  (VIEWS + '07-rear-safe-office.png', (1150, 720, 1450, 900)),
    'stone':  (SHEETS + 'Expansion/10-shop-furniture.png', (930, 90, 1450, 150)),
    'bronze': (SHEETS + 'Expansion/09-display-inserts.png', (420, 205, 455, 325)),
    'plaster': (VIEWS + '07-rear-safe-office.png', (330, 75, 800, 190)),
}


def load_box(path, box):
    return np.asarray(Image.open(path).convert('RGB').crop(box), dtype=np.float32) / 255


def blur(a, sigma):
    """Gaussian blur of a float image (per channel) through Pillow."""
    out = []
    for c in range(a.shape[2]):
        img = Image.fromarray(np.clip(a[..., c] * 255, 0, 255).astype(np.uint8))
        out.append(np.asarray(img.filter(ImageFilter.GaussianBlur(sigma)), dtype=np.float32) / 255)
    return np.dstack(out)


def flatten(a):
    """Divide out the render's lighting gradient, keeping the mean colour."""
    low = blur(a, max(4, min(a.shape[:2]) / 6))
    mean = a.reshape(-1, 3).mean(axis=0)
    return np.clip(a / np.maximum(low, 1e-3) * mean, 0, 1)


def seamless(a):
    """Blend with the half-wrapped copy; dividing by the weight norm keeps the variance
    (a plain cross-fade would wash the grain out where both copies meet)."""
    h, w = a.shape[:2]
    rolled = np.roll(np.roll(a, h // 2, axis=0), w // 2, axis=1)
    y, x = np.mgrid[0:h, 0:w]
    wt = (np.sin(x / w * math.pi) * np.sin(y / h * math.pi))[..., None]
    mean = a.reshape(-1, 3).mean(axis=0)
    out = mean + ((a - mean) * wt + (rolled - mean) * (1 - wt)) / np.sqrt(wt ** 2 + (1 - wt) ** 2)
    return np.clip(out, 0, 1)


def luminance(a):
    return a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114


def height_from(a):
    lum = luminance(a)
    return lum - blur(lum[..., None].repeat(3, axis=2), 6)[..., 0]


def tile_from_crop(path, box, size):
    a = flatten(load_box(path, box))
    a = np.asarray(to_image(a).resize((size, size), Image.LANCZOS), dtype=np.float32) / 255
    return seamless(a)


def sample(path, box):
    return flatten(load_box(path, box)).reshape(-1, 3).mean(axis=0)


def floor_tile(size, base, seed=3):
    """Polished beige porcelain, 2x2 tiles of 0.6 m per texture, hairline grout,
    per-tile tint and soft cloudy veining at very low contrast."""
    y, x = np.mgrid[0:size, 0:size] / size
    cells = 2
    tx, ty = np.floor(x * cells).astype(int), np.floor(y * cells).astype(int)
    rng = np.random.default_rng(seed)
    # Brightness-only variation: per-channel tints shift the hue tile by tile.
    tint = rng.normal(0, 0.012, (cells, cells, 1)).astype(np.float32)
    cloud = periodic_noise(size, 6, seed + 1, 0.55, 3)
    veins = np.abs(np.sin((stretch(periodic_noise(size, 5, seed + 2, 0.5, 2), 3) * 9 + x * 2) * math.pi))
    rgb = base[None, None, :] * (0.93 + 0.12 * cloud[..., None]) + tint[ty, tx]
    rgb = mix(rgb, base * 0.88, (veins < 0.06).astype(np.float32) * 0.35)
    gx = np.minimum((x * cells) % 1, 1 - (x * cells) % 1) * size / cells
    gy = np.minimum((y * cells) % 1, 1 - (y * cells) % 1) * size / cells
    grout = np.clip(1.6 - np.minimum(gx, gy), 0, 1)
    rgb = mix(rgb, base * 0.78, grout * 0.85)
    height = 1 - grout * 0.6 + cloud * 0.02
    return np.clip(rgb, 0, 1), height


def stone_top(size, base, seed=5):
    """Cream limestone/marble top: soft mottling and a few faint drifting veins."""
    y, x = np.mgrid[0:size, 0:size] / size
    cloud = periodic_noise(size, 6, seed, 0.55, 3)
    flow = stretch(periodic_noise(size, 5, seed + 1, 0.5, 2), 4)
    veins = np.abs(np.sin((flow * 7 + y) * math.pi))
    rgb = base[None, None, :] * (0.94 + 0.1 * cloud[..., None])
    rgb = mix(rgb, base * 0.82, np.clip(0.05 - veins, 0, 1) * 10 * 0.5)
    return np.clip(rgb, 0, 1), cloud * 0.2


def bronze_brushed(size, base, seed=7):
    """Brushed bronze: long horizontal scratches over a slow tonal drift."""
    streaks = np.rot90(stretch(np.rot90(speckle(size, seed)), 48), -1)
    drift = periodic_noise(size, 3, seed + 1, 0.5, 2)
    rgb = base[None, None, :] * (0.86 + 0.18 * streaks[..., None] + 0.08 * drift[..., None])
    return np.clip(rgb, 0, 1), streaks * 0.3


def plaster(size, base, seed=11):
    """Warm matt plaster: soft trowel clouds and fine grain around the sampled wall colour."""
    cloud = periodic_noise(size, 6, seed, 0.55, 2)
    grain = speckle(size, seed + 1, 0.8)
    lum = 0.95 + 0.07 * cloud + 0.03 * (grain - 0.5)
    return np.clip(base[None, None, :] * lum[..., None], 0, 1), cloud * 0.3 + grain * 0.1


def pavement_slab(size, seed=9):
    """Night pavement: 2x2 concrete slabs of 0.6 m, darker joints, aggregate speckle."""
    y, x = np.mgrid[0:size, 0:size] / size
    cells = 2
    tx, ty = np.floor(x * cells).astype(int), np.floor(y * cells).astype(int)
    rng = np.random.default_rng(seed)
    tint = rng.normal(0, 0.025, (cells, cells)).astype(np.float32)
    base = np.array([0.40, 0.40, 0.39], dtype=np.float32)
    grain = speckle(size, seed + 1, 0.6)
    cloud = periodic_noise(size, 6, seed + 2, 0.55, 4)
    lum = 0.88 + 0.16 * cloud + 0.08 * (grain - 0.5) + tint[ty, tx]
    rgb = base[None, None, :] * lum[..., None]
    gx = np.minimum((x * cells) % 1, 1 - (x * cells) % 1) * size / cells
    gy = np.minimum((y * cells) % 1, 1 - (y * cells) % 1) * size / cells
    joint = np.clip(4 - np.minimum(gx, gy), 0, 3) / 3
    rgb = mix(rgb, base * 0.45, joint)
    return np.clip(rgb, 0, 1), 1 - joint * 0.8 + grain * 0.05


def asphalt_wet(size, seed=13):
    """Dark asphalt with light aggregate; wetness comes from material smoothness, not albedo."""
    grain = speckle(size, seed, 0.5)
    stones = (speckle(size, seed + 1, 0.0) > 0.985).astype(np.float32)
    cloud = periodic_noise(size, 5, seed + 2, 0.5, 4)
    lum = 0.11 + 0.03 * cloud + 0.03 * (grain - 0.5) + 0.12 * stones
    rgb = np.dstack([lum, lum, lum * 1.03])
    return np.clip(rgb, 0, 1), grain * 0.4 + stones * 0.6


def write(out, name, rgb, height, strength):
    to_image(rgb).save(os.path.join(out, name + '_Color.png'))
    normal_map(height.astype(np.float32), strength).save(os.path.join(out, name + '_Normal.png'))
    print('wrote', name)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--out', default='Prototype/ThirdParty/ReferenceTextures')
    parser.add_argument('--size', type=int, default=1024)
    args = parser.parse_args()
    os.makedirs(args.out, exist_ok=True)
    for name, (path, box, metres, strength) in RECIPES.items():
        rgb = tile_from_crop(path, box, args.size)
        write(args.out, name, rgb, height_from(rgb), strength * 6)
    palette = {k: sample(*v) for k, v in SAMPLES.items()}
    write(args.out, 'FloorTile', *floor_tile(args.size, palette['floor']), 4)
    write(args.out, 'StoneTop', *stone_top(args.size, palette['stone']), 2)
    write(args.out, 'BronzeBrushed', *bronze_brushed(args.size, palette['bronze']), 3)
    # The sampled wall sits in shade in the render; lift it to a lit-wall albedo.
    write(args.out, 'PlasterWarm', *plaster(args.size, np.clip(palette['plaster'] * 1.3, 0, 0.8)), 2)
    write(args.out, 'PavementSlab', *pavement_slab(args.size), 4)
    write(args.out, 'AsphaltWet', *asphalt_wet(args.size), 3)


if __name__ == '__main__':
    main()
