"""Builds Codex_v2_full/Batch22d_Input for the Batch 22d Qori arm repaint.

Everything is measured from the accepted-for-rig Batch 22c delivery; nothing is typed in by hand except the
design rules written as constants below (tube width, tolerances), each with the measurement it comes from.

    python Tools/ArtRequests/Batch22d/make_batch22d_input.py

Writes (all in Batch22d_Input):
  Qori_v3_Arm_Template.png           154x259 colour guide at the final canvas: paint inside it
  Qori_v3_Arm_Mask_Outer.png         white = arm pixels allowed (alpha may be > 0 only here)
  Qori_v3_Arm_Mask_Inner.png         white = arm pixels required fully opaque (alpha >= 250)
  Qori_v3_Arm_Mask_HandCover.png     white = where the arm's bottom runs on over the hands (rows 243-256)
  Qori_v3_Arm_Template_4x_Annotated.png  4x preview with labels, the 22c arm ghosted and the mantle line
  batch22d_targets.json              every target number and threshold the checker uses
  check_arm_22d.py                   the acceptance checker (copied from this folder)
  reference_22c/                     the 22c Arm, Leg, hands, Mantle and registration, read-only references
"""
import hashlib, json, math, shutil
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ART = Path.home() / 'OneDrive/Documents/ChatGPT/Qolossal/Art/Codex_v2_full'
SRC = ART / 'Batch22c'
OUT = ART / 'Batch22d_Input'

reg = json.loads((SRC / 'Qori_v3_Registration.json').read_text())
parts = {p['name']: p for p in reg['parts']}
arm_entry = parts['NearArm']
W, H = arm_entry['canvas']
PIV = np.array(arm_entry['pivotTopLeftPx'], float)
chain = arm_entry['boneChain']
SHOULDER, ELBOW, WRIST = [np.array(p, float) for p in chain['topLeftPx']]
X0 = SHOULDER[0]  # the bone line: shoulder, elbow and wrist share this x

def rgba(name):
    return np.array(Image.open(SRC / name).convert('RGBA')).astype(float)

arm = rgba('Qori_v3_Arm.png')
leg = rgba('Qori_v3_Leg.png')

def olive(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    return (a[..., 3] >= 128) & (g >= r * 0.86) & (g >= b * 1.35)

def lum(a):
    return 0.299 * a[..., 0] + 0.587 * a[..., 1] + 0.114 * a[..., 2]

def shift_and(m, k):
    out = m.copy()
    for dy in range(-k, k + 1):
        for dx in range(-k, k + 1):
            out &= np.roll(np.roll(m, dy, 0), dx, 1)
    return out

def shift_or(m, k):
    out = m.copy()
    for dy in range(-k, k + 1):
        for dx in range(-k, k + 1):
            out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out

def row_extent(mask, y):
    xs = np.nonzero(mask[y])[0]
    return (int(xs.min()), int(xs.max())) if len(xs) else None

# ---------------------------------------------------------------- measurements of the 22c arm
alpha = arm[..., 3]
opaque = alpha >= 128
ol = olive(arm)
olive_rows = np.nonzero(ol.sum(1) >= 4)[0]
WRAP_TOP, WRAP_BOTTOM = int(olive_rows.min()), int(olive_rows.max())
ys, xs = np.nonzero(opaque)
ARM_TOP, ARM_BOTTOM = int(ys.min()), int(ys.max())

def seam_rows(a, y0, y1):
    al = a[..., 3]; L = lum(a); out = []
    for y in range(max(y0, 1), y1 + 1):
        both = (al[y] >= 250) & (al[y - 1] >= 250)
        xx = np.nonzero(both)[0]
        if len(xx) < 10:
            continue
        l, r = xx.min(), xx.max(); m = (r - l) * 0.15
        sel = xx[(xx >= l + m) & (xx <= r - m)]
        d = L[y, sel] - L[y - 1, sel]
        out.append({'row': y, 'signed': float(d.mean()), 'meanAbs': float(np.abs(d).mean()),
                    'coherent': float(max((d > 10).mean(), (d < -10).mean()))})
    return out

arm_seam = seam_rows(arm, 30, WRAP_TOP - 1)
worst = max(arm_seam, key=lambda r: r['coherent'])
leg_olive = np.nonzero(olive(leg).sum(1) >= 4)[0]
leg_seam = seam_rows(leg, 30, int(leg_olive.min()) - 1)

widths = {}
for y in range(ARM_TOP, ARM_BOTTOM + 1, 1):
    e = row_extent(opaque, y)
    if e:
        widths[y] = e

# Knob: 22c pixels outside the new tube (computed below) in the shoulder rows.

# ---------------------------------------------------------------- the new silhouette
# Tube width: 48 px = the median opaque width of the 22c arm between the cap and the wrap (rows 60-176),
# which the review passed as the right thickness. Centred on the bone line, straight.
mid_widths = [widths[y][1] - widths[y][0] + 1 for y in range(60, 177)]
TUBE_W = int(round(float(np.median(mid_widths)) / 2) * 2)
HALF = TUBE_W / 2
TOL = 3  # px either side of every template edge
CAP_TOP = ARM_TOP  # keep the 22c top row so the shoulder still has paint above the pivot when the arm swings
CAP_RY = SHOULDER[1] - CAP_TOP

yy, xx = np.mgrid[0:H, 0:W]
cx = xx + 0.5 - X0
cyy = yy + 0.5
tube = np.zeros((H, W), bool)
# rounded top: half-ellipse, half-width HALF, from CAP_TOP down to the shoulder joint
cap = (cyy >= CAP_TOP) & (cyy <= SHOULDER[1]) & ((cx / HALF) ** 2 + ((cyy - SHOULDER[1]) / CAP_RY) ** 2 <= 1)
straight = (cyy > SHOULDER[1]) & (cyy < WRAP_TOP) & (np.abs(cx) <= HALF)
tube = cap | straight

# Wrap and the bark flare under it keep the 22c silhouette (the hands were cut to fit it).
keep = opaque & (yy >= WRAP_TOP)

# Hand cover: the hands start at arm row 243.15 (hand top row 64, pivot y 30, wrist y 209.15). The arm runs on
# over them inside all three hands, so their straight top edge is under opaque arm, then fades out.
hands = {}
for key in ('Hand_Relaxed', 'Hand_Grip', 'Hand_Spread'):
    ex = reg['sourceExtraction'][key]
    pv = np.array(ex['pivotTopLeftPx'], float)
    h = np.array(Image.open(SRC / ('Qori_v3_%s.png' % key)).convert('RGBA'))[..., 3]
    m = np.zeros((H, W), bool)
    hy, hx = np.nonzero(h >= 250)
    ay = np.round(hy - pv[1] + WRIST[1]).astype(int); ax = np.round(hx - pv[0] + WRIST[0]).astype(int)
    ok = (ay >= 0) & (ay < H) & (ax >= 0) & (ax < W)
    m[ay[ok], ax[ok]] = True
    top = int(hy.min()); hands[key] = {'mask': m, 'topRowArm': float(top - pv[1] + WRIST[1]), 'pivot': pv.tolist()}
HAND_TOP = min(v['topRowArm'] for v in hands.values())  # 243.15
all_hands = hands['Hand_Relaxed']['mask'] & hands['Hand_Grip']['mask'] & hands['Hand_Spread']['mask']
COVER_FIRST = int(math.floor(HAND_TOP))       # 243
COVER_LAST = COVER_FIRST + 13                 # 256: 4 opaque rows, then a 10-row fade
# Rows 243-245: the 22c flare carried straight on for 3 more rows at its row-242 width, fully opaque, so every
# hand's straight top edge (and its soft bilinear fringe) is under solid arm. Rows 246-256: fade out, only where
# all three hands are (shrunk 2 px at the sides), so the fade never hangs over background on any hand.
e = row_extent(opaque, ARM_BOTTOM)
OPAQUE_LAST = COVER_FIRST + 2
solid = (yy >= COVER_FIRST) & (yy <= OPAQUE_LAST) & (xx >= e[0]) & (xx <= e[1])
fade = all_hands & np.roll(all_hands, 1, 1) & np.roll(all_hands, -1, 1) & np.roll(all_hands, 2, 1) & np.roll(all_hands, -2, 1)
fade &= (yy > OPAQUE_LAST) & (yy <= COVER_LAST) & (xx >= e[0]) & (xx <= e[1])
cover = solid | fade
cover_inner = solid & (xx >= e[0] + TOL) & (xx <= e[1] - TOL)

outer = shift_or(tube, TOL) | shift_or(keep, TOL) | cover
outer &= ~((yy > COVER_LAST))
inner = shift_and(tube, TOL) | (shift_and(keep, TOL) & (yy <= ARM_BOTTOM - TOL)) | cover_inner
# cap: the inner mask must not demand pixels the outer cap tolerance can't hold
inner &= outer

# Mantle line over the near arm at rest (Mantle drawOrder 11 above NearArm 9), in arm pixels.
def rot(a):
    a = math.radians(a); return np.array([[math.cos(a), -math.sin(a)], [math.sin(a), math.cos(a)]])
def world(name):
    d = parts[name]
    if d['parentBone'] == 'Body':
        return np.array(d['restPosition'], float), d['restRotationDegrees']
    pp, pa = world(d['parentBone'])
    return pp + rot(pa) @ np.array(d['restPosition'], float), pa + d['restRotationDegrees']
def mask_in_arm(arm_name, part_name):
    d = parts[part_name]
    img = np.array(Image.open(SRC / d['path']).convert('RGBA'))[..., 3]
    apos, aang = world(arm_name); ppos, pang = world(part_name)
    local = np.stack([(xx + 0.5 - PIV[0]) / 100, -(yy + 0.5 - PIV[1]) / 100], -1) @ rot(aang).T + apos - ppos
    local = local @ rot(-pang).T
    if part_name == 'Cape': local[..., 0] /= reg['capeRestGather']
    px = np.floor(local[..., 0] * 100 + d['pivotTopLeftPx'][0]).astype(int)
    py = np.floor(-local[..., 1] * 100 + d['pivotTopLeftPx'][1]).astype(int)
    ok = (px >= 0) & (py >= 0) & (px < img.shape[1]) & (py < img.shape[0])
    m = np.zeros((H, W), bool); m[ok] = img[py[ok], px[ok]] >= 128
    return m
mantle_near = mask_in_arm('NearArm', 'Mantle')
mantle_low = {}
for x in range(W):
    col = np.nonzero(mantle_near[:, x])[0]
    mantle_low[x] = int(col.max()) if len(col) else None

knob = opaque & ~outer & (yy < SHOULDER[1] + 30)
kys, kxs = np.nonzero(knob)
knob_vis = knob & ~mantle_near

# ---------------------------------------------------------------- colour targets from the 22c leg
def interior(al, k):
    return shift_and(al >= 255, k)
def bark_stats(a, r0, r1):
    inn = interior(a[..., 3], 3); band = np.zeros(inn.shape, bool); band[r0:r1 + 1] = True
    m = inn & band & ~olive(a); px = a[m][:, :3]; L = lum(px)
    q = {}
    for p in (5, 25, 50, 75, 95):
        sel = np.abs(L - np.percentile(L, p)) <= 3
        q['p%d' % p] = [int(round(v)) for v in np.median(px[sel], 0)]
    edge = (a[..., 3] >= 200) & ~interior(a[..., 3], 2) & band
    prof = np.zeros(10); cnt = np.zeros(10)
    for y in range(r0, r1 + 1):
        xs_ = np.nonzero(a[y, :, 3] >= 128)[0]
        if len(xs_) < 10: continue
        l, r = xs_.min(), xs_.max()
        for x in range(l, r + 1):
            if a[y, x, 3] < 255 or olive(a[y:y + 1, x:x + 1])[0, 0]: continue
            b = min(9, int((x - l) / (r - l + 1) * 10)); prof[b] += lum(a[y, x]); cnt[b] += 1
    return {'quantilesRGB': q, 'outlineMedianRGB': [int(round(v)) for v in np.median(a[edge][:, :3], 0)],
            'crossProfileLum': [round(float(v), 1) for v in prof / np.maximum(cnt, 1)],
            'grainFraction': round(float((L < np.median(L) - 35).mean()), 3), 'pixels': int(len(px))}
leg_bark_rows = (40, int(leg_olive.min()) - 4)
leg_stats = bark_stats(leg, *leg_bark_rows)
arm_stats = bark_stats(arm, int(SHOULDER[1]), WRAP_TOP - 9)
wrap_px = arm[ol][:, :3]
wrap_median = [int(round(v)) for v in np.median(wrap_px, 0)]

# ---------------------------------------------------------------- hashes of everything that must not change
keep_files = sorted(p.name for p in SRC.glob('Qori_v3_*.png')
                    if p.name != 'Qori_v3_Arm.png' and not p.name.startswith('Qori_v3_Proof'))
hashes = {n: hashlib.sha256((SRC / n).read_bytes()).hexdigest() for n in keep_files}
arm22c_sha = hashlib.sha256((SRC / 'Qori_v3_Arm.png').read_bytes()).hexdigest()
canon = Path('C:/UnityProjects/Qolossal/Tools/ArtImport/codex_v2_accepted.json')
canon_sha = hashlib.sha256(canon.read_bytes()).hexdigest()
canon_count = len(json.loads(canon.read_text())['assets'])

targets = {
    'about': 'Batch 22d Qori_v3_Arm targets. Measured from Batch22c by make_batch22d_input.py on 2026-10-08. '
             'Pixel coordinates are top-left PNG pixels of the 154x259 arm canvas (x right, y down).',
    'canvas': [W, H], 'pivotTopLeftPx': PIV.tolist(), 'pixelsPerUnit': reg['pixelsPerUnit'],
    'joints': {'shoulder': SHOULDER.tolist(), 'elbow': ELBOW.tolist(), 'wrist': WRIST.tolist()},
    'boneLineX': X0,
    'bendBlendWidthPx': chain['bendBlendWidthPx'],
    'tube': {'widthPx': TUBE_W, 'leftEdgeX': X0 - HALF, 'rightEdgeX': X0 + HALF, 'capTopRow': CAP_TOP,
             'capShape': 'half-ellipse, half-width %.0f px, from row %d down to the shoulder row %.2f' % (HALF, CAP_TOP, SHOULDER[1]),
             'straightRows': [int(math.ceil(SHOULDER[1])), WRAP_TOP - 1],
             'basis': 'median opaque width of the 22c arm over rows 60-176 = %.1f px' % float(np.median(mid_widths))},
    'toleranceEachSidePx': TOL,
    'centreLine': {'rows': [30, WRAP_TOP - 1], 'maxOffsetPx': 3.0},
    'wrap': {'rows22c': [WRAP_TOP, WRAP_BOTTOM], 'allowedRows': [WRAP_TOP - 4, WRAP_BOTTOM + 4],
             'centreRow22c': (WRAP_TOP + WRAP_BOTTOM) / 2, 'centreToleranceRows': 4, 'oliveMedianRGB22c': wrap_median,
             'oliveMedianToleranceRGB': 12},
    'flare': {'rows': [WRAP_BOTTOM + 1, ARM_BOTTOM], 'note': 'bark below the wrap, the back of the wrist; 22c silhouette kept'},
    'handCover': {'firstRow': COVER_FIRST, 'opaqueThroughRow': OPAQUE_LAST, 'lastRow': COVER_LAST, 'opaqueColumns': [int(e[0]), int(e[1])],
                  'handTopRowInArm': HAND_TOP, 'note': 'rows firstRow-opaqueThroughRow: the row-242 flare continued at full width, opaque; then a fade to 0 by lastRow, only where all three hands overlap (shrunk 2 px at the sides)'},
    'seam': {'rows': [30, 'wrapTop-1'], 'maxAbsSignedStep': 6.0, 'maxMeanAbsStep': 10.0, 'maxCoherent': 0.25,
             'barkAboveWrap': {'rowsNear': 8, 'rowsFar': 32, 'maxMedianLumDiff': 8.0},
             'barkAroundWrap': {'maxLighterLum': 4.0, 'measured22cArm': '+8.6 (pasted 22b block, lighter)', 'measured22cLeg': '-12.3 (wrap shadow)'},
             'measured22cArm': {'worst': worst, 'seamRow': 172},
             'measured22cLeg': {'maxAbsSigned': max(abs(r['signed']) for r in leg_seam), 'maxMeanAbs': max(r['meanAbs'] for r in leg_seam),
                                 'maxCoherent': max(r['coherent'] for r in leg_seam)}},
    'colour': {'legBark': leg_stats, 'legRows': list(leg_bark_rows), 'arm22cBark': arm_stats,
               'tolerance': {'p50': 8, 'p5': 12, 'p95': 10, 'outline': 12, 'profileBin': 15, 'grainFraction': [0.07, 0.16]}},
    'knob22c': {'pixelsOutsideTemplate': int(knob.sum()), 'visibleBesideMantleAtRest': int(knob_vis.sum()),
                'bbox': [int(kxs.min()), int(kys.min()), int(kxs.max()), int(kys.max())] if len(kxs) else None},
    'mantleNearArmLowestRowByColumn': {str(x): mantle_low[x] for x in range(int(X0 - HALF) - 4, int(X0 + HALF) + 5)},
    'registration': {
        'required': {'NearHand.drawOrder': 8.9, 'FarHand.drawOrder': 1.9},
        'why': 'hands draw just under their arm, so the arm runs on over the straight top edge of every hand',
        'allowedTopLevelChanges': ['status', 'supersedes', 'sourceExtraction.Arm (canvas and pivot unchanged)',
                                   'barkColourNormalization.Arm']},
    'unchangedFiles22cSha256': hashes,
    'arm22cSha256': arm22c_sha,
    'canonicalManifest': {'path': str(canon), 'sha256': canon_sha, 'entries': canon_count},
    'elbowTests': {'anglesDeg': [90, 45, -20], 'maxInteriorHoles': 0},
    'handJoin': {'gapAlphaMin': 0.95, 'maxCoherent': 0.25, 'maxAbsSignedStep': 6.0},
}

# ---------------------------------------------------------------- write
OUT.mkdir(exist_ok=True)
ref = OUT / 'reference_22c'; ref.mkdir(exist_ok=True)
for n in ['Qori_v3_Arm.png', 'Qori_v3_Leg.png', 'Qori_v3_Hand_Relaxed.png', 'Qori_v3_Hand_Grip.png',
          'Qori_v3_Hand_Spread.png', 'Qori_v3_Mantle.png', 'Qori_v3_Registration.json']:
    shutil.copyfile(SRC / n, ref / n)
shutil.copyfile(HERE / 'check_arm_22d.py', OUT / 'check_arm_22d.py')

def save_mask(m, name):
    Image.fromarray((m * 255).astype(np.uint8), 'L').save(OUT / name)
save_mask(outer, 'Qori_v3_Arm_Mask_Outer.png')
save_mask(inner, 'Qori_v3_Arm_Mask_Inner.png')
save_mask(cover, 'Qori_v3_Arm_Mask_HandCover.png')

# Colour template at the final canvas: grey = must be opaque, yellow = optional edge band,
# blue = hand cover, red column = bone line, magenta crosses = joints, cyan rows = wrap band.
t = np.zeros((H, W, 4), np.uint8)
t[outer] = (240, 200, 60, 140)
t[inner] = (150, 150, 150, 255)
t[cover] = (90, 140, 230, 255)
t[:, int(X0)] = np.where(t[:, int(X0), 3:4] > 0, np.array([220, 40, 40, 255], np.uint8), t[:, int(X0)])
for r in (WRAP_TOP, WRAP_BOTTOM):
    t[r, :, :] = np.where(outer[r][:, None], np.array([40, 220, 220, 255], np.uint8), t[r])
for j in (SHOULDER, ELBOW, WRIST):
    jx, jy = int(j[0]), int(round(j[1]))
    for k in range(-4, 5):
        t[jy, jx + k] = (230, 40, 230, 255); t[jy + k, jx] = (230, 40, 230, 255)
Image.fromarray(t, 'RGBA').save(OUT / 'Qori_v3_Arm_Template.png')

# 4x annotated preview
S = 4; pad = 260
big = Image.new('RGBA', (W * S + pad, H * S), (255, 255, 255, 255))
ghost = Image.fromarray(arm.astype(np.uint8), 'RGBA').resize((W * S, H * S), Image.NEAREST)
g = np.array(ghost).astype(float); g[..., 3] *= 0.45
big.alpha_composite(Image.fromarray(g.astype(np.uint8), 'RGBA'))
tb = np.array(Image.fromarray(t, 'RGBA').resize((W * S, H * S), Image.NEAREST)).astype(float)
tb[..., 3] *= 0.55
big.alpha_composite(Image.fromarray(tb.astype(np.uint8), 'RGBA'))
# Mantle coverage over the near arm at rest, hatched green (Mantle draws above NearArm)
hatch = ((np.add.outer(np.arange(H * S), np.arange(W * S)) // 3) % 4 == 0)
mm = np.kron(mantle_near.astype(np.uint8), np.ones((S, S), np.uint8)).astype(bool)
ov = np.zeros((H * S, W * S, 4), np.uint8)
ov[mm] = (30, 120, 30, 70); ov[mm & hatch] = (20, 100, 20, 200)
big.alpha_composite(Image.fromarray(ov, 'RGBA'), (0, 0))
d = ImageDraw.Draw(big)
try:
    font = ImageFont.truetype('arial.ttf', 15)
except OSError:
    font = ImageFont.load_default()
labels = [
    (CAP_TOP, 'row %d  cap top (keep)' % CAP_TOP),
    (SHOULDER[1], 'row %.2f  SHOULDER pivot x %.2f' % (SHOULDER[1], SHOULDER[0])),
    (ELBOW[1], 'row %.2f  ELBOW' % ELBOW[1]),
    (172, 'row 172  22c seam (fix)'),
    (WRAP_TOP, 'row %d  wrap top' % WRAP_TOP),
    (WRIST[1], 'row %.2f  WRIST' % WRIST[1]),
    (WRAP_BOTTOM, 'row %d  wrap bottom' % WRAP_BOTTOM),
    (HAND_TOP, 'row %.2f  hands start' % HAND_TOP),
    (COVER_LAST, 'row %d  arm ends (fade)' % COVER_LAST),
]
for row, text in labels:
    y = int(row * S)
    d.line([(0, y), (W * S, y)], fill=(0, 0, 0, 90))
    d.text((W * S + 6, y - 8), text, fill=(0, 0, 0, 255), font=font)
d.text((6, 6), 'grey: must be opaque   yellow: optional 3 px edge band   blue: hand cover', fill=(0, 0, 0, 255), font=font)
d.text((6, 26), 'red: bone line x %.2f   magenta: joints   green hatch: mantle over the near arm at rest   faint: 22c arm' % X0, fill=(0, 0, 0, 255), font=font)
big.convert('RGB').save(OUT / 'Qori_v3_Arm_Template_4x_Annotated.png')

(OUT / 'batch22d_targets.json').write_text(json.dumps(targets, indent=1))
print('wrote', OUT)
print('tube width', TUBE_W, 'edges x %.2f..%.2f' % (X0 - HALF, X0 + HALF), 'cap top', CAP_TOP)
print('wrap rows', WRAP_TOP, WRAP_BOTTOM, 'arm bottom', ARM_BOTTOM, 'hand top', HAND_TOP)
print('knob 22c', targets['knob22c'])
print('outer px', int(outer.sum()), 'inner px', int(inner.sum()), 'cover px', int(cover.sum()))
print('cover rows/cols', row_extent(cover, COVER_FIRST), row_extent(cover, COVER_LAST))
print('leg colour', leg_stats)
print('arm colour', arm_stats)
print('wrap median', wrap_median)
print('seam worst', worst)
