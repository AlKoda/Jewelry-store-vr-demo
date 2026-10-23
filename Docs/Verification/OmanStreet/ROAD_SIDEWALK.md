# Local road and sidewalk repair — 21 October 2026

Windows player rebuilt successfully at 14:45 local time. No promotion, commit, push or official snapshot refresh. Original backup scene SHA256 remains E32E3E9EDD15021CC7C66AF3D6A30DB6729ADE22A6C9EF8B989BEDB48468B42C. Official ZIP remains 63E75AC1EAFFD20B8ABA4F52FCC93075DEB369A45E467A43F4698EDC63F3A072. HEAD remains 1005d47765abefd595dededebf57895ef6c6a999.

## Evidence

- ROAD_SURFACE_CHECKS_PASS: no raised parking renderer; three road crack masks plus concrete fractures; filtered linear data; 28 opaque junction backings. Actual rendered triangles intercept 84 rays through junctions at three heights.
- ROAD_GPU_LAYERING_PASS: network, stress and fatigue channels each darken the painted surface in an isolated render. CPU mask coverage guard rejects incorrectly filled parking masks.
- OMAN_EXTERIOR_CHECKS_PASS: 29 shops, protected street layout, no new realtime lights, 54,980 exterior triangles.
- ARCHITECTURAL_GLASS_CHECKS_PASS: four physical panes, 1,100 fragments, 60,040 glass assembly triangles unchanged.
- GENERAL_SCENE_REVIEW_PASS: 613 renderer components / 1,179 slots; zero missing references/scripts/shaders/meshes, invalid lit normals or exact duplicate renderer candidates. This exact-duplicate scan is not an exhaustive partial-coplanar intersection audit.
- Shop model, showcase refinement, raw-car source/GPU normal and VR visual checks pass; original interior/car transforms unchanged.
- Fresh light bake, 14 reflection captures, strict Windows build and EXISTING_SCENE_DELIVERY_PASS / ROAD_SIDEWALK_LOCAL_DELIVERY_PASS.

Actual flashlight-lit Unity views: UnityProject_Demo/Builds/road-sidewalk-0.png through -7.png. Views 0–1 cover parking paint/cracks, 2–3 pavement, 4–5 nearby grazing angles, 6–7 building junctions. Visually checked road 0, pavement 2 and near-row junctions 6. Static views cannot certify all temporal flicker or Quest performance. Seven generated originals and the derived packed masks, normals and layout are identified in the asset provenance.

Identified flicker causes addressed: raised marking boxes removed entirely; paint is now within the opaque asphalt surface. Sub-millimetre glass contact layers receive depth bias. No claim that every unrelated object is flicker-free. Actual Quest GPU/shader cost and headset acceptance remain pending.

Build log: Builds/road-repair.log. Existing unrelated Quest setup analyzer warnings and RawCarClay derivative warning remain; strict build completed successfully. No changes to Claude's standalone setup or accepted hand/camera/belt code.
