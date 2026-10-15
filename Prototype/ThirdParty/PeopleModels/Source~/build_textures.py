"""Convert Rocketbox TGA maps (2048 px) to the project's texture convention and write the MTL.

    python3 -I build_textures.py config.json

Per material: <Name>_<Mat>_Color.jpg (sRGB), <Name>_<Mat>_Normal.jpg (tangent space, OpenGL +Y),
<Name>_<Mat>_MetallicSmoothness.png (R metallic, A smoothness, linear) derived from the Rocketbox
specular map. Optional edits (recolouring, painting out) run on the full-size colour map first.
Optional per-material keys: quality (JPEG, default 88), jpeg_max (cap every decoded colour texel at
this sRGB value, see save_jpeg_capped) with subsampling (0 = 4:4:4, the default when jpeg_max is set).
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageFilter

cfg = json.load(open(sys.argv[1]))
out, name = cfg['out'], cfg['name']
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import edits as E  # project-made recolour helpers



def save_jpeg_capped(img, path, quality, subsampling, cap):
    """Save an sRGB JPEG whose decoded texels never exceed `cap` (used for the white coverall maps).
    Texels above the cap are clamped first; JPEG (and Lanczos) ringing at hard colour edges, such as
    a blue glove against a white sleeve, still overshoots by up to ~15 levels, so the image is encoded,
    decoded and the overshoot (plus a smooth 2-texel falloff around it) is taken off the luminance of
    the source, repeated until no texel decodes above the cap (at most 16 passes)."""
    import io
    from scipy.ndimage import gaussian_filter, maximum_filter
    work = np.minimum(np.asarray(img.convert('RGB'), np.float32), cap)
    for _ in range(16):
        buf = io.BytesIO()
        Image.fromarray(work.round().clip(0, 255).astype(np.uint8)).save(
            buf, 'JPEG', quality=quality, optimize=True, progressive=False, subsampling=subsampling)
        dec = np.asarray(Image.open(io.BytesIO(buf.getvalue())).convert('RGB'), np.float32)
        over = np.maximum(dec - cap, 0).max(axis=2)
        if over.max() <= 0:
            break
        corr = np.maximum(gaussian_filter(maximum_filter(over, 5), 2.0), over) + (over > 0)
        work = np.clip(work - corr[..., None], 0, 255)
    else:
        raise SystemExit('jpeg_max: %s still decodes above %d' % (path, cap))
    open(path, 'wb').write(buf.getvalue())


mtl = ['# %s materials. Unity reads Kd, map_Kd and d; assign _Normal and _MetallicSmoothness in code.' % name]
for mat, m in cfg['materials'].items():
    pre = os.path.join(cfg['textures'], m['prefix'])
    color = Image.open(pre + '_color.tga')
    has_alpha = color.mode == 'RGBA' and m.get('alpha', False)
    color = color.convert('RGBA' if has_alpha else 'RGB')
    normal = Image.open(pre + '_normal.tga').convert('RGB') if os.path.exists(pre + '_normal.tga') else None
    spec = Image.open(pre + '_specular.tga').convert('L') if os.path.exists(pre + '_specular.tga') else None
    smooth_override = None
    metal_override = None
    for ed in m.get('edits', []):
        res = getattr(E, ed['fn'])(color, normal, spec, out, name, mat, **ed.get('args', {}))
        color, normal, spec = res[0], res[1], res[2]
        if len(res) > 3 and res[3] is not None:
            smooth_override = res[3]
        if len(res) > 4 and res[4] is not None:
            metal_override = res[4]
    size = m.get('size', 1024)
    cname = '%s_%s_Color.%s' % (name, mat, 'png' if has_alpha else 'jpg')
    if has_alpha:
        # Keep the source aspect (hair atlas is 1024 x 512) and bleed the colour of the strands into
        # the transparent texels so mip-mapped alpha-test edges do not pick up a dark fringe.
        w, h = color.size
        c = color.resize((size, max(1, round(size * h / w))), Image.LANCZOS)
        a = np.asarray(c).astype(np.float32)
        rgb, al = a[..., :3].copy(), a[..., 3]
        known = al > 8
        from scipy.ndimage import distance_transform_edt
        _, (iy, ix) = distance_transform_edt(~known, return_indices=True)
        rgb[~known] = rgb[iy[~known], ix[~known]]
        c = Image.fromarray(np.dstack([rgb, al]).clip(0, 255).astype(np.uint8), 'RGBA')
        c.save(os.path.join(out, cname), optimize=True)
    elif m.get('jpeg_max'):
        c = color.resize((size, size), Image.LANCZOS)
        save_jpeg_capped(c, os.path.join(out, cname), m.get('quality', 88), m.get('subsampling', 0), m['jpeg_max'])
    else:
        c = color.resize((size, size), Image.LANCZOS)
        c.save(os.path.join(out, cname), quality=m.get('quality', 88), optimize=True, progressive=False)
    maps = {'Color': cname}
    if normal is not None and m.get('nsize', 1024):
        n = np.asarray(normal).astype(np.float32) / 255 * 2 - 1
        if m.get('flipG'):
            n[..., 1] *= -1
        n /= np.linalg.norm(n, axis=2, keepdims=True) + 1e-6
        nimg = Image.fromarray(((n * 0.5 + 0.5) * 255 + 0.5).clip(0, 255).astype(np.uint8))
        nimg = nimg.resize((m.get('nsize', 1024), m.get('nsize', 1024)), Image.LANCZOS)
        nn = '%s_%s_Normal.jpg' % (name, mat)
        nimg.save(os.path.join(out, nn), quality=90, optimize=True)
        maps['Normal'] = nn
    if spec is not None and m.get('mssize', 512):
        s = np.asarray(spec).astype(np.float32) / 255
        sm = np.clip(0.16 + 2.4 * s, 0.12, 0.72)
        if smooth_override is not None:
            sm = smooth_override
        met = np.clip((s - 0.35) / 0.3, 0, 1) if m.get('metal') else np.zeros_like(s)
        if metal_override is not None:
            met = metal_override
        rgba = np.zeros(s.shape + (4,), np.uint8)
        rgba[..., 0] = (met * 255 + 0.5).astype(np.uint8)
        rgba[..., 3] = (sm * 255 + 0.5).astype(np.uint8)
        ms = Image.fromarray(rgba, 'RGBA').resize((m.get('mssize', 512), m.get('mssize', 512)), Image.LANCZOS)
        mn = '%s_%s_MetallicSmoothness.png' % (name, mat)
        ms.save(os.path.join(out, mn), optimize=True)
        maps['MetallicSmoothness'] = mn
    mtl += ['', 'newmtl %s' % mat, 'Kd 1.000 1.000 1.000', 'Ks 0.040 0.040 0.040', 'd 1.000',
            'map_Kd %s' % cname]
    print(mat, maps)
if cfg.get('write_mtl', False):
    open(os.path.join(out, name + '.mtl'), 'w').write('\n'.join(mtl) + '\n')
