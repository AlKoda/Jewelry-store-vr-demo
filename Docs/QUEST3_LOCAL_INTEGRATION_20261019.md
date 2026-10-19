# Quest 3 integration review against the tested local build

Date: October 19, 2026. Source review only; no Unity/player launched, no packages installed, no build, bake or headset test performed in this session.

## Compared states

- Last local tested scene: Builds/StaffRooms20261018; shared bilingual layout, Arabic map signs, clean static reflections, 44 UV cues, provided car, indoor tool bench and staff furnishings. Canonical local source contains substantial uncommitted work; preserve it.
- Fetched origin/main through 0ff2fe3. New runtime changes are 85daf22 and fda16c8. ee794d0 proposes reorganization only; 0ff2fe3 contains the earlier source review and delivery specification. They have been read, not merged over the local build.
- Most new functionality fits the project: free wrist orientation, upright placement, tool wheel, distance grab, holsters, camera zoom/flash/review, comfort vignette and controller-driven translucent hands. Source-level checks authored by Claude are not executed test results.

## Integration order and concrete gaps

| Priority | Finding and evidence | Required action | Model impact |
|---|---|---|---|
| P0 | Local LanguageSwitcher.Update consumes F7; upstream DemoDesktopPanel adds an F7 vignette toggle. A naive merge invokes both. | Preserve F7 language switching; move comfort control to an unused, documented binding/settings option. Update hints and translations. | None. |
| P0 | Local SceneReviewPass adds VRDoorHandPush to both hands; that component creates its own Translucent contact hand. Upstream XRControllerInput creates XRGhostHand. | Retain the swept door-contact behavior but remove/disable its duplicate visual when the new ghost hand is used. One visible hand per controller. Test both hands push all four leaves. | Existing procedural ghost hands sufficient initially. |
| P0 | GhostHand shader depth pass has no explicit stereo vertex/fragment implementation; colour pass has stereo macros. | Implement/test stereo-safe depth and colour passes; verify both eyes, finger overlap, hands crossing, case glass and held props. Avoid claiming a global depth prepass from separate finger renderers. | Optional later: optimized rigged glove pair. |
| P0 | Current package-free desktop project is not a configured OpenXR player. DemoModeSwitch only switches rig components. | Resolve compatible XR Plug-in Management/OpenXR packages on PC, configure Windows runtime and controller profiles, record versions, test actual Link/Air Link initialization. Never force VR controls with no working XR session. | None. |
| P0 | DemoBuild/CombinedBuild regenerate the scene; sync-project.cmd mirrors source folders. | Create protected editable project/scene and a build-existing-scene route that never generates or mirrors over manual changes. Test save/reopen/build preserves a deliberately moved object. | None. |
| P1 | XRControllerInput.Update/LatePose use device.isValid rather than full tracking validity; XRGhostHand.Apply only checks VR mode. | Gate poses, actions and visibility on position/rotation tracking. Cancel wheel/aim, reset stale buttons, define safe held-tool behavior and test recovery with grip held. | None. |
| P1 | New holsters can move a torch onto an occupied hand; local torch/camera transfer already has its own equipped/beam state and hand ownership. | Merge by behavior, not replacement. Keep actual beam clicks, camera-to-opposite-hand transfer and equipped-but-off torch visible. Define one hand occupancy policy; test torch off/on, both torches and tool/camera combinations. | Per-tool grip anchors; optional poses, not mandatory new objects. |
| P1 | Upstream additions to DemoDesktopPanel/DemoUi/EvidenceCamera overlap locally modified UI, resources and camera behavior. | Preserve fitted bilingual layout, fonts, translated feedback, audio, printed tape and custom reflection captures. Translate new wheel/holster/camera/comfort labels; test world-space labels too. | None. |
| P1 | Local ToolStation.UpdateLampShadows still gives portable lamps Hard/None shadows and shortens unshadowed ranges. Short range does not stop wall leakage. Camera flash is also unshadowed upstream. | Establish an occlusion-safe light budget, improve lamp softness/contact and verify flash/photos against walls. Preserve current 2K soft white torch; do not regress to upstream's older shadow settings. | None. |
| P1 | New generic/free tool rotations and camera zoom/flash/review affect anchoring and saved captures. | Test grip offsets, lamp upright exception, camera view direction, floor placement, tape anchors, collision, review/live return, reset and shutter clicks. Keep hands/UI out of photos. | Optional DSLR/torch visual refinements only. |
| P2 | New procedural hands use 17 renderers per hand and two shader passes; holsters repeatedly search hands and distance grab allocates SphereCastAll. | Profile before optimizing. Cache stable references and use nonalloc queries if needed; test thin glass/corners cannot permit grabs through solid barriers. | Optional single low-poly skinned hand mesh per side. |
| P2 | Reorganization proposal assumes metadata survives source mirroring. | Move scripts together with their authoritative .meta files; explicitly migrate project GUIDs and verify serialized references. Defer reorganization and asset deletion until verified release. | None. |

## Models and scene work

| Item | Decision |
|---|---|
| Transparent hands | Already authored upstream. Application supplies the visuals, not the headset automatically. Controller poses and grip/trigger values drive them; optical bare-hand tracking is a separate feature. Integrate one pair after shader/tracking fixes. |
| Better hand model | Optional after profiling: low-poly left/right rigged gloves, wrist pivot, open/grip/trigger poses and grip anchors. Static WorkGloves prop cannot substitute for this. |
| Camera/white and UV torches | Existing models/logic can be reused. Refine visual children and grip/lens/screen anchors if screenshots/headset show misalignment; preserve behaviors. |
| Existing shop/cases/doors/shards/staff props | Preserve local improvements. Upstream review describes an older look in places; do not replace repaired glass with older generation or redo already wired textures. |
| Outdoor glove box and equipment case | Confirmed user screenshot: leftover floating props where removed table stood. Remove or relocate with colliders on PC, check every dressing object in that area. Deferred note remains active. |
| Police car | Leave current model unchanged until the user supplies the replacement. Then validate dimensions, pivots, wheels, livery, glass and light anchors. |
| Additional scenery | Choose only useful provided models. No new collection/bulk placement required for the new VR controls. |

## Three delivery entry points

Keep one source project and shared scene/game logic. Target Windows PCVR with Quest 3 over Link/Air Link first; a standalone headset APK is a separate, unconfirmed deliverable.

1. Play-Keyboard.cmd: explicit desktop launch; XR unavailable must not disable keyboard input.
2. Play-VR.cmd: start a tested OpenXR player, report missing runtime clearly, enter the night scene without a monitor-only menu.
3. Open-In-Unity.cmd: open UnityProject_Editable at a user-owned JewelryStoreNight_Editable.unity without automatic sync/regeneration.

Prefer one tested XR-enabled binary with explicit mode selection and automatic detection. If runtime lifecycle or forced desktop startup is unreliable, use separate Desktop and Quest3-PCVR build profiles from the same sources. F9 rig switching exists but seamless XR loader startup/shutdown is not yet proven. Deliver whole player folders, not bare EXEs; deliver Assets + .meta, Packages/manifest/lock, ProjectSettings and baked data for editing. A .unity scene alone is insufficient.

## Shadows acceptance

Preserve baked soft direct lighting, bounce and static cubemaps for the shop/architecture; current local white flashlight already has filtered 2048 shadows and NightQuality allows soft shadows. Check small-object contact, bias/acne, detached feet, staircase edges, light through office walls, moving tools, transparent glass frames, portable lamps and camera flash. Clear glass must not cast opaque black slabs. Use grounded soft/contact approximations only where physically plausible. Measure GPU time and both-eye agreement on Quest 3 PCVR before increasing lights/resolution; no performance promise from source inspection.

## Work requiring the connected PC — do only after the user asks

1. Back up the full local source, .meta, tested scene and clean build; reconcile fetched changes without discarding uncommitted integration. Do not replace local ZIP with upstream's older snapshot.
2. Resolve the listed source conflicts; compile all C# and both shader passes. Preserve local audio, Arabic/English fit, new staff layout and neutral glass.
3. Configure and record XR packages/runtime/input profiles. Validate tracked hands, opacity, loss/recovery, occupied hands, wheel/holsters and all four physical doors.
4. Fix the outdoor leftover props; inspect room/object alignment and re-bake if positions of static objects change. Generate fresh GPU reflection captures using local ReflectionCapture; do not rely on the corrupt cached LightingData captures.
5. Run relevant existing validators and actual player regression; inspect both languages at all UI scales, both eyes, sound, photos, UV marks, shadows and reset. Profile in headset; simulated pose checks are insufficient.
6. Create the editable project/scene and non-regenerating build path; verify a manual move survives save, reopen and build. Produce and test the three entry points from fresh extraction.
7. Update compact/full archives with metadata and licences. Only then identify unused dependencies and archive/remove them on a deliberate release/archive branch. Never blindly delete Resources, shader references, fallbacks, bake data or .meta files.

## Official references consulted

- Unity Meta Quest workflow: https://docs.unity.com/en-us/engine/6000.0/manual/xr/configuring-project-for/meta-quest-develop
- Unity OpenXR Touch Plus profile API (reference, not a package-version prescription): https://docs.unity.cn/Packages/com.unity.xr.openxr%401.14/api/UnityEngine.XR.OpenXR.Features.Interactions.MetaQuestTouchPlusControllerProfile.html
- Unity custom stereo shader requirements: https://docs.unity.com/en-us/engine/7000.0/manual/xr/graphics/stereo-rendering/single-pass-instancing (apply equivalent Built-in guidance for installed Unity version).

No runtime source changed, assets removed, upstream merged, archive rebuilt, commit/push, Unity launch or PC/headset execution occurred. This review supplements origin/main Docs/CLAUDE_REVIEW_2026-10-19.md and DELIVERY_PLAN.md with conflicts in the actual tested local version.
