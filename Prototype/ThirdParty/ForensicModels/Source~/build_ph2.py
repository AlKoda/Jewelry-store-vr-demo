"""Convert the October 24, 2026 batch of Poly Haven glTFs to the pack format (second batch; build_ph.py made the first).
build_ph2.py SRC_ROOT PACK_DIR GLB_TO_OBJ NAME [NAME...]

SRC_ROOT/<Name>/ holds a copy of the mirror folder: <Name>.gltf (the Poly Haven glTF renamed), <slug>.bin and
textures/<slug>_{diff,nor_gl,arm}_{1k,2k}.jpg. Colour maps that relabel_ph2.py rewrote are used as they are found there.

Per model:
  - whole models: Tools/glb_to_obj.py (--ground, optional --scale), then Source~/decimate_exact.py when a target is set;
  - extracted models (CigaretteButt, ReagentBottles): the chosen glTF nodes are read with trimesh (node transforms
    applied) and written with the same OBJ layout as glb_to_obj.py (v/vt per corner, one g/usemtl per node);
  - OBJ numbers rounded as assemble.py does (v/vt 4 decimals = 0.1 mm), header comment normalised;
  - base colour -> <Name>_Color.jpg (q90), nor_gl -> <Name>_Normal.jpg (q90), ARM (R=AO, G=rough, B=metal) ->
    <Name>_MetallicSmoothness.png (R = metal, G = B = 0, A = 1 - rough);
  - glass sub-materials written as MTL constants (Kd, d), as the first batch did.
Writes SRC_ROOT/_work/stats_ph2.json (tris, size, materials, maps) for write_sources_ph2.py.
"""
import sys, os, json, glob, shutil, subprocess
sys.path.append(os.path.expanduser('~/.local/lib/python3.13/site-packages'))
import numpy as np
from PIL import Image
import trimesh

GLASS_CLEAR = ((0.85, 0.92, 0.9), 0.25)
SPEC = {
    # name: dict(scale, tris (0 = keep), cpx, npx, mpx, glass {mtl: (Kd, d)}, nodes (None = all), hook)
    'LiftingTapeRoll':     dict(scale=2.0, tris=0,    cpx=512,  npx=512,  mpx=256, glass={}, hook='tape'),
    'ReagentSprayBottle':  dict(scale=1.0, tris=0,    cpx=1024, npx=512,  mpx=512, glass={}),
    'DentalStoneBag':      dict(scale=1.0, tris=0,    cpx=1024, npx=512,  mpx=512, glass={}),
    'EvidenceBox':         dict(scale=1.0, tris=6000, cpx=1024, npx=1024, mpx=512, glass={}),
    'BurglarToolbox':      dict(scale=1.0, tris=9000, cpx=1024, npx=1024, mpx=512, glass={}),
    'BaseballBat':         dict(scale=1.0, tris=0,    cpx=1024, npx=512,  mpx=256, glass={}),
    'PhillipsScrewdriver': dict(scale=1.0, tris=0,    cpx=512,  npx=512,  mpx=256, glass={}),
    'Lighter':             dict(scale=1.0, tris=0,    cpx=512,  npx=512,  mpx=256, glass={}),
    'Spectacles':          dict(scale=1.0, tris=9000, cpx=512,  npx=512,  mpx=256,
                                glass={'round_spectacles_glass': ((0.9, 0.94, 0.95), 0.2)}),
    'DroppedWatch':        dict(scale=1.0, tris=9000, cpx=512,  npx=512,  mpx=256,
                                glass={'digital_wrist_watch_glass': ((0.8, 0.86, 0.9), 0.3)}),
    'CigaretteButt':       dict(scale=1.0, tris=0,    cpx=512,  npx=256,  mpx=256, glass={},
                                nodes=['cigarette_case_cigarette_c'], hook='cigarette'),
    'ReagentBottles':      dict(scale=1.0, tris=2900, cpx=512,  npx=512,  mpx=256,
                                glass={'chemistry_set_glass': ((0.86, 0.92, 0.9), 0.3)},
                                nodes=['chemistry_set_bottle_clear_a', 'chemistry_set_bottle_clear_lid',
                                       'chemistry_set_bottle_clear_b', 'chemistry_set_bottle_clear_b_cork',
                                       'chemistry_set_bottle_clear_d', 'chemistry_set_bottle_clear_d_cork']),
}
HERE = os.path.dirname(os.path.abspath(__file__))


def fmt(tok, nd):
    v = round(float(tok), nd)
    s = ('%.' + str(nd) + 'f') % v
    s = s.rstrip('0').rstrip('.') if '.' in s else s
    return '0' if s in ('-0', '') else s


def safe(text):
    return ''.join(c if c.isalnum() else '_' for c in str(text)) or 'mat'


def find(folder, key):
    hits = sorted(glob.glob(os.path.join(folder, 'textures', '*%s*' % key)))
    return hits[0] if hits else None


# ---------------------------------------------------------------- OBJ in memory: groups of (name, mtl, V, T, F)
def read_obj(path):
    V, T, groups, head, cur = [], [], [], [], None
    for line in open(path):
        p = line.split()
        if not p:
            continue
        if p[0] == 'v': V.append([float(x) for x in p[1:4]])
        elif p[0] == 'vt': T.append([float(x) for x in p[1:3]])
        elif p[0] == 'g': cur = [p[1], None, []]; groups.append(cur)
        elif p[0] == 'usemtl': cur[1] = p[1]
        elif p[0] == 'f':
            cur[2].append([(int(c.split('/')[0]) - 1, int(c.split('/')[1]) - 1) for c in p[1:]])
    V, T = np.array(V), np.array(T)
    out = []
    for name, mtl, faces in groups:   # re-index per group with corners split per (v, vt), like decimate_exact
        keys, pts, uvs, F = {}, [], [], []
        for f in faces:
            tri = []
            for vi, ti in f:
                k = (vi, ti)
                if k not in keys:
                    keys[k] = len(pts); pts.append(V[vi]); uvs.append(T[ti])
                tri.append(keys[k])
            F.append(tri)
        out.append([name, mtl, np.array(pts), np.array(uvs), np.array(F, np.int64)])
    return out


def write_obj(groups, path, name, origin):
    lines = ['# %s - ForensicModels pack. Units: metres, Y up. %s' % (name, origin), 'mtllib %s.mtl' % name]
    base = 0
    body = []
    for gname, mtl, P, UV, F in groups:
        lines += ['v ' + ' '.join(fmt(x, 4) for x in p) for p in P]
        lines += ['vt ' + ' '.join(fmt(x, 4) for x in t) for t in UV]
        body.append('g ' + gname); body.append('usemtl ' + mtl)
        body += ['f %d/%d %d/%d %d/%d' % (a + 1 + base, a + 1 + base, b + 1 + base, b + 1 + base, c + 1 + base, c + 1 + base)
                 for a, b, c in F]
        base += len(P)
    open(path, 'w').write('\n'.join(lines + body) + '\n')


def ground(groups):
    allp = np.vstack([g[2] for g in groups])
    off = np.array([(allp[:, 0].min() + allp[:, 0].max()) / 2, allp[:, 1].min(), (allp[:, 2].min() + allp[:, 2].max()) / 2])
    for g in groups:
        g[2] = g[2] - off
    return groups


# ---------------------------------------------------------------- extraction of glTF nodes (trimesh)
def extract_nodes(gltf, wanted):
    sc = trimesh.load(gltf, force='scene')
    groups = []
    for node in sc.graph.nodes_geometry:
        if node not in wanted:
            continue
        T, gname = sc.graph[node]
        m = sc.geometry[gname].copy(); m.apply_transform(T)
        mat = getattr(m.visual, 'material', None)
        groups.append([safe(node), safe(getattr(mat, 'name', None) or 'mat'), np.asarray(m.vertices, float),
                       np.asarray(m.visual.uv, float), np.asarray(m.faces, np.int64)])
    missing = [w for w in wanted if safe(w) not in [g[0] for g in groups]]
    assert not missing, missing
    groups.sort(key=lambda g: wanted.index([w for w in wanted if safe(w) == g[0]][0]))
    return groups


# ---------------------------------------------------------------- hooks
def hook_tape(groups, src_folder):
    """Split the roll into tape and core by radius about the Y axis, so the tape can get its own (translucent) material."""
    assert len(groups) == 1
    gname, mtl, P, UV, F = groups[0]
    r = np.sqrt(P[:, 0] ** 2 + P[:, 2] ** 2)
    fr = r[F].mean(1)
    rs = np.sort(np.unique(np.round(r, 4)))
    gaps = np.diff(rs); cut = (rs[gaps.argmax()] + rs[gaps.argmax() + 1]) / 2
    core = fr < cut
    # side faces (annulus) span both radii: a face is core only if all three corners are inside the cut
    core = (r[F] < cut).all(1)
    print('  tape/core radius cut %.4f m: tape faces %d, core faces %d' % (cut, (~core).sum(), core.sum()))
    tape = [gname + '_tape', 'lifting_tape', P, UV, F[~core]]
    corg = [gname + '_core', 'tape_core', P, UV, F[core]]
    out = []
    for g in (tape, corg):   # drop unused vertices per group
        used = np.unique(g[4]); remap = -np.ones(len(g[2]), np.int64); remap[used] = np.arange(len(used))
        out.append([g[0], g[1], g[2][used], g[3][used], remap[g[4]]])
    return out


def clip_triangles(P, UV, F, axis, limit, keep_below):
    """Clip triangles against the plane coord[axis] = limit (Sutherland-Hodgman per triangle), UVs interpolated."""
    newP, newUV, newF = [], [], []
    def add(p, uv):
        newP.append(p); newUV.append(uv); return len(newP) - 1
    for tri in F:
        pts = [(P[i], UV[i]) for i in tri]
        inside = lambda p: (p[axis] <= limit) if keep_below else (p[axis] >= limit)
        poly = []
        for i in range(3):
            a, b = pts[i], pts[(i + 1) % 3]
            if inside(a[0]): poly.append(a)
            if inside(a[0]) != inside(b[0]):
                t = (limit - a[0][axis]) / (b[0][axis] - a[0][axis])
                poly.append((a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1])))
        if len(poly) < 3:
            continue
        idx = [add(p, uv) for p, uv in poly]
        for k in range(1, len(idx) - 1):
            newF.append([idx[0], idx[k], idx[k + 1]])
    P2, UV2, F2 = np.array(newP), np.array(newUV), np.array(newF, np.int64)
    # merge duplicate corners
    key = np.round(np.hstack([P2, UV2]), 6)
    _, first, inv = np.unique(key, axis=0, return_index=True, return_inverse=True)
    return P2[first], UV2[first], inv.reshape(-1)[F2]


def hook_cigarette(groups, src_folder):
    """One loose cigarette (86 mm, along Z) -> a 25 mm butt: cut past the filter, cap the cut, crush the paper end."""
    assert len(groups) == 1
    gname, mtl, P, UV, F = groups[0]
    z0, z1 = P[:, 2].min(), P[:, 2].max()
    # which end is the filter: sample the colour map at the UVs of the faces nearest each end
    diff = np.asarray(Image.open(find(src_folder, '_diff_')).convert('RGB'), np.float32)
    h, w = diff.shape[:2]
    def end_colour(zend):
        sel = np.abs(P[F].mean(1)[:, 2] - zend) < 0.004
        uv = UV[F[sel]].reshape(-1, 2)
        px = diff[np.clip(((1 - uv[:, 1]) * h).astype(int), 0, h - 1), np.clip((uv[:, 0] * w).astype(int), 0, w - 1)]
        return px.mean(0)
    c0, c1 = end_colour(z0), end_colour(z1)
    print('  end colours z0 %s z1 %s' % (np.round(c0), np.round(c1)))
    # the filter is the tan (cork-pattern) end; the cut tobacco at the other end is much darker
    filter_at_z0 = c0.sum() > c1.sum()
    L = 0.025
    if filter_at_z0:
        limit = z0 + L; P2, UV2, F2 = clip_triangles(P, UV, F, 2, limit, keep_below=True)
    else:
        limit = z1 - L; P2, UV2, F2 = clip_triangles(P, UV, F, 2, limit, keep_below=False)
    # cap: boundary loop at the cut plane, fan from its centre; UV = the original tobacco end (the cut end's own end face)
    end_face_uv = UV[F[np.abs(P[F].mean(1)[:, 2] - (z1 if filter_at_z0 else z0)) < 0.002]].reshape(-1, 2).mean(0)
    on = np.abs(P2[:, 2] - limit) < 1e-6
    ring = np.where(on)[0]
    if len(ring) >= 3:
        centre = P2[ring].mean(0)
        ang = np.arctan2(P2[ring, 1] - centre[1], P2[ring, 0] - centre[0])
        ring = ring[np.argsort(ang)]
        ci = len(P2); P2 = np.vstack([P2, centre]); UV2 = np.vstack([UV2, end_face_uv])
        cap = []
        for k in range(len(ring)):
            a, b = ring[k], ring[(k + 1) % len(ring)]
            cap.append([ci, b, a] if filter_at_z0 else [ci, a, b])
        F2 = np.vstack([F2, np.array(cap, np.int64)])
    # crush: beyond the filter (last 40 % of the butt) flatten the paper end and bend it sideways a little
    zf = z0 if filter_at_z0 else z1
    t = np.clip((np.abs(P2[:, 2] - zf) / L - 0.6) / 0.4, 0, 1)
    s = t * t * (3 - 2 * t)
    cy = P2[:, 1].mean()
    P2[:, 1] = cy + (P2[:, 1] - cy) * (1 - 0.45 * s)
    P2[:, 0] += 0.0025 * s * s
    # drop degenerate triangles
    a, b, c = P2[F2[:, 0]], P2[F2[:, 1]], P2[F2[:, 2]]
    area = np.linalg.norm(np.cross(b - a, c - a), axis=1) / 2
    F2 = F2[area > 1e-12]
    print('  cigarette %.1f mm -> butt %.1f mm, %d tris' % ((z1 - z0) * 1000, (P2[:, 2].max() - P2[:, 2].min()) * 1000, len(F2)))
    return [[gname, mtl, P2, UV2, F2]]


HOOKS = {'tape': hook_tape, 'cigarette': hook_cigarette}


# ---------------------------------------------------------------- main
def main():
    src_root, out, tool = sys.argv[1:4]
    os.makedirs(out, exist_ok=True)
    work = os.path.join(src_root, '_work'); os.makedirs(work, exist_ok=True)
    rp = os.path.join(work, 'stats_ph2.json')
    stats = json.load(open(rp)) if os.path.exists(rp) else {}
    for name in sys.argv[4:]:
        spec = SPEC[name]
        folder = os.path.join(src_root, name)
        gltf = os.path.join(folder, name + '.gltf')
        tout = os.path.join(work, name); shutil.rmtree(tout, ignore_errors=True); os.makedirs(tout)
        print(name)
        if spec.get('nodes'):
            groups = extract_nodes(gltf, spec['nodes'])
            raw = os.path.join(tout, name + '_raw.obj')
            write_obj_raw(groups, raw, name)
            mtl_names = {g[1] for g in groups}
            kd = {}
        else:
            cmd = [sys.executable, '-P', tool, tout, gltf, '--max-texture', str(spec['cpx']), '--ground']
            if spec['scale'] != 1.0: cmd += ['--scale', str(spec['scale'])]
            r = subprocess.run(cmd, capture_output=True, text=True)
            if r.returncode: print(r.stderr[-800:]); sys.exit(1)
            print(' ', r.stdout.strip())
            raw = os.path.join(tout, name + '.obj')
            kd = {}
            for blk in open(os.path.join(tout, name + '.mtl')).read().strip().split('\n\n'):
                ls = blk.split('\n'); kd[ls[0].split()[1]] = ls
        if spec['tris']:
            dec = os.path.join(tout, name + '_dec.obj')
            r = subprocess.run([sys.executable, '-P', os.path.join(HERE, 'decimate_exact.py'), raw, dec, str(spec['tris'])],
                               capture_output=True, text=True)
            if r.returncode: print(r.stderr[-800:]); sys.exit(1)
            print('  decimated:', r.stdout.strip().split('\n')[-1])
            raw = dec
        groups = read_obj(raw)
        if spec.get('hook'):
            groups = HOOKS[spec['hook']](groups, folder)
        groups = ground(groups)
        # textures
        color = name + '_Color.jpg'
        diff = find(folder, '_diff_')
        im = Image.open(diff).convert('RGB')
        if max(im.size) > spec['cpx']:
            im = im.resize((spec['cpx'], spec['cpx'] * im.size[1] // im.size[0]), Image.LANCZOS)
        im.save(os.path.join(out, color), quality=90, optimize=True)
        nor = find(folder, 'nor_gl')
        Image.open(nor).convert('RGB').resize((spec['npx'], spec['npx']), Image.LANCZOS).save(
            os.path.join(out, name + '_Normal.jpg'), quality=90, optimize=True)
        arm = np.asarray(Image.open(find(folder, '_arm_')).convert('RGB').resize((spec['mpx'], spec['mpx']), Image.LANCZOS)).astype(np.float32)
        ms = np.zeros(arm.shape[:2] + (4,), np.uint8)
        ms[..., 0] = arm[..., 2].clip(0, 255); ms[..., 3] = (255 - arm[..., 1]).clip(0, 255)
        if spec.get('hook') == 'tape':
            ms = tape_ms(ms, groups)
        Image.fromarray(ms, 'RGBA').save(os.path.join(out, name + '_MetallicSmoothness.png'), optimize=True)
        # MTL
        blocks = []
        for mname in dict.fromkeys(g[1] for g in groups):
            if mname in spec['glass']:
                (r_, g_, b_), d = spec['glass'][mname]
                blocks.append('newmtl %s\nKd %.3f %.3f %.3f\nKs 0.5 0.5 0.5\nNs 200\nd %.2f' % (mname, r_, g_, b_, d))
            else:
                d = 0.9 if mname == 'lifting_tape' else 1.0
                blocks.append('newmtl %s\nKd 1.000 1.000 1.000\nKs 0.05 0.05 0.05\nd %.1f\nmap_Kd %s' % (mname, d, color))
        open(os.path.join(out, name + '.mtl'), 'w').write('\n\n'.join(blocks) + '\n')
        write_obj(groups, os.path.join(out, name + '.obj'), name, 'Converted from Poly Haven (CC0).')
        allp = np.vstack([g[2] for g in groups])
        stats[name + '.obj'] = {'kind': 'model', 'tris': int(sum(len(g[4]) for g in groups)),
                                'size_m': np.round(allp.max(0) - allp.min(0), 3).tolist(), 'min_y': round(float(allp[:, 1].min()), 4),
                                'materials': list(dict.fromkeys(g[1] for g in groups)), 'maps': [color]}
        print(' ', stats[name + '.obj'])
    json.dump(stats, open(rp, 'w'), indent=1)


def write_obj_raw(groups, path, name):
    """Un-rounded OBJ in the glb_to_obj layout, so decimate_exact.py can read it."""
    lines = ['mtllib %s.mtl' % name]
    base = 0; body = []
    for gname, mtl, P, UV, F in groups:
        lines += ['v %.5f %.5f %.5f' % tuple(p) for p in P]
        lines += ['vt %.5f %.5f' % tuple(t) for t in UV]
        body.append('g ' + gname); body.append('usemtl ' + mtl)
        body += ['f %d/%d %d/%d %d/%d' % (a + 1 + base, a + 1 + base, b + 1 + base, b + 1 + base, c + 1 + base, c + 1 + base) for a, b, c in F]
        base += len(P)
    open(path, 'w').write('\n'.join(lines + body) + '\n')


def tape_ms(ms, groups):
    """Lifting tape: glossy (smoothness 0.85) on the tape faces, the core keeps the source roughness."""
    h, w = ms.shape[:2]
    from PIL import ImageDraw
    mask = Image.new('L', (w, h), 0); d = ImageDraw.Draw(mask)
    for gname, mtl, P, UV, F in groups:
        if mtl != 'lifting_tape':
            continue
        for f in F:
            d.polygon([(UV[i][0] * w, (1 - UV[i][1]) * h) for i in f], fill=255)
    m = np.asarray(mask) > 0
    ms = ms.copy(); ms[m, 3] = np.clip(0.4 * ms[m, 3] + 0.6 * 217, 0, 255).astype(np.uint8)
    return ms


main()
