"""Minimal mesh + texture toolkit for the ForensicModels pack (project-made assets).

Meshes are kept unindexed per material as triangle corner arrays: P (n,3,3), UV (n,3,2), N (n,3,3).
OBJ output welds identical v / vt / vn rows. Units: metres, Y up, OBJ V up (row 0 of an image is v = 1).
"""
import math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

# ----------------------------------------------------------------------------------------- meshes
class Mesh:
    def __init__(self):
        self.parts = {}  # material -> [P, UV, N]

    def add(self, mat, P, UV, N=None):
        P = np.asarray(P, np.float64).reshape(-1, 3, 3)
        UV = np.asarray(UV, np.float64).reshape(-1, 3, 2)
        if N is None:
            N = flat_normals(P)
        N = np.asarray(N, np.float64).reshape(-1, 3, 3)
        if mat in self.parts:
            a = self.parts[mat]
            self.parts[mat] = [np.vstack([a[0], P]), np.vstack([a[1], UV]), np.vstack([a[2], N])]
        else:
            self.parts[mat] = [P, UV, N]
        return self

    def merge(self, other, M=None):
        for mat, (P, UV, N) in other.parts.items():
            if M is not None:
                P, N = xform(P, N, M)
            self.add(mat, P, UV, N)
        return self

    def transformed(self, M):
        out = Mesh()
        return out.merge(self, M)

    def tris(self):
        return sum(len(p[0]) for p in self.parts.values())

    def bounds(self):
        allp = np.vstack([p[0].reshape(-1, 3) for p in self.parts.values()])
        return allp.min(0), allp.max(0)

    def ground(self, center_xz=True):
        lo, hi = self.bounds()
        off = np.array([(lo[0] + hi[0]) / 2 if center_xz else 0, lo[1], (lo[2] + hi[2]) / 2 if center_xz else 0])
        for mat in self.parts:
            self.parts[mat][0] = self.parts[mat][0] - off
        return self

    def write(self, path, materials, name=None):
        """materials: {mat: dict(Kd=(r,g,b), d=1, map=None, Ks=0.05, Ns=None)}"""
        name = name or os.path.splitext(os.path.basename(path))[0]
        V, T, Nn = {}, {}, {}
        vl, tl, nl = [], [], []
        def idx(table, lst, key):
            i = table.get(key)
            if i is None:
                i = len(lst) + 1; table[key] = i; lst.append(key)
            return i
        lines = ['# %s - ForensicModels (project-made, CC0). Units: metres, Y up.' % name, 'mtllib %s.mtl' % name]
        body = []
        for mat, (P, UV, N) in self.parts.items():
            body.append('g %s' % mat)
            body.append('usemtl %s' % mat)
            Pr = np.round(P, 5); UVr = np.round(UV, 5)
            Nr = N / (np.linalg.norm(N, axis=2, keepdims=True) + 1e-12); Nr = np.round(Nr, 4)
            for f in range(len(P)):
                corners = []
                for c in range(3):
                    vi = idx(V, vl, tuple(Pr[f, c])); ti = idx(T, tl, tuple(UVr[f, c])); ni = idx(Nn, nl, tuple(Nr[f, c]))
                    corners.append('%d/%d/%d' % (vi, ti, ni))
                body.append('f ' + ' '.join(corners))
        out = lines + ['v %.5f %.5f %.5f' % v for v in vl] + ['vt %.5f %.5f' % t for t in tl] + \
            ['vn %.4f %.4f %.4f' % n for n in nl] + body
        with open(path, 'w') as h:
            h.write('\n'.join(out) + '\n')
        mtl = []
        for mat in self.parts:
            m = materials.get(mat, {})
            kd = m.get('Kd', (1, 1, 1))
            s = 'newmtl %s\nKd %.3f %.3f %.3f\nKs %.3f %.3f %.3f\nd %.3f' % (
                mat, kd[0], kd[1], kd[2], m.get('Ks', 0.05), m.get('Ks', 0.05), m.get('Ks', 0.05), m.get('d', 1.0))
            if m.get('Ns'):
                s += '\nNs %d' % m['Ns']
            if m.get('map'):
                s += '\nmap_Kd %s' % m['map']
            mtl.append(s)
        with open(os.path.join(os.path.dirname(path), name + '.mtl'), 'w') as h:
            h.write('\n\n'.join(mtl) + '\n')
        return self.tris()


def flat_normals(P):
    n = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
    n /= np.linalg.norm(n, axis=1, keepdims=True) + 1e-12
    return np.repeat(n[:, None, :], 3, axis=1)


def smooth_normals(P, angle=50.0):
    """Per-corner normals averaged over faces sharing the position within an angle."""
    fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
    area_n = fn.copy()
    fnu = fn / (np.linalg.norm(fn, axis=1, keepdims=True) + 1e-12)
    corners = P.reshape(-1, 3)
    key = np.round(corners, 6)
    _, inv = np.unique(key, axis=0, return_inverse=True)
    inv = inv.ravel()
    face_of = np.repeat(np.arange(len(P)), 3)
    order = np.argsort(inv, kind='stable')
    N = np.zeros_like(corners)
    cosl = math.cos(math.radians(angle))
    bounds = np.flatnonzero(np.diff(inv[order])) + 1
    for grp in np.split(order, bounds):
        fs = face_of[grp]
        un = fnu[fs]
        sim = (un @ un.T) >= cosl
        N[grp] = sim.astype(float) @ area_n[fs]
    N /= np.linalg.norm(N, axis=1, keepdims=True) + 1e-12
    return N.reshape(-1, 3, 3)


def xform(P, N, M):
    M = np.asarray(M, float)
    R = M[:3, :3]; t = M[:3, 3]
    P2 = P @ R.T + t
    Rn = np.linalg.inv(R).T
    N2 = N @ Rn.T
    N2 /= np.linalg.norm(N2, axis=2, keepdims=True) + 1e-12
    return P2, N2


def T(x=0, y=0, z=0):
    M = np.eye(4); M[:3, 3] = (x, y, z); return M


def S(x, y=None, z=None):
    M = np.eye(4); M[0, 0] = x; M[1, 1] = x if y is None else y; M[2, 2] = x if z is None else z; return M


def Rx(deg):
    a = math.radians(deg); c, s = math.cos(a), math.sin(a)
    M = np.eye(4); M[1, 1] = c; M[1, 2] = -s; M[2, 1] = s; M[2, 2] = c; return M


def Ry(deg):
    a = math.radians(deg); c, s = math.cos(a), math.sin(a)
    M = np.eye(4); M[0, 0] = c; M[0, 2] = s; M[2, 0] = -s; M[2, 2] = c; return M


def Rz(deg):
    a = math.radians(deg); c, s = math.cos(a), math.sin(a)
    M = np.eye(4); M[0, 0] = c; M[0, 1] = -s; M[1, 0] = s; M[1, 1] = c; return M


def chain(*Ms):
    out = np.eye(4)
    for M in Ms:
        out = out @ M
    return out


def quads_to_tris(Q):
    """Q (n,4,k) -> (2n,3,k), quad corners counter-clockwise."""
    Q = np.asarray(Q)
    a = Q[:, [0, 1, 2]]; b = Q[:, [0, 2, 3]]
    return np.concatenate([a, b], axis=0)


def grid_surface(Pg, UVg, Ng=None, flip=False):
    """Pg (r,c,3) grid -> triangles. Normals from Ng grid if given."""
    r, c = Pg.shape[:2]
    def quads(G):
        return np.stack([G[:-1, :-1], G[1:, :-1], G[1:, 1:], G[:-1, 1:]], axis=2).reshape(-1, 4, G.shape[-1])
    QP = quads(Pg); QU = quads(UVg)
    if flip:
        QP = QP[:, ::-1]; QU = QU[:, ::-1]
    P = quads_to_tris(QP); UV = quads_to_tris(QU)
    if Ng is not None:
        QN = quads(Ng)
        if flip:
            QN = QN[:, ::-1]
        N = quads_to_tris(QN)
    else:
        N = None
    return P, UV, N


def lathe(profile, segs=32, uv=(0, 0, 1, 1), sx=1.0, sz=1.0, smooth=True, a0=0.0, a1=360.0, flip=False):
    """profile: [(radius, y)] from bottom to top. Returns P, UV, N. u around (a0..a1), v along profile length."""
    prof = np.asarray(profile, float)
    L = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(prof, axis=0), axis=1))])
    L = L / (L[-1] or 1)
    ang = np.radians(np.linspace(a0, a1, segs + 1))
    R = prof[:, 0][:, None]; Y = prof[:, 1][:, None]
    X = R * np.cos(ang)[None] * sx; Z = -R * np.sin(ang)[None] * sz
    Pg = np.stack([X, np.repeat(Y, segs + 1, 1), Z], -1)
    u0, v0, u1, v1 = uv
    U = u0 + (u1 - u0) * (np.linspace(0, 1, segs + 1))[None].repeat(len(prof), 0)
    Vv = v0 + (v1 - v0) * L[:, None].repeat(segs + 1, 1)
    UVg = np.stack([U, Vv], -1)
    P, UVt, _ = grid_surface(Pg, UVg, flip=not flip)
    N = smooth_normals(P, 60) if smooth else None
    return P, UVt, N


def disk(radius, y, segs=32, uv_center=(0.5, 0.5), uv_radius=0.5, up=True, sx=1.0, sz=1.0):
    ang = np.radians(np.linspace(0, 360, segs + 1))
    P = []; UV = []
    for i in range(segs):
        a, b = ang[i], ang[i + 1]
        pa = (radius * math.cos(a) * sx, y, -radius * math.sin(a) * sz)
        pb = (radius * math.cos(b) * sx, y, -radius * math.sin(b) * sz)
        c = (0, y, 0)
        ua = (uv_center[0] + uv_radius * math.cos(a), uv_center[1] + uv_radius * math.sin(a))
        ub = (uv_center[0] + uv_radius * math.cos(b), uv_center[1] + uv_radius * math.sin(b))
        if up:
            P.append([c, pa, pb]); UV.append([uv_center, ua, ub])
        else:
            P.append([c, pb, pa]); UV.append([uv_center, ub, ua])
    P = np.array(P); UV = np.array(UV)
    N = np.zeros_like(P); N[..., 1] = 1 if up else -1
    return P, UV, N


def box(size, center=(0, 0, 0), uvs=None, scale_uv=None):
    """Axis box. uvs: dict face->(u0,v0,u1,v1) for faces px,nx,py,ny,pz,nz; default whole texture.
    scale_uv: metres per UV unit for world-scale mapping (overrides uvs)."""
    sx, sy, sz = [s / 2 for s in size]
    cx, cy, cz = center
    faces = {
        'px': ([(sx, -sy, sz), (sx, -sy, -sz), (sx, sy, -sz), (sx, sy, sz)], (1, 0, 0)),
        'nx': ([(-sx, -sy, -sz), (-sx, -sy, sz), (-sx, sy, sz), (-sx, sy, -sz)], (-1, 0, 0)),
        'py': ([(-sx, sy, sz), (sx, sy, sz), (sx, sy, -sz), (-sx, sy, -sz)], (0, 1, 0)),
        'ny': ([(-sx, -sy, -sz), (sx, -sy, -sz), (sx, -sy, sz), (-sx, -sy, sz)], (0, -1, 0)),
        'pz': ([(-sx, -sy, sz), (sx, -sy, sz), (sx, sy, sz), (-sx, sy, sz)], (0, 0, 1)),
        'nz': ([(sx, -sy, -sz), (-sx, -sy, -sz), (-sx, sy, -sz), (sx, sy, -sz)], (0, 0, -1)),
    }
    P = []; UV = []; N = []
    for k, (q, n) in faces.items():
        q = np.array(q) + (cx, cy, cz)
        if scale_uv:
            # planar world mapping on the two axes spanning the face
            ax = [i for i in range(3) if n[i] == 0]
            uvq = q[:, ax] / scale_uv
        else:
            u0, v0, u1, v1 = (uvs or {}).get(k, (0, 0, 1, 1))
            uvq = np.array([(u0, v0), (u1, v0), (u1, v1), (u0, v1)], float)
        for tri in ((0, 1, 2), (0, 2, 3)):
            P.append(q[list(tri)]); UV.append(uvq[list(tri)]); N.append([n] * 3)
    return np.array(P, float), np.array(UV, float), np.array(N, float)


def rounded_box(size, radius, seg=4, uv_mode='box', uv_rects=None, center=(0, 0, 0)):
    """Rounded box from a subdivided cube. uv_mode 'box': per-face planar into uv_rects[face] (default 0..1)."""
    h = np.array(size, float) / 2
    r = min(radius, *(h * 0.999))
    inner = h - r
    def axis_coords(hv, iv):
        lo = [-iv - r * math.cos(math.pi / 2 * i / seg) for i in range(seg + 1)]
        hi = [iv + r * math.cos(math.pi / 2 * i / seg) for i in range(seg, -1, -1)]
        return np.array(lo + hi)
    out_P, out_UV, out_N = [], [], []
    face_defs = {
        'px': (0, 1, 1, 2), 'nx': (0, -1, 2, 1), 'py': (1, 1, 2, 0), 'ny': (1, -1, 0, 2),
        'pz': (2, 1, 0, 1), 'nz': (2, -1, 1, 0)}
    for name, (axis, sign, ua, va) in face_defs.items():
        A = axis_coords(h[ua], inner[ua]); B = axis_coords(h[va], inner[va])
        G = np.zeros((len(B), len(A), 3))
        G[..., axis] = sign * h[axis]
        G[..., ua] = A[None, :]
        G[..., va] = B[:, None]
        cl = np.clip(G, -inner, inner)
        d = G - cl
        n = d / (np.linalg.norm(d, axis=-1, keepdims=True) + 1e-12)
        # flat regions: normal = face axis
        flat = np.linalg.norm(d, axis=-1) < 1e-9
        n[flat] = 0; n[flat, axis] = sign
        Pg = cl + n * r
        u0, v0, u1, v1 = (uv_rects or {}).get(name, (0, 0, 1, 1))
        Ug = (Pg[..., ua] + h[ua]) / (2 * h[ua]); Vg = (Pg[..., va] + h[va]) / (2 * h[va])
        UVg = np.stack([u0 + (u1 - u0) * Ug, v0 + (v1 - v0) * Vg], -1)
        P, UV, N = grid_surface(Pg, UVg, n, flip=False)
        # orientation check: make normals agree with winding
        fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
        if np.mean(np.sum(fn * N.mean(1), axis=1)) < 0:
            P = P[:, ::-1]; UV = UV[:, ::-1]; N = N[:, ::-1]
        out_P.append(P); out_UV.append(UV); out_N.append(N)
    P = np.vstack(out_P) + np.array(center); UV = np.vstack(out_UV); N = np.vstack(out_N)
    return P, UV, N


def tube(path, radius, segs=12, uv=(0, 0, 1, 1), closed_ends=True, radii=None):
    """Sweep a circle along a polyline (parallel transport frames)."""
    path = np.asarray(path, float)
    n = len(path)
    tang = np.gradient(path, axis=0); tang /= np.linalg.norm(tang, axis=1, keepdims=True)
    ref = np.array([0, 1, 0]) if abs(tang[0][1]) < 0.9 else np.array([1, 0, 0])
    nrm = np.cross(tang[0], ref); nrm /= np.linalg.norm(nrm)
    frames = []
    for i in range(n):
        if i > 0:
            b = np.cross(tang[i - 1], tang[i])
            if np.linalg.norm(b) > 1e-9:
                b /= np.linalg.norm(b)
                ang = math.acos(np.clip(np.dot(tang[i - 1], tang[i]), -1, 1))
                nrm = rotate_vec(nrm, b, ang)
        bin_ = np.cross(tang[i], nrm)
        frames.append((nrm.copy(), bin_))
    ang = np.linspace(0, 2 * math.pi, segs + 1)
    rr = np.full(n, radius) if radii is None else np.asarray(radii, float)
    G = np.zeros((n, segs + 1, 3)); NG = np.zeros_like(G)
    for i in range(n):
        a, b = frames[i]
        d = np.cos(ang)[:, None] * a + np.sin(ang)[:, None] * b
        G[i] = path[i] + rr[i] * d; NG[i] = d
    L = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(path, axis=0), axis=1))]); L /= L[-1] or 1
    u0, v0, u1, v1 = uv
    UVg = np.stack(np.meshgrid(u0 + (u1 - u0) * np.linspace(0, 1, segs + 1), v0 + (v1 - v0) * L), -1)
    P, UV, N = grid_surface(G, UVg, NG)
    fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
    if np.mean(np.sum(fn * N.mean(1), axis=1)) < 0:
        P = P[:, ::-1]; UV = UV[:, ::-1]; N = N[:, ::-1]
    Ps, UVs, Ns = [P], [UV], [N]
    if closed_ends:
        for i, s in ((0, -1), (n - 1, 1)):
            c = path[i]; ring = G[i]
            for k in range(segs):
                tri = np.array([c, ring[k], ring[k + 1]]) if s > 0 else np.array([c, ring[k + 1], ring[k]])
                fnn = np.cross(tri[1] - tri[0], tri[2] - tri[0])
                if np.dot(fnn, tang[i] * s) < 0:
                    tri = tri[::-1]
                Ps.append(tri[None]); UVs.append(np.array([[(u0, v0)] * 3])); Ns.append(np.array([[tang[i] * s] * 3]))
    return np.vstack(Ps), np.vstack(UVs), np.vstack(Ns)


def rotate_vec(v, axis, ang):
    axis = axis / np.linalg.norm(axis)
    return v * math.cos(ang) + np.cross(axis, v) * math.sin(ang) + axis * np.dot(axis, v) * (1 - math.cos(ang))


def triangulate(poly):
    """Ear clipping for a simple polygon (counter-clockwise, (n,2)). Returns index triples."""
    pts = [tuple(p) for p in poly]
    idx = list(range(len(pts)))
    if area2(poly) < 0:
        idx.reverse()
    tris = []
    guard = 0
    while len(idx) > 3 and guard < 10000:
        guard += 1
        ear = False
        for i in range(len(idx)):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % len(idx)]
            A, B, C = np.array(pts[a]), np.array(pts[b]), np.array(pts[c])
            if cross2(B - A, C - B) <= 1e-14:
                continue
            ok = True
            for j in idx:
                if j in (a, b, c):
                    continue
                if point_in_tri(np.array(pts[j]), A, B, C):
                    ok = False; break
            if ok:
                tris.append((a, b, c)); idx.pop(i); ear = True; break
        if not ear:
            break
    if len(idx) == 3:
        tris.append(tuple(idx))
    return tris


def area2(poly):
    p = np.asarray(poly)
    return np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1]) / 2


def cross2(a, b):
    return a[0] * b[1] - a[1] * b[0]


def point_in_tri(p, a, b, c):
    d1 = cross2(b - a, p - a); d2 = cross2(c - b, p - b); d3 = cross2(a - c, p - c)
    return (d1 >= 0) and (d2 >= 0) and (d3 >= 0)


def extrude(poly, depth, uv_top=None, uv_side=None, y0=0.0, side_mat_uv_scale=None, bevel=0.0):
    """Extrude a 2D polygon (x, z in metres, counter-clockwise seen from +Y) upward from y0 by depth.
    uv_top(x, z) -> (u, v); uv_side: (u0, v0, u1, v1) rect stretched along the perimeter.
    Returns (top P,UV,N), (bottom ...), (side ...)."""
    poly = np.asarray(poly, float)
    if area2(poly) < 0:
        poly = poly[::-1]
    tris = triangulate(poly)
    def mk(yv, up):
        P = []; UV = []
        for a, b, c in tris:
            tri = [(poly[k][0], yv, poly[k][1]) for k in (a, b, c)]
            # CCW in x/z seen from +Y means (x, -z) orientation; ensure upward normal
            t = np.array(tri)
            fn = np.cross(t[1] - t[0], t[2] - t[0])
            if (fn[1] < 0) == up:
                t = t[::-1]
            P.append(t)
            UV.append([uv_top(p[0], p[2]) if uv_top else (p[0], p[2]) for p in t])
        P = np.array(P); UV = np.array(UV); N = np.zeros_like(P); N[..., 1] = 1 if up else -1
        return P, UV, N
    top = mk(y0 + depth, True); bot = mk(y0, False)
    per = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(np.vstack([poly, poly[:1]]), axis=0), axis=1))])
    tot = per[-1]
    u0, v0, u1, v1 = uv_side or (0, 0, 1, 1)
    P = []; UV = []; N = []
    for i in range(len(poly)):
        a = poly[i]; b = poly[(i + 1) % len(poly)]
        q = np.array([(a[0], y0, a[1]), (b[0], y0, b[1]), (b[0], y0 + depth, b[1]), (a[0], y0 + depth, a[1])])
        if side_mat_uv_scale:
            ua, ub = per[i] / side_mat_uv_scale, per[i + 1] / side_mat_uv_scale
            uq = [(ua, 0), (ub, 0), (ub, depth / side_mat_uv_scale), (ua, depth / side_mat_uv_scale)]
        else:
            ua, ub = u0 + (u1 - u0) * per[i] / tot, u0 + (u1 - u0) * per[i + 1] / tot
            uq = [(ua, v0), (ub, v0), (ub, v1), (ua, v1)]
        e = b - a; n = np.array([e[1], 0, -e[0]]); n /= np.linalg.norm(n) + 1e-12
        # outward normal check: for CCW polygon in x/z (seen from +Y with z down?) compute against centroid
        for tri in ((0, 1, 2), (0, 2, 3)):
            t = q[list(tri)]
            fn = np.cross(t[1] - t[0], t[2] - t[0])
            uu = [uq[k] for k in tri]
            if np.dot(fn, n) < 0:
                t = t[::-1]; uu = uu[::-1]
            P.append(t); UV.append(uu); N.append([n] * 3)
    side = (np.array(P), np.array(UV), np.array(N))
    return top, bot, side


# --------------------------------------------------------------------------------------- textures
def noise(h, w, scale, seed, octaves=4, persistence=0.5):
    """Value-noise fBm in [0,1] via bicubic upsampling of random grids."""
    rng = np.random.default_rng(seed)
    acc = np.zeros((h, w), np.float32); amp = 1.0; tot = 0
    for o in range(octaves):
        gh = max(2, int(h / scale * 2 ** o)); gw = max(2, int(w / scale * 2 ** o))
        g = rng.random((gh, gw)).astype(np.float32)
        im = Image.fromarray((g * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
        acc += np.asarray(im, np.float32) / 255 * amp; tot += amp; amp *= persistence
    return acc / tot


def normal_from_height(hmap, strength=2.0):
    hm = np.asarray(hmap, np.float32)
    dx = (np.roll(hm, -1, 1) - np.roll(hm, 1, 1)) * strength
    dy = (np.roll(hm, -1, 0) - np.roll(hm, 1, 0)) * strength
    n = np.dstack([-dx, dy, np.ones_like(hm)])  # OpenGL (Y+) convention: +v is up in the image => -row
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return ((n * 0.5 + 0.5) * 255).clip(0, 255).astype(np.uint8)


def save_color(path, rgb, quality=90):
    a = np.asarray(rgb)
    if a.dtype != np.uint8:
        a = (np.clip(a, 0, 1) * 255).astype(np.uint8)
    im = Image.fromarray(a)
    if path.endswith('.png'):
        im.save(path, optimize=True)
    else:
        im.convert('RGB').save(path, quality=quality, optimize=True)


def save_ms(path, metal, smooth):
    m = (np.clip(np.asarray(metal, np.float32), 0, 1) * 255).astype(np.uint8)
    s = (np.clip(np.asarray(smooth, np.float32), 0, 1) * 255).astype(np.uint8)
    if m.ndim == 0 or m.size == 1:
        m = np.full(s.shape, int(m), np.uint8)
    Image.fromarray(np.dstack([m, m, m, s]), 'RGBA').save(path, optimize=True)


def font(path_or_name, size):
    candidates = {
        'barlow': os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Fonts', 'Barlow-Regular.ttf'),
        'barlow-med': os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Fonts', 'Barlow-Medium.ttf'),
        'barlow-semi': os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Fonts', 'Barlow-SemiBold.ttf'),
        'mono': '/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf',
        'mono-bold': '/usr/share/fonts/truetype/dejavu/DejaVuSansMono-Bold.ttf',
        'sans-bold': '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',
        'sans': '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',
        'serif': '/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf',
        'serif-bold': '/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf',
    }
    return ImageFont.truetype(candidates.get(path_or_name, path_or_name), size)
