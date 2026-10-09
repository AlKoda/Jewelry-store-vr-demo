# Jewelry Store VR Demo

An instructor-led VR jewelry-store crime-scene sandbox for an opening-day demonstration on **October 27, 2026**.

## Latest milestone

[Refined shop, indoor walking and city street](Docs/ENVIRONMENT_BATCH_03.md) — 22 Editor Play mode assertions passed.

[Download the latest complete Unity project](Downloads/JewelryStore_City_Unity6000.6.2f1.zip). Extract into UnityProject, run Open-Unity-Blockout.cmd, and open Assets/CrimeSceneDemo/Scenes/JewelryStoreCity.unity.

WASD/arrows walk (Shift runs); hold right mouse to look; left click picks up and places tools; 1/2/3 spawn a cone, marker or tape post into the hand; T connects tape posts; F holds the camera and P photographs; Tab hides the presenter panel; Home returns to the inside start. The street is visible through the storefront while the player remains inside. Full list in [controls and integration](Docs/CONTROLS_AND_INTEGRATION.md).

Build-Windows.cmd produces a standalone Windows demo in Builds\Windows; Run-Demo.cmd starts it. XR-ready movement and hand scripts are in place without toolkit dependencies; see [setup](Docs/SETUP.md) for the package plan.

## Previous verified milestone

A generated Unity 6000.6.2f1 project snapshot and actual scene captures are available. Scripts compiled and 13 core runtime assertions passed in Editor Play mode on the user's PC. Quest setup and standalone build verification remain pending. See [verification report](Docs/VERIFICATION_2026-10-08.md), including the temporary Package Manager workaround.

## Start here

- [Previous blockout snapshot](Downloads/JewelryStore_Unity6000.6.2f1.zip).
- [Verified scene screenshots and results](Docs/VERIFICATION_2026-10-08.md).

- [Project brief](Docs/PROJECT_BRIEF.md): agreed scope and hardware.
- [Roadmap](Docs/ROADMAP.md): progress, priorities and completion criteria.
- [Setup](Docs/SETUP.md): PC setup and camera integration.
- [Scene layout](Docs/SCENE_LAYOUT.md): provisional store design.
- [Asset register](Docs/ASSET_REGISTER.md): sources and licensing.
- [Handoff](Docs/HANDOFF.md): instructions for continuing with an assistant.
- [Tools and detail batch 02](Docs/TOOLS_BATCH_02.md): import the whole source batch.
- [Controls and integration](Docs/CONTROLS_AND_INTEGRATION.md).
- [Environment batch 01](Docs/ENVIRONMENT_BATCH_01.md): store generator and import instructions.
- [Camera prototype](Prototype/Scripts/EvidenceCamera.cs).

## Repository structure

```
Docs/                  Planning, setup, asset records and handoff
Prototype/Scripts/     Portable C# scripts awaiting Unity integration
Prototype/Editor/      Unity Editor environment generator
UnityProject/          To be created on the Windows PC
```

Download using **Code → Download ZIP**, or clone this repository with GitHub Desktop. Extract the project snapshot into UnityProject alongside this README, then run Open-Unity-Blockout.cmd. The snapshot has no XR packages yet.

## Demo behavior

Explore freely, place cones, scene tape and numbered markers, take saved photographs, and show the headset view on a monitor. Guidance and review come from the instructor. No scores, timers, forced sequence or automatic judgments.

## Collaboration

Update the roadmap after changes. Record the Unity and package versions when setup begins. Keep Unity .meta files alongside assets. Record third-party licenses before committing assets. Original project licensing has not yet been selected.
