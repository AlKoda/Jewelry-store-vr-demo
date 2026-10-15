"""sources.json entries for the LOD variants: lod_sources.py PACKDIR REPORT.json  (REPORT = make_lods.py --report)
Adds/updates the files entries of the six LOD OBJs (bytes and SHA-256 from disk, licence and source of the model they come
from, derived_from, tool, target, tris, rules, VERIFY measures), a 'lods' list on each source model, the top-level
'lod_variants' note, and the entries of the new or changed Source~ files (this script included). Idempotent; standard
library only (python3 -I)."""
import sys, os, json, hashlib

PK, REP = sys.argv[1], sys.argv[2]
R = json.load(open(REP, encoding='utf-8'))
path = os.path.join(PK, 'sources.json')
S = json.load(open(path, encoding='utf-8'))
files = {e['file']: e for e in S['files']}
# Unity LODGroup screen-relative transition heights (LOD0, LOD1, LOD2; below the last one the object is culled).
# PoliceCar 0.30 and StreetLamp 0.60 for LOD1 are the wiring brief; the rest are suggestions. Unverified in Unity.
SWITCH = {'PoliceCar': (0.30, 0.10, 0.01), 'StreetLamp': (0.60, 0.15, 0.01), 'UtilityBox': (0.25, 0.10, 0.01)}


def sha(p):
    return hashlib.sha256(open(p, 'rb').read()).hexdigest()


for fn, r in sorted(R.items()):
    p = os.path.join(PK, fn)
    if sha(p) != r['sha256']:
        sys.exit('%s on disk differs from the report (rerun make_lods.py)' % fn)
    base = files[r['derived_from']]
    model, level = fn[:-4].rsplit('_', 1)
    kept = [g['g'] for g in r['groups'] if g['mode'] == 'copy']
    v = r['verify']
    files[fn] = dict(
        file=fn, bytes=os.path.getsize(p), sha256=r['sha256'], license=base['license'], source=base['source'],
        used_by=base['used_by'], derived_from=r['derived_from'], derived_from_sha256=base['sha256'],
        tool='Source~/make_lods.py (python3 -I, deterministic)', target=r['target'], tris=r['tris'],
        mtl=model + '.mtl (the original MTL, shared, not copied)',
        derivation='seam-aware half-edge quadric decimation per group (proportional share of the target); every vertex is '
                   'an original v/vt/vn of %s; groups under 64 tris and keep-list groups copied unchanged' % r['derived_from']
                   + ('; relaxed seams (%s, UV jump <= %g)' % (r['relax'], r['uv_max']) if r['relax'] else '; seam-exact'),
        keep=r['keep'], fixed=r['fixed'], kept_unchanged=kept,
        verify=dict(bounds_max_diff_m=v['bounds_max_diff_m'], group_box_max_diff_m=v['group_box_max_diff_m'],
                    open_edges_source_lod=v['open_edges'], facing_away_source_lod=v['facing_away'],
                    zero_area=v['zero_area'], relaxed_collapses=v['relaxed_collapses'],
                    **{k: x for k, x in v.items() if k.startswith(('box_diff_m', 'wheel_min_y', 'height_m'))}))
    m = S['models'][r['derived_from']]
    lods = [x for x in m.get('lods', []) if x['file'] != fn]
    sw = SWITCH[model]
    lods.append(dict(file=fn, tris=r['tris'], target=r['target'],
                     unity_lodgroup_transition=sw[1] if level == 'LOD1' else sw[2],
                     shown_below_screen_height=sw[0] if level == 'LOD1' else sw[1]))
    m['lods'] = sorted(lods, key=lambda x: x['file'])
    m['lod0_unity_lodgroup_transition'] = sw[0]
for f in ('Source~/make_lods.py', 'Source~/lod_sources.py', 'Source~/Preview_LODs.jpg', 'Source~/check_pack.py',
          'Source~/README_build.txt'):
    p = os.path.join(PK, f)
    e = files.get(f, dict(file=f, license='Project-owned', source='build script / preview render', used_by=[]))
    e.update(bytes=os.path.getsize(p), sha256=sha(p))
    files[f] = e
S['files'] = [files[k] for k in sorted(files)]
S['lod_variants'] = dict(
    status='wiring batch 2026-10-15, unverified in Unity (not imported, no LODGroup wired yet)',
    files=sorted(R), tool='Source~/make_lods.py', preview='Source~/Preview_LODs.jpg (software render, back faces culled)',
    materials='each LOD OBJ names the original <Model>.mtl; no MTL or texture was copied or changed',
    not_made='ApartmentBlock: seen at LOD0 from every eye position (14 m or more away); decimation would damage its flat facade planes',
    rules='see the make_lods.py docstring: seam-aware half-edge collapse (UV seams, hard edges and group boundaries shorten '
          'only along themselves, on all sides at once), group boxes kept exactly, no cracks, no inside-out folds over 1 cm2; '
          'LOD2 relaxes seams with an attribute (normal/UV) cost',
    unity_lodgroup_transition={k: list(v) for k, v in SWITCH.items()})
note = ('LOD variants (unverified in Unity): PoliceCar/StreetLamp/UtilityBox_LOD1 and _LOD2.obj from Source~/make_lods.py; '
        'they share the original MTLs; Source~/check_pack.py reads each OBJ\'s mtllib')
S['revision_2026_10_15'] = [x for x in S['revision_2026_10_15'] if not x.startswith('LOD variants')] + [note]
with open(path, 'w', encoding='utf-8', newline='\n') as fh:
    fh.write(json.dumps(S, indent=1, ensure_ascii=False) + '\n')
print('sources.json: %d files, LOD entries %s' % (len(S['files']), ', '.join(sorted(R))))
