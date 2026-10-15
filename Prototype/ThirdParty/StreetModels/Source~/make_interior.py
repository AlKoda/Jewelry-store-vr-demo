"""ApartmentBlock_Interior_Color.jpg: night room backdrops behind the ApartmentBlock windows (5 bays x 4 storeys).
Layout (unchanged, it is what ApartmentBlock.obj's Interior UVs expect): 1024 x 1024, 5 columns x 4 rows, one cell per
bay and storey; the TOP row is the ground floor; each cell is the 3 x 3 m back wall 1.4 m behind the facade.

Each cell is a soft-focus perspective view (75 deg, square in world space) re-projected from a CC0 Poly Haven interior
HDRI (Lebombo by Greg Zaal, St. Fagans Interior by Morgan Evans, Empty Warehouse 01 by Sergej Majboroda; the 1K .hdr files
from github.com/pmndrs/drei-assets), then re-lit for night by this script: daylight windows replaced by night glass,
lamp pools and the photographed wall sconces as warm light sources, colour temperature per flat, bloom, filmic tone
mapping, and per cell overlays (sheer curtains, backlit thick curtains, Venetian blinds, TV glow, dark rooms lit only
by street-light spill, dark shops with an exit-light glow). Rendered at 2x and downsampled; mild grain.
usage: python3 -P make_interior.py HDRI_DIR OUT.jpg [preview.jpg]"""
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pano

HD, OUT = sys.argv[1], sys.argv[2]
COLS, ROWS = 5, 4
S = 2                       # supersampling
CW, CH = 1024 * S // COLS, 1024 * S // ROWS   # 409 x 512 cell at 2x (3 x 3 m, non-square texels)
rng = np.random.default_rng(20261015)

KELVIN = {2700: (1.00, 0.70, 0.40), 3000: (1.00, 0.76, 0.50), 3500: (1.00, 0.83, 0.63), 4000: (1.00, 0.89, 0.75),
          5000: (0.92, 0.95, 1.00)}


def lum(a):
    return a[..., 0] * 0.2126 + a[..., 1] * 0.7152 + a[..., 2] * 0.0722


def load(name):
    P = pano.load_hdr(os.path.join(HD, name + '_1k.hdr'))
    if name == 'lebombo':
        # kitchen pass-through window (daylight greenery) -> night glass with a faint city glow
        m = np.zeros(P.shape[:2]); m[229:257, 49:83] = 1
        m = ndimage.gaussian_filter(m, 1.2)[..., None]
        night = np.array([0.010, 0.012, 0.020]) * (1 + 0.3 * rng.standard_normal(P.shape[:2] + (1,)).clip(-1, 1))
        P = P * (1 - m) + night * m
        # big window wall (yaw 185-245) and its sun patch are never cropped (yaws used: 0-150, 310-350)
    if name == 'st_fagans_interior':
        # daylight glass: door strip (yaw 85-93) and the windows behind the stairs (yaw 309-342) -> night glass
        m = np.zeros(P.shape[:2]); m[205:286, 240:267] = 1; m[205:283, 880:973] = 1
        m = ndimage.gaussian_filter(m, 1.5)[..., None]
        yy = np.arange(P.shape[0])[:, None, None]
        night = np.array([0.010, 0.012, 0.020]) * (1 + 1.5 * np.clip((yy - 205) / 80.0, 0, 1)) + np.zeros(P.shape)
        # a few distant warm lights outside (other buildings / street lamps)
        for _ in range(6):
            yy, xx = rng.integers(222, 275), rng.integers(884, 970)
            night[yy, xx] = np.array([1.2, 0.8, 0.4]) * rng.uniform(0.3, 1.0)
        night = ndimage.gaussian_filter(night, (1.2, 1.2, 0))
        P = P * (1 - m) + night * m
    return P


PANOS = {n: load(n) for n in ('lebombo', 'st_fagans_interior', 'empty_warehouse_01')}
SCONCES = {'lebombo': [(143.1, 11.3), (315.4, 12.3), (320.6, 12.3)]}


def gauss2(w, h, cx, cy, sx, sy):
    x = (np.arange(w) + 0.5) / w; y = (np.arange(h) + 0.5) / h
    X, Y = np.meshgrid(x, y)
    return np.exp(-((X - cx) ** 2 / (2 * sx ** 2) + (Y - cy) ** 2 / (2 * sy ** 2)))


def view(name, yaw, pitch=6.0, fov=75.0):
    v = pano.perspective(PANOS[name], yaw, pitch, fov, CW, CH, vfov_deg=fov)
    v = v / np.median(lum(v)) * 0.18
    # soft focus of the room itself (seen through glass from across the street); overlays at the window stay sharper
    v = ndimage.gaussian_filter(v, (2.4, 2.4, 0))
    return v, (name, yaw, pitch, fov)


def sconce_glow(cam, power=2.2):
    name, yaw, pitch, fov = cam
    g = np.zeros((CH, CW))
    for sy, sp in SCONCES.get(name, []):
        p = pano.project(yaw, pitch, fov, CW, CH, sy, sp, vfov_deg=fov)
        if p is None or not (-40 < p[0] < CW + 40 and -40 < p[1] < CH + 40):
            continue
        cx, cy = p[0] / CW, p[1] / CH
        g += power * gauss2(CW, CH, cx, cy, 0.006, 0.005) + 0.30 * gauss2(CW, CH, cx, cy + 0.015, 0.05, 0.06) \
            + 0.12 * gauss2(CW, CH, cx, cy + 0.04, 0.22, 0.26)
    return g


def lit(img, cam, kelvin=2700, lamps=(), amb=0.22, exposure=1.0, sconces=True):
    """Night re-light: ambient + lamp pools (+ sconce glows) times the photographed albedo-ish image."""
    L = np.full((CH, CW), amb)
    for cx, cy, r, k in lamps:
        L += k * gauss2(CW, CH, cx, cy, r, r * 1.1)
    # ceiling a bit darker (lamps below it), corners fall off
    y = (np.arange(CH) + 0.5) / CH
    L *= (0.75 + 0.25 * np.clip(y * 2.2, 0, 1))[:, None]
    L *= 0.8 + 0.2 * gauss2(CW, CH, 0.5, 0.55, 0.45, 0.5)
    out = img * L[..., None] * np.array(KELVIN[kelvin]) * exposure
    if sconces:
        g = sconce_glow(cam)
        out = out + g[..., None] * np.array(KELVIN[kelvin])[None, None] * 0.6 * exposure
        out = out * (1 + 0.5 * ndimage.gaussian_filter(g, 25)[..., None])
    return out


def dark(img, spill=0.05, tint=(0.55, 0.66, 1.0), tv=0.0, tv_pos=(0.25, 0.85), warm_leak=0.0, clip=0.25):
    img = np.minimum(img, clip)   # lamps and tubes are off in a dark room
    y = (np.arange(CH) + 0.5) / CH
    L = spill * (0.35 + 0.9 * np.clip((y - 0.35) / 0.65, 0, 1) ** 1.5)[:, None] * np.ones((1, CW))
    out = img * L[..., None] * np.array(tint)
    if tv:
        flick = 1 + 0.15 * rng.standard_normal()
        # flickering blue-white light from a TV out of view, falling on the walls and ceiling
        g = (gauss2(CW, CH, tv_pos[0], tv_pos[1], 0.30, 0.40) + 0.35 * gauss2(CW, CH, tv_pos[0], 0.1, 0.45, 0.2)) * tv * flick
        out = out + img * g[..., None] * np.array([0.42, 0.60, 1.0])
    if warm_leak:
        g = gauss2(CW, CH, 0.9, 0.6, 0.12, 0.4) * warm_leak
        out = out + img * g[..., None] * np.array(KELVIN[2700])
    return out


def folds(w, k=11, seed=0):
    r = np.random.default_rng(seed)
    x = (np.arange(w) + 0.5) / w
    f = np.zeros(w)
    for i in range(4):
        fr = k * (0.6 + 0.8 * r.random()) * (1 + i * 0.7)
        f += np.sin(2 * np.pi * (fr * x + r.random())) / (1 + i)
    f += 0.15 * ndimage.gaussian_filter1d(r.standard_normal(w), 2)
    f = (f - f.min()) / (f.max() - f.min())
    return f


def sheer(room, colour=(1.0, 0.97, 0.9), gap=(0.42, 0.58), trans=0.5, seed=1):
    """Translucent net curtains in front of a lit room; the room shows through blurred, the fabric glows."""
    soft = ndimage.gaussian_filter(room, (5, 5, 0))
    glow = ndimage.gaussian_filter(room, (45, 45, 0))
    f = folds(CW, 14, seed)[None, :, None]
    fabric = glow * np.array(colour) * (0.55 + 0.45 * f) * 1.25
    T = trans * (0.85 + 0.3 * (1 - f))
    x = (np.arange(CW) + 0.5) / CW
    cover = ((x < gap[0]) | (x > gap[1])).astype(float)
    cover = ndimage.gaussian_filter1d(cover, 3)[None, :, None]
    cur = T * soft + (1 - T) * fabric
    return room * (1 - cover) + cur * cover


def thick(room, colour=(0.75, 0.55, 0.35), gap=(0.47, 0.53), seed=2):
    """Lined curtains drawn almost shut, lit from inside: glow through the weave and a bright gap."""
    glow = ndimage.gaussian_filter(room, (60, 60, 0)) + 0.03
    f = folds(CW, 9, seed)[None, :, None]
    y = (np.arange(CH) + 0.5) / CH
    hem = (1 - 0.25 * np.exp(-((y - 0.02) / 0.02) ** 2))[:, None, None]
    fabric = glow * np.array(colour) * (0.25 + 0.40 * f) * hem
    x = (np.arange(CW) + 0.5) / CW
    open_ = ((x > gap[0]) & (x < gap[1])).astype(float)
    open_ = ndimage.gaussian_filter1d(open_, 2)[None, :, None]
    return fabric * (1 - open_) + room * open_


def blinds(room, pitch=9, open_frac=0.28, raised=0.0, colour=(0.95, 0.93, 0.88), seed=3):
    """Venetian blinds lit from inside; slats tilted, room visible through the gaps; optionally raised from the bottom."""
    soft = ndimage.gaussian_filter(room, (2, 2, 0))
    glow = ndimage.gaussian_filter(room, (35, 35, 0))
    # slats are lit by the lamps; take the room's light colour at half saturation (a green wall must not tint them)
    gl = lum(glow)[..., None]
    glow = 0.5 * glow + 0.5 * gl * np.array(room.reshape(-1, 3).mean(0) / max(lum(room.reshape(-1, 3).mean(0)), 1e-6))
    yy = np.arange(CH)
    ph = (yy % pitch) / pitch
    slat = (ph < 1 - open_frac).astype(float)
    shade = (0.75 + 0.35 * np.clip(ph / (1 - open_frac), 0, 1))  # lit edge towards the gap
    sl = (glow * np.array(colour) * 0.95 * shade[:, None, None])
    a = slat[:, None, None] * np.ones((1, CW, 1))
    out = sl * a + soft * 0.9 * (1 - a)
    if raised > 0:
        y = (yy + 0.5) / CH
        keep = (y < 1 - raised).astype(float)
        rail = np.exp(-((y - (1 - raised)) / 0.006) ** 2)
        out = out * keep[:, None, None] + room * (1 - keep)[:, None, None]
        out = out * (1 - 0.7 * rail)[:, None, None]
    return out


def exit_light(img, cx=0.72, cy=0.18):
    g = gauss2(CW, CH, cx, cy, 0.035, 0.016)
    box = (g > 0.6).astype(float)
    halo = ndimage.gaussian_filter(box, 14) * 0.6 + ndimage.gaussian_filter(box, 50) * 0.25
    return img + (box * 1.4 + halo)[..., None] * np.array([0.15, 1.0, 0.45])


def led(img, cx, cy, col=(1.0, 0.1, 0.05)):
    g = gauss2(CW, CH, cx, cy, 0.004, 0.003)
    return img + (g * 2.0 + ndimage.gaussian_filter(g, 6) * 2)[..., None] * np.array(col)


def tonemap(x, exposure=1.0):
    x = np.maximum(x * exposure, 0)
    x = ndimage.gaussian_filter(x, (0.8, 0.8, 0))
    bright = np.maximum(x - 0.9, 0)
    x = x + 0.35 * ndimage.gaussian_filter(bright, (9, 9, 0)) + 0.15 * ndimage.gaussian_filter(bright, (30, 30, 0))
    a, b, c, d, e = 2.51, 0.03, 2.43, 0.59, 0.14          # ACES filmic fit (Narkowicz)
    y = np.clip((x * (a * x + b)) / (x * (c * x + d) + e), 0, 1)
    return y ** (1 / 2.2)


# ------------------------------------------------------------------ cells (row 0 = ground floor = top of the image)
def cell(r, c):
    if r == 0:
        if c == 2:   # lit entrance hall / stair lobby, cool-white fluorescent
            v, cam = view('st_fagans_interior', 312, 2.0, 80)
            return tonemap(lit(v, cam, 4000, lamps=[(0.5, 0.25, 0.35, 0.6)], amb=0.55), 1.15)
        yaw = {0: 60, 1: 250, 3: 120, 4: 5}[c]
        v, cam = view('empty_warehouse_01', yaw, 0.0, 80)
        out = dark(v, spill=0.06, tint=(0.5, 0.62, 1.0), clip=0.8)
        if c == 1:
            out = exit_light(out, 0.70, 0.16)
        if c == 4:
            out = led(out, 0.18, 0.12)
        if c == 0:
            out = exit_light(out, 0.25, 0.20)
        return tonemap(out, 1.0)
    spec = {
        (1, 0): ('lit', 'lebombo', 140, dict(kelvin=2700, lamps=[(0.2, 0.75, 0.22, 0.9)], amb=0.3)),
        (1, 1): ('dark', 'lebombo', 20, dict(spill=0.05)),
        (1, 2): ('sheer', 'lebombo', 330, dict(kelvin=3000, lamps=[(0.75, 0.7, 0.25, 0.7)], amb=0.3)),
        (1, 3): ('tv', 'st_fagans_interior', 75, dict(tv=0.35, tv_pos=(0.25, 0.6))),
        (1, 4): ('blinds', 'st_fagans_interior', 60, dict(kelvin=3500, lamps=[(0.5, 0.3, 0.4, 0.5)], amb=0.35)),
        (2, 0): ('dark', 'st_fagans_interior', 40, dict(spill=0.045)),
        (2, 1): ('lit', 'st_fagans_interior', 75, dict(kelvin=3500, lamps=[(0.35, 0.25, 0.3, 0.6), (0.8, 0.3, 0.25, 0.4)], amb=0.35)),
        (2, 2): ('dark', 'lebombo', 320, dict(spill=0.04, warm_leak=0.05)),
        (2, 3): ('thick', 'lebombo', 120, dict(kelvin=2700, lamps=[(0.5, 0.7, 0.3, 1.0)], amb=0.3)),
        (2, 4): ('dark', 'lebombo', 140, dict(spill=0.05)),
        (3, 0): ('blinds_half', 'lebombo', 0, dict(kelvin=2700, lamps=[(0.3, 0.8, 0.25, 0.8)], amb=0.3)),
        (3, 1): ('dark', 'lebombo', 100, dict(spill=0.04)),
        (3, 2): ('lit', 'lebombo', 120, dict(kelvin=3000, lamps=[(0.85, 0.75, 0.22, 0.6)], amb=0.3)),
        (3, 3): ('tv', 'lebombo', 60, dict(tv=0.3, tv_pos=(0.7, 0.55))),
        (3, 4): ('dark', 'lebombo', 345, dict(spill=0.045)),
    }
    kind, name, yaw, kw = spec[(r, c)]
    v, cam = view(name, yaw, 6.0, 75)
    if kind in ('dark', 'tv'):
        return tonemap(dark(v, **kw), 1.0)
    room = lit(v, cam, **kw)
    if kind == 'sheer':
        room = sheer(room, seed=r * 7 + c)
    elif kind == 'thick':
        room = thick(room, colour=(0.8, 0.45, 0.28), seed=r * 7 + c)
    elif kind == 'blinds':
        room = blinds(room, seed=r * 7 + c)
    elif kind == 'blinds_half':
        room = blinds(room, raised=0.38, open_frac=0.2, seed=r * 7 + c)
    return tonemap(room, 1.0)


canvas = np.zeros((1024 * S, 1024 * S, 3), np.float32)
for r in range(ROWS):
    for c in range(COLS):
        x0, x1 = round(c * 1024 * S / COLS), round((c + 1) * 1024 * S / COLS)
        y0, y1 = r * CH, (r + 1) * CH
        img = cell(r, c)
        im = Image.fromarray((np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8)).resize((x1 - x0, y1 - y0), Image.LANCZOS)
        canvas[y0:y1, x0:x1] = np.asarray(im).astype(np.float32) / 255
out = Image.fromarray((np.clip(canvas, 0, 1) * 255 + 0.5).astype(np.uint8)).resize((1024, 1024), Image.LANCZOS)
a = np.asarray(out).astype(np.float32)
a += np.random.default_rng(3).normal(0, 1.6, a.shape)
Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(OUT, quality=90, optimize=True, progressive=True)
if len(sys.argv) > 3:
    Image.open(OUT).resize((1000, 1000)).save(sys.argv[3], quality=90)
print('saved', OUT, os.path.getsize(OUT))
