# Jewelry Store VR Demo

## Run it — double-click Start-Demo.cmd

On the Windows PC with Unity 6000.6.2f1 installed, double-click **Start-Demo.cmd** in this folder. The first time it unpacks the project into UnityProject_Demo and builds **Builds\Windows\JewelryStoreDemo.exe** (5–15 minutes, no Unity window); after that it only rebuilds when the source changed (git checkout) and otherwise starts the existing build at once. The program opens on a start menu listing the maps, newest first, with the build date and source commit: Enter starts the night store, F10 in a map returns to the menu. `Start-Demo.cmd bake` rebuilds with baked lighting (slower, final look); `Start-Demo.cmd rebuild` forces a rebuild after downloading a newer ZIP; `Start-Demo.cmd run` just starts it. If a build fails, the window lists the errors from build-latest.log. Copy the whole Builds\Windows folder to show the demo on another PC; that PC needs no Unity. *Not yet run on the PC (unverified).*

Instructor-led jewelry-store crime-scene demo for October 27, 2026. No scores, timers or forced sequence.

## Night store source — October 11 (unverified)

The expanded night store is now in source: run NightStoreValidation.RunNightQuick (no bake) or RunNight (bake) after syncing Scripts, Editor, Shaders and ThirdParty. See the top of [HANDOFF](Docs/HANDOFF.md).

## Current plan — October 11

The next scene is an **expanded store with a nighttime police arrival**, reusing existing tools and low-poly assets. See the [scenario and migration plan](Docs/NIGHT_ARRIVAL_PLAN.md). The reference is now received: see the [reference-based layout plan](Docs/REFERENCE_LAYOUT_PLAN.md), with proposed dimensions and asset reuse. This is planning work, not a new build.

## Glass project (now contains unverified night-store sources)

[Download the Glass and Tools Unity project](Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip) and [view the screenshots/report](Docs/GLASS_AND_TOOLS.md).

Extract into **UnityProject_Glass** beside this README, run **Open-Unity-Glass.cmd**, and open **Assets/CrimeSceneDemo/Scenes/JewelryStoreGlass.unity**. Unity 6000.6.2f1, Built-in pipeline; launcher uses the current -noUpm workaround.

October 10: all source and the world-text shader compiled; **137 automated Editor Play mode assertions passed**. That result applies to the October 10 snapshot (`git show d5e9cc8:Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip`). The current zip (October 11) also carries the later textured-finish, panel, measuring-tape, lighting, lamp, UV, StoreKit and NightStore sources; the newest of these have **not been compiled**; a compile error in them would stop JewelryStoreGlass from entering Play mode. Its Shaders folder also lacks UVFluorescence.shader.meta (Unity generates one on import). Glass fragments now rest on surfaces, marker numbers no longer show through, and tape posts connect automatically when placed. Robbed stock includes empty pads, overturned stands and dropped jewelry. The menu starts closed; Tab opens it. First finishes for walls, stone flooring, lights, doors, glass and display cases use simple geometry and flat materials. Four shadowless lights; one static room reflection cubemap. Quest performance/tracking and standalone build remain pending.

Historical Forms, Expanded and Review projects remain available. This download is a Unity project, not an executable.

## Controls

Tested Glass build (October 10):

- WASD/arrows: walk; right click: toggle mouse look; Escape: release cursor; Home: return inside.
- Click: pick up/place; 1/2/3/4: cone, marker, tape post or L-scale (photo size reference).
- Q/E or wheel: rotate; R/Delete/Backspace: remove pointed tool; T/X: manually connect/cancel tape.
- F: hold/return camera; P: photograph.
- I: intact/robbed shop; Tab: presenter panel; F9: desktop/VR mode.
- Place tape posts to connect automatically; panel can start a separate run.
- Panel: placed-tool Remove buttons, reset, photographs and instructor review.

Added in the current source (unverified): 5/6: measuring tape or scene lamp; T also switches a held or pointed scene lamp (at most two lit); L: flashlight; U: UV torch; N: night/day; B: instructor beacon at the pointer; Tab cycles the presenter panel hidden/strip/full; [ / ]: panel scale; Shift: walk faster. Full table: [controls](Docs/CONTROLS_AND_INTEGRATION.md).

## Development

Prototype contains editable source; Downloads contains generated projects with Assets, .meta files and ProjectSettings. Keep current source and snapshots together. ShopCityValidation.RunGlass regenerates the finished procedural variant and its verification report; RunTextured adds the generated/scanned textures, imported CC0 kits and HDR sky (unverified); copy Prototype/Shaders alongside Editor and Scripts when syncing source; RunExpanded retains the imported-assets variant.

VR adapters pass programmatic checks, but actual tracking requires OpenXR setup and Quest testing. Package Manager startup remains unresolved. See [setup](Docs/SETUP.md), [controls](Docs/CONTROLS_AND_INTEGRATION.md), [handoff](Docs/HANDOFF.md), [roadmap](Docs/ROADMAP.md), [brief](Docs/PROJECT_BRIEF.md), [rehearsal](Docs/REHEARSAL.md) and [asset register](Docs/ASSET_REGISTER.md).
