"""Refine existing licensed crowd geometry, retaining UVs, bones and motion.

One local Loop-style subdivision on the face/ears/hands rounds angular outlines.
Displacement is bounded to preserve identity, eyelids, lips and finger spacing.
The source FBXs remain untouched; derivatives contain near and distance meshes.
Run in Blender background from the checkout root.
"""
import bpy, bmesh
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree

ROOT=Path.cwd();OUT=ROOT/'Prototype/ThirdParty/PeopleModels/Animated'

def refine_skin(ob):
 displacement_limit=.0018/max(.00001,min(abs(s) for s in ob.matrix_world.to_scale()))
 bm=bmesh.new();bm.from_mesh(ob.data);deform=bm.verts.layers.deform.active
 names={g.index:g.name for g in ob.vertex_groups}
 selected=[]
 for f in bm.faces:
  mat=ob.data.materials[f.material_index].name.lower()
  if 'opacity' in mat:continue
  hand=sum(sum(w for k,w in v[deform].items() if 'Hand' in names[k] or 'Finger' in names[k]) for v in f.verts)/len(f.verts) if deform else 0
  if 'head' in mat or hand>.72:selected.append(f)
 edges=list({e for f in selected for e in f.edges});chosen=set(selected);old_verts={v for f in selected for v in f.verts}
 positions={v:v.co.copy() for v in old_verts};targets={};edge_targets=[];midpoints=[]
 for v in old_verts:
  neighbours=[e.other_vert(v) for e in v.link_edges if e in edges];n=len(neighbours)
  if n<3:continue
  # Hold UV/material boundaries in place: no neck or hairline tears.
  if any(f not in chosen for f in v.link_faces):continue
  beta=3/(8*n) if n>3 else 3/16
  target=v.co*(1-n*beta)+sum((p.co for p in neighbours),Vector())*beta
  delta=(target-v.co)*.65
  if delta.length>displacement_limit:delta*=displacement_limit/delta.length
  targets[v]=v.co+delta
 for e in edges:
  a,b=e.verts;linked=[f for f in e.link_faces if f in chosen]
  midpoint=(a.co+b.co)*.5;target=midpoint.copy()
  if len(linked)==2 and all(len(f.verts)==3 for f in linked):
   opposite=[next(v for v in f.verts if v!=a and v!=b) for f in linked]
   target=(a.co+b.co)*.375+(opposite[0].co+opposite[1].co)*.125
  delta=(target-midpoint)*.75
  if delta.length>displacement_limit:delta*=displacement_limit/delta.length
  midpoints.append(midpoint);edge_targets.append(midpoint+delta)
 tree=KDTree(len(midpoints))
 for i,p in enumerate(midpoints):tree.insert(p,i)
 tree.balance();old_tree=KDTree(len(positions));old_targets=[]
 for i,(v,p) in enumerate(positions.items()):old_tree.insert(p,i);old_targets.append(targets.get(v,p))
 old_tree.balance();before=len(bm.faces)
 result=bmesh.ops.subdivide_edges(bm,edges=edges,cuts=1,use_grid_fill=True)
 for v in bm.verts:
  co,i,d=old_tree.find(v.co)
  if d<.000001:v.co=old_targets[i];continue
  co,i,d=tree.find(v.co)
  if d<.00001:v.co=edge_targets[i]
 bm.normal_update();bm.to_mesh(ob.data);bm.free();ob.data.update()
 print('ANATOMY_REFINED',ob.name,'faces',before,'->',len(ob.data.polygons),'selected',len(selected))

def add_distance_mesh(ob,ratio=.30):
 far=ob.copy();far.data=ob.data.copy();bpy.context.collection.objects.link(far);far.name='CrowdSkin_Distance'
 bpy.ops.object.select_all(action='DESELECT');far.select_set(True);bpy.context.view_layer.objects.active=far
 # Decimation runs before skinning, so both levels share the same bind pose.
 for mod in list(far.modifiers):far.modifiers.remove(mod)
 dec=far.modifiers.new('Distance geometry budget','DECIMATE');dec.ratio=ratio;dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name)
 mod=far.modifiers.new('Crowd skin','ARMATURE');mod.object=next(o for o in bpy.data.objects if o.type=='ARMATURE')
 print('CROWD_LOD_MESH',ob.name,sum(len(p.vertices)-2 for p in ob.data.polygons),'distance',sum(len(p.vertices)-2 for p in far.data.polygons))
 return far

def export_actor(arm,path):
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True)
 for ob in bpy.data.objects:
  if ob.type=='MESH':ob.select_set(True)
 bpy.context.view_layer.objects.active=arm
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_step=1,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',use_mesh_modifiers=True)

def build(role):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(OUT/(role+'.fbx')))
 arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');arm.data.pose_position='REST';bpy.context.view_layer.update()
 skin=next(o for o in bpy.data.objects if o.type=='MESH');refine_skin(skin);add_distance_mesh(skin)
 arm.data.pose_position='POSE';bpy.context.view_layer.update();export_actor(arm,OUT/(role+'_Detailed.fbx'))

if __name__=='__main__':
 for role in ['Walker','Listener','Observer']:build(role)
