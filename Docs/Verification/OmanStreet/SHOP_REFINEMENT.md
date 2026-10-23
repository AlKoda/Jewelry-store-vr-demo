# Wider lighting and shop refinement — 21 October 2026

Local player rebuilt at 15:35 and reopened in keyboard fullscreen. Source entry points: PresentationPolish.ShopAtmosphereInspectRun, ShopAtmosphereRefinementRun and ShopAtmosphereDeliveryRun. No GitHub promotion, commits or official snapshot refresh.

## Delivered

- Municipal light cones 115→150 degrees, range 17→23 m; storefront cones 105→130, range 7→9 m. Broader soft profile replaces tightly focused cookies. Existing baked warm sources retained, no additional realtime lights.
- White torch 42→78 degrees; UV 32→68, with defined inner cones and the broad profile assigned in the scene. Existing grip/hand/belt and Android hooks preserved.
- Two live intact Glass children actually overlapped the broken door panes. Removed both; retained one fractured assembly per leaf and original door interactions. Old flat door shard renderer disabled.
- Entrance/two display-case fine piles: 3,600 closed triangular grains in three combined meshes, rough dielectric scattering plus fine contact coverage, no emission/physics/scripts. The fine prism uses eight triangles to control cost. Existing larger shards retained. Fine-glass visibility test changed 160,165 pixels above .008 RGB difference, proving the new layer contributes visibly; not a realism score.
- Retuned ceiling/floor/ceramic PBR maps and finish, added a seated staff ceiling hatch. Safe shell/lining/hardware materials and gasket/fastener/service-plate geometry improved without changing doors, stock or collisions.
- Reversed and darkened server scan; because both scanned broad faces expose wiring, modeled closed equipment fronts cover the room side.
- Split four disconnected snack objects while preserving their texture coordinates and original surfaces. Varied yaw/depth and kept them clear of the existing plant.
- Added existing broom, dustpan and spray bottle assets; modeled an open cleaning bucket with handle and cloth. No external downloads or new licences.
- Car direct lighting was already face shaded. Zero gloss, probe GI and per-polygon SH now keep indirect light from smoothing the supplied normals. Source FBX/importer/triangle positions unchanged, 7,317 triangles, SHA256 BEF2445C622AC69BAFB734D90BE5F7173EC9046F03790B48DD18D6761CE1445A. No texture maps, subdivision or mesh edits.

## Checks and limits

SHOP_ATMOSPHERE_CHECKS_PASS; architectural glass, continuous road, Oman exterior, shop/refined inserts and raw-car source/GPU-normal checks pass. Eight normal/grazing white/UV shadow cases pass for PC and simulated Quest; no lit self-shadow artifact pixels above threshold. Four occluder checks still produce rendered shadows at PC 2048 and simulated Quest 1024.

Fresh bake 13.9 seconds, three lightmaps up to 1024, 676 probes and fourteen reflection captures. General scene scan: 632 renderer components / 1,200 material slots, zero missing references/shaders/meshes, invalid lit normals or exact duplicate renderer candidates. VR visual and strict Windows build pass. SHOP_ATMOSPHERE_LOCAL_DELIVERY_PASS in Builds/shop-refinement-delivery.log. This is not an exhaustive partial-overlap audit or hardware frame-time measurement.

Actual Unity images: UnityProject_Demo/Builds/shop-refinement-after-0.png to -9.png (break table, server, safes, doors, threshold pile, case pile, ceiling, vase, car, cleaning corner). Additional fine-glass-visibility.png, architectural-glass-refined-shop-* and lighting-wider-* images. Renders reviewed; user/headset acceptance and Quest performance remain pending.

Official ZIP unchanged: 63E75AC1EAFFD20B8ABA4F52FCC93075DEB369A45E467A43F4698EDC63F3A072. HEAD unchanged: 1005d47765abefd595dededebf57895ef6c6a999.
