"""Fetch a .gltf plus all relative buffers/images from a raw.githubusercontent base URL.
usage: fetch_gltf.py BASE_URL GLTF_NAME OUTDIR"""
import json, os, sys, subprocess, urllib.parse, hashlib
base, name, out = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
def get(rel):
    dst = os.path.join(out, urllib.parse.unquote(rel))
    os.makedirs(os.path.dirname(dst) or '.', exist_ok=True)
    if not os.path.exists(dst):
        url = base.rstrip('/') + '/' + urllib.parse.quote(urllib.parse.unquote(rel))
        r = subprocess.run(['curl', '-sSfL', '--retry', '3', url, '-o', dst])
        if r.returncode:
            print('FAIL', url); return None
    data = open(dst, 'rb').read()
    lfs = data[:40].startswith(b'version https://git-lfs')
    print('%-60s %9d %s%s' % (rel, len(data), hashlib.sha256(data).hexdigest()[:16], ' LFS-POINTER' if lfs else ''))
    return dst
p = get(name)
d = json.load(open(p))
for b in d.get('buffers', []) + d.get('images', []):
    if 'uri' in b and not b['uri'].startswith('data:'):
        get(b['uri'])
