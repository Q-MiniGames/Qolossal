"""Exports a Ludo.ai idle (transparent PNGs, frame_000.png ...) for the Qori test scene, unshifted.

Not art. Ludo holds the camera and feet still in an idle, so frames are not re-registered: they are padded onto one
canvas, the ground row put at the feet's lowest standing row and the pivot between the feet. The falling leaves stay
(Qori stands still, so they fall in place). The loop is the whole export (the last frame leads back into the first).
Usage: python export_ludo_idle.py <frames_dir> <out_dir> [cycleSeconds]
"""
import glob, json, os, sys
import numpy as np
from PIL import Image

SRC, OUT = sys.argv[1], sys.argv[2]
CYCLE = float(sys.argv[3]) if len(sys.argv) > 3 else 2.4
os.makedirs(OUT, exist_ok=True)
files = sorted(glob.glob(os.path.join(SRC, 'frame_*.png')))
imgs = [Image.open(f).convert('RGBA') for f in files]
W, H = imgs[0].size
masks = [np.array(im)[:, :, 3] > 128 for im in imgs]
# standing feet: the most common lowest row (leaves falling below the feet are ignored), and the feet's span there
lows = [int(np.where(m.any(1))[0].max()) for m in masks]
ground = int(np.bincount(lows).argmax())
spans = []
for m, low in zip(masks, lows):
    if low != ground: continue
    xs = np.where(m[ground - 15:ground + 1].any(0))[0]; spans.append((xs.min(), xs.max()))
fx0, fx1 = np.median([s[0] for s in spans]), np.median([s[1] for s in spans])
PAD = 20
CW, CH = W + 2 * PAD, H + 2 * PAD
for k, im in enumerate(imgs):
    a = np.array(im); a[a[:, :, 3] == 0, :3] = 0
    out = Image.new('RGBA', (CW, CH), (0, 0, 0, 0)); out.paste(Image.fromarray(a), (PAD, PAD))
    out.save(os.path.join(OUT, 'Qori_LudoIdle_%02d.png' % (k + 1)))
standing = [m for m, low in zip(masks, lows) if low == ground]
top = int(np.median([np.where(m.any(1))[0].min() for m in standing]))
info = dict(source=SRC, name='LudoIdle', frameCount=len(imgs), cycleSeconds=CYCLE, canvas=[CW, CH], groundRow=ground + PAD,
            pivotColumn=int(round((fx0 + fx1) / 2)) + PAD, figureHeightPx=ground - top + 1, strideCanvasPx=0)
json.dump(info, open(os.path.join(OUT, 'Qori_LudoIdle_info.json'), 'w'), indent=1)
print(json.dumps(info))
