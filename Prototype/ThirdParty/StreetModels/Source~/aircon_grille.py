"""Build AirconUnit_AirconGrille_Color.png (512 x 512 RGBA) for Poly Haven exterior_aircon_unit (CC0).
Poly Haven ships no colour map for the grille material (exterior_aircon_unit_02): its glTF uses the opacity mask
exterior_aircon_unit_01_opacity_1k.jpg as base colour. Here:
  A   = that opacity mask, resized to 512 (LANCZOS); cut-out coverage of the rear wire mesh is kept (0.29 -> 0.30)
  RGB = plausible material colours per UV region of the grille atlas (checked by rendering a colour-coded atlas):
        metallic texels (B of exterior_aircon_unit_02_arm > 0.5): aluminium condenser fins, light grey
        left half, non-metal: dark gaps between fins / coil
        top right: fan blades, dark grey plastic
        bottom right: rear coil-guard wire mesh, dark coated steel
        x cavity shading from the authored normal map (flat = 1, steep = darker) and grime from the roughness channel
usage: python3 -P aircon_grille.py TEXTURES_DIR OUT.png"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

T, out = sys.argv[1], sys.argv[2]
N = 512
rs = lambda f, mode='RGB': Image.open(T + '/' + f).convert(mode).resize((N, N), Image.LANCZOS)
alpha = np.asarray(rs('exterior_aircon_unit_01_opacity_1k.jpg', 'L')).astype(np.float32) / 255
arm = np.asarray(rs('exterior_aircon_unit_02_arm_1k.jpg')).astype(np.float32) / 255
nrm = np.asarray(rs('exterior_aircon_unit_02_nor_gl_1k.jpg')).astype(np.float32) / 255
rough, metal = arm[..., 1], arm[..., 2]

yy, xx = np.mgrid[0:N, 0:N]
left = xx < N // 2
top_right = (~left) & (yy < N // 2)
alu = np.array([0.76, 0.77, 0.78]); gap = np.array([0.10, 0.10, 0.10])
fan = np.array([0.11, 0.11, 0.115]); wire = np.array([0.16, 0.16, 0.16])
col = np.zeros((N, N, 3), np.float32)
col[left] = gap
col[top_right] = fan
col[(~left) & (~top_right)] = wire
m = ndimage.gaussian_filter(np.clip((metal - 0.35) / 0.3, 0, 1), 0.6)[..., None]
col = col * (1 - m) + alu * m

nz = np.clip(nrm[..., 2] * 2 - 1, 0, 1)
cavity = 0.72 + 0.28 * nz ** 2
g = rough - ndimage.uniform_filter(rough, 41)
grime = np.clip(1.0 - 0.6 * g, 0.8, 1.15)
lowfreq = ndimage.gaussian_filter(np.random.default_rng(7).standard_normal((N, N)), 18)
lowfreq = 1.0 + 0.06 * lowfreq / (np.abs(lowfreq).max() + 1e-6)
rgb = np.clip(col * (cavity * grime * lowfreq)[..., None], 0, 1)
rgba = np.dstack([(rgb * 255 + 0.5).astype(np.uint8), (np.clip(alpha, 0, 1) * 255 + 0.5).astype(np.uint8)])
Image.fromarray(rgba, 'RGBA').save(out, optimize=True)
cov = float((alpha > 0.5).mean())
print('saved', out, 'alpha<128 fraction %.3f' % (1 - cov), 'rgb mean', (rgb.mean(axis=(0, 1)) * 255).round(1))
