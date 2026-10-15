"""Assemble the InteriorModels pack: copy built files, write models.json, sources.json, README.md, LICENSE.txt, Source~.
make_docs.py BUILD_DIR PACK_DIR MIRROR_MODELS PH_REPORT PROC_REPORT PREVIEW_JPG [PREVIOUS_SOURCES_JSON]
Upstream source hashes are computed from MIRROR_MODELS; a Poly Haven model missing there keeps the entry of the
previous sources.json (under its old name when it was renamed, see pack_meta.RENAMED)."""
import sys, os, json, hashlib, shutil, re, glob
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pack_meta import *

BUILD, PACK, MIR, PHR, PRR, PREV = sys.argv[1:7]
PREVSRC = json.load(open(sys.argv[7])) if len(sys.argv) > 7 else None
HERE = os.path.dirname(os.path.abspath(__file__))
if os.path.exists(PACK): shutil.rmtree(PACK)
os.makedirs(os.path.join(PACK, 'Source~'))
for f in sorted(os.listdir(BUILD)):
    shutil.copy(os.path.join(BUILD, f), os.path.join(PACK, f))
for f in ['convert_ph.py', 'build_ph.py', 'edit_textures.py', 'gen_art.py', 'meshkit.py', 'proc_lib.py', 'proc_materials.py',
          'proc_office.py', 'proc_fixtures.py', 'render.py', 'pack_meta.py', 'make_docs.py', 'gen_text.py',
          'bake_gravel.py', 'leaf_cards.py', 'prep_print.py', 'render2.py', 'check_obj.py', 'disc_edge_check.py', 'cracks.py', 'grid.py', 'sheet.py']:
    shutil.copy(os.path.join(HERE, f), os.path.join(PACK, 'Source~', f))
shutil.copy(PREV, os.path.join(PACK, 'Source~', 'InteriorModels_Previews.jpg'))
sha = lambda p: hashlib.sha256(open(p, 'rb').read()).hexdigest()
ph = json.load(open(PHR)); pr = json.load(open(PRR))
DATE = '2026-10-14'

def model_files(name):
    files = [name + '.obj', name + '.mtl']
    for line in open(os.path.join(PACK, name + '.mtl')):
        p = line.split()
        if len(p) >= 2 and (p[0] == 'map_Kd' or (p[0] == '#' and p[1] in ('Normal', 'MetallicSmoothness'))):
            fn = p[-1]
            if fn not in files: files.append(fn)
    return files

models = {}
for name, rep in list(ph.items()) + list(pr.items()):
    cat, what, place = INFO[name]
    e = {'category': cat, 'what': what, 'placement': place, 'tris': rep['tris'], 'size_m': rep['size_m'], 'bounds_min': rep['min'],
         'bounds_max': rep['max'], 'pivot': rep['pivot'], 'materials': rep['materials'], 'files': model_files(name)}
    if name in PH:
        pid, title, authors, ver, ch = PH[name]
        e.update({'source': 'Poly Haven', 'source_id': pid, 'source_title': title, 'source_url': 'https://polyhaven.com/a/' + pid,
                  'author': authors, 'license': 'CC0-1.0', 'source_tris': rep.get('source_tris'), 'verified': ver, 'changes': COMMON_CHANGES + ch})
        if name in RENAMED: e['previous_name'] = RENAMED[name]
        geo = {k: rep[k] for k in ('nodes', 'backfaces', 'winding_flipped', 'winding_flipped_at_write') if k in rep}
        if geo: e['build_report'] = geo
        arts = [f for f in e['files'] if f in ART]
        if arts: e['artwork'] = {f: ART[f] for f in arts}
    else:
        e.update({'source': 'project-made (procedural, Source~/proc_office.py or proc_fixtures.py)', 'author': 'Project (generated October 14, 2026)',
                  'license': 'Project-owned (treat as CC0)'})
    models[name] = e
models = dict(sorted(models.items()))
json.dump({'pack': 'InteriorModels', 'prepared': DATE, 'revised': REVISED, 'unverified_in_unity': True,
           'conventions': {'units': 'metres, Y up, front faces +Z', 'pivot': {'floor': 'lowest point at y = 0, centred in x/z', 'wall': 'back face at z = 0, centred in x and y, faces +Z',
                           'ceiling': 'top at y = 0, hangs down (-Y)', 'none': 'authored origin, see what/placement'},
                           'maps': {'Color': 'sRGB base colour (map_Kd in the MTL)', 'Normal': 'tangent space, OpenGL +Y (Unity convention); import as Normal map',
                                    'MetallicSmoothness': 'RGBA PNG, R = G = B = metallic, A = smoothness; import with sRGB off; Standard Metallic slot, Source = Metallic Alpha'},
                           'constants': 'materials without maps carry Kd (and metallic/smoothness here); glass materials have d < 1 in the MTL',
                           'tiling': 'Shared_* textures are seamless; models using them have world-scale UVs (see the tiling note per material)'},
           'models': models}, open(os.path.join(PACK, 'models.json'), 'w'), indent=1)

# ------------------------------------------------------------------ sources.json
owners = {}
for name, e in models.items():
    for f in e['files']: owners.setdefault(f, []).append(name)
files = []
for f in sorted(os.listdir(PACK)):
    p = os.path.join(PACK, f)
    if os.path.isdir(p) or f in ('sources.json', 'README.md', 'LICENSE.txt', 'models.json'): continue
    ms = owners.get(f, [])
    phm = [m for m in ms if m in PH]
    if phm and not f.startswith('Shared_'):
        pid, title, authors, ver, ch = PH[phm[0]]
        if f in ART:
            a_ = ART[f]
            files.append(dict({'file': f, 'models': ms}, **{k: v for k, v in a_.items()}, modified=True, sha256=sha(p), bytes=os.path.getsize(p)))
            continue
        files.append({'file': f, 'models': ms, 'source_url': 'https://polyhaven.com/a/' + pid, 'mirror': MIRROR['repo'] + '/tree/' + MIRROR['commit'] + '/assets/3d/models/' + pid,
                      'version': 'Poly Haven 2K glTF via mirror commit ' + MIRROR['commit'][:12] + ', fetched ' + DATE + (', rebuilt ' + REVISED if phm[0] in REBUILT else ''),
                      'license': 'CC0-1.0', 'author': authors,
                      'modified': True, 'sha256': sha(p), 'bytes': os.path.getsize(p)})
    else:
        files.append({'file': f, 'models': ms, 'source_url': None, 'version': 'generated ' + DATE, 'license': 'Project-owned (treat as CC0)',
                      'author': 'Project', 'modified': False, 'sha256': sha(p), 'bytes': os.path.getsize(p)})
for f in sorted(os.listdir(os.path.join(PACK, 'Source~'))):
    p = os.path.join(PACK, 'Source~', f)
    files.append({'file': 'Source~/' + f, 'models': [], 'source_url': None, 'version': 'generated ' + DATE + ', revised ' + REVISED, 'license': 'Project-owned', 'author': 'Project',
                  'sha256': sha(p), 'bytes': os.path.getsize(p), 'note': ('meshkit.py adapted from Prototype/ThirdParty/ForensicModels/Source~/meshkit.py (project-owned)' if f == 'meshkit.py' else None)})
upstream = {}
for name, (pid, title, authors, ver, ch) in PH.items():
    d = os.path.join(MIR, pid); src = []
    if not os.path.isdir(d) and PREVSRC:
        upstream[name] = PREVSRC['upstream_models'][RENAMED.get(name, name)]; continue
    for root, _, fs in os.walk(d):
        for fn in sorted(fs):
            if fn.endswith('.import'): continue
            q = os.path.join(root, fn); src.append({'path': os.path.relpath(q, os.path.dirname(MIR.rstrip('/'))).replace(os.sep, '/'), 'sha256': sha(q), 'bytes': os.path.getsize(q)})
    upstream[name] = {'poly_haven_id': pid, 'title': title, 'author': authors, 'license': 'CC0-1.0', 'page': 'https://polyhaven.com/a/' + pid,
                      'mirror_repo': MIRROR['repo'], 'mirror_commit': MIRROR['commit'], 'verified_against_upstream': ver, 'source_files': src}
json.dump({'pack': 'InteriorModels', 'prepared': DATE, 'revised': REVISED, 'licence_poly_haven': LICENSE_PH,
           'licence_public_domain_painting': {'file': 'PictureFrameWhite_Artwork_Color.jpg', 'repo': 'https://github.com/pmilovanov/hermitage-art',
               'commit': 'b8ead49c010eac0c8366bafc4ffaaf1f1a447805', 'repo_licence': 'CC0 1.0 Universal (LICENSE file in the repository)',
               'status': 'public domain: Paul Cezanne died 1906; faithful photographic reproduction of a 2D work'}, 'mirror': MIRROR, 'hash_checks': CHECKS,
           'fonts_rasterised': 'DejaVu Sans / Sans Mono (Bitstream Vera licence), GNU FreeSans Oblique (GPL with font exception), only as rendered pixels in project textures; no font file shipped',
           'upstream_models': upstream, 'files': files}, open(os.path.join(PACK, 'sources.json'), 'w'), indent=1)
print('files', len(files))
