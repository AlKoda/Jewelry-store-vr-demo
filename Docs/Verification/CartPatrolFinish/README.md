# Trolley, camera grips and patrol finishing — October 26

Base commit: fad610f. User confirmed “card” meant the outdoor equipment trolley. Same editable project/scene, existing crowd and sandbox mechanics retained.

## Behaviour and models

- PortableFieldKit.RollOnGround defaults on for carts. Grabbing records the existing grip point without moving the cart. Horizontal handle displacement drives acceleration-limited movement (0.85 m/s maximum), gradual steering and braking. Hand elevation does not lift it. A tracking/teleport jump over 65 cm releases the handle. Placement parks it where it stands; reset is the explicit home relocation.
- Ground samples under all four wheels, a 4.5 cm upward-step limit, swept body clearance and a final rotated-volume check prevent unsupported travel/obstacle penetration. The chassis remains upright. Movement uses no dynamic rigidbody. Passengers retain their local positions/rotations, so the case, lid and tool docks ride together.
- While the handle is held, keyboard/thumbstick walking is capped at 80% of trolley maximum speed (0.68 m/s at the default), including sprint. This keeps ordinary locomotion from outrunning the trolley; release restores normal pace. Actual room-scale movement is unaffected and excessive separation releases the handle.
- Four native caster forks swivel; individual rubber/hub meshes spin from travelled distance. Dynamic cart parts remain on the cart's existing reflection-excluded layer. The original batched frame remains disabled for provenance.
- Reversal: stop Play mode, select NightStore/InvestigationFieldwork/ExteriorFieldTable and disable Roll On Ground on PortableFieldKit. The prior hand-follow branch remains in the same component. The authoring finish hook enables the new default when regenerating fieldwork.
- EvidenceCamera.HandSeat is mirrored outwards: the camera body offsets +85 mm for a left holder, -85 mm for a right holder. Each hand is on its corresponding camera edge. The accepted HandHold quaternion, authored fingers, photo lens and torch handoff are unchanged.
- Patrol: darker procedural rubber tread/sidewall response, neutralized lamp chroma and two native source-fitted parked wipers. Near 34,184 / distance 15,000 triangles, no extra light or collider. Original body geometry, UVs/corner normals and source colour SHA remain unchanged. Wipers add 216 triangles with 22 source-surface contacts and a 3 mm offset.

## Completed local verification

Unity 6000.6.2f1 Built-in, CartPatrolFinishRun: CART_PATROL_FINISH_PASS and EXISTING_SCENE_DELIVERY_PASS in Builds/cart-patrol-finish.log. Final pace integration: CartPatrolVerifyRun completed CART_PATROL_FINAL_VERIFY_PASS with full fieldwork, presentation and strict build checks in Builds/cart-patrol-verify.log.

- Targeted trolley regression: no movement at grab, bounded per-step motion, floor lock during hand elevation, release braking, wall obstruction, tracking-jump release and reversible legacy movement.
- Full FieldworkChecks, PortableCaseChecks and ForensicSandboxChecks passed: actual trolley/case passenger movement, moving docks, reset, case support and tool/evidence workflows. Updated only the cart's authoring hook and rolling/local-passenger contract in Claude's sandbox authoring file, as required by the explicit user request.
- Camera left/right mounting and retained rotation checks passed. Native hand renders: UnityProject_Demo/Builds/camera-grips.png and camera-grips-front.png. Existing conservative-box diagnostics report 4.7 mm left / 7.7 mm right overlap; these are bounding-box approximations rather than exact bevelled-mesh contact measurements. Human grip acceptance remains pending.
- Car source texture/mesh, optics, LOD, shadows, collider and reflection checks passed. Actual inspection renders: UnityProject_Demo/Builds/PatrolReview/polished-*.png; wipers visible in view 0, rubber close-up view 5.
- Fresh bake: three lightmaps, 800 light probes; 15 reflection captures refreshed. Presentation, torch-shadow and VR visual checks passed. Strict Windows/PCVR build completed at Builds/Windows/JewelryStoreDemo.exe. No player was launched.

Quest Link push feel, camera side grips and frame time require a human test. No standalone APK/frame-time measurement in this pass.

Canonical snapshot refreshed to 154.7 MB. CRC, all 417 source/local/archive code and metadata files, exact saved-scene parity, car-pack source/local/archive bytes and provenance hashes passed. The archived scene contains the grounded trolley setting, four caster pivots and native wipers. Original colour SHA is unchanged.
