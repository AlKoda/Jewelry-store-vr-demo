# Small numpy ray tracer for jewellery product shots (project-original code).
# Units: millimetres, y up. Metals are signed-distance fields (sphere traced),
# gems are convex polyhedra (analytic, dispersive, internal reflections),
# pearls are analytic spheres. Environment is a procedural photo studio.
import numpy as np

F = np.float32


def norm(v):
    return v / np.maximum(np.linalg.norm(v, axis=-1, keepdims=True), 1e-12)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def rot_y(a):
    c, s = np.cos(a), np.sin(a)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]], F)


def rot_x(a):
    c, s = np.cos(a), np.sin(a)
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]], F)


def rot_z(a):
    c, s = np.cos(a), np.sin(a)
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]], F)


# ---------------------------------------------------------------- environment
class Studio:
    """Dark studio with rectangular soft boxes (gnomonic rectangles)."""

    def __init__(self, boxes, ambient=(0.004, 0.004, 0.005), floor=0.0, hdr=None, hdr_gain=0.0, hdr_rot=0.0,
                 dome=None):
        self.dome = dome
        self.boxes = []
        for b in boxes:
            c = norm(np.array(b['dir'], F))
            up0 = np.array(b.get('up', (0, 1, 0)), F)
            if abs(np.dot(up0, c)) > 0.95:
                up0 = np.array((0, 0, 1), F)
            right = norm(np.cross(up0, c))
            up = norm(np.cross(c, right))
            if 'roll' in b:
                a = np.radians(b['roll'])
                right, up = right * np.cos(a) + up * np.sin(a), -right * np.sin(a) + up * np.cos(a)
            self.boxes.append((c, right, up, np.tan(np.radians(b['w'])), np.tan(np.radians(b['h'])),
                               np.array(b['color'], F) * b.get('power', 1.0), b.get('edge', 0.02), b.get('falloff', 0.35),
                               b.get('mult', None)))
        self.ambient = np.array(ambient, F)
        self.floor = floor
        self.hdr = hdr
        self.hdr_gain = hdr_gain
        self.hdr_rot = hdr_rot

    def __call__(self, d, soft=0.0):
        d = d.astype(F)
        n = d.shape[0]
        y = d[:, 1:2]
        col = np.broadcast_to(self.ambient, (n, 3)) * (0.6 + 0.8 * np.clip(y, 0, 1))
        if self.dome is not None:
            dc, y0, y1 = self.dome
            col = col + np.array(dc, F) * smoothstep(y0, y1 + soft * 0.5, y)
        for c, right, up, tu, tv, color, edge, falloff, mult in self.boxes:
            cd = d @ c
            front = cd > 0.05
            cdc = np.maximum(cd, 0.05)
            u = (d @ right) / cdc
            v = (d @ up) / cdc
            e = edge + soft
            mu = smoothstep(tu + e, tu - e, np.abs(u))
            mv = smoothstep(tv + e, tv - e, np.abs(v))
            grad = 1.0 - falloff * np.clip((u / tu) ** 2 + (v / tv) ** 2, 0, 2) * 0.5
            # widen energy-conserving-ish: blurred boxes get dimmer
            k = (tu * tv) / ((tu + soft) * (tv + soft))
            if mult is not None:
                m = (mu * mv * front)[:, None]
                col = col * (1 - m * (1 - mult))
                continue
            m = (mu * mv * grad * front * k)[:, None]
            col = col + m * color
        if self.hdr is not None and self.hdr_gain > 0:
            H, W, _ = self.hdr.shape
            phi = np.arctan2(d[:, 0], -d[:, 2]) + self.hdr_rot
            th = np.arccos(np.clip(d[:, 1], -1, 1))
            ui = ((phi / (2 * np.pi) + 0.5) % 1.0 * W).astype(np.int32) % W
            vi = np.clip((th / np.pi * H).astype(np.int32), 0, H - 1)
            col = col + self.hdr[vi, ui] * self.hdr_gain
        return col

    def irradiance(self, nrm, samples=48, seed=1):
        # cosine-weighted Monte Carlo with a fixed sample set (smooth, deterministic)
        rng = np.random.default_rng(seed)
        u1 = rng.random(samples).astype(F)
        u2 = rng.random(samples).astype(F)
        r = np.sqrt(u1)
        th = 2 * np.pi * u2
        loc = np.stack([r * np.cos(th), np.sqrt(1 - u1), r * np.sin(th)], -1)  # y is normal axis
        t1 = norm(np.cross(nrm, np.where(np.abs(nrm[:, 1:2]) < 0.9, [[0, 1, 0]], [[1, 0, 0]]).astype(F)))
        t2 = np.cross(nrm, t1)
        acc = np.zeros_like(nrm)
        for i in range(samples):
            dd = t1 * loc[i, 0] + nrm * loc[i, 1] + t2 * loc[i, 2]
            acc += self(dd, soft=0.15)
        return acc / samples


# ---------------------------------------------------------------- gems
def brilliant_planes(D=1.0, table=0.57, crown=34.5, pav=40.8, girdle=0.03, star=21.5, ug=41.5, lg=42.0, twist=0.0):
    R = D / 2.0
    g = girdle * D / 2.0
    ca, pa = np.radians(crown), np.radians(pav)
    yt = g + (R - table * D / 2) * np.tan(ca)
    N, Dd = [], []

    def add(n, d):
        n = np.array(n, np.float64)
        ln = np.linalg.norm(n)
        N.append(n / ln)
        Dd.append(d / ln)

    add((0, 1, 0), yt)
    for k in range(8):
        f = np.radians(45 * k) + twist
        add((np.sin(ca) * np.cos(f), np.cos(ca), np.sin(ca) * np.sin(f)), np.sin(ca) * R + np.cos(ca) * g)
        # pavilion main through the culet
        p = g + R * np.tan(pa)
        add((np.sin(pa) * np.cos(f), -np.cos(pa), np.sin(pa) * np.sin(f)), p * np.cos(pa))
        # star facet
        s = np.radians(star)
        ps = f + np.radians(22.5)
        rs = table * D / 2 * np.cos(np.radians(22.5))
        add((np.sin(s) * np.cos(ps), np.cos(s), np.sin(s) * np.sin(ps)), np.sin(s) * rs + np.cos(s) * yt)
    for k in range(16):
        f = np.radians(22.5 * k + 11.25) + twist
        a = np.radians(ug)
        add((np.sin(a) * np.cos(f), np.cos(a), np.sin(a) * np.sin(f)), np.sin(a) * R + np.cos(a) * g)
        b = np.radians(lg)
        add((np.sin(b) * np.cos(f), -np.cos(b), np.sin(b) * np.sin(f)), np.sin(b) * R + np.cos(b) * g)
    for k in range(32):
        f = np.radians(11.25 * k + 5.625) + twist
        add((np.cos(f), 0, np.sin(f)), R)
    return np.array(N, F), np.array(Dd, F), yt, g + R * np.tan(pa)


class Gem:
    def __init__(self, center, rot, D, ior=(2.405, 2.420, 2.445), sigma=(0.0, 0.0, 0.0), tint=(1, 1, 1), **cut):
        n, d, top, bottom = brilliant_planes(D, **cut)
        rot = np.array(rot, F)
        self.n = (n @ rot.T).astype(F)
        c = np.array(center, F)
        self.d = (d + self.n @ c).astype(F)
        self.center = c
        self.radius = float(np.sqrt((D / 2) ** 2 + max(top, bottom) ** 2)) * 1.02
        self.ior = ior
        self.sigma = np.array(sigma, F)
        self.tint = np.array(tint, F)

    def intersect(self, o, d):
        n = o.shape[0]
        t0 = np.full(n, -1e9, F)
        t1 = np.full(n, 1e9, F)
        idx = np.zeros(n, np.int32)
        ok = np.ones(n, bool)
        for i in range(self.n.shape[0]):
            ni = self.n[i]
            den = d @ ni
            num = self.d[i] - o @ ni
            with np.errstate(divide='ignore', invalid='ignore'):
                t = num / den
            ent = den < -1e-9
            ext = den > 1e-9
            par = ~(ent | ext)
            ok &= ~(par & (num < 0))
            upd = ent & (t > t0)
            t0 = np.where(upd, t, t0)
            idx = np.where(upd, i, idx)
            t1 = np.where(ext & (t < t1), t, t1)
        hit = ok & (t0 <= t1) & (t0 > 1e-4)
        return np.where(hit, t0, np.inf).astype(F), idx

    def exit(self, o, d):
        n = o.shape[0]
        t1 = np.full(n, 1e9, F)
        idx = np.zeros(n, np.int32)
        for i in range(self.n.shape[0]):
            ni = self.n[i]
            den = d @ ni
            num = self.d[i] - o @ ni
            with np.errstate(divide='ignore', invalid='ignore'):
                t = num / den
            upd = (den > 1e-9) & (t < t1) & (t > 1e-5)
            t1 = np.where(upd, t, t1)
            idx = np.where(upd, i, idx)
        return t1, idx


def fresnel_dielectric(cosi, eta_i, eta_t):
    cosi = np.clip(cosi, 0, 1)
    sint = eta_i / eta_t * np.sqrt(np.maximum(0, 1 - cosi * cosi))
    tir = sint >= 1
    cost = np.sqrt(np.maximum(0, 1 - sint * sint))
    rs = (eta_i * cosi - eta_t * cost) / (eta_i * cosi + eta_t * cost + 1e-9)
    rp = (eta_i * cost - eta_t * cosi) / (eta_i * cost + eta_t * cosi + 1e-9)
    f = 0.5 * (rs * rs + rp * rp)
    return np.where(tir, 1.0, f).astype(F), tir


def reflect(d, n):
    return d - 2 * np.sum(d * n, -1, keepdims=True) * n


def refract(d, n, eta):
    # n faces against d; eta = n_i / n_t
    cosi = -np.sum(d * n, -1, keepdims=True)
    k = 1 - eta * eta * (1 - cosi * cosi)
    return norm(eta * d + (eta * cosi - np.sqrt(np.maximum(k, 0))) * n)


def shade_gem(gem, p, d, nrm, env, bounces=9):
    out = np.zeros((p.shape[0], 3), F)
    cosi = np.clip(-np.sum(d * nrm, -1), 0, 1)
    for c in range(3):
        ior = gem.ior[c]
        Fr, _ = fresnel_dielectric(cosi, 1.0, ior)
        rd = reflect(d, nrm)
        out[:, c] += Fr * env(rd)[:, c]
        T = (1 - Fr)
        dirc = refract(d, nrm, 1.0 / ior)
        pos = p + dirc * 1e-4
        alive = np.ones(p.shape[0], bool)
        for b in range(bounces):
            t, idx = gem.exit(pos, dirc)
            T = T * np.exp(-gem.sigma[c] * t)
            pos = pos + dirc * t[:, None]
            nn = gem.n[idx]  # outward
            ci = np.clip(np.sum(dirc * nn, -1), 0, 1)
            F2, tir = fresnel_dielectric(ci, ior, 1.0)
            # transmitted part
            tdir = refract(dirc, -nn, ior)
            tr = np.where(tir, 0.0, (1 - F2) * T)
            out[:, c] += tr * env(tdir)[:, c] * gem.tint[c]
            T = T * F2
            dirc = reflect(dirc, nn)
            pos = pos - nn * 1e-4
        out[:, c] += T * 0.15 * env(dirc)[:, c]
    return out


# ---------------------------------------------------------------- sdf helpers
def sd_capsule(p, a, b, r):
    pa = p - a
    ba = (b - a)
    h = np.clip((pa @ ba) / (ba @ ba), 0, 1)
    return np.linalg.norm(pa - h[:, None] * ba, axis=1) - r


def sd_torus_xy(p, R, r):
    # torus around z axis
    q = np.sqrt(p[:, 0] ** 2 + p[:, 1] ** 2) - R
    return np.sqrt(q * q + p[:, 2] ** 2) - r


def sd_torus_xz(p, R, r):
    q = np.sqrt(p[:, 0] ** 2 + p[:, 2] ** 2) - R
    return np.sqrt(q * q + p[:, 1] ** 2) - r


def sd_round_box2(qx, qy, bx, by, r):
    dx = np.abs(qx) - bx + r
    dy = np.abs(qy) - by + r
    return np.sqrt(np.maximum(dx, 0) ** 2 + np.maximum(dy, 0) ** 2) + np.minimum(np.maximum(dx, dy), 0) - r


def smin(a, b, k):
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0, 1)
    return b * (1 - h) + a * h - k * h * (1 - h)


class SDFScene:
    """sdf(p) -> (dist, material index). bound = (center, radius)."""

    def __init__(self, fn, bound, materials):
        self.fn = fn
        self.bc = np.array(bound[0], F)
        self.br = float(bound[1])
        self.materials = materials

    def dist(self, p):
        return self.fn(p)[0]

    def normal(self, p, e=0.004):
        ex = np.array([e, 0, 0], F)
        ey = np.array([0, e, 0], F)
        ez = np.array([0, 0, e], F)
        nx = self.dist(p + ex) - self.dist(p - ex)
        ny = self.dist(p + ey) - self.dist(p - ey)
        nz = self.dist(p + ez) - self.dist(p - ez)
        return norm(np.stack([nx, ny, nz], -1))

    def march(self, o, d, iters=220, eps=0.0015):
        n = o.shape[0]
        oc = o - self.bc
        b = np.sum(oc * d, -1)
        c = np.sum(oc * oc, -1) - self.br ** 2
        disc = b * b - c
        hitb = disc > 0
        sq = np.sqrt(np.maximum(disc, 0))
        t0 = np.maximum(-b - sq, 0)
        t1 = -b + sq
        tout = np.full(n, np.inf, F)
        idx = np.nonzero(hitb & (t1 > 0))[0]
        t = t0[idx].astype(F)
        tend = t1[idx]
        for _ in range(iters):
            if idx.size == 0:
                break
            p = o[idx] + d[idx] * t[:, None]
            dist = self.dist(p)
            hit = dist < eps
            tout[idx[hit]] = t[hit]
            t = t + np.maximum(dist, 0) * 0.85
            keep = (~hit) & (t < tend)
            idx, t, tend = idx[keep], t[keep], tend[keep]
        return tout

    def ao(self, p, n, steps=5, scale=0.35):
        occ = np.zeros(p.shape[0], F)
        w = 1.0
        for i in range(1, steps + 1):
            h = scale * i
            occ += w * np.maximum(h - self.dist(p + n * h), 0)
            w *= 0.55
        return np.clip(1 - 1.6 * occ / scale, 0.15, 1)


class Spheres:
    def __init__(self, centers, radii):
        self.c = np.array(centers, F)
        self.r = np.array(radii, F)

    def intersect(self, o, d):
        n = o.shape[0]
        tbest = np.full(n, np.inf, F)
        ib = np.full(n, -1, np.int32)
        for i in range(self.c.shape[0]):
            oc = o - self.c[i]
            b = np.sum(oc * d, -1)
            cc = np.sum(oc * oc, -1) - self.r[i] ** 2
            disc = b * b - cc
            m = disc > 0
            t = -b - np.sqrt(np.maximum(disc, 0))
            upd = m & (t > 1e-4) & (t < tbest)
            tbest = np.where(upd, t, tbest)
            ib = np.where(upd, i, ib)
        return tbest, ib


# ---------------------------------------------------------------- scene / render
METALS = {
    'gold18': (np.array([0.98, 0.74, 0.36], F), 0.012),
    'gold_satin': (np.array([0.98, 0.74, 0.36], F), 0.10),
    'rose': (np.array([0.98, 0.66, 0.52], F), 0.014),
    'platinum': (np.array([0.70, 0.67, 0.63], F), 0.012),
    'platinum_satin': (np.array([0.70, 0.67, 0.63], F), 0.12),
    'whitegold': (np.array([0.80, 0.79, 0.77], F), 0.010),
}


class Scene:
    def __init__(self, env, sdf=None, gems=(), spheres=None, pearl=None, ground=None, bg=None):
        self.env = env
        self.sdf = sdf
        self.gems = list(gems)
        self.spheres = spheres
        self.pearl = pearl or {}
        self.ground = ground  # dict(y=0, refl=0.25, fade=40, center=(x,z))
        self.bg = bg

    def trace(self, o, d, use_ground=True):
        n = o.shape[0]
        tb = np.full(n, np.inf, F)
        kind = np.zeros(n, np.int8)  # 0 miss 1 metal 2 gem 3 sphere 4 ground
        sub = np.full(n, -1, np.int32)
        if self.sdf is not None:
            t = self.sdf.march(o, d)
            u = t < tb
            tb = np.where(u, t, tb); kind[u] = 1
        for gi, g in enumerate(self.gems):
            oc = o - g.center
            b = np.sum(oc * d, -1)
            c = np.sum(oc * oc, -1) - g.radius ** 2
            cand = np.nonzero(b * b - c > 0)[0]
            if cand.size == 0:
                continue
            t, _ = g.intersect(o[cand], d[cand])
            u = t < tb[cand]
            ci = cand[u]
            tb[ci] = t[u]; kind[ci] = 2; sub[ci] = gi
        if self.spheres is not None:
            t, ib = self.spheres.intersect(o, d)
            u = t < tb
            tb = np.where(u, t, tb); kind[u] = 3; sub = np.where(u, ib, sub)
        if self.ground is not None and use_ground:
            gy = self.ground['y']
            with np.errstate(divide='ignore', invalid='ignore'):
                t = (gy - o[:, 1]) / d[:, 1]
            u = (d[:, 1] < -1e-6) & (t > 1e-3) & (t < tb)
            tb = np.where(u, t, tb); kind[u] = 4
        return tb, kind, sub

    def shade(self, o, d, depth, bgcol):
        n = o.shape[0]
        out = np.array(bgcol, F, copy=True)
        alpha = np.zeros(n, F)
        t, kind, sub = self.trace(o, d, use_ground=depth > 0)
        p = o + d * np.where(np.isfinite(t), t, 0)[:, None]
        # metal
        m = np.nonzero(kind == 1)[0]
        if m.size:
            pm = p[m]
            nm = self.sdf.normal(pm)
            _, mat = self.sdf.fn(pm)
            v = d[m]
            cosv = np.clip(-np.sum(v * nm, -1), 0, 1)[:, None]
            r = reflect(v, nm)
            col = np.zeros((m.size, 3), F)
            ao = self.sdf.ao(pm, nm)
            for mi, (name) in enumerate(self.sdf.materials):
                sel = mat == mi
                if not np.any(sel):
                    continue
                f0, rough = METALS[name]
                Fs = f0 + (1 - f0) * (1 - cosv[sel]) ** 5
                if depth > 0:
                    # one secondary bounce for self reflections
                    sec, sa = self.shade(pm[sel] + nm[sel] * 0.01, r[sel], depth - 1,
                                         self.env(r[sel], soft=rough))
                    refl = sec
                else:
                    refl = self.env(r[sel], soft=rough)
                col[sel] = Fs * refl * ao[sel, None]
            out[m] = col
            alpha[m] = 1
        g = np.nonzero(kind == 2)[0]
        if g.size:
            for gi, gem in enumerate(self.gems):
                sel = g[sub[g] == gi]
                if sel.size == 0:
                    continue
                ps = p[sel]
                # facet normal from entering plane
                _, idx = gem.intersect(o[sel], d[sel])
                nn = gem.n[idx]
                out[sel] = shade_gem(gem, ps, d[sel], nn, self.env)
                alpha[sel] = 1
        s = np.nonzero(kind == 3)[0]
        if s.size:
            out[s] = self.shade_pearl(p[s], d[s], sub[s])
            alpha[s] = 1
        gr = np.nonzero(kind == 4)[0]
        if gr.size:
            pg = p[gr]
            ng = np.tile(np.array([[0, 1, 0]], F), (gr.size, 1))
            v = d[gr]
            r = reflect(v, ng)
            cosv = np.clip(-v[:, 1], 0, 1)
            Fg, _ = fresnel_dielectric(cosv, 1.0, 1.5)
            sec, sa = self.shade(pg + ng * 0.01, r, depth - 1, bgcol[gr] * 0)
            G = self.ground
            dx = pg[:, 0] - G['center'][0]
            dz = pg[:, 2] - G['center'][1]
            fade = np.exp(-(dx * dx + dz * dz) / G['fade'] ** 2)
            k = np.clip(G['refl'] * fade * (0.75 + 0.25 * (1 - cosv) ** 3), 0, 1)[:, None]
            out[gr] = bgcol[gr] * (1 - k * sa[:, None]) + sec * k * sa[:, None] * 1.0
            alpha[gr] = (k[:, 0] * sa)
        return out, alpha

    def shade_pearl(self, p, d, si):
        P = self.pearl
        c = self.spheres.c[si]
        nrm = norm(p - c)
        q = (p - c) * 1.9 + si[:, None].astype(F) * 3.1
        bump = np.stack([np.sin(q[:, 1] * 1.3 + np.cos(q[:, 2])), np.sin(q[:, 2] * 1.1 + np.cos(q[:, 0])),
                         np.sin(q[:, 0] * 1.2 + np.cos(q[:, 1]))], -1).astype(F)
        nrm = norm(nrm + bump * P.get('bump', 0.025))
        v = -d
        cosv = np.clip(np.sum(nrm * v, -1), 0, 1)
        base = np.array(P.get('color', (0.86, 0.82, 0.76)), F)
        var = np.stack([np.sin(si * 2.3), np.sin(si * 1.7 + 1), np.sin(si * 3.1 + 2)], -1).astype(F)
        base = base[None, :] * (1 + 0.035 * var)
        irr = self.env.irradiance(nrm, samples=40)
        # occlusion by neighbour spheres + ground
        occ = np.zeros(p.shape[0], F)
        for j in range(self.spheres.c.shape[0]):
            dv = self.spheres.c[j] - p
            dist = np.linalg.norm(dv, axis=1)
            own = si == j
            cosj = np.clip(np.sum(dv * nrm, -1) / np.maximum(dist, 1e-6), 0, 1)
            occ += np.where(own, 0, (self.spheres.r[j] ** 2 / np.maximum(dist, 1e-3) ** 2) * cosj)
        if self.ground is not None:
            h = p[:, 1] - self.ground['y']
            occ += np.clip(-nrm[:, 1], 0, 1) * np.exp(-h / 2.0) * 0.6
        ao = np.clip(1 - occ, 0.1, 1)[:, None]
        # orient: angle dependent overtone
        ov = np.array(P.get('overtone', (1.0, 0.93, 0.95)), F)
        ov2 = np.array(P.get('overtone2', (0.92, 0.98, 0.96)), F)
        w = ((1 - cosv) ** 1.5)[:, None]
        tint = base * (ov * (1 - w) + ov2 * w)
        sss = (0.45 + 0.55 * cosv)[:, None]
        diffuse = tint * irr * ao * sss * P.get('diffuse', 0.62)
        # layered lustre: sharp spec + broad sheen
        r = reflect(d, nrm)
        Fs, _ = fresnel_dielectric(cosv, 1.0, 1.65)
        lust = P.get('lustre', 2.6)
        spec = self.env(r, soft=0.02) * (Fs[:, None] * lust) * (0.75 + 0.25 * tint) \
            + self.env(r, soft=0.25) * 0.10 * tint
        return diffuse + spec * ao


def render(scene, cam_pos, target, fov_deg, W, H, ss=2, bg=None, up=(0, 1, 0), chunk=600000, depth=1,
           window=None, shift=(0.0, 0.0)):
    """Return linear RGB (H,W,3) and alpha (H,W). bg(u,v) -> rgb (arrays in [0,1])."""
    cam_pos = np.array(cam_pos, F)
    fwd = norm(np.array(target, F) - cam_pos)
    right = norm(np.cross(fwd, np.array(up, F)))
    upv = np.cross(right, fwd)
    Ws, Hs = W * ss, H * ss
    tanf = np.tan(np.radians(fov_deg) / 2)
    aspect = W / H
    x0, y0, x1, y1 = window if window else (0, 0, W, H)
    xs = (np.arange(x0 * ss, x1 * ss, dtype=F) + 0.5) / Ws
    ys = (np.arange(y0 * ss, y1 * ss, dtype=F) + 0.5) / Hs
    U, V = np.meshgrid(xs, ys)
    U = U.ravel(); V = V.ravel()
    px = ((2 * U - 1) - shift[0]) * tanf * aspect
    py = ((1 - 2 * V) - shift[1]) * tanf
    dirs = norm(px[:, None] * right + py[:, None] * upv + fwd)
    out = np.zeros((U.size, 3), F)
    alpha = np.zeros(U.size, F)
    for s in range(0, U.size, chunk):
        e = min(U.size, s + chunk)
        dd = dirs[s:e]
        oo = np.broadcast_to(cam_pos, dd.shape).copy()
        bgc = bg(U[s:e], V[s:e]) if bg is not None else np.zeros((e - s, 3), F)
        o_, a_ = scene.shade(oo, dd, depth, bgc.astype(F))
        out[s:e] = o_
        alpha[s:e] = a_
    hh, ww = (y1 - y0) * ss, (x1 - x0) * ss
    img = out.reshape(hh, ww, 3)
    al = alpha.reshape(hh, ww)
    if ss > 1:
        img = img.reshape(hh // ss, ss, ww // ss, ss, 3).mean((1, 3))
        al = al.reshape(hh // ss, ss, ww // ss, ss).mean((1, 3))
    return img, al


def tonemap(x, exposure=1.0):
    x = np.maximum(x * exposure, 0)
    y = (x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14)
    y = np.clip(y, 0, 1)
    return np.where(y <= 0.0031308, 12.92 * y, 1.055 * np.power(y, 1 / 2.4) - 0.055)
