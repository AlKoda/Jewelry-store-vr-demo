// node simplify.mjs in.gltf out.gltf ratio [error] [meshNameRegexToKeepFull]
import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import { weld, simplify } from '@gltf-transform/functions';
import { MeshoptSimplifier } from 'meshoptimizer';
const [,, input, output, ratioS, errorS] = process.argv;
await MeshoptSimplifier.ready;
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const doc = await io.read(input);
const count = () => doc.getRoot().listMeshes().reduce((s, m) => s + m.listPrimitives().reduce((t, p) => t + (p.getIndices() ? p.getIndices().getCount() : p.getAttribute('POSITION').getCount()) / 3, 0), 0);
const before = count();
await doc.transform(weld(), simplify({ simplifier: MeshoptSimplifier, ratio: parseFloat(ratioS), error: parseFloat(errorS || '0.002'), lockBorder: false }));
console.log('tris', before, '->', count());
await io.write(output, doc);
