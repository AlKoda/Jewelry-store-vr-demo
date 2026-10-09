# Assistant handoff

## Current state

Verified: [Batch 04](ENVIRONMENT_BATCH_04.md). Unity 6000.6.2f1, Built-in pipeline. The refined shop/city includes inside-only desktop walking, colored tools, current-view photography and more scene detail. **33 Editor Play mode assertions passed** on October 8, 2026 with the batch-04 source.

Unverified since then (batches 05 and 06 below): pointer-based tool handling replaced DesktopToolPlacement, the presenter panel was rewritten, and toolkit-independent XR adapters, a status board and a build script were added. Scenes in the snapshot predate these changes. **Run `ShopCityValidation.RunExpanded`** (isolated demo project, -executeMethod) to regenerate JewelryStoreExpanded.unity, captures and results, then check by hand: marker digits visible, tools follow the pointer onto floor and display tops, P photographs the view, F then P photographs through the held camera, movement smooth. Expected: 45 assertions.

Open Assets/CrimeSceneDemo/Scenes/JewelryStoreExpanded.unity. Download Downloads/JewelryStore_Expanded_Unity6000.6.2f1.zip for the full generated project; Prototype contains the matching editable source. Earlier snapshots are historical.

PC root: C:\Users\MOBPC\Documents\Codex\Jewelry-store-vr-demo\Jewelry-store-vr-demo-main

## Goal

Instructor-led VR jewelry robbery demonstration by October 27, 2026. No score, timer or forced procedure. Keep the user inside the shop, with a visible lightweight street outside. Work in complete batches and save progress to GitHub.

## Source map

Runtime: ToolStation, DeployedTool, SceneTape, DemoToolGeometry (tools); EvidenceCamera (photos, HoldBy/ReturnToRack, CaptureFromView); DemoSession, ResettableSceneObject (reset; Resetting fires before clearing); InteriorBounds (shared interior regions); ToolHolder → DesktopInteractor (mouse/keyboard) and HandInteractor (tracked hands); XRLocomotion (teleport, snap turn); ShopWalkController (desktop walking); DemoDesktopPanel (presenter panel); DemoStatusBoard (wall text).

Editor: JewelryStoreBuilder, ShopCityRefinement, ShopPresentationExpansion (geometry); DemoToolsBuilder (tool station, desktop player, status board); XRRigBuilder (rig skeleton); DemoValidation (shared checks) with JewelrySceneValidation.Run, ShopCityValidation.Run and ShopCityValidation.RunExpanded; DemoBuild (Windows player).

## Next technical dependency

Unity Package Manager fails to start normally; the -noUpm launcher permits package-free work. SETUP.md lists things to try. Resolve this before installing OpenXR/XR Interaction Toolkit. The PC reports RTX 5060 Ti and approximately 16 GB RAM.

## Pending

XR package install, Quest connection, binding controller actions to HandInteractor/XRLocomotion (see CONTROLS_AND_INTEGRATION.md), headset HUD, hands-on keyboard/mouse usability, headset performance and standalone build validation (Build-Windows.cmd is ready but unrun). Interior colliders and InteriorBounds constrain the desktop controller and teleport targets, not physical headset tracking.

## Collaboration rules

Read the brief, roadmap and latest batch report. Preserve .meta files. Update source and the complete snapshot together; exclude Library/caches. Pull current GitHub files before editing. Keep deployment ownership during XR grabbing and release held objects before reset (ToolHolder does this on DemoSession.Resetting). Record external asset licenses. Repeat tests when changes or unresolved concerns justify them.

## Batch 06 — XR-ready adapters, status board, build script (October 9, 2026, unverified)

ToolHolder is the shared base for DesktopInteractor and HandInteractor (grab/carry/drop/trigger for tracked hands, no toolkit API). XRLocomotion adds bounds-checked teleport and snap turn, on the desktop player now and the XR rig later. EvidenceCamera.HoldBy/ReturnToRack pass the camera between holders and return it on reset. DemoStatusBoard shows tool count, next marker, tape state and the last photo above the rack. DemoBuild plus Build-Windows.cmd/Run-Demo.cmd produce the standalone Windows demo. XRRigBuilder creates a rig skeleton. SETUP.md has Package Manager troubleshooting and the XR package plan.

## Batch 05 — desktop interaction (October 8, 2026, unverified)

InteriorBounds (Contains/Clamp) replaced hard-coded limits. DesktopInteractor: left click picks up and places tools on pointed surfaces, 1/2/3 spawn into the hand, Q/E rotate, Delete removes, T/X tape, F camera, P photograph. ShopWalkController: bounds-driven, sprint, Looking state. DemoDesktopPanel: status, spawn/photo/reset buttons and the controls guide; Tab toggles. DemoValidation holds what the validators shared; DemoToolsBuilder.CreateDesktopPlayer replaced ShopCityRefinement.CreateWalker. Merged with batch 04: DesktopToolPlacement and ShopPresentationValidation were folded into DesktopInteractor and ShopCityValidation.RunExpanded; CaptureFromView was kept.

Earlier fixes the same day: marker labels had no font; two CharacterController.Move calls per frame made fall speed accumulate; build settings pointed at the old scene.
