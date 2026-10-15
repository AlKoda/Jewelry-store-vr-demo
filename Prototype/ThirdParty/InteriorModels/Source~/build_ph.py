"""Build the Poly Haven part of InteriorModels: build_ph.py MIRROR_MODELS EDITED_SRC OUT REPORT [names...]"""
import sys, os, json, subprocess
MIR, EDT, OUT, REP = sys.argv[1:5]
ONLY = set(sys.argv[5:])
HERE = os.path.dirname(os.path.abspath(__file__))
WORK = os.path.dirname(EDT.rstrip('/'))
G = lambda d: [0.92, 0.94, 0.95, d]
# October 15: every decimated model keeps its UV seams closed: 'dec': preserve_border (seams and open borders stay
# fixed; the triangle count then stops where the seams allow) or 'seam_aware' (welded decimation re-split per UV island,
# for the desk lamp whose thin rods are seams everywhere). Plain UV-exact decimation opened cracks along the seams.
SPECS = {
 'LoungeChairMidCentury': ('mid_century_lounge_chair', {'short': {'mid_century_lounge_chair': 'Chair'}}),
 'ArmChairModern': ('modern_arm_chair_01', {'short': {'modern_arm_chair_01_legs': 'Frame', 'modern_arm_chair_01_pillow': 'Cushions'},
     'color_px': {'modern_arm_chair_01_legs': 512}, 'normal_px': {'modern_arm_chair_01_legs': 512}, 'ms_px': {'modern_arm_chair_01_legs': 256}}),
 'OttomanLeather': ('Ottoman_01', {'short': {'Ottoman_01': 'Leather'}}),
 'CoffeeTableConcrete': ('modern_coffee_table_01', {'short': {'modern_coffee_table_01': 'Table'}}),
 # Plants (rebuilt October 15): pot shell kept at source detail (only faces sealed under the gravel are dropped),
 # pebble layer baked onto a heightfield disc (bake_gravel.py), leaves two-sided. Tall plant: leaf blades as
 # alpha-cut cards and stalks as low-poly tubes (leaf_cards.py), trunk decimated with welded seams.
 'PottedPlantTall': ('potted_plant_01', {'tris': 15000, 'dec': {'agg': 3, 'preserve_border': True},
     'short': {'potted_plant_01_leaves': 'Leaves', 'potted_plant_01_pot': 'Pot'},
     'per_node': {'potted_plant_01_pot': 0, 'potted_plant_01_stem': {'tris': 1650, 'weld': True}},
     'gravel': {'potted_plant_01_pebbles': {'name': 'Gravel', 'shell': 'potted_plant_01_pot', 'rings': 7, 'segs': 40}},
     'leaf_cards': {'potted_plant_01_leaves': {'rest_uv_boxes': [[0.85, 1.01, -0.01, 1.01], [0.42, 1.01, -0.01, 0.105]],
                    'k_big': 7, 'in_big': 3, 'k_small': 4, 'tubes': {'sides': 5, 'segs': 5}, 'mask_px': 1024}},
     'color_px': {'potted_plant_01_pot': 1024}, 'normal_px': {'potted_plant_01_pot': 1024}, 'ms': 256}),
 'PottedPlantArrowhead': ('potted_plant_02', {'tris': 15000, 'dec': {'agg': 3, 'preserve_border': True},
     'short': {'potted_plant_02_leaves': 'Leaves', 'potted_plant_02_pot': 'Pot'},
     'share': {'potted_plant_02_leaves': 5300}, 'per_node': {'potted_plant_02_pot': 0},
     'gravel': {'potted_plant_02_dirt': {'name': 'Gravel', 'shell': 'potted_plant_02_pot', 'rings': 7, 'segs': 40}},
     'backfaces': ['potted_plant_02_leaves'],
     'color_px': {'potted_plant_02_pot': 1024}, 'normal_px': {'potted_plant_02_pot': 1024}, 'ms': 256}),
 'PottedSucculent': ('potted_plant_04', {'tris': 5000, 'dec': {'preserve_border': True}, 'short': {'potted_plant_04': 'Plant'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'WallClock': ('@wall_clock', {'pivot': 'wall', 'short': {'wall_clock': 'Clock', 'wall_clock_glass': 'Glass'},
     'glass': {'wall_clock_glass': G(0.18)}, 'normal': 512}),
 'DeskLampArm': ('desk_lamp_arm_01', {'tris': 15000, 'seam_aware': ['desk_lamp_arm_01', 'desk_lamp_arm_01_light'], 'short': {'desk_lamp_arm_01': 'Lamp', 'desk_lamp_arm_01_light': 'Bulb'}}),
 'StationerySet': ('stationery_supplies', {'short': {'stationery_supplies': 'Stationery'}, 'normal': 512, 'ms': 256}),
 'PendantLampGlobe': ('modern_ceiling_lamp_01', {'pivot': 'ceiling', 'short': {'modern_ceiling_lamp_01': 'Fitting',
     'modern_ceiling_globe': 'Bulb', 'modern_ceiling_lamp_01_glass': 'GlassShade'}, 'glass': {'modern_ceiling_lamp_01_glass': G(0.25)},
     'color': 512, 'normal': 512, 'ms': 256}),
 'FireExtinguisher': ('@korean_fire_extinguisher_01', {'tris': 8000, 'dec': {'preserve_border': True}, 'short': {'korean_fire_extinguisher_01_body': 'Body',
     'korean_fire_extinguisher_01_glass': 'GaugeGlass', 'korean_fire_extinguisher_01_paper': 'Tag'},
     'glass': {'korean_fire_extinguisher_01_glass': G(0.25)}, 'color_px': {'korean_fire_extinguisher_01_paper': 512},
     'normal_px': {'korean_fire_extinguisher_01_paper': 256}, 'ms_px': {'korean_fire_extinguisher_01_paper': 128}}),
 'FireAlarmCallPoint': ('@fire_alarm', {'pivot': 'wall', 'short': {'fire_alarm': 'CallPoint'}, 'normal': 512, 'ms': 256,
     'drop_box': [['fire_alarm', [-0.0252, -0.0723, 0.0212], [0.0252, -0.0613, 0.0240]]]}),
 'PictureFrameBlack': ('hanging_picture_frame_01', {'pivot': 'wall', 'short': {'hanging_picture_frame_01': 'Frame',
     'hanging_picture_frame_01_artwork': 'Artwork', 'hanging_picture_frame_01_glass': 'Glass'},
     'glass': {'hanging_picture_frame_01_glass': G(0.12)}, 'uv_fit': ['hanging_picture_frame_01_artwork'],
     'no_maps': ['hanging_picture_frame_01_artwork'],
     'color_override': {'hanging_picture_frame_01_artwork': os.path.join(WORK, 'art_frame1.png')},
     'color_size': {'hanging_picture_frame_01_artwork': [690, 1024]}, 'color': 512, 'normal': 512, 'ms': 256}),
 # October 15: renamed from PictureFrameSilver (Poly Haven: distressed white wooden frame); the inherited artwork
 # (painterly photo with a third-party signature) is replaced by a public-domain painting (prep_print.py)
 'PictureFrameWhite': ('hanging_picture_frame_02', {'pivot': 'wall', 'short': {'hanging_picture_frame_02': 'Frame',
     'hanging_picture_frame_02_artwork': 'Artwork', 'hanging_picture_frame_02_glass': 'Glass'},
     'glass': {'hanging_picture_frame_02_glass': G(0.12)}, 'uv_fit': ['hanging_picture_frame_02_artwork'],
     'no_maps': ['hanging_picture_frame_02_artwork'],
     'color_override': {'hanging_picture_frame_02_artwork': os.path.join(WORK, 'art_white.png')},
     'color_size': {'hanging_picture_frame_02_artwork': [1280, 750]}, 'color': 512, 'normal': 512, 'ms': 256}),
 'PictureFrameGilt': ('fancy_picture_frame_01', {'pivot': 'wall', 'short': {'fancy_picture_frame_01': 'Frame',
     'fancy_picture_frame_01_canvas': 'Artwork'}, 'uv_fit': ['fancy_picture_frame_01_canvas'],
     'no_maps': ['fancy_picture_frame_01_canvas'], 'color_px': {'fancy_picture_frame_01_canvas': 1024}, 'normal': 512, 'ms': 256}),
 # October 15: yaw -90 so the photo faces +Z like every other model ((x, y, z) -> (-z, y, x))
 'PhotoFrameDesk': ('standing_picture_frame_01', {'yaw': -90, 'short': {'standing_picture_frame_01': 'Frame',
     'standing_picture_frame_01_artwork': 'Artwork', 'standing_picture_frame_01_glass': 'Glass'},
     'glass': {'standing_picture_frame_01_glass': G(0.12)}, 'uv_fit': ['standing_picture_frame_01_artwork'],
     'no_maps': ['standing_picture_frame_01_artwork'], 'color_px': {'standing_picture_frame_01_artwork': 512},
     'color': 512, 'normal': 256, 'ms': 128}),
 'VaseCeramicTall': ('ceramic_vase_03', {'tris': 3000, 'dec': {'preserve_border': True}, 'short': {'ceramic_vase_03': 'Ceramic'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'VaseCeramicBottle': ('ceramic_vase_01', {'tris': 3000, 'dec': {'preserve_border': True}, 'short': {'ceramic_vase_01': 'Ceramic'}, 'color': 512, 'normal': 512, 'ms': 256}),
 'OfficeDeskMetal': ('metal_office_desk', {'short': {'metal_office_desk': 'Desk'}}),
 'CCTVBulletHousing': ('@security_camera_02', {'tris': 7000, 'dec': {'preserve_border': True}, 'short': {'security_camera_02': 'Camera', 'security_camera_02_glass': 'Window'},
     'glass': {'security_camera_02_glass': [0.05, 0.05, 0.06, 0.55]}, 'normal': 1024, 'ms': 512}),
 'ShelvingSteelWide': ('steel_frame_shelves_03', {'short': {'steel_frame_shelves_03': 'Frame', 'steel_frame_shelves_03_drawers': 'Drawers'},
     'color_px': {'steel_frame_shelves_03_drawers': 512}, 'normal_px': {'steel_frame_shelves_03_drawers': 512}, 'ms_px': {'steel_frame_shelves_03_drawers': 256}}),
 'CardboardBox': ('cardboard_box_01', {'tris': 3000, 'dec': {'preserve_border': True}, 'short': {'cardboard_box_01': 'Cardboard'}, 'color': 512, 'normal': 512, 'ms': 256}),
}
procs = []
for name, (mid, spec) in SPECS.items():
    if ONLY and name not in ONLY: continue
    root = EDT if mid.startswith('@') else MIR; mid = mid.lstrip('@')
    spec = dict(spec); spec['report'] = REP
    spec.setdefault('color', 1024); spec.setdefault('normal', 1024); spec.setdefault('ms', 512)
    src = os.path.join(root, mid, mid + '.gltf')
    r = subprocess.run(['python3', '-P', os.path.join(HERE, 'convert_ph.py'), src, OUT, name, '--spec', json.dumps(spec)], capture_output=True, text=True)
    print((r.stdout.strip()[:160]), r.stderr.strip()[-400:] if r.returncode else '')
