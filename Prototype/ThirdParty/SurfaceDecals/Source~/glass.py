"""Top-down broken-glass floor decals: annealed display-case shards and tempered-glass crumbs."""
import sys, os
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
from dutil import *


def shard_poly(r, cx, cy, length, aspect, ang, kind):
    if kind == 'shard':
        nv = r.choice([3, 3, 4, 4, 5])
        w = length / aspect
        # points along an elongated axis: sharp tips, straight-ish long edges
        ts = np.sort(r.uniform(-0.5, 0.5, nv))
        ts[0], ts[-1] = -0.5, 0.5
        pts = []
        for i, t in enumerate(ts):
            side = 1 if i % 2 == 0 else -1
            off = side * w * r.uniform(0.15, 0.6) * (1 - abs(t) * (0.6 if i in (0, nv - 1) else 0.2))
            if i in (0, nv - 1):
                off *= r.uniform(0.0, 0.4)
            pts.append((t * length, off))
        pts = np.array(pts)
        # order around centroid
        c = pts.mean(0)
        order = np.argsort(np.arctan2(pts[:, 1] - c[1], pts[:, 0] - c[0]))
        pts = pts[order]
    else:  # tempered crumb: chunky quad / pentagon
        nv = r.choice([4, 4, 4, 5, 6])
        angs = np.sort(r.uniform(0, 2 * np.pi, nv))
        angs = np.linspace(0, 2 * np.pi, nv, endpoint=False) + r.uniform(-0.35, 0.35, nv) + r.uniform(0, 6.28)
        rad = length / 2 * r.uniform(0.7, 1.15, nv)
        pts = np.stack([np.cos(angs) * rad * aspect ** 0.5, np.sin(angs) * rad / aspect ** 0.5], 1)
    c, s = np.cos(ang), np.sin(ang)
    R = np.array([[c, -s], [s, c]])
    return pts @ R.T + np.array([cx, cy])


def render(out_name, n, metres, kind, seed, count, impact=(0.5, 0.5), spread=0.22, out_dir='.'):
    r = rng(seed)
    ss = 2
    N = n * ss
    ppm = N / (metres * 1000.0)
    idimg = Image.new('I', (N, N), 0)
    d = ImageDraw.Draw(idimg)
    shards = []
    for i in range(count):
        # position: clustered around the impact point, with a long tail
        rad = abs(r.normal(0, spread)) if r.random() < 0.75 else r.uniform(0, 0.62)
        th = r.uniform(0, 2 * np.pi)
        x = impact[0] + np.cos(th) * rad
        y = impact[1] + np.sin(th) * rad * 0.8
        if not (0.03 < x < 0.97 and 0.03 < y < 0.97):
            continue
        if kind == 'shard':
            # size in mm: power law, larger near the impact
            L = min(110, 3 + r.pareto(1.8) * 5.5 * (1.15 - min(rad, 0.5) * 0.6))
            aspect = r.uniform(1.4, 4.5)
            ang = th + r.normal(0, 1.1)
        else:
            L = r.uniform(4, 11) * (1.0 if r.random() < 0.85 else 0.45)
            aspect = r.uniform(1.0, 1.5)
            ang = r.uniform(0, np.pi)
        if L * ppm < 2.5:
            continue
        poly = shard_poly(r, x * N, y * N, L * ppm, aspect, ang, kind)
        if poly.min() < 0.012 * N or poly.max() > 0.988 * N:
            continue
        shards.append((poly, L))
    # draw small ones first so large shards lie on top
    shards.sort(key=lambda s: s[1])
    for k, (poly, L) in enumerate(shards):
        d.polygon([tuple(p) for p in poly], fill=k + 1)
    ids = np.asarray(idimg, np.int64)
    K = len(shards) + 1
    # per shard: tilt, tint, base alpha
    tilt_x = r.normal(0, 0.06, K); tilt_y = r.normal(0, 0.06, K)
    tilt_x[0] = tilt_y[0] = 0
    tint = r.uniform(-1, 1, K)
    inside = ids > 0
    # distance to the shard's own border (edges where id changes)
    border = np.zeros_like(inside)
    border[:-1] |= ids[:-1] != ids[1:]
    border[1:] |= ids[:-1] != ids[1:]
    border[:, :-1] |= ids[:, :-1] != ids[:, 1:]
    border[:, 1:] |= ids[:, :-1] != ids[:, 1:]
    dist = ndi.distance_transform_edt(~border) / ppm  # mm
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32) / ppm
    thick = 4.0 if kind == 'shard' else 5.0
    edge_w = 1.1 if kind == 'shard' else 1.6
    bev = np.clip(dist / edge_w, 0, 1)
    height = np.where(inside, (tilt_x[ids] * xx + tilt_y[ids] * yy) % 7.0 * 0 + tilt_x[ids] * (xx - xx.mean()) * 0.0, 0)
    # plane per shard: use local coordinates relative to the shard centroid to avoid huge offsets
    import warnings
    warnings.simplefilter('ignore')
    cxs = ndi.mean(xx, ids, index=np.arange(K)); cys = ndi.mean(yy, ids, index=np.arange(K))
    cxs = np.nan_to_num(np.asarray(cxs)); cys = np.nan_to_num(np.asarray(cys))
    plane = tilt_x[ids] * (xx - cxs[ids]) + tilt_y[ids] * (yy - cys[ids])
    height = np.where(inside, thick * 0.35 + plane + thick * 0.65 * bev ** 0.6, 0.0)
    if kind == 'crumb':
        # tempered crumbs are lumpy, with a crazed top surface
        lump = fbm(N, N, 3 * ppm, 3, 0.5, seed + 5)
        height += np.where(inside, 0.2 * lump * bev, 0)
    # colour and alpha
    edge = (1 - bev) * inside
    face_a = (0.09 if kind == 'shard' else 0.16) + 0.04 * tint[ids]
    ebright = r.uniform(0.35, 1.0, K)
    a = np.where(inside, face_a + (0.66 if kind == 'shard' else 0.55) * ebright[ids] * edge ** 0.8, 0.0)
    # glass dust: fine glittering grains around the shards
    dust = (r.random((N, N)) > (0.9965 if kind == 'shard' else 0.995)).astype(np.float32)
    density = np.exp(-(((xx / (metres * 1000) - impact[0]) ** 2 + (yy / (metres * 1000) - impact[1]) ** 2) / (2 * (spread * 1.3) ** 2)))
    dust *= density > r.random((N, N)) * 0.9
    dust = ndi.gaussian_filter(dust, 0.7) * 3.5
    a = np.maximum(a, np.clip(dust, 0, 0.75))
    height = np.maximum(height, np.clip(dust, 0, 1) * 0.8)
    face_col = np.array([0.70, 0.78, 0.76]); edge_col = np.array([0.80, 0.93, 0.88])
    col = face_col[None, None] * (1 - edge[..., None]) + edge_col[None, None] * edge[..., None]
    col = col * (1 + 0.04 * tint[ids][..., None])
    # downsample
    def down(v):
        if v.ndim == 2:
            return v.reshape(n, ss, n, ss).mean((1, 3))
        return v.reshape(n, ss, n, ss, v.shape[2]).mean((1, 3))
    a_d = down(a.astype(np.float64))
    col_d = down((col * a[..., None]).astype(np.float64)) / np.maximum(a_d[..., None], 1e-4)
    h_d = down(height.astype(np.float64))
    nrm = normal_from_height(h_d, strength=0.9 * (n / (metres * 1000)) ** -1 * 0.6)
    save_rgba(os.path.join(out_dir, out_name + '.png'), col_d, border_fade(a_d, 0.03))
    save_normal(os.path.join(out_dir, out_name + '_Normal.png'), nrm)
    print(out_name, len(shards), 'pieces')


if __name__ == '__main__':
    out = sys.argv[1]
    which = sys.argv[2:] or ['shards', 'crumbs']
    if 'shards' in which:
        render('GlassShards_Scatter', 2048, 1.2, 'shard', 7, 2600, impact=(0.5, 0.42), spread=0.17, out_dir=out)
    if 'crumbs' in which:
        render('GlassCrumbs_Tempered', 1024, 0.8, 'crumb', 9, 4200, impact=(0.47, 0.5), spread=0.2, out_dir=out)
