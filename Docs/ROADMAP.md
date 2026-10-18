## Smooth VR movement — October 18 (Claude, unverified)

- [x] Left-stick smooth walking with wall and bounds stops; F8 switches to teleport; 3 checks.
- [ ] Headset test on the PC; consider a comfort vignette and VR footsteps from MovedDistance.

## Four more asset packs — October 17, round 2 (Claude, unverified)

- [x] AmbientProps2, RealismTextures, RealFoley2 fetched with licences and register rows.
- [ ] Wire all packs in with fallbacks and checks; audition RealFoley/RealFoley2.

## Three more asset packs — October 17 (Claude, unverified)

- [x] AmbientProps, CutoutDecals, RealFoley fetched with licences and register rows (see HANDOFF).
- [ ] Wire them into the night store, with fallbacks and validator checks; swap the synthetic footsteps for RealFoley using its README table.
- [ ] Listen to RealFoley and check the props and decals in the headset.

## Fixture detail and realism gap round — October 16 (Claude, unverified, never run in Unity)

The first gap round after the realism wiring, ranked by guest impact per unit of frame time. The Roslyn compile check reports only the 2 known NightLighting.cs:624 errors (an API the old reference DLLs lack); nothing has been compiled or run in Unity. The A numbers are the realism critic's gap list of October 15: A1 the main fixtures, A4 the wet street, A9 fabrics and small metals in VR, A13 alarm and security hardware, A15 architectural trim, A16 minor exterior items, A17 weak textures. HANDOFF has the details, the triangle and budget tables, the check names and the PC steps.

- [x] Validator helper cleanup: the area files' private ray and footprint helpers are shared DemoValidation helpers, with the new CoplanarOverlap and AppendWorldMesh; StoreKit.Transparent and KitMesh.Emit's loop use DemoAssetLibrary.Blended. This closes the October 16 cleanup item below.
- [x] A1, fixture detail (done, unverified): 10 mm case panes with a green edge on the overlay top, bevelled bronze posts and rails that butt, beads, piping, LED channels, staff-side locks and a plinth gap on the 14 cases; the counter; bevelled safes with hinges, bolt receivers, bolt-work, a dial safe and a keypad safe; glazing beads, through-bolted pulls, deadlocks and contacts on the forced doors; caps, beads and gaskets on the 8 facade bays; contacts and locks on the two side-room doors. The z-fighting frame joints on the cases and the counter are fixed.
- [x] A13, security hardware, the rest without the roller grille (done, unverified): three PIR detectors, door contacts, a second alarm keypad, two CCTV domes and two smoke detectors; the corner camera's plate square to the wall, and the camera 3 cm lower so its hood clears the ceiling. The roller grille waits on the entry decision (2).
- [x] A15, architectural trim (done, unverified): architraves round the office opening, the skirting corners and the missing office-face run, a ceiling shadow gap, switches with one plain rocker and sockets with blank faces (decision 1). The cut CCTV cable is in the hidden Case evidence set; the DVR is left to decision 8.
- [x] A9, partial (done, unverified): the small-metal smoothness clamp (gold, silver, coin and bar 0.72, pearl 0.70, gem 0.85). The sheen and specular anti-aliasing shader stays open.
- [x] A16, trees only (done, unverified): three bare deciduous trees across the street. The LED street column and the CSI van or ambulance stay open.
- [x] 19 new checks: 8 in StoreKitValidation (CaseGlass, FrameJoint, FixturePart, SafeBolt, CaseHardware, Glazing, Contact, Device) and 11 in the new NightStoreValidationDetail.cs.
- [x] Sync the Downloads ZIP: StoreKitDevices.cs and NightStoreValidationDetail.cs with fresh .meta files, and the changed Editor files (59.1 MB).
- [ ] Apply and verify on the PC: Docs/PC_VERIFICATION.md (compile, RunNightQuick, RunNight, RunGallery, RunGlass, desktop and headset checks).
- [ ] Run on the PC and report the first FAIL line, if any, with the Info lines above it. Expected:
  - RunGallery +8, with the "<entry> triangles=... submeshes=" lines read against HANDOFF's table;
  - RunNightQuick +19, recording "Budget: triangles=" (about 163k), "Budget: materials=" (about 129; then set MaterialBudget to that value + 8) and "Budget: submeshes";
  - RunNight +19, recording the Lightmaps= line and judging the case Bodies' baked sharpness (the probe-lit hardware split is the fallback);
  - the ShopCity runs: no change.
- [ ] Frame Debugger batches and SetPass calls at the arrival view and at the central cluster, on a baked build.
- [ ] Headset checks (Quest 2 over Link, baked build): shimmer on the dropped ring, the coins and the pearl strand under the torch; the case edge band; no flicker at the case corners, the architraves or the keypads' screens; the broken cases' deck fragments (4.5 mm over the suede) not looking afloat; the trees through the windows.
- [ ] Desktop check: a tool set down on a case top rests on the glass, and the slightly less mirror-like gold and silver still look right.
- [ ] Next round, in order:
  1. Wet-street reflection streaks (A4): a per-camera, ground-aligned mode in GlowBillboard, each streak at the lamp's mirror point for the eye and turned to each camera in onPreCull (about 6 additive cards, transparency 57 -> 63 of 70; the glow-card checks need widening). A fixed streak would slide off its lamp's mirror line as the guest walks.
  2. The sheen and specular anti-aliasing shader (the rest of A9): one forward-only surface shader with a sheen term for velvet and suede and roughness widening from the screen-space normal variance for the small metals and the thinnest parts, with lightmaps, probes and single-pass and multiview stereo; it needs a compile and a headset profile.
  3. Foliage for the trees: alpha-tested leaf cards need a leaf texture the repository lacks, one cutout material and cutout overdraw seen through the facade.
  4. Second-tier fixtures and safe edge-wear (A17).
- Deferred, each with its reason:
  - The DVR, its cables and trunking: whether the recorder is intact, taken or damaged decides what footage exists, which is part of decision 8. InteriorModels DVRRecorder is ready (206 triangles; as shipped, its textured case and printed front would add two materials (its constant rubber can share an existing one); with the case remapped to KitDeviceBlack, the front adds one).
  - Green rims on the broken glass: the Glass and Shards renderers must stay all-blended (shadows, the case-glass merge and the transparency count depend on it), so opaque rims need a sibling renderer per broken part (+6 to +10), and the fracture look waits on decision 2.
  - The probe-lit hardware split: a fallback only, if the bake blurs the case Bodies (+14 renderers, FixturePartChecks updated).
  - Laminated glass and the roller grille: decision 2. The call point: decision 10. Socket and switch faces: decision 1.
- [ ] Known gaps left as they are (HANDOFF lists them): on the broken right sidelight 3 of the 4 sill fragments pass under the new shop-side bead (keep the sill spill street-side of z -0.005 with smaller fragments); the gallery's closed safe has its door parts reaching into the shelf fronts (both night-store safes stand open); the alarm keypads' screens are only 0.1 mm proud; the corner camera no longer takes a user-converted model, because only the procedural one takes the aim.

## Validator checks for the realism wiring — October 16 (Claude, unverified, never compiled)

This batch adds checks for the October 15 wiring's behaviours: 82 in the night store's runs and 1 more in the ShopCity runs. It also fixes the builder and runtime issues the checks exposed. HANDOFF has the check names, the PASS counts and the PC steps.

- [x] NightStoreValidation is partial, with six area files: street, interior, closing procedure and decals, lighting, presenter switches and credits, and sound and music. They are wired into Generate (Edit mode, before the first save), Tick stage 1, DisturbBeforeReset and stage 3.
- [x] DemoValidation.LauncherChecks (the launcher mirror, all twelve packs together, the licence copy) runs in NightStoreValidation and in every ShopCity run. Also new: a check of the start menu's credits summary, and one that the old shop has no night-store components.
- [x] Shared helpers: DemoAssetLibrary.WorldBounds and Blended, DemoGeometry.PathUnder and Opaque, and DemoBuild.AddMenuMusic.
- [x] Fixes:
  - glow halos turn to each rendering camera (Camera.onPreCull);
  - the lost-cat flyer moved to the right pier;
  - three showroom reverb zones, none reaching the pavement;
  - no geometric shoe print at arrival;
  - KitMesh's fingerprint includes atlas UVs;
  - NightDecals takes an imported flag;
  - AmbientZones holds a flat cutoff curve;
  - the Scene-tab rows stay stable between Layout and Repaint.
- [x] Sync the Downloads ZIP: the six new area files with fresh .meta files and the 19 changed files (59.1 MB).
- [ ] Run on the PC and report the first FAIL line, if any, with the Info lines above it. Expected:
  - RunNightQuick +82 (+44 from the ZIP alone);
  - RunNight +81 (+43 from the ZIP alone);
  - RunGlass and every other ShopCity run +5;
  - RunGallery: no change.
- [ ] Still unchecked, because batch mode cannot observe them; they stay on the PC lists: audible playback and reverb wetness, the Scene tab's drawing, the start menu's key handling, the halos in the headset, and the glass footsteps on the door and sidelight fragments (a known gap).
- [x] Cleanup: the area files' private triangle-ray helpers (StreetMeshRay, InteriorRay, DressingRay) could become one DemoValidation helper, and StoreKit's private Transparent duplicates DemoAssetLibrary.Blended. Done in the fixture-detail round above (unverified).

## Realism packs wired into the night store — October 15 (Claude, unverified, never compiled)

The twelve packs are wired into the night store for Quest 2 over Link and for mouse and keyboard, each with a primitive or procedural fallback, and the validator pass has landed. HANDOFF has the details, the check names and the PASS counts.

- [x] Import rules for the twelve packs (PackImport.cs) and pack-aware model import (no uv2 on props, chart margins on the buildings and the rug).
- [x] DemoAssetLibrary.PlacePack with LOD classes; shared pack materials keyed by colour map, tint and role; glass from the MTL alpha; finishes from the pack data; atlas cells; credits for CC BY and MIT models.
- [x] StoreKit palette: one set per texture name in a fixed priority (PBRInterior/PBRExterior, then ReferenceTextures or, for the ceiling, NightMaterials, then GeneratedTextures); new brick and shutter; AO on six surfaces.
- [x] PoliceLook: one setting, `Provisional` (today's look, plus a generic V8 idle) by default; `ProvisionalRealisticUS` for review only.
- [x] Street: lanterns, the ApartmentBlock across the street, brick, render and shutter facades, bollards, drains, a utility box, a bus shelter, bike racks, a forensic case, a glove box, a passer-by and an alarm bell box; the KayKit towers removed.
- [x] Lighting: the lamp profile with the streetlight cookie, the car-driven lenses and light bar, additive halos and washes without lights, the overcast panorama sky, an across-street reflection probe and NightSky cookies on the torches and lamps.
- [x] Interior: snake plants, a rug, a monitor and keyboard, an extinguisher, a clock, ceiling diffusers, and wall graphics from the WallGraphics atlases (showroom, office and street).
- [x] Closing procedure: the cases hold display furniture only (`StockInSafes`), with left-behind jewellery in the open safes, on the desk and on the floor.
- [x] Decals: always-on glass shards, puddles, litter and grime; "Ambient wear" and "Case evidence" as hidden presenter sets.
- [x] Soundscape: zones that muffle the street inside, random street events, reverb, footsteps, recorded set-downs, the alarm bell as a presenter set, music in the menu, an underscore switch and the reset sting.
- [x] Presenter switches in the Scene tab: light bar flashing, the scene-dressing sets and the underscore music.
- [x] Credits from the placed assets in the Settings tab and on the start menu, and the packs' extra licence notices in the build.
- [x] Tools/sync-project.cmd: Open-Unity-Glass.cmd and Start-Demo.cmd mirror Scripts, Editor, Shaders and ThirdParty.
- [x] Prep inside the packs: LOD1/LOD2 for 17 models and four atlases (two WallGraphics pages, the SurfaceDecals Ambient and Evidence sets), all checked by script.
- [x] Validator pass: 21 new NightStoreValidation checks (asset library, import, police, LOD budget), 3 StoreKit palette checks, 2 InterfaceChecks and 5 ShopCity checks; two checks renamed; budgets counted at the LOD level selected from the worst eye; MaterialBudget 136. Expected: +26 RunNightQuick and RunNight, +3 RunGallery, +7 RunGlass and RunTextured, +6 the other ShopCity runs.
- [x] Checks for the new behaviours that still had none (CLAUDE.md asks for one each): street, lighting, interior, closing procedure, decals, soundscape and music, presenter switches and their reset, credits and licence notices, and the launcher mirror ("all twelve packs present"). The October 16 batch above covers them, unverified; what batch mode cannot observe stays on the PC lists.
- [x] Copy the 12 new .cs files (each with a fresh .meta) and the changed scripts into Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip (59.0 MB). The twelve realism packs stay out of the ZIP; Start-Demo.cmd and Open-Unity-Glass.cmd mirror them from the repository.
- [ ] Run on the PC:
  - the first RunNightQuick, recording the one-time reimport time and the Budget lines;
  - set MaterialBudget from the log (materials + 8);
  - RunNight, recording the bake seconds and the Lightmaps= line;
  - RunGallery and RunGlass.
- [ ] Bake checks: dark rectangles under the decals (fall back with `NightDecals.DecalsLightmapped = false`), the lantern pools, ApartmentBlock chart bleeding.
- [ ] Headset checks (Quest 2 over Link, baked build): the light bar's photosensitivity and steady switch, halos in both eyes, decal z-fighting, KitGem shimmer, LOD pops, no footsteps from room-scale walking, teleports or snap turns, frame time.
- [ ] Desktop checks: the Scene-tab switches and their reset, footsteps, the menu music fade, credits.
- [ ] Speaker checks: audio levels, reverb, loop clicks after the Vorbis re-encode.
- [ ] Open-Unity-Glass.cmd mirroring on the PC.
- [ ] Koda's decisions (for Koda.md):
  - decision 1, the country and police service, which now includes the ProvisionalRealisticUS option;
  - the alarm bell (6);
  - ambient wear and GlassSmudge (7);
  - the case-evidence contents (2, 5 and 8);
  - StockInSafes (9);
  - the call-point style (10).
- Deferred, each with its reason:
  - GlamVelvetSofa: untextured (Kd 0.012 reads black), 2.19 m wide with 1 cm to the side table, and CC BY; the StoreKit sofa stays.
  - FireAlarmCallPoint: a US pull station, which would mix countries in a store that prices in pounds; its style waits for the country decision.
  - Gem shader: few gems show under the closing procedure, and a stereo shader needs a headset check; KitGem (opaque metallic) stands in.
  - HDRI reflections: the photographic HDRIs are identifiable, have lamp pixels of 12,800–54,000 and would leave a seam at x 14; the generated sky and the across-street probe are used instead.
  - Wet-street streak quads: wrong stereo depth, and they do not move with the head; the probes already reflect the lenses. Revisit with a view-dependent shader after a headset check.
  - GlassSmudge: its fingerprint clusters read as evidence; listed for Koda with the ambient wear.
  - Country presets: Koda's decision 1; a decided service becomes one more PoliceLook value.
  - Laminated glass: waits on the entry decision (decision 2).
  - Fixture-detail generator: a large procedural StoreKit pass that gets its own batch. Done in the October 16 fixture-detail round (unverified).
  - Sheen, specular anti-aliasing, torch beams, people's hands: custom shaders, particles and rigs, each needing headset checks; the metals are clamped meanwhile.
  - Pack torch models: the runtime torches work in both modes; pack meshes need prefab references and hand-pose checks.
  - Security hardware, procedure props, trim and trees: separate batches (the bell box is added now). The October 16 fixture-detail round adds the security devices, the trim and three bare trees (unverified); the procedure props and the roller grille remain.
  - Other vehicles and buildings (Hatchback, SedanClassic, the Downtown buildings): CC BY, cost materials and sit behind the arrival view.
  - Country-specific street furniture (signs, hydrant, name plates, LED light bar): depends on the country.
  - ApartmentBlock LODs: it is at LOD0 from 14 m or more, and decimation would damage its flat facade.
  - Dog barks 01–03: the CC BY 3.0 evidence conflicts; only the CC0 _04 is scheduled.
  - Alarm bell by default: whether it still rings is a scenario choice, and continuous ringing tires a session; it is a presenter switch, off by default.
  - Glass tinkle events: random settling sounds could startle trainees.
  - Bloom and flares: they need post-processing, and the scene renders LDR without packages.
  - Stock extras (price tags, stands, posters, vinyl, 100 g bars): they conflict with the closing procedure and cost materials.
  - Pre-placed barriers and markers: placing them is the trainee's task.

## Realism asset packs — October 15 (Claude, unverified)

- [x] Twelve license-checked packs in Prototype/ThirdParty (PBR materials, night HDRIs and skies, jewellery, forensic, street, interior and people models, wall graphics, decals, a night soundscape, music), with 95 register rows.
- [x] Wire them into the night store with fallbacks, a texture import postprocessor, and LODs or decimation within the triangle and material budgets (the October 15 wiring batch above; unverified, never compiled). Its validator pass has landed, and the checks for the remaining new behaviours followed in the October 16 batch (see above; unverified).
- [ ] Decide the country (police livery, tape text, siren) — see for Koda.md, decision 1; the choice now sits behind `PoliceLook.Current`.

## Performance and realism fixes from the audit — October 15 (Claude, unverified, never compiled)

The audit's 46 confirmed findings are fixed in source, except for two that are deferred and four that are only partly done. HANDOFF lists those, along with the details, the check names and the PASS counts.

- [x] Night lighting realism: 20 texels/m with per-zone scales (new GroundScale), combined-directional lightmaps, a lightmapped row across the street, lenses in the baked reflection probes, 256 px glass probes (sky 128 px).
- [x] Lit police tape (torch and lamp light, shadows); flat-albedo overview map (MapAlbedo shader); LDR anti-aliased photos with a mipmapped display copy.
- [x] VR frame:
  - Torches and lamps keep their pixel lights (ForcePixel; preview headlights Auto).
  - The viewfinder skips the flashlight shadow.
  - The map renders only on its tab, once a second.
  - Faded hints stop drawing.
  - Head and controller poses are read again before rendering, with a fixed update order.
  - The recentre blinks.
  - The headset interface stays out of photos, the viewfinder and the map.
- [x] Desktop HUD, launcher and photo capture without per-event or per-photo allocations.
- [x] Build:
  - A failed bake falls back to the preview, and the start menu shows the real lighting.
  - `Start-Demo.cmd bake` is sticky.
  - Linear before the first import.
  - Player builds strip the deferred shader pass.
- [x] Tools released overhead drop to the floor. The grab scan no longer truncates.
- [x] Theme re-lookup after a scene load; F2 not saved; presenter-only toasts kept off the headset; runtime materials and textures freed.
- [x] 30 new validator checks.
- [x] Copy the changed files and the two new ones (ForwardOnlyShaderStripping.cs, MapAlbedo.shader, each with a fresh .meta) into Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip (done with 03ad44c).
- [ ] Run the validators on the PC:
  - RunNightQuick and RunNight: +24 each. Record the bake seconds and the Lightmaps= line.
  - RunGallery: unchanged.
  - RunGlass: +16.
- [ ] Manual checks: launcher bake, rebuild and footer; Linear on the first unpack; OnGUI GC Alloc near 0; no map render on other tabs; tape under the flashlight; visual check of the bake.
- [ ] Quest 2 checks:
  - Frame time, on a baked build only.
  - Tape in both eyes.
  - Held tool locked to the controller.
  - No headset interface in photos.
- [ ] Decide with measurements: baking by default (BI-01, after a timed bake); occlusion culling (R12/BI-10, after a headset profile); async photo readback.

## October 13 — interface redesign (Claude, unverified)

- [x] Shared DemoUi theme (Barlow, dark glass and brass, icons) for the monitor, start menu, status board and controllers.
- [x] Desktop HUD: hotbar, crosshair, context prompts, toasts, live view; tabbed drawer; F1 controls overlay; F2 clean view; settings (size, HUD, prompts, sensitivity, invert Y, field of view).
- [x] Crouch (C) and zoom (Z); number-pad tool keys.
- [x] Quest 2 controller models, look-to-show tooltips, tool carousel, wrist status, VR toasts, haptics, ring teleport marker.
- [ ] Compile and run RunGlass / RunNightQuick on the PC; screenshot the HUD, drawer tabs and help overlay.
- [ ] Headset pass: tooltip placement on the real controllers, text size, fade angle, haptic strength.

## October 12 — Tripo models

- [x] Tripo Studio guide, prompts and 33 input images (Docs/TRIPO_GUIDE.md, Docs/TripoInputs).
- [ ] Generate batches 1–5 on a paid plan; register each model.
- [ ] Override hooks for cases, safes, doors, jewellery props and tools; PBR repack importer.

## October 12 — one-click build (Claude, unverified)

- [x] Start-Demo.cmd: update, sync, build and start; DemoBuild.BuildLatest; in-game start menu (DemoLauncher, F10 back).
- [ ] First run on the PC; keep the resulting Builds\Windows folder as the tested backup.

## October 11 — expanded store and nighttime arrival (current priority; planning only)

The next milestone is [NIGHT_ARRIVAL_PLAN.md](NIGHT_ARRIVAL_PLAN.md). It supersedes the immediate texture pass. Existing completion entries below are historical; newer source remains unverified until run in Unity.

- [x] Record scenario, reuse strategy, dependencies and phased work.
- [x] Receive and inspect store-reference.jpg; record observed layout and proposed blockout dimensions in [REFERENCE_LAYOUT_PLAN.md](REFERENCE_LAYOUT_PLAN.md).
- [x] Build the proposed 16 x 12 m store from one layout definition (Claude, unverified; NightStoreValidation.RunNightQuick / RunNight).
- [ ] Run it on the PC, compare the eight capture views with the references, tune dimensions after a walkthrough.
- [x] Preserve baseline and create a separate expanded low-poly scene (JewelryStoreNight; StoreKit library; unverified).
- [x] Add parked police car, staging area and connected indoor/outdoor navigation (unverified).
- [x] Extend tape/cone placement, map and review bounds to the accessible exterior (unverified).
- [x] Establish midnight appearance: baked headlights/streetlights/moonlight, probes, fixtures off, flashlight with cookie (unverified; bake time and FPS to measure).
- [x] Add bounded/occluded UV trace demonstration (placeholder traces hidden by default) and unbranded DSLR visual (unverified).
- [x] Optional placeable scene lamp (unverified).
- [x] Night tool prefabs styled like the shop's and saved in their own folder; carried lamp follows the staging-bay street level (Oct 11 audit, unverified).
- [x] Wire the staged NightMaterials packed maps and ceiling/facade finishes, NightGraphics signs (fascia, Closed, Staff Only, collection) and NightAudio clips (DemoSoundBank) with fallbacks (Oct 11, unverified; decals, SceneTape and MarkerNumbers deliberately not placed).
- [ ] Audition the NightAudio clips in Unity/headset and check sign readability and the marble/plaster maps under the flashlight.
- [ ] Replace ReferenceTextures test placeholders and procedural StoreKit pieces with the user's final textures and converted models (same names; overrides apply only to Instantiate-placed fixtures, stateful cases/safes/doors/windows/dressing props need a builder change that keeps their Door/Lid/Glass_* children).
- [ ] Re-stage robbery evidence, verify reset/photos and profile on Quest.
- [ ] Resume store materials after the blockout is accepted.

## October 10 — feature batches: panel, viewfinder, measure, review, lighting, VR comfort (Claude, unverified)

- [x] Presenter panel Strip/Full layouts, UI scale, map glyphs, live camera viewfinder, photo count.
- [x] Measuring-tape tool kind with distance labels and same-kind auto-connect.
- [x] Review report: contact sheet, shot list, SVG scene sketch per entry with connections.
- [x] Night preset with dusk sky, flashlight (desktop and VR), instructor beacon from pointer or panel map.
- [x] Teleport arc, comfort fade (fade-from-black on the move frame since the Oct 11 audit, unverified), hand hint labels with menu-button toggle.
- [x] KayKit Prototype Bits, two more cars, safe-room storage, coffee table and dusk HDR.
- [ ] Run ShopCityValidation.RunTextured on the PC and report the count; check the panel on the projector and the arc in the headset.

## October 10 — imported kits, scanned textures, HDR environment (Claude, unverified)

- [x] KayKit City and Furniture Bits (CC0) placed by the generators with primitive fallback; one atlas draw per kit.
- [x] cgbookcase parquet and gold maps; HDRI Haven sky and reflection environment.
- [x] Async photo encoding with LastPhoto texture (updated only after a successful save since the Oct 11 audit, unverified); status-board tape prompt; nine reliability fixes (see HANDOFF).
- [ ] Run RunTextured on the PC; inspect the street from inside the shop, the safe room and the reflections; report the count.
- [ ] Next: presenter panel redesign (strip/full modes, UI scale, photo thumbnail), live viewfinder, measuring tape, photo log in the review, flashlight/night preset, instructor beacon, smoothed spectator camera.

## October 10 — textures and L-scale (Claude, unverified)

- [x] Tools/make_textures.py: nine tileable albedo + normal textures, project-owned.
- [x] ShopTextureFinish and RunTextured: textured floor, walls, ceiling, counter, cabinets, trims, carpet, street, pavement with world-size tiling.
- [x] Forensic L-scale tool (key 4 / rack / panel / VR cycle).
- [ ] Run RunTextured on the PC; inspect captures; check headset cost of normal maps.

## October 10 — glass and tools completed

- [x] Seat all 32 fragments; smaller transparent pieces with physical edges.
- [x] Correct marker A-frame and save depth-tested number labels into prefabs.
- [x] Auto-connect placed tape posts; update moving endpoints; separate tape runs.
- [x] R/Delete and explicit placed-tool Remove buttons; remove connected tape with posts.
- [x] Static glass reflection, fracture outlines and clear glazing.
- [x] Render and inspect both marker faces, tape, fragments and storefront; 137 automated assertions passed.
- [ ] Quest 2 input/comfort and glass overdraw/FPS profiling.
- [ ] Standalone build validation.

See [report and screenshots](GLASS_AND_TOOLS.md).

## October 10 — robbery refinement completed

- [x] Hide presenter menu at startup; compact controls hint.
- [x] Toggle mouse look; suspend desktop movement and tool shortcuts while menu is open.
- [x] Reject tool placement without a valid surface; skip hidden overview renders.
- [x] Empty/disturbed displays, overturned and fallen stands, scattered jewelry and open tray.
- [x] Restore tidy stock in intact mode; preserve reset behavior.
- [x] Actual Unity renders and automated Editor Play mode validation; see [report](ROBBERY_REFINEMENT.md).
- [ ] Physical Quest 2 control feel, tracking and FPS test.
- [ ] Standalone build verification.

# Roadmap

Updated: October 9, 2026.
Statuses: Planned / In progress / Blocked / Complete.
Dates are targets, dependent on access to the PC and headset.

| ID | Feature | Priority | Status | Target | Completion criteria |
|---|---|---|---|---|---|
| P01 | Repository and planning documents | Required | Complete | Oct 7–9 | Brief, roadmap, setup and asset register stored on GitHub |
| P02 | Camera starter script | Required | Complete | Oct 7–9 | Portable source saved; runtime validation tracked separately |
| U01 | Unity project setup | Required | Planned | Oct 10–12 | Editor/pipeline/packages recorded; project opens without errors |
| U02 | Quest PC connection | Required | In progress | Oct 10–12 | Head and controller tracking work in a Windows build (tracking scripts in place via built-in XR input; needs OpenXR installed to test) |
| U03 | Movement | Required | In progress | Oct 10–12 | Desktop walking done; thumbstick teleport (marker preview, refused outside the shop or above step height) and snap turn around the head, untested on a headset |
| U04 | Store blockout | Required | In progress | Oct 10–12 | Scene generated and saved; scale, traversal and collision checks in Quest remain pending |
| T01 | Cones and markers | Required | In progress | Oct 13–16 | Spawn, grab, place, move and remove objects anywhere suitable (desktop pointer done; XR grab pending) |
| T02 | Scene tape | Required | In progress | Oct 13–16 | Tape spans two posts and follows repositioning |
| T03 | Camera integration | Required | In progress | Oct 17–19 | Controller capture saves readable PNGs; failures shown clearly |
| S01 | Crime scene dressing | Required | In progress | Oct 17–19 | All six agreed evidence/damage elements visible |
| V01 | Visual polish | Required | In progress | Oct 20–23 | Materials and lighting readable in headset (CC0 city models added to the backdrop; shop interior still generated geometry) |
| D01 | Monitor view and controls | Required | In progress | Oct 20–23 | Visitors see view; presenter can access controls guide (desktop panel with guide done; headset mirror pending) |
| D02 | Reset | Required | In progress | Oct 20–23 | Clears deployed tools, restores scene and preserves photos |
| Q01 | Full rehearsal | Required | Planned | Oct 24–26 | Demonstration runs through tools, capture, review and reset ([run sheet](REHEARSAL.md) drafted) |
| Q02 | Performance and backup | Required | In progress | Oct 24–26 | Headset performance acceptable; tested build and backup saved (build script ready; build itself unverified) |
| X01 | Intact-store state | Optional | In progress | After core demo | Shared layout switches between intact and robbed states (CrimeSceneState toggle, I key / panel; unverified) |
| X02 | Additional cases | Future | Planned | After presentation | Reusable scene/tool structure established |

## Batch 03 additions

| Feature | Status | Evidence / remaining work |
|---|---|---|
| Refined shop and city geometry | Complete for desktop milestone | Real rendered captures; 4,740 city mesh triangles and 18 renderers |
| Inside desktop spawn and walking | Complete for desktop milestone | Movement and collision assertions passed |
| Exterior access prevention | Complete for desktop milestone | Front/side constraints passed; XR teleport/physical tracking not integrated |
| Full project snapshot on GitHub | Complete | Downloads/JewelryStore_City_Unity6000.6.2f1.zip |

## Batch 04 additions

| Feature | Status | Evidence / remaining work |
|---|---|---|
| Refined furniture, damage and lighting | Complete for desktop milestone | Actual Unity captures in VerificationExpanded |
| Mouse surface placement | Complete for desktop milestone | Verified as DesktopToolPlacement; since folded into DesktopInteractor (pick up / place), unverified |
| Current-view photography | Complete for desktop milestone | Image saved and dedicated camera pose restored |
| Complete expanded snapshot | Complete | Downloads/JewelryStore_Expanded_Unity6000.6.2f1.zip |

## Verification log

Batch 04: 33 Editor Play mode assertions passed. See [report](ENVIRONMENT_BATCH_04.md). Headset and hands-on input/performance checks are still pending.

Batch 03: 22 Editor Play mode assertions passed; see [environment report](ENVIRONMENT_BATCH_03.md). Quest and standalone build checks remain pending.

Environment batch 01 generator uploaded: complete editable store geometry source, awaiting compilation and scene validation. See [batch instructions](ENVIRONMENT_BATCH_01.md).

Batch 02 uploaded: tool lifecycle, tape connections, scene reset, desktop panel, camera feedback/folder access and detailed geometry source. See [batch 02](TOOLS_BATCH_02.md). Basic source delimiter checks passed; functionality remains unverified.

October 8: All source compiled under Unity 6000.6.2f1. Store scene saved and actual images captured. Thirteen core runtime assertions passed in Editor Play mode. Full feature completion still awaits UI/VR checks. Package Manager launch issue remains open; current project runs with -noUpm. [Detailed verification](VERIFICATION_2026-10-08.md).

October 9: batch 13 — six more licensed sample models (street lanterns, watch, plant, candle holder, lamp, chair) and the Tools/glb_to_obj.py converter. Unverified. See [handoff](HANDOFF.md).

October 9: batch 12 — intact/robbed store comparison toggle (X01). Unverified. See [handoff](HANDOFF.md).

October 9: batch 11 — generated click/thud/ambience sounds, emissive light fixtures, credits in the panel, third-party licenses copied into builds. Unverified. See [handoff](HANDOFF.md).

October 9: batch 10 — photo frame on the wall and in the panel, Khronos CC0/CC BY props (armchair, sofa, vase, bottle, sunglasses), TrafficCone tool visual. Unverified. See [handoff](HANDOFF.md).

October 9: batch 09 — rack samples, tool highlight, VR hand labels, presenter overview map and restyled panel, DemoAssetLibrary with Kenney CC0 city models (first third-party assets, see ASSET_REGISTER.md). Unverified. See [handoff](HANDOFF.md).

October 9: batch 08 — single player rig for desktop and VR (DemoModeSwitch, F9), head and controller tracking through the built-in XR input API, VR control mapping. Unverified. See [handoff](HANDOFF.md).

October 9: batch 06 — toolkit-independent XR adapters (HandInteractor, XRLocomotion), camera hand-off, status board, Windows build script, Package Manager troubleshooting notes. Awaiting Unity re-run. See [handoff](HANDOFF.md).

October 8 (later): marker label font, walker grounding and build-scene fixes, then batch 05 desktop interaction (pointer tool handling, camera hold, presenter panel, shared validation code) committed; awaiting Unity re-run. See [handoff](HANDOFF.md).

When testing begins, record date, build/version, device, result and any remaining issue. Do not mark a feature complete merely because its source exists.

## Presentation checklist

- [ ] Quest charged, controllers charged and connection tested.
- [ ] Demonstration build starts reliably.
- [ ] Monitor view visible to visitors.
- [ ] All tools exercised.
- [ ] Photograph folder located and images reviewed.
- [ ] Reset demonstrated without deleting photos.
- [ ] Backup build copied to a separate location.

## Batch 05

Panel merge repaired; generated scene and all source compiled. Forty automated Editor Play mode assertions passed, including instructor review photo copies, tool positions, manual snapshots and before-reset records. Latest snapshot: Downloads/JewelryStore_Review_Unity6000.6.2f1.zip. See [report](REVIEW_BATCH_05.md). UPM normal startup still fails; XR and hands-on input checks remain pending. Merged on October 9 with the Claude batches (DesktopToolPlacement superseded by DesktopInteractor; review checks moved into DemoValidation); that merged state is unverified.

## October 10 geometry pass — verified

Chamfered cabinets, shaped busts, framed niches, storefront/rear-wall depth and tapered cars generated and inspected in actual Unity captures. All source compiled; 72 automated Editor Play mode checks passed. New shop detail: 1,624 triangles / 24 renderers. City: 5,044 triangles / 19 renderers. Fixed nearest-item camera grabbing and camera release before mode switching. Complete snapshot: Downloads/JewelryStore_Forms_Unity6000.6.2f1.zip. See [report and screenshots](GEOMETRY_FORMS.md).

Quest frame time, real controller tracking, hands-on usability and standalone build remain pending. Imported-assets RunExpanded was not rerun; RunForms uses procedural assets.

## October 10 interior finish — verified

Completed the shell pass (walls, floor, lighting and doors), then the glass/display pass. All source and the world-text shader compiled; 81 Editor Play mode checks passed. Added floor/door geometry: 358 triangles / 10 renderers. Four shadowless lights; no new texture maps. Simplified jewelry loops and glass surfaces. Latest download: Downloads/JewelryStore_Interior_Unity6000.6.2f1.zip. See [report and screenshots](INTERIOR_FINISH.md).

V01 now has a verified first material/lighting pass in desktop Unity; final polish and headset readability remain pending. Real Quest performance and a standalone build are still outstanding.

## October 11 — asset preparation (staged, not Unity-integrated)

- [x] Inspect and preserve all ten ReferenceTextures Color/Normal sets.
- [x] Prepare missing ceiling/facade PBR maps, constant roughness support, fictional signage and generic decals.
- [x] Curate existing CC0 models and prepare four CC0 interaction sounds plus original ambience.
- [x] Save sources/licenses, previews, file validation, asset-only package and root for Koda.md.
- [ ] Integrate selected assets after blockout validation; retain pending scenario decisions.
- [ ] Audition audio, inspect in Unity, bake and test Quest comfort/FPS with Koda.

See [asset preparation report](ASSET_PREPARATION_2026-10-11.md). Historical validation claims do not apply to these newly prepared assets.
