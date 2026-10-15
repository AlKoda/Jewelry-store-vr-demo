"""Minimal Radiance RGBE (.hdr) reader/writer with new-style RLE, numpy only."""
import numpy as np, re

def read_hdr(path):
    with open(path, 'rb') as f:
        data = f.read()
    # header
    pos = 0
    header = []
    while True:
        end = data.index(b'\n', pos)
        line = data[pos:end]
        pos = end + 1
        if line == b'':
            break
        header.append(line.decode('latin1'))
    end = data.index(b'\n', pos)
    res = data[pos:end].decode('latin1').split()
    pos = end + 1
    assert res[0] == '-Y' and res[2] == '+X', res
    h, w = int(res[1]), int(res[3])
    buf = np.frombuffer(data, dtype=np.uint8, offset=pos)
    out = np.zeros((h, w, 4), np.uint8)
    p = 0
    for y in range(h):
        if w < 8 or w > 0x7fff or buf[p] != 2 or buf[p+1] != 2 or (buf[p+2] & 0x80):
            # flat
            out[y] = buf[p:p+w*4].reshape(w, 4); p += w*4; continue
        assert (int(buf[p+2]) << 8 | int(buf[p+3])) == w
        p += 4
        for c in range(4):
            x = 0
            row = out[y, :, c]
            while x < w:
                n = int(buf[p]); p += 1
                if n > 128:
                    n -= 128
                    row[x:x+n] = buf[p]; p += 1
                else:
                    row[x:x+n] = buf[p:p+n]; p += n
                x += n
    e = out[..., 3].astype(np.int32)
    scale = np.where(e > 0, np.ldexp(1.0, e - 136), 0.0)
    rgb = out[..., :3].astype(np.float64) * scale[..., None]
    return rgb.astype(np.float32), header

def to_rgbe(rgb):
    rgb = np.maximum(rgb.astype(np.float64), 0)
    m = rgb.max(axis=2)
    mant, ex = np.frexp(m)
    valid = m > 1e-32
    sc = np.where(valid, mant * 256.0 / np.where(valid, m, 1), 0)
    out = np.zeros(rgb.shape[:2] + (4,), np.uint8)
    out[..., :3] = np.clip(np.floor(rgb * sc[..., None]), 0, 255).astype(np.uint8)
    out[..., 3] = np.where(valid, ex + 128, 0).astype(np.uint8)
    return out

def _rle_channel(row):
    out = bytearray()
    w = len(row); x = 0
    while x < w:
        # find run
        run = 1
        while x + run < w and run < 127 and row[x+run] == row[x]:
            run += 1
        if run >= 3:
            out.append(128 + run); out.append(int(row[x])); x += run; continue
        # literal: extend until a run of >=3 starts
        start = x; n = 0
        while x < w and n < 128:
            if x + 2 < w and row[x] == row[x+1] == row[x+2]:
                break
            x += 1; n += 1
        out.append(n); out.extend(bytes(row[start:start+n]))
    return out

def write_hdr(path, rgb, comment='Generated'):
    h, w, _ = rgb.shape
    q = to_rgbe(rgb)
    hdr = b'#?RADIANCE\n# ' + comment.encode('ascii') + b'\nFORMAT=32-bit_rle_rgbe\n\n' + f'-Y {h} +X {w}\n'.encode()
    body = bytearray()
    for y in range(h):
        body += bytes([2, 2, (w >> 8) & 0xff, w & 0xff])
        for c in range(4):
            body += _rle_channel(q[y, :, c].tobytes())
    with open(path, 'wb') as f:
        f.write(hdr); f.write(body)

def preview(rgb, exposure=1.0, gamma=2.2):
    x = rgb * exposure
    x = x / (1 + x)  # Reinhard
    x = np.clip(x, 0, 1) ** (1 / gamma)
    return (x * 255 + 0.5).astype(np.uint8)
