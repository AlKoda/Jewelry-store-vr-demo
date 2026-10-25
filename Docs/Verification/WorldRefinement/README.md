# World refinement — October 26, 2026

Unity 6000.6.2f1, Built-in pipeline, existing JewelryStoreNight scene. Source checkpoint: main 1efd731; local scene/garment checkpoint ../Backups/Before-World-Refinement-20261026. This pass does not regenerate fieldwork or alter forensic/controller interactions.

## Changes

- Two original Omani garments: continuous shoulders/sleeves, uneven folds, relaxed mesh, barycentric UV/weight transfer and smoothed four-bone skinning. Retained MIT Rocketbox faces, rigs, feet and licensed motion. Five people, 44,527 triangles (previously 38,567), below the existing 55,000 cap.
- Reduced cloth/skin normal strength, more natural ivory/sand cloth colours, conversation playback 0.86. Head aim stays relative to the authored animation, with frame-time smoothing and 28° cap (12° on the phone). It no longer adds the same turn indefinitely when animation is culled.
- Twenty-three environment finishes: lower grain/cloth relief, satin case wood and hardware, calmer plaster/shutters/painted street steel, floor and ceiling-diffuser response. SatinWood retains existing colour/normal maps and lightmap/shadow support; one additional biased colour sample softens exaggerated photographic grain contrast.
- Unequal lower-sill roughness on the current patrol car, preserving source colour bytes, body mesh, UVs, corner normals, LODs, optics and collision.
- The open lighter is laid on its broad side, bottom 1 mm above the floor, height below 4 cm. E25 remains bound to its specimen. Eight loose display insert groups have small varied offsets/yaws, checked within their cabinet footprints. Existing scattered jewellery/glass and the outdoor staging layout remain.

## Completed local checks

`PresentationPolish.WorldRefinementDeliveryRun` completed with `WORLD_REFINEMENT_DELIVERY_PASS` and `EXISTING_SCENE_DELIVERY_PASS` in Builds/world-refinement-delivery.log.

- All five actors, Motion/Idle at nine phases each: valid skin bounds and stretched-edge checks. Walking, watch pauses, route return, visitor yielding and alternating conversation passed.
- Head regression: 1,200 stopped-animation updates at 60 Hz followed by 600 at 30 Hz remain bounded and stable; a fresh sampled animation pose remains the baseline.
- Valid crowd material/bone references; no new crowd lights or rigidbodies. Patrol source-texture SHA, geometry, LODs, optics fitting, probe/collision checks passed.
- Shop, insert bounds, lighter floor contact/height, map structure and missing mesh/shader/script checks passed. Exact duplicate candidate count remains 11, unchanged from the audit.
- Actual road material visibility passed: parking mask 4,611 changed pixels; crack mask 1,726. Road/paving shader and import rules retained.
- Normal and UV torch rendered shadows passed for both graphics tiers. VR visual checks passed for both hands, tool clearance, wheel icons and belt anchors.
- Fresh bake: three lightmaps, largest 1,024 px; 800 light probes. Existing reflection captures refreshed. Strict Windows/PCVR build completed at Builds/Windows/JewelryStoreDemo.exe.

Actual Unity inspection captures are local only at UnityProject_Demo/Builds/WorldRefinement/{before,after}; lighter close-up is after/dropped-lighter.png. Car views are in Builds/PatrolReview/world-after-*.png under the Unity project. No generated concept imagery is presented as a build screenshot.

Canonical downloadable snapshot refreshed to 154.6 MB. CRC verification passed; all 413 source code/shader/metadata files exactly match their local Unity copies and archive entries. The archived saved scene is byte-identical to the scene used for the build. Both crowd FBXs match the local project, retained GUID metadata in the archive and refreshed provenance hashes. After regenerating fieldwork, run WorldRefinementDeliveryRun again to retain the lighter's authored resting orientation.

## Remaining review

Human headset acceptance and GPU/CPU frame-time measurement are pending. These remain economical background characters, rather than scanned cinematic humans. No standalone APK was built or performance measured in this pass. The player remains closed.
