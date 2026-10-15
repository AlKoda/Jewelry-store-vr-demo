"""Project-made tileable PBR textures and the sign atlas for the procedural street props.
usage: proc_tex.py OUTDIR FONT.ttf"""
import sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

out, font_path = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
rng = np.random.default_rng(2026)


def tile_noise(n, scale, seed, octaves=4):
    """Tileable value noise via FFT-filtered white noise."""
    r = np.random.default_rng(seed)
    acc = np.zeros((n, n))
    amp = 1.0
    for o in range(octaves):
        w = r.normal(size=(n, n))
        fy = np.fft.fftfreq(n)[:, None]; fx = np.fft.fftfreq(n)[None, :]
        f = np.sqrt(fx ** 2 + fy ** 2)
        s = scale * (2 ** o)
        filt = np.exp(-(f * n / s) ** 2)
        layer = np.real(np.fft.ifft2(np.fft.fft2(w) * filt))
        layer /= layer.std() + 1e-9
        acc += amp * layer
        amp *= 0.5
    acc = (acc - acc.min()) / (acc.max() - acc.min())
    return acc


def normal_from_height(h, strength):
    gy, gx = np.gradient(h)
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) / 2
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) / 2
    nx, ny, nz = -gx * strength, gy * strength, np.ones_like(h)
    l = np.sqrt(nx ** 2 + ny ** 2 + nz ** 2)
    n = np.dstack([nx / l, ny / l, nz / l])
    return ((n * 0.5 + 0.5) * 255 + 0.5).astype(np.uint8)


def save_rgb(a, name, q=88):
    Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(out, name), quality=q, optimize=True)


def save_ms(metal, smooth, name, size=256):
    a = np.dstack([metal, np.zeros_like(metal), np.zeros_like(metal), smooth])
    Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), 'RGBA').resize((size, size), Image.LANCZOS).save(os.path.join(out, name), optimize=True)


def scratches(n, count, seed, length=(20, 120)):
    r = np.random.default_rng(seed)
    im = Image.new('L', (n, n), 0)
    d = ImageDraw.Draw(im)
    for _ in range(count):
        x, y = r.integers(0, n, 2); L = r.integers(*length); a = r.uniform(0, np.pi)
        x2, y2 = x + L * np.cos(a), y + L * np.sin(a)
        w = int(r.integers(1, 3))
        for ox in (-n, 0, n):
            for oy in (-n, 0, n):
                d.line([x + ox, y + oy, x2 + ox, y2 + oy], fill=int(r.integers(90, 255)), width=w)
    return np.asarray(im.filter(ImageFilter.GaussianBlur(0.6))) / 255.0


N = 512
# ---------------------------------------------------------------- painted / powder-coated metal (neutral, tint with Kd)
low = tile_noise(N, 3, 1); mid = tile_noise(N, 12, 2); fine = tile_noise(N, 90, 3, octaves=2)
scr = scratches(N, 60, 4)
chips = (tile_noise(N, 20, 5) > 0.83).astype(float) * (tile_noise(N, 60, 6) > 0.5)
alb = 0.86 + 0.06 * (low - 0.5) + 0.03 * (mid - 0.5) - 0.18 * chips
alb = alb[..., None] * np.array([1.0, 1.0, 1.0])
alb = alb * (1 - 0.10 * scr[..., None]) + 0.10 * scr[..., None] * np.array([0.75, 0.75, 0.76])
save_rgb(alb, 'Proc_PaintedMetal_Color.jpg')
h = 0.6 * fine + 0.25 * mid - 0.5 * chips - 0.3 * scr
Image.fromarray(normal_from_height(h, 2.2)).save(os.path.join(out, 'Proc_PaintedMetal_Normal.jpg'), quality=90, optimize=True)
save_ms(0.05 * chips + 0.0 * low, 0.52 + 0.08 * (mid - 0.5) - 0.15 * chips + 0.12 * scr, 'Proc_PaintedMetal_MetallicSmoothness.png')

# ---------------------------------------------------------------- hot-dip galvanised steel (spangle)
pts = rng.uniform(0, N, (700, 2))
yy, xx = np.mgrid[0:N, 0:N]
best = np.full((N, N), 1e9); second = np.full((N, N), 1e9); cell = np.zeros((N, N), int)
for i, (px, py) in enumerate(pts):
    dx = np.minimum(abs(xx - px), N - abs(xx - px)); dy = np.minimum(abs(yy - py), N - abs(yy - py))
    dd = np.sqrt(dx ** 2 + dy ** 2)
    m = dd < best
    second = np.where(m, best, np.minimum(second, dd)); best = np.where(m, dd, best); cell = np.where(m, i, cell)
shade = rng.uniform(-1, 1, len(pts))[cell]
edge = np.clip(1 - (second - best) / 3.0, 0, 1)
g = 0.64 + 0.025 * shade + 0.06 * (low - 0.5) + 0.03 * (mid - 0.5) - 0.025 * edge
dirt = tile_noise(N, 4, 9)
g = g - 0.10 * np.clip(dirt - 0.6, 0, 1)
galv = np.dstack([g * 0.98, g, g * 1.02])
save_rgb(galv, 'Proc_Galvanised_Color.jpg')
Image.fromarray(normal_from_height(0.15 * shade * (1 - edge) + 0.3 * fine, 1.0)).save(os.path.join(out, 'Proc_Galvanised_Normal.jpg'), quality=90, optimize=True)
save_ms(0.85 - 0.25 * np.clip(dirt - 0.6, 0, 1) + 0.05 * shade, 0.48 + 0.10 * shade - 0.2 * np.clip(dirt - 0.6, 0, 1), 'Proc_Galvanised_MetallicSmoothness.png')

# ---------------------------------------------------------------- cast iron (grates, drain), dark rough
ci = 0.20 + 0.08 * low + 0.05 * (mid - 0.5)
rust = np.clip(tile_noise(N, 6, 11) - 0.55, 0, 1) * 2.0
cast = np.dstack([ci + 0.10 * rust, ci + 0.04 * rust, ci - 0.02 * rust])
save_rgb(cast, 'Proc_CastIron_Color.jpg')
Image.fromarray(normal_from_height(0.8 * fine + 0.3 * mid, 3.0)).save(os.path.join(out, 'Proc_CastIron_Normal.jpg'), quality=90, optimize=True)
save_ms(0.6 - 0.5 * rust, 0.30 + 0.15 * (1 - rust) * (mid > 0.6), 'Proc_CastIron_MetallicSmoothness.png')

# ---------------------------------------------------------------- sign / panel atlas 1024
A = 1024
atlas = Image.new('RGB', (A, A), (128, 128, 128))
d = ImageDraw.Draw(atlas)
F = lambda s: ImageFont.truetype(font_path, s)
RED, BLUE, WHITE, BLACK, YEL = (200, 20, 30), (0, 70, 160), (240, 240, 238), (15, 15, 16), (245, 190, 20)
# cell A: no-stopping sign (u 0-0.5, v 0.5-1) -> pixels x 0-512, y 0-512
cx, cy, R = 256, 256, 240
d.ellipse([cx - R, cy - R, cx + R, cy + R], fill=RED)
d.ellipse([cx - R + 46, cy - R + 46, cx + R - 46, cy + R - 46], fill=BLUE)
for ang in (45, -45):
    a = np.radians(ang); dx, dy = np.cos(a) * (R - 46), np.sin(a) * (R - 46)
    d.line([cx - dx, cy - dy, cx + dx, cy + dy], fill=RED, width=46)
# cell B: one-way sign (u 0.5-1, v 0.5-1) -> x 512-1024, y 0-512
d.rectangle([516, 4, 1020, 508], fill=WHITE)
d.rectangle([530, 18, 1006, 494], fill=BLUE)
d.polygon([(768, 70), (900, 230), (810, 230), (810, 440), (726, 440), (726, 230), (636, 230)], fill=WHITE)
# cell C: street name plate (u 0-1, v 0.25-0.5) -> y 512-768
d.rectangle([0, 512, 1023, 767], fill=(20, 32, 70))
d.rectangle([14, 526, 1009, 753], outline=WHITE, width=8)
txt = 'KESTREL STREET'
fs = 150
while d.textlength(txt, font=F(fs)) > 880:
    fs -= 4
f = F(fs)
w = d.textlength(txt, font=f)
d.text(((A - w) / 2, 548), txt, font=f, fill=WHITE)
f2 = F(40); t2 = 'Old Town  -  District 4'
d.text(((A - d.textlength(t2, font=f2)) / 2, 692), t2, font=f2, fill=(200, 205, 215))
# cell D: traffic-signal backboard (u 0-0.25, v 0-0.25) -> x 0-256, y 768-1024 (black with yellow border)
d.rectangle([0, 768, 255, 1023], fill=YEL)
d.rectangle([22, 790, 233, 1001], fill=BLACK)
# cell E: pay-and-display machine front (u 0.25-0.5, v 0-0.25) -> x 256-512, y 768-1024
d.rectangle([256, 768, 511, 1023], fill=(52, 56, 60))
d.rectangle([286, 790, 481, 860], fill=(25, 60, 70))            # display
d.text((296, 800), '12:47  P 2.40', font=F(30), fill=(150, 230, 200))
for r_ in range(4):
    for c_ in range(3):
        x0 = 300 + c_ * 42; y0 = 875 + r_ * 30
        d.rounded_rectangle([x0, y0, x0 + 34, y0 + 24], 4, fill=(170, 172, 175))
d.rectangle([440, 878, 488, 890], fill=(10, 10, 10))              # card slot
d.rectangle([440, 905, 488, 990], fill=(30, 30, 32))              # ticket bay
d.rounded_rectangle([290, 995, 340, 1018], 6, fill=BLUE)
d.text((303, 993), 'P', font=F(24), fill=WHITE)
# cell F: retro-reflective band (u 0.5-0.75, v 0-0.25) -> x 512-768
band = Image.new('RGB', (256, 256), (235, 236, 232))
bd = ImageDraw.Draw(band)
for yy_ in range(0, 256, 8):
    for xx_ in range(0, 256, 8):
        bd.polygon([(xx_, yy_ + 4), (xx_ + 4, yy_), (xx_ + 8, yy_ + 4), (xx_ + 4, yy_ + 8)], outline=(210, 212, 208))
atlas.paste(band, (512, 768))
# cell G: bus-stop timetable / city map panel (u 0.75-1, v 0-0.25) -> x 768-1024
d.rectangle([768, 768, 1023, 1023], fill=(235, 236, 230))
d.rectangle([768, 768, 1023, 812], fill=(0, 110, 90))
d.text((782, 774), 'BUS  12 . 31', font=F(32), fill=WHITE)
for i in range(10):
    yv = 830 + i * 18
    d.line([782, yv, 1008, yv], fill=(170, 170, 170), width=2)
    d.text((784, yv - 16), '%02d:%02d' % (6 + i, (i * 17) % 60), font=F(14), fill=(40, 40, 40))
# texture noise / wear on the whole atlas
a = np.asarray(atlas).astype(np.float32) / 255
wear = tile_noise(A, 6, 21)
a = a * (0.93 + 0.07 * wear[..., None])
save_rgb(a, 'Proc_Signs_Color.jpg', q=90)

# ---------------------------------------------------------------- frosted lightbox (bus shelter ad panel, project-made abstract art)
L = Image.new('RGB', (512, 1024), (240, 240, 236))
ld = ImageDraw.Draw(L)
for i in range(1024):
    t = i / 1024
    ld.line([0, i, 511, i], fill=(int(30 + 40 * t), int(40 + 60 * t), int(70 + 110 * t)))
for k in range(9):
    r_ = 60 + k * 26
    ld.ellipse([256 - r_, 380 - r_, 256 + r_, 380 + r_], outline=(220, 200, 150), width=2)
ld.text((60, 760), 'CITY OF', font=F(54), fill=(235, 235, 235))
ld.text((60, 820), 'LIGHTS', font=F(110), fill=(250, 220, 150))
ld.text((60, 950), 'Winter festival  -  Old Town', font=F(30), fill=(220, 220, 225))
L = L.filter(ImageFilter.GaussianBlur(0.8))
L.save(os.path.join(out, 'Proc_Lightbox_Color.jpg'), quality=88, optimize=True)
print('textures written to', out)
