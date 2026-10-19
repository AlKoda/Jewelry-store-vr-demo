# PBRInterior: interior materials for the night store

Staged on October 14, 2026. **Not wired into any scene and not yet imported or checked in Unity.**
The pack holds 18 seamless PBR material sets for the 16 x 12 m jewellery store: the 16
requested names plus two extras (StoneTopDark, BrassPolished). Sources are ambientCG CC0
scans or procedural materials, adapted for the Built-in Standard (metallic) shader, and
two sets are fully procedural project work. Licences are in LICENSE.txt. Hashes and
provenance are in sources.json. The reproducible build is Source~/build_pbr_interior.py.
Unity ignores folders whose names end in `~`.

## File convention (every set)

| File | Content | Unity import |
|---|---|---|
| `<Name>_Color.jpg` | sRGB albedo, JPEG q90 | Default, sRGB on, Repeat, mipmaps on; aniso 8 on floors |
| `<Name>_Normal.png` | tangent-space normal, **OpenGL +Y** (Unity's convention, no flip needed) | Texture Type: Normal map |
| `<Name>_MetallicSmoothness.png` | RGBA, R = metallic, G = B = 0, A = smoothness (1 - roughness), 1024 px | sRGB **off**, Alpha Source: Input Texture Alpha. Standard: Metallic slot, Smoothness Source = Metallic Alpha, Smoothness scale 1 |
| `<Name>_AO.jpg` (FloorTile, Carpet) | linear ambient occlusion | sRGB off, Occlusion slot, strength 0.5-1 |
| `GlassSmudge_ColorAlpha.png` | RGBA overlay, sRGB colour, linear alpha | sRGB on, Alpha Is Transparency on |

Tiling: with UVs in metres, set the material tiling to 1 / tile size. For example, FloorTile
at 2.4 m uses tiling 0.4167. All maps tile seamlessly. The wrap edges were checked
numerically against interior pixel and JPEG-block boundaries. Opaque materials need no
alpha. Use BC7 / High Quality compression for the marbles and the wallpaper if banding
shows. Normal Quality is fine elsewhere.

## Materials

| Set | Maps (px) | Real-world tile | What it is | Suggested placement |
|---|---|---|---|---|
| **FloorTile** | Color 2048, Normal 2048, MS 1024, AO 1024 | **2.4 x 2.4 m** = 4 x 4 polished marble tiles of 60 cm, 2.5 mm joints | Warm ivory polished marble (ambientCG Marble021), large-format tiles with grout, 1 mm bevel, slight lippage and per-tile tone; tile smoothness 0.7-1.0 (mean 0.88), joints rough (about 0.3 at 1024 px) | Whole shop floor (16 x 12 m = 6.7 x 5 repeats). Align a joint with the shopfront line. Normal strength 1 |
| **StoneTop** | Color 2048, Normal 1024, MS 1024 | 1.0 x 1.0 m | White Carrara-type marble with grey veins, re-polished (smoothness ~0.85) | Display-case tops, counter top, island tops, window bay sills |
| **StoneTopDark** (extra) | Color 2048, Normal 1024, MS 1024 | 1.0 x 1.0 m | Black marble with grey/white veins, polished (smoothness ~0.89) | Case plinths and kick bases, counter front panel, accent bands. Keep to small areas because the strong veins repeat visibly |
| **Walnut** | Color 2048, Normal 1024, MS 1024 | 1.0 x 1.0 m, grain along U | Dark walnut veneer, satin lacquer (smoothness ~0.66) | Counter body, wall-case cabinetry, side islands, office desk, door leaves. Run the grain horizontally |
| **PlasterWarm** | Color 2048x1024, Normal/MS 1024x512 | **2.0 x 1.0 m** | Smooth painted plaster, warm greige, matte (smoothness ~0.18); hairline cracks of the source removed | All shop and side-room walls. Normal strength 0.5-1 |
| **CeilingPaint** | Color 2048, Normal 1024, MS 1024 | 2.0 x 2.0 m | Procedural matte rolled ceiling paint (stipple, faint roller laps), off-white | Ceilings of shop, office and side rooms. Supersedes NightMaterials/CeilingPaint (512 px) |
| **VelvetCharcoal** | 1024 (all) | 0.5 x 0.5 m | Charcoal velvet with soft crush shading and fine pile; rough (smoothness ~0.18) | Jewellery trays, ring rolls, bust covers, case-floor liners. Raise the albedo slightly if it reads black under the torch |
| **SuedeCream** | 1024 (all) | 0.5 x 0.5 m | Cream microsuede, mottled nap, very matte (smoothness ~0.10) | Display pads, necklace busts, case back panels, seating cushions |
| **BronzeBrushed** | 1024 (all) | 0.5 x 0.5 m, brushing along U | Brushed bronze/brass, metallic 1, smoothness ~0.62 | Case frames and trims, kick plates, door pulls, sign frames, shelf edges |
| **SafeSteel** | 1024 (all) | 0.5 x 0.5 m | Procedural dark graphite satin powder coat on steel (orange peel, faint scratches), metallic 0, smoothness ~0.56 | Bodies and doors of the two open safes in the rear office, office cabinet |
| **BrushedSteel** | 1024 (all) | 0.5 x 0.5 m, brushing along U | Brushed stainless steel, metallic 1, smoothness ~0.53 | Safe door inner plates and bolts, door hardware, forced storefront door frames, office fittings |
| **Chrome** | 1024 (all) | 0.5 x 0.5 m | Mirror chrome with very faint wipe marks, metallic 1, smoothness ~0.96 | Glass clamps and case fittings, chair legs, light fittings. Needs the reflection probe to look right |
| **BrassPolished** (extra) | 1024 (all) | 0.5 x 0.5 m | Polished brass, metallic 1, smoothness ~0.91 | Small hardware, jewellery risers, key escutcheons, handles |
| **Leather** | 1024 (all) | **0.2 x 0.2 m** (pebble grain ~5 mm) | Black pebble-grain leather, semi-matte (smoothness ~0.55) | Seating-corner chairs and sofa, counter stools, desk pad, ring boxes |
| **Carpet** | 1024 (all) + AO | 0.5 x 0.5 m | Dark charcoal-navy cut-pile commercial carpet, smoothness ~0.12 | Rear office and closed side rooms, seating-corner rug |
| **Terrazzo** | Color 2048, Normal 1024, MS 1024 | 1.0 x 1.0 m (chips 1-4 cm) | Polished white terrazzo with black/grey chips, smoothness ~0.83 | Alternative floor for the entrance vestibule or back corridor; island tops |
| **Wallpaper** | Color 2048, Normal 1024, MS 1024 | **0.5 x 0.5 m** (2 x 2 ogee repeats of 25 cm) | Original tone-on-tone ogee print (light champagne ink on taupe ground) on textured paper; ink is smoother than the paper and catches the torch | Feature wall behind the counter, seating corner, wall-case back panels |
| **GlassSmudge** | ColorAlpha 2048, Normal 1024, MS 1024 | **0.6 x 0.6 m** (prints ~2 cm) | Fingerprint clusters, faint cloth-wipe arcs and dust as an alpha overlay (alpha 0-0.6, mostly clean); smoothness drops from 0.98 on clean glass to ~0.72 in the heaviest grease | Display-case glass tops and the storefront glass, as an overlay (see below) |

### GlassSmudge use
The glass in the scene stays a separate transparent material. Put the smudges on a second
material:

1. Use a quad or a duplicate of the glass face 0.5 mm in front of the glass, with Standard,
   Rendering Mode **Transparent**, Albedo = GlassSmudge_ColorAlpha (white tint), Metallic
   map = GlassSmudge_MetallicSmoothness (Smoothness Source = Metallic Alpha), and Normal =
   GlassSmudge_Normal at strength 0.3. Transparent keeps the specular, so the prints show
   when the flashlight grazes the glass.
2. Alternatively, use the alpha as a mask in a custom glass shader to raise roughness
   locally.

For evidence play, the same alpha can drive a UV-torch reveal: show it brighter or
emissive only inside the UV cone. That is a runtime idea and is not implemented.

## Notes
- Night lighting: the albedos are kept in the physically valid range (black leather and
  marble about sRGB 31-33, white marble about 236). Most surfaces will look dark under the
  shop's night lighting. Adjust the lights, not the textures.
- Metals use metallic 1 with coloured albedo as F0 (steel ~192, chrome ~196, bronze
  (203,168,118), brass (236,208,146)). Under the Built-in Standard shader they need a
  reflection probe in the shop. Without one they render dark.
- The pack does not replace ReferenceTextures or NightMaterials. Wiring needs
  StoreKit.Surface (or equivalent) to point at `ThirdParty/PBRInterior/<Name>_*` and set
  tiling from the sizes above.
- Budget: about 38 MB of maps. Possible savings if needed: Normal at 512 px for Velvet,
  Suede, Leather and Carpet (they are the largest PNGs at 1.8-2.8 MB).
