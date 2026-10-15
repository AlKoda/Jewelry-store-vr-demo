import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import { dequantize } from '@gltf-transform/functions';
import { MeshoptDecoder } from 'meshoptimizer';
const [,, input, output] = process.argv;
await MeshoptDecoder.ready;
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({ 'meshopt.decoder': MeshoptDecoder });
const doc = await io.read(input);
for (const ext of doc.getRoot().listExtensionsUsed()) {
  if (['EXT_meshopt_compression', 'KHR_mesh_quantization'].includes(ext.extensionName)) ext.dispose();
}
await doc.transform(dequantize());
// keep webp images but write them as files
doc.getRoot().listTextures().forEach((t, i) => { const mt = t.getMimeType() || 'image/webp'; t.setMimeType(mt); t.setURI('tex_' + i + (mt === 'image/png' ? '.png' : mt === 'image/jpeg' ? '.jpg' : '.webp')); });
await io.write(output, doc);
console.log('ok', doc.getRoot().listMeshes().length, 'meshes', doc.getRoot().listTextures().map(t => t.getURI() + ':' + t.getMimeType()).join(' '));
