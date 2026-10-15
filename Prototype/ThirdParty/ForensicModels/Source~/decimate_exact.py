"""UV-exact decimation for a converted OBJ (v/vt per corner): decimate_exact.py in.obj out.obj target_tris
Vertices are split per (position, uv) so UV islands stay separate, borders are preserved, and the replayed
collapses keep original vertices, so every kept vertex keeps its exact UV."""
import sys
import numpy as np
import fast_simplification as fs

src, dst, target = sys.argv[1], sys.argv[2], int(sys.argv[3])
head, V, T, groups = [], [], [], []   # groups: [name, mtl, faces(list of (vi, ti) triples)]
cur = None
for line in open(src):
    p = line.split()
    if not p:
        continue
    if p[0] == 'v': V.append([float(x) for x in p[1:4]])
    elif p[0] == 'vt': T.append([float(x) for x in p[1:3]])
    elif p[0] == 'g': cur = [p[1], None, []]; groups.append(cur)
    elif p[0] == 'usemtl': cur[1] = p[1]
    elif p[0] == 'f':
        cs = [c.split('/') for c in p[1:]]
        cur[2].append([(int(c[0]) - 1, int(c[1]) - 1) for c in cs])
    elif p[0] in ('mtllib', '#'): head.append(line.rstrip('\n'))
V = np.array(V); T = np.array(T)
total = sum(len(g[2]) for g in groups)
outV, outT, body = [], [], []
for name, mtl, faces in groups:
    share = max(32, int(round(target * len(faces) / total)))
    keys = {}; pts = []; uvs = []; F = []
    for f in faces:
        tri = []
        for vi, ti in f:
            k = (vi, ti)
            if k not in keys:
                keys[k] = len(pts); pts.append(V[vi]); uvs.append(T[ti])
            tri.append(keys[k])
        F.append(tri)
    pts = np.array(pts, np.float32); uvs = np.array(uvs); F = np.array(F, np.int64)
    if len(F) > share:
        _, _, coll = fs.simplify(pts, F, target_count=share, return_collapses=True)
        P2, F2, mapping = fs.replay_simplification(pts, F, coll)
        # UV of each decimated vertex = UV of the closest original vertex merged into it (same UV island)
        orig = np.full(len(P2), -1); best = np.full(len(P2), np.inf)
        d = np.linalg.norm(pts - P2[np.clip(mapping, 0, None)], axis=1)
        for i in np.argsort(d):
            j = mapping[i]
            if j >= 0 and orig[j] < 0:
                orig[j] = i
        assert (orig >= 0).all()
        pts2, uv2, F = P2, uvs[orig], F2
    else:
        pts2, uv2 = pts, uvs
    base = len(outV)
    outV += [tuple(p) for p in pts2]; outT += [tuple(t) for t in uv2]
    body.append('g ' + name); body.append('usemtl ' + mtl)
    body += ['f %d/%d %d/%d %d/%d' % (a + 1 + base, a + 1 + base, b + 1 + base, b + 1 + base, c + 1 + base, c + 1 + base) for a, b, c in F]
    print(name, len(faces), '->', len(F))
lines = head + ['v %.5f %.5f %.5f' % v for v in outV] + ['vt %.5f %.5f' % t for t in outT] + body
open(dst, 'w').write('\n'.join(lines) + '\n')
print('total', sum(1 for l in body if l.startswith('f ')))
