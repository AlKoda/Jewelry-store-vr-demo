"""Add the October 24, 2026 batch (second Poly Haven batch, build_ph2.py) to sources.json without touching the
existing entries. write_sources_ph2.py WORK PACK
WORK/src/_work/stats_ph2.json comes from build_ph2.py; WORK/src_original_sha256.txt lists the staged, unmodified
mirror files as "<sha256>  ./<Name>/<path>"."""
import sys, os, json, hashlib

W, PACK = sys.argv[1], sys.argv[2]
stats = json.load(open(os.path.join(W, 'src', '_work', 'stats_ph2.json')))
src_sha = {}
for line in open(os.path.join(W, 'src_original_sha256.txt')):
    h, p = line.split(None, 1); src_sha[p.strip().lstrip('./')] = h

CC0 = {'license': 'CC0-1.0', 'license_url': 'https://creativecommons.org/publicdomain/zero/1.0/',
       'publisher_license': 'https://polyhaven.com/license',
       'publisher_license_read_from': 'https://github.com/Poly-Haven/polyhaven.com/blob/b1a6aa13afc03ae00860f6ac5fe6d5daf8e95356/public/locales/en/license.json'}
GEADAS = ('Geadas11/Claude_steam_game', '8b6aa56f951450b7a05434615bcdda2bd2d1e8fa', 'assets/3d/models/%s/',
          'assets/3d/CREDITS.md ("Todos do Poly Haven, licenca CC0"; tools/fetch_polyhaven.py saves the api.polyhaven.com download unchanged)')
DOPESICK = ('andersanders806-dev/dopesick-game', 'e0b84f5eda44ac0e5fca605292cf6cac5aac3cce', 'assets/polyhaven/%s/',
            'README.md and dev-tools/fetch_polyhaven_models.py ("Download CC0 3D models from Poly Haven")')
TIDDYBUB = ('Tiddybub/3d-assets', '11ff7082e566a6278553bf7623980cb93dd41e27', 'sci-fi/%s/', 'sci-fi/chemistry_set/SOURCE.md ("License: CC0-1.0", source Poly Haven / Jiri Ptacek); README.md (every asset CC0)')
TABLETASK = 'github.com/omarrayyann/tabletask @ c59db35b6d7a40d328d5bbb2558ce0bf54d43e03, assets/sources/polyhaven.md (Poly Haven model credit table)'
QUALIA = 'github.com/duplighost/qualiacology @ 788d1c8bee0453d2713ff541621d573eb0c465f4, olly-olly/ASSET-LICENSES.md and olly-olly/assets/SOURCES.md (per-file records of the dl.polyhaven.org downloads)'
GENDARMERIA = 'github.com/sebasfavaron/gendarmeria-poc @ ff94bc01a1819c4b2b795d1e49bfe9f895221e55, CREDITOS.md'
BLACKSITE = 'github.com/ReidT205/blacksite-assets @ 1476a0ff2ee751ed4c161de5b0034c1aa9affaf0, ATTRIBUTION.md (Poly Haven CC0 1.0 table)'
BACKROOMS = 'github.com/PianoNic/BackroomsBaden @ 794e9165e268dfcaaa0a6bbbf51686491af110d9, client/public/models/LICENSES.md'
WROCLAW = 'github.com/chmajster/WroclawTheGame @ 72fd1f811a09a98c7f93d49f0d0864408ba29fc6, docs/FREE_MODEL_ASSETS.md'
FALLOUT = 'github.com/m4tthewdavis/fallout-minnesota @ 005554ade78794ea09b8da24dbb7aa6b5070b785, assets/models/metal_toolbox/ (1K copy; metal_toolbox.bin byte-identical) and assets/CREDITS.md (Mateusz Sadek)'
THELEVELS = 'github.com/VoxogoBrusch/the-levels @ 1e7c37785504d23d9a2f669b3b922eac14cedb01, public/assets/models/medical_tape/ (1K copy; medical_tape.bin byte-identical) and ASSET_LICENSES.md ("Poly Haven distributes these assets under CC0 1.0")'
INDEX = 'Poly Haven model index (api.polyhaven.com/assets?t=models, April 2024) mirrored in github.com/Akshay25798/VFX-Houdini-Asset_Browser @ a6bbb5f6bc3dda8852477dc4d035258685234084, downloads/json/models.json ("authors")'
NOT_CONFIRMED = 'not confirmed (no mirror credit file names the author; polyhaven.com is not reachable from the build machine; CC0 needs no attribution)'

PH = {
    # pack name: (asset id, title, authors, mirror (repo, commit, dir, credits), corroboration, author_source)
    'LiftingTapeRoll': ('medical_tape', 'Medical Tape', {'Miroslav Turura': 'All'}, GEADAS, [THELEVELS], TABLETASK),
    'ReagentSprayBottle': ('all_purpose_cleaner', 'All Purpose Cleaner', {'Kuutti Siitonen': 'All'}, GEADAS,
                           ['github.com/Tiddybub/3d-assets @ 11ff7082e566a6278553bf7623980cb93dd41e27, props/all_purpose_cleaner/ (listed, not fetched)'], INDEX + '; also ' + TABLETASK),
    'DentalStoneBag': ('cement_bag', 'Cement Bag', {'PierreB3D': 'All'}, DOPESICK,
                       ['github.com/EliteGamer007/6dof-drone-sim @ 3b970771, assets/polyhaven/models/cement_bag/ (listed, not fetched)'], GENDARMERIA + '; ' + BLACKSITE),
    'EvidenceBox': ('cardboard_box_01', 'Cardboard Box 01', {'Rahul Chaudhary': 'All'}, DOPESICK,
                    [QUALIA + ' (same dl.polyhaven.org 1K files)'], INDEX + '; ' + QUALIA + '; ' + GENDARMERIA + '; ' + BLACKSITE),
    'BurglarToolbox': ('metal_toolbox', 'Metal Toolbox', {'Mateusz Sadek': 'All'}, GEADAS, [FALLOUT], TABLETASK + '; ' + BLACKSITE + '; ' + WROCLAW + '; ' + FALLOUT),
    'BaseballBat': ('baseball_bat', 'Baseball Bat', {'Enrique Martín': 'All'}, DOPESICK, [QUALIA + ' (same dl.polyhaven.org 1K files)'], INDEX + '; ' + QUALIA),
    'RedScrewdriver': ('screwdriver', 'Screwdriver', {'PierreB3D': 'All'}, GEADAS, [], TABLETASK),
    'Lighter': ('vintage_lighter', 'Vintage Lighter', {'Slinc': 'All'}, GEADAS, [], TABLETASK),
    'Spectacles': ('round_spectacles', 'Round Spectacles', {'Sean Buckley': 'All'}, GEADAS,
                   ['github.com/tuniveza/Generative-Games @ 831f115caeea2201fdac5315bac4fcb707eeaf85, The-Ruins-of-Bezan/assets/models/round_spectacles/ (listed in CREDITS.md, not fetched)'], TABLETASK),
    'DroppedWatch': ('digital_wrist_watch', 'Digital Wrist Watch', {'Adrian C': 'All'}, GEADAS, [], TABLETASK + '; ' + BACKROOMS),
    'CigaretteButt': ('cigarette_case', 'Cigarette Case', NOT_CONFIRMED, GEADAS, [], None),
    'ReagentBottles': ('chemistry_set', 'Chemistry Set', {'Jiří Ptáček': 'All'}, TIDDYBUB, [], 'sci-fi/chemistry_set/SOURCE.md of the mirror; ' + BLACKSITE),
}
COMMON = ('Converted glTF -> OBJ/MTL (Tools/glb_to_obj.py --ground: lowest point y = 0, centred in x/z; metres kept), '
          'then Source~/build_ph2.py (October 24, 2026): base colour -> <Name>_Color.jpg; nor_gl -> <Name>_Normal.jpg (OpenGL/Unity '
          'convention); ARM (R=AO, G=rough, B=metal) -> <Name>_MetallicSmoothness.png (R = metal, G = B = 0, A = 1 - rough). '
          'OBJ numbers rounded to 0.1 mm. Glass sub-materials replaced by MTL constants.')
CHANGES = {
    'LiftingTapeRoll': 'Scaled x2 (tape width 0.025 -> 0.05 m, a 2 in lifting-tape roll). Mesh split by radius into materials lifting_tape '
                       '(d 0.9, for a Fade/Transparent material) and tape_core. Colour map rebuilt by Source~/relabel_ph2.py: tape faces as clear, '
                       'slightly translucent off-white film, core faces toned light kraft cardboard; tape smoothness raised to ~0.85 in the MS alpha. 2K maps -> 512/512/256.',
    'ReagentSprayBottle': 'Colour map rewritten by Source~/relabel_ph2.py: pink body -> neutral white polyethylene (red cap kept); the three printed '
                          'labels ("Planis" / "SUPER CLEANER+" leaf / back panel) painted out and two plain white labels reading "LATENT BLOOD REAGENT" '
                          'drawn (DejaVu Sans; fictional lot and text; no pictograms, no brand). Note: the source is a jug with a red flip cap, not a trigger sprayer. 2K maps -> 1024/512/512.',
    'DentalStoneBag': 'Colour map rebuilt from scratch by Source~/relabel_ph2.py: off-white/grey paper shaded by the source AO channel, with a plain '
                      'two-colour print "DENTAL STONE / forensic casting grade / 11 kg" (DejaVu Sans) on the top, underside, sides and ends. '
                      'No "CEMENT" print, brand or logo of the source remains. Normal and ARM maps unchanged (512).',
    'EvidenceBox': 'Decimated 16,952 -> 5,999 triangles (UV-exact, Source~/decimate_exact.py). Nothing added.',
    'BurglarToolbox': 'Lid and handle decimated per group (6,051 -> 4,499 and 3,682 -> 1,000; UV-exact, fast_simplification agg 3), body, tray and '
                      'latches kept: 14,228 -> 9,994 triangles. The round brand-style sticker on the front ("STEEL..." roundel) covered with paint '
                      'cloned from the same map (Source~/relabel_ph2.py). The box is modelled open (lid up, tray visible). 2K maps -> 1024/1024/512.',
    'BaseballBat': 'As is (2,672 triangles). 1K maps -> 1024/512/256.',
    'RedScrewdriver': 'As is (3,412 triangles), upright like FlatheadScrewdriver (handle at y = 0, tip up). The tip is a flat blade, so the model '
                      'is named by its red handle, not "Phillips". Handle wordmark ("LANSTEY") painted out (Source~/relabel_ph2.py). 2K maps -> 512/512/256.',
    'Lighter': 'As is (3,366 triangles, lid open). Engraved wordmark ("TROP (R)") on the case painted out (Source~/relabel_ph2.py). The '
               'vintage_lighter_alpha sub-material (60 triangles) keeps the colour map with d 1.0. 2K maps -> 512/512/256.',
    'Spectacles': 'Decimated 11,810 -> 9,000 triangles (UV-exact); lens sub-material round_spectacles_glass as MTL glass constants. 2K maps -> 512/512/256.',
    'DroppedWatch': 'Decimated 10,390 -> 9,002 triangles (UV-exact); crystal sub-material digital_wrist_watch_glass as MTL glass constants. Bezel '
                    'wordmark ("RISTO") and faint case-back engraving painted out; generic function labels kept (Source~/relabel_ph2.py). 2K maps -> 512/512/256.',
    'CigaretteButt': 'One loose cigarette (glTF node cigarette_case_cigarette_c, 798 triangles, 86 mm) extracted with trimesh; cut 25 mm from the '
                     'filter end with UV-interpolated clipping, the cut capped with a fan mapped to the source tobacco end, and the paper end '
                     'flattened by 45 % and bent 2.5 mm (crushed look); 192 triangles. The rest of the cigarette case is not shipped. 2K maps -> 512/256/256.',
    'ReagentBottles': 'Six glTF nodes extracted with trimesh (bottle_clear_a + lid, bottle_clear_b + cork, bottle_clear_d + cork: three stoppered '
                      'glass bottles standing in an L, 5 cm pitch), decimated 3,584 -> 2,897 triangles (UV-exact); glass as MTL constants '
                      '(chemistry_set_glass), corks keep the maps. The other 46 nodes of the set are not shipped. 1K maps -> 512/512/256.',
}
NEW_SCRIPTS = ['Source~/build_ph2.py', 'Source~/relabel_ph2.py', 'Source~/write_sources_ph2.py']


def sha(p):
    return hashlib.sha256(open(p, 'rb').read()).hexdigest()


doc = json.load(open(os.path.join(PACK, 'sources.json')))
names = set(PH)
doc['files'] = [e for e in doc['files'] if not (e.get('model') in names or e['file'] in NEW_SCRIPTS)]
for k in list(doc['models']):
    if k[:-4] in names:
        del doc['models'][k]
for k, v in stats.items():
    doc['models'][k] = v
doc['models'] = dict(sorted(doc['models'].items()))

new = []
for f in sorted(os.listdir(PACK)):
    base = os.path.splitext(f)[0]
    m = next((n for n in sorted(names, key=len, reverse=True) if base == n or base.startswith(n + '_')), None)
    if m is None or f.endswith('.meta'):
        continue
    p = os.path.join(PACK, f)
    aid, title, authors, (repo, commit, mdir, credits), corr, author_source = PH[m]
    e = {'file': f, 'bytes': os.path.getsize(p), 'sha256': sha(p), 'model': m, 'origin': 'Poly Haven', 'asset_id': aid, 'asset_title': title,
         'source_url': 'https://polyhaven.com/a/' + aid, 'author': authors, **CC0,
         'mirror_repository': 'https://github.com/' + repo, 'mirror_commit': commit, 'mirror_dir': mdir % aid, 'mirror_credits_file': credits,
         'corroboration': corr,
         'source_files': [{'file': k.split('/', 1)[1].replace(m + '.gltf', aid + ('_1k' if aid == 'cement_bag' and False else '') + '.gltf'), 'sha256': v}
                          for k, v in sorted(src_sha.items()) if k.startswith(m + '/')],
         'fetched': '2026-10-24', 'changes': COMMON + ' ' + CHANGES[m]}
    if author_source:
        e['author_source'] = author_source
    if m == 'CigaretteButt':
        e['source_nodes'] = ['cigarette_case_cigarette_c']
    if m == 'ReagentBottles':
        e['source_nodes'] = ['chemistry_set_bottle_clear_a', 'chemistry_set_bottle_clear_lid', 'chemistry_set_bottle_clear_b',
                             'chemistry_set_bottle_clear_b_cork', 'chemistry_set_bottle_clear_d', 'chemistry_set_bottle_clear_d_cork']
    new.append(e)
for s in NEW_SCRIPTS:
    p = os.path.join(PACK, s)
    new.append({'file': s, 'bytes': os.path.getsize(p), 'sha256': sha(p), 'origin': 'Project-owned original work', 'license': 'Project-owned',
                'notes': 'Generator / conversion script for the October 24, 2026 batch (Unity ignores folders ending in ~).'})
doc['files'] += new
doc['files'].sort(key=lambda e: (e['file'].startswith('Source~'), e['file']))
doc['batch_2026_10_24'] = {
    'added': '2026-10-24', 'status': 'not yet wired; unverified in Unity',
    'models': sorted(names),
    'pipeline': 'Source~/relabel_ph2.py (colour-map edits on staged copies) -> Source~/build_ph2.py (conversion, decimation, extraction, pack files) -> Source~/write_sources_ph2.py',
    'sources': 'Poly Haven CC0 glTF (1K or 2K) taken unmodified from the public GitHub mirrors named per file; polyhaven.com itself was not reachable from the build machine. '
               'Authors as given by the mirror credit files named in author_source; CigaretteButt has no author record.',
    'texture_rules': 'Colour maps 512 or 1024 px JPG q90; normal 256 to 1024 px JPG q90 (OpenGL +Y); MetallicSmoothness 256 to 512 px PNG (R metal, G = B = 0, A smoothness, sRGB off).',
}
json.dump(doc, open(os.path.join(PACK, 'sources.json'), 'w'), indent=1, ensure_ascii=False)
print('files', len(doc['files']), 'new', len(new), 'models', len(doc['models']))
