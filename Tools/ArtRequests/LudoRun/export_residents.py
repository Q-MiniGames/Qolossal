"""Cuts the residents' Ludo idles into game frames under Assets/Art/Ludo/Residents/<Rig>: every second frame of the
clip's steady part, scaled to 216 px per unit and to the figure's true size, on a canvas with the feet at bottom centre,
so a sprite pivot of bottom centre stands the figure where its jointed version stood.
Usage: python export_residents.py [Rig ...]
"""
import glob, json, os, sys
import cv2
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
IN = os.path.join(ROOT, 'Tools', 'IncomingArt', 'Ludo', 'Residents')
OUT = os.path.join(ROOT, 'Assets', 'Art', 'Ludo', 'Residents')
PPU = 216
STEP = 2     # every second frame: 18 of the 36, played at 8 a second
feet = json.load(open(os.path.join(IN, 'feet.json')))


def bbox(a, threshold=32):
    ys, xs = np.where(a[:, :, 3] > threshold)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def premultiplied_resize(a, size):
    a = a.astype(np.float32)
    a[:, :, :3] *= a[:, :, 3:] / 255
    a = cv2.resize(a, size, interpolation=cv2.INTER_AREA)
    a[:, :, :3] /= np.maximum(a[:, :, 3:] / 255, 1 / 255)
    return np.clip(a, 0, 255).astype(np.uint8)


total = 0
for rig in (sys.argv[1:] or sorted(feet)):
    paths = sorted(glob.glob(os.path.join(IN, rig, 'frames', 'frame_*.png')))
    if not paths: print('no frames:', rig); continue
    # The clip ends where it starts; the last frame repeats the first.
    frames = [np.asarray(Image.open(p).convert('RGBA')) for p in paths[:-1][::STEP]]
    x0, y0, x1, y1 = bbox(frames[0])
    info = feet[rig]
    scale = info['unitsTall'] * PPU / (y1 - y0)
    # Where the feet are in Ludo's frame: the same share across and at the bottom of the first frame's figure.
    foot_x = x0 + (x1 - x0) * info['originFromLeft'] / info['unitsWide']
    foot_y = y1 - (y1 - y0) * info['originFromBottom'] / info['unitsTall']
    h, w = frames[0].shape[:2]
    half = max(foot_x, w - foot_x)
    # A canvas symmetric about the feet, ending at them (anything below the feet is at most a soft shadow).
    left = int(np.floor(foot_x - half)); right = int(np.ceil(foot_x + half))
    target = os.path.join(OUT, rig); os.makedirs(target, exist_ok=True)
    for old in glob.glob(os.path.join(target, '*.png')): os.remove(old)
    size = (max(2, round((right - left) * scale)), max(2, round(foot_y * scale)))
    for i, f in enumerate(frames):
        pad = np.zeros((int(np.ceil(foot_y)), right - left, 4), dtype=np.uint8)
        src = f[:int(np.ceil(foot_y)), max(left, 0):min(right, w)]
        pad[:src.shape[0], max(0, -left):max(0, -left) + src.shape[1]] = src
        small = premultiplied_resize(pad, size)
        Image.fromarray(small, 'RGBA').save(os.path.join(target, '%s_Idle_%02d.png' % (rig, i)))
    # BC7-class compression is 1 byte a pixel, before mips.
    bytes_each = size[0] * size[1]
    total += bytes_each * len(frames)
    print('%s: %d frames %dx%d, about %.1f MB compressed; figure %.2f u tall' % (rig, len(frames), size[0], size[1], bytes_each * len(frames) / 1e6, info['unitsTall']))
print('all: about %.1f MB; each scene loads only the figures it holds' % (total / 1e6))
