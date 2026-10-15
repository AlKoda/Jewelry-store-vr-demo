# WallGraphics

Images that make the night store's walls, glass and street look lived-in: nine public-domain
paintings and a gilt frame moulding, the Aurel & Finch poster and lightbox set, shop notices
and stickers, and street-level paper, signs and graffiti. Prepared October 14, 2026, revised
October 15, 2026 (see Revisions at the end).
**Not yet wired into any builder and never imported into Unity: unverified.**
Licences are in LICENSE.txt, and hashes and sources in sources.json. Total size is about 24 MB: 18 MB of
single images and 6.3 MB for the two atlases in Atlas/ (see Atlases (wiring batch)).

All pixel sizes are multiples of 4, and no side is larger than 2048 px. The two atlas pages are powers of
two (2048 x 2048 and 2048 x 1024). Of the single images, only nine are a power of two on both sides (FrameMoulding_Gilt_Color and _Normal, Lightbox_AurelFinchWide,
WindowVinyl_AurelFinch, Sign_PleaseRingBell, Sticker_AlarmMonitored, Sticker_InsurerApproved,
Graffiti_Tags, StreetPosters_Weathered). The other 27 keep the proportions of the real object
(paintings, 2:3 posters, A4/A3 sheets at 724 x 1024, 1024 x 1448 and 2048 x 1448, the 1536 x 1024
notice board). Give each quad the physical size below, not the pixel ratio. Positions use NightStoreLayout coordinates (x across the
facade, z into the shop, front glass at z 0, rear partition at z 8.5).

## Import notes (Unity 6, Built-in pipeline)

- Colour images: sRGB on, mipmaps on, Wrap Clamp, Aniso 4 to 8 (wall text is seen at grazing angles), Max Size 2048.
  Use High Quality compression (BC7) for anything with small text (notices, sheets, newspaper, plan).
  **Non-Power of 2: set None on every non-power-of-two image** (all except the nine listed above). The
  default ToNearest resamples them (724 x 1024 becomes 512 x 1024, 1024 x 1448 becomes 1024 x 1024,
  2048 x 1448 and 2048 x 1352 become 2048 x 1024), which throws away 25 to 30 % of the resolution and
  blurs the small text on the A4 sheets, fire plan, calendar and newspaper. With None the texture keeps
  its exact size; mipmaps still work and BC7/DXT compression still applies because every side is a
  multiple of 4.
- PNGs with alpha: Alpha Is Transparency on. Stickers, vinyl and signs work with Standard Cutout (cutoff 0.5).
  Use Fade only for the tape on Flyer_LostCat and the lamination edge on Notice_PoliceLine. Put decals 2 to 3 mm off the surface with shadows off.
- Lightboxes (Poster_*, Lightbox_AurelFinchWide): Standard with the same texture as Emission,
  intensity 0.3 to 0.6 if the boxes are lit, or no emission so the burgled shop stays dark at night.
- Window vinyl and door stickers face the street. Use a single-sided quad on the outside of the
  glass, plus a second quad flipped in U if the inside view must read too (real vinyl reads mirrored from inside).
- Brass plate: Standard metallic 0.9, smoothness 0.55, with Plate_OpeningHours as Albedo (Cutout for the rounded corners).
- FrameMoulding_Gilt_Normal.png: Texture Type Normal map (OpenGL/green-up, Unity's convention), Wrap Repeat U.
- Everything here is albedo with mild baked micro-shading (engraving, paper waves, frame relief) and no baked scene lighting.

## Artwork (public domain, 2048 px long side, JPG)

| File | Image px | Suggested image size (m) | Suggested placement |
|---|---|---|---|
| Art_Heda_BreakfastWithLobster.jpg | 2044 x 2048 | 0.80 x 0.80 | Showroom rear partition above the rear wall case (x -5.5, y 1.9). Silver and gilt still life suits a jeweller |
| Art_FantinLatour_RosesAndNasturtiums.jpg | 2048 x 1596 | 0.90 x 0.70 | Left wall between wall cases, or the seating corner |
| Art_Lorrain_MorningInTheHarbour.jpg | 2048 x 1568 | 0.92 x 0.70 | Left wall sign frame (golden light against dark walls) |
| Art_Monet_WaterlooBridgeFog.jpg | 2048 x 1312 | 1.00 x 0.64 | Right wall between the wall cases |
| Art_Pissarro_BoulevardMontmartre.jpg | 2048 x 1612 | 0.89 x 0.70 | Seating corner above the side table (x 7.0, z 1.1) |
| Art_Bellotto_NeumarktDresden.jpg | 2048 x 1156 | 1.40 x 0.79 | Above the sofa (x 5.6, z 1.1, facing into the shop) |
| Art_Pereda_StillLifeEbonyChest.jpg | 2048 x 1752 | 0.82 x 0.70 | Office wall behind the desk |
| Art_Aivazovsky_NinthWave.jpg | 2048 x 1352 | 1.06 x 0.70 | Office side wall |
| Art_Robert_LandscapeWithWaterfall.jpg | 936 x 2048 | 0.55 x 1.20 | Narrow wall beside the office opening |
| FrameMoulding_Gilt_Color.jpg + _Normal.png | 1024 x 256 | Moulding 0.08 m wide, U repeats every 0.32 m | Frame bars around the paintings (four mitred strips). The image's top row is the outer edge and its bottom row the sight (inner) edge. Metallic 0.8, smoothness 0.5 |

Hang the centres at 1.55 to 1.65 m. A 2 to 3 cm frame depth and the gilt moulding give
the paintings the look of real frames. These are museum scans, so keep them opaque Standard
(no emission).

## Brand: Aurel & Finch (project-original, ray-traced jewellery)

| File | Px | Physical size (m) | Placement |
|---|---|---|---|
| Poster_SolitaireCollection.jpg | 1024 x 1536 | 0.80 x 1.20 | Lightbox or framed print. Wall sign frames (Sign_Left0..2, Sign_Right0..1) or freestanding window lightbox |
| Poster_TimelessSince1962.jpg | 1024 x 1536 | 0.80 x 1.20 | as above (pearl strand) |
| Poster_ForeverEngraved.jpg | 1024 x 1536 | 0.80 x 1.20 | as above (wedding bands) |
| Poster_MidnightSapphire.jpg | 1024 x 1536 | 0.80 x 1.20 | as above (sapphire halo pendant) |
| Lightbox_AurelFinchWide.jpg | 2048 x 1024 | 2.00 x 1.00 | Above the counter (Sign_Counter, x 5.5, y 2.0 on the rear partition) |
| WindowVinyl_AurelFinch.png | 2048 x 512 RGBA | 1.20 x 0.30 | Gold-leaf lettering on the entrance transom glass or a side bay, centre at 2.1 m |

The posters are dark with gold type and match the existing NightGraphics fascia and the Barlow
interface font. If a forced lightbox has a broken face, place the poster quad behind a cracked glass
pane rather than editing the image.

## Store notices (project-original; all names and numbers fictional)

| File | Px | Physical size (m) | Placement |
|---|---|---|---|
| Plate_OpeningHours.png | 768 x 1024 RGBA | 0.24 x 0.32 | Brushed brass plate, outside on the pier left of the entrance, centre 1.5 m |
| Sign_PleaseRingBell.png | 1024 x 512 RGBA | 0.24 x 0.12 | Black acrylic beside the door at 1.3 m, arrow pointing at the bell push |
| Sign_CCTV.png | 768 x 1024 RGBA | 0.30 x 0.40 | Inside the entrance transom facing out, and/or on the office partition near the camera |
| Sticker_CardsAccepted.png | 768 x 512 RGBA | 0.12 x 0.08 | Entrance door glass at 1.2 m |
| Sticker_AlarmMonitored.png | 512 x 512 RGBA | 0.10 x 0.10 | Door glass top corner and a facade bay corner |
| Sticker_InsurerApproved.png | 512 x 256 RGBA | 0.12 x 0.06 | Door glass below the alarm sticker |
| Sign_BackIn5Minutes.png | 1024 x 768 RGBA | 0.26 x 0.19 (card 0.20 x 0.13) | Hung inside the door glass on its cord, an alternative to the Closed sign |
| Certificate_GemmologyDiplomaFramed.jpg | 1024 x 768 | 0.48 x 0.36 | Frame, mat and glass are already in the image. Office wall above the desk, or behind the counter |
| NoticeBoard_StaffCork.jpg | 1536 x 1024 | 0.90 x 0.60 | Office or staff side of the rear partition, centre 1.5 m. Rota, closing procedure, fire action, memo, holiday planner, post-its and an emergency glazier's card (fictional "Quellbrook Glazing") |
| Sheet_StaffRota.jpg | 724 x 1024 | 0.21 x 0.297 (A4) | Loose on the office desk, a clipboard, or kicked onto the floor |
| Sheet_ClosingProcedure.jpg | 724 x 1024 | 0.21 x 0.297 (A4) | Same. The checklist (stock to the safe by 17.45, dual control) supports the story |
| Calendar_October2026.png | 1024 x 1448 RGBA | 0.30 x 0.42 | Office wall. Days crossed off to 13 October, notes on 2, 9, 15, 16, 21, 25 and 28 October. The alpha hole at the top centre is the hanger |
| FireEvacuationPlan.jpg | 2048 x 1448 | 0.42 x 0.297 (A3, clip frame) | Rear partition just right of the office opening. Its "You are here" (x 1.8, z 8.0) and the drawn plan follow NightStoreLayout: 16 x 12 m shell, office x ±3 behind z 8.5, side rooms, cases, counter, sofa and safes |

## Street level (project-original; CC0 photos and public-domain paintings inside, see LICENSE.txt)

| File | Px | Physical size (m) | Placement |
|---|---|---|---|
| StreetPosters_Weathered.jpg | 2048 x 1024 | 3.00 x 1.50 | Opaque wall panel: brick with layered, torn fly-posters, sun fade, rain streaks and a few tags. Building wall across the street at pavement level (bottom 0.2 m above the far pavement) |
| Graffiti_Tags.png | 2048 x 1024 RGBA | 3.00 x 1.50 | Spray tags, a silver throw-up and drips. Decal on side walls, shutters, a dumpster or the far buildings (Cutout 0.3 or Fade) |
| Sign_NoParking.png | 1024 x 1536 RGBA | 0.70 x 1.05 (disc 0.60 Ø, plate 0.49 x 0.29) | On a 2.6 m pole at the kerb (KerbZ -3), disc centre about 2.3 m. Generic blue/red disc with a time plate |
| Newspaper_FrontPage.jpg | 1024 x 1536 | 0.28 x 0.42 | Fictional evening paper dated Wednesday 14 October 2026. On the counter or office desk, or crumpled on the pavement |
| Flyer_LostCat.png | 768 x 1024 RGBA | 0.23 x 0.30 | Taped to a streetlight pole at 1.5 m (on a curved quad or a flat quad 1 cm off the pole). Ten tear-off tabs read "Mabel 07700 900 482" (all 11 digits); three are torn off |
| Notice_PoliceLine.png | 1024 x 724 RGBA | 0.30 x 0.21 (laminated A4) | Cable-tied to the cordon barrier or the forced door. Generic, no force crest |

## Atlases (wiring batch)

Two atlas pages, built October 15, 2026 by Source~/make_atlas.py from the single images above. With them the
night store's wall graphics need one texture for the interior and one for the street instead of fourteen. The
single images are kept unchanged, so a builder can still use one of them on its own where it needs more
resolution. **Never imported into Unity: unverified.**

- Atlas/NightStoreInterior_Color.png: 2048 x 2048 RGBA (sRGB chunk), 4,723,853 bytes, 11 cells, 80.6 % filled.
- Atlas/NightStoreStreet_Color.png: 2048 x 1024 RGBA, 1,562,918 bytes, 3 cells, 57.0 % filled.
- Atlas/NightStoreInterior.json (944 bytes) and Atlas/NightStoreStreet.json (275 bytes) hold the layouts for
  Unity's JsonUtility: `{width, height, cells: [{name, x, y, w, h}]}`. Values are pixels, with y counted from
  the BOTTOM edge (Unity's UV origin), so a cell's UV rect runs from (x / width, y / height) to
  ((x + w) / width, (y + h) / height). `name` is the stem of the single image, for example `Sign_CCTV`.
  Unity imports .json files as TextAssets.

How they are built: each source is resampled to its cell with Lanczos in sRGB-encoded values (as the other
generators here resize). RGBA sources are resampled with premultiplied alpha, and opaque sources (the JPGs) get
alpha 255. Under alpha 0, the RGB inside an alpha cell is filled from the visible texels; no visible texel
changes. Each cell's edge colour is bled 8 px into the gap at alpha 0. The rest of the unused page takes the
nearby cells' colours, also at alpha 0, so no mip level pulls in black. Cells are 16 px apart and 8 px from the
page edge. Checked from the layouts, with bilinear sampling at a cell's edge:
- Mip levels 0 to 2 never mix two cells.
- Mip 3 never mixes two cells on the street page. On the interior page it mixes them only along the stacked
  plan, lightbox and Pissarro edges (y 732/748 and 1260/1276 from the top, which sit on a multiple of 4 px but
  not of 8).
- From mip 4 up (the 128 px level of a 2048 page), cells 16 px apart can share a texel along their edges.

These levels are used only from far away. UVs need no half-texel inset. No cell is
rotated or cropped. Each cell's aspect is within 0.33 % of its source (Heda 764 x 768 from 2044 x 2048,
Pissarro 768 x 604 from 2048 x 1612; the others are exact).

| Cell (`name`) | Px | JSON x, y (y from bottom) | UV u0, v0, u1, v1 | Physical size (m) | px/m | Material |
|---|---|---|---|---|---|---|
| **NightStoreInterior_Color.png** | | | | | | |
| FireEvacuationPlan | 1024 x 724 | 8, 1316 | 0.00391, 0.64258, 0.50391, 0.99609 | 0.42 x 0.297 | 2438 | Opaque |
| Art_Heda_BreakfastWithLobster | 764 x 768 | 1048, 1272 | 0.51172, 0.62109, 0.88477, 0.99609 | 0.80 x 0.80 | 955 | Opaque |
| Lightbox_AurelFinchWide | 1024 x 512 | 8, 788 | 0.00391, 0.38477, 0.50391, 0.63477 | 2.00 x 1.00 | 512 | Opaque; Emission = atlas if lit |
| NoticeBoard_StaffCork | 768 x 512 | 1048, 744 | 0.51172, 0.36328, 0.88672, 0.61328 | 0.90 x 0.60 | 853 | Opaque |
| Art_Pissarro_BoulevardMontmartre | 768 x 604 | 8, 168 | 0.00391, 0.08203, 0.37891, 0.37695 | 0.89 x 0.70 | 863 | Opaque |
| Certificate_GemmologyDiplomaFramed | 512 x 384 | 792, 344 | 0.38672, 0.16797, 0.63672, 0.35547 | 0.48 x 0.36 | 1067 | Opaque |
| Calendar_October2026 | 384 x 543 | 1320, 185 | 0.64453, 0.09033, 0.83203, 0.35547 | 0.30 x 0.42 | 1280 | Cutout 0.5 (hanger hole) |
| Sign_CCTV | 288 x 384 | 1720, 344 | 0.83984, 0.16797, 0.98047, 0.35547 | 0.30 x 0.40 | 960 | Cutout 0.5 |
| Sticker_AlarmMonitored | 256 x 256 | 792, 72 | 0.38672, 0.03516, 0.51172, 0.16016 | 0.10 x 0.10 | 2560 | Cutout 0.5 |
| Sticker_CardsAccepted | 288 x 192 | 1720, 136 | 0.83984, 0.06641, 0.98047, 0.16016 | 0.12 x 0.08 | 2400 | Cutout 0.5 |
| Sticker_InsurerApproved | 256 x 128 | 8, 24 | 0.00391, 0.01172, 0.12891, 0.07422 | 0.12 x 0.06 | 2133 | Cutout 0.5 |
| **NightStoreStreet_Color.png** | | | | | | |
| StreetPosters_Weathered | 1008 x 504 | 8, 512 | 0.00391, 0.50000, 0.49609, 0.99219 | 3.00 x 1.50 | 336 | Opaque |
| Graffiti_Tags | 1008 x 504 | 1032, 512 | 0.50391, 0.50000, 0.99609, 0.99219 | 3.00 x 1.50 | 336 | Cutout 0.3 or Fade |
| Flyer_LostCat | 366 x 488 | 8, 8 | 0.00391, 0.00781, 0.18262, 0.48438 | 0.23 x 0.30 | 1591 | Fade (tape), else Cutout 0.5 |

Physical sizes and placements are the ones in the tables above. Give each quad that size, not the pixel ratio
(Heda is 0.80 x 0.80 m, the calendar 0.30 x 0.42 m).

Import (Unity 6, Built-in; unverified): Default texture, sRGB on, Alpha Source Input Texture Alpha, Alpha Is
Transparency on, mipmaps on, Wrap Clamp, Trilinear, Aniso 8, Max Size 2048. Keep the interior page at 2048.
Max Size 1024 would halve every cell and take the plan's 6 px footer down to 3 px. Both pages are powers of
two, so the Non-Power of 2 setting does not apply.
- PC (BC7, High Quality): the interior page uses 4 MiB (5.33 MiB with mips) and the street page 2 MiB (2.67 MiB).
  For comparison, the eleven interior single images at their own sizes are 18.0 M texels, which is 17.1 MiB
  (22.8 MiB with mips), and the three street images are 4.75 MiB (6.33 MiB with mips).
- Quest 2 (ASTC): use 4x4 for the interior page (5.33 MiB with mips) because of its small text. 5x5 (3.42 MiB)
  is fine only if the plan's footer still reads in the headset. Use 6x6 for the street page (1.19 MiB with mips).
- Materials: the quads on one page share at most three materials, and quads with the same material batch.
  - Interior: Standard Opaque (paintings, plan, notice board, certificate), Standard Cutout (calendar, CCTV
    sign, stickers) and, if the lightbox is lit, Standard with the atlas as Emission.
  - Street: Standard Opaque (posters), Standard Cutout (graffiti) and Standard Fade (the flyer's tape).

Legibility at the atlas size, checked October 15 with 100 % crops:
- The fire evacuation plan reads completely, down to its 6 px footer line.
- These also read: the calendar's dates and handwritten notes, the CCTV sign's small print, all three
  stickers, the diploma text, the lightbox's bottom line and the phone numbers on the flyer's tabs.
- On the notice board, the headings, memo, Fire Action text, post-its, holiday planner and glazier's card read.
  **The body text of its A4 sheets does not**: the rota's shift times and the closing-procedure checklist are
  3 to 4 px tall at 768 x 512. Where a guest should read them up close, use Store/NoticeBoard_StaffCork.jpg
  (1536 x 1024, 1707 px/m) on its own, or the loose Store/Sheet_StaffRota.jpg and Sheet_ClosingProcedure.jpg.

## Regenerating

Source~/ holds the generators; Unity ignores the folder. Run them from a folder that holds the
scripts, with fonts/gfonts (a sparse checkout of github.com/google/fonts) next to that folder:
`render_jewels.py <out> 3`, then `posters.py <renders> Brand`, `store_a.py Store`,
`store_b.py Store <PoppyField.jpg>`, `street.py Street Artwork <skimage data dir>`, `frame.py Artwork`,
and `prep_paintings.py <hermitage files> Artwork`. The final hero renders take about 10 minutes on 4 cores.
The atlases need only numpy and Pillow. From the repository root, run a copy of the script that lives outside
the pack: `cp Prototype/ThirdParty/WallGraphics/Source~/make_atlas.py /tmp/ && python3 -I /tmp/make_atlas.py
Prototype/ThirdParty/WallGraphics` (add `--crops <dir>` to get 100 % crops of every cell). The script checks the
layouts and source hashes before writing anything and exits 1 naming the cell when a check fails. It then writes
Atlas/ and replaces the Atlas/ entries in sources.json. It runs in about 15 s, and a rerun with the same
numpy/Pillow reproduces the same bytes.

## Revisions

October 15, 2026, after an independent review:
- NoticeBoard_StaffCork.jpg: the glazier's business card read "Clearview Glazing", which is the name
  of real UK businesses. It now reads the invented "Quellbrook Glazing", and its pin sits in the card's
  blank corner so the name is legible. Nothing else on the board changed.
- Flyer_LostCat.png: the tab phone number was cut at the perforation ("07700 900 48"). It is now set
  at 12 px so all 11 digits fit, and the two adjacent torn tabs no longer leave a paper sliver. Toner
  specks are kept 4 px clear of the tab text, so no speck reads as a dot inside the number.
- Import notes: Non-Power of 2 must be None (see above), not the default ToNearest.
- The rebuilt files come from the same seeded scripts in Source~; every other image is byte-identical
  to the October 14 build.
- Atlases (wiring batch, later the same day): Atlas/ holds two atlas pages and their JsonUtility layouts, built
  by the new Source~/make_atlas.py from fourteen of the images (see Atlases (wiring batch)). sources.json gained
  four Atlas/ entries, each with derived_from (file, sha256, rect) and tool. LICENSE.txt gained section 4. No
  existing image changed.
- Still to do outside this folder, by whoever integrates the pack: add the WallGraphics rows to
  Docs/ASSET_REGISTER.md (public-domain paintings, CC0 scikit-image photos inside the flyer and
  newspaper, project-original graphics), remove /Prototype/ThirdParty/WallGraphics/ from
  .git/info/exclude once the pack is accepted, update HANDOFF.md and ROADMAP.md, and add .meta files
  when the pack goes into the snapshot zip.
- Status of that list at the atlas batch (October 15): Docs/ASSET_REGISTER.md has the three WallGraphics
  rows, and the pack is tracked in git and no longer in .git/info/exclude. Still open outside this folder:
  name the Atlas/ files in those rows and in HANDOFF.md and ROADMAP.md, and add .meta files (the two PNGs
  with the import settings above; the JSON files import as TextAssets).
