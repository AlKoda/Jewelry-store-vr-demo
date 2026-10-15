"""Shared tiling materials for the project-made InteriorModels (512 px, seamless). Writes Shared_<Name>_* files
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
and returns a dict used by the model generators. All procedural, project-owned."""
import numpy as np
from proc_lib import *

def build():
    M = {}
    px = 512
    # black textured ABS plastic (monitor/keyboard/chair shell): fine stipple
    st = tileable_noise(px, 64, 1, 3, 0.6) * 0.6 + tileable_noise(px, 16, 2, 2) * 0.4
    col = np.dstack([0.040 + 0.012 * st] * 3); col[..., 2] += 0.004
    M['PlasticBlack'] = dict(maps=save_set('Shared_PlasticBlack', col, st, 1.2, 0.0, 0.42), tiling_m=0.12)
    # warm white appliance plastic: very light orange peel
    op = tileable_noise(px, 40, 3, 3, 0.55)
    col = np.dstack([0.86 + 0.02 * op, 0.86 + 0.02 * op, 0.84 + 0.02 * op])
    M['PlasticWhite'] = dict(maps=save_set('Shared_PlasticWhite', col, op, 0.8, 0.0, 0.58), tiling_m=0.2)
    # charcoal upholstery fabric: basket weave
    y, x = np.mgrid[0:px, 0:px].astype(np.float32)
    n = 32  # threads per tile
    wx = np.sin(x / px * n * 2 * np.pi); wy = np.sin(y / px * n * 2 * np.pi)
    checker = (np.floor(x / px * n) + np.floor(y / px * n)) % 2
    h = np.where(checker > 0, np.abs(wx), np.abs(wy)) * 0.7 + tileable_noise(px, 128, 4, 2) * 0.3
    fl = tileable_noise(px, 8, 5, 3)
    base = np.array([0.105, 0.105, 0.115])
    col = base[None, None] * (0.75 + 0.35 * h[..., None]) * (0.9 + 0.2 * fl[..., None])
    M['FabricCharcoal'] = dict(maps=save_set('Shared_FabricCharcoal', col, h, 2.5, 0.0, 0.06), tiling_m=0.08)
    # chair-back mesh: elastomeric weave with see-through diamonds (dark base, lighter strands)
    n = 24
    a = np.abs(np.sin((x + y) / px * n * np.pi)); b = np.abs(np.sin((x - y) / px * n * np.pi))
    strand = np.clip(np.maximum(a, b) * 1.3 - 0.3, 0, 1)
    hole = 1 - strand
    col = np.dstack([0.02 + 0.07 * strand] * 3)
    M['MeshWeave'] = dict(maps=save_set('Shared_MeshWeave', col, strand, 3.0, 0.0, 0.25), tiling_m=0.05)
    # brushed stainless steel: streaks along U
    streak = np.asarray(Image.fromarray((tileable_noise(px, 256, 6, 2) * 255).astype(np.uint8)).resize((px // 64, px), Image.BILINEAR).resize((px, px), Image.BICUBIC), np.float32) / 255
    streak = np.tile(streak[:, : px // 64 + 1].mean(1, keepdims=True), (1, px)) * 0.5 + tileable_noise(px, 6, 7, 2) * 0.5
    fine = np.random.default_rng(8).random((px, 1)).astype(np.float32)
    fine = np.tile(fine, (1, px))
    h = streak * 0.5 + fine * 0.5
    col = np.dstack([0.60 + 0.08 * h, 0.61 + 0.08 * h, 0.62 + 0.08 * h])
    M['BrushedSteel'] = dict(maps=save_set('Shared_BrushedSteel', col, fine * 0.4, 1.0, 1.0, 0.52 + 0.12 * streak, mpx=256), tiling_m=0.25)
    # light grey powder-coated steel (filing cabinet, DVR lid): orange peel + very slight mottling
    op = tileable_noise(px, 48, 9, 3, 0.55); mot = tileable_noise(px, 4, 10, 2)
    col = np.dstack([0.56 + 0.02 * mot, 0.57 + 0.02 * mot, 0.58 + 0.02 * mot])
    M['PowderCoatGrey'] = dict(maps=save_set('Shared_PowderCoatGrey', col, op, 1.0, 0.0, 0.40), tiling_m=0.3)
    # constants (no textures)
    M['Chrome'] = dict(Kd=(0.80, 0.80, 0.82), metallic=1.0, smoothness=0.92)
    M['Aluminium'] = dict(Kd=(0.76, 0.77, 0.78), metallic=1.0, smoothness=0.7)
    M['Rubber'] = dict(Kd=(0.035, 0.035, 0.035), metallic=0.0, smoothness=0.25)
    M['GlossBlack'] = dict(Kd=(0.02, 0.02, 0.022), metallic=0.0, smoothness=0.88)
    return M

def mat(M, key, **extra):
    m = dict(M[key]); maps = m.pop('maps', None); out = {}
    if maps:
        out['maps'] = {k: v for k, v in maps.items() if k in ('Color', 'Normal', 'MetallicSmoothness')}
        for k in ('metallic', 'smoothness'):
            if k in maps: out[k] = maps[k]
        out['tiling'] = 'world-scale UVs, %.2f m per tile' % m.get('tiling_m', 1)
    else:
        out.update({k: v for k, v in m.items() if k != 'tiling_m'})
    out.update(extra)
    return out
