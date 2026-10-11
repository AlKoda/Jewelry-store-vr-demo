# Reuse existing licensed models

Use the files at the paths in models.json; do not import another complete pack.
The manifest measures existing OBJ geometry, validates face indices and resolves
MTL diffuse atlases. It does not certify pivots, collider behavior or Unity rendering.
Full measurements: Docs/AssetPreparation/model-audit.md.

- Police car: KayKitCity/car_police plus all four wheel OBJ files. 1,316 triangles
  total; existing ShopCityRefinement.Car uses 4.5 scale and the authored wheel
  offsets. Keep the assembly; do not center each wheel independently. Nose +Z
  per NightStoreLayout. Body length is about 4.22 m at that scale. Keep its generic
  livery pending country/service decision. Headlight alignment remains unverified.
- Streetlight: 176 triangles; existing 4.5 scale gives ~4.32 m height.
- Bench: 44 triangles; scale 4.5 gives ~1.8 m width. Exterior seating option.
- chair_A: 308 triangles; start 0.66 scale, ~0.83 m height; rear office option.
- couch: 636 triangles; start 0.66 scale, ~1.98 m width; front-right seating option.
- table_low: 276 triangles; start 0.66 scale, ~1.58 x 0.33 x 0.99 m; seating table.
- cactus_medium_A: 448 triangles; start 0.66 scale, ~0.55 m height; one decor instance.
- cabinet_small_decorated: 464 triangles; start 0.66 scale, ~1.07 m height; rear storage.

City models share citybits_texture.png; furniture shares furniturebits_texture.png.
Use one shared atlas material per pack and simple box colliders; avoid MeshCollider
on decorative meshes. Bake static props; do not add physics to each detail.
Retain procedural StoreKit display cases, safes, doors, torch and DSLR for now:
their parts, hinge conventions and broken variants are tailored to the demo.
Renaming a static pack OBJ to safe_large would lose the expected moving-door setup.
Do not put these optional furniture files in the StoreKit override folder without
checking metric size, -Z front, pivot and hierarchy. Existing night car reuse stays
unchanged. No new burglary-tool model or country-specific service branding added.
