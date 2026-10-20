"""Original periodic microtextures, free of baked directional light or shadows."""
from pathlib import Path
import sys
sys.path.insert(0,'.asset-cache/audio-python313')
from PIL import Image
import numpy as np

out=Path('Prototype/ThirdParty/ArabicSceneSigns')
n=512;y,x=np.mgrid[:n,:n]/n
rng=np.random.default_rng(127)
def field():
    h=np.zeros((n,n))
    for i in range(22):
        a,b=rng.integers(20,150,size=2);h+=np.cos(2*np.pi*(a*x+b*y)+rng.uniform(0,6.28))/22
    return h
def normal(h,strength):
    dx=(np.roll(h,-1,1)-np.roll(h,1,1))*strength
    dy=(np.roll(h,-1,0)-np.roll(h,1,0))*strength
    a=np.stack((-dx,-dy,np.ones_like(h)),axis=-1);a/=np.linalg.norm(a,axis=-1,keepdims=True)
    return np.uint8(np.clip(a*.5+.5,0,1)*255)
for name,base,strength in [('FineSuede',198,2.0),('FineLeather',185,2.5)]:
    h=field();color=np.repeat(np.clip(base+h[...,None]*24,0,255).astype(np.uint8),3,axis=2)
    Image.fromarray(color).save(out/(name+'_Color.png'))
    Image.fromarray(normal(h,strength)).save(out/(name+'_Normal.png'))
# A 6 mm repeating stitch dash; represented by a filtered normal/albedo strip.
dash=np.exp(-((x-.5)/.32)**8-((y-.5)/.085)**4)
Image.fromarray(np.repeat(np.uint8(80+dash[...,None]*245),3,axis=2)).save(out/'LeatherStitch_Color.png')
Image.fromarray(normal(dash,.8)).save(out/'LeatherStitch_Normal.png')
print('Created six periodic suede/leather/stitch maps (512 square)')
