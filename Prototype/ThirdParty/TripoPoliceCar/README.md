# Koda's Tripo Oman patrol car

User supplied October 25, 2026: `police car 3d model.glb` and
`police car 3d model (1).zip`, generated in Tripo Studio from the chosen
Royal Oman Police Mercedes sedan photograph. Original GLB and FBX are retained
under `Original~`, which Unity ignores. Original JPEG is copied byte-for-byte
as `PatrolOriginal_Color.jpg`; it remains the active body colour source.

`Tools/refine_tripo_car.py` exports the source body at a uniform 5.12 m length,
retaining body UVs and supplied corner normals. It replaces wheel faces with
original project-made tyre/rim/rotor/caliper/nut assemblies. No subdivision,
smoothing modifier, body remesh or flattening is applied. `PatrolDistance.obj`
retains the complete original 15,000 triangles. `geometry.json` records source
hashes and generated counts; Unity removes degenerate wheel-cap triangles.

Unity authoring: `PresentationPolish.PatrolCarDeliveryRun`. The existing car
placement and original car asset pack are retained. Near model has approximately
25,576 triangles; far model has 15,000. Four wheel
materials share one renderer. A baked 128px reflection probe excludes the car's
visual layer, preventing reflections of its own cabin/body. No new live light.

`CrimeScene/PatrolCarFinish` retains the original paint texture and separates
paint, tinted exterior glazing, rubber, metal and lamp surface responses. Native
Arabic/English door markings and number-free Arabic plates are projected onto
the existing mesh, avoiding floating cards. A localized ink cleanup samples
four unlettered samples from the original navy paint, with a feathered edge;
it does not replace the original colour file. Sample UVs/colours are recorded
in paint-samples.json and embedded in the shader.
Artwork reuses the project's InvestigationDetail native lettering and official
ROP emblem; provenance and rights restrictions are in LICENSE.txt/sources.json.

This is an exterior model; it has no detailed cabin or operable car doors.
Glazing is deliberately tinted/opaque rather than exposing an empty interior.
Some fine shape irregularity remains from single-image reconstruction.
