"""Builds Batch27_Input for the Codex prompt CODEX_PROMPT_BATCH27_QORI_WALK.md: the 16 guide frames
rendered from the current rig (render_walk_guides.py), the targets, the registration template, the
checker and read-only references. Not art.

Usage: python make_batch27_input.py <Batch27_Input dir>
"""
import json, os, shutil, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
out = sys.argv[1]
os.makedirs(out, exist_ok=True)
subprocess.check_call([sys.executable, os.path.join(HERE, 'render_walk_guides.py'), out, '16'])
G = json.load(open(os.path.join(out, 'batch27_guide_frames.json')))

TARGETS = dict(
    note='Limits for check_walk_27.py. Measured on the guide frames (which pass) and a broken mock (which fails); see the prompt.',
    maxSoftAlphaShare=0.01,      # share of visible px with alpha 1-249 more than 3 px from solid paint (a glow)
    capeFreedomPx=60,            # the cloak may drag up to this far from its guide position
    edgeTolerancePx=3, minCoreMatch=0.97, minBodyIoU=0.80,
    outlineTolerancePx=10, maxOutsideShare=0.006,
    bodyAreaRatio=[0.93, 1.07], capeAreaRatio=[0.85, 1.20],
    maxHeadPaintPx=150,
    soleTolerancePx=3, footBandPx=70, minFootBandIoU=0.72, maxFootShiftPx=6,
    bobTolerancePx=5, minBobCorrelation=0.85, minUnderHeadSolid=0.97,
    maxMedianColourDiff=16, maxNeonShare=0.001, detailRatio=[0.75, 1.6], limbWidthRatio=[0.82, 1.18],
    handTolerancePx=8, handAreaRatio=[0.85, 1.20], gripRadiusPx=45, minGripCover=0.80, maxStaffPaintShare=0.02,
    silhouetteChangeRatio=[0.55, 1.7], maxPopFactor=2.2, maxNeighbourColourStep=5,    # added to the guide's own step for that pair
    maxNeighbourDetailStep=0.15,
)
json.dump(TARGETS, open(os.path.join(out, 'batch27_targets.json'), 'w'), indent=1)

# Registration template: what the game needs to play the frames; Codex returns it unchanged.
n = G['frameCount']; cycle_ms = round(G['clipLength'] * 1000)
dur = [cycle_ms // n + (1 if i < cycle_ms % n else 0) for i in range(n)]
f = G['frames']
stance = [fr['ankleBack'][0] for fr in f[0:7]]       # far-side foot planted over frames 1-7
ground = round((stance[0] - stance[-1]) / 6 * n, 1)
reg = dict(
    version=1, status='template (Claude, Batch 27 input)', character='Qori (current in-game rig look)',
    canvas=G['canvas'], pixelsPerUnit=100, footLineRow=G['footLineRow'], pivotColumn=G['pivotColumn'], frameCount=n,
    facing='right (the game mirrors for left)',
    layerOrder=['Body (Codex)', 'weapon (game, on weaponGrip at weaponAngle)', 'FrontHand (Codex)', 'Head_* sprite + ears (game, on neck at headAngle)'],
    cycleSeconds=G['clipLength'], groundPxPerCycle=ground,
    frames=[dict(frame=fr['frame'], body=fr['bodyFile'], frontHand=fr['frontHandFile'], neck=fr['neck'], headAngle=fr['headAngle'],
                 earUpperAngle=fr['earUpperAngle'], earLowerAngle=fr['earLowerAngle'], weaponGrip=fr['weaponGrip'],
                 weaponAngle=fr['weaponAngle'], durationMs=dur[i]) for i, fr in enumerate(f)],
)
json.dump(reg, open(os.path.join(out, 'Qori_Walk_Registration_TEMPLATE.json'), 'w'), indent=1)

shutil.copy(os.path.join(HERE, 'check_walk_27.py'), out)
refs = os.path.join(out, 'reference')
shutil.copy(os.path.join(ROOT, 'Assets', 'Art', 'Characters', 'Qori', 'Qori_Idle_v1.png'), os.path.join(refs, 'Qori_Idle_v1_ApprovedDrawing.png'))
parts = os.path.join(refs, 'CurrentRigParts'); os.makedirs(parts, exist_ok=True)
for p in os.listdir(os.path.join(ROOT, 'Assets', 'Art', 'Characters', 'QoriRig', 'Parts')):
    if p.endswith('.png'): shutil.copy(os.path.join(ROOT, 'Assets', 'Art', 'Characters', 'QoriRig', 'Parts', p), parts)
shutil.copy(os.path.join(ROOT, 'Assets', 'Resources', 'Armory', 'Weapons', 'LeafStaff.png'), refs)
for s in ('render_walk_guides.py', 'make_batch27_input.py'):
    shutil.copy(os.path.join(HERE, s), os.path.join(out, 'sources_' + s))
print('Batch27_Input written to', out, '| groundPxPerCycle', ground, '| durations', dur)
