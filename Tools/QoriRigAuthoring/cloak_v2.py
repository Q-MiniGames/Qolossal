"""Qori's cloak v2 (Codex Reviews 15-18): the shoulder mantle as its own part, so the camera-side arm
comes out from under it and the shoulders flow into the cloak as one garment.

Applied to the rig data after export.py has built it (export.py calls apply()), or on its own to
patch the current QoriRigData.json:  python cloak_v2.py

What changes:
- Parts: Torso becomes the bare torso (same canvas and pivot), Cape1..3 and their lower halves
  become the re-hung cloak panels, and a new Mantle part is added. The PNGs are copied from the
  accepted Codex staging folder (Player/QoriCloak) into Parts/.
- Bones: a Mantle bone on the torso, the cape bones at the panels' new rest positions, tints
  and draw orders from QORI_CLOAK_REGISTRATION.json.
- Clips: every cape rotation track is offset so each panel hangs at its registered rest angle
  in the idle pose and keeps the clip's motion relative to idle (Codex's assembly rule).
- Draw order, back to front: cloak panels, weapon arm and legs, torso, skirt, the glide arm (11/12,
  QoriAnimator), camera-side upper arm (13), mantle (14), ears (15/16), head (17), the blade in the
  camera-side hand (18) and its fist (19). The ledge-hang reach arm (18/19) is drawn in front of
  the head, as before, so its elbow never disappears into the head.
"""
import json, os, shutil, sys

_HERE = os.path.dirname(os.path.abspath(__file__))
_PROJECT = os.path.abspath(os.path.join(_HERE, '..', '..'))
SOURCE = os.path.join(_PROJECT, 'Tools', 'IncomingArt', 'Codex_v2', 'Player', 'QoriCloak')
OUT = os.path.join(_PROJECT, 'Assets', 'Art', 'Characters', 'QoriRig')

ORDERS = {'UpperArmFar': 13, 'Mantle': 14, 'EarUpper': 15, 'EarLower': 16, 'Head': 17, 'ForearmFar': 19,
          'UpperArmReachFar': 18, 'ForearmReachFar': 19}
WEAPON_ORDER_FAR = 18   # the blade in the camera-side hand: between its upper arm and its fist
FILES = {'Torso': 'Qori_Torso', 'Mantle': 'Qori_Mantle'}
for i in (1, 2, 3):
    FILES[f'Cape{i}'] = f'Qori_Cloak{i}'; FILES[f'Cape{i}Lower'] = f'Qori_Cloak{i}Lower'


def apply(data, copy_art=True):
    if data.get('cloakVersion') == 2:
        return data
    reg = json.load(open(os.path.join(SOURCE, 'QORI_CLOAK_REGISTRATION.json'), encoding='utf-8'))
    by_part = {r['name'][len('Qori_'):].replace('Cloak', 'Cape'): r for r in reg['parts']}

    # Parts: same rig file names for the torso and capes (other code keys on them), plus the mantle.
    parts = {p['name']: p for p in data['parts']}
    for part, src in FILES.items():
        r = by_part[part]
        dest = 'Parts/' + ('Qori_' + part + '.png')
        if copy_art:
            shutil.copyfile(os.path.join(SOURCE, src + '.png'), os.path.join(OUT, dest))
        entry = parts.get(part) or {'name': part}
        entry['file'] = dest; entry['pivot'] = [float(r['pivotUnity'][0]), float(r['pivotUnity'][1])]
        if part not in parts:
            data['parts'].append(entry); parts[part] = entry
    data['parts'].sort(key=lambda p: p['name'])

    # Bones: rest positions, tints and orders of the cape panels; the mantle on the torso.
    bones = data['bones']
    idle = next(c for c in data['clips'] if c['name'] == 'Idle')
    idle_rot = {t['path']: t['v'][0] for t in idle['tracks'] if t['attr'] == 'localEulerAnglesRaw.z'}
    offsets = {}
    for b in bones:
        if b['name'].startswith('Cape'):
            r = by_part[b['name']]
            b['pos'] = [float(r['restPosition'][0]), float(r['restPosition'][1])]
            b['order'] = int(r['drawOrder']); b['tint'] = float(r['runtimeTint'])
            offsets[b['path']] = float(r['restRotationDegrees']) - idle_rot.get(b['path'], 0.0)
        if b['name'] in ORDERS:
            b['order'] = ORDERS[b['name']]
    if not any(b['name'] == 'Mantle' for b in bones):
        m = by_part['Mantle']
        at = next(i for i, b in enumerate(bones) if b['name'] == 'Torso') + 1   # parents precede children
        bones.insert(at, dict(name='Mantle', parent='Torso', path='Body/Torso/Mantle',
                              pos=[float(m['restPosition'][0]), float(m['restPosition'][1])], rot=0.0,
                              part='Mantle', order=ORDERS['Mantle'], tint=float(m['runtimeTint'])))

    # Clips: each panel at its registered rest angle in idle, moving as the clip moves it.
    for c in data['clips']:
        for t in c['tracks']:
            if t['attr'] == 'localEulerAnglesRaw.z' and t['path'] in offsets:
                t['v'] = [round(v + offsets[t['path']], 5) for v in t['v']]

    data['weaponOrderFar'] = WEAPON_ORDER_FAR
    data['cloakVersion'] = 2
    return data


if __name__ == '__main__':
    path = os.path.join(OUT, 'QoriRigData.json')
    data = json.load(open(path, encoding='utf-8'))
    if data.get('cloakVersion') == 2:
        sys.exit('QoriRigData.json already has cloak v2')
    apply(data)
    json.dump(data, open(path, 'w'), separators=(',', ':'))
    print('cloak v2 applied to', path)
