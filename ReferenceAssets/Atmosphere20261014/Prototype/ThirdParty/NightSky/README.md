# NightSky

Night sky, environment lighting, moon, light cookies and glow sprites for the night jewellery store
(street at night after a burglary; a wet street suggests recent rain, so an overcast city sky fits best).
Fetched or generated October 14, 2026. Nothing here has been imported or seen in Unity yet (unverified).
Licences: LICENSE.txt. Hashes, mirrors and commits: sources.json. Total pack size about 20 MB.

## Recommended night-store setup (suggestion, not wired)

- Skybox: `city_night_sky_overcast_1k.hdr` (generated, no ground clutter) or `zwinger_night_1k.hdr`
  (real overcast city night) through Skybox/Panoramic. Clear-night alternative:
  `city_night_sky_clear_2k.hdr` plus `moon_disc_512.png` on a far billboard.
- Reflections for shop glass, chrome and the wet street: `cobblestone_street_night_2k.hdr` imported as a
  Cube (Environment Reflections Source = Custom), or as the fallback sky of the exterior reflection probe.
  It is a warm sodium/LED-lit European street at night, which matches the scene far better than
  `lightroom_14b.hdr` for the night preset.
- Flashlight: `cookie_flashlight_led_512.png` instead of the 128 px cookie that DemoToolsBuilder generates.
- UV torch: `cookie_uv_torch_512.png`. Street lamps: `cookie_streetlight_512.png`.
  Placed scene lamps: `cookie_scene_floodlight_512.png`.
- Lamp heads and the police light bar: `glow_soft_256.png`, `glow_starburst_512.png`,
  `flare_streak_512x128.png`, `flare_halo_ring_256.png` on camera-facing additive quads.

## Orientation and exposure

Unity Skybox/Panoramic (Latitude Longitude Layout, 360) maps image centre (u = 0.5) to world +X,
u = 0.25 to +Z, u = 0.75 to -Z, top row to zenith. The generated skies use +Z = north: east at the image
centre, the brightest city glow towards south-south-west (u about 0.82). Use the material's Rotation to aim
glow and features; keep the bright lamps of the photographic HDRIs away from where they would contradict
scene lights.

Radiance is relative. The generated skies are scaled to `cobblestone_street_night_2k` (lamp-lit pavement
about 0.13). Measured values (luminance) and a Skybox Exposure that matches the generated skies' zenith:

| File | Size | Zenith | 60 deg | Horizon band | Brightest pixel | Suggested Exposure |
|---|---|---|---|---|---|---|
| cobblestone_street_night_2k.hdr | 2048x1024 | 0.0019 | 0.0021 | 0.18 (lit facades) | 35 000 (lamps) | 1.0-1.5 |
| hansaplatz_1k.hdr | 1024x512 | 0.0015 | 0.0017 | 0.20 (shopfronts) | 12 800 | 1.0-1.8 |
| zwinger_night_1k.hdr | 1024x512 | 0.134 | 0.128 | 0.18 | 54 000 | 0.08-0.12 |
| rooftop_night_1k.hdr | 1024x512 | 0.164 | 0.169 | 0.09 | 27 000 | 0.1-0.2 |
| modern_evening_street_1k.hdr | 1024x512 | 1.36 | 1.29 | 0.06 | 5 300 | 0.01-0.03 (late dusk) |
| city_night_sky_clear_2k.hdr | 2048x1024 | 0.0028 | 0.0036 | 0.028 (light dome) | 1.3 (brightest star) | 1.0 |
| city_night_sky_overcast_1k.hdr | 1024x512 | 0.0126 | 0.0124 | 0.007 | 0.016 | 1.0 |

The generated skies contain no lamps, so as an ambient source they give far less light than the
photographic HDRIs (cosine-weighted upper-hemisphere mean 0.011 against 0.7-2.2); keep the scene's own
lights as the key and set Ambient Intensity or a Gradient ambient by eye.

## HDRIs (Radiance RGBE .hdr, equirectangular, linear)

| File | What it is | Suggested use |
|---|---|---|
| `cobblestone_street_night_2k.hdr` | Poly Haven "Cobblestone Street Night" (Greg Zaal, Jenelle van Heerden): narrow German cobblestone street at night, warm street lamps, parked cars, clear dark sky. Unmodified 2k. Plates are illegible. | Night reflection cubemap; optional skybox (its facades rise to 45-60 deg, so they show above low modelled buildings). |
| `hansaplatz_1k.hdr` | Poly Haven "Hansaplatz" (Greg Zaal), Hamburg city square at night: lit shopfronts, apartment blocks, bare trees, clear black sky. Downsampled from the original 2k. Shop signs are illegible at this size; a small German police car is parked in front of the shops at u about 0.6. | Alternative night reflection/ambient for the shopping street; rotate the police car behind modelled buildings if used as skybox. |
| `zwinger_night_1k.hdr` | Poly Haven "Zwinger Night" (Greg Zaal), Dresden palace courtyard at night under an overcast sky lit orange by the city. Downsampled from 2k. The palace skyline sits at about 12-18 deg altitude (towers up to 23 deg). | Overcast-night skybox behind the buildings across the street; the most "after the rain" real sky in the pack. |
| `rooftop_night_1k.hdr` | Poly Haven "Rooftop Night" (Greg Zaal): rooftop car park at late twilight, purple-pink partly cloudy sky with city glow and the moon. Downsampled from 2k. | Late-dusk skybox variant; the moon is already in the image. |
| `modern_evening_street_1k.hdr` | Poly Haven "Modern Evening Street" (Grzegorz Wronkowski): European office street at dusk, low-contrast partly cloudy sky, warm street lamps. Unmodified 1k. | Late-dusk variant (much brighter: lower Exposure). |
| `city_night_sky_clear_2k.hdr` | Generated: clear light-polluted city sky. Deep blue-grey zenith, warm light dome towards SSW, faint thin cirrus lit from below, 238 real stars from the Yale Bright Star Catalogue placed for mid-October around 23:00 at 51.5 deg N (Polaris due north at 52 deg, Cassiopeia near the zenith, Capella in the north-east), extinction and a city limiting magnitude of about 4.7 applied. Ground half is flat dark grey. | Clear-night skybox; pair with the moon billboard. |
| `city_night_sky_overcast_1k.hdr` | Generated: stratocumulus deck lit from below by sodium/LED street lighting, brightest overhead and fading into haze at the horizon, low contrast like a real urban overcast night. Ground half flat dark grey. | Default night skybox; overcast skies are featureless, so 1k is enough. |

Unity import (Built-in): for a skybox keep Texture Shape 2D, Wrap U Repeat / V Clamp, Generate Mip Maps
off (avoids the seam line of Skybox/Panoramic), Max Size 2048 (1024 for the 1k files), Compression
High Quality (BC6H) or None, then a Skybox/Panoramic material (Mapping Latitude Longitude Layout,
Image Type 360 Degrees). For reflections import a second copy, or switch the shape: Texture Shape Cube,
Mapping Latitude-Longitude Layout (Cylindrical), Convolution Type Specular (Glossy Reflection), Fixup Edge
Seams on, Max Size 512-1024. Unity imports .hdr natively, no package needed.

## Moon

| File | What it is | Use |
|---|---|---|
| `moon.jpg` | NASA SVS CGI Moon Kit colour map `lroc_color_poles_2k.jpg`, 2048x1024 equirectangular, centre = 0 deg longitude (near side). | Albedo for a moon sphere (Unity sphere UVs: rotate so u = 0.5 faces the viewer), or for custom sky shaders. sRGB on, Wrap U Repeat / V Clamp. |
| `moon_disc_512.png` | Full moon near side rendered from the 4k kit map, north up, RGBA with a soft alpha edge; disc diameter 506 px in a 512 px square. | Far billboard in the clear-sky variant: unlit (or Particles/Standard Unlit, Fade/Additive) material with HDR tint 2-4 so it blooms. Real angular diameter 0.52 deg: at 1000 m distance the quad is 9.15 m wide (scale 2-3x for a more cinematic moon). Place it at altitude 25-40 deg, away from the light dome. |

## Light cookies (white on black, linear values, 512 px, grayscale PNG)

Import: Texture Type Cookie, Light Type Spotlight, Alpha Source From Gray Scale, Wrap Clamp, mipmaps on,
no compression or a high-quality single-channel format. The outer 3 px are black so clamping cannot streak.
Values are linear (pixel/255 = light multiplier) and carry a 0.5-LSB dither against banding.
The cookie spans the whole spot cone.

| File | Look | Suggested light |
|---|---|---|
| `cookie_flashlight_led_512.png` | Reflector LED torch: round hotspot with a corona, a faint darker band and reflector-rim ring, soft spill to the cone edge, slight angular unevenness and lens dust. | Flashlight spot: Spot Angle 55-65 deg (the hotspot then spans about 7-10 deg), 5600-6000 K, range 15-25 m. |
| `cookie_uv_torch_512.png` | 365-395 nm forensic torch with an aspheric lens: projected square LED die with soft edges, faint halo and lens-rim ring. | UV torch spot: Spot Angle 25-35 deg, colour deep violet (about 0.35, 0.10, 1.0), low intensity; fluorescence comes from the materials. |
| `cookie_streetlight_512.png` | LED street-lamp distribution seen from above: long batwing throw left-right (along the kerb), short sharp cut-off towards the image top (house fronts), wider side at the image bottom (road). | Street-lamp spot pointing down, rotation (90, yaw, 0): the texture's top edge then faces world forward of the yaw, so choose yaw so the top points at the shop facades. Spot Angle 130-140 deg, lamp head 5-6 m high, 3000-4000 K. |
| `cookie_scene_floodlight_512.png` | Rectangular LED floodlight (about 16:10) with soft edges, slightly brighter centre, very faint LED-array pattern. | Scene lamps placed by the visitor: Spot Angle 70-90 deg, 5000-6500 K. |

## Glow and flare sprites (white on black, sRGB-encoded, grayscale PNG)

Import: Texture Type Default, sRGB on, Alpha Source From Gray Scale (for alpha-blended shaders) or None
(for additive), Wrap Clamp, mipmaps on. Use additive, camera-facing quads; tint in the material.

| File | Use |
|---|---|
| `glow_soft_256.png` | Halo around street-lamp heads, headlights and window spill in mist: 0.6-1.2 m quad at the lamp, warm tint. |
| `glow_starburst_512.png` | Six-spike starburst with fine rays (wet-lens look) for distant street lamps and the torch lens seen head-on: 1.5-3 m quad at far lamps; fade with view angle. |
| `flare_streak_512x128.png` | Anamorphic horizontal streak for the police light bar strobes: about 1.5 x 0.4 m per lamp, tint red / blue, animate alpha with the strobe pattern. |
| `flare_halo_ring_256.png` | Mist halo ring around lamps (rain haze), 2-3 m quad at 10-20% alpha. |
| `lens_dirt_1024.png` | RGB lens dirt (soft dust discs, two or three wipes, specks), mostly black. For a custom bloom or the photo-camera viewfinder; the Built-in pipeline without the Post Processing package has no bloom, so it needs a small custom effect or an overlay quad that brightens with nearby lights. |

## Generators~ (ignored by Unity because of the trailing ~)

Project-owned Python (numpy + Pillow) that reproduces every generated file byte for byte:
`gen_skies.py <bsc5-short.json> <clear.hdr> <overcast.hdr> <preview_dir>`,
`gen_sprites.py <out_dir>`, `make_moon_disc.py <lroc_color_poles_4k.jpg> <out.png> 512`,
`downsample_hdr.py <in_2k.hdr> <out_1k.hdr> <comment>`, and `hdrio.py` (RGBE reader/writer).
They can move to Tools/ if preferred.

## Rejected candidates

- three.js `examples/textures/lensflare/*` (CC BY-NC-SA 3.0, NonCommercial) and `lensdirt.png` (no licence).
- NASA Deep Star Maps 2020 (`starmap_2020_*`): carries separate ESA Gaia DR2 credit terms; replaced by the
  catalogue-based generated sky (in a lit city only about 240 stars are visible anyway).
- Poly Haven `shanghai_bund`, `potsdamer_platz`, `modern_buildings_night`: identifiable skylines/landmarks
  and large illuminated brand signs; `kloppenheim_02`, `moonless_golf`, `satara_night`, `dikhololo_night`:
  rural, not urban.
- @pmndrs/assets `night.exr`/`city.exr` (CC0, but only 512x256).
