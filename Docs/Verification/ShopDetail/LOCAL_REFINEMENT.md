# Local shop and all-glass refinement — October 23, 2026

## Delivered scene changes

- Approved door silhouettes/openings retained: only small jagged perimeter remnants,
  never a sheet across the middle. Two-sided hanging sign has a real 9 mm backing
  and thin hangers. Handle mounting arms attach to the frame. Two legacy UV marks
  formerly over missing glass now sit on the metal stile; six handle traces remain.
- 57 intact panes across thirteen case assemblies have closed thickness and 0.45 mm
  polished bevels, preserving original bounds/renderer batching/collision. Separate
  indoor polish, weathered windows, fractured faces, cut edges, loose shards and
  small-prop glass. Main pane absorption .13-.14 versus previous .055; cut edges
  .235-.24. Natural Fresnel/probe reflections, no green opaque tint or screen grab.
- One optional extra map sample adds faint real photographic polish variation.
  Source: existing PBRInterior GlassSmudge_ColorAlpha.png, CC0 ambientCG derivative;
  source licences/provenance retained. No new normal distortion or extra cameras.
- Six individually seeded case fractures use real case dimensions; legacy stretched
  window meshes/scatters are inactive. Three deck/floor sets each have 70/140 native
  pieces. Fine crumbs mix compact three/four-sided solids; other fragments mix wider
  chips and narrower splinters, with closed thickness and contact shading.
- Fine coverage uses the image as a coverage cue with restrained diffuse response;
  specularity is coverage-masked, removing the old full-quad glossy rectangle and
  strong baked photographic highlights. Fine 3D glass scattering is separately tuned.
  Existing architectural fragments/fine piles retained, no per-piece physics.
- Repair box: hollow fabric-lined shell, hinged lid, metal latch/hardware, partition,
  physical Arabic intake sheet and small telescoping watch spring bar. A short blue
  thread sits by the office paperwork. Forceps collection requires open lids.
- Existing staff-door/counter UV traces now support two designated UV observations;
  UV off, incorrect cone, opaque blockers and reset are tested. Background marks
  remain visual context. Fluorescence/colour alone never establishes an identity.
- Eighteen stable clues, ten sample points/packets. Register has a survey overview,
  shows the actual latest selected entry, and fits long Arabic/English notes.
- Static municipal light radius .42 m and shop exterior radius .24 m with restrained
  indirect contribution. Interior practicals remain OFF. No new lights.

## Verification

Builds/shop-detail-refine.log: GLASS_FAMILY_AUTHORED (57 panes/six case fractures),
GLASS_FAMILY_CHECKS_PASS, SHOP_SEARCH_BEHAVIOUR_PASS, FIELDWORK_BEHAVIOUR_PASS,
ARCHITECTURAL_GLASS_CHECKS_PASS, GENERAL_SCENE_REVIEW_PASS,
TORCH_RENDERED_SHADOW_PASS (white/UV and two quality tiers),
CLEAN_REFLECTION_CAPTURE_PASS and SHOP_DETAIL_REFINEMENT_READY.

Fresh bake: 19.5 seconds, three lightmaps (largest 1024), 676 probes; fourteen
reflection captures. Architectural pane/debris assembly 53,632 triangles; counts
exclude fine piles and case groups and are not a device frame-time measurement.
Current fieldwork hierarchy 6,274 authored triangles, 6,658 in play-mode scan.
No additional realtime lights, rigidbodies or UVTrace components. Case glass remains
combined; the optional wear sample is only enabled on large polished/window faces.

Builds/shop-detail-runtime.log: actual Start/event/JSON lifecycle and completed-file
retention, portable case/tool torch replacement, all earlier carry/dock/support,
brush/lift and new box/UV discovery/reset checks pass. Logs create verification
records under persistentDataPath; no user photos/records were deleted.

Builds/shop-detail-delivery.log: FINE_GLASS_VISIBILITY_PASS (22,332 changed pixels),
GLASS_FAMILY_CHECKS_PASS, VR_VISUAL_VALIDATION_PASS and SHOP_DETAIL_DELIVERY_PASS.
The strict existing-scene Windows build serves keyboard and PCVR. No new standalone
APK was built/installed. Ten actual scene views: UnityProject_Demo/Builds/
shop-detail-after-0.png through -9.png (doors, threshold, cases, repair box, window,
rear door face, street lighting, close fracture and low floor debris).

Source/GUID/asset parity and scene/player hashes: DELIVERY.json. Car's entire
21-transform hierarchy/mesh/material references and official ZIP: PROTECTION.json.
No commit/push or official ZIP refresh; the Oman local-review restriction remains.
Incremental checkpoint Before-Shop-Detail-20261023 retains old generated assets and
lighting as well as scene/source. Accepted controller/camera/torch grips and
Claude-owned files were not edited in this pass.

## Test next in the headset

Compare intact panes at normal/grazing angles, fracture openings/cut edges and floor
pile visibility under both lights. Check stereo reflections, fine-glint stability,
UV inspection/reach, tiny-part pickup, register text and actual frame time. Current
Windows renders and simulated-controller checks do not establish Quest performance.
