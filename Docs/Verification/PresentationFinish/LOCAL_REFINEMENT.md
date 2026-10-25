# Presentation polish — October 25, 2026

## Delivered in the saved scene and Windows/PCVR player

- Road cracks, paving fractures and parking bays restored. Root cause: generic PackImport version 2 added OmanExterior without recognizing its older file names, imported data masks as sRGB and height-normal images as colour. Mask gamma suppressed the paint abrasion threshold; normal interpretation caused harsh ground grain. Version 3 recognizes these maps, keeps paint at 2048 on Windows, linear mask channels, generated normals, mipmaps and anisotropic filtering. Existing GUIDs preserved.
- Ground shader reduces aggregate contrast and distant normal shimmer, retaining broad weathering. Parking paint and cracks share one opaque surface; fissures render after paint. No floating overlay planes were introduced.
- Five field-tool models rebuilt: flat tapered serrated forceps, shaped cotton swab, ferruled powder brush with fibre bundles, magnetic wand with plunger/rings and powder head, layered hinge-lift backing card with PET cover/tab. Existing grips, working-tip positions and tool methods retained. Other forensic materials have gentler normal relief/smoothness; paper brightness reduced.
- Narrow tray slots moved 3 cm away from the divider. Actual mesh vertices are checked against the tray walls, and pickup boxes are checked for overlap. The case carry handle folds outside the front. NearKit now selects the nearest reachable handle rather than PhysX overlap order, preventing the cart bar from taking a case grab.
- Editor-generated latent marks are transient and omitted from saved scenes, preventing duplicate runtime marks after validation.

## Verification

Unity 6000.6.2f1, existing JewelryStoreNight scene. PresentationPolish.PresentationFinishRun passed full FieldworkChecks/ForensicSandboxChecks, saved/reopened scene checks and strict Windows build. Final log: repository Builds/presentation-finish.log. Before/after images and material reports: UnityProject_Demo/Builds/PresentationFinish. Preview lighting is temporary and removed; no new runtime lights, cloth physics or rigidbodies. Fieldwork geometry: 39,695 triangles in checks (not a headset timing result).

Actual imported road material tested on GPU with each real map disabled in turn: parking paint affected 4,611 pixels and fracture maps 1,726 pixels at 384x256, each required at least 120. Synthetic channel test also verifies that every road-crack channel cuts through parking paint. Inspection confirmed restored bays, varied cracks, sidewalk fractures, whole tool tips and the unobstructed tray. Saved scene contains zero test-generated LatentMark children. Car transform/mesh/material signature remained unchanged.

No live headset acceptance or frame-time measurement in this batch. No player launch. Local backup: ../Backups/Before-Presentation-Polish-20261025. Source authoring hooks preserve refined tools and handle/slot placement on future fieldwork regeneration. Texture metadata is carried in the canonical snapshot and source pack with stable GUIDs.

Final source reconciliation: pulled the concurrent Menu+X reset and car-reference documentation without overwriting them. Rebuilt the existing Windows scene with that combined source (Builds/presentation-final-build.log: EXISTING_SCENE_DELIVERY_PASS). Snapshot integrity and scene/source parity are checked before publication.
