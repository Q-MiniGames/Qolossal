"""Timing-accurate preview of the sword combo + up/down attacks, with a blade-tip trail."""
import os as _os
_HERE=_os.path.dirname(_os.path.abspath(__file__))
_PROJECT=_os.path.abspath(_os.path.join(_HERE,'..','..'))
_WORK=_os.path.join(_HERE,'work')
_os.makedirs(_WORK,exist_ok=True)
import numpy as np, subprocess, os, shutil
from PIL import Image, ImageDraw
import anim, rig, clips
C = clips.build_all()
FPS = 60
def at_phase(name, t, su, ac, rc):
    if t < su: n = .30*t/su
    elif t < su+ac: n = .30 + .25*(t-su)/ac
    else: n = .55 + .449*min((t-su-ac)/rc, 1)
    return anim.pose_at(C[name], n), su <= t < su+ac
def tip(pose):
    M = rig.solve(pose)['WeaponMount']; return M[:, :2] @ np.array([740.0, 0]) + M[:, 2]
# (clip, startup, active, recovery, fraction of recovery shown before the next one starts)
seq = [('Idle', .5), ('AttackFront', .16, .18, .23, .25), ('AttackFront2', .09, .13, .12, .25),
       ('AttackFront3', .07, .13, .14, .25), ('AttackFront4', .22, .30, .38, 1.0), ('Idle', .6),
       ('AttackUp', .16, .18, .23, 1.0), ('Idle', .5), ('AttackDown', .16, .18, .23, 1.0), ('Idle', .4)]
frames = []; prev = None; trail = []
for item in seq:
    name = item[0]
    if len(item) == 2:
        dur = item[1]; n = round(dur*FPS)
        poses = [(anim.pose_at(C[name], (i/FPS) % C[name]['length']), False) for i in range(n)]
    else:
        _, su, ac, rc, frac = item; dur = su+ac+rc*frac; n = round(dur*FPS)
        poses = [at_phase(name, i/FPS, su, ac, rc) for i in range(n)]
    for i, (p, active) in enumerate(poses):
        t = i/FPS; bl = .05 if name.startswith('Attack') else .12
        if prev is not None and t < bl: p = anim.blend(prev, p, t/bl)
        tp = tip(p)
        trail = [(x, y, a-1) for x, y, a in trail if a > 1] + ([(tp[0], tp[1], 8)] if active else [])
        im = rig.render(p, size=(1700, 1450), origin=(150, -100), ground=1245, scale=.42, bg=(58, 62, 70))
        d = ImageDraw.Draw(im, 'RGBA')
        pts = [((x-150)*.42, (-y+100)*.42, a) for x, y, a in trail]
        for (x0, y0, a0), (x1, y1, a1) in zip(pts, pts[1:]):
            d.line([(x0, y0), (x1, y1)], fill=(235, 245, 170, int(30*a1)), width=int(2+a1*1.5))
        d.text((8, 8), name, fill=(230, 230, 210, 255))
        frames.append(im.convert('RGB'))
    prev = poses[-1][0]
tmp = _os.path.join(_WORK,'combo_frames'); shutil.rmtree(tmp, ignore_errors=True); os.makedirs(tmp)
for i, f in enumerate(frames): f.save(f'{tmp}/{i:04d}.png')
out = _os.path.join(_WORK,'qori_attacks_preview.mp4')
subprocess.run(['ffmpeg', '-loglevel', 'error', '-y', '-framerate', str(FPS), '-i', f'{tmp}/%04d.png', '-pix_fmt', 'yuv420p',
                '-vf', 'scale=trunc(iw/2)*2:trunc(ih/2)*2', '-movflags', 'faststart', out], check=True)
print(len(frames), out)
