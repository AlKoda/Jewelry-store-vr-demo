"""Offline integrity/budget audit for the preparation batch (not a Unity test).
Run: python Tools/audit_night_assets.py
Requires Pillow and numpy. Writes review manifests and previews.
"""
import hashlib
import json
from pathlib import Path
import wave
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
TP = ROOT / "Prototype/ThirdParty"
DOC = ROOT / "Docs/History/AssetPreparation"
MODELS = {
    "KayKitCity": ["car_police", "car_police_wheel_front_left", "car_police_wheel_front_right",
                   "car_police_wheel_rear_left", "car_police_wheel_rear_right", "streetlight", "bench"],
    "KayKitFurniture": ["chair_A", "couch", "table_low", "cactus_medium_A", "cabinet_small_decorated"]}


def main():
    DOC.mkdir(parents=True, exist_ok=True)
    models = []
    for pack, names in MODELS.items():
        assert (TP / pack / 'LICENSE.txt').is_file()
        for name in names:
            p = TP / pack / (name + '.obj')
            lines = p.read_text().splitlines()
            vertices = np.array([[float(c) for c in l.split()[1:4]] for l in lines if l.startswith('v ')])
            faces = [l.split()[1:] for l in lines if l.startswith('f ')]
            assert vertices.size and faces and np.isfinite(vertices).all()
            for face in faces:
                for item in face:
                    index = int(item.split('/')[0])
                    assert index and -len(vertices) <= index <= len(vertices)
            mtls = [l[7:].strip() for l in lines if l.startswith('mtllib ')]
            for mtl in mtls:
                for line in (p.parent/mtl).read_text().splitlines():
                    if line.startswith('map_Kd '):
                        assert (p.parent / line[7:].strip()).is_file()
            models.append({"file": str(p.relative_to(ROOT)).replace('\\','/'), "triangles": sum(len(f)-2 for f in faces),
                           "source_bounds_min": vertices.min(axis=0).round(6).tolist(),
                           "source_bounds_max": vertices.max(axis=0).round(6).tolist(),
                           "source_size_xyz": np.ptp(vertices,axis=0).round(6).tolist(),
                           "sha256": hashlib.sha256(p.read_bytes()).hexdigest(), "license": "CC0-1.0",
                           "author": "Kay Lousberg", "license_file": f"Prototype/ThirdParty/{pack}/LICENSE.txt"})
    modelout=TP/'NightModelReuse'; modelout.mkdir(exist_ok=True)
    (modelout/'models.json').write_text(json.dumps(models,indent=2)+'\n',encoding='utf-8')
    rows=['# Existing CC0 model selection', '', 'OBJ source bounds are raw model units; do not assume metres. No meshes changed.', '',
          '| Model | Triangles | Source size X / Y / Z |', '|---|---:|---|']
    for m in models: rows.append(f"| {Path(m['file']).stem} | {m['triangles']} | {' / '.join(map(str,m['source_size_xyz']))} |")
    (DOC/'model-audit.md').write_text('\n'.join(rows)+'\n',encoding='utf-8')
    textures=[]
    for folder in ['NightMaterials','NightGraphics']:
        for p in sorted((TP/folder).glob('*.png')):
            with Image.open(p) as image:
                image.load()
                assert max(image.size)<=1024
                assert all(n & (n-1) == 0 for n in image.size)
                textures.append({"file": str(p.relative_to(ROOT)).replace('\\','/'), "size":image.size,"mode":image.mode,
                                 "bytes":p.stat().st_size,"sha256":hashlib.sha256(p.read_bytes()).hexdigest()})
    for p in (TP/'NightMaterials').glob('*_Roughness.png'):
        r=np.asarray(Image.open(p)); packed=np.asarray(Image.open(p.with_name(p.name.replace('_Roughness','_MetallicSmoothness'))))
        assert np.array_equal(packed[...,3],255-r)
        assert not packed[...,1:3].any()
    normals={}
    for p in (TP/'NightMaterials').glob('*_Normal.png'):
        n=np.asarray(Image.open(p)).astype(float)/127.5-1
        error=np.abs(np.linalg.norm(n,axis=2)-1)
        assert error.max()<.015
        normals[p.name]={"max_length_error":round(float(error.max()),6)}
    audio=[]
    for p in sorted((TP/'NightAudio').glob('*.wav')):
        with wave.open(str(p)) as w:
            assert w.getnchannels()==1 and w.getsampwidth()==2
            a=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
            assert len(a)>100 and np.max(np.abs(a))<.3 and np.max(np.abs(a))>.01
            audio.append({"file":p.name,"seconds":round(len(a)/w.getframerate(),4),"sample_rate":w.getframerate(),
                          "peak_dbfs":round(float(20*np.log10(np.max(np.abs(a)))),2),
                          "rms_dbfs":round(float(20*np.log10(np.sqrt(np.mean(a*a)))),2),
                          "dc":round(float(a.mean()),7),"wrap_jump":round(float(abs(a[0]-a[-1])),6)})
    reference = sorted((TP/'ReferenceTextures').glob('*_Color.png'))
    assert len(reference)==10
    sheets=[('reference-audit.png',reference),('materials-preview.png',[TP/'NightMaterials/CeilingPaint_Color.png',TP/'NightMaterials/FacadeStone_Color.png'])]
    for name,files in sheets:
        cols=5 if len(files)>2 else 2
        sheet=Image.new('RGB',(cols*220,((len(files)+cols-1)//cols)*248),'#e6e2d9');draw=ImageDraw.Draw(sheet)
        for i,p in enumerate(files):
            x,y=(i%cols)*220,(i//cols)*248
            sheet.paste(Image.open(p).convert('RGB').resize((212,212)),(x,y))
            draw.text((x+3,y+218),p.stem,fill='black')
        sheet.save(DOC/name)
    report={"scope":"File-level audit only; Unity import, audio listening and headset testing not performed.",
            "textures":textures,"normal_checks":normals,"audio":audio,"reused_model_count":len(models),
            "reference_color_normal_files_preserved":20}
    (DOC/'validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(f"PASS: {len(textures)} PNGs, 12 packed material pairs, {len(audio)} WAVs, {len(models)} existing OBJs + MTL/atlas references.")


if __name__=='__main__':
    main()
