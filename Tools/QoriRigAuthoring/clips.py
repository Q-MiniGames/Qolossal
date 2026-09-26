import math
import numpy as np
import anim, rig
from anim import ss, lerp, ANKLE_Y

B = rig.BONES
TAU = 2*math.pi
HAND_N = B['HandNear']['pos'].copy()
HAND_F = B['HandFar']['pos'].copy()

def around(p, dx=0.0, dy=0.0, c=None):
    """Point offset from rest, following the body translation in controls c."""
    bx = c.get('body_x', 0) if c else 0; by = c.get('body_y', 0) if c else 0
    return (p[0]+dx+bx, p[1]+dy+by)

def wdir(a): return np.array([math.cos(math.radians(a)), math.sin(math.radians(a))])

# ----------------------------------------------------------------- idle
IDLE_T = 2.6
def idle(t):
    p = TAU*t/IDLE_T
    c = dict(body_y=-12 + 3.0*math.sin(p), body_rot=-1.5,
             torso_sy=1 + .012*math.sin(p), torso_rot=-2.5 + .9*math.sin(p-.6),
             head_rot=3 + 1.8*math.sin(p-1.1), skirt_rot=.9*math.sin(p-1.5),
             footN=(58, ANKLE_Y, 0), footF=(-42, ANKLE_Y, 0),
             cape_u=-8 + 1.5*math.sin(p-1.8), cape_l=-4 + 2.2*math.sin(p-2.5), cape_w=.25*math.sin(2*p-1))
    # The camera-side hand holds the blade low and forward (as in the walk); the weapon arm
    # hangs relaxed. QoriAnimator moves the blade to the far grip in Idle, Walk and Run.
    w = -13 + 2.5*math.sin(p-.9)
    c['weapon'] = w
    c['weapon_far'] = w
    c['handF'] = around(HAND_F, 6, 10 + 2.5*math.sin(p-.4), c)
    c['handN'] = around(HAND_N, -10, 26 + 2*math.sin(p-.7), c)
    return c

# ----------------------------------------------------------------- gait
def leg_target(phase, D, A, lift, xo, toe_off=-35, strike=14, kick=0.0):
    s = phase % 1.0
    if s < D:
        u = s/D
        x = xo + A*(1-2*u)
        heel = ss(.6, 1, u)
        y = ANKLE_Y + 16*heel
        a = strike*(1-ss(0, .28, u)) + toe_off*heel
    else:
        u = (s-D)/(1-D)
        e = u*u*(3-2*u)
        e = e + .10*math.sin(math.pi*e)*e          # slight reach/overshoot before strike
        x = xo - A + 2*A*min(e, 1.04)
        y = ANKLE_Y + 16*(1-ss(0, .15, u)) + lift*math.sin(math.pi*u**.75) + kick*math.sin(math.pi*min(u*1.8, 1))
        a = lerp(toe_off, strike, ss(.15, .9, u)) - 10*math.sin(math.pi*u)
    return (x, y, a)

def make_gait(T, D, A, lift, xo, lean, bob, base_y, kick, arm_swing, weapon, twist=3.0, skirt=5.0, head=6.0, cape=-40.0,
              far_carry=False):
    def f(t):
        ph = (t/T) % 1.0
        bobv = -bob*math.cos(TAU*2*(ph - D/2))
        c = dict(body_y=base_y + bobv, body_x=3*math.sin(TAU*2*(ph-D/2)), body_rot=-lean,
                 torso_rot=-2 + twist*math.sin(TAU*ph), torso_sy=1 - .015*math.cos(TAU*2*(ph-D/2)),
                 head_rot=head + 2.2*math.cos(TAU*2*ph - 1.6),
                 skirt_rot=-skirt*.6 + skirt*math.sin(TAU*2*ph - 1.9),
                 skirt_sy=1 + .03*math.sin(TAU*2*ph - 1.9),
                 footN=leg_target(ph, D, A, lift, xo, kick=kick),
                 footF=leg_target(ph+.5, D, A, lift, xo-12, kick=kick))
        sw = math.cos(TAU*ph)
        c['handN'] = around(HAND_N, -8 - arm_swing*sw, 18 + 10*math.sin(TAU*ph), c)
        c['weapon'] = weapon + 7*sw
        c['handF'] = around(HAND_F, 30 + 1.3*arm_swing*sw, 40 + 14*abs(math.sin(TAU*ph)), c)
        c['elbowF'] = -1
        if far_carry:
            # The camera-side hand carries the blade (low, trailing), the weapon arm swings free behind.
            c['handF'] = around(HAND_F, -4 - arm_swing*sw, 22 + 10*math.sin(TAU*ph), c)
            c['weapon_far'] = weapon + 7*sw
            c['handN'] = around(HAND_N, 30 + 1.3*arm_swing*sw, 40 + 14*abs(math.sin(TAU*ph)), c)
        c['cape_u'] = cape + 4*math.sin(TAU*2*ph - 2.2)
        c['cape_l'] = cape*.5 + 7*math.sin(TAU*2*ph - 2.9)
        c['cape_w'] = math.sin(TAU*2*ph - 2.5)
        return c
    return f

RUN_T = 0.40
run = make_gait(T=RUN_T, D=.36, A=150, lift=95, xo=22, lean=9, bob=11, base_y=-26, kick=40,
                arm_swing=26, weapon=142, far_carry=True)
WALK_T = 0.62
walk = make_gait(T=WALK_T, D=.58, A=95, lift=46, xo=18, lean=4, bob=6, base_y=-14, kick=0,
                 arm_swing=14, weapon=-17, twist=2, skirt=3, head=4, cape=-16, far_carry=True)

CLIPS = {}
def build():
    CLIPS['Idle'] = anim.bake(idle, IDLE_T, True)
    CLIPS['Run'] = anim.bake(run, RUN_T, True)
    CLIPS['Walk'] = anim.bake(walk, WALK_T, True)
    return CLIPS

# ----------------------------------------------------------------- keypose clips
from scipy.interpolate import PchipInterpolator, CubicSpline

def keyed(keys, loop=False):
    """keys: [(t, controls)]; every numeric/tuple control is interpolated with a
    monotone cubic (continuous velocity, no overshoot) -> fluid, not robotic."""
    ts = np.array([k[0] for k in keys])
    names = set().union(*[k[1].keys() for k in keys])
    interp = {}
    for n in names:
        vals = []
        last = None
        for _, c in keys:
            v = c.get(n, last)
            if v is None: v = anim.DEFAULT[n]
            last = v; vals.append(v)
        if isinstance(vals[0], (int, float)) and not isinstance(vals[0], bool):
            arr = np.array(vals, float)
            interp[n] = (CubicSpline(ts, arr, bc_type='periodic') if loop else PchipInterpolator(ts, arr))
        elif isinstance(vals[0], (tuple, list, np.ndarray)):
            arr = np.array([list(v) for v in vals], float)
            interp[n] = (CubicSpline(ts, arr, axis=0, bc_type='periodic') if loop else PchipInterpolator(ts, arr, axis=0))
        else:
            interp[n] = vals  # ints like elbow sign: step
    def f(t):
        c = {}
        for n, it in interp.items():
            if isinstance(it, list):
                i = max(0, np.searchsorted(ts, t, side='right')-1); c[n] = it[i]
            else:
                v = it(min(max(t, ts[0]), ts[-1]))
                c[n] = tuple(v) if np.ndim(v) else float(v)
        return c
    return f

READY = dict(body_x=0, body_y=-12, body_rot=-1.5, torso_rot=-2.5, head_rot=3, skirt_rot=0,
             footN=(58, ANKLE_Y, 0), footF=(-42, ANKLE_Y, 0), weapon=-14,
             handN=(760, -826), handF=(716, -812), torso_sy=1.0, cape_u=-8, cape_l=-4, cape_w=0)
def R(**kw): return {**READY, **kw}

# Attack clips are 1.0s long and scrubbed by combat phase:
#   startup -> 0.00..0.30, active -> 0.30..0.55, recovery -> 0.55..1.00
# Attack design notes (applies to every swing below)
#  - 0.00-0.22  fast anticipation into a coiled pose (squash, weight back)
#  - 0.22-0.30  tiny "tension creep": the pose holds, so the strike reads
#  - 0.30-0.40  the strike: ~80% of the arc happens here (front-loaded), body lunges
#  - 0.40-0.55  follow-through overshoot past the target
#  - 0.55-0.70  hold the finished pose (this is what makes a hit feel heavy)
#  - 0.70-1.00  ease back to ready
# Combo swings chain: each one starts where the previous one's hold ends.

LOW_HOLD = dict(handN=(846, -896), weapon=-80, torso_rot=-18, body_x=36, body_y=-40, body_rot=-8)

# 1. Opener: big overhead diagonal cut.
FRONT_KEYS = ([
    (0.00, R()),
    (0.10, R(body_x=-10, body_y=-26, torso_rot=8, torso_sy=.975, head_rot=-1, handN=(690, -640), weapon=110,
             handF=(760, -760), skirt_rot=3, cape_u=0, cape_l=4)),
    (0.24, R(body_x=-22, body_y=-38, torso_rot=15, body_rot=3, torso_sy=.955, head_rot=-5, handN=(636, -560), weapon=162,
             handF=(800, -742), skirt_rot=6, cape_u=6, cape_l=9, footN=(92, ANKLE_Y, 6), footF=(-72, ANKLE_Y, -8))),
    (0.30, R(body_x=-24, body_y=-40, torso_rot=16, body_rot=3, torso_sy=.95, head_rot=-6, handN=(632, -552), weapon=168,
             handF=(804, -740), skirt_rot=6, cape_u=7, cape_l=10, footN=(92, ANKLE_Y, 6), footF=(-72, ANKLE_Y, -8))),
    (0.35, R(body_x=6, body_y=-26, torso_rot=-2, body_rot=-3, torso_sy=1.035, head_rot=4, handN=(830, -566), weapon=64,
             handF=(700, -770), skirt_rot=0, cape_u=-10, cape_l=12, cape_w=.8, footN=(120, ANKLE_Y, 0), footF=(-84, ANKLE_Y, -16))),
    (0.40, R(body_x=34, body_y=-36, torso_rot=-16, body_rot=-8, torso_sy=1.02, head_rot=8, handN=(908, -724), weapon=-14,
             handF=(640, -790), skirt_rot=-7, cape_u=-30, cape_l=-4, cape_w=.4, footN=(126, ANKLE_Y, 0), footF=(-88, ANKLE_Y, -20))),
    (0.55, R(**LOW_HOLD, torso_sy=.975, head_rot=9, handF=(630, -800), skirt_rot=-9, cape_u=-42, cape_l=-26, cape_w=-.4,
             footN=(126, ANKLE_Y, 0), footF=(-90, ANKLE_Y, -22))),
    (0.70, R(**{**LOW_HOLD, 'handN': (842, -902), 'weapon': -84}, head_rot=8, handF=(640, -805), skirt_rot=-4,
             cape_u=-24, cape_l=-26, footN=(124, ANKLE_Y, 0), footF=(-88, ANKLE_Y, -20))),
    (1.00, R()),
])

# 2. Rising backhand: from the low hold, sweep up and forward.
SLASH2_KEYS = ([
    (0.00, R(**LOW_HOLD, head_rot=8, footN=(124, ANKLE_Y, 0), footF=(-88, ANKLE_Y, -20), cape_u=-24, cape_l=-20)),
    (0.22, R(handN=(796, -930), weapon=-128, torso_rot=-6, body_x=24, body_y=-48, body_rot=-4, torso_sy=.955, head_rot=2,
             handF=(700, -760), footN=(110, ANKLE_Y, 0), footF=(-80, ANKLE_Y, -12), cape_u=-6, cape_l=6, skirt_rot=-2)),
    (0.30, R(handN=(792, -934), weapon=-134, torso_rot=-5, body_x=22, body_y=-50, body_rot=-4, torso_sy=.95, head_rot=2,
             handF=(700, -764), footN=(110, ANKLE_Y, 0), footF=(-80, ANKLE_Y, -12), cape_u=-4, cape_l=8, skirt_rot=-2)),
    (0.38, R(handN=(922, -700), weapon=28, torso_rot=10, body_x=46, body_y=-24, body_rot=-2, torso_sy=1.045, head_rot=12,
             handF=(640, -800), footN=(128, ANKLE_Y, 0), footF=(-92, ANKLE_Y, -24), cape_u=-26, cape_l=-6, cape_w=.7, skirt_rot=4)),
    (0.55, R(handN=(842, -566), weapon=78, torso_rot=15, body_x=44, body_y=-20, body_rot=0, torso_sy=1.02, head_rot=14,
             handF=(650, -790), footN=(128, ANKLE_Y, 0), footF=(-92, ANKLE_Y, -22), cape_u=-30, cape_l=-22, cape_w=-.3, skirt_rot=5)),
    (0.68, R(handN=(832, -560), weapon=84, torso_rot=15, body_x=42, body_y=-22, torso_sy=1.0, head_rot=12,
             handF=(655, -790), footN=(126, ANKLE_Y, 0), footF=(-90, ANKLE_Y, -20), cape_u=-18, cape_l=-18)),
    (1.00, R()),
])

HIGH_HOLD = dict(handN=(832, -560), weapon=84, torso_rot=15, body_x=42, body_y=-22)

# 3. Return cut: from high, a fast diagonal back down.
SLASH3_KEYS = ([
    (0.00, R(**HIGH_HOLD, head_rot=12, handF=(655, -790), footN=(126, ANKLE_Y, 0), footF=(-90, ANKLE_Y, -20), cape_u=-18, cape_l=-18)),
    (0.22, R(handN=(780, -524), weapon=122, torso_rot=19, body_x=34, body_y=-30, body_rot=2, torso_sy=.96, head_rot=4,
             handF=(760, -760), footN=(120, ANKLE_Y, 0), footF=(-86, ANKLE_Y, -14), cape_u=2, cape_l=6)),
    (0.30, R(handN=(776, -520), weapon=126, torso_rot=20, body_x=32, body_y=-32, body_rot=2, torso_sy=.955, head_rot=4,
             handF=(762, -758), footN=(120, ANKLE_Y, 0), footF=(-86, ANKLE_Y, -14), cape_u=3, cape_l=8)),
    (0.38, R(handN=(934, -760), weapon=-22, torso_rot=-16, body_x=56, body_y=-38, body_rot=-9, torso_sy=1.03, head_rot=9,
             handF=(630, -796), footN=(138, ANKLE_Y, 0), footF=(-96, ANKLE_Y, -22), cape_u=-34, cape_l=-6, cape_w=.6, skirt_rot=-8)),
    (0.55, R(handN=(872, -900), weapon=-74, torso_rot=-20, body_x=58, body_y=-44, body_rot=-9, torso_sy=.97, head_rot=9,
             handF=(626, -802), footN=(138, ANKLE_Y, 0), footF=(-98, ANKLE_Y, -22), cape_u=-44, cape_l=-28, cape_w=-.4, skirt_rot=-9)),
    (0.68, R(handN=(868, -904), weapon=-78, torso_rot=-19, body_x=56, body_y=-42, body_rot=-8, head_rot=8,
             handF=(632, -804), footN=(136, ANKLE_Y, 0), footF=(-96, ANKLE_Y, -20), cape_u=-26, cape_l=-26)),
    (1.00, R()),
])

# 4. Finisher: coil back, then a long lunging thrust.
SLASH4_KEYS = ([
    (0.00, R(handN=(868, -904), weapon=-78, torso_rot=-19, body_x=56, body_y=-42, body_rot=-8, head_rot=8,
             handF=(632, -804), footN=(136, ANKLE_Y, 0), footF=(-96, ANKLE_Y, -20), cape_u=-26, cape_l=-26)),
    (0.24, R(handN=(640, -772), weapon=2, torso_rot=14, body_x=-18, body_y=-44, body_rot=4, torso_sy=.945, head_rot=-2,
             handF=(808, -706), footN=(74, ANKLE_Y, 4), footF=(-70, ANKLE_Y, -6), cape_u=8, cape_l=10, skirt_rot=6)),
    (0.30, R(handN=(632, -768), weapon=3, torso_rot=15, body_x=-22, body_y=-46, body_rot=4, torso_sy=.94, head_rot=-2,
             handF=(812, -704), footN=(74, ANKLE_Y, 4), footF=(-70, ANKLE_Y, -6), cape_u=9, cape_l=11, skirt_rot=6)),
    (0.37, R(handN=(968, -748), weapon=0, torso_rot=-12, body_x=66, body_y=-46, body_rot=-11, torso_sy=1.05, head_rot=6,
             handF=(616, -800), footN=(160, ANKLE_Y, 0), footF=(-104, ANKLE_Y, -26), cape_u=-46, cape_l=-16, cape_w=.8, skirt_rot=-10)),
    (0.55, R(handN=(972, -752), weapon=-2, torso_rot=-13, body_x=70, body_y=-48, body_rot=-11, torso_sy=1.02, head_rot=6,
             handF=(612, -804), footN=(160, ANKLE_Y, 0), footF=(-106, ANKLE_Y, -26), cape_u=-50, cape_l=-34, cape_w=-.5, skirt_rot=-10)),
    (0.72, R(handN=(900, -792), weapon=-10, torso_rot=-8, body_x=52, body_y=-40, body_rot=-7, head_rot=5,
             handF=(640, -800), footN=(150, ANKLE_Y, 0), footF=(-100, ANKLE_Y, -18), cape_u=-24, cape_l=-24)),
    (1.00, R()),
])

# Up: crouch low, spring up and carve an arc over the head.
UP_KEYS = ([
    (0.00, R()),
    (0.22, R(body_y=-54, torso_rot=-10, body_rot=-3, torso_sy=.95, head_rot=-4, handN=(806, -904), weapon=-62,
             handF=(730, -860), skirt_rot=-3, cape_u=-4, cape_l=4, footN=(64, ANKLE_Y, 0), footF=(-50, ANKLE_Y, 0))),
    (0.30, R(body_y=-56, torso_rot=-11, body_rot=-3, torso_sy=.945, head_rot=-4, handN=(810, -908), weapon=-58,
             handF=(732, -862), skirt_rot=-3, cape_u=-3, cape_l=5, footN=(64, ANKLE_Y, 0), footF=(-50, ANKLE_Y, 0))),
    (0.38, R(body_y=2, torso_rot=4, body_rot=2, torso_sy=1.055, head_rot=16, handN=(846, -548), weapon=82,
             handF=(690, -760), skirt_rot=2, cape_u=-12, cape_l=-2, cape_w=.6, footN=(54, ANKLE_Y+16, -16), footF=(-40, ANKLE_Y+20, -24))),
    (0.55, R(body_y=-6, torso_rot=12, body_rot=4, torso_sy=1.03, head_rot=16, handN=(716, -500), weapon=156,
             handF=(680, -720), skirt_rot=4, cape_u=-22, cape_l=-14, cape_w=-.3, footN=(54, ANKLE_Y+8, -8), footF=(-40, ANKLE_Y+10, -14))),
    (0.68, R(body_y=-16, torso_rot=10, body_rot=3, head_rot=12, handN=(708, -512), weapon=162, handF=(682, -730), cape_u=-14, cape_l=-12)),
    (1.00, R()),
])

attack_front = keyed(FRONT_KEYS)
attack_front2 = keyed(SLASH2_KEYS)
attack_front3 = keyed(SLASH3_KEYS)
attack_front4 = keyed(SLASH4_KEYS)
attack_up = keyed(UP_KEYS)
def airborne(keys, lift=1.0):
    out = []
    for t, c in keys:
        c = dict(c); c.pop('footN', None); c.pop('footF', None)
        c.update(footN=(46, ANKLE_Y+80*lift, -28), footF=(-34, ANKLE_Y+58*lift, -36))
        c['body_y'] = c.get('body_y', 0)*.3 + 6
        out.append((t, c))
    return out
attack_air_front = keyed(airborne(FRONT_KEYS))
attack_air_front2 = keyed(airborne(SLASH2_KEYS))
attack_air_front3 = keyed(airborne(SLASH3_KEYS))
attack_air_front4 = keyed(airborne(SLASH4_KEYS))
attack_air_up = keyed(airborne(UP_KEYS))

AIR_FEET = dict(footN=(50, ANKLE_Y+95, -25), footF=(-30, ANKLE_Y+70, -35))
attack_down = keyed([
    (0.00, R(**AIR_FEET, handN=(760, -800), weapon=-20)),
    (0.24, R(**AIR_FEET, body_y=14, torso_rot=8, torso_sy=.95, handN=(694, -548), weapon=104, handF=(760, -700),
             head_rot=-4, skirt_rot=5, cape_u=-8, cape_l=4)),
    (0.30, R(**AIR_FEET, body_y=16, torso_rot=8, torso_sy=.945, handN=(690, -544), weapon=100, handF=(762, -698),
             head_rot=-4, skirt_rot=5, cape_u=-6, cape_l=6)),
    (0.37, R(footN=(74, ANKLE_Y+160, -40), footF=(-6, ANKLE_Y+140, -46), body_y=24, torso_rot=-12, body_rot=-6, torso_sy=1.05,
             handN=(758, -928), weapon=-90, handF=(640, -700), head_rot=-16, skirt_rot=-7, skirt_sy=.9,
             cape_u=-64, cape_l=-42, cape_w=.9)),
    (0.55, R(footN=(74, ANKLE_Y+160, -40), footF=(-6, ANKLE_Y+140, -46), body_y=22, torso_rot=-13, body_rot=-6, torso_sy=1.03,
             handN=(754, -934), weapon=-93, handF=(636, -704), head_rot=-16, skirt_rot=-7, skirt_sy=.9,
             cape_u=-70, cape_l=-48, cape_w=-.6)),
    (0.72, R(**AIR_FEET, body_y=10, torso_rot=-7, handN=(766, -880), weapon=-74, handF=(700, -760), head_rot=-9, cape_u=-40, cape_l=-24)),
    (1.00, R(**AIR_FEET, handN=(760, -800), weapon=-20)),
])

# ----------------------------------------------------------------- air
RISE_T = 0.6
rise = keyed([
    (0.0, R(cape_u=2, cape_l=5, cape_w=-.3, body_y=6, body_rot=-4, torso_rot=-2, torso_sy=1.03, head_rot=10, skirt_rot=-6, skirt_sy=.94,
            footN=(62, ANKLE_Y+120, 22), footF=(-58, ANKLE_Y+24, -40), handN=(700, -700), weapon=150, handF=(660, -600))),
    (0.3, R(cape_u=3, cape_l=8, cape_w=.3, body_y=10, body_rot=-4, torso_rot=-3, torso_sy=1.035, head_rot=12, skirt_rot=-8, skirt_sy=.92,
            footN=(66, ANKLE_Y+128, 26), footF=(-62, ANKLE_Y+20, -44), handN=(700, -690), weapon=154, handF=(662, -590))),
    (0.6, R(cape_u=2, cape_l=5, cape_w=-.3, body_y=6, body_rot=-4, torso_rot=-2, torso_sy=1.03, head_rot=10, skirt_rot=-6, skirt_sy=.94,
            footN=(62, ANKLE_Y+120, 22), footF=(-58, ANKLE_Y+24, -40), handN=(700, -700), weapon=150, handF=(660, -600))),
], loop=True)

FALL_T = 0.5
fall = keyed([
    (0.0, R(cape_u=-72, cape_l=-38, cape_w=-1, body_y=0, body_rot=2, torso_rot=4, head_rot=-8, skirt_rot=4, skirt_sy=1.06, skirt_sx=1.05,
            footN=(40, ANKLE_Y+40, -30), footF=(-20, ANKLE_Y+62, -38), handN=(760, -640), weapon=150, handF=(560, -600), elbowF=1)),
    (0.25, R(cape_u=-78, cape_l=-30, cape_w=1, body_y=2, body_rot=2, torso_rot=5, head_rot=-9, skirt_rot=2, skirt_sy=1.1, skirt_sx=1.07,
            footN=(46, ANKLE_Y+46, -34), footF=(-26, ANKLE_Y+56, -34), handN=(764, -630), weapon=146, handF=(556, -588), elbowF=1)),
    (0.5, R(cape_u=-72, cape_l=-38, cape_w=-1, body_y=0, body_rot=2, torso_rot=4, head_rot=-8, skirt_rot=4, skirt_sy=1.06, skirt_sx=1.05,
            footN=(40, ANKLE_Y+40, -30), footF=(-20, ANKLE_Y+62, -38), handN=(760, -640), weapon=150, handF=(560, -600), elbowF=1)),
], loop=True)

LAND_T = 0.26
land = keyed([
    (0.00, R(cape_u=-66, cape_l=-36, body_y=-20, body_rot=-3, torso_rot=2, head_rot=-6, skirt_rot=3, skirt_sy=1.05, handN=(760, -700), weapon=120, handF=(600, -660),
             footN=(56, ANKLE_Y, 10), footF=(-44, ANKLE_Y, 0))),
    (0.06, R(cape_u=-30, cape_l=-6, cape_w=.5, body_y=-58, body_rot=-6, torso_rot=-8, torso_sy=.95, head_rot=-2, skirt_rot=-2, skirt_sy=.9, skirt_sx=1.06, handN=(780, -800), weapon=-10,
             handF=(700, -780), footN=(64, ANKLE_Y, 0), footF=(-52, ANKLE_Y, 0))),
    (0.16, R(cape_u=5, cape_l=9, cape_w=-.3, body_y=-26, body_rot=-3, torso_rot=-4, torso_sy=1.0, head_rot=4, skirt_rot=1, skirt_sy=1.02, weapon=-24)),
    (0.26, R()),
])

HANG_T = 1.4
def hang(t):
    p = TAU*t/HANG_T
    c = R(body_y=0, body_rot=4*math.sin(p), torso_rot=-4 + 2*math.sin(p-.5), head_rot=14 + 3*math.sin(p-1.0),
          skirt_rot=3*math.sin(p-1.3), skirt_sy=1.04,
          cape_u=-6 + 5*math.sin(p-1.6), cape_l=-2 + 6*math.sin(p-2.2), cape_w=.4*math.sin(2*p-1),
          footN=(20 + 14*math.sin(p-.8), ANKLE_Y+18, -40), footF=(-10 + 14*math.sin(p-1.1), ANKLE_Y+30, -44),
          handN=(846 + 4*math.sin(p-.3), -486), elbowN=-1, handF=(650 + 10*math.sin(p-.9), -790), elbowF=-1, weapon=92)
    return c

# ================================================================= other weapons
# Same timeline layout as the sword (0-.30 wind-up, .30-.55 hit, .55-1 recovery),
# but each weapon has its own body language:
#   Mace  - heavy: long lift, full-body smash into the ground, big squash, long hold
#   Spear - reach: pull back, long straight lunge, little arc (hits along a line)
#   Whip  - snap: the hand leads, then yanks back at the crack so the lash cracks forward
#   Sling - cock overhead while aiming, whip the arm through on release

# ---- Seedpod Mace
MACE_FRONT = [
    (0.00, R()),
    (0.14, R(handN=(700, -650), weapon=118, torso_rot=10, body_x=-10, body_y=-22, torso_sy=.98, handF=(690, -670),
             head_rot=-2, cape_u=-2, cape_l=4)),
    (0.26, R(handN=(652, -506), weapon=176, torso_rot=21, body_rot=6, body_x=-28, body_y=-18, torso_sy=1.04, handF=(650, -560),
             head_rot=-8, skirt_rot=6, cape_u=8, cape_l=10, footN=(104, ANKLE_Y, 4), footF=(-84, ANKLE_Y, -6))),
    (0.30, R(handN=(646, -498), weapon=182, torso_rot=22, body_rot=6, body_x=-30, body_y=-18, torso_sy=1.045, handF=(646, -552),
             head_rot=-8, skirt_rot=6, cape_u=9, cape_l=11, footN=(104, ANKLE_Y, 4), footF=(-84, ANKLE_Y, -6))),
    (0.36, R(handN=(862, -612), weapon=64, torso_rot=-6, body_rot=-4, body_x=10, body_y=-36, torso_sy=1.0, handF=(820, -650),
             head_rot=4, cape_u=-14, cape_l=10, cape_w=.8, footN=(120, ANKLE_Y, 0), footF=(-92, ANKLE_Y, -14))),
    (0.42, R(handN=(900, -884), weapon=-30, torso_rot=-24, body_rot=-12, body_x=40, body_y=-74, torso_sy=.92, skirt_sy=.9,
             handF=(850, -860), head_rot=6, skirt_rot=-8, cape_u=-22, cape_l=-4, cape_w=.3,
             footN=(128, ANKLE_Y, 0), footF=(-96, ANKLE_Y, -20))),
    (0.55, R(handN=(898, -880), weapon=-32, torso_rot=-23, body_rot=-11, body_x=40, body_y=-70, torso_sy=.94, skirt_sy=.93,
             handF=(848, -856), head_rot=7, skirt_rot=-7, cape_u=-32, cape_l=-24, cape_w=-.4,
             footN=(128, ANKLE_Y, 0), footF=(-96, ANKLE_Y, -20))),
    (0.74, R(handN=(880, -864), weapon=-36, torso_rot=-18, body_rot=-8, body_x=34, body_y=-52, torso_sy=.97,
             handF=(836, -846), head_rot=6, cape_u=-20, cape_l=-18, footN=(126, ANKLE_Y, 0), footF=(-94, ANKLE_Y, -18))),
    (1.00, R()),
]
MACE_UP = [
    (0.00, R()),
    (0.22, R(handN=(700, -904), weapon=-156, torso_rot=-8, body_rot=-2, body_y=-58, torso_sy=.95, handF=(690, -880),
             head_rot=-4, cape_u=-6, cape_l=4, footN=(70, ANKLE_Y, 0), footF=(-56, ANKLE_Y, 0))),
    (0.30, R(handN=(696, -908), weapon=-160, torso_rot=-9, body_rot=-2, body_y=-60, torso_sy=.945, handF=(686, -884),
             head_rot=-4, cape_u=-5, cape_l=5, footN=(70, ANKLE_Y, 0), footF=(-56, ANKLE_Y, 0))),
    (0.38, R(handN=(866, -606), weapon=56, torso_rot=4, body_rot=2, body_x=16, body_y=-4, torso_sy=1.05, handF=(820, -640),
             head_rot=16, cape_u=-16, cape_l=-2, cape_w=.7, footN=(64, ANKLE_Y+18, -16), footF=(-50, ANKLE_Y+22, -24))),
    (0.55, R(handN=(768, -474), weapon=118, torso_rot=12, body_rot=4, body_x=12, body_y=-8, torso_sy=1.03, handF=(740, -520),
             head_rot=16, cape_u=-26, cape_l=-16, cape_w=-.3, footN=(62, ANKLE_Y+8, -8), footF=(-50, ANKLE_Y+10, -14))),
    (0.74, R(handN=(760, -490), weapon=124, torso_rot=10, body_rot=3, body_x=8, body_y=-20, handF=(736, -540), head_rot=12,
             cape_u=-14, cape_l=-12)),
    (1.00, R()),
]
MACE_DOWN = [
    (0.00, R(**AIR_FEET, handN=(760, -800), weapon=-20)),
    (0.26, R(**AIR_FEET, body_y=16, torso_rot=12, torso_sy=1.04, handN=(680, -520), weapon=150, handF=(680, -560),
             head_rot=-6, skirt_rot=6, cape_u=-4, cape_l=6)),
    (0.30, R(**AIR_FEET, body_y=18, torso_rot=12, torso_sy=1.045, handN=(676, -514), weapon=154, handF=(676, -556),
             head_rot=-6, skirt_rot=6, cape_u=-3, cape_l=7)),
    (0.38, R(footN=(70, ANKLE_Y+170, -40), footF=(-8, ANKLE_Y+150, -46), body_y=26, torso_rot=-16, body_rot=-6, torso_sy=.95,
             handN=(760, -930), weapon=-88, handF=(740, -900), head_rot=-16, skirt_sy=.88, cape_u=-66, cape_l=-44, cape_w=.9)),
    (0.55, R(footN=(70, ANKLE_Y+170, -40), footF=(-8, ANKLE_Y+150, -46), body_y=24, torso_rot=-16, body_rot=-6, torso_sy=.96,
             handN=(758, -934), weapon=-90, handF=(738, -904), head_rot=-16, skirt_sy=.88, cape_u=-72, cape_l=-50, cape_w=-.6)),
    (0.74, R(**AIR_FEET, body_y=12, torso_rot=-8, handN=(764, -880), weapon=-76, handF=(730, -860), head_rot=-9, cape_u=-40, cape_l=-24)),
    (1.00, R(**AIR_FEET, handN=(760, -800), weapon=-20)),
]

# ---- Thorn Spear
SPEAR_FRONT = [
    (0.00, R(weapon=-4)),
    (0.22, R(handN=(656, -764), weapon=3, torso_rot=13, body_rot=4, body_x=-18, body_y=-34, torso_sy=.955, handF=(720, -748),
             head_rot=-2, skirt_rot=5, cape_u=6, cape_l=8, footN=(76, ANKLE_Y, 4), footF=(-72, ANKLE_Y, -6))),
    (0.30, R(handN=(648, -762), weapon=3, torso_rot=14, body_rot=4, body_x=-22, body_y=-36, torso_sy=.95, handF=(714, -746),
             head_rot=-2, skirt_rot=5, cape_u=7, cape_l=9, footN=(76, ANKLE_Y, 4), footF=(-72, ANKLE_Y, -6))),
    (0.36, R(handN=(978, -742), weapon=0, torso_rot=-12, body_rot=-11, body_x=64, body_y=-44, torso_sy=1.05, handF=(900, -752),
             head_rot=6, skirt_rot=-9, cape_u=-44, cape_l=-14, cape_w=.8, footN=(160, ANKLE_Y, 0), footF=(-104, ANKLE_Y, -26))),
    (0.55, R(handN=(982, -746), weapon=-1, torso_rot=-13, body_rot=-11, body_x=68, body_y=-46, torso_sy=1.02, handF=(904, -756),
             head_rot=6, skirt_rot=-9, cape_u=-48, cape_l=-32, cape_w=-.5, footN=(160, ANKLE_Y, 0), footF=(-106, ANKLE_Y, -26))),
    (0.74, R(handN=(890, -786), weapon=-4, torso_rot=-6, body_rot=-6, body_x=46, body_y=-36, handF=(830, -790), head_rot=5,
             cape_u=-22, cape_l=-22, footN=(146, ANKLE_Y, 0), footF=(-98, ANKLE_Y, -16))),
    (1.00, R(weapon=-4)),
]
SPEAR_UP = [
    (0.00, R(weapon=-4)),
    (0.22, R(handN=(780, -880), weapon=78, torso_rot=-4, body_y=-50, torso_sy=.95, handF=(760, -800), head_rot=6,
             footN=(64, ANKLE_Y, 0), footF=(-50, ANKLE_Y, 0), cape_u=-4, cape_l=4)),
    (0.30, R(handN=(778, -886), weapon=80, torso_rot=-4, body_y=-52, torso_sy=.945, handF=(758, -806), head_rot=6,
             footN=(64, ANKLE_Y, 0), footF=(-50, ANKLE_Y, 0), cape_u=-3, cape_l=5)),
    (0.36, R(handN=(806, -440), weapon=86, torso_rot=6, body_rot=2, body_y=4, torso_sy=1.06, handF=(790, -560), head_rot=18,
             footN=(56, ANKLE_Y+22, -20), footF=(-44, ANKLE_Y+26, -26), cape_u=-10, cape_l=-2, cape_w=.6)),
    (0.55, R(handN=(804, -436), weapon=87, torso_rot=6, body_rot=2, body_y=2, torso_sy=1.05, handF=(790, -556), head_rot=18,
             footN=(56, ANKLE_Y+20, -20), footF=(-44, ANKLE_Y+24, -26), cape_u=-18, cape_l=-12, cape_w=-.3)),
    (0.74, R(handN=(790, -560), weapon=80, torso_rot=3, body_y=-16, handF=(770, -660), head_rot=12, cape_u=-12, cape_l=-10)),
    (1.00, R(weapon=-4)),
]
SPEAR_DOWN = [
    (0.00, R(**AIR_FEET, handN=(760, -800), weapon=-20)),
    (0.24, R(**AIR_FEET, body_y=14, torso_rot=6, torso_sy=1.03, handN=(726, -610), weapon=-88, handF=(716, -680),
             head_rot=-10, cape_u=-10, cape_l=2)),
    (0.30, R(**AIR_FEET, body_y=16, torso_rot=6, torso_sy=1.035, handN=(724, -600), weapon=-89, handF=(714, -672),
             head_rot=-10, cape_u=-8, cape_l=4)),
    (0.36, R(footN=(70, ANKLE_Y+160, -40), footF=(-6, ANKLE_Y+140, -46), body_y=22, torso_rot=-10, body_rot=-4, torso_sy=.96,
             handN=(750, -940), weapon=-90, handF=(730, -880), head_rot=-16, skirt_sy=.9, cape_u=-64, cape_l=-42, cape_w=.8)),
    (0.55, R(footN=(70, ANKLE_Y+160, -40), footF=(-6, ANKLE_Y+140, -46), body_y=20, torso_rot=-10, body_rot=-4, torso_sy=.97,
             handN=(748, -944), weapon=-90, handF=(728, -884), head_rot=-16, skirt_sy=.9, cape_u=-70, cape_l=-48, cape_w=-.5)),
    (0.74, R(**AIR_FEET, body_y=10, torso_rot=-6, handN=(760, -870), weapon=-70, handF=(730, -840), head_rot=-9, cape_u=-38, cape_l=-24)),
    (1.00, R(**AIR_FEET, handN=(760, -800), weapon=-20)),
]

# ---- Vine Whip (3-hit combo, then up / down). The lash follows the handle's direction.
WHIP_FRONT1 = [   # forward crack
    (0.00, R()),
    (0.22, R(handN=(650, -560), weapon=148, torso_rot=14, body_rot=3, body_x=-16, body_y=-30, torso_sy=.96, head_rot=-4,
             handF=(790, -760), cape_u=4, cape_l=8, footN=(90, ANKLE_Y, 4), footF=(-70, ANKLE_Y, -6))),
    (0.30, R(handN=(646, -554), weapon=152, torso_rot=15, body_rot=3, body_x=-18, body_y=-32, torso_sy=.955, head_rot=-4,
             handF=(792, -758), cape_u=5, cape_l=9, footN=(90, ANKLE_Y, 4), footF=(-70, ANKLE_Y, -6))),
    (0.37, R(handN=(900, -680), weapon=2, torso_rot=-12, body_rot=-7, body_x=30, body_y=-30, torso_sy=1.03, head_rot=7,
             handF=(650, -790), cape_u=-28, cape_l=-4, cape_w=.6, footN=(120, ANKLE_Y, 0), footF=(-86, ANKLE_Y, -18))),
    (0.44, R(handN=(846, -716), weapon=-24, torso_rot=-8, body_rot=-5, body_x=22, body_y=-34, torso_sy=1.0, head_rot=8,
             handF=(660, -790), cape_u=-30, cape_l=-18, footN=(118, ANKLE_Y, 0), footF=(-84, ANKLE_Y, -16))),
    (0.60, R(handN=(840, -724), weapon=-30, torso_rot=-8, body_rot=-5, body_x=20, body_y=-32, head_rot=8,
             handF=(664, -792), cape_u=-22, cape_l=-20, footN=(116, ANKLE_Y, 0), footF=(-84, ANKLE_Y, -14))),
    (1.00, R()),
]
WHIP_FRONT2 = [   # returning lash: low-forward flick up and back
    (0.00, R(handN=(840, -724), weapon=-30, torso_rot=-8, body_x=20, body_y=-32, head_rot=8)),
    (0.22, R(handN=(846, -880), weapon=-46, torso_rot=-10, body_x=24, body_y=-44, torso_sy=.96, head_rot=4, handF=(700, -780))),
    (0.30, R(handN=(842, -884), weapon=-48, torso_rot=-10, body_x=24, body_y=-46, torso_sy=.955, head_rot=4, handF=(700, -782))),
    (0.37, R(handN=(890, -640), weapon=20, torso_rot=8, body_x=30, body_y=-24, torso_sy=1.04, head_rot=10, handF=(660, -790),
             cape_u=-20, cape_l=-4, cape_w=.6)),
    (0.48, R(handN=(820, -560), weapon=60, torso_rot=12, body_x=24, body_y=-22, head_rot=12, handF=(664, -786), cape_u=-18, cape_l=-14)),
    (0.62, R(handN=(816, -566), weapon=62, torso_rot=11, body_x=22, body_y=-24, head_rot=11, handF=(668, -788))),
    (1.00, R()),
]
WHIP_FRONT3 = [   # broad sweep: full-body turn
    (0.00, R(handN=(816, -566), weapon=62, torso_rot=11, body_x=22, body_y=-24, head_rot=11)),
    (0.24, R(handN=(630, -590), weapon=138, torso_rot=20, body_rot=5, body_x=-24, body_y=-36, torso_sy=.95, head_rot=-6,
             handF=(800, -740), cape_u=8, cape_l=10, footN=(96, ANKLE_Y, 4), footF=(-76, ANKLE_Y, -8))),
    (0.30, R(handN=(624, -586), weapon=142, torso_rot=21, body_rot=5, body_x=-26, body_y=-38, torso_sy=.945, head_rot=-6,
             handF=(804, -738), cape_u=9, cape_l=11, footN=(96, ANKLE_Y, 4), footF=(-76, ANKLE_Y, -8))),
    (0.38, R(handN=(930, -700), weapon=0, torso_rot=-16, body_rot=-9, body_x=44, body_y=-36, torso_sy=1.04, head_rot=8,
             handF=(640, -796), cape_u=-34, cape_l=-6, cape_w=.7, footN=(134, ANKLE_Y, 0), footF=(-96, ANKLE_Y, -22))),
    (0.52, R(handN=(870, -890), weapon=-64, torso_rot=-20, body_rot=-9, body_x=48, body_y=-44, torso_sy=.97, head_rot=9,
             handF=(630, -800), cape_u=-44, cape_l=-28, cape_w=-.4, footN=(134, ANKLE_Y, 0), footF=(-96, ANKLE_Y, -22))),
    (0.70, R(handN=(866, -894), weapon=-68, torso_rot=-18, body_rot=-8, body_x=44, body_y=-40, head_rot=8,
             handF=(636, -800), cape_u=-24, cape_l=-24, footN=(130, ANKLE_Y, 0), footF=(-94, ANKLE_Y, -18))),
    (1.00, R()),
]
WHIP_UP = [
    (0.00, R()),
    (0.22, R(handN=(820, -800), weapon=18, torso_rot=-6, body_y=-44, torso_sy=.955, head_rot=2, handF=(720, -800))),
    (0.30, R(handN=(824, -806), weapon=16, torso_rot=-6, body_y=-46, torso_sy=.95, head_rot=2, handF=(720, -804))),
    (0.38, R(handN=(820, -520), weapon=100, torso_rot=6, body_y=-4, torso_sy=1.05, head_rot=16, handF=(700, -760),
             footN=(56, ANKLE_Y+14, -14), footF=(-42, ANKLE_Y+18, -20), cape_u=-12, cape_l=-2, cape_w=.6)),
    (0.52, R(handN=(730, -500), weapon=152, torso_rot=12, body_y=-8, torso_sy=1.03, head_rot=16, handF=(690, -740),
             cape_u=-22, cape_l=-14)),
    (0.70, R(handN=(724, -512), weapon=156, torso_rot=10, body_y=-18, head_rot=12, handF=(690, -760))),
    (1.00, R()),
]
WHIP_DOWN = [
    (0.00, R(**AIR_FEET, handN=(760, -800), weapon=-20)),
    (0.22, R(**AIR_FEET, body_y=12, torso_rot=6, handN=(740, -600), weapon=70, handF=(700, -660), head_rot=-6)),
    (0.30, R(**AIR_FEET, body_y=14, torso_rot=6, handN=(738, -596), weapon=72, handF=(700, -656), head_rot=-6)),
    (0.38, R(footN=(66, ANKLE_Y+150, -40), footF=(-8, ANKLE_Y+130, -46), body_y=20, torso_rot=-10, torso_sy=1.03,
             handN=(800, -900), weapon=-85, handF=(660, -720), head_rot=-14, cape_u=-60, cape_l=-40, cape_w=.8)),
    (0.52, R(footN=(66, ANKLE_Y+150, -40), footF=(-8, ANKLE_Y+130, -46), body_y=18, torso_rot=-12,
             handN=(740, -912), weapon=-138, handF=(660, -724), head_rot=-14, cape_u=-66, cape_l=-46, cape_w=-.6)),
    (0.72, R(**AIR_FEET, body_y=10, torso_rot=-6, handN=(750, -880), weapon=-110, handF=(700, -780), head_rot=-9, cape_u=-38, cape_l=-22)),
    (1.00, R(**AIR_FEET, handN=(760, -800), weapon=-20)),
]

# ---- Resin Sling (the pouch orbits the hand; the seed launches at 55% of the hit window)
SLING_THROW = [
    (0.00, R()),
    (0.22, R(handN=(640, -640), weapon=110, torso_rot=10, body_rot=2, body_x=-10, body_y=-26, torso_sy=.97, head_rot=4,
             handF=(840, -700), elbowF=-1, cape_u=2, cape_l=6, footN=(88, ANKLE_Y, 4), footF=(-66, ANKLE_Y, -6))),
    (0.30, R(handN=(636, -636), weapon=114, torso_rot=11, body_rot=2, body_x=-12, body_y=-27, torso_sy=.965, head_rot=4,
             handF=(842, -698), elbowF=-1, cape_u=3, cape_l=7, footN=(88, ANKLE_Y, 4), footF=(-66, ANKLE_Y, -6))),
    (0.44, R(handN=(912, -646), weapon=8, torso_rot=-10, body_rot=-6, body_x=26, body_y=-30, torso_sy=1.03, head_rot=6,
             handF=(660, -790), cape_u=-26, cape_l=-6, cape_w=.6, footN=(116, ANKLE_Y, 0), footF=(-84, ANKLE_Y, -16))),
    (0.58, R(handN=(880, -770), weapon=-26, torso_rot=-12, body_rot=-6, body_x=28, body_y=-32, head_rot=6,
             handF=(650, -796), cape_u=-30, cape_l=-18, footN=(116, ANKLE_Y, 0), footF=(-84, ANKLE_Y, -16))),
    (1.00, R()),
]

OTHER_WEAPONS = {
    'Mace': dict(front=[MACE_FRONT], up=MACE_UP, down=MACE_DOWN),
    'Spear': dict(front=[SPEAR_FRONT], up=SPEAR_UP, down=SPEAR_DOWN),
    'Whip': dict(front=[WHIP_FRONT1, WHIP_FRONT2, WHIP_FRONT3], up=WHIP_UP, down=WHIP_DOWN),
}

# ================================================================= walls and ledges
# Geometry of the player collider in rig px (y-up, facing +x): 0.8 x 1.2 world, rig 0.1464 scale.
#   collider front x ~946, back x ~400, top y ~-419, bottom (ground) ~-1239.
WALL_X = 952            # wall surface when sliding / jumping up a wall (Qori faces it)
BACK_WALL_X = 402       # wall behind Qori at a wall jump away from it
LEDGE = (953.0, -378.0) # ledge corner while hanging (PlayerMovement.TryGrabLedge hang position)
LEDGE_RISE = 885.0      # collider travel up during the climb (progress 0 -> .65)
LEDGE_ACROSS = 601.0    # collider travel forward onto the ledge (progress .65 -> 1)
def foot_x(x): return x - B['ThighNear']['pos'][0]      # absolute x -> foot control x
def foot_xf(x): return x - B['ThighFar']['pos'][0]

# ---- wall slide: pressed to the wall, hands and one foot braking, the other leg hanging
WALL_SLIDE_T = 0.8
def _slide(k):   # k: 0..1 small brace/slip cycle
    return R(body_x=112, body_y=30-6*k, body_rot=8+1.5*k, torso_rot=4+2*k, head_rot=22+2*k, skirt_rot=10+2*k, skirt_sy=1.08,
             handN=(WALL_X-6, -470-14*k), elbowN=-1, handF=(WALL_X-10, -640+10*k), elbowF=-1, weapon=60,
             footN=(foot_x(WALL_X-36), -930-8*k, 76), footF=(foot_xf(WALL_X-60), -1080+6*k, 64),
             cape_u=-60-6*k, cape_l=-30+4*k, cape_w=-.6+1.2*k)
wall_slide = keyed([(0.0, _slide(0)), (0.4, _slide(1)), (0.8, _slide(0))], loop=True)

# ---- wall jump straight up the wall (climbing): coil, kick off the wall foot, reach up high
WALL_JUMP_UP_T = 0.42
wall_jump_up = keyed([
    (0.00, R(body_x=96, body_y=10, body_rot=4, torso_rot=10, torso_sy=.95, head_rot=14,
             handN=(WALL_X-10, -640), handF=(WALL_X-16, -740), weapon=60,
             footN=(foot_x(WALL_X-44), -960, 76), footF=(foot_xf(870), -1080, 40), cape_u=-40, cape_l=-20)),
    (0.10, R(body_x=92, body_y=60, body_rot=2, torso_rot=0, torso_sy=1.05, head_rot=22,
             handN=(WALL_X-12, -380), handF=(WALL_X-20, -820), weapon=60, skirt_sy=.94,
             footN=(foot_x(WALL_X-60), -1160, 60), footF=(foot_xf(880), -960, 50), cape_u=-6, cape_l=6, cape_w=.5)),
    (0.26, R(body_x=90, body_y=56, body_rot=3, torso_rot=2, torso_sy=1.03, head_rot=20,
             handN=(WALL_X-14, -420), handF=(WALL_X-24, -560), weapon=60,
             footN=(foot_x(WALL_X-80), -1120, 30), footF=(foot_xf(890), -1000, 56), cape_u=4, cape_l=10, cape_w=-.3)),
    (0.42, R(body_x=88, body_y=44, body_rot=4, torso_rot=4, head_rot=18,
             handN=(WALL_X-14, -470), handF=(WALL_X-20, -600), weapon=60,
             footN=(foot_x(WALL_X-60), -1080, 50), footF=(foot_xf(870), -1060, 40), cape_u=-10, cape_l=0)),
])

# ---- wall jump away: Qori already faces away from the wall (it is behind him)
WALL_JUMP_OFF_T = 0.45
_OFF_AIR = R(body_y=8, body_rot=-6, torso_rot=-4, torso_sy=1.03, head_rot=10, skirt_rot=-6, skirt_sy=.94,
             footN=(62, ANKLE_Y+120, 22), footF=(-70, ANKLE_Y+50, -40), handN=(740, -690), weapon=40, handF=(660, -600),
             cape_u=0, cape_l=6, cape_w=-.3)
wall_jump_off = keyed([
    (0.00, R(body_x=-70, body_y=-40, body_rot=-14, torso_rot=-16, torso_sy=.94, head_rot=6,
             footN=(foot_x(BACK_WALL_X+40), -980, -80), footF=(foot_xf(BACK_WALL_X+50), -1100, -80),
             handN=(640, -780), weapon=-40, handF=(560, -740), elbowF=1, skirt_rot=10, cape_u=10, cape_l=14)),
    (0.10, R(body_x=10, body_y=10, body_rot=-24, torso_rot=-14, torso_sy=1.06, head_rot=8,
             footN=(foot_x(470), -1060, -70), footF=(foot_xf(440), -1160, -80),
             handN=(900, -600), weapon=30, handF=(860, -560), skirt_rot=-8, skirt_sy=.92, cape_u=4, cape_l=12, cape_w=.8)),
    (0.24, R(body_x=10, body_y=12, body_rot=-14, torso_rot=-8, torso_sy=1.04, head_rot=10,
             footN=(40, ANKLE_Y+90, -10), footF=(-90, ANKLE_Y+40, -50),
             handN=(800, -640), weapon=40, handF=(720, -600), skirt_rot=-8, skirt_sy=.93, cape_u=2, cape_l=8, cape_w=-.4)),
    (0.45, _OFF_AIR),
])

# ---- ledge hang: hanging from the long reach arms (rig bones *Reach*, swapped in by
# QoriAnimator). Both fists grip the ledge top just behind the lip, the body hangs below with
# the chest against the wall and the head tipped back looking up, legs dangling with one foot
# scrabbling on the wall. HANG_DROP lowers the body (and feet) relative to the collider.
LEDGE_HANG_T = 1.8
HANG_DROP = -10.0
def _ledge_hands(cx, cy):
    return dict(handN=(cx+40, cy+30), elbowN=+1, handF=(cx+5, cy+34), elbowF=-1)
_hang_base = dict(body_x=110, body_y=HANG_DROP, body_rot=-3, torso_rot=-2, head_rot=45, skirt_rot=4, skirt_sy=1.08, weapon=90,
                  cape_u=14, cape_l=10, **_ledge_hands(*LEDGE))
_FOOT_N = (foot_x(WALL_X-34), -1010 + HANG_DROP, 70)
_FOOT_F = (foot_xf(840), -1190 + HANG_DROP, -30)
def ledge_hang(t):
    p = TAU*t/LEDGE_HANG_T
    return R(**{**_hang_base, 'body_y': HANG_DROP + 5*math.sin(p), 'body_rot': -3 + 1.5*math.sin(p-.6),
                'torso_rot': -2 + 1.2*math.sin(p-.9), 'head_rot': 45 + 2.5*math.sin(p-1.2),
                'skirt_rot': 4 + 3*math.sin(p-1.4), 'cape_w': .35*math.sin(p-1.6), 'cape_u': 14 + 4*math.sin(p-1.3),
                'footN': (_FOOT_N[0], _FOOT_N[1] + 14*math.sin(p-.4), _FOOT_N[2] + 6*math.sin(p-.4)),
                'footF': (_FOOT_F[0] + 18*math.sin(p-1.0), _FOOT_F[1] + 8*math.sin(p-1.3), _FOOT_F[2] + 8*math.sin(p-1.0))})

# ---- ledge climb (scrubbed by PlayerMovement.LedgeClimbProgress: 0-.65 up, .65-1 across)
def ledge_corner(p):
    up = min(p/.65, 1.0); across = max(0.0, (p-.65)/.35)
    return LEDGE[0] - LEDGE_ACROSS*across, LEDGE[1] - LEDGE_RISE*up
_TOP = LEDGE[1] - LEDGE_RISE          # ledge top in the body frame once risen (y-up)
_ANK_TOP = _TOP + anim.SOLE
_climb_body = keyed([
    (0.00, R(**{k: v for k, v in _hang_base.items() if not k.startswith(('hand', 'elbow'))},
             footN=_FOOT_N, footF=_FOOT_F)),
    (0.10, R(body_x=116, body_y=70, body_rot=-8, torso_rot=-6, torso_sy=.96, head_rot=24, skirt_sy=1.06, weapon=90,
             footN=(foot_x(WALL_X-40), -1030, 80), footF=(foot_xf(860), -1130, 30), cape_u=4, cape_l=4)),
    (0.28, R(body_x=150, body_y=80, body_rot=-18, torso_rot=-18, torso_sy=1.03, head_rot=14, skirt_sy=.96, weapon=90,
             footN=(foot_x(WALL_X-40), -1110, 70), footF=(foot_xf(880), -1180, 40), cape_u=-24, cape_l=-12, cape_w=.5)),
    (0.46, R(body_x=200, body_y=-70, body_rot=-22, torso_rot=-22, torso_sy=.97, head_rot=4, skirt_sy=.94, weapon=90,
             footN=(foot_x(1000), _ANK_TOP-60, 30), footF=(foot_xf(900), -1300, 50), cape_u=-40, cape_l=-24, cape_w=-.4)),
    (0.65, R(body_x=250, body_y=-150, body_rot=-18, torso_rot=-16, torso_sy=.95, head_rot=2, skirt_sy=.92, weapon=90,
             footN=(foot_x(1010), _ANK_TOP, 0), footF=(foot_xf(930), -1290, -60), cape_u=-30, cape_l=-24)),
    (0.82, R(body_x=150, body_y=-70, body_rot=-8, torso_rot=-8, torso_sy=.98, head_rot=4, weapon=40,
             footN=(foot_x(LEDGE[0]-LEDGE_ACROSS*(.17/.35)+160), _ANK_TOP, 0), footF=(foot_xf(LEDGE[0]-LEDGE_ACROSS*(.17/.35)+70), _ANK_TOP, 0),
             handN=(800, -760), handF=(740, -760), cape_u=-16, cape_l=-12)),
    (1.00, R(footN=(58, _ANK_TOP, 0), footF=(-42, _ANK_TOP, 0))),
])
def ledge_climb(t):
    c = _climb_body(t)
    cx, cy = ledge_corner(t)
    planted = _ledge_hands(cx, cy)
    # hands stay planted on the ledge while the body rises, then let go and swing to ready
    w = ss(.52, .74, t)
    for k in ('handN', 'handF'):
        free = c.get(k, READY[k])
        c[k] = tuple(np.array(planted[k])*(1-w) + np.array(free)*w)
    c['elbowN'] = -1; c['elbowF'] = -1
    return c

def build_all():
    build()
    CLIPS['AttackFront'] = _bake('AttackFront', attack_front, 1.0, False)
    CLIPS['AttackUp'] = _bake('AttackUp', attack_up, 1.0, False)
    CLIPS['AttackDown'] = _bake('AttackDown', attack_down, 1.0, False)
    CLIPS['AttackAirFront'] = _bake('AttackAirFront', attack_air_front, 1.0, False)
    CLIPS['AttackAirUp'] = _bake('AttackAirUp', attack_air_up, 1.0, False)
    for i, (g, a) in enumerate([(attack_front2, attack_air_front2), (attack_front3, attack_air_front3), (attack_front4, attack_air_front4)], start=2):
        CLIPS[f'AttackFront{i}'] = _bake(f'AttackFront{i}', g, 1.0, False)
        CLIPS[f'AttackAirFront{i}'] = _bake(f'AttackAirFront{i}', a, 1.0, False)
    for w, d in OTHER_WEAPONS.items():
        for i, keys in enumerate(d['front'], start=1):
            CLIPS[f'{w}_Front{i}'] = _bake(f'{w}_Front{i}', keyed(keys), 1.0, False)
            CLIPS[f'{w}_AirFront{i}'] = _bake(f'{w}_AirFront{i}', keyed(airborne(keys)), 1.0, False)
        CLIPS[f'{w}_Up'] = _bake(f'{w}_Up', keyed(d['up']), 1.0, False)
        CLIPS[f'{w}_AirUp'] = _bake(f'{w}_AirUp', keyed(airborne(d['up'])), 1.0, False)
        CLIPS[f'{w}_Down'] = _bake(f'{w}_Down', keyed(d['down']), 1.0, False)
    CLIPS['Sling_Throw'] = _bake('Sling_Throw', keyed(SLING_THROW), 1.0, False)
    CLIPS['Sling_AirThrow'] = _bake('Sling_AirThrow', keyed(airborne(SLING_THROW)), 1.0, False)
    CLIPS['Rise'] = _bake('Rise', rise, RISE_T, True)
    CLIPS['Fall'] = _bake('Fall', fall, FALL_T, True)
    CLIPS['Land'] = _bake('Land', land, LAND_T, False)
    CLIPS['Hang'] = _bake('Hang', hang, HANG_T, True)
    CLIPS['WallSlide'] = _bake('WallSlide', wall_slide, WALL_SLIDE_T, True)
    CLIPS['WallJumpUp'] = _bake('WallJumpUp', wall_jump_up, WALL_JUMP_UP_T, False)
    CLIPS['WallJumpOff'] = _bake('WallJumpOff', wall_jump_off, WALL_JUMP_OFF_T, False)
    CLIPS['LedgeHang'] = _bake('LedgeHang', ledge_hang, LEDGE_HANG_T, True)
    CLIPS['LedgeClimb'] = _bake('LedgeClimb', ledge_climb, 1.0, False)
    # Qori carries the weapon in the camera-side hand everywhere it is visible: idle/walk/run are
    # authored that way; attacks and air/landing clips are re-targeted by swapping the hands.
    # Hanging, ledge and wall-grip clips keep both hands as authored (the blade is stowed there).
    return CLIPS

FAR_AUTHORED = {'Idle', 'Walk', 'Run'}
BOTH_HANDS_GRIP = {'Hang', 'WallSlide', 'WallJumpUp', 'LedgeHang', 'LedgeClimb'}
def _bake(name, fn, length, loop):
    if name in FAR_AUTHORED | BOTH_HANDS_GRIP:
        return anim.bake(fn, length, loop)
    def swapped(t):
        c = dict(fn(t)); c['swap_hands'] = True
        return c
    return anim.bake(swapped, length, loop)
