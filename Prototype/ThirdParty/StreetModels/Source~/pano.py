"""Equirectangular HDR helpers: decode (via ffmpeg, Radiance .hdr) and perspective crops."""
import subprocess
import numpy as np


def load_hdr(path):
    """Radiance .hdr -> float32 HxWx3 linear RGB (ffmpeg's hdr decoder, planar G,B,R float)."""
    probe = subprocess.run(['ffprobe', '-v', 'error', '-show_entries', 'stream=width,height', '-of', 'csv=p=0', path],
                           capture_output=True, text=True, check=True).stdout.split()[0]
    w, h = [int(x) for x in probe.split(',')]
    raw = subprocess.run(['ffmpeg', '-v', 'quiet', '-i', path, '-frames:v', '1', '-f', 'rawvideo', '-pix_fmt', 'gbrpf32le', '-'],
                         capture_output=True, check=False).stdout
    a = np.frombuffer(raw[:w * h * 12], np.float32).reshape(3, h, w)
    return np.ascontiguousarray(np.stack([a[2], a[0], a[1]], -1))


def bilinear(img, x, y):
    h, w = img.shape[:2]
    x = np.mod(x, w); y = np.clip(y, 0, h - 1.001)
    x0 = np.floor(x).astype(int); y0 = np.floor(y).astype(int)
    fx = (x - x0)[..., None]; fy = (y - y0)[..., None]
    x1 = (x0 + 1) % w; y1 = np.minimum(y0 + 1, h - 1)
    return (img[y0, x0] * (1 - fx) * (1 - fy) + img[y0, x1] * fx * (1 - fy) +
            img[y1, x0] * (1 - fx) * fy + img[y1, x1] * fx * fy)


def perspective(pano, yaw_deg, pitch_deg, hfov_deg, w, h, vfov_deg=None):
    """Pinhole view from the panorama centre. Yaw increases to the right in the panorama (x = yaw / 360 * W)."""
    H, W = pano.shape[:2]
    yaw, pit = np.radians(yaw_deg), np.radians(pitch_deg)
    tx = np.tan(np.radians(hfov_deg) / 2)
    ty = np.tan(np.radians(vfov_deg) / 2) if vfov_deg else tx * h / w
    xs = (np.arange(w) + 0.5) / w * 2 - 1
    ys = 1 - (np.arange(h) + 0.5) / h * 2
    X, Y = np.meshgrid(xs * tx, ys * ty)
    fwd = np.array([np.sin(yaw) * np.cos(pit), np.sin(pit), np.cos(yaw) * np.cos(pit)])
    right = np.array([np.cos(yaw), 0.0, -np.sin(yaw)])
    up = np.cross(fwd, right) * -1
    up = np.array([-np.sin(yaw) * np.sin(pit), np.cos(pit), -np.cos(yaw) * np.sin(pit)])
    d = fwd[None, None] + X[..., None] * right[None, None] + Y[..., None] * up[None, None]
    d /= np.linalg.norm(d, axis=-1, keepdims=True)
    lon = np.arctan2(d[..., 0], d[..., 2])
    lat = np.arcsin(np.clip(d[..., 1], -1, 1))
    px = lon / (2 * np.pi) * W
    py = (0.5 - lat / np.pi) * H
    return bilinear(pano, px, py)


def project(yaw_deg, pitch_deg, hfov_deg, w, h, p_yaw, p_pitch, vfov_deg=None):
    """Image position (x, y in pixels) of panorama direction (p_yaw, p_pitch) in the perspective view, or None."""
    yaw, pit = np.radians(yaw_deg), np.radians(pitch_deg)
    tx = np.tan(np.radians(hfov_deg) / 2)
    ty = np.tan(np.radians(vfov_deg) / 2) if vfov_deg else tx * h / w
    fwd = np.array([np.sin(yaw) * np.cos(pit), np.sin(pit), np.cos(yaw) * np.cos(pit)])
    right = np.array([np.cos(yaw), 0.0, -np.sin(yaw)])
    up = np.array([-np.sin(yaw) * np.sin(pit), np.cos(pit), -np.cos(yaw) * np.sin(pit)])
    a, b = np.radians(p_yaw), np.radians(p_pitch)
    d = np.array([np.sin(a) * np.cos(b), np.sin(b), np.cos(a) * np.cos(b)])
    z = d @ fwd
    if z <= 0:
        return None
    x = (d @ right) / z / tx; y = (d @ up) / z / ty
    return ((x + 1) / 2 * w, (1 - y) / 2 * h)
