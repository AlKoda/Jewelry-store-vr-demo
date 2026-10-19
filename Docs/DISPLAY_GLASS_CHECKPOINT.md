# Display cases and user props — October 13, 2026

## Current result
Local Windows build: Builds/Windows/JewelryStoreDemo.exe. Start it directly to inspect this tested version; Start-Demo.cmd pulls GitHub and rebuilds instead.
Launcher choices include the baked night store, Display glass — material review, and Your props — scale review. In review scenes drag to orbit, scroll to zoom, F10 returns to the launcher.

Display cases use 6 mm bevelled glass, IOR 1.52 dielectric reflection, narrow green polished edges, champagne metal, walnut cabinetry and ivory lining. Broken panes use thick clear fragments. Store mesh merging now preserves material identity regardless of submesh order; face/edge counts and pane collision are checked. The exterior design and car candidate were not changed.

Seven user props are staged inside on/around an equipment table, and the supplied 1.8 m sofa replaces the showroom sofa visuals. Materials are intentionally plain. Props retain the supplied sculpted forms; they are scenery, not replacements for interactive forensic tools. The glove mesh remains in its sculpted bent pose despite export rotation. Originals and editable Blender files remain in ReferenceAssets/UserProps. Police car is preserved in ReferenceAssets/PoliceCar, not integrated.

## Verified locally
- Unity 6000.6.2f1 Built-in, NVIDIA RTX 5060 Ti.
- DisplayCaseReview.Run: 11 explicit checks plus completion; seven actual Unity editor captures.
- NightStoreValidation.RunNightQuick: 146 checks before the final material-identity regression assertions.
- Final NightStoreValidation.RunNight: 159 checks, one lightmap, 82,663 triangles, 392 renderers. First full bake 35.7 seconds; final cached bake 3.0 seconds.
- Windows build succeeded with baked lighting and the current source.
- Opt-in Windows player smoke loaded DisplayCaseMaterialReview, UserPropsReview and JewelryStoreNight; all three rendered at 1920x1080, no exceptions logged. Captures are Docs/VerificationDisplayCases/player-*.png. These are camera renders from the player, without the IMGUI overlay.
- The pulled October 13 interface source compiles and its automated night checks pass. Manual full HUD and headset checks remain outstanding.

## Rendering limitations and useful next steps
This is real-time probe-based glass, not ray-traced glass or a claim of photorealism. There is no refraction or per-object real-time reflection. The review room deliberately exposes the material at different angles; final photographic quality also needs finer cabinetry joints, better jewelry meshes and final wood/lining textures. Existing placeholder textures are still apparent.

Immediate editor captures after asset regeneration can show stale/dark reflection or material state; final player images are the authoritative visual evidence. The final player interior shows clear panes, not the excessive green visible in one immediate editor bake capture. Do not use that stale editor capture as the final result.

## Local / Git state
Base main is 23dc3eb after fast-forwarding four GitHub commits. Changes remain local, uncommitted and unpushed. Backup of the resumed work and pre-update ZIP is in .asset-cache/glass-resume-20261013; the temporary tracked checkpoint stash was retained. No source work was discarded.
Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip has been refreshed from Assets, ProjectSettings and Packages, including generated scenes, baked data and existing .meta identities. New source/model .meta files are also alongside Prototype assets. Library, build output, photos and Blender .blend1 backups are excluded.

## Reproduction
Sync Prototype/Editor, Scripts, Shaders and ThirdParty into UnityProject_Demo/Assets/CrimeSceneDemo. Launch Unity with -batchmode -noUpm -projectPath <project> -executeMethod DisplayCaseReview.Run, then NightStoreValidation.RunNight. Use DemoBuild.BuildLatest -demoBake to regenerate and build. Do not add -quit to the asynchronous validators.
For a player smoke test launch JewelryStoreDemo.exe with -displayReviewSmoke -logFile <path>; it renders the review scenes and interior, then exits. Ordinary launches do not run this test.
