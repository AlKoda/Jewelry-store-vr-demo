import sys, os, json, hashlib, glob
from PIL import Image
W, P = sys.argv[1], sys.argv[2]
sys.path.insert(0, os.path.join(W, 'gen'))
from materials import MATS, srgb
man = json.load(open(os.path.join(W, 'stage2', 'build_manifest.json')))
post = json.load(open(os.path.join(W, 'post_meta.json')))
DATE = 'October 14, 2026'
PH = dict(source='https://polyhaven.com/a/vintage_pocket_watch', author='Tal Swicegood (Poly Haven)', license='CC0-1.0',
          license_url='https://creativecommons.org/publicdomain/zero/1.0/',
          fetched_from='https://github.com/QueenOfSquiggles/squiggle-pt/tree/deabff55b5df0b8989e58400bdf05de1c8e1eae1/Game/Assets/Models/Decoration',
          commit='deabff55b5df0b8989e58400bdf05de1c8e1eae1', files='vintage_pocket_watch_2k.gltf, vintage_pocket_watch.bin, textures/vintage_pocket_watch_{diff,nor_gl,arm}_2k.jpg')
SK = {
 'RingAntiqueTigerEye': dict(title='An Old Ring', source='https://sketchfab.com/3d-models/an-old-ring-ea5e0c11f2e046c993239ea1a3efcad7',
    author='Qifan Zhang (https://sketchfab.com/qifanzhang)', fetched_from='https://github.com/mhh-hazrati8068/nafis-jewellary/tree/0d857d9d8bdb61e5130d5683344db44a71db4e9b/public/models/an_old_ring',
    commit='0d857d9d8bdb61e5130d5683344db44a71db4e9b', changes='decimated 19,092 -> 7,000 triangles; re-oriented (finger axis X, stone up); scaled to metres (26.3 mm across, 17.1 mm inner diameter like the procedural rings); textures resized (colour 1024 JPG, normal and metallic-smoothness 512 PNG)'),
 'EarringPearlDrop': dict(title='Earring Jade', source='https://sketchfab.com/3d-models/earring-jade-c91e552728fb4b149d52193fd3e03ac5',
    author='Softmind Game Factory (https://sketchfab.com/softmind)', fetched_from='https://github.com/hiukim/mind-ar-js/tree/fb5db87b1e5076ba86fde82101955da70b629a1c/examples/face-tracking/assets/earring',
    commit='fb5db87b1e5076ba86fde82101955da70b629a1c', changes='decimated 11,832 -> 7,000 triangles; scaled to 40 mm (metres); colour and normal maps resized; metallic-smoothness map rebuilt from the colour map (gold metallic 1, pearls 0, smoothness 0.71 from the glTF roughnessFactor 0.292) because the upstream metallicRoughness map is a constant white'),
 'EarringGoldLeaf': dict(title='Gold leaf Earring', source='https://sketchfab.com/3d-models/gold-leaf-earring-64c4c78a861d45a28aa1b78873b02d14',
    author='DineshThennarasan (https://sketchfab.com/dineshthennarasan)', fetched_from='https://github.com/kongsanggun/AR_Randering/tree/2add0e0ae54f2679783ec7a352724457b6fd8611/public/model/earring/gold_leaf_earring',
    commit='2add0e0ae54f2679783ec7a352724457b6fd8611', changes='decimated 18,760 -> 6,998 triangles; scaled to 50 mm (metres); both materials replaced by Gold18k'),
}
CCBY = 'CC-BY-4.0'; CCBY_URL = 'https://creativecommons.org/licenses/by/4.0/'

DESC = {  # name: (category, what, placement, notes)
 'RingSolitaireDiamond': ('Ring', 'Platinum solitaire: 1 ct round brilliant (6.5 mm, 57 facets at Tolkowsky-like proportions) in a 4-claw basket with two gallery rails on a tapered comfort-fit shank with cathedral shoulders. About US size 6.75 (17.1 mm inner diameter).', 'Central case on RingCone or in RingBoxOpen; one in the robbed bay as an overlooked piece.', 'Finger axis = X, stone up. Hole centre at y = 0.0103 m. In RingBoxOpen rotate 90 deg about Y and place the hole centre at about (0, 0.034, 0); on RingCone rotate 90 deg about Z, hole centre at y = 0.0665 m.'),
 'RingGoldBand': ('Ring', '18k yellow-gold comfort-fit wedding band, 4.0 mm wide, 1.8 mm thick.', 'Ring trays, or dropped on the floor near a broken bay (evidence marker).', 'Finger axis X, hole centre at y = 0.0103 m; lying flat: rotate 90 deg about Z.'),
 'RingSapphireHalo': ('Ring', 'Platinum cathedral halo ring: 8 x 6 mm oval sapphire (north-south) in four claws that run down into the shank, with an under-gallery rail; halo of 18 single-cut 1.3 mm diamonds with shared beads on a frame whose tapered gallery skirt rests on raised cathedral shoulders (the head is one connected piece with the shank).', 'Central case, coloured-stone tray.', 'Finger axis X. Hole centre at y = 0.0102 m.'),
 'RingDiamondEternity': ('Ring', 'Platinum full eternity band, 3 mm, 22 x 2.0 mm single-cut diamonds with shared bead claws.', 'Bridal tray, wall case.', 'Finger axis X. Hole centre at y = 0.0111 m.'),
 'RingRubyThreeStone': ('Ring', '18k yellow-gold three-stone ring: 6 mm round ruby between two 4.5 mm diamonds, set along the band.', 'Central case or side island.', 'Finger axis X. Hole centre at y = 0.0104 m.'),
 'RingAntiqueTigerEye': ('Ring', 'Large antique-style signet ring with a cabochon tiger-eye and engraved, gem-set shoulders (textured, third-party CC BY); 17.1 mm inner diameter like the procedural rings.', 'Estate/antique tray, rear office repair tray.', 'Finger axis X, stone up. Hole centre at (0, 0.0121, 0.0014) m in model space (the other rings: (0, about 0.0103, 0)); use this offset on RingCone / in RingBoxOpen. Uses Color/Normal/MetallicSmoothness maps.'),
 'EarringsDiamondStuds': ('Earrings', 'Pair of 0.5 ct diamond studs (5.1 mm brilliants in 4-claw baskets whose claws and post are soldered to a small seat under the culet) with posts and butterfly backs.', 'Earring box, EarringStand card, central case.', 'Stones face +Z, posts point back (-Z); 16 mm apart.'),
 'EarringsGoldHoops': ('Earrings', 'Pair of 18k hinged hoops, 24 mm diameter, 2.4 mm tube tapering at the hinge.', 'Hang on EarringStand bar (bar at y = 0.124 m).', 'Hoop plane XY (facing +Z).'),
 'EarringPearlDrop': ('Earrings', 'Single pearl drop earring: pearl cluster top with gem surround, link and 13 mm baroque drop (textured, third-party CC BY).', 'EarringStand, necklace bust display, wall case.', 'Hanging (top at y = 0.040 m). Duplicate and mirror (scale X = -1) for a pair. Metallic-smoothness map: gold metallic 1, pearls 0, smoothness 0.71.'),
 'EarringGoldLeaf': ('Earrings', 'Single gold filigree leaf earring with stud, 50 mm long (third-party CC BY).', 'EarringStand, wall case.', 'Hanging, front +Z. Mirror for a pair.'),
 'NecklaceBust': ('Display', 'Velvet-covered neck form (jewellery bust) with sloping shoulders on a 10 mm walnut plinth, 326 mm tall.', 'Wall cases and window bays; one knocked over in a broken bay.', 'Front +Z. The *Draped necklaces use exactly the same transform.'),
 'NecklacePearlDraped': ('Necklace', 'Hand-knotted Akoya pearl strand, 45 cm: 58 pearls of about 7 mm with a silk knot in every gap and a fluted gold box clasp resting on the back of the neck, draped on NecklaceBust.', 'On NecklaceBust (same position and rotation).', 'Not grounded: authored in the NecklaceBust frame.'),
 'NecklacePendantDraped': ('Necklace', '18k cable chain (4.4 mm links, 45 cm) with a 1 ct round diamond pendant on a bail, draped on NecklaceBust.', 'On NecklaceBust (same transform).', 'Not grounded: authored in the NecklaceBust frame.'),
 'NecklacePearlDropped': ('Necklace', 'The same knotted pearl strand, clasp undone, lying in an irregular curve on the floor (dropped during the burglary).', 'Floor below a smashed bay, next to an evidence marker.', 'Lies on y = 0.'),
 'NecklacePendantDropped': ('Necklace', 'The gold chain and diamond pendant lying on the floor in an irregular curve.', 'Floor or counter top near the forced door / broken case.', 'Lies on y = 0.'),
 'BraceletTennisDiamond': ('Bracelet', 'Platinum tennis bracelet, 54 x 3 mm single-cut diamonds in box settings, laid as a flat oval, with box clasp.', 'Bracelet tray in the central case.', 'Lying flat.'),
 'BangleGold': ('Bracelet', '18k yellow-gold round bangle, 62 mm inner diameter, 5 x 2.6 mm D section.', 'Tray, or rolled under a case.', 'Lying flat (axis vertical).'),
 'WatchDressSteel': ('Watch', '39 mm stainless dress watch: polished case, domed crystal, navy sunburst dial (applied indices, date "14", fictional Aurel & Finch signature), dauphine hands at 10:09, crown, lugs 20 mm apart with spring bars, and a padded brown leather strap that wraps WatchPillow tightly and tucks under it (the buckle is under the cushion and not modelled).', 'Watch case on WatchPillow.', 'Same transform as WatchPillow: the case back rests 0.5 mm into the crown of the cushion, the strap follows the cushion 0.2 mm off its surface and its tails end flat on the shelf under the cushion edge; nothing goes below y = 0. The strap is shaped for the pillow, so use the watch with WatchPillow only. Glass material is transparent (alpha 0.2).'),
 'WatchPillow': ('Display', 'C-section black leather watch cushion, 78 x 62 x 48 mm, flat bottom.', 'Watch wall case / counter.', 'Place WatchDressSteel at the same transform (the pair stands flat on y = 0).'),
 'PocketWatchVintage': ('Watch', 'Vintage engraved gold pocket watch with enamel Roman dial and hands (Poly Haven, CC0), lying face up, bow away from the viewer.', 'Antique case, rear office repair tray, open safe.', 'Uses Color/Normal/MetallicSmoothness maps; Glass alpha 0.2.'),
 'GemRoundBrilliant': ('Loose gem', '1 ct round brilliant diamond, 6.5 mm, 57 facets (crown 34.5 deg, pavilion 40.75 deg, table 57%).', 'Gem tray, tweezers, GemParcel; scattered on the floor as evidence.', 'Table up, culet at y = 0.'),
 'GemPrincessCut': ('Loose gem', 'Princess-cut diamond, 5.5 mm square.', 'Gem tray.', 'Table up.'),
 'GemEmeraldCut': ('Loose gem', 'Emerald-cut emerald, 9 x 7 mm step cut with cut corners.', 'Coloured-stone tray.', 'Table up.'),
 'GemOvalSapphire': ('Loose gem', 'Oval brilliant sapphire, 8 x 6 mm.', 'Coloured-stone tray.', 'Table up.'),
 'GemCushionRuby': ('Loose gem', 'Cushion-cut ruby, about 6 mm.', 'Coloured-stone tray.', 'Table up.'),
 'GemPearDiamond': ('Loose gem', 'Pear brilliant diamond, about 8 x 6 mm.', 'Gem tray.', 'Table up.'),
 'GemParcel': ('Loose gem', 'Opened diamond paper (white outside, blue liner, printed lot label) with 12 loose brilliants of 2.6-4.2 mm resting on the liner (table down or tipped on the pavilion).', 'Rear office desk, open safe, counter (abandoned by the burglars).', 'Lying flat.'),
 'RingCone': ('Display', 'Black velvet ring finger/cone on a walnut disc, 90 mm tall.', 'Central cases; rings slide on with axis vertical.', 'Ring hole centre for a 17.1 mm ring at y = 0.0665 m.'),
 'EarringStand': ('Display', 'Polished brass T-bar earring stand, 128 mm tall, 130 mm bar.', 'Central case, side islands.', 'Bar axis X at y = 0.124 m.'),
 'RingBoxOpen': ('Packaging', 'Navy velvet ring box, 52 x 48 x 42 mm, lid open 105 deg on a gold hinge pin along the back seam (square seam edges, so the halves close flush), cream satin lining with two rolls (ring slot along X).', 'Counter; an empty open box next to a broken bay is good evidence dressing.', 'Ring hole centre about (0, 0.034, 0) with the ring axis along Z (shank in the slot between the rolls).'),
 'RingBoxClosed': ('Packaging', 'The same ring box closed (flush seam, gold hinge pin at the back).', 'Drawers, safe shelves, counter.', ''),
 'PriceTag01': ('Price', 'Jewellery string tag (30 x 13 mm card, cotton loop) printed "18K Diamond Solitaire £4,950".', 'Tied to rings/necklaces in trays.', 'Shares PriceTags_Color.png (atlas cell 0).'),
 'PriceTag02': ('Price', 'String tag "Platinum Eternity £3,200".', 'Bridal tray.', 'Atlas cell 1.'),
 'PriceTag03': ('Price', 'String tag "Sapphire Halo £3,900".', 'Coloured-stone tray.', 'Atlas cell 6.'),
 'PriceTag04': ('Price', 'String tag "Diamond Studs 1.0ct £2,600".', 'Earrings.', 'Atlas cell 3.'),
 'PriceStand01': ('Price', 'Clear acrylic price stand (60 mm) with printed card "Akoya Pearl Strand £1,850".', 'Next to NecklaceBust.', 'Atlas cell 2.'),
 'PriceStand02': ('Price', 'Acrylic price stand "Tennis Bracelet 3ct £7,400".', 'Bracelet tray.', 'Atlas cell 4.'),
 'PriceStand03': ('Price', 'Acrylic price stand "Dress Watch 39mm £5,750".', 'Watch case.', 'Atlas cell 5.'),
 'PriceStand04': ('Price', 'Acrylic price stand "Fine Gold 100 g £6,120".', 'Bullion shelf / office.', 'Atlas cell 7.'),
 'GiftBag': ('Packaging', 'Matte black paper gift bag 150 x 190 x 80 mm with gusseted sides, cream inside, rope handles and gold-foil Aurel & Finch print (fictional shop).', 'Behind the counter; tipped over on the floor.', ''),
 'GiftBox': ('Packaging', 'Black card gift box 85 x 85 x 40 mm with champagne satin ribbon and bow.', 'Counter, wrapping station.', ''),
 'GoldBar1kg': ('Bullion', '1 kg cast fine-gold bar, 116 x 52 x 9.5 mm (draft angle, rounded edges, orange-peel cast surface, stamped fictional assayer marks in the normal map).', 'Open safes in the rear office (some left behind).', 'Assign GoldBar1kg_Normal.png as the normal map.'),
 'GoldBar100g': ('Bullion', '100 g minted fine-gold bar, 55 x 31 x 3.1 mm with struck frame and text.', 'Safe shelves, display tray.', 'Assign GoldBar100g_Normal.png.'),
 'GoldCoin': ('Bullion', '1 oz fine-gold bullion coin, 32.7 x 2.9 mm, raised rim; original design (sunburst obverse, laurel reverse, 2026).', 'Safe trays, coin tubes, scattered on the office floor.', 'Assign GoldCoin_Normal.png. Obverse up.'),
 'JewelersLoupe': ('Tool', '10x triplet folding loupe (21 mm lens): black barrel, glass, chrome swing-out case opened 160 deg.', 'Counter mat, rear office desk.', 'Glass alpha 0.2.'),
 'PaymentTerminal': ('Counter', 'Countertop card payment terminal on its charging base: keypad toward the customer (+Z), screen showing "£4,950.00", receipt roll and paper at the back.', 'Main counter by the till.', 'TerminalTop texture can also be the emission map (dim screen glow at night).'),
 'PosTablet': ('Counter', 'Till tablet (250 x 172 mm) on a brushed-steel swivel stand, screen faces +Z showing a sale.', 'Main counter (screen toward the staff side).', 'PosTablet_Screen.png as emission for a lit screen.'),
}

files = sorted(f for f in os.listdir(P) if os.path.isfile(os.path.join(P, f)) and f.split('.')[-1] in ('obj', 'mtl', 'png', 'jpg'))
src_files = sorted('Source~/' + f for f in os.listdir(os.path.join(P, 'Source~')))
def sha(p): return hashlib.sha256(open(os.path.join(P, p), 'rb').read()).hexdigest()

models = {}
for name, d in man['models'].items(): models[name] = dict(d, origin='procedural')
for name, d in post.items():
    models[name] = dict(tris=d['tris'], size_m=d['size_m'], origin='third-party')
order = list(DESC.keys())
assert set(order) == set(models), (set(order) ^ set(models))

def owner(f):
    base = f.split('.')[0].split('_')[0]
    for n in SK:
        if f.startswith(n): return n
    if f.startswith('PocketWatchVintage'): return 'PocketWatchVintage'
    return 'procedural'

sources = []
for f in files + src_files:
    o = owner(os.path.basename(f))
    if o == 'PocketWatchVintage':
        e = dict(file=f, source=PH['source'], fetched_from=PH['fetched_from'], commit=PH['commit'], license='CC0-1.0', license_url=PH['license_url'], author=PH['author'],
                 modified=True)
    elif o in SK:
        s = SK[o]; e = dict(file=f, source=s['source'], fetched_from=s['fetched_from'], commit=s['commit'], license=CCBY, license_url=CCBY_URL, author=s['author'],
                            title=s['title'], modified=True)
    else:
        e = dict(file=f, source='original (Source~/build.py, Source~/textures.py)', commit='n/a (generated %s)' % DATE, license='project-owned (original work)', author='Jewelry-store-vr-demo project (generated with Claude)')
    if f.endswith('Preview.jpg'): e['license'] = 'project-owned contact sheet; it also shows renders of PocketWatchVintage (CC0) and RingAntiqueTigerEye, EarringPearlDrop, EarringGoldLeaf (CC BY 4.0, attribution in LICENSE.txt)'
    e['sha256'] = sha(f); e['bytes'] = os.path.getsize(os.path.join(P, f))
    sources.append(e)
json.dump(dict(pack='JewelryModels', generated=DATE, files=sources), open(os.path.join(P, 'sources.json'), 'w'), indent=1)

# models.json
out = {}
for n in order:
    m = models[n]; c, what, place, notes = DESC[n]
    mtlf = os.path.join(P, n + '.mtl'); mats = [l.split()[1] for l in open(mtlf) if l.startswith('newmtl')]
    texs = sorted({l.split()[1] for l in open(mtlf) if l.startswith('map_Kd')})
    extra = sorted(f for f in files if f.startswith(n + '_') and f not in texs)
    out[n] = dict(file=n + '.obj', category=c, origin=m['origin'], triangles=m['tris'], size_m=m['size_m'], materials=mats, color_maps=texs,
                  extra_maps=extra, placement=place, notes=notes)
json.dump(out, open(os.path.join(P, 'models.json'), 'w'), indent=1)

# materials.json (Standard shader recommendations)
used = sorted({m for v in out.values() for m in v['materials']})
allm = man['materials']
allm.setdefault('PocketWatchVintage', dict(Kd=(1, 1, 1), metallic=1.0, smoothness=0.7, note='textured: PocketWatchVintage_Color.jpg + _Normal.png + _MetallicSmoothness.png'))
allm.setdefault('RingAntiqueTigerEye', dict(Kd=(1, 1, 1), metallic=1.0, smoothness=0.7, note='textured: RingAntiqueTigerEye_Color.jpg + _Normal.png + _MetallicSmoothness.png'))
allm.setdefault('EarringPearlDrop', dict(Kd=(1, 1, 1), metallic=1.0, smoothness=0.7, note='textured: EarringPearlDrop_Color.jpg + _Normal.png + _MetallicSmoothness.png'))
mj = {}
for m in used:
    d = allm[m]
    mode = d.get('mode', 'Transparent' if d.get('d', 1) < 1 else 'Opaque')
    rgb = srgb(d['Kd'])
    mj[m] = dict(albedo_srgb=rgb, albedo_hex='#%02X%02X%02X' % tuple(int(round(x * 255)) for x in rgb), alpha=d.get('d', 1.0),
                 metallic=d.get('metallic', 0.0), smoothness=d.get('smoothness', 0.5), rendering_mode=mode, emission=d.get('emission', 0.0), note=d.get('note', ''))
json.dump(dict(shader='Built-in Standard (metallic workflow)', colour_space_note='albedo values are sRGB colours as typed into the Unity colour picker', materials=mj),
          open(os.path.join(P, 'materials.json'), 'w'), indent=1)
json.dump(dict(models=out), open(os.path.join(W, 'docs_models.json'), 'w'))
print('files', len(files), 'source files', len(src_files), 'models', len(out), 'materials', len(mj))
d = json.load(open(os.path.join(P, 'materials.json'))); M = d['materials']
for n in ('PocketWatchVintage', 'RingAntiqueTigerEye', 'EarringPearlDrop'):
    M[n].update(albedo_map=n + '_Color.jpg', normal_map=n + '_Normal.png', metallic_smoothness_map=n + '_MetallicSmoothness.png', metallic=1.0, smoothness=1.0,
                note='metallic from the R channel and smoothness from the alpha of the metallic-smoothness map (Standard: Source = Metallic Alpha, smoothness scale 1)')
M['EarringPearlDrop']['note'] += '; the map gives the gold metallic 1 and the pearls 0, smoothness 0.71 everywhere (without the map use metallic 1, smoothness 0.71)'
for n in ('GoldBar1kg', 'GoldBar100g', 'GoldCoin'):
    M[n].update(albedo_map=n + '_Color.png', normal_map=n + '_Normal.png')
for n, f in (('WatchDial', 'WatchDial_Color.png'), ('PriceTag', 'PriceTags_Color.png'), ('GiftBag', 'GiftBag_Color.png'), ('TerminalTop', 'PaymentTerminal_Color.png'), ('PosScreen', 'PosTablet_Screen.png'), ('GemPaper', 'GemPaper_Color.png')):
    M[n]['albedo_map'] = f
for n in ('TerminalTop', 'PosScreen'): M[n]['emission_map'] = M[n]['albedo_map']
json.dump(d, open(os.path.join(P, 'materials.json'), 'w'), indent=1)
