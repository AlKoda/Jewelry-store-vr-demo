# Rules for every assistant on this project (Claude, ChatGPT/Codex and any other)

Read this file first, then [Docs/HANDOFF.md](Docs/HANDOFF.md) (current state and source map), then [Docs/ROADMAP.md](Docs/ROADMAP.md) (open work).

## Who is working on what

Update your own row **before** you start and again when you stop. Pull first: another assistant may have changed it. Do not edit files listed in someone else's row without asking Koda.

| Assistant | Working on | Files | Since | Status |
|---|---|---|---|---|
| Claude | Koda's second test fixes (October 24): wand/brush contact by tip, faint glow, hint on B/Y or V only, text-free case with lid straps, grabbable doors; runs `PresentationPolish.SandboxRefreshRun` in Koda's checkout (`C:\Users\MOBPC\Documents\ChatGPT\Jewelry store\repository`) with a partial checkout of Claude's files when ChatGPT has work staged | Prototype/Scripts/Core (forensic classes, ForensicGlow, PushDoorLeaf), Tools/FieldInvestigationTool.cs, VR/HandInteractor.cs, VR/XRControllerInput.cs, Desktop/DesktopInteractor.cs, Editor/NightStore/ForensicSandbox.cs, IntegrationDelivery.cs, SceneReviewPass.cs (door tuning) | 2026-10-24 | PC run passed, build in Koda's folder; headset test pending |
| ChatGPT | Patrol car close-up refinement | TripoPoliceCar generator/materials/shaders/authoring, scene and Docs | 2026-10-26 | complete: split spokes, closed barrels, fitted transparent optics, 256px baked reflections; 33,968/15,000 LODs; fresh bake and strict Windows build passed |

## One project: edit it, never fork it

- There is **one** project and one scene in use, the night store. When you work on something, change the existing files in place.
- Never make a new copy, a new numbered or dated version, a new scene variant, a new Unity project or a new zip for a batch of work. That is what made this repository hard to follow before October 19 (City, Expanded, Forms, Glass, Interior, Review, Robbery…).
- Git keeps every earlier version, so nothing is lost by editing in place. To keep an old behaviour switchable, use a setting on the existing component (like `PoliceLook.Current` or `XRControllerInput.SmoothMovement`), not a second copy.
- New files only when something genuinely new is added (a new component, a new asset pack), placed in the folder the source map in HANDOFF gives for it.

## Where things go

- **Code:** `Prototype/Scripts/<Core|VR|Desktop|Tools|Lighting|Audio|UI>` (runtime) and `Prototype/Editor/<NightStore|Shared|Build|Validation|Legacy>` (Editor only). Shaders go in `Prototype/Shaders`. `Prototype` is the only place to edit code: the Unity project folders on the PC are copies.
- **Every new `.cs` or `.shader` needs a `.meta` beside it, with a fresh guid** (`python3 -I Tools/zip_snapshot.py` writes missing ones). Never change or delete an existing `.meta`: scenes find scripts by that guid. Move a script only together with its `.meta` (`git mv` both).
- **Third-party assets:** `Prototype/ThirdParty/<pack>/` with LICENSE.txt, sources.json and README.md, plus a row in `Docs/ASSET_REGISTER.md`. CC0 preferred, CC BY with attribution accepted. Convert glTF/GLB with `Tools/glb_to_obj.py`. Never delete a pack's licence file. Keep sounds, textures, models and code even when unused: they may be used later.
- **Docs:** live docs in `Docs/`. Reports of finished work go to `Docs/History/`. Reference images go to `Docs/References/`. Validators write to `Docs/Verification/<run>`. Never commit photos taken in the demo, Library, Builds, logs or Unity project folders (`UnityProject*`).
- **The snapshot zip:** `Downloads/JewelryStore_Unity6000.6.2f1.zip` is the only one. After changing scripts, shaders or small ThirdParty packs, refresh it with `python3 -I Tools/zip_snapshot.py`. The canonical snapshot uses Git LFS (250 MB guard); clone with Git LFS and run git lfs pull. Large packs can stay out (the launchers copy them from the checkout).

## Working rules

- Unity 6000.6.2f1, Built-in pipeline. Package Manager now works: keep it enabled. Pinned OpenXR/XR Management/Input System setup is documented in Docs/VR_TESTING.md. Do not hard-code an XR toolkit API until the package versions are known; bind controller actions to HandInteractor/XRLocomotion methods.
- Shared code, do not duplicate: ToolHolder, InteriorBounds, DemoGeometry, DemoAssetLibrary (always with a primitive fallback), DemoValidation, DemoUi.
- Add a validator check (DemoValidation, ShopCityValidation, NightStoreValidation or StoreKitValidation) for every new behaviour.
- Work directly on `main`. Pull before editing and again before pushing. Small commits with clear messages.
- With every batch: update the top of `Docs/HANDOFF.md` (state table) and `Docs/ROADMAP.md`. Mark anything not run in Unity as **unverified**. Keep HANDOFF short: it describes the present, and the History files hold the past.
- Nothing can be compiled outside Unity. Koda runs the validators on the PC and reports the results; never claim something works in Unity or the headset before that report.
