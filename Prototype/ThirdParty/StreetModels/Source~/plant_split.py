"""Split Poly Haven potted_plant_01 into one glTF node per leaf blade / leaf stalk (plus pot and stem) so that meshoptimizer
simplifies every leaf on its own budget (a single whole-mesh pass collapsed many leaves into zero-area triangles).
The pebble layer is left out: it is replaced by a low-poly gravel disc in plant_build.py.
usage: python3 -P plant_split.py SRC.gltf OUT.gltf"""
import sys, os, json, struct
import numpy as np
import trimesh

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from comps import components

src, out = sys.argv[1], sys.argv[2]
sc = trimesh.load(src, force='scene', process=False)
parts = []
for node in sc.graph.nodes_geometry:
    T, gname = sc.graph[node]
    m = sc.geometry[gname].copy()
    m.apply_transform(T)
    if node.endswith('_pebbles'):
        continue
    uv = np.asarray(m.visual.uv, np.float32)
    nrm = np.asarray(m.vertex_normals, np.float32)
    if node.endswith('_leaves'):
        # weld on position + UV only (the source splits normals along hard edges, which blocks meshopt collapses);
        # merged vertices get the normalised mean normal, UV seams stay split
        key = np.hstack([np.round(m.vertices, 6), np.round(uv, 5)])
        _, first, inv0 = np.unique(key, axis=0, return_index=True, return_inverse=True)
        inv0 = inv0.reshape(-1)
        nsum = np.zeros((len(first), 3)); np.add.at(nsum, inv0, nrm)
        nsum /= np.maximum(np.linalg.norm(nsum, axis=1, keepdims=True), 1e-12)
        V, N, U, Fw = m.vertices[first], nsum.astype(np.float32), uv[first], inv0[m.faces]
        k, lab = components(V, Fw)
        for i in range(k):
            f = Fw[lab == i]
            vi, inv = np.unique(f, return_inverse=True)
            # blades are broad open surfaces (width/length > 0.3); stalks (petioles) are thin tubes (< 0.11)
            c = V[vi] - V[vi].mean(0)
            sv = np.linalg.svd(c, compute_uv=False)
            kind = 'blade' if sv[1] / sv[0] > 0.3 else 'stalk'
            parts.append(('%s_%03d' % (kind, i), V[vi], N[vi], U[vi], inv.reshape(-1, 3)))
    else:
        parts.append((node.replace('potted_plant_01_', ''), m.vertices, nrm, uv, m.faces))

buf = bytearray()
acc, views, meshes, nodes = [], [], [], []


def add(arr, target, comp, typ, minmax=False):
    global buf
    while len(buf) % 4:
        buf += b'\0'
    data = arr.tobytes()
    views.append(dict(buffer=0, byteOffset=len(buf), byteLength=len(data), target=target))
    buf += data
    a = dict(bufferView=len(views) - 1, componentType=comp, count=int(arr.shape[0]), type=typ)
    if minmax:
        a['min'] = arr.min(0).tolist(); a['max'] = arr.max(0).tolist()
    acc.append(a)
    return len(acc) - 1


for name, v, n, uv, f in parts:
    uvg = uv.copy(); uvg[:, 1] = 1.0 - uvg[:, 1]  # trimesh flips V on load; write glTF convention back
    p = add(np.ascontiguousarray(v, np.float32), 34962, 5126, 'VEC3', True)
    nn = add(np.ascontiguousarray(n, np.float32), 34962, 5126, 'VEC3')
    t = add(np.ascontiguousarray(uvg, np.float32), 34962, 5126, 'VEC2')
    ix = add(np.ascontiguousarray(f.reshape(-1), np.uint32), 34963, 5125, 'SCALAR')
    meshes.append(dict(name=name, primitives=[dict(attributes=dict(POSITION=p, NORMAL=nn, TEXCOORD_0=t), indices=ix)]))
    nodes.append(dict(name=name, mesh=len(meshes) - 1))

g = dict(asset=dict(version='2.0', generator='plant_split.py'), scene=0, scenes=[dict(nodes=list(range(len(nodes))))],
         nodes=nodes, meshes=meshes, accessors=acc, bufferViews=views,
         buffers=[dict(byteLength=len(buf), uri=os.path.basename(out).replace('.gltf', '.bin'))])
open(out.replace('.gltf', '.bin'), 'wb').write(bytes(buf))
json.dump(g, open(out, 'w'))
print(len(parts), 'parts;', sum(len(p[4]) for p in parts), 'tris')
