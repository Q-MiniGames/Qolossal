"""Head size of Qori in a frame: the cream face dome, as one connected piece (ears, tunic and staff are separate).
Used to size every Ludo clip the same: body height changes with the pose (crouch, stretch, wings), the head does not.
Usage: python ludo_head_size.py <frames_dir> ...   (prints the median face height and width per folder)
"""
import glob, os, sys
import cv2
import numpy as np
from PIL import Image


def face_box(path):
    a = np.array(Image.open(path).convert('RGBA'))
    m = a[:, :, 3] > 200
    if not m.any(): return None
    r = np.where(m.any(1))[0]; top, bot = r.min(), r.max()
    rgb = a[:, :, :3].astype(int)
    cream = m & (rgb.min(axis=2) > 185) & (rgb[:, :, 0] >= rgb[:, :, 2])
    cream[int(top + .55 * (bot - top)):] = False
    cream = cv2.morphologyEx(cream.astype(np.uint8), cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    # the eyes: the face is the cream piece that surrounds the darkest blobs in the head
    dark = (m & (rgb.max(axis=2) < 60)).astype(np.uint8); dark[int(top + .55 * (bot - top)):] = 0
    n, lab, st, cen = cv2.connectedComponentsWithStats(cream)
    if n < 2: return None
    dys, dxs = np.where(dark)
    best, score = None, -1
    for c in range(1, n):
        x, y, w, h, area = st[c]
        if area < 400: continue
        # how many eye pixels sit inside this piece's box (the face encloses the eyes)
        inside = ((dxs >= x) & (dxs < x + w) & (dys >= y) & (dys < y + h)).sum() if len(dxs) else 0
        s = inside * 10 + area
        if s > score: best, score = (w, h, area), s
    return best


def clip_head(folder, step=1):
    fs = sorted(glob.glob(os.path.join(folder, '*.png')))[::step]
    boxes = [b for b in (face_box(f) for f in fs) if b]
    if not boxes: return None
    return float(np.median([b[1] for b in boxes])), float(np.median([b[0] for b in boxes])), float(np.median([np.sqrt(b[2]) for b in boxes]))


if __name__ == '__main__':
    for d in sys.argv[1:]:
        print(os.path.basename(d.rstrip('/\\')), clip_head(d, 2))
