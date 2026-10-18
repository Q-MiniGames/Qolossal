"""Turns a Ludo.ai frame export (transparent PNGs, frame_000.png ...) into one steady game loop for the Qori test scene.

Not art: loop selection and registration of a generated animation.
- Loop: the start and length (within lengths) whose last frame leads back into the first most smoothly.
- Steady: the face held on one column (the body's width swings with the cloak); each frame's lowest foot on one ground row for frames where a foot is down,
  airborne frames eased between them (run). Specks and leaves floating free of the body are removed.
- Stride: how far a planted foot travels back per frame, relative to the torso, times the loop length.
Usage: python clean_ludo_frames.py <frames_dir> <out_dir> <name> <minLen> <maxLen> [contactTolPx]
"""
import glob, json, os, sys
import numpy as np
from PIL import Image

SRC, OUT, NAME = sys.argv[1], sys.argv[2], sys.argv[3]
LMIN, LMAX = int(sys.argv[4]), int(sys.argv[5])
CONTACT_TOL = int(sys.argv[6]) if len(sys.argv) > 6 else 6
os.makedirs(OUT, exist_ok=True)
files = sorted(glob.glob(os.path.join(SRC, 'frame_*.png')))
imgs = [np.array(Image.open(f).convert('RGBA')) for f in files]
H, W = imgs[0].shape[:2]


def main_body(a):
    """Alpha with only the body (largest piece and anything touching it); free-floating leaves and specks removed."""
    m = a > 40
    seed = np.zeros_like(m)
    ys, xs = np.where(a > 250)
    k = int(np.argmin((ys - ys.mean()) ** 2 + (xs - xs.mean()) ** 2))   # the opaque pixel nearest the figure's centre
    seed[ys[k], xs[k]] = True
    reg = seed
    while True:   # grow through connected pixels (8-connected, by dilation)
        g = reg.copy()
        g[1:] |= reg[:-1]; g[:-1] |= reg[1:]; g[:, 1:] |= reg[:, :-1]; g[:, :-1] |= reg[:, 1:]
        g &= m
        if g.sum() == reg.sum(): break
        reg = g
    out = a.copy(); out[~reg] = 0
    return out


alphas = [main_body(im[:, :, 3]) for im in imgs]
M = [a > 128 for a in alphas]
n = len(M)
D = np.array([[(M[i] ^ M[j]).sum() / max(M[i].sum(), 1) for j in range(n)] for i in range(n)])
step = float(np.median([D[i, i + 1] for i in range(n - 1)]))
best = min(((D[i, i + L], i, L) for L in range(LMIN, LMAX + 1) for i in range(n - L)), key=lambda x: x[0])
_, START, LEN = best
idx = list(range(START, START + LEN))

rows = lambda m: np.where(m.any(1))[0]
lows = [rows(M[i]).max() for i in idx]
# Horizontal anchor: the face (the cream head), not the body's width, which the swinging cloak and arms skew.
def face_x(i):
    im = imgs[i]; m = M[i]
    r = rows(m); top, bot = r.min(), r.max()
    cream = m & (im[:, :, :3].min(axis=2) > 200)
    cream[int(top + .45 * (bot - top)):] = False             # head only
    ys, xs = np.where(cream)
    return float(np.median(xs))
torso_x = [face_x(i) for i in idx]
contact = [l >= max(lows) - CONTACT_TOL for l in lows]
offy = [(-lows[k] if contact[k] else None) for k in range(LEN)]
for k in range(LEN):
    if offy[k] is None:
        p = next(d for d in range(1, LEN) if contact[(k - d) % LEN]); q = next(d for d in range(1, LEN) if contact[(k + d) % LEN])
        offy[k] = offy[(k - p) % LEN] + (offy[(k + q) % LEN] - offy[(k - p) % LEN]) * p / (p + q)
# Vertical: the head's path smoothed to its first two harmonics (two strides per loop), so it bobs without jolts;
# no frame may sink its feet more than 2 px below the ground line.
def face_y(i):
    im = imgs[i]; m = M[i]
    r = rows(m); top, bot = r.min(), r.max()
    cream = m & (im[:, :, :3].min(axis=2) > 200); cream[int(top + .45 * (bot - top)):] = False
    return float(np.median(np.where(cream)[0]))
hy = np.array([face_y(i) + offy[k] for k, i in enumerate(idx)])
F = np.fft.rfft(hy); F[3:] = 0; smooth = np.fft.irfft(F, LEN)
for k in range(LEN):
    offy[k] += min(smooth[k] - hy[k], 2.0 + (-lows[k] - offy[k]))   # (-lows - offy) = how far the feet are above the ground
# planted-foot travel: the lowest 12 rows, the piece nearest the previous frame's foot
foot_x = []
for k, i in enumerate(idx):
    b = M[i][lows[k] - 12:lows[k] + 1]; xs = np.where(b.any(0))[0]
    foot_x.append((xs.min() + xs.max()) / 2 - torso_x[k] if len(xs) else np.nan)
speeds = [foot_x[k] - foot_x[k + 1] for k in range(LEN - 1) if contact[k] and contact[k + 1] and 0 < foot_x[k] - foot_x[k + 1] < 60]
speed = float(np.median(speeds)) if speeds else float('nan')

PAD = 40
CW, CH = W + 2 * PAD, H + 2 * PAD
GROUND, PIVOT = CH - PAD, CW // 2
meta = []
for k, i in enumerate(idx):
    im = imgs[i].copy(); im[:, :, 3] = alphas[i]; im[im[:, :, 3] == 0, :3] = 0
    out = Image.new('RGBA', (CW, CH), (0, 0, 0, 0))
    out.paste(Image.fromarray(im), (int(round(PIVOT - torso_x[k])), int(round(GROUND + offy[k]))))
    out.save(os.path.join(OUT, 'Qori_%s_%02d.png' % (NAME, k + 1)))
    meta.append(dict(frame=k + 1, source=os.path.basename(files[i]), contact=bool(contact[k])))
fig = np.array(Image.open(os.path.join(OUT, 'Qori_%s_%02d.png' % (NAME, 1))))[:, :, 3] > 128
r = rows(fig)
info = dict(source=SRC, name=NAME, loopStart=START, frameCount=LEN, loopClosure=round(float(best[0]), 3), medianStep=round(step, 3),
            canvas=[CW, CH], groundRow=GROUND, pivotColumn=PIVOT, figureHeightPx=int(r.max() - r.min() + 1),
            plantedFootPxPerFrame=round(speed, 2), strideCanvasPx=round(speed * LEN, 1), frames=meta)
json.dump(info, open(os.path.join(OUT, 'Qori_%s_info.json' % NAME), 'w'), indent=1)
print(json.dumps({k: v for k, v in info.items() if k != 'frames'}))
