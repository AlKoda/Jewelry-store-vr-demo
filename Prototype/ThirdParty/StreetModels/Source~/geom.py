"""Small procedural geometry kit with UVs (metres-based) for project-made street props."""
import numpy as np
import trimesh


class Part:
    def __init__(self, v, f, uv, mat):
        self.v = np.asarray(v, float); self.f = np.asarray(f, int); self.uv = np.asarray(uv, float); self.mat = mat

    def moved(self, M):
        v = np.c_[self.v, np.ones(len(self.v))] @ np.asarray(M).T
        return Part(v[:, :3], self.f, self.uv, self.mat)

    def t(self, x=0, y=0, z=0):
        return self.moved(trimesh.transformations.translation_matrix([x, y, z]))

    def ry(self, deg):
        return self.moved(trimesh.transformations.rotation_matrix(np.radians(deg), [0, 1, 0]))

    def rx(self, deg):
        return self.moved(trimesh.transformations.rotation_matrix(np.radians(deg), [1, 0, 0]))

    def rz(self, deg):
        return self.moved(trimesh.transformations.rotation_matrix(np.radians(deg), [0, 0, 1]))

    def uvmap(self, rect):
        """Remap 0..1 uv into atlas rect (u0, v0, u1, v1)."""
        u0, v0, u1, v1 = rect
        uv = self.uv.copy(); uv[:, 0] = u0 + uv[:, 0] * (u1 - u0); uv[:, 1] = v0 + uv[:, 1] * (v1 - v0)
        return Part(self.v, self.f, uv, self.mat)

    def with_mat(self, mat):
        return Part(self.v, self.f, self.uv, mat)


def box(sx, sy, sz, mat, uvscale=1.0, center=(0, 0, 0), atlas=None):
    """Axis box centred at center; each face gets planar UVs in metres (or 0..1 if atlas)."""
    hx, hy, hz = sx / 2, sy / 2, sz / 2
    cx, cy, cz = center
    faces = [  # normal, corners (ccw seen from outside), (u axis, v axis)
        ((0, 0, 1), [(-hx, -hy, hz), (hx, -hy, hz), (hx, hy, hz), (-hx, hy, hz)]),
        ((0, 0, -1), [(hx, -hy, -hz), (-hx, -hy, -hz), (-hx, hy, -hz), (hx, hy, -hz)]),
        ((1, 0, 0), [(hx, -hy, hz), (hx, -hy, -hz), (hx, hy, -hz), (hx, hy, hz)]),
        ((-1, 0, 0), [(-hx, -hy, -hz), (-hx, -hy, hz), (-hx, hy, hz), (-hx, hy, -hz)]),
        ((0, 1, 0), [(-hx, hy, hz), (hx, hy, hz), (hx, hy, -hz), (-hx, hy, -hz)]),
        ((0, -1, 0), [(-hx, -hy, -hz), (hx, -hy, -hz), (hx, -hy, hz), (-hx, -hy, hz)]),
    ]
    V, F, UV = [], [], []
    for n, cs in faces:
        b = len(V)
        p = np.array(cs, float)
        e1 = p[1] - p[0]; e2 = p[3] - p[0]
        L1, L2 = np.linalg.norm(e1), np.linalg.norm(e2)
        for i, c in enumerate(p):
            V.append(c + center)
        if atlas is None:
            UV += [(0, 0), (L1 * uvscale, 0), (L1 * uvscale, L2 * uvscale), (0, L2 * uvscale)]
        else:
            UV += [(0, 0), (1, 0), (1, 1), (0, 1)]
        F += [(b, b + 1, b + 2), (b, b + 2, b + 3)]
    return Part(V, F, UV, mat)


def quad(p0, p1, p2, p3, mat, uv=((0, 0), (1, 0), (1, 1), (0, 1))):
    return Part([p0, p1, p2, p3], [(0, 1, 2), (0, 2, 3)], uv, mat)


def lathe(profile, mat, seg=24, uvscale=1.0, a0=0.0, a1=2 * np.pi, cap_top=False, cap_bottom=False):
    """Revolve profile [(r, y), ...] (bottom to top) around Y. v = arc length along profile (m), u = circumference (m)."""
    prof = np.asarray(profile, float)
    full = abs(a1 - a0 - 2 * np.pi) < 1e-6
    n = seg + 1
    ang = np.linspace(a0, a1, n)
    s = np.r_[0, np.cumsum(np.linalg.norm(np.diff(prof, axis=0), axis=1))]
    rmax = max(prof[:, 0].max(), 1e-3)
    V, UV, F = [], [], []
    for i, (r, y) in enumerate(prof):
        for j, a in enumerate(ang):
            V.append((r * np.cos(a), y, r * np.sin(a)))
            UV.append(((a - a0) * rmax * uvscale, s[i] * uvscale))
    for i in range(len(prof) - 1):
        for j in range(seg):
            a = i * n + j; b = a + 1; c = a + n; d = c + 1
            F += [(a, c, b), (b, c, d)]
    part = Part(V, F, UV, mat)
    parts = [part]
    if cap_top and prof[-1, 0] > 1e-6:
        parts.append(disc(prof[-1, 0], prof[-1, 1], mat, seg, up=True))
    if cap_bottom and prof[0, 0] > 1e-6:
        parts.append(disc(prof[0, 0], prof[0, 1], mat, seg, up=False))
    return merge(parts)


def disc(r, y, mat, seg=24, up=True):
    V = [(0, y, 0)]; UV = [(0.5 * r, 0.5 * r)]
    for j in range(seg + 1):
        a = 2 * np.pi * j / seg
        V.append((r * np.cos(a), y, r * np.sin(a))); UV.append((r * (0.5 + 0.5 * np.cos(a)), r * (0.5 + 0.5 * np.sin(a))))
    F = []
    for j in range(seg):
        F.append((0, j + 2, j + 1) if up else (0, j + 1, j + 2))
    return Part(V, F, UV, mat)


def cylinder(r, h, mat, seg=20, caps=True, y0=0.0):
    return lathe([(r, y0), (r, y0 + h)], mat, seg, cap_top=caps, cap_bottom=caps)


def tube(path, r, mat, seg=12, closed=False):
    """Sweep a circle of radius r along a polyline path (N x 3). UV: u around (m), v along (m)."""
    P = np.asarray(path, float)
    N = len(P)
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1)[:, None]
    up = np.array([0, 0, 1.0]) if abs(T[0][2]) < 0.9 else np.array([1.0, 0, 0])
    Nn = np.cross(T[0], up); Nn /= np.linalg.norm(Nn)
    V, UV, F = [], [], []
    s = np.r_[0, np.cumsum(np.linalg.norm(np.diff(P, axis=0), axis=1))]
    frames = []
    n = Nn
    for i in range(N):
        n = n - T[i] * np.dot(n, T[i]); n /= np.linalg.norm(n)
        b = np.cross(T[i], n)
        frames.append((n, b))
    for i in range(N):
        n, b = frames[i]
        for j in range(seg + 1):
            a = 2 * np.pi * j / seg
            V.append(P[i] + r * (np.cos(a) * n + np.sin(a) * b))
            UV.append((a * r, s[i]))
    for i in range(N - 1):
        for j in range(seg):
            a = i * (seg + 1) + j; bb = a + 1; c = a + seg + 1; d = c + 1
            F += [(a, bb, c), (bb, d, c)]
    return Part(V, F, UV, mat)


def arc_path(center, radius, a0, a1, n, plane='xy'):
    a = np.linspace(a0, a1, n)
    cx, cy, cz = center
    if plane == 'xy':
        return np.c_[cx + radius * np.cos(a), cy + radius * np.sin(a), np.full(n, cz)]
    return np.c_[cx + radius * np.cos(a), np.full(n, cy), cz + radius * np.sin(a)]


def merge(parts):
    """Merge parts with the same material into one Part per material (returns list if mixed)."""
    if not parts:
        return []
    mats = {p.mat for p in parts}
    if len(mats) == 1:
        V, F, UV = [], [], []
        off = 0
        for p in parts:
            V.append(p.v); F.append(p.f + off); UV.append(p.uv); off += len(p.v)
        return Part(np.vstack(V), np.vstack(F), np.vstack(UV), parts[0].mat)
    raise ValueError('mixed materials')


def flatten(items):
    out = []
    for it in items:
        if isinstance(it, (list, tuple)):
            out += flatten(it)
        else:
            out.append(it)
    return out


def to_groups(parts):
    """Group parts by material -> list of (mat, trimesh with uv)."""
    parts = flatten(parts)
    by = {}
    for p in parts:
        by.setdefault(p.mat, []).append(p)
    groups = []
    for mat, ps in by.items():
        m = merge(ps)
        tm = trimesh.Trimesh(m.v, m.f, process=False)
        tm.visual = trimesh.visual.TextureVisuals(uv=m.uv)
        groups.append((mat, tm))
    return groups
