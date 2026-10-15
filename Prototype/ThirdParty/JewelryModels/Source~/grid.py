import sys, os, glob, numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render as R
from PIL import Image, ImageDraw
from concurrent.futures import ProcessPoolExecutor
stage, out = sys.argv[1], sys.argv[2]
files = sorted(glob.glob(os.path.join(stage, '*.obj')))
S = 220
def one(f):
    parts = R.load_parts(f)
    img = Image.fromarray((R.render(parts, S, 0.75, 0.45) * 255).astype(np.uint8))
    d = ImageDraw.Draw(img); d.rectangle([0, S - 14, S, S], fill=(0, 0, 0)); d.text((3, S - 13), os.path.basename(f)[:-4], fill=(255, 230, 120))
    return np.asarray(img)
with ProcessPoolExecutor(4) as ex:
    tiles = list(ex.map(one, files))
cols = 7; rows = (len(tiles) + cols - 1) // cols
sheet = Image.new('RGB', (cols * S, rows * S), (40, 40, 44))
for i, t in enumerate(tiles):
    sheet.paste(Image.fromarray(t), ((i % cols) * S, (i // cols) * S))
sheet.save(out, quality=88, optimize=True)
print(len(tiles), sheet.size)
