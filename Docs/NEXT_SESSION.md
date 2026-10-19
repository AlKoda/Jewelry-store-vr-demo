# Next session — user priorities, October 13, 2026

Status: notes only. User explicitly asked to defer implementation until they return with enough credit. Do not start work or schedule automatic continuation from this note.

## Revised goal
Make the store as close to photorealistic as practical without substantially degrading performance: better lighting, materials, models and environmental detail. This is a demonstration for the supervisor overseeing a three-person experiment in AI capabilities and effective AI use; the user wants a visually impressive result. Measure performance as visual quality improves rather than treating triangle counts alone as proof of performance. Exact frame-rate / hardware targets have not yet been agreed.

This expands the earlier interior-only scope to include supporting surroundings, car-light tuning, skybox, distant fog and atmosphere. The supplied police-car model remains a candidate; the user has not specifically requested replacing the existing car yet.

## Requested work, in the user's order
- [ ] Fix UI alignment. User reports that the current UI is not aligned correctly. Reproduce in the actual player; inspect layout, scaling, anchoring and different window sizes.
- [ ] Investigate why the DO NOT CROSS tape glows and correct its unnatural appearance. Cause has not been diagnosed; do not claim emission or a shader is responsible until inspected.
- [ ] Make both flashlights toggle on/off when clicked, with an audible switch sound on each toggle. Interpret as the regular flashlight and UV flashlight in the current tool set; verify how click interaction fits existing controls during implementation.
- [ ] Dim the car's lights and shift them to a yellowish white. They should illuminate enough of the store for navigation while leaving the handheld flashlight meaningfully useful. Evaluate lights together in the actual scene.
- [ ] Improve surrounding-area surfaces using textures/pictures and a suitable skybox. Add distant fog to conceal map edges and create an eerie nighttime atmosphere.
- [ ] Push the store toward photorealism with better models, materials, lighting, glass, surface detail and coherent scale. Balance visible improvement against measured frame time, memory and responsiveness; do not assume costly effects are required.
- [ ] Add footsteps and nighttime ambience. User delegates finding suitable sounds to the assistant; source usable/licensed audio, retain attribution, and tune levels so the ambience supports the scene. Also source the flashlight switch sounds.

## Suggested next-session sequence
First reproduce UI/tape issues and record a visual/performance baseline from the current tested player. Then implement click toggles and sound, balance headlights versus flashlights, and develop the store's realism and surrounding atmosphere with before/after captures and measured performance. Preserve the working build as a reference. This sequence is a planning suggestion, not work performed.

## Starting point
See DISPLAY_GLASS_CHECKPOINT.md for source paths, local build, archive, validation and limitations. The latest tested local baked build passed 159 night checks plus three player scene captures. Source changes are still local and unpushed. No implementation, asset downloads, builds or audio acquisition were done for this new request.
