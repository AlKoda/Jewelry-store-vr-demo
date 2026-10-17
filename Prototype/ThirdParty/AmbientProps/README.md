# AmbientProps: lived-in props for the night store, office and street

Infrastructure, office clutter and litter that make the night store and street read as a real, used place. Made for Unity 6000.6.2f1, Built-in pipeline, no packages (OBJ/MTL + PNG). Prepared 2026-10-17. **Not imported or checked in Unity yet (unverified); nothing is wired.**

- 9 Poly Haven models (CC0), 7 Sketchfab models (**CC BY 4.0, attribution required**, see LICENSE.txt), 2 Quaternius models (CC0, low-poly flat-colour style).
- Total 46151 triangles; every prop is at or below about 5k triangles; textures 1024 px PNG base colour only (the converter drops normal/roughness maps).
- Total size: see the end of this file.

## Conventions

| Item | Convention |
|---|---|
| Units | Metres, Y up, Scale Factor 1. Each model is grounded (lowest point y = 0) and centred in x/z by `Tools/glb_to_obj.py --ground`. Wall-mounted items (switch, outlet, AC unit, beacon, power box) therefore need their back moved to the wall in code. |
| Materials | `map_Kd` colour PNG; Standard shader. Quaternius models and AlarmBeacon have constant MTL colours. |
| Sets | ElectricCables, IndustrialPipes, CardboardBoxSet, ACCondenserSet and NetworkServerRackRow contain several pieces laid out side by side; OBJ groups (or connected parts) separate them. |

## Models

| Model | Tris | Size m (x × y × z) | Source / author | Licence | Suggested use |
|---|---|---|---|---|---|
| **IndustrialPipes** | 4,995 | 1.40 x 1.96 x 0.31 | [Modular Industrial Pipes 01](https://polyhaven.com/a/modular_industrial_pipes_01), Jorge Camacho | CC0 1.0 | Exposed pipe runs at the back wall / service corridor / facade (8 pieces laid out side by side; split by OBJ group). |
| **ElectricCables** | 5,235 | 2.08 x 0.76 x 0.21 | [Modular Electric Cables](https://polyhaven.com/a/modular_electric_cables), Kuutti Siitonen | CC0 1.0 | Surface-mounted conduit/cable runs and junction boxes; 49-piece kit laid flat in the XY plane on a grid, each piece (OBJ group) needs its own placement. |
| **SprayPaintBottles** | 4,000 | 0.16 x 0.23 x 0.07 | [Spray Paint Bottles](https://polyhaven.com/a/spray_paint_bottles), James Ray Cock | CC0 1.0 | Discarded cans in the back alley; clean and dirty material variants. |
| **PlasticCrate** | 4,000 | 0.30 x 0.26 x 0.41 | [Plastic Crate 01](https://polyhaven.com/a/plastic_crate_01), PierreB3D | CC0 1.0 | Back-room storage, stacked by the rear door. |
| **UtilityBoxSmall** | 3,999 | 0.92 x 1.12 x 0.43 | [Utility Box 02](https://polyhaven.com/a/utility_box_02), James Ray Cock | CC0 1.0 | Electrical cabinet on the facade/back wall (complements StreetModels/UtilityBox). |
| **PlasticThermos** | 2,999 | 0.14 x 0.32 x 0.11 | [Plastic Thermos](https://polyhaven.com/a/plastic_thermos), PierreB3D | CC0 1.0 | Office/staff-room desk clutter (stand-in for a mug). |
| **RustedCan** | 3,520 | 0.12 x 0.15 x 0.12 | [Can Rusted](https://polyhaven.com/a/can_rusted), Rahul Chaudhary | CC0 1.0 | Litter in gutter/alley (a food tin, not a crushed drinks can). |
| **PowerBox** | 4,997 | 0.46 x 0.50 x 0.40 | [Power Box 01](https://polyhaven.com/a/power_box_01), Rico Cilliers, Yann Kervran | CC0 1.0 | Wall-mounted electrical box in the office / back room. |
| **WetFloorSign** | 228 | 0.30 x 0.63 x 0.36 | [Wet Floor Sign 01](https://polyhaven.com/a/WetFloorSign_01), Fran Calvente | CC0 1.0 | Cleaner left it near the staff door. |
| **NetworkServerRackRow** | 1,144 | 1.11 x 1.83 x 2.10 | [Network Server Rack](https://sketchfab.com/3d-models/network-server-rack-3b15fb8779274844b54e63a40194d450), Khoa Nguyen (https://sketchfab.com/ngdkh) | CC-BY 4.0 | Row of three 1.8 m network cabinets (about 0.87 m wide x 0.75 m deep each) for the office/back room; long axis along Z. |
| **IndoorACUnit** | 528 | 1.25 x 0.42 x 0.31 | [Indoor air conditioner unit](https://sketchfab.com/3d-models/indoor-air-conditioner-unit-d93c5557a9ba46afbb00e35f48343077), Rylae Shylna (https://sketchfab.com/risteralline) | CC-BY 4.0 | Wall split AC indoor unit, high on the office or sales-floor wall (scale 2.35 as used by the mirror -> 1.25 m wide). |
| **ACCondenserSet** | 584 | 6.21 x 4.53 x 5.12 | [air conditioners](https://sketchfab.com/3d-models/air-conditioners-65417369a2724c9ebf06e7698238a856), Lora (https://sketchfab.com/Lora_o) | CC-BY 4.0 | Outdoor condenser collection (centimetre source scaled x0.01): three floor units about 2.1 x 1.0 x 1.5 m and five wall units about 1.1 x 0.85 x 0.45 m in one OBJ. Split by connected parts or place the whole cluster on the back yard/roof; consider x0.75 for domestic-size wall units. |
| **LightSwitch** | 3,668 | 0.01 x 0.11 x 0.07 | [Light Switch](https://sketchfab.com/3d-models/light-switch-808c3de4dd874fc1b67ff15cd2645555), Avot (https://sketchfab.com/avot) | CC-BY 4.0 | Beside doors at 1.1 m; face is along +/-X (rotate to the wall). |
| **PowerOutlet** | 28 | 0.08 x 0.12 x 0.01 | [Outlet](https://sketchfab.com/3d-models/outlet-192d0b1de17f4fcdbfeca3fe873c7bdf), Drake (https://sketchfab.com/dlp14c) | CC-BY 4.0 | Wall sockets at 0.3 m along office/back-room walls (US duplex style). |
| **AlarmBeacon** | 3,997 | 0.14 x 0.18 x 0.09 | [Alarm Light](https://sketchfab.com/3d-models/alarm-light-0e009c47066d4ab4b4329fdd4820f141), 5CNG5 (https://sketchfab.com/5CNG5) | CC-BY 4.0 | Wall-mounted alarm beacon/sounder (scale 0.17 as used by the mirror); Lumina material should be emissive red. No textures (constant MTL colours). |
| **CardboardBoxSet** | 1,188 | 1.45 x 0.85 x 0.94 | [Set of Cardboard Boxes](https://sketchfab.com/3d-models/set-of-cardboard-boxes-8986ba512f704ac5b253286a0d1ad8bb), NotAnotherApocalypticCo. (https://sketchfab.com/notanotherapocalypticco) | CC-BY 4.0 | Four box sizes side by side (11 OBJ groups); flattened/stacked boxes by the bins or in the back room. |
| **WheelieBin** | 739 | 0.61 x 1.11 x 0.80 | [Wheelie bin](https://poly.pizza/m/XSwahu252t), Quaternius | CC0 1.0 | Back alley / beside the rear door. Low-poly, flat colours (no textures); set metallic 0. |
| **Dumpster** | 302 | 1.71 x 1.42 x 0.91 | [Dumpster](https://poly.pizza/m/PKsbolkZSr), Quaternius | CC0 1.0 | Back alley. Low-poly, flat colours (no textures); set metallic 0. |

## Not found (left for later)

Free, licence-verifiable models were not found on GitHub-reachable mirrors for: router / UPS / power strip, rugs and door mats, mugs (PlasticThermos stands in), paper cups, plastic drink bottles, crushed drinks cans, plastic bags, cigarette butts, fallen leaves/debris meshes, ceiling ducts, signage brackets. These are better made as project geometry or decals (e.g. Poly Haven CC0 `leaves_forest_ground` texture for leaf litter decals; InteriorModels already has Rug, WallVent, CeilingDiffuser, CardboardBox, LeverArchFiles, OfficeChair, FireAlarmCallPoint; StreetModels has AirconUnit, MetalTrashCan, WasteContainer1100L; ForensicModels has a trash bag).
Total size: 20.9 MB (20875620 bytes before this README line was added). Largest files are the 1024 px PNGs (under 1.7 MB each).
