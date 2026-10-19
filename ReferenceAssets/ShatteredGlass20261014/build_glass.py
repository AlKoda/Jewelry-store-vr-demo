import bpy, math, random, pathlib, json, sys
from mathutils import Vector, Matrix
R=pathlib.Path(__file__).resolve().parent;OUT=R/'Deliverables'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC'
def mat(name,color,rough,trans=0,metal=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Transmission Weight'].default_value=trans;p.inputs['Metallic'].default_value=metal;p.inputs['IOR'].default_value=1.52;return m
face=mat('ClearFractureFaces',(.96,.99,.98),.025,1)
edge=mat('FreshCutEdges',(.65,.84,.76),.13,.94)
metal=mat('FrameGraphite',(.055,.062,.068),.3,metal=.75)
floor=mat('Pavement',(.075,.082,.09),.65)
# Shader microstructure is preview-only, never a white/emissive shard paint.
n=floor.node_tree.nodes;l=floor.node_tree.links;t=n.new('ShaderNodeTexNoise');t.inputs['Scale'].default_value=190;t.inputs['Detail'].default_value=2;b=n.new('ShaderNodeBump');b.inputs['Strength'].default_value=.24;b.inputs['Distance'].default_value=.002;l.new(t.outputs['Fac'],b.inputs['Height']);l.new(b.outputs['Normal'],n.get('Principled BSDF').inputs['Normal'])
class Builder:
 def __init__(self):self.v=[];self.f=[];self.mi=[];self.parts=0
 def prism(self,poly,thickness,vertical=False,centre=(0,0,0),rotation=None):
  self.parts+=1;start=len(self.v);N=len(poly)
  for z in [-thickness/2,thickness/2]:
   for x,y in poly:
    p=Vector((x,-z,y)) if vertical else Vector((x,y,z))
    if rotation:p=rotation@p
    self.v.append(tuple(p+Vector(centre)))
  self.f.extend([tuple(start+i for i in reversed(range(N))),tuple(start+N+i for i in range(N))]);self.mi.extend([0,0])
  for i in range(N):j=(i+1)%N;self.f.append((start+i,start+j,start+N+j,start+N+i));self.mi.append(1)
 def emit(self,name):
  m=bpy.data.meshes.new(name);m.from_pydata(self.v,[],self.f);m.materials.append(face);m.materials.append(edge);m.update()
  for p,i in zip(m.polygons,self.mi):p.material_index=i
  uv=m.uv_layers.new()
  for p in m.polygons:
   a=max(range(3),key=lambda i:abs(p.normal[i]));axes=[(1,2),(0,2),(0,1)][a]
   for li in p.loop_indices:v=m.vertices[m.loops[li].vertex_index].co;uv.data[li].uv=(v[axes[0]],v[axes[1]])
  o=bpy.data.objects.new(name,m);scene.collection.objects.link(o);o['fragment_count']=self.parts;return o
# Radial rings share vertices before tiny separation, preserving a coherent impact pattern.
def panel(name,w,h,seed,broken):
 rng=random.Random(seed);cx=rng.uniform(-.12,.12)*w;cy=h*.47;count=34
 angles=sorted((i+rng.uniform(-.24,.24))*2*math.pi/count for i in range(count))
 angles=sorted(angles+[(math.atan2(y-cy,x-cx)%(2*math.pi)) for x,y in [(-w/2,0),(w/2,0),(w/2,h),(-w/2,h)]])
 count=len(angles)
 rings=[[] for i in range(5)]
 for a in angles:
  dx,dy=math.cos(a),math.sin(a)
  limits=[((w/2-cx)/dx if dx>0 else (-w/2-cx)/dx),((h-cy)/dy if dy>0 else -cy/dy)]
  length=min(limits)
  for j,t in enumerate([.04,.20,.47,.74,1.]):
   f=t if j==4 else t*rng.uniform(.8,1.12)
   rings[j].append((cx+dx*length*f,cy+dy*length*f))
 b=Builder()
 for j in range(4):
  if broken and j<2:continue
  for i in range(count):
   k=(i+1)%count;poly=[rings[j][i],rings[j+1][i],rings[j+1][k],rings[j][k]]
   # Slight opening between adjoining faces reads as a fracture instead of painted lines.
   mid=sum((Vector(p) for p in poly),Vector((0,0)))/4
   poly=[tuple(mid+(Vector(p)-mid)*.996) for p in poly]
   b.prism(poly,.008,True)
 if not broken:
  for i in range(count):b.prism([(cx,cy),rings[0][i],rings[0][(i+1)%count]],.008,True)
 return b.emit(name)
assets=[]
for args in [('Window_Broken',1.92,2.13,31,True),('Window_Cracked',1.92,2.13,31,False),('DoorGlass_Left_Broken',.775,2.05,15,True),('DoorGlass_Right_Broken',.775,2.05,21,True)]:assets.append(panel(*args))
def scatter(name,seed,count,rx,ry):
 rng=random.Random(seed);b=Builder()
 for i in range(count):
  radius=rng.random()**1.7;a=rng.uniform(0,2*math.pi);x=math.cos(a)*rx*radius;y=math.sin(a)*ry*radius
  size=rng.uniform(.008,.036)*(1.5-radius)
  if i<12:size=rng.uniform(.045,.10)
  sides=rng.choice([3,4,5,6]);theta=rng.uniform(0,6.28)
  # Points on an ellipse are convex; thin facets remain closed solids.
  angles=sorted(theta+(j+rng.uniform(-.20,.20))*2*math.pi/sides for j in range(sides))
  stretch=rng.uniform(.3,1.0);poly=[(math.cos(a)*size,math.sin(a)*size*stretch) for a in angles]
  thickness=rng.uniform(.003,.007);rotation=Matrix.Rotation(rng.uniform(-.15,.15),3,'X')@Matrix.Rotation(rng.uniform(-.15,.15),3,'Y')
  bottom=min((rotation@Vector((px,py,z))).z for px,py in poly for z in [-thickness/2,thickness/2])
  b.prism(poly,thickness,centre=(x,y,.0012-bottom),rotation=rotation)
 return b.emit(name)
assets.extend([scatter('GroundGlass_Threshold',41,190,1.2,.85),scatter('GroundGlass_WindowFall',52,135,.95,.55),scatter('GroundGlass_Sparse',91,45,1.1,.8)])
report=[]
for o in assets:
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.export_scene.fbx(filepath=str(OUT/(o.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
 bpy.ops.wm.obj_export(filepath=str(OUT/(o.name+'.obj')),export_selected_objects=True,forward_axis='NEGATIVE_Z',up_axis='Y',export_materials=True,export_triangulated_mesh=True)
 o.data.calc_loop_triangles();report.append({'name':o.name,'triangles':len(o.data.loop_triangles),'fragments':o['fragment_count'],'mesh_objects':1,'material_slots':2,'dimensions_blender_xyz':list(o.dimensions)})
(OUT/'geometry-report.json').write_text(json.dumps(report,indent=2))
# Contact sheet scene: framed broken window, broken door and their loose glass.
for o in assets:o.hide_render=True
window=assets[0];window.hide_render=False;window.location=(0,0,.43)
door=assets[2];door.hide_render=False;door.location=(1.73,0,.19)
ground=assets[4];ground.hide_render=False;ground.location=(1.35,-.48,0)
fall=assets[5];fall.hide_render=False;fall.location=(-.12,-.20,0)
def box(name,loc,size,m):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m);mod=o.modifiers.new('Edge','BEVEL');mod.width=.002;mod.segments=2;return o
for x in [-.99,.99]:box('WindowFrame',(x,0,1.5),(.06,.1,3),metal)
for z in [.39,2.6]:box('WindowRail',(0,0,z),(1.98,.1,.08),metal)
box('WindowKickPanel',(0,0,.18),(1.98,.09,.35),metal)
for x in [1.3075,2.1525]:box('DoorFrame',(x,0,1.2),(.07,.09,2.4),metal)
for z in [.10,2.28]:box('DoorRail',(1.73,0,z),(.915,.09,.16 if z<1 else .08),metal)
box('DoorPull',(2.10,-.09,1.05),(.022,.025,.60),metal)
box('Floor',(0,0,-.03),(200,200,.06),floor)
# Muted background provides readable contrast through clear glass.
box('BackWall',(0,2.5,1.5),(12,.1,3),mat('WarmPlaster',(.19,.16,.13),.9))
world=bpy.data.worlds.new('NightStudio');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.16,.24,1);world.node_tree.nodes['Background'].inputs[1].default_value=.3
for name,loc,power,size,color,target in [('ColdRake',(-2,-2,2.8),450,2.5,(.7,.82,1),(0,0,1)),('WarmInterior',(1,1.8,2.5),350,1.6,(1,.76,.48),(0,0,1)),('GroundHighlight',(-1,-1,1.0),100,.8,(.8,.9,1),(.3,0,0))]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='RECTANGLE';d.size=size;d.size_y=size*.25;d.color=color;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(4,-6,3.1));c=bpy.context.object;c.rotation_euler=(Vector((.60,0,1.4))-c.location).to_track_quat('-Z','Y').to_euler();c.data.lens=48;scene.camera=c
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
bpy.ops.wm.save_as_mainfile(filepath=str(R/'ShatteredGlass_Review.blend'))
scene.render.filepath=str(R/'entrance-glass.png');bpy.ops.render.render(write_still=True)
c.location=(1.8,-2.2,.85);c.rotation_euler=(Vector((.65,-.30,.03))-c.location).to_track_quat('-Z','Y').to_euler();c.data.lens=55;scene.render.filepath=str(R/'ground-glass-detail.png');bpy.ops.render.render(write_still=True)
window.hide_render=True;door.hide_render=True;ground.hide_render=True;fall.hide_render=True;assets[1].hide_render=False;assets[1].location=(0,0,.43)
c.location=(2.4,-4.7,2.4);c.rotation_euler=(Vector((0,0,1.5))-c.location).to_track_quat('-Z','Y').to_euler();c.data.lens=55;scene.render.filepath=str(R/'window-cracked.png');bpy.ops.render.render(write_still=True)
print('GLASS_ART_COMPLETE',json.dumps(report))
