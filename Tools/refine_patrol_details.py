"""Fit native parked windshield wipers to the unmodified supplied car surface."""
import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
root=Path(__file__).resolve().parents[1];pack=root/'Prototype/ThirdParty/TripoPoliceCar'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(pack/'Original~/police_car.glb'))
ob=next(o for o in bpy.data.objects if o.type=='MESH');ob.data.calc_loop_triangles()
def converted(v):
 p=ob.matrix_world@v;return Vector((-p.y*5.12,p.z*5.12,-p.x*5.12))
source=[converted(v.co) for v in ob.data.vertices]
tree=BVHTree.FromPolygons(source,[tuple(t.vertices) for t in ob.data.loop_triangles],all_triangles=True)
verts=[];normals=[];faces=[];contacts=[]
def seat(x,z):
 p,n,i,d=tree.ray_cast(Vector((x,3,z)),Vector((0,-1,0)))
 if p is None:raise ValueError('Wiper surface ray missed')
 contacts.append(tuple(p));return p+n*.003,n
def segment(a,b,n,width,thick):
 along=(b-a).normalized();across=along.cross(n).normalized()*width*.5;up=n*thick*.5
 points=[a-across-up,a+across-up,a+across+up,a-across+up,b-across-up,b+across-up,b+across+up,b-across+up]
 for quad in [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]:
  pp=[points[i] for i in quad];normal=(pp[1]-pp[0]).cross(pp[2]-pp[0]).normalized();start=len(verts)
  verts.extend(pp);normals.extend([normal]*4);faces.extend([(start+1,start+2,start+3),(start+1,start+3,start+4)])
for side in [-1,1]:
 points=[]
 for i in range(9):
  t=i/8;x=side*(.07+.53*t);z=1.135-.025*(1-(2*t-1)**2);points.append(seat(x,z))
 for (a,n),(b,m) in zip(points,points[1:]):segment(a,b,(n+m).normalized(),.010,.004)
 p,n=seat(side*.34,1.235);q,m=seat(side*.39,1.12);segment(p+n*.003,q+m*.003,(n+m).normalized(),.009,.006)
out=['# Native fitted windshield wipers; original body unchanged','o ParkedWipers']
out+=['v %.8f %.8f %.8f'%tuple(p) for p in verts];out+=['vn %.8f %.8f %.8f'%tuple(n) for n in normals]
out+=['f '+' '.join('%d//%d'%(i,i) for i in f) for f in faces]
(pack/'PatrolWipers.obj').write_text('\n'.join(out)+'\n')
(pack/'wiper-geometry.json').write_text(json.dumps({'triangles':len(faces),'contact_points':contacts,'surface_offset_m':.003,'original_body_modified':False},indent=2))
print('PATROL_WIPERS_EXPORTED',len(faces),'triangles; source surface contacts',len(contacts))
