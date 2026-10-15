"""Assemble a 5-bay, 4-storey apartment block from Poly Haven's Modular Urban Apartments Facade (CC0).
usage: build_apartment.py SIMPLIFIED_KIT.gltf ORIGINAL_KIT.gltf OUTDIR"""
import sys, os, json, re
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
import trimesh
from PIL import Image, ImageDraw, ImageFilter
import conv

kit, orig, out = sys.argv[1:4]
os.makedirs(out, exist_ok=True)
NAME = 'ApartmentBlock'
gj = json.load(open(kit))
T = {n['name']: np.array(n.get('translation', [0, 0, 0]), float) for n in gj['nodes'] if 'name' in n}
scene = trimesh.load(kit, force='scene', process=False)
graph_nodes = list(scene.graph.nodes_geometry)
MATS = {}


def module(name):
    parts = []
    for gn in graph_nodes:
        if gn == name or re.fullmatch(re.escape(name) + r'_[0-9a-f]{6}', gn):
            M, g = scene.graph[gn]
            m = scene.geometry[g].copy()
            m.apply_transform(M)
            m.apply_translation(-T[name])
            mat = m.visual.material
            MATS[mat.name] = mat
            parts.append((mat.name, m))
    assert parts, name
    return parts


placed = []  # (matname, mesh)


def put(name, x, y, z=0.0):
    for mn, m in module(name):
        m = m.copy(); m.apply_translation([x, y, z]); placed.append((mn, m))


BAYS = 5
W = 3.0
G = 0.15                 # ground-floor level above the pavement (base is partly buried)
FLOORS = 4
TOP = G + FLOORS * 3.0
ground = ['window_centered_double_01', 'window_centered_double_01', 'door_centered_large_01',
          'window_centered_double_01', 'window_centered_double_01']
wall_for = lambda w: 'wall_' + w
upper = {1: ['window_centered_small_01', 'window_centered_double_01', 'window_centered_large_01', 'window_centered_double_01', 'window_centered_small_01'],
         2: ['window_centered_small_02', 'window_centered_double_02', 'window_centered_large_02', 'window_centered_double_02', 'window_centered_small_02'],
         3: ['window_centered_small_03', 'window_centered_double_03', 'window_centered_large_03', 'window_centered_double_03', 'window_centered_small_03']}
wall_name = {'door_centered_large_01': 'wall_door_centered_large_01'}
for i in range(BAYS):
    x = W * (i + 1)
    put('base_standard_01', x, G - 0.75)
    w = ground[i]
    put(wall_name.get(w, 'wall_' + w), x, G)
    put(w, x, G)
    for k in range(1, FLOORS):
        w = upper[k][i]
        put('wall_' + w, x, G + 3 * k)
        put(w, x, G + 3 * k)
        put('cornice_standard_standard_01', x, G + 3 * k - 0.1)
    put('crown_standard_standard_01', x, TOP)
for k in range(FLOORS):
    put('wall_pier_standard_01', 0.0, G + 3 * k)
    put('wall_pier_standard_01', BAYS * W, G + 3 * k)
    if k:
        put('cornice_pier_standard_01', 0.0, G + 3 * k - 0.1)
        put('cornice_pier_standard_01', BAYS * W, G + 3 * k - 0.1)
put('crown_pier_pedestal_01', 0.0, TOP)
put('crown_pier_pedestal_01', BAYS * W, TOP)
put('base_end_01', 0.0, G - 0.75)
put('base_end_02', BAYS * W, G - 0.75)

# ---------------------------------------------------------------- shell (sides, back, roof) in plaster
wall_ref = module('wall_standard_standard_01')[0][1]
uv = wall_ref.visual.uv
uv_per_m = (uv[:, 0].max() - uv[:, 0].min()) / 3.0, (uv[:, 1].max() - uv[:, 1].min()) / 3.0
DEPTH = 9.0
x0, x1 = -0.13, BAYS * W + 0.13
yb, yt = G - 0.6, TOP + 0.6


def quad(p0, p1, p2, p3, uvs):
    v = np.array([p0, p1, p2, p3], float)
    f = np.array([[0, 1, 2], [0, 2, 3]])
    m = trimesh.Trimesh(v, f, process=False)
    m.visual = trimesh.visual.TextureVisuals(uv=np.array(uvs, float))
    return m


def wall_uv(a, b, h0, h1):  # a,b horizontal metres along wall
    su, sv = uv_per_m
    return [(a * su, h0 * sv), (b * su, h0 * sv), (b * su, h1 * sv), (a * su, h1 * sv)]


plaster = 'modular_urban_apartments_facade_plaster'
# left side wall faces -x, right faces +x, back faces -z
placed.append((plaster, quad((x0, yb, -DEPTH), (x0, yb, 0.0), (x0, yt, 0.0), (x0, yt, -DEPTH), wall_uv(0, DEPTH, yb, yt))))
placed.append((plaster, quad((x1, yb, 0.0), (x1, yb, -DEPTH), (x1, yt, -DEPTH), (x1, yt, 0.0), wall_uv(0, DEPTH, yb, yt))))
placed.append((plaster, quad((x1, yb, -DEPTH), (x0, yb, -DEPTH), (x0, yt, -DEPTH), (x1, yt, -DEPTH), wall_uv(0, x1 - x0, yb, yt))))
roof_y = TOP + 0.55
placed.append(('modular_urban_apartments_facade_trim_02',
               quad((x0, roof_y, 0.0), (x1, roof_y, 0.0), (x1, roof_y, -DEPTH), (x0, roof_y, -DEPTH),
                    [(0.02, 0.6), (0.98, 0.6), (0.98, 0.62), (0.02, 0.62)])))

# ---------------------------------------------------------------- lit/unlit room backdrops (project-made texture)
ROOM_Z = -1.4
COLS, ROWS = BAYS, FLOORS
for k in range(FLOORS):
    y0 = G + 3 * k
    v0, v1 = 1 - (k + 1) / ROWS, 1 - k / ROWS
    placed.append(('Interior', quad((0, y0, ROOM_Z), (BAYS * W, y0, ROOM_Z), (BAYS * W, y0 + 3, ROOM_Z), (0, y0 + 3, ROOM_Z),
                                    [(0, v0), (1, v0), (1, v1), (0, v1)])))
    # floor and ceiling of the room strip so oblique views do not see through
    placed.append(('Interior', quad((0, y0 + 0.02, 0.0), (BAYS * W, y0 + 0.02, 0.0), (BAYS * W, y0 + 0.02, ROOM_Z), (0, y0 + 0.02, ROOM_Z),
                                    [(0.0, v0 + 0.002), (1, v0 + 0.002), (1, v0 + 0.004), (0.0, v0 + 0.004)])))
    placed.append(('Interior', quad((0, y0 + 2.98, ROOM_Z), (BAYS * W, y0 + 2.98, ROOM_Z), (BAYS * W, y0 + 2.98, 0.0), (0, y0 + 2.98, 0.0),
                                    [(0.0, v1 - 0.004), (1, v1 - 0.004), (1, v1 - 0.002), (0.0, v1 - 0.002)])))


# The Interior colour map (ApartmentBlock_Interior_Color.jpg, same 5 x 4 cell layout as the UVs above) is made by
# make_interior.py from CC0 Poly Haven interior HDRIs (soft-focus night views); run it separately:
#   python3 -P make_interior.py <drei-assets>/hdri OUTDIR/ApartmentBlock_Interior_Color.jpg


# ---------------------------------------------------------------- write
mat_rename = {'modular_urban_apartments_facade_objects': 'WindowFrames', 'modular_urban_apartments_facade_glass': 'Glass',
              'modular_urban_apartments_facade_trim_01': 'TrimStone', 'modular_urban_apartments_facade_trim_02': 'CrownDark',
              'modular_urban_apartments_facade_plaster': 'Plaster', 'Interior': 'Interior'}
# textures come from the full-resolution 1K kit (the simplified kit references the same image files)
groups = {}
for mn, m in placed:
    groups.setdefault(mn, []).append(m)
mtl, objg = [], []
for mn, ms in groups.items():
    rn = mat_rename[mn]
    merged = conv.weld(conv.concat(ms), normals=False)
    if rn == 'Interior':
        fn = '%s_Interior_Color.jpg' % NAME   # written by make_interior.py
        info = dict(color=fn, kd=[1, 1, 1], alpha=1.0, metallic=0.0, smoothness=0.1, emission='use the colour map as emission map (x0.6 to x1.0) for lit windows at night')
    elif rn == 'Glass':
        info = dict(kd=[0.10, 0.11, 0.12], alpha=0.35, metallic=0.0, smoothness=0.95)
    else:
        mp = dict(max_tex=1024, nrm_tex=1024, ms_tex=256) if rn in ('Plaster', 'WindowFrames') else dict(max_tex=1024, nrm_tex=512, ms_tex=128)
        info = conv.write_maps(out, '%s_%s' % (NAME, rn), MATS[mn], max_tex=mp['max_tex'], ms_tex=mp['ms_tex'], nrm_tex=mp['nrm_tex'], ao_strength=0.5, arm=True)
    mtl.append((rn, info))
    objg.append((rn, rn, merged))
conv.write_mtl(out, NAME, mtl)
res = conv.write_obj(out, NAME, objg, ground=False, center=False, write_normals=False)
res['materials'] = {k: v for k, v in mtl}
logp = os.path.join(out, '.build_log.json')
log = json.load(open(logp)) if os.path.exists(logp) else {}
log[NAME] = res
json.dump(log, open(logp, 'w'), indent=1)
print(NAME, res)
