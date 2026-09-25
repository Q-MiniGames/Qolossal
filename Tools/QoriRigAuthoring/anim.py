"""Animation authoring: controls -> IK -> bone rotations -> sampled keys.

A clip is a function f(t) -> controls dict (see DEFAULT). Everything is in
rig px, y-up; angles in degrees (CCW +).
"""
import os as _os
_HERE=_os.path.dirname(_os.path.abspath(__file__))
_PROJECT=_os.path.abspath(_os.path.join(_HERE,'..','..'))
_WORK=_os.path.join(_HERE,'work')
_os.makedirs(_WORK,exist_ok=True)
import math, json
import numpy as np
import rig

GROUND = -1245.0
SOLE = rig.META['leg']['sole']          # ankle height above ground with a flat foot
ANKLE_Y = GROUND + SOLE
FPS = 30

def ss(a, b, x):  # smoothstep
    t = min(max((x-a)/(b-a), 0.0), 1.0); return t*t*(3-2*t)
def lerp(a, b, t): return a + (b-a)*t
def ang(v): return math.degrees(math.atan2(v[1], v[0]))

B = rig.BONES
def rest_dir(child_pos, parent_pos): return ang(np.array(child_pos) - np.array(parent_pos))
LEG = {}
for s in ('Near', 'Far'):
    H, K, A = B[f'Thigh{s}']['pos'], B[f'Shin{s}']['pos'], B[f'Foot{s}']['pos']
    LEG[s] = dict(l1=np.linalg.norm(K-H), l2=np.linalg.norm(A-K), a1=rest_dir(K, H), a2=rest_dir(A, K))
ARM = {}
for s in ('Near', 'Far'):
    S_, E, F = B[f'UpperArm{s}']['pos'], B[f'Forearm{s}']['pos'], B[f'Hand{s}']['pos']
    ARM[s] = dict(l1=np.linalg.norm(E-S_), l2=np.linalg.norm(F-E), a1=rest_dir(E, S_), a2=rest_dir(F, E))

REACH = {}
for s in ('Near', 'Far'):
    if f'UpperArmReach{s}' not in B: continue
    S_, E = B[f'UpperArmReach{s}']['pos'], B[f'ForearmReach{s}']['pos']
    F = E + np.array([rig.META['arm']['reach_fist_from_elbow'][0], -rig.META['arm']['reach_fist_from_elbow'][1]])
    REACH[s] = dict(l1=np.linalg.norm(E-S_), l2=np.linalg.norm(F-E), a1=rest_dir(E, S_), a2=rest_dir(F, E))

def two_bone(root, target, l1, l2, bend):
    """Return (angle1, angle2) world directions. bend=+1 rotates the first bone CCW
    from the root->target line (knee forward for a downward leg)."""
    d = np.array(target, float) - root
    dist = min(max(np.linalg.norm(d), abs(l1-l2)+1e-3), l1+l2-1e-3)
    base = ang(d)
    off = math.degrees(math.acos(max(-1, min(1, (l1*l1 + dist*dist - l2*l2)/(2*l1*dist)))))
    a1 = base + bend*off
    mid = root + l1*np.array([math.cos(math.radians(a1)), math.sin(math.radians(a1))])
    tgt = root + d/np.linalg.norm(d)*dist
    return a1, ang(tgt - mid)

DEFAULT = dict(
    body_x=0.0, body_y=0.0, body_rot=0.0, body_sx=1.0, body_sy=1.0,
    torso_rot=0.0, torso_sy=1.0, head_rot=0.0, skirt_rot=0.0, skirt_sx=1.0, skirt_sy=1.0,
    handN=None, handF=None, elbowN=-1, elbowF=-1, weapon=-20.0, weapon_far=None,
    armN_rot=None, foreN_rot=None, armF_rot=None, foreF_rot=None,
    footN=(40.0, ANKLE_Y, 0.0), footF=(-30.0, ANKLE_Y, 0.0),   # x relative to rest hip x
    cape_u=-8.0, cape_l=-4.0, cape_w=0.0,   # cloak: upper/lower drape angle (neg = trails back), flutter -1..1
)
CAPE_SPREAD = [-6.0, 0.0, 5.0]
CAPE_WAVE_U = [4.0, -5.0, 6.0]
CAPE_WAVE_L = [9.0, -7.0, 12.0]

def pose_from_controls(c):
    c = {**DEFAULT, **c}
    pose = {
        'Body': dict(x=c['body_x'], y=c['body_y'], rot=c['body_rot'], sx=c['body_sx'], sy=c['body_sy']),
        'Torso': dict(rot=c['torso_rot'], sy=c['torso_sy']),
        'Head': dict(rot=c['head_rot']),
        'Skirt': dict(rot=c['skirt_rot'], sx=c['skirt_sx'], sy=c['skirt_sy']),
    }
    W = rig.solve(pose)
    # legs
    body_w = rig.world_angle(W['Body'])
    for s in ('Near', 'Far'):
        L = LEG[s]; hip = W[f'Thigh{s}'][:, 2]
        fx, fy, fang = c['foot'+s[0]]
        target = np.array([B[f'Thigh{s}']['pos'][0] + fx, fy])
        a1, a2 = two_bone(hip, target, L['l1'], L['l2'], +1)
        r1 = a1 - L['a1'] - body_w
        r2 = a2 - L['a2'] - (a1 - L['a1'])
        r3 = fang - (a2 - L['a2'])
        pose[f'Thigh{s}'] = dict(rot=r1); pose[f'Shin{s}'] = dict(rot=r2); pose[f'Foot{s}'] = dict(rot=r3)
    W = rig.solve(pose)
    torso_w = rig.world_angle(W['Torso'])
    for s in ('Near', 'Far'):
        A = ARM[s]; key = 'hand'+s[0]
        if c[key] is not None:
            sh = W[f'UpperArm{s}'][:, 2]
            a1, a2 = two_bone(sh, np.array(c[key]), A['l1'], A['l2'], c['elbow'+s[0]])
            r1 = a1 - A['a1'] - torso_w; r2 = a2 - A['a2'] - (a1 - A['a1'])
        else:
            r1 = c['arm'+s[0]+'_rot'] or 0.0; r2 = c['fore'+s[0]+'_rot'] or 0.0
        pose[f'UpperArm{s}'] = dict(rot=r1); pose[f'Forearm{s}'] = dict(rot=r2)
        if s in REACH:   # the long ledge arms reach for the same hand target
            A = REACH[s]
            if c[key] is not None:
                a1, a2 = two_bone(W[f'UpperArmReach{s}'][:, 2], np.array(c[key]), A['l1'], A['l2'], c['elbow'+s[0]])
                r1 = a1 - A['a1'] - torso_w; r2 = a2 - A['a2'] - (a1 - A['a1'])
            pose[f'UpperArmReach{s}'] = dict(rot=r1); pose[f'ForearmReach{s}'] = dict(rot=r2)
    for i, cp in enumerate(rig.CAPES):
        pose[cp] = dict(rot=c['cape_u'] + CAPE_SPREAD[i] + CAPE_WAVE_U[i]*c['cape_w'])
        pose[cp + 'Lower'] = dict(rot=c['cape_l'] + CAPE_WAVE_L[i]*c['cape_w'])
    W = rig.solve(pose)
    fore_w = rig.world_angle(W['ForearmNear'])
    pose['WeaponMount'] = dict(rot=c['weapon'] - rig.WEAPON_REST_ANGLE - fore_w)
    if 'WeaponMountFar' in rig.BONES:   # blade angle when carried in the camera-side hand
        far_w = rig.world_angle(W['ForearmFar'])
        angle = c['weapon_far'] if c['weapon_far'] is not None else c['weapon']
        pose['WeaponMountFar'] = dict(rot=angle - rig.WEAPON_REST_ANGLE - far_w)
    return pose

# ---------------------------------------------------------------- baking
TRACKS = [(b, 'rot') for b in rig.ORDER if b != 'Root' and b not in ('HandNear', 'HandFar')] + \
         [('Body', 'x'), ('Body', 'y'), ('Body', 'sx'), ('Body', 'sy'), ('Torso', 'sy'), ('Skirt', 'sx'), ('Skirt', 'sy')]
DEF_VAL = {'rot': 0.0, 'x': 0.0, 'y': 0.0, 'sx': 1.0, 'sy': 1.0}

def bake(fn, length, loop, fps=FPS):
    n = max(2, round(length*fps))
    times = np.linspace(0, length, n+1)
    poses = [pose_from_controls(fn(t if not (loop and i == n) else 0.0)) for i, t in enumerate(times)]
    tracks = {}
    for b, p in TRACKS:
        v = np.array([pp.get(b, {}).get(p, DEF_VAL[p]) for pp in poses], float)
        if p == 'rot':
            v = np.degrees(np.unwrap(np.radians(v)))
            if loop:  # make the seam exact after unwrapping
                v[-1] = v[0] + round((v[-1]-v[0])/360)*360
        tracks[f'{b}.{p}'] = v
    # tangents (per second): Catmull-Rom, cyclic for loops
    tan = {}
    dt = times[1]-times[0]
    for k, v in tracks.items():
        g = np.zeros_like(v)
        g[1:-1] = (v[2:]-v[:-2])/(2*dt)
        if loop:
            g[0] = g[-1] = (v[1]-v[0] + v[-1]-v[-2])/(2*dt)
        else:
            g[0] = (v[1]-v[0])/dt; g[-1] = (v[-1]-v[-2])/dt
        tan[k] = g
    return dict(length=float(length), loop=loop, times=times, tracks=tracks, tangents=tan)

def hermite(clip, key, t):
    T, v, g = clip['times'], clip['tracks'][key], clip['tangents'][key]
    t = min(max(t, 0), T[-1])
    i = min(np.searchsorted(T, t, side='right')-1, len(T)-2)
    h = T[i+1]-T[i]; u = (t-T[i])/h
    h00 = 2*u**3-3*u**2+1; h10 = u**3-2*u**2+u; h01 = -2*u**3+3*u**2; h11 = u**3-u**2
    return h00*v[i] + h10*h*g[i] + h01*v[i+1] + h11*h*g[i+1]

def pose_at(clip, t):
    pose = {}
    for key in clip['tracks']:
        b, p = key.split('.')
        pose.setdefault(b, {})[p] = hermite(clip, key, t)
    return pose

def blend(pa, pb, w):
    out = {}
    for b in set(pa) | set(pb):
        out[b] = {}
        for p in set(pa.get(b, {})) | set(pb.get(b, {})):
            a = pa.get(b, {}).get(p, DEF_VAL[p]); c = pb.get(b, {}).get(p, DEF_VAL[p])
            out[b][p] = a + (c-a)*w
    return out
