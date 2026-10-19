import bpy,bmesh,json,pathlib,hashlib
from mathutils import Vector
r=pathlib.Path(__file__).resolve().parent/'Deliverables';rows=[]
for path in sorted(r.glob('*.fbx')):
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.fbx(filepath=str(path))
 objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];tri=0;nonmanifold=0;zero=0;minz=1e9
 for o in objects:
  o.data.calc_loop_triangles();tri+=len(o.data.loop_triangles)
  bm=bmesh.new();bm.from_mesh(o.data);nonmanifold+=sum(not e.is_manifold for e in bm.edges);zero+=sum(f.calc_area()<1e-12 for f in bm.faces);bm.free()
  minz=min(minz,min((o.matrix_world@v.co).z for v in o.data.vertices))
 row={'file':path.name,'triangles':tri,'nonmanifold_edges':nonmanifold,'degenerate_faces':zero,'has_uv':all(o.data.uv_layers.active is not None for o in objects),'origin_zero':all(o.location.length<1e-5 for o in objects),'min_z':minz,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()};rows.append(row)
 assert nonmanifold==0 and zero==0 and row['has_uv'] and row['origin_zero'],row
(r/'validation.json').write_text(json.dumps(rows,indent=2));print('PASS',len(rows),'FBX files, closed solids, UVs and zero origins')
