# Jewelry Store VR Demo

An instructor-led VR crime-scene demo for police training: a jewellery store at night after a break-in, with the police just arrived. The trainee walks the scene, places cones, evidence markers, tape, scales and scene lamps, photographs evidence and uses white and UV torches. A presenter guides it from the PC. There are no scores, timers or forced sequence. Demo date: **October 27, 2026**.

Unity 6000.6.2f1 (Built-in pipeline) on a Windows PC, with keyboard and mouse or a Meta Quest 3 over Quest Link / Air Link.

## Start here (Windows PC)

| Double-click | For |
|---|---|
| **Play-Keyboard.cmd** | The demo with keyboard and mouse. The first run builds it (5–15 minutes); after that it rebuilds only when the source changed. `Play-Keyboard.cmd bake` gives the final baked lighting. |
| **Play-VR.cmd** | The same demo in the Quest 3. Start Quest Link or Air Link first. *Needs OpenXR set up in the project on the PC first: see Docs/ROADMAP.md, section 1.* |
| **Open-In-Unity.cmd** | Opens the project in the Unity editor for your own adjustments. |

If Unity is not at the Unity Hub default path, set `JEWELRY_UNITY` to the full path of Unity.exe. To show the demo on another PC, copy the whole `Builds\Windows` folder; that PC needs no Unity.

## Controls

Full table: [Docs/CONTROLS.md](Docs/CONTROLS.md). In short:
- **Keyboard:** WASD to walk, right click for mouse look, click to pick up or place, 1–6 for the tools, F to take the camera and P to photograph, L flashlight, U UV torch, Tab presenter panel, F1 help.
- **Quest 3:**
  - Left stick walks; right stick turns.
  - Grip holds; trigger uses.
  - Hold the left trigger for the tool wheel.
  - Grip at your hips or chest takes the torches or the camera.
- **Presenter keys:** F7 comfort vignette, F8 smooth walking or teleport, F3/F4 hand opacity, F9 desktop/VR, F10 back to the start menu.

## For developers and AI assistants

- **Rules:** [AGENTS.md](AGENTS.md), shared by Claude and ChatGPT. It covers one project edited in place (never a new version per batch), where each kind of file goes, and who is working on what.
- **Current state and source map:** [Docs/HANDOFF.md](Docs/HANDOFF.md).
- **Open work:** [Docs/ROADMAP.md](Docs/ROADMAP.md).
- **Testing on the PC:** [Docs/PC_VERIFICATION.md](Docs/PC_VERIFICATION.md).
- **Open decisions for Koda:** [Docs/DECISIONS.md](Docs/DECISIONS.md).
- **Assets and licences:** [Docs/ASSET_REGISTER.md](Docs/ASSET_REGISTER.md).
- **Delivery plan:** [Docs/DELIVERY_PLAN.md](Docs/DELIVERY_PLAN.md).

```
Prototype/   all source: Scripts, Editor, Shaders, ThirdParty (edit only here)
Tools/       build, sync and zip helpers; asset preparation scripts
Downloads/   the one Unity project snapshot (unpacked by the launchers)
Docs/        live documents; References, Verification results and History
```
