# Jewellery scenes for the Aurel & Finch posters (project-original).
import numpy as np
from jrt import *


def studio(variant='warm'):
    boxes = [
        dict(dir=(0.0, 0.45, -1.0), w=60, h=4, color=(1, 1, 1), mult=0.08),
        dict(dir=(0.9, 0.6, 0.4), w=10, h=40, color=(1, 1, 1), mult=0.05),
        dict(dir=(0.05, 1.0, 0.25), w=38, h=22, color=(1.0, 0.97, 0.92), power=2.6, edge=0.03, falloff=0.45),
        dict(dir=(-1.0, 0.35, 0.35), w=7, h=34, color=(1.0, 0.98, 0.95), power=5.0, edge=0.01),
        dict(dir=(1.0, 0.30, -0.15), w=5, h=30, color=(1.0, 0.97, 0.93), power=4.0, edge=0.01),
        dict(dir=(0.15, 0.25, 1.0), w=28, h=9, color=(1.0, 0.95, 0.88), power=0.9, edge=0.04),
        dict(dir=(-0.35, 0.55, -1.0), w=14, h=10, color=(1.0, 0.96, 0.9), power=2.2, edge=0.02),
        dict(dir=(0.6, 0.05, 0.8), w=3, h=12, color=(1.0, 0.9, 0.75), power=3.0, edge=0.01),
        dict(dir=(-0.7, -0.1, 0.7), w=10, h=3, color=(0.9, 0.92, 1.0), power=1.2, edge=0.02),
    ]
    return Studio(boxes, ambient=(0.02, 0.018, 0.016), dome=((0.55, 0.53, 0.50), 0.05, 0.8))


def solitaire_scene(env, metal='gold18', yaw=-0.55, D=6.5):
    r_in, t, w = 8.6, 1.65, 2.2
    Rmid = r_in + t / 2
    Rout = r_in + t
    C = np.array([0, Rout, 0], F)
    Ry = rot_y(yaw)
    RyT = Ry.T
    gem_y = Rout + 0.43 * D + 0.55  # girdle height above ring centre (local)
    R = D / 2
    # prong layout (local coordinates, ring axis along z)
    prongs = []
    for k in range(6):
        f = np.radians(60 * k + 30)
        cf, sf = np.cos(f), np.sin(f)
        base = np.array([0.95 * cf, Rout - 0.6, 0.95 * sf], F)
        mid = np.array([(R + 0.22) * cf, gem_y + 0.05, (R + 0.22) * sf], F)
        tip = np.array([(R - 0.30) * cf, gem_y + 0.62, (R - 0.30) * sf], F)
        prongs.append((base, mid, tip))

    def fn(p):
        q = (p - C) @ Ry  # world -> local (rotation transpose applied as row vectors)
        ang = np.arctan2(q[:, 0], q[:, 1])  # 0 at top
        top = np.cos(ang)
        ww = w * (1 - 0.18 * np.clip(top, 0, 1) ** 3)
        tt = t * (1 - 0.10 * np.clip(top, 0, 1) ** 3)
        rr = np.sqrt(q[:, 0] ** 2 + q[:, 1] ** 2)
        band = sd_round_box2(rr - Rmid, q[:, 2], tt / 2, ww / 2, 0.55)
        head = np.full(q.shape[0], 1e9, F)
        for base, mid, tip in prongs:
            dpr = np.minimum(sd_capsule(q, base, mid, 0.40), sd_capsule(q, mid, tip, 0.36))
            head = np.minimum(head, dpr)
        # basket gallery rings
        qb = q - np.array([0, gem_y - 1.35, 0], F)
        head = np.minimum(head, sd_torus_xz(qb, R * 0.62, 0.28))
        qb2 = q - np.array([0, gem_y - 2.35, 0], F)
        head = np.minimum(head, sd_torus_xz(qb2, R * 0.34, 0.30))
        dist = smin(band, head, 0.6)
        return dist.astype(F), np.zeros(q.shape[0], np.int32)

    gem_c = C + np.array([0, gem_y, 0], F) @ RyT.T if False else C + Ry @ np.array([0, gem_y, 0], F)
    gem = Gem(gem_c, Ry @ rot_y(0.2), D)
    sdf = SDFScene(fn, (C + np.array([0, 2.5, 0], F), Rout + 9.0), [metal])
    sc = Scene(env, sdf=sdf, gems=[gem], ground=dict(y=0.0, refl=0.30, fade=26.0, center=(0, 0)))
    return sc, C


if __name__ == '__main__':
    import sys, time
    from PIL import Image
    env = studio()
    sc, C = solitaire_scene(env)
    t0 = time.time()
    img, al = render(sc, cam_pos=(0, 38, 52), target=(0, 13, 0), fov_deg=28, W=320, H=320, ss=1,
                     bg=lambda u, v: np.zeros((u.size, 3), F))
    print('time', time.time() - t0)
    x = tonemap(img, 1.0)
    Image.fromarray((x * 255).astype(np.uint8)).save(sys.argv[1])


def pearl_scene(env, n=60, r=3.6, gap=0.25):
    # strand lying on the surface as a loose loop crossing at the clasp, plus a tail
    pts = []
    # parametric loop: superellipse-ish curve with an inward tuck
    ts = np.linspace(0, 1, 4000)
    a, b = 36.0, 40.0
    ang = 2 * np.pi * ts * 0.93 + 0.35
    x = a * np.cos(ang) + 6 * np.sin(2 * ang)
    z = b * np.sin(ang) - 4 * np.cos(3 * ang)
    seg = np.sqrt(np.diff(x) ** 2 + np.diff(z) ** 2)
    s = np.concatenate([[0], np.cumsum(seg)])
    step = 2 * r + gap
    for k in range(int(s[-1] // step)):
        i = np.searchsorted(s, k * step)
        pts.append((x[i], r, z[i]))
    pts = pts[:n]
    centers = np.array(pts, F)
    radii = np.full(len(pts), r, F) * (1 + 0.02 * np.sin(np.arange(len(pts)) * 1.7))
    centers[:, 1] = radii
    # clasp: small gold box + ring between first and last pearl
    p0, p1 = centers[0], centers[-1]
    mid = (p0 + p1) / 2
    mid[1] = 2.2
    dirv = (p1 - p0); dirv[1] = 0; dirv = dirv / np.linalg.norm(dirv)

    def fn(p):
        q = p - mid
        # clasp barrel: capsule along dirv
        d1 = sd_capsule(p, mid - dirv * 3.2, mid + dirv * 3.2, 1.9)
        # ring loops to the pearls
        d2 = sd_capsule(p, p0 + dirv * 0 + np.array([0, -1.4, 0], F), mid - dirv * 3.0, 0.45)
        d3 = sd_capsule(p, p1 + np.array([0, -1.4, 0], F), mid + dirv * 3.0, 0.45)
        return np.minimum(np.minimum(d1, d2), d3).astype(F), np.zeros(p.shape[0], np.int32)

    sdf = SDFScene(fn, (mid, 9.0), ['gold18'])
    sc = Scene(env, sdf=sdf, spheres=Spheres(centers, radii),
               pearl=dict(diffuse=1.15, lustre=3.2, color=(0.96, 0.89, 0.80), overtone=(1.0, 0.93, 0.93), overtone2=(0.90, 0.97, 1.0)),
               ground=dict(y=0.0, refl=0.28, fade=70.0, center=(0, 0)))
    return sc


def sd_band_local(q, Rmid, t, w, rr=0.7):
    rad = np.sqrt(q[:, 0] ** 2 + q[:, 1] ** 2)
    return sd_round_box2(rad - Rmid, q[:, 2], t / 2, w / 2, rr)


def bands_scene(env):
    # his & hers wedding bands: yellow gold (front) and platinum with satin centre (behind)
    A = dict(c=np.array([-7.5, 10.4, 6.0], F), R=rot_y(-0.75) @ rot_x(0.0), r_in=8.7, t=1.7, w=3.2)
    B = dict(c=np.array([7.0, 11.9, -5.0], F), R=rot_y(0.55), r_in=9.9, t=2.0, w=5.2)

    def fn(p):
        qa = (p - A['c']) @ A['R']
        qb = (p - B['c']) @ B['R']
        da = sd_band_local(qa, A['r_in'] + A['t'] / 2, A['t'], A['w'], 0.75)
        db = sd_band_local(qb, B['r_in'] + B['t'] / 2, B['t'], B['w'], 0.8)
        # satin centre stripe on B (material 2) vs polished edges (material 1)
        matb = np.where(np.abs(qb[:, 2]) < 1.15, 2, 1)
        mat = np.where(da < db, 0, matb).astype(np.int32)
        return np.minimum(da, db).astype(F), mat

    sdf = SDFScene(fn, (np.array([0, 11, 0], F), 26.0), ['gold18', 'platinum', 'platinum_satin'])
    return Scene(env, sdf=sdf, ground=dict(y=0.0, refl=0.30, fade=45.0, center=(0, 0)))


def pendant_scene(env, chain_top=70.0, spread=46.0):
    rot = rot_x(np.radians(90))  # gem table faces +z (camera)
    Ds = 7.0
    sapph = Gem(np.array([0, 0, 0], F), rot @ rot_y(0.1), Ds, ior=(1.760, 1.768, 1.780),
                sigma=(0.46, 0.27, 0.03), tint=(0.9, 0.95, 1.0))
    gems = [sapph]
    nh = 18
    Rh = Ds / 2 + 0.95
    for k in range(nh):
        a = 2 * np.pi * k / nh
        c = np.array([Rh * np.cos(a), Rh * np.sin(a), -0.35], F)
        gems.append(Gem(c, rot @ rot_y(a), 1.35))
    bail_c = np.array([0, Rh + 2.35, -0.8], F)
    top = bail_c + np.array([0, 1.2, 0], F)
    L = np.array([-spread, chain_top, -6.0], F)
    Rr = np.array([spread, chain_top, -6.0], F)

    def chain(p, a, b):
        pa = p - a
        ba = b - a
        L2 = ba @ ba
        h = np.clip((pa @ ba) / L2, 0, 1)
        s = h * np.sqrt(L2)
        rad = 0.30 + 0.06 * np.sin(s * 2 * np.pi / 1.05)
        return np.linalg.norm(pa - h[:, None] * ba, axis=1) - rad

    def fn(p):
        # halo plate (white gold) behind the small stones
        q = p - np.array([0, 0, -1.25], F)
        halo = sd_torus_xy(q, Rh, 0.95)
        cup = sd_torus_xy(p - np.array([0, 0, -0.35], F), Ds / 2 + 0.05, 0.42)
        qb = p - bail_c
        # bail: torus in the y-z plane (axis x)
        qq = np.sqrt(qb[:, 1] ** 2 + qb[:, 2] ** 2) - 1.35
        bail = np.sqrt(qq * qq + qb[:, 0] ** 2) - 0.48
        link = sd_capsule(p, np.array([0, Rh + 0.6, -0.9], F), bail_c - np.array([0, 1.2, 0], F), 0.45)
        metal = np.minimum(np.minimum(halo, cup), np.minimum(bail, link))
        ch = np.minimum(chain(p, top, L), chain(p, top, Rr))
        d = np.minimum(metal, ch)
        return d.astype(F), np.zeros(p.shape[0], np.int32)

    sdf = SDFScene(fn, (np.array([0, chain_top / 2, -3], F), np.sqrt(spread ** 2 + (chain_top / 2) ** 2) + 12), ['whitegold'])
    return Scene(env, sdf=sdf, gems=gems)
