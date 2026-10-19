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

First headset feedback confirmed scene visibility but identified flipped hands, blank wheel icons and excessive labels. The latest rebuilt player corrects hand mirroring/orientation, supplies missing panel UVs, hides detailed hints by default (Menu toggles), and stabilizes/proximity-gates holsters. The actual rendered icon texture/stable-anchor validator passed; human retest remains required.
