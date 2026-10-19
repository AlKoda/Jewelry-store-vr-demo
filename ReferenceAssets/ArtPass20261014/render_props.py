import bpy,pathlib
from mathutils import Vector
r=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(r/'DisplayCase_ArtReview.blend'))
keep=('Tray','SuedeRoll','VelvetSaddle','SaddleEnd','Pedestal','BraceletBase','GoldBand','Bezel','FacetedStone','StudioFloor')
for o in bpy.context.scene.objects:
 if o.type=='MESH' and not o.name.startswith(keep):o.hide_render=True
bpy.data.objects['StudioFloor'].location.z=.691
c=bpy.context.scene.camera;c.location=(.65,-1.55,1.6);c.rotation_euler=(Vector((-.05,0,.78))-c.location).to_track_quat('-Z','Y').to_euler();c.data.lens=45
bpy.context.scene.render.filepath=str(r/'props-detail.png');bpy.context.scene.cycles.samples=48
bpy.ops.render.render(write_still=True)
