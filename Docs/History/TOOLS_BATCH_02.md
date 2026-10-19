# Batch 02 — tools, reset, camera and geometry

## Status

October 8 update: all source compiled on the user's PC under Unity 6000.6.2f1; generated scene and 13 core Play mode assertions passed. A project snapshot and real Unity captures are available. See VERIFICATION_2026-10-08.md. XR/controller and presentation UI checks remain pending.

## Import as a batch

1. Copy **all** Prototype/Scripts/*.cs into Assets/CrimeSceneDemo/Scripts/.
2. Copy **all** Prototype/Editor/*.cs into Assets/CrimeSceneDemo/Editor/.
3. Wait for Unity compilation.
4. In a new or suitable scene, run Crime Scene Demo → Create Store Blockout.
5. Run Crime Scene Demo → Create Tool Station.
6. Save the scene and commit generated prefab/mesh assets and .meta files.
7. Add a viewing camera and basic lighting for desktop inspection, or configure the XR rig when ready.
8. Enter Play mode to use the desktop panel.

For an earlier generated store, the new geometry requires regenerating its root. Save a backup scene first. The tool station can be generated on an existing blockout, but its newly added visual details will not appear automatically.

## Delivered components

| Component | Behavior |
|---|---|
| DeployedTool | Tool identity, readable marker numbers, direct placement and removal |
| ToolStation | Spawn cones/markers/posts; sequential marker numbers; connect selected posts |
| SceneTape | Ribbon follows both post anchors; removes itself when a post is removed |
| DemoSession | Clears placed tools and restores explicitly registered scene props |
| ResettableSceneObject | Records and restores pose, scale and active state |
| DemoToolGeometry | Untextured cone, double-sided numbered marker and post geometry |
| DemoToolsBuilder | Generates reusable prefabs, station, camera body and desktop panel |
| DemoDesktopPanel | Spawn, select, move, rotate, remove, connect, capture, folder access and reset |
| EvidenceCamera | PNG capture, sequential filenames, status/events and synthesized shutter sound |

The generated handheld camera is registered for pose reset. Photos and the photo counter are preserved. Scene evidence is static and needs no reset until made movable.

## Desktop operation

Spawn a tool; select it in the list. Move by 0.1 metre per button press, raise/lower or rotate 15 degrees. Spawned tools initially share the station's spawn point; move each out of the way.

For tape, spawn two posts, move them, select the first in the tool list and click Select post for tape. Select the second and repeat. Cancel clears the pending selection. Tape follows repositioning and has no collision.

The photo camera starts facing into the store from the tool station. Its pose can be edited in Unity or controlled through future grabbing integration. Take photograph saves a PNG; Open photo folder opens Windows Explorer. Shutter feedback occurs only after successful saving.

## VR integration remaining

Add XR grabbing to deployed prefab roots and the handheld camera. Roots already have kinematic Rigidbodies and child colliders. Use the installed XR toolkit's collider/rigidbody setup requirements. Wire tool menu buttons to ToolStation spawn methods. Wire post selection to SelectTapePost, removal to RemoveTool and camera activation to CapturePhoto.

Keep the dedicated photo camera disabled between captures, untagged, on the active render pipeline's normal independent-camera rendering path. If using URP, inspect its additional camera settings and use a Base camera rather than an unassigned Overlay camera. Exclude headset HUD layers from photos. Render-pipeline compatibility remains unverified.

The desktop OnGUI panel is not a headset HUD. Hide it with Visible=false for the final presentation if desired.

## Geometry additions

Window trim and pointed sill remnants, necklace busts and segmented jewelry rings, safe wheel/dial, shoe heel/toe tread and torch cylinder shoulder/valve/gauge. No textures, custom shaders, third-party models or forensic interpretations added.

## First grouped acceptance session

- Compile all scripts and generate/save/reopen the scene.
- Spawn multiple tool types; marker numbers increase without reuse after deletion.
- Move/rotate/remove tools and check label readability.
- Connect two posts; move either; remove one and confirm tape disappears.
- Cancel a pending connection and start another.
- Capture several readable photos; confirm numbered unique files and shutter feedback.
- Open the folder and inspect images after exiting the demo.
- Reset repeatedly: tools/tape clear, marker numbering restarts, camera returns, saved photos remain.
- Confirm photos show the dedicated camera view and no desktop panel.
- Add XR bindings and check grabbing, placement and frame timing in Quest.

## Limitations

No XR controller bindings or headset HUD yet. Cone uses a stepped taper; tool colors await materials. Tape has no printed warning text. Capture is synchronous after rendering and may briefly stall for PNG encoding. Generated scene still needs lighting and a view camera. No performance claims are made until headset testing.
