# Environment batch 01 — untextured store

## Delivered source

Prototype/Editor/JewelryStoreBuilder.cs generates an editable store directly in Unity. No purchased assets, Blender installation, textures, custom shaders, XR packages or runtime generator are required. Unity's default material is used only to make geometry visible; every visible object still requires a shader internally.

This is a blockout with believable dimensions, not a surveyed real store or final forensic reconstruction. Source is prepared; compilation, scene generation and headset validation are pending.

## Installation when home

1. Create or open the Unity project.
2. Copy JewelryStoreBuilder.cs into Assets/CrimeSceneDemo/Editor/ (the Editor folder matters).
3. Wait for compilation; resolve any errors before proceeding.
4. Open a scene and select Crime Scene Demo → Create Store Blockout.
5. Save the scene under Assets/CrimeSceneDemo/Scenes/.
6. Add appropriate preview lighting and a camera, or integrate the XR rig in the next batch.

The generator adds to the current scene, without replacing or saving that scene. It refuses to create a duplicate named root. Undo removes the generated root; the reusable shard mesh asset remains under Assets/CrimeSceneDemo/Generated/. Retain its .meta file.

## Included

- 10 × 8 m showroom and 3 × 3 m rear room, approximately 3 m high.
- Front pavement, storefront opening and 1.6 m entrance.
- Rear doorway 1.4 m wide and 2.3 m high.
- Two central islands with approximately 2.2 m clear space between them.
- Two wall display cases, service counter and tool-rack placeholder.
- Jewelry pads, remaining jewelry placeholders and empty pads.
- Hollow safe with shelf and open door.
- Deterministic window and display debris.
- Geometric shoe-print placeholder.
- Cylinder, torch handle/nozzle and segmented hose placeholders.
- Named groups and reference positions for later integration.
- Architecture/furniture colliders; small jewelry, tread and debris have no collisions.

## Deferred

Transparent glazing, detailed fracture edges, jewelry models, authentic shoe tread, torch detail, signs, textures, custom materials, lighting setup, XR rig, tool interactions and reset behavior.

The missing window panel represents broken glazing in the blockout. Display cases use open frames so opaque placeholder glass does not conceal the contents. The shoe print is raised geometry until a decal or mesh treatment is selected.

## Performance approach

Primitive colliders and static scenery first. No debris physics. Do not merge the whole store into one mesh: preserve selectable objects and movable tool boundaries. Consider static batching or combining compatible static meshes only after selecting materials and checking performance. Final VR performance is unmeasured.

## First validation session

Check compilation and scene generation, room/doorway scale, travel routes, safe-door obstruction, visibility of the evidence, collision boundaries, material compatibility and Quest performance. No testing has been performed remotely.
