# PBRExterior: exterior materials for the night store

Tileable PBR sets for the street, the pavement, the kerb and the shop front, plus the buildings across the street. They are made for Unity 6000.6.2f1 with the Built-in pipeline and the Standard shader in the metallic workflow. Prepared October 14, 2026. **Not imported or checked in Unity yet.** Total size is about 29.5 MB (28.2 MiB) including this README, and no file is over 2.4 MB.

Sources: Poly Haven and ambientCG (CC0), plus two original procedural sets. See LICENSE.txt and sources.json, which list every hash, commit and change.

## File convention

| Suffix | Content | Unity import |
|---|---|---|
| `_Color.jpg` | Base colour | sRGB on, Repeat, mipmaps on |
| `_Normal.png` | Tangent-space normal, OpenGL +Y | Texture Type **Normal map**, Repeat |
| `_MetallicSmoothness.png` | R = metallic, G = B = 0, A = 1 - roughness | **sRGB off**, Repeat; Standard: Metallic slot, Source = Metallic Alpha, Smoothness (GlossMapScale) = 1 |
| `_AO.jpg` | Ambient occlusion, white = open | **sRGB off**, Occlusion slot, strength 0.6-1 |

All textures are power-of-two and seamless; tiling was checked on 2x2 mosaics. In Standard, the smoothness multiplier can only lower smoothness, so the wet looks come as separate sets.

Note: the existing StoreKit loader looks for `<texture>_Color.png`. The colour maps in this pack are **.jpg**, so pass the extension when this pack is wired in.

## Materials

| Set | Files (resolution) | Real tile size (UV repeat) | Source | Suggested use in the night store | Starting settings |
|---|---|---|---|---|---|
| **AsphaltWet** | Color 2048, Normal 1024, MetallicSmoothness 1024, AO 512 | 3.0 x 3.0 m (Poly Haven) | Poly Haven asphalt_02, wet variant derived here | The street and carriageway in front of the shop. Water lies in cracks and aggregate pits (smoothness ~0.96). The surface has a thin film (smoothness ~0.7) that catches the streetlight and the police light-bar reflections. | Bump 1.0, reflection probe on the street. Mean smoothness 0.72. |
| **AsphaltDry** | same as Wet | 3.0 x 3.0 m | Poly Haven asphalt_02 (Rob Tuytel), cracked grey asphalt, photoscanned | Sheltered or dry areas: under the parked police car, the far road, or a drying edge. It matches AsphaltWet pixel for pixel, so the two can be blended or swapped without a seam. | Mean smoothness 0.23 |
| **PavementSlab** | Color 2048, Normal 1024, MetallicSmoothness 1024, AO 1024 | 2.0 x 2.0 m (4 x 4 slabs of 0.5 m, estimated) | ambientCG PavingStones136 (desaturated to grey) | The pavement between the kerb and the shop front. Align the UV origin to the facade line so a joint runs along the shop front. | Bump 1.0; smoothness 0.50 dry |
| **PavementSlabWet** | Color 2048, MetallicSmoothness 1024 (**reuse PavementSlab_Normal.png and PavementSlab_AO.jpg**) | 2.0 x 2.0 m | derived from PavingStones136 | The same pavement after rain. Joints hold water (smoothness 0.92), slabs are damp (~0.64). | Use this or PavementSlab, not both |
| **Concrete** | Color 2048x1024, Normal 1024x512, MetallicSmoothness 1024x512 | 2.0 x 1.0 m (estimated) | ambientCG Concrete034 (albedo lowered 20 %) | Cast-concrete kerb, bollards, the step under the doors, the base plinth of the shop front, and lamp-post footings | Bump **0.5-0.7**, because the source relief is strong for a kerb |
| **FacadeStone** | Color 2048, Normal 1024, MetallicSmoothness 1024 | 1.2 x 1.2 m (ambientCG page) | ambientCG Travertine001 (vein-cut travertine; finish re-authored to polished-and-worn) | Storefront piers, the fascia band and the stall riser under the windows. Run the veins horizontally. | Bump 0.3-0.5; smoothness ~0.88 (lower GlossMapScale to ~0.6 for honed stone) |
| **BrickFacade** | Color 2048, Normal 1024, AO 1024, MetallicSmoothness 512 | 3.0 x 3.0 m (Poly Haven; 39 courses = 77 mm each) | Poly Haven brick_wall_006 (Jan Burghardt), worn red brick, photoscanned | Buildings across the street and the side or party walls. Brick scale reads correctly at 1 tile per 3 m. | Bump 1.0; AO 0.8 |
| **Render** | Color 1024, Normal 512, MetallicSmoothness 512, AO 512 | 2.0 x 2.0 m (Poly Haven) | Poly Haven plastered_wall (Amal Kumar), weathered warm-grey plaster | Upper floors and painted stucco on the facades across the street. Tint it with the material colour (cream, pale grey, off-white) to vary buildings. | Bump 0.6-1.0 |
| **Granite** | Color 1024, Normal 1024, MetallicSmoothness 512 | 0.6 x 0.6 m (estimated, 6-12 mm grains) | ambientCG Granite002A (light grey salt-and-pepper granite; re-authored as a flamed finish) | Granite kerb stones, threshold step, bollard caps and window sills. Sparse glint pixels sparkle under the torch. | Bump 0.6-1.0; smoothness ~0.40 |
| **RustyMetal** | Color 1024, Normal 1024, MetallicSmoothness 512 | 1.0 x 1.0 m (estimated) | ambientCG Metal053C (bare steel with rust; metallic mask included) | Drain grates, the dumpster, the back-alley door, old signage brackets, the lamp-post base, and rust bleed on the shutter housing | Bump 1.0 |
| **MetalShutter** | Color 1024, Normal 1024, MetallicSmoothness 512, AO 512 | 1.0 x 1.0 m, 13 horizontal slats of 77 mm | Original procedural work (project-owned) | Galvanised roller shutters on the closed neighbouring shops and the side rooms' street doors, and half-lowered over the forced storefront for drama. Slats run along U. | Metallic 0.9 clean / 0.15 grime; smoothness ~0.5 |
| **PuddleMask** | PuddleMask.png, 1024 grayscale (L) | **8 x 8 m** suggested | Original procedural work (project-owned) | Puddles on the wet street, or the pavement edge by the kerb. 1 = standing water (~14 %), 0.45 to 0 = wet rim fading over 0.2-0.6 m, 0 = base surface. | See wiring notes |
| **LeakStain** | LeakStain_ColorAlpha.png, 256x1024 RGBA (A = opacity) | 0.5 x 2.0 m decal | ambientCG Leaking006 | Rust and water run-off under window sills, the AC unit or the shutter box on the facades. Use a quad with Standard **Fade** mode. | Smoothness 0.2, no normal |

## Wiring notes (for whoever integrates it)

- **UV scale.** Ground generators use metric UVs, so tiling = 1 / tile size: asphalt 0.333, slabs 0.5, travertine 0.833, brick 0.333, render 0.5, granite 1.667, shutter 1.0, puddle 0.125.
- **Puddles in Built-in Standard (no custom shader).** Either:
  - lay a large quad 3-5 mm above the road with Standard **Fade** mode, base colour near black (0.02, 0.022, 0.025), smoothness 0.97, alpha from PuddleMask (copy the mask into the alpha of a small black texture), tiling 0.125 and a random offset; or
  - make a 2-layer road shader that lerps AsphaltDry or AsphaltWet toward water using the mask.

  The reflections need a reflection probe (box projection) over the street. Planar or SSR reflections are not available in Built-in without extra code.
- **Asphalt pair.** AsphaltDry and AsphaltWet share UVs and features. A vertex-colour or mask lerp between them gives drying patches under awnings or the car.
- **Visible repeat.** asphalt_02 has one long crack running along V, so it repeats every 3 m. Run the cracks along the street (V along the kerb) so they read as a longitudinal joint, or split the road into two strips with different UV offsets. The 8 m PuddleMask overlay also breaks up the repeat.
- **Source type.** The three Poly Haven sets (asphalt, brick, plaster) are photoscans. Travertine001 (FacadeStone) is an ambientCG procedural material, not a scan, according to the mirror's manifest. It still reads well at pier scale. The creation method of the other ambientCG sets was not checked.
- **Performance.** Use the default DXT/BC compression (BC5 for normals, which Unity picks automatically for Normal map type), keep mipmaps on, and use anisotropic filtering 4-8 on the ground sets (asphalt and slabs) because they are viewed at grazing angles in VR. Max Size 2048 for the hero colour maps, 1024 for the rest.
- **Names.** NightMaterials already has a `FacadeStone_*` set (dark marble, PNG). These live in a different folder, so the names do not collide, but `DemoAssetLibrary.Texture(pack, name)` must be called with `"PBRExterior"`.

## Rebuilding

`Source~/build_pbr_exterior.py <raw_dir> <out_dir>` regenerates every file from the untouched source maps listed in sources.json. It needs numpy and Pillow only and is deterministic. Unity ignores folders whose names end in `~`.
