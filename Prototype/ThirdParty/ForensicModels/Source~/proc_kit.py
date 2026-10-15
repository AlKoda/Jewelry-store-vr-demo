"""Forensic kit: FingerprintBrush, FingerprintPowderJar, NitrileGloveBox, ForensicCase, TacticalFlashlight, UVTorch,
PoliceRadio. Project-made geometry and textures."""
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
import meshkit as mk

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)


def img(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))


def arr(im):
    return np.asarray(im, np.float32) / 255


def report(name, m):
    lo, hi = m.bounds(); print(name, m.tris(), np.round(hi - lo, 4))


def lathe_v(profile):
    p = np.asarray(profile, float)
    L = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(p, axis=0), axis=1))])
    return L / L[-1]


# ---------------------------------------------------------------- fingerprint brush
def brush():
    S = 512
    col = np.zeros((S, S, 3), np.float32); hgt = np.zeros((S, S), np.float32)
    ms_m = np.zeros((S, S), np.float32); ms_s = np.zeros((S, S), np.float32)
    # u 0..0.4 handle (black lacquered wood), 0.4..0.55 ferrule (nickel), 0.55..1 fibreglass head (pale gold)
    a, b = int(0.4 * S), int(0.55 * S)
    n = mk.noise(S, S, 30, 3, 4)
    col[:, :a] = 0.035 + 0.02 * n[:, :a, None]; ms_s[:, :a] = 0.72; hgt[:, :a] = n[:, :a] * 0.1
    col[:, a:b] = 0.62 + 0.08 * n[:, a:b, None]; ms_m[:, a:b] = 1; ms_s[:, a:b] = 0.7
    rows = np.arange(S)[:, None]
    hgt[:, a:b] = 0.5 + 0.5 * np.sin(rows / S * 2 * math.pi * 6) * np.ones((1, b - a))  # crimp rings
    rng = np.random.default_rng(4)
    strands = np.repeat(rng.random((1, S - b)), S, 0)
    strands = np.asarray(Image.fromarray((strands * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6)), np.float32) / 255
    fib = 0.75 + 0.25 * strands + 0.08 * (n[:, b:] - 0.5)
    col[:, b:] = np.dstack([fib * 0.86, fib * 0.74, fib * 0.50]); ms_s[:, b:] = 0.45; hgt[:, b:] = strands
    # strands run along v, so the stripes must vary along u: transpose region logic -> use columns as strands
    mk.save_color(os.path.join(OUT, 'FingerprintBrush_Color.jpg'), col, 90)
    Image.fromarray(mk.normal_from_height(hgt, 3)).save(os.path.join(OUT, 'FingerprintBrush_Normal.jpg'), quality=92)
    mk.save_ms(os.path.join(OUT, 'FingerprintBrush_MetallicSmoothness.png'), ms_m, ms_s)
    m = mk.Mesh()
    handle = [(0, 0), (0.0035, 0.0003), (0.0045, 0.002), (0.0050, 0.01), (0.0056, 0.05), (0.0052, 0.085), (0.0049, 0.094)]
    P, UV, N = mk.lathe(handle, 20, uv=(0, 0, 0.4, 1)); m.add('BrushAtlas', P, UV, N)
    fer = [(0.0049, 0.094), (0.0058, 0.0945), (0.0059, 0.111), (0.0056, 0.1115)]
    P, UV, N = mk.lathe(fer, 20, uv=(0.4, 0, 0.55, 1), smooth=False); m.add('BrushAtlas', P, UV, mk.smooth_normals(P, 35))
    head = [(0.0054, 0.111), (0.0075, 0.116), (0.0105, 0.124), (0.0122, 0.132), (0.0118, 0.140), (0.0095, 0.146), (0.006, 0.1495),
            (0.0, 0.1505)]
    # fibre head: u (around) carries the strand stripes so they run tip-to-ferrule
    P, UV, N = mk.lathe(head, 28, uv=(0.55, 0, 1.0, 1)); m.add('BrushAtlas', P, UV, N)
    m.ground()
    m.write(os.path.join(OUT, 'FingerprintBrush.obj'), {'BrushAtlas': dict(map='FingerprintBrush_Color.jpg')})
    report('FingerprintBrush', m)


# ---------------------------------------------------------------- powder jar
def powder_jar():
    W, H = 1024, 512
    col = np.ones((H, W, 3), np.float32) * 0.93
    n = mk.noise(H, W, 40, 8, 4)
    col += (n[..., None] - 0.5) * 0.03
    im = img(col); d = ImageDraw.Draw(im)
    # label band: rows 0..256 (v 0.5..1)
    d.rectangle([0, 0, W, 256], fill=(245, 245, 242))
    d.rectangle([0, 0, W, 40], fill=(20, 20, 22)); d.rectangle([0, 216, W, 256], fill=(20, 20, 22))
    fH = mk.font('sans-bold', 46); f = mk.font('sans', 22); fb = mk.font('sans-bold', 22)
    for x0 in (30, 542):
        d.text((x0, 56), 'BLACK', font=fH, fill=(15, 15, 15))
        d.text((x0, 108), 'DUSTING POWDER', font=fb, fill=(15, 15, 15))
        d.text((x0, 140), 'Latent print development', font=f, fill=(60, 60, 60))
        d.text((x0, 172), 'Net 57 g    Lot 26-114', font=f, fill=(60, 60, 60))
    d.text((40, 6), 'FOR FORENSIC USE ONLY', font=mk.font('sans-bold', 22), fill=(240, 200, 30))
    d.text((552, 6), 'KEEP LID CLOSED', font=mk.font('sans-bold', 22), fill=(240, 200, 30))
    # cap region rows 300..512: ribbed black
    capr = np.zeros((212, W), np.float32)
    capc = np.ones((212, W, 3), np.float32) * 0.04
    im.paste(img(capc), (0, 300))
    a = arr(im)
    hgt = np.zeros((H, W), np.float32)
    cols = np.arange(W)[None, :]
    hgt[300:] = (0.5 + 0.5 * np.sin(cols / W * 2 * math.pi * 90)) * np.ones((212, 1))
    ms_s = np.full((H, W), 0.45, np.float32); ms_s[300:] = 0.35; ms_s[:256] = 0.3
    mk.save_color(os.path.join(OUT, 'FingerprintPowderJar_Color.jpg'), np.asarray(im.filter(ImageFilter.GaussianBlur(0.4))), 90)
    Image.fromarray(mk.normal_from_height(hgt, 2.0)).resize((512, 256)).save(os.path.join(OUT, 'FingerprintPowderJar_Normal.jpg'), quality=92)
    mk.save_ms(os.path.join(OUT, 'FingerprintPowderJar_MetallicSmoothness.png'), np.zeros((128, 256)), np.asarray(Image.fromarray((ms_s * 255).astype(np.uint8)).resize((256, 128)), np.float32) / 255)
    m = mk.Mesh()
    body = [(0, 0), (0.024, 0), (0.0275, 0.002), (0.028, 0.006), (0.028, 0.008), (0.028, 0.028), (0.028, 0.048), (0.028, 0.050), (0.026, 0.055), (0.0225, 0.057), (0.0225, 0.062)]
    v = lathe_v(body)
    P, UV, N = mk.lathe(body, 40, uv=(0, 0.42, 1, 0.42)); UV = UV.copy()
    # map the straight wall (y 0.006..0.050) to the label band v 0.5..1, the rest to plain white
    ys = P[..., 1]
    lab = (ys >= 0.008) & (ys <= 0.048)
    UV[..., 1] = np.where(lab, 0.5 + 0.5 * (ys - 0.008) / 0.040, 0.45)
    # wrap: lathe u runs 0..1 around; keep
    m.add('JarAtlas', P, UV, mk.smooth_normals(P, 50))
    P, UV, N = mk.disk(0.024, 0.0, 30, (0.5, 0.45), 0.0, up=False); m.add('JarAtlas', P, UV, N)
    cap = [(0.0, 0.0805), (0.026, 0.0805), (0.0295, 0.079), (0.0300, 0.077), (0.0300, 0.061), (0.0290, 0.060), (0.0235, 0.060)]
    cap = cap[::-1]
    P, UV, N = mk.lathe(cap, 40, uv=(0, 0.02, 1, 0.38)); m.add('JarAtlas', P, UV, mk.smooth_normals(P, 50))
    m.ground()
    m.write(os.path.join(OUT, 'FingerprintPowderJar.obj'), {'JarAtlas': dict(map='FingerprintPowderJar_Color.jpg')})
    report('FingerprintPowderJar', m)


# ---------------------------------------------------------------- nitrile glove box
def glove_box():
    S = 1024
    Lx, Hy, Dz = 0.235, 0.065, 0.125
    base = np.ones((S, S, 3), np.float32) * 0.95 + (mk.noise(S, S, 50, 12, 4)[..., None] - 0.5) * 0.02
    im = img(base); d = ImageDraw.Draw(im)
    blue = (28, 78, 168)
    top_h = int(S * Dz / Lx)            # 545 rows: top face
    side_h = int(S * Hy / Lx)           # 283 rows: long sides
    # top face art
    d.rectangle([0, 0, S, 70], fill=blue); d.rectangle([0, top_h - 70, S, top_h], fill=blue)
    fB = mk.font('sans-bold', 40); f = mk.font('sans', 22); fS = mk.font('sans-bold', 26)
    d.text((40, 12), 'NITRILE EXAMINATION GLOVES', font=fB, fill=(255, 255, 255))
    d.text((40, top_h - 56), 'POWDER-FREE  -  NON-STERILE  -  AMBIDEXTROUS', font=fS, fill=(255, 255, 255))
    cx, cy = S / 2, top_h / 2
    # dispenser opening (perforated oval, torn open) -> dark interior
    d.ellipse([cx - 230, cy - 95, cx + 230, cy + 95], fill=(30, 40, 60))
    for k in range(80):
        t = 2 * math.pi * k / 80
        d.ellipse([cx + 240 * math.cos(t) - 2, cy + 104 * math.sin(t) - 2, cx + 240 * math.cos(t) + 2, cy + 104 * math.sin(t) + 2], fill=(150, 150, 150))
    d.text((40, 90), '100', font=mk.font('sans-bold', 60), fill=blue)
    d.text((40, 156), 'GLOVES', font=f, fill=blue)
    d.rounded_rectangle([S - 150, 90, S - 40, 200], 14, fill=blue)
    d.text((S - 118, 100), 'M', font=mk.font('sans-bold', 80), fill=(255, 255, 255))
    # long side art
    y0 = top_h
    d.rectangle([0, y0, S, y0 + side_h], fill=(246, 246, 244))
    d.rectangle([0, y0, 300, y0 + side_h], fill=blue)
    d.text((24, y0 + 40), 'NITRILE', font=mk.font('sans-bold', 54), fill=(255, 255, 255))
    d.text((24, y0 + 110), 'EXAM GLOVES', font=fS, fill=(255, 255, 255))
    d.text((24, y0 + 190), 'SIZE M  -  100', font=fS, fill=(255, 255, 255))
    d.text((330, y0 + 50), 'Single use only. Latex free.', font=f, fill=(40, 40, 40))
    d.text((330, y0 + 90), 'Store in a cool, dry place away from', font=f, fill=(40, 40, 40))
    d.text((330, y0 + 120), 'direct sunlight. Lot 2609-17', font=f, fill=(40, 40, 40))
    # hand outline icon
    hx, hy = 860, y0 + 140
    d.rounded_rectangle([hx - 45, hy - 20, hx + 45, hy + 90], 30, outline=blue, width=6)
    for k in range(4):
        d.rounded_rectangle([hx - 42 + k * 22, hy - 100, hx - 26 + k * 22, hy - 10], 8, outline=blue, width=6)
    d.rounded_rectangle([hx + 45, hy, hx + 90, hy + 18], 8, outline=blue, width=6)
    # end faces
    y1 = top_h + side_h
    d.rectangle([0, y1, S, S], fill=blue)
    d.text((30, y1 + 50), 'M', font=mk.font('sans-bold', 110), fill=(255, 255, 255))
    d.text((542, y1 + 50), 'M', font=mk.font('sans-bold', 110), fill=(255, 255, 255))
    d.text((150, y1 + 80), 'NITRILE', font=fS, fill=(255, 255, 255)); d.text((662, y1 + 80), 'NITRILE', font=fS, fill=(255, 255, 255))
    im = im.filter(ImageFilter.GaussianBlur(0.4))
    mk.save_color(os.path.join(OUT, 'NitrileGloveBox_Color.jpg'), np.asarray(im), 88)
    vt = 1 - top_h / S; vs = 1 - (top_h + side_h) / S
    rects = {'py': (0, vt, 1, 1), 'pz': (0, vs, 1, vt), 'nz': (0, vs, 1, vt), 'px': (0, 0, 0.5, vs), 'nx': (0.5, 0, 1, vs),
             'ny': (0, vs, 1, vt)}
    P, UV, N = mk.rounded_box((Lx, Hy, Dz), 0.0025, 2, uv_rects=rects)
    # rounded_box maps faces with (u along first in-plane axis, v along second); fix orientations for top & sides
    m = mk.Mesh(); m.add('GloveBoxCard', P, UV, N)
    # blue glove tuft emerging from the opening
    rng = np.random.default_rng(9)
    prof = [(0.0, 0.0), (0.045, 0.0), (0.05, 0.006), (0.046, 0.014), (0.036, 0.022), (0.022, 0.03), (0.008, 0.034), (0.0, 0.035)]
    segs = 36
    P, UV, N = mk.lathe(prof, segs, uv=(0, 0, 1, 1))
    ang = np.arctan2(-P[..., 2], P[..., 0])
    rad = np.linalg.norm(P[..., [0, 2]], axis=-1)
    bump = 1 + 0.18 * np.sin(ang * 5 + 0.7) + 0.1 * np.sin(ang * 11 + 2.1) + 0.06 * np.sin(P[..., 1] * 600)
    P = P.copy(); P[..., 0] *= bump * 1.9; P[..., 2] *= bump * 0.75; P[..., 1] *= (1 + 0.25 * np.sin(ang * 3))
    UV = np.full_like(UV, 0.5)
    m.add('GloveNitrile', P + [0, Hy / 2 - 0.004, 0], UV, mk.smooth_normals(P + [0, Hy / 2 - 0.004, 0], 70))
    m.ground()
    m.write(os.path.join(OUT, 'NitrileGloveBox.obj'), {'GloveBoxCard': dict(map='NitrileGloveBox_Color.jpg'),
                                                       'GloveNitrile': dict(Kd=(0.22, 0.42, 0.86), Ks=0.3)})
    report('NitrileGloveBox', m)


# ---------------------------------------------------------------- forensic case (aluminium)
def forensic_case():
    S = 1024
    col = np.zeros((S, S, 3), np.float32); hgt = np.zeros((S, S), np.float32)
    mm = np.zeros((S, S), np.float32); sm = np.zeros((S, S), np.float32)
    # left half: brushed aluminium panel (horizontal brushing)
    rng = np.random.default_rng(14)
    streak = rng.random((S, 1)).astype(np.float32)
    streak = np.asarray(Image.fromarray((np.repeat(streak, 512, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur((6, 0.5))), np.float32) / 255
    nz = mk.noise(S, 512, 80, 15, 4)
    alu = 0.66 + 0.06 * (streak - 0.5) + 0.05 * (nz - 0.5)
    col[:, :512] = alu[..., None] * np.array([1.0, 1.0, 1.02])
    hgt[:, :512] = streak * 0.3
    mm[:, :512] = 1; sm[:, :512] = 0.5 + 0.1 * (nz - 0.5)
    # right-bottom: black textured plastic (handle, latches, rubber feet)
    pn = mk.noise(512, 256, 3, 16, 2)
    col[512:, 512:768] = (0.04 + 0.02 * pn)[..., None]; hgt[512:, 512:768] = pn; sm[512:, 512:768] = 0.3
    # chrome corners
    col[512:, 768:] = 0.85; mm[512:, 768:] = 1; sm[512:, 768:] = 0.88
    # extrusion frame (anodised aluminium, brighter)
    col[256:512, 512:] = 0.75 + 0.04 * (mk.noise(256, 512, 30, 17, 3)[..., None] - 0.5); mm[256:512, 512:] = 1; sm[256:512, 512:] = 0.62
    im = img(col)
    # sticker at top-right (rows 0..256, u 0.5..1): "CRIME SCENE KIT"
    st = Image.new('RGB', (512, 256), (245, 245, 240)); d = ImageDraw.Draw(st)
    for i in range(-256, 512, 40):
        d.polygon([(i, 0), (i + 20, 0), (i + 20 + 40, 40), (i + 40, 40)], fill=(240, 190, 20))
        d.polygon([(i, 216), (i + 20, 216), (i + 20 + 40, 256), (i + 40, 256)], fill=(240, 190, 20))
    d.rectangle([0, 0, 512, 40], outline=None)
    d.text((28, 62), 'CRIME SCENE', font=mk.font('sans-bold', 54), fill=(15, 15, 15))
    d.text((28, 128), 'INVESTIGATION KIT', font=mk.font('sans-bold', 34), fill=(15, 15, 15))
    d.text((28, 176), 'Unit 7  -  check seal before use', font=mk.font('sans', 20), fill=(60, 60, 60))
    im.paste(st, (512, 0))
    sm[:256, 512:] = 0.3; mm[:256, 512:] = 0
    mk.save_color(os.path.join(OUT, 'ForensicCase_Color.jpg'), np.asarray(im), 88)
    Image.fromarray(mk.normal_from_height(hgt, 1.5)).resize((512, 512)).save(os.path.join(OUT, 'ForensicCase_Normal.jpg'), quality=90)
    mk.save_ms(os.path.join(OUT, 'ForensicCase_MetallicSmoothness.png'),
               np.asarray(Image.fromarray((mm * 255).astype(np.uint8)).resize((512, 512)), np.float32) / 255,
               np.asarray(Image.fromarray((sm * 255).astype(np.uint8)).resize((512, 512)), np.float32) / 255)
    PANEL = (0.0, 0.0, 0.5, 1.0); FRAME = (0.52, 0.27, 0.98, 0.48); BLACK = (0.52, 0.02, 0.73, 0.48); CHROME = (0.77, 0.02, 0.98, 0.48)
    STICK = (0.5, 0.75, 1.0, 1.0)
    W, Hh, D = 0.46, 0.15, 0.34
    m = mk.Mesh()
    P, UV, N = mk.rounded_box((W - 0.012, Hh - 0.012, D - 0.012), 0.004, 2, uv_rects={k: PANEL for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')})
    m.add('CaseAtlas', P, UV, N)
    # frame extrusions along all 12 edges + the lid/base seam ring
    t = 0.014
    def bar(size, c):
        P, UV, N = mk.box(size, c, uvs={k: FRAME for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')}); m.add('CaseAtlas', P, UV, N)
    for sy in (-1, 1):
        for sz in (-1, 1):
            bar((W - 0.03, t, t), (0, sy * (Hh / 2 - t / 2), sz * (D / 2 - t / 2)))
        for sx in (-1, 1):
            bar((t, t, D - 0.03), (sx * (W / 2 - t / 2), sy * (Hh / 2 - t / 2), 0))
    for sx in (-1, 1):
        for sz in (-1, 1):
            bar((t, Hh - 0.03, t), (sx * (W / 2 - t / 2), 0, sz * (D / 2 - t / 2)))
    seam_y = 0.012
    for sz in (-1, 1):
        bar((W - 0.004, 0.012, 0.006), (0, seam_y, sz * (D / 2 - 0.003)))
    for sx in (-1, 1):
        bar((0.006, 0.012, D - 0.004), (sx * (W / 2 - 0.003), seam_y, 0))
    # chrome corners
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                P, UV, N = mk.rounded_box((0.032, 0.032, 0.032), 0.009, 3, uv_rects={k: CHROME for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')})
                m.add('CaseAtlas', P + [sx * (W / 2 - 0.014), sy * (Hh / 2 - 0.014), sz * (D / 2 - 0.014)], UV, N)
    # latches on the front (+z)
    for sx in (-0.13, 0.13):
        P, UV, N = mk.rounded_box((0.045, 0.034, 0.012), 0.003, 2, uv_rects={k: CHROME for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')})
        m.add('CaseAtlas', P + [sx, seam_y, D / 2 + 0.004], UV, N)
    # handle on the front
    path = [(-0.07, seam_y - 0.005, D / 2 + 0.004)] + [(0.065 * math.cos(a), seam_y - 0.005 - 0.0, D / 2 + 0.012 + 0.03 * math.sin(a)) for a in np.linspace(math.pi, 0, 9)][1:-1] + [(0.07, seam_y - 0.005, D / 2 + 0.004)]
    P, UV, N = mk.tube(path, 0.009, 12, uv=BLACK); m.add('CaseAtlas', P, UV, N)
    for sx in (-0.075, 0.075):
        P, UV, N = mk.rounded_box((0.028, 0.03, 0.012), 0.004, 2, uv_rects={k: CHROME for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')})
        m.add('CaseAtlas', P + [sx, seam_y - 0.005, D / 2 + 0.002], UV, N)
    # sticker on the lid
    P, UV, N = mk.box((0.20, 0.0008, 0.10), (0.05, Hh / 2 - 0.0058 + 0.0004, -0.03), uvs={'py': STICK})
    keep = N[:, 0, 1] > 0.5
    m.add('CaseAtlas', P[keep], UV[keep], N[keep])
    m.ground()
    m.write(os.path.join(OUT, 'ForensicCase.obj'), {'CaseAtlas': dict(map='ForensicCase_Color.jpg', Ks=0.4)})
    report('ForensicCase', m)


# ---------------------------------------------------------------- tactical flashlight + UV torch
def flashlight():
    prof = [(0.0, -0.0025), (0.0065, -0.0025), (0.0075, -0.0005),          # rubber tail switch
            (0.0118, 0.0), (0.0128, 0.002), (0.0128, 0.016), (0.0121, 0.019),  # tail cap
            (0.0121, 0.024), (0.0124, 0.025), (0.0124, 0.088), (0.0121, 0.089),  # knurled body
            (0.0121, 0.096), (0.0135, 0.100), (0.0182, 0.122), (0.0196, 0.126),   # flared head
            (0.0196, 0.150), (0.0186, 0.1545), (0.0166, 0.1545), (0.0160, 0.1530)]
    v = lathe_v(prof)
    S = 512
    def atlas(bezel_rgb, label):
        col = np.ones((S, S, 3), np.float32) * 0.045
        n = mk.noise(S, S, 25, 30, 3)
        col += (n[..., None] - 0.5) * 0.02
        hgt = np.zeros((S, S), np.float32)
        sm = np.full((S, S), 0.55, np.float32); mm = np.full((S, S), 0.85, np.float32)
        def rowr(y0, y1):
            i0 = np.searchsorted([p[1] for p in prof], y0); i1 = np.searchsorted([p[1] for p in prof], y1)
            return int((1 - v[min(i1, len(v) - 1)]) * S), int((1 - v[i0]) * S)
        # knurl diamond pattern on the body
        r0, r1 = rowr(0.025, 0.088)
        yy, xx = np.mgrid[r0:r1, 0:S]
        kn = (np.abs(((xx + yy) % 12) - 6) + np.abs(((xx - yy) % 12) - 6)) / 12
        hgt[r0:r1] = kn; sm[r0:r1] = 0.4
        # rubber switch
        rs0, rs1 = rowr(-0.0025, 0.0)
        col[rs0:rs1] = 0.06; mm[rs0:rs1] = 0; sm[rs0:rs1] = 0.25
        # bezel ring
        b0, b1 = rowr(0.126, 0.1545)
        col[b0:b1] = np.array(bezel_rgb); sm[b0:b1] = 0.7
        im = img(col); d = ImageDraw.Draw(im)
        h0, h1 = rowr(0.100, 0.122)
        d.text((40, (h0 + h1) // 2 - 10), label, font=mk.font('sans-bold', 18), fill=(200, 200, 200))
        return np.asarray(im), hgt, mm, sm
    col, hgt, mm, sm = atlas((0.05, 0.05, 0.055), 'LED  1000 lm')
    mk.save_color(os.path.join(OUT, 'TacticalFlashlight_Color.jpg'), col, 90)
    Image.fromarray(mk.normal_from_height(hgt, 2.5)).save(os.path.join(OUT, 'TacticalFlashlight_Normal.jpg'), quality=92)
    mk.save_ms(os.path.join(OUT, 'TacticalFlashlight_MetallicSmoothness.png'), mm, sm)
    col2, _, mm2, sm2 = atlas((0.32, 0.12, 0.62), 'UV 365 nm')
    mk.save_color(os.path.join(OUT, 'UVTorch_Color.jpg'), col2, 90)
    m = mk.Mesh()
    P, UV, N = mk.lathe(prof, 32, uv=(0, 0, 1, 1), smooth=False)
    m.add('TorchBody', P, UV, mk.smooth_normals(P, 40))
    # reflector cone + lens
    refl = [(0.0160, 0.1530), (0.0060, 0.1405), (0.0035, 0.1400)]
    P, UV, N = mk.lathe(refl, 32, uv=(0, 0.995, 1, 0.995)); m.add('TorchReflector', P, UV, N)
    P, UV, N = mk.disk(0.0035, 0.1400, 16, (0.5, 0.5), 0.0, up=True); m.add('TorchEmitter', P, UV, N)
    P, UV, N = mk.disk(0.0162, 0.1515, 32, (0.5, 0.5), 0.0, up=True); m.add('TorchLens', P, UV, N)
    # pocket clip
    P, UV, N = mk.box((0.0035, 0.045, 0.0012), (0, 0.055, 0.0132), uvs={k: (0.1, 0.5, 0.11, 0.51) for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')})
    m.add('TorchBody', P, UV, N)
    # orient: beam along +Z, pivot at the middle of the grip
    M = mk.chain(mk.Rx(90), mk.T(0, -0.06, 0))
    out = m.transformed(M)
    mats = {'TorchBody': dict(map='TacticalFlashlight_Color.jpg', Ks=0.3), 'TorchReflector': dict(Kd=(0.85, 0.85, 0.86), Ks=0.9, Ns=800),
            'TorchEmitter': dict(Kd=(1.0, 0.97, 0.88)), 'TorchLens': dict(Kd=(0.9, 0.93, 0.95), d=0.25, Ks=0.9, Ns=900)}
    out.write(os.path.join(OUT, 'TacticalFlashlight.obj'), mats)
    report('TacticalFlashlight', out)
    mats2 = dict(mats); mats2['TorchBody'] = dict(map='UVTorch_Color.jpg', Ks=0.3)
    mats2['TorchEmitter'] = dict(Kd=(0.45, 0.2, 1.0)); mats2['TorchLens'] = dict(Kd=(0.35, 0.22, 0.6), d=0.55, Ks=0.9, Ns=900)
    out.write(os.path.join(OUT, 'UVTorch.obj'), mats2)
    report('UVTorch', out)


# ---------------------------------------------------------------- police radio (handheld)
def radio():
    S = 1024
    W, H, D = 0.058, 0.125, 0.034
    col = np.ones((S, S, 3), np.float32) * 0.055
    n = mk.noise(S, S, 12, 40, 3)
    col += (n[..., None] - 0.5) * 0.025
    hgt = (n - 0.5) * 0.15
    sm = np.full((S, S), 0.32, np.float32)
    im = img(col); d = ImageDraw.Draw(im)
    hm = Image.fromarray(((hgt + 0.5) * 255).clip(0, 255).astype(np.uint8)); dh = ImageDraw.Draw(hm)
    # front face: left half of the atlas (512 x 1024 ~ 0.058 x 0.125 at 8.6 px/mm)
    FW = 475
    pxm = FW / W
    def R(x0, y0, x1, y1):  # metres from the top-left of the front face -> pixels
        return [x0 * pxm, y0 * pxm, x1 * pxm, y1 * pxm]
    # speaker grille
    for gy in np.arange(0.008, 0.050, 0.0032):
        for gx in np.arange(0.008, W - 0.007, 0.0032):
            r = 0.0009 * pxm
            cx, cy = gx * pxm, gy * pxm
            d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(5, 5, 6)); dh.ellipse([cx - r, cy - r, cx + r, cy + r], fill=20)
    # display
    d.rounded_rectangle(R(0.008, 0.056, W - 0.008, 0.078), 6, fill=(18, 26, 24)); dh.rounded_rectangle(R(0.008, 0.056, W - 0.008, 0.078), 6, fill=110)
    d.text((0.012 * pxm, 0.059 * pxm), 'CH 03  TG 0471', font=mk.font('mono-bold', 22), fill=(70, 150, 120))
    d.text((0.012 * pxm, 0.068 * pxm), 'OPS-2      ||||', font=mk.font('mono', 20), fill=(50, 110, 90))
    # keypad 3 x 4
    keys = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '*', '0', '#']
    for k, lab in enumerate(keys):
        cx = 0.012 + (k % 3) * 0.017; cy = 0.084 + (k // 3) * 0.0095
        d.rounded_rectangle(R(cx, cy, cx + 0.013, cy + 0.0075), 8, fill=(30, 30, 33)); dh.rounded_rectangle(R(cx, cy, cx + 0.013, cy + 0.0075), 8, fill=190)
        d.text(((cx + 0.005) * pxm, (cy + 0.0012) * pxm), lab, font=mk.font('sans-bold', 24), fill=(190, 190, 190))
    # right half regions: knurl strip (knobs), orange (emergency), rubber (antenna)
    d.rectangle([600, 0, 1024, 200], fill=(14, 14, 15))
    for x in range(600, 1024, 10):
        dh.rectangle([x, 0, x + 4, 200], fill=200)
    d.rectangle([600, 220, 1024, 330], fill=(230, 95, 20))
    d.rectangle([600, 350, 1024, 600], fill=(20, 20, 21))
    im = im.filter(ImageFilter.GaussianBlur(0.4))
    mk.save_color(os.path.join(OUT, 'PoliceRadio_Color.jpg'), np.asarray(im), 90)
    Image.fromarray(mk.normal_from_height(np.asarray(hm.filter(ImageFilter.GaussianBlur(1.2)), np.float32) / 255, 4)).resize((512, 512)).save(
        os.path.join(OUT, 'PoliceRadio_Normal.jpg'), quality=92)
    sm[:, :512] = 0.35; sm2 = sm.copy()
    mk.save_ms(os.path.join(OUT, 'PoliceRadio_MetallicSmoothness.png'), np.zeros((256, 256)), np.asarray(Image.fromarray((sm2 * 255).astype(np.uint8)).resize((256, 256)), np.float32) / 255)
    FRONT = (0, 0, FW / S, 1); PLAIN = (0.52, 0.45, 0.56, 0.5); KNURL = (600 / S, 1 - 200 / S, 1, 1); ORANGE = (0.6, 1 - 330 / S, 1, 1 - 220 / S)
    RUBBER = (0.6, 1 - 600 / S, 1, 1 - 350 / S)
    rects = {k: PLAIN for k in ('px', 'nx', 'py', 'ny', 'nz')}; rects['pz'] = FRONT
    m = mk.Mesh()
    P, UV, N = mk.rounded_box((W, H, D), 0.007, 3, uv_rects=rects)
    m.add('RadioAtlas', P + [0, H / 2, 0], UV, N)
    # antenna (stubby, tapered rubber) at the back-left of the top
    ant = [(0.0, 0.0), (0.0068, 0.0), (0.0068, 0.012), (0.0058, 0.016), (0.0050, 0.070), (0.0042, 0.082), (0.0, 0.085)]
    P, UV, N = mk.lathe(ant, 16, uv=RUBBER); m.add('RadioAtlas', P + [-0.015, H - 0.002, -0.004], UV, N)
    # knobs
    for x, h, r in ((0.010, 0.013, 0.0075), (0.0215, 0.009, 0.0055)):
        knob = [(0.0, 0.0), (r, 0.0), (r, h), (r * 0.85, h + 0.0012), (0.0, h + 0.0012)]
        P, UV, N = mk.lathe(knob, 20, uv=KNURL, smooth=False); m.add('RadioAtlas', P + [x, H - 0.002, 0.002], UV, mk.smooth_normals(P, 40))
    # emergency button
    P, UV, N = mk.rounded_box((0.010, 0.004, 0.008), 0.0015, 2, uv_rects={k: ORANGE for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')})
    m.add('RadioAtlas', P + [0.0, H + 0.001, 0.008], UV, N)
    # push-to-talk on the left side, belt clip on the back
    P, UV, N = mk.rounded_box((0.004, 0.032, 0.014), 0.0015, 2, uv_rects={k: RUBBER for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')})
    m.add('RadioAtlas', P + [-W / 2 - 0.0012, H * 0.62, 0.0], UV, N)
    P, UV, N = mk.rounded_box((0.034, 0.075, 0.004), 0.0015, 2, uv_rects={k: PLAIN for k in ('px', 'nx', 'py', 'ny', 'pz', 'nz')})
    m.add('RadioAtlas', P + [0, H * 0.55, -D / 2 - 0.003], UV, N)
    m.ground()
    m.write(os.path.join(OUT, 'PoliceRadio.obj'), {'RadioAtlas': dict(map='PoliceRadio_Color.jpg', Ks=0.2)})
    report('PoliceRadio', m)


if __name__ == '__main__':
    brush(); powder_jar(); glove_box(); forensic_case(); flashlight(); radio()
