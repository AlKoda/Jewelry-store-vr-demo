# Jewelry Store VR Demo

Instructor-led jewelry-store crime-scene demo for October 27, 2026. No scores, timers or forced sequence.

## Latest tested project

[Download the Interior Unity project](Downloads/JewelryStore_Interior_Unity6000.6.2f1.zip) and [view the screenshots/report](Docs/INTERIOR_FINISH.md).

Extract into **UnityProject_Interior** beside this README, run **Open-Unity-Interior.cmd**, and open **Assets/CrimeSceneDemo/Scenes/JewelryStoreInterior.unity**. Unity 6000.6.2f1, Built-in pipeline; launcher uses the current -noUpm workaround.

October 10: all source and the world-text shader compiled; **81 automated Editor Play mode checks passed**. First finishes for walls, stone flooring, lights, doors, glass and display cases use simple geometry and flat materials. Four shadowless lights; no new texture maps. Quest performance/tracking and standalone build remain pending.

Historical Forms, Expanded and Review projects remain available. This download is a Unity project, not an executable.

## Controls

- WASD/arrows: walk; hold right mouse: look; Home: return inside.
- Click: pick up/place; 1/2/3: cone, marker or tape post.
- Q/E or wheel: rotate; Delete: remove; T/X: connect/cancel tape.
- F: hold/return camera; P: photograph.
- I: intact/robbed shop; Tab: presenter panel; F9: desktop/VR mode.
- Panel: reset, photographs and instructor review.

## Development

Prototype contains editable source; Downloads contains generated projects with Assets, .meta files and ProjectSettings. Keep current source and snapshots together. ShopCityValidation.RunInterior regenerates the finished procedural variant and its verification report; copy Prototype/Shaders alongside Editor and Scripts when syncing source; RunExpanded retains the imported-assets variant.

VR adapters pass programmatic checks, but actual tracking requires OpenXR setup and Quest testing. Package Manager startup remains unresolved. See [setup](Docs/SETUP.md), [controls](Docs/CONTROLS_AND_INTEGRATION.md), [handoff](Docs/HANDOFF.md), [roadmap](Docs/ROADMAP.md), [brief](Docs/PROJECT_BRIEF.md), [rehearsal](Docs/REHEARSAL.md) and [asset register](Docs/ASSET_REGISTER.md).
