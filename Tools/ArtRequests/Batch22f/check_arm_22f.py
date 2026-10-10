"""Batch 22f acceptance checker for Qori_v3_Arm.png (wrap-edge repaint of the 22e arm). Needs Python 3 with numpy and Pillow only.

    python check_arm_22f.py <Batch22f delivery folder> [<Batch22f_Input folder>]

All the Batch 22d and 22e tests still run; section 10 adds the wrap-edge tests.

Run it on the delivery folder before handing over. Every test prints PASS or FAIL with the measured value
and the limit. Fix the painting and run it again until the last line says ALL PASS. It also writes
Qori_v3_ARM_CHECK.json and three proof images into the delivery folder:
  Qori_v3_Proof_ArmCheck_Elbow_2x.png      the arm alone, skinned at +90, +45 and -20 degrees, 2x nearest
  Qori_v3_Proof_ArmCheck_HandJoin_2x.png   arm over each hand at the registered draw order, 2x bilinear
  Qori_v3_Proof_ArmCheck_Template.png      the delivered arm over the template, 4x, failures in red
"""
import hashlib, json, math, sys
from pathlib import Path
import numpy as np
from PIL import Image

DELIVERY = Path(sys.argv[1]) if len(sys.argv) > 1 else Path('.')
INPUT = Path(sys.argv[2]) if len(sys.argv) > 2 else Path(__file__).resolve().parent
T = json.loads((INPUT / 'batch22f_targets.json').read_text())
REF = INPUT / 'reference_22d'
results = []

def check(name, ok, measured, limit):
    results.append({'test': name, 'pass': bool(ok), 'measured': measured, 'limit': limit})
    print(('PASS  ' if ok else 'FAIL  ') + name + ': ' + str(measured) + '  (limit: ' + str(limit) + ')')

def rgba(path):
    return np.array(Image.open(path).convert('RGBA')).astype(float)

def lum(a):
    return 0.299 * a[..., 0] + 0.587 * a[..., 1] + 0.114 * a[..., 2]

def olive(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    return (a[..., 3] >= 128) & (g >= r * 0.86) & (g >= b * 1.35)

def shrink(m, k):
    out = m.copy()
    for dy in range(-k, k + 1):
        for dx in range(-k, k + 1):
            out &= np.roll(np.roll(m, dy, 0), dx, 1)
    return out

def grow(m, k):
    out = m.copy()
    for dy in range(-k, k + 1):
        for dx in range(-k, k + 1):
            out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out

def seam_rows(a, y0, y1):
    # Per row: compare each pixel with the one above it, over the middle 70% of the row. A seam is a row where
    # the step is large and goes the same way across the row.
    al = a[..., 3]; L = lum(a); out = []
    for y in range(max(y0, 1), y1 + 1):
        both = (al[y] >= 250) & (al[y - 1] >= 250)
        xx = np.nonzero(both)[0]
        if len(xx) < 10:
            continue
        l, r = xx.min(), xx.max(); m = (r - l) * 0.15
        sel = xx[(xx >= l + m) & (xx <= r - m)]
        d = L[y, sel] - L[y - 1, sel]
        out.append((y, float(d.mean()), float(np.abs(d).mean()), float(max((d > 10).mean(), (d < -10).mean()))))
    return out

arm_path = DELIVERY / 'Qori_v3_Arm.png'
im = Image.open(arm_path)
W, H = T['canvas']
X0 = T['boneLineX']

# 1. file format
check('canvas size', im.size == (W, H), list(im.size), [W, H])
check('PNG mode RGBA', im.mode == 'RGBA', im.mode, 'RGBA')
a = rgba(arm_path)
al = a[..., 3]
check('RGB is 0 wherever alpha is 0 (straight alpha, no halo)', int((a[al == 0][:, :3] > 0).any(axis=1).sum()) == 0,
      int((a[al == 0][:, :3] > 0).any(axis=1).sum()), 0)
_same = hashlib.sha256(arm_path.read_bytes()).hexdigest() == T['arm22dSha256']
check('not byte-identical to the 22d arm', not _same, 'identical' if _same else 'sha256 differs', 'must differ')

# 2. silhouette against the template masks
outer = np.array(Image.open(INPUT / 'Qori_v3_Arm_Mask_Outer.png')) > 127
inner = np.array(Image.open(INPUT / 'Qori_v3_Arm_Mask_Inner.png')) > 127
cover = np.array(Image.open(INPUT / 'Qori_v3_Arm_Mask_HandCover.png')) > 127
outside = (al > 0) & ~outer
check('no arm pixel (alpha > 0) outside Qori_v3_Arm_Mask_Outer', int(outside.sum()) == 0, int(outside.sum()), 0)
holes = inner & (al < 250)
check('every Qori_v3_Arm_Mask_Inner pixel opaque (alpha >= 250)', int(holes.sum()) == 0, int(holes.sum()), 0)
tube = T['tube']
top_rows = np.nonzero((al >= 128).any(axis=1))[0]
check('cap top row', abs(int(top_rows.min()) - tube['capTopRow']) <= 3, int(top_rows.min()), '%d +/- 3' % tube['capTopRow'])
y0, y1 = T['centreLine']['rows']
offs = []
for y in range(y0, y1 + 1):
    xs = np.nonzero(al[y] >= 128)[0]
    if len(xs):
        offs.append(abs((xs.min() + xs.max() + 1) / 2 - X0))
check('straight: centre of every row %d-%d within 3 px of the bone line x %.2f' % (y0, y1, X0),
      max(offs) <= T['centreLine']['maxOffsetPx'], round(max(offs), 2), T['centreLine']['maxOffsetPx'])
sh = T['joints']['shoulder']
knob = (al >= 128) & ~outer & (np.mgrid[0:H, 0:W][0] < sh[1] + 30)
check('no shoulder knob: pixels outside the tube near the shoulder', int(knob.sum()) == 0, int(knob.sum()), 0)

# 3. wrap position and colour
ol = olive(a)
orows = np.nonzero(ol.sum(1) >= 4)[0]
wr = T['wrap']
if len(orows):
    check('wrap rows inside %s' % wr['allowedRows'], orows.min() >= wr['allowedRows'][0] and orows.max() <= wr['allowedRows'][1],
          [int(orows.min()), int(orows.max())], wr['allowedRows'])
    centre = (orows.min() + orows.max()) / 2
    check('wrap centre row', abs(centre - wr['centreRow22c']) <= wr['centreToleranceRows'], centre, '%.1f +/- %d' % (wr['centreRow22c'], wr['centreToleranceRows']))
    med = np.median(a[ol][:, :3], 0)
    diff = float(np.abs(med - np.array(wr['oliveMedianRGB22c'])).max())
    check('wrap olive median RGB matches 22c', diff <= wr['oliveMedianToleranceRGB'], [int(v) for v in med], '%s +/- %d' % (wr['oliveMedianRGB22c'], wr['oliveMedianToleranceRGB']))
    wrap_top = int(orows.min())
else:
    check('wrap present (olive pixels)', False, 0, '> 0 rows')
    wrap_top = wr['rows22c'][0]

# 4. seams: no straight horizontal edge across the bark above the wrap
sm = T['seam']
rows = seam_rows(a, sm['rows'][0], wrap_top - 1)
ws = max(rows, key=lambda r: abs(r[1])); wa = max(rows, key=lambda r: r[2]); wc = max(rows, key=lambda r: r[3])
check('seam: largest row-to-row mean luminance step, rows %d-%d' % (sm['rows'][0], wrap_top - 1), abs(ws[1]) <= sm['maxAbsSignedStep'],
      'row %d: %.1f' % (ws[0], ws[1]), sm['maxAbsSignedStep'])
check('seam: largest mean absolute row step', wa[2] <= sm['maxMeanAbsStep'], 'row %d: %.1f' % (wa[0], wa[2]), sm['maxMeanAbsStep'])
check('seam: largest share of a row stepping >10 the same way', wc[3] <= sm['maxCoherent'], 'row %d: %.2f' % (wc[0], wc[3]), sm['maxCoherent'])
# bark showing beside and below the wrap leaves must be the same bark as above it: never lighter (a pasted block
# was lighter in 22c); a little darker is the wrap's shadow (the 22c leg's is 12 darker)
bt = sm['barkAroundWrap']
bottom = int(math.floor(T['handCover']['handTopRowInArm'])) - 1
bark_in = (al >= 250) & ~grow(ol, 2)
for k in (1, 2):
    bark_in &= np.roll(bark_in, k, 1) & np.roll(bark_in, -k, 1)
Lb = lum(a)
above_m = float(np.median(Lb[wrap_top - 30:wrap_top][bark_in[wrap_top - 30:wrap_top]]))
around = Lb[wrap_top:bottom + 1][bark_in[wrap_top:bottom + 1]]
around_m = float(np.median(around)) if len(around) else above_m
check('bark beside/below the wrap (rows %d-%d) not lighter than the 30 bark rows above' % (wrap_top, bottom),
      around_m - above_m <= bt['maxLighterLum'], 'median %.1f vs %.1f above (%+.1f)' % (around_m, above_m, around_m - above_m), '<= %+.1f' % bt['maxLighterLum'])
ba = sm['barkAboveWrap']
L = lum(a); solid = (al >= 250) & ~ol
near = L[wrap_top - ba['rowsNear']:wrap_top][solid[wrap_top - ba['rowsNear']:wrap_top]]
far = L[wrap_top - ba['rowsFar']:wrap_top - ba['rowsNear']][solid[wrap_top - ba['rowsFar']:wrap_top - ba['rowsNear']]]
dd = abs(float(np.median(near) - np.median(far)))
check('bark just above the wrap matches the bark above it (no pasted block)', dd <= ba['maxMedianLumDiff'], round(dd, 1), ba['maxMedianLumDiff'])

# 5. colour against the 22c leg
c = T['colour']; tol = c['tolerance']; legc = c['legBark']
def interior(al, k): return shrink(al >= 255, k)
inn = interior(al, 3); band = np.zeros(inn.shape, bool); band[int(sh[1]):wrap_top - 8] = True
m = inn & band & ~ol; px = a[m][:, :3]; Lp = lum(px)
for q in ('p5', 'p50', 'p95'):
    sel = np.abs(Lp - np.percentile(Lp, int(q[1:]))) <= 3
    got = np.median(px[sel], 0); want = np.array(legc['quantilesRGB'][q])
    check('bark colour %s matches the 22c leg' % q, float(np.abs(got - want).max()) <= tol[q], [int(round(v)) for v in got], '%s +/- %d' % (legc['quantilesRGB'][q], tol[q]))
edge = (al >= 200) & ~interior(al, 2) & band
got = np.median(a[edge][:, :3], 0)
check('outline colour matches the 22c leg', float(np.abs(got - np.array(legc['outlineMedianRGB'])).max()) <= tol['outline'],
      [int(round(v)) for v in got], '%s +/- %d' % (legc['outlineMedianRGB'], tol['outline']))
gf = float((Lp < np.median(Lp) - 35).mean())
check('grain amount (share of bark darker than median-35)', tol['grainFraction'][0] <= gf <= tol['grainFraction'][1], round(gf, 3), tol['grainFraction'])
prof = np.zeros(10); cnt = np.zeros(10)
for y in range(int(sh[1]), wrap_top - 8):
    xs = np.nonzero(al[y] >= 128)[0]
    if len(xs) < 10: continue
    l, r = xs.min(), xs.max()
    for x in range(l, r + 1):
        if al[y, x] < 255 or ol[y, x]: continue
        b = min(9, int((x - l) / (r - l + 1) * 10)); prof[b] += L[y, x]; cnt[b] += 1
prof = prof / np.maximum(cnt, 1)
pd = float(np.abs(prof - np.array(legc['crossProfileLum'])).max())
check('light across the limb (10 bins left to right) matches the 22c leg', pd <= tol['profileBin'], [round(v) for v in prof], '%s +/- %d' % ([round(v) for v in legc['crossProfileLum']], tol['profileBin']))

# 6. byte-identical parts and registration
for n, h in T['unchangedFilesSha256'].items():
    p = DELIVERY / n
    got = hashlib.sha256(p.read_bytes()).hexdigest() if p.exists() else 'missing'
    check('%s byte-identical to 22c/22d' % n, got == h, got[:12], h[:12])
reg_new = json.loads((DELIVERY / 'Qori_v3_Registration.json').read_text())
reg_old = json.loads((REF / 'Qori_v3_Registration.json').read_text())
req = T['registration']['required']
old_parts = {p['name']: p for p in reg_old['parts']}; new_parts = {p['name']: p for p in reg_new['parts']}
check('registration part names and order unchanged', [p['name'] for p in reg_new['parts']] == [p['name'] for p in reg_old['parts']],
      [p['name'] for p in reg_new['parts']], 'same as 22d')
bad = []
for name, op in old_parts.items():
    np_ = new_parts.get(name, {})
    for k in set(op) | set(np_):
        want = op.get(k)
        if '%s.%s' % (name, k) in req:
            want = req['%s.%s' % (name, k)]
        if np_.get(k) != want:
            bad.append('%s.%s' % (name, k))
check('registration parts identical to 22d (NearHand 8.9, FarHand 1.9)', not bad, bad or 'ok', 'no change')
allowed = {'parts', 'status', 'supersedes', 'sourceExtraction', 'barkColourNormalization'}
diff_keys = [k for k in set(reg_old) | set(reg_new) if k not in allowed and reg_old.get(k) != reg_new.get(k)]
check('registration top-level fields unchanged (except status, supersedes, Arm extraction/colour notes)', not diff_keys, diff_keys or 'ok', 'none')
se_bad = [k for k in reg_old['sourceExtraction'] if k != 'Arm' and reg_old['sourceExtraction'][k] != reg_new['sourceExtraction'].get(k)]
arm_x = reg_new['sourceExtraction'].get('Arm', {})
se_ok = not se_bad and arm_x.get('canvas') == reg_old['sourceExtraction']['Arm']['canvas'] and arm_x.get('pivotTopLeftPx') == reg_old['sourceExtraction']['Arm']['pivotTopLeftPx']
check('sourceExtraction unchanged except Arm notes; Arm canvas/pivot unchanged', se_ok, se_bad or 'ok', 'Arm canvas %s pivot %s' % (W, T['pivotTopLeftPx']))

# 7. skinned elbow at +90, +45, -20 (two-bone linear blend, the registration's own weight rule)
def rot(deg):
    r = math.radians(deg); return np.array([[math.cos(r), -math.sin(r)], [math.sin(r), math.cos(r)]])
def skin(img, angle, scale=1, pad=110):
    h, w = img.shape[:2]; pv = np.array(T['pivotTopLeftPx']); jy = T['joints']['elbow'][1]; half = T['bendBlendWidthPx'] / 2
    step = 3
    gx = np.unique(np.r_[np.arange(0, w, step), w - 1]); gy = np.unique(np.r_[np.arange(0, h, step), h - 1])
    X, Y = np.meshgrid(gx, gy); uv = np.stack([X.ravel(), Y.ravel()], 1).astype(float)
    q = uv - pv; jl = np.array([T['joints']['elbow'][0], jy]) - pv
    lower = np.clip((uv[:, 1] - jy + half) / (2 * half), 0, 1)
    # rotate in image space (y down): counter-clockwise on screen = clockwise in these coordinates
    R = rot(-angle)
    bent = jl + (q - jl) @ R.T
    dest = (q * (1 - lower[:, None]) + bent * lower[:, None]) * scale + pad * scale
    N = int((max(w, h) + 2 * pad) * scale)
    out = np.zeros((N, N, 4)); tex = img / 255.0; tex[..., :3] *= tex[..., 3:4]
    nx = len(gx)
    for iy in range(len(gy) - 1):
        for ix in range(nx - 1):
            i0 = iy * nx + ix; quad = [i0, i0 + 1, i0 + nx + 1, i0 + nx]
            if tex[int(uv[quad[0], 1]):int(uv[quad[2], 1]) + 1, int(uv[quad[0], 0]):int(uv[quad[2], 0]) + 1, 3].max() == 0:
                continue
            for tri in ((quad[0], quad[1], quad[2]), (quad[0], quad[2], quad[3])):
                p = dest[list(tri)]; s = uv[list(tri)]
                x0 = max(0, int(p[:, 0].min())); x1 = min(N - 1, int(math.ceil(p[:, 0].max())))
                y0_ = max(0, int(p[:, 1].min())); y1_ = min(N - 1, int(math.ceil(p[:, 1].max())))
                M = np.array([p[1] - p[0], p[2] - p[0]]).T
                if abs(np.linalg.det(M)) < 1e-9: continue
                XX, YY = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0_, y1_ + 1) + 0.5)
                bc = np.stack([XX - p[0, 0], YY - p[0, 1]], -1) @ np.linalg.inv(M).T
                ins = (bc[..., 0] >= -1e-6) & (bc[..., 1] >= -1e-6) & (bc.sum(-1) <= 1 + 1e-6)
                if not ins.any(): continue
                u = s[0] + bc[..., :1] * (s[1] - s[0]) + bc[..., 1:2] * (s[2] - s[0])
                ux = np.clip(u[..., 0], 0, w - 1.001); uy = np.clip(u[..., 1], 0, h - 1.001)
                fx = np.floor(ux).astype(int); fy = np.floor(uy).astype(int); tx = (ux - fx)[..., None]; ty = (uy - fy)[..., None]
                col = (tex[fy, fx] * (1 - tx) + tex[fy, fx + 1] * tx) * (1 - ty) + (tex[fy + 1, fx] * (1 - tx) + tex[fy + 1, fx + 1] * tx) * ty
                region = out[y0_:y1_ + 1, x0:x1 + 1]
                region[ins] = col[ins] + region[ins] * (1 - col[ins][:, 3:4])
    return out, (np.array(T['joints']['elbow']) - pv + pad) * scale

def interior_holes(mask):
    # transparent pixels not connected to the border
    from collections import deque
    h, w = mask.shape; seen = np.zeros_like(mask); dq = deque()
    for x in range(w):
        for y in (0, h - 1):
            if not mask[y, x] and not seen[y, x]: seen[y, x] = True; dq.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if not mask[y, x] and not seen[y, x]: seen[y, x] = True; dq.append((y, x))
    while dq:
        y, x = dq.popleft()
        for yy, xx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
            if 0 <= yy < h and 0 <= xx < w and not mask[yy, xx] and not seen[yy, xx]:
                seen[yy, xx] = True; dq.append((yy, xx))
    return int((~mask & ~seen).sum())

straight_holes = interior_holes(al >= 64)
check('straight arm: see-through pixels (alpha < 64) enclosed by the arm', straight_holes == 0, straight_holes, 0)
crops = []
for ang in T['elbowTests']['anglesDeg']:
    img, el = skin(a, ang)
    holes = interior_holes(img[..., 3] >= 0.25)
    check('skinned elbow %+d deg: see-through holes (alpha < 0.25) inside the limb' % ang, holes <= T['elbowTests']['maxInteriorHoles'], holes, 0)
    if ang == 90:
        # seam check along the bent forearm: rows across the horizontal forearm become columns
        pass
    ex, ey = int(el[0]), int(el[1])
    crop = img[max(0, ey - 100):ey + 100, max(0, ex - 100):ex + 100]
    bg = np.ones(crop.shape[:2] + (3,)) * 147 / 255.0
    rgb = crop[..., :3] + bg * (1 - crop[..., 3:4])
    crops.append(Image.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8)).resize((400, 400), Image.NEAREST))
sheet = Image.new('RGB', (400 * len(crops), 400), (147, 147, 147))
for i, cimg in enumerate(crops): sheet.paste(cimg, (400 * i, 0))
sheet.save(DELIVERY / 'Qori_v3_Proof_ArmCheck_Elbow_2x.png')

# 8. hand joins: arm and each hand at the registered draw order, upsampled 2x bilinear like the GPU
order_arm = new_parts['NearArm']['drawOrder']; order_hand = new_parts['NearHand']['drawOrder']
wrist = np.array(T['joints']['wrist'])
hj = T['handJoin']; join_imgs = []
for key in ('Hand_Relaxed', 'Hand_Grip', 'Hand_Spread'):
    hpath = DELIVERY / ('Qori_v3_%s.png' % key)
    hand = rgba(hpath); pv = np.array(reg_new['sourceExtraction'][key]['pivotTopLeftPx'])
    # place the hand in arm space on an enlarged canvas (extra rows below)
    EH = H + 140
    layer = np.zeros((EH, W + 80, 4)); ox = 40
    hy, hx = np.mgrid[0:hand.shape[0], 0:hand.shape[1]]
    # sub-pixel offset: resample the hand with bilinear at the fractional position
    off = wrist - pv
    fx, fy = off[0] - math.floor(off[0]), off[1] - math.floor(off[1])
    hs = Image.fromarray(hand.astype(np.uint8), 'RGBA')
    hs = hs.transform(hs.size, Image.Transform.AFFINE, (1, 0, -fx, 0, 1, -fy), Image.Resampling.BILINEAR)
    hp = np.array(hs).astype(float)
    y0h, x0h = int(math.floor(off[1])), int(math.floor(off[0])) + ox
    ys_, xs_ = slice(max(0, y0h), min(EH, y0h + hp.shape[0])), slice(max(0, x0h), min(W + 80, x0h + hp.shape[1]))
    layer[ys_, xs_] = hp[ys_.start - y0h:ys_.stop - y0h, xs_.start - x0h:xs_.stop - x0h]
    arml = np.zeros_like(layer); arml[:H, ox:ox + W] = a
    def up(x):
        # Pillow resizes RGBA with premultiplied alpha internally and returns straight colour; premultiply after
        r = np.array(Image.fromarray(np.clip(x, 0, 255).astype(np.uint8), 'RGBA').resize((x.shape[1] * 2, x.shape[0] * 2), Image.BILINEAR)).astype(float) / 255.0
        r[..., :3] *= r[..., 3:4]
        return r
    A, Hh = up(arml), up(layer)
    top, bot = (A, Hh) if order_arm > order_hand else (Hh, A)
    comp = top + bot * (1 - top[..., 3:4])
    # gap: in every column where the arm is solid just above the hands' top row and the hand is solid just below
    # it, the composite must stay covered across the join (a see-through line here is what Unity showed in 22c)
    top = int(math.floor(T['handCover']['handTopRowInArm']))
    cols = (arml[top - 1, :, 3] >= 250) & (layer[top + 2, :, 3] >= 250)
    cols = cols & np.roll(cols, 1) & np.roll(cols, -1) & np.roll(cols, 2) & np.roll(cols, -2)  # skip 2 outline columns
    cols2 = np.repeat(cols, 2)
    gap = np.zeros(comp.shape[:2], bool)
    gap[(top - 1) * 2:(top + 3) * 2, :] = (comp[(top - 1) * 2:(top + 3) * 2, :, 3] < hj['gapAlphaMin']) & cols2[None, :]
    both = np.kron(((arml[..., 3] >= 128) | (layer[..., 3] >= 128)).astype(np.uint8), np.ones((2, 2), np.uint8)).astype(bool)
    both = shrink(shrink(grow(both, 4), 4), 3)
    rows = slice((top - 8) * 2, (top + 14) * 2)
    gy_, gx_ = np.nonzero(gap)
    where = 'arm px x %d-%d, rows %d-%d' % (gx_.min() / 2 - ox, gx_.max() / 2 - ox, gy_.min() / 2, gy_.max() / 2) if len(gy_) else 'none'
    check('%s join: no see-through line (coverage < %.2f inside arm+hand)' % (key, hj['gapAlphaMin']), int(gap.sum()) == 0, '%d px at %s' % (int(gap.sum()), where), 0)
    bgc = np.array([147, 147, 147]) / 255.0
    rgb = comp[..., :3] + bgc * (1 - comp[..., 3:4])
    ca = np.concatenate([rgb * 255, comp[..., 3:4] * 255], -1)
    ca[..., 3] = np.where(both, 255, 0)
    sr = seam_rows(ca[rows], 1, rows.stop - rows.start - 1)
    wc_ = max(sr, key=lambda r: r[3]); ws_ = max(sr, key=lambda r: abs(r[1]))
    check('%s join: no straight edge across the wrist (coherent step)' % key, wc_[3] <= hj['maxCoherent'],
          '2x row %d: %.2f' % (wc_[0] + rows.start, wc_[3]), hj['maxCoherent'])
    check('%s join: no brightness step across the wrist' % key, abs(ws_[1]) <= hj['maxAbsSignedStep'],
          '2x row %d: %.1f' % (ws_[0] + rows.start, ws_[1]), hj['maxAbsSignedStep'])
    cy = int(wrist[1] * 2) + 70; cx_ = int((wrist[0] + ox) * 2)
    join_imgs.append(Image.fromarray(np.clip(rgb * 255, 0, 255).astype(np.uint8)).crop((cx_ - 130, cy - 130, cx_ + 130, cy + 130)))
js = Image.new('RGB', (260 * 3, 260))
for i, ji in enumerate(join_imgs): js.paste(ji, (260 * i, 0))
js.save(DELIVERY / 'Qori_v3_Proof_ArmCheck_HandJoin_2x.png')

# 9. the wrist (Batch 22e): rows 236-256 repainted as bark, everything above locked
wt = T['wrist22e']
ref_arm = rgba(REF / 'Qori_v3_Arm.png')
lk = wt['lockedRows'][1] + 1
diffm = (a[:lk] != ref_arm[:lk]).any(-1)
check('rows 0-%d pixel-identical to the 22d arm' % (lk - 1), int(diffm.sum()) == 0, int(diffm.sum()), 0)
r_, g_, b_ = a[..., 0], a[..., 1], a[..., 2]
green = (al >= 64) & (g_ > r_ + 15) & (g_ > b_ + 40)
gy, gx = np.nonzero(green)
check('no stray bright-green pixel (g > r+15 and g > b+40)', len(gy) == 0, [(int(x), int(y)) for y, x in zip(gy, gx)][:5] or 0, 0)
def detail(img, y0, y1, amin=250):
    Lx = lum(img); ok = (img[..., 3] >= amin) & ~olive(img)
    ok2 = ok[:, 1:] & ok[:, :-1]; ok2 &= np.roll(ok2, 2, 1) & np.roll(ok2, -2, 1)
    band = np.zeros(ok2.shape, bool); band[y0:y1 + 1] = True
    d = np.abs(np.diff(Lx, axis=1))[ok2 & band]
    return float(d.mean()) if len(d) else 0.0
dt = wt['detail']; p0 = wt['flareRows'][0]; c0 = T['handCover']['firstRow']; c1 = T['handCover']['opaqueThroughRow']
for (y0_, y1_, amin, lim, what) in ((p0, wt['flareRows'][1], 250, dt['minFlareAndCover236_245'], 'flare'),
                                    (c0, c1, 250, dt['minFlareAndCover236_245'], 'opaque hand cover'),
                                    (c1 + 1, 252, 128, dt['minFade246_252_alpha128'], 'fade (alpha >= 128)')):
    v = detail(a, y0_, y1_, amin)
    check('painted grain, not smeared: %s rows %d-%d, mean horizontal luminance detail' % (what, y0_, y1_), v >= lim,
          round(v, 2), '>= %.1f (bark above %.1f, hands %s; 22d %s)' % (lim, dt['barkRows140_175'],
          [v_['detailTop14Rows'] for v_ in dt['handsTop14Rows'].values()], round(detail(ref_arm, y0_, y1_, amin), 2)))
def grain_ratio(img, y0, y1):
    Lx = lum(img); ol_ = olive(img); g = ol_.copy()
    for k in (-2, -1, 1, 2):
        g |= np.roll(ol_, k, 1) | np.roll(ol_, k, 0)
    okk = img[..., 3] >= 255
    for k in (1, 2):
        okk &= np.roll(okk, k, 1) & np.roll(okk, -k, 1) & np.roll(okk, k, 0) & np.roll(okk, -k, 0)
    m = okk & ~g; bnd = np.zeros(m.shape, bool); bnd[y0:y1 + 1] = True; m &= bnd
    dh = np.abs(Lx - np.roll(Lx, 1, 1))[m & np.roll(m, 1, 1)]; dv = np.abs(Lx - np.roll(Lx, 1, 0))[m & np.roll(m, 1, 0)]
    return float(dh.mean() / dv.mean()) if len(dh) and len(dv) and dv.mean() > 0 else 0.0
gd = wt['grainDirection']; gm = gd['measured']
olr = np.nonzero(olive(a).sum(1) >= 4)[0]
wrap_bot = int(olr.max()) if len(olr) else wt['flareRows'][0] - 1
for (y0_, y1_, lim, what, was) in ((wrap_top, wt['flareRows'][0] - 1, gd['minBesideWrap'], 'bark beside the wrap', gm['arm22dBesideWrap216_235']),
                                   (wt['flareRows'][0], wt['flareRows'][1], gd['minFlare236_242'], 'flare', gm['arm22dFlare236_242'])):
    v = grain_ratio(a, y0_, y1_)
    check('grain runs along the arm, no cross streaks: %s rows %d-%d, across/along detail ratio' % (what, y0_, y1_), v >= lim, round(v, 2),
          '>= %.1f (arm bark %.2f, leg beside its wrap %.2f; 22d %.2f)' % (lim, gm['armBark60_175'], gm['leg22cBesideWrap300_340'], was))
rows_b = seam_rows(a, wrap_bot + 1, c1)
if rows_b:
    ws_ = max(rows_b, key=lambda r: abs(r[1])); wa_ = max(rows_b, key=lambda r: r[2]); wc_ = max(rows_b, key=lambda r: r[3])
    check('seam below the wrap: largest row-to-row mean luminance step, rows %d-%d' % (wrap_bot + 1, c1), abs(ws_[1]) <= sm['maxAbsSignedStep'], 'row %d: %.1f' % (ws_[0], ws_[1]), sm['maxAbsSignedStep'])
    check('seam below the wrap: largest mean absolute row step', wa_[2] <= sm['maxMeanAbsStep'], 'row %d: %.1f' % (wa_[0], wa_[2]), sm['maxMeanAbsStep'])
    check('seam below the wrap: largest share of a row stepping >10 the same way', wc_[3] <= sm['maxCoherent'], 'row %d: %.2f' % (wc_[0], wc_[3]), sm['maxCoherent'])
else:
    check('seam below the wrap: bark rows to measure', False, 0, '> 0')
ok = (al >= 250) & ~olive(a); band = np.zeros(ok.shape, bool); band[p0:c1 + 1] = True
ml = float(np.median(lum(a)[ok & band])); lo, hi = wt['medianLum236_245']
check('wrist bark tone between the arm bark and the hands: median luminance rows %d-%d' % (p0, c1), lo <= ml <= hi, round(ml, 1), [lo, hi])
allh = np.ones((H, W), bool); hand_masks = {}
for key in ('Hand_Relaxed', 'Hand_Grip', 'Hand_Spread'):
    pv = np.array(reg_new['sourceExtraction'][key]['pivotTopLeftPx'], float)
    hh = np.array(Image.open(DELIVERY / ('Qori_v3_%s.png' % key)).convert('RGBA'))[..., 3]
    m = np.zeros((H, W), bool); hy, hx = np.nonzero(hh >= 128)
    ay = np.round(hy - pv[1] + T['joints']['wrist'][1]).astype(int); ax = np.round(hx - pv[0] + T['joints']['wrist'][0]).astype(int)
    k_ = (ay >= 0) & (ay < H) & (ax >= 0) & (ax < W); m[ay[k_], ax[k_]] = True
    hand_masks[key] = m; allh &= m
for key, m in hand_masks.items():
    over = (al > 32) & ~m; over[:c0] = False
    check('hand cover stays on the %s (alpha > 32 below row %d only where the hand is)' % (key[5:], c0 - 1), int(over.sum()) <= wt['maxOverhangPxPerHand'],
          int(over.sum()), wt['maxOverhangPxPerHand'])
cols = allh.copy()
for dx in (-2, -1, 1, 2): cols &= np.roll(allh, dx, 1)
fd = wt['fade']; prev = None; worst = (None, 0.0)
for y in range(fd['rows'][0], fd['rows'][1] + 1):
    if not cols[y].any(): continue
    m_ = float(al[y, cols[y]].mean())
    if prev is not None and prev - m_ > worst[1]: worst = (y, prev - m_)
    prev = m_
check('soft fade: largest drop in mean alpha from one row to the next, rows %d-%d' % tuple(fd['rows']), worst[1] <= fd['maxMeanAlphaDropPerRow'],
      'row %s: %.1f' % worst, fd['maxMeanAlphaDropPerRow'])
check('fade ends: mean alpha of row %d' % fd['rows'][1], prev is not None and prev <= fd['maxMeanAlphaLastRow'], round(prev or 0, 1), '<= %d' % fd['maxMeanAlphaLastRow'])

# 10. the wrap edge (Batch 22f): only the edge zone may change; no dark band, a soft edge, no steps
we = T['wrapEdge22f']
arm22e = rgba(INPUT / 'reference_22e' / 'Qori_v3_Arm.png')
_same_e = hashlib.sha256(arm_path.read_bytes()).hexdigest() == T['arm22eSha256']
check('not byte-identical to the 22e arm', not _same_e, 'identical' if _same_e else 'sha256 differs', 'must differ')
zone = np.array(Image.open(INPUT / 'Qori_v3_Arm_Mask_EdgeZone.png')) > 127
changed = (a != arm22e).any(-1) & ~zone
cy_, cx_ = np.nonzero(changed)
check('every pixel outside Qori_v3_Arm_Mask_EdgeZone identical to the 22e arm', int(changed.sum()) == 0,
      '%d px%s' % (int(changed.sum()), (' e.g. x %d row %d' % (cx_[0], cy_[0])) if len(cy_) else ''), 0)
r0, r1 = we['rows']; Lr = lum(a); olr_ = olive(a)
rims = {'L': [], 'R': []}; softs = {'L': 0, 'R': 0}; steps = {'L': [], 'R': []}; prev = None; nrows = 0
for y in range(r0, r1 + 1):
    xs = np.nonzero(al[y] >= 128)[0]; xa = np.nonzero(al[y] > 0)[0]
    if not len(xs): continue
    l, r = xs.min(), xs.max(); nrows += 1
    k = 0
    while l + k < r and not olr_[y, l + k] and Lr[y, l + k] < 115: k += 1
    rims['L'].append(k)
    k = 0
    while r - k > l and not olr_[y, r - k] and Lr[y, r - k] < 115: k += 1
    rims['R'].append(k)
    softs['L'] += int(0 < al[y, xa.min()] < 224); softs['R'] += int(0 < al[y, xa.max()] < 224)
    if prev is not None:
        steps['L'].append(abs(int(l) - prev[0])); steps['R'].append(abs(int(r) - prev[1]))
    prev = (int(l), int(r))
dr = we['darkRim']; m_ = we['measured']
for s, name in (('L', 'left'), ('R', 'right')):
    v = rims[s]
    check('no dark band at the %s edge of the wrap, rows %d-%d: mean / largest dark-rim width' % (name, r0, r1),
          np.mean(v) <= dr['maxMeanPerSide'] and max(v) <= dr['maxPerRow'], '%.1f / %d px' % (np.mean(v), max(v)),
          'mean <= %.0f, max <= %d (arm above %.1f / %d; 22e %.1f / %d)' % (dr['maxMeanPerSide'], dr['maxPerRow'], m_['darkRimArm60_175'][s]['mean'],
          m_['darkRimArm60_175'][s]['max'], m_['darkRim22e180_242'][s]['mean'], m_['darkRim22e180_242'][s]['max']))
    sh = softs[s] / max(nrows, 1)
    check('soft (antialiased) %s edge: share of rows %d-%d whose outermost pixel has alpha 1-223' % (name, r0, r1), sh >= we['softEdge']['minShare'],
          round(sh, 2), '>= %.1f (arm above %.2f; 22e %.2f)' % (we['softEdge']['minShare'], m_['softEdgeArm60_175'][s], m_['softEdge22e180_242'][s]))
    st = max(steps[s]) if steps[s] else 0
    check('smooth %s silhouette: largest change of the edge from one row to the next' % name, st <= we['maxEdgeStepPx'], '%d px' % st, '<= %d' % we['maxEdgeStepPx'])
zb = we['zoneBoundary']; olg = olive(a)
for _ in range(2):  # the leaves and their outline: their own edge is meant to contrast, so they are left out here
    for k_ in (-1, 1):
        olg = olg | np.roll(olg, k_, 1) | np.roll(olg, k_, 0)
okb = (al >= 250) & ~olg
for d_, name in ((1, 'left'), (-1, 'right')):
    # left side: the zone is outside (left of) the locked bark, so its locked neighbour is at x+1; right side at x-1
    m = zone & okb & np.roll(okb & ~zone, -d_, 1)
    L22e = lum(arm22e); m &= arm22e[..., 3] >= 250
    diff = Lr[m] - np.roll(Lr, -d_, 1)[m]; diff_e = L22e[m] - np.roll(L22e, -d_, 1)[m]
    sgn, ab = (float(diff.mean()), float(np.abs(diff).mean())) if m.any() else (0.0, 0.0)
    sgn_e, ab_e = (float(diff_e.mean()), float(np.abs(diff_e).mean())) if m.any() else (0.0, 0.0)
    check('repaint blends into the untouched bark at the %s inner edge of the zone: mean / mean absolute step, against 22e' % name,
          abs(sgn - sgn_e) <= zb['maxSignedChange'] and ab - ab_e <= zb['maxAbsIncrease'],
          '%+.1f / %.1f (22e %+.1f / %.1f, %d px)' % (sgn, ab, sgn_e, ab_e, int(m.sum())),
          'mean within %.0f of 22e, abs at most %.0f above 22e' % (zb['maxSignedChange'], zb['maxAbsIncrease']))

# template overlay proof
S = 4
tpl = Image.open(INPUT / 'Qori_v3_Arm_Template.png').convert('RGBA').resize((W * S, H * S), Image.NEAREST)
base = Image.new('RGBA', (W * S, H * S), (255, 255, 255, 255)); base.alpha_composite(tpl)
armb = Image.fromarray(a.astype(np.uint8), 'RGBA').resize((W * S, H * S), Image.NEAREST)
ab = np.array(armb).astype(float); ab[..., 3] *= 0.8
base.alpha_composite(Image.fromarray(ab.astype(np.uint8), 'RGBA'))
bad_px = np.kron((outside | holes).astype(np.uint8), np.ones((S, S), np.uint8)).astype(bool)
arr = np.array(base); arr[bad_px] = (255, 0, 0, 255)
Image.fromarray(arr).convert('RGB').save(DELIVERY / 'Qori_v3_Proof_ArmCheck_Template.png')

failed = [r for r in results if not r['pass']]
(DELIVERY / 'Qori_v3_ARM_CHECK.json').write_text(json.dumps({'allPass': not failed, 'results': results}, indent=1))
print()
print('ALL PASS' if not failed else '%d FAILED: fix and run again' % len(failed))
sys.exit(0 if not failed else 1)
