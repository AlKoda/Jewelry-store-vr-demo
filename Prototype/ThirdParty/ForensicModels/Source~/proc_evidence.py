"""Evidence items: EvidenceMarkerTent (+ number atlas), ForensicScaleL, EvidenceBagPaper, EvidenceBagPlastic,
ForensicPapers atlas with ScatteredPapers and PaperSheetForm. All project-made (CC0 dedication by the project)."""
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
import meshkit as mk

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(2026)


def paper_grain(h, w, base, seed, fibre=0.03, blotch=0.04):
    n1 = mk.noise(h, w, 40, seed, 5) - 0.5
    n2 = mk.noise(h, w, 4, seed + 1, 2) - 0.5
    img = np.ones((h, w, 3), np.float32) * np.array(base, np.float32)
    img += (n1 * blotch + n2 * fibre)[..., None]
    return img


def to_img(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))


# ------------------------------------------------------------------ evidence marker tent (A-frame)
def marker_atlas():
    S = 1024; C = 256
    yellow = np.array([0.98, 0.78, 0.05])
    base = paper_grain(S, S, yellow, 5, fibre=0.015, blotch=0.03)
    im = to_img(base).convert('RGB')
    d = ImageDraw.Draw(im)
    fnum = mk.font('sans-bold', 92); fsmall = mk.font('sans-bold', 9)
    for k in range(16):
        col, row = k % 4, k // 4
        x0, y0 = col * C, row * C
        for half in (0, 1):  # two outer faces, each 256 x 124 at y0+2 and y0+130
            fy = y0 + 2 + half * 128
            d.rectangle([x0 + 5, fy + 4, x0 + C - 6, fy + 119], outline=(20, 20, 20), width=3)
            txt = str(k + 1)
            bb = d.textbbox((0, 0), txt, font=fnum)
            tw, th = bb[2] - bb[0], bb[3] - bb[1]
            d.text((x0 + (C - tw) / 2 - bb[0], fy + 12 - bb[1] + (78 - th) / 2), txt, font=fnum, fill=(15, 15, 15))
            # 10 cm scale along the bottom edge of the face (face width = 10 cm = 246 px between the borders)
            sx0, sx1, by = x0 + 5, x0 + C - 6, fy + 118
            for mm in range(0, 101):
                x = sx0 + (sx1 - sx0) * mm / 100
                L = 14 if mm % 10 == 0 else (9 if mm % 5 == 0 else 5)
                d.line([(x, by - L), (x, by)], fill=(20, 20, 20), width=1)
            for cm in range(1, 10):
                x = sx0 + (sx1 - sx0) * cm / 10
                d.text((x - 3, by - 25), str(cm), font=fsmall, fill=(20, 20, 20))
    im = im.filter(ImageFilter.GaussianBlur(0.35))
    mk.save_color(os.path.join(OUT, 'EvidenceMarkerTent_Atlas.png'), np.asarray(im))


def marker_mesh():
    W, D, Ls, t = 0.1029, 0.024, 0.0506, 0.0018  # face art 252 x 124 px; ruler = exactly 10 cm
    H = math.sqrt(Ls ** 2 - D ** 2)
    tilt = math.degrees(math.atan2(D, H))
    m = mk.Mesh()
    cell_u0, cell_v0 = 0.0, 0.75  # number 1 cell (top-left); other numbers via texture offset (col*0.25, -row*0.25)
    plain = (cell_u0 + 0.125, cell_v0 + 0.125)  # plain yellow strip between the two faces
    for side, (v_lo, v_hi) in ((1, (cell_v0 + 0.125 + 2 / 1024, cell_v0 + 0.25 - 4 / 1024)),
                               (-1, (cell_v0 + 4 / 1024, cell_v0 + 0.125 - 2 / 1024))):
        P, UV, N = mk.box((W, Ls, t))
        # outer face (+z before rotation) gets the number art, everything else the plain strip
        UV = UV.copy()
        for f in range(len(P)):
            if N[f, 0, 2] > 0.5:
                u = (P[f, :, 0] + W / 2) / W; v = (P[f, :, 1] + Ls / 2) / Ls
                UV[f, :, 0] = cell_u0 + (2 + u * 252) / 1024
                UV[f, :, 1] = v_lo + (v_hi - v_lo) * v
            else:
                UV[f] = plain
        M = mk.chain(mk.Ry(0 if side == 1 else 180), mk.T(0, H / 2, D / 2), mk.Rx(-tilt), mk.T(0, 0, t / 2))
        sub = mk.Mesh(); sub.add('MarkerPlastic', P, UV, N)
        m.merge(sub, M)
    # hinge
    P, UV, N = mk.lathe([(0.0, -W / 2), (0.0032, -W / 2), (0.0032, W / 2), (0.0, W / 2)], 12)
    UV[:] = plain
    sub = mk.Mesh(); sub.add('MarkerPlastic', P, UV, N)
    m.merge(sub, mk.chain(mk.T(0, H + 0.0005, 0), mk.Rz(90)))
    m.ground()
    t_ = m.write(os.path.join(OUT, 'EvidenceMarkerTent.obj'),
                 {'MarkerPlastic': dict(Kd=(1, 1, 1), map='EvidenceMarkerTent_Atlas.png', Ks=0.3)})
    lo, hi = m.bounds(); print('EvidenceMarkerTent', t_, np.round(hi - lo, 3))


# ------------------------------------------------------------------ L-shaped photographic scale
def scale_l():
    S = 1024; span = 0.15  # texture covers a 15 x 15 cm square
    px = S / span
    img = paper_grain(S, S, (0.95, 0.95, 0.93), 9, fibre=0.01, blotch=0.015)
    im = to_img(img); d = ImageDraw.Draw(im)
    f = mk.font('sans-bold', 22); fs = mk.font('sans', 15)
    A = 0.032  # arm width
    # horizontal arm along the bottom (z from 0 to A), vertical arm along the left (x from 0 to A)
    def X(m_): return m_ * px
    def Y(m_): return S - m_ * px   # z (metres) -> image row
    # outer edge graduations
    for mm in range(0, 151):
        x = X(mm / 1000); L = 34 if mm % 10 == 0 else (24 if mm % 5 == 0 else 14)
        d.line([(x, Y(0)), (x, Y(0) - L)], fill=(10, 10, 10), width=2)
        if mm % 10 == 0 and 0 < mm < 150:
            d.text((x - 6 if mm < 100 else x - 12, Y(0) - 64), str(mm // 10), font=f, fill=(10, 10, 10))
    for mm in range(0, 151):
        y = Y(mm / 1000); L = 34 if mm % 10 == 0 else (24 if mm % 5 == 0 else 14)
        d.line([(0, y), (L, y)], fill=(10, 10, 10), width=2)
        if mm % 10 == 0 and 0 < mm < 150:
            d.text((40, y - 12), str(mm // 10), font=f, fill=(10, 10, 10))
    # 18% grey and black/white reference bars on the horizontal arm, circles for perspective control
    d.rectangle([X(0.055), Y(A - 0.004), X(0.085), Y(0.016)], fill=(119, 119, 119))
    d.rectangle([X(0.088), Y(A - 0.004), X(0.103), Y(0.016)], fill=(12, 12, 12))
    for cx, cz in ((0.017, 0.017), (0.135, 0.022), (0.017, 0.135)):
        r = 0.0045 * px
        d.ellipse([X(cx) - r, Y(cz) - r, X(cx) + r, Y(cz) + r], fill=(10, 10, 10))
    d.text((X(0.106), Y(0.03)), 'cm', font=f, fill=(10, 10, 10))
    d.text((X(0.040), Y(0.03) + 1), 'PHOTO SCALE', font=fs, fill=(30, 30, 30))
    im = im.filter(ImageFilter.GaussianBlur(0.4))
    mk.save_color(os.path.join(OUT, 'ForensicScaleL_Color.jpg'), np.asarray(im), 92)
    poly = [(0, 0), (span, 0), (span, A), (A, A), (A, 0.10), (0, 0.10)]
    # x right, z "up the image": convert to world x / -z so the scale reads correctly from above
    polyw = [(x - span / 2, -(z - 0.05)) for x, z in poly]
    top, bot, side = mk.extrude(polyw, 0.0016, uv_top=lambda x, z: ((x + span / 2) / span, (-z + 0.05) / span))
    m = mk.Mesh()
    m.add('ScalePlastic', *top)
    bP, bUV, bN = bot; bUV = np.full_like(bUV, 0.5)  # plain back
    m.add('ScalePlastic', bP, bUV, bN)
    sP, sUV, sN = side; sUV = np.full_like(sUV, 0.5)
    m.add('ScalePlastic', sP, sUV, sN)
    m.ground(center_xz=False)
    t_ = m.write(os.path.join(OUT, 'ForensicScaleL.obj'), {'ScalePlastic': dict(Kd=(1, 1, 1), map='ForensicScaleL_Color.jpg', Ks=0.1)})
    lo, hi = m.bounds(); print('ForensicScaleL', t_, np.round(hi - lo, 4))


# ------------------------------------------------------------------ evidence bags
def form_lines(d, x0, y0, w, rows, f, line_h, ink=(25, 25, 25)):
    y = y0
    for label in rows:
        d.text((x0, y), label, font=f, fill=ink)
        bb = d.textbbox((x0, y), label, font=f)
        d.line([(bb[2] + 8, y + line_h - 10), (x0 + w, y + line_h - 10)], fill=ink, width=2)
        y += line_h
    return y


def hand(d, xy, text, size=30, ink=(30, 45, 120)):
    d.text(xy, text, font=mk.font('/usr/share/fonts/truetype/dejavu/DejaVuSerif-Italic.ttf' if os.path.exists('/usr/share/fonts/truetype/dejavu/DejaVuSerif-Italic.ttf') else 'serif', size), fill=ink)


def bag_paper():
    S = 1024
    kraft = paper_grain(S, S, (0.62, 0.47, 0.30), 21, fibre=0.05, blotch=0.06)
    im = to_img(kraft); d = ImageDraw.Draw(im)
    ink = (28, 22, 18)
    fH = mk.font('sans-bold', 70); fL = mk.font('sans-bold', 25); fS = mk.font('sans', 21)
    # front of the bag occupies u 0..0.62 (x) and v 0.18..1 (z), flap strip at the bottom of the texture
    X0, X1, Y0 = 30, 610, 40
    d.rectangle([X0, Y0, X1, Y0 + 92], fill=ink)
    d.text((X0 + 22, Y0 + 8), 'EVIDENCE', font=fH, fill=(205, 170, 120))
    y = form_lines(d, X0, Y0 + 120, X1 - X0, ['AGENCY', 'CASE No.', 'ITEM No.', 'DATE / TIME COLLECTED', 'COLLECTED BY',
                                             'LOCATION'], fL, 56, ink)
    d.text((X0, y + 6), 'DESCRIPTION OF EVIDENCE', font=fL, fill=ink)
    d.rectangle([X0, y + 40, X1, y + 170], outline=ink, width=3)
    y += 190
    d.text((X0, y), 'CHAIN OF CUSTODY', font=fL, fill=ink)
    y += 36
    cols = [X0, X0 + 180, X0 + 360, X0 + 470, X1]
    for r in range(5):
        d.line([(X0, y + r * 40), (X1, y + r * 40)], fill=ink, width=2)
    for c in cols:
        d.line([(c, y), (c, y + 160)], fill=ink, width=2)
    for c, lab in zip(cols, ['FROM', 'TO', 'DATE', 'TIME']):
        d.text((c + 8, y + 8), lab, font=fS, fill=ink)
    # handwritten entries
    hand(d, (X0 + 120, Y0 + 116), 'City Police - CSI')
    hand(d, (X0 + 150, Y0 + 172), '26/10/0471')
    hand(d, (X0 + 150, Y0 + 228), '7')
    hand(d, (X0 + 330, Y0 + 284), '14/10  02:47')
    hand(d, (X0 + 220, Y0 + 340), 'DC R. Hale  4471')
    hand(d, (X0 + 150, Y0 + 396), 'Display case 3, floor')
    hand(d, (X0 + 14, Y0 + 520), 'Glass fragments with possible')
    hand(d, (X0 + 14, Y0 + 556), 'glove mark, from case 3 bed')
    # red sealing tape strip on the flap region (v 0..0.12)
    tape = np.ones((120, S, 3), np.float32) * np.array([0.72, 0.08, 0.07])
    tape += (mk.noise(120, S, 6, 4, 2)[..., None] - 0.5) * 0.06
    timg = to_img(tape); td = ImageDraw.Draw(timg)
    ft = mk.font('sans-bold', 30)
    for i in range(0, S, 330):
        td.text((i + 10, 40), 'EVIDENCE  SEALED', font=ft, fill=(245, 240, 235))
    im.paste(timg, (0, S - 120))
    # kraft of the flap above the tape
    mk.save_color(os.path.join(OUT, 'EvidenceBagPaper_Color.jpg'), np.asarray(im.filter(ImageFilter.GaussianBlur(0.4))), 88)
    # normal: paper crinkle
    h = mk.noise(512, 512, 60, 31, 5) * 0.7 + mk.noise(512, 512, 8, 32, 2) * 0.3
    Image.fromarray(mk.normal_from_height(h, 2.5)).save(os.path.join(OUT, 'EvidenceBagPaper_Normal.jpg'), quality=90)

    W, L, thick = 0.20, 0.33, 0.022
    nx, nz = 18, 26
    xs = np.linspace(-W / 2, W / 2, nx); zs = np.linspace(-L / 2, L / 2, nz)
    X, Z = np.meshgrid(xs, zs)
    ex = np.clip(1 - (2 * X / W) ** 6, 0, 1) ** 0.5; ez = np.clip(1 - (2 * Z / L) ** 8, 0, 1) ** 0.5
    wr = (mk.noise(nz, nx, 6, 77, 2) - 0.5) * 0.004
    Y = thick * ex * ez * (0.85 + 0.15 * np.cos(Z / L * math.pi)) + wr * ex * ez
    Pg = np.stack([X, Y, Z], -1)
    U = (X + W / 2) / W * 0.62; V = 0.12 + (1 - (Z + L / 2) / L) * 0.88
    # front faces up; image rows go top->bottom so v must increase towards -z (far end = top of print)
    V = 0.12 + ((L / 2 - Z) / L) * 0.88
    P, UV, _ = mk.grid_surface(Pg, np.stack([U, V], -1))
    fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
    if np.mean(fn[:, 1]) < 0:
        P = P[:, ::-1]; UV = UV[:, ::-1]
    m = mk.Mesh(); m.add('BagKraft', P, UV, mk.smooth_normals(P, 70))
    Pb = Pg.copy(); Pb[..., 1] = 0.0004 * ex * ez
    Pb2, UVb, _ = mk.grid_surface(Pb, np.stack([U * 0 + 0.85, V * 0 + 0.5], -1))
    fn = np.cross(Pb2[:, 1] - Pb2[:, 0], Pb2[:, 2] - Pb2[:, 0])
    if np.mean(fn[:, 1]) > 0:
        Pb2 = Pb2[:, ::-1]; UVb = UVb[:, ::-1]
    m.add('BagKraft', Pb2, UVb)
    # folded and taped top flap lying on the far end of the bag
    fy = float(Y.max())
    P, UV, N = mk.box((W * 0.98, 0.0015, 0.05))
    UV = UV.copy()
    for f in range(len(P)):
        if N[f, 0, 1] > 0.5:
            u = (P[f, :, 0] + W / 2) / W; v = (0.025 - P[f, :, 2]) / 0.05
            UV[f, :, 0] = u * 0.95; UV[f, :, 1] = 0.01 + 0.1 * v
        else:
            UV[f] = (0.85, 0.5)
    sub = mk.Mesh(); sub.add('BagKraft', P, UV, N)
    m.merge(sub, mk.T(0, fy * 0.55 + 0.001, -L / 2 + 0.028))
    m.ground()
    t_ = m.write(os.path.join(OUT, 'EvidenceBagPaper.obj'), {'BagKraft': dict(Kd=(1, 1, 1), map='EvidenceBagPaper_Color.jpg')})
    lo, hi = m.bounds(); print('EvidenceBagPaper', t_, np.round(hi - lo, 3))


def bag_plastic():
    S = 1024
    rgba = np.zeros((S, S, 4), np.float32)
    rgba[..., :3] = 0.92; rgba[..., 3] = 0.16
    # subtle wrinkle highlights in the polythene
    wr = mk.noise(S, S, 90, 41, 4)
    rgba[..., 3] += np.clip((wr - 0.55) * 0.6, 0, 0.2)
    im = Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), 'RGBA')
    d = ImageDraw.Draw(im)
    red = (190, 25, 25, 255)
    # zip seal + red tamper strip at the top (v 0.9..1 -> rows 0..100)
    d.rectangle([0, 0, S, 70], fill=(200, 30, 28, 245))
    fT = mk.font('sans-bold', 28)
    for i in range(0, S, 340):
        d.text((i + 18, 20), 'TAMPER EVIDENT', font=fT, fill=(255, 255, 255, 255))
    d.rectangle([0, 84, S, 92], fill=(225, 225, 225, 150))
    # white write-on panel
    X0, Y0, X1, Y1 = 90, 150, 934, 760
    d.rectangle([X0, Y0, X1, Y1], fill=(246, 246, 244, 255))
    fH = mk.font('sans-bold', 64); fL = mk.font('sans-bold', 24)
    d.text((X0 + 26, Y0 + 18), 'EVIDENCE', font=fH, fill=red)
    d.line([(X0 + 20, Y0 + 100), (X1 - 20, Y0 + 100)], fill=red, width=4)
    y = Y0 + 122
    for lab in ['CASE No.', 'ITEM No.', 'DATE', 'TIME', 'COLLECTED BY', 'DESCRIPTION']:
        d.text((X0 + 26, y), lab, font=fL, fill=(40, 40, 40, 255))
        bb = d.textbbox((X0 + 26, y), lab, font=fL)
        d.line([(bb[2] + 10, y + 30), (X1 - 26, y + 30)], fill=(60, 60, 60, 255), width=2)
        y += 66
    hand(d, (X0 + 200, Y0 + 116), '26/10/0471', 32, (20, 20, 20))
    hand(d, (X0 + 200, Y0 + 182), '12', 32, (20, 20, 20))
    hand(d, (X0 + 130, Y0 + 248), '14/10/2026', 32, (20, 20, 20))
    hand(d, (X0 + 130, Y0 + 314), '03:05', 32, (20, 20, 20))
    hand(d, (X0 + 270, Y0 + 380), 'DC R. Hale', 32, (20, 20, 20))
    hand(d, (X0 + 260, Y0 + 446), 'Torn glove fragment', 32, (20, 20, 20))
    mk.save_color(os.path.join(OUT, 'EvidenceBagPlastic_Color.png'), np.asarray(im.filter(ImageFilter.GaussianBlur(0.4))))
    W, L, thick = 0.23, 0.30, 0.007
    nx, nz = 14, 18
    xs = np.linspace(-W / 2, W / 2, nx); zs = np.linspace(-L / 2, L / 2, nz)
    X, Z = np.meshgrid(xs, zs)
    ex = np.clip(1 - (2 * X / W) ** 8, 0, 1) ** 0.5; ez = np.clip(1 - (2 * Z / L) ** 8, 0, 1) ** 0.5
    wr = (mk.noise(nz, nx, 5, 88, 2) - 0.5) * 0.003
    Y = thick * ex * ez + wr * ex * ez + 0.0005
    U = (X + W / 2) / W; V = (L / 2 - Z) / L
    P, UV, _ = mk.grid_surface(np.stack([X, Y, Z], -1), np.stack([U, V], -1))
    fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
    if np.mean(fn[:, 1]) < 0:
        P = P[:, ::-1]; UV = UV[:, ::-1]
    m = mk.Mesh(); m.add('BagPolythene', P, UV, mk.smooth_normals(P, 70))
    Pb = np.stack([X, np.full_like(X, 0.0003), Z], -1)
    P2, UV2, _ = mk.grid_surface(Pb, np.stack([1 - U, V], -1))
    fn = np.cross(P2[:, 1] - P2[:, 0], P2[:, 2] - P2[:, 0])
    if np.mean(fn[:, 1]) > 0:
        P2 = P2[:, ::-1]; UV2 = UV2[:, ::-1]
    m.add('BagPolythene', P2, UV2)
    m.ground()
    t_ = m.write(os.path.join(OUT, 'EvidenceBagPlastic.obj'),
                 {'BagPolythene': dict(Kd=(1, 1, 1), d=1.0, map='EvidenceBagPlastic_Color.png', Ks=0.5, Ns=300)})
    lo, hi = m.bounds(); print('EvidenceBagPlastic', t_, np.round(hi - lo, 3))


# ------------------------------------------------------------------ paper documents atlas + scatter
def doc_header(d, x, y, w, title, sub):
    d.text((x, y), 'AUREL & FINCH', font=mk.font('serif-bold', 40), fill=(30, 30, 34))
    d.text((x, y + 48), 'Fine Jewellers  -  12 Kingsgate Arcade', font=mk.font('serif', 18), fill=(60, 60, 64))
    d.line([(x, y + 80), (x + w, y + 80)], fill=(150, 120, 60), width=3)
    d.text((x, y + 96), title, font=mk.font('sans-bold', 28), fill=(20, 20, 24))
    d.text((x, y + 132), sub, font=mk.font('sans', 17), fill=(70, 70, 74))


def doc_inventory(W, H):
    im = to_img(paper_grain(H, W, (0.95, 0.95, 0.93), 61, 0.012, 0.012)); d = ImageDraw.Draw(im)
    doc_header(d, 52, 50, W - 104, 'STOCK INVENTORY - DISPLAY CASE 3', 'Counted 13/10/2026 18:05   Checked by: M. Finch')
    f = mk.font('mono', 16); fb = mk.font('mono-bold', 16)
    y = 225
    d.text((52, y), 'CODE     DESCRIPTION                      QTY   PRICE', font=fb, fill=(20, 20, 20)); y += 30
    items = [('R-1182', '18ct yellow gold solitaire 0.50ct', 1, '3,950'), ('R-1190', 'Platinum band, brushed 4mm', 2, '1,240'),
             ('R-1204', 'Sapphire & diamond cluster ring', 1, '2,780'), ('N-0711', '9ct gold curb chain 50cm', 3, '640'),
             ('N-0716', 'Pearl strand, 7mm akoya', 1, '1,150'), ('E-0420', 'Diamond studs 0.30ct pair', 2, '1,890'),
             ('E-0433', 'Emerald drop earrings', 1, '2,240'), ('B-0305', 'Tennis bracelet 2.00ct', 1, '5,600'),
             ('W-0098', 'Ladies dress watch, steel', 1, '1,480'), ('R-1211', 'Rose gold eternity ring', 1, '1,720'),
             ('P-0140', 'Opal pendant on chain', 1, '880'), ('R-1215', 'Signet ring, 9ct, oval', 2, '520')]
    for code, desc, q, p in items:
        d.text((52, y), '%-8s %-32s %3d %8s' % (code, desc, q, p), font=f, fill=(35, 35, 38)); y += 27
        d.line([(52, y - 4), (W - 52, y - 4)], fill=(205, 205, 200), width=1)
    y += 16
    d.text((52, y), 'TOTAL ITEMS 17        TOTAL RETAIL  28,980', font=fb, fill=(20, 20, 20)); y += 50
    hand(d, (60, y), 'Case 3 locked 18:20  - M.F.', 26)
    for i in range(6):
        d.text((W - 160, H - 60), 'Page 1 of 1', font=mk.font('sans', 15), fill=(90, 90, 90))
    return im


def doc_certificate(W, H):
    im = to_img(paper_grain(H, W, (0.96, 0.94, 0.88), 62, 0.012, 0.02)); d = ImageDraw.Draw(im)
    d.rectangle([24, 24, W - 24, H - 24], outline=(150, 120, 60), width=4)
    d.rectangle([34, 34, W - 34, H - 34], outline=(150, 120, 60), width=1)
    doc_header(d, 64, 60, W - 128, 'VALUATION CERTIFICATE', 'For insurance purposes  -  No. AF-26-0388')
    f = mk.font('serif', 19); y = 240
    for line in ['Description: One ladies ring, 18ct white gold,', 'claw set round brilliant cut diamond of',
                 'approximately 1.02ct, colour G, clarity VS2,', 'with tapered baguette shoulders (0.24ct).', '',
                 'Finger size: M 1/2      Weight: 4.1 g', '', 'Replacement value:  GBP 7,400.00', '',
                 'This valuation reflects the retail replacement', 'cost at the date shown and is not an offer', 'to purchase.']:
        d.text((64, y), line, font=f, fill=(40, 36, 30)); y += 31
    hand(d, (64, H - 210), 'A. Aurel  FGA DGA', 30)
    d.line([(64, H - 170), (380, H - 170)], fill=(60, 60, 60), width=2)
    d.text((64, H - 160), 'Valuer', font=mk.font('sans', 15), fill=(80, 80, 80))
    d.ellipse([W - 230, H - 250, W - 90, H - 110], outline=(160, 40, 40), width=4)
    d.text((W - 205, H - 192), 'AUREL', font=mk.font('serif-bold', 26), fill=(160, 40, 40))
    return im


def doc_scene_log(W, H):
    im = to_img(paper_grain(H, W, (0.97, 0.97, 0.96), 63, 0.01, 0.01)); d = ImageDraw.Draw(im)
    fb = mk.font('sans-bold', 30); f = mk.font('sans', 17); fs = mk.font('sans-bold', 16)
    d.rectangle([40, 40, W - 40, 100], fill=(25, 35, 70))
    d.text((56, 52), 'POLICE  -  SCENE ATTENDANCE LOG', font=fb, fill=(255, 255, 255))
    y = 120
    for lab in ['Incident ref.', 'Location', 'Scene opened', 'Log keeper']:
        d.text((44, y), lab, font=fs, fill=(30, 30, 30))
        d.line([(190, y + 20), (W - 44, y + 20)], fill=(90, 90, 90), width=1)
        y += 36
    hand(d, (200, 114), '26/10/0471   burglary (commercial)', 24)
    hand(d, (200, 150), 'Aurel & Finch, 12 Kingsgate Arcade', 24)
    hand(d, (200, 186), '14/10/2026  01:58', 24)
    hand(d, (200, 222), 'PC J. Osei  2290', 24)
    cols = [44, 250, 400, 500, 600, W - 44]
    y0 = y + 20
    for r in range(16):
        d.line([(44, y0 + r * 44), (W - 44, y0 + r * 44)], fill=(60, 60, 60), width=2 if r < 2 else 1)
    for c in cols:
        d.line([(c, y0), (c, y0 + 15 * 44)], fill=(60, 60, 60), width=1)
    for c, lab in zip(cols, ['NAME / NUMBER', 'ROLE', 'IN', 'OUT', 'SIGNATURE']):
        d.text((c + 6, y0 + 12), lab, font=fs, fill=(30, 30, 30))
    entries = [('PC J. Osei 2290', 'scene guard', '01:58', ''), ('PC L. Grant 3115', 'first resp.', '01:52', '02:20'),
               ('DC R. Hale 4471', 'CSI', '02:31', ''), ('DS M. Brandt', 'OIC', '02:40', '03:15'),
               ('A. Aurel', 'keyholder', '02:55', '03:05')]
    for i, (n, r, ti, to) in enumerate(entries):
        yy = y0 + (i + 1) * 44 + 8
        hand(d, (cols[0] + 6, yy), n, 21); hand(d, (cols[1] + 6, yy), r, 21)
        hand(d, (cols[2] + 6, yy), ti, 21)
        if to:
            hand(d, (cols[3] + 6, yy), to, 21)
        hand(d, (cols[4] + 10, yy), '~' + n.split()[1][:4].lower() + '~', 21)
    d.text((44, H - 70), 'Every person entering the cordon must be logged. Protective clothing worn: Y / N', font=f, fill=(50, 50, 50))
    return im


def doc_delivery(W, H):
    im = to_img(paper_grain(H, W, (0.93, 0.95, 0.97), 64, 0.012, 0.012)); d = ImageDraw.Draw(im)
    doc_header(d, 52, 50, W - 104, 'REPAIR & COLLECTION DOCKET', 'Workshop copy  -  keep with item')
    f = mk.font('sans', 19); y = 230
    for lab, val in [('Docket', '4417'), ('Customer', 'Mrs P. Lennox'), ('Item', 'Gold bangle, hinge repair'), ('Received', '06/10/2026'),
                     ('Ready', '15/10/2026'), ('Charge', '85.00 paid')]:
        d.text((52, y), lab, font=mk.font('sans-bold', 19), fill=(30, 30, 30))
        hand(d, (230, y - 6), val, 27)
        d.line([(220, y + 26), (W - 52, y + 26)], fill=(120, 120, 120), width=1)
        y += 52
    d.rectangle([52, y + 20, W - 52, y + 260], outline=(90, 90, 90), width=2)
    d.text((62, y + 30), 'Workshop notes', font=mk.font('sans-bold', 17), fill=(40, 40, 40))
    hand(d, (70, y + 70), 'Replace hinge pin, polish,', 25); hand(d, (70, y + 108), 'check safety catch.', 25)
    d.text((52, H - 80), 'Items not collected within 90 days may be sold to cover costs.', font=mk.font('sans', 14), fill=(80, 80, 80))
    return im


def receipt(W, H, seed):
    im = to_img(paper_grain(H, W, (0.97, 0.97, 0.95), seed, 0.008, 0.01)); d = ImageDraw.Draw(im)
    f = mk.font('mono', 15); fb = mk.font('mono-bold', 17)
    y = 26
    for line in ['   AUREL & FINCH', ' 12 Kingsgate Arcade', '', ' 13/10/26  17:42  T2', ' -------------------',
                 ' R-1222 RING    410.00', ' GIFT BOX         0.00', ' -------------------', ' TOTAL         410.00',
                 ' CARD          410.00', '', '  THANK YOU', '', ' Ref 0093-1187']:
        d.text((10, y), line, font=fb if 'TOTAL' in line or 'AUREL' in line else f, fill=(60, 60, 64)); y += 24
    return im


def papers():
    S = 2048
    atlas = Image.new('RGB', (S, S), (240, 240, 236))
    cellw, cellh, dw = 1024, 1024, 724
    docs = [doc_inventory, doc_certificate, doc_scene_log, doc_delivery]
    rects = {}
    for k, fn in enumerate(docs):
        cx, cy = (k % 2) * cellw, (k // 2) * cellh
        atlas.paste(fn(dw, cellh), (cx, cy))
        rects[fn.__name__] = (cx, cy, cx + dw, cy + cellh)
    # receipts + blank paper in the 300 px strips
    atlas.paste(receipt(296, 760, 70), (728, 4)); rects['receipt1'] = (728, 4, 1024, 764)
    atlas.paste(receipt(296, 760, 71).transpose(Image.FLIP_LEFT_RIGHT).transpose(Image.FLIP_LEFT_RIGHT), (1752, 4)); rects['receipt2'] = (1752, 4, 2048, 764)
    blank = to_img(paper_grain(250, 290, (0.95, 0.95, 0.93), 72, 0.012, 0.012))
    atlas.paste(blank, (730, 780)); rects['blank'] = (730, 780, 1020, 1030)
    a = np.asarray(atlas.filter(ImageFilter.GaussianBlur(0.45)))
    mk.save_color(os.path.join(OUT, 'ForensicPapers_Color.jpg'), a, 86)

    def uvrect(name):
        x0, y0, x1, y1 = rects[name]
        return x0 / S, 1 - y1 / S, x1 / S, 1 - y0 / S

    def sheet(w, l, rect, curl, seed, nx=7, nz=9, back='blank'):
        r = np.random.default_rng(seed)
        xs = np.linspace(-w / 2, w / 2, nx); zs = np.linspace(-l / 2, l / 2, nz)
        X, Z = np.meshgrid(xs, zs)
        # gentle curl: corners/edges lifted, plus a soft crease
        cx, cz = r.uniform(-1, 1, 2)
        Y = curl * (np.clip((X / (w / 2)) * cx, 0, None) ** 2 + np.clip((Z / (l / 2)) * cz, 0, None) ** 2) / 2
        Y += r.uniform(0, 0.004) * np.abs(np.sin((X / w + r.uniform()) * math.pi)) * (r.random() < 0.5)
        Y += 0.0004
        u0, v0, u1, v1 = uvrect(rect)
        U = u0 + (u1 - u0) * (X + w / 2) / w; V = v0 + (v1 - v0) * (l / 2 - Z) / l
        P, UV, _ = mk.grid_surface(np.stack([X, Y, Z], -1), np.stack([U, V], -1))
        fn = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
        if np.mean(fn[:, 1]) < 0:
            P = P[:, ::-1]; UV = UV[:, ::-1]
        out = mk.Mesh(); out.add('Paper', P, UV, mk.smooth_normals(P, 80))
        b0, c0, b1, c1 = uvrect(back)
        Ub = b0 + (b1 - b0) * (X + w / 2) / w; Vb = c0 + (c1 - c0) * (Z + l / 2) / l
        Pb, UVb, _ = mk.grid_surface(np.stack([X, Y - 0.0002, Z], -1), np.stack([Ub, Vb], -1))
        fn = np.cross(Pb[:, 1] - Pb[:, 0], Pb[:, 2] - Pb[:, 0])
        if np.mean(fn[:, 1]) > 0:
            Pb = Pb[:, ::-1]; UVb = UVb[:, ::-1]
        out.add('Paper', Pb, UVb, mk.smooth_normals(Pb, 80))
        return out

    A4 = (0.210, 0.297)
    m = mk.Mesh()
    layout = [('doc_inventory', 0.00, 0.00, 12, A4), ('doc_certificate', 0.24, 0.10, -28, A4), ('doc_delivery', -0.22, 0.14, 40, A4),
              ('doc_inventory', 0.10, -0.26, 95, A4), ('doc_certificate', -0.30, -0.20, -65, A4), ('doc_delivery', 0.42, -0.18, 150, A4),
              ('receipt1', -0.05, 0.36, 70, (0.08, 0.205)), ('receipt2', 0.30, 0.34, -20, (0.08, 0.205))]
    for i, (rect, x, z, rot, (w, l)) in enumerate(layout):
        s = sheet(w, l, rect, rng.uniform(0.008, 0.03), 100 + i)
        m.merge(s, mk.chain(mk.T(x, 0.0012 * i, z), mk.Ry(rot)))
    m.ground()
    t_ = m.write(os.path.join(OUT, 'ScatteredPapers.obj'), {'Paper': dict(Kd=(1, 1, 1), map='ForensicPapers_Color.jpg')})
    lo, hi = m.bounds(); print('ScatteredPapers', t_, np.round(hi - lo, 3))
    f = sheet(0.210, 0.297, 'doc_scene_log', 0.0, 5, 2, 2)
    f.ground()
    t_ = f.write(os.path.join(OUT, 'PaperSheetForm.obj'), {'Paper': dict(Kd=(1, 1, 1), map='ForensicPapers_Color.jpg')})
    lo, hi = f.bounds(); print('PaperSheetForm', t_, np.round(hi - lo, 4))


if __name__ == '__main__':
    marker_atlas(); marker_mesh(); scale_l(); bag_paper(); bag_plastic(); papers()
