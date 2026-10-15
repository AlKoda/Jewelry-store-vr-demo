"""Replace a dense pebble/gravel layer inside a pot by a low-poly heightfield disc with baked textures
(InteriorModels pack, project code). Used by convert_ph.py for the Poly Haven potted plants.

bake(P, UV, N, F, tex, shell, opts) -> dict with
  part   : (P, UV, N, F) of the disc (front faces +Y, CCW seen from above, normals from the coarse surface)
  images : {'Color': PIL RGB, 'Normal': PIL RGB (tangent space, OpenGL +Y), 'MS': PIL RGBA (R=G=B metal 0, A smoothness)}
  drop   : boolean mask over the shell faces that lie inside the sealed cavity below the disc (never visible)
  info   : numbers for the report

Method: the pebble mesh is rasterised top-down (orthographic, 1024 px over the pot opening). Per pixel the top
surface keeps its height, its interpolated UV and its shading normal; the source colour, normal (OpenGL) and
roughness maps are sampled through the UV and the normal map is applied in each triangle's own tangent frame.
A blurred copy of the height field is the coarse surface: a polar grid disc follows it, its edge sits in the wall's
thickness (per segment 1.5 mm past the inner wall found by a ray from the pot axis, plus the chord sag), and the baked normals are re-expressed in the coarse surface's tangent frame (u = +X, v = -Z).
Crevices between pebbles get a cavity darkening from the local height difference."""
import numpy as np
from PIL import Image, ImageFilter


def _blur(a, sigma_px):
    """Gaussian blur of a float array via separable numpy convolution (edge-padded)."""
    r = int(3 * sigma_px) + 1
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma_px) ** 2); k /= k.sum()
    p = np.pad(a, r, mode='edge')
    p = np.apply_along_axis(lambda m: np.convolve(m, k, mode='valid'), 0, p)
    p = np.apply_along_axis(lambda m: np.convolve(m, k, mode='valid'), 1, p)
    return p


def _sample(img, uv):
    """Bilinear sample of an HxWxC float image at OBJ-convention UVs (v up; trimesh flips glTF v on load)."""
    H, W = img.shape[:2]
    x = np.clip(uv[:, 0] * W - 0.5, 0, W - 1.001); y = np.clip((1 - uv[:, 1]) * H - 0.5, 0, H - 1.001)
    x0 = np.floor(x).astype(int); y0 = np.floor(y).astype(int); fx = (x - x0)[:, None]; fy = (y - y0)[:, None]
    a = img[y0, x0]; b = img[y0, x0 + 1]; c = img[y0 + 1, x0]; d = img[y0 + 1, x0 + 1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def _ray(o, d, T):
    """Distances along the ray o + t d (t > 0) to every crossing with the triangles T (n x 3 x 3), Moller-Trumbore."""
    e1 = T[:, 1] - T[:, 0]; e2 = T[:, 2] - T[:, 0]; p = np.cross(d, e2); det = np.sum(e1 * p, 1)
    ok = np.abs(det) > 1e-14; inv = np.where(ok, 1 / np.where(ok, det, 1), 0); s = o - T[:, 0]
    u = np.sum(s * p, 1) * inv; q = np.cross(s, e1); v = (q @ d) * inv; t = np.sum(e2 * q, 1) * inv
    m = ok & (u >= -1e-7) & (v >= -1e-7) & (u + v <= 1 + 1e-7) & (t > 1e-6)   # tolerant: rays through shared edges
    t = np.sort(t[m])
    return t[np.r_[True, np.diff(t) > 1e-5]] if len(t) else t          # one crossing per shared edge


def bake(P, UV, N, F, tex, shell, opts):
    res = opts.get('bake_px', 1024)
    rings, segs = opts.get('rings', 7), opts.get('segs', 40)
    SP, SF, SN = shell
    # ---- pot axis and inner wall profile from the shell
    lo, hi = SP.min(0), SP.max(0); cx, cz = (lo[0] + hi[0]) / 2, (lo[2] + hi[2]) / 2
    tm_c = SP[SF].mean(1); fn = np.cross(SP[SF[:, 1]] - SP[SF[:, 0]], SP[SF[:, 2]] - SP[SF[:, 0]])
    fn /= np.linalg.norm(fn, axis=1, keepdims=True) + 1e-15
    rad = tm_c[:, [0, 2]] - [cx, cz]; rr = np.linalg.norm(rad, axis=1); rad /= rr[:, None] + 1e-12
    outward = np.sum(fn[:, [0, 2]] * rad, 1)
    import trimesh
    cc = trimesh.graph.connected_components(trimesh.Trimesh(SP, SF, process=False).face_adjacency, nodes=np.arange(len(SF)), min_len=1)
    inner = np.zeros(len(SF), bool)
    wall = np.zeros(len(SF), bool)
    comp_info = []
    for c in cc:
        o, u = outward[c].mean(), fn[c, 1].mean()
        comp_info.append((c, o, u))
        if o < -0.5 and u > -0.2: wall[c] = True
    ycav = SP[np.unique(SF[wall])][:, 1].min()
    rwall = np.linalg.norm(SP[np.unique(SF[wall])][:, [0, 2]] - [cx, cz], axis=1)
    for c, o, u in comp_info:
        if wall[c].all(): inner[c] = True
        elif u > 0.9 and SP[np.unique(SF[c])][:, 1].min() >= ycav - 0.005 and rr[c].mean() < rwall.max():
            inner[c] = True
    # inner wall profile: vertices of every face that faces the axis inside the cavity (per face, so the upper
    # inner wall is found even when it belongs to the rim's mesh component)
    fin = (outward < -0.3) & (tm_c[:, 1] > ycav - 0.002)
    wv = SP[np.unique(SF[fin])]; wy = wv[:, 1]; wr = np.linalg.norm(wv[:, [0, 2]] - [cx, cz], axis=1)
    ytop_in = float(wy.max())
    def r_in(y):
        y = np.atleast_1d(np.asarray(y, float)); out = np.empty(len(y))
        for k, yy in enumerate(y):
            m = np.abs(wy - yy) < 0.006
            out[k] = wr[m].max() if m.any() else wr[np.argmin(np.abs(wy - yy))]
        return out
    # disc edge radius: outside the polygonal inner wall everywhere (vertex radius, chord sag of the disc's own
    # segments) plus 1.5 mm, i.e. inside the wall's thickness
    edge_r = lambda y: r_in(y) / np.cos(np.pi / segs) + 0.0015
    ymean = np.median(P[:, 1])
    R = float(edge_r(np.linspace(P[:, 1].min(), min(P[:, 1].max(), ytop_in), 12)).max()) + 0.002   # bake half-size
    px = 2 * R / res
    Hh = np.full((res, res), -np.inf, np.float32); Tid = np.full((res, res), -1, np.int64)
    Bc = np.zeros((res, res, 3), np.float32)
    X = (P[:, 0] - (cx - R)) / px - 0.5; Z = (P[:, 2] - (cz - R)) / px - 0.5   # pixel coords (col, row)
    for t, (i0, i1, i2) in enumerate(F):
        xs = np.array([X[i0], X[i1], X[i2]]); zs = np.array([Z[i0], Z[i1], Z[i2]])
        c0, c1 = int(np.floor(xs.min())), int(np.ceil(xs.max())); r0, r1 = int(np.floor(zs.min())), int(np.ceil(zs.max()))
        c0, r0 = max(c0, 0), max(r0, 0); c1, r1 = min(c1, res - 1), min(r1, res - 1)
        if c1 < c0 or r1 < r0: continue
        gx, gz = np.meshgrid(np.arange(c0, c1 + 1), np.arange(r0, r1 + 1))
        d = (zs[1] - zs[2]) * (xs[0] - xs[2]) + (xs[2] - xs[1]) * (zs[0] - zs[2])
        if abs(d) < 1e-12: continue
        b0 = ((zs[1] - zs[2]) * (gx - xs[2]) + (xs[2] - xs[1]) * (gz - zs[2])) / d
        b1 = ((zs[2] - zs[0]) * (gx - xs[2]) + (xs[0] - xs[2]) * (gz - zs[2])) / d
        b2 = 1 - b0 - b1
        m = (b0 >= -1e-6) & (b1 >= -1e-6) & (b2 >= -1e-6)
        if not m.any(): continue
        y = b0 * P[i0, 1] + b1 * P[i1, 1] + b2 * P[i2, 1]
        rows, cols = gz[m], gx[m]; yv = y[m]
        upd = yv > Hh[rows, cols]
        rows, cols, yv = rows[upd], cols[upd], yv[upd]
        Hh[rows, cols] = yv; Tid[rows, cols] = t
        Bc[rows, cols] = np.stack([b0[m][upd], b1[m][upd], b2[m][upd]], 1)
    cov = Tid >= 0
    # ---- per-pixel attributes
    ids = Tid[cov]; bc = Bc[cov]; Ft = F[ids]
    uv = np.einsum('nk,nkj->nj', bc, UV[Ft]); nrm = np.einsum('nk,nkj->nj', bc, N[Ft])
    nrm /= np.linalg.norm(nrm, axis=1, keepdims=True) + 1e-12
    p0, p1, p2 = P[Ft[:, 0]], P[Ft[:, 1]], P[Ft[:, 2]]; t0, t1, t2 = UV[Ft[:, 0]], UV[Ft[:, 1]], UV[Ft[:, 2]]
    e1, e2 = p1 - p0, p2 - p0; d1, d2 = t1 - t0, t2 - t0
    det = d1[:, 0] * d2[:, 1] - d2[:, 0] * d1[:, 1]; det = np.where(np.abs(det) < 1e-12, 1e-12, det)
    Tg = (e1 * d2[:, 1:2] - e2 * d1[:, 1:2]) / det[:, None]; Bg = (e2 * d1[:, 0:1] - e1 * d2[:, 0:1]) / det[:, None]
    # UVs are v-up here, so dP/dv is the OpenGL normal map's green axis directly
    Tg -= nrm * np.sum(Tg * nrm, 1, keepdims=True); Tg /= np.linalg.norm(Tg, axis=1, keepdims=True) + 1e-12
    sgn = np.sign(np.sum(np.cross(nrm, Tg) * Bg, 1)); sgn[sgn == 0] = 1
    Bn = np.cross(nrm, Tg) * sgn[:, None]
    col = np.asarray(Image.open(tex['color']).convert('RGB'), np.float32) / 255
    nmap = np.asarray(Image.open(tex['normal']).convert('RGB'), np.float32) / 255 * 2 - 1
    rough = np.asarray(Image.open(tex['rough']).convert('RGB'), np.float32)[..., 1:2] / 255
    uvs = uv.copy()
    alb = _sample(col, uvs); nt = _sample(nmap, uvs); ro = _sample(rough, uvs)[:, 0]
    nw = Tg * nt[:, 0:1] + Bn * nt[:, 1:2] + nrm * nt[:, 2:3]; nw /= np.linalg.norm(nw, axis=1, keepdims=True) + 1e-12
    ALB = np.zeros((res, res, 3), np.float32); NW = np.zeros((res, res, 3), np.float32); NW[..., 1] = 1
    RO = np.full((res, res), 0.9, np.float32)
    ALB[cov] = alb; NW[cov] = nw; RO[cov] = ro
    # ---- fill the uncovered gaps (deep soil between and under the stones)
    hmin = float(Hh[cov].min()) if cov.any() else float(P[:, 1].min())
    floor_h = float(np.percentile(Hh[cov], 2)) - 0.004
    H = np.where(cov, Hh, floor_h).astype(np.float32)
    soil = np.median(alb, 0) * 0.32
    ALB[~cov] = soil
    # ---- coarse surface and cavity darkening
    Hb = _blur(H, 0.008 / px)                                   # 8 mm: the disc follows the mound, not single stones
    Hs = _blur(H, 0.0025 / px)
    cav = np.clip((Hs - H) / 0.004, 0, 1)
    ao = 1 - 0.35 * cav
    ao *= np.where(cov, 1.0, 0.85)
    ALB *= ao[..., None]
    gz_, gx_ = np.gradient(Hb, px)
    Nc = np.dstack([-gx_, np.ones_like(Hb), -gz_]); Nc /= np.linalg.norm(Nc, axis=2, keepdims=True)
    Tc = np.zeros_like(Nc); Tc[..., 0] = 1
    Tc -= Nc * np.sum(Tc * Nc, 2, keepdims=True); Tc /= np.linalg.norm(Tc, axis=2, keepdims=True)
    Bcf = np.cross(Nc, Tc)                                      # = -Z on a flat disc (= dP/dv)
    nts = np.dstack([np.sum(NW * Tc, 2), np.sum(NW * Bcf, 2), np.sum(NW * Nc, 2)])
    nts[..., 2] = np.maximum(nts[..., 2], 0.05); nts /= np.linalg.norm(nts, axis=2, keepdims=True)
    # ---- polar grid disc
    def hb_at(x, z):
        c = np.clip((x - (cx - R)) / px - 0.5, 0, res - 1.001); r = np.clip((z - (cz - R)) / px - 0.5, 0, res - 1.001)
        c0 = np.floor(c).astype(int); r0 = np.floor(r).astype(int); fx = c - c0; fz = r - r0
        v = lambda a: (a[r0, c0] * (1 - fx) + a[r0, c0 + 1] * fx) * (1 - fz) + (a[r0 + 1, c0] * (1 - fx) + a[r0 + 1, c0 + 1] * fx) * fz
        return v(Hb), np.stack([v(Nc[..., k]) for k in range(3)], 1)
    verts = [(cx, cz)]
    for i in range(1, rings + 1):
        for s in range(segs):
            a = 2 * np.pi * s / segs
            verts.append((i, a))
    VP = []; ring_idx = []
    y0, n0 = hb_at(np.array([cx]), np.array([cz])); VP.append([cx, y0[0], cz])
    for i in range(1, rings + 1):
        a = 2 * np.pi * np.arange(segs) / segs
        frac = i / rings
        if i < rings:
            r = frac * r_in(ymean)[0]
            x, z = cx + r * np.cos(a), cz + r * np.sin(a)
            y, _ = hb_at(x, z)
        else:
            # edge: inside the wall's thickness at the local gravel height, never above the inner rim (iterate on
            # the height). Per segment a ray from the axis finds the inner wall (first crossing) and the outer wall
            # (second); the vertex goes 1.5 mm past the inner wall plus the chord sag of the disc's own segments,
            # and never further than halfway into the wall.
            y = np.full(segs, ymean); dirs = np.stack([np.cos(a), np.zeros(segs), np.sin(a)], 1); ST = SP[SF]
            for _ in range(3):
                r = np.empty(segs)
                for s_ in range(segs):
                    t = np.sort(_ray(np.array([cx, y[s_], cz]), dirs[s_], ST))
                    r_wall = t[0] if len(t) else float(edge_r(y[s_:s_ + 1])[0])
                    r_out = t[1] if len(t) > 1 else r_wall + 0.01
                    r[s_] = min(r_wall / np.cos(np.pi / segs) + 0.0015, 0.5 * (r_wall + r_out))
                x, z = cx + r * np.cos(a), cz + r * np.sin(a)
                y, _ = hb_at(x, z); y = np.minimum(y, ytop_in - 0.003)
        ring_idx.append(len(VP)); VP += np.stack([x, y, z], 1).tolist()
    VP = np.array(VP)
    _, VN = hb_at(VP[:, 0], VP[:, 2]); VN /= np.linalg.norm(VN, axis=1, keepdims=True)
    VUV = np.stack([(VP[:, 0] - (cx - R)) / (2 * R), 1 - (VP[:, 2] - (cz - R)) / (2 * R)], 1)
    FF = []
    for s in range(segs):
        a, b = ring_idx[0] + s, ring_idx[0] + (s + 1) % segs
        FF.append([0, b, a])
    for i in range(rings - 1):
        o0, o1 = ring_idx[i], ring_idx[i + 1]
        for s in range(segs):
            a0, b0 = o0 + s, o0 + (s + 1) % segs; a1, b1 = o1 + s, o1 + (s + 1) % segs
            FF += [[a0, b0, b1], [a0, b1, a1]]
    FF = np.array(FF)
    gn = np.cross(VP[FF[:, 1]] - VP[FF[:, 0]], VP[FF[:, 2]] - VP[FF[:, 0]])
    flip = gn[:, 1] < 0; FF[flip] = FF[flip][:, ::-1]
    # ---- hidden shell faces: inside the cavity and entirely below the disc
    dmin = VP[:, 1].min() - 0.003
    drop = inner & (SP[SF][:, :, 1].max(1) < dmin)
    # ---- images (bake res -> output res; normals renormalised after resizing)
    cpx, npx, mpx = opts.get('color_px', 512), opts.get('normal_px', 512), opts.get('ms_px', 256)
    im_c = Image.fromarray((np.clip(ALB, 0, 1) ** 1 * 255).astype(np.uint8)).resize((cpx, cpx), Image.LANCZOS)
    nn = np.asarray(Image.fromarray(((nts * 0.5 + 0.5) * 255).clip(0, 255).astype(np.uint8)).resize((npx, npx), Image.LANCZOS), np.float32) / 255 * 2 - 1
    nn /= np.linalg.norm(nn, axis=2, keepdims=True) + 1e-12
    im_n = Image.fromarray(((nn * 0.5 + 0.5) * 255).clip(0, 255).astype(np.uint8))
    sm = Image.fromarray(((1 - RO) * 255).clip(0, 255).astype(np.uint8)).resize((mpx, mpx), Image.LANCZOS)
    z8 = Image.new('L', (mpx, mpx), 0)
    im_m = Image.merge('RGBA', (z8, z8, z8, sm))
    info = {'pebble_tris': int(len(F)), 'disc_tris': int(len(FF)), 'coverage': round(float(cov.mean()), 3),
            'bake_half_size_m': round(R, 4), 'disc_y_range': [round(float(VP[:, 1].min()), 4), round(float(VP[:, 1].max()), 4)],
            'edge_radius_m': [round(float(np.linalg.norm(VP[ring_idx[-1]:, [0, 2]] - [cx, cz], axis=1).min()), 4),
                              round(float(np.linalg.norm(VP[ring_idx[-1]:, [0, 2]] - [cx, cz], axis=1).max()), 4)],
            'inner_wall_radius_at_edge_m': [round(float(r_in(VP[ring_idx[-1]:, 1]).min()), 4), round(float(r_in(VP[ring_idx[-1]:, 1]).max()), 4)],
            'shell_faces_dropped': int(drop.sum()), 'smoothness_mean': round(float(1 - RO.mean()), 3)}
    return {'part': (VP, VUV, VN, FF), 'images': {'Color': im_c, 'Normal': im_n, 'MS': im_m}, 'drop': drop, 'info': info,
            'debug': {'H': H, 'cov': cov}}
