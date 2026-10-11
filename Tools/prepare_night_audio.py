"""Prepare a small Kenney CC0 selection and a synthesized ambience bed.
Usage: python Tools/prepare_night_audio.py .asset-cache/interface.zip
Requires numpy, soundfile. No runtime changes; original OGG files are retained.
"""
import hashlib
import io
import json
from pathlib import Path
import sys
import zipfile
import numpy as np
import soundfile as sf

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Prototype/ThirdParty/NightAudio"
SELECTION = {"click_001": "CameraClick", "switch_001": "FlashlightSwitch",
             "drop_001": "ToolSetDown", "select_001": "PanelSelect"}


def main(archive):
    OUT.mkdir(parents=True, exist_ok=True)
    originals = OUT / "Originals"
    originals.mkdir(exist_ok=True)
    records = []
    with zipfile.ZipFile(archive) as z:
        # Preserve the author's actual license separately from the batch credits.
        (originals / "License.txt").write_bytes(z.read("License.txt"))
        for source, target in SELECTION.items():
            raw = z.read(f"Audio/{source}.ogg")
            (originals / (source + ".ogg")).write_bytes(raw)
            data, rate = sf.read(io.BytesIO(raw), always_2d=True)
            mono = data.mean(axis=1)
            # Conservative audition level, not a final headset mix.
            mono -= mono.mean()
            peak = np.max(np.abs(mono))
            assert peak > 1e-5
            mono *= 0.25 / peak  # -12 dBFS peak
            fade = min(int(rate * .002), len(mono) // 4)
            mono[:fade] *= np.linspace(0, 1, fade)
            mono[-fade:] *= np.linspace(1, 0, fade)
            sf.write(OUT / (target + ".wav"), mono, rate, subtype="PCM_16")
            records.append({"file": target + ".wav", "source_member": f"Audio/{source}.ogg",
                            "source_sha256": hashlib.sha256(raw).hexdigest(),
                            "duration_seconds": round(len(mono)/rate, 4), "sample_rate": rate,
                            "changes": "mono downmix, DC removal, -12 dBFS peak, 2ms edge fades"})
    # Periodic filtered noise, no voices, sirens, music, rain or geographic cues.
    rate, seconds = 24000, 16
    count = rate * seconds
    rng = np.random.default_rng(27102026)
    freq = np.fft.rfftfreq(count, 1/rate)
    envelope = 1/(1+(freq/300)**2) * (1-np.exp(-(freq/45)**2))
    spectrum = (rng.normal(size=len(freq)) + 1j*rng.normal(size=len(freq))) * envelope
    spectrum[0] = 0
    spectrum[-1] = 0
    bed = np.fft.irfft(spectrum, n=count)
    bed *= .08 / np.max(np.abs(bed))
    sf.write(OUT / "DistantAir_Loop.wav", bed, rate, subtype="PCM_16")
    (OUT / "sources.json").write_text(json.dumps({
        "download_date": "2026-10-11", "author": "Kenney", "license": "CC0-1.0",
        "source": "https://kenney.nl/assets/interface-sounds",
        "download": "https://kenney.nl/media/pages/assets/interface-sounds/fa43c1dd4d-1677589452/kenney_interface-sounds.zip",
        "archive_sha256": hashlib.sha256(Path(archive).read_bytes()).hexdigest(),
        "selection": records,
        "DistantAir_Loop.wav": {"author": "Project synthesis with Codex", "source": "Tools/prepare_night_audio.py",
                                "license": "Project-generated, no third-party content", "seconds": seconds,
                                "sample_rate": rate, "seed": 27102026, "loop": "periodic FFT noise"}}, indent=2)+"\n", encoding="utf-8")
    print("Prepared four CC0 interaction WAVs and one original 16-second ambience loop.")


if __name__ == "__main__":
    main(sys.argv[1])
