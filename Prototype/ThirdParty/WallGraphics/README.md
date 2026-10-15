# WallGraphics

Images that make the night store's walls, glass and street look lived-in: nine public-domain
paintings and a gilt frame moulding, the Aurel & Finch poster and lightbox set, shop notices
and stickers, and street-level paper, signs and graffiti. Prepared October 14, 2026, revised
October 15, 2026 (see Revisions at the end).
**Not yet wired into any builder and never imported into Unity: unverified.**
Licences are in LICENSE.txt, and hashes and sources in sources.json. Total size is about 18 MB.

All pixel sizes are multiples of 4, and no side is larger than 2048 px. Only nine images are a power
of two on both sides (FrameMoulding_Gilt_Color and _Normal, Lightbox_AurelFinchWide,
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

## Regenerating

Source~/ holds the generators; Unity ignores the folder. Run them from a folder that holds the
scripts, with fonts/gfonts (a sparse checkout of github.com/google/fonts) next to that folder:
`render_jewels.py <out> 3`, then `posters.py <renders> Brand`, `store_a.py Store`,
`store_b.py Store <PoppyField.jpg>`, `street.py Street Artwork <skimage data dir>`, `frame.py Artwork`,
and `prep_paintings.py <hermitage files> Artwork`. The final hero renders take about 10 minutes on 4 cores.

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
- Still to do outside this folder, by whoever integrates the pack: add the WallGraphics rows to
  Docs/ASSET_REGISTER.md (public-domain paintings, CC0 scikit-image photos inside the flyer and
  newspaper, project-original graphics), remove /Prototype/ThirdParty/WallGraphics/ from
  .git/info/exclude once the pack is accepted, update HANDOFF.md and ROADMAP.md, and add .meta files
  when the pack goes into the snapshot zip.
