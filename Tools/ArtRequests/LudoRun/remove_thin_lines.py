"""Removes thin lines Ludo drew off the figure (the ledge clip's 'thread' from the hanging hand).

Not art: a cleanup. A line is what a 5 px opening of the alpha removes and runs long (over 30 px) in one direction;
the figure's own thin details (fingers, leaf tips, toes) are short and stay. Edits PNGs in place.
Usage: python remove_thin_lines.py <frames_dir> ...
"""
import glob, os, sys
import cv2
import numpy as np
from PIL import Image


def clean(path, min_len=30):
    a = np.array(Image.open(path).convert('RGBA'))
    m = (a[:, :, 3] > 10).astype(np.uint8)
    opened = cv2.morphologyEx(m, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5)))
    thin = (m & (1 - opened)).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(thin)
    kill = np.zeros_like(thin)
    for c in range(1, n):
        if max(st[c, cv2.CC_STAT_WIDTH], st[c, cv2.CC_STAT_HEIGHT]) >= min_len:
            kill[lab == c] = 1
    if not kill.any(): return 0
    kill = cv2.dilate(kill, np.ones((3, 3), np.uint8)) & (1 - opened)   # its soft edge, never the body
    a[kill > 0] = 0
    Image.fromarray(a).save(path)
    return int(kill.sum())


if __name__ == '__main__':
    for d in sys.argv[1:]:
        c = [clean(f) for f in sorted(glob.glob(os.path.join(d, '*.png')))]
        print(os.path.basename(d.rstrip('/\\')), 'frames with a line removed:', sum(1 for v in c if v), 'of', len(c))
