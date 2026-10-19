import bpy,pathlib
from mathutils import Vector
out=pathlib.Path(r'C:/Users/MOBPC/Documents/ChatGPT/Jewelry store/repository/ReferenceAssets/PoliceCar20261017')
bpy.ops.wm.open_mainfile(filepath=str(out/'PoliceCar_Finished.blend'))
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.location.z=-.015
world=bpy.data.worlds.new('Studio');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.3,.3,.3,1);world.node_tree.nodes['Background'].inputs[1].default_value=.5;bpy.context.scene.world=world
for loc,power in [((3,-4,7),1200),((-3,2,5),900)]:
 d=bpy.data.lights.new('Softbox','AREA');d.energy=power;d.shape='DISK';d.size=5;o=bpy.data.objects.new('Softbox',d);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.7))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(5,-7,4));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.8))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=50;s=bpy.context.scene;s.camera=cam;s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.use_denoising=True;s.render.resolution_x=1200;s.render.resolution_y=850;s.render.resolution_percentage=100;s.render.filepath=str(out/'finished.png');bpy.ops.render.render(write_still=True)
