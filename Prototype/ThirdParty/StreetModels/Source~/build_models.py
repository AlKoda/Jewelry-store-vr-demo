"""Build StreetModels OBJ/MTL/maps from downloaded glTF sources (scratch -> OUT).
usage: build_models.py OUTDIR [name ...]"""
import sys, os, json, re
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
import trimesh
from PIL import Image, ImageDraw
import conv

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
S = lambda *p: os.path.join(ROOT, 'src', *p)
SIMP = lambda i: os.path.join(ROOT, 'simp', i, i + '.gltf')

# ---------------------------------------------------------------- helpers

def by_names(names):
    names = set(names)
    return lambda n: n in names


def paint_rects(img, rects, ring=6):
    """Fill rectangles with the median colour of a ring around each (removes text)."""
    a = np.asarray(img.convert('RGB')).copy()
    for x0, y0, x1, y1 in rects:
        X0, Y0, X1, Y1 = max(0, x0 - ring), max(0, y0 - ring), min(a.shape[1], x1 + ring), min(a.shape[0], y1 + ring)
        block = a[Y0:Y1, X0:X1].reshape(-1, 3)
        inner = np.zeros((Y1 - Y0, X1 - X0), bool); inner[y0 - Y0:y1 - Y0, x0 - X0:x1 - X0] = True
        col = np.median(block[~inner.reshape(-1)], axis=0)
        a[y0:y1, x0:x1] = col
    return Image.fromarray(a)


SPEC = {}


def spec(name, **kw):
    SPEC[name] = kw


# ---------------------------------------------------------------- Poly Haven (CC0)
spec('StreetLamp', arm=True, src=SIMP('street_lamp_01'), target=None,
     rename={'street_lamp_01': 'LampMetal', 'street_lamp_01_glass': 'LampGlass', 'street_lamp_01_bulb': 'LampBulb'},
     maps={'LampMetal': dict(max_tex=1024, nrm_tex=512, ms_tex=256), 'LampGlass': 'const', 'LampBulb': 'const'},
     const={'LampGlass': dict(kd=[0.82, 0.82, 0.78], alpha=0.35, metallic=0.0, smoothness=0.95),
            'LampBulb': dict(kd=[1.0, 0.92, 0.75], alpha=1.0, metallic=0.0, smoothness=0.5, emission='warm white (1.0, 0.85, 0.6) x2-4 at night')})
spec('FireHydrant', arm=True, src=SIMP('fire_hydrant'), target=None,
     select=lambda n: n.endswith('_aged') or '_aged_' in n, rename={'fire_hydrant_aged': 'Hydrant'},
     maps={'Hydrant': dict(max_tex=1024, ms_tex=256)})
spec('MetalTrashCan', arm=True, src=SIMP('metal_trash_can'), target=None,
     select=lambda n: 'rust' not in n, rename={'metal_trash_can': 'GalvanisedSteel'},
     maps={'GalvanisedSteel': dict(max_tex=1024, nrm_tex=512, ms_tex=256)})
spec('StreetBench', arm=True, src=SIMP('modular_street_seating'), target=None,
     select=by_names(['seat', 'seat_back', 'arm_rest_01', 'arm_rest_02', 'back_support_l', 'back_support_r',
                      'legs_single', 'legs_double', 'crossbar', 'suspended_support_01']),
     rename={'modular_street_seating_timber': 'Timber', 'modular_street_seating_armrests': 'Steel',
             'modular_street_seating_supports': 'CastIron'},
     maps={'Timber': dict(max_tex=1024, ms_tex=256), 'Steel': dict(max_tex=512, ms_tex=128),
           'CastIron': dict(max_tex=512, ms_tex=128)})
spec('ManholeCover', arm=True, src=SIMP('water_manhole_cover'), target=None,
     rename={'water_manhole_cover': 'CastIron'}, maps={'CastIron': dict(max_tex=1024, ms_tex=256)}, ground=False)
spec('UtilityBox', arm=True, src=SIMP('utility_box_01'), target=None,
     rename={'utility_box_01': 'PaintedSteel'}, maps={'PaintedSteel': dict(max_tex=1024, nrm_tex=512, ms_tex=256)})
spec('AirconUnit', arm=True, src=SIMP('exterior_aircon_unit'), target=None, ground=False, center=False,
     select=lambda n: 'rusted' not in n,
     rename={'exterior_aircon_unit_01': 'AirconBody', 'exterior_aircon_unit_02': 'AirconGrille'},
     maps={'AirconBody': dict(max_tex=1024, nrm_tex=512, ms_tex=256), 'AirconGrille': dict(max_tex=512, ms_tex=128, alpha=True)})
spec('RollerShutterDoor', arm=True, src=S('eg/rollershutter_door/rollershutter_door_1k.gltf'),
     select=lambda n: 'graffiti' not in n, rename={'rollershutter_door': 'Shutter'},
     maps={'Shutter': dict(max_tex=1024, nrm_tex=512, ms_tex=256)}, extra_graffiti=S('eg/rollershutter_door/textures/rollershutter_door_graffiti_diff_1k.jpg'))
spec('RollerShutterWindow', arm=True, src=S('eg/rollershutter_window_01/rollershutter_window_01_1k.gltf'),
     select=lambda n: 'graffiti' not in n, rename={'rollershutter_window_01': 'Shutter'},
     maps={'Shutter': dict(max_tex=1024, nrm_tex=512, ms_tex=256)})
spec('SecurityLight', arm=True, src=SIMP('security_light'), target=None,
     rename={'security_light': 'Housing', 'security_light_glass': 'Glass', 'security_light_bulb': 'Bulb'},
     maps={'Housing': dict(max_tex=512, ms_tex=128), 'Glass': 'const', 'Bulb': 'const'}, ground=False, center=False,
     const={'Glass': dict(kd=[0.85, 0.85, 0.82], alpha=0.55, metallic=0.0, smoothness=0.7),
            'Bulb': dict(kd=[1.0, 0.85, 0.6], alpha=1.0, metallic=0.0, smoothness=0.5, emission='sodium/warm (1.0, 0.7, 0.35) x3 when lit')})

spec('PottedPlant', src=SIMP('potted_plant_01'), target=None,
     rename={'potted_plant_01_pot': 'Pot', 'potted_plant_01_leaves': 'Leaves'},
     maps={'Pot': dict(max_tex=1024, nrm_tex=512, ms_tex=256), 'Leaves': dict(max_tex=1024, nrm_tex=512, ms_tex=256)})

# ---------------------------------------------------------------- Daniel Zhabotinsky (CC BY 4.0)
POLICE_SLOGANS = [(926, 671, 1061, 722), (933, 1926, 1060, 1977)]
spec('PoliceCar', src=S('zh/police_sedan/scene.gltf'), target=None,
     exclude=lambda n: 'badges' in n.lower(),
     rename={'Negotiator80cop_body': 'Body', 'UCB_BOTTOM': 'Chassis', 'GEN4DR_INTERIOR': 'Interior',
             'RB1c_Tire': 'Wheel', 'Riverside88_lights_glass': 'Lights', 'UCB_GLASS_CLEAN': 'Glass',
             'Negotiator80_Misc': 'Plate'},
     maps={'Body': dict(max_tex=1024, ms_tex=8), 'Chassis': dict(max_tex=1024, ms_tex=256),
           'Interior': dict(max_tex=512, ms_tex=256), 'Wheel': dict(max_tex=512, ms_tex=256),
           'Lights': dict(max_tex=512, ms_tex=8), 'Glass': 'const', 'Plate': 'const'},
     patch={'Negotiator80cop_body': lambda im: paint_rects(im, POLICE_SLOGANS)},
     const={'Plate': dict(kd=[0.86, 0.86, 0.84], metallic=0.0, smoothness=0.45),
            'Glass': dict(kd=[0.18, 0.2, 0.21], alpha=0.35, metallic=0.0, smoothness=0.95)},
     rotate_y=0)
spec('Hatchback', src=S('flow/ace/ace.gltf'), target=None,
     exclude=lambda n: 'Badges' in n,
     rename={'Ace11_Bodymat': 'Paint', 'UCB_BOTTOM_LT': 'Chassis', 'UCB_Interiors_2': 'Interior',
             'RB1c_Tire_1k': 'Wheel', 'UCB_Lights_and_Glass': 'Lights', 'UCB_Lights_and_Glass_Transperent': 'Glass',
             'Numberplates_Misk_U': 'Plate'},
     maps={'Paint': 'const', 'Chassis': dict(max_tex=1024, ms_tex=256), 'Interior': dict(max_tex=512, ms_tex=256),
           'Wheel': dict(max_tex=512, ms_tex=256), 'Lights': dict(max_tex=512, ms_tex=8), 'Glass': 'const',
           'Plate': 'const'},
     const={'Plate': dict(kd=[0.86, 0.86, 0.84], metallic=0.0, smoothness=0.45),
            'Paint': dict(kd=[0.33, 0.36, 0.40], metallic=0.6, smoothness=0.85),
            'Glass': dict(kd=[0.12, 0.13, 0.14], alpha=0.45, metallic=0.0, smoothness=0.95)})

spec('SedanClassic', src=S('zh/sedan88/scene.gltf'), target=None, scale=0.0254, rotate_y=180,
     exclude=lambda n: 'badges' in n.lower(),
     rename={'Bodycolor': 'Paint', 'UCB_BOTTOM': 'Chassis', 'GEN4DR_INTERIOR': 'Interior', 'RIMSTOCK_04': 'Wheel',
             'Riverside88_Headlights': 'Lights', 'UCB_GLASS': 'Glass', 'Numberplates': 'Plate'},
     maps={'Paint': 'const', 'Chassis': dict(max_tex=1024, ms_tex=256), 'Interior': dict(max_tex=512, ms_tex=256),
           'Wheel': dict(max_tex=512, ms_tex=256), 'Lights': dict(max_tex=512, ms_tex=8), 'Glass': 'const', 'Plate': 'const'},
     const={'Plate': dict(kd=[0.86, 0.86, 0.84], metallic=0.0, smoothness=0.45),
            'Paint': dict(kd=[0.22, 0.05, 0.06], metallic=0.5, smoothness=0.8),
            'Glass': dict(kd=[0.12, 0.13, 0.14], alpha=0.45, metallic=0.0, smoothness=0.95)})

DT = S('zh/downtown/scene.gltf')
DT_RENAME = {'Downtown_Facade_FLR_2': 'DT_BrickFacade', 'Downtown_Firstfloor_FLR_2': 'DT_Storefronts',
             'Downtown_Maskedstuff_1': 'DT_MaskedDetails', 'Brownstone_Facade_Flr_2': 'DT_BrownstoneFacade',
             'Downtown_Facade_FLR_8': 'DT_ConcreteFacade', 'Downtown_Facade_FLR_6': 'DT_PanelFacade',
             'Downtown_Modern_Firstfloor_FLR_1': 'DT_ModernGroundFloor', 'Downtown_Brownstone_Firstfloor_1': 'DT_BrownstoneGroundFloor'}
for bname, out in [('BrownstoneOffice_2', 'BuildingBrickCorner'), ('Mall_1', 'BuildingBrickArcade'),
                   ('ModernOffice_2', 'BuildingNarrowOffice'), ('Residential_3', 'BuildingBrickResidential')]:
    spec(out, src=DT, select=(lambda b: (lambda n: n.startswith('Downtown_%s_Downtown' % b) or n.startswith('Downtown_%s_Brownstone' % b)))(bname),
         rename=DT_RENAME, shared_maps=True, normals=False, maps={'*': dict(max_tex=1024, ms_tex=8, ao=0)}, target=None)


# ---------------------------------------------------------------- build

def build(name, out, log):
    sp = SPEC[name]
    parts = conv.load_parts(sp['src'], select=sp.get('select'), exclude=sp.get('exclude'))
    rename = sp.get('rename', {})
    groups, mats_done, mtl = [], {}, []
    # optional texture patch (in place on the PIL images held by trimesh materials)
    for node, m, mat in parts:
        mname = getattr(mat, 'name', 'mat')
        for key, fn in sp.get('patch', {}).items():
            if mname == key and not getattr(mat, '_patched', False):
                mat.baseColorTexture = fn(mat.baseColorTexture)
                mat._patched = True
    total = sum(len(m.faces) for _, m, _ in parts)
    target = sp.get('target')
    for node, m, mat in parts:
        if target and total > target:
            share = max(60, int(target * len(m.faces) / total))
            if len(m.faces) > share and len(m.faces) > 200:
                m = conv.decimate(m, share)
        mname_src = getattr(mat, 'name', 'mat') or 'mat'
        mname = rename.get(mname_src, conv.safe(mname_src))
        if mname not in mats_done:
            mp = sp['maps'].get(mname, sp['maps'].get('*', dict(max_tex=1024, ms_tex=512)))
            prefix = mname if sp.get('shared_maps') else '%s_%s' % (name, mname)
            if mp == 'const':
                c = sp.get('const', {}).get(mname)
                fac = conv._factor(getattr(mat, 'baseColorFactor', None)) if mat is not None else [0.8] * 4
                info = dict(kd=[round(x, 4) for x in fac[:3]], alpha=round(fac[3], 3),
                            metallic=float(getattr(mat, 'metallicFactor', 0) or 0),
                            smoothness=round(1 - float(getattr(mat, 'roughnessFactor', 1) if getattr(mat, 'roughnessFactor', None) is not None else 1), 3))
                if c:
                    info.update(c)
            else:
                if sp.get('shared_maps') and os.path.exists(os.path.join(out, prefix + '_Color.jpg')):
                    info = json.load(open(os.path.join(out, '.%s.json' % prefix)))
                else:
                    info = conv.write_maps(out, prefix, mat, max_tex=mp.get('max_tex', 1024), ms_tex=mp.get('ms_tex', 512), nrm_tex=mp.get('nrm_tex'),
                                           ao_strength=mp.get('ao', 0.5), alpha=mp.get('alpha', False),
                                           arm=sp.get('arm', False))
                    if mp.get('ms_tex', 512) <= 8 and info.get('metallic_smoothness'):
                        # constant-ish map: replace by constants (saves files)
                        a = np.asarray(Image.open(os.path.join(out, info['metallic_smoothness'])))
                        info['metallic'] = round(float(a[..., 0].mean()) / 255, 3)
                        info['smoothness'] = round(float(a[..., 3].mean()) / 255, 3)
                        os.remove(os.path.join(out, info.pop('metallic_smoothness')))
                    if sp.get('shared_maps'):
                        json.dump(info, open(os.path.join(out, '.%s.json' % prefix), 'w'))
            mats_done[mname] = info
            mtl.append((mname, info))
        m = conv.weld(m, normals=sp.get('normals', True))
        groups.append((node, mname, m))
    conv.write_mtl(out, name, mtl)
    res = conv.write_obj(out, name, groups, ground=sp.get('ground', True), center=sp.get('center', True),
                         rotate_y_deg=sp.get('rotate_y', 0), write_normals=sp.get('normals', True), scale=sp.get('scale', 1.0))
    res['materials'] = {k: v for k, v in mtl}
    if sp.get('extra_graffiti'):
        im = Image.open(sp['extra_graffiti']).convert('RGB').resize((1024, 1024), Image.LANCZOS)
        # bake the same AO as the plain colour map
        mat0 = parts[0][2]
        ao = np.asarray(mat0.metallicRoughnessTexture.convert('RGB').resize((1024, 1024))).astype(np.float32)[..., 0] / 255
        a = np.asarray(im).astype(np.float32) / 255 * (0.5 + 0.5 * ao)[..., None]
        fn = '%s_Graffiti_Color.jpg' % name
        Image.fromarray((np.clip(a, 0, 1) * 255 + .5).astype(np.uint8)).save(os.path.join(out, fn), quality=88, optimize=True, progressive=True)
        res['alt_color'] = fn
    log[name] = res
    print(name, res['tris'], res['size_m'])


if __name__ == '__main__':
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    names = sys.argv[2:] or list(SPEC)
    logp = os.path.join(out, '.build_log.json')
    log = json.load(open(logp)) if os.path.exists(logp) else {}
    for n in names:
        build(n, out, log)
    json.dump(log, open(logp, 'w'), indent=1)
