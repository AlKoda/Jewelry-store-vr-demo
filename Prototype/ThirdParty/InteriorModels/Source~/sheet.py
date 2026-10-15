import sys, os
from PIL import Image, ImageDraw
out = sys.argv[1]; files = sys.argv[2:]
n = len(files); cols = min(6, n); rows = (n + cols - 1) // cols; s = 260
sheet = Image.new('RGB', (cols * s, rows * (s + 18)), (20, 20, 20)); d = ImageDraw.Draw(sheet)
for i, f in enumerate(files):
    im = Image.open(f).convert('RGB').resize((s, s)); x, y = (i % cols) * s, (i // cols) * (s + 18)
    sheet.paste(im, (x, y)); d.text((x + 4, y + s + 2), os.path.basename(f)[:-4], fill=(230, 230, 230))
sheet.save(out)
