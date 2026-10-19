# Source-only status — October 19, 2026

Implemented in Prototype, NOT compiled/built/headset tested:
- Three-way integration of Claude 85daf22/fda16c8, preserving local bilingual UI, supplied car, glass, sound, clean reflection capture and staff-room changes. Backup: .asset-cache/quest3-source-before-20261019.
- F6 comfort / F7 language; translucent ghost hand shader now has explicit stereo-enabled depth and colour passes. Door sweep uses tracked poses and no duplicate primitive hand visual.
- Tracking validity gates input/ghost hands; loss cancels aim/wheel, pauses held-tool following, clears button state. Reconnection waits for neutral controls; this needs headset testing.
- Torch holstering distinguishes equipped from beam-on, prevents overlap with tools, and keeps camera hand transfer logic. Additional occupancy/transfer combinations still require hardware review.
- One active portable lamp, soft 1024 shadows, remains occluded alongside the existing 2048 soft torch. Camera flash has capture-time soft shadows. Existing night validators updated for this explicit budget. Performance and appearance unverified.
- Outdoor glove box and equipment case disabled during integration generation with an assertion; scene has NOT yet been regenerated, so old player still contains them.
- -demo-mode desktop/vr parsing, safe missing-headset fallback, direct VR night-scene entry once runtime starts. Loader/packages remain a PC task. Play-Keyboard.cmd / Play-VR.cmd intentionally require a validated Builds/Delivery player, so they cannot falsely launch the old desktop-only binary as VR.
- Open-In-Unity.cmd and Tools/Prepare-EditableProject.ps1: copy Assets/Packages/ProjectSettings with metadata into a NEW protected project, refusing overwrite. Run manually only once integration passes. Editor menu creates a user-owned scene and builds that existing scene, without generator calls. Mirroring and integrated regeneration reject the editable project marker.

Do not use these sources as a verified Quest build. Start-Latest.cmd still points to the unchanged tested October 18 player. The editable project and Builds/Delivery do not exist until the PC steps below run. No cleanup, package install, Unity launch, bake or build was performed.

## Next connected-PC steps

1. Compile in a separate candidate project. Run new ShopCityValidation VR checks plus NightStoreValidation.RunNightQuick / RunNight, gallery and player regression; inspect updated lamp budget and metadata.
2. Resolve OpenXR/XR Plug-in Management packages and profile settings for Unity 6000.6.2f1 Windows Quest 3 Link/Air Link; record versions. Test startup/timeout/reconnect, both eyes, neutral rearm, wheel, holsters and four doors.
3. Regenerate candidate scene to apply outdoor-remnant removal, bake with graphics enabled and explicit clean ReflectionCapture; verify visuals, saved photos and headset GPU times.
4. Verify all new UI strings and world-space hints in Arabic/English. New feedback translations added; broader VR hint localization needs a visual audit.
5. Prepare editable copy, create scene copy, deliberately move an object, save/reopen/build existing scene to prove edits survive. Test the three launchers, missing headset and clean extraction. Do not mirror into this copy.
6. Only promote to Builds/Delivery after these pass. Preserve October 18 player as fallback. Cleanup/reorganization and optional rigged glove/DSLR/torch refinements remain later work.


Source packaging/static checks: new source .meta GUIDs added, JSON catalogs parsed, PowerShell preparation script parsed without syntax errors, source conflict-marker/whitespace checks passed. These are not C#/shader compilation. Compact and Quest3_SourceCandidate archives are explicitly labelled UNVERIFIED and retain the previous generated scene/bakes; the previous verified compact ZIP is backed up under .asset-cache/verified-compact-before-quest3-20261019.zip. Full October 18 archive/player remain untouched. New VR quit chord: left Menu + Y held two seconds, intercepted before normal button actions; verify on Quest before release. Delivery build refuses missing resolved OpenXR packages. No commits/push.
