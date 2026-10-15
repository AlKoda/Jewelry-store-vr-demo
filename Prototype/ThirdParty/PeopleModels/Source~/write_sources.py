"""Write sources.json for the PeopleModels pack (shipped-file hashes + Rocketbox source files).

    python3 -P write_sources.py ROCKETBOX_CLONE PACK_DIR SUMMARY_JSON
"""
import hashlib, json, os, subprocess, sys
from PIL import Image

RB, PACK, SUMMARY = sys.argv[1], sys.argv[2], sys.argv[3]
COMMIT = subprocess.run(['git', '-C', RB, 'rev-parse', 'HEAD'], capture_output=True, text=True).stdout.strip()
REPO = 'https://github.com/microsoft/Microsoft-Rocketbox'
TREE = '%s/tree/%s/' % (REPO, COMMIT)
MIT = {'name': 'MIT License', 'url': '%s/blob/%s/LICENSE.md' % (REPO, COMMIT),
       'copyright': 'Copyright (c) 2020 Microsoft'}
AUTHOR = 'Microsoft (copyright holder); avatars created by Rocketbox Studios GmbH (Markus Wojcik et al.)'
PROJECT = 'Project-owned original work (Jewelry-store-vr-demo PeopleModels pack)'

FIG = {  # figure -> avatar folder, texture prefix, per-material source maps, edits
    'PoliceOfficer': ('Professions/Police_Male_03', 'm172', {'Body': 'm172_body', 'Head': 'm172_head', 'Equipment': 'm172_equipment', 'Pistol': 'm172_pistol'},
                      {'Equipment': 'Small "RBX" badge on the radio cloned out in colour, normal and specular (paint_out).',
                       'Pistol': 'Two maker-style grip medallions (left and right grip panels) cloned out in colour, normal and specular (paint_out).'}),
    'ForensicExaminer': ('Professions/Medical_Male_04', 'm162', {'Body': 'm162_body', 'Head': 'm162_head', 'Protection': 'm162_protection'},
                         {'Body': 'Green scrubs recoloured to a white disposable coverall with long sleeves and blue nitrile gloves '
                                  '(forensic_body); normal map flattened under sleeves and gloves; smoothness set to 0.24 fabric / 0.58 gloves.',
                          'Protection': 'Surgical cap recoloured white (forensic_protection); mask unchanged; smoothness 0.22.'}),
    'Detective': ('Professions/Police_Male_07', 'm176', {'Body': 'm176_body', 'Head': 'm176_head'}, {}),
    'DetectiveFemale': ('Professions/Business_Female_04', 'f020', {'Body': 'f020_body', 'Head': 'f020_head', 'Hair': 'f020_opacity'},
                        {'Hair': 'RGBA kept at 1024 x 512, colour bled into transparent texels for alpha testing; the hair cards are duplicated back-to-back in the OBJ.'}),
    'PasserBy': ('Adults/Male_Adult_12', 'm007', {'Body': 'm007_body', 'Head': 'm007_head'}, {}),
}
ANIM_DIR = 'Assets/Animations/all_animations_max_motextr_static/'
COLOR_REVISION = {  # (figure, material) -> extra note for the colour map only
    ('ForensicExaminer', 'Body'): 'White levels (revised 2026-10-15): coverall median 220 sRGB with highlights rolled off below 238, '
                                  'glove highlights rolled off the same way (glove median unchanged), a two-texel blend at the cuffs, '
                                  'unused UV space edge-padded, saved as 4:4:4 JPG q88 with every decoded texel at most 240 (save_jpeg_capped).',
    ('ForensicExaminer', 'Protection'): 'White levels (revised 2026-10-15): cap median 220 sRGB with highlights rolled off below 238; '
                                        'the mask is unchanged apart from its brightest texels (241-243) capped at 240; unused UV space '
                                        'edge-padded; 4:4:4 JPG q88 with every decoded texel at most 240.',
}


def sha(p):
    h = hashlib.sha256()
    with open(p, 'rb') as f:
        for chunk in iter(lambda: f.read(1 << 20), b''):
            h.update(chunk)
    return h.hexdigest()


def src(rel):
    p = os.path.join(RB, rel)
    return {'path': rel, 'url': '%s/blob/%s/%s' % (REPO, COMMIT, rel), 'bytes': os.path.getsize(p), 'sha256': sha(p)}


summary = json.load(open(SUMMARY))
files = []
for fn in sorted(os.listdir(PACK)):
    p = os.path.join(PACK, fn)
    if os.path.isdir(p) or fn in ('sources.json', 'LICENSE.txt', 'README.md'):
        continue
    e = {'file': fn, 'bytes': os.path.getsize(p), 'sha256': sha(p)}
    stem, ext = os.path.splitext(fn)
    if fn.startswith('PeopleProps_'):
        im = Image.open(p)
        e.update(kind='texture', pixels=list(im.size), license='Project-owned', author=PROJECT,
                 source_url='Source~/make_props_atlas.py (procedural, no third-party input)',
                 what='Shared atlas for the hand props (notebook, pen, phone, camera)')
    elif ext in ('.obj', '.mtl'):
        info = summary[stem]
        fig = info['figure']
        avatar, prefix, maps, _ = FIG[fig]
        e.update(kind='model' if ext == '.obj' else 'material', license='MIT (Rocketbox avatar); hand props project-owned',
                 author=AUTHOR, source_url=TREE + 'Assets/Avatars/' + avatar, commit=COMMIT,
                 derived_from=[src('Assets/Avatars/%s/Export/%s.fbx' % (avatar, os.path.basename(avatar))),
                               src(ANIM_DIR + info['anim'] + '.max.fbx')],
                 pose={'animation': info['anim'], 'frame': info['frame']})
        if ext == '.obj':
            e.update(tris=info['tris'], size_m=info['size_m'], materials=info['materials'])
    else:
        fig, mat, kind = stem.split('_')
        avatar, prefix, maps, edits = FIG[fig]
        base = 'Assets/Avatars/%s/Textures/%s' % (avatar, maps[mat])
        srcmap = {'Color': '_color.tga', 'Normal': '_normal.tga', 'MetallicSmoothness': '_specular.tga'}[kind]
        im = Image.open(p)
        e.update(kind='texture', pixels=list(im.size), mode=im.mode, license='MIT', author=AUTHOR,
                 source_url=TREE + 'Assets/Avatars/' + avatar + '/Textures', commit=COMMIT,
                 derived_from=[src(base + srcmap)])
        note = edits.get(mat, 'No content change.')
        if kind == 'Color' and (fig, mat) in COLOR_REVISION:
            note += ' ' + COLOR_REVISION[(fig, mat)]
        if (fig, mat, kind) == ('ForensicExaminer', 'Body', 'Normal'):
            note += ' Rebuilt 2026-10-15 with the deterministic bone labels (two shoulder triangles changed class).'
        if kind == 'MetallicSmoothness':
            note = ('Packed from the Rocketbox specular map: A = clamp(0.16 + 2.4 * specular, 0.12, 0.72), '
                    'R = metallic (0 except the metal parts of Equipment and Pistol). ' + note)
        sw, sh = Image.open(os.path.join(RB, base + srcmap)).size
        e['changes'] = 'Converted from the %d x %d TGA to %d x %d %s. ' % (sw, sh, im.size[0], im.size[1], ext[1:].upper()) + note
    files.append(e)
for fn in sorted(os.listdir(os.path.join(PACK, 'Source~'))):
    p = os.path.join(PACK, 'Source~', fn)
    files.append({'file': 'Source~/' + fn, 'kind': 'build script' if fn.endswith('.py') else 'preview image',
                  'bytes': os.path.getsize(p), 'sha256': sha(p), 'license': 'Project-owned', 'author': PROJECT,
                  'source_url': 'project-made'})
out = {
    'pack': 'PeopleModels', 'prepared': '2026-10-14', 'finished': '2026-10-15', 'unverified_in_unity': True,
    'revisions': ['2026-10-15: ForensicExaminer_Body_Color.jpg and ForensicExaminer_Protection_Color.jpg rebuilt after review: '
                  'the white coverall and cap were near-clipped (median 245, 35% of texels >= 250); now median 220 sRGB, '
                  'maximum 240, fold shading kept. Source~/Preview.jpg re-rendered. pose_export.py now breaks bone-label ties '
                  'deterministically (first corner), so ForensicExaminer_Body_Normal.jpg changed in two shoulder triangles.'],
    'upstream': {'repository': REPO, 'commit': COMMIT, 'commit_date': subprocess.run(['git', '-C', RB, 'log', '-1', '--format=%cI'], capture_output=True, text=True).stdout.strip(),
                 'license': MIT, 'license_statement': 'README.md: "12/2020: Updated license to MIT." and "November 2020 License Update: The library of avatars is now released under MIT License."',
                 'fetched_with': 'git clone --filter=blob:none --depth 1 --no-checkout + sparse checkout (no LFS objects involved)'},
    'tools': {'posing': 'Blender 5.2.2 LTS as the bpy Python module (pip), Source~/pose_export.py', 'textures': 'Pillow 12, numpy, scipy (Source~/build_textures.py, edits.py)'},
    'units': 'metres, Y up, front = +Z, feet on y = 0, centred in x/z (OBJ/MTL, Unity imports without packages)',
    'texture_convention': {'Color': 'sRGB JPG (map_Kd); DetectiveFemale_Hair_Color.png is RGBA for alpha test',
                           'Normal': 'tangent space, OpenGL +Y (Unity convention), JPG',
                           'MetallicSmoothness': 'PNG RGBA, R = metallic, G = B = 0, A = smoothness (linear)'},
    'files': files,
}
json.dump(out, open(os.path.join(PACK, 'sources.json'), 'w'), indent=1)
print(len(files), 'entries; commit', COMMIT)
