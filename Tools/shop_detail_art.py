"""Project-authored Arabic print designs and kraft substrate; no external artwork."""
from pathlib import Path
import sys, math
sys.path.insert(0, '.asset-cache/audio-python313')
from PIL import Image, ImageDraw, ImageFont, features
import numpy as np
import arabic_reshaper
from bidi.algorithm import get_display

folder=Path('Prototype/ThirdParty/ArabicSceneSigns')
font='Prototype/ThirdParty/Fonts/Arabic/NotoSansArabic.ttf'
raqm=features.check('raqm')
def text(draw, xy, value, size, color, maxwidth=1050):
    value=value if raqm else get_display(arabic_reshaper.reshape(value))
    kw={'direction':'rtl','language':'ar'} if raqm else {}
    while True:
        f=ImageFont.truetype(font,size)
        if draw.textbbox((0,0),value,font=f,**kw)[2]<=maxwidth or size<=16:break
        size-=1
    draw.text(xy,value,font=f,fill=color,anchor='mm',**kw)

def panel(name,title,sub,icon):
    w,h=1200,780
    rng=np.random.default_rng(108+icon)
    a=np.zeros((h,w,3),dtype=np.int16)+[19,34,30]
    a+=rng.normal(0,.65,(h,w,1)).astype(np.int16)
    im=Image.fromarray(np.uint8(np.clip(a,0,255)));d=ImageDraw.Draw(im)
    gold=(173,147,94);light=(216,198,152);dim=(129,126,106)
    d.rectangle((25,25,w-26,h-26),outline=gold,width=3)
    d.rectangle((38,38,w-39,h-39),outline=(70,84,69),width=1)
    # Sparse engraved motifs, different for each actual collection.
    if icon==0:
        d.ellipse((495,118,650,273),outline=gold,width=5)
        d.ellipse((555,145,710,300),outline=light,width=4)
        d.polygon([(550,102),(579,102),(594,120),(564,151),(534,120)],outline=light,width=3)
    elif icon==1:
        for x in [535,665]:
            d.line((x,115,x,190),fill=gold,width=4)
            d.ellipse((x-27,185,x+27,265),outline=light,width=4)
            d.ellipse((x-7,214,x+7,232),fill=gold)
    elif icon==2:
        d.polygon([(600,108),(700,195),(600,295),(500,195)],outline=gold,width=4)
        d.line([(500,195),(700,195),(600,108),(600,295),(500,195)],fill=gold,width=2)
        d.line((525,130,675,280),fill=dim,width=2)
    elif icon==3:
        d.arc((450,50,750,280),0,180,fill=gold,width=5)
        d.polygon([(600,272),(620,300),(600,328),(580,300)],outline=light,width=4)
    else:
        d.rounded_rectangle((566,98,634,320),10,outline=gold,width=4)
        d.ellipse((525,145,675,295),fill=(19,34,30),outline=light,width=4)
        d.line((600,168,600,220,630,235),fill=light,width=4)
        for i in range(12):
            a=i*math.pi/6;d.ellipse((600+60*math.sin(a)-2,220-60*math.cos(a)-2,600+60*math.sin(a)+2,220-60*math.cos(a)+2),fill=gold)
    text(d,(600,430),title,90,light)
    d.line((470,523,730,523),fill=gold,width=2)
    text(d,(600,597),sub,38,dim)
    im.save(folder/(name+'.png'))

for args in [
    ('CollectionRings_Print','خواتم','تفاصيل تدوم',0),
    ('CollectionEarrings_Print','أقراط','أناقة في كل تفصيل',1),
    ('Craftsmanship_Print','صياغة وتصميم','حرفة تتوارثها الأجيال',2),
    ('CollectionNecklaces_Print','قلائد','قطع تحكي حكايتك',3),
    ('CollectionWatches_Print','ساعات وأساور','اختيارات بعناية',4),
]:panel(*args)

for name,word in [('Open_Print','مفتوح'),('Closed_Print','مغلق')]:
    im=Image.new('RGB',(1000,500),(25,38,32));d=ImageDraw.Draw(im)
    d.rounded_rectangle((18,18,981,481),14,outline=(180,156,108),width=4)
    text(d,(500,235),word,165,(223,209,174),900)
    im.save(folder/(name+'.png'))

rng=np.random.default_rng(91);h=w=512
noise=rng.normal(0,2.0,(h,w,1))+rng.normal(0,.7,(h,1,1))
paper=np.clip(np.array([165,137,100])+noise,0,255).astype(np.uint8)
Image.fromarray(paper).save(folder/'KraftFiber_Color.png')
(folder/'SHOP_PRINTS.md').write_text('''# Shop print refresh

Original vector-like layouts rasterized by Tools/shop_detail_art.py using Noto Sans Arabic, with RAQM or explicit Arabic reshaping. Same permanent Arabic language as the shop, independent of UI selection. Five distinct collection/craft motifs replace repeated plaques. Two readable door-card faces say مغلق (closed) outside and مفتوح (open) inside. Kraft substrate is seeded procedural fiber noise, without baked highlights. Existing pack licence and font OFL apply. No downloaded artwork.
''',encoding='utf-8')
print('Created five collection prints, two door faces and one kraft texture')
