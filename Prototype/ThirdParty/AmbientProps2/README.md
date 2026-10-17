# AmbientProps2: second batch of lived-in props

Fills the gaps left by AmbientProps (mugs, cups, cans, bottles, takeaway boxes, rugs and door mats, coat rack, shop shelving, cash register, ceiling ducts, street planters, street trees with leaves, parked civilian cars, a scooter). Made for Unity 6000.6.2f1, Built-in pipeline, no packages (OBJ/MTL + JPG/PNG). Prepared 2026-10-17. **Not imported or checked in Unity yet (unverified); nothing is wired.**

- 56 models: 24 Poly Haven (CC0, PBR maps), 23 Kenney (CC0, stylised colour atlas or flat colours), 9 Poly Pizza (6 CC0 Quaternius / Isa Lousberg, 3 **CC BY 3.0, attribution required**, see LICENSE.txt).
- Total 156,503 triangles, 24.0 MB. Textures at most 1024 px.

## Conventions

| Item | Convention |
|---|---|
| Units | Metres, Y up, Scale Factor 1, lowest point y = 0, centred in x/z. FluorescentBatten uses a ceiling pivot (top at y = 0). Front/long axis is whatever the source used (vans and scooter: long axis X). |
| PBR maps (Poly Haven) | Converted with InteriorModels/Source~/convert_ph.py (via Source~/build_ph2.py), not Tools/glb_to_obj.py, so normal and roughness are kept: `<Name>[_<Mat>]_Color.jpg` (map_Kd), `_Normal.jpg` (tangent space, OpenGL +Y), `_MetallicSmoothness.png` (R = metallic, A = smoothness). The MTL names the extra maps in `# Normal <file>` / `# MetallicSmoothness <file>` comment lines, as in InteriorModels and StreetModels, so the same importer code applies; Unity OBJ import ignores them, so they must be assigned by the asset library (normal map texture type must be set to Normal map). Glass materials (CashRegisterVintage/Glass) carry `d` < 1. |
| Kenney / Poly Pizza | Tools/glb_to_obj.py --ground --scale (scale per model in sources.json). Kenney kits share a 512 px colour atlas, copied per model as `<Name>_colormap.png`; the furniture kit, SUV, van and shopping cart use constant MTL colours. Stylised low-poly look: best as distant or small clutter. |
| Sets | AirDuctKit (16 duct pieces), TeaSetMugs (tray, pot and cups), SuitcaseVintage (two cases) are several pieces in one OBJ; split by connected parts. |
| Decimation | Poly Haven models above about 5k were reduced with seam-preserving decimation; Broom (9.7k, bristles), ToolCart (8k), AirDuctKit (10k, whole kit), WornMetalRack (6.4k), FluorescentBatten (6.5k) and ParkedPickup (6.4k) stay above 5k. |

## Models

| Model | Tris | Size m (x × y × z) | Maps | Source / author | Licence | Suggested use |
|---|---|---|---|---|---|---|
| **CashRegisterVintage** | 5,003 | 0.60 x 0.62 x 0.62 | Color+Normal+MS | [Cash Register 01](https://polyhaven.com/a/CashRegister_01), Joe Seabuhr | CC0 1.0 | Vintage brass till; back counter / display piece (the working POS is JewelryModels/PosTablet + PaymentTerminal). |
| **StreetPlanterBox** | 5,038 | 0.91 x 0.42 x 0.41 | Color+Normal+MS | [Planter Box 01](https://polyhaven.com/a/planter_box_01), James Ray Cock | CC0 1.0 | Potted planter by the shop entrance / sidewalk. |
| **WaterBottleGallon** | 4,000 | 0.17 x 0.28 x 0.13 | Color+Normal+MS | [Plastic Bottle Gallon](https://polyhaven.com/a/plastic_bottle_gallon), Rahul Chaudhary | CC0 1.0 | Plastic drink/water bottle on desks, back room (0.27 m). |
| **FoodPackets** | 4,712 | 0.52 x 0.24 x 0.11 | Color+Normal+MS | [Long Life Food](https://polyhaven.com/a/long_life_food), Mia Pecina Zorko | CC0 1.0 | Snack/food packets on the staff-room shelf or a desk. |
| **PlasticTub** | 4,070 | 0.90 x 0.43 x 0.63 | Color+Normal+MS | [Plastic Container](https://polyhaven.com/a/plastic_container), not recorded in a reachable mirror (see the Poly Haven page) | CC0 1.0 | Storage tub in the back room. |
| **DisplayShelvesWood** | 3,174 | 0.37 x 1.56 x 1.08 | Color+Normal+MS | [Wooden Display Shelves 01](https://polyhaven.com/a/wooden_display_shelves_01), James Ray Cock | CC0 1.0 | Shop shelving (1.56 m) for the sales floor or back office. |
| **Broom** | 9,676 | 0.42 x 1.20 x 0.57 | Color+Normal+MS | [Plastic Broom](https://polyhaven.com/a/plastic_broom), not recorded in a reachable mirror (see the Poly Haven page) | CC0 1.0 | Leaning in the back room / by the staff door. 9.7k tris (bristles kept at source detail). |
| **Dustpan** | 4,332 | 0.33 x 0.10 x 0.37 | Color+Normal+MS | [Dustpan](https://polyhaven.com/a/dustpan), not recorded in a reachable mirror (see the Poly Haven page) | CC0 1.0 | With the broom. |
| **CigarettePack** | 2,814 | 0.06 x 0.10 x 0.03 | Color+Normal+MS | [Cigarette Pack](https://polyhaven.com/a/cigarette_pack), not recorded in a reachable mirror (see the Poly Haven page) | CC0 1.0 | Street/alley litter, staff smoking spot. |
| **FluorescentBatten** | 6,522 | 0.91 x 0.04 x 0.65 | Color+Normal+MS | [Mounted Fluorescent Lights](https://polyhaven.com/a/mounted_fluorescent_lights), Ulan Cabanilla | CC0 1.0 | Ceiling batten for back room / office (pivot: top at y = 0, hangs down). |
| **MetalStool** | 5,000 | 0.35 x 0.88 x 0.35 | Color+Normal+MS | [Metal Stool 01](https://polyhaven.com/a/metal_stool_01), not recorded in a reachable mirror (see the Poly Haven page) | CC0 1.0 | Workbench / back-office stool. |
| **TeaSetMugs** | 4,999 | 0.92 x 0.17 x 0.48 | Color+Normal+MS | [Tea Set 01](https://polyhaven.com/a/tea_set_01), Rico Cilliers (model), James Ray Cock (texturing), Jurita Burger (graphic design) | CC0 1.0 | Mugs/cups on a tray in the staff room (split by connected parts for single cups). |
| **ConcreteRoadBarrier** | 5,000 | 1.54 x 0.83 x 0.64 | Color+Normal+MS | [Concrete Road Barrier](https://polyhaven.com/a/concrete_road_barrier), Amal Kumar | CC0 1.0 | Street edge / closed-off lane (1.55 m). |
| **SidewalkChalkboard** | 2,372 | 0.92 x 1.51 x 0.76 | Color+Normal+MS | [Standing Chalkboard 01](https://polyhaven.com/a/standing_chalkboard_01), ParzivalCG | CC0 1.0 | A-board shop sign on the pavement. |
| **SuitcaseVintage** | 4,998 | 1.61 x 0.57 x 0.24 | Color+Normal+MS | [Vintage Suitcase](https://polyhaven.com/a/vintage_suitcase), Maximilian Schuster | CC0 1.0 | Two suitcases (side by side); lost property / office. |
| **MonoblocChair** | 3,356 | 0.64 x 0.88 x 0.63 | Color+Normal+MS | [Plastic Monobloc Chair 01](https://polyhaven.com/a/plastic_monobloc_chair_01), Kuutti Siitonen | CC0 1.0 | Staff smoking spot / back yard. |
| **PlasticJug** | 4,000 | 0.28 x 0.21 x 0.17 | Color+Normal+MS | [Jug 01](https://polyhaven.com/a/jug_01), Kuutti Siitonen | CC0 1.0 | Staff room counter. |
| **WornMetalRack** | 6,372 | 0.92 x 1.90 x 0.60 | Color+Normal+MS | [Worn Metal Rack](https://polyhaven.com/a/worn_metal_rack), Luca B | CC0 1.0 | Back-room stock shelving (1.9 m). |
| **CleanerSprayBottle** | 4,000 | 0.09 x 0.23 x 0.09 | Color+Normal+MS | [Multi Cleaner Bottle](https://polyhaven.com/a/multi_cleaner_bottle), James Ray Cock | CC0 1.0 | Glass cleaner by the counter / cleaning cupboard. |
| **LaptopClassic** | 5,017 | 0.65 x 0.54 x 0.49 | Color+Normal+MS | [Classic Laptop](https://polyhaven.com/a/classic_laptop), Arrangemonk | CC0 1.0 | Office desk (older laptop stands in for the missing PC tower). |
| **WickerBasket** | 5,000 | 0.35 x 0.20 x 0.25 | Color+Normal+MS | [Wicker Basket 02](https://polyhaven.com/a/wicker_basket_02), not recorded in a reachable mirror (see the Poly Haven page) | CC0 1.0 | Shop decoration / staff room. |
| **ToolCart** | 8,000 | 1.27 x 0.96 x 0.75 | Color+Normal+MS | [Tool Cart](https://polyhaven.com/a/tool_cart), Savva Zakharov | CC0 1.0 | Maintenance cart in the back room. 8k tris. |
| **JerryCan** | 4,812 | 0.35 x 0.46 x 0.17 | Color+Normal+MS | [Metal Jerrycan](https://polyhaven.com/a/metal_jerrycan), Sean Buckley | CC0 1.0 | Back yard / alley. |
| **AirDuctKit** | 10,000 | 4.17 x 1.32 x 1.46 | Color+Normal+MS | [Modular Airduct Circular 01](https://polyhaven.com/a/modular_airduct_circular_01), Riley Queen | CC0 1.0 | Ceiling duct kit: 16 pieces (straights, bends, braces, fan, rectangular converter) laid out side by side in one OBJ; split by connected parts. 10k tris for the whole kit. |
| **ParkedSUV** | 3,294 | 2.11 x 1.53 x 4.21 | flat MTL colours | [SUV / crossover](https://poly.pizza/m/xsMtZhBkxL), Quaternius | CC0 1.0 | Parked civilian car (4.2 m), flat colours. |
| **ParkedPickup** | 6,432 | 2.31 x 1.85 x 5.18 | Color | [Pickup truck](https://poly.pizza/m/qn4grQgHm8), Quaternius | CC0 1.0 | Parked pickup (5.2 m); 6.4k tris. |
| **ParkedVan** | 3,225 | 4.26 x 2.38 x 2.39 | flat MTL colours | [Panel van](https://poly.pizza/m/BbRojf2v3H), PuKkBuMXDD | CC-BY 3.0 | Parked panel van (4.3 m, long axis X), flat colours. |
| **StreetTreeMaple** | 4,800 | 6.70 x 6.71 x 6.24 | Color | [Red maple (MapleTree_2)](https://poly.pizza/m/iGFtQd0PJO), Quaternius | CC0 1.0 | Street tree with leaves (6.7 m); scale 0.7 for a young street tree. |
| **StreetTreeYoung** | 4,999 | 5.67 x 5.10 x 2.40 | Color+Normal+MS | [Young broadleaf tree 4 (NormalTree_4)](https://poly.pizza/m/etFGNvsiFv), Quaternius | CC0 1.0 | Street tree (5.1 m); decimated 6,338 -> 4,999 tris with tools/glb_to_obj.py --target-tris (UVs resampled: check leaves). |
| **FloweringBush** | 1,368 | 1.92 x 1.58 x 1.97 | Color+Normal+MS | [Bush with flowers](https://poly.pizza/m/U1ymDy8tbY), Quaternius | CC0 1.0 | Planter filler / tree pit. |
| **ShoppingCart** | 2,868 | 0.66 x 1.00 x 1.22 | flat MTL colours | [Shopping cart](https://poly.pizza/m/4lhRcMbl1pw), Poly by Google | CC-BY 3.0 | Abandoned trolley on the street. |
| **ParkLitterBin** | 504 | 0.80 x 0.91 x 0.80 | Color | [Park litter bin](https://poly.pizza/m/2UXIdGYkg9), Isa Lousberg | CC0 1.0 | Pavement litter bin. |
| **ParkedScooter** | 2,116 | 1.83 x 1.28 x 0.81 | Color | [Scooter](https://poly.pizza/m/9RqkkBOPNtd), Poly by Google | CC-BY 3.0 | Parked moped/scooter on the pavement (1.8 m). Licence evidence: v1/attributions.md of the mirror only. |
| **MugStylised** | 232 | 0.12 x 0.10 x 0.10 | Color | [Kenney Food Kit: mug](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Mug on desks / counter. |
| **CupCoffeeKenney** | 108 | 0.08 x 0.05 x 0.10 | Color | [Kenney Food Kit: cup-coffee](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Coffee cup (Kenney "cup-coffee"; 0.05 m tall at this scale: check the shape and rescale). |
| **TeaCup** | 144 | 0.08 x 0.07 x 0.10 | Color | [Kenney Food Kit: cup-tea](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Cup on a desk. |
| **DrinkCan** | 164 | 0.08 x 0.12 x 0.08 | Color | [Kenney Food Kit: soda-can](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Drink can (0.12 m). |
| **DrinkCanCrushed** | 212 | 0.09 x 0.06 x 0.09 | Color | [Kenney Food Kit: soda-can-crushed](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Crushed can litter in gutter / alley. |
| **PlasticSodaBottle** | 100 | 0.06 x 0.20 x 0.06 | Color | [Kenney Food Kit: soda-bottle](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Plastic drink bottle (0.2 m). |
| **GlassBottle** | 96 | 0.05 x 0.22 x 0.05 | Color | [Kenney Food Kit: bottle-oil](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Glass bottle (Kenney oil bottle, 0.22 m) as litter / staff room. |
| **PizzaBox** | 84 | 0.33 x 0.31 x 0.33 | Color | [Kenney Food Kit: pizza-box](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Takeaway pizza box on the office desk or by the bins. |
| **NoodleBox** | 162 | 0.16 x 0.20 x 0.20 | Color | [Kenney Food Kit: chinese](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Takeaway noodle box. |
| **TakeawayClamshell** | 240 | 0.44 x 0.29 x 0.27 | Color | [Kenney Food Kit: styrofoam-dinner](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Open foam takeaway box with food. |
| **TakeawayClamshellEmpty** | 152 | 0.44 x 0.29 x 0.27 | Color | [Kenney Food Kit: styrofoam](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Open empty foam takeaway box (litter). |
| **DrinkCarton** | 94 | 0.08 x 0.21 x 0.08 | Color | [Kenney Food Kit: carton](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Drink carton. |
| **BagFlat** | 114 | 0.23 x 0.07 x 0.16 | Color | [Kenney Food Kit: bag-flat](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Flattened bag (litter). |
| **PaperBag** | 46 | 0.13 x 0.36 x 0.25 | Color | [Kenney Food Kit: bag](https://kenney.nl/assets/food-kit), Kenney (www.kenney.nl) | CC0 1.0 | Paper shopping/takeaway bag (0.36 m). |
| **ShopShelfBags** | 892 | 1.60 x 1.73 x 1.40 | Color | [Kenney Mini Market: shelf-bags](https://kenney.nl/assets/mini-market), Kenney (www.kenney.nl) | CC0 1.0 | Stocked shop shelf (stylised; 1.4 m deep at this scale, consider 1.4-1.6). |
| **ShopShelfBoxes** | 436 | 1.60 x 1.70 x 1.40 | Color | [Kenney Mini Market: shelf-boxes](https://kenney.nl/assets/mini-market), Kenney (www.kenney.nl) | CC0 1.0 | Stocked shop shelf with boxes. |
| **ShoppingBasket** | 288 | 0.46 x 0.33 x 0.46 | Color | [Kenney Mini Market: shopping-basket](https://kenney.nl/assets/mini-market), Kenney (www.kenney.nl) | CC0 1.0 | Hand basket. |
| **BottleReturnMachine** | 324 | 0.74 x 1.80 x 0.79 | Color | [Kenney Mini Market: bottle-return](https://kenney.nl/assets/mini-market), Kenney (www.kenney.nl) | CC0 1.0 | Vending/bottle-return machine (1.8 m) for a street front. |
| **CoatRackStanding** | 190 | 0.64 x 1.81 x 0.64 | flat MTL colours | [Kenney Furniture Kit: coatRackStanding](https://kenney.nl/assets/furniture-kit), Kenney (www.kenney.nl) | CC0 1.0 | Coat stand in the office (1.8 m), flat colours. |
| **CoatRackWall** | 132 | 1.05 x 0.66 x 0.32 | flat MTL colours | [Kenney Furniture Kit: coatRack](https://kenney.nl/assets/furniture-kit), Kenney (www.kenney.nl) | CC0 1.0 | Wall hook rail in the staff room. |
| **DoorMat** | 204 | 0.75 x 0.02 x 0.41 | flat MTL colours | [Kenney Furniture Kit: rugDoormat](https://kenney.nl/assets/furniture-kit), Kenney (www.kenney.nl) | CC0 1.0 | Door mat at the entrance / staff door (0.75 x 0.42 m). |
| **RugRectangle** | 28 | 2.75 x 0.02 x 1.61 | flat MTL colours | [Kenney Furniture Kit: rugRectangle](https://kenney.nl/assets/furniture-kit), Kenney (www.kenney.nl) | CC0 1.0 | Rectangular rug (2.75 x 1.6 m) for the office. |
| **RugRound** | 188 | 1.61 x 0.02 x 1.61 | flat MTL colours | [Kenney Furniture Kit: rugRound](https://kenney.nl/assets/furniture-kit), Kenney (www.kenney.nl) | CC0 1.0 | Round rug (1.6 m). |

## Not found (gaps)

- Router, network switch, UPS, power strip, desktop PC tower, office printer: no CC0/CC BY model with verifiable licence found in reachable mirrors (LaptopClassic is the stand-in; NetworkServerRackRow in AmbientProps covers the rack).
- Newspapers, ceiling-mounted rectangular duct (only the circular kit), street signage brackets, hand umbrella (only a CC BY beach parasol exists), bicycle (a "City Pack" Bicycle.glb exists in ssebs/DankNooner without per-item licence evidence, not taken), water cooler and card terminal (already in InteriorModels/WaterCooler and JewelryModels/PaymentTerminal).
- Photoreal street trees within 5k tris: Poly Haven trees are 30k to 1.6M tris; only the stylised Quaternius trees fit.

## Rebuild

Source~/build_ph2.py (Poly Haven, needs trimesh, numpy, pillow, scipy, fast_simplification), Source~/unglb.py (GLB to glTF + bin + images), Source~/objstat.py (stats), Source~/write_docs.py and write_text.py (sources.json, LICENSE.txt, README.md, register rows). Paths in the scripts point at the scratch clone used on 2026-10-17.

Total size: 24.0 MB (219 files in the pack root, Source~ excluded from Unity import by the trailing ~).
