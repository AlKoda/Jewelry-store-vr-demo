# Prepared material maps

Staged assets only; no runtime or scene change. Preserve `ReferenceTextures`.

| Set | Size | Intended repeat | Content |
|---|---|---|---|
| CeilingPaint | 512 x 512 | Start at 2 m, artistic scale | CC0 painted plaster; Color, Normal, Roughness, MetallicSmoothness |
| FacadeStone | 1024 x 1024 | Start at 2 m, artistic scale | CC0 dark marble; same four maps |
| Ten existing ReferenceTextures names | 16 x 16 | Constant | Roughness and MetallicSmoothness only; match existing scalar finish |

Import Color as sRGB, repeat, mipmaps enabled. Import Normal as Normal map (+Y),
repeat, mipmaps enabled; start normal strength 0.25 on ceiling, 0.4 on facade.
Roughness and MetallicSmoothness are linear data: disable sRGB.
For Built-in Standard metallic workflow use MetallicSmoothness in the metallic
slot, smoothness source Metallic Alpha, smoothness multiplier 1. The standalone
Roughness is a source/interchange map: do not put it directly in Standard's metallic
slot. For constant maps, the existing material scalar is cheaper and equivalent.

Packed channels: R = metallic, G/B = 0, A = 1 - roughness. No AO is packed here.
Normal maps are OpenGL +Y, not the archive's alternative DirectX -Y files.
Use opaque materials and no displacement/tessellation. Start with platform
compression and inspect fine normal noise and signs in the headset before finalizing.

Current StoreKit.Surface only loads Color/Normal from ReferenceTextures and has
no roughness handling. KitCladding currently passes no texture name. Integration
must explicitly point the two surfaces at this pack and assign the packed maps;
copying this folder alone does not change the generated night scene. A copied
CeilingPaint Color/Normal in ReferenceTextures would auto-load, but preserve source
and licensing if deliberately choosing that route. Keep scene integration separate
from the historical projects and validate after regenerating.
