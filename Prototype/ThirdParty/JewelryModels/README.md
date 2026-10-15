# JewelryModels

Realistic jewellery-shop merchandise, displays, packaging, bullion and counter equipment for the night jewellery store
(47 OBJ models, 143,931 triangles in total). Most models are original, procedurally generated at true size from
the scripts in `Source~/` (project-owned); four are third-party (one CC0, three CC BY 4.0, see `LICENSE.txt`).
`Source~/Preview.jpg` shows every model. Nothing here has been imported into Unity yet: **unverified in Unity**.

## Conventions

- OBJ + MTL, **metres**, **Y up**, **front = +Z** (authored like glTF; Unity's OBJ importer mirrors X so the model reads the same).
- Pivot: bottom centre (lowest point at y = 0, centred in x/z) unless the notes say "same transform as ...".
- Rings: finger axis along **X**, stone up (+Y). Rotate 90 deg about Z to slide a ring on a cone, 90 deg about Y for a ring box slot.
- Procedural models carry explicit normals (`vn`): set *Normals = Import* (the default) so faceted gems stay flat-shaded and
  metal stays smooth. The four converted third-party models have no normals: let Unity *Calculate* them (smoothing angle 60).
- MTL `Kd` is written as an **sRGB** colour (what Unity's colour picker shows); `materials.json` gives the recommended Built-in
  Standard values (albedo, metallic, smoothness, rendering mode, emission and which maps to add) for every material name.
  Unity's OBJ import only wires `map_Kd`; normal, metallic-smoothness and emission maps must be assigned in code
  (`DemoAssetLibrary.NormalMap` / `DataMap`) or by hand.
- Transparent materials (Diamond, Sapphire, Ruby, Emerald, Glass, Acrylic) are written with `d < 1`, but the importer makes
  opaque Standard materials: switch them to Transparent (or a gem shader) when wiring.
- Small jewellery should not be lightmap-static: light it with light probes and a reflection probe in each case
  (same rule as `StoreKitProps.ProbeLitParts`). Add one box collider per item with `DemoAssetLibrary.AddBoundsCollider`.
- Triangle budget: jewellery 1-8k (the two pendant necklaces are 8.8k because of their ~140 modelled chain links and
  the two pearl strands 8.5k because of their modelled silk knots), stands and packaging under 3k.

## Paired transforms

| Pair | How to place |
|---|---|
| `NecklaceBust` + `NecklacePearlDraped` / `NecklacePendantDraped` | Same position, rotation and scale; the necklaces were generated on the bust surface. |
| `WatchPillow` + `WatchDressSteel` | Same transform. The case back rests 0.5 mm into the crown of the cushion, the strap hugs the cushion (0.2 mm clearance) and its tails end flat under the cushion's bottom edges, so the pair stands on y = 0 with nothing below it. |
| Any ring + `RingCone` | All six rings have a 17.1 mm inner diameter. Rotate the ring 90 deg about Z (axis vertical) and put its hole centre on the cone axis at y = 0.0665 m above the cone base. Hole centres in model space: (0, 0.0102-0.0111, 0) for the procedural rings (see each ring's notes), (0, 0.0121, 0.0014) for `RingAntiqueTigerEye`. |
| Any ring + `RingBoxOpen` | Rotate the ring 90 deg about Y (axis along Z); hole centre at about (0, 0.034, 0) in box space (same hole-centre offsets as above). |
| `EarringsGoldHoops`, `EarringPearlDrop`, `EarringGoldLeaf` + `EarringStand` | Bar axis X at y = 0.124 m; hang the top of the earring on the bar. |
| `PriceTag01-04`, `PriceStand01-04` | One shared atlas `PriceTags_Color.png` (4 x 2 cells); each OBJ already points at its cell. |

## Models

| File | Kind | What it is | Size W x H x D (m) | Tris | Materials / maps | Origin / licence | Suggested placement in the night store | Notes |
|---|---|---|---|---|---|---|---|---|
| `RingSolitaireDiamond.obj` | Ring | Platinum solitaire: 1 ct round brilliant (6.5 mm, 57 facets at Tolkowsky-like proportions) in a 4-claw basket with two gallery rails on a tapered comfort-fit shank with cathedral shoulders. About US size 6.75 (17.1 mm inner diameter). | 0.0065 x 0.0249 x 0.0206 | 5454 | Platinum, Diamond | original, project-owned | Central case on RingCone or in RingBoxOpen; one in the robbed bay as an overlooked piece. | Finger axis = X, stone up. Hole centre at y = 0.0103 m. In RingBoxOpen rotate 90 deg about Y and place the hole centre at about (0, 0.034, 0); on RingCone rotate 90 deg about Z, hole centre at y = 0.0665 m. |
| `RingGoldBand.obj` | Ring | 18k yellow-gold comfort-fit wedding band, 4.0 mm wide, 1.8 mm thick. | 0.0040 x 0.0207 x 0.0207 | 3456 | Gold18k | original, project-owned | Ring trays, or dropped on the floor near a broken bay (evidence marker). | Finger axis X, hole centre at y = 0.0103 m; lying flat: rotate 90 deg about Z. |
| `RingSapphireHalo.obj` | Ring | Platinum cathedral halo ring: 8 x 6 mm oval sapphire (north-south) in four claws that run down into the shank, with an under-gallery rail; halo of 18 single-cut 1.3 mm diamonds with shared beads on a frame whose tapered gallery skirt rests on raised cathedral shoulders (the head is one connected piece with the shank). | 0.0113 x 0.0240 x 0.0205 | 7442 | Platinum, Sapphire, Diamond | original, project-owned | Central case, coloured-stone tray. | Finger axis X. Hole centre at y = 0.0102 m. |
| `RingDiamondEternity.obj` | Ring | Platinum full eternity band, 3 mm, 22 x 2.0 mm single-cut diamonds with shared bead claws. | 0.0030 x 0.0221 x 0.0219 | 6116 | Platinum, Diamond | original, project-owned | Bridal tray, wall case. | Finger axis X. Hole centre at y = 0.0111 m. |
| `RingRubyThreeStone.obj` | Ring | 18k yellow-gold three-stone ring: 6 mm round ruby between two 4.5 mm diamonds, set along the band. | 0.0060 x 0.0247 x 0.0207 | 7402 | Gold18k, Ruby, Diamond | original, project-owned | Central case or side island. | Finger axis X. Hole centre at y = 0.0104 m. |
| `RingAntiqueTigerEye.obj` | Ring | Large antique-style signet ring with a cabochon tiger-eye and engraved, gem-set shoulders (textured, third-party CC BY); 17.1 mm inner diameter like the procedural rings. | 0.0240 x 0.0297 x 0.0263 | 7000 | RingAntiqueTigerEye; maps: RingAntiqueTigerEye_Color.jpg, RingAntiqueTigerEye_MetallicSmoothness.png, RingAntiqueTigerEye_Normal.png | CC BY 4.0 - Qifan Zhang | Estate/antique tray, rear office repair tray. | Finger axis X, stone up. Hole centre at (0, 0.0121, 0.0014) m in model space (the other rings: (0, about 0.0103, 0)); use this offset on RingCone / in RingBoxOpen. Uses Color/Normal/MetallicSmoothness maps. |
| `EarringsDiamondStuds.obj` | Earrings | Pair of 0.5 ct diamond studs (5.1 mm brilliants in 4-claw baskets whose claws and post are soldered to a small seat under the culet) with posts and butterfly backs. | 0.0220 x 0.0051 x 0.0147 | 5044 | Diamond, Platinum | original, project-owned | Earring box, EarringStand card, central case. | Stones face +Z, posts point back (-Z); 16 mm apart. |
| `EarringsGoldHoops.obj` | Earrings | Pair of 18k hinged hoops, 24 mm diameter, 2.4 mm tube tapering at the hinge. | 0.0564 x 0.0259 x 0.0024 | 3616 | Gold18k | original, project-owned | Hang on EarringStand bar (bar at y = 0.124 m). | Hoop plane XY (facing +Z). |
| `EarringPearlDrop.obj` | Earrings | Single pearl drop earring: pearl cluster top with gem surround, link and 13 mm baroque drop (textured, third-party CC BY). | 0.0149 x 0.0400 x 0.0075 | 7000 | EarringPearlDrop; maps: EarringPearlDrop_Color.jpg, EarringPearlDrop_MetallicSmoothness.png, EarringPearlDrop_Normal.png | CC BY 4.0 - Softmind Game Factory | EarringStand, necklace bust display, wall case. | Hanging (top at y = 0.040 m). Duplicate and mirror (scale X = -1) for a pair. Metallic-smoothness map: gold metallic 1, pearls 0, smoothness 0.71. |
| `EarringGoldLeaf.obj` | Earrings | Single gold filigree leaf earring with stud, 50 mm long (third-party CC BY). | 0.0194 x 0.0500 x 0.0154 | 6998 | Gold18k | CC BY 4.0 - DineshThennarasan | EarringStand, wall case. | Hanging, front +Z. Mirror for a pair. |
| `NecklaceBust.obj` | Display | Velvet-covered neck form (jewellery bust) with sloping shoulders on a 10 mm walnut plinth, 326 mm tall. | 0.2500 x 0.3260 x 0.1300 | 2712 | VelvetBlack, Walnut | original, project-owned | Wall cases and window bays; one knocked over in a broken bay. | Front +Z. The *Draped necklaces use exactly the same transform. |
| `NecklacePearlDraped.obj` | Necklace | Hand-knotted Akoya pearl strand, 45 cm: 58 pearls of about 7 mm with a silk knot in every gap and a fluted gold box clasp resting on the back of the neck, draped on NecklaceBust. | 0.1673 x 0.0865 x 0.1067 | 8484 | Pearl, Gold18k, Cord | original, project-owned | On NecklaceBust (same position and rotation). | Not grounded: authored in the NecklaceBust frame. |
| `NecklacePendantDraped.obj` | Necklace | 18k cable chain (4.4 mm links, 45 cm) with a 1 ct round diamond pendant on a bail, draped on NecklaceBust. | 0.1644 x 0.0911 x 0.1067 | 8846 | Gold18k, Diamond | original, project-owned | On NecklaceBust (same transform). | Not grounded: authored in the NecklaceBust frame. |
| `NecklacePearlDropped.obj` | Necklace | The same knotted pearl strand, clasp undone, lying in an irregular curve on the floor (dropped during the burglary). | 0.2192 x 0.0070 x 0.0968 | 8460 | Pearl, Gold18k, Cord | original, project-owned | Floor below a smashed bay, next to an evidence marker. | Lies on y = 0. |
| `NecklacePendantDropped.obj` | Necklace | The gold chain and diamond pendant lying on the floor in an irregular curve. | 0.2319 x 0.0053 x 0.0809 | 8846 | Gold18k, Diamond | original, project-owned | Floor or counter top near the forced door / broken case. | Lies on y = 0. |
| `BraceletTennisDiamond.obj` | Bracelet | Platinum tennis bracelet, 54 x 3 mm single-cut diamonds in box settings, laid as a flat oval, with box clasp. | 0.0756 x 0.0026 x 0.0592 | 5832 | Diamond, Platinum | original, project-owned | Bracelet tray in the central case. | Lying flat. |
| `BangleGold.obj` | Bracelet | 18k yellow-gold round bangle, 62 mm inner diameter, 5 x 2.6 mm D section. | 0.0672 x 0.0050 x 0.0672 | 3072 | Gold18k | original, project-owned | Tray, or rolled under a case. | Lying flat (axis vertical). |
| `WatchDressSteel.obj` | Watch | 39 mm stainless dress watch: polished case, domed crystal, navy sunburst dial (applied indices, date "14", fictional Aurel & Finch signature), dauphine hands at 10:09, crown, lugs 20 mm apart with spring bars, and a padded brown leather strap that wraps WatchPillow tightly and tucks under it (the buckle is under the cushion and not modelled). | 0.0417 x 0.0559 x 0.0683 | 7352 | SteelPolished, WatchDial, Glass, Gold18k, LeatherBrown; maps: WatchDial_Color.png | original, project-owned | Watch case on WatchPillow. | Same transform as WatchPillow: the case back rests 0.5 mm into the crown of the cushion, the strap follows the cushion 0.2 mm off its surface and its tails end flat on the shelf under the cushion edge; nothing goes below y = 0. The strap is shaped for the pillow, so use the watch with WatchPillow only. Glass material is transparent (alpha 0.2). |
| `WatchPillow.obj` | Display | C-section black leather watch cushion, 78 x 62 x 48 mm, flat bottom. | 0.0790 x 0.0475 x 0.0620 | 2112 | LeatherBlack | original, project-owned | Watch wall case / counter. | Place WatchDressSteel at the same transform (the pair stands flat on y = 0). |
| `PocketWatchVintage.obj` | Watch | Vintage engraved gold pocket watch with enamel Roman dial and hands (Poly Haven, CC0), lying face up, bow away from the viewer. | 0.0613 x 0.0136 x 0.0799 | 8033 | PocketWatchVintage, Glass, BlackMetal; maps: PocketWatchVintage_Color.jpg, PocketWatchVintage_MetallicSmoothness.png, PocketWatchVintage_Normal.png | CC0 - Poly Haven / Tal Swicegood | Antique case, rear office repair tray, open safe. | Uses Color/Normal/MetallicSmoothness maps; Glass alpha 0.2. |
| `GemRoundBrilliant.obj` | Loose gem | 1 ct round brilliant diamond, 6.5 mm, 57 facets (crown 34.5 deg, pavilion 40.75 deg, table 57%). | 0.0065 x 0.0039 x 0.0065 | 110 | Diamond | original, project-owned | Gem tray, tweezers, GemParcel; scattered on the floor as evidence. | Table up, culet at y = 0. |
| `GemPrincessCut.obj` | Loose gem | Princess-cut diamond, 5.5 mm square. | 0.0055 x 0.0039 x 0.0055 | 102 | Diamond | original, project-owned | Gem tray. | Table up. |
| `GemEmeraldCut.obj` | Loose gem | Emerald-cut emerald, 9 x 7 mm step cut with cut corners. | 0.0070 x 0.0043 x 0.0090 | 96 | Emerald | original, project-owned | Coloured-stone tray. | Table up. |
| `GemOvalSapphire.obj` | Loose gem | Oval brilliant sapphire, 8 x 6 mm. | 0.0060 x 0.0036 x 0.0080 | 110 | Sapphire | original, project-owned | Coloured-stone tray. | Table up. |
| `GemCushionRuby.obj` | Loose gem | Cushion-cut ruby, about 6 mm. | 0.0058 x 0.0039 x 0.0058 | 110 | Ruby | original, project-owned | Coloured-stone tray. | Table up. |
| `GemPearDiamond.obj` | Loose gem | Pear brilliant diamond, about 8 x 6 mm. | 0.0060 x 0.0036 x 0.0082 | 110 | Diamond | original, project-owned | Gem tray. | Table up. |
| `GemParcel.obj` | Loose gem | Opened diamond paper (white outside, blue liner, printed lot label) with 12 loose brilliants of 2.6-4.2 mm resting on the liner (table down or tipped on the pavilion). | 0.1100 x 0.0071 x 0.0720 | 2640 | GemPaper, Diamond; maps: GemPaper_Color.png | original, project-owned | Rear office desk, open safe, counter (abandoned by the burglars). | Lying flat. |
| `RingCone.obj` | Display | Black velvet ring finger/cone on a walnut disc, 90 mm tall. | 0.0500 x 0.0904 x 0.0500 | 1200 | VelvetBlack, Walnut | original, project-owned | Central cases; rings slide on with axis vertical. | Ring hole centre for a 17.1 mm ring at y = 0.0665 m. |
| `EarringStand.obj` | Display | Polished brass T-bar earring stand, 128 mm tall, 130 mm bar. | 0.1396 x 0.1282 x 0.0660 | 1304 | Brass | original, project-owned | Central case, side islands. | Bar axis X at y = 0.124 m. |
| `RingBoxOpen.obj` | Packaging | Navy velvet ring box, 52 x 48 x 42 mm, lid open 105 deg on a gold hinge pin along the back seam (square seam edges, so the halves close flush), cream satin lining with two rolls (ring slot along X). | 0.0520 x 0.0724 x 0.0738 | 1592 | VelvetNavy, VelvetCream, Gold18k | original, project-owned | Counter; an empty open box next to a broken bay is good evidence dressing. | Ring hole centre about (0, 0.034, 0) with the ring axis along Z (shank in the slot between the rolls). |
| `RingBoxClosed.obj` | Packaging | The same ring box closed (flush seam, gold hinge pin at the back). | 0.0520 x 0.0420 x 0.0485 | 1592 | VelvetNavy, VelvetCream, Gold18k | original, project-owned | Drawers, safe shelves, counter. | - |
| `PriceTag01.obj` | Price | Jewellery string tag (30 x 13 mm card, cotton loop) printed "18K Diamond Solitaire £4,950". | 0.0713 x 0.0011 x 0.0130 | 592 | PriceTag, Cord; maps: PriceTags_Color.png | original, project-owned | Tied to rings/necklaces in trays. | Shares PriceTags_Color.png (atlas cell 0). |
| `PriceTag02.obj` | Price | String tag "Platinum Eternity £3,200". | 0.0713 x 0.0011 x 0.0130 | 592 | PriceTag, Cord; maps: PriceTags_Color.png | original, project-owned | Bridal tray. | Atlas cell 1. |
| `PriceTag03.obj` | Price | String tag "Sapphire Halo £3,900". | 0.0713 x 0.0011 x 0.0130 | 592 | PriceTag, Cord; maps: PriceTags_Color.png | original, project-owned | Coloured-stone tray. | Atlas cell 6. |
| `PriceTag04.obj` | Price | String tag "Diamond Studs 1.0ct £2,600". | 0.0713 x 0.0011 x 0.0130 | 592 | PriceTag, Cord; maps: PriceTags_Color.png | original, project-owned | Earrings. | Atlas cell 3. |
| `PriceStand01.obj` | Price | Clear acrylic price stand (60 mm) with printed card "Akoya Pearl Strand £1,850". | 0.0600 x 0.0294 x 0.0305 | 108 | Acrylic, PriceTag; maps: PriceTags_Color.png | original, project-owned | Next to NecklaceBust. | Atlas cell 2. |
| `PriceStand02.obj` | Price | Acrylic price stand "Tennis Bracelet 3ct £7,400". | 0.0600 x 0.0294 x 0.0305 | 108 | Acrylic, PriceTag; maps: PriceTags_Color.png | original, project-owned | Bracelet tray. | Atlas cell 4. |
| `PriceStand03.obj` | Price | Acrylic price stand "Dress Watch 39mm £5,750". | 0.0600 x 0.0294 x 0.0305 | 108 | Acrylic, PriceTag; maps: PriceTags_Color.png | original, project-owned | Watch case. | Atlas cell 5. |
| `PriceStand04.obj` | Price | Acrylic price stand "Fine Gold 100 g £6,120". | 0.0600 x 0.0294 x 0.0305 | 108 | Acrylic, PriceTag; maps: PriceTags_Color.png | original, project-owned | Bullion shelf / office. | Atlas cell 7. |
| `GiftBag.obj` | Packaging | Matte black paper gift bag 150 x 190 x 80 mm with gusseted sides, cream inside, rope handles and gold-foil Aurel & Finch print (fictional shop). | 0.1700 x 0.2420 x 0.0954 | 1664 | GiftBag, PaperWhite, Cord; maps: GiftBag_Color.png | original, project-owned | Behind the counter; tipped over on the floor. | - |
| `GiftBox.obj` | Packaging | Black card gift box 85 x 85 x 40 mm with champagne satin ribbon and bow. | 0.1006 x 0.0659 x 0.1006 | 1456 | PaperBlack, SatinRibbon | original, project-owned | Counter, wrapping station. | - |
| `GoldBar1kg.obj` | Bullion | 1 kg cast fine-gold bar, 116 x 52 x 9.5 mm (draft angle, rounded edges, orange-peel cast surface, stamped fictional assayer marks in the normal map). | 0.1160 x 0.0095 x 0.0520 | 364 | GoldBar1kg; maps: GoldBar1kg_Color.png, GoldBar1kg_Normal.png | original, project-owned | Open safes in the rear office (some left behind). | Assign GoldBar1kg_Normal.png as the normal map. |
| `GoldBar100g.obj` | Bullion | 100 g minted fine-gold bar, 55 x 31 x 3.1 mm with struck frame and text. | 0.0310 x 0.0031 x 0.0550 | 108 | GoldBar100g; maps: GoldBar100g_Color.png, GoldBar100g_Normal.png | original, project-owned | Safe shelves, display tray. | Assign GoldBar100g_Normal.png. |
| `GoldCoin.obj` | Bullion | 1 oz fine-gold bullion coin, 32.7 x 2.9 mm, raised rim; original design (sunburst obverse, laurel reverse, 2026). | 0.0327 x 0.0029 x 0.0327 | 1728 | GoldCoin; maps: GoldCoin_Color.png, GoldCoin_Normal.png | original, project-owned | Safe trays, coin tubes, scattered on the office floor. | Assign GoldCoin_Normal.png. Obverse up. |
| `JewelersLoupe.obj` | Tool | 10x triplet folding loupe (21 mm lens): black barrel, glass, chrome swing-out case opened 160 deg. | 0.0707 x 0.0118 x 0.0363 | 1648 | BlackMetal, Glass, Chrome | original, project-owned | Counter mat, rear office desk. | Glass alpha 0.2. |
| `PaymentTerminal.obj` | Counter | Countertop card payment terminal on its charging base: keypad toward the customer (+Z), screen showing "£4,950.00", receipt roll and paper at the back. | 0.0780 x 0.0763 x 0.1700 | 1032 | PlasticGrey, PlasticBlack, TerminalTop, PaperWhite; maps: PaymentTerminal_Color.png | original, project-owned | Main counter by the till. | TerminalTop texture can also be the emission map (dim screen glow at night). |
| `PosTablet.obj` | Counter | Till tablet (250 x 172 mm) on a brushed-steel swivel stand, screen faces +Z showing a sale. | 0.2500 x 0.2354 x 0.1250 | 888 | SteelBrushed, PlasticBlack, PosScreen; maps: PosTablet_Screen.png | original, project-owned | Main counter (screen toward the staff side). | PosTablet_Screen.png as emission for a lit screen. |

## Textures

| File | Pixels | Kind | Used by |
|---|---|---|---|
| `EarringPearlDrop_Color.jpg` | 1024 x 1024 | colour (sRGB) | EarringPearlDrop |
| `EarringPearlDrop_MetallicSmoothness.png` | 512 x 512 | metallic-smoothness (R metal, A smoothness; linear) | EarringPearlDrop |
| `EarringPearlDrop_Normal.png` | 512 x 512 | normal map (OpenGL, +Y green) | EarringPearlDrop |
| `GemPaper_Color.png` | 1024 x 512 | colour (sRGB) | GemParcel |
| `GiftBag_Color.png` | 1024 x 1024 | colour (sRGB) | GiftBag |
| `GoldBar100g_Color.png` | 512 x 1024 | colour (sRGB) | GoldBar100g |
| `GoldBar100g_Normal.png` | 512 x 1024 | normal map (OpenGL, +Y green) | GoldBar100g |
| `GoldBar1kg_Color.png` | 1024 x 512 | colour (sRGB) | GoldBar1kg |
| `GoldBar1kg_Normal.png` | 1024 x 512 | normal map (OpenGL, +Y green) | GoldBar1kg |
| `GoldCoin_Color.png` | 1024 x 512 | colour (sRGB) | GoldCoin |
| `GoldCoin_Normal.png` | 1024 x 512 | normal map (OpenGL, +Y green) | GoldCoin |
| `PaymentTerminal_Color.png` | 512 x 1024 | colour (sRGB) | PaymentTerminal |
| `PocketWatchVintage_Color.jpg` | 1024 x 1024 | colour (sRGB) | PocketWatchVintage |
| `PocketWatchVintage_MetallicSmoothness.png` | 512 x 512 | metallic-smoothness (R metal, A smoothness; linear) | PocketWatchVintage |
| `PocketWatchVintage_Normal.png` | 1024 x 1024 | normal map (OpenGL, +Y green) | PocketWatchVintage |
| `PosTablet_Screen.png` | 1024 x 1024 | emissive-capable screen print | PosTablet |
| `PriceTags_Color.png` | 1024 x 512 | colour (sRGB) | PriceStand01, PriceStand02, PriceStand03, PriceStand04, PriceTag01, PriceTag02, PriceTag03, PriceTag04 |
| `RingAntiqueTigerEye_Color.jpg` | 1024 x 1024 | colour (sRGB) | RingAntiqueTigerEye |
| `RingAntiqueTigerEye_MetallicSmoothness.png` | 512 x 512 | metallic-smoothness (R metal, A smoothness; linear) | RingAntiqueTigerEye |
| `RingAntiqueTigerEye_Normal.png` | 512 x 512 | normal map (OpenGL, +Y green) | RingAntiqueTigerEye |
| `WatchDial_Color.png` | 1024 x 1024 | colour (sRGB) | WatchDressSteel |

## Ideas for the burglary scene

- Smashed central bays: `NecklacePearlDropped`, `NecklacePendantDropped`, a `RingGoldBand`, a few `Gem*` stones and an empty
  `RingBoxOpen` on the floor or ledge make natural evidence-marker targets (they lie on y = 0).
- Intact wall cases: `NecklaceBust` with a draped necklace, `RingCone` with a ring, `EarringStand` with hoops, `WatchPillow` +
  `WatchDressSteel`, each with a `PriceStand` or `PriceTag`.
- Rear office with the open safes: `GoldBar1kg`, `GoldBar100g`, `GoldCoin` stacks left behind, a `GemParcel` on the desk,
  `JewelersLoupe`, `PocketWatchVintage`, `RingAntiqueTigerEye` in a repair tray.
- Counter: `PaymentTerminal`, `PosTablet` (screen as a faint emission at night), `GiftBag`, `GiftBox`.
- Diamonds read best with a reflection probe per case, smoothness 0.98 and a sparkle/cubemap gem shader; under the UV torch
  many real diamonds fluoresce blue - an emission boost when the UV lamp is on would be a nice, cheap effect.

## Revision of October 15, 2026 (fixes after an independent check)

- `RingSapphireHalo`: the halo head no longer floats 1.7 mm above the shank. The halo frame now has a tapered gallery
  skirt that rests on raised cathedral shoulders, the four claws run down past the pavilion into the top of the shank,
  an under-gallery rail ties them together and the sapphire's culet sits on the shank: one connected piece.
- `WatchDressSteel`: the case back now rests on the crown of `WatchPillow` (0.5 mm into the soft cushion instead of
  3.2 mm above it), the lugs are 20 mm apart to take the 20 mm strap, and the padded strap follows the cushion 0.2 mm
  off its surface and tucks under its bottom edges, so nothing (strap or buckle) goes below y = 0 any more. The buckle
  sits under the cushion and is not modelled.
- `RingAntiqueTigerEye`: rescaled x 1.225 (26.3 mm across) so its finger hole is 17.1 mm like the procedural rings
  (it was 14.0 mm); its hole centre is (0, 0.0121, 0.0014) m in model space.
- `EarringPearlDrop_MetallicSmoothness.png`: was all zero (an RGBA resize with zero alpha wiped the RGB). It is now
  built from the colour map: gold metallic 1, pearls 0, smoothness 0.71 (= 1 - the glTF roughnessFactor 0.292).
  `PocketWatchVintage_` and `RingAntiqueTigerEye_MetallicSmoothness.png` were regenerated with the same per-channel
  converter (`Source~/maps.py`); they change by less than 1 level on average.
- Same pass, other floating parts found by a surface-gap scan of every model (any part more than 0.15 mm from the rest):
  - `EarringsDiamondStuds`: the post stopped 0.28 mm short of the basket; claws and post now meet on a small seat disc.
  - `NecklaceBust`: the plinth top falls about 1 mm towards its rounded edges, which left the bust's bottom edge up to
    0.9 mm in the air at the sides; the bottom edge now sits 1.3 mm into the plinth (no gap anywhere). The plinth is
    10 mm thick (the old text said 6 mm).
  - `NecklacePearlDraped` / `NecklacePearlDropped`: the pearls were loose beads 0.6 mm apart; the strands are now
    hand-knotted (a silk knot in every gap, threaded into the fluted clasp, which lies along the strand) and the draped
    clasp rests on the neck instead of 2 mm off it. 58 pearls each, 8.5k triangles.
  - `GemParcel`: the loose stones hovered 0.8 mm above the paper; each now rests on the paper surface under it. The
    paper is 0.1 mm thick (was a 0.3 mm open sandwich).
  - `RingBoxOpen` / `RingBoxClosed`: the lid swung about a point 2.2 mm outside the rounded shells and the hinge pin
    floated 2.5 mm behind the box. The base and lid now have square seam edges that close flush, the lid is hinged on
    that back seam edge and the 36 mm hinge pin lies along it.
  - `JewelersLoupe`: the lens barrel now fills the case hole (it had 0.3 mm of air all round).
  Every other model was already connected within 0.15 mm, apart from two deliberate clearances: each price-stand card
  sits 0.3 mm inside its acrylic holder and the pendant's bail rides 0.25 mm clear of the chain link through it. The
  hoops and the studs are pairs of separate earrings.

## Regenerating

`python3 -P Source~/build.py OUT_DIR` (numpy, scipy, Pillow; PYTHONPATH = Source~) rebuilds every procedural model and
texture; `Source~/postprocess.py` documents how the four third-party models were re-oriented, scaled and their maps
converted after `Tools/glb_to_obj.py` (map conversions in `Source~/maps.py`). `Source~/render.py` is the small numpy rasteriser used for `Preview.jpg`.
Hashes, sources and licences of every shipped file are in `sources.json`; sizes and triangle counts in `models.json`.
