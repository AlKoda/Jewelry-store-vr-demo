"""Write sources.json for the ForensicModels pack. write_sources.py WORK PACK"""
import sys, os, json, hashlib, re

W, PACK = sys.argv[1], sys.argv[2]
stats = json.load(open(os.path.join(W, 'stats.json')))
src_sha = {}
for line in open(os.path.join(W, 'src_original_sha256.txt')):
    h, p = line.split(None, 1); src_sha[p.strip()] = h

CC0 = {'license': 'CC0-1.0', 'license_url': 'https://creativecommons.org/publicdomain/zero/1.0/',
       'publisher_license': 'https://polyhaven.com/license',
       'publisher_license_read_from': 'https://github.com/Poly-Haven/polyhaven.com/blob/b1a6aa13afc03ae00860f6ac5fe6d5daf8e95356/public/locales/en/license.json'}
CONCORD = 'github.com/ConcordDev/concord-cognitive-engine @ a9e29c84d175189ca890f013ed47ed8887e78a7d, apps/concordia-living-world/unity-client/Assets/Concordia/PolyHaven/Models/%s/ (Git LFS; manifest source "Poly Haven (CC0)")'
DESK = 'github.com/HHSOLL/DeskteriorOnline @ 6102359dba9fd95f599408be1873abaa86e20730, apps/web/public/assets/models/%s/textures/'
GEADAS = 'github.com/Geadas11/Claude_steam_game @ 8b6aa56f951450b7a05434615bcdda2bd2d1e8fa, assets/3d/models/%s/'
VSE = ('github.com/ryan-ressmeyer/VisionStimulusEngine @ 2bf1c339e48d6c908a1200507e7db5b9850315bd, assets/3d/manifest.toml: '
       'the SHA-256 it pins for the official dl.polyhaven.org downloads (cross_pein_hammer_1k.gltf, cross_pein_hammer.bin, '
       'diff_1k, nor_gl_1k, arm_1k) equal all five source_files hashes here, so the mirror copy is the unmodified Poly Haven release')
PH = {
    # pack name: (asset id, title, authors, mirror repo, commit, mirror dir, src folder, corroboration, credits file)
    'Crowbar': ('crowbar_01', 'Crowbar 01', {'Alexander Otterbeck': 'All'}, 'tuniveza/Generative-Games', '831f115caeea2201fdac5315bac4fcb707eeaf85',
                'The-Ruins-of-Bezan/assets/models/crowbar_01/', 'Crowbar', [DESK % 'crowbar_01' + ' (arm, diff, nor_gl byte-identical)'], 'The-Ruins-of-Bezan/CREDITS.md'),
    'Sledgehammer': ('sledgehammer_01', 'Sledgehammer 01', {'Dylan Guzman': 'All'}, 'tuniveza/Generative-Games', '831f115caeea2201fdac5315bac4fcb707eeaf85',
                     'The-Ruins-of-Bezan/assets/models/sledgehammer_01/', 'Sledgehammer', [], 'The-Ruins-of-Bezan/CREDITS.md'),
    'BoltCutters': ('bolt_cutters_01', 'Bolt Cutters 01', {'Antanas Kep': 'Modeling & Texturing', 'Yann Kervran': 'Rigging'}, 'Tiddybub/3d-assets',
                    '11ff7082e566a6278553bf7623980cb93dd41e27', 'sci-fi/bolt_cutters_01/', 'BoltCutters', [DESK % 'bolt_cutters_01' + ' (arm, diff, nor_gl byte-identical)'], 'README.md (all assets CC0, source link https://polyhaven.com/a/bolt_cutters_01)'),
    'FlatheadScrewdriver': ('flathead_screwdriver', 'Flathead Screwdriver', {'Dylan Detwiller': 'All'}, 'fi-77-70/Game-Jam', '9f83e5aa5b9b7d80d4d3397309d96a32ab9d8b63',
                            'assets/Tools/screwdiver/', 'FlatheadScrewdriver', [DESK % 'flathead_screwdriver' + ' (arm, diff, nor_gl byte-identical)'], None),
    'CrossPeinHammer': ('cross_pein_hammer', 'Cross Pein Hammer', {'Tics': 'All'}, 'mydkonline/laughgg', '3a35f37d59700fb6be0ce9197c8c18902502c505',
                        'assets/ph/cross_pein_hammer/', 'CrossPeinHammer', [CONCORD % 'cross_pein_hammer' + ' (diff_1k byte-identical)', VSE], 'CREDITS.md (Poly Haven assets, CC0)'),
    'WorkGloves': ('garden_gloves_01', 'Garden Gloves 01', {'Satyaki Mandal': 'All'}, 'Geadas11/Claude_steam_game', '8b6aa56f951450b7a05434615bcdda2bd2d1e8fa',
                   'assets/3d/models/garden_gloves_01/', 'WorkGloves', [], 'assets/3d/CREDITS.md (https://polyhaven.com/a/garden_gloves_01, CC0)'),
    'Stone': ('stone_01', 'Stone 01', {'Dario Barresi': 'Photography', 'Rico Cilliers': 'Photography, processing'}, 'awplanets/awplanet',
              '40c01a6984e35b24bad0c1222c6c5970b64f18d2', 'public/models/nature/polyhaven/stone_01/', 'Stone', [], 'public/models/nature/polyhaven/LICENSE.md (Poly Haven, CC0)'),
    'Trashbag': ('trashbag', 'Trashbag', {'Benny Weimer': 'All'}, 'TheMarco/liminal', 'e0950c5982e79878c4e04f1cca89b70190df8dd1',
                 'models/cc0/trashbag/', 'Trashbag', [CONCORD % 'trashbag' + ' (diff_1k byte-identical)'], 'THIRD_PARTY_ASSETS.md, models/cc0/SOURCES.md'),
    'Clipboard': ('clipboard', 'Clipboard', {'ProgrammerOnCoffee': 'All'}, 'TheMarco/liminal', 'e0950c5982e79878c4e04f1cca89b70190df8dd1',
                  'models/cc0/clipboard/', 'Clipboard', [GEADAS % 'clipboard' + ' (clipboard.bin byte-identical)',
                  'gltf sha256 equals the sourceSha256 recorded for https://dl.polyhaven.org/file/ph-assets/Models/gltf/1k/clipboard/clipboard_1k.gltf in a third-party asset manifest'], 'THIRD_PARTY_ASSETS.md, models/cc0/SOURCES.md'),
    'MeasuringTape': ('measuring_tape_01', 'Measuring Tape 01', {'Kuutti Siitonen': 'All'}, 'sulemanali5760/brick-by-brick', '4424aafaf92880ce051945b1ba2aa636b4a50fb3',
                      'assets/source/polyhaven/measuring_tape_01/', 'MeasuringTape', [GEADAS % 'measuring_tape_01' + ' (bin byte-identical)'], 'docs/ASSETS.md (Poly Haven, CC0)'),
    'MagnifyingGlass': ('magnifying_glass_01', 'Magnifying Glass 01', {'Nazar Borodavka': 'All'}, 'tuniveza/Generative-Games', '831f115caeea2201fdac5315bac4fcb707eeaf85',
                        'The-Ruins-of-Bezan/assets/models/magnifying_glass_01/', 'MagnifyingGlass', [GEADAS % 'magnifying_glass_01' + ' (bin byte-identical)'], 'The-Ruins-of-Bezan/CREDITS.md'),
    'SecurityCamera': ('security_camera_01', 'Security Camera 01', {'Alexander Otterbeck': 'Modeling & Texturing', 'Yann Kervran': 'Rigging'}, 'TheMarco/liminal',
                       'e0950c5982e79878c4e04f1cca89b70190df8dd1', 'models/cc0/security_camera_01/', 'SecurityCamera', [], 'THIRD_PARTY_ASSETS.md, models/cc0/SOURCES.md'),
    'PortableSearchlight': ('portable_searchlight', 'Portable Searchlight', {'Elijah Cragg': 'All'}, 'Xavier-ky/oilsoul', '3cf8f19cca7d4d8ac21d3774c72fac7a10796f35',
                            'frontend/public/models/field-props/polyhaven/portable_searchlight/', 'PortableSearchlight', [CONCORD % 'portable_searchlight' + ' (diff_1k byte-identical)'], 'frontend/public/models/field-props/CREDITS.md'),
    'LeatherBinder': ('binder_notebook', 'Binder Notebook', {'DaDrood': 'All'}, 'Geadas11/Claude_steam_game', '8b6aa56f951450b7a05434615bcdda2bd2d1e8fa',
                      'assets/3d/models/binder_notebook/', 'LeatherBinder', ['asset page https://polyhaven.com/a/binder_notebook found by web search: CC0, ~0.6 m, 18K triangles'], 'assets/3d/CREDITS.md (https://polyhaven.com/a/binder_notebook, CC0)'),
    'OfficeNotepads': ('office_notepads', 'Office Notepads', {'Ulan Cabanilla': 'All'}, 'TheMarco/liminal', 'e0950c5982e79878c4e04f1cca89b70190df8dd1',
                       'models/cc0/office_notepads/', 'OfficeNotepads', [], 'THIRD_PARTY_ASSETS.md, models/cc0/SOURCES.md'),
}
CHANGES = {
    'common': 'Converted glTF -> OBJ/MTL with Tools/glb_to_obj.py (--ground: lowest point y = 0, centred in x/z; metres kept). '
              'Base colour -> <Name>_Color.jpg; nor_gl -> <Name>_Normal.jpg (OpenGL/Unity convention); ARM (R=AO, G=rough, B=metal) -> '
              '<Name>_MetallicSmoothness.png (R = metal, A = 1 - rough). OBJ numbers rounded to 0.1 mm. Glass sub-materials replaced by MTL constants.',
    'WorkGloves': 'Woven brand-style tag ("YOKI\'S") on the glove back removed from all three maps: colour map covered with knit '
                  'cloned from the same glove (Source~/delabel.py); roughness/metal (ARM, hence the MetallicSmoothness alpha) and '
                  'normal map filled with boundary-diffused low frequencies plus knit cloned from the same glove back, which also '
                  'removes the tag\'s raised outline (Source~/delabel_glove_maps.py, October 15, 2026). 2K maps reduced to 1K.',
    'MeasuringTape': 'Wordmark ("Q-TECH") in the yellow label painted out; colour map 512 px.',
    'SecurityCamera': 'Badge text ("FocusSpy") painted out of the housing; decimated 13,571 -> 9,001 triangles (UV-exact, Source~/decimate_exact.py).',
    'PortableSearchlight': 'Plate text ("SENTINEL IQ-4") painted out; decimated 18,802 -> 8,999 triangles (UV-exact).',
    'Stone': 'Decimated 53,520 -> 4,999 triangles (UV-exact).',
    'LeatherBinder': 'Decimated 18,077 -> 8,000 triangles (UV-exact); 2K maps reduced to 1K colour / 512 normal.',
    'OfficeNotepads': 'The ten pads of the showcase grid re-laid as a non-overlapping floor scatter (Source~/scatter_groups.py).',
    'Trashbag': 'Maps reduced to 512 px.',
}
PROJECT = {
    'EvidenceBagPaper': 'Source~/proc_evidence.py', 'EvidenceBagPlastic': 'Source~/proc_evidence.py', 'EvidenceMarkerTent': 'Source~/proc_evidence.py',
    'ForensicScaleL': 'Source~/proc_evidence.py', 'PaperSheetForm': 'Source~/proc_evidence.py', 'ScatteredPapers': 'Source~/proc_evidence.py',
    'GlassShardsLarge': 'Source~/proc_glass.py', 'GlassShardsScatter': 'Source~/proc_glass.py', 'TemperedGlassCrumbs': 'Source~/proc_glass.py',
    'GlassCrackDecal': 'Source~/proc_glass.py',
    'FingerprintBrush': 'Source~/proc_kit.py', 'FingerprintPowderJar': 'Source~/proc_kit.py', 'NitrileGloveBox': 'Source~/proc_kit.py',
    'ForensicCase': 'Source~/proc_kit.py', 'TacticalFlashlight': 'Source~/proc_kit.py', 'UVTorch': 'Source~/proc_kit.py', 'PoliceRadio': 'Source~/proc_kit.py',
    'PoliceBarrier': 'Source~/proc_scene.py', 'BrickHalf': 'Source~/proc_scene.py', 'ShoePrintCast': 'Source~/proc_scene.py',
    'CameraTripod': 'Source~/proc_scene.py', 'PoliceCap': 'Source~/proc_scene.py', 'DuffelBag': 'Source~/proc_scene.py',
    'Balaclava': 'Source~/proc_scene.py', 'EvidenceMarkerCone': 'Source~/proc_scene.py',
}
SHARED = {'ForensicPapers_Color.jpg': ['PaperSheetForm', 'ScatteredPapers']}
# where an author name comes from when it is not Poly Haven's own metadata
AUTHOR_SOURCE = {
    'CrossPeinHammer': 'attribution = "Tics; Poly Haven" in github.com/ryan-ressmeyer/VisionStimulusEngine @ 2bf1c339e48d6c908a1200507e7db5b9850315bd, '
                       'assets/3d/manifest.toml, a record whose pinned dl.polyhaven.org hashes match this pack\'s source files byte for byte '
                       '(its other Poly Haven credits, e.g. Greg Zaal, Sergej Majboroda, Rob Tuytel, are the published authors). '
                       'Only one such record was found and polyhaven.com is not reachable from the build machine, so the name is not confirmed '
                       'against the asset page itself.',
    'PortableSearchlight': 'Credited as "Poly Haven - Portable Searchlight, https://polyhaven.com/a/portable_searchlight, author Elijah Cragg, '
                           'CC0 1.0, official 1K glTF, 18,802 triangles" in the mirror\'s frontend/public/models/field-props/CREDITS.md',
    'LeatherBinder': 'Poly Haven API record mirrored in github.com/mylife-as-miles/Palindrome-Three-Bedroom @ 20f15f3e8e6267e54c39d5f2ec7f7d7c66744f8e, '
                     'assets/material-authoring/polyhaven-furniture/selection/office-leisure/binder_notebook-info.json: "authors": {"DaDrood": "All"}, '
                     'polycount 18077 (matches the source glTF); same credit in github.com/PianoNic/BackroomsBaden @ 794e9165, client/public/models/LICENSES.md',
}
# shipped maps rebuilt by a later script instead of build_ph.py/assemble.py
GENERATOR = {
    'WorkGloves_MetallicSmoothness.png': 'Source~/delabel_glove_maps.py (from the unmodified 2K arm and nor_gl maps listed in source_files)',
    'WorkGloves_Normal.jpg': 'Source~/delabel_glove_maps.py (from the unmodified 2K arm and nor_gl maps listed in source_files)',
}


def sha(p):
    return hashlib.sha256(open(p, 'rb').read()).hexdigest()


def model_of(f):
    if f in SHARED: return SHARED[f][0]
    base = os.path.splitext(f)[0]
    for name in sorted(list(PH) + list(PROJECT), key=len, reverse=True):
        if base == name or base.startswith(name + '_'):
            return name
    return None


files = []
for f in sorted(os.listdir(PACK)):
    p = os.path.join(PACK, f)
    if os.path.isdir(p) or f in ('sources.json', 'LICENSE.txt', 'README.md'):
        continue
    m = model_of(f)
    e = {'file': f, 'bytes': os.path.getsize(p), 'sha256': sha(p), 'model': m}
    if f in SHARED: e['used_by'] = SHARED[f]
    if m in PH:
        aid, title, authors, repo, commit, mdir, srcdir, corr, credits = PH[m]
        e.update({'origin': 'Poly Haven', 'asset_id': aid, 'asset_title': title, 'source_url': 'https://polyhaven.com/a/' + aid,
                  'author': authors or 'Poly Haven contributor (name not readable from this environment; see the asset page)',
                  **CC0, 'mirror_repository': 'https://github.com/' + repo, 'mirror_commit': commit, 'mirror_dir': mdir,
                  'mirror_credits_file': credits, 'corroboration': corr,
                  'source_files': [{'file': k.split('/', 2)[2], 'sha256': v} for k, v in src_sha.items() if k.startswith('src/%s/' % srcdir) and not k.endswith('.md')],
                  'changes': CHANGES['common'] + (' ' + CHANGES[m] if m in CHANGES else '')})
        if m in AUTHOR_SOURCE: e['author_source'] = AUTHOR_SOURCE[m]
        if f in GENERATOR: e['generator'] = GENERATOR[f]
    else:
        e.update({'origin': 'Project-owned original work', 'source_url': 'https://github.com/AlKoda/Jewelry-store-vr-demo (generated for this repository; pack folder Prototype/ThirdParty/ForensicModels)',
                  'generator': PROJECT.get(m, ''), 'author': 'Project (generated with Claude Code, October 2026)',
                  'license': 'Project-owned; no third-party material', 'version': 'October 14, 2026',
                  'notes': 'Procedural geometry and textures; printed text rasterised with DejaVu fonts (Bitstream Vera / DejaVu licence, '
                           'glyph rendering only, no font file shipped); all names, case numbers and organisations are fictional.'})
    files.append(e)
for d in ('Source~',):
    for f in sorted(os.listdir(os.path.join(PACK, d))):
        p = os.path.join(PACK, d, f)
        files.append({'file': d + '/' + f, 'bytes': os.path.getsize(p), 'sha256': sha(p), 'origin': 'Project-owned original work',
                      'license': 'Project-owned', 'notes': 'Generator / conversion script (Unity ignores folders ending in ~).'})
doc = {'pack': 'ForensicModels', 'prepared': '2026-10-14', 'unverified_in_unity': True,
       'units': 'metres, Y up, OBJ/MTL (Unity imports without packages)',
       'texture_convention': {'Color': 'sRGB JPG (map_Kd in the MTL)', 'Normal': 'tangent-space, OpenGL +Y (Unity convention), JPG',
                              'MetallicSmoothness': 'PNG RGBA, R = metallic, G = B = 0, A = smoothness (linear)'},
       'models': {k: v for k, v in stats.items() if v.get('kind') == 'model'},
       'files': files}
json.dump(doc, open(os.path.join(PACK, 'sources.json'), 'w'), indent=1)
print('files', len(files), 'unassigned', [e['file'] for e in files if e.get('model') is None and not e['file'].startswith('Source~')])
