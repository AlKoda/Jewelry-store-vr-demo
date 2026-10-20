# Showcase refinement — October 20, 2026

Current JewelryStoreNight scene, Unity 6000.6.2f1 / Built-in / DX11. SHOWCASE_REFINEMENT_DELIVERY_PASS / EXISTING_SCENE_DELIVERY_PASS.

## Implemented models

- Watch roll: closed leather shell with real thickness, rounded padded bolsters, end caps, seam piping and a stitched closure flap. Cream suede contrasts with the leather.
- Presentation boxes: rounded walnut floor and four independent sides, recessed divided lining, ring rolls, tilted padded lid, hinge barrels and leaves, screw heads, lid stays and front latch.
- Necklace ramps: curved padded surface over a closed wood support, piping and small leather retaining tabs/fixings.
- Remaining inserts: refined earring arches with collars/rounded bases, padded oval ring trays, trimmed stepped pedestals, seam-piped pillows and beveled octagonal plinths. Cabinets/glass/drawers and aisle footprints retained.
- Fine surface detail comes from six original periodic 512 px suede/leather/stitch maps, repeat wrapping, mipmaps and restrained bump. Android upholstery overrides are ASTC 6x6 / 512; photographic signs are capped at 1024 on Android. No added lights.

## Artwork

Built-in image_gen produced three final photographic Arabic signs and one construction reference. Final signs: ArabicSceneSigns/CollectionEarrings_Photographic.png, CollectionNecklaces_Photographic.png and Craftsmanship_Photographic.png. Original simple prints retained. Generated images were inspected for Arabic text and saved in the workspace; physical frame heights follow each actual texture aspect. Exact prompts/mode/paths: [SHOWCASE_REFINEMENT_PROMPTS.json](../../../Prototype/ThirdParty/ArabicSceneSigns/SHOWCASE_REFINEMENT_PROMPTS.json). [Construction concept](../../References/ShowcaseConstructionConcept.png) is a design reference, not an image of the game.

## Verification

- Actual eight inserts total 39,356 triangles, below the enforced 40,000 budget; no extra realtime lights. This is a geometry budget, not a GPU frame-time measurement.
- Rounded-box primitive has outward winding, no degenerate triangles and a closed manifold after positional welding. 1 mm box normals remain unit length; Face/Quad/Loft use explicit normalization to avoid the default tiny-vector cutoff. New insert normals are valid. Intentional projected contact meshes retain their original unlit data; no unrelated geometry is reshaped.
- Model vertices fit existing case deck/glass envelope. Five distinct collection textures, physical-sign clearance, open/closed door faces, marker size and actual VR/desktop offset pickup/move/release checks passed. Active shop audit covered 768 material slots / 193 materials. Duplicate opaque face scan: zero.
- Existing scene/fog, off interior fixtures, car mesh preservation, white/UV shadow renders, tool safety, belt bending and camera grips pass. VR visual tests and strict Windows player build pass.
- Fresh ProgressiveGPU bake: 36.2 seconds, seven 1024 lightmaps, 676 light probes; fourteen reflection captures refreshed. No temporary torch models saved.

Actual saved-scene torch renders: [watch roll](refined-watch-roll.png), [presentation boxes](refined-boxes.png), [photographic wall print](refined-sign.png). These show the current in-game materials and geometry, separate from the generated concept.

Remaining: human headset visual acceptance, shimmer/flicker evaluation in both eyes and Quest GPU/frame-rate profiling. No claim of measured standalone performance or complete photorealism.
