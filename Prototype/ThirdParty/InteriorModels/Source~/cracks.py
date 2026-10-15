"""cracks.py OBJ [GLTF]: open edges after welding vertices by position (mm grid). Edges used once are holes or
outlines; compare the shipped OBJ with its glTF source to see whether decimation opened cracks along UV seams."""
import sys, numpy as np
def open_edges(P, F, tol=1e-4):
    key = np.round(P / tol).astype(np.int64); _, inv = np.unique(key, axis=0, return_inverse=True); inv = inv.ravel(); Fw = inv[F]
    ok = (Fw[:, 0] != Fw[:, 1]) & (Fw[:, 1] != Fw[:, 2]) & (Fw[:, 0] != Fw[:, 2]); Fw = Fw[ok]
    e = np.sort(np.vstack([Fw[:, [0, 1]], Fw[:, [1, 2]], Fw[:, [2, 0]]]), 1); ue, cnt = np.unique(e, axis=0, return_counts=True)
    return int((cnt == 1).sum())
V, F = [], []
for l in open(sys.argv[1]):
    p = l.split()
    if not p: continue
    if p[0] == 'v': V.append([float(x) for x in p[1:4]])
    elif p[0] == 'f': F.append([int(c.split('/')[0]) - 1 for c in p[1:]])
out = open_edges(np.array(V), np.array(F))
msg = '%s: open edges %d' % (sys.argv[1].split('/')[-1], out)
if len(sys.argv) > 2:
    import trimesh
    sc = trimesh.load(sys.argv[2], force='scene', process=False)
    ms = sc.dump(concatenate=False); P = np.vstack([m.vertices for m in ms]); off = np.cumsum([0] + [len(m.vertices) for m in ms])[:-1]
    Fs = np.vstack([np.asarray(m.faces) + o for m, o in zip(ms, off)])
    msg += ' | source open edges %d' % open_edges(P, Fs)
print(msg)
