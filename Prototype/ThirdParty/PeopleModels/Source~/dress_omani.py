"""Original Omani garments fitted to the existing MIT Rocketbox crowd rigs.
Run Blender background from repository root. Original animation and exposed skin
are retained; covered western clothing is removed, never layered under the robe.
"""
import bpy, math, json,sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
sys.path.insert(0,str(Path(__file__).parent))
from refine_crowd_meshes import refine_skin,add_distance_mesh,export_actor
ROOT=Path.cwd(); OUT=ROOT/'Prototype/ThirdParty/PeopleModels/OmaniCrowd'
OUT.mkdir(parents=True,exist_ok=True)

def build(role):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(ROOT/f'Prototype/ThirdParty/PeopleModels/Animated/{role}.fbx'))
 arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');arm.data.pose_position='REST';bpy.context.view_layer.update()
 def bone(n):return arm.matrix_world@arm.data.bones['Bip01 '+n].head_local
 # Preserve the original anatomically fitted shoulders and arms as a tailoring
 # target, before removing the western clothing. This is a surface reference,
 # not a second jacket left beneath the robe.
 body=next(o for o in bpy.data.objects if o.type=='MESH');body.data.calc_loop_triangles()
 body_points=[body.matrix_world@v.co for v in body.data.vertices];body_faces=[]
 for tri in body.data.loop_triangles:
  mat=body.data.materials[tri.material_index].name.lower();center=sum((body_points[i] for i in tri.vertices),Vector())/3
  if 'body' in mat and center.z>.90:body_faces.append(tuple(tri.vertices))
 body_tree=BVHTree.FromPolygons(body_points,body_faces,all_triangles=True)
 # Keep original skin, neck and footwear, removing all covered clothing and hair cards.
 for ob in list(bpy.data.objects):
  if ob.type!='MESH':continue
  import bmesh
  bm=bmesh.new();bm.from_mesh(ob.data)
  remove=[]
  for f in bm.faces:
   pts=[ob.matrix_world@v.co for v in f.verts];c=sum(pts,Vector())/len(pts)
   mat=ob.data.materials[f.material_index].name.lower()
   side='L' if c.x>0 else 'R';wrist=bone(side+' Hand');direction=(wrist-bone(side+' Forearm')).normalized()
   hand=max((p-wrist).dot(direction) for p in pts)>-.008 and abs(c.x)>.52
   keep=((c.z>bone('Neck').z-.025 and 'head' in mat) or c.z<.105 or hand)
   if not keep or 'opacity' in mat:remove.append(f)
  bmesh.ops.delete(bm,geom=remove,context='FACES')
  # Extend the existing textured neck inside the collar. The source jackets
  # hid this boundary; a nod must not expose a gap between head and dishdasha.
  neck=bone('Neck');edges=[]
  for e in bm.edges:
   pts=[ob.matrix_world@v.co for v in e.verts]
   if e.is_boundary and all(abs(p.x)<.11 and neck.z-.07<p.z<neck.z+.003 for p in pts):edges.append(e)
  result=bmesh.ops.extrude_edge_only(bm,edges=edges)
  deform=bm.verts.layers.deform.active;spine=ob.vertex_groups['Bip01 Spine2'].index;neck_group=ob.vertex_groups['Bip01 Neck'].index
  for v in [g for g in result['geom'] if isinstance(g,bmesh.types.BMVert)]:
   p=ob.matrix_world@v.co;p.x*=.97;p.y=neck.y+(p.y-neck.y)*.97;p.z-=.065;v.co=ob.matrix_world.inverted()@p
   v[deform].clear();v[deform][spine]=.8;v[deform][neck_group]=.2
  bm.to_mesh(ob.data);bm.free()
  refine_skin(ob)
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
 def rings(rows,N=32,closed=False,folds=True):
  start=len(verts)
  for j,(center,rx,ry,w,axis) in enumerate(rows):
   axis=Vector(axis).normalized();u=Vector((1,0,0))
   if abs(u.dot(axis))>.9:u=Vector((0,1,0))
   u=(u-axis*u.dot(axis)).normalized();v=axis.cross(u)
   for i in range(N):
    t=2*math.pi*i/N
    # Several unequal folds, rather than identical vertical corrugations.
    fold=1+(.043*math.cos(9*t+.18*math.sin(j*.3))+.025*math.sin(14*t+j*.13)+.012*math.cos(5*t-j*.08) if folds else 0)
    verts.append(Vector(center)+u*(rx*math.cos(t)*fold)+v*(ry*math.sin(t)*fold));weights.append(w);uv.append((i/N,j/max(1,len(rows)-1)))
   if j:
    for i in range(N):
     a=start+(j-1)*N+i;b=start+(j-1)*N+(i+1)%N;c=start+j*N+(i+1)%N;d=start+j*N+i;faces.append((a,b,c,d))
  if closed:
   faces.append(tuple(start+i for i in reversed(range(N))))
   faces.append(tuple(start+(len(rows)-1)*N+i for i in range(N)))
 def tailor(ob):
  # Join the shoulder/sleeve volumes into one continuous garment. Preserve
  # original bone weights and surface UVs by barycentric transfer in rest pose.
  bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
  for mod in list(ob.modifiers):ob.modifiers.remove(mod)
  import bmesh
  bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
  ob.data.calc_loop_triangles()
  source=[v.co.copy() for v in ob.data.vertices]
  triangles=[tuple(t.vertices) for t in ob.data.loop_triangles]
  source_weights=[{g.group:g.weight for g in v.groups} for v in ob.data.vertices]
  source_uv=[tuple(ob.data.uv_layers.active.data[li].uv.copy() for li in t.loops) for t in ob.data.loop_triangles]
  tree=BVHTree.FromPolygons(source,triangles,all_triangles=True)
  remesh=ob.modifiers.new('Continuous tailored shoulders','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.011;remesh.use_smooth_shade=True
  bpy.ops.object.modifier_apply(modifier=remesh.name)
  smooth=ob.modifiers.new('Relax cloth joins','SMOOTH');smooth.factor=.65;smooth.iterations=5;bpy.ops.object.modifier_apply(modifier=smooth.name)
  # Shape the upper garment around the actual body surface. Cylindrical sleeves
  # and an ellipsoid chest cannot reproduce a shoulder's slope or an elbow.
  fitted=0
  for v in ob.data.vertices:
   if v.co.z<.90:continue
   p,n,idx,d=body_tree.find_nearest(v.co)
   if d>.13:continue
   blend=min(1,max(0,(v.co.z-1.20)/.13))*.90 if abs(v.co.x)<.24 else .90
   # The source phone actor's jacket has overlapping front flaps. Do not imprint
   # those flaps/zipper onto a continuous dishdasha chest.
   if v.co.y<-.035:blend*=min(1,abs(v.co.x)/.17)
   if v.co.z>1.49:blend*=max(0,(1.52-v.co.z)/.03)
   v.co=v.co.lerp(p+n*.007,blend);fitted+=1
  # Small, uneven compression folds at the elbow and cuff; no cloth simulation.
  for v in ob.data.vertices:
   if abs(v.co.x)<.24:continue
   side='L' if v.co.x>0 else 'R';elbow=bone(side+' Forearm');wrist=bone(side+' Hand');axis=(wrist-elbow).normalized()
   longitudinal=(v.co-elbow).dot(axis);radial=v.co-(elbow+axis*longitudinal)
   if radial.length<.008:continue
   envelope=math.exp(-(longitudinal/.055)**2)+.7*math.exp(-((longitudinal-(wrist-elbow).length+.025)/.045)**2)
   v.co+=radial.normalized()*(.0022*math.sin(longitudinal*175+radial.y*33)*envelope)
  print('TAILORED_BODY_PROJECTION',role,fitted)
  relax_group=ob.vertex_groups.new(name='TailoringRelax')
  for v in ob.data.vertices:
   if v.co.z>.93:relax_group.add([v.index],min(1,(v.co.z-.93)/.15),'REPLACE')
  smooth=ob.modifiers.new('Relax fitted fabric','SMOOTH');smooth.factor=.5;smooth.iterations=10;smooth.vertex_group=relax_group.name;bpy.ops.object.modifier_apply(modifier=smooth.name)
  temporary_group=ob.vertex_groups.get('TailoringRelax')
  if temporary_group:ob.vertex_groups.remove(temporary_group)
  tris=sum(len(p.vertices)-2 for p in ob.data.polygons)
  dec=ob.modifiers.new('Crowd cloth budget','DECIMATE');dec.ratio=min(1,7600/max(1,tris));bpy.ops.object.modifier_apply(modifier=dec.name)
  # Open the ankle hem again; the closed volume was needed only for union.
  bm=bmesh.new();bm.from_mesh(ob.data)
  bmesh.ops.delete(bm,geom=[f for f in bm.faces if all(v.co.z<.112 for v in f.verts)],context='FACES')
  hem=[e for e in bm.edges if e.is_boundary and max(v.co.z for v in e.verts)<.14]
  result=bmesh.ops.extrude_edge_only(bm,edges=hem)
  for v in [g for g in result['geom'] if isinstance(g,bmesh.types.BMVert)]:v.co.x*=.993;v.co.y*=.993;v.co.z+=.006
  bm.to_mesh(ob.data);bm.free()
  ob.vertex_groups.clear()
  layer=ob.data.uv_layers.get('UVMap') or ob.data.uv_layers.new(name='UVMap')
  projected_uv={};skin=[]
  for v in ob.data.vertices:
   point,normal,idx,distance=tree.find_nearest(v.co);ids=triangles[idx];a,b,c=[source[i] for i in ids]
   bary=barycentric_transform(point,a,b,c,Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)))
   w={}
   for vi,amount in zip(ids,bary):
    for key,value in source_weights[vi].items():w[key]=w.get(key,0)+value*max(0,amount)
   skin.append(w)
   uvs=source_uv[idx];u=sum(p.x*q for p,q in zip(uvs,bary));vv=sum(p.y*q for p,q in zip(uvs,bary));projected_uv[v.index]=(u,vv)
  # The union removes intersecting surfaces; blend weights across that new
  # shoulder seam too, so adjacent vertices do not follow different rigid bones.
  neighbours=[set() for v in ob.data.vertices]
  for e in ob.data.edges:
   a,b=e.vertices;neighbours[a].add(b);neighbours[b].add(a)
  for iteration in range(20):
   relaxed=[]
   for i,w in enumerate(skin):
    if not neighbours[i]:relaxed.append(w);continue
    combined={key:value*.35 for key,value in w.items()}
    for j in neighbours[i]:
     for key,value in skin[j].items():combined[key]=combined.get(key,0)+value*.65/len(neighbours[i])
    relaxed.append(combined)
   skin=relaxed
  for i,w in enumerate(skin):
   best=sorted(w.items(),key=lambda item:item[1],reverse=True)[:4];total=sum(value for _,value in best)
   for key,value in best:
    name=weight_names[key];group=ob.vertex_groups.get(name) or ob.vertex_groups.new(name=name);group.add([i],value/max(total,1e-8),'REPLACE')
  for poly in ob.data.polygons:
   poly.use_smooth=True
   for li in poly.loop_indices:layer.data[li].uv=projected_uv[ob.data.loops[li].vertex_index]
  mod=ob.modifiers.new('Crowd skin','ARMATURE');mod.object=arm
  print('TAILORED_GARMENT',role,'triangles',sum(len(p.vertices)-2 for p in ob.data.polygons))
 def torso_weight(z):
  levels=[(.88,'Pelvis'),(1.04,'Spine'),(1.20,'Spine1'),(1.38,'Spine2'),(1.53,'Neck')]
  if z<=levels[0][0]:return {'Pelvis':1}
  for (a,an),(b,bn) in zip(levels,levels[1:]):
   if z<=b:t=(z-a)/(b-a);return {an:1-t,bn:t}
  return {'Neck':1}
 # Ankle-length flare, soft longitudinal folds, round collarless shoulder line.
 rows=[]
 for z,rx,ry in [(.105,.25,.18),(.20,.247,.177),(.30,.242,.173),(.40,.236,.169),(.50,.23,.166),(.60,.225,.162),(.70,.22,.158),(.80,.215,.153),(.90,.21,.147),(1.04,.205,.142),(1.18,.21,.14),(1.32,.215,.14),(1.40,.205,.13),(1.465,.14,.10),(1.515,.073,.071)]:
  rows.append(((0,.008,z),rx,ry,torso_weight(z),(0,0,1)))
 rings(rows,64,closed=True)
 for side in ['L','R']:
  a=bone(side+' UpperArm');b=bone(side+' Forearm');c=bone(side+' Hand');rows=[(a.lerp(bone('Spine2'),.3),.065,.065,{side+' UpperArm':1},b-a)]
  for i in range(9):
   t=i/8
   if t<.5:
    f=t*2;center=a.lerp(b,f);w={side+' UpperArm':1-f*.5,side+' Forearm':f*.5}
   else:
    f=(t-.5)*2;center=b.lerp(c,f);w={side+' Forearm':1-f*.15,side+' Hand':f*.15}
   axis=(b-a) if t<.5 else (c-b);radius=.080*(1-t)+.046*t
   if i==8:center+=axis.normalized()*.020
   rows.append((center,radius,radius,w,axis))
  rings(rows,24,closed=True)
 garment=mesh('Dishdasha',cloth)
 weight_names={g.index:g.name for g in garment.vertex_groups}
 tailor(garment)
 # Turned cuff bands cover the irregular union boundary without clipping the
 # palm. They follow the anatomical wrist axis and are skinned to the forearm.
 for side in ['L','R']:
  wrist=bone(side+' Hand');axis=(wrist-bone(side+' Forearm')).normalized()
  rings([(wrist+axis*d,r,r,{side+' Forearm':.85,side+' Hand':.15},axis) for d,r in [(.005,.041),(.009,.042),(.014,.042),(.017,.041)]],32,folds=False)
 mesh('TurnedSleeveCuffs',cloth)
 # The neckline is part of the garment surface. A separate rigid trim ring
 # detached from the cloth during head bends, so only the cotton tassel is added.
 garment.data.calc_loop_triangles();chest_tree=BVHTree.FromPolygons([v.co for v in garment.data.vertices],[tuple(t.vertices) for t in garment.data.loop_triangles],all_triangles=True)
 tassel=[]
 for z,r in [(1.285,.003),(1.29,.005),(1.305,.004),(1.40,.002),(1.43,.003)]:
  hit,normal,idx,distance=chest_tree.ray_cast(Vector((.025,-.5,z)),Vector((0,1,0)))
  tassel.append(((.025,hit.y-.003 if hit else -.15,z),r,r,{'Spine2':1},(0,0,1)))
 rings(tassel,12,folds=False)
 mesh('CottonFurakha',cloth)
 # Headwear follows the existing head bone; distinct cap and folded turban silhouettes.
 head=bone('Head');top=max((o.matrix_world@v.co).z for o in bpy.data.objects if o.type=='MESH' and o.name=='CrowdSkin' for v in o.data.vertices)
 base=top-.063
 skin=bpy.data.objects['CrowdSkin'];scalp=[skin.matrix_world@v.co for v in skin.data.vertices if base-.015<(skin.matrix_world@v.co).z<base+.025]
 head.x=(min(p.x for p in scalp)+max(p.x for p in scalp))*.5;head.y=(min(p.y for p in scalp)+max(p.y for p in scalp))*.5
 # Covered scalp geometry must not poke through the fitted cloth headwear.
 bm=bmesh.new();bm.from_mesh(skin.data)
 bmesh.ops.delete(bm,geom=[f for f in bm.faces if (skin.matrix_world@f.calc_center_median()).z>base+.005],context='FACES');bm.to_mesh(skin.data);bm.free()
 if role=='Talker':
  rings([((head.x,head.y,z),rx,ry,{'Head':1},(0,0,1)) for z,rx,ry in [(base,.093,.103),(base+.003,.094,.104),(base+.052,.092,.102),(base+.066,.088,.098),(base+.072,.066,.073),(base+.075,.001,.001)]],64,closed=True,folds=False)
 else:
  profile=[(-.025,.091,.105),(0,.108,.121),(.030,.114,.126),(.060,.108,.119),(.090,.089,.10),(.115,.05,.058),(.123,.001,.001)]
  rings([((head.x,head.y,base+z),rx,ry,{'Head':1},(0,0,1)) for z,rx,ry in profile],64,closed=True,folds=False)
 mesh('Kumma' if role=='Talker' else 'Mussar',hat)
 if role=='Talker':
  # Seated embroidered diamond motifs are actual narrow geometry, rather than
  # a painted cylinder with a jagged rim. No new texture or transparency layer.
  for j in range(24):
   angle=j*2*math.pi/24;points=[]
   for da,dz in [(-.065,0),(0,.017),(.065,0),(0,-.017),(-.065,0)]:
    t=angle+da;z=base+.029+dz;f=(z-base-.003)/.049;rx=.094-.002*f;ry=.104-.002*f
    points.append(Vector((head.x+(rx+.0006)*math.cos(t),head.y+(ry+.0006)*math.sin(t),z)))
   for a,b in zip(points,points[1:]):
    normal=Vector(((a.x+b.x)*.5-head.x,(a.y+b.y)*.5-head.y,0)).normalized();cross=normal.cross(b-a).normalized()*.00050;start=len(verts)
    for p in [a-cross,a+cross,b+cross,b-cross]:verts.append(p);weights.append({'Head':1});uv.append((0,0))
    faces.append((start,start+3,start+2,start+1))
  mesh('KummaStitchedMotifs',trim)
 if role=='Phone':
  # Broad overlapping cloth wraps conform to the cap, with seated edges rather
  # than floating horizontal ribbons. Each follows the changing head radius.
  def profile_radius(z):
   for (a,ax,ay),(b,bx,by) in zip(profile,profile[1:]):
    if z<=b:
     f=max(0,(z-a)/(b-a));return ax+(bx-ax)*f,ay+(by-ay)*f
   return .001,.001
  for j in range(3):
   N=96;start=len(verts)
   for i in range(N):
    t=2*math.pi*i/(N-1)
    for k in [0,1]:
     z=-.010+j*.026+k*.030+.010*math.cos(t+.8*j);rx,ry=profile_radius(z);ease=.002+.0015*math.sin(k*math.pi/2)
     verts.append((head.x+(rx+ease)*math.cos(t),head.y+(ry+ease)*math.sin(t),base+z));weights.append({'Head':1});uv.append((i/(N-1),k))
   for i in range(N-1):a=start+2*i;b=a+2;faces.append((a,b,b+1,a+1))
  mesh('MussarClothWraps',hat)
 # One skinned renderer preserves the existing crowd bounds/culling contract.
 bpy.ops.object.select_all(action='DESELECT')
 for ob in bpy.data.objects:
  if ob.type=='MESH':ob.select_set(True)
 bpy.context.view_layer.objects.active=bpy.data.objects['CrowdSkin'];bpy.ops.object.join()
 add_distance_mesh(bpy.context.view_layer.objects.active,.30)
 arm.data.pose_position='POSE';bpy.context.view_layer.update()
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True)
 for o in bpy.data.objects:
  if o.type=='MESH':o.select_set(True)
 bpy.context.view_layer.objects.active=arm
 export_actor(arm,OUT/(role+'.fbx'))
 print('OMANI_EXPORTED',role)
for role in ['Talker','Phone']:build(role)

