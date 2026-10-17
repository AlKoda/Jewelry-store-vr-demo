import json, hashlib, os, glob, sys
W = sys.argv[1]; P = '/home/user/Jewelry-store-vr-demo/Prototype/ThirdParty/AmbientProps2'
st = {**json.load(open(W + '/stat1.json')), **json.load(open(W + '/stat2.json'))}
sha = lambda f: hashlib.sha256(open(f, 'rb').read()).hexdigest()
GE = ('https://github.com/Geadas11/Claude_steam_game', '8b6aa56f951450b7a05434615bcdda2bd2d1e8fa', 'assets/3d/models/{id}/')
DX = ('https://github.com/Hona/daxship', 'd63856c6af9aba445a4fe8bf832d0e0ecf2572e5', 'apps/web/public/assets/models/environment/poly-haven/{id}/')
GD = ('https://github.com/bumbeishvili/green-diamond', '4d0fc086f71d89040c6a9355b782fea4de7d6e7c', 'assets/models/{id}')
JRA = ('https://github.com/series-ai/jam-ready-assets', '2d7c41049c1db4d670b1ded88a7aeec0d7c2f38c', '{id}')
RQ = ('https://github.com/RetroDECK/RetroQUEST', 'dfa19a5602a31f64bd890d15279a61f43b127328', 'assets/kenney_furniture-kit/Models/GLTF format/{id}')
DK = ('https://github.com/ssebs/DankNooner', '2d0f7af27726a6f58aa486f7940267f8231339c9', 'v1/assets/vehicles/{id}')
UNK = 'not recorded in a reachable mirror (see the Poly Haven page)'
PH = [  # name, id, title, authors, mirror, use
 ('CashRegisterVintage', 'CashRegister_01', 'Cash Register 01', 'Joe Seabuhr', GE, 'Vintage brass till; back counter / display piece (the working POS is JewelryModels/PosTablet + PaymentTerminal).'),
 ('StreetPlanterBox', 'planter_box_01', 'Planter Box 01', 'James Ray Cock', GE, 'Potted planter by the shop entrance / sidewalk.'),
 ('WaterBottleGallon', 'plastic_bottle_gallon', 'Plastic Bottle Gallon', 'Rahul Chaudhary', GE, 'Plastic drink/water bottle on desks, back room (0.27 m).'),
 ('FoodPackets', 'long_life_food', 'Long Life Food', 'Mia Pecina Zorko', GE, 'Snack/food packets on the staff-room shelf or a desk.'),
 ('PlasticTub', 'plastic_container', 'Plastic Container', UNK, GE, 'Storage tub in the back room.'),
 ('DisplayShelvesWood', 'wooden_display_shelves_01', 'Wooden Display Shelves 01', 'James Ray Cock', GE, 'Shop shelving (1.56 m) for the sales floor or back office.'),
 ('Broom', 'plastic_broom', 'Plastic Broom', UNK, GE, 'Leaning in the back room / by the staff door. 9.7k tris (bristles kept at source detail).'),
 ('Dustpan', 'dustpan', 'Dustpan', UNK, GE, 'With the broom.'),
 ('CigarettePack', 'cigarette_pack', 'Cigarette Pack', UNK, GE, 'Street/alley litter, staff smoking spot.'),
 ('FluorescentBatten', 'mounted_fluorescent_lights', 'Mounted Fluorescent Lights', 'Ulan Cabanilla', GE, 'Ceiling batten for back room / office (pivot: top at y = 0, hangs down).'),
 ('MetalStool', 'metal_stool_01', 'Metal Stool 01', UNK, GE, 'Workbench / back-office stool.'),
 ('TeaSetMugs', 'tea_set_01', 'Tea Set 01', 'Rico Cilliers (model), James Ray Cock (texturing), Jurita Burger (graphic design)', GE, 'Mugs/cups on a tray in the staff room (split by connected parts for single cups).'),
 ('ConcreteRoadBarrier', 'concrete_road_barrier', 'Concrete Road Barrier', 'Amal Kumar', GE, 'Street edge / closed-off lane (1.55 m).'),
 ('SidewalkChalkboard', 'standing_chalkboard_01', 'Standing Chalkboard 01', 'ParzivalCG', GE, 'A-board shop sign on the pavement.'),
 ('SuitcaseVintage', 'vintage_suitcase', 'Vintage Suitcase', 'Maximilian Schuster', GE, 'Two suitcases (side by side); lost property / office.'),
 ('MonoblocChair', 'plastic_monobloc_chair_01', 'Plastic Monobloc Chair 01', 'Kuutti Siitonen', GE, 'Staff smoking spot / back yard.'),
 ('PlasticJug', 'jug_01', 'Jug 01', 'Kuutti Siitonen', GE, 'Staff room counter.'),
 ('WornMetalRack', 'worn_metal_rack', 'Worn Metal Rack', 'Luca B', GE, 'Back-room stock shelving (1.9 m).'),
 ('CleanerSprayBottle', 'multi_cleaner_bottle', 'Multi Cleaner Bottle', 'James Ray Cock', GE, 'Glass cleaner by the counter / cleaning cupboard.'),
 ('LaptopClassic', 'classic_laptop', 'Classic Laptop', 'Arrangemonk', GE, 'Office desk (older laptop stands in for the missing PC tower).'),
 ('WickerBasket', 'wicker_basket_02', 'Wicker Basket 02', UNK, GE, 'Shop decoration / staff room.'),
 ('ToolCart', 'tool_cart', 'Tool Cart', 'Savva Zakharov', GE, 'Maintenance cart in the back room. 8k tris.'),
 ('JerryCan', 'metal_jerrycan', 'Metal Jerrycan', 'Sean Buckley', GE, 'Back yard / alley.'),
 ('AirDuctKit', 'modular_airduct_circular_01', 'Modular Airduct Circular 01', 'Riley Queen', DX, 'Ceiling duct kit: 16 pieces (straights, bends, braces, fan, rectangular converter) laid out side by side in one OBJ; split by connected parts. 10k tris for the whole kit.'),
]
LP = [  # name, src rel, title, author, licence, mirror, page, scale, use
 ('ParkedSUV', 'cars/car_suv.glb', 'SUV / crossover', 'Quaternius', 'CC0-1.0', GD, 'https://poly.pizza/m/xsMtZhBkxL', 1, 'Parked civilian car (4.2 m), flat colours.'),
 ('ParkedPickup', 'cars/car_pickup.glb', 'Pickup truck', 'Quaternius', 'CC0-1.0', GD, 'https://poly.pizza/m/qn4grQgHm8', 1, 'Parked pickup (5.2 m); 6.4k tris.'),
 ('ParkedVan', 'cars/car_van.glb', 'Panel van', 'PuKkBuMXDD', 'CC-BY-3.0', GD, 'https://poly.pizza/m/BbRojf2v3H', 0.75, 'Parked panel van (4.3 m, long axis X), flat colours.'),
 ('StreetTreeMaple', 'nature/tree_maple_red.glb', 'Red maple (MapleTree_2)', 'Quaternius', 'CC0-1.0', GD, 'https://poly.pizza/m/iGFtQd0PJO', 1, 'Street tree with leaves (6.7 m); scale 0.7 for a young street tree.'),
 ('StreetTreeYoung', 'nature/tree_young_4.glb', 'Young broadleaf tree 4 (NormalTree_4)', 'Quaternius', 'CC0-1.0', GD, 'https://poly.pizza/m/etFGNvsiFv', 1, 'Street tree (5.1 m); decimated 6,338 -> 4,999 tris with tools/glb_to_obj.py --target-tris (UVs resampled: check leaves).'),
 ('FloweringBush', 'nature/bush_flowering.glb', 'Bush with flowers', 'Quaternius', 'CC0-1.0', GD, 'https://poly.pizza/m/U1ymDy8tbY', 1, 'Planter filler / tree pit.'),
 ('ShoppingCart', 'props/shopping_cart.glb', 'Shopping cart', 'Poly by Google', 'CC-BY-3.0', GD, 'https://poly.pizza/m/4lhRcMbl1pw', 1, 'Abandoned trolley on the street.'),
 ('ParkLitterBin', 'props/trash_bin_park.glb', 'Park litter bin', 'Isa Lousberg', 'CC0-1.0', GD, 'https://poly.pizza/m/2UXIdGYkg9', 0.8, 'Pavement litter bin.'),
 ('ParkedScooter', 'Scooter01.glb', 'Scooter', 'Poly by Google', 'CC-BY-3.0', DK, 'https://poly.pizza/m/9RqkkBOPNtd', 0.0024, 'Parked moped/scooter on the pavement (1.8 m). Licence evidence: v1/attributions.md of the mirror only.'),
]
KF = 'kenney-food-kit/3D/food/Models/GLB format/'; KM = 'kenney-mini-market/3D/city/Models/GLB format/'
KN = [  # name, mirror, rel, kit, scale, use
 ('MugStylised', JRA, KF + 'mug.glb', 'Food Kit', 0.35, 'Mug on desks / counter.'),
 ('CupCoffeeKenney', JRA, KF + 'cup-coffee.glb', 'Food Kit', 0.35, 'Coffee cup (Kenney "cup-coffee"; 0.05 m tall at this scale: check the shape and rescale).'),
 ('TeaCup', JRA, KF + 'cup-tea.glb', 'Food Kit', 0.35, 'Cup on a desk.'),
 ('DrinkCan', JRA, KF + 'soda-can.glb', 'Food Kit', 0.35, 'Drink can (0.12 m).'),
 ('DrinkCanCrushed', JRA, KF + 'soda-can-crushed.glb', 'Food Kit', 0.35, 'Crushed can litter in gutter / alley.'),
 ('PlasticSodaBottle', JRA, KF + 'soda-bottle.glb', 'Food Kit', 0.35, 'Plastic drink bottle (0.2 m).'),
 ('GlassBottle', JRA, KF + 'bottle-oil.glb', 'Food Kit', 0.35, 'Glass bottle (Kenney oil bottle, 0.22 m) as litter / staff room.'),
 ('PizzaBox', JRA, KF + 'pizza-box.glb', 'Food Kit', 0.35, 'Takeaway pizza box on the office desk or by the bins.'),
 ('NoodleBox', JRA, KF + 'chinese.glb', 'Food Kit', 0.35, 'Takeaway noodle box.'),
 ('TakeawayClamshell', JRA, KF + 'styrofoam-dinner.glb', 'Food Kit', 0.35, 'Open foam takeaway box with food.'),
 ('TakeawayClamshellEmpty', JRA, KF + 'styrofoam.glb', 'Food Kit', 0.35, 'Open empty foam takeaway box (litter).'),
 ('DrinkCarton', JRA, KF + 'carton.glb', 'Food Kit', 0.35, 'Drink carton.'),
 ('BagFlat', JRA, KF + 'bag-flat.glb', 'Food Kit', 0.35, 'Flattened bag (litter).'),
 ('PaperBag', JRA, KF + 'bag.glb', 'Food Kit', 0.6, 'Paper shopping/takeaway bag (0.36 m).'),
 ('ShopShelfBags', JRA, KM + 'shelf-bags.glb', 'Mini Market', 2.0, 'Stocked shop shelf (stylised; 1.4 m deep at this scale, consider 1.4-1.6).'),
 ('ShopShelfBoxes', JRA, KM + 'shelf-boxes.glb', 'Mini Market', 2.0, 'Stocked shop shelf with boxes.'),
 ('ShoppingBasket', JRA, KM + 'shopping-basket.glb', 'Mini Market', 1.3, 'Hand basket.'),
 ('BottleReturnMachine', JRA, KM + 'bottle-return.glb', 'Mini Market', 1.65, 'Vending/bottle-return machine (1.8 m) for a street front.'),
 ('CoatRackStanding', RQ, 'coatRackStanding.glb', 'Furniture Kit', 2.35, 'Coat stand in the office (1.8 m), flat colours.'),
 ('CoatRackWall', RQ, 'coatRack.glb', 'Furniture Kit', 2.35, 'Wall hook rail in the staff room.'),
 ('DoorMat', RQ, 'rugDoormat.glb', 'Furniture Kit', 1.75, 'Door mat at the entrance / staff door (0.75 x 0.42 m).'),
 ('RugRectangle', RQ, 'rugRectangle.glb', 'Furniture Kit', 1.75, 'Rectangular rug (2.75 x 1.6 m) for the office.'),
 ('RugRound', RQ, 'rugRound.glb', 'Furniture Kit', 1.75, 'Round rug (1.6 m).'),
]
SRC = W + '/src/'
def local(m, rel):
    root = {GE[0]: SRC + 'geadas/', DX[0]: SRC + 'daxship/', GD[0]: SRC + 'gd/', JRA[0]: SRC + 'jra/', RQ[0]: SRC + 'rq/assets/kenney_furniture-kit/Models/GLTF format/', DK[0]: SRC + 'dank/v1/assets/vehicles/'}[m[0]]
    return root
models = {}
for name, pid, title, auth, m, use in PH:
    d = SRC + ('geadas/' + GE[2] if m is GE else 'daxship/' + DX[2]).format(id=pid)
    files = sorted(os.path.relpath(os.path.join(r, f), d) for r, _, fs in os.walk(d) for f in fs if not f.endswith('.import'))
    models[name] = {'title': title, 'provider': 'Poly Haven', 'authors': auth, 'page': 'https://polyhaven.com/a/' + pid, 'license': 'CC0-1.0',
        'license_url': 'https://creativecommons.org/publicdomain/zero/1.0/', 'mirror': m[0], 'mirror_commit': m[1], 'mirror_path': m[2].format(id=pid),
        'source_files_sha256': {f: sha(os.path.join(d, f)) for f in files}, 'conversion': 'Source~/build_ph2.py -> InteriorModels/Source~/convert_ph.py (Color / Normal / MetallicSmoothness maps kept)' + ('; GLB unpacked first with Source~/unglb.py' if pid in ('tea_set_01', 'concrete_road_barrier') else ''), 'use': use, **st[name]}
for name, rel, title, auth, lic, m, page, sc, use in LP:
    f = SRC + 'gd/' + os.path.basename(rel) if m is GD else SRC + 'dank/v1/assets/vehicles/' + rel
    models[name] = {'title': title, 'provider': 'Poly Pizza', 'authors': auth, 'page': page, 'license': lic,
        'license_url': 'https://creativecommons.org/publicdomain/zero/1.0/' if lic == 'CC0-1.0' else 'https://creativecommons.org/licenses/by/3.0/',
        'mirror': m[0], 'mirror_commit': m[1], 'mirror_path': m[2].format(id=rel), 'source_files_sha256': {os.path.basename(rel): sha(f)},
        'conversion': 'Tools/glb_to_obj.py --ground --scale %g%s' % (sc, ' --target-tris 5000' if name == 'StreetTreeYoung' else ''), 'use': use, **st[name]}
for name, m, rel, kit, sc, use in KN:
    f = (SRC + 'jra/' + rel) if m is JRA else SRC + 'rq/assets/kenney_furniture-kit/Models/GLTF format/' + rel
    models[name] = {'title': 'Kenney %s: %s' % (kit, os.path.basename(rel)[:-4]), 'provider': 'Kenney', 'authors': 'Kenney (www.kenney.nl)', 'page': 'https://kenney.nl/assets/' + kit.lower().replace(' ', '-'),
        'license': 'CC0-1.0', 'license_url': 'https://creativecommons.org/publicdomain/zero/1.0/', 'mirror': m[0], 'mirror_commit': m[1],
        'mirror_path': m[2].format(id=rel), 'source_files_sha256': {os.path.basename(rel): sha(f)},
        'conversion': 'Tools/glb_to_obj.py --ground --scale %g (Kenney colormap atlas copied per model as <Name>_colormap.png)' % sc + ('; LFS object fetched from media.githubusercontent.com' if m is JRA else ''), 'use': use, **st[name]}
lic_ev = {
 'Poly Haven': 'CC0 per https://polyhaven.com/license; ids and the CC0 statement also in the mirror Geadas11/Claude_steam_game assets/3d/CREDITS.md and Hona/daxship; authors from the Poly Haven API listing (Akshay25798/VFX-Houdini-Asset_Browser downloads/json/models.json) or the credits files of ReidT205/blacksite-assets ATTRIBUTION.md and m4tthewdavis/fallout-minnesota assets/CREDITS.md. Six authors were not found in a reachable mirror; CC0 needs no attribution.',
 'Kenney': 'CC0 1.0 per the License.txt shipped in each kit (kenney-food-kit/3D/food/License.txt, kenney-mini-market/3D/city/License.txt, kenney_furniture-kit/License.txt), copied to Source~/licenses/.',
 'Poly Pizza (green-diamond)': 'Licence and author per item in bumbeishvili/green-diamond assets/models/manifest.json and CREDITS.md (CC0 for Quaternius / Isa Lousberg; CC BY 3.0 for PuKkBuMXDD and Poly by Google). poly.pizza is not reachable from the build container.',
 'Poly Pizza (DankNooner)': 'Scooter: ssebs/DankNooner v1/attributions.md, "Scooter by Poly by Google [CC-BY] (https://creativecommons.org/licenses/by/3.0/) via Poly Pizza (https://poly.pizza/m/9RqkkBOPNtd)". Single-source evidence; confirm on poly.pizza before release.'}
tot = sum(v['tris'] for v in models.values())
size = sum(os.path.getsize(f) for f in glob.glob(P + '/*') if os.path.isfile(f))
json.dump({'pack': 'AmbientProps2', 'prepared': '2026-10-17', 'unverified_in_unity': True,
  'units': 'metres, Y up, ground at y = 0, centred in x/z (FluorescentBatten: ceiling pivot, top at y = 0)',
  'texture_convention': {'Color': 'sRGB JPG/PNG (map_Kd)', 'Normal': '_Normal.jpg, tangent-space OpenGL +Y, named in the MTL as a "# Normal <file>" comment line', 'MetallicSmoothness': '_MetallicSmoothness.png RGBA, R = G = B = metallic, A = smoothness, "# MetallicSmoothness <file>" comment line'},
  'license_evidence': lic_ev, 'total_triangles': tot, 'pack_bytes': size, 'models': models}, open(P + '/sources.json', 'w'), indent=1)
print(tot, size, len(models))
