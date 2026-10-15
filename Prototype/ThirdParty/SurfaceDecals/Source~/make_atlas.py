"""Packs SurfaceDecals cells into the two wiring-batch texture atlases (project-owned code).

Run it from a copy outside the pack and pass the pack directory, so nothing inside the pack is
imported (python3 -I also keeps the script and current directories off sys.path):

    python3 -I make_atlas.py <SurfaceDecals dir> <out dir>            build, then verify
    python3 -I make_atlas.py <SurfaceDecals dir> <out dir> --verify   verify the existing files only

Per set (Ambient, Evidence) it writes
    <Set>_Color.png               2048 x 2048 RGBA, sRGB, straight alpha
    <Set>_Normal.png              2048 x 2048 RGB, OpenGL / Unity +Y, flat = (128, 128, 255)
    <Set>_MetallicSmoothness.png  1024 x 1024 RGBA, R = metallic, A = smoothness (same layout, half size)
    <Set>.json                    JsonUtility {width, height, cells: [{name, x, y, w, h}]},
                                  colour-atlas pixels, y counted from the BOTTOM edge (Unity UV)

The layouts below are fixed: a cell is resampled only to the size its rect states (aspect checked to
1 %), never rotated or flipped. Any failed check exits with status 1 and names the offending cell.
Needs numpy and Pillow only (no scipy), so it runs under python3 -I without user site-packages.
"""
import json
import os
import sys

import numpy as np
from PIL import Image

PAGE = 2048            # colour and normal atlas size
MS_PAGE = PAGE // 2    # metallic-smoothness atlas size (same layout at half resolution)
GAP = 16               # minimum distance between two cells
EDGE = 8               # minimum distance between a cell and the page edge
ASPECT_TOL = 0.01
FLAT = (128, 128, 255)
UNIT_TOL = 0.01        # 8-bit normals decode to |n| = 1 within this
OWN = 'own'            # smoothness column: the decal ships its own _MetallicSmoothness map

# name, rect (x, y, w, h) with y from the TOP edge, ships own _Normal, smoothness, real size (m)
SETS = {
    'Ambient': [
        ('Puddle_Kerbside', (16, 8, 992, 496), True, OWN, (2.0, 1.0)),
        ('Puddle_Round', (1032, 8, 496, 496), True, OWN, (1.2, 1.2)),
        ('Puddle_Cluster', (1544, 8, 496, 496), True, OWN, (1.5, 1.5)),
        ('OilStain', (8, 520, 496, 496), False, OWN, (1.0, 1.0)),
        ('LitterScatter', (520, 520, 496, 496), True, 0.4, (1.0, 1.0)),
        ('LeafLitter_Wet', (1032, 520, 496, 496), True, OWN, (1.0, 1.0)),
        ('ChewingGum_Scatter', (1544, 520, 496, 496), True, 0.3, (1.0, 1.0)),
        ('GrimeStreaks_Wall', (8, 1040, 496, 992), False, 0.15, (0.6, 1.2)),
        ('WaterStain_FloorDried', (520, 1032, 496, 496), False, 0.2, (1.0, 1.0)),
        ('FloorScuffs_Heel', (1032, 1032, 496, 496), False, 0.35, (1.0, 1.0)),
        ('FloorScratches_Fine', (1544, 1032, 496, 496), False, 0.4, (1.0, 1.0)),
        ('DirtEdge_WallBase', (528, 1544, 992, 248), False, 0.15, (2.0, 0.25)),
    ],
    'Evidence': [
        ('ShoePrint_Sneaker_Mud_L', (8, 8, 496, 992), True, 0.45, (0.2, 0.4)),
        ('ShoePrint_Sneaker_Mud_R', (520, 8, 496, 992), True, 0.45, (0.2, 0.4)),
        ('ShoePrint_Sneaker_Dust_L', (1032, 8, 496, 992), True, 0.15, (0.2, 0.4)),
        ('ShoePrint_Sneaker_Dust_R', (1544, 8, 496, 992), True, 0.15, (0.2, 0.4)),
        ('Fingerprints_Atlas', (8, 1016, 1024, 512), False, 0.5, (0.1, 0.05)),
        ('PryMarks_DoorFrame', (8, 1544, 496, 496), True, OWN, (0.12, 0.12)),
        ('GlassHandprint_Latent', (520, 1544, 496, 496), False, 0.5, (0.19, 0.19)),
        ('DirtTracked_Entrance', (1048, 1016, 992, 992), False, 0.3, (1.5, 1.5)),
    ],
}
SHOE_PRINTS = ('ShoePrint_',)   # cells whose toe must stay at +V (top of the image)

LANCZOS = Image.Resampling.LANCZOS
BOX = Image.Resampling.BOX
BILINEAR = Image.Resampling.BILINEAR


class AtlasError(Exception):
    def __init__(self, set_name, cell, msg):
        super().__init__('%s / %s: %s' % (set_name, cell, msg))


def q8(v):
    """0..1 constant to the 8-bit value stored in the file (round half up)."""
    return int(np.floor(v * 255.0 + 0.5 + 1e-6))


def srgb_to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def linear_to_srgb(c):
    c = np.clip(c, 0.0, 1.0)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)


def resize_f(a, w, h, filt):
    im = Image.fromarray(np.ascontiguousarray(a, np.float32), 'F')
    return np.asarray(im.resize((w, h), filt), np.float64)


def load(path, mode):
    im = Image.open(path)
    im.load()
    if im.mode != mode:
        raise ValueError('%s is %s, expected %s' % (os.path.basename(path), im.mode, mode))
    return np.asarray(im)


def corr(a, b):
    a = a.ravel() - a.mean()
    b = b.ravel() - b.mean()
    d = np.sqrt((a * a).sum() * (b * b).sum())
    return float((a * b).sum() / d) if d > 0 else 0.0


def half(rect):
    return tuple(v // 2 for v in rect)


def nearest_cell(cells, mask, scale=1):
    """Name of the cell closest to the first True pixel of mask (for failure messages)."""
    py, px = (int(v) * scale for v in np.argwhere(mask)[0])
    def dist(rect):
        x, y, w, h = rect
        return max(x - px, 0, px - (x + w - 1)) ** 2 + max(y - py, 0, py - (y + h - 1)) ** 2
    name = min(cells, key=lambda c: dist(c[1]))[0]
    return name if dist(dict((c[0], c[1]) for c in cells)[name]) == 0 else 'gap next to ' + name


# --------------------------------------------------------------------------------------------
# Layout checks (run before building and again on the JSON read back)

def check_layout(set_name, cells, src_sizes):
    for name, (x, y, w, h), _, ms, size_m in cells:
        if min(w, h) <= 0:
            raise AtlasError(set_name, name, 'empty rect')
        if x < EDGE or y < EDGE or x + w > PAGE - EDGE or y + h > PAGE - EDGE:
            raise AtlasError(set_name, name, 'rect %s is not inside the page with a %d px edge'
                             % ((x, y, w, h), EDGE))
        if any(v % 2 for v in (x, y, w, h)):
            raise AtlasError(set_name, name, 'rect %s has odd values; the half-size metallic '
                             'atlas would not share the layout' % ((x, y, w, h),))
        sw, sh = src_sizes[name]
        if abs((w / h) / (sw / sh) - 1.0) > ASPECT_TOL:
            raise AtlasError(set_name, name, 'rect aspect %.4f differs from the source %dx%d '
                             '(%.4f) by more than 1 %%; refusing to stretch or rotate'
                             % (w / h, sw, sh, sw / sh))
        if w > sw * (1 + ASPECT_TOL) or h > sh * (1 + ASPECT_TOL):
            raise AtlasError(set_name, name, 'rect %dx%d would upscale the %dx%d source'
                             % (w, h, sw, sh))
        if ms != OWN and not 0.0 <= ms <= 1.0:
            raise AtlasError(set_name, name, 'smoothness %r outside 0..1' % (ms,))
    for i, a in enumerate(cells):
        ax, ay, aw, ah = a[1]
        for b in cells[i + 1:]:
            bx, by, bw, bh = b[1]
            dx = max(bx - (ax + aw), ax - (bx + bw))
            dy = max(by - (ay + ah), ay - (by + bh))
            if dx < 0 and dy < 0:
                raise AtlasError(set_name, a[0], 'overlaps %s' % b[0])
            if max(dx, dy) < GAP:
                raise AtlasError(set_name, a[0], 'only %d px from %s (need %d)'
                                 % (max(dx, dy), b[0], GAP))
    names = [c[0] for c in cells]
    if len(set(names)) != len(names):
        raise AtlasError(set_name, '-', 'duplicate cell names')


def check_sources(set_name, cells, pack):
    """Every declared map must exist, and every shipped map must be used."""
    sizes = {}
    for name, _, own_normal, ms, _ in cells:
        base = os.path.join(pack, name)
        if not os.path.isfile(base + '.png'):
            raise AtlasError(set_name, name, 'missing %s.png' % name)
        with Image.open(base + '.png') as im:
            if im.mode != 'RGBA':
                raise AtlasError(set_name, name, 'colour source is %s, expected RGBA' % im.mode)
            sizes[name] = im.size
        for suffix, declared in (('_Normal', own_normal), ('_MetallicSmoothness', ms == OWN)):
            path = base + suffix + '.png'
            if declared != os.path.isfile(path):
                raise AtlasError(set_name, name, '%s%s.png %s but the layout says %s'
                                 % (name, suffix, 'exists' if not declared else 'is missing',
                                    'own map' if declared else 'constant/flat'))
            if declared:
                with Image.open(path) as im:
                    if im.size != sizes[name]:
                        raise AtlasError(set_name, name, '%s size %s differs from the colour map %s'
                                         % (suffix, im.size, sizes[name]))
    return sizes


# --------------------------------------------------------------------------------------------
# Building

def fill_colour(set_name, name, rgba):
    """The decal's constant colour under alpha 0 (pack convention); alpha-weighted mean if none."""
    z = rgba[..., 3] == 0
    if not z.any():
        a = rgba[..., 3:4] / 255.0
        return np.round((rgba[..., :3] * a).reshape(-1, 3).sum(0) / max(a.sum(), 1e-6)).astype(np.uint8)
    u = np.unique(rgba[z][:, :3], axis=0)
    if len(u) != 1:
        raise AtlasError(set_name, name, '%d different colours under alpha 0 (expected one)' % len(u))
    return u[0]


def resample_color(rgba, w, h):
    """Premultiplied, linear-light Lanczos resample of a straight-alpha sRGB image."""
    a = rgba[..., 3] / 255.0
    lin = srgb_to_linear(rgba[..., :3] / 255.0) * a[..., None]
    ar = np.clip(resize_f(a, w, h, LANCZOS), 0.0, 1.0)
    pm = np.stack([np.clip(resize_f(lin[..., c], w, h, LANCZOS), 0.0, None) for c in range(3)], -1)
    rgb = np.where(ar[..., None] > 0, pm / np.maximum(ar[..., None], 1e-9), 0.0)
    out = np.zeros((h, w, 4), np.uint8)
    out[..., :3] = np.round(linear_to_srgb(rgb) * 255.0)
    out[..., 3] = np.round(ar * 255.0)
    return out


def decode_normal(rgb):
    return rgb.astype(np.float64) / 255.0 * 2.0 - 1.0


def encode_normal(n):
    return np.clip(np.round((n * 0.5 + 0.5) * 255.0), 0, 255).astype(np.uint8)


def resample_normal(rgb, w, h):
    """Resample tangent-space normals as vectors (Lanczos per component), then renormalise."""
    n = decode_normal(rgb)
    if (w, h) != (rgb.shape[1], rgb.shape[0]):
        n = np.stack([resize_f(n[..., c], w, h, LANCZOS) for c in range(3)], -1)
    n[..., 2] = np.maximum(n[..., 2], 1e-3)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return encode_normal(n)


def resample_ms(rgba, w, h):
    """Box-filtered R (metallic) and A (smoothness); G and B stay 0 as in the pack's maps."""
    out = np.zeros((h, w, 4), np.uint8)
    for c in (0, 3):
        out[..., c] = np.clip(np.round(resize_f(rgba[..., c].astype(np.float64), w, h, BOX)), 0, 255)
    return out


def bleed(color, rects):
    """Every gap pixel takes the colour of the nearest cell pixel (exact Euclidean distance to the
    axis-aligned rects); alpha stays 0 there."""
    yy = np.arange(PAGE)[:, None]
    xx = np.arange(PAGE)[None, :]
    best = np.full((PAGE, PAGE), np.iinfo(np.int64).max, np.int64)
    sx = np.zeros((PAGE, PAGE), np.int64)
    sy = np.zeros((PAGE, PAGE), np.int64)
    for x, y, w, h in rects:
        cx = np.clip(xx, x, x + w - 1)
        cy = np.clip(yy, y, y + h - 1)
        d = (xx - cx) ** 2 + (yy - cy) ** 2
        closer = d < best
        best = np.where(closer, d, best)
        sx = np.where(closer, cx, sx)
        sy = np.where(closer, cy, sy)
    gap = best > 0
    color[gap, :3] = color[sy[gap], sx[gap], :3]
    color[gap, 3] = 0


def build(set_name, cells, pack, out):
    color = np.zeros((PAGE, PAGE, 4), np.uint8)
    normal = np.empty((PAGE, PAGE, 3), np.uint8)
    normal[...] = FLAT
    ms = np.zeros((MS_PAGE, MS_PAGE, 4), np.uint8)
    inside = np.zeros((PAGE, PAGE), bool)
    for name, rect, own_normal, smooth, _ in cells:
        x, y, w, h = rect
        base = os.path.join(pack, name)
        src = load(base + '.png', 'RGBA')
        fill = fill_colour(set_name, name, src)
        sh, sw = src.shape[:2]
        if (sw, sh) == (w, h):
            block = src.copy()
            how = '1:1 copy'
        else:
            block = resample_color(src, w, h)
            block[block[..., 3] == 0, :3] = fill
            how = 'x%.4f Lanczos' % (w / sw)
        color[y:y + h, x:x + w] = block
        inside[y:y + h, x:x + w] = True
        if own_normal:
            normal[y:y + h, x:x + w] = resample_normal(load(base + '_Normal.png', 'RGB'), w, h)
        hx, hy, hw, hh = half(rect)
        if smooth == OWN:
            ms[hy:hy + hh, hx:hx + hw] = resample_ms(load(base + '_MetallicSmoothness.png', 'RGBA'), hw, hh)
        else:
            ms[hy:hy + hh, hx:hx + hw, 3] = q8(smooth)
        print('  %-24s %4dx%-4d -> %4dx%-4d at (%4d,%4d) %-15s normal %-4s smoothness %s'
              % (name, sw, sh, w, h, x, y, how, 'own' if own_normal else 'flat',
                 'own map' if smooth == OWN else '%.2f (%d)' % (smooth, q8(smooth))))
    bleed(color, [c[1] for c in cells])

    os.makedirs(out, exist_ok=True)
    Image.fromarray(color, 'RGBA').save(os.path.join(out, set_name + '_Color.png'), optimize=True)
    Image.fromarray(normal, 'RGB').save(os.path.join(out, set_name + '_Normal.png'), optimize=True)
    Image.fromarray(ms, 'RGBA').save(os.path.join(out, set_name + '_MetallicSmoothness.png'), optimize=True)
    doc = {'width': PAGE, 'height': PAGE,
           'cells': [{'name': n, 'x': r[0], 'y': PAGE - r[1] - r[3], 'w': r[2], 'h': r[3]}
                     for n, r, _, _, _ in cells]}
    with open(os.path.join(out, set_name + '.json'), 'w', encoding='utf-8', newline='\n') as f:
        json.dump(doc, f, indent=1)
        f.write('\n')


# --------------------------------------------------------------------------------------------
# Verification (reads the written files back)

def read_json_layout(set_name, cells, path):
    with open(path, encoding='utf-8') as f:
        doc = json.load(f)
    if set(doc) != {'width', 'height', 'cells'} or doc['width'] != PAGE or doc['height'] != PAGE:
        raise AtlasError(set_name, '-', 'JSON header %s is not {width: %d, height: %d, cells}'
                         % (sorted(doc), PAGE, PAGE))
    if [c.get('name') for c in doc['cells']] != [c[0] for c in cells]:
        raise AtlasError(set_name, '-', 'JSON cell names/order differ from the layout')
    rebuilt = []
    for c, layout in zip(doc['cells'], cells):
        name = layout[0]
        if set(c) != {'name', 'x', 'y', 'w', 'h'} or not all(type(c[k]) is int for k in 'xywh'):
            raise AtlasError(set_name, name, 'JSON cell is not {name, x, y, w, h} with integers')
        top = (c['x'], PAGE - c['y'] - c['h'], c['w'], c['h'])
        if top != layout[1]:
            raise AtlasError(set_name, name, 'JSON rect %s (bottom-up) round-trips to %s, layout %s'
                             % ((c['x'], c['y'], c['w'], c['h']), top, layout[1]))
        rebuilt.append((name, top) + tuple(layout[2:]))
    return rebuilt


def verify(set_name, cells, pack, out):
    sizes = check_sources(set_name, cells, pack)
    cells = read_json_layout(set_name, cells, os.path.join(out, set_name + '.json'))
    check_layout(set_name, cells, sizes)
    try:
        color = load(os.path.join(out, set_name + '_Color.png'), 'RGBA')
        normal = load(os.path.join(out, set_name + '_Normal.png'), 'RGB')
        ms = load(os.path.join(out, set_name + '_MetallicSmoothness.png'), 'RGBA')
    except (OSError, ValueError) as e:
        raise AtlasError(set_name, '-', str(e))
    for label, img, size in (('colour', color, PAGE), ('normal', normal, PAGE), ('metallic', ms, MS_PAGE)):
        if img.shape[:2] != (size, size):
            raise AtlasError(set_name, '-', '%s atlas is %s, expected %d x %d' % (label, img.shape[:2], size, size))

    inside = np.zeros((PAGE, PAGE), bool)
    inside_ms = np.zeros((MS_PAGE, MS_PAGE), bool)
    for _, (x, y, w, h), _, _, _ in cells:
        inside[y:y + h, x:x + w] = True
        hx, hy, hw, hh = half((x, y, w, h))
        inside_ms[hy:hy + hh, hx:hx + hw] = True
    bad = (color[..., 3] > 0) & ~inside
    if bad.any():
        raise AtlasError(set_name, nearest_cell(cells, bad), 'colour alpha is not 0 in the gap')
    bad = (normal != FLAT).any(-1) & ~inside
    if bad.any():
        raise AtlasError(set_name, nearest_cell(cells, bad), 'normal is not flat (128, 128, 255) in the gap')
    bad = ms.any(-1) & ~inside_ms
    if bad.any():
        raise AtlasError(set_name, nearest_cell(cells, bad, 2), 'metallic-smoothness is not 0 in the gap')
    n = decode_normal(normal)
    ln = np.linalg.norm(n, axis=-1)
    bad = (np.abs(ln - 1.0) > UNIT_TOL) | (n[..., 2] <= 0)
    if bad.any():
        by, bx = np.argwhere(bad)[0]
        raise AtlasError(set_name, nearest_cell(cells, bad), 'normal at (%d, %d) decodes to length %.4f'
                         % (bx, by, ln[by, bx]))

    report = []
    for name, (x, y, w, h), own_normal, smooth, size_m in cells:
        base = os.path.join(pack, name)
        src = load(base + '.png', 'RGBA')
        cell = color[y:y + h, x:x + w]
        fill = fill_colour(set_name, name, src)
        z = cell[..., 3] == 0
        if z.any() and (cell[z][:, :3] != fill).any():
            raise AtlasError(set_name, name, 'colour under alpha 0 is not the fill colour %s' % (tuple(fill),))
        # Bleed: the 1 px ring just outside the cell repeats the cell's edge colour.
        for ring, edge in ((color[y - 1, x:x + w, :3], cell[0, :, :3]), (color[y + h, x:x + w, :3], cell[-1, :, :3]),
                           (color[y:y + h, x - 1, :3], cell[:, 0, :3]), (color[y:y + h, x + w, :3], cell[:, -1, :3])):
            if (ring != edge).any():
                raise AtlasError(set_name, name, 'gap next to the cell does not carry the bled edge colour')
        sh, sw = src.shape[:2]
        ref_a = resize_f(src[..., 3].astype(np.float64), w, h, BILINEAR)
        ca = cell[..., 3].astype(np.float64)
        if (sw, sh) == (w, h):
            if (cell != src).any():
                raise AtlasError(set_name, name, '1:1 cell is not an exact copy of the source')
            c_alpha = 1.0
        else:
            c_alpha = corr(ca, ref_a)
            if c_alpha < 0.97:
                raise AtlasError(set_name, name, 'alpha correlates only %.3f with the source (moved or rotated?)' % c_alpha)
        note = ''
        if name.startswith(SHOE_PRINTS):
            c_flip = corr(ca, ref_a[::-1])
            c_rot = corr(ca, ref_a[::-1, ::-1])
            occ = ca > 0.15 * 255
            rows = np.nonzero(occ.any(1))[0]
            widths = np.array([np.ptp(np.nonzero(r)[0]) if r.any() else 0 for r in occ])
            widest = int(widths.argmax())
            mid = (rows.min() + rows.max()) / 2.0
            top_mass = ca[:h // 2].sum() / max(ca[h // 2:].sum(), 1e-9)
            if not (c_alpha > c_flip + 0.2 and c_alpha > c_rot + 0.2 and widest < mid and top_mass > 1.0):
                raise AtlasError(set_name, name, 'toe is not at +V (corr %.3f, flipped %.3f, rot180 %.3f, '
                                 'widest row %d vs middle %.0f, top/bottom mass %.2f)'
                                 % (c_alpha, c_flip, c_rot, widest, mid, top_mass))
            note = '  toe +V: widest row %d of %d-%d, top/bottom mass %.2f, corr flipped %.3f, rot180 %.3f' % (
                widest, rows.min(), rows.max(), top_mass, c_flip, c_rot)
        ncell = normal[y:y + h, x:x + w]
        if own_normal:
            sn = decode_normal(load(base + '_Normal.png', 'RGB'))
            cn = decode_normal(ncell)
            c_n = min(corr(cn[..., k], resize_f(sn[..., k], w, h, BILINEAR)) for k in (0, 1))
            if c_n < 0.9:
                raise AtlasError(set_name, name, 'normal X/Y correlate only %.3f with the source' % c_n)
        else:
            if (ncell != FLAT).any():
                raise AtlasError(set_name, name, 'cell without a shipped _Normal is not flat')
            c_n = None
        hx, hy, hw, hh = half((x, y, w, h))
        mcell = ms[hy:hy + hh, hx:hx + hw]
        if smooth == OWN:
            sm = load(base + '_MetallicSmoothness.png', 'RGBA').astype(np.float64)
            for k, label in ((0, 'metallic'), (3, 'smoothness')):
                ref = resize_f(sm[..., k], hw, hh, BILINEAR)
                got = mcell[..., k].astype(np.float64)
                if abs(got.mean() - sm[..., k].mean()) > 2.0:
                    raise AtlasError(set_name, name, '%s mean %.1f differs from the source %.1f'
                                     % (label, got.mean(), sm[..., k].mean()))
                if ref.std() > 0 and corr(got, ref) < 0.95:
                    raise AtlasError(set_name, name, '%s correlates only %.3f with the source' % (label, corr(got, ref)))
            if mcell[..., 1:3].any():
                raise AtlasError(set_name, name, 'metallic G/B are not 0')
            ms_text = 'own (A %d-%d, mean %.3f)' % (mcell[..., 3].min(), mcell[..., 3].max(), mcell[..., 3].mean() / 255)
        else:
            want = q8(smooth)
            if (mcell[..., 3] != want).any() or mcell[..., :3].any():
                raise AtlasError(set_name, name, 'metallic-smoothness is not R=0, A=%d (%.2f) throughout' % (want, smooth))
            ms_text = '%.2f (A = %d)' % (smooth, want)
        report.append((name, (x, y, w, h), (sw, sh), size_m, c_alpha, c_n, ms_text + note))
    return report


def gpu_bytes(w, h, block_w, block_h, bytes_per_block=16):
    """Size of a block-compressed texture with its full mip chain."""
    total = 0
    while True:
        total += -(-w // block_w) * -(-h // block_h) * bytes_per_block
        if w == 1 and h == 1:
            return total
        w, h = max(1, w // 2), max(1, h // 2)


def main(argv):
    if len(argv) not in (3, 4) or (len(argv) == 4 and argv[3] != '--verify'):
        print(__doc__)
        return 2
    pack, out = argv[1], argv[2]
    try:
        for set_name, cells in SETS.items():
            sizes = check_sources(set_name, cells, pack)
            check_layout(set_name, cells, sizes)
            if len(argv) == 3:
                print('%s: building' % set_name)
                build(set_name, cells, pack, out)
            report = verify(set_name, cells, pack, out)
            area = sum(r[1][2] * r[1][3] for r in report)
            print('%s: verified %d cells, %.1f %% of the page filled' % (set_name, len(report), 100.0 * area / PAGE ** 2))
            # Texel density on the real-size quad, U x V (DirtEdge_WallBase is anisotropic in its source too).
            for name, rect, src, size_m, c_a, c_n, ms_text in report:
                print('  %-24s rect %-22s src %4dx%-4d %4.0f x %-4.0f px/m  alpha corr %.3f  normal corr %s  smoothness %s'
                      % (name, rect, src[0], src[1], rect[2] / size_m[0], rect[3] / size_m[1], c_a,
                         '-' if c_n is None else '%.3f' % c_n, ms_text))
            for suffix in ('_Color.png', '_Normal.png', '_MetallicSmoothness.png', '.json'):
                path = os.path.join(out, set_name + suffix)
                print('  %-34s %9d bytes' % (os.path.basename(path), os.path.getsize(path)))
            pc = 2 * gpu_bytes(PAGE, PAGE, 4, 4) + gpu_bytes(MS_PAGE, MS_PAGE, 4, 4)
            quest = gpu_bytes(PAGE, PAGE, 6, 6) + gpu_bytes(PAGE, PAGE, 5, 5) + gpu_bytes(MS_PAGE, MS_PAGE, 6, 6)
            print('  GPU memory with mips: DXT5 colour + BC5 normal + DXT5 MS = %d bytes (%.2f MB); '
                  'ASTC 6x6 / 5x5 / 6x6 = %d bytes (%.2f MB)' % (pc, pc / 1e6, quest, quest / 1e6))
    except AtlasError as e:
        print('FAIL ' + str(e), file=sys.stderr)
        return 1
    print('OK')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
