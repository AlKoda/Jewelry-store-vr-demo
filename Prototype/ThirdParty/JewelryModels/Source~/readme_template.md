# JewelryModels

Realistic jewellery-shop merchandise, displays, packaging, bullion and counter equipment for the night jewellery store
({{COUNT}} OBJ models, {{TRIS}} triangles in total). Most models are original, procedurally generated at true size from
the scripts in `Source~/` (project-owned); four are third-party (one CC0, three CC BY 4.0, see `LICENSE.txt`).
`Source~/Preview.jpg` shows every model. Nothing here has been imported into Unity yet: **unverified in Unity**.

## Conventions

- OBJ + MTL, **metres**, **Y up**, **front = +Z** (authored like glTF; Unity's OBJ importer mirrors X so the model reads the same).
- Pivot: bottom centre (lowest point at y = 0, centred in x/z) unless the notes say "same transform as ...".
- Rings: finger axis along **X**, stone up (+Y). Rotate 90 deg about Z to slide a ring on a cone, 90 deg about Y for a ring box slot.
- Procedural models carry explicit normals (`vn`): set *Normals = Import* (the default) so faceted gems stay flat-shaded and
  metal stays smooth. The four converted third-party models have no normals: let Unity *Calculate* them (smoothing angle 60).
- MTL `Kd` is written as an **sRGB** colour (what Unity's colour picker shows); `materials.json` gives the recommended Built-in
  Standard values (albedo, metallic, smoothness, rendering mode, emission and which maps to add) for every material name.
  Unity's OBJ import only wires `map_Kd`; normal, metallic-smoothness and emission maps must be assigned in code
  (`DemoAssetLibrary.NormalMap` / `DataMap`) or by hand.
- Transparent materials (Diamond, Sapphire, Ruby, Emerald, Glass, Acrylic) are written with `d < 1`, but the importer makes
  opaque Standard materials: switch them to Transparent (or a gem shader) when wiring.
- Small jewellery should not be lightmap-static: light it with light probes and a reflection probe in each case
  (same rule as `StoreKitProps.ProbeLitParts`). Add one box collider per item with `DemoAssetLibrary.AddBoundsCollider`.
- Triangle budget: jewellery 1-8k (the two pendant necklaces are 8.8k because of their ~140 modelled chain links and
  the two pearl strands 8.5k because of their modelled silk knots), stands and packaging under 3k.

## Paired transforms

| Pair | How to place |
|---|---|
| `NecklaceBust` + `NecklacePearlDraped` / `NecklacePendantDraped` | Same position, rotation and scale; the necklaces were generated on the bust surface. |
| `WatchPillow` + `WatchDressSteel` | Same transform. The case back rests 0.5 mm into the crown of the cushion, the strap hugs the cushion (0.2 mm clearance) and its tails end flat under the cushion's bottom edges, so the pair stands on y = 0 with nothing below it. |
| Any ring + `RingCone` | All six rings have a 17.1 mm inner diameter. Rotate the ring 90 deg about Z (axis vertical) and put its hole centre on the cone axis at y = 0.0665 m above the cone base. Hole centres in model space: (0, 0.0102-0.0111, 0) for the procedural rings (see each ring's notes), (0, 0.0121, 0.0014) for `RingAntiqueTigerEye`. |
| Any ring + `RingBoxOpen` | Rotate the ring 90 deg about Y (axis along Z); hole centre at about (0, 0.034, 0) in box space (same hole-centre offsets as above). |
| `EarringsGoldHoops`, `EarringPearlDrop`, `EarringGoldLeaf` + `EarringStand` | Bar axis X at y = 0.124 m; hang the top of the earring on the bar. |
| `PriceTag01-04`, `PriceStand01-04` | One shared atlas `PriceTags_Color.png` (4 x 2 cells); each OBJ already points at its cell. |

## Models

| File | Kind | What it is | Size W x H x D (m) | Tris | Materials / maps | Origin / licence | Suggested placement in the night store | Notes |
|---|---|---|---|---|---|---|---|---|
{{MODELS}}

## Textures

| File | Pixels | Kind | Used by |
|---|---|---|---|
{{TEXTURES}}

## Ideas for the burglary scene

- Smashed central bays: `NecklacePearlDropped`, `NecklacePendantDropped`, a `RingGoldBand`, a few `Gem*` stones and an empty
  `RingBoxOpen` on the floor or ledge make natural evidence-marker targets (they lie on y = 0).
- Intact wall cases: `NecklaceBust` with a draped necklace, `RingCone` with a ring, `EarringStand` with hoops, `WatchPillow` +
  `WatchDressSteel`, each with a `PriceStand` or `PriceTag`.
- Rear office with the open safes: `GoldBar1kg`, `GoldBar100g`, `GoldCoin` stacks left behind, a `GemParcel` on the desk,
  `JewelersLoupe`, `PocketWatchVintage`, `RingAntiqueTigerEye` in a repair tray.
- Counter: `PaymentTerminal`, `PosTablet` (screen as a faint emission at night), `GiftBag`, `GiftBox`.
- Diamonds read best with a reflection probe per case, smoothness 0.98 and a sparkle/cubemap gem shader; under the UV torch
  many real diamonds fluoresce blue - an emission boost when the UV lamp is on would be a nice, cheap effect.

## Revision of October 15, 2026 (fixes after an independent check)

- `RingSapphireHalo`: the halo head no longer floats 1.7 mm above the shank. The halo frame now has a tapered gallery
  skirt that rests on raised cathedral shoulders, the four claws run down past the pavilion into the top of the shank,
  an under-gallery rail ties them together and the sapphire's culet sits on the shank: one connected piece.
- `WatchDressSteel`: the case back now rests on the crown of `WatchPillow` (0.5 mm into the soft cushion instead of
  3.2 mm above it), the lugs are 20 mm apart to take the 20 mm strap, and the padded strap follows the cushion 0.2 mm
  off its surface and tucks under its bottom edges, so nothing (strap or buckle) goes below y = 0 any more. The buckle
  sits under the cushion and is not modelled.
- `RingAntiqueTigerEye`: rescaled x 1.225 (26.3 mm across) so its finger hole is 17.1 mm like the procedural rings
  (it was 14.0 mm); its hole centre is (0, 0.0121, 0.0014) m in model space.
- `EarringPearlDrop_MetallicSmoothness.png`: was all zero (an RGBA resize with zero alpha wiped the RGB). It is now
  built from the colour map: gold metallic 1, pearls 0, smoothness 0.71 (= 1 - the glTF roughnessFactor 0.292).
  `PocketWatchVintage_` and `RingAntiqueTigerEye_MetallicSmoothness.png` were regenerated with the same per-channel
  converter (`Source~/maps.py`); they change by less than 1 level on average.
- Same pass, other floating parts found by a surface-gap scan of every model (any part more than 0.15 mm from the rest):
  - `EarringsDiamondStuds`: the post stopped 0.28 mm short of the basket; claws and post now meet on a small seat disc.
  - `NecklaceBust`: the plinth top falls about 1 mm towards its rounded edges, which left the bust's bottom edge up to
    0.9 mm in the air at the sides; the bottom edge now sits 1.3 mm into the plinth (no gap anywhere). The plinth is
    10 mm thick (the old text said 6 mm).
  - `NecklacePearlDraped` / `NecklacePearlDropped`: the pearls were loose beads 0.6 mm apart; the strands are now
    hand-knotted (a silk knot in every gap, threaded into the fluted clasp, which lies along the strand) and the draped
    clasp rests on the neck instead of 2 mm off it. 58 pearls each, 8.5k triangles.
  - `GemParcel`: the loose stones hovered 0.8 mm above the paper; each now rests on the paper surface under it. The
    paper is 0.1 mm thick (was a 0.3 mm open sandwich).
  - `RingBoxOpen` / `RingBoxClosed`: the lid swung about a point 2.2 mm outside the rounded shells and the hinge pin
    floated 2.5 mm behind the box. The base and lid now have square seam edges that close flush, the lid is hinged on
    that back seam edge and the 36 mm hinge pin lies along it.
  - `JewelersLoupe`: the lens barrel now fills the case hole (it had 0.3 mm of air all round).
  Every other model was already connected within 0.15 mm, apart from two deliberate clearances: each price-stand card
  sits 0.3 mm inside its acrylic holder and the pendant's bail rides 0.25 mm clear of the chain link through it. The
  hoops and the studs are pairs of separate earrings.

## Regenerating

`python3 -P Source~/build.py OUT_DIR` (numpy, scipy, Pillow; PYTHONPATH = Source~) rebuilds every procedural model and
texture; `Source~/postprocess.py` documents how the four third-party models were re-oriented, scaled and their maps
converted after `Tools/glb_to_obj.py` (map conversions in `Source~/maps.py`). `Source~/render.py` is the small numpy rasteriser used for `Preview.jpg`.
Hashes, sources and licences of every shipped file are in `sources.json`; sizes and triangle counts in `models.json`.
