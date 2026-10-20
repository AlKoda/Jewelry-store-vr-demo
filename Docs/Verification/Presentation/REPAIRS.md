# User-reported build repairs — October 20, 2026

Unity 6000.6.2f1, Built-in/DX11, current saved JewelryStoreNight scene. Strict Windows build passed. Existing scene layout, supplied FBX geometry and store interior lights OFF are preserved.

Verified in Unity:

- 178 prop material slots receive restrained finishes. Non-screen accidental emission is disabled with EmissiveIsBlack, screen glow is dimmed, dielectric finishes do not use metallic maps. Three local rear-room reflection captures were added.
- Road/pavement use dry PBR sets, reduced gloss and metric UV0 instead of accumulated extension UVs. Five branched trees use small photographic leaf sprays with coverage-preserving alpha mips and subtle wind. Existing large leaf cards/bare trees are inactive.
- Legacy siren bar, colored washes, headlight halos and old beams are inactive. Two warm baked lamps align with the supplied model's actual front. No added luminous rectangles; car mesh SHA256 remains BEF2445C622AC69BAFB734D90BE5F7173EC9046F03790B48DD18D6761CE1445A.
- Shard faces alpha .045, cut edges .10; no diffuse-white glass shading. Closed thickness remains. Individual projected contacts replace the broad pile patch. Actual glass-on/off render comparison changed 78,152 pixels above 0.006 channel difference; close-up inspected. These are raster transparency/probe reflections, without ray-traced refraction or caustics.
- Belt retreats 13 cm at rest and up to another 28 cm with measured headset height loss; smoothing and floor clearance retained. Pitch-only looking down does not retreat it. Simulated standing/look-down/bending checks pass.
- Both camera handle poses match the accepted torch rotation and contact point; native animated hand/camera renders inspected. Existing empty-hand forward-axis, mesh shape and torch clearance checks still pass (maximum sampled torch penetration ~1.13 mm).
- Tool safety, trigger selection, UV occlusion and wall blocking checks pass.
- White/UV actual torch shadows render in PC soft-2048 and simulated Quest-tier hard-1024 settings, with 3,865–3,943 visibly shadowed pixels. This is a desktop simulation of the tier, not a Quest GPU measurement.
- Fresh bake: seven 1024 lightmaps, 676 light probes; 14 reflection captures refreshed. REPAIR_REVIEW_DELIVERY_PASS / EXISTING_SCENE_DELIVERY_PASS.

Images: repair-0 office, repair-1 staff room, repair-2 storage, repair-3 threshold, repair-4 car/rear street, repair-5 foliage, repair-6 low glass angle, repair-7 car/front; glass-close.png and camera-grips.png show targeted reviews. Cameras use the actual scene/torch and do not change its lights permanently.

Remaining human checks: Quest belt location while bending, floor tool selection versus holsters, camera wrist comfort/handle fit, glass legibility in stereo, frame time and standalone build. Android Build Support is still unavailable here. No headset acceptance is claimed.

## Camera-hand / floating-torch follow-up

Restored the earlier simple Meta holding animation at Koda's request. No procedural finger IK or custom finger twisting remains. The camera mount follows that grasp while preserving the controller/torch aiming axis. Native bone rotations are compared against a fresh animation evaluation to verify no individual pose overrides. Empty-hand and torch regressions are retained. Camera bounds overlap remains diagnostic (conservative box depth is logged); a collision-free physical camera grasp is not claimed.

The floating white/UV torch pair came from saved editor validation objects: 36 orphan light/model roots were found under Player/DesktopPreviewCamera and removed. Preview lights and every model child now use DontSave; validators release models/lights and rendering callbacks explicitly. Scene-save and reflection-capture guards remove camera preview remnants. The cleaned saved scene contains zero preauthored runtime torch names; runtime-equipped torches and belt props are retained. Ownership checks exercise both torches, no-save flags and destruction without leftovers. No lightmap rebake was needed; reflection captures were refreshed after removal.

Updated camera render, authored-animation integrity, existing hand/torch alignment and strict Windows build checks passed. Human headset acceptance remains pending. Further finger adjustment was stopped at the user's request.