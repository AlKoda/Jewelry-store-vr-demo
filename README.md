# Jewelry Store VR Demo

Instructor-led jewelry-store crime-scene prototype for the October 27, 2026 demonstration.

## Latest project

October 9: the verified review project is [JewelryStore_Review_Unity6000.6.2f1.zip](Downloads/JewelryStore_Review_Unity6000.6.2f1.zip) (40 assertions, [batch 05 report](Docs/REVIEW_BATCH_05.md)): the panel saves local instructor reviews with photographs and tool positions. The merged source below includes that recorder plus the unverified desktop/VR player rig; see the [handoff](Docs/HANDOFF.md) for what to run.

[Download the expanded Unity project](Downloads/JewelryStore_Expanded_Unity6000.6.2f1.zip) and [see screenshots and the batch report](Docs/ENVIRONMENT_BATCH_04.md).

Extract the snapshot into UnityProject alongside this README. Run Open-Unity-Blockout.cmd and open **Assets/CrimeSceneDemo/Scenes/JewelryStoreExpanded.unity**.

Unity 6000.6.2f1, Built-in pipeline. Batch 04 (expanded shop) compiled and passed 33 Editor Play mode assertions on the user's PC. Batches 05 and 06 (pointer tool handling, presenter panel, XR-ready adapters, build script) are in the source and snapshot but have not been run in Unity yet: run `ShopCityValidation.RunExpanded` to regenerate the scene and verify them. This is a desktop prototype; Quest controls and standalone build testing remain pending. Package Manager currently needs the documented -noUpm workaround.

## Controls

- WASD/arrows: walk inside the shop (Shift runs). Hold right mouse: look. Home: return to the start.
- Left click: pick up the pointed tool, or place the held one. 1/2/3: new cone, marker or tape post into the hand.
- Q/E or mouse wheel: rotate the held tool. Delete: remove it. T: select tape posts; X: cancel tape.
- F or click the camera: hold it / put it back. P: photograph (your view, or through the held camera).
- Tab: hide the presenter panel. Full list in [controls and integration](Docs/CONTROLS_AND_INTEGRATION.md).

## VR

The same scene runs in VR: with OpenXR enabled and a headset active the Player switches to head and controller tracking at start; otherwise it is the mouse-and-keyboard demo. F9 flips modes for testing. Controller mapping in [controls and integration](Docs/CONTROLS_AND_INTEGRATION.md).

## Build

Build-Windows.cmd produces a standalone Windows demo in Builds\Windows; Run-Demo.cmd starts it. XR-ready movement and hand scripts are in place without toolkit dependencies; see [setup](Docs/SETUP.md) for the package plan and Package Manager troubleshooting.

## Project documents

- [Brief](Docs/PROJECT_BRIEF.md)
- [Roadmap](Docs/ROADMAP.md)
- [Current handoff](Docs/HANDOFF.md)
- [Demonstration run sheet](Docs/REHEARSAL.md)
- [Setup](Docs/SETUP.md)
- [Controls and integration](Docs/CONTROLS_AND_INTEGRATION.md)
- [Asset register](Docs/ASSET_REGISTER.md)
- [Scene layout](Docs/SCENE_LAYOUT.md)
- [Batch 04: expanded shop and mouse placement](Docs/ENVIRONMENT_BATCH_04.md)
- [Batch 03: city and inside walking](Docs/ENVIRONMENT_BATCH_03.md)
- [Batch 02: tools](Docs/TOOLS_BATCH_02.md), [batch 01: store generator](Docs/ENVIRONMENT_BATCH_01.md)
- [October 8 verification](Docs/VERIFICATION_2026-10-08.md)

## Repository structure

```
Docs/                  Planning, setup, asset records, verification and handoff
Prototype/Scripts/     Runtime C# (copied into the Unity project)
Prototype/Editor/      Scene generators, validators and the build script
Downloads/             Complete generated Unity project snapshots
UnityProject/          Created on the Windows PC by extracting a snapshot
```

## Demo behavior

Explore freely, place cones, scene tape and numbered markers, take saved photographs, and show the headset view on a monitor. Guidance and review come from the instructor. No scores, timers, forced sequence or automatic judgments.

## Collaboration

Update the roadmap and handoff after changes. Keep Unity .meta files alongside assets. Update source and the project snapshot together. Record third-party licenses before committing assets. Original project licensing has not yet been selected.
