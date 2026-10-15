# InteriorModels: realistic interior props for the night jewellery store

Furniture, office equipment, lighting, safety and building fixtures, plants and decor for the 16 x 12 m night store, its rear office and side rooms. Made for Unity 6000.6.2f1 (Built-in pipeline, no packages): OBJ/MTL plus PBR texture maps. Prepared October 14, 2026, revised October 15, 2026 (see "Changes on October 15"). **Not imported or checked in Unity yet.**

- **23 Poly Haven models** (CC0, photoreal PBR textures), converted to OBJ, decimated where needed (1-15k triangles each), with brand-like lettering removed from four of them.
- **25 project-made models** (procedural geometry and procedural textures, project-owned) for items Poly Haven does not have: office chair, monitor, keyboard, phone, filing cabinets, appliances, CCTV dome, light fittings, exit sign, smoke detector, vents, door handles, rug, snake plant.
- Total size **27.4 MB** (27385794 bytes, including Source~ and these documents); largest file 1.29 MB. Every model has 60 to 15000 triangles.
- Credits in LICENSE.txt; per-file source, licence and SHA-256 in sources.json; per-material map wiring, bounds and pivots in models.json. `Source~/InteriorModels_Previews.jpg` shows every model (software preview, not a Unity render).

Already covered elsewhere, so not duplicated here: SheenChair, GlamVelvetSofa, ChairDamaskPurplegold, GlassVaseFlowers, DiffuseTransmissionPlant (KhronosSamples); bullet SecurityCamera, LeatherBinder, OfficeNotepads, ScatteredPapers, Clipboard (ForensicModels); GiftBag shopping bag (JewelryModels).

## Conventions

| Item | Convention |
|---|---|
| Units / axes | Metres, Y up, front faces **+Z**. Import with **Scale Factor 1**, Convert Units off. Unity's OBJ importer handles the handedness flip. |
| Pivots | `floor`: lowest point at y = 0, centred in x/z. `wall`: back face at z = 0, centred in x and y, facing +Z (put the pivot on the wall). `ceiling`: top at y = 0, hanging down (put the pivot on the ceiling). `none`: see the model row (CCTVDome and RecessedDownlight: y = 0 is the ceiling surface; door handles: origin on the door face). |
| `<Name>.obj/.mtl` | One OBJ group and material per surface. `map_Kd` points at the colour map; the Normal and MetallicSmoothness file names are written as `# Normal ...` / `# MetallicSmoothness ...` comment lines in the MTL and listed in models.json. Unity reads only Kd, `map_Kd` and `d`, so assign the other maps in code (DemoAssetLibrary) or on the material. OBJ files carry welded `v/vt/vn` (authored normals). |
| `_Color.jpg/.png` | Base colour, sRGB. |
| `_Normal.jpg` | Tangent-space normal map, OpenGL +Y (= Unity). Texture Type **Normal map**, strength 1. |
| `_MetallicSmoothness.png` | R = G = B = metallic, A = smoothness. **sRGB off**. Standard: Metallic slot, Source = Metallic Alpha, Smoothness scale 1 (same as ForensicModels, PBRExterior, NightMaterials). |
| Constant materials | No maps: use Kd plus the `metallic` / `smoothness` values in models.json (e.g. Chrome 1 / 0.92, Aluminium 1 / 0.7, Rubber 0 / 0.25). |
| Glass | MTL `d` < 1 (frame glass 0.12, clock 0.18, pendant shade 0.25, dome 0.55, water bottle 0.35). Use the project glass material (Standard Fade/Transparent, smoothness ~0.95). |
| Emission | models.json marks emissive surfaces (`emission`): monitor screens, ExitSign face, TrackLight/RecessedDownlight lenses, LEDPanel diffuser, pendant and desk-lamp bulbs, CCTV IR LEDs, smoke-detector LED. Use the Color map (or a warm/cool white) as emission when lit. |
| Shared_* textures | Seamless 512 px tiling materials used by the project-made models (PlasticBlack, PlasticWhite, FabricCharcoal, MeshWeave, BrushedSteel, PowderCoatGrey). Those models have world-scale UVs; keep Wrap Mode = Repeat. |
| Two-sided parts | Unity's Standard shader culls back faces, so every thin part carries explicit back faces (a reversed copy with negated normals): SnakePlant leaves, the office-chair mesh back, the PottedPlantTall leaf cards and the PottedPlantArrowhead leaves. The other models need no back faces from normal viewpoints (checked in software renders with back-face culling). No double-sided shader needed. |
| Alpha cut-out | `PottedPlantTall_Leaves_Color.png` has the leaf outline in its alpha channel (colour padded outwards under the transparent part). Standard shader **Rendering Mode Cutout**, Alpha Cutoff 0.5; texture import: Alpha Is Transparency on, keep mipmaps (or enable Mip Maps Preserve Coverage). |
| Winding and seams | Every face of every model agrees with its vertex normals (`Source~/check_obj.py`), and no decimated model has cracks along its UV seams (`Source~/cracks.py` compares open edges with the glTF source). |

## Seating

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **ArmChairModern** | 0.82 × 1.02 × 0.99 | 8,916 | floor | Frame: C+N+MS; Cushions: C+N+MS | Poly Haven `modern_arm_chair_01` (Vibrant Nordic) | Modern armchair: black cushions in an oiled-oak frame. Seating corner, or the visitor chair in the rear office. |
| **LoungeChairMidCentury** | 1.01 × 1.17 × 1.19 | 6,148 | floor | Chair: C+N+MS | Poly Haven `mid_century_lounge_chair` (Kuutti Siitonen) | Mid-century swivel lounge chair: cognac leather shell on a 4-star aluminium base. Customer seating corner, angled towards the coffee table and rug (pairs with the existing SheenChair / GlamVelvetSofa). |
| **OttomanLeather** | 0.89 × 0.62 × 0.62 | 4,240 | floor | Leather: C+N+MS | Poly Haven `Ottoman_01` (Caspian Fortune) | Tufted dark-brown leather ottoman / footstool. Seating corner, in front of the sofa or beside the lounge chair. |

## Tables

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **CoffeeTableConcrete** | 0.60 × 0.39 × 1.20 | 4,504 | floor | Table: C+N+MS | Poly Haven `modern_coffee_table_01` (Amin) | Low coffee table: concrete top on folded oak legs. Centre of the seating corner on the Rug; a vase or the succulent on top. |

## Soft furnishing

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **Rug** | 2.00 × 0.01 × 1.40 | 108 | floor | Rug: C+N | Project-made | Wool area rug 2.0 x 1.4 m, warm grey field with charcoal and brass border. Under the seating corner group. |

## Plants

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **PottedPlantArrowhead** | 0.70 × 0.84 × 0.66 | 14,912 | floor | Gravel: C+N+MS; Leaves: C+N+MS; Pot: C+N+MS | Poly Haven `potted_plant_02` (Rico Cilliers) | Arrowhead plant (Syngonium) in a wide terracotta pot on gravel, 0.84 m; leaves have back faces. Floor beside the counter end or in the rear office corner. |
| **PottedPlantTall** | 0.58 × 1.34 × 0.63 | 14,965 | floor | Gravel: C+N+MS; Leaves: C (cut-out alpha)+N+MS; Pot: C+N+MS | Poly Haven `potted_plant_01` (Rico Cilliers) | Small-leaved indoor tree (ficus-like) in a smooth terracotta urn on gravel, 1.34 m; leaf blades are alpha-cut cards (Leaves material: Cutout, cutoff 0.5). Storefront corner beside the forced doors, or the end of the seating corner. |
| **PottedSucculent** | 0.17 × 0.27 × 0.18 | 4,999 | floor | Plant: C+N+MS | Poly Haven `potted_plant_04` (James Ray Cock) | Haworthia-type succulent in a white glazed pot on gravel, 27 cm. Counter, office desk or coffee table. |
| **SnakePlant** | 0.26 × 1.08 × 0.26 | 2,112 | floor | Pot: C+N; Soil: C+N; Leaf: C+N | Project-made | Snake plant (Sansevieria laurentii, yellow-edged banded leaves) in a charcoal ceramic pot, 1.08 m. Flanking the rear office door or by the entrance; reads well against plain walls. |

## Decor

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **VaseCeramicBottle** | 0.20 × 0.40 × 0.20 | 3,984 | floor | Ceramic: C+N+MS | Poly Haven `ceramic_vase_01` (James Ray Cock) | White ceramic bottle vase with a wavy lip, 40 cm. Same as the tall vase; pair them on a plinth. |
| **VaseCeramicTall** | 0.11 × 0.41 × 0.11 | 5,388 | floor | Ceramic: C+N+MS | Poly Haven `ceramic_vase_03` (James Ray Cock) | Tall square white ceramic vase, 41 cm. Wall-case plinths, counter end or coffee table. |

## Wall

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **PictureFrameBlack** | 0.59 × 0.84 × 0.02 | 2,586 | wall | Glass: glass constant (d 0.12); Artwork: C; Frame: C+N+MS | Poly Haven `hanging_picture_frame_01` (James Ray Cock) | Black poster frame (59 x 84 cm) with mount, glass and an original seascape print (swap the Artwork texture freely). Seating corner or office wall at eye level (centre about 1.55 m). |
| **PictureFrameGilt** | 0.60 × 0.46 × 0.02 | 938 | wall | Artwork: C; Frame: C+N+MS | Poly Haven `fancy_picture_frame_01` (Rico Cilliers, Rob Tuytel) | Ornate gilt frame (60 x 46 cm) with a classical landscape painting. Office or a side-room wall; suits a heritage jeweller look. |
| **PictureFrameWhite** | 0.75 × 0.50 × 0.03 | 3,678 | wall | Artwork: C; Frame: C+N+MS; Glass: glass constant (d 0.12) | Poly Haven `hanging_picture_frame_02` (James Ray Cock) | Distressed white-painted wooden frame (75 x 50 cm) with glass, holding a print of Cezanne's The Banks of the Marne (c. 1888, public domain); swap the Artwork texture freely. Seating corner wall or behind the counter, centre about 1.55 m. |
| **WallClock** | 0.32 × 0.32 × 0.05 | 3,658 | wall | Clock: C+N+MS; Glass: glass constant (d 0.18) | Poly Haven `wall_clock` (PierreB3D) | Office wall clock (32 cm) with metal bezel and glass; hands are separate parts (hour, minute, second) in one mesh. Rear office above the door, about 2.1 m to the centre; or behind the counter. |

## Lighting

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **LEDPanel** | 0.59 × 0.01 × 0.59 | 110 | ceiling | Frame: C+N; Diffuser: constant | Project-made | 600 x 600 mm LED ceiling panel. Rear office and side rooms (cool white). |
| **PendantLampGlobe** | 0.43 × 0.95 × 0.43 | 5,602 | ceiling | GlassShade: glass constant (d 0.25); Fitting: C+N+MS; Bulb: C+N+MS | Poly Haven `modern_ceiling_lamp_01` (James Ray Cock) | Pendant with a clear glass globe shade and visible bulb, 0.95 m drop including cable. Above the seating corner or over the counter; raise it to shorten the drop. |
| **RecessedDownlight** | 0.12 × 0.06 × 0.12 | 512 | none | Trim: C+N; Reflector: constant; Lens: constant | Project-made | Recessed LED downlight (12 cm trim); y = 0 is the ceiling surface, reflector rises into the ceiling. Grid across the sales-floor ceiling (about 1.5-2 m spacing). |
| **TrackLight** | 1.20 × 0.17 × 0.15 | 4,488 | ceiling | Housing: constant; Reflector: constant; Lens: constant | Project-made | Black 1.2 m ceiling track with three adjustable LED spot heads. Over the wall cases, heads aimed at the displays; add one Unity spot light per head. |

## Office

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **ComputerMonitor** | 0.54 × 0.44 × 0.19 | 1,538 | floor | Housing: C+N; Screen: C | Project-made | 24-inch flat-panel monitor; screen sub-material shows a generic lock screen (02:47). Office desk. Screen can glow at night (Color map as emission). |
| **DeskLampArm** | 0.20 × 0.89 × 0.61 | 15,000 | floor | Lamp: C+N+MS; Bulb: C+N+MS | Poly Haven `desk_lamp_arm_01` (Kuutti Siitonen, Yann Kervran) | Orange architect desk lamp with spring arm and edge clamp (0.89 m extended). Clamped to the rear office desk edge: the clamp jaw (lowest point) wraps under the desk top. |
| **DeskPhone** | 0.24 × 0.13 × 0.19 | 2,304 | floor | Body: C+N; Face: C+N; Rubber: constant | Project-made | IP desk phone with corded handset, LCD (02:47, Ext 201) and keypad. Office desk or behind the counter (alarm call cue). |
| **Keyboard** | 0.45 × 0.03 × 0.25 | 1,376 | floor | Keyboard: C; Rubber: constant | Project-made | Full-size 104-key keyboard with printed legends (no logo), 45 x 25 cm incl. cable. Office desk in front of the monitor. |
| **LeverArchFiles** | 0.42 × 0.32 × 0.28 | 60 | floor | Binders: C | Project-made | Row of five lever-arch files with labelled spines (invoices, stock register, insurance, safe log, suppliers). Shelves in the rear office; one can lie on the floor. |
| **Mouse** | 0.25 × 0.05 × 0.21 | 1,124 | floor | Mouse: C; Rubber: constant; Pad: C+N | Project-made | Corded mouse on a 25 x 21 cm fabric mouse mat. Office desk to the right of the keyboard. |
| **OfficeChair** | 0.64 × 1.07 × 0.69 | 7,344 | floor | Base: constant; Rubber: constant; PlasticBlack: C+N; Chrome: constant; Fabric: C+N; Mesh: C+N; ArmPad: C+N | Project-made | Swivel task chair: mesh back, charcoal fabric seat, polished 5-star base, twin-wheel casters. At the office desk; rolled away or tipped over after the burglary. |
| **OfficeDeskMetal** | 2.00 × 0.79 × 0.95 | 6,898 | floor | Desk: C+N+MS | Poly Haven `metal_office_desk` (Ulan Cabanilla) | Steel twin-pedestal office desk, 2.0 x 0.95 m with drawers. Rear office, back against a wall; monitor, keyboard, phone, lamp on top. |
| **PhotoFrameDesk** | 0.20 × 0.25 × 0.10 | 1,634 | floor | Glass: glass constant (d 0.12); Artwork: C; Frame: C+N+MS | Poly Haven `standing_picture_frame_01` (James Ray Cock) | Standing desk photo frame (20 x 25 cm, leaning back on its stand) with a black-and-white canal photograph facing +Z. Office desk or behind the counter. |
| **StationerySet** | 0.30 × 0.16 × 0.08 | 4,412 | floor | Stationery: C+N+MS | Poly Haven `stationery_supplies` (Mateusz Sadek) | Pens, pencils, eraser and a frosted pen cup laid out on a 30 cm footprint. Office desk beside the keyboard; scatter the pens if the desk was searched. |

## Storage

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **CardboardBox** | 0.39 × 0.34 × 0.52 | 3,000 | floor | Cardboard: C+N+MS | Poly Haven `cardboard_box_01` (Rahul Chaudhary) | Worn taped cardboard box (39 x 34 x 52 cm). Side rooms, under the counter, stacked as stock. |
| **FilingCabinet** | 0.47 × 1.32 × 0.66 | 2,976 | floor | Body: C+N; Plinth: C+N; Handle: constant; Recess: constant; Label: C | Project-made | 4-drawer light-grey steel filing cabinet, 0.47 x 1.32 x 0.62 m, labelled drawers. Rear office against a wall. |
| **FilingCabinetOpen** | 0.47 × 1.36 × 1.02 | 3,588 | floor | Body: C+N; Plinth: C+N; Handle: constant; Recess: constant; Label: C; Rail: constant; FolderKraft: constant; Folder: constant; Paper: constant | Project-made | Same cabinet with the top drawer pulled out 36 cm, hanging files rummaged and sheets lifted. Rear office: a searched-cabinet evidence spot (place markers and scale here). |
| **ShelvingSteelWide** | 2.35 × 2.31 × 0.72 | 8,396 | floor | Drawers: C+N+MS; Frame: C+N+MS | Poly Haven `steel_frame_shelves_03` (Ulan Cabanilla) | Wide steel-frame shelving with wooden boards and a two-drawer unit, 2.35 x 2.31 m. Rear office or a side room; load it with LeverArchFiles and CardboardBox. |

## Security

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **CCTVBulletHousing** | 0.14 × 0.21 × 0.39 | 8,507 | floor | Camera: C+N+MS; Window: glass constant (d 0.55) | Poly Haven `security_camera_02` (Garrison Gager, Yann Kervran) | Outdoor bullet camera in a weather housing on a wall bracket (39 cm); lens window faces +Z, bracket foot at the bottom/back. Outside above the storefront or in the side corridor; complements the ForensicModels SecurityCamera. |
| **CCTVDome** | 0.14 × 0.08 × 0.14 | 1,440 | none | Housing: C+N; Dome: glass constant (d 0.55); Inner: C+N; Lens: constant; IR: constant | Project-made | Ceiling dome camera (14 cm) with smoked dome and tilted lens. y = 0 is the ceiling surface. Sales-floor ceiling (2-3 units covering the cases) and the office. |
| **DVRRecorder** | 0.38 × 0.05 × 0.29 | 206 | floor | Case: C+N; Front: C; Rubber: constant | Project-made | 8-channel CCTV recorder box (38 x 29 cm) with status LEDs. Office shelf under the SecurityMonitor; or leave its spot empty (stolen recorder) with loose cables. |
| **SecurityMonitor** | 0.54 × 0.44 × 0.19 | 1,538 | floor | Housing: C+N; Screen: C | Project-made | Same monitor showing a four-camera CCTV grid; CAM 03 (office) reads VIDEO LOSS. Rear office beside the DVR: the lost office feed is an investigation cue. |

## Appliances

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **CoffeeMachine** | 0.33 × 0.39 × 0.51 | 1,666 | floor | Body: C+N; Panel: C+N+MS; Recess: constant; Steel: constant; GlossBlack: constant; Cup: constant | Project-made | Bean-to-cup coffee machine with brushed fascia, display, drip tray and two cups. Staff counter in the rear office, or a hospitality cabinet in the seating corner. |
| **MiniFridge** | 0.48 × 0.84 × 0.53 | 1,152 | floor | Body: C+N; Door: C+N; Gasket: constant; Handle: constant; Rubber: constant; Coil: constant | Project-made | Under-counter mini fridge, 48 x 84 x 50 cm, white. Rear office under a counter or in a side room. |
| **WaterCooler** | 0.31 × 1.50 × 0.38 | 2,748 | floor | Body: C+N; Recess: constant; Tray: C+N; TapRed: constant; TapBlue: constant; Bottle: glass constant (d 0.35); Vent: C | Project-made | Floor-standing water cooler with hot/cold taps and a 19-litre blue bottle, 1.5 m. Staff corner of the rear office or a side room. |

## Safety

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **ExitSign** | 0.36 × 0.14 × 0.03 | 110 | wall | Housing: C+N; Face: C | Project-made | Illuminated exit sign 36 x 13.5 cm with an original running-figure pictogram, arrow and EXIT. Above the rear exit and inside above the front door, about 2.2 m; always lit at night. |
| **FireAlarmCallPoint** | 0.10 × 0.14 × 0.03 | 2,059 | wall | CallPoint: C+N+MS | Poly Haven `fire_alarm` (Slinc) | Red manual fire alarm pull station (10 x 14 cm). Walls beside the rear exit and inside the front door, about 1.4 m high. |
| **FireExtinguisher** | 0.28 × 0.66 × 0.37 | 8,005 | floor | Body: C+N+MS; GaugeGlass: glass constant (d 0.25); Tag: C+N+MS | Poly Haven `korean_fire_extinguisher_01` (UM JOORIN) | 3 kg red ABC powder extinguisher (0.66 m on its floor stand), original English label and inspection tag. Beside the rear exit or the office door, under the ExitSign. |
| **SmokeDetector** | 0.12 × 0.04 × 0.12 | 556 | ceiling | Body: C+N; Slot: constant; LED: constant | Project-made | Ceiling optical smoke detector (11.6 cm) with status LED. One per room on the ceiling. |

## Building

| Model | Real size (m, x × y × z) | Tris | Pivot | Materials (C colour, N normal, MS metallic-smoothness) | Source | What it is / suggested placement in the night store |
|---|---|---|---|---|---|---|
| **CeilingDiffuser** | 0.60 × 0.07 × 0.60 | 104 | ceiling | Diffuser: C+N; Duct: constant | Project-made | Square 4-way ceiling air diffuser 60 x 60 cm. Sales-floor and office ceilings between the downlights. |
| **DoorLeverHandle** | 0.16 × 0.09 × 0.13 | 700 | none | Steel: C+N+MS; Keyhole: constant | Project-made | Brushed stainless lever on a round rose with an escutcheon; origin at the door face (z = 0), lever along +X. Rear office and side-room doors at 1.0-1.05 m; mirror X for the other hand / other side. |
| **DoorPullHandle** | 0.04 × 0.60 × 0.08 | 384 | none | Steel: C+N+MS | Project-made | Stainless pull bar, 60 cm long, 40 cm fixing centres; origin at the glass face. The forced storefront glass doors, vertical, centred about 1.0 m high (both faces). |
| **WallVent** | 0.42 × 0.22 × 0.02 | 168 | wall | Grille: C+N; Duct: constant | Project-made | Wall return-air grille 42 x 22 cm with angled louvres. Low on the office wall or high on the side walls. |

## Wiring ideas (night store)

- **Rear office scene**: OfficeDeskMetal against the wall; ComputerMonitor, Keyboard, Mouse, DeskPhone, StationerySet, PhotoFrameDesk and DeskLampArm on it; OfficeChair pushed back or tipped over; SecurityMonitor + DVRRecorder on ShelvingSteelWide (CAM 03 shows VIDEO LOSS: the office camera was cut); FilingCabinetOpen next to the open safes, LeverArchFiles on the shelf and one on the floor; WallClock above the door; LEDPanel + SmokeDetector on the ceiling.
- **Seating corner**: Rug, CoffeeTableConcrete, LoungeChairMidCentury, ArmChairModern, OttomanLeather beside the existing GlamVelvetSofa/SheenChair; PendantLampGlobe above; PictureFrameBlack/White on the wall; PottedPlantTall or SnakePlant in the corner; a vase or the succulent on the table.
- **Sales floor ceiling**: RecessedDownlight grid, TrackLight runs over the wall cases, CCTVDome over the central cluster and the counter, CeilingDiffuser between lights. At night most fittings are off: keep the lens/diffuser materials non-emissive and let the ExitSign and the monitors provide the only interior glow.
- **Exits**: ExitSign (always lit), FireAlarmCallPoint at 1.4 m and FireExtinguisher on its stand beside the rear exit; DoorPullHandle pair on the forced storefront doors; DoorLeverHandle on the office and side-room doors.
- **Side rooms / staff area**: ShelvingSteelWide with CardboardBox stock, WaterCooler, CoffeeMachine, MiniFridge, WallVent.
- Keep a primitive fallback in DemoAssetLibrary for every model, as for the other packs.

## Changes on October 15, 2026

- **PottedPlantTall** and **PottedPlantArrowhead** rebuilt: the pot shells keep their full source detail (smooth round rims, no facets or dents); the dense pebble layers (57k / 54k triangles) are baked onto 520-triangle heightfield discs with their own `Gravel` maps, whose edges sit inside the pot wall all round (`Source~/disc_edge_check.py`: no gap, no poke-through). The tall plant's leaf blades are alpha-cut cards (exact original outline in the alpha mask) and its stalks low-poly tubes; the arrowhead leaves keep their geometry with back faces. 0 faces disagree with their normals (before: 24-27 % of the pot triangles).
- **FireExtinguisher**: the Korean stencil and small text on the base plaque are now gone from the normal map too (the embossed relief was flattened), and the "UMS" hose print is removed from both the colour and the normal map.
- **PhotoFrameDesk** rotated -90 degrees about Y: the photo now faces +Z; size 0.20 x 0.25 x 0.10 m.
- **PictureFrameSilver renamed PictureFrameWhite** (Poly Haven's asset is a distressed white wooden frame, metallic about 0). Its inherited artwork (a painterly-filtered photograph with a third-party signature) is replaced by a public-domain painting, Paul Cezanne's *The Banks of the Marne* (c. 1888). Update any reference to the old name.
- **DeskLampArm, VaseCeramicTall, VaseCeramicBottle, PottedSucculent, CardboardBox, CCTVBulletHousing, FireExtinguisher** re-decimated from the sources: the earlier UV-exact decimation had opened cracks along the UV seams (holes in the desk-lamp shade and broken arm rods, holes at the vase bases, a slit down the succulent pot, a split box corner). Seams and open borders are now held fixed (the desk lamp, whose thin rods are seams everywhere, uses a seam-aware decimation re-split per UV island); after welding by position each model has the same open edges as its source (`Source~/cracks.py`; desk lamp: 24 sub-millimetre slivers). Triangle counts changed: DeskLampArm 15,000, VaseCeramicTall 5,388, VaseCeramicBottle 3,984, CCTVBulletHousing 8,507, FireExtinguisher 8,005, CardboardBox 3,000, PottedSucculent 4,999. Textures unchanged except the extinguisher fixes above.

## LOD variants

Lower-detail versions of FireExtinguisher and WallClock for a Unity LODGroup (October 15, 2026; **not imported or checked in Unity yet**). The full model is LOD0; `<Model>_LOD1.obj` and `<Model>_LOD2.obj` sit beside it and use its MTL and maps (`mtllib <Model>.mtl`, same materials, no texture change), with the same pivot and bounds. FireAlarmCallPoint has no LODs: it is a US-style pull station and is not placed in the night store.

| File | Triangles (target) | Groups | Bytes |
|---|---|---|---|
| FireExtinguisher.obj (LOD0) | 8,005 | Body 7,715; GaugeGlass 258; Tag 32 | 635,038 |
| FireExtinguisher_LOD1.obj | 2,000 (2,000) | Body 7,715 to 1,710; GaugeGlass 258 (kept); Tag 32 (kept) | 154,863 |
| FireExtinguisher_LOD2.obj | 646 (600) | Body 7,715 to 550; GaugeGlass 258 to 64; Tag 32 (kept) | 50,171 |
| WallClock.obj (LOD0) | 3,658 | Clock 3,130; Glass 528 | 289,109 |
| WallClock_LOD1.obj | 900 (900) | Clock 3,130 to 700; Glass 528 to 200 | 70,887 |
| WallClock_LOD2.obj | 316 (300) | Clock 3,130 to 252; Glass 528 to 64 | 25,285 |

Made by `Source~/make_lods.py`, a copy of ForensicModels' `decimate_exact.py` with the lods-street extensions (StreetModels `Source~/make_lods.py`): a seam-aware half-edge collapse in which every vertex and corner is an original `v/vt/vn` of the full model (no vertex moves and the UVs stay exact, so the maps fit unchanged); UV seams, hard edges and group joins shorten only along themselves, so no cracks open, and no collapse may turn a triangle against its vertex normals. Added for these props:

- **Parts** (connected pieces of a group, such as one clock hand) keep their exact bounding boxes, small parts can be locked unchanged, and the large parts can get their own triangle budgets. No collapse may make a triangle sample another part's texels (without this guard an orange patch showed on the black lever).
- **FireExtinguisher**: LOD1 keeps GaugeGlass and Tag unchanged and all 32 outer columns of the yellow band, so the recessed cylinder wall never shows through it. LOD2 leaves out the parts under 25 mm (gauge internals, lever pins, rivet heads, label tab) and the thin pull pin, tag wire and hose ferrule, which are under a pixel at LOD2 distances; the large parts get budgets (hose 420 to 28, stand 596 to 56, band 258 to 86, cylinder 1,598 to 140) and the band keeps every second outer column; 66 collapses cross a UV seam inside a part (corners move at most 0.2 in UV).
- **WallClock**: both LODs leave out the parts that face the wall (rear knob, rear plate and back cover, 718 triangles): they cannot be seen on a wall-mounted clock, so use the full model if the clock is ever shown from behind. Plain decimation turned the case ring into a jagged polygon whose triangles crossed the dial, so the case (a 96 x 12 lathe grid) is rebuilt on 32 (LOD1) or 12 (LOD2) of its columns from its own vertices, keeping every visible profile band with its UV island; the hour and minute hands and the dial are locked, and the second hand is decimated inside its exact box (178 to 66 / 178 to 18). LOD2 also leaves out the 6 mm centre cap and the arbor under it.

Checked by the script before anything is written (exit code 1 on any failure; all passed):

- triangles within 10 % of the target and of each fixed group share: FireExtinguisher_LOD2 is 7.7 % and WallClock_LOD2 5.3 % over, because the parts that remain stall at their box, seam and texel guards;
- bounds within 5 mm of the full model (measured: 0.0 mm) and the pivot kept (extinguisher: floor y = 0; clock: wall z = 0); same materials (usemtl set) and group order; every corner an original v/vt/vn; groups kept unchanged are identical; no zero-area triangle;
- every source part present with its box within 1 mm, apart from the parts left out on purpose (listed per file in sources.json); the clock hands are checked by name (hour and minute hand unchanged, second hand 178 to 66 at LOD1 / 178 to 18 at LOD2);
- no open edge with an end off the source's open borders (a crack along a UV seam would add one); `Source~/cracks.py` open edges: FireExtinguisher 571 to 298 / 98, WallClock 494 to 226 / 92 (full model to LOD1 / LOD2: never more);
- `Source~/check_obj.py`: 0 faces whose winding disagrees with their vertex normals (as in the full models), and trimesh reads every triangle;
- no triangle samples another part's texels, apart from 4 / 1 triangles of the clock case's hidden back band (LOD1 / LOD2), which faces the wall.

`Source~/LOD_Previews.jpg`: full model, LOD1 and LOD2 side by side (software renders with back faces culled, `Source~/render2.py`), plus a close-up of the clock dial: the hour, minute and second hands are intact at LOD1 (and at LOD2, where the centre cap is left out).

Suggested LODGroup transitions (screen-relative height; a starting point, unverified in Unity, tune on the Quest 2 with QualitySettings.lodBias 1): FireExtinguisher LOD0 above 0.25, LOD1 above 0.08, LOD2 above 0.015; WallClock LOD0 above 0.15, LOD1 above 0.035, LOD2 above 0.008; culled below. With a vertical field of view of about 90 degrees that is LOD1 from about 1.3 m and LOD2 from about 4 m for the extinguisher, LOD1 from about 1.1 m and LOD2 from about 4.5 m for the clock (where its 12-sided case is under about a pixel off round).

Rebuild from the repository root: `python3 -I Prototype/ThirdParty/InteriorModels/Source~/make_lods.py Prototype/ThirdParty/InteriorModels --previews` (deterministic: a rerun gives the same bytes). It rewrites the `lods` entries in models.json, the LOD entries in sources.json, this section and the total size above. `Source~/make_docs.py` rebuilds the pack without the LODs, so run make_lods.py after it.

## Not found / left out

- No CC0 or CC BY photoreal **monstera, ficus or snake plant** was reachable; PottedPlantTall is a small-leaved ficus-like tree from Poly Haven and SnakePlant is project-made. No monstera.
- No open-licensed realistic **wheeled office chair, monitor, keyboard, desk phone, water cooler, coffee machine, mini fridge, dome camera, exit sign, smoke detector, vents or door hardware** was found in reachable CC0/CC BY sources, so these are project-made (procedural) and simpler than the Poly Haven scans.
- **Shopping bags** and a **vase with flowers** were not added: JewelryModels/GiftBag and KhronosSamples/GlassVaseFlowers already cover them.
- BlenderKit items found in a GitHub mirror (monitor, fridge, coffee machine, rug, pendant, dracaena) are "Royalty Free" (no redistribution of the asset) and were rejected.

