"""Write sources.json, LICENSE.txt and README.md for the StreetModels pack.
usage: write_docs.py PACKDIR SCRATCHROOT"""
import sys, os, json, hashlib, glob, re

PK, SC = sys.argv[1], sys.argv[2]
LOG = json.load(open(os.path.join(SC, 'build', '.build_log.json')))
DATE = '2026-10-14'
CC0 = ('CC0-1.0', 'https://creativecommons.org/publicdomain/zero/1.0/')
CCBY = ('CC-BY-4.0', 'https://creativecommons.org/licenses/by/4.0/')
RAW = 'https://raw.githubusercontent.com/%s/%s/%s'
RAISIM = 'raisimTech/raisim2Lib @ 94cdaa6b66f31b61e45ee0dd1efcb2c46a9498b4, rsc/city/sources.json (SHA-256 of the dl.polyhaven.org files)'
NVREF = 'codejeet/NVMatrixEngine @ aa777402961cb78ee6861658ab46fc014ebf763a, engine/assets/neon-night/assets.json (SHA-256 pinned per dl.polyhaven.org URL)'


def sha(p):
    return hashlib.sha256(open(p, 'rb').read()).hexdigest()


def ph(id_, title, authors, repo, commit, path, local, verify):
    files = []
    for f in sorted(glob.glob(os.path.join(SC, 'src', local, '**', '*'), recursive=True)):
        if os.path.isfile(f):
            rel = os.path.relpath(f, os.path.join(SC, 'src', local))
            files.append(dict(file=rel, download_url=RAW % (repo, commit, path + '/' + rel), sha256=sha(f), bytes=os.path.getsize(f)))
    return dict(provider='Poly Haven', asset_id=id_, title=title, authors=authors, page='https://polyhaven.com/a/' + id_,
                original_download='https://dl.polyhaven.org/file/ph-assets/Models/gltf/1k/%s/%s_1k.gltf (1K glTF)' % (id_, id_),
                license=CC0[0], license_url=CC0[1], publisher_license='https://polyhaven.com/license',
                publisher_license_text='github.com/Poly-Haven/polyhaven.com @ b1a6aa13afc03ae00860f6ac5fe6d5daf8e95356, public/locales/en/license.json: "Our assets are all licensed as CC0"; "You can use our assets for any purpose, including commercial work"; "You do not need to give credit"; "You can redistribute them"',
                mirror_repository='https://github.com/' + repo, mirror_commit=commit, mirror_path=path,
                mirror_note="the mirror repository's own licence covers its code only; these are the unmodified Poly Haven CC0 files",
                integrity_check=verify, source_files=files)


def zh(title, sketchfab, repo, commit, path, local, license_file, extra=''):
    files = []
    for f in sorted(glob.glob(os.path.join(SC, 'src', local, '**', '*'), recursive=True)):
        if os.path.isfile(f):
            rel = os.path.relpath(f, os.path.join(SC, 'src', local))
            files.append(dict(file=rel, download_url=RAW % (repo, commit, path + '/' + rel), sha256=sha(f), bytes=os.path.getsize(f)))
    return dict(provider='Sketchfab (via GitHub mirror)', title=title, author='Daniel Zhabotinsky', author_url='https://sketchfab.com/DanielZhabotinsky',
                page=sketchfab, license=CCBY[0], license_url=CCBY[1],
                attribution='This work is based on "%s" (%s) by Daniel Zhabotinsky (https://sketchfab.com/DanielZhabotinsky) licensed under CC-BY-4.0 (http://creativecommons.org/licenses/by/4.0/)' % (title, sketchfab),
                license_evidence=license_file + extra,
                mirror_repository='https://github.com/' + repo, mirror_commit=commit, mirror_path=path, source_files=files)


FAB = ('rawprogress/fable-cities', 'aea8b1035030952555395de0c1de14ba693a1427', 'public/assets/shared/models')
EG = ('EliteGamer007/6dof-drone-sim', '3b97077101bedb1326cf6483e04dc51ccf130fa3', 'assets/polyhaven/models')
NV = ('codejeet/NVMatrixEngine', 'aa777402961cb78ee6861658ab46fc014ebf763a', 'engine/assets/neon-night/sources')
SRC = {
    'street_lamp_01': ph('street_lamp_01', 'Street Lamp 01', ['Josh Dean'], FAB[0], FAB[1], FAB[2] + '/street_lamp_01', 'fable/street_lamp_01',
                         'all 5 files byte-identical to the dl.polyhaven.org hashes in ' + RAISIM),
    'fire_hydrant': ph('fire_hydrant', 'Fire Hydrant', ['Gonçalo Felício'], FAB[0], FAB[1], FAB[2] + '/fire_hydrant', 'fable/fire_hydrant',
                       'all 8 files byte-identical to the dl.polyhaven.org hashes in ' + RAISIM + ' and in ' + NVREF),
    'metal_trash_can': ph('metal_trash_can', 'Metal Trash Can', ['GurJas Studios'], FAB[0], FAB[1], FAB[2] + '/metal_trash_can', 'fable/metal_trash_can',
                          'all 8 files byte-identical to the dl.polyhaven.org hashes in ' + RAISIM),
    'modular_street_seating': ph('modular_street_seating', 'Modular Street Seating', ['Stuart Attenborrow'], FAB[0], FAB[1], FAB[2] + '/modular_street_seating',
                                 'fable/modular_street_seating', 'all 14 files byte-identical to the dl.polyhaven.org hashes in ' + RAISIM),
    'potted_plant_01': ph('potted_plant_01', 'Potted Plant 01', ['Rico Cilliers'], FAB[0], FAB[1], FAB[2] + '/potted_plant_01', 'fable/potted_plant_01',
                          'no second copy found; the mirror\'s info.json names Poly Haven, Rico Cilliers and CC0 (matches the Poly Haven API listing)'),
    'water_manhole_cover': ph('water_manhole_cover', 'Water Manhole Cover', ['Raunox'], EG[0], EG[1], EG[2] + '/water_manhole_cover', 'eg/water_manhole_cover',
                              'all 5 files byte-identical to the dl.polyhaven.org hashes in ' + RAISIM),
    'rollershutter_door': ph('rollershutter_door', 'Rollershutter Door', ['MP'], EG[0], EG[1], EG[2] + '/rollershutter_door', 'eg/rollershutter_door',
                             'buffer and all 4 textures byte-identical to the copy in github.com/rsasaki0109/RobotNativeEngine @ f614389425a1e7d7005fbf188226a99dacb82f6b (assets/props/polyhaven_warehouse/rollershutter_door)'),
    'rollershutter_window_01': ph('rollershutter_window_01', 'Rollershutter Window 01', ['MP'], EG[0], EG[1], EG[2] + '/rollershutter_window_01', 'eg/rollershutter_window_01',
                                  'same mirror and naming as the verified rollershutter_door; no second copy compared'),
    'security_light': ph('security_light', 'Security Light', ['Maximilian Schuster'], 'ebdevStudent/hackPHS', 'c64330bbe7d782aab42d40e503a1afdd8734adde',
                         'static/assets/models/security_light', 'eg/security_light',
                         'all 3 textures byte-identical to the copy in github.com/HHSOLL/DeskteriorOnline @ 6102359dba9fd95f599408be1873abaa86e20730 (that copy has a re-packed buffer; the 141 kB original buffer was used)'),
    'utility_box_01': ph('utility_box_01', 'Utility Box 01', ['James Ray Cock'], NV[0], NV[1], NV[2] + '/utility_box_01', 'nv/utility_box_01',
                         'all 5 files byte-identical to the dl.polyhaven.org hashes in ' + NVREF),
    'exterior_aircon_unit': ph('exterior_aircon_unit', 'Exterior Aircon Unit', ['Monsta3D'], NV[0], NV[1], NV[2] + '/exterior_aircon_unit', 'nv/exterior_aircon_unit',
                               'all 14 files byte-identical to the dl.polyhaven.org hashes in ' + NVREF),
    'modular_urban_apartments_facade': ph('modular_urban_apartments_facade', 'Modular Urban Apartments Facade', ['James Ray Cock'], 'vincentyenn/interactive-portfolio',
                                          '410905e7ca904beed3c4caf2e709bb53e9183db2', 'public/models/loft-city', 'vy/modular_urban_apartments_facade',
                                          'geometry buffer (8,168,772 bytes) byte-identical to the dl.polyhaven.org hash in ' + RAISIM + '; the mirror\'s SOURCE.txt states the files are unmodified 1K Poly Haven files'),
    'police_sedan': zh('80 American Police Sedan - Low poly model', 'https://sketchfab.com/3d-models/80-american-police-sedan-low-poly-model-945fd09f513f4d1ab19b6388a7ea7323',
                       'CloudDCrow/space-drive', '8a7e7f1e17844877219670817b6d032a76a26b53', 'public/models/popo', 'zh/police_sedan',
                       'Sketchfab license.txt shipped beside the model (license type: CC-BY-4.0) and asset.extras.license in scene.gltf'),
    'ace11': dict(zh("Ace '11 - Low Poly model", 'https://sketchfab.com/3d-models/ace-11-low-poly-model-055ff8a21b8d4d279debca089e2fafcd',
                     'austencloud/flow-arts-composer', 'bec387c891edf3b8d2fac0842e2e7c417470e033', 'static/models/flow-fest/cars', 'flow',
                     'asset.extras.license in the GLB (CC-BY-4.0); the mirror\'s CREDITS.md (CC BY 4.0, web-optimised copy: WebP textures, meshopt compression, light simplification);',
                     ' independent second statement for the same Sketchfab model id 055ff8a21b8d4d279debca089e2fafcd: github.com/scenemax3d/scenemax3d-desktop resources-basic/resources/Models/modern_hatchback/license.txt (CC-BY-4.0)')),
    'sedan88': zh('88 American Sedan - Low poly model', 'https://sketchfab.com/3d-models/88-american-sedan-low-poly-model-8509a7f82665442592627940b7a1cfab',
                  'josiahthr/FinalGame', '9c3fb66495210c8b9dccc3b5f28ba7633d59af53', 'Objects/88_american_sedan_-_low_poly_model', 'zh/sedan88',
                  'Sketchfab license.txt shipped beside the model (license type: CC-BY-4.0) and asset.extras.license in scene.gltf'),
    'downtown': zh('Downtown Buildings Set - Low Poly model', 'https://sketchfab.com/3d-models/downtown-buildings-set-low-poly-model-7378e7fb9c914c39880d9913a6f4e1d6',
                   'Orchemi/threejs-lab', '2bcbfee4539e745662980c48d4b721faeace1b31', 'public/models/test2', 'zh/downtown',
                   'Sketchfab license.txt shipped beside the model (license type: CC-BY-4.0) and asset.extras.license in scene.gltf'),
}
# flow dir contains decoded intermediates too: keep only the GLB as the source file
SRC['ace11']['source_files'] = [f for f in SRC['ace11']['source_files'] if f['file'] == 'ace-hatchback.glb']
SRC['ace11']['mirror_path'] = 'static/models/flow-fest/cars/ace-hatchback.glb'
SRC['ace11']['title_note'] = "The same Sketchfab model id (055ff8a21b8d4d279debca089e2fafcd) was earlier titled 'Modern Hatchback - Low Poly model'"



def phh(id_, title, authors, page_note):
    rel = 'hdri/%s_1k.hdr' % id_
    f = os.path.join(SC, 'src', 'drei', rel)
    return dict(provider='Poly Haven (formerly HDRI Haven)', asset_id=id_, title=title, authors=authors, page='https://polyhaven.com/a/' + id_,
                original_download='https://dl.polyhaven.org/file/ph-assets/HDRIs/hdr/1k/%s_1k.hdr (1K Radiance HDR)' % id_,
                license=CC0[0], license_url=CC0[1], publisher_license='https://polyhaven.com/license',
                publisher_license_text='see the Poly Haven entry above (all Poly Haven assets are CC0)',
                mirror_repository='https://github.com/pmndrs/drei-assets', mirror_commit=DREI, mirror_path=rel,
                mirror_note="drei-assets README: 'HDRIs from [HDRI Haven](https://hdrihaven.com)' (HDRI Haven is Poly Haven's former name); the repository's own licence covers its code only",
                integrity_check=page_note,
                authors_source='Poly Haven API listing copy: github.com/Akshay25798/VFX-Houdini-Asset_Browser @ a6bbb5f6bc3dda8852477dc4d035258685234084, downloads/json/hdris.json',
                used_for='ApartmentBlock_Interior_Color.jpg (perspective crops re-lit for night by Source~/make_interior.py); the HDRI itself is not shipped',
                source_files=[dict(file=rel, download_url=RAW % ('pmndrs/drei-assets', DREI, rel), sha256=sha(f), bytes=os.path.getsize(f))])


DREI = '456060a26bbeb8fdf79326f224b6d99b8bcce736'
SRC['hdri_lebombo'] = phh('lebombo', 'Lebombo', ['Greg Zaal'],
                          'byte-identical copy in github.com/ChrisCrossCrash/ck-assets @ 39eeab5f79c7f60c0b07655c810da1638bdc1019 (assets/hdri); MD5 eb75d6df7d824e83940e2c7f2ef65c70 also recorded by an unrelated Godot project (github.com/eugenood/emobotics .import/lebombo_1k.hdr-*.md5)')
SRC['hdri_st_fagans_interior'] = phh('st_fagans_interior', 'St. Fagans Interior', ['Morgan Evans'],
                                     'byte-identical copy in github.com/ChrisCrossCrash/ck-assets @ 39eeab5f79c7f60c0b07655c810da1638bdc1019 (assets/hdri); no independent hash found')
SRC['hdri_empty_warehouse_01'] = phh('empty_warehouse_01', 'Empty Warehouse 01', ['Sergej Majboroda'],
                                     'byte-identical copy in github.com/ChrisCrossCrash/ck-assets @ 39eeab5f79c7f60c0b07655c810da1638bdc1019 (assets/hdri); no independent hash found')

PROJECT = dict(provider='Project-made (procedural, this repository)', license='Project-owned original work; no third-party content',
               tools='Source~/proc_models.py, Source~/geom.py, Source~/proc_tex.py (numpy, Pillow, trimesh)',
               font_note='Sign lettering is rendered into Proc_Signs_Color.jpg and Proc_Lightbox_Color.jpg with Barlow SemiBold (SIL OFL 1.1, already in Prototype/ThirdParty/Fonts); the font itself is not included here. All names are fictional (Kestrel Street, Old Town - District 4, "City of Lights").')

# ---------------------------------------------------------------- per-model description
M = {}


def model(name, src, cat, what, place, notes, changes):
    M[name] = dict(src=src, cat=cat, what=what, place=place, notes=notes, changes=changes)


PHC = 'Converted 1K glTF -> OBJ/MTL; meshoptimizer simplification (UV seams kept); textures re-encoded: Color = base colour x (0.5 + 0.5 AO from the ARM map), Normal = OpenGL normal (unchanged content, resized), MetallicSmoothness = R metal, A = 1 - roughness'
model('StreetLamp', 'street_lamp_01', 'Street furniture', 'Classic cast-iron lamp post with a four-sided glazed lantern head (glass and bulb are separate sub-materials).',
      'Pavement edge, 0.5 m in from the kerb, every 20-25 m; one just left of the store front. Put a Unity Point/Spot light at the lantern (y = 3.27 m).',
      'LampGlass -> Standard Fade/Transparent; LampBulb -> emission.', PHC + '; 30,610 -> 5,373 tris (revision 2026-10-15: 2 zero-area triangles removed).')
model('FireHydrant', 'fire_hydrant', 'Street furniture', 'Weathered red pillar fire hydrant with chained caps (the "aged" variant of the Poly Haven lineup).',
      'Pavement near the kerb, a few metres from the store entrance; keep it clear of parked cars.', '', PHC + '; aged variant only; 43,158 -> 3,809 tris (revision 2026-10-15: 348 zero-area triangles removed).')
NOAO = ('Color = base colour only: this asset\'s ARM map has an empty AO channel, so no AO is baked (revision 2026-10-15: the first '
        'conversion multiplied it in and shipped the colour map at half brightness)')
model('MetalTrashCan', 'metal_trash_can', 'Street furniture', 'Galvanised steel dustbin with handles, its lid leaning against the side (as authored).',
      'Side alley, next to the bus shelter, or at a shop door across the street.', '', PHC.replace('Color = base colour x (0.5 + 0.5 AO from the ARM map)', NOAO) + '; clean (non-rust) variant; 6,428 -> 2,345 tris (revision 2026-10-15: 11 zero-area triangles removed).')
model('StreetBench', 'modular_street_seating', 'Street furniture', 'Timber-slat street bench with cast-iron legs, backrest and arm rests (one straight 2.4 m module).',
      'Pavement across the street or at the seating corner outside; back to a facade or planter.', '', PHC.replace('Color = base colour x (0.5 + 0.5 AO from the ARM map)', NOAO) + '; straight-bench nodes of the kit only; 25,156 (kit) -> 3,931 tris (revision 2026-10-15: 110 zero-area triangles removed).')
model('ManholeCover', 'water_manhole_cover', 'Street furniture', 'Round cast-iron water manhole cover in its frame (generic lettering).',
      'Road surface or pavement; its top is 4 cm above y = 0 (sink it 3-4 cm into the asphalt).', 'Not grounded: authored origin kept (frame bottom at y = -0.03).', PHC + '; 6,301 -> 1,891 tris (revision 2026-10-15: 1 zero-area triangles removed).')
model('UtilityBox', 'utility_box_01', 'Street furniture', 'Grey-blue street electrical cabinet on a concrete plinth with warning stickers (generic "DANGER" sign).',
      'Back of the pavement against a building, or beside the traffic light.', '', PHC + '; 4,404 -> 3,082 tris.')
model('AirconUnit', 'exterior_aircon_unit', 'Building details', 'Split-system outdoor air-conditioning unit on wall brackets with condensate pipe (non-rusted variant).',
      'On facades across the street (2.5-6 m up), on the rear office wall, or on flat roofs.',
      'Not grounded/centred: the unit sits on y = 0 with the drain pipe hanging to y = -0.32; back (wall side) at z = -0.22, fan grille faces +Z. AirconGrille: AirconUnit_AirconGrille_Color.png is RGBA (alpha = the Poly Haven opacity mask; it cuts the rear coil-guard wire mesh) -> Standard Rendering Mode Cutout, Alpha Cutoff 0.5; texture import: Alpha Is Transparency on, Mip Maps Preserve Coverage on (cutoff 0.5).',
      PHC + '; 18,986 (two variants) -> 3,174 tris (revision 2026-10-15: 148 zero-area triangles removed). AirconGrille colour map (revision 2026-10-15): Poly Haven ships no colour map for this material (its glTF uses the opacity mask as base colour), so Source~/aircon_grille.py builds it: A = exterior_aircon_unit_01_opacity_1k.jpg at 512 px (cut-out coverage kept, 0.29 -> 0.30), RGB = aluminium for metallic texels (condenser fins), dark grey plastic for the fan blades, dark coated steel for the rear wire guard, times cavity shading from the authored normal map and grime from the roughness channel; the AO bake is skipped (empty AO channel).')
PHN = 'Converted 1K glTF -> OBJ/MTL (no simplification); textures re-encoded: Color = base colour x (0.5 + 0.5 AO from the ARM map), Normal = OpenGL normal (resized), MetallicSmoothness = R metal, A = 1 - roughness'
model('RollerShutterDoor', 'rollershutter_door', 'Building details', 'Closed steel roller shutter for a 1 m door, with box housing (plain variant). RollerShutterDoor_Graffiti_Color.jpg is the authored tagged variant (same UVs, swap the albedo).',
      'Closed shop doors across the street, the rear service door of the store.', 'Back face at z = 0, front +Z. The graffiti texture contains tags and a German quote, no brands.', PHN + '; plain variant geometry, both colour maps kept; 552 tris.')
model('RollerShutterWindow', 'rollershutter_window_01', 'Building details', 'Closed steel roller shutter for a 2.1 m shop window.',
      'Shop windows of the buildings across the street (night: all shops shuttered).', 'Back face at z = 0, front +Z.', PHN + '; plain variant only; 552 tris.')
model('SecurityLight', 'security_light', 'Building details', 'Wall-mounted industrial security light with frosted dome (glass and lamp as sub-materials).',
      'Above the rear service door, alley walls, side of the store.', 'Not grounded/centred: wall mount at z = -0.03, light hangs below the origin (to y = -0.36). Glass -> Fade; Bulb -> emission + a Spot light.', PHC + '; 4,668 -> 2,800 tris.')
model('PottedPlant', 'potted_plant_01', 'Planters and greenery', 'Terracotta urn with a leafy shrub: 91 modelled leaf blades on 91 leaf stalks and a branching stem (real geometry, no alpha), gravel top.',
      'Pairs flanking shop entrances across the street or the store entrance; seating corner.',
      'Pivot at the pot base centre. Two materials, two OBJ groups (Pot: pot + stem + gravel disc; Leaves: blades + stalks). Leaf blades are double-sided in the mesh (back faces duplicated), so the Standard shader needs no Cull Off. No zero-area triangles.',
      'Revision 2026-10-15, rebuilt within the 6k small-prop budget (was 11,662 tris incl. 1,650 zero-area): leaf mesh split into its 182 parts; the 91 blades simplified one by one with meshoptimizer 0.22 (seam-aware, budget raised per blade until >= 90 % of its area is kept: 9-23 tris each) and made double-sided (2,362 tris); the 91 thin tubular stalks rebuilt as 3-sided, 2-segment tubes on their fitted centrelines with their measured radius (0.8-4.1 mm) and their own UV strip (1,092 tris); pot 5,440 -> 1,000 and stem 7,008 -> 425 with meshoptimizer; the 508-pebble layer (56,878 tris) replaced by a 72-tri domed disc mapped onto the photographic gravel patch that the Poly Haven pot atlas already contains; zero-area slivers (40) and duplicate triangles removed; the 30 stem slivers that the simplification had folded inwards (back-facing from outside, unlike the original stem) re-wound to match their vertex normals (Source~/fix_stem_winding.py). Total 4,951 tris. Source~/plant_split.py, simplify_adaptive.mjs, plant_build.py. Textures unchanged: Color/Normal/MetallicSmoothness from the 1K maps (roughness map, no AO).')
model('ApartmentBlock', 'modular_urban_apartments_facade', 'Buildings', 'Four-storey, five-bay terracotta-render apartment building assembled from the Poly Haven modular facade kit: ground floor with entrance door and tall windows, three upper floors with sills/cornices, dark crown cornice, plus a closed plaster shell (sides, back, roof) and lit/dark night room backdrops behind every window (re-lit views of CC0 Poly Haven interior HDRIs).',
      'Directly across the street from the store (facade plane z = 0 faces +Z: rotate so it faces the store). Pavement level y = 0; the plinth goes to y = -0.6.',
      'Interior material: assign ApartmentBlock_Interior_Color.jpg also as emission (x0.6-1.0) so the lit windows glow at night (dark rooms stay dark). Glass -> Fade (alpha 0.35). No vertex normals in the OBJ: set Normals = Calculate, smoothing angle 30-45.',
      'Kit simplified with meshoptimizer (270,045 -> 48,128 tris), 64 module placements assembled in Python (5 bays x 3 m, storeys 3 m, ground level +0.15 m), shell and interior planes added, merged per material: 24,410 tris (revision 2026-10-15: 105 zero-area triangles removed). Texture conversion as above. ApartmentBlock_Interior_Color.jpg (revision 2026-10-15, replaces flat vector-drawn rooms): 20 cells (5 bays x 4 storeys, top row = ground floor), each a soft-focus 75-degree perspective view re-projected from a CC0 Poly Haven interior HDRI (Lebombo by Greg Zaal, St. Fagans Interior by Morgan Evans, Empty Warehouse 01 by Sergej Majboroda) and re-lit for night by Source~/make_interior.py: daylight windows replaced by night glass, lamp pools and the photographed wall sconces as light sources, 2700-4000 K per flat, bloom, ACES-fit tone mapping; overlays per cell: sheer curtains, backlit lined curtains, Venetian blinds (one half raised), TV glow, dark flats lit only by street-light spill, four dark shops with exit-light/standby LED glows and a lit stair hall. 7 of 15 upper windows lit, 2 with TV glow. Grain added; 1024 x 1024 JPG.')
ZHC = 'Converted glTF -> OBJ/MTL (node transforms baked, metres, Y up, forward = +X); textures resized to 1K/512 and re-encoded (Color/Normal/MetallicSmoothness); KHR_materials_specular ignored'
model('PoliceCar', 'police_sedan', 'Vehicles', '1980s American police sedan, black-and-white livery with "POLICE" and unit number, roof light bar, push bar, detailed interior. Fictional make.',
      'Parked at the kerb outside the forced storefront, nose toward the traffic direction; doors closed.',
      'Light-bar lenses are in the Lights material (shared atlas). For flashing, add red/blue point lights or use PoliceLightBar. Glass -> Fade.',
      ZHC + '; trunk badges removed (parody marque atlas); number-plate texture replaced by a plain plate colour; a slogan painted out of the body livery (two places); 20,840 tris (revision 2026-10-15: 8 zero-area triangles removed).')
model('Hatchback', 'ace11', 'Vehicles', "Modern (2011-style) five-door hatchback, grey metallic paint, fictional make.",
      'Parked civilian car along the kerb across the street or behind the police car.',
      'Paint is a constant colour (change Kd / material colour freely). Glass -> Fade.',
      ZHC + '; decoded from the mirror\'s web-optimised GLB (meshopt + WebP -> plain glTF + PNG); badges removed (the badge atlas holds parody marques); plates replaced by plain colour; body paint set to grey metallic; 15,588 tris.')
model('SedanClassic', 'sedan88', 'Vehicles', 'Late-1980s American four-door sedan, dark red paint, fictional make.',
      'Second parked civilian car; adds period variety next to the police car.', 'Paint is a constant colour. Glass -> Fade.',
      ZHC + '; source was in inches: scaled by 0.0254 and turned 180 deg so the front faces +X; trunk badges removed; plates replaced by plain colour; paint set to dark red; 22,549 tris (revision 2026-10-15: 37 zero-area triangles removed).')
DTC = 'Converted the selected building from the set (node transforms baked, metres, Y up); shared 2K atlases resized to 1K JPG and renamed DT_*; metallicRoughness maps (near-constant) folded into material constants; no vertex normals written (flat facades)'
model('BuildingBrickCorner', 'downtown', 'Buildings', 'Six-storey brick corner building (14.7 x 39.8 m footprint, 26 m) with stone ground floor, shopfronts and fire escapes.',
      'Across the street, beside the ApartmentBlock; narrow shopfront end is -Z, long shop side is +X.',
      'Set Normals = Calculate (smoothing 0-30). DT_MaskedDetails (fire escapes, railings, razor/barbed wire, chain-link, truss) uses DT_MaskedDetails_Color.png (RGBA): Standard Rendering Mode Cutout, Alpha Cutoff 0.5; texture import Alpha Is Transparency on, Mip Maps Preserve Coverage on. Its cards are double-sided in the mesh (back faces duplicated).',
      DTC + '; 3,261 tris. Revision 2026-10-15: the Sketchfab export lost the opacity of the Maskedstuff atlas (opaque JPEG composited on black), so the fire-escape cards rendered as near-black slabs; Source~/masked_details.py rebuilds the alpha from brightness (max RGB ramp 8 -> 20, 3 -> 8 in the dim chain-link band; specks < 4 px removed), lifts thin wires darker than 40/255 towards 40 (gain <= 4), bleeds colour into transparent texels and saves DT_MaskedDetails_Color.png; the OBJ gets 228 back-face triangles (reversed winding, own vertices) for the masked cards: 3,489 tris.')
model('BuildingBrickArcade', 'downtown', 'Buildings', 'Two/three-storey brick commercial block with four tall arched shop windows (31.7 x 31.6 m, 17.7 m).',
      'Across the street or at the end of the block; arcade front is the +Z face.', 'Normals = Calculate.', DTC + '; 1,437 tris.')
model('BuildingNarrowOffice', 'downtown', 'Buildings', 'Narrow seven-storey concrete-panel office infill (10 x 21.4 m, 21 m) with blank party walls.',
      'Gap filler between larger buildings across the street; windowed front is +Z.', 'Normals = Calculate.', DTC + '; 825 tris.')
model('BuildingBrickResidential', 'downtown', 'Buildings', 'Seven-storey brick residential block (37 x 37 m, 27.9 m), same facade on all four sides, shops at street level.',
      'Background block at the end of the street or around the corner.', 'Normals = Calculate.', DTC + '; 1,682 tris.')
PRC = 'Project-made procedural model (Source~/proc_models.py); no vertex normals (Normals = Calculate, smoothing 30-60)'
model('TrafficLight', None, 'Street furniture', 'Galvanised signal pole (114 mm) with a three-aspect 200 mm signal head on a yellow-bordered backboard, hoods and a yellow pedestrian push-button box.',
      'Street corner near the store / crossing; head faces approaching traffic (+Z).', 'LensRed/LensAmber/LensGreen: set emission on the active aspect (e.g. red), others dark.', PRC + '; 1,158 tris (revision 2026-10-15: 92 zero-area triangles removed).')
model('Bollard', None, 'Street furniture', 'Anthracite steel bollard (140 mm, 0.99 m) with domed top and two retro-reflective bands.',
      'Row along the kerb in front of the store (anti-ram-raid), 1.2-1.5 m apart.', '', PRC + '; 408 tris (revision 2026-10-15: 24 zero-area triangles removed).')
model('BikeRack', None, 'Street furniture', 'Galvanised Sheffield cycle stand (48 mm tube, 0.75 m wide).',
      'Pavement across the street or beside the bus shelter, several in a row 1 m apart.', 'Legs go 0.12 m below y = 0 (embedded in paving).', PRC + '; 996 tris.')
model('SignNoStopping', None, 'Signs', 'No-stopping (clearway) disc, 600 mm, on a 2.9 m galvanised post.',
      'Kerb outside the store (the police car parks here anyway).', 'Signs material: opaque, smoothness ~0.35.', PRC + '; 264 tris (revision 2026-10-15: 16 zero-area triangles removed).')
model('SignOneWayStreetName', None, 'Signs', 'One-way sign above a fictional "KESTREL STREET" name plate on a galvanised post.',
      'Street corner at one end of the block.', '', PRC + '; 156 tris (revision 2026-10-15: 16 zero-area triangles removed).')
model('StreetNamePlate', None, 'Signs', 'Wall-mounted fictional street-name plate "KESTREL STREET / Old Town - District 4" (0.9 x 0.225 m).',
      'On a facade corner at 2.5-3 m height.', 'Pivot at plate centre, back face at z = 0.', PRC + '; 14 tris.')
model('ParkingMeter', None, 'Street furniture', 'Pay-and-display parking machine with display, keypad, card slot and solar cap (generic "P" icon only).',
      'Pavement edge across the street or next to the parked hatchback.', '', PRC + '; 62 tris.')
model('StormDrainGrate', None, 'Street furniture', 'Cast-iron road gully grate 0.62 x 0.42 m with frame and dark pit under the bars.',
      'In the wet gutter against the kerb, every 20-30 m; one by the police car (puddles collect there).', 'Lay on the road surface (12 mm proud).', PRC + '; 194 tris.')
model('WasteContainer1100L', None, 'Street furniture', 'Four-wheel 1100 L galvanised waste container with dark green plastic lid, castors, handles.',
      'Side alley / service lane next to the store or across the street.', '', PRC + '; 880 tris (revision 2026-10-15: 144 zero-area triangles removed).')
model('BusShelter', None, 'Street furniture', 'Glazed bus shelter (3.9 x 2.6 x 1.6 m) with anthracite frame, glass back and end screen, bench, timetable and an illuminated advertising box with a fictional festival poster.',
      'Pavement across the street, open side facing the road.', 'Glass -> Fade (alpha 0.22). Lightbox -> emission with Proc_Lightbox_Color.jpg.', PRC + '; 234 tris.')
model('PoliceLightBar', None, 'Vehicles', 'Modern LED police light bar (1.22 m) with red/blue LED modules under a clear lens and white take-down LEDs.',
      'On the roof of a police car (KayKit car_police or PoliceCar if a modern bar is wanted).', 'LedRed/LedBlue/LedWhite are separate materials: animate their emission colours for flashing; LensClear -> Fade.', PRC + '; 372 tris.')


def files_for(name):
    fs = [name + '.obj', name + '.mtl'] + sorted(os.path.basename(f) for f in glob.glob(os.path.join(PK, name + '_*')))
    return [f for f in fs if os.path.exists(os.path.join(PK, f))]


def mtl_maps(name):
    maps = set()
    for line in open(os.path.join(PK, name + '.mtl')):
        if line.startswith('map_Kd'):
            maps.add(line.split()[1])
    return sorted(maps)


# ---------------------------------------------------------------- sources.json
shipped = sorted(f for f in os.listdir(PK) if os.path.isfile(os.path.join(PK, f)) and not f.startswith('.')
                 and f not in ('sources.json', 'README.md', 'LICENSE.txt'))
out = dict(pack='StreetModels', prepared=DATE, revised='2026-10-15', unverified_in_unity=True,
           revision_2026_10_15=['AirconUnit_AirconGrille_Color.png rebuilt: RGB grille colours, alpha from the Poly Haven opacity mask (was the mask as RGB, alpha 255)',
                                'PottedPlant rebuilt: 11,662 -> 4,951 tris, no zero-area triangles, double-sided leaf blades, gravel disc',
                                'ApartmentBlock_Interior_Color.jpg replaced: soft-focus night views from CC0 Poly Haven interior HDRIs',
                                'DT_MaskedDetails_Color.jpg replaced by DT_MaskedDetails_Color.png (RGBA cut-out); BuildingBrickCorner.mtl points at it and its masked cards are double-sided',
                                'MetalTrashCan and StreetBench colour maps re-exported at full brightness (empty AO channel was baked in, halving them)',
                                'zero-area triangles (simplification slivers, collapsed cap/pole fans) removed from 14 OBJs; no visible change (Source~/strip_degenerate.py)',
                                'PottedPlant: 30 stem slivers folded inwards by the simplification re-wound to face outwards like their vertex normals (Source~/fix_stem_winding.py); tris unchanged'],
           units='metres, Y up, OBJ/MTL (Unity imports without packages)',
           texture_convention={'Color': 'sRGB JPG/PNG (map_Kd in the MTL)', 'Normal': 'tangent-space OpenGL +Y JPG (Unity convention)',
                               'MetallicSmoothness': 'PNG RGBA, R = metallic, G = B = 0, A = smoothness (linear, sRGB off)'},
           sources=SRC, project_made=PROJECT, models={}, files=[])
owner = {}
for name, d in M.items():
    lg = LOG[name]
    fl = files_for(name)
    maps_used = mtl_maps(name)
    for mp in maps_used:
        if mp not in fl:
            fl.append(mp)
    for f in fl:
        owner.setdefault(f, []).append(name)
    srcd = SRC.get(d['src']) if d['src'] else None
    out['models'][name + '.obj'] = dict(
        category=d['cat'], what=d['what'], tris=lg['tris'], size_m=lg['size_m'], min_y=lg['min_y'],
        files=fl, materials=lg['materials'],
        source=(d['src'] if d['src'] else 'project-made'),
        license=(srcd['license'] if srcd else 'Project-owned'),
        author=(', '.join(srcd['authors']) if srcd and 'authors' in srcd else (srcd['author'] if srcd else 'Project (generated)')),
        source_url=(srcd['page'] if srcd else 'Source~/proc_models.py'),
        changes=d['changes'])
for f in shipped:
    names = owner.get(f, [])
    if f.startswith('Proc_'):
        lic, src = 'Project-owned', 'project-made (Source~/proc_tex.py)'
    elif names:
        s0 = M[names[0]]['src']
        lic = SRC[s0]['license'] if s0 else 'Project-owned'
        src = SRC[s0]['page'] if s0 else 'project-made'
    else:
        lic, src = '?', '?'
    if f == 'ApartmentBlock_Interior_Color.jpg':
        lic = 'CC0-1.0 (derived from CC0 Poly Haven HDRIs; re-lighting and compositing project-made)'
        src = 'Source~/make_interior.py from ' + ', '.join(SRC[k]['page'] for k in ('hdri_lebombo', 'hdri_st_fagans_interior', 'hdri_empty_warehouse_01'))
    out['files'].append(dict(file=f, bytes=os.path.getsize(os.path.join(PK, f)), sha256=sha(os.path.join(PK, f)), license=lic, source=src, used_by=names))
for f in sorted(os.listdir(os.path.join(PK, 'Source~'))):
    p = os.path.join(PK, 'Source~', f)
    out['files'].append(dict(file='Source~/' + f, bytes=os.path.getsize(p), sha256=sha(p), license='Project-owned', source='build script / preview render', used_by=[]))
unknown = [x['file'] for x in out['files'] if x['license'] == '?']
assert not unknown, unknown
json.dump(out, open(os.path.join(PK, 'sources.json'), 'w'), indent=1, ensure_ascii=False)
print('sources.json', len(out['files']), 'files')
json.dump(dict(M=M), open(os.path.join(SC, 'meta', 'docs_models.json'), 'w'), indent=1)
