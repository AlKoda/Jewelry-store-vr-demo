# Jewelry Store VR Demo

Instructor-led jewelry-store crime-scene demo for October 27, 2026. No scores, timers or forced sequence.

## Latest tested project

[Download the Glass and Tools Unity project](Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip) and [view the screenshots/report](Docs/GLASS_AND_TOOLS.md).

Extract into **UnityProject_Glass** beside this README, run **Open-Unity-Glass.cmd**, and open **Assets/CrimeSceneDemo/Scenes/JewelryStoreGlass.unity**. Unity 6000.6.2f1, Built-in pipeline; launcher uses the current -noUpm workaround.

October 10: all source and the world-text shader compiled; **137 automated Editor Play mode assertions passed**. Glass fragments now rest on surfaces, marker numbers no longer show through, and tape posts connect automatically when placed. Robbed stock includes empty pads, overturned stands and dropped jewelry. The menu starts closed; Tab opens it. First finishes for walls, stone flooring, lights, doors, glass and display cases use simple geometry and flat materials. Four shadowless lights; one static room reflection cubemap. Quest performance/tracking and standalone build remain pending.

Historical Forms, Expanded and Review projects remain available. This download is a Unity project, not an executable.

## Controls

- WASD/arrows: walk; right click: toggle mouse look; Escape: release cursor; Home: return inside.
- Click: pick up/place; 1/2/3: cone, marker or tape post.
- Q/E or wheel: rotate; R/Delete: remove pointed tool; T/X: manually connect/cancel tape.
- F: hold/return camera; P: photograph.
- I: intact/robbed shop; Tab: presenter panel; F9: desktop/VR mode.
- Place tape posts to connect automatically; panel can start a separate run.
- Panel: placed-tool Remove buttons, reset, photographs and instructor review.

## Development

Prototype contains editable source; Downloads contains generated projects with Assets, .meta files and ProjectSettings. Keep current source and snapshots together. ShopCityValidation.RunGlass regenerates the finished procedural variant and its verification report; copy Prototype/Shaders alongside Editor and Scripts when syncing source; RunExpanded retains the imported-assets variant.

VR adapters pass programmatic checks, but actual tracking requires OpenXR setup and Quest testing. Package Manager startup remains unresolved. See [setup](Docs/SETUP.md), [controls](Docs/CONTROLS_AND_INTEGRATION.md), [handoff](Docs/HANDOFF.md), [roadmap](Docs/ROADMAP.md), [brief](Docs/PROJECT_BRIEF.md), [rehearsal](Docs/REHEARSAL.md) and [asset register](Docs/ASSET_REGISTER.md).
