# CutoutDecals

Transparent (straight-alpha) PNG cut-outs that make floors, walls and the street look lived in.
Every pixel comes from CC0 photographs (Poly Haven, ambientCG); shapes, recolours and composites
are made by `Source~/build_cutouts.py` (seeded, reproducible). Prepared October 17, 2026.
**Not wired into any builder and never imported into Unity: unverified.** 25 PNGs, 28.3 MB.
Hashes, pixel sizes, per-file source URLs/commits, real-world size and placement: `sources.json`
(atlas cells listed under `rects` with Unity-style `uv`).

## Files

| File | px | Real size | Content / source | Use |
|---|---|---|---|---|
| Leaves/Leaves_{Green,Autumn,Dry,Wet}_Atlas.png | 2048² (4x4 cells of 512) | cell 16 cm | 14 photographed leaves (arrowhead, dock, lobed) from Poly Haven potted_plant_01/02 and weed_plant_02, black-background key; autumn/dry/wet are HSV recolours | single fallen leaves |
| Leaves/LeafCluster_DryPile.png | 1024² | 0.9 m | ~70 dry/autumn leaves with contact shadow | doorway corners, tree bases |
| Leaves/LeafCluster_AutumnScatter.png | 1024² | 1.2 m | loose mixed scatter | pavement under the street trees |
| Leaves/LeafCluster_WetGutter.png | 1024x512 | 1.6 x 0.8 m | wet dark band | kerb / gutter / drains |
| Leaves/LeafCluster_GreenFresh.png | 1024² | 0.8 m | wind-blown green leaves | under trees |
| Foliage/FoliageCrown_Atlas.png | 2048² (2x2) | ~1.8 m per card | dense crown cards: Summer_A, Summer_B, Autumn, Sparse_LateAutumn | billboards / crossed quads on the three bare trees |
| Foliage/GrassClump_Atlas.png | 2048x1024 (4 cells 512x1024) | 0.25 x 0.5 m, root bottom-centre | grass blades from Poly Haven flower_empodium: green tall/short, mixed, dry straw | tree pits, wall bases |
| Ground/WeedsInCrack_Moss.png, _Clover.png | 1024x256 | 1.2 x 0.3 m | ambientCG Ground037 / Ground079S masked to a crack + tiny weed leaves | slab joints, kerb/wall junction |
| Ground/MossPatch.png | 1024² | 0.8 m | ambientCG Ground037 moss | shaded wall bases |
| Stains/Stain_OilLarge.png | 1024² | 1.2 m | darkened asphalt photo (ambientCG Asphalt010) | parking bay, road |
| Stains/Stain_MudSplash.png | 1024² | 1.0 m | ambientCG Ground049C wet mud | tree pits, entrance |
| Stains/Stain_DustPatch.png | 1024² | 1.0 m | ambientCG Ground072, very feathered | interior corners |
| Stains/DirtEdge_WallBase.png | 1024x256 | 1.5 x 0.35 m, opaque at V=0 | ambientCG Ground053 | wall/shutter base, tile in U |
| Stains/RustStreak_Wall.png | 512x1024 | 0.3 x 0.6 m, source at top | Poly Haven can_rusted rust | under brackets, shutter guides |
| Cracks/Crack_AsphaltNetwork.png | 1024² | 1.5 m | crack network keyed from ambientCG Asphalt021 | road |
| Cracks/Crack_ConcreteHairline.png | 1024² | 1.2 m | hairline cracks from ambientCG Concrete033 | slabs, screed |
| Scuffs/RubberScuffs_Floor.png | 1024x512 | 1.0 x 0.5 m | black rubber arcs coloured from Poly Haven old_tyre (no footprints) | shop floor |
| Litter/Litter_Atlas.png | 2048² (5x5 cells of 408) | see sources.json | 3 receipts, 4 torn label/flyer scraps, 3 crumpled paper balls, 2 plastic-bag fragments, rusty lid + crushed lid, 6 cigarette butts | gutter, pavement, doorway |
| Graffiti/Graffiti_PieceBubble.png | 378x800 | ~0.9 x 1.9 m | pink/orange bubble piece + tags, keyed from Poly Haven rollershutter_door | side walls, utility boxes (not the shop front) |
| Graffiti/Graffiti_PieceSnak.png | 590x412 | ~1.6 x 1.1 m | teal "Snak" piece, keyed from rollershutter_window_01 (shutter slat shading kept) | as above |
| Graffiti/Graffiti_TagFlow.png | 230x115 | 0.5 x 0.25 m | small "FLOW" throw-up | as above |

## Derivation (Source~/build_cutouts.py)
- Leaves/grass: threshold the black atlas background (max RGB > 22/255), morphological opening
  (disk r=9) to remove UV-padding streaks, connected components, 1 px erosion and 0.8 px feather.
- Recolours: HSV hue remap toward orange/red/yellow (autumn), brown with desaturation and darker
  curled edges (dry), dark saturated brown (wet).
- Composites: seeded placement with rotation, scale and a soft contact shadow; crown cards darken
  inner leaves for self-shadowing.
- Ground/stains: photo colour under fbm masks; cracks keyed by local darkness (blur(L,6)-L);
  graffiti keyed by saturation and darkness against an 85th-percentile local background.
- Litter: paper/label/bag photos with procedural torn edges and crease shading taken from the
  trashbag photo; receipt print lines and cigarette shapes are procedural over photo colour.
- Edge bleeding: RGB of every pixel with alpha < 0.9 is replaced by the nearest opaque colour.
  Alpha is straight (not premultiplied).

## Import notes (Unity 6, Built-in)
sRGB on, Alpha Is Transparency on, mipmaps on, Wrap Clamp, Max Size 2048. Leaves/litter/grass/
foliage: Standard Cutout 0.4-0.5 (double-sided quads for foliage and grass). Stains, dirt, moss,
cracks, scuffs, graffiti: Fade/transparent decal quads 2-3 mm off the surface, shadows off.
Graffiti PNGs are non-power-of-two and not multiples of 4: set Non-Power of 2 = ToNearest or
leave uncompressed.

## Gaps
No cobwebs, no stickers or torn posters (WallGraphics already has Sticker_*, StreetPosters_Weathered,
Graffiti_Tags), no bottle crown caps (only a rusty can lid), no separate normal or smoothness maps,
no photographed puddles (SurfaceDecals has procedural ones). Receipt print and cigarette butt shapes
are procedural over photo colour rather than photographed objects. Crown cards use houseplant/weed
leaves, not a street-tree species.
