import sys, os, json
from PIL import Image
P = sys.argv[1]
m = json.load(open(os.path.join(P, 'models.json')))
DESC = {}
exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'make_docs.py')).read().split('DESC = ')[1].split('\nfiles = ')[0].join(['DESC = ', '']), globals())
lic = {'PocketWatchVintage': 'CC0 - Poly Haven / Tal Swicegood', 'RingAntiqueTigerEye': 'CC BY 4.0 - Qifan Zhang', 'EarringPearlDrop': 'CC BY 4.0 - Softmind Game Factory',
       'EarringGoldLeaf': 'CC BY 4.0 - DineshThennarasan'}
rows = []
for n, d in m.items():
    s = ' x '.join('%.4f' % v for v in d['size_m'])
    maps = ', '.join(d['materials'])
    if d['color_maps'] or d['extra_maps']: maps += '; maps: ' + ', '.join(d['color_maps'] + d['extra_maps'])
    rows.append('| `%s.obj` | %s | %s | %s | %d | %s | %s | %s | %s |' % (n, d['category'], DESC[n][1], s, d['triangles'], maps, lic.get(n, 'original, project-owned'), d['placement'], d['notes'] or '-'))
tex = []
for f in sorted(os.listdir(P)):
    if f.endswith(('.png', '.jpg')):
        im = Image.open(os.path.join(P, f)); kind = 'normal map (OpenGL, +Y green)' if 'Normal' in f else 'metallic-smoothness (R metal, A smoothness; linear)' if 'Metallic' in f else 'emissive-capable screen print' if 'Screen' in f else 'colour (sRGB)'
        users = sorted(n for n, d in m.items() if f in d['color_maps'] + d['extra_maps'])
        tex.append('| `%s` | %d x %d | %s | %s |' % (f, im.size[0], im.size[1], kind, ', '.join(users)))
tot = sum(os.path.getsize(os.path.join(dp, f)) for dp, _, fs in os.walk(P) for f in fs)
txt = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'readme_template.md')).read()
txt = txt.replace('{{MODELS}}', '\n'.join(rows)).replace('{{TEXTURES}}', '\n'.join(tex)).replace('{{COUNT}}', str(len(m))).replace('{{TRIS}}', '{:,}'.format(sum(d['triangles'] for d in m.values())))
open(os.path.join(P, 'README.md'), 'w').write(txt)
print('README written', len(rows), len(tex))
