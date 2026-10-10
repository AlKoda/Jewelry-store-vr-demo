# Latest source change: five feature batches — panel, viewfinder, measuring tape, review report, lighting, VR comfort (October 10, unverified)

Implemented and self-reviewed in sequence on top of the imported-kits batch below; every batch added its own checks. Regenerate with **ShopCityValidation.RunTextured** (RunGlass still works for the untextured variant). None of it has been compiled or run in Unity yet.

- **Presenter panel** (DemoDesktopPanel): three layouts, Hidden → Strip (slim top bar: status, photo/intact/reset buttons, thumbnail, live view) → Full (reorganised sidebar), cycled by Tab; persisted UI scale 1–2 ([ and ] or +/- buttons, PlayerPrefs DemoPanelScale) through GUI.matrix; only the Full layout pauses desktop input (CapturesInput). The overview map draws per-kind tool glyphs, the camera and the visitor, and clicking it places the instructor beacon. PLACED TOOLS shows per-kind counts and the last 8 with Remove.
- **Live viewfinder** (EvidenceCamera.Viewfinder): a 480x270 render texture while the camera body is held, refreshed at 15 Hz without enabling the photo camera, shown in every layout and bottom-right when the panel is hidden; PhotoCount counts saved photos. PhotoFrame and the panel share EvidenceCamera.LastPhoto.
- **Measuring tape** (DemoToolKind.Measure, key 5, fifth rack sample, panel button, VR cycle): an 8 cm reel; two placed or selected reels stretch a flat yellow ribbon whose floating label reads the span ("1.50 m") and follows moved endpoints (SceneTape.DistanceLabel). ToolStation connects same-kind pairs only, with separate auto-connect chains for posts and reels (ToolStation.PendingPrompt feeds the board and panel).
- **Instructor review** (SessionReviewRecorder): review.html now has a photo contact sheet (shot number, time, camera pose), a shot list table and a 240 px inline SVG plan per entry drawn from InteriorBounds, with per-kind glyphs (colours shared with the panel via DemoDesktopPanel.KindColor), recorded ribbon connections (Entry.connections, additive) and the camera heading on photo entries.
- **Lighting** (DemoLighting, new): night preset (N or panel: lights to 25%, ambient 30%, fixture emission off, closer darker fog, one-second fade, dusk sky swap when the HDR pack is present) and a warm flashlight (L, right-stick click in VR, panel) riding the view or right hand across F9 switches. **Beacon** (DemoBeacon, new): pulsing amber ring and 2 m column clamped into InteriorBounds, placed with B at the pointer or by clicking the panel map; reset restores day, no flashlight, no beacon.
- **VR comfort**: teleporting aims a ballistic arc (XRLocomotion.FindArcDestination, LineRenderer green/red) sharing the straight ray's validation; XRComfortFade blinks 0→1→0 over 0.25 s on teleports and snap turns (head camera, VR-only); hand hint labels gained a dark backing and two hint lines, toggled for both hands by the left menu button.
- **More assets**: KayKitPrototype pack (CC0 shipment boxes, barrel; staged in the safe room, ShopAssetDressing.Count is 22 with a coffee table, book set and standing frame), hatchback and station wagon at the far kerbs, crates by the dumpster, and the_sky_is_on_fire dusk HDR (NightSky.mat).

New checks: 4 panel (PanelChecks + PhotoStage photo count), 5 + 3 measuring tape (ToolStage, InteractionChecks, HandChecks), 4 + 1 review report (PhotoStage, ResetStage), 6 + 1 lighting/beacon (LightingChecks, HandChecks, dusk-sky check when textured), 4 VR comfort (LocomotionChecks) — RunTextured should land around 182; the validator prints the exact count. Unity will generate .meta files for the new scripts (DemoLighting, DemoBeacon, XRComfortFade) and assets; the Glass snapshot zip carries minimal metas for the three scripts.

# Latest source change: imported kits, scanned textures, HDR sky, fixes (October 10, unverified)

**ShopCityValidation.RunTextured** now generates the full variant: RunGlass chain **with imported models** (the forms flag no longer switches them off), generated textures, and four new packs under Prototype/ThirdParty (each with LICENSE.txt; see ASSET_REGISTER.md): **KayKitCity** (police/taxi/sedan cars with wheels at the kerb, streetlights replacing the lamp boxes, benches, four towers alternating with the Kenney blocks, hydrant, traffic lights, dumpster, bushes in a StreetFurniture group), **KayKitFurniture** (standing lamp, rug, two picture frames, rear-wall shelf, safe-room cabinet and cactus; ShopAssetDressing.Count is 16), **CgbookcaseTextures** (parquet for the new SafeRoomFloor material, worn gold for JewelryGold) and **Environments** (venetian crossroads panorama as Skybox/Panoramic StreetSky.mat, light-room cubemap as custom reflections; cameras that cleared to a colour now clear to the sky). DemoAssetLibrary shares one atlas material per pack so each kit combines into a single draw; the backdrop triangle budget check is 40k. Sync Prototype/ThirdParty/* into Assets/CrimeSceneDemo/ThirdParty/*; Unity writes the .meta files and ShopTextureFinish sets the HDR import shapes itself.

Fixes in the same batch: the glass cubemap is baked after the textures; the recolour pass skips the tool rack; Loft/combined meshes get box-projected UVs and tangents in DemoGeometry.SaveMesh (checked by "Generated meshes carry UVs and tangents"); ToolsChanged fires on tape selection so the status board shows "choose the second post"; Hold no longer carries a tool while the presenter panel is open; the VR panel's Photograph button uses the held camera or the main view; highlight tints toward amber (visible on textured tools); DemoModeSwitch re-samples the headset while in Auto; IntactStateBuilder strips colliders from toggled props. **EvidenceCamera** encodes and writes the PNG on a worker thread (Status "Saving..." between shutter and PhotoSaved) and keeps the frame as `LastPhoto`, which PhotoFrame shows directly. Expected RunTextured count with every pack present: about 154 (137 + the previous 5 + 12 new checks, one of which replaces the geometry-only dressing check).

# Latest source change: generated textures and the L-scale (October 10, unverified)

**ShopCityValidation.RunTextured** = RunGlass + ShopTextureFinish: tileable textures generated by Tools/make_textures.py (Prototype/ThirdParty/GeneratedTextures: stone tiles, plaster, ceiling, walnut, brushed brass, marble, dark carpet, asphalt, pavement; albedo + normal maps, project-owned, no downloads) are applied to the existing finish materials with per-renderer tiling from world size; the tile skins are hidden under the textured floor; sidewalks got their own Pavement material in ShopCityRefinement. Without the texture files the flat finish stays. A fourth tool, the forensic **L-scale** (DemoToolKind.Scale: two 15 cm arms with 1 cm bands; key 4, rack sample, panel button, VR kind cycle) spawns like the others. Sync Prototype/ThirdParty/GeneratedTextures into Assets/CrimeSceneDemo/ThirdParty/GeneratedTextures; Unity creates the texture .meta files on import. Expected: RunGlass count + 5 (one scale check, four texture checks).

# Latest update: glass and tools

Current project: **UnityProject_Glass**, scene **JewelryStoreGlass**. Download Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip; launcher Open-Unity-Glass.cmd. [Report and captures](GLASS_AND_TOOLS.md). 137 automated assertions passed; physical Quest input/FPS and standalone build still pending.

Regenerate with ShopCityValidation.RunGlass. Copy all Scripts, Editor and Shaders. ShopGlassRefinement runs after the Interior and Robbery passes, bakes one 256px cubemap, seats all fragments, adds fracture edges and styles the tape prefab. Marker panel angles are fixed in DemoToolGeometry; ShopPresentationExpansion saves DepthTestedLabel into marker prefabs. Do not restore default font materials there.

ToolHolder.Place calls ToolStation.NotifyPlaced; spawning or switching modes does not auto-connect tape. New posts connect to the previous placed post. Repositioning an existing post does not duplicate tape. StartNewTapeRun clears the chain. R/Delete or the panel removes placed tools; connected tape disappears with its endpoint. DesktopInteractor.Aim is shared by runtime pointer input and selection validation.

Older milestone notes follow; use the Glass project above for current work.

# Latest update: robbery refinement

Use **JewelryStoreRobbery** in **UnityProject_Robbery**, or Downloads/JewelryStore_Robbery_Unity6000.6.2f1.zip and Open-Unity-Robbery.cmd. [Report and screenshots](ROBBERY_REFINEMENT.md). Regenerate with ShopCityValidation.RunRobbery. This runs the prior Interior chain then ShopRobberyDressing.Apply, preserving intact stock separately from disturbed stock. Never run dressing twice on the same scene.

91 automated assertions passed (92 PASS lines with completion). Desktop panel starts closed; right click toggles look, Escape releases; Tab opens the panel and suspends walking/tool hotkeys. Overview rendering is suspended while hidden. Invalid pointer targets cannot place a tool at an old location. Physical Quest input/performance and standalone builds are still pending.

Previous milestone notes follow for context; use the Robbery project above for current work.

# Assistant handoff

## Current state

All work lands on main; pull before editing.

**Latest verified scene: JewelryStoreInterior**, October 10, 2026. Unity 6000.6.2f1 Built-in; all source and the world-text shader compiled and **81 automated Editor Play mode checks passed**. See [INTERIOR_FINISH.md](INTERIOR_FINISH.md) for renders, material choices, measured budgets and limits.

Download Downloads/JewelryStore_Interior_Unity6000.6.2f1.zip; extract into UnityProject_Interior beside README, use Open-Unity-Interior.cmd and open Assets/CrimeSceneDemo/Scenes/JewelryStoreInterior.unity. The same project is already on the PC.

Run ShopCityValidation.RunInterior in an isolated project to regenerate. RunForms and historical downloads remain available. Imported-assets RunExpanded was not rerun.

Order: Forms -> ShopInteriorFinish (walls/floor/lights/doors) -> ShopDisplayFinish (glass/cases/jewelry/world labels). Keep Prototype/Shaders/WorldText.shader synchronized into Assets/CrimeSceneDemo/Shaders; Scripts/DepthTestedLabel.cs keeps the font atlas live. All materials use flat colors; no new texture maps. Glass has no refraction. The staff door is static, fixed open; the safe door still follows the intact/robbed toggle.

Measured: finish floor/door geometry 358 triangles / 10 renderers; display strips 96 / 1; form detail 1,624 / 24; city 5,044 / 19. Initial active scene MeshFilters: 14,058 triangles / 322 mesh renderers, excluding text. Four shadowless lights; three local fills use vertex lighting. These are content counts, not FPS measurements.

The inherited interaction fixes remain: closest item wins when grabbing; release holders before mode-switch deactivation; synchronous grab tests call Physics.SyncTransforms. Quest tracking, headset frame time/stereo, hands-on usability and standalone build remain pending. Package Manager needs -noUpm; the known Editor SearchDatabase exception did not prevent validation.

PC root: C:\Users\MOBPC\Documents\Codex\Jewelry-store-vr-demo\Jewelry-store-vr-demo-main

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

## Review fixes after batch 13 (unverified)

DemoPhysics.Nearest is the one ignoring raycast (64 hits, upward-only option) used by the pointer, hands, tool settling and teleport aiming (which now skips the rig and the held tool). A tool knows its Holder, so two hands cannot hold one tool and a hand holding the camera spawns nothing. Settling casts from above the tool (clears a case it was lowered into) and is silent on reset or disable. DemoModeSwitch also disables the CharacterController in VR. The overview map culls a ceiling layer instead of toggling objects. The photo frame uses the Standard shader (always in builds). The walker clamps before its single Move and caps fall speed. The review recorder keeps an entry when writing fails; the mouse wheel over the panel no longer rotates the tool. Expected City result: 70 assertions.

## Batch 13 — more models and a reusable converter (October 9, 2026, unverified)

Six more Khronos sample models (Lantern as the street lamps; ChronographWatch and the plant, candle holder, iridescent lamp and damask chair as shop dressing), decimated and converted with the new Tools/glb_to_obj.py (trimesh + fast_simplification; texture downscale, decimation, scale, grounding). ShopAssetDressing.Count is the single source for the dressing check. Expected City result: 67 assertions.

## Batch 12 — intact store comparison (October 9, 2026, unverified)

CrimeSceneState (on the store root) swaps the robbed view (FixedEvidence, jagged glass, dropped tray, fallen trim, darkened door edges) for an intact overlay (whole storefront pane, intact case tops, stock on the emptied pads) and closes the safe door; I key or the panel button toggles it, and a session reset returns to robbed. IntactStateBuilder generates the overlay; JewelryStoreBuilder.Stock is shared with the display generator; DemoGeometry.Translucent is shared with the expansion's glass. Roadmap X01. Expected City result: 67 assertions.

## Batch 11 — sound and lit fixtures (October 9, 2026, unverified)

DemoSounds generates a click (tool taken), a thud (tool set down) and a looping street ambience (AmbientSound at the storefront); no audio assets. Ceiling fixtures and street-lamp heads use an emissive LampGlow material (DemoGeometry.Glowing). The panel ends with the model credits and DemoBuild copies every ThirdParty LICENSE.txt into Builds/Windows/ThirdPartyLicenses. Expected City result: 64 assertions.

## Review fixes after batch 10 (unverified)

Teleport goes to the destination the marker showed, refuses surfaces above step height, and snap turn moves the root through Relocate; the panel evaluates the pointer on demand and exposes RequestReset (two requests within four seconds) for validation; Remove clears the desktop hover; the snapshot no longer carries the removed XRRigBuilder.

## Batch 10 — photo frame and third-party props (October 9, 2026, unverified)

PhotoFrame: the last photograph appears on a quad on the wall above the rack (VR and monitor) and as a thumbnail in the panel. ShopAssetDressing places Khronos sample models (armchair, sofa, vase, bottle, sunglasses; licenses in ASSET_REGISTER.md and ThirdParty/KhronosSamples/LICENSE.txt) with box colliders on the furniture; the TrafficCone model replaces the generated cone visual when present (one bounds collider on the tool root). Unity generates .meta files for the OBJ/MTL/PNG on first import; commit them from the PC. Expected City result: 61 assertions.

## Batch 09 — usability, presenter map, first third-party models (October 9, 2026, unverified)

Tool rack samples (half-size cone, marker and tape post on the rack): click one or grab it to get a new tool in the hand. The pointed, nearest or held tool is highlighted (DeployedTool.SetHighlight, property block). Each VR hand has a small label naming the tool its A/X button spawns. DemoOverviewMap renders a top-down view into the presenter panel with the player's position; the panel was restyled (dark background, sections, mode-aware guide). DemoAssetLibrary places models from Assets/CrimeSceneDemo/ThirdParty with a primitive fallback; the Kenney Starter Kit City Builder models (CC0, see ASSET_REGISTER.md) replace the distant box buildings, add a fountain and street trees. Unity generates .meta files for the OBJ/MTL/PNG on first import; commit them from the PC. Expected City result: 56 assertions.

## History

- Batch 13 (Claude, unverified): six more Khronos models, Tools/glb_to_obj.py converter.
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
