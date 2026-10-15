"""Final pack check: images open, OBJs load, MTL maps exist, degenerate triangles, sources.json coverage + hashes, sizes."""
import sys, os, json, hashlib, re
import numpy as np
from PIL import Image
import trimesh

PK = sys.argv[1]
S = json.load(open(os.path.join(PK, 'sources.json')))
errors = []
files = []
for dp, dn, fn in os.walk(PK):
    for f in fn:
        files.append(os.path.relpath(os.path.join(dp, f), PK))
files.sort()
listed = {e['file']: e for e in S['files']}
docs = {'sources.json', 'README.md', 'LICENSE.txt'}
for f in files:
    p = os.path.join(PK, f)
    if f.startswith('.') or '/.' in f:
        errors.append('hidden file ' + f)
    if not re.fullmatch(r'[A-Za-z0-9_~./-]+', f):
        errors.append('bad name ' + f)
    if os.path.getsize(p) > 20e6:
        errors.append('too big ' + f)
    if f in docs:
        continue
    e = listed.get(f)
    if e is None:
        errors.append('not in sources.json: ' + f)
    elif e['sha256'] != hashlib.sha256(open(p, 'rb').read()).hexdigest():
        errors.append('sha mismatch: ' + f)
    elif e['license'] in ('?', '') or e['source'] in ('?', ''):
        errors.append('no licence/source: ' + f)
for f in listed:
    if not os.path.exists(os.path.join(PK, f)):
        errors.append('listed but missing: ' + f)
n_img = 0
for f in files:
    if f.lower().endswith(('.jpg', '.png')):
        with Image.open(os.path.join(PK, f)) as im:
            im.verify()
        with Image.open(os.path.join(PK, f)) as im:
            im.load()
            n_img += 1
            if f.endswith('_Color.png') and im.mode != 'RGBA':
                errors.append('colour PNG without alpha: ' + f)
            if f.endswith('_Color.png'):
                a = np.asarray(im)[..., 3]
                print('  %s alpha: %.1f %% below 128, min %d max %d' % (f, (a < 128).mean() * 100, a.min(), a.max()))
readme = open(os.path.join(PK, 'README.md')).read()
lic = open(os.path.join(PK, 'LICENSE.txt')).read()
tot_tris = 0
for f in files:
    if not f.endswith('.obj'):
        continue
    name = f[:-4]
    p = os.path.join(PK, f)
    sc = trimesh.load(p, force='scene', process=False)
    nf = sum(len(g.faces) for g in sc.geometry.values())
    # raw parse for degenerate faces and mtl refs
    V, deg, nfr = [], 0, 0
    for line in open(p):
        if line.startswith('v '):
            V.append([float(x) for x in line.split()[1:4]])
    V = np.array(V)
    for line in open(p):
        if line.startswith('f '):
            idx = [int(t.split('/')[0]) - 1 for t in line.split()[1:]]
            a, b, c = V[idx[0]], V[idx[1]], V[idx[2]]
            nfr += 1
            if np.linalg.norm(np.cross(b - a, c - a)) / 2 < 1e-10:
                deg += 1
    mtl = os.path.join(PK, name + '.mtl')
    maps = [l.split()[1] for l in open(mtl) if l.startswith('map_Kd')]
    for m in maps:
        if not os.path.exists(os.path.join(PK, m)):
            errors.append('%s.mtl references missing %s' % (name, m))
    if ('**%s**' % name) not in readme:
        errors.append('README has no row for ' + name)
    if name not in lic:
        errors.append('LICENSE does not mention ' + name)
    if name in ('PottedPlant',) and deg:
        errors.append('%s has %d zero-area triangles' % (name, deg))
    m = S['models'].get(f)
    if m is None or m['tris'] != nfr:
        errors.append('tris mismatch %s: doc %s, file %d' % (f, m and m['tris'], nfr))
    tot_tris += nfr
    print('  %-26s tris %6d (trimesh %6d) zero-area %4d maps %s' % (name, nfr, nf, deg, ','.join(maps)))
total = sum(os.path.getsize(os.path.join(PK, f)) for f in files)
print('files %d, images %d, OBJ tris total %d, total bytes %d (%.2f MB), largest %s' % (
    len(files), n_img, tot_tris, total, total / 1e6, max(files, key=lambda f: os.path.getsize(os.path.join(PK, f)))))
if total > 30e6:
    errors.append('over the 30 MB budget')
print('ERRORS:' if errors else 'OK, no errors')
for e in errors:
    print('  ' + e)
