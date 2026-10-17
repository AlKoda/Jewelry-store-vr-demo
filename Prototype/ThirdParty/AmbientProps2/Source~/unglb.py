import sys, json, struct, os
src, outdir, name = sys.argv[1:4]
b = open(src,'rb').read(); assert b[:4]==b'glTF'
off=12; js=None; bin_=None
while off < len(b):
    ln, ty = struct.unpack('<II', b[off:off+8]); ch=b[off+8:off+8+ln]; off+=8+ln
    if ty==0x4E4F534A: js=json.loads(ch)
    elif ty==0x004E4942: bin_=ch
os.makedirs(outdir, exist_ok=True)
for i,im in enumerate(js.get('images',[])):
    if 'bufferView' in im:
        bv=js['bufferViews'][im['bufferView']]; data=bin_[bv.get('byteOffset',0):bv.get('byteOffset',0)+bv['byteLength']]
        ext='.png' if 'png' in im.get('mimeType','') else '.jpg'
        fn='%s_img%d%s'%(name,i,ext); open(os.path.join(outdir,fn),'wb').write(data)
        im.pop('bufferView'); im.pop('mimeType',None); im['uri']=fn
open(os.path.join(outdir,name+'.bin'),'wb').write(bin_)
js['buffers']=[{'uri':name+'.bin','byteLength':len(bin_)}]
json.dump(js, open(os.path.join(outdir,name+'.gltf'),'w'))
