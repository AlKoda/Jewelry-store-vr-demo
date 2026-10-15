"""grid.py out.png cols rows... : each row arg is comma-separated image paths; labels drawn from file names"""
import sys
from PIL import Image, ImageDraw
out = sys.argv[1]; rows = [r.split(',') for r in sys.argv[2:]]
ims = [[Image.open(p).convert('RGB') for p in r] for r in rows]
w, h = ims[0][0].size; nc = max(len(r) for r in ims)
sheet = Image.new('RGB', (w * nc, h * len(ims)), (30, 30, 30)); d = ImageDraw.Draw(sheet)
for j, r in enumerate(ims):
    for i, im in enumerate(r):
        sheet.paste(im.resize((w, h)), (i * w, j * h)); d.text((i * w + 6, j * h + 6), rows[j][i].split('/')[-1], fill=(255, 255, 0))
sheet.save(out)
