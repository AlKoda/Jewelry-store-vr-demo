"""Project-made office props: OfficeChair, ComputerMonitor, SecurityMonitor, Keyboard, Mouse, DeskPhone,
FilingCabinet, FilingCabinetOpen, LeverArchFiles, DVRRecorder. Procedural geometry + procedural textures."""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from proc_lib import *
from proc_materials import build, mat

M = build()
rng = np.random.default_rng(42)

def tiled(mesh, key_scales):
    for k, s in key_scales.items():
        if k in mesh.parts: retile(mesh, k, s)

# ======================================================================= OfficeChair
def office_chair():
    m = Mesh()
    for k in range(5):
        a = 360.0 * k / 5 + 18
        leg = rbox((0.27, 0.034, 0.048), 0.012, center=(0.17, 0.0, 0), seg=1)
        add(m, 'Base', leg, chain(Ry(a), T(0, 0.082, 0), Rz(-5)))
        # caster: twin wheel + hood + stem
        ca = math.radians(a); cx, cz = 0.305 * math.cos(ca), -0.305 * math.sin(ca)
        wheel = cyl(0.026, 0.044, 16, y0=-0.022)
        add(m, 'Rubber', wheel, chain(T(cx, 0.026, cz), Ry(a), Rx(90)))
        hood = rbox((0.05, 0.028, 0.052), 0.012, center=(0, 0, 0), seg=1)
        add(m, 'PlasticBlack', hood, chain(T(cx, 0.05, cz), Ry(a)))
        add(m, 'Chrome', cyl(0.006, 0.03, 8, y0=0.055), T(cx, 0, cz))
    add(m, 'Base', cyl(0.05, 0.07, 24, y0=0.06, r_top=0.04))
    add(m, 'PlasticBlack', cyl(0.033, 0.15, 24, y0=0.12, r_top=0.03))  # gas-lift shroud
    add(m, 'Chrome', cyl(0.014, 0.14, 16, y0=0.26))                     # piston
    add(m, 'PlasticBlack', rbox((0.2, 0.05, 0.25), 0.015, center=(0, 0.41, 0.0)))  # mechanism
    lever = tube([(0.09, 0.41, 0.07), (0.17, 0.405, 0.09), (0.22, 0.40, 0.10)], 0.006, 8)
    add(m, 'PlasticBlack', lever); add(m, 'PlasticBlack', rbox((0.05, 0.018, 0.025), 0.008, center=(0.235, 0.40, 0.10), seg=1))
    add(m, 'PlasticBlack', rbox((0.5, 0.025, 0.48), 0.012, center=(0, 0.447, 0.01)))   # seat shell
    add(m, 'Fabric', rbox((0.49, 0.08, 0.47), 0.034, center=(0, 0.495, 0.012), seg=4))  # cushion
    # back support spine
    add(m, 'PlasticBlack', tube([(0, 0.42, -0.11), (0, 0.43, -0.25), (0, 0.47, -0.31), (0, 0.6, -0.33)], 0.02, 12))
    # curved mesh back with frame
    def back_pt(u, v):
        x = 0.235 * u * (1 - 0.08 * v)
        y = 0.56 + 0.5 * v
        z = -0.31 - 0.09 * v + 0.055 * u * u - 0.02 * math.sin(math.pi * min(v / 0.6, 1.0)) * (1 - u * u)
        return (x, y, z)
    nu, nv = 14, 16
    G = np.array([[back_pt(-1 + 2 * i / nu, j / nv) for i in range(nu + 1)] for j in range(nv + 1)])
    UVg = np.zeros(G.shape[:2] + (2,))
    Pf, UVf, _ = grid_surface(G, UVg, flip=False)
    Nf = smooth_normals(Pf, 70)
    add(m, 'Mesh', (Pf, UVf, Nf)); add(m, 'Mesh', (Pf[:, ::-1], UVf[:, ::-1], -Nf[:, ::-1]))
    # frame loop around the panel (rounded corners)
    loop = []
    for i in range(0, nu + 1): loop.append(back_pt(-1 + 2 * i / nu, 0))
    for j in range(1, nv + 1): loop.append(back_pt(1, j / nv))
    for i in range(nu - 1, -1, -1): loop.append(back_pt(-1 + 2 * i / nu, 1))
    for j in range(nv - 1, 0, -1): loop.append(back_pt(-1, j / nv))
    loop.append(loop[0])
    add(m, 'PlasticBlack', tube(np.array(loop), 0.014, 10, closed_ends=False))
    # armrests
    for s in (-1, 1):
        add(m, 'PlasticBlack', rbox((0.07, 0.022, 0.07), 0.008, center=(s * 0.235, 0.452, -0.02), seg=1))
        add(m, 'PlasticBlack', rbox((0.032, 0.21, 0.06), 0.01, center=(s * 0.272, 0.555, -0.02), seg=1))
        add(m, 'ArmPad', rbox((0.088, 0.03, 0.26), 0.013, center=(s * 0.272, 0.672, 0.0), seg=3))
    tiled(m, {'Fabric': 0.08, 'PlasticBlack': 0.12, 'ArmPad': 0.12, 'Base': 0.25})
    # mesh back: map planar x/y in world scale for the weave
    P, UV, N = m.parts['Mesh']; m.parts['Mesh'][1] = np.stack([P[..., 0] / 0.05, P[..., 1] / 0.05], -1)
    mats = {'Fabric': mat(M, 'FabricCharcoal'), 'PlasticBlack': mat(M, 'PlasticBlack'), 'ArmPad': mat(M, 'PlasticBlack', smoothness=0.3),
            'Mesh': mat(M, 'MeshWeave', note='double-sided panel (two opposite faces), opaque'), 'Base': mat(M, 'Aluminium'),
            'Chrome': mat(M, 'Chrome'), 'Rubber': mat(M, 'Rubber')}
    return write_model('OfficeChair', m, mats, {'kind': 'model', 'what': 'Swivel task chair: mesh back, charcoal fabric seat, polished 5-star base, twin-wheel casters'})

# ======================================================================= monitor + screens
def screen_lock(path):
    W, H = 1024, 576
    y, x = np.mgrid[0:H, 0:W].astype(np.float32)
    g = np.dstack([0.03 + 0.06 * (1 - y / H), 0.06 + 0.10 * (1 - y / H), 0.12 + 0.16 * (1 - y / H)])
    g += np.exp(-(((x - 700) / 380) ** 2 + ((y - 120) / 260) ** 2))[..., None] * np.array([0.10, 0.12, 0.18])
    im = Image.fromarray((np.clip(g, 0, 1) * 255).astype(np.uint8)); d = ImageDraw.Draw(im)
    d.text((80, 300), '02:47', font=F('sans', 150), fill=(235, 240, 250), anchor='ls')
    d.text((86, 350), 'Wednesday 14 October', font=F('sans', 34), fill=(200, 210, 225), anchor='ls')
    d.rounded_rectangle([86, 470, 380, 510], 18, outline=(200, 210, 225), width=2)
    d.text((110, 491), 'Press any key to unlock', font=F('sans', 20), fill=(200, 210, 225), anchor='lm')
    im.save(os.path.join(OUT, path), quality=88)

def cam_view(w, h, seed, kind):
    r = np.random.default_rng(seed)
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    img = np.full((h, w), 0.08, np.float32)
    hz = h * (0.30 + 0.1 * r.random())
    # floor with perspective tile grid
    fy = np.clip((y - hz) / (h - hz), 1e-3, 1)
    floor = (y > hz)
    zz = 1 / fy; xx = (x - w / 2) / w * zz * 2.2
    grid = ((np.abs(np.sin(zz * 3.2)) < 0.06) | (np.abs(np.sin(xx * 3.2)) < 0.06 / np.maximum(fy, 0.2)))
    img[floor] = 0.22 + 0.25 * fy[floor] - 0.06 * grid[floor]
    img[~floor] = 0.12 + 0.05 * (y[~floor] / hz)
    if kind == 'shop':
        for _ in range(4):
            cx = r.uniform(0.1, 0.9) * w; cyy = r.uniform(hz + 10, h * 0.85); s = (cyy - hz) / (h - hz) + 0.2
            bw, bh = 110 * s, 55 * s
            box = (np.abs(x - cx) < bw) & (y > cyy - bh) & (y < cyy)
            img[box] = 0.36 + 0.15 * ((y[box] - (cyy - bh)) / bh)
            top = (np.abs(x - cx) < bw) & (np.abs(y - (cyy - bh)) < 2.5)
            img[top] = 0.75
    elif kind == 'door':
        dx = w * 0.62
        door = (np.abs(x - dx) < w * 0.09) & (y > hz - h * 0.25) & (y < hz + h * 0.25)
        img[door] = 0.30
        img[(np.abs(x - dx) < w * 0.09) & (np.abs(y - (hz - h * 0.25)) < 2)] = 0.6
        img += 0.25 * np.exp(-(((x - w * 0.15) / 60) ** 2 + ((y - h * 0.2) / 40) ** 2))
    img += r.normal(0, 0.035, img.shape)
    img *= 1 - 0.35 * (((x - w / 2) / w) ** 2 + ((y - h / 2) / h) ** 2)
    img = np.asarray(Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8)), np.float32) / 255
    return img

def screen_cctv(path):
    W, H = 1024, 576
    im = Image.new('RGB', (W, H), (8, 8, 10)); d = ImageDraw.Draw(im)
    labels = [('CAM 01  ENTRANCE', 'door'), ('CAM 02  SALES FLOOR', 'shop'), ('CAM 03  OFFICE', None), ('CAM 04  REAR DOOR', 'door')]
    cw, ch = W // 2 - 6, H // 2 - 6
    for i, (lab, kind) in enumerate(labels):
        ox, oy = 4 + (i % 2) * (cw + 4), 4 + (i // 2) * (ch + 4)
        if kind:
            v = cam_view(cw, ch, 20 + i, kind)
            tint = np.dstack([v * 0.92, v, v * 0.95])
            im.paste(Image.fromarray((np.clip(tint, 0, 1) * 255).astype(np.uint8)), (ox, oy))
        else:
            d.rectangle([ox, oy, ox + cw, oy + ch], fill=(20, 22, 60))
            d.text((ox + cw // 2, oy + ch // 2), 'VIDEO LOSS', font=F('mono-bold', 34), fill=(230, 230, 235), anchor='mm')
        d.text((ox + 10, oy + 8), lab, font=F('mono-bold', 18), fill=(235, 235, 235))
        d.text((ox + cw - 10, oy + ch - 10), '2026-10-14  02:47:%02d' % (13 + i), font=F('mono', 16), fill=(235, 235, 235), anchor='rs')
        if kind:
            d.ellipse([ox + cw - 58, oy + 10, ox + cw - 46, oy + 22], fill=(220, 30, 30)); d.text((ox + cw - 40, oy + 8), 'REC', font=F('mono-bold', 16), fill=(235, 235, 235))
    im.save(os.path.join(OUT, path), quality=88)

def monitor(name, screen_file, what):
    m = Mesh()
    W, H, yb = 0.541, 0.323, 0.115
    yc = yb + H / 2
    tilt = chain(T(0, yc, 0), Rx(-5), T(0, -yc, 0))
    add(m, 'Housing', rbox((W, H, 0.016), 0.004, center=(0, yc, 0)), tilt)
    add(m, 'Housing', rbox((0.36, 0.22, 0.036), 0.016, center=(0, yc - 0.01, -0.022)), tilt)
    # screen quad, slightly proud of the bezel front
    sx0, sx1, sy0, sy1 = -W / 2 + 0.007, W / 2 - 0.007, yb + 0.02, yb + H - 0.007
    add(m, 'Screen', quad((sx0, sy0, 0.0082), (sx1, sy0, 0.0082), (sx1, sy1, 0.0082), (sx0, sy1, 0.0082), (0, 0, 1, 1), (0, 0, 1)), tilt)
    add(m, 'Housing', rbox((0.05, 0.26, 0.024), 0.009, center=(0, 0.14, -0.055)), chain(T(0, 0.02, -0.055), Rx(-8), T(0, -0.02, 0.055)))
    add(m, 'Housing', rbox((0.25, 0.012, 0.19), 0.006, center=(0, 0.006, -0.04), seg=3))
    add(m, 'Housing', cyl(0.004, 0.002, 12, y0=0.0), T(W / 2 - 0.02, yb + 0.003, 0.0085))   # power LED
    retile(m, 'Housing', 0.12)
    mats = {'Housing': mat(M, 'PlasticBlack'),
            'Screen': {'maps': {'Color': screen_file}, 'smoothness': 0.9, 'metallic': 0.0,
                       'emission': 'use the Color map as emission (intensity ~0.6) when the screen is on; plain black when off'}}
    return write_model(name, m, mats, {'kind': 'model', 'what': what})

# ======================================================================= keyboard
def keyboard():
    U = 0.019; gap = 0.0016
    rows = [  # (width units ... ) per row of the main block; None = gap
        [1, None, 1, 1, 1, 1, None, 1, 1, 1, 1, None, 1, 1, 1, 1],
        [1] * 13 + [2], [1.5] + [1] * 12 + [1.5], [1.75] + [1] * 11 + [2.25], [2.25] + [1] * 10 + [2.75],
        [1.25, 1.25, 1.25, 6.25, 1.25, 1.25, 1.25, 1.25]]
    legends = [['Esc', None, 'F1', 'F2', 'F3', 'F4', None, 'F5', 'F6', 'F7', 'F8', None, 'F9', 'F10', 'F11', 'F12'],
               list('`1234567890-=') + ['Backspace'], ['Tab'] + list('QWERTYUIOP[]') + ['\\'], ['Caps'] + list("ASDFGHJKL;'") + ['Enter'],
               ['Shift'] + list('ZXCVBNM,./') + ['Shift'], ['Ctrl', 'Win', 'Alt', '', 'Alt', 'Fn', 'Menu', 'Ctrl']]
    keys = []  # (x0, z0, w, d, legend)
    zrow = [0, 1.5, 2.5, 3.5, 4.5, 5.5]
    for r, row in enumerate(rows):
        x = 0.0
        for k, wu in enumerate(row):
            if wu is None: x += 0.5 * U; continue
            keys.append((x, zrow[r] * U, wu * U, U, legends[r][k])); x += wu * U
    nav_x = 15.5 * U
    for r, labs in ((0, ['PrtSc', 'ScrLk', 'Pause']), (1, ['Ins', 'Home', 'PgUp']), (2, ['Del', 'End', 'PgDn'])):
        for k, l in enumerate(labs): keys.append((nav_x + k * U, zrow[r] * U, U, U, l))
    keys.append((nav_x + U, zrow[4] * U, U, U, '^')); 
    for k, l in enumerate(['<', 'v', '>']): keys.append((nav_x + k * U, zrow[5] * U, U, U, l))
    np_x = 19 * U
    pad = [(0, 1, 'Num'), (1, 1, '/'), (2, 1, '*'), (3, 1, '-'), (0, 2, '7'), (1, 2, '8'), (2, 2, '9'), (0, 3, '4'), (1, 3, '5'), (2, 3, '6'),
           (0, 4, '1'), (1, 4, '2'), (2, 4, '3'), (0, 5, '0w'), (2, 5, '.')]
    for c, r, l in pad:
        w = 2 * U if l == '0w' else U
        keys.append((np_x + c * U, zrow[r] * U, w, U, '0' if l == '0w' else l))
    keys.append((np_x + 3 * U, zrow[2] * U, U, 2 * U, '+')); keys.append((np_x + 3 * U, zrow[4] * U, U, 2 * U, 'Enter'))
    Wk = 23 * U; Dk = 6.5 * U
    # texture: top view of the keyboard, 2048 x (2048*Dk/Wk)
    TW = 2048; TH = int(TW * (Dk + 0.012) / (Wk + 0.012)); TH += TH % 2
    sc = TW / (Wk + 0.012)
    im = Image.new('RGB', (TW, TH), (14, 14, 16)); d = ImageDraw.Draw(im)
    for (x0, z0, w, dd, l) in keys:
        a = ((x0 + gap + 0.006) * sc, (z0 + gap + 0.006) * sc, (x0 + w - gap + 0.006) * sc, (z0 + dd - gap + 0.006) * sc)
        d.rectangle(a, fill=(30, 30, 33))
        d.rectangle((a[0] + 4, a[1] + 4, a[2] - 4, a[3] - 4), fill=(36, 36, 40))
        if l:
            fs = 22 if len(l) == 1 else 15
            d.text((a[0] + 9, a[1] + 7), l, font=F('sans', fs), fill=(205, 205, 210))
    arr = np.asarray(im, np.float32) / 255
    arr += (fbm(TH, TW, 30, 3)[..., None] - 0.5) * 0.02
    tex = save_set('Keyboard', arr, None, cpx=(1024, TH // 2))
    m = Mesh()
    toUV = lambda X, Z: ((X + Wk / 2 + 0.006) / (Wk + 0.012), 1 - (Z + Dk / 2 + 0.006) / (Dk + 0.012))
    # case: rounded slab, slight wedge via tilt
    case = rbox((Wk + 0.012, 0.022, Dk + 0.012), 0.006, center=(0, 0.011, 0))
    P, UV, N = case
    UV = np.stack([(P[..., 0] + Wk / 2 + 0.006) / (Wk + 0.012), 1 - (P[..., 2] + Dk / 2 + 0.006) / (Dk + 0.012)], -1)
    # keep case UVs inside the dark border of the texture
    UV = np.clip(UV, 0.002, 0.004 + 0 * UV)
    add(m, 'Keyboard', (P, UV, N))
    for (x0, z0, w, dd, l) in keys:
        X0, X1 = x0 + gap - Wk / 2, x0 + w - gap - Wk / 2
        Z0, Z1 = z0 + gap - Dk / 2, z0 + dd - gap - Dk / 2
        yb, yt = 0.022, 0.032 - 0.0006 * ((z0 / U) - 3)
        ins = 0.0018
        top = [(X0 + ins, yt, Z1 - ins), (X1 - ins, yt, Z1 - ins), (X1 - ins, yt, Z0 + ins), (X0 + ins, yt, Z0 + ins)]
        bot = [(X0, yb, Z1), (X1, yb, Z1), (X1, yb, Z0), (X0, yb, Z0)]
        uvt = [toUV(p[0], p[2]) for p in top]
        Pq = [[top[0], top[1], top[2]], [top[0], top[2], top[3]]]
        UVq = [[uvt[0], uvt[1], uvt[2]], [uvt[0], uvt[2], uvt[3]]]
        add(m, 'Keyboard', (np.array(Pq), np.array(UVq), None))
        for i in range(4):
            a, b = bot[i], bot[(i + 1) % 4]; c, dd_ = top[(i + 1) % 4], top[i]
            uva, uvb = toUV(a[0], a[2]), toUV(b[0], b[2])
            Pq = [[a, b, c], [a, c, dd_]]
            UVq = [[uva, uvb, toUV(c[0], c[2])], [uva, toUV(c[0], c[2]), toUV(dd_[0], dd_[2])]]
            add(m, 'Keyboard', (np.array(Pq), np.array(UVq), None))
    # cable stub at the back
    add(m, 'Rubber', tube([(-Wk / 2 + 0.05, 0.012, -Dk / 2 - 0.006), (-Wk / 2 + 0.05, 0.006, -Dk / 2 - 0.06), (-Wk / 2 + 0.12, 0.004, -Dk / 2 - 0.12)], 0.0022, 6))
    # flip so the key rows run front (+Z = space bar) to back (function row): our rows grew towards +Z already
    mats = {'Keyboard': {'maps': tex, 'smoothness': 0.35, 'metallic': 0.0}, 'Rubber': mat(M, 'Rubber')}
    return write_model('Keyboard', m, mats, {'kind': 'model', 'what': 'Full-size 104-key keyboard with printed legends (generic, no logo)'})

# ======================================================================= mouse + pad
def mouse():
    m = Mesh()
    nu, nv = 24, 14
    G = np.zeros((nv + 1, nu + 1, 3))
    for j in range(nv + 1):
        t = j / nv  # 0 = back, 1 = front
        z = -0.06 + 0.12 * t
        half_w = 0.031 * math.sin(math.pi * (0.12 + 0.82 * t)) ** 0.6
        hmax = 0.036 * (math.sin(math.pi * (0.25 + 0.6 * t)) ** 0.8) + 0.004
        for i in range(nu + 1):
            a = math.pi * i / nu
            G[j, i] = (-math.cos(a) * half_w, math.sin(a) * hmax + 0.003, z)
    UVg = np.stack(np.meshgrid(np.linspace(0, 1, nu + 1), np.linspace(0, 1, nv + 1)), -1)
    P, UV, _ = grid_surface(G, UVg)
    N = smooth_normals(P, 60)
    fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
    if np.mean(fn[:, 1]) < 0: P, UV, N = P[:, ::-1], UV[:, ::-1], N[:, ::-1]
    add(m, 'Mouse', (P, UV, N))
    # base plate
    ring = G[:, 0].tolist() + G[::-1, -1].tolist()
    poly = np.array([(p[0], p[2]) for p in ring])
    top, bot, side = extrude(poly, 0.003, y0=0.0)
    add(m, 'Rubber', bot); add(m, 'Rubber', side)
    add(m, 'Rubber', cyl(0.009, 0.007, 16, y0=-0.0035), chain(T(0, 0.036, 0.022), Rz(90)))  # wheel
    tex = np.zeros((256, 256, 3), np.float32) + 0.045
    tex[:, 126:130] = 0.015  # button split line along the top (u = 0.5)
    tex[150:, :] = tex[150:, :]  # (front half = buttons)
    tex[150:153, :] = 0.02
    mt = save_set('Mouse', tex + (fbm(256, 256, 20, 4)[..., None] - 0.5) * 0.01, None)
    mp = Mesh()
    add(m, 'Pad', rbox((0.25, 0.003, 0.21), 0.0015, center=(0.0, -0.0015, 0.0), seg=2))
    P, UV, N = m.parts['Mouse']
    mats = {'Mouse': {'maps': mt, 'smoothness': 0.45, 'metallic': 0.0}, 'Rubber': mat(M, 'Rubber'),
            'Pad': mat(M, 'FabricCharcoal')}
    retile(m, 'Pad', 0.08)
    return write_model('Mouse', m, mats, {'kind': 'model', 'what': 'Corded optical mouse on a fabric mouse mat (mat 25 x 21 cm)'})

# ======================================================================= desk phone
def desk_phone():
    m = Mesh()
    Wd = 0.205
    prof = [(0.095, 0.0), (-0.095, 0.0), (-0.095, 0.085), (-0.07, 0.092), (0.08, 0.034), (0.095, 0.026)]  # (z, y)
    # extrude along x
    pz = np.array(prof)
    tris = triangulate(np.array([(p[0], p[1]) for p in prof]))
    for sx in (-Wd / 2, Wd / 2):
        Pq = []
        for a, b, c in tris:
            t = np.array([(sx, pz[k][1], pz[k][0]) for k in (a, b, c)])
            fn = np.cross(t[1] - t[0], t[2] - t[0])
            if (fn[0] > 0) != (sx > 0): t = t[::-1]
            Pq.append(t)
        Pq = np.array(Pq); add(m, 'Body', (Pq, np.zeros(Pq.shape[:2] + (2,)) + 0.02, None))
    face_quad = None
    for i in range(len(prof)):
        a, b = prof[i], prof[(i + 1) % len(prof)]
        q = [(-Wd / 2, a[1], a[0]), (Wd / 2, a[1], a[0]), (Wd / 2, b[1], b[0]), (-Wd / 2, b[1], b[0])]
        P = np.array([[q[0], q[1], q[2]], [q[0], q[2], q[3]]])
        mid = np.array([0, (a[1] + b[1]) / 2, (a[0] + b[0]) / 2]); cen = np.array([0, 0.04, 0])
        fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
        if np.dot(fn[0], mid - cen) < 0: P = P[:, ::-1]
        if i == 3:  # the slanted keypad face from (-0.07,0.092) to (0.08,0.034)
            face_quad = (a, b)
            UV = np.zeros(P.shape[:2] + (2,))
            for f in range(2):
                for c in range(3):
                    p = P[f, c]; UV[f, c] = ((p[0] + Wd / 2) / Wd, 0.0 + (p[2] - b[0]) / (a[0] - b[0]))
            add(m, 'Face', (P, UV, None))
        else:
            add(m, 'Body', (P, np.zeros(P.shape[:2] + (2,)) + 0.02, None))
    # handset on the left, resting in a cradle along z, slightly lifted at the back
    path = [(-0.062, 0.096 - 0.0 * t, -0.085 + 0.175 * t) for t in np.linspace(0, 1, 18)]
    path = [(x, 0.088 - 0.045 * ((z + 0.085) / 0.175) + 0.017, z) for x, y, z in path]
    radii = [0.017 + 0.007 * (abs(t - 0.5) * 2) ** 3 for t in np.linspace(0, 1, 18)]
    add(m, 'Body', tube(path, 0.02, 16, radii=radii))
    # coiled cord from the handset front end to the left side
    hel = []
    for k in range(160):
        t = k / 159
        cx, cy, cz = -0.1 - 0.035 * t, 0.012, 0.07 - 0.12 * t
        a = t * 2 * math.pi * 22
        hel.append((cx + 0.006 * math.cos(a), cy + 0.006 * math.sin(a), cz))
    add(m, 'Rubber', tube(hel, 0.0018, 5))
    add(m, 'Rubber', rbox((0.18, 0.004, 0.17), 0.002, center=(0, 0.002, 0.0), seg=1))
    # face texture: LCD, soft keys, keypad, nav cluster (left part covered by handset)
    TW, TH = 1024, int(1024 * 0.16 / 0.205)
    im = Image.new('RGB', (TW, TH), (24, 24, 26)); d = ImageDraw.Draw(im)
    s = TW / Wd
    lx0, ly0 = int(0.065 * s), int(0.012 * s)
    d.rounded_rectangle([lx0, ly0, lx0 + int(0.095 * s), ly0 + int(0.045 * s)], 10, fill=(70, 84, 72))
    d.text((lx0 + 20, ly0 + 18), '02:47   14 Oct', font=F('mono-bold', 26), fill=(20, 26, 22))
    d.text((lx0 + 20, ly0 + 60), 'Ext 201', font=F('mono', 24), fill=(20, 26, 22))
    d.text((lx0 + 20, ly0 + 100), 'No new calls', font=F('mono', 22), fill=(20, 26, 22))
    keys = [['1', '2', '3'], ['4', '5', '6'], ['7', '8', '9'], ['*', '0', '#']]
    kx0, ky0 = int(0.075 * s), int(0.075 * s); kw, kh = int(0.022 * s), int(0.014 * s)
    for r, row in enumerate(keys):
        for c, k in enumerate(row):
            x0, y0 = kx0 + c * (kw + 18), ky0 + r * (kh + 16)
            d.rounded_rectangle([x0, y0, x0 + kw, y0 + kh], 12, fill=(48, 48, 52))
            d.text((x0 + kw // 2, y0 + kh // 2), k, font=F('sans-bold', 34), fill=(225, 225, 228), anchor='mm')
    for i, lab in enumerate(['HOLD', 'XFER', 'CONF', 'MUTE', 'SPKR']):
        x0, y0 = int(0.18 * s), ky0 + i * 50
        d.rounded_rectangle([x0 - 70, y0, x0 + 70, y0 + 34], 10, fill=(40, 40, 44))
        d.text((x0, y0 + 17), lab, font=F('sans', 20), fill=(200, 200, 205), anchor='mm')
    d.ellipse([int(0.18 * s) - 60, int(0.012 * s), int(0.18 * s) + 60, int(0.012 * s) + 120], outline=(70, 70, 76), width=8)
    arr = np.asarray(im, np.float32) / 255 + (fbm(TH, TW, 25, 9)[..., None] - 0.5) * 0.02
    h = (np.asarray(im.convert('L'), np.float32) / 255)
    ft = save_set('DeskPhone_Face', arr, blur(h, 1.5), 3.0, cpx=(512, TH // 2), npx=(512, TH // 2))
    retile(m, 'Body', 0.12)
    mats = {'Body': mat(M, 'PlasticBlack'), 'Face': {'maps': ft, 'smoothness': 0.4, 'metallic': 0.0}, 'Rubber': mat(M, 'Rubber')}
    return write_model('DeskPhone', m, mats, {'kind': 'model', 'what': 'Office IP desk phone with corded handset, LCD and keypad (generic)'})

# ======================================================================= filing cabinet
def label_atlas():
    TW, TH = 512, 256
    im = Image.new('RGB', (TW, TH), (236, 232, 220)); d = ImageDraw.Draw(im)
    labs = ['A - F', 'G - M', 'N - S', 'T - Z', 'INVOICES', 'STOCK 2026', 'INSURANCE', 'PAYROLL', 'SUPPLIERS', 'VALUATIONS']
    for i, l in enumerate(labs):
        x0, y0 = (i % 2) * 256, (i // 2) * 51
        d.rectangle([x0 + 2, y0 + 2, x0 + 253, y0 + 48], fill=(240, 236, 224), outline=(150, 150, 150))
        d.text((x0 + 128, y0 + 25), l, font=F('sans-bold', 26), fill=(30, 40, 90), anchor='mm')
    return im, labs

def filing_cabinet(open_drawer=False):
    m = Mesh()
    Wc, Hc, Dc = 0.47, 1.32, 0.62
    t = 0.012
    add(m, 'Body', box((Wc, Hc - 0.06, Dc), center=(0, 0.06 + (Hc - 0.06) / 2, 0)))
    add(m, 'Plinth', box((Wc - 0.03, 0.06, Dc - 0.04), center=(0, 0.03, -0.01)))
    dh = (Hc - 0.06 - 0.02) / 4
    im, labs = label_atlas(); im.save(os.path.join(OUT, 'FilingCabinet_Labels.png'))
    for k in range(4):
        y0 = 0.07 + k * dh
        yc = y0 + dh / 2
        zoff = 0.36 if (open_drawer and k == 3) else 0.0
        zf = Dc / 2 + 0.011 + zoff
        add(m, 'Body', rbox((Wc - 0.012, dh - 0.006, 0.022), 0.004, center=(0, yc, zf), seg=2))
        # pull handle: recessed bar
        add(m, 'Handle', rbox((0.2, 0.022, 0.026), 0.008, center=(0, yc + dh * 0.18, zf + 0.018), seg=2))
        add(m, 'Recess', box((0.2, 0.012, 0.004), center=(0, yc + dh * 0.18 + 0.016, zf + 0.011)))
        # label holder + card
        add(m, 'Handle', rbox((0.09, 0.048, 0.004), 0.002, center=(0, yc + dh * 0.33, zf + 0.012), seg=1))
        li = [0, 1, 2, 3][3 - k]
        u0, v1 = (li % 2) * 0.5, 1 - (li // 2) * 0.2
        add(m, 'Label', quad((-0.04, yc + dh * 0.33 - 0.019, zf + 0.0145), (0.04, yc + dh * 0.33 - 0.019, zf + 0.0145),
                             (0.04, yc + dh * 0.33 + 0.019, zf + 0.0145), (-0.04, yc + dh * 0.33 + 0.019, zf + 0.0145), (u0 + 0.004, v1 - 0.196, u0 + 0.496, v1 - 0.004), (0, 0, 1)))
        if open_drawer and k == 3:
            # drawer box sides and bottom, with hanging files
            L = 0.55; zb = zf - 0.011 - L / 2
            for s in (-1, 1):
                add(m, 'Body', box((0.008, dh - 0.05, L), center=(s * (Wc / 2 - 0.03), yc - 0.02, zb)))
                add(m, 'Rail', box((0.012, 0.018, L + 0.2), center=(s * (Wc / 2 - 0.016), yc + dh / 2 - 0.06, zb - 0.1)))
            add(m, 'Body', box((Wc - 0.06, 0.008, L), center=(0, y0 + 0.03, zb)))
            add(m, 'Body', box((Wc - 0.06, dh - 0.05, 0.008), center=(0, yc - 0.02, zf - 0.011 - L)))
            for f in range(14):
                zz = zf - 0.06 - f * 0.036 + rng.uniform(-0.005, 0.005)
                lean = rng.uniform(-9, 9)
                fm = 'Folder' if f % 3 else 'FolderKraft'
                add(m, fm, box((Wc - 0.08, dh - 0.07, 0.003), center=(0, 0, 0)), chain(T(0, yc - 0.01, zz), Rx(lean)))
                add(m, fm, box((0.06, 0.018, 0.003), center=(0, 0, 0)), chain(T(-0.12 + (f % 4) * 0.08, yc + dh / 2 - 0.045, zz), Rx(lean)))
                add(m, 'Rail', box((Wc - 0.05, 0.006, 0.006), center=(0, yc + dh / 2 - 0.055, zz)))
            # a few loose sheets pulled up
            for f in range(3):
                add(m, 'Paper', box((0.21, 0.297, 0.001), center=(0, 0, 0)), chain(T(rng.uniform(-0.05, 0.05), yc + 0.06, zf - 0.15 - f * 0.1), Rx(rng.uniform(-25, 25)), Ry(rng.uniform(-10, 10))))
    add(m, 'Handle', cyl(0.009, 0.006, 16, y0=0), chain(T(Wc / 2 - 0.045, Hc - 0.035, Dc / 2), Rx(90)))
    retile(m, 'Body', 0.3)
    mats = {'Body': mat(M, 'PowderCoatGrey'), 'Plinth': mat(M, 'PlasticBlack'), 'Handle': mat(M, 'Aluminium'),
            'Recess': {'Kd': (0.05, 0.05, 0.05), 'smoothness': 0.3, 'metallic': 0.0},
            'Label': {'maps': {'Color': 'FilingCabinet_Labels.png'}, 'smoothness': 0.2, 'metallic': 0.0},
            'Rail': mat(M, 'Aluminium'), 'Folder': {'Kd': (0.18, 0.36, 0.22), 'smoothness': 0.15, 'metallic': 0.0},
            'FolderKraft': {'Kd': (0.62, 0.5, 0.34), 'smoothness': 0.15, 'metallic': 0.0}, 'Paper': {'Kd': (0.92, 0.92, 0.9), 'smoothness': 0.1, 'metallic': 0.0}}
    name = 'FilingCabinetOpen' if open_drawer else 'FilingCabinet'
    what = '4-drawer steel filing cabinet' + (' with the top drawer pulled out and hanging files rummaged' if open_drawer else ', closed')
    return write_model(name, m, mats, {'kind': 'model', 'what': what})

# ======================================================================= lever arch files
def lever_arch_files():
    TW, TH = 1024, 512
    im = Image.new('RGB', (TW, TH), (200, 200, 200)); d = ImageDraw.Draw(im)
    cols = [(28, 42, 92), (24, 24, 26), (96, 98, 104), (110, 26, 36), (28, 42, 92)]
    titles = ['INVOICES\n2026', 'STOCK\nREGISTER', 'INSURANCE', 'SAFE\nLOG', 'SUPPLIERS']
    for i, c in enumerate(cols):
        x0 = i * 160
        d.rectangle([x0, 0, x0 + 159, TH - 1], fill=c)
        # board texture speckle
        d.rectangle([x0 + 22, 40, x0 + 137, 210], fill=(240, 238, 230), outline=(180, 180, 180), width=3)
        d.multiline_text((x0 + 80, 125), titles[i], font=F('sans-bold', 22), fill=(25, 30, 70), anchor='mm', align='center')
        d.ellipse([x0 + 50, 330, x0 + 110, 390], fill=(15, 15, 15)); d.ellipse([x0 + 50, 330, x0 + 110, 390], outline=(170, 170, 175), width=6)
    # paper edge cell
    for y in range(0, TH, 3):
        g = 215 + int(rng.integers(-18, 18)); d.line([(800, y), (1023, y)], fill=(g, g, g - 5))
    arr = np.asarray(im, np.float32) / 255
    arr[:, :800] *= (0.92 + 0.12 * fbm(TH, 800, 40, 13))[..., None]
    tex = save_set('LeverArchFiles', arr, None, cpx=(512, 256))
    m = Mesh()
    w, h, dd = 0.075, 0.318, 0.285
    for i in range(5):
        x = (i - 2) * (w + 0.002)
        lean = 0 if i < 4 else -6
        P, UV, N = box((w, h, dd), center=(0, h / 2, 0))
        # face uv: spine (+Z) -> spine cell, covers (+-X) -> plain board area of the cell, back/top/bottom -> paper cell
        UV = UV.copy()
        u0, u1 = i * 160 / TW, (i * 160 + 159) / TW
        nrm = N[:, 0]
        for f in range(len(P)):
            n = nrm[f]
            if n[2] > 0.5: rect = (u0, 0, u1, 1)
            elif abs(n[0]) > 0.5: rect = (u0 + 0.002, 0.02, u0 + 0.01, 0.06)
            else: rect = (0.8, 0, 1.0, 1)
            UV[f] = np.stack([rect[0] + (rect[2] - rect[0]) * box_face_u(P[f], n, w, h, dd), rect[1] + (rect[3] - rect[1]) * box_face_v(P[f], n, w, h, dd)], -1)
        add(m, 'Binders', (P, UV, N), chain(T(x, 0, 0), Rz(lean)))
    mats = {'Binders': {'maps': tex, 'smoothness': 0.3, 'metallic': 0.0}}
    return write_model('LeverArchFiles', m, mats, {'kind': 'model', 'what': 'Row of five lever-arch files (spines with labels and finger holes) for office shelves'})

def box_face_u(Pf, n, w, h, dd):
    if abs(n[2]) > 0.5: return (Pf[:, 0] + w / 2) / w
    if abs(n[0]) > 0.5: return (Pf[:, 2] + dd / 2) / dd
    return (Pf[:, 0] + w / 2) / w

def box_face_v(Pf, n, w, h, dd):
    if abs(n[1]) > 0.5: return (Pf[:, 2] + dd / 2) / dd
    return Pf[:, 1] / h

# ======================================================================= DVR
def dvr():
    TW, TH = 1024, 128
    im = Image.new('RGB', (TW, TH), (20, 20, 22)); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, TW, 6], fill=(40, 40, 44))
    d.text((40, 64), 'NVR 8CH', font=F('sans-bold', 30), fill=(170, 170, 175), anchor='lm')
    for i, (lab, col) in enumerate([('PWR', (40, 220, 80)), ('HDD', (240, 140, 20)), ('NET', (40, 220, 80)), ('REC', (40, 40, 44))]):
        x = 640 + i * 90
        d.ellipse([x - 8, 46, x + 8, 62], fill=col); d.text((x, 92), lab, font=F('sans', 18), fill=(150, 150, 155), anchor='mm')
    d.rectangle([300, 40, 420, 88], outline=(60, 60, 64), width=3); d.text((360, 64), 'USB', font=F('sans', 18), fill=(120, 120, 125), anchor='mm')
    arr = np.asarray(im, np.float32) / 255
    tex = save_set('DVRRecorder_Front', arr, None, cpx=(512, 64))
    m = Mesh()
    Wd, Hd, Dd = 0.38, 0.045, 0.29
    add(m, 'Case', box((Wd, Hd, Dd), center=(0, 0.006 + Hd / 2, 0)))
    add(m, 'Front', quad((-Wd / 2, 0.006, Dd / 2 + 0.0005), (Wd / 2, 0.006, Dd / 2 + 0.0005), (Wd / 2, 0.006 + Hd, Dd / 2 + 0.0005), (-Wd / 2, 0.006 + Hd, Dd / 2 + 0.0005), (0, 0, 1, 1), (0, 0, 1)))
    for sx in (-1, 1):
        for sz in (-1, 1):
            add(m, 'Rubber', cyl(0.012, 0.006, 12, y0=0), T(sx * (Wd / 2 - 0.03), 0, sz * (Dd / 2 - 0.03)))
    retile(m, 'Case', 0.3)
    mats = {'Case': mat(M, 'PowderCoatGrey', Kd=(0.25, 0.25, 0.27), note='tint the powder-coat map dark (Kd 0.25) for a black steel case'),
            'Front': {'maps': tex, 'smoothness': 0.5, 'metallic': 0.0, 'emission': 'LED dots only; optional'}, 'Rubber': mat(M, 'Rubber')}
    return write_model('DVRRecorder', m, mats, {'kind': 'model', 'what': '8-channel CCTV network video recorder (1U desktop box) with status LEDs'})

if __name__ == '__main__':
    screen_lock('Monitor_Screen_Desktop.jpg'); screen_cctv('Monitor_Screen_CCTV.jpg')
    office_chair()
    monitor('ComputerMonitor', 'Monitor_Screen_Desktop.jpg', '24-inch flat-panel monitor on a stand; screen shows a generic lock screen')
    monitor('SecurityMonitor', 'Monitor_Screen_CCTV.jpg', 'Same 24-inch monitor showing a four-camera CCTV view (CAM 03 office feed lost)')
    keyboard(); mouse(); desk_phone(); filing_cabinet(False); filing_cabinet(True); lever_arch_files(); dvr()
