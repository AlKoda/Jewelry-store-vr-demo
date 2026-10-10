#!/usr/bin/env python3
"""Generate tileable albedo textures (and matching normal maps) for the shop.

No external art: everything is noise, geometry and a little colour theory, so the
output is project-owned. Every texture wraps seamlessly (periodic noise) and is
written as PNG into the given folder, 1024x1024 unless --size is given.

    python3 -P Tools/make_textures.py Prototype/ThirdParty/GeneratedTextures [--size 1024]

Requires numpy and pillow. Optionally pass --from-photo IMAGE NAME to turn any photo into a
seamless tile (mirror-blend) named NAME.png as well.
"""
import argparse, math, os, sys
import numpy as np
from PIL import Image, ImageFilter


def periodic_noise(size, octaves=5, seed=0, persistence=0.5, base=4):
    """Tileable fractal value noise in [0,1]: each octave is a periodic grid of random values
    upsampled with bicubic wrap, so the result repeats exactly at the texture edge."""
    rng = np.random.default_rng(seed)
    total = np.zeros((size, size), dtype=np.float32)
    amplitude, norm = 1.0, 0.0
    for octave in range(octaves):
        cells = base * (2 ** octave)
        grid = rng.random((cells, cells)).astype(np.float32)
        # Tile 3x3 and crop the centre so the bicubic filter sees periodic neighbours.
        tiled = np.tile(grid, (3, 3))
        image = Image.fromarray((tiled * 255).astype(np.uint8)).resize((size * 3, size * 3), Image.BICUBIC)
        layer = np.asarray(image, dtype=np.float32)[size:2 * size, size:2 * size] / 255.0
        total += layer * amplitude
        norm += amplitude
        amplitude *= persistence
    return total / norm


def stretch(field, factor):
    """Anisotropic blur of a periodic field along y (grain direction): tile horizontally,
    shrink x by factor, enlarge back, crop. Keeps the result periodic."""
    size = field.shape[0]
    tiled = np.tile(field, (1, 3))
    image = Image.fromarray((tiled * 255).astype(np.uint8))
    small = image.resize((max(3, size * 3 // factor), size), Image.BICUBIC).resize((size * 3, size), Image.BICUBIC)
    return np.asarray(small, dtype=np.float32)[:, size:2 * size] / 255


def speckle(size, seed, blur=0.0):
    """Per-pixel random grain (periodic by construction)."""
    rng = np.random.default_rng(seed)
    grain = rng.random((size, size)).astype(np.float32)
    if blur > 0:
        grain = np.asarray(Image.fromarray((grain * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(blur)), dtype=np.float32) / 255
    return grain


def to_image(rgb):
    return Image.fromarray(np.clip(rgb * 255, 0, 255).astype(np.uint8), 'RGB')


def mix(a, b, t):
    t = t[..., None] if t.ndim == 2 else t
    return a * (1 - t) + b * t


def colour(hex_value):
    return np.array([int(hex_value[i:i + 2], 16) / 255 for i in (0, 2, 4)], dtype=np.float32)


def normal_map(height, strength=2.0):
    """Tangent-space normal map from a periodic height field (wrap-around gradients)."""
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * strength
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * strength
    n = np.dstack([-dx, -dy, np.ones_like(height)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return to_image(n * 0.5 + 0.5)


def stone_tiles(size, seed):
    """Pale stone floor tiles, 4x4 per texture, narrow dark grout, per-tile tint and veining."""
    y, x = np.mgrid[0:size, 0:size] / size
    tiles = 4
    u, v = (x * tiles) % 1, (y * tiles) % 1
    grout = 0.025
    edge = np.minimum(np.minimum(u, 1 - u), np.minimum(v, 1 - v))
    in_tile = np.clip((edge - grout) / 0.012, 0, 1)
    rng = np.random.default_rng(seed)
    tint = rng.random((tiles, tiles)).astype(np.float32)
    tile_tint = tint[(y * tiles).astype(int) % tiles, (x * tiles).astype(int) % tiles]
    veins = periodic_noise(size, 6, seed + 1, 0.55, 3)
    grain = periodic_noise(size, 7, seed + 2, 0.5, 16)
    stone = mix(colour('c9c1b1'), colour('ddd6c8'), 0.6 * tile_tint + 0.4 * veins)
    stone = mix(stone, colour('b8ad99'), np.clip((veins - 0.62) * 6, 0, 1) * 0.5)
    stone *= (0.93 + 0.14 * grain)[..., None]
    rgb = mix(colour('6b655c'), stone, in_tile)
    height = in_tile * (0.6 + 0.4 * grain) * 0.6 + 0.4 * veins * in_tile
    return to_image(rgb), normal_map(height, 1.5)


def plaster(size, seed):
    """Warm, lightly troweled plaster for the walls."""
    fine = periodic_noise(size, 7, seed, 0.6, 12)
    broad = periodic_noise(size, 4, seed + 1, 0.5, 2)
    rgb = mix(colour('d9c9b2'), colour('e9ddc9'), 0.5 * fine + 0.5 * broad)
    return to_image(rgb), normal_map(fine * 0.7 + broad * 0.3, 0.8)


def walnut(size, seed):
    """Dark walnut planks: fine grain lines running along y, wavy from stretched noise,
    with broad tonal variation and three plank seams."""
    y, x = np.mgrid[0:size, 0:size] / size
    wave = stretch(periodic_noise(size, 5, seed + 1, 0.5, 2), 24)
    broad = stretch(periodic_noise(size, 4, seed, 0.5, 2), 6)
    # Grain lines: phase advances across x, bent by the wave field.
    lines = 0.5 + 0.5 * np.sin(2 * math.pi * (x * 28 + wave * 2.2))
    pores = stretch(speckle(size, seed + 2), 12)
    rgb = mix(colour('4a2e1b'), colour('7a4f2d'), 0.6 * broad + 0.4 * pores)
    rgb = mix(rgb, colour('2e1b0f'), (lines ** 3) * 0.55)
    seam = np.minimum((x * 3) % 1, 1 - (x * 3) % 1) < 0.004
    rgb[seam] *= 0.5
    return to_image(rgb), normal_map(lines * 0.35 + pores * 0.25 - seam * 0.6, 1.0)


def brushed_brass(size, seed):
    """Satin brass: directional micro-scratches over a warm metallic base."""
    scratches = np.asarray(Image.fromarray((periodic_noise(size, 5, seed, 0.5, 4) * 255).astype(np.uint8))
                           .resize((size, size // 32), Image.BICUBIC).resize((size, size), Image.BICUBIC), dtype=np.float32) / 255
    broad = periodic_noise(size, 3, seed + 1, 0.5, 2)
    rgb = mix(colour('8a6a35'), colour('c9a45a'), 0.6 * scratches + 0.4 * broad)
    return to_image(rgb), normal_map(scratches, 0.5)


def dark_carpet(size, seed):
    """Charcoal carpet for the safe room: dense fibre noise."""
    fibre = periodic_noise(size, 8, seed, 0.65, 32)
    broad = periodic_noise(size, 3, seed + 1, 0.5, 2)
    rgb = mix(colour('2b2d31'), colour('44474d'), 0.75 * fibre + 0.25 * broad)
    return to_image(rgb), normal_map(fibre, 1.0)


def asphalt(size, seed):
    """Street asphalt: aggregate speckle over broad tonal patches, a few pale chips."""
    grain = speckle(size, seed, 0.8)
    patches = periodic_noise(size, 4, seed + 1, 0.5, 2)
    chips = np.clip((speckle(size, seed + 3, 1.2) - 0.72) * 8, 0, 1)
    rgb = mix(colour('2b2d30'), colour('45484c'), 0.55 * grain + 0.45 * patches)
    rgb = mix(rgb, colour('8a8782'), chips * 0.6)
    return to_image(rgb), normal_map(grain * 0.7 + chips * 0.3, 1.6)


def pavement(size, seed):
    """Concrete paving slabs, 2x2 per texture, with chamfered joints and wear."""
    y, x = np.mgrid[0:size, 0:size] / size
    u, v = (x * 2) % 1, (y * 2) % 1
    edge = np.minimum(np.minimum(u, 1 - u), np.minimum(v, 1 - v))
    in_slab = np.clip((edge - 0.012) / 0.02, 0, 1)
    wear = periodic_noise(size, 6, seed, 0.55, 3)
    fine = periodic_noise(size, 8, seed + 1, 0.6, 16)
    slab = mix(colour('9c9890'), colour('b9b5ab'), 0.5 * wear + 0.5 * fine)
    rgb = mix(colour('5e5a54'), slab, in_slab)
    return to_image(rgb), normal_map(in_slab * (0.7 + 0.3 * fine), 1.6)


def marble(size, seed):
    """White marble: soft grey clouds, two scales of thin veins following the clouds."""
    clouds = periodic_noise(size, 5, seed, 0.6, 2)
    detail = periodic_noise(size, 7, seed + 1, 0.5, 6)
    def veins(field, frequency, width):
        return np.clip(1 - np.abs(np.sin(2 * math.pi * (field * frequency))) / width, 0, 1)
    coarse = veins(clouds + 0.15 * detail, 2.5, 0.08)
    fine = veins(clouds * 1.7 + 0.3 * detail, 3.5, 0.05)
    rgb = mix(colour('e4e2dc'), colour('f6f4f0'), 0.6 * detail + 0.4 * clouds)
    rgb = mix(rgb, colour('b9bcc2'), np.clip(clouds - 0.55, 0, 1) * 1.2)
    rgb = mix(rgb, colour('7f838b'), coarse * 0.55 + fine * 0.3)
    return to_image(rgb), normal_map(detail * 0.2, 0.3)


def ceiling(size, seed):
    """Matte painted ceiling with faint roller texture."""
    fine = periodic_noise(size, 7, seed, 0.6, 16)
    rgb = mix(colour('ece9e3'), colour('f6f4ef'), fine)
    return to_image(rgb), normal_map(fine, 0.3)


GENERATORS = {
    'StoneTiles': stone_tiles, 'Plaster': plaster, 'Walnut': walnut, 'BrushedBrass': brushed_brass,
    'DarkCarpet': dark_carpet, 'Asphalt': asphalt, 'Pavement': pavement, 'Marble': marble, 'Ceiling': ceiling,
}


def seamless_from_photo(path, size):
    """Mirror-blend a photo into a seamless tile: blend each edge with its wrapped neighbour."""
    image = Image.open(path).convert('RGB').resize((size, size), Image.LANCZOS)
    a = np.asarray(image, dtype=np.float32) / 255
    rolled = np.roll(np.roll(a, size // 2, axis=0), size // 2, axis=1)
    y, x = np.mgrid[0:size, 0:size] / size
    # Weight 1 at the centre, 0 at the edges: edges take the rolled (wrapped) image.
    w = (np.sin(x * math.pi) * np.sin(y * math.pi))[..., None]
    return to_image(a * w + rolled * (1 - w))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('out')
    parser.add_argument('--size', type=int, default=1024)
    parser.add_argument('--seed', type=int, default=11)
    parser.add_argument('--from-photo', nargs=2, metavar=('IMAGE', 'NAME'), action='append', default=[])
    args = parser.parse_args()
    os.makedirs(args.out, exist_ok=True)
    for index, (name, generator) in enumerate(GENERATORS.items()):
        albedo, normal = generator(args.size, args.seed + index * 17)
        albedo.save(os.path.join(args.out, name + '.png'), optimize=True)
        normal.save(os.path.join(args.out, name + '_Normal.png'), optimize=True)
        print(name)
    for image, name in args.from_photo:
        seamless_from_photo(image, args.size).save(os.path.join(args.out, name + '.png'), optimize=True)
        print(name, '(from photo)')


if __name__ == '__main__':
    sys.exit(main())
