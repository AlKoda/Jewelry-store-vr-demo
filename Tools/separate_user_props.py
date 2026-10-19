import bpy, json, pathlib, math
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[1]
folder=root/'ReferenceAssets'/'UserProps'
all_reports=[]
for index,path in enumerate(sorted(folder.glob('*.fbx'))):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path))
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
    parts=sorted([o for o in bpy.context.scene.objects if o.type=='MESH'],key=lambda o:len(o.data.polygons),reverse=True)
    report=[]
    for number,o in enumerate(parts):
        o.name=f'set{index}_part{number:02d}'
        points=[o.matrix_world@Vector(p) for p in o.bound_box]
        low=Vector([min(p[i] for p in points) for i in range(3)])
        high=Vector([max(p[i] for p in points) for i in range(3)])
        report.append({'name':o.name,'polygons':len(o.data.polygons),'min':list(low),'max':list(high)})
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/f'separated-{index}.blend'))
    major=[o for o in parts if len(o.data.polygons)>80]
    for o in parts:
        if o not in major: o.hide_render=True
    for number,o in enumerate(major):
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY',center='BOUNDS')
        span=max(o.dimensions);o.scale*=1/span
        o.location=Vector(((number%6)*1.7,0,-(number//6)*1.7))
        mat=bpy.data.materials.new('Clay');mat.diffuse_color=(.55,.59,.63,1)
        o.data.materials.clear();o.data.materials.append(mat)
        bpy.ops.object.text_add(location=o.location+Vector((-.65,-.6,-.72)),rotation=(math.pi/2,0,0))
        text=bpy.context.object;text.data.body=o.name;text.data.size=.15
    rows=math.ceil(len(major)/6)
    center=Vector((4.25,0,-(rows-1)*.85))
    bpy.ops.object.camera_add(location=center+Vector((0,-18,5)))
    camera=bpy.context.object;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO';camera.data.ortho_scale=max(11,rows*1.8)
    scene=bpy.context.scene;scene.camera=camera;scene.render.engine='BLENDER_WORKBENCH'
    scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL'
    scene.display.shading.show_cavity=True;scene.display.shading.cavity_type='BOTH'
    scene.display.shading.background_type='VIEWPORT';scene.display.shading.background_color=(.08,.08,.08)
    scene.render.resolution_x=2200;scene.render.resolution_y=max(700,rows*350);scene.render.resolution_percentage=100
    scene.render.filepath=str(folder/f'parts-{index}.png');bpy.ops.render.render(write_still=True)
    all_reports.append({'file':path.name,'parts':report})
(folder/'parts.json').write_text(json.dumps(all_reports,indent=2))
