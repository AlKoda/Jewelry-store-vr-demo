from pathlib import Path
import sys,json,shutil
sys.path.insert(0,'.asset-cache/audio-python313')
from PIL import Image,ImageDraw,ImageFont,features,ImageFilter
import numpy as np
import arabic_reshaper
from bidi.algorithm import get_display
root=Path('Prototype/ThirdParty/ArabicSceneSigns');root.mkdir(exist_ok=True)
fontpath='Prototype/ThirdParty/Fonts/Arabic/NotoSansArabic.ttf'
raqm=features.check('raqm');print('Arabic raster shaping (RAQM):',raqm)
def drawtext(draw,xy,text,size,fill,anchor='mm'):
 f=ImageFont.truetype(fontpath,max(9,int(size)));kw={'direction':'rtl','language':'ar'} if raqm else {}
 draw.text(xy,text if raqm else get_display(arabic_reshaper.reshape(text)),font=f,fill=fill,anchor=anchor,**kw)
def sign(name,lines,size=(1024,512),green=False,yellow=False):
 w,h=size;bg=(14,83,55) if green else (230,181,45) if yellow else (25,32,34);fg=(245,244,224) if green else (20,23,22) if yellow else (200,172,112)
 im=Image.new('RGB',(w,h),bg);d=ImageDraw.Draw(im);d.rectangle((w*.025,h*.045,w*.975,h*.955),outline=fg,width=max(1,int(h*.008)))
 for i,line in enumerate(lines):
  fontsize=min(w/max(len(line),5)*1.5,h/(len(lines)+.8))
  drawtext(d,(w*.5,h*(i+1)/(len(lines)+1)),line,fontsize,fg)
 im.save(root/(name+'.png'));return im
sign('ShopFascia',['أوريل وفينش','للمجوهرات'],(2048,640))
for name,lines in {'Closed':['مغلق'],'StaffOnly':['للموظفين فقط'],'CollectionRings':['خواتم'],'CollectionNecklaces':['قلائد'],'Exit':['خروج'],'SceneTape':['مسرح الجريمة  •  ممنوع العبور'],'CCTV':['المحل مراقب','بالكاميرات'],'Alarm':['نظام إنذار','ومراقبة أمنية'],'Cards':['نقبل الدفع','بالبطاقات'],'Insured':['المحل مؤمن عليه'],'StoreNotice':['إعلان','المحل مغلق للصيانة'],'MissingCat':['إعلان','يرجى التواصل مع الإدارة'],'NoticeBoard':['لوحة الموظفين','يرجى الالتزام بإجراءات الأمن','أعد الأدوات إلى أماكنها'],'Certificate':['شهادة تدريب','العناية بالمجوهرات','نموذج عرض تعليمي'],'Calendar':['أكتوبر ٢٠٢٦','جدول الموظفين'],'Receipt':['أوريل وفينش','مجوهرات ومشغولات','شكرا لزيارتكم']}.items():sign(name,lines,(1024,384) if name=='SceneTape' else (1024,512),name=='Exit',name=='SceneTape')
# Arabic evacuation diagram; retain actual store outline and the two staff rooms.
im=Image.new('RGB',(1024,724),(229,230,217));d=ImageDraw.Draw(im);drawtext(d,(512,65),'خطة الإخلاء',70,(25,85,50));d.rectangle((170,155,854,560),outline=(30,40,35),width=9);d.line((170,410,854,410),fill=(30,40,35),width=6);d.line((420,410,420,560),fill=(30,40,35),width=6);d.line((604,410,604,560),fill=(30,40,35),width=6)
for x in (280,512,744):d.rectangle((x-38,270,x+38,320),outline=(113,104,80),width=4)
drawtext(d,(512,200),'صالة العرض',42,(30,55,40));drawtext(d,(300,480),'المخزن',30,(30,55,40));drawtext(d,(512,480),'المكتب',30,(30,55,40));drawtext(d,(730,480),'الخدمات',30,(30,55,40));d.line((512,380,512,150),fill=(35,144,68),width=14);d.polygon([(512,130),(490,175),(534,175)],fill=(35,144,68));drawtext(d,(512,630),'اتبع الأسهم إلى المخرج • حافظ على الممرات خالية',32,(25,85,50));im.save(root/'FireEvacuationPlan.png')
# The warning is ink printed onto an 8 cm strip, not a hovering text object.
im=Image.new('RGB',(2048,128),(215,174,49));d=ImageDraw.Draw(im)
for x in range(-100,2200,100):
 for y in [0,117]:d.polygon([(x,y),(x+38,y),(x+57,y+11),(x+19,y+11)],fill=(39,37,24))
drawtext(d,(1024,56),'مسرح الجريمة  •  ممنوع العبور',103,(27,29,24))
im.save(root/'SceneTape.png')
# Reproducible, restrained surface wear. Keep every warning readable.
rng=np.random.default_rng(181026)
for path in sorted(root.glob('*.png')):
 if path.stem.startswith('NightStore'):continue
 im=Image.open(path).convert('RGB');w,h=im.size;a=np.asarray(im).astype(np.float32)
 grain=rng.normal(0,1.2,(h,w,1));a+=grain
 dirt=Image.new('L',(max(8,w//64),max(8,h//64)));dirt.putdata(rng.integers(0,255,dirt.width*dirt.height).tolist());dirt=dirt.resize((w,h),Image.Resampling.BICUBIC).filter(ImageFilter.GaussianBlur(max(2,h*.013)))
 a*=1-(np.asarray(dirt)[:,:,None]/255)*(.075 if path.stem=='SceneTape' else .028)
 im=Image.fromarray(np.uint8(np.clip(a,0,255)));d=ImageDraw.Draw(im,'RGBA')
 if path.stem=='SceneTape':
  # Longitudinal abrasion and irregular creases from being unrolled/reused.
  for _ in range(115):
   x=int(rng.integers(0,w));y=int(rng.choice([rng.integers(0,15),rng.integers(h-15,h)]));length=int(rng.integers(3,70));d.line((x,y,x+length,y+int(rng.integers(-2,3))),fill=(110,95,51,int(rng.integers(16,65))),width=int(rng.integers(1,3)))
  for x in [193,640,1170,1695]:
   d.line((x,0,x+18,h),fill=(66,61,39,28),width=3);d.line((x+4,0,x+22,h),fill=(255,240,176,40),width=2)
  for _ in range(380):
   x=int(rng.integers(w));y=int(rng.integers(h));d.line((x,y,x+int(rng.integers(1,8)),y),fill=(213,177,71,55),width=1)
 else:
  for _ in range(24):
   x=int(rng.integers(w));y=int(rng.choice([rng.integers(0,max(2,h//20)),rng.integers(h-max(2,h//20),h)]));d.line((x,y,x+int(rng.integers(2,25)),y+1),fill=(135,133,119,25),width=1)
 im.save(path)
# Subtle substrate relief is shared by the enamel/printed signs.
height=rng.normal(0,.003,(256,256));dy,dx=np.gradient(height);normal=np.stack((-dx,-dy,np.ones_like(dx)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
Image.fromarray(np.uint8((normal*.5+.5)*255)).save(root/'SignWear_Normal.png')
# Preserve road symbols and UV rectangles, replace the fictional English labels.
im=Image.open('Prototype/ThirdParty/StreetModels/Proc_Signs_Color.jpg').convert('RGB');d=ImageDraw.Draw(im)
d.rectangle((32,548,993,740),fill=(26,35,68));drawtext(d,(512,610),'شارع الصقر',82,(232,231,218));drawtext(d,(512,701),'البلدة القديمة • الحي الرابع',34,(184,188,192))
d.rectangle((770,770,1023,812),fill=(14,96,77));drawtext(d,(897,787),'الحافلات ١٢ • ٣١',25,(223,234,216));im.save(root/'StreetSymbols.png')
im=Image.open('Prototype/ThirdParty/StreetModels/Proc_Lightbox_Color.jpg').convert('RGB');d=ImageDraw.Draw(im)
for y in range(740,1024):
 t=y/1024;d.line((0,y,511,y),fill=(int(29+30*t),int(40+50*t),int(67+95*t)))
drawtext(d,(256,794),'مدينة',73,(241,239,216));drawtext(d,(256,882),'الأنوار',91,(244,211,127));drawtext(d,(256,979),'مهرجان الشتاء • البلدة القديمة',24,(226,228,210));im.save(root/'StreetLightbox.png')
# Preserve atlas UVs and the original paintings; replace only text-bearing cells.
lookup={'FireEvacuationPlan':'FireEvacuationPlan','Lightbox_AurelFinchWide':'ShopFascia','NoticeBoard_StaffCork':'NoticeBoard','Certificate_GemmologyDiplomaFramed':'Certificate','Calendar_October2026':'Calendar','Sign_CCTV':'CCTV','Sticker_AlarmMonitored':'Alarm','Sticker_CardsAccepted':'Cards','Sticker_InsurerApproved':'Insured','StreetPosters_Weathered':'StoreNotice','Graffiti_Tags':'StoreNotice','Flyer_LostCat':'MissingCat'}
for native in ['Sticker_CardsAccepted','StreetPosters_Weathered','Graffiti_Tags']:lookup.pop(native,None)
for name in ['NightStoreInterior','NightStoreStreet']:
 folder=Path('Prototype/ThirdParty/WallGraphics/Atlas');layout=json.loads((folder/(name+'.json')).read_text());im=Image.open(folder/(name+'_Color.png')).convert('RGBA')
 for cell in layout['cells']:
  target=lookup.get(cell['name'])
  if not target:continue
  patch=Image.open(root/(target+'.png')).convert('RGBA').resize((cell['w'],cell['h']),Image.Resampling.LANCZOS);im.paste(patch,(cell['x'],layout['height']-cell['y']-cell['h']))
 im.save(root/(name+'_Color.png'))
(root/'LICENSE.txt').write_text('Project-authored Arabic sign layouts and text. Noto Sans Arabic font: SIL Open Font License 1.1; see ../Fonts/Arabic/OFL.txt.\nAtlas derivatives preserve the original artwork cells from WallGraphics; original attribution remains in WallGraphics/LICENSE.txt and the in-app credits. Text-bearing cells replaced with project-authored Arabic graphics.\n',encoding='utf-8')
(root/'README.md').write_text('# Permanent Arabic scene signage\n\nGenerated with Pillow and Noto Sans Arabic (SIL OFL 1.1), using RAQM shaping when available. Original Latin sign assets remain preserved. Map signage is independent of the UI language switch. Brand transliteration: أوريل وفينش للمجوهرات (Aurel & Finch). Atlas UV rectangles remain unchanged; painting cells are retained.\n',encoding='utf-8')
