# Local Oman street preview — October 21, 2026

Awaiting Koda approval. No GitHub upload/commit and no official ZIP refresh.
Backup: C:/Users/MOBPC/Documents/ChatGPT/Jewelry store/Backups/Before-Oman-Street-20261021
Baseline: 1005d47765abefd595dededebf57895ef6c6a999.
Original saved-scene SHA256: E32E3E9EDD15021CC7C66AF3D6A30DB6729ADE22A6C9EF8B989BEDB48468B42C.
Official snapshot remains byte-identical to backup: 63E75AC1EAFFD20B8ABA4F52FCC93075DEB369A45E467A43F4698EDC63F3A072.

## Scene

Existing UnityProject_Demo/Assets/CrimeSceneDemo/Scenes/JewelryStoreNight.unity edited in place.
Exterior/OmanStreet replaces the active western street dressing; old exterior groups are inactive.
29 shops, 12 maintained / 12 weathered / 5 neglected. Four lower units; arched galleries, arched/rectangular barred windows, selective canopies; independently varied roofs, signs, painted steel/shutters and plaster condition.
Arabic/bilingual artwork remains integrated into real framed signboards, with original 4:1 artwork aspect. All 29 shop-sign local bounds are below y3.62 m and behind-face max z <= -.449 m, clearing the neighboring gallery slabs and wall faces. Gateway signs are mounted clear on both sides.
5 m empty parking bays beside the rows, 7 m unobstructed access lane, black/white kerbs and dry photographic road/paving maps. Six refined tapered municipal posts with base plates/bolts, curved arms, sealed lenses and cooling fins; baked warm pools plus subdued external shop lights. Original dark shop fixtures remain OFF.
Store furniture/layout, façade frames/handles and accepted VR runtime code preserved; architectural glass/debris changes are described below; car transform and original FBX source/importer/geometry preserved. One gray, untextured supplied car; no added cars or people.

## Performance choices and limits

Entire added exterior: 56,876 triangles. Background continuity: only 3,504 triangles in eight bounded renderer chunks (multiple materials; this is not a draw-call count). No distant colliders, lights, scripts or interiors. Two T-junction/dogleg closures and short side streets behind the playable ends remove open map-edge views. Neighborhood shells occupy all directions. Fog blends depth, rather than hiding missing ground.
New exterior lights are all baked: zero added realtime lights. New artwork capped at 1024 px, mipmaps, Android ASTC 6x6. Original generated images retained as source artwork.
Active complete-scene audit: 613 renderer components, 1,178 material slots; no missing shaders/materials/scripts/meshes, no invalid lit normals, no exact duplicate renderer candidates. These are scene-reference checks, not GPU draws or frame timing.
Fresh final geometry bake: 6.9 seconds, three 512px-max lightmaps, 676 probes; reflection captures refreshed. Strict Windows build passed.
Human headset acceptance, actual Quest 3 standalone frame time/comfort and Android build of this art pass remain unverified. The prior official APK has not been replaced.

## Review

Actual Unity camera renders: UnityProject_Demo/Builds/oman-street-0.png through -11.png. These include storefront, both ends, corner views, opposite shops, inside-to-street view, and close sign/lamp inspections. Docs/References/OmanStreetConcept.png is concept artwork, NOT an in-game screenshot.
Player: Builds/Windows/JewelryStoreDemo.exe. Existing Play-Keyboard.cmd and Play-VR.cmd launch the local current player; Open-In-Unity.cmd opens the current editable project. Do not explicitly regenerate the original scene via CombinedBuild.

## Restoration

Only restore if Koda requests it. Close Unity/player, restore backed-up Assets/Packages/ProjectSettings and original Windows player plus canonical Prototype/Tools from the backup. Current local unapproved source changes remain on codex/oman-street-preview. Do not overwrite the official snapshot or push before approval.


## Architectural glass expansion

Koda then added the real-photo glass brief. Four saved architectural panes are replaced with unique per-seed physical 6 mm fracture meshes (the asset paths are distinct, so window/door dimensions cannot overwrite one another), radial cracks and retained broken edges. One window remains cracked, three panels have real missing regions. Reported removed areas: 0 / .454 / .361 / .084 square metres for the four source panes. Cracks scatter direct light and use dielectric highlights; no emission.

Three batched debris clusters contain 1,100 closed small fragments, with a low-opacity, flashlight-lit fine-glass coverage layer at the two main fall locations. Background haze in the generated coverage image is rejected by alpha and luminance before lighting. No opaque white pane surface, screen grab or per-fragment physics. Glass geometry including the projected contacts and seams totals 60,040 triangles. Fragment faces stay almost clear; newly exposed cut facets scatter more light. Contact layers are below the real fragments. Six actual flashlight-lit before/after views, including both sides, standing and crouched floor views, are saved as UnityProject_Demo/Builds/architectural-glass-after-0.png through -5.png.

Validation also verifies unique architectural panes, millimetre normals, no glowing crack materials, non-physical batched debris, floor height envelopes and transparent face thresholds. Current complete-scene audit: 613 renderer components / 1,178 material slots; zero missing references, invalid lit normals or exact duplicate renderer candidates. Strict Windows build passed after the final fresh bake/reflection refresh. This does not certify headset frame rate or perfect photorealism. Human visual acceptance remains necessary.

Fine fracture/cut-edge detail fades from 7 to 15 m to reduce subpixel shimmer without fading main-pane reflections. Review cameras use 4x MSAA. Existing display-case glass defaults to no distance detail fade, preserving its appearance.
