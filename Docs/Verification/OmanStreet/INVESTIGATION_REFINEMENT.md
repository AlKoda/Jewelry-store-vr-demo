# Local investigation refinement — October 21, 2026

Car work paused by Koda. Existing supplied FBX, plain gray face-shaded material, placement and collision remain. CAR_FROZEN_CHECK.json compares all 21 PoliceCar transforms and every mesh/material reference with the pre-work checkpoint: identical. Native livery drafts and rejected extra sedan retained unused. No GitHub commit/push or official ZIP change.

Implemented in the existing scene:

- Glass: clear surfaces alpha .055, debris faces .045, fresh cut edges .16, restrained grazing absorption and surface film. Same geometry and rendering approach; no screen grab, extra lights or per-shard physics.
- Blood: corrected normalized edge interpolation in DropMask; actual saved alpha maximum .831 versus .067 previously. Six irregular small transfers remain within 5.4 cm mask quads; corresponding UV masks use the same repaired shape.
- Other cues: three incomplete dust impressions, tiny blue textile fibres and an open bent jewellery clasp. No automatic evidence scoring or invented forensic conclusions.
- Tool placement: preserve held grip/orientation, then translate onto support at release. Readable mesh support points or transformed local bounds handle headset imports without requiring runtime mesh reads. No rigidbody throwing or changes to accepted hand/camera grips.
- Two existing MIT-licensed photo-textured figures on far pavement at X +/-32.5, beyond walkable X +/-29. Existing LOD where available, probe lighting, no extra lights/colliders. Evaluated extra car failed visual selection and was removed.

Validation:

- InvestigationRefinementRun: targeted scene edit; 38.2 s bake, three lightmaps up to 1024 px, 676 probes and 14 refreshed reflection captures; actual entrance, piles, floor evidence and observer views.
- InvestigationDeliveryRun: strict Windows existing-scene build PASS, INVESTIGATION_DELIVERY_PASS in Builds/investigation-delivery.log.
- Keyboard/VR grip-offset pickup, floor seating and .6 m counter seating PASS; tool/player collision safety and UV occlusion/wall tests PASS for all six tool kinds.
- Actual white/UV shadow render checks PASS for desktop and simulated headset quality tiers. These do not certify hardware performance.
- Evidence render on/off comparison: 1,391 changed pixels at .015 threshold; final mask alpha .8313726. Build-time repeat also passes.
- Glass geometry, street constraints, original FBX/GPU-normal checks and VR visual validation PASS. Reference scan: 653 active renderer components, 1,221 material slots, zero missing references/meshes/shaders/scripts, zero invalid lit normals and zero exact duplicate renderer candidates. These are component counts, not GPU draw calls.

Actual images: UnityProject_Demo/Builds/investigation-*.png, blood-visibility.png and architectural-glass-visible-*.png. They are real Unity camera renders, not generated concepts. Detailed close-up uses an inspection light; gameplay views use the actual torch/practical lighting.

Source changes: ToolHolder, DisplayGlass, PresentationPolish.DropMask, ShopModelReview support checks, ArchitecturalGlassReview defaults/checks and InvestigationRefinement authoring/review tools. Claude's Android hooks and accepted hands/camera/belt unchanged.

Hardware Quest 3 acceptance and frame timing remain pending. Local preview only. Official ZIP SHA256 remains 63E75AC1EAFFD20B8ABA4F52FCC93075DEB369A45E467A43F4698EDC63F3A072; Git HEAD remains 1005d47765abefd595dededebf57895ef6c6a999. Checkpoint: ../Backups/Before-Livery-Evidence-20261021.
