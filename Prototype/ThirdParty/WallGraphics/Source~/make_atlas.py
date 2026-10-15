"""Night-store texture atlases built from existing WallGraphics images (derived works; no new licence terms).

    python3 -I make_atlas.py PACKDIR [--crops DIR]

From the repository root, run a copy that lives outside the pack (as the pack's other tools):
    cp Prototype/ThirdParty/WallGraphics/Source~/make_atlas.py /tmp/make_atlas.py
    python3 -I /tmp/make_atlas.py Prototype/ThirdParty/WallGraphics

Writes PACKDIR/Atlas/NightStoreInterior_Color.png (2048 x 2048 RGBA, sRGB chunk), NightStoreStreet_Color.png
(2048 x 1024 RGBA) and their layouts NightStoreInterior.json / NightStoreStreet.json for Unity's JsonUtility:
{width, height, cells: [{name, x, y, w, h}]} in pixels with y counted from the BOTTOM edge (Unity's UV origin), so a
cell's UV rect is (x / width, y / height, w / width, h / height). Then replaces the Atlas/ entries of
PACKDIR/sources.json (bytes, sha256, derived_from, tool); every other entry is kept byte for byte.
--crops DIR also writes every cell at 100 % (DIR/<page>__<cell>.png) for a legibility review.

The layouts are fixed (rects below, y from the TOP edge, as the architect's script verified them). Each source is
resampled to its rect with Lanczos in sRGB-encoded values (as the pack's own generators resize), RGBA sources with
premultiplied alpha so no colour leaks from transparent texels. Opaque sources (JPG) get alpha 255. The RGB under
alpha 0 inside an alpha cell is filled from the visible texels (push-pull, as Unity's Alpha Is Transparency does);
no visible texel changes. Each cell's edge colour is then bled 8 px into the gap at alpha 0 (clamp extension, corners
take the corner texel), and the rest of the unused page gets the push-pull colour of the nearest cells, also at
alpha 0, so neither the first mip levels (bleed) nor the deep ones (fill) pull in black.

VERIFY, before anything is written (exit 1 naming the page and cell): pages are powers of two; every listed source
exists, has the alpha the layout expects and the sha256 sources.json records; no rects overlap; every gap between
cells is at least 16 px (Chebyshev, so the two 8 px bleeds never meet); every rect is at least 8 px inside its page;
every rect's aspect is within 1 % of its source's; names are unique. After the build: opaque cells are alpha 255,
alpha cells keep the source silhouette (texels under 10 % or over 90 % covered in the area-averaged source stay
below or above alpha 128; covered area within 0.2 %; fully transparent holes and tabs still present), every texel outside the cells is alpha 0, every bleed ring equals its edge. After
writing (to .tmp files first): the PNG re-reads pixel-identical and the JSON round-trips (exact keys, integer
values, y back to the top-down rects, size equal to the PNG's). A cell is never rotated, cropped or rescaled away
from its listed rect; a layout that does not fit fails instead.
"""
import hashlib, json, os, sys
import numpy as np
import PIL
from PIL import Image, PngImagePlugin

Image.MAX_IMAGE_PIXELS = None
BLEED = 8           # px of edge colour around each cell (alpha 0)
GAP = 16            # minimum px between two cells (two bleeds)
MARGIN = 8          # minimum px between a cell and the page edge (one bleed)
ASPECT_TOL = 0.01   # cell aspect vs source aspect

# (name = source file stem, source path in the pack, 'opaque' | 'alpha', (x, y_top, w, h), physical size in m (w, h)
# from the README tables; the size is documentation only and is printed, not written to the JSON).
PAGES = [
    dict(name='NightStoreInterior', png='NightStoreInterior_Color.png', json='NightStoreInterior.json', size=(2048, 2048),
         cells=[
             ('FireEvacuationPlan', 'Store/FireEvacuationPlan.jpg', 'opaque', (8, 8, 1024, 724), (0.42, 0.297)),
             ('Art_Heda_BreakfastWithLobster', 'Artwork/Art_Heda_BreakfastWithLobster.jpg', 'opaque', (1048, 8, 764, 768), (0.80, 0.80)),
             ('Lightbox_AurelFinchWide', 'Brand/Lightbox_AurelFinchWide.jpg', 'opaque', (8, 748, 1024, 512), (2.00, 1.00)),
             ('NoticeBoard_StaffCork', 'Store/NoticeBoard_StaffCork.jpg', 'opaque', (1048, 792, 768, 512), (0.90, 0.60)),
             ('Art_Pissarro_BoulevardMontmartre', 'Artwork/Art_Pissarro_BoulevardMontmartre.jpg', 'opaque', (8, 1276, 768, 604), (0.89, 0.70)),
             ('Certificate_GemmologyDiplomaFramed', 'Store/Certificate_GemmologyDiplomaFramed.jpg', 'opaque', (792, 1320, 512, 384), (0.48, 0.36)),
             ('Calendar_October2026', 'Store/Calendar_October2026.png', 'alpha', (1320, 1320, 384, 543), (0.30, 0.42)),
             ('Sign_CCTV', 'Store/Sign_CCTV.png', 'alpha', (1720, 1320, 288, 384), (0.30, 0.40)),
             ('Sticker_AlarmMonitored', 'Store/Sticker_AlarmMonitored.png', 'alpha', (792, 1720, 256, 256), (0.10, 0.10)),
             ('Sticker_CardsAccepted', 'Store/Sticker_CardsAccepted.png', 'alpha', (1720, 1720, 288, 192), (0.12, 0.08)),
             ('Sticker_InsurerApproved', 'Store/Sticker_InsurerApproved.png', 'alpha', (8, 1896, 256, 128), (0.12, 0.06)),
         ]),
    dict(name='NightStoreStreet', png='NightStoreStreet_Color.png', json='NightStoreStreet.json', size=(2048, 1024),
         cells=[
             ('StreetPosters_Weathered', 'Street/StreetPosters_Weathered.jpg', 'opaque', (8, 8, 1008, 504), (3.00, 1.50)),
             ('Graffiti_Tags', 'Street/Graffiti_Tags.png', 'alpha', (1032, 8, 1008, 504), (3.00, 1.50)),
             ('Flyer_LostCat', 'Street/Flyer_LostCat.png', 'alpha', (8, 528, 366, 488), (0.23, 0.30)),
         ]),
]


class Fail(Exception):
    pass


def sha256(path):
    with open(path, 'rb') as f:
        return hashlib.sha256(f.read()).hexdigest()


def pot(n):
    return n > 0 and (n & (n - 1)) == 0


def check_layout(pack, page, recorded):
    """Layout and source checks; returns {cell name: (opened source image, sha256)}."""
    pw, ph = page['size']
    tag = page['name']
    if not (pot(pw) and pot(ph)):
        raise Fail(f'{tag}: page {pw} x {ph} is not a power of two')
    names = [c[0] for c in page['cells']]
    for n in names:
        if names.count(n) > 1:
            raise Fail(f'{tag}/{n}: name listed twice')
    srcs = {}
    for name, rel, mode, (x, y, w, h), _ in page['cells']:
        where = f'{tag}/{name}'
        path = os.path.join(pack, rel)
        if os.path.splitext(os.path.basename(rel))[0] != name:
            raise Fail(f'{where}: name is not the stem of {rel}')
        if not os.path.isfile(path):
            raise Fail(f'{where}: source {rel} is missing')
        digest = sha256(path)
        if rel in recorded and recorded[rel] != digest:
            raise Fail(f'{where}: {rel} sha256 {digest[:16]}... differs from sources.json {recorded[rel][:16]}...')
        if rel not in recorded:
            raise Fail(f'{where}: {rel} has no sources.json entry')
        im = Image.open(path)
        im.load()
        has_alpha = 'A' in im.getbands()
        if mode == 'alpha' and not has_alpha:
            raise Fail(f'{where}: layout expects alpha but {rel} is {im.mode}')
        if mode == 'opaque' and has_alpha and np.asarray(im.convert('RGBA').getchannel('A')).min() < 255:
            raise Fail(f'{where}: layout expects an opaque source but {rel} has transparent texels')
        if min(x, y, w, h) < 0 or w == 0 or h == 0 or any(int(v) != v for v in (x, y, w, h)):
            raise Fail(f'{where}: rect {(x, y, w, h)} is not a positive integer rect')
        if x < MARGIN or y < MARGIN or x + w > pw - MARGIN or y + h > ph - MARGIN:
            raise Fail(f'{where}: rect {(x, y, w, h)} is not {MARGIN} px inside the {pw} x {ph} page')
        sw, sh = im.size
        err = (w / h) / (sw / sh) - 1
        if abs(err) > ASPECT_TOL:
            raise Fail(f'{where}: aspect {w}x{h} vs source {sw}x{sh} is off by {err * 100:+.2f} % (limit 1 %)')
        srcs[name] = (im, digest)
    cells = page['cells']
    for i in range(len(cells)):
        for j in range(i + 1, len(cells)):
            ax, ay, aw, ah = cells[i][3]
            bx, by, bw, bh = cells[j][3]
            dx = max(bx - (ax + aw), ax - (bx + bw))     # < 0 when the x ranges overlap
            dy = max(by - (ay + ah), ay - (by + bh))
            if dx < 0 and dy < 0:
                raise Fail(f'{tag}/{cells[i][0]}: overlaps {cells[j][0]}')
            if max(dx, dy) < GAP:
                raise Fail(f'{tag}/{cells[i][0]}: only {max(dx, dy)} px from {cells[j][0]} (need {GAP})')
    return srcs


def lanczos(a, w, h):
    """Per-channel float Lanczos resample of an (H, W, C) float32 array to (h, w, C)."""
    return np.stack([np.asarray(Image.fromarray(np.ascontiguousarray(a[..., c]), 'F').resize((w, h), Image.LANCZOS))
                     for c in range(a.shape[2])], -1)


def push_pull(rgb, wgt):
    """Fill texels with wgt == 0 from the weighted mean of known texels at ever coarser 2x2 levels; known texels keep
    their colour exactly."""
    cs, ws = [rgb * wgt[..., None]], [wgt]
    while cs[-1].shape[0] > 1 or cs[-1].shape[1] > 1:
        c, w = cs[-1], ws[-1]
        h2, w2 = (c.shape[0] + 1) // 2, (c.shape[1] + 1) // 2
        c = np.pad(c, ((0, h2 * 2 - c.shape[0]), (0, w2 * 2 - c.shape[1]), (0, 0)))
        w = np.pad(w, ((0, h2 * 2 - w.shape[0]), (0, w2 * 2 - w.shape[1])))
        cs.append(c.reshape(h2, 2, w2, 2, -1).sum((1, 3)))
        ws.append(w.reshape(h2, 2, w2, 2).sum((1, 3)))
    col = cs[-1] / max(float(ws[-1].max()), 1e-12)
    for c, w in zip(reversed(cs[:-1]), reversed(ws[:-1])):
        up = np.repeat(np.repeat(col, 2, 0), 2, 1)[:c.shape[0], :c.shape[1]]
        col = np.where(w[..., None] > 0, c / np.maximum(w, 1e-12)[..., None], up)
    return np.where(wgt[..., None] > 0, rgb, col).astype(np.float32)


def resample_cell(im, mode, w, h):
    """Source -> (rgb float32 holding integer values 0..255, alpha uint8) at w x h."""
    if mode == 'opaque':
        rgb = np.round(np.clip(lanczos(np.asarray(im.convert('RGB'), np.float32), w, h), 0, 255))
        return rgb.astype(np.float32), np.full((h, w), 255, np.uint8)
    s = np.asarray(im.convert('RGBA'), np.float32)
    a = s[..., 3:] / 255.0
    r = lanczos(np.concatenate([s[..., :3] * a, a], -1), w, h)
    alpha = np.clip(r[..., 3], 0, 1)
    a8 = np.round(alpha * 255).astype(np.uint8)
    rgb = np.clip(r[..., :3] / np.maximum(alpha, 1e-6)[..., None], 0, 255)
    rgb = push_pull(rgb, np.where(a8 > 0, alpha, 0).astype(np.float32))   # colour under alpha 0 only
    return np.round(np.clip(rgb, 0, 255)).astype(np.float32), a8


def build_page(page, srcs):
    pw, ph = page['size']
    rgb = np.zeros((ph, pw, 3), np.float32)
    alpha = np.zeros((ph, pw), np.uint8)
    owner = np.full((ph, pw), -1, np.int16)
    built = {}
    for i, (name, rel, mode, (x, y, w, h), _) in enumerate(page['cells']):
        crgb, ca = resample_cell(srcs[name][0], mode, w, h)
        built[name] = (crgb, ca)
        ys = slice(y - BLEED, y + h + BLEED)
        xs = slice(x - BLEED, x + w + BLEED)
        if (owner[ys, xs] >= 0).any():
            other = page['cells'][int(owner[ys, xs].max())][0]
            raise Fail(f"{page['name']}/{name}: bleed meets {other}")
        owner[ys, xs] = i
        iy = np.clip(np.arange(-BLEED, h + BLEED), 0, h - 1)
        ix = np.clip(np.arange(-BLEED, w + BLEED), 0, w - 1)
        rgb[ys, xs] = crgb[iy][:, ix]
        alpha[y:y + h, x:x + w] = ca
    rgb = push_pull(rgb, (owner >= 0).astype(np.float32))
    out = np.dstack([np.round(np.clip(rgb, 0, 255)).astype(np.uint8), alpha])
    return out, built


def area_alpha(im, w, h):
    """Source alpha area-averaged (box filter) to w x h, 0..1."""
    a = np.asarray(im.convert('RGBA').getchannel('A'), np.float32)
    return np.asarray(Image.fromarray(a, 'F').resize((w, h), Image.BOX)) / 255.0


def verify_page(page, srcs, out, built):
    pw, ph = page['size']
    tag = page['name']
    if out.shape != (ph, pw, 4) or out.dtype != np.uint8:
        raise Fail(f'{tag}: built page is {out.shape} {out.dtype}')
    inside = np.zeros((ph, pw), bool)
    for name, rel, mode, (x, y, w, h), _ in page['cells']:
        where = f'{tag}/{name}'
        inside[y:y + h, x:x + w] = True
        ca = out[y:y + h, x:x + w, 3]
        if mode == 'opaque':
            if ca.min() != 255:
                raise Fail(f'{where}: opaque cell has alpha below 255')
        else:
            ref = area_alpha(srcs[name][0], w, h)
            lost = int(((ref > 0.9) & (ca < 128)).sum())      # Lanczos sharpens 1 px strokes, so only texels
            gained = int(((ref < 0.1) & (ca >= 128)).sum())   # under 10 % / over 90 % covered must keep their side
            cover_src, cover_out = float((ref >= 0.5).mean()), float((ca >= 128).mean())
            if lost or gained or abs(cover_src - cover_out) > 0.002:
                raise Fail(f'{where}: silhouette changed ({lost} opaque texels lost, {gained} gained, '
                           f'cover {cover_src:.4f} -> {cover_out:.4f})')
            if (np.asarray(srcs[name][0].convert('RGBA').getchannel('A')) == 0).any() and not (ca == 0).any():
                raise Fail(f'{where}: the source has fully transparent texels (holes, tabs) but the cell has none')
        crgb, _ = built[name]
        iy = np.clip(np.arange(-BLEED, h + BLEED), 0, h - 1)
        ix = np.clip(np.arange(-BLEED, w + BLEED), 0, w - 1)
        ring = out[y - BLEED:y + h + BLEED, x - BLEED:x + w + BLEED]
        if not np.array_equal(ring[..., :3], crgb[iy][:, ix].astype(np.uint8)):
            raise Fail(f'{where}: bleed ring differs from the clamped edge colour')
    if out[..., 3][~inside].max() != 0:
        raise Fail(f'{tag}: texels outside the cells have alpha above 0')


def format_layout(width, height, cells):
    """JSON text, one cell per line; cells are (name, x, y_bottom, w, h)."""
    lines = ['{', f'  "width": {width},', f'  "height": {height},', '  "cells": [']
    for i, (name, x, yb, w, h) in enumerate(cells):
        lines.append(f'    {{"name": {json.dumps(name)}, "x": {x}, "y": {yb}, "w": {w}, "h": {h}}}' + (',' if i < len(cells) - 1 else ''))
    lines += ['  ]', '}', '']
    return '\n'.join(lines)


def layout_json(page):
    pw, ph = page['size']
    return format_layout(pw, ph, [(name, x, ph - y - h, w, h) for name, _, _, (x, y, w, h), _ in page['cells']])


def verify_json(page, text, png_size):
    tag = page['name']
    d = json.loads(text)
    if set(d) != {'width', 'height', 'cells'}:
        raise Fail(f'{tag}: JSON keys {sorted(d)}')
    if (d['width'], d['height']) != tuple(page['size']) or (d['width'], d['height']) != png_size:
        raise Fail(f"{tag}: JSON size {d['width']} x {d['height']} vs page {page['size']} / PNG {png_size}")
    if len(d['cells']) != len(page['cells']):
        raise Fail(f'{tag}: JSON lists {len(d["cells"])} cells, layout {len(page["cells"])}')
    for c, (name, _, _, (x, y, w, h), _) in zip(d['cells'], page['cells']):
        if set(c) != {'name', 'x', 'y', 'w', 'h'} or any(type(c[k]) is not int for k in 'xywh'):
            raise Fail(f'{tag}/{name}: JSON cell has keys {sorted(c)} or non-integer values')
        if c['name'] != name or (c['x'], d['height'] - c['y'] - c['h'], c['w'], c['h']) != (x, y, w, h):
            raise Fail(f'{tag}/{name}: JSON cell {c} does not round-trip to {(x, y, w, h)}')
    rebuilt = format_layout(d['width'], d['height'], [(c['name'], c['x'], c['y'], c['w'], c['h']) for c in d['cells']])
    if rebuilt != text:
        raise Fail(f'{tag}: JSON text does not round-trip (parse and re-format differ)')


def write_page(atlas_dir, page, out, js):
    """Write to .tmp, re-read and compare, then move into place."""
    png = os.path.join(atlas_dir, page['png'])
    jsn = os.path.join(atlas_dir, page['json'])
    info = PngImagePlugin.PngInfo()
    info.add(b'sRGB', b'\x00')                     # rendering intent: perceptual
    Image.fromarray(out, 'RGBA').save(png + '.tmp', 'PNG', pnginfo=info, compress_level=9)
    with open(jsn + '.tmp', 'w', newline='\n') as f:
        f.write(js)
    try:
        back = Image.open(png + '.tmp')
        back.load()
        if back.mode != 'RGBA' or not np.array_equal(np.asarray(back), out):
            raise Fail(f"{page['name']}: written PNG does not re-read pixel-identical")
        with open(jsn + '.tmp') as f:
            verify_json(page, f.read(), back.size)
    except Exception:
        for p in (png + '.tmp', jsn + '.tmp'):
            if os.path.exists(p):
                os.remove(p)
        raise
    os.replace(png + '.tmp', png)
    os.replace(jsn + '.tmp', jsn)
    return png, jsn


def update_sources(pack, entries):
    path = os.path.join(pack, 'sources.json')
    with open(path, encoding='utf-8') as f:
        text = f.read()
    d = json.loads(text)
    if json.dumps(d, indent=1) != text:
        raise Fail('sources.json: not in the json.dumps(indent=1) layout this script rewrites; not touched')
    kept = [it for it in d['items'] if not it['file'].startswith('Atlas/')]
    new = list(entries)
    items = []
    for it in kept:                                          # keep order; atlas entries go where they sort
        while new and new[0]['file'] < it['file']:
            items.append(new.pop(0))
        items.append(it)
    items += new
    d['items'] = items
    text2 = json.dumps(d, indent=1)
    back = json.loads(text2)
    if [it for it in back['items'] if not it['file'].startswith('Atlas/')] != kept:
        raise Fail('sources.json: rewriting would change entries outside Atlas/')
    with open(path + '.tmp', 'w', encoding='utf-8', newline='\n') as f:
        f.write(text2)
    os.replace(path + '.tmp', path)


def main(argv):
    if len(argv) < 2 or argv[1].startswith('-'):
        print(__doc__)
        return 2
    pack = argv[1]
    crops = argv[argv.index('--crops') + 1] if '--crops' in argv else None
    with open(os.path.join(pack, 'sources.json'), encoding='utf-8') as f:
        recorded = {it['file']: it['sha256'] for it in json.load(f)['items']}
    tool = (f'Source~/make_atlas.py (python3 {sys.version.split()[0]}, numpy {np.__version__}, Pillow {PIL.__version__}): '
            'Lanczos per cell in sRGB-encoded values, premultiplied alpha for RGBA sources, RGB under alpha 0 '
            'filled by push-pull, 8 px clamped edge bleed at alpha 0, push-pull fill of unused texels at alpha 0, '
            'PNG compress level 9 with an sRGB chunk')
    try:
        checked = [(page, check_layout(pack, page, recorded)) for page in PAGES]   # all layouts before any build
        results = []
        for page, srcs in checked:
            out, built = build_page(page, srcs)
            verify_page(page, srcs, out, built)
            results.append((page, srcs, out))
        atlas_dir = os.path.join(pack, 'Atlas')
        os.makedirs(atlas_dir, exist_ok=True)
        entries = []
        for page, srcs, out in results:
            pw, ph = page['size']
            png, jsn = write_page(atlas_dir, page, out, layout_json(page))
            used = sum(w * h for _, _, _, (_, _, w, h), _ in page['cells'])
            print(f"{page['png']}: {pw} x {ph} RGBA, {os.path.getsize(png)} bytes, {used / (pw * ph) * 100:.1f} % filled; "
                  f"{page['json']}: {os.path.getsize(jsn)} bytes")
            for name, rel, mode, (x, y, w, h), (mw, mh) in page['cells']:
                sw, sh = srcs[name][0].size
                print(f'  {name:36s} {mode:6s} top ({x}, {y}) bottom-y {ph - y - h:4d}  {w} x {h} from {sw} x {sh} '
                      f'(x{w / sw:.3f})  {mw:.3f} x {mh:.3f} m = {w / mw:.0f} x {h / mh:.0f} px/m')
            derived = [dict(file=rel, sha256=srcs[name][1], rect_top_left=[x, y, w, h])
                       for name, rel, _, (x, y, w, h), _ in page['cells']]
            entries.append(dict(
                file='Atlas/' + page['png'], bytes=os.path.getsize(png), sha256=sha256(png),
                kind='derived texture atlas (built from images in this pack)',
                width=pw, height=ph, layout='Atlas/' + page['json'],
                author='Project (Source~/make_atlas.py)', source='Prototype/ThirdParty/WallGraphics/Source~/make_atlas.py',
                commit='n/a (built October 15, 2026)', tool=tool, derived_from=derived,
                license='Inherits the licences of its cells (see derived_from and LICENSE.txt section 4); no new terms'))
            entries.append(dict(
                file='Atlas/' + page['json'], bytes=os.path.getsize(jsn), sha256=sha256(jsn),
                kind='atlas layout for Unity JsonUtility: {width, height, cells: [{name, x, y, w, h}]}, px, y from the bottom edge',
                describes='Atlas/' + page['png'], author='Project (Source~/make_atlas.py)',
                source='Prototype/ThirdParty/WallGraphics/Source~/make_atlas.py', commit='n/a (built October 15, 2026)',
                tool=tool, license='Project-owned data; no third-party content'))
            if crops:
                os.makedirs(crops, exist_ok=True)
                for name, _, _, (x, y, w, h), _ in page['cells']:
                    Image.fromarray(out[y:y + h, x:x + w], 'RGBA').save(os.path.join(crops, f"{page['name']}__{name}.png"))
        update_sources(pack, sorted(entries, key=lambda e: e['file']))
    except Fail as e:
        print('FAIL ' + str(e), file=sys.stderr)
        return 1
    print('OK: layouts, sources, silhouettes, bleed, PNG re-read, JSON round-trip and sources.json verified')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
