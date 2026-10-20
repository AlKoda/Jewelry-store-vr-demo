"""Draw a layout reference for image_gen from the existing store fixture coordinates.

This is an authoring reference, not the delivered sign texture. The final artwork is
generated with image_gen, preserving these walls, fixture footprints and route.
"""
from pathlib import Path
import math
import re
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / '.asset-cache/audio-python313'))
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
output = root / 'Docs/References/EvacuationLayoutReference.png'
image = Image.new('RGB', (1800, 1272), '#f6f4eb')
draw = ImageDraw.Draw(image)
font = 'C:/Windows/Fonts/arial.ttf'
def text(xy, value, size=27, fill='#25352d'):
    draw.text(xy, value, font=ImageFont.truetype(font,size), fill=fill, anchor='mm')
def point(x,z): return (120+(x+8)*75,220+z*75)
def line(a,b,fill='#303b35',width=10): draw.line([point(*a),point(*b)],fill=fill,width=width)
draw.rectangle((20,20,1780,1252),outline='#1f654a',width=6)
draw.rectangle((40,40,1760,158),fill='#1f654a')
text((900,95),'EVACUATION PLAN — ARABIC ARTWORK REFERENCE',42,'white')
text((720,185),'FRONT / STREET',25)
for a,b in [((-8,0),(-1,0)),((1,0),(8,0)),((-8,0),(-8,12)),((8,0),(8,12)),((-8,12),(8,12))]:line(a,b)
for a,b in [((-8,8.5),(-4.35,8.5)),((-3.45,8.5),(-.8,8.5)),((.8,8.5),(3.45,8.5)),((4.35,8.5),(8,8.5)),((-3,8.5),(-3,12)),((3,8.5),(3,12))]:line(a,b)
draw.rectangle((point(-.95,-.22),point(.95,.16)),fill='#24834d')
text(point(0,-.02),'EXIT',25,'white')
layout=(root/'Prototype/Editor/NightStore/NightStoreLayout.cs').read_text(encoding='utf-8')
pattern=r'new Fixture\("Showroom", "([^"]+)", "(display_case|wall_case|counter)", (-?[\d.]+)f?, 0, (-?[\d.]+)f?, (-?[\d.]+)\)'
for name,kind,x,z,yaw in re.findall(pattern,layout):
    x,z,yaw=float(x),float(z),float(yaw)
    w,d=(1.8,.65) if kind=='display_case' else (1.8,.38) if kind=='wall_case' else (2.4,.65)
    a=math.radians(yaw)
    corners=[]
    for dx,dz in [(-w/2,-d/2),(w/2,-d/2),(w/2,d/2),(-w/2,d/2)]:
        corners.append(point(x+dx*math.cos(a)+dz*math.sin(a),z-dx*math.sin(a)+dz*math.cos(a)))
    draw.polygon(corners,fill='#d4d7d1',outline='#8f9891',width=3)
text(point(-.9,2.3),'SHOWROOM',33)
text(point(-5.5,10.25),'STOCK',27)
text(point(0,10.25),'OFFICE',27)
text(point(5.5,10.25),'STAFF',27)
route=[(1.6,8.1),(1.6,7.4),(3.25,7.4),(3.25,2.0),(0,2.0),(0,.26)]
draw.line([point(*p) for p in route],fill='#27824d',width=14,joint='curve')
tip=point(0,.26);draw.polygon([tip,(tip[0]-20,tip[1]+33),(tip[0]+20,tip[1]+33)],fill='#27824d')
here=point(1.6,8.1);draw.ellipse((here[0]-14,here[1]-14,here[0]+14,here[1]+14),fill='#c4312c',outline='white',width=3)
text((1545,360),'LEGEND',30)
for y,label in [(450,'GREEN: EXIT ROUTE'),(550,'RED: YOU ARE HERE'),(650,'GREY: DISPLAY CASES'),(750,'TOP: MAIN EXIT')]:text((1545,y),label,22)
text((900,1190),'Keep aisles clear — diagram is for the fictional training scene',27)
output.parent.mkdir(parents=True,exist_ok=True)
image.save(output)
print(output)
