import bpy,json,pathlib,math
from mathutils import Vector,Matrix
out=pathlib.Path(r'C:/Users/MOBPC/Documents/ChatGPT/Jewelry store/repository/ReferenceAssets/PoliceCar20261017')
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(out/'Original.glb'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)]);scale=4.7/(hi.y-lo.y)
transforms={o:o.matrix_world.copy() for o in meshes}
for o in meshes:
 mw=transforms[o];o.data=o.data.copy();o.parent=None;o.matrix_world=mw
 for v in o.data.vertices:v.co=(mw@v.co-Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z)))*scale
 o.matrix_world=Matrix.Identity(4)
report=[]
for o in meshes:
 o.data.calc_loop_triangles();report.append({'name':o.name,'triangles':len(o.data.loop_triangles),'min':[min(v.co[i] for v in o.data.vertices) for i in range(3)],'max':[max(v.co[i] for v in o.data.vertices) for i in range(3)]})
(out/'inspection.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(out/'PoliceCar.blend'))
mat=bpy.data.materials.new('InspectionGrey');mat.diffuse_color=(.32,.36,.4,1)
for o in meshes:o.data.materials.clear();o.data.materials.append(mat)
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.location.z=-.015
world=bpy.data.worlds.new('Studio');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.3,.3,.3,1);world.node_tree.nodes['Background'].inputs[1].default_value=.5;bpy.context.scene.world=world
for loc,power in [((3,-4,7),1200),((-3,2,5),900)]:
 d=bpy.data.lights.new('Softbox','AREA');d.energy=power;d.shape='DISK';d.size=5;o=bpy.data.objects.new('Softbox',d);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.7))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(5,-7,4));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.8))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=50;s=bpy.context.scene;s.camera=cam;s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.use_denoising=True;s.render.resolution_x=1200;s.render.resolution_y=850;s.render.resolution_percentage=100;s.render.filepath=str(out/'inspection.png');bpy.ops.render.render(write_still=True)
print('CAR_SUMMARY',len(meshes),sum(x['triangles'] for x in report),'dimensions',list((hi-lo)*scale))
