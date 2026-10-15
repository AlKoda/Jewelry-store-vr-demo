"""Render OBJ figures side by side with Cycles (CPU) for visual checks.
    python -I render_preview.py -- out.png yaw_deg obj1 [obj2 ...]
"""
import bpy, sys, math, os
args = sys.argv[sys.argv.index('--') + 1:]
out, yaw, objs = args[0], float(args[1]), args[2:]
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
x = 0.0
maxh = 0
for i, path in enumerate(objs):
    before = set(bpy.data.objects)
    bpy.ops.wm.obj_import(filepath=path, forward_axis='NEGATIVE_Z', up_axis='Y')
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        o.location.x += x
        o.rotation_euler.z += math.radians(yaw)
        maxh = max(maxh, o.dimensions.z)
        for m in o.data.materials:
            if m and m.node_tree:
                b = m.node_tree.nodes.get('Principled BSDF')
                if b:
                    # alpha-tested PNG maps (hair cards): wire the texture alpha like the cutout shader would
                    for n in m.node_tree.nodes:
                        if n.type == 'TEX_IMAGE' and n.image and n.image.filepath.lower().endswith('.png'):
                            m.node_tree.links.new(n.outputs['Alpha'], b.inputs['Alpha'])
                    b.inputs['Roughness'].default_value = 0.6
                    if 'Specular IOR Level' in b.inputs: b.inputs['Specular IOR Level'].default_value = 0.3
    x += 1.0
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
sc.collection.objects.link(cam)
cx = (x - 1.0) / 2
cam.data.type = 'ORTHO'
cam.data.ortho_scale = max(x, 2.0)
cam.location = (cx, -6, 1.0)
cam.rotation_euler = (math.radians(90), 0, 0)
sc.camera = cam
sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN'))
sun.data.energy = 3.0
sun.rotation_euler = (math.radians(50), math.radians(20), math.radians(-30))
sc.collection.objects.link(sun)
w = bpy.data.worlds.new('w'); sc.world = w
w.use_nodes = True
w.node_tree.nodes['Background'].inputs[0].default_value = (0.35, 0.37, 0.4, 1)
w.node_tree.nodes['Background'].inputs[1].default_value = 0.8
sc.render.engine = 'CYCLES'
sc.cycles.device = 'CPU'
sc.cycles.samples = 24
sc.render.resolution_x = int(420 * x)
sc.render.resolution_y = 840
focus = os.environ.get('FOCUS')
if focus:
    fx, fy, fz, fs = (float(t) for t in focus.split(','))
    a = math.radians(yaw)
    bx, by, bz = fx, -fz, fy
    bx, by = bx * math.cos(a) - by * math.sin(a), bx * math.sin(a) + by * math.cos(a)
    cam.location = (bx, -6, bz)
    cam.data.ortho_scale = fs
    sc.render.resolution_x = sc.render.resolution_y = 600
sc.render.filepath = out
bpy.ops.render.render(write_still=True)
