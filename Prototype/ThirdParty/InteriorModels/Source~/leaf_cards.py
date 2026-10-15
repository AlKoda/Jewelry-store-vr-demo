"""Alpha-cut leaf cards for a dense Poly Haven plant (InteriorModels pack, project code).

The source leaves of potted_plant_01 are single-sided blades whose scalloped outline is modelled in geometry
(~600 triangles per leaf). Decimating that outline gives jagged polygon leaves. Here every leaf blade is
replaced by a small card that keeps the exact look through an alpha mask instead:
  * the blade's UV outline -> convex hull -> smallest enclosing k-gon (edges removed by extending their
    neighbours, least added area first), plus a few interior vertices (farthest-point sampled) for curvature;
  * original vertices keep their exact position, UV and normal; new k-gon corners get position and normal from a
    least-squares affine fit UV -> XYZ over the nearest blade vertices (extrapolation of a few millimetres);
  * the alpha mask is the UV footprint of all original leaf triangles (rasterised at 4x, box-filtered), so the
    card shows exactly the original blade outline with an alpha cut-off of 0.5;
  * colours outside the footprint are padded outwards so mip-mapping does not bleed black into the edges.
Petiole and stalk faces (their own UV strips) are returned separately for normal decimation.

build(P, UV, N, F, opts) -> dict(P, UV, N, F_cards, F_rest, mask (PIL L, opts['mask_px']), info)"""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy.spatial import ConvexHull, Delaunay, cKDTree


def _isect(a, b, c, d):
    r, s = b - a, d - c
    den = r[0] * s[1] - r[1] * s[0]
    if abs(den) < 1e-14: return None
    t = ((c - a)[0] * s[1] - (c - a)[1] * s[0]) / den
    return a + t * r


def enclose(poly, k, max_grow=1.6):
    """Smallest-added-area enclosing k-gon of a convex CCW polygon. Returns list of (uv, src_index or -1)."""
    pts = [(np.asarray(p[0], float), p[1]) for p in poly]
    a0 = abs(_area([p[0] for p in pts]))
    while len(pts) > k:
        n = len(pts); best = None
        for i in range(n):
            a, b, c, d = pts[i - 1][0], pts[i][0], pts[(i + 1) % n][0], pts[(i + 2) % n][0]
            X = _isect(a, b, c, d)
            if X is None or np.dot(X - b, b - a) <= 0 or np.dot(X - c, c - d) <= 0: continue
            ar = 0.5 * abs((b - X)[0] * (c - X)[1] - (b - X)[1] * (c - X)[0])
            if best is None or ar < best[0]: best = (ar, i, X)
        if best is None: break
        _, i, X = best
        if abs(_area([p[0] for p in pts])) + best[0] > a0 * max_grow: break
        n = len(pts); j = (i + 1) % n
        new = [(X, -1)]
        pts = [p for q, p in enumerate(pts) if q not in (i, j)]
        ins = i if j > i else i - 1
        pts[ins:ins] = new
    return pts


def _area(p):
    p = np.asarray(p); return 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1])


def build(P, UV, N, F, opts):
    import trimesh
    fc = UV[F].mean(1)
    pet = np.zeros(len(F), bool)
    for (u0, u1, v0, v1) in opts.get('rest_uv_boxes', []):
        pet |= (fc[:, 0] >= u0) & (fc[:, 0] <= u1) & (fc[:, 1] >= v0) & (fc[:, 1] <= v1)
    tm = trimesh.Trimesh(P, F, process=False)
    cc = trimesh.graph.connected_components(tm.face_adjacency, nodes=np.arange(len(F)), min_len=1)
    # ---- alpha mask: footprint of every original leaf triangle in UV space
    mpx = opts.get('mask_px', 1024); ss = 4; R = mpx * ss
    mk = Image.new('L', (R, R), 0); dr = ImageDraw.Draw(mk)
    for t in UV[F]:
        dr.polygon([(t[0, 0] * R, (1 - t[0, 1]) * R), (t[1, 0] * R, (1 - t[1, 1]) * R), (t[2, 0] * R, (1 - t[2, 1]) * R)], fill=255)
    mk_full = np.asarray(mk) > 127
    mask = mk.resize((mpx, mpx), Image.BOX)
    gm = np.asarray(mask.resize((512, 512), Image.BOX)) > 40      # coarse global footprint for the foreign check
    newP, newU, newN = [P], [UV], [N]; nv = len(P)
    cards = []; rest = [np.where(pet)[0]]; stats = {'cards': 0, 'kept': 0, 'grow': [], 'k': [], 'tris': []}
    for c in cc:
        bl = c[~pet[c]]
        if len(bl) == 0: continue
        big = len(bl) > opts.get('big_faces', 100)
        k, n_in = (opts.get('k_big', 8), opts.get('in_big', 4)) if big else (opts.get('k_small', 5), opts.get('in_small', 0))
        if len(bl) < opts.get('min_faces', 6):
            rest.append(bl); stats['kept'] += 1; continue
        vid = np.unique(F[bl]); u = UV[vid]
        try:
            h = ConvexHull(u)
        except Exception:
            rest.append(bl); stats['kept'] += 1; continue
        hull = [(u[i], int(vid[i])) for i in h.vertices]          # CCW
        if _area([p[0] for p in hull]) < 0: hull = hull[::-1]
        # own footprint (coarse) for the foreign-texture check
        own = Image.new('L', (512, 512), 0); d2 = ImageDraw.Draw(own)
        for t in UV[F[bl]]:
            d2.polygon([(t[q, 0] * 512, (1 - t[q, 1]) * 512) for q in range(3)], fill=255)
        own = np.asarray(own.filter(ImageFilter.MaxFilter(9))) > 0
        hm = Image.new('L', (512, 512), 0); ImageDraw.Draw(hm).polygon([(p[0][0] * 512, (1 - p[0][1]) * 512) for p in hull], fill=255)
        own |= np.asarray(hm.filter(ImageFilter.MaxFilter(3))) > 0   # inside the blade's own hull nothing is foreign
        kk = k
        while True:
            poly = enclose(hull, kk)
            pm = Image.new('L', (512, 512), 0); ImageDraw.Draw(pm).polygon([(p[0][0] * 512, (1 - p[0][1]) * 512) for p in poly], fill=255)
            pm = np.asarray(pm) > 0
            foreign = (pm & gm & ~own).sum()
            if foreign <= max(2, 0.004 * pm.sum()) or kk >= len(hull): break
            kk += 2
        grow = abs(_area([p[0] for p in poly])) / max(abs(_area([p[0] for p in hull])), 1e-12)
        stats['grow'].append(grow)
        # vertices: polygon corners + interior samples
        tree = cKDTree(u)
        cu, cidx = [], []
        for uv_, src in poly:
            if src >= 0:
                cidx.append(src); cu.append(UV[src])
            else:
                _, nn = tree.query(uv_, k=min(10, len(u)))
                A = np.c_[u[nn], np.ones(len(nn))]
                coef, *_ = np.linalg.lstsq(A, P[vid[nn]], rcond=None)
                p3 = np.r_[uv_, 1] @ coef
                w = 1 / (np.linalg.norm(u[nn] - uv_, axis=1) + 1e-6)
                n3 = (N[vid[nn]] * w[:, None]).sum(0); n3 /= np.linalg.norm(n3) + 1e-12
                newP.append(p3[None]); newU.append(np.asarray(uv_)[None]); newN.append(n3[None])
                cidx.append(nv); cu.append(uv_); nv += 1
        if n_in:
            e = np.sort(np.vstack([F[bl][:, [0, 1]], F[bl][:, [1, 2]], F[bl][:, [2, 0]]]), 1)
            ue, cnt = np.unique(e, axis=0, return_counts=True)
            bd = set(int(x) for x in ue[cnt == 1].ravel())
            inner = np.array([v for v in vid if v not in bd])
            sel = []
            if len(inner):
                ref = np.array(cu)
                for _ in range(n_in):
                    dd = np.min(np.linalg.norm(UV[inner][:, None] - ref[None], axis=2), 1)
                    j = inner[int(np.argmax(dd))]
                    if dd.max() < 1e-4: break
                    sel.append(int(j)); ref = np.vstack([ref, UV[j]])
            for j in sel: cidx.append(j); cu.append(UV[j])
        cu = np.array(cu); cidx = np.array(cidx)
        try:
            tri = Delaunay(cu)
        except Exception:
            rest.append(bl); stats['kept'] += 1; continue
        T = cidx[tri.simplices]
        cards.append(T); stats['cards'] += 1; stats['k'].append(len(poly)); stats['tris'].append(len(T))
    P2 = np.vstack(newP); U2 = np.vstack(newU); N2 = np.vstack(newN)
    Fc = np.vstack(cards) if cards else np.zeros((0, 3), int)
    Fr = F[np.concatenate(rest)] if rest else np.zeros((0, 3), int)
    g = np.array(stats['grow']) if stats['grow'] else np.array([1.0])
    info = {'leaf_cards': stats['cards'], 'card_tris': int(len(Fc)), 'kept_components': stats['kept'], 'rest_tris_before': int(len(Fr)),
            'card_area_vs_hull_median': round(float(np.median(g)), 3), 'card_area_vs_hull_max': round(float(g.max()), 3),
            'polygon_corners_hist': {int(a): int(b) for a, b in zip(*np.unique(stats['k'], return_counts=True))}}
    return {'P': P2, 'UV': U2, 'N': N2, 'F_cards': Fc, 'F_rest': Fr, 'mask': mask, 'info': info}


def pad_colour(rgb, alpha, iters=12):
    """Push leaf colours outwards into the transparent area (avoids dark fringes on mip levels)."""
    a = np.asarray(alpha, np.float32) / 255; c = np.asarray(rgb, np.float32)
    known = a > 0.5; out = c.copy()
    acc = c * known[..., None]; w = known.astype(np.float32)
    for r in (1, 2, 4, 8, 16, 32, 64):
        A = np.stack([np.asarray(Image.fromarray(acc[..., k].clip(0, 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(r)), np.float32) for k in range(3)], -1)
        W = np.asarray(Image.fromarray((w * 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(r)), np.float32) / 255
        fill = (~known) & (W > 0.02)
        est = A / np.maximum(W[..., None], 1e-3)
        out[fill] = est[fill]; acc[fill] = est[fill]; w[fill] = 1; known = known | fill
        if known.all(): break
    return Image.fromarray(out.clip(0, 255).astype(np.uint8))


def rebuild_tubes(P, UV, N, F, sides=4, segs=4, min_len=0.01):
    """Rebuild thin stalk tubes as low-poly tubes along their own centre line.
    Per welded component: geodesic distance from one end (double sweep), segs + 1 stations at even distance,
    centre = mean of the vertices near the station, radius = their median distance from it; rings of `sides`
    vertices (parallel-transport frame) plus a seam duplicate. Each new vertex takes the UV of the nearest
    original vertex. Components that are not tube-like (fit error > 2.5 x radius, or shorter than min_len)
    are returned unchanged. Returns (P, UV, N, F, stats)."""
    import trimesh
    from scipy.sparse import coo_matrix
    from scipy.sparse.csgraph import dijkstra
    key = np.round(P, 6); W, first, inv = np.unique(key, axis=0, return_index=True, return_inverse=True); inv = inv.ravel()
    Fw = inv[F]; ok = (Fw[:, 0] != Fw[:, 1]) & (Fw[:, 1] != Fw[:, 2]) & (Fw[:, 0] != Fw[:, 2]); Fw = Fw[ok]; Fo = F[ok]
    tm = trimesh.Trimesh(W, Fw, process=False)
    cc = trimesh.graph.connected_components(tm.face_adjacency, nodes=np.arange(len(Fw)), min_len=1)
    oP, oU, oN, oF = [], [], [], []; nv = 0; st = {'tubes': 0, 'kept': 0}
    tree_all = cKDTree(P)
    for c in cc:
        fw = Fw[c]; vid = np.unique(fw); loc = {v: i for i, v in enumerate(vid)}
        e = np.vstack([fw[:, [0, 1]], fw[:, [1, 2]], fw[:, [2, 0]]])
        a_ = np.array([loc[x] for x in e[:, 0]]); b_ = np.array([loc[x] for x in e[:, 1]])
        w_ = np.linalg.norm(W[e[:, 0]] - W[e[:, 1]], axis=1)
        G = coo_matrix((np.r_[w_, w_], (np.r_[a_, b_], np.r_[b_, a_])), shape=(len(vid), len(vid))).tocsr()
        d0 = dijkstra(G, indices=0); s = int(np.argmax(np.where(np.isfinite(d0), d0, -1)))
        d = dijkstra(G, indices=s); d[~np.isfinite(d)] = 0; L = d.max()
        X = W[vid]
        keep_orig = L < min_len
        if not keep_orig:
            st_t = np.linspace(0, L, segs + 1); hw = L / segs * 0.5
            C, Rr = [], []
            for t in st_t:
                m = np.abs(d - t) <= hw * (1.0 if 0 < t < L else 0.6)
                if m.sum() < 3: m = np.argsort(np.abs(d - t))[:6]
                C.append(X[m].mean(0))
            C = np.array(C)
            Tg = np.gradient(C, axis=0); Tg /= np.linalg.norm(Tg, axis=1, keepdims=True) + 1e-12
            for k, t in enumerate(st_t):
                m = np.abs(d - t) <= hw
                if m.sum() < 3: m = np.argsort(np.abs(d - t))[:6]
                rel = X[m] - C[k]; rel -= np.outer(rel @ Tg[k], Tg[k])
                Rr.append(np.median(np.linalg.norm(rel, axis=1)))
            Rr = np.array(Rr); rmed = max(float(np.median(Rr)), 1e-4)
            # fit error: distance of every vertex to the polyline minus the radius
            seg_err = []
            for x in X[::max(1, len(X) // 200)]:
                best = 1e9
                for k in range(segs):
                    p0, p1 = C[k], C[k + 1]; v = p1 - p0; tt = np.clip(np.dot(x - p0, v) / (np.dot(v, v) + 1e-15), 0, 1)
                    best = min(best, np.linalg.norm(x - (p0 + tt * v)))
                seg_err.append(abs(best - rmed))
            keep_orig = np.percentile(seg_err, 95) > 2.5 * rmed + 0.0015
        if keep_orig:
            fo = Fo[c]; u_ = np.unique(fo); rm = {v: i for i, v in enumerate(u_)}
            oP.append(P[u_]); oU.append(UV[u_]); oN.append(N[u_]); oF.append(np.vectorize(rm.get)(fo) + nv); nv += len(u_); st['kept'] += 1
            continue
        # frames (parallel transport)
        ref = np.array([0, 1, 0]) if abs(Tg[0][1]) < 0.9 else np.array([1, 0, 0])
        Nf = np.cross(Tg[0], ref); Nf /= np.linalg.norm(Nf)
        rings = []
        for k in range(segs + 1):
            if k:
                Nf = Nf - np.dot(Nf, Tg[k]) * Tg[k]; Nf /= np.linalg.norm(Nf) + 1e-12
            Bf = np.cross(Tg[k], Nf)
            ang = 2 * np.pi * np.arange(sides + 1) / sides
            dirs = np.cos(ang)[:, None] * Nf + np.sin(ang)[:, None] * Bf
            rings.append((C[k] + max(Rr[k], 0.3 * rmed) * dirs, dirs))
        cidx = np.unique(Fo[c]); ctree = cKDTree(P[cidx])
        for k, (pts, dirs) in enumerate(rings):
            _, nn = ctree.query(pts)
            oP.append(pts); oN.append(dirs); oU.append(UV[cidx[nn]])
        ring0 = nv
        for k in range(segs):
            for j in range(sides):
                a = ring0 + k * (sides + 1) + j; b = a + 1; a2 = a + sides + 1; b2 = a2 + 1
                oF.append(np.array([[a, a2, b2], [a, b2, b]]))
        nv += (segs + 1) * (sides + 1); st['tubes'] += 1
    Pn = np.vstack(oP); Un = np.vstack(oU); Nn = np.vstack(oN); Fn = np.vstack(oF)
    # orientation: outward (agree with the radial normals)
    gn = np.cross(Pn[Fn[:, 1]] - Pn[Fn[:, 0]], Pn[Fn[:, 2]] - Pn[Fn[:, 0]])
    fl = np.sum(gn * Nn[Fn].mean(1), 1) < 0; Fn[fl] = Fn[fl][:, ::-1]
    st['tris'] = int(len(Fn))
    return Pn, Un, Nn, Fn, st
