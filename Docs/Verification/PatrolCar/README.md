# Tripo patrol car refinement — October 26, 2026

The supplied textured Tripo sedan is integrated into the existing night scene.
The original colour JPEG remains byte-identical (SHA-256
`BC49A14385E5B319B64E90A8A55AF9E0A833EE78206DA34390D6C1D608BA9C8B`).
Original GLB/FBX and the previous plain car are retained. Local pre-change scene
checkpoints: `../Backups/Before-Tripo-Car-20261025` and `../Backups/Before-Car-Closeup-20261026`.

Delivered changes:

- Native correctly oriented POLICE / الشرطة door artwork and ROP emblem;
  original garbled lettering is replaced only within its region, with paint
  sampled from four unlettered positions per side. No floating logo rectangle.
- Number-free Arabic الشرطة plates, front and rear.
- Separate physically based paint, glazing, tyre, alloy, brake, trim and lens
  finishes. Original paint/trim/lamp colour detail remains. Dark exterior glazing
  reflects the scene; no false transparent view into an empty cabin.
- Four native wheel assemblies with curved tyre shoulders/tread grooves,
  ten split-spoke pairs, shaped emblem hubs, closed rim barrels, rotors, calipers and wheel nuts. Added four transparent projector covers with chrome seats and source-fitted light guides; every optical vertex is within 9 mm of the original housing. Body UVs/corner normals retained;
  no body smoothing modifier, subdivision or flattening.
- Near LOD 33,968 triangles; complete original 15,000-triangle distance LOD.
  Four wheel materials share one renderer. No new realtime light or rigidbody. The 256px probe is baked once and has no per-frame capture cost.
- Baked 256px local reflection probe, excluding vehicle layer 27. Existing
  warm/dim headlamps repositioned to the new model. Car stays parked along road.
- Authoring, existing-scene material passes and validators recognize the
  authorized replacement; future fieldwork passes preserve it.

Verification completed locally in Unity 6000.6.2f1:

- `PatrolCarDeliveryRun`: source geometry/material/LOD checks, general scene
  references, presentation safety, rendered white/UV torch shadows at both
  quality tiers, fresh 800-probe lighting bake, 15 reflection captures and strict
  Windows/PCVR build. Latest delivery log: `Builds/patrol-closeup-delivery.log`.
- `PatrolCarVerifyRun`: reopen the saved scene; source texture byte hash, finite
  mesh attributes/UVs, fitted dimensions, LOD references/budgets, correct baked
  reflection exclusion, single static collision box, no dynamic bodies and
  scene references. Re-render both sides/front/rear under actual night lighting
  and temporary inspection lighting, then strict Windows build.
  Log: `Builds/patrol-verify.log`.
- Actual Unity renders: `UnityProject_Demo/Builds/PatrolReview/final-0..6.png`
  and `night-0..6.png`; close-ups 5/6 show wheel and optics. Temporary inspection light is destroyed before saving.

These are editor/player-build checks, not headset performance acceptance.
The source is a single-image exterior reconstruction; fine body irregularities
remain, and it has no detailed cabin or operable vehicle doors. No claim of
perfect photorealism or measured Quest standalone frame time is made.
