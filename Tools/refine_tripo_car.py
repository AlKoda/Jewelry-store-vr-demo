"""Export the supplied Tripo geometry at real scale, without subdivision or flattening.
Run with Blender 5.2 --background --python Tools/refine_tripo_car.py.
The original atlas is copied byte-for-byte. Surface separation and clean native
typography are applied by the Unity shader, not painted into the source image.
"""
import bpy, json, hashlib, shutil, math, sys
from pathlib import Path
from mathutils import Vector
repo=Path(__file__).resolve().parents[1]
pack=repo/'Prototype/ThirdParty/TripoPoliceCar';pack.mkdir(parents=True,exist_ok=True)
original=pack/'Original~';original.mkdir(exist_ok=True)
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
source=Path(args[0]) if args else original/'police_car.glb'
if hashlib.sha256(source.read_bytes()).hexdigest()!='0a281d6fe793e1cb9456add0979d9fe886e89c5551345ad02e92211695d25543':
    raise ValueError('This fitted-wheel generator is for the October 25 supplied Tripo car; a different source needs refitting.')
if source.resolve()!=(original/'police_car.glb').resolve():shutil.copy2(source,original/'police_car.glb')
if not (original/'police_car.fbx').exists():shutil.copy2(repo/'.asset-cache/tripo-car/police_car_3d_model.fbx',original/'police_car.fbx')
if not (pack/'PatrolOriginal_Color.jpg').exists():shutil.copy2(repo/'.asset-cache/tripo-car/police_car_3d_model.fbm/police_car_3d_model_basecolor.jpg',pack/'PatrolOriginal_Color.jpg')
for name,new in [('PoliceDoorMarkings.png','PatrolDoor_Color.png'),('PoliceArabicPlate.png','PatrolPlate_Color.png')]:
    shutil.copy2(repo/'Prototype/ThirdParty/InvestigationDetail'/name,pack/new)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
mesh=obj.data;mesh.calc_loop_triangles()
# Unity OBJ importer mirrors the X coordinate. Pre-mirror width to keep handedness.
# Original GLB: X length (nose negative), Y width, Z up. Metres: 5.12m long.
def pos(v):
    p=obj.matrix_world@v
    return (-p.y*5.12,p.z*5.12,-p.x*5.12)
def norm(v):
    p=obj.matrix_world.to_3x3()@v
    return (-p.y,p.z,-p.x)
lines=['# Koda supplied Tripo car; uniform 5.12m scale','o PatrolBody']
for v in mesh.vertices: lines.append('v %.9f %.9f %.9f'%pos(v.co))
uv=mesh.uv_layers.active.data
for l in mesh.loops: lines.append('vt %.9f %.9f'%tuple(uv[l.index].uv))
for l in mesh.loops: lines.append('vn %.9f %.9f %.9f'%norm(mesh.corner_normals[l.index].vector))
body=[];allfaces=[]
for tri in mesh.loop_triangles:
    # Axis permutation is orientation-preserving; Unity handles OBJ handedness.
    loops=list(tri.loops)
    face='f '+' '.join(f'{mesh.loops[i].vertex_index+1}/{i+1}/{i+1}' for i in loops)
    allfaces.append(face)
    centre=sum((obj.matrix_world@mesh.vertices[mesh.loops[i].vertex_index].co for i in loops),Vector())/3
    radius=min(math.hypot(centre.x+.326,centre.z-.068),math.hypot(centre.x-.302,centre.z-.068))
    points=[obj.matrix_world@mesh.vertices[mesh.loops[i].vertex_index].co for i in loops]
    within=all(abs(p.y)>.158 and min(math.hypot(p.x+.326,p.z-.068),math.hypot(p.x-.302,p.z-.068))<.072 for p in points)
    if not within:body.append(face)
(pack/'PatrolBody.obj').write_text('\n'.join(lines+body)+'\n')
(pack/'PatrolDistance.obj').write_text('\n'.join(lines+allfaces)+'\n')
# Original wheel triangles are replaced by four matched native assemblies.
# All four wheels share one renderer with four material slots.
verts=[];normals=[];faces=[];materials=[]
def vert(p,n):verts.append(p);normals.append(n);return len(verts)
def face(indices,mat):faces.append(indices);materials.append(mat)
def ring(cx,cy,cz,profile,mat,segments=48):
    start=len(verts)
    for j,(ax,r) in enumerate(profile):
        a0,r0=profile[max(j-1,0)];a1,r1=profile[min(j+1,len(profile)-1)]
        da,dr=a1-a0,r1-r0;den=max(math.hypot(da,dr),1e-8)
        for i in range(segments):
            a=i*math.tau/segments;c,s=math.cos(a),math.sin(a)
            vert((cx+ax,cy+r*c,cz+r*s),(-dr/den,da*c/den,da*s/den))
    for j in range(len(profile)-1):
        for i in range(segments):
            a=start+j*segments+i+1;b=start+j*segments+(i+1)%segments+1;c=b+segments;d=a+segments
            face((a,b,c),mat);face((a,c,d),mat)
def box(cx,cy,cz,sx,sy,sz,angle,mat):
    # Bevelled eight-sided spoke profile, with all end faces closed.
    outline=[(-sy*.5,-sz*.32),(-sy*.32,-sz*.5),(sy*.32,-sz*.5),(sy*.5,-sz*.32),(sy*.5,sz*.32),(sy*.32,sz*.5),(-sy*.32,sz*.5),(-sy*.5,sz*.32)]
    start=len(verts);co,si=math.cos(angle),math.sin(angle)
    for x in (-sx*.5,sx*.5):
        for y,z in outline:vert((cx+x,cy+y*co-z*si,cz+y*si+z*co),(1 if x>0 else -1,0,0))
    for i in range(1,7):face((start+1,start+i+2,start+i+1),mat);face((start+9,start+i+9,start+i+10),mat)
    for i in range(8):
        a=start+i+1;b=start+(i+1)%8+1;c=b+8;d=a+8
        # Use true face normals on side bevels by duplicating vertices.
        pp=[Vector(verts[k-1]) for k in (a,b,c,d)];nn=(pp[1]-pp[0]).cross(pp[2]-pp[0]).normalized();ii=[vert(tuple(p),tuple(nn)) for p in pp]
        face((ii[0],ii[1],ii[2]),mat);face((ii[0],ii[2],ii[3]),mat)
for sign in (-1,1):
 for z in (1.66912,-1.54624):
    x=sign*.912;y=.34816
    tyre=[(-.125,.231),(-.127,.285),(-.112,.330),(-.085,.348),(-.062,.351),(-.057,.345),(-.051,.351),(-.02,.352),(-.015,.346),(-.009,.352),(.024,.352),(.030,.346),(.036,.352),(.067,.351),(.073,.345),(.079,.350),(.103,.337),(.122,.30),(.125,.231)]
    ring(x,y,z,tyre,'Rubber')
    outside=x+sign*.128
    ring(outside,y,z,[(-.012,.209),(-.012,.226),(0,.232),(.012,.226),(.012,.209)],'Alloy')
    ring(outside-sign*.032,y,z,[(-.004,.026),(-.004,.174),(.004,.174),(.004,.026)],'Brake')
    ring(outside+sign*.006,y,z,[(-.018,0),(-.018,.049),(.008,.051),(.015,.044),(.015,0)],'Alloy',32)
    for i in range(10):
        a=i*math.tau/10;r=.132
        box(outside,y+r*math.cos(a),z+r*math.sin(a),.024,.168,.024,a,'Alloy')
    for i in range(5):
        a=i*math.tau/5;r=.034
        ring(outside+sign*.017,y+r*math.cos(a),z+r*math.sin(a),[(-.003,0),(-.003,.006),(.003,.006),(.003,0)],'DarkMetal',8)
    box(outside-sign*.028,y+.10,z+.114,.048,.085,.046,.22,'DarkMetal')
wheel=['# Original project-made tyre, rim, rotor, caliper and wheel nut assemblies','mtllib PatrolWheels.mtl','o PatrolWheels']
for p in verts:wheel.append('v %.8f %.8f %.8f'%p)
for n in normals:wheel.append('vn %.8f %.8f %.8f'%n)
last=None
for ff,mat in zip(faces,materials):
    if mat!=last:wheel.append('usemtl '+mat);last=mat
    wheel.append('f '+' '.join(f'{i}//{i}' for i in ff))
(pack/'PatrolWheels.obj').write_text('\n'.join(wheel)+'\n')
(pack/'PatrolWheels.mtl').write_text('\n'.join(f'newmtl {n}\nKd .3 .3 .3\n' for n in ['Rubber','Alloy','Brake','DarkMetal']))
metadata=dict(source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),source_triangles=len(mesh.loop_triangles),body_triangles=len(body),wheel_triangles=len(faces),source_vertices=len(mesh.vertices),length_metres=5.12,geometry_changes='Body: uniform scale and axis conversion, original wheel faces removed for new wheel assemblies. Original body corner normals and UVs retained. No smoothing modifier, remesh, subdivision or flattening. Distance mesh retains all original 15000 triangles.')
(pack/'geometry.json').write_text(json.dumps(metadata,indent=2))
print(json.dumps(metadata))
