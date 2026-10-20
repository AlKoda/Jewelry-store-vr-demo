# Current project handoff

Updated October 20, 2026. Read AGENTS.md, Docs/VR_TESTING.md and Docs/ROADMAP.md.

One authoritative source tree: Prototype, using Claude's organized runtime/editor folders. One current project: UnityProject_Demo. One saved night scene: JewelryStoreNight. One output: Builds/Windows/JewelryStoreDemo.exe. Three primary entry points: Play-Keyboard.cmd, Play-VR.cmd, Open-In-Unity.cmd. Start-Latest/Start-Demo are compatibility aliases.

## October 20 reported-build repairs

Current saved scene and Windows player include the material/street/glass/VR fixes. Rear-room and selected prop materials use restrained Standard finishes; accidental emission is disabled, dielectric props no longer use metallic maps, and actual screens remain dim. Three localized storage/office/break-room reflection captures avoid showroom/sky reflections leaking into those rooms.

Road/pavement use the existing dry photographic PBR sets, restrained gloss and repaired metric UV0 coordinates; ground extensions retain their geometry and separate baked UV2. Five trees now use tapered branches and small photographic elm sprays, alpha-tested mips and 9 mm wind, replacing the large generic leaf cards and bare tree meshes. Generated artwork/prompt: GeneratedTextures/ELM_TWIG.md.

Legacy siren bar, colored washes, car halos and old headlight beams are disabled. Two dim warm baked headlights align with the supplied FBX's actual nose (car-local -Z); no added luminous bumper rectangles. Original FBX mesh, scale and parked transform are preserved. Interior fixtures stay OFF. Fresh bake: seven 1024 lightmaps, 676 probes; fourteen refreshed reflection captures.

Ground glass now has low absorption (face alpha .045, cut edge .10), independent dielectric Fresnel reflections and no diffuse-white surface. Faint contacts follow individual upper shard footprints instead of a broad pile blob. Closed shard geometry and existing fragments remain.

XR belt is 13 cm farther back; measured headset height loss moves it up to another 28 cm back, smoothly, while looking down alone does not. Belt/holsters retain a minimum floor clearance. Camera grip uses the accepted torch's axis and a model handle contact on either hand; desktop camera pose is unchanged. Official hand assets and empty-hand/torch alignment remain intact.

Unity repair checks, actual PC/Quest-tier torch shadow renders, hand/torch clearance checks, camera grip renders, glass on/off visibility comparison and strict Windows build passed. Quest comfort, camera fit, floor pickups and standalone performance still require headset acceptance. Evidence: Verification/Presentation/REPAIRS.md. Targeted entrypoints: PresentationPolish.RepairsRun (repair and bake) / RepairsReviewRun (validate saved scene and build); do not regenerate the layout.

## Generated environment signs — October 20

Additional dressing: `PresentationPolish.PropsRun` adds 15 selected existing-pack props in the saved scene: phone/mouse, filing cabinet, succulent, ceramic vase, two indoor plants, two street benches, bins, planted pots and bike racks. Three intersecting candidates were rejected. Floor placement is seated to its support, bounds checked against solid fixtures, and the front/central exit aisle excluded. Imported materials/LODs are retained; reflection captures refreshed. Existing furniture/layout and interior light state are preserved. Quest hardware performance remains unverified.

Five generated Arabic designs now replace the closed card (both readable sides), staff-only plaques, CCTV notice and evacuation diagram, with a paper reminder on a corkboard in the office. The plan follows the actual rooms/cases and front exit. Original artwork and exact prompts retained in ArabicSceneSigns/ENVIRONMENT_SIGNS.md. Desktop textures use mipmaps and 2048 maximum; Android overrides cap at 1024 ASTC. Signs are non-emissive; interior lights remain off. PresentationPolish.SignsRun updates the existing scene without layout regeneration.

## October 20 art and interaction repairs (Codex)

The store's interior fixtures remain **off**, as Koda clarified. Exterior sign lamps/street spill and handheld lights provide illumination. `PresentationPolish` updates the saved scene in place, preserving its interior layout: varied exterior upper storeys, cornices, stepped/arched parapets, awnings, painted window surrounds, gutters, rails, deeper terrace rows, a 360-degree geometry skyline, lighter fog and five leafy canopies with modest shader wind. The old photographic horizon cards are inactive. Road surfaces extend beyond the fogged camera range, and continuous movement regions allow both pavements and the road (x -29..29); outer safety barriers remain.

Koda's October 20 FBX car is imported directly, with its original texture and **no mesh edits/flattening**. Uniform instance scale fits 4.6 m; rotation parks it along the road. Source SHA-256 and provenance are in ThirdParty/SuppliedPoliceCar. New Arabic wordmark/section artwork is in ArabicSceneSigns/*_Designed.png; original signs remain. Both facade and counter branding use the new artwork. Prompt records: ArabicSceneSigns/DESIGNED_BRANDING.md.

Small deployed tools are kinematic trigger targets: selectable/grabbable, UV-occluding, and unable to displace the player. A placed scene lamp stays solid; a held lamp becomes non-solid. Shared pointer rays explicitly include tool triggers; locomotion/floor rays ignore them. Display furniture has closed, UV-mapped undersides (268 open rims repaired in existing combined meshes); future builders also close the bases. Door fingerprints sit on the actual pull bars. Six floor transfers now use varied, sparse masks at 2.5–5.4 cm quad sizes (individual drops smaller), with faint UV response.

Claude's shadow hooks are integrated: both torches use 2048 soft maps on PC / 1024 hard maps in the Quest tier, with restrained bias; opaque ceiling shadow faces cast both sides. Extra headlight specular passes are disabled for the parked car. A torch cleanup callback bug found during render validation is fixed. Fresh bake: seven 1024 lightmaps, 676 light probes, eleven refreshed reflection captures. PC/editor checks and hardware limits are recorded in Docs/Verification/Presentation/REPORT.md. Quest standalone performance and headset acceptance remain unverified; this PC currently lacks Unity Android Build Support.

Portability: sync copies current pack metadata as well as payloads; the snapshot carries all active ThirdParty metadata, including legacy UserProps GUIDs. Large packs (now also designed Arabic signs and supplied car) mirror from the checkout. Existing GUIDs were preserved. Do not replace the legacy UserProps metadata with its differing source GUIDs.

## Quest 3 standalone app — October 20 (Claude, unverified: never built)

Koda needs to show the demo on the Quest 3 without the PC. The same project and saved scene now also build as an Android app; see [QUEST_STANDALONE.md](QUEST_STANDALONE.md) for setup and use.
- **Build-Quest-App.cmd** (→ Tools/build-quest.cmd) syncs, runs `QuestBuild.Run` with `-buildTarget Android` (no scene regeneration), and installs `Builds/Quest/JewelryStoreDemo.apk` over USB with `adb install -r`. Tools/pull-quest-photos.cmd copies the headset's photos.
- **QuestBuild:** IL2CPP, ARM64, Vulkan, ASTC, API 32. OpenXR Android loader with Meta Quest Support and the Touch / Touch Plus profiles, multiview, XR started with the app. The version code grows with every build. Windows XR settings are untouched.
- **DemoPlatform.Standalone** (Android):
  - DemoLaunchOptions goes straight to VR and the night scene.
  - Shadow maps are halved and hard (DemoLighting, ToolStation lamps, camera flash).
  - NightQuality's Quest tier: 2 pixel lights, HardOnly, 8 m shadows, LOD bias 0.7.
  - The headlight `_Specular` lights, DemoDesktopPanel, DemoOverviewMap and LanguageSwitcher are disabled on scene load.
  - The camera viewfinder refreshes at 8 Hz.
- **PackImport:** an Android platform override caps every pack texture at 1024 (Automatic format, so ASTC).
- **XRPresenterMenu** (left controller, PC and Quest): hold the left menu button 0.8 s. The right controller points and the right trigger chooses: reset, movement, vignette, language, height ±5 cm, light bar, hints, close. A short press keeps toggling hints.
- **Tools/build-demo.cmd and Open-In-Unity.cmd** pass `-buildTarget Win64`, so the project returns to Windows after a Quest build.
- **Checks:** ShopCityValidation.QuestStandaloneChecks covers the tier, the menu (open, ray picking, right trigger, actions, close) and the app identity.
- **Compile:** the cloud Roslyn check with package stand-ins shows only Unity 6 APIs missing from its 2021.1 references. Not compiled in Unity and never built for Android. First PC run: install the Android module, then Build-Quest-App.cmd.

## Shadows, UV torch and reflections — October 19 (Claude, unverified in Unity)

Koda asked for the shadow bugs, the UV light passing through objects, and texturing/reflection quality.
- **UV torch:** the violet spot casts soft 1024 shadows, so it stops at walls, the counter and closed cases. Clear glass casts no shadow, so it still lights into a case. Fluorescent traces test five points (centre and corners): a half-covered trace glows at half strength, and a tool standing on a trace hides it. The glow edge and falloff follow the light's inner/outer cone. The torches stay exclusive (one torch shadow at most).
- **Shadows:**
  - The headlights' realtime `_Specular` companions (SceneReviewPass), unshadowed 18 m spots that lit the store through its walls, now cast hard 512 shadows.
  - The camera viewfinder no longer disables all shadows while the flashlight is on.
  - Shadow bias on the flashlight, UV torch, lamps and flash moves from 0.015/0.12 to 0.04/0.3 (stripe acne on grazing surfaces).
  - The quick-preview fallback's near lights get hard low shadows.
  - Street trees cast in the bake; the ceiling shadow-gap faces cast two-sided.
- **Materials:**
  - AmbientProps take their RealismTextures/PropMaps set per material via `DemoAssetLibrary.ApplyPropMaps` (pipes group 1/2, cable boxes, dirty spray cans were wrong or bare).
  - Estimated sets get a 0.6 gloss scale.
  - Pack AO maps are wired.
  - A finish's smoothness scales a packed map.
  - Rust and painted-metal tileables get aniso 8.
- **Reflections:**
  - Probe blend distances are set (cases 0.1 m, 256 px; entrance 0.5; showroom/office 0.75/0.5; display-case bakes 0.25). The 1 m default kept small case probes from reaching full weight.
  - ReflectionCapture adds a second pass with the probes on, so metals seen in reflections are not black.
- **What applies when:** runtime changes (UV, bias, viewfinder) apply on the next normal build. Builder changes (probes, trees, ceiling gap, headlight companions, prop materials, reflection capture) reach the saved scene only through the regenerating bake build (CombinedBuild), which replaces manual scene edits.
- **Checks:** NightStoreValidation gains UV visibility (partial, glass-like, tool on trace), probe blend and prop-map checks, and is updated for the shadowed UV torch, two-sided gap and preview shadows.
- **Not compiled in Unity.** The cloud Roslyn check shows only the XR-package references it cannot resolve. Shadow cost (up to torch + lamp + two hard 512 headlight maps) needs a PCVR profile.

## Current verified state

Local repairs from October 18 preserved: neutral/broken glass, clean static reflection captures, bilingual UI fit, Arabic physical signs/tape, supplied car, quieter recorded sounds, staff furnishings and 44 authored UV cues. Outdoor glove box/case remnants are now inactive in the regenerated scene. Raw assets are retained, not bulk-enabled.

Claude's latest organization and VR features are integrated with local fixes: F6 comfort/F7 language, explicit stereo depth pass, one ghost hand per controller, tracking gates/neutral rearm, bounded soft lamp shadows and camera-flash shadows. Runtime and editor sources compile in Unity 6000.6.2f1 with resolved XR packages. Strict Windows build and existing-scene build passed. Desktop regression COMBINED_PLAYER_PASS. Actual Meta OpenXR startup reached VISIBLE/FOCUSED; human headset acceptance remains pending. No headset FPS certification.

Packages: OpenXR 1.16.1, XR Management 4.5.3, Input System 1.20.1. Package Manager works and must remain enabled. Runtime initializes on VR launch only. Use DX11/SPI/Oculus Touch profile; settings and package locks are inside the snapshot and pinned template. Quest3Setup.Run repairs editor XR settings if needed.

## Editing/building

Change code only under Prototype; sync retains authoritative code GUIDs. Edit object positions in the existing saved night scene through Open-In-Unity. Normal rebuild uses EditableDelivery.Run and never invokes NightStoreBuilder. Only explicit bake uses CombinedBuild.Run, regenerating layout and fresh GPU reflections. Do not regenerate manual edits unintentionally. No additional project/scene copies are required.

The single compact ZIP carries the current saved scene, bake data, XR configuration, package manifest/lock and source; large packs sync from the repository. Libraries/player folders/logs remain local generated output. GitHub is the portable source/configuration base.

## Evidence and remaining work

Docs/Verification/Quest3/runtime-summary.txt records sanitized test results. Build logs under Builds are local. Historical local checkpoints/reviews are preserved under Docs/History and cache; do not confuse their old source paths/status with this handoff. Headset controls/comfort/photos, both-eye rendering, controller reconnect and profiling still need Koda's acceptance. Cleanup and car replacement remain deferred.

Latest headset follow-up: official Meta rigged controller hand meshes, smooth right-stick turning, headset-following utility belt with visible holstered models, default +12 cm height (F11/F12 adjustment), individual grip anchors, closer camera and padded selection volumes/point-delete. Detailed labels default off and wheel UVs are fixed. Strict build and rendered-icon/smooth-belt/skinned-asset validation passed. Human hand orientation, grip fit and comfort remain unverified. See VR_TESTING.md for controls and latest runtime limitations.

Hand follow-up: Meta authored OpenXR animation controllers/clips/masks now replace guessed finger-axis rotations. Both left/right middle fingertips moved over 2.5 cm between open/closed poses in the Unity validator; strict player build passed. Hand shading adds directional surface contrast and tool/torch grips sit lower in the fingers. Actual headset grip fit remains pending.

Current hand repair: Meta FBX armature/mesh conversion is normalized without deforming the skeleton. The actual anatomical wrist-to-finger axis maps to the Unity XR controller forward axis, with mirrored palm directions/thumbs up. Copying Meta prefab grip offsets directly was rejected in headset (fingers sideways); that path has been replaced. Both hand forward-axis checks, open/closed skinned mesh checks and strict saved-scene build pass. Rendered hands were visually inspected; latest headset acceptance is pending.

The belt and holstered tools are 12 cm lower. A native startup crash was symbolicated to ShaderVariantMonitor::FrameFinished; keepLoadedShadersAlive is enabled in EditableDelivery. Koda confirmed the subsequent player stayed running. The .gitignore rule now includes Editor/Build source and metadata in fresh checkouts, fixing a source/snapshot discrepancy.

Latest fine adjustment: empty hands pitch 12 degrees down from the calibrated controller frame. White/UV torch pickup uses a separate cylindrical grip pose and matching lens/handle anchor, rather than placing a horizontal barrel through the default controller hand. Both sides pass sampled vertex/triangle-centroid clearance checks in normal and fully pressed input states (maximum measured overlap about 1.13 mm, under the 1.5 mm contact tolerance). Combined rendered previews were inspected; strict build passed. Human angle/grip acceptance remains pending.

Latest correction: the original empty-hand models were correct; Koda meant the held-item pose looked like the opposite hand. The mistaken global model swap is reverted. The flashlight-only extra 180-degree palm roll is removed, with its lens anchor transformed to preserve barrel contact. Physical controller ownership and empty-hand angles are retained. Matching handed animation, held-palm orientation, mesh shape and torch clearance checks passed; strict saved-scene build passed. Headset confirmation remains pending.
