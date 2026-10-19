# Shattered entrance glass — October 14 art pass

Staged art only. Game code, current Unity scenes/build, collision logic and Claude's work were not changed. All model geometry was newly authored for this project. No downloaded textures or third-party geometry are used in this pack.

## Assets
- Window_Broken: irregular central opening, retained peripheral fragments and secondary cracks.
- Window_Cracked: matching impact pattern with the centre still present; an alternative to the broken window, not an overlay to stack on top of it.
- DoorGlass_Left_Broken and DoorGlass_Right_Broken: different impact patterns, sized to the current door glass opening.
- GroundGlass_Threshold: dense near-impact pile with progressively smaller scattered pieces.
- GroundGlass_WindowFall: narrower fall beneath a window.
- GroundGlass_Sparse: lighter scatter for distant or less disturbed areas; a separately authored alternative, not an exact decimated LOD of the dense pile.

All seven are single meshes with two material slots (clear broad faces and cut edges), closed thick solids, metre-scale UVs and zero object origins. Door/window panels are 8 mm thick. Ground pieces are 3–7 mm thick, irregular triangles/quadrilaterals/polygons with varied tilt. Their lowest points sit 1.2 mm above the placement plane. Several large pieces accompany smaller fragments; density falls off from the impact region. These are art-directed static patterns, not a fracture/structural simulation or a material-specific safety-glass model. Overlapping fragments are not physics-settled.

## Dimensions and placement
FBX/OBJ exports convert Blender Z-up to Y-up. The width is along X and the panel is vertical. Panel origin is at the bottom-centre of its nominal glass opening, not the complete frame. Panel geometry includes a sub-millimetre fracture separation.

Window nominal size: 1.92 m wide x 2.13 m high. Place its base at store height 0.43 m in an existing 2 m facade bay. Door nominal size: 0.775 m wide x 2.05 m high. Place its base at leaf-local height 0.19 m, centre across the leaf; parent it to that leaf so it follows the existing hinge rotation. Do not place a door pane at the whole entrance root. Match the original glass-depth offset when integrating. The two door patterns are variants, not hinge-coded assets.

Ground packs are centred around their placement origin on a flat horizontal plane. Their footprint and maximum height are in Deliverables/geometry-report.json. Place one threshold cluster around the actual opening and one window cluster below a broken bay. Avoid stacking duplicate packs. Check overlaps with bollards, curbs and thresholds in the final scene; the generic studio scatter does not know the store collision layout.

## Materials and performance
ClearFractureFaces: IOR 1.52, roughness 0.025, transmission 1 in Blender. FreshCutEdges: subtle green tint, roughness 0.13, transmission 0.94. Neither material emits light. Sparkles in the previews are reflections from lights. The preview floor's procedural bump is studio-only and is not exported as a game texture.

FBX and OBJ do not reproduce Cycles transmission automatically. Assign the existing game's transparent glass shader and edge material explicitly, with appropriate sorting, reflection probes and shadows. Do not use an emissive or opaque-white material to imitate highlights. Prefer two-sided closed geometry (already supplied) over duplicate coplanar transparent overlays. Each mesh has two slots, so this is not necessarily one draw call. Transparency fill rate and overdraw need testing in the actual player; triangle counts alone do not prove performance. No per-shard rigidbodies are supplied or recommended for this static dressing pass.

Preserve the existing traversal and collision behavior. A visual hole should not accidentally retain a solid full-pane collider; conversely, changing collision is Claude's integration task, not part of this art delivery. Do not remove an entire scene collider without checking door frames and existing test assumptions.

## Files and validation
Editable master: ShatteredGlass_Review.blend. Export formats: Deliverables/*.fbx and *.obj/*.mtl. Preview images: entrance-glass.png, ground-glass-detail.png, window-cracked.png. All previews are Blender Cycles studio renders, NOT in-game results. Studio frames, floor, background and lights are illustrative and are not part of the seven exported glass models.

FBX files are reimported into a clean Blender process; validation.json records triangle totals, non-manifold-edge and degenerate-face checks, UV presence, zero origins and hashes. Every piece is closed. Runtime transparency, scene placement, shader mapping and VR performance remain unverified until the joint Unity integration pass.

Reproduction: run Blender in background with build_glass.py, then validate_glass.py. Scripts are isolated art-authoring utilities, not changes to game/editor code.

## Export cost

| Asset | Triangles | Fragments |
|---|---:|---:|
| Window_Broken | 912 | 76 |
| Window_Cracked | 2128 | 190 |
| DoorGlass_Left_Broken | 912 | 76 |
| DoorGlass_Right_Broken | 912 | 76 |
| GroundGlass_Threshold | 2644 | 190 |
| GroundGlass_WindowFall | 1916 | 135 |
| GroundGlass_Sparse | 652 | 45 |
