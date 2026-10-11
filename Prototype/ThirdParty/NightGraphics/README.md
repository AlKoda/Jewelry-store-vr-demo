# Night graphics

Ten editable SVG masters plus Unity-ready PNGs; the SVGs use outlined geometric
lettering, so no font installation or Unity SVG package is required. Use PNGs in Unity.
Preview: Docs/AssetPreparation/graphics-preview.png.

- ShopFascia: 1024 x 256, proposed 4 x 1 m frontage sign.
- CollectionRings / CollectionNecklaces: 512 x 256, proposed 0.8 x 0.4 m interior signs.
- Closed / StaffOnly: 512 x 256, proposed 0.4 x 0.2 m door signs.
- SceneTape: 1024 x 128, generic text without service/country branding. Repeat U only.
- MarkerNumbers: 512 x 256, 4 columns x 2 rows, numbers 1–4 above 5–8.
  Each 128 px cell has whitespace. Unity bottom-origin UV offset for number n:
  ((n-1)%4/4, 1-(floor((n-1)/4)+1)/2); scale=(0.25,0.5). Inset UV edges slightly
  if filtering bleeds; retain current depth-tested labels when they are clearer.
- ShoeTrace_Generic: 512 x 512 RGBA; illustrative 0.30 m-long shoe trace.
  Art occupies about 78% of image height: use a ~0.385 m square quad for a 0.30 m mark.
- Scuff_Generic / DustSmear_Generic: 512 x 256 RGBA; optional subtle floor dressing.

Signs/tape/markers: sRGB on, mipmaps on, bilinear or trilinear filtering. Signs
and atlas Clamp; tape Repeat U / Clamp V. Use opaque Standard materials and no
emission so the shop stays dark. Fine lettering needs in-headset readability review.
Decals: sRGB on, Alpha Is Transparency on, clamp, mipmaps on. Put on small quads
2–3 mm above the surface, disable shadow casting, and avoid stacking transparent
quads. The Built-in pipeline has no assumed URP decal dependency here. Standard
Fade is adequate for a first material; confirm sorting and no z-fighting in Unity.
Keep alpha straight; do not add UV emission. Decals remain unplaced until the
instructor settles the story. Nothing is automatically classified as evidence.
