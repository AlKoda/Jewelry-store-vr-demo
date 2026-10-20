# October 20 presentation update

The existing saved night scene is updated in place. Interior lights stay off. No alternate Unity project or scene was created.

Implemented: true undersides on display furniture; smaller varied blood transfers; pull-handle fingerprints; safe pass-through tools with working ray selection and UV occlusion; solid placed lamps; generated Arabic brand/section plaques; exterior upper storeys, parapets, cornices, awnings, rails and gutters; leafy shader wind canopies; deeper terrace rows, geometry skyline and reduced fog; longer road surfaces and continuous playable street/pavement bounds. The supplied car uses the original FBX meshes and texture with uniform placement scale, parked parallel to the road.

Validation:

- Actual runtime white and UV light setup rendered against an opaque blocker, compared with shadows disabled. Both PC soft 2048 and simulated Quest-tier hard 1024 maps visibly darkened thousands of receiving-surface pixels.
- Six tool kinds checked through real CharacterController moves: small tools do not push/block; placed lamp blocks; held lamp is non-solid; pointer rays still select all six; a solid wall still blocks movement. Non-solid tools still occlude UV traces, while held tools do not.
- Scene checks cover original imported car mesh references/uniform scale, six prints on pull handles, small floor-transfer sizes, inactive old street barriers, architecture/canopy/skyline presence, continuous kerb bounds and interior fixtures remaining off.
- Display base repair closed 268 open small planar rims using the existing material with real UVs and outward normals. Source builders also close future bases.
- GPU lighting bake and two-pass reflection capture refreshed the changed scene. Seven lightmaps (largest 1024), 676 light probes, eleven box-projected reflection captures.
- Strict Windows saved-scene player build and hand/icon/holster validation run without regenerating the store layout. Final player regression outcome is recorded in RESULTS.txt.

The first render test exposed an editor-only cleanup error from destroying a torch light before its camera callback/model; cleanup and stale-light guards were repaired and the final checks rerun. Intermediate bake/preview iterations are not acceptance evidence.

Limits: Quest-tier shadow rendering here runs on the PC GPU. No Android APK, standalone frame-rate claim or headset acceptance is implied. Unity Android Build Support is not installed on this PC; Claude's standalone build path is retained. Headset checks remain in ROADMAP.md.

Art/provenance: built-in image_gen, using the imagegen skill. Final saved sign files and exact prompts are listed in Prototype/ThirdParty/ArabicSceneSigns/DESIGNED_BRANDING.md. Supplied FBX SHA-256 is BEF2445C622AC69BAFB734D90BE5F7173EC9046F03790B48DD18D6761CE1445A; no source mesh bytes changed. Stock reference screenshots informed facade proportions only and are not game textures.
