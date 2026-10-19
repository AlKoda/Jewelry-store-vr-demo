# Combined integration checkpoint — October 17

Source worktree: .asset-cache/combined-20261017, based on origin/main 94cbf36. Original root work and previous builds preserved. No commits/pushes performed.

Completed prior request: twelve Claude packs and their source wiring, supplied car prepared to 4.7 m with 3 LODs, neutral glass art integration, dim warm headlights. Main and WIP Prototype currently have no differences, so main includes Claude's latest available work.

Fixed during Unity validation:
- OBJ Kd/d opacity recovery before material deduplication (Unity 6 discards tints on textured OBJ imports).
- Custom SurfaceDecal shader preserves early transparent decal queue instead of Standard material validation resetting it. Standard alpha-blend keyword removed from custom shader to avoid player-only shadow compilation conflict; validator recognises explicit custom fade shader.
- Car geometry normalization clones shared mesh data and snapshots transforms, preventing two tiny repeated pieces from expanding its bounds/collider into the entrance.
- Car uses light probes and no lightmap UVs. Additional probe coverage samples near accessible region centres.
- Police preset checks read the selected car's measured anchors/dimensions; no country/siren/radio choice made.
- Tape pass tag comparisons ignore case (Unity returns uppercase tags).

Validation: quick 262 checks pass, full baked 262 checks pass; baked run 14.9 s, 1 lightmap, 129,779 selected triangles, 453 selected renderers. Baseline evidence copied to Docs/VerificationCombinedBaselineQuick and ...BaselineBaked. Material count 125, budget adjusted to count+8=133. Builds/combined-baked-validation.log.

Build: Builds/Combined20261017/JewelryStoreDemo.exe; combined-build-2.log succeeds without shader errors. Player log combined-player.log has COMBINED_TORCH_PASS, COMBINED_MUSIC_PASS and COMBINED_PLAYER_PASS, no exceptions. Actual renders Docs/VerificationCombined. Frame sample from hidden window is invalid (too low); do not report it as performance evidence. Hidden ScreenCapture is black; direct camera renders are valid. Foreground HUD/performance and real headset testing remain.

Next: apply Docs/USER_REVIEW_20261017.md only now that integration is complete. Root source/main must eventually be updated from tested worktree while preserving earlier local changes in a backup/stash. Need update source ZIP/assets metadata/docs after final work. Do not leave the only copy of integration in ignored .asset-cache.
