import sys, os, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from hdrio import read_hdr, write_hdr
src, dst, comment = sys.argv[1], sys.argv[2], sys.argv[3]
rgb, _ = read_hdr(src)
h, w, _ = rgb.shape
d = rgb.astype(np.float64).reshape(h // 2, 2, w // 2, 2, 3).mean(axis=(1, 3))
write_hdr(dst, d.astype(np.float32), comment)
back, _ = read_hdr(dst)
print(dst, back.shape, 'max rel err', float(np.max(np.abs(back - d) / (d.max(axis=2, keepdims=True) + 1e-9))))
