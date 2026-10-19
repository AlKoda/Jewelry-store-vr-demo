# Claude source review — Quest 3 and delivery
Review date: 2026-10-19. Reviewed main at fda16c80951a557f8bf447e349dcf36ade22e437, including October 14–19 changes and the recent pack documentation. Static source review only: no Unity compile, shader compile, bake, build, visual inspection or headset measurement was possible. This is a targeted integration review, not proof that every asset or code path is correct.

## Overall fit
The changes broadly fit the instructor-led, untimed, unscored night-arrival scenario: expanded store, deployable tools, screenshot camera, white/UV torches, presenter-controlled evidence, low-cost baked lighting and optional comfort controls. Preserve shared ToolHolder, InteriorBounds and DemoAssetLibrary. No extra gameplay framework or duplicate keyboard/VR scene is needed.

New comfort features are authored, not verified. Existing documentation includes stale claims (no vignette, VR footsteps not wired, Quest 2) that should not override the latest code. Footsteps observes rig-root displacement, so smooth VR locomotion may already produce footsteps without explicitly consuming MovedDistance; verify before adding a second driver.

## Findings, in delivery order
| Priority | Evidence | What must change or be checked | Model needed? |
|---|---|---|---|
| P0 | DemoLauncher.cs explicitly says desktop build / OpenXR absent; CLAUDE.md describes package-free -noUpm projects | On PC install compatible XR Plug-in Management/OpenXR, configure Windows PCVR and runtime, test Quest 3 Link/Air Link and controller profiles. Existing input code alone cannot start an XR runtime. Record resolved package versions. | No |
| P0 | GhostHand.shader:14–18 is a fixed-function depth-only pass; only its colour pass has instancing/stereo macros | Replace the depth pass with an explicit stereo-compatible vertex/fragment pass, or adopt a tested ghost-hand material. Test both eyes under single-pass instancing and multipass. Also test overlap between separately rendered fingers and the two hands: a per-renderer depth pass is not a global hand prepass. This is a source-level rendering risk, not an observed headset failure. | No for initial hands |
| P0 | NightStoreBuilder.BuildScene creates a new empty scene and saves JewelryStoreNight; DemoBuild.BuildLatestReport calls it on every rebuild | Create a user-owned scene copy and an explicit build-existing-scene route. Keep generation/validation scenes separate. Do not run the regeneration build against the editable deliverable. Moving large baked fixtures requires a new bake. | No |
| P1 | XRControllerInput.cs:124 checks isValid only; LatePose also uses isValid only | Gate hand poses/actions on tracking validity, hide or clearly mark lost hands, cancel wheel/aim, clear stale button states and choose a safe held-tool policy. A valid device can be temporarily untracked. Test reconnect with grip held. | No |
| P1 | XRHolsters.TryUse flashlight/UV paths lack the held-object guard used by its camera path | Define whether a hand can carry a torch and another tool at once. Prevent overlapping tool/torch visuals or explicitly support a combined grip. Check taking both torches onto one controller and returning either without consuming the wrong action. | Optional authored grip poses |
| P1 | ToolStation.UpdateLampShadows gives the only realtime shadow to white flashlight or last lamp; other lamps remain lit without shadows | Shortening range does not prevent light crossing a nearby wall. Prefer disabling an unshadowed lamp or implement an occlusion-safe fallback; consider two shadowed lights only after PCVR profiling. Verify under-counter and office-wall leakage. | No |
| P1 | PackImport.Packs lists 12 older packs, excluding AmbientProps, AmbientProps2, CutoutDecals, RealFoley, RealFoley2, RealismTextures | Select only useful assets, add import/material/audio rules before placement, extend credits and validators. These six newer packs are not automatically scene-ready. Source README files explicitly say not wired/auditioned. | Assets already supplied |
| P1 | DemoAssetLibrary.BuildPackMaterial looks for supplementary maps inside the model's pack | RealismTextures/PropMaps are in a separate pack from AmbientProps colour maps. Add an explicit mapping; simply copying them cannot assign their normals/smoothness/AO. | No |
| P1 | DemoLauncher is monitor IMGUI with keyboard/mouse scene selection | Provide a headset-usable start path (direct night-scene launch or controller-operable menu) and return/quit path. Desktop instructor assistance may remain a fallback, but a VR shortcut must reliably reach the scene. | No |
| P2 | XRGhostHand builds 17 renderers per hand from stock capsule/sphere meshes, each using two passes | Profile 34 transparent renderers and their mesh cost outside the static environment budget; use lower-resolution primitives or a single skinned hand mesh if needed. No exact hand triangle count was measured here. | Optional optimized rigged hand pair |
| P2 | XRHolsters.LateUpdate searches all HandInteractor objects every frame; HandInteractor.PointedTool uses SphereCastAll | Cache hand references and avoid routine allocations if profiler shows pressure; handle device/scene changes safely. | No |
| P2 | PointedTool uses a spherecast candidate but a centre-ray blocker check ending 5 cm short of the hit distance | Test objects beside/behind thin case glass and near wall corners. Use an obstruction check consistent with the swept grab volume if it permits pulls through obstacles. | No |
| P2 | Old asset note still says the October 15 packs need explicit wiring, though they were wired by 9305177 | Read current source/pack status; do not redo the existing twelve-pack integration. Keep future status tables per pack. | No |

## Transparent Quest 3 hands
Already authored: XRGhostHand is created by XRControllerInput.Awake; grip/trigger/thumb inputs animate a primitive pair, starting opacity 0.35, F3/F4 adjust it, controllers hide while hands show. DemoToolsBuilder.AddTheme retains the Ghost shader. Hands are excluded from evidence photos using the interface layer and do not cast shadows.

These are software visuals attached to controller poses. The headset does not automatically insert hand meshes into this Unity application. This requirement is controller-driven hands, not optical bare-hand tracking; do not add XR Hands/Meta hand tracking solely to satisfy it. The existing WorkGloves.obj is a static prop, not evidence of a rigged usable hand pair.

For the deadline, keep the procedural pair after the stereo/tracking fixes. Later: low-poly left/right gloved skinned meshes, wrist pivots, compatible bones, open/grip/trigger poses and per-tool grip anchors. Current HandInteractor uses one generic HeldOffset, so a prettier mesh alone cannot fix camera/handle alignment.

Official references (select versions matching the installed package; do not install a preview just because it appears here):
- https://developers.meta.com/vr/documentation/unity/unity-touch-plus-controllers/
- https://docs.unity.cn/Packages/com.unity.xr.openxr@1.14/api/UnityEngine.XR.OpenXR.Features.Interactions.MetaQuestTouchPlusControllerProfile.html
- https://docs.unity.cn/Manual/SinglePassInstancing.html

## Shadows — required appearance
Keep the static shop, car and stationary architecture on baked direct lighting with baked soft shadows, bounce and probes. This is a useful realistic approximation for this fixed nighttime case, without full realtime lighting everywhere.
- NightLighting's baked headlights give fixed fixtures long directional shadows. RunNightQuick and unbaked Start-Demo builds deliberately use shadowless environmental lights: they cannot approve the final look.
- NightQuality currently selects HardOnly for realtime shadows and 14 m distance. That does not erase baked soft shadows. The flashlight uses low-resolution hard shadows; judge sharpness, bias, acne and contact gaps on PC.
- Deployed tools need live illumination/occlusion. Baked headlights cannot create a new moving cone/marker shadow. Probes light them but do not produce their contact shadows. For headlight-only views, consider a cheap soft blob/contact approximation under deployed tools or a tightly bounded realtime headlight/shadow solution after profiling.
- Transparent glass deliberately casts no solid black shadow; retain opaque frames as casters. Clear glass should not act as an opaque wall.
- Camera flash has no shadows; viewfinder may suppress torch shadows for performance, while saved captures retain the normal shadow settings. Test for misleading light-through-wall photos and document or revise that tradeoff.
- Ghost hands are interface visuals and intentionally cast no shadow.
- Final acceptance: no light through solid office walls, grounded cases/tools, plausible headlight direction, no detached shadow feet or flicker, both-eye agreement, readable saved photos. Record GPU frame time and shadow settings.

## Models: reuse, reshape or add
| Asset | Repository state / recommended work |
|---|---|
| Display/wall cases, counter, large/tall safes, storefront/window modules | StoreKit already generates detailed fixtures and broken/hinged variants. Keep them initially. Match frame/pane thickness, wall joins and grounded shards after visual checks; a new image-to-mesh pass is unnecessary. |
| Necklace bust, ring tray, boxes, stands, dropped jewelry | Procedural versions and JewelryModels exist. Reuse and tune scale; keep empty nighttime displays and selected dropped pieces. Separate lids/doors only if animation is required; fixed open scenery can stay static. |
| Cone, marker, tape post, scale/reel and scene lamp | Working procedural tools already preserve deployment logic. ForensicModels includes marker/scale/searchlight alternatives. Replace visual children only after checking pivot, collider, marker-number faces, tape anchors and lamp head direction. |
| DSLR | Current procedural camera exists; no dedicated DSLR mesh appeared in the reviewed ThirdParty paths. Optional visual replacement: unbranded camera with lens-forward axis, grip anchor, separate screen/viewfinder material and shutter/lens transforms. Keep EvidenceCamera behavior. |
| White / UV torches | TacticalFlashlight and UVTorch OBJ assets exist. Optional visual swaps need controller grip/lens offsets; do not replace beam and UV logic with mesh alone. |
| Transparent controller hands | Procedural pair exists. Optional new asset is a rigged glove pair, not static WorkGloves. First fix shader/tracking and test current visuals. |
| Desk/chair/shelving/drawers/sofa/tables/plants/security fixtures | Existing InteriorModels/KayKit/procedural choices cover most needs. Match palette/scale and remove duplicates from active scene; retain source archives until final cleanup. |
| Police car | Existing provisional and US review options are in PoliceLook. Country/service remains Koda's decision. Local body/livery may require a remodel, UV artwork, separate wheels/headlight parts and recalibrated light anchors. No country inferred from timezone. |
| Across-street buildings | Existing apartment/facade assets already wired. Additional AmbientProps2 parked cars/trees are optional, not a reason to increase the environment budget. |
| Trees with leaves | CutoutDecals includes crown/leaf cards, AmbientProps2 includes trees. Add cutout coverage/mipmap and LOD rules before replacing bare trees. No new leaf imagery is immediately necessary. |
| DVR / burglary tools / evidence | DVRRecorder, crowbars, hammer and other props exist. Placement depends on instructor story approval. Keep optional evidence hidden until approved. |
| Roller grille / local emergency fixtures | Conditional modeling/style work only after entry/country decisions; current neutral fixtures should remain usable. |

## Scope and verification limits
No assets were removed. Do cleanup only after dependency/build usage checks, license/credit checks and successful desktop/VR rehearsals. Archive historical source/reference packs separately if useful; do not blindly remove fallbacks, shader references, Resources, generated bake data or .meta files.

## Follow-up: Claude reorganization proposal at ee794d0
Read Docs/REORGANIZATION_PLAN.md; it is a proposal only, with no runtime changes since fda16c8. Its three launcher names are adopted in DELIVERY_PLAN.md. Keep reorganization deferred until Quest 3 verification, as that proposal requests.
Two corrections are needed before carrying it out:
- Moving source scripts while sync-project.cmd excludes *.meta does NOT move existing project-side .meta files into their new subfolders. Unity can create new GUIDs and lose serialized references. Build an explicit source-to-project metadata migration (preserve each old GUID with its moved script), or commit authoritative .meta files and change synchronization deliberately. Test serialized scene/prefab references and the rebuilt snapshot before deleting old copies.
- Do not use an automatic mirroring/regeneration editor path as the only user-editable delivery. Preserve a user-owned scene and existing-scene build route; keep edits outside mirrored folders. Merely opening a .unity scene cannot supply the entire project.
