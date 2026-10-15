import json, sys, os
from PIL import Image
p = sys.argv[1]; d = json.load(open(p)); base = os.path.dirname(p)
for im in d['images']:
    if im['uri'].endswith('.webp'):
        src = os.path.join(base, im['uri']); dst = src[:-5] + '.png'
        Image.open(src).save(dst); im['uri'] = os.path.basename(dst); im['mimeType'] = 'image/png'
for t in d['textures']:
    e = t.get('extensions', {}).pop('EXT_texture_webp', None)
    if e: t['source'] = e['source']
    if 'extensions' in t and not t['extensions']: del t['extensions']
for k in ('extensionsUsed', 'extensionsRequired'):
    if k in d:
        d[k] = [x for x in d[k] if x != 'EXT_texture_webp']
json.dump(d, open(p, 'w'))
print('fixed', p)
