# For Koda

Updated October 11, 2026. Demo deadline: **October 27, 2026**.

## Your decisions

1. Country / police service: which service, language and livery should be represented?
   Prepared tape and signage are generic; the existing KayKit police car stays provisional.
2. Entry: did the offender enter through the door or the display window? This determines
   the final fracture pattern, debris, route and placement of marks.
3. UV traces: what simulated material/observation is the instructor demonstrating?
   Keep placeholder UV traces hidden until agreed. Fluorescence does not identify a substance.
4. One real measurement: frontage width, entrance width or another known dimension.
   The current 16 x 12 m footprint is still an estimate.
5. Burglary tools: should any discarded tools form part of this story? None added in this batch.

## Prepared for you

The ten ReferenceTextures Color/Normal sets were inspected and preserved byte-for-byte.
New assets are under Prototype/ThirdParty:

- **NightMaterials**: CC0 ceiling plaster and dark facade marble, Color/Normal/Roughness
  plus Standard metallic/smoothness maps. Ten small constant roughness/packed pairs
  match the existing placeholder material values. They are not measured PBR scans.
- **NightGraphics**: fictional Aurel & Finch fascia, collection signs, Closed and Staff
  Only signs, generic scene tape, number atlas, shoe-trace/scuff/dust decals. Ten PNGs
  plus editable SVGs. No real brand/service logos; replace the fictional name if desired.
- **NightAudio**: four Kenney CC0 interaction sounds plus a neutral synthesized 16-second
  ambience loop. No voices, music or sirens; volume suggestions in the folder README.
- **NightModelReuse**: measured selection of 12 existing CC0 OBJ files, including the
  police-car assembly and furniture. Reuse their existing folders and shared atlases.

Each folder has source/author/license documentation in LICENSE.txt. See
[asset report and previews](Docs/ASSET_PREPARATION_2026-10-11.md).
Downloads/JewelryStore_NightAssets_20261011.zip is an **asset-only package**, not a Unity project or build.

## Remaining integration and manual review

1. Use an isolated project with the latest Prototype Scripts, Editor, Shaders and ThirdParty.
   Preserve all existing .meta files. Unity must generate/import new assets and their .meta files.
   The older extracted UnityProject_Glass is a historical baseline, not the latest night source.
2. Run StoreKitValidation.RunGallery and NightStoreValidation.RunNightQuick on current source
   before adopting these finishes. Then wire materials/sign PNGs/audio explicitly using each
   folder README. Copying the new folders alone does not wire them into StoreKit or DemoSounds.
3. Check scale and orientation of optional furniture; retain procedural safes/doors/cases
   with their hinged/broken variants. Keep the police car's four authored wheel offsets.
4. Place only case-approved decals. Check alpha sorting, z-fighting and readability under
   white light. Do not turn the generic marks into automatic UV evidence.
5. Audition the five WAVs on speakers/headset. Adjust for your voice; check the ambience
   loop, switch timing and mute controls. Audio was decoded/analysed, not heard in Unity.
6. Run NightStoreValidation.RunNight for the baked version; inspect daylight/night captures,
   headlight alignment, flashlight occlusion, reflection brightness, saved photos and reset.
7. You own headset comfort, measured VR FPS/frame time, final scenario decisions and the
   instructor walkthrough. No Unity execution, bake, headset test or standalone build was
   performed for this asset-preparation batch. File checks are not performance certification.

Suggested remaining milestones: October 12–16 integration/layout walkthrough; October 17–20
story placement and lighting; October 21–23 headset profiling/readability; October 24–26 full
rehearsal and tested backup. Keep exploration untimed and unscored throughout.
