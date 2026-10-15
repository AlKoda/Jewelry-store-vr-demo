"""Project-made procedural street props (OBJ/MTL) using the shared Proc_* textures.
usage: proc_models.py OUTDIR"""
import sys, os, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
import conv
from geom import Part, box, quad, lathe, disc, cylinder, tube, arc_path, merge, to_groups, flatten

out = sys.argv[1]
os.makedirs(out, exist_ok=True)

# material library: name -> MTL info (+ Unity hints recorded in the build log)
P, G, C, S = 'Proc_PaintedMetal', 'Proc_Galvanised', 'Proc_CastIron', 'Proc_Signs'
MAT = {
    'PaintAnthracite': dict(color=P + '_Color.jpg', kd=[0.16, 0.17, 0.18], set=P, smoothness_scale=1.0),
    'PaintBlack': dict(color=P + '_Color.jpg', kd=[0.045, 0.045, 0.05], set=P, smoothness_scale=1.0),
    'PaintYellow': dict(color=P + '_Color.jpg', kd=[0.86, 0.62, 0.06], set=P, smoothness_scale=1.0),
    'PlasticLid': dict(color=P + '_Color.jpg', kd=[0.07, 0.16, 0.11], set=P, smoothness_scale=0.6),
    'Galvanised': dict(color=G + '_Color.jpg', kd=[1, 1, 1], set=G),
    'CastIron': dict(color=C + '_Color.jpg', kd=[1, 1, 1], set=C),
    'Signs': dict(color=S + '_Color.jpg', kd=[1, 1, 1], metallic=0.0, smoothness=0.35),
    'Lightbox': dict(color='Proc_Lightbox_Color.jpg', kd=[1, 1, 1], metallic=0.0, smoothness=0.6,
                     emission='Proc_Lightbox_Color.jpg as emission map, intensity 0.8-1.5'),
    'Void': dict(kd=[0.01, 0.01, 0.01], metallic=0.0, smoothness=0.0),
    'Rubber': dict(kd=[0.03, 0.03, 0.03], metallic=0.0, smoothness=0.25),
    'Glass': dict(kd=[0.55, 0.62, 0.62], alpha=0.22, metallic=0.0, smoothness=0.95),
    'LensRed': dict(kd=[0.45, 0.02, 0.02], alpha=1.0, metallic=0.0, smoothness=0.9, emission='(1.0, 0.08, 0.04) when lit'),
    'LensAmber': dict(kd=[0.5, 0.28, 0.0], alpha=1.0, metallic=0.0, smoothness=0.9, emission='(1.0, 0.55, 0.05) when lit'),
    'LensGreen': dict(kd=[0.0, 0.32, 0.2], alpha=1.0, metallic=0.0, smoothness=0.9, emission='(0.1, 1.0, 0.65) when lit'),
    'LensClear': dict(kd=[0.85, 0.87, 0.9], alpha=0.3, metallic=0.0, smoothness=0.95),
    'LedRed': dict(kd=[0.35, 0.02, 0.02], metallic=0.0, smoothness=0.8, emission='(1.0, 0.05, 0.03) x3 when flashing'),
    'LedBlue': dict(kd=[0.02, 0.05, 0.35], metallic=0.0, smoothness=0.8, emission='(0.05, 0.25, 1.0) x3 when flashing'),
    'LedWhite': dict(kd=[0.6, 0.6, 0.6], metallic=0.0, smoothness=0.8, emission='(1, 1, 1) x2 for take-down lights'),
}
LOG = {}


def norm_uv(p):
    uv = p.uv.copy()
    for k in (0, 1):
        lo, hi = uv[:, k].min(), uv[:, k].max()
        uv[:, k] = (uv[:, k] - lo) / (hi - lo if hi > lo else 1)
    return Part(p.v, p.f, uv, p.mat)


def flip(p):
    return Part(p.v, p.f[:, ::-1], p.uv, p.mat)


def double(p):
    return [p, flip(p)]


def cell(r):  # atlas rect in uv (u0, v0, u1, v1); image y grows down, uv v grows up
    return r


CELL_NOSTOP = (0.0, 0.5, 0.5, 1.0)
CELL_ONEWAY = (0.5, 0.5, 1.0, 1.0)
CELL_NAME = (0.0, 0.25, 1.0, 0.5)
CELL_BACKBOARD = (0.0, 0.0, 0.25, 0.25)
CELL_PAYPANEL = (0.25, 0.0, 0.5, 0.25)
CELL_REFLECT = (0.5, 0.0, 0.75, 0.25)
CELL_TIMETABLE = (0.75, 0.0, 1.0, 0.25)


def emit(name, parts, notes):
    groups = [(m, conv.weld(tm, normals=False)) for m, tm in to_groups(parts)]
    mtl = []
    for m, _ in groups:
        info = dict(MAT[m]); info.setdefault('alpha', 1.0)
        mtl.append((m, info))
    conv.write_mtl(out, name, mtl)
    res = conv.write_obj(out, name, [(m, m, tm) for m, tm in groups], ground=False, center=False, write_normals=False)
    res['materials'] = {m: MAT[m] for m, _ in groups}
    res['notes'] = notes
    LOG[name] = res
    print(name, res['tris'], res['size_m'])


# ---------------------------------------------------------------- TrafficLight
def traffic_light():
    pr = 0.0575
    parts = [lathe([(0.17, 0.0), (0.17, 0.018), (0.08, 0.04), (pr + 0.004, 0.09)], 'Galvanised', 24, cap_top=False),
             lathe([(pr, 0.09), (pr, 3.48), (pr * 0.8, 3.51), (0.0, 3.53)], 'Galvanised', 20)]
    # pedestrian push-button unit on +Z at 1.05 m
    parts.append(box(0.12, 0.26, 0.09, 'PaintYellow', center=(0, 1.1, pr + 0.045)))
    parts.append(box(0.07, 0.07, 0.012, 'PaintBlack', center=(0, 1.16, pr + 0.096)))
    # signal head
    hz = pr + 0.07 + 0.11  # housing centre z
    hy = 2.95
    parts.append(box(0.30, 0.95, 0.22, 'PaintBlack', center=(0, hy, hz)))
    for by in (hy - 0.33, hy + 0.33):
        parts.append(box(0.06, 0.06, 0.10, 'PaintBlack', center=(0, by, pr + 0.04)))
    # backboard: black box + atlas face
    bz = hz - 0.11 - 0.012
    parts.append(box(0.52, 1.17, 0.018, 'PaintBlack', center=(0, hy, bz)))
    parts.append(quad((-0.26, hy - 0.585, bz + 0.0102), (0.26, hy - 0.585, bz + 0.0102), (0.26, hy + 0.585, bz + 0.0102),
                      (-0.26, hy + 0.585, bz + 0.0102), 'Signs').uvmap(CELL_BACKBOARD))
    fz = hz + 0.11
    for dy, mat in ((0.30, 'LensRed'), (0.0, 'LensAmber'), (-0.30, 'LensGreen')):
        lens = lathe([(0.1, 0.0), (0.095, 0.012), (0.06, 0.022), (0.0, 0.026)], mat, 24).rx(90).t(0, hy + dy, fz)
        ring = lathe([(0.118, 0.0), (0.118, 0.012), (0.1, 0.012)], 'PaintBlack', 24).rx(90).t(0, hy + dy, fz)
        visor = lathe([(0.122, 0.0), (0.122, 0.22)], 'PaintBlack', 16, a0=np.pi * 1.05, a1=np.pi * 1.95).rx(90).t(0, hy + dy, fz)
        parts += [lens, ring] + double(visor)
    emit('TrafficLight', parts, 'Pole-mounted 3-aspect signal (200 mm lenses) with yellow pedestrian push-button; the head faces +Z (towards approaching traffic). Pivot at the pole base centre.')


# ---------------------------------------------------------------- Bollard
def bollard():
    r = 0.07
    parts = [lathe([(r, 0.0), (r, 0.9), (r * 0.95, 0.93), (r * 0.7, 0.965), (r * 0.35, 0.985), (0.0, 0.99)], 'PaintAnthracite', 24),
             lathe([(0.095, 0.0), (0.095, 0.008), (r + 0.004, 0.012)], 'Galvanised', 24)]
    for y0, y1 in ((0.78, 0.84), (0.66, 0.70)):
        band = lathe([(r + 0.0015, y0), (r + 0.0015, y1)], 'Signs', 24)
        parts.append(norm_uv(band).uvmap(CELL_REFLECT))
    emit('Bollard', parts, 'Steel street bollard, 140 mm diameter, 0.99 m tall, two retro-reflective bands. Pivot at the base centre.')


# ---------------------------------------------------------------- BikeRack (Sheffield stand)
def bike_rack():
    r = 0.024; hw = 0.35; top = 0.80; rad = 0.15
    path = [(-hw, -0.12, 0), (-hw, top - rad, 0)]
    path += list(arc_path((-hw + rad, top - rad, 0), rad, np.pi, np.pi / 2, 9)[1:])
    path += [(hw - rad, top, 0)]
    path += list(arc_path((hw - rad, top - rad, 0), rad, np.pi / 2, 0, 9)[1:])
    path += [(hw, -0.12, 0)]
    # densify straight runs for nicer tangents
    P_ = np.array(path, float)
    dense = [P_[0]]
    for a, b in zip(P_[:-1], P_[1:]):
        n = max(1, int(np.linalg.norm(b - a) / 0.12))
        for k in range(1, n + 1):
            dense.append(a + (b - a) * k / n)
    parts = [tube(np.array(dense), r, 'Galvanised', 14)]
    for x in (-hw, hw):
        parts.append(lathe([(0.05, 0.0), (0.05, 0.006), (r + 0.004, 0.012)], 'Galvanised', 16).t(x, 0, 0))
    emit('BikeRack', parts, 'Galvanised Sheffield cycle stand (48 mm tube), 0.75 m wide, 0.82 m tall; set 0.12 m into the pavement (mesh goes to y = -0.12). Pivot at ground level, centre. Place several 1 m apart.')


# ---------------------------------------------------------------- Sign posts
def pole(h, r=0.03):
    return [lathe([(r, -0.1), (r, h), (r * 0.6, h + 0.012), (0, h + 0.018)], 'Galvanised', 16)]


def clamp(y, r=0.03, z_out=0.03):
    return box(0.08, 0.04, 0.03, 'Galvanised', center=(0, y, r + 0.012))


def sign_no_stopping():
    parts = pole(2.9)
    cy, R, z = 2.5, 0.3, 0.05
    front = norm_uv(disc(R, 0, 'Signs', 40, up=True)).uvmap(CELL_NOSTOP)
    front = Part(front.v, front.f, front.uv, 'Signs').rx(90).t(0, cy, z + 0.006)
    # fix mirrored u after rotation: map u -> keep (disc u from x, v from z)
    back = disc(R, 0, 'Galvanised', 40, up=False).rx(90).t(0, cy, z)
    rim = lathe([(R, 0.0), (R, 0.006)], 'Galvanised', 40).rx(90).t(0, cy, z)
    parts += [front, back, rim, clamp(cy - 0.18), clamp(cy + 0.18)]
    emit('SignNoStopping', parts, 'No-stopping (clearway) sign, 600 mm disc on a 2.9 m galvanised post (60 mm); sign faces +Z. Post goes 0.1 m below ground.')


def sign_one_way():
    parts = pole(2.95)
    z = 0.05
    def plate(w, h, cy, rect):
        zf = z + 0.0072
        f = quad((-w / 2, cy - h / 2, zf), (w / 2, cy - h / 2, zf), (w / 2, cy + h / 2, zf),
                 (-w / 2, cy + h / 2, zf), 'Signs').uvmap(rect)
        b = box(w, h, 0.006, 'Galvanised', center=(0, cy, z + 0.003))
        return [f, b, clamp(cy - h / 2 + 0.06), clamp(cy + h / 2 - 0.06)]
    parts += plate(0.6, 0.6, 2.55, CELL_ONEWAY)
    parts += plate(0.9, 0.225, 2.05, CELL_NAME)
    emit('SignOneWayStreetName', parts, 'One-way sign (600 mm) above a fictional street-name plate "KESTREL STREET" on a 2.95 m galvanised post; faces +Z.')


def street_name_plate():
    w, h, d = 0.9, 0.225, 0.02
    parts = [box(w, h, d, 'PaintBlack', center=(0, 0, d / 2)),
             quad((-w / 2, -h / 2, d + 0.0005), (w / 2, -h / 2, d + 0.0005), (w / 2, h / 2, d + 0.0005), (-w / 2, h / 2, d + 0.0005), 'Signs').uvmap(CELL_NAME)]
    emit('StreetNamePlate', parts, 'Wall-mounted fictional street-name plate (0.9 x 0.225 m). Back face at z = 0 (put it on a facade), text faces +Z; pivot at the plate centre.')


# ---------------------------------------------------------------- Pay-and-display parking machine
def parking_meter():
    parts = [box(0.40, 0.10, 0.30, 'Galvanised', center=(0, 0.05, 0)),
             box(0.32, 1.25, 0.24, 'PaintAnthracite', center=(0, 0.10 + 0.625, 0))]
    # slanted cap with solar panel
    cap = box(0.36, 0.06, 0.30, 'PaintAnthracite', center=(0, 1.38, 0)).rx(-12).t(0, 0.0, 0)
    sol = box(0.30, 0.012, 0.24, 'Void', center=(0, 1.415, 0)).rx(-12)
    parts += [cap, sol]
    fz = 0.12 + 0.0008
    parts.append(quad((-0.14, 0.78, fz), (0.14, 0.78, fz), (0.14, 1.24, fz), (-0.14, 1.24, fz), 'Signs').uvmap(CELL_PAYPANEL))
    parts.append(box(0.24, 0.05, 0.05, 'PaintBlack', center=(0, 0.62, 0.12)))  # ticket tray lip
    emit('ParkingMeter', parts, 'Pay-and-display parking machine, 0.40 x 1.44 x 0.30 m; display and keypad face +Z. Generic icons only.')


# ---------------------------------------------------------------- Storm drain grate (gully)
def storm_drain():
    L, W, fw, top = 0.62, 0.42, 0.05, 0.012
    parts = [quad((-L / 2, 0.002, W / 2), (L / 2, 0.002, W / 2), (L / 2, 0.002, -W / 2), (-L / 2, 0.002, -W / 2), 'Void')]
    for (cx, cz, sx, sz) in ((0, W / 2 - fw / 2, L, fw), (0, -W / 2 + fw / 2, L, fw), (L / 2 - fw / 2, 0, fw, W - 2 * fw), (-L / 2 + fw / 2, 0, fw, W - 2 * fw)):
        parts.append(box(sx, top, sz, 'CastIron', center=(cx, top / 2, cz)))
    n = 11
    inner = L - 2 * fw
    for i in range(n):
        x = -inner / 2 + inner * (i + 0.5) / n
        parts.append(box(0.022, top - 0.001, W - 2 * fw, 'CastIron', center=(x, (top - 0.001) / 2, 0)))
    parts.append(box(inner, top - 0.002, 0.02, 'CastIron', center=(0, (top - 0.002) / 2, 0)))
    emit('StormDrainGrate', parts, 'Road gully grate 0.62 x 0.42 m, 12 mm proud of the road; long side along X (lay it in the gutter against the kerb, long side parallel to the kerb). A black void plane under the bars fakes the pit.')


# ---------------------------------------------------------------- 1100 L wheeled waste container
def tapered_box(b0, b1, y0, y1, mat):
    """bottom half-sizes b0=(hx,hz), top half-sizes b1, open top. UV metres."""
    (bx, bz), (tx, tz) = b0, b1
    c = [(-bx, y0, -bz), (bx, y0, -bz), (bx, y0, bz), (-bx, y0, bz), (-tx, y1, -tz), (tx, y1, -tz), (tx, y1, tz), (-tx, y1, tz)]
    sides = [(3, 2, 6, 7), (1, 0, 4, 5), (2, 1, 5, 6), (0, 3, 7, 4), (0, 1, 2, 3)]
    parts = []
    for idx in sides:
        p = [c[i] for i in idx]
        w = np.linalg.norm(np.subtract(p[1], p[0])); h = np.linalg.norm(np.subtract(p[3], p[0]))
        parts.append(quad(*p, mat, uv=((0, 0), (w, 0), (w, h), (0, h))))
    return parts


def dumpster():
    y0, y1 = 0.22, 1.24
    parts = tapered_box((0.60, 0.46), (0.665, 0.52), y0, y1, 'Galvanised')
    parts += [flip(p) for p in tapered_box((0.585, 0.445), (0.65, 0.505), y0 + 0.02, y1, 'Galvanised')]
    # rim
    for (cx, cz, sx, sz) in ((0, 0.53, 1.37, 0.04), (0, -0.53, 1.37, 0.04), (0.675, 0, 0.04, 1.1), (-0.675, 0, 0.04, 1.1)):
        parts.append(box(sx, 0.05, sz, 'Galvanised', center=(cx, y1 - 0.02, cz)))
    # lid (slightly domed, two boxes) hinged at the back (-Z)
    parts.append(box(1.40, 0.04, 1.12, 'PlasticLid', center=(0, y1 + 0.03, 0.0)))
    parts.append(box(1.30, 0.03, 0.9, 'PlasticLid', center=(0, y1 + 0.065, 0.02)))
    parts.append(box(0.5, 0.025, 0.06, 'PlasticLid', center=(0, y1 + 0.03, 0.585)))  # front grip
    # chassis frame + castors
    parts.append(box(1.22, 0.05, 0.94, 'Galvanised', center=(0, y0 - 0.02, 0)))
    for sx in (-0.5, 0.5):
        for sz in (-0.36, 0.36):
            parts.append(box(0.08, 0.06, 0.08, 'Galvanised', center=(sx, y0 - 0.07, sz)))
            wheel = lathe([(0.0, -0.022), (0.095, -0.022), (0.1, 0.0), (0.095, 0.022), (0.0, 0.022)], 'Rubber', 18).rx(90).ry(90)
            parts.append(wheel.t(sx, 0.1, sz))
            parts.append(box(0.06, 0.1, 0.012, 'Galvanised', center=(sx, 0.15, sz + 0.03)))
            parts.append(box(0.06, 0.1, 0.012, 'Galvanised', center=(sx, 0.15, sz - 0.03)))
    # push handles (front +Z) and side trunnions
    hp = np.array([(-0.45, 0.95, 0.53), (-0.45, 0.95, 0.60), (0.45, 0.95, 0.60), (0.45, 0.95, 0.53)])
    parts.append(tube(hp, 0.016, 'Galvanised', 10))
    for x in (-0.7, 0.7):
        parts.append(cylinder(0.04, 0.08, 'Galvanised', 16).rz(90).t(x + (0.04 if x > 0 else -0.04), 0.9, 0))
    emit('WasteContainer1100L', parts, 'Four-wheel 1100 litre galvanised waste container with dark green plastic lid (EN 840 size class), 1.40 x 1.36 x 1.12 m. Front handles on +Z, lid hinge on -Z.')


# ---------------------------------------------------------------- Bus shelter
def bus_shelter():
    L, D, H = 3.6, 1.4, 2.45
    parts = []
    post = 0.08
    for x in (-L / 2, 0.0, L / 2):
        parts.append(box(post, H, post, 'PaintAnthracite', center=(x, H / 2, -D / 2 + 0.05)))
    for x in (-L / 2, L / 2):
        parts.append(box(post, H, post, 'PaintAnthracite', center=(x, H / 2, D / 2 - 0.25)))
    # roof (slight fall to the back) and fascia
    parts.append(box(L + 0.3, 0.07, D + 0.2, 'PaintAnthracite', center=(0, H + 0.05, 0.0)).rx(1.5))
    parts.append(box(L + 0.3, 0.16, 0.04, 'PaintAnthracite', center=(0, H + 0.03, D / 2 + 0.1)))
    # glass back panels (two bays) and end screens
    for xc in (-L / 4, L / 4):
        g = box(L / 2 - post - 0.02, 1.95, 0.012, 'Glass', center=(xc, 0.15 + 0.975, -D / 2 + 0.05))
        parts.append(g)
        parts.append(box(L / 2 - post, 0.05, 0.05, 'PaintAnthracite', center=(xc, 0.12, -D / 2 + 0.05)))
        parts.append(box(L / 2 - post, 0.05, 0.05, 'PaintAnthracite', center=(xc, 2.13, -D / 2 + 0.05)))
    for x in (-L / 2,):
        parts.append(box(0.012, 1.95, D - 0.35, 'Glass', center=(x, 0.15 + 0.975, -0.175 + 0.05 - 0.05)))
    # advertising lightbox at the +X end (double sided, faces +X and -X)
    lx = L / 2
    parts.append(box(0.16, 1.95, D - 0.4, 'PaintAnthracite', center=(lx, 0.15 + 0.975, -0.15)))
    for sgn in (1, -1):
        x = lx + sgn * 0.0805
        zA, zB = -0.15 - (D - 0.55) / 2, -0.15 + (D - 0.55) / 2
        y0, y1 = 0.25, 2.0
        if sgn > 0:
            q = quad((x, y0, zB), (x, y0, zA), (x, y1, zA), (x, y1, zB), 'Lightbox')
        else:
            q = quad((x, y0, zA), (x, y0, zB), (x, y1, zB), (x, y1, zA), 'Lightbox')
        parts.append(q)
    # bench
    parts.append(box(1.6, 0.04, 0.34, 'Galvanised', center=(-0.9, 0.46, -D / 2 + 0.25)))
    for x in (-1.6, -0.2):
        parts.append(box(0.04, 0.4, 0.3, 'PaintAnthracite', center=(x, 0.24, -D / 2 + 0.24)))
    # timetable panel on the middle post
    tz = -D / 2 + 0.05 + post / 2 + 0.003
    parts.append(box(0.42, 0.42, 0.02, 'PaintAnthracite', center=(0.0, 1.45, tz + 0.01)))
    parts.append(quad((-0.2, 1.25, tz + 0.021), (0.2, 1.25, tz + 0.021), (0.2, 1.65, tz + 0.021), (-0.2, 1.65, tz + 0.021), 'Signs').uvmap(CELL_TIMETABLE))
    emit('BusShelter', parts, 'Glazed bus shelter 3.9 x 2.6 x 1.6 m: open side faces +Z (kerb), glass back at z = -0.65, glass end screen at -X, illuminated advertising box at +X (fictional poster), bench and timetable inside.')


# ---------------------------------------------------------------- Police light bar (modern LED)
def light_bar():
    L, D = 1.22, 0.30
    parts = [box(L, 0.035, D, 'PaintBlack', center=(0, 0.06, 0)),
             box(L - 0.02, 0.05, D - 0.02, 'LensClear', center=(0, 0.103, 0))]
    for x in (-0.45, 0.45):
        parts.append(box(0.06, 0.045, 0.22, 'Rubber', center=(x, 0.0225, 0)))
    n = 6
    for side, mat in ((-1, 'LedRed'), (1, 'LedBlue')):
        for i in range(n):
            x = side * (0.12 + i * 0.085)
            parts.append(box(0.07, 0.03, 0.03, mat, center=(x, 0.095, D / 2 - 0.045)))   # front
            parts.append(box(0.07, 0.03, 0.03, mat, center=(x, 0.095, -D / 2 + 0.045)))  # rear
        parts.append(box(0.03, 0.03, 0.12, mat, center=(side * (L / 2 - 0.04), 0.095, 0)))  # ends
    parts.append(box(0.16, 0.03, 0.03, 'LedWhite', center=(0, 0.095, D / 2 - 0.045)))
    emit('PoliceLightBar', parts, 'Modern LED light bar 1.22 x 0.13 x 0.30 m: red LEDs on -X half, blue on +X half, white take-down LEDs at the front centre. Long axis X, front = +Z. On PoliceCar (forward = +X) rotate 90 deg about Y; on the KayKit police car scale as needed.')


for fn in (traffic_light, bollard, bike_rack, sign_no_stopping, sign_one_way, street_name_plate, parking_meter, storm_drain,
           dumpster, bus_shelter, light_bar):
    fn()
logp = os.path.join(out, '.build_log.json')
log = json.load(open(logp)) if os.path.exists(logp) else {}
log.update(LOG)
json.dump(log, open(logp, 'w'), indent=1)
