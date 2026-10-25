"""Export the supplied Tripo geometry at real scale, without subdivision or flattening.
Run with Blender 5.2 --background --python Tools/refine_tripo_car.py.
The original atlas is copied byte-for-byte. Surface separation and clean native
typography are applied by the Unity shader, not painted into the source image.
"""
import bpy, json, hashlib, shutil, math, sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
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
    ring(x,y,z,tyre,'Rubber',64)
    outside=x+sign*.128
    ring(outside,y,z,[(-.012,.209),(-.012,.226),(0,.232),(.012,.226),(.012,.209)],'Alloy')
    # Closed rim barrel: the open spaces around the brake disc must reveal a
    # deep wheel interior, not the road through a paper-thin decorative rim.
    inner=min(0,-sign*.17);outer=max(0,-sign*.17)
    ring(outside,y,z,[(inner,.205),(inner,.212),(outer,.212),(outer,.205),(inner,.205)],'DarkMetal')
    ring(outside-sign*.032,y,z,[(-.004,.026),(-.004,.174),(.004,.174),(.004,.026)],'Brake')
    ring(outside+sign*.006,y,z,[(-.018,0),(-.018,.038),(.008,.040),(.015,.036),(.015,0)],'Alloy',32)
    # Ten split pairs: swept/tapered spokes with a narrow machined face and
    # bevelled shoulders, instead of rectangular bars.
    for i in range(10):
     for split in (-1,1):
        ids=[]
        for radius,width,spread in [(.052,.012,.018),(.135,.011,.027),(.222,.006,.039)]:
            angle=i*math.tau/10+split*spread;co,si=math.cos(angle),math.sin(angle)
            ringids=[]
            for ax,tangent in [(-.014,-width*.5),(-.010,-width*.5),(.008,-width*.32),(.011,width*.0),(.008,width*.32),(-.010,width*.5),(-.014,width*.5),(-.016,0)]:
                xx=outside+sign*ax
                ringids.append(vert((xx,y+radius*co-tangent*si,z+radius*si+tangent*co),(sign,0,0)))
            ids.append(ringids)
        # Split corners preserve the narrow bevel highlight.
        for j in range(2):
         for k in range(8):
            q=[ids[j][k],ids[j][(k+1)%8],ids[j+1][(k+1)%8],ids[j+1][k]]
            pp=[Vector(verts[t-1]) for t in q];nn=(pp[1]-pp[0]).cross(pp[2]-pp[0]).normalized()
            if sign<0:nn=-nn
            vv=[vert(tuple(p),tuple(nn)) for p in pp]
            if sign>0:face((vv[0],vv[1],vv[2]),'Alloy');face((vv[0],vv[2],vv[3]),'Alloy')
            else:face((vv[2],vv[1],vv[0]),'Alloy');face((vv[3],vv[2],vv[0]),'Alloy')
        for j in (0,2):
         for k in range(1,7):
            f=(ids[j][0],ids[j][k],ids[j][k+1]);face(f if (j==2)==(sign>0) else f[::-1],'Alloy')
    for i in range(5):
        a=i*math.tau/5;r=.034
        ring(outside+sign*.017,y+r*math.cos(a),z+r*math.sin(a),[(-.003,0),(-.003,.006),(.003,.006),(.003,0)],'Alloy',8)
    # Small dark hub insert and a raised three-point emblem, as on the reference.
    ring(outside+sign*.024,y,z,[(-.002,0),(-.002,.027),(.002,.027),(.002,0)],'DarkMetal',24)
    for i in range(3):
        a=i*math.tau/3;r=.012
        box(outside+sign*.028,y+r*math.cos(a),z+r*math.sin(a),.002,.025,.0025,a,'Alloy')
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

# Thin optical trim fitted by ray projection against the actual source body.
# Each tube follows the body at 1.8mm: no free-floating cards or flat light quads.
detail_verts=[];detail_normals=[];detail_faces=[];detail_materials=[]
positions=[obj.matrix_world@v.co for v in mesh.vertices]
tree=BVHTree.FromPolygons(positions,[t.vertices for t in mesh.loop_triangles],all_triangles=True)
def on_front(side,up):
    hit,n,_,_=tree.ray_cast(Vector((-1,side,up)),Vector((1,0,0)),2)
    if hit is None:raise ValueError('Optical trim missed body')
    q=hit+n*(.0018/5.12)
    return Vector((-q.y*5.12,q.z*5.12,-q.x*5.12))
def projector(side,up):
    hit,n,_,_=tree.ray_cast(Vector((-1,side,up)),Vector((1,0,0)),2)
    if hit is None:raise ValueError('Projector missed headlamp')
    p=Vector((-hit.y*5.12,hit.z*5.12,-hit.x*5.12));axis=Vector((-n.y,n.z,-n.x)).normalized()
    a=axis.cross(Vector((0,1,0))).normalized();b=axis.cross(a).normalized()
    for profile,mat in [([(-.002,.033),(0,.037),(.003,.037),(.005,.033)],'SatinChrome'),([(.002,.032),(.005,.027),(.008,.014),(.009,0)],'ProjectorLens')]:
        start=len(detail_verts);segments=24
        for j,(ax,r) in enumerate(profile):
            left=profile[max(0,j-1)];right=profile[min(len(profile)-1,j+1)];da=right[0]-left[0];dr=right[1]-left[1]
            for k in range(segments):
                t=k*math.tau/segments;radial=a*math.cos(t)+b*math.sin(t);nn=(radial*da-axis*dr).normalized()
                detail_verts.append(tuple(p+axis*ax+radial*r));detail_normals.append(tuple(nn))
        for j in range(len(profile)-1):
         for k in range(segments):
            aa=start+j*segments+k+1;bb=start+j*segments+(k+1)%segments+1;cc=bb+segments;dd=aa+segments
            detail_faces.extend([(aa,bb,cc),(aa,cc,dd)]);detail_materials.extend([mat,mat])
def tube(points,radius,material,closed=False):
    if closed:
        if (points[-1]-points[0]).length<1e-8:points=points[:-1]
        points=points+[points[0]]
    start=len(detail_verts);sides=6
    for i,p in enumerate(points):
        tangent=(points[min(i+1,len(points)-1)]-points[max(i-1,0)]).normalized()
        normal=tangent.cross(Vector((0,0,1))).normalized()
        if normal.length<.01:normal=tangent.cross(Vector((0,1,0))).normalized()
        other=tangent.cross(normal).normalized()
        for k in range(sides):
            a=k*math.tau/sides;n=normal*math.cos(a)+other*math.sin(a)
            detail_verts.append(tuple(p+n*radius));detail_normals.append(tuple(n))
    for i in range(len(points)-1):
     for k in range(sides):
        a=start+i*sides+k+1;b=start+i*sides+(k+1)%sides+1;c=b+sides;d=a+sides
        detail_faces.extend([(a,b,c),(a,c,d)]);detail_materials.extend([material,material])
    if not closed:
     for end in (0,len(points)-1):
      for k in range(1,sides-1):
        f=(start+end*sides+1,start+end*sides+k+1,start+end*sides+k+2)
        detail_faces.append(f if end else f[::-1]);detail_materials.append(material)
def fitted_path(coords,sign):
    points=[]
    for i in range(len(coords)-1):
        a,b=Vector(coords[i]),Vector(coords[i+1])
        for k in range(5):
            p=a.lerp(b,k/5);points.append(on_front(p.x*sign,p.y))
    points.append(on_front(coords[-1][0]*sign,coords[-1][1]));return points
for sign in (-1,1):
    for side,up in [(.134,.139),(.159,.139)]:projector(side*sign,up)
    # A restrained DRL/light-guide detail, under the existing textured lens.
    guide=[(.123,.127),(.143,.124),(.160,.129),(.168,.136)]
    tube(fitted_path(guide,sign),.0019,'LightGuide')
# Fit every optical vertex to the original curved housing. A plane fitted only
# at its centre can float over a faceted single-image reconstruction at the rim.
for i,(x,y,z) in enumerate(detail_verts):
    point=Vector((-z/5.12,-x/5.12,y/5.12))
    hit,n,_,_=tree.ray_cast(Vector((-1,point.y,point.z)),Vector((1,0,0)),2)
    if hit is None:raise ValueError('Optical vertex missed the headlamp housing')
    gap=max(.0015,min(.009,(point-hit).dot(n)*5.12))
    p=hit+n*(gap/5.12)
    detail_verts[i]=(-p.y*5.12,p.z*5.12,-p.x*5.12)
detail=['# Body-conforming headlamp rim/light-guide trim','mtllib PatrolOptics.mtl','o PatrolOptics']
for p in detail_verts:detail.append('v %.8f %.8f %.8f'%p)
for n in detail_normals:detail.append('vn %.8f %.8f %.8f'%n)
last=None
for f,m in zip(detail_faces,detail_materials):
    if m!=last:detail.append('usemtl '+m);last=m
    detail.append('f '+' '.join(f'{i}//{i}' for i in f))
(pack/'PatrolOptics.obj').write_text('\n'.join(detail)+'\n')
(pack/'PatrolOptics.mtl').write_text('newmtl SatinChrome\nKd .5 .5 .5\nnewmtl LightGuide\nKd .6 .6 .6\nnewmtl ProjectorLens\nKd .04 .05 .06\n')
metadata['optics_triangles']=len(detail_faces)
distances=[]
for x,y,z in detail_verts:
    point=Vector((-z/5.12,-x/5.12,y/5.12));nearest=tree.find_nearest(point)
    distances.append(nearest[3]*5.12)
metadata['optics_max_surface_distance_m']=max(distances)
if max(distances)>.016:raise ValueError('Headlamp detail is floating more than 16mm from its fitted housing')
(pack/'geometry.json').write_text(json.dumps(metadata,indent=2))
print('OPTICS',len(detail_faces),'WHEELS',len(faces))
