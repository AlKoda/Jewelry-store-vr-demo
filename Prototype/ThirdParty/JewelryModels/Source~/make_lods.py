"""LOD variants of six JewelryModels items: python3 -I make_lods.py PACKDIR [--report OUT.json] [--previews] [--no-docs]
[Model ...]. From the repository root (VERIFY, write the LODs, render Source~/Preview_LODs.jpg, update the documents):
python3 -I Prototype/ThirdParty/JewelryModels/Source~/make_lods.py Prototype/ThirdParty/JewelryModels --previews
Writes <Model>_LOD1.obj and <Model>_LOD2.obj to PACKDIR for GoldCoin, RingGoldBand, RingBoxClosed, JewelersLoupe,
GemParcel and NecklacePearlDropped (GoldBar1kg, 364 tris, gets none). The full model stays LOD0; no existing OBJ, MTL
or texture is changed. In the night store LOD1 is the top level of the small items (coin, band, ring box, loupe).
Began as a copy of Prototype/ThirdParty/ForensicModels/Source~/decimate_exact.py (split per corner, decimate per group,
keep original vertices) and carries the lods-street extensions (Prototype/ThirdParty/StreetModels/Source~/make_lods.py
as of 2026-10-15; both SHA-256 in TOOLS below). From the lods-street tool:
(a) Faces are fan-triangulated and split per (v, vt, vn) corner value; vn is optional.
(b) Groups are runs of faces under one g/usemtl state; their g/usemtl lines, names and order are written back
    unchanged. 'mtllib' names the ORIGINAL MTL (shared, never copied).
(c) Groups under 64 triangles and groups on the model's keep list are copied unchanged (their positions are locked in
    the others); every other group is decimated to its explicit target, or to its proportional share of what is left.
(d) Seam-aware half-edge collapse with quadric error (meshoptimizer's rules) on one mesh of all decimated groups:
    positions are welded only to find neighbours, so hard edges, UV seams and group boundaries are borders that shorten
    only along themselves, on both sides at once (no cracks); open borders move only along the border; a line where
    three or more sheets meet shortens only along itself, on all sheets at once. No vertex moves: every kept vertex is
    an original v/vt/vn, written as its original text. The last vertex on each face of a group's bounding box is never
    removed. No triangle turns more than ~78 degrees in one collapse, and none may end up facing more than 90 degrees
    away from the source triangle it comes from (here for every triangle: the street tool's 1 cm2 lower limit is larger
    than most jewellery).
(e) relax: a vertex may also collapse across a seam; the corners the collapse edge does not map take the target's
    corner with the nearest normal/UV in the same group (never further than uv_max in UV), at an attribute cost.
    (Defined, but no level here uses it.)
(f) Deterministic (no randomness, sorted tie-breaks): a rerun reproduces the same bytes.
Added for the jewellery:
(g) Per-part decimation of the gem and pearl groups ('parts': GemParcel's Diamond at LOD2, the necklace's Pearl): the
    group is split into its connected components (welded by position) and each stone or pearl gets its proportional
    share of the group's target, rounded to an even number (a closed part loses two triangles per collapse), with a
    floor of 8 triangles; the last vertex on each face of every part's bounding box is never removed. Every stone and
    pearl stays a closed shape with its exact box (a stone keeps its lowest point on the paper; a pearl at 8 triangles
    is the octahedron of its six extreme points). A part that stalls above its share: the other parts of its group take
    the excess (never below the floor); if it is still above, it becomes the convex hull of its box vertices when that
    is no larger (half-edge collapses cannot flip an edge; two LOD2 pearls need it). Per-corner vn, for every
    decimated group: a connected part whose every corner normal is its face's normal (faceted stones, the gem paper,
    the loupe's chrome plates) is decimated on (v, vt) alone and written with one new vn per triangle, its facet
    normal, so it stays flat-shaded; every other part keeps its original per-corner vn (smooth metal, pearls, velvet),
    hard edges included.
(h) 'close' (the necklace's Cord): its 58 silk knots are open tubes whose ends lie inside the pearls' drill holes, and an
    open tube cannot lose its last 6 triangles by edge collapse. Each end loop is first capped with a fan of the loop's
    own corners, so every knot is closed and can go down to 4. 'cull' (Cord at LOD2): 50 triangles cannot hold 58
    knots, so 12 of them, evenly spaced along the strand, are kept (the knots are under half a pixel at LOD2).
(i) 'grid': a part built by a lathe or a sweep ('revolve': rings of one radius and height about an axis) or an extruded
    plate ('prism': the border loops of each flat layer) is rebuilt as a regular grid instead of collapsed: every k-th
    column (or the columns nearest an even spacing, keeping the four box extremes) and the profile rows whose removal
    changes the profile least (Visvalingam; poles and one row per box extreme locked), re-triangulated in the source
    winding with the original corner whose normal is nearest each new triangle's. Edge collapse on these regular grids
    picks among equal costs and leaves them lumpy (the band at 860 looked hammered); the grid keeps them even.
Also changed from the lods-street tool:
- Zero-area triangles of the sources (two coincident vertices where a lathe profile meets its axis: the coin centres,
  the ring-box roll ends, the loupe lens apexes) are dropped before decimation; they would lock their neighbours.
- 'drop_internal' (the closed ring box's navy shells): coincident triangle pairs with opposite winding (the base's top
  face against the lid's bottom face) lie inside the closed box and are dropped; they made every vertex there
  non-manifold. 'near_hard': the vertices next to a hard edge are kept (the lid seam keeps its LOD0 shading strip).
- 'rest' (GemParcel): the paper triangles under the lowest 0.6 mm of every stone are locked, so each stone rests on
  the decimated paper exactly as on LOD0.
- UV seams of untextured materials (no map in the MTL or in materials.json) are not borders: their vt is still an
  original value, but nothing reads it. Seams of the textured GoldCoin and GemPaper are kept exactly. Smooth corners at
  one position whose normals differ by less than 5 degrees are one corner (the lathe leaves such fans at its poles).
- Winding guard (as in PeopleModels/Source~/make_lods.py): no collapse may turn a smooth triangle against its own
  corner normals.
- The source OBJ's leading comment lines (pack, units, pivot) are kept at the top of each LOD.
VERIFY (in memory, before anything is written; exit 1 naming the model and the failing measure, nothing written):
triangles within 10 % of the target and of every explicit group target; bounds within 0.2 mm of the source (trimesh,
process=False); nothing below y = 0; the ring's hole centre within 0.1 mm of the source's (circle fit to the innermost
0.15 mm of the inner surface; RingGoldBand at y 0.0103); the connected-component count of every group unchanged (12
stones, 58 pearls; culled knots excepted); every stone and pearl closed (no open edge within the part), with at least
'min_part' triangles (GemParcel LOD2: 12 per stone) and its exact box; no GemParcel stone more than 0.1 mm into the
decimated paper; the same g/usemtl order and usemtl set, vt/vn presence as in the source, the original mtllib; every
v/vt/vn index valid, every face a triangle, no zero-area triangle (< 1e-10 m2 as written); every corner an original
(v, vt) and, on smooth parts, an original (v, vt, vn); every faceted triangle's vn its facet normal (FACET_TOL); copied
groups identical; no new open edges (position-welded over the whole model); no more triangles wound against their own
vertex normals than the source; trimesh reads every triangle.
--previews runs Source~/render_lods.py (Source~/Preview_LODs.jpg) after writing. Unless --no-docs, the 'lods' entry of
each model in models.json, the LOD, preview and script entries and 'lod_variants' in sources.json, the README section
'LOD variants' (with its lines in the introduction and in 'Regenerating') and the LOD paragraph of LICENSE.txt are
then rewritten; every other entry and section is kept (VERIFY checks it). Source~/make_docs.py rebuilds the documents
without the LODs: run this script after it.
The build needs only the standard library; -I hides the user site-packages, so that one directory (never the script dir
or the cwd) is appended to import trimesh for VERIFY. render_lods.py runs as its own process (numpy, trimesh, Pillow)."""
import sys, os, io, site, json, heapq, hashlib, math, subprocess

MIN_KEEP = 64            # groups under this many triangles are copied unchanged
MIN_SHARE = 32           # floor of a decimated group's share (as in decimate_exact.py)
PART_FLOOR = 8           # (g) floor of a stone's or pearl's share
BORDER_W, SEAM_W = 10.0, 1.0   # edge-constraint quadric weights: open borders, UV/normal/group seams
ATTR_L2 = 1e-4           # m2: an attribute difference of 0.1 costs like a 1 mm offset over 1 cm
UV_W = 100.0             # attribute distance = |dn|^2 + UV_W |duv|^2 (0.01 UV ~ 0.1 normal)
UV_MAX = 0.02            # default relax UV limit: a corner never moves further in UV (about 20 texels at 1K)
LEN_W = 1e-10            # tie-break on flat areas: shorter edges first (keeps triangles compact)
MIN_COS = 0.2            # reject a collapse that turns any triangle by more than ~78 degrees
FOLD_AREA = 0.0          # m2: triangles larger than this may never face more than 90 degrees away from their source
MIN_AREA = 1e-9         # m2, 10x the zero-area limit (the smallest source facet, on a 2.6 mm stone, is 2.4e-8 m2)
BOX_TOL = 1e-7           # m: vertices this close to a face of their group's or part's box hold that face
GRID_TOL = 3e-6          # m: (i) radii and heights closer than this are one grid row (positions have 6 decimals)
FLAT_COS = 0.9995        # a corner normal this close to its face normal counts as the facet normal (written with 4 decimals)
WELD_COS = 0.9962        # smooth corners at one position (and UV) whose normals are within 5 degrees are one corner
FACET_TOL = 2e-3         # VERIFY: |written facet vn - geometric unit normal|
BOUNDS_TOL = 0.0002      # m: LOD bounds within 0.2 mm of the source bounds
HOLE_TOL = 0.0001        # m: ring hole centre within 0.1 mm of the source's
FLOOR_TOL = 1e-9         # m: nothing below y = 0
MAX_SINK = 0.0001        # m: a GemParcel stone vertex may lie at most 0.1 mm under the decimated paper's top sheet
REST_ZONE = 0.0006       # m: 'rest' locks the paper triangles under the lowest 0.6 mm of every stone
FACET = -2               # corner normal index meaning "write this triangle's facet normal"
MODELS = {
    # keep: copied unchanged ('mtl:X' = every group using material X); fixed: explicit group targets; parts: groups
    # decimated per connected part (g); min_part: VERIFY floor per part; relax: 'early' or 'late'; uv_max: relax UV limit.
    'GoldCoin': {'LOD1': dict(target=430, grid={'GoldCoin': [dict(kind='revolve', axis=1, cols=32, rows=7)]}),
                 'LOD2': dict(target=110, grid={'GoldCoin': [dict(kind='revolve', axis=1, cols=12, rows=5)]})},
    'RingGoldBand': {'LOD1': dict(target=860, grid={'Gold18k': [dict(kind='revolve', axis=0, cols=48, rows=9)]}),
                     'LOD2': dict(target=220, grid={'Gold18k': [dict(kind='revolve', axis=0, cols=16, rows=7)]})},
    # Gold18k (hinge pin, 32 tris) is under MIN_KEEP: copied unchanged at both levels.
    # VelvetCream (lining well, lid lining, two rolls) lies wholly inside the closed shells (4 mm clear on every side):
    # it is cut to a token share and the visible navy shell takes the rest.
    'RingBoxClosed': {'LOD1': dict(target=400, drop_internal=['VelvetNavy'], fixed={'VelvetNavy': 336, 'VelvetCream': 32},
                                   near_hard=['VelvetNavy']),
                      'LOD2': dict(target=120, drop_internal=['VelvetNavy'], fixed={'VelvetNavy': 64, 'VelvetCream': 24})},
    # The barrel, both lenses, both case plates and the pivot pin are lathe / extrusion grids (i); barrel and lenses
    # keep the same 12 (LOD2: 8) columns, so each lens rim lies on the barrel's inner wall polygon (no gap).
    'JewelersLoupe': {'LOD1': dict(target=430, fixed={'BlackMetal': 100, 'Glass': 160, 'Chrome': 170},
                                   grid={'BlackMetal': [dict(kind='revolve', axis=1, cols=12, rows=4)],
                                         'Glass': [dict(kind='revolve', axis=1, cols=12, rows=4)],
                                         'Chrome': [dict(kind='prism', axis=1, cols=10, rows=4),
                                                    dict(kind='revolve', axis=1, cols=4, rows=2)]}),
                      'LOD2': dict(target=140, fixed={'BlackMetal': 48, 'Glass': 48, 'Chrome': 52},
                                   grid={'BlackMetal': [dict(kind='revolve', axis=1, cols=8, rows=3)],
                                         'Glass': [dict(kind='revolve', axis=1, cols=8, rows=2)]})},
    'GemParcel': {'LOD1': dict(target=1620, keep=['Diamond'], fixed={'GemPaper': 300}, rest={'GemPaper': 'Diamond'}),
                  'LOD2': dict(target=340, fixed={'GemPaper': 100, 'Diamond': 240}, parts=['Diamond'],
                               min_part={'Diamond': 12}, rest={'GemPaper': 'Diamond'})},
    'NecklacePearlDropped': {'LOD1': dict(target=2100, keep=['Gold18k'], fixed={'Pearl': 1740, 'Cord': 250}, parts=['Pearl'],
                                          close=['Cord']),
                             'LOD2': dict(target=530, fixed={'Pearl': 464, 'Cord': 50, 'Gold18k': 16}, parts=['Pearl'],
                                          close=['Cord'], cull={'Cord': 12})},
}
CHECKS = {   # extra VERIFY measures per model: ring hole (axis X, centre y), stones resting on the paper
    'RingGoldBand': dict(hole=dict(axis=0, y=0.0103)),
    'GemParcel': dict(stones=('Diamond', 'GemPaper')),
}
# Suggested Unity LODGroup screen-relative transition heights (LOD Bias 1): each level is shown while the model's screen
# height is at least its value, and the model is culled below the last. Suggestions only, unverified in Unity. The four
# small items start at LOD1 in the night store (their LOD0 value is for close inspection elsewhere).
SWITCH = {
    'GoldCoin': (0.10, 0.015, 0.002), 'RingGoldBand': (0.10, 0.015, 0.002),
    'RingBoxClosed': (0.10, 0.02, 0.003), 'JewelersLoupe': (0.10, 0.02, 0.003),
    'GemParcel': (0.10, 0.03, 0.004), 'NecklacePearlDropped': (0.10, 0.03, 0.004),
}
TOP_LOD1 = ('GoldCoin', 'RingGoldBand', 'RingBoxClosed', 'JewelersLoupe')
FOV = (('Quest 2', 93.0), ('desktop', 60.0))     # vertical field of view (degrees) for the distances in the README
DATE = 'October 15, 2026'
TOOL_ID = 'Source~/make_lods.py'
TOOLS = {   # what this script was copied from (paths from the repository root, SHA-256 at the time of the copy)
    'decimate_exact': ('Prototype/ThirdParty/ForensicModels/Source~/decimate_exact.py',
                       '3abde4fc3daac0c57d328417ed500fb9b916a4a4f5e42fca5d6a4603f2dcb195'),
    'lods_street': ('Prototype/ThirdParty/StreetModels/Source~/make_lods.py',
                    '3a85204f2f97b59c04a34fe1b0c74e64d825731fedba82f9adcb95993d15aaeb'),
}
LOCKED, MANIFOLD, BORDER, SEAM, CHAIN = range(5)   # vertex kinds (CHAIN comes as (CHAIN, (p, q), k))


def parse(path):
    """-> dict(V, T, N: [(text, floats)], lead: leading comment lines, head: mtllib lines,
    segs: [dict(head, g, mtl, faces)], tail)"""
    V, T, N, head, segs, pend, lead = [], [], [], [], [], [], []
    g = mtl = cur = None
    for no, raw in enumerate(open(path, encoding='utf-8'), 1):
        line = raw.strip()
        if not line:
            continue
        if line[0] == '#':
            if not (V or T or N or head or segs or pend):
                lead.append(line)
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
    return dict(V=V, T=T, N=N, lead=lead, head=head, segs=segs, tail=pend)


def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def norm(a): return math.sqrt(dot(a, a))


def unit(a):
    n = norm(a)
    return (a[0] / n, a[1] / n, a[2] / n) if n > 0 else (0.0, 0.0, 0.0)


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


def components(pos_tris):
    """connected parts of a triangle list (three hashable position keys each) -> lists of triangle indices, in the
    order of each part's first triangle"""
    par = {}

    def root(x):
        while par[x] != x:
            par[x] = par[par[x]]; x = par[x]
        return x
    for w in pos_tris:
        for x in w:
            par.setdefault(x, x)
        for x in w[1:]:
            a, b = root(w[0]), root(x)
            if a != b:
                par[max(a, b)] = min(a, b)
    out, at = [], {}
    for i, w in enumerate(pos_tris):
        r = root(w[0])
        if r not in at:
            at[r] = len(out); out.append([])
        out[at[r]].append(i)
    return out


def pos_of(M, f):
    return tuple(M['V'][c[0]][1][:3] for c in f)


def cap_loops(M, faces):
    """fan caps for the open border loops of a part (welded by position): each loop is closed by triangles from its
    first vertex, wound against the border so the part becomes closed; corners are the loop vertices' own corners"""
    nxt, cor = {}, {}
    for f in faces:
        ps = pos_of(M, f)
        for i in range(3):
            nxt.setdefault(ps[i], []).append(ps[(i + 1) % 3]); cor.setdefault(ps[i], f[i])
    border = {a: [b for b in bs if a not in nxt.get(b, [])] for a, bs in nxt.items()}
    border = {a: bs for a, bs in border.items() if bs}
    if any(len(bs) != 1 for bs in border.values()):
        sys.exit('close: a border vertex with %d open edges' % max(len(bs) for bs in border.values()))
    out, seen = [], set()
    for a0 in sorted(border):
        if a0 in seen:
            continue
        loop, a = [], a0
        while a not in seen:
            seen.add(a); loop.append(a); a = border[a][0]
        if a != a0 or len(loop) < 3:
            sys.exit('close: an open border that is not a simple loop')
        out += [(cor[loop[0]], cor[loop[j + 1]], cor[loop[j]]) for j in range(1, len(loop) - 1)]
    return out


def convex_hull(P):
    """incremental convex hull of a few points -> outward triangles as index triples, or None (flat or too few)"""
    n = len(P)
    if n < 4:
        return None
    lo = [min(p[k] for p in P) for k in range(3)]; hi = [max(p[k] for p in P) for k in range(3)]
    eps = 1e-12 * norm(sub(hi, lo)) ** 3
    i0 = 0
    i1 = max(range(n), key=lambda i: (norm(sub(P[i], P[i0])), -i))
    i2 = max(range(n), key=lambda i: (norm(cross(sub(P[i1], P[i0]), sub(P[i], P[i0]))), -i))
    nr = cross(sub(P[i1], P[i0]), sub(P[i2], P[i0]))
    i3 = max(range(n), key=lambda i: (abs(dot(nr, sub(P[i], P[i0]))), -i))
    if abs(dot(nr, sub(P[i3], P[i0]))) <= eps:
        return None
    c = tuple(sum(P[i][k] for i in (i0, i1, i2, i3)) / 4 for k in range(3))

    def fnorm(f):
        return cross(sub(P[f[1]], P[f[0]]), sub(P[f[2]], P[f[0]]))
    faces = []
    for f in ((i0, i1, i2), (i0, i2, i3), (i0, i3, i1), (i1, i3, i2)):
        faces.append(f if dot(fnorm(f), sub(P[f[0]], c)) > 0 else (f[0], f[2], f[1]))
    for i in range(n):
        if i in (i0, i1, i2, i3):
            continue
        vis = [f for f in faces if dot(fnorm(f), sub(P[i], P[f[0]])) > eps]
        if not vis:
            continue
        es = {(f[k], f[(k + 1) % 3]) for f in vis for k in range(3)}
        faces = [f for f in faces if f not in vis] + [(a, b, i) for a, b in sorted(es) if (b, a) not in es]
    return faces


def rest_positions(M, paper, stones):
    """positions of the upward paper triangles under the lowest REST_ZONE of every stone (connected part)"""
    up = [pos_of(M, f) for f in paper]
    up = [t for t in up if cross(sub(t[1], t[0]), sub(t[2], t[0]))[1] > 0]
    tris = [pos_of(M, f) for f in stones]
    out = set()
    for part in components(tris):
        st = [tris[j] for j in part]
        pts = {p for t in st for p in t} | {tuple(sum(p[k] for p in t) / 3 for k in range(3)) for t in st}
        yl = min(p[1] for p in pts)
        for x, y, z in sorted(pts):
            if y > yl + REST_ZONE:
                continue
            for t in up:
                (ax, _, az), (bx, _, bz), (cx, _, cz) = t
                den = (bz - cz) * (ax - cx) + (cx - bx) * (az - cz)
                if den == 0:
                    continue
                w0 = ((bz - cz) * (x - cx) + (cx - bx) * (z - cz)) / den
                w1 = ((cz - az) * (x - cx) + (ax - cx) * (z - cz)) / den
                if min(w0, w1, 1 - w0 - w1) >= -1e-9:
                    out.update(t)
    return out


def clusters(vals, tol):
    """value -> cluster number; sorted values closer than tol to their predecessor share a cluster"""
    out, k, prev = {}, -1, None
    for x in sorted(set(vals)):
        if prev is None or x - prev > tol:
            k += 1
        out[x] = k; prev = x
    return out


def grid_part(M, faces, spec, faceted):
    """(i) structured rebuild of a part made by a lathe or sweep ('revolve': rows are rings of one (radius, height)
    about spec['axis'], e.g. the band, the coin, the loupe's pivot pin) or an extruded plate ('prism': rows are the
    border loops of each flat layer, e.g. the loupe's chrome case plates). Every row has the same number of columns;
    points on the axis are poles. Kept: spec['cols'] columns, every k-th (else, on a revolve, those nearest an even
    spacing, which keep the four box extremes when cols is a multiple of 4; on a prism, those that change the outline
    least, extremes locked) and spec['rows'] rows (the profile points whose removal changes the
    profile least, Visvalingam; one row per box extreme and the poles are locked); the kept grid is re-triangulated in
    the source winding. Corners: the original corner whose normal is nearest the new triangle's (faceted: its facet
    normal). -> (list of faces, dict of kept columns and rows), or None if the part is not such a grid"""
    ax = spec['axis']; o = [k for k in range(3) if k != ax]
    P = sorted({p for f in faces for p in pos_of(M, f)})
    lo, hi = box(P)
    if spec['kind'] == 'prism':
        rowkey = {}
        for h in sorted({p[ax] for p in P}):
            flat = [pos_of(M, f) for f in faces if all(abs(q[ax] - h) < 1e-9 for q in pos_of(M, f))]
            es = {}
            for t in flat:
                for i in range(3):
                    e = (min(t[i], t[(i + 1) % 3]), max(t[i], t[(i + 1) % 3])); es[e] = es.get(e, 0) + 1
            loops = components([e for e, n in es.items() if n == 1])
            bl = [e for e, n in es.items() if n == 1]
            for k, lp in enumerate(loops):
                for j in lp:
                    for q in bl[j]:
                        rowkey[q] = (h, k)
        if len(rowkey) != len(P):
            return None
        inner = {}
        for q, key in rowkey.items():
            inner.setdefault(key, []).append(q)
        cl = min(inner.values(), key=lambda L: (max(q[o[0]] for q in L) - min(q[o[0]] for q in L)))
        cen = [(min(q[k] for q in cl) + max(q[k] for q in cl)) / 2 for k in range(3)]
    else:
        cen = [(lo[k] + hi[k]) / 2 for k in range(3)]
        rq = {q: math.hypot(q[o[0]] - cen[o[0]], q[o[1]] - cen[o[1]]) for q in P}
        cr, ch = clusters([rq[q] for q in P], GRID_TOL), clusters([q[ax] for q in P], GRID_TOL)
        rowkey = {q: ('pole', ch[q[ax]]) if rq[q] < GRID_TOL else (cr[rq[q]], ch[q[ax]]) for q in P}
    rows = {}
    for q in P:
        rows.setdefault(rowkey[q], []).append(q)
    for key, L in rows.items():                 # each row in angle order from the +o[0] direction
        L.sort(key=lambda q: (math.atan2(q[o[1]] - cen[o[1]], q[o[0]] - cen[o[0]]) + 1e-9) % (2 * math.pi))
    poles = {k for k, L in rows.items() if len(L) == 1}
    ncol = {len(L) for k, L in rows.items() if k not in poles}
    if len(ncol) != 1 or len(rows) - len(poles) < 2:
        return None
    C = ncol.pop()
    rc = {}                                       # position -> (row, column); a pole has column None
    for k, L in rows.items():
        for j, q in enumerate(L):
            rc[q] = (k, None if k in poles else j)
    adj = {k: set() for k in rows}                 # profile: rows joined by source triangles
    for f in faces:
        ks = {rc[q][0] for q in pos_of(M, f)}
        for a in ks:
            adj[a] |= ks - {a}
    if any(len(v) > 2 for v in adj.values()):
        return None
    ends = sorted((k for k in rows if len(adj[k]) == 1), key=str)
    cyc = not ends
    prof, prev, k = [], None, (ends[0] if ends else sorted(rows, key=str)[0])
    while k is not None and k not in prof:
        prof.append(k)
        nx = sorted((x for x in adj[k] if x != prev and x not in prof), key=str)
        prev, k = k, (nx[0] if nx else None)
    if len(prof) != len(rows):
        return None
    sidx = {k: i for i, k in enumerate(prof)}
    # source winding in (column, profile) parameter space
    sign = 0
    for f in faces:
        ps = pos_of(M, f)
        if any(rc[q][1] is None for q in ps):
            continue
        cs = [rc[q][1] for q in ps]; ss = [sidx[rc[q][0]] for q in ps]
        if max(cs) - min(cs) > 1 or max(ss) - min(ss) > 1:
            continue                              # wraps around the seam column or the profile cycle
        d = (cs[1] - cs[0]) * (ss[2] - ss[0]) - (cs[2] - cs[0]) * (ss[1] - ss[0])
        if d:
            sign = 1 if d > 0 else -1
            break
    if not sign:
        return None
    # rows: Visvalingam on the (radius, height) profile; poles and one row per box extreme locked
    def rh(k):
        L = rows[k]
        return (sum(math.hypot(q[o[0]] - cen[o[0]], q[o[1]] - cen[o[1]]) for q in L) / len(L), L[0][ax])
    ring = [k for k in prof if k not in poles]
    lock = set(poles)
    for key in (lambda k: rh(k)[0], lambda k: -rh(k)[0], lambda k: rh(k)[1], lambda k: -rh(k)[1]):
        lock.add(min(ring, key=lambda k: (round(key(k), 9), sidx[k])))
    keep = list(prof)
    while sum(1 for k in keep if k not in poles) > spec['rows']:
        best = None
        for i, k in enumerate(keep):
            if k in lock or (not cyc and i in (0, len(keep) - 1)):
                continue
            a, b = keep[i - 1], keep[(i + 1) % len(keep)]
            (r0, h0), (r1, h1), (r2, h2) = rh(a), rh(k), rh(b)
            area = abs((r1 - r0) * (h2 - h0) - (r2 - r0) * (h1 - h0))
            if best is None or area < best[0]:
                best = (area, i)
        if best is None:
            return None
        keep.pop(best[1])
    # columns: every k-th if that keeps the four box extremes, else the least outline change (extremes locked)
    ext = set()
    for kk in (o[0], o[1]):
        for sgn in (1, -1):
            m = max(sgn * q[kk] for q in P)
            ext.add(min(rc[q][1] for q in P if rc[q][1] is not None and abs(sgn * q[kk] - m) <= BOX_TOL))
    n = spec['cols']
    near = sorted({int(math.floor(j * C / float(n) + 0.5)) for j in range(n)})
    if C % n == 0 and all(e % (C // n) == 0 for e in ext):
        cols = list(range(0, C, C // n))
    elif spec['kind'] == 'revolve' and len(near) == n and ext <= set(near):
        cols = near                               # as even as C allows (steps differ by one column)
    else:
        cols = list(range(C))
        while len(cols) > n:
            best = None
            for i, j in enumerate(cols):
                if j in ext:
                    continue
                a, b = cols[i - 1], cols[(i + 1) % len(cols)]
                area = 0.0
                for k in ring:
                    L = rows[k]
                    (x0, y0), (x1, y1), (x2, y2) = ((L[c][o[0]], L[c][o[1]]) for c in (a, j, b))
                    area += abs((x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0))
                if best is None or area < best[0]:
                    best = (area, i)
            cols.pop(best[1])
    cor = {}
    for f in faces:
        for q, c in zip(pos_of(M, f), f):
            cor.setdefault(q, set()).add(c)
    out = []
    rp = list(zip(keep, keep[1:] + keep[:1])) if cyc else list(zip(keep, keep[1:]))
    for ra, rb in rp:
        for ca, cb in zip(cols, cols[1:] + cols[:1]):
            def at(k, c):
                return rows[k][0] if k in poles else rows[k][c]
            A, B, Cq, D = at(ra, ca), at(ra, cb), at(rb, cb), at(rb, ca)
            for t in ((A, B, Cq), (A, Cq, D)):
                if len(set(t)) < 3:
                    continue
                cu = [0 if q in (A, D) else 1 for q in t]
                su = [0 if q in (A, B) else 1 for q in t]
                d = (cu[1] - cu[0]) * (su[2] - su[0]) - (cu[2] - cu[0]) * (su[1] - su[0])
                if (d > 0) != (sign > 0):
                    t = (t[0], t[2], t[1])
                nn = unit(cross(sub(t[1], t[0]), sub(t[2], t[0])))
                fc = []
                for q in t:
                    cs = sorted(cor[q])
                    if faceted:
                        c = cs[0]; fc.append((c[0], c[1], FACET))
                    else:
                        fc.append(max(cs, key=lambda c: (dot(unit(M['N'][c[2]][1]), nn) if c[2] >= 0 else 0, c)))
                out.append(tuple(fc))
    meta = dict(kind=spec['kind'], cols=len(cols), of_cols=C, rows=sum(1 for k in keep if k not in poles),
                of_rows=len(ring), poles=len(poles), tris=len(out))
    return out, meta


def is_faceted(M, faces):
    """every corner normal of every face is that face's own normal (a flat-shaded part)"""
    for f in faces:
        a, b, c = pos_of(M, f)
        n = unit(cross(sub(b, a), sub(c, a)))
        for x in f:
            if x[2] < 0 or dot(unit(M['N'][x[2]][1]), n) < FLAT_COS:
                return False
    return True


class Mesh:
    """The decimated groups of one model, split into units (a group, or one part of a 'parts' group) and per
    (unit, v, vt, vn) corner, with the half-edge collapse state. A unit's vt is left out of the corner when its material
    is untextured, its vn when its part is faceted (the written vn is then the triangle's facet normal)."""

    def __init__(self, M, units, locked_pos):
        V, T, N = M['V'], M['T'], M['N']
        cid, wid, near = {}, {}, {}
        self.corner, self.cw, self.cn, self.cuv, self.pos = [], [], [], [], []  # corner: (unit, vi, ti, ni), vertex, normal, uv
        self.tris, self.tc, self.ts = [], [], []                                # triangle: vertices, corners, unit
        for s, un in enumerate(units):
            for f, fac in zip(un['faces'], un['facet']):
                cs = []
                for vi, ti, ni in f:
                    key = (s, V[vi][1], T[ti][1] if ti >= 0 and not un['uvfree'] else None,
                           N[ni][1] if ni >= 0 and not fac else None)
                    c = cid.get(key)
                    if c is None and key[3] is not None:   # a normal within 5 degrees of one already there: same corner
                        nk = unit(key[3])
                        for c2 in near.get(key[:3], []):
                            if dot(self.cn[c2], nk) >= WELD_COS:
                                c = cid[key] = c2
                                break
                    if c is None:
                        c = cid[key] = len(self.corner)
                        self.corner.append((s, vi, ti, FACET if fac else ni)); self.cuv.append(key[2])
                        self.cn.append(unit(key[3]) if key[3] is not None else None)
                        near.setdefault(key[:3], []).append(c)
                        w = wid.get(key[1][:3])
                        if w is None:
                            w = wid[key[1][:3]] = len(self.pos); self.pos.append(key[1][:3])
                        self.cw.append(w)
                    cs.append(c)
                self.tris.append([self.cw[c] for c in cs]); self.tc.append(cs); self.ts.append(s)
        n = len(self.pos)
        sids = range(len(units))
        self.vt = [set() for _ in range(n)]
        self.mem = [{} for _ in range(n)]          # live triangles per unit at each vertex
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
        # unit-box guards: ext_of[w] = (unit, box face) held by w; ext_n = vertices still holding each one. A unit is
        # a group, or one stone or pearl of a 'parts' group (g). A vertex never joins a unit it was not in (corners map
        # within a unit), so holders only get fewer.
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
        self.hulled = 0
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
        if self.cn[a] is not None and self.cn[b] is not None:
            d += sum((x - y) ** 2 for x, y in zip(self.cn[a], self.cn[b]))
        if self.cuv[a] is not None and self.cuv[b] is not None:
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
                return None                          # that unit has reached its share
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
            return None                              # would remove the last vertex holding a face of a unit box
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
        for cu, cv in mapc.items():
            if self.cn[cu] is not None and self.cn[cv] is not None:
                cost += ATTR_L2 * area.get(cu, 0.0) * sum((x - y) ** 2 for x, y in zip(self.cn[cu], self.cn[cv]))
        if need:                                     # relax: nearest corner of v in the same unit
            vc = sorted({self.corner_at(t, v) for t in self.vt[v]})
            for c in need:
                cand = [(self.adist(c, c2), c2) for c2 in vc if self.corner[c2][0] == self.corner[c][0] and
                        (self.cuv[c] is None or sum((x - y) ** 2 for x, y in zip(self.cuv[c], self.cuv[c2])) <= self.uv_max ** 2)]
                if not cand:
                    return None
                d, c2 = min(cand)
                mapc[c] = c2
                cost += ATTR_L2 * area[c] * d
        for t in others:                             # winding guard: no smooth triangle may face against its normals
            ns = [self.cn[mapc.get(c, c)] for c in self.tc[t]]
            if any(x is None for x in ns):
                continue                             # faceted: its vn is written from the geometry
            w = self.tris[t]; i = w.index(u)
            a, b = w[(i + 1) % 3], w[(i + 2) % 3]
            n1 = cross(sub(self.pos[a], pv), sub(self.pos[b], pv))
            if dot(n1, tuple(ns[0][k] + ns[1][k] + ns[2][k] for k in range(3))) <= 0:
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
        """collapse greedily until every unit is at its share (or nothing is left to collapse). relax 'early': relaxed
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

    def hull_part(self, s, goal):
        """(g) a convex part (a pearl) that stalls above its share: rebuilt as the convex hull of its vertices that hold
        a face of its box, if that is no more than the share, every vertex has one corner and no hull triangle faces
        against its corner normals. Half-edge collapses cannot flip an edge; this does. -> True if rebuilt"""
        live = [t for t in range(len(self.tris)) if self.alive[t] and self.ts[t] == s]
        cor = {}
        for t in live:
            for w, c in zip(self.tris[t], self.tc[t]):
                cor.setdefault(w, set()).add(c)
        hold = sorted(w for w in cor if any(k[0] == s for k in self.ext_of[w]))
        H = convex_hull([self.pos[w] for w in hold])
        if H is None or len(H) > goal or any(len(cor[w]) != 1 for w in hold):
            return False
        new = []
        for a, b, c in H:
            w = [hold[a], hold[b], hold[c]]; cs = [min(cor[x]) for x in w]
            n1 = cross(sub(self.pos[w[1]], self.pos[w[0]]), sub(self.pos[w[2]], self.pos[w[0]]))
            if norm(n1) / 2 < MIN_AREA:
                return False
            if all(self.cn[x] is not None for x in cs) and \
                    dot(n1, tuple(sum(self.cn[x][k] for x in cs) for k in range(3))) <= 0:
                return False
            new.append((w, cs, unit(n1)))
        for t in live:
            self.alive[t] = False
            for x in self.tris[t]:
                self.vt[x].discard(t)
        for w, cs, nn in new:
            t = len(self.tris)
            self.tris.append(w); self.tc.append(cs); self.ts.append(s); self.alive.append(True); self.fn0.append(nn)
            for x in w:
                self.vt[x].add(t)
        self.nseg[s] = len(new)
        self.hulled += 1
        return True

    def faces(self, s):
        return [tuple(self.corner[c][1:] for c in self.tc[t]) for t in range(len(self.tris)) if self.alive[t] and self.ts[t] == s]


def selects(sel, seg):
    return sel == 'mtl:' + str(seg['mtl']) or sel == seg['g']


def part_shares(total, sizes, floor):
    """(g) a group's target split over its parts: proportional, even (closed parts), at least floor, at most the part"""
    n = sum(sizes)
    return [min(m, max(floor, 2 * int(round(total * m / n / 2.0)))) for m in sizes]


def build(M, cfg, name, textured):
    """-> (list of (seg, faces, mode, goal, info) in original order, relaxed collapse count); mode copy / fixed / share.
    info: dropped zero-area triangles, parts (component sizes before / after) and faceted part count."""
    segs, target = M['segs'], cfg['target']
    keep, fixed, parts = cfg.get('keep', []), cfg.get('fixed', {}), cfg.get('parts', [])
    for sel in keep + list(fixed) + list(parts):
        if not any(selects(sel, s) for s in segs):
            sys.exit('%s: keep/fixed/parts selector %r matches no group' % (name, sel))
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
    for sel in cfg.get('near_hard', []):
        # the vertices next to a hard edge are kept, so the edge's shading stays as narrow as in LOD0 (the closed ring
        # box's lid seam: its normals tilt into the seam, and without the next row the groove fades over a whole side)
        for x in segs:
            if not selects(sel, x):
                continue
            ncor, nb = {}, {}
            for f in x['faces']:
                ps = pos_of(M, f)
                for q, c in zip(ps, f):
                    ncor.setdefault(q, set()).add(M['N'][c[2]][1] if c[2] >= 0 else None)
                    nb.setdefault(q, set()).update(ps)
            for q in ncor:
                if len(ncor[q]) > 1:
                    locked_pos |= nb[q]
    n_rest = 0
    for gp, gs in cfg.get('rest', {}).items():
        # the paper under every stone stays exact: stones rest on it as in LOD0 (VERIFY MAX_SINK)
        rp = rest_positions(M, [f for x in segs if selects(gp, x) for f in x['faces']],
                            [f for x in segs if selects(gs, x) for f in x['faces']])
        n_rest += len(rp); locked_pos |= rp
    sids = [i for i in mode if mode[i] != 'copy']
    free = [i for i in sids if mode[i] == 'share']
    fixed_n = sum(len(segs[i]['faces']) for i in mode if mode[i] == 'copy') + sum(goal[i] for i in sids if mode[i] == 'fixed')
    nfree = sum(len(segs[i]['faces']) for i in free)
    for i in free:
        goal[i] = min(len(segs[i]['faces']), max(MIN_SHARE, int(round((target - fixed_n) * len(segs[i]['faces']) / nfree))))
    goal0 = dict(goal)                       # the configured target (fixed) or share, before grid parts are taken out
    # units: a decimated group, or each part of a 'parts' group (g); zero-area triangles dropped; faceted parts flagged
    units, uof, info, gridf, gridn, gridm = [], {}, {}, {}, {}, {}
    for i in sids:
        s = segs[i]
        faces = [f for f in s['faces'] if len(set(pos_of(M, f))) == 3 and
                 norm(cross(sub(pos_of(M, f)[1], pos_of(M, f)[0]), sub(pos_of(M, f)[2], pos_of(M, f)[0]))) > 0]
        n_zero = len(s['faces']) - len(faces)
        if any(selects(k, s) for k in cfg.get('drop_internal', [])):
            # coincident triangle pairs with opposite winding (two solids touching face to face, e.g. the closed ring
            # box's base top and lid bottom): both lie inside the model, so both are dropped
            seen = {}
            for j, f in enumerate(faces):
                seen.setdefault(frozenset(pos_of(M, f)), []).append(j)
            gone = set()
            for js in seen.values():
                if len(js) == 2:
                    a, b = (pos_of(M, faces[j]) for j in js)
                    if dot(cross(sub(a[1], a[0]), sub(a[2], a[0])), cross(sub(b[1], b[0]), sub(b[2], b[0]))) < 0:
                        gone.update(js)
            faces = [f for j, f in enumerate(faces) if j not in gone]
        comps = components([pos_of(M, f) for f in faces])
        info_c = {}
        if s['g'] in cfg.get('cull', {}):
            # keep this many parts, evenly spaced in file order (the necklace's knots run along the strand)
            k = cfg['cull'][s['g']]
            keepi = sorted({int((j + 0.5) * len(comps) / k) for j in range(k)})
            info_c['culled'] = len(comps) - len(keepi)
            faces = [faces[j] for i2 in keepi for j in comps[i2]]
            comps = components([pos_of(M, f) for f in faces])
        if any(selects(k, s) for k in cfg.get('close', [])):
            # (h) parts whose open ends are hidden inside other parts (the knots' ends in the pearls' drill holes)
            # are capped first, so they are closed and can go below the 6 triangles of an open tube
            capped = []
            for cp in comps:
                capped += [faces[j] for j in cp] + cap_loops(M, [faces[j] for j in cp])
            info_c['cap_tris'] = len(capped) - len(faces)
            faces = capped
            comps = components([pos_of(M, f) for f in faces])
        flat = [is_faceted(M, [faces[j] for j in cp]) for cp in comps]
        uvfree = not textured(s['mtl'])
        info[i] = dict(dropped=n_zero, faceted_parts=sum(flat), parts=len(comps), uvfree=uvfree,
                       internal=len(s['faces']) - n_zero - len(faces) + info_c.get('cap_tris', 0), **info_c)
        if 'culled' in info_c:
            info[i]['internal'] = 0
        specs = [sp for k, sps in cfg.get('grid', {}).items() if selects(k, s) for sp in sps]
        if specs:                            # (i) grid parts are rebuilt here and leave the decimation
            rest = []
            for cp, fl in zip(comps, flat):
                cf = [faces[j] for j in cp]
                for sp in specs:
                    g2 = grid_part(M, cf, sp, fl)
                    if g2 is not None:
                        gridf.setdefault(i, []).extend(g2[0]); gridn[i] = gridn.get(i, 0) + 1
                        gridm.setdefault(i, []).append(g2[1])
                        break
                else:
                    rest.append(cp)
            if i in gridf:
                faces = [faces[j] for cp in rest for j in cp]
                flat = [fl for cp, fl in zip(comps, flat) if cp in rest]
                comps = components([pos_of(M, f) for f in faces])
        if i in gridf:
            info[i]['grid_parts'] = gridn[i]
            info[i]['grid'] = gridm[i]
            goal[i] = max(0, goal[i] - len(gridf[i]))
            if not faces:
                uof[i] = []
                continue
        if any(selects(k, s) for k in parts):
            uof[i] = []
            for cp, fl in zip(comps, flat):
                uof[i].append(len(units))
                units.append(dict(seg=i, faces=[faces[j] for j in cp], facet=[fl] * len(cp), uvfree=uvfree))
        else:
            fl_of = {j: fl for cp, fl in zip(comps, flat) for j in cp}
            uof[i] = [len(units)]
            units.append(dict(seg=i, faces=faces, facet=[fl_of[j] for j in range(len(faces))], uvfree=uvfree))
    ugoal = {}
    for i in sids:
        us = uof[i]
        if not us:
            continue
        if len(us) == 1 and not any(selects(k, segs[i]) for k in parts):
            ugoal[us[0]] = goal[i]
        else:
            for u, g in zip(us, part_shares(goal[i], [len(units[u]['faces']) for u in us], PART_FLOOR)):
                ugoal[u] = g
    mesh = Mesh(M, units, locked_pos) if units else None
    if mesh:
        mesh.uv_max = cfg.get('uv_max', UV_MAX)
    copy_n = sum(len(segs[i]['faces']) for i in mode if mode[i] == 'copy')
    for _ in range(8):
        if mesh is None:
            break
        mesh.run(ugoal, cfg.get('relax'))
        again = False
        for i in sids:                       # (g) parts that stall above their share: the group's others take the excess
            us = uof[i]
            if len(us) < 2:
                continue
            over = sum(mesh.nseg[u] for u in us) - goal[i]
            adj = [u for u in us if mesh.nseg[u] <= ugoal[u] and ugoal[u] > PART_FLOOR]
            if over > 0 and adj:
                tot = sum(ugoal[u] for u in adj)
                for u in adj:
                    ugoal[u] = max(PART_FLOOR, ugoal[u] - 2 * int(math.ceil(over * ugoal[u] / tot / 2.0)))
                again = True
        over = copy_n + sum(mesh.nseg.values()) - target    # groups that stall above their share: the others take it
        adj = [i for i in free if mesh.nseg[uof[i][0]] <= ugoal[uof[i][0]] and ugoal[uof[i][0]] > MIN_SHARE]
        if over > 0 and adj:
            tot = sum(ugoal[uof[i][0]] for i in adj)
            for i in adj:
                u = uof[i][0]
                ugoal[u] = max(MIN_SHARE, ugoal[u] - int(math.ceil(over * ugoal[u] / tot)))
            again = True
        if not again:
            break
    for i in sids:                           # (g) stalled convex parts: the hull of their box vertices
        if len(uof[i]) > 1:
            for u in uof[i]:
                if mesh.nseg[u] > ugoal[u]:
                    mesh.hull_part(u, ugoal[u])
    out = []
    for i, s in enumerate(segs):
        if mode[i] == 'copy':
            out.append((s, list(s['faces']), 'copy', len(s['faces']), dict(dropped=0)))
        else:
            fs = gridf.get(i, []) + [f for u in uof[i] for f in mesh.faces(u)]
            inf = dict(info[i])
            if n_rest and any(selects(gp, s) for gp in cfg.get('rest', {})):
                inf['rest_locked_vertices'] = n_rest
            if len(uof[i]) > 1:
                inf['part_tris'] = [mesh.nseg[u] for u in uof[i]]
            out.append((s, fs, mode[i], goal0[i] if mode[i] == 'fixed' else goal[i] + len(gridf.get(i, [])), inf))
    return out, (mesh.relaxed_collapses if mesh else 0), (mesh.hulled if mesh else 0)


def facet_text(M, f):
    """the facet normal of a written triangle, as a vn text with the source's 4 decimals"""
    a, b, c = pos_of(M, f)
    n = unit(cross(sub(b, a), sub(c, a)))
    return ' '.join(('%.4f' % x).replace('-0.0000', '0.0000') for x in n)


def write(M, groups, name, level, target, mtl_lines):
    V, T, N = M['V'], M['T'], M['N']
    ntri = sum(len(f) for _, f, _, _, _ in groups)
    L = list(M['lead'])
    L += ['# %s_%s - LOD variant of %s.obj (target %d tris): %d tris, made by Source~/make_lods.py'
          % (name, level, name, target, ntri),
          '# Every vertex is an original v/vt of %s.obj (smooth parts: original v/vt/vn; faceted parts: their facet '
          'normals); materials come from the original MTL (shared, not copied).' % name]
    L += mtl_lines
    base = [0, 0, 0]
    for s, faces, _, _, _ in groups:
        L += s['head']
        used = [sorted({c[k] for f in faces for c in f if c[k] >= 0}) for k in range(3)]
        idx = [{o: base[k] + j + 1 for j, o in enumerate(used[k])} for k in range(3)]
        fvn = {}                                     # facet normal text -> new vn index
        ftx = [facet_text(M, f) if f[0][2] == FACET else None for f in faces]
        for t in ftx:
            if t is not None and t not in fvn:
                fvn[t] = base[2] + len(used[2]) + len(fvn) + 1
        for k, tag, src in ((0, 'v', V), (1, 'vt', T), (2, 'vn', N)):
            L += ['%s %s' % (tag, src[o][0]) for o in used[k]]
            if k == 2:
                L += ['vn ' + t for t in fvn]
            base[k] += len(used[k]) + (len(fvn) if k == 2 else 0)
        for f, ft in zip(faces, ftx):
            cs = []
            for vi, ti, ni in f:
                a = str(idx[0][vi])
                nn = fvn[ft] if ni == FACET else idx[2][ni] if ni >= 0 else None
                if ti >= 0 and nn is not None:
                    a += '/%d/%d' % (idx[1][ti], nn)
                elif ti >= 0:
                    a += '/%d' % idx[1][ti]
                elif nn is not None:
                    a += '//%d' % nn
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
    order = []
    for line in text.split('\n'):
        if line.startswith(('g ', 'usemtl ')):
            order.append(line.strip())
    return dict(V=V, T=T, N=N, F=F, mtls=mtls, bad=bad, npoly=npoly, order=order)


def corner_vals(R, c):
    return (R['V'][c[0]], R['T'][c[1]] if c[1] >= 0 else None, R['N'][c[2]] if c[2] >= 0 else None)


def tri_pos(R, cs):
    return tuple(R['V'][x[0]][:3] for x in cs)


def facing_away(R):
    """triangles wound against the sum of their vertex normals (back faces where the shading says front)"""
    n = 0
    for _, _, cs in R['F']:
        if all(x[2] >= 0 for x in cs):
            a, b, c = tri_pos(R, cs)
            if dot(cross(sub(b, a), sub(c, a)), [sum(R['N'][x[2]][k] for x in cs) for k in range(3)]) < 0:
                n += 1
    return n


def open_edges(tris):
    """position-welded open edges of a list of position triples (a crack between parts or groups adds some)"""
    key, E = {}, {}
    for t in tris:
        w = [key.setdefault(p, len(key)) for p in t]
        for i in range(3):
            e = (min(w[i], w[(i + 1) % 3]), max(w[i], w[(i + 1) % 3]))
            E[e] = E.get(e, 0) + 1
    return sum(1 for n in E.values() if n == 1)


def box(pts):
    return [[min(p[k] for p in pts) for k in range(3)], [max(p[k] for p in pts) for k in range(3)]]


def solve3(A, b):
    """3x3 linear solve (Cramer)"""
    def det(m):
        return (m[0][0] * (m[1][1] * m[2][2] - m[1][2] * m[2][1]) - m[0][1] * (m[1][0] * m[2][2] - m[1][2] * m[2][0])
                + m[0][2] * (m[1][0] * m[2][1] - m[1][1] * m[2][0]))
    d = det(A)
    out = []
    for j in range(3):
        m = [[b[i] if k == j else A[i][k] for k in range(3)] for i in range(3)]
        out.append(det(m) / d)
    return out


def hole_centre(pts, axis):
    """centre of a ring's hole in the plane across its finger axis: a least-squares circle (Kasa fit) through the
    vertices of the innermost 0.15 mm of the inner surface -> ((a, b) in the plane's two axes, fitted radius, n points)"""
    ax = [k for k in range(3) if k != axis]
    P = [(p[ax[0]], p[ax[1]]) for p in pts]
    lo = [min(p[k] for p in P) for k in range(2)]; hi = [max(p[k] for p in P) for k in range(2)]
    c0 = ((lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2)
    r = [math.hypot(p[0] - c0[0], p[1] - c0[1]) for p in P]
    rmin = min(r)
    Q = [(p[0] - c0[0], p[1] - c0[1]) for p, rr in zip(P, r) if rr < rmin + 0.00015]
    A = [[0.0] * 3 for _ in range(3)]; b = [0.0] * 3
    for x, y in Q:
        row = (x, y, 1.0); rhs = -(x * x + y * y)
        for i in range(3):
            b[i] += row[i] * rhs
            for k in range(3):
                A[i][k] += row[i] * row[k]
    D, E, Fc = solve3(A, b)
    cx, cy = -D / 2, -E / 2
    return (c0[0] + cx, c0[1] + cy), math.sqrt(max(cx * cx + cy * cy - Fc, 0.0)), len(Q)


def paper_sink(stones, paper):
    """largest depth (m) of a stone vertex or triangle centre under the paper's upward-facing sheet (negative: above)"""
    up = [t for t in paper if cross(sub(t[1], t[0]), sub(t[2], t[0]))[1] > 0]
    bx = [(min(p[0] for p in t), max(p[0] for p in t), min(p[2] for p in t), max(p[2] for p in t)) for t in up]
    worst = -1.0
    for st in stones:
        pts = {p for t in st for p in t} | {tuple(sum(p[k] for p in t) / 3 for k in range(3)) for t in st}
        ylow = min(p[1] for p in pts)
        for x, y, z in sorted(pts):
            if y > ylow + 0.0006:
                continue                     # only the stone's lowest 0.6 mm can reach the paper
            for t, (x0, x1, z0, z1) in zip(up, bx):
                if not (x0 <= x <= x1 and z0 <= z <= z1):
                    continue
                (ax, ay, az), (bx_, by, bz), (cx, cy, cz) = t
                den = (bz - cz) * (ax - cx) + (cx - bx_) * (az - cz)
                if den == 0:
                    continue
                w0 = ((bz - cz) * (x - cx) + (cx - bx_) * (z - cz)) / den
                w1 = ((cz - az) * (x - cx) + (ax - cx) * (z - cz)) / den
                w2 = 1 - w0 - w1
                if min(w0, w1, w2) >= -1e-9:
                    worst = max(worst, w0 * ay + w1 * by + w2 * cy - y)
    return worst


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


def group_tris(R, groups):
    """the written triangles of each group, in the order of build()'s list"""
    out, pos = [], 0
    for s, faces, _, _, _ in groups:
        out.append(R['F'][pos:pos + len(faces)]); pos += len(faces)
    return out


def verify(trimesh, name, level, text, src_text, src_R, groups, cfg, pack, mtl_lines):
    fails, meas = [], {}
    R = raw_obj(text)
    target = cfg['target']
    n = len(R['F'])
    meas['tris'] = n
    if abs(n - target) > 0.10 * target:
        fails.append('triangles %d not within 10 %% of %d' % (n, target))
    for s, faces, mode, goal, _ in groups:
        if mode == 'fixed' and abs(len(faces) - goal) > 0.10 * goal:
            fails.append('group %s: %d triangles not within 10 %% of its target %d' % (s['g'], len(faces), goal))
    if R['bad'] or R['npoly']:
        fails.append('invalid v/vt/vn index or face: %s' % (R['bad'] or ['%d faces are not triangles' % R['npoly']])[0])
        return fails, meas
    sc, ss = tm_scene(trimesh, text, pack), tm_scene(trimesh, src_text, pack)
    tm_n = sum(len(g.faces) for g in sc.geometry.values())
    if tm_n != n:
        fails.append('trimesh reads %d triangles, file has %d' % (tm_n, n))
    db = float(abs(sc.bounds - ss.bounds).max())
    meas['bounds_max_diff_m'] = db
    meas['bounds'] = [[round(x, 5) for x in r] for r in sc.bounds.tolist()]
    if db > BOUNDS_TOL:
        fails.append('bounds differ by %.5f m (> %.4f m)' % (db, BOUNDS_TOL))
    meas['group_box_max_diff_m'] = max(float(abs(sc.geometry[k].bounds - ss.geometry[k].bounds).max()) for k in ss.geometry)
    ymin = min(R['V'][c[0]][1] for _, _, cs in R['F'] for c in cs)
    meas['min_y'] = ymin
    if ymin < -FLOOR_TOL:
        fails.append('lowest vertex at y = %.6f m (below 0)' % ymin)
    if R['mtls'] != src_R['mtls']:
        fails.append('usemtl set %s != source %s' % (sorted(R['mtls']), sorted(src_R['mtls'])))
    if R['order'] != src_R['order']:
        fails.append('g/usemtl order differs from the source')
    if (len(R['N']) > 0) != (len(src_R['N']) > 0) or (len(R['T']) > 0) != (len(src_R['T']) > 0):
        fails.append('vt/vn presence differs from the source')
    if [l for l in text.split('\n') if l.startswith('mtllib')] != mtl_lines:
        fails.append('mtllib is not the original MTL')
    for l in mtl_lines:
        if not os.path.isfile(os.path.join(pack, l.split(None, 1)[1])):
            fails.append('original MTL %s missing' % l)
    zero = 0
    for _, _, cs in R['F']:
        a, b, c = tri_pos(R, cs)
        if norm(cross(sub(b, a), sub(c, a))) / 2 < 1e-10:
            zero += 1
    meas['zero_area'] = zero
    if zero:
        fails.append('%d zero-area triangles' % zero)
    # corners: smooth parts an original (v, vt, vn); faceted parts an original (v, vt) with their facet normal as vn
    orig = {corner_vals(src_R, c) for _, _, cs in src_R['F'] for c in cs}
    orig_vt = {x[:2] for x in orig}
    alien = facet_bad = nfacet = 0
    GT, SG = group_tris(R, groups), {}
    for g, m, cs in src_R['F']:
        SG.setdefault((g, m), []).append(cs)
    for (s, faces, mode, goal, inf), wt in zip(groups, GT):
        for f, (_, _, cs) in zip(faces, wt):
            vals = [corner_vals(R, c) for c in cs]
            if f[0][2] == FACET:
                nfacet += 1
                a, b, c = tri_pos(R, cs)
                nn = unit(cross(sub(b, a), sub(c, a)))
                alien += sum(1 for x in vals if x[:2] not in orig_vt)
                facet_bad += sum(1 for x in vals if x[2] is None or norm(sub(x[2], nn)) > FACET_TOL)
            else:
                alien += sum(1 for x in vals if x not in orig)
        if mode == 'copy' and [tuple(corner_vals(R, c) for c in cs) for _, _, cs in wt] != \
                [tuple(corner_vals(src_R, c) for c in cs) for cs in SG.get((s['g'], s['mtl']), [])]:
            fails.append('group %s should be unchanged' % s['g'])
    meas['faceted_tris'] = nfacet
    if alien:
        fails.append('%d corners are not an original (v, vt[, vn])' % alien)
    if facet_bad:
        fails.append('%d faceted corners do not carry their facet normal' % facet_bad)
    # components per group (12 stones, 58 pearls ...); every part of a 'parts' group closed, with its exact box
    meas['components'] = {}
    for (s, faces, mode, goal, inf), wt in zip(groups, GT):
        src_t = [tri_pos(src_R, cs) for cs in SG[(s['g'], s['mtl'])]]
        src_t = [t for t in src_t if len(set(t)) == 3]
        lod_t = [tri_pos(R, cs) for _, _, cs in wt]
        cs0, cs1 = components(src_t), components(lod_t)
        meas['components'][s['g']] = [len(cs0), len(cs1)]
        meas.setdefault('part_tris', {})[s['g']] = [min(len(p) for p in cs1), max(len(p) for p in cs1)] if cs1 else [0, 0]
        if len(cs0) - inf.get('culled', 0) != len(cs1):
            fails.append('group %s: %d connected parts, source %d' % (s['g'], len(cs1), len(cs0)))
            continue
        if not any(selects(k, s) for k in cfg.get('parts', [])):
            continue
        sizes = [len(p) for p in cs1]
        floor = cfg.get('min_part', {}).get(s['g'], PART_FLOOR)
        oe = [open_edges([lod_t[j] for j in p]) for p in cs1]
        bd = max(max(abs(x - y) for r0, r1 in zip(box([q for j in p0 for q in src_t[j]]), box([q for j in p1 for q in lod_t[j]]))
                     for x, y in zip(r0, r1)) for p0, p1 in zip(cs0, cs1))
        meas['parts ' + s['g']] = dict(n=len(cs1), min_tris=min(sizes), max_tris=max(sizes), open_edges=sum(oe), box_max_diff_m=bd)
        if min(sizes) < floor:
            fails.append('group %s: a part has %d triangles (< %d)' % (s['g'], min(sizes), floor))
        if sum(oe):
            fails.append('group %s: %d open edges inside its parts (a stone or pearl not closed)' % (s['g'], sum(oe)))
        if bd > 1e-6:
            fails.append('group %s: a part box moved by %.6f m' % (s['g'], bd))
    chk = CHECKS.get(name, {})
    if 'hole' in chk:
        ax = chk['hole']['axis']
        (c1, r1, n1) = hole_centre(sorted({R['V'][c[0]][:3] for _, _, cs in R['F'] for c in cs}), ax)
        (c0, r0, n0) = hole_centre(sorted({src_R['V'][c[0]][:3] for _, _, cs in src_R['F'] for c in cs}), ax)
        d = math.hypot(c1[0] - c0[0], c1[1] - c0[1])
        meas['hole'] = dict(centre=[round(x, 6) for x in c1], source_centre=[round(x, 6) for x in c0], diff_m=d,
                            radius_m=[round(r0, 6), round(r1, 6)], points=[n0, n1])
        if d > HOLE_TOL or abs(c0[0] - chk['hole']['y']) > HOLE_TOL:
            fails.append('hole centre (%.5f, %.5f) is %.5f m from the source (%.5f, %.5f)' % (c1[0], c1[1], d, c0[0], c0[1]))
    if 'stones' in chk:
        gs, gp = chk['stones']
        st = {}
        for key, RR in (('src', src_R), ('lod', R)):
            tri = {}
            for g, m, cs in RR['F']:
                tri.setdefault(g, []).append(tri_pos(RR, cs))
            stones = [[tri[gs][j] for j in p] for p in components(tri[gs])]
            st[key] = paper_sink(stones, tri[gp])
        meas['stone_sink_m'] = [round(st['src'], 7), round(st['lod'], 7)]
        if st['lod'] > MAX_SINK:
            fails.append('a stone sinks %.5f m into the decimated paper (> %.4f m)' % (st['lod'], MAX_SINK))
    meas['facing_away'] = [facing_away(src_R), facing_away(R)]
    if meas['facing_away'][1] > meas['facing_away'][0]:
        fails.append('%d triangles face against their vertex normals (source %d)' % tuple(meas['facing_away'][::-1]))
    oe, oe0 = open_edges([tri_pos(R, cs) for _, _, cs in R['F']]), open_edges([tri_pos(src_R, cs) for _, _, cs in src_R['F']])
    meas['open_edges'] = [oe0, oe]
    if oe > oe0:
        fails.append('open edges %d > source %d (crack)' % (oe, oe0))
    return fails, meas


def textured_fn(pack, name):
    """material name -> True if its MTL block (of this model) or materials.json names a texture map"""
    maps = set()
    cur = None
    for line in open(os.path.join(pack, name + '.mtl'), encoding='utf-8'):
        p = line.split()
        if p and p[0] == 'newmtl':
            cur = p[1]
        elif p and p[0].startswith('map_') and cur:
            maps.add(cur)
    mj = os.path.join(pack, 'materials.json')
    if os.path.isfile(mj):
        for k, v in json.load(open(mj, encoding='utf-8')).get('materials', {}).items():
            if any(x.endswith('_map') for x in v):
                maps.add(k)
    return lambda m: m in maps


def fmt(n):
    return '{:,}'.format(n)


def dist(size, h, fov):
    """distance (m) at which a model of this size fills h of the screen height (Unity LODGroup, LOD Bias 1)"""
    return size / (2 * h * math.tan(math.radians(fov) / 2))


def how(name, level, r):
    """one line on how a LOD was made, from its report"""
    out = []
    for d in r['groups']:
        g = '%s %s -> %s' % (d['g'], fmt(d['src']), fmt(d['tris']))
        if d['mode'] == 'copy':
            out.append('%s unchanged' % d['g']); continue
        bits, seen = [], []
        for m in d.get('grid', []):
            t = '%s grid %d of %d columns x %d of %d rows%s' % (m['kind'], m['cols'], m['of_cols'], m['rows'], m['of_rows'],
                                                             ' + %d pole%s' % (m['poles'], 's' * (m['poles'] > 1)) if m['poles'] else '')
            seen.append(t)
        for t in sorted(set(seen), key=seen.index):
            bits.append(('%d x ' % seen.count(t) if seen.count(t) > 1 else '') + t)
        if d.get('part_tris'):
            bits.append('per part, %d parts of %s tris' % (d['parts'], '-'.join(str(x) for x in sorted(set(d['part_tris'])))))
        if d.get('cap_tris'):
            pt = r['verify'].get('part_tris', {}).get(d['g'], [0, 0])
            bits.append('knot ends capped, %d knots of %s tris' % (d['parts'], '-'.join(str(x) for x in sorted(set(pt)))))
        if d.get('culled'):
            bits.append('%d of %d knots kept' % (d['parts'], d['parts'] + d['culled']))
        if d.get('internal') and not d.get('cap_tris'):
            bits.append('%d enclosed coincident tris dropped' % d['internal'])
        if d.get('rest_locked_vertices'):
            bits.append('paper under the stones kept')
        if d.get('faceted_parts'):
            bits.append('flat-shaded')
        if not bits or (len(bits) == 1 and bits[0] == 'flat-shaded') or (d.get('grid_parts', 0) < d.get('parts', 0) and d.get('grid')):
            bits.insert(len(d.get('grid', [])), 'edge collapse')
        out.append('%s (%s)' % (g, ', '.join(bits)))
    return '; '.join(out)


def lods_entry(name, src, r1, r2):
    sw = SWITCH[name]
    lv = [dict(level='LOD0', file=name + '.obj', triangles=src['tris'], screen_height_min=sw[0])]
    for level, r, h in (('LOD1', r1, sw[1]), ('LOD2', r2, sw[2])):
        lv.append(dict(level=level, file='%s_%s.obj' % (name, level), triangles=r['tris'], target=r['target'],
                       bytes=r['bytes'], screen_height_min=h))
    return dict(levels=lv, night_store_top_level='LOD1' if name in TOP_LOD1 else 'LOD0', lodgroup_size_m=round(src['size'], 4),
                note='screen_height_min: suggested Unity LODGroup screenRelativeTransitionHeight (LOD Bias 1), culled below '
                     'the last; unverified in Unity. Every level uses %s.mtl with the same g/usemtl names; made by '
                     'Source~/make_lods.py (README: LOD variants).' % name)


README_INTRO = (' Six models also have LOD1/LOD2 variants (12 more OBJ files, see LOD variants).')
README_REGEN = ('`Source~/make_lods.py` rebuilds the 12 LOD variants from the shipped OBJs and `Source~/render_lods.py` their '
                'contact sheet `Source~/Preview_LODs.jpg`; run make_lods.py after `Source~/make_docs.py`, which writes the '
                'documents without the LOD entries.')
LIC_HEAD = '  LOD variants ('


def readme_section(names, srcs, rep):
    L = ['## LOD variants', '',
         'LOD1 and LOD2 of six models, made %s by `Source~/make_lods.py` from the shipped OBJs (standard library only; from '
         'the repository root: `python3 -I Prototype/ThirdParty/JewelryModels/Source~/make_lods.py '
         'Prototype/ThirdParty/JewelryModels --previews`; deterministic, a rerun gives the same bytes). The full models '
         'stay LOD0; no existing OBJ, MTL or texture was changed. In the night store **LOD1 is the top level** of the four '
         'small items (GoldCoin, RingGoldBand, RingBoxClosed, JewelersLoupe), so their LOD1 was checked closely in '
         '`Source~/Preview_LODs.jpg` (LOD0 | LOD1 | LOD2 of each model, whole and close up; each OBJ read per corner as Unity '
         'imports it, back faces culled as in Unity; render.py\'s trimesh loader merged the LOD1 ring box\'s seam corners). '
         'GoldBar1kg (364 tris) gets none. **Not imported, wired or looked at in Unity yet: unverified.**' % DATE, '',
         '| Model | Level | File | Triangles (target) | Bytes | How | Suggested LODGroup level: shown from (screen height; '
         'about %s / %s distance) |' % (FOV[0][0], FOV[1][0]),
         '|---|---|---|---|---|---|---|']
    total = 0
    for name in names:
        src = srcs[name]; sw = SWITCH[name]
        for k, level in enumerate(('LOD0', 'LOD1', 'LOD2')):
            h = sw[k]
            where = '%s %% (%.2f m / %.2f m)' % (('%g' % (h * 100)), dist(src['size'], h, FOV[0][1]), dist(src['size'], h, FOV[1][1]))
            if level == 'LOD0':
                row = [name, level, '`%s.obj`' % name, fmt(src['tris']), fmt(src['bytes']), 'the shipped model',
                       where + ('; not used in the night store' if name in TOP_LOD1 else '')]
            else:
                r = rep['%s_%s.obj' % (name, level)]
                total += r['bytes']
                row = [name, level, '**`%s_%s.obj`**' % (name, level), '%s (%s)' % (fmt(r['tris']), fmt(r['target'])),
                       fmt(r['bytes']), how(name, level, r),
                       where + (' - **top level in the night store**' if level == 'LOD1' and name in TOP_LOD1 else '')
                       + ('; culled below' if level == 'LOD2' else '')]
            L.append('| ' + ' | '.join(row) + ' |')
    v = {fn: r['verify'] for fn, r in rep.items()}
    hole = [v[fn]['hole']['diff_m'] for fn in sorted(v) if 'hole' in v[fn]]
    sink = [v[fn]['stone_sink_m'] for fn in sorted(v) if 'stone_sink_m' in v[fn]]
    L += ['',
          'Distances: d = size / (2 h tan(fov / 2)) with LOD Bias 1, a %g-degree vertical field of view on %s and %g degrees '
          'on the desktop camera, size = the largest bounds axis. The 12 LOD files add %s bytes; no texture was added.'
          % (FOV[0][1], FOV[0][0], FOV[1][1], fmt(total)), '',
          '- **Shared materials.** Each LOD says `mtllib <Model>.mtl` (the original file, not copied) and keeps the '
          '`g`/`usemtl` names and order of LOD0, so the LOD renderers take LOD0\'s materials and maps (GoldCoin_Color/'
          '_Normal, GemPaper_Color) unchanged.',
          '- **Original vertices, per-corner normals.** Every vertex is an original v/vt of LOD0, written as its original '
          'text. Smooth parts keep their original per-corner vn (gold, pearls, velvet stay smooth; the coin\'s rim chamfers '
          'and the loupe barrel\'s inner lip stay hard edges). Faceted parts (the 12 stones, the gem paper, the loupe\'s '
          'chrome case plates) get one new vn per triangle, its facet normal, so they stay flat-shaded. Import with '
          '*Normals = Import*, as LOD0.',
          '- **Regular grids where LOD0 is one** (band, coin, loupe barrel, lenses, case plates, pivot pin): kept every '
          'k-th column (or the columns nearest an even spacing) and the profile rows that matter most, then re-triangulated. '
          'Plain edge collapse left the LOD1 band visibly lumpy (it picks among equal costs on a regular grid). The '
          'loupe\'s barrel and lenses use the same 12 (LOD2: 8) angles, so each lens rim lies on the barrel wall.',
          '- **Stones and pearls are decimated one by one** (rule g): each gets its share (GemParcel LOD2: 20 per stone, '
          'necklace: 30 per pearl at LOD1, 8 at LOD2) and keeps its exact bounding box and a closed surface; at 8 triangles '
          'a pearl is the octahedron of its six extreme points (two LOD2 pearls are built as that hull directly, because '
          'edge collapse cannot flip the last edge). GemParcel LOD1 keeps all 12 stones byte-for-byte.',
          '- **Silk knots.** The 58 knots of the strand are open tubes whose ends sit in the pearls\' drill holes; each end is '
          'capped (with the loop\'s own vertices), so a knot can go below an open tube\'s 6 triangles: 4-6 each at LOD1. '
          'LOD2\'s 50 triangles cannot hold 58 knots: 12 knots, evenly spaced along the strand, are kept (under half a pixel '
          'at LOD2 distances).',
          '- **Ring box.** The 144 triangles where the closed box\'s base top and lid bottom coincide lie inside the box '
          '(0 of 20,736 test rays from them reach the outside) and are not carried over; the cream lining (wholly inside '
          'the closed box) gets a token 32 / 24 triangles so the visible navy shell takes the rest; the vertices along the '
          'lid seam and the row next to it are kept at LOD1, so the seam\'s shading strip is LOD0\'s; the hinge pin '
          '(Gold18k, 32 tris) is unchanged.',
          '- **Gem paper.** The paper triangles under the lowest 0.6 mm of every stone are kept, so every stone rests on '
          'the decimated paper exactly as on LOD0 (the lowest stone point is %s mm above the paper at LOD1 / LOD2, %s mm '
          'on LOD0). The downward bottom sheet shows through the top sheet only in renderers that draw back faces.'
          % ('/'.join('%.4f' % (-x[1] * 1000) for x in sink), '%.4f' % (-sink[0][0] * 1000) if sink else '-'),
          '- **Verified offline** by make_lods.py (exit 1 and nothing written on failure): triangles within 10 %% of every '
          'target and explicit group target; bounds within 0.2 mm of LOD0 (measured: identical, 0.0 mm, at every level); '
          'nothing below y = 0; RingGoldBand\'s hole centre within 0.1 mm of LOD0\'s (measured %s um); connected parts per '
          'group unchanged (12 stones, 58 pearls; knots 58 at LOD1, 12 at LOD2 by design); every stone and pearl closed with '
          'its exact box; same g/usemtl order, usemtl set and mtllib; valid indices, triangles only, no zero-area triangle; '
          'every corner an original v/vt (smooth parts: v/vt/vn); every faceted vn its facet normal; unchanged groups '
          'identical; no new open edges (the loupe lens rims are open in LOD0 too); no triangle wound against its own '
          'normals (0 at every level, as LOD0); trimesh reads every triangle.'
          % '/'.join('%.2f' % (x * 1e6) for x in hole),
          '- **Noticed in LOD0, not changed here:** RingBoxClosed.obj holds the 144 coincident, enclosed triangles named '
          'above, which may z-fight along the seam in Unity; the lathe parts of GoldCoin, RingBoxClosed and JewelersLoupe '
          'carry 368 zero-area triangles at their axis points (none in the LODs).',
          '- `sources.json` has an entry per LOD file (SHA-256, bytes, triangles, target, derived_from, derivation, groups, '
          'VERIFY measures) and `lod_variants`; `models.json` has a `lods` entry per model with the suggested transitions.',
          '']
    return '\n'.join(L)


def strip_mine(readme, lic):
    """the README and LICENSE text without this script's section and lines"""
    r = readme.replace(README_INTRO, '').replace('\n' + README_REGEN, '')
    if '\n## LOD variants\n' in r:
        a = r.index('\n## LOD variants\n')
        b = r.find('\n## ', a + 5)
        r = r[:a] + (r[b:] if b >= 0 else '\n')
    out, skip = [], False
    for line in lic.split('\n'):
        if line.startswith(LIC_HEAD):
            skip = True
        elif skip and not line.startswith('  '):
            skip = False
        if not skip:
            out.append(line)
    return r, '\n'.join(out)


def update_docs(pack, names, srcs, rep):
    """rewrite this script's entries in models.json, sources.json, README.md and LICENSE.txt; everything else is kept
    (checked before anything is written)"""
    P = lambda f: os.path.join(pack, f)
    mj, sj = json.load(open(P('models.json'), encoding='utf-8')), json.load(open(P('sources.json'), encoding='utf-8'))
    readme, lic = open(P('README.md'), encoding='utf-8').read(), open(P('LICENSE.txt'), encoding='utf-8').read()
    old = (json.dumps(mj), json.dumps(sj), strip_mine(readme, lic))
    for name in names:
        mj[name]['lods'] = lods_entry(name, srcs[name], rep[name + '_LOD1.obj'], rep[name + '_LOD2.obj'])
    mine = set()
    ent = []
    owner = dict(commit='n/a (generated %s)' % DATE, license='project-owned (original work)',
                 author='Jewelry-store-vr-demo project (generated with Claude)')
    for name in names:
        for level in ('LOD1', 'LOD2'):
            fn = '%s_%s.obj' % (name, level); r = rep[fn]; mine.add(fn)
            vv = {k: r['verify'][k] for k in ('bounds_max_diff_m', 'min_y', 'components', 'part_tris', 'open_edges',
                                              'facing_away', 'zero_area', 'hole', 'stone_sink_m', 'hulled_parts') if k in r['verify']}
            ent.append(dict(file=fn, source='derived from %s.obj by Source~/make_lods.py' % name, **owner,
                            sha256=r['sha256'], bytes=r['bytes'], triangles=r['tris'], target=r['target'],
                            derived_from=name + '.obj', derived_from_sha256=srcs[name]['sha256'], tool=TOOL_ID,
                            derivation='%s %s -> %s triangles: %s' % (name, fmt(srcs[name]['tris']), fmt(r['tris']), how(name, level, r)),
                            groups=[{k: d[k] for k in ('g', 'mtl', 'mode', 'src', 'tris', 'target', 'share', 'parts', 'part_tris', 'grid')
                                     if k in d} for d in r['groups']], verify=vv))
    for fn, src in (('Source~/make_lods.py', 'original (Source~/make_lods.py; copied from %s and %s)' % (
                        TOOLS['decimate_exact'][0], TOOLS['lods_street'][0])),
                    ('Source~/render_lods.py', 'original (Source~/render_lods.py, uses Source~/render.py)'),
                    ('Source~/Preview_LODs.jpg', 'original (Source~/render_lods.py)')):
        if os.path.isfile(P(fn)):
            data = open(P(fn), 'rb').read(); mine.add(fn)
            e = dict(file=fn, source=src, **owner, sha256=hashlib.sha256(data).hexdigest(), bytes=len(data))
            if fn.endswith('make_lods.py'):
                e['copied_from'] = [dict(path=p_, sha256=h_) for p_, h_ in TOOLS.values()]
            if fn.endswith('.jpg'):
                e['license'] = 'project-owned contact sheet (renders of project-owned models only)'
            ent.append(e)
    keep_e = [f for f in sj['files'] if f['file'] not in mine]
    canon = lambda L: sorted([f for f in L if not f['file'].startswith('Source~/')], key=lambda f: f['file']) + \
        sorted([f for f in L if f['file'].startswith('Source~/')], key=lambda f: f['file'])
    sj['files'] = canon(keep_e + ent) if canon(keep_e) == keep_e else keep_e + ent
    sj['lod_variants'] = dict(tool=TOOL_ID, preview='Source~/Preview_LODs.jpg', generated=DATE,
                              copied_from=[dict(path=p_, sha256=h_) for p_, h_ in TOOLS.values()],
                              models={n: [n + '_LOD1.obj', n + '_LOD2.obj'] for n in names},
                              note='LOD0 is the shipped model; in the night store LOD1 is the top level of %s. Unverified '
                                   'in Unity.' % ', '.join(TOP_LOD1))
    # README: section before 'Regenerating', a sentence in the introduction, a line in 'Regenerating'
    r, lic2 = strip_mine(readme, lic)
    sec = readme_section(names, srcs, rep)
    anchor = '`Source~/Preview.jpg` shows every model.'
    if anchor in r:
        r = r.replace(anchor, anchor + README_INTRO, 1)
    if '\n## Regenerating\n' in r:
        a = r.index('\n## Regenerating\n')
        r = r[:a + 1] + sec + '\n' + r[a + 1:]
        b = r.find('\n## ', r.index('\n## Regenerating\n') + 5)
        b = len(r.rstrip('\n')) if b < 0 else b
        r = r[:b] + '\n' + README_REGEN + r[b:]
    else:
        r = r.rstrip('\n') + '\n\n' + sec
    lines = lic2.split('\n')
    at = next((k for k, l in enumerate(lines) if 'Source~/Preview.jpg also shows renders' in l), None)
    words = (LIC_HEAD.strip() + DATE + '): ' + ', '.join('%s_LOD1/_LOD2.obj' % n for n in names) + ', made from the '
             'project-owned models above by Source~/make_lods.py (they use the same MTLs), and Source~/Preview_LODs.jpg, '
             'which shows project-owned models only, are project-owned too.').split(' ')
    block = ['  ' + words[0]]
    for w in words[1:]:                      # wrapped like the rest of the file, every line indented by two spaces
        if len(block[-1]) + 1 + len(w) > 116:
            block.append('  ' + w)
        else:
            block[-1] += ' ' + w
    if at is None:
        sys.exit('LICENSE.txt: the section 1 file list was not found')
    lines[at + 1:at + 1] = block
    lic_new = '\n'.join(lines)
    new = (json.dumps({k: {x: y for x, y in v.items() if x != 'lods'} if k in names else v for k, v in mj.items()}),
           json.dumps(dict(sj, files=[f for f in sj['files'] if f['file'] not in mine], **{'lod_variants': None})),
           strip_mine(r, lic_new))
    oldc = (json.dumps({k: {x: y for x, y in v.items() if x != 'lods'} if k in names else v
                        for k, v in json.loads(old[0]).items()}),
            json.dumps(dict(json.loads(old[1]), files=[f for f in json.loads(old[1])['files'] if f['file'] not in mine],
                            **{'lod_variants': None})), old[2])
    for what, a, b in zip(('models.json', 'sources.json', 'README.md / LICENSE.txt'), oldc, new):
        if a != b:
            sys.exit('docs: %s would change outside this script\'s entries' % what)
    for f, text in (('models.json', json.dumps(mj, indent=1)), ('sources.json', json.dumps(sj, indent=1)),
                    ('README.md', r), ('LICENSE.txt', lic_new)):
        with open(P(f), 'w', encoding='utf-8', newline='\n') as fh:
            fh.write(text)
    print('docs: models.json lods x%d, sources.json %d entries + lod_variants, README section and LICENSE lines'
          % (len(names), len(ent)))


def main():
    args = sys.argv[1:]
    pack = args.pop(0)
    report, docs, previews = None, True, False
    while args and args[0].startswith('--'):
        a = args.pop(0)
        if a == '--report':
            report = args.pop(0)
        elif a == '--no-docs':
            docs = False
        elif a == '--previews':
            previews = True
        else:
            sys.exit('unknown option ' + a)
    names = args or list(MODELS)
    trimesh = load_trimesh()
    rep, ok, out_files, srcs = {}, True, {}, {}
    for name in names:
        src = os.path.join(pack, name + '.obj')
        src_text = open(src, encoding='utf-8').read()
        M = parse(src)
        src_R = raw_obj(src_text)
        sb = box([src_R['V'][c[0]][:3] for _, _, cs in src_R['F'] for c in cs])
        srcs[name] = dict(tris=len(src_R['F']), bytes=os.path.getsize(src), size=max(sb[1][k] - sb[0][k] for k in range(3)),
                          sha256=hashlib.sha256(open(src, 'rb').read()).hexdigest())
        mtl_lines = M['head'] or ['mtllib %s.mtl' % name]
        textured = textured_fn(pack, name)
        for level, cfg in MODELS[name].items():
            groups, relaxed, hulled = build(M, cfg, name, textured)
            text = write(M, groups, name, level, cfg['target'], mtl_lines)
            fails, meas = verify(trimesh, name, level, text, src_text, src_R, groups, cfg, pack, mtl_lines)
            meas['relaxed_collapses'] = relaxed
            meas['hulled_parts'] = hulled
            fn = '%s_%s.obj' % (name, level)
            data = text.encode('utf-8')
            gl = []
            for s, f, mode, goal, inf in groups:
                d = dict(g=s['g'], mtl=s['mtl'], mode=mode, src=len(s['faces']), tris=len(f))
                if mode != 'copy':
                    d['target' if mode == 'fixed' else 'share'] = goal
                    d.update({k: v for k, v in inf.items() if k != 'part_tris'})
                    if 'part_tris' in inf:
                        d['part_tris'] = [min(inf['part_tris']), max(inf['part_tris'])]
                gl.append(d)
            rep[fn] = dict(derived_from=name + '.obj', target=cfg['target'], keep=cfg.get('keep', []),
                           fixed=cfg.get('fixed', {}), parts=cfg.get('parts', []), relax=cfg.get('relax'),
                           uv_max=cfg.get('uv_max', UV_MAX) if cfg.get('relax') else None, tris=meas['tris'],
                           bytes=len(data), sha256=hashlib.sha256(data).hexdigest(), verify=meas, groups=gl)
            print('%-28s target %5d -> %5d tris, %7d bytes, bounds diff %.6f m, open edges %d -> %d, facing away %d -> %d, '
                  'zero-area %d, relaxed %d, hulled %d' % (fn, cfg['target'], meas['tris'], len(data),
                                                 meas.get('bounds_max_diff_m', -1),
                                                 meas.get('open_edges', [0, 0])[0], meas.get('open_edges', [0, 0])[1],
                                                 meas.get('facing_away', [0, 0])[0], meas.get('facing_away', [0, 0])[1],
                                                 meas.get('zero_area', -1), relaxed, hulled))
            for d in gl:
                print('    %-14s %-12s %-6s %5d -> %5d' % (d['g'][:14], d['mtl'], d['mode'], d['src'], d['tris'])
                      + ('  (target %d)' % d['target'] if 'target' in d else '  (share %d)' % d['share'] if 'share' in d else '')
                      + ('  parts %d, %d-%d tris each' % (d['parts'], d['part_tris'][0], d['part_tris'][1]) if 'part_tris' in d else
                         '  parts %d' % d['parts'] if 'parts' in d else '')
                      + ('  faceted parts %d' % d['faceted_parts'] if d.get('faceted_parts') else '')
                      + ('  dropped zero-area %d' % d['dropped'] if d.get('dropped') else '')
                      + ('  dropped internal %d' % d['internal'] if d.get('internal') else '')
                      + ('  capped +%d' % d['cap_tris'] if d.get('cap_tris') else '')
                      + ('  culled %d parts' % d['culled'] if d.get('culled') else '')
                      + ('  grid-rebuilt %d parts' % d['grid_parts'] if d.get('grid_parts') else '')
                      + ('  rest-locked %d vertices' % d['rest_locked_vertices'] if d.get('rest_locked_vertices') else '')
                      + ('  uv-free' if d.get('uvfree') else ''))
            for k, v in meas.items():
                if k.startswith(('hole', 'stone', 'parts', 'components', 'min_y')):
                    print('    %s: %s' % (k, v))
            for f in fails:
                print('VERIFY FAILED %s %s: %s' % (name, level, f))
            ok &= not fails
            out_files[fn] = data
    if not ok:
        sys.exit('VERIFY failed: nothing written')
    for fn, data in out_files.items():
        with open(os.path.join(pack, fn), 'wb') as fh:
            fh.write(data)
    if report:
        with open(report, 'w', encoding='utf-8', newline='\n') as fh:
            json.dump(rep, fh, indent=1)
    print('VERIFY passed; wrote', ', '.join(out_files))
    if previews:
        here = os.path.dirname(os.path.abspath(__file__))
        subprocess.run([sys.executable, '-I', '-B', os.path.join(here, 'render_lods.py'), pack,
                        os.path.join(pack, 'Source~', 'Preview_LODs.jpg')], check=True)
    if docs and set(names) == set(MODELS) and all(os.path.isfile(os.path.join(pack, f)) for f in
                                                   ('models.json', 'sources.json', 'README.md', 'LICENSE.txt')):
        update_docs(pack, names, srcs, rep)
    elif docs:
        print('docs: skipped (not every model was built, or PACKDIR has no pack documents)')


if __name__ == '__main__':
    main()
