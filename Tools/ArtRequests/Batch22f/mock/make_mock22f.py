"""Feasibility mock for the Batch 22f checker (not art). The 22e arm with only its edge zone changed: a smoothed,
antialiased silhouette outside the leaves, and the dark band brightened toward the arm's own edge shading profile
(measured on the locked bark, rows 150-175), blended back to the untouched pixels at the zone's inner edge.
    python make_mock22f.py <Batch22f_Input> <out.png>"""
import sys
import numpy as np
from PIL import Image
IN, OUTP = sys.argv[1], sys.argv[2]
A = np.array(Image.open(IN + '/reference_22e/Qori_v3_Arm.png').convert('RGBA')).astype(float)
zone = np.array(Image.open(IN + '/Qori_v3_Arm_Mask_EdgeZone.png')) > 127
outer = np.array(Image.open(IN + '/Qori_v3_Arm_Mask_Outer.png')) > 127
inner = np.array(Image.open(IN + '/Qori_v3_Arm_Mask_Inner.png')) > 127
H, W = A.shape[:2]
L = 0.299 * A[..., 0] + 0.587 * A[..., 1] + 0.114 * A[..., 2]
ol = (A[..., 3] >= 128) & (A[..., 1] >= A[..., 0] * 0.86) & (A[..., 1] >= A[..., 2] * 1.35)
leaf = ol.copy()
for dy in (-1, 0, 1):
    for dx in (-1, 0, 1):
        leaf |= np.roll(np.roll(ol, dy, 0), dx, 1)
leaf &= A[..., 3] > 0
DEPTH = 22
# the arm's edge profile: mean luminance by distance from each edge, rows 150-175
prof = {s: np.array([np.mean([L[y, 52 + d] if s == 'L' else L[y, 99 - d] for y in range(150, 176)]) for d in range(DEPTH)]) for s in 'LR'}
ys = np.arange(180, 240)
def smooth(v, k=4):
    return np.convolve(np.pad(v, k, mode='edge'), np.ones(2 * k + 1) / (2 * k + 1), 'valid')
l = np.array([np.nonzero(A[y, :, 3] >= 128)[0].min() for y in ys], float)
r = np.array([np.nonzero(A[y, :, 3] >= 128)[0].max() + 1 for y in ys], float)
cl = np.full(len(ys), np.inf); cr = np.full(len(ys), -np.inf); ol_ = np.zeros(len(ys)); or_ = np.zeros(len(ys))
for i, y in enumerate(ys):
    mx = np.nonzero(leaf[y] | inner[y])[0]
    if len(mx): cl[i] = mx.min() - 0.5; cr[i] = mx.max() + 1.5
    ox = np.nonzero(outer[y])[0]; ol_[i] = ox.min(); or_[i] = ox.max() + 1
r[-1] = max(r[-1], np.nonzero(A[240, :, 3] >= 128)[0].max() + 1); l[-1] = min(l[-1], np.nonzero(A[240, :, 3] >= 128)[0].min())
for _ in range(30):   # smooth, always outside what must stay opaque and inside the outer mask
    l = np.clip(np.minimum(smooth(l), cl), ol_, None); r = np.clip(np.maximum(smooth(r), cr), None, or_)
l = np.floor(l) + 0.5; r = np.floor(r) + 0.5
out = A.copy()
for i, y in enumerate(ys):
    xs = np.nonzero(A[y, :, 3] >= 128)[0]; l0, r0 = xs.min(), xs.max()
    for x in np.nonzero(zone[y])[0]:
        if leaf[y, x]: continue
        cov = np.clip(x + 1 - l[i], 0, 1) * np.clip(r[i] - x, 0, 1)
        if cov <= 0:
            out[y, x] = 0; continue
        side = 'L' if x - l0 < r0 - x else 'R'
        d = int(max(0, (x - np.floor(l[i])) if side == 'L' else (np.ceil(r[i]) - 1 - x)))
        d = min(d, DEPTH - 1)
        w = 1.0 if d < 10 else max(0.0, (16 - d) / 6)
        base = A[y, x, :3] if A[y, x, 3] >= 128 else A[y, int(np.clip(x, l0 + 1, r0 - 1)), :3]
        k = prof[side][d] / max(L[y, x] if A[y, x, 3] >= 128 else L[y, int(np.clip(x, l0 + 1, r0 - 1))], 20)
        out[y, x, :3] = base * (1 - w + w * np.clip(k, 0.6, 2.2)); out[y, x, 3] = round(255 * cov)
out[..., :3][out[..., 3] == 0] = 0
Image.fromarray(np.clip(out, 0, 255).round().astype(np.uint8), 'RGBA').save(OUTP)
