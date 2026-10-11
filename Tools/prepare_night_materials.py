"""Prepare selected CC0 PBR maps from downloaded ambientCG 1K PNG archives.

Run from repository root: python Tools/prepare_night_materials.py .asset-cache
Requires Pillow and numpy. Never overwrites ReferenceTextures.
"""
import hashlib
import io
import json
from pathlib import Path
import sys
import zipfile

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Prototype/ThirdParty/NightMaterials"
RECIPES = [("PaintedPlaster017", "CeilingPaint", 512), ("Marble006", "FacadeStone", 1024)]
# Artist-selected scalar starting points, matching StoreKit's current smoothness.
ROUGHNESS = {"Walnut": (0.65, 0), "SuedeCream": (0.95, 0),
             "VelvetCharcoal": (0.95, 0), "BronzeBrushed": (0.55, 0.8),
             "StoneTop": (0.5, 0), "SafeSteel": (0.65, 0.1),
             "FloorTile": (0.45, 0), "PlasterWarm": (0.88, 0),
             "PavementSlab": (0.65, 0), "AsphaltWet": (0.3, 0)}


def packed(roughness, metallic=0):
    a = np.zeros((*roughness.shape, 4), dtype=np.uint8)
    a[..., 0] = round(metallic * 255)
    a[..., 3] = 255 - roughness
    return Image.fromarray(a)


def main(cache):
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    for asset, name, size in RECIPES:
        archive = Path(cache) / (asset + ".zip")
        with zipfile.ZipFile(archive) as z:
            for source, target in [("Color", "Color"), ("NormalGL", "Normal"), ("Roughness", "Roughness")]:
                member = f"{asset}_1K-PNG_{source}.png"
                image = Image.open(io.BytesIO(z.read(member))).convert("L" if source == "Roughness" else "RGB")
                image = image.resize((size, size), Image.Resampling.LANCZOS)
                if source == "NormalGL":
                    n = np.asarray(image).astype(float) / 127.5 - 1
                    n /= np.maximum(np.linalg.norm(n, axis=2, keepdims=True), 1e-8)
                    image = Image.fromarray(np.clip((n + 1) * 127.5, 0, 255).round().astype("uint8"))
                image.save(OUT / f"{name}_{target}.png")
                if source == "Roughness":
                    packed(np.asarray(image)).save(OUT / f"{name}_MetallicSmoothness.png")
        records.append({"name": name, "source": f"https://ambientcg.com/view?id={asset}",
                        "download": f"https://ambientcg.com/get?file={asset}_1K-PNG.zip",
                        "archive_sha256": hashlib.sha256(archive.read_bytes()).hexdigest(),
                        "author": "Lennart Demes / ambientCG", "license": "CC0-1.0",
                        "size": size, "normal": "OpenGL +Y, renormalized after resize"})
    for name, (roughness, metallic) in ROUGHNESS.items():
        r = np.full((16, 16), round(roughness * 255), dtype=np.uint8)
        Image.fromarray(r).save(OUT / f"{name}_Roughness.png")
        packed(r, metallic).save(OUT / f"{name}_MetallicSmoothness.png")
    (OUT / "sources.json").write_text(json.dumps(records, indent=2) + "\n", encoding="utf-8")
    print("Prepared 28 maps; existing ReferenceTextures preserved.")


if __name__ == "__main__":
    main(sys.argv[1])
