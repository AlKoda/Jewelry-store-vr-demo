# Jewelry Store VR Demo

Instructor-led jewelry-store crime-scene demo for October 27, 2026. No scores, timers or forced sequence.

## Latest tested project

[Download the Forms Unity project](Downloads/JewelryStore_Forms_Unity6000.6.2f1.zip) and [view the screenshots and report](Docs/GEOMETRY_FORMS.md).

Extract into **UnityProject_Forms** beside this README, run **Open-Unity-Forms.cmd**, and open **Assets/CrimeSceneDemo/Scenes/JewelryStoreForms.unity**. Unity 6000.6.2f1, Built-in pipeline. The launcher uses the current -noUpm workaround.

October 10: all source compiled and **72 automated Editor Play mode checks passed**. Refined cabinet/bust shapes, wall niches, storefront framing and tapered cars use flat materials and simple geometry. New shop detail: 1,624 triangles / 24 renderers; city backdrop: 5,044 / 19. These counts are not an FPS measurement.

The Forms scene uses procedural environment and tool models. Historical Expanded and Review downloads remain available. This download is a Unity project; standalone build and Quest testing remain pending.

## Controls

- WASD/arrows: walk; hold right mouse: look; Home: return inside.
- Click: pick up/place; 1/2/3: cone, marker or tape post.
- Q/E or wheel: rotate; Delete: remove; T/X: connect/cancel tape.
- F: hold/return camera; P: photograph.
- I: intact/robbed shop; Tab: presenter panel; F9: desktop/VR mode.
- Panel: reset, photographs and instructor review.

## Development

Prototype contains editable source; Downloads contains generated projects with Assets, .meta files and ProjectSettings. Keep current source and snapshots together. ShopCityValidation.RunForms regenerates the procedural variant and its verification report; RunExpanded retains the imported-assets variant.

VR adapters pass programmatic checks, but actual tracking requires OpenXR setup and Quest testing. Package Manager startup remains unresolved. See [setup](Docs/SETUP.md), [controls](Docs/CONTROLS_AND_INTEGRATION.md), [handoff](Docs/HANDOFF.md), [roadmap](Docs/ROADMAP.md), [brief](Docs/PROJECT_BRIEF.md), [rehearsal](Docs/REHEARSAL.md) and [asset register](Docs/ASSET_REGISTER.md).
