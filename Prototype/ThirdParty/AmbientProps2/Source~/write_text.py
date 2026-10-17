import json, sys
P = '/home/user/Jewelry-store-vr-demo/Prototype/ThirdParty/AmbientProps2'
R = '/home/user/Jewelry-store-vr-demo/Docs/ASSET_REGISTER.md'
j = json.load(open(P + '/sources.json')); M = j['models']
by = lambda lic: [(k, v) for k, v in M.items() if v['license'] == lic]
L = ['AmbientProps2 - second batch of lived-in props for the night jewellery store, office and street',
     'Prepared 2026-10-17. Not yet imported or checked in Unity (unverified).', '',
     'Per-file source URL, mirror commit and SHA-256 are in sources.json. Two licences:',
     '  1. CC0 1.0 (Poly Haven, Kenney, Quaternius, Isa Lousberg): no attribution required; credits below are a courtesy.',
     '  2. CC BY 3.0 (Poly Pizza uploads by PuKkBuMXDD and Poly by Google): ATTRIBUTION IS REQUIRED wherever the demo is shared.', '',
     'CC0 1.0:   https://creativecommons.org/publicdomain/zero/1.0/', 'CC BY 3.0: https://creativecommons.org/licenses/by/3.0/', '']
for k, v in j['license_evidence'].items(): L += ['Licence evidence, %s: %s' % (k, v), '']
L += ['CC BY 3.0 attribution lines (copy into the demo credits):']
for k, v in by('CC-BY-3.0'):
    L.append('  "%s" by %s (%s), licensed under CC BY 3.0 (https://creativecommons.org/licenses/by/3.0/). Modified: converted to OBJ, scaled, grounded. (%s.*)' % (v['title'], v['authors'], v['page'], k))
L += ['', 'CC0 credits (courtesy):']
for k, v in by('CC0-1.0'):
    L.append('  "%s" by %s, %s (%s.*)' % (v['title'], v['authors'], v['page'], k))
open(P + '/LICENSE.txt', 'w').write('\n'.join(L) + '\n')
tot = j['total_triangles']; mb = j['pack_bytes'] / 1e6
rows = []
for k, v in M.items():
    s = ' x '.join('%.2f' % x for x in v['size_m'])
    maps = 'Color+Normal+MS' if any('Normal' in m for m in v['maps']) else ('Color' if v['maps'] else 'flat MTL colours')
    rows.append('| **%s** | %s | %s | %s | [%s](%s), %s | %s | %s |' % (k, format(v['tris'], ','), s, maps, v['title'], v['page'], v['authors'], v['license'].replace('-1.0', ' 1.0').replace('-3.0', ' 3.0'), v['use']))
md = ['# AmbientProps2: second batch of lived-in props', '',
 'Fills the gaps left by AmbientProps (mugs, cups, cans, bottles, takeaway boxes, rugs and door mats, coat rack, shop shelving, cash register, ceiling ducts, street planters, street trees with leaves, parked civilian cars, a scooter). Made for Unity 6000.6.2f1, Built-in pipeline, no packages (OBJ/MTL + JPG/PNG). Prepared 2026-10-17. **Not imported or checked in Unity yet (unverified); nothing is wired.**', '',
 '- %d models: 24 Poly Haven (CC0, PBR maps), 23 Kenney (CC0, stylised colour atlas or flat colours), 9 Poly Pizza (6 CC0 Quaternius / Isa Lousberg, 3 **CC BY 3.0, attribution required**, see LICENSE.txt).' % len(M),
 '- Total %s triangles, %.1f MB. Textures at most 1024 px.' % (format(tot, ','), mb), '',
 '## Conventions', '', '| Item | Convention |', '|---|---|',
 '| Units | Metres, Y up, Scale Factor 1, lowest point y = 0, centred in x/z. FluorescentBatten uses a ceiling pivot (top at y = 0). Front/long axis is whatever the source used (vans and scooter: long axis X). |',
 '| PBR maps (Poly Haven) | Converted with InteriorModels/Source~/convert_ph.py (via Source~/build_ph2.py), not Tools/glb_to_obj.py, so normal and roughness are kept: `<Name>[_<Mat>]_Color.jpg` (map_Kd), `_Normal.jpg` (tangent space, OpenGL +Y), `_MetallicSmoothness.png` (R = metallic, A = smoothness). The MTL names the extra maps in `# Normal <file>` / `# MetallicSmoothness <file>` comment lines, as in InteriorModels and StreetModels, so the same importer code applies; Unity OBJ import ignores them, so they must be assigned by the asset library (normal map texture type must be set to Normal map). Glass materials (CashRegisterVintage/Glass) carry `d` < 1. |',
 '| Kenney / Poly Pizza | Tools/glb_to_obj.py --ground --scale (scale per model in sources.json). Kenney kits share a 512 px colour atlas, copied per model as `<Name>_colormap.png`; the furniture kit, SUV, van and shopping cart use constant MTL colours. Stylised low-poly look: best as distant or small clutter. |',
 '| Sets | AirDuctKit (16 duct pieces), TeaSetMugs (tray, pot and cups), SuitcaseVintage (two cases) are several pieces in one OBJ; split by connected parts. |',
 '| Decimation | Poly Haven models above about 5k were reduced with seam-preserving decimation; Broom (9.7k, bristles), ToolCart (8k), AirDuctKit (10k, whole kit), WornMetalRack (6.4k), FluorescentBatten (6.5k) and ParkedPickup (6.4k) stay above 5k. |', '',
 '## Models', '', '| Model | Tris | Size m (x × y × z) | Maps | Source / author | Licence | Suggested use |', '|---|---|---|---|---|---|---|'] + rows + ['',
 '## Not found (gaps)', '',
 '- Router, network switch, UPS, power strip, desktop PC tower, office printer: no CC0/CC BY model with verifiable licence found in reachable mirrors (LaptopClassic is the stand-in; NetworkServerRackRow in AmbientProps covers the rack).',
 '- Newspapers, ceiling-mounted rectangular duct (only the circular kit), street signage brackets, hand umbrella (only a CC BY beach parasol exists), bicycle (a "City Pack" Bicycle.glb exists in ssebs/DankNooner without per-item licence evidence, not taken), water cooler and card terminal (already in InteriorModels/WaterCooler and JewelryModels/PaymentTerminal).',
 '- Photoreal street trees within 5k tris: Poly Haven trees are 30k to 1.6M tris; only the stylised Quaternius trees fit.', '',
 '## Rebuild', '', 'Source~/build_ph2.py (Poly Haven, needs trimesh, numpy, pillow, scipy, fast_simplification), Source~/unglb.py (GLB to glTF + bin + images), Source~/objstat.py (stats), Source~/write_docs.py and write_text.py (sources.json, LICENSE.txt, README.md, register rows). Paths in the scripts point at the scratch clone used on 2026-10-17.', '',
 'Total size: %.1f MB (%d files in the pack root).' % (mb, 0)]
import os
n = len([f for f in os.listdir(P) if os.path.isfile(os.path.join(P, f))])
md[-1] = 'Total size: %.1f MB (%d files in the pack root, Source~ excluded from Unity import by the trailing ~).' % (mb, n)
open(P + '/README.md', 'w').write('\n'.join(md) + '\n')
reg = []
for k, v in M.items():
    att = '**Required**: line in Prototype/ThirdParty/AmbientProps2/LICENSE.txt' if v['license'] == 'CC-BY-3.0' else 'None required (courtesy in LICENSE.txt)'
    reg.append('| %s (%s) | %s (via %s) | %s (%s) | mirror @ %s; fetched 2026-10-17 | %s | %s | Yes | Prototype/ThirdParty/AmbientProps2/%s.* | Fetched October 17, %s tris, not yet wired; unverified in Unity |' % (
        k, v['title'], v['page'], v['mirror'], v['authors'], v['provider'], v['mirror_commit'][:10], v['license'].replace('-1.0', ' 1.0').replace('-3.0', ' 3.0').replace('CC-BY', 'CC BY'), att, k, format(v['tris'], ',')))
s = open(R).read()
if 'AmbientProps2/' not in s:
    open(R, 'a').write(('' if s.endswith('\n') else '\n') + '\n'.join(reg) + '\n')
print(len(reg), n, mb)
