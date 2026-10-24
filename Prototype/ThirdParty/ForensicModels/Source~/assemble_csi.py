"""Assemble the CSI consumables batch (proc_csi.py output) into the ForensicModels pack and record it in sources.json.
assemble_csi.py STAGE_DIR PACK_DIR

Same conventions as assemble.py (which rebuilds the October 14 pack and is left unchanged): OBJ numbers rounded
(v/vt 4 decimals = 0.1 mm, vn 3 decimals), comment header normalised, MTL copied. Textures are copied as proc_csi.py
wrote them (Color JPG q90 / RGBA PNG, Normal JPG q92, MetallicSmoothness PNG with R = metallic, G = B = 0, A = smoothness).
Only the files of this batch are touched; nothing else in the pack is removed or rewritten.

sources.json: appends one entry per shipped file (bytes, SHA-256, generator, licence) and per batch script, adds the
models to "models", and a "csi_consumables" block. Then VERIFY: every OBJ parses (triangles only, indices valid), the
MTL exists and every map_Kd resolves, textures are 1024 px or smaller, base on y = 0 and centred in x/z, and the bounds
match the nominal real sizes recorded by proc_csi.py (within 25 % or 1 cm). Prints a table (name, size, tris, bytes).
"""
import sys, os, re, json, shutil, hashlib
import numpy as np
from PIL import Image

STAGE, PACK = sys.argv[1], sys.argv[2]
HERE = os.path.dirname(os.path.abspath(__file__))
STATS = json.load(open(os.path.join(STAGE, 'stats_csi.json')))
ADDED = 'October 24, 2026'
HEADER = '# %s - ForensicModels pack. Units: metres, Y up. Project-owned original work, generated for this repository.'


def fmt(tok, nd):
    v = round(float(tok), nd)
    s = ('%.' + str(nd) + 'f') % v
    s = s.rstrip('0').rstrip('.') if '.' in s else s
    return '0' if s in ('-0', '') else s


def round_obj(src, dst, name):
    out = [HEADER % name]
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
        out.append(line)
    open(dst, 'w').write('\n'.join(out) + '\n')


def sha(p):
    return hashlib.sha256(open(p, 'rb').read()).hexdigest()


def files_of(name, info):
    if info['kind'] == 'decal':
        return list(info['files'])
    fs = [name + '.obj', name + '.mtl']
    for suf in ('_Color.jpg', '_Color.png', '_Normal.jpg', '_MetallicSmoothness.png'):
        if os.path.exists(os.path.join(STAGE, name + suf)):
            fs.append(name + suf)
    return fs


# ------------------------------------------------------------------------------------------ copy
shipped = {}
for name, info in STATS.items():
    for f in files_of(name, info):
        src, dst = os.path.join(STAGE, f), os.path.join(PACK, f)
        if f.endswith('.obj'):
            round_obj(src, dst, name)
        elif f.endswith('_MetallicSmoothness.png'):
            a = np.asarray(Image.open(src).convert('RGBA')); out = np.zeros_like(a); out[..., 0] = a[..., 0]; out[..., 3] = a[..., 3]
            Image.fromarray(out, 'RGBA').save(dst, optimize=True)   # R = metallic, G = B = 0, A = smoothness
        else:
            shutil.copy(src, dst)
        shipped[f] = name

# ------------------------------------------------------------------------------------- sources.json
SJ = os.path.join(PACK, 'sources.json')
raw = open(SJ).read()
doc = json.loads(raw)
if json.dumps(doc, indent=1) != raw.rstrip('\n'):
    print('note: sources.json was not in json.dump(indent=1) form; rewriting it in that form (content preserved)')
NOTE = ('Procedural geometry and textures (Source~/proc_csi.py, numpy + Pillow, assembled by Source~/assemble_csi.py); printed text '
        'rasterised with the DejaVu fonts of the build machine and, for the short Arabic lines, the project\'s Noto Sans Arabic '
        '(Prototype/ThirdParty/Fonts/Arabic, SIL OFL; glyph rendering only, no font file shipped). All names, case numbers, lot '
        'numbers, forms and suppliers ("Fictional Lab Supply") are fictional; no brand is depicted. The biohazard pictogram is an '
        'own drawing of the generic public-domain symbol.')
doc['files'] = [e for e in doc['files'] if e['file'] not in shipped and e['file'] not in ('Source~/proc_csi.py', 'Source~/assemble_csi.py')]
batch = []
for f in sorted(shipped):
    name = shipped[f]
    e = {'file': f, 'bytes': os.path.getsize(os.path.join(PACK, f)), 'sha256': sha(os.path.join(PACK, f)), 'model': name,
         'origin': 'Project-owned original work',
         'source_url': 'https://github.com/AlKoda/Jewelry-store-vr-demo (generated for this repository; pack folder Prototype/ThirdParty/ForensicModels)',
         'generator': 'Source~/proc_csi.py', 'author': 'Project (generated with Claude Code, October 2026)',
         'license': 'Project-owned; no third-party material', 'version': ADDED, 'notes': NOTE}
    if name == 'LiftCardWithPrint' and f.endswith('_Color.jpg'):
        fp = os.path.normpath(os.path.join(HERE, '..', '..', 'SurfaceDecals', 'Fingerprints_Atlas.png'))
        e['derived_from'] = {'file': 'Prototype/ThirdParty/SurfaceDecals/Fingerprints_Atlas.png (project-made synthetic prints, no real person)',
                             'sha256': sha(fp) if os.path.exists(fp) else None, 'use': 'two developed-print cells (bottom row) composited onto the tape strip'}
    batch.append(e)
for script in ('proc_csi.py', 'assemble_csi.py'):
    p = os.path.join(PACK, 'Source~', script)
    batch.append({'file': 'Source~/' + script, 'bytes': os.path.getsize(p), 'sha256': sha(p), 'origin': 'Project-owned original work',
                  'license': 'Project-owned', 'version': ADDED,
                  'notes': 'Generator / assembly script of the CSI consumables batch (Unity ignores folders ending in ~).'})
doc['files'] += batch
for name, info in STATS.items():
    if info['kind'] == 'model':
        doc['models'][name + '.obj'] = {'kind': 'model', 'tris': info['tris'], 'size_m': info['size_m'], 'min_y': info['min_y'],
                                        'materials': info['materials'], 'maps': info['maps'], 'added': ADDED}
doc['csi_consumables'] = {
    'added': '2026-10-24', 'status': 'unverified in Unity (not imported, not wired)',
    'generator': 'Source~/proc_csi.py', 'assembler': 'Source~/assemble_csi.py',
    'dimensions_from': 'research_notes/Crime scene forensic procedures/equipment_training.md (section 1) and fingerprints.md (sections 3, 5, 8)',
    'models': {n: {'nominal_size_m': i['nominal_size_m'], 'tris': i['tris'], 'note': i['note']} for n, i in STATS.items() if i['kind'] == 'model'},
    'decals': {n: {'files': i['files'], 'suggested_size_m': i['suggested_size_m'], 'note': i['note']} for n, i in STATS.items() if i['kind'] == 'decal'},
    'text': 'English fictional print; short Arabic lines in Noto Sans Arabic (project font) on BackingCard*, LiftCardWithPrint, HingeLifter*, '
            'GelLifter*, SwabTube, SwabOpen, SwabBox, KMTestKit, MikrosilTube, MikrosilCatalyst, EvidenceTag, EvidenceLabelRoll, '
            'BiohazardBinSmall, HairFibreSpecimen',
}
open(SJ, 'w').write(json.dumps(doc, indent=1))

# ---------------------------------------------------------------------------------------- VERIFY
def parse_obj(path):
    V, T, N, F, mtl, mats = [], [], [], [], None, set()
    for line in open(path):
        p = line.split()
        if not p or p[0] == '#':
            continue
        if p[0] == 'v': V.append([float(x) for x in p[1:4]])
        elif p[0] == 'vt': T.append([float(x) for x in p[1:3]])
        elif p[0] == 'vn': N.append([float(x) for x in p[1:4]])
        elif p[0] == 'mtllib': mtl = p[1]
        elif p[0] == 'usemtl': mats.add(p[1])
        elif p[0] == 'f':
            assert len(p) == 4, 'non-triangle face in ' + path
            for c in p[1:]:
                q = c.split('/')
                vi = int(q[0]); assert 1 <= vi <= len(V), 'bad v index'
                if len(q) > 1 and q[1]: assert 1 <= int(q[1]) <= len(T), 'bad vt index'
                if len(q) > 2 and q[2]: assert 1 <= int(q[2]) <= len(N), 'bad vn index'
            F.append(p[1:])
    return np.array(V), len(F), mtl, mats


problems = []
rows = []
for name, info in STATS.items():
    fs = files_of(name, info)
    nbytes = sum(os.path.getsize(os.path.join(PACK, f)) for f in fs)
    for f in fs:
        if f.endswith(('.jpg', '.png')):
            im = Image.open(os.path.join(PACK, f))
            if max(im.size) > 1024:
                problems.append('%s: texture %s is %d px' % (name, f, max(im.size)))
            if f.endswith('_MetallicSmoothness.png'):
                a = np.asarray(im.convert('RGBA'))
                if a[..., 1].any() or a[..., 2].any():
                    problems.append('%s: MS map has G/B data' % name)
    if info['kind'] == 'decal':
        rows.append((name, 'decal 1024 px', '-', nbytes)); continue
    V, ntri, mtl, mats = parse_obj(os.path.join(PACK, name + '.obj'))
    if mtl != name + '.mtl' or not os.path.exists(os.path.join(PACK, mtl)):
        problems.append('%s: mtllib %s missing' % (name, mtl))
    mtxt = open(os.path.join(PACK, name + '.mtl')).read()
    defined = set(re.findall(r'^newmtl\s+(\S+)', mtxt, re.M))
    if mats - defined:
        problems.append('%s: materials %s not in MTL' % (name, sorted(mats - defined)))
    for ref in re.findall(r'map_Kd\s+(\S+)', mtxt):
        if not os.path.exists(os.path.join(PACK, ref)):
            problems.append('%s: map_Kd %s missing' % (name, ref))
    lo, hi = V.min(0), V.max(0); size = hi - lo
    if abs(lo[1]) > 0.0002:
        problems.append('%s: min y = %.4f' % (name, lo[1]))
    cx, cz = (lo[0] + hi[0]) / 2, (lo[2] + hi[2]) / 2
    if abs(cx) > 0.001 or abs(cz) > 0.001:
        problems.append('%s: not centred (%.4f, %.4f)' % (name, cx, cz))
    nom = np.array(info['nominal_size_m'])
    dev = np.abs(size - nom)
    if (dev > np.maximum(0.25 * nom, 0.01)).any():
        problems.append('%s: size %s vs nominal %s' % (name, np.round(size, 4), nom))
    if ntri != info['tris']:
        problems.append('%s: %d triangles in the pack, %d at generation' % (name, ntri, info['tris']))
    if ntri > 4000:
        problems.append('%s: %d triangles (over 4,000)' % (name, ntri))
    rows.append((name, '%.3f x %.3f x %.3f' % tuple(size), ntri, nbytes))

print('%-24s %-24s %6s %9s' % ('model', 'size x y z (m)', 'tris', 'bytes'))
for r in rows:
    print('%-24s %-24s %6s %9d' % r)
print('total bytes', sum(r[3] for r in rows), 'files', len(shipped))
if problems:
    print('PROBLEMS:'); [print(' -', p) for p in problems]; sys.exit(1)
print('VERIFY ok')
