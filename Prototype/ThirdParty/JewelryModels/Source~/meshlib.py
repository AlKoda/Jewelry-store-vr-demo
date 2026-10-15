"""Small mesh toolkit for the JewelryModels generator (project-owned, numpy only).
Conventions: metres, Y up, front = +Z (glTF/OBJ right-handed; Unity's OBJ importer mirrors X so the model
reads the same in Unity). Every Part has its own material name, per-vertex normals and optional UVs."""
import numpy as np
from scipy.spatial import ConvexHull

TAU = 2 * np.pi

def unit(v):
    v = np.asarray(v, float)
    n = np.linalg.norm(v, axis=-1, keepdims=True)
    return v / np.where(n < 1e-15, 1, n)

def rot(axis, ang):
    axis = unit(axis); x, y, z = axis; c, s = np.cos(ang), np.sin(ang); C = 1 - c
    return np.array([[c + x * x * C, x * y * C - z * s, x * z * C + y * s],
                     [y * x * C + z * s, c + y * y * C, y * z * C - x * s],
                     [z * x * C - y * s, z * y * C + x * s, c + z * z * C]])

RX = lambda a: rot([1, 0, 0], a)
RY = lambda a: rot([0, 1, 0], a)
RZ = lambda a: rot([0, 0, 1], a)

def smooth_normals(v, f):
    n = np.zeros_like(v)
    tri = v[f]
    fn = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])  # area weighted
    for k in range(3):
        np.add.at(n, f[:, k], fn)
    return unit(n)

class Part:
    def __init__(self, v, f, mat, n=None, uv=None, smooth=True, name=None):
        v = np.asarray(v, float); f = np.asarray(f, np.int64).reshape(-1, 3)
        if not smooth:
            v2 = v[f].reshape(-1, 3)
            tri = v[f]
            fn = unit(np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0]))
            n = np.repeat(fn, 3, axis=0)
            if uv is not None: uv = np.asarray(uv, float)[f].reshape(-1, 2)
            f = np.arange(len(v2)).reshape(-1, 3); v = v2
        elif n is None:
            n = smooth_normals(v, f)
        self.v, self.f, self.n, self.mat, self.name = v, f, unit(n), mat, name or mat
        self.uv = None if uv is None else np.asarray(uv, float)

    def copy(self):
        p = Part.__new__(Part)
        p.v, p.f, p.n, p.mat, p.name = self.v.copy(), self.f.copy(), self.n.copy(), self.mat, self.name
        p.uv = None if self.uv is None else self.uv.copy()
        return p

    def xf(self, M=None, t=(0, 0, 0), s=1.0):
        """apply v' = M @ (s*v) + t (s may be a 3-vector); normals with inverse transpose."""
        M = np.eye(3) if M is None else np.asarray(M, float)
        S = np.diag(np.broadcast_to(np.asarray(s, float), (3,)))
        A = M @ S
        p = self.copy()
        p.v = self.v @ A.T + np.asarray(t, float)
        p.n = unit(self.n @ np.linalg.inv(A))
        if np.linalg.det(A) < 0: p.f = p.f[:, ::-1]
        return p

    def flip(self):
        p = self.copy(); p.f = p.f[:, ::-1]; p.n = -p.n; return p

    @property
    def tris(self): return len(self.f)

def merge(parts, mat=None, name=None):
    """merge parts that share a material into one part"""
    v, f, n, uv, off = [], [], [], [], 0
    has_uv = all(p.uv is not None for p in parts)
    for p in parts:
        v.append(p.v); f.append(p.f + off); n.append(p.n); off += len(p.v)
        if has_uv: uv.append(p.uv)
    q = Part.__new__(Part)
    q.v, q.f, q.n = np.vstack(v), np.vstack(f), np.vstack(n)
    q.uv = np.vstack(uv) if has_uv else None
    q.mat = mat or parts[0].mat; q.name = name or q.mat
    return q

def grid_faces(rows, cols, wrap_cols=False, wrap_rows=False):
    """faces for a (rows x cols) vertex grid, index = r*cols + c"""
    F = []
    cmax = cols if wrap_cols else cols - 1
    rmax = rows if wrap_rows else rows - 1
    for r in range(rmax):
        r2 = (r + 1) % rows
        for c in range(cmax):
            c2 = (c + 1) % cols
            a, b, cc, d = r * cols + c, r * cols + c2, r2 * cols + c2, r2 * cols + c
            F.append([a, b, cc]); F.append([a, cc, d])
    return np.array(F)

def lathe(profile, segs, mat, cap_bottom=False, cap_top=False, u_scale=1.0, smooth=True, a0=0.0, a1=TAU):
    """revolve profile [(r, y), ...] (bottom to top) around +Y. Faces outward when r increases to the right of travel."""
    prof = np.asarray(profile, float); m = len(prof)
    full = abs(a1 - a0 - TAU) < 1e-9
    nseg = segs if full else segs + 1
    ang = a0 + (a1 - a0) * np.arange(nseg) / segs
    V = []; UV = []
    L = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(prof, axis=0), axis=1))]); L = L / max(L[-1], 1e-12)
    for i, a in enumerate(ang):
        for j, (r, y) in enumerate(prof):
            V.append([r * np.sin(a), y, r * np.cos(a)]); UV.append([u_scale * i / segs, L[j]])
    V = np.array(V)
    F = []
    for i in range(segs):
        i2 = (i + 1) % nseg if full else i + 1
        for j in range(m - 1):
            a, b, c, d = i * m + j, i2 * m + j, i2 * m + j + 1, i * m + j + 1
            F.append([a, b, c]); F.append([a, c, d])
    F = np.array(F)
    parts = [Part(V, F, mat, uv=np.array(UV), smooth=smooth)]
    if cap_bottom and prof[0, 0] > 1e-9:
        parts.append(disc(prof[0, 0], segs, mat, y=prof[0, 1], up=False))
    if cap_top and prof[-1, 0] > 1e-9:
        parts.append(disc(prof[-1, 0], segs, mat, y=prof[-1, 1], up=True))
    return parts[0] if len(parts) == 1 else merge(parts)

def disc(r, segs, mat, y=0.0, up=True, uv_scale=None):
    a = TAU * np.arange(segs) / segs
    V = np.vstack([[0, y, 0], np.stack([r * np.sin(a), np.full(segs, y), r * np.cos(a)], 1)])
    F = np.array([[0, 1 + i, 1 + (i + 1) % segs] for i in range(segs)])
    if not up: F = F[:, ::-1]
    uv = np.stack([0.5 + 0.5 * V[:, 0] / r, 0.5 + 0.5 * V[:, 2] / r], 1)
    N = np.tile([0, 1 if up else -1, 0], (len(V), 1))
    return Part(V, F, mat, n=N, uv=uv)

def sweep(path, section, mat, closed=True, scale=None, up=None, cap=False, smooth=True, twist=0.0, u_len=None):
    """sweep 2D section [(x, y)] (x along frame 'side', y along frame 'normal') along path (N,3).
    Parallel-transport frames. scale: per-point (N,) or (N,2). Section is assumed closed (loop)."""
    P = np.asarray(path, float); N = len(P); S = np.asarray(section, float); M = len(S)
    T = np.zeros_like(P)
    if closed:
        T = P[(np.arange(N) + 1) % N] - P[(np.arange(N) - 1) % N]
    else:
        T[1:-1] = P[2:] - P[:-2]; T[0] = P[1] - P[0]; T[-1] = P[-1] - P[-2]
    T = unit(T)
    ref = np.array([0, 1.0, 0]) if up is None else np.asarray(up, float)
    if abs(np.dot(ref, T[0])) > 0.9: ref = np.array([1.0, 0, 0])
    B = [unit(np.cross(T[0], ref))]; 
    for i in range(1, N):
        b = B[-1] - np.dot(B[-1], T[i]) * T[i]
        B.append(unit(b))
    B = np.array(B)
    if closed:  # distribute closing twist error
        b_end = B[-1] - np.dot(B[-1], T[0]) * T[0]; b_end = unit(b_end)
        Nn0 = np.cross(T[0], B[0])
        err = np.arctan2(np.dot(np.cross(b_end, B[0]), T[0]), np.dot(b_end, B[0]))
        for i in range(N):
            B[i] = rot(T[i], err * i / N) @ B[i]
    Nn = unit(np.cross(T, B))
    if scale is None: sc = np.ones((N, 2))
    else:
        sc = np.asarray(scale, float)
        if sc.ndim == 1: sc = np.stack([sc, sc], 1)
    V = []; UV = []
    seglen = np.linalg.norm(np.diff(P, axis=0), axis=1)
    acc = np.concatenate([[0], np.cumsum(seglen)])
    for i in range(N):
        tw = twist * i / N
        c, s = np.cos(tw), np.sin(tw)
        for j in range(M):
            x, y = S[j]; x, y = x * c - y * s, x * s + y * c
            V.append(P[i] + B[i] * x * sc[i, 0] + Nn[i] * y * sc[i, 1])
            UV.append([j / M, acc[i] / (u_len or max(acc[-1], 1e-9))])
    V = np.array(V)
    F = grid_faces(N, M, wrap_cols=True, wrap_rows=closed)
    part = Part(V, F, mat, uv=np.array(UV), smooth=smooth)
    if cap and not closed:
        caps = [part]
        for idx, sign in ((0, -1), (N - 1, 1)):
            ctr = P[idx]
            vv = np.vstack([ctr, V[idx * M:(idx + 1) * M]])
            ff = np.array([[0, 1 + (k + 1) % M, 1 + k] for k in range(M)])
            if sign > 0: ff = ff[:, ::-1]
            nn = np.tile(T[idx] * sign, (len(vv), 1))
            caps.append(Part(vv, ff, mat, n=nn, uv=np.zeros((len(vv), 2))))
        part = merge(caps)
    return part

def circle_path(R, n, plane='xz', y=0.0):
    a = TAU * np.arange(n) / n
    if plane == 'xz': return np.stack([R * np.sin(a), np.full(n, y), R * np.cos(a)], 1)
    if plane == 'yz': return np.stack([np.full(n, y), R * np.cos(a), R * np.sin(a)], 1)
    if plane == 'xy': return np.stack([R * np.sin(a), R * np.cos(a), np.full(n, y)], 1)

def circle2(r, n, a0=0.0):
    a = a0 + TAU * np.arange(n) / n
    return np.stack([r * np.cos(a), r * np.sin(a)], 1)

def uv_sphere(r, segs, rings, mat, smooth=True):
    V = [[0, r, 0]]; UV = [[0.5, 1]]
    for i in range(1, rings):
        th = np.pi * i / rings
        for j in range(segs):
            ph = TAU * j / segs
            V.append([r * np.sin(th) * np.sin(ph), r * np.cos(th), r * np.sin(th) * np.cos(ph)]); UV.append([j / segs, 1 - i / rings])
    V.append([0, -r, 0]); UV.append([0.5, 0])
    V = np.array(V); F = []
    for j in range(segs):
        F.append([0, 1 + j, 1 + (j + 1) % segs])
    for i in range(rings - 2):
        for j in range(segs):
            a = 1 + i * segs + j; b = 1 + i * segs + (j + 1) % segs
            c = 1 + (i + 1) * segs + (j + 1) % segs; d = 1 + (i + 1) * segs + j
            F.append([a, d, c]); F.append([a, c, b])
    last = len(V) - 1; base = 1 + (rings - 2) * segs
    for j in range(segs):
        F.append([last, base + (j + 1) % segs, base + j])
    return Part(V, np.array(F), mat, n=unit(V), uv=np.array(UV), smooth=smooth)

def rounded_box(size, radius, mat, k=6, smooth=True, uv_mode='box', sharp=None):
    """box centred at origin with rounded edges (radius), k subdivisions per face edge.
    sharp='top' / 'bottom' keeps the edges of that face square (vertical corners stay rounded), e.g. for the
    two halves of a hinged box that must close flush."""
    sx, sy, sz = np.asarray(size, float) / 2; r = min(radius, sx, sy, sz) * 0.999
    inner = np.array([sx - r, sy - r, sz - r])
    lo_in, hi_in = -inner.copy(), inner.copy()
    if sharp == 'top': hi_in[1] = sy
    if sharp == 'bottom': lo_in[1] = -sy
    faces = []
    V = []; F = []; UVs = []
    t = np.linspace(-1, 1, k + 1)
    # 6 faces as grids on the unit cube
    dirs = [(0, 1, 2, 1), (0, 1, 2, -1), (1, 2, 0, 1), (1, 2, 0, -1), (2, 0, 1, 1), (2, 0, 1, -1)]
    for (a, b, c, s) in dirs:
        base = len(V)
        for i in range(k + 1):
            for j in range(k + 1):
                p = np.zeros(3); p[c] = s; p[a] = t[i]; p[b] = t[j]
                q = p * np.array([sx, sy, sz])
                cl = np.clip(q, lo_in, hi_in)
                d = q - cl; dn = np.linalg.norm(d)
                q2 = cl + (d / dn * r if dn > 1e-12 else 0)
                V.append(q2)
                UVs.append([(t[i] + 1) / 2, (t[j] + 1) / 2])
        for i in range(k):
            for j in range(k):
                v0 = base + i * (k + 1) + j; v1 = v0 + 1; v2 = v0 + (k + 1) + 1; v3 = v0 + (k + 1)
                tri = [[v0, v3, v2], [v0, v2, v1]] if s > 0 else [[v0, v2, v3], [v0, v1, v2]]
                F += tri
    V = np.array(V); F = np.array(F)
    # fix orientation per face using outward test
    cen = V[F].mean(1); fn = np.cross(V[F[:, 1]] - V[F[:, 0]], V[F[:, 2]] - V[F[:, 0]])
    bad = (fn * cen).sum(1) < 0; F[bad] = F[bad][:, ::-1]
    # weld duplicates on edges for smooth normals
    key = np.round(V / 1e-7).astype(np.int64)
    _, idx, inv = np.unique(key, axis=0, return_index=True, return_inverse=True)
    inv = inv.reshape(-1)
    V2 = V[idx]; F2 = inv[F]; UV2 = np.array(UVs)[idx]
    keep = (F2[:, 0] != F2[:, 1]) & (F2[:, 1] != F2[:, 2]) & (F2[:, 0] != F2[:, 2])
    F2 = F2[keep]
    if uv_mode == 'box':  # simple planar projection by dominant normal
        n = smooth_normals(V2, F2); ax = np.abs(n).argmax(1)
        UV2 = np.where(ax[:, None] == 0, V2[:, [2, 1]], np.where(ax[:, None] == 1, V2[:, [0, 2]], V2[:, [0, 1]]))
        UV2 = UV2 / max(sx, sy, sz) / 2 + 0.5
    return Part(V2, F2, mat, uv=UV2, smooth=smooth)

def convex(points, mat):
    """flat-shaded convex hull (gems)."""
    P = np.asarray(points, float)
    h = ConvexHull(P)
    F = h.simplices.copy()
    for i, (a, b, c) in enumerate(F):
        nrm = np.cross(P[b] - P[a], P[c] - P[a])
        if np.dot(nrm, h.equations[i][:3]) < 0: F[i] = [a, c, b]
    # flat normals straight from hull equations (coplanar triangles share them exactly)
    V = P[F].reshape(-1, 3); N = np.repeat(h.equations[:, :3], 3, axis=0)
    # planar UV from x/z
    ext = np.ptp(P, axis=0).max()
    uv = (V[:, [0, 2]] - P[:, [0, 2]].min(0)) / max(ext, 1e-9)
    return Part(V, np.arange(len(V)).reshape(-1, 3), mat, n=N, uv=uv)

def cylinder(r, h, segs, mat, cap=True, y0=0.0, smooth=True, r_top=None):
    rt = r if r_top is None else r_top
    return lathe([(r, y0), (rt, y0 + h)], segs, mat, cap_bottom=cap, cap_top=cap, smooth=smooth)

def bbox(parts):
    V = np.vstack([p.v for p in parts]); return V.min(0), V.max(0)

def ground(parts, center_xz=True):
    lo, hi = bbox(parts)
    off = np.array([(lo[0] + hi[0]) / 2 if center_xz else 0, lo[1], (lo[2] + hi[2]) / 2 if center_xz else 0])
    return [p.xf(t=-off) for p in parts]

def write_obj(path_obj, parts, materials, header=''):
    """materials: name -> dict(Kd=(r,g,b), Ks=(..), Ns=.., d=.., map_Kd=file or None)"""
    import os
    base = os.path.splitext(os.path.basename(path_obj))[0]
    used = []
    lines = ['# ' + l for l in header.splitlines()] + ['mtllib %s.mtl' % base]
    vo = to = no = 0
    # group parts by material to keep submesh count low
    order = []
    for p in parts:
        if p.mat not in order: order.append(p.mat)
    for m in order:
        group = [p for p in parts if p.mat == m]
        q = merge(group) if len(group) > 1 else group[0]
        used.append(m)
        lines.append('g %s' % m)
        lines.append('usemtl %s' % m)
        lines += ['v %.6f %.6f %.6f' % tuple(x) for x in q.v]
        has_uv = q.uv is not None
        if has_uv: lines += ['vt %.5f %.5f' % tuple(x) for x in q.uv]
        lines += ['vn %.4f %.4f %.4f' % tuple(x) for x in q.n]
        for a, b, c in q.f + 1:
            if has_uv:
                lines.append('f %d/%d/%d %d/%d/%d %d/%d/%d' % (a + vo, a + to, a + no, b + vo, b + to, b + no, c + vo, c + to, c + no))
            else:
                lines.append('f %d//%d %d//%d %d//%d' % (a + vo, a + no, b + vo, b + no, c + vo, c + no))
        vo += len(q.v); no += len(q.v); to += len(q.v) if has_uv else 0
    with open(path_obj, 'w', newline='\n') as fh:
        fh.write('\n'.join(lines) + '\n')
    ml = []
    for m in used:
        d = materials[m]
        kd = d.get('Kd', (0.8, 0.8, 0.8)); ks = d.get('Ks', (0.5, 0.5, 0.5))
        s = 'newmtl %s\nKd %.4f %.4f %.4f\nKs %.4f %.4f %.4f\nNs %.1f\nd %.3f\nillum 2' % (m, kd[0], kd[1], kd[2], ks[0], ks[1], ks[2], d.get('Ns', 100), d.get('d', 1.0))
        if d.get('map_Kd'): s += '\nmap_Kd %s' % d['map_Kd']
        ml.append(s)
    with open(os.path.splitext(path_obj)[0] + '.mtl', 'w', newline='\n') as fh:
        fh.write('\n\n'.join(ml) + '\n')
    return sum(p.tris for p in parts)
