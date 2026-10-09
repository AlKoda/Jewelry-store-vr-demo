# Roadmap

Updated: October 8, 2026.
Statuses: Planned / In progress / Blocked / Complete.
Dates are targets, dependent on access to the PC and headset.

| ID | Feature | Priority | Status | Target | Completion criteria |
|---|---|---|---|---|---|
| P01 | Repository and planning documents | Required | Complete | Oct 7–9 | Brief, roadmap, setup and asset register stored on GitHub |
| P02 | Camera starter script | Required | Complete | Oct 7–9 | Portable source saved; runtime validation tracked separately |
| U01 | Unity project setup | Required | Planned | Oct 10–12 | Editor/pipeline/packages recorded; project opens without errors |
| U02 | Quest PC connection | Required | Planned | Oct 10–12 | Head and controller tracking work in a Windows build |
| U03 | Movement | Required | In progress | Oct 10–12 | Desktop walking with shared interior bounds done; teleport and snap turn await XR packages |
| U04 | Store blockout | Required | In progress | Oct 10–12 | Scene generated and saved; scale, traversal and collision checks in Quest remain pending |
| T01 | Cones and markers | Required | In progress | Oct 13–16 | Spawn, grab, place, move and remove objects anywhere suitable (desktop pointer done; XR grab pending) |
| T02 | Scene tape | Required | In progress | Oct 13–16 | Tape spans two posts and follows repositioning |
| T03 | Camera integration | Required | In progress | Oct 17–19 | Controller capture saves readable PNGs; failures shown clearly |
| S01 | Crime scene dressing | Required | In progress | Oct 17–19 | All six agreed evidence/damage elements visible |
| V01 | Visual polish | Required | Planned | Oct 20–23 | Materials and lighting readable in headset |
| D01 | Monitor view and controls | Required | In progress | Oct 20–23 | Visitors see view; presenter can access controls guide (desktop panel with guide done; headset mirror pending) |
| D02 | Reset | Required | In progress | Oct 20–23 | Clears deployed tools, restores scene and preserves photos |
| Q01 | Full rehearsal | Required | Planned | Oct 24–26 | Demonstration runs through tools, capture, review and reset |
| Q02 | Performance and backup | Required | In progress | Oct 24–26 | Headset performance acceptable; tested build and backup saved (build script ready; build itself unverified) |
| X01 | Intact-store state | Optional | Planned | After core demo | Shared layout switches between intact and robbed states |
| X02 | Additional cases | Future | Planned | After presentation | Reusable scene/tool structure established |

## Batch 03 additions

| Feature | Status | Evidence / remaining work |
|---|---|---|
| Refined shop and city geometry | Complete for desktop milestone | Real rendered captures; 4,740 city mesh triangles and 18 renderers |
| Inside desktop spawn and walking | Complete for desktop milestone | Movement and collision assertions passed |
| Exterior access prevention | Complete for desktop milestone | Front/side constraints passed; XR teleport/physical tracking not integrated |
| Full project snapshot on GitHub | Complete | Downloads/JewelryStore_City_Unity6000.6.2f1.zip |

## Verification log

Batch 03: 22 Editor Play mode assertions passed; see [environment report](ENVIRONMENT_BATCH_03.md). Quest and standalone build checks remain pending.

Environment batch 01 generator uploaded: complete editable store geometry source, awaiting compilation and scene validation. See [batch instructions](ENVIRONMENT_BATCH_01.md).

Batch 02 uploaded: tool lifecycle, tape connections, scene reset, desktop panel, camera feedback/folder access and detailed geometry source. See [batch 02](TOOLS_BATCH_02.md). Basic source delimiter checks passed; functionality remains unverified.

October 8: All source compiled under Unity 6000.6.2f1. Store scene saved and actual images captured. Thirteen core runtime assertions passed in Editor Play mode. Full feature completion still awaits UI/VR checks. Package Manager launch issue remains open; current project runs with -noUpm. [Detailed verification](VERIFICATION_2026-10-08.md).

October 9: batch 05 — toolkit-independent XR adapters (HandInteractor, XRLocomotion), camera hand-off, status board, Windows build script, Package Manager troubleshooting notes. Awaiting Unity re-run. See [handoff](HANDOFF.md).

October 8 (later): marker label font, walker grounding and build-scene fixes, then batch 04 desktop interaction (pointer tool handling, camera hold, presenter panel, shared validation code) committed; awaiting Unity re-run. See [handoff](HANDOFF.md).

When testing begins, record date, build/version, device, result and any remaining issue. Do not mark a feature complete merely because its source exists.

## Presentation checklist

- [ ] Quest charged, controllers charged and connection tested.
- [ ] Demonstration build starts reliably.
- [ ] Monitor view visible to visitors.
- [ ] All tools exercised.
- [ ] Photograph folder located and images reviewed.
- [ ] Reset demonstrated without deleting photos.
- [ ] Backup build copied to a separate location.
