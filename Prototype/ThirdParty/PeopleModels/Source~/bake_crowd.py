"""Bake MIT Rocketbox mocap to a small, generic skinned crowd. Blender --background.

Source: .asset-cache/crowd-source at the repository root, verified by Git blob SHA.
Output: existing PeopleModels/Animated, without touching original posed OBJ assets.
Each FBX has identical Rig paths for its role and idle clips. No humanoid package
or runtime retargeting dependency. Pose sampling removes planar root travel,
seats the supporting foot, and closes stationary loop tails gently.
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector, Quaternion
import numpy as np

ROOT=Path.cwd(); CACHE=ROOT/'.asset-cache/crowd-source'; OUT=ROOT/'Prototype/ThirdParty/PeopleModels/Animated'
BODY=('Pelvis','Spine','Neck','Head','Clavicle','UpperArm','Forearm','Hand','Finger','Thigh','Calf','Foot','Toe')
JOBS=[
 ('Walker','Adults/Male_Adult_05','m_walk_slow_01','xy',True),
 ('Walker_Idle','Adults/Male_Adult_05','m_idle_neutral_02','static',False),
 ('Talker','Adults/Male_Adult_10','m_gestic_talk_neutral_01','static',False),
 ('Talker_Idle','Adults/Male_Adult_10','m_idle_neutral_02','static',False),
 ('Listener','Adults/Female_Adult_06','f_gestic_talk_neutral_01','static',False),
 ('Listener_Idle','Adults/Female_Adult_06','f_idle_neutral_02','static',False),
 ('Phone','Adults/Male_Adult_12','m_cell_phone_talk_01','static',False),
 ('Observer','Professions/Business_Female_04','f_idle_neutral_02','static',False),
]

def import_fbx(path,anim):
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(path),use_anim=anim)
 return [o for o in bpy.data.objects if o not in before]

def world(arm,bone): return arm.matrix_world@arm.pose.bones[bone].matrix

def bake(job):
 name,avatar,motion,folder,walking=job
 bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene
 objects=import_fbx(CACHE/'Assets/Avatars'/avatar/'Export'/(Path(avatar).name+'.fbx'),False)
 arm=next(o for o in objects if o.type=='ARMATURE');mesh=next(o for o in objects if o.type=='MESH')
 arm.name='Rig';mesh.name='CrowdSkin'
 bpy.context.view_layer.update()
 rest_forward=world(arm,'Bip01 MNose').translation-world(arm,'Bip01 Head').translation;rest_forward.z=0;rest_forward.normalize()
 forward_local=world(arm,'Bip01 Pelvis').to_3x3().inverted()@rest_forward
 animated=import_fbx(CACHE/'Assets/Animations'/('all_animations_max_motextr_'+folder)/(motion+'.max.fbx'),True)
 source=next(o for o in animated if o.type=='ARMATURE')
 action=source.animation_data.action;start,end=map(int,action.frame_range)
 fps=scene.render.fps/scene.render.fps_base
 if name=='Phone': start+=int((end-start)*.16);end-=int((end-start)*.12)
 scene.frame_set(start);bpy.context.view_layer.update()
 f=world(source,'Bip01 Pelvis').to_3x3()@forward_local;f.z=0;f.normalize()
 # The common source direction is Blender -Y, exported as Unity +Z.
 space=bpy.data.objects.new('SourceOrientation',None);scene.collection.objects.link(space)
 previous=source.matrix_world.copy();source.parent=space;source.matrix_world=previous
 space.rotation_euler.z=-math.pi/2-math.atan2(f.y,f.x)
 for pb in arm.pose.bones:
  short=pb.name.replace('Bip01 ','')
  if pb.name not in source.pose.bones or not any(short.startswith(k) or (' '+k) in pb.name for k in BODY): continue
  c=pb.constraints.new('COPY_ROTATION');c.target=source;c.subtarget=pb.name;c.owner_space=c.target_space='WORLD'
  if short=='Pelvis':
   c=pb.constraints.new('COPY_LOCATION');c.target=source;c.subtarget=pb.name;c.owner_space=c.target_space='WORLD'
 scene.frame_set(start);bpy.context.view_layer.update();first=world(source,'Bip01 Pelvis').translation.copy()
 scene.frame_set(end);bpy.context.view_layer.update();last=world(source,'Bip01 Pelvis').translation.copy()
 travel=last-first;travel.z=0
 duration=(end-start)/fps
 frames=list(range(start,end+1,2))
 if frames[-1]!=end:frames.append(end)
 poses=[];heights=[]
 for frame in frames:
  scene.frame_set(frame);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
  evmesh=mesh.evaluated_get(dg);me=evmesh.to_mesh();coords=np.empty(len(me.vertices)*3,dtype=np.float64);me.vertices.foreach_get('co',coords);coords=coords.reshape(-1,3)
  mw=np.array(mesh.matrix_world);z=coords@mw[2,:3]+mw[2,3];floor=float(z.min());heights.append(float(z.max()-floor));evmesh.to_mesh_clear()
  t=(frame-start)/max(1,end-start)
  offset=Vector((-first.x-travel.x*t,-first.y-travel.y*t,-floor))
  local_offset=arm.matrix_world.to_3x3().inverted()@offset
  evaluated=arm.evaluated_get(dg)
  matrices={pb.name:pb.matrix.copy() for pb in evaluated.pose.bones}
  for m in matrices.values():m.translation+=local_offset
  poses.append(matrices)
 # Remove constraints/source animation only after every evaluated pose is captured.
 for pb in arm.pose.bones:
  for c in list(pb.constraints):pb.constraints.remove(c)
  pb.rotation_mode='QUATERNION'
 arm.animation_data_clear();arm.animation_data_create()
 result=bpy.data.actions.new(name);arm.animation_data.action=result
 for idx,(frame,matrices) in enumerate(zip(frames,poses)):
  scene.frame_set(frame-start+1)
  # Cyclic shoulder/hand settling rather than an abrupt cut at the end of gestures.
  tail=.12 if walking else .65
  remaining=(end-frame)/fps
  if remaining<tail:
   blend=1-remaining/tail;blend=blend*blend*(3-2*blend)
   matrices={key:value.copy() for key,value in matrices.items()}
   for pb in arm.pose.bones:
    a=matrices[pb.name];b=poses[0][pb.name]
    loc=a.translation.lerp(b.translation,blend);rot=a.to_quaternion().slerp(b.to_quaternion(),blend);scale=a.to_scale().lerp(b.to_scale(),blend)
    from mathutils import Matrix
    matrices[pb.name]=Matrix.LocRotScale(loc,rot,scale)
  # Derive each local basis from the captured parent, never the previous frame's
  # dependency-graph pose. Sequential pb.matrix assignment leaves stale parent
  # matrices during baking and can stretch facial and finger vertices in FBX.
  for pb in arm.pose.bones:
   args={}
   if pb.parent:args={'parent_matrix':matrices[pb.parent.name],'parent_matrix_local':pb.parent.bone.matrix_local}
   pb.matrix_basis=pb.bone.convert_local_to_pose(matrices[pb.name],pb.bone.matrix_local,invert=True,**args)
  for pb in arm.pose.bones:
   pb.keyframe_insert('location',frame=frame-start+1,group=pb.name);pb.keyframe_insert('rotation_quaternion',frame=frame-start+1,group=pb.name);pb.keyframe_insert('scale',frame=frame-start+1,group=pb.name)
 scene.frame_start=1;scene.frame_end=end-start+1;scene.frame_set(1);bpy.context.view_layer.update()
 # Only the character's armature/mesh, with the same hierarchy for all its clips.
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=arm
 OUT.mkdir(exist_ok=True)
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,
  axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',use_mesh_modifiers=True,
  bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_use_all_bones=False,bake_anim_step=2,bake_anim_simplify_factor=.3,
  path_mode='STRIP',embed_textures=False)
 result={'name':name,'avatar':avatar,'motion':motion,'motion_folder':folder,'source_frames':[start,end],'fps':fps,'duration':duration,
  'stride_distance':float(travel.length) if walking else 0,'walk_speed':float(travel.length/max(.001,duration)) if walking else 0,
  'height_range':[min(heights),max(heights)],'bones':len(arm.data.bones),'vertices':len(mesh.data.vertices),'polygons':len(mesh.data.polygons)}
 print('CROWD_BAKED',json.dumps(result),flush=True);return result

selected=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
report=[]
for job in JOBS:
 if not selected or job[0] in selected:report.append(bake(job))
(OUT/'bake-report.json').write_text(json.dumps(report,indent=2))
