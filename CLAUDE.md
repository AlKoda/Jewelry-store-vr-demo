# Working notes for assistants

Read Docs/HANDOFF.md first: current state, source map, what is verified and what is not. Then Docs/ROADMAP.md.

- Unity 6000.6.2f1, Built-in pipeline, no packages (Package Manager is broken on the PC; launchers use -noUpm). Nothing here can be compiled outside Unity; the user runs the validators on their PC and reports results.
- Runtime code lives in Prototype/Scripts, Editor code in Prototype/Editor. The Unity project is generated from them; Downloads/JewelryStore_Expanded_Unity6000.6.2f1.zip must be updated whenever scripts change (copy scripts in, add a minimal .meta with a fresh guid for new files, keep existing .meta files).
- Validate with ShopCityValidation.RunExpanded (-executeMethod, isolated project). Add a check in DemoValidation or ShopCityValidation for every new behavior.
- Shared code: ToolHolder for interaction adapters, InteriorBounds for the interior, DemoGeometry for generators, DemoAssetLibrary for third-party models (always with a primitive fallback), DemoValidation for validators. Do not duplicate these.
- Third-party assets go under Prototype/ThirdParty/<pack>/ with a LICENSE.txt and a row in Docs/ASSET_REGISTER.md; CC0 preferred, CC BY with attribution accepted. Convert glTF/GLB with Tools/glb_to_obj.py (OBJ is what the package-free project imports). From this cloud environment only raw.githubusercontent.com is reachable.
- Do not hard-code an XR toolkit API until the package versions are known; bind controller actions to HandInteractor/XRLocomotion methods instead.
- All work goes directly to `main`; pull it before editing, because another assistant (ChatGPT) works on the same repository. Update HANDOFF.md and ROADMAP.md with every batch and mark anything not yet run in Unity as unverified.
- Commit .meta files, Assets, ProjectSettings; never Library, Builds, logs or photos.
