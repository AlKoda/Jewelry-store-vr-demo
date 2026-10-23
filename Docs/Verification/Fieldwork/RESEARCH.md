# Investigation interaction research — October 23, 2026

Primary sources reviewed before choosing this batch:

| Project / developer source | Useful design idea | Applied here |
|---|---|---|
| [The Room VR, Fireproof Games developer article](https://blog.playstation.com/2020/03/23/how-fireproof-games-brought-puzzle-sensation-the-room-to-ps-vr/) | Tactile manipulation of detailed switches, locks, clasps and mechanisms | A hollow document box with an animated hinged lid, visible lining and latch; physical forceps/swab models; a register resting on a real cart |
| [Scene Investigators, EQ Studios](https://www.eqstudios.com/sceneinvestigators) | Observe the environment, take notes, compare context and avoid filling gaps with certainty | A receipt, stock tag and staff reminder provide context alongside traces; descriptions distinguish observations from unconfirmed causes; no automatic culprit identification |
| [Forensic Detective VR proof of concept, Rising Pixel](https://www.risingpixel.com/case-studies/forensic-detective) | A life-size scene with footwear/physical traces to examine | Existing partial footwear/fibre/clasp cues become inspectable; six bounded collection targets support forceps and swab use |

These are design references, not asset sources. No screenshots, audio, models or
proprietary code from those games were put into this project. The field cart,
tools, box, pouch, cable tie and paper graphics are project-created. The existing
MIT Rocketbox animated crowd remains in place.

Ideas deferred: elaborate NPC dialogue, time-rewind crime reconstruction,
laboratory identification and live CCTV feeds. They require authored narrative,
additional interaction review or rendering cost. This batch focuses on a complete
inspection/collection/record/reset loop using the current scene and input rig.

## Portable case and fingerprint development — October 23

Additional primary references reviewed for the carryable-kit request:

| Developer source | Reported focus | Design choice here |
|---|---|---|
| [Ghost Town, Fireproof Games](https://www.fireproofgames.com/games/ghost-town) | Atmospheric locations and hands-on VR puzzles/exploration | Carry a detailed open case into the scene, then work with the other hand; a physical guide and register remain attached to its lid |
| [Red Matter 2, Vertical Robot](https://redmatter2.com/) | Physical interactions, exploration and examining clues in detailed environments | Develop a small handle mark through sustained brushing, photograph it with the existing camera, then collect a lift card |
| [Meta performance testing guide](https://developers.meta.com/vr/documentation/unity/unity-perf/) | Profile bottlenecks and measure changes on the target device | Reuse existing brush/powder models and the existing decal shader; combine case geometry, exclude movable parts from static captures and add no lights or dynamic rigidbodies |

The right column is our design inference, not a claim that those games contain
our evidence workflow. No game assets were copied. New case/label/ridge meshes
are original project work; the existing ForensicModels and SurfaceDecals packs
retain their provenance. Only designated marks can be developed, and no ridge
pattern identifies a person. Device frame time and hand fit still need a Quest
test; geometry/component counts alone cannot establish performance.
