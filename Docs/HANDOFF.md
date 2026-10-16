# Fixture detail and realism gap round — October 16 (Claude, unverified, never run in Unity)

**To apply and verify everything since October 14 on the PC, follow [PC_VERIFICATION.md](PC_VERIFICATION.md)** (one ordered checklist and a ready-to-paste prompt for the next assistant).

This round adds detail where guests stand: the 14 display and wall cases, the counter, both safes, the forced entrance doors, the eight facade bays and the two side-room doors. It also adds architectural trim, security devices and three bare trees across the street, and lowers the gloss of the small metals so they shimmer less in the headset. The items were ranked by guest impact per unit of frame time; the small-metal finishes cost no frame time at all. The validator helper cleanup came first, because the new checks call its shared helpers; guests see no difference from it. An architect planned the round, a skeptic checked it against the code, a revised plan resolved every blocking point, and one implementer per file group wrote it. The fixture detail is faces and submeshes of each fixture's existing renderers. The only new renderers are 20 static ones (15 devices, the architraves, the shadow gap and 3 trees) and 1 hidden one (the cut cable). No light and no texture is added, and no collider changes, so walking, teleporting and tool placement work as before in VR and on mouse and keyboard. Nothing that depends on the country, the entry route or the evidence was decided (see "Open decisions stay open" below). **Nothing here has been compiled or run in Unity.**

What was checked here: the Roslyn compile check (C# 9, UNITY_EDITOR, every Prototype/Scripts and Prototype/Editor file against Unity 2021.1 reference DLLs, with a shim for the Unity 6 Find*ByType APIs) ran on the builder changes, again on the merged tree after the cross-file review, and once more on the final tree for these notes. Each time the Editor compile reported only the 2 pre-existing errors at NightLighting.cs:624 (those DLLs lack `LightingSettings.lightmapCompression`, a Unity 2022+ API) and no warning, and Prototype/Scripts alone (no UNITY_EDITOR) reported 0 errors and 0 warnings. No error comes from a file this round touched. A scratch harness ran the real ProceduralMesh with this tree's and 6ceceea's builder code on stand-in Unity types: it reproduces every triangle count below, a port of the 8 new gallery checks passes on the new builders, and 7 of them fail on 6ceceea's (the eighth, FixturePart, guards the renderer structure, which 6ceceea already has). None of this replaces a compile in Unity 6000.6.

- **What the guest sees, by area** (default settings):
  - **Cases** (8 display cases, 6 wall cases):
    - 10 mm panes; the top pane is an overlay on the frame, so its dark green 10 mm edge shows on all four sides (KitGlassEdge, opaque). The case top stays at 1.05 m.
    - Bevelled bronze posts and rails. The rails run between the posts, so the corners butt instead of overlapping, and stop 0.5 mm under the top pane, which lies on them.
    - Inside: a black silicone bead at the foot of each standing pane, piping round the suede deck, and an LED channel with its diffuser (off) under the front rail (an island case also has one under the back rail).
    - Staff side: two cupboard locks and the doors' meeting line in the carcass panel, and a key lock at the foot of the sliding pane. Both case drawers (the central case's and the knocked case's, which lies on the floor) have a lock too.
    - A dark plinth gap (a shadow line) under the walnut carcass, also on the counter. The framed panels are rebuilt from bevelled members that butt (see the fixes below).
    - On the broken cases the glass fragments on the deck now lie 4.5 mm over the suede (was 2 mm), so they clear the 3 mm piping; the crown of an all-broken top lies on the rails.
  - **Counter:** the same dark plinth gap and framed panels, and a stone top with its long edges eased.
  - **Safes:**
    - Bevelled top and bottom walls; the side walls stay square, so no vertical edge changes its bevel halfway up.
    - Barrel hinges with domed finials on plates, dark bolt receivers on the left jamb facing the three bolts, and a bevelled door with a dark bolt channel on its free edge. Inside the door: a stainless bolt bar with a carrier at each bolt.
    - The large safe keeps its dial (with an index mark above it) and spoked handle and gains a key lock beside the dial. The tall safe's dial becomes an electronic lock (dark housing, a screen that is off, twelve grey keys) with its lever handle and a key lock.
  - **Forced entrance doors:** bevelled jambs, head, hinge stiles and rails (the lock stile stays square, because the pry marks and the UV smear span its 70 mm faces), 12 mm glazing beads round the glass on both faces, round pull bars through-bolted to each other through the glass, a deadlock on both faces of each bottom rail, and a door contact under the head over each leaf's magnet.
  - **Facade bays:** street-side snap caps on the mullions, sill and transom (adjacent bays' caps meet in one 60 mm cap with a V seam), 12 mm beads on the shop face of the glass and a 5 mm gasket in front of its street face. All of it is in the frame's metal, so the bays gain no submesh.
  - **Side-room doors:** a door contact under the head facing a magnet on the leaf; the office door has a key lock under its lever on both faces, the service door one on its push side above the bar. All of it stays clear of the Staff Only sign.
  - **Trim and devices:**
    - Walnut architraves round the office opening on both faces of the partition (45 mm wide, 18 mm proud). The head runs between the legs, as on the side-room door casings.
    - Skirting: the corrected corners and the run on the office face of the partition, which had none (see the fixes below).
    - A 15 mm dark shadow gap under the ceiling round the showroom and office walls (none on the facade, whose window heads reach the ceiling).
    - Three PIR detectors (high in the showroom's front corners and in the office), two light switches (both faces of the partition beside the opening), five sockets above the skirting, a second alarm keypad by the office opening, and two CCTV domes and two smoke detectors on the ceiling. The switch and socket plates are plain 82 mm squares: each switch has one wide rocker, each socket a blank insert with no pin pattern (decision 1).
  - **The corner camera** on the rear-left partition: its plate now sits square to the wall; only the arm and housing turn towards the entrance (NightStoreLayout.CameraAim, -42.5°). Before, the whole camera was turned and half its plate was inside the partition. It also sits 3 cm lower (pivot at 2.75 m, was 2.78), so the hood's raised rear stays 2 cm under the ceiling and below the shadow gap; at 2.78 it cut 1 cm into the slab.
  - **Across the street:** three bare deciduous trees on the far pavement (Exterior/Trees/Tree_0 to Tree_2), each about 6.2 m tall in a 1 m metal grate. They are a late-autumn simplification: the wet leaves in the gutter are theirs, and foliage is deferred. No species is named and they are not pollarded, since both hint at a country.
  - **Hidden at arrival:** the corner camera's cut cable, 0.3 m of black cable with its copper core showing, hanging under the camera's plate. It is the only new evidence-like item, and it is in the hidden "Case evidence" set (decision 8).
- **Per-fixture triangles, old -> new** (counted from the builder code; each StoreKit budget cap leaves room for a small detail above the new count):

  | Fixture | Triangles | StoreKit budget cap |
  |---|---|---|
  | display_case, intact | 428 -> 1,100 (Body 368 -> 1,040, panes 60) | 900 -> 1,300 |
  | display_case, front broken / all broken / with drawer | 448 -> 1,118 / 458 -> 1,036 / 512 -> 1,246 | 900 -> 1,300 / 1,300 / 1,450 |
  | display_case with the cracked top (night store only) | 668 -> 1,340 | — |
  | wall_case / all broken | 380 -> 972 (Body 924) / 906 | 900 -> 1,150 |
  | counter | 226 -> 498 | 400 -> 600 |
  | safe_large | 656 -> 931 (Body 190 -> 336, Door 466 -> 595) | 900 -> 1,150 |
  | safe_tall | 544 -> 774 | 800 -> 1,000 |
  | storefront_doors: closed / BrokenOpen / the night entrance, both panes broken | 310 -> 1,006 / 392 -> 1,088 / 474 -> 1,170 (each leaf 120 -> 432, the frame 46 -> 118) | 500 -> 1,200 / 700 -> 1,300 |
  | window_module: intact / cracked / broken | 118 -> 286 / 358 -> 526 / 210 -> 378 | 300 -> 400 / 500 -> 650 / 500 |
  | office_door / service_door | 204 -> 352 / 132 -> 218 | 300 -> 420 / 300 |
  | New devices (StoreKitDevices.cs): cctv_dome / smoke_detector / pir_detector / light_switch / socket_outlet | 206 (a 16-sided loft, so the glossy dome shows no lathe rings) / 86 / 44 / 40 / 30 | 250 / 120 / 80 / 60 / 50 |
  | alarm_keypad (the second one uses the existing builder) | 134 | 200 |
  | security_camera | unchanged (aim only) | 250 |

  The display-case Body is made of the plinth 10, carcass 12, 16 frame bars 448, deck 10, piping 40, deck trims 48, 8 rail and post bars 224, edge band 8, beads 8, two LED channels 28, cupboard locks 126 and the glass lock 78.
- **Budgets** (estimates; the first RunNightQuick log replaces them):

  | Budget | Limit | Before | After |
  |---|---|---|---|
  | Triangles, selected LOD at the worst eye | 220k | ~148k | ~163k (+15.1k: 8 display cases +5,402, 6 wall cases +3,552, counter +272, safes +505, entrance +696, 8 facade modules +1,344, side doors +234, 15 devices +1,080, architraves 168, office-face skirting 24, shadow gap 14, 3 trees 1,776 at 592 each with no LOD). Hidden: the cut cable, +40 in Case evidence |
  | Materials | 136 | ~128 | ~129. The one addition is KitGlassEdge, the 10 mm glass edge on 13 case tops: no opaque blue-green dielectric exists in the palette, and the band must stay opaque so it adds no transparent renderer. Everything else reuses materials already in the scene: KitLightLens for the LED diffusers (KitStripLight is not in the night store); KitFrameMetal for the plinth gaps, the LED channels and the door and window beads, caps and gaskets; DeviceBlack for the case beads, keyways and bolt receivers; KitWindowDark for the domes; Lacquer, DeviceGrey and DeviceBlack for the devices; KitConcrete bark with a KitFrameMetal grate for the trees; DeviceBlack and Bronze for the cut cable |
  | Transparent renderers, selected LOD | 70 | ~57 | +0: the edge band, the domes, the beads and the gaskets are all opaque |
  | Renderers before static batching | 900 | ~530 | ~550: +20 (15 devices, the architraves, the shadow gap, 3 trees), plus 1 hidden (the cut cable) |
  | Submeshes (draw calls before static batching) | — | not logged | about +119: 78 on existing fixtures (55 from the case Bodies, which go from 3 to 7 materials each, 6 on the all-broken case; 8 from the safes, 5 from the entrance, 7 from the side-room doors, 2 from the case drawers and 1 from the counter; the bays gain none) and about 41 from the new renderers (the devices 33, the trees 6, the architraves and the shadow gap 1 each). The new small materials repeat across many static renderers, so static batching should merge most of them per lightmap (unmeasured). The new "Budget: submeshes" Info line logs the total |
  | Lightmaps | 8 x 1024 | 2–4 estimated | 2–4 estimated. About 1.1 m² of architraves and 0.5 m² of skirting are added at shell density. A bevelled bar unwraps to about 3 charts against a box's 6, so the case Bodies' chart count stays about level. Probe-lit: every device (under 0.3 m), the shadow gap, the trees and the cut cable |
  | Pixel lights | 4 | 4 | 4: no Light component added; the LED diffusers and the safe's screen stay black-emissive |
  | Texture memory | — | ~155 MB | +0: no new texture |
- **Shimmer: small-metal finishes** (smoothness; colours and reflectance unchanged). A curved metal part a few millimetres across, at arm's length on Quest 2 (about 0.9 mm per pixel at 1 m), turns its highlight into a sub-pixel spark at 0.8, which 4x MSAA cannot fix (it resolves geometry edges, not shading). At 0.72 or less the highlight is twice as wide and about a quarter as bright at its peak; it stays sub-pixel, but it flickers far less. Gems are capped at 0.85; flat glass, screens and lenses are exempt. One material serves desktop and VR, so the desktop loses a little polish too.

  | Material | Before | After | Where |
  |---|---|---|---|
  | KitGold, KitSilver (StoreKitProps) | 0.80 | 0.72 | the kit's rings, chain and bangle; the pack's ring band, the ring box's hinge pin and the pearl strand's clasp; through JewelryRemap also the loupe's chrome |
  | KitPearl (NightStoreDressing) | 0.80 | 0.70 | the 7 mm beads of the pearl strand |
  | KitGem (NightStoreDressing) | 0.92 | 0.85 | the GemParcel's facets, 1–2 px across |
  | JewelryModels GoldCoin (DemoAssetLibrary finish table) | 0.85 | 0.72 | the coin's milled edge |
  | JewelryModels GoldBar1kg | 0.78 | 0.72 | the bar's stamped relief |

  The old shop's JewelryGold (ShopDisplayFinish) is a separate material and keeps 0.8.
- **Unchanged finishes, with reasons:**
  - KitBronze and KitStainless keep their PBRInterior BronzeBrushed and BrushedSteel maps (smoothness about 0.62 and 0.53; scalars 0.45 and 0.55). They are already under the cap and sit on posts, rails, pulls and bolts at least 12 mm across.
  - Velvet and suede (smoothness about 0.18 and 0.10) are matte and do not shimmer. They lack the sheen that makes fabric read as fabric, but the Standard shader has no sheen term.
  - The thinnest parts (the ring box's 1 mm hinge pin and the band's 1.8 mm edge, 1–2 px at 1 m) still alias across their width.
  - Both need one forward-only surface shader (deferred): the Standard BRDF plus a Charlie or Ashikhmin sheen term for velvet and suede, and roughness widening from the screen-space normal variance (ddx/ddy, Kaplanyan-Hoffman) for the small metals and those thin parts. It must keep directional lightmaps, light probes, box-projected reflection probes and single-pass instanced and multiview stereo, cast shadows through Fallback Standard, survive ForwardOnlyShaderStripping, ship through a material in the scene, and have a check that it compiles and is supported. It cannot be compiled or profiled here, and it touches every palette fabric and metal.
- **Refactor** (behaviour-neutral; no existing check label or Info text changes):
  - DemoValidation gains the geometry helpers the area files used to keep privately: `RayMesh` with `RayMeshOf`, `NewRayMesh` and `ClearRayMeshes`, `TriangleRay` (one Möller-Trumbore triangle ray for StreetMeshRay, InteriorRay and DressingRay), `SurfaceY` (was GlowSurfaceY), `BoxColliderBounds`, `Footprint`, `FootprintGap`, `RectPointDistance`, `PointSegmentDistance`, `RectSegmentDistance` and `Mm`. Two helpers are new: `AppendWorldMesh` (several meshes into one world-space list) and `CoplanarOverlap` (the first pair of same-facing triangles that lie in one plane and overlap with positive area, the cause of z-fighting; shared edges and touching corners do not count). The Street, Interior, Dressing and Lighting area files call them. Helpers with their own tolerances stay local, each with a comment giving the reason.
  - `StoreKit.Transparent` and the transparency loop in `KitMesh.Emit` are replaced by `DemoAssetLibrary.Blended`, closing the October 16 entry's cleanup item.
  - New `KitMesh.Bar`: a box with the four edges along its longest axis bevelled (ProceduralMesh.ChamferBox turned onto that axis), 28 triangles; its end faces stay square, as on cut profiles. `StoreKit.KeyLock` and `KeyCylinder` are the shared locks.
  - `StoreKit.SecurityCamera(parent, aim)` takes an aim angle; the triangle count does not depend on it.
- **Fixes found by review:**
  - FramedPanels had coplanar 5 mm overlaps, which z-fight: the stile tops against the carcass top, and the corner stiles against each other. It is rebuilt so its members butt: the rails run between the stiles, and the front and back stiles cover the corners.
  - The case top rails overlapped at the corners. They now run between the posts.
  - Four skirting corners overlapped (the showroom's and the office's rear corners) and now butt. The showroom-face skirting now stops at the architrave legs' outer faces, x ±(OfficeOpeningHalf + ArchitraveWidth). The office face of the partition gets the skirting it lacked (+24 triangles).
  - The corner camera's plate was half inside the partition, and its hood cut 1 cm into the ceiling slab (above).
  - The storefront pull standoffs floated 21 mm off the glass; the standoffs now pass through it.
  - Cross-file review: FrameJointChecks had been left out of `StoreKitValidation.Check` (written before `DemoValidation.CoplanarOverlap` existed); it is now called after CaseGlassChecks. A port of the builders and of the shipped CoplanarOverlap finds no overlap on the seven case and counter Bodies, and an overlap on each 6ceceea Body. The "Budget: submeshes" Info line was missing from NightStoreValidation.Budgets and is now logged.
- **Side benefit:** a tool set down on a case top now rests on the glass. The top pane ends at the collider's top (1.05 m); before, the tool floated about 1 cm above the inset pane.
- **Known gaps, left as they are:**
  - On the broken right sidelight (Sidelight_Right), 3 of the 4 glass fragments on the sill (y 0.432 m) pass under the new shop-side bottom bead (y 0.430–0.442 m). The plan kept the window Shards unchanged; a later fix keeps the sill spill on the street side of z -0.005 with smaller fragments.
  - Only in the gallery's closed safe: the door's lining panel, and now its bolt bar and carriers, reach into the shelf fronts. Both night-store safes stand open, so guests never see it.
  - The alarm keypad's screen is only 0.1 mm proud of its body (the existing builder, now on two keypads); watch it for flicker at a distance.
  - The corner camera is now always the procedural one, because only it takes the aim: a user-converted security_camera model is no longer placed there. The new devices still go through StoreKit.Instantiate, so a converted model with their name would replace them.
- **Open decisions stay open** ("for Koda.md"):
  - PoliceLook stays Provisional.
  - No laminated-glass or roller-grille state, and no change to the fractures, glass fans or floor shards (decision 2); the case tops change shape only for the overlay pane.
  - No UV trace, tool or evidence moves. The cut cable is hidden in Case evidence, and no video recorder (DVR) is placed (decision 8).
  - The alarm bell stays off at arrival (decision 6), and `StockInSafes` stays true (decision 9).
  - The sockets have blank faces and the switches one plain rocker (decision 1), and no call point is placed (decision 10).
- **New checks** (Edit mode; none needs a pack):
  - **StoreKitValidation**, 8 gallery checks. `StoreKitValidation.Check` runs inside RunGallery and inside NightStoreValidation's Generate, so they count in both:
    - "Case panes are 10 mm and the top pane rests on the rails, its green edge band opaque on all four sides" (CaseGlassChecks)
    - "Case and counter bodies have no coplanar overlapping faces, so their frame joints cannot z-fight" (FrameJointChecks; `CoplanarOverlap` on every case and counter Body)
    - "Fixture detail stays merged: cases, safes, doors, bays and the counter keep their renderers and part names" (FixturePartChecks)
    - "Safe bolt receivers on the jamb line up with the door's three bolts" (SafeBoltChecks)
    - "Case locks face the staff side and the LED diffusers face down inside the glass" (CaseHardwareChecks)
    - "Glazing beads, caps and gaskets frame the door and window glass without entering it" (GlazingChecks)
    - "Door contacts pair up on closed leaves and part when a leaf is forced open" (ContactChecks)
    - "Wall devices pivot at the bottom centre of their back, at most 6 cm proud; ceiling devices hang at or below the ceiling plane" (DeviceChecks)
  - **NightStoreValidationDetail.cs** (new area file, prefix Detail; `DetailChecks` runs in Generate right after CreditChecks, before the first save, so it reads the meshes before Play mode's static batching), 11 checks:
    - "Shared triangle-ray, footprint and coplanar helpers measure known shapes exactly" (DetailHelpers)
    - "Dressing and jewellery finishes stay at or below 0.72 smoothness (gems 0.85), glass and mapped surfaces aside" (DetailMetals; it also runs without JewelryModels, because the StoreKit stand-ins carry KitGold)
    - "Case tops show their green glass edge exactly where the top pane is intact, at the cases' 1.05 m height" (DetailCaseEdges)
    - "Walnut architraves frame the office opening on both faces and the skirting butts into them, with no coplanar overlap" (DetailTrim; `CoplanarOverlap` on the architraves and skirting together)
    - "A dark shadow gap runs round the showroom and office walls under the ceiling, probe-lit and shadowless" (DetailShadowGap)
    - "Switches, sockets, PIR detectors and the office keypad sit on their walls, above the skirting and clear of the wall graphics and fixtures" (DetailWallDevices)
    - "CCTV domes and smoke detectors hang flush under the ceiling, clear of the downlights, tracks and diffusers" (DetailCeilingDevices)
    - "PIR detectors face into the showroom and the office, and the showroom ones never towards the shop windows" (DetailPirs: all three face their room's centre; only the two showroom ones must also face away from the facade)
    - "The corner camera's plate sits flat on the partition and only its housing turns towards the entrance" (DetailCamera)
    - "The only cut cable hangs under the security camera in the hidden Case evidence set, in front of the wall" (DetailCutCable)
    - "Street trees stand on the far pavement clear of the lamps, the shelter, the racks and the facades, probe-lit, shadowless and without colliders" (DetailTrees; the crowns must stay within FarPavementZ + 0.3 and FarKerbZ + 0.3)
  - **Existing checks that change without a new name:** "Store kit builds every fixture" now expects 31 fixtures (was 26). The per-entry Info line of "Store kit fixtures stay within their triangle budgets" now ends in `submeshes=`. NightStoreValidation.Budgets logs a new Info line, "Budget: submeshes (draw calls before static batching, all active renderers)=", which no check reads.
- **Expected PASS-count changes** (on top of the October 16 validator entry's figures):

  | Validator | Change | Made up of |
  |---|---|---|
  | NightStoreValidation.RunNightQuick | +19, with or without the packs | 8 StoreKitValidation checks (StoreKitValidation.Check runs inside it) and 11 Detail checks |
  | NightStoreValidation.RunNight | +19, with or without the packs | the same |
  | StoreKitValidation.RunGallery | +8 | the gallery checks |
  | ShopCityValidation (every run), JewelrySceneValidation, GlassToolValidation | none | — |
- **Checks predicted to fail:** none on today's code, by the plan's traces. The closest margins, in case one fails:
  - the wall clock 14.8 mm above the architrave head (2.445 m), where the clock check needs 5 mm;
  - the extinguisher 65 mm beside the nearer architrave leg, where its check needs 20 mm;
  - the front PIRs 14 cm from the Pissarro's gilt frame;
  - the tree crowns about 0.33 m inside both z limits (the crowns span about z -16.35 to -14.05; worst-case reach 1.29 m across and 1.15 m in z);
  - the case rails 0.5 mm under the top pane;
  - the case LED diffuser 2.5 mm inside the CaseHardware limit.
  - Existing checks to watch: "Night store material budget" (about 129 of 136), "Store kit fixtures stay within their triangle budgets" (the new caps), "Night store static flags follow the bake policy" (the architraves lightmapped, the devices, shadow gap and trees probe-lit) and the clock and extinguisher checks beside the office opening.
- **Deferred** (ROADMAP has the details):
  - **Wet-street reflection streaks** (A4), the next round's first item and the most guest impact of what is left. A reflection's place depends on the eye (it lies at the lamp's mirror point), so each streak must turn and move per camera in GlowBillboard, in VR and on the desktop; about 6 additive cards (transparency 57 -> 63 of 70), and the strict glow-card checks need widening. Fixed streaks were rejected: they would slide off their lamps as the guest walks, and the Wash_ cards already give the static part.
  - **The sheen and specular anti-aliasing shader** (the rest of A9). The Standard shader has no sheen term and no geometric specular anti-aliasing, so this round only lowers the gloss. A custom forward shader with stereo, lightmap and probe support touches every palette fabric and metal and cannot be compiled or profiled here.
  - **Foliage for the trees.** Leaf cards need a leaf texture the repository lacks, one cutout material and cutout overdraw seen through the facade.
  - **The DVR, its cables and trunking.** Whether the recorder is intact, taken or damaged decides what footage exists, which is Koda's decision 8; the options are in "for Koda.md".
  - **Green rims on the broken glass.** The broken glass and shards must stay all-blended (shadows, the case-glass merge and the transparency count depend on it), so opaque rims need 6 to 10 extra renderers, and the fracture look waits on decision 2.
  - **Second-tier fixtures and safe edge-wear** (A17): less guest impact per frame than this round's items.
  - A fallback only, if the bake blurs the case Bodies: move the locks, keyways, LED parts, piping and beads into a probe-lit "Hardware" child per case (+14 renderers, with FixturePartChecks updated).
- **Files added or changed:**
  - New, each now in Downloads/JewelryStore_Glass_Unity6000.6.2f1.zip with a fresh .meta guid (the ZIP is synced to this round, 59.1 MB, with the changed Editor files updated): Prototype/Editor/StoreKitDevices.cs (the five devices) and Prototype/Editor/NightStoreValidationDetail.cs (the Detail checks; it needs the matching NightStoreValidation.cs, which calls DetailChecks). The ZIP sync is done separately; until then the ZIP holds the 6ceceea scripts.
  - Changed, keeping their existing .meta files:
    - Editor builders: StoreKit (case, counter, safe, door and bay detail, KitGlassEdge, the budget caps, the camera aim, the five new Names and gallery lifts), KitMesh (Bar; Emit uses Blended), StoreKitProps (KitGold, KitSilver), NightStoreDressing (KitGem, KitPearl), DemoAssetLibrary (the coin and bar finishes), ShopDisplayFinish (a comment only), NightStoreBuilder (architraves, skirting, shadow gap, domes and smoke detectors, the camera with its aim), NightStoreLayout (the wall devices as Fixtures rows, the camera at yaw 0 and 2.75 m, CameraAim, Domes, SmokeDetectors), NightDecals (the cut cable), NightStreet (the trees).
    - Editor validators: DemoValidation (the geometry helpers), NightStoreValidation (the DetailChecks call, the submeshes Info line), NightStoreValidationStreet, NightStoreValidationInterior, NightStoreValidationDressing and NightStoreValidationLighting (the shared helpers), StoreKitValidation (the 8 checks, 31 fixtures, the submeshes Info).
  - No runtime script, shader, scene or pack file changed. Outside the ZIP: Docs/HANDOFF.md, Docs/ROADMAP.md and for Koda.md.
- **To verify on the PC:**
  1. **Compile.** Bring the two new files and the changed ones into the project (sync the ZIP, or open through Open-Unity-Glass.cmd or Start-Demo.cmd). The Console must show no compile error and no warning from these files.
  2. **StoreKitValidation.RunGallery.** Expect +8. Read every "<entry> triangles=... submeshes=" line against the per-fixture table above, and record the coplanar result of FrameJointChecks.
  3. **NightStoreValidation.RunNightQuick.** Expect +19. Record "Budget: triangles=" (about 163k expected), "Budget: materials=" (about 129) and "Budget: submeshes ...". Set MaterialBudget to the logged materials value + 8, as its comment says. Also record these Info lines: "Case edges: ..." (a band on 13 cases, none on RightIsland_Front), the DetailTrim coplanar result, each wall device's back gap, the PIR dots, the camera dot (at least 0.95) with its deepest plate vertex, the cut cable's wall gap, the trees' bounds, and DetailMetals' list of scanned materials.
  4. **NightStoreValidation.RunNight.** Expect +19. Check the "Lightmaps=" line (2–4 maps estimated), and look at the case Bodies' baked sharpness. If they blur, use the probe-lit hardware fallback under Deferred.
  5. **Frame Debugger:** record batches and SetPass calls at the arrival view and at the central cluster, on a baked build.
  6. **Headset (Quest 2 over Link, baked build):** shimmer on the dropped ring, the coins and the pearl strand under the torch (a soft highlight, not single-pixel sparks, while the gold still reads as polished); the green edge band on the case tops; no flicker at any case corner, at the architraves or on the keypads' screens; whether the deck fragments in the broken cases look as if they float (4.5 mm over the suede); the sill fragments under the bead on the broken right sidelight; the trees through the windows.
  7. **Desktop:** set a tool down on a case top; it must rest on the glass. Judge whether the gold and silver, slightly less mirror-like than before, still look right.
  8. **Inspector:** after the first night build, KitGold and KitSilver read 0.72, KitGem 0.85 and KitPearl 0.70 (StoreKit's folder), and the JewelryModels GoldCoin and GoldBar1kg pack materials 0.72. The old shop's JewelryGold stays 0.8 (ShopCityValidation.RunGlass shows no difference).

# Validator checks for the realism wiring — October 16 (Claude, unverified, never compiled)

The October 15 wiring (commit 9305177, the entry below) added behaviours that had no validator check, although CLAUDE.md asks for one per behaviour. This batch adds 83 checks: 82 in the night store's runs (six new NightStoreValidation area files, DemoValidation.LauncherChecks and one InterfaceChecks line) and 1 more in every ShopCity run. It also fixes the builder and runtime issues the checks exposed. An architect planned the checks and a skeptic traced each one against the code and the pack files (OBJ bounds and groups, MTLs, atlas JSONs, PNG sizes). One implementer per area wrote them, a wiring pass called them, and a cross-review read the whole diff. Every check looks at the built scene or the imported assets: objects, components, materials, static flags, mesh triangles and clips. None re-reads a constant the builder also reads, so a check fails when its behaviour breaks. When a check's pack is missing, it logs an Info line and skips, or checks the builder's fallback under a name of its own. **Nothing here has been compiled or run in Unity.**

What was checked here: tree-sitter found no syntax error. A Roslyn compile (C# 9, UNITY_EDITOR) of every Prototype/Scripts and Prototype/Editor file ran against older Unity reference DLLs, with a stub for the Unity 6 find APIs. It found no error in any validator file. The only errors left are the 2 pre-existing ones at NightLighting.cs:624: those DLLs lack `LightingSettings.lightmapCompression`. Prototype/Scripts also compiles alone without UNITY_EDITOR, so no editor API reached the runtime. None of this replaces a compile in Unity 6000.6.

- **Wiring.**
  - `NightStoreValidation` is now `public static partial class`. [InitializeOnLoad] and the static constructor stay in NightStoreValidation.cs. Each area file has its own `#if UNITY_EDITOR`, its usings and `using L = NightStoreLayout;`. Every helper, field and nested type in an area file carries that area's prefix: Street, Interior, Dressing or Decal, Glow or Lantern, Presenter or Credit, and Sound. The entry points keep the names the plan gave them (ClosingProcedureChecks, ArrivalPrintChecks, LightingPackChecks and so on). A scan of all seven files found no member defined twice.
  - **Edit mode** (Generate, so RunNightQuick and RunNight):
    - `DemoValidation.LauncherChecks()` runs right after the reimport Info line.
    - After `PoliceLookChecks` and before the first `SaveScene`, in this order: StreetPackChecks, InteriorPackChecks, ClosingProcedureChecks, DecalChecks, LightingPackChecks, PresenterBuildChecks, CreditChecks.
    - At that point NightLighting.Apply has set the lights, lenses, glow cards and lightmap scales. There is no bake, no realtime fallback and no player rig yet. The builders' records (NightInterior.Graphics and Props, NightDecals' statics) are still valid; they are not after Play mode's domain reload. Every mesh is read here too, because Play mode's static batching merges meshes.
  - **Play mode, Tick stage 1:**
    - ArrivalPrintChecks and PresenterArrivalChecks run right after LightingChecks, before anything is switched.
    - StreetPlayChecks runs after PlacementChecks, LightingPlayChecks after RealtimeLightChecks, and SoundPlayChecks after StagedSoundChecks, before InterfaceChecks and ToolStage.
    - Each check puts back what it changes: the sets, the light bar, the torches and lamp, the hand and pointer, the walker, the VR mode and the music.
  - **Reset:** `PresenterDisturb()` is now the last step of DisturbBeforeReset. It shows every set, holds the light bar steady, turns the underscore on and counts the stings played so far. `PresenterResetChecks()` runs in stage 3, right after ResetChecks.
  - ShopCityValidation.Generate, behind every ShopCity entry point, calls `DemoValidation.LauncherChecks()` right after Begin.
  - DemoValidation.Check still throws on the first failure, so one failing check ends the run. A failure in Edit mode stops it before the scene is saved, and in RunNight before the bake.
- **New checks.** Edit means before the first save; Play means Tick stage 1 unless stated. A pack name in square brackets marks a check that logs an Info line and skips without that pack.
  - **Launcher and licences** (DemoValidation.LauncherChecks, Edit; in NightStoreValidation and in every ShopCity run):
    - "Both launchers mirror the project through Tools/sync-project.cmd; the frozen launchers do not". It logs an Info line and skips when the project does not sit beside the repository's Tools/sync-project.cmd.
    - "All twelve realism packs are mirrored together, each with its LICENSE.txt". It logs an Info line and skips when no pack is present (a project from the ZIP alone), and fails when only some are.
    - "The licence copy ships every pack LICENSE.txt and Licenses/*.txt and skips ignored folders". It runs DemoBuild.LicenseFiles on a probe folder in the temp directory, then on this project's ThirdParty.
  - **Street** (NightStoreValidationStreet.cs). Measurements come from renderer bounds, mesh vertices and triangles, colliders and flags:
    - Edit, in StreetPackChecks, which syncs physics first:
      - "Street furniture rests on the pavement, the street or the table under it" [StreetModels, ForensicModels]
      - "Street props that reach the play area carry one bounds collider; the rest carry none" [StreetModels, ForensicModels or PeopleModels]
      - "Street props cast shadows as listed, their glass never, and keep their LOD levels" [the same]
      - "Figures stand at least 2.5 m outside the play area with no collider" [PeopleModels]. It measures on mesh vertices: the turned renderer boxes would show 2.38 m instead of 2.80 m.
      - "Bollards stand in the anti-ram row, solid and grounded, the pack's casting shadows". It always runs; without StreetModels it checks the procedural bollards' colliders only.
      - "The row across the street leaves the apartment block's slot free exactly when the block stands in it" (always runs)
      - "The apartment block stands in its slot across the street, its facade to the shop" [StreetModels ApartmentBlock]. It checks the bounds and the triangle winding along the front, so a mirrored import fails.
      - "The apartment block bakes into the lightmaps at the terrace's density and casts shadows" [StreetModels ApartmentBlock]
      - "The KayKit skyline towers are gone; nothing stands beyond the far pavement" (always runs)
      - "Brick on the left, render on the right and half-closed shutters on both neighbours and across the street" (always runs; measured per submesh)
      - "Lit windows, the apartment's rooms and the bus shelter's lightbox are baked emitters, and the street adds no light". It always runs; the apartment and shelter parts only when those models are placed.
      - "The alarm bell box sits on the fascia cladding, clear of the shop sign and the coping". It always runs: a triangle ray starts 1 cm in front of the box's back.
      - "No hydrant, parking machine, traffic light, road sign or name plate stands outside the shop (country left open)" (always runs)
    - Play, in StreetPlayChecks:
      - "Street furniture and bollards keep the walk route and the spawn clear". Each collider must keep the walker's radius plus 5 cm from every route segment, not just from the route points.
      - "Rack samples on the equipment table stay clickable from both sides, clear of the glove box". The desktop pointer aims from the street side and from the shop side.
      - "Rack samples on the equipment table stay grabbable beside the glove box and under the camera". It reads HandInteractor's own grab choice by reflection, without spawning a tool.
  - **Interior** (NightStoreValidationInterior.cs, InteriorPackChecks, Edit). Distances are triangle rays on the meshes themselves, since the skirting, glass and pictures have no colliders:
    - "NightInterior.Props names the props this build placed" [InteriorModels]
    - "Snake plants fill the three planters, each solid with one bounds collider". Without InteriorModels the check is named "Planters keep their procedural body without InteriorModels"; with a StoreKit planter override it is "User-converted planters stay as they are".
    - "The rug lies flat under the sofa and side table, lightmapped, with no collider or shadow" [InteriorModels Rug]
    - "Monitor and keyboard stand on the desk, clear of the transport tray, the loupe and each other" [ComputerMonitor and Keyboard]
    - "The monitor screen glows without a light and stays out of the bake; monitor and keyboard are solid" [the same]
    - "The extinguisher stands on the floor a few millimetres off the skirting, clear of the office opening" [FireExtinguisher]
    - "The wall clock hangs flat on the lintel above the office opening, its top level the LOD1 file" [WallClock]
    - "Ceiling diffusers sit flush under the ceiling between the downlights and cast no shadow" [CeilingDiffuser]
    - "Wall graphics lie flat 0.5 to 70 mm in front of their surfaces, facing out and unmirrored" [WallGraphics]
    - "The lost-cat flyer lies flat on the right pier's stone, clear of the lamp posts" [the WallGraphics street page]
    - "Wall graphics sample only their own atlas cell at its aspect" [WallGraphics]. Cells are read fresh from the layout JSON, and U and V pixel density must agree within 2 %.
    - "The still life and the lightbox take the rear and counter frames' place". Without the interior page it is named "Rear and counter sign frames keep their emblem without the WallGraphics atlas"; with a sign_frame override it logs an Info line and skips.
    - "Wall graphics use the two atlas pages and the gilt moulding, alpha-tested, unlit and shadowless" [WallGraphics]. It also wants one picture quad per placed graphic and 40 gilt triangles per framed painting, with the moulding's face 3 cm off the wall.
    - "Interior graphics and fittings stay country-neutral: no police notice, no pull-station call point" (always runs)
  - **Closing procedure and decals** (NightStoreValidationDressing.cs):
    - Edit, in ClosingProcedureChecks:
      - "Cases hold display furniture only under the closing procedure". With `StockInSafes` false it is named "Intact cases keep their stocked decks while StockInSafes is off".
      - "Left-behind jewellery lies on the open safes' lining and shelves". Without JewelryModels it checks StoreKit's ring-box stand-ins.
      - "Jewellery on the desk and the floor rests on its surface, clear of the walk route and the UV traces"
      - "Left-behind jewellery keeps its LOD levels, casts no shadow and stays opaque and probe-lit (glass lenses aside)" [JewelryModels]
      - "Glass zones cover the entrance fans, the broken cases' floor spill and the shard scatter's impact points"
    - Edit, in DecalChecks:
      - "Glass shards lie in front of the broken cases only" [the SurfaceDecals shard scatter]
      - "Always-on decals cast no shadow, are never occluders or probe-static, and bake at their surface's density" [SurfaceDecals]
      - "Decal quads sample only their own atlas cells at the cells' aspect" [SurfaceDecals]
      - "Decals lie 1 to 4.5 mm above the surface beneath them, parallel to it" [SurfaceDecals]
      - "Hidden decal sets stay out of the bake and the discarded tools rest on the floor or pavement". It always runs; without ForensicModels it checks the stand-in tools.
      - "Hidden floor marks draw before every transparent and the latent prints after the case glass" [SurfaceDecals]
      - "The case-evidence trail runs from the pavement through the forced doors along the walk route" [the SurfaceDecals Evidence atlas]
    - Play, at arrival: "No print-like or footwear-like mark is drawn at arrival". It always runs. It also looks for the small shop's untextured ShoePrint_GeometryPlaceholder by name, which the texture scan cannot see.
  - **Lighting** (NightStoreValidationLighting.cs):
    - Edit, in LightingPackChecks:
      - "Glow halos and washes add no light: additive cards without collider, shadow or probes". Without the NightSky glow sprite it is named "Without the glow sprite no glow card is built and no light is added".
      - "Each glow halo is saved on its lens, facing the arrival view" [the NightSky glow sprite]
      - "The light bar's washes lie flat just over the street at the car and over the shop floor inside the entrance" [the same]. Mesh rays measure the gap, so the police car's collider no longer gets in the way.
      - "The night sky is the overcast panorama; reflections come from the sky and the baked probes". Without the panorama it is named "The night sky is the procedural sky without the panorama; reflections come from the sky and the baked probes".
      - "The across-street reflection probe encloses the far facades and the far pavement" (always runs)
      - "Streetlight cookies throw their short sharp edge towards the nearer facade". Without the cookie it is named "Streetlights point straight down, turned to the nearer facade, without a cookie".
      - "Each lantern holds its light and one lens at the bulb inside its glass, and casts no shadow" [StreetModels StreetLamp]
    - Play, in LightingPlayChecks:
      - "Torches and lit scene lamps project the NightSky cookies". Without NightSky it is named "Torches keep the generated cookie and scene lamps project none without NightSky".
      - "The light bar crossfades between red and blue without a jump, and its washes follow"
      - "Light bar steady switch holds both halves dimmed with the washes off (desktop-only toast); a reset flashes it again"
      - "Glow halos turn to whichever camera renders them; a headlamp's halo shows only ahead of the car" [the NightSky glow sprite]. It needs the GlowBillboard fix below.
      - "Quick-preview streetlights follow the lamp profile: shadowless, near ones per-pixel, far ones vertex-lit". It runs in RunNightQuick only; RunNight logs an Info line instead.
  - **Presenter switches, reset and credits** (NightStoreValidationPresenter.cs):
    - Edit:
      - "Every presenter set is built hidden, its content a child of the set". The expected sets come from the pack files: Case evidence always, Ambient wear with SurfaceDecals' wear cells, and the Alarm bell with NightSoundscape's bell clip.
      - "The credits file names every placed CC BY and MIT asset and nothing else". The credits needed are read from every mesh and clip the scene, its hidden sets and its referenced prefabs use.
      - "The player build ships every pack's licence notice, the credited packs' among them"
    - Play, at arrival:
      - "Presenter sets are hidden at arrival and each listed once for the Scene tab; the underscore is off"
      - "Each presenter-set switch shows and hides its set and confirms on the monitor only"
    - Play, stage 3, after the reset:
      - "A session reset hides every presenter set and makes the light bar flash again; the underscore keeps its setting"
      - "A session reset plays the sting and ducks the underscore" [the Music ResetSting]
  - **Sound and music** (NightStoreValidationSound.cs, SoundPlayChecks, Play). No audio device is assumed, so nothing asserts isPlaying. The checks read back volumes, filters, clips and start offsets, and drive AmbientZones.Step, AmbientEvents.Fire and Footsteps.Advance directly:
    - "Night store soundscape and its street bed are built"
    - "The street is muffled inside the shop and full range on the pavement"
    - "Walking through the door fades the street (half the smoothing time gives half the blend at the log-frequency midpoint)"
    - "Exterior beds, street events and the alarm bell blend with the zones; the door whistle, the music, the interface and the footsteps stay out"
    - "The bank plays NightSoundscape's shutter, torch click and city bed, the bed in 2D" [NightSoundscape]
    - "Looped beds start at random points; the door whistle starts at its beginning"
    - "Cars pass and the CC0 dog barks now and then, each through its own 2D source and after the last has ended" [NightSoundscape]
    - "The V8 idle plays 3D at the police car's bonnet, heard at the spawn and muffled inside with the street" [NightSoundscape]
    - "The alarm bell rings from the bell box only while its set is shown" [NightSoundscape]
    - "Room reverb fills the showroom wall to wall and the office, and stops before the pavement; music and interface sounds stay dry"
    - "Desktop footsteps step every 0.7 m, crunch in the glass zones and skip jumps". Without NightSoundscape's footsteps it is named "The walker has footsteps whenever the bank has footstep takes".
    - "Room-scale walking, teleports and snap turns make no footsteps in VR" [NightSoundscape]
    - "Cones and markers set down with their own takes; other tools keep the thud". It always runs and spawns no marker.
    - "The underscore and the reset sting play from NightStore/Music, 2D, wired to the session" [Music]
    - "The underscore is off at arrival and its switch turns it on and off with a monitor-only toast; a sting ducks it" [the Music InvestigationLoop]. With the sting alone it is named "No underscore plays at arrival".
    - "The start menu plays its theme, fading in, then to silence over DemoLauncher.MusicFadeSeconds before a map loads" [the Music MenuTheme]. It logs an Info line instead when the frame time is too long to drive the fade frame by frame.
  - **DemoValidation.InterfaceChecks** (Play; NightStoreValidation and every ShopCity run): "The start menu's credits summary is the first credits line".
  - **ShopCityValidation.BackdropChecks** (every ShopCity run): "The old shop has no night-store soundscape, music, footsteps, glow or presenter sets".
- **Expected PASS-count changes** (on top of the October 15 entry's figures; all assume the default PoliceLook):

  | Validator | With all twelve packs | From the ZIP alone, no packs | Made up of (ZIP-alone count in brackets) |
  |---|---|---|---|
  | NightStoreValidation.RunNightQuick | +82 (52 in Edit mode, 30 in Play mode) | +44 | Launcher 3 (2), Street 16 (10), Interior 14 (3), Closing procedure and decals 13 (6), Lighting 12 (8), Presenter and credits 7 (6), Sound 16 (8), InterfaceChecks 1 (1) |
  | NightStoreValidation.RunNight | +81 | +43 | the same, less the quick-preview streetlight check, which logs an Info line once baked |
  | ShopCityValidation: Run, RunExpanded, RunForms, RunInterior, RunRobbery, RunGlass, RunTextured | +5 | +4 | Launcher 3 (2), credits summary 1, old shop without night-store components 1 |
  | StoreKitValidation.RunGallery | none | none | — |
  | JewelrySceneValidation, GlassToolValidation | none | none | — |

  - The night figures with the packs are 1 lower when the frame time is too long to drive the menu-theme fade (the "Menu theme fade not checked" Info line). From the ZIP alone the Music pack is absent, so that check is already left out.
  - Each night and ShopCity figure is 1 lower when the project does not sit beside the repository's Tools/sync-project.cmd (the "Launchers not found" Info line).
  - Against the counts before 9305177: RunNightQuick +108 and RunNight +107 with the packs (+59 and +58 from the ZIP alone), RunGallery +3, RunGlass and RunTextured +12, the other ShopCity runs +11.
- **What runs from the ZIP alone.** These checks run with no pack: the launcher and licence checks; seven street checks in Edit mode (bollards, the row's slot, the towers, the facades, the emitters, the bell box, the neutral kerb) and all three Play checks; the planters' and sign frames' fallbacks and the interior neutrality check; the decks, safes, desk and floor, glass zones, hidden sets and arrival print checks; the glow-card fallback, sky, probe, cookie fallback, torch cookies, light bar, steady switch and quick-preview lamps; the presenter build, credits, licences, arrival and reset checks; the soundscape, zones (2), lists, offsets, reverb, footsteps fallback and set-down checks; and the credits summary in InterfaceChecks. Every other check logs a "... not present: ... skipped" Info line.
- **Refactors** (behaviour-neutral; the October 15 entry's text is updated to match):
  - `DemoAssetLibrary.WorldBounds` replaces NightInterior's private WorldBounds and the loops in Seat and NightStreet.InPlayArea, and the validators use it.
  - `DemoAssetLibrary.Blended` is the one no-shadow rule: a renderer whose materials are all blended casts none. It replaces NightStreet.NoTransparentShadows and NightStoreBuilder's first-material test. No OBJ group in today's packs mixes blended and opaque materials, so no shadow changes.
  - `DemoGeometry.PathUnder` replaces the private copies in NightDecals and NightLighting.
  - `DemoGeometry.Opaque` moves out of DemoAssetLibrary, where it was private.
  - `DemoBuild.AddMenuMusic` is now the one method that builds the launcher's MenuMusic object; CreateLauncherScene and the menu-theme check both call it.
  - `NightDecals.Apply(root, folder, imported)`: every caller passed true before; NightStoreBuilder now passes its useImportedAssets.
- **Bug fixes found while writing the checks:**
  - GlowBillboard turned the halos to Camera.main in LateUpdate, so the evidence photo and the live viewfinder drew cards that faced the head. The cards now turn to each camera as it renders, on Camera.onPreCull, which fires once for the XR camera in single-pass or multiview.
  - The lost-cat flyer: a flat 0.23 m card on a lamp post about 0.1 m across stood about 6 cm off the post on each side. It is now on the right pier's street face (x 8.3), 3 mm off the stone, in every build.
  - The one showroom reverb zone (full within 4 m, none beyond 7 m) reached the pavement and missed the showroom's front corners. Three zones now stand in a row at mid-depth: Reverb_ShowroomLeft, Reverb_Showroom and Reverb_ShowroomRight, each full within 3 m and silent beyond 4.5 m.
  - NightStoreDressing still placed the small shop's geometric shoe print (ShoePrint_GeometryPlaceholder) by the office opening, a footwear mark at arrival. It is gone. The procedural ring and ring-box stand-ins now cast no shadow, like the pack pieces.
  - KitMesh's fingerprint ignored UVs, so a rebuild in the same session after an atlas change kept the cached mesh with the old atlas UVs. Explicit atlas UVs are now part of the fingerprint (ProceduralMesh flags them). Only the decal and wall-graphic meshes get new asset names.
  - AmbientZones: a low-pass filter beside an AudioSource has a distance curve that Unity may write over cutoffFrequency. Each exterior filter now gets one flat curve at the zones' cutoff; the curve is shared, so a blend allocates nothing.
  - DemoDesktopPanel: the Scene tab's SCENE DRESSING rows keep the label and note copied at the Layout event. A set destroyed since then keeps a disabled placeholder row, so Layout and Repaint draw the same controls.
  - Tools/sync-project.cmd's header named a planned check; it now names DemoValidation.LauncherChecks (CRLF kept). NightStoreLayout changed only in a comment: the KayKit post is octagonal, and PostRadius 0.054 clears it.
  - The skeptic found seven checks that would fail as first planned; each is fixed in the check:
    - the extinguisher is measured on the skirting's triangles at 5 cm height;
    - the flyer is moved (above);
    - the gilt count is 40 triangles per frame;
    - the loupe's StoreKit.Glass lens is allowed among the opaque jewellery;
    - DirtEdge_WallBase keeps its 0.5 aspect;
    - the evidence trail keeps a 5 cm margin to the dropped pieces;
    - the wash height is measured with mesh rays.
- **Checks predicted to fail:** none on today's code, by the cross-review's traces. The closest margins, in case one fails:
  - "The KayKit skyline towers are gone ..." has 5 cm of margin: the deepest exterior renderer reaches z -27.0 against a limit of -27.05.
  - The reverb check has 3.7 cm to spare at the side walls, and the pavement edge sits 4.60 m from the nearest zone against its 4.5 m reach.
  - The glove box is about 3.8 cm from the nearest rack sample. For the grab, the L-scale is 3 cm away and the evidence camera 12 cm.
  - The bell box's back must be within 3 mm of the cladding (0 mm expected).
  - The extinguisher must stand 2–12 mm off the skirting (6 mm expected), the clock within 3 mm of the wall (1 mm expected), and the diffusers flush within 3 mm.
  - The passer-by must stand at least 2.5 m outside the play area (about 2.80 m expected).
  - The menu-theme fade logs an Info line, not a failure, when the frame time is too long.
  - Unity allows reading imported meshes in Edit mode whatever their Read/Write setting. If a run logs "Not allowed to access vertices", the street figure and the apartment winding parts skip with an Info line; report it.
- **Skipped, each with its reason:**
  - **Moving the NitrileGloveBox:** HandInteractor.NearestTool ignores colliders that are not a tool, a rack sample or the camera, so a grab cannot be stolen. The two rack-sample checks now cover clicks and grabs; move the box only if one of them fails.
  - **Adding Opaque to "Fade and Cutout helpers set the Standard blend state":** it would rename an existing check. "Transparency comes from the MTL alpha, finishes from the pack data" already covers part of the opaque role: the ApartmentBlock's WindowFrames material stays below queue 2500.
  - **A real Grab() in the rack-sample check:** it would spawn a marker, and ToolStation.RemoveTool does not reset the marker numbering, so ToolStage's "Sequential marker numbers" would fail later. The check reads Grab's choice by reflection instead.
  - **The exact 8/3/3 picture-quad counts:** a brittle copy of the builder's spec list. The check wants one quad per NightInterior.Graphics record instead.
  - **A stale cached mesh after an atlas change:** this cannot happen in a fresh -executeMethod run, so the KitMesh fingerprint was fixed instead.
  - **Behaviour batch mode cannot observe:** audible playback, loudness and reverb wetness (no audio device; the reverb check only logs a linear wetness estimate); the Scene tab's drawing (OnGUI); the start menu's key handling (a second key press ignored, Esc quitting); the halos in both eyes and decal z-fighting. These stay on the PC lists.
  - **The player build's licence copy and its removal of stale notices:** they run only in a player build. The checks cover DemoBuild.LicenseFiles' naming and skip rules on a probe folder and on this project.
  - **Glass footsteps on the entrance doors' and right sidelight's floor fragments:** the glass zones do not cover all of them. The glass-zone check reports them (a known gap; the steps there stay hard).
  - **Left open (minor cleanups):** the area files each keep a private triangle-ray helper (StreetMeshRay, InteriorRay, DressingRay) and private footprint helpers, which could become one DemoValidation helper. StoreKit's private Transparent duplicates DemoAssetLibrary.Blended. (Fixture-detail round above, unverified: both done.)
- **Files added or changed** (the Downloads ZIP is synced to this batch: 59.1 MB, the six new area files added with fresh .meta guids, the 19 changed files updated):
  - New, each now in the ZIP with a fresh .meta guid: Prototype/Editor/NightStoreValidationStreet.cs, NightStoreValidationInterior.cs, NightStoreValidationDressing.cs, NightStoreValidationLighting.cs, NightStoreValidationPresenter.cs and NightStoreValidationSound.cs. They need the partial NightStoreValidation.cs below: with the 9305177 copy, the Editor scripts do not compile (CS0260).
  - Changed, keeping their existing .meta files:
    - Editor validators: NightStoreValidation (partial, the wiring), DemoValidation (LauncherChecks, SyncsBefore, FilesMissing, the credits-summary check), ShopCityValidation (the LauncherChecks call, the old-shop check).
    - Editor builders: DemoAssetLibrary, DemoGeometry, DemoBuild, KitMesh, NightAudioBuilder, NightDecals, NightInterior, NightLighting, NightStoreBuilder, NightStoreDressing, NightStoreLayout (a comment only), NightStreet.
    - Scripts: AmbientZones, DemoDesktopPanel, GlowBillboard, ProceduralMesh.
  - Outside the ZIP: Tools/sync-project.cmd (the header comment), Docs/HANDOFF.md and Docs/ROADMAP.md.
  - No shader, pack or scene file changed.
- **To verify on the PC:**
  1. **Compile.** Bring the six new files and the 19 changed ones into the project: sync the ZIP, or open through Open-Unity-Glass.cmd or Start-Demo.cmd, which mirror Prototype. The Console must show no compile error and no warning from these files. A CS0260 error means the project still has the 9305177 NightStoreValidation.cs, which is not partial.
  2. **NightStoreValidation.RunNightQuick with all twelve packs mirrored.** Expect +82 PASS lines over the October 15 figure (+108 over the count before 9305177), or +81 with the "Menu theme fade not checked" Info line. Record these Info lines from Docs/VerificationNight/runtime-results.txt:
     - "Street packs", "Lighting packs" and "Sound packs" (all present), "Realism packs present: 12 of 12" and the "Licence copy" line (the probe publishes A.txt, A_Dog.txt, B_Originals_License.txt);
     - the street furniture bases: grates -0.120 on -0.120, racks -0.120 on 0.000, the glove box 0.740 on 0.740;
     - the figure gap: PasserBy_Phone about 2.80 m;
     - the apartment block: x -3.80..11.91, z -26.00..-16.58, front slab about 226 m² facing the street against about 12 m² facing away;
     - the walk route: nearest collider Bollard_2 at about 1.73 m; the glove box about 0.038 m from the nearest sample; the bell box cladding about 0.000 m behind its back;
     - the extinguisher about 6 mm off the skirting; the clock about 1 mm off the partition; the lost-cat flyer centred near x 8.300 with every ray on the cladding, about 3 mm behind, and the nearest lamp post axis about 2.5 m away; the gilt moulding 30 mm off the wall;
     - "Glow halos per camera: ahead faced, beside faced, behind faced; main camera faced";
     - the "Reverb:" line: 4 zones, least across mid-depth 1.00, most along the pavement's inner edge 0.00 and 0.00 outside the door, plus the front corners' and side-wall cases' wet shares, for listening;
     - the credits line (attribution lines after the built-in one, with nothing missing or extra) and the case-evidence trail line.
  3. **If a FAIL line appears,** the run stops there. In Edit mode that is before the scene is saved. Report the FAIL line with the Info lines just above it. If a rack-sample check fails, move the NitrileGloveBox (NightStreet.Furniture) away from the samples.
  4. **NightStoreValidation.RunNight.** Expect +81 (+80 without the menu-theme fade), and the Info line "Night lighting baked: the quick preview's streetlight profile is not in use". The new Edit-mode checks run before the bake, so a failure there leaves no lightmaps.
  5. **Optional: a project unpacked from the ZIP alone, without a sync.** Expect +44 in RunNightQuick and +43 in RunNight, with "No realism pack in this project ..." and the per-pack "not present" Info lines.
  6. **ShopCityValidation.RunGlass and one other ShopCity run.** Expect +5 each (+4 without the packs). "The old shop has no night-store soundscape, music, footsteps, glow or presenter sets" must pass, and the old shop must look and sound as before.
  7. **StoreKitValidation.RunGallery.** This batch changes nothing there.
  8. **Desktop:** toggle the SCENE DRESSING switches and reset the session with the drawer open; the Console must log no GUI error. Take an evidence photo and look through the live viewfinder towards a lamp: the halos face the photo camera, not the head.
  9. **Headset (Quest 2 over Link):** the halos still face both eyes, and the per-camera billboard (six cards) adds no visible frame time.
  10. **Speakers and headset:** the reverb is full across the showroom (judge whether it is too wet) and absent on the pavement; the street stays muffled inside (the flat cutoff curve); the lost-cat flyer lies flat on the right pier.

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
    - Kit and people: a ForensicCase beside the equipment table and a box of nitrile gloves on it; one passer-by on his phone on the pavement left of the shop, 3 m outside the play area; a lost-cat flyer taped to the right pier's street face (in every build: the posts are too thin for the flat card).
    - Surfaces: the wet asphalt and the wet paving slabs carry their PBRExterior colour, normal, smoothness and AO maps, and the kerbs are concrete. Always-on decals: puddles, oil, wet leaves, litter and chewing gum on the street and both pavements.
    - Sound: a stereo city bed, the wind, the V8 idle, a car passing unseen every 20–60 s and a distant dog every 1–4 minutes (the CC0 recording only). No sirens and no radio.
  - **Shop front:** an alarm bell box on the fascia (silent at arrival); three stickers on the left sidelight (alarm monitored, insurer approved, cards accepted) and a CCTV sign on the left pier. The wind whistles through the forced doors. The piers and fascia keep the NightMaterials dark marble.
  - **Showroom:**
    - Surfaces from PBRInterior: a polished marble floor (glossier than before, with AO), warm plaster, walnut, marble tops, velvet, suede, brushed bronze, brushed stainless, black leather and a plasterboard ceiling.
    - The closing procedure: stock went to the safes by 17:45, so the cases hold display furniture only. Glass shards lie on the floor in front of the three broken cases. A pearl strand and a gold band lie on the floor where the burglars dropped them, clear of the walk route and the UV traces.
    - Props: snake plants in place of the front and rear-left planters, a rug under the sofa and side table, a fire extinguisher right of the office opening and a wall clock above it, and four air diffusers on the ceiling.
    - Wall graphics: Heda's still life in a gilt frame replaces the rear sign frame's emblem, and the Aurel & Finch lightbox replaces the counter sign frame's. The fire evacuation plan hangs on the rear partition, and Pissarro's Boulevard Montmartre, also gilt-framed, over the seating corner.
    - Sound: the street is muffled inside (low-passed to about 1.5 kHz and lowered to 55 %), under a room tone, with three Room reverb zones in a row across the showroom.
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
  - Shared helpers that replace private copies (follow-up fixes, see "Files changed or added"): `DemoAssetLibrary.WorldBounds` (the world box of a model's renderers, used by Seat, NightStreet.InPlayArea and the extinguisher's set-back); `DemoAssetLibrary.Blended` (the one no-shadow rule: a renderer whose materials are all blended casts none; PlacePack and NightStoreBuilder's shadowed kit groups use it, and NightStreet.NoTransparentShadows is gone); `DemoGeometry.PathUnder` (NightDecals, NightLighting); and `DemoGeometry.Opaque` (PackMaterial's opaque role, formerly private in DemoAssetLibrary).
- **PBR palette (StoreKit, StoreKitProps).** Each palette texture name has one source, in a fixed priority: the first set whose colour file exists supplies the colour, normal, MetallicSmoothness and AO maps and the tiling, so maps never mix across packs. The order is PBRInterior/PBRExterior, then ReferenceTextures (NightMaterials for the ceiling), then GeneratedTextures.
  - New materials: KitBrick (the left neighbour) and KitShutter (the half-closed shutters of the neighbours' and the across-street shopfronts). Stainless, Concrete, Render and Leather now take PBR sets. Six surfaces get AO maps: the floor, pavement, asphalt, render, brick and shutter.
  - FacadeStone stays the NightMaterials dark marble, on today's tint (`Calibrated = false`). GlassSmudge is not applied (it reads as fingerprints; Koda's decision 7).
  - The ReferenceTextures and generated sets keep their scalar finish. The NightMaterials 16 px constant maps equalled those scalars, so they look the same.
- **Street and interior (NightStreet.cs, NightInterior.cs, new; NightStoreLayout, NightStoreBuilder):**
  - `NightStoreLayout.LampProfile` holds the lantern (light at 3.27 m, baked intensity 3.5) and the KayKit lamp as its fallback. Each post keeps exactly one "Lens", sized to fit inside the glass, and every lantern renderer is ShadowCastingMode.Off.
  - The ApartmentBlock sits under Exterior/Buildings at StreetScale, matching its 6 texels/m import margin. Props whose footprint reaches a movement region get one bounds collider; renderers whose materials are all blended cast no shadows (`DemoAssetLibrary.Blended`, applied by PlacePack).
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
  - `NightDecals.Apply(root, folder, imported)`: NightStoreBuilder passes its useImportedAssets. With false no SurfaceDecals or ForensicModels file is read: no decal and no "Ambient wear" set are built, and "Case evidence" holds only the procedural stand-in tools.
- **Sound and music (NightAudioBuilder.cs, AmbientZones.cs, AmbientEvents.cs, Footsteps.cs, DemoMusic.cs, all new; DemoSoundBank, DemoSounds, AmbientSound, ToolHolder).** NightAudioBuilder replaces NightStoreBuilder.StagedSounds.
  - The sound bank prefers the NightSoundscape takes (the DSLR shutter, the torch click, the city bed as a 2D bed) and keeps NightAudio as the fallback. Cones and markers have their own set-down takes; every other tool keeps the thud.
  - AmbientZones muffles the exterior beds inside the showroom and office and plays HVACRoomTone_Loop indoors. Every bed in the zones starts at a random point. The door whistle, in neither zone list, starts at its beginning.
  - Reverb (AudioReverbPreset.Room): the 15.4 m showroom is wider than one zone can cover without reaching the street, so three zones (Reverb_ShowroomLeft, Reverb_Showroom, Reverb_ShowroomRight) stand in a row at mid-depth, each full within 3 m and silent beyond 4.5 m: at mid-depth the reverb is full from side wall to side wall, and the pavement stays dry. Reverb_Office is full within 1.5 m and none beyond 3 m.
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

  Audio at arrival (default PoliceLook, all packs): 5 looping beds (the city bed, the wind, the car idle, the room tone and the door whistle), one AmbientEvents scheduler with 2 one-shot sources (the car passes and the dog), 4 reverb zones (three across the showroom, one in the office) and 1 footstep source. The alarm bell and the underscore are off. The US preset adds the radio loop and the siren and radio sources.
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

  October 16 update: the validator batch above adds more on top of these. RunNightQuick is now +108 in all (+59 from the ZIP alone), RunNight +107 (+58), RunGlass and RunTextured +12, the other ShopCity runs +11 (each 1 fewer without the packs), and RunGallery stays +3. The October 16 entry has the breakdown.
- **Behaviours with no check yet** (now covered by the October 16 validator batch above). CLAUDE.md asks for a check for every new behaviour; these still have none and are the next validator batch: the street (lanterns and lamp profile, the ApartmentBlock facing the shop, the facade sets, the towers gone, figures at least 2.5 m outside the play area, furniture on its surface and clear of the walk route, colliders, the bell box); the lighting (halos and washes without lights, the panorama sky, the AcrossStreet probe, the NightSky cookies, the crossfade and steady modes); the interior (snake plants and rug with their fallbacks, the desk props clear of the tray, the monitor glow without a light, the partition fittings, the ceiling diffusers, wall-graphic size, facing and material count); the closing procedure (display-only cases, stock inside the safes, floor pieces clear of the route and the UV traces, glass zones); the decals (flags, cells, the trail, "No print-like or footwear-like mark is visible at arrival"); the soundscape, footsteps, set-downs, reverb, alarm bell and music; the presenter switches and their reset; the credits and the packs' licence notices; and the launcher mirror (Tools/sync-project.cmd's header still names a planned "all twelve packs are present" check that was not written).
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
  - The fixture-detail generator: a large procedural StoreKit pass of its own. (October 16 fixture-detail round, unverified: done.)
  - Sheen, specular anti-aliasing, torch beams, people's hands: custom shaders, particles and rigs, each needing headset checks. The metals are clamped meanwhile.
  - Pack torch models: the runtime torches work in both modes; pack meshes need prefab references and hand-pose checks.
  - Security hardware, procedure props, trim and trees: separate batches (only the bell box is added now). (October 16 fixture-detail round, unverified: the security devices, the trim and three bare trees are added; the procedure props and the roller grille are not.)
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
- **Files changed or added** (the Downloads ZIP was synced at commit 9305177 and again with the October 16 validator batch, which carries the follow-up fixes at the end of this list: 59.0 MB then, the 12 new .cs files added with fresh .meta guids, 26 changed files updated, the twelve packs left out):
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
  - Follow-up fixes after 9305177 (unverified, never compiled; in the ZIP since the October 16 sync, with their existing .meta files):
    - Editor: DemoAssetLibrary, DemoGeometry, NightStreet, NightLighting and NightStoreBuilder (the shared helpers above), NightInterior (the helpers and the lost-cat flyer moved from the first lamp post to the right pier, whatever the lamp), NightDecals (the helpers and Apply's imported flag), NightAudioBuilder (four reverb zones), KitMesh (explicit atlas UVs are part of a mesh's fingerprint, so a changed atlas gives a new mesh asset), DemoBuild (`AddMenuMusic`, the launcher's MenuMusic object, unchanged in behaviour), NightStoreDressing (JewelryStoreBuilder's geometric shoe print is no longer placed by the office opening, so no footwear mark shows at arrival; the procedural ring and ring-box stand-ins cast no shadows, like the pack pieces), NightStoreLayout (a comment on the KayKit post only).
    - Scripts: GlowBillboard (the cards turn to each camera as it renders, on Camera.onPreCull, so the evidence photo and the viewfinder see them facing too), DemoDesktopPanel (a presenter set destroyed since the last Layout event keeps its row, inert, until the next one), ProceduralMesh (the explicit-UV flag KitMesh reads), AmbientZones (each exterior bed's low-pass filter gets a flat cutoff curve, so the source's distance curve cannot override the zones' cutoff).
    - No new runtime or builder file, so no new .meta for those. The six new validator area files (NightStoreValidationDressing, Interior, Lighting, Presenter, Sound and Street; untracked, each needs a fresh .meta when synced) are now wired into the partial NightStoreValidation.cs. Copy them together with it. The October 16 entry above lists their checks and PASS counts; the figures in this entry's "To verify on the PC" leave them out.
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
     - the halos facing the viewer in both eyes without cutting the lamp housings, no z-fighting on the decals at grazing angles, no shimmer on the KitGem diamonds (KitGem is 0.85 since the October 16 fixture-detail round, unverified), and no visible LOD pops on the lanterns, the utility box and the passer-by;
     - no footsteps from room-scale walking, teleports or snap turns;
     - the frame time, judged only on a baked build.
  9. **Desktop (mouse and keyboard):** the drawer's Scene tab shows "Light bar flashing", the three SCENE DRESSING switches and "Underscore music", each working and each reset as described in CONTROLS_AND_INTEGRATION.md; footsteps every 0.7 m and glass steps in the glass zones; the menu theme fading before a map loads, a second key press ignored while it loads, and Esc quitting from an empty menu; the credits in the Settings tab and their first line on the start menu.
  10. **Audio levels, on speakers and in the headset:** the city bed (0.45), the wind (0.35) and the V8 idle (0.5 at the bonnet) outside; the muffled street and the room tone inside; the door whistle, the cone and marker set-downs, car passes and the dog; the Room reverb across the showroom and in the office, which may be too wet, and none on the pavement; no clicks where the loops wrap after the Vorbis re-encode; the menu theme at 0.25 (its README suggests 0.5), the underscore switch and the reset sting.
  11. **Visual walk on a baked build, desktop and headset:**
      - street: the lanterns upright with lit glass, the ApartmentBlock facing the shop (not mirrored, with lit rooms), the brick, render and shutter facades, the bus shelter's lightbox, the lost-cat flyer flat on the right pier;
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

Night store (October 11–16, unverified): NightStoreLayout (every anchor), NightStoreBuilder, NightStoreDressing (robbery state and the closing procedure), NightLighting, StoreKit/StoreKitProps/StoreKitDevices/KitMesh (fixtures, the wall and ceiling devices and the palette set table), PoliceLook (the one police setting), NightStreet, NightInterior, NightDecals, NightAudioBuilder and PackImport (import rules for the twelve realism packs), validated by NightStoreValidation and StoreKitValidation. Runtime additions: PresenterSet (presenter switches), GlowBillboard, AmbientZones, AmbientEvents, Footsteps, DemoMusic, LightBarBlink, NightQuality. Tools/sync-project.cmd mirrors Prototype into a project for Start-Demo.cmd and Open-Unity-Glass.cmd.

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
