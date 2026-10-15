# PeopleModels: posed human figures for the night jewellery store

Static figures that give the burglary scene some life: a uniformed police officer by the patrol car, a forensic examiner at work, two plain-clothes detectives and a passer-by on the far pavement. They were made for Unity 6000.6.2f1 with the Built-in pipeline and no packages (OBJ/MTL plus textures). Prepared October 14-15, 2026. **Not imported or checked in Unity yet.**

Revised October 15, 2026 after review: the forensic examiner's white coverall and cap were near-clipped (median 245 sRGB, about a third of the texels at 250 or more), so they would have blown out to flat white under the torch, the camera flash and bloom. `ForensicExaminer_Body_Color.jpg` and `ForensicExaminer_Protection_Color.jpg` were rebuilt: the white now has a median of 220 sRGB and a maximum of 240 (below Unity's 243 limit for non-metal albedo), and the fold shading is kept. `Source~/Preview.jpg` was re-rendered. The bone labelling in `Source~/pose_export.py` now breaks ties deterministically, so a rebuild reproduces the pack byte for byte; as a result `ForensicExaminer_Body_Normal.jpg` also changed slightly (two triangles at the right shoulder now count as sleeve). Nothing else changed.

- There are **7 posed meshes** made from **5 Microsoft Rocketbox avatars**, licensed under MIT. These are photo-textured, fully rigged avatars with realistic skin, clothing and hair from a professional avatar library. Each one is posed with one frame of a Rocketbox motion-capture clip, then baked to a static mesh. You don't need a rig, an Animator or any packages.
- The hand props (notebook, pen, phone and camera) and their atlas are **project-made**.
- Two figures also have **LOD variants** (added October 15, 2026): `PasserBy_Phone_LOD1/LOD2` (1,800 and 599 triangles) and `PoliceOfficer_Standing_LOD1/LOD2` (2,999 and 899 triangles). They use the original MTLs and textures. See [LOD variants](#lod-variants).
- The total size is 16.5 MB (75 files, including the docs, `Source~` and the four LOD files, which add 0.55 MB), under the 20 MB budget. The largest file is 1.16 MB. Each figure has 6.8k to 12.7k triangles and 2 to 5 materials.
- `Source~/Preview.jpg` is a Blender Cycles render of all seven figures, front and three-quarter view. Unity ignores `Source~`, which also holds the build scripts.

See LICENSE.txt for the MIT notice that has to stay with the files. sources.json has the upstream commit and the SHA-256 of every source and shipped file.

## Conventions

| Item | Convention |
|---|---|
| Units / axes | Metres, Y up, **front = +Z**, feet on y = 0, centred in x/z. Import with **Scale Factor 1** and Convert Units off. Unity's OBJ importer flips handedness, so the figure still faces +Z (transform.forward). |
| Pose | Baked into the mesh. No skeleton. To turn a figure, rotate it about Y only. |
| `<Figure>_<Pose>.obj/.mtl` | `map_Kd` points at `<Figure>_<Material>_Color.jpg`. The hand props use `PeopleProps_Color.jpg`. Unity's OBJ importer reads only Kd, `map_Kd` and `d`. Assign the other maps in code (DemoAssetLibrary.NormalMap / ApplyMetallicSmoothness) or on the material. |
| `_Color.jpg` | Base colour, sRGB. Body and Head are 2048 px. Downscale with the importer's **Max Size 1024** for figures that are only seen from the street. The two ForensicExaminer white maps (Body, Protection) are 4:4:4 JPGs in which no texel exceeds 240 sRGB, and their unused UV space is edge-padded; leave the material colour at white (1, 1, 1) and don't brighten them in the importer. |
| `_Normal.jpg` | Tangent-space normal map, OpenGL +Y (this matches Unity). Texture Type **Normal map**, strength 1. |
| `_MetallicSmoothness.png` | R = metallic, G = B = 0, A = smoothness. **sRGB off**. In Standard, put it in the Metallic slot with Source = Metallic Alpha and Smoothness scale 1. This is the same convention as PBRExterior, ForensicModels and NightMaterials. Smoothness ranges from about 0.1 to 0.8, with a median of 0.2 to 0.35 for skin and cloth. Only the Equipment and Pistol metal parts and the prop metal have metallic > 0. |
| `DetectiveFemale_Hair_Color.png` | RGBA (1024 x 512) for the ponytail, fringe and eyelash cards. Material **Hair**: Standard Rendering Mode **Cutout**, Alpha Cutoff 0.35 to 0.45, smoothness about 0.35. Turn on Alpha Is Transparency on the texture. The cards are already duplicated back-to-back, so back-face culling is fine. Without Cutout they show as dark rectangles round the head. |
| Static batching / lighting | All figures can be marked Static (batching, occluder off). Use light probes or treat them as dynamic receivers, so the flashlight and UV torch light them like the rest of the scene. They cast shadows. |

## Figures

| Model | Real size (m, x × y × z) | Tris | Materials (maps) | Rocketbox source, pose | Description | Suggested placement in the night store |
|---|---|---|---|---|---|---|
| **PoliceOfficer_Standing** | 0.62 × 1.85 × 0.41 | 12,445 | Body, Head (Color 2K, Normal 1K, MS 512); Equipment (1K/512/256); Pistol (256/256/128) | Professions/Police_Male_03, `m_idle_neutral_02` frame 30 | Uniformed constable at ease. Peaked cap with a "POLICE" band, black uniform jacket with reflective cuffs, "POLICE" chest patch, shoulder radio, duty belt with radio, pouches and a holstered pistol. Arms relaxed. | On the pavement next to the patrol car, between the car and the forced storefront doors, facing the doors or the street. Works as the scene guard at the barrier tape. |
| **PoliceOfficer_TakingNotes** | 0.61 × 1.80 × 0.57 | 12,661 | same + Props | same avatar, `m_documents_note` frame 190 | The same officer writing in a pocket notebook (black cover, written page) with a ballpoint pen held in the right hand. | Keeping the scene log at the cordon by the entrance, or facing the PasserBy as if taking a witness statement. Use only one of the two officer poses, or both at different places (same face). |
| **ForensicExaminer_Crouching** | 0.67 × 1.15 × 0.69 | 8,240 | Body, Head, Protection (Color 2K/1K, Normal 1K, MS 512) | Professions/Medical_Male_04, `m_crouch_idle` frame 1 | Crime-scene examiner in a deep crouch. White disposable coverall (recoloured from scrubs), white hood cap, face mask, blue nitrile gloves resting on the knees. The white is an off-white albedo (median 220, maximum 240 sRGB), so the folds stay visible in the torch beam and the photo flash. | Beside the smashed central display case, a little behind a numbered evidence marker, as if examining glass on the floor. Also works at the forced door threshold (footwear marks). |
| **ForensicExaminer_Photographing** | 0.60 × 1.79 × 0.52 | 8,612 | same + Props | same avatar, `m_take_picture` frame 80 | The same examiner standing and taking a photograph with a DSLR with a speedlight held to the eye. The lens points along +Z. | Photographing the open safes in the rear office (place him about 2 m in front of the safes, facing them), or a broken wall-case bay. Use either this pose or Crouching for one examiner. Use both only if a second examiner is wanted (same face). |
| **Detective_LookingAround** | 0.66 × 1.80 × 0.46 | 7,597 | Body, Head (Color 2K, Normal 1K, MS 512) | Professions/Police_Male_07, `m_idle_look_around_02` frame 40 | Plain-clothes detective. White shirt, tie, navy protective vest with "POLICE", dark trousers, warrant badge on the belt. Standing, with the head turned to his left, surveying the room. | Inside, by the counter or the central cluster, turned so his gaze goes towards the broken cases. Alternatively in the rear-office doorway. |
| **DetectiveFemale_TakingNotes** | 0.56 × 1.73 × 0.49 | 9,038 | Body, Head (Color 2K, Normal 1K, MS 512), **Hair (RGBA PNG, Cutout)** + Props | Professions/Business_Female_04, `f_documents_note` frame 150 | Plain-clothes detective in a dark pinstripe trouser suit with a ponytail, holding a pocket notebook and a pen in front of her chest. | Inside, near the Detective (about 1.2 m apart, facing each other at an angle), or by the seating corner. Also works outside at the door, talking to the officer. |
| **PasserBy_Phone** | 0.64 × 1.78 × 0.56 | 6,808 | Body, Head (Color 2K, Normal 1K, MS 512) + Props, PropScreen | Adults/Male_Adult_12, `m_cell_phone_textmessage` frame 100 | Bystander in a denim jacket, white T-shirt, jeans and trainers, looking down at a smartphone in his right hand. | On the far pavement across the street, under a streetlight, outside the cordon. Give the PropScreen material a weak emission (PeopleProps_Color, intensity about 0.5) so the screen glows at night. |

Triangle counts include the hand props. All seven figures together are about 65k triangles. In normal use you would place 3 to 5 of them, which is about 40 to 50k triangles and 12 to 18 draw calls.

### Shared files

| File | What it is |
|---|---|
| `PeopleProps_Color.jpg` (512 px) | Project-made atlas for the hand props: notebook cover and scribbled page, page edges, pen, phone body, blurred camera-app screen, camera body, zoom lens, lens glass, flash window. |
| `PeopleProps_MetallicSmoothness.png` (256 px) | Metallic/smoothness for the props (metal pen tip and clip, glossy screen and lens glass). |
| `<Figure>_<Material>_Normal.jpg` / `_MetallicSmoothness.png` | Per-material maps (see Conventions). The two poses of one figure share the same textures. |

## LOD variants

Added October 15, 2026. **Not imported or checked in Unity yet.** Two figures have lower-detail meshes for a Unity LODGroup (LOD0 is the original OBJ). They were decimated offline by `Source~/make_lods.py`; no existing file was changed.

| File | Triangles (target) | Groups (source -> LOD) | Bytes | Height, lowest vertex | Winding (validate.py) |
|---|---|---|---|---|---|
| `PasserBy_Phone_LOD1.obj` | 1,800 (1,800) | Body 3,694 -> 942, Head 3,028 -> 772, Props 84 and PropScreen 2 unchanged | 143,158 | 1.775 m, y = 0 | 0.9967 (source 0.9985) |
| `PasserBy_Phone_LOD2.obj` | 599 (600) | Body -> 281, Head -> 232, Props and PropScreen unchanged | 48,925 | 1.775 m, y = 0 | 0.9933 |
| `PoliceOfficer_Standing_LOD1.obj` | 2,999 (3,000) | Body 4,662 -> 1,117, Head 3,590 -> 861, Equipment 3,646 -> 874, Pistol 547 -> 147 | 274,090 | 1.849 m, y = 0 | 0.9987 (source 0.9992) |
| `PoliceOfficer_Standing_LOD2.obj` | 899 (900) | Body -> 275, Head -> 213, Equipment -> 366, Pistol -> 45 | 85,720 | 1.849 m, y = 0 | 0.9989 |

`PoliceOfficer_Standing_LOD1/LOD2` are used only by the provisional police look (`PoliceLook.ProvisionalRealisticUS`). The country and police service are still an open decision, so these two files may be dropped or replaced together with that look.

How to use them:

- **Same materials, same maps.** Each LOD names the original MTL (`mtllib PasserBy_Phone.mtl`, `mtllib PoliceOfficer_Standing.mtl`); no new MTL or texture was added. Give the LOD1 and LOD2 renderers the LOD0 materials (the same instances, with the normal and metallic/smoothness maps set as in the wiring notes) rather than the copies the OBJ importer makes, so the LODGroup adds no materials.
- **Same frame.** Metres, Y up, front = +Z, feet on y = 0, the same x/z centre and exactly the same bounds as the source (the decimation keeps every group's bounding box), so the LODs sit at the same local position as LOD0 and the CapsuleCollider stays as it is.
- **Suggested LODGroup thresholds** (relative screen height, tune in Unity): LOD0 above 0.25, LOD1 from 0.25 to 0.08, LOD2 below 0.08, no culling. For a 1.85 m figure that switches to LOD1 at about 3.3 m on Quest 2 (about 6 m on a 60-degree desktop camera) and to LOD2 at about 10 m (20 m on desktop).
- The Props group of the passer-by (phone) and PropScreen are copied unchanged, so the screen emission works the same at every LOD.

How they were made (`Source~/make_lods.py`, a copy of the StreetModels lods-street tool as of October 15, 2026, which extends ForensicModels' `decimate_exact.py`):

- Seam-aware half-edge collapse with quadric error. Corners are split per (v, vt, vn), positions are welded only to find neighbours, so on LOD1 the UV seams, hard normal edges and the joins between groups shorten only along themselves, on both sides at once. **No vertex moves:** every corner of a LOD is an original v/vt/vn of the source, written as its original text, so LOD1 maps its textures exactly as LOD0. The last vertex on each face of a group's bounding box is never removed, so the height and the floor contact are exact.
- Groups under 64 triangles and the listed groups (Props, PropScreen) are copied unchanged; the other groups get a share of the target in proportion to their size (a group that cannot get down to its share keeps more and the others take less).
- LOD2 is "relaxed": a vertex may also collapse across a seam or where three or more corners meet. The corners that move take the nearest normal/UV of the same group, never further than 0.02 in UV (0.1 for the officer), and that difference counts in the cost. The geometry stays closed. Passer-by: 15 such collapses; officer: 366. The officer's Equipment is cut into many small UV islands; with the 0.02 limit its seams used up the budget and the trouser legs thinned out (16.5 % of the silhouette lost instead of 8.9 %).
- A winding guard, added for the figures: no collapse may turn a triangle against its own corner normals. Without it, the thin closed parts (belt pouches, collar, fingers) folded over and 2 to 7 % of the triangles faced against their normals.
- Output is deterministic: a rerun gives the same bytes.

Checks (run before anything is written; the script exits with 1 and writes nothing if one fails): triangles within 10 % of the target; bounds within 1 cm and height within 1 cm of the source; lowest vertex on y = 0; same usemtl set and group order; mtllib is the original MTL; every v/vt/vn index valid and every face a triangle; no zero-area triangle; every corner an original (v, vt, vn); copied groups identical; no new open edges (a crack would add some: 40 -> 16 and 11 for the passer-by, 55 -> 27 and 9 for the officer); and `Source~/validate.py` run on a staging copy of each LOD (winding at least 0.99, every triangle read by trimesh, the colour maps present).

Offline quality measurements (scratch scripts, not shipped; front-face coverage from 32 orthographic views, 8 directions x 4 heights, 2 mm pixels, compared with the source):

| | Silhouette area lost | Source silhouette not covered, more than 6 mm inside its edge |
|---|---|---|
| PasserBy LOD1 / LOD2 | 1.4 % / 6.9 % | 0.16 % / 3.4 % |
| PoliceOfficer LOD1 / LOD2 | 1.8 % / 8.9 % | 0.29 % / 5.1 % |

No view shows a back face through a LOD, so there are no holes: the LOD1 losses are fingers and thin edges, and the LOD2 losses are thin parts and limbs that become slimmer. The vertices stay on the original surface, so a limb with few vertices left becomes an inscribed polygon (the officer's calf section at 30 cm height is 132 cm2 instead of 160 cm2). For comparison, the plain `decimate_exact.py` approach (the split mesh decimated per group, without the seam rules) opened see-through cracks along the UV seams, through which the back faces showed: 2.4 % / 3.9 % at LOD1 and 9.4 % / 11.4 % at LOD2, the largest about 380 cm2 in one view.

Known limitations of the LODs: on the officer's LOD2 the face, the jacket front and the collar show smeared texture where seams gave way, the radio on the belt is reduced to a thin dark shape and the hands are simplified to mitten shapes; the passer-by's LOD2 hands are simplified the same way. Use LOD2 only at small screen sizes as suggested above, and if it looks too slim or smeared in Unity, raise its target in `MODELS` and rerun. The preview row (`Source~/render_preview.py`) needs Blender's bpy module, which was not available when the LODs were made, so `Source~/Preview.jpg` still shows only the seven source figures.

Rebuild: from this folder, `python3 -I Source~/make_lods.py .` (about 35 s; the decimation needs only the standard library, VERIFY uses trimesh and runs `validate.py` with numpy, trimesh and Pillow). It rewrites the four LODs, their entries and its own entry in sources.json, and leaves every other entry as it is.

## Import and wiring notes

1. Put the folder under `Assets/CrimeSceneDemo/ThirdParty/PeopleModels` the same way as the other packs. Unity creates the .meta files.
2. For the textures, use DemoAssetLibrary as the other OBJ packs do. Load the model with a primitive fallback (a grey capsule 1.8 m tall, or nothing at all, because people are optional). Then set `_BumpMap` from `<Figure>_<Material>_Normal.jpg` and call `ApplyMetallicSmoothness` with `<Figure>_<Material>_MetallicSmoothness.png`. Materials come out of the OBJ named Body, Head, Equipment, Pistol, Protection, Hair, Props and PropScreen. Map them by figure prefix plus material name (Props and PropScreen use `PeopleProps_*`).
3. `DetectiveFemale` / `Hair`: set Standard to Cutout as described above. This is the only material that needs alpha.
4. Collision: add a CapsuleCollider (radius 0.25, height 1.8; for the crouching pose radius 0.35, height 1.15) so that the visitor and placed tools do not pass through the figures. Do not use a MeshCollider.
5. Validation suggestion (DemoValidation / NightStoreValidation): check that each placed figure has its OBJ loaded with its Color map, and that the bounds height is within 1.1 to 1.9 m. Check that the Hair material of DetectiveFemale uses `_ALPHATEST_ON`. Also check that no figure overlaps a display case, the car or the door swing.
6. For realism at night: keep the figures away from the brightest scene lamps. The Rocketbox skin reads best under the street lights, the patrol car's light bar and the visitor's torch. Don't put a figure where the visitor first enters, so the first view stays the empty, forced store.

## Known limitations

- The meshes are static: there is no idle sway and no blinking. A figure that stands still reads well from more than 2 m away. Up close in VR the fixed pose is noticeable. If animation is wanted later, Unity imports the rigged Rocketbox FBX natively without packages (same MIT licence). Those avatars use 3ds Max materials, which need the repository's `FixRocketboxMaxImport.cs`. They were not shipped here because each avatar's raw TGA set is about 100 MB.
- The hand props are simple modelled shapes. The pen and phone partly intersect the fingers in a few spots that are only visible within about 30 cm.
- The officer's uniform is a generic European/UK patrol style with a holstered pistol. The detective's belt badge is a generic US-style shield ("LIEUTENANT A. SMITH ... DEPARTMENT POLICE"). Both are small, but if the story needs a strictly British force, use the officer without a close-up of the belt, and place the detective so the badge is not in view.
- The forensic coverall is a texture recolour of scrubs, so there is no hood mesh or overshoes. It reads as a white coverall with a hood cap from normal viewing distance.

## Source~ (build scripts, ignored by Unity)

`python3 -P Source~/build_all.py <Rocketbox clone> <pack dir> <python with bpy 5.2.2>` rebuilds every OBJ, MTL and texture from a sparse clone of the Rocketbox repository at the commit in sources.json. Then run `write_sources.py` and `validate.py`, and finally `python3 -I Source~/make_lods.py .` for the LOD variants. On October 15, 2026 a full rebuild reproduced all 56 model and texture files that existed then byte for byte.

`validate.py` and `write_sources.py` predate the LODs and were left unchanged: `validate.py` looks for a `<name>.mtl` beside every OBJ and `write_sources.py` for a pose entry for every OBJ, so both stop on the `_LOD` files. Until they skip `_LOD` files, run them with the four LOD files moved out of the folder, then run `make_lods.py`, which checks its LODs with `validate.py` on a staging copy and puts its own entries back into sources.json.

| File | Role |
|---|---|
| `build_all.py` | Driver: the figure list, poses, prop placement and per-material texture settings (for example `jpeg_max` 240 on the two examiner white maps). |
| `pose_export.py` | Runs inside bpy. Poses an avatar with one animation frame, bakes it to OBJ, adds the hand props and writes the per-triangle bone labels that the examiner recolour uses. |
| `props.py` | Project-made hand-prop geometry: notebook, pen, phone and camera. |
| `make_props_atlas.py` | Project-made procedural atlas `PeopleProps_*`. |
| `build_textures.py` | Converts the TGA maps to Color JPG, Normal JPG and MetallicSmoothness PNG. Includes `save_jpeg_capped`, which encodes a JPG in which no texel decodes above the cap. |
| `edits.py` | Texture edits: the examiner recolour (`forensic_body`, `forensic_protection`, white levels `WHITE_*`) and the clone-out of the radio badge and pistol medallions (`paint_out`). |
| `render_preview.py` | Renders `Preview.jpg` rows with Cycles (yaw 0 and 35 degrees). |
| `scan_frames.py` | Helper used to pick the animation frames. Runs in bpy and prints the frame range of an animation FBX and the hand and head positions at about 25 sampled frames. |
| `make_lods.py` | Writes the four `_LOD1`/`_LOD2` OBJs (see LOD variants) and their sources.json entries. A copy of `StreetModels/Source~/make_lods.py` (October 15, 2026, SHA-256 702c4278...) with the figure table, a winding guard, the figure checks and the sources.json update. Needs only the standard library; VERIFY uses trimesh and `validate.py`. |
| `validate.py` | Checks that every OBJ loads in trimesh, that winding agrees with the normals, that the bounds and MTL maps are right and that every texture opens. |
| `write_sources.py` | Writes `sources.json` (upstream commit, source paths, SHA-256 of every source and shipped file). |
| `Preview.jpg` | A Blender Cycles render of the seven figures (front, then three-quarter view). It is not a Unity screenshot. |
