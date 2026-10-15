"""Assemble the ForensicModels pack from the staging folders, optimising sizes.
assemble.py WORK_DIR PACK_DIR
- OBJ: numbers rounded (v/vt 4 decimals = 0.1 mm, vn 3 decimals), comment header normalised.
- *_Normal.jpg: optional resize, re-encoded JPG q90.
- *_MetallicSmoothness.png: R = metallic, G = B = 0, A = smoothness (same convention as PBRExterior), optional resize.
- *_Color.jpg: optional resize, JPG q90.
"""
import sys, os, re, json, shutil
import numpy as np
from PIL import Image

W, PACK = sys.argv[1], sys.argv[2]
os.makedirs(PACK, exist_ok=True)

# model: (stage dir, colour px, normal px, ms px) ; None = keep
PLAN = {
    # Poly Haven conversions
    'Crowbar': ('stage', None, None, 512), 'Sledgehammer': ('stage', None, None, 512), 'BoltCutters': ('stage', None, None, 512),
    'CrossPeinHammer': ('stage', None, None, 256), 'FlatheadScrewdriver': ('stage', None, None, 256),
    'WorkGloves': ('stage', None, 1024, 512), 'Stone': ('stage', None, None, 256), 'Trashbag': ('stage', 512, 512, 256),
    'Clipboard': ('stage', None, 512, 256), 'MeasuringTape': ('stage', None, None, 256), 'MagnifyingGlass': ('stage', None, None, 256),
    'PortableSearchlight': ('stage', None, 512, 512), 'SecurityCamera': ('stage', None, 512, 512),
    'LeatherBinder': ('stage', None, 512, 256), 'OfficeNotepads': ('stage', None, 512, 256),
    # project-made: evidence, papers, glass
    'EvidenceBagPaper': ('stage_proc', None, 512, None), 'EvidenceBagPlastic': ('stage_proc', None, None, None),
    'EvidenceMarkerTent': ('stage_proc', None, None, None), 'ForensicScaleL': ('stage_proc', None, None, None),
    'PaperSheetForm': ('stage_proc', None, None, None), 'ScatteredPapers': ('stage_proc', None, None, None),
    'GlassShardsLarge': ('stage_glass2', None, None, None), 'GlassShardsScatter': ('stage_glass2', None, None, None),
    'TemperedGlassCrumbs': ('stage_glass2', None, None, None),
    # project-made: forensic kit
    'FingerprintBrush': ('stage_kit', None, None, None), 'FingerprintPowderJar': ('stage_kit', None, None, None),
    'NitrileGloveBox': ('stage_kit', None, None, None), 'ForensicCase': ('stage_kit', None, None, None),
    'TacticalFlashlight': ('stage_kit', None, None, None), 'UVTorch': ('stage_kit', None, None, None),
    'PoliceRadio': ('stage_kit', None, None, None),
    # project-made: scene and police items
    'PoliceBarrier': ('stage_scene', None, 512, 512), 'BrickHalf': ('stage_scene', 512, 512, 256),
    'ShoePrintCast': ('stage_scene', None, 1024, 256), 'CameraTripod': ('stage_scene', None, 512, 256),
    'PoliceCap': ('stage_scene', None, 512, 256), 'DuffelBag': ('stage_scene', None, 512, 256),
    'Balaclava': ('stage_scene', None, 512, 256), 'EvidenceMarkerCone': ('stage_scene', None, None, None),
}
EXTRA = [('stage_glass2', 'GlassCrackDecal.png', 'GlassCrackDecal_ColorAlpha.png'),
         ('stage_glass2', 'GlassCrackDecal_Normal.png', 'GlassCrackDecal_Normal.png')]
RENAME = {'EvidenceMarkerCone_Atlas.png': 'EvidenceMarkerCone_Atlas.jpg'}


def fmt(tok, nd):
    v = round(float(tok), nd)
    s = ('%.' + str(nd) + 'f') % v
    s = s.rstrip('0').rstrip('.') if '.' in s else s
    return '0' if s in ('-0', '') else s


def obj(src, dst, name, origin, keep_uv=True):
    out = ['# %s - ForensicModels pack. Units: metres, Y up. %s' % (name, origin)]
    for line in open(src):
        line = line.rstrip('\n')
        if line.startswith('#') or not line.strip():
            continue
        p = line.split()
        if p[0] == 'v':
            line = 'v ' + ' '.join(fmt(t, 4) for t in p[1:4])
        elif p[0] == 'vt':
            line = 'vt ' + ' '.join(fmt(t, 4) for t in p[1:3])
        elif p[0] == 'vn':
            line = 'vn ' + ' '.join(fmt(t, 3) for t in p[1:4])
        if not keep_uv:
            if p[0] == 'vt':
                continue
            if p[0] == 'f':
                cs = [c.split('/') for c in p[1:]]
                line = 'f ' + ' '.join(c[0] + ('//' + c[2] if len(c) > 2 and c[2] else '') for c in cs)
        out.append(line)
    open(dst, 'w').write('\n'.join(out) + '\n')


def main():
    for f in os.listdir(PACK):
        if f.endswith(('.obj', '.mtl', '.jpg', '.png')):
            os.remove(os.path.join(PACK, f))
    for name, (stage, cpx, npx, mpx) in PLAN.items():
        sd = os.path.join(W, stage)
        origin = 'Converted from Poly Haven (CC0).' if stage == 'stage' else 'Project-owned original work, generated for this repository.'
        mtl = open(os.path.join(sd, name + '.mtl')).read()
        obj(os.path.join(sd, name + '.obj'), os.path.join(PACK, name + '.obj'), name, origin, keep_uv='map_Kd' in mtl)
        for k, v in RENAME.items():
            mtl = mtl.replace(k, v)
        open(os.path.join(PACK, name + '.mtl'), 'w').write(mtl.strip() + '\n')
        for ref in re.findall(r'map_Kd\s+(\S+)', mtl):
            src_name = {v: k for k, v in RENAME.items()}.get(ref, ref)
            dst = os.path.join(PACK, ref)
            if os.path.exists(dst):
                continue
            im = Image.open(os.path.join(sd, src_name))
            if cpx and max(im.size) > cpx:
                im = im.resize((cpx, cpx * im.size[1] // im.size[0]), Image.LANCZOS)
            if ref.endswith('.jpg'):
                im.convert('RGB').save(dst, quality=90, optimize=True)
            else:
                im.save(dst, optimize=True)
        n = os.path.join(sd, name + '_Normal.jpg')
        if os.path.exists(n):
            im = Image.open(n).convert('RGB')
            if npx and max(im.size) > npx:
                im = im.resize((npx, npx * im.size[1] // im.size[0]), Image.LANCZOS)
            im.save(os.path.join(PACK, name + '_Normal.jpg'), quality=90, optimize=True)
        ms = os.path.join(sd, name + '_MetallicSmoothness.png')
        if os.path.exists(ms):
            a = np.asarray(Image.open(ms).convert('RGBA'))
            out = np.zeros_like(a); out[..., 0] = a[..., 0]; out[..., 3] = a[..., 3]
            im = Image.fromarray(out, 'RGBA')
            if mpx and max(im.size) > mpx:
                im = im.resize((mpx, mpx * im.size[1] // im.size[0]), Image.LANCZOS)
            im.save(os.path.join(PACK, name + '_MetallicSmoothness.png'), optimize=True)
    for stage, src, dst in EXTRA:
        shutil.copy(os.path.join(W, stage, src), os.path.join(PACK, dst))
    tot = sum(os.path.getsize(os.path.join(PACK, f)) for f in os.listdir(PACK))
    print('files', len(os.listdir(PACK)), 'bytes', tot)


main()
