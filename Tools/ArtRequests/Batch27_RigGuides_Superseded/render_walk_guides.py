"""Batch 27 input: renders the current in-game Qori rig (QoriRigData.json, the source QoriRigBuilder
builds QoriRig.prefab from) through its Walk clip into fixed-canvas guide frames for Codex to paint over.

Not art: these are paint-over guides, masks and measured targets. Draws the rig as the game shows it
while walking: normal arms (reach arms off), camera-side forearm = ForearmOpen (QoriAnimator.freeOpen),
Head_Neutral, the Leaf Staff on WeaponMountFar. Python 3, numpy and Pillow only.

Usage: python render_walk_guides.py <out_dir> [frames=16]
"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
RIG = os.path.join(ROOT, 'Assets', 'Art', 'Characters', 'QoriRig')
STAFF = os.path.join(ROOT, 'Assets', 'Resources', 'Armory', 'Weapons', 'LeafStaff.png')
STAFF_GRIP = (0.16, 0.4517)          # Grips.txt, pivot fractions, y from the bottom
STAFF_LENGTH = 7.489                 # QoriArmoryFactory.StaffLength (grip -> tip, units)

CANVAS = (1280, 1360)                # final frame canvas (px), 1 px = 1 rig sprite px (PPU 100)
FOOT_LINE_Y = 1290                   # row of the ground line (soles of planted feet touch it)
PIVOT_X = 760                        # column of the pelvis averaged over the cycle

# Body-part groups (for the per-part masks and checks)
GROUPS = {
    'head':  ['Head'],
    'ears':  ['EarUpper', 'EarLower'],
    'torso': ['Torso', 'Skirt', 'Mantle'],
    'cape':  ['Cape1', 'Cape1Lower', 'Cape2', 'Cape2Lower', 'Cape3', 'Cape3Lower'],
    'arm_back':  ['UpperArmNear', 'ForearmNear'],
    'arm_front': ['UpperArmFar'],
    'hand_front': ['ForearmFar'],
    'leg_back':  ['ThighNear', 'ShinNear', 'FootNear'],
    'leg_front': ['ThighFar', 'ShinFar', 'FootFar'],
}
HIDDEN = {'UpperArmReachNear', 'ForearmReachNear', 'UpperArmReachFar', 'ForearmReachFar'}
SPRITE_SWAP = {'ForearmFar': 'ForearmOpen'}
GROUP_COLOURS = {'head': (235, 225, 200), 'ears': (150, 190, 110), 'torso': (220, 200, 150), 'cape': (95, 140, 80),
                 'arm_back': (120, 80, 160), 'arm_front': (230, 90, 60), 'hand_front': (255, 60, 160), 'leg_back': (60, 110, 200), 'leg_front': (240, 170, 40),
                 'staff': (255, 255, 255)}


def load():
    with open(os.path.join(RIG, 'QoriRigData.json')) as f:
        d = json.load(f)
    parts = {p['name']: p for p in d['parts']}
    images = {}
    for p in d['parts']:
        images[p['name']] = Image.open(os.path.join(RIG, p['file'])).convert('RGBA')
    return d, parts, images


def sample(track, times, t):
    """Hermite sample of a baked track: v = key values, t = key tangents, times = the clip's key times."""
    v, tan = track['v'], track['t']
    if t <= times[0]: return v[0]
    for i in range(1, len(times)):
        if t <= times[i]:
            h = times[i] - times[i - 1]; u = (t - times[i - 1]) / h
            h00 = 2 * u ** 3 - 3 * u ** 2 + 1; h10 = u ** 3 - 2 * u ** 2 + u; h01 = -2 * u ** 3 + 3 * u ** 2; h11 = u ** 3 - u ** 2
            return h00 * v[i - 1] + h10 * h * tan[i - 1] + h01 * v[i] + h11 * h * tan[i]
    return v[-1]


def mat(tx, ty, rot, sx=1.0, sy=1.0):
    c, s = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    return np.array([[c * sx, -s * sy, tx], [s * sx, c * sy, ty], [0, 0, 1.0]])


# ---- Direction applied on top of the rig's Walk clip (see the Batch 27 prompt, section 3) ----
# Pelvis bob per frame of a step (px, + = up), frames 1-8 and again 9-16: contact, down (lowest, f3),
# passing (f5), up (highest, f7). The rig's own Walk is highest at contact and lowest at passing, which
# reads as a plod; this swaps it to the standard walk and doubles its height (2% of Qori's height).
BOB_PX = [2, -6, -12, -7, 0, 8, 12, 8]
ARM_PHASE = 0.5        # arms, hands and the weapon grip sampled half a cycle later: the camera-side
                       # shoulder (and both hands on the shaft) now swings opposite the camera-side leg
EAR_LAG = (2, 3)       # frames the upper and lower ear trail the head's vertical motion
EAR_GAIN = (6.0, 8.0)  # degrees at full head speed (- = tip up)
CAPE_LAG = 2           # frames the cloak hem trails the body's vertical motion
CAPE_GAIN = 6.0        # degrees on each lower cloak panel at full speed
SKIRT_GAIN = 2.5       # degrees on the skirt (lag 1 frame)
HEAD_LAG = 1           # frames the head nod trails the bob
HEAD_GAIN = 1.5        # degrees of nod at full speed (- = chin down)
# Torso lean per frame of a step (degrees added to the rig's mean torso angle; - = forward): leans into
# the down, straightens on the up, the same in both steps. The rig's own torso track leans 4 degrees more
# on the second step than the first, which reads as a limp. The head counter-rotates so the face stays level.
TORSO_WAVE = [0, -0.5, -1, -0.5, 0, 0.5, 1, 0.5]
ARM_PATHS = ('UpperArm', 'Forearm', 'WeaponMount', 'Hand')


def bob(k):
    return BOB_PX[k % 8]


def bob_speed(k):
    """Normalised vertical speed of the pelvis at frame k (central difference, -1..1)."""
    v = [(BOB_PX[(i + 1) % 8] - BOB_PX[(i - 1) % 8]) / 2.0 for i in range(8)]
    return v[k % 8] / max(abs(x) for x in v)


def locals_at(d, clip, t, arm_t):
    tracks = {}
    for tr in clip['tracks']:
        tracks.setdefault(tr['path'], {})[tr['attr']] = tr
    loc = {}
    for b in d['bones']:
        tk = tracks.get(b['path'], {})
        tt = arm_t if any(a in b['path'].split('/')[-1] for a in ARM_PATHS) else t
        x, y = b['pos']; rot = b['rot']; sx = sy = 1.0
        if 'm_LocalPosition.x' in tk: x = sample(tk['m_LocalPosition.x'], clip['times'], tt)
        if 'm_LocalPosition.y' in tk: y = sample(tk['m_LocalPosition.y'], clip['times'], tt)
        if 'localEulerAnglesRaw.z' in tk: rot = sample(tk['localEulerAnglesRaw.z'], clip['times'], tt)
        if 'm_LocalScale.y' in tk: sy = sample(tk['m_LocalScale.y'], clip['times'], tt)
        if 'm_LocalScale.x' in tk: sx = sample(tk['m_LocalScale.x'], clip['times'], tt)
        loc[b['name']] = [x, y, rot, sx, sy]
    return loc


def worlds(d, loc):
    world = {'Root': np.eye(3)}
    for b in d['bones']:
        world[b['name']] = world[b['parent']] @ mat(*loc[b['name']])
    return world


def ang2(v):
    return math.degrees(math.atan2(v[1], v[0]))


PELVIS_MEAN = [None]
TORSO_MEAN = [None]
HEAD_MEAN = [None]


def pose(d, clip, t, k=None, n=16, direct=True):
    """World matrices (units, y up) of every bone. With direct=True, frame k gets the Batch 27
    direction: new bob (legs re-solved by two-bone IK so planted feet stay put), arms half a cycle
    later, and ear, cloak, skirt and head lag."""
    L = clip['length']
    if not direct or k is None:
        loc = locals_at(d, clip, t, t)
        return worlds(d, loc), {b: v[2] for b, v in loc.items()}
    loc = locals_at(d, clip, t, (t + ARM_PHASE * L) % L)
    orig = worlds(d, loc)
    if PELVIS_MEAN[0] is None:
        PELVIS_MEAN[0] = float(np.mean([locals_at(d, clip, L * i / n, 0)['Body'][1] for i in range(n)]))
    if TORSO_MEAN[0] is None:
        TORSO_MEAN[0] = float(np.mean([locals_at(d, clip, L * i / n, 0)['Torso'][2] for i in range(n)]))
        HEAD_MEAN[0] = float(np.mean([locals_at(d, clip, L * i / n, 0)['Head'][2] for i in range(n)]))
    loc['Body'][1] = PELVIS_MEAN[0] + bob(k) / 100.0
    loc['Torso'][2] = TORSO_MEAN[0] + TORSO_WAVE[k % 8]
    loc['Head'][2] = HEAD_MEAN[0] - TORSO_WAVE[k % 8]
    world = worlds(d, loc)
    by_name = {b['name']: b for b in d['bones']}
    for side in ('Near', 'Far'):
        th, sh, ft = 'Thigh' + side, 'Shin' + side, 'Foot' + side
        hip = world[th][:2, 2]
        ankle = orig[ft][:2, 2]
        knee0 = orig[sh][:2, 2]
        v1 = np.array(by_name[sh]['pos']); v2 = np.array(by_name[ft]['pos'])
        l1, l2 = np.linalg.norm(v1), np.linalg.norm(v2)
        dvec = ankle - hip; dist = np.linalg.norm(dvec)
        dist_c = min(max(dist, abs(l1 - l2) + 1e-4), l1 + l2 - 1e-4)
        a = (l1 * l1 - l2 * l2 + dist_c * dist_c) / (2 * dist_c)
        h = math.sqrt(max(l1 * l1 - a * a, 0))
        u = dvec / dist; perp = np.array([-u[1], u[0]])
        cands = [hip + u * a + perp * h, hip + u * a - perp * h]
        knee = min(cands, key=lambda c: np.linalg.norm(c - knee0))
        ankle_r = knee + (ankle - knee) / np.linalg.norm(ankle - knee) * l2
        th_w = ang2(knee - hip) - ang2(v1)
        sh_w = ang2(ankle_r - knee) - ang2(v2)
        body_w = ang2(world['Body'][:2, 0])
        foot_w = ang2(orig[ft][:2, 0])
        loc[th][2] = th_w - body_w
        loc[sh][2] = sh_w - th_w
        loc[ft][2] = foot_w - sh_w
    vs = lambda lag: bob_speed(k - lag)
    loc['EarUpper'][2] += EAR_GAIN[0] * vs(EAR_LAG[0])
    loc['EarLower'][2] += EAR_GAIN[1] * vs(EAR_LAG[1])
    for c in ('Cape1Lower', 'Cape2Lower', 'Cape3Lower'):
        loc[c][2] += CAPE_GAIN * vs(CAPE_LAG)
    loc['Skirt'][2] += SKIRT_GAIN * vs(1)
    loc['Head'][2] += HEAD_GAIN * vs(HEAD_LAG)
    world = worlds(d, loc)
    return world, {b: v[2] for b, v in loc.items()}


# Root -> canvas offset (units), calibrated in main() so the planted sole sits on FOOT_LINE_Y and the
# pelvis averages over PIVOT_X.
OFFSET = [0.0, 0.0]


def to_px(m):
    """Map from units (y up, Root at the origin) to canvas px (y down)."""
    return np.array([[100.0, 0, PIVOT_X + 100.0 * OFFSET[0]], [0, -100.0, FOOT_LINE_Y - 100.0 * OFFSET[1]], [0, 0, 1]]) @ m


def draw_sprite(canvas, img, pivot, world, tint=1.0, solid=None):
    """Composite img with its pivot at the bone origin. world: units matrix of the bone."""
    w, h = img.size
    # sprite px (u right, v down) -> bone-local units
    s2b = np.array([[0.01, 0, -pivot[0] * w * 0.01], [0, -0.01, (h - pivot[1] * h) * 0.01], [0, 0, 1]])
    full = to_px(world) @ s2b                     # sprite px -> canvas px
    inv = np.linalg.inv(full)
    src = img
    if solid is not None:
        a = np.array(img)[:, :, 3]
        arr = np.zeros((h, w, 4), np.uint8); arr[:, :, :3] = solid; arr[:, :, 3] = a
        src = Image.fromarray(arr)
    elif tint != 1.0:
        arr = np.array(img).astype(np.float32); arr[:, :, :3] *= tint
        src = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
    layer = src.transform(CANVAS, Image.AFFINE, tuple(inv[:2].reshape(-1)), resample=Image.BICUBIC)
    canvas.alpha_composite(layer)
    return layer


def bone_px(world, name, local=(0.0, 0.0)):
    p = to_px(world[name]) @ np.array([local[0], local[1], 1.0])
    return float(p[0]), float(p[1])


def bone_angle(world, name):
    m = world[name]
    return math.degrees(math.atan2(m[1, 0], m[0, 0]))




# The delivery layers, back to front: Body (Codex paints), the weapon (game), FrontHand (Codex paints),
# Head with ears (game, the accepted head sprites, placed from the registration).
LAYERS = {
    'body': lambda b: b not in ('Head', 'EarUpper', 'EarLower', 'ForearmFar'),
    'front': lambda b: b == 'ForearmFar',
    'head': lambda b: b in ('Head', 'EarUpper', 'EarLower'),
    'staff': lambda b: False,
}


def render(d, parts, images, clip, t, staff_img, k=None, n=16, direct=True, layer=None, group=None, solid=False):
    """layer: one of LAYERS (None = everything); group: one of GROUPS or 'staff'; solid: flat group colours."""
    world, local_rot = pose(d, clip, t, k, n, direct)
    canvas = Image.new('RGBA', CANVAS, (0, 0, 0, 0))
    items = []
    for i, b in enumerate(d['bones']):
        if not b['part'] or b['name'] in HIDDEN: continue
        items.append((b['order'], i, b))
    items.append((d.get('weaponOrderFar', 15), 10 ** 6, None))
    for order, i, b in sorted(items, key=lambda x: (x[0], x[1])):
        if b is None:
            if layer not in (None, 'staff') or group not in (None, 'staff'): continue
            sc = STAFF_LENGTH / (staff_img.width * (1 - STAFF_GRIP[0]) / 100)
            draw_sprite(canvas, staff_img, STAFF_GRIP, world['WeaponMountFar'] @ mat(0, 0, 0, sc, sc), 1.0,
                        GROUP_COLOURS['staff'] if solid else None)
            continue
        name = b['name']
        g = next((g for g, names in GROUPS.items() if name in names), None)
        if layer is not None and not LAYERS[layer](name): continue
        if group is not None and group != g: continue
        part = SPRITE_SWAP.get(name, b['part'])
        draw_sprite(canvas, images[part], parts[part]['pivot'], world[name], b.get('tint', 1.0),
                    GROUP_COLOURS.get(g, (128, 128, 128)) if solid else None)
    return canvas, world, local_rot


def alpha(img):
    return np.array(img)[:, :, 3]


def detail(img, mask):
    """Mean luminance step between horizontal neighbours inside mask (texture, not smear)."""
    a = np.array(img).astype(np.float32)
    lum = a[:, :, 0] * .299 + a[:, :, 1] * .587 + a[:, :, 2] * .114
    m = (mask[:, 1:] > 250) & (mask[:, :-1] > 250)
    return float(np.abs(np.diff(lum, axis=1))[m].mean()) if m.any() else 0.0


def main():
    out = sys.argv[1]
    n = int(sys.argv[2]) if len(sys.argv) > 2 else 16
    for sub in ('guides', 'masks', 'layers', 'reference'):
        os.makedirs(os.path.join(out, sub), exist_ok=True)
    d, parts, images = load()
    clip = next(c for c in d['clips'] if c['name'] == 'Walk')
    staff = Image.open(STAFF).convert('RGBA')
    length = clip['length']
    # Calibrate: the lowest leg pixel over the cycle goes on the foot line; the pelvis averages on PIVOT_X.
    OFFSET[0], OFFSET[1] = 0.0, 6.0
    lows, pelx = [], []
    for k in range(n):
        lg, world, _ = render(d, parts, images, clip, length * k / n, staff, k, n, layer='body')
        rows = np.where(alpha(lg).max(axis=1) > 128)[0]
        lows.append(rows.max()); pelx.append(bone_px(world, 'Body')[0])
    OFFSET[1] += (max(lows) - FOOT_LINE_Y) / 100.0
    OFFSET[0] += (PIVOT_X - float(np.mean(pelx))) / 100.0
    frames = []
    font = ImageFont.load_default()
    for k in range(n):
        t = length * k / n
        name = 'Qori_Walk_%02d' % (k + 1)
        full, world, rot = render(d, parts, images, clip, t, staff, k, n)
        full.save(os.path.join(out, 'guides', name + '_Guide.png'))
        lay = {}
        for ln in LAYERS:
            lay[ln], _, _ = render(d, parts, images, clip, t, staff, k, n, layer=ln)
        lay['body'].save(os.path.join(out, 'guides', name + '_Body_Guide.png'))
        lay['front'].save(os.path.join(out, 'guides', name + '_FrontHand_Guide.png'))
        lay['head'].save(os.path.join(out, 'layers', name + '_HeadLayer.png'))
        lay['staff'].save(os.path.join(out, 'layers', name + '_StaffLayer.png'))
        flat, _, _ = render(d, parts, images, clip, t, staff, k, n, solid=True)
        flat.save(os.path.join(out, 'guides', name + '_Parts.png'))
        ref, _, _ = render(d, parts, images, clip, t, staff, k, n, direct=False)
        ref.save(os.path.join(out, 'reference', name + '_RigClipAsIs.png'))
        masks = {}
        for g in list(GROUPS) + ['staff']:
            gl, _, _ = render(d, parts, images, clip, t, staff, k, n, group=g)
            masks[g] = alpha(gl)
            Image.fromarray(masks[g]).save(os.path.join(out, 'masks', '%s_Mask_%s.png' % (name, g)))
        mb, mf = alpha(lay['body']), alpha(lay['front'])
        Image.fromarray(mb).save(os.path.join(out, 'masks', name + '_Mask_Body.png'))
        Image.fromarray(mf).save(os.path.join(out, 'masks', name + '_Mask_FrontHand.png'))
        ys, xs = np.where(mb > 128)
        sole = {}
        for side in ('back', 'front'):
            rows = np.where(masks['leg_' + side].max(axis=1) > 128)[0]
            sole[side] = int(rows.max())
        torso_rows = np.where(masks['torso'].max(axis=1) > 128)[0]
        P = lambda b: [round(v, 1) for v in bone_px(world, b)]
        rec = dict(
            frame=k + 1, clipTime=round(t, 5),
            bodyFile=name + '_Body.png', frontHandFile=name + '_FrontHand.png',
            bboxBody=[int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())],
            mantleTopRow=int(torso_rows.min()),
            soleRow=sole, pelvis=P('Body'), bodyAngle=round(bone_angle(world, 'Torso'), 2),
            neck=P('Head'), headAngle=round(bone_angle(world, 'Head'), 2),
            earUpper=P('EarUpper'), earUpperAngle=round(bone_angle(world, 'EarUpper'), 2),
            earLower=P('EarLower'), earLowerAngle=round(bone_angle(world, 'EarLower'), 2),
            weaponGrip=P('WeaponMountFar'), weaponAngle=round(bone_angle(world, 'WeaponMountFar'), 2),
            handBack=P('HandNear'), handFront=P('HandFar'),
            kneeBack=P('ShinNear'), kneeFront=P('ShinFar'), ankleBack=P('FootNear'), ankleFront=P('FootFar'),
            areaPx={g: int((masks[g] > 128).sum()) for g in masks},
            areaBodyPx=int((mb > 128).sum()), areaFrontHandPx=int((mf > 128).sum()),
            detailBody=round(detail(lay['body'], mb), 2),
        )
        frames.append(rec)
        ann = Image.new('RGBA', CANVAS, (200, 204, 206, 255))
        ghost = np.array(lay['head']); ghost[:, :, 3] = (ghost[:, :, 3] * .35).astype(np.uint8)
        ann.alpha_composite(flat); ann.alpha_composite(Image.fromarray(ghost))
        dr = ImageDraw.Draw(ann)
        dr.line([(0, FOOT_LINE_Y), (CANVAS[0], FOOT_LINE_Y)], fill=(255, 0, 0, 255), width=2)
        dr.line([(PIVOT_X, 0), (PIVOT_X, CANVAS[1])], fill=(0, 200, 255, 255), width=1)
        dr.line([(0, rec['mantleTopRow']), (CANVAS[0], rec['mantleTopRow'])], fill=(0, 160, 0, 255), width=1)
        for key in ('pelvis', 'neck', 'kneeBack', 'kneeFront', 'ankleBack', 'ankleFront', 'handBack', 'handFront', 'weaponGrip'):
            x, y = rec[key]; dr.ellipse([x - 6, y - 6, x + 6, y + 6], outline=(0, 0, 0, 255), width=2)
        gx, gy = rec['weaponGrip']; a = math.radians(rec['weaponAngle'])
        dr.line([(gx - 400 * math.cos(a), gy + 400 * math.sin(a)), (gx + 700 * math.cos(a), gy - 700 * math.sin(a))], fill=(255, 255, 255, 255), width=1)
        dr.text((14, 14), '%s   t = %.3f s' % (name, t), fill=(0, 0, 0, 255), font=font)
        ann.convert('RGB').save(os.path.join(out, 'guides', name + '_Annotated.png'))

    def sheet(folder, suffix, file, scale=4, bg=(150, 170, 175, 255)):
        cols = 8; w, h = CANVAS[0] // scale, CANVAS[1] // scale
        s = Image.new('RGBA', (cols * w, ((n + cols - 1) // cols) * h), bg)
        dr = ImageDraw.Draw(s)
        for k in range(n):
            im = Image.open(os.path.join(out, folder, 'Qori_Walk_%02d_%s.png' % (k + 1, suffix))).convert('RGBA')
            s.alpha_composite(im.resize((w, h), Image.LANCZOS), ((k % cols) * w, (k // cols) * h))
            dr.text(((k % cols) * w + 6, (k // cols) * h + 6), '%02d' % (k + 1), fill=(0, 0, 0, 255), font=font)
            y = (k // cols) * h + FOOT_LINE_Y // scale
            dr.line([((k % cols) * w, y), ((k % cols + 1) * w, y)], fill=(200, 0, 0, 255))
        s.convert('RGB').save(os.path.join(out, file))
    sheet('guides', 'Guide', 'Batch27_Guide_Sheet.png')
    sheet('guides', 'Body_Guide', 'Batch27_Body_Guide_Sheet.png')
    sheet('guides', 'Annotated', 'Batch27_Annotated_Sheet.png')
    sheet('reference', 'RigClipAsIs', 'Batch27_RigClipAsIs_Sheet.png')
    with open(os.path.join(out, 'batch27_guide_frames.json'), 'w') as f:
        json.dump(dict(canvas=list(CANVAS), footLineRow=FOOT_LINE_Y, pivotColumn=PIVOT_X, pixelsPerUnit=100,
                       clip='Walk', clipLength=length, frameCount=n,
                       direction=dict(bobPx=BOB_PX, armPhase=ARM_PHASE, earLag=EAR_LAG, earGain=EAR_GAIN,
                                      capeLag=CAPE_LAG, torsoWave=TORSO_WAVE, capeGain=CAPE_GAIN, skirtGain=SKIRT_GAIN, headLag=HEAD_LAG, headGain=HEAD_GAIN),
                       frames=frames, groupColours=GROUP_COLOURS,
                       source='Assets/Art/Characters/QoriRig/QoriRigData.json'), f, indent=1)
    print('wrote %d guide frames to %s' % (n, out))


if __name__ == '__main__':
    main()
