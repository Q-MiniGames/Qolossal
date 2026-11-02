"""Batch 27 acceptance checker: the Young Adventurer Qori walk cycle (16 new full-figure frames) against
the walk block-out and the approved model.

Usage: python check_walk_27.py <delivery_dir> <input_dir>
  <input_dir> is Batch27_Input (blockout, masks, reference, batch27_blockout.json, batch27_targets.json,
  Qori_Walk_Registration_TEMPLATE.json). Writes Qori_Walk_CHECK.json and the proofs into <delivery_dir>.
Python 3, numpy and Pillow only. Prints PASS/FAIL per test; the last line is ALL PASS or N FAIL.
"""
import hashlib, json, os, sys
import numpy as np
from PIL import Image, ImageDraw

DELIVERY, INPUT = sys.argv[1], sys.argv[2]
B = json.load(open(os.path.join(INPUT, 'batch27_blockout.json')))
T = json.load(open(os.path.join(INPUT, 'batch27_targets.json')))
W, H = B['canvas']; N = B['frameCount']; FL = B['footLineRow']
results = []


def test(section, name, ok, value, limit):
    results.append(dict(section=section, test=name, ok=bool(ok), value=value, limit=limit))
    print('%s  [%s] %s: %s (limit %s)' % ('PASS' if ok else 'FAIL', section, name, value, limit))


def rgba(p): return np.array(Image.open(p).convert('RGBA'))
def mask(p): return np.array(Image.open(p).convert('L'))


def _shift_or(m, s, axis):
    out = m.copy()
    if axis == 0: out[s:] |= m[:-s]; out[:-s] |= m[s:]
    else: out[:, s:] |= m[:, :-s]; out[:, :-s] |= m[:, s:]
    return out


def dilate(m, r):
    """Square dilation by r px (separable, by doubling)."""
    m = m > 0
    for axis in (0, 1):
        reach = 0
        while reach < r:
            s = min(reach + 1, r - reach); m = _shift_or(m, s, axis); reach += s
    return m


def erode(m, r): return ~dilate(~(m > 0), r) if r > 0 else m > 0


def lum(a):
    a = a.astype(np.float32); return a[:, :, 0] * .299 + a[:, :, 1] * .587 + a[:, :, 2] * .114


def detail(img, m):
    """Mean luminance step between horizontal neighbours inside m (painted texture)."""
    l = lum(img); mm = m[:, 1:] & m[:, :-1]
    return float(np.abs(np.diff(l, axis=1))[mm].mean()) if mm.any() else 0.0


def iou(a, b, region=None):
    if region is not None: a, b = a & region, b & region
    u = (a | b).sum(); return float((a & b).sum() / u) if u else 1.0


def near_colour(img, col, tol):
    return np.abs(img[:, :, :3].astype(int) - np.array(col)).max(axis=2) <= tol


name = lambda k: 'Qori_Walk_%02d' % k
F = B['frames']
model = rgba(os.path.join(INPUT, 'reference', 'Model_OnCanvas.png'))
mA = model[:, :, 3] > 200
FACE = T['faceColour']
M = {}
def m_(k, part):
    key = (k, part)
    if key not in M: M[key] = mask(os.path.join(INPUT, 'masks', '%s_Mask_%s.png' % (name(k + 1), part))) > 128
    return M[key]


def head_box(img, head, neck_y, seed_offset=None):
    """The cream face around a head centre: (width, height, cx, cy) of the connected cream region grown
    from the pixel nearest the expected face centre, or None. Ears, tunic and crest are not connected."""
    hx, hy = head; hw, hh = B['headSize']
    x0, x1 = int(max(hx - hw, 0)), int(min(hx + hw, W)); y0, y1 = int(max(hy - hh, 0)), int(neck_y)
    sub = img[y0:y1, x0:x1]
    cream = erode(near_colour(sub, FACE, T['faceTolerance']) & (sub[:, :, 3] >= 250), 4)
    ys, xs = np.where(cream)
    if len(xs) < 500: return None
    off = seed_offset if seed_offset is not None else T['faceCentreOffset']
    ex, ey = hx + off[0] - x0, hy + off[1] - y0
    i = int(np.argmin((xs - ex) ** 2 + (ys - ey) ** 2))
    if (xs[i] - ex) ** 2 + (ys[i] - ey) ** 2 > (0.25 * hw) ** 2: return None
    region = np.zeros_like(cream); region[ys[i], xs[i]] = True
    while True:                                   # grow the seed inside the cream (connected region)
        grown = dilate(region, 4) & cream
        if grown.sum() == region.sum(): break
        region = grown
    ys, xs = np.where(region)
    if len(xs) < 500: return None
    xa, xb = np.percentile(xs, [1, 99]); ya, yb = np.percentile(ys, [1, 99])
    return float(xb - xa), float(yb - ya), float((xa + xb) / 2 + x0), float((ya + yb) / 2 + y0)


# ---------------------------------------------------------------- 1. files and format
S = '1 format'
for k in range(1, N + 1):
    p = os.path.join(DELIVERY, name(k) + '.png')
    if not os.path.exists(p): test(S, name(k) + '.png exists', False, 'missing', 'present'); continue
    im = Image.open(p)
    test(S, '%s.png is %dx%d RGBA' % (name(k), W, H), im.size == (W, H) and im.mode == 'RGBA', '%dx%d %s' % (im.size + (im.mode,)), '%dx%d RGBA' % (W, H))
if any(not r['ok'] for r in results):
    print('%d FAIL (files missing or wrong format; nothing else checked)' % sum(not r['ok'] for r in results)); sys.exit(1)
img = [rgba(os.path.join(DELIVERY, name(k) + '.png')) for k in range(1, N + 1)]
P = [i[:, :, 3] > 128 for i in img]
for k in range(N):
    a = img[k][:, :, 3]
    dirty = int(img[k][:, :, :3][a == 0].max()) if (a == 0).any() else 0
    test(S, '%s straight alpha (RGB = 0 where alpha = 0)' % name(k + 1), dirty == 0, dirty, 0)
    soft = float(((a > 0) & (a < 250) & ~dilate(a >= 250, 3)).sum() / max((a > 0).sum(), 1))
    test(S, '%s no glow or soft aura' % name(k + 1), soft <= T['maxSoftAlphaShare'], round(soft, 4), '<= %s' % T['maxSoftAlphaShare'])
    edge = int(a[:8].max() > 0) + int(a[-8:].max() > 0) + int(a[:, :8].max() > 0) + int(a[:, -8:].max() > 0)
    test(S, '%s nothing touches the canvas edge (no background, no border)' % name(k + 1), edge == 0, edge, 0)
hs = [hashlib.sha256(open(os.path.join(DELIVERY, name(k) + '.png'), 'rb').read()).hexdigest() for k in range(1, N + 1)]
test(S, 'all 16 frames are different files', len(set(hs)) == N, len(set(hs)), N)

# ---------------------------------------------------------------- 2. the block-out (pose, size, place)
S = '2 block-out'
BO = [mask(os.path.join(INPUT, 'masks', name(k + 1) + '_Mask_All.png')) > 128 for k in range(N)]
for k in range(N):
    nm = name(k + 1)
    core = mask(os.path.join(INPUT, 'masks', nm + '_Mask_Core.png')) > 128
    free = np.zeros((H, W), bool)
    for part, r in B['freePx'].items(): free |= dilate(m_(k, part), r)
    inner = erode(core, T['coreErodePx'])
    cov = float((P[k] & inner).sum() / max(inner.sum(), 1))
    test(S, '%s block-out body covered by paint (core shrunk %d px)' % (nm, T['coreErodePx']), cov >= T['minCoreCover'], round(cov, 4), '>= %s' % T['minCoreCover'])
    out = P[k] & ~dilate(BO[k], T['outlineTolerancePx']) & ~free
    v = float(out.sum() / max(P[k].sum(), 1))
    test(S, '%s paint beyond the block-out (+%d px; cloak and ears free)' % (nm, T['outlineTolerancePx']), v <= T['maxOutsideShare'], round(v, 4), '<= %s' % T['maxOutsideShare'])
    r = float(P[k].sum() / max(BO[k].sum(), 1))
    test(S, '%s figure area / block-out area' % nm, T['areaRatio'][0] <= r <= T['areaRatio'][1], round(r, 3), T['areaRatio'])

# ---------------------------------------------------------------- 3. ground and feet
S = '3 ground'
feet_x = {'near': [None] * N, 'far': [None] * N}
for k in range(N):
    nm = name(k + 1); f = F[k]
    low = int(np.where(P[k].any(axis=1))[0].max())
    test(S, '%s lowest sole on the ground line %d' % (nm, FL), FL - T['soleTolerancePx'] <= low <= FL + 1, low, '[%d, %d]' % (FL - T['soleTolerancePx'], FL + 1))
    band = np.zeros((H, W), bool); band[FL - T['footBandPx']:FL + 2] = True
    v = iou(P[k], BO[k], band)
    test(S, '%s feet band (bottom %d rows) overlap with block-out, IoU' % (nm, T['footBandPx']), v >= T['minFootBandIoU'], round(v, 3), '>= %s' % T['minFootBandIoU'])
    for side in ('near', 'far'):
        if not f['legs'][side]['planted']: continue
        legm = m_(k, 'leg_' + side)
        box = np.zeros((H, W), bool); box[FL - 45:FL + 2, :] = True
        other = dilate(m_(k, 'leg_' + ('far' if side == 'near' else 'near')), 6)
        g = np.where(legm & box)[1]; p = np.where(P[k] & box & dilate(legm, 14) & ~(other & ~legm))[1]
        if len(p) == 0 or len(g) == 0:
            test(S, '%s %s planted foot present' % (nm, side), False, 'none', 'present'); continue
        dx = abs(float(p.mean()) - float(g.mean())); feet_x[side][k] = float(p.mean()) - float(g.mean())
        test(S, '%s %s planted foot where the block-out has it, |dx|' % (nm, side), dx <= T['maxFootShiftPx'], round(dx, 1), '<= %s' % T['maxFootShiftPx'])
for side in ('near', 'far'):
    xs = feet_x[side]
    steps = [abs(xs[i + 1] - xs[i]) for i in range(N - 1) if xs[i] is not None and xs[i + 1] is not None]
    if steps:
        test(S, '%s planted foot keeps the block-out ground speed %.1f px a frame (no skating), worst error' % (side, B['groundPxPerFrame']),
             max(steps) <= T['maxStrideErrorPx'], round(max(steps), 1), '<= %s' % T['maxStrideErrorPx'])

# ---------------------------------------------------------------- 4. the model (head, palette, colour, detail)
S = '4 model'
mh = head_box(model, T['modelHead'], T['modelNeckRow'])
assert mh is not None, 'model face not found'
heads = []
for k in range(N):
    nm = name(k + 1); f = F[k]
    hb = head_box(img[k], f['head'], f['neck'][1]); heads.append(hb)
    if hb is None:
        test(S, '%s cream face found where the block-out head is' % nm, False, 'none', 'found'); continue
    for i, lab in ((0, 'width'), (1, 'height')):
        r = hb[i] / mh[i]
        test(S, '%s face %s / model face %s' % (nm, lab, lab), abs(r - 1) <= T['faceSizeTolerance'], round(r, 3), '1 +/- %s' % T['faceSizeTolerance'])
    for i, lab, ref in ((3, 'height (bob)', 1), (2, 'position along x', 0)):
        d = abs(hb[i] - (f['head'][ref] + T['faceCentreOffset'][ref]))
        test(S, '%s face %s follows the block-out head, |d|' % (nm, lab), d <= T['maxHeadShiftPx'], round(d, 1), '<= %s' % T['maxHeadShiftPx'])
pal = np.array(T['modelPalette'])
rng = np.random.default_rng(0)
for k in range(N):
    nm = name(k + 1); a = img[k][:, :, 3] >= 250
    px_ = img[k][:, :, :3][a].astype(int)
    if len(px_) > 60000: px_ = px_[rng.choice(len(px_), 60000, replace=False)]
    d = np.abs(px_[:, None, :] - pal[None, :, :]).max(axis=2).min(axis=1)
    share = float((d <= T['paletteTolerance']).mean())
    test(S, '%s colours from the model palette (share within %d)' % (nm, T['paletteTolerance']), share >= T['minPaletteShare'], round(share, 3), '>= %s' % T['minPaletteShare'])
    hsv = np.array(Image.fromarray(img[k][:, :, :3]).convert('HSV')).astype(np.float32) / 255
    neon = float(((hsv[:, :, 1] > .85) & (hsv[:, :, 2] > .6) & a).sum() / max(a.sum(), 1))
    test(S, '%s no neon colour' % nm, neon <= T['maxNeonShare'], round(neon, 4), '<= %s' % T['maxNeonShare'])
    for part, key in (('leg_near', 'Bark'), ('cape', 'Cape')):
        mm = erode(m_(k, part), 6) & a
        if part == 'cape':
            for other in B['core']: mm &= ~dilate(m_(k, other), 6)
        if mm.sum() < 300: continue
        med = np.median(img[k][:, :, :3][mm], axis=0); dd = float(np.abs(med - np.array(T['model' + key])).max())
        test(S, '%s %s median colour vs the model' % (nm, key.lower()), dd <= T['maxMedianColourDiff'], round(dd, 1), '<= %s' % T['maxMedianColourDiff'])
mdet = detail(model, erode(mA, 4))
dets = [detail(img[k], erode(P[k], 4)) for k in range(N)]
for k in range(N):
    r = dets[k] / mdet
    test(S, '%s painted detail / model detail (not smeared, not noisy)' % name(k + 1), T['detailRatio'][0] <= r <= T['detailRatio'][1], round(r, 3), T['detailRatio'])

# ---------------------------------------------------------------- 5. loop, follow-through and flicker
S = '5 loop'
pairs = [(k, (k + 1) % N) for k in range(N)]
xp = [float((P[a] ^ P[b]).sum()) for a, b in pairs]; xb = [float((BO[a] ^ BO[b]).sum()) for a, b in pairs]
med = float(np.median(xp))
for (a, b), vp, vb in zip(pairs, xp, xb):
    tag = '%s -> %s' % (name(a + 1), name(b + 1))
    r = vp / max(vb, 1)
    test(S, '%s silhouette change / block-out change' % tag, T['changeRatio'][0] <= r <= T['changeRatio'][1], round(r, 3), T['changeRatio'])
    test(S, '%s no pop (change / median change)' % tag, vp <= T['maxPopFactor'] * med, round(vp / med, 2), '<= %s' % T['maxPopFactor'])
    if heads[a] and heads[b]:
        r = abs(heads[a][0] - heads[b][0]) / heads[a][0]
        test(S, '%s face size steady' % tag, r <= T['maxNeighbourFaceChange'], round(r, 4), '<= %s' % T['maxNeighbourFaceChange'])
    r = abs(dets[a] - dets[b]) / max(dets[a], dets[b])
    test(S, '%s detail steady' % tag, r <= T['maxNeighbourDetailStep'], round(r, 3), '<= %s' % T['maxNeighbourDetailStep'])
    aa, ab = img[a][:, :, 3] >= 250, img[b][:, :, 3] >= 250
    d = float(np.abs(img[a][:, :, :3][aa].mean(axis=0) - img[b][:, :, :3][ab].mean(axis=0)).max())
    test(S, '%s overall colour steady (mean change)' % tag, d <= T['maxNeighbourColourStep'], round(d, 1), '<= %s' % T['maxNeighbourColourStep'])
fa, fb = T['followFrames']
for part in ('cape', 'ear_far', 'ear_near'):
    zone = dilate(m_(fa - 1, part) | m_(fb - 1, part), B['freePx'][part])
    for other in B['core']: zone &= ~(m_(fa - 1, other) | m_(fb - 1, other))
    vp = float(((P[fa - 1] ^ P[fb - 1]) & zone).sum()); vb = float(((BO[fa - 1] ^ BO[fb - 1]) & zone).sum())
    test(S, '%s moves between frames %d and %d (follow-through present; share of the block-out motion)' % (part, fa, fb), vp >= T['minFollowShare'] * vb,
         round(vp / max(vb, 1), 3), '>= %s' % T['minFollowShare'])

# Added after Review of the first delivery: a cloak cut off under the mantle, and repainted frames that pop.
for k in range(N):
    f = F[k]; e = P[k][1:] & ~P[k][:-1]                       # top edges: clear above, paint below
    y0, y1, x1 = int(f['neck'][1]), int(f['pelvis'][1]), int(f['neck'][0])
    best = 0
    for y in range(y0, y1):
        r = e[y, :x1]
        if r.any():
            x = np.flatnonzero(np.diff(np.concatenate(([0], r.astype(int), [0])))); best = max(best, int((x[1::2] - x[::2]).max()))
    test(S, '%s cloak hangs from under the mantle (no straight cut at its top; longest flat top edge, px)' % name(k + 1),
         best <= T.get('maxStraightTopEdgePx', 15), best, '<= %s' % T.get('maxStraightTopEdgePx', 15))
steps = []
for a, b in pairs:
    m = m_(a, 'mantle')
    dx = int(round(F[b]['neck'][0] - F[a]['neck'][0])); dy = int(round(F[b]['neck'][1] - F[a]['neck'][1]))
    best = 1e9
    for ox in range(-6, 7, 2):
        for oy in range(-6, 7, 2):
            bb = np.roll(np.roll(img[b], -(dy + oy), 0), -(dx + ox), 1)
            mm = m & (img[a][:, :, 3] >= 250) & (bb[:, :, 3] >= 250)
            if mm.sum() > 2000: best = min(best, float(np.abs(img[a][:, :, :3].astype(float) - bb[:, :, :3]).mean(axis=2)[mm].mean()))
    steps.append(best)
med_s = float(np.median(steps))
for (a, b), v in zip(pairs, steps):
    test(S, '%s -> %s mantle painted the same (aligned difference / median of all pairs)' % (name(a + 1), name(b + 1)),
         v <= T.get('maxMantleStepFactor', 1.5) * med_s, round(v / med_s, 2), '<= %s' % T.get('maxMantleStepFactor', 1.5))

for k in range(N):            # no detached specks or smears floating off the figure
    a = img[k][:, :, 3] > 40
    sm = a[::2, ::2]
    seed = erode(sm, 6); ys, xs = np.where(seed)
    reg = np.zeros_like(sm)
    if len(xs): reg[ys[len(ys) // 2], xs[len(xs) // 2]] = True
    while True:                                   # the figure: everything connected to its body
        g = dilate(reg, 3) & sm
        if g.sum() == reg.sum(): break
        reg = g
    v = int((sm & ~reg).sum()) * 4
    test(S, '%s no detached specks or smears off the figure (px)' % name(k + 1), v <= T.get('maxStrayPx', 0), v, '<= %s' % T.get('maxStrayPx', 0))

# ---------------------------------------------------------------- 6. registration
S = '6 registration'
rp, tp = os.path.join(DELIVERY, 'Qori_Walk_Registration.json'), os.path.join(INPUT, 'Qori_Walk_Registration_TEMPLATE.json')
if not os.path.exists(rp): test(S, 'Qori_Walk_Registration.json exists', False, 'missing', 'present')
else:
    R, TT = json.load(open(rp)), json.load(open(tp))
    for key in ('canvas', 'pixelsPerUnit', 'footLineRow', 'pivotColumn', 'frameCount', 'cycleSeconds', 'groundPxPerFrame', 'frames'):
        test(S, 'registration %s unchanged' % key, R.get(key) == TT.get(key), 'same' if R.get(key) == TT.get(key) else 'changed', 'same as template')

# ---------------------------------------------------------------- proofs
bg = (150, 170, 175, 255); sc = 4; w, h = W // sc, H // sc
sheet = Image.new('RGBA', (8 * w, 2 * h), bg); over = Image.new('RGBA', (8 * w, 2 * h), (235, 235, 235, 255))
for k in range(N):
    sheet.alpha_composite(Image.fromarray(img[k]).resize((w, h), Image.LANCZOS), ((k % 8) * w, (k // 8) * h))
    o = np.full((H, W, 4), 255, np.uint8); o[:, :, :3] = 235
    o[P[k] & BO[k]] = (170, 170, 170, 255); o[P[k] & ~BO[k]] = (220, 40, 40, 255); o[~P[k] & BO[k]] = (40, 90, 230, 255); o[FL, :] = (0, 0, 0, 255)
    over.alpha_composite(Image.fromarray(o).resize((w, h), Image.NEAREST), ((k % 8) * w, (k // 8) * h))
for im in (sheet, over):
    dr = ImageDraw.Draw(im)
    for k in range(N): dr.text(((k % 8) * w + 6, (k // 8) * h + 6), '%02d' % (k + 1), fill=(0, 0, 0, 255))
sheet.convert('RGB').save(os.path.join(DELIVERY, 'Qori_Walk_Proof_Sheet.png'))
over.convert('RGB').save(os.path.join(DELIVERY, 'Qori_Walk_Proof_BlockoutOverlay.png'))
reg = json.load(open(tp))
gif = [Image.alpha_composite(Image.new('RGBA', (W, H), bg), Image.fromarray(i)).resize((W // 2, H // 2), Image.LANCZOS).convert('P', palette=Image.ADAPTIVE) for i in img]
gif[0].save(os.path.join(DELIVERY, 'Qori_Walk_Proof_Loop.gif'), save_all=True, append_images=gif[1:], duration=[f['durationMs'] for f in reg['frames']], loop=0)
gif[0].save(os.path.join(DELIVERY, 'Qori_Walk_Proof_Loop_Slow.gif'), save_all=True, append_images=gif[1:], duration=[round(f['durationMs'] / .6) for f in reg['frames']], loop=0)
strip = Image.new('RGBA', (W, 260), bg)
for k in range(N):
    part = Image.fromarray(img[k]).crop((0, FL - 220, W, FL + 40)); a = np.array(part); a[:, :, 3] = (a[:, :, 3] * .22).astype(np.uint8)
    strip.alpha_composite(Image.fromarray(a))
ImageDraw.Draw(strip).line([(0, 220), (W, 220)], fill=(200, 0, 0, 255))
strip.convert('RGB').save(os.path.join(DELIVERY, 'Qori_Walk_Proof_FeetOnion.png'))
cmp_ = Image.new('RGBA', (2 * W, H), bg); cmp_.alpha_composite(Image.fromarray(model)); cmp_.alpha_composite(Image.fromarray(img[4]), (W, 0))
ImageDraw.Draw(cmp_).line([(0, FL), (2 * W, FL)], fill=(200, 0, 0, 255))
cmp_.convert('RGB').resize((W, H // 2)).save(os.path.join(DELIVERY, 'Qori_Walk_Proof_ModelCompare.png'))

fails = [r for r in results if not r['ok']]
json.dump(dict(passed=len(results) - len(fails), failed=len(fails), results=results), open(os.path.join(DELIVERY, 'Qori_Walk_CHECK.json'), 'w'), indent=1)
print('%d tests, %d failed' % (len(results), len(fails)))
print('ALL PASS' if not fails else '%d FAIL' % len(fails))
sys.exit(0 if not fails else 1)
