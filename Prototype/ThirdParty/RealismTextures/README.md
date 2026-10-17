# RealismTextures: material maps for AmbientProps, surface sets and missing grime decals

Made for Unity 6000.6.2f1, Built-in pipeline, Standard shader, no packages. Prepared 2026-10-17. **Not imported or checked in Unity yet (unverified); nothing is wired.** Derivation: `Source~/build_realism.py` (seeded, deterministic). Per-file source, licence and SHA-256: `sources.json`. Licences: `LICENSE.txt` (CC0 except the six derived PropMaps, which are CC BY 4.0).

## Import settings

| Suffix | Texture type | sRGB | Notes |
|---|---|---|---|
| `_Color`, `_Neutral`, decal PNGs | Default | on | Decals: Alpha Is Transparency on; straight alpha with edge bleed |
| `_Normal` | Normal map | off | OpenGL convention (+Y), Unity's default; do not flip G |
| `_MetallicSmoothness` | Default | off | Standard `_MetallicGlossMap`: R metallic, A smoothness (Source: Metallic Alpha) |
| `_Occlusion` | Default | off | Standard `_OcclusionMap` (G used) |
| `_PaintMask` | Default | off | White = paint, black = bare metal |

## PropMaps: maps for the colour-only AmbientProps models (1024 px, same UVs)

| Prop (AmbientProps) | Maps | Quality |
|---|---|---|
| PlasticCrate, PlasticThermos, UtilityBoxSmall, SprayPaintBottles (+_Dirty), ElectricCables (+_Boxes), IndustrialPipes_Group01/02, RustedCan, PowerBox, WetFloorSign | Normal, MetallicSmoothness, Occlusion | **Original Poly Haven scans** for exactly those models; UV match with the AmbientProps colour PNG verified (correlation 1.0) |
| CardboardBoxSet, ACCondenserSet, IndoorACUnit, NetworkServerRackRow, LightSwitch, PowerOutlet | Normal, MetallicSmoothness, Occlusion | **Approximation** derived from the colour map (high-pass luminance relief, constant base smoothness per prop; NetworkServerRackRow metallic 0.55, others 0). CC BY 4.0, attribution in LICENSE.txt |

Assign as `<Prop>_Normal` -> \_BumpMap, `<Prop>_MetallicSmoothness` -> \_MetallicGlossMap, `<Prop>_Occlusion` -> \_OcclusionMap on the material that uses `<Prop>_*.png` from AmbientProps. Quaternius WheelieBin/Dumpster and AlarmBeacon have constant MTL colours and no UV texture, so use the tileable sets below on them (PaintedMetalChipped / PlasticTextured / RustCoarse).

## Surfaces: tileable sets (1024 px; CardboardPanel 256 x 512)

| Set | Maps | Source | Use |
|---|---|---|---|
| PaintedMetalChipped | Color (red paint), Neutral (grey, tint in material), PaintMask, Normal, MetallicSmoothness, Occlusion | ambientCG PaintedMetal004 (CC0) | Dumpster, shutters, bins, railings, AC casings; tint Neutral for any paint colour |
| PlasticTextured | Color, Normal, MetallicSmoothness, Occlusion | ambientCG Rubber004 (CC0) | Fine-stippled matte plastic/rubber: wheelie bin, crates, cable trunking (tint Color) |
| RustCoarse | Color, Normal, MetallicSmoothness, Occlusion | Poly Haven rust_coarse_01 (CC0) | Heavy rust; complements PBRExterior/RustyMetal (Metal053C), not a duplicate |
| CardboardPanel | Color, Normal, MetallicSmoothness, Occlusion | Poly Haven cardboard_box_01 (CC0), one undamaged side panel, made tileable | Cardboard boxes/flattened cartons on primitives; tile 0.5 x 1 m |

## Decals (straight alpha, edge bleed)

| File | Size | Content | Source |
|---|---|---|---|
| PeelingPaint_Chips01/02 (+_Normal) | 1024 | Bare substrate showing through chipped paint, soft radial falloff | Chip shapes from ambientCG PaintedMetal004 (CC0); colour procedural |
| Cobweb_Corner01/02 | 1024 | Orb web anchored at texel (0,0) = top-left; rotate/mirror the quad for other corners | Procedural (not photographic) |
| Cobweb_Hanging01 | 512 x 1024 | Sagging strands and dangling dust beads, top edge anchored | Procedural |
| RainOnGlass (+_Normal, _MetallicSmoothness) | 1024 | Mist droplets, beads and run-down trails for the shop window (alpha 0-0.35; the normal does the work) | Procedural |
| DrinkSpill_Cola/Coffee/Beer/SodaDriedRing (+_MetallicSmoothness) | 512 | Liquid spills with splash droplets; dried ring = sticky residue | Procedural |
| BottleCaps_Atlas (+_MetallicSmoothness) | 1024, 4x4 | Unbranded crown caps: 8 flat, 4 bent, 4 rusty, with contact shadow | Procedural |
| PigeonDroppings_Atlas (+_Normal) | 1024, 4x4 | Bird droppings for ledges, kerbs, bench tops | Procedural |

## Not included (already present elsewhere; do not duplicate)

Puddles with reflection masks (SurfaceDecals/Puddle_*, PBRExterior/PuddleMask), chewing gum (SurfaceDecals/ChewingGum_Scatter), glass smudges and fingerprints (PBRInterior/GlassSmudge, SurfaceDecals/Fingerprints_Atlas, GlassHandprint_Latent), dust (SurfaceDecals/DustFilm_MissingItems, CutoutDecals/Stain_DustPatch), ceiling water stains (SurfaceDecals/WaterStain_Ceiling, PBRExterior/LeakStain), street posters (WallGraphics/Street/StreetPosters_Weathered).

## Gaps (nothing CC0 reachable from this environment)

- Photographic cobwebs: none found in reachable CC0 mirrors; the procedural webs stand in.
- Real fallen street-tree leaves (plane, maple, oak): none; CutoutDecals/Leaves (Poly Haven potted/weed plant leaves) remain the only photo leaves.
- Real-photo window backplates (lit/unlit apartments), shopfront interiors and night street posters: none. Poly Haven interior HDRIs are not mirrored in any repository found; ambientCG/Poly Haven sites and Wikimedia are not reachable.

Total size: 58498837 bytes (about 55 MB).
