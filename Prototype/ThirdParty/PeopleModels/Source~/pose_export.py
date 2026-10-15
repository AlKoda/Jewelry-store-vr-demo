"""Pose a Microsoft Rocketbox avatar with one frame of a Rocketbox animation and bake it to a static OBJ.

Run with the bpy module (Blender 5.2 as a Python module):
    python -I pose_export.py -- config.json

config.json: {"avatar": fbx, "anim": fbx, "frame": int, "out": dir, "name": str, "yaw_extra": deg,
              "materials": {"m172_body": "Body", ...}, "props": [...], "double_sided": ["Hair"]}
Writes <out>/<name>.obj (+ .mtl, metres, Y up, facing +Z, feet on y = 0, centred in x/z) and a
labels file <out>/<name>_facelabels.npy (dominant bone per triangle) used by the texture step.
"""
import bpy, sys, json, math, os
import numpy as np
from mathutils import Matrix, Vector

cfg = json.load(open(sys.argv[sys.argv.index('--') + 1]))
BODY_BONES = ('Pelvis', 'Spine', 'Neck', 'Head', 'Clavicle', 'UpperArm', 'Forearm', 'Hand', 'Finger',
              'Thigh', 'Calf', 'Foot', 'Toe')

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


def imported(fn, **kw):
    before = set(bpy.data.objects)
    fn(**kw)
    return [o for o in bpy.data.objects if o not in before]


av_objs = imported(bpy.ops.import_scene.fbx, filepath=cfg['avatar'], use_anim=False)
av_arm = [o for o in av_objs if o.type == 'ARMATURE'][0]
av_mesh = [o for o in av_objs if o.type == 'MESH'][0]

# Facing in the rest pose: the nose bone lies in front of the head bone.
def bone_world(arm, name):
    pb = arm.pose.bones[name]
    return arm.matrix_world @ pb.matrix

bpy.context.view_layer.update()
rest_head = bone_world(av_arm, 'Bip01 Head').translation
rest_nose = bone_world(av_arm, 'Bip01 MNose').translation
rest_fwd_world = (rest_nose - rest_head)
rest_fwd_world.z = 0
rest_fwd_world.normalize()
pelvis_rest = bone_world(av_arm, 'Bip01 Pelvis').to_3x3()
fwd_local = pelvis_rest.inverted() @ rest_fwd_world  # forward expressed in the pelvis frame

if cfg.get('anim'):
    an_objs = imported(bpy.ops.import_scene.fbx, filepath=cfg['anim'], use_anim=True)
    an_arm = [o for o in an_objs if o.type == 'ARMATURE'][0]
    for pb in av_arm.pose.bones:
        short = pb.name.replace('Bip01 ', '')
        if not any(short.startswith(k) or (' ' + k) in pb.name for k in BODY_BONES):
            continue
        if pb.name not in an_arm.pose.bones:
            continue
        c = pb.constraints.new('COPY_ROTATION')
        c.target, c.subtarget = an_arm, pb.name
        c.owner_space = c.target_space = 'WORLD'
        if pb.name == 'Bip01 Pelvis':
            c2 = pb.constraints.new('COPY_LOCATION')
            c2.target, c2.subtarget = an_arm, pb.name
            c2.owner_space = c2.target_space = 'WORLD'
    print('RANGE', [tuple(a.frame_range) for a in bpy.data.actions if a.name.startswith('Bip01|')])
    scene.frame_set(int(cfg['frame']))
bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()

# Facing after posing, from the pelvis frame (or an explicit yaw override).
pelvis_now = bone_world(av_arm, 'Bip01 Pelvis').to_3x3()
fwd = pelvis_now @ fwd_local
fwd_obj = np.array([fwd.x, fwd.z, -fwd.y])  # Blender Z-up -> OBJ Y-up
yaw = math.atan2(fwd_obj[0], fwd_obj[2])   # angle of forward from +Z about +Y
yaw_fix = -yaw + math.radians(cfg.get('yaw_extra', 0))
cy, sy = math.cos(yaw_fix), math.sin(yaw_fix)
R = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]])

ev = av_mesh.evaluated_get(dg)
me = ev.to_mesh()
mw = np.array(av_mesh.matrix_world)


def to_obj(p):
    p = np.atleast_2d(np.asarray(p, float))
    return np.c_[p[:, 0], p[:, 2], -p[:, 1]] @ R.T


co = np.array([v.co[:] for v in me.vertices])
co = to_obj((np.c_[co, np.ones(len(co))] @ mw.T)[:, :3])
nrm_m = np.linalg.inv(np.array(mw[:3, :3])).T
cn = to_obj(np.array([n.vector[:] for n in me.corner_normals]) @ nrm_m.T)
cn /= np.linalg.norm(cn, axis=1, keepdims=True) + 1e-12
uv = np.array([d.uv[:] for d in me.uv_layers.active.data])
lo, hi = co.min(0), co.max(0)
shift = np.array([(lo[0] + hi[0]) / 2, lo[1], (lo[2] + hi[2]) / 2])
co -= shift


def bpos(name):
    return to_obj(np.array((av_arm.matrix_world @ av_arm.pose.bones[name].head)[:]))[0] - shift


def palm(side):
    h, f0, f1, f2, f4 = (bpos('Bip01 %s %s' % (side, b)) for b in ('Hand', 'Finger0', 'Finger1', 'Finger2', 'Finger4'))
    y = f2 - h; y /= np.linalg.norm(y)
    x = f1 - f4; x -= y * x.dot(y); x /= np.linalg.norm(x)
    z = np.cross(x, y)
    curl = sum((bpos('Bip01 %s Finger%d2' % (side, k)) - bpos('Bip01 %s Finger%d' % (side, k))).dot(z) for k in (1, 2, 3, 4))
    if curl < 0:
        z = -z; x = -x
    origin = 0.5 * h + 0.5 * (f1 + f2 + f4) / 3
    return origin, np.stack([x, y, z], 1)  # columns: across palm, along fingers, palm normal


def euler(deg):
    rx, ry, rz = np.radians(deg)
    X = np.array([[1, 0, 0], [0, np.cos(rx), -np.sin(rx)], [0, np.sin(rx), np.cos(rx)]])
    Y = np.array([[np.cos(ry), 0, np.sin(ry)], [0, 1, 0], [-np.sin(ry), 0, np.cos(ry)]])
    Z = np.array([[np.cos(rz), -np.sin(rz), 0], [np.sin(rz), np.cos(rz), 0], [0, 0, 1]])
    return Z @ Y @ X


sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import props as P
prop_parts = []
for pr in cfg.get('props', []):
    if pr['anchor'].startswith('palm:'):
        o, B = palm(pr['anchor'][5:])
    elif pr['anchor'].startswith('pinch:'):
        sd = pr['anchor'][6:]
        _, B = palm(sd)
        o = 0.5 * (bpos('Bip01 %s Finger02' % sd) + bpos('Bip01 %s Finger12' % sd))
    elif pr['anchor'] == 'face':
        hd = bpos('Bip01 Head'); ns = bpos('Bip01 MNose')
        f = ns - hd; f[1] = 0; f /= np.linalg.norm(f)
        B = np.stack([np.cross([0, 1, 0], f), [0, 1, 0], f], 1)  # x left->right? y up, z forward
        o = ns
    Mrot = B @ euler(pr.get('rot', [0, 0, 0]))
    off = B @ np.array(pr.get('offset', [0, 0, 0]))
    if pr.get('aim_face'):
        # local +y towards the eyes (between nose and head bone), local +z along the fingers
        eye = 0.5 * (bpos('Bip01 MNose') + bpos('Bip01 Head')) + np.array([0, 0.03, 0])
        ay = eye - (o + off); ay /= np.linalg.norm(ay)
        az = B[:, 1] - ay * B[:, 1].dot(ay); az /= np.linalg.norm(az)
        Mrot = np.stack([np.cross(ay, az), ay, az], 1) @ euler(pr.get('aim_rot', [0, 0, 0]))
    for mat, v, puv, pn, t in P.BUILDERS[pr['kind']]():
        v = (v * pr.get('scale', 1.0)) @ Mrot.T + o + off
        pn = pn @ Mrot.T
        # make the winding agree with the normals
        fn = np.cross(v[t[:, 1]] - v[t[:, 0]], v[t[:, 2]] - v[t[:, 0]])
        bad = (fn * (pn[t[:, 0]] + pn[t[:, 1]] + pn[t[:, 2]])).sum(1) < 0
        t = t.copy(); t[bad] = t[bad][:, [0, 2, 1]]
        prop_parts.append((mat, v, puv, pn, t))

# Dominant bone per polygon (for texture masks).
groups = {vg.index: vg.name for vg in av_mesh.vertex_groups}
vbone = []
for v in av_mesh.data.vertices:
    best = max(v.groups, key=lambda g: g.weight, default=None)
    vbone.append(groups[best.group] if best else '')

matmap = cfg['materials']
os.makedirs(cfg['out'], exist_ok=True)
name = cfg['name']
# Collect everything as corner lists: body corners reference me.loops; props append their own.
all_v = [co]
all_uv = [uv]
all_n = [cn]
faces = {}  # material -> list of (vi, ti, ni) triples
labels = []
for p in me.polygons:
    mname = me.materials[p.material_index].name if me.materials[p.material_index] else 'mat'
    out_name = matmap.get(mname, mname)
    if out_name is None:
        continue
    idx = list(p.loop_indices)
    vs = [me.loops[i].vertex_index for i in idx]
    for k in range(1, len(idx) - 1):
        tri = (0, k, k + 1)
        faces.setdefault(out_name, []).append([(vs[t], idx[t], idx[t]) for t in tri])
        bones = [vbone[vs[t]] for t in tri]
        # majority bone; a three-way tie goes to the first corner (deterministic, unlike set order)
        labels.append((out_name, max(bones, key=bones.count), [[round(float(x), 5) for x in uv[idx[t]]] for t in tri]))
nv, nt = len(co), len(uv)
for mat, v, puv, pn, t in prop_parts:
    all_v.append(v); all_uv.append(puv); all_n.append(pn)
    for tri in t:
        faces.setdefault(mat, []).append([(nv + i, nt + i, nt + i) for i in tri])
    nv += len(v); nt += len(v)
V = np.vstack(all_v); UV = np.vstack(all_uv); N = np.vstack(all_n)
# Double-sided materials (alpha-tested hair cards): the Built-in Standard shader culls back faces,
# so append a reversed copy of each triangle with negated normals.
neg_base = len(N)
if cfg.get('double_sided'):
    N = np.vstack([N, -cn])
    for m in cfg['double_sided']:
        if m in faces:
            faces[m] = faces[m] + [[(a, b, neg_base + c) for (a, b, c) in tri[::-1]] for tri in faces[m]]
N /= np.linalg.norm(N, axis=1, keepdims=True) + 1e-12
uv_u, uv_i = np.unique(np.round(UV, 6), axis=0, return_inverse=True)
n_u, n_i = np.unique(np.round(N, 3), axis=0, return_inverse=True)
v_u, v_i = np.unique(np.round(V, 5), axis=0, return_inverse=True)
uv_i, n_i, v_i = uv_i.ravel(), n_i.ravel(), v_i.ravel()
lines = ['# %s: Microsoft Rocketbox avatar (MIT), posed and baked by the PeopleModels pack' % name,
         '# metres, Y up, facing +Z, feet on y = 0', 'mtllib %s.mtl' % name]
lines += ['v %.5f %.5f %.5f' % tuple(p) for p in v_u]
lines += ['vt %.6f %.6f' % tuple(t) for t in uv_u]
lines += ['vn %.3f %.3f %.3f' % tuple(n) for n in n_u]
tri_count = 0
order = [m for m in matmap.values() if m in faces] + [m for m in faces if m not in matmap.values()]
for m in order:
    lines += ['usemtl %s' % m, 'g %s' % m]
    for tri in faces[m]:
        lines.append('f ' + ' '.join('%d/%d/%d' % (v_i[a] + 1, uv_i[b] + 1, n_i[c] + 1) for a, b, c in tri))
        tri_count += 1
open(os.path.join(cfg['out'], name + '.obj'), 'w').write('\n'.join(lines) + '\n')
json.dump(labels, open(os.path.join(cfg['out'], name + '_labels.json'), 'w'))
info = {'tris': tri_count, 'verts': len(v_u), 'size_m': (v_u.max(0) - v_u.min(0)).round(4).tolist(),
        'min_y': float(v_u[:, 1].min()), 'palmL': palm('L')[0].round(3).tolist(), 'palmR': palm('R')[0].round(3).tolist(), 'palmRaxes': palm('R')[1].T.round(2).tolist(), 'palmLaxes': palm('L')[1].T.round(2).tolist(), 'nose': bpos('Bip01 MNose').round(3).tolist(), 'yaw_fix_deg': math.degrees(yaw_fix), 'materials': order}
json.dump(info, open(os.path.join(cfg['out'], name + '_info.json'), 'w'), indent=1)
print('INFO', json.dumps(info))
ev.to_mesh_clear()
