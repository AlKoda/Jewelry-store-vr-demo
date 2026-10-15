"""Remove zero-area triangles (area < 1e-10 m2, i.e. collinear or coincident corners, as written in the OBJ) from OBJ files
in place. Only 'f' lines are dropped; v/vt/vn lines and groups are kept, so all indices stay valid.
usage: python3 -P strip_degenerate.py OUT.json file.obj [...]"""
import sys, json
import numpy as np

out, paths = sys.argv[1], sys.argv[2:]
report = {}
for p in paths:
    lines = open(p).read().split('\n')
    V = []
    keep, removed, total = [], 0, 0
    for line in lines:
        if line.startswith('v '):
            V.append([float(x) for x in line.split()[1:4]])
        if line.startswith('f '):
            total += 1
            idx = [int(t.split('/')[0]) for t in line.split()[1:]]
            assert all(i > 0 for i in idx) and len(idx) == 3, p
            a, b, c = (np.array(V[i - 1]) for i in idx)
            if np.linalg.norm(np.cross(b - a, c - a)) / 2 < 1e-10:
                removed += 1
                continue
        keep.append(line)
    if removed:
        open(p, 'w').write('\n'.join(keep))
    report[p.rsplit('/', 1)[-1][:-4]] = dict(before=total, after=total - removed, removed=removed)
json.dump(report, open(out, 'w'), indent=1)
for k, v in report.items():
    if v['removed']:
        print('%-26s %6d -> %6d (%d zero-area removed)' % (k, v['before'], v['after'], v['removed']))
