"""Earrings, necklaces, bracelets, watch, displays, packaging, bullion, tools. Metres, Y up, front +Z."""
import numpy as np
from meshlib import (Part, merge, unit, rot, RX, RY, RZ, TAU, lathe, disc, sweep, circle2, uv_sphere, rounded_box,
                     convex, cylinder, smooth_normals, grid_faces)
from gems import round_brilliant, single_cut, oval_brilliant
from rings import band, tube, torus, catmull, prong_head, orient, MM

def lathe_sharp(profile, segs, mat):
    """lathe with crisp edges at every profile vertex (each profile segment is its own smooth band)."""
    bands = [lathe([profile[i], profile[i + 1]], segs, mat) for i in range(len(profile) - 1)
             if np.hypot(profile[i + 1][0] - profile[i][0], profile[i + 1][1] - profile[i][1]) > 1e-9]
    return merge(bands, mat)

# ------------------------------------------------------------------ earrings
def stud(D=5.1 * MM, metal='Platinum'):
    gem = round_brilliant(D, 'Diamond'); depth = -gem.v[:, 1].min()
    parts = [gem] + prong_head(D / 2, 0.0, metal, n=4, prong_r=0.34 * MM, depth=depth, crown_tip=0.6 * MM)
    base_y = -depth * 1.25
    parts.append(cylinder(0.42 * MM, 11 * MM, 10, metal, y0=base_y - 11 * MM))            # post
    # basket seat: a small disc under the culet that the four claw feet and the post are soldered to
    parts.append(cylinder(0.35 * D / 2 + 0.36 * MM, 0.6 * MM, 16, metal, y0=base_y - 0.3 * MM))
    # butterfly (scroll) back: two rolled wings on a plate
    plate = rounded_box((6.0 * MM, 0.7 * MM, 4.6 * MM), 0.3 * MM, metal, k=3).xf(t=(0, base_y - 8.5 * MM, 0))
    parts.append(plate)
    for s in (-1, 1):
        a = np.linspace(-0.3, np.pi * 1.25, 9)
        path = np.stack([s * (1.6 * MM + 1.1 * MM * np.cos(a)) * 0 + s * 1.8 * MM, base_y - 8.5 * MM + 1.0 * MM * np.sin(a) * 0 + 1.1 * MM * (1 - np.cos(a)) * 0.6,
                         np.zeros_like(a)], 1)
        wing = rounded_box((2.6 * MM, 2.0 * MM, 4.4 * MM), 0.9 * MM, metal, k=3).xf(t=(s * 1.7 * MM, base_y - 7.6 * MM, 0))
        parts.append(wing)
    return parts

def earrings_studs():
    parts = []
    for s in (-1, 1):
        parts += [p.xf(RX(np.pi / 2), t=(s * 8 * MM, 0, 0)) for p in stud()]   # stone faces +Z, post goes back (-Z)
    return parts

def hoop(D=24 * MM, d=2.4 * MM, metal='Gold18k'):
    gap = 0.22  # radians open at the top for the hinge post
    n = 72; a = np.linspace(gap / 2, TAU - gap / 2, n)
    path = np.stack([np.sin(a) * D / 2, np.cos(a) * D / 2 + D / 2, np.zeros(n)], 1)   # XY plane, faces +Z
    sec = circle2(d / 2, 12)
    rr = np.ones(n); taper = np.clip(np.minimum(a - gap / 2, TAU - gap / 2 - a) / 0.35, 0, 1); rr = 0.55 + 0.45 * np.sqrt(taper)
    body = orient(sweep(path, sec, metal, closed=False, scale=rr, cap=True))
    post = tube(np.array([[np.sin(gap / 2) * D / 2, np.cos(gap / 2) * D / 2 + D / 2, 0], [-np.sin(gap / 2) * D / 2 - 0.4 * MM, np.cos(gap / 2) * D / 2 + D / 2, 0]]),
                0.38 * MM, metal, sides=8)
    return [body, post]

def earrings_hoops():
    out = []
    for s in (-1, 1):
        out += [p.xf(t=(s * 15 * MM, 0, 0)) for p in hoop()]
    return out

# ------------------------------------------------------------------ chains / necklaces
def cable_chain(path_pts, metal, link_len=4.2 * MM, link_w=2.9 * MM, wire=0.62 * MM, closed=False, oseg=6, wseg=4):
    """links of an oval cable chain along a polyline, alternating 90 degrees."""
    P = np.asarray(path_pts, float)
    seg = np.linalg.norm(np.diff(P, axis=0), axis=1); acc = np.concatenate([[0], np.cumsum(seg)]); L = acc[-1]
    pitch = link_len - 2 * wire
    n = int(L / pitch)
    a = TAU * np.arange(oseg) / oseg
    oval = np.stack([np.cos(a) * (link_len - wire) / 2, np.sin(a) * (link_w - wire) / 2], 1)  # (along, side)
    sec = circle2(wire / 2, wseg, a0=np.pi / 4)
    base = sweep(np.stack([oval[:, 0], np.zeros(oseg), oval[:, 1]], 1), sec, metal, closed=True, up=[0, 1, 0])
    base = orient(base)   # link lies in XZ plane, long axis X
    parts = []
    up0 = np.array([0, 1.0, 0])
    for k in range(n):
        s = (k + 0.5) * pitch
        i = min(np.searchsorted(acc, s) - 1, len(P) - 2); f = (s - acc[i]) / max(seg[i], 1e-12)
        c = P[i] * (1 - f) + P[i + 1] * f
        t = unit(P[i + 1] - P[i])
        side = unit(np.cross(up0, t)) if abs(np.dot(up0, t)) < 0.95 else unit(np.cross([1.0, 0, 0], t))
        nrm = np.cross(t, side)
        if k % 2: side, nrm = nrm, -side
        M = np.stack([t, nrm, side], 1)  # local x->t, y->nrm, z->side
        parts.append(base.xf(M, t=c))
    return [merge(parts, metal)]

def resample(P, n, closed=False):
    P = np.asarray(P, float)
    Q = np.vstack([P, P[:1]]) if closed else P
    seg = np.linalg.norm(np.diff(Q, axis=0), axis=1); acc = np.concatenate([[0], np.cumsum(seg)])
    s = np.linspace(0, acc[-1], n, endpoint=not closed)
    out = np.stack([np.interp(s, acc, Q[:, k]) for k in range(3)], 1)
    return out, acc[-1]

# bust shape shared by the bust model and the draped necklaces
BUST_SECTIONS = [  # (y, half-width x, half-depth z, z-centre, superellipse exponent)
    (0.000, 0.110, 0.058, 0.000, 2.6), (0.060, 0.116, 0.060, 0.000, 2.6), (0.120, 0.119, 0.059, 0.002, 2.6),
    (0.145, 0.117, 0.057, 0.003, 2.6), (0.158, 0.110, 0.055, 0.004, 2.5), (0.168, 0.098, 0.053, 0.005, 2.4),
    (0.177, 0.083, 0.051, 0.006, 2.3), (0.185, 0.068, 0.049, 0.007, 2.2), (0.193, 0.056, 0.047, 0.008, 2.1),
    (0.201, 0.048, 0.044, 0.009, 2.0), (0.212, 0.043, 0.041, 0.010, 2.0), (0.240, 0.039, 0.039, 0.012, 2.0),
    (0.300, 0.037, 0.037, 0.016, 2.0), (0.312, 0.035, 0.035, 0.016, 2.0)]

def bust_radius(y, ang):
    """distance from the bust axis at height y in direction ang (0 = front +Z)."""
    S = np.array(BUST_SECTIONS)
    hx = np.interp(y, S[:, 0], S[:, 1]); hz = np.interp(y, S[:, 0], S[:, 2]); zc = np.interp(y, S[:, 0], S[:, 3]); p = np.interp(y, S[:, 0], S[:, 4])
    c, s = np.cos(ang), np.sin(ang)
    r = 1.0 / ((abs(s) / hx) ** p + (abs(c) / hz) ** p) ** (1 / p)
    return r, zc

def bust(mat='VelvetBlack', base_mat='Walnut'):
    S = np.array(BUST_SECTIONS); seg = 56
    rows = []
    ys = np.concatenate([np.linspace(0, 0.14, 6), np.linspace(0.147, 0.214, 12), np.linspace(0.226, 0.312, 5)])
    ys[0] = -0.0013   # the plinth top falls ~1 mm towards its rounded edges: sink the open bottom edge into it (no gap)
    V = []
    for y in ys:
        for j in range(seg):
            a = TAU * j / seg
            r, zc = bust_radius(y, a)
            V.append([r * np.sin(a), y, r * np.cos(a) + zc])
    V = np.array(V)
    F = grid_faces(len(ys), seg, wrap_cols=True)
    uv = np.array([[j / seg, y / 0.312] for y in ys for j in range(seg)])
    body = orient(Part(V, F, mat, uv=uv))
    # top cap: slightly domed disc
    top = ys[-1]; r_top = []
    capv = [[0, top + 0.004, S[-1, 3]]]
    for j in range(seg):
        a = TAU * j / seg; r, zc = bust_radius(top, a); capv.append([r * np.sin(a), top, r * np.cos(a) + zc])
    capv = np.array(capv); capf = np.array([[0, 1 + (j + 1) % seg, 1 + j] for j in range(seg)])
    cap = Part(capv, capf, mat, uv=np.c_[capv[:, 0] * 5 + 0.5, capv[:, 2] * 5 + 0.5])
    cap = orient(cap) if False else cap
    # bottom: walnut plinth 6 mm, slightly larger
    pl = rounded_box((0.250, 0.010, 0.130), 0.004, base_mat, k=4).xf(t=(0, -0.005, 0.0))
    # flip cap orientation if needed (normal must point up)
    if np.cross(capv[capf[0, 1]] - capv[capf[0, 0]], capv[capf[0, 2]] - capv[capf[0, 0]])[1] < 0: cap = cap.flip()
    return [body, cap, pl]

def drape_path(length, y_back=0.208, n=400, lift=0.0):
    """closed necklace path resting on the bust: back high around the neck, front dropping to fit length."""
    def build(drop):
        P = []
        for i in range(n):
            t = TAU * i / n
            w = ((1 + np.cos(t)) / 2) ** 2.2
            y = y_back - drop * w
            r, zc = bust_radius(y, t)
            # strand sits on the slope: push outward by bead radius
            P.append([(r + lift) * np.sin(t), y + lift * 0.3, (r + lift) * np.cos(t) + zc])
        P = np.array(P)
        return P, np.linalg.norm(np.diff(np.vstack([P, P[:1]]), axis=0), axis=1).sum()
    lo, hi = 0.0, 0.12
    for _ in range(40):
        mid = (lo + hi) / 2; _, L = build(mid)
        if L < length: lo = mid
        else: hi = mid
    return build((lo + hi) / 2)[0]

def dropped_path(length, n=400, seed=3, width=0.13):
    """open, irregular curve lying on the floor (y = 0) - a necklace dropped during the burglary."""
    rng = np.random.default_rng(seed)
    t = np.linspace(0, 1, 9)
    ctrl = np.stack([np.cos(t * 5.2 + 0.3) * width * (0.6 + 0.4 * rng.random(9)), np.zeros(9), np.sin(t * 5.2) * width * 0.55 * (0.6 + 0.4 * rng.random(9)) + t * 0.05], 1)
    P = catmull(ctrl, 30)
    P, L = resample(P, n)
    return (P - P.mean(0)) * (length / L) * np.array([1, 0, 1])

def pearl_strand(path, closed, d=7.0 * MM, clasp=True, graduated=False, mat='Pearl', metal='Gold18k', thread='Cord', clasp_offset=(0, 0, 0)):
    """hand-knotted strand: pearls with a silk knot in every 0.6 mm gap, fluted box clasp threaded to the end pearls."""
    P, L = resample(path, 2000, closed=closed)
    acc = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(P, axis=0), axis=1))])
    at = lambda s: P[min(np.searchsorted(acc, s), len(P) - 1)]
    CL, gap = 2.5 * MM, 0.6 * MM                        # clasp half-length, knot gap
    s = (CL + gap if clasp else 0) + d / 2; k = 0
    end = acc[-1] - ((CL + gap if (clasp and closed) else 0) + d / 2)
    S = []; Ds = []
    while s < end:
        di = d * (1 + (0.25 * np.cos(TAU * s / acc[-1]) if graduated else 0)) * (1 + 0.03 * np.sin(k * 2.7))
        S.append(s); Ds.append(di)
        s += di + gap; k += 1
    if clasp and closed and len(S) > 1:   # spread the leftover length over the knots so the clasp gap matches the others
        left = (acc[-1] - (CL + gap)) - (S[-1] + Ds[-1] / 2)
        S = [sk + i * left / (len(S) - 1) for i, sk in enumerate(S)]
    parts = []; C = []; Rr = []
    pearl = uv_sphere(1.0, 10, 7, mat)
    for k, (sk, di) in enumerate(zip(S, Ds)):
        c = at(sk)
        parts.append(pearl.xf(RY(k * 1.3), t=c, s=di / 2 * np.array([1, 0.97, 1])))
        C.append(c); Rr.append(di / 2 * 0.97)
    out = [merge(parts, mat)]
    knots = []
    def knot(a, ra, b, rb):
        """silk between two beads: thread enters each bead's drill hole, a knot (0.9 mm) fills the gap."""
        u = unit(b - a); p0 = a + u * (ra - 0.25 * MM); p1 = b - u * (rb - 0.25 * MM)
        Lk = np.linalg.norm(p1 - p0)
        if Lk < 1e-6: return
        prof = [(0.28 * MM, 0), (0.45 * MM, 0.25 * MM + gap * 0.5), (0.28 * MM, Lk)]
        y = unit(u); x = unit(np.cross([0.0, 1.0, 0.0] if abs(y[1]) < 0.9 else [1.0, 0, 0], y)); z = np.cross(x, y)
        knots.append(lathe(prof, 6, thread).xf(np.stack([x, y, z], 1), t=p0))
    for i in range(len(C) - 1):
        knot(C[i], Rr[i], C[i + 1], Rr[i + 1])
    if clasp:
        c = P[0] + np.asarray(clasp_offset, float)       # e.g. let the clasp sag onto the neck
        tg = unit((P[1] - P[-1]) if closed else (P[1] - P[0]))
        up = np.array([0, 1.0, 0]) - tg * tg[1]; up = unit(up); side = np.cross(tg, up)
        M = np.stack([tg, up, side], 1)                 # box long axis along the strand
        out.append(rounded_box((2 * CL, 3 * MM, 3.4 * MM), 0.8 * MM, metal, k=3).xf(M, t=c))
        # thread from the clasp ends (rings) to the first / last pearl
        knot(c + tg * (CL - 0.25 * MM), 0.25 * MM, C[0], Rr[0])
        if closed: knot(C[-1], Rr[-1], c - tg * (CL - 0.25 * MM), 0.25 * MM)
    out.append(merge(knots, thread))
    return out

def pendant(metal='Gold18k', D=6.5 * MM):
    """4-prong round diamond pendant hanging from a bail; stone faces +Z; origin at the bail top."""
    gem = round_brilliant(D, 'Diamond'); depth = -gem.v[:, 1].min()
    head = [gem] + prong_head(D / 2, 0.0, metal, n=4, prong_r=0.36 * MM, depth=depth, crown_tip=0.6 * MM, segs_gallery=24)
    head = [p.xf(RX(np.pi / 2)) for p in head]          # table faces +Z
    bail = torus(1.6 * MM, 0.45 * MM, metal, segs=20, sides=8, plane='yz')   # ring plane YZ, chain passes along X
    bail = bail.xf(t=(0, D / 2 + 2.2 * MM, -0.6 * MM))
    return [p.xf(t=(0, -(D / 2 + 3.8 * MM), 0)) for p in head + [bail]]

def necklace_pendant(path, closed, metal='Gold18k', pend_at=None):
    parts = cable_chain(np.vstack([path, path[:1]]) if closed else path, metal, link_len=4.4 * MM, link_w=2.9 * MM, wire=0.58 * MM)
    if pend_at is not None:
        pos, down, front = pend_at
        M = np.stack([unit(np.cross(down, front)) * -1, -down, front], 1)
        parts += [p.xf(M, t=pos) for p in pendant(metal)]
    return parts

# ------------------------------------------------------------------ bracelets
def tennis_bracelet(metal='Platinum', n=54, stone=3.0 * MM, a=35 * MM, b=28 * MM):
    """flat oval lying on the counter (y up), stones facing up."""
    m = 400; t = TAU * np.arange(m) / m
    oval = np.stack([a * np.cos(t), np.zeros(m), b * np.sin(t)], 1)
    P, L = resample(oval, 4000, closed=True)
    acc = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(P, axis=0), axis=1))])
    pitch = acc[-1] / n
    gem = single_cut(stone, 'Diamond'); dep = -gem.v[:, 1].min()
    cupl = pitch * 0.96; cupw = stone * 1.10
    cup = rounded_box((cupl, dep + 0.7 * MM, cupw), 0.45 * MM, metal, k=1).xf(t=(0, -(dep + 0.7 * MM) / 2 + 0.15 * MM, 0))
    bead = uv_sphere(0.40 * MM, 6, 3, metal)
    link = [gem, cup] + [bead.xf(t=(pitch * 0.5, 0.20 * MM, sz * stone * 0.42)) for sz in (-1, 1)]
    stones, met = [], []
    for k in range(n):
        s = (k + 0.5) * pitch; i = min(np.searchsorted(acc, s), len(P) - 1)
        c = P[i]; tn = unit(P[(i + 1) % len(P)] - P[i - 1])
        M = np.stack([tn, [0, 1, 0], np.cross(tn, [0, 1, 0])], 1)
        for p in link:
            q = p.xf(M, t=c + [0, dep + 0.55 * MM, 0])
            (stones if p.mat == 'Diamond' else met).append(q)
    clasp = rounded_box((7 * MM, 2.6 * MM, 3.6 * MM), 0.6 * MM, metal, k=3).xf(t=(a + 0.4 * MM, 1.3 * MM, 0))
    return [merge(stones, 'Diamond'), merge(met, metal), clasp]

def bangle(metal='Gold18k', rin=31 * MM, w=5 * MM, t=2.6 * MM):
    b = band(rin, lambda p: w, lambda p: t, metal, segs=96, m=16, outer_exp=0.8, inner_exp=0.35, side=0.4)
    return [b.xf(RZ(np.pi / 2))]      # axis vertical: lying flat on the counter

# ------------------------------------------------------------------ watch + pillow
PILLOW_A, PILLOW_B = 31 * MM, 25 * MM     # half-width (z) and half-height (y) of the pillow ellipse

def watch(metal='SteelPolished', strap='LeatherBrown', case_r=19.5 * MM):
    parts = []
    top = PILLOW_B * 2 - 0.5 * MM   # flat case back pressed 0.5 mm into the crown of the soft cushion
    y0 = top
    prof = [(0.0, 0), (case_r * 0.80, 0), (case_r * 0.92, 0.6 * MM), (case_r * 0.99, 2.2 * MM), (case_r, 4.0 * MM),
            (case_r * 0.99, 5.8 * MM), (case_r * 0.955, 7.0 * MM), (case_r * 0.90, 7.6 * MM), (case_r * 0.86, 7.6 * MM)]
    parts.append(lathe([(r, y0 + y) for r, y in prof], 72, metal))
    dial_r = case_r * 0.84
    # dial (textured) and flange
    dial = disc(dial_r, 72, 'WatchDial', y=y0 + 5.6 * MM)
    dial.uv = np.c_[0.5 + 0.5 * dial.v[:, 0] / dial_r, 0.5 - 0.5 * dial.v[:, 2] / dial_r]  # 12 o'clock toward -Z
    parts.append(dial)
    parts.append(lathe([(dial_r, y0 + 5.6 * MM), (case_r * 0.86, y0 + 7.6 * MM)], 72, metal))
    # domed crystal
    cr = [(case_r * 0.86 * np.cos(a), y0 + 7.6 * MM + 1.3 * MM * np.sin(a)) for a in np.linspace(0, np.pi / 2, 7)]
    parts.append(lathe(cr, 72, 'Glass'))
    # hands (10:09:36), dauphine-ish plates
    def hand(length, width, ang, h, mat=metal, thick=0.25 * MM):
        pts = np.array([[-width * 0.5, -1.4 * MM], [width * 0.5, -1.4 * MM], [width * 0.35, length * 0.75], [0, length], [-width * 0.35, length * 0.75]])
        top_ = np.c_[pts[:, 0], np.full(5, h + thick), -pts[:, 1]]; bot = np.c_[pts[:, 0], np.full(5, h), -pts[:, 1]]
        V = np.vstack([top_, bot]); F = [[0, 1, 2], [0, 2, 4], [2, 3, 4], [5, 7, 6], [5, 9, 7], [7, 9, 8]]
        for i in range(5):
            j = (i + 1) % 5; F += [[i, i + 5, j + 5], [i, j + 5, j]]
        p = Part(V, np.array(F), mat, smooth=False)
        return p.xf(RY(-ang))
    hy = y0 + 5.7 * MM
    parts.append(hand(dial_r * 0.62, 1.6 * MM, TAU * (10 + 9 / 60) / 12, hy))
    parts.append(hand(dial_r * 0.90, 1.2 * MM, TAU * 9.6 / 60, hy + 0.35 * MM))
    parts.append(hand(dial_r * 0.95, 0.35 * MM, TAU * 36 / 60, hy + 0.7 * MM, mat='Gold18k', thick=0.15 * MM))
    parts.append(cylinder(0.9 * MM, 1.2 * MM, 12, metal, y0=hy + 0.9 * MM))
    # crown at 3 o'clock (+X)
    crown = lathe([(0, 0), (2.6 * MM, 0), (2.6 * MM, 2.6 * MM), (2.2 * MM, 3.0 * MM), (0, 3.0 * MM)], 18, metal)
    parts.append(crown.xf(RZ(-np.pi / 2), t=(case_r - 0.3 * MM, y0 + 4.0 * MM, 0)))
    # lugs (4) as swept bars
    for sz in (-1, 1):
        for sx in (-1, 1):
            a = np.linspace(0, 1, 6)
            path = np.stack([np.full(6, sx * 11.1 * MM), y0 + 4.6 * MM - 2.6 * MM * a ** 1.5, sz * (case_r * 0.80 + 7.5 * MM * a)], 1)
            sec = np.array([[-1.1, -1.4], [1.1, -1.4], [1.1, 1.4], [-1.1, 1.4]]) * MM
            parts.append(orient(sweep(path, sec, metal, closed=False, cap=True, smooth=False)))
    # padded leather strap: leaves the spring bars between the lugs, runs tight around the cushion (0.2 mm clear of
    # its surface) and tucks under it, pressed flat on the shelf; the tails end under the cushion's flat bottom,
    # where the buckle would be (hidden). Nothing goes below the cushion's base plane.
    parts += strap_on_pillow(strap, y0)
    # spring bars between the lugs (hidden inside the strap loops)
    for sz in (-1, 1):
        a = 0.85; sb = np.array([0, y0 + 4.6 * MM - 2.6 * MM * a ** 1.5, sz * (case_r * 0.80 + 7.5 * MM * a)])
        parts.append(cylinder(0.75 * MM, 22.0 * MM, 10, metal).xf(RZ(np.pi / 2), t=sb + np.array([11.0 * MM, 0, 0])))
    return parts

PILLOW_BASE = 0.10 * PILLOW_B      # the cushion's flat bottom (its base plane, y = 0 after grounding)

def pillow_outline(n=4000):
    """the cushion's middle cross-section (z, y) as a dense convex polyline (same formula as watch_pillow)."""
    a = np.linspace(0, TAU, n, endpoint=False)
    return np.stack([PILLOW_A * np.sin(a), np.maximum(PILLOW_B + PILLOW_B * np.cos(a), PILLOW_BASE)], 1)

def strap_on_pillow(mat, y0, case_r=19.5 * MM, w_lug=19.8 * MM, w_end=16.5 * MM, t_lug=3.2 * MM, t_end=2.6 * MM, clear=0.2 * MM):
    """two padded strap halves (+Z = 6 o'clock, -Z = 12 o'clock) from the spring bars around the cushion."""
    P = pillow_outline()
    th = np.linspace(np.pi / 2, -np.pi / 2 - 0.35, 1600)            # normal angle in the (z, y) plane: top -> +Z side -> under
    N = np.stack([np.cos(th), np.sin(th)], 1)
    sup = P[np.argmax(P @ N.T, axis=0)]                           # support point of the cushion section for each normal
    sec = np.array([[0.50, 0.0], [0.48, 0.25], [0.40, 0.40], [0.22, 0.48], [0.0, 0.50], [-0.32, 0.49], [-0.48, 0.44], [-0.50, 0.25],
                    [-0.50, 0.0], [-0.50, -0.25], [-0.48, -0.44], [-0.32, -0.49], [0.0, -0.50], [0.22, -0.48], [0.40, -0.40], [0.48, -0.25]])
    out = []
    for sz in (1, -1):
        a = 0.85                                                  # spring bar position along the lug
        S = np.array([case_r * 0.80 + 7.5 * MM * a, y0 + 4.6 * MM - 2.6 * MM * a ** 1.5])
        ldir = unit(np.array([7.5, -3.9 * a ** 0.5]))             # lug direction (z, y)
        S0, S1 = S - ldir * 1.0 * MM, S + ldir * 1.6 * MM         # strap end folds round the spring bar
        off = sup + (t_lug / 2 + clear) * N
        g = (off * N).sum(1) - N @ S1                             # support line through S1 = tangent from S1 to the strap curve
        k = np.where(((g[:-1] < 0) & (g[1:] >= 0)) | ((g[:-1] >= 0) & (g[1:] < 0)))[0]
        kt = k[th[k] < 1.2][0] + 1                                # lower tangent point (strap comes down the side)
        line = np.array([S0, S, S1] + [S1 + (off[kt] - S1) * f for f in np.linspace(0, 1, 9)[1:-1]])
        curve = off[kt:]
        P2 = np.vstack([line, curve])
        seg = np.r_[0, np.cumsum(np.linalg.norm(np.diff(P2, axis=0), axis=1))]
        u2 = seg / seg[-1]; t2 = t_lug + (t_end - t_lug) * u2
        P2[len(line):] = sup[kt:] + (t2[len(line):, None] / 2 + clear) * N[kt:]   # hug with the local thickness
        seg = np.r_[0, np.cumsum(np.linalg.norm(np.diff(P2, axis=0), axis=1))]
        s = np.linspace(0, seg[-1], int(seg[-1] / (0.9 * MM)) + 1)
        P3 = np.stack([np.interp(s, seg, P2[:, 0]), np.interp(s, seg, P2[:, 1])], 1)
        u = s / s[-1]; t = t_lug + (t_end - t_lug) * u
        for it in range(3):                                       # soften the bend at the tangent point, keep it on the shelf
            P3[1:-1] = 0.25 * P3[:-2] + 0.5 * P3[1:-1] + 0.25 * P3[2:]
            P3[:, 1] = np.maximum(P3[:, 1], PILLOW_BASE + t / 2 + 0.02 * MM)
        P3[0], P3[1] = S0, S
        under = np.where((P3[:, 0] < 12.4 * MM) & (u > 0.5))[0]    # tail ends under the flat bottom (which starts at z = 11.9 mm)
        n_keep = under[0] if len(under) else len(P3)
        P3, t, u = P3[:n_keep], t[:n_keep], u[:n_keep]
        path = np.stack([np.zeros(len(P3)), P3[:, 1], sz * P3[:, 0]], 1)
        T0 = unit(path[2] - path[1]); B0 = np.cross(T0, [1.0, 0, 0])
        outward = unit(np.array([0, S[1] - PILLOW_B, sz * S[0]]))
        sgn = 1.0 if np.dot(B0, outward) > 0 else -1.0           # domed face outward, flat face on the cushion
        w = w_lug + (w_end - w_lug) * u
        st = sweep(path, sec * np.array([sgn, 1.0]), mat, closed=False, scale=np.stack([t, w], 1), cap=True, up=[1, 0, 0])
        out.append(orient(st))
    return out

def watch_pillow(mat='LeatherBlack', length=78 * MM):
    """C-section cushion: ellipse (z half-width A, y half-height B) along X with rounded ends; flat bottom."""
    m = 48; n = 22
    A, B = PILLOW_A, PILLOW_B; rr = 9 * MM
    xs = np.concatenate([[-length / 2 + rr * (1 - np.cos(a)) for a in np.linspace(0, np.pi / 2, 6)][:-1],
                         np.linspace(-length / 2 + rr, length / 2 - rr, n - 10),
                         [length / 2 - rr * (1 - np.cos(a)) for a in np.linspace(np.pi / 2, 0, 6)][1:]])
    sc = np.concatenate([[np.sin(a) for a in np.linspace(0, np.pi / 2, 6)][:-1], np.ones(n - 10), [np.sin(a) for a in np.linspace(np.pi / 2, 0, 6)][1:]])
    sc = 0.25 + 0.75 * sc
    V = []
    for x, s in zip(xs, sc):
        for j in range(m):
            a = TAU * j / m
            y = B + B * np.cos(a) * s; z = A * np.sin(a) * s
            y = max(y, 0.10 * B)                 # flatten the bottom (sits on the counter)
            V.append([x, y, z])
    V = np.array(V)
    F = grid_faces(len(xs), m, wrap_cols=True)
    capL = Part(np.vstack([[xs[0] - 0.5 * MM, B, 0], V[:m]]), np.array([[0, 1 + (j + 1) % m, 1 + j] for j in range(m)]), mat)
    capR = Part(np.vstack([[xs[-1] + 0.5 * MM, B, 0], V[-m:]]), np.array([[0, 1 + j, 1 + (j + 1) % m] for j in range(m)]), mat)
    body = Part(V, F, mat, uv=np.array([[j / m, i / len(xs)] for i in range(len(xs)) for j in range(m)]))
    out = merge([body, capL.__class__(capL.v, capL.f, mat, uv=np.zeros((len(capL.v), 2))), capR.__class__(capR.v, capR.f, mat, uv=np.zeros((len(capR.v), 2)))])
    return [orient(out)]

# ------------------------------------------------------------------ display stands
def ring_cone(mat='VelvetBlack', base='Walnut'):
    prof = [(0.0, 0.010), (0.0115, 0.010), (0.0112, 0.020), (0.0090, 0.060), (0.0074, 0.083)]
    prof += [(0.0074 * np.cos(a), 0.083 + 0.0074 * np.sin(a)) for a in np.linspace(0.3, np.pi / 2, 5)]
    cone = lathe(prof, 40, mat)
    plinth = lathe_sharp([(0, 0), (0.024, 0), (0.025, 0.002), (0.025, 0.008), (0.024, 0.010), (0, 0.010)], 48, base)
    return [cone, plinth]

def earring_stand(metal='Brass'):
    base = lathe([(0, 0), (0.032, 0), (0.033, 0.002), (0.032, 0.006), (0.010, 0.009), (0.004, 0.010), (0, 0.010)], 56, metal)
    post = cylinder(0.0028, 0.115, 16, metal, y0=0.009)
    bar = cylinder(0.0024, 0.130, 16, metal).xf(RZ(np.pi / 2), t=(0.065, 0.124, 0))
    ends = [uv_sphere(0.0038, 12, 8, metal).xf(t=(s * 0.066, 0.124, 0)) for s in (-1, 1)]
    joint = uv_sphere(0.0042, 12, 8, metal).xf(t=(0, 0.124, 0))
    return [base, post, bar, joint] + ends

def ring_box(open_deg=105, shell='VelvetNavy', lining='VelvetCream', trim='Gold18k'):
    W, D, Hb, Hl = 0.052, 0.048, 0.026, 0.016
    parts = [rounded_box((W, Hb, D), 0.007, shell, k=6, sharp='top').xf(t=(0, Hb / 2, 0))]
    # lining well + two rolls with a slot between them along X
    parts.append(rounded_box((W - 0.008, 0.002, D - 0.008), 0.0009, lining, k=3).xf(t=(0, Hb - 0.0003, 0)))
    for sz in (-1, 1):
        roll = lathe([(0, -0.019), (0.0065, -0.019), (0.0072, -0.016), (0.0072, 0.016), (0.0065, 0.019), (0, 0.019)], 24, lining)
        parts.append(roll.xf(RZ(np.pi / 2), t=(0, Hb - 0.0012, sz * 0.0078)))
    # hinge pin along the back seam edge (within the flat part of the back, clear of the rounded corners)
    parts.append(lathe([(0.0005, -0.018), (0.0005, 0.018)], 8, trim, cap_bottom=True, cap_top=True).xf(RZ(np.pi / 2), t=(0, Hb, -D / 2)))
    lid = [rounded_box((W, Hl, D), 0.007, shell, k=6, sharp='bottom').xf(t=(0, Hl / 2, 0)),
           rounded_box((W - 0.009, 0.0016, D - 0.009), 0.0007, lining, k=3).xf(t=(0, -0.0005, 0))]
    # lid hinged on the back top edge (z = -D/2, y = Hb); closed lid sits on the base
    ang = np.radians(open_deg)
    for p in lid:
        q = p.xf(t=(0, 0, D / 2))                       # hinge line at local z=0
        q = q.xf(RX(-ang))                              # open towards the back
        parts.append(q.xf(t=(0, Hb, -D / 2)))
    return parts

def price_tag(cell=0, string_mat='Cord'):
    W, H, T = 0.030, 0.013, 0.0004
    card = rounded_box((W, T, H), 0.0018, 'PriceTag', k=4, uv_mode=None)
    # UV: map x/z to the atlas cell (4 x 2 grid), both faces
    cx, cy = cell % 4, cell // 4
    u = (card.v[:, 0] / W + 0.5); v = (card.v[:, 2] / H + 0.5)
    card.uv = np.c_[(cx + 0.03 + 0.94 * u) / 4, 1 - (cy + 0.03 + 0.94 * v) / 2]
    card = card.xf(t=(0, T / 2, 0))
    # string loop from the hole at the left end
    a = np.linspace(0, TAU, 40, endpoint=False)
    loop = np.stack([-W / 2 + 0.003 - 0.022 - 0.022 * np.cos(a), np.full(40, T + 0.0003) + 0.0002 * np.sin(2 * a), 0.006 * np.sin(a)], 1)
    string = orient(sweep(loop, circle2(0.00025, 5), string_mat, closed=True))
    return [card, string]

def price_stand(cell=0):
    """acrylic L-stand with a printed card (uses the price tag atlas cell)."""
    W, H = 0.060, 0.026
    base = rounded_box((W, 0.0025, 0.030), 0.0008, 'Acrylic', k=2).xf(t=(0, 0.00125, 0))
    back = rounded_box((W, H * 1.15, 0.0025), 0.0008, 'Acrylic', k=2).xf(RX(-np.radians(18)), t=(0, H * 0.58, -0.010))
    card = rounded_box((W * 0.94, H, 0.0004), 0.0002, 'PriceTag', k=1, uv_mode=None)
    cx, cy = cell % 4, cell // 4
    u = card.v[:, 0] / (W * 0.94) + 0.5; v = card.v[:, 1] / H + 0.5
    card.uv = np.c_[(cx + 0.02 + 0.96 * u) / 4, 1 - (cy + 0.02 + 0.96 * (1 - v)) / 2]
    card = card.xf(RX(-np.radians(18)), t=(0, H * 0.58, -0.0082))
    return [base, back, card]

# ------------------------------------------------------------------ packaging
def gift_bag(W=0.150, H=0.190, D=0.080, outer='GiftBag', inner='PaperWhite', rope='Cord'):
    """open-top paper bag with gusseted sides and rope handles; texture: front/back panel atlas u<0.75."""
    t = 0.0008
    nx, ny, nz = 6, 10, 4
    def wall(p0, du, dv, nu, nv, uv0, uv1, gusset=False):
        V = []; UV = []
        for i in range(nv + 1):
            for j in range(nu + 1):
                p = p0 + du * j / nu + dv * i / nv
                if gusset:
                    c = 1 - abs(2 * j / nu - 1)            # 1 at the middle crease
                    p = p + unit(np.cross(dv, du)) * -0.010 * c * (0.35 + 0.65 * i / nv)
                V.append(p); UV.append([uv0[0] + (uv1[0] - uv0[0]) * j / nu, uv0[1] + (uv1[1] - uv0[1]) * i / nv])
        F = grid_faces(nv + 1, nu + 1)
        return Part(np.array(V), F, outer, uv=np.array(UV), smooth=False)
    ex, ey, ez = np.eye(3)
    hx, hz = W / 2, D / 2
    panel = ((0.0, 0.0), (0.75, 1.0)); side = ((0.80, 0.0), (0.98, 1.0))
    walls = [wall(np.array([-hx, 0, hz]), ex * W, ey * H, nx, ny, *panel),                    # front
             wall(np.array([hx, 0, -hz]), -ex * W, ey * H, nx, ny, *panel),                   # back
             wall(np.array([hx, 0, hz]), -ez * D, ey * H, nz, ny, *side, gusset=True),        # right side
             wall(np.array([-hx, 0, -hz]), ez * D, ey * H, nz, ny, *side, gusset=True),       # left side
             wall(np.array([-hx, 0, -hz]), ex * W, ez * D, nx, nz, *side)]                    # bottom (faces down)
    out = []
    for wpart in walls:
        out.append(wpart)
        innerp = wpart.xf(t=-wpart.n.mean(0) * t).flip(); innerp.mat = inner
        out.append(innerp)
    # rope handles: arcs anchored on front and back panels
    for sz in (1, -1):
        a = np.linspace(0, np.pi, 24)
        path = np.stack([-0.032 * np.cos(a), H - 0.025 + 0.075 * np.sin(a), np.full(24, sz * (hz + 0.0015)) + sz * 0.004 * np.sin(a)], 1)
        out.append(tube(path, 0.0022, rope, sides=8, tip=False))
    return out

def gift_box(S=0.085, H=0.040, box='PaperBlack', ribbon='SatinRibbon'):
    parts = [rounded_box((S, H * 0.92, S), 0.0015, box, k=3).xf(t=(0, H * 0.46, 0)),
             rounded_box((S + 0.003, H * 0.38, S + 0.003), 0.0015, box, k=3).xf(t=(0, H - H * 0.19 + 0.001, 0))]
    top = H + 0.0015
    rw, rt = 0.012, 0.0006
    for M in (np.eye(3), RY(np.pi / 2)):
        loop = np.array([[-S / 2 - 0.0018, 0.0002, 0], [-S / 2 - 0.0018, top + rt, 0], [S / 2 + 0.0018, top + rt, 0], [S / 2 + 0.0018, 0.0002, 0]])
        loop = np.vstack([loop[i] * (1 - f) + loop[(i + 1) % 4] * f for i in range(4) for f in np.linspace(0, 1, 6, endpoint=False)])
        sec = np.array([[-0.5, -0.5], [0.5, -0.5], [0.5, 0.5], [-0.5, 0.5]])
        rb = sweep(loop, sec, ribbon, closed=True, scale=np.tile([[rw, rt * 2]], (len(loop), 1)), up=[0, 0, 1], smooth=False)
        parts.append(orient(rb).xf(M))
    # bow: two loops and two tails
    for s in (-1, 1):
        a = np.linspace(0, TAU, 30, endpoint=False)
        path = np.stack([s * (0.016 + 0.016 * np.cos(a)), top + 0.007 + 0.007 * np.sin(a), 0.004 * np.sin(a) * s], 1)
        sec = np.array([[-0.5, -0.5], [0.5, -0.5], [0.5, 0.5], [-0.5, 0.5]])
        parts.append(orient(sweep(path, sec, ribbon, closed=True, scale=np.tile([[0.010, 0.0012]], (30, 1)), up=[0, 0, 1], smooth=True)))
        tail = np.array([[0, top + 0.002, 0], [s * 0.010, top + 0.001, 0.012], [s * 0.016, top + 0.0005, 0.026]])
        tail = catmull(tail, 6)
        parts.append(orient(sweep(tail, sec, ribbon, closed=False, cap=True, scale=np.tile([[0.010, 0.0010]], (len(tail), 1)), up=[0, 1, 0], smooth=False)))
    parts.append(uv_sphere(0.0055, 12, 8, ribbon).xf(t=(0, top + 0.004, 0), s=(1, 0.7, 1)))
    return parts

# ------------------------------------------------------------------ bullion
def rounded_hull(corners, face_normals, r, mat, sphere_n=(10, 6)):
    """Minkowski sum of a convex polytope (corner points) with a sphere: exact per-vertex normals."""
    from scipy.spatial import ConvexHull
    pts, nrm = [], []
    dirs = [unit(np.array([np.sin(t) * np.cos(p), np.cos(t), np.sin(t) * np.sin(p)]))
            for t in np.linspace(0, np.pi, sphere_n[1] + 1) for p in np.linspace(0, TAU, sphere_n[0], endpoint=False)]
    dirs = np.vstack([np.array(dirs), np.asarray(face_normals, float)])
    for c in corners:
        for d in dirs:
            pts.append(c + r * d); nrm.append(d)
    pts, nrm = np.array(pts), np.array(nrm)
    key = np.round(pts / 1e-8).astype(np.int64); _, idx = np.unique(key, axis=0, return_index=True)
    pts, nrm = pts[idx], nrm[idx]
    h = ConvexHull(pts)
    used = np.unique(h.simplices)
    remap = -np.ones(len(pts), int); remap[used] = np.arange(len(used))
    F = remap[h.simplices]; V = pts[used]; N = nrm[used]
    for i, (a, b, c) in enumerate(F):
        if np.dot(np.cross(V[b] - V[a], V[c] - V[a]), h.equations[i][:3]) < 0: F[i] = [a, c, b]
    return Part(V, F, mat, n=N)

def gold_bar_cast(mat='GoldBar1kg'):
    r = 0.0022
    tl, tw, bl, bw, h = 0.116 / 2 - r, 0.052 / 2 - r, 0.106 / 2 - r, 0.044 / 2 - r, 0.0095
    corners = [[sx * tl, h - r, sz * tw] for sx in (-1, 1) for sz in (-1, 1)] + [[sx * bl, r, sz * bw] for sx in (-1, 1) for sz in (-1, 1)]
    dl, dw = tl - bl, tw - bw; hh = h - 2 * r
    fn = [[0, 1, 0], [0, -1, 0], unit([hh, -dl, 0]), unit([-hh, -dl, 0]), unit([0, -dw, hh]), unit([0, -dw, -hh])]
    p = rounded_hull(np.array(corners), fn, r, mat, sphere_n=(16, 8))
    lo, hi = p.v.min(0), p.v.max(0)
    p.uv = np.c_[(p.v[:, 0] - lo[0]) / (hi[0] - lo[0]), 1 - (p.v[:, 2] - lo[2]) / (hi[2] - lo[2])]
    return [p]

def gold_bar_minted(mat='GoldBar100g'):
    r = 0.0007
    a, b, h = 0.031 / 2 - r, 0.055 / 2 - r, 0.0031
    corners = [[sx * a, y, sz * b] for sx in (-1, 1) for sz in (-1, 1) for y in (r, h - r)]
    fn = [[0, 1, 0], [0, -1, 0], [1, 0, 0], [-1, 0, 0], [0, 0, 1], [0, 0, -1]]
    p = rounded_hull(np.array(corners), fn, r, mat, sphere_n=(8, 4))
    lo, hi = p.v.min(0), p.v.max(0)
    p.uv = np.c_[(p.v[:, 0] - lo[0]) / (hi[0] - lo[0]), (p.v[:, 2] - lo[2]) / (hi[2] - lo[2])]
    p.uv[:, 1] = 1 - p.uv[:, 1]
    return [p]

def gold_coin(mat='GoldCoin', D=0.0327, T=0.00287):
    R = D / 2; rim = 0.0011
    prof = [(0, T - 0.00035), (R - rim, T - 0.00035), (R - rim * 0.7, T), (R - 0.0002, T), (R, T - 0.0002), (R, 0.0002),
            (R - 0.0002, 0), (R - rim * 0.7, 0), (R - rim, 0.00035), (0, 0.00035)]
    p = lathe_sharp(prof[::-1], 96, mat)
    # sharp edges: rebuild normals per ring section using smooth normals is fine; UVs: top -> left half, bottom -> right half
    v = p.v
    top = v[:, 1] > T * 0.5
    u = np.where(top, 0.25 + 0.25 * v[:, 0] / R * 0.985, 0.75 - 0.25 * v[:, 0] / R * 0.985)
    w = 0.5 - 0.5 * v[:, 2] / R * 0.985
    edge = np.hypot(v[:, 0], v[:, 2]) > R - 0.0001
    p.uv = np.c_[u, w]
    return [p]

# ------------------------------------------------------------------ tools / counter items
def loupe(body='BlackMetal', case='Chrome', glass='Glass'):
    parts = []
    br, bh = 0.0115, 0.011
    parts.append(lathe([(br * 0.80, 0), (br, 0.0006), (br, bh - 0.0006), (br * 0.80, bh)], 40, body))
    parts.append(lathe([(br * 0.80, bh), (br * 0.80, 0.0)], 40, body))  # inner wall
    for y, s in ((0.0008, -1), (bh - 0.0008, 1)):
        parts.append(lathe([(br * 0.80 * np.cos(a), y + s * 0.0012 * np.sin(a)) for a in np.linspace(0, np.pi / 2, 5)][::-1 if s < 0 else 1], 40, glass))
    # chrome case plate (teardrop with hole) lying flat, rotated open around a pivot pin
    a = np.linspace(0, TAU, 40, endpoint=False)
    outline = np.stack([0.0145 * np.cos(a) + np.where(np.cos(a) > 0, 0.010 * np.cos(a) ** 3, 0), 0.0145 * np.sin(a)], 1)
    hole = np.stack([0.01148 * np.cos(a), 0.01148 * np.sin(a)], 1)   # the barrel (r 11.5 mm) is held in this hole
    V = []; F = []
    for y in (0.0, 0.0018):
        V += [[x, y, z] for x, z in outline] + [[x, y, z] for x, z in hole]
    V = np.array(V); n = 40
    def idx(layer, ring, i): return layer * 2 * n + ring * n + (i % n)
    for i in range(n):
        for layer, flip in ((1, False), (0, True)):
            f1 = [idx(layer, 0, i), idx(layer, 1, i), idx(layer, 1, i + 1)]; f2 = [idx(layer, 0, i), idx(layer, 1, i + 1), idx(layer, 0, i + 1)]
            if flip: f1, f2 = f1[::-1], f2[::-1]
            F += [f1[::-1], f2[::-1]] if not flip else [f1[::-1], f2[::-1]]
        for ring in (0, 1):
            q = [idx(0, ring, i), idx(0, ring, i + 1), idx(1, ring, i + 1), idx(1, ring, i)]
            F += [[q[0], q[1], q[2]], [q[0], q[2], q[3]]]
    plate = Part(V, np.array(F), case, smooth=False)
    # orient each face outward from the plate centre-line (cheap fix)
    cen = plate.v[plate.f].mean(1); fn = np.cross(plate.v[plate.f[:, 1]] - plate.v[plate.f[:, 0]], plate.v[plate.f[:, 2]] - plate.v[plate.f[:, 0]])
    mid = np.array([0, 0.0009, 0])
    plate_c = cen - mid; rad = np.hypot(plate_c[:, 0], plate_c[:, 2])
    outward = np.where(np.abs(fn[:, 1]) > np.abs(fn).max(1) * 0.9, np.sign(plate_c[:, 1]) * np.sign(fn[:, 1]),
                       np.sign((fn[:, 0] * plate_c[:, 0] + fn[:, 2] * plate_c[:, 2])) * np.where(rad > 0.013, 1, -1))
    F2 = plate.f.copy(); F2[outward < 0] = F2[outward < 0][:, ::-1]
    plate = Part(plate.v, F2, case, smooth=False)
    pivot = np.array([0.0215, 0, 0])
    parts.append(plate)
    plate2 = plate.xf(t=-pivot).xf(RY(np.radians(160))).xf(t=pivot)
    parts.append(plate2.xf(t=(0, 0.0, 0)))
    parts.append(cylinder(0.0016, 0.0042, 12, case, y0=-0.0003).xf(t=pivot))
    # barrel sits inside the first plate's hole
    return parts

def payment_terminal():
    L, W, H = 0.168, 0.078, 0.046
    parts = [rounded_box((W, 0.020, L), 0.008, 'PlasticGrey', k=6).xf(t=(0, 0.010, 0))]        # charging base
    body = rounded_box((W - 0.004, 0.026, L - 0.006), 0.009, 'PlasticBlack', k=6)
    tilt = np.radians(12)
    parts.append(body.xf(RX(tilt), t=(0, 0.031, 0.0)))
    top = rounded_box((W - 0.012, 0.0012, L - 0.020), 0.0005, 'TerminalTop', k=1, uv_mode=None)
    top.uv = np.c_[top.v[:, 0] / (W - 0.012) + 0.5, 0.5 - top.v[:, 2] / (L - 0.020)]   # screen toward -Z (back), keypad toward the customer (+Z)
    parts.append(top.xf(RX(tilt), t=(0, 0.031, 0.0)).xf(t=np.array([0, 0.013, 0]) @ RX(tilt).T))
    # receipt roll bulge at the back and paper tongue
    parts.append(lathe([(0.0, -0.034), (0.022, -0.034), (0.022, 0.034), (0.0, 0.034)], 24, 'PlasticBlack').xf(RZ(np.pi / 2), t=(0, 0.040, -0.064)))
    parts.append(rounded_box((0.056, 0.0003, 0.030), 0.0001, 'PaperWhite', k=1).xf(RX(-np.radians(55)), t=(0, 0.064, -0.072)))
    return parts

def pos_tablet():
    parts = [lathe([(0, 0), (0.060, 0), (0.062, 0.004), (0.058, 0.010), (0.016, 0.014), (0, 0.014)], 48, 'SteelBrushed')]
    parts.append(cylinder(0.012, 0.13, 24, 'SteelBrushed', y0=0.012))
    head = rounded_box((0.040, 0.030, 0.030), 0.006, 'SteelBrushed', k=3).xf(t=(0, 0.150, 0))
    parts.append(head)
    tilt = RX(np.radians(60))
    tab = rounded_box((0.250, 0.0095, 0.172), 0.006, 'PlasticBlack', k=4)
    scr = rounded_box((0.232, 0.0006, 0.154), 0.0002, 'PosScreen', k=1, uv_mode=None)
    scr.uv = np.c_[scr.v[:, 0] / 0.232 + 0.5, 0.5 - scr.v[:, 2] / 0.154]
    off = np.array([0, 0.150, 0]) + tilt @ np.array([0, 0.0205, 0])
    parts.append(tab.xf(tilt, t=off))
    parts.append(scr.xf(tilt, t=off + tilt @ np.array([0, 0.0049, 0])))
    return parts

def gem_parcel(n=12, seed=7):
    """opened diamond paper: white outside, blue liner up, loose brilliants on it."""
    rng = np.random.default_rng(seed)
    Wd, Ld = 0.110, 0.072; nx, nz = 22, 15
    xs = np.linspace(-Wd / 2, Wd / 2, nx + 1); zs = np.linspace(-Ld / 2, Ld / 2, nz + 1)
    V = []; UV = []
    for z in zs:
        for x in xs:
            fold = 0.0012 * (np.abs(np.abs(x) - Wd / 6) < Wd / nx * 0.6) + 0.0008 * (np.abs(np.abs(z) - Ld / 4) < Ld / nz * 0.6)
            lift = 0.004 * (np.abs(x) / (Wd / 2)) ** 4 + 0.003 * (np.abs(z) / (Ld / 2)) ** 4
            V.append([x, 0.0004 + fold + lift, z]); UV.append([0.5 + 0.5 * (x / Wd + 0.5), 1 - (z / Ld + 0.5)])
    V = np.array(V); F = grid_faces(nz + 1, nx + 1)
    top = orient_up(Part(V, F, 'GemPaper', uv=np.array(UV), smooth=False))
    bot = top.xf(t=(0, -0.0001, 0)).flip()   # 0.1 mm paper; bot.uv = np.c_[(top.uv[:, 0] - 0.5), top.uv[:, 1]]; bot.mat = 'GemPaper'
    parts = [top, bot]
    stones = []
    for k in range(n):
        d = rng.uniform(2.6, 4.2) * MM
        g = round_brilliant(d, 'Diamond')
        dep = -g.v[:, 1].min()
        if rng.random() < 0.5:   # lying table-down or tilted on a facet
            g = g.xf(RX(np.pi), t=(0, 0, 0)); base_y = g.v[:, 1].min()
        else:
            g = g.xf(RX(np.radians(rng.uniform(55, 75))) @ RY(rng.uniform(0, TAU))); base_y = g.v[:, 1].min()
        x, z = rng.normal(0, 0.012), rng.normal(0, 0.008)
        g = g.xf(RY(rng.uniform(0, TAU)), t=(x, -base_y, z))          # lowest point at y = 0
        lift = (paper_height(top, g.v[:, 0], g.v[:, 2]) - g.v[:, 1]).max()
        stones.append(g.xf(t=(0, lift + 0.00002, 0)))                  # resting on the paper (20 um)
    parts.append(merge(stones, 'Diamond'))
    return parts

def paper_height(part, x, z):
    """height of a (single-valued, upward) triangle sheet under the points (x, z); -inf outside."""
    A, B, Cc = part.v[part.f[:, 0]], part.v[part.f[:, 1]], part.v[part.f[:, 2]]
    h = np.full(len(x), -np.inf)
    for i in range(len(x)):
        d = (B[:, 2] - Cc[:, 2]) * (A[:, 0] - Cc[:, 0]) + (Cc[:, 0] - B[:, 0]) * (A[:, 2] - Cc[:, 2])
        ok = np.abs(d) > 1e-15
        w0 = ((B[:, 2] - Cc[:, 2]) * (x[i] - Cc[:, 0]) + (Cc[:, 0] - B[:, 0]) * (z[i] - Cc[:, 2])) / np.where(ok, d, 1)
        w1 = ((Cc[:, 2] - A[:, 2]) * (x[i] - Cc[:, 0]) + (A[:, 0] - Cc[:, 0]) * (z[i] - Cc[:, 2])) / np.where(ok, d, 1)
        w2 = 1 - w0 - w1
        m = ok & (w0 >= -1e-9) & (w1 >= -1e-9) & (w2 >= -1e-9)
        if m.any(): h[i] = (w0 * A[:, 1] + w1 * B[:, 1] + w2 * Cc[:, 1])[m].max()
    return h

def orient_up(p):
    fn = np.cross(p.v[p.f[:, 1]] - p.v[p.f[:, 0]], p.v[p.f[:, 2]] - p.v[p.f[:, 0]])
    return p.flip() if fn[:, 1].sum() < 0 else p
