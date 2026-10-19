# Quest 3 PCVR testing

Use Unity **6000.6.2f1**. This is one Windows player and one saved night scene, shared by keyboard and Quest 3 Link/Air Link. It is not a standalone Android APK.

## Start

- **Play-VR.cmd**: activate Quest Link/Air Link first, then run this. The player starts OpenXR and enters the night scene directly when the headset is available.
- **Play-Keyboard.cmd**: same player, desktop controls, no XR loader startup required.
- **Open-In-Unity.cmd**: opens the single UnityProject_Demo folder. Edit the saved JewelryStoreNight scene; normal builds do not regenerate your positions.

A fresh checkout unpacks Downloads/JewelryStore_Unity6000.6.2f1.zip once, then syncs the large asset packs from Prototype. Package Manager must be enabled. Pinned package manifests are also reviewable under Tools/ProjectTemplate/Packages. First-time resolution/import can take several minutes. Open Unity once and run Quest3Setup.Run if XR settings need repairing.

Both play launchers accept **run** (existing player), **rebuild** (compile/build saved scene), or **bake** (explicit full scene regeneration and GPU bake). **bake regenerates layout**: do not use it after manual scene edits unless you intend that. The Unity menu Crime Scene > Delivery > Build current night scene without regeneration preserves manual positions. Large moved objects need a lighting rebake; rebuilding an EXE alone does not update baked shadows.

## Controls to test

- Left stick: smooth movement; F8 on PC switches movement mode. Right stick: turn/teleport according to current mode; confirm in headset hints.
- Grip: pick up/place tools. Empty left trigger: tool wheel; point/select with the right controller or left stick.
- Right trigger: use held tool/camera. Camera A: flash; B: review; stick: zoom. Confirm hand hints and photo output.
- Stick click: torch beam toggle. Hip/chest holsters: torch/camera retrieval. Taking tools clears an overlapping torch; camera moves the torch to the other hand when possible.
- Hands start translucent; F3/F4 opacity. F6 comfort vignette. F7 Arabic/English UI. F9 rig override is a diagnostic, not XR loader initialization.
- Hold left Menu + Y for two seconds: quit. Verify this chord on the headset before relying on it during presentation.

## Verified October 19

- Package Manager resolved OpenXR **1.16.1**, XR Management **4.5.3**, Input System **1.20.1**, with explicit Unity built-in modules. An older Input System version failed against this Unity editor's API and was replaced.
- Windows/DX11, single-pass instanced, Oculus Touch interaction profile, both input backends. Runtime initializes on VR launch only.
- C# compiled; strict integrated build and non-regenerating saved-scene build passed.
- Actual-player desktop regression passed: four doors/pose sweeps, camera/torch transfer, sounds, language, UV rendered pixels and 2K soft torch shadows. Summary: Docs/Verification/Quest3/runtime-summary.txt.
- A real Meta OpenXR session reached VISIBLE and FOCUSED and eye textures were created. This proves runtime startup, not completion of a human headset acceptance test.

## If the headset does not appear

XR_ERROR_FORM_FACTOR_UNAVAILABLE means the Meta runtime has no available headset for the app at that moment. Confirm the PC Meta Horizon Link app reports connected and that the headset is inside the PC Link environment, then relaunch Play-VR.cmd run. Initial attempts here failed until Link became available; a later launch succeeded. The app falls back to desktop if initialization fails. The selected runtime must be Meta's installed oculus_openxr_64.json; no registry change was required here.

## Human acceptance still required

Check both eyes, both hands and tracking recovery, door pushing, tool wheel/holster occupancy, photo viewfinder, language fit, floor height, snap turning and comfort. Inspect portable-lamp/flash occlusion, contact shadows and glass in motion. Measure headset GPU frame time before raising resolution or shadow budgets. Keep the provided car until its replacement arrives. Do not remove unused packs until dependency review and the presentation rehearsal pass.


## First headset feedback and follow-up

Koda confirmed the store appears through Link, but reported flipped hands, cluttered body tools/hand text and blank wheel graphics. Follow-up source fixes use neutral controller-relative hand axes with corrected left/right thumb mirroring, default-off detailed hand hints (left Menu toggles help), body-heading holsters that do not orbit with head gaze and proximity-only holster markers. RoundedQuad now supplies UVs: missing UVs caused the icon textures to sample only one transparent texel. VRVisualValidation reproduces this with a rendered-pixel test and verifies holster stability while the head turns. These visual fixes require another human headset check; initial startup success was not a complete acceptance pass.
