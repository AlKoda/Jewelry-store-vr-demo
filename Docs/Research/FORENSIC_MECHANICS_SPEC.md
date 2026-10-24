# Forensic mechanics specification (research to simulation)

Written October 24, 2026 by Claude from the research report [Crime scene forensic procedures.md](Crime%20scene%20forensic%20procedures.md) and its six notes files in [Notes/](Notes/). It says how the researched practice becomes tools, surfaces, step sequences and training scoring in the night jewellery store. It is the specification the fieldwork code follows; every rule below names the note it comes from so the content can be checked against the primary source later (the researchers read sources through search extracts, not full PDFs: see the caveat at the top of the report).

The governing direction is Koda's unguided sandbox (Docs/NEXT_SESSION_NOTES.md): no quests, no hints, no gating. The simulation therefore does not tell the trainee what to do. It makes every tool work on every compatible surface, records what was done in what order (the exhibit record and a procedure log), and lets the instructor or a review screen judge the sequence against the checklist in section 6. Nothing identifies a suspect.

## 1. Principles carried into every mechanic

| Principle | Source note | How the simulation applies it |
|---|---|---|
| Least intrusive first: visual search and photography before anything that alters the item; transient and fragile evidence first; biological and trace before powder and chemicals (NIJ 2013 walk-through and prioritisation) | scene_management §2, biological_trace §4 | Every clue keeps an ordered procedure log. Powdering before a photo, or powdering an item that still carries un-swabbed biological material, is recorded as an out-of-sequence step (not blocked). |
| Locard: every contact leaves a trace; cordons, common approach path, scene log, PPE and glove changes exist to control it | scene_management §1, §3 | PPE items (gloves, mask, overshoes) are dressing in the kit; a glove-change action between items is logged when the trainee performs it (optional). The scene log is the register. |
| Photograph before lifting, with a scale, camera axis at 90 degrees (Illinois SP, WSP, Robinson) | fingerprints §4, documentation_burglary §1 | The evidence camera already exists. A developed mark lifted without a photograph in the log is an out-of-sequence step. The L-scale placed within 0.25 m of the mark before the photo counts as "with scale". |
| Chain of custody: unique exhibit reference, who, when, where, sealed container | scene_management §4, biological_trace §8 | Collecting a sample seals a packet and writes an entry (id, title, action, UTC time, position). The entry gains the method used and the container type. |
| Powder contrast and surface class decide the method (HOSDB 2007: aluminium with glass-fibre brush on glass and smooth surfaces; black magnetic on textured plastic; not magnetic on steel) | fingerprints §3 | Surfaces carry a class. The chosen powder and applicator against the class are logged as correct, acceptable or wrong. |
| Over-brushing and too much powder destroy ridge detail ("less is best", stop when developed) | fingerprints §3 | Brushing continues to add coverage past full development and then degrades the quality of the mark; a degraded mark still lifts but records a lower quality grade (CAST 0 to 4). |
| Swabbing a surface for DNA after powdering recovers less; photograph the mark and then swab, or gel-lift to recover both (HOSDB 2007, Staffordshire trials) | documentation_burglary §5 | A handle that carries both a latent mark and touch DNA records which came first; a nylon swab over an undeveloped mark destroys the mark; a gel lift of a developed mark leaves DNA recoverable. |
| Blood presumptive tests are two-stage and have a time window; luminol is sprayed from about 50 cm in darkness | biological_trace §1, §2 | The KM test is a three-drop sequence on a filter-paper rub of the stain; a colour before the peroxide drop is an invalid test. The reagent sprayer only shows a glow with the torches off and the store dark. |
| Casting: dental stone 2 lb to 8-10 oz water, set 25-30 min before lifting; Mikrosil sets in 6-10 min; never fit a suspect tool into the mark | impressions_toolmarks_glass §1, §2 | Casting is a pour-and-wait step with a visible timer on the cast; the burglar tools cannot be held against a tool mark while it is unrecorded (a held tool near a mark logs an error). |
| Nothing at the scene identifies a person; comparison happens in the laboratory (ACE-V, elimination prints) | fingerprints §7 | Record texts say "comparison required". The register never names a suspect. |

## 2. Surfaces and traces in the night store

Where the researched evidence types already exist in the scene, and where new ones go (authoring lives in `Prototype/Editor/NightStore/PortableFieldwork.cs` and `InvestigationGameplay.cs`).

| Scene location | Trace | Surface class | Methods that work | Notes / source |
|---|---|---|---|---|
| Entrance door pull bars | Latent marks (two developable, existing) and touch DNA | Non-porous metal, curved | Powder (aluminium or black on dark bar) with brush or wand; tape or hinge lift; swab | Existing `DustablePrint` marks; add the DNA layer so the order (photo, lift, then swab, or swab first) is logged |
| Door glass remnants and broken bay edges | Fibres snagged on edges, blood from a cut, latent marks on glass edges | Glass, sharp | Forceps or tape lift for fibres; KM test then swab for blood; powder (aluminium) for marks | impressions_toolmarks_glass §4; biological_trace §5 |
| Display-case glass tops and sides | Latent marks (random field, 60 % of panels carry 0 to 3 marks; some are smudges with no ridge detail) | Glass | Aluminium powder with glass-fibre brush; magnetic wand acceptable; black powder acceptable with a dark backing | New `LatentMarkField` component so brushing any case can find marks, not only designated ones |
| Display-case frames and the office safe | Pry marks (impressed and striated) | Painted metal | Photograph with scale, Mikrosil cast; recover whole item is not available in VR | impressions_toolmarks_glass §2 |
| Floor by the smashed case and the entrance | Footwear marks in glass dust (two-dimensional) | Dusty floor | Oblique torch light to find; photograph with scale at 90 degrees; gel lifter (black) or electrostatic film | impressions_toolmarks_glass §1 |
| Sandy verge or planter outside | Three-dimensional footwear impression | Soil | Photograph with scale, dental-stone cast, wait, lift, mark the back | impressions_toolmarks_glass §1 |
| Counter and showroom floor | Dropped items: work gloves, balaclava, cigarette butt, lighter, spectacles, bat, toolbox | Items | Photograph in place with a marker; forceps or gloved pick-up into a paper bag or box; swab the glove interior later (lab) | biological_trace §6 |
| Office floor | Papers and documents | Porous paper | Powder is the wrong method (log a wrong-method step): paper goes to the laboratory for ninhydrin or DFO; photograph and bag in paper | fingerprints §6 |
| Any surface | Red-brown stains (two real, one paint false positive) | Any | KM test (positive, negative, invalid); reagent spray in the dark | biological_trace §1, §2 |

## 3. Tools and consumables (what the hand holds)

The existing `FieldInvestigationTool.Method` set is Forceps, Swab, Brush, LiftCard. It grows to the set below. Each entry names the pack model that dresses it (ForensicModels, October 24 additions) and what it can act on.

| Method | Model | Acts on | Result |
|---|---|---|---|
| Brush (glass-fibre, with the powder jar) | FingerprintBrush + FingerprintPowderJar | Non-porous surfaces | Adds coverage to latent marks under the stroke; over-brushing degrades |
| MagneticWand | MagneticWand | Textured plastic and non-porous surfaces, not steel | Same as Brush, slower on glass, correct on textured, wrong on steel |
| Tape (lifting tape, with a backing card) | LiftingTapeRoll + BackingCardWhite/Black | Developed marks on flat or gently curved surfaces | Produces a LiftCardWithPrint exhibit; a dark card for aluminium powder, a white card for black powder |
| HingeLifter | HingeLifterBlack/White | Developed marks | Same as Tape in one piece |
| GelLifter | GelLifterBlack/White | Developed marks and dust footwear marks | Lifts ridges or a dust mark and leaves DNA recoverable |
| Swab (wet, then dry) | SwabTube, SwabOpen, DistilledWaterAmpoule, SwabBox | Handles, tools, glass edges, blood | Seals a swab box; order relative to powder is logged |
| KMTest | KMTestKit | Red-brown stains | Three-drop sequence; positive turns pink within the window |
| ReagentSpray | ReagentSprayBottle | Floors and walls in darkness | Shows latent blood traces (existing UV trace shader reused with a blue-white tint) |
| CastingKit (dental stone) | DentalStoneBag, MixingBowlRubber, CastingFrame, ShoePrintCast | Three-dimensional impressions | Pour, wait, lift the cast exhibit |
| MikrosilKit | MikrosilTube, MikrosilCatalyst, MixingSpatula, ToolMarkCast | Tool marks | Spread, wait, peel the cast exhibit |
| Forceps | existing | Fibres, hairs, butts, small items | Into a paper fold or bag |
| Packaging | EvidenceBagPaper, EvidenceBagPlastic, EvidenceBox, EvidenceTag, EvidenceLabelRoll, SwabBox | Any collected exhibit | Container type recorded (paper for biological, box for sharp, tamper-evident plastic for dry items) |
| Scale and markers | ForensicScaleL, EvidenceMarkerTent | Photography | "With scale" flag on photos within 0.25 m |
| Protection | NitrileGloveBox, FaceMask, ShoeCoverPair, OrangeGoggles, SteppingPlate | Dressing and optional logged actions | Glove change logged between items when used |

## 4. State machines per evidence type

Each clue owns a `ProcedureLog`: an ordered list of (step, tool, UTC, correct / acceptable / out-of-sequence / wrong-method). Steps are never blocked except where physics makes them impossible (a lift before development lifts nothing).

**Latent mark (powder route).** Hidden → Searched (oblique torch within 0.4 m, 2 s) → Developed (coverage reaches 1.0 by brushing; keeps counting to 1.6 where quality drops from grade 4 to 2) → Photographed (camera within 0.6 m, mark in frame; scale nearby sets the flag) → Lifted (Tape, HingeLifter or GelLifter; produces the lift exhibit; contrast rule checked) → Packaged and recorded. Swabbing the same surface before Developed is correct for DNA and destroys the mark (nylon swab), which the log records as the choice made, not as an error, unless no photograph of the location exists.

**Dust footwear mark.** Hidden → Searched (oblique light) → Photographed with scale at 90 degrees (camera pitch within 15 degrees of the surface normal) → Lifted (GelLifter black or ElectrostaticLiftFilm) → Recorded. Walking over an un-recorded mark (player capsule crosses it) logs "mark trodden on".

**Three-dimensional impression.** Photographed with scale → Framed (CastingFrame placed) → Poured (CastingKit; mixture needs the bowl filled with water first, logged as the ratio step) → Setting (30 real seconds stands for 30 minutes; the cast shows a timer texture) → Lifted → Marked (initials, date, exhibit number written automatically on the back when bagged).

**Tool mark.** Photographed with scale (normal and oblique light flags) → Cast (MikrosilKit; 8 real seconds) → Peeled → Recorded. A burglar tool brought within 5 cm of an un-cast mark logs "suspect tool offered to mark".

**Red-brown stain.** Observed → Tested (KMTest: swab rub, ethanol drop, reagent drop, 10 s pause, peroxide drop; pink within 10 s is positive, pink before the peroxide is invalid; paint stays white) → Swabbed (wet swab then dry swab; control swab from a clean area is a bonus step) → Packaged in paper. The reagent sprayer in darkness reveals the two latent blood traces (existing floor trace pair) and nothing on the paint stain.

**Dropped item.** Photographed in place with a marker → Picked (forceps or gloved hand) → Bagged (paper for porous or biological, box for the bat and the toolbox) → Recorded. Picking without a photograph logs out-of-sequence.

**Paper document.** Photographed → Bagged in paper. Powdering a document logs wrong-method (porous surface).

## 5. Scoring for training (review screen, not in-world hints)

Points are not shown while working. The register's last page, or the PC presenter panel, lists per clue: steps done in order, flags, quality grade. Suggested rubric, from the mock-scene rubrics in equipment_training §2 (Staffordshire 40/30/20/10 weighting, Western TC checklist):

| Weight | Criterion | Counted from |
|---|---|---|
| 40 % | Evidence recovered: fraction of traces found, developed and collected with a usable grade | ProcedureLog end states, mark quality |
| 30 % | Sequence and method: photo before lift, biological before powder, right powder for the surface, right container | out-of-sequence and wrong-method flags |
| 20 % | Documentation: photos with scale, marker present, exhibit entries complete | camera flags, register entries |
| 10 % | Scene discipline: no mark trodden on, no suspect tool offered to a mark, glove changes between items | discipline flags |

## 6. Training checklists (from the report, section "Training checklists")

The report carries the per-evidence checklists in the words of the sources; the mechanics above implement the steps in bold in the report. Where the simulation compresses time (casting, Mikrosil, KM window) the compression is stated to the trainee in the field guide card inside the case lid.

## 7. Implementation plan

Phase 1 (this batch, unverified in Unity): the models and decals in ForensicModels; this specification; code for multi-method clues (`EvidenceClue.Methods`, `SurfaceClass`), the `ProcedureLog`, over-brushing quality, the magnetic wand, tape and gel lifts, swab-order logging, the KM test, dental-stone and Mikrosil casting, dust footwear marks, the latent-mark field on display cases, dropped-item bagging; authoring of the new tools in the portable case and the new traces in the night store; validator checks for each state machine.

Phase 2: reagent spray in darkness with photography settings, electrostatic lifting, a glove-change action, stepping plates that matter, the review screen with the rubric, Arabic text for every new record string, and the headset test of reach and two-hand workflows.

Out of scope by Koda's direction: quests, hints, progression gates, suspect identification.
