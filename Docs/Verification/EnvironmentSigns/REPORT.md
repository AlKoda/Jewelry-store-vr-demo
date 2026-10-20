# Arabic environment signs

October 20, 2026. Unity 6000.6.2f1, saved JewelryStoreNight scene, DX11 Windows build.

- EnvironmentSigns validation passed: four plaque faces, three added notice faces, all five required generated textures present.
- Windows strict build and existing VR visual validation passed: ENVIRONMENT_SIGNS_DELIVERY_PASS / EXISTING_SCENE_DELIVERY_PASS.
- Rendered close-ups inspected: evacuation plan, paper staff reminder, CCTV plaque, closed card. Correct joined Arabic and front-facing UVs. Both closed-card sides use the same readable artwork.
- Removed old atlas faces for plan/CCTV/noticeboard rather than leaving competing text. Cork backing replaces the old noticeboard lettering; existing frame retained.
- Evacuation artwork follows the authoring reference derived from NightStoreLayout: front exit, three rear rooms, case islands and clear aisle route. This is fictional training-scene artwork, not a certified plan.
- Textures use mipmaps, 2048 desktop maximum and Android 1024 ASTC override. No new lights or emissive materials. Store interior fixtures remain off.
- Quest standalone and in-headset legibility/performance have not been tested.

sign-0.png: evacuation; sign-1.png: staff paper; sign-2.png: CCTV; sign-3.png: closed card.
