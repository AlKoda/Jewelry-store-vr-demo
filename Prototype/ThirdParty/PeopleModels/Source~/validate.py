"""Validate the PeopleModels pack: every OBJ loads, winding agrees with normals, textures open."""
import glob, os, sys, json
import numpy as np
import trimesh
from PIL import Image
pack = sys.argv[1]
report = {}
for f in sorted(glob.glob(os.path.join(pack, '*.obj'))):
    # raw parse for winding check
    V, N, F = [], [], []
    mtls = set()
    for line in open(f):
        if line.startswith('v '): V.append([float(x) for x in line.split()[1:4]])
        elif line.startswith('vn '): N.append([float(x) for x in line.split()[1:4]])
        elif line.startswith('usemtl'): mtls.add(line.split()[1])
        elif line.startswith('f '):
            F.append([tuple(int(i) - 1 for i in c.split('/')) for c in line.split()[1:]])
    V, N = np.array(V), np.array(N)
    tri_v = np.array([[c[0] for c in t] for t in F]); tri_n = np.array([[c[2] for c in t] for t in F])
    fn = np.cross(V[tri_v[:, 1]] - V[tri_v[:, 0]], V[tri_v[:, 2]] - V[tri_v[:, 0]])
    agree = ((fn * N[tri_n].sum(1)).sum(1) > 0).mean()
    m = trimesh.load(f, force='mesh')
    lo, hi = V.min(0), V.max(0)
    # facing: centroid of the top 12 cm (head) vs the nose direction is not available here; report bounds
    mtl = f[:-4] + '.mtl'
    maps = [l.split()[1] for l in open(mtl) if l.startswith('map_Kd')]
    missing = [x for x in maps if not os.path.exists(os.path.join(pack, x))]
    report[os.path.basename(f)] = {'tris': len(F), 'trimesh_faces': len(m.faces), 'winding_ok': round(float(agree), 4),
                                   'size_m': (hi - lo).round(3).tolist(), 'min_y': round(float(lo[1]), 4),
                                   'center_xz': [round(float((lo[0] + hi[0]) / 2), 4), round(float((lo[2] + hi[2]) / 2), 4)],
                                   'materials': sorted(mtls), 'missing_maps': missing}
for f in sorted(glob.glob(os.path.join(pack, '*.jpg')) + glob.glob(os.path.join(pack, '*.png'))):
    im = Image.open(f); im.load()
    report[os.path.basename(f)] = {'size': im.size, 'mode': im.mode}
print(json.dumps(report, indent=1))
