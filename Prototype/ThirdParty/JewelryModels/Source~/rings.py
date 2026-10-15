"""Rings: finger axis along X, ring centre at the origin before grounding, stone up (+Y)."""
import numpy as np
from meshlib import Part, merge, unit, rot, TAU, uv_sphere, sweep, circle2, RY, RX, RZ
from gems import round_brilliant, oval_brilliant, single_cut

MM = 0.001

def orient(p):
    vol = np.einsum('ij,ij->i', p.v[p.f[:, 0]], np.cross(p.v[p.f[:, 1]], p.v[p.f[:, 2]])).sum()
    return p.flip() if vol < 0 else p

def section(w, t, m=16, outer_exp=0.9, inner_exp=0.35, side=0.55):
    """comfort-fit D section, closed loop, (x axial, r radial from inner surface)"""
    pts = []
    for k in range(m):
        u = TAU * k / m
        c, s = np.cos(u), np.sin(u)
        x = (w / 2) * np.sign(c) * abs(c) ** side
        e = outer_exp if s > 0 else inner_exp
        y = np.sign(s) * abs(s) ** e
        pts.append((x, t / 2 + y * t / 2))
    return np.array(pts)

def band(rin, w_fn, t_fn, mat, segs=96, m=16, **sk):
    """band swept around the X axis; w_fn/t_fn give width/thickness (m) for angle phi (0 = top)."""
    V = []
    for i in range(segs):
        ph = TAU * i / segs
        ph_s = (ph + np.pi) % TAU - np.pi
        er = np.array([0, np.cos(ph), np.sin(ph)])
        for x, r in section(w_fn(ph_s), t_fn(ph_s), m, **sk):
            V.append(np.array([x, 0, 0]) + (rin + r) * er)
    V = np.array(V)
    F = []
    for i in range(segs):
        i2 = (i + 1) % segs
        for j in range(m):
            j2 = (j + 1) % m
            a, b, c, d = i * m + j, i * m + j2, i2 * m + j2, i2 * m + j
            F += [[a, b, c], [a, c, d]]
    uv = np.array([[j / m, i / segs] for i in range(segs) for j in range(m)])
    return orient(Part(V, np.array(F), mat, uv=uv))

def catmull(P, n_per=6):
    P = np.asarray(P, float); out = []
    Pp = np.vstack([P[0] * 2 - P[1], P, P[-1] * 2 - P[-2]])
    for i in range(1, len(Pp) - 2):
        p0, p1, p2, p3 = Pp[i - 1], Pp[i], Pp[i + 1], Pp[i + 2]
        for k in range(n_per):
            t = k / n_per
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(P[-1])
    return np.array(out)

def tube(points, radius, mat, sides=8, tip=True, start_cap=True):
    """open tube along points with per-point radius; rounded tip at the end."""
    P = np.asarray(points, float); R = np.broadcast_to(np.asarray(radius, float), (len(P),)).copy()
    if tip:
        d = unit(P[-1] - P[-2]); r0 = R[-1]
        extra = [P[-1] + d * r0 * np.sin(a) for a in (0.5, 0.9, 1.25)]
        P = np.vstack([P, extra]); R = np.concatenate([R, [r0 * np.cos(0.5), r0 * np.cos(0.9), r0 * np.cos(1.25)]])
    sec = circle2(1.0, sides)
    part = sweep(P, sec, mat, closed=False, scale=R, cap=True)
    return orient(part)

def torus(R, r, mat, segs=48, sides=8, plane='xz', y=0.0):
    a = TAU * np.arange(segs) / segs
    if plane == 'xz': path = np.stack([R * np.sin(a), np.full(segs, y), R * np.cos(a)], 1)
    else: path = np.stack([np.full(segs, y), R * np.cos(a), R * np.sin(a)], 1)
    return orient(sweep(path, circle2(r, sides), mat, closed=True))

def prong_head(stone_r, girdle_y, mat, n=4, a0=np.pi / 4, prong_r=0.42 * MM, depth=2.9 * MM, crown_tip=0.75 * MM, segs_gallery=32, sides=8):
    """n claw prongs around a round stone whose girdle is at height girdle_y; plus two gallery rings."""
    parts = []
    R = stone_r
    ctrl = [(0.35 * R, -depth * 1.25), (0.65 * R, -depth * 0.78), (0.92 * R, -depth * 0.32), (R + 0.16 * MM, 0.0),
            (R - 0.02 * MM, 0.50 * MM), (R - 0.42 * MM, crown_tip)]
    for k in range(n):
        a = a0 + TAU * k / n
        pts = np.array([[rho * np.sin(a), girdle_y + h, rho * np.cos(a)] for rho, h in ctrl])
        path = catmull(pts, 3)
        rr = np.linspace(prong_r * 1.1, prong_r * 0.85, len(path))
        parts.append(tube(path, rr, mat, sides=sides))
    # gallery rings
    def rho_at(h):
        hs = [c[1] for c in ctrl[:4]]; rs = [c[0] for c in ctrl[:4]]
        return np.interp(h, hs, rs)
    for h, rr in ((-depth * 0.45, 0.30 * MM), (-depth * 0.95, 0.26 * MM)):
        parts.append(torus(rho_at(h), rr, mat, segs=segs_gallery, sides=6, y=girdle_y + h))
    return parts

def ring_solitaire(metal='Platinum', stone='Diamond', D=6.5 * MM, rin=8.55 * MM):
    t0, w0 = 1.75 * MM, 2.1 * MM
    def t_fn(ph):
        a = abs(ph)
        arch = np.exp(-((a - 0.24) / 0.13) ** 2) if a < 0.9 else 0
        return t0 + 1.35 * MM * arch
    def w_fn(ph):
        a = abs(ph); return w0 - 0.35 * MM * np.exp(-(a / 0.5) ** 2)
    shank = band(rin, w_fn, t_fn, metal, segs=120, m=14)
    culet_y = rin + t0 + 0.30 * MM
    gem = round_brilliant(D, stone)
    depth = -gem.v[:, 1].min()
    girdle_y = culet_y + depth
    gem = gem.xf(t=(0, girdle_y, 0))
    head = prong_head(D / 2, girdle_y, metal, n=4, depth=depth)
    return [shank, gem] + head

def ring_band(metal='Gold18k', w=4.0 * MM, t=1.8 * MM, rin=8.55 * MM):
    return [band(rin, lambda p: w, lambda p: t, metal, segs=96, m=18, outer_exp=1.0, inner_exp=0.45, side=0.35)]

def ring_halo(metal='Platinum', center='Sapphire', L=8.0 * MM, W=6.0 * MM, rin=8.55 * MM, melee=1.3 * MM):
    """Cathedral halo ring. The head is built as a real setting: the halo frame has a tapered gallery skirt, the
    shank shoulders rise into that skirt at both ends of the halo's minor axis, the four claws run down past the
    pavilion into the top of the shank, and an under-gallery rail ties the claws together; the sapphire's culet
    rests on the shank. Every part touches or overlaps its neighbour (no floating head)."""
    t0, w0 = 1.7 * MM, 2.0 * MM
    sh_amp, sh_c, sh_s = 1.4 * MM, 0.34, 0.15          # cathedral shoulders under the halo ends
    def t_fn(ph):
        a = abs(ph); return t0 + sh_amp * np.exp(-((a - sh_c) / sh_s) ** 2)
    shank = band(rin, lambda p: w0 - 0.2 * MM * np.exp(-(abs(p) / 0.4) ** 2), t_fn, metal, segs=120, m=14)
    gem = oval_brilliant(L, W, center)
    gem = gem.xf(RY(np.pi / 2))                 # long axis along the finger (X), north-south on the hand
    depth = -gem.v[:, 1].min()
    girdle_y = rin + t0 - 0.05 * MM + depth     # culet sits 0.05 mm into the top of the shank
    gem = gem.xf(t=(0, girdle_y, 0))
    parts = [shank, gem]
    # halo frame: oval under the girdle, melee stones on top, tapered gallery skirt below (section: x inward, y up)
    n = 18
    a = TAU * np.arange(64) / 64
    hx, hz = L / 2 + melee * 0.62, W / 2 + melee * 0.62
    frame_path = np.stack([hx * np.cos(a), np.full(64, girdle_y - 0.55 * MM), hz * np.sin(a)], 1)
    frame_sec = np.array([[-0.85, -0.3], [-0.55, -1.9], [0.45, -1.9], [0.85, -0.6], [0.95, 0.45], [0.0, 0.62], [-0.95, 0.45]]) * melee * 0.7
    frame_sec[:, 1] *= -1                      # the sweep frame's normal axis points down (-Y) on this path
    parts.append(orient(sweep(frame_path, frame_sec, metal, closed=True)))
    # melee stones evenly spaced along the halo perimeter
    per = np.cumsum(np.hypot(np.diff(np.append(frame_path[:, 0], frame_path[0, 0])), np.diff(np.append(frame_path[:, 2], frame_path[0, 2]))))
    per = np.concatenate([[0], per]); tot = per[-1]
    beads = []
    for k in range(n):
        s = tot * (k + 0.5) / n
        i = np.searchsorted(per, s) - 1; f = (s - per[i]) / max(per[i + 1] - per[i], 1e-9)
        p = frame_path[i % 64] * (1 - f) + frame_path[(i + 1) % 64] * f
        st = single_cut(melee, 'Diamond').xf(t=(p[0], girdle_y - 0.08 * MM, p[2]))
        parts.append(st)
        # two beads (shared prongs) between stones
        s2 = tot * (k + 1.0) / n
        i = np.searchsorted(per, s2) - 1; f = (s2 - per[i]) / max(per[i + 1] - per[i], 1e-9)
        q = frame_path[i % 64] * (1 - f) + frame_path[(i + 1) % 64] * f
        beads.append(uv_sphere(0.22 * MM, 6, 4, metal).xf(t=(q[0], girdle_y + 0.16 * MM, q[2])))
    parts += beads
    # four claws on the centre stone, running down past the pavilion into the top of the shank
    shank_top = rin + t0                         # top of the shank under the stone (bump is negligible there)
    rail_y = girdle_y - depth * 0.5
    rail_k = []
    for ang in (0.6, np.pi - 0.6, np.pi + 0.6, -0.6):
        ex, ez = L / 2 * np.cos(ang), W / 2 * np.sin(ang)
        rad = np.hypot(ex, ez); dirv = np.array([ex, 0, ez]) / rad
        foot = np.array([np.sign(ex) * 0.42 * MM, shank_top - 0.30 * MM, np.sign(ez) * 0.50 * MM])
        pts = [foot,
               np.array([0, girdle_y - depth * 0.8, 0]) + dirv * rad * 0.42,
               np.array([0, girdle_y - 0.6 * MM, 0]) + dirv * (rad + 0.05 * MM),
               np.array([0, girdle_y + 0.25 * MM, 0]) + dirv * (rad + 0.12 * MM),
               np.array([0, girdle_y + 0.62 * MM, 0]) + dirv * (rad - 0.35 * MM)]
        path = catmull(pts, 4)
        parts.append(tube(path, np.linspace(0.38, 0.30, len(path)) * MM, metal, sides=7))
        j = np.argmin(np.abs(path[:, 1] - rail_y)); rail_k.append(np.hypot(path[j, 0], path[j, 2]) / rad)
    # under-gallery rail through the claws at half the pavilion depth
    k = float(np.mean(rail_k)); b = TAU * np.arange(40) / 40
    rail = np.stack([L / 2 * k * np.cos(b), np.full(40, rail_y), W / 2 * k * np.sin(b)], 1)
    parts.append(orient(sweep(rail, circle2(0.24 * MM, 6), metal, closed=True)))
    return parts

def ring_eternity(metal='Platinum', rin=8.55 * MM, n=22, stone=2.0 * MM):
    w, t = 3.0 * MM, 1.9 * MM
    base = band(rin, lambda p: w, lambda p: t, metal, segs=110, m=16, outer_exp=0.6, side=0.3)
    parts = [base]
    rr = rin + t + 0.05 * MM
    for k in range(n):
        ph = TAU * (k + 0.5) / n
        st = single_cut(stone, 'Diamond')            # table up = +Y
        st = st.xf(RX(ph), t=(0, rr * np.cos(ph), rr * np.sin(ph)))
        parts.append(st)
        # shared prongs between stones: a pair of beads either side of the channel
        ph2 = TAU * (k + 1.0) / n
        for sx in (-1, 1):
            parts.append(uv_sphere(0.33 * MM, 6, 4, metal).xf(t=(sx * w * 0.36, (rr + 0.22 * MM) * np.cos(ph2), (rr + 0.22 * MM) * np.sin(ph2))))
    return parts


def ring_three_stone(metal='Gold18k', center='Ruby', Dc=6.0 * MM, Ds=4.5 * MM, rin=8.55 * MM):
    """centre stone plus two side stones following the band (across the finger, along Z)."""
    t0, w0 = 1.8 * MM, 2.3 * MM
    def t_fn(ph):
        a = abs(ph); return t0 + 0.9 * MM * np.exp(-((a - 0.36) / 0.18) ** 2)
    shank = band(rin, lambda p: w0 + 0.6 * MM * np.exp(-(abs(p) / 0.45) ** 2), t_fn, metal, segs=96, m=12)
    parts = [shank]
    for D, side, mat in ((Dc, 0, center), (Ds, -1, 'Diamond'), (Ds, 1, 'Diamond')):
        gem = round_brilliant(D, mat); depth = -gem.v[:, 1].min()
        culet_y = rin + t0 + (0.25 if side == 0 else 0.05) * MM
        gy = culet_y + depth
        unit_parts = [gem.xf(t=(0, gy, 0))] + prong_head(D / 2, gy, metal, n=4, prong_r=(0.40 if side == 0 else 0.34) * MM, depth=depth,
                                                          crown_tip=(0.7 if side == 0 else 0.55) * MM, segs_gallery=24, sides=(8 if side == 0 else 6))
        if side:
            ang = side * np.arcsin(((Dc + Ds) / 2 + 0.35 * MM) / (gy + 0.3 * MM))
            unit_parts = [p.xf(RX(ang)) for p in unit_parts]
        parts += unit_parts
    return parts
