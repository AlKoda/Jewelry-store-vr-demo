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

Koda confirmed the store appears through Link, but reported flipped hands, cluttered body tools/hand text and blank wheel graphics. RoundedQuad now supplies UVs and detailed hand hints default off (left Menu toggles help).

The latest follow-up replaces primitive hands with Meta's official rigged OpenXR hand meshes, animated by controller input. This does not enable optical hand tracking. The utility belt and holstered torch/camera models smoothly follow headset yaw. Right-stick turning accelerates smoothly, without repeated fade pulses. Standing height defaults 12 cm higher; F11/F12 adjust it by 5 cm and save the setting. Tools use individual handle anchors, the camera sits closer to the hand, and selection volumes include a small margin. B/Y removes a pointed deployed tool when the hand is empty; held-tool removal and camera review retain priority.

Strict saved-scene build and VRVisualValidation passed, including rendered icon pixels, smooth belt anchors and both skinned Meta hand assets. Human checks of orientation, finger curl, grip fit, height and comfort are still required. The latest test launch recovered from an initial STOPPING state and reached VISIBLE/FOCUSED. This confirms runtime startup; human acceptance is still pending.


## Authored hand animation follow-up

The current build uses Meta SDK 207.0.1 OpenXR animation controllers, clips and masks on the official mesh skeleton. Flex/Pinch/Point inputs replace manual bone-axis rotations. A Unity validator confirms both middle fingertips move more than 2.5 cm between open and closed poses. Surface shading and lower tool/torch grip placement address flat appearance and tools resting above the hand. This is controller-driven animation, not optical hand tracking or the full Interaction SDK grab stack. Human visual acceptance remains required.

## Hand import repair

Previous headset feedback rejected the hand result. Finger movement alone had missed the FBX armature/unit mismatch. The repaired setup preserves Meta bake-axis/hierarchy settings, compensates the retained 100x conversion at the model wrapper, and aligns after Animator reset. CheckHands now instantiates the actual controller-hand component, bakes both skinned meshes in open/closed poses, checks physical dimensions and thickness, and renders a preview for inspection. The preview shows intact hands; headset grip fit still requires a human check.

## Controller-frame correction

The latest build no longer copies the extra frame from Meta's controller-hand prefab. It explicitly maps the hand's wrist-to-finger direction to this application's controller forward axis, with palms inward and thumbs up. The runtime component is rendered in open/closed poses and checked for physical size, thickness and both forward directions. Headset acceptance remains pending. The belt, camera holster and torch holsters are 12 cm lower together.

The native shader startup crash is mitigated with keepLoadedShadersAlive in EditableDelivery. The next player remained running according to Koda. This is not a long-duration stability certification.
