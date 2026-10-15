# ForensicModels: burglary, forensic and police props for the night store

Models for the burglary aftermath and the police and forensic work in the night jewellery store: tools left behind by the burglars, forensic and police equipment, and scene debris (glass, papers). Made for Unity 6000.6.2f1 with the Built-in pipeline, no packages (OBJ/MTL plus textures). Prepared October 14, 2026. **Not imported or checked in Unity yet.**

- 15 photoscanned or PBR models from **Poly Haven** (CC0), converted to OBJ. Four colour maps had brand-style lettering painted out. The glove's woven tag was also removed from its smoothness and normal maps (October 15, 2026).
- 24 **project-made** models, plus one decal. Procedural geometry and textures are project-owned. All printed text is fictional.
- **LOD variants** (LOD1 and LOD2) of ForensicCase, Crowbar, Sledgehammer and FlatheadScrewdriver: eight OBJs added October 15, 2026 (see [LOD variants](#lod-variants)).
- Total size is 19.0 MB (18,983,652 bytes, including sources.json, LICENSE.txt and `Source~`). The largest file is under 0.7 MB. Every model has 4 to 9,120 triangles.

See LICENSE.txt for credits, and sources.json for mirror commits and SHA-256 of every source and shipped file. Authors are given in the Source column below; CC0 needs no attribution, but credit is welcome.

## Conventions

| Item | Convention |
|---|---|
| Units / axes | Metres, Y up. Every model sits on y = 0 and is centred in x/z, except the two hand torches (see below). Import with **Scale Factor 1** and Convert Units off. Unity's OBJ importer handles the handedness flip. |
| Orientation | Poly Haven tools keep the authored pose. Crowbar, sledgehammer, bolt cutters, hammer, screwdriver and magnifier **stand upright** (long axis = Y). Rotate them 90 degrees about X or Z to lie them on the floor. |
| `<Name>.obj/.mtl` | `map_Kd` points at `<Name>_Color.jpg` (or the named atlas). Unity's OBJ importer reads only Kd, `map_Kd` and `d`. Assign the other maps in code (DemoAssetLibrary) or on the material. |
| `_Color.jpg` | Base colour, sRGB. |
| `_Normal.jpg` | Tangent-space normal map, OpenGL +Y, which matches Unity. Set Texture Type to **Normal map**. Strength 1 (0.5 to 0.8 for the fabric items if they look too rough). |
| `_MetallicSmoothness.png` | R = metallic, G = B = 0, A = smoothness. **sRGB off**. In Standard: Metallic slot, Source = Metallic Alpha, Smoothness scale 1. This matches PBRExterior and NightMaterials. |
| Glass / lens sub-materials | MTL constants (Kd colour, `d` opacity). Use the project's glass material (Standard **Fade** or **Transparent**, smoothness ~0.95), as the case glass already does. |

## Burglary tools left behind

| Model | Real size (m, x × y × z) | Tris | Maps | Source | Suggested placement in the night store |
|---|---|---|---|---|---|
| **Crowbar** | 0.04 × 0.555 × 0.128 | 2,300 | Color 1K, Normal 1K, MS 512 | Poly Haven crowbar_01, by Alexander Otterbeck | On the floor just inside the forced storefront door. Pry marks on the door frame tie it to the entry point. |
| **Sledgehammer** | 0.21 × 1.006 × 0.09 | 2,596 | Color 1K, Normal 1K, MS 512 | Poly Haven sledgehammer_01, by Dylan Guzman | Dropped in the glass beside the smashed central display case. |
| **CrossPeinHammer** | 0.10 × 0.30 × 0.024 | 2,124 | 512 | Poly Haven cross_pein_hammer, by Tics (one record; see LICENSE.txt) | On the floor by a broken wall case, or on the counter. |
| **FlatheadScrewdriver** | 0.03 × 0.261 × 0.03 | 2,300 | 512 | Poly Haven flathead_screwdriver, by Dylan Detwiller | Rear office, under the forced office door lock or at the safe. |
| **BoltCutters** | 0.151 × 0.803 × 0.036 | 3,410 | Color 1K, Normal 1K, MS 512 | Poly Haven bolt_cutters_01 (rusty), by Antanas Kep, Yann Kervran | By the side-room door padlock, or abandoned near the rear office. |
| **WorkGloves** | 0.227 × 0.076 × 0.299 | 6,822 | Color 1K, Normal 1K, MS 512 | Poly Haven garden_gloves_01 (pair), by Satyaki Mandal | One spot near the rear exit or the pavement: a DNA and fibre evidence item. The woven brand-style tag on the glove back was removed from the colour, smoothness and normal maps. |
| **Stone** | 0.146 × 0.071 × 0.088 | 4,999 | 512 | Poly Haven stone_01 (photoscan), by Dario Barresi, Rico Cilliers | Inside the broken glass bay, among the shards. It is the object thrown through the window. |
| **BrickHalf** | 0.112 × 0.065 × 0.102 | 5,332 | 512 | Project-made | Alternative to the stone: a half brick with a fresh fracture face and mortar crusts, in a bay or on the pavement. |
| **DuffelBag** | 0.62 × 0.35 × 0.34 | 4,260 | Color 1K, Normal 512, MS 256 | Project-made | Black nylon holdall, half slumped. Abandoned by a side island or the rear door. |
| **Balaclava** | 0.29 × 0.04 × 0.40 | 4,608 | Color 1K, Normal 512, MS 256 | Project-made | Crumpled black rib-knit balaclava with an eye slot, lying flat. Rear exit or the pavement. |
| **Trashbag** | 0.53 × 0.58 × 0.46 | 4,482 | 512 | Poly Haven trashbag, by Benny Weimer | Bin bag on the pavement by the kerb (street realism), or a loot bag dropped outside. |

## Forensic and police equipment

| Model | Real size (m) | Tris | Maps | Source | Suggested placement / use |
|---|---|---|---|---|---|
| **EvidenceMarkerTent** | 0.103 × 0.048 × 0.051 | 96 | Atlas 1K PNG | Project-made | Yellow A-frame marker with a 10 cm ruler. The atlas holds numbers **1 to 16** in a 4 × 4 grid. The mesh shows cell 1; for number n set the material texture offset to ((n-1) % 4 × 0.25, -((n-1) // 4) × 0.25). Visual for the placed numbered markers. |
| **EvidenceMarkerCone** | 0.11 × 0.12 × 0.095 | 12 | Atlas 1K JPG | Project-made | Alternative design: a three-sided numbered cone, 12 cm tall. Same 1 to 16 atlas layout and offset rule. |
| **ForensicScaleL** | 0.15 × 0.002 × 0.10 | 20 | Color 1K | Project-made | L-shaped photo scale (cm, 18 % grey patch). Lay it beside shoe marks, tool marks or the stone before photos. Visual for the "scale" tool. |
| **EvidenceBagPaper** | 0.20 × 0.022 × 0.33 | 1,712 | Color 1K, Normal 512 | Project-made | Sealed kraft evidence bag with a filled-in label (fictional case). On the forensic case or the counter. |
| **EvidenceBagPlastic** | 0.23 × 0.008 × 0.30 | 884 | Color 1K PNG (RGBA) | Project-made | Tamper-evident polythene bag. Use Standard **Fade** with the colour alpha. |
| **ForensicCase** | 0.46 × 0.15 × 0.39 | 6,614 | Color 1K, Normal 512, MS 512 | Project-made | Aluminium crime-scene kit case, closed. Stage it on the common approach path just inside the door. |
| **FingerprintBrush** | 0.024 × 0.15 × 0.024 | 752 | 512 | Project-made | Fibreglass dusting brush. Lay it beside the powder jar on a display-case lid. |
| **FingerprintPowderJar** | 0.06 × 0.081 × 0.06 | 1,310 | Color 1K, Normal 512, MS 256 | Project-made | Black dusting powder jar. Same place as the brush. |
| **NitrileGloveBox** | 0.235 × 0.103 × 0.125 | 804 | Color 1K | Project-made | Blue nitrile glove box with a glove tuft. On the forensic case or the police car bonnet. Second material `GloveNitrile` is a flat blue. |
| **TacticalFlashlight** | 0.039 × 0.039 × 0.157 | 1,340 | 512 | Project-made | Black aluminium torch. **Pivot at the middle of the grip, beam along +Z**, ready as the hand-held flashlight visual. `TorchEmitter` is the LED disc inside the reflector (z = 0.08); give it emission when switched on. `TorchLens` (z = 0.092) needs the glass material. |
| **UVTorch** | 0.039 × 0.039 × 0.157 | 1,340 | Color 512 (**reuse TacticalFlashlight_Normal.jpg and _MetallicSmoothness.png**) | Project-made | The same body with a violet bezel and lens, for the UV torch tool. Same pivot. |
| **PoliceRadio** | 0.061 × 0.208 × 0.039 | 2,000 | Color 1K, Normal 512, MS 256 | Project-made | Handheld radio with antenna and keypad, upright. On the counter or the police car seat. |
| **CameraTripod** | 0.97 × 1.51 × 0.84 | 3,704 | Color 1K, Normal 512, MS 256 | Project-made | Aluminium tripod with pan head and an unbranded DSLR (lens along +Z). Beside a display case. Visual anchor for the photo tool. Sub-material `LensGlass` is front-element glass. |
| **ShoePrintCast** | 0.33 × 0.032 × 0.16 | 7,118 | Color 1K, Normal 1K, MS 256 | Project-made | Dental-stone cast of a trainer sole with scratched case notes. On the forensic case, or by a soft verge outside. Use it with ForensicScaleL. |
| **Clipboard** | 0.229 × 0.042 × 0.337 | 6,176 | Color 1K, Normal 512, MS 256 | Poly Haven clipboard, by ProgrammerOnCoffee | Scene attendance log at the cordon. The board surface is at y = 0.005 and the clip at the −Z end. Put **PaperSheetForm** on it at local (0, 0.0055, 0.015); the form's header then sits under the clip. |
| **PaperSheetForm** | 0.21 × 0 × 0.297 | 4 | ForensicPapers_Color.jpg | Project-made | A4 "Scene attendance log" (two-sided quad). On the clipboard or the counter. |
| **MeasuringTape** | 0.041 × 0.073 × 0.169 | 2,868 | 512 | Poly Haven measuring_tape_01, by Kuutti Siitonen | Next to the scale and shoe marks. |
| **MagnifyingGlass** | 0.132 × 0.272 × 0.027 | 7,240 | 512 | Poly Haven magnifying_glass_01, by Nazar Borodavka | On the forensic case. Lens sub-material `magnifying_glass_01_lense` needs the glass material. |
| **PortableSearchlight** | 0.165 × 0.185 × 0.253 | 8,999 | Color 1K, Normal 512, MS 512 | Poly Haven portable_searchlight, by Elijah Cragg | Visual for the placeable scene lamp. The lens faces **+Z**; put a spot light just in front of it, pointing +Z. |
| **PoliceCap** | 0.24 × 0.16 × 0.31 | 2,898 | Color 1K, Normal 512, MS 256 | Project-made | Navy peaked cap with a black band, patent visor and a generic star badge (no lettering). Peak toward +Z. On the police car roof or bonnet, or on the counter. |
| **PoliceBarrier** | 1.20 × 1.05 × 0.69 | 236 | Color 1K, Normal 512, MS 512 | Project-made | A-frame sawhorse with blue/white retro-reflective board, "POLICE / DO NOT CROSS". Two or three on the pavement across the entrance, inside the tape line. |

## Scene items

| Model | Real size (m) | Tris | Maps | Source | Suggested placement |
|---|---|---|---|---|---|
| **GlassShardsLarge** | 1.07 × 0.04 × 0.75 | 1,376 | MTL constants (`GlassFace`, `GlassEdge`) | Project-made | Large annealed-glass plates and daggers under a smashed display case or a broken bay. |
| **GlassShardsScatter** | 1.06 × 0.02 × 0.71 | 4,144 | MTL constants | Project-made | Mid-size shard spray thrown outward from an impact. Rotate copies so the spray points away from where the blow came from. |
| **TemperedGlassCrumbs** | 0.78 × 0.023 × 0.60 | 9,120 | MTL constants | Project-made | Cube-like crumbs of toughened glass. Under the forced storefront door leaf and its frame. |
| **GlassCrackDecal** | texture only, 1024 px | - | `_ColorAlpha.png` (RGBA) + `_Normal.png` | Project-made | Spider-web impact (radial and concentric cracks) for cracked panes that are still intact. Quad on or 1 mm in front of the pane, Standard Fade, smoothness 0.9. Suggested size 0.6 to 1.0 m. |
| **ScatteredPapers** | 1.06 × 0.03 × 0.83 | 1,536 | ForensicPapers_Color.jpg | Project-made | Fictional stock inventory, certificates, scene log, delivery note and receipts, slightly curled. Rear office floor between the two open safes. |
| **OfficeNotepads** | 1.25 × 0.022 × 0.96 | 666 | Color 1K, Normal 512, MS 256 | Poly Haven office_notepads, by Ulan Cabanilla | Ten legal pads and notepads swept off a shelf, re-laid as a floor scatter. Rear office. |
| **LeatherBinder** | 0.58 × 0.025 × 0.20 | 8,000 | Color 1K, Normal 512, MS 256 | Poly Haven binder_notebook, by DaDrood | Open leather binder and a closed one. On the office desk or the floor by the safes. |
| **SecurityCamera** | 0.17 × 0.29 × 0.55 | 9,001 | Color 1K, Normal 512, MS 512 | Poly Haven security_camera_01, by Alexander Otterbeck, Yann Kervran | CCTV housing on its bracket. The lens window faces **+Z** and the bracket foot is at the back (−Z end, lowest point). Rotate it so the foot meets the wall or ceiling, high in a corner inside the shop or over the entrance outside. It is a natural "check the CCTV" cue. The window sub-material is dark glass. |

## LOD variants

Added October 15, 2026. **Not imported, wired or checked in Unity yet.** ForensicCase and the three upright tools have two lower-detail meshes each for a Unity LODGroup (LOD0 is the original OBJ, unchanged). They were decimated offline by `Source~/make_lods.py`. No existing model, MTL or texture was changed, and no MTL or texture was added.

| Model | Level | File | Triangles (target) | Bytes | Surface deviation, mean / max |
|---|---|---|---|---|---|
| ForensicCase | LOD0 | ForensicCase.obj | 6,614 | 355,063 | - |
| ForensicCase | LOD1 | **ForensicCase_LOD1.obj** | 1,600 (1,600) | 84,152 | 0.43 / 1.9 mm |
| ForensicCase | LOD2 | **ForensicCase_LOD2.obj** | 500 (500) | 26,005 | 1.04 / 5.6 mm |
| Crowbar | LOD0 | Crowbar.obj | 2,300 | 119,886 | - |
| Crowbar | LOD1 | **Crowbar_LOD1.obj** | 600 (600) | 33,817 | 0.02 / 0.5 mm |
| Crowbar | LOD2 | **Crowbar_LOD2.obj** | 200 (200) | 12,046 | 0.29 / 2.3 mm |
| Sledgehammer | LOD0 | Sledgehammer.obj | 2,596 | 135,985 | - |
| Sledgehammer | LOD1 | **Sledgehammer_LOD1.obj** | 650 (650) | 37,203 | 0.20 / 2.4 mm |
| Sledgehammer | LOD2 | **Sledgehammer_LOD2.obj** | 199 (200) | 13,526 | 0.97 / 6.6 mm |
| FlatheadScrewdriver | LOD0 | FlatheadScrewdriver.obj | 2,300 | 125,203 | - |
| FlatheadScrewdriver | LOD1 | **FlatheadScrewdriver_LOD1.obj** | 600 (600) | 35,577 | 0.17 / 1.4 mm |
| FlatheadScrewdriver | LOD2 | **FlatheadScrewdriver_LOD2.obj** | 200 (200) | 13,131 | 0.82 / 3.6 mm |

Surface deviation is the distance from every source vertex and source triangle centre to the LOD surface. The eight LOD files add 255,457 bytes, and `Source~/make_lods.py` adds 48,586.

How to use them:

- **Same materials, same maps.** Each LOD names the original MTL (`mtllib <Model>.mtl`) and keeps the same `g`/`usemtl` names (`CaseAtlas`, `crowbar_01`, `sledgehammer_01`, `flathead_screwdriver`). Give the LOD1 and LOD2 renderers the LOD0 material instances, with the normal and metallic/smoothness maps set as in the wiring notes, not the copies the OBJ importer makes. The LODGroup then adds no materials or textures. Every UV is an original one, so the maps fit exactly at every level, and the night look (normal and smoothness maps under the torch beam) carries over.
- **Same frame.** The bounds are identical to LOD0 (0 mm difference measured). The tools stand upright (long axis Y, base on y = 0, centred in x/z), and the case sits on y = 0, centred. Colliders, grab points and placements measured on LOD0 stay valid. Keep one collider on the LOD0 object or the LODGroup root, not one per level.
- **Normals.** The ForensicCase LODs carry original `vn` lines (176 and 40), like LOD0. The three tools have no `vn` at any level, as before, so Unity calculates their normals on import. Give the LOD files the same Normals and Smoothing Angle import settings as LOD0.
- **Suggested LODGroup thresholds** (screen-relative height, LOD Bias 1; tune in Unity): LOD0 above 0.20, LOD1 from 0.20 to 0.06, LOD2 from 0.06 to 0.01, culled below 0.01. Unity takes the LODGroup size from the largest bounds axis. That gives these switch distances (Quest 2 with a 93-degree vertical field of view / desktop camera with 60 degrees):

  | Model (size) | LOD0 -> LOD1 | LOD1 -> LOD2 | Culled |
  |---|---|---|---|
  | ForensicCase (0.46 m) | 1.1 m / 2.0 m | 3.7 m / 6.7 m | 22 m / 40 m |
  | Crowbar (0.56 m) | 1.3 m / 2.4 m | 4.4 m / 8.0 m | 26 m / 48 m |
  | Sledgehammer (1.01 m) | 2.4 m / 4.4 m | 8.0 m / 14.5 m | 48 m / 87 m |
  | FlatheadScrewdriver (0.26 m) | 0.6 m / 1.1 m | 2.1 m / 3.8 m | 12 m / 23 m |

  A tool held in VR is closer than its LOD1 distance, so it shows LOD0.
- **Fallback.** Load the LOD files through DemoAssetLibrary only when they exist. A project unpacked from a ZIP without them keeps LOD0 alone, with no LODGroup, as it does now.

How they were made:

- `Source~/make_lods.py` is a copy of the StreetModels LOD tool (`Prototype/ThirdParty/StreetModels/Source~/make_lods.py` as of October 15, 2026, SHA-256 3a85204f2f97b59c...). That tool extends this pack's `decimate_exact.py`, which is unchanged and still reproduces the shipped pack. Changes in the copy: the model table, the source's leading comment line is kept at the top of each LOD, the VERIFY checks below, and the sources.json entries.
- Seam-aware half-edge collapse with quadric error. Corners are split per (v, vt, vn), and **no vertex moves**: every corner is an original v/vt (and vn) line of the source, written as its original text. UV seams and hard normal edges shorten only along themselves, on both sides at once. Each face of the bounding box keeps its last vertex, and no triangle over 1 cm2 folds inside out. Both levels are seam-exact (no relaxed collapses). A relaxed ForensicCase_LOD2, tried first, smeared the left latch.
- Open borders lose edges only along themselves, and no new open edge appears. ForensicCase keeps 4 at every level and Crowbar has none. Sledgehammer goes 16 -> 16 -> 7 (the handle end inside the head). FlatheadScrewdriver goes 52 -> 16 -> 12 over three rims:
  - the 5 mm opening where the shaft enters the ferrule becomes a triangle at both levels;
  - the shaft's open lower end inside the ferrule (4 mm) goes from 12 to 4 edges;
  - the ferrule's lower rim on the handle (17 mm) goes from 16 to 9, then 5 edges.
- Checked by make_lods.py before anything is written. On any failure it exits non-zero and writes nothing. The checks:
  - triangles within 10 % of the target (all exact except Sledgehammer_LOD2, 199 of 200);
  - bounds within 5 mm of the source (0 measured), base on y = 0, and the tools upright;
  - the same `usemtl` set and group order, and vt/vn as in the source (no vn on the tools);
  - every index valid, every face a triangle, and no zero-area triangle (smallest 1.4e-7 m2);
  - every corner an original value, no new open edge, and no triangle facing against its normals (case: 0 before and after).

  The results were also re-checked with a separate script during preparation (trimesh triangle counts, bounds, indices, SHA-256 against sources.json). That script is not shipped.
- To rerun, from the repository root: `python3 -I Prototype/ThirdParty/ForensicModels/Source~/make_lods.py Prototype/ThirdParty/ForensicModels`. It takes about 35 s and is deterministic, so a rerun gives the same bytes. It also rewrites the LOD entries, its own entry and the `lod_variants` block in sources.json, and checks that every other entry is unchanged.
- sources.json has:
  - a files entry per LOD: bytes, SHA-256, `derived_from` with the source's SHA-256, `tool` and `tool_sha256`, target and triangles, `derivation`, licence and VERIFY measures;
  - an entry for make_lods.py;
  - `lod_variants`, with the suggested transitions.

## Wiring notes (for whoever integrates the pack)

- **DemoAssetLibrary.** Load `<Name>.obj` and keep a primitive fallback. Set `_BumpMap` from `_Normal.jpg` and `_MetallicGlossMap` from `_MetallicSmoothness.png` with `_GlossMapScale` 1, as StoreKit.Surface already does for NightMaterials. Turn on `_NORMALMAP` and `_METALLICGLOSSMAP`.
- **Performance.** Every model is 10k triangles or fewer, and all are single-material except the glass, lens and torch parts. Mark the static ones (shards, papers, barrier, camera) as static for batching. The textures are already 512 to 1K. Use the default compression; for Quest Link, max size 1024 is enough.
- **Colliders.** Use box colliders for grabbable items, and none for shards and papers (decor only). The torches and cones are small, so make the trigger slightly larger for VR grabbing.
- **Night look.** The black items (duffel bag, balaclava, gloves, crowbar) depend on their normal and smoothness maps to read under the torch beam. Do not drop those maps.
- **Not included.** There are no animations, no rigs (the Poly Haven rigs for bolt cutters and the camera were not converted, so the static pose only), LODs only for the four models under LOD variants, and no Unity `.meta` files (Unity creates them on import).
- **Source~/** holds the scripts that made or converted everything. Unity ignores it. `delabel.py` painted lettering out of four colour maps; `delabel_glove_maps.py` rebuilds WorkGloves_MetallicSmoothness.png and WorkGloves_Normal.jpg from the 2K Poly Haven maps with the glove tag removed. `make_lods.py` makes the eight LOD variants and their sources.json entries. To rebuild the procedural items, run `proc_scene.py`, `proc_kit.py`, `proc_evidence.py` and `proc_glass.py` with an output folder and `meshkit.py` alongside, then `assemble.py`.
