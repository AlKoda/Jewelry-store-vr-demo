"""Build the CutoutDecals pack from CC0 Poly Haven / ambientCG photographs.
Usage: python3 -P build_cutouts.py <download_root> <out_dir>
Every output is straight-alpha RGBA PNG; RGB under transparent pixels is filled
with the nearest opaque colour (edge bleeding) so mipmaps/bilinear never show a dark halo."""
import sys, json, os, numpy as np
from PIL import Image, ImageFilter, ImageDraw
from scipy import ndimage as ndi
D, OUT = sys.argv[1], sys.argv[2]
R = np.random.default_rng(20261017)
PH = D + '/tuniveza_Generative-Games/The-Ruins-of-Bezan/assets/models/'
LM = D + '/TheMarco_liminal/models/cc0/'
SH = D + '/EliteGamer007_6dof-drone-sim/assets/polyhaven/models/'
TA = D + '/ta/PBR/'
def load(p): return np.asarray(Image.open(p).convert('RGB')).astype(np.float32) / 255
def disk(r):
    y, x = np.mgrid[-r:r+1, -r:r+1]; return x*x + y*y <= r*r
def noise(h, w, cell, seed):
    g = np.random.default_rng(seed).random((max(2, h//cell+2), max(2, w//cell+2)))
    return np.asarray(Image.fromarray((g*255).astype(np.uint8)).resize((w+cell, h+cell), Image.BICUBIC))[:h, :w] / 255.
def fbm(h, w, seed, base=128, oct=5):
    t = np.zeros((h, w)); a = 1; s = 0
    for o in range(oct):
        t += a*noise(h, w, max(2, base >> o), seed+o); s += a; a *= .5
    return t/s
def sstep(a, b, x): x = np.clip((x-a)/(b-a), 0, 1); return x*x*(3-2*x)
def bleed(rgb, a):
    core = a > 0.9
    if core.sum() == 0: core = a > 0.3
    if core.sum() == 0: return rgb
    idx = ndi.distance_transform_edt(~core, return_distances=False, return_indices=True)
    out = rgb.copy(); out[~core] = rgb[idx[0], idx[1]][~core]; return out
FILES = []
def save(name, rgba, **meta):
    rgba = np.clip(rgba, 0, 1); a = rgba[..., 3]
    rgba[..., :3] = bleed(rgba[..., :3], a)
    p = os.path.join(OUT, name); os.makedirs(os.path.dirname(p), exist_ok=True)
    Image.fromarray((rgba*255+.5).astype(np.uint8), 'RGBA').save(p, optimize=True)
    meta.update(file=name, pixels=list(rgba.shape[1::-1])); FILES.append(meta)
def to_img(rgba): return Image.fromarray((np.clip(rgba, 0, 1)*255+.5).astype(np.uint8), 'RGBA')
def from_img(im): return np.asarray(im).astype(np.float32)/255
# ---------- leaf sprites ----------
def extract(path, keep, xmax=1024, r=9):
    a = load(path); m = a.max(2) > 22/255; m[:, xmax:] = False
    m = ndi.binary_opening(m, structure=disk(r)); lab, n = ndi.label(m); objs = ndi.find_objects(lab); res = []
    for i in keep:
        sl = objs[i-1]; mm = (lab == i)
        soft = ndi.gaussian_filter(ndi.binary_erosion(mm, iterations=1).astype(np.float32), 0.8)
        y0, y1, x0, x1 = sl[0].start-4, sl[0].stop+4, sl[1].start-4, sl[1].stop+4
        y0, x0 = max(0, y0), max(0, x0)
        rgba = np.dstack([a[y0:y1, x0:x1], soft[y0:y1, x0:x1]]); rgba[..., :3] = bleed(rgba[..., :3], rgba[..., 3])
        res.append(rgba)
    return res
LEAF_SRC = [('potted_plant_02', PH+'potted_plant_02/textures/potted_plant_02_leaves_diff_1k.jpg', [1, 2, 4, 6], 800, 'arrowhead (Syngonium-type) leaf'),
            ('weed_plant_02', PH+'weed_plant_02/textures/weed_plant_02_diff_1k.jpg', [1, 3, 5, 8, 12], 1024, 'dock (Rumex) leaf'),
            ('potted_plant_01', LM+'potted_plant_01/textures/potted_plant_01_leaves_diff_1k.jpg', [1, 2, 3, 7, 10], 820, 'lobed (Pelargonium-type) leaf')]
LEAVES = []
for nm, p, keep, xm, kind in LEAF_SRC:
    for s in extract(p, keep, xm): LEAVES.append((nm, kind, s))
def hsv(rgb):
    im = Image.fromarray((rgb*255).astype(np.uint8)).convert('HSV'); return np.asarray(im).astype(np.float32)/255
def rgb_(h):
    return np.asarray(Image.fromarray((np.clip(h, 0, 1)*255).astype(np.uint8), 'HSV').convert('RGB')).astype(np.float32)/255
def recolour(s, mode, seed):
    g = np.random.default_rng(seed); rgb = s[..., :3]; a = s[..., 3]; H = hsv(rgb)
    if mode == 'green': return s.copy()
    m = H[..., 0][a > .5].mean() if (a > .5).any() else .25
    h, w = a.shape; blot = fbm(h, w, seed, max(8, w//4), 4)
    if mode == 'autumn':
        tgt = g.choice([0.01, 0.04, 0.07, 0.10, 0.12]) + (blot-.5)*0.06
        H[..., 0] = (tgt + (H[..., 0]-m)*0.25) % 1; H[..., 1] = np.clip(H[..., 1]*1.25+.1, 0, 1); H[..., 2] *= 1.05
    elif mode == 'dry':
        tgt = g.uniform(.05, .09); H[..., 0] = (tgt + (H[..., 0]-m)*0.15) % 1
        H[..., 1] = H[..., 1]*.55; H[..., 2] *= .70 + .25*blot
    elif mode == 'wet':
        tgt = g.uniform(.03, .08); H[..., 0] = (tgt + (H[..., 0]-m)*0.2) % 1
        H[..., 1] = np.clip(H[..., 1]*1.1, 0, 1); H[..., 2] *= .42 + .12*blot
    out = rgb_(H)
    if mode == 'dry':  # edges curl and brown more
        d = ndi.distance_transform_edt(a > .5); e = np.exp(-d/6)[..., None]
        out = out*(1-.35*e) + np.array([.25, .16, .08])*.35*e
    return np.dstack([out, a])
def fit_cell(s, cw, ch, fill=.9):
    im = to_img(s); k = min(cw*fill/im.width, ch*fill/im.height); im = im.resize((max(1, int(im.width*k)), max(1, int(im.height*k))), Image.LANCZOS)
    c = Image.new('RGBA', (cw, ch), (0, 0, 0, 0)); c.alpha_composite(im, ((cw-im.width)//2, (ch-im.height)//2)); return c
def atlas(name, sprites, cols, rows, cell, **meta):
    page = Image.new('RGBA', (cols*cell, rows*cell), (0, 0, 0, 0)); rects = []
    for i, (lab, s) in enumerate(sprites):
        x, y = (i % cols)*cell, (i//cols)*cell; page.alpha_composite(fit_cell(s, cell, cell), (x, y))
        rects.append(dict(name=lab, x=x, y=y, w=cell, h=cell, uv=[x/page.width, 1-(y+cell)/page.height, cell/page.width, cell/page.height]))
    save(name, from_img(page).copy(), rects=rects, **meta)
for mode, nm in [('green', 'Leaves_Green_Atlas'), ('autumn', 'Leaves_Autumn_Atlas'), ('dry', 'Leaves_Dry_Atlas'), ('wet', 'Leaves_Wet_Atlas')]:
    sp = [(f'{mode}_{i:02d}_{LEAVES[i % len(LEAVES)][0]}', recolour(LEAVES[i % len(LEAVES)][2], mode, 100+i)) for i in range(16)]
    atlas(f'Leaves/{nm}.png', sp, 4, 4, 512, group='Leaves', real_size='each cell 0.16 x 0.16 m (leaf 8-15 cm)',
          description=f'16 single photographed leaves (arrowhead, dock, lobed), {mode} colour state; 4x4 grid of 512 px cells, uv rects listed (Unity bottom-left origin).',
          placement='Single fallen leaves on pavement, kerb, doormat, window ledge; random yaw', derived=('original colours' if mode == 'green' else f'HSV recolour "{mode}" in build_cutouts.py:recolour'))
# ---------- composites ----------
def place(canvas, s, x, y, scale, rot, shadow=0.35):
    im = to_img(s); im = im.resize((max(2, int(im.width*scale)), max(2, int(im.height*scale))), Image.LANCZOS).rotate(rot, Image.BICUBIC, expand=True)
    if shadow:
        a = np.asarray(im)[..., 3].astype(np.float32)/255; sh = ndi.gaussian_filter(a, 3)*shadow
        shim = to_img(np.dstack([np.zeros(a.shape+(3,)), sh])); canvas.alpha_composite(shim, (int(x-im.width/2+3), int(y-im.height/2+4)))
    canvas.alpha_composite(im, (int(x-im.width/2), int(y-im.height/2)))
def pile(name, w, h, n, modes, size, density, seed, **meta):
    g = np.random.default_rng(seed); c = Image.new('RGBA', (w, h), (0, 0, 0, 0)); dens = fbm(h, w, seed, 256, 3)*density(w, h)
    k = 0
    while k < n:
        x, y = g.uniform(.08, .92)*w, g.uniform(.08, .92)*h
        if g.random() > dens[int(y), int(x)]: continue
        L = LEAVES[g.integers(len(LEAVES))][2]; s = recolour(L, g.choice(modes), int(g.integers(1e6)))
        sc = size*g.uniform(.6, 1.2)/max(L.shape[:2]); place(c, s, x, y, sc, g.uniform(0, 360)); k += 1
    arr = from_img(c).copy(); edge = sstep(0, .08, np.minimum.reduce(np.meshgrid(np.linspace(0, 1, w), np.linspace(0, 1, h))+np.meshgrid(np.linspace(1, 0, w), np.linspace(1, 0, h))))
    arr[..., 3] *= edge; save(name, arr, group='Leaves', **meta)
def blob(w, h): yy, xx = np.mgrid[0:h, 0:w]; r = np.hypot((xx-w/2)/(w/2), (yy-h/2)/(h/2)); return np.clip(1.3-r, 0, 1)
def strip(w, h): yy = np.mgrid[0:h, 0:w][0]; return np.clip(1.4-np.abs(yy-h*.62)/(h*.35), 0, 1)
pile('Leaves/LeafCluster_DryPile.png', 1024, 1024, 70, ['dry', 'dry', 'autumn'], 150, blob, 1, real_size='0.9 x 0.9 m', description='Pile of dry brown leaves with soft contact shadow', placement='Doorway corners, tree bases, against kerb')
pile('Leaves/LeafCluster_AutumnScatter.png', 1024, 1024, 38, ['autumn', 'autumn', 'dry', 'green'], 140, lambda w, h: np.ones((h, w))*.8, 2, real_size='1.2 x 1.2 m', description='Loose scatter of mixed autumn leaves', placement='Pavement, under the three street trees')
pile('Leaves/LeafCluster_WetGutter.png', 1024, 512, 55, ['wet', 'wet', 'dry'], 120, strip, 3, real_size='1.6 x 0.8 m (long axis U)', description='Wet dark leaves washed into a band, for gutters', placement='Along the kerb/gutter line, drains')
pile('Leaves/LeafCluster_GreenFresh.png', 1024, 1024, 30, ['green', 'green', 'autumn'], 140, blob, 4, real_size='0.8 x 0.8 m', description='Freshly fallen green leaves after wind', placement='Under trees, planters')
# foliage crown cards
def crown(seed, modes, n, dens=1.0):
    g = np.random.default_rng(seed); c = Image.new('RGBA', (1024, 1024), (0, 0, 0, 0)); shape = fbm(1024, 1024, seed, 300, 3)
    pts = []
    while len(pts) < n:
        x, y = g.uniform(40, 984), g.uniform(40, 984); r = np.hypot(x-512, (y-540)*1.1)/470
        if r < 1 and g.random() < (1-r**2)*(.5+shape[int(y), int(x)])*dens: pts.append((x, y, r))
    pts.sort(key=lambda p: -p[2])  # outer first, inner later? draw inner (darker) first instead
    pts.sort(key=lambda p: p[2])
    for x, y, r in pts:
        L = LEAVES[g.integers(len(LEAVES))][2]; s = recolour(L, g.choice(modes), int(g.integers(1e6)))
        s = s.copy(); s[..., :3] *= .55 + .45*min(1, r+.2) + g.uniform(-.05, .05)  # self-shadowing toward core
        place(c, s, x, y, g.uniform(60, 95)/max(L.shape[:2]), g.uniform(0, 360), shadow=.25)
    return c
page = Image.new('RGBA', (2048, 2048)); rects = []
for i, (lab, modes, n, dn) in enumerate([('Summer_A', ['green'], 900, 1), ('Summer_B', ['green', 'green', 'autumn'], 800, 1), ('Autumn', ['autumn', 'autumn', 'dry', 'green'], 750, 1), ('Sparse_LateAutumn', ['autumn', 'dry'], 320, .6)]):
    x, y = (i % 2)*1024, (i//2)*1024; page.alpha_composite(crown(10+i, modes, n, dn), (x, y)); rects.append(dict(name=lab, x=x, y=y, w=1024, h=1024, uv=[x/2048, 1-(y+1024)/2048, .5, .5]))
save('Foliage/FoliageCrown_Atlas.png', from_img(page).copy(), group='Foliage', rects=rects, real_size='each card ~1.8 x 1.8 m', description='Four dense leaf-crown billboard cards (summer x2, autumn, sparse late autumn) built from the photographed leaves; inner leaves darkened for self-shadowing', placement='Crossed quads / billboards on the three bare procedural trees; Standard Cutout 0.4, double-sided')
# grass blades
emp = load(PH+'flower_empodium/textures/flower_empodium_diff_1k.jpg'); em = emp.max(2) > 22/255
em = ndi.binary_opening(em, iterations=1); lab, n = ndi.label(em); BL = []
for i, sl in enumerate(ndi.find_objects(lab)):
    mm = lab[sl] == i+1
    hh, ww = sl[0].stop-sl[0].start, sl[1].stop-sl[1].start
    if mm.sum() < 1500 or hh < 150 or hh < 2.5*ww: continue
    a = ndi.gaussian_filter(mm.astype(np.float32), .7); BL.append(np.dstack([bleed(emp[sl], a), a]))
def clump(seed, n, h_px, dry):
    g = np.random.default_rng(seed); c = Image.new('RGBA', (512, 1024), (0, 0, 0, 0))
    for k in range(n):
        b = BL[g.integers(len(BL))]; s = recolour(b, 'dry' if g.random() < dry else 'green', int(g.integers(1e6))) if dry else b.copy()
        s[..., :3] *= g.uniform(.8, 1.15); im = to_img(s); sc = h_px*g.uniform(.5, 1)/im.height
        im = im.resize((max(2, int(im.width*sc)), max(2, int(im.height*sc))), Image.LANCZOS)
        if g.random() < .5: im = im.transpose(Image.FLIP_LEFT_RIGHT)
        im = im.rotate(g.uniform(-25, 25), Image.BICUBIC, expand=True)
        c.alpha_composite(im, (int(256+g.normal(0, 40)-im.width/2), max(0, 1000-im.height)))
    return c
page = Image.new('RGBA', (2048, 1024)); rects = []
for i, (lab, n, hp, dry) in enumerate([('Green_Tall', 40, 900, 0), ('Green_Short', 50, 560, 0), ('Mixed_Dry', 45, 760, .5), ('Straw_Dry', 40, 650, 1)]):
    page.alpha_composite(clump(30+i, n, hp, dry), (i*512, 0)); rects.append(dict(name=lab, x=i*512, y=0, w=512, h=1024, uv=[i/4, 0, .25, 1]))
save('Foliage/GrassClump_Atlas.png', from_img(page).copy(), group='Foliage', rects=rects, real_size='each card 0.25 x 0.5 m, root at bottom centre', description='Four upright grass clump cards from photographed grass blades (green tall/short, mixed, dry straw)', placement='Crossed quads at tree pits, wall bases, pavement cracks; Cutout 0.4')
# ---------- ground photo masks ----------
def masked(tex, w, h, mask, x=0, y=0, gain=1.0):
    t = load(tex)[y:y+h, x:x+w]*gain; return np.dstack([t, mask])
def crackmask(w, h, seed, n, width, horizontal=True):
    g = np.random.default_rng(seed); im = Image.new('L', (w, h)); d = ImageDraw.Draw(im)
    for k in range(n):
        x, y = (0, g.uniform(.3, .7)*h) if horizontal else (g.uniform(.2, .8)*w, g.uniform(.2, .8)*h); ang = 0 if horizontal else g.uniform(0, 6.28); pts = [(x, y)]
        L = w if horizontal else g.uniform(.3, .6)*w
        for s in range(int(L/12)):
            ang += g.normal(0, .35); ang *= .9 if horizontal else 1; x += 12*np.cos(ang); y += 12*np.sin(ang); pts.append((x, y))
        d.line(pts, fill=255, width=int(width*(1 if k == 0 else .5)))
    return np.asarray(im.filter(ImageFilter.GaussianBlur(width*.4))).astype(np.float32)/255
# weeds / moss in cracks
for i, (src, nm) in enumerate([(TA+'Ground/Ground037_2K-JPG/Ground037_2K-JPG_Color.jpg', 'WeedsInCrack_Moss'), (TA+'Ground/Ground079S_2K-JPG/Ground079S_2K-JPG_Color.jpg', 'WeedsInCrack_Clover')]):
    w, h = 1024, 256; cm = crackmask(w, h, 40+i, 3, 46); nz = fbm(h, w, 50+i, 24, 4)
    a = sstep(.35, .6, cm*(.6+.8*nz)); arr = masked(src, w, h, a, 200, 600)
    c = to_img(arr)
    g = np.random.default_rng(60+i)
    for k in range(9):
        L = LEAVES[-1-g.integers(5)][2]; x = g.uniform(60, 960); place(c, L, x, h*.5+g.normal(0, 15), g.uniform(40, 70)/max(L.shape[:2]), g.uniform(0, 360), .3)
    save(f'Ground/{nm}.png', from_img(c).copy(), group='Weeds', real_size='1.2 x 0.3 m (long axis U)', description='Moss/grass photo masked to a crack line with tiny lobed weed leaves', placement='Pavement slab joints, kerb/wall junction')
w = h = 1024; m = sstep(.45, .62, fbm(h, w, 70, 220, 5)*blob(w, h)*1.6)
save('Ground/MossPatch.png', masked(TA+'Ground/Ground037_2K-JPG/Ground037_2K-JPG_Color.jpg', w, h, m), group='Weeds', real_size='0.8 x 0.8 m', description='Irregular moss patch (ambientCG Ground037 moss photo, fbm mask)', placement='Shaded wall bases, north side of kerb, roof ledge')
# stains
for nm, src, xy, gain, lo, hi, sz, desc, pl in [
  ('Stain_OilLarge', TA+'Asphalt/Asphalt010_2K-JPG/Asphalt010_2K-JPG_Color.jpg', (300, 300), .35, .42, .62, '1.2 x 1.2 m', 'Dark engine-oil stain, asphalt photo darkened under an fbm blob', 'Parking bay, road by the kerb'),
  ('Stain_MudSplash', TA+'Ground/Ground049C_2K-JPG/Ground049C_2K-JPG_Color.jpg', (500, 200), 1.0, .5, .58, '1.0 x 1.0 m', 'Wet mud splash/smear from ground photo', 'Street near tree pits, entrance mat'),
  ('Stain_DustPatch', TA+'Ground/Ground072_2K-JPG/Ground072_2K-JPG_Color.jpg', (100, 900), 1.1, .3, .75, '1.0 x 1.0 m', 'Soft dust/grit patch, very feathered', 'Interior corners, under shelving, pavement')]:
    s = hash(nm) % 1000; mask = sstep(lo, hi, fbm(1024, 1024, s, 260, 6)*blob(1024, 1024)*1.7)
    if 'Dust' in nm: mask *= .6
    if 'Oil' in nm: mask *= .85
    save(f'Stains/{nm}.png', masked(src, 1024, 1024, mask, *xy, gain=gain), group='Stains', real_size=sz, description=desc, placement=pl)
# dirt edge strip
w, h = 1024, 256; yy = np.linspace(0, 1, h)[:, None]; m = sstep(.0, 1, (1-yy)**1.5*(.4+1.2*fbm(h, w, 80, 64, 5)))*.9
save('Stains/DirtEdge_WallBase.png', masked(TA+'Ground/Ground053_2K-JPG/Ground053_2K-JPG_Color.jpg', w, h, m, 0, 0, .8), group='Stains', real_size='1.5 x 0.35 m, opaque edge at V=0', description='Splashed dirt along a wall base, photo soil under a rising noisy gradient', placement='Exterior wall/shutter base, kerb face; tile in U')
m = sstep(.15, .8, ndi.gaussian_filter((np.random.default_rng(81).random((256, 1024)) > .9985).astype(float), 1.2)*30+0)  # unused fine grit
# rust streak from rusted can
rust = load(LM+'can_rusted/textures/can_rusted_diff_1k.jpg')[560:1000, 20:1000]
rr = np.asarray(Image.fromarray((rust*255).astype(np.uint8)).crop((0, 0, 220, 440)).resize((512, 1024), Image.BICUBIC)).astype(np.float32)/255
xx = np.linspace(-1, 1, 512)[None, :]; yy = np.linspace(0, 1, 1024)[:, None]; n = fbm(1024, 512, 90, 40, 4)
m = sstep(.25, .7, (1-np.abs(xx)/(.25+.5*yy))*(1-yy*.8)*(.5+n))
save('Stains/RustStreak_Wall.png', np.dstack([rr, m]), group='Stains', real_size='0.3 x 0.6 m, source at top', description='Rust run-off streak below a bracket/bolt, from rusted-can photo', placement='Under shutter guides, sign brackets, drainpipe clips')
# cracks from photo
def photo_crack(src, x, y, w, h, thr, seed):
    t = load(src)[y:y+h, x:x+w]; L = t.mean(2); d = ndi.gaussian_filter(L, 6)-L
    a = sstep(thr, thr*2.4, d)*sstep(.2, .5, fbm(h, w, seed, 200, 3)*blob(w, h)*2); return np.dstack([t*.8, a])
save('Cracks/Crack_AsphaltNetwork.png', photo_crack(TA+'Asphalt/Asphalt021_2K-JPG/Asphalt021_2K-JPG_Color.jpg', 400, 400, 1024, 1024, .045, 91), group='Cracks', real_size='1.5 x 1.5 m', description='Alligator crack network lifted from an asphalt photo (local-darkness key)', placement='Road surface, old car-park patches')
save('Cracks/Crack_ConcreteHairline.png', photo_crack(TA+'Concrete/Concrete033_2K-JPG/Concrete033_2K-JPG_Color.jpg', 200, 500, 1024, 1024, .03, 92), group='Cracks', real_size='1.2 x 1.2 m', description='Hairline concrete cracks lifted from a concrete photo', placement='Pavement slabs, interior screed, plinths')
# rubber scuffs
tyre = load(LM+'old_tyre/textures/old_tyre_diff_1k.jpg')[0:512, 0:1024]; w, h = 1024, 512; im = Image.new('L', (w, h)); dr = ImageDraw.Draw(im); g = np.random.default_rng(95)
for k in range(14):
    cx, cy, r = g.uniform(100, 900), g.uniform(100, 400), g.uniform(60, 260); a0 = g.uniform(0, 360)
    dr.arc([cx-r, cy-r, cx+r, cy+r], a0, a0+g.uniform(15, 50), fill=int(g.uniform(90, 230)), width=int(g.uniform(4, 16)))
m = np.asarray(im.filter(ImageFilter.GaussianBlur(2))).astype(np.float32)/255*(.4+.6*fbm(h, w, 96, 16, 3))
save('Scuffs/RubberScuffs_Floor.png', np.dstack([tyre*.6, m]), group='Scuffs', real_size='1.0 x 0.5 m', description='Black rubber heel/trolley scuff arcs, colour from tyre photo (no footprints)', placement='Shop floor near counters and door, threshold')
# ---------- litter ----------
paper = load(LM+'book_encyclopedia_set_01/textures/book_encyclopedia_set_01_paper_diff_1k.jpg')
bag = load(LM+'trashbag/textures/trashbag_diff_1k.jpg'); food = load(LM+'long_life_food/textures/long_life_food_diff_1k.jpg')
crease = ndi.gaussian_filter(bag.mean(2), 1) - ndi.gaussian_filter(bag.mean(2), 12); crease = (crease-crease.mean())/crease.std()
def torn(h, w, seed, inset=10, rough=8):
    g = np.random.default_rng(seed); m = np.zeros((h, w), np.float32); yy, xx = np.mgrid[0:h, 0:w]
    e = lambda n, s: np.asarray(Image.fromarray((g.random(max(2, n//6))*255).astype(np.uint8)[None]).resize((n, 1), Image.BICUBIC))[0]/255*s
    top, bot = e(w, rough), e(w, rough); lef, rig = e(h, rough*.5), e(h, rough*.5)
    m = (yy > inset+top[None]) & (yy < h-inset-bot[None]) & (xx > inset+lef[:, None]) & (xx < w-inset-rig[:, None])
    return ndi.gaussian_filter(m.astype(np.float32), .8)
def crumple(rgb, k, seed):
    h, w = rgb.shape[:2]; g = np.random.default_rng(seed); y, x = g.integers(0, 1024-h), g.integers(0, 1024-w) if w < 1024 else 0
    c = crease[y:y+h, x:x+w] if w < 1024 else crease[:h, :w]; return np.clip(rgb*(1+k*c[..., None]*.12), 0, 1)
items = []
for i in range(3):  # receipts
    h, w = 440, 130; base = paper[100+i*200:100+i*200+h, 300:300+w]*np.array([1, .99, .96]); g = np.random.default_rng(200+i)
    pr = np.ones((h, w))
    for row in range(40, h-60, 13):
        L = int(g.uniform(.3, .85)*(w-30)); pr[row:row+4, 15:15+L] -= g.uniform(.35, .6)*(g.random(L) > .25)
    pr = ndi.gaussian_filter(pr, .6); items.append((f'Receipt_{i}', np.dstack([crumple(base*pr[..., None], 1+i, i), torn(h, w, 300+i, 4, 10)])))
for i, (y0, x0, hh, ww) in enumerate([(70, 80, 160, 220), (360, 10, 230, 330), (850, 20, 150, 330), (290, 260, 300, 200)]):  # flyer/label fragments
    t = food[y0:y0+hh, x0:x0+ww]; items.append((f'LabelScrap_{i}', np.dstack([crumple(t, 3, 10+i), torn(hh, ww, 400+i, 6, 18)])))
for i in range(3):  # crumpled paper balls
    s = 260; yy, xx = np.mgrid[0:s, 0:s]; r = np.hypot(xx-s/2, yy-s/2)/(s/2); n = fbm(s, s, 500+i, 30, 3)
    a = sstep(.98, .9, r+.25*(n-.5)); base = paper[300:300+s, 100*i:100*i+s] if i < 2 else food[650:650+s, 20:20+s]
    shade = np.clip(1+crease[200*i:200*i+s, 300:300+s]*.35, .3, 1.5)*(1-.35*r**2); items.append((f'CrumpledPaper_{i}', np.dstack([np.clip(base*shade[..., None], 0, 1), a])))
for i in range(2):  # plastic bag fragments
    s = 420; n = fbm(s, s, 600+i, 70, 5); yy, xx = np.mgrid[0:s, 0:s]; r = np.hypot(xx-s/2, (yy-s/2)*1.3)/(s/2)
    a = sstep(.55, .62, (1-r)+.6*n)*.97; t = bag[100+300*i:100+300*i+s, 200:200+s]; items.append((f'PlasticBag_{i}', np.dstack([t*1.2, a])))
lid = load(LM+'can_rusted/textures/can_rusted_diff_1k.jpg')[10:240, 0:230]; yy, xx = np.mgrid[0:230, 0:230]; r = np.hypot(xx-115, yy-113)/108
items.append(('Cap_RustyLid', np.dstack([lid, sstep(1.0, .95, r)])))
crushed = to_img(np.dstack([lid, sstep(1.0, .95, r)])).resize((230, 150), Image.LANCZOS); items.append(('Cap_RustyLid_Crushed', from_img(crushed).copy()))
tan = load(TA+'Ground/Ground072_2K-JPG/Ground072_2K-JPG_Color.jpg')
for i in range(6):  # cigarette butts
    g = np.random.default_rng(700+i); L, W = int(g.uniform(110, 170)), 34; yy, xx = np.mgrid[0:W, 0:L]
    cyl = (1-((yy-W/2)/(W/2))**2).clip(0, 1)**.5; filt = int(L*.62); base = paper[200:200+W, 50*i:50*i+L].copy()
    base[:, :filt] = tan[300+40*i:300+40*i+W, 100:100+filt]*np.array([1.25, 1.0, .7])*1.15
    base[:, filt:filt+3] *= .8; end = xx > L-12; base[end] = base[end]*np.array([.45, .42, .4])
    a = sstep(.05, .25, cyl)*(xx < L-g.uniform(0, 6)); rgb = base*(.55+.45*cyl[..., None]); s = np.dstack([rgb, a])
    items.append((f'CigaretteButt_{i}', from_img(to_img(s).rotate(g.uniform(-30, 30), Image.BICUBIC, expand=True)).copy()))
page = Image.new('RGBA', (2048, 2048)); rects = []; cols = 5; cell = 2048//cols; cell = 408
for i, (lab, s) in enumerate(items):
    x, y = (i % cols)*cell+4, (i//cols)*cell+4; page.alpha_composite(fit_cell(s, cell, cell, .88 if 'Cig' not in lab else .6), (x, y))
    rects.append(dict(name=lab, x=x, y=y, w=cell, h=cell, uv=[x/2048, 1-(y+cell)/2048, cell/2048, cell/2048]))
save('Litter/Litter_Atlas.png', from_img(page).copy(), group='Litter', rects=rects, real_size='cell size by item: receipt 8x28 cm, label scraps 10-20 cm, crumpled paper 7 cm, bag 35 cm, cap 3 cm, butt 3 cm (cell width ~2x item length)',
     description=f'{len(items)} litter cutouts on 408 px cells: receipts, torn product-label flyer scraps, crumpled paper, plastic bag fragments, rusty lid caps, cigarette butts', placement='Gutter, pavement, doorway, under benches; random yaw, 1-3 mm above ground')
# ---------- graffiti / stickers / posters ----------
def key_graffiti(img, rot=0):
    L = img.mean(2); sat = img.max(2)-img.min(2); bg = ndi.percentile_filter(L, 85, size=41)
    a = np.clip(np.maximum(sstep(.16, .32, sat), sstep(.2, .38, bg-L)), 0, 1); a = ndi.grey_closing(a, size=(9, 3)); a = ndi.gaussian_filter(a, .6)
    # unmultiply the grey shutter so paint colours stay saturated
    rgb = np.clip((img - (1-a[..., None])*bg[..., None]*0) , 0, 1)
    s = np.dstack([rgb, a]); im = to_img(s)
    if rot: im = im.rotate(rot, expand=True)
    return from_img(im).copy()
door = load(SH+'rollershutter_door/textures/rollershutter_door_graffiti_diff_1k.jpg'); win = load(SH+'rollershutter_window_01/textures/rollershutter_window_01_graffiti_diff_1k.jpg')
for nm, arr, rot, desc in [('Graffiti_PieceBubble', door[262:640, 0:800], 90, 'Pink/orange bubble-letter piece with throw-up and tags'),
                           ('Graffiti_PieceSnak', win[560:972, 30:620], 0, 'Teal/blue "Snak" piece with tags and handwriting'),
                           ('Graffiti_TagFlow', win[640:870, 790:905], -90, 'Small purple/red "FLOW" throw-up')]:
    g_ = key_graffiti(arr, rot); im = to_img(g_)
    if max(im.size) > 1024: im = im.resize((int(im.width*1024/max(im.size)), int(im.height*1024/max(im.size))), Image.LANCZOS)
    save(f'Graffiti/{nm}.png', from_img(im).copy(), group='Graffiti', real_size='~1.6 m wide (pieces), 0.5 m (tag)', description=desc+'; keyed off a Poly Haven roller-shutter photo (saturation + darkness vs. local background)', placement='Side walls, shutters, utility boxes across the street (not on the shop front)')

json.dump(FILES, open(os.path.join(OUT, 'manifest.json'), 'w'), indent=1)
