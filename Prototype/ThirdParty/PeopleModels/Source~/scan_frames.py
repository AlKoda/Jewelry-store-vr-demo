import bpy, sys
args = sys.argv[sys.argv.index('--') + 1:]
anim = args[0]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=anim, use_anim=True)
arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
act = arm.animation_data.action
print('range', tuple(act.frame_range))
s, e = int(act.frame_range[0]), int(act.frame_range[1])
for f in range(s, e + 1, max(1, (e - s) // 25)):
    bpy.context.scene.frame_set(f)
    W = arm.matrix_world
    p = lambda n: W @ arm.pose.bones[n].head
    L, R, H = p('Bip01 L Hand'), p('Bip01 R Hand'), p('Bip01 Head')
    print(f, 'L-R dist %.2f' % (L - R).length, 'Lz %.2f Rz %.2f Hz %.2f' % (L.z, R.z, H.z), 'L-head %.2f R-head %.2f' % ((L - H).length, (R - H).length))
