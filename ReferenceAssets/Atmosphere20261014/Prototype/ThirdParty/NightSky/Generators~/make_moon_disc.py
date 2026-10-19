import sys, numpy as np
from PIL import Image
src, dst, N = sys.argv[1], sys.argv[2], int(sys.argv[3])
m = np.asarray(Image.open(src).convert('RGB')).astype(np.float64) / 255.0
# sRGB -> linear for filtering
lin = np.where(m <= 0.04045, m / 12.92, ((m + 0.055) / 1.055) ** 2.4)
H, W, _ = lin.shape
SS = 4  # supersampling per axis
R = N / 2 - 3.0  # disc radius in px, 3 px transparent margin
acc = np.zeros((N, N, 3)); cov = np.zeros((N, N))
offs = (np.arange(SS) + 0.5) / SS
yy, xx = np.mgrid[0:N, 0:N].astype(np.float64)
for oy in offs:
    for ox in offs:
        x = (xx + ox - N / 2) / R
        y = -(yy + oy - N / 2) / R
        r2 = x * x + y * y
        inside = r2 < 1.0
        z = np.sqrt(np.clip(1 - r2, 0, 1))
        lat = np.arcsin(np.clip(y, -1, 1))
        lon = np.arctan2(x, z)
        u = (lon + np.pi) / (2 * np.pi) * W - 0.5
        v = (np.pi / 2 - lat) / np.pi * H - 0.5
        u0 = np.floor(u).astype(int); v0 = np.floor(v).astype(int)
        fu = (u - u0)[..., None]; fv = (v - v0)[..., None]
        u0m = np.mod(u0, W); u1m = np.mod(u0 + 1, W)
        v0c = np.clip(v0, 0, H - 1); v1c = np.clip(v0 + 1, 0, H - 1)
        c = (lin[v0c, u0m] * (1 - fu) * (1 - fv) + lin[v0c, u1m] * fu * (1 - fv) +
             lin[v1c, u0m] * (1 - fu) * fv + lin[v1c, u1m] * fu * fv)
        # full-moon photometry is nearly flat; add a slight limb darkening (about 8 percent at the very edge)
        shade = 0.92 + 0.08 * z
        acc += np.where(inside[..., None], c * shade[..., None], 0)
        cov += inside
cov_n = cov / (SS * SS)
col = np.where(cov[..., None] > 0, acc / np.maximum(cov[..., None], 1), 0)
# gentle desaturation (the eye sees the night-time moon nearly neutral) and normalise peak to 0.95
g = col @ np.array([0.2126, 0.7152, 0.0722])
col = g[..., None] + (col - g[..., None]) * 0.6
col = col / np.percentile(col[cov_n > 0.99], 99.5) * 0.92
col = np.clip(col, 0, 1)
srgb = np.where(col <= 0.0031308, col * 12.92, 1.055 * col ** (1 / 2.4) - 0.055)
rgba = np.dstack([srgb, cov_n])
Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), 'RGBA').save(dst, optimize=True)
print(dst)
