# Animated bystanders — local preview, October 21, 2026

Five distinct Microsoft Rocketbox MIT avatars, using Rocketbox's captured motion.
Upstream commit: `0943055db6ec570bcef9f2c8b41c9e5467c808f9`.
The parent PeopleModels LICENSE.txt applies; retain it when redistributing.

| Role | Source avatar | Motion |
|---|---|---|
| Walker | Adults/Male_Adult_05 | m_walk_slow_01, m_idle_neutral_02 |
| Talker | Adults/Male_Adult_10 | m_gestic_talk_neutral_01, m_idle_neutral_02 |
| Listener | Adults/Female_Adult_06 | f_gestic_talk_neutral_01, f_idle_neutral_02 |
| Phone | Adults/Male_Adult_12 | m_cell_phone_talk_01 |
| Observer | Professions/Business_Female_04 | f_idle_neutral_02 |

Meshes retain their original skin, UVs and 80-bone hierarchy. Offline world-space
retargeting bakes local animation curves at 15 samples/second, removes planar root
travel, seats the supporting foot and blends loop tails. Unity legacy Animation
plays these directly; no live humanoid retargeting or extra runtime package.
Root movement is a bounded pavement route with watch pauses and visitor yielding.
Conversation gestures alternate. A project-made physical phone follows the hand.

New albedo maps are PNG, at most 1024, with RGB capped at 238 to prevent glaring
white cloth. Hair/eyelash alpha is preserved. Normal maps are at most 512. Android
uses ASTC 6x6. Phone and Observer reuse existing parent pack colour/normal maps.
Matte non-emissive cloth/skin and two-sided alpha-tested hair receive existing
probes and torch shadows. Crowd render layer 24 is omitted from static reflection
captures so moving people do not leave a reflection ghost. Do not mark skins static.

Reproduction: download the exact paths/hashes in sources.json beneath the local
`.asset-cache/crowd-source`, run Source~/prepare_crowd_textures.py, then run Blender
5.2 background with Source~/bake_crowd.py from the checkout root. Asset provenance
and SHA-256 values are in sources.json. Raw source downloads are not shipped.

October 26 model refinement: Walker_Detailed.fbx, Listener_Detailed.fbx and
Observer_Detailed.fbx retain the original rig, UVs and motion, with a local
Loop-style subdivision of the face, ears and hands. Smoothing displacement is
limited to 1.8 mm in world units, preserving identity and finger spacing. Their
paired distance meshes are decimated offline, before skinning. Reproduce with
Blender background and Source~/refine_crowd_meshes.py, then Source~/dress_omani.py
for the two dressed actors. Original Animated FBXs remain the reproducible inputs.

The active five-person crowd totals 123,296 near / 36,985 distance triangles,
using exclusive per-actor LODs at 0.21 screen height. Existing lights only;
offscreen animation culling; one shared 40-vertex soft foot-contact mesh. These
are asset budgets, not a measured standalone Quest frame rate. Headset acceptance
is pending. Both mesh levels pass animation/skin and rendered silhouette checks.
