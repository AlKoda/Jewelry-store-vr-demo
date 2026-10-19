# October 14 art pass — display cases and jewelry displays

Art-only staging prepared locally while Claude owns game/editor code. No C#, shaders, current Unity scenes, live materials, user-supplied models or current build were modified. No GitHub push. No message was sent to Claude.

## Open and review
- Editable master: ReferenceAssets/ArtPass20261014/DisplayCase_ArtReview.blend (textures packed).
- Review images: case-beauty.png, case-detail.png, props-detail.png in the same folder.
- Importable assets: Deliverables/*.fbx, plus OBJ/MTL alternatives; Materials/ contains the complete five selected PBR map sets.
- Previews are Blender Cycles studio renders, NOT screenshots of the running Unity game. They assess the models/materials. They do not establish Unity appearance or performance parity.

## Geometry
| Asset | Triangles | Mesh objects in export | Dimensions (metres, width x depth x height) |
|---|---:|---:|---|
| DisplayCase_Hero | 2,916 | 6 | 1.800 x 0.658 x 1.050 |
| RingTray_Hero | 908 | 1 | 0.340 x 0.232 x 0.058 |
| BraceletStand_Hero | 1,028 | 1 | 0.264 x 0.120 x 0.187 |
| BezelRing | 1,024 | 1 | 0.0224 x 0.0070 x 0.0253 |
| BezelRing_LOD1 | 358 | 1 | approximately the same |

Case: satin walnut panels with narrow reveals; recessed rear pulls; recessed black plinth; champagne trim and uprights; graphite seals; inset cream lining; separate 6 mm front/back/side/top panes with softened edges. No large opaque collision box or game behavior is embedded. This is an intact asset; damaged states need integration with the existing broken-case system.

The master retains individual components for editing. Export combines opaque case components into one mesh with four material slots, leaving five panes independent. Multiple material slots still mean multiple draw calls. Tray, stand and ring each export as one mesh with their material slots. Ring LOD is supplied but no automatic distance switching is installed. The case has more triangles than the previous 1,080-triangle version; no performance improvement is claimed. Use selectively for nearby cases and retain cheaper distant assets where appropriate.

## Material mapping for later integration
| Slot | Map set | UV scale / tiling |
|---|---|---|
| WalnutVeneer | Walnut | 1 |
| ChampagneBrushed | BronzeBrushed | 2 |
| IvoryMicrosuede | SuedeCream | 2 |
| CharcoalVelvet | VelvetCharcoal | 2 |
| DarkStone | StoneTopDark | 1 |

Maps: Color sRGB; Normal tangent-space OpenGL +Y; MetallicSmoothness linear RGBA, R metallic and A smoothness. Use the existing shader convention, not roughness in alpha. Blender previews invert smoothness for Principled roughness. Normal strength in the preview is 0.25. Box surfaces use metre-based planar UVs; curved parts have their primitive UVs and should be checked for texel density when integrated. FBX/MTL cannot preserve the entire Blender node graph: assign these maps and tiling explicitly in Unity.

GraphiteGasket uses dark neutral nonmetal, roughness 0.6. LowIronGlass in the Blender preview uses IOR 1.52, roughness 0.015 and transmission 1. Route its five panes to the game's glass shader; importing FBX will not reproduce Cycles transmission automatically. JewelryGold is metallic with roughness 0.18; Gemstone is a faceted studio material (IOR 2.42), requiring an appropriate realtime approximation later.

## Provenance
Original new model geometry was authored for this project in Blender. Materials are an unchanged selected subset of Claude's staged PBRInterior pack at Git commit 32ea46740f9f37099cd5ed6027c5f9f5ea1677a1, copied without merging that branch. Sources: Walnut/Wood067, SuedeCream/Fabric028, VelvetCharcoal/Fabric023, BronzeBrushed/Metal009, StoneTopDark/Marble006. The material pack's LICENSE.txt and complete sources.json are included; they describe source adaptation and mirror hashes. Selected-file hashes were compared with that manifest (material-validation.json). ambientCG's official license page was checked October 14 and states CC0 for its asset files: https://docs.ambientcg.com/license/ . The whole WIP pack has NOT been approved or integrated by this art pass.

## Checks and remaining work
FBX exports reimported successfully in a clean Blender process; dimensions, triangle totals, floor pivots and UV-layer presence recorded in export-validation.json. No missing image errors in studio renders. The ring floor pivot was corrected during export. UV presence is not a full UV-distortion audit. PBR hash matches are not proof of game compatibility.

Next joint review with Claude: compare visual direction, import into a separate Unity review scene, map materials, retain collisions and damaged-case states, choose LOD distances, verify glass sorting/probe response, inspect curved-part UV density, and measure real frame time / texture memory. UI, flashlight interaction, lighting code, audio and atmosphere remain with Claude.
