# Desktop, Quest 3 and editable Unity delivery

One shared source project is recommended. DemoModeSwitch already selects desktop or VR from XRSettings.isDeviceActive and supports F9 override. It enables/disables input components, colliders and VR objects. This does not install or start an XR loader.

## Three final entry points
1. **Play-Keyboard.cmd** — launch a tested Windows desktop player with an explicit desktop override.
2. **Play-VR.cmd** — launch a tested Windows OpenXR PCVR player for Quest Link/Air Link, with readable missing-runtime feedback and a headset-usable scene entry.
3. **Open-In-Unity.cmd** — open a self-contained editable Unity project at the user-owned night scene, without automatic source mirroring or store regeneration.

These entry points are a delivery specification, not existing tested binaries. Current Start-Demo.cmd and Open-Unity-Glass.cmd remain the existing utilities. Desktop/VR launch argument parsing and XR initialization policy still need implementation and PC validation. F9 switches rig controls; it is not evidence that XR runtime startup/shutdown is seamless.

## Shared binary or separate builds?
Prefer one XR-enabled Windows player supporting both modes once it is tested. If forced-desktop startup or XR runtime availability causes issues, ship separate Desktop and PCVR build profiles from the SAME source/project and scenes. Do not maintain diverging copies of game logic.
- Desktop output: Builds/Desktop with EXE, its _Data folder, UnityPlayer.dll and required runtime/license files.
- PCVR output if separate: Builds/Quest3-PCVR with the same complete player folder.
- Editable output: UnityProject_Editable with Assets (including .meta), Packages/manifest.json + lock file, ProjectSettings and baked data. Open that folder through Unity Hub using the recorded Unity version. A .unity file alone is a scene, not the whole project.
- Initial target is Quest 3 connected to the Windows PC. An Android standalone APK is a separate target with different memory/render budgets; it is not promised by this delivery plan.

## Protect manual editing
DemoBuild.BuildLatestReport regenerates JewelryStoreNight through NightStoreBuilder.BuildScene. Tools/sync-project.cmd mirrors source directories with /MIR. Open-Unity-Glass.cmd performs that sync before opening.
- Create the editable project separately after integration.
- Save a user-owned scene, e.g. JewelryStoreNight_Editable.unity, with manual positions preserved.
- Add a build-existing-scene command/profile which NEVER calls NightStoreBuilder.BuildScene.
- Keep editable-only scripts/assets outside mirrored source folders and commit their .meta files.
- Large moved fixtures require rebaking; baked shadows stay at their old locations until then. Split fixed structure from interactive props where useful.
- Use regeneration/validator workspaces separately from the user-editable project. Test a distinctive manual move survives save/reopen and an existing-scene build.

## PC delivery gate
Resolve XR packages/runtime and input profiles; compile C# and shaders; run existing validators; inspect both-eye hands and shadows; test keyboard->VR->keyboard, disconnected headset, tracking loss/recovery, photos and reset; build complete player folders; test from a fresh extracted folder; record FPS/frame time and build/commit stamp.

## Cleanup later
Keep main as source of truth. After verification, use an archive branch/tag for historical milestones and a release branch/package containing the tested deliverables. Identify dependencies before removing packs. Never delete license files or GUID metadata for retained assets. No cleanup/deletion has been performed by the offline review.

## Coordination with Claude
Docs/REORGANIZATION_PLAN.md (ee794d0) proposes the same three entry points. Use its names, but defer its physical moves until the Quest 3 test. Correct its .meta migration assumptions before execution: /XF *.meta does not carry old GUID files along with moved scripts. Proposed -demo-mode desktop/vr arguments are not implemented yet and must include safe missing-runtime behavior, not merely force VR controls with no headset.
