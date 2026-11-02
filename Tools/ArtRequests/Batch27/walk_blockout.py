"""Batch 27 input: a 16-frame walk block-out for the Young Adventurer Qori (the approved Batch 19b look).

Not art. A posed construction figure (flat shapes per body part) with the model's proportions measured
on the large three-quarter figure of Codex_v2_full/Batch19b/Concept_Qori_v3b_Turnaround.png, walked by
the direction below, on one fixed canvas with a fixed ground line and pivot. Codex paints the model over it.
Python 3, numpy and Pillow only.

Usage: python walk_blockout.py <out_dir>
"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

# ---------------------------------------------------------------- canvas
W, H = 1400, 1400
FOOT_LINE = 1340          # soles of planted feet sit on this row
PIVOT_X = 760             # pelvis column (fixed: the game moves Qori, the frames walk on the spot)
N = 16
CYCLE_S = 0.72            # seconds per cycle at the drawn speed (45 ms a frame)

# ---------------------------------------------------------------- model (19b turnaround, large figure, x1.13)
S = 1.13                                   # 885 model px (dome top to sole) -> 1000 canvas px
HEAD_W, HEAD_H = 210 * S, 200 * S          # cream head dome
HEAD_ABOVE_NECK = 100 * S                  # head centre above the neck point
HEAD_FWD = 12 * S
TORSO_LEN = 345 * S                        # hip joints (under the skirt) to neck
THIGH, SHIN = 140 * S, 112 * S
ANKLE_H = 88 * S                           # ankle centre above the sole
HEEL, TOE = 46 * S, 100 * S                # sole length behind / in front of the ankle
HIP_SPREAD = 32 * S                        # each hip from the pelvis centre, along x (three-quarter view)
UPPER_ARM, FOREARM, HAND = 112 * S, 92 * S, 76 * S
EAR_LEN = 330 * S
EAR_REST = {'near': 166.0, 'far': 143.0}  # degrees, measured base -> tip (0 = right, 90 = up)
CAPE_LEN = 490 * S
WIDTH = dict(thigh=48 * S, shin=38 * S, upper_arm=32 * S, forearm=28 * S, hand=40 * S)

# ---------------------------------------------------------------- direction
STEP = 0.64 * (THIGH + SHIN + ANKLE_H)     # step length (px); stride per cycle = 2 * STEP
STANCE = 0.60                              # share of the cycle each foot is on the ground
BASE_HIP = 0.875 * (THIGH + SHIN + ANKLE_H) # pelvis height above the ground, knees soft
BOB = 0.016 * 1000                         # +/- px: 32 px from the down to the up
LEAN = -5.0                                # torso, degrees (- = forward)
LEAN_DOWN = -1.5                           # extra forward lean on the down
ARM_SWING = 22.0                           # upper arm, degrees each way
ARM_LAG = 1                                # frames the arm swing trails the legs
EAR_GAIN = {'far': 9.0, 'near': 11.0}       # degrees at full vertical speed
EAR_LAG = {'far': 2, 'near': 3}
HEAD_NOD = 2.0
CAPE_TRAIL = 16.0                          # degrees the cloak trails back at walking speed
CAPE_WAVE = 26.0                           # px at the hem: a ripple travelling down the cloak once per step
CAPE_WAVE_DELAY = 0.5                      # of a step, top to hem (the hem answers 4 frames after the shoulders)


def ang(v): return math.degrees(math.atan2(v[1], v[0]))
def vec(a, l=1.0): return np.array([math.cos(math.radians(a)), math.sin(math.radians(a))]) * l
def px(p): return (float(PIVOT_X + p[0]), float(FOOT_LINE - p[1]))   # y-up local -> canvas


def bob(t):
    """Pelvis height offset: lowest on the down (t = 1/8, 5/8), highest on the up (3/8, 7/8)."""
    return -BOB * math.cos(4 * math.pi * (t - 0.125))


def vbob(t, lag_frames=0):
    """Normalised vertical speed of the pelvis, lag_frames late (-1..1)."""
    t = t - lag_frames / N
    return math.sin(4 * math.pi * (t - 0.125))


def stance_ankle(p):
    """Ankle (x, y-up, relative to the pelvis column) and foot angle while the foot is on the ground,
    p in [0, STANCE). The contact point slides back at the ground speed (2 * STEP per cycle)."""
    heel_x = STEP * STANCE - 2 * STEP * p
    if p < 0.08:                                   # heel strike, rolling down onto the sole
        a = 10.0 * (1 - p / 0.08)
        return np.array([heel_x, 0.0]) + rot(np.array([HEEL, ANKLE_H]), a), a
    if p < STANCE - 0.2:                           # flat
        return np.array([heel_x + HEEL, ANKLE_H]), 0.0
    k = (p - (STANCE - 0.2)) / 0.2                 # heel rises, rolling over the ball of the foot
    a = -34.0 * k
    ball = np.array([heel_x + HEEL + 0.7 * TOE, 0.0])
    return ball + rot(np.array([-0.7 * TOE, ANKLE_H]), a), a


def foot(phase):
    """Ankle, foot angle and planted flag for a leg at its own phase (0 = heel strike, STANCE = toe off)."""
    p = phase % 1.0
    if p < STANCE:
        ankle, a = stance_ankle(p)
        return ankle, a, True
    k = (p - STANCE) / (1 - STANCE)
    start, a0 = stance_ankle(STANCE - 1e-6)
    end, a1 = stance_ankle(0.0)
    s = (1 - math.cos(math.pi * k)) / 2
    pos = start + (end - start) * s
    pos[1] += 0.17 * (THIGH + SHIN) * math.sin(math.pi * k)
    a = a0 + (a1 - a0) * s - 14 * math.sin(math.pi * k)
    low = min((rot(np.array(v), a))[1] for v in ((-HEEL, -ANKLE_H), (TOE, -ANKLE_H)))
    if pos[1] + low < 6.0: pos[1] = 6.0 - low          # a swinging foot never touches the ground
    return pos, a, False


def rot(v, a):
    c, s = math.cos(math.radians(a)), math.sin(math.radians(a))
    return np.array([v[0] * c - v[1] * s, v[0] * s + v[1] * c])


def ik(hip, ankle, l1, l2, forward=1):
    d = ankle - hip; dist = np.linalg.norm(d)
    dist_c = min(max(dist, abs(l1 - l2) + 1e-3), l1 + l2 - 1e-3)
    a = (l1 * l1 - l2 * l2 + dist_c * dist_c) / (2 * dist_c)
    h = math.sqrt(max(l1 * l1 - a * a, 0.0))
    u = d / dist; perp = np.array([-u[1], u[0]])
    c1, c2 = hip + u * a + perp * h, hip + u * a - perp * h
    knee = c1 if (c1[0] - c2[0]) * forward > 0 else c2       # knees point forward (right)
    ankle_r = knee + (ankle - knee) / np.linalg.norm(ankle - knee) * l2
    return knee, ankle_r, dist > l1 + l2


# ---------------------------------------------------------------- cloak: a damped chain, walked to a steady loop
def simulate_cape(anchors):
    """anchors: per frame the cloak's attachment point (y-up). Returns per frame 7 chain points."""
    n = 7; seg = CAPE_LEN / (n - 1); sub = 24
    pts = np.array([anchors[0] + vec(-90 - CAPE_TRAIL, seg * i) for i in range(n)])
    prev = pts.copy()
    dt = CYCLE_S / N / sub
    g = np.array([0.0, -2600.0]); air = np.array([-2.0 * STEP / CYCLE_S, 0.0])   # air moving back past Qori
    out = []
    for cyc in range(8):
        for k in range(N):
            a0, a1 = anchors[k], anchors[(k + 1) % N]
            for s in range(sub):
                anc = a0 + (a1 - a0) * (s + 1) / sub
                vel = (pts - prev) / dt
                acc = g + 1.6 * (air - vel) * np.linspace(0.2, 1.0, n)[:, None]
                nxt = pts + (pts - prev) * 0.985 + acc * dt * dt
                prev, pts = pts, nxt
                pts[0] = anc
                for _ in range(6):
                    for i in range(n - 1):
                        d = pts[i + 1] - pts[i]; L = np.linalg.norm(d)
                        corr = (L - seg) / L * d
                        if i == 0: pts[i + 1] -= corr
                        else: pts[i] += corr * .5; pts[i + 1] -= corr * .5
                    # the cloak hangs behind the body: never forward of the anchor column
                    pts[1:, 0] = np.minimum(pts[1:, 0], pts[0, 0] - 4)
            if cyc == 7: out.append(pts.copy())
    return out


# ---------------------------------------------------------------- pose
def pose(k, cape=None):
    t = k / N
    P = np.array([0.0, BASE_HIP + bob(t)])                       # pelvis
    lean = LEAN + LEAN_DOWN * max(0.0, -bob(t) / BOB)
    twist = math.cos(2 * math.pi * t)                            # +1 when the near leg is forward
    J = dict(pelvis=P)
    legs = {}
    for side, ph, hx in (('near', 0.0, -HIP_SPREAD), ('far', 0.5, HIP_SPREAD)):
        hip = P + np.array([hx + 7 * (twist if side == 'near' else -twist), 0.0])
        ankle, fa, planted = foot(t - ph)
        ankle = ankle + np.array([hx, 0.0])          # each foot tracks under its own hip
        knee, ankle_r, over = ik(hip, ankle, THIGH, SHIN)
        heel = ankle_r + rot(np.array([-HEEL, -ANKLE_H]), fa)
        ball = ankle_r + rot(np.array([0.7 * TOE, -ANKLE_H]), fa)
        toe_a = 0.0 if (planted and fa < 0) else fa          # the toes stay flat while the heel rolls up
        toe = ball + rot(np.array([0.3 * TOE, 0.0]), toe_a)
        legs[side] = dict(hip=hip, knee=knee, ankle=ankle_r, heel=heel, ball=ball, toe=toe, toeAngle=toe_a, footAngle=fa, planted=planted, overreach=over)
    neck = P + rot(np.array([0.0, TORSO_LEN]), lean)
    J['neck'] = neck
    # head: rides the neck, nods a frame late, stays near level
    nod = HEAD_NOD * vbob(t, 1)
    head = neck + rot(np.array([HEAD_FWD, HEAD_ABOVE_NECK]), lean * 0.3) + np.array([0.0, 4.0 * vbob(t, 1)])
    arms = {}
    for side, sx, sy, sign in (('near', -58 * S, -40 * S, -1), ('far', 42 * S, -50 * S, 1)):
        sh = neck + rot(np.array([sx + sign * 10 * twist * -1, sy]), lean)
        swing = -ARM_SWING * math.cos(2 * math.pi * (t - ARM_LAG / N)) * (1 if side == 'near' else -1)
        ua = -90 + swing + lean
        elbow = sh + vec(ua, UPPER_ARM)
        bend = 14 + 22 * max(0.0, swing / ARM_SWING)              # elbows bend more on the forward swing
        fa_ = ua + bend
        wrist = elbow + vec(fa_, FOREARM)
        hand = wrist + vec(fa_ + 6, HAND)
        arms[side] = dict(shoulder=sh, elbow=elbow, wrist=wrist, handTip=hand, swing=round(swing, 2))
    ears = {}
    for side in ('far', 'near'):
        base = head + np.array([-0.36 * HEAD_W, 0.22 * HEAD_H])
        # The ears trail the head: while it drops they lift (angle falls, tip up), while it rises they droop.
        a = EAR_REST[side] + nod + EAR_GAIN[side] * vbob(t, EAR_LAG[side])
        a_tip = a + 0.6 * EAR_GAIN[side] * (vbob(t, EAR_LAG[side] + 1) - vbob(t, EAR_LAG[side]))
        ears[side] = dict(base=base, angle=round(a, 2), tipAngle=round(a_tip, 2))
    belt = P + rot(np.array([0.0, 135 * S]), lean)
    pouch_a = 8.0 * math.sin(2 * math.pi * (t - 2 / N)) - 6.0 * vbob(t, 2)
    J.update(head=head, belt=belt, cape_anchor=neck + rot(np.array([-46 * S, -26 * S]), lean))
    return dict(t=t, lean=lean, nod=nod, legs=legs, arms=arms, ears=ears, joints=J, pouchAngle=pouch_a,
                skirtLift=-10.0 * vbob(t, 1), cape=cape[k] if cape is not None else None)


# ---------------------------------------------------------------- drawing
COLOURS = dict(cape=(95, 140, 80), ear_far=(150, 190, 110), ear_near=(170, 210, 120), arm_far=(120, 80, 160),
               leg_far=(60, 110, 200), leg_near=(240, 170, 40), skirt=(205, 190, 140), torso=(220, 200, 150),
               mantle=(70, 120, 60), pouch=(150, 90, 50), arm_near=(230, 90, 60), head=(240, 228, 200))
TONES = dict(cape=(125, 117, 68), ear_far=(144, 132, 69), ear_near=(150, 138, 74), arm_far=(170, 125, 92),
             leg_far=(170, 125, 92), leg_near=(183, 135, 101), skirt=(236, 212, 188), torso=(242, 218, 194),
             mantle=(122, 116, 62), pouch=(145, 102, 70), arm_near=(183, 135, 101), head=(240, 216, 190))
ORDER = ['cape', 'ear_far', 'ear_near', 'arm_far', 'leg_far', 'leg_near', 'skirt', 'torso', 'mantle', 'pouch', 'arm_near', 'head']
CORE = ['arm_far', 'leg_far', 'leg_near', 'skirt', 'torso', 'mantle', 'pouch', 'arm_near', 'head']
FREE = {'cape': 55, 'ear_far': 40, 'ear_near': 40}          # px of freedom for follow-through parts


def capsule(dr, a, b, w, col):
    a, b = px(a), px(b)
    dr.line([a, b], fill=col, width=int(round(w)))
    for p in (a, b):
        dr.ellipse([p[0] - w / 2, p[1] - w / 2, p[0] + w / 2, p[1] + w / 2], fill=col)


def taper(dr, a, b, wa, wb, col):
    a, b = np.array(a), np.array(b)
    d = b - a; d = d / np.linalg.norm(d); n = np.array([-d[1], d[0]])
    poly = [a + n * wa / 2, b + n * wb / 2, b - n * wb / 2, a - n * wa / 2]
    dr.polygon([px(p) for p in poly], fill=col)
    for p, w in ((a, wa), (b, wb)):
        q = px(p); dr.ellipse([q[0] - w / 2, q[1] - w / 2, q[0] + w / 2, q[1] + w / 2], fill=col)


def leaf(base, a_base, a_tip, length, width):
    """Leaf polygon from base, bending from a_base to a_tip along its length."""
    pts_l, pts_r = [], []
    p = np.array(base, float)
    steps = 14
    for i in range(steps + 1):
        s = i / steps
        a = a_base + (a_tip - a_base) * s * s
        w = width * math.sin(math.pi * min(1.0, s * 1.05)) ** 0.8 * (1 - 0.25 * s)
        n = vec(a + 90)
        pts_l.append(p + n * w / 2); pts_r.append(p - n * w / 2)
        p = p + vec(a, length / steps)
    return pts_l + pts_r[::-1]


def draw_part(dr, part, P, col):
    L, A, E, J = P['legs'], P['arms'], P['ears'], P['joints']
    if part == 'cape':
        c = P['cape']; n = len(c)
        lw = [130 * S + (330 * S - 130 * S) * (i / (n - 1)) ** 0.8 for i in range(n)]
        left, right = [], []
        for i in range(n):
            d = c[min(i + 1, n - 1)] - c[max(i - 1, 0)]; d = d / np.linalg.norm(d); nrm = np.array([-d[1], d[0]])
            right.append(c[i] - nrm * lw[i] * 0.30); left.append(c[i] + nrm * lw[i] * 0.70)   # trails behind (left)
        hem = []
        a, b = left[-1], right[-1]
        for j in range(1, 6):          # five shallow lobes along the hem
            m = a + (b - a) * (j - .5) / 5
            dn = c[-1] - c[-2]; dn = dn / np.linalg.norm(dn)
            hem.append(m + dn * 14 * S)
        poly = right + [b] + hem[::-1] + [a] + left[::-1]
        dr.polygon([px(p) for p in poly], fill=col)
    elif part.startswith('ear'):
        e = E[part.split('_')[1]]
        dr.polygon([px(p) for p in leaf(e['base'], e['angle'], e['tipAngle'], EAR_LEN, 0.27 * EAR_LEN)], fill=col)
    elif part.startswith('arm'):
        a = A[part.split('_')[1]]
        taper(dr, a['shoulder'], a['elbow'], WIDTH['upper_arm'], WIDTH['upper_arm'] * .9, col)
        taper(dr, a['elbow'], a['wrist'], WIDTH['forearm'], WIDTH['forearm'] * .95, col)
        hand_c = a['wrist'] + (a['handTip'] - a['wrist']) * .5
        q = px(hand_c); r = WIDTH['hand'] / 2
        dr.ellipse([q[0] - r, q[1] - r * 1.15, q[0] + r, q[1] + r * 1.15], fill=col)
        capsule(dr, a['wrist'], a['handTip'], WIDTH['hand'] * .55, col)
    elif part.startswith('leg'):
        l = L[part.split('_')[1]]
        taper(dr, l['hip'], l['knee'], WIDTH['thigh'], WIDTH['thigh'] * .8, col)
        taper(dr, l['knee'], l['ankle'], WIDTH['shin'], WIDTH['shin'] * .9, col)
        fa, ta = l['footAngle'], l['toeAngle']
        back = l['ankle'] + rot(np.array([-0.55 * HEEL, 0.25 * ANKLE_H]), fa)
        front = l['ankle'] + rot(np.array([0.35 * TOE, -0.05 * ANKLE_H]), fa)
        toe_top = l['toe'] + rot(np.array([-0.15 * TOE, 0.32 * ANKLE_H]), ta)
        dr.polygon([px(p) for p in (back, l['heel'], l['ball'], l['toe'], toe_top, front)], fill=col)
        for i in range(3):        # three root toes, rounded
            c = l['toe'] + rot(np.array([-8 * S - 14 * S * i, 15 * S]), ta)
            q = px(c); r = 15 * S
            dr.ellipse([q[0] - r, q[1] - r, q[0] + r, q[1] + r], fill=col)
    elif part == 'torso':
        Pv, nk, lean = J['pelvis'], J['neck'], P['lean']
        pts = [Pv + rot(np.array([x, y]), lean) for x, y in ((-70 * S, -20 * S), (75 * S, -20 * S), (82 * S, 250 * S), (58 * S, TORSO_LEN - 10), (-55 * S, TORSO_LEN - 10), (-78 * S, 250 * S))]
        dr.polygon([px(p) for p in pts], fill=col)
    elif part == 'skirt':
        Pv, lean, lift = J['pelvis'], P['lean'], P['skirtLift']
        fx = (L['near']['knee'][0] + L['far']['knee'][0]) / 2 - Pv[0]
        top = [Pv + rot(np.array([x, 135 * S]), lean) for x in (-82 * S, 84 * S)]
        hem = []
        for i in range(9):
            s = i / 8
            x = -105 * S + 215 * S * s + 0.18 * fx
            y = (-62 * S if i % 2 == 0 else -36 * S) + lift * (0.4 + 0.6 * (1 - s))
            hem.append(Pv + rot(np.array([x, y]), lean))
        dr.polygon([px(p) for p in [top[0]] + hem + [top[1]]], fill=col)
    elif part == 'mantle':
        nk, lean = J['neck'], P['lean']
        pts = []
        for i in range(19):
            s = i / 18; a = 200 - 220 * s
            r = (170 * S if i % 2 == 0 else 138 * S) * (0.9 + 0.1 * math.sin(math.pi * s))
            p = np.array([math.cos(math.radians(a)) * r, -abs(math.sin(math.radians(a))) * 150 * S * (1 if i % 2 == 0 else .82) - 10 * S])
            pts.append(nk + rot(p, lean))
        pts += [nk + rot(np.array([120 * S, 30 * S]), lean), nk + rot(np.array([-130 * S, 30 * S]), lean)]
        dr.polygon([px(p) for p in pts], fill=col)
    elif part == 'pouch':
        b = J['belt'] + rot(np.array([-58 * S, -6 * S]), P['lean'])
        c = b + vec(-90 + P['pouchAngle'], 52 * S)
        q = px(c); r = 34 * S
        dr.ellipse([q[0] - r, q[1] - r * 1.1, q[0] + r, q[1] + r * 1.1], fill=col)
        capsule(dr, b, c, 14 * S, col)
    elif part == 'head':
        c = px(J['head'])
        dr.ellipse([c[0] - HEAD_W / 2, c[1] - HEAD_H / 2, c[0] + HEAD_W / 2, c[1] + HEAD_H / 2], fill=col)
        # crest: leaves behind the dome on the back of the head
        for a, l in ((95, 150 * S), (125, 140 * S), (155, 132 * S), (185, 122 * S), (215, 112 * S)):
            pts = leaf(J['head'] + vec(a + P['nod'], 70 * S), a + P['nod'], a + P['nod'] + 12, l - 70 * S + 40 * S, 34 * S)
            dr.polygon([px(p) for p in pts], fill=col)


def render(P, parts=None, palette=COLOURS):
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    dr = ImageDraw.Draw(img)
    for part in ORDER:
        if parts is None or part in parts:
            draw_part(dr, part, P, palette[part] + (255,))
    return img


def main():
    out = sys.argv[1]
    for sub in ('blockout', 'masks', 'reference'):
        os.makedirs(os.path.join(out, sub), exist_ok=True)
    anchors = [pose(k)['joints']['cape_anchor'] for k in range(N)]
    cape = simulate_cape(anchors)
    for k in range(N):
        c = cape[k]; n = len(c)
        for i in range(1, n):
            s_ = i / (n - 1)
            d = c[i] - c[i - 1]; d = d / np.linalg.norm(d); nrm = np.array([-d[1], d[0]])
            c[i] = c[i] + nrm * CAPE_WAVE * s_ ** 1.5 * math.sin(2 * math.pi * (2 * k / N - CAPE_WAVE_DELAY * s_))
    poses = [pose(k, cape) for k in range(N)]
    font = ImageFont.load_default()
    frames = []
    for k, P in enumerate(poses):
        nm = 'Qori_Walk_%02d' % (k + 1)
        img = render(P); img.save(os.path.join(out, 'blockout', nm + '_Blockout.png'))
        render(P, palette=TONES).save(os.path.join(out, 'blockout', nm + '_Blockout_Toned.png'))
        masks = {}
        for part in ORDER:
            a = np.array(render(P, [part]))[:, :, 3]
            masks[part] = a
            Image.fromarray(a).save(os.path.join(out, 'masks', '%s_Mask_%s.png' % (nm, part)))
        allm = np.array(img)[:, :, 3]
        core = np.zeros_like(allm)
        for part in CORE: core = np.maximum(core, masks[part])
        Image.fromarray(allm).save(os.path.join(out, 'masks', nm + '_Mask_All.png'))
        Image.fromarray(core).save(os.path.join(out, 'masks', nm + '_Mask_Core.png'))
        L, A, E, J = P['legs'], P['arms'], P['ears'], P['joints']
        R = lambda p: [round(v, 1) for v in px(p)]
        head_c = px(J['head'])
        rec = dict(frame=k + 1, file=nm + '.png', t=round(P['t'], 4),
                   pelvis=R(J['pelvis']), neck=R(J['neck']), head=R(J['head']),
                   headTopRow=round(head_c[1] - HEAD_H / 2, 1), headSize=[round(HEAD_W, 1), round(HEAD_H, 1)],
                   lean=round(P['lean'], 2), nod=round(P['nod'], 2),
                   legs={s: dict(hip=R(l['hip']), knee=R(l['knee']), ankle=R(l['ankle']), heel=R(l['heel']), toe=R(l['toe']),
                                 footAngle=round(l['footAngle'], 1), planted=l['planted'], sole=round(max(px(l['heel'])[1], px(l['ball'])[1], px(l['toe'])[1]), 1))
                         for s, l in L.items()},
                   arms={s: dict(shoulder=R(a['shoulder']), elbow=R(a['elbow']), wrist=R(a['wrist']), handTip=R(a['handTip']), swing=a['swing']) for s, a in A.items()},
                   ears={s: dict(base=R(e['base']), angle=e['angle'], tipAngle=e['tipAngle']) for s, e in E.items()},
                   capeChain=[R(p) for p in P['cape']], pouchAngle=round(P['pouchAngle'], 2), skirtLift=round(P['skirtLift'], 2),
                   areaPx={p: int((m > 128).sum()) for p, m in masks.items()}, areaAll=int((allm > 128).sum()))
        frames.append(rec)
        ann = Image.new('RGBA', (W, H), (205, 208, 210, 255)); ann.alpha_composite(img)
        dr = ImageDraw.Draw(ann)
        dr.line([(0, FOOT_LINE), (W, FOOT_LINE)], fill=(220, 0, 0, 255), width=2)
        dr.line([(PIVOT_X, 0), (PIVOT_X, H)], fill=(0, 190, 255, 255), width=1)
        dr.line([(0, rec['headTopRow']), (W, rec['headTopRow'])], fill=(0, 150, 0, 255), width=1)
        for s in ('near', 'far'):
            l = rec['legs'][s]; a = rec['arms'][s]
            pts = [l['hip'], l['knee'], l['ankle']]
            dr.line(pts, fill=(0, 0, 0, 255), width=3)
            dr.line([a['shoulder'], a['elbow'], a['wrist'], a['handTip']], fill=(0, 0, 0, 255), width=3)
            for p in pts + [a['shoulder'], a['elbow'], a['wrist']]:
                dr.ellipse([p[0] - 6, p[1] - 6, p[0] + 6, p[1] + 6], outline=(0, 0, 0, 255), width=2)
        dr.line([rec['pelvis'], rec['neck'], rec['head']], fill=(0, 0, 0, 255), width=3)
        dr.line(rec['capeChain'], fill=(20, 60, 20, 255), width=2)
        dr.text((14, 14), '%s   t = %.3f   %s' % (nm, P['t'], KEYS.get(k + 1, '')), fill=(0, 0, 0, 255), font=font)
        ann.convert('RGB').save(os.path.join(out, 'blockout', nm + '_Annotated.png'))

    def sheet(suffix, file, scale=4):
        cols = 8; w, h = W // scale, H // scale
        s = Image.new('RGBA', (cols * w, 2 * h), (150, 170, 175, 255)); dr = ImageDraw.Draw(s)
        for k in range(N):
            im = Image.open(os.path.join(out, 'blockout', 'Qori_Walk_%02d_%s.png' % (k + 1, suffix))).convert('RGBA')
            s.alpha_composite(im.resize((w, h), Image.LANCZOS), ((k % cols) * w, (k // cols) * h))
            dr.text(((k % cols) * w + 6, (k // cols) * h + 6), '%02d %s' % (k + 1, KEYS.get(k + 1, '')), fill=(0, 0, 0, 255), font=font)
            y = (k // cols) * h + FOOT_LINE // scale; dr.line([((k % cols) * w, y), ((k % cols + 1) * w, y)], fill=(200, 0, 0, 255))
        s.convert('RGB').save(os.path.join(out, file))
    sheet('Blockout', 'Batch27_Blockout_Sheet.png'); sheet('Annotated', 'Batch27_Annotated_Sheet.png'); sheet('Blockout_Toned', 'Batch27_Blockout_Toned_Sheet.png')
    meta = dict(canvas=[W, H], footLineRow=FOOT_LINE, pivotColumn=PIVOT_X, frameCount=N, cycleSeconds=CYCLE_S,
                frameMs=round(CYCLE_S * 1000 / N, 2), stridePxPerCycle=round(2 * STEP, 1), groundPxPerFrame=round(2 * STEP / N, 2),
                modelScale=S, headSize=[round(HEAD_W, 1), round(HEAD_H, 1)], earLength=round(EAR_LEN, 1),
                order=ORDER, core=CORE, freePx=FREE, colours=COLOURS, tones=TONES, keys=KEYS,
                direction=dict(bobPx=round(2 * BOB, 1), lean=LEAN, leanDown=LEAN_DOWN, armSwing=ARM_SWING, armLagFrames=ARM_LAG,
                               earGain=EAR_GAIN, earLagFrames=EAR_LAG, headNod=HEAD_NOD, stance=STANCE, capeTrail=CAPE_TRAIL, capeWave=CAPE_WAVE, capeWaveDelay=CAPE_WAVE_DELAY),
                frames=frames, source='Codex_v2_full/Batch19b/Concept_Qori_v3b_Turnaround.png (large figure), x%.2f' % S)
    json.dump(meta, open(os.path.join(out, 'batch27_blockout.json'), 'w'), indent=1)
    print('block-out written to', out)


KEYS = {1: 'CONTACT near leg fwd', 3: 'DOWN', 5: 'PASSING', 7: 'UP', 9: 'CONTACT far leg fwd', 11: 'DOWN', 13: 'PASSING', 15: 'UP'}

if __name__ == '__main__':
    main()
