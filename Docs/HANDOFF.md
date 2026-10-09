# Assistant handoff

## Current state

All work now lands on `main` directly; there is no separate feature branch. Pull before editing.

Verified on the PC (October 9, 2026, Unity 6000.6.2f1, Built-in pipeline): the expanded shop, pointer placement, current-view photography and the **instructor review recorder** — 40 assertions, see [REVIEW_BATCH_05.md](REVIEW_BATCH_05.md). That run used the source *before* the batches listed below were merged in.

Unverified since then: the single Player rig for desktop and VR (DemoModeSwitch, F9), head and controller tracking through the built-in XR input API, ToolHolder/HandInteractor/XRLocomotion, the status board, DemoGeometry, the build script, and the removal of DesktopToolPlacement in favour of DesktopInteractor. **Run `ShopCityValidation.RunExpanded`** (isolated project, -executeMethod) to regenerate JewelryStoreExpanded.unity, captures and results. Expected: 66 assertions. Then check by hand: marker digits visible, click-to-pick-up and place, P photographs the view, F then P through the camera, review HTML opens, F9 flips to VR mode and back.

Latest snapshot with the merged source: Downloads/JewelryStore_Expanded_Unity6000.6.2f1.zip (scene files inside predate this merge; the validation run regenerates them). Downloads/JewelryStore_Review_Unity6000.6.2f1.zip is the verified batch-05 project. Older snapshots are history.

PC root: C:\Users\MOBPC\Documents\Codex\Jewelry-store-vr-demo\Jewelry-store-vr-demo-main (UnityProject, and UnityProject_Next for the review batch).

## Goal

Instructor-led VR jewelry robbery demonstration by October 27, 2026. No score, timer or forced procedure. Keep the user inside the shop, with a visible lightweight street outside. The same scene must run with mouse and keyboard (for testing) and in VR.

## Source map

Runtime: DemoModeSwitch (desktop/VR, F9), XRHeadTracking, XRControllerInput (built-in XR input API); ToolStation, DeployedTool, SceneTape, DemoToolGeometry (tools); EvidenceCamera (photos, HoldBy/ReturnToRack, CaptureFromView, LastCapturePosition); SessionReviewRecorder (local review JSON/HTML with photos and tool positions); DemoSession, ResettableSceneObject (reset; Resetting fires before clearing); InteriorBounds (shared interior regions); ToolHolder → DesktopInteractor (mouse/keyboard) and HandInteractor (tracked hands); XRLocomotion (teleport, snap turn); ShopWalkController (desktop walking); DemoDesktopPanel (presenter panel with DemoOverviewMap); DemoStatusBoard (wall text); ToolRackSample (rack samples spawn tools).

Editor: DemoAssetLibrary (third-party models with fallback), DemoGeometry (shared primitive/material/text/mesh helpers), JewelryStoreBuilder, ShopCityRefinement, ShopPresentationExpansion (geometry); DemoToolsBuilder (tool station, review recorder, status board, photo frame, player for desktop and VR); ShopAssetDressing (third-party props); IntactStateBuilder (intact overlay + CrimeSceneState); DemoValidation (shared checks) with JewelrySceneValidation.Run, ShopCityValidation.Run and ShopCityValidation.RunExpanded; DemoBuild (Windows player).

## Next technical dependency

Unity Package Manager fails to start normally (IPC server timeout); the -noUpm launcher permits package-free work. SETUP.md lists things to try. Resolve this, then install XR Plug-in Management + OpenXR; the scene needs no further changes to run in VR.

## Pending

Package Manager fix, OpenXR install, Quest tracking test, hands-on keyboard/mouse usability, headset performance, standalone build validation (Build-Windows.cmd is ready but unrun). Interior colliders and InteriorBounds constrain the desktop controller and teleport targets, not physical headset tracking.

## Collaboration rules

Read the brief, roadmap and this file. Preserve .meta files. Update source and the Expanded snapshot together; exclude Library/caches/InstructorReviews. **Pull `main` before editing**; two assistants work on this repository and the October 9 double implementation of mouse placement cost a day. Keep deployment ownership during XR grabbing and release held objects before reset (ToolHolder does this on DemoSession.Resetting). Record external asset licenses. Repeat tests when changes or unresolved concerns justify them.

## Batch 12 — intact store comparison (October 9, 2026, unverified)

CrimeSceneState (on the store root) swaps the robbed view (FixedEvidence, jagged glass, dropped tray, fallen trim, darkened door edges) for an intact overlay (whole storefront pane, intact case tops, stock on the emptied pads) and closes the safe door; I key or the panel button toggles it, and a session reset returns to robbed. IntactStateBuilder generates the overlay; JewelryStoreBuilder.Stock is shared with the display generator; DemoGeometry.Translucent is shared with the expansion's glass. Roadmap X01. Expected City result: 66 assertions.

## Batch 11 — sound and lit fixtures (October 9, 2026, unverified)

DemoSounds generates a click (tool taken), a thud (tool set down) and a looping street ambience (AmbientSound at the storefront); no audio assets. Ceiling fixtures and street-lamp heads use an emissive LampGlow material (DemoGeometry.Glowing). The panel ends with the model credits and DemoBuild copies every ThirdParty LICENSE.txt into Builds/Windows/ThirdPartyLicenses. Expected City result: 64 assertions.

## Review fixes after batch 10 (unverified)

Teleport goes to the destination the marker showed, refuses surfaces above step height, and snap turn moves the root through Relocate; the panel evaluates the pointer on demand and exposes RequestReset (two requests within four seconds) for validation; Remove clears the desktop hover; the snapshot no longer carries the removed XRRigBuilder.

## Batch 10 — photo frame and third-party props (October 9, 2026, unverified)

PhotoFrame: the last photograph appears on a quad on the wall above the rack (VR and monitor) and as a thumbnail in the panel. ShopAssetDressing places Khronos sample models (armchair, sofa, vase, bottle, sunglasses; licenses in ASSET_REGISTER.md and ThirdParty/KhronosSamples/LICENSE.txt) with box colliders on the furniture; the TrafficCone model replaces the generated cone visual when present (one bounds collider on the tool root). Unity generates .meta files for the OBJ/MTL/PNG on first import; commit them from the PC. Expected City result: 61 assertions.

## Batch 09 — usability, presenter map, first third-party models (October 9, 2026, unverified)

Tool rack samples (half-size cone, marker and tape post on the rack): click one or grab it to get a new tool in the hand. The pointed, nearest or held tool is highlighted (DeployedTool.SetHighlight, property block). Each VR hand has a small label naming the tool its A/X button spawns. DemoOverviewMap renders a top-down view into the presenter panel with the player's position; the panel was restyled (dark background, sections, mode-aware guide). DemoAssetLibrary places models from Assets/CrimeSceneDemo/ThirdParty with a primitive fallback; the Kenney Starter Kit City Builder models (CC0, see ASSET_REGISTER.md) replace the distant box buildings, add a fountain and street trees. Unity generates .meta files for the OBJ/MTL/PNG on first import; commit them from the PC. Expected City result: 56 assertions.

## History

- Batch 12 (Claude, unverified): intact/robbed store toggle (X01).
- Batch 11 (Claude, unverified): generated sounds, ambience, emissive fixtures, credits, licenses shipped with builds.
- Batch 10 (Claude, unverified): PhotoFrame, ShopAssetDressing with Khronos sample models, TrafficCone tool visual.
- Batch 09 (Claude, unverified): rack samples, highlight, hand labels, overview map, panel restyle, DemoAssetLibrary and Kenney CC0 city models.
- Batch 08 (Claude, unverified): one Player rig for desktop and VR; DemoModeSwitch; XRHeadTracking and XRControllerInput through UnityEngine.XR.InputDevices; VR mapping in CONTROLS_AND_INTEGRATION.md.
- Batch 07 (Claude, unverified): DemoGeometry consolidation; SceneTape.Refresh; two-click reset; REHEARSAL.md run sheet; review fixes (released tools settle on the surface beneath, taking a second tool puts the first down, default bounds, XRLocomotion.Relocate).
- Batch 06 (Claude, unverified): ToolHolder base, HandInteractor, XRLocomotion, EvidenceCamera hand-off, DemoStatusBoard, DemoBuild + Build-Windows.cmd/Run-Demo.cmd, Package Manager troubleshooting in SETUP.md.
- Batch 05 review (ChatGPT, verified, 40 assertions): SessionReviewRecorder, panel repair of the broken auto-merge, REVIEW_BATCH_05.md. Its DesktopToolPlacement/carry toggle were superseded by DesktopInteractor in this merge; its review checks now live in DemoValidation.
- Batch 05 (Claude): InteriorBounds, DesktopInteractor, presenter panel, DemoValidation, CreatePlayer; earlier fixes (marker font, walker grounding, build scene).
- Batch 04 (ChatGPT, verified, 33 assertions): expanded shop detail, coloured tools, CaptureFromView, street props. See ENVIRONMENT_BATCH_04.md.
- Batch 03 and earlier: see ENVIRONMENT_BATCH_03.md, TOOLS_BATCH_02.md, ENVIRONMENT_BATCH_01.md, VERIFICATION_2026-10-08.md.
