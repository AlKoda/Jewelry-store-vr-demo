"""Project-made texture edits for the PeopleModels pack.

forensic_body / forensic_protection turn the Rocketbox Medical_Male_04 (green scrubs, surgical cap and
mask) into a crime-scene examiner: white disposable coverall, white hood cap, blue nitrile gloves.
The white is kept below Unity's non-metal albedo limit (median 220 sRGB, highlights rolled off below
238, see WHITE_*); build_textures.py then caps the JPEG at 240 (jpeg_max) against ringing.
paint_out blurs small areas (emblems) so no maker's mark survives.
Every function takes (color, normal, spec, out, name, mat, **args) and returns
(color, normal, spec, smoothness_override_or_None, metallic_override_or_None) at source resolution.
"""
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

TYVEK = np.array([236, 238, 240], np.float32) / 255
NITRILE = np.array([62, 96, 205], np.float32) / 255
# White fabric albedo levels (sRGB, 0-1). Unity recommends at most 243 for a non-metal albedo; the
# coverall and cap are normalised to a median of 220 and their highlights roll off smoothly from 224
# towards 238, so nothing clips and the fold shading survives the torch, the camera flash and bloom.
WHITE_MEDIAN = 220 / 255
WHITE_KNEE = 224 / 255
WHITE_CEIL = 238 / 255
CUFF_FEATHER = 1.0  # gaussian sigma (texels) of the sleeve-to-glove blend at the cuff


def _rolloff(rgb):
    """Compress the brightest channel of each (n, 3) colour above WHITE_KNEE with an exponential
    shoulder (slope 1 at the knee, asymptote WHITE_CEIL), keeping the hue. Values below the knee
    are unchanged."""
    y = rgb.max(axis=-1)
    span = WHITE_CEIL - WHITE_KNEE
    yc = np.where(y > WHITE_KNEE, WHITE_KNEE + span * (1 - np.exp(-(y - WHITE_KNEE) / span)), y)
    return rgb * (yc / np.maximum(y, 1e-6))[..., None]


def _white_fabric(shade, ref):
    """TYVEK albedo times a per-texel shade, scaled so that the median channel mean of the texels in
    `ref` (a subset of `shade`) is WHITE_MEDIAN, with the highlights rolled off below WHITE_CEIL
    (_rolloff). shade: 1-D float array; returns an (n, 3) array."""
    k = WHITE_MEDIAN / (float(TYVEK.mean()) * float(np.median(ref)) + 1e-6)
    return _rolloff(TYVEK[None, :] * (shade * k)[:, None])


def _pad(rgb, valid):
    """Edge padding in place: the unused (black) texels take the colour of the nearest used texel.
    This stops the JPEG and Lanczos ringing at the UV island borders (white next to black overshoots
    past 250) and the dark seams in the mip chain. Only texels with no source content change."""
    from scipy.ndimage import distance_transform_edt
    _, (iy, ix) = distance_transform_edt(~valid, return_indices=True)
    rgb[~valid] = rgb[iy[~valid], ix[~valid]]


def _label_mask(labels_path, material, size, classify):
    """Rasterise the per-triangle bone labels of one material into class maps (uint8)."""
    labels = json.load(open(labels_path))
    img = Image.new('L', (size, size), 0)
    d = ImageDraw.Draw(img)
    # draw low classes first so higher classes (hands) win at shared seams
    tris = []
    for mat, bone, uv in labels:
        if mat != material:
            continue
        c = classify(bone)
        tris.append((c, [(u * size, (1 - v) * size) for u, v in uv]))
    for c, pts in sorted(tris, key=lambda t: t[0]):
        d.polygon(pts, fill=c, outline=c)
    # grow each island so texture filtering at seams does not pick up the old colour
    a = np.asarray(img).copy()
    for _ in range(24):
        g = np.asarray(Image.fromarray(a).filter(ImageFilter.MaxFilter(9)))
        a = np.where(a > 0, a, g)
    return a


def _lum(rgb):
    return rgb[..., 0] * 0.299 + rgb[..., 1] * 0.587 + rgb[..., 2] * 0.114


def _blur(arr, r):
    from scipy.ndimage import gaussian_filter
    return gaussian_filter(arr.astype(np.float32), r, mode='nearest')


def forensic_body(color, normal, spec, out, name, mat, labels=None, **_):
    size = color.size[0]

    def classify(bone):
        if 'Hand' in bone or 'Finger' in bone:
            return 3
        if 'Forearm' in bone or 'UpperArm' in bone:
            return 2
        return 1

    cls = _label_mask(labels, mat, size, classify)
    rgb = np.asarray(color.convert('RGB'), np.float32) / 255
    L = _lum(rgb)
    valid = L > 0.004
    # Fabric shading: original luminance relative to its local mean keeps folds and seams.
    Ls = _blur(L, 2.0)
    Lbig = _blur(L, 40.0) + 1e-3
    fold = np.clip(Ls / Lbig, 0.55, 1.25)
    broad = np.clip(Lbig / np.median(Lbig[valid & (cls == 1)]), 0.6, 1.15)
    shade = np.clip(fold * (0.75 + 0.25 * broad), 0.45, 1.12)
    out_rgb = rgb.copy()
    fabric = (cls == 1) | (cls == 2)
    # sleeves over the bare forearms: smoother shading (no skin hair / veins)
    arm_shade = np.clip(_blur(L, 6.0) / (_blur(L, 40.0) + 1e-3), 0.7, 1.15)
    sh = np.where(cls == 2, arm_shade, shade)
    # subtle crinkle noise typical of spun-bonded polyethylene
    rng = np.random.default_rng(3)
    n = rng.random((size // 4, size // 4)).astype(np.float32)
    n = np.asarray(Image.fromarray(n, 'F').resize((size, size), Image.BICUBIC), np.float32)
    sh = sh * (0.97 + 0.06 * n)
    # white coverall: median 220 sRGB, highlights rolled off below WHITE_CEIL (see _white_fabric)
    gl = cls == 3
    white = _white_fabric(sh.ravel(), sh[fabric & valid]).reshape(rgb.shape)
    # gloves: keep the hand's form shading, remove skin colour (not affected by the white levels)
    hand_shade = np.clip(_blur(L, 1.5) / (np.median(L[gl & valid]) + 1e-3), 0.45, 1.35)
    glove = NITRILE[None, None, :] * hand_shade[..., None]
    # The glove keeps its colour and median; only its blue highlights, which used to clip at 255,
    # roll off below WHITE_CEIL like the white (they also caused JPEG ringing on the white cuff).
    glove = _rolloff(glove)
    # A soft cuff: sleeve and glove blend over about two texels (normalised, so glove edges at UV
    # borders stay blue). A hard white/blue step makes the JPEG ring well past the white ceiling.
    bg, bf = _blur(gl.astype(np.float32), CUFF_FEATHER), _blur(fabric.astype(np.float32), CUFF_FEATHER)
    a = np.clip(bg / (bg + bf + 1e-6), 0, 1)[..., None]
    dressed = fabric | gl
    out_rgb[dressed] = (white * (1 - a) + glove * a)[dressed]
    _pad(out_rgb, valid)
    smooth = np.full(L.shape, 0.24, np.float32)
    smooth[gl] = 0.58
    # flatten the skin detail in the normal map under sleeves and gloves
    nrm = np.asarray(normal, np.float32) / 255 * 2 - 1
    k = np.where(cls == 2, 0.8, np.where(gl, 0.5, 0.0))[..., None]
    flat = np.array([0, 0, 1], np.float32)
    nrm = nrm * (1 - k) + flat * k
    nrm /= np.linalg.norm(nrm, axis=2, keepdims=True) + 1e-6
    normal = Image.fromarray(((nrm * 0.5 + 0.5) * 255 + 0.5).clip(0, 255).astype(np.uint8))
    color = Image.fromarray((out_rgb.clip(0, 1) * 255 + 0.5).astype(np.uint8))
    return color, normal, spec, smooth, None


def forensic_protection(color, normal, spec, out, name, mat, **_):
    """Surgical cap (blue-green) -> white hood fabric; the white mask and ties stay."""
    rgb = np.asarray(color.convert('RGB'), np.float32) / 255
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    tinted = (g - r > 0.06) | (b - r > 0.06)
    L = _lum(rgb)
    Ls = _blur(L, 1.0)
    ref = np.median(L[tinted]) + 1e-3
    shade = np.clip(Ls / ref, 0.5, 1.2)
    out_rgb = rgb.copy()
    s = (0.85 + 0.15 * shade[tinted]) * shade[tinted] ** 0.35
    # same white levels as the coverall: median 220 sRGB, highlights rolled off below 238
    out_rgb[tinted] = _white_fabric(s, s)
    _pad(out_rgb, L > 0.004)
    color = Image.fromarray((out_rgb.clip(0, 1) * 255 + 0.5).astype(np.uint8))
    smooth = np.full(L.shape, 0.22, np.float32)
    return color, normal, spec, smooth, None


def paint_out(color, normal, spec, out, name, mat, boxes=(), **_):
    """Clone-stamp over small marks. boxes: [x0, y0, x1, y1, dx, dy] in source pixels; the box is
    replaced by the area shifted by (dx, dy), with a feathered edge, in the colour, normal and
    specular maps alike."""
    for b in boxes:
        x0, y0, x1, y1, dx, dy = (int(v) for v in b)
        w, h = x1 - x0, y1 - y0
        m = Image.new('L', (w, h), 0)
        ImageDraw.Draw(m).rectangle([3, 3, w - 4, h - 4], fill=255)
        m = m.filter(ImageFilter.GaussianBlur(2))
        for im in (color, normal, spec):
            if im is None:
                continue
            patch = im.crop((x0 + dx, y0 + dy, x1 + dx, y1 + dy))
            im.paste(patch, (x0, y0), m)
    return color, normal, spec, None, None
