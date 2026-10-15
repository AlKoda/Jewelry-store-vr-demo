"""Minimal numpy reader for the plain intermediate glTF files (no sparse accessors, no extensions)."""
import json, os
import numpy as np

CT = {5126: np.float32, 5125: np.uint32, 5123: np.uint16, 5121: np.uint8}
NC = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4}


def read(path):
    g = json.load(open(path))
    bufs = [open(os.path.join(os.path.dirname(path), b['uri']), 'rb').read() for b in g['buffers']]

    def acc(i):
        a = g['accessors'][i]; bv = g['bufferViews'][a['bufferView']]
        dt = np.dtype(CT[a['componentType']]); nc = NC[a['type']]
        off = bv.get('byteOffset', 0) + a.get('byteOffset', 0)
        stride = bv.get('byteStride', dt.itemsize * nc)
        raw = np.frombuffer(bufs[bv['buffer']], np.uint8, count=stride * (a['count'] - 1) + dt.itemsize * nc, offset=off)
        if stride == dt.itemsize * nc:
            return raw.view(dt).reshape(a['count'], nc) if nc > 1 else raw.view(dt).copy()
        out = np.lib.stride_tricks.as_strided(raw, (a['count'], dt.itemsize * nc), (stride, 1))
        return np.ascontiguousarray(out).view(dt).reshape(a['count'], nc)

    parts = []
    for node in g['nodes']:
        if 'mesh' not in node:
            continue
        for p in g['meshes'][node['mesh']]['primitives']:
            at = p['attributes']
            uv = acc(at['TEXCOORD_0']).astype(np.float64).copy(); uv[:, 1] = 1.0 - uv[:, 1]  # back to OBJ/trimesh V-up
            parts.append(dict(name=node['name'], v=acc(at['POSITION']).astype(np.float64), n=acc(at['NORMAL']).astype(np.float64),
                              uv=uv, f=acc(p['indices']).astype(np.int64).reshape(-1, 3)))
    return parts
