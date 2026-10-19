# Reorganisation plan (proposal only, nothing moved yet)

Written October 19, 2026 by Claude after reading the whole repository, at Koda's request. **No file has been moved, renamed or deleted.** Koda will ask for this to be carried out after the Quest 3 test. Until then, ChatGPT and Claude both keep working in the current layout.

Before carrying it out:
1. Ask ChatGPT what it is working on and which files it has uncommitted. Do the move only when its work is pushed, so nobody's edits land in a moved file.
2. Do the move in one commit containing only moves (`git mv`), so `git log --follow` keeps every file's history. Path fixes go in a second commit.
3. Update CLAUDE.md, README, the launchers, Tools/sync-project.cmd and the zip script in that second commit, and run the validators once on the PC.

## 1. What is wrong today

| Problem | Where | Why it hurts |
|---|---|---|
| Nine launchers at the root, five for scenes no longer worked on | `Open-Unity-Blockout/Forms/Interior/Robbery/Glass.cmd`, `Build-Windows.cmd`, `Run-Demo.cmd`, `Start-Demo.cmd` | It is not clear which one to click. Koda wants exactly three: **keyboard demo, VR demo, open in Unity**. |
| Ten zips in Downloads, nine of them historical | `Downloads/JewelryStore_{City,Expanded,Forms,Interior,Review,Robbery,…}.zip`, `NightAssets_20261011.zip`, `TripoInputs.zip` | ~20 MB of old snapshots; only `JewelryStore_Glass_…zip` is current, and its name says "Glass" while it now holds the night store. |
| Two copies of the code | `Prototype/` (source) and the zip / `UnityProject_*` (copies made by sync-project.cmd) | Editing the wrong copy is lost on the next sync. The zip has to be re-synced by hand after every batch. |
| Old scene builders mixed with the current ones | `Prototype/Editor`: 9 `Shop*` refinement steps plus `JewelryStoreBuilder`, `IntactStateBuilder`, `GlassToolValidation`, `JewelrySceneValidation` (October 8–11 shop) next to 15 `NightStore*` / `Night*` files and `StoreKit*` | 45 files and 20,700 lines in one flat folder; it is hard to tell what builds the night store. |
| Runtime scripts flat in one folder | `Prototype/Scripts`: 52 files covering VR, desktop, tools, camera, lighting, sound, UI and session | A new contributor cannot find the VR code at a glance. |
| 28 docs flat in `Docs/`, many of them dated batch reports | `ENVIRONMENT_BATCH_01/03/04`, `TOOLS_BATCH_02`, `REVIEW_BATCH_05`, `GLASS_AND_TOOLS`, `GEOMETRY_FORMS`, `INTERIOR_FINISH`, `ROBBERY_REFINEMENT`, `VERIFICATION_2026-10-08`, `ASSET_PREPARATION_2026-10-11`, `STORE_ASSET_REVIEW`, … | The few live documents (HANDOFF, ROADMAP, CONTROLS, ASSET_REGISTER, PC_VERIFICATION) are lost among history. |
| HANDOFF.md has 1,053 lines; ROADMAP.md has 387 | `Docs/` | Both are append-only logs. The current state is only in the top few entries. |
| Seven verification folders, one per historical variant | `Docs/Verification`, `VerificationCity`, `…Expanded`, `…Forms`, `…Glass`, `…Interior`, `…Review`, `…Robbery` | Only Glass and the night store (VerificationNight, produced on the PC) still matter. |
| Photos at the repository root | `Screenshot_20261011_…PDFPadActivity0.jpg` (2 MB), `store-reference.jpg` (0.8 MB) | CLAUDE.md says photos are never committed. The reference image belongs in Docs/References; the phone screenshot should leave the repo. |
| A decisions file with a space in its name, at the root | `for Koda.md` | Awkward to type in launchers and prompts. |
| README out of date | `README.md` | It still opens with the October 10–16 history and the Glass project, not "what to click". |
| Local-only work notes | `Docs/WORK_IN_PROGRESS.md`, `Docs/WorkInProgress/` (excluded in .git/info/exclude) | They describe a finished Claude workflow from October 14–15 and confuse readers who see them on disk. |
| Third-party packs: 30 folders, 485 MB, one level | `Prototype/ThirdParty/*` | This is fine as it is (each has LICENSE.txt, sources.json and README.md). Only add a grouping index; moving them would break the GUIDs and paths in builders. |

## 2. Proposed layout

```
Jewelry-store-vr-demo/
├─ README.md                    what this is, the three launchers, where to read next
├─ CLAUDE.md                    rules for assistants (kept; paths updated)
├─ AGENTS.md                    same rules for ChatGPT/Codex, plus "who is working on what" (new)
├─ Play-Keyboard.cmd            builds if needed, starts the demo in desktop (keyboard and mouse) mode
├─ Play-VR.cmd                  builds if needed, starts the demo in VR mode (Quest 3 over Link / Air Link)
├─ Open-In-Unity.cmd            syncs Prototype into UnityProject and opens the editor
│
├─ Prototype/                   THE source of truth: only edit here
│  ├─ Scripts/
│  │  ├─ Core/                  DemoSession, DemoLauncher, DemoModeSwitch, CrimeSceneState, ResettableSceneObject, DemoPhysics, InteriorBounds, ReturnToLauncher, SessionReviewRecorder
│  │  ├─ VR/                    XRControllerInput, XRLocomotion, XRHeadTracking, XRHandHud, XRToolWheel, XRHolsters, XRGhostHand, XRComfortFade, XRComfortVignette, HandInteractor
│  │  ├─ Desktop/               DesktopInteractor, ShopWalkController, DemoDesktopPanel, DemoOverviewMap, DemoBeacon
│  │  ├─ Tools/                 ToolHolder, ToolStation, ToolRackSample, DeployedTool, DemoToolGeometry, SceneTape, EvidenceCamera, CameraScreen, PhotoFrame
│  │  ├─ Lighting/              DemoLighting, LightBarBlink, NightQuality, GlowBillboard, UVTrace
│  │  ├─ Audio/                 DemoSounds, DemoSoundBank, DemoMusic, AmbientSound, AmbientZones, AmbientEvents, Footsteps
│  │  └─ UI/                    DemoUi, DemoUiTheme, DemoFeedback, DemoStatusBoard, DepthTestedLabel, PresenterSet, ProceduralMesh
│  ├─ Editor/
│  │  ├─ Build/                 DemoBuild, ForwardOnlyShaderStripping, PackImport, ThirdPartyModelImport
│  │  ├─ Shared/                DemoGeometry, DemoAssetLibrary, DemoToolsBuilder, KitMesh, UVTraceBuilder, PoliceLook
│  │  ├─ NightStore/            NightStoreBuilder, NightStoreLayout, NightStreet, NightInterior, NightStoreDressing, NightLighting, NightDecals, NightAudioBuilder, StoreKit, StoreKitProps, StoreKitDevices
│  │  ├─ Validation/            DemoValidation, NightStoreValidation*.cs (8), StoreKitValidation, ShopCityValidation
│  │  └─ Legacy/                the October 8–11 shop: JewelryStoreBuilder, IntactStateBuilder, ShopAssetDressing, ShopCityRefinement, ShopDisplayFinish, ShopFormRefinement, ShopGlassRefinement, ShopInteriorFinish, ShopPresentationExpansion, ShopRobberyDressing, ShopTextureFinish, GlassToolValidation, JewelrySceneValidation
│  ├─ Shaders/                  unchanged
│  └─ ThirdParty/               unchanged (paths are baked into builders and ASSET_REGISTER); add ThirdParty/INDEX.md grouping packs by use
│
├─ Docs/
│  ├─ HANDOFF.md                current state only (≤150 lines): what works, what is unverified, who works on what
│  ├─ ROADMAP.md                open items only; finished items move to History
│  ├─ CONTROLS.md               renamed from CONTROLS_AND_INTEGRATION.md
│  ├─ PC_VERIFICATION.md        how to test on the PC
│  ├─ DECISIONS.md              renamed from "for Koda.md"
│  ├─ ASSET_REGISTER.md         unchanged
│  ├─ PROJECT_BRIEF.md, SETUP.md, REHEARSAL.md, SCENE_LAYOUT.md, NIGHT_ARRIVAL_PLAN.md, REFERENCE_LAYOUT_PLAN.md
│  ├─ References/               store-reference.jpg, LayoutReferences/, TripoInputs/, TRIPO_GUIDE.md
│  ├─ Verification/             Night/ (from the PC) and Glass/; the old variant folders go to History
│  └─ History/                  the dated batch reports, old HANDOFF/ROADMAP entries (split by date), old verification folders
│
├─ Tools/                       unchanged (python helpers, sync-project.cmd); add zip_snapshot.py (the zip sync, now only in a Claude scratchpad)
└─ Downloads/
   └─ JewelryStore_Unity6000.6.2f1.zip   the one current snapshot (renamed from …Glass…); historical zips moved to a GitHub Release or deleted (they stay in git history)
```

## 3. The three launchers Koda asked for

ChatGPT has been told to make these. This is how they should map onto what exists, so the two assistants don't build different versions:

| Launcher | Built from | Behaviour |
|---|---|---|
| `Play-Keyboard.cmd` | `Start-Demo.cmd` (pull, build if the source changed, run) | Runs the exe with `-demo-mode desktop` (to be added: DemoModeSwitch reads it at start; today F9 switches). |
| `Play-VR.cmd` | The same build | Runs with `-demo-mode vr`. Note in the window: Quest Link or Air Link must be on and the Oculus/Meta runtime set as the OpenXR runtime. |
| `Open-In-Unity.cmd` | `Open-Unity-Glass.cmd` | Syncs Prototype into **one** project folder (`UnityProject`) and opens the editor with `-noUpm`, opening the night-store scene. Replaces Glass, Blockout, Forms, Interior and Robbery. |

`Build-Windows.cmd` and `Run-Demo.cmd` become redundant. One build serves both play launchers. `Start-Demo.cmd bake` survives as `Play-Keyboard.cmd bake` (or a fourth helper `Tools\build-baked.cmd`).

## 4. Keeping Claude and ChatGPT in sync

- **AGENTS.md** at the root: the same rules as CLAUDE.md, which ChatGPT/Codex reads, plus a short table:

  | Assistant | Working on | Files | Since |
  |---|---|---|---|

  Each assistant updates its own row before starting and clears it when done. Claude currently works in `Prototype/Scripts/XR*.cs`, HandInteractor, EvidenceCamera and the ShopCityValidation VR checks.
- **One source:** only `Prototype/` is edited. The zip becomes a generated artifact, built by `Tools/zip_snapshot.py` (to be committed). Either assistant can regenerate it, or it can be dropped from git entirely once the launchers build from Prototype.
- **HANDOFF.md** holds the current state only. History goes to `Docs/History/HANDOFF-2026-10.md`, so neither assistant has to read 1,000 lines to know where to start.

## 5. Things that must not break during the move

- Builders refer to `Assets/CrimeSceneDemo/ThirdParty/<pack>/…` and `Assets/CrimeSceneDemo/Scripts`. Moving files into subfolders of Scripts/Editor is safe: Unity finds scripts by GUID, and sync-project mirrors whole folders. Moving or renaming ThirdParty packs is **not** safe without updating every builder path, so leave them.
- `DemoValidation.ScenesFolder` and `DemoBuild.LauncherScene` hard-code `Assets/CrimeSceneDemo/Scenes`. Keep that.
- The existing `.meta` files in the zip and project must move with their scripts, or the GUIDs change and scene references break. Use the `git mv` of the source and let sync-project re-mirror; the project-side .meta files are left alone by robocopy `/XF *.meta`. After the move, the old empty folders' metas need deleting once.
- The validators write into `Docs/Verification*`. Update those output paths in DemoValidation, NightStoreValidation and ShopCityValidation when the Verification folders move.
- The launchers and README refer to `UnityProject_Glass` and `UnityProject_Demo`. Keep the old folder names working for one round (or copy once) so Koda's PC setup is not lost.

## 6. Order of work when Koda says go

1. Agree with ChatGPT on a quiet moment, then pull.
2. Commit 1, moves only: Scripts and Editor into subfolders; docs into History and References; root photos and `for Koda.md`.
3. Commit 2, fixes: paths in validators, launchers, sync-project, README, CLAUDE.md and the new AGENTS.md; the three launchers (or adopt ChatGPT's, if they're already in); `Tools/zip_snapshot.py`; HANDOFF trimmed; the zip rebuilt.
4. Koda runs `Open-In-Unity.cmd`, checks that the Console is clean, then runs `RunNightQuick` and `RunGlass`.
5. Delete the historical zips and the old launchers only after step 4 passes.
