# Recording build — October 18, 2026

Launch `Start-Latest.cmd` → `Builds/Recording20261018/JewelryStoreDemo.exe`. October 17 player is preserved in Builds/Review20261017. Canonical source remains Prototype; tested project remains .asset-cache/combined-20261017/UnityProject_Demo.

Applied the user's screenshot review requests:

- Replaced the imported fracture meshes in the playable scene with directly authored metre-scale, bevelled closed glass geometry. New door/window fracture cells have an irregular impact opening, fitted to the existing frames; hinge/body/VR hand/reset behaviour remains intact.
- Removed the oversized transparent shards extending above the display cases. Every replacement pane/scatter has a generated-scene world-bounds check. Case panes are only the intended 0.29 m high.
- Added denser overlapping debris, larger fallen pieces, physical glass thickness and facet bevels; floor-specific glass finishes improve readability against the paving. Added subtle local contact darkening under the pile (raster approximation, not ray-traced caustics) and a baked 256px entrance reflection probe.
- Removed the malformed/floating StationerySet from the counter, retaining the grounded photograph and phone. No downloaded model is added in this pass.
- Changed the flashlight from low-resolution hard shadows to filtered 2048px shadows, with tighter bias/near-plane settings. Updated runtime NightQuality so it no longer forces all shadows to HardOnly/Medium and overrides the intended soft-shadow setting.

Validation: strict Unity Windows build, generated-scene checks (42), existing actual-player interaction checks for all four doors / torso / controller sweeps / reset / camera hand transfer / torch switching / sound references, plus a runtime assertion for the soft 2K torch shadow settings. Inspect the final logs and captures in Docs/VerificationRecording20261018; actual headset testing remains pending. Existing audio improvements are retained. New fracture source: Prototype/Editor/RecordingGlass.cs; contact shader: Prototype/Shaders/GlassContact.shader. Reference image remains Docs/ReviewReferences20261017/approved-glass-appearance-target.png.

For a cleaner recording: close help (F1/H), use Tab to hide the presenter panel and F2 to hide the hotbar. Right mouse toggles mouse look. L toggles the flashlight beam; U toggles UV. Record the actual player; the earlier Blender image uses a different renderer and is a target, not a claim of identical lighting.

Source/assets and compact project snapshot updated locally. No git commit/push. Preserve the earlier stash and original Blender/FBX/GLB assets. Future Claude assets should be selected for suitability, not bulk-enabled.

Final results: strict Windows player build succeeded; 42 generated-scene checks and final player checks passed with no runtime error/exception. Captures of the replaced door glass, layered floor debris, cleared counter/torch shadows, and cases with no oversized transparent geometry were visually inspected. Diagnostic foreground frame sample: median 6.95 ms, p95 7.37 ms at 1600x900 DX11 on RTX 5060 Ti, one short view sample (not a VR benchmark). Earlier October 17 captures are preserved under .asset-cache/recording-before-20261018. Compact project ZIP approximately 84 MB; full local project archive is under Builds/Recording20261018.
