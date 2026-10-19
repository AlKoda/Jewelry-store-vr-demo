# Current project handoff

Updated October 19, 2026. Read AGENTS.md, Docs/VR_TESTING.md and Docs/ROADMAP.md.

One authoritative source tree: Prototype, using Claude's organized runtime/editor folders. One current project: UnityProject_Demo. One saved night scene: JewelryStoreNight. One output: Builds/Windows/JewelryStoreDemo.exe. Three primary entry points: Play-Keyboard.cmd, Play-VR.cmd, Open-In-Unity.cmd. Start-Latest/Start-Demo are compatibility aliases.

## Current verified state

Local repairs from October 18 preserved: neutral/broken glass, clean static reflection captures, bilingual UI fit, Arabic physical signs/tape, supplied car, quieter recorded sounds, staff furnishings and 44 authored UV cues. Outdoor glove box/case remnants are now inactive in the regenerated scene. Raw assets are retained, not bulk-enabled.

Claude's latest organization and VR features are integrated with local fixes: F6 comfort/F7 language, explicit stereo depth pass, one ghost hand per controller, tracking gates/neutral rearm, bounded soft lamp shadows and camera-flash shadows. Runtime and editor sources compile in Unity 6000.6.2f1 with resolved XR packages. Strict Windows build and existing-scene build passed. Desktop regression COMBINED_PLAYER_PASS. Actual Meta OpenXR startup reached VISIBLE/FOCUSED; human headset acceptance remains pending. No headset FPS certification.

Packages: OpenXR 1.16.1, XR Management 4.5.3, Input System 1.20.1. Package Manager works and must remain enabled. Runtime initializes on VR launch only. Use DX11/SPI/Oculus Touch profile; settings and package locks are inside the snapshot and pinned template. Quest3Setup.Run repairs editor XR settings if needed.

## Editing/building

Change code only under Prototype; sync retains authoritative code GUIDs. Edit object positions in the existing saved night scene through Open-In-Unity. Normal rebuild uses EditableDelivery.Run and never invokes NightStoreBuilder. Only explicit bake uses CombinedBuild.Run, regenerating layout and fresh GPU reflections. Do not regenerate manual edits unintentionally. No additional project/scene copies are required.

The single compact ZIP carries the current saved scene, bake data, XR configuration, package manifest/lock and source; large packs sync from the repository. Libraries/player folders/logs remain local generated output. GitHub is the portable source/configuration base.

## Evidence and remaining work

Docs/Verification/Quest3/runtime-summary.txt records sanitized test results. Build logs under Builds are local. Historical local checkpoints/reviews are preserved under Docs/History and cache; do not confuse their old source paths/status with this handoff. Headset controls/comfort/photos, both-eye rendering, controller reconnect and profiling still need Koda's acceptance. Cleanup and car replacement remain deferred.

Latest headset follow-up: official Meta rigged controller hand meshes, smooth right-stick turning, headset-following utility belt with visible holstered models, default +12 cm height (F11/F12 adjustment), individual grip anchors, closer camera and padded selection volumes/point-delete. Detailed labels default off and wheel UVs are fixed. Strict build and rendered-icon/smooth-belt/skinned-asset validation passed. Human hand orientation, grip fit and comfort remain unverified. See VR_TESTING.md for controls and latest runtime limitations.

Hand follow-up: Meta authored OpenXR animation controllers/clips/masks now replace guessed finger-axis rotations. Both left/right middle fingertips moved over 2.5 cm between open/closed poses in the Unity validator; strict player build passed. Hand shading adds directional surface contrast and tool/torch grips sit lower in the fingers. Actual headset grip fit remains pending.

Current hand repair: Meta FBX armature/mesh conversion is normalized without deforming the skeleton. The actual anatomical wrist-to-finger axis maps to the Unity XR controller forward axis, with mirrored palm directions/thumbs up. Copying Meta prefab grip offsets directly was rejected in headset (fingers sideways); that path has been replaced. Both hand forward-axis checks, open/closed skinned mesh checks and strict saved-scene build pass. Rendered hands were visually inspected; latest headset acceptance is pending.

The belt and holstered tools are 12 cm lower. A native startup crash was symbolicated to ShaderVariantMonitor::FrameFinished; keepLoadedShadersAlive is enabled in EditableDelivery. Koda confirmed the subsequent player stayed running. The .gitignore rule now includes Editor/Build source and metadata in fresh checkouts, fixing a source/snapshot discrepancy.

Latest fine adjustment: empty hands pitch 12 degrees down from the calibrated controller frame. White/UV torch pickup uses a separate cylindrical grip pose and matching lens/handle anchor, rather than placing a horizontal barrel through the default controller hand. Both sides pass sampled vertex/triangle-centroid clearance checks in normal and fully pressed input states (maximum measured overlap about 1.13 mm, under the 1.5 mm contact tolerance). Combined rendered previews were inspected; strict build passed. Human angle/grip acceptance remains pending.

Latest correction: Koda withdrew the earlier positive headset reply and requested only swapping the left/right grab models. Visual meshes, matching animations and their fitted anchors are exchanged; physical controller inputs/tool ownership and existing angles stay unchanged. Headset acceptance is pending.
Swapped-model saved-scene build and existing orientation/mesh/torch-clearance checks passed. No further angle tuning was performed.
