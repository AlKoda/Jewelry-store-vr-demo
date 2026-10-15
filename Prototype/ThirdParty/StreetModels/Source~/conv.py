"""glTF -> OBJ/MTL + Color/Normal/MetallicSmoothness maps (ForensicModels/PBRExterior convention).

Color      sRGB JPG (base colour x factor, optional AO baked in), map_Kd in the MTL
Normal     tangent-space OpenGL +Y JPG
MetallicSmoothness  PNG RGBA, R = metallic, G = B = 0, A = smoothness (1 - roughness)
"""
import os, re, json, hashlib
import numpy as np
import trimesh
from PIL import Image

Image.MAX_IMAGE_PIXELS = None


def safe(t):
    return re.sub(r'[^A-Za-z0-9]+', '_', str(t)).strip('_') or 'mat'


def load_parts(path, select=None, exclude=None, extra_transform=None):
    """Return list of (node_name, mesh(world), material). select/exclude: callables on node name."""
    scene = trimesh.load(path, force='scene', process=False)
    parts = []
    for node in scene.graph.nodes_geometry:
        mat4, gname = scene.graph[node]
        if select and not select(node):
            continue
        if exclude and exclude(node):
            continue
        g = scene.geometry[gname]
        if not isinstance(g, trimesh.Trimesh) or len(g.faces) == 0:
            continue
        m = g.copy()
        m.apply_transform(mat4)
        if extra_transform is not None:
            m.apply_transform(extra_transform)
        parts.append((node, m, getattr(m.visual, 'material', None)))
    return parts


def decimate(mesh, target):
    import fast_simplification
    from scipy.spatial import cKDTree
    if len(mesh.faces) <= target:
        return mesh
    uv = getattr(mesh.visual, 'uv', None)
    ratio = 1.0 - target / len(mesh.faces)
    pts, faces = fast_simplification.simplify(mesh.vertices.astype(np.float32), mesh.faces.astype(np.int32),
                                              target_reduction=ratio)
    new = trimesh.Trimesh(pts, faces, process=False)
    if uv is not None and len(uv) == len(mesh.vertices):
        _, idx = cKDTree(mesh.vertices).query(pts)
        new.visual = trimesh.visual.TextureVisuals(uv=uv[idx], material=mesh.visual.material)
    return new


def _img(x):
    if x is None:
        return None
    if isinstance(x, Image.Image):
        return x
    return None


def _factor(f, n=4, default=1.0):
    if f is None:
        return [default] * n
    f = [float(v) for v in f]
    if max(f) > 1.0:
        f = [v / 255.0 for v in f]
    return (f + [1.0] * n)[:n]


def write_maps(out, prefix, material, max_tex=1024, ms_tex=512, ao_strength=0.5, color_override=None,
               jpg_quality=88, alpha=False, nrm_tex=None, arm=False):
    """Write texture maps for one material. Returns dict with file names and constants."""
    info = {}
    bc = _img(getattr(material, 'baseColorTexture', None)) if material is not None else None
    fac = _factor(getattr(material, 'baseColorFactor', None)) if material is not None else [0.8, 0.8, 0.8, 1]
    mrt = _img(getattr(material, 'metallicRoughnessTexture', None)) if material is not None else None
    occ = _img(getattr(material, 'occlusionTexture', None)) if material is not None else None
    nrm = _img(getattr(material, 'normalTexture', None)) if material is not None else None
    mf = getattr(material, 'metallicFactor', None) if material is not None else None
    rf = getattr(material, 'roughnessFactor', None) if material is not None else None
    mf = 1.0 if mf is None else float(mf)
    rf = 1.0 if rf is None else float(rf)
    if mrt is None:
        mf_c = 0.0 if getattr(material, 'metallicFactor', None) is None else mf
        info['metallic'] = round(mf_c, 3)
        info['smoothness'] = round(1.0 - rf, 3)
    info['kd'] = [round(v, 4) for v in fac[:3]]
    info['alpha'] = round(fac[3], 3)
    if bc is not None:
        img = bc.convert('RGBA')
        size = img.size
        if max(size) > max_tex:
            r = max_tex / max(size)
            size = (max(1, round(size[0] * r)), max(1, round(size[1] * r)))
            img = img.resize(size, Image.LANCZOS)
        a = np.asarray(img).astype(np.float32) / 255.0
        rgb = a[..., :3] * np.array(fac[:3], np.float32)
        if color_override is not None:
            rgb = color_override(rgb)
        aomap = occ if occ is not None else (mrt if arm else None)
        if ao_strength > 0 and aomap is not None:
            ao = np.asarray(aomap.convert('RGB').resize(size, Image.LANCZOS)).astype(np.float32)[..., 0] / 255.0
            # some Poly Haven 'arm' maps carry no AO (R channel all black: metal_trash_can, modular_street_seating,
            # exterior_aircon_unit_02); baking that would halve the albedo, so skip it
            if float(ao.mean()) > 0.02:
                rgb = rgb * (1.0 - ao_strength + ao_strength * ao)[..., None]
        rgb8 = (np.clip(rgb, 0, 1) * 255 + 0.5).astype(np.uint8)
        if alpha:
            al = (a[..., 3] * fac[3] * 255 + 0.5).astype(np.uint8)
            fn = prefix + '_Color.png'
            Image.fromarray(np.dstack([rgb8, al]), 'RGBA').save(os.path.join(out, fn), optimize=True)
        else:
            fn = prefix + '_Color.jpg'
            Image.fromarray(rgb8, 'RGB').save(os.path.join(out, fn), quality=jpg_quality, optimize=True,
                                              progressive=True)
        info['color'] = fn
        info['kd'] = [1, 1, 1]
    if nrm is not None:
        img = nrm.convert('RGB')
        nt = nrm_tex or max_tex
        if max(img.size) > nt:
            r = nt / max(img.size)
            img = img.resize((max(1, round(img.size[0] * r)), max(1, round(img.size[1] * r))), Image.LANCZOS)
        fn = prefix + '_Normal.jpg'
        img.save(os.path.join(out, fn), quality=92, optimize=True)
        info['normal'] = fn
    if mrt is not None:
        img = mrt.convert('RGB')
        if max(img.size) > ms_tex:
            r = ms_tex / max(img.size)
            img = img.resize((max(1, round(img.size[0] * r)), max(1, round(img.size[1] * r))), Image.LANCZOS)
        a = np.asarray(img).astype(np.float32) / 255.0
        metal = np.clip(a[..., 2] * mf, 0, 1)
        smooth = np.clip(1.0 - a[..., 1] * rf, 0, 1)
        z = np.zeros_like(metal)
        arr = (np.dstack([metal, z, z, smooth]) * 255 + 0.5).astype(np.uint8)
        fn = prefix + '_MetallicSmoothness.png'
        Image.fromarray(arr, 'RGBA').save(os.path.join(out, fn), optimize=True)
        info['metallic_smoothness'] = fn
    return info


def write_obj(out, name, groups, ground=True, center=True, scale=1.0, rotate_y_deg=0.0, write_normals=True,
              smooth_normals=False):
    """groups: list of (group_name, material_name, mesh). Writes NAME.obj (+ mtl lines handled separately)."""
    meshes = [m for _, _, m in groups]
    allv = np.vstack([m.vertices for m in meshes]) * scale
    if rotate_y_deg:
        R = trimesh.transformations.rotation_matrix(np.radians(rotate_y_deg), [0, 1, 0])[:3, :3]
        allv = allv @ R.T
    else:
        R = np.eye(3)
    off = np.zeros(3)
    if ground:
        off[1] = allv[:, 1].min()
    if center:
        off[0] = (allv[:, 0].min() + allv[:, 0].max()) / 2
        off[2] = (allv[:, 2].min() + allv[:, 2].max()) / 2
    lines = ['# %s - converted for Unity (metres, Y up)' % name, 'mtllib %s.mtl' % name]
    vo = to = no = 0
    tris = 0
    for gname, mname, m in groups:
        v = (m.vertices * scale) @ R.T - off
        uv = getattr(m.visual, 'uv', None)
        if uv is not None and len(uv) != len(m.vertices):
            uv = None
        lines.append('g %s' % safe(gname))
        lines.append('usemtl %s' % mname)
        lines += ['v %.4f %.4f %.4f' % tuple(p) for p in v]
        if uv is not None:
            lines += ['vt %.4f %.4f' % (t[0], t[1]) for t in uv]
        if write_normals:
            n = m.vertex_normals @ R.T
            lines += ['vn %.3f %.3f %.3f' % tuple(p) for p in n]
        for a, b, c in m.faces:
            a1, b1, c1 = a + 1, b + 1, c + 1
            if uv is not None and write_normals:
                lines.append('f %d/%d/%d %d/%d/%d %d/%d/%d' % (a1 + vo, a1 + to, a1 + no, b1 + vo, b1 + to, b1 + no,
                                                               c1 + vo, c1 + to, c1 + no))
            elif uv is not None:
                lines.append('f %d/%d %d/%d %d/%d' % (a1 + vo, a1 + to, b1 + vo, b1 + to, c1 + vo, c1 + to))
            elif write_normals:
                lines.append('f %d//%d %d//%d %d//%d' % (a1 + vo, a1 + no, b1 + vo, b1 + no, c1 + vo, c1 + no))
            else:
                lines.append('f %d %d %d' % (a1 + vo, b1 + vo, c1 + vo))
        vo += len(v)
        if uv is not None:
            to += len(uv)
        if write_normals:
            no += len(v)
        tris += len(m.faces)
    with open(os.path.join(out, name + '.obj'), 'w') as fh:
        fh.write('\n'.join(lines) + '\n')
    allv2 = allv - off
    return dict(tris=int(tris), size_m=[round(float(x), 3) for x in (allv2.max(0) - allv2.min(0))],
                min_y=round(float(allv2[:, 1].min()), 4))


def write_mtl(out, name, mats):
    """mats: list of (mtl_name, info)"""
    blocks = []
    for mname, info in mats:
        kd = info.get('kd', [1, 1, 1])
        b = ['newmtl %s' % mname, 'Kd %.4f %.4f %.4f' % tuple(kd), 'Ks 0.04 0.04 0.04', 'd %.3f' % info.get('alpha', 1.0)]
        if info.get('color'):
            b.append('map_Kd %s' % info['color'])
        blocks.append('\n'.join(b))
    with open(os.path.join(out, name + '.mtl'), 'w') as fh:
        fh.write('\n\n'.join(blocks) + '\n')


def sha256(p):
    return hashlib.sha256(open(p, 'rb').read()).hexdigest()


def concat(meshes):
    """Concatenate meshes keeping UVs (zeros where missing)."""
    vs, fs, uvs, ns = [], [], [], []
    off = 0
    for m in meshes:
        vs.append(np.asarray(m.vertices)); fs.append(np.asarray(m.faces) + off)
        uv = getattr(m.visual, 'uv', None)
        uvs.append(np.asarray(uv) if uv is not None and len(uv) == len(m.vertices) else np.zeros((len(m.vertices), 2)))
        ns.append(np.asarray(m.vertex_normals))
        off += len(m.vertices)
    out = trimesh.Trimesh(np.vstack(vs), np.vstack(fs), vertex_normals=np.vstack(ns), process=False)
    out.visual = trimesh.visual.TextureVisuals(uv=np.vstack(uvs))
    return out


def weld(m, decimals=5, normals=True):
    """Merge vertices that share position, uv and (optionally) normal (keeps UV seams)."""
    uv = getattr(m.visual, 'uv', None)
    n = np.asarray(m.vertex_normals)
    key = [np.round(m.vertices, decimals)] + ([np.round(n, 3)] if normals else [])
    if uv is not None and len(uv) == len(m.vertices):
        key.append(np.round(uv, 5))
    key = np.hstack(key)
    _, idx, inv = np.unique(key, axis=0, return_index=True, return_inverse=True)
    inv = inv.reshape(-1)
    out = trimesh.Trimesh(m.vertices[idx], inv[m.faces], vertex_normals=n[idx], process=False)
    if uv is not None and len(uv) == len(m.vertices):
        out.visual = trimesh.visual.TextureVisuals(uv=np.asarray(uv)[idx])
    return out
