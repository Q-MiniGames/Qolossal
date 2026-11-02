"""Batch 27 acceptance checker: Qori's painted walk cycle against the rig-rendered guide frames.

Usage: python check_walk_27.py <delivery_dir> <input_dir>
  <input_dir> is Batch27_Input (guides, masks, layers, batch27_guide_frames.json, batch27_targets.json,
  Qori_Walk_Registration_TEMPLATE.json). Writes Qori_Walk_CHECK.json and the proofs into <delivery_dir>.
Python 3, numpy and Pillow only. Prints PASS/FAIL per test; the last line is ALL PASS or N FAIL.
"""
import hashlib, json, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

DELIVERY, INPUT = sys.argv[1], sys.argv[2]
G = json.load(open(os.path.join(INPUT, 'batch27_guide_frames.json')))
T = json.load(open(os.path.join(INPUT, 'batch27_targets.json')))
W, H = G['canvas']
N = G['frameCount']
results = []


def test(section, name, ok, value, limit):
    results.append(dict(section=section, test=name, ok=bool(ok), value=value, limit=limit))
    print('%s  [%s] %s: %s (limit %s)' % ('PASS' if ok else 'FAIL', section, name, value, limit))


def rgba(path):
    return np.array(Image.open(path).convert('RGBA'))


def mask(path):
    return np.array(Image.open(path).convert('L'))


def _shift_or(m, s, axis):
    out = m.copy()
    if axis == 0: out[s:] |= m[:-s]; out[:-s] |= m[s:]
    else: out[:, s:] |= m[:, :-s]; out[:, :-s] |= m[:, s:]
    return out


def dilate(m, r):
    """Square dilation by r px (separable, by doubling)."""
    m = m > 0
    if r <= 0: return m
    for axis in (0, 1):
        reach = 0
        while reach < r:
            s = min(reach + 1, r - reach)        # OR with a shift of s keeps the run contiguous
            m = _shift_or(m, s, axis); reach += s
    return m


def erode(m, r):
    if r <= 0: return m > 0
    return ~dilate(~(m > 0), r)


def lum(a):
    a = a.astype(np.float32)
    return a[:, :, 0] * .299 + a[:, :, 1] * .587 + a[:, :, 2] * .114


def detail(img, m):
    l = lum(img)
    mm = m[:, 1:] & m[:, :-1]
    return float(np.abs(np.diff(l, axis=1))[mm].mean()) if mm.any() else 0.0


def iou(a, b, region=None):
    if region is not None: a, b = a & region, b & region
    u = (a | b).sum()
    return float((a & b).sum() / u) if u else 1.0


def run_width(alpha_bool, x, y):
    x, y = int(round(x)), int(round(y))
    if not (0 <= y < H and 0 <= x < W) or not alpha_bool[y, x]: return 0
    row = alpha_bool[y]
    l = x
    while l > 0 and row[l - 1]: l -= 1
    r = x
    while r < W - 1 and row[r + 1]: r += 1
    return r - l + 1


name = lambda k: 'Qori_Walk_%02d' % k
YY, XX = np.mgrid[0:H, 0:W]
frames = G['frames']
painted, guides, fronts, gfronts = [], [], [], []

# ---------------------------------------------------------------- 1. files and format
S = '1 format'
for k in range(1, N + 1):
    for kind in ('Body', 'FrontHand'):
        p = os.path.join(DELIVERY, '%s_%s.png' % (name(k), kind))
        if not os.path.exists(p):
            test(S, '%s_%s.png exists' % (name(k), kind), False, 'missing', 'present'); continue
        im = Image.open(p)
        test(S, '%s_%s.png is %dx%d RGBA' % (name(k), kind, W, H), im.size == (W, H) and im.mode == 'RGBA',
             '%dx%d %s' % (im.size[0], im.size[1], im.mode), '%dx%d RGBA' % (W, H))
missing = [r for r in results if not r['ok']]
if missing:
    print('%d FAIL (files missing or wrong format; nothing else checked)' % len(missing)); sys.exit(1)

for k in range(1, N + 1):
    painted.append(rgba(os.path.join(DELIVERY, name(k) + '_Body.png')))
    fronts.append(rgba(os.path.join(DELIVERY, name(k) + '_FrontHand.png')))
    guides.append(rgba(os.path.join(INPUT, 'guides', name(k) + '_Body_Guide.png')))
    gfronts.append(rgba(os.path.join(INPUT, 'guides', name(k) + '_FrontHand_Guide.png')))

for k in range(N):
    for kind, img in (('Body', painted[k]), ('FrontHand', fronts[k])):
        a = img[:, :, 3]
        dirty = int(img[:, :, :3][a == 0].max()) if (a == 0).any() else 0
        test(S, '%s %s straight alpha (RGB = 0 where alpha = 0)' % (name(k + 1), kind), dirty == 0, dirty, 0)
        vis = a > 0
        # soft px further than 3 px from any solid px: a glow or aura, not edge antialiasing
        soft = float(((a > 0) & (a < 250) & ~dilate(a >= 250, 3)).sum() / max(vis.sum(), 1))
        test(S, '%s %s no halo or soft aura (share of partly transparent px)' % (name(k + 1), kind),
             soft <= T['maxSoftAlphaShare'], round(soft, 4), T['maxSoftAlphaShare'])
hashes = [hashlib.sha256(open(os.path.join(DELIVERY, name(k) + '_Body.png'), 'rb').read()).hexdigest() for k in range(1, N + 1)]
test(S, 'all 16 Body frames are different files', len(set(hashes)) == N, len(set(hashes)), N)

# ---------------------------------------------------------------- 2. silhouette against the guide
S = '2 silhouette'
P = [p[:, :, 3] > 128 for p in painted]
Gm = [g[:, :, 3] > 128 for g in guides]
for k in range(N):
    nm = name(k + 1)
    cape = mask(os.path.join(INPUT, 'masks', nm + '_Mask_cape.png')) > 128
    cape_zone = dilate(cape, T['capeFreedomPx'])
    noncape = np.zeros((H, W), bool)
    for g in ('torso', 'arm_back', 'arm_front', 'leg_back', 'leg_front'):
        noncape |= mask(os.path.join(INPUT, 'masks', '%s_Mask_%s.png' % (nm, g))) > 128
    core_region = ~cape_zone | noncape      # the cloak may move; everything drawn over it may not
    tol = T['edgeTolerancePx']
    pr = float((P[k] & core_region & dilate(Gm[k], tol)).sum() / max((P[k] & core_region).sum(), 1))
    rc = float((Gm[k] & core_region & dilate(P[k], tol)).sum() / max((Gm[k] & core_region).sum(), 1))
    test(S, '%s body (not cloak): painted px within %d px of the guide' % (nm, tol), pr >= T['minCoreMatch'], round(pr, 4), '>= %s' % T['minCoreMatch'])
    test(S, '%s body (not cloak): guide px within %d px of paint (nothing missing)' % (nm, tol), rc >= T['minCoreMatch'], round(rc, 4), '>= %s' % T['minCoreMatch'])
    v = iou(P[k], Gm[k])
    test(S, '%s whole Body layer overlap with guide, IoU' % nm, v >= T['minBodyIoU'], round(v, 3), '>= %s' % T['minBodyIoU'])
    outside = P[k] & ~dilate(Gm[k], T['outlineTolerancePx']) & core_region
    v = float(outside.sum() / Gm[k].sum())
    test(S, '%s paint outside the guide (+%d px, cloak zone excepted)' % (nm, T['outlineTolerancePx']), v <= T['maxOutsideShare'],
         round(v, 4), '<= %s' % T['maxOutsideShare'])
    r = float(P[k].sum() / Gm[k].sum())
    test(S, '%s Body area / guide area' % nm, T['bodyAreaRatio'][0] <= r <= T['bodyAreaRatio'][1], round(r, 3), T['bodyAreaRatio'])
    pc = float((P[k] & cape_zone & ~dilate(Gm[k] & ~cape, 4)).sum()); gc = float((cape & ~dilate(Gm[k] & ~cape, 4)).sum())
    r = pc / max(gc, 1)
    test(S, '%s cloak area / guide cloak area' % nm, T['capeAreaRatio'][0] <= r <= T['capeAreaRatio'][1], round(r, 3), T['capeAreaRatio'])
    head = mask(os.path.join(INPUT, 'masks', nm + '_Mask_head.png')) > 20
    ears = mask(os.path.join(INPUT, 'masks', nm + '_Mask_ears.png')) > 20
    v = int((P[k] & (head | ears) & ~dilate(Gm[k], 6)).sum())
    test(S, '%s no head or ears painted (px in the head/ear area outside the guide body)' % nm, v <= T['maxHeadPaintPx'], v,
         '<= %s' % T['maxHeadPaintPx'])

# ---------------------------------------------------------------- 3. ground, bob and the planted foot
S = '3 ground'
tops_p, tops_g = [], []
for k in range(N):
    nm = name(k + 1); f = frames[k]
    rows = np.where(P[k].any(axis=1))[0]; grows = np.where(Gm[k].any(axis=1))[0]
    low, glow = int(rows.max()), int(grows.max())
    test(S, '%s lowest sole row (guide %d)' % (nm, glow), glow - T['soleTolerancePx'] <= low <= glow + 1, low,
         '[%d, %d]' % (glow - T['soleTolerancePx'], glow + 1))
    band = np.zeros((H, W), bool); band[G['footLineRow'] - T['footBandPx']:G['footLineRow'] + 2] = True
    v = iou(P[k], Gm[k], band)
    test(S, '%s feet band (bottom %d rows) overlap, IoU' % (nm, T['footBandPx']), v >= T['minFootBandIoU'], round(v, 3),
         '>= %s' % T['minFootBandIoU'])
    for side, ankle in (('far-side', 'ankleBack'), ('camera-side', 'ankleFront')):
        ax, ay = f[ankle]
        if ay < G['footLineRow'] - 90: continue          # foot in the air
        box = np.zeros((H, W), bool); box[int(ay) - 10:G['footLineRow'] + 2, int(ax) - 70:int(ax) + 90] = True
        legm = mask(os.path.join(INPUT, 'masks', '%s_Mask_leg_%s.png' % (nm, 'back' if side == 'far-side' else 'front'))) > 128
        gx = np.where(legm & box)[1]; px = np.where(P[k] & box & dilate(legm, 10))[1]
        if len(gx) == 0 or len(px) == 0:
            test(S, '%s %s planted foot present' % (nm, side), False, 'none', 'present'); continue
        dx = abs(float(px.mean()) - float(gx.mean()))
        test(S, '%s %s planted foot position (no skating), |dx| px' % (nm, side), dx <= T['maxFootShiftPx'], round(dx, 1),
             '<= %s' % T['maxFootShiftPx'])
    tops_p.append(int(rows.min())); tops_g.append(int(grows.min()))
    test(S, '%s top of the mantle row (bob; guide %d)' % (nm, grows.min()), abs(rows.min() - grows.min()) <= T['bobTolerancePx'],
         int(rows.min()), '+/- %s' % T['bobTolerancePx'])
c = float(np.corrcoef(tops_p, tops_g)[0, 1]) if np.std(tops_p) > 0 else 0.0
test(S, 'bob follows the guide over the cycle (correlation of mantle tops)', c >= T['minBobCorrelation'], round(c, 3),
     '>= %s' % T['minBobCorrelation'])
for k in range(N):
    nm = name(k + 1)
    head = mask(os.path.join(INPUT, 'masks', nm + '_Mask_head.png')) > 20
    need = (guides[k][:, :, 3] >= 250) & head
    got = float((painted[k][:, :, 3][need] >= 250).mean()) if need.any() else 1.0
    test(S, '%s neck and collar solid under the head (share)' % nm, got >= T['minUnderHeadSolid'], round(got, 3),
         '>= %s' % T['minUnderHeadSolid'])

# ---------------------------------------------------------------- 4. palette, detail and part sizes
S = '4 colour and size'
GROUP_COLOURS = {g: tuple(c) for g, c in G['groupColours'].items()}
for k in range(N):
    nm = name(k + 1)
    flat = rgba(os.path.join(INPUT, 'guides', nm + '_Parts.png'))
    for g in ('torso', 'cape', 'leg_back', 'leg_front'):
        col = np.array(GROUP_COLOURS[g])
        vis = (np.abs(flat[:, :, :3].astype(int) - col).max(axis=2) <= 3) & (flat[:, :, 3] == 255)
        vis = erode(vis, 3) & (painted[k][:, :, 3] >= 250)
        if vis.sum() < 200: continue
        mp = np.median(painted[k][:, :, :3][vis], axis=0); mg = np.median(guides[k][:, :, :3][vis], axis=0)
        d = float(np.abs(mp - mg).max())
        test(S, '%s %s median colour vs current Qori, max channel difference' % (nm, g), d <= T['maxMedianColourDiff'], round(d, 1),
             '<= %s' % T['maxMedianColourDiff'])
    a = painted[k][:, :, 3]
    hsv = np.array(Image.fromarray(painted[k][:, :, :3]).convert('HSV')).astype(np.float32) / 255
    neon = float(((hsv[:, :, 1] > .85) & (hsv[:, :, 2] > .6) & (a > 128)).sum() / max((a > 128).sum(), 1))
    test(S, '%s no neon colour (share of very saturated bright px)' % nm, neon <= T['maxNeonShare'], round(neon, 4), '<= %s' % T['maxNeonShare'])
    core = erode(Gm[k] & P[k], 3)
    dp, dg = detail(painted[k], core), detail(guides[k], core)
    r = dp / max(dg, 1e-6)
    test(S, '%s painted detail / current Qori detail (not smeared, not noisy)' % nm, T['detailRatio'][0] <= r <= T['detailRatio'][1],
         round(r, 3), T['detailRatio'])
    f = frames[k]
    for side, knee, ankle in (('far-side', 'kneeBack', 'ankleBack'), ('camera-side', 'kneeFront', 'ankleFront')):
        ratios = []
        for s in np.linspace(.3, .7, 9):
            x = f[knee][0] + (f[ankle][0] - f[knee][0]) * s; y = f[knee][1] + (f[ankle][1] - f[knee][1]) * s
            wg, wp = run_width(Gm[k], x, y), run_width(P[k], x, y)
            if wg: ratios.append(wp / wg)
        if not ratios: continue
        r = float(np.median(ratios))
        test(S, '%s %s shin width / current Qori shin' % (nm, side), T['limbWidthRatio'][0] <= r <= T['limbWidthRatio'][1], round(r, 3),
             T['limbWidthRatio'])

# ---------------------------------------------------------------- 5. the front hand on the weapon
S = '5 front hand'
for k in range(N):
    nm = name(k + 1); f = frames[k]
    pf, gf = fronts[k][:, :, 3] > 128, gfronts[k][:, :, 3] > 128
    out = float((fronts[k][:, :, 3] > 32)[~dilate(gf, T['handTolerancePx'])].sum() / max(gf.sum(), 1))
    test(S, '%s FrontHand inside the guide (+%d px)' % (nm, T['handTolerancePx']), out <= T['maxOutsideShare'], round(out, 4),
         '<= %s' % T['maxOutsideShare'])
    r = float(pf.sum() / max(gf.sum(), 1))
    test(S, '%s FrontHand area / guide' % nm, T['handAreaRatio'][0] <= r <= T['handAreaRatio'][1], round(r, 3), T['handAreaRatio'])
    staff = mask(os.path.join(INPUT, 'masks', nm + '_Mask_staff.png')) > 128
    disc = (XX - f['weaponGrip'][0]) ** 2 + (YY - f['weaponGrip'][1]) ** 2 <= T['gripRadiusPx'] ** 2
    zone = staff & disc
    cp, cg = float((pf & zone).sum() / max(zone.sum(), 1)), float((gf & zone).sum() / max(zone.sum(), 1))
    test(S, '%s fingers close over the shaft at the grip (share of shaft covered; guide %.2f)' % (nm, cg),
         cp >= T['minGripCover'] * cg, round(cp, 3), '>= %.3f' % (T['minGripCover'] * cg))
    v = iou(P[k], Gm[k], dilate(staff, 4) & ~disc)
    allstaff = float((P[k] & staff & ~Gm[k]).sum() / max(staff.sum(), 1))
    test(S, '%s no weapon painted into the Body layer (share of shaft px painted where the guide is empty)' % nm,
         allstaff <= T['maxStaffPaintShare'], round(allstaff, 4), '<= %s' % T['maxStaffPaintShare'])

# ---------------------------------------------------------------- 6. loop and flicker
S = '6 loop'
def xor_area(a, b): return float((a ^ b).sum())
pairs = [(k, (k + 1) % N) for k in range(N)]
xp = [xor_area(P[a], P[b]) for a, b in pairs]; xg = [xor_area(Gm[a], Gm[b]) for a, b in pairs]
med = float(np.median(xp))
for (a, b), vp, vg in zip(pairs, xp, xg):
    r = vp / max(vg, 1)
    test(S, '%s -> %s silhouette change / guide change' % (name(a + 1), name(b + 1)), T['silhouetteChangeRatio'][0] <= r <= T['silhouetteChangeRatio'][1],
         round(r, 3), T['silhouetteChangeRatio'])
    test(S, '%s -> %s no pop (change / median change)' % (name(a + 1), name(b + 1)), vp <= T['maxPopFactor'] * med, round(vp / med, 2),
         '<= %s' % T['maxPopFactor'])
for a, b in pairs:
    for g in ('torso', 'cape'):
        flat_a = rgba(os.path.join(INPUT, 'guides', name(a + 1) + '_Parts.png')); flat_b = rgba(os.path.join(INPUT, 'guides', name(b + 1) + '_Parts.png'))
        col = np.array(GROUP_COLOURS[g])
        va = erode((np.abs(flat_a[:, :, :3].astype(int) - col).max(axis=2) <= 3) & (flat_a[:, :, 3] == 255), 3) & (painted[a][:, :, 3] >= 250)
        vb = erode((np.abs(flat_b[:, :, :3].astype(int) - col).max(axis=2) <= 3) & (flat_b[:, :, 3] == 255), 3) & (painted[b][:, :, 3] >= 250)
        if va.sum() < 200 or vb.sum() < 200: continue
        d = float(np.abs(np.median(painted[a][:, :, :3][va], axis=0) - np.median(painted[b][:, :, :3][vb], axis=0)).max())
        dg = float(np.abs(np.median(guides[a][:, :, :3][va], axis=0) - np.median(guides[b][:, :, :3][vb], axis=0)).max())
        lim = dg + T['maxNeighbourColourStep']
        test(S, '%s -> %s %s colour steady (median change; guide %.0f)' % (name(a + 1), name(b + 1), g, dg), d <= lim, round(d, 1),
             '<= %.0f' % lim)
    ca, cb = erode(Gm[a] & P[a], 3), erode(Gm[b] & P[b], 3)
    da, db = detail(painted[a], ca), detail(painted[b], cb)
    r = abs(da - db) / max(da, db, 1e-6)
    test(S, '%s -> %s detail steady (relative change)' % (name(a + 1), name(b + 1)), r <= T['maxNeighbourDetailStep'], round(r, 3),
         '<= %s' % T['maxNeighbourDetailStep'])

# ---------------------------------------------------------------- 7. registration
S = '7 registration'
rp = os.path.join(DELIVERY, 'Qori_Walk_Registration.json')
tp = os.path.join(INPUT, 'Qori_Walk_Registration_TEMPLATE.json')
if not os.path.exists(rp):
    test(S, 'Qori_Walk_Registration.json exists', False, 'missing', 'present')
else:
    R, TT = json.load(open(rp)), json.load(open(tp))
    keep = ('canvas', 'pixelsPerUnit', 'footLineRow', 'pivotColumn', 'frameCount', 'layerOrder', 'cycleSeconds', 'groundPxPerCycle')
    for key in keep:
        test(S, 'registration %s unchanged' % key, R.get(key) == TT.get(key), R.get(key), TT.get(key))
    fk = ('frame', 'body', 'frontHand', 'neck', 'headAngle', 'earUpperAngle', 'earLowerAngle', 'weaponGrip', 'weaponAngle', 'durationMs')
    same = len(R.get('frames', [])) == len(TT['frames']) and all(all(a.get(x) == b.get(x) for x in fk) for a, b in zip(R['frames'], TT['frames']))
    test(S, 'registration frames unchanged (head, ears, weapon grip, timing)', same, 'same' if same else 'changed', 'same as template')

# ---------------------------------------------------------------- proofs
font = None
def composite(k):
    c = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    c.alpha_composite(Image.fromarray(painted[k]))
    c.alpha_composite(Image.open(os.path.join(INPUT, 'layers', name(k + 1) + '_StaffLayer.png')).convert('RGBA'))
    c.alpha_composite(Image.fromarray(fronts[k]))
    c.alpha_composite(Image.open(os.path.join(INPUT, 'layers', name(k + 1) + '_HeadLayer.png')).convert('RGBA'))
    return c
comps = [composite(k) for k in range(N)]
bg = (150, 170, 175, 255)
sc = 4; w, h = W // sc, H // sc
sheet = Image.new('RGBA', (8 * w, 2 * h), bg); over = Image.new('RGBA', (8 * w, 2 * h), (235, 235, 235, 255))
for k in range(N):
    sheet.alpha_composite(comps[k].resize((w, h), Image.LANCZOS), ((k % 8) * w, (k // 8) * h))
    o = np.full((H, W, 4), 255, np.uint8); o[:, :, :3] = 235
    o[P[k] & Gm[k]] = (170, 170, 170, 255); o[P[k] & ~Gm[k]] = (220, 40, 40, 255); o[~P[k] & Gm[k]] = (40, 90, 230, 255)
    o[G['footLineRow'], :] = (0, 0, 0, 255)
    over.alpha_composite(Image.fromarray(o).resize((w, h), Image.NEAREST), ((k % 8) * w, (k // 8) * h))
for im in (sheet, over):
    dr = ImageDraw.Draw(im)
    for k in range(N): dr.text(((k % 8) * w + 6, (k // 8) * h + 6), '%02d' % (k + 1), fill=(0, 0, 0, 255))
sheet.convert('RGB').save(os.path.join(DELIVERY, 'Qori_Walk_Proof_Sheet.png'))
over.convert('RGB').save(os.path.join(DELIVERY, 'Qori_Walk_Proof_GuideOverlay.png'))
gif = [Image.alpha_composite(Image.new('RGBA', (W, H), bg), c).resize((W // 2, H // 2), Image.LANCZOS).convert('P', palette=Image.ADAPTIVE) for c in comps]
reg = json.load(open(tp))
gif[0].save(os.path.join(DELIVERY, 'Qori_Walk_Proof_Loop.gif'), save_all=True, append_images=gif[1:], duration=[f['durationMs'] for f in reg['frames']], loop=0)
gif[0].save(os.path.join(DELIVERY, 'Qori_Walk_Proof_Loop_Slow.gif'), save_all=True, append_images=gif[1:], duration=[round(f['durationMs'] / .6) for f in reg['frames']], loop=0)
strip = Image.new('RGBA', (W, 260), bg)
for k in range(N):
    part = Image.fromarray(painted[k]).crop((0, G['footLineRow'] - 220, W, G['footLineRow'] + 40))
    a = np.array(part); a[:, :, 3] = (a[:, :, 3] * .22).astype(np.uint8); strip.alpha_composite(Image.fromarray(a))
ImageDraw.Draw(strip).line([(0, 220), (W, 220)], fill=(200, 0, 0, 255))
strip.convert('RGB').save(os.path.join(DELIVERY, 'Qori_Walk_Proof_FeetOnion.png'))

fails = [r for r in results if not r['ok']]
json.dump(dict(passed=len(results) - len(fails), failed=len(fails), results=results), open(os.path.join(DELIVERY, 'Qori_Walk_CHECK.json'), 'w'), indent=1)
print('%d tests, %d failed' % (len(results), len(fails)))
print('ALL PASS' if not fails else '%d FAIL' % len(fails))
sys.exit(0 if not fails else 1)
