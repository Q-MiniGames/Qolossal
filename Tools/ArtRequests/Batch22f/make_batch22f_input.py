"""Builds Codex_v2_full/Batch22f_Input for the Batch 22f Qori wrap-edge repaint.

Batch 22e (CLAUDE_REVIEW_38) fixed the wrist; the bark at the outer sides of the leaf wrap is a dark, hard-edged band
that reads as brown flaps in the rig. 22f repaints only that edge band. Measured from the Batch 22e delivery and the
Batch 22e input masks. No options:

    python Tools/ArtRequests/Batch22f/make_batch22f_input.py

Writes (all in Batch22f_Input):
  Qori_v3_Arm_Mask_Outer.png          white = arm pixels allowed (alpha may be > 0 only here; same as 22e)
  Qori_v3_Arm_Mask_Inner.png          white = must be opaque (22e's, without the old leaf-tip area beyond the tube)
  Qori_v3_Arm_Mask_EdgeZone.png       white = the only pixels that may differ from the 22e arm
  Qori_v3_Arm_EdgeZone_4x.png         4x view of rows 170-250: the 22e arm with the edge zone outlined
  batch22f_targets.json               the 22e targets plus the wrap-edge targets
  check_arm_22f.py                    the acceptance checker (copied from this folder)
  reference_22d/, reference_22e/      read-only references (22d for the inherited tests, 22e = the start file)
"""
import hashlib, json, shutil
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ART = Path.home() / 'OneDrive/Documents/ChatGPT/Qolossal/Art/Codex_v2_full'
SRC = ART / 'Batch22e'
SRC_IN = ART / 'Batch22e_Input'
OUT = ART / 'Batch22f_Input'

T = json.loads((SRC_IN / 'batch22e_targets.json').read_text())
W, H = T['canvas']
yy, xx = np.mgrid[0:H, 0:W]

def lum(a):
    return 0.299 * a[..., 0] + 0.587 * a[..., 1] + 0.114 * a[..., 2]
def olive(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    return (a[..., 3] >= 128) & (g >= r * 0.86) & (g >= b * 1.35)
def grow(m, k):
    out = m.copy()
    for dy in range(-k, k + 1):
        for dx in range(-k, k + 1):
            out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out

arm = np.array(Image.open(SRC / 'Qori_v3_Arm.png').convert('RGBA')).astype(float)
al = arm[..., 3]; L = lum(arm); ol = olive(arm)
outer = np.array(Image.open(SRC_IN / 'Qori_v3_Arm_Mask_Outer.png')) > 127
inner22e = np.array(Image.open(SRC_IN / 'Qori_v3_Arm_Mask_Inner.png')) > 127

ROWS = (180, 242)            # wrap and flare rows; above is the locked arm, below the accepted hand cover
DEPTH = 22                   # the edge band reaches this far in from each side of the 22e silhouette (its dark band is up to 20 px)
tube = T['tube']; TL, TR = tube['leftEdgeX'], tube['rightEdgeX']
TOL = T['toleranceEachSidePx']

# Edge zone: per row, the DEPTH pixels in from each 22e edge, plus the transparent pixels out to the outer mask,
# never a leaf pixel (the leaves stay as painted, 1 px of their outline included).
leaf = grow(ol, 1) & (al > 0)    # the leaves and their painted outline; transparent pixels beside them may be filled
zone = np.zeros((H, W), bool)
for y in range(ROWS[0], ROWS[1] + 1):
    xs = np.nonzero(al[y] >= 128)[0]
    l, r = int(xs.min()), int(xs.max())
    zone[y, :l + DEPTH] = True; zone[y, r - DEPTH + 1:] = True
zone &= outer & ~leaf

# Inner mask: 22e's, but beside the wrap (rows 180-235) only the tube core is required, so the sides can tuck in.
inner = inner22e.copy()
wrap_rows = (yy >= ROWS[0]) & (yy <= 235)
inner[wrap_rows & ((xx < TL + TOL) | (xx > TR - TOL))] = False

# ---------------------------------------------------------------- measurements
def rim(a, y0, y1):
    # per row and side: how many pixels in from the silhouette edge stay darker than 115 (stops at a leaf)
    La = lum(a); ola = olive(a); out = {'L': [], 'R': []}
    for y in range(y0, y1 + 1):
        xs = np.nonzero(a[y, :, 3] >= 128)[0]
        if not len(xs): continue
        l, r = xs.min(), xs.max()
        k = 0
        while l + k < r and not ola[y, l + k] and La[y, l + k] < 115: k += 1
        out['L'].append(k)
        k = 0
        while r - k > l and not ola[y, r - k] and La[y, r - k] < 115: k += 1
        out['R'].append(k)
    return out
def soft(a, y0, y1):
    # share of rows whose outermost pixel on that side is partly transparent (an antialiased edge)
    res = {'L': 0, 'R': 0}; n = 0
    for y in range(y0, y1 + 1):
        xs = np.nonzero(a[y, :, 3] > 0)[0]
        if not len(xs): continue
        n += 1
        res['L'] += int(0 < a[y, xs.min(), 3] < 224); res['R'] += int(0 < a[y, xs.max(), 3] < 224)
    return {k: round(v / n, 2) for k, v in res.items()}
arm_rim = rim(arm, 60, 175); wrap_rim = rim(arm, *ROWS)
measured = {
    'darkRimArm60_175': {s: {'mean': round(float(np.mean(v)), 1), 'max': int(np.max(v))} for s, v in arm_rim.items()},
    'darkRim22e180_242': {s: {'mean': round(float(np.mean(v)), 1), 'max': int(np.max(v))} for s, v in wrap_rim.items()},
    'softEdgeArm60_175': soft(arm, 60, 175), 'softEdge22e180_242': soft(arm, *ROWS),
    'beyondTubeBark22e': int(((al >= 128) & ~ol & wrap_rows & ((xx < TL) | (xx > TR + 1))).sum()),
}
targets = dict(T)
targets['about'] = 'Batch 22f Qori_v3_Arm targets: repaint only the wrap edge zone of the 22e arm. Measured from Batch22e by make_batch22f_input.py on 2026-10-08.'
targets['arm22eSha256'] = hashlib.sha256((SRC / 'Qori_v3_Arm.png').read_bytes()).hexdigest()
targets['wrapEdge22f'] = {
    'rows': list(ROWS), 'zoneDepthPx': DEPTH,
    'darkRim': {'measure': 'pixels in from the silhouette edge, per row and side, with luminance < 115 (stops at a leaf)',
                'maxMeanPerSide': 7.0, 'maxPerRow': 9},
    'softEdge': {'measure': 'share of rows whose outermost pixel on that side has alpha 1-223', 'minShare': 0.8},
    'maxEdgeStepPx': 2,
    'zoneBoundary': {'measure': 'luminance of edge-zone bark minus the locked bark beside it (horizontal neighbours, per side), compared with the same pixels in 22e',
                     'maxSignedChange': 6.0, 'maxAbsIncrease': 6.0},
    'measured': measured,
}

OUT.mkdir(parents=True, exist_ok=True)
for m, n in ((outer, 'Qori_v3_Arm_Mask_Outer.png'), (inner, 'Qori_v3_Arm_Mask_Inner.png'), (zone, 'Qori_v3_Arm_Mask_EdgeZone.png')):
    Image.fromarray((m * 255).astype(np.uint8), 'L').save(OUT / n)

S = 4; Y0 = 170; Y1 = 250
base = Image.new('RGBA', (W * S, (Y1 - Y0) * S), (150, 150, 150, 255))
base.alpha_composite(Image.fromarray(arm[Y0:Y1].astype(np.uint8), 'RGBA').resize((W * S, (Y1 - Y0) * S), Image.NEAREST))
ov = np.zeros((Y1 - Y0, W, 4), np.uint8); ov[zone[Y0:Y1]] = (40, 120, 255, 90)
edge = zone & ~np.roll(zone, 1, 1) | zone & ~np.roll(zone, -1, 1) | zone & ~np.roll(zone, 1, 0) | zone & ~np.roll(zone, -1, 0)
ov[edge[Y0:Y1]] = (40, 120, 255, 255)
base.alpha_composite(Image.fromarray(ov, 'RGBA').resize(base.size, Image.NEAREST))
d = ImageDraw.Draw(base)
try:
    font = ImageFont.truetype('arial.ttf', 13)
except OSError:
    font = ImageFont.load_default()
for y in (ROWS[0], 236, ROWS[1] + 1):
    d.line([(0, (y - Y0) * S), (W * S, (y - Y0) * S)], fill=(0, 0, 0, 255)); d.text((2, (y - Y0) * S + 1), 'row %d' % y, fill=(0, 0, 0, 255), font=font)
d.text((4, 4), 'blue: edge zone, the only pixels that may change', fill=(0, 0, 0, 255), font=font)
base.convert('RGB').save(OUT / 'Qori_v3_Arm_EdgeZone_4x.png')

(OUT / 'batch22f_targets.json').write_text(json.dumps(targets, indent=1))
shutil.copy(HERE / 'check_arm_22f.py', OUT / 'check_arm_22f.py')
for sub, src, names in (('reference_22d', SRC_IN / 'reference_22d', ('Qori_v3_Arm.png',)),
                        ('reference_22e', SRC, ('Qori_v3_Arm.png', 'Qori_v3_Registration.json', 'Qori_v3_Proof_ArmCheck_HandJoin_2x.png'))):
    (OUT / sub).mkdir(exist_ok=True)
    for n in names:
        shutil.copy(src / n, OUT / sub / n)
shutil.copy(SRC_IN / 'reference_22d' / 'Qori_v3_Registration.json', OUT / 'reference_22d' / 'Qori_v3_Registration.json')
shutil.copy(SRC_IN / 'Qori_v3_Arm_Mask_HandCover.png', OUT / 'Qori_v3_Arm_Mask_HandCover.png')
shutil.copy(SRC_IN / 'Qori_v3_Arm_Template.png', OUT / 'Qori_v3_Arm_Template.png')
rig = Path(__file__).resolve().parents[2] / 'IncomingArt/Codex_v2/CLAUDE_REVIEW_38_Wrists.jpg'
if rig.exists():
    shutil.copy(rig, OUT / 'reference_22e' / 'CLAUDE_REVIEW_38_Wrists.jpg')

print('wrote', OUT)
print('zone px', int(zone.sum()), 'inner px', int(inner.sum()), '(22e', int(inner22e.sum()), ')')
print(json.dumps(measured))
