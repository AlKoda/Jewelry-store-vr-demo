# Realism packs wired into the night store — October 15 (Claude, unverified, never compiled)

The twelve realism packs now feed the night store (JewelryStoreNight), for Quest 2 over Link and for mouse and keyboard. An architect planned the batch, a skeptic checked the plan against the code and the pack files, and a second architect pass resolved every blocking point. Prep scripts then made LOD variants and atlases inside seven packs, each file group was implemented in turn, a validator pass added the checks listed below, and a cross-review read the whole diff. A C# parser found no syntax errors in the 85 Editor and Scripts files, and a script that resolves every static `Class.Member` access found no renamed or removed names. Every placement goes through DemoAssetLibrary (or StoreKit's set table) and keeps a fallback: when a pack file is missing, the night store builds today's primitive or procedural version, so a project unpacked from the ZIP alone still builds today's store. **Nothing here has been compiled or run in Unity.** The night store itself has never run (there is no Docs/VerificationNight), so every budget figure below is an estimate.

- **What the guest sees and hears, by area** (default settings, all twelve packs mirrored into the project):
  - **Arrival and the street:**
    - The police car is today's KayKit car_police with its red and blue light bar at 1.5 Hz. The bar now crossfades at each switch instead of snapping, and it lights dim red and blue washes on the street and on the shop floor. A V8 idle runs at the bonnet.
    - Four Poly Haven lanterns (StreetModels StreetLamp, no shadows) replace the KayKit lamps. Each light sits at its bulb, 3.27 m up inside the glass, and its baked pool falls through the NightSky streetlight cookie, with the sharp edge towards the facades.
    - Soft halos glow on the four lantern glasses and the two headlamps. They are camera-facing cards, not lights; a headlamp's halo shows only from in front of the car.
    - Across the street: the ApartmentBlock with its lit rooms (lightmapped), the rendered row on either side with half-closed metal shutters, and weathered fly posters on its blank ground floor. The KayKit skyline towers are gone. The sky is the generated overcast night panorama.
    - The neighbours: brick on the left, render on the right and metal shutters on both shopfronts, with graffiti tags on the left shutter, a utility box against the left neighbour and grime under the right neighbour's sill.
    - Street furniture: four anthracite bollards (StreetModels, with shadows) in the anti-ram row, two gully grates in the gutter, and across the street a bus shelter with a lit lightbox and two bike racks. There are no road signs, hydrants or name plates, because they depend on the country.
    - Kit and people: a ForensicCase beside the equipment table and a box of nitrile gloves on it; one passer-by on his phone on the pavement left of the shop, 3 m outside the play area; a lost-cat flyer on the first lamp post.
    - Surfaces: the wet asphalt and the wet paving slabs carry their PBRExterior colour, normal, smoothness and AO maps, and the kerbs are concrete. Always-on decals: puddles, oil, wet leaves, litter and chewing gum on the street and both pavements.
    - Sound: a stereo city bed, the wind, the V8 idle, a car passing unseen every 20–60 s and a distant dog every 1–4 minutes (the CC0 recording only). No sirens and no radio.
  - **Shop front:** an alarm bell box on the fascia (silent at arrival); three stickers on the left sidelight (alarm monitored, insurer approved, cards accepted) and a CCTV sign on the left pier. The wind whistles through the forced doors. The piers and fascia keep the NightMaterials dark marble.
  - **Showroom:**
    - Surfaces from PBRInterior: a polished marble floor (glossier than before, with AO), warm plaster, walnut, marble tops, velvet, suede, brushed bronze, brushed stainless, black leather and a plasterboard ceiling.
    - The closing procedure: stock went to the safes by 17:45, so the cases hold display furniture only. Glass shards lie on the floor in front of the three broken cases. A pearl strand and a gold band lie on the floor where the burglars dropped them, clear of the walk route and the UV traces.
    - Props: snake plants in place of the front and rear-left planters, a rug under the sofa and side table, a fire extinguisher right of the office opening and a wall clock above it, and four air diffusers on the ceiling.
    - Wall graphics: Heda's still life in a gilt frame replaces the rear sign frame's emblem, and the Aurel & Finch lightbox replaces the counter sign frame's. The fire evacuation plan hangs on the rear partition, and Pissarro's Boulevard Montmartre, also gilt-framed, over the seating corner.
    - Sound: the street is muffled inside (low-passed to about 1.5 kHz and lowered to 55 %), under a room tone, with a Room reverb.
  - **Office:** the two safes finished in PBRInterior's powder-coated safe steel; a snake plant in the office planter; a monitor with a glowing screen (no light) and a keyboard on the desk, with the transport tray moved to (-1.25, 0.75, 9.4) to clear them; the jeweller's loupe on the desk and a coin on the floor; in the open large safe a 1 kg gold bar, a gem parcel, three closed ring boxes and two coins, and in the tall safe one coin; a staff notice board, the October 2026 calendar and a gemmology certificate on the office side of the partition; a reverb zone of its own.
  - **Footsteps (desktop):** a step every 0.7 m of walking, and glass steps in the glass zones (the entrance fans and the strips in front of the broken cases). In VR, room-scale walking, teleports and snap turns make no steps.
  - **Hidden at arrival:** the alarm bell, the "Ambient wear" and "Case evidence" sets and the investigation underscore, each a switch in the drawer's Scene tab (see below). No print-like or footwear-like mark is visible at arrival.
  - **Menu and reset:** the start menu plays the menu theme and fades it out over 0.6 s before a map loads, and a session reset plays a short sting. The Settings tab lists the credits of every placed CC BY or MIT asset, and the start menu shows their first line above the build stamp.
  - **The old shop (JewelryStoreGlass) looks and sounds as before.**
- **Police look (PoliceLook.cs, new).** Every police-specific choice now sits behind one setting, `PoliceLook.Current`, because the country and the police service are still Koda's decision 1 (see "Open decision" below). The builder, NightLighting, the tool styling (DemoToolsBuilder's new optional tape text) and NightAudioBuilder all read `PoliceLook.Active`, and `NightStoreBuilder.PlacedCar` records which car a build placed.
  - `Service.Provisional`, the default, reproduces today's look exactly: the KayKit car_police with today's anchors, 0.3 x 0.12 headlamp lenses and 0.38 x 0.07 x 0.16 light-bar halves at x ±0.2, red and blue at 1.5 Hz, and the "CRIME SCENE - DO NOT CROSS" tape. It has no sirens, no radio and no police figure. Its one accepted addition is a generic V8 idle at the bonnet (NightSoundscape PoliceCarIdle_Loop.wav), which fits any country's patrol car.
  - `Service.ProvisionalRealisticUS` ("Provisional realistic US, for review") swaps in the photoreal 1980s US sedan (StreetModels/PoliceCar, CC BY 4.0, with its LOD files) with anchors measured on its OBJ: headlights at (±0.52, 0.653, 2.215) with 0.33 x 0.11 lenses, and the light bar at (0, 1.48, -0.255) with halves that enclose Lightbar_glass. It adds four US wail and yelp sirens, radio traffic (RadioChatter, squelch, beeps and PoliceCarRadio_Loop.ogg at the dash) and an armed officer (PeopleModels PoliceOfficer_Standing) at (12.5, 0, -1.6) facing the shop. It is selected only by editing that one line. While it is selected, "Police choices come from the PoliceLook setting, Provisional by default" fails on purpose.
- **Import rules (PackImport.cs, new; ThirdPartyModelImport.cs):**
  - One AssetPostprocessor rule table for the twelve packs, keyed on the pack folder and the file suffix. For textures it sets the type, sRGB, cut-out and blended alpha, wrap, NPOT, aniso, compression and maximum size. Cookies, normal maps, the linear MetallicSmoothness and AO maps and the HDR skies each have their own rule. For audio, the loops and the long one-shots (sirens, car passes) are compressed in memory as Vorbis, the other one-shots are PCM, and the music streams. A Python copy of the texture rules, run over all 626 pack images, gave the planned value for every sample.
  - The older packs and the Source~/Generators~ folders are not touched. `PackImport.Apply()` reimports, in one batch, any pack texture or clip imported before the rules existed. `PackImport.CutoffOf` lets material code alpha-test at the import cutoff.
  - Model import: models outside the twelve packs keep their lightmap UVs as before. Inside them, only the StreetModels buildings (ApartmentBlock, Building*) and the rug keep them, with Calculate chart margins at 6 and 16 texels per metre; every other pack prop is probe-lit. Pack models without normals get calculated normals (30 degrees for the street models, 60 for jewellery and tools). Pack models import no animation, cameras or lights. `GenerateLightmapUVs()` now batches its reimports and adds `PackImport.Apply()`'s count; its two callers are unchanged.
- **Asset library (DemoAssetLibrary, DemoGeometry, ProceduralMesh):**
  - `PlacePack` places a pack model with the LOD levels from the prep files, in LOD classes with fixed screen-height transitions (Tiny 2 % / 0.6 % / cull 0.1 %, Prop 10 / 3 / 0.4 %, Large 30 / 10 / 0.4 %, Post 60 / 20 / 0.5 %), a fade mode of None and no colliders on the levels. A missing model returns null.
  - `PackMaterial` builds one shared material per pack, colour map, Kd colour (RGBA) and role (Glass, Cutout, Emissive or Opaque), saved under Assets/CrimeSceneDemo/PackMaterials. The finish, emissive and cut-out tables are keyed by pack, model and material, never by a bare name, so the Proc_PaintedMetal tints, the monitor's and the snake-plant pot's Shared_PlasticBlack, and the passer-by's Props and PropScreen stay separate, and the PoliceCar's "Interior" never glows. Transparency comes from the MTL d value (imported alpha below 0.99), never from a name, so the ApartmentBlock's WindowFrames stay opaque. Map-less finishes (for example PoliceCar Body and Lights at 0.89 smoothness, the lamp bulb and the lightbox emission) come from the packs' sources.json, models.json and materials.json.
  - Also new: atlas cell lookup from the pack JSON files (explicit pixel rects, y from the bottom), `Seat`, `DemoGeometry.Fade` and `Cutout`, `ProceduralMesh.Quad` with a UV rect, and credits: every placed CC BY or MIT model records its attribution, and NightStoreBuilder writes them to Resources/DemoCredits.txt. The importer helpers (NormalMapAt, DataMap, SignTexture, Audio) leave governed pack files to PackImport.
- **PBR palette (StoreKit, StoreKitProps).** Each palette texture name has one source, in a fixed priority: the first set whose colour file exists supplies the colour, normal, MetallicSmoothness and AO maps and the tiling, so maps never mix across packs. The order is PBRInterior/PBRExterior, then ReferenceTextures (NightMaterials for the ceiling), then GeneratedTextures.
  - New materials: KitBrick (the left neighbour) and KitShutter (the half-closed shutters of the neighbours' and the across-street shopfronts). Stainless, Concrete, Render and Leather now take PBR sets. Six surfaces get AO maps: the floor, pavement, asphalt, render, brick and shutter.
  - FacadeStone stays the NightMaterials dark marble, on today's tint (`Calibrated = false`). GlassSmudge is not applied (it reads as fingerprints; Koda's decision 7).
  - The ReferenceTextures and generated sets keep their scalar finish. The NightMaterials 16 px constant maps equalled those scalars, so they look the same.
- **Street and interior (NightStreet.cs, NightInterior.cs, new; NightStoreLayout, NightStoreBuilder):**
  - `NightStoreLayout.LampProfile` holds the lantern (light at 3.27 m, baked intensity 3.5) and the KayKit lamp as its fallback. Each post keeps exactly one "Lens", sized to fit inside the glass, and every lantern renderer is ShadowCastingMode.Off.
  - The ApartmentBlock sits under Exterior/Buildings at StreetScale, matching its 6 texels/m import margin. Props whose footprint reaches a movement region get one bounds collider; blended renderers cast no shadows.
  - The fire extinguisher (Prop LODs) stands at x 1.05, set back until its stand is 6 mm off the walnut skirting; the clock uses its LOD1 file as the top level. The ceiling diffusers hide with the ceiling on the overview map.
  - The wall graphics are three KitMeshes from the two WallGraphics atlas pages, in three materials (interior page, street page, gilt moulding). Without the atlases the sign frames keep their emblems.
- **Night lighting (NightLighting.cs, LightBarBlink, DemoLighting, ToolStation, GlowBillboard.cs new).** NightLighting reads the lamp profile and the placed car in Lights, UseRealtimeFallback, UseBaked and LightBar.
  - The streetlights take their intensity and spot angles from the profile, and the cookie_streetlight_512 is baked into their pools. The headlamp lenses and light-bar halves follow the placed car's preset.
  - Glow without lights: one additive NightGlow material, six camera-facing halos and two light-bar washes, 8 transparent cards and 16 triangles in all. Without glow_soft_256.png none of it is built.
  - The light bar crossfades at each switch (`LightBarBlink.Ramp`) and has a steady mode, `DemoLighting.ToggleLightBarFlashing()`: both halves glow at 35 % and the washes are off. A reset makes it flash again.
  - Sky and probes: city_night_sky_overcast_1k.hdr is the skybox, with the procedural sky as the fallback. A fourth baked box probe, ReflectionProbe_AcrossStreet (256 px), covers the far facades. There are no HDRI reflections.
  - Cookies: the flashlight, the UV torch and lit scene lamps use the NightSky cookies.
- **Closing procedure (NightStoreDressing).** `NightStoreDressing.StockInSafes` is true by default; false restores the October 11 stocked decks (Koda's decision 9). The left-behind pieces sit in a new Dressing/Stock group: the small pieces use their _LOD1 file as the top level, and all of them are probe-lit and cast no shadows. New shared materials: KitGem (opaque metallic), KitPearl and KitVelvetNavy. `GlassZones()` gives the glass areas the glass footsteps use.
- **Decals (NightDecals.cs, new):**
  - Always on: 17 decals in 5 lightmapped renderers with 2 materials (the Ambient atlas page and the shard scatter), as Fade quads 1.5–3 mm off their surfaces at queue 2460. They cast no shadows, are never OccluderStatic or ReflectionProbeStatic, and get their flags after MarkStatic. `NightDecals.DecalsLightmapped = true`; false makes them probe-lit.
  - Two hidden presenter sets under Dressing/PresenterSets, probe-lit and never static:
    - "Ambient wear": a water stain inside the entrance, heel scuffs, fine scratches and dust along the wall base (Koda's decision 7).
    - "Case evidence", a placeholder for Koda's decisions 2, 5 and 8: a mud shoe trail up to the forced doors, tracked dirt inside, 15 dust prints along the walk route, pry marks on the right door leaf, latent prints and a handprint on the case glass, and the crowbar, sledgehammer and flathead screwdriver (ForensicModels with their LODs, or procedural stand-ins without the pack).
- **Sound and music (NightAudioBuilder.cs, AmbientZones.cs, AmbientEvents.cs, Footsteps.cs, DemoMusic.cs, all new; DemoSoundBank, DemoSounds, AmbientSound, ToolHolder).** NightAudioBuilder replaces NightStoreBuilder.StagedSounds.
  - The sound bank prefers the NightSoundscape takes (the DSLR shutter, the torch click, the city bed as a 2D bed) and keeps NightAudio as the fallback. Cones and markers have their own set-down takes; every other tool keeps the thud.
  - AmbientZones muffles the exterior beds inside the showroom and office and plays HVACRoomTone_Loop indoors. Every bed in the zones starts at a random point. The door whistle, in neither zone list, starts at its beginning.
  - AmbientEvents schedules the car passes and the dog (DogBarkDistant_04 only). Sirens and radio are scheduled only in ProvisionalRealisticUS.
  - DemoMusic: the investigation underscore (InvestigationLoop.ogg) is off at arrival; a reset plays ResetSting.ogg and ducks the bed under it. The start menu plays MenuTheme.ogg.
- **Presenter switches (PresenterSet.cs, new; DemoDesktopPanel).** The drawer's Scene tab gains "Light bar flashing" under LIGHTING (with a note for visitors sensitive to flashing light), a SCENE DRESSING section with one switch per presenter set (Alarm bell, Ambient wear, Case evidence) and "Underscore music" under MUSIC. All are off at arrival except the light bar, which flashes. A session reset makes the light bar flash again and hides every presenter set; the underscore keeps its setting. Their toasts show on the monitor only. Docs/CONTROLS_AND_INTEGRATION.md has the table and the methods to bind.
- **Credits and the launcher (DemoUi, DemoLauncher, DemoBuild).** `DemoUi.DefaultCredits` holds the old panel text, and `DemoUi.Credits` reads Resources/DemoCredits once. The licence copy now also ships every .txt in a pack's Licenses folder (as `<pack>_<file>.txt`) and removes stale notices. On the start menu a second key press while a map loads is ignored, and Esc quits even when no map is listed.
- **Launchers (Tools/sync-project.cmd, new; Open-Unity-Glass.cmd, Start-Demo.cmd).** One helper checks for Assets and ProjectSettings, then mirrors Prototype Scripts, Editor, Shaders and ThirdParty with robocopy /MIR /XF *.meta. Start-Demo.cmd and Open-Unity-Glass.cmd call it, so UnityProject_Glass now gets the packs too (about 262 MB the first time). The historical launchers (Blockout, Forms, Interior, Robbery) stay untouched on purpose: they open frozen October 9–10 snapshots, where /MIR would delete files and reimport about 1,200 assets under the old importer. README.md has the new launch note.
- **Budget accounting (NightStoreValidation.Budgets).** Triangles, renderers and transparent renderers now count only the LOD level each LODGroup selects, by the LODGroup inspector's formula (`SelectedLod`). The eyes are the spawn, every walk-route point and the office opening, 1.6 m up, at FOV 60 and LOD bias 1, and each budget is taken at its own worst eye. Every level drawn at once and the hidden presenter sets are logged as Info. Materials and textures still count every level. MaterialBudget is raised from 96 to 136.
- **Prepared inside the packs** (offline scripts, documented in each pack's README, sources.json and LICENSE.txt; nothing imported in Unity yet). The LOD files share the original MTLs and maps; no texture was added or changed for them.

  | Pack | Model: triangles LOD0 → LOD1 / LOD2 |
  |---|---|
  | StreetModels | PoliceCar 20,840 → 5,994 / 1,928 (US preset only); StreetLamp 5,373 → 1,398 / 450; UtilityBox 3,082 → 899 / 299 |
  | PeopleModels | PasserBy_Phone 6,808 → 1,800 / 599; PoliceOfficer_Standing 12,445 → 2,999 / 899 (US preset only) |
  | ForensicModels | ForensicCase 6,614 → 1,600 / 500; Crowbar 2,300 → 600 / 200; Sledgehammer 2,596 → 650 / 199; FlatheadScrewdriver 2,300 → 600 / 200 |
  | InteriorModels | FireExtinguisher 8,005 → 2,000 / 646; WallClock 3,658 → 900 / 316 |
  | JewelryModels | GoldCoin 1,728 → 448 / 120; RingGoldBand 3,456 → 864 / 224; RingBoxClosed 1,592 → 400 / 120; JewelersLoupe 1,648 → 440 / 148; GemParcel 2,640 → 1,620 / 340; NecklacePearlDropped 8,460 → 2,098 / 530 |

  - Each pack's Source~/make_lods.py builds the levels from the original vertices and corners (a seam-aware edge collapse, or a coarser revolve grid for the round jewellery) and checks every file against its source: bounds, groups and materials. The pack READMEs have the figures and the suggested switch heights.
  - Atlases (Source~/make_atlas.py): WallGraphics/Atlas NightStoreInterior (2048 x 2048, 11 cells) and NightStoreStreet (2048 x 1024, 3 cells); SurfaceDecals/Atlas Ambient (12 cells) and Evidence (8 cells), each a 2048 colour and normal page plus a 1024 MetallicSmoothness page. They use explicit pixel rects with 16 px gutters, checked by script (no overlap, aspect within 1 %).
- **Budgets** (estimates from the OBJ, MTL and image files; replace them with the first RunNightQuick log):

  | Budget | Limit | Before (plan estimate) | After |
  |---|---|---|---|
  | Triangles, selected LOD at the worst eye | 220k | ~70k | ~148k (every level at once ~184k, logged as Info; the US preset ~228k with every level) |
  | Renderers before static batching, selected LOD | 900 | ~400 | ~530 (~577 with every level) |
  | Materials | 136 (was 96) | ~75 | ~128 (+53 from the placed models' MTLs after PackMaterial's sharing); the five hidden presenter-set materials add 5 when shown |
  | Transparent renderers, selected LOD | 70 | ~40 | ~57 (~69 with every level); the floor and street decals are queue 2460 and do not count |
  | Lightmaps | 8 x 1024 | 2–4 estimated | 2–4 estimated; the ApartmentBlock adds about 0.05 of one map |
  | Pixel lights | 4 | 4 | 4: no realtime light added; the halos and washes are additive cards |
  | Texture memory referenced | — | — | ~155 MB at the capped import sizes (all packs at native size would be ~460 MB) |

  Audio at arrival (default PoliceLook, all packs): 5 looping beds (the city bed, the wind, the car idle, the room tone and the door whistle), one AmbientEvents scheduler with 2 one-shot sources (the car passes and the dog), 2 reverb zones and 1 footstep source. The alarm bell and the underscore are off. The US preset adds the radio loop and the siren and radio sources.
- **One-time reimport:**
  - First open after the pull and sync: PackImport is a new texture and audio postprocessor, so Unity reimports every texture and audio clip in the project once, not only the packs. Expect several minutes.
  - First RunNightQuick or build: every pack model reimports once (props lose uv2; the buildings and the rug get their chart margins), and so does any pack texture or clip that does not match the rules. RunNightQuick and RunNight log the total as the Info line "Third-party assets reimported to the import rules (models, then pack textures and audio): N" (it used to read "Third-party models reimported with lightmap UVs"). Build (quick) and DemoBuild reimport without a log line. A second run should log no such line.
- **New checks** (in the validator files now; the counts assume all twelve packs are mirrored, and a check whose pack is missing logs an Info line instead):
  - **NightStoreValidation, Edit mode** (RunNightQuick and RunNight):
    - Asset library (AssetLibraryChecks, before the build):
      - "Pack placement shares one material per pack, colour map, tint and role across instances and LOD levels"
      - "LOD groups use the prep variants with the class transitions"
      - "Shared colour maps with different tints or roles stay separate"
      - "Transparency comes from the MTL alpha, finishes from the pack data"
      - "The OBJ import carries the MTL d as colour alpha (a glass material without a finish entry)"
      - "Pack material convention wires Normal and MetallicSmoothness by the colour map stem"
      - "Cut-out colour maps alpha-test at the import rules' cutoff"
      - "Missing pack models return null"
      - "Atlas cells load as UV rects inside [0,1] (SurfaceDecals/Atlas/Ambient.json)"
      - "Atlas cells load as UV rects inside [0,1] (WallGraphics/Atlas/NightStoreInterior.json)"
      - "Fade and Cutout helpers set the Standard blend state"
      - "ProceduralMesh.Quad maps the UV rect to its corners, faces outward and skips degenerate quads"
      - "Seat rests a prop's lowest renderer point on the surface"
      - "Placing an attributable model records its credit"
    - Import (PackImportChecks, after the static checks):
      - "Pack textures follow the folder and suffix import rules"
      - "PackImport.Apply leaves nothing to reimport on a second run"
      - "Import rules leave the older packs alone"
      - "Prop packs import without lightmap UVs; buildings, the rug and KayKit keep them with their planned chart margins"
      - "Pack audio imports: loops compressed in memory, one-shots PCM, music streaming"
    - Police (PoliceLookChecks, after the lighting pass): "Police choices come from the PoliceLook setting, Provisional by default". It checks the KayKit car's light-bar halves and lenses, 1.5 Hz, the CRIME SCENE tape, no siren or radio events, no radio loop and no police figure.
    - Budgets: "Budgets count the LOD level selected from the worst eye". It also tests `SelectedLod` at the group's own centre (LOD0) and 10 km above it (culled).
    - StoreKit palette (StoreKitValidation.Check, which Generate calls): "Palette sets come from one pack each, in the documented priority", "Palette tiling follows each set's real-world tile size" and "Brick, shutter and stainless use their PBR sets and keep a scalar fallback".
    - Renamed, so the count is unchanged: "Night materials use packed metallic-smoothness maps" is now "Night palette maps all come from the selected set", and "Night ceiling and facade use the staged plaster and marble" is now "Ceiling and facade use the selected sets". The old checks would fail with the new StoreKit.
    - Kept under their old names: "Staged floor decals stay unplaced" still passes, because it looks for the NightGraphics ShoeTrace, Scuff and DustSmear textures, and the SurfaceDecals pages are named Ambient_Color, Evidence_Color and GlassShards_Scatter. "Kit models beside the spawn bake at their import margin density" now covers the KayKit car (and the KayKit lamps when they are the fallback); the lanterns are probe-lit.
    - "Staged sounds replace generated clips" now also counts the NightSoundscape takes as staged audio, so a project with NightSoundscape but without NightAudio still passes.
  - **DemoValidation.InterfaceChecks** (Play mode; ShopCityValidation and NightStoreValidation): "Panel credits come from DemoUi.Credits" and "Credits fall back to the built-in line without a credits file".
  - **ShopCityValidation:** "Shop tape keeps the CRIME SCENE text" (glass runs only); "Shop torches keep the generated cookie" and "Shop lamps have no cookie" (HandChecks); "Without a sound bank, set-downs keep the generated thud" and "Shop ambience keeps spatial blend 0.6" (BackdropChecks).
- **Expected PASS-count changes:**

  | Validator | Change | Made up of |
  |---|---|---|
  | NightStoreValidation.RunNightQuick | +26 | 14 asset library, 5 import, 1 police, 1 LOD budget, 3 StoreKit palette, 2 InterfaceChecks; the 2 renamed checks change nothing |
  | NightStoreValidation.RunNight | +26 | the same checks |
  | Either, in a project unpacked from the ZIP without a sync | +15 | the 11 checks that need a new pack log Info instead |
  | StoreKitValidation.RunGallery | +3 | the three palette checks |
  | ShopCityValidation.RunGlass, RunTextured | +7 | 2 InterfaceChecks, 2 cookies, 2 sound, 1 tape text |
  | ShopCityValidation.Run, RunExpanded, RunForms, RunInterior, RunRobbery | +6 | the same without the tape-text check |
  | JewelrySceneValidation, GlassToolValidation | none | no new checks |

- **Behaviours with no check yet.** CLAUDE.md asks for a check for every new behaviour; these still have none and are the next validator batch: the street (lanterns and lamp profile, the ApartmentBlock facing the shop, the facade sets, the towers gone, figures at least 2.5 m outside the play area, furniture on its surface and clear of the walk route, colliders, the bell box); the lighting (halos and washes without lights, the panorama sky, the AcrossStreet probe, the NightSky cookies, the crossfade and steady modes); the interior (snake plants and rug with their fallbacks, the desk props clear of the tray, the monitor glow without a light, the partition fittings, the ceiling diffusers, wall-graphic size, facing and material count); the closing procedure (display-only cases, stock inside the safes, floor pieces clear of the route and the UV traces, glass zones); the decals (flags, cells, the trail, "No print-like or footwear-like mark is visible at arrival"); the soundscape, footsteps, set-downs, reverb, alarm bell and music; the presenter switches and their reset; the credits and the packs' licence notices; and the launcher mirror (Tools/sync-project.cmd's header still names a planned "all twelve packs are present" check that was not written).
- **Existing checks to watch on the first run,** because the new content touches what they measure: "Night store material budget" (about 128 against 136), "Night store transparency budget" (about 57 of 70), "Night dressing is combined by material" (the new props sit in its batched zones), "Night store static flags follow the bake policy" (pack props without uv2 must be probe-lit), "Light lenses are captured by the baked reflection probes" (exactly one Lens per lantern) and, after the bake, "No lightmapped renderer outgrows one lightmap".
- **Deferred** (each with its reason in ROADMAP):
  - GlamVelvetSofa: untextured (it reads black), 2.19 m wide with 1 cm to the side table, and CC BY. The StoreKit sofa stays.
  - FireAlarmCallPoint: a US pull station, which would mix countries in a store that prices in pounds; it waits for the country (decision 10).
  - Gem shader: few gems show under the closing procedure, and a stereo shader needs a headset check. KitGem stands in.
  - HDRI reflections: the photographic HDRIs are identifiable, hold lamp pixels of 12,800–54,000 and would leave a seam at x 14.
  - Wet-street streak quads: wrong stereo depth, and they do not move with the head; the probes already reflect the lenses.
  - GlassSmudge and ambient wear by default: they read as evidence (decision 7).
  - Country presets: decision 1; a decided service becomes one more PoliceLook value.
  - Laminated glass: waits on the entry decision (decision 2).
  - The fixture-detail generator: a large procedural StoreKit pass of its own.
  - Sheen, specular anti-aliasing, torch beams, people's hands: custom shaders, particles and rigs, each needing headset checks. The metals are clamped meanwhile.
  - Pack torch models: the runtime torches work in both modes; pack meshes need prefab references and hand-pose checks.
  - Security hardware, procedure props, trim and trees: separate batches (only the bell box is added now).
  - Other vehicles and buildings (Hatchback, SedanClassic, the Downtown buildings): CC BY, they cost materials and sit behind the arrival view.
  - Country-specific street furniture: signs, hydrant, name plates and the LED light bar depend on the country.
  - ApartmentBlock LODs: it is at LOD0 from 14 m or more, and decimation would damage its flat facade.
  - Dog barks 01–03: their CC BY 3.0 evidence conflicts; only the CC0 _04 is scheduled.
  - The alarm bell by default: a scenario choice (decision 6), and continuous ringing tires a session.
  - Glass tinkle events: random settling sounds could startle trainees.
  - Bloom and flares: they need post-processing, and the scene renders LDR without packages.
  - Stock extras (price tags, stands, posters, vinyl, 100 g bars): they conflict with the closing procedure and cost materials.
  - Pre-placed barriers and markers: placing them is the trainee's task.
- **Open decision: the country and the police service** (Koda's decision 1 in for Koda.md). Nothing in the source picks a country: the default `PoliceLook.Provisional` is today's look plus the generic V8 idle, and the US car, sirens, radio and officer exist only behind `ProvisionalRealisticUS` for review. Once Koda decides, the service becomes one more `PoliceLook.Service` value with its own car, livery, tape text, siren and radio. The fire alarm call point, the street signs, the hydrant, the name plates and the LED light bar wait for the same decision.
- **Known issues to judge on the PC:**
  - **Asset assumptions.** The glass rule assumes Unity's OBJ import puts the MTL d into the colour alpha. The Info line "Imported MTL alpha of GemParcel Diamond (d 0.35)" should read about 0.35; if not, only the glass in the finish table turns transparent. Finish lookups also assume Unity names OBJ materials after their newmtl lines.
  - **Import choices beyond the plan** (low risk, to confirm): GlassCrackDecal_ColorAlpha.png and moon_disc_512.png are also imported as blended (their pack READMEs say Fade); moon.jpg gets 1024 and Repeat; packs without AO maps would size AO like their MS maps; the model reimport test also compares the import flags, normals and object scale.
  - **Lights and glow.** The streetlight cookie's centre texel is 0.61, so the ground straight under each lamp is about 0.6 times as bright as before; raise `Lantern.BakedIntensity` (3.5) in NightStoreLayout if the street reads too dark. The flashlight keeps its 42° spot, although the NightSky README suggests 55–65° for its new cookie. The shop-floor wash may read as a hotspot.
  - **Surfaces.** The PBRInterior floor is much glossier (mean smoothness 0.88 against 0.55). Velvet and black leather may read very dark under the torch. FacadeStone with `Calibrated = true` would be about 20 % brighter.
  - **Pack geometry.** The notice board's A4 body text is not readable at its atlas size. Bollard and BusShelter make two identical Signs materials (+1). The clock LODs have gaps seen from behind. The PoliceCar LOD2 has coarse wheels and bumper, softer hard edges and possible small texture smears, acceptable only below its 10 % switch. The officer's LOD2 has texture smears. The hidden tools' handle ends float 1–2 cm. In the US preset, the radio loop and the radio one-shots can overlap.
  - **Final textures.** A final texture dropped into ReferenceTextures under an existing name now comes second to the PBRInterior/PBRExterior set of that name. To use it, put its set first in `StoreKit.Sets`.
  - **Credits in the old shop.** DemoCredits lists only what the night store placed. JewelryStoreGlass, in the same player, places Khronos CC BY models whose lines its Settings tab therefore lacks; KhronosSamples.txt in ThirdPartyLicenses still carries them. Merging the credits needs a ShopCity or NightStoreBuilder change.
  - **Pack tools.** PeopleModels' older Source~/validate.py and write_sources.py stop on the _LOD files (that pack's README gives the workaround).
- **Files changed or added** (the Downloads ZIP is synced: 59.0 MB, the 12 new .cs files added with fresh .meta guids, 26 changed files updated, the twelve packs left out):
  - New, each now in Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip with a fresh .meta guid:
    - Editor: PackImport.cs, PoliceLook.cs, NightStreet.cs, NightInterior.cs, NightDecals.cs, NightAudioBuilder.cs.
    - Scripts: PresenterSet.cs, GlowBillboard.cs, AmbientZones.cs, AmbientEvents.cs, Footsteps.cs, DemoMusic.cs.
    - All twelve must land together: NightStoreBuilder calls NightInterior, NightDecals and NightAudioBuilder, and DemoDesktopPanel needs PresenterSet and DemoMusic.
  - Changed, updated in the ZIP with their existing .meta files:
    - Editor: DemoAssetLibrary, DemoBuild, DemoGeometry, DemoToolsBuilder, DemoValidation, NightLighting, NightStoreBuilder, NightStoreDressing, NightStoreLayout, NightStoreValidation, ShopCityValidation, StoreKit, StoreKitProps, StoreKitValidation, ThirdPartyModelImport.
    - Scripts: AmbientSound, DemoDesktopPanel, DemoLauncher, DemoLighting, DemoSoundBank, DemoSounds, DemoUi, LightBarBlink, ProceduralMesh, ToolHolder, ToolStation.
    - No shader changed.
  - Packs (they stay out of the ZIP for size; both launchers mirror them):
    - New LOD files: StreetModels PoliceCar, StreetLamp and UtilityBox _LOD1/_LOD2; PeopleModels PasserBy_Phone and PoliceOfficer_Standing _LOD1/_LOD2; ForensicModels ForensicCase, Crowbar, Sledgehammer and FlatheadScrewdriver _LOD1/_LOD2; InteriorModels FireExtinguisher and WallClock _LOD1/_LOD2; JewelryModels GoldCoin, RingGoldBand, RingBoxClosed, JewelersLoupe, GemParcel and NecklacePearlDropped _LOD1/_LOD2.
    - New atlas folders: WallGraphics/Atlas and SurfaceDecals/Atlas.
    - New Source~ scripts and previews: make_lods.py in StreetModels, PeopleModels, ForensicModels, InteriorModels and JewelryModels; StreetModels lod_sources.py and Preview_LODs.jpg; JewelryModels render_lods.py and Preview_LODs.jpg; InteriorModels LOD_Previews.jpg; make_atlas.py in WallGraphics and SurfaceDecals.
    - Updated README.md, LICENSE.txt and sources.json in StreetModels, PeopleModels, ForensicModels, InteriorModels, JewelryModels, WallGraphics and SurfaceDecals; models.json in InteriorModels and JewelryModels; StreetModels Source~/check_pack.py and README_build.txt.
  - Outside the ZIP: Tools/sync-project.cmd (new), Open-Unity-Glass.cmd, Start-Demo.cmd, README.md, for Koda.md, and Docs/HANDOFF.md, ROADMAP.md, ASSET_REGISTER.md and CONTROLS_AND_INTEGRATION.md.
- **To verify on the PC:**
  1. **First compile and NightStoreValidation.RunNightQuick.** Open the project through Open-Unity-Glass.cmd or Start-Demo.cmd, so the packs are mirrored, and note how long the one-time texture and audio reimport takes. The Console must show no compile errors or warnings from the new files. Expect +26 PASS lines (+15 in a project unpacked from the ZIP without a sync). Record these Info lines here:
     - the "Third-party assets reimported to the import rules ..." count (and that a second RunNightQuick logs none and passes "PackImport.Apply leaves nothing to reimport on a second run");
     - "Imported MTL alpha of GemParcel Diamond (d 0.35)", which should read about 0.35;
     - "Police look: Provisional (today's look); car placed car_police";
     - the "Budget:" lines: triangles, renderers and transparent renderers at their worst eyes, every level at once, and the hidden presenter sets.
  2. **MaterialBudget from the log.** Set NightStoreValidation.MaterialBudget to the logged "Budget: materials=" value + 8, and record the value here.
  3. **Import spot checks in the Inspector:** PBRInterior/FloorTile_Normal.png (Normal map, 2048); NightSky/cookie_streetlight_512.png (Cookie, Clamp, Border Mip Maps); NightSky/city_night_sky_overcast_1k.hdr (2D, no mipmaps, U Repeat / V Clamp, 1024); WallGraphics/Store/Sheet_StaffRota.jpg (Non-Power of 2 None, 724 x 1024, Clamp); StreetModels/DT_MaskedDetails_Color.png (Alpha Is Transparency, Mip Maps Preserve Coverage 0.5); NightSoundscape/NightCity_Loop.ogg (Compressed In Memory, Vorbis 60, Load In Background); Music/MenuTheme.ogg (Streaming, Vorbis 70). StreetModels/PoliceCar.obj and StreetLamp.obj have no UV2; ApartmentBlock.obj and InteriorModels/Rug.obj show Generate Lightmap UVs with Margin Method Calculate at 6 and 16; KayKitCity/car_police.obj still has UV2; the models without normals (ApartmentBlock, BusShelter, Crowbar, EarringGoldLeaf and the rest) look smoothly shaded, not faceted black.
  4. **NightStoreValidation.RunNight,** the first bake of the wired store. Record the bake seconds and the "Lightmaps=" line here (estimate 2–4 maps, budget 8 x 1024). Then check:
     - the decals: look for dark rectangles under the puddles, the shard scatter and the grime; if they show, set `NightDecals.DecalsLightmapped = false` (probe-lit) and rebuild;
     - the lantern pools: they should fall through the cookie with the sharp edge towards the facades on each side;
     - the ApartmentBlock: no chart bleeding in the Baked Lightmap / UV Charts view (6 texels/m margins at StreetScale).
  5. **StoreKitValidation.RunGallery.** Expect +3. The Info line "Palette materials with a texture set" and the tile spot checks ("5 of 5" with the packs) should name the winning pack per material, for example KitFloorTile from PBRInterior/FloorTile.
  6. **ShopCityValidation.RunGlass.** Expect +7 (+6 in the other ShopCity runs), with the old shop otherwise unchanged in look and sound.
  7. **Open-Unity-Glass mirroring.** Double-click Open-Unity-Glass.cmd. It must copy Scripts, Editor, Shaders and ThirdParty into UnityProject_Glass (about 262 MB of packs the first time) and then open Unity, and the JewelryStoreGlass scene must still open. A missing Assets folder must stop the launcher with the message to extract the Glass zip.
  8. **Headset (Quest 2 over Link), on a baked build:**
     - the light bar's crossfade and the red and blue washes, judged for comfort; the "Light bar flashing" switch must hold the bar steady with no washes, and a reset must bring the flashing back. If it is too strong, lower the alpha of WashRed and WashBlue (0.06) or raise `LightBarBlink.Ramp`;
     - the halos facing the viewer in both eyes without cutting the lamp housings, no z-fighting on the decals at grazing angles, no shimmer on the KitGem diamonds, and no visible LOD pops on the lanterns, the utility box and the passer-by;
     - no footsteps from room-scale walking, teleports or snap turns;
     - the frame time, judged only on a baked build.
  9. **Desktop (mouse and keyboard):** the drawer's Scene tab shows "Light bar flashing", the three SCENE DRESSING switches and "Underscore music", each working and each reset as described in CONTROLS_AND_INTEGRATION.md; footsteps every 0.7 m and glass steps in the glass zones; the menu theme fading before a map loads, a second key press ignored while it loads, and Esc quitting from an empty menu; the credits in the Settings tab and their first line on the start menu.
  10. **Audio levels, on speakers and in the headset:** the city bed (0.45), the wind (0.35) and the V8 idle (0.5 at the bonnet) outside; the muffled street and the room tone inside; the door whistle, the cone and marker set-downs, car passes and the dog; the Room reverb, which may be too wet; no clicks where the loops wrap after the Vorbis re-encode; the menu theme at 0.25 (its README suggests 0.5), the underscore switch and the reset sting.
  11. **Visual walk on a baked build, desktop and headset:**
      - street: the lanterns upright with lit glass, the ApartmentBlock facing the shop (not mirrored, with lit rooms), the brick, render and shutter facades, the bus shelter's lightbox;
      - interior: the snake plants, the rug, the monitor glow, the extinguisher and clock with their LOD switches, the diffusers flush with the ceiling, the gilt frames not mirrored, the stickers readable from the pavement;
      - office: the safes' left-behind stock with nothing floating, the loupe and the tray on the desk;
      - presenter sets: Ambient wear and Case evidence each shown, then hidden again by a reset.
  12. **Optional US review.** Set `PoliceLook.Current = Service.ProvisionalRealisticUS`, then use Build (quick). Check that the car's nose faces the entrance, the lenses sit on the lamps, the bar halves enclose the roof glass and the officer stands at (12.5, -1.6) facing the shop. "Police choices come from the PoliceLook setting, Provisional by default" fails while it is selected. Set it back afterwards.

# Realism asset packs — October 15 (Claude, unverified, not yet used in any scene)

Twelve new packs under Prototype/ThirdParty/, about 262 MB in total. Each has a LICENSE.txt, a README.md, a per-file sources.json (source URL, author, license, sha256), and its build scripts in Source~ (Unity ignores that folder). The asset-sourcing workflow fetched or generated them; an independent reviewer then checked each pack's licences, files, format, budget and quality, and a repair agent fixed what it flagged. Docs/ASSET_REGISTER.md has 95 new rows under "Realism packs (October 14–15)".

| Pack | Size | Contents |
|---|---|---|
| PBRInterior | 37 MB | ambientCG CC0 materials (polished marble floor, stone tops, walnut, terrazzo, leather, carpet, velvet, suede, brushed metals, chrome, brass, glass smudge, wallpaper) plus procedural sets; Color .jpg, Normal (OpenGL), MetallicSmoothness RGBA .png |
| PBRExterior | 29 MB | Poly Haven/ambientCG asphalt (dry and wet), pavement, facade stone, brick, render, metal shutter and more |
| NightSky | 18 MB | Poly Haven night HDRIs (cobblestone street, Zwinger, Hansaplatz) plus procedural city_night_sky skyboxes; light cookies and flare sprites |
| JewelryModels | 20 MB | 47 OBJ models (rings, earrings, necklaces and bust, bracelets, watches and pillow, loose gems, ring cone, ring boxes, price tags, gift bag, gold bars, loupe, payment terminal), project-made |
| ForensicModels | 19 MB | Poly Haven crowbar, bolt cutters, gloves, tripod and others, plus procedural evidence items |
| StreetModels | 24 MB | Poly Haven street lamp, aircon unit and street furniture; CC BY cars, police car and van; procedural traffic light, signs, bike rack, bus shelter, drain, bollards, light bar |
| InteriorModels | 27 MB | 48 Poly Haven furniture and decor models (chairs, tables, plants, vases, frames, clock, lamps, desk) |
| WallGraphics | 18 MB | 9 public-domain paintings, project-made jewellery posters, store notices and street graphics (fictional names, checked against real businesses) |
| SurfaceDecals | 15 MB | project-made shoe prints, fingerprints, glass handprint, shards, cracks, puddles, grime, light cookies |
| NightSoundscape | 31 MB | 86 clips: night beds, alarm bell, interior machines, police car idle/radio/doors, sirens, cars, dogs, footsteps, glass, tool foley (CC0 recordings via GitHub mirrors plus synthesis; dog barks 01–03 are CC BY 3.0) |
| Music | 4.6 MB | John Bartmann CC0 ambient beds (menu theme, two investigation loops) and an original reset sting |
| PeopleModels | 16 MB | 5 Microsoft Rocketbox avatars (MIT), posed and baked to static OBJ: police officers, forensic examiner, detective, bystander |

- **Not in the Downloads ZIP** (size): Start-Demo.cmd mirrors Prototype/ThirdParty into UnityProject_Demo, so the one-click build sees them. Projects unpacked by the Open-Unity-*.cmd launchers do not, until the wiring batch handles that. Unity creates the .meta files on import. (October 15 wiring batch, unverified: Open-Unity-Glass.cmd now mirrors them too, through Tools/sync-project.cmd; the historical launchers do not.)
- **Nothing uses them yet.** The next batch wires them into the night store with the existing primitive fallbacks. Known issues to handle there: PBRInterior and PBRExterior reuse names from NightMaterials and ReferenceTextures, and their colour maps are .jpg where StoreKit looks for _Color.png; the packs add about 651k triangles against the 220k scene budget, so props need LODs, decimation or selective use; WallGraphics has 27 non-power-of-two images; there is no texture import postprocessor yet. (October 15 wiring batch, unverified: done. See the entry above for what is used, PackImport.cs, the StoreKit set priority and the LOD variants.)
- **Decision for Koda:** the police details are mixed (US-style sedan livery and siren, UK-style store). The "country" choice in "for Koda.md" decides which livery, tape text and siren to use. (October 15 wiring batch: the choice now sits behind `PoliceLook.Current`; the default keeps today's look.)
- **To verify on the PC:** the first Start-Demo.cmd after pulling imports about 1,200 new files (several minutes longer); check the console for import errors.

# Performance and realism fixes from the audit — October 15 (Claude, unverified, never compiled)

A read-only audit of six areas (rendering, VR frame, CPU/GC, build/import, physics/logic, correctness) produced 57 findings. Two skeptics checked each one: 46 were confirmed and 11 rejected. This batch fixes the confirmed findings in the source, except the ones listed under "Deferred or dropped" (two deferred, four only partly done). It also adds 30 validator checks. Each change was reviewed by an adversarial reader and the whole diff was cross-reviewed; a C# parser found no syntax errors in the 30 changed or new .cs files. The aim is more realism at the same or lower cost, on Quest 2 and with mouse and keyboard, with both modes still working. **Nothing here has been compiled or run in Unity.**

- **Rendering and lighting (night store):**
  - Twice the lightmap density: DemoGeometry.LightmapTexelsPerMetre is now 20 (was 10), which gives 5 cm texels on the interior shell, where the headlights' case shadows fall. The per-zone scales in NightLighting are: shell 1; fixtures and facade 0.8; near pavement and kerbs 0.75 (new GroundScale; a 64 m strip needs 960 px, under the 1024 px maximum); KayKit car, streetlights, table, easel and bollards 0.5; street and the neighbouring terrace 0.3 (StreetScale, was 0.6); far pavement, the row across the street and the skyline 0.1 (FarScale, was 0.2). The Neighbours rule now runs before the Buildings rule. The pavement and kerb KitMeshes use GroundScale, so their chart margins match. NightStoreBuilder's own scaleInLightmap loop is gone, because NightLighting.LightmapScales sets every zone after Build.
  - Directional lightmaps: lightmaps are CombinedDirectional (were NonDirectional), so the normal maps on the plaster, paving and facade stone now respond to the baked light. This costs one extra lightmap sample per pixel and about 1.3 MB per 1024 px map.
  - Lightmapped street backdrop: the row of buildings across the street is now a lightmapped KitMesh at FarScale, with uv2. Before, it was one probe-lit renderer 54 m wide, lit evenly. The far streetlights' pools now reach its shopfronts. The KayKit skyline towers bake at 14 texels per model unit (FarScale 0.1 x 20 x scale 7), which is acceptable at that distance.
  - Lenses in the reflections: the streetlight and headlamp lenses are now ReflectionProbeStatic (NightLighting.ProbeLens). They do not contribute GI, so the lightmaps and the GI policy are unchanged. The showroom probe now carries headlamp glints for the case glass, the jewellery and the floor. The street probe carries the streetlight glow on the wet asphalt. The blinking light bar stays dynamic.
  - Sharper probes: reflection probes bake at 256 px (NightLighting.ReflectionResolution, was 128), because the 0.95-smooth glass samples about mip 0.5. The night sky's default reflection stays at 128 px (new SkyReflectionResolution).
  - Lit police tape: Shaders/CrimeSceneTape.shader is now a Standard surface shader (`addshadow fullforwardshadows`, no lightmap, meta or deferred variants, DisableBatching so the object-space sag survives). The tape is dark until a torch or lamp reaches it, is shadowed by the ForwardAdd spot lights, and casts its sagging shadow. Unchanged: the shader name, properties, _Span, stripes and sag. It has Metallic 0, Smoothness 0.3 and no emission. PoliceTape.mat in the daytime shop is now shaded too.
  - Readable overview map: the presenter's overview map renders in flat albedo, darker with height, through the new replacement shader Shaders/MapAlbedo.shader (Hidden/CrimeScene/MapAlbedo). Before, the night store came out almost black. DemoToolsBuilder assigns the shader to DemoOverviewMap.MapShader, which also keeps it in the build. Without the shader the map renders lit and a warning is logged.
  - Better evidence photos: the photo camera renders LDR with MSAA. The capture target takes QualitySettings.antiAliasing samples and is resolved before ReadPixels. A half-size copy with GPU-built mipmaps (EvidenceCamera.LastPhotoDisplay) feeds the wall frame, the DSLR rear screen and the panel thumbnails, so they no longer shimmer. LastPhoto stays the full 1920-wide photo.
- **VR frame:**
  - Pixel lights in the preview: the unbaked preview is RunNightQuick, or a build whose bake was skipped or failed. In it, the headlights are now Auto instead of ForcePixel. The flashlight, the UV torch (DemoLighting.Create) and the deployed lamps (DemoToolGeometry.Lamp) are ForcePixel, so they always keep one of the four pixel-light slots.
  - Viewfinder shadows: the 15 Hz viewfinder render skips the shadow map while the flashlight is on. QualitySettings.shadows is switched off for that render only and restored in a finally block. A lit lamp keeps its shadow, and saved photos keep every shadow.
  - Overview map cost: the map renders only while the drawer's Map tab is open. Before, it rendered on every tab whenever the drawer was open. It now renders once a second (was four times), LDR and without MSAA, and renders at once when the tab is reopened.
  - Faded controller hints stop rendering. That was about 55 transparent renderers every frame. ShowHints turns them back on at once, and their leader lines live in the tip's own space.
  - Pose timing: XRHeadTracking (execution order -110) and XRControllerInput (-100) read the pose again in Application.onBeforeRender (LatePose), and a held tool follows that late hand pose. HandInteractor (default order) therefore always places the held tool and the grab highlight from this frame's pose.
  - The start-up recentre now blinks through the comfort fade like a teleport, and the frame of any jump renders fully black.
  - Headset interface on its own layer (DemoUi.HeadsetUiLayer = 5, Unity's unused "UI" layer): controller models, hints, wrist card, toast, carousel, teleport arc and ring, and the comfort fade. The head and desktop cameras draw it. The photo camera and the overview map cull it, so evidence photos, the viewfinder and the map no longer show it. The hands themselves stay on Default, because held tools, the camera body and the torches ride on them.
- **CPU and GC (desktop HUD, photos):**
  - DemoUi: the texture and style caches are keyed by value (tuples with Color, an icon array) instead of strings built on every IMGUI event. Box draws on Repaint only. DemoUi.Content keeps one GUIContent per distinct string (at most 512). Key hints are split once, and the slider track is made once.
  - DemoDesktopPanel:
    - Allocations: hint arrays and labels are static. Count text is rebuilt only when a count changes. The strip's button actions are made once, and closures are created only inside click branches. The prompt fills one reused list; the public ContextPrompt() still returns an array.
    - Hierarchy walks: one GetComponentsInChildren per ToolsChanged now feeds the drawer rows, the map glyphs and the kind chips. Before, there was one walk per IMGUI event.
    - Layout pass: useGUILayout is on only while the drawer or the controls overlay is open, because only they use GUILayout, so the HUD skips the extra Layout event.
  - DemoLauncher has static hint arrays and useGUILayout off. DeployedTool caches Title per kind and a marker's DisplayName.
  - Photo capture: the needless Apply (an 8 MB upload) is gone, and one pixel buffer is reused instead of a fresh 6 MB array per photo. The GPU readback itself is still synchronous (see Deferred).
- **Build and import:**
  - Failed bake: a failed bake no longer ships baked-only lights without lightmaps, which render black. NightStoreBuilder.BuildScene now returns whether it baked. On a failed bake it clears the lightmaps, falls back to the preview and logs the warning `JEWELRY_BUILD_LIGHTING bake failed`. build-latest.log always carries `JEWELRY_BUILD_LIGHTING baked` or `JEWELRY_BUILD_LIGHTING preview`. DemoBuild.WriteBuildInfo(bake, baked) writes the real lighting mode to the start-menu footer: "lighting baked", "preview (run Start-Demo.cmd bake for baked lighting)" or "preview (the bake failed, see build-latest.log)".
  - Sticky bake choice: `Start-Demo.cmd bake` writes Builds\Windows\lighting.txt containing "bake". After that, a plain double-click that rebuilds after a pull prints "Baked lighting: ..." and bakes again. `Start-Demo.cmd rebuild` builds a preview and deletes the file. README line 5 now says this and adds: judge frame rate only on a baked build. Baking is still not the default (see Deferred).
  - Linear before the first import: right after the first unpack, Start-Demo.cmd patches UnityProject_Demo's ProjectSettings.asset (`m_ActiveColorSpace: 1`) and GraphicsSettings.asset (`m_LightsUseLinearIntensity: 1`) with a PowerShell text replace, before Unity opens the project. The first build therefore imports every texture once instead of twice. UnityProject_Glass and Build-Windows.cmd keep the zip's Gamma.
  - New Editor/ForwardOnlyShaderStripping.cs (IPreprocessShaders): player builds drop every shader's DEFERRED pass, since every camera renders Forward. It switches itself off if a Standalone graphics tier is ever set to Deferred. It saves build time and player size only; frame time does not change. Editor rendering and the bake's Meta pass are untouched.
- **Physics and logic:**
  - Overhead releases: ToolHolder.Settle drops a tool released overhead to the floor of the region under it (the street in the staging bay). Before, the tool floated or landed on the ceiling slab. The drop cast starts at most DropCeilingClearance (2.7 m) above that floor, which is below the 3.0 m ceilings and above the 2.1 m cases, and a miss lands on the floor.
  - Grab scan: HandInteractor's grab scan ignores triggers and doubles its overlap buffer whenever 32 colliders fill it, so a pile of tools no longer hides the nearest one.
- **Correctness:**
  - DemoUi searches for the theme again at once when the cached one was destroyed (a scene load or F10), so world text is no longer built in the fallback font for up to a second.
  - F2 (hide the HUD) lasts for one map load. It is no longer saved, and the DemoHudVisible key that older builds wrote is deleted. Docs/CONTROLS_AND_INTEGRATION.md is updated.
  - The headset toast skips presenter-only messages: interface size, "HUD on" and settings restored. These use the new desktopOnly flag on DemoFeedback.Post and DemoFeedback.TryNewestForHeadset. The monitor still shows them.
  - DontSave runtime objects are freed: the status board destroys its panel material, and the overview map destroys its render texture.
- **New checks:**
  - **NightStoreValidation, Edit mode** (LightmapChecks, after "Night store lightmap budget", in both RunNightQuick and RunNight):
    - "Night lightmaps are directional (normal maps respond to baked light)"
    - "Lightmap density by zone"
    - "No lightmapped renderer outgrows one lightmap"
    - "Kit models beside the spawn bake at their import margin density"
    - "Light lenses are captured by the baked reflection probes"
    - "Buildings across the street are lightmapped at the far density"
    - "Glass reflection probes bake at 256 px, the night sky at 128 px"
  - **NightStoreValidation, Play mode:**
    - "Forward-only player builds strip the deferred shader pass"
    - "Handheld lights keep a per-pixel slot"
    - "VR recentre blinks through the comfort fade"
    - "Police tape is lit by the torches and lamps and casts shadows"
    - "Tool released overhead in the staging bay drops to the street"
    - "Overview renders only on the drawer's Map tab"
    - "Overview map draws albedo through its replacement shader"
  - **DemoValidation.InterfaceChecks** (run by ShopCityValidation and NightStoreValidation):
    - "UI fonts re-resolve at once after the theme object is replaced"
    - "Headset toast skips presenter-only messages; the monitor still shows them"
    - "Faded controller hints stop rendering"
    - "Before-render pose refresh leaves the controllers alone without a headset"
    - "Headset interface stays out of evidence photos, the viewfinder and the map"
    - "Head and controller poses are applied before HandInteractor places the held tool"
  - **DemoValidation.PhotoStage:**
    - "Status board frees its panel material when destroyed". This one runs only after InterfaceChecks, so JewelrySceneValidation skips it.
    - "Photo frame samples a mipmapped copy of the photograph"
    - "Saved photograph matches the in-memory photo"
    - "Photo camera renders LDR with anti-aliasing"
  - **ShopCityValidation:**
    - "Desktop HUD starts visible each run and F2 is not remembered"
    - "Viewfinder skips the flashlight shadow map and restores the quality settings"
    - "Tool released overhead drops to the floor, not onto the roof"
    - "Grab finds the nearest tool beside a pile of 36 colliders"
    - "Overview map draws albedo through its replacement shader"
  - **ShopRobberyDressing.Check:**
    - "Overview skips rendering on other drawer tabs". The existing "Overview rendering resumes when menu opens" now selects the Map tab first, so it keeps passing under the new Map-tab rule.
- **Expected PASS-count changes:**

  | Validator | Change | Made up of |
  |---|---|---|
  | NightStoreValidation.RunNightQuick | +24 | 14 night, 6 InterfaceChecks, 4 PhotoStage |
  | NightStoreValidation.RunNight | +24 | same as RunNightQuick |
  | ShopCityValidation.RunGlass | +16 | 5 shop, 1 robbery dressing, 6 InterfaceChecks, 4 PhotoStage |
  | ShopCityValidation.RunRobbery, RunTextured | +16 | same as RunGlass |
  | ShopCityValidation.Run, RunExpanded, RunForms, RunInterior | +15 | RunGlass without the robbery dressing |
  | JewelrySceneValidation | +3 | PhotoStage without the status-board check |
  | StoreKitValidation.RunGallery | none | no new checks |

  Both "Overview map draws albedo..." checks need MapAlbedo.shader in the project. NightStoreValidation needs ForwardOnlyShaderStripping.cs to compile.
- **Deferred or dropped:**
  - **Baking by default (BI-01), deferred.** This is a product decision that needs a measurement on the PC first. If it were the default, a double-click would run a bake that has never run (no RunNight result exists). It would do so on every pull, including docs-only commits from either assistant, and add unmeasured minutes each time (GPU lightmapper; far longer on the CPU fallback). The safety half ships now: the preview fallback on a failed bake, the real lighting mode on the start menu, and a bake choice that persists across pulls.
  - **Occlusion culling (R12, BI-10), deferred until a headset profile exists.** Two of the four skeptics found the gain close to zero where VR frame time is decided: the open-plan showroom, the glass frontage (not an occluder) and the 1.6 x 2.4 m office opening all see most of the scene, and the side-room doors are not occluders. Umbra also adds a query for every camera on every frame, including the 15 Hz viewfinder. There is an untested risk of single-pass-stereo popping at openings and of a head leaned into a 0.25 m wall.
  - **Only partly done:**
    - Photo readback (VR-01/CPU-GC-4): it is still a synchronous ReadPixels, one stall per photo. AsyncGPUReadback needs RGB24 packing, row order and render-texture lifetime checks that cannot be made without Unity.
    - Preview lights (VR-07): the headlights' realtime range is not cut, the near streetlights are not ForceVertex, and nothing warns when VR starts on an unbaked preview. The rule to judge frame time only on a baked build stands in for the warning.
    - Grab scan (PL-5): it still does GetComponentInParent per collider; only the truncation is fixed.
    - KayKit lightmap-UV margins (BI-06): ThirdPartyModelImport is unchanged. The new density gives the car and the streetlights 45 texels per model unit, above the 40 their default margin assumes. "Kit models beside the spawn bake at their import margin density" checks this.
  - The 11 rejected findings need nothing.
- **Files changed** (`git diff --stat -- Prototype Start-Demo.cmd`, plus two new files). These must be copied into Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip under Assets/CrimeSceneDemo/<folder>/, keeping the existing .meta files. That sync is a separate step and **has not been done yet**.
  - Scripts: CameraScreen, DemoDesktopPanel, DemoFeedback, DemoLauncher, DemoLighting, DemoOverviewMap, DemoStatusBoard, DemoToolGeometry, DemoUi, DeployedTool, EvidenceCamera, HandInteractor, PhotoFrame, ToolHolder, XRComfortFade, XRControllerInput, XRHandHud, XRHeadTracking, XRLocomotion.
  - Editor: DemoBuild, DemoGeometry, DemoToolsBuilder, DemoValidation, NightLighting, NightStoreBuilder, NightStoreValidation, ShopCityValidation, ShopRobberyDressing, StoreKit, and the **new** ForwardOnlyShaderStripping.cs (needs a .meta with a fresh guid).
  - Shaders: CrimeSceneTape.shader (keep its existing guid, so PoliceTape.mat keeps its reference) and the **new** MapAlbedo.shader (needs a .meta with a fresh guid).
  - Outside the zip: Start-Demo.cmd, README.md (line 5) and Docs/CONTROLS_AND_INTEGRATION.md (F2, headset toast, PlayerPrefs).
  - Until the zip is refreshed, a project unpacked from it needs Scripts, Editor and Shaders synced from Prototype. Start-Demo.cmd mirrors those folders, so its builds are not affected.
- **To verify on the PC:**
  1. **NightStoreValidation.RunNightQuick.** This is the first compile of these edits. Check that the Console has no errors, including no shader errors for CrimeScene/Tape and Hidden/CrimeScene/MapAlbedo. The APIs to watch are EditorGraphicsSettings.GetTierSettings, Shader.passCount / Shader.FindPassTagValue (a Material.FindPass fallback is in a comment beside the tape check), ShaderUtil.ShaderHasError and Texture2D.GetRawTextureData<byte>. Expect +24 PASS lines. Confirm that "Tool released overhead in the staging bay drops to the street" passes at its test spot (6.5, 2.3, -4.0).
  2. **NightStoreValidation.RunNight.** This is the first bake ever, at 20 texels/m with combined-directional lightmaps. Write the bake seconds and the "Lightmaps=" line into this file: the estimate is 2-4 maps, the budget is 8, and the largest map must be 1024 px or less. No earlier figure exists to compare with. Expect +24 PASS lines.
  3. **StoreKitValidation.RunGallery.** Same PASS count. The KitMesh chart margins halve with the new density and every saved mesh is re-unwrapped. The old night_across_* mesh asset becomes an orphan, which should be reported only as Info.
  4. **ShopCityValidation.RunGlass.** Expect +16 PASS lines. GlassToolValidation "Shader compiles: CrimeScene/Tape" must still pass. Confirm that "Tool released overhead drops to the floor, not onto the roof" passes at (-3, 2.4, 5). The flashlight is now ForcePixel in the old shop too. Check that the shaded PoliceTape.mat reads well under the shop lighting and that the black CRIME SCENE text is still readable on it.
  5. **Launcher.** Run `Start-Demo.cmd bake`: build-latest.log should show `JEWELRY_BUILD_LIGHTING baked` and the start-menu footer "lighting baked". After the next pull or commit change, double-click Start-Demo.cmd without arguments. It must print "Baked lighting: ..." and bake again. `Start-Demo.cmd rebuild` must build a preview and delete Builds\Windows\lighting.txt. No validator can trigger the bake-failure fallback. If a bake ever fails, the log should show the `JEWELRY_BUILD_LIGHTING bake failed` warning and the footer "preview (the bake failed, see build-latest.log)".
  6. **Linear first unpack.** Delete UnityProject_Demo and run Start-Demo.cmd. ProjectSettings.asset must read `m_ActiveColorSpace: 1` and GraphicsSettings.asset `m_LightsUseLinearIntensity: 1`. Both files must still be valid YAML that Unity opens, with no BOM. build-latest.log should show a single texture import. UnityProject_Glass must stay Gamma.
  7. **Visual check on a baked build:**
     - Headlamp glints in the case glass and the jewellery.
     - The streetlight glow on the wet asphalt.
     - Normal-map relief on the plaster, paving and facade stone under the baked light.
     - Light pools on the shopfronts across the street.
     - No chart bleeding on the KayKit police car and streetlights.
     - Skyline towers acceptable at 14 texels per unit.
     - The overview map reads as a floor plan.
     - The photo frame and the DSLR rear screen do not shimmer.
  8. **Desktop, mouse and keyboard:**
     - GUILayout: open and close the drawer and F1, and click the strip's Panel button, the drawer's x and Settings "Controls (F1)". The Console must show no GUILayout errors.
     - Profiler: GC Alloc for DemoDesktopPanel.OnGUI should be near 0 B per frame on the idle HUD, in desktop and in VR mode.
     - Frame Debugger: no overview-camera render while the drawer shows Tools, and about one a second on Map.
     - HUD: hide it with F2, then press F10 and reopen the map. The HUD must be back on.
     - Tape: deploy tape and sweep the flashlight and a lamp across it. The yellow should brighten under the beam and stay dark outside it, with the stripes and sag unchanged. Tape behind a post or case should be in its shadow, with no light leaking through.
  9. **Quest 2 over Link** (when OpenXR is installed):
     - Frame time: judge it only on a `Start-Demo.cmd bake` or RunNight build, never on the preview. In the preview, check that the flashlight, UV torch and lamps still light per pixel with the headlights on.
     - Tape: it renders in both eyes (single-pass instanced) and casts a sagging shadow under the flashlight. Compare GPU frame time with the tape on and off.
     - Held tool: it stays locked to the controller in fast swings.
     - Hints: faded hints disappear, then return when the controller is looked at or the menu button shows them.
     - Recentre: the recentre at start blinks.
     - Photos: evidence photos, the viewfinder and the presenter's map show no controllers, hints or toast.
     - Viewfinder: holding the camera with the flashlight on shows no periodic frame spike in the Profiler.

# Interface redesign — October 13 (Claude, unverified, never compiled)

The monitor and the headset now share one look (DemoUi: dark glass, brass accent, the Barlow font, anti-aliased rounded panels, keycap hints, SDF tool icons), drawn differently for each: IMGUI on the monitor, world-space panels on the Quest 2 controllers. **Nothing here has been compiled or run in Unity.**

- **New runtime files** (each needs a .meta when synced; the zip has them): DemoUi.cs (palette, fonts from DemoUiTheme, runtime textures and IMGUI styles, icons, world panels/text, TextWidth), DemoUiTheme.cs (Regular/Medium/SemiBold fonts and the controller shader, added by DemoToolsBuilder.AddTheme to ToolSystem and by DemoBuild to the launcher scene), DemoFeedback.cs (one static toast feed: newest four, 3.5 s, posted by ToolHolder spawn/place/remove, ToolStation tape/measure/lamp, EvidenceCamera saved/failed, DemoSession reset, DemoLighting toggles, DemoBeacon, CrimeSceneState, refused desktop placement), XRHandHud.cs, Shaders/UiShaded.shader (opaque key-light + rim for the controller models), ThirdParty/Fonts (Barlow, SIL OFL 1.1).
- **Desktop (DemoDesktopPanel rewritten, public API kept):** HUD with status chips, a clickable hotbar (1–6 tools with icons and placed counts, F camera, L flashlight, U UV torch), crosshair with target name while mouse look is on, a tooltip on what the cursor is over, context key prompts and a warning line ("Aim at the floor…"), toasts top right, live view with thirds, shutter flash, reset-armed banner. Tab still cycles hidden → strip → full; the strip is restyled (still 56 tall) and the full layout is a 420-wide right drawer with Tools / Camera / Map / Scene / Settings tabs (last tab remembered). F1 or H opens a controls overlay with a mouse-and-keyboard and a Quest 2 page (pauses input like the drawer); Esc closes it or the drawer; F2 hides the HUD. Settings persist in PlayerPrefs: DemoPanelScale, DemoHudVisible, DemoShowPrompts, DemoPanelTab, DemoLookSensitivity, DemoInvertY, DemoFieldOfView. Panel clicks are deferred to the end of OnGUI so the IMGUI layout stays consistent; spawning from the drawer closes it so the tool can be placed at once.
- **Movement QoL (ShopWalkController):** C crouches (eye 0.95 m, half speed), Z held zooms to 30° with slower look, invert Y, sensitivity and field of view settings; Home also stands up and un-zooms. Number-pad 1–6 work too.
- **VR (XRControllerInput + XRHandHud):** a procedural Quest 2 style controller replaces the box (the box renderer is hidden, its collider kept), buttons light up while pressed; tooltips with leader lines (Trigger, Grip, A/X = the builder's Label, still "new cone", B/Y, Stick, left Menu) show for 20 s and then when the controller is looked at; the menu button still hides them on both hands. B/Y with an empty hand shows a tool carousel with icons for 2.5 s. The left wrist card shows tools, next marker, photos, torches and the pending tape selection. The right hand owns a lazy-follow toast below the view repeating DemoFeedback. Haptics per action (XRControllerInput.Pulse: refusal 0.75 > photo > place > spawn > teleport > grab > select). Teleport: ring marker with a faint disc, arc blue when valid and red otherwise, refusal pulse on an invalid release.
- **World status board and start menu** restyled with the same theme (board text unchanged apart from colour tags; panel sized to the text).
- **New checks** (DemoValidation.InterfaceChecks, run by ShopCityValidation after HandChecks and by NightStoreValidation before ToolStage; +11 each): "Interface theme carries the Barlow fonts and the controller shader", "Feedback feed keeps the newest four messages and expires them", "Hotbar maps keys 1-6 to the tool kinds in order", "Help overlay pauses desktop input", "Context prompt follows what the desktop visitor holds", "Crouch lowers the desktop eye height", "Zoom narrows the view", "Controller tooltips and status card build on both hands", "Tool carousel shows the newly selected kind", "Controller interface updates a frame and counts a new tool" (XRHandHud.LateUpdate by reflection), "Haptic pulses rank refusal above placement above grab".
- **Review fixes (same day, two read-only reviewers; no compile, shader or check failures found):** the VR toast is parented to the rig so teleports and snap turns carry it instead of sweeping it across the room; XRHandHud and the teleport marker free the materials they create; RoundedQuad rounds sizes to millimetres (bounded mesh cache); the controls overlay disables the panel and map underneath it; the hotbar's hit test covers its frame; HintWidth uses the keycaps' 24 px minimum; Camera/Map tab images fit beside the scrollbar; the hotbar is drawn only with a desktop interactor; the "Z zoom" camera hints are gone because zoom narrows the view, not the photo lens. Known: world text (queue 3100) can draw over a nearer panel of another element; beyond the one validated frame, look-to-fade, highlights, haptics and the teleport ring only run in a headset.
- **To verify on the PC:** compile; RunGlass and RunNightQuick pass; look at the HUD at 1080p and at scale x2; Quest 2 over Link (when OpenXR is installed): tooltip placement around the real controller pose, text size, whether the hint fade-in angle (30°, 0.8 m) feels right, haptics strength.

# Tripo model guide — October 12

[TRIPO_GUIDE.md](TRIPO_GUIDE.md): researched settings and 36 prompts for Tripo Studio (P2.0 for hero props, H3.1 + Smart Low-poly for small ones; one object per generation except four clutter sets), with 33 ready single-object input images cut from the asset sheets in Docs/TripoInputs. Converted models go to Prototype/ThirdParty/StoreKit/<name>.fbx; cases, safes, doors, jewellery props and tools still need per-family override code that keeps their hinges, panes and states, plus a metallic/roughness repack on import.

# One-click build and start menu — October 12 (Claude, unverified)

**Start-Demo.cmd** (repo root) is the new entry point: optional git pull, unpack Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip into UnityProject_Demo once, robocopy Prototype Scripts/Editor/Shaders/ThirdParty into it, run `Unity -batchmode -noUpm -executeMethod DemoBuild.BuildLatest` (add `-demoBake` via `Start-Demo.cmd bake`), then start Builds/Windows/JewelryStoreDemo.exe; it skips the build when Builds/Windows/built-from.txt already holds the current commit. (October 15, unverified: `Start-Demo.cmd bake` also writes Builds/Windows/lighting.txt, so later rebuilds bake again until `Start-Demo.cmd rebuild` builds a preview and deletes it; build-latest.log carries `JEWELRY_BUILD_LIGHTING baked` or `preview`; a failed bake ships the preview and the footer reads "preview (the bake failed, see build-latest.log)".) DemoBuild.BuildLatest regenerates the night store (NightStoreBuilder.BuildScene, now public with askToSave), creates Scenes/DemoLauncher.unity, writes Resources/DemoBuildInfo.txt (date, commit from JEWELRY_COMMIT or .git, lighting mode), registers DemoLauncher + JewelryStoreNight (+ JewelryStoreGlass when its scene exists) and builds. Runtime: DemoLauncher (IMGUI start menu, newest first, Enter / 1–9, Esc quits) and ReturnToLauncher (F10 back to the menu, survives scene loads). Check: "Start menu lists the night store first and leaves itself out" (NightStoreValidation static checks). The bundled JewelryStoreGlass scene was generated by October 10 code and may log missing references for features added since (e.g. the measure/lamp prefabs); the night store is generated fresh every build.

Review fixes (Oct 12, unverified): Start-Demo.cmd pulls inside one parenthesised block and restarts itself (JEWELRY_UPDATED), so a pull that rewrites the launcher cannot desynchronise cmd; paths echoed inside blocks are quoted (a folder like "...-main (1)" no longer aborts the script before `pause`); the sync uses robocopy /MIR /XF *.meta so scripts deleted from Prototype leave the project. NightQuality now saves and restores QualitySettings in the player as well, so the daytime shop no longer inherits the night shadows/MSAA after F10. DemoBuild treats JEWELRY_COMMIT "unknown" as unset and reads .git. The start menu says desktop only (no XR plug-in in the build), and DemoModeSwitch ignores F9 in a player with no XR device.

# Full audit — October 11 (Claude, unverified, never compiled)

Seven area reviewers read every script, shader, doc and launcher; two skeptics per finding tried to refute each one: 30 candidates, 26 confirmed and fixed, 4 refuted; the staged night assets were wired; a final compile and regression recheck found no code defects. Code fixes include: the comfort fade now covers the teleport and snap turn instead of starting after them; per-holder highlight tracking (one VR hand no longer clears the other's highlight); a failed photo write no longer leaves the frame and thumbnail showing an unsaved picture; Bake Lighting Only switches to Linear first; the night store styles its tools and no longer overwrites the shop's tool prefabs; the carried lamp lands on the street level; old-shop checks fixed for the five kit cars and the sky clear flags; a failed generation starts a fresh results file. **Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip is refreshed** with every script, shader and ThirdParty folder on main, including NightMaterials/Graphics/Audio, DemoSoundBank.cs.meta and UVFluorescence.shader.meta; CLAUDE.md now names that zip and the night validators.

# Staged night assets wired — October 11 (Claude, unverified, never compiled)

NightMaterials, NightGraphics and NightAudio are now used by the night store, each with the old behaviour when its files are absent. StoreKit.Surface assigns NightMaterials/`<Name>_MetallicSmoothness.png` as the Standard `_MetallicGlossMap` (`_METALLICGLOSSMAP`, smoothness from metallic alpha, `_GlossMapScale` 1, imported linear through DemoAssetLibrary.DataMap); the ceiling takes CeilingPaint Color/Normal (before the GeneratedTextures stand-in) and KitCladding (piers, fascia band, parapet) takes FacadeStone. NightStoreBuilder places opaque, non-emissive sign quads (DemoAssetLibrary.SignTexture): ShopFascia above the entrance, Closed inside the left door's glass (with a plain back), StaffOnly on both side-room door leaves, CollectionRings/Necklaces in the left/right wall sign frames (procedural frames only). Shoe/scuff/dust decals, SceneTape and MarkerNumbers stay unused. Audio: a new runtime DemoSoundBank (Scripts/DemoSoundBank.cs, needs a .meta when synced) holds the WAVs as AudioClip assets assigned by NightStoreBuilder.StagedSounds (PCM, decompress on load), so a player build carries them; DemoSounds prefers its clips (shutter → CameraClick, ToolSetDown for placing, FlashlightSwitch on the L/U/panel/VR toggles, PanelSelect for panel buttons, DistantAir_Loop for AmbientSound) at the README volumes, the generated clips stay as the fallback and the old shop has no bank. New night checks (counted only when the files are synced): "Night materials use packed metallic-smoothness maps", "Night ceiling and facade use the staged plaster and marble", "Fascia shows the shop sign", "Closed sign on the entrance door and Staff Only on the side-room doors", "Collection signs sit in the wall sign frames", "Staged floor decals stay unplaced" (always), "Staged sounds replace generated clips" (or "Generated sounds stay without staged audio"). MaterialBudget is 96 (was 90) for the five sign materials. The Glass zip now carries these folders.

# Asset preparation — October 11, 2026

New staged assets and reuse manifest are documented in [ASSET_PREPARATION_2026-10-11.md](ASSET_PREPARATION_2026-10-11.md). Read root [for Koda.md](../for%20Koda.md) for decisions and remaining integration. ReferenceTextures preserved; NightMaterials, NightGraphics, NightAudio and NightModelReuse added. File-level checks passed; no Unity/headset run or runtime integration in this batch. Downloads/JewelryStore_NightAssets_20261011.zip is asset-only, not a tested Unity project.
# Latest source change: expanded NIGHT store built from the references (October 11, Claude, unverified)

Implements batches 1–5 of [NIGHT_ARRIVAL_PLAN.md](NIGHT_ARRIVAL_PLAN.md) following [REFERENCE_LAYOUT_PLAN.md](REFERENCE_LAYOUT_PLAN.md). Six sequential implement → three-lens review (compile, behaviour, VR performance/realism) → fix passes. **Nothing has been compiled or run in Unity.** The old shop and every old validator are kept; shared builders were made layout-driven with defaults equal to the old values (DemoToolsBuilder.ToolLayout.Shop, DemoValidation.ToolTestPoints).

Run on the PC, in an isolated project with Scripts, Editor, **Shaders** and ThirdParty synced (the UV shader is new):
1. `StoreKitValidation.RunGallery` — Edit-mode check of the fixture/prop library, scene StoreKitGallery, captures in Docs/VerificationStoreKit.
2. `NightStoreValidation.RunNightQuick` — builds JewelryStoreNight without baking (baked lights previewed as realtime, no shadows) and runs every Play-mode check.
3. `NightStoreValidation.RunNight` — the same with the lightmap/probe bake (ProgressiveGPU, CPU fallback). Prints checks, bake seconds, lightmaps, triangles and renderers; captures night_*.png and day_*.png in Docs/VerificationNight.
**RunNightQuick, RunNight and the Night Store build/bake menu items (Build (quick), Build And Bake, Bake Lighting Only) switch the whole project to Linear colour space and turn on linear light intensity** (PlayerSettings.colorSpace and GraphicsSettings.lightsUseLinearIntensity are project-wide; one-time texture reimport; older scenes and the old-shop validators stay valid but render in linear from then on, and lights with intensity other than 1 look different, so use an isolated project). ThirdPartyModelImport gives third-party models lightmap UVs (one-time reimport). (October 15, unverified: Start-Demo.cmd now writes Linear and linear light intensity into UnityProject_Demo's ProjectSettings right after the first unpack, before Unity's first import; UnityProject_Glass and Build-Windows.cmd keep the zip's Gamma.)

What exists now:
- **StoreKit** (StoreKit.cs, StoreKitProps.cs, KitMesh.cs, ProceduralMesh.cs): low-poly procedural versions of everything on the asset sheets: 26 fixtures (display/wall cases with separate panes and broken variants, counter, drawer unit, desk, chair, shelving, locker, two safes with hinged doors, sofa, side table, planter, signs, storefront/office/service doors, window modules intact/cracked/broken, bollard, exit sign, camera, track/down lights, POS, keypad) and the jewellery props (bust, bracelet/earring stands, ring tray, risers, boxes with hinged lids, trays, bags, rings). Metric UVs + lightmap UVs; palette materials take ReferenceTextures automatically. **A converted model at Prototype/ThirdParty/StoreKit/<snake_case_name>.fbx or .obj replaces the procedural one with no code change, but only for pieces placed through StoreKit.Instantiate.** In the night store these are the layout fixtures on NightStoreBuilder.Place's default branch (counter, drawer_unit, office_desk, shelving_unit, locker_cabinet, waiting_sofa, side_table, planter, sign_frame, exit_sign, security_camera, pos_terminal, alarm_keypad) plus downlight, track_light and bollard. Display/wall cases, safes, office/service/storefront doors, window modules, the toppled office chair and all jewellery props are built directly with their state (Glass_* panes, hinged doors/lids) and always stay procedural until their builders get an override hook that keeps those children.
- **Night store** (NightStoreLayout = one definition of every anchor/region; NightStoreBuilder; NightStoreDressing = the robbery state, swappable): 16 × 12 m shop, eight-bay facade with forced storefront doors and broken/cracked bays, central three-case cluster, side islands, wall cases, rear partitions, office with two open safes, closed side rooms, seating corner; pavement, kerb, street, buildings, bollards, streetlights, police car aimed at the entrance; spawn beside the car; equipment table with the tools, camera, board and frame on the pavement. InteriorBounds covers showroom, office, threshold, pavement and staging bay; map and review sketch follow it.
- **Lighting** (NightLighting.cs, NightQuality.cs, LightBarBlink.cs): all baked — moonlight, four streetlights, two headlights; emissive exit sign/keypad/lenses; light probes; three baked box-projected reflection probes; dark night sky and fog. At runtime one light casts a realtime shadow: the white flashlight while it is on, otherwise the scene lamp lit last (the UV torch never casts one; other lit lamps are unshadowed and use the shorter LampUnshadowedRange); NightQuality sets 4 pixel lights, hard shadows 14 m, 4x MSAA, no realtime probes. N toggles a diagnostic daylight for comparing with the reference.
- **Equipment**: unbranded DSLR whose rear screen shows the live viewfinder (CameraScreen); white flashlight with a cookie and model (L, right stick click); **UV torch** (U, left stick click) with the CrimeScene/UVFluorescence shader — authored traces glow only in the beam with a clear line of sight; three placeholder traces hidden by default (panel toggle; meaning is the instructor's decision); **scene lamp** tool (DemoToolKind.Lamp, key 6, at most two lit); belt-stanchion tape posts; cone with base.
- **Budgets checked** by the validator: triangles (split store/exterior/dressing/tools), renderers, materials, textures, transparency, lightmaps, realtime lights and shadows.
- **ReferenceTextures** (Tools/reference_textures.py): **test placeholders** cut/sampled from the user's reference renders; the user will supply final textures with the same names.

Known open points (from the batch reports): kerb climbing by the CharacterController and some physics assumptions are untested; baked light intensities assume inverse-square falloff; KayKit headlamp quads may not line up exactly with the car model; no intact/robbed toggle in the night store yet; lamps without the shadow can still light through walls within their shortened LampUnshadowedRange; the desktop torch model is hidden from photo, viewfinder and map cameras (unverified until run in Unity); editing in diagnostic daylight and saving would bake the wrong baseline (warning logged).

Old-pipeline audit fixes (Oct 11, unverified in Unity): the backdrop check counts ShopCityRefinement.ParkedCars (5 kit cars, was 3); RunTextured keeps the player camera on the HDR sky after CreatePlayer; DemoValidation.Begin starts a fresh runtime-results.txt so an Edit-mode generation failure is not appended to the previous run's PASS lines. (Both former open points, the shader .meta and CLAUDE.md naming the Expanded zip, are resolved.)

Docs/launcher audit fixes (Oct 11): Build-Windows.cmd now builds UnityProject_Glass (extract the Glass zip there; Open-Unity-Blockout.cmd stays on the historical Expanded project); README retitles the Glass download because the zip now carries uncompiled night-store sources (the 137-assertion pass is the October 10 blob, `d5e9cc8`) and splits Controls into tested and unverified keys; CONTROLS_AND_INTEGRATION adds the scene lamp to B/Y, Trigger, T and HandInteractor.Trigger; ShopAssetDressing.Count is 23 (ten KayKit furniture pieces).

Cross-file audit fixes (Oct 11, unverified in Unity): the night store now styles its own tool prefabs. DemoToolsBuilder.ToolLayout has AssetFolder (night: NightStoreGenerated/Tools, so a night build no longer overwrites the shop's GeneratedTools prefabs that saved shop scenes reference) and Styled; DemoToolsBuilder.StyleStation applies the shop's presentation styling (charcoal/ivory L-scale bands, yellow markers/reels, depth-tested CrimeScene/WorldText numbers and distance label, CrimeScene/Tape ribbon with CRIME SCENE warnings) before the rack samples are made, and gives the night status board a depth-tested label. The shop passes (ShopPresentationExpansion.StyleTools, ShopGlassRefinement.StyleTape) call the same shared methods with their own materials, so shop output is unchanged. A carried lamp clamps to Bounds.FloorAt (street level in the staging bay) instead of y 0. New night checks (+3): "Night tool prefabs are styled (scale bands, depth-tested numbers, police tape)", "Night tool prefabs live in the night store's own folder", "Carried lamp rests on the street in the staging bay". The styled rack samples (up to five materials: Graphite, Linen, EvidenceYellow, ConeOrange, ToolNumber) and the five NightGraphics sign materials both add to the night material budget (96, was 90), which may exceed its +6 headroom; on the first RunNightQuick set MaterialBudget from the logged "Budget: materials=" value.

# Reference received — October 11

Read [REFERENCE_LAYOUT_PLAN.md](REFERENCE_LAYOUT_PLAN.md) alongside the nighttime scenario. store-reference.jpg is now visually inspected: wide glazed frontage/central breakage, broad showroom with central and side displays, rear office with two safes, rear side partitions and front-right seating. Proposed 16 x 12 m footprint is an estimate, not a measured fact. The isometric cutaway does not mean a roofless first-person scene. Image UI and pre-placed tape/markers are not required features.

This remains a documentation-only planning update. Preserve the previous shop and build the new layout in a separate scene; no code/build changes yet. Earlier notes saying the reference is missing are superseded.

# Current user direction — October 11: expanded store, nighttime first response

**Planning only.** Read [NIGHT_ARRIVAL_PLAN.md](NIGHT_ARRIVAL_PLAN.md) before implementing. The user wants a larger store closely matching a reference, but no image/link was attached. Do not invent exact proportions. Preserve the previous shop and reuse its components in a new scene.

Scenario: a neighbor hears a nighttime disturbance, calls police, and the visitor arrives at a ransacked jewelry store. Proposed initial spawn is beside a parked car facing the frontage, headlights on. Exterior staging is now playable; revise InteriorBounds, IndoorBoundary_Front, teleport/tool limits and map/review framing together. Store fixtures start off. Reuse tape, cones, markers, photos and white flashlight; plan an unbranded DSLR visual, UV trace demonstration and optional portable lamp.

This direction takes priority over immediate texturing. No code, scene, build or asset changes were made in the planning pass. All earlier validation claims below apply to their historical revisions.

# Latest source change: five feature batches — panel, viewfinder, measuring tape, review report, lighting, VR comfort (October 10, unverified)

Implemented and self-reviewed in sequence on top of the imported-kits batch below; every batch added its own checks. Regenerate with **ShopCityValidation.RunTextured** (RunGlass still works for the untextured variant). None of it has been compiled or run in Unity yet.

- **Presenter panel** (DemoDesktopPanel): three layouts, Hidden → Strip (slim top bar: status, photo/intact/reset buttons, thumbnail, live view) → Full (reorganised sidebar), cycled by Tab; persisted UI scale 1–2 ([ and ] or +/- buttons, PlayerPrefs DemoPanelScale) through GUI.matrix; only the Full layout pauses desktop input (CapturesInput). The overview map draws per-kind tool glyphs, the camera and the visitor, and clicking it places the instructor beacon. PLACED TOOLS shows per-kind counts and the last 8 with Remove.
- **Live viewfinder** (EvidenceCamera.Viewfinder): a 480x270 render texture while the camera body is held, refreshed at 15 Hz without enabling the photo camera, shown in every layout and bottom-right when the panel is hidden; PhotoCount counts saved photos. PhotoFrame and the panel share EvidenceCamera.LastPhoto, which is updated only after the PNG write succeeds (the frame is read into a private scratch texture first, so a failed save leaves the previous photo on the wall and in the panel; audit fix Oct 11, unverified).
- **Measuring tape** (DemoToolKind.Measure, key 5, fifth rack sample, panel button, VR cycle): an 8 cm reel; two placed or selected reels stretch a flat yellow ribbon whose floating label reads the span ("1.50 m") and follows moved endpoints (SceneTape.DistanceLabel). ToolStation connects same-kind pairs only, with separate auto-connect chains for posts and reels (ToolStation.PendingPrompt feeds the board and panel).
- **Instructor review** (SessionReviewRecorder): review.html now has a photo contact sheet (shot number, time, camera pose), a shot list table and a 240 px inline SVG plan per entry drawn from InteriorBounds, with per-kind glyphs (colours shared with the panel via DemoDesktopPanel.KindColor), recorded ribbon connections (Entry.connections, additive) and the camera heading on photo entries.
- **Lighting** (DemoLighting, new): night preset (N or panel: lights to 25%, ambient 30%, fixture emission off, closer darker fog, one-second fade, dusk sky swap when the HDR pack is present) and a warm flashlight (L, right-stick click in VR, panel) riding the view or right hand across F9 switches. **Beacon** (DemoBeacon, new): pulsing amber ring and 2 m column clamped into InteriorBounds, placed with B at the pointer or by clicking the panel map; reset restores day, no flashlight, no beacon.
- **VR comfort**: teleporting aims a ballistic arc (XRLocomotion.FindArcDestination, LineRenderer green/red) sharing the straight ray's validation; XRComfortFade goes to full black in the frame of a teleport or snap turn, so the jump is never seen, then fades back in over 0.25 s (head camera, VR-only; the quad sits just past the near clip plane in render queue 5000 so close walls or tools cannot cover it; audit fix Oct 11, unverified); a tool's highlight is counted per holder, so one VR hand cannot untint a tool the other holds ("A held tool stays highlighted when the other hand moves on", HandChecks; unverified); hand hint labels gained a dark backing and two hint lines, toggled for both hands by the left menu button.
- **More assets**: KayKitPrototype pack (CC0 shipment boxes, barrel; staged in the safe room, ShopAssetDressing.Count is 23 with a coffee table, book set and standing frame), hatchback and station wagon at the far kerbs, crates by the dumpster, and the_sky_is_on_fire dusk HDR (NightSky.mat).

New checks: 4 panel (PanelChecks + PhotoStage photo count), 5 + 3 measuring tape (ToolStage, InteractionChecks, HandChecks), 4 + 1 review report (PhotoStage, ResetStage), 6 + 1 lighting/beacon (LightingChecks, HandChecks, dusk-sky check when textured), 4 VR comfort (LocomotionChecks) — RunTextured should land around 182; the validator prints the exact count. Unity will generate .meta files for the new scripts (DemoLighting, DemoBeacon, XRComfortFade) and assets; the Glass snapshot zip carries minimal metas for the three scripts.

# Latest source change: imported kits, scanned textures, HDR sky, fixes (October 10, unverified)

**ShopCityValidation.RunTextured** now generates the full variant: RunGlass chain **with imported models** (the forms flag no longer switches them off), generated textures, and four new packs under Prototype/ThirdParty (each with LICENSE.txt; see ASSET_REGISTER.md): **KayKitCity** (police/taxi/sedan cars with wheels at the kerb, streetlights replacing the lamp boxes, benches, four towers alternating with the Kenney blocks, hydrant, traffic lights, dumpster, bushes in a StreetFurniture group), **KayKitFurniture** (standing lamp, rug, two picture frames, rear-wall shelf, safe-room cabinet and cactus; ShopAssetDressing.Count is 16), **CgbookcaseTextures** (parquet for the new SafeRoomFloor material, worn gold for JewelryGold) and **Environments** (venetian crossroads panorama as Skybox/Panoramic StreetSky.mat, light-room cubemap as custom reflections; cameras that cleared to a colour now clear to the sky). DemoAssetLibrary shares one atlas material per pack so each kit combines into a single draw; the backdrop triangle budget check is 40k. Sync Prototype/ThirdParty/* into Assets/CrimeSceneDemo/ThirdParty/*; Unity writes the .meta files and ShopTextureFinish sets the HDR import shapes itself.

Fixes in the same batch: the glass cubemap is baked after the textures; the recolour pass skips the tool rack; Loft/combined meshes get box-projected UVs and tangents in DemoGeometry.SaveMesh (checked by "Generated meshes carry UVs and tangents"); ToolsChanged fires on tape selection so the status board shows "choose the second post"; Hold no longer carries a tool while the presenter panel is open; the VR panel's Photograph button uses the held camera or the main view; highlight tints toward amber (visible on textured tools); DemoModeSwitch re-samples the headset while in Auto; IntactStateBuilder strips colliders from toggled props. **EvidenceCamera** encodes and writes the PNG on a worker thread (Status "Saving..." between shutter and PhotoSaved) and keeps the frame as `LastPhoto`, which PhotoFrame shows directly. Expected RunTextured count with every pack present: about 154 (137 + the previous 5 + 12 new checks, one of which replaces the geometry-only dressing check).

# Latest source change: generated textures and the L-scale (October 10, unverified)

**ShopCityValidation.RunTextured** = RunGlass + ShopTextureFinish: tileable textures generated by Tools/make_textures.py (Prototype/ThirdParty/GeneratedTextures: stone tiles, plaster, ceiling, walnut, brushed brass, marble, dark carpet, asphalt, pavement; albedo + normal maps, project-owned, no downloads) are applied to the existing finish materials with per-renderer tiling from world size; the tile skins are hidden under the textured floor; sidewalks got their own Pavement material in ShopCityRefinement. Without the texture files the flat finish stays. A fourth tool, the forensic **L-scale** (DemoToolKind.Scale: two 15 cm arms with 1 cm bands; key 4, rack sample, panel button, VR kind cycle) spawns like the others. Sync Prototype/ThirdParty/GeneratedTextures into Assets/CrimeSceneDemo/ThirdParty/GeneratedTextures; Unity creates the texture .meta files on import. Expected: RunGlass count + 5 (one scale check, four texture checks).

# Latest update: glass and tools

Current project: **UnityProject_Glass**, scene **JewelryStoreGlass**. Download Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip; launcher Open-Unity-Glass.cmd. [Report and captures](GLASS_AND_TOOLS.md). 137 automated assertions passed on the October 10 snapshot (`git show d5e9cc8:Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip`); the current zip also carries the uncompiled October 11 night-store/StoreKit sources, so that pass does not cover it. Build-Windows.cmd builds UnityProject_Glass; physical Quest input/FPS and standalone build still pending.

Regenerate with ShopCityValidation.RunGlass. Copy all Scripts, Editor and Shaders. ShopGlassRefinement runs after the Interior and Robbery passes, bakes one 256px cubemap, seats all fragments, adds fracture edges and styles the tape prefab. Marker panel angles are fixed in DemoToolGeometry; ShopPresentationExpansion saves DepthTestedLabel into marker prefabs. Do not restore default font materials there.

ToolHolder.Place calls ToolStation.NotifyPlaced; spawning or switching modes does not auto-connect tape. New posts connect to the previous placed post. Repositioning an existing post does not duplicate tape. StartNewTapeRun clears the chain. R/Delete or the panel removes placed tools; connected tape disappears with its endpoint. DesktopInteractor.Aim is shared by runtime pointer input and selection validation.

Older milestone notes follow; use the Glass project above for current work.

# Latest update: robbery refinement

Use **JewelryStoreRobbery** in **UnityProject_Robbery**, or Downloads/JewelryStore_Robbery_Unity6000.6.2f1.zip and Open-Unity-Robbery.cmd. [Report and screenshots](ROBBERY_REFINEMENT.md). Regenerate with ShopCityValidation.RunRobbery. This runs the prior Interior chain then ShopRobberyDressing.Apply, preserving intact stock separately from disturbed stock. Never run dressing twice on the same scene.

91 automated assertions passed (92 PASS lines with completion). Desktop panel starts closed; right click toggles look, Escape releases; Tab opens the panel and suspends walking/tool hotkeys. Overview rendering is suspended while hidden. Invalid pointer targets cannot place a tool at an old location. Physical Quest input/performance and standalone builds are still pending.

Previous milestone notes follow for context; use the Robbery project above for current work.

# Assistant handoff

## Current state

All work lands on main; pull before editing.

**Latest verified scene: JewelryStoreInterior**, October 10, 2026. Unity 6000.6.2f1 Built-in; all source and the world-text shader compiled and **81 automated Editor Play mode checks passed**. See [INTERIOR_FINISH.md](INTERIOR_FINISH.md) for renders, material choices, measured budgets and limits.

Download Downloads/JewelryStore_Interior_Unity6000.6.2f1.zip; extract into UnityProject_Interior beside README, use Open-Unity-Interior.cmd and open Assets/CrimeSceneDemo/Scenes/JewelryStoreInterior.unity. The same project is already on the PC.

Run ShopCityValidation.RunInterior in an isolated project to regenerate. RunForms and historical downloads remain available. Imported-assets RunExpanded was not rerun.

Order: Forms -> ShopInteriorFinish (walls/floor/lights/doors) -> ShopDisplayFinish (glass/cases/jewelry/world labels). Keep Prototype/Shaders/WorldText.shader synchronized into Assets/CrimeSceneDemo/Shaders; Scripts/DepthTestedLabel.cs keeps the font atlas live. All materials use flat colors; no new texture maps. Glass has no refraction. The staff door is static, fixed open; the safe door still follows the intact/robbed toggle.

Measured: finish floor/door geometry 358 triangles / 10 renderers; display strips 96 / 1; form detail 1,624 / 24; city 5,044 / 19. Initial active scene MeshFilters: 14,058 triangles / 322 mesh renderers, excluding text. Four shadowless lights; three local fills use vertex lighting. These are content counts, not FPS measurements.

The inherited interaction fixes remain: closest item wins when grabbing; release holders before mode-switch deactivation; synchronous grab tests call Physics.SyncTransforms. Quest tracking, headset frame time/stereo, hands-on usability and standalone build remain pending. Package Manager needs -noUpm; the known Editor SearchDatabase exception did not prevent validation.

PC root: C:\Users\MOBPC\Documents\Codex\Jewelry-store-vr-demo\Jewelry-store-vr-demo-main

## Goal

Instructor-led VR jewelry robbery demonstration by October 27, 2026. No score, timer or forced procedure. Keep the user inside the shop, with a visible lightweight street outside. The same scene must run with mouse and keyboard (for testing) and in VR.

## Source map

Runtime: DemoModeSwitch (desktop/VR, F9), XRHeadTracking, XRControllerInput (built-in XR input API); ToolStation, DeployedTool, SceneTape, DemoToolGeometry (tools); EvidenceCamera (photos, HoldBy/ReturnToRack, CaptureFromView, LastCapturePosition); SessionReviewRecorder (local review JSON/HTML with photos and tool positions); DemoSession, ResettableSceneObject (reset; Resetting fires before clearing); InteriorBounds (shared interior regions); ToolHolder → DesktopInteractor (mouse/keyboard) and HandInteractor (tracked hands); XRLocomotion (teleport, snap turn); ShopWalkController (desktop walking); DemoDesktopPanel (presenter panel with DemoOverviewMap); DemoStatusBoard (wall text); ToolRackSample (rack samples spawn tools).

Editor: DemoAssetLibrary (third-party models with fallback), DemoGeometry (shared primitive/material/text/mesh helpers), JewelryStoreBuilder, ShopCityRefinement, ShopPresentationExpansion (geometry); DemoToolsBuilder (tool station, review recorder, status board, photo frame, player for desktop and VR); ShopAssetDressing (third-party props); IntactStateBuilder (intact overlay + CrimeSceneState); DemoValidation (shared checks) with JewelrySceneValidation.Run, ShopCityValidation.Run and ShopCityValidation.RunExpanded; DemoBuild (Windows player).

Night store (October 11–15, unverified): NightStoreLayout (every anchor), NightStoreBuilder, NightStoreDressing (robbery state and the closing procedure), NightLighting, StoreKit/StoreKitProps/KitMesh (fixtures and the palette set table), PoliceLook (the one police setting), NightStreet, NightInterior, NightDecals, NightAudioBuilder and PackImport (import rules for the twelve realism packs), validated by NightStoreValidation and StoreKitValidation. Runtime additions: PresenterSet (presenter switches), GlowBillboard, AmbientZones, AmbientEvents, Footsteps, DemoMusic, LightBarBlink, NightQuality. Tools/sync-project.cmd mirrors Prototype into a project for Start-Demo.cmd and Open-Unity-Glass.cmd.

## Next technical dependency

Unity Package Manager fails to start normally (IPC server timeout); the -noUpm launcher permits package-free work. SETUP.md lists things to try. Resolve this, then install XR Plug-in Management + OpenXR; the scene needs no further changes to run in VR.

## Pending

Package Manager fix, OpenXR install, Quest tracking test, hands-on keyboard/mouse usability, headset performance, standalone build validation (Build-Windows.cmd is ready but unrun). Interior colliders and InteriorBounds constrain the desktop controller and teleport targets, not physical headset tracking.

## Collaboration rules

Read the brief, roadmap and this file. Preserve .meta files. Update source and the Glass snapshot (Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip, project UnityProject_Glass) together and run the validators, RunExpanded included, in the Glass project. The Expanded zip, like the Forms/Interior/Robbery/City zips, is a historical snapshot frozen at October 9 without the night-store, StoreKit, UV, lamp or beacon code (CLAUDE.md still names the Expanded zip and needs the same change by the user); exclude Library/caches/InstructorReviews. **Pull `main` before editing**; two assistants work on this repository and the October 9 double implementation of mouse placement cost a day. Keep deployment ownership during XR grabbing and release held objects before reset (ToolHolder does this on DemoSession.Resetting). Record external asset licenses. Repeat tests when changes or unresolved concerns justify them.

## Review fixes after batch 13 (unverified)

DemoPhysics.Nearest is the one ignoring raycast (64 hits, upward-only option) used by the pointer, hands, tool settling and teleport aiming (which now skips the rig and the held tool). A tool knows its Holder, so two hands cannot hold one tool and a hand holding the camera spawns nothing. Settling casts from above the tool (clears a case it was lowered into) and is silent on reset or disable. DemoModeSwitch also disables the CharacterController in VR. The overview map culls a ceiling layer instead of toggling objects. The photo frame uses the Standard shader (always in builds). The walker clamps before its single Move and caps fall speed. The review recorder keeps an entry when writing fails; the mouse wheel over the panel no longer rotates the tool. Expected City result: 70 assertions.

## Batch 13 — more models and a reusable converter (October 9, 2026, unverified)

Six more Khronos sample models (Lantern as the street lamps; ChronographWatch and the plant, candle holder, iridescent lamp and damask chair as shop dressing), decimated and converted with the new Tools/glb_to_obj.py (trimesh + fast_simplification; texture downscale, decimation, scale, grounding). ShopAssetDressing.Count is the single source for the dressing check. Expected City result: 67 assertions.

## Batch 12 — intact store comparison (October 9, 2026, unverified)

CrimeSceneState (on the store root) swaps the robbed view (FixedEvidence, jagged glass, dropped tray, fallen trim, darkened door edges) for an intact overlay (whole storefront pane, intact case tops, stock on the emptied pads) and closes the safe door; I key or the panel button toggles it, and a session reset returns to robbed. IntactStateBuilder generates the overlay; JewelryStoreBuilder.Stock is shared with the display generator; DemoGeometry.Translucent is shared with the expansion's glass. Roadmap X01. Expected City result: 67 assertions.

## Batch 11 — sound and lit fixtures (October 9, 2026, unverified)

DemoSounds generates a click (tool taken), a thud (tool set down) and a looping street ambience (AmbientSound at the storefront); no audio assets. Ceiling fixtures and street-lamp heads use an emissive LampGlow material (DemoGeometry.Glowing). The panel ends with the model credits and DemoBuild copies every ThirdParty LICENSE.txt into Builds/Windows/ThirdPartyLicenses. Expected City result: 64 assertions.

## Review fixes after batch 10 (unverified)

Teleport goes to the destination the marker showed, refuses surfaces above step height, and snap turn moves the root through Relocate; the panel evaluates the pointer on demand and exposes RequestReset (two requests within four seconds) for validation; Remove clears the desktop hover; the snapshot no longer carries the removed XRRigBuilder.

## Batch 10 — photo frame and third-party props (October 9, 2026, unverified)

PhotoFrame: the last photograph appears on a quad on the wall above the rack (VR and monitor) and as a thumbnail in the panel. ShopAssetDressing places Khronos sample models (armchair, sofa, vase, bottle, sunglasses; licenses in ASSET_REGISTER.md and ThirdParty/KhronosSamples/LICENSE.txt) with box colliders on the furniture; the TrafficCone model replaces the generated cone visual when present (one bounds collider on the tool root). Unity generates .meta files for the OBJ/MTL/PNG on first import; commit them from the PC. Expected City result: 61 assertions.

## Batch 09 — usability, presenter map, first third-party models (October 9, 2026, unverified)

Tool rack samples (half-size cone, marker and tape post on the rack): click one or grab it to get a new tool in the hand. The pointed, nearest or held tool is highlighted (DeployedTool.SetHighlight, property block). Each VR hand has a small label naming the tool its A/X button spawns. DemoOverviewMap renders a top-down view into the presenter panel with the player's position; the panel was restyled (dark background, sections, mode-aware guide). DemoAssetLibrary places models from Assets/CrimeSceneDemo/ThirdParty with a primitive fallback; the Kenney Starter Kit City Builder models (CC0, see ASSET_REGISTER.md) replace the distant box buildings, add a fountain and street trees. Unity generates .meta files for the OBJ/MTL/PNG on first import; commit them from the PC. Expected City result: 56 assertions.

## History

- Batch 13 (Claude, unverified): six more Khronos models, Tools/glb_to_obj.py converter.
- Batch 12 (Claude, unverified): intact/robbed store toggle (X01).
- Batch 11 (Claude, unverified): generated sounds, ambience, emissive fixtures, credits, licenses shipped with builds.
- Batch 10 (Claude, unverified): PhotoFrame, ShopAssetDressing with Khronos sample models, TrafficCone tool visual.
- Batch 09 (Claude, unverified): rack samples, highlight, hand labels, overview map, panel restyle, DemoAssetLibrary and Kenney CC0 city models.
- Batch 08 (Claude, unverified): one Player rig for desktop and VR; DemoModeSwitch; XRHeadTracking and XRControllerInput through UnityEngine.XR.InputDevices; VR mapping in CONTROLS_AND_INTEGRATION.md.
- Batch 07 (Claude, unverified): DemoGeometry consolidation; SceneTape.Refresh; two-click reset; REHEARSAL.md run sheet; review fixes (released tools settle on the surface beneath, taking a second tool puts the first down, default bounds, XRLocomotion.Relocate).
- Batch 06 (Claude, unverified): ToolHolder base, HandInteractor, XRLocomotion, EvidenceCamera hand-off, DemoStatusBoard, DemoBuild + Build-Windows.cmd/Run-Demo.cmd, Package Manager troubleshooting in SETUP.md.
- Batch 05 review (ChatGPT, verified, 40 assertions): SessionReviewRecorder, panel repair of the broken auto-merge, REVIEW_BATCH_05.md. Its DesktopToolPlacement/carry toggle were superseded by DesktopInteractor in this merge; its review checks now live in DemoValidation.
- Batch 05 (Claude): InteriorBounds, DesktopInteractor, presenter panel, DemoValidation, CreatePlayer; earlier fixes (marker font, walker grounding, build scene).
- Batch 04 (ChatGPT, verified, 33 assertions): expanded shop detail, coloured tools, CaptureFromView, street props. See ENVIRONMENT_BATCH_04.md.
- Batch 03 and earlier: see ENVIRONMENT_BATCH_03.md, TOOLS_BATCH_02.md, ENVIRONMENT_BATCH_01.md, VERIFICATION_2026-10-08.md.
