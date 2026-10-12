# Tripo Studio guide — models for the jewelry store (October 12, 2026)

Researched October 12, 2026 (official help pages, the Tripo API/SDK source, the ComfyUI Tripo nodes, Tripo's blog and independent reviews; sources at the end). Tripo changes fast: **Studio always shows the credit cost before you click — trust that over this page.**

## The decisions in short

1. **One object per generation** for everything you will see up close or touch (tools, cases, safes, busts, furniture). Tripo's "4 in one run" (P2.0 multi-version) gives **four versions of the same object** at different polycounts, not four different objects. A sheet of 4–5 different tools tends to fuse them, and they then share one face budget and one texture.
2. **Exception — clutter sets:** four inputs are deliberately groups of small background items (`display_risers`, `shopping_bags`, `transport_trays`, `dropped_jewelry`). Generate each as one model and split it afterwards (Blender: *Mesh → Separate → By Loose Parts*, or Tripo *Segmentation*). That is where several objects per generation pays off.
3. **Use the input images, not text, wherever one exists.** Image-to-3D follows your reference designs; text-to-3D invents its own. Ready-made inputs (one centred object, 1024 px, labels removed, light objects on grey because white-on-white causes holes) are in [`Docs/TripoInputs/`](TripoInputs/). Text prompts below are for items without an image, or as a fallback when an image result fails.
4. **Two model tiers to stay inside a Pro month (3,000 credits):**
   - **P2.0** (newest, Sept 2026; ~100 credits per run, up to 4 polycount versions in that run on Pro/Max) for hard-surface hero props: camera, safes, display/wall case, counter, drawer unit, desk, flashlights, tape stanchion, scene lamp, security camera.
   - **H3.1 + Smart Low-poly** (~25 + 10 texture + 20 low-poly ≈ 55 credits) for simple or small items: marker, cone, busts, trays, boxes, stands, sofa, side table, terminal, keypad, track light, clutter sets.
   - Total for everything below ≈ 2,400–2,700 credits: fits one Pro month with a little room for re-rolls.
5. **Make the final models on a paid plan.** Tripo's own help page says free-tier outputs carry no commercial rights (other Tripo pages say CC BY 4.0 — they contradict each other, so avoid the question). Record each model in `Docs/ASSET_REGISTER.md` as "Tripo AI generated, paid plan".
6. **Leave out what Tripo does badly:** glass (build panes in Unity — the display-case input already has none), chrome, thin chains and straps, text and logos (decals in Unity), hinged parts as one mesh (generate open, then separate the door/lid).

## Default settings (use these unless a prompt says otherwise)

| Setting | Value | Why |
|---|---|---|
| Mode | **Image to 3D** with the input from `Docs/TripoInputs/` | follows the agreed design |
| Model | **P2.0** for hero props, **H3.1** (v3.1) with **Smart Low-poly** for small/simple props | quality where it shows, credits saved elsewhere |
| Polygons / face target | as given per item (triangles) | VR budget; P2 treats it as a target, H3.1 without a limit makes 1–2 million faces |
| Topology | **Triangles** (Quad off) | Unity uses triangles; quads only help if you will edit in Blender (quad output is FBX, max 25k) |
| Texture | **Standard (2K)**; **Detailed (4K)** only for the camera, safes and display case | 2K is plenty in a headset; 4K costs memory |
| PBR | **On** | metallic, roughness and normal maps (repacked for Unity on import, see below) |
| Texture alignment | **Original image** | keeps the reference colours |
| Orientation | **Align to image** | upright like the input |
| Auto-size / real-world scale | **On** if Studio offers it | metres, matches the scene |
| Seeds | leave random; when a result is close, keep the seed and only re-roll the texture | cheaper than a full re-generation |
| P2.0 versions (Pro) | **2 versions**: the target and about half of it | pick the lighter one if it looks the same in VR |

Image inputs need no prompt (image-to-3D has no prompt or negative prompt). For text-to-3D: max 1,024 characters, subject first, then shape, material, details, style; keep it to one object.

## Workflow per model

1. Open the input image (or paste the text prompt), apply the settings above and the item's face target.
2. Generate. Check from all sides: flat base, closed back, no fused extra parts, straight edges.
3. Close but wrong texture → *Texture Gen* / re-texture (~10–15 credits) instead of a new generation. Small geometry fault → *Mesh Edit* (P2.0) or *Magic Brush* for paint.
4. Hinged objects (safes, jewellery boxes): *Segmentation* (~40) to split door/lid, or do it in Blender.
5. **Export: FBX**, textures embedded or alongside as PNG, scale 1, **pivot at bottom centre** if offered, **bake on**. (GLB also works through `Tools/glb_to_obj.py`, but that keeps only the base colour.)
6. Name the file exactly as listed (`snake_case`) and put it in `Prototype/ThirdParty/StoreKit/` with its textures; add a row to `Docs/ASSET_REGISTER.md`.
7. Unity: StoreKit swaps it in automatically for the names marked **auto** below; the others (cases, safes, doors, jewellery props, tools) need a small code change per family that keeps their hinges, glass panes and states — ask Claude when the models exist. Tripo's metallic/roughness maps need repacking for the Standard shader (metallic in R, smoothness = 1 − roughness in A); Claude can add an importer that does this automatically.

---

## Prompts and settings

Face targets are triangles. "Input" is the file in `Docs/TripoInputs/`. "Text" is for text-to-3D or as a fallback.

### Batch 1 — scene tools (seen in the hand, highest priority)

**1. `evidence_marker`** — H3.1 + Smart Low-poly, target **1,500**, Standard texture
Input: `evidence_marker.jpg`
Text: `Yellow plastic forensic evidence marker, folding A-frame tent shape, two flat panels joined at a top hinge, small feet at the base, opaque matte yellow plastic, slightly rounded edges, no number printed, clean product model`
Note: numbers are added in Unity, so blank panels are fine (the input shows "1"; ignore it if Tripo bakes it into the texture).

**2. `traffic_cone`** — H3.1 + Smart Low-poly, target **2,000**, Standard
Input: `traffic_cone.jpg`
Text: `Orange traffic safety cone with a square black rubber base, one wide white reflective band, matte plastic, hollow top opening, 45 cm tall, clean product model`

**3. `tape_stanchion`** — P2.0, target **3,000** (2 versions: 3,000 / 1,500), Standard
Input: `tape_stanchion.jpg`
Text: `Retractable belt barrier stanchion post, round weighted black base, slim black metal pole 95 cm tall, yellow belt cassette head with a dark slot at the top, matte finish, no belt pulled out`

**4. `dslr_camera`** — P2.0, target **8,000** (2 versions: 8,000 / 4,000), **Detailed (4K)**
Input: `dslr_camera.jpg`
Text: `Unbranded black DSLR camera with a zoom lens attached, textured rubber grip, pentaprism hump with hot shoe, mode dial and shutter button on top, ribbed lens rings, dark glass front element, matte black plastic and rubber, no logos or text`

**5. `flashlight_white`** — P2.0, target **3,000** (2 versions: 3,000 / 1,500), Standard
Input: `flashlight_white.jpg`
Text: `Black tactical LED flashlight, cylindrical aluminium body with knurled grip, wider head with silver reflector and clear lens, rubber tail switch, matte anodized black, 16 cm long`

**6. `flashlight_uv`** — **no new generation**: open `flashlight_white`, *Texture Gen* with the prompt `same flashlight, lens ring anodized purple, deep violet lens glass` (~10–15 credits). Same mesh, so it swaps cleanly.
Fallback input: `flashlight_uv.jpg`.

**7. `scene_lamp`** — P2.0, target **6,000**, Standard
Input: `scene_lamp.jpg`
Text: `Portable LED light panel on a black aluminium tripod stand, rectangular panel with a grid of small LEDs and a white diffuser face, yoke bracket, telescopic centre column, three thin legs with rubber feet, about 1.6 m tall`
Note: thin tripod legs are a Tripo weak spot; if they break, keep the panel and let Unity draw the legs.

### Batch 2 — store fixtures (biggest visual gain)

**8. `display_case`** — P2.0, target **8,000** (2 versions: 8,000 / 4,000), **Detailed (4K)**
Input: `display_case.jpg` (glass deliberately absent)
Text: `Jewelry store display counter base without glass, dark walnut wood cabinet with recessed plinth and an inset front panel, cream suede display deck on top, thin brushed bronze frame posts and top rails forming an empty open box above the deck, 1.8 m wide 0.65 m deep 1.05 m tall`

**9. `wall_case`** — reuse `display_case` (same module against the wall in Unity). No generation.

**10. `counter`** — P2.0, target **4,000**, Standard — **auto**
Input: `counter.jpg`
Text: `Jewelry shop checkout counter, dark walnut wood cabinet with framed front panel and recessed plinth, cream limestone top, 1.8 m wide 0.6 m deep 0.95 m tall`

**11. `safe_large`** — P2.0, target **10,000**, **Detailed (4K)**, then Segmentation to separate the door
Input: `safe_open.jpg` (open, so the inside is modelled); `safe_closed.jpg` for a closed variant if wanted
Text: `Heavy black steel jewelry store safe with its door open, thick walls, two interior shelves with bronze-brown lining, door hinged on the right with three round locking bolts on its edge, bronze inner door panel, short feet, textured matte black powder coat`

**12. `safe_tall`** — generate later only if the room needs a second shape; otherwise use `safe_large` scaled in Unity.

**13. `drawer_unit`** — P2.0, target **5,000**, Standard — **auto**
Input: `drawer_unit.jpg`
Text: `Walnut wood three-drawer office chest, middle drawer pulled open, long bronze bar handles, small keyholes, bronze plinth band at the base, 0.9 m wide 0.75 m tall`

**14. `office_desk`** — P2.0, target **4,000**, Standard — **auto**
Input: `office_desk.jpg`
Text: `Simple walnut wood office desk with four tapered legs and one front drawer slightly open, 1.4 m wide 0.7 m deep 0.75 m tall`

**15. `wall_display`** — H3.1 + Smart Low-poly, target **3,000**, Standard
Input: `wall_display.jpg`
Text: `Tall open wall display cabinet, walnut wood with brushed bronze edge trim, three empty shelves, no items on the shelves, 0.9 m wide 0.4 m deep 2.1 m tall`
Note: the input shows jewellery on the shelves; delete those parts or re-texture — the robbed store wants them empty.

### Batch 3 — jewellery display props (many copies in the scene; keep them light)

**16. `necklace_bust`** — H3.1 + Smart Low-poly, target **1,500**, Standard
Input: `necklace_bust.jpg`
Text: `Jewelry necklace display bust, smooth neck and shoulders form on a rectangular base, covered in charcoal grey velvet, no necklace, 30 cm tall`

**17. `velvet_tray`** — H3.1 + Smart Low-poly, target **800**, Standard
Input: `velvet_tray.jpg`
Text: `Rectangular jewelry display tray with three rounded ring rolls, charcoal velvet, low raised rim, 35 cm by 25 cm`

**18. `ring_tray`** — H3.1 + Smart Low-poly, target **800**, Standard
Input: `ring_tray.jpg`
Text: `Walnut wood jewelry tray with four cream suede ring rolls, low rim, 30 cm by 22 cm, empty`

**19. `bracelet_stand`** — H3.1 + Smart Low-poly, target **800**, Standard
Input: `bracelet_stand.jpg`
Text: `Bracelet display T-bar stand, horizontal charcoal velvet roll on a short bronze post, rectangular walnut base`

**20. `earring_stand`** — H3.1 + Smart Low-poly, target **300**, Standard
Input: `earring_stand.jpg`
Text: `Earring display board, upright charcoal velvet panel with small holes, leaning slightly back on a walnut wedge base`

**21. `jewelry_box` family** — H3.1 + Smart Low-poly, target **800** each, Standard; Segmentation (or Blender) to separate the lid
Inputs: `ring_box.jpg`, `bracelet_box.jpg`, `necklace_box.jpg`, `jewelry_box_closed.jpg`
Text (ring box): `Small open ring box, dark charcoal leatherette outside with a thin bronze trim line, cream velvet interior with a ring slot, lid open at 100 degrees, 6 cm cube`

### Batch 4 — clutter sets (several objects per generation, split afterwards)

**22. `display_risers`** (3 cubes) — H3.1 + Smart Low-poly, target **600**, Standard. Input: `display_risers.jpg`.
**23. `shopping_bags`** (3 bags) — H3.1 + Smart Low-poly, target **1,500**, Standard. Input: `shopping_bags.jpg`. Thin handles may fuse; acceptable for background.
**24. `transport_trays`** (2 stacked) — H3.1 + Smart Low-poly, target **600**, Standard. Input: `transport_trays.jpg`.
**25. `dropped_jewelry`** (rings and a bangle) — H3.1 + Smart Low-poly, target **1,500**, Standard. Input: `dropped_jewelry.jpg`. Thin rings are a weak spot; if they come out lumpy, keep the procedural rings.

### Batch 5 — furniture and fittings (auto-swapped)

**26. `waiting_sofa`** — H3.1 + Smart Low-poly, target **5,000**, Standard — **auto**
Input: `waiting_sofa.jpg`
Text: `Two-seat waiting room sofa, charcoal grey woven fabric, square arms, two seat and two back cushions, walnut wood plinth and short tapered legs, 1.7 m wide`

**27. `side_table`** — H3.1 + Smart Low-poly, target **2,000**, Standard — **auto**
Input: `side_table.jpg`
Text: `Low rectangular coffee table, cream marble top, walnut wood legs, thin brushed bronze stretchers and feet, 1.0 m by 0.55 m, 0.45 m tall`

**28. `security_camera`** — P2.0, target **2,500**, Standard — **auto**
Input: `security_camera.jpg`
Text: `Wall-mounted CCTV security camera, light grey metal housing with sun visor, dark lens, articulated wall bracket with square mounting plate and screws`

**29. `track_light`** — H3.1 + Smart Low-poly, target **1,500**, Standard — **auto**
Input: `track_light.jpg`
Text: `Ceiling track light, straight black rail with three black cylindrical spotlights on short bronze stems, lights off`

**30. `pos_terminal`** — H3.1 + Smart Low-poly, target **1,000**, Standard — **auto**
Input: `pos_terminal.jpg`
Text: `Point-of-sale touchscreen terminal, black tilted display on a short stand with a flat base, screen off, no logos`

**31. `alarm_keypad`** — H3.1 + Smart Low-poly, target **500**, Standard — **auto**
Input: `alarm_keypad.jpg`
Text: `Wall-mounted alarm keypad, brushed grey metal box, small dark display at the top, three by three dark rubber buttons, no text`

### Items without an input image (text-to-3D)

**32. `office_chair`** — P2.0 text-to-3D, target **4,000**, Standard
`Black mesh office swivel chair, padded seat, mid back, fixed armrests, gas lift column, five-star base with castors, matte black plastic and fabric`

**33. `shelving_unit`** — H3.1 + Smart Low-poly text-to-3D, target **2,000** — **auto**
`Dark grey metal storage shelving unit, four open shelves, 1.0 m wide 0.4 m deep 1.9 m tall, plain cardboard archive boxes on the shelves, matte powder coat`

**34. `locker_cabinet`** — H3.1 + Smart Low-poly text-to-3D, target **800** — **auto**
`Tall cream metal office storage cabinet with one door and a vertical bar handle, 0.6 m wide 0.5 m deep 1.8 m tall, matte painted steel`

**35. `planter`** — H3.1 + Smart Low-poly text-to-3D, target **3,000** — **auto**
`Square cream ceramic planter with a lush green indoor plant, broad glossy leaves, 1 m tall overall`

**36. `police_car`** — **wait** for the country and livery decision (see "for Koda.md"). Then P2.0 text-to-3D, target **15,000**, Detailed: `Modern [country] police patrol sedan, [livery colours and markings in words, no readable text], light bar on the roof, separate wheels`. Text and badges go on as decals in Unity.

Skip for now (procedural versions are good enough and Tripo is weak at them): doors and window modules (glass, hinges), exit sign (text), downlight, bollard, L-scale, measuring reel.

## Credits plan (one Pro month)

| Group | Items | Approx. credits |
|---|---|---|
| P2.0 hero props | 3, 4, 5, 7, 8, 10, 11, 13, 14, 28, 32 | 11 × 100 = 1,100 |
| H3.1 + Smart Low-poly | 1, 2, 15–27 (jewellery boxes ×4), 29–31, 33–35 | about 24 × 55 = 1,320 |
| Re-textures, segmentation, re-rolls | 6, 11, 21 and fixes | about 200–300 |
| **Total** | | **≈ 2,600–2,700 of 3,000** |

Prices from Tripo's help page (Studio): Generate 25, Texture +10, HD texture +10, Smart Low-poly +20, Quad +5, Generate in Parts +30, Segmentation 40, Texture Gen 10 (+5 PBR, +10 HD); P2.0 ≈ 100 per run. Studio shows the exact cost before each action.

## Sources

- Tripo P2.0 launch and September update: tripo3d.ai/blog/tripo-september-update, tripo3d.ai/blog/tripo-p2-0-preview, PR Newswire 2026-09-21
- Credit table: tripo3d.ai/help/credit/how-do-credits-work · Plans: tripo3d.ai/pricing
- Commercial use: tripo3d.ai/help/privacy-policy/how-to-use-tripo-models-commercially
- Image input tips: tripo3d.ai/help/features/how-to-get-better-image-to-3d-results · Prompting: tripo3d.ai/blog/text-to-3d-prompt-engineering
- Settings and limits verified in code: github.com/comfyanonymous/ComfyUI (comfy_api_nodes/nodes_tripo.py), github.com/VAST-AI-Research/tripo-python-sdk
- Independent tests: digitalproduction.com (P2.0 quad test, 2026-09-29), dev.to Unity pipeline review
- Unity performance targets: developers.meta.com/horizon/documentation/unity/unity-perf

Open points the research could not settle: whether Studio enforces a 500-face minimum on P2.0, the exact Studio labels for multi-version, the Team plan price, and whether Studio exposes pivot and texture-size options in its export dialog.
