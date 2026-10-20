# Open work

October 20, 2026. Source, local Unity project and GitHub must remain aligned.

- [x] Reconcile Claude's folder organization with local glass/UI/audio/scene fixes and preserve GUIDs.
- [x] Enable Package Manager and resolve compatible OpenXR, XR Management and Input System packages; compile in Unity.
- [x] Configure Windows DX11/SPI/controller interaction profile; build one keyboard/PCVR player and confirm a real Meta XR session starts.
- [x] Regenerate current scene with outdoor table remnants disabled; fresh lighting/reflections; desktop player regression.
- [x] Build existing saved scene without regeneration; provide three launchers and one portable snapshot.
- [ ] Retest official Meta controller hands, smooth turning, headset-following belt, raised/adjustable height, grip anchors, camera placement and padded tool selection. Finish human Quest 3 acceptance: both eyes/hands, tracking recovery, movement comfort, wheel, holsters, camera, door pushes, hints and quit path.
- [ ] Headset visual audit: portable lamps, camera flash, contact shadows, glass/reflections, UV evidence and bilingual UI. Measure GPU time and tune budget.
- [ ] Run the broader legacy/gallery/new VR validators when they can be run without overwriting the user's active scene; report exact pass/fail counts. Desktop pose regression does not replace these or hardware testing.
- [ ] Rehearse presentation; then dependency-based cleanup/archive, retaining licences and metadata.
- [x] Import Koda's October 20 FBX car unchanged; park along the road with uniform scale.

See VR_TESTING.md for the actual setup, controls, build behavior and connection troubleshooting.

- [ ] Accept authored Meta hand poses in the headset: finger curl, palm facing, tool/torch grip fit and shaded transparency. Animation deformation and strict build passed; human fit pending.

- [x] Repair hand mesh import/unit conversion and post-animation alignment; inspect rendered open/closed meshes from the actual runtime component.
- [ ] Retest repaired hands in headset; previous hand implementation was rejected by Koda.

- [x] Lower belt and holstered tool positions 12 cm.
- [x] Symbolicate native shader crash and enable shader lifetime mitigation; Koda confirmed next player stays running.
- [ ] Accept latest controller-frame mapping in the headset. Sideways fingers were corrected by mapping the anatomical forward axis to the controller forward axis; both editor axis checks pass.

- [ ] Accept 12-degree downward hand adjustment and dedicated white/UV flashlight grip in the headset; combined renders and sampled grip-clearance checks pass.

- [ ] Retest corrected flashlight-only palm orientation; original empty-hand models restored after misunderstanding. Build and grip checks pass.
- [ ] Headset check of Claude's October 19 shadow/UV/reflection round: UV stops at walls, no stripe acne, case reflections, ambient prop materials; profile the shadow maps (needs a bake build for the builder changes).
- [ ] Quest 3 standalone app: install Android Build Support, run Build-Quest-App.cmd, check frame rate (72 Hz) in the showroom with the flashlight, presenter menu size, glass/reflections; then fixed foveation if needed (Docs/QUEST_STANDALONE.md).

- [x] Render-check actual white/UV torch shadows in PC and Quest quality tiers; keep store interior fixtures off.
- [x] Remove player-displacing collision from small tools; preserve selection, UV occlusion and solid placed scene lamps.
- [x] Close display-prop undersides, move entrance prints onto handles and reduce/ vary small-injury blood transfers.
- [x] Add generated Arabic brand/section plaques, exterior-only upper storeys, varied cornices/parapets, awnings, rails, leafy wind canopies and deeper street rows.
- [x] Replace disconnected horizon cards with a geometry skyline; reduce fog and extend ground beyond the view range.
- [ ] Human Quest retest: both torch shadows, picking/deleting tools, both street kerbs, dark interior, hand grips and frame rate. Desktop/editor tests do not replace headset testing.

- [x] Generate and integrate Arabic closed/staff/CCTV notices, workplace reminder and a store-layout evacuation plan; keep original artwork and prompts.
- [x] Add selected shop/staff-room and pavement props with overlap rejection and a clear central aisle.

- [x] Repair prop finishes/rear-room reflections, metric street surfaces, photographic tree sprays, legacy car light effects and transparent shard/contact shading.
- [x] Retreat belt on measured bending (not head pitch); align camera handle and aiming axis with torch.
- [ ] Headset acceptance of new belt retreat, floor pickups, camera handle fit and latest glass; Quest performance remains unmeasured.

- [x] Restore the simple authored camera holding grip; remove saved preview torch duplicates and prevent render checks from saving them.
- [ ] Confirm the restored normal camera grip in the headset; no further finger changes requested.

- [x] Raise belt/holsters 3 cm and restore earlier photographic city backdrop with its fog. Headset comfort acceptance remains pending.
