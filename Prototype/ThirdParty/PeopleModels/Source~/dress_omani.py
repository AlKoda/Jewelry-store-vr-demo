"""Original Omani garments fitted to the existing MIT Rocketbox crowd rigs.
Run Blender background from repository root. Original animation and exposed skin
are retained; covered western clothing is removed, never layered under the robe.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path.cwd(); OUT=ROOT/'Prototype/ThirdParty/PeopleModels/OmaniCrowd'
OUT.mkdir(parents=True,exist_ok=True)

def build(role):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(ROOT/f'Prototype/ThirdParty/PeopleModels/Animated/{role}.fbx'))
 arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');arm.data.pose_position='REST';bpy.context.view_layer.update()
 def bone(n):return arm.matrix_world@arm.data.bones['Bip01 '+n].head_local
 # Keep original skin, neck and footwear, removing all covered clothing and hair cards.
 for ob in list(bpy.data.objects):
  if ob.type!='MESH':continue
  import bmesh
  bm=bmesh.new();bm.from_mesh(ob.data)
  remove=[]
  for f in bm.faces:
   pts=[ob.matrix_world@v.co for v in f.verts];c=sum(pts,Vector())/len(pts)
   mat=ob.data.materials[f.material_index].name.lower()
   keep=((c.z>bone('Neck').z-.025 and 'head' in mat) or c.z<.115 or abs(c.x)>abs(bone('L Hand').x)-.025)
   if not keep or 'opacity' in mat:remove.append(f)
  bmesh.ops.delete(bm,geom=remove,context='FACES');bm.to_mesh(ob.data);bm.free()
 cloth=bpy.data.materials.new('OmaniIvory' if role=='Talker' else 'OmaniSand');cloth.diffuse_color=(.72,.70,.64,1) if role=='Talker' else (.43,.38,.29,1)
 trim=bpy.data.materials.new('OmaniEmbroidery');trim.diffuse_color=(.22,.18,.12,1)
 hat=bpy.data.materials.new('OmaniKumma' if role=='Talker' else 'OmaniMussar');hat.diffuse_color=(.7,.64,.5,1)
 verts=[];faces=[];weights=[];mats=[];uv=[]
 def mesh(name,material):
  nonlocal verts,faces,weights,mats,uv
  data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.materials.append(material);data.update()
  ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob)
  for poly in data.polygons:poly.use_smooth=True
  layer=data.uv_layers.new(name='UVMap')
  for poly in data.polygons:
   for li in poly.loop_indices:layer.data[li].uv=uv[data.loops[li].vertex_index]
  for i,w in enumerate(weights):
   for key,value in w.items():
    group=ob.vertex_groups.get('Bip01 '+key) or ob.vertex_groups.new(name='Bip01 '+key);group.add([i],value,'REPLACE')
  mod=ob.modifiers.new('Crowd skin','ARMATURE');mod.object=arm;ob.parent=arm;ob.matrix_parent_inverse=arm.matrix_world.inverted()
  verts=[];faces=[];weights=[];uv=[]
  return ob
 def rings(rows,N=32):
  start=len(verts)
  for j,(center,rx,ry,w,axis) in enumerate(rows):
   axis=Vector(axis).normalized();u=Vector((1,0,0))
   if abs(u.dot(axis))>.9:u=Vector((0,1,0))
   u=(u-axis*u.dot(axis)).normalized();v=axis.cross(u)
   for i in range(N):
    t=2*math.pi*i/N;fold=1+.022*math.cos(12*t+j*.16)
    verts.append(Vector(center)+u*(rx*math.cos(t)*fold)+v*(ry*math.sin(t)*fold));weights.append(w);uv.append((i/N,j/max(1,len(rows)-1)))
   if j:
    for i in range(N):
     a=start+(j-1)*N+i;b=start+(j-1)*N+(i+1)%N;c=start+j*N+(i+1)%N;d=start+j*N+i;faces.append((a,b,c,d))
 def torso_weight(z):
  levels=[(.88,'Pelvis'),(1.04,'Spine'),(1.20,'Spine1'),(1.38,'Spine2'),(1.53,'Neck')]
  if z<=levels[0][0]:return {'Pelvis':1}
  for (a,an),(b,bn) in zip(levels,levels[1:]):
   if z<=b:t=(z-a)/(b-a);return {an:1-t,bn:t}
  return {'Neck':1}
 # Ankle-length flare, soft longitudinal folds, round collarless shoulder line.
 rows=[]
 for z,rx,ry in [(.14,.26,.19),(.28,.25,.18),(.48,.24,.175),(.68,.23,.165),(.88,.22,.155),(1.04,.205,.145),(1.18,.21,.145),(1.32,.235,.15),(1.41,.245,.14),(1.46,.195,.115),(1.51,.075,.073)]:
  rows.append(((0,.008,z),rx,ry,torso_weight(z),(0,0,1)))
 rings(rows,40)
 for side in ['L','R']:
  a=bone(side+' UpperArm');b=bone(side+' Forearm');c=bone(side+' Hand');rows=[(a.lerp(bone('Spine2'),.3),.065,.065,{side+' UpperArm':1},b-a)]
  for i in range(9):
   t=i/8
   if t<.5:
    f=t*2;center=a.lerp(b,f);w={side+' UpperArm':1-f*.5,side+' Forearm':f*.5}
   else:
    f=(t-.5)*2;center=b.lerp(c,f);w={side+' Forearm':1-f*.15,side+' Hand':f*.15}
   axis=(b-a) if t<.5 else (c-b);radius=.095*(1-t)+.046*t
   rows.append((center,radius,radius,w,axis))
  rings(rows,20)
 mesh('Dishdasha',cloth)
 # Flat neckline trim and chest tassel, not a standing shirt collar.
 rings([((0,.008,z),r,r,{'Neck':1},(0,0,1)) for z,r in [(1.506,.077),(1.515,.077)]],32)
 rings([((.025,-.143,z),r,r,{'Spine2':1},(0,0,1)) for z,r in [(1.26,.008),(1.29,.006),(1.39,.003),(1.43,.004)]],10)
 mesh('NecklineAndFurakha',trim)
 # Headwear follows the existing head bone; distinct cap and folded turban silhouettes.
 head=bone('Head');top=max((o.matrix_world@v.co).z for o in bpy.data.objects if o.type=='MESH' and o.name=='CrowdSkin' for v in o.data.vertices)
 base=top-.075
 if role=='Talker':
  rings([((head.x,head.y,z),rx,ry,{'Head':1},(0,0,1)) for z,rx,ry in [(base,.091,.102),(base+.025,.094,.106),(base+.085,.087,.097),(base+.10,.055,.062),(base+.105,.001,.001)]],40)
 else:
  rows=[]
  for j in range(13):
   f=j/12;rad=math.sin(math.pi*(.18+.82*f));rows.append(((head.x,head.y,base-.015+f*.16),.117*rad,.128*rad,{'Head':1},(0,0,1)))
  rings(rows,40)
 mesh('Kumma' if role=='Talker' else 'Mussar',hat)
 if role=='Phone':
  # Four overlapping cloth folds, kept modest for the distant phone spectator.
  for j in range(4):
   N=48;start=len(verts)
   for i in range(N):
    t=2*math.pi*i/N
    for k in [0,1]:
     verts.append((head.x+.112*math.cos(t),head.y+.122*math.sin(t),base+.012+j*.023+k*.009+.013*math.cos(t+.6*j)));weights.append({'Head':1});uv.append((i/N,k))
   for i in range(N):a=start+2*i;b=start+2*((i+1)%N);faces.append((a,b,b+1,a+1))
  mesh('MussarFoldEdges',trim)
 # One skinned renderer preserves the existing crowd bounds/culling contract.
 bpy.ops.object.select_all(action='DESELECT')
 for ob in bpy.data.objects:
  if ob.type=='MESH':ob.select_set(True)
 bpy.context.view_layer.objects.active=bpy.data.objects['CrowdSkin'];bpy.ops.object.join()
 arm.data.pose_position='POSE';bpy.context.view_layer.update()
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True)
 for o in bpy.data.objects:
  if o.type=='MESH':o.select_set(True)
 bpy.context.view_layer.objects.active=arm
 bpy.ops.export_scene.fbx(filepath=str(OUT/(role+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_step=1,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',use_mesh_modifiers=True)
 print('OMANI_EXPORTED',role)
for role in ['Talker','Phone']:build(role)

