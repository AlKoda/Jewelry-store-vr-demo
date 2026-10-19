import bpy,pathlib,json,hashlib
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parent
reports=[]
for path in (root/'Deliverables').glob('*.fbx'):
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 bpy.ops.import_scene.fbx(filepath=str(path))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 points=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
 lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
 tris=0
 for o in meshes:o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
 reports.append({'file':path.name,'triangles_after_fbx_roundtrip':tris,'dimensions_blender_xyz':[round(hi[i]-lo[i],5) for i in range(3)],'min_z':lo[2],'all_object_origins_at_zero':all(o.location.length<.0001 for o in meshes),'all_meshes_have_uv':all(o.data.uv_layers.active is not None for o in meshes),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
(root/'Deliverables/export-validation.json').write_text(json.dumps(reports,indent=2))
print(json.dumps(reports))
