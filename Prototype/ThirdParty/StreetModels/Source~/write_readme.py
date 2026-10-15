"""Write README.md and LICENSE.txt for StreetModels from sources.json.
usage: write_readme.py PACKDIR"""
import sys, os, json

PK = sys.argv[1]
S = json.load(open(os.path.join(PK, 'sources.json')))
models = S['models']
total = sum(os.path.getsize(os.path.join(dp, f)) for dp, _, fs in os.walk(PK) for f in fs)


def fmt_size(s):
    return ' x '.join('%.2f' % x for x in s)


def maps_of(m):
    out = []
    for mn, info in m['materials'].items():
        parts = []
        if info.get('color'):
            parts.append('C')
            if info['color'].split('_Color')[0] in ('Proc_PaintedMetal', 'Proc_Galvanised', 'Proc_CastIron'):
                parts.append('shared N+MS')
        if info.get('normal'):
            parts.append('N')
        if info.get('metallic_smoothness'):
            parts.append('MS')
        if not parts:
            parts.append('const')
        out.append('%s (%s)' % (mn, '+'.join(parts)))
    return '; '.join(out)


CATS = ['Street furniture', 'Signs', 'Building details', 'Planters and greenery', 'Buildings', 'Vehicles']
SRCNAME = {k: (v.get('title'), (', '.join(v['authors']) if 'authors' in v else v.get('author')), v['license']) for k, v in S['sources'].items()}

lines = []
A = lines.append
A('# StreetModels: realistic street and exterior models for the night jewellery-store block')
A('')
A('Street furniture, signs, building details, buildings across the street and parked vehicles for the night scene outside the 16 x 12 m store: pavement, kerb, wet street, street lamps, the police car at the kerb. Made for Unity 6000.6.2f1 with the Built-in pipeline and no packages (OBJ/MTL plus textures). Prepared %s. **Not imported or checked in Unity yet (unverified).**' % S['prepared'])
A('')
A('- 12 photoscanned/PBR models from **Poly Haven** (CC0), one of them (ApartmentBlock) assembled from the Modular Urban Apartments Facade kit.')
A('- 3 cars and 4 buildings by **Daniel Zhabotinsky** (CC BY 4.0, attribution required, see LICENSE.txt). Fictional makes; badges, number plates and a livery slogan were removed.')
A('- ApartmentBlock\'s lit/dark room backdrops (ApartmentBlock_Interior_Color.jpg) are night re-lights of three **CC0 Poly Haven interior HDRIs** (Lebombo, St. Fagans Interior, Empty Warehouse 01); the HDRIs themselves are not shipped.')
A('- 11 **project-made** procedural models (traffic light, bollard, cycle stand, signs, parking machine, gully grate, 1100 L container, bus shelter, LED light bar) with shared project-made textures. All names are fictional.')
A('- **Revised %s** after review: AirconGrille colour/alpha map, PottedPlant rebuilt (4,951 tris), new interior backdrops, DT_MaskedDetails cut-out PNG with double-sided fire escapes, MetalTrashCan/StreetBench colour maps at full brightness, zero-area triangles removed from all OBJs (see sources.json `revision_2026_10_15`).' % S['revised'])
A('- Total size **%.1f MB** (%s bytes, everything in this folder incl. `Source~`). Largest file %s (%.1f MB). Every file is under 3 MB.' % (
    total / 1e6, format(total, ','), max((f for f in S['files']), key=lambda x: x['bytes'])['file'], max(f['bytes'] for f in S['files']) / 1e6))
A('')
A('LICENSE.txt has the credits (copy the CC BY lines into the demo credits). sources.json has mirror commits, download URLs and SHA-256 of every source and shipped file. `Source~/` (ignored by Unity) holds the build scripts and `Preview.jpg`, a software render of every model.')
A('')
A('## Conventions')
A('')
A('| Item | Convention |')
A('|---|---|')
A('| Units / axes | Metres, Y up. Import with **Scale Factor 1**, Convert Units off. Unity\'s OBJ importer handles the handedness flip; lettering reads correctly. |')
A('| Pivot | Base centre on y = 0 unless the table says otherwise (wall-mounted items keep the authored wall-side origin). |')
A('| Facing | Street furniture and signs face **+Z** (towards the road/viewer). Cars: **forward = +X**. Buildings: see the table. |')
A('| `<Name>.obj/.mtl` | `map_Kd` points at the colour map. Unity\'s OBJ importer reads only Kd, `map_Kd` and `d`; assign the other maps in code (DemoAssetLibrary) or on the material. |')
A('| `_Color.jpg/.png` | Base colour, sRGB. Poly Haven colour maps have 50 % of the authored AO baked in. |')
A('| `_Normal.jpg` | Tangent-space normal, OpenGL +Y (matches Unity). Texture Type = **Normal map**. |')
A('| `_MetallicSmoothness.png` | R = metallic, G = B = 0, A = smoothness. **sRGB off**. Standard: Metallic slot, Source = Metallic Alpha, Smoothness scale 1 (same as ForensicModels, PBRExterior, NightMaterials). |')
A('| Materials without maps | Constant Kd/`d` in the MTL; the wanted metallic/smoothness/emission values are in sources.json (`models.<Name>.obj.materials`). |')
A('| Glass, lenses (`d` < 1) | Standard **Fade** (or Transparent), smoothness 0.9-0.95. |')
A('| Cut-out maps (`_Color.png` with alpha) | `AirconUnit_AirconGrille_Color.png` and `DT_MaskedDetails_Color.png`: Standard Rendering Mode **Cutout**, Alpha Cutoff 0.5. Texture import: Alpha Is Transparency on, **Mip Maps Preserve Coverage** on (cutoff 0.5) so thin wires do not vanish at distance. The MTL cannot say Cutout; set it on the material (DemoAssetLibrary). |')
A('| Double-sided parts | PottedPlant leaf blades and BuildingBrickCorner\'s DT_MaskedDetails cards (fire escapes, railings, wire) have their back faces duplicated in the OBJ, so the Standard shader (back-face culling) shows them from both sides; no Cull Off shader needed. |')
A('| Normals | Poly Haven props and cars carry vertex normals (Normals = Import). ApartmentBlock, the Downtown buildings and all procedural models have **no normals**: Normals = Calculate, smoothing angle 30-45. |')
A('| Shared textures | `DT_*` (Downtown buildings) and `Proc_*` (procedural props) are shared between several OBJs. `Proc_*_Normal.jpg` and `Proc_*_MetallicSmoothness.png` belong to the `Proc_PaintedMetal`, `Proc_Galvanised` and `Proc_CastIron` colour maps: every material whose `map_Kd` is `Proc_X_Color.jpg` takes `Proc_X_Normal.jpg` and `Proc_X_MetallicSmoothness.png` (the painted-metal colour is neutral; the MTL Kd tints it anthracite/black/yellow/green). Tile 1 UV = 1 m. |')
A('')
for cat in CATS:
    A('## ' + cat)
    A('')
    A('| Model | What it is | Real size (m, x × y × z) | Tris | Materials (C colour, N normal, MS metal/smooth) | Source / licence | Suggested placement in the night scene | Import notes |')
    A('|---|---|---|---|---|---|---|---|')
    for name, m in models.items():
        if m['category'] != cat:
            continue
        src = m['source']
        if src == 'project-made':
            st = 'Project-made (procedural)'
        else:
            t, a, l = SRCNAME[src]
            st = '%s, %s, %s' % (t, a, l.replace('-', ' ', 1) if l.startswith('CC0') else l)
        doc = json.load(open(os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'meta', 'docs_models.json')))['M'][name[:-4]]
        A('| **%s** | %s | %s | %s | %s | %s | %s | %s |' % (name[:-4], m['what'], fmt_size(m['size_m']), format(m['tris'], ','), maps_of(m), st, doc['place'], doc['notes'] or '-'))
    A('')
A('## Wiring ideas')
A('')
A('- **Street lighting:** StreetLamp every ~20 m on the store side with a warm Point light (range 12-15 m, intensity ~1.5) at y = 3.27 m and an emissive LampBulb; SecurityLight over the rear door with a narrow Spot light. Bake or use a few real-time lights only (Quest Link budget).')
A('- **Across the street:** ApartmentBlock directly opposite (interior emission gives lit and dark windows), BuildingNarrowOffice and BuildingBrickCorner beside it, BuildingBrickArcade / BuildingBrickResidential further away. Put RollerShutterWindow/RollerShutterDoor over the ground-floor openings of the Downtown buildings, AirconUnit on a few facades, StreetNamePlate on a corner.')
A('- **Kerb line:** Bollard row in front of the forced storefront (realistic anti-ram-raid detail), FireHydrant, SignNoStopping, StormDrainGrate in the wet gutter, ManholeCover on the road, ParkingMeter and BikeRack across the street, BusShelter with its lightbox lit, TrafficLight at the corner with only the red lens emissive.')
A('- **Vehicles:** PoliceCar at the kerb outside the store (forward +X), optionally with PoliceLightBar replacing the car\'s own bar for a modern look; Hatchback and SedanClassic parked across the street. The existing KayKit police car can stay as the primitive fallback.')
A('- **Service lane:** WasteContainer1100L and MetalTrashCan by the rear office side, UtilityBox against a wall.')
A('- **Fallbacks:** keep the primitive/KayKit fallbacks in DemoAssetLibrary; every model here is optional.')
A('- **Materials in code:** when DemoAssetLibrary builds Standard materials, switch AirconGrille and DT_MaskedDetails to Cutout (alpha 0.5) and give ApartmentBlock\'s Interior material its colour map as emission (x0.6-1.0).')
A('')
A('## Not found / not included')
A('')
A('- No CC0/CC BY realistic **trees** small enough for VR (Poly Haven trees are 30k-17M tris with heavy alpha cards); PottedPlant is the only greenery. A procedural or billboard street tree is still open.')
A('- No redistributable realistic **ambulance or forensic van**, **mailbox** or **newspaper box** without brands; the Zhabotinsky "Urban Stuff/Garbage" prop sets (CC BY) exist only as Unity-imported .asset meshes in large game repositories and were not fetched.')
A('- The police car is 1980s-styled (the only CC BY police car with fictional livery found); PoliceLightBar gives it or the KayKit car a modern LED bar.')
open(os.path.join(PK, 'README.md'), 'w').write('\n'.join(lines) + '\n')

# ---------------------------------------------------------------- LICENSE.txt
L = []
B = L.append
B('StreetModels - street and exterior models for the night jewellery-store scene')
B('Prepared %s. Not yet imported or checked in Unity.' % S['prepared'])
B('')
B('This folder mixes three licences. Per-file licence, source URL, mirror commit and SHA-256 are in sources.json.')
B('  1. Poly Haven models: CC0 1.0 (public domain dedication). No attribution required; credits below are a courtesy.')
B('  2. Daniel Zhabotinsky models: CC BY 4.0. ATTRIBUTION IS REQUIRED wherever the demo is shared (credits screen / docs).')
B('  3. Project-made procedural models and textures: owned by this project, no third-party content.')
B('')
B('CC0 1.0:   https://creativecommons.org/publicdomain/zero/1.0/')
B('CC BY 4.0: https://creativecommons.org/licenses/by/4.0/')
B('')
B('=' * 79)
B('1. Poly Haven (CC0 1.0)')
B('=' * 79)
B('Publisher licence: https://polyhaven.com/license, read from the site source github.com/Poly-Haven/polyhaven.com')
B('@ b1a6aa13afc03ae00860f6ac5fe6d5daf8e95356, public/locales/en/license.json:')
B('  "Our assets are all licensed as CC0, which is effectively Public Domain ..."')
B('  "You can use our assets for any purpose, including commercial work."')
B('  "You do not need to give credit or attribution when using them (although it is appreciated)."')
B('  "You can redistribute them, share them around, include them when sharing your own work, or even in a product you sell."')
B('polyhaven.com is not reachable from the build container, so the unmodified 1K glTF files were taken from public GitHub')
B('repositories that ship them (each repository\'s own licence covers its code only). Where possible every file was checked')
B('byte-for-byte against SHA-256 values that other projects recorded for the dl.polyhaven.org originals (see integrity_check).')
B('Authors are from the Poly Haven API listing (copy in github.com/Akshay25798/VFX-Houdini-Asset_Browser downloads/json/models.json).')
B('')
for name, m in models.items():
    src = m['source']
    if src == 'project-made' or S['sources'][src]['license'] != 'CC0-1.0':
        continue
    s = S['sources'][src]
    B('%s' % ', '.join(f for f in m['files'] if not f.startswith('DT_')))
    B('  "%s" by %s - %s - CC0 1.0' % (s['title'], ', '.join(s['authors']), s['page']))
    B('  Mirror: %s @ %s, %s' % (s['mirror_repository'], s['mirror_commit'], s['mirror_path']))
    B('  Integrity: %s' % s['integrity_check'])
    B('  Changes: %s' % m['changes'])
    B('')
B('ApartmentBlock_Interior_Color.jpg (revision 2026-10-15) is derived from three CC0 Poly Haven HDRIs (not shipped):')
for k in ('hdri_lebombo', 'hdri_st_fagans_interior', 'hdri_empty_warehouse_01'):
    s = S['sources'][k]
    B('  "%s" by %s - %s - CC0 1.0' % (s['title'], ', '.join(s['authors']), s['page']))
    B('    Mirror: %s @ %s, %s' % (s['mirror_repository'], s['mirror_commit'], s['mirror_path']))
    B('    Integrity: %s' % s['integrity_check'])
B('  Changes: perspective crops (75 deg) re-lit for night, curtains/blinds/TV/dark-room overlays and soft focus by')
B('  Source~/make_interior.py (project-made processing). The result stays CC0-compatible; no attribution required.')
B('')
B('=' * 79)
B('2. Daniel Zhabotinsky (CC BY 4.0) - attribution required')
B('=' * 79)
B('Licence evidence: the license.txt that Sketchfab ships with each download ("license type: CC-BY-4.0 ... Author must be')
B('credited. Commercial use is allowed.") and the licence field Sketchfab writes into each glTF (asset.extras.license).')
B('Sketchfab is not reachable from the build container; the files come from the GitHub repositories named below.')
B('All makes and marques are fictional; the parody badge atlases were not shipped (badge meshes removed), number plates were')
B('replaced by a plain plate colour, and a film-quote slogan was painted out of the police livery.')
B('')
B('Credit lines to copy (as requested by the author):')
done = set()
for name, m in models.items():
    src = m['source']
    if src == 'project-made' or S['sources'][src]['license'] != 'CC-BY-4.0' or src in done:
        continue
    done.add(src)
    s = S['sources'][src]
    users = [n[:-4] for n, mm in models.items() if mm['source'] == src]
    B('')
    B('* %s' % s['attribution'])
    B('  Used for: %s' % ', '.join(users))
    B('  Mirror: %s @ %s, %s' % (s['mirror_repository'], s['mirror_commit'], s['mirror_path']))
    B('  Licence evidence: %s' % s['license_evidence'])
    for u in users:
        B('  Changes (%s): %s' % (u, models[u + '.obj']['changes']))
B('')
B('Short form for a credits screen:')
B('  Police sedan, hatchback, 1988 sedan and downtown buildings by Daniel Zhabotinsky (sketchfab.com/DanielZhabotinsky), CC BY 4.0, modified.')
B('')
B('=' * 79)
B('3. Project-made (owned by this project)')
B('=' * 79)
proj = [n[:-4] for n, m in models.items() if m['source'] == 'project-made']
B('Models: ' + ', '.join(proj) + '.')
B('Textures: Proc_PaintedMetal_*, Proc_Galvanised_*, Proc_CastIron_*, Proc_Signs_Color.jpg, Proc_Lightbox_Color.jpg.')
B('Generated with numpy/Pillow/trimesh by the scripts in Source~ (proc_models.py, geom.py, proc_tex.py).')
B(S['project_made']['font_note'])
B('The build and fix scripts in Source~ (conversion, PottedPlant rebuild, AirconGrille and DT_MaskedDetails maps,')
B('interior re-lighting) are project-made; they ship no third-party content.')
B('Road-sign symbols follow the generic Vienna-convention designs (no-stopping disc, one-way arrow); no logos, brands or')
B('real place names. Source~/Preview.jpg is a software render of the models in this folder.')
open(os.path.join(PK, 'LICENSE.txt'), 'w').write('\n'.join(L) + '\n')
print('README.md and LICENSE.txt written; total bytes', total)
