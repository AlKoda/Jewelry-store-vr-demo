Build scripts for StreetModels (ignored by Unity because of the ~ suffix). Project-owned.
They were run from a scratch folder with this layout:
  src/<mirror>/<asset>/...   unmodified downloads (fetch_gltf.py BASE_URL NAME.gltf OUTDIR; HDRIs: git sparse checkout of pmndrs/drei-assets)
  simp/<asset>/<asset>.gltf  meshoptimizer-simplified copies (node simplify.mjs / simplify_per.mjs / simplify_adaptive.mjs, needs
                             npm i @gltf-transform/core@4.3.0 @gltf-transform/extensions@4.3.0 @gltf-transform/functions@4.3.0 meshoptimizer@0.22.0)
  build/                     output (build_models.py, build_apartment.py, proc_tex.py + proc_models.py)
decode.mjs + webp_fix.py turn the meshopt/WebP hatchback GLB into a plain glTF with PNG textures.
write_docs.py / write_readme.py regenerate sources.json, README.md and LICENSE.txt; preview_cull.py renders Preview.jpg
(software rasteriser with back-face culling, like Unity's Standard shader; preview.py is the older no-culling version).

Revision 2026-10-15 (run with python3 -P; Python needs numpy, scipy, Pillow, trimesh; ffmpeg for the .hdr decode):
  conv.py              skips the AO bake when the ARM map's AO channel is empty (MetalTrashCan, StreetBench, AirconGrille)
  aircon_grille.py     TEXTURES_DIR OUT.png  -> AirconUnit_AirconGrille_Color.png (alpha = Poly Haven opacity mask)
  plant_split.py       potted_plant_01_1k.gltf split.gltf  (one node per leaf blade / stalk, pot, stem; pebbles dropped)
  simplify_adaptive.mjs split.gltf simp.gltf '{"blade_": [10, 0.9], "pot": [1000, 0.97], "stem": [300, 0.9]}'
  plant_build.py       split.gltf simp.gltf OUTDIR -> PottedPlant.obj/.mtl (stalk tubes, double-sided blades, gravel disc)
  masked_details.py    Downtown_Maskedstuff_1_baseColor.jpeg PACKDIR OUTDIR -> DT_MaskedDetails_Color.png + double-sided BuildingBrickCorner.obj/.mtl
  make_interior.py     <drei-assets>/hdri OUT.jpg -> ApartmentBlock_Interior_Color.jpg (uses pano.py)
  strip_degenerate.py  report.json *.obj  -> drops zero-area triangles (face lines only) from the OBJs in place
  fix_stem_winding.py  PottedPlant.obj    -> re-winds the 30 stem slivers meshoptimizer folded inwards (run after strip_degenerate)
  check_pack.py        PACKDIR -> images open, OBJs load, MTL maps exist, sources.json hashes/coverage, sizes
  comps.py, gltfio.py  helpers (connected components; minimal glTF reader)
Exact source URLs, commits and SHA-256 are in ../sources.json.
