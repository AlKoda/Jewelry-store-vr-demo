# Applying and verifying the October 14–16 realism work on the PC

Nothing from October 14–16 has run in Unity yet. The only compile so far was in the cloud: a Roslyn C# 9 compile of every Prototype/Scripts and Prototype/Editor file against Unity 2021.1 reference libraries (the Unity 6 Find*ByType APIs shimmed). It reports two errors, both at NightLighting.cs:624. They are `LightingSettings.lightmapCompression`, which exists in Unity 6 but not in those older libraries. The runtime scripts compile with no error.

This page gives the order to apply and check the work in. The details behind each step are in the top four entries of [HANDOFF](HANDOFF.md).

## What is on main

| Commit | What it brings |
|---|---|
| 03ad44c | Performance and realism fixes from the audit, including ForwardOnlyShaderStripping.cs and MapAlbedo.shader. |
| ae7b302 | Twelve realism packs under Prototype/ThirdParty, about 262 MB. They are not in the Downloads ZIP; the launchers copy them in. |
| 9305177 | The packs wired into the night store: street, lighting, interior, closing procedure, decals, sound, presenter switches and credits. Also adds Tools/sync-project.cmd. |
| 6ceceea | 83 validator checks for that wiring, plus the builder bugs they exposed. |
| (this commit) | The fixture detail and realism gap round: closer detail on the cases, counter, safes, doors and bays; trim; security devices; three street trees; lower gloss on small metals; 19 more checks. |

## Steps

1. **Pull.** Close Unity, then run `git pull` on main. The first pull fetches the packs (about 262 MB).
2. **Open through a launcher.** Both launchers call Tools\sync-project.cmd, which mirrors Scripts, Editor, Shaders and ThirdParty into the project.
   - `Open-Unity-Glass.cmd` mirrors into UnityProject_Glass, beside the README, and opens the editor. The validators write to Docs from this project. If UnityProject_Glass does not exist yet, extract Downloads\JewelryStore_Glass_Unity6000.6.2f1.zip into it first.
   - `Start-Demo.cmd` mirrors into UnityProject_Demo and builds Builds\Windows\JewelryStoreDemo.exe. Use `Start-Demo.cmd bake` for the final look.
   - The first open reimports about 1,200 pack files under the new import rules. Note how many minutes it takes.
   - If Unity is not at the Hub's default path, set `JEWELRY_UNITY` to Unity.exe.
3. **Compile.** The Console must show no compile error.
   - A CS0260 error means an old, non-partial NightStoreValidation.cs is still in the project. Run the launcher again.
   - Write down any other error with its file and line.
4. **Run the validators** on UnityProject_Glass, with the editor closed, in the order below. Use this command, changing the method name each time:

   `"%JEWELRY_UNITY%" -batchmode -noUpm -projectPath "%CD%\UnityProject_Glass" -executeMethod NightStoreValidation.RunNightQuick -logFile night-quick.log`

   The table gives the expected extra PASS lines over the count before 9305177.

   | Validator | All twelve packs mirrored | Project from the ZIP alone |
   |---|---|---|
   | `NightStoreValidation.RunNightQuick` (no bake) | +127 | +78 |
   | `NightStoreValidation.RunNight` (bake) | +126 | +77 |
   | `StoreKitValidation.RunGallery` | +11 | +11 |
   | `ShopCityValidation.RunGlass` (the old shop must look and sound as before) | +12 | +11 |

   - The ZIP-alone column is an estimate, added up from the per-batch figures in HANDOFF.
   - Each night figure is one lower if the "Menu theme fade not checked" Info line appears.
   - Every night and ShopCity figure is one lower if the project does not sit beside Tools\sync-project.cmd.
   - For RunNight, note the bake time in seconds and the `Lightmaps=` line.
   - A run stops at its first FAIL. Copy that FAIL line together with the Info lines just above it.
   - The night store's results go to Docs/VerificationNight/runtime-results.txt. Keep that file and the `Budget:` lines.
5. **Set MaterialBudget from the log.** Set `NightStoreValidation.MaterialBudget` to the logged `Budget: materials=` value plus 8. It is 136 now; about 129 is expected.
6. **Check on the desktop (mouse and keyboard), on a baked build.**
   - **Street:** the lanterns, the apartment block with its lit rooms, the wet street, and the three bare trees.
   - **Shop front:** the forced doors with their new frames and pull bars, the stickers, and the bell box.
   - **Showroom:**
     - the cases up close: glass edges, posts and rails, locks, LED channels, and no z-fighting at the frame joints;
     - the counter, the paintings and the plants;
     - the trim and devices: skirting, the ceiling shadow gap, switches and sockets, PIR detectors, CCTV domes and smoke detectors.
   - **Office:** the safes with their bolt-work, the stock left inside, the monitor, and the second alarm keypad.
   - **Presenter drawer, Scene tab:** "Light bar flashing", "Alarm bell", "Ambient wear", "Case evidence" and "Underscore music". Each must switch on and off, and a session reset (F10) must put it back. "Case evidence" now also shows a cut camera cable.
   - **Evidence photo:** take one towards a lamp. The halos must face the photo camera.
   - **Frame Debugger:** note the batches and SetPass calls in the showroom.
7. **Check in the headset (Quest 2 over Link), on a baked build.**
   - The light-bar crossfade and the red and blue washes must be comfortable. The "Light bar flashing" switch holds the bar steady.
   - The halos must look right in both eyes.
   - Small metal parts must not shimmer. The gloss is now capped at 0.72.
   - No z-fighting on decals, trim or case frames.
   - The shards on broken case decks may float about 4.5 mm above the suede. Say so if you can see it.
   - No visible LOD pops.
   - Judge the frame time only on a baked build.
8. **Listen on speakers and in the headset.**
   - The street is muffled inside.
   - There is reverb across the showroom and none on the pavement.
   - The loops have no clicks.
   - Note whether any level is too loud or too quiet.
9. **Decisions for Koda** (see "for Koda.md"):
   - 1: country and police service. `PoliceLook` stays Provisional, today's look. The switch and socket faces are blank for the same reason.
   - 9: `NightStoreDressing.StockInSafes = true` follows the closing procedure, so the cases are empty at night. One line restores the stocked cases.
   - The rest are still open: the entry route, the UV traces, tools and evidence, the alarm bell, everyday wear, the call point, and whether to add a DVR recorder to the office.

## Prompt to paste into ChatGPT or a new Claude session

> You are working on AlKoda/Jewelry-store-vr-demo (Unity 6000.6.2f1, Built-in pipeline, no packages, launchers use -noUpm). Read CLAUDE.md, then Docs/PC_VERIFICATION.md, then the top four entries of Docs/HANDOFF.md. All work since commit 03ad44c is unverified in Unity. I ran the steps in Docs/PC_VERIFICATION.md on my PC; below are the Console errors, the first FAIL line with the Info lines above it, the `Budget:` lines and my notes from the desktop and headset checks. Fix the cause of each problem in Prototype/ (do not weaken a check unless the check itself is wrong, and say which), keep every fallback for missing packs, and add a validator check for any new behaviour. Then update Docs/HANDOFF.md and Docs/ROADMAP.md, and sync Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip (new .cs files need a .meta with a fresh guid; the twelve realism packs stay out of the zip). Pull main first and commit directly to main. Do not decide the open questions in "for Koda.md".
>
> [paste the Console errors, the FAIL line with its Info lines, the Budget lines and your notes here]
