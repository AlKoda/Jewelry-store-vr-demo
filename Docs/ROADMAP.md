# Open work

October 19, 2026. Source, local Unity project and GitHub must remain aligned.

- [x] Reconcile Claude's folder organization with local glass/UI/audio/scene fixes and preserve GUIDs.
- [x] Enable Package Manager and resolve compatible OpenXR, XR Management and Input System packages; compile in Unity.
- [x] Configure Windows DX11/SPI/controller interaction profile; build one keyboard/PCVR player and confirm a real Meta XR session starts.
- [x] Regenerate current scene with outdoor table remnants disabled; fresh lighting/reflections; desktop player regression.
- [x] Build existing saved scene without regeneration; provide three launchers and one portable snapshot.
- [ ] Retest official Meta controller hands, smooth turning, headset-following belt, raised/adjustable height, grip anchors, camera placement and padded tool selection. Finish human Quest 3 acceptance: both eyes/hands, tracking recovery, movement comfort, wheel, holsters, camera, door pushes, hints and quit path.
- [ ] Headset visual audit: portable lamps, camera flash, contact shadows, glass/reflections, UV evidence and bilingual UI. Measure GPU time and tune budget.
- [ ] Run the broader legacy/gallery/new VR validators when they can be run without overwriting the user's active scene; report exact pass/fail counts. Desktop pose regression does not replace these or hardware testing.
- [ ] Rehearse presentation; then dependency-based cleanup/archive, retaining licences and metadata.
- [ ] Replace car only when Koda supplies the new model.

See VR_TESTING.md for the actual setup, controls, build behavior and connection troubleshooting.

- [ ] Accept authored Meta hand poses in the headset: finger curl, palm facing, tool/torch grip fit and shaded transparency. Animation deformation and strict build passed; human fit pending.

- [x] Repair hand mesh import/unit conversion and post-animation alignment; inspect rendered open/closed meshes from the actual runtime component.
- [ ] Retest repaired hands in headset; previous hand implementation was rejected by Koda.

- [x] Lower belt and holstered tool positions 12 cm.
- [x] Symbolicate native shader crash and enable shader lifetime mitigation; Koda confirmed next player stays running.
- [ ] Accept latest controller-frame mapping in the headset. Sideways fingers were corrected by mapping the anatomical forward axis to the controller forward axis; both editor axis checks pass.

- [ ] Accept 12-degree downward hand adjustment and dedicated white/UV flashlight grip in the headset; combined renders and sampled grip-clearance checks pass.

- [ ] Retest corrected flashlight-only palm orientation; original empty-hand models restored after misunderstanding. Build and grip checks pass.
