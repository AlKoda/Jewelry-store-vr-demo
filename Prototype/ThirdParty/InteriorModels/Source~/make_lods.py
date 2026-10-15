"""LOD variants of InteriorModels props: python3 -I make_lods.py PACKDIR [--report OUT.json] [--previews] [Model ...]
From the repository root:
  python3 -I Prototype/ThirdParty/InteriorModels/Source~/make_lods.py Prototype/ThirdParty/InteriorModels --previews
Writes <Model>_LOD1.obj and <Model>_LOD2.obj to PACKDIR for the models in MODELS (FireExtinguisher, WallClock). The full
model stays LOD0; no existing OBJ, MTL or texture is changed. No FireAlarmCallPoint LODs: it is a US-style pull station
and is not placed in the night store.
Began as a copy of Prototype/ThirdParty/ForensicModels/Source~/decimate_exact.py (split per corner, decimate per group,
keep original vertices) and carries the lods-street extensions (Prototype/ThirdParty/StreetModels/Source~/make_lods.py
as of 2026-10-15, SHA-256 7f35da82d77dd3e8...). From the lods-street tool:
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
  text.
- relax (LOD2 only; LOD1 is seam-exact): a vertex may also collapse across a seam or where three or more corners meet;
  the corners that the collapse edge does not map take the target's corner with the nearest normal/UV in the same group
  (never further than uv_max in UV) and that difference, times the area it covers, is added to the cost (attribute-aware
  quadric error). Vertices still keep their original v/vt/vn and the geometry stays closed (no cracks).
- No collapse may turn a triangle to face away from its own vertex normals (Unity would cull it or light it from behind),
  and no triangle over 1 cm2 may end up facing more than 90 degrees away from the source triangle it comes from (no
  inside-out folds; the fold guard of the later lods-street revision, SHA-256 3a85204f2f97b59c..., which reports
  rather than refuses triangles against their normals; here they are still refused, as check_obj.py must pass).
- Deterministic (no randomness, sorted tie-breaks): a rerun reproduces the same bytes.
Added for the interior props (settings per model and level in MODELS):
- Part guards: a part is a connected piece of a group (welded by position), e.g. one clock hand. The last vertex on each
  face of every part's bounding box is never removed, so every part keeps its exact box (and with it every group box);
  the lods-street tool guards whole groups only, which let the clock's dial, centre cap and arbor shrink or vanish.
  keep_parts locks the parts under that many triangles (copied unchanged inside their group).
- Part budgets (part_targets): with group shares alone the seam-heavy valve parts of the extinguisher kept their
  triangles and the cylinder, the one part that could still collapse, became a cone. Named parts get explicit targets
  and the other parts of the group share the rest in proportion; a part that stalls passes its excess to the others.
- Parts left out: drop_parts (hidden in use, e.g. the back of a wall clock, or thinner than a pixel at that distance)
  and min_part (box diagonal under that many metres).
- lathe: a surface-of-revolution part is rebuilt on a coarser grid of its own vertices (see lathe()): plain decimation
  turned the clock case ring into a jagged polygon whose triangles crossed the dial.
- keep_angles: the outer vertices of a ring-shaped part at every step degrees about an axis are locked, so a band hugging
  a cylinder keeps chords that stay outside it (the extinguisher's yellow band).
- uv_guard: no collapse may make a triangle sample another part's texels (its UV triangle leaving the part's islands for
  another part's; padding between islands is allowed). Without it an orange patch appeared on the black lever.
- The source OBJ's leading comment lines (pack, units, licence) are kept at the top of each LOD, as in
  PeopleModels/Source~/make_lods.py.
VERIFY (in memory and on a staging copy, before anything is written; exit 1 naming the model and the failing measure):
triangles within 10 % of the target and of every fixed group share, bounds within 5 mm (trimesh, process=False), the
pivot kept (floor: lowest point at y = 0; wall: back at z = 0), same usemtl set and group order, every v/vt/vn index
valid, every face a triangle, no zero-area triangle (< 1e-10 m2 as written), every corner an original (v, vt, vn) value,
copied groups identical, every part of the source present with its box within 1 mm unless it is left out on purpose (the
clock hands are checked by name), no triangle joining two source parts, no triangle sampling another part's texels
(except on a lathe band that is hidden in use), no new open edges (position-welded over the whole model) and no open
edge with an end off the source's open borders (a crack along a UV seam opens one between seam vertices that are
interior in the source), no more triangles facing away from their vertex normals than the source has; and the pack's
own tools on a staging copy: Source~/check_obj.py (faces whose winding disagrees with their vertex normals: no more
than the source, which has 0; trimesh reads every triangle) and Source~/cracks.py (open edges on its 0.1 mm weld: no
more than the source).
--previews renders the full model and both LODs with Source~/render2.py (back faces culled, as Unity's Standard shader),
plus a close-up of the clock hands, and composes Source~/LOD_Previews.jpg with Source~/grid.py.
When every model was built and PACKDIR holds the pack documents, the 'lods' entry of each model in models.json, the LOD,
preview and script entries in sources.json, the README section 'LOD variants' and the README total-size line are
rewritten; every other entry and section is kept. Source~/make_docs.py rebuilds the pack without the LODs: run this
script after it.
The build needs only the standard library; -I hides the user site-packages, so that one directory (never the script dir
or the cwd) is appended to import trimesh for VERIFY. check_obj.py, cracks.py, render2.py and grid.py run as their own
processes (numpy, trimesh, Pillow)."""
import sys, os, io, re, site, json, heapq, hashlib, math, itertools, shutil, subprocess, tempfile

CASE_RINGS = [(140.0, 25.2), (139.5, 29.4), (142.6, 29.4), (142.6, 35.4), (155.8, 31.2), (158.5, 31.2), (160.2, 0.0),
              (153.7, 23.0), (154.9, 23.0)]   # WallClock case profile rings kept, (radius, z) in mm, front lip to inner ledge
CASE_HIDDEN = [(153.7, 0.0)]                 # the back rim ring (against the wall): the band across it is never seen
MIN_KEEP = 64            # groups under this many triangles are copied unchanged
MIN_SHARE = 32           # floor of a decimated group's share (as in decimate_exact.py)
BORDER_W, SEAM_W = 10.0, 1.0   # edge-constraint quadric weights: open borders, UV/normal/group seams
ATTR_L2 = 1e-4           # m2: an attribute difference of 0.1 costs like a 1 mm offset over 1 cm
UV_W = 100.0             # attribute distance = |dn|^2 + UV_W |duv|^2 (0.01 UV ~ 0.1 normal)
UV_MAX = 0.02            # default relax UV limit: a corner never moves further in UV (about 20 texels at 1K)
LEN_W = 1e-10            # tie-break on flat areas: shorter edges first (keeps triangles compact)
MIN_COS = 0.2            # reject a collapse that turns any triangle by more than ~78 degrees
MIN_AREA = 1e-9          # m2, 10x the zero-area limit of strip_degenerate.py
FOLD_AREA = 1e-4         # m2: a triangle larger than 1 cm2 may never face more than 90 degrees away from its source triangle
BOX_TOL = 1e-6           # m: vertices this close to a face of their part's box hold that face
MIN_PART = 4             # floor of a part's share (part_shares)
RING_TOL = 2e-4          # m: lathe vertices this close in (radius, height) form one profile ring
COL_TOL = 0.5            # degrees: lathe vertices this close in angle form one column
BOUNDS_TOL = 0.005       # m: whole-model bounds against the source
PART_TOL = 0.001         # m: every part's box against the source
PIVOT_TOL = 1e-6         # m: the pivot plane (floor y = 0, wall z = 0)
SHARE_TOL = 0.10         # triangles within 10 % of the target and of every fixed group share
MODELS = {
    # keep: groups copied unchanged (group name, or 'mtl:X' = every group using material X); fixed: explicit group
    # targets; keep_parts: parts under this many triangles are locked; part_targets: explicit part targets (by PART_NAMES;
    # the other parts share the rest of their group in proportion); drop_parts: parts left out at that level (hidden in
    # use, or thinner than a pixel at that distance); min_part: parts whose box diagonal is under this (m) are left out;
    # part_shares: per-part shares without explicit targets (implied by part_targets);
    # lathe: parts rebuilt on a coarser grid of their own vertices (see lathe()); keep_angles: the outer vertices of a
    # ring-shaped part at every step degrees about an axis are locked; relax: 'early' (relaxed from the start) or 'late'
    # (only once seam-exact collapses run out), LOD2 only; uv_max: relax UV limit (default UV_MAX); uv_guard (default
    # on): no collapse may make a triangle sample another part's texels.
    'FireExtinguisher': {
        # LOD1: the yellow band keeps its 32 outer columns, so the recessed cylinder wall never shows through it.
        'LOD1': dict(target=2000, keep=['GaugeGlass', 'Tag'], keep_parts=64,
                     keep_angles={'Body': {'band': dict(axis=1, center=(0.0, -0.0309), step=11.25, r_min=0.092)}}),
        # LOD2: 25 parts in 504 triangles. Plain shares let the seam-heavy valve parts keep their triangles and the
        # cylinder became a cone, so the large parts get explicit targets; parts under 25 mm (gauge internals, pins,
        # rivets) and the thin pull pin, tag wire and hose ferrule are left out; the band keeps every second outer
        # column (16, 22.5 degrees apart: its chords stay 0.4 mm outside the 90.6 mm cylinder recess).
        'LOD2': dict(target=600, keep=['Tag'], fixed={'GaugeGlass': 64}, min_part=0.025,
                     drop_parts={'Body': ['tag wire', 'pull pin', 'hose ferrule']},
                     part_targets={'Body': {'cylinder': 140, 'stand': 56, 'band': 80, 'hose': 28, 'carry handle': 30,
                                            'squeeze lever': 24, 'valve body': 20, 'neck collar': 18, 'hose coupling': 16,
                                            'nozzle': 20, 'gauge': 16}},
                     keep_angles={'Body': {'band': dict(axis=1, center=(0.0, -0.0309), step=22.5, r_min=0.092)}},
                     relax='late', uv_max=0.2)},
    # The clock hangs on the wall: its rear knob, rear plate and inner rim (back cover) are never seen and are left out
    # of both LODs. The case is a 96 x 12 lathe grid; plain decimation made it a jagged polygon whose triangles crossed
    # the dial at LOD2, so it is rebuilt on 32 (LOD1) or 12 (LOD2) of its columns. Every visible profile band keeps its
    # UV island; only the rings at 139.6/25.3 mm (a 0.4 mm bevel) and 149.5/34.1 mm (the bezel crown, 0.9 mm off the new
    # line), across which the UVs run on, are left out, and the back rim and inner wall merge into one hidden band. Hour
    # and minute hand, dial (and at LOD1 the arbor and centre cap) are locked; the second hand is decimated inside its
    # exact box; at LOD2 the arbor and cap (under 10 mm) are left out.
    'WallClock': {'LOD1': dict(target=900, fixed={'Clock': 700, 'Glass': 200}, keep_parts=64,
                               drop_parts={'Clock': ['rear knob', 'rear plate', 'inner rim']},
                               lathe={'Clock': {'case': dict(columns=32, rings=CASE_RINGS, hidden=CASE_HIDDEN)}}),
                  'LOD2': dict(target=300, fixed={'Clock': 236, 'Glass': 64}, keep_parts=24, min_part=0.01,
                               drop_parts={'Clock': ['rear knob', 'rear plate', 'inner rim']},
                               lathe={'Clock': {'case': dict(columns=12, rings=CASE_RINGS, hidden=CASE_HIDDEN)}},
                               relax='early', uv_max=0.35)},
}
PART_NAMES = {   # parts (connected pieces) in order of first appearance in their group
    ('FireExtinguisher', 'Body'): ['carry handle', 'hose coupling', 'pivot pin a', 'tag wire', 'gauge', 'squeeze lever',
                                   'pull pin', 'neck collar', 'rivet head a', 'valve body', 'coupling ring', 'hose', 'stand',
                                   'rivet head b', 'hose ferrule', 'nozzle', 'band', 'gauge stem', 'needle hub',
                                   'gauge back', 'gauge nut', 'pivot pin b', 'gauge needle', 'label tab', 'cylinder'],
    ('WallClock', 'Clock'): ['case', 'rear knob', 'rear plate', 'inner rim', 'dial', 'hour hand', 'arbor', 'second hand',
                             'centre cap', 'minute hand'],
}
CHECKS = {   # extra VERIFY measures per model/level: pivot plane; named parts that must keep their box within 1 mm
    ('FireExtinguisher', 'LOD1'): dict(pivot='floor'), ('FireExtinguisher', 'LOD2'): dict(pivot='floor'),
    ('WallClock', 'LOD1'): dict(pivot='wall', parts={'Clock': ['hour hand', 'minute hand', 'second hand']}),
    ('WallClock', 'LOD2'): dict(pivot='wall', parts={'Clock': ['hour hand', 'minute hand', 'second hand']}),
}
PREVIEWS = {     # Source~/render2.py views per model: (file-name suffix, arguments)
    'FireExtinguisher': [('', ['--yaw', '35', '--pitch', '12']), ('_back', ['--yaw', '215', '--pitch', '12'])],
    'WallClock': [('', ['--yaw', '25', '--pitch', '10']),
                  ('_hands', ['--yaw', '0', '--pitch', '0', '--target', '0.01', '-0.02', '0.03', '--radius', '0.085'])],
}
FP_GRID = 64             # UV footprint hash cells per UV unit
FP_EPS = 0.02            # barycentric slack of the footprint test (about a texel along a 1K island edge)
FP_SAMPLES = [(1 / 3, 1 / 3, 1 / 3), (0.5, 0.5, 0), (0, 0.5, 0.5), (0.5, 0, 0.5), (0.6, 0.2, 0.2), (0.2, 0.6, 0.2),
              (0.2, 0.2, 0.6)]
LOCKED, MANIFOLD, BORDER, SEAM, CHAIN = range(5)   # vertex kinds (CHAIN comes as (CHAIN, (p, q), k))


def parse(path):
    """-> dict(V, T, N: [(text, floats)], head: mtllib lines, segs: [dict(head, g, mtl, faces)], tail, notes)"""
    V, T, N, head, segs, pend, notes = [], [], [], [], [], [], []
    g = mtl = cur = None
    for no, raw in enumerate(open(path, encoding='utf-8'), 1):
        line = raw.strip()
        if line[:1] == '#' and not (V or head or segs or pend):
            notes.append(line)                  # leading comments (pack, units, licence) are kept
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


def parts_of(tris):
    """connected parts of a triangle list (each triangle three hashable vertex keys, e.g. positions) -> lists of
    triangle indices, in order of first appearance (deterministic)"""
    par = {}

    def root(x):
        while par[x] != x:
            par[x] = par[par[x]]; x = par[x]
        return x
    for t in tris:
        for x in t:
            par.setdefault(x, x)
        a = root(t[0])
        for x in t[1:]:
            b = root(x)
            if a != b:
                par[b] = a
    out = {}
    for i, t in enumerate(tris):
        out.setdefault(root(t[0]), []).append(i)
    return list(out.values())


class Footprint:
    """The UV triangles of one group (one material, one texture) of the source, tagged with their part and hashed on a
    grid: parts_at(p) is the set of parts whose UVs cover UV point p (within FP_EPS). A LOD triangle with a sample
    (FP_SAMPLES) on another part's UVs and not on its own would show that part's texels (e.g. orange band paint on a
    black lever); samples on the padding between islands only see the dilated edge colour and are allowed."""

    def __init__(self, tris, owners):
        self.cells, self.tris, self.owners = {}, tris, owners
        for i, t in enumerate(tris):
            lo = [int(math.floor(min(p[k] for p in t) * FP_GRID)) for k in (0, 1)]
            hi = [int(math.floor(max(p[k] for p in t) * FP_GRID)) for k in (0, 1)]
            for x in range(lo[0], hi[0] + 1):
                for y in range(lo[1], hi[1] + 1):
                    self.cells.setdefault((x, y), []).append(i)

    def parts_at(self, p):
        out = set()
        for i in self.cells.get((int(math.floor(p[0] * FP_GRID)), int(math.floor(p[1] * FP_GRID))), ()):
            if self.owners[i] in out:
                continue
            a, b, c = self.tris[i]
            d = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
            if abs(d) < 1e-14:
                continue
            w0 = ((b[1] - c[1]) * (p[0] - c[0]) + (c[0] - b[0]) * (p[1] - c[1])) / d
            w1 = ((c[1] - a[1]) * (p[0] - c[0]) + (a[0] - c[0]) * (p[1] - c[1])) / d
            if w0 >= -FP_EPS and w1 >= -FP_EPS and 1 - w0 - w1 >= -FP_EPS:
                out.add(self.owners[i])
        return out

    def bleeds(self, uv, part):
        """True when a sample of UV triangle uv lies on another part's UVs and not on part's own"""
        for q in uv_samples(uv):
            at = self.parts_at(q)
            if at and part not in at:
                return True
        return False


def uv_samples(t):
    """FP_SAMPLES points of UV triangle t (barycentric)"""
    return [tuple(w[0] * t[0][k] + w[1] * t[1][k] + w[2] * t[2][k] for k in (0, 1)) for w in FP_SAMPLES]


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
        # part-box guards: ext_of[w] = (group, part, box face) held by w; ext_n = vertices still holding each one. A part
        # is a connected piece of a group (e.g. one clock hand); its box guards hold the group box too. A vertex lies in
        # one part per group and never joins a group it was not in (corners map within a group), so holders only get fewer.
        # tpart[t] = (group, part); npart = live triangles per part; pgoal = per-part shares (None: groups only)
        self.ext_of = [[] for _ in range(n)]
        self.ext_n = {}
        self.tpart = [None] * len(self.tris)
        self.npart, self.part_locked, self.pgoal = {}, {}, None
        for s in sids:
            ts = [t for t in range(len(self.tris)) if self.ts[t] == s]
            parts = parts_of([self.tris[t] for t in ts])
            for k, part in enumerate(parts):
                for i in part:
                    self.tpart[ts[i]] = (s, k)
                self.npart[(s, k)] = len(part)
                ws = sorted({w for i in part for w in self.tris[ts[i]]})
                self.part_locked[(s, k)] = all(w in self.locked for w in ws)
                lo = [min(self.pos[w][j] for w in ws) for j in range(3)]
                hi = [max(self.pos[w][j] for w in ws) for j in range(3)]
                for w in ws:
                    for j in range(3):
                        for face, ref in ((2 * j, lo[j]), (2 * j + 1, hi[j])):
                            if abs(self.pos[w][j] - ref) <= BOX_TOL:
                                key = (s, k, face)
                                self.ext_of[w].append(key); self.ext_n[key] = self.ext_n.get(key, 0) + 1
        self.ver = [0] * n
        self.kc = {}
        self.heap = []
        self.relaxed = False
        self.uv_max = UV_MAX
        self.relaxed_collapses = 0
        self.goal = dict(self.nseg)
        self.npart0 = dict(self.npart)
        self.fp = {}             # UV footprint per group (uv_guard)
        if self.cuv and self.cuv[0] is not None:
            for s in sids:
                ts = [t for t in range(len(self.tris)) if self.ts[t] == s]
                self.fp[s] = Footprint([tuple(self.cuv[c][:2] for c in self.tc[t]) for t in ts], [self.tpart[t] for t in ts])
        self.uv_guard = False

    def part_goals(self, goal, pfix=None):
        """each group's goal split over its parts: locked parts keep all theirs, parts with an explicit target (pfix)
        get it, the others share the rest in proportion to their source triangles (at least MIN_PART)"""
        pg, pfix = {}, pfix or {}
        for s in goal:
            ps = [p for p in sorted(self.npart0) if p[0] == s]
            for p in ps:
                if self.part_locked[p] or p in pfix:
                    pg[p] = self.npart0[p] if self.part_locked[p] else min(self.npart0[p], pfix[p])
            rest = goal[s] - sum(pg[p] for p in ps if p in pg)
            nfree = sum(self.npart0[p] for p in ps if p not in pg)
            for p in ps:
                if p not in pg:
                    pg[p] = min(self.npart0[p], max(MIN_PART, int(round(rest * self.npart0[p] / nfree))))
        return pg

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

    def nsum(self, cs, i, ci):
        """sum of the corner normals of triangle corners cs with corner i replaced by ci"""
        ns = [self.cn[ci if j == i else c] for j, c in enumerate(cs)]
        return (ns[0][0] + ns[1][0] + ns[2][0], ns[0][1] + ns[1][1] + ns[2][1], ns[0][2] + ns[1][2] + ns[2][2])

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
            if self.pgoal is not None and self.npart[self.tpart[t]] <= self.pgoal[self.tpart[t]]:
                return None                          # that part has reached its share
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
        area, turned = {}, []
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
            turned.append((t, i, n0, n1))
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
        if self.cn[0] is not None:                   # no triangle may come to face away from its own vertex normals
            for t, i, n0, n1 in turned:              # (Unity culls it, or lights it from behind)
                cs = self.tc[t]
                if dot(n1, self.nsum(cs, i, mapc[cs[i]])) <= 0 < dot(n0, self.nsum(cs, i, cs[i])):
                    return None
        if self.uv_guard and self.fp:                # uv_guard: no changed triangle may sample another part's UVs
            for t, i, n0, n1 in turned:
                uv = [self.cuv[mapc[c] if j == i else c][:2] for j, c in enumerate(self.tc[t])]
                if self.fp[self.ts[t]].bleeds(uv, self.tpart[t]):
                    return None
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
            self.alive[t] = False; self.nseg[self.ts[t]] -= 1; self.npart[self.tpart[t]] -= 1
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


def lathe(M, faces, spec, label, fp=None, own=None):
    """Rebuilds one surface-of-revolution part (a grid of profile rings x angular columns about the axis spec['axis']
    through the origin) on a coarser grid of its own vertices: spec['columns'] columns (the seam and box columns plus
    evenly spread others, see below) and the profile rings listed in spec['rings'] ((radius, height) in mm, in profile
    order, both ends included). The corners of each new quad are the source corners (v, vt, vn) of the source cell next
    to them on the quad's side, so a band that keeps both its rings keeps its UV island and normals. A left-out ring must
    not be a UV seam (the band would sweep the texture across other islands) unless it is listed in spec['hidden'] (a
    band that is never seen in use). Each quad is split along the diagonal whose triangles do not sample another part's
    texels (fp: the group's Footprint, own: this part), else along the shorter one.
    -> (new faces in the source winding, dict of grid counts)"""
    V = M['V']
    ax = spec.get('axis', 2)
    i1, i2 = [j for j in range(3) if j != ax]
    pos = lambda c: V[c[0]][1][:3]
    P = sorted({pos(c) for f in faces for c in f})
    rh = {p: (math.hypot(p[i1], p[i2]), p[ax]) for p in P}
    rings, ring = [], {}
    for p in sorted(P, key=lambda p: rh[p]):
        for k, (r, h) in enumerate(rings):
            if abs(r - rh[p][0]) <= RING_TOL and abs(h - rh[p][1]) <= RING_TOL:
                ring[p] = k
                break
        else:
            ring[p] = len(rings); rings.append(rh[p])
    th = {p: math.degrees(math.atan2(p[i2], p[i1])) % 360.0 for p in P}
    cols = []
    for p in sorted(P, key=lambda p: (th[p], p)):
        if cols and th[p] - cols[-1][-1] < COL_TOL:
            cols[-1].append(th[p])
        else:
            cols.append([th[p]])
    if len(cols) > 1 and cols[0][0] + 360.0 - cols[-1][-1] < COL_TOL:
        cols[0] = cols.pop() + cols[0]                # the column at 0 / 360 degrees
    col = {p: next(k for k, c in enumerate(cols) if th[p] in c) for p in P}
    nr, nc = len(rings), len(cols)
    grid = {(ring[p], col[p]): p for p in P}
    if len(grid) != len(P) or nr * nc != len(P):
        sys.exit('%s: not a lathe grid (%d vertices, %d rings x %d columns)' % (label, len(P), nr, nc))
    # columns in angle order from the one nearest 0 degrees; profile path from the ring adjacency
    start = min(range(nc), key=lambda k: min(min(a, 360.0 - a) for a in cols[k]))
    order = list(range(start, nc)) + list(range(start))
    cidx = {k: j for j, k in enumerate(order)}      # column -> position around (0 = angle 0)
    adj = {k: set() for k in range(nr)}
    cell = {}                                        # (ring pair, column pair) -> {grid vertex: corner}
    for f in faces:
        g = [(ring[pos(c)], col[pos(c)]) for c in f]
        rs, cs = {x[0] for x in g}, {x[1] for x in g}
        if len(rs) != 2 or len(cs) != 2:
            sys.exit('%s: a face spans %d rings and %d columns (not one grid cell)' % (label, len(rs), len(cs)))
        a, b = sorted(rs)
        adj[a].add(b); adj[b].add(a)
        key = (frozenset(rs), frozenset(cs))
        for x, c in zip(g, f):
            if cell.setdefault(key, {}).setdefault(x, c) != c:
                sys.exit('%s: two corners for one vertex in one cell' % label)
    if any(len(n) > 2 for n in adj.values()):
        sys.exit('%s: the profile is not a simple line' % label)
    ends = [k for k in range(nr) if len(adj[k]) == 1]
    want = []
    for r_mm, h_mm in spec['rings']:
        k = min(range(nr), key=lambda k: math.hypot(rings[k][0] * 1000 - r_mm, rings[k][1] * 1000 - h_mm))
        if math.hypot(rings[k][0] * 1000 - r_mm, rings[k][1] * 1000 - h_mm) > 0.3:
            sys.exit('%s: no profile ring at (%.1f, %.1f) mm' % (label, r_mm, h_mm))
        want.append(k)
    if len(ends) != 2 or want[0] not in ends or want[-1] not in ends:
        sys.exit('%s: the kept rings must start and end at the two ends of the profile' % label)
    path = [want[0]]
    while len(path) < nr:
        path.append(next(x for x in sorted(adj[path[-1]]) if x not in path))
    pidx = {k: i for i, k in enumerate(path)}
    if [pidx[k] for k in want] != sorted(pidx[k] for k in want):
        sys.exit('%s: the kept rings are not in profile order' % label)
    hidden = set()
    for r_mm, h_mm in spec.get('hidden', []):
        hidden.add(min(range(nr), key=lambda k: math.hypot(rings[k][0] * 1000 - r_mm, rings[k][1] * 1000 - h_mm)))

    def corner(rp, rq, cp, cq, x):
        """corner of grid vertex x in the source cell between rings rp, rq and columns cp, cq"""
        return cell[(frozenset((rp, rq)), frozenset((cp, cq)))][x]

    def attrs(c):
        return (M['T'][c[1]][1] if c[1] >= 0 else None, M['N'][c[2]][1] if c[2] >= 0 else None)
    # a left-out ring must not be a UV seam (different vt above and below) unless it is hidden
    for i in range(1, nr - 1):
        rr = path[i]
        if rr in want or rr in hidden:
            continue
        for j in range(nc):
            c0, c1 = order[j], order[(j + 1) % nc]
            if attrs(corner(path[i - 1], rr, c0, c1, (rr, c0)))[0] != attrs(corner(rr, path[i + 1], c0, c1, (rr, c0)))[0]:
                sys.exit('%s: left-out ring (%.1f, %.1f) mm is a UV seam; keep it or list it as hidden'
                         % (label, rings[rr][0] * 1000, rings[rr][1] * 1000))
    # source winding in (profile, column) parameter space
    sign = set()
    for f in faces:
        g = [(pidx[ring[pos(c)]], cidx[col[pos(c)]]) for c in f]
        cs = [x[1] for x in g]
        if max(cs) - min(cs) > 1:                    # the cell across angle 0: unwrap
            g = [(a, b + nc if b == 0 else b) for a, b in g]
        sa = (g[1][0] - g[0][0]) * (g[2][1] - g[0][1]) - (g[1][1] - g[0][1]) * (g[2][0] - g[0][0])
        sign.add(sa > 0)
    if len(sign) != 1:
        sys.exit('%s: inconsistent winding in the source grid' % label)
    flip = sign.pop()
    bands = list(zip(want, want[1:]))
    hidden_band = {(a, b): any(path[x] in hidden for x in range(pidx[a] + 1, pidx[b])) for a, b in bands}

    def quad(a, b, ja, jb):
        """the two triangles of band a -> b between column positions ja < jb, split along the diagonal whose triangles
        sample fewest texels of other parts (then the shorter one) -> (triangles, how many sample another part)"""
        an, bp = path[pidx[a] + 1], path[pidx[b] - 1]
        c, d, c1, d1 = order[ja % nc], order[jb % nc], order[(ja + 1) % nc], order[(jb - 1) % nc]
        A, B = corner(a, an, c, c1, (a, c)), corner(a, an, d1, d, (a, d))
        C, D = corner(bp, b, d1, d, (b, d)), corner(bp, b, c, c1, (b, c))
        pa, pb, pc, pd = (V[x[0]][1][:3] for x in (A, B, C, D))
        splits = [[(A, B, C), (A, C, D)], [(A, B, D), (B, C, D)]]
        if norm(sub(pa, pc)) > norm(sub(pb, pd)):
            splits.reverse()                         # the shorter diagonal first

        def bleeds(tris):
            return sum(1 for t in tris if fp is not None and t[0][1] >= 0 and fp.bleeds([M['T'][x[1]][1][:2] for x in t], own))
        best = min(splits, key=bleeds)
        return [(t[0], t[2], t[1]) if flip else t for t in best], bleeds(best)

    def stretch_bleed(js):
        """triangles of the visible bands between consecutive column positions js that sample another part"""
        return sum(quad(a, b, x, y)[1] for a, b in bands if not hidden_band[(a, b)] for x, y in zip(js, js[1:]))
    # columns: the seam columns (a vertex with different corners left and right inside one band) and the columns that
    # hold the part's box are kept; the other kept columns are spread evenly between them. Where a visible quad of a
    # stretch would sample another part's texels, the stretch's columns move: the evenest choice without that wins.
    req = {0}
    for i in range(nr - 1):
        ra, rb = path[i], path[i + 1]
        for j in range(nc):
            c0, c1, cm = order[j], order[(j + 1) % nc], order[j - 1]
            if any(attrs(corner(ra, rb, cm, c0, (rr, c0))) != attrs(corner(ra, rb, c0, c1, (rr, c0))) for rr in (ra, rb)):
                req.add(j)
    for ii in (i1, i2):
        for f in (min, max):
            ext = f(p[ii] for p in P)
            req |= {cidx[col[p]] for p in P if abs(p[ii] - ext) <= BOX_TOL}
    req = sorted(req)
    extra = spec['columns'] - len(req)
    if extra < 0:
        sys.exit('%s: %d columns are fewer than the %d seam and box columns' % (label, spec['columns'], len(req)))
    spans = [((req[(i + 1) % len(req)] - req[i]) % nc) or nc for i in range(len(req))]
    share = [extra * sp / float(nc) for sp in spans]
    m = [int(x) for x in share]
    for i in sorted(range(len(req)), key=lambda i: (-(share[i] - m[i]), i))[:extra - sum(m)]:
        m[i] += 1                                    # largest remainders
    keep_j, moved = [], 0
    for i, r0 in enumerate(req):
        sp, mi = spans[i], m[i]
        even = [r0 + int(round(sp * q / float(mi + 1))) for q in range(1, mi + 1)]
        js = [r0] + even + [r0 + sp]
        if fp is not None and stretch_bleed(js):
            def unevenness(ins):
                g = [y - x for x, y in zip([r0] + list(ins), list(ins) + [r0 + sp])]
                return (max(g), sum((x - sp / float(mi + 1)) ** 2 for x in g), ins)
            if math.comb(sp - 1, mi) <= 5000:
                cands = sorted(itertools.combinations(range(r0 + 1, r0 + sp), mi), key=unevenness)
            else:                                    # shift single columns by up to 3 positions
                cands = sorted({tuple(sorted(set(even[:q] + [even[q] + dd] + even[q + 1:]))) for q in range(mi)
                                for dd in (-3, -2, -1, 1, 2, 3) if r0 < even[q] + dd < r0 + sp}, key=unevenness)
            for ins in cands:
                if len(ins) == mi and not stretch_bleed([r0] + list(ins) + [r0 + sp]):
                    even = list(ins); moved += 1
                    break
        keep_j += [r0] + even
    keep_j.append(keep_j[0] + nc)
    out, hid = [], []
    for a, b in bands:
        for x, y in zip(keep_j, keep_j[1:]):
            tris = quad(a, b, x, y)[0]
            out += tris
            if hidden_band[(a, b)]:
                hid += [frozenset(V[c[0]][1][:3] for c in t) for t in tris]
    return out, dict(rings=nr, columns=nc, kept_rings=len(want), kept_columns=len(keep_j) - 1, stretches_moved=moved,
                     tris=len(faces), new_tris=len(out), hidden_band_tris=len(hid), _hidden=hid)


def build(M, cfg, name):
    """-> (list of (seg, faces, mode, share) in original order, relaxed collapse count, notes per group);
    mode copy / fixed / share"""
    M0, target = M, cfg['target']
    M = dict(M, segs=[dict(s) for s in M['segs']]); segs = M['segs']
    drop, lath, min_part = cfg.get('drop_parts', {}), cfg.get('lathe', {}), cfg.get('min_part', 0.0)
    ptarget = cfg.get('part_targets', {})
    keep, fixed = cfg.get('keep', []), cfg.get('fixed', {})
    for sel in keep + list(fixed):
        if not any(selects(sel, s) for s in segs):
            sys.exit('%s: keep/fixed selector %r matches no group' % (name, sel))
    note = {i: {} for i in range(len(segs))}
    locked_pos, kept_parts = set(), {}
    for i, s in enumerate(segs):  # parts left out or rebuilt first (groups that are copied unchanged excepted)
        names = PART_NAMES.get((name, s['g']), [])
        for x in list(drop.get(s['g'], [])) + list(lath.get(s['g'], {})) + list(ptarget.get(s['g'], {})):
            if x not in names:
                sys.exit('%s: part %r of %s not in PART_NAMES' % (name, x, s['g']))
        if len(s['faces']) < MIN_KEEP or any(selects(k, s) for k in keep):
            continue
        parts = parts_of([tuple(M['V'][c[0]][1][:3] for c in f) for f in s['faces']])
        faces, gone, small = list(s['faces']), set(), []
        for k, p in enumerate(parts):
            P = [M['V'][c[0]][1][:3] for j in p for c in s['faces'][j]]
            diag = norm(sub(tuple(max(x[j] for x in P) for j in range(3)), tuple(min(x[j] for x in P) for j in range(3))))
            pname = names[k] if k < len(names) else ''
            if pname in drop.get(s['g'], []):
                gone |= set(p)               # hidden in use (e.g. against the wall)
            elif diag < min_part:
                gone |= set(p); small.append(k)   # smaller than min_part: under a pixel or two at this level
            elif pname in lath.get(s['g'], {}):
                gfp, own = None, {j: kk for kk, pp in enumerate(parts) for j in pp}
                if all(c[1] >= 0 for f in s['faces'] for c in f):
                    gfp = Footprint([tuple(M['T'][c[1]][1][:2] for c in f) for f in s['faces']],
                                    [own[j] for j in range(len(s['faces']))])
                new, info = lathe(M, [s['faces'][j] for j in p], lath[s['g']][pname], '%s %s %s' % (name, s['g'], pname), gfp, k)
                faces[p[0]] = new            # rebuilt in place of the part's first face; its other faces go
                gone |= set(p[1:])
                locked_pos |= {M['V'][c[0]][1][:3] for f in new for c in f}
                note[i]['lathe ' + pname] = info
        for k, p in enumerate(parts):    # keep_angles: the outer vertices of a ring-shaped part at every step degrees
            spec = cfg.get('keep_angles', {}).get(s['g'], {}).get(names[k] if k < len(names) else '')
            if spec and p[0] not in gone:
                ax = spec['axis']; i1, i2 = [j for j in range(3) if j != ax]; c1, c2 = spec['center']
                for x in {M['V'][c[0]][1][:3] for j in p for c in s['faces'][j]}:
                    a = math.degrees(math.atan2(x[i2] - c2, x[i1] - c1)) % spec['step']
                    if math.hypot(x[i1] - c1, x[i2] - c2) >= spec['r_min'] and min(a, spec['step'] - a) < COL_TOL:
                        locked_pos.add(x)
        kept_parts[i] = [k for k in range(len(parts)) if parts[k][0] not in gone]
        if gone or small:
            note[i]['dropped'] = sorted(names[k] if k < len(names) else 'part %d' % k for k in range(len(parts))
                                        if parts[k][0] in gone and k not in small)
            note[i]['dropped_small'] = len(small)
        flat = []
        for j, f in enumerate(faces):
            if j in gone:
                continue
            flat += f if isinstance(f, list) else [f]
        s['faces'] = flat
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
    locked_pos |= {M['V'][c[0]][1][:3] for i in mode if mode[i] == 'copy' for f in segs[i]['faces'] for c in f}
    sids = [i for i in mode if mode[i] != 'copy']
    for i in sids:              # parts under keep_parts triangles are locked (copied unchanged inside their group)
        tris = [tuple(M['V'][c[0]][1][:3] for c in f) for f in segs[i]['faces']]
        small = [p for p in parts_of(tris) if len(p) < cfg.get('keep_parts', 0)]
        for p in small:
            locked_pos |= {x for j in p for x in tris[j]}
        note[i]['locked_parts'] = [len(small), sum(len(p) for p in small)]
    free = [i for i in sids if mode[i] == 'share']
    fixed_n = sum(len(segs[i]['faces']) for i in mode if mode[i] == 'copy') + sum(goal[i] for i in sids if mode[i] == 'fixed')
    nfree = sum(len(segs[i]['faces']) for i in free)
    for i in free:
        goal[i] = min(len(segs[i]['faces']), max(MIN_SHARE, int(round((target - fixed_n) * len(segs[i]['faces']) / nfree))))
    mesh = Mesh(M, sids, locked_pos) if sids else None
    if mesh:
        mesh.uv_max = cfg.get('uv_max', UV_MAX)
        mesh.uv_guard = cfg.get('uv_guard', True)
        for i in mesh.fp:        # the guard's footprints include the parts left out (their texels are another part's)
            f0 = M0['segs'][i]['faces']
            p0 = parts_of([tuple(M['V'][c[0]][1][:3] for c in f) for f in f0])
            kp = kept_parts.get(i, list(range(len(p0))))
            own = {}
            for k, p in enumerate(p0):
                for j in p:
                    own[j] = (i, kp.index(k)) if k in kp else ('left out', k)
            mesh.fp[i] = Footprint([tuple(M['T'][c[1]][1][:2] for c in f) for f in f0], [own[j] for j in range(len(f0))])
    pfix = {}                   # explicit part targets, keyed like the mesh parts (group, index among the kept parts)
    for i in sids:
        names = PART_NAMES.get((name, segs[i]['g']), [])
        for pname, n in ptarget.get(segs[i]['g'], {}).items():
            pfix[(i, kept_parts[i].index(names.index(pname)))] = n
    for _ in range(8):           # groups that stall above their share: the others take the excess
        if mesh is None:
            break
        if cfg.get('part_shares') or pfix:
            pg = mesh.part_goals(goal, pfix)
            for _ in range(8):       # parts that stall above their share: the other parts of the group take the excess
                mesh.pgoal = pg
                mesh.run(goal, cfg.get('relax'))
                again = False
                for s in sids:
                    over = mesh.nseg[s] - goal[s]
                    adj = [p for p in pg if p[0] == s and not mesh.part_locked[p] and p not in pfix and mesh.npart[p] <= pg[p]
                           and pg[p] > MIN_PART]
                    if over > 0 and adj:
                        tot = sum(pg[p] for p in adj)
                        for p in adj:
                            pg[p] = max(MIN_PART, pg[p] - int(math.ceil(over * pg[p] / tot)))
                        again = True
                if not again:
                    break
        else:
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
    return out, (mesh.relaxed_collapses if mesh else 0), note


def write(M, groups, name, level, target, mtl_lines):
    V, T, N = M['V'], M['T'], M['N']
    ntri = sum(len(f) for _, f, _, _ in groups)
    L = M['notes'] + ['# %s_%s - LOD variant of %s.obj (target %d tris): %d tris, made by Source~/make_lods.py'
                      % (name, level, name, target, ntri),
                      '# Every vertex is an original v/vt/vn of %s.obj; materials come from the original MTL '
                      '(shared, not copied).'
                      % name]
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
    """position-welded open edges over the whole model (a crack between parts or groups adds some), as position pairs"""
    E = {}
    for _, _, cs in R['F']:
        w = [R['V'][c[0]][:3] for c in cs]
        for i in range(3):
            e = (min(w[i], w[(i + 1) % 3]), max(w[i], w[(i + 1) % 3]))
            E[e] = E.get(e, 0) + 1
    return sorted(e for e, n in E.items() if n == 1)


def part_rows(R, src_R, model):
    """every part of every source group (welded by position) against the LOD triangles on its positions ->
    ([dict(group, index, name, src, tris, size, diag, diff: box difference or None)], triangles that join two parts)"""
    src, lod, suv, luv = {}, {}, {}, {}
    for X, D, U in ((src_R, src, suv), (R, lod, luv)):
        for g, m, cs in X['F']:
            D.setdefault((g, m), []).append(tuple(X['V'][c[0]][:3] for c in cs))
            U.setdefault((g, m), []).append(tuple(X['T'][c[1]][:2] for c in cs) if all(c[1] >= 0 for c in cs) else None)
    rows, bridge = [], 0
    for key, tris in src.items():
        parts = parts_of(tris)
        owner = {x: k for k, p in enumerate(parts) for j in p for x in tris[j]}
        fp = None
        if all(u is not None for u in suv[key]):
            fp = Footprint(suv[key], [owner[t[0]] for t in tris])
        got, guv = [[] for _ in parts], [[] for _ in parts]
        for t, uv in zip(lod.get(key, []), luv.get(key, [])):
            ks = {owner.get(x) for x in t}
            if len(ks) != 1 or None in ks:
                bridge += 1
                continue
            k = ks.pop()
            got[k].append(t); guv[k].append(uv)
        names = PART_NAMES.get((model, key[0]), [])
        for k, p in enumerate(parts):
            box = [[f(x[j] for i in p for x in tris[i]) for j in range(3)] for f in (min, max)]
            d = None
            if got[k]:
                b2 = [[f(x[j] for t in got[k] for x in t) for j in range(3)] for f in (min, max)]
                d = max(abs(a - b) for r, r2 in zip(box, b2) for a, b in zip(r, r2))
            bleed = []              # LOD triangles that sample another part's texels (Footprint)
            if fp is not None and all(u is not None for u in guv[k]):
                bleed = [frozenset(t) for t, uv in zip(got[k], guv[k]) if fp.bleeds(uv, k)]
            rows.append(dict(group=key[0], index=k, name=names[k] if k < len(names) else '', src=len(p), tris=len(got[k]),
                             size=[round(b - a, 4) for a, b in zip(*box)], diag=norm(sub(box[1], box[0])), diff=d, bleed=bleed))
    return rows, bridge


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


def verify(trimesh, name, level, text, src_text, src_R, groups, cfg, pack, mtl_lines, note):
    fails, meas = [], {}
    R = raw_obj(text)
    target = cfg['target']
    n = len(R['F'])
    meas['tris'] = n
    if abs(n - target) > SHARE_TOL * target:
        fails.append('triangles %d not within 10 %% of %d' % (n, target))
    for s, faces, mode, goal in groups:
        if mode == 'fixed' and abs(len(faces) - goal) > SHARE_TOL * goal:
            fails.append('group %s has %d triangles, not within 10 %% of its share %d' % (s['g'], len(faces), goal))
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
        fails.append('bounds differ by %.4f m (> 5 mm)' % db)
    gd = max(float(abs(sc.geometry[k].bounds - ss.geometry[k].bounds).max()) for k in ss.geometry)
    meas['group_box_max_diff_m'] = gd
    chk = CHECKS.get((name, level), {})
    if 'pivot' in chk:          # floor: lowest point at y = 0; wall: back face at z = 0
        ax = {'floor': 1, 'wall': 2}[chk['pivot']]
        lo = min(R['V'][c[0]][ax] for _, _, cs in R['F'] for c in cs)
        meas['pivot'] = '%s: min %s = %.6f' % (chk['pivot'], 'xyz'[ax], lo)
        if abs(lo) > PIVOT_TOL:
            fails.append('%s pivot lost: lowest %s = %.6f, not 0' % (chk['pivot'], 'xyz'[ax], lo))
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
    rows, bridge = part_rows(R, src_R, name)
    hidden = {t for n in note.values() for v in n.values() if isinstance(v, dict) for t in v.get('_hidden', [])}
    for r in rows:              # texels of another part: allowed only on lathe bands that are never seen in use
        r['bleed_hidden'] = sum(1 for t in r['bleed'] if t in hidden)
        r['bleed'] = len(r['bleed']) - r['bleed_hidden']
        if r['bleed']:
            fails.append('%d triangles of part %s %s sample another part\'s texels'
                         % (r['bleed'], r['group'], r['name'] or r['index']))
    meas['texel_bleed'] = [sum(r['bleed'] for r in rows), sum(r['bleed_hidden'] for r in rows)]
    count = {}                  # groups build() leaves whole (under MIN_KEEP triangles, or on the keep list)
    for g, m, _ in src_R['F']:
        count[(g, m)] = count.get((g, m), 0) + 1
    copied = {g for (g, m), k in count.items() if k < MIN_KEEP or any(selects(x, dict(g=g, mtl=m)) for x in cfg.get('keep', []))}
    for r in rows:              # left out on purpose: hidden in use (drop_parts) or smaller than min_part
        r['left_out'] = r['group'] not in copied and (r['name'] in cfg.get('drop_parts', {}).get(r['group'], []) or
                                                      r['diag'] < cfg.get('min_part', 0.0))
        if r['left_out'] and r['tris']:
            fails.append('part %s %d %s should be left out' % (r['group'], r['index'], r['name']))
    meas['parts'] = rows
    kept = [r for r in rows if not r['left_out']]
    lost = [r for r in kept if r['diff'] is None or r['diff'] > PART_TOL]
    meas['parts_kept'] = [len(kept) - len(lost), len(kept)]
    meas['parts_left_out'] = ['%s %s' % (r['group'], r['name'] or 'part %d' % r['index']) for r in rows if r['left_out']]
    meas['part_box_max_diff_m'] = max(r['diff'] for r in kept if r['diff'] is not None)
    if bridge:
        fails.append('%d triangles join two parts of the source' % bridge)
    for r in lost:
        fails.append('part %s %d %s (%d tris) %s' % (r['group'], r['index'], r['name'], r['src'], 'missing' if r['diff'] is None
                                                     else 'box differs by %.4f m (> 1 mm)' % r['diff']))
    for gname, want in chk.get('parts', {}).items():
        for pname in want:
            if not any(r['group'] == gname and r['name'] == pname and r['diff'] is not None and r['diff'] <= PART_TOL
                       for r in kept):
                fails.append('named part %s / %s not kept' % (gname, pname))
    fa, fa0 = facing_away(R), facing_away(src_R)
    meas['facing_away'] = [fa0, fa]
    if fa > fa0:
        fails.append('%d triangles face away from their vertex normals (source %d)' % (fa, fa0))
    oe, oe0 = open_edges(R), open_edges(src_R)
    meas['open_edges'] = [len(oe0), len(oe)]
    if len(oe) > len(oe0):
        fails.append('open edges %d > source %d (crack)' % (len(oe), len(oe0)))
    rim = {x for e in oe0 for x in e}
    off = sum(1 for e in oe if e[0] not in rim or e[1] not in rim)
    meas['open_edges_off_source_border'] = off
    if off:
        fails.append('%d open edges have an end off the source\'s open borders (crack along a seam)' % off)
    return fails, meas


def run_pack_checks(pack, texts):
    """Source~/check_obj.py and Source~/cracks.py (beside this script) on a staging folder that holds every LOD
    (in-memory text) and its source OBJ and MTL -> {file: dict(tris, trimesh_faces, disagree, degenerate, cracks)}"""
    here = os.path.dirname(os.path.abspath(__file__))
    env = {k: v for k, v in os.environ.items() if k != 'PYTHONNOUSERSITE'}   # the tools import numpy/trimesh

    def run(script, args):
        r = subprocess.run([sys.executable, '-P', os.path.join(here, script)] + args, capture_output=True, text=True, env=env)
        if r.returncode:
            sys.exit('%s failed:\n%s' % (script, r.stderr[-2000:]))
        return r.stdout
    stage = tempfile.mkdtemp(prefix='interior_lods_')
    try:
        for fn, data in texts.items():
            stem = fn.rsplit('_LOD', 1)[0]
            with open(os.path.join(stage, fn), 'wb') as fh:
                fh.write(data)
            for x in (stem + '.obj', stem + '.mtl'):
                shutil.copyfile(os.path.join(pack, x), os.path.join(stage, x))
        objs = sorted(f for f in os.listdir(stage) if f.endswith('.obj'))
        out = {f: dict(disagree={}, degenerate=0) for f in objs}
        cur = None
        for line in run('check_obj.py', [os.path.join(stage, f) for f in objs]).splitlines():
            m = re.match(r'^(\S+\.obj): (\d+) tris, .*\| trimesh faces (\d+)$', line)
            g = re.match(r'^\s+(\S+) (\d+) tris, (\d+) disagree \([^)]*\), (\d+) degenerate$', line)
            if m:
                cur = out[m.group(1)]; cur['tris'] = int(m.group(2)); cur['trimesh_faces'] = int(m.group(3))
            elif g and cur is not None:
                cur['disagree'][g.group(1)] = int(g.group(3)); cur['degenerate'] += int(g.group(4))
        for f in objs:
            m = re.search(r'open edges (\d+)', run('cracks.py', [os.path.join(stage, f)]))
            out[f]['cracks'] = int(m.group(1)) if m else None
        return out
    finally:
        shutil.rmtree(stage, ignore_errors=True)


def make_previews(pack, names):
    """Source~/render2.py views of every model and its LODs, composed into Source~/LOD_Previews.jpg by Source~/grid.py
    (one row per view: full model, LOD1, LOD2)"""
    here = os.path.dirname(os.path.abspath(__file__))
    env = {k: v for k, v in os.environ.items() if k != 'PYTHONNOUSERSITE'}
    stage = tempfile.mkdtemp(prefix='interior_lod_previews_')
    try:
        rows = []
        for name in names:
            for suffix, args in PREVIEWS.get(name, []):
                row = []
                for fn in [name] + ['%s_%s' % (name, lv) for lv in MODELS[name]]:
                    out = os.path.join(stage, fn + suffix + '.png')
                    r = subprocess.run([sys.executable, '-P', os.path.join(here, 'render2.py'), out,
                                        os.path.join(pack, fn + '.obj'),
                                        '--size', '400'] + args, capture_output=True, text=True, env=env)
                    if r.returncode:
                        sys.exit('render2.py failed:\n' + r.stderr[-2000:])
                    row.append(out)
                rows.append(','.join(row))
        dst = os.path.join(pack, 'Source~', 'LOD_Previews.jpg')
        r = subprocess.run([sys.executable, '-P', os.path.join(here, 'grid.py'), dst] + rows, capture_output=True, text=True,
                           env=env)
        if r.returncode:
            sys.exit('grid.py failed:\n' + r.stderr[-2000:])
        return dst
    finally:
        shutil.rmtree(stage, ignore_errors=True)


def sha_file(p):
    with open(p, 'rb') as fh:
        return hashlib.sha256(fh.read()).hexdigest()


def dump_json(path, data):
    with open(path, 'w', encoding='utf-8', newline='\n') as fh:
        fh.write(json.dumps(data, indent=1))      # the pack's JSON style (Source~/make_docs.py): indent 1, no final newline


def group_text(r):
    return '; '.join('%s %s' % (g['g'], '%s (kept)' % format(g['tris'], ',') if g['mode'] == 'copy' else '%s to %s' % (
        format(g['src'], ','), format(g['tris'], ','))) for g in r['groups'])


def part_text(v, names):
    """'name a to b' for the named parts of a LOD's report"""
    return ', '.join('%s %s to %s' % (p['name'], format(p['src'], ','), format(p['tris'], ',')) for p in v['parts']
                     if p['name'] in names)


def write_docs(pack, rep):
    """Rewrites the 'lods' entries in models.json, the LOD / preview / script entries in sources.json, the README section
    'LOD variants' and the README total-size line; every other entry and section stays as it is."""
    here = os.path.dirname(os.path.abspath(__file__))
    me, prev = 'Source~/make_lods.py', 'Source~/LOD_Previews.jpg'
    # models.json: one 'lods' list per model (file, triangles)
    mp = os.path.join(pack, 'models.json')
    with open(mp, encoding='utf-8') as fh:
        models = json.load(fh)
    for name in MODELS:
        models['models'][name]['lods'] = [dict(level=int(fn[-5]), file=fn, triangles=r['tris'])
                                          for fn, r in sorted(rep.items()) if r['derived_from'] == name + '.obj']
    dump_json(mp, models)
    # sources.json: the LODs, the preview and this script; every other entry kept
    sp = os.path.join(pack, 'sources.json')
    with open(sp, encoding='utf-8') as fh:
        data = json.load(fh)
    entries = {e['file']: e for e in data['files'] if e['file'] not in set(rep) | {me, prev}}
    for fn, r in rep.items():
        base = entries[r['derived_from']]
        v = r['verify']
        left = v['parts_left_out']
        entries[fn] = {
            'file': fn, 'models': base['models'], 'source_url': base['source_url'], 'mirror': base['mirror'],
            'version': 'LOD%s variant made 2026-10-15 by %s from the shipped %s' % (fn[-5], me, r['derived_from']),
            'license': base['license'], 'author': base['author'], 'modified': True, 'sha256': r['sha256'], 'bytes': r['bytes'],
            'kind': 'model LOD', 'lod': int(fn[-5]), 'tris': r['tris'], 'target_tris': r['target'],
            'source_tris': r['source_tris'],
            'derived_from': [{'file': r['derived_from'], 'bytes': base['bytes'], 'sha256': base['sha256']}],
            'tool': me, 'mtllib': r['derived_from'][:-4] + '.mtl (the full model\'s MTL and maps, unchanged)',
            'settings': r['settings'], 'groups': r['groups'], 'relaxed_collapses': v['relaxed_collapses'],
            'verify': {'bounds_max_diff_m': round(v['bounds_max_diff_m'], 6), 'pivot': v.get('pivot'),
                       'parts_kept': v['parts_kept'], 'parts_left_out': left,
                       'part_box_max_diff_m': round(v['part_box_max_diff_m'], 6),
                       'open_edges_welded': v['open_edges'], 'open_edges_off_source_border': v['open_edges_off_source_border'],
                       'facing_away': v['facing_away'], 'zero_area': v['zero_area'], 'texel_bleed': v['texel_bleed'],
                       'check_obj_disagree': v['check_obj_disagree'], 'cracks_open_edges': v['cracks_open_edges']},
            'changes': 'Decimated from %s (%s to %s triangles) by seam-aware half-edge collapse: every vertex and corner is '
                       'an original v/vt/vn (no vertex moved, UVs exact), UV seams, hard edges and group joins shorten only '
                       'along themselves (no cracks), every kept part keeps its exact bounding box, no collapse may turn a '
                       'triangle against its vertex normals or make it sample another part\'s texels%s; %s%s. Uses the '
                       'original MTL and maps (no texture change).'
                       % (r['derived_from'], format(r['source_tris'], ','), format(r['tris'], ','),
                          ('; %d collapses across a UV seam inside a part (those corners take the nearest UV of the same '
                           'group, at most %s apart)' % (v['relaxed_collapses'], r['uv_max'])) if v['relaxed_collapses'] else '',
                          group_text(r), ('; left out: ' + ', '.join(x.split(' ', 1)[1] for x in left)) if left else '')}
    entries[me] = {'file': me, 'models': sorted(MODELS), 'source_url': None, 'version': 'generated 2026-10-15',
                   'license': 'Project-owned', 'author': 'Project', 'sha256': sha_file(os.path.join(here, 'make_lods.py')),
                   'bytes': os.path.getsize(os.path.join(here, 'make_lods.py')),
                   'note': 'LOD builder: a copy of ForensicModels/Source~/decimate_exact.py with the lods-street extensions '
                           '(StreetModels/Source~/make_lods.py), plus part guards, part targets, lathe rebuilds and a '
                           'texel-bleed guard (project-owned)',
                   'derived_from': [{'path': 'Prototype/ThirdParty/ForensicModels/Source~/decimate_exact.py',
                                     'sha256': '3abde4fc3daac0c57d328417ed500fb9b916a4a4f5e42fca5d6a4603f2dcb195'},
                                    {'path': 'Prototype/ThirdParty/StreetModels/Source~/make_lods.py',
                                     'sha256': '7f35da82d77dd3e87c4c5eda12477d3ceff6713f4b02cb58bd1e7e4b2873a22b',
                                     'note': 'the lods-street tool as of 2026-10-15 (copied)'},
                                    {'path': 'Prototype/ThirdParty/StreetModels/Source~/make_lods.py',
                                     'sha256': '3a85204f2f97b59c04a34fe1b0c74e64d825731fedba82f9adcb95993d15aaeb',
                                     'note': 'its later revision of 2026-10-15: the fold guard only'}]}
    pv = os.path.join(pack, prev)
    if os.path.isfile(pv):
        entries[prev] = {'file': prev, 'models': sorted(MODELS), 'source_url': None, 'version': 'generated 2026-10-15',
                         'license': 'Project-owned', 'author': 'Project', 'sha256': sha_file(pv), 'bytes': os.path.getsize(pv),
                         'note': 'software renders (Source~/render2.py, back faces culled) of the full models and their LODs, '
                                 'plus the clock hands close up; made by Source~/make_lods.py --previews'}
    names = sorted(entries)
    data['files'] = ([entries[n] for n in names if not n.startswith('Source~/')] +
                     [entries[n] for n in names if n.startswith('Source~/')])
    dump_json(sp, data)
    # README: section 'LOD variants' (before 'Not found / left out') and the total-size line
    rp = os.path.join(pack, 'README.md')
    with open(rp, encoding='utf-8') as fh:
        readme = fh.read()
    fe1, fe2 = rep['FireExtinguisher_LOD1.obj'], rep['FireExtinguisher_LOD2.obj']
    wc1, wc2 = rep['WallClock_LOD1.obj'], rep['WallClock_LOD2.obj']
    rows = []
    for name, lods in (('FireExtinguisher', (fe1, fe2)), ('WallClock', (wc1, wc2))):
        r0 = lods[0]
        rows.append('| %s.obj (LOD0) | %s | %s | %s |' % (name, format(r0['source_tris'], ','), '; '.join(
            '%s %s' % (g['g'], format(g['src'], ',')) for g in r0['groups']), format(r0['source_bytes'], ',')))
        for lv, r in enumerate(lods, 1):
            rows.append('| %s_LOD%d.obj | %s (%s) | %s | %s |' % (name, lv, format(r['tris'], ','), format(r['target'], ','),
                                                                  group_text(r), format(r['bytes'], ',')))
    over = lambda r: 100.0 * (r['tris'] - r['target']) / r['target']
    v = {fn: r['verify'] for fn, r in rep.items()}
    hands = ['hour hand', 'minute hand', 'second hand']
    bleed_hidden = [v[f]['texel_bleed'][1] for f in ('WallClock_LOD1.obj', 'WallClock_LOD2.obj')]
    cr = lambda name: '%d to %d / %d' % tuple(v[name + '_LOD1.obj']['cracks_open_edges'] +
                                              v[name + '_LOD2.obj']['cracks_open_edges'][1:])
    lathe1 = wc1['groups'][0].get('lathe case', {})
    sec = ['## LOD variants', '',
           'Lower-detail versions of FireExtinguisher and WallClock for a Unity LODGroup (October 15, 2026; **not imported or '
           'checked in Unity yet**). The full model is LOD0; `<Model>_LOD1.obj` and `<Model>_LOD2.obj` sit beside it and use '
           'its MTL and maps (`mtllib <Model>.mtl`, same materials, no texture change), with the same pivot and bounds. '
           'FireAlarmCallPoint has no LODs: it is a US-style pull station and is not placed in the night store.', '',
           '| File | Triangles (target) | Groups | Bytes |', '|---|---|---|---|'] + rows + ['',
           'Made by `Source~/make_lods.py`, a copy of ForensicModels\' `decimate_exact.py` with the lods-street extensions '
           '(StreetModels `Source~/make_lods.py`): a seam-aware half-edge collapse in which every vertex and corner is an '
           'original `v/vt/vn` of the full model (no vertex moves and the UVs stay exact, so the maps fit unchanged); UV '
           'seams, hard edges and group joins shorten only along themselves, so no cracks open, and no collapse may turn a '
           'triangle against its vertex normals. Added for these props:', '',
           '- **Parts** (connected pieces of a group, such as one clock hand) keep their exact bounding boxes, small parts '
           'can be locked unchanged, and the large parts can get their own triangle budgets. No collapse may make a triangle '
           'sample another part\'s texels (without this guard an orange patch showed on the black lever).',
           '- **FireExtinguisher**: LOD1 keeps GaugeGlass and Tag unchanged and all 32 outer columns of the yellow band, so the '
           'recessed cylinder wall never shows through it. LOD2 leaves out the parts under 25 mm (gauge internals, lever pins, '
           'rivet heads, label tab) and the thin pull pin, tag wire and hose ferrule, which are under a pixel at LOD2 '
           'distances; the large parts get budgets (%s) and the band keeps every second outer column; %d collapses cross a UV '
           'seam inside a part (corners move at most 0.2 in UV).'
           % (part_text(v['FireExtinguisher_LOD2.obj'], ['cylinder', 'stand', 'band', 'hose']),
              v['FireExtinguisher_LOD2.obj']['relaxed_collapses']),
           '- **WallClock**: both LODs leave out the parts that face the wall (rear knob, rear plate and back cover, %s '
           'triangles): they cannot be seen on a wall-mounted clock, so use the full model if the clock is ever shown from '
           'behind. Plain decimation turned the case ring into a jagged polygon whose triangles crossed the dial, so the case '
           '(a 96 x 12 lathe grid) is rebuilt on %d (LOD1) or %d (LOD2) of its columns from its own vertices, keeping every '
           'visible profile band with its UV island; the hour and minute hands and the dial are locked, and the second hand '
           'is decimated inside its exact box (%s). LOD2 also leaves out the 6 mm centre cap and the arbor under it.'
           % (format(sum(p['src'] for p in v['WallClock_LOD1.obj']['parts'] if p['name'] in
                         MODELS['WallClock']['LOD1']['drop_parts']['Clock']), ','), lathe1.get('kept_columns', 0),
              wc2['groups'][0].get('lathe case', {}).get('kept_columns', 0),
              ' / '.join(part_text(v[f], ['second hand']).replace('second hand ', '')
                         for f in ('WallClock_LOD1.obj', 'WallClock_LOD2.obj'))), '',
           'Checked by the script before anything is written (exit code 1 on any failure; all passed):', '',
           '- triangles within 10 %% of the target and of each fixed group share: FireExtinguisher_LOD2 is %.1f %% and '
           'WallClock_LOD2 %.1f %% over, because the parts that remain stall at their box, seam and texel guards;'
           % (over(fe2), over(wc2)),
           '- bounds within 5 mm of the full model (measured: %.1f mm) and the pivot kept (extinguisher: floor y = 0; clock: '
           'wall z = 0); same materials (usemtl set) and group order; every corner an original v/vt/vn; groups kept unchanged '
           'are identical; no zero-area triangle;' % (1000 * max(x['bounds_max_diff_m'] for x in v.values())),
           '- every source part present with its box within 1 mm, apart from the parts left out on purpose (listed per file '
           'in sources.json); the clock hands are checked by name (hour and minute hand unchanged, second hand %s);'
           % ' / '.join(part_text(v[f], ['second hand']).replace('second hand ', '') + ' at LOD%d' % lv
                        for lv, f in ((1, 'WallClock_LOD1.obj'), (2, 'WallClock_LOD2.obj'))),
           '- no open edge with an end off the source\'s open borders (a crack along a UV seam would add one); '
           '`Source~/cracks.py` open edges: FireExtinguisher %s, WallClock %s (full model to LOD1 / LOD2: never more);'
           % (cr('FireExtinguisher'), cr('WallClock')),
           '- `Source~/check_obj.py`: %d faces whose winding disagrees with their vertex normals (as in the full models), and '
           'trimesh reads every triangle;' % sum(sum(x['check_obj_disagree'][1].values()) for x in v.values()),
           '- no triangle samples another part\'s texels, apart from %d / %d triangles of the clock case\'s hidden back band '
           '(LOD1 / LOD2), which faces the wall.' % tuple(bleed_hidden), '',
           '`Source~/LOD_Previews.jpg`: full model, LOD1 and LOD2 side by side (software renders with back faces culled, '
           '`Source~/render2.py`), plus a close-up of the clock dial: the hour, minute and second hands are intact at LOD1 '
           '(and at LOD2, where the centre cap is left out).', '',
           'Suggested LODGroup transitions (screen-relative height; a starting point, unverified in Unity, tune on the Quest 2 '
           'with QualitySettings.lodBias 1): FireExtinguisher LOD0 above 0.25, LOD1 above 0.08, LOD2 above 0.015; WallClock '
           'LOD0 above 0.15, LOD1 above 0.035, LOD2 above 0.008; culled below. With a vertical field of view of about 90 '
           'degrees that is LOD1 from about 1.3 m and LOD2 from about 4 m for the extinguisher, LOD1 from about 1.1 m and LOD2 '
           'from about 4.5 m for the clock (where its 12-sided case is under about a pixel off round).', '',
           'Rebuild from the repository root: `python3 -I Prototype/ThirdParty/InteriorModels/Source~/make_lods.py '
           'Prototype/ThirdParty/InteriorModels --previews` (deterministic: a rerun gives the same bytes). It rewrites the '
           '`lods` entries in models.json, the LOD entries in sources.json, this section and the total size above. '
           '`Source~/make_docs.py` rebuilds the pack without the LODs, so run make_lods.py after it.', '']
    section = '\n'.join(sec)
    if '## LOD variants\n' in readme:
        a = readme.index('## LOD variants\n')
        b = readme.find('\n## ', a + 1)
        readme = readme[:a] + section + ('\n' + readme[b + 1:] if b >= 0 else '')
    else:
        a = readme.index('## Not found / left out\n')
        readme = readme[:a] + section + '\n' + readme[a:]
    for _ in range(5):           # the total includes this README: repeat until the line no longer changes it
        sizes = [os.path.getsize(os.path.join(d, f)) for d, _, fs in os.walk(pack) for f in fs if os.path.join(d, f) != rp]
        total = sum(sizes) + len(readme.encode('utf-8'))
        new = re.sub(r'Total size \*\*[0-9.]+ MB\*\* \(\d+ bytes, including Source~ and these documents\); '
                     r'largest file [0-9.]+ MB\.',
                     'Total size **%.1f MB** (%d bytes, including Source~ and these documents); largest file %.2f MB.'
                     % (total / 1e6, total, max(sizes + [len(readme.encode('utf-8'))]) / 1e6), readme)
        if new == readme:
            break
        readme = new
    with open(rp, 'w', encoding='utf-8', newline='\n') as fh:
        fh.write(readme)


def main():
    args = sys.argv[1:]
    pack = args.pop(0)
    report, previews = None, False
    while args and args[0].startswith('--'):
        a = args.pop(0)
        if a == '--report':
            report = args.pop(0)
        elif a == '--previews':
            previews = True
        else:
            sys.exit('unknown option ' + a)
    names = args or list(MODELS)
    for name in names:
        if name not in MODELS:
            sys.exit('no LOD table for %s (models: %s)' % (name, ', '.join(MODELS)))
    trimesh = load_trimesh()
    rep, ok, out_files = {}, True, {}
    for name in names:
        src = os.path.join(pack, name + '.obj')
        with open(src, encoding='utf-8') as fh:
            src_text = fh.read()
        M = parse(src)
        src_R = raw_obj(src_text)
        mtl_lines = M['head'] or ['mtllib %s.mtl' % name]
        for level, cfg in MODELS[name].items():
            groups, relaxed, note = build(M, cfg, name)
            text = write(M, groups, name, level, cfg['target'], mtl_lines)
            fails, meas = verify(trimesh, name, level, text, src_text, src_R, groups, cfg, pack, mtl_lines, note)
            meas['relaxed_collapses'] = relaxed
            fn = '%s_%s.obj' % (name, level)
            data = text.encode('utf-8')
            gl = []
            for i, (s, f, mode, goal) in enumerate(groups):
                d = dict(g=s['g'], mtl=s['mtl'], mode=mode, src=len(M['segs'][i]['faces']), tris=len(f))
                if mode != 'copy':
                    d['share'] = goal
                d.update({k: ({a: b for a, b in v.items() if not a.startswith('_')} if isinstance(v, dict) else v)
                          for k, v in note[i].items()})
                gl.append(d)
            rep[fn] = dict(derived_from=name + '.obj', target=cfg['target'], settings=cfg, relax=cfg.get('relax'),
                           uv_max=cfg.get('uv_max', UV_MAX) if cfg.get('relax') else None, tris=meas['tris'],
                           source_tris=len(src_R['F']), source_bytes=len(src_text.encode('utf-8')),
                           bytes=len(data), sha256=hashlib.sha256(data).hexdigest(), verify=meas, groups=gl)
            oe, fa = meas.get('open_edges', [0, 0]), meas.get('facing_away', [0, 0])
            print('%-26s target %5d -> %5d tris, %7d bytes, bounds diff %.5f m (groups %.5f m, parts %s within %.5f m), '
                  'open edges %d -> %d (%d off the source border), facing away %d -> %d, zero-area %d, relaxed collapses %d'
                  % (fn, cfg['target'], meas['tris'], len(data), meas.get('bounds_max_diff_m', -1),
                     meas.get('group_box_max_diff_m', -1), '%d/%d' % tuple(meas.get('parts_kept', (0, 0))),
                     meas.get('part_box_max_diff_m', -1), oe[0], oe[1], meas.get('open_edges_off_source_border', -1),
                     fa[0], fa[1], meas.get('zero_area', -1), relaxed))
            for d in gl:
                print('    %-12s %-12s %-6s %5d -> %5d' % (d['g'][:12], d['mtl'], d['mode'], d['src'], d['tris'])
                      + ('  (share %d)' % d['share'] if 'share' in d else '')
                      + ''.join('  %s %s' % (k, d[k]) for k in sorted(d) if k.startswith(('locked', 'lathe', 'dropped'))))
            for p in meas.get('parts', []):
                print('      part %-10s %2d %-12s %4d -> %4d tris, size %s, box diff %s'
                      % (p['group'], p['index'], p['name'], p['src'], p['tris'], p['size'], 'left out' if p['left_out'] else
                         'missing' if p['diff'] is None else '%.5f m' % p['diff']))
            if 'pivot' in meas:
                print('    pivot', meas['pivot'])
            for f in fails:
                print('VERIFY FAILED %s %s: %s' % (name, level, f))
            ok &= not fails
            out_files[fn] = data
    if out_files:               # the pack's own checks on a staging copy
        chk = run_pack_checks(pack, out_files)
        for fn in out_files:
            stem = fn.rsplit('_LOD', 1)[0] + '.obj'
            c, c0 = chk[fn], chk[stem]
            vm = rep[fn]['verify']
            vm['check_obj_disagree'] = [c0['disagree'], c['disagree']]
            vm['cracks_open_edges'] = [c0['cracks'], c['cracks']]
            print('%-26s check_obj.py: %d tris (trimesh %d), disagree %s (source %s), degenerate %d; cracks.py: open edges %s '
                  '(source %s)' % (fn, c.get('tris', -1), c.get('trimesh_faces', -1), c['disagree'], c0['disagree'],
                                   c['degenerate'], c['cracks'], c0['cracks']))
            bad = []
            if c.get('tris') != rep[fn]['tris'] or c.get('trimesh_faces') != rep[fn]['tris']:
                bad.append('check_obj.py reads %s tris (trimesh %s), not %d'
                           % (c.get('tris'), c.get('trimesh_faces'), rep[fn]['tris']))
            if any(n > c0['disagree'].get(g, 0) for g, n in c['disagree'].items()):
                bad.append('check_obj.py: faces whose winding disagrees with their normals %s (source %s)'
                           % (c['disagree'], c0['disagree']))
            if c['degenerate']:
                bad.append('check_obj.py: %d degenerate faces' % c['degenerate'])
            if c['cracks'] is None or c0['cracks'] is None or c['cracks'] > c0['cracks']:
                bad.append('cracks.py: open edges %s > source %s (crack)' % (c['cracks'], c0['cracks']))
            for f in bad:
                print('VERIFY FAILED %s: %s' % (fn, f))
            ok &= not bad
    if not ok:
        sys.exit('VERIFY failed: nothing written')
    for fn, data in out_files.items():
        with open(os.path.join(pack, fn), 'wb') as fh:
            fh.write(data)
    if previews:
        print('previews:', make_previews(pack, names))
    if report:
        with open(report, 'w', encoding='utf-8', newline='\n') as fh:
            json.dump(rep, fh, indent=1)
    docs = ('models.json', 'sources.json', 'README.md')
    if set(names) == set(MODELS) and all(os.path.isfile(os.path.join(pack, f)) for f in docs):
        write_docs(pack, rep)
        print('models.json, sources.json and README.md: LOD entries rewritten')
    print('VERIFY passed; wrote', ', '.join(out_files))


if __name__ == '__main__':
    main()
