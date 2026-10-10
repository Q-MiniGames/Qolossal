"""Builds Codex_v2_full/Batch22e_Input for the Batch 22e Qori wrist repaint (arm rows 180-256: leaf wrap, flare, hand cover).

Everything is measured from the Batch 22d delivery (CLAUDE_REVIEW_37: arm passes down to the wrap, the flare and
hand cover are smeared) and the 22d input masks. Run it with no arguments; it has no options.

    python Tools/ArtRequests/Batch22e/make_batch22e_input.py

Writes (all in Batch22e_Input):
  Qori_v3_Arm_Template.png            154x259 colour guide: locked rows ghosted, paint zone in colour
  Qori_v3_Arm_Mask_Outer.png          white = arm pixels allowed (alpha may be > 0 only here)
  Qori_v3_Arm_Mask_Inner.png          white = arm pixels required fully opaque (alpha >= 250)
  Qori_v3_Arm_Mask_HandCover.png      white = rows 243-256 inside all three hands (the cover may exist only here)
  Qori_v3_Arm_Template_4x_Annotated.png  4x view of rows 170-259 with labels, the three hand outlines and the 22d arm
  batch22e_targets.json               every target and threshold the checker uses
  check_arm_22e.py                    the acceptance checker (copied from this folder)
  reference_22d/                      the 22d Arm, three hands, registration, and the 22d proof of the fault
"""
import hashlib, json, math, shutil
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ART = Path.home() / 'OneDrive/Documents/ChatGPT/Qolossal/Art/Codex_v2_full'
SRC = ART / 'Batch22d'
SRC_IN = ART / 'Batch22d_Input'
OUT = ART / 'Batch22e_Input'

T22 = json.loads((SRC_IN / 'batch22d_targets.json').read_text())
reg = json.loads((SRC / 'Qori_v3_Registration.json').read_text())
W, H = T22['canvas']
WRIST = np.array(T22['joints']['wrist'], float)
yy, xx = np.mgrid[0:H, 0:W]

def rgba(p):
    return np.array(Image.open(p).convert('RGBA')).astype(float)
def lum(a):
    return 0.299 * a[..., 0] + 0.587 * a[..., 1] + 0.114 * a[..., 2]
def olive(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    return (a[..., 3] >= 128) & (g >= r * 0.86) & (g >= b * 1.35)
def shift_and(m, k):
    out = m.copy()
    for dy in range(-k, k + 1):
        for dx in range(-k, k + 1):
            out &= np.roll(np.roll(m, dy, 0), dx, 1)
    return out
def hshrink(m, k):
    out = m.copy()
    for dx in range(-k, k + 1):
        out &= np.roll(m, dx, 1)
    return out
def row_extent(m, y):
    xs = np.nonzero(m[y])[0]
    return (int(xs.min()), int(xs.max())) if len(xs) else None

arm = rgba(SRC / 'Qori_v3_Arm.png')
al = arm[..., 3]
outer22 = np.array(Image.open(SRC_IN / 'Qori_v3_Arm_Mask_Outer.png')) > 127
inner22 = np.array(Image.open(SRC_IN / 'Qori_v3_Arm_Mask_Inner.png')) > 127

# Rows 0-179 (rounded top, upper arm, elbow, forearm) stay pixel-identical to 22d. The wrap is repainted with the
# wrist: the 22d bark beside its lower leaves (rows ~216-235) carries pale cross-grain streaks (Review 37), and a lock
# line inside the wrap would leave a seam in the bark beside the leaves. Row 179 is plain bark, covered by the seam tests.
LOCK_LAST = 179
PAINT_FIRST = LOCK_LAST + 1
FLARE_FIRST = 236
FLARE_LAST = 242
COVER_FIRST, OPAQUE_LAST, COVER_LAST = 243, 245, 256
# One stray bright-green pixel at the lower left tip of the 22d wrap (Review 37), shown in the 4x view.
STRAY = (58, 231)

# ---------------------------------------------------------------- hands in arm pixels (alpha >= 128)
hands = {}
for key in ('Hand_Relaxed', 'Hand_Grip', 'Hand_Spread'):
    pv = np.array(reg['sourceExtraction'][key]['pivotTopLeftPx'], float)
    h = np.array(Image.open(SRC / ('Qori_v3_%s.png' % key)).convert('RGBA'))[..., 3]
    m = np.zeros((H, W), bool)
    hy, hx = np.nonzero(h >= 128)
    ay = np.round(hy - pv[1] + WRIST[1]).astype(int); ax = np.round(hx - pv[0] + WRIST[0]).astype(int)
    ok = (ay >= 0) & (ay < H) & (ax >= 0) & (ax < W)
    m[ay[ok], ax[ok]] = True
    hands[key] = m
all_hands = hands['Hand_Relaxed'] & hands['Hand_Grip'] & hands['Hand_Spread']
in_cover_rows = (yy >= COVER_FIRST) & (yy <= COVER_LAST)
cover = all_hands & in_cover_rows                       # alpha > 0 allowed only here below row 242
cover_in = hshrink(all_hands, 2) & (yy >= COVER_FIRST) & (yy <= OPAQUE_LAST)   # must be opaque

# 22d overhang: pixels of its opaque rows 243-245 outside each hand
overhang22 = {k: int(((al > 32) & ~m & in_cover_rows).sum()) for k, m in hands.items()}

# Flare corners: at the hands' top row the narrowest hand spans these columns, so the flare may round off its
# bottom corners down to them (the 22d flare runs x 42-118 at row 242 and stands 4-9 px past the Grip and Spread).
top_ext = row_extent(all_hands, COVER_FIRST)
FLARE_ROUND_ROWS = (239, FLARE_LAST)
flare_inner = inner22 & (yy >= FLARE_FIRST) & (yy <= FLARE_LAST)
corner = (yy >= FLARE_ROUND_ROWS[0]) & (yy <= FLARE_ROUND_ROWS[1]) & ((xx < top_ext[0] + 2) | (xx > top_ext[1] - 2))
flare_inner &= ~corner

outer = (outer22 & (yy <= FLARE_LAST)) | cover
inner = (inner22 & (yy < FLARE_FIRST)) | flare_inner | cover_in
inner &= outer

# ---------------------------------------------------------------- measurements that set the new thresholds
L = lum(arm); ol = olive(arm)
def detail(a, y0, y1, amin=250):
    Lx = lum(a); ok = (a[..., 3] >= amin) & ~olive(a)
    ok2 = ok[:, 1:] & ok[:, :-1]; ok2 &= np.roll(ok2, 2, 1) & np.roll(ok2, -2, 1)
    band = np.zeros(ok2.shape, bool); band[y0:y1 + 1] = True
    d = np.abs(np.diff(Lx, axis=1))[ok2 & band]
    return float(d.mean()) if len(d) else 0.0, int(len(d))
bark_detail = detail(arm, 140, 175)[0]
hand_stats = {}
for key in hands:
    hh = rgba(SRC / ('Qori_v3_%s.png' % key))
    top = int(np.nonzero((hh[..., 3] >= 250).any(1))[0][0])
    d = detail(hh, top, top + 13)[0]
    ok = (hh[top:top + 14, :, 3] >= 250)
    hand_stats[key] = {'topRow': top, 'detailTop14Rows': round(d, 2), 'medianLumTop14Rows': round(float(np.median(lum(hh)[top:top + 14][ok])), 1)}
hand_detail_min = min(v['detailTop14Rows'] for v in hand_stats.values())
def med_lum(a, y0, y1):
    ok = (a[..., 3] >= 250) & ~olive(a); band = np.zeros(ok.shape, bool); band[y0:y1 + 1] = True
    return round(float(np.median(lum(a)[ok & band])), 1)
cols_in = hshrink(all_hands, 2)
def alpha_rows(a):
    return {y: round(float(a[y, cols_in[y], 3].mean()), 1) if cols_in[y].any() else None for y in range(OPAQUE_LAST, COVER_LAST + 1)}
measured22d = {
    'flareDetail236_242': round(detail(arm, FLARE_FIRST, FLARE_LAST)[0], 2),
    'coverDetail243_245': round(detail(arm, COVER_FIRST, OPAQUE_LAST)[0], 2),
    'fadeDetail246_252_alpha128': round(detail(arm, OPAQUE_LAST + 1, 252, 128)[0], 2),
    'barkDetail140_175': round(bark_detail, 2),
    'medianLum236_245': med_lum(arm, FLARE_FIRST, OPAQUE_LAST),
    'barkMedianLum140_175': med_lum(arm, 140, 175),
    'coverAlphaByRow': alpha_rows(arm),
    'overhangPxPerHand': overhang22,
    'strayGreenPixel': {'xy': list(STRAY), 'rgba': [int(v) for v in arm[STRAY[1], STRAY[0]]]},
}
def grain_ratio(a, y0, y1):
    # lengthwise grain: luminance changes much more across the limb (horizontal neighbours) than along it
    Lx = lum(a); ol_ = olive(a); g = ol_.copy()
    for k in (-2, -1, 1, 2):
        g |= np.roll(ol_, k, 1) | np.roll(ol_, k, 0)
    ok = a[..., 3] >= 255
    for k in (1, 2):
        ok &= np.roll(ok, k, 1) & np.roll(ok, -k, 1) & np.roll(ok, k, 0) & np.roll(ok, -k, 0)
    m = ok & ~g; band = np.zeros(m.shape, bool); band[y0:y1 + 1] = True; m &= band
    dh = np.abs(Lx - np.roll(Lx, 1, 1))[m & np.roll(m, 1, 1)]; dv = np.abs(Lx - np.roll(Lx, 1, 0))[m & np.roll(m, 1, 0)]
    return round(float(dh.mean() / dv.mean()), 2) if len(dh) and len(dv) and dv.mean() > 0 else None
leg22c = rgba(SRC_IN / 'reference_22c' / 'Qori_v3_Leg.png')
grain = {'armBark60_175': grain_ratio(arm, 60, 175), 'arm22dBesideWrap216_235': grain_ratio(arm, 216, 235),
         'arm22dFlare236_242': grain_ratio(arm, FLARE_FIRST, FLARE_LAST), 'leg22cBark60_300': grain_ratio(leg22c, 60, 300),
         'leg22cBesideWrap300_340': grain_ratio(leg22c, 300, 340)}
measured22d['grainRatio'] = grain
DETAIL_MIN = round(0.6 * bark_detail, 1)   # 12.4 -> 7.4; hands' own top rows are 9.6-11.3
FADE_DETAIL_MIN = round(0.5 * bark_detail, 1)
lum_lo = min(measured22d['barkMedianLum140_175'], min(v['medianLumTop14Rows'] for v in hand_stats.values())) - 8
lum_hi = max(measured22d['barkMedianLum140_175'], max(v['medianLumTop14Rows'] for v in hand_stats.values())) + 8

targets = dict(T22)
targets['about'] = ('Batch 22e Qori_v3_Arm targets: repaint rows %d-%d only. Measured from Batch22d by make_batch22e_input.py on 2026-10-08. '
                    'Pixel coordinates are top-left PNG pixels of the 154x259 arm canvas (x right, y down).' % (PAINT_FIRST, COVER_LAST))
targets['arm22dSha256'] = hashlib.sha256((SRC / 'Qori_v3_Arm.png').read_bytes()).hexdigest()
targets['unchangedFilesSha256'] = {n: h for n, h in T22['unchangedFiles22cSha256'].items()}
for n in targets['unchangedFilesSha256']:
    assert hashlib.sha256((SRC / n).read_bytes()).hexdigest() == targets['unchangedFilesSha256'][n], n
targets.pop('unchangedFiles22cSha256', None)
targets['registration'] = {'required': {}, 'note': 'identical to 22d (NearHand 8.9, FarHand 1.9 stay); status, supersedes and the Arm notes may change'}
targets['handCover'] = dict(T22['handCover'], opaqueColumnsByRow={str(y): row_extent(cover_in, y) for y in range(COVER_FIRST, OPAQUE_LAST + 1)},
                            note='rows 243-245 opaque inside all three hands (2 px in from their edges); rows 246-256 fade to 0, only inside all three hands')
targets['wrist22e'] = {
    'lockedRows': [0, LOCK_LAST],
    'paintRows': [PAINT_FIRST, COVER_LAST],
    'flareRows': [FLARE_FIRST, FLARE_LAST], 'flareCornerRoundRows': list(FLARE_ROUND_ROWS), 'flareCornerColumnsAtHandTop': list(top_ext),
    'detail': {'measure': 'mean |luminance step| between horizontal neighbours, opaque non-olive pixels 2 px in from any edge',
               'barkRows140_175': round(bark_detail, 2), 'handsTop14Rows': hand_stats,
               'minFlareAndCover236_245': DETAIL_MIN, 'minFade246_252_alpha128': FADE_DETAIL_MIN},
    'medianLum236_245': [round(lum_lo, 1), round(lum_hi, 1)],
    'grainDirection': {'measure': 'mean |horizontal neighbour step| / mean |vertical neighbour step|, bark 2 px from leaves and edges',
                       'minBesideWrap': 1.6, 'minFlare236_242': 1.8, 'measured': grain},
    'belowWrapSeam': {'rows': ['wrapBottom+1', OPAQUE_LAST], 'note': 'same seam limits as above the wrap'},
    'fade': {'rows': [OPAQUE_LAST, COVER_LAST], 'maxMeanAlphaDropPerRow': 30, 'maxMeanAlphaLastRow': 10,
             'measuredOn': 'columns 2 px inside all three hands'},
    'maxOverhangPxPerHand': 0,
    'measured22d': measured22d,
}

# ---------------------------------------------------------------- write
OUT.mkdir(parents=True, exist_ok=True)
def save_mask(m, name):
    Image.fromarray((m * 255).astype(np.uint8), 'L').save(OUT / name)
save_mask(outer, 'Qori_v3_Arm_Mask_Outer.png')
save_mask(inner, 'Qori_v3_Arm_Mask_Inner.png')
save_mask(cover, 'Qori_v3_Arm_Mask_HandCover.png')

# Template: the locked 22d rows ghosted at 35%, the paint zone: grey = must be opaque, yellow = may be filled,
# blue = cover fade zone, red column = bone line, magenta cross = wrist, cyan row = first paint row.
t = np.zeros((H, W, 4), np.uint8)
ghost = arm.copy(); ghost[..., 3] *= 0.35
t[:PAINT_FIRST] = ghost[:PAINT_FIRST].astype(np.uint8)
zone = yy >= PAINT_FIRST
t[zone & outer] = (250, 225, 120, 255)
t[zone & inner] = (150, 150, 150, 255)
t[zone & cover & ~inner] = (90, 140, 230, 255)
X0 = int(round(T22['boneLineX']))
t[PAINT_FIRST:, X0] = (220, 40, 40, 255)
t[PAINT_FIRST, :] = np.where(outer[PAINT_FIRST][:, None], np.array([0, 200, 220, 255], np.uint8), t[PAINT_FIRST])
Image.fromarray(t, 'RGBA').save(OUT / 'Qori_v3_Arm_Template.png')

S = 4; Y0 = 160
crop = Image.fromarray(t[Y0:], 'RGBA').resize((W * S, (H - Y0) * S), Image.NEAREST)
base = Image.new('RGBA', crop.size, (255, 255, 255, 255)); base.alpha_composite(crop)
d = ImageDraw.Draw(base)
try:
    font = ImageFont.truetype('arial.ttf', 13)
except OSError:
    font = ImageFont.load_default()
colours = {'Hand_Relaxed': (0, 150, 0, 255), 'Hand_Grip': (200, 0, 160, 255), 'Hand_Spread': (230, 120, 0, 255)}
for key, m in hands.items():
    edge = m & ~shift_and(m, 1)
    ys, xs = np.nonzero(edge & (yy >= Y0))
    for y, x in zip(ys, xs):
        d.rectangle([x * S, (y - Y0) * S, x * S + S - 1, (y - Y0) * S + S - 1], fill=colours[key])
lv = np.array(Image.fromarray(np.where(olive(arm)[..., None], np.concatenate([arm[..., :3], np.full((H, W, 1), 110.0)], -1), 0).astype(np.uint8), 'RGBA'))
leaves = Image.fromarray(lv[Y0:], 'RGBA').resize((W * S, (H - Y0) * S), Image.NEAREST)
base.alpha_composite(leaves); d = ImageDraw.Draw(base)
for y in (PAINT_FIRST, FLARE_FIRST, FLARE_LAST + 1, OPAQUE_LAST + 1, COVER_LAST + 1):
    d.line([(0, (y - Y0) * S), (W * S, (y - Y0) * S)], fill=(0, 0, 0, 255))
    d.text((2, (y - Y0) * S + 1), 'row %d' % y, fill=(0, 0, 0, 255), font=font)
d.rectangle([STRAY[0] * S - 2, (STRAY[1] - Y0) * S - 2, STRAY[0] * S + S + 1, (STRAY[1] - Y0) * S + S + 1], outline=(255, 0, 0, 255), width=2)
d.text((4, 4), 'ghost: locked 22d rows, and the 22d leaves   grey: opaque   yellow: may fill   blue: fade', fill=(0, 0, 0, 255), font=font)
d.text((4, 20), 'hand outlines: Relaxed green, Grip magenta, Spread orange   red box: stray pixel', fill=(0, 0, 0, 255), font=font)
base.convert('RGB').save(OUT / 'Qori_v3_Arm_Template_4x_Annotated.png')

(OUT / 'batch22e_targets.json').write_text(json.dumps(targets, indent=1))
shutil.copy(HERE / 'check_arm_22e.py', OUT / 'check_arm_22e.py')
ref = OUT / 'reference_22d'; ref.mkdir(exist_ok=True)
for n in ('Qori_v3_Arm.png', 'Qori_v3_Hand_Relaxed.png', 'Qori_v3_Hand_Grip.png', 'Qori_v3_Hand_Spread.png',
          'Qori_v3_Registration.json', 'Qori_v3_Proof_ArmCheck_HandJoin_2x.png'):
    shutil.copy(SRC / n, ref / n)

print('wrote', OUT)
print('bark detail %.2f, hands %s, 22d flare %.2f cover %.2f fade %.2f -> min %.1f / fade %.1f'
      % (bark_detail, {k: v['detailTop14Rows'] for k, v in hand_stats.items()}, measured22d['flareDetail236_242'],
         measured22d['coverDetail243_245'], measured22d['fadeDetail246_252_alpha128'], DETAIL_MIN, FADE_DETAIL_MIN))
print('cover opaque rows', {y: row_extent(cover_in, y) for y in range(COVER_FIRST, OPAQUE_LAST + 1)}, 'hand-top columns', top_ext)
print('22d overhang', overhang22, '22d alpha rows', measured22d['coverAlphaByRow'])
print('lum window', round(lum_lo, 1), round(lum_hi, 1), '22d', measured22d['medianLum236_245'])
print('outer px', int(outer.sum()), 'inner px', int(inner.sum()), 'cover px', int(cover.sum()))
