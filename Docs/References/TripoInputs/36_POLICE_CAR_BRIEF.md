# Royal Oman Police patrol car — Tripo input and decal brief (October 25, 2026)

Car: Mercedes-Benz S-Class (W222, long wheelbase) in Royal Oman Police patrol livery, from Koda's reference photo.

## 1. Tripo Studio (the body, no text)

Input picture: `36_P2_15000f_police_car.png` (1400 px, the car cropped from the photo; Tripo removes the background itself).

1. tripo3d.ai → Studio → **3D Workspace** → **Smart Topology Mesh** (Smart Mesh), version **P2.0**.
2. Upload `36_P2_15000f_police_car.png` as a single image (not multi-view). Type nothing in a prompt box.
3. Topology **Triangle**, face count **15,000**. Texture **Standard**, **PBR on**. Pro: Number of Generations 2, second count 8,000.
4. Generate (about 100 credits). Check all sides: wheels separate from the body, white roof and boot, navy bonnet and lower doors, light bar present.
5. If the first result fuses the wheels or loses the navy band, generate again with **Image to 3D → v3.1 (H3.1)** and the same picture, Smart Low-poly off, PBR on.
6. **Export → FBX**, textures included, scale 1, name `police_car_rop.fbx`. Put it with its textures in `Prototype/ThirdParty/SuppliedPoliceCar/Tripo/` (or send it to Claude).

Tripo will paint a blurred version of the door lettering and crest into the texture. That is expected: the clean artwork below goes on top as decals in Unity.

## 2. Artwork for ChatGPT (clean PNGs, transparent background)

References in this folder: `36b_door_decal_reference.png` (door, from the photo), `36c_front_plate_reference.png` (front plate, from the photo, blue), `36d_livery_side_reference.png` (the white/navy split), and in `TripoRefs/ForGPT/` on the PC: `rop_plate_red_reference.png` (an Omani red plate used only for the character layout; the patrol car's plate is blue), `rop_crest_from_plate.png` and `rop_crest_from_tailgate.png` (the crest, small).

Please produce, each as a transparent PNG at the stated size:

| File | Size (px) | Content |
|---|---|---|
| `ROP_Crest.png` | 1024 × 1024 | The Royal Oman Police crest as on the door: the national emblem (khanjar over two crossed swords) under a crown, inside a round frame, in white with red and gold details as in the references. Centred, transparent outside the frame. |
| `ROP_DoorDecal.png` | 2048 × 1024 | The whole front-door block: `POLICE` (white, bold sans, left), the crest (centre), `الشرطة` (white, bold Naskh, right), and `www.rop.gov.om` (white, small, centred under the crest). Proportions as in `36b_door_decal_reference.png`; the block is about 0.55 m wide on the car. |
| `ROP_Plate.png` | 1040 × 220 | The ROP patrol-car plate (Koda: **blue plate, white characters**), front and rear identical: number `6669` on the left, class letters `ح و` over `H W` in the middle, `عُمان` on the right with the small ROP crest above it, thin white frame line. Layout as `rop_plate_red_reference.png` but blue (`#1F3F8F`) instead of red. |
| `ROP_LightBar_Lens.png` | 1024 × 128 | Blue translucent lens strip for the low-profile roof bar, with the LED segment pattern. |

Rules: no drop shadows, no bevels, flat colours, nothing outside the stated canvas. Navy for anything that must match the body: `#0B1E4A`. Fonts: a bold grotesque for `POLICE` (Helvetica Bold or Arial Black look), a bold Naskh for the Arabic.

## 3. What Claude does afterwards

Convert the FBX in Blender, replace `ThirdParty/SuppliedPoliceCar/police+car+3d+model.fbx` in the scene, project the decals on the doors, plates and roof bar, update the car geometry checks in `ReviewAndRawCar.cs`, rebake, rebuild.
