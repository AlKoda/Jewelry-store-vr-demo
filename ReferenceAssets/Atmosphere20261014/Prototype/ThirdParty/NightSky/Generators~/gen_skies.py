"""Procedural light-polluted city night skies (equirectangular, linear radiance, Radiance .hdr).

Convention (matches Unity Skybox/Panoramic, latitude-longitude layout):
  image top = zenith, u = 0.25 -> +Z (north), u = 0.5 -> +X (east), u = 0.75 -> -Z (south).
Radiance scale matched to Poly Haven cobblestone_street_night (lamp-lit pavement ~0.13).

usage: gen_skies.py <bsc5-short.json> <out_clear.hdr> <out_overcast.hdr> <preview_dir>
"""
import sys, os, json, math
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from hdrio import write_hdr, read_hdr, preview
from PIL import Image

RNG_SEED = 20261014


# ---------------------------------------------------------------- noise
class Perlin2D:
    def __init__(self, seed):
        rng = np.random.default_rng(seed)
        self.perm = np.concatenate([rng.permutation(256)] * 2)
        ang = rng.uniform(0, 2 * np.pi, 256)
        self.g = np.stack([np.cos(ang), np.sin(ang)], axis=1)

    def __call__(self, x, y):
        xi = np.floor(x).astype(np.int64); yi = np.floor(y).astype(np.int64)
        xf = x - xi; yf = y - yi
        xi &= 255; yi &= 255
        p = self.perm
        def grad(ix, iy, dx, dy):
            h = p[p[ix] + iy]
            g = self.g[h]
            return g[..., 0] * dx + g[..., 1] * dy
        n00 = grad(xi, yi, xf, yf)
        n10 = grad(xi + 1, yi, xf - 1, yf)
        n01 = grad(xi, yi + 1, xf, yf - 1)
        n11 = grad(xi + 1, yi + 1, xf - 1, yf - 1)
        fx = xf * xf * xf * (xf * (xf * 6 - 15) + 10)
        fy = yf * yf * yf * (yf * (yf * 6 - 15) + 10)
        a = n00 + fx * (n10 - n00); b = n01 + fx * (n11 - n01)
        return a + fy * (b - a)  # about [-0.7, 0.7]


def fbm(noise, x, y, octaves, footprint, lacunarity=2.03, gain=0.5, rot=0.5):
    """Band-limited fBm: octaves fade out where the pixel footprint exceeds the octave wavelength."""
    s = np.zeros_like(x); norm = 0.0
    amp = 1.0; f = 1.0
    c, sn = math.cos(rot), math.sin(rot)
    for o in range(octaves):
        w = np.clip(1.5 - footprint * f * 2.0, 0.0, 1.0)  # fade when footprint*freq > 0.25
        s += amp * w * noise(x * f + 17.3 * o, y * f - 9.1 * o)
        norm += amp
        x, y = c * x - sn * y, sn * x + c * y
        f *= lacunarity; amp *= gain
    return s / norm


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------- geometry
def grid(W, H):
    j = (np.arange(H) + 0.5) / H
    i = (np.arange(W) + 0.5) / W
    alt = (np.pi / 2 - j * np.pi)[:, None] * np.ones((1, W))
    az = ((i - 0.25) * 2 * np.pi)[None, :] * np.ones((H, 1))  # 0 = north (+Z), pi/2 = east (+X)
    return alt, az


def blackbody_rgb(T):
    """Approximate linear-sRGB colour of a blackbody (Tanner Helland fit, linearised), luminance-normalised."""
    t = T / 100.0
    r = 255.0 if t <= 66 else 329.698727446 * (t - 60) ** -0.1332047592
    g = (99.4708025861 * math.log(t) - 161.1195681661) if t <= 66 else 288.1221695283 * (t - 60) ** -0.0755148492
    b = 255.0 if t >= 66 else (0.0 if t <= 19 else 138.5177312231 * math.log(t - 10) - 305.0447927307)
    c = np.clip(np.array([r, g, b]) / 255.0, 0, 1)
    c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return c / max(1e-6, c @ np.array([0.2126, 0.7152, 0.0722]))


def parse_ra(s):
    h, m, sec = s.replace('h', ' ').replace('m', ' ').replace('s', ' ').split()
    return (float(h) + float(m) / 60 + float(sec) / 3600) * 15.0


def parse_dec(s):
    sign = -1 if s.strip().startswith('-') else 1
    t = s.strip().lstrip('+-').replace('°', ' ').replace('′', ' ').replace('″', ' ').split()
    return sign * (float(t[0]) + float(t[1]) / 60 + float(t[2]) / 3600)


# ---------------------------------------------------------------- clear sky with stars
def clear_sky(W, H, stars_json):
    alt, az = grid(W, H)
    sa = np.sin(np.clip(alt, 0, None))
    alt_deg = np.degrees(alt)
    perlin = Perlin2D(RNG_SEED)

    zenith = np.array([0.0019, 0.0022, 0.0041])          # deep blue-grey
    glow_col = np.array([1.0, 0.74, 0.47])               # sodium + 3000-4000 K LED mixture
    glow_col = glow_col / (glow_col @ np.array([0.2126, 0.7152, 0.0722]))
    city_az = np.radians(205.0)                          # brightest light dome towards SSW (the city centre)
    dome = 1.0 + 0.55 * np.cos(az - city_az) + 0.15 * np.cos(2 * (az - city_az))
    a = np.clip(alt_deg, 0, None)
    glow = 0.030 * np.exp(-a / 9.0) + 0.0075 * np.exp(-a / 32.0)
    sky = zenith[None, None, :] * (1.0 + 0.6 * np.exp(-a / 25.0))[..., None] + (glow * dome)[..., None] * glow_col

    # thin, sparse cirrus/haze patches lit from below by the city (only above the horizon)
    up = np.clip(sa, 0.02, None)
    px, pz = np.cos(alt) * np.sin(az) / up, np.cos(alt) * np.cos(az) / up
    foot = (np.pi / H) / up ** 2 * 0.9
    n = fbm(perlin, px * 0.9, pz * 0.35, 7, foot)
    n2 = fbm(perlin, px * 2.7 + 40, pz * 1.1 - 13, 5, foot * 3)
    cirrus = smoothstep(0.08, 0.38, n + 0.25 * n2) * (alt_deg > 0)
    cirrus *= 0.55 + 0.45 * smoothstep(5, 30, alt_deg)        # fade into the haze at the horizon
    cloud_lum = (0.0045 + 1.25 * glow * dome)[..., None] * glow_col
    sky = sky * (1 - 0.35 * cirrus[..., None]) + cloud_lum * cirrus[..., None] * 0.6
    transmit = 1.0 - 0.8 * cirrus                               # stars dim behind cirrus

    # stars from the Yale Bright Star Catalogue, mid-October around 23:00 local time, latitude 51.5 N
    lat = np.radians(51.5)
    lst = 23.2 * 15.0
    data = json.load(open(stars_json, encoding='utf-8'))
    bg_lum = sky @ np.array([0.2126, 0.7152, 0.0722])
    bg_zen = float(np.median(bg_lum[:int(H * 0.04)]))
    sigma0 = 0.55 * np.pi / H
    stars_img = np.zeros_like(sky)
    count = 0
    for s in data:
        V = float(s['V'])
        if V > 5.2:
            continue
        ra = math.radians(parse_ra(s['RA'])); dec = math.radians(parse_dec(s['Dec']))
        ha = math.radians(lst) - ra
        sin_alt = math.sin(dec) * math.sin(lat) + math.cos(dec) * math.cos(lat) * math.cos(ha)
        s_alt = math.asin(max(-1, min(1, sin_alt)))
        if s_alt < math.radians(1.0):
            continue
        cos_az = (math.sin(dec) - math.sin(s_alt) * math.sin(lat)) / (math.cos(s_alt) * math.cos(lat))
        s_az = math.acos(max(-1, min(1, cos_az)))
        if math.sin(ha) > 0:
            s_az = 2 * math.pi - s_az
        alt_d = math.degrees(s_alt)
        X = 1.0 / (math.sin(s_alt) + 0.50572 * (alt_d + 6.07995) ** -1.6364)  # Kasten-Young airmass
        m_eff = V + 0.35 * (X - 1.0)                                        # hazy urban extinction
        jj = int((math.pi / 2 - s_alt) / math.pi * H)
        ii = int(((s_az / (2 * math.pi)) + 0.25) % 1.0 * W)
        bg = float(bg_lum[min(jj, H - 1), ii % W])
        m_lim = 4.7 - 2.5 * math.log10(max(bg / bg_zen, 1.0)) * 0.55     # naked-eye limit under the light dome
        vis = float(smoothstep(m_lim + 0.1, m_lim - 0.7, np.array(m_eff)))
        if vis <= 0:
            continue
        peak = 1.6 * 10 ** (-0.4 * m_eff) * vis
        sig = sigma0 * (1.0 + 0.18 * max(0.0, 2.0 - m_eff))
        col = blackbody_rgb(float(s.get('K') or 6000))
        col = 1.0 + (col - 1.0) * 0.45   # colours only faintly perceptible at night
        sd = np.array([math.cos(s_alt) * math.sin(s_az), math.sin(s_alt), math.cos(s_alt) * math.cos(s_az)])
        rj = 4 if m_eff > 2.0 else 14   # wide window for bright stars so the halo is not clipped
        ri = int(math.ceil(rj / max(math.cos(s_alt), 0.05)))
        j0, j1 = max(jj - rj, 0), min(jj + rj + 1, H)
        cols = np.arange(ii - ri, ii + ri + 1) % W
        pa, pz_ = alt[j0:j1][:, cols], az[j0:j1][:, cols]
        d = np.stack([np.cos(pa) * np.sin(pz_), np.sin(pa), np.cos(pa) * np.cos(pz_)], axis=-1)
        ang = np.arccos(np.clip(d @ sd, -1, 1))
        prof = np.exp(-0.5 * (ang / sig) ** 2) + 0.012 * np.exp(-ang / (sig * 4)) * np.clip(1 - ang / (rj * np.pi / H), 0, 1)  # core + faint halo
        stars_img[j0:j1, cols] += (peak * prof)[..., None] * col
        count += 1
    sky = sky + stars_img * transmit[..., None]

    ground = np.array([0.0052, 0.0047, 0.0042])
    below = alt_deg < 0
    horizon = sky[np.argmin(np.abs(alt_deg[:, 0])), :, :]
    blend = np.exp(np.clip(alt_deg, None, 0) / 1.5)[..., None]
    sky = np.where(below[..., None], ground + (horizon[None, :, :] - ground) * blend, sky)
    return sky.astype(np.float32), count


# ---------------------------------------------------------------- overcast sky
def overcast_sky(W, H):
    alt, az = grid(W, H)
    alt_deg = np.degrees(alt)
    perlin = Perlin2D(RNG_SEED + 7)
    a = np.clip(alt_deg, 0, None)
    base_col = np.array([1.0, 0.76, 0.53])
    base_col = base_col / (base_col @ np.array([0.2126, 0.7152, 0.0722]))
    lz = 0.0125                                                  # zenith luminance (city centre under the cloud deck)
    shape = 0.45 + 0.55 * (1 - np.exp(-a / 14.0))
    city_az = np.radians(205.0)
    dome = 1.0 + 0.12 * np.cos(az - city_az)
    base = (lz * shape * dome)[..., None] * base_col

    up = np.clip(np.sin(np.clip(alt, 0, None)), 0.015, None)
    px, pz = np.cos(alt) * np.sin(az) / up, np.cos(alt) * np.cos(az) / up
    foot = (np.pi / H) / up ** 2 * 0.8
    # stratocumulus deck: large rolls stretched along a wind direction plus billow detail
    wind = 0.6
    c, s = math.cos(wind), math.sin(wind)
    qx, qz = c * px - s * pz, s * px + c * pz
    rolls = fbm(perlin, qx * 0.32, qz * 0.95, 4, foot * 0.95)
    billow = fbm(perlin, px * 1.1 + 11, pz * 1.1 - 5, 7, foot * 1.1)
    fine = fbm(perlin, px * 3.6 - 3, pz * 3.6 + 8, 5, foot * 3.6)
    dens = 0.5 + 1.0 * rolls + 0.55 * billow + 0.12 * fine
    thick = smoothstep(0.0, 1.0, dens)
    # thicker cloud reflects more city light back down (brighter); thinner parts read darker
    mod = 0.74 + 0.46 * thick
    sky = base * mod[..., None]
    # rare thin patches where the deck almost breaks: darker and slightly bluer
    gap = smoothstep(0.02, -0.25, dens)
    sky = sky * (1 - 0.35 * gap[..., None]) + gap[..., None] * np.array([0.0010, 0.0011, 0.0016])
    # horizon haze: detail dissolves into an even glow
    haze = np.exp(-a / 5.0)[..., None]
    sky = sky * (1 - haze) + haze * (base * 1.02)

    ground = np.array([0.0060, 0.0052, 0.0045])
    below = alt_deg < 0
    horizon = sky[np.argmin(np.abs(alt_deg[:, 0])), :, :]
    blend = np.exp(np.clip(alt_deg, None, 0) / 1.5)[..., None]
    sky = np.where(below[..., None], ground + (horizon[None, :, :] - ground) * blend, sky)
    return sky.astype(np.float32)


def dither(img, amount=0.004, seed=RNG_SEED):
    """Tiny multiplicative grain (like sensor noise) so 8-bit RGBE mantissas do not band."""
    rng = np.random.default_rng(seed)
    g = rng.normal(0.0, amount, img.shape[:2])
    g[img.shape[0] // 2:] = 0.0  # sky half only; the ground half stays flat (smaller file)
    return (img * (1.0 + g)[..., None]).astype(np.float32)


if __name__ == '__main__':
    stars_json, out_clear, out_over, prev = sys.argv[1:5]
    sky, n = clear_sky(2048, 1024, stars_json)
    sky = dither(sky)
    write_hdr(out_clear, sky, 'Project-generated city night sky (clear, Yale BSC5 stars), CC0')
    print('clear sky stars rendered:', n)
    ov = dither(overcast_sky(1024, 512), seed=RNG_SEED + 1)
    write_hdr(out_over, ov, 'Project-generated city night sky (overcast), CC0')
    for path, name in ((out_clear, 'clear'), (out_over, 'overcast')):
        rgb, _ = read_hdr(path)
        for e in (8.0, 25.0):
            Image.fromarray(preview(rgb, exposure=e)).save(os.path.join(prev, f'{name}_exp{int(e)}.jpg'), quality=90)
