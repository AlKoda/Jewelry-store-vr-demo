#!/usr/bin/env python3
"""Generate a model with the Tripo Platform API (api.tripo3d.ai) without the Studio UI.

Standard library only, so it runs on the PC or in the cloud. The key comes from TRIPO_API_KEY or
--key-file (a text file holding the tsk_... key; never commit it). API credits are separate from
Studio credits (platform.tripo3d.ai). Endpoints follow the public v2 OpenAPI docs as of October 2026;
unverified until the first run, so read the printed server replies.

  python3 Tools/tripo_generate.py --prompt "..." --out Downloads/Tripo/police_car --faces 15000
  python3 Tools/tripo_generate.py --image ref.jpg --out Downloads/Tripo/police_car --faces 15000 --fbx

Writes <out>.glb (PBR), <out>.fbx when --fbx, <out>_preview.webp and <out>_task.json.
"""
import argparse, json, mimetypes, os, sys, time, urllib.request, urllib.error, uuid

BASE = "https://api.tripo3d.ai/v2/openapi"


def key_from(args):
    k = os.environ.get("TRIPO_API_KEY", "")
    if args.key_file and os.path.exists(args.key_file):
        k = open(args.key_file, encoding="utf-8").read().strip()
    if not k.startswith("tsk_"):
        sys.exit("No Tripo API key: set TRIPO_API_KEY or pass --key-file <file with tsk_... key>")
    return k


def call(key, path, body=None, files=None):
    url = BASE + path
    if files:
        boundary = uuid.uuid4().hex
        parts = []
        for name, filename in files.items():
            ctype = mimetypes.guess_type(filename)[0] or "application/octet-stream"
            parts.append(("--%s\r\nContent-Disposition: form-data; name=\"%s\"; filename=\"%s\"\r\nContent-Type: %s\r\n\r\n"
                          % (boundary, name, os.path.basename(filename), ctype)).encode() + open(filename, "rb").read() + b"\r\n")
        data = b"".join(parts) + ("--%s--\r\n" % boundary).encode()
        req = urllib.request.Request(url, data=data, headers={"Content-Type": "multipart/form-data; boundary=" + boundary})
    elif body is not None:
        req = urllib.request.Request(url, data=json.dumps(body).encode(), headers={"Content-Type": "application/json"})
    else:
        req = urllib.request.Request(url)
    req.add_header("Authorization", "Bearer " + key)
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            reply = json.loads(r.read().decode())
    except urllib.error.HTTPError as e:
        sys.exit("Tripo %s -> HTTP %s: %s" % (path, e.code, e.read().decode(errors="replace")[:800]))
    if reply.get("code") != 0:
        sys.exit("Tripo %s refused: %s" % (path, json.dumps(reply)[:800]))
    return reply["data"]


def wait(key, task_id, label):
    while True:
        d = call(key, "/task/" + task_id)
        status = d.get("status")
        print("  %s: %s %s%%" % (label, status, d.get("progress", "")), flush=True)
        if status in ("success", "failed", "cancelled", "banned", "expired", "unknown"):
            if status != "success":
                sys.exit("Tripo task %s ended %s: %s" % (task_id, status, json.dumps(d)[:800]))
            return d
        time.sleep(6)


def download(url, path):
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    with urllib.request.urlopen(url, timeout=600) as r, open(path, "wb") as f:
        f.write(r.read())
    print("  wrote %s (%.1f MB)" % (path, os.path.getsize(path) / 1e6))


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--prompt")
    p.add_argument("--negative", default="text, letters, logos, people, background, ground plane, two cars")
    p.add_argument("--image", help="reference picture; image-to-model follows it closely")
    p.add_argument("--out", required=True, help="output path without extension")
    p.add_argument("--faces", type=int, default=15000)
    p.add_argument("--model-version", default=None, help="e.g. v3.0-20250812; omitted = Tripo's current default")
    p.add_argument("--texture-quality", default="detailed", choices=["standard", "detailed"])
    p.add_argument("--quad", action="store_true")
    p.add_argument("--fbx", action="store_true", help="also request an FBX conversion")
    p.add_argument("--key-file")
    args = p.parse_args()
    if not args.prompt and not args.image:
        sys.exit("Give --prompt or --image")
    key = key_from(args)

    body = {"face_limit": args.faces, "texture": True, "pbr": True, "texture_quality": args.texture_quality, "quad": args.quad}
    if args.model_version:
        body["model_version"] = args.model_version
    if args.image:
        print("Uploading", args.image)
        up = call(key, "/upload", files={"file": args.image})
        body.update({"type": "image_to_model", "file": {"type": os.path.splitext(args.image)[1].lstrip(".").lower() or "jpg", "file_token": up["image_token"]}})
    else:
        body.update({"type": "text_to_model", "prompt": args.prompt, "negative_prompt": args.negative})
    print("Creating task:", json.dumps({k: v for k, v in body.items() if k != "file"}))
    task_id = call(key, "/task", body)["task_id"]
    done = wait(key, task_id, "model")
    out = done.get("output", {})
    json.dump(done, open(args.out + "_task.json", "w"), indent=1)
    model_url = out.get("pbr_model") or out.get("model")
    if not model_url:
        sys.exit("No model URL in the reply: " + json.dumps(out)[:800])
    download(model_url, args.out + ".glb")
    if out.get("rendered_image"):
        download(out["rendered_image"], args.out + "_preview.webp")
    if args.fbx:
        conv = call(key, "/task", {"type": "convert_model", "format": "FBX", "original_model_task_id": task_id, "face_limit": args.faces})["task_id"]
        c = wait(key, conv, "fbx")
        url = c.get("output", {}).get("model")
        if url:
            download(url, args.out + ".fbx")
    print("Done:", task_id)


if __name__ == "__main__":
    main()
