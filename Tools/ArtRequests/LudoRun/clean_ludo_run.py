"""Cleans a Ludo.ai run (video + its 6x6 sprite sheet) into game frames for the Qori walk/run test.

Not art: registration and cleanup of a generated animation.
- RGB comes from the video (no watermark). The video has a white background and Qori's face and tunic are white
  too, so the silhouette (alpha) comes from the sprite sheet's purple-keyed frames, which match video frames
  (scale 3.02). Each output frame takes the nearest sheet silhouette, warped by optical flow onto its video frame,
  and sharpens it with the video's own ink edges.
- One clean cycle: video frames J0, J0+2, ... (14 frames; the video's run cycle is 28 frames at 30 fps and frame
  J0+28 matches J0 closely).
- Steadied: the torso held on one column; the lowest foot of every contact frame on one ground row, flight frames
  eased between. Colours matched to the model image (Qori.png) in Lab (mean and spread).
Usage: python clean_ludo_run.py <video.mp4> <sheet.png> <model.png> <out_dir>
"""
import json, os, sys
import cv2
import numpy as np
from PIL import Image

VIDEO, SHEET, MODEL, OUT = sys.argv[1:5]
J0, STEP, N = 37, 2, 14            # loop start, video-frame step, frames per cycle
SCALE = 3.02                        # video px per sheet px
CANVAS = 760                        # output canvas (square), half the video resolution
OUT_SCALE = .5                      # output px per video px
GROUND = 720                        # output row of the ground line
PIVOT = 380                         # output column of the torso
os.makedirs(OUT, exist_ok=True)

# ---- video frames
cap = cv2.VideoCapture(VIDEO); vids = []
while True:
    ok, f = cap.read()
    if not ok: break
    vids.append(f)
gray = [cv2.cvtColor(v, cv2.COLOR_BGR2GRAY) for v in vids]
VH, VW = vids[0].shape[:2]

# ---- sheet silhouettes
sh = np.array(Image.open(SHEET).convert('RGB')).astype(int)
fh, fw = sh.shape[0] // 6, sh.shape[1] // 6
bgc = np.median(sh[:20, :20].reshape(-1, 3), axis=0)
def sheet_mask(i):
    r, c = divmod(i, 6); f = sh[r*fh:(r+1)*fh, c*fw:(c+1)*fw]
    m = (np.abs(f - bgc).sum(2) > 150).astype(np.uint8)
    # fill holes (watermark letters inside the figure): background = connected to the frame border
    ff = m.copy(); pad = np.zeros((fh + 2, fw + 2), np.uint8)
    cv2.floodFill(ff, pad, (0, 0), 2)
    return ((ff != 2)).astype(np.uint8)
def vnon(j):
    m = (vids[j].min(2) < 230); m[:15] = 0; m[-25:] = 0; m[:, :20] = 0; m[:, -20:] = 0; return m.astype(np.uint8)

# sheet frame -> video frame and offset (best match by silhouette)
small = lambda j: cv2.resize(vnon(j) * 255, None, fx=1 / SCALE, fy=1 / SCALE, interpolation=cv2.INTER_AREA) > 128
matches = []
for i in range(36):
    sm = sheet_mask(i).astype(bool); best = None
    for j in range(min(69, len(vids))):
        v = small(j); ys, xs = np.where(v); ys2, xs2 = np.where(sm)
        dx0, dy0 = xs2.min() - xs.min(), ys2.max() - ys.max()
        for dx in range(dx0 - 3, dx0 + 4):
            for dy in range(dy0 - 3, dy0 + 4):
                vw = cv2.warpAffine(v.astype(np.uint8), np.float32([[1, 0, dx], [0, 1, dy]]), (fw, fh)) > 0
                s = (vw & ~sm).sum() + .2 * (sm & ~vw).sum()
                if best is None or s < best[0]: best = (s, j, dx, dy)
    matches.append(dict(sheet=i, video=best[1], dx=best[2], dy=best[3]))
def full_mask(i):
    """Sheet silhouette i in video pixels."""
    m = matches[i]
    M = np.float32([[SCALE, 0, -m['dx'] * SCALE], [0, SCALE, -m['dy'] * SCALE]])
    return cv2.warpAffine(sheet_mask(i).astype(np.float32), M, (VW, VH), flags=cv2.INTER_LINEAR)

# ---- per output frame: alpha
frames, alphas = [], []
for k in range(N):
    t = J0 + STEP * k
    i = min(range(36), key=lambda s: (abs(matches[s]['video'] - t), s))
    src = matches[i]['video']
    a = full_mask(i)
    if src != t:   # warp the silhouette from its video frame to this one
        flow = cv2.calcOpticalFlowFarneback(gray[t], gray[src], None, .5, 4, 25, 5, 7, 1.5, 0)
        gx, gy = np.meshgrid(np.arange(VW, dtype=np.float32), np.arange(VH, dtype=np.float32))
        a = cv2.remap(a, gx + flow[..., 0], gy + flow[..., 1], cv2.INTER_LINEAR)
    core = cv2.erode((a > .5).astype(np.uint8), np.ones((9, 9), np.uint8))
    near = cv2.dilate((a > .5).astype(np.uint8), np.ones((13, 13), np.uint8))
    ink = vnon(t)
    alpha = np.maximum(core, ink & near).astype(np.float32)
    alpha = cv2.morphologyEx(alpha, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    # keep the largest connected piece (drops specks)
    n, lab, st, _ = cv2.connectedComponentsWithStats((alpha > .5).astype(np.uint8))
    if n > 1:
        keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); alpha = (lab == keep).astype(np.float32)
    # white-fringe cleanup: near-white pixels within 4 px of the edge are background showing through
    rim = (alpha > .5) & ~cv2.erode((alpha > .5).astype(np.uint8), np.ones((9, 9), np.uint8)).astype(bool)
    alpha[rim & (vids[t].min(2) > 228)] = 0
    # pure-white patches touching the edge are background, except big ones (the face, the tunic)
    wmask = ((alpha > .5) & (vids[t].min(2) >= 246)).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(wmask)
    if n > 1:
        big = st[1:, cv2.CC_STAT_AREA].max()
        edge = cv2.dilate((alpha <= .5).astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)
        r = np.where((alpha > .5).any(1))[0]; top, bot = r.min(), r.max(); hgt = bot - top
        for c in range(1, n):
            comp = lab == c
            cy = st[c, cv2.CC_STAT_TOP] + st[c, cv2.CC_STAT_HEIGHT] / 2
            in_head_or_torso = top + .08 * hgt < cy < top + .70 * hgt
            if (comp & edge).any() and (st[c, cv2.CC_STAT_AREA] < .25 * big or not in_head_or_torso): alpha[comp] = 0
    alpha = cv2.morphologyEx(alpha, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    alpha = cv2.GaussianBlur(alpha, (3, 3), 0)
    frames.append(cv2.cvtColor(vids[t], cv2.COLOR_BGR2RGB)); alphas.append(alpha)

# ---- colour match to the model (Lab mean and spread over the figure)
model = np.array(Image.open(MODEL).convert('RGB'))
mbg = np.median(model[5:40, 5:40].reshape(-1, 3), axis=0)
mfig = np.abs(model.astype(int) - mbg).sum(2) > 40
mlab = cv2.cvtColor(model, cv2.COLOR_RGB2LAB).astype(np.float32)[mfig]
allpx = np.concatenate([cv2.cvtColor(f, cv2.COLOR_RGB2LAB).astype(np.float32)[a > .5] for f, a in zip(frames, alphas)])
smean, sstd = allpx.mean(0), allpx.std(0); tmean, tstd = mlab.mean(0), mlab.std(0)
STRENGTH = .85
CREAM = [int(v) for v in np.median(model[int(model.shape[0]*.30):int(model.shape[0]*.36), int(model.shape[1]*.50):int(model.shape[1]*.56)].reshape(-1, 3), axis=0)]   # the model's cheek
def recolour(f):
    lab = cv2.cvtColor(f, cv2.COLOR_RGB2LAB).astype(np.float32)
    m = (lab - smean) / sstd * tstd + tmean
    lab = lab + (m - lab) * STRENGTH
    out = cv2.cvtColor(np.clip(lab, 0, 255).astype(np.uint8), cv2.COLOR_LAB2RGB).astype(np.float32)
    white = (f.min(axis=2) >= 232)[..., None]               # the face and tunic: the model's cream, keeping their shading
    shade = f.astype(np.float32).mean(axis=2, keepdims=True) / 255.0
    cream = np.array(CREAM, np.float32) * (.82 + .18 * shade)
    return np.where(white, out * .3 + cream * .7, out).clip(0, 255).astype(np.uint8)

# ---- steady: torso column and ground row
def rows(a): return np.where((a > .5).any(1))[0]
lows = [rows(a).max() for a in alphas]
torso_x = []
for a in alphas:
    r = rows(a); top, bot = r.min(), r.max(); band = a[int(top + .35 * (bot - top)):int(top + .55 * (bot - top))] > .5
    xs = np.where(band.any(0))[0]; torso_x.append((xs.min() + xs.max()) / 2)
contact = [l >= max(lows) - 14 for l in lows]          # a foot on the ground (video px)
offy = [None] * N
for k in range(N):
    if contact[k]: offy[k] = -lows[k]
for k in range(N):                                       # flight: ease between neighbouring contacts (loop-aware)
    if offy[k] is None:
        p = next(d for d in range(1, N) if contact[(k - d) % N]); q = next(d for d in range(1, N) if contact[(k + d) % N])
        offy[k] = offy[(k - p) % N] + (offy[(k + q) % N] - offy[(k - p) % N]) * p / (p + q)
# planted-foot ground speed: the lowest foot's x on consecutive contact frames
foot_x = []
for k, a in enumerate(alphas):
    b = a[lows[k] - 20:lows[k] + 1] > .5; xs = np.where(b.any(0))[0]; foot_x.append((xs.min() + xs.max()) / 2 - torso_x[k])
speeds = [foot_x[k] - foot_x[k + 1] for k in range(N - 1) if contact[k] and contact[k + 1]]
speed_px = float(np.median(speeds)) if speeds else 0.0     # video px per output frame (2 video frames)

meta = []
for k in range(N):
    rgb = recolour(frames[k]); a = alphas[k]
    out = np.zeros((CANVAS, CANVAS, 4), np.uint8)
    M = np.float32([[OUT_SCALE, 0, PIVOT - torso_x[k] * OUT_SCALE], [0, OUT_SCALE, GROUND + offy[k] * OUT_SCALE]])
    rgbw = cv2.warpAffine(rgb, M, (CANVAS, CANVAS), flags=cv2.INTER_AREA)
    aw = cv2.warpAffine(a, M, (CANVAS, CANVAS), flags=cv2.INTER_AREA)
    out[..., :3] = rgbw; out[..., 3] = np.clip(aw * 255, 0, 255).astype(np.uint8); out[out[..., 3] == 0, :3] = 0
    Image.fromarray(out).save(os.path.join(OUT, 'Qori_Run_%02d.png' % (k + 1)))
    meta.append(dict(frame=k + 1, video=J0 + STEP * k, contact=bool(contact[k])))
ys = np.where(np.array(Image.open(os.path.join(OUT, 'Qori_Run_01.png')))[..., 3].max(1) > 128)[0]
info = dict(source='Ludo.ai run video + sheet (user upload, 13 Oct 2026)', loopVideoFrames=[J0, J0 + STEP * N], frameCount=N,
            cycleSecondsAtSource=N * STEP / 30.0, canvas=CANVAS, groundRow=GROUND, pivotColumn=PIVOT,
            figureHeightPx=int(ys.max() - ys.min() + 1), strideCanvasPx=round(speed_px * N * OUT_SCALE, 1),
            colourMatch=dict(model=os.path.basename(MODEL), strength=STRENGTH), frames=meta, sheetToVideo=matches)
json.dump(info, open(os.path.join(OUT, 'Qori_Run_info.json'), 'w'), indent=1)
print(json.dumps({k: v for k, v in info.items() if k not in ('frames', 'sheetToVideo')}))
