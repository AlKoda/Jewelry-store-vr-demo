"""Flip the winding of PottedPlant stem triangles that meshoptimizer folded over (revision 2026-10-15).
The original Poly Haven stem (7,008 tris) has no triangle whose geometric normal opposes its authored vertex normals;
after simplifying it to 425 tris about 30 tiny slivers (0.1-10 cm2) in the twigs point inwards, so the Standard shader
culls them from outside (pin-holes in the twigs). Their winding is reversed so they face the same way as their
(outward) vertex normals. Only the 'Pot' group above the gravel (y > 0.55 m, i.e. the stem) is touched; the pot shell,
gravel disc and the double-sided leaf blades are left as they are. Positions, UVs, normals and face count are unchanged.
usage: python3 -P fix_stem_winding.py PottedPlant.obj   (rewrites the file in place)"""
import sys
import numpy as np

path = sys.argv[1]
lines = open(path).read().split('\n')
V, N = [], []
for l in lines:
    if l.startswith('v '):
        V.append([float(x) for x in l.split()[1:4]])
    elif l.startswith('vn '):
        N.append([float(x) for x in l.split()[1:4]])
V, N = np.array(V), np.array(N)
group, flipped, checked = None, 0, 0
for i, l in enumerate(lines):
    if l.startswith('g '):
        group = l.split()[1]
    if not l.startswith('f ') or group != 'Pot':
        continue
    toks = l.split()[1:]
    vi = [int(t.split('/')[0]) - 1 for t in toks]
    ni = [int(t.split('/')[2]) - 1 for t in toks]
    a, b, c = V[vi]
    if min(a[1], b[1], c[1]) <= 0.55:
        continue
    checked += 1
    fn = np.cross(b - a, c - a)
    fn /= np.linalg.norm(fn) + 1e-15
    if np.mean([fn @ N[k] for k in ni]) < 0:
        lines[i] = 'f ' + ' '.join(toks[::-1])
        flipped += 1
open(path, 'w').write('\n'.join(lines))
print('stem faces checked %d, flipped %d' % (checked, flipped))
