# Local evidence and interaction refinement — October 23, 2026

One saved JewelryStoreNight scene, on local codex/oman-street-preview. No GitHub
promotion or official snapshot update. Police car unchanged. Existing accepted
camera/torch grips, belt, architectural glass, Oman street and five animated
spectators remain. Claude-owned scripts were not edited.

## Added

- A compact two-level fieldwork cart beside the front-right seating area, clear
  of the checkout counter props. Satin metal, rubber tray, paper clipboard and
  sample packets; all new fieldwork meshes total **2,536 triangles**. No new lights.
- Two separate reusable field-kit tools: evidence forceps and a sterile swab.
  They do not change the six placement-tool slots or the existing controller
  wheel. They are trigger-only, with no rigidbodies or throwing forces. Release
  returns them to their authored tray positions. Taking another placement tool,
  camera, changing mode or resetting releases a field tool. Equipping a torch on
  the same VR hand also returns the field tool instead of stacking both models.
- Twelve stable evidence IDs. Six are sample targets: two small existing floor
  transfers, existing blue fibres, existing broken clasp, a loose glass fragment
  and a cut cable tie. Others cover a pouch, partial sole mark, stock tag, service
  receipt, staff reminder and safe-frame scuffs. Existing blood/UV traces and fine
  glass accumulations remain. No automatic identity, blood identification or
  attribution to a suspect is asserted.
- A closed hollow document box on the office drawer cabinet. Its lid opens above
  its rear hinge, exposing an Arabic service receipt. The document cannot be
  inspected while closed. A nearby staff note adds ordinary shop context.
- A physical paged evidence register. UI language switching applies to its
  dynamic text; permanent shop documents/guide remain Arabic. Records include
  stable ID, observation, action, UTC timestamp and position. Successfully saved
  camera photo paths are recorded separately; no unsupported automatic claim
  that a particular photo proves a particular clue. Sealed packets appear as
  samples are collected.
- Runtime records are written under Application.persistentDataPath/EvidenceRecords.
  Reset restores sampled specimens, closes lids, returns field tools and starts
  a new register. Completed JSON files and photographs are retained.

## Controls

Keyboard/mouse: near an item, **G or left click** inspects, opens/closes the box,
takes/uses a field tool or advances the register. **R** returns a field tool.
Collection reach is 2 m from the view. Normal placement-tool controls remain.

VR: grip the tray tool within reach; use the **right trigger** to inspect or use
it; release grip to return it. Sample collection requires the controller to be
within .65 m. Empty right trigger operates the document box and register. Left
trigger retains the established tool-wheel behavior. No extra hand labels.

## Verification

- FieldworkChecks / NightStoreValidation.FieldworkChecks: unique IDs, six targets,
  two-tool exclusivity, controller ray targeting, opaque occlusion, correct and
  wrong tool, bounded reach, duplicate prevention, specimen visibility, tray return,
  container access, desktop collection, Arabic/English page output, JSON round-trip
  and reset passed. Builds/fieldwork-refine.log.
- Actual Unity play mode: component Start/events, collection/reset checks, real
  JSON disk write and retention across the session reset, animated lid update
  passed. Builds/fieldwork-runtime.log. A verification JSON record was deliberately
  written to the application's EvidenceRecords folder; it is a test observation.
- Targeted save, 14.4 s fresh GPU bake: three lightmaps up to 1024, 676 probes,
  fourteen refreshed reflection captures. Field tools and dynamic register ink
  share the crowd's reflection-excluded render layer to avoid frozen reflections.
- Existing keyboard/VR tool safety, white/UV rendered shadows, shop model checks,
  original FBX car, street bounds and five-actor crowd structure passed.
- Actual close-up scene images: UnityProject_Demo/Builds/fieldwork-0.png through -3.
  These are scene renders, not generated concepts. Initial overlapping placements
  were rejected before saving; the kit uses its own cart and the box uses the
  office cabinet.
- Final strict Windows existing-scene build passed: FIELDWORK_DELIVERY_PASS,
  VR_VISUAL_VALIDATION_PASS in Builds/fieldwork-delivery.log. Source/Unity files and
  GUIDs match; hashes and checks in DELIVERY.json. PROTECTION.json confirms all 21
  police car transforms/mesh/material references and the official ZIP are unchanged.
  Same-hand torch replacement passed in actual play mode. The physical register
  uses the shared depth-tested font-atlas refresh helper for long/changing text.
  Quest grip fit, stereo legibility and frame time remain pending.

Entry points: PresentationPolish.FieldworkRefinementRun (targeted author/check/
save/bake/render), FieldworkDeliveryRun (check/build the saved scene),
FieldworkPlayVerification.Run (real play-mode lifecycle and persistence check).
Do not regenerate the entire scene with CombinedBuild. InvestigationRefinementRun
now rebinds fieldwork if it replaces the older physical evidence meshes.

Incremental pre-change checkpoint: ../Backups/Before-Evidence-Gameplay-20261023.
The earlier full pre-Oman backup remains available. This checkpoint is not a
second deliverable or an official release.
