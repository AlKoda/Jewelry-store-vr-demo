# October 14 local art preview

Launch `Start-ArtPreview.cmd` in the repository, or `Builds/ArtPreview/JewelryStoreDemo.exe`. Enter opens the night store; option 2 is the controlled display-case review; F10 returns to the menu. The previous build in Builds/Windows is preserved.

Integrated into copies of the existing scenes:
- New fractured panes in both entrance doors, the right sidelight, and cracked Bay_2.
- Three floor-glass scatters at the threshold and broken sidelight. Clear faces and green cut edges use the non-emissive dielectric glass shader. Existing collision and door transforms retained.
- New 2,916-triangle case model replaces the two intact rear islands. Damaged cases and the central open drawer remain their existing geometry.
- Walnut, suede, velvet, brushed bronze, stone, steel, plaster, ceiling and marble floor material substitutions. Normal maps and linear metallic/smoothness maps imported, metre-based tiling set. All 56 supplied interior texture hashes match sources.json. Textures capped at 1024 in this preview.
- New ring tray and bracelet stand in the controlled case review. Tiny bezel ring remains staged, not included: Unity imported an empty mesh; requires an export-scale repair before integration.

The art assets came from ReferenceAssets/ArtPass20261014 and ReferenceAssets/ShatteredGlass20261014. Interior materials are the already staged WIP pack from commit 32ea467, not every later asset on Claude's WIP branch. Main remains 23dc3eb with earlier local changes. No WIP branch merge, commit or push was made.

This is a local integration bridge, not a gameplay change. Shared Prototype source was not edited. ArtPreviewBuild.cs and ArtPreviewCapture.cs are kept in ReferenceAssets/ArtPreviewIntegration and copied into the ignored Unity project. The scenes and generated materials are under Assets/CrimeSceneDemo/ArtPreview. FBX root scaling must be preserved: single-mesh files carry a root scale of 100, unlike the multi-object case. Do not replace it with Vector3.one.

Build target: Windows x64, Unity 6000.6.2f1, Built-in renderer. Night lighting rebaked: 1 x 512 lightmap, 596 light probes. Existing lighting settings retained; brightness/headlight tuning, UI, audio, torch interactions, exterior textures and atmosphere are still outstanding. This is not the final photorealism or performance pass.

Rebuild: after copying the two bridge scripts and staged assets into the existing local Unity project, run Unity with -batchmode -noUpm -projectPath <UnityProject_Demo> -executeMethod ArtPreviewBuild.Run -logFile <log>. It writes Builds/ArtPreview without replacing Builds/Windows. The bridge updates the local Resources/DemoBuildInfo stamp.

Verification: Builds/art-preview-build.log; Builds/art-preview-player.log; actual player captures under Docs/VerificationArtPreview. The opt-in -artPreviewSmoke visits all three scenes and captures the entrance, ground glass, rear case and material review. It does not run during normal play. These checks establish scene loading and visual integration, not a full gameplay regression or VR performance measurement.

Final verification: Windows build succeeded (156,910,812 bytes reported by Unity); all three ART_PLAYER_PASS markers present with no logged exceptions. Actual player images visually inspected after preserving FBX root scale. Interactive preview launched for the user.
