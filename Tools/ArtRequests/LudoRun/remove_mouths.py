"""Paints out a mouth Ludo drew on Qori's face (Qori has no mouth: the model has eyes, brows and a tiny nose only).

Not art: a cleanup. Per frame: the cream face is found (one connected piece, as in ludo_head_size.py), the eyes are its
largest dark blobs, and any dark marks in the face below the eyes (a smile, an open mouth) are filled from the
surrounding cream by inpainting. The nose (tiny, at eye height on the front edge) and the far eye are kept. Edits PNGs in place.
Usage: python remove_mouths.py <frames_dir> ...
"""
import glob, os, sys
import cv2
import numpy as np
from PIL import Image


DARK, RIM = 125, 5


def clean(path):
    a = np.array(Image.open(path).convert('RGBA'))
    m = a[:, :, 3] > 200
    r = np.where(m.any(1))[0]; top, bot = r.min(), r.max()
    rgb = a[:, :, :3].astype(int)
    cream = (m & (rgb.min(axis=2) > 185)).astype(np.uint8); cream[int(top + .55 * (bot - top)):] = 0
    cream = cv2.morphologyEx(cream, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    # close over the eyes, nose and mouth so the face is one solid piece (they can split the cream in two)
    cream = cv2.morphologyEx(cream, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (31, 31)))
    n, lab, st, _ = cv2.connectedComponentsWithStats(cream)
    if n < 2: return 0
    c = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
    x, y, w, h, _ = st[c]
    # the face region: the cream piece with its holes (eyes, nose, mouth) filled
    face = (lab == c).astype(np.uint8)
    cnts, _ = cv2.findContours(face, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    filled = np.zeros_like(face); cv2.drawContours(filled, cnts, -1, 1, -1)
    filled = cv2.erode(filled, np.ones((5, 5), np.uint8))
    dark = ((rgb.max(axis=2) < DARK) & (filled > 0)).astype(np.uint8)
    dn, dlab, dst, dcen = cv2.connectedComponentsWithStats(dark)
    if dn < 2: return 0
    # the main eye is the largest dark blob; a mark whose centre sits well below it is a mouth. The nose (tiny, at eye
    # height on the front edge) and the far eye (level with the main eye) stay.
    eye = max(range(1, dn), key=lambda k: dst[k, cv2.CC_STAT_AREA])
    eye_cy = dcen[eye][1]
    mouth = np.zeros_like(dark)
    for k in range(1, dn):
        if k == eye: continue
        if dcen[k][1] > eye_cy + .18 * h and 12 <= dst[k, cv2.CC_STAT_AREA] < .6 * dst[eye, cv2.CC_STAT_AREA]:
            mouth[dlab == k] = 1
    # the soft rim of the mouth line too
    mouth = cv2.dilate(mouth, np.ones((RIM, RIM), np.uint8)) & filled
    if mouth.sum() == 0: return 0
    fixed = cv2.inpaint(np.ascontiguousarray(a[:, :, :3]), mouth * 255, 6, cv2.INPAINT_TELEA)
    a[:, :, :3] = np.where(mouth[..., None] > 0, fixed, a[:, :, :3])
    Image.fromarray(a).save(path)
    return int(mouth.sum())


if __name__ == '__main__':
    # arguments: <frames_dir>[@first-last] ... (1-based frame numbers; run it only on frames where Ludo drew a mouth)
    args = sys.argv[1:]
    if args and args[0] == '--strong':          # a second pass for faint leftovers: lighter marks, wider rim
        DARK, RIM = 155, 7; args = args[1:]
    for arg in args:
        d, _, rng = arg.partition('@')
        fs = sorted(glob.glob(os.path.join(d, '*.png')))
        if rng:
            lo, hi = (int(v) for v in rng.split('-')); fs = fs[lo - 1:hi]
        counts = [clean(f) for f in fs]
        print(os.path.basename(d.rstrip('/\\')), rng or 'all', 'frames cleaned:', sum(1 for c in counts if c), 'of', len(counts))
