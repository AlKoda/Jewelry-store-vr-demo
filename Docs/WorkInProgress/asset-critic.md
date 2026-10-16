**Completeness critique of the realism asset sweep. Inspected read-only; no project file was changed.**

**Precondition (blocks everything below).** None of the 12 new packs is used in any scene, and none has been imported into Unity. The night store still renders on:
- the ReferenceTextures placeholders, which StoreKit.TexturePack loads first. They are near-flat swatches: FloorTile std 6.2 sRGB levels, StoneTop 3.1, Walnut 2.8.
- the KayKit police car, streetlights and cartoon towers.
- the 900-triangle StoreKit fixtures.

The 12 packs (262 MB) are hidden by .git/info/exclude and exist only in this container. The other assistant cannot see them, and they are lost if the container resets. Load order matters: PBRInterior reuses NightMaterials/ReferenceTextures names (FloorTile, StoneTop, Walnut, SafeSteel, PlasterWarm, VelvetCharcoal, SuedeCream, BronzeBrushed, CeilingPaint), and PBRExterior reuses FacadeStone, AsphaltWet and PavementSlab. Its colour maps are .jpg, but StoreKit looks for _Color.png. If it falls through map by map, it will pair one pack's colour map with another pack's smoothness map.

## A. Realism gaps, highest impact first

1. **The main fixtures are crude.** Display cases and wall cases are 900 triangles, safes 800–900, and the storefront, window modules and counter 300–700 (StoreKit.cs Budgets). Guests stand at arm's length from them. They also have no override hook, so a better model cannot replace them. **Route: procedural** (a much more detailed StoreKit generator: bevelled aluminium profiles, 10–12 mm glass with a green edge, silicone beads, lock cylinders, LED strips, PBRInterior deck fabrics; safe bolt-work, hinges, handle wheel, shelves). Tripo, as in TRIPO_GUIDE, is not a reachable channel and needs a paid plan.
2. **The police details mix countries.** The open "country" decision in "for Koda.md" blocks this. Against a £/UK-style store, the packs contain:
   - a 1980s US sedan in black-and-white livery, and red/blue light bars (both the procedural bar and PoliceLightBar)
   - a holstered pistol on the uniformed officer and a US shield badge on the detective
   - one North American siren
   - "CRIME SCENE – DO NOT CROSS" tape (DemoToolsBuilder) and a "POLICE / DO NOT CROSS" barrier
   - German signs and a Polizei car in the HDRIs

   A police-training audience will notice. **Route: procedural.** Paint a livery onto StreetModels/Hatchback (its Paint UVs cover 0–1; check for overlaps) and use a blue-only PoliceLightBar. Recolour the Rocketbox uniforms (hi-vis) and delete the pistol part. Change the tape text. Synthesize the siren, or take a CC0 recording through the GitHub mirrors.
3. **The arrival has no light-bar wash and no glow.** LightBarBlink only changes emission, and casts no light. NightQuality sets `allowHDR = false` and there is no post-processing, so lamps, the light bar and the exit sign get no halo. **Route: procedural.** Animated additive wash quads or a Projector on the facade and wet road, or 1–2 vertex-lit "Not Important" lights. A billboard-halo component can use the existing NightSky/SurfaceDecals glow sprites. keijiro/KinoBloom (MIT, GitHub) is an option, but it is untested in single-pass VR and costs GPU time.
4. **The wet street will look flat.** Built-in has no screen-space or planar reflections. **Route: procedural** stretched lamp-reflection billboards on the asphalt and puddles (PuddleMask, AsphaltWet) plus a box-projected exterior reflection probe.
5. **The street context is thin.** The shop is one storey (parapet 3.6 m). The neighbours on either side are 7.5 m render boxes with flat window quads, and the KayKit cartoon towers behind clash with the photoscans. **Route: procedural** facades with recessed windows (PBRExterior Brick/Render plus ApartmentBlock window frames and interior images) and an interior-mapping shader for lit rooms. Put RollerShutterWindow and graffiti on neighbouring shopfronts. Use the Zhabotinsky low-poly buildings (1024 px colour, no normal maps) only for the far row.
6. **The glass breaks the wrong way for a jeweller.** There is no laminated (anti-bandit) pane state: holed, a sagging crack web held by the interlayer, little scatter. What exists is annealed daggers, tempered crumbs and a spider-web decal. **Route: procedural** fracture mesh plus GlassCrackDecal. It needs the entry decision (#2 in "for Koda.md").
7. **Gems will read as plain glass.** They are 110-triangle meshes on Standard Fade: no dispersion or internal reflection, and every gem is a sorted transparent object. **Route: procedural** opaque fake-refraction gem shader (cubemap, facet normals, RGB-offset sparkle), or an MIT GitHub shader after a licence check.
8. **The case dressing is thin, and the story conflicts with it.** Slotted ring trays, bracelet T-bars and ramps, watch C-cuffs, acrylic risers, necklace easels and the counter mirror exist only as 300–450-triangle StoreKitProps. Also, Sheet_ClosingProcedure moves stock to the safe by 17:45, so at night the cases should show bare pads, not dust-film outlines of lifted items. **Route: procedural.**
9. **Fabrics and small metals will look wrong in VR.** Velvet, suede and leather have no sheen in Standard. Tiny high-smoothness metals (Chrome 0.94–0.97, Brass ~0.91, gold) will shimmer with only 4x MSAA. **Route: procedural** shader (a sheen term, plus geometric specular anti-aliasing or a smoothness clamp on small items).
10. **The torch beam is invisible.** The parts exist (LightShaft_Gradient, DustMotes_Atlas), but there is no beam-cone mesh, no soft additive depth-fade shader and no dust particles. **Route: procedural** (Particle System is core).
11. **The people are frozen.** The poses are baked static meshes, which look uncanny within about 2 m, and they carry the country errors in #2. **Route: GitHub** (Microsoft Rocketbox rigged FBX plus idle clips, MIT, through the sparse clone already used; Mecanim is core). Downsample the ~100 MB texture set per avatar. Otherwise keep figures outside or at a distance.
12. **The player has no hands.** VR shows only controller models. **Route:** a Rocketbox hand mesh (GitHub, MIT) recoloured as blue nitrile, with two poses; procedural fallback.
13. **Alarm and security hardware is missing.** There is no facade bell box with strobe, although the alarm audio is written for one. There are also no PIR detectors, door contacts or interior roller grille. **Route: procedural** (CC0 roller-shutter models already exist).
14. **Forensic procedure props are missing.** For example: stepping plates for the common approach path, overshoes, lift cards and tape, swab tubes, exhibit labels and an evidence-tape roll. **Route: procedural.**
15. **Architectural trim is missing.** Skirting, shadow gap or cornice, architraves, sockets and switches, and cut CCTV/DVR cables. **Route: procedural.**
16. **Minor exterior items are missing.** No street trees (Poly Haven trees are 30k+ triangles): procedural with bpy Sapling, or npm @dgreenheck/ez-tree (MIT, check), or billboards. No modern LED street column (StreetLamp is a heritage lantern, but its cookie is an LED batwing): procedural. No CSI van or ambulance: Tripo, or leave it out.
17. **Some textures are still weak.**
    - PBRExterior FacadeStone and RustyMetal normal maps are nearly flat (measured tilt mean 0.58° and 0.62°, p99 1.6° and 2.6°). **Route: GitHub**: re-fetch the ambientCG NormalGL maps for Travertine001 and Metal053C from the same mirrors.
    - SafeSteel colour is almost uniform (std 1.8 sRGB levels) with no edge wear. **Route: procedural** wear masks.
    - The Concrete, Granite and PavementSlab tile sizes are estimates.
18. **Audio gaps.** Synthetic radio babble (trainees may notice it), one North American siren, dog barks 01–03 (CC BY 3.0, licence evidence conflicting), no carpet footsteps (if Carpet is used), and no reverb zones. **Route: procedural** (siren synthesis, carpet steps, AudioReverbZone); use DogBarkDistant_04 alone if the CC BY credit cannot be shown.
19. **Duplicates and identifiable places.**
    - Pick one set per role: NightSky and SurfaceDecals both ship flashlight/UV/flood cookies and flare sprites.
    - Use zwinger_night (Dresden landmark), cobblestone (readable signs) and hansaplatz (Polizei car) only as blurred reflection cubemaps. Use the procedural city_night_sky_* files as the skybox.

## B. Performance risks

1. **Triangles.** The new packs total about 651k triangles (Interior 181k, Jewellery 144k, Forensic 133k, Street 128k, People 65k). NightStoreValidation.TriangleBudget is 220k for the whole scene.
   - Jewellery items are 5–9k triangles each, so a case of 12 rings is about 70k.
   - The cars are 15.6–22.5k, ApartmentBlock 24k, DeskLampArm and the potted plants about 15k, and Khronos SheenChair 40k if it is used.
   - No pack has LODs. **Route: procedural** LODs with bpy decimate or npm meshoptimizer/gltfpack; LODGroup is core.
2. **Transparency and materials.** The budgets are TransparentBudget 70 and MaterialBudget 96.
   - The packs bring about 219 material names (more once each texture gets its own material) and 22 transparent sub-materials.
   - On top of that come the Fade decals, GlassSmudge overlays and fingerprint quads stacked on the case glass, which also means stereo overdraw.
   - **Route:** an opaque gem shader, cutout and atlased decals, and shared metal, gem and fabric materials (JewelryModels materials.json already names them).
3. **Lightmaps.** The budget is 8 maps of 1024 px. ThirdPartyModelImport gives every model lightmap UVs, so small props must be probe-lit, or texel density collapses.
4. **Realtime lights.** There are 4 pixel lights, already shared by the flashlight, the UV torch and two lamps. Light-bar or streetlight realtime lights would go over, so use vertex lights or decals.
5. **Texture memory and import settings.**
   - Estimated GPU memory at native size is about 460 MB. Some maps are larger than needed: Wallpaper is 2048 px for a 0.5 m tile, and the 10 People 2K maps are for figures seen beyond 2 m. Cap them at 1024.
   - WallGraphics has 27 non-power-of-two images that need Non-Power of 2 = None.
   - No pack ships .meta files, and only a model import script exists. Cutout, Alpha Is Transparency, preserve coverage, aniso, cookie and NPOT settings need a texture AssetPostprocessor keyed on file suffix.

## C. Licence and process risks

1. **The snapshot zip will be too big for GitHub.** The Glass zip is 58.9 MB; adding the 262 MB of packs makes about 300 MB. GitHub rejects files over 100 MB, there is no LFS, and .git is already 787 MB. Split the zips per pack, leave Source~ out, or use release assets.
2. **The in-app credits are out of date.** DemoDesktopPanel.Credits lists only Kenney, KayKit, Khronos, cgbookcase, HDRI Haven and Barlow. It needs:
   - Daniel Zhabotinsky (CC BY 4.0: PoliceCar, Hatchback, SedanClassic, 4 buildings)
   - Qifan Zhang, Softmind Game Factory and DineshThennarasan (CC BY 4.0 jewellery)
   - Kinoton (CC BY 3.0), or drop dog barks 01–03
   - the Microsoft Rocketbox MIT notice

   DemoBuild.CopyLicenses copies only files named LICENSE.txt, so NightSoundscape/Licenses/*.txt is skipped.
3. **Licence evidence is second-hand.** ambientCG, Poly Haven, Sketchfab and Freesound do not resolve from here, so the evidence rests on GitHub mirrors and API dumps. The user should spot-check the source URLs in a normal browser before October 27.
4. **Bookkeeping.** All 12 packs lack Docs/ASSET_REGISTER.md rows and are still listed in .git/info/exclude.

The two measurement scripts are in /tmp/claude-0/-home-user-Jewelry-store-vr-demo/cc2183ba-645a-5bd8-af98-25ffcc190d06/scratchpad/critic.