"""Convert a Poly Haven glTF (gltf + bin + textures) to the InteriorModels pack format.

convert_ph.py SRC_GLTF OUT_DIR NAME --spec JSON
spec keys: tris (target, 0 = keep), color/normal/ms (px), glass {mat: [r,g,b,d]}, short {mat: Suffix},
           pivot floor|wall|ceiling, yaw (deg about Y), scale, skip [mat...], alpha_png (bool), report path,
           per_node {node: target tris, 0 = keep, or {tris, weld}}: decimate those glTF nodes one by one (their
           material is then not decimated as a whole; weld = decimate with seams welded),
           leaf_cards {mat: opts}: leaf blades -> alpha-cut cards, stalks decimated welded (leaf_cards.py), gravel {node: opts}: replace a pebble layer by a baked heightfield disc
           (bake_gravel.py; opts: name, shell node, rings, segs, color_px, normal_px, ms_px),
           backfaces [mat...]: add reversed copies of every face (single-sided leaves, rendered with culling),
           dec {agg, preserve_border}: fast_simplification options (preserve_border keeps leaf outlines and UV seams),
           seam_aware [mat...]: decimate those materials on the welded mesh and re-split per UV island (decimate_seam_aware).
Every part gets a winding check: faces whose geometric normal disagrees with their vertex normals are flipped (once after
decimation and again on the rounded values written to the OBJ).
Geometry: trimesh scene dump (node transforms applied), grouped per material, UV-exact decimation
(collapses replayed on the original vertices, so kept vertices keep their exact UV and normal).
Textures: baseColor -> _Color.jpg (PNG with alpha for MASK materials), normal -> _Normal.jpg (OpenGL +Y),
metallicRoughness (G rough, B metal, times factors) -> _MetallicSmoothness.png (R=G=B metal, A = 1 - rough)."""
import sys, os, json, math, argparse
import numpy as np
import trimesh
from PIL import Image
import fast_simplification as fs
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

def img_for(gl, base, texinfo):
    if not texinfo: return None
    t = gl['textures'][texinfo['index']]
    src = t.get('source')
    uri = gl['images'][src]['uri']
    return os.path.join(base, uri)

DEC = {}  # set from spec['dec'] in main(): {'agg': 3, 'preserve_border': True} keeps open borders and UV seams closed

def decimate_welded(P, UV, N, F, target, agg=7):
    """Decimate with vertices welded by position (UV seams cannot open into cracks); each kept vertex takes the
    attributes of one original vertex at that position. For thin parts with uniform textures (stalks)."""
    key = np.round(P, 6); W, first, inv = np.unique(key, axis=0, return_index=True, return_inverse=True)
    inv = inv.ravel(); Fw = inv[F]
    ok = (Fw[:, 0] != Fw[:, 1]) & (Fw[:, 1] != Fw[:, 2]) & (Fw[:, 0] != Fw[:, 2]); Fw = Fw[ok]
    if len(Fw) <= target: return P[first], UV[first], N[first], Fw
    _, _, coll = fs.simplify(W.astype(np.float32), Fw.astype(np.int64), target_count=target, return_collapses=True, agg=agg)
    W2, F2, mapping = fs.replay_simplification(W.astype(np.float32), Fw.astype(np.int64), coll)
    orig = np.full(len(W2), -1)
    d = np.linalg.norm(W - W2[np.clip(mapping, 0, None)], axis=1)
    for i in np.argsort(d):
        j = mapping[i]
        if j >= 0 and orig[j] < 0: orig[j] = i
    return P[first[orig]], UV[first[orig]], N[first[orig]], F2

def decimate_seam_aware(P, UV, N, F, target, agg=7):
    """Decimate on the position-welded mesh (UV seams cannot open into cracks, and parts whose UV seams are
    everywhere, such as thin lamp rods, can still be reduced), then give every output face the UVs and normals
    of ONE UV island of the source: each corner takes the exact attributes of the source vertex at that position
    in the face's island; a corner that lies outside that island (collapsed across a seam) gets UV and normal
    interpolated at its closest point on the island's own triangles. Real open borders stay fixed."""
    import trimesh
    from scipy.spatial import cKDTree
    key = np.round(P, 6); W, first, inv = np.unique(key, axis=0, return_index=True, return_inverse=True); inv = inv.ravel()
    Fw = inv[F]; ok = (Fw[:, 0] != Fw[:, 1]) & (Fw[:, 1] != Fw[:, 2]) & (Fw[:, 0] != Fw[:, 2]); Fw = Fw[ok]; Fs = F[ok]
    if len(Fw) <= target: return P, UV, N, F
    _, _, coll = fs.simplify(W.astype(np.float32), Fw.astype(np.int64), target_count=target, return_collapses=True, agg=agg, preserve_border=True)
    W2, F2, mapping = fs.replay_simplification(W.astype(np.float32), Fw.astype(np.int64), coll)
    keep = np.full(len(W2), -1); d = np.linalg.norm(W - W2[np.clip(mapping, 0, None)], axis=1)
    for i in np.argsort(d):
        j = mapping[i]
        if j >= 0 and keep[j] < 0: keep[j] = i
    # UV islands of the split source mesh
    tm = trimesh.Trimesh(P, Fs, process=False)
    isl_f = np.zeros(len(Fs), int)
    for k, c in enumerate(trimesh.graph.connected_components(tm.face_adjacency, nodes=np.arange(len(Fs)), min_len=1)): isl_f[c] = k
    isl_v = np.full(len(P), -1); isl_v[Fs.ravel()] = np.repeat(isl_f, 3)
    cand = {}
    for v in np.unique(Fs):
        cand.setdefault(int(inv[v]), {})[int(isl_v[v])] = int(v)
    cent = cKDTree(P[Fs].mean(1))
    faces_of = {}
    for fi, k in enumerate(isl_f): faces_of.setdefault(int(k), []).append(fi)
    def closest_attr(p, isl):
        T = P[Fs[faces_of[isl]]]; a, b, c = T[:, 0], T[:, 1], T[:, 2]
        # closest point on each triangle (Ericson), vectorised
        ab, ac, ap = b - a, c - a, p - a
        d1, d2 = np.sum(ab * ap, 1), np.sum(ac * ap, 1)
        bp = p - b; d3, d4 = np.sum(ab * bp, 1), np.sum(ac * bp, 1)
        cp = p - c; d5, d6 = np.sum(ab * cp, 1), np.sum(ac * cp, 1)
        va = d3 * d6 - d5 * d4; vb = d5 * d2 - d1 * d6; vc = d1 * d4 - d3 * d2
        den = np.where(np.abs(va + vb + vc) < 1e-20, 1e-20, va + vb + vc)
        v_ = np.clip(vb / den, 0, 1); w_ = np.clip(vc / den, 0, 1); s_ = v_ + w_
        v_ = np.where(s_ > 1, v_ / s_, v_); w_ = np.where(s_ > 1, w_ / s_, w_); u_ = 1 - v_ - w_
        q = a * u_[:, None] + b * v_[:, None] + c * w_[:, None]
        k = int(np.argmin(np.linalg.norm(q - p, axis=1))); fi = faces_of[isl][k]; bc = np.array([u_[k], v_[k], w_[k]])
        return bc @ UV[Fs[fi]], bc @ N[Fs[fi]]
    outP, outU, outN, outF = [], [], [], []; vid = {}; projected = 0
    for f in F2:
        ws = [int(keep[j]) for j in f]
        sets = [set(cand[w]) for w in ws]
        common = sets[0] & sets[1] & sets[2]
        cpos = W[ws].mean(0)
        if common:
            isl = min(common) if len(common) == 1 else int(isl_f[cent.query(cpos, k=1)[1]]) if int(isl_f[cent.query(cpos, k=1)[1]]) in common else min(common)
        else:
            _, nn = cent.query(cpos, k=min(8, len(Fs)))
            isls = [int(isl_f[x]) for x in np.atleast_1d(nn)]
            score = {i: sum(i in st for st in sets) for i in isls}
            isl = max(isls, key=lambda i: (score[i], -isls.index(i)))
        tri = []
        for w in ws:
            if isl in cand[w]:
                kk = ('v', cand[w][isl])
                if kk not in vid:
                    vid[kk] = len(outP); v = cand[w][isl]; outP.append(P[v]); outU.append(UV[v]); outN.append(N[v])
            else:
                kk = ('p', w, isl)
                if kk not in vid:
                    uv_, n_ = closest_attr(W[w], isl); vid[kk] = len(outP); outP.append(W[w]); outU.append(uv_); outN.append(n_ / (np.linalg.norm(n_) + 1e-12)); projected += 1
            tri.append(vid[kk])
        outF.append(tri)
    print('seam_aware: %d -> %d faces, %d corner vertices projected across seams' % (len(Fs), len(outF), projected))
    return np.array(outP), np.array(outU), np.array(outN), np.array(outF)

def decimate(P, UV, N, F, target):
    if len(F) <= target: return P, UV, N, F
    _, _, coll = fs.simplify(P.astype(np.float32), F.astype(np.int64), target_count=target, return_collapses=True, **DEC)
    P2, F2, mapping = fs.replay_simplification(P.astype(np.float32), F.astype(np.int64), coll)
    orig = np.full(len(P2), -1); 
    d = np.linalg.norm(P - P2[np.clip(mapping, 0, None)], axis=1)
    for i in np.argsort(d):
        j = mapping[i]
        if j >= 0 and orig[j] < 0: orig[j] = i
    assert (orig >= 0).all()
    return P[orig], UV[orig], N[orig], F2


def inside_poly(pts, poly):
    x, y = pts[:, 0][:, None], pts[:, 1][:, None]
    x1, y1 = poly[:, 0][None], poly[:, 1][None]
    x2, y2 = np.roll(poly[:, 0], -1)[None], np.roll(poly[:, 1], -1)[None]
    cond = ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-15) + x1)
    return (np.sum(cond, axis=1) % 2) == 1

def leaf_retri(P, UV, N, F, k_boundary=10, k_inner=3):
    """Re-triangulate every leaf (connected component) from a subset of its ORIGINAL vertices:
    k_boundary points spaced along the outline + k_inner farthest interior points, Delaunay in the
    leaf's UV plane, triangles kept when their UV centroid lies inside the outline. Kept vertices keep
    exact position, UV and normal. Components that are not a single open disk are left unchanged."""
    from scipy.spatial import Delaunay
    tm = trimesh.Trimesh(P, F, process=False)
    cc = trimesh.graph.connected_components(tm.face_adjacency, nodes=np.arange(len(F)), min_len=1)
    out = []; done = 0
    for c in cc:
        Fc = F[c]
        if len(Fc) <= 2 * k_boundary:
            out.append(Fc); continue
        e = np.sort(np.vstack([Fc[:, [0, 1]], Fc[:, [1, 2]], Fc[:, [2, 0]]]), axis=1)
        ue, cnt = np.unique(e, axis=0, return_counts=True)
        be = ue[cnt == 1]
        if len(be) < k_boundary:
            out.append(Fc); continue
        # order the boundary loop
        adj = {}
        for a, b in be: adj.setdefault(a, []).append(b); adj.setdefault(b, []).append(a)
        if any(len(v) != 2 for v in adj.values()):
            out.append(Fc); continue
        start = be[0][0]; loop = [start]; prev = None; cur = start
        while True:
            nxt = [x for x in adj[cur] if x != prev][0]
            if nxt == start: break
            loop.append(nxt); prev, cur = cur, nxt
            if len(loop) > len(be): break
        if len(loop) != len(be):
            out.append(Fc); continue
        loop = np.array(loop)
        seg = np.linalg.norm(P[np.roll(loop, -1)] - P[loop], axis=1); L = np.concatenate([[0], np.cumsum(seg)[:-1]])
        # pick boundary points evenly by arc length, but always keep the sharpest corners (tip / stalk)
        sel = set()
        for t in np.linspace(0, seg.sum(), k_boundary, endpoint=False):
            sel.add(int(loop[np.argmin(np.abs(L - t))]))
        ang = []
        for i in range(len(loop)):
            a_, b_, c_ = UV[loop[i - 1]], UV[loop[i]], UV[loop[(i + 1) % len(loop)]]
            v1, v2 = a_ - b_, c_ - b_
            ang.append(np.dot(v1, v2) / (np.linalg.norm(v1) * np.linalg.norm(v2) + 1e-12))
        for i in np.argsort(ang)[::-1][:2]: sel.add(int(loop[i]))
        bsel = [v for v in loop if v in sel]
        inner = np.setdiff1d(np.unique(Fc), loop)
        isel = []
        if len(inner):
            pts = UV[bsel]
            for _ in range(k_inner):
                ref = np.vstack([pts] + ([UV[isel]] if isel else []))
                d = np.min(np.linalg.norm(UV[inner][:, None] - ref[None], axis=2), axis=1)
                j = inner[int(np.argmax(d))]
                if j in isel: break
                isel.append(int(j))
        verts = np.array(bsel + isel)
        uvv = UV[verts]
        if np.linalg.matrix_rank(uvv - uvv.mean(0)) < 2:
            out.append(Fc); continue
        try:
            tri = Delaunay(uvv)
        except Exception:
            out.append(Fc); continue
        cent = uvv[tri.simplices].mean(1)
        ok = inside_poly(cent, UV[loop])
        T = verts[tri.simplices[ok]]
        if len(T) == 0:
            out.append(Fc); continue
        # orientation: agree with the original vertex normals
        fn = np.cross(P[T[:, 1]] - P[T[:, 0]], P[T[:, 2]] - P[T[:, 0]])
        flip = np.sum(fn * N[T].mean(1), axis=1) < 0
        T[flip] = T[flip][:, ::-1]
        out.append(T); done += 1
    print('leaf_retri: re-triangulated', done, 'of', len(cc), 'leaves')
    return np.vstack(out)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('src'); ap.add_argument('out'); ap.add_argument('name'); ap.add_argument('--spec', default='{}')
    a = ap.parse_args(); spec = json.loads(a.spec)
    DEC.update(spec.get('dec', {}))
    base = os.path.dirname(a.src)
    gl = json.load(open(a.src))
    gmats = {m.get('name', 'mat%d' % i): m for i, m in enumerate(gl.get('materials', []))}
    scene = trimesh.load(a.src, force='scene', process=False)
    meshes = scene.dump(concatenate=False)
    groups = {}; bynode = {}; gravel_src = {}
    pernode = spec.get('per_node', {}); gspec = spec.get('gravel', {})
    for m in meshes:
        mn = getattr(m.visual.material, 'name', None) or 'mat'
        if mn in spec.get('skip', []): continue
        node = m.metadata.get('node') or m.metadata.get('name')
        uv = m.visual.uv if getattr(m.visual, 'uv', None) is not None else np.zeros((len(m.vertices), 2))
        arr = (np.asarray(m.vertices, float), np.asarray(uv, float), np.asarray(m.vertex_normals, float), np.asarray(m.faces))
        if node in gspec:
            gravel_src[node] = (mn,) + arr; continue
        bynode.setdefault(mn, []).append((node,) + arr)
        g = groups.setdefault(mn, [[], [], [], [], 0])
        g[0].append(arr[0]); g[1].append(arr[1]); g[2].append(arr[2])
        g[3].append(arr[3] + g[4]); g[4] += len(arr[0])
    parts = {}; baked = {}; node_report = {}; alpha_masks = {}; card_info = {}; nodec = set()
    total = sum(sum(len(f) for f in g[3]) for g in groups.values()) + sum(len(v[4]) for v in gravel_src.values())
    target = spec.get('tris', 0)
    # gravel layers -> baked discs (and the shell faces they seal off)
    drops = {}
    for node, (mn, P, UV, N, F) in gravel_src.items():
        import bake_gravel
        o = gspec[node]; gm = gmats.get(mn, {}); pbr = gm.get('pbrMetallicRoughness', {})
        tex = {'color': img_for(gl, base, pbr.get('baseColorTexture')), 'normal': img_for(gl, base, gm.get('normalTexture')),
               'rough': img_for(gl, base, pbr.get('metallicRoughnessTexture'))}
        sh = [x for x in bynode[mn] if x[0] == o['shell']][0]
        r = bake_gravel.bake(P, UV, N, F, tex, (sh[1], sh[4], sh[3]), o)
        drops[o['shell']] = r['drop']
        baked['__gravel_' + node] = {'name': o.get('name', 'Gravel'), 'images': r['images'], 'info': r['info']}
        parts['__gravel_' + node] = list(r['part'])
        node_report[node] = dict(r['info'], replaced_by=o.get('name', 'Gravel'))
        print('gravel', node, json.dumps(r['info']))
    for mn, g in groups.items():
        if any(x[0] in pernode for x in bynode[mn]):
            Ps, Us, Ns, Fs, off_ = [], [], [], [], 0
            for node, P, UV, N, F in bynode[mn]:
                n0 = len(F)
                if node in drops: F = F[~drops[node]]
                pn = pernode.get(node, 0); pn = pn if isinstance(pn, dict) else {'tris': pn}
                tgt = int(pn.get('tris', 0))
                if tgt and len(F) > tgt:
                    P, UV, N, F = (decimate_welded if pn.get('weld') else decimate)(P, UV, N, F, tgt)
                node_report[node] = {'source_tris': int(n0), 'tris': int(len(F)), 'hidden_dropped': int(drops[node].sum()) if node in drops else 0}
                Ps.append(P); Us.append(UV); Ns.append(N); Fs.append(F + off_); off_ += len(P)
            parts[mn] = [np.vstack(Ps), np.vstack(Us), np.vstack(Ns), np.vstack(Fs)]
            continue
        P = np.vstack(g[0]); UV = np.vstack(g[1]); N = np.vstack(g[2]); F = np.vstack(g[3])
        frac = spec.get('drop_small', {}).get(mn)
        if frac:
            tm = trimesh.Trimesh(P, F, process=False)
            cc = trimesh.graph.connected_components(tm.face_adjacency, nodes=np.arange(len(F)), min_len=1)
            areas = np.array([tm.area_faces[c].sum() for c in cc])
            order = np.argsort(areas); ndrop = int(len(cc) * frac)
            keep = np.ones(len(F), bool)
            for ci in order[:ndrop]: keep[cc[ci]] = False
            F = F[keep]
            print('drop_small', mn, 'removed', ndrop, 'of', len(cc), 'components ->', len(F), 'faces')
        for bmat, blo, bhi in spec.get('drop_box', []):
            if bmat != mn: continue
            tm = trimesh.Trimesh(P, F, process=False)
            cc = trimesh.graph.connected_components(tm.face_adjacency, nodes=np.arange(len(F)), min_len=1)
            keep = np.ones(len(F), bool); nd = 0
            for c in cc:
                v = P[np.unique(F[c])]
                if (v.min(0) >= np.array(blo)).all() and (v.max(0) <= np.array(bhi)).all():
                    keep[c] = False; nd += 1
            F = F[keep]
            print('drop_box', mn, 'removed', nd, 'components ->', len(F), 'faces')
        if mn in spec.get('leaf_cards', {}):
            import leaf_cards
            o = spec['leaf_cards'][mn]
            r = leaf_cards.build(P, UV, N, F, o)
            Fr = r['F_rest']
            if len(Fr) and o.get('tubes'):
                Pr, Ur, Nr, Fr, tst = leaf_cards.rebuild_tubes(r['P'], r['UV'], r['N'], Fr, **o['tubes'])
                r['info']['stalks'] = tst
            elif len(Fr):
                Pr, Ur, Nr, Fr = decimate_welded(r['P'], r['UV'], r['N'], Fr, o.get('rest_tris', 4000))
            else:
                Pr, Ur, Nr = r['P'][:0], r['UV'][:0], r['N'][:0]
            Pc, Uc, Nc, Fc = r['P'], r['UV'], r['N'], r['F_cards']
            used = np.unique(Fc); remap = np.full(len(Pc), -1); remap[used] = np.arange(len(used))
            Pc, Uc, Nc, Fc = Pc[used], Uc[used], Nc[used], remap[Fc]
            # cards get back faces (single-sided blades); stalks are tubes and do not need them
            nC = len(Pc)
            Pc = np.vstack([Pc, Pc]); Uc = np.vstack([Uc, Uc]); Nc = np.vstack([Nc, -Nc]); Fc = np.vstack([Fc, Fc[:, ::-1] + nC])
            P = np.vstack([Pc, Pr]); UV = np.vstack([Uc, Ur]); N = np.vstack([Nc, Nr]); F = np.vstack([Fc, Fr + len(Pc)])
            alpha_masks[mn] = r['mask']; card_info[mn] = dict(r['info'], rest_tris=int(len(Fr)), card_tris_with_backfaces=int(len(Fc)))
            print('leaf_cards', mn, json.dumps(card_info[mn]))
            parts[mn] = [P, UV, N, F]; nodec.add(mn)
            continue
        if mn in spec.get('leaf_retri', {}):
            kb, ki = spec['leaf_retri'][mn]
            F = leaf_retri(P, UV, N, F, kb, ki)
            print('leaf_retri', mn, '->', len(F), 'faces')
        if mn in spec.get('uv_fit', []):
            lo_uv, hi_uv = UV[F.ravel()].min(0), UV[F.ravel()].max(0)
            spec.setdefault('_crop', {})[mn] = (lo_uv.tolist(), hi_uv.tolist())
            UV = (UV - lo_uv) / np.maximum(hi_uv - lo_uv, 1e-9)
        if target and total > target:
            share = max(48, int(round(target * len(F) / total)))
            share = int(spec.get('share', {}).get(mn, share))
            P, UV, N, F = (decimate_seam_aware if mn in spec.get('seam_aware', []) else decimate)(P, UV, N, F, share)
        parts[mn] = [P, UV, N, F]
    # winding: every face must agree with its own vertex normals (decimation can fold faces over)
    wind = {}
    for mn in parts:
        P, UV, N, F = parts[mn]
        gn = np.cross(P[F[:, 1]] - P[F[:, 0]], P[F[:, 2]] - P[F[:, 0]])
        flip = np.sum(gn * N[F].mean(1), axis=1) < 0
        F = F.copy(); F[flip] = F[flip][:, ::-1]; parts[mn][3] = F
        wind[mn] = int(flip.sum())
    print('winding flips', wind)
    for mn in spec.get('backfaces', []):
        P, UV, N, F = parts[mn]; n = len(P)
        parts[mn] = [np.vstack([P, P]), np.vstack([UV, UV]), np.vstack([N, -N]), np.vstack([F, F[:, ::-1] + n])]
    # transforms
    yaw = math.radians(spec.get('yaw', 0)); sc = spec.get('scale', 1.0)
    R = np.array([[math.cos(yaw), 0, math.sin(yaw)], [0, 1, 0], [-math.sin(yaw), 0, math.cos(yaw)]])
    pre = spec.get('rot')  # optional extra rotation matrix rows
    for mn in parts:
        P, UV, N, F = parts[mn]
        if pre: Rm = np.array(pre); P = P @ Rm.T; N = N @ Rm.T
        parts[mn][0] = (P @ R.T) * sc; parts[mn][2] = N @ R.T
    allP = np.vstack([p[0] for p in parts.values()]); lo, hi = allP.min(0), allP.max(0)
    piv = spec.get('pivot', 'floor')
    if piv == 'floor': off = np.array([(lo[0] + hi[0]) / 2, lo[1], (lo[2] + hi[2]) / 2])
    elif piv == 'wall': off = np.array([(lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, lo[2]])
    elif piv == 'ceiling': off = np.array([(lo[0] + hi[0]) / 2, hi[1], (lo[2] + hi[2]) / 2])
    else: off = np.zeros(3)
    for mn in parts: parts[mn][0] = parts[mn][0] - off
    os.makedirs(a.out, exist_ok=True)
    name = a.name; short = spec.get('short', {}); glass = spec.get('glass', {})
    solid = [mn for mn in parts if mn not in glass]
    mtl = []; maps = {}; written = {}
    for mn in parts:
        gm = gmats.get(mn, {})
        if mn in baked:
            b = baked[mn]; mname = b['name']; info = {}
            fn = '%s_%s_Color.jpg' % (name, mname); b['images']['Color'].save(os.path.join(a.out, fn), quality=spec.get('q', 88), optimize=True); info['Color'] = fn
            fn = '%s_%s_Normal.jpg' % (name, mname); b['images']['Normal'].save(os.path.join(a.out, fn), quality=90, optimize=True); info['Normal'] = fn
            fn = '%s_%s_MetallicSmoothness.png' % (name, mname); b['images']['MS'].save(os.path.join(a.out, fn), optimize=True); info['MetallicSmoothness'] = fn
            info['metallic_mean'] = 0.0; info['smoothness_mean'] = b['info']['smoothness_mean']
            info['baked'] = 'top-down bake of the source pebble mesh (%d tris) onto a %d-triangle heightfield disc' % (b['info']['pebble_tris'], b['info']['disc_tris'])
            s = 'newmtl %s\nKd 1.000 1.000 1.000\nKs 0.050 0.050 0.050\nd 1.0\nmap_Kd %s\n# Normal %s\n# MetallicSmoothness %s' % (mname, info['Color'], info['Normal'], info['MetallicSmoothness'])
            mtl.append(s); maps[mname] = info; short[mn] = mname
            continue
        if mn in glass:
            r, gg, b, d = glass[mn]
            mtl.append('newmtl %s\nKd %.3f %.3f %.3f\nKs 0.500 0.500 0.500\nNs 200\nd %.2f' % (short.get(mn, mn), r, gg, b, d))
            maps[short.get(mn, mn)] = {'glass': True, 'Kd': [r, gg, b], 'd': d}
            continue
        suf = '' if len(solid) == 1 else '_' + short.get(mn, mn)
        pbr = gm.get('pbrMetallicRoughness', {})
        bcf = pbr.get('baseColorFactor', [1, 1, 1, 1])
        mask = gm.get('alphaMode') == 'MASK'
        info = {}
        cimg = img_for(gl, base, pbr.get('baseColorTexture'))
        cimg = spec.get('color_override', {}).get(mn, cimg)
        ckey = ('C', cimg, spec.get('color_px', {}).get(mn, spec.get('color', 1024)), mn in spec.get('uv_fit', []))
        if cimg and ckey in written and not ckey[3]:
            info['Color'] = written[ckey]; cimg = None
        if cimg:
            im = Image.open(cimg)
            mask = mask and im.mode in ('RGBA', 'LA', 'P')
            crop = spec.get('_crop', {}).get(mn)
            if crop and mn not in spec.get('color_override', {}):
                (u0, v0), (u1, v1) = crop; W_, H_ = im.size
                im = im.crop((int(round(u0 * W_)), int(round((1 - v1) * H_)), int(round(u1 * W_)), int(round((1 - v0) * H_))))
            size = spec.get('color_size', {}).get(mn) or (spec.get('color', 1024),) * 2
            if isinstance(spec.get('color_px', {}).get(mn), int): size = (spec['color_px'][mn],) * 2
            if crop and mn not in spec.get('color_size', {}):
                cw, ch = im.size; L = spec.get('color_px', {}).get(mn, spec.get('color', 1024)); sc_ = L / max(cw, ch)
                size = (max(1, int(round(cw * sc_))), max(1, int(round(ch * sc_))))
            im = im.convert('RGBA' if mask else 'RGB').resize(tuple(size), Image.LANCZOS)
            if mn in alpha_masks:
                import leaf_cards
                al = alpha_masks[mn].resize(tuple(size), Image.BOX)
                im = leaf_cards.pad_colour(im.convert('RGB'), al).convert('RGBA'); im.putalpha(al); mask = True
            if mask:
                fn = '%s%s_Color.png' % (name, suf); im.save(os.path.join(a.out, fn), optimize=True)
            else:
                fn = '%s%s_Color.jpg' % (name, suf); im.save(os.path.join(a.out, fn), quality=spec.get('q', 88), optimize=True)
            info['Color'] = fn; written[ckey] = fn
        nimg = img_for(gl, base, gm.get('normalTexture'))
        if mn in spec.get('no_maps', []): nimg = None
        npx_ = spec.get('normal_px', {}).get(mn, spec.get('normal', 1024))
        if nimg and ('N', nimg, npx_) in written:
            info['Normal'] = written[('N', nimg, npx_)]; nimg = None
        if nimg and spec.get('normal', 1024):
            fn = '%s%s_Normal.jpg' % (name, suf); written[('N', nimg, npx_)] = fn
            Image.open(nimg).convert('RGB').resize((spec.get('normal_px', {}).get(mn, spec.get('normal', 1024)),) * 2, Image.LANCZOS).save(os.path.join(a.out, fn), quality=90, optimize=True)
            info['Normal'] = fn
        mimg = img_for(gl, base, pbr.get('metallicRoughnessTexture'))
        if mn in spec.get('no_maps', []): mimg = None
        mf = pbr.get('metallicFactor', 1.0); rf = pbr.get('roughnessFactor', 1.0)
        mpx_ = spec.get('ms_px', {}).get(mn, spec.get('ms', 512))
        if mimg and ('M', mimg, mpx_, mf, rf) in written:
            info['MetallicSmoothness'] = written[('M', mimg, mpx_, mf, rf)]; mimg = None; info['shared'] = True
        if mimg and spec.get('ms', 512):
            px = mpx_
            arr = np.asarray(Image.open(mimg).convert('RGB').resize((px, px), Image.LANCZOS)).astype(np.float32) / 255
            metal = arr[..., 2] * mf; rough = arr[..., 1] * rf
            m8 = (metal * 255).clip(0, 255).astype(np.uint8); s8 = ((1 - rough) * 255).clip(0, 255).astype(np.uint8)
            fn = '%s%s_MetallicSmoothness.png' % (name, suf); written[('M', mimg, mpx_, mf, rf)] = fn
            Image.fromarray(np.dstack([m8, m8, m8, s8]), 'RGBA').save(os.path.join(a.out, fn), optimize=True)
            info['MetallicSmoothness'] = fn
            info['metallic_mean'] = round(float(metal.mean()), 3); info['smoothness_mean'] = round(float(1 - rough.mean()), 3)
        elif not info.get('shared'):
            info['metallic'] = mf if 'metallicFactor' in pbr else 0.0; info['smoothness'] = round(1 - rf, 3) if 'roughnessFactor' in pbr else 0.2
            if mimg and mn in spec.get('no_maps', []): pass
        if gm.get('emissiveFactor') and max(gm['emissiveFactor']) > 0: info['emission_color'] = [round(x, 3) for x in gm['emissiveFactor']]
        if mask: info['alpha_cutoff'] = gm.get('alphaCutoff', 0.5)
        if mn in card_info: info['leaf_cards'] = card_info[mn]
        mname = short.get(mn, mn)
        s = 'newmtl %s\nKd %.3f %.3f %.3f\nKs 0.050 0.050 0.050\nd 1.0' % (mname, bcf[0], bcf[1], bcf[2])
        if 'Color' in info: s += '\nmap_Kd ' + info['Color']
        for k in ('Normal', 'MetallicSmoothness'):
            if k in info: s += '\n# %s %s' % (k, info[k])
        mtl.append(s); maps[mname] = info
    # OBJ with welded v / vt / vn
    V, Tt, Nn = {}, {}, {}; vl, tl, nl = [], [], []
    def idx(tab, lst, key):
        i = tab.get(key)
        if i is None: i = len(lst) + 1; tab[key] = i; lst.append(key)
        return i
    body = []; tris = 0; wind_w = {}
    for mn, (P, UV, N, F) in parts.items():
        mname = short.get(mn, mn)
        body.append('g %s' % mname); body.append('usemtl %s' % mname)
        Pr = np.round(P, 4) + 0.0; Ur = np.round(UV, 4) + 0.0
        Nu = N / (np.linalg.norm(N, axis=1, keepdims=True) + 1e-12); Nr = np.round(Nu, 3) + 0.0
        # final winding check on the values actually written (rounding can tip near edge-on faces)
        gn = np.cross(Pr[F[:, 1]] - Pr[F[:, 0]], Pr[F[:, 2]] - Pr[F[:, 0]])
        fl = np.sum(gn * Nr[F].mean(1), axis=1) < 0
        if fl.any():
            F = F.copy(); F[fl] = F[fl][:, ::-1]; wind_w[mname] = int(fl.sum())
        for f in F:
            cs = []
            for k in f:
                cs.append('%d/%d/%d' % (idx(V, vl, tuple(Pr[k])), idx(Tt, tl, tuple(Ur[k])), idx(Nn, nl, tuple(Nr[k]))))
            body.append('f ' + ' '.join(cs)); tris += 1
    fmt = lambda x: ('%.4f' % x).rstrip('0').rstrip('.') if x != 0 else '0'
    fmt3 = lambda x: ('%.3f' % x).rstrip('0').rstrip('.') if x != 0 else '0'
    lines = ['# %s - InteriorModels pack. Units: metres, Y up, front +Z. Converted from Poly Haven glTF (CC0).' % name,
             'mtllib %s.mtl' % name]
    lines += ['v %s %s %s' % tuple(fmt(c) for c in v) for v in vl]
    lines += ['vt %s %s' % tuple(fmt(c) for c in t) for t in tl]
    lines += ['vn %s %s %s' % tuple(fmt3(c) for c in n) for n in nl]
    open(os.path.join(a.out, name + '.obj'), 'w').write('\n'.join(lines + body) + '\n')
    open(os.path.join(a.out, name + '.mtl'), 'w').write('\n\n'.join(mtl) + '\n')
    allP = np.vstack([p[0] for p in parts.values()])
    rep = {'tris': tris, 'size_m': np.round(allP.max(0) - allP.min(0), 3).tolist(), 'min': np.round(allP.min(0), 3).tolist(),
           'max': np.round(allP.max(0), 3).tolist(), 'pivot': piv, 'materials': maps, 'source_tris': total}
    if node_report: rep['nodes'] = node_report
    if any(wind.values()): rep['winding_flipped'] = {short.get(k, k): v for k, v in wind.items() if v}
    if wind_w: rep['winding_flipped_at_write'] = wind_w
    if spec.get('backfaces'): rep['backfaces'] = [short.get(k, k) for k in spec['backfaces']]
    print(name, json.dumps(rep))
    if spec.get('report'):
        r = json.load(open(spec['report'])) if os.path.exists(spec['report']) else {}
        r[name] = rep; json.dump(r, open(spec['report'], 'w'), indent=1)

main()
