# PeopleModels: posed human figures for the night jewellery store

Static figures that give the burglary scene some life: a uniformed police officer by the patrol car, a forensic examiner at work, two plain-clothes detectives and a passer-by on the far pavement. They were made for Unity 6000.6.2f1 with the Built-in pipeline and no packages (OBJ/MTL plus textures). Prepared October 14-15, 2026. **Not imported or checked in Unity yet.**

Revised October 15, 2026 after review: the forensic examiner's white coverall and cap were near-clipped (median 245 sRGB, about a third of the texels at 250 or more), so they would have blown out to flat white under the torch, the camera flash and bloom. `ForensicExaminer_Body_Color.jpg` and `ForensicExaminer_Protection_Color.jpg` were rebuilt: the white now has a median of 220 sRGB and a maximum of 240 (below Unity's 243 limit for non-metal albedo), and the fold shading is kept. `Source~/Preview.jpg` was re-rendered. The bone labelling in `Source~/pose_export.py` now breaks ties deterministically, so a rebuild reproduces the pack byte for byte; as a result `ForensicExaminer_Body_Normal.jpg` also changed slightly (two triangles at the right shoulder now count as sleeve). Nothing else changed.

- There are **7 posed meshes** made from **5 Microsoft Rocketbox avatars**, licensed under MIT. These are photo-textured, fully rigged avatars with realistic skin, clothing and hair from a professional avatar library. Each one is posed with one frame of a Rocketbox motion-capture clip, then baked to a static mesh. You don't need a rig, an Animator or any packages.
- The hand props (notebook, pen, phone and camera) and their atlas are **project-made**.
- The total size is 15.9 MB (70 files, including the docs and `Source~`), well under the 20 MB budget. The largest file is 1.16 MB. Each figure has 6.8k to 12.7k triangles and 2 to 5 materials.
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

`python3 -P Source~/build_all.py <Rocketbox clone> <pack dir> <python with bpy 5.2.2>` rebuilds every OBJ, MTL and texture from a sparse clone of the Rocketbox repository at the commit in sources.json. Then run `write_sources.py` and `validate.py`. On October 15, 2026 a full rebuild reproduced all 56 shipped model and texture files byte for byte.

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
| `validate.py` | Checks that every OBJ loads in trimesh, that winding agrees with the normals, that the bounds and MTL maps are right and that every texture opens. |
| `write_sources.py` | Writes `sources.json` (upstream commit, source paths, SHA-256 of every source and shipped file). |
| `Preview.jpg` | A Blender Cycles render of the seven figures (front, then three-quarter view). It is not a Unity screenshot. |
