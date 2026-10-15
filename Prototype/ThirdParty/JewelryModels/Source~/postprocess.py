"""Re-orient/scale converted OBJs (from Tools/glb_to_obj.py), rename materials, convert extra maps.
postprocess.py WORK STAGE"""
import sys, os, re, json, numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from maps import ms_map, ms_from_color   # fixed October 15, 2026 (see maps.py)
W, STAGE = sys.argv[1], sys.argv[2]
CONV = os.path.join(W, 'conv1', 'out'); DL = os.path.join(W, 'dl')

def R(axis, deg):
    a = np.radians(deg); c, s = np.cos(a), np.sin(a)
    return {'x': np.array([[1, 0, 0], [0, c, -s], [0, s, c]]), 'y': np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]]), 'z': np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])}[axis]

def normal_map(src, out, size):
    Image.open(src).convert('RGB').resize((size, size), Image.LANCZOS).save(out, optimize=True)

def color_jpg(src, out, size):
    im = Image.open(src).convert('RGB')
    if max(im.size) > size: im = im.resize((size, size), Image.LANCZOS)
    im.save(out, quality=90, optimize=True)

def process(name, rot, scale_axis, target, mats, header, color=None, extras=()):
    """mats: old material name -> (new name, Kd or None, d or None, map_Kd or None)"""
    lines = open(os.path.join(CONV, name + '.obj')).read().splitlines()
    V = np.array([[float(x) for x in l.split()[1:4]] for l in lines if l.startswith('v ')])
    M = np.eye(3)
    for ax, deg in rot: M = (np.asarray(deg, float) if ax == 'M' else R(ax, deg)) @ M
    V2 = V @ M.T
    ext = np.ptp(V2, axis=0)
    s = target / ext['xyz'.index(scale_axis)]
    V2 *= s
    lo, hi = V2.min(0), V2.max(0)
    V2 -= np.array([(lo[0] + hi[0]) / 2, lo[1], (lo[2] + hi[2]) / 2])
    out = ['# ' + h for h in header.splitlines()]
    vi = 0
    for l in lines:
        if l.startswith('v '):
            out.append('v %.6f %.6f %.6f' % tuple(V2[vi])); vi += 1
        elif l.startswith('usemtl '):
            out.append('usemtl ' + mats[l.split()[1]][0])
        elif l.startswith('g '):
            g = l.split(None, 1)[1]
            out.append('g ' + re.sub(r'[^A-Za-z0-9_]', '_', g))
        else:
            out.append(l)
    open(os.path.join(STAGE, name + '.obj'), 'w', newline='\n').write('\n'.join(out) + '\n')
    ml = []
    for old, (new, kd, d, tex, ns) in mats.items():
        if any(new == m.split('\n')[0][7:] for m in ml): continue
        kd = tuple(float(x) ** (1 / 2.2) for x in (kd or (1, 1, 1)))   # linear albedo -> sRGB for the MTL
        blk = 'newmtl %s\nKd %.4f %.4f %.4f\nKs 0.5000 0.5000 0.5000\nNs %.1f\nd %.3f\nillum 2' % (new, kd[0], kd[1], kd[2], ns, d if d is not None else 1.0)
        if tex: blk += '\nmap_Kd ' + tex
        ml.append(blk)
    open(os.path.join(STAGE, name + '.mtl'), 'w', newline='\n').write('\n\n'.join(ml) + '\n')
    for fn, args in extras: fn(*args)
    tris = sum(1 for l in lines if l.startswith('f '))
    size = V2.max(0) - V2.min(0)
    print('%-24s tris %6d  size mm %s  scale %.6g' % (name, tris, np.round(size * 1000, 1), s))
    return dict(tris=tris, size_m=[round(float(x), 4) for x in size], scale=float(s), rotation=rot)

meta = {}
J = os.path.join
# ---- Poly Haven vintage pocket watch (CC0): lying face up, bow away from the viewer (-Z)
pw = J(DL, 'ph_pocketwatch_b', 'textures')
meta['PocketWatchVintage'] = process('PocketWatchVintage', [('x', -90)], 'x', 0.0613,
    {'vintage_pocket_watch': ('PocketWatchVintage', None, None, 'PocketWatchVintage_Color.jpg', 400),
     'vintage_pocket_watch_glass': ('Glass', (0.90, 0.94, 0.95), 0.20, None, 980),
     'vintage_pocket_watch_hands': ('BlackMetal', (0.03, 0.03, 0.03), None, None, 600)},
    'Vintage Pocket Watch by Tal Swicegood, Poly Haven (https://polyhaven.com/a/vintage_pocket_watch), CC0 1.0.\n'
    'Decimated to ~8k triangles, laid face up, metres. Part of the JewelryModels pack.',
    extras=[(color_jpg, (J(pw, 'vintage_pocket_watch_diff_2k.jpg'), J(STAGE, 'PocketWatchVintage_Color.jpg'), 1024)),
            (normal_map, (J(pw, 'vintage_pocket_watch_nor_gl_2k.jpg'), J(STAGE, 'PocketWatchVintage_Normal.png'), 1024)),
            (ms_map, (J(pw, 'vintage_pocket_watch_arm_2k.jpg'), J(STAGE, 'PocketWatchVintage_MetallicSmoothness.png'), 512, {'rough': 1, 'metal': 2}))])
# ---- An Old Ring (Qifan Zhang, CC BY 4.0): finger axis X, stone up
ro = J(DL, 'ring_old', 'textures')
ax = np.array([-0.127, -0.937, 0.324]); ax /= np.linalg.norm(ax)
if ax[0] < 0: ax = -ax
up = np.array([0, 0, 1.0]) - np.dot([0, 0, 1.0], ax) * ax; up /= np.linalg.norm(up)
MR = np.stack([ax, up, np.cross(ax, up)])          # rows: new X (finger axis), new Y (stone up), new Z
# target: 26.33 mm across (z) gives an 8.55 mm inner radius = 17.1 mm inner diameter (US size ~6.75), the same as the
# procedural rings (the first conversion used 21.5 mm, which left a 14.0 mm finger hole)
meta['RingAntiqueTigerEye'] = process('RingAntiqueTigerEye', [('M', MR.tolist())], 'z', 0.02633,
    {'EMP_Accessories': ('RingAntiqueTigerEye', None, None, 'RingAntiqueTigerEye_Color.jpg', 500)},
    'Based on "An Old Ring" (https://sketchfab.com/3d-models/an-old-ring-ea5e0c11f2e046c993239ea1a3efcad7)\n'
    'by Qifan Zhang (https://sketchfab.com/qifanzhang), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).\n'
    'Changes: decimated to 7k triangles, re-oriented (finger axis X, stone up), scaled to metres (17.1 mm inner diameter), textures resized.',
    extras=[(color_jpg, (J(ro, 'EMP_Accessories_baseColor.png'), J(STAGE, 'RingAntiqueTigerEye_Color.jpg'), 1024)),
            (normal_map, (J(ro, 'EMP_Accessories_normal.png'), J(STAGE, 'RingAntiqueTigerEye_Normal.png'), 512)),
            (ms_map, (J(ro, 'EMP_Accessories_metallicRoughness.png'), J(STAGE, 'RingAntiqueTigerEye_MetallicSmoothness.png'), 512, {'rough': 1, 'metal': 2}))])
# ---- Earring Jade (Softmind Game Factory, CC BY 4.0): pearl drop earring hanging, front +Z
ej = J(DL, 'earring_jade', 'textures')
meta['EarringPearlDrop'] = process('EarringPearlDrop', [], 'y', 0.040,
    {'PEARL_DROP_EARRINGSG': ('EarringPearlDrop', None, None, 'EarringPearlDrop_Color.jpg', 600)},
    'Based on "Earring Jade" (https://sketchfab.com/3d-models/earring-jade-c91e552728fb4b149d52193fd3e03ac5)\n'
    'by Softmind Game Factory (https://sketchfab.com/softmind), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).\n'
    'Changes: decimated to 7k triangles, scaled to 40 mm (metres), textures resized.',
    extras=[(color_jpg, (J(ej, 'PEARL_DROP_EARRINGSG_baseColor.png'), J(STAGE, 'EarringPearlDrop_Color.jpg'), 1024)),
            (normal_map, (J(ej, 'PEARL_DROP_EARRINGSG_normal.png'), J(STAGE, 'EarringPearlDrop_Normal.png'), 512)),
            # upstream metallicRoughness map is a constant white palette image (metallicFactor 1, roughnessFactor 0.2918),
            # i.e. everything metallic including the pearls: derive metallic from the colour map instead (gold 1, pearl 0)
            (ms_from_color, (J(ej, 'PEARL_DROP_EARRINGSG_baseColor.png'), J(STAGE, 'EarringPearlDrop_MetallicSmoothness.png'), 512, 1 - 0.2918254573170732))])
# ---- Gold leaf Earring (DineshThennarasan, CC BY 4.0)
meta['EarringGoldLeaf'] = process('EarringGoldLeaf', [], 'y', 0.050,
    {'Material_001': ('Gold18k', (1.0, 0.78, 0.40), None, None, 800), 'Material_002': ('Gold18k', (1.0, 0.78, 0.40), None, None, 800)},
    'Based on "Gold leaf Earring" (https://sketchfab.com/3d-models/gold-leaf-earring-64c4c78a861d45a28aa1b78873b02d14)\n'
    'by DineshThennarasan (https://sketchfab.com/dineshthennarasan), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).\n'
    'Changes: decimated to 7k triangles, scaled to 50 mm (metres), materials merged to Gold18k.')
# ---- Decahedron Bracelet: rejected after decimation (wire pattern breaks up)
if False: meta['BraceletSilverCuff'] = process('BraceletSilverCuff', [('x', 90)], 'x', 0.070,
    {'material_0': ('Silver925', (0.97, 0.96, 0.94), None, None, 700)},
    'Based on "Decahedron Bracelet" (https://sketchfab.com/3d-models/decahedron-bracelet-050e16e18feb4ac4b2ce5235141c97e6)\n'
    'by plasmeo3d (https://sketchfab.com/plasmeo3d), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).\n'
    'Changes: decimated to 8k triangles, laid flat, scaled to 70 mm (metres), material renamed Silver925.')
json.dump(meta, open(J(W, 'post_meta.json'), 'w'), indent=1)
