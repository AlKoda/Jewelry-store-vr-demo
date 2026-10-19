"""Refresh Downloads/JewelryStore_Unity6000.6.2f1.zip from Prototype.

Usage:  python3 -I Tools/zip_snapshot.py [repo]      (repo defaults to the folder above Tools)

The zip is the one Unity project snapshot (Assets + ProjectSettings) that Play-Keyboard.cmd,
Play-VR.cmd and Open-In-Unity.cmd unpack on first run. This script
  * replaces Assets/CrimeSceneDemo/{Scripts,Editor,Shaders} with Prototype's, subfolders and
    committed .meta files included. A .cs or .shader without a .meta gets one written next to it
    in Prototype (fresh guid), so commit it together with the file;
  * copies tracked Prototype/ThirdParty files except the large packs in LARGE (the launchers mirror
    those into the project from the checkout, and GitHub refuses files over 100 MB);
  * keeps everything else in the zip (scenes, generated assets, ProjectSettings) unchanged.
Run it after every batch that changes scripts, shaders or the small ThirdParty packs.
"""
import os
import subprocess
import sys
import uuid
import zipfile

REPO = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), '..'))
ZIP = os.path.join(REPO, 'Downloads', 'JewelryStore_Unity6000.6.2f1.zip')
BASE = 'Assets/CrimeSceneDemo/'
CODE = ('Scripts', 'Editor', 'Shaders')
LARGE = {'PBRInterior', 'PBRExterior', 'NightSky', 'JewelryModels', 'ForensicModels', 'StreetModels',
         'InteriorModels', 'WallGraphics', 'SurfaceDecals', 'NightSoundscape', 'Music', 'PeopleModels',
         'AmbientProps', 'AmbientProps2', 'CutoutDecals', 'RealismTextures', 'RealFoley', 'RealFoley2', 'Environments', 'ReferenceTextures', 'CgbookcaseTextures'}
LIMIT_MB = 95

MONO = ("fileFormatVersion: 2\nguid: {g}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n"
        "  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData: \n"
        "  assetBundleName: \n  assetBundleVariant: \n")
SHADER = ("fileFormatVersion: 2\nguid: {g}\nShaderImporter:\n  externalObjects: {{}}\n  defaultTextures: []\n"
          "  nonModifiableTextures: []\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
FOLDER = ("fileFormatVersion: 2\nguid: {g}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n"
          "  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


def ensure_metas(root):
    """Writes a .meta for every script, shader and subfolder in Prototype that lacks one."""
    written = []
    for folder, dirs, files in os.walk(root):
        for d in dirs:
            meta = os.path.join(folder, d + '.meta')
            if not os.path.exists(meta):
                open(meta, 'w').write(FOLDER.format(g=uuid.uuid4().hex)); written.append(meta)
        for f in files:
            if f.endswith(('.cs', '.shader')) and not os.path.exists(os.path.join(folder, f + '.meta')):
                template = MONO if f.endswith('.cs') else SHADER
                open(os.path.join(folder, f + '.meta'), 'w').write(template.format(g=uuid.uuid4().hex))
                written.append(os.path.join(folder, f + '.meta'))
    return written


def main():
    with zipfile.ZipFile(ZIP) as z:
        entries = {i.filename: z.read(i.filename) for i in z.infolist() if not i.is_dir()}
    # Carry the actual single project's saved scene/bakes, XR configuration and
    # resolved package setup. Code is replaced from authoritative Prototype below.
    project = os.path.join(REPO, 'UnityProject_Demo')
    if os.path.isdir(os.path.join(project, 'Assets')):
        for folder in ('Assets', 'Packages', 'ProjectSettings'):
            for directory, _, names in os.walk(os.path.join(project, folder)):
                for name in names:
                    path=os.path.join(directory,name)
                    rel=os.path.relpath(path,project).replace(os.sep,'/')
                    if rel.startswith(BASE+'ThirdParty/') or any(rel.startswith(BASE+c+'/') for c in CODE): continue
                    entries[rel]=open(path,'rb').read()
        entries['READ_ME_LATEST.txt']=b'Quest 3 PCVR + keyboard, Unity 6000.6.2f1. One project and saved night scene. See repository Docs/VR_TESTING.md. Large packs must be synced from the repository. Do not regenerate a manually edited scene unintentionally.\n'
    before = set(entries)
    written = []
    for code in CODE:
        prefix = BASE + code + '/'
        for key in [k for k in entries if k.startswith(prefix)]:
            del entries[key]
        src = os.path.join(REPO, 'Prototype', code)
        written += ensure_metas(src)
        for folder, _, files in os.walk(src):
            for f in files:
                path = os.path.join(folder, f)
                entries[prefix + os.path.relpath(path, src).replace(os.sep, '/')] = open(path, 'rb').read()
    tracked = subprocess.run(['git', '-C', REPO, 'ls-files', 'Prototype/ThirdParty'],
                             capture_output=True, text=True, check=True).stdout.splitlines()
    for rel in tracked:
        inner = rel[len('Prototype/ThirdParty/'):]
        if inner.split('/')[0] in LARGE or not os.path.isfile(os.path.join(REPO, rel)):
            continue
        entries[BASE + 'ThirdParty/' + inner] = open(os.path.join(REPO, rel), 'rb').read()
    tmp = ZIP + '.tmp'
    with zipfile.ZipFile(tmp, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as out:
        for key in sorted(entries):
            out.writestr(key, entries[key])
    size = os.path.getsize(tmp) / 1048576
    if size > LIMIT_MB:
        os.remove(tmp)
        sys.exit('zip would be %.1f MB, over %d MB: add the new large pack to LARGE' % (size, LIMIT_MB))
    os.replace(tmp, ZIP)
    added, removed = set(entries) - before, before - set(entries)
    print('zip %.1f MB, %d entries: %d added, %d removed' % (size, len(entries), len(added), len(removed)))
    for path in written:
        print('  new .meta written, commit it:', os.path.relpath(path, REPO))


if __name__ == '__main__':
    main()
