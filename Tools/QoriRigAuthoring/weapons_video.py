"""Timing-accurate preview of the mace, spear, whip and sling attacks."""
import os as _os
_HERE=_os.path.dirname(_os.path.abspath(__file__))
_PROJECT=_os.path.abspath(_os.path.join(_HERE,'..','..'))
_WORK=_os.path.join(_HERE,'work')
_os.makedirs(_WORK,exist_ok=True)
import numpy as np, subprocess, os, shutil, math
from PIL import Image, ImageDraw
import anim, rig, clips
C = clips.build_all()
FPS = 60
SPR = {'Mace': 'Weapon_Mace', 'Spear': 'Weapon_Spear', 'Whip': 'Weapon_Whip', 'Sling': None}
def at_phase(name, t, su, ac, rc):
    if t < su: return anim.pose_at(C[name], .30*t/su), 0.0, 'S', t/su
    if t < su+ac: return anim.pose_at(C[name], .30 + .25*(t-su)/ac), 1.0, 'A', (t-su)/ac
    p = min((t-su-ac)/rc, 1); return anim.pose_at(C[name], .55 + .449*p), 0.0, 'R', p
seq = [('Mace', 'Idle', .5), ('Mace', 'Mace_Front1', .34, .20, .42, 1), ('Mace', 'Idle', .3), ('Mace', 'Mace_Up', .34, .20, .42, 1),
       ('Spear', 'Idle', .4), ('Spear', 'Spear_Front1', .17, .12, .23, 1), ('Spear', 'Idle', .3), ('Spear', 'Spear_Up', .17, .12, .23, 1),
       ('Whip', 'Idle', .4), ('Whip', 'Whip_Front1', .23, .28, .30, .3), ('Whip', 'Whip_Front2', .12, .28, .18, .3),
       ('Whip', 'Whip_Front3', .28, .34, .30, 1), ('Whip', 'Idle', .3), ('Whip', 'Whip_Up', .23, .28, .30, 1),
       ('Sling', 'Idle', .4), ('Sling', 'Sling_Throw', .28, .18, .24, 1), ('Sling', 'Idle', .5)]
frames = []; prev = None
for item in seq:
    wpn, name = item[0], item[1]
    if len(item) == 3:
        poses = [(anim.pose_at(C[name], (i/FPS) % C[name]['length']), 0, 'I', 0) for i in range(round(item[2]*FPS))]
    else:
        su, ac, rc, frac = item[2:]
        poses = [at_phase(name, i/FPS, su, ac, rc) for i in range(round((su+ac+rc*frac)*FPS))]
    for i, (p, act, ph, pr) in enumerate(poses):
        t = i/FPS; bl = .05 if name != 'Idle' else .12
        if prev is not None and t < bl: p = anim.blend(prev, p, t/bl)
        im = rig.render(p, size=(1900, 1450), origin=(150, -100), ground=1245, scale=.4, sprites={'WeaponMount': SPR[wpn]})
        d = ImageDraw.Draw(im)
        M = rig.solve(p)['WeaponMount']
        if wpn == 'Whip':   # rough lash: extends during the hit window
            st = .15*pr if ph == 'S' else (.15 + .85*math.sin(pr*math.pi*.8)) if ph == 'A' else (.15+.85*math.sin(math.pi*.8))*(1-pr) if ph == 'R' else 0
            a = M[:, :2] @ np.array([150.0, 0]) + M[:, 2]; b = M[:, :2] @ np.array([150 + 120 + 1100*st, 0]) + M[:, 2]
            d.line([((a[0]-150)*.4, (-a[1]+100)*.4), ((b[0]-150)*.4, (-b[1]+100)*.4)], fill=(110, 140, 50, 255), width=4)
        if wpn == 'Sling':
            c = M[:, 2]; d.ellipse(((c[0]-150)*.4-7, (-c[1]+100)*.4-7, (c[0]-150)*.4+7, (-c[1]+100)*.4+7), outline=(220, 200, 120, 255), width=3)
        d.text((8, 8), f'{wpn}: {name}', fill=(230, 230, 210, 255))
        frames.append(im.convert('RGB'))
    prev = poses[-1][0]
tmp = _os.path.join(_WORK,'wv'); shutil.rmtree(tmp, ignore_errors=True); os.makedirs(tmp)
for i, f in enumerate(frames): f.save(f'{tmp}/{i:04d}.png')
out = _os.path.join(_WORK,'qori_weapons_preview.mp4')
subprocess.run(['ffmpeg', '-loglevel', 'error', '-y', '-framerate', str(FPS), '-i', f'{tmp}/%04d.png', '-pix_fmt', 'yuv420p',
                '-vf', 'scale=trunc(iw/2)*2:trunc(ih/2)*2', '-movflags', 'faststart', out], check=True)
print(len(frames))
