"""Gemstone cuts built as convex hulls of their facet vertices (flat shaded). Girdle plane at y=0, table up (+Y)."""
import numpy as np
from meshlib import convex, TAU

def brilliant_points(D, table=0.57, crown_deg=34.5, pav_deg=40.75, girdle=0.03, star=0.5, lgf=0.77, n=8):
    R = D / 2; hg = girdle * D / 2
    ta, pa = np.tan(np.radians(crown_deg)), np.tan(np.radians(pav_deg))
    step = TAU / (2 * n)                       # 22.5 deg for n = 8
    rT = table * R / np.cos(step)              # table octagon circumradius
    P = []
    hc = hg + (R - rT) * ta                    # table height
    for k in range(n):                          # table corners at main angles
        a = 2 * k * step; P.append([rT * np.sin(a), hc, rT * np.cos(a)])
    s = rT * np.cos(step) + star * (R - rT * np.cos(step))
    for k in range(n):                          # star tips between mains
        a = (2 * k + 1) * step; x = s * np.cos(step)
        P.append([s * np.sin(a), hg + (R - x) * ta, s * np.cos(a)])
    for k in range(2 * n):                      # girdle top / bottom
        a = k * step
        P.append([R * np.sin(a), hg, R * np.cos(a)]); P.append([R * np.sin(a), -hg, R * np.cos(a)])
    l = R * (1 - lgf)
    l = R - lgf * R
    for k in range(n):                          # lower girdle tips
        a = (2 * k + 1) * step; lr = (R - lgf * R)  # radial distance of tip from centre
        lr = R * (1 - lgf) + 0.0
        lr = R - lgf * R
        x = lr * np.cos(step)
        P.append([lr * np.sin(a), -hg - (R - x) * pa, lr * np.cos(a)])
    P.append([0, -hg - R * pa, 0])            # culet
    return np.array(P)

def round_brilliant(D, mat, **kw):
    return convex(brilliant_points(D, **kw), mat)

def oval_brilliant(L, W, mat, **kw):
    P = brilliant_points(W, **kw); P[:, 2] *= L / W     # long axis along Z
    return convex(P, mat)

def cushion_brilliant(W, mat, p=3.0, **kw):
    P = brilliant_points(W, **kw)
    r = np.hypot(P[:, 0], P[:, 2]); a = np.arctan2(P[:, 0], P[:, 2])
    f = 1.0 / (np.abs(np.cos(a)) ** p + np.abs(np.sin(a)) ** p) ** (1 / p)   # superellipse radius factor
    P[:, 0] *= f / f.max() * 1.0; P[:, 2] *= f / f.max() * 1.0
    P[:, 0] *= 1 / np.sqrt(2) ** 0 ; 
    return convex(P, mat)

def single_cut(D, mat, table=0.6, crown_deg=33, pav_deg=41, girdle=0.03):
    """8+8 facet melee cut for tiny stones (pave, halo, tennis bracelets)."""
    R = D / 2; hg = girdle * D / 2; P = []
    rT = table * R / np.cos(TAU / 16)
    hc = hg + (R - rT * np.cos(TAU / 16)) * np.tan(np.radians(crown_deg))
    for k in range(8):
        a = k * TAU / 8; P.append([rT * np.sin(a), hc, rT * np.cos(a)])
        a2 = a + TAU / 16
        P.append([R * np.sin(a2), hg, R * np.cos(a2)]); P.append([R * np.sin(a2), -hg, R * np.cos(a2)])
    P.append([0, -hg - R * np.tan(np.radians(pav_deg)), 0])
    return convex(np.array(P), mat)

def octagon(a, b, c):
    """rectangle half sizes a (x), b (z) with 45-degree corner cut c; CCW points (x, z)"""
    return np.array([[a - c, b], [-(a - c), b], [-a, b - c], [-a, -(b - c)], [-(a - c), -b], [a - c, -b], [a, -(b - c)], [a, b - c]])

def emerald_cut(L, W, mat, corner=0.15, girdle=0.03, crown=(0.06, 0.10, 0.13), crown_inset=(0.10, 0.18, 0.24),
                pav=(0.14, 0.26, 0.37), pav_inset=(0.12, 0.26, 0.40)):
    """step cut: rings of inset octagons. Long axis along Z. heights/insets as fractions of W."""
    a, b = W / 2, L / 2; c = corner * W; hg = girdle * W / 2; P = []
    def ring(inset, y):
        d = inset * W
        for x, z in octagon(a - d, b - d, max(c - d * 0.414, 0.02 * W)):
            P.append([x, y, z])
    ring(0, hg); ring(0, -hg)
    for h, i in zip(crown, crown_inset): ring(i, hg + h * W)
    for h, i in zip(pav, pav_inset): ring(i, -hg - h * W)
    keel = (L - W) / 2 * 0.9
    P.append([0, -hg - 0.46 * W, keel]); P.append([0, -hg - 0.46 * W, -keel])
    return convex(np.array(P), mat)

def princess_cut(W, mat, girdle=0.025):
    a = W / 2; hg = girdle * W / 2; P = []
    def sq(s, y, cut=0.02):
        c = cut * W
        for x, z in octagon(s, s, c): P.append([x, y, z])
    sq(a, hg); sq(a, -hg)
    sq(a * 0.86, hg + 0.06 * W); sq(a * 0.70, hg + 0.11 * W)          # crown bevels and table
    # pavilion: chevron rings rotated alternately produce the X pattern of a princess
    for s, d, r in ((0.80, 0.14, 0), (0.55, 0.32, 1), (0.30, 0.46, 0)):
        y = -hg - d * W
        if r == 0:
            sq(a * s, y, 0.01)
        else:
            for k in range(4):
                ang = k * TAU / 4
                P.append([a * s * np.sin(ang), y, a * s * np.cos(ang)])
    P.append([0, -hg - 0.58 * W, 0])
    return convex(np.array(P), mat)

def pear_brilliant(L, W, mat, **kw):
    P = brilliant_points(W, **kw)
    z = P[:, 2] / (W / 2)                       # -1..1 along length
    # taper the +Z half towards a point
    t = np.clip(z, 0, 1)
    P[:, 0] *= 1 - 0.75 * t ** 1.6
    P[:, 2] = np.where(P[:, 2] > 0, P[:, 2] * (L / W) * 1.15, P[:, 2] * 1.0)
    P[:, 2] -= (P[:, 2].max() + P[:, 2].min()) / 2
    return convex(P, mat)
