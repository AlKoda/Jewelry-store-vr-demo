# Crowd geometry rebuild — October 26, 2026

Koda clarified that the people needed better models, following the preceding world/material pass. This batch changes actual meshes, rather than relying on lighting or surface adjustments. Base: c48b0ac. Existing scene, crowd placement, count, materials, motion and interaction systems retained.

## Geometry

- All five actors: locally subdivided face/ear/hand topology, with bounded Loop-style smoothing (maximum 1.8 mm displacement). Original licensed UVs, facial identities, skeletons and animation bindings retained. Three new *_Detailed.fbx derivatives preserve their original Animated inputs.
- Both Omani actors: upper garment fitted to the original anatomical shoulder/arm surface; jacket flaps are excluded from the continuous front. Relaxed cloth joins, unequal lengthwise folds, elbow/cuff compression, turned hems/cuff bands and a chest-fitted cotton furakha. Covered original cuffs and scalp removed. Fitted kumma has native stitched motifs; mussar wraps follow its volume. The textured neck extends inside the garment with torso/neck skin weights. Detached trim at the neckline is removed.
- Two exclusive skinned LODs on the same rig per actor, at 0.21 screen height. Total near 123,296 triangles; distance 36,985 (previous single level 44,527). Guards: 125,000 near / 55,000 distance. No extra people, bones, lights, rigidbodies or cloth simulation. These are geometry budgets, not measured frame time.

## Local Unity verification

Unity 6000.6.2f1, Built-in. PresentationPolish.CrowdModelDeliveryRun completed with CROWD_MODEL_DELIVERY_PASS and EXISTING_SCENE_DELIVERY_PASS in Builds/crowd-model-delivery.log.

- Both mesh levels, both clips and all five actors at nine phases: valid skin bounds, bone references and stretched-edge checks.
- Existing loop seams, bounded walking/watch/return, visitor yielding, alternating conversation and additive-head/culling regressions passed.
- Actual rendered near/far silhouette differences: Walker 0.10%, Talker 0.12%, Listener 0.09%, Phone 0.22%, Observer 0.11%. Both levels render and retain the full-body outline.
- Material/shader/mesh reference review, protected patrol-car source/geometry/LOD checks and presentation/torch-shadow/VR visual checks passed.
- Fresh lighting bake (three lightmaps, 800 probes), reflection refresh and strict saved-scene Windows/PCVR build completed. The lighter/shop layout from the preceding pass remains in the saved scene.
- Actual neutral-light Unity model views: UnityProject_Demo/Builds/CrowdModels/{before,after}. View 0 is full body at the near level, 1–3 inspect the head from different directions, view 4 forces the distance level. Street views are Builds/crowd-review-*.png under the Unity project. Images stay local.

Generators: PeopleModels/Source~/refine_crowd_meshes.py and dress_omani.py. Source hashes and original Microsoft MIT attribution remain with the asset pack. No new downloaded skin textures or scanned-human assets were introduced.

Canonical snapshot refreshed (154.6 MB): CRC, all 413 code/shader/metadata files and exact saved-scene parity passed. All three new detailed FBXs match the local project and provenance hashes, and their GUID metadata is present in the archive. Both Omani FBXs and both mirrored generators also match. The large PeopleModels payload is mirrored by the existing launchers from the checkout.

Human Quest Link appearance, movement and LOD acceptance remain pending. No standalone APK/frame-time measurement or player launch in this pass. This refines the current background characters; it does not turn them into scanned cinematic humans.
