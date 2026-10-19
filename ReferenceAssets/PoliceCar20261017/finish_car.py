import bpy,pathlib,json,bmesh
from mathutils import Vector
root=pathlib.Path(r'C:/Users/MOBPC/Documents/ChatGPT/Jewelry store/repository/ReferenceAssets/PoliceCar20261017');out=root/'Deliverables';out.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(root/'PoliceCar.blend'))
def mat(name,c,metal=0,rough=.4):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough;return m
white=mat('CarPaintWhite',(.65,.68,.7),.35,.28);navy=mat('CarPaintNavy',(.018,.027,.045),.4,.28);rubber=mat('CarRubber',(.012,.014,.017),0,.72);chrome=mat('CarChrome',(.36,.38,.4),.9,.25);glass=mat('CarWindow',(.012,.02,.028),.05,.12);lamp=mat('CarHeadlamp',(.64,.61,.51),.55,.22);red=mat('CarTailLens',(.22,.008,.006),.1,.3)
materials=[white,navy,rubber,chrome,glass,lamp,red]
for o in list(bpy.context.scene.objects):
 if o.type!='MESH':continue
 idx=int(o.name.split('_')[-1]);
 if idx==0:
  bm=bmesh.new();bm.from_mesh(o.data)
  for axis,level in [(2,.55),(2,.75),(2,1.05),(1,-.35),(1,1.2)]:
   no=Vector((1 if axis==0 else 0,1 if axis==1 else 0,1 if axis==2 else 0));co=no*level;bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=co,plane_no=no,clear_inner=False,clear_outer=False)
  bm.to_mesh(o.data);bm.free();o.data.update()
 o.data.materials.clear()
 for m in materials:o.data.materials.append(m)
 if idx==4: # keep the physical housing; the existing animated light-bar lenses sit above it
  default=2
 elif idx in [1,2,3,5]:default=2
 elif idx in [14,15,16,17,19,37,38,40,42,43,44,45]:default=3
 elif idx in [13,25,39,47]:default=4
 elif idx in [8,20,30,31,33,34]:default=5
 elif idx in [12,29,48,50]:default=6
 elif idx in [6,7,10,21,22,24,26,27,28,32,35,36,51] or idx>51:default=2
 else:default=0
 for p in o.data.polygons:
  p.material_index=default;p.use_smooth=True
  if idx==0:
   c=p.center
   if .55<c.z<.75 and abs(c.x)>.83:p.material_index=1
   if -.35<c.y<1.2 and 1.05<c.z<1.43 and abs(c.x)>.68 and abs(p.normal.x)>.35:p.material_index=4
# Thin fitted window covers suppress self-intersecting glass in the supplied AI mesh.
def cover(name,verts,reverse=False):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],[tuple(reversed(range(4))) if reverse else (0,1,2,3)]);mesh.materials.append(glass);obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
cover('WindshieldCover',[(-.75,-.99,1.055),(.75,-.99,1.055),(.66,-.14,1.47),(-.66,-.14,1.47)])
for sign in [-1,1]:
 cover('FrontSideWindow',[(sign*.945,-.42,1.055),(sign*.945,.35,1.055),(sign*.715,.35,1.435),(sign*.715,-.15,1.435)],sign<0)
 cover('RearSideWindow',[(sign*.94,.44,1.08),(sign*.87,1.12,1.14),(sign*.665,.93,1.435),(sign*.715,.44,1.435)],sign<0)
bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=next(o for o in bpy.context.scene.objects if o.type=='MESH');bpy.ops.object.join();car=bpy.context.object;car.name='UserPoliceCar'
# Blender -Y nose exports Unity +Z through the OBJ importer with NEGATIVE_Z forward.
for suffix,ratio in [('',1),('_LOD1',.55),('_LOD2',.25)]:
 dup=car.copy();dup.data=car.data.copy();bpy.context.collection.objects.link(dup);bpy.ops.object.select_all(action='DESELECT');dup.select_set(True);bpy.context.view_layer.objects.active=dup
 if ratio<1:
  m=dup.modifiers.new('LOD reduction','DECIMATE');m.ratio=ratio;bpy.ops.object.modifier_apply(modifier=m.name)
 bpy.ops.wm.obj_export(filepath=str(out/('UserPoliceCar'+suffix+'.obj')),export_selected_objects=True,forward_axis='NEGATIVE_Z',up_axis='Y',export_triangulated_mesh=True,export_materials=True)
 bpy.data.objects.remove(dup,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'PoliceCar_Finished.blend'))
print('CAR_EXPORT_COMPLETE')
