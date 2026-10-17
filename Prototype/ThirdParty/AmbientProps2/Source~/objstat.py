import sys, glob, os, json
from PIL import Image
res = {}
for f in sorted(glob.glob(sys.argv[1] + '/*.obj')):
    vs = []; tris = 0; mtl = None
    for l in open(f):
        if l.startswith('v '): vs.append(tuple(map(float, l.split()[1:4])))
        elif l.startswith('f '): tris += len(l.split()) - 3
        elif l.startswith('mtllib'): mtl = l.split(None, 1)[1].strip()
    mn = [min(v[i] for v in vs) for i in range(3)]; mx = [max(v[i] for v in vs) for i in range(3)]
    maps = []
    for l in open(os.path.join(os.path.dirname(f), mtl)):
        p = l.split()
        if len(p) >= 2 and (p[0] == 'map_Kd' or (p[0] == '#' and p[1] in ('Normal', 'MetallicSmoothness'))): maps.append(p[-1])
    px = max([max(Image.open(os.path.join(os.path.dirname(f), m)).size) for m in maps] or [0])
    res[os.path.basename(f)[:-4]] = {'tris': tris, 'size_m': [round(mx[i] - mn[i], 3) for i in range(3)], 'min_y': round(mn[1], 4), 'maps': sorted(set(maps)), 'max_px': px}
json.dump(res, open(sys.argv[2], 'w'), indent=1)
for k, v in res.items(): print(k, v['tris'], v['size_m'], v['min_y'], v['max_px'], len(v['maps']))
