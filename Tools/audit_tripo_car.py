import bpy, json, math
from pathlib import Path
from mathutils import Vector
base=Path(__file__).resolve().parents[1]
out=base/'.asset-cache/tripo-car'
out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(base/'Prototype/ThirdParty/TripoPoliceCar/Original~/police_car.glb'))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
report=[]
for o in objects:
    bpy.context.view_layer.objects.active=o
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    pts=[o.matrix_world@v.co for v in o.data.vertices]
    report.append(dict(name=o.name,vertices=len(o.data.vertices),faces=len(o.data.polygons),min=[min(p[i] for p in pts) for i in range(3)],max=[max(p[i] for p in pts) for i in range(3)],materials=[m.name for m in o.data.materials]))
    for m in o.data.materials:
        bs=m.node_tree.nodes.get('Principled BSDF')
        if bs: bs.inputs['Roughness'].default_value=.36
for im in bpy.data.images:
    if im.source=='FILE' or im.packed_file:
        im.filepath_raw=str(out/(im.name+'.png'));im.file_format='PNG';im.save()
(out/'audit.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16
scene.render.resolution_x=1200;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.18,.18,.18,1)
points=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
centre=(lo+hi)/2;span=max(hi-lo)
for at,power,size in [((2,-3,4),900,4),((-2,1,3),700,3),((0,3,4),1200,4)]:
    bpy.ops.object.light_add(type='AREA',location=centre+Vector(at)*span*.5);l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=size;l.rotation_euler=(centre-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=span*1.30
for i,d in enumerate([(3,-5,2),(0,-5,.7),(-3,5,2),(0,0,5)]):
    cam.location=centre+Vector(d).normalized()*span*1.5;cam.rotation_euler=(centre-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(out/f'original-{i}.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'source.blend'))
