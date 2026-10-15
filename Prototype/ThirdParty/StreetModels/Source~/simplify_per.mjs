// node simplify_per.mjs in.gltf out.gltf '{"meshNameSubstring": [ratio, error], "*": [ratio, error]}'
import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import { weld, simplifyPrimitive } from '@gltf-transform/functions';
import { MeshoptSimplifier } from 'meshoptimizer';
const [,, input, output, spec] = process.argv;
const S = JSON.parse(spec);
await MeshoptSimplifier.ready;
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const doc = await io.read(input);
await doc.transform(weld());
for (const node of doc.getRoot().listNodes()) {
  const mesh = node.getMesh(); if (!mesh) continue;
  const name = node.getName() + ' ' + mesh.getName();
  let key = Object.keys(S).find(k => k !== '*' && name.includes(k)) || '*';
  if (!S[key]) continue;
  const [ratio, error] = S[key];
  for (const prim of mesh.listPrimitives()) {
    const before = prim.getIndices().getCount() / 3;
    simplifyPrimitive(prim, { simplifier: MeshoptSimplifier, ratio, error, lockBorder: false });
    console.log(name, before, '->', prim.getIndices().getCount() / 3);
  }
}
await io.write(output, doc);
