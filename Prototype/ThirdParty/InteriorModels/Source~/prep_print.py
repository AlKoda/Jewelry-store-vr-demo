"""Print for PictureFrameWhite (InteriorModels): crop a public-domain painting scan to the frame's picture
aspect (0.604 x 0.354 m = 1.706:1) and save it as the colour-override source for convert_ph.py.
prep_print.py SRC_JPG OUT_PNG
Source used: Paul Cezanne (1839-1906), "The Banks of the Marne (Villa on the Bank of a River)", c. 1888,
State Hermitage Museum; scan from github.com/pmilovanov/hermitage-art @ b8ead49c (CC0 1.0, public domain).
Crop: 40 px trimmed left and right (canvas edge), full remaining width, rows 300 .. 300 + width / 1.706 (keeps the
villa, the tree line and the upper part of the reflection); resized to 2048 px wide, light unsharp mask."""
import sys
from PIL import Image, ImageFilter
Image.MAX_IMAGE_PIXELS = None
src, out = sys.argv[1:3]
im = Image.open(src).convert('RGB')
W, H = im.size
x0, x1 = 40, W - 40; h = int(round((x1 - x0) / (0.6041 / 0.3540))); y0 = 300
assert y0 + h <= H - 30, (y0, h, H)
im = im.crop((x0, y0, x1, y0 + h)).resize((2048, int(round(2048 * h / (x1 - x0)))), Image.LANCZOS)
im = im.filter(ImageFilter.UnsharpMask(radius=1.2, percent=40, threshold=2))
im.save(out)
print(out, im.size, 'crop', (x0, y0, x1, y0 + h))
