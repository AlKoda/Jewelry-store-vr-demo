"""DT_MaskedDetails_Color.png (RGBA) for the Downtown Buildings Set (Daniel Zhabotinsky, CC BY 4.0) and a
double-sided BuildingBrickCorner.obj.
The Sketchfab glTF ships this atlas (fire escapes, railings, razor and barbed wire, chain-link, truss) as an opaque
JPEG with the cut-outs composited onto black, so the cut-out shape is rebuilt from brightness:
  alpha = ramp of max(R,G,B) from 8 to 20 (alpha 0.5 at 14; background noise floor 0-4; the razor and barbed wire
          are only 15-31 bright, a 30-40 threshold would erase them); in the chain-link band (rows 560-830, wires
          5-10 bright over a 0-2 background) the ramp is 3 to 8; 3x3 median first, specks < 4 px removed
  RGB   = source colour; opaque texels darker than max-channel 40 are lifted towards 40 (gain <= 4) because the
          export darkened thin wires against black; transparent texels get the nearest opaque colour
          (no black fringe under bilinear filtering and mip-maps)
OBJ: every DT_MaskedDetails triangle gets a back-face twin (reversed winding, own vertex copies) so the cards show
from both sides with Unity's Standard shader (the source material is doubleSided). MTL points at the PNG.
usage: python3 -P masked_details.py SRC_baseColor.jpeg PACKDIR OUTDIR"""
import sys, os, re
import numpy as np
from PIL import Image
from scipy import ndimage

src_path, pack, out = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)

# ------------------------------------------------------------------ texture
src = np.asarray(Image.open(src_path).convert('RGB')).astype(np.float32)
mx = ndimage.median_filter(src.max(2), size=3)
alpha = np.clip((mx - 8.0) / 12.0, 0, 1)
FENCE = slice(560, 830)  # chain-link band: 1-px diamond wires only 5-10 bright over a 0-2 background
alpha[FENCE] = np.clip((mx[FENCE] - 3.0) / 5.0, 0, 1)
solid = alpha >= 0.5
lab, n = ndimage.label(solid, structure=np.ones((3, 3)))
sizes = ndimage.sum(solid, lab, range(1, n + 1))
small = np.isin(lab, np.nonzero(sizes < 4)[0] + 1)
alpha[small] = 0
# brightness floor: opaque texels darker than max-channel 40 are lifted towards 40 (gain <= 4), so thin wires
# read as dark weathered steel instead of black (the export premultiplied them against black)
gain = np.clip(40.0 / np.maximum(src.max(2), 1.0), 1.0, 4.0)
rgb = np.where((alpha > 0)[..., None], np.clip(src * gain[..., None], 0, 255), src)
# colour bleed into transparent texels
opaque = alpha >= 0.5
_, (iy, ix) = ndimage.distance_transform_edt(~opaque, return_indices=True)
rgb = np.where(opaque[..., None], rgb, rgb[iy, ix])
rgba = np.dstack([(rgb + 0.5).astype(np.uint8), (alpha * 255 + 0.5).astype(np.uint8)])
Image.fromarray(rgba, 'RGBA').save(os.path.join(out, 'DT_MaskedDetails_Color.png'), optimize=True)
print('alpha >= 0.5 fraction per band:', [round(float((alpha[a:b] >= 0.5).mean()), 3) for a, b in
                                          [(0, 135), (135, 265), (280, 345), (345, 425), (425, 560), (560, 830), (830, 1024)]])

# ------------------------------------------------------------------ OBJ
V, VT, groups = [], [], []
cur = None
for line in open(os.path.join(pack, 'BuildingBrickCorner.obj')):
    t = line.split()
    if not t:
        continue
    if t[0] == 'v':
        V.append(line.strip())
    elif t[0] == 'vt':
        VT.append(line.strip())
    elif t[0] == 'g':
        cur = dict(name=t[1], mtl=None, faces=[]); groups.append(cur)
    elif t[0] == 'usemtl':
        cur['mtl'] = t[1]
    elif t[0] == 'f':
        cur['faces'].append([tuple(int(x) for x in p.split('/')) for p in t[1:]])
    elif t[0] == 'vn':
        raise SystemExit('unexpected normals')
lines = ['# BuildingBrickCorner - converted for Unity (metres, Y up); DT_MaskedDetails faces doubled (back faces)',
         'mtllib BuildingBrickCorner.mtl']
added = 0
for g in groups:
    faces = g['faces']
    vi = sorted({p[0] for f in faces for p in f}); ti = sorted({p[1] for f in faces for p in f})
    if g['mtl'] == 'DT_MaskedDetails':
        assert vi == ti
    vmap = {o: i + 1 for i, o in enumerate(vi)}; tmap = {o: i + 1 for i, o in enumerate(ti)}
    g['v'] = [V[i - 1] for i in vi]; g['vt'] = [VT[i - 1] for i in ti]
    g['f'] = [[(vmap[a], tmap[b]) for a, b in f] for f in faces]
    if g['mtl'] == 'DT_MaskedDetails':
        nv, nt = len(g['v']), len(g['vt'])
        g['v'] = g['v'] + g['v']; g['vt'] = g['vt'] + g['vt']
        back = [[(a + nv, b + nt) for a, b in f[::-1]] for f in g['f']]
        added += len(back)
        g['f'] = g['f'] + back
vo = to = 0
for g in groups:
    lines += ['g %s' % g['name'], 'usemtl %s' % g['mtl']] + g['v'] + g['vt']
    lines += ['f ' + ' '.join('%d/%d' % (a + vo, b + to) for a, b in f) for f in g['f']]
    vo += len(g['v']); to += len(g['vt'])
open(os.path.join(out, 'BuildingBrickCorner.obj'), 'w').write('\n'.join(lines) + '\n')
mtl = open(os.path.join(pack, 'BuildingBrickCorner.mtl')).read().replace('DT_MaskedDetails_Color.jpg', 'DT_MaskedDetails_Color.png')
open(os.path.join(out, 'BuildingBrickCorner.mtl'), 'w').write(mtl)
print('back faces added', added, 'total tris', sum(len(g['f']) for g in groups))
