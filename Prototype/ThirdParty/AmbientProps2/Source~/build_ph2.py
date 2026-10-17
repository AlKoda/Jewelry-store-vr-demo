import sys, os, json, subprocess
CONV = '/home/user/Jewelry-store-vr-demo/Prototype/ThirdParty/InteriorModels/Source~/convert_ph.py'
W = sys.argv[1]; OUT = W + '/out'; REP = W + '/ph_report.json'
GE = W + '/src/geadas/assets/3d/models'; UN = W + '/unp'; DX = W + '/src/daxship/apps/web/public/assets/models/environment/poly-haven'
G = lambda d: [0.92, 0.94, 0.95, d]
B = {'preserve_border': True}
S = {
 'CashRegisterVintage': (GE, 'CashRegister_01', {'tris': 5000, 'seam_aware': ['CashRegister_01'], 'short': {'CashRegister_01': 'Register', 'CashRegister_01_glass': 'Glass'}, 'glass': {'CashRegister_01_glass': G(0.2)}}),
 'StreetPlanterBox': (GE, 'planter_box_01', {'tris': 5000, 'dec': B, 'short': {'planter_box_01': 'Planter'}}),
 'WaterBottleGallon': (GE, 'plastic_bottle_gallon', {'tris': 4000, 'dec': B, 'short': {'plastic_bottle_gallon': 'Bottle'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'FoodPackets': (GE, 'long_life_food', {'tris': 4712, 'short': {'long_life_food': 'Packets'}, 'color': 1024, 'normal': 512, 'ms': 256}),
 'PlasticTub': (GE, 'plastic_container', {'short': {'plastic_container': 'Plastic'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'DisplayShelvesWood': (GE, 'wooden_display_shelves_01', {'short': {'wooden_display_shelves_01': 'Wood'}}),
 'Broom': (GE, 'plastic_broom', {'tris': 5000, 'seam_aware': ['plastic_broom', 'plastic_broom_bristles'], 'short': {'plastic_broom': 'Broom', 'plastic_broom_bristles': 'Bristles'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'Dustpan': (GE, 'dustpan', {'short': {'dustpan': 'Dustpan'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'CigarettePack': (GE, 'cigarette_pack', {'tris': 2000, 'seam_aware': ['cigarette_pack'], 'short': {'cigarette_pack': 'Pack'}, 'color': 512, 'normal': 256, 'ms': 256}),
 'FluorescentBatten': (GE, 'mounted_fluorescent_lights', {'pivot': 'ceiling', 'tris': 5000, 'seam_aware': ['mounted_fluorescent_lights', 'mounted_fluorescent_lights_glass'], 'short': {'mounted_fluorescent_lights': 'Fitting', 'mounted_fluorescent_lights_glass': 'Tube'}}),
 'MetalStool': (GE, 'metal_stool_01', {'tris': 5000, 'dec': B, 'short': {'metal_stool_01': 'Stool'}}),
 'TeaSetMugs': (UN, 'tea_set_01', {'tris': 5000, 'dec': B, 'short': {'tea_set_01': 'Ceramic'}}),
 'ConcreteRoadBarrier': (UN, 'concrete_road_barrier', {'tris': 5000, 'dec': B, 'short': {'concrete_road_barrier': 'Concrete'}}),
 'SidewalkChalkboard': (GE, 'standing_chalkboard_01', {'short': {'standing_chalkboard_01_frame': 'Frame', 'standing_chalkboard_01_board': 'Board'}}),
 'SuitcaseVintage': (GE, 'vintage_suitcase', {'tris': 5000, 'seam_aware': ['vintage_suitcase_01', 'vintage_suitcase_02'], 'short': {'vintage_suitcase_01': 'CaseA', 'vintage_suitcase_02': 'CaseB'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'MonoblocChair': (GE, 'plastic_monobloc_chair_01', {'short': {'plastic_monobloc_chair_01': 'Plastic'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'PlasticJug': (GE, 'jug_01', {'tris': 4000, 'dec': B, 'short': {'jug_01': 'Jug'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'WornMetalRack': (GE, 'worn_metal_rack', {'short': {'worn_metal_rack': 'Rack'}}),
 'CleanerSprayBottle': (GE, 'multi_cleaner_bottle', {'tris': 4000, 'dec': B, 'short': {'multi_cleaner_bottle': 'Bottle'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'LaptopClassic': (GE, 'classic_laptop', {'tris': 5000, 'seam_aware': ['classic_laptop', 'classic_laptop_screen'], 'short': {'classic_laptop': 'Laptop', 'classic_laptop_screen': 'Screen'}, 'color': 1024, 'normal': 512, 'ms': 256}),
 'WickerBasket': (GE, 'wicker_basket_02', {'tris': 5000, 'dec': B, 'short': {'wicker_basket_02': 'Wicker'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'ToolCart': (GE, 'tool_cart', {'tris': 8000, 'seam_aware': ['tool_cart'], 'short': {'tool_cart': 'Cart'}}),
 'JerryCan': (GE, 'metal_jerrycan', {'tris': 4000, 'dec': B, 'short': {'metal_jerrycan': 'Can'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'AirDuctKit': (DX, 'modular_airduct_circular_01', {'tris': 10000, 'dec': B, 'short': {'modular_airduct_circular_01': 'Duct'}}),
}
only = set(sys.argv[2:])
for name, (root, mid, spec) in S.items():
    if only and name not in only: continue
    spec = dict(spec); spec['report'] = REP
    spec.setdefault('color', 1024); spec.setdefault('normal', 1024); spec.setdefault('ms', 512)
    d = os.path.join(root, mid); src = [os.path.join(d, f) for f in os.listdir(d) if f.endswith('.gltf')][0]
    r = subprocess.run(['python3', '-P', CONV, src, OUT, name, '--spec', json.dumps(spec)], capture_output=True, text=True,
                       env=dict(os.environ, PYTHONPATH=W + '/../pylib'), cwd=W + '/tools')
    print(name, r.returncode, r.stdout.strip()[:200], r.stderr.strip()[-600:] if r.returncode else '')
