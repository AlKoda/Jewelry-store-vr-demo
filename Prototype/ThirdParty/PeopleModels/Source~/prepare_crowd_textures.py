"""Rebuild the three new crowd avatars' maps from pinned Rocketbox TGA sources.

Run from the checkout root with Pillow and numpy installed. Existing Phone and
Observer use the original PeopleModels maps, without modifying them.
"""
from pathlib import Path
import numpy as np
from PIL import Image

cache=Path('.asset-cache/crowd-source/Assets/Avatars/Adults')
out=Path('Prototype/ThirdParty/PeopleModels/Animated');out.mkdir(exist_ok=True)
for role,avatar,prefix in [('Walker','Male_Adult_05','m009'),('Talker','Male_Adult_10','m024'),('Listener','Female_Adult_06','f202')]:
 for source,part in [('body','Body'),('head','Head'),('opacity','Cutout')]:
  im=Image.open(cache/avatar/'Textures'/f'{prefix}_{source}_color.tga').convert('RGBA')
  im.thumbnail((1024,1024),Image.Resampling.LANCZOS)
  pixels=np.array(im);pixels[:,:,:3]=np.minimum(pixels[:,:,:3],238)
  Image.fromarray(pixels).save(out/f'{role}_{part}_Color.png')
 for source,part in [('body','Body'),('head','Head')]:
  im=Image.open(cache/avatar/'Textures'/f'{prefix}_{source}_normal.tga').convert('RGB')
  im.thumbnail((512,512),Image.Resampling.LANCZOS);im.save(out/f'{role}_{part}_Normal.png')
