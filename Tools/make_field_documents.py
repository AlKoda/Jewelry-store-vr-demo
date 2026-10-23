"""Original Arabic field-kit paper graphics; no downloaded/generated lettering."""
from pathlib import Path
import sys,json,hashlib
sys.path.insert(0,'.asset-cache/audio-python313')
from PIL import Image,ImageDraw,ImageFont,features
import numpy as np
import arabic_reshaper
from bidi.algorithm import get_display
out=Path('Prototype/ThirdParty/InvestigationDetail/FieldDocuments');out.mkdir(exist_ok=True)
font='Prototype/ThirdParty/Fonts/Arabic/NotoSansArabic.ttf';rng=np.random.default_rng(231026)
def text(d,x,y,value,size=35,fill=(45,44,38)):
 kw={'direction':'rtl','language':'ar'} if features.check('raqm') else {}
 if not kw:value=get_display(arabic_reshaper.reshape(value))
 d.text((x,y),value,font=ImageFont.truetype(font,size),fill=fill,anchor='mm',**kw)
def paper(name,lines):
 w,h=768,1024;n=rng.normal(0,1.2,(h,w,1));base=np.array([224,217,197])[None,None,:];im=Image.fromarray(np.clip(base+n,0,255).astype('uint8'));d=ImageDraw.Draw(im)
 for yy in range(120,920,90):d.line((65,yy,703,yy),fill=(192,187,171),width=1)
 d.rectangle((30,30,w-30,h-30),outline=(159,154,137),width=2)
 for i,line in enumerate(lines):text(d,w/2,95+i*105,line,34 if i else 43)
 # Gentle folds, not black dirt or fake printed highlights.
 d.line((w//2,10,w//2,h-10),fill=(207,201,183),width=2)
 im.save(out/(name+'.png'))
paper('SalesReceipt',['أوريل وفينش للمجوهرات','إيصال خدمة • نموذج عرض','صيانة قفل سوار','الكمية: ١','تسليم القطعة بعد الفحص','يرجى الاحتفاظ بالإيصال','نسخة المتجر'])
paper('StockTag',['بطاقة مخزون','قفل سوار','الحالة: قيد الفحص','أوريل وفينش'])
paper('StaffNote',['ملاحظة الموظفين','أعد علب العرض إلى الخزانة','اترك قطع الصيانة منفصلة','تأكد من إغلاق صندوق الأوراق','قبل نهاية العمل'])
paper('FieldGuide',['حقيبة فحص الموقع','صوّر الموقع قبل الجمع','الملقط: القطع الصغيرة','المسحة: آثار السطح','الفرشاة: إظهار أثر المقبض','بطاقة الرفع: جمع الأثر','احمل الحقيبة من المقبض','ضعها على سطح مستو'])
paper('RegisterPaper',[])
paper('RepairIntake',['قسم الصيانة','قطع مفصولة للفحص','احفظ الأجزاء داخل العلبة','أغلق الغطاء بعد العمل','أوريل وفينش للمجوهرات'])
im=Image.new('RGB',(1024,160),(218,215,198));d=ImageDraw.Draw(im)
d.rectangle((5,5,1018,154),outline=(95,99,90),width=4)
text(d,512,60,'حقيبة فحص الموقع',58)
text(d,512,120,'توثيق الآثار • جمع العينات',31)
im.save(out/'CaseLabel.png')
files=[{'file':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size} for p in sorted(out.glob('*.png'))]
(out/'sources.json').write_text(json.dumps({'created':'2026-10-23','author':'Jewelry store project','license':'Project-owned original artwork; Noto Sans Arabic font under parent Fonts licence','method':'Pillow, fixed-seed paper grain, correctly shaped Arabic type; fictional shop documents, no generated image lettering','reproduce':'Tools/make_field_documents.py','files':files},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
