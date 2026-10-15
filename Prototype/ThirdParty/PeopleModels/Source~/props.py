"""Small hand props (project-made geometry) for the posed figures. Units: metres.

Every builder returns a list of parts: (material, verts Nx3, uvs Nx2, normals Nx3, tris Mx3), with
unshared vertices per face corner so normals and UVs stay simple. UV regions refer to the shared
atlas PeopleProps_Color.jpg made by make_props_atlas.py.
"""
import numpy as np

# Atlas regions (u0, v0, u1, v1).
REG = {
    'cover': (0.0, 0.5, 0.5, 1.0),
    'page': (0.5, 0.5, 1.0, 1.0),
    'phone_body': (0.0, 0.25, 0.25, 0.5),
    'phone_screen': (0.25, 0.25, 0.5, 0.5),
    'cam_body': (0.5, 0.25, 0.75, 0.5),
    'lens': (0.75, 0.25, 1.0, 0.5),
    'glass': (0.0, 0.0, 0.125, 0.125),
    'pen': (0.125, 0.0, 0.25, 0.125),
    'metal': (0.25, 0.0, 0.375, 0.125),
    'flash': (0.375, 0.0, 0.5, 0.125),
    'page_edge': (0.5, 0.0, 0.625, 0.125),
    'strap': (0.625, 0.0, 0.75, 0.125),
}


def _rect(region, inset=0.004):
    u0, v0, u1, v1 = REG[region]
    return u0 + inset, v0 + inset, u1 - inset, v1 - inset


def box(size, center=(0, 0, 0), faces=None, material='Props', default='cover'):
    """faces: dict {'+x','-x','+y','-y','+z','-z'} -> region (or (region, material))."""
    sx, sy, sz = np.array(size) / 2
    c = np.array(center, float)
    faces = faces or {}
    parts = []
    spec = {
        '+x': ([1, 0, 0], [[1, -1, 1], [1, -1, -1], [1, 1, -1], [1, 1, 1]]),
        '-x': ([-1, 0, 0], [[-1, -1, -1], [-1, -1, 1], [-1, 1, 1], [-1, 1, -1]]),
        '+y': ([0, 1, 0], [[-1, 1, 1], [1, 1, 1], [1, 1, -1], [-1, 1, -1]]),
        '-y': ([0, -1, 0], [[-1, -1, -1], [1, -1, -1], [1, -1, 1], [-1, -1, 1]]),
        '+z': ([0, 0, 1], [[-1, -1, 1], [1, -1, 1], [1, 1, 1], [-1, 1, 1]]),
        '-z': ([0, 0, -1], [[1, -1, -1], [-1, -1, -1], [-1, 1, -1], [1, 1, -1]]),
    }
    for key, (n, corners) in spec.items():
        reg = faces.get(key, default)
        mat = material
        if isinstance(reg, tuple):
            reg, mat = reg
        if reg is None:
            continue
        u0, v0, u1, v1 = _rect(reg)
        v = np.array(corners, float) * [sx, sy, sz] + c
        uv = np.array([[u0, v0], [u1, v0], [u1, v1], [u0, v1]])
        parts.append((mat, v, uv, np.tile(n, (4, 1)).astype(float), np.array([[0, 1, 2], [0, 2, 3]])))
    return parts


def cylinder(r, h, n=16, center=(0, 0, 0), axis='y', side='pen', cap='metal', r2=None, material='Props',
             cap_material=None):
    """Cylinder (or cone frustum with r2) along +axis, base at center - h/2."""
    r2 = r if r2 is None else r2
    a = np.linspace(0, 2 * np.pi, n + 1)
    u0, v0, u1, v1 = _rect(side)
    verts, uvs, nrms, tris = [], [], [], []
    slope = (r - r2) / h
    for i in range(n):
        for j, (rr, y) in enumerate(((r, -h / 2), (r2, h / 2))):
            pass
        q = []
        for k, (ang, rr, y) in enumerate(((a[i], r, -h / 2), (a[i + 1], r, -h / 2), (a[i + 1], r2, h / 2),
                                          (a[i], r2, h / 2))):
            q.append([rr * np.cos(ang), y, rr * np.sin(ang)])
            nn = np.array([np.cos(ang), slope, np.sin(ang)])
            nrms.append(nn / np.linalg.norm(nn))
            uvs.append([u0 + (u1 - u0) * (ang / (2 * np.pi)), v0 if y < 0 else v1])
        base = len(verts)
        verts += q
        tris += [[base, base + 2, base + 1], [base, base + 3, base + 2]]
    parts = [(material, np.array(verts), np.array(uvs), np.array(nrms), np.array(tris))]
    cu0, cv0, cu1, cv1 = _rect(cap)
    for y, rr, sgn in ((h / 2, r2, 1), (-h / 2, r, -1)):
        v = [[0, y, 0]] + [[rr * np.cos(t), y, rr * np.sin(t)] for t in a[:-1]]
        uv = [[(cu0 + cu1) / 2, (cv0 + cv1) / 2]] + [[(cu0 + cu1) / 2 + (cu1 - cu0) / 2 * np.cos(t),
                                                      (cv0 + cv1) / 2 + (cv1 - cv0) / 2 * np.sin(t)] for t in a[:-1]]
        t = [[0, 1 + (i + 1) % n, 1 + i] if sgn > 0 else [0, 1 + i, 1 + (i + 1) % n] for i in range(n)]
        parts.append((cap_material or material, np.array(v, float), np.array(uv), np.tile([0, sgn, 0], (n + 1, 1)).astype(float),
                      np.array(t)))
    out = []
    perm = {'y': [0, 1, 2], 'x': [1, 0, 2], 'z': [0, 2, 1]}[axis]
    for mat, v, uv, nn, t in parts:
        v, nn = v[:, perm], nn[:, perm]
        if axis in ('x', 'z'):
            t = t[:, [0, 2, 1]]  # the swap of two axes mirrors: restore winding
        out.append((mat, v + np.array(center, float), uv, nn, t))
    return out


def notebook():
    """Pocket notebook, 90 x 140 mm, 9 mm thick; written page facing +y, top edge towards +z."""
    parts = box((0.090, 0.0075, 0.140), (0, 0.00375, 0),
                {'+y': 'page', '-y': 'cover', '+x': 'page_edge', '-x': 'cover', '+z': 'page_edge', '-z': 'page_edge'})
    parts += box((0.092, 0.0012, 0.142), (0, -0.0006, 0), {'+y': 'cover', '-y': 'cover', '+x': 'cover', '-x': 'cover',
                                                             '+z': 'cover', '-z': 'cover'})
    # flipped-over cover hanging behind the top edge
    parts += box((0.092, 0.0012, 0.030), (0, -0.003, 0.084), {k: 'cover' for k in ('+x', '-x', '+y', '-y', '+z', '-z')})
    return parts


def pen():
    """Ballpoint pen, 140 mm along +y, tip at -y."""
    parts = cylinder(0.0045, 0.110, 12, (0, 0.015, 0), 'y', 'pen', 'pen')
    parts += cylinder(0.0045, 0.022, 12, (0, -0.051, 0), 'y', 'metal', 'metal', r2=0.0045)
    parts += cylinder(0.0045, 0.008, 12, (0, -0.066, 0), 'y', 'metal', 'metal', r2=0.0045 * 0.99)[0:1]
    parts += cylinder(0.0045, 0.010, 12, (0, -0.0665, 0), 'y', 'metal', 'metal', r2=0.0008)
    parts += box((0.0016, 0.004, 0.045), (0, 0.045, 0.0058), {}, default='metal')  # clip
    return parts


def phone():
    """Smartphone 72 x 150 x 8 mm, screen facing +y, top towards +z."""
    parts = box((0.072, 0.008, 0.150), (0, 0, 0), {'+y': 'phone_body'}, default='phone_body')
    parts += box((0.066, 0.0004, 0.142), (0, 0.0042, 0), {'+y': ('phone_screen', 'PropScreen'), '-y': None,
                                                           '+x': 'phone_body', '-x': 'phone_body', '+z': 'phone_body', '-z': 'phone_body'})
    parts += cylinder(0.006, 0.0016, 16, (0.020, -0.0045, 0.058), 'y', 'metal', 'glass')
    return parts


def camera():
    """DSLR with zoom lens and speedlight. Frame: x right (photographer's view), y up, lens towards -z,
    back screen at +z. Origin at the centre of the body."""
    parts = box((0.140, 0.098, 0.072), (0, 0, 0), {'+z': 'cam_body'}, default='cam_body')
    parts += box((0.034, 0.090, 0.040), (0.062, -0.003, -0.044), {}, default='cam_body')       # grip
    parts += box((0.058, 0.030, 0.060), (0, 0.062, 0), {}, default='cam_body')                  # prism hump
    parts += box((0.090, 0.060, 0.002), (-0.012, 0.004, 0.0365), {'+z': 'glass'}, default='cam_body')  # rear screen
    parts += cylinder(0.037, 0.030, 24, (0, -0.002, -0.050), 'z', 'cam_body', 'cam_body')      # mount ring
    parts += cylinder(0.036, 0.085, 24, (0, -0.002, -0.106), 'z', 'lens', 'glass')              # zoom barrel
    parts += cylinder(0.040, 0.030, 24, (0, -0.002, -0.160), 'z', 'cam_body', 'glass', r2=0.042)  # hood
    # speedlight on the hot shoe
    parts += box((0.020, 0.012, 0.020), (0, 0.083, 0), {}, default='metal')
    parts += box((0.060, 0.062, 0.040), (0, 0.120, 0.004), {}, default='cam_body')
    parts += box((0.068, 0.040, 0.054), (0, 0.171, -0.004), {'-z': 'flash'}, default='cam_body')
    return parts


BUILDERS = {'notebook': notebook, 'pen': pen, 'phone': phone, 'camera': camera}
