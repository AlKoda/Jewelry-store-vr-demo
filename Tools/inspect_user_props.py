"""Blender background inventory and neutral renders of the user's original FBX files."""
import bpy, json, pathlib
from mathutils import Vector

root=pathlib.Path(__file__).resolve().parents[1]
folder=root/'ReferenceAssets'/'UserProps'
report=[]
for index,path in enumerate(sorted(folder.glob('*.fbx'))):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    report.append({'file':path.name,'objects':[{'name':o.name,'vertices':len(o.data.vertices),'polygons':len(o.data.polygons),'dimensions':list(o.dimensions)} for o in meshes]})
    points=[o.matrix_world@Vector(p) for o in meshes for p in o.bound_box]
    low=Vector([min(p[i] for p in points) for i in range(3)])
    high=Vector([max(p[i] for p in points) for i in range(3)])
    center=(low+high)*.5; span=max(high-low)
    material=bpy.data.materials.new('Neutral clay');material.diffuse_color=(.45,.50,.55,1)
    for o in meshes:
        o.data.materials.clear();o.data.materials.append(material)
    bpy.ops.object.camera_add(location=center+Vector((1.3,-2,1.6))*span)
    camera=bpy.context.object;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO';camera.data.ortho_scale=span*1.45
    scene=bpy.context.scene;scene.camera=camera
    scene.render.engine='BLENDER_WORKBENCH'
    scene.display.shading.light='STUDIO';scene.display.shading.studiolight_rotate_z=.5
    scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True
    scene.display.shading.show_cavity=True;scene.display.shading.cavity_type='BOTH'
    scene.display.shading.background_type='VIEWPORT';scene.display.shading.background_color=(.12,.12,.12)
    scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
    scene.render.filepath=str(folder/f'inspection-{index}.png')
    bpy.ops.render.render(write_still=True)
(folder/'inventory.json').write_text(json.dumps(report,indent=2))
