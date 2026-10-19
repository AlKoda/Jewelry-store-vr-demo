# Expanded store: nighttime first-response scenario

**Eight generated modeling references are now available:** [gallery and original PNGs](References/LayoutReferences/NightStore/README.md). These are concept images, not a tested build; use the written layout for dimensions and read the gallery's consistency notes.
October 11, 2026 — planning only; no scene, code or asset changes made.

## Direction and scenario
A nearby resident hears a disturbance during the night and calls police. The responding officer arrives to find a jewelry store broken into and ransacked. The instructor uses the scene to discuss securing the area, identifying potential evidence and preserving/documenting it until further assistance arrives.

This remains a freely explorable, instructor-led demonstration: no forced action order, scoring, countdown, automatic evidence identification or mandatory tutorial. The instructor supplies operational procedure. No suspects, casualties or active confrontation are assumed for this version.

The expanded store should closely follow the user's visual reference. **The reference has now been received and inspected: [Docs/References/store-reference.jpg](../Docs/References/store-reference.jpg).** See [REFERENCE_LAYOUT_PLAN.md](REFERENCE_LAYOUT_PLAN.md) for observed features, proposed dimensions and the migration order. Dimensions and obscured rooms remain estimates; do not scale up the entire old scene uniformly.

Store lighting is off at arrival. Nighttime and lights being off are confirmed; deliberate power sabotage is not established by the story. Do not add a cut cable or electrical clue without a case decision.

## What the visitor experiences
1. Proposed starting implementation: spawn standing beside a parked police car facing the frontage, with headlights on. Arrival is implied; driving and a moving VR vehicle are deferred.
2. Explore a small accessible exterior staging area, see the broken frontage and deploy cones/tape as desired.
3. Walk or teleport through the entrance and inspect the store using a white flashlight; optionally use a UV torch or portable scene lamp.
4. Place clue markers and photograph chosen views with an unbranded DSLR-style camera.
5. Instructor and visitors review actual saved photographs and placed tools. Reset restores the nighttime case.

These describe available activities, not an enforced sequence. The player can stay as long as needed.

## Preserve and reuse
| Existing work | Treatment |
|---|---|
| Current small-shop scene, source history and screenshots | Preserve as a comparison and fallback |
| Display cases, pads, jewelry, safe, doors and fracture meshes | Reposition modular pieces at their original real-world size |
| Robbed/intact stock and scattered evidence | Rebuild positions for the new layout; preserve state switching |
| Cones, markers and automatic tape connections/removal | Retain interactions; extend valid placement to the staging area |
| EvidenceCamera, screenshot saving and review records | Retain behavior; replace only the camera's visual shell as needed |
| Desktop/VR adapters, teleport and controller tools | Reuse after verifying current source in Unity |
| KayKit police car and selected furniture | Inspect scale/style, then reuse; car model availability does not establish functional headlights |
| DemoLighting and flashlight code | Evaluate as a foundation; current newer batch is not yet Unity-verified |
| Texture library | Keep available, but defer texture work until the new blockout works |
| UV torch and portable scene lamp | New planned capabilities, not currently verified implementations |

Use a generic black DSLR-style body, lens, grip and shutter button, without a Canon logo. An exact camera model requires a separate reference; do not delay the first blockout for it.

## Layout organization
Functional zones, now organized around the supplied reference (see REFERENCE_LAYOUT_PLAN.md):
- Parked response car and equipment access.
- Small walkable pavement/frontage area for tape and cones.
- Broken storefront and accessible entrance.
- Expanded showroom with disrupted display areas.
- Service counter and rear office/safe area.
- Visual-only neighborhood beyond the accessible perimeter.

Keep larger floor/wall sections modular. Move displays and add modules instead of stretching cases, jewelry, tools or doors. Preserve readable walking routes and evidence visibility.

Give spawn points, car pose, door openings, tool access and evidence locations named anchors in one layout definition. Derive movement/placement regions, overview map framing and review sketches from the same layout information where practical. Avoid scattering new hard-coded coordinates across builders and validators.

### Critical migration dependencies
The existing InteriorBounds contains two small-shop rectangles. These currently constrain desktop movement, teleport and tool placement. The new scene needs connected regions for staging, entrance, showroom and rear rooms, plus physical colliders.

The current IndoorBoundary_Front blocks the whole storefront. In the new scene, retain solid wall/window protection but provide the intended entrance route. Put the outer boundary beyond the accessible staging area. Tape is a placed training prop; it should not create an invisible movement wall.

Recalculate overview/map click conversion, beacon limits, review SVG bounds, saved-photo camera positions and test locations. Replace old assertions that universally reject the street with new assertions that allow designated staging space and reject the distant background.

## Night lighting
- Store fixtures start off, including emissive fixture materials. A bright dusk sky is not an adequate midnight setup.
- Headlight cones point at the frontage and illuminate surfaces; meshes glowing by themselves do not light the scene.
- White flashlight follows the held/controller pose, with a practical desktop equivalent.
- UV mode affects only explicitly authored fluorescent trace examples inside the beam, range and line of sight. A purple tint alone is insufficient; traces must not show through walls.
- UV fluorescence is an observation cue, not proof of material identity. Do not make every fingerprint or stain automatically glow. Choose and document the simulated trace with the instructor.
- Optional portable LED lamp can be placed and aimed. Start with a simple stand and one light.
- Keep controls readable without making the store uniformly bright. Provide an instructor brightness adjustment if needed.
- Re-bake/select reflections for the night state. Daytime glass reflections must not remain bright in an otherwise dark shop; reset/state changes must restore matching lighting.

Provisional performance strategy: two headlight spotlights, one active inspection beam, optional one lamp; profile overlap before setting the final light/shadow budget. Reuse emission for headlamp appearance. Start with tightly limited shadow ranges/resolutions, and verify that beams do not illuminate through solid walls. Cheap shadowless lights alone do not provide this occlusion.

Use low-poly geometry, limited transparent surfaces, simple materials and static background buildings. No realtime reflection cameras or volumetric fog for the first version. Measure on the actual PC/Quest connection before promising FPS. Prior scene triangle/check counts do not validate this larger scene.

## Case dressing
Reuse broken glass, damaged cases, empty pads, overturned stands, hurriedly dropped jewelry, shoe trace, open safe and discarded torch as candidate scene elements. Positions should tell a coherent route from entry through the showroom to the safe, with useful photograph angles. The reference layout and final case decide which elements remain.

Keep the existing shoe trace readable in white light. Add a separate, deliberately authored UV demonstration trace only after its intended meaning is agreed. Do not add new case conclusions solely to justify an effect.

## Work batches and completion criteria
| Batch | Work | Completion evidence |
|---|---|---|
| 0 — Reference and baseline | Obtain reference; identify facade, footprint, room connections and approximate scale. Verify latest source separately from historical build. | Annotated proposed floor plan, asset shortlist, recoverable baseline |
| 1 — Expanded blockout | New scene such as JewelryStoreNightArrival; modular shell; reused cases/counter/safe; parked car and staging area | Top-down plan plus entrance/showroom screenshots; player-size proportions checked |
| 2 — Access and perimeter | Update spawn, bounds, entrance collisions, teleport, exterior tool placement and map/review coordinates | Car-to-store-to-safe route works; valid staging accepts tape/cones; distant city remains inaccessible |
| 3 — Night arrival | Fixtures off; headlights, white flashlight, matching night reflections and usable exposure | Same camera views in diagnostic daylight and scenario night; beams/occlusion checked |
| 4 — Inspection equipment | Generic DSLR visual, UV beam/trace, optional lamp; reuse marker/tape/removal/photo systems | Saved photos show actual light/UV state; moving/removing tools works inside and outside |
| 5 — Case and rehearsal | Ransacked layout, traces, reset, intact comparison, monitor view and Quest profiling | Instructor can run a complete untimed demo; reset preserves photos and restores the case |

Keep batch 4's portable lamp as a stretch item if headlights plus flashlight are sufficient. Prioritize the enlarged reference layout, accessible perimeter, night illumination and existing evidence tools before textures or additional decorative props.

## Repository and collaboration
- This document is the current design direction, superseding the store-texturing pass as the next priority.
- Preserve historical scenes/downloads; build this scenario in a separate scene and isolated working project first.
- Keep layout geometry, case state and lighting separate so later cases can reuse the store.
- Publish each completed batch with source, a matching generated project when available, screenshots and an honest validation report.
- Do not label current unverified source or this plan as a tested build.
- Work toward the October 27 demo date, with layout/blockout decisions first and visual finish afterward.

## Information still needed
The reference image is received and sufficient to start the blockout. Additional angles or one known measurement would improve accuracy but are not required to begin. Hidden rooms and working dimensions remain explicitly marked assumptions.

Optional: approximate frontage width (or one known-size object), whether the intended break-in is through the large window or door, and what UV demonstration trace the instructor wants. Proposed default arrival is beside a parked car; a driving sequence is not included in the initial scope.
