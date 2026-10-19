# Staff-room polish � October 18, 2026

Latest local Windows player: Builds/StaffRooms20261018/JewelryStoreDemo.exe. Start-Latest.cmd opens this checkpoint.

- Removed the outdoor folding table, easel, photo frame and status board from the active scene. Physical tool samples and camera are retained on an indoor workbench in the left staff room; the UI remains available.
- Furnished the left room with the existing storage, a modest server bank and tool bench; the right room has a small break table, two chairs, a mini fridge, coffee machine and water dispenser. Added office seating. Added physical furniture bounds, preserving room entry paths.
- Roughened the added architectural and prop metals, including the packed smoothness textures. New prop finishes use separate material assets, preserving shared store/car materials.
- Added 29 synthetic background fingerprints on case tops, the counter, desk, fridge and staff door. These represent ordinary customer/worker contact as well as potentially relevant traces; no scoring or attribution is implied.
- Added six small dried floor-blood clusters with aligned faint green UV overlays. Fingerprints are invisible under white light; dried blood remains faintly visible under white light. The original nine door/handle UV cues remain, for 44 authored traces total. Visibility retains range, beam, facing and occlusion checks; traces have no collision or additional lights.
- The car model, its materials and lighting were left as requested; its replacement is deferred.

Source: Prototype. Tested project: .asset-cache/combined-20261017/UnityProject_Demo. Output target: Builds/StaffRooms20261018/JewelryStoreDemo.exe. Actual headset/OpenXR verification remains pending.


Validation and build details:

- Scene construction passed STAFF_ROOM_POLISH_PASS: all 44 authored traces, indoor physical tools, inactive exterior presentation equipment, furniture within room exterior bounds.
- Strict Windows generation passed in Builds/staff-rooms-gpu-build.log; final reflection-corrected rebuild passed in Builds/staff-rooms-reflection-custom-build.log.
- Fresh explicit captures replaced corrupt cached reflection images. ReflectionCapture bakes each probe with indirect reflections suppressed to avoid feedback, then assigns the result as a Custom static cubemap; this avoids LightingData replacing it with a stale baked texture when loading the player. No realtime capture cost. GPU-enabled baking is required, and the helper rejects a Null graphics device.
- Final targeted player verification: Builds/staff-rooms-custom-player.log, EXPANDED_ONLY_PASS. Rendered-pixel differences: 777 green pixels for the original door smear, 197 for a case fingerprint and 591 for a floor cluster. White-light hiding and occlusion passed; screenshots visually checked after correcting reflections.
- Final full-player result: Builds/staff-rooms-release-player.log, COMBINED_PLAYER_PASS. Full interaction regression was also run for all four doors, desktop traversal, simulated torso/controller pushes, smooth VR pushing, torch/camera transfer, sound references, language switching and 2K shadows. Actual headset/OpenXR remains untested. Do not use the unfocused automated frame-time sample as a performance benchmark.
- Current captures: Docs/VerificationStaffRooms20261018. Compact Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip and complete Builds/StaffRooms20261018/JewelryStore_Complete_Unity6000.6.2f1.zip are regenerated from the tested project, with metadata preserved and ZIP CRC validation. The earlier checkpoint remains in Builds/Expanded20261018.
- No commit or push performed.

Verified archives: compact 83,258,398 bytes; complete 478,304,714 bytes, both CRC checked.


## English UI fitting follow-up

Applied the same 510-unit drawer and 90 x 94 hotbar slots to both languages. Label/button width fitting now handles English too; tool-tile labels and shortcut/count labels have enough line height for the runtime font. Verified visible-player English and Arabic hotbars/tool panels at 100%, 125% and 200% scale; no clipped tool names in the captures. Builds/bilingual-fit-release-build.log: REVIEW_PLAYER_BUILD_PASS. Builds/bilingual-fit-visible-player.log: EXPANDED_ONLY_PASS. Captures remain under Docs/VerificationStaffRooms20261018/ui-english-* and ui-arabic-*. Archives regenerated and CRC checked: compact 83,258,384 bytes; complete 478,304,700 bytes. Start-Latest.cmd opens the updated player.
