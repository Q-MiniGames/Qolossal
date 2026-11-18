"""Cuts the ambient Ludo clips into game frames under Assets/Art/Ludo. Run sheets_ambient.py first.
Bird: Perch (sits and crouches), Rise (leaves the twig), Flap (one wingbeat loop), all on one canvas so nothing jumps,
and Twig, the perch on its own, because Ludo left it floating behind the bird. Splash: the churn with no stream and no
tinted ripple, closed with a cross-fade. Butterfly: the still, flapped in code (Ludo's own flap barely moved).
"""
import glob, os
import cv2
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
IN = os.path.join(ROOT, 'Tools', 'IncomingArt', 'Ludo', 'Ambient')
OUT = os.path.join(ROOT, 'Assets', 'Art', 'Ludo')
PERCH, RISE = (0, 9), (9, 13)
FLAP_FROM, FLAP_MIN, FLAP_MAX = 13, 9, 13     # where the flight settles; a wingbeat is about this many frames
SPLASH_SECTION, SPLASH_LOOP, SPLASH_FADE = (18, 29), 8, 3
BIRD_WIDTH, SPLASH_WIDTH, BUTTERFLY_WIDTH = 160, 256, 128


def load(name):
    return [np.asarray(Image.open(p).convert('RGBA')).copy() for p in sorted(glob.glob(os.path.join(IN, name, 'frames', '*.png')))]


def parts(frame):
    """Separate pieces of the picture, largest first, as boolean masks."""
    count, labels, stats, _ = cv2.connectedComponentsWithStats((frame[:, :, 3] > 32).astype(np.uint8), connectivity=8)
    order = sorted(range(1, count), key=lambda i: -stats[i, cv2.CC_STAT_AREA])
    return [(labels == i) for i in order], [int(stats[i, cv2.CC_STAT_AREA]) for i in order]


def keep(frame, mask):
    out = frame.copy()
    grown = cv2.dilate(mask.astype(np.uint8), np.ones((5, 5), np.uint8)) > 0
    out[~grown] = 0
    return out


def save_set(frames, folder, name, width):
    alpha = np.max([f[:, :, 3] for f in frames], axis=0)
    ys, xs = np.where(alpha > 8)
    pad = 4
    x0, x1, y0, y1 = max(xs.min() - pad, 0), xs.max() + pad + 1, max(ys.min() - pad, 0), ys.max() + pad + 1
    return (x0, y0, x1, y1), width / (x1 - x0)


def write(frame, box, scale, path):
    x0, y0, x1, y1 = box
    im = Image.fromarray(frame[y0:y1, x0:x1], 'RGBA')
    # Premultiplied while scaling, so no pale fringe comes from the transparent pixels.
    a = np.asarray(im, dtype=np.float32)
    a[:, :, :3] *= a[:, :, 3:] / 255
    size = (max(1, round(im.width * scale)), max(1, round(im.height * scale)))
    a = cv2.resize(a, size, interpolation=cv2.INTER_AREA)
    a[:, :, :3] /= np.maximum(a[:, :, 3:] / 255, 1 / 255)
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), 'RGBA').save(path)


def bird():
    frames = load('Bird')
    target = os.path.join(OUT, 'Bird'); os.makedirs(target, exist_ok=True)
    clean, twig, twig_at = [], None, None
    for i, f in enumerate(frames):
        masks, areas = parts(f)
        if i >= RISE[0] and len(areas) > 1 and areas[1] > 150 and twig is None:
            twig, twig_at = keep(f, masks[1]), i
        clean.append(f if i < RISE[0] else keep(f, masks[0]))
    gray = [c[:, :, 3].astype(np.float32) / 255 for c in clean]
    best = min(((float(np.abs(gray[a] - gray[a + n]).mean()), a, n) for a in range(FLAP_FROM, 20) for n in range(FLAP_MIN, FLAP_MAX + 1) if a + n < len(gray)), key=lambda t: t[0])
    _, a, n = best
    flap = clean[a:a + n]
    sets = clean[PERCH[0]:PERCH[1]] + clean[RISE[0]:RISE[1]] + flap + ([twig] if twig is not None else [])
    box, scale = save_set(sets, target, 'Bird', BIRD_WIDTH)
    for k, f in enumerate(clean[PERCH[0]:PERCH[1]]): write(f, box, scale, os.path.join(target, 'Bird_Perch_%02d.png' % k))
    for k, f in enumerate(clean[RISE[0]:RISE[1]]): write(f, box, scale, os.path.join(target, 'Bird_Rise_%02d.png' % k))
    for k, f in enumerate(flap): write(f, box, scale, os.path.join(target, 'Bird_Flap_%02d.png' % k))
    if twig is not None: write(twig, box, scale, os.path.join(target, 'Bird_Twig.png'))
    alpha = clean[0][box[1]:box[3], box[0]:box[2], 3]
    bottom = np.where(alpha.max(axis=1) > 32)[0].max()
    print('bird: flap %d frames from %d (seam %.4f), twig from frame %s; canvas %dx%d at %.3f; perch foot %.3f of the way down' % (n, a, best[0], twig_at, box[2] - box[0], box[3] - box[1], scale, bottom / alpha.shape[0]))


def splash():
    frames = load('Splash')[SPLASH_SECTION[0]:SPLASH_SECTION[1]]
    target = os.path.join(OUT, 'Splash'); os.makedirs(target, exist_ok=True)
    arrays = np.stack([f.astype(np.float32) for f in frames])
    loop = arrays[:SPLASH_LOOP].copy()
    for k in range(SPLASH_FADE):
        t = (k + 1) / (SPLASH_FADE + 1)
        loop[k] = arrays[SPLASH_LOOP + k] * (1 - t) + arrays[k] * t
    loop = loop.clip(0, 255).astype(np.uint8)
    box, scale = save_set(list(loop), target, 'Splash', SPLASH_WIDTH)
    for k, f in enumerate(loop): write(f, box, scale, os.path.join(target, 'Splash_%02d.png' % k))
    alpha = loop[0][box[1]:box[3], box[0]:box[2], 3]
    print('splash: %d frames, canvas %dx%d at %.3f; base %.3f of the way down' % (len(loop), box[2] - box[0], box[3] - box[1], scale, np.where(alpha.max(axis=1) > 32)[0].max() / alpha.shape[0]))


def butterfly():
    target = os.path.join(OUT, 'Butterfly'); os.makedirs(target, exist_ok=True)
    f = np.asarray(Image.open(os.path.join(IN, 'Butterfly_Still.png')).convert('RGBA')).copy()
    masks, _ = parts(f)
    f = keep(f, masks[0])
    box, scale = save_set([f], target, 'Butterfly', BUTTERFLY_WIDTH)
    write(f, box, scale, os.path.join(target, 'Butterfly.png'))
    print('butterfly: canvas %dx%d' % (box[2] - box[0], box[3] - box[1]))


bird(); splash(); butterfly()
