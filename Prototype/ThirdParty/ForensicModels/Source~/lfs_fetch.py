"""Fetch Git LFS objects of the ConcordDev Poly Haven mirror from media.githubusercontent.com.
lfs_fetch.py CLONE_DIR OUT_DIR MODEL [MODEL...] [--files diffuse,nor_gl,arm,fbx]
Each pointer's oid (sha256) is checked against the downloaded bytes."""
import sys, os, hashlib, subprocess, json

REPO = 'ConcordDev/concord-cognitive-engine'
BASE = 'apps/concordia-living-world/unity-client/Assets/Concordia/PolyHaven/Models'

def main():
    args = sys.argv[1:]
    kinds = ['diffuse', 'nor_gl', 'arm', 'fbx']
    if '--files' in args:
        i = args.index('--files'); kinds = args[i + 1].split(','); del args[i:i + 2]
    clone, out = args[0], args[1]
    commit = subprocess.check_output(['git', '-C', clone, 'rev-parse', 'HEAD'], text=True).strip()
    log = []
    for model in args[2:]:
        folder = os.path.join(clone, BASE, model)
        dest = os.path.join(out, model); os.makedirs(dest, exist_ok=True)
        for name in sorted(os.listdir(folder)):
            path = os.path.join(folder, name)
            if not os.path.isfile(path) or name.endswith('.meta'):
                continue
            kind = 'fbx' if name.endswith('.fbx') else next((k for k in ('diffuse', 'nor_gl', 'arm') if '_%s_' % k in name), None)
            if kind not in kinds:
                continue
            ptr = open(path, 'rb').read(400).decode('ascii', 'replace')
            if not ptr.startswith('version https://git-lfs'):
                data = open(path, 'rb').read(); oid = hashlib.sha256(data).hexdigest()
            else:
                oid = [l.split(':', 1)[1] for l in ptr.splitlines() if l.startswith('oid sha256:')][0]
                url = 'https://media.githubusercontent.com/media/%s/%s/%s/%s/%s' % (REPO, commit, BASE, model, name)
                data = subprocess.check_output(['curl', '-sSfL', '--retry', '3', url])
                got = hashlib.sha256(data).hexdigest()
                if got != oid:
                    raise SystemExit('hash mismatch %s %s %s' % (name, got, oid))
            open(os.path.join(dest, name), 'wb').write(data)
            log.append({'model': model, 'file': name, 'sha256': oid, 'bytes': len(data),
                        'mirror_path': '%s/%s/%s' % (BASE, model, name), 'commit': commit})
            print(model, name, len(data))
    p = os.path.join(out, 'lfs_log.json')
    old = json.load(open(p)) if os.path.exists(p) else []
    keep = {(e['model'], e['file']): e for e in old}
    for e in log: keep[(e['model'], e['file'])] = e
    json.dump(sorted(keep.values(), key=lambda e: (e['model'], e['file'])), open(p, 'w'), indent=1)

main()
