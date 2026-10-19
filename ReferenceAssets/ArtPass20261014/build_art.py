import bpy, math, json, pathlib, shutil, sys
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parent
OUT=ROOT/'Deliverables'; OUT.mkdir(exist_ok=True)
TEX=ROOT/'SourceMaterials/Prototype/ThirdParty/PBRInterior'
if not TEX.exists(): TEX=OUT/'Materials'
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene; scene.unit_settings.system='METRIC'
materials={}
def mat(name,color,metal=0,rough=.4,texture=None,tile=1):
 m=bpy.data.materials.new(name); m.use_nodes=True
 n=m.node_tree.nodes; l=m.node_tree.links; p=n.get('Principled BSDF')
 p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 if texture:
  uv=n.new('ShaderNodeTexCoord'); mapping=n.new('ShaderNodeVectorMath');mapping.operation='SCALE';mapping.inputs[3].default_value=1/tile;l.new(uv.outputs['UV'],mapping.inputs[0])
  for suffix in ['Color.jpg','Normal.png','MetallicSmoothness.png']:
   t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(TEX/(texture+'_'+suffix)),check_existing=True);l.new(mapping.outputs[0],t.inputs[0])
   if suffix=='Color.jpg': l.new(t.outputs['Color'],p.inputs['Base Color'])
   else:
    t.image.colorspace_settings.name='Non-Color'
    if suffix=='Normal.png':
     norm=n.new('ShaderNodeNormalMap');norm.inputs['Strength'].default_value=.25;l.new(t.outputs[0],norm.inputs['Color']);l.new(norm.outputs[0],p.inputs['Normal'])
    else:
     sep=n.new('ShaderNodeSeparateColor');l.new(t.outputs[0],sep.inputs[0]);l.new(sep.outputs[0],p.inputs['Metallic']);inv=n.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;l.new(t.outputs['Alpha'],inv.inputs[1]);l.new(inv.outputs[0],p.inputs['Roughness'])
 materials[name]=m;return m
wood=mat('WalnutVeneer',(.12,.06,.03),texture='Walnut',tile=1)
brass=mat('ChampagneBrushed',(.5,.32,.12),1,.3,texture='BronzeBrushed',tile=.5)
suede=mat('IvoryMicrosuede',(.62,.55,.43),rough=.85,texture='SuedeCream',tile=.5)
velvet=mat('CharcoalVelvet',(.025,.028,.032),rough=.85,texture='VelvetCharcoal',tile=.5)
black=mat('GraphiteGasket',(.012,.014,.016),rough=.6)
gold=mat('JewelryGold',(.83,.57,.22),1,.18)
glass=mat('LowIronGlass',(.96,.99,.98),rough=.015);p=glass.node_tree.nodes.get('Principled BSDF');p.inputs['Transmission Weight'].default_value=1;p.inputs['IOR'].default_value=1.52
stone=mat('DarkStone',(.06,.06,.06),texture='StoneTopDark')
assets={}; current=[]
def uv(o):
 layer=o.data.uv_layers.active or o.data.uv_layers.new()
 for poly in o.data.polygons:
  a=max(range(3),key=lambda i:abs(poly.normal[i])); axes=[(1,2),(0,2),(0,1)][a]
  for i in poly.loop_indices:
   v=o.matrix_world@o.data.vertices[o.data.loops[i].vertex_index].co;layer.data[i].uv=(v[axes[0]],v[axes[1]])
def box(name,loc,size,m,bevel=.002):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m);uv(o)
 if bevel:
  b=o.modifiers.new('Machined edge','BEVEL');b.width=bevel;b.segments=2;bpy.ops.object.modifier_apply(modifier=b.name)
  w=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');w.keep_sharp=True;bpy.ops.object.modifier_apply(modifier=w.name)
 current.append(o);return o
def cyl(name,loc,radius,depth,m,rotation=(0,0,0),verts=24):
 bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,depth=depth,location=loc,rotation=rotation);o=bpy.context.object;o.name=name;o.data.materials.append(m)
 for f in o.data.polygons:f.use_smooth=len(f.vertices)==4
 current.append(o);return o
# Model footprint matches existing case. Blender Z up; exports convert to Unity Y up.
box('CabinetShell',(0,0,.372),(1.77,.62,.60),wood,.003)
box('RecessedPlinth',(0,0,.04),(1.66,.53,.08),black,.002)
for x in [-.45,.45]:
 box('FrontVeneerPanel', (x,-.319,.373),(.894,.016,.584),wood,.0015)
 box('RearAccessDoor',(x,.319,.373),(.894,.016,.584),wood,.0015)
 box('RecessedRearPull',(x,.329,.61),(.15,.004,.012),black,.002)
for x in [-.891,.891]:box('EndVeneerPanel',(x,0,.373),(.018,.62,.584),wood,.0015)
box('FloatingDeck',(0,0,.684),(1.8,.65,.028),brass,.002)
box('SuedeDisplayPad',(0,0,.704),(1.742,.592,.012),suede,.003)
for x in [-.887,.887]:
 for y in [-.312,.312]:box('CornerUpright',(x,y,.878),(.018,.018,.344),brass,.001)
for y in [-.312,.312]:
 box('TopLongRail',(0,y,1.041),(1.774,.018,.018),brass,.001)
 box('LowerSeal',(0,y,.718),(1.744,.008,.008),black,.0008)
for x in [-.887,.887]:box('TopEndRail',(x,0,1.041),(.018,.606,.018),brass,.001)
box('Glass_Front',(0,-.312,.875),(1.754,.006,.31),glass,.0005)
box('Glass_Back',(0,.312,.875),(1.754,.006,.31),glass,.0005)
for x in [-.887,.887]:box('Glass_Left' if x<0 else 'Glass_Right',(x,0,.875),(.006,.606,.31),glass,.0005)
box('Glass_Top',(0,0,1.044),(1.754,.606,.006),glass,.0005)
assets['DisplayCase_Hero']=list(current);current=[]
# Ring tray, individual padded rolls and an inset tray base.
box('TrayBase',(0,0,.009),(.34,.23,.018),wood,.003)
for y in [-.11,.11]:box('TrayRim',(0,y,.024),(.34,.012,.036),wood,.002)
for x in [-.164,.164]:box('TrayRim',(x,0,.024),(.012,.208,.036),wood,.002)
for y in [-.075,-.025,.025,.075]:
 cyl('SuedeRoll',(0,y,.034),.024,.308,suede,(0,math.pi/2,0),24)
assets['RingTray_Hero']=list(current);current=[]
# Rounded bracelet saddle with a separate metal pedestal.
box('BraceletBase',(0,0,.009),(.22,.12,.018),stone,.004)
cyl('Pedestal',(0,0,.085),.009,.14,brass,verts=20)
cyl('VelvetSaddle',(0,0,.16),.027,.25,velvet,(0,math.pi/2,0),32)
for x in [-.125,.125]:
 bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=10,radius=1,location=(x,0,.16));o=bpy.context.object;o.name='SaddleEnd';o.scale=(.007,.027,.027);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(velvet)
 for f in o.data.polygons:f.use_smooth=True
 current.append(o)
assets['BraceletStand_Hero']=list(current);current=[]
# Reusable ring with bezel-set gem; sized to an approximately 18 mm inner diameter.
bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=10,location=(0,0,.0105),rotation=(math.pi/2,0,0),major_radius=.010,minor_radius=.0012);o=bpy.context.object;o.name='GoldBand';o.data.materials.append(gold)
for f in o.data.polygons:f.use_smooth=True
current.append(o)
cyl('Bezel',(0,0,.021),.0035,.002,gold,verts=12)
gem=mat('Gemstone',(.75,.87,.92),rough=.035);p=gem.node_tree.nodes.get('Principled BSDF');p.inputs['Transmission Weight'].default_value=.95;p.inputs['IOR'].default_value=2.42
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=.0035,location=(0,0,.0225));o=bpy.context.object;o.name='FacetedStone';o.scale.z=.6;o.data.materials.append(gem);current.append(o)
assets['BezelRing']=list(current);current=[]
report=[]
for name,source_objects in assets.items():
 # Export a compact copy; retain editable individual components in the master.
 objects=[]
 for source in source_objects:
  o=source.copy();o.data=source.data.copy();scene.collection.objects.link(o);objects.append(o)
 minimum=min((o.matrix_world@Vector(p)).z for o in objects for p in o.bound_box)
 for o in objects:o.location.z-=minimum
 transparent=[o for o in objects if o.name.startswith('Glass_')]
 opaque=[o for o in objects if not o.name.startswith('Glass_')]
 bpy.ops.object.select_all(action='DESELECT')
 for o in opaque:o.select_set(True)
 bpy.context.view_layer.objects.active=opaque[0];bpy.ops.object.join();joined=bpy.context.object;joined.name=name+'_Opaque'
 objects=[joined]+transparent
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=False)
 bpy.ops.wm.obj_export(filepath=str(OUT/(name+'.obj')),export_selected_objects=True,forward_axis='NEGATIVE_Z',up_axis='Y',export_materials=True,export_triangulated_mesh=True,path_mode='COPY')
 tris=0
 for o in objects:o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
 report.append({'asset':name,'triangles':tris,'mesh_objects':len(objects),'materials':sorted({m.name for o in objects for m in o.data.materials}),'units':'metres','pivot':'floor centre; Blender Z up / export Y up'})
 for o in objects:bpy.data.objects.remove(o,do_unlink=True)
# Position review objects within case, after exporting each with its own floor pivot.
for o in assets['RingTray_Hero']:o.location+=Vector((-.49,-.025,.711))
for o in assets['BraceletStand_Hero']:o.location+=Vector((.46,.025,.711))
for o in assets['BezelRing']:o.location+=Vector((-.49,-.025,.758))
# Instances of jewelry reuse the same geometry.
for x in [-.59,-.39]:
 for original in assets['BezelRing']:
  o=original.copy();o.data=original.data;scene.collection.objects.link(o);o.location.x+=x+.49
# Studio only; intentionally outside export lists.
box('StudioFloor',(0,0,-.025),(200,200,.04),mat('StudioGrey',(.09,.10,.11),rough=.65),0)
world=bpy.data.worlds.new('StudioWorld');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.2,.23,.28,1);world.node_tree.nodes['Background'].inputs[1].default_value=.35
for name,loc,power,size,target in [('LargeSoftbox',(-2,-2,4),450,3,(0,0,.5)),('Rim',(1.3,1.8,2.4),350,2,(0,0,.8)),('Strip',(-2,1,1.5),150,1.5,(0,0,.6))]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='RECTANGLE';d.size=size;d.size_y=size*.5;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(2.15,-2.7,1.85));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.59))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.lens=54;scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
for im in bpy.data.images:
 if im.source=='FILE': im.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'DisplayCase_ArtReview.blend'))
(OUT/'geometry-report.json').write_text(json.dumps(report,indent=2))
if '--export-only' in sys.argv: sys.exit(0)
scene.render.filepath=str(ROOT/'case-beauty.png');bpy.ops.render.render(write_still=True)
camera.location=(1.25,-1.5,1.34);camera.rotation_euler=(Vector((0,0,.83))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.lens=58;scene.render.filepath=str(ROOT/'case-detail.png');bpy.ops.render.render(write_still=True)
print('ART_PASS_COMPLETE',json.dumps(report))
