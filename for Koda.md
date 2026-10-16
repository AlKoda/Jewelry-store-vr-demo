# For Koda

Updated October 16, 2026. Demo deadline: **October 27, 2026**.

## Your decisions

1. Country / police service: which service, language and livery should be represented?
   Prepared tape and signage are generic; the existing KayKit police car stays provisional.
   Since October 15 every police choice in the night store comes from one setting,
   `PoliceLook.Current` in Prototype/Editor/PoliceLook.cs (unverified, never compiled):
   - **Provisional** (the default) keeps today's look: the KayKit police car with its red and
     blue light bar, the CRIME SCENE - DO NOT CROSS tape, no sirens, no radio and no police
     figure. Its one addition is a generic V8 engine idle at the car.
   - **ProvisionalRealisticUS**, for your review only: a photoreal 1980s American police
     sedan (CC BY 4.0, credited in the app), American wail and yelp sirens, radio traffic,
     and an armed officer standing on the pavement right of the shop. To see it, change that
     one line, then rebuild.
   Neither value is a decision. Once you choose a service, it becomes one more value with its
   own car, livery, tape text, siren and radio.
   Since October 16 the shop also has wall sockets and light switches. Each is a single 82 mm
   square plate: the sockets have a blank face with no pin holes, the switches one plain
   rocker. Their size, pin pattern and switch style follow the country, so they stay generic
   until you decide.
2. Entry: did the offender enter through the door or the display window? This determines
   the final fracture pattern, debris, route and placement of marks (see 8).
3. UV traces: what simulated material/observation is the instructor demonstrating?
   Keep placeholder UV traces hidden until agreed. Fluorescence does not identify a substance.
4. One real measurement: frontage width, entrance width or another known dimension.
   The current 16 x 12 m footprint is still an estimate.
5. Burglary tools: should any discarded tools form part of this story? Since October 15 a
   crowbar, a sledgehammer and a flathead screwdriver exist as placeholders inside the hidden
   "Case evidence" set (see 8). Nothing shows at arrival.
6. Alarm bell: is the alarm still ringing when the police arrive? The bell box is on the
   fascia either way. The ringing is the "Alarm bell" switch in the presenter drawer's Scene
   tab, off at arrival, and a session reset silences it again. Continuous ringing would also
   tire a session.
7. Everyday wear: should normal wear be visible in the shop? The "Ambient wear" switch shows
   a water stain inside the entrance, heel scuffs, fine scratches and dust along the wall
   base. It is hidden at arrival, because wear can read as evidence. For the same reason the
   GlassSmudge overlay (fingerprint-like smudges on the case glass, PBRInterior pack) is not
   applied anywhere. Should either be on, and where?
8. Case evidence contents (goes with decisions 2 and 5): the "Case evidence" switch shows a
   placeholder set, hidden at arrival:
   - a mud shoe trail on the pavement up to the forced entrance doors, with tracked dirt just inside;
   - 15 dust shoe prints along the walk route to the office opening;
   - pry marks on the right door leaf at 1.05 m;
   - four latent fingertips on the central case top and a handprint on a cracked island top;
   - the three tools from decision 5;
   - since October 16, a cut CCTV cable hanging under the rear-left security camera.
   Which of these belong to your story, and where should they go?
   The office has no video recorder (DVR) at arrival. The options are:
   - (a) an intact recorder on the drawer unit;
   - (b) its spot empty, with loose cut cables (the recorder was taken);
   - (c) a damaged recorder.
   A model is ready for any of them: InteriorModels DVRRecorder (project-made, treated as CC0,
   206 triangles, one or two extra materials). Which fits the story?
9. Stock in the safes: under the closing procedure (stock into the safes by 17:45, under
   dual control) the cases now hold display furniture only at night. The burglars smashed
   empty cases, forced both safes and dropped a little stock: a gold bar, a gem parcel, ring
   boxes and coins in the safes, a loupe on the desk, and a pearl strand, a coin and a ring on
   the floor. The setting is `NightStoreDressing.StockInSafes` (true); false restores the
   October 11 version, with stocked display decks. Which matches your scenario?
10. Fire alarm call point: none is placed. The available model is an American pull station,
    which would fix the country before decision 1 is made. Which style should it be? This
    follows from decision 1.

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

October 15 (unverified, never compiled): the twelve realism packs are now wired into the night
store, each with the old look as its fallback:
- PBR floors, stone, wood, metals and facades;
- Poly Haven street lanterns and an apartment block across the street;
- interior props and framed wall graphics;
- the left-behind jewellery;
- puddles, litter and glass shards on the floors;
- a street soundscape with footsteps, music in the start menu, and in-app credits.
Docs/HANDOFF.md lists every change. The presenter drawer's Scene tab gains these switches:
- "Light bar flashing", with a note for visitors sensitive to flashing light;
- "Alarm bell", "Ambient wear" and "Case evidence" (decisions 6 to 8), all off at arrival;
- "Underscore music", off at arrival.
A session reset makes the light bar flash again and hides the alarm bell, the wear and the
evidence. The underscore music keeps its setting through a reset.

October 16 (unverified, never compiled in Unity): more detail where visitors stand:
- the display and wall cases, the counter, both safes, the forced entrance doors, the shop
  windows and the side-room doors (thicker case glass with a green edge, bevelled frames,
  locks, hinges, door contacts);
- trim round the office opening, skirting, a shadow gap under the ceiling, switches and
  sockets;
- motion detectors, a second alarm keypad, ceiling camera domes and smoke detectors;
- three bare trees across the street.
The small jewellery, coins and pearls are slightly less glossy, so they sparkle less in the
headset. None of this changes a decision above.

## Remaining integration and manual review

1. Use an isolated project with the latest Prototype Scripts, Editor, Shaders and ThirdParty.
   Preserve all existing .meta files. Unity must generate/import new assets and their .meta files.
   The older extracted UnityProject_Glass is a historical baseline, not the latest night source,
   unless it is opened with Open-Unity-Glass.cmd: since October 15 (unverified) that launcher
   first mirrors the latest Scripts, Editor, Shaders and ThirdParty into it.
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
7. October 15 wiring, in the headset and on speakers:
   - whether the crossfading light bar and its coloured washes are comfortable to look at
     (the "Light bar flashing" switch holds the bar steady);
   - the sound levels of the street, the room tone, footsteps, passing cars and the dog;
   - whether the reverb sounds natural;
   - whether the glossier PBR floor and the darker velvet read well under the torch;
   - since October 16: whether the dropped ring, the coins and the pearl strand still sparkle
     too much under the torch, and whether the green edge on the case tops reads as glass;
   - since October 16: whether the three bare trees across the street suit late October, or
     should keep some leaves. Leaves need a leaf texture the project does not have yet, so
     they are left out for now.
8. You own headset comfort, measured VR FPS/frame time, final scenario decisions and the
   instructor walkthrough. No Unity execution, bake, headset test or standalone build was
   performed for this asset-preparation batch. File checks are not performance certification.

Suggested remaining milestones: October 12–16 integration/layout walkthrough; October 17–20
story placement and lighting; October 21–23 headset profiling/readability; October 24–26 full
rehearsal and tested backup. Keep exploration untimed and unscored throughout.
