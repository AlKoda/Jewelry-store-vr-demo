"""check_obj.py OBJ...: parse OBJ (v/vt/vn), per group: tris, faces whose geometric normal disagrees with the mean of
their vn, degenerate faces; overall bounds; also trimesh load check."""
import sys, numpy as np, trimesh
for path in sys.argv[1:]:
    V, N = [], []; groups = {}; cur = 'default'
    for line in open(path):
        p = line.split()
        if not p: continue
        if p[0] == 'v': V.append([float(x) for x in p[1:4]])
        elif p[0] == 'vn': N.append([float(x) for x in p[1:4]])
        elif p[0] in ('g', 'usemtl'): cur = p[1]
        elif p[0] == 'f':
            cs = [c.split('/') for c in p[1:]]
            groups.setdefault(cur, []).append([(int(c[0]) - 1, int(c[2]) - 1) for c in cs])
    V = np.array(V); N = np.array(N); tot = 0; out = []
    for g, fs in groups.items():
        F = np.array(fs); P = V[F[:, :, 0]]; nn = N[F[:, :, 1]].mean(1)
        gn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0]); area = np.linalg.norm(gn, axis=1)
        dis = (np.sum(gn * nn, 1) < 0) & (area > 1e-14)
        out.append('%s %d tris, %d disagree (%.2f%%), %d degenerate' % (g, len(F), dis.sum(), 100 * dis.mean(), (area <= 1e-14).sum()))
        tot += len(F)
    m = trimesh.load(path, force='mesh', process=False)
    print('%s: %d tris, bounds %s..%s size %s | trimesh faces %d' % (path.split('/')[-1], tot, V.min(0).round(3), V.max(0).round(3), (V.max(0) - V.min(0)).round(3), len(m.faces)))
    for o in out: print('   ', o)
