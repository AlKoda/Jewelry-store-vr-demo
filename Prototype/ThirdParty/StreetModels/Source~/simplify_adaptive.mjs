// node simplify_adaptive.mjs in.gltf out.gltf '{"namePrefix": [targetTris, minAreaRatio], ...}'
// Per primitive: meshoptimizer simplify (seam-aware: vertices that share a position but not UV/normal are wedges),
// starting at targetTris and raising the budget by 25 % until the result keeps at least minAreaRatio of the
// surface area. Zero-area slivers between coincident seam wedges and unused vertices are removed later in Python.
import { NodeIO } from '@gltf-transform/core';
import { weld } from '@gltf-transform/functions';
import { MeshoptSimplifier } from 'meshoptimizer';
const [,, input, output, spec] = process.argv;
const S = JSON.parse(spec);
await MeshoptSimplifier.ready;
const io = new NodeIO();
const doc = await io.read(input);
await doc.transform(weld());

function areas(idx, p) {
  let total = 0, degenerate = 0;
  for (let i = 0; i < idx.length; i += 3) {
    const a = idx[i] * 3, b = idx[i + 1] * 3, c = idx[i + 2] * 3;
    const ux = p[b] - p[a], uy = p[b + 1] - p[a + 1], uz = p[b + 2] - p[a + 2];
    const vx = p[c] - p[a], vy = p[c + 1] - p[a + 1], vz = p[c + 2] - p[a + 2];
    const cx = uy * vz - uz * vy, cy = uz * vx - ux * vz, cz = ux * vy - uy * vx;
    const ar = 0.5 * Math.sqrt(cx * cx + cy * cy + cz * cz);
    total += ar; if (ar < 1e-10) degenerate++;
  }
  return [total, degenerate];
}

let before = 0, after = 0, raised = 0;
for (const node of doc.getRoot().listNodes()) {
  const mesh = node.getMesh(); if (!mesh) continue;
  const key = Object.keys(S).find(k => node.getName().startsWith(k));
  if (!key) continue;
  const [target, minRatio] = S[key];
  for (const prim of mesh.listPrimitives()) {
    const pos = prim.getAttribute('POSITION').getArray();
    const idx = Uint32Array.from(prim.getIndices().getArray());
    const n = idx.length / 3;
    const [a0] = areas(idx, pos);
    let best = idx, t = target;
    while (t < n) {
      const [out] = MeshoptSimplifier.simplify(idx, pos, 3, t * 3, 1.0);
      const [a, deg] = areas(out, pos);
      if (a >= minRatio * a0 && out.length / 3 - deg >= Math.min(t, 4)) { best = out; break; }
      t = Math.ceil(t * 1.25); raised++;
    }
    prim.getIndices().setArray(best);
    before += n; after += best.length / 3;
    if (process.env.VERBOSE) console.log(node.getName(), n, '->', best.length / 3);
  }
}
for (const b of doc.getRoot().listBuffers()) b.setURI(output.split('/').pop().replace('.gltf', '.bin'));
console.log('tris', before, '->', after, '; budget raised', raised, 'times');
await io.write(output, doc);
