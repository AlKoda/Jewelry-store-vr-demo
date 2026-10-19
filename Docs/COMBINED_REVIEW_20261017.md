# Combined playable review — October 17, 2026

Current launcher: `Start-Latest.cmd`. Player: `Builds/Review20261017/JewelryStoreDemo.exe`.
Canonical source: `Prototype/`; latest tested Unity project: `.asset-cache/combined-20261017/UnityProject_Demo`. Source is local and includes origin/main through 94cbf36. No commit or push made. The previous tracked local checkpoint is safely stashed as c71d3d8 and also backed up under `.asset-cache/root-before-combined-20261017`.

## Changes included

- Integrated Claude's October 14–16 code and twelve asset packs, including PBR materials, shop/street/forensic models, audio, optional music, sky and surface detail. Assets are selected for suitability; future Claude additions should be reviewed, not included indiscriminately.
- Imported the supplied police car at measured metre scale, with shared paint/rubber/chrome/lens finishes and three LODs. Fixed shared-mesh transform corruption which originally inflated its bounds. Headlights are dim warm white; baked diffuse lighting has small realtime companions for moving glass/metal highlights.
- Neutral dielectric glass replaces the green tint. Four fracture panes and thick fragments on the entrance, broken cases and ground replace the old flat shard strips. Case-local baked reflection probes support reflections. This is Built-in raster glass, not ray-traced transmission/refraction.
- Closed furniture undersides; real cabinet and desk drawer recesses with side/back panels and runners, replacing fake black aperture plates. Detailed facade windows now use photographic room textures and frame depth.
- Photographic night sky, scene fog, and a curved four-section city horizon. The horizon shader now receives the same distance fog as buildings and fades at top/bottom/side edges; the road and pavements extend into full fog instead of stopping visibly; it no longer uses four floating flat cards ignoring fog. Distant people disabled for the closed road.
- Four dynamic constrained doors: both entrance leaves plus office/service leaves. Desktop capsule contacts, tracked VR torso sweeps and tracked palm/fingertip sweeps push the same rigidbodies. Translucent rounded hand proxies replace opaque controller bodies while tracking is active. Side-room locomotion bounds now allow crossing both formerly blocked thresholds. Reset closes the doors. Hand tracking jumps/teleports do not generate impulses.
- White and UV torch beam toggles leave their models in the hand when off; switches play a click. Empty-space desktop click operates a carried torch; trigger operates the torch on that VR hand. L/U remain keyboard beam switches. Taking the camera moves the torch to the other hand; returning it restores the usual hand. Left click photographs while carrying the camera. Existing tool selection/placement has priority over the torch click.
- City ambience lowered .45→.12, wind .35→.075, room tone .25→.065, entrance whistle .20→.045, car idle .50→.18. Hard steps .35→.22; glass uses .15 with newly level-matched recorded clips. Eight real shoe-on-broken-glass takes replace synthesized glass crunch. Hard steps also remastered, smaller pitch variation and no immediate repeat. Sources and edits: `Prototype/ThirdParty/RealFoley/sources.json` (CC0).
- UI scale accounts for available screen size; switch labels/keycaps no longer wrap incorrectly. Dirty/untracked Prototype changes cause Start-Demo to rebuild instead of silently using a stale commit-stamped player. Standard builds use the combined baked pipeline.

## Verification

- Unity 6000.6.2f1 compiled the combined root source, including older retained art helper scripts.
- Latest baseline quick validation: 262 checks passed, selected LOD geometry 132,787 triangles / 453 renderers. This validates Claude's static generated baseline, before the additional review pass; it is not a final-frame count.
- The Unity editor emitted an internal SearchDatabase indexing exception during that validation; the validation itself completed all 262 checks. It is not a player script exception.
- Combined edit checks: 21 passed, including all four dynamic leaves, real drawer aperture, shader support, car dimensions, sky/fog, reflection probes and realtime headlight companions.
- Strict baked build and final runtime rebuild succeeded. Final player checks passed: all four doors from desktop body, simulated VR torso and simulated controller hand, closed/reset state, independent torch beam, camera hand transfer, quieter mix and recorded clip references. See final logs `Builds/review-build-access.log`, `Builds/review-build-runtime-final.log`, `Builds/review-player-final.log`.
- Captures: `Docs/VerificationReviewPlayer/`; baseline checks: `Docs/VerificationReviewBaseline/`. Recorded glass sample: `Docs/VerificationReview/recorded-glass-audition.wav`.

## Limits / next review

Actual headset tracking, hand contact reach, comfort, audio mix on headset speakers/headphones and stereo rendering still require a hardware test. VR push adapters are pose-tested; this no-packages project does not install/configure an OpenXR provider. Short foreground desktop frame samples are diagnostic only, not Quest or whole-scene performance certification. More art refinement remains possible; no claim of complete photorealism or bug-free operation.

The full local project archive in Builds contains all packs; the compact tracked Downloads snapshot omits the twelve large packs for GitHub size limits. Use Tools/sync-project.cmd before rebuilding the compact snapshot from this repository. Never edit generated Library files. Keep the user-provided FBX/GLB originals in Downloads and the prepared Blender sources under ReferenceAssets.

Final foreground desktop diagnostic: median 6.93 ms, p95 7.35 ms at 1600×900 DX11 on RTX 5060 Ti, one view/short sample, not a headset benchmark. Player emitted no error/exception in the final run. `git diff --check` passed. Compact source project ZIP is approximately 81.5 MB; complete local project ZIP approximately 305 MB, both CRC-verified.
