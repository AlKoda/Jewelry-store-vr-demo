"""Retexture three Poly Haven colour maps of the October 24, 2026 batch (in place on the staged copies, before build_ph2.py).
relabel_ph2.py SRC_ROOT

  LiftingTapeRoll     medical_tape: the tape faces (by radius about the roll axis) become a clear, slightly translucent
                      off-white lifting tape; the cardboard core faces are kept and toned a light kraft.
  ReagentSprayBottle  all_purpose_cleaner: the pink body becomes neutral white polyethylene, the red trigger is kept;
                      the three printed labels (front "leaf", back panel, shoulder ellipse) are painted out and two plain
                      white labels reading "LATENT BLOOD REAGENT" are drawn in DejaVu Sans. No pictograms, no brand.
  DentalStoneBag      cement_bag: the whole map is rebuilt as off-white/grey paper (shaded by the AO channel of the ARM map)
                      with a plain two-colour print: "DENTAL STONE / forensic casting grade / 11 kg". No brand, no logo.

Every printed string is fictional/generic. Fonts: DejaVu Sans (glyph images only, no font file shipped).
"""
import sys, os
sys.path.append(os.path.expanduser('~/.local/lib/python3.13/site-packages'))
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import trimesh

R = sys.argv[1]
FONT = '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
FONT_B = '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'


def font(size, bold=False):
    return ImageFont.truetype(FONT_B if bold else FONT, size)


def grain(shape, amp, seed, blur=0.8):
    rng = np.random.default_rng(seed)
    n = rng.normal(0, 1, shape[:2]).astype(np.float32)
    n = np.asarray(Image.fromarray(((n * 40) + 128).clip(0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(blur)), np.float32) - 128
    return n[..., None] / 40 * amp


def uv_mask(gltf, size, select):
    """Rasterise the UV triangles of the faces for which select(mesh, face_index_array) is True."""
    sc = trimesh.load(gltf, force='scene')
    mask = Image.new('L', (size, size), 0); d = ImageDraw.Draw(mask)
    for node in sc.graph.nodes_geometry:
        T, g = sc.graph[node]; m = sc.geometry[g].copy(); m.apply_transform(T)
        uv = m.visual.uv
        keep = select(m)
        for f in m.faces[keep]:
            d.polygon([(uv[i][0] * size, (1 - uv[i][1]) * size) for i in f], fill=255)
    return np.asarray(mask.filter(ImageFilter.MaxFilter(3))) > 0


def centred(draw, box, text, size, bold, fill, margin=0.94):
    """Draw text centred in box (x0, y0, x1, y1); the font shrinks from `size` until the text fits the box width."""
    x0, y0, x1, y1 = box
    size = int(size)
    while size > 6:
        f = font(size, bold)
        bb = draw.textbbox((0, 0), text, font=f)
        w, h = bb[2] - bb[0], bb[3] - bb[1]
        if w <= margin * (x1 - x0) and h <= (y1 - y0):
            break
        size -= 1
    draw.text(((x0 + x1 - w) / 2 - bb[0], (y0 + y1 - h) / 2 - bb[1]), text, font=f, fill=fill)


# ---------------------------------------------------------------- 1. lifting tape roll
def tape(folder):
    gltf = os.path.join(folder, 'LiftingTapeRoll.gltf')
    path = os.path.join(folder, 'textures', 'medical_tape_diff_2k.jpg')
    im = np.asarray(Image.open(path).convert('RGB'), np.float32)
    n = im.shape[0]
    sc = trimesh.load(gltf, force='scene')
    m = list(sc.geometry.values())[0]
    r = np.sqrt(m.vertices[:, 0] ** 2 + m.vertices[:, 2] ** 2)
    rs = np.sort(np.unique(np.round(r, 4))); gaps = np.diff(rs); cut = (rs[gaps.argmax()] + rs[gaps.argmax() + 1]) / 2
    core = uv_mask(gltf, n, lambda mm: (np.sqrt(mm.vertices[:, 0] ** 2 + mm.vertices[:, 2] ** 2)[mm.faces] < cut).all(1))
    lum = im.mean(2, keepdims=True)
    # tape: clear polyester film over white paper liner look: cool off-white, low contrast, soft gloss streaks
    base = np.array([236, 238, 236], np.float32)
    detail = (lum - lum[~core].mean()) * 0.25
    tape_rgb = base + detail + grain(im.shape, 3, 11, 1.5)
    xs = np.linspace(0, 1, n)[None, :, None]
    rings = 5 * np.sin(xs * 2 * np.pi * 60) * (xs > 0.47)                # faint wound-layer rings on the side faces (u = radius there)
    tape_rgb = tape_rgb + rings
    # core: light kraft cardboard, keeps the source's fibre detail
    kraft = np.array([198, 172, 134], np.float32)
    core_rgb = kraft * (0.55 + 0.45 * lum / max(lum[core].mean(), 1)) + grain(im.shape, 5, 12, 0.6)
    out = np.where(core[..., None], core_rgb, tape_rgb)
    Image.fromarray(out.clip(0, 255).astype(np.uint8)).save(path, quality=95)
    print('tape: core cut radius %.4f m, core texels %.1f %%' % (cut, 100 * core.mean()))


# ---------------------------------------------------------------- 2. reagent spray bottle
def bottle(folder):
    path = os.path.join(folder, 'textures', 'all_purpose_cleaner_diff_2k.jpg')
    im = np.asarray(Image.open(path).convert('RGB'), np.float32)
    n = im.shape[0]
    r, g, b = im[..., 0], im[..., 1], im[..., 2]
    red = (r > 120) & (g < 0.5 * r) & (b < 0.5 * r)                     # trigger: kept
    pink = (r > 120) & (b > 0.45 * r) & (g < 0.75 * r) & ~red             # body colour
    # body -> neutral white polyethylene, keeping the shading of the pink
    lum = im.mean(2)
    ref = np.median(lum[pink])
    white = np.array([228, 229, 226], np.float32)
    body = white[None, None, :] * (0.55 + 0.45 * (lum / ref))[..., None]
    out = im.copy()
    out[pink] = body[pink]
    # paint out the three printed labels with body colour (bounding boxes in 2K texels), then add grain
    for (x0, y0, x1, y1) in ((540, 590, 1070, 900), (505, 1140, 1060, 1935), (1050, 1180, 1320, 1670)):
        # fill from the recoloured body ring around the box: blend of the row-wise and column-wise edge values
        pad = 12
        top = out[y0 - pad:y0, x0:x1].mean(0); bot = out[y1:y1 + pad, x0:x1].mean(0)
        lef = out[y0:y1, x0 - pad:x0].mean(1); rig = out[y0:y1, x1:x1 + pad].mean(1)
        ty = np.linspace(0, 1, y1 - y0)[:, None, None]; tx = np.linspace(0, 1, x1 - x0)[None, :, None]
        vert = top[None] * (1 - ty) + bot[None] * ty
        horz = lef[:, None] * (1 - tx) + rig[:, None] * tx
        out[y0:y1, x0:x1] = (vert + horz) / 2
    out += grain(im.shape, 2.5, 21, 1.0)
    for (x0, y0, x1, y1) in ((540, 590, 1070, 900), (505, 1140, 1060, 1935), (1050, 1180, 1320, 1670)):
        red[y0:y1, x0:x1] = False                                        # red print inside the old labels stays painted out
    out[red] = im[red]
    img = Image.fromarray(out.clip(0, 255).astype(np.uint8))
    # labels. In this UV layout u runs up the bottle and v around it, so label art is rotated 90 degrees clockwise
    # (the label's top edge becomes its right edge; the text reads top to bottom in the texture, like the original print).
    def label(w, h, main):
        L = Image.new('RGB', (w, h), (250, 250, 247)); d = ImageDraw.Draw(L)
        d.rectangle((0, 0, w - 1, h - 1), outline=(60, 60, 64), width=max(2, w // 120))
        if main:
            band = int(h * 0.17)
            d.rectangle((0, 0, w, band), fill=(38, 40, 46))
            centred(d, (0, 0, w, band), 'FORENSIC  REAGENT', int(band * 0.42), True, (245, 245, 240))
            centred(d, (0, band, w, int(h * 0.47)), 'LATENT BLOOD', int(h * 0.15), True, (30, 32, 36))
            centred(d, (0, int(h * 0.44), w, int(h * 0.68)), 'REAGENT', int(h * 0.15), True, (30, 32, 36))
            centred(d, (0, int(h * 0.68), w, int(h * 0.78)), 'chemiluminescent spray  -  500 ml', int(h * 0.055), False, (60, 60, 64))
            centred(d, (0, int(h * 0.80), w, int(h * 0.87)), 'Mix fresh. Keep dark and cool. Do not ingest.', int(h * 0.04), False, (60, 60, 64))
            centred(d, (0, int(h * 0.88), w, int(h * 0.96)), 'LOT 26-10-A    EXP 10/2027', int(h * 0.04), False, (60, 60, 64))
        else:
            centred(d, (0, int(h * 0.05), w, int(h * 0.22)), 'LATENT BLOOD REAGENT', int(h * 0.075), True, (30, 32, 36))
            lines = ['Working solution, 500 ml', 'Spray a fine mist from 30 cm', 'in darkness; photograph at once.',
                     'Contains: luminol, sodium carbonate,', 'hydrogen peroxide (dilute).', 'Avoid skin and eye contact.',
                     'Wear gloves and eye protection.', 'Rinse surfaces after examination.']
            y = int(h * 0.26)
            for s in lines:
                centred(d, (0, y, w, y + int(h * 0.07)), s, int(h * 0.045), False, (60, 60, 64)); y += int(h * 0.075)
            d.rectangle((int(w * 0.1), int(h * 0.9), int(w * 0.9), int(h * 0.95)), fill=(38, 40, 46))
        return L
    # front label: u 0.275..0.50 (height), v 0.60..0.91 (around)  -> texels x 563..1024, y 1229..1864
    front = label(1864 - 1229, 1024 - 563, True).rotate(270, expand=True)
    img.paste(front, (563, 1229))
    # back label: u 0.275..0.515, v 0.30..0.43 -> x 563..1055, y 614..880
    back = label(880 - 614, 1055 - 563, False).rotate(270, expand=True)
    img.paste(back, (563, 614))
    img.save(path, quality=95)
    print('bottle: pink texels %.1f %%, red %.1f %%' % (100 * pink.mean(), 100 * red.mean()))


# ---------------------------------------------------------------- 3. dental stone bag
def bag(folder):
    path = os.path.join(folder, 'textures', 'cement_bag_diff_1k.jpg')
    arm = np.asarray(Image.open(os.path.join(folder, 'textures', 'cement_bag_arm_1k.jpg')).convert('RGB'), np.float32)
    n = arm.shape[0]
    ao = arm[..., 0] / 255
    paper = np.array([222, 219, 211], np.float32)
    base = paper[None, None, :] * (0.62 + 0.38 * ao)[..., None]
    base += grain((n, n, 3), 4, 31, 0.5) + grain((n, n, 3), 6, 32, 2.5)
    img = Image.fromarray(base.clip(0, 255).astype(np.uint8))
    ink = (44, 52, 70); band = (84, 120, 112); light = (236, 234, 228)

    def design(w, h):
        L = Image.new('RGBA', (w, h), (0, 0, 0, 0)); d = ImageDraw.Draw(L)
        d.rectangle((int(w * 0.03), int(h * 0.05), int(w * 0.97), int(h * 0.95)), outline=ink, width=max(2, h // 90))
        d.rectangle((int(w * 0.03), int(h * 0.05), int(w * 0.97), int(h * 0.24)), fill=band)
        centred(d, (int(w * 0.03), int(h * 0.05), int(w * 0.97), int(h * 0.24)), 'FORENSIC CASTING GRADE', int(h * 0.085), True, light)
        centred(d, (int(w * 0.03), int(h * 0.24), int(w * 0.97), int(h * 0.60)), 'DENTAL STONE', int(h * 0.24), True, ink)
        centred(d, (int(w * 0.03), int(h * 0.58), int(w * 0.97), int(h * 0.70)), 'Type IV die stone  -  high strength, low expansion', int(h * 0.055), False, ink)
        centred(d, (int(w * 0.03), int(h * 0.70), int(w * 0.97), int(h * 0.80)), 'For footwear, tyre and tool-mark impressions', int(h * 0.05), False, ink)
        d.rectangle((int(w * 0.36), int(h * 0.82), int(w * 0.64), int(h * 0.94)), fill=ink)
        centred(d, (int(w * 0.36), int(h * 0.82), int(w * 0.64), int(h * 0.94)), '11 kg  NET', int(h * 0.075), True, light)
        centred(d, (int(w * 0.03), int(h * 0.82), int(w * 0.34), int(h * 0.94)), 'Keep dry', int(h * 0.045), False, ink)
        centred(d, (int(w * 0.66), int(h * 0.82), int(w * 0.97), int(h * 0.94)), 'Mix 100 g : 24 ml water', int(h * 0.045), False, ink)
        return L

    def side(w, h):
        L = Image.new('RGBA', (w, h), (0, 0, 0, 0)); d = ImageDraw.Draw(L)
        d.rectangle((0, int(h * 0.35), w, int(h * 0.65)), fill=band)
        centred(d, (0, int(h * 0.35), w, int(h * 0.65)), 'DENTAL STONE   -   forensic casting grade   -   11 kg', int(h * 0.17), True, light)
        return L

    def end(w, h):
        L = Image.new('RGBA', (w, h), (0, 0, 0, 0)); d = ImageDraw.Draw(L)
        centred(d, (0, int(h * 0.15), w, int(h * 0.55)), 'DENTAL STONE', int(h * 0.3), True, ink)
        centred(d, (0, int(h * 0.55), w, int(h * 0.85)), '11 kg  -  keep dry', int(h * 0.17), False, ink)
        return L

    # top face: u 0.03..0.64 (bag length), v 0.58..0.98 (bag width); paper print, slightly worn
    top = design(int(n * 0.61), int(n * 0.40))
    img.paste(top, (int(n * 0.03), int(n * 0.58)), top)
    # underside: same islands mirrored in u
    bottom = design(int(n * 0.61), int(n * 0.36)).transpose(Image.FLIP_LEFT_RIGHT)
    img.paste(bottom, (int(n * 0.03), int(n * 0.16)), bottom)
    # long sides (u 0.67..0.78 and 0.80..0.90 are the bag height, v 0.38..0.98 the length): rotated strips
    s1 = side(int(n * 0.60), int(n * 0.11)).rotate(90, expand=True)
    img.paste(s1, (int(n * 0.67), int(n * 0.38)), s1)
    s2 = side(int(n * 0.60), int(n * 0.10)).rotate(270, expand=True)
    img.paste(s2, (int(n * 0.80), int(n * 0.38)), s2)
    # ends (u 0.02..0.42 and 0.55..0.95, v 0.01..0.13)
    e1 = end(int(n * 0.40), int(n * 0.12)); img.paste(e1, (int(n * 0.02), int(n * 0.01)), e1)
    e2 = end(int(n * 0.40), int(n * 0.12)).rotate(180); img.paste(e2, (int(n * 0.55), int(n * 0.01)), e2)
    # wear: the print fades a little with the AO/creases
    out = np.asarray(img, np.float32)
    out = out * (0.9 + 0.1 * ao)[..., None]
    Image.fromarray(out.clip(0, 255).astype(np.uint8)).save(path, quality=95)
    print('bag: rebuilt')


tape(os.path.join(R, 'LiftingTapeRoll'))
bottle(os.path.join(R, 'ReagentSprayBottle'))
bag(os.path.join(R, 'DentalStoneBag'))
