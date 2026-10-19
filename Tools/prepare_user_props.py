"""Regroup inspected components, set metre scale and floor pivots, export Unity OBJ."""
import bpy,json,pathlib,math
from mathutils import Vector,Matrix
root=pathlib.Path(__file__).resolve().parents[1]
source=root/'ReferenceAssets'/'UserProps'
output=root/'Prototype'/'ThirdParty'/'UserProps';output.mkdir(parents=True,exist_ok=True)
# Set index follows the alphabetically sorted original filenames. Axis is Blender Z-up.
specs=[('camera',0,[2,6],0,.15,.004),('flashlight',0,[5],2,.24,.002),
       ('gloves',2,[0],1,.25,.002),('cone',1,[32],2,.45,.002),
       ('evidence_marker',2,[48],2,.12,.001),('equipment_case',0,[10,23],0,.34,.002),
       ('canister',0,[9],2,.42,.003),('sofa',1,[4],0,1.8,.008)]
manifest=[]
for name,index,ids,axis,size,padding in specs:
    bpy.ops.wm.open_mainfile(filepath=str(source/f'separated-{index}.blend'))
    anchors=[bpy.data.objects[f'set{index}_part{i:02d}'] for i in ids]
    points=[o.matrix_world@Vector(p) for o in anchors for p in o.bound_box]
    lo=Vector([min(p[i] for p in points)-padding for i in range(3)])
    hi=Vector([max(p[i] for p in points)+padding for i in range(3)])
    chosen=[]
    for o in bpy.context.scene.objects:
        if o.type!='MESH':continue
        pts=[o.matrix_world@Vector(p) for p in o.bound_box]
        center=sum(pts,Vector())/8
        # Keep small disconnected buttons/handles within the selected object's volume.
        if o in anchors or (all(lo[i]<=center[i]<=hi[i] for i in range(3)) and max(o.dimensions)<max(hi-lo)):
            chosen.append(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in chosen:o.select_set(True)
    bpy.context.view_layer.objects.active=anchors[0]
    component_names=[o.name for o in chosen]
    bpy.ops.object.join();obj=bpy.context.object;obj.name=name
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    if name=="gloves":
        for v in obj.data.vertices:v.co=Matrix.Rotation(math.pi/2,4,"X")@v.co
    coords=[v.co.copy() for v in obj.data.vertices]
    low=Vector([min(p[i] for p in coords) for i in range(3)])
    high=Vector([max(p[i] for p in coords) for i in range(3)])
    origin=Vector(((low.x+high.x)/2,(low.y+high.y)/2,low.z))
    factor=size/(high[axis]-low[axis])
    for v in obj.data.vertices:v.co=(v.co-origin)*factor
    obj.data.materials.clear()
    for poly in obj.data.polygons:poly.use_smooth=True
    bpy.ops.wm.obj_export(filepath=str(output/f'{name}.obj'),export_selected_objects=True,
        forward_axis='NEGATIVE_Z',up_axis='Y',export_materials=False,export_triangulated_mesh=True)
    obj.data.calc_loop_triangles()
    manifest.append({'name':name,'source_set':index,'components':component_names,
        'target_axis':'XYZ'[axis],'target_metres':size,'triangles':len(obj.data.loop_triangles),
        'dimensions_metres':list((high-low)*factor)})
    # Keep a curated editable source with only this asset selected, not the source collage.
    for other in list(bpy.context.scene.objects):
        if other!=obj:bpy.data.objects.remove(other,do_unlink=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(source/f'{name}.blend'))
(output/'manifest.json').write_text(json.dumps(manifest,indent=2))
