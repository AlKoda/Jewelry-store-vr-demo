"""scatter_groups.py in.obj out.obj seed spread_x spread_z : re-lay each OBJ group (rotated about Y, moved) without overlaps."""
import sys, math
import numpy as np
src, dst, seed, sx, sz = sys.argv[1], sys.argv[2], int(sys.argv[3]), float(sys.argv[4]), float(sys.argv[5])
lines = open(src).read().splitlines()
V = np.array([[float(x) for x in l.split()[1:4]] for l in lines if l.startswith('v ')])
grp_of = np.full(len(V), -1); g = -1; names = []
for l in lines:
    if l.startswith('g '): g += 1; names.append(l)
    elif l.startswith('f '):
        for c in l.split()[1:]: grp_of[int(c.split('/')[0]) - 1] = g
rng = np.random.default_rng(seed)
def rect2d(P):
    lo, hi = P[:, [0, 2]].min(0), P[:, [0, 2]].max(0)
    return np.array([lo, (hi[0], lo[1]), hi, (lo[0], hi[1])])
def sat(A, B):
    for poly in (A, B):
        for i in range(4):
            e = poly[(i + 1) % 4] - poly[i]; n = np.array([-e[1], e[0]])
            pa, pb = A @ n, B @ n
            if pa.max() < pb.min() or pb.max() < pa.min(): return False
    return True
placed = []   # (corners2d, top_y)
for k in rng.permutation(g + 1):
    idx = grp_of == k
    P = V[idx].copy(); c = (P.min(0) + P.max(0)) / 2
    Q = P - np.array([c[0], P[:, 1].min(), c[2]])
    loc = rect2d(Q)
    best = None
    for _ in range(300):
        a = rng.uniform(0, 2 * math.pi); ca, sa = math.cos(a), math.sin(a)
        R2 = np.array([[ca, sa], [-sa, ca]])
        pos = np.array([rng.uniform(-sx / 2, sx / 2), rng.uniform(-sz / 2, sz / 2)])
        cor = loc @ R2.T + pos
        hits = [t for poly, t in placed if sat(cor, poly)]
        if len(hits) <= 1 and (best is None or len(hits) < best[0]):
            best = (len(hits), a, pos, cor, max(hits) if hits else 0.0)
            if not hits and rng.random() < 0.6: break
    _, a, pos, cor, base = best
    ca, sa = math.cos(a), math.sin(a)
    R = np.array([[ca, 0, sa], [0, 1, 0], [-sa, 0, ca]])
    Vn = Q @ R.T + np.array([pos[0], base + (0.0008 if base > 0 else 0), pos[1]])
    if base > 0:  # resting on another pad: tilt slightly so it does not float flat
        Vn[:, 1] += 0.004 * (Vn[:, 0] - pos[0])
    V[idx] = Vn
    placed.append((cor, Vn[:, 1].max()))
V[:, 1] -= V[:, 1].min()
V[:, [0, 2]] -= (V[:, [0, 2]].min(0) + V[:, [0, 2]].max(0)) / 2
vi = 0; out = []
for l in lines:
    if l.startswith('v '):
        out.append('v %.5f %.5f %.5f' % tuple(V[vi])); vi += 1
    else:
        out.append(l)
open(dst, 'w').write('\n'.join(out) + '\n')
print('extent', np.round(V.max(0) - V.min(0), 3))
