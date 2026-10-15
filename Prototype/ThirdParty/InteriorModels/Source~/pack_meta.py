"""Metadata for InteriorModels docs: Poly Haven sources and descriptions/placements for every model."""
MIRROR = {'repo': 'https://github.com/Geadas11/Claude_steam_game', 'commit': '8b6aa56f951450b7a05434615bcdda2bd2d1e8fa',
          'path': 'assets/3d/models/<id>/ (<id>.gltf, <id>.bin, textures/<id>_*_2k.jpg)',
          'note': 'tools/fetch_polyhaven.py in that repository saves the files served by api.polyhaven.com (dl.polyhaven.org, 2K glTF) unmodified; '
                  'its assets/3d/CREDITS.md lists every model as Poly Haven CC0.'}
CHECKS = {
 'toonstudio': {'repo': 'https://github.com/blue45f/toonstudio', 'commit': '526e7bc10963e2cfaaa7278dbb6a86b8ef09acd3',
                'path': 'apps/web/public/assets/studio/cc0-20260906/assets/polyhaven-<id>/SOURCE.json',
                'what': 'SHA-256 and Poly Haven API MD5 of each dl.polyhaven.org file, plus Poly Haven metadata (authors)'},
 'sev-1': {'repo': 'https://github.com/acltabontabon/sev-1', 'commit': 'cc500f9aeeb6d5e8fbc911ec1c0839b855d6002a',
           'path': 'assets/environment/poly_haven_workstation/provenance.json', 'what': 'Poly Haven API MD5 of each file'},
 'calinou': {'repo': 'https://github.com/Calinou/godot-benchmark-tool', 'commit': 'bbc93c93d6a90947245b3d5777f403e92d0c5e7b',
             'path': 'benchmark_scene/polyhaven/mid_century_lounge_chair.gltf/', 'what': 'independent byte-identical copy'},
}
LICENSE_PH = {'name': 'CC0 1.0 Universal (Poly Haven licence)', 'url': 'https://polyhaven.com/license',
              'spdx': 'CC0-1.0', 'read_from': 'github.com/Poly-Haven/polyhaven.com @ b1a6aa13afc03ae00860f6ac5fe6d5daf8e95356, public/locales/en/license.json'}
# name: (poly haven id, title, authors, verification, changes)
WF = ''
SEAM = ' (rebuilt October 15 with UV seams and open borders held fixed: the earlier decimation had opened cracks along the seams%s; every face agrees with its vertex normals)'
PH = {
 'LoungeChairMidCentury': ('mid_century_lounge_chair', 'Mid Century Lounge Chair', 'Kuutti Siitonen', 'sha256 (toonstudio) + byte-identical copy (Calinou)', []),
 'ArmChairModern': ('modern_arm_chair_01', 'Modern Arm Chair 01', 'Vibrant Nordic', 'sha256 (toonstudio)', []),
 'OttomanLeather': ('Ottoman_01', 'Ottoman 01', 'Caspian Fortune', 'sha256 (toonstudio)', []),
 'CoffeeTableConcrete': ('modern_coffee_table_01', 'Modern Coffee Table 01', 'Amin', 'mirror only (download script)', []),
 'PottedPlantTall': ('potted_plant_01', 'Potted Plant 01', 'Rico Cilliers', 'sha256 (toonstudio)',
                     ['rebuilt October 15 (176k -> 14,965 triangles): pot shell kept at source detail (5,440 -> 5,184; only 256 faces sealed below the gravel dropped); '
                      'the 56.9k-triangle pebble layer baked top-down onto a 520-triangle heightfield disc with its own Color/Normal/MetallicSmoothness maps (Gravel material, Source~/bake_gravel.py); '
                      'leaf blades replaced by alpha-cut cards that keep the original blade vertices and outline (293 leaves, 3,062 triangles incl. back faces; Leaves_Color.png carries the cut-out alpha, Source~/leaf_cards.py); '
                      'leaf stalks rebuilt as 5-sided tubes (4,550 triangles); trunk decimated with welded seams (7,008 -> 1,649); every face agrees with its vertex normals']),
 'PottedPlantArrowhead': ('potted_plant_02', 'Potted Plant 02', 'Rico Cilliers', 'sha256 (toonstudio)',
                          ['rebuilt October 15 (70k -> 14,912 triangles): pot shell kept at source detail (5,184 -> 3,792; only the 1,392 faces sealed below the gravel dropped); '
                           'the 53.9k-triangle dirt layer baked onto a 520-triangle heightfield disc (Gravel material, Source~/bake_gravel.py); '
                           'leaves decimated 10,692 -> 5,300 with borders preserved and given back faces (10,600 triangles); every face agrees with its vertex normals']),
 'PottedSucculent': ('potted_plant_04', 'Potted Plant 04', 'James Ray Cock', 'sha256 (toonstudio)', ['decimated 8.9k -> 5k triangles' + SEAM % ', e.g. a slit down the pot']),
 'WallClock': ('wall_clock', 'Wall Clock', 'PierreB3D', 'sha256 (toonstudio)', ['maker-style wordmark on the dial painted out (patch from the clean dial)']),
 'DeskLampArm': ('desk_lamp_arm_01', 'Desk Lamp Arm 01', 'Kuutti Siitonen, Yann Kervran', 'sha256 (toonstudio) + MD5 (sev-1)', ['decimated 24.1k -> 15k triangles by seam-aware decimation (welded mesh, re-split per UV island, Source~/convert_ph.py decimate_seam_aware; rebuilt October 15: the earlier version had holes in the shade and broken arm rods); every face agrees with its vertex normals']),
 'StationerySet': ('stationery_supplies', 'Stationery Supplies', 'Mateusz Sadek', 'mirror only (download script)', ['pencil print "Haven Penworks" is Poly Haven\'s own fictional brand and was kept']),
 'PendantLampGlobe': ('modern_ceiling_lamp_01', 'Modern Ceiling Lamp 01', 'James Ray Cock', 'sha256 (toonstudio)', ['clear glass shade written as an MTL constant']),
 'FireExtinguisher': ('korean_fire_extinguisher_01', 'Korean Fire Extinguisher 01', 'UM JOORIN', 'sha256 (toonstudio)',
                      ['Korean stencil and small text on the base plaque removed from the colour map and (October 15) its embossed relief flattened in the normal map; '
                       '"UMS" print on the hose removed from the colour and normal maps (October 15); product label (with a maker name) replaced by an original English label; '
                       'Korean inspection tag with handwritten names replaced by an original English inspection record; decimated 9.9k -> 8k triangles' + SEAM % '']),
 'FireAlarmCallPoint': ('fire_alarm', 'Fire Alarm', 'Slinc', 'sha256 (toonstudio)',
                        ['raised maker-style wordmark letters removed from the lower name plate (85 small letter meshes) and the matching texture lettering painted out']),
 'PictureFrameBlack': ('hanging_picture_frame_01', 'Hanging Picture Frame 01', 'James Ray Cock', 'mirror only (download script)',
                       ['artwork replaced by an original project print (minimal seascape, PictureFrameBlack_Artwork_Color.jpg); artwork UVs normalised to 0..1']),
 'PictureFrameWhite': ('hanging_picture_frame_02', 'Hanging Picture Frame 02', 'James Ray Cock', 'sha256 (toonstudio)',
                       ['renamed from PictureFrameSilver (October 15): Poly Haven describes a distressed white wooden frame',
                        'inherited artwork (a painterly-filtered photograph carrying a third-party signature) replaced by a public-domain painting: '
                        'Paul Cezanne, The Banks of the Marne (c. 1888), cropped to the picture area (PictureFrameWhite_Artwork_Color.jpg, Source~/prep_print.py); artwork UVs normalised to 0..1']),
 'PictureFrameGilt': ('fancy_picture_frame_01', 'Fancy Picture Frame 01', 'Rico Cilliers, Rob Tuytel', 'sha256 (toonstudio)', ['canvas painting cropped to its visible area, UVs normalised to 0..1']),
 'PhotoFrameDesk': ('standing_picture_frame_01', 'Standing Picture Frame 01', 'James Ray Cock', 'mirror only (download script)',
                    ['rotated -90 degrees about Y so the photo faces +Z like every other model (October 15)', 'photo cropped to its visible area, UVs normalised to 0..1']),
 'VaseCeramicTall': ('ceramic_vase_03', 'Ceramic Vase 03', 'James Ray Cock', 'sha256 (toonstudio)', ['decimated 11.1k -> 5.4k triangles' + SEAM % ', holes at the base']),
 'VaseCeramicBottle': ('ceramic_vase_01', 'Ceramic Vase 01', 'James Ray Cock', 'sha256 (toonstudio)', ['decimated 10.3k -> 4.0k triangles' + SEAM % ', holes in the body']),
 'OfficeDeskMetal': ('metal_office_desk', 'Metal Office Desk', 'Ulan Cabanilla', 'MD5 (sev-1)', []),
 'CCTVBulletHousing': ('security_camera_02', 'Security Camera 02', 'Garrison Gager, Yann Kervran', 'sha256 (toonstudio)',
                       ['fictional spec/regulatory sticker text blanked; decimated 12.5k -> 8.5k triangles; rig not kept' + SEAM % '']),
 'ShelvingSteelWide': ('steel_frame_shelves_03', 'Steel Frame Shelves 03', 'Ulan Cabanilla', 'mirror only (download script)', []),
 'CardboardBox': ('cardboard_box_01', 'Cardboard Box 01', 'Rahul Chaudhary', 'sha256 (toonstudio)', ['decimated 17k -> 3k triangles' + SEAM % ', a split corner']),
}
COMMON_CHANGES = ['converted glTF -> OBJ/MTL (Source~/convert_ph.py): node transforms applied, metres, Y up, welded v/vt/vn',
                  'base colour -> _Color.jpg, nor_gl -> _Normal.jpg, ARM/roughness (G rough, B metal) -> _MetallicSmoothness.png (R=G=B metal, A = 1 - rough); 2K sources downsized to 1K/512',
                  'pivot moved (floor: lowest point y = 0, centred in x/z; wall: back at z = 0, centred in x/y; ceiling: top at y = 0)']
# name: (category, description, placement)
INFO = {
 'LoungeChairMidCentury': ('Seating', 'Mid-century swivel lounge chair: cognac leather shell on a 4-star aluminium base.', 'Customer seating corner, angled towards the coffee table and rug (pairs with the existing SheenChair / GlamVelvetSofa).'),
 'ArmChairModern': ('Seating', 'Modern armchair: black cushions in an oiled-oak frame.', 'Seating corner, or the visitor chair in the rear office.'),
 'OttomanLeather': ('Seating', 'Tufted dark-brown leather ottoman / footstool.', 'Seating corner, in front of the sofa or beside the lounge chair.'),
 'CoffeeTableConcrete': ('Tables', 'Low coffee table: concrete top on folded oak legs.', 'Centre of the seating corner on the Rug; a vase or the succulent on top.'),
 'PottedPlantTall': ('Plants', 'Small-leaved indoor tree (ficus-like) in a smooth terracotta urn on gravel, 1.34 m; leaf blades are alpha-cut cards (Leaves material: Cutout, cutoff 0.5).', 'Storefront corner beside the forced doors, or the end of the seating corner.'),
 'PottedPlantArrowhead': ('Plants', 'Arrowhead plant (Syngonium) in a wide terracotta pot on gravel, 0.84 m; leaves have back faces.', 'Floor beside the counter end or in the rear office corner.'),
 'PottedSucculent': ('Plants', 'Haworthia-type succulent in a white glazed pot on gravel, 27 cm.', 'Counter, office desk or coffee table.'),
 'SnakePlant': ('Plants', 'Snake plant (Sansevieria laurentii, yellow-edged banded leaves) in a charcoal ceramic pot, 1.08 m.', 'Flanking the rear office door or by the entrance; reads well against plain walls.'),
 'WallClock': ('Wall', 'Office wall clock (32 cm) with metal bezel and glass; hands are separate parts (hour, minute, second) in one mesh.', 'Rear office above the door, about 2.1 m to the centre; or behind the counter.'),
 'DeskLampArm': ('Office', 'Orange architect desk lamp with spring arm and edge clamp (0.89 m extended).', 'Clamped to the rear office desk edge: the clamp jaw (lowest point) wraps under the desk top.'),
 'StationerySet': ('Office', 'Pens, pencils, eraser and a frosted pen cup laid out on a 30 cm footprint.', 'Office desk beside the keyboard; scatter the pens if the desk was searched.'),
 'PendantLampGlobe': ('Lighting', 'Pendant with a clear glass globe shade and visible bulb, 0.95 m drop including cable.', 'Above the seating corner or over the counter; raise it to shorten the drop.'),
 'FireExtinguisher': ('Safety', '3 kg red ABC powder extinguisher (0.66 m on its floor stand), original English label and inspection tag.', 'Beside the rear exit or the office door, under the ExitSign.'),
 'FireAlarmCallPoint': ('Safety', 'Red manual fire alarm pull station (10 x 14 cm).', 'Walls beside the rear exit and inside the front door, about 1.4 m high.'),
 'PictureFrameBlack': ('Wall', 'Black poster frame (59 x 84 cm) with mount, glass and an original seascape print (swap the Artwork texture freely).', 'Seating corner or office wall at eye level (centre about 1.55 m).'),
 'PictureFrameWhite': ('Wall', 'Distressed white-painted wooden frame (75 x 50 cm) with glass, holding a print of Cezanne\'s The Banks of the Marne (c. 1888, public domain); swap the Artwork texture freely.', 'Seating corner wall or behind the counter, centre about 1.55 m.'),
 'PictureFrameGilt': ('Wall', 'Ornate gilt frame (60 x 46 cm) with a classical landscape painting.', 'Office or a side-room wall; suits a heritage jeweller look.'),
 'PhotoFrameDesk': ('Office', 'Standing desk photo frame (20 x 25 cm, leaning back on its stand) with a black-and-white canal photograph facing +Z.', 'Office desk or behind the counter.'),
 'VaseCeramicTall': ('Decor', 'Tall square white ceramic vase, 41 cm.', 'Wall-case plinths, counter end or coffee table.'),
 'VaseCeramicBottle': ('Decor', 'White ceramic bottle vase with a wavy lip, 40 cm.', 'Same as the tall vase; pair them on a plinth.'),
 'OfficeDeskMetal': ('Office', 'Steel twin-pedestal office desk, 2.0 x 0.95 m with drawers.', 'Rear office, back against a wall; monitor, keyboard, phone, lamp on top.'),
 'CCTVBulletHousing': ('Security', 'Outdoor bullet camera in a weather housing on a wall bracket (39 cm); lens window faces +Z, bracket foot at the bottom/back.', 'Outside above the storefront or in the side corridor; complements the ForensicModels SecurityCamera.'),
 'ShelvingSteelWide': ('Storage', 'Wide steel-frame shelving with wooden boards and a two-drawer unit, 2.35 x 2.31 m.', 'Rear office or a side room; load it with LeverArchFiles and CardboardBox.'),
 'CardboardBox': ('Storage', 'Worn taped cardboard box (39 x 34 x 52 cm).', 'Side rooms, under the counter, stacked as stock.'),
 'OfficeChair': ('Office', 'Swivel task chair: mesh back, charcoal fabric seat, polished 5-star base, twin-wheel casters.', 'At the office desk; rolled away or tipped over after the burglary.'),
 'ComputerMonitor': ('Office', '24-inch flat-panel monitor; screen sub-material shows a generic lock screen (02:47).', 'Office desk. Screen can glow at night (Color map as emission).'),
 'SecurityMonitor': ('Security', 'Same monitor showing a four-camera CCTV grid; CAM 03 (office) reads VIDEO LOSS.', 'Rear office beside the DVR: the lost office feed is an investigation cue.'),
 'Keyboard': ('Office', 'Full-size 104-key keyboard with printed legends (no logo), 45 x 25 cm incl. cable.', 'Office desk in front of the monitor.'),
 'Mouse': ('Office', 'Corded mouse on a 25 x 21 cm fabric mouse mat.', 'Office desk to the right of the keyboard.'),
 'DeskPhone': ('Office', 'IP desk phone with corded handset, LCD (02:47, Ext 201) and keypad.', 'Office desk or behind the counter (alarm call cue).'),
 'FilingCabinet': ('Storage', '4-drawer light-grey steel filing cabinet, 0.47 x 1.32 x 0.62 m, labelled drawers.', 'Rear office against a wall.'),
 'FilingCabinetOpen': ('Storage', 'Same cabinet with the top drawer pulled out 36 cm, hanging files rummaged and sheets lifted.', 'Rear office: a searched-cabinet evidence spot (place markers and scale here).'),
 'LeverArchFiles': ('Office', 'Row of five lever-arch files with labelled spines (invoices, stock register, insurance, safe log, suppliers).', 'Shelves in the rear office; one can lie on the floor.'),
 'DVRRecorder': ('Security', '8-channel CCTV recorder box (38 x 29 cm) with status LEDs.', 'Office shelf under the SecurityMonitor; or leave its spot empty (stolen recorder) with loose cables.'),
 'WaterCooler': ('Appliances', 'Floor-standing water cooler with hot/cold taps and a 19-litre blue bottle, 1.5 m.', 'Staff corner of the rear office or a side room.'),
 'CoffeeMachine': ('Appliances', 'Bean-to-cup coffee machine with brushed fascia, display, drip tray and two cups.', 'Staff counter in the rear office, or a hospitality cabinet in the seating corner.'),
 'MiniFridge': ('Appliances', 'Under-counter mini fridge, 48 x 84 x 50 cm, white.', 'Rear office under a counter or in a side room.'),
 'CCTVDome': ('Security', 'Ceiling dome camera (14 cm) with smoked dome and tilted lens. y = 0 is the ceiling surface.', 'Sales-floor ceiling (2-3 units covering the cases) and the office.'),
 'TrackLight': ('Lighting', 'Black 1.2 m ceiling track with three adjustable LED spot heads.', 'Over the wall cases, heads aimed at the displays; add one Unity spot light per head.'),
 'RecessedDownlight': ('Lighting', 'Recessed LED downlight (12 cm trim); y = 0 is the ceiling surface, reflector rises into the ceiling.', 'Grid across the sales-floor ceiling (about 1.5-2 m spacing).'),
 'LEDPanel': ('Lighting', '600 x 600 mm LED ceiling panel.', 'Rear office and side rooms (cool white).'),
 'ExitSign': ('Safety', 'Illuminated exit sign 36 x 13.5 cm with an original running-figure pictogram, arrow and EXIT.', 'Above the rear exit and inside above the front door, about 2.2 m; always lit at night.'),
 'SmokeDetector': ('Safety', 'Ceiling optical smoke detector (11.6 cm) with status LED.', 'One per room on the ceiling.'),
 'WallVent': ('Building', 'Wall return-air grille 42 x 22 cm with angled louvres.', 'Low on the office wall or high on the side walls.'),
 'CeilingDiffuser': ('Building', 'Square 4-way ceiling air diffuser 60 x 60 cm.', 'Sales-floor and office ceilings between the downlights.'),
 'DoorLeverHandle': ('Building', 'Brushed stainless lever on a round rose with an escutcheon; origin at the door face (z = 0), lever along +X.', 'Rear office and side-room doors at 1.0-1.05 m; mirror X for the other hand / other side.'),
 'DoorPullHandle': ('Building', 'Stainless pull bar, 60 cm long, 40 cm fixing centres; origin at the glass face.', 'The forced storefront glass doors, vertical, centred about 1.0 m high (both faces).'),
 'Rug': ('Soft furnishing', 'Wool area rug 2.0 x 1.4 m, warm grey field with charcoal and brass border.', 'Under the seating corner group.'),
}

REVISED = '2026-10-15'
# models rebuilt on October 15 (verifier fixes); their files carry 'rebuilt 2026-10-15' in sources.json
REBUILT = ['PottedPlantTall', 'PottedPlantArrowhead', 'FireExtinguisher', 'PictureFrameWhite', 'PhotoFrameDesk',
           'CCTVBulletHousing', 'CardboardBox', 'DeskLampArm', 'PottedSucculent', 'VaseCeramicBottle', 'VaseCeramicTall']
RENAMED = {'PictureFrameWhite': 'PictureFrameSilver'}
# artwork files whose licence differs from their model's
ART = {
 'PictureFrameBlack_Artwork_Color.jpg': {'license': 'Project-owned (original print)', 'author': 'Project', 'source_url': None,
     'version': 'generated 2026-10-14 (Source~/gen_art.py)'},
 'PictureFrameWhite_Artwork_Color.jpg': {'license': 'Public domain (painting; reproduction carries no new copyright); repository dedicated CC0-1.0',
     'author': 'Paul Cezanne (1839-1906); scan published by github.com/pmilovanov/hermitage-art',
     'source_url': 'https://github.com/pmilovanov/hermitage-art/blob/b8ead49c010eac0c8366bafc4ffaaf1f1a447805/Cezanne,_Paul_-_The_Banks_of_the_Marne_(Villa_on_the_Bank_of_a_River).jpg',
     'version': 'hermitage-art commit b8ead49c010eac0c8366bafc4ffaaf1f1a447805 (2021-01-28), fetched 2026-10-15',
     'source_sha256': 'd0b9bc6f99f7c9c9dbc908546e7aa74bf8f6b4cba5c494c1b9ed59715e6d444d', 'source_bytes': 8216755,
     'painting': 'Paul Cezanne, The Banks of the Marne (Villa on the Bank of a River), c. 1888, oil on canvas, State Hermitage Museum',
     'changes': 'cropped (40 px trimmed left and right, rows 300-2795 of 3440 kept) to the 1.706:1 picture area, resized to 1280 x 750, light unsharp mask'},
}
