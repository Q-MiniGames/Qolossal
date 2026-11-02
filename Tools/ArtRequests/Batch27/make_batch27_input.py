"""Builds Batch27_Input for CODEX_PROMPT_BATCH27_QORI_WALK.md (the Young Adventurer Qori walk): the
16-frame block-out (walk_blockout.py), the model placed on the frame canvas, the targets measured on the
model, the registration template, the checker and the model references. Not art.

Usage: python make_batch27_input.py <Batch27_Input dir> [<Codex_v2_full dir>]
"""
import json, os, shutil, subprocess, sys
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
out = sys.argv[1]
FULL = sys.argv[2] if len(sys.argv) > 2 else r'C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full'
os.makedirs(out, exist_ok=True)
subprocess.check_call([sys.executable, os.path.join(HERE, 'walk_blockout.py'), out])
B = json.load(open(os.path.join(out, 'batch27_blockout.json')))
W, H = B['canvas']; FL = B['footLineRow']; PX = B['pivotColumn']; S = B['modelScale']


def _shift_or(m, k, axis):
    o = m.copy()
    if axis == 0: o[k:] |= m[:-k]; o[:-k] |= m[k:]
    else: o[:, k:] |= m[:, :-k]; o[:, :-k] |= m[:, k:]
    return o
def dilate(m, r):
    m = m > 0
    for axis in (0, 1):
        reach = 0
        while reach < r:
            k = min(reach + 1, r - reach); m = _shift_or(m, k, axis); reach += k
    return m
def erode(m, r): return ~dilate(~(m > 0), r)


# ---------------------------------------------------------------- the model on the frame canvas
MODEL_SRC = os.path.join(FULL, 'Batch19b', 'Concept_Qori_v3b_Turnaround.png')
SOLE_Y, PELVIS_X, HEAD_C = 1140, 345, (412, 355)        # measured on the large left figure (model px)
src = np.array(Image.open(MODEL_SRC).convert('RGB')).astype(int)[:, :545]
bgc = np.median(src[5:40, 5:40].reshape(-1, 3), axis=0)
bgish = np.abs(src - bgc).sum(axis=2) <= 22
bgish[1150:] = True                                      # the painted ground shadow
# background = bg-coloured pixels connected to the sheet's border (pale paint inside the figure stays)
bgm = np.zeros_like(bgish); bgm[0, :] = bgish[0, :]; bgm[:, 0] |= bgish[:, 0]; bgm[-1, :] |= bgish[-1, :]
small = bgish[::2, ::2]; reg = bgm[::2, ::2].copy()
while True:
    g = dilate(reg, 3) & small
    if g.sum() == reg.sum(): break
    reg = g
fig = ~np.kron(reg, np.ones((2, 2), bool))[:src.shape[0], :src.shape[1]]
fig[1143:] = False; fig[:, 528:] = False                 # ground shadow; the next turnaround view
rgba_ = np.zeros(src.shape[:2] + (4,), np.uint8); rgba_[:, :, :3] = src; rgba_[:, :, 3] = fig * 255
m = Image.fromarray(rgba_).resize((int(545 * S), int(src.shape[0] * S)), Image.LANCZOS)
model = Image.new('RGBA', (W, H), (0, 0, 0, 0))
ox, oy = int(round(PX - PELVIS_X * S)), int(round(FL - SOLE_Y * S))
model.alpha_composite(m, (ox, oy))
ma = np.array(model); ma[ma[:, :, 3] < 128] = 0; ma[ma[:, :, 3] >= 128, 3] = 255
os.makedirs(os.path.join(out, 'reference'), exist_ok=True)
Image.fromarray(ma).save(os.path.join(out, 'reference', 'Model_OnCanvas.png'))
model_head = [round(ox + HEAD_C[0] * S, 1), round(oy + HEAD_C[1] * S, 1)]
model_neck_row = round(oy + 455 * S, 1)

# ---------------------------------------------------------------- targets measured on the model
def kmeans(x, k, it=25):
    rng = np.random.default_rng(1); c = x[rng.choice(len(x), k, replace=False)].astype(float)
    for _ in range(it):
        lab = np.abs(x[:, None, :] - c[None]).sum(axis=2).argmin(axis=1)
        for j in range(k):
            if (lab == j).any(): c[j] = x[lab == j].mean(axis=0)
    return c
px_ = ma[:, :, :3][ma[:, :, 3] == 255].astype(float)
pal = kmeans(px_[np.random.default_rng(0).choice(len(px_), 40000, replace=False)], 14)
def med(x0, y0, x1, y1):
    X0, Y0, X1, Y1 = [int(round(v * S)) for v in (x0, y0, x1, y1)]
    reg = ma[oy + Y0:oy + Y1, ox + X0:ox + X1]; return [int(v) for v in np.median(reg[:, :, :3][reg[:, :, 3] == 255], axis=0)]
face = med(440, 405, 485, 440)                    # the cream cheek below the eye
TARGETS = dict(
    note='Limits for check_walk_27.py, measured on the model (Batch 19b turnaround, large figure, x%.2f) and the block-out.' % S,
    maxSoftAlphaShare=0.01,
    coreErodePx=10, minCoreCover=0.90, outlineTolerancePx=28, maxOutsideShare=0.03, areaRatio=[0.80, 1.30],
    soleTolerancePx=4, footBandPx=60, minFootBandIoU=0.45, maxFootShiftPx=12, maxStrideErrorPx=7,
    faceColour=face, faceTolerance=34, modelHead=model_head, modelNeckRow=model_neck_row, faceSizeTolerance=0.07,
    maxHeadShiftPx=16,
    modelPalette=[[int(v) for v in c] for c in pal], paletteTolerance=42, minPaletteShare=0.90, maxNeonShare=0.001,
    modelBark=med(270, 880, 300, 920), modelCape=med(60, 800, 120, 880), maxMedianColourDiff=24,
    detailRatio=[0.55, 1.6],
    changeRatio=[0.45, 2.0], maxPopFactor=2.3, maxNeighbourFaceChange=0.03, maxNeighbourDetailStep=0.18, maxNeighbourColourStep=6,
    followFrames=[4, 8], minFollowShare=0.35,
    maxStraightTopEdgePx=15, maxMantleStepFactor=1.5, maxStrayPx=0,
)
# where the cream face sits relative to the head centre, on the model: seeded at the cheek sample
TARGETS['faceCentreOffset'] = [round(462 * S + ox - model_head[0], 1), round(422 * S + oy - model_head[1], 1)]
json.dump(TARGETS, open(os.path.join(out, 'batch27_targets.json'), 'w'), indent=1)
sys.argv = ['check_walk_27.py', out, out]
src_ = open(os.path.join(HERE, 'check_walk_27.py')).read()
ns = {}
exec(src_.split('name = lambda k')[0].replace('DELIVERY, INPUT = sys.argv[1], sys.argv[2]', 'DELIVERY, INPUT = %r, %r' % (out, out)), ns)
ns['np'] = np
ns['FACE'] = TARGETS['faceColour']
exec(src_[src_.index('def head_box'):src_.index('# ---------------------------------------------------------------- 1. files')], ns)
mh = ns['head_box'](ma, model_head, model_neck_row, TARGETS['faceCentreOffset'])
fx, fy = mh[2] - model_head[0], mh[3] - model_head[1]
TARGETS['faceCentreOffset'] = [round(fx, 1), round(fy, 1)]
TARGETS['modelFaceSize'] = [round(mh[0], 1), round(mh[1], 1)]
json.dump(TARGETS, open(os.path.join(out, 'batch27_targets.json'), 'w'), indent=1)

# ---------------------------------------------------------------- registration template
n = B['frameCount']; cyc = round(B['cycleSeconds'] * 1000)
dur = [cyc // n + (1 if i < cyc % n else 0) for i in range(n)]
reg = dict(version=1, status='template (Claude, Batch 27 input)', character='Qori, Young Adventurer (Batch 19b model)',
           canvas=B['canvas'], pixelsPerUnit=100, footLineRow=FL, pivotColumn=PX, frameCount=n, facing='right, three-quarter (the game mirrors for left)',
           cycleSeconds=B['cycleSeconds'], groundPxPerFrame=B['groundPxPerFrame'],
           frames=[dict(frame=f['frame'], file=f['file'], key=B['keys'].get(str(f['frame']), B['keys'].get(f['frame'], '')), durationMs=dur[i]) for i, f in enumerate(B['frames'])])
json.dump(reg, open(os.path.join(out, 'Qori_Walk_Registration_TEMPLATE.json'), 'w'), indent=1)

# ---------------------------------------------------------------- the model references
ref = os.path.join(out, 'reference')
for rel, nm in (('Batch19b/Concept_Qori_v3b_Turnaround.png', 'Model_A_Turnaround_19b.png'), ('Batch19b/Concept_Qori_v3b_StaffIdle.png', 'Model_B_StaffIdle_19b.png'),
                ('Batch19b/Concept_Qori_v3b_CapeStudy.png', 'Model_C_CapeStudy_19b.png'), ('Batch19b/Concept_Qori_v3b_Ears.png', 'Model_D_Ears_19b.png'),
                ('Batch19b/Concept_Qori_v3b_Motion.png', 'Model_E_Motion_19b.png'), ('Batch19/sources/Concept_Qori_v3_YoungAdventurer_reference.png', 'Model_F_YoungAdventurer_original.png')):
    shutil.copy(os.path.join(FULL, rel.replace('/', os.sep)), os.path.join(ref, nm))
# side-by-side height check: the model on the canvas next to block-out frame 5
cmp_ = Image.new('RGBA', (2 * W, H), (205, 208, 210, 255)); cmp_.alpha_composite(Image.fromarray(ma)); cmp_.alpha_composite(Image.open(os.path.join(out, 'blockout', 'Qori_Walk_05_Blockout.png')), (W, 0))
cmp_.convert('RGB').save(os.path.join(out, 'Batch27_Model_vs_Blockout.png'))
shutil.copy(os.path.join(HERE, 'check_walk_27.py'), out)
for s in ('walk_blockout.py', 'make_batch27_input.py'): shutil.copy(os.path.join(HERE, s), os.path.join(out, 'sources_' + s))
print('Batch27_Input written to', out, '| face', TARGETS['modelFaceSize'], 'offset', TARGETS['faceCentreOffset'], '| durations', dur)
