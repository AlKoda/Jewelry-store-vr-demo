# Road and sidewalk materials — 21 October 2026

Generated with the built-in imagegen tool. Original outputs preserved, no external stock imagery. In-game variants are imported at 1024 pixels with mipmaps; parking mask is authored from scene dimensions, not generated signage. Crack masks are packed into RGBA to reduce texture bindings. These images contain no captured lighting.

## RoadAggregateOriginal.png
Output: exec-1c457302-ab2a-4e80-bd9d-94c77080a9d7.png

Use case: photorealistic-natural. Asset type: seamless square base-color scan texture for dry asphalt in an Omani commercial street, to be tiled unobtrusively in a Quest 3 game. Orthographic straight overhead macro material scan covering approximately 80 cm of roadway. Dark neutral gray weathered asphalt with finely varied crushed stone aggregate, subtly sun-aged binder and tiny mineral flecks. Uniform material grain and restrained contrast, natural realistic rough surface, no obvious memorable large features. Completely flat diffuse illumination with NO directional shadows, NO highlights and NO baked ambient occlusion. NO cracks, NO lane markings, NO potholes, NO litter, NO large patches, NO distinctive streaks, NO writing. Tile seamlessly across all four edges. Entire image is the material; no border. Realistic fine aggregate, not a repeated graphic pattern, not gravel piles.

## RoadNetworkOriginal.png
Output: exec-471ad631-3559-47c0-949c-49fb08516f0e.png

Use case: photorealistic-natural. Asset type: square seamless grayscale crack MASK for a game road material, NOT a picture of a road. Pure BLACK background, thin WHITE irregular interconnected asphalt fissures only. A believable aged road crack network spanning the whole tile, with a few long branching connected veins, junctions, small secondary hairline branches and a few irregular larger cells. Natural stress cracking, varied widths, mostly fine 1–4 pixel white lines and occasional 6-pixel wider fissures at junctions, soft antialiased edges. About 90 to 95 percent of the image stays pure black; avoid dense crazy paving or a uniform cobweb/grid. White means recessed crack coverage; black means intact asphalt. No aggregate grain, no gray pavement fill, no lighting, no shadows, no highlights, no 3D bevel, no text, no border, no paint or lane markings. Cracks meet opposite edges seamlessly for tiling. Output directly useful as a single-channel material mask.

## RoadWeatherOriginal.png
Output: exec-0f5708c7-3018-45e0-85ae-5f09c5aa99e4.png

Use case: photorealistic-natural. Asset type: square seamless grayscale asphalt weathering and worn-paint breakup MASK for a game material. Neutral MID-GRAY overall with restrained darker and lighter variation, soft broad irregular sun-aged and dusty patches mixed with much finer speckled granular wear and scuffed porous areas. Natural roadside abrasion and sun weathering, low contrast, seamless all edges, uniform overhead flat signal map. No directional lighting, no photographic shadows, no road markings, no text, no cracks, no potholes, no objects, no borders. Avoid a regular noise/cloud grid, distinctive repeated dark spots or stripes. This is a scalar mask used both for slow broad road color variation and fine chipped-paint breakup, so preserve fine irregular grain within gentle larger patches; mostly values between gray 80 and gray 180, not pure black or white.

## RoadStressOriginal.png
Output: exec-1d2d78c1-71d0-4f07-a516-b9feb36cd218.png

Use case: photorealistic-natural. Asset type: black-and-white texture mask for longitudinal asphalt cracking in a realistic game. Pure BLACK background. Thin WHITE narrow jagged meandering stress fissures, mostly long veins running roughly vertically with varied little branches extending sideways and a few intersections. Different from an all-over polygon/cobweb pattern: asymmetrical sparse connected hairline roads cracks, different lengths and thicknesses, approximately 95 percent pure black. Antialiased white hairlines 1–4 pixels, occasional slightly wider sections. Entire square is flat mask data: no asphalt fill, no shadows, no highlights, no lighting, no writing, no border, no lane paint. White pixels denote sunken cracks, black intact road. Fine physically plausible asphalt fatigue fissures. Tileable edges if possible.

## RoadFatigueOriginal.png
Output: exec-da52754d-36d3-411e-969e-ac34cfbd459b.png

Use case: photorealistic-natural. Asset type: square flat grayscale material MASK of localized alligator cracking in old asphalt. Pure BLACK background. Two or three irregular loose areas of fine WHITE connected fatigue cracks, with small polygonal cells near damaged centers, dissolving into isolated short branching fissures and intact black margins. Naturally uneven, different cell sizes; no perfect web, regular grid or stone paving pattern. Mostly fine 1–3 pixel white antialiased lines, occasional wider branching junctions. Keep at least 90 percent of the image black. No road surface color, no gravel, no shadows, no highlights, no lighting, no text, no border. White denotes recessed cracks. Believable minor aged-road cracking, not a destroyed road or giant chasms. Keep the outer edges black to place the damage in selected areas.

## PavingFracturesOriginal.png
Output: exec-09d59f85-b6dd-4288-bc1b-76b413f2037e.png

Use case: photorealistic-natural. Asset type: square grayscale mask for sparse hairline cracks in concrete sidewalk paving slabs. Pure BLACK background. Thin WHITE irregular angular fractures with small branches, different isolated zigzag cracks, a few Y-shaped cracks and some subtle chipped-edge traces; physically plausible worn concrete. Do NOT draw regular tile joints or square paving blocks; the game provides those separately. Sparse defects scattered with broad intact black areas. Lines mostly 1–3 pixels wide with antialiased edges. No full cobweb, no dense alligator cracking, no huge chasms. No surface fill, no lighting, no shadows, no color, no texture grain, no writing, no border. Flat single-channel white crack coverage on black, different concrete fracture shapes suitable for random placement on maintained and weathered slabs.

## PavingAggregateOriginal.png
Output: exec-0a54b0f9-06a1-407c-8d9f-0193746587f2.png

Use case: photorealistic-natural. Square seamless diffuse base-color material scan of aged pale warm gray concrete sidewalk, fine sand aggregate and subtle pores, dry sun-weathered Omani commercial street. Flat orthographic overhead lighting, uniform fine grain, low contrast, no shadows, no specular highlights. No paving joints, no slab grid, no cracks, no objects, no paint, no text, no distinctive large patches. All four edges seamless. Fine realistic concrete suitable for game paving blocks with separate generated joints and crack masks.

