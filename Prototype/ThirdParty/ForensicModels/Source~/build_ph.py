"""Convert staged Poly Haven glTFs to the pack format.
build_ph.py SRC_ROOT OUT_DIR TOOL NAME [NAME...]
Per model: Tools/glb_to_obj.py conversion, base colour -> <Name>_Color.jpg, normal -> <Name>_Normal.jpg,
ARM (R=AO, G=rough, B=metal) -> <Name>_MetallicSmoothness.png (RGB metal, A = 1 - rough)."""
import sys, os, json, glob, shutil, subprocess, re
import numpy as np
from PIL import Image

SPEC = {
  # name: (target tris or 0, colour px, normal px, ms px, glass materials {mat: (Kd, d)})
  'Crowbar':            (0,    1024, 1024, 512, {}),
  'Sledgehammer':       (0,    1024, 1024, 512, {}),
  'BoltCutters':        (0,    1024, 1024, 512, {}),
  'FlatheadScrewdriver':(0,    512,  512,  256, {}),
  'CrossPeinHammer':    (0,    512,  512,  256, {}),
  'WorkGloves':         (0,    1024, 1024, 512, {}),
  'Trashbag':           (0,    1024, 1024, 512, {}),
  'Stone':              (4000, 512,  512,  256, {}),
  'Clipboard':          (0,    1024, 1024, 512, {}),
  'MeasuringTape':      (0,    512,  512,  256, {}),
  'MagnifyingGlass':    (0,    512,  512,  256, {'magnifying_glass_01_lense': ((0.85, 0.92, 0.9), 0.25)}),
  'SecurityCamera':     (9000, 1024, 1024, 512, {'security_camera_01_glass': ((0.05, 0.05, 0.06), 0.55)}),
  'PortableSearchlight':(9000, 1024, 1024, 512, {'portable_searchlight_glass': ((0.92, 0.94, 0.95), 0.3)}),
  'OfficeNotepads':     (0,    1024, 512,  256, {}),
  'LeatherBinder':      (8000, 1024, 512,  256, {}),
}

def find(folder, key):
    hits = sorted(glob.glob(os.path.join(folder, 'textures', '*%s*' % key)))
    return hits[0] if hits else None

def main():
    src_root, out, tool = sys.argv[1:4]
    os.makedirs(out, exist_ok=True)
    report = {}
    for name in sys.argv[4:]:
        tris, cpx, npx, mpx, glass = SPEC[name]
        folder = os.path.join(src_root, name)
        gltf = [g for g in glob.glob(os.path.join(folder, '*.gltf'))][0]
        work = os.path.join(src_root, '_work'); os.makedirs(work, exist_ok=True)
        # copy gltf under the target name so the tool names its output after the pack name
        tmp = os.path.join(folder, name + '.gltf')
        if not os.path.exists(tmp): shutil.copy(gltf, tmp)
        tout = os.path.join(work, name); shutil.rmtree(tout, ignore_errors=True); os.makedirs(tout)
        cmd = ['python3', '-P', tool, tout, tmp, '--max-texture', str(cpx), '--ground']
        if tris: cmd += ['--target-tris', str(tris)]
        r = subprocess.run(cmd, capture_output=True, text=True)
        print(r.stdout.strip(), r.stderr.strip()[-300:] if r.returncode else '')
        objp = os.path.join(tout, name + '.obj'); mtlp = os.path.join(tout, name + '.mtl')
        mtl = open(mtlp).read()
        # colour textures written by the tool -> one JPG (all materials of these models share one atlas)
        pngs = sorted(glob.glob(os.path.join(tout, name + '_*.png')))
        color = name + '_Color.jpg'
        if pngs:
            Image.open(pngs[0]).convert('RGB').save(os.path.join(out, color), quality=90, optimize=True)
        blocks = []
        for blk in mtl.strip().split('\n\n'):
            lines = blk.split('\n'); mname = lines[0].split()[1]
            if mname in glass:
                kd, d = glass[mname]
                blk = 'newmtl %s\nKd %.3f %.3f %.3f\nKs 0.5 0.5 0.5\nNs 200\nd %.2f' % (mname, kd[0], kd[1], kd[2], d)
            else:
                blk = '\n'.join(l if not l.startswith('map_Kd') else 'map_Kd ' + color for l in lines)
                blk = blk.replace('\nd 1.000', '\nd 1.0')
            blocks.append(blk)
        open(os.path.join(out, name + '.mtl'), 'w').write('\n\n'.join(blocks) + '\n')
        shutil.copy(objp, os.path.join(out, name + '.obj'))
        # normal map (OpenGL convention = Unity convention)
        nor = find(folder, 'nor_gl')
        Image.open(nor).convert('RGB').resize((npx, npx), Image.LANCZOS).save(os.path.join(out, name + '_Normal.jpg'), quality=92, optimize=True)
        arm = find(folder, '_arm_')
        a = np.asarray(Image.open(arm).convert('RGB').resize((mpx, mpx), Image.LANCZOS)).astype(np.float32)
        metal = a[..., 2]; smooth = 255 - a[..., 1]
        ms = np.dstack([metal, metal, metal, smooth]).clip(0, 255).astype(np.uint8)
        Image.fromarray(ms, 'RGBA').save(os.path.join(out, name + '_MetallicSmoothness.png'), optimize=True)
        # stats
        V = [l for l in open(objp) if l.startswith('v ')]
        F = [l for l in open(objp) if l.startswith('f ')]
        P = np.array([[float(x) for x in l.split()[1:4]] for l in V])
        report[name] = {'tris': len(F), 'size_m': np.round(P.max(0) - P.min(0), 3).tolist()}
        print(name, report[name])
    rp = os.path.join(src_root, '_work', 'report.json'); old = json.load(open(rp)) if os.path.exists(rp) else {}
    old.update(report); json.dump(old, open(rp, 'w'), indent=1)

main()
