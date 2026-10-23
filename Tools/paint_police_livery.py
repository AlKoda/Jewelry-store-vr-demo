"""Paint a clean UV livery from the unchanged supplied car triangles.
No source photograph is rendered: its luminance only identifies existing window/trim regions.
All visible colours, surface response and correctly shaped wording are authored here.
Run PoliceLiveryInspectRun first to export root-local triangles without editing the mesh.
"""
from pathlib import Path
import sys,json
sys.path.insert(0,'.asset-cache/audio-python313')
import numpy as np
from PIL import Image,ImageDraw,ImageFont,features
from scipy.ndimage import distance_transform_edt
import arabic_reshaper
from bidi.algorithm import get_display
N=2048
out=Path('Prototype/ThirdParty/InvestigationDetail');out.mkdir(exist_ok=True)
source=np.asarray(Image.open('Prototype/ThirdParty/SuppliedPoliceCar/police+car+3d+model.fbm/police+car+3d+model_basecolor.jpg').convert('RGB').resize((N,N)),dtype=float)/255
tri=np.loadtxt('UnityProject_Demo/Builds/car-triangles.csv',delimiter=',').reshape(-1,3,5)
colour=np.zeros((N,N,3),dtype=np.uint8);response=np.zeros((N,N,3),dtype=np.uint8);coverage=np.zeros((N,N),bool)
font='Prototype/ThirdParty/Fonts/Arabic/NotoSansArabic.ttf'
def text(draw,at,t,size,anchor='mm'):
 f=ImageFont.truetype(font,size)
 if features.check('raqm'):draw.text(at,t,font=f,fill='white',anchor=anchor,direction='rtl',language='ar')
 else:draw.text(at,get_display(arabic_reshaper.reshape(t)),font=f,fill='white',anchor=anchor)
# Native typography. Official emblem has genuine alpha; only the badge portion is used.
side=Image.new('RGBA',(1536,384));d=ImageDraw.Draw(side)
f=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',133)
d.text((300,198),'POLICE',font=f,fill='white',anchor='mm');text(d,(1235,175),'الشرطة',157)
badge=Image.open(out/'OmanPoliceEmblem.png').convert('RGBA').crop((7,5,251,273));badge.thumbnail((250,300),Image.Resampling.LANCZOS);side.alpha_composite(badge,((1536-badge.width)//2,(384-badge.height)//2))
side.save(out/'PoliceDoorMarkings.png')
plate=Image.new('RGBA',(1024,256),(10,33,84,255));d=ImageDraw.Draw(plate);d.rounded_rectangle((8,8,1015,247),radius=10,outline=(230,236,239),width=7);text(d,(512,100),'الشرطة',208);plate.save(out/'PoliceArabicPlate.png')
sa=np.asarray(side);pa=np.asarray(plate)
def sample(a,u,v):
 return a[np.clip((v*(a.shape[0]-1)).astype(int),0,a.shape[0]-1),np.clip((u*(a.shape[1]-1)).astype(int),0,a.shape[1]-1)]
for tr in tri:
 pts=tr[:,:3];uv=tr[:,3:]*[N-1,-(N-1)]+[0,N-1]
 left,top=np.maximum(np.floor(uv.min(0)).astype(int)-1,0);right,bottom=np.minimum(np.ceil(uv.max(0)).astype(int)+1,N-1)
 if right<left or bottom<top:continue
 xx,yy=np.meshgrid(np.arange(left,right+1),np.arange(top,bottom+1));p=np.stack((xx,yy),-1)
 a,b,c=uv;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
 if abs(den)<1e-8:continue
 w0=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den;w1=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;w2=1-w0-w1
 good=(w0>=-.0001)&(w1>=-.0001)&(w2>=-.0001)
 if not good.any():continue
 X=xx[good];Y=yy[good];xyz=w0[good,None]*pts[0]+w1[good,None]*pts[1]+w2[good,None]*pts[2];x,y,z=xyz.T;ax=abs(x)
 normal=np.cross(pts[1]-pts[0],pts[2]-pts[0]);normal/=max(np.linalg.norm(normal),1e-10)
 rgb=np.tile([218,222,220],(len(x),1));rough=np.full(len(x),102);metal=np.full(len(x),0)
 # Navy bonnet and continuous doors; warm-neutral white roof, fenders and bumpers.
 navy=((ax>.72)&(z>-.99)&(z<1.03)&(y>.30)&(y<1.01))|((z<-1.01)&(y>.80)&(abs(normal[1])>.35))
 rgb[navy]=[15,27,49];rough[navy]=112
 lum=source[Y,X].max(1)
 glass=(y>1.045)&(y<1.485)&(z>-1.13)&(z<1.62)&(lum<.80)
 glass &= ~((abs(normal[1])>.88)&(y>1.43))

 rgb[glass]=[19,28,33];rough[glass]=224
 pillar=glass&(ax>.78)&(abs(z-(.15+.15*(y-1.05)))<.03)
 rgb[pillar]=[12,14,16];rough[pillar]=96
 # Keep narrow rubber window seals and lower sill distinct from paint.
 seals=(y>1.0)&(y<1.51)&(z>-1.19)&(z<1.63)&(lum<.16)&~glass
 under=(y<.22)|((z<-2.13)&(y<.48))|((z>2.19)&(y<.35))
 rgb[seals|under]=[22,23,25];rough[seals|under]=43
 # Existing wheel geometry, not drawn-on wheels. Ring radii follow actual axle centres.
 wheelR=np.minimum(np.sqrt((y-.351)**2+(z+1.442)**2),np.sqrt((y-.351)**2+(z-1.425)**2))
 tyre=(ax>.88)&(wheelR<.354)&(y<.71)
 rgb[tyre]=[26,27,28];rough[tyre]=44
 rim=tyre&(wheelR<.237)&(ax>.985)
 rgb[rim]=[123,128,132];rough[rim]=174;metal[rim]=216
 # Front grille and lower intake occupy actual recessed front-facing faces.
 grille=(z<-2.16)&(y>.49)&(y<.85)&(ax<.64)&(lum<.48)
 rgb[grille]=[27,31,35];rough[grille]=121;metal[grille]=150
 chrome=(z<-2.18)&(y>.49)&(y<.85)&(ax<.66)&(lum>=.48)
 rgb[chrome]=[150,159,166];rough[chrome]=192;metal[chrome]=235
 frontlamp=(z<-2.05)&(ax>.64)&(y>.68)&(y<.96)
 rgb[frontlamp]=[177,190,193];rough[frontlamp]=218;metal[frontlamp]=63
 taillamp=(z>2.04)&(ax>.56)&(y>.68)&(y<.96)
 rgb[taillamp]=[37,31,32];rough[taillamp]=202
 rearRed=taillamp&(((y>.81)&(y<.89))|((ax>.88)&(y>.69)&(y<.89)))
 rgb[rearRed]=[102,10,16]
 bar=y>1.57
 rgb[bar]=[17,70,128];rough[bar]=200
 # Mirror housings remain white, reflective black lenses on their outer faces.
 mirror=(ax>1.075)&(y>1.04)&(y<1.31)
 rgb[mirror]=[210,215,214];rough[mirror]=148
 # Project markings onto the supplied surface; no floating quads or extra mesh.
 marking=(ax>.91)&(y>.45)&(y<.81)&(z>-.95)&(z<.47)&navy
 u=.5-z/1.42*np.sign(x)-.24*np.sign(x)
 # Common door centre z=-.24, read correctly from either side.
 u=.5+(z+.24)/1.42*np.sign(x);v=(.81-y)/.36
 ink=sample(sa,u,v);a=ink[:,3]/255*marking
 rgb=np.rint(rgb*(1-a[:,None])+ink[:,:3]*a[:,None]);rough=np.where(a>.1,115,rough)
 # Both plates use exactly the same Arabic word; no original numbers can bleed through.
 frontplate=(z<-2.255)&(ax<.285)&(y>.39)&(y<.535)
 backplate=(z>2.27)&(ax<.30)&(y>.52)&(y<.675)
 pick=frontplate|backplate
 u=.5+x/.57*np.where(frontplate,1,-1);v=np.where(frontplate,(.535-y)/.145,(.675-y)/.155)
 platepix=sample(pa,u,v);rgb[pick]=platepix[pick,:3];rough[pick]=113;metal[pick]=0
 colour[Y,X]=np.uint8(rgb);response[Y,X]=np.stack((metal,rough,np.full(len(x),255)),1);coverage[Y,X]=True
# Pad UV islands into empty texels; stable mip edges without copying source pixels.
_,idx=distance_transform_edt(~coverage,return_indices=True)
for array in (colour,response):array[~coverage]=array[idx[0][~coverage],idx[1][~coverage]]
Image.fromarray(colour).save(out/'PoliceAuthoredPaint.png');Image.fromarray(response).save(out/'PoliceSurfaceResponse.png')
print('Authored clean paint/response, correctly shaped door lettering and number-free plates; triangles:',len(tri))
