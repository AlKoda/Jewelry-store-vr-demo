import bpy,pathlib,json
r=pathlib.Path(__file__).resolve().parent/'Deliverables'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(r/'BezelRing.fbx'))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in objects:
 bpy.context.view_layer.objects.active=o
 m=o.modifiers.new('Distance reduction','DECIMATE');m.ratio=.35;bpy.ops.object.modifier_apply(modifier=m.name)
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(r/'BezelRing_LOD1.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
