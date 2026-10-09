#!/usr/bin/env python3
"""Convert glTF/GLB models to OBJ + MTL + base-colour PNGs for the package-free Unity project.

Unity imports OBJ without any package; glTF would need one. Only base colour survives
(no normal/roughness maps), which is what the Built-in Standard shader uses here.

    python3 -P Tools/glb_to_obj.py OUT_DIR model.glb [model2.glb ...]
        --max-texture 1024      longest texture edge (default 1024)
        --skip Plane,Bulb       drop geometries whose name or material contains any of these
        --target-tris 12000     decimate to about this many triangles (needs fast_simplification)
        --scale 0.01            multiply vertices (e.g. centimetre models)
        --ground                move the model so its lowest point is y = 0 and it is centred in x/z

Requires: pip install trimesh numpy pillow [fast_simplification].
"""
import argparse, os, sys
import numpy as np
import trimesh
from PIL import Image


def safe(text):
    return ''.join(c if c.isalnum() else '_' for c in str(text)) or 'mat'


def convert(path, out, max_texture, skip, target_tris, scale, ground):
    name = os.path.splitext(os.path.basename(path))[0]
    scene = trimesh.load(path, force='scene')
    meshes = scene.dump(concatenate=False)  # node transforms applied
    parts = []
    for index, mesh in enumerate(meshes):
        material = getattr(mesh.visual, 'material', None)
        label = (mesh.metadata.get('name', '') or '') + ' ' + str(getattr(material, 'name', '') or '')
        if any(s.lower() in label.lower() for s in skip):
            continue
        parts.append((index, mesh, material))
    total = sum(len(m.faces) for _, m, _ in parts)
    if target_tris and total > target_tris:
        import fast_simplification
        for i, (index, mesh, material) in enumerate(parts):
            share = max(64, int(target_tris * len(mesh.faces) / total))
            if len(mesh.faces) <= share:
                continue
            uv = mesh.visual.uv if hasattr(mesh.visual, 'uv') and mesh.visual.uv is not None else None
            # Decimate positions; UVs are re-sampled from the nearest original vertex.
            points, faces = fast_simplification.simplify(mesh.vertices.astype(np.float32), mesh.faces.astype(np.int32),
                                                         target_count=share)
            new = trimesh.Trimesh(points, faces, process=False)
            if uv is not None:
                from scipy.spatial import cKDTree
                _, idx = cKDTree(mesh.vertices).query(points)
                new.visual = trimesh.visual.TextureVisuals(uv=uv[idx], material=material)
            parts[i] = (index, new, material)
    # Optional normalisation of the whole model.
    if scale != 1 or ground:
        all_v = np.vstack([m.vertices for _, m, _ in parts]) * scale
        offset = np.array([(all_v[:, 0].min() + all_v[:, 0].max()) / 2, all_v[:, 1].min(),
                           (all_v[:, 2].min() + all_v[:, 2].max()) / 2]) if ground else np.zeros(3)
    else:
        offset = np.zeros(3)
    obj = ['mtllib %s.mtl' % name]
    mtl = []
    written = set()
    voff = toff = 0
    for index, mesh, material in parts:
        mname = safe(getattr(material, 'name', None) or 'mat%d' % index)
        if mname not in written:
            written.add(mname)
            kd, alpha, texture = [1, 1, 1], 1.0, None
            if material is not None:
                factor = getattr(material, 'baseColorFactor', None)
                if factor is not None:
                    factor = [float(x) / 255 if max(factor) > 1 else float(x) for x in factor]
                    kd, alpha = factor[:3], (factor[3] if len(factor) > 3 else 1.0)
                image = getattr(material, 'baseColorTexture', None) or getattr(material, 'image', None)
                if image is not None:
                    image = image.convert('RGBA')
                    if max(image.size) > max_texture:
                        ratio = max_texture / max(image.size)
                        image = image.resize((max(1, int(image.size[0] * ratio)), max(1, int(image.size[1] * ratio))), Image.LANCZOS)
                    texture = '%s_%s.png' % (name, mname)
                    image.save(os.path.join(out, texture), optimize=True)
            mtl.append('newmtl %s\nKd %.3f %.3f %.3f\nKs 0.05 0.05 0.05\nd %.3f%s'
                       % (mname, kd[0], kd[1], kd[2], alpha, ('\nmap_Kd ' + texture) if texture else ''))
        uv = mesh.visual.uv if hasattr(mesh.visual, 'uv') and mesh.visual.uv is not None and len(mesh.visual.uv) == len(mesh.vertices) else None
        vertices = mesh.vertices * scale - offset
        obj.append('g ' + safe(mesh.metadata.get('name', '') or 'part%d' % index))
        obj.append('usemtl ' + mname)
        obj += ['v %.5f %.5f %.5f' % tuple(v) for v in vertices]
        if uv is not None:
            obj += ['vt %.5f %.5f' % tuple(t) for t in uv]
            obj += ['f %d/%d %d/%d %d/%d' % (a + 1 + voff, a + 1 + toff, b + 1 + voff, b + 1 + toff, c + 1 + voff, c + 1 + toff)
                    for a, b, c in mesh.faces]
            toff += len(uv)
        else:
            obj += ['f %d %d %d' % (a + 1 + voff, b + 1 + voff, c + 1 + voff) for a, b, c in mesh.faces]
        voff += len(vertices)
    with open(os.path.join(out, name + '.obj'), 'w') as handle:
        handle.write('\n'.join(obj) + '\n')
    with open(os.path.join(out, name + '.mtl'), 'w') as handle:
        handle.write('\n\n'.join(mtl) + '\n')
    tris = sum(len(m.faces) for _, m, _ in parts)
    print('%s: %d parts, %d triangles, %d materials' % (name, len(parts), tris, len(written)))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('out')
    parser.add_argument('models', nargs='+')
    parser.add_argument('--max-texture', type=int, default=1024)
    parser.add_argument('--skip', default='')
    parser.add_argument('--target-tris', type=int, default=0)
    parser.add_argument('--scale', type=float, default=1.0)
    parser.add_argument('--ground', action='store_true')
    args = parser.parse_args()
    os.makedirs(args.out, exist_ok=True)
    skip = [s for s in args.skip.split(',') if s]
    for model in args.models:
        convert(model, args.out, args.max_texture, skip, args.target_tris, args.scale, args.ground)


if __name__ == '__main__':
    sys.exit(main())
