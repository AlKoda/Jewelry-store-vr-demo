"""Assemble PottedPlant.obj/.mtl from Poly Haven potted_plant_01 (CC0) for a <= 6k-triangle street prop.
Inputs: SPLIT.gltf from plant_split.py (one node per leaf blade / leaf stalk, plus pot and stem) and SIMP.gltf from
node/simplify_adaptive.mjs (meshoptimizer 0.22, per-part budgets, seam-aware).
- leaf blades, pot, stem: meshoptimizer result; zero-area slivers, duplicate triangles and unused vertices removed
- leaf blades: made double-sided by duplicating each triangle with reversed winding and negated normals
  (the Poly Haven material is doubleSided; Unity's Standard shader culls back faces)
- leaf stalks (thin closed tubes that meshoptimizer cannot reduce without collapsing): rebuilt as 3-sided,
  2-segment tubes along the fitted centreline with the measured radius, UV-mapped onto each stalk's own texture strip
- the 508-pebble layer (56,878 tris) is replaced by a domed disc mapped onto the photographic top-down gravel patch
  that the pot atlas already contains (UV centre 0.1348, 0.8652, radius 0.112)
- pivot: centre of the pot base (the Poly Haven origin), y = 0 on the ground
usage: python3 -P plant_build.py SPLIT.gltf SIMP.gltf OUTDIR"""
import sys, os, json
import numpy as np
import trimesh

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(HERE)), 'tools'))  # models-street/tools (conv.py)
import gltfio
import conv

STALK_SIDES, STALK_SEGS = 3, 2


def clean(v, n, uv, f, min_area=1e-10):
    a = np.linalg.norm(np.cross(v[f[:, 1]] - v[f[:, 0]], v[f[:, 2]] - v[f[:, 0]]), axis=1) / 2
    keep = (a > min_area) & (f[:, 0] != f[:, 1]) & (f[:, 1] != f[:, 2]) & (f[:, 0] != f[:, 2])
    f = f[keep]
    s = np.sort(f, axis=1)
    _, first = np.unique(s, axis=0, return_index=True)
    f = f[np.sort(first)]
    used, inv = np.unique(f, return_inverse=True)
    return v[used], n[used], uv[used], inv.reshape(-1, 3), int((~keep).sum())


def mk(v, n, uv, f):
    m = trimesh.Trimesh(v, f, vertex_normals=n, process=False)
    m.visual = trimesh.visual.TextureVisuals(uv=uv)
    return m


def area(v, f):
    return float((np.linalg.norm(np.cross(v[f[:, 1]] - v[f[:, 0]], v[f[:, 2]] - v[f[:, 0]]), axis=1) / 2).sum())


def rebuild_stalk(p, sides=STALK_SIDES, segs=STALK_SEGS):
    v, uv = p['v'], p['uv']
    c0 = v.mean(0)
    _, _, vt = np.linalg.svd(v - c0, full_matrices=False)
    ax = vt[0]
    t = (v - c0) @ ax
    tmin, tmax = t.min(), t.max()
    # centreline: centroid of vertices in slabs along the main axis
    nb = segs + 1
    centres, radii = [], []
    for k in range(nb):
        tc = tmin + (tmax - tmin) * k / segs
        w = np.abs(t - tc) <= (tmax - tmin) / (2 * segs) + 1e-9
        if k in (0, segs):  # ends: use the outermost 12 % so the tube reaches the tip
            w = (t <= tmin + 0.12 * (tmax - tmin)) if k == 0 else (t >= tmax - 0.12 * (tmax - tmin))
        cc = v[w].mean(0)
        if k == 0:
            cc = cc + ax * (tmin - (cc - c0) @ ax)
        elif k == segs:
            cc = cc + ax * (tmax - (cc - c0) @ ax)
        centres.append(cc)
    centres = np.array(centres)
    # radius from the perpendicular distance of all vertices to the polyline (robust median)
    def dist_to_poly(P):
        best = np.full(len(P), np.inf)
        for a, b in zip(centres[:-1], centres[1:]):
            d = b - a; L = d @ d
            s = np.clip(((P - a) @ d) / max(L, 1e-12), 0, 1)
            best = np.minimum(best, np.linalg.norm(P - (a + s[:, None] * d), axis=1))
        return best
    r = float(np.median(dist_to_poly(v)))
    # UV strip: long axis of the strip runs along the stalk; short axis around it
    lo, hi = uv.min(0), uv.max(0)
    long_ax = int(np.argmax(hi - lo)); short_ax = 1 - long_ax
    corr = np.corrcoef(t, uv[:, long_ax])[0, 1]
    vs, ns, uvs, fs = [], [], [], []
    for k in range(nb):
        if k == 0:
            tang = centres[1] - centres[0]
        elif k == segs:
            tang = centres[-1] - centres[-2]
        else:
            tang = centres[k + 1] - centres[k - 1]
        tang /= np.linalg.norm(tang)
        ref = np.array([0.0, 1.0, 0.0]) if abs(tang[1]) < 0.9 else np.array([1.0, 0.0, 0.0])
        e1 = np.cross(tang, ref); e1 /= np.linalg.norm(e1)
        e2 = np.cross(tang, e1)
        # ends taper slightly (petiole tip / blade junction)
        rk = r * (0.75 if k == segs else 1.0)
        L = k / segs
        lv = lo[long_ax] + (hi[long_ax] - lo[long_ax]) * (L if corr >= 0 else 1 - L)
        for j in range(sides + 1):
            a = 2 * np.pi * j / sides
            d = np.cos(a) * e1 + np.sin(a) * e2
            vs.append(centres[k] + rk * d); ns.append(d)
            q = [0.0, 0.0]; q[long_ax] = lv; q[short_ax] = lo[short_ax] + (hi[short_ax] - lo[short_ax]) * j / sides
            uvs.append(q)
    R = sides + 1
    for k in range(segs):
        for j in range(sides):
            a, b, c, d = k * R + j, k * R + j + 1, (k + 1) * R + j, (k + 1) * R + j + 1
            fs += [[a, c, d], [a, d, b]]
    v2, n2, uv2, f2 = np.array(vs), np.array(ns), np.array(uvs), np.array(fs)
    m = trimesh.Trimesh(v2, f2, process=False)
    # outward winding: face normals must agree with the radial vertex normals
    fn = m.face_normals
    if np.mean(np.sum(fn * n2[f2].mean(1), axis=1)) < 0:
        f2 = f2[:, ::-1]
    return v2, n2, uv2, f2, r, float(np.linalg.norm(np.diff(centres, axis=0), axis=1).sum())


def gravel_disc(r=0.212, y_edge=0.500, y_mid=0.507, rings=2, seg=24, uc=(0.1348, 0.8652), ur=0.112):
    vs, uvs = [[0.0, y_mid, 0.0]], [list(uc)]
    for k in range(1, rings + 1):
        t = k / rings
        y = y_mid + (y_edge - y_mid) * t * t
        for j in range(seg):
            a = 2 * np.pi * j / seg
            vs.append([r * t * np.cos(a), y, r * t * np.sin(a)])
            uvs.append([uc[0] + ur * t * np.cos(a), uc[1] - ur * t * np.sin(a)])
    fs = [[0, 1 + (j + 1) % seg, 1 + j] for j in range(seg)]
    for k in range(1, rings):
        o0, o1 = 1 + (k - 1) * seg, 1 + k * seg
        for j in range(seg):
            a, b = o0 + j, o0 + (j + 1) % seg
            c, d = o1 + j, o1 + (j + 1) % seg
            fs += [[a, b, d], [a, d, c]]
    v = np.array(vs); f = np.array(fs)
    m = trimesh.Trimesh(v, f, process=False)
    if m.face_normals[:, 1].mean() < 0:
        f = f[:, ::-1]
        m = trimesh.Trimesh(v, f, process=False)
    return v, np.array(m.vertex_normals), np.array(uvs), f


if __name__ == '__main__':
    split, simp, out = sys.argv[1], sys.argv[2], sys.argv[3]
    S = {p['name']: p for p in gltfio.read(split)}
    blades, pot, stalks = [], [], []
    removed = 0
    rep = dict(blade_area_ratio=[], stalk_radius_mm=[], stalk_length_cm=[])
    for p in gltfio.read(simp):
        if p['name'].startswith('stalk_'):
            continue
        v, n, uv, f, r = clean(p['v'], p['n'], p['uv'], p['f'])
        removed += r
        if p['name'].startswith('blade_'):
            o = S[p['name']]
            rep['blade_area_ratio'].append(area(v, f) / area(o['v'], o['f']))
            blades.append(mk(v, n, uv, f))
        else:
            pot.append(mk(v, n, uv, f))
    for name, p in S.items():
        if name.startswith('stalk_'):
            v, n, uv, f, r, L = rebuild_stalk(p)
            rep['stalk_radius_mm'].append(r * 1000); rep['stalk_length_cm'].append(L * 100)
            stalks.append(mk(v, n, uv, f))
    front = conv.concat(blades)
    fv, fn, fu, ff = np.asarray(front.vertices), np.asarray(front.vertex_normals), np.asarray(front.visual.uv), np.asarray(front.faces)
    both = mk(np.vstack([fv, fv]), np.vstack([fn, -fn]), np.vstack([fu, fu]), np.vstack([ff, ff[:, ::-1] + len(fv)]))
    leaves = conv.concat([both] + stalks)
    pot.append(mk(*gravel_disc()))
    potm = conv.concat(pot)
    groups = [('Pot', 'Pot', potm), ('Leaves', 'Leaves', leaves)]
    res = conv.write_obj(out, 'PottedPlant', groups, ground=True, center=False)
    open(os.path.join(out, 'PottedPlant.mtl'), 'w').write(
        'newmtl Pot\nKd 1.0000 1.0000 1.0000\nKs 0.04 0.04 0.04\nd 1.000\nmap_Kd PottedPlant_Pot_Color.jpg\n\n'
        'newmtl Leaves\nKd 1.0000 1.0000 1.0000\nKs 0.04 0.04 0.04\nd 1.000\nmap_Kd PottedPlant_Leaves_Color.jpg\n')
    info = dict(tris=res['tris'], size_m=res['size_m'], min_y=res['min_y'], slivers_removed=removed,
                pot_stem_disc=len(potm.faces), blades_one_side=len(ff), blades_two_sided=len(both.faces),
                stalks=sum(len(s.faces) for s in stalks), blade_count=len(blades), stalk_count=len(stalks),
                blade_area_ratio_min=round(min(rep['blade_area_ratio']), 3),
                stalk_radius_mm=[round(float(np.min(rep['stalk_radius_mm'])), 2), round(float(np.max(rep['stalk_radius_mm'])), 2)],
                stalk_length_cm=[round(float(np.min(rep['stalk_length_cm'])), 1), round(float(np.max(rep['stalk_length_cm'])), 1)])
    json.dump(info, open(os.path.join(out, 'plant_build.json'), 'w'), indent=1)
    print(json.dumps(info))
