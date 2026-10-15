"""Build the PeopleModels pack from a sparse clone of microsoft/Microsoft-Rocketbox.

    python3 -P build_all.py ROCKETBOX_CLONE PACK_DIR BPY_PYTHON [Figure1,Figure2]

Steps: pose each avatar with one frame of a Rocketbox animation and bake it to OBJ (pose_export.py,
run with Blender's bpy module), convert the TGA maps (build_textures.py, edits.py), make the prop
atlas (make_props_atlas.py) and write one MTL per OBJ.
"""
import json, os, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
RB, PACK, BPY = sys.argv[1], sys.argv[2], sys.argv[3]
ONLY = set(sys.argv[4].split(',')) if len(sys.argv) > 4 else None  # optional: rebuild only these figures
WORK = os.path.join(os.path.dirname(HERE), 'work', 'build')
os.makedirs(WORK, exist_ok=True)
os.makedirs(PACK, exist_ok=True)
AV = os.path.join(RB, 'Assets', 'Avatars')
AN = os.path.join(RB, 'Assets', 'Animations', 'all_animations_max_motextr_static')

FIGURES = {
    'PoliceOfficer': {
        'avatar': 'Professions/Police_Male_03', 'tex': 'm172',
        'materials': {'m172_body': 'Body', 'm172_head': 'Head', 'm172_equpiment': 'Equipment', 'm172_pistol': 'Pistol'},
        'maps': {'Body': {'prefix': 'm172_body', 'size': 2048},
                 'Head': {'prefix': 'm172_head', 'size': 2048},
                 'Equipment': {'prefix': 'm172_equipment', 'metal': True, 'size': 1024, 'nsize': 512, 'mssize': 256,
                               'edits': [{'fn': 'paint_out', 'args': {'boxes': [[766, 518, 830, 562, 0, 38]]}}]},
                 'Pistol': {'prefix': 'm172_pistol', 'metal': True, 'size': 256, 'nsize': 256, 'mssize': 128,
                            'edits': [{'fn': 'paint_out', 'args': {'boxes': [[222, 158, 272, 208, 0, -50], [236, 732, 290, 784, 30, 0]]}}]}},
        'poses': {
            'PoliceOfficer_Standing': {'anim': 'm_idle_neutral_02', 'frame': 30},
            'PoliceOfficer_TakingNotes': {'anim': 'm_documents_note', 'frame': 190, 'props': [
                {'kind': 'notebook', 'anchor': 'palm:L', 'offset': [0, 0.015, 0.022], 'rot': [90, 0, 0]},
                {'kind': 'pen', 'anchor': 'pinch:R', 'offset': [0, -0.03, -0.004], 'rot': [215, 0, 0]}]},
        }},
    'ForensicExaminer': {
        'avatar': 'Professions/Medical_Male_04', 'tex': 'm162',
        'materials': {'m162_body': 'Body', 'm162_head': 'Head', 'm162_protection': 'Protection'},
        'maps': {'Body': {'prefix': 'm162_body', 'size': 2048, 'jpeg_max': 240, 'subsampling': 0,
                          'edits': [{'fn': 'forensic_body', 'args': {'labels': 'LABELS'}}]},
                 'Head': {'prefix': 'm162_head', 'size': 2048},
                 'Protection': {'prefix': 'm162_protection', 'size': 1024, 'nsize': 1024, 'jpeg_max': 240, 'subsampling': 0,
                                'edits': [{'fn': 'forensic_protection'}]}},
        'poses': {
            'ForensicExaminer_Crouching': {'anim': 'm_crouch_idle', 'frame': 1},
            'ForensicExaminer_Photographing': {'anim': 'm_take_picture', 'frame': 80, 'props': [
                {'kind': 'camera', 'anchor': 'face', 'offset': [0.0, -0.005, 0.06], 'rot': [0, 180, 0]}]},
        }},
    'Detective': {
        'avatar': 'Professions/Police_Male_07', 'tex': 'm176',
        'materials': {'m176_body': 'Body', 'm176_head': 'Head'},
        'maps': {'Body': {'prefix': 'm176_body', 'size': 2048}, 'Head': {'prefix': 'm176_head', 'size': 2048}},
        'poses': {'Detective_LookingAround': {'anim': 'm_idle_look_around_02', 'frame': 40}}},
    'DetectiveFemale': {
        'avatar': 'Professions/Business_Female_04', 'tex': 'f020',
        'materials': {'f020_body': 'Body', 'f020_head': 'Head', 'f020_opacity': 'Hair'},
        'double_sided': ['Hair'],
        'maps': {'Body': {'prefix': 'f020_body', 'size': 2048}, 'Head': {'prefix': 'f020_head', 'size': 2048},
                 'Hair': {'prefix': 'f020_opacity', 'alpha': True, 'size': 1024}},
        'poses': {'DetectiveFemale_TakingNotes': {'anim': 'f_documents_note', 'frame': 150, 'props': [
            {'kind': 'notebook', 'anchor': 'palm:L', 'offset': [0, 0.015, 0.022], 'rot': [90, 0, 0]},
            {'kind': 'pen', 'anchor': 'pinch:R', 'offset': [0, -0.03, -0.004], 'rot': [215, 0, 0]}]}}},
    'PasserBy': {
        'avatar': 'Adults/Male_Adult_12', 'tex': 'm007',
        'materials': {'m007_body': 'Body', 'm007_head': 'Head'},
        'maps': {'Body': {'prefix': 'm007_body', 'size': 2048}, 'Head': {'prefix': 'm007_head', 'size': 2048}},
        'poses': {'PasserBy_Phone': {'anim': 'm_cell_phone_textmessage', 'frame': 100, 'props': [
            {'kind': 'phone', 'anchor': 'palm:R', 'offset': [0.008, 0.05, 0.018], 'rot': [90, 0, 180]}]}}},
}

# Allow quick size experiments: PEOPLE_MAXTEX caps colour map size.
cap = int(os.environ.get('PEOPLE_MAXTEX', '0') or 0)

summary = {}
if os.path.exists(os.path.join(WORK, 'summary.json')):
    summary = json.load(open(os.path.join(WORK, 'summary.json')))
subprocess.run([sys.executable, '-P', os.path.join(HERE, 'make_props_atlas.py'), PACK], check=True)
for fig, F in FIGURES.items():
    if ONLY and fig not in ONLY:
        continue
    avatar_dir = os.path.join(AV, F['avatar'])
    fbx = os.path.join(avatar_dir, 'Export', os.path.basename(F['avatar']) + '.fbx')
    labels = None
    for pose, P in F['poses'].items():
        cfg = {'avatar': fbx, 'anim': os.path.join(AN, P['anim'] + '.max.fbx'), 'frame': P['frame'], 'out': PACK,
               'name': pose, 'materials': F['materials'], 'props': P.get('props', []),
               'double_sided': F.get('double_sided', [])}
        cp = os.path.join(WORK, pose + '.json')
        json.dump(cfg, open(cp, 'w'), indent=1)
        r = subprocess.run([BPY, '-I', os.path.join(HERE, 'pose_export.py'), '--', cp], capture_output=True, text=True)
        line = [l for l in r.stdout.splitlines() if l.startswith('INFO')]
        if r.returncode or not line:
            print(r.stdout[-2000:], r.stderr[-2000:])
            raise SystemExit('pose failed: ' + pose)
        info = json.loads(line[0][5:])
        # keep the helper outputs out of the pack
        for ext in ('_labels.json', '_info.json'):
            src = os.path.join(PACK, pose + ext)
            os.replace(src, os.path.join(WORK, pose + ext))
        labels = labels or os.path.join(WORK, pose + '_labels.json')
        summary[pose] = {'figure': fig, 'anim': P['anim'], 'frame': P['frame'], **info}
    maps = json.loads(json.dumps(F['maps']).replace('"LABELS"', json.dumps(labels)))
    if cap:
        for m in maps.values():
            m['size'] = min(m.get('size', 1024), cap)
    tcfg = {'out': PACK, 'name': fig, 'textures': os.path.join(avatar_dir, 'Textures'), 'materials': maps}
    tp = os.path.join(WORK, fig + '_tex.json')
    json.dump(tcfg, open(tp, 'w'), indent=1)
    subprocess.run([sys.executable, '-P', os.path.join(HERE, 'build_textures.py'), tp], check=True)
    # one MTL per OBJ
    for pose in F['poses']:
        mats = summary[pose]['materials']
        lines = ['# %s. Unity reads Kd, map_Kd and d only: assign <Map>_Normal (normal map) and' % pose,
                 '# <Map>_MetallicSmoothness (Standard Metallic, source = metallic alpha) in code; see README.md.']
        for m in mats:
            if m in ('Props', 'PropScreen'):
                tex = 'PeopleProps_Color.jpg'
            else:
                tex = '%s_%s_Color.%s' % (fig, m, 'png' if F['maps'].get(m, {}).get('alpha') else 'jpg')
            lines += ['', 'newmtl %s' % m, 'Kd 1.000 1.000 1.000', 'Ks 0.040 0.040 0.040', 'd 1.000', 'map_Kd %s' % tex]
        open(os.path.join(PACK, pose + '.mtl'), 'w').write('\n'.join(lines) + '\n')
json.dump(summary, open(os.path.join(WORK, 'summary.json'), 'w'), indent=1)
for k, v in summary.items():
    print(k, v['tris'], v['size_m'], v['materials'])
