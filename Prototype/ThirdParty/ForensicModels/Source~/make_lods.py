"""LOD variants of four ForensicModels props: python3 -I make_lods.py PACKDIR [--report OUT.json] [Model ...]
Writes <Model>_LOD1.obj and <Model>_LOD2.obj to PACKDIR for ForensicCase, Crowbar, Sledgehammer and FlatheadScrewdriver.
decimate_exact.py (which reproduces the shipped pack) stays as it is; this is a copy of the lods-street tool,
Prototype/ThirdParty/StreetModels/Source~/make_lods.py as of 2026-10-15 (SHA-256 3a85204f2f97b59c...), which extends
decimate_exact.py: split per corner, decimate per group, keep original vertices. Changed here: the model table, the
leading comment lines of the source OBJ are kept (as in the PeopleModels copy), and the VERIFY and sources.json parts
listed at the end. From the lods-street tool:
- Faces are fan-triangulated and split per (v, vt, vn) corner value; vn is optional (a model without vn stays without vn).
- Groups are runs of faces under one g/usemtl state (usemtl may come before or after g); their g/usemtl lines, names and
  order are written back unchanged. 'mtllib' names the ORIGINAL MTL (shared, never copied).
- Groups under 64 triangles and groups on the model's keep list are copied unchanged (their positions are locked in the
  others); every other group is decimated to its proportional share of what the target leaves (explicit targets first).
- Decimation is a seam-aware half-edge collapse with quadric error (meshoptimizer's rules). The decimated groups form one
  mesh, split per (group, v, vt, vn): positions are welded only to find neighbours, so hard edges, UV seams and group
  boundaries are borders that shorten only along themselves, on both sides at once (no cracks); open borders move only
  along the border; a line where three or more sheets meet (a panel edge lying on another part) shortens only along
  itself, on all sheets at once. No vertex moves: every kept vertex is an original v/vt/vn, written as its original
  text. The last vertex on each face of a group's bounding box is never removed, so every group box stays exact. No
  triangle turns more than ~78 degrees in one collapse, and none over 1 cm2 may end up facing more than 90 degrees away
  from the source triangle it comes from (no inside-out folds, which Unity would cull).
- relax (LOD2 only; LOD1 is seam-exact): a vertex may also collapse across a seam or where three or more corners meet;
  the corners that the collapse edge does not map take the target's corner with the nearest normal/UV in the same group
  (never further than uv_max in UV) and that difference, times the area it covers, is added to the cost (attribute-aware
  quadric error). Vertices still keep their original v/vt/vn and the geometry stays closed (no cracks).
- Deterministic (no randomness, sorted tie-breaks): a rerun reproduces the same bytes.
Here every level is seam-exact (no relax): all eight reach their targets without crossing a seam, and a relaxed
ForensicCase_LOD2 smeared the left latch.
VERIFY (in memory, before anything is written; exit 1 naming the model and the failing measure): triangles within 10 %
of the target, bounds within 5 mm of the source (trimesh, process=False), base on y = 0 (lowest vertex within 0.01 mm),
the tools upright (long axis = Y: the Y size is the largest), same usemtl set, the same g/usemtl order, vt/vn presence
as in the source (the three tools have no vn and stay without), every v/vt/vn index valid, every face a triangle, no
zero-area triangle (< 1e-10 m2 as written), every corner an original (v, vt, vn) value, copied groups identical, no new
open edges (position-welded over the whole model: a crack would add some), no more triangles wound against the sum of
their own vertex normals than the source (ForensicCase; the tools have no vn). Reported, not failed: the surface
deviation, i.e. the distance from every source vertex and triangle centre to the LOD surface (mean, 99th percentile,
max). A failure exits non-zero and writes nothing.
After writing, sources.json gets a files entry per LOD (bytes, SHA-256, derived_from, tool, triangles, derivation,
VERIFY measures), an entry for this script and a top-level 'lod_variants' block; every other entry is checked to be
unchanged. A rerun gives the same bytes.
Run from the repository root: python3 -I Prototype/ThirdParty/ForensicModels/Source~/make_lods.py
Prototype/ThirdParty/ForensicModels. The build needs only the standard library; -I also hides the user
site-packages, so that one directory (never the script dir or the cwd) is appended to import trimesh (and numpy, which
trimesh needs) for VERIFY."""
import sys, os, io, site, json, heapq, hashlib, math

MIN_KEEP = 64            # groups under this many triangles are copied unchanged
MIN_SHARE = 32           # floor of a decimated group's share (as in decimate_exact.py)
BORDER_W, SEAM_W = 10.0, 1.0   # edge-constraint quadric weights: open borders, UV/normal/group seams
ATTR_L2 = 1e-4           # m2: an attribute difference of 0.1 costs like a 1 mm offset over 1 cm
UV_W = 100.0             # attribute distance = |dn|^2 + UV_W |duv|^2 (0.01 UV ~ 0.1 normal)
UV_MAX = 0.02            # default relax UV limit: a corner never moves further in UV (about 20 texels at 1K)
LEN_W = 1e-10            # tie-break on flat areas: shorter edges first (keeps triangles compact)
MIN_COS = 0.2            # reject a collapse that turns any triangle by more than ~78 degrees
FOLD_AREA = 1e-4         # m2: a triangle larger than 1 cm2 may never face more than 90 degrees away from its source triangle
MIN_AREA = 1e-9          # m2, 10x the zero-area limit of strip_degenerate.py
BOX_TOL = 1e-6           # m: vertices this close to a face of their group's box hold that face
BOUNDS_TOL = 0.005       # m: LOD bounds within 5 mm of the source bounds
FLOOR_TOL = 1e-5         # m: the lowest vertex sits on y = 0
MODELS = {
    # keep: copied unchanged ('mtl:X' = every group using material X); fixed: explicit per-group targets;
    # relax: 'early' (relaxed from the start) or 'late' (only once seam-exact collapses run out); uv_max: relax UV limit.
    # All four are single-group, single-material models and every level is seam-exact (see the docstring).
    'ForensicCase': {'LOD1': dict(target=1600), 'LOD2': dict(target=500)},
    'Crowbar': {'LOD1': dict(target=600), 'LOD2': dict(target=200)},
    'Sledgehammer': {'LOD1': dict(target=650), 'LOD2': dict(target=200)},
    'FlatheadScrewdriver': {'LOD1': dict(target=600), 'LOD2': dict(target=200)},
}
TOOLS = ('Crowbar', 'Sledgehammer', 'FlatheadScrewdriver')   # stand upright: long axis = Y, no vn
CHECKS = {   # extra VERIFY measures per model/level: base_y0 = lowest vertex on y = 0, upright = Y is the longest axis
    (name, level): dict(base_y0=True, upright=name in TOOLS, no_vn=name in TOOLS) for name in MODELS for level in MODELS[name]
}
# Suggested Unity LODGroup screen-relative transition heights (LOD0 -> LOD1, LOD1 -> LOD2, LOD2 -> culled), LOD Bias 1.
# Suggestions only, unverified in Unity; the README converts them to distances.
SWITCH = {name: (0.20, 0.06, 0.01) for name in MODELS}
TOOL_ID = 'Source~/make_lods.py'
STREET_TOOL = ('Prototype/ThirdParty/StreetModels/Source~/make_lods.py',
               '3a85204f2f97b59c04a34fe1b0c74e64d825731fedba82f9adcb95993d15aaeb')
LOCKED, MANIFOLD, BORDER, SEAM, CHAIN = range(5)   # vertex kinds (CHAIN comes as (CHAIN, (p, q), k))


def parse(path):
    """-> dict(V, T, N: [(text, floats)], head: mtllib lines, segs: [dict(head, g, mtl, faces)], tail, notes)"""
    V, T, N, head, segs, pend, notes = [], [], [], [], [], [], []
    g = mtl = cur = None
    for no, raw in enumerate(open(path, encoding='utf-8'), 1):
        line = raw.strip()
        if line[:1] == '#' and not (V or head or segs or pend):
            notes.append(line)                  # leading comments (pack, units, origin) are kept
        if not line or line[0] == '#':
            continue
        p = line.split()
        k = p[0]
        if k in ('v', 'vt', 'vn'):
            {'v': V, 'vt': T, 'vn': N}[k].append((' '.join(p[1:]), tuple(float(x) for x in p[1:])))
        elif k == 'mtllib':
            head.append(line)
        elif k in ('g', 'usemtl', 's', 'o'):
            pend.append(line)
            if k == 'g':
                g = line[1:].strip()
            elif k == 'usemtl':
                mtl = line[6:].strip()
            cur = None          # the next face starts a new group
        elif k == 'f':
            if cur is None:
                cur = dict(head=pend, g=g, mtl=mtl, faces=[]); segs.append(cur); pend = []
            cs = []
            for tok in p[1:]:
                ids = (tok.split('/') + ['', ''])[:3]
                c = []
                for x, L in zip(ids, (V, T, N)):
                    if x == '':
                        c.append(-1); continue
                    i = int(x)
                    i = i - 1 if i > 0 else len(L) + i
                    if not 0 <= i < len(L):
                        sys.exit('%s:%d: index %s out of range' % (path, no, x))
                    c.append(i)
                cs.append(tuple(c))
            for j in range(1, len(cs) - 1):      # fan triangulation
                cur['faces'].append((cs[0], cs[j], cs[j + 1]))
        else:
            sys.exit('%s:%d: unsupported OBJ statement %r' % (path, no, k))
    return dict(V=V, T=T, N=N, head=head, segs=segs, tail=pend, notes=notes)


def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def norm(a): return math.sqrt(dot(a, a))


def plane_q(n, d, w):
    a, b, c = n
    return [w * a * a, w * a * b, w * a * c, w * a * d, w * b * b, w * b * c, w * b * d, w * c * c, w * c * d, w * d * d]


def q_add(q, r):
    for i in range(10):
        q[i] += r[i]


def q_eval(q, p):
    x, y, z = p
    return (q[0] * x * x + 2 * q[1] * x * y + 2 * q[2] * x * z + 2 * q[3] * x + q[4] * y * y + 2 * q[5] * y * z
            + 2 * q[6] * y + q[7] * z * z + 2 * q[8] * z + q[9])


class Mesh:
    """The decimated groups of one model, split per (group, v, vt, vn) corner, with the half-edge collapse state."""

    def __init__(self, M, sids, locked_pos):
        V, T, N = M['V'], M['T'], M['N']
        cid, wid = {}, {}
        self.corner, self.cw, self.cn, self.cuv, self.pos = [], [], [], [], []  # corner: (group, vi, ti, ni), vertex, normal, uv
        self.tris, self.tc, self.ts = [], [], []                                # triangle: vertices, corners, group
        for s in sids:
            for f in M['segs'][s]['faces']:
                cs = []
                for vi, ti, ni in f:
                    key = (s, V[vi][1], T[ti][1] if ti >= 0 else None, N[ni][1] if ni >= 0 else None)
                    c = cid.get(key)
                    if c is None:
                        c = cid[key] = len(self.corner)
                        self.corner.append((s, vi, ti, ni)); self.cuv.append(key[2]); self.cn.append(key[3])
                        w = wid.get(key[1][:3])
                        if w is None:
                            w = wid[key[1][:3]] = len(self.pos); self.pos.append(key[1][:3])
                        self.cw.append(w)
                    cs.append(c)
                self.tris.append([self.cw[c] for c in cs]); self.tc.append(cs); self.ts.append(s)
        n = len(self.pos)
        self.vt = [set() for _ in range(n)]
        self.mem = [{} for _ in range(n)]          # live triangles per group at each vertex
        self.nseg = {s: 0 for s in sids}
        for t, w in enumerate(self.tris):
            self.nseg[self.ts[t]] += 1
            for x in w:
                self.vt[x].add(t); self.mem[x][self.ts[t]] = self.mem[x].get(self.ts[t], 0) + 1
        self.alive = [True] * len(self.tris)
        self.locked = {w for w in range(n) if self.pos[w] in locked_pos}
        for w in self.tris:
            if len(set(w)) < 3:
                self.locked |= set(w)
        # quadrics: area-weighted face planes plus edge planes along open borders and seams
        self.Q = [[0.0] * 10 for _ in range(n)]
        fn, edges = [], {}
        self.fn0 = fn                                # source face normal of every triangle (fold guard)
        for t, w in enumerate(self.tris):
            p0, p1, p2 = (self.pos[x] for x in w)
            nr = cross(sub(p1, p0), sub(p2, p0)); ln = norm(nr)
            nn = (nr[0] / ln, nr[1] / ln, nr[2] / ln) if ln > 0 else (0.0, 0.0, 0.0)
            fn.append(nn)
            if ln > 0:
                q = plane_q(nn, -dot(nn, p0), ln / 2)
                for x in w:
                    q_add(self.Q[x], q)
            for i in range(3):
                a, b = w[i], w[(i + 1) % 3]
                edges.setdefault((min(a, b), max(a, b)), []).append(t)
        for (a, b), ts in edges.items():
            if len(ts) == 1:
                wt = BORDER_W
            elif len(ts) == 2 and (self.corner_at(ts[0], a) != self.corner_at(ts[1], a) or
                                   self.corner_at(ts[0], b) != self.corner_at(ts[1], b)):
                wt = SEAM_W
            else:
                continue
            e = sub(self.pos[b], self.pos[a]); le = norm(e)
            for t in ts:
                m = cross(e, fn[t]); lm = norm(m)
                if lm == 0 or le == 0:
                    continue
                m = (m[0] / lm, m[1] / lm, m[2] / lm)
                q = plane_q(m, -dot(m, self.pos[a]), wt * le * le)
                q_add(self.Q[a], q); q_add(self.Q[b], q)
        # group-box guards: ext_of[w] = (group, box face) held by w; ext_n = vertices still holding each one.
        # A vertex never joins a group it was not in (corners map within a group), so holders only get fewer.
        self.ext_of = [[] for _ in range(n)]
        self.ext_n = {}
        for s in sids:
            P = [self.pos[w] for w in range(n) if s in self.mem[w]]
            lo = [min(p[k] for p in P) for k in range(3)]
            hi = [max(p[k] for p in P) for k in range(3)]
            for w in range(n):
                if s in self.mem[w]:
                    for k in range(3):
                        for face, ref in ((2 * k, lo[k]), (2 * k + 1, hi[k])):
                            if abs(self.pos[w][k] - ref) <= BOX_TOL:
                                self.ext_of[w].append((s, face)); self.ext_n[(s, face)] = self.ext_n.get((s, face), 0) + 1
        self.ver = [0] * n
        self.kc = {}
        self.heap = []
        self.relaxed = False
        self.uv_max = UV_MAX
        self.relaxed_collapses = 0
        self.goal = dict(self.nseg)

    def corner_at(self, t, x):
        return self.tc[t][self.tris[t].index(x)]

    def nbrs(self, x):
        s = set()
        for t in self.vt[x]:
            s.update(self.tris[t])
        s.discard(x)
        return s

    def kind(self, u):
        c = self.kc.get(u)
        if c is None or c[0] != self.ver[u]:
            c = self.kc[u] = (self.ver[u], self.classify(u))
        return c[1]

    def classify(self, u):
        if u in self.locked or not self.vt[u]:
            return LOCKED
        mult = {}
        for t in self.vt[u]:
            for x in self.tris[t]:
                if x != u:
                    mult[x] = mult.get(x, 0) + 1
        chain = sorted(x for x in mult if mult[x] > 2)
        if chain:
            return self.classify_chain(u, chain, mult)
        succ, pred, cus = {}, {}, set()
        for t in self.vt[u]:
            w = self.tris[t]; i = w.index(u)
            a, b = w[(i + 1) % 3], w[(i + 2) % 3]
            if a in succ or b in pred:
                return LOCKED                       # non-manifold or inconsistent winding
            succ[a] = t; pred[b] = t; cus.add(self.tc[t][i])
        starts = [a for a in succ if a not in pred]
        if len(starts) > 1:
            return LOCKED
        x = first = starts[0] if starts else min(succ)
        cnt = 0
        while x in succ and cnt <= len(succ):       # walk the fan: it must be one disc or one half-disc
            w = self.tris[succ[x]]; x = w[(w.index(u) + 2) % 3]; cnt += 1
            if x == first:
                break
        if cnt != len(self.vt[u]):
            return LOCKED
        if self.relaxed:
            return MANIFOLD if not starts else BORDER
        seams = sum(1 for x in succ if x in pred and (self.corner_at(succ[x], u) != self.corner_at(pred[x], u) or
                                                      self.corner_at(succ[x], x) != self.corner_at(pred[x], x)))
        if not starts:
            return MANIFOLD if len(cus) == 1 and seams == 0 else SEAM if len(cus) == 2 and seams == 2 else LOCKED
        return BORDER if len(cus) == 1 and seams == 0 else LOCKED

    def classify_chain(self, u, chain, mult):
        """u on a line where k >= 3 sheets meet (non-manifold edges u-p and u-q, e.g. a panel edge on another part):
        if every sheet is a half-disc from p to q with consistent winding, u may slide onto p or q on all sheets at
        once (k triangles go), like a seam with k sides. -> (CHAIN, (p, q), k) or LOCKED"""
        if len(chain) != 2:
            return LOCKED
        p, q = chain
        k = mult[p]
        if mult[q] != k:
            return LOCKED
        ts = sorted(self.vt[u])
        par = {t: t for t in ts}

        def root(t):
            while par[t] != t:
                par[t] = par[par[t]]; t = par[t]
            return t
        by = {}
        for t in ts:
            w = self.tris[t]; i = w.index(u)
            for j, x in ((1, w[(i + 1) % 3]), (2, w[(i + 2) % 3])):
                if x not in (p, q):
                    by.setdefault(x, []).append((t, j))
        for l in by.values():
            if len(l) != 2 or l[0][1] == l[1][1]:
                return LOCKED                       # open border, a further sheet, or flipped winding inside a sheet
            par[root(l[0][0])] = root(l[1][0])
        sheets = {}
        for t in ts:
            sheets.setdefault(root(t), []).append(t)
        if len(sheets) != k:
            return LOCKED
        for sh in sheets.values():
            if sum(1 for t in sh if p in self.tris[t]) != 1 or sum(1 for t in sh if q in self.tris[t]) != 1:
                return LOCKED
            if not self.relaxed and len({self.corner_at(t, u) for t in sh}) != 1:
                return LOCKED                       # seam-exact: one corner per sheet
        return (CHAIN, (p, q), k)

    def adist(self, a, b):
        d = 0.0
        if self.cn[a] is not None:
            d += sum((x - y) ** 2 for x, y in zip(self.cn[a], self.cn[b]))
        if self.cuv[a] is not None:
            d += UV_W * sum((x - y) ** 2 for x, y in zip(self.cuv[a], self.cuv[b]))
        return d

    def evaluate(self, u, v):
        """(cost, corner map, relaxed?) of collapsing u onto v, or None if not allowed now"""
        k = self.kind(u)
        if k == LOCKED:
            return None
        shared = [t for t in self.vt[u] if v in self.tris[t]]
        if isinstance(k, tuple):
            if v not in k[1] or len(shared) != k[2]:
                return None                          # a k-sheet line vertex moves only along the line
        elif len(shared) != (1 if k == BORDER else 2):
            return None                              # border vertices move only along the border
        for t in shared:
            if self.nseg[self.ts[t]] <= self.goal[self.ts[t]]:
                return None                          # that group has reached its share
        mapc = {}
        for t in shared:
            cu, cv = self.corner_at(t, u), self.corner_at(t, v)
            if mapc.setdefault(cu, cv) != cv:
                return None
        others = [t for t in self.vt[u] if t not in shared]
        need = sorted({self.corner_at(t, u) for t in others} - set(mapc))
        if need and not self.relaxed:
            return None                              # a seam vertex moves only along its seam (both sides at once)
        opp = {x for t in shared for x in self.tris[t]} - {u, v}
        if any(self.ext_of[x] for x in opp | {u, v}) and not self.box_ok(u, v, shared, others, opp):
            return None                              # would remove the last vertex holding a face of a group box
        if self.nbrs(u) & self.nbrs(v) != opp:
            return None                              # link condition (no non-manifold edge)
        pu, pv = self.pos[u], self.pos[v]
        vset = {frozenset(self.tris[t]) for t in self.vt[v] if t not in shared}
        area = {}
        for t in others:
            w = self.tris[t]; i = w.index(u)
            a, b = w[(i + 1) % 3], w[(i + 2) % 3]
            n0 = cross(sub(self.pos[a], pu), sub(self.pos[b], pu))
            n1 = cross(sub(self.pos[a], pv), sub(self.pos[b], pv))
            l0, l1 = norm(n0), norm(n1)
            if l1 / 2 < MIN_AREA or dot(n0, n1) < MIN_COS * l0 * l1 or frozenset((v, a, b)) in vset:
                return None
            if l1 / 2 > FOLD_AREA and dot(n1, self.fn0[t]) <= 0 and any(self.fn0[t]):
                return None                          # would face away from the source triangle it comes from (a fold)
            area[self.tc[t][i]] = area.get(self.tc[t][i], 0.0) + l0 / 2
        cost = q_eval(self.Q[u], pv)
        if self.cn[0] is not None:
            for cu, cv in mapc.items():
                cost += ATTR_L2 * area.get(cu, 0.0) * sum((x - y) ** 2 for x, y in zip(self.cn[cu], self.cn[cv]))
        if need:                                     # relax: nearest corner of v in the same group
            vc = sorted({self.corner_at(t, v) for t in self.vt[v]})
            for c in need:
                cand = [(self.adist(c, c2), c2) for c2 in vc if self.corner[c2][0] == self.corner[c][0] and
                        (self.cuv[c] is None or sum((x - y) ** 2 for x, y in zip(self.cuv[c], self.cuv[c2])) <= self.uv_max ** 2)]
                if not cand:
                    return None
                d, c2 = min(cand)
                mapc[c] = c2
                cost += ATTR_L2 * area[c] * d
        e = sub(pu, pv)
        return cost + LEN_W * dot(e, e), mapc, bool(need)

    def mem_delta(self, u, v, shared, others):
        d = {}
        for t in shared:
            for x in self.tris[t]:
                d[(x, self.ts[t])] = d.get((x, self.ts[t]), 0) - 1
        for t in others:
            s = self.ts[t]
            d[(u, s)] = d.get((u, s), 0) - 1; d[(v, s)] = d.get((v, s), 0) + 1
        return d

    def box_losses(self, u, v, shared, others, opp):
        d = self.mem_delta(u, v, shared, others)
        loss = {}
        for x in sorted(opp | {u, v}):
            for key in self.ext_of[x]:
                m = self.mem[x].get(key[0], 0)
                if m > 0 and m + d.get((x, key[0]), 0) <= 0:
                    loss[key] = loss.get(key, 0) + 1
        return loss

    def box_ok(self, u, v, shared, others, opp):
        return all(self.ext_n[key] > n for key, n in self.box_losses(u, v, shared, others, opp).items())

    def push(self, u):
        best = None
        if self.kind(u) != LOCKED:
            for v in sorted(self.nbrs(u)):
                r = self.evaluate(u, v)
                if r is not None and (best is None or r[0] < best[0]):
                    best = (r[0], v)
        if best is not None:
            heapq.heappush(self.heap, (best[0], u, best[1], self.ver[u]))

    def collapse(self, u, v, mapc):
        shared = [t for t in self.vt[u] if v in self.tris[t]]
        others = [t for t in self.vt[u] if t not in shared]
        opp = {x for t in shared for x in self.tris[t]} - {u, v}
        for key, n in self.box_losses(u, v, shared, others, opp).items():
            self.ext_n[key] -= n
        for (x, s), dd in self.mem_delta(u, v, shared, others).items():
            m = self.mem[x].get(s, 0) + dd
            if m > 0:
                self.mem[x][s] = m
            else:
                self.mem[x].pop(s, None)
        for t in shared:
            self.alive[t] = False; self.nseg[self.ts[t]] -= 1
            for x in self.tris[t]:
                self.vt[x].discard(t)
        for t in others:
            i = self.tris[t].index(u)
            self.tris[t][i] = v; self.tc[t][i] = mapc[self.tc[t][i]]
            self.vt[v].add(t)
        self.vt[u] = set()
        q_add(self.Q[v], self.Q[u])
        for x in [v] + sorted(self.nbrs(v)):
            self.ver[x] += 1
            self.push(x)

    def run(self, goal, relax=None):
        """collapse greedily until every group is at its share (or nothing is left to collapse). relax 'early': relaxed
        from the start; 'late': a second, relaxed pass once the seam-exact collapses run out. Lowering the goals and
        calling again continues."""
        self.goal = dict(goal)
        if relax == 'early' and not self.relaxed:
            self.relaxed = True; self.kc = {}
        while True:
            self.heap = []
            for u in range(len(self.pos)):
                self.push(u)
            while self.heap and any(self.nseg[s] > self.goal[s] for s in self.nseg):
                cost, u, v, ver = heapq.heappop(self.heap)
                if ver != self.ver[u] or not self.vt[u]:
                    continue
                r = self.evaluate(u, v)
                if r is None:
                    self.push(u)                     # neighbourhood or shares changed: find u's best collapse again
                    continue
                self.collapse(u, v, r[1])
                self.relaxed_collapses += r[2]
            if self.relaxed or not relax or not any(self.nseg[s] > self.goal[s] for s in self.nseg):
                return
            self.relaxed = True
            self.kc = {}

    def faces(self, s):
        return [tuple(self.corner[c][1:] for c in self.tc[t]) for t in range(len(self.tris)) if self.alive[t] and self.ts[t] == s]


def selects(sel, seg):
    return sel == 'mtl:' + str(seg['mtl']) or sel == seg['g']


def build(M, cfg, name):
    """-> (list of (seg, faces, mode, share) in original order, relaxed collapse count); mode copy / fixed / share"""
    segs, target = M['segs'], cfg['target']
    keep, fixed = cfg.get('keep', []), cfg.get('fixed', {})
    for sel in keep + list(fixed):
        if not any(selects(sel, s) for s in segs):
            sys.exit('%s: keep/fixed selector %r matches no group' % (name, sel))
    mode, goal = {}, {}
    for i, s in enumerate(segs):
        n = len(s['faces'])
        fx = [fixed[k] for k in fixed if selects(k, s)]
        if n < MIN_KEEP or any(selects(k, s) for k in keep):
            mode[i] = 'copy'
        elif fx:
            mode[i] = 'fixed'; goal[i] = min(n, fx[0])
        else:
            mode[i] = 'share'
    locked_pos = {M['V'][c[0]][1][:3] for i in mode if mode[i] == 'copy' for f in segs[i]['faces'] for c in f}
    sids = [i for i in mode if mode[i] != 'copy']
    free = [i for i in sids if mode[i] == 'share']
    fixed_n = sum(len(segs[i]['faces']) for i in mode if mode[i] == 'copy') + sum(goal[i] for i in sids if mode[i] == 'fixed')
    nfree = sum(len(segs[i]['faces']) for i in free)
    for i in free:
        goal[i] = min(len(segs[i]['faces']), max(MIN_SHARE, int(round((target - fixed_n) * len(segs[i]['faces']) / nfree))))
    mesh = Mesh(M, sids, locked_pos) if sids else None
    if mesh:
        mesh.uv_max = cfg.get('uv_max', UV_MAX)
    for _ in range(8):           # groups that stall above their share: the others take the excess
        if mesh is None:
            break
        mesh.run(goal, cfg.get('relax'))
        over = sum(len(segs[i]['faces']) for i in mode if mode[i] == 'copy') + sum(mesh.nseg.values()) - target
        adj = [i for i in free if mesh.nseg[i] <= goal[i] and goal[i] > MIN_SHARE]
        if over <= 0 or not adj:
            break
        tot = sum(goal[i] for i in adj)
        for i in adj:
            goal[i] = max(MIN_SHARE, goal[i] - int(math.ceil(over * goal[i] / tot)))
    out = []
    for i, s in enumerate(segs):
        if mode[i] == 'copy':
            out.append((s, list(s['faces']), 'copy', len(s['faces'])))
        else:
            out.append((s, mesh.faces(i), mode[i], goal[i]))
    return out, (mesh.relaxed_collapses if mesh else 0)


def write(M, groups, name, level, target, mtl_lines):
    V, T, N = M['V'], M['T'], M['N']
    ntri = sum(len(f) for _, f, _, _ in groups)
    L = M['notes'] + ['# %s_%s - LOD variant of %s.obj (target %d tris): %d tris, made by Source~/make_lods.py'
                      % (name, level, name, target, ntri),
                      '# Every vertex is an original %s of %s.obj; materials come from the original MTL (shared, not '
                      'copied).' % ('v/vt/vn' if N else 'v/vt (no vn, as in the source)', name)]
    L += mtl_lines
    base = [0, 0, 0]
    for s, faces, _, _ in groups:
        L += s['head']
        used = [sorted({c[k] for f in faces for c in f if c[k] >= 0}) for k in range(3)]
        idx = [{o: base[k] + j + 1 for j, o in enumerate(used[k])} for k in range(3)]
        for k, tag, src in ((0, 'v', V), (1, 'vt', T), (2, 'vn', N)):
            L += ['%s %s' % (tag, src[o][0]) for o in used[k]]
            base[k] += len(used[k])
        for f in faces:
            cs = []
            for vi, ti, ni in f:
                a = str(idx[0][vi])
                if ti >= 0 and ni >= 0:
                    a += '/%d/%d' % (idx[1][ti], idx[2][ni])
                elif ti >= 0:
                    a += '/%d' % idx[1][ti]
                elif ni >= 0:
                    a += '//%d' % idx[2][ni]
                cs.append(a)
            L.append('f ' + ' '.join(cs))
    L += M['tail']
    return '\n'.join(L) + '\n'


def raw_obj(text):
    """minimal reader for VERIFY: v/vt/vn floats, usemtl names, triangles as (group, mtl, [(vi, ti, ni)] x 3),
    number of faces that were not triangles, problems"""
    V, T, N, F, mtls, bad, npoly = [], [], [], [], set(), [], 0
    g = m = None
    for line in text.split('\n'):
        p = line.split()
        if not p:
            continue
        if p[0] == 'v':
            V.append(tuple(float(x) for x in p[1:]))
        elif p[0] == 'vt':
            T.append(tuple(float(x) for x in p[1:]))
        elif p[0] == 'vn':
            N.append(tuple(float(x) for x in p[1:]))
        elif p[0] == 'g':
            g = line[1:].strip()
        elif p[0] == 'usemtl':
            m = line[6:].strip(); mtls.add(m)
        elif p[0] == 'f':
            cs = []
            for tok in p[1:]:
                ids = (tok.split('/') + ['', ''])[:3]
                cs.append(tuple(int(x) - 1 if x else -1 for x in ids))
            if len(cs) != 3:
                npoly += 1
                if len(cs) < 3:
                    bad.append('face with %d corners' % len(cs))
            for j in range(1, len(cs) - 1):     # fan triangulation, as in parse()
                F.append((g, m, [cs[0], cs[j], cs[j + 1]]))
    for _, _, cs in F:
        for c in cs:
            for i, L, tag in zip(c, (V, T, N), ('v', 'vt', 'vn')):
                if i >= len(L) or i < -1 or (tag == 'v' and i < 0):
                    bad.append('%s index %d of %d' % (tag, i + 1, len(L)))
    return dict(V=V, T=T, N=N, F=F, mtls=mtls, bad=bad, npoly=npoly)


def corner_vals(R, c):
    return (R['V'][c[0]], R['T'][c[1]] if c[1] >= 0 else None, R['N'][c[2]] if c[2] >= 0 else None)


def facing_away(R):
    """triangles wound against the sum of their vertex normals (back faces where the shading says front)"""
    n = 0
    for _, _, cs in R['F']:
        if all(x[2] >= 0 for x in cs):
            a, b, c = (R['V'][x[0]][:3] for x in cs)
            if dot(cross(sub(b, a), sub(c, a)), [sum(R['N'][x[2]][k] for x in cs) for k in range(3)]) < 0:
                n += 1
    return n


def open_edges(R):
    """position-welded open edges over the whole model (a crack between parts or groups adds some)"""
    key, E = {}, {}
    for _, _, cs in R['F']:
        w = [key.setdefault(R['V'][c[0]][:3], len(key)) for c in cs]
        for i in range(3):
            e = (min(w[i], w[(i + 1) % 3]), max(w[i], w[(i + 1) % 3]))
            E[e] = E.get(e, 0) + 1
    return sum(1 for n in E.values() if n == 1)


def load_trimesh():
    try:
        import trimesh
    except ImportError:
        sys.path.append(site.getusersitepackages())   # python3 -I hides it; add only this directory
        import trimesh
    return trimesh


def tm_scene(trimesh, data, pack):
    return trimesh.load(io.BytesIO(data.encode('utf-8')), file_type='obj', force='scene', process=False,
                        group_material=False, split_groups=True, skip_materials=True,
                        resolver=trimesh.resolvers.FilePathResolver(os.path.join(pack, 'x.obj')))


def deviation(R, src_R):
    """surface deviation in m: distance from every source vertex and every source triangle centre to the nearest LOD
    triangle -> (mean, 99th percentile, max). Brute force with numpy (no spatial index is needed at these sizes)."""
    import numpy as np
    tri = np.array([[R['V'][c[0]][:3] for c in cs] for _, _, cs in R['F']], float)
    src = np.array([[src_R['V'][c[0]][:3] for c in cs] for _, _, cs in src_R['F']], float)
    used = sorted({c[0] for _, _, cs in src_R['F'] for c in cs})
    P = np.concatenate([np.array([src_R['V'][i][:3] for i in used], float), src.mean(axis=1)])
    a, b, c = tri[:, 0], tri[:, 1], tri[:, 2]
    n = np.cross(b - a, c - a)
    nl = np.maximum(np.linalg.norm(n, axis=1), 1e-30)
    out = np.empty(len(P))
    step = max(1, 400000 // len(tri))
    for s in range(0, len(P), step):
        p = P[s:s + step, None, :]
        inside = np.ones((len(p), len(tri)), bool)       # the projection onto the plane lies inside the triangle
        de = np.full((len(p), len(tri)), np.inf)          # distance to the nearest of the three edges
        for u, v in ((a, b), (b, c), (c, a)):
            e = v - u
            inside &= np.einsum('ijk,jk->ij', np.cross(e, p - u), n) >= 0
            t = np.clip(np.einsum('ijk,jk->ij', p - u, e) / np.maximum(np.einsum('jk,jk->j', e, e), 1e-30), 0, 1)
            de = np.minimum(de, np.linalg.norm(p - (u + t[..., None] * e), axis=2))
        dp = np.abs(np.einsum('ijk,jk->ij', p - a, n)) / nl
        out[s:s + step] = np.where(inside, np.minimum(dp, de), de).min(axis=1)
    return float(out.mean()), float(np.percentile(out, 99)), float(out.max())


def verify(trimesh, name, level, text, src_text, src_R, groups, cfg, pack, mtl_lines):
    fails, meas = [], {}
    R = raw_obj(text)
    target = cfg['target']
    n = len(R['F'])
    meas['tris'] = n
    if abs(n - target) > 0.10 * target:
        fails.append('triangles %d not within 10 %% of %d' % (n, target))
    if R['bad'] or R['npoly']:
        fails.append('invalid v/vt/vn index or face: %s' % (R['bad'] or ['%d faces are not triangles' % R['npoly']])[0])
        return fails, meas
    sc, ss = tm_scene(trimesh, text, pack), tm_scene(trimesh, src_text, pack)
    tm_n = sum(len(g.faces) for g in sc.geometry.values())
    if tm_n != n:
        fails.append('trimesh reads %d triangles, file has %d' % (tm_n, n))
    db = float(abs(sc.bounds - ss.bounds).max())
    meas['bounds_max_diff_m'] = db
    meas['bounds'] = [[round(x, 4) for x in r] for r in sc.bounds.tolist()]
    if db > BOUNDS_TOL:
        fails.append('bounds differ by %.4f m (> %g m)' % (db, BOUNDS_TOL))
    gd = max(float(abs(sc.geometry[k].bounds - ss.geometry[k].bounds).max()) for k in ss.geometry)
    meas['group_box_max_diff_m'] = gd
    size = [float(x) for x in sc.bounds[1] - sc.bounds[0]]
    meas['size_m'] = [round(x, 4) for x in size]
    meas['centre_xz_m'] = [round(float(sc.bounds[0][k] + sc.bounds[1][k]) / 2, 5) for k in (0, 2)]
    chk = CHECKS.get((name, level), {})
    if chk.get('base_y0'):
        y = min(R['V'][c[0]][1] for _, _, cs in R['F'] for c in cs)
        meas['min_y'] = y
        if abs(y) > FLOOR_TOL:
            fails.append('lowest vertex at y = %.5f (base not on y = 0)' % y)
    if chk.get('upright'):
        meas['upright'] = size[1] > max(size[0], size[2])
        if not meas['upright']:
            fails.append('not upright: size %s m, Y is not the longest axis' % meas['size_m'])
    meas['vn'] = len(R['N'])
    if chk.get('no_vn') and R['N']:
        fails.append('%d vn lines (the source has none)' % len(R['N']))
    if R['mtls'] != src_R['mtls']:
        fails.append('usemtl set %s != source %s' % (sorted(R['mtls']), sorted(src_R['mtls'])))
    runs = [[gm for k, gm in enumerate((g, m) for g, m, _ in X['F']) if k == 0 or gm != (X['F'][k - 1][0], X['F'][k - 1][1])]
            for X in (R, src_R)]
    if runs[0] != runs[1]:
        fails.append('group order %s != source %s' % (runs[0], runs[1]))
    if (len(R['N']) > 0) != (len(src_R['N']) > 0) or (len(R['T']) > 0) != (len(src_R['T']) > 0):
        fails.append('vt/vn presence differs from the source')
    if [l for l in text.split('\n') if l.startswith('mtllib')] != mtl_lines:
        fails.append('mtllib is not the original MTL')
    for l in mtl_lines:
        if not os.path.isfile(os.path.join(pack, l.split(None, 1)[1])):
            fails.append('original MTL %s missing' % l)
    zero = 0
    for _, _, cs in R['F']:
        a, b, c = (R['V'][x[0]][:3] for x in cs)
        if norm(cross(sub(b, a), sub(c, a))) / 2 < 1e-10:
            zero += 1
    meas['zero_area'] = zero
    if zero:
        fails.append('%d zero-area triangles' % zero)
    orig = {corner_vals(src_R, c) for _, _, cs in src_R['F'] for c in cs}
    alien = sum(1 for _, _, cs in R['F'] for c in cs if corner_vals(R, c) not in orig)
    if alien:
        fails.append('%d corners are not an original (v, vt, vn)' % alien)
    pos, sp = 0, {}
    for g, m, cs in src_R['F']:
        sp.setdefault((g, m), []).append(tuple(corner_vals(src_R, c) for c in cs))
    for s, faces, mode, _ in groups:
        got = [tuple(corner_vals(R, c) for c in cs) for _, _, cs in R['F'][pos:pos + len(faces)]]
        pos += len(faces)
        if mode == 'copy' and got != sp.get((s['g'], s['mtl'])):
            fails.append('group %s should be unchanged' % s['g'])
    fa = meas['facing_away'] = [facing_away(src_R), facing_away(R)]
    if fa[1] > fa[0]:
        fails.append('%d triangles face against their vertex normals (source %d)' % (fa[1], fa[0]))
    oe, oe0 = open_edges(R), open_edges(src_R)
    meas['open_edges'] = [oe0, oe]
    if oe > oe0:
        fails.append('open edges %d > source %d (crack)' % (oe, oe0))
    if not zero:
        dm, d99, dx = deviation(R, src_R)
        meas['deviation_mm'] = dict(mean=round(dm * 1000, 3), p99=round(d99 * 1000, 3), max=round(dx * 1000, 3))
    return fails, meas


def sha_file(p):
    with open(p, 'rb') as fh:
        return hashlib.sha256(fh.read()).hexdigest()


def sources_text(pack, rep):
    """-> the new sources.json text: the files entries of the LODs and of this script added (or rewritten) and a
    top-level 'lod_variants' block. Every other entry must stay exactly as it was, in the same order (exit 1
    otherwise). The file keeps its format (indent 1, ASCII, no final newline)."""
    with open(os.path.join(pack, 'sources.json'), encoding='utf-8') as fh:
        data = json.load(fh)
    mine = set(rep) | {TOOL_ID}
    before = [json.dumps(e, sort_keys=True) for e in data['files'] if e['file'] not in mine]
    entries = {e['file']: e for e in data['files']}
    here = os.path.dirname(os.path.abspath(__file__))
    tool_sha = sha_file(os.path.join(here, 'make_lods.py'))
    tool = ('Source~/make_lods.py (seam-aware half-edge collapse with quadric error, seam-exact; a copy of the lods-street '
            'tool; python3 -I, standard library, trimesh and numpy for VERIFY only)')
    lods = {}
    for fn, r in sorted(rep.items()):
        base, model = entries[r['derived_from']], data['models'][r['derived_from']]
        v = r['verify']
        stem, level = fn[:-4].rsplit('_', 1)
        cc0 = base['license'] == 'CC0-1.0'
        if sha_file(os.path.join(pack, r['derived_from'])) != base['sha256']:
            sys.exit('%s on disk differs from its sources.json entry; nothing written' % r['derived_from'])
        if sum(g['src'] for g in r['groups']) != model['tris']:
            sys.exit('%s: sources.json says %d tris; nothing written' % (r['derived_from'], model['tris']))
        e = {'file': fn, 'bytes': r['bytes'], 'sha256': r['sha256'], 'model': base['model'], 'lod': int(level[-1])}
        for k in ('origin', 'asset_id', 'asset_title', 'source_url', 'author', 'license', 'license_url', 'publisher_license'):
            if k in base:
                e[k] = base[k]
        e['license_note'] = ('CC0 derivative of %s; stays CC0 1.0' % r['derived_from'] if cc0 else
                             'Project-owned derivative of %s (project-owned like its source)' % r['derived_from'])
        e['derived_from'] = [{'file': r['derived_from'], 'bytes': base['bytes'], 'sha256': base['sha256']}]
        e['tool'] = tool
        e['tool_sha256'] = tool_sha
        e['mtllib'] = stem + '.mtl'
        e['target_tris'] = r['target']
        e['tris'] = r['tris']
        e['source_tris'] = model['tris']
        e['materials'] = model['materials']
        e['vn'] = v['vn'] > 0
        e['size_m'] = v['size_m']
        e['min_y'] = v['min_y']
        e['derivation'] = (
            'Decimated from %s (%s to %s triangles, target %s) by seam-aware half-edge collapse with quadric error, '
            'seam-exact: every vertex and corner is an original v/vt%s line of %s (no vertex moved%s), UV seams%s and '
            'open borders shorten only along themselves (no cracks), the bounding box is kept exactly%s, no triangle over '
            '1 cm2 folds inside out. Uses %s and its maps (shared; no MTL or texture added or changed).'
            % (r['derived_from'], '{:,}'.format(model['tris']), '{:,}'.format(r['tris']), '{:,}'.format(r['target']),
               '/vn' if v['vn'] else '', r['derived_from'],
               '' if v['vn'] else ', no vn added', ', hard normal edges' if v['vn'] else '',
               ' (upright, base on y = 0)' if v.get('upright') else ' (base on y = 0)', e['mtllib']))
        e['verify'] = {k: v[k] for k in ('bounds_max_diff_m', 'zero_area', 'open_edges', 'facing_away', 'deviation_mm',
                                         'centre_xz_m', 'relaxed_collapses') if k in v}
        entries[fn] = e
        sw = SWITCH[stem]
        lods.setdefault(r['derived_from'], {})[level] = {
            'file': fn, 'tris': r['tris'], 'target': r['target'], 'bytes': r['bytes'],
            'shown_from_screen_height': sw[0] if level == 'LOD1' else sw[1],
            'shown_down_to_screen_height': sw[1] if level == 'LOD1' else sw[2]}
    entries[TOOL_ID] = {
        'file': TOOL_ID, 'bytes': os.path.getsize(os.path.join(here, 'make_lods.py')), 'sha256': tool_sha,
        'origin': 'Project-owned original work', 'license': 'Project-owned',
        'notes': 'LOD generator for ForensicCase, Crowbar, Sledgehammer and FlatheadScrewdriver (Unity ignores folders '
                 'ending in ~). decimate_exact.py, which reproduces the shipped pack, is unchanged.',
        'derived_from': [{'path': STREET_TOOL[0], 'sha256': STREET_TOOL[1],
                          'note': 'the lods-street tool as of 2026-10-15, which extends '
                                  'Prototype/ThirdParty/ForensicModels/Source~/decimate_exact.py'}]}
    root = sorted(k for k in entries if not k.startswith('Source~/'))
    data['files'] = [entries[k] for k in root] + [entries[k] for k in sorted(set(entries) - set(root))]
    after = [json.dumps(e, sort_keys=True) for e in data['files'] if e['file'] not in mine]
    if after != before:
        sys.exit('sources.json: an existing entry would change or move; nothing written')
    data['lod_variants'] = {
        'added': '2026-10-15', 'status': 'unverified in Unity (not imported, no LODGroup wired yet)',
        'tool': TOOL_ID, 'tool_sha256': tool_sha,
        'materials': 'each LOD OBJ names the original <Model>.mtl; no MTL or texture was added or changed',
        'rules': 'see the make_lods.py docstring: seam-exact half-edge collapse, every vertex an original v/vt/vn, bounds '
                 'kept exactly (tools upright, base on y = 0), no cracks, no inside-out folds over 1 cm2; VERIFY: '
                 'triangles within 10 %, bounds within 5 mm, same usemtl set, valid indices, no zero-area triangle',
        'unity_lodgroup_transition_note': 'suggested LODGroup screen-relative heights (LOD Bias 1): LOD0 above the '
                                          'first, LOD1 down to the second, LOD2 down to the third, culled below',
        'unity_lodgroup_transition': {k + '.obj': list(v) for k, v in sorted(SWITCH.items())},
        'models': {k: lods[k] for k in sorted(lods)}}
    return json.dumps(data, indent=1)


def main():
    args = sys.argv[1:]
    if not args:
        sys.exit(__doc__.split('\n')[0])
    pack = args.pop(0)
    report = None
    if args and args[0] == '--report':
        args.pop(0); report = args.pop(0)
    names = args or list(MODELS)
    for name in names:
        if name not in MODELS:
            sys.exit('unknown model %r (known: %s)' % (name, ', '.join(MODELS)))
    trimesh = load_trimesh()
    rep, ok, out_files = {}, True, {}
    for name in names:
        src = os.path.join(pack, name + '.obj')
        src_text = open(src, encoding='utf-8').read()
        M = parse(src)
        src_R = raw_obj(src_text)
        mtl_lines = M['head'] or ['mtllib %s.mtl' % name]
        for level, cfg in MODELS[name].items():
            groups, relaxed = build(M, cfg, name)
            text = write(M, groups, name, level, cfg['target'], mtl_lines)
            fails, meas = verify(trimesh, name, level, text, src_text, src_R, groups, cfg, pack, mtl_lines)
            meas['relaxed_collapses'] = relaxed
            fn = '%s_%s.obj' % (name, level)
            data = text.encode('utf-8')
            gl = [dict(g=s['g'], mtl=s['mtl'], mode=mode, src=len(s['faces']), tris=len(f),
                       **({} if mode == 'copy' else {'share': goal})) for s, f, mode, goal in groups]
            rep[fn] = dict(derived_from=name + '.obj', target=cfg['target'], keep=cfg.get('keep', []),
                           fixed=cfg.get('fixed', {}), relax=cfg.get('relax'),
                           uv_max=cfg.get('uv_max', UV_MAX) if cfg.get('relax') else None, tris=meas['tris'],
                           bytes=len(data), sha256=hashlib.sha256(data).hexdigest(), verify=meas, groups=gl)
            oe, fa = meas.get('open_edges', [0, 0]), meas.get('facing_away', [0, 0])
            print('%-28s target %5d -> %5d tris, %6d bytes, bounds diff %.5f m, open edges %d -> %d, '
                  'facing away %d -> %d, zero-area %d, relaxed collapses %d'
                  % (fn, cfg['target'], meas['tris'], len(data), meas.get('bounds_max_diff_m', -1),
                     oe[0], oe[1], fa[0], fa[1], meas.get('zero_area', -1), relaxed))
            for d in gl:
                print('    %-28s %-20s %-5s %5d -> %5d' % (d['g'][:28], d['mtl'], d['mode'], d['src'], d['tris'])
                      + ('  (share %d)' % d['share'] if 'share' in d else ''))
            for k in ('size_m', 'min_y', 'upright', 'vn', 'centre_xz_m', 'deviation_mm'):
                if k in meas:
                    print('    %s: %s' % (k, meas[k]))
            for f in fails:
                print('VERIFY FAILED %s %s: %s' % (name, level, f))
            ok &= not fails
            out_files[fn] = data
    if not ok:
        sys.exit('VERIFY failed: nothing written')
    full = set(names) == set(MODELS) and os.path.isfile(os.path.join(pack, 'sources.json'))
    sources = sources_text(pack, rep) if full else None   # built (and checked) before anything is written
    for fn, data in out_files.items():
        with open(os.path.join(pack, fn), 'wb') as fh:
            fh.write(data)
    if report:
        with open(report, 'w', encoding='utf-8', newline='\n') as fh:
            json.dump(rep, fh, indent=1)
    print('VERIFY passed; wrote', ', '.join(out_files))
    if sources is not None:
        with open(os.path.join(pack, 'sources.json'), 'w', encoding='utf-8', newline='\n') as fh:
            fh.write(sources)
        print('sources.json: LOD entries, the make_lods.py entry and lod_variants written; other entries unchanged')
    else:
        print('sources.json not touched (run without a model list to update it)')


if __name__ == '__main__':
    main()
