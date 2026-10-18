"""Cuts the Ludo.ai Thornheart clips (the wide v4 model) into the animations ThornheartBoss plays.

Not art: cutting and registration of generated animations. Both clips are put on one canvas so they line up: the
v4-4 clip (hurt and death) is drawn about 8% larger than v4-1, so it is scaled to v4-1's idle width, and each clip's
first frame (the same idle pose in both) has its feet centre placed on one point. Writes
Assets/QoriWalkTest/ArtThornheart/<Segment>/TH_<Segment>_NN.png and TH_<Segment>_info.json (canvas, ground row, pivot
column, body height, heart position).
Usage: python export_thornheart.py <Thornheart dir> <out dir>
"""
import json, os, sys
import numpy as np
from PIL import Image

SRC, OUT = sys.argv[1], sys.argv[2]
CW, CH, PIVOT_X, GROUND = 760, 520, 380, 470   # the canvas, and where the idle's feet centre sits on it

# segment: (clip folder, frames)
SEGMENTS = {
    'Idle':  ('Thornheart_v4-1', list(range(0, 12))),    # breathing, the heart slowly lighting up
    'Raise': ('Thornheart_v4-1', list(range(12, 26))),   # the right claw rises and opens like a jaw (wind-up and roar)
    'Hurt':  ('Thornheart_v4-4', list(range(0, 12))),    # the heart bursts with light
    'Death': ('Thornheart_v4-4', list(range(12, 36))),   # the collapse
}


def load(folder, i):
    return Image.open(os.path.join(SRC, folder, 'frame_%03d.png' % i)).convert('RGBA')


def feet(img):
    a = np.array(img)[:, :, 3] > 128
    ys, xs = np.where(a)
    return (xs.min() + xs.max()) / 2, ys.max(), xs.max() - xs.min(), ys.max() - ys.min()


ref = feet(load('Thornheart_v4-1', 0))
placement = {}
for folder in ('Thornheart_v4-1', 'Thornheart_v4-4'):
    cx, bottom, w, h = feet(load(folder, 0))
    s = ref[2] / w
    placement[folder] = (s, PIVOT_X - cx * s, GROUND - bottom * s)

# the heart: the bright green glow in the idle's brightest frame, on the canvas
a = np.array(load('Thornheart_v4-1', 11)).astype(int)
g = (a[:, :, 3] > 128) & (a[:, :, 1] > 170) & (a[:, :, 1] > a[:, :, 0] + 50) & (a[:, :, 1] > a[:, :, 2] + 40)
gy, gx = np.where(g)
s, ox, oy = placement['Thornheart_v4-1']
heart = [round(float(np.median(gx)) * s + ox, 1), round(float(np.median(gy)) * s + oy, 1)]

for name, (folder, frames) in SEGMENTS.items():
    s, ox, oy = placement[folder]
    d = os.path.join(OUT, name); os.makedirs(d, exist_ok=True)
    for k, i in enumerate(frames):
        img = load(folder, i)
        if s != 1: img = img.resize((round(img.width * s), round(img.height * s)), Image.LANCZOS)
        canvas = Image.new('RGBA', (CW, CH), (0, 0, 0, 0))
        canvas.alpha_composite(img, (round(ox), round(oy)))
        arr = np.array(canvas); arr[arr[:, :, 3] == 0, :3] = 0
        Image.fromarray(arr).save(os.path.join(d, 'TH_%s_%02d.png' % (name, k + 1)))
    info = dict(name=name, source=os.path.join(SRC, folder), frames=frames, frameCount=len(frames), canvas=[CW, CH],
                groundRow=GROUND, pivotColumn=PIVOT_X, figureHeightPx=float(ref[3]), figureWidthPx=float(ref[2]),
                heartPx=heart, scale=round(s, 4))
    json.dump(info, open(os.path.join(d, 'TH_%s_info.json' % name), 'w'), indent=1)
    print(name, len(frames), 'scale', round(s, 3))
print('idle body', ref[2], 'x', ref[3], 'px; heart at', heart)
