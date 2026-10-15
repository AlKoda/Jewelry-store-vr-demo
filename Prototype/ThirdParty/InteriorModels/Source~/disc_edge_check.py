"""disc_edge_check.py OBJ: samples the Gravel disc's outer edge densely (10 points per segment) and casts a ray from the
pot axis through every sample at its height. A sample must lie beyond the first Pot crossing (inner wall: no gap
into the cavity) and before the second (outer wall: does not poke out)."""
import sys, numpy as np
def ray_hits(o, d, T):
    e1 = T[:, 1] - T[:, 0]; e2 = T[:, 2] - T[:, 0]; p = np.cross(d, e2); det = np.sum(e1 * p, 1)
    ok = np.abs(det) > 1e-14; inv = np.where(ok, 1 / np.where(ok, det, 1), 0); s = o - T[:, 0]
    u = np.sum(s * p, 1) * inv; q = np.cross(s, e1); v = (q @ d) * inv; t = np.sum(e2 * q, 1) * inv
    m = ok & (u >= -1e-7) & (v >= -1e-7) & (u + v <= 1 + 1e-7) & (t > 1e-6)   # tolerant: rays through shared edges
    t = np.sort(t[m])
    return t[np.r_[True, np.diff(t) > 1e-5]] if len(t) else t          # one crossing per shared edge
V, groups, cur = [], {}, None
for line in open(sys.argv[1]):
    p = line.split()
    if not p: continue
    if p[0] == 'v': V.append([float(x) for x in p[1:4]])
    elif p[0] == 'usemtl': cur = p[1]
    elif p[0] == 'f': groups.setdefault(cur, []).append([int(c.split('/')[0]) - 1 for c in p[1:]])
V = np.array(V); G = np.array(groups['Gravel']); T = V[np.array(groups['Pot'])]
# boundary loop of the disc
e = np.sort(np.vstack([G[:, [0, 1]], G[:, [1, 2]], G[:, [2, 0]]]), 1); ue, cnt = np.unique(e, axis=0, return_counts=True); be = ue[cnt == 1]
bv = np.unique(be); c = V[np.unique(G)].mean(0)
# the disc centre vertex: the one with the most faces
deg = np.bincount(G.ravel()); c = V[np.argmax(deg)]
gaps, outs = [], []
for a, b in be:
    for t in np.linspace(0, 1, 10, endpoint=False):
        q = V[a] * (1 - t) + V[b] * t
        o = np.array([c[0], q[1], c[2]]); d = q - o; d[1] = 0; rq = np.linalg.norm(d); d /= rq
        h = ray_hits(o, d, T); h = h[h > 0.5 * rq]      # ignore the stem near the axis
        if len(h) == 0: gaps.append(9); outs.append(-9); continue
        gaps.append(h[0] - rq)            # > 0: sample short of the inner wall (gap)
        outs.append((h[1] - rq) if len(h) > 1 else -9)   # < 0: beyond the outer wall
gaps = np.array(gaps); outs = np.array(outs)
print('%s: %d edge samples; worst gap to inner wall %.2f mm (>0 = visible gap), samples with gap %d; min distance to outer wall %.2f mm (<0 = pokes out)'
      % (sys.argv[1].split('/')[-1], len(gaps), gaps.max() * 1000, (gaps > 0).sum(), outs.min() * 1000))
