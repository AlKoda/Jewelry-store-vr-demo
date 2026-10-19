# Roadmap: open work

Updated October 19, 2026. Only open items are listed here. Tick an item and move it to the next History file when it is done. The full October 8–19 roadmap, with every finished item, is in [History/ROADMAP-2026-10-08_to_19.md](History/ROADMAP-2026-10-08_to_19.md). Deadline: **October 27, 2026**.

## 1. On the PC first (blocks everything else)
- [ ] Open with `Open-In-Unity.cmd`; the Console must be clean (C# and shaders, including GhostHand). Report any error with its file and line.
- [ ] Run `NightStoreValidation.RunNightQuick`, `StoreKitValidation.RunGallery` and `ShopCityValidation.RunGlass`, then `RunNight` (bake). Report the totals, the first FAIL with the Info lines above it, the `Budget:` lines and the bake time ([PC_VERIFICATION.md](PC_VERIFICATION.md)).
- [ ] Set `NightStoreValidation.MaterialBudget` from the logged `Budget: materials=` value plus 8.
- [ ] Resolve Package Manager access; install and configure OpenXR + XR Plug-in Management for Windows with the Meta runtime and Quest 3 Touch Plus profiles. Record the package versions. (ChatGPT review P0)

## 2. Quest 3 test (Koda)
- [ ] Movement: smooth walking speed (1.4 m/s), teleport via F8, snap turn, comfort vignette strength (F7).
- [ ] Hands: alignment with the real hands, opacity (F3/F4), finger response, both eyes, tracking loss and recovery.
- [ ] Tools: free rotation in the hand and upright landing, tool wheel (left trigger; right pointer or left stick), distance grab, holster positions standing/sitting/crouching.
- [ ] Camera: zoom rate, flash, photo review, photos saved to the folder.
- [ ] Torches: beam aim along the controller (Quest controllers point slightly up).
- [ ] Lighting and graphics: baked shadows, no light through walls, shimmer, z-fighting, frame time on a baked build.

## 3. Fixes already known (ChatGPT review, October 19)
- [ ] GhostHand.shader: replace the fixed-function depth pass with a stereo-safe vertex/fragment pass (P0).
- [ ] Protect manual edits: a user-owned scene and a build route that never regenerates it (P0, [DELIVERY_PLAN.md](DELIVERY_PLAN.md)).
- [ ] Gate controller poses and actions on tracking state; cancel wheel and aim on loss (P1).
- [ ] Holsters: decide whether a hand can hold a torch and a tool at once (P1).
- [ ] Portable lamps without the shadow slot can light through walls (P1).
- [ ] A headset-usable start path past the desktop start menu (P1).

## 4. Content
- [ ] Wire the six newer packs selectively (import rules, credits, checks): AmbientProps, AmbientProps2, CutoutDecals, RealFoley (replace the synthetic footsteps using its README table), RealFoley2, RealismTextures (needs an explicit map lookup in DemoAssetLibrary).
- [ ] Koda's decisions in [DECISIONS.md](DECISIONS.md): country and police service, entry route, UV meaning, one real measurement, evidence items.

## 5. Delivery
- [ ] Tested `Play-Keyboard` and `Play-VR` builds; an editable Unity project for Koda; copy the tested `Builds\Windows` folder as a backup.
- [ ] Rehearsal checklist in [REHEARSAL.md](REHEARSAL.md).
