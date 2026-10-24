# ForensicModels: burglary, forensic and police props for the night store

Models for the burglary aftermath and the police and forensic work in the night jewellery store: tools left behind by the burglars, forensic and police equipment, and scene debris (glass, papers). Made for Unity 6000.6.2f1 with the Built-in pipeline, no packages (OBJ/MTL plus textures). Prepared October 14, 2026. **Not imported or checked in Unity yet.**

- 15 photoscanned or PBR models from **Poly Haven** (CC0), converted to OBJ. Four colour maps had brand-style lettering painted out. The glove's woven tag was also removed from its smoothness and normal maps (October 15, 2026).
- 24 **project-made** models, plus one decal. Procedural geometry and textures are project-owned. All printed text is fictional.
- **LOD variants** (LOD1 and LOD2) of ForensicCase, Crowbar, Sledgehammer and FlatheadScrewdriver: eight OBJs added October 15, 2026 (see [LOD variants](#lod-variants)).
- **CSI consumables and evidence** (October 24, 2026): 29 more project-made models and two decal textures (magnetic wand, gel and hinge lifters, backing and lift cards, swabs, test kit, silicone casting set, dental-stone bowl and frame, tags and labels, biohazard bin, stepping plate, ALS goggles, PPE, trace specimens, footwear and pry-mark decals), 3,209,775 bytes; see [CSI consumables and evidence](#csi-consumables-and-evidence).
- **Poly Haven batch 2** (October 24, 2026): 12 more CC0 models converted from public GitHub mirrors (lifting-tape roll, reagent bottle, dental-stone bag, cardboard box, toolbox, bat, screwdriver, lighter, spectacles, watch, cigarette butt, reagent bottles), 6,954,056 bytes in 60 files. Three colour maps were rewritten (tape, reagent bottle, dental-stone bag) and four wordmarks painted out. Rows marked *October 24* in the tables below. Scripts: `Source~/relabel_ph2.py`, `Source~/build_ph2.py`, `Source~/write_sources_ph2.py`. **Not imported or checked in Unity yet.**
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
| **BurglarToolbox** | 0.400 × 0.305 × 0.271 | 9,994 | Color 1K, Normal 1K, MS 512 | Poly Haven metal_toolbox, by Mateusz Sadek (*October 24*) | Dark green steel toolbox, modelled **open** (lid up, tray visible); a burglar's tool carrier dropped by the rear office or the side-room door. The round brand-style sticker on the front was painted over. Lid and handle decimated, body kept. |
| **BaseballBat** | 0.064 × 0.913 × 0.064 | 2,672 | Color 1K, Normal 512, MS 256 | Poly Haven baseball_bat, by Enrique Martín (*October 24*) | Wooden bat, **upright** (long axis Y, knob at y = 0). Smash-and-grab weapon alternative to the sledgehammer: lay it in the glass by a smashed case. |
| **RedScrewdriver** | 0.027 × 0.209 × 0.027 | 3,412 | 512 | Poly Haven screwdriver, by PierreB3D (*October 24*) | Second screwdriver, red handle, **upright** like FlatheadScrewdriver (handle at y = 0, tip up). The tip is a flat blade (the Poly Haven model is not a Phillips). Handle wordmark painted out. Rear office lock or the display-case hinges. |

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
| **LiftingTapeRoll** | 0.091 × 0.050 × 0.090 | 1,408 | Color 512, Normal 512, MS 256 | Poly Haven medical_tape, by Miroslav Turura (*October 24*) | Roll of 2 in (0.05 m) clear fingerprint lifting tape, lying flat (roll axis Y). Scaled ×2 from the 25 mm medical tape; the tape faces were recoloured to a clear, slightly translucent off-white film, the cardboard core toned light kraft. Two materials: `lifting_tape` (d 0.9; give it the Fade glass-style material, smoothness ~0.85 already in the MS alpha) and `tape_core` (opaque). Beside the powder jar and brush, or on a backing card. |
| **ReagentSprayBottle** | 0.157 × 0.320 × 0.117 | 4,624 | Color 1K, Normal 512, MS 512 | Poly Haven all_purpose_cleaner, by Kuutti Siitonen (*October 24*) | 1.5 l white polyethylene bottle with a red flip cap and a plain white label "LATENT BLOOD REAGENT" (front, with a smaller instruction label on the back; fictional text, no pictograms, no brand). **Note:** the source is a jug-style bottle, not a trigger sprayer; a trigger head would be project geometry. On the forensic case, for the blood-reagent step in darkness. |
| **DentalStoneBag** | 0.462 × 0.180 × 0.700 | 844 | Color 1K, Normal 512, MS 512 | Poly Haven cement_bag, by PierreB3D (*October 24*) | Lying paper sack, off-white/grey, printed "DENTAL STONE / forensic casting grade / 11 kg" on both faces, sides and ends (long axis Z). The whole colour map was rebuilt; nothing of the cement print remains. By the casting set (RubberMixingBowl, CastingFrame) outside the window or by the rear door. |
| **ReagentBottles** | 0.150 × 0.107 × 0.150 | 2,897 | Color 512, Normal 512, MS 256 | Poly Haven chemistry_set, by Jiří Ptáček (*October 24*) | Three stoppered glass bottles (two corks, one glass lid) standing together in an L, 5 cm pitch, 6.5 to 10 cm tall, extracted from the chemistry set. Sub-material `chemistry_set_glass` is MTL glass constants (project glass material); `chemistry_set` is the cork texture. On the forensic case or the counter as the reagent and developer bottles. |
| **EvidenceBox** | 0.387 × 0.341 × 0.515 | 5,999 | Color 1K, Normal 1K, MS 512 | Poly Haven cardboard_box_01, by Rahul Chaudhary (*October 24*) | Worn, taped cardboard box (photoscan, decimated 16,952 -> 5,999; nothing added) as the evidence transport box by the door, or stock packaging in the rear office. |

## CSI consumables and evidence

Added October 24, 2026: 29 project-made models and two decal textures of crime-scene consumables and evidence items for the fieldwork workflow (powder, lift, swab, test, cast, package). **Not imported, wired or checked in Unity yet.** Generated by `Source~/proc_csi.py` and assembled by `Source~/assemble_csi.py` (same OBJ rounding and map conventions as the rest of the pack; see [Conventions](#conventions)). Real sizes follow the research notes in `research_notes/Crime scene forensic procedures/` (hinge lifters 2 × 4 in, backing cards 3 × 5 in, BVDA gel lifter 13 × 18 cm, Mikrosil 7 oz, stepping plate 350 × 350 mm, Sirchie casting frame 7.25 × 18 in). Every model sits on y = 0, centred in x/z; every model is under 2,400 triangles; textures are 1024 px or smaller (most 512). Printed text is fictional and brand-free; the short Arabic lines use the project's Noto Sans Arabic (Prototype/ThirdParty/Fonts/Arabic). The batch adds 3,209,775 bytes in 123 files (plus the two scripts). Transparent parts (`CoverSheetPET`, `LifterFilm`, `TubeClear`, `Polycarbonate`, `Acetate`, `LensOrange`) are MTL constants for the project's glass/Fade material, as in the table above.

| Model | Real size (m, x × y × z) | Tris | Maps | Source | Suggested use in the night store and the fieldwork workflow |
|---|---|---|---|---|---|
| **MagneticWand** | 0.020 × 0.176 × 0.019 | 1,316 | Color 512, Normal 512, MS 256 | Project-made | Magnetic powder applicator: 0.14 m pen body with plunger and an 18 mm powder ball at the tip (`PowderBall`), no bristles. Upright, ball at the base. The wand for black magnetic powder on textured plastics and u-PVC (door frame, till housing); the alternative to FingerprintBrush, beside FingerprintPowderJar on the kit case. |
| **GelLifterBlack** | 0.130 × 0.028 × 0.180 | 1,436 | Color 1K, MS 256 | Project-made | BVDA-style 13 x 18 cm gelatin lifter: 0.6 mm gel on a carrier, clear PET cover sheet (`CoverSheetPET`, glass material) curled up at the +x/+z corner, printed back. Black for dust marks and aluminium powder. Lift step for footwear dust marks on the shop floor and for textured surfaces. |
| **GelLifterWhite** | 0.130 × 0.028 × 0.180 | 1,436 | Color 1K, MS 256 | Project-made | Same, white gel: for black-powder marks. |
| **BackingCardWhite** | 0.127 × 0.001 × 0.076 | 12 | Color 1K, MS 256 | Project-made | 3 x 5 in (76 x 127 mm) backing card, plain front up, printed LP-3 lift form on the underside (case, lift no., location, date, officer, orientation arrow; English and Arabic). Takes black-powder lifts. The card the lift tape is laid on; fill the form after the lift. |
| **BackingCardBlack** | 0.127 × 0.001 × 0.076 | 12 | Color 1K, MS 256 | Project-made | Same, black card with white print: for aluminium, white and fluorescent powder lifts. |
| **LiftCardWithPrint** | 0.127 × 0.001 × 0.076 | 24 | Color 1K, MS 256 | Project-made | The white card after a lift: a 90 x 50 mm clear tape strip (`LiftTape`) carrying two black-powder ridge impressions and a felt-pen lift number (ridge art from the project's SurfaceDecals/Fingerprints_Atlas.png, synthetic). Swap BackingCardWhite for it when the tape is pressed onto the card; show it in the evidence log. |
| **HingeLifterBlack** | 0.051 × 0.049 × 0.102 | 24 | Color 512, MS 128 | Project-made | 2 x 4 in hinge lifter: card flat, hinge at the -z short edge, clear adhesive flap (`LifterFilm`, glass material) open at 30 degrees. Single-piece lift on flat glass and chrome (display cases, door push plates): close the flap over the developed mark. |
| **HingeLifterWhite** | 0.051 × 0.049 × 0.102 | 24 | Color 512, MS 128 | Project-made | Same, white card. |
| **SwabTube** | 0.016 × 0.150 × 0.016 | 832 | Color 512, Normal 512, MS 256 | Project-made | Sterile swab in a 150 mm clear tube (`TubeClear`, glass material) with blue cap and label band; the swab shows inside. Upright. Sealed state of the DNA swab before use, and the sample tube in the kit slots afterwards. |
| **SwabOpen** | 0.005 × 0.150 × 0.005 | 180 | Color 256, Normal 256, MS 256 | Project-made | The swab alone: 150 mm polystyrene shaft, cotton tip at the top. The held swab for blood on the broken glass and touch DNA on the handles; moisten it from DistilledWaterAmpoule first. |
| **SwabBox** | 0.175 × 0.032 × 0.032 | 108 | Color 1K, Normal 512, MS 128 | Project-made | 175 x 32 x 32 mm kraft swab drying box with label, ventilation holes and biohazard mark; lies flat, label up. Packaging step for a used swab (air-dry before sealing). On the kit case or the counter. |
| **DistilledWaterAmpoule** | 0.018 × 0.074 × 0.018 | 444 | Color 256 | Project-made | 10 ml LDPE sterile-water ampoule with twist-off tab, 74 mm, upright. Translucent: Standard Fade with the colour alpha (`d` 0.85). Moistens the swab for dried stains. |
| **KMTestKit** | 0.120 × 0.107 × 0.103 | 1,404 | Color 1K | Project-made | Open 120 x 80 x 30 mm case, lid leaning back at 105 degrees with the instructions inside; three 15 ml dropper bottles (1 ethanol, 2 reagent, 3 peroxide) lie inside with a filter-paper pad. Presumptive blood test on a swab (pink = positive) before packaging. On the counter beside the swabs. |
| **MikrosilTube** | 0.066 × 0.042 × 0.192 | 1,164 | Color 1K, Normal 512, MS 256 | Project-made | 7 oz (198 g) brown silicone casting putty tube, 192 mm with cap, lying on its side (cap toward +z, flat crimp at -z). Tool-mark casting of the pry marks on the door frame and the safe, with MikrosilCatalyst, MixingSpatula and ToolMarkCast. |
| **MikrosilCatalyst** | 0.030 × 0.019 × 0.117 | 1,164 | Color 512, Normal 512, MS 256 | Project-made | 117 mm catalyst tube for the putty, lying on its side. |
| **MixingSpatula** | 0.150 × 0.002 × 0.018 | 68 | Color 256 | Project-made | Wooden 150 x 18 mm mixing spatula (tongue depressor), lying flat along X. Beside the tubes. |
| **ToolMarkCast** | 0.060 × 0.010 × 0.025 | 1,296 | Color 512, Normal 512, MS 128 | Project-made | Cured brown silicone cast strip 60 x 25 x 4 mm, striation relief in the normal map, one edge (+z) curled up; striated face up. Result of the tool-mark cast step; package it in SwabBox or a small bag. |
| **MixingBowlRubber** | 0.164 × 0.071 × 0.157 | 760 | Color 512, Normal 512, MS 256 | Project-made | Flexible black rubber dental-stone mixing bowl, 0.16 m diameter, dried stone residue inside. Beside the casting frame on the pavement or the soft verge; casting step for the shoe impression outside. |
| **CastingFrame** | 0.457 × 0.025 × 0.187 | 636 | Color 256, Normal 256, MS 128 | Project-made | Folding aluminium casting frame 7.25 x 18 in (184 x 457 mm): open rectangle, 25 mm bars, corner hinge knuckles. Set around the outdoor shoe impression before pouring; ShoePrintCast is the result. |
| **EvidenceTag** | 0.065 × 0.002 × 0.143 | 660 | Color 512 | Project-made | 60 x 120 mm manila tag with metal eyelet (`Eyelet`), cotton string (`String`), red EVIDENCE header and item fields, chain-of-custody lines on the back; lies flat, eyelet toward -z. Tie to the crowbar, bolt cutters or duffel bag: the labelling step for items too large for a bag. |
| **EvidenceLabelRoll** | 0.080 × 0.050 × 0.080 | 276 | Color 512 | Project-made | Roll of white EVIDENCE labels, 80 mm diameter, 50 mm wide, standing on its face with the leading label peeling at +x. On the kit case: label dispenser for sealed bags and boxes. |
| **BiohazardBinSmall** | 0.180 × 0.286 × 0.135 | 582 | Color 1K | Project-made | 5 L yellow sharps and infectious-waste container with a red lid and an open flap over the aperture; generic biohazard pictogram, English and Arabic text, MAX line. Beside the kit case or the police car: disposal of used swabs, gloves and glass-handling waste. |
| **SteppingPlate** | 0.350 × 0.026 × 0.350 | 524 | MTL constants | Project-made | 350 x 350 mm clear polycarbonate stepping plate (`Polycarbonate`: glass material, alpha ~0.3) on four 20 mm rubber feet; MTL constants only. Two or three on the common approach path across the glass-strewn floor: the player walks on them, not on the floor. |
| **OrangeGoggles** | 0.162 × 0.078 × 0.107 | 720 | Color 512, Normal 256, MS 128 | Project-made | ALS viewing goggles: black vented frame, orange lens (`LensOrange`: Fade, orange tint, alpha 0.55), elastic strap; standing upright, lens toward +z. On the kit case next to UVTorch; worn (hidden from the player camera or shown on the NPC) while the blue-light search runs, tinting the view orange. |
| **FaceMask** | 0.255 × 0.011 × 0.095 | 2,356 | Color 512, Normal 256, MS 64 | Project-made | Folded pale-blue surgical mask with three pleats, nose strip (at -z) and ear loops (`EarLoop`), lying flat. PPE before the DNA steps; on the kit case or beside NitrileGloveBox. |
| **ShoeCoverPair** | 0.297 × 0.036 × 0.317 | 1,344 | Color 512, Normal 512, MS 128 | Project-made | Two blue disposable overshoes crumpled on the floor, gathered elastic opening on top. Inside the door on the approach path: the PPE step at the cordon. |
| **HairFibreSpecimen** | 0.040 × 0.001 × 0.040 | 36 | Color PNG (RGBA) 512, MS 128 | Project-made | 40 x 40 mm clear acetate (`Acetate`, glass material) with a 12 mm tape strip holding five dark hairs and two coloured fibres (`SpecimenTape`: Standard Fade with the PNG alpha, tape 0.3, hairs opaque) and a small label. Packaged state of a tape-lifted hair and fibre trace. |
| **FibreTuft** | 0.014 × 0.002 × 0.013 | 768 | MTL constants | Project-made | Tiny tuft of red textile fibres, about 12 mm, MTL constant. Snagged on a glass edge of the broken bay or the forced door frame: trace target to photograph, then tape-lift (becomes HairFibreSpecimen). |
| **ElectrostaticLiftFilm** | 0.300 × 0.002 × 0.400 | 880 | Color 1K | Project-made | Black 30 x 40 cm electrostatic lifting film, nearly flat, with the faint grey dust shoe mark transferred (long axis Z). On the floor beside ForensicScaleL: result of lifting FootwearDustMark electrostatically. |
| **FootwearDustMark** | texture only, 1024 px | - | `FootwearDustMark_ColorAlpha.png` (RGBA) + `FootwearDustMark_Normal.png` (512 px) | Project-made | Partial trainer sole in grey dust with scuff streaks, toe toward +u, on a transparent background. Quad 0.32 m on the floor inside the door (pavement dust), Standard Fade, 1 mm above the floor; ForensicScaleL beside it. The mark GelLifterBlack or ElectrostaticLiftFilm recovers, and a photography target. |
| **PryMark** | texture only, 1024 px | - | `PryMark_ColorAlpha.png` (RGBA) + `PryMark_Normal.png` (512 px) | Project-made | Two overlapping flat-blade pry impressions (25 x 6 mm) with chipped paint and drag scratches, centred in the texture. Quad 0.12 m on the door frame at the lock, Standard Fade, 1 mm proud of the frame. Tool-mark target for photograph, scale and the Mikrosil cast. |
| **Lighter** | 0.080 × 0.065 × 0.014 | 3,366 | Color 512, Normal 512, MS 256 | Poly Haven vintage_lighter, by Slinc (*October 24*) | Brass flip lighter with the lid open (lid to −X), standing on its base. Dropped-property evidence by the rear door or the pavement; a fingerprint carrier (powder step). The engraved wordmark on the case was painted out; the second material `vintage_lighter_alpha` (60 tris) keeps the colour map. |
| **Spectacles** | 0.158 × 0.046 × 0.149 | 9,000 | Color 512, Normal 512, MS 256 | Poly Haven round_spectacles, by Sean Buckley (*October 24*) | Round metal spectacles lying on their temples (temples toward −Z). Dropped-property evidence inside the shop (victim or burglar). Lens sub-material `round_spectacles_glass` needs the glass material. Decimated 11,810 -> 9,000. |
| **DroppedWatch** | 0.036 × 0.008 × 0.217 | 9,002 | Color 512, Normal 512, MS 256 | Poly Haven digital_wrist_watch, by Adrian C (*October 24*) | Steel digital watch with an open bracelet, lying flat face up (bracelet along Z). Evidence item on the floor by a display island; crystal sub-material `digital_wrist_watch_glass` needs the glass material. Bezel wordmark painted out, generic function labels kept. Decimated 10,390 -> 9,002. |
| **CigaretteButt** | 0.010 × 0.007 × 0.025 | 192 | Color 512, Normal 256, MS 256 | Poly Haven cigarette_case (one cigarette), author not recorded by the mirrors (*October 24*) | 25 mm butt cut from one loose cigarette of the case: cork-pattern filter plus a few millimetres of paper, the paper end flattened and bent (crushed look), the cut capped. Lying along Z. DNA evidence on the pavement by the side door or in the gutter; use with the swab and a marker. Low-poly (the filter end of the source has 16 segments). |

State pairs for the workflow: BackingCardWhite -> LiftCardWithPrint (lift pressed on), SwabTube -> SwabOpen -> SwabBox (swab taken, dried, packaged), FibreTuft -> HairFibreSpecimen (trace lifted), FootwearDustMark -> GelLifterBlack / ElectrostaticLiftFilm (mark lifted), PryMark -> MikrosilTube + MikrosilCatalyst + MixingSpatula -> ToolMarkCast (mark cast), CastingFrame + MixingBowlRubber -> ShoePrintCast (impression cast). The HingeLifter, GelLifter, card and tube models are light enough (12 to 1,436 triangles) to appear in every kit slot at once.

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
- **Source~/** holds the scripts that made or converted everything. Unity ignores it. `delabel.py` painted lettering out of four colour maps; `delabel_glove_maps.py` rebuilds WorkGloves_MetallicSmoothness.png and WorkGloves_Normal.jpg from the 2K Poly Haven maps with the glove tag removed. `make_lods.py` makes the eight LOD variants and their sources.json entries. To rebuild the procedural items, run `proc_scene.py`, `proc_kit.py`, `proc_evidence.py` and `proc_glass.py` with an output folder and `meshkit.py` alongside, then `assemble.py`. The CSI consumables batch is rebuilt by `proc_csi.py OUT` followed by `assemble_csi.py OUT <pack folder>`, which also rewrites its own sources.json entries and verifies every file (bounds, indices, map references, texture sizes). The Poly Haven batch 2 (October 24) is rebuilt from staged mirror copies (`SRC/<Name>/<Name>.gltf` plus the `.bin` and `textures/`): `relabel_ph2.py SRC` edits the colour maps in place, `build_ph2.py SRC <pack folder> Tools/glb_to_obj.py <names>` converts, decimates (UV-exact, `decimate_exact.py` or per-group) and extracts, and `write_sources_ph2.py WORK <pack folder>` adds the sources.json entries without touching the others.
