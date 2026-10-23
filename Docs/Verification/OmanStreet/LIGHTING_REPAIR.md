# Lighting and hovering decals — 21 October 2026

User screenshots reproduced in the saved scene. With torch off and aggregate normal strength zero, the segment seam remained. With lightmaps disabled, it disappeared. Diagnostic images: UnityProject_Demo/Builds/lighting-before-<mode>-<view>.png; modes 0 original, 1 no normal strength, 2 no lightmaps, 3 torch enabled. Inspection report: lighting-surface-inspect.txt in the same Builds folder.

## Final changes

- Separate 10 m asphalt box tops replaced by one continuous 140 x 17 m quad with normalized, continuous UV2 and no internal chart boundaries. Road scaleInLightmap .70, paving/kerbs .45, versus .06 previously. Existing material/paint/cracks preserved. Colliders and walkable bounds unchanged.
- Increased baked direct/indirect/environment samples from 32/256/128 to 128/512/256, padding 2 to 4. Baked soft practical-source shadows retained; white and UV real-time torch setup unchanged. No new volumetric buffers, realtime lamps or post-processing.
- Three active leaf decals at pavement height were 12 cm above the lower asphalt. Disabled all legacy DryLeavesAtTree instances and broadened cleanup for future repair/rebuild entry points.
- Rejected a compressed UV chart trial after it produced visible bands. Delivered the full normalized chart, checked in the final actual renders.

## Verification

LightingSurfaceChecks verifies continuous two-triangle road chart, no segmented asphalt renderers, required density and no active legacy leaf decals. Protected interior and car transform signatures unchanged. Geometry is 54,814 exterior triangles, reduced from 54,980.

Eight GRAZING_TORCH_SHADOW_PASS cases: white/UV, PC/simulated standalone tier, light at 1.5 m or .25 m height, rendered over the actual road mesh/material. Each compared shadowed/unshadowed frames without occluders; zero lit receiver pixels darkened above .035 threshold. Lit pixels per configuration: 31,879 / 42,712 / 1,750 / 7,927, repeated for both tiers. Four TORCH_RENDERED_SHADOW_PASS tests confirm an occluder does cast a visible shadow; PC maps 2048, simulated Quest maps 1024.

Fresh bake: 13.9 seconds; three maps, maximum 1024; 676 probes. Fourteen reflection captures refreshed. Shop model, architectural glass, car-source/GPU-normal, references/normals, VR visual and strict Windows build checks pass. LIGHTING_SURFACE_DELIVERY_PASS in Builds/lighting-delivery.log. Current player rebuilt at 15:04 local and reopened in keyboard fullscreen.

Final actual views: UnityProject_Demo/Builds/lighting-after-0-0.png (no torch at former central seam), -0-1.png (near parking under street lamp), -3-0.png and -3-1.png (torch), plus low road/pavement and glass views in road-sidewalk-* and architectural-glass-lighting-*.

This is a targeted lighting defect repair. No claim of perfect shadows everywhere, real Quest FPS, volumetric occlusion or native headset acceptance. Hardware retest remains open. Official ZIP SHA256 stays 63E75AC1EAFFD20B8ABA4F52FCC93075DEB369A45E467A43F4698EDC63F3A072, HEAD stays 1005d47765abefd595dededebf57895ef6c6a999; no upload or promotion.
