"""Cutout skeleton + software renderer used to preview animations before Unity.

Internal math is y-UP rig px (Y = -y_image). Rotations in degrees, CCW positive
(Unity's localEulerAngles.z convention).
"""
import os as _os
_HERE=_os.path.dirname(_os.path.abspath(__file__))
_PROJECT=_os.path.abspath(_os.path.join(_HERE,'..','..'))
_WORK=_os.path.join(_HERE,'work')
_os.makedirs(_WORK,exist_ok=True)
import json, math
import numpy as np
from PIL import Image

META = json.load(open(_os.path.join(_WORK,'parts.json')))
PARTS = META['parts']
_imgs = {}
def part_img(name):
    if name not in _imgs:
        _imgs[name] = Image.open(_os.path.join(_WORK,'parts',name+'.png')).convert('RGBA')
    return _imgs[name]

def J(name):  # rest joint of a part, y-up
    x, y = PARTS[name]['joint']; return np.array([x, -y])

ROOT = np.array([591.0, -665.0])
PELVIS = J('Torso')
FIST_OFF = np.array([META['arm']['fist_from_elbow'][0], -META['arm']['fist_from_elbow'][1]])

# bone: parent, rest absolute position (y-up), sprite part, sorting order, tint
BONES = {}
def bone(name, parent, pos, part=None, order=0, tint=1.0):
    BONES[name] = dict(parent=parent, pos=np.array(pos, float), part=part, order=order, tint=tint)

bone('Root', None, ROOT)
bone('Body', 'Root', PELVIS)
bone('Skirt', 'Body', J('Skirt'), 'Skirt', 9)
bone('Torso', 'Body', PELVIS, 'Torso', 10)
bone('Head', 'Torso', J('Head_Neutral'), 'Head_Neutral', 13)
for ear, order in (('EarUpper', 11), ('EarLower', 12)):   # leaf ears hinge under the hair, behind the face
    if ear in PARTS: bone(ear, 'Head', J(ear), ear, order)
CAPES = [p for p in ('Cape1', 'Cape2', 'Cape3') if p in PARTS]
for i, cp in enumerate(CAPES):   # leaf cloak: behind everything, largest panel furthest back
    bone(cp, 'Torso', J(cp), cp, -6 + 2*i, .9)
    bone(cp + 'Lower', cp, J(cp + 'Lower'), cp + 'Lower', -5 + 2*i, .9)
# The torso is painted three-quarter toward the camera: its camera-side armhole is on the
# back edge. So the free arm ('Far' bones) is drawn in front, the weapon arm behind the body.
for side, tint, base in (('Near', .72, 0), ('Far', 1.0, 15)):
    bone(f'UpperArm{side}', 'Torso', J(f'UpperArm{side}'), f'UpperArm{side}', base, tint)
    bone(f'Forearm{side}', f'UpperArm{side}', J(f'Forearm{side}'), f'Forearm{side}', base+2 if side == 'Near' else base+1, tint)
    bone(f'Hand{side}', f'Forearm{side}', J(f'Forearm{side}')+FIST_OFF)
# Reach arms: longer chains on the same shoulders, shown instead of the normal arms while
# hanging from a ledge (QoriAnimator swaps them). They solve to the same hand targets.
# Both reach arms go up BEHIND the big head (the far one still in front of the torso), so
# the arms never cross the face while he hangs.
for side, tint, base in (('Near', .72, 0), ('Far', 1.0, 11)):
    if f'UpperArmReach{side}' not in PARTS: continue
    bone(f'UpperArmReach{side}', 'Torso', J(f'UpperArmReach{side}'), f'UpperArmReach{side}', base, tint)
    bone(f'ForearmReach{side}', f'UpperArmReach{side}', J(f'ForearmReach{side}'), f'ForearmReach{side}', base+2 if side == 'Near' else base+1, tint)
REACH_ARM_BONES = [b for b in BONES if 'Reach' in b]
bone('WeaponMount', 'HandNear', J('ForearmNear')+FIST_OFF, 'Weapon', 1)   # between the weapon arm's upper arm (0) and fist (2)
for side, tint, base in (('Near', .76, 2), ('Far', 1.0, 6)):   # camera-side leg (hip on the back edge) in front
    bone(f'Thigh{side}', 'Body', J(f'Thigh{side}'), f'Thigh{side}', base, tint)
    bone(f'Shin{side}', f'Thigh{side}', J(f'Shin{side}'), f'Shin{side}', base+2, tint)
    bone(f'Foot{side}', f'Shin{side}', J(f'Foot{side}'), f'Foot{side}', base+1, tint)
WEAPON_REST_ANGLE = -12.6   # WeaponMount rest rotation; the art points +x

ORDER = list(BONES)  # parents precede children

def local_pos(name):
    b = BONES[name]
    return b['pos'] - (BONES[b['parent']]['pos'] if b['parent'] else 0)

def rot(deg):
    r = math.radians(deg); c, s = math.cos(r), math.sin(r)
    return np.array([[c, -s], [s, c]])

def solve(pose):
    """pose: {bone: {'rot','x','y','sx','sy'}} (x,y are offsets from rest local pos).
    Returns world 2x3 matrices."""
    W = {}
    for n in ORDER:
        b = BONES[n]; p = pose.get(n, {})
        lp = (local_pos(n) if b['parent'] else b['pos']) + np.array([p.get('x', 0.0), p.get('y', 0.0)])
        extra = WEAPON_REST_ANGLE if n == 'WeaponMount' else 0.0
        L = np.zeros((2, 3)); L[:, :2] = rot(p.get('rot', 0.0) + extra) @ np.diag([p.get('sx', 1.0), p.get('sy', 1.0)]); L[:, 2] = lp
        if b['parent']:
            P = W[b['parent']]
            M = np.zeros((2, 3)); M[:, :2] = P[:, :2] @ L[:, :2]; M[:, 2] = P[:, :2] @ L[:, 2] + P[:, 2]
        else:
            M = L
        W[n] = M
    return W

def world_angle(M):
    return math.degrees(math.atan2(M[1, 0], M[0, 0]))

def render(pose, size=(900, 1000), origin=(170, 380), sprites=None, bg=(58, 62, 70), ground=None, scale=0.5):
    """Draw the rig. origin: rig-px (y-down) at canvas top-left. sprites: bone->part overrides."""
    W = solve(pose)
    cw, ch = int(size[0]*scale), int(size[1]*scale)
    canvas = Image.new('RGBA', (cw, ch), tuple(bg) + ((255,) if len(bg) == 3 else ()))
    if ground is not None:
        gy = int((ground - origin[1])*scale)
        canvas.paste((90, 84, 70, 255), (0, gy, cw, ch))
    items = sorted([n for n in ORDER if BONES[n]['part']], key=lambda n: BONES[n]['order'])
    for n in items:
        part = (sprites or {}).get(n, BONES[n]['part'])
        if part is None: continue
        img = part_img(part); piv = PARTS[part]['pivot']
        t = BONES[n]['tint']
        if t != 1.0:
            a = np.array(img).astype(float); a[:, :, :3] *= t; img = Image.fromarray(a.clip(0, 255).astype(np.uint8))
        M = W[n]
        # image (u,v) -> rig y-up: M @ [u-px, -(v-py)] ; -> canvas: ((X - ox)*s, (-Y - oy)*s)
        A = M[:, :2] @ np.array([[1, 0], [0, -1]])
        bvec = M[:, 2] - A @ np.array(piv)
        F = np.array([[scale, 0], [0, -scale]]) @ A
        f = np.array([scale, -scale]) * bvec - np.array([origin[0]*scale, -(-origin[1])*scale * -1])
        f = np.array([(bvec[0] - origin[0])*scale, (-bvec[1] - origin[1])*scale])
        Fi = np.linalg.inv(F)
        c = -Fi @ f
        layer = img.transform((cw, ch), Image.AFFINE, (Fi[0, 0], Fi[0, 1], c[0], Fi[1, 0], Fi[1, 1], c[1]), resample=Image.BILINEAR)
        canvas.alpha_composite(layer)
    return canvas

if __name__ == '__main__':
    render({}, ground=1245).save(_os.path.join(_WORK,'rest.png'))
