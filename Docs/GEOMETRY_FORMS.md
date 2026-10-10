# Geometry forms pass — October 10, 2026

Status: source implemented; Unity compilation, generated scene and headset performance are **not yet verified**. Remote commands stalled. Existing Downloads ZIPs do not contain this refinement.

## Geometry

Four display cabinets get chamfered corners, recessed toe-kicks, shallow end panels, drawer reveals and pulls. Necklace mounts become shaped shoulder-and-neck forms instead of spheres. Display pads get beveled outlines. Stock on the inactive intact overlay is refined too; evidence positions and state ownership are preserved.

Shallow framed wall niches, rear branding borders/ribs, a safe-room door surround and deeper storefront sill/header add depth. West niches avoid the existing photograph frame. Parked cars receive tapered bodies, sloping cabins and eight-sided wheels.

The new RunForms scene skips imported city models, lanterns, shop dressing and the imported cone; it uses procedural fallbacks with flat colors. No new texture maps, image generation, downloads, realtime lights or decorative colliders. Existing glass, text and runtime photographs remain functional.

## Cost and verification

Only the new opaque decorative groups are combined, separately per cabinet and wall, to preserve spatial culling. Colliders, interactive objects and intact/robbed state roots are not merged.

The new budget check enforces <=5,000 triangles and <=32 renderers for FormDetails and verifies no colliders, lights, textures or transparent materials within that root. This is an added-detail budget, not a complete-scene or FPS measurement. Existing city-budget checks also run.

Mesh counts from the loft algorithm: cabinet 64; plinth 48; bust 80; tray 48; car body 64; cabin 32; wheel 32 triangles. Independent mathematical checks found no zero-area or inward-facing triangles for those seven profiles. These checks do not establish Unity compilation, visual quality, persistence or VR performance.

## Generate

Use current main source together. In a separate Unity project copied from a snapshot, synchronize Prototype/Editor and Prototype/Scripts into Assets/CrimeSceneDemo/Editor and Scripts, preserving matching .meta files. Remove source files deleted from main so older duplicate implementations are absent.

Run Unity 6000.6.2f1:

```text
Unity.exe -batchmode -noUpm -projectPath "<isolated project>" -executeMethod ShopCityValidation.RunForms -logFile "<log path>"
```

Do not add -quit: validation exits after its Play mode checks. This generates a fresh scene, saves Assets/CrimeSceneDemo/Scenes/JewelryStoreForms.unity, and writes captures and results into Docs/VerificationForms alongside the project. It runs the existing interaction/reset/intact-state checks plus the new geometry budget checks.

For an already expanded scene, Crime Scene Demo > Refine Store Forms applies the shop pass once. It does not remove imported assets already present; use RunForms for the complete procedural environment.

Next: compile, inspect the actual showroom/display/street renders, test the intact toggle and safe-room route, measure total rendering statistics and Quest frame time, then publish a matching project ZIP. Keep the verified review snapshot available meanwhile.
