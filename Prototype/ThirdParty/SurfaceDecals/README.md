# SurfaceDecals

Decal and effect textures for the night jewellery-store burglary scene: shoe prints, fingerprints,
broken glass, cracks, puddles, dirt and stains, scuffs and tool marks, tyre marks, street litter,
effect sprites, a caustics tile and spot-light cookies. Prepared October 14, 2026.
**Nothing here has been imported or checked in Unity yet (unverified).**

Almost everything is original procedural work made for this project (Python generators in
`Source~/`, which Unity ignores). Two groups come from CC0 sources: `SmokePuff_Kenney*.png`
(Kenney Particle Pack) and `LeafLitter_Wet*` (built from the photoscanned ambientCG LeafSet024).
See LICENSE.txt; per-file hashes, sizes and sources are in sources.json.

Blood is deliberately absent. Fingerprints are synthetic (generated ridge flow, not any real
person's print). Shoe soles and the tyre tread are generic designs, not any brand's pattern.
No text, logos or trademarks appear anywhere.

## Conventions

- **Color decals** (`<Name>.png`): sRGB RGBA, straight alpha. Fully transparent pixels carry a constant
  fill colour so mipmaps do not get dark fringes. Import: sRGB on, Alpha Source = Input Texture
  Alpha, **Alpha Is Transparency on**, Wrap = Clamp, mipmaps on, Aniso 4-8 for floor decals,
  Max Size = native size (2048 for GlassShards_Scatter, else 1024/512).
- **`_Normal.png`**: OpenGL / Unity convention (+Y = green up), flat = (128,128,255). Import as
  Texture Type = Normal map (sRGB off is implied). Strength 1.0 is calibrated; 0.5-1.5 is fine.
- **`_MetallicSmoothness.png`**: Unity Standard metallic map, R = metallic, A = smoothness. Import with
  sRGB off; set the material's Smoothness Source = Metallic Alpha, Smoothness scale 1.
  Shipped for the puddles, wet shoe prints, oil stain, pry marks and wet leaves. Other decals: use a
  constant smoothness (values below).
- **Cookies** (`Cookie_*.png`): grayscale, white light on black, outer 3 px exactly black. Import as
  Texture Type = Cookie, Light Type = Spotlight, Alpha Source = From Gray Scale, Wrap = Clamp.
- **Caustics_Tile.png**: grayscale, seamless (Wrap = Repeat).
- **Orientation**: image top = +V. "Toe = +V" means the toe points to the top of the image.
- **Physical size**: the "Real size" column is the quad size in metres. Keep the quad aspect equal to
  the image aspect unless noted. The prints are real scale (sneaker sole 290 mm, boot sole 305 mm).

## Using them in the Built-in pipeline (no packages)

Built-in has no projector-decal system for PBR, so use thin quads:

1. A quad lying 1-3 mm above the surface (floor decals) or 0.5-1 mm in front of it (walls, glass,
   door frame), no collider, Cast Shadows off, Receive Shadows on.
2. Material: Standard, Rendering Mode **Fade** for dirt, prints, stains, glass scatter, litter and
   puddles (fades both diffuse and specular with alpha, so the empty part of the quad stays invisible).
   Use **Cutout** (cutoff 0.35) for the leaf litter and the litter scatter if sorting artefacts appear.
   Render queue 2450-2500 so decals draw before other transparents; vary by +1 to order overlaps.
3. Smoothness when no `_MetallicSmoothness` is shipped: mud 0.45, dust 0.15, glass 0.9, cracks 0.2,
   gum 0.3, scuffs 0.35, grime 0.15, water stains 0.2, drag marks 0.5, tyre skid 0.35, tyre tread 0.4,
   litter 0.4, fingerprints 0.5 (on glass), crack seal 0.6.
4. On the transparent glass (display cases, storefront), put the fingerprint quads in front of the
   glass with render queue one above the glass material.
5. Effects: use Legacy Shaders/Particles/Additive (Soft) or Alpha Blended for DustMotes, FogPuff,
   SmokePuff, LightShaft and LensGlow; the RGB is the tint and the alpha is the intensity. The flare
   textures can build a Unity **Flare** asset (Built-in Flare Layer on the camera) for the streetlights
   and police light bar.
6. Performance (PC VR): all textures compress to DXT5/BC7 (colour) and BC5 (normals). Share one
   material per decal type; GPU instancing on. Put fine-detail decals (fingerprints, pry marks, gum)
   on a LOD/visibility toggle beyond 6-8 m.

### Shoe-print walk sequences

Stride (heel to heel of the same foot) 1.3-1.5 m, so consecutive L/R prints are 0.65-0.75 m apart along
the walk line, 0.08-0.12 m either side of it, toes turned out 5-10 degrees. Use the L/R files in turn
(never the same file twice in a row; for longer trails mirror U of an L print to get an extra R). Scale
alpha down 10-15 % per step for mud as the sole runs out of mud. Two suspects are provided: sneaker
and lug boot. Each file has its own cuts, stone holes and wear patches, which is what a forensic
examiner compares, so the "same" print in two places should use the same file.

### Fingerprint atlas UVs

`Fingerprints_Atlas.png` is 4 x 2 cells. Material tiling (0.25, 0.5); offset x = column * 0.25;
offset y = 0.5 for the top row (latent) and 0 for the bottom row (developed with black powder).
A 25 mm quad per print. Swap from the latent to the developed cell when the player dusts: both cells
of a column hold the same print in the same place, so only the offset y changes and the print does
not move or change shape (the developed cell adds a faint powder haze around it).

### Light cookies

Set the spot angle as listed, then tune range/intensity. Flashlight: LED cookie, 45 degrees,
neutral white 5500-6000 K. UV torch: UV cookie, light colour about (0.45, 0.2, 1.0), low intensity,
used with emissive fluorescent evidence. Scene lamp: SceneFlood cookie, 80 degrees. Display spots:
GallerySpot cookie, 25 degrees. Cookies cost little in Built-in forward rendering, but each
shadowed spot light adds a pass; keep shadows on the player flashlight only.

### Caustics

`Caustics_Tile` can be the cookie of a downward or wall-facing spot light (Wrap Repeat, scroll the
light's local position slowly) or the albedo of an additive projector-style quad, to fake puddle
reflections dancing on the kerb and shop front under the streetlights. Two copies scrolled in
different directions and multiplied give the usual animated look.

## Files

### Shoe prints

| File(s) | Pixels | Real size | What it is | Suggested placement |
|---|---|---|---|---|
| `ShoePrint_Sneaker_Mud_L.png` + `ShoePrint_Sneaker_Mud_L_Normal.png` | 512x1024 | 0.20 x 0.40 m quad (toe = +V) | Wet mud print, left foot, generic court/casual sneaker, 290 mm sole (EU 43 / UK 9): rim with siping, herringbone, pivot rings, flex grooves. Wear patches plus unique cuts / stone holes (identifying marks); crumbs around, toe-off smear. | Pavement just outside the forced door and the first 2-3 m of shop floor; walk sequence fades inward |
| `ShoePrint_Sneaker_Dust_L.png` + `ShoePrint_Sneaker_Dust_L_Normal.png` | 512x1024 | 0.20 x 0.40 m quad (toe = +V) | Dry dust / glass-powder impression, left foot, generic court/casual sneaker, 290 mm sole (EU 43 / UK 9): rim with siping, herringbone, pivot rings, flex grooves. Speckled partial transfer, light grey. | Dark polished floor around the smashed cases and behind the counter; shows best under the flashlight at a grazing angle |
| `ShoePrint_Sneaker_Mud_R.png` + `ShoePrint_Sneaker_Mud_R_Normal.png` | 512x1024 | 0.20 x 0.40 m quad (toe = +V) | Wet mud print, right foot, generic court/casual sneaker, 290 mm sole (EU 43 / UK 9): rim with siping, herringbone, pivot rings, flex grooves. Wear patches plus unique cuts / stone holes (identifying marks); crumbs around, toe-off smear. | Pavement just outside the forced door and the first 2-3 m of shop floor; walk sequence fades inward |
| `ShoePrint_Sneaker_Dust_R.png` + `ShoePrint_Sneaker_Dust_R_Normal.png` | 512x1024 | 0.20 x 0.40 m quad (toe = +V) | Dry dust / glass-powder impression, right foot, generic court/casual sneaker, 290 mm sole (EU 43 / UK 9): rim with siping, herringbone, pivot rings, flex grooves. Speckled partial transfer, light grey. | Dark polished floor around the smashed cases and behind the counter; shows best under the flashlight at a grazing angle |
| `ShoePrint_Boot_Mud_L.png` + `ShoePrint_Boot_Mud_L_Normal.png` | 512x1024 | 0.20 x 0.40 m quad (toe = +V) | Wet mud print, left foot, generic lug-sole work boot, 305 mm sole: chevron lugs, perimeter lugs, separate heel block, no shank contact. Wear patches plus unique cuts / stone holes (identifying marks); crumbs around, toe-off smear. | Pavement just outside the forced door and the first 2-3 m of shop floor; walk sequence fades inward |
| `ShoePrint_Boot_Dust_L.png` + `ShoePrint_Boot_Dust_L_Normal.png` | 512x1024 | 0.20 x 0.40 m quad (toe = +V) | Dry dust / glass-powder impression, left foot, generic lug-sole work boot, 305 mm sole: chevron lugs, perimeter lugs, separate heel block, no shank contact. Speckled partial transfer, light grey. | Dark polished floor around the smashed cases and behind the counter; shows best under the flashlight at a grazing angle |
| `ShoePrint_Boot_Mud_R.png` + `ShoePrint_Boot_Mud_R_Normal.png` | 512x1024 | 0.20 x 0.40 m quad (toe = +V) | Wet mud print, right foot, generic lug-sole work boot, 305 mm sole: chevron lugs, perimeter lugs, separate heel block, no shank contact. Wear patches plus unique cuts / stone holes (identifying marks); crumbs around, toe-off smear. | Pavement just outside the forced door and the first 2-3 m of shop floor; walk sequence fades inward |
| `ShoePrint_Boot_Dust_R.png` + `ShoePrint_Boot_Dust_R_Normal.png` | 512x1024 | 0.20 x 0.40 m quad (toe = +V) | Dry dust / glass-powder impression, right foot, generic lug-sole work boot, 305 mm sole: chevron lugs, perimeter lugs, separate heel block, no shank contact. Speckled partial transfer, light grey. | Dark polished floor around the smashed cases and behind the counter; shows best under the flashlight at a grazing angle |
| `ShoePrint_Sneaker_Wet_L.png` + `ShoePrint_Sneaker_Wet_L_MetallicSmoothness.png` + `ShoePrint_Sneaker_Wet_L_Normal.png` | 512x1024 | 0.20 x 0.40 m quad (toe = +V) | Water-film print from the wet street, left sneaker, partly evaporated, beaded water and grit. Ships with _MetallicSmoothness (film 0.9, else 0.25). | Polished floor inside the entrance (drying wet prints) |
| `ShoePrint_Sneaker_Wet_R.png` + `ShoePrint_Sneaker_Wet_R_MetallicSmoothness.png` + `ShoePrint_Sneaker_Wet_R_Normal.png` | 512x1024 | 0.20 x 0.40 m quad (toe = +V) | Water-film print from the wet street, right sneaker, partly evaporated, beaded water and grit. Ships with _MetallicSmoothness (film 0.9, else 0.25). | Polished floor inside the entrance (drying wet prints) |

### Fingerprints

| File(s) | Pixels | Real size | What it is | Suggested placement |
|---|---|---|---|---|
| `Fingerprints_Atlas.png` | 1024x512 | each cell 25 x 25 mm (atlas 4 x 2 cells of 256 px) | Synthetic fingerprints (no real person): loop R, whorl, loop L, arch. Top row latent grease (whitish, alpha <= 0.36), bottom row the same print after black powder dusting: identical ridges, outline and position in the cell (alpha correlation 0.95 per cell), plus powder grain, brush haze and stray specks. Regenerated October 15, 2026. | Display-case glass, storefront door glass and the safe door; swap latent -> developed when the player dusts |
| `GlassHandprint_Latent.png` | 1024x1024 | 0.19 x 0.19 m | Latent left-hand print on glass (four fingertips with ridges, finger segments, palm pads, creases, slight lift smear); thumb on the right = seen from the side the person stood. Mirror U for a right hand. | Top of a smashed display case or inside of the storefront glass (subtle) |

### Broken glass

| File(s) | Pixels | Real size | What it is | Suggested placement |
|---|---|---|---|---|
| `GlassShards_Scatter.png` + `GlassShards_Scatter_Normal.png` | 2048x2048 | 1.2 x 1.2 m | ~2500 annealed-glass shards (2-110 mm slivers and wedges, larger near the impact point at UV 0.50, 0.58) plus glass dust; faces faint, fractured edges bright green-white; per-shard tilt in the normal map for glints. | Floor around and under the smashed central cases (one decal per case, rotated) |
| `GlassCrumbs_Tempered.png` + `GlassCrumbs_Tempered_Normal.png` | 1024x1024 | 0.8 x 0.8 m | ~4100 tempered (safety) glass crumbs, 4-11 mm cubes, dense pile thinning outward, glints in the normal map. | Inside and outside the forced storefront door / broken glass bays |

### Cracks

| File(s) | Pixels | Real size | What it is | Suggested placement |
|---|---|---|---|---|
| `ConcreteCrack_Long.png` + `ConcreteCrack_Long_Normal.png` | 2048x512 | 2.0 x 0.5 m | Long branching crack in concrete: jagged micro path, tapering ends, spalled edge chips, dirt/moisture halo. | Pavement slabs, kerb, rear-office screed floor |
| `ConcreteCrack_Corner.png` + `ConcreteCrack_Corner_Normal.png` | 1024x1024 | 1.0 x 1.0 m | Radial crack network from a corner impact with two concentric connecting cracks. | Corner of a pavement slab or kerb stone (wheel strike), or the floor where the safe was dropped |
| `AsphaltCrackSeal.png` + `AsphaltCrackSeal_Normal.png` | 512x2048 | 0.5 x 2.0 m (long axis = V) | Bitumen crack-seal overband (~50 mm) with embedded sand and tyre-polished middle; slightly raised. | Carriageway; run it roughly along or across the lane |

### Puddles

| File(s) | Pixels | Real size | What it is | Suggested placement |
|---|---|---|---|---|
| `Puddle_Kerbside.png` + `Puddle_Kerbside_MetallicSmoothness.png` + `Puddle_Kerbside_Normal.png` | 1024x512 | 2.0 x 1.0 m (top edge = kerb face) | Gutter puddle lying against the kerb; water level follows a height field so edges follow contours; damp rim and silt line. With _Normal (ripples/meniscus) and _MetallicSmoothness (water 0.97, damp ~0.6). | Gutter along the kerb in front of the shop / police car |
| `Puddle_Round.png` + `Puddle_Round_MetallicSmoothness.png` + `Puddle_Round_Normal.png` | 1024x1024 | 1.2 x 1.2 m | Single rounded puddle with damp rim and silt line; _Normal and _MetallicSmoothness. | Road surface, pavement dips |
| `Puddle_Cluster.png` + `Puddle_Cluster_MetallicSmoothness.png` + `Puddle_Cluster_Normal.png` | 1024x1024 | 1.5 x 1.5 m | Cluster of small puddles in surface dips; _Normal and _MetallicSmoothness. | Uneven pavement slabs, road near drains |

### Dirt and stains

| File(s) | Pixels | Real size | What it is | Suggested placement |
|---|---|---|---|---|
| `GrimeStreaks_Wall.png` | 512x1024 | 0.6 x 1.2 m (top = ledge) | Soot/dirt run-off streaks under a ledge, dark top band, drip ends, faint rust tint. | Facades across the street, under sills, shutter box, fascia |
| `DirtTracked_Entrance.png` | 1024x1024 | 1.5 x 1.5 m (bottom edge = door threshold) | Wet street dirt tracked in: two faint fading walking tracks (boot + sneaker, 5 steps each) heading into the shop, grit, threshold haze. | Shop floor directly inside the forced door |
| `DirtEdge_WallBase.png` | 1024x256 | 2.0 x 0.25 m (top edge = wall) | Dust, grit and fluff collected along a wall base or kerb face. | Skirting of side rooms/office, kerb face, shop-front plinth |
| `WaterStain_Ceiling.png` | 1024x1024 | 0.9 x 0.9 m | Brown water stain with 4 tide lines (sharp outer edge, soft inner side). | Ceiling of the rear office or a closed side room |
| `WaterStain_FloorDried.png` | 1024x1024 | 1.0 x 1.0 m | Whitish mineral tide marks left by a dried puddle. | Stone floor near the entrance / under the broken glass bay |
| `OilStain.png` + `OilStain_MetallicSmoothness.png` | 1024x1024 | 1.0 x 1.0 m | Engine oil soaked into asphalt/concrete with feathered edge and separate drips; _MetallicSmoothness (core 0.8). | Parking bay, under the police car engine, kerbside |
| `DustFilm_MissingItems.png` | 1024x1024 | 0.6 x 0.6 m (top edge = back of the shelf) | Even dust film on a shelf/case floor with crisp clean outlines where items were lifted (2 ring boxes, a bust base, a watch tray, 3 ring stands), dust rims, fibres and two four-finger glove drag marks. | Floor of an emptied wall case or display shelf (the "missing items" evidence) |

### Street litter

| File(s) | Pixels | Real size | What it is | Suggested placement |
|---|---|---|---|---|
| `ChewingGum_Scatter.png` + `ChewingGum_Scatter_Normal.png` | 1024x1024 | 1.0 x 1.0 m | 34 flattened gum spots (fresh light grey to blackened); _Normal. | Pavement near the shop door and bus stop |
| `LitterScatter.png` + `LitterScatter_Normal.png` | 1024x1024 | 1.0 x 1.0 m | 16 cigarette butts, 5 receipt scraps (unreadable print lines), 4 generic crown caps, 3 burnt matches, foil bits, grit; _Normal. | Pavement, gutter, around the bench and bins |
| `LeafLitter_Wet.png` + `LeafLitter_Wet_MetallicSmoothness.png` + `LeafLitter_Wet_Normal.png` | 1024x1024 | 1.0 x 1.0 m | 58 photoscanned beech leaves (62-98 mm) recoloured to wet autumn browns/ochres; _Normal (rotated with each leaf) and _MetallicSmoothness (wet 0.55-0.9). | Pavement, gutter, kerb line, near street trees |

### Scratches and scuffs

| File(s) | Pixels | Real size | What it is | Suggested placement |
|---|---|---|---|---|
| `FloorScuffs_Heel.png` | 1024x1024 | 1.0 x 1.0 m | 30 black rubber heel marks and sole scuffs: striated smears with a dense start and fading tail. | Pale stone floor in aisles, at the counter, office doorway |
| `FloorScratches_Fine.png` | 1024x1024 | 1.0 x 1.0 m | 420 fine wear scratches with a dominant direction, intermittent; light grey. Use mainly to break smoothness. | Polished floor walkways, glass case tops, counter top |
| `DragMarks_Floor.png` + `DragMarks_Floor_Normal.png` | 512x2048 | 0.6 x 2.4 m (drag along V) | Heavy object dragged: two parallel bands (0.42 m apart) of scratches and gouges, ploughed grit; _Normal. | Rear office floor from the safe, or under a pulled-out cabinet |
| `PryMarks_DoorFrame.png` + `PryMarks_DoorFrame_MetallicSmoothness.png` + `PryMarks_DoorFrame_Normal.png` | 512x512 | 0.12 x 0.12 m (right edge = frame edge at the door gap) | Three flat-blade pry dents (21-25 mm wide) with exposed brushed metal, raised burrs, paint flakes and slip scratches; _Normal and _MetallicSmoothness (metallic on bare metal). | Storefront door frame beside the lock, rear/office door frame (tool-mark evidence) |

### Tyre marks

| File(s) | Pixels | Real size | What it is | Suggested placement |
|---|---|---|---|---|
| `TyreSkid.png` | 512x2048 | 0.3 x 2.4 m (travel = +V) | Braking skid of one 205 mm tyre: faint start, darkening, heavy stop deposit, 4 groove lines and striations. | Carriageway in front of the shop (getaway vehicle braking/accelerating); place two 1.5 m apart |
| `TyreTread_Mud.png` + `TyreTread_Mud_Normal.png` | 512x2048 | 0.3 x 2.4 m (mud fades along +V) | Muddy tread print of a generic car tyre (4 grooves, shoulder blocks, sipes) with clumps; _Normal. | Road / kerb where a car drove off the verge or through mud |

### Effect sprites

| File(s) | Pixels | Real size | What it is | Suggested placement |
|---|---|---|---|---|
| `DustMotes_Atlas.png` | 512x512 | particle size 2-6 mm (2 x 2 frames) | Frames: soft mote (TL), defocused 7-blade bokeh disc (TR), fibre (BL), speck cluster (BR). White, alpha. | Particle system inside the flashlight cone / near the scene lamps |
| `LightShaft_Gradient.png` | 512x1024 | card ~0.3-0.6 m wide x 2-6 m long (source = top) | Soft volumetric beam card: widening, exponential fade, faint dust striations. | Beam cards from streetlights through the glass bays, police light-bar wash, torch beam haze |
| `FogPuff_Atlas.png` | 1024x1024 | puff 1-3 m (2 x 2 frames) | Four soft fog/steam puffs (domain-warped noise), white with alpha. | Low mist over the wet street, steam from a drain grate |
| `SmokePuff_Kenney01.png` | 512x512 | puff 0.5-2 m | Kenney Particle Pack smoke_01 (soft smoke puff), converted to RGBA. | Alternative smoke/fog/dust-cloud particles (glass dust when a case is struck) |
| `SmokePuff_Kenney04.png` | 512x512 | puff 0.5-2 m | Kenney Particle Pack smoke_04 (soft smoke puff), converted to RGBA. | Alternative smoke/fog/dust-cloud particles (glass dust when a case is struck) |
| `SmokePuff_Kenney07.png` | 512x512 | puff 0.5-2 m | Kenney Particle Pack smoke_07 (soft smoke puff), converted to RGBA. | Alternative smoke/fog/dust-cloud particles (glass dust when a case is struck) |
| `SmokePuff_Kenney08.png` | 512x512 | puff 0.5-2 m | Kenney Particle Pack smoke_08 (soft smoke puff), converted to RGBA. | Alternative smoke/fog/dust-cloud particles (glass dust when a case is struck) |
| `LensGlow.png` | 512x512 | screen-space / billboard | Point-light glow: core, double halo, faint chromatic ring. RGB tint + alpha. | Streetlight lamps, police light-bar LEDs, torch lens seen head-on |
| `LensFlare_Streak.png` | 1024x256 | billboard | Anamorphic horizontal streak, cool blue-white. | Police light bar and headlights (Unity Flare asset element or billboard) |
| `LensFlare_Ghosts.png` | 1024x256 | 4 frames of 256 px | Flare ghosts: hexagon aperture ghost, chromatic ring, warm soft disc, green rimmed disc. | Elements of a Unity Flare asset (Built-in Flare Layer) |
| `LensStarburst.png` | 512x512 | billboard | 6-spike diffraction starburst with fine radial streaks. | Streetlights and police strobes in the photo-camera view |
| `Caustics_Tile.png` | 1024x1024 | tile 0.5-1.5 m (seamless) | Tileable water-caustic network computed by photon mapping through a wavy surface (grayscale). | Puddle reflections on walls/kerb (projector or light cookie, scrolled), reflections inside glass cases |

### Light cookies

| File(s) | Pixels | Real size | What it is | Suggested placement |
|---|---|---|---|---|
| `Cookie_FlashlightLED.png` | 512x512 | spot cookie (angle 40-50 deg) | Modern LED torch: tight hotspot, faint ring, wide soft spill, faint die square. | Player flashlight spot light |
| `Cookie_FlashlightIncandescent.png` | 512x512 | spot cookie (angle 30-40 deg) | Old reflector torch: dark filament hole, bright donut, reflector artefact rings. | Alternative flashlight / police officer torch |
| `Cookie_UVTorch.png` | 512x512 | spot cookie (angle 35 deg) | 9-LED UV torch: overlapping blotchy LED spots. | Player UV torch (light colour ~ 0.45, 0.2, 1.0) |
| `Cookie_GallerySpot.png` | 512x512 | spot cookie (angle 20-30 deg) | Framed track spot: flat plateau, crisp edge, faint spill ring. | Display-case track spots (still on in the dark store) |
| `Cookie_SceneFlood.png` | 512x512 | spot cookie (angle 70-90 deg) | Rectangular LED flood panel beam (4:3) with a very faint LED grid. | Placeable scene lamps on tripods |


## Changes and provenance

- Original files: generated October 14, 2026 by the scripts in `Source~/` (run with Python 3, numpy,
  scipy, Pillow: `python3 footprints.py <out>`, `fingerprints.py`, `glass.py`, `cracks.py`,
  `puddles.py`, `surface.py`, `roadlitter.py`, `effects.py`; `leaves_photo.py <LeafSet024 dir> <out>`).
  Random seeds are fixed, so a rerun reproduces the same images. `Fingerprints_Atlas.png` was
  regenerated on October 15, 2026 after a review: the latent and developed rows now use the same
  seed per column (`python3 fingerprints.py <out> atlas`).
- `SmokePuff_Kenney01/04/07/08.png`: Kenney Particle Pack 1.1 `smoke_01/04/07/08.png` (PNG
  Transparent), converted from palette PNG to RGBA, colour of fully transparent pixels set to white.
- `LeafLitter_Wet*`: derived from ambientCG LeafSet024 (2K JPG Color, NormalGL, Opacity, Roughness).

## Overlap with other packs

- PBRInterior/GlassSmudge is a 0.6 m tiling smudge overlay for glass; the fingerprints here are single
  evidence prints and a handprint to place on top of it.
- ForensicModels/GlassCrackDecal is for cracked glass panes; GlassShards/GlassCrumbs here are floor
  scatter (cheaper than the GlassShards OBJ meshes at distance; use both: meshes near, decal around).
- PBRExterior/PuddleMask is a tiling 8 m mask for a road shader; the puddles here are individual
  quads with their own normal and smoothness. PBRExterior/LeakStain (ambientCG Leaking006) is a
  rust/water run-off stain; GrimeStreaks_Wall is soot/dirt run-off under ledges.
- NightGraphics ShoeTrace/Scuff/DustSmear are illustrative flat graphics; the decals here are the
  realistic replacements.
