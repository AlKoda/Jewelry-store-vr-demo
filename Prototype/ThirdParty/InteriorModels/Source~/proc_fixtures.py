"""Project-made appliances, fixtures and soft furnishings: WaterCooler, CoffeeMachine, MiniFridge, CCTVDome,
TrackLight, RecessedDownlight, LEDPanel, ExitSign, SmokeDetector, WallVent, CeilingDiffuser, DoorLeverHandle,
DoorPullHandle, Rug, SnakePlant. Procedural geometry + procedural textures, project-owned."""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from proc_lib import *
from proc_materials import build, mat

M = build()
rng = np.random.default_rng(7)

def lathe_profile(profile, segs=32, mat_uv=(0, 0, 1, 1)):
    return lathe(profile, segs, uv=mat_uv)

# ======================================================================= water cooler
def water_cooler():
    m = Mesh()
    Wc, Hc, Dc = 0.31, 0.98, 0.32
    add(m, 'Body', rbox((Wc, Hc, Dc), 0.02, center=(0, Hc / 2, 0), seg=2))
    # dispensing recess (dark) + drip tray
    add(m, 'Recess', box((0.22, 0.26, 0.012), center=(0, 0.74, Dc / 2 - 0.004)))
    add(m, 'Tray', rbox((0.22, 0.02, 0.07), 0.006, center=(0, 0.62, Dc / 2 + 0.025), seg=1))
    for i in range(9):  # tray grille slots
        add(m, 'Recess', box((0.008, 0.003, 0.055), center=(-0.08 + i * 0.02, 0.631, Dc / 2 + 0.025)))
    # taps: red (hot) and blue (cold)
    for sx, mt in ((-0.05, 'TapRed'), (0.05, 'TapBlue')):
        add(m, mt, rbox((0.045, 0.05, 0.05), 0.01, center=(sx, 0.86, Dc / 2 + 0.02), seg=1))
        add(m, mt, cyl(0.007, 0.03, 10, y0=0), T(sx, 0.81, Dc / 2 + 0.03))
    # top collar and the inverted 19 l bottle
    add(m, 'Body', cyl(0.06, 0.03, 32, y0=Hc), T(0, 0, 0))
    R = 0.135
    prof = [(0.025, Hc + 0.0), (0.028, Hc + 0.06), (0.06, Hc + 0.09), (R - 0.01, Hc + 0.13), (R, Hc + 0.16)]
    for k in range(5):
        y = Hc + 0.18 + k * 0.065
        prof += [(R, y - 0.02), (R - 0.006, y), (R, y + 0.02)]
    prof += [(R, Hc + 0.5), (R - 0.02, Hc + 0.52), (0.0, Hc + 0.525)]
    add(m, 'Bottle', lathe_profile(prof, 40))
    add(m, 'Bottle', disk(0.025, Hc, 24, up=False))
    add(m, 'Vent', box((0.2, 0.15, 0.004), center=(0, 0.25, Dc / 2 + 0.001)))
    retile(m, 'Body', 0.2)
    vent = np.zeros((128, 256, 3), np.float32) + 0.8
    for i in range(0, 128, 12): vent[i:i + 6] = 0.2
    vt = save_set('WaterCooler_Vent', vent, None)
    mats = {'Body': mat(M, 'PlasticWhite'), 'Recess': {'Kd': (0.12, 0.12, 0.13), 'smoothness': 0.3, 'metallic': 0.0},
            'Tray': mat(M, 'PlasticWhite', Kd=(0.6, 0.6, 0.62)), 'TapRed': {'Kd': (0.7, 0.06, 0.05), 'smoothness': 0.6, 'metallic': 0.0},
            'TapBlue': {'Kd': (0.05, 0.22, 0.65), 'smoothness': 0.6, 'metallic': 0.0},
            'Bottle': {'Kd': (0.55, 0.75, 0.95), 'd': 0.35, 'Ks': 0.5, 'Ns': 200, 'smoothness': 0.92, 'metallic': 0.0,
                       'note': 'polycarbonate bottle: Standard Transparent/Fade, alpha 0.35'},
            'Vent': {'maps': vt, 'smoothness': 0.3, 'metallic': 0.0}}
    retile(m, 'Tray', 0.2)
    return write_model('WaterCooler', m, mats, {'kind': 'model', 'what': 'Floor-standing bottled water cooler with hot/cold taps and a 19 l blue bottle'})

# ======================================================================= coffee machine
def coffee_machine():
    m = Mesh()
    W, H, D = 0.30, 0.37, 0.42
    add(m, 'Body', rbox((W, H, D), 0.02, center=(0, H / 2, 0), seg=2))
    # front steel panel with display / buttons (texture)
    TW, TH = 512, 256
    im = Image.new('RGB', (TW, TH), (150, 152, 156)); d = ImageDraw.Draw(im)
    d.rounded_rectangle([156, 26, 356, 116], 12, fill=(12, 14, 16))
    d.text((256, 56), 'READY', font=F('sans-bold', 30), fill=(240, 240, 240), anchor='mm')
    d.text((256, 92), 'Espresso  Lungo  Cappuccino', font=F('sans', 14), fill=(170, 175, 180), anchor='mm')
    for i in range(4):
        x = 110 + i * 98
        d.ellipse([x - 26, 150, x + 26, 202], fill=(30, 30, 33)); d.ellipse([x - 21, 155, x + 21, 197], outline=(200, 200, 200), width=2)
    for i, t in enumerate(['1 CUP', '2 CUPS', 'HOT WATER', 'STEAM']):
        d.text((110 + i * 98, 224), t, font=F('sans', 13), fill=(30, 30, 30), anchor='mm')
    arr = np.asarray(im, np.float32) / 255
    streak = np.tile(np.random.default_rng(3).random((TH, 1)), (1, TW)).astype(np.float32)
    arr = arr * (0.96 + 0.06 * streak[..., None])
    ft = save_set('CoffeeMachine_Panel', arr, streak * 0.2, 1.0, metal=np.where(np.asarray(im.convert('L')) > 120, 1.0, 0.0), smooth=0.55, mpx=128)
    add(m, 'Panel', quad((-W / 2 + 0.02, H - 0.14, D / 2 + 0.002), (W / 2 - 0.02, H - 0.14, D / 2 + 0.002), (W / 2 - 0.02, H - 0.02, D / 2 + 0.002), (-W / 2 + 0.02, H - 0.02, D / 2 + 0.002), (0, 0, 1, 1), (0, 0, 1)))
    # brew recess, spout, drip tray
    add(m, 'Recess', box((0.2, 0.17, 0.01), center=(0, 0.125, D / 2 - 0.002)))
    add(m, 'Body', rbox((0.11, 0.035, 0.08), 0.01, center=(0, 0.225, D / 2 + 0.02), seg=1))
    for sx in (-0.015, 0.015):
        add(m, 'Steel', cyl(0.006, 0.02, 10, y0=0.188), T(sx, 0, D / 2 + 0.03))
    add(m, 'Steel', rbox((0.24, 0.025, 0.11), 0.006, center=(0, 0.0125, D / 2 + 0.03), seg=1))
    for i in range(10):
        add(m, 'Recess', box((0.012, 0.002, 0.08), center=(-0.09 + i * 0.02, 0.026, D / 2 + 0.03)))
    # bean hopper lid on top + steam wand on the right
    add(m, 'GlossBlack', rbox((0.16, 0.02, 0.16), 0.03, center=(-0.04, H + 0.008, -0.07), seg=2))
    add(m, 'Steel', tube([(W / 2 - 0.015, 0.29, D / 2 - 0.05), (W / 2 + 0.02, 0.27, D / 2 - 0.02), (W / 2 + 0.02, 0.12, D / 2)], 0.006, 10))
    # two cups
    for sx in (-0.03, 0.03):
        add(m, 'Cup', lathe([(0.0, 0.0), (0.022, 0.0), (0.026, 0.006), (0.03, 0.055), (0.026, 0.055), (0.022, 0.008), (0.0, 0.008)], 24), T(sx, 0.026, D / 2 + 0.03))
    retile(m, 'Body', 0.12)
    mats = {'Body': mat(M, 'PlasticBlack', smoothness=0.6), 'Panel': {'maps': ft, 'note': 'brushed steel fascia; display can glow'},
            'Recess': {'Kd': (0.02, 0.02, 0.02), 'smoothness': 0.3, 'metallic': 0.0}, 'Steel': mat(M, 'Chrome'),
            'GlossBlack': mat(M, 'GlossBlack'), 'Cup': {'Kd': (0.93, 0.93, 0.91), 'smoothness': 0.85, 'metallic': 0.0}}
    return write_model('CoffeeMachine', m, mats, {'kind': 'model', 'what': 'Bean-to-cup coffee machine (black body, brushed steel fascia, drip tray, two cups)'})

# ======================================================================= mini fridge
def mini_fridge():
    m = Mesh()
    W, H, D = 0.48, 0.84, 0.5
    add(m, 'Body', rbox((W, H - 0.02, D - 0.05), 0.012, center=(0, 0.02 + (H - 0.02) / 2, -0.025), seg=2))
    add(m, 'Door', rbox((W - 0.004, H - 0.03, 0.045), 0.01, center=(0, 0.025 + (H - 0.03) / 2, D / 2 - 0.025), seg=2))
    add(m, 'Gasket', box((W - 0.01, H - 0.04, 0.006), center=(0, 0.025 + (H - 0.03) / 2, D / 2 - 0.05)))
    add(m, 'Handle', rbox((0.2, 0.025, 0.02), 0.008, center=(0.0, H - 0.03, D / 2 + 0.008), seg=1))  # recessed top handle lip
    for sx in (-1, 1):
        for sz in (-1, 1):
            add(m, 'Rubber', cyl(0.015, 0.02, 12, y0=0), T(sx * (W / 2 - 0.04), 0, sz * (D / 2 - 0.06)))
    for i in range(10):  # rear condenser coil hint
        add(m, 'Coil', cyl(0.003, W - 0.12, 6, y0=-(W - 0.12) / 2), chain(T(0, 0.15 + i * 0.05, -D / 2 - 0.008), Rz(90)))
    retile(m, 'Body', 0.2); retile(m, 'Door', 0.2)
    mats = {'Body': mat(M, 'PlasticWhite', Kd=(0.95, 0.95, 0.95)), 'Door': mat(M, 'PlasticWhite'), 'Gasket': mat(M, 'Rubber'),
            'Handle': mat(M, 'Aluminium'), 'Rubber': mat(M, 'Rubber'), 'Coil': {'Kd': (0.1, 0.1, 0.1), 'metallic': 0.6, 'smoothness': 0.3}}
    return write_model('MiniFridge', m, mats, {'kind': 'model', 'what': 'Under-counter mini fridge (white, 48 x 84 x 50 cm)'})

# ======================================================================= CCTV dome
def cctv_dome():
    m = Mesh()
    add(m, 'Housing', lathe([(0.0, 0.0), (0.072, 0.0), (0.072, -0.012), (0.066, -0.026), (0.052, -0.03), (0.0, -0.03)], 40))
    dome = [(0.052 * math.cos(a), -0.03 - 0.052 * math.sin(a)) for a in np.linspace(0, math.pi / 2, 10)]
    add(m, 'Dome', lathe(dome, 40))
    # camera module inside: ball + lens
    add(m, 'Inner', lathe([(0.0, -0.03), (0.034, -0.032), (0.038, -0.05), (0.03, -0.068), (0.0, -0.072)], 24))
    add(m, 'Inner', cyl(0.012, 0.016, 16, y0=0), chain(T(0, -0.055, 0.018), Rx(60)))
    add(m, 'Lens', disk(0.009, 0.0, 16, up=True), chain(T(0, -0.055, 0.018), Rx(60), T(0, 0.0161, 0)))
    for k in range(6):  # IR LEDs ring hint
        a = k * 60
        add(m, 'IR', disk(0.003, 0.0, 8, up=True), chain(T(0, -0.055, 0.018), Rx(60), T(0.015 * math.cos(math.radians(a)), 0.0162, 0.015 * math.sin(math.radians(a)))))
    mats = {'Housing': mat(M, 'PlasticWhite'), 'Dome': {'Kd': (0.05, 0.05, 0.06), 'd': 0.55, 'Ks': 0.5, 'Ns': 220, 'smoothness': 0.95, 'metallic': 0.0,
            'note': 'smoked acrylic dome: Standard Transparent, alpha 0.55'}, 'Inner': mat(M, 'PlasticBlack'),
            'Lens': {'Kd': (0.02, 0.03, 0.05), 'smoothness': 0.95, 'metallic': 0.3}, 'IR': {'Kd': (0.25, 0.05, 0.05), 'smoothness': 0.8, 'metallic': 0.0,
            'emission': 'faint deep red (IR LEDs glow dimly at night)'}}
    retile(m, 'Housing', 0.2)
    return write_model('CCTVDome', m, mats, {'kind': 'model', 'what': 'Ceiling dome CCTV camera (14 cm), smoked dome over a tilted lens'}, pivot='none')

# ======================================================================= lights
def track_light():
    m = Mesh()
    L = 1.2
    add(m, 'Housing', rbox((L, 0.03, 0.035), 0.004, center=(0, -0.015, 0), seg=1))
    add(m, 'Housing', box((L - 0.02, 0.004, 0.012), center=(0, -0.031, 0)))
    for i, x in enumerate((-0.4, 0.0, 0.4)):
        tilt = (-25, 20, -15)[i]; yaw = (30, -10, 160)[i]
        add(m, 'Housing', rbox((0.06, 0.03, 0.04), 0.006, center=(x, -0.045, 0), seg=1))  # adaptor
        add(m, 'Housing', cyl(0.006, 0.05, 10, y0=-0.11), T(x, 0, 0))                       # stem
        head = chain(T(x, -0.12, 0), Ry(yaw), Rx(90 + tilt))
        add(m, 'Housing', lathe([(0.0, -0.06), (0.032, -0.06), (0.036, -0.05), (0.036, 0.06), (0.033, 0.065), (0.0, 0.065)], 28), head)
        for k in range(8):  # cooling fins
            add(m, 'Housing', cyl(0.039, 0.003, 28, y0=-0.04 + k * 0.007, caps=True), head)
        add(m, 'Reflector', lathe([(0.031, 0.066), (0.012, 0.03), (0.0, 0.03)], 28), head)
        add(m, 'Lens', disk(0.012, 0.031, 20, up=True), head)
    mats = {'Housing': {'Kd': (0.03, 0.03, 0.03), 'smoothness': 0.45, 'metallic': 0.2}, 'Reflector': mat(M, 'Chrome'),
            'Lens': {'Kd': (1.0, 0.95, 0.85), 'smoothness': 0.9, 'metallic': 0.0, 'emission': 'warm white 3000 K, intensity 3-6 when lit; spot light per head along its axis'}}
    return write_model('TrackLight', m, mats, {'kind': 'model', 'what': 'Black ceiling track (1.2 m) with three adjustable LED spot heads'}, pivot='ceiling')

def recessed_downlight():
    m = Mesh()
    add(m, 'Trim', lathe([(0.048, 0.0), (0.06, 0.0), (0.06, -0.003), (0.056, -0.005), (0.048, -0.005)], 40))
    add(m, 'Reflector', lathe([(0.048, -0.005), (0.042, 0.03), (0.025, 0.055)], 40))
    add(m, 'Lens', disk(0.025, 0.055, 32, up=False))
    mats = {'Trim': mat(M, 'PlasticWhite', Kd=(0.95, 0.95, 0.95)), 'Reflector': mat(M, 'Aluminium', smoothness=0.6),
            'Lens': {'Kd': (1.0, 0.97, 0.9), 'smoothness': 0.8, 'metallic': 0.0, 'emission': 'warm white, intensity 4-8 when lit'}}
    retile(m, 'Trim', 0.2)
    return write_model('RecessedDownlight', m, mats, {'kind': 'model', 'what': 'Recessed LED downlight (12 cm trim); y = 0 is the ceiling surface, the reflector rises into the ceiling'}, pivot='none')

def led_panel():
    m = Mesh()
    S = 0.595
    add(m, 'Frame', rbox((S, 0.012, S), 0.003, center=(0, -0.006, 0), seg=1))
    add(m, 'Diffuser', quad((-S / 2 + 0.012, -0.0125, S / 2 - 0.012), (S / 2 - 0.012, -0.0125, S / 2 - 0.012), (S / 2 - 0.012, -0.0125, -S / 2 + 0.012), (-S / 2 + 0.012, -0.0125, -S / 2 + 0.012), (0, 0, 1, 1), (0, -1, 0)))
    P, UV, N = m.parts['Diffuser']; m.parts['Diffuser'][0] = P[:, ::-1]; m.parts['Diffuser'][1] = UV[:, ::-1]
    mats = {'Frame': mat(M, 'PlasticWhite', Kd=(0.93, 0.93, 0.93)), 'Diffuser': {'Kd': (0.96, 0.96, 0.97), 'smoothness': 0.2, 'metallic': 0.0,
            'emission': 'cool white 4000 K, intensity 1.5-3 when lit (back office / side rooms)'}}
    retile(m, 'Frame', 0.2)
    return write_model('LEDPanel', m, mats, {'kind': 'model', 'what': '600 x 600 mm LED ceiling panel (lay-in grid or surface mount)'}, pivot='ceiling')

# ======================================================================= exit sign
def exit_sign_texture(path):
    TW, TH = 1024, 384
    im = Image.new('RGB', (TW, TH), (0, 128, 72)); d = ImageDraw.Draw(im)
    wht = (245, 250, 245)
    # door (left), running figure (centre-left), arrow (right) and EXIT lettering - original drawing
    d.rectangle([60, 60, 250, 324], outline=wht, width=16); d.rectangle([60, 60, 250, 324], fill=None)
    d.rectangle([76, 76, 234, 308], fill=(245, 250, 245)); d.rectangle([92, 92, 218, 308], fill=(0, 128, 72))
    # running figure, mirrored so it runs left towards the door (xc = 340)
    poly = [(300, 150), (360, 150), (395, 215), (450, 230), (445, 255), (380, 245), (365, 225), (350, 270), (395, 310), (380, 335), (320, 290), (300, 240), (260, 310), (230, 300), (275, 220), (285, 180), (245, 210), (230, 195)]
    poly = [(680 - px, py) for px, py in poly]
    d.ellipse([350 - 26, 110 - 26, 350 + 26, 110 + 26], fill=wht)
    d.polygon(poly, fill=wht)
    d.polygon([(500, 192), (575, 122), (575, 162), (680, 162), (680, 222), (575, 222), (575, 262)], fill=wht)
    d.text((845, 196), 'EXIT', font=F('sans-bold', 112), fill=wht, anchor='mm')
    im.save(os.path.join(OUT, path))

def exit_sign():
    exit_sign_texture('ExitSign_Face.png')
    m = Mesh()
    W, H, D = 0.36, 0.135, 0.028
    add(m, 'Housing', rbox((W, H, D), 0.006, center=(0, 0, D / 2), seg=1))
    add(m, 'Face', quad((-W / 2 + 0.008, -H / 2 + 0.008, D + 0.0005), (W / 2 - 0.008, -H / 2 + 0.008, D + 0.0005), (W / 2 - 0.008, H / 2 - 0.008, D + 0.0005), (-W / 2 + 0.008, H / 2 - 0.008, D + 0.0005), (0, 0, 1, 1), (0, 0, 1)))
    mats = {'Housing': mat(M, 'PlasticWhite', Kd=(0.94, 0.94, 0.94)),
            'Face': {'maps': {'Color': 'ExitSign_Face.png'}, 'smoothness': 0.7, 'metallic': 0.0, 'emission': 'use the Color map as emission, intensity 1.2-2 (maintained exit sign, always lit)'}}
    retile(m, 'Housing', 0.2)
    return write_model('ExitSign', m, mats, {'kind': 'model', 'what': 'Illuminated wall-mounted exit sign (green running-figure pictogram, arrow, EXIT; original artwork)'}, pivot='wall')

def smoke_detector():
    m = Mesh()
    add(m, 'Body', lathe([(0.0, 0.0), (0.058, 0.0), (0.058, -0.012), (0.055, -0.03), (0.046, -0.042), (0.0, -0.045)], 40))
    for k in range(12):  # smoke entry slots
        a = k * 30
        add(m, 'Slot', box((0.003, 0.01, 0.016), center=(0, 0, 0)), chain(Ry(a), T(0.052, -0.022, 0)))
    add(m, 'LED', disk(0.0025, 0.0, 12, up=False), T(0.02, -0.0435, 0.0))
    mats = {'Body': mat(M, 'PlasticWhite', Kd=(0.95, 0.95, 0.93)), 'Slot': {'Kd': (0.25, 0.25, 0.25), 'smoothness': 0.2, 'metallic': 0.0},
            'LED': {'Kd': (0.8, 0.05, 0.05), 'smoothness': 0.8, 'metallic': 0.0, 'emission': 'blink red every ~40 s'}}
    retile(m, 'Body', 0.2)
    return write_model('SmokeDetector', m, mats, {'kind': 'model', 'what': 'Ceiling optical smoke detector (11.6 cm) with status LED'}, pivot='ceiling')

def wall_vent():
    m = Mesh()
    W, H = 0.42, 0.22
    fr = 0.025
    add(m, 'Grille', box((W, fr, 0.012), center=(0, H / 2 - fr / 2, 0.006)))
    add(m, 'Grille', box((W, fr, 0.012), center=(0, -H / 2 + fr / 2, 0.006)))
    add(m, 'Grille', box((fr, H, 0.012), center=(-W / 2 + fr / 2, 0, 0.006)))
    add(m, 'Grille', box((fr, H, 0.012), center=(W / 2 - fr / 2, 0, 0.006)))
    n = 9
    for i in range(n):
        y = -H / 2 + fr + (i + 0.5) * (H - 2 * fr) / n
        add(m, 'Grille', box((W - 2 * fr, 0.0015, 0.022), center=(0, 0, 0)), chain(T(0, y, 0.004), Rx(-40)))
    add(m, 'Duct', box((W - 2 * fr, H - 2 * fr, 0.002), center=(0, 0, -0.01)))
    mats = {'Grille': mat(M, 'PlasticWhite', Kd=(0.93, 0.93, 0.92)), 'Duct': {'Kd': (0.03, 0.03, 0.03), 'smoothness': 0.1, 'metallic': 0.0}}
    retile(m, 'Grille', 0.2)
    return write_model('WallVent', m, mats, {'kind': 'model', 'what': 'Wall return-air grille 42 x 22 cm with angled louvres'}, pivot='wall')

def ceiling_diffuser():
    m = Mesh()
    S = 0.6
    rings = [(0.3, 0.24), (0.24, 0.185), (0.185, 0.13), (0.13, 0.075)]
    for k, (a, b) in enumerate(rings):
        y0 = -0.006 - k * 0.012
        for rot in range(4):
            q = quad((-a, y0, a), (a, y0, a), (b, y0 - 0.02, b), (-b, y0 - 0.02, b), (0, 0, 1, 1))
            P, UV, N = q
            fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
            if fn[0][1] > 0: P = P[:, ::-1]; UV = UV[:, ::-1]
            add(m, 'Diffuser', (P, UV, None), Ry(90 * rot))
    add(m, 'Diffuser', box((0.15, 0.004, 0.15), center=(0, -0.06, 0)))
    for rot in range(4):
        add(m, 'Diffuser', box((S, 0.006, 0.03), center=(0, -0.003, S / 2 - 0.015)), Ry(90 * rot))
    add(m, 'Duct', box((S - 0.04, 0.002, S - 0.04), center=(0, 0.004, 0)))
    mats = {'Diffuser': mat(M, 'PlasticWhite', Kd=(0.94, 0.94, 0.94)), 'Duct': {'Kd': (0.03, 0.03, 0.03), 'smoothness': 0.1, 'metallic': 0.0}}
    retile(m, 'Diffuser', 0.2)
    return write_model('CeilingDiffuser', m, mats, {'kind': 'model', 'what': 'Square 4-way ceiling air diffuser 60 x 60 cm'}, pivot='ceiling')

# ======================================================================= door hardware
def door_lever():
    m = Mesh()
    add(m, 'Steel', lathe([(0.0, 0.0), (0.026, 0.0), (0.026, 0.006), (0.022, 0.01), (0.0, 0.01)], 32), Rx(90))
    add(m, 'Steel', cyl(0.011, 0.05, 20, y0=0.0), Rx(90))
    path = [(0, 0, 0.058)] + [(0.0 + 0.012 * math.sin(t * math.pi / 2) , 0, 0.058 - 0.0) for t in (0.3,)]
    lever = tube([(0.0, 0.0, 0.05), (0.0, 0.0, 0.062), (0.015, 0.0, 0.068), (0.06, 0.0, 0.068), (0.13, 0.0, 0.066)], 0.0095, 16)
    add(m, 'Steel', lever)
    add(m, 'Steel', lathe([(0.0, 0.0), (0.016, 0.0), (0.016, 0.004), (0.0, 0.004)], 24), chain(Rx(90), T(0, -0.055, 0)))
    add(m, 'Keyhole', box((0.004, 0.012, 0.001), center=(0, -0.055, 0.0045)))
    add(m, 'Keyhole', cyl(0.0035, 0.001, 12, y0=0), chain(T(0, -0.05, 0.004), Rx(90)))
    mats = {'Steel': mat(M, 'BrushedSteel'), 'Keyhole': {'Kd': (0.02, 0.02, 0.02), 'smoothness': 0.2, 'metallic': 0.0}}
    retile(m, 'Steel', 0.1)
    return write_model('DoorLeverHandle', m, mats, {'kind': 'model', 'what': 'Brushed stainless lever handle on round rose with an escutcheon below (one side of the door; mirror X for the other hand)'}, pivot='none')

def door_pull():
    m = Mesh()
    L = 0.6; r = 0.0125
    add(m, 'Steel', cyl(r, L, 24, y0=-L / 2), T(0, 0, 0.065))
    for y in (-0.2, 0.2):
        add(m, 'Steel', cyl(0.009, 0.065, 16, y0=0), chain(T(0, y, 0), Rx(90)))
        add(m, 'Steel', cyl(0.02, 0.006, 20, y0=0), chain(T(0, y, 0), Rx(90)))
    mats = {'Steel': mat(M, 'BrushedSteel')}
    retile(m, 'Steel', 0.1)
    return write_model('DoorPullHandle', m, mats, {'kind': 'model', 'what': 'Stainless steel pull bar (60 cm, 40 cm centres) for the glass shop doors'}, pivot='none')

# ======================================================================= rug
def rug():
    TW, TH = 1024, 716
    y, x = np.mgrid[0:TH, 0:TW].astype(np.float32)
    base = np.array([0.56, 0.52, 0.46])
    pile = fbm(TH, TW, 3, 21, 3) * 0.6 + fbm(TH, TW, 40, 22, 3) * 0.4
    col = np.ones((TH, TW, 3), np.float32) * base
    # border bands
    bx = np.minimum(x, TW - 1 - x) / TW; by = np.minimum(y, TH - 1 - y) / TH * (TH / TW)
    bd = np.minimum(bx, by)
    band1 = (bd > 0.025) & (bd < 0.045); band2 = (bd > 0.06) & (bd < 0.066)
    col[band1] = np.array([0.30, 0.31, 0.33]); col[band2] = np.array([0.72, 0.62, 0.42])
    # soft abstract field: large low-contrast strokes
    f = fbm(TH, TW, 260, 23, 4)
    inner = bd > 0.07
    col[inner] = col[inner] * (0.94 + 0.12 * f[inner][:, None])
    col = col * (0.88 + 0.24 * pile[..., None])
    h = pile * 0.8 + (band1 | band2) * 0.15
    tex = save_set('Rug', col, h, 3.0, 0.0, 0.05, cpx=(1024, 716), npx=(512, 358))
    m = Mesh()
    W, D, T_ = 2.0, 1.4, 0.012
    P, UV, N = rbox((W, T_, D), 0.005, center=(0, T_ / 2, 0), seg=1)
    UV = np.stack([(P[..., 0] + W / 2) / W, 1 - (P[..., 2] + D / 2) / D], -1)
    add(m, 'Rug', (P, UV, N))
    mats = {'Rug': {'maps': tex, 'smoothness': 0.05, 'metallic': 0.0}}
    return write_model('Rug', m, mats, {'kind': 'model', 'what': 'Wool area rug 2.0 x 1.4 m, warm grey with a charcoal and brass border (original pattern)'})

# ======================================================================= snake plant
def snake_plant():
    TW, TH = 256, 1024
    y, x = np.mgrid[0:TH, 0:TW].astype(np.float32)
    u = x / TW; v = y / TH
    band = np.sin(v * 60 + 6 * fbm(TH, TW, 120, 31)) * 0.5 + 0.5
    zig = np.sin(v * 140 + u * 12 + 4 * fbm(TH, TW, 40, 32))
    dark = np.array([0.05, 0.16, 0.07]); light = np.array([0.32, 0.45, 0.22])
    col = dark + (light - dark) * (0.25 + 0.5 * band[..., None] * (zig[..., None] > 0.3))
    margin = (np.abs(u - 0.5) > 0.42)
    col[margin] = np.array([0.78, 0.72, 0.25]) * (0.9 + 0.1 * fbm(TH, TW, 30, 33)[margin][:, None])
    col *= (0.85 + 0.25 * fbm(TH, TW, 80, 34))[..., None]
    lt = save_set('SnakePlant_Leaf', col, band * 0.2 + fbm(TH, TW, 20, 35) * 0.3, 1.5, 0.0, 0.45, cpx=(128, 512), npx=(128, 512))
    m = Mesh()
    # pot: tapered matt ceramic cylinder with a rim and soil
    add(m, 'Pot', lathe([(0.0, 0.0), (0.105, 0.0), (0.112, 0.006), (0.13, 0.27), (0.122, 0.275), (0.118, 0.255), (0.0, 0.255)], 40))
    add(m, 'Soil', disk(0.118, 0.255, 32, up=True))
    for i in range(20):
        ang = rng.uniform(0, 360); r0 = rng.uniform(0.0, 0.075)
        h = rng.uniform(0.5, 0.85) * (1.0 if r0 < 0.04 else 0.8)
        w = rng.uniform(0.055, 0.085)
        lean = rng.uniform(4, 16) * (r0 / 0.07 + 0.3); twist = rng.uniform(-40, 40)
        nseg = 10
        rows = []
        for j in range(nseg + 1):
            t = j / nseg
            width = w * (math.sin(math.pi * min(0.15 + 0.85 * t, 1.0)) ** 0.5) * (1 - t ** 3) + 0.002
            if t > 0.92: width *= (1 - t) / 0.08
            yy = 0.24 + h * t
            off = math.radians(lean) * h * t * t * 0.5
            tw = math.radians(twist * t)
            ridge = 0.008 * (1 - t)
            row = []
            for k, s in enumerate((-1, 0, 1)):
                lx = s * width / 2 * math.cos(tw); lz = s * width / 2 * math.sin(tw) + (ridge if s == 0 else 0)
                row.append((lx, yy, lz + off))
            rows.append(row)
        G = np.array(rows)
        UVg = np.stack(np.meshgrid([0.0, 0.5, 1.0], np.linspace(0, 1, nseg + 1)), -1)
        P, UV, _ = grid_surface(G, UVg)
        Nf = smooth_normals(P, 80)
        Mx = chain(T(r0 * math.cos(math.radians(ang)), 0, r0 * math.sin(math.radians(ang))), Ry(ang))
        add(m, 'Leaf', (P, UV, Nf), Mx); add(m, 'Leaf', (P[:, ::-1], UV[:, ::-1], -Nf[:, ::-1]), Mx)
    retile(m, 'Pot', 0.3)
    st = np.dstack([0.16 + 0.05 * fbm(256, 256, 20, 40)] * 3) * np.array([1.0, 0.85, 0.7])
    soil = save_set('SnakePlant_Soil', st, fbm(256, 256, 6, 41), 4.0, 0.0, 0.05)
    mats = {'Pot': mat(M, 'PlasticBlack', Kd=(0.35, 0.34, 0.33), smoothness=0.25, note='matt charcoal ceramic: tint the plastic map grey'),
            'Soil': {'maps': soil, 'smoothness': 0.05, 'metallic': 0.0}, 'Leaf': {'maps': lt, 'smoothness': 0.45, 'metallic': 0.0, 'note': 'two-sided leaves (duplicated faces)'}}
    return write_model('SnakePlant', m, mats, {'kind': 'model', 'what': 'Snake plant (Sansevieria laurentii) in a charcoal ceramic pot, about 1.0 m tall'})

if __name__ == '__main__':
    water_cooler(); coffee_machine(); mini_fridge(); cctv_dome(); track_light(); recessed_downlight(); led_panel()
    exit_sign(); smoke_detector(); wall_vent(); ceiling_diffuser(); door_lever(); door_pull(); rug(); snake_plant()
