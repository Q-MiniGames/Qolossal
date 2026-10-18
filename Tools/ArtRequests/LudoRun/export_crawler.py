"""Cuts the Ludo.ai Bramble Crawler clips into the animations CrawlerFramesOnCreature plays (test room).

Not art: cutting and registration of generated animations. Ludo drew the three clips at different sizes, so each is
scaled to the walk's first frame (the average of the width and height ratios of each clip's standing first frame),
and each clip's first frame has its feet centre placed on one point. Writes
Assets/QoriWalkTest/ArtCrawler/<Segment>/CR_<Segment>_NN.png and CR_<Segment>_info.json.
Usage: python export_crawler.py <Crawler dir> <out dir>
"""
import json, os, sys
import numpy as np
from PIL import Image

SRC, OUT = sys.argv[1], sys.argv[2]
CW, CH, PIVOT_X, GROUND = 900, 560, 450, 500
WALK, WARN, HURT = 'Cralwer - Walk', 'Crawler - Warning and charge', 'Crawler - Hurt and death'

SEGMENTS = {
    'Walk':   (WALK, list(range(0, 35))),    # one stride cycle (frame 35 is frame 0 again)
    'Warn':   (WARN, list(range(0, 11))),    # head lowers, jaw opens, back thorns bristle
    'Charge': (WARN, list(range(13, 25))),   # the gallop, one cycle (frame 25 is frame 13 again)
    'Hurt':   (HURT, list(range(1, 7))),     # rears back from the hit
    'Death':  (HURT, list(range(7, 27))),    # the collapse onto its side
}


def load(folder, i):
    return Image.open(os.path.join(SRC, folder, 'frame_%03d.png' % i)).convert('RGBA')


def box(img):
    a = np.array(img)[:, :, 3] > 128
    ys, xs = np.where(a)
    return (xs.min() + xs.max()) / 2, ys.max(), xs.max() - xs.min(), ys.max() - ys.min()


ref = box(load(WALK, 0))
placement = {}
for folder in (WALK, WARN, HURT):
    cx, bottom, w, h = box(load(folder, 0))
    s = (ref[2] / w + ref[3] / h) / 2
    placement[folder] = (s, PIVOT_X - cx * s, GROUND - bottom * s)

for name, (folder, frames) in SEGMENTS.items():
    s, ox, oy = placement[folder]
    if name == 'Charge':   # the gallop's feet reach below the standing pose's: lift it so its median low point is the ground
        lows = [box(load(folder, i))[1] * s + oy for i in frames]
        oy -= float(np.median(lows)) - GROUND
    d = os.path.join(OUT, name); os.makedirs(d, exist_ok=True)
    for k, i in enumerate(frames):
        img = load(folder, i)
        if abs(s - 1) > 1e-3: img = img.resize((round(img.width * s), round(img.height * s)), Image.LANCZOS)
        canvas = Image.new('RGBA', (CW, CH), (0, 0, 0, 0))
        canvas.alpha_composite(img, (round(ox), round(oy)))
        arr = np.array(canvas); arr[arr[:, :, 3] == 0, :3] = 0
        Image.fromarray(arr).save(os.path.join(d, 'CR_%s_%02d.png' % (name, k + 1)))
    info = dict(name=name, source=os.path.join(SRC, folder), frames=frames, frameCount=len(frames), canvas=[CW, CH],
                groundRow=GROUND, pivotColumn=PIVOT_X, figureHeightPx=float(ref[3]), figureWidthPx=float(ref[2]), scale=round(s, 4))
    json.dump(info, open(os.path.join(d, 'CR_%s_info.json' % name), 'w'), indent=1)
    print(name, len(frames), 'scale', round(s, 3))
