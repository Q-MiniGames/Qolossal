"""Export parts + skeleton + baked clips for the Unity builder (QoriRigData.json)."""
import os as _os
_HERE=_os.path.dirname(_os.path.abspath(__file__))
_PROJECT=_os.path.abspath(_os.path.join(_HERE,'..','..'))
_WORK=_os.path.join(_HERE,'work')
_os.makedirs(_WORK,exist_ok=True)
import json, os, shutil
import numpy as np
import rig, anim, clips

OUT = _os.path.join(_PROJECT,'Assets','Art','Characters','QoriRig')
os.makedirs(OUT + '/Parts', exist_ok=True)

C = clips.build_all()
data = dict(version=1, pixelsPerUnit=100, weaponRestAngle=rig.WEAPON_REST_ANGLE,
            rootOffset=[float(rig.ROOT[0]), float(rig.ROOT[1])], weaponOrder=int(rig.BONES['WeaponMount']['order']), parts=[], bones=[], clips=[])

used = sorted({b['part'] for b in rig.BONES.values() if b['part'] and b['part'] != 'Weapon'} |
              {'Head_Neutral', 'Head_Up', 'Head_Down', 'Head_Focus'} |
              ({'Head_Blink', 'Head_Hurt', 'Head_Effort', 'ForearmOpen'} & set(rig.PARTS)))
for p in used:
    meta = rig.PARTS[p]
    w, h = meta['size']; px, py = meta['pivot']
    fname = 'Qori_' + p.replace('Near', '').replace('Far', '') + '.png'
    shutil.copy(_os.path.join(_WORK,'parts',p+'.png'), f'{OUT}/Parts/{fname}')
    data['parts'].append(dict(name=p, file=f'Parts/{fname}', pivot=[px/w, 1-py/h]))

def path_of(n):
    chain = []
    while n and n != 'Root':
        chain.append(n); n = rig.BONES[n]['parent']
    return '/'.join(reversed(chain))

rest_local = {}
for n in rig.ORDER:
    if n == 'Root': continue
    b = rig.BONES[n]
    lp = rig.local_pos(n) / 100.0
    rest_local[n] = lp
    data['bones'].append(dict(name=n, parent=b['parent'] or '', path=path_of(n), pos=[float(lp[0]), float(lp[1])],
                              rot=float(rig.WEAPON_REST_ANGLE if n == 'WeaponMount' else 0.0),
                              part=(b['part'] if b['part'] not in (None, 'Weapon') else ''),
                              order=int(b['order']), tint=float(b['tint'])))

for name, clip in C.items():
    tracks = []
    for key, v in clip['tracks'].items():
        bone_name, prop = key.split('.')
        g = clip['tangents'][key]
        if prop == 'rot':
            add = rig.WEAPON_REST_ANGLE if bone_name == 'WeaponMount' else 0.0
            vals, tans, attr = v + add, g, 'localEulerAnglesRaw.z'
        elif prop in ('x', 'y'):
            i = 0 if prop == 'x' else 1
            vals, tans, attr = rest_local[bone_name][i] + v/100.0, g/100.0, f'm_LocalPosition.{prop}'
        else:
            vals, tans, attr = v, g, 'm_LocalScale.' + ('x' if prop == 'sx' else 'y')
        if np.ptp(vals) < 1e-5 and abs(vals[0] - (rest_local[bone_name][0 if prop == 'x' else 1] if prop in ('x', 'y') else (1.0 if prop in ('sx', 'sy') else 0.0))) < 1e-5 and prop != 'rot':
            continue  # skip flat default position/scale tracks
        tracks.append(dict(path=path_of(bone_name), attr=attr,
                           v=[round(float(x), 5) for x in vals], t=[round(float(x), 5) for x in tans]))
    data['clips'].append(dict(name=name, length=clip['length'], loop=clip['loop'],
                               times=[round(float(t), 5) for t in clip['times']], tracks=tracks))

json.dump(data, open(f'{OUT}/QoriRigData.json', 'w'), separators=(',', ':'))
print('clips', [(c['name'], len(c['tracks'])) for c in data['clips']], os.path.getsize(f'{OUT}/QoriRigData.json'))
