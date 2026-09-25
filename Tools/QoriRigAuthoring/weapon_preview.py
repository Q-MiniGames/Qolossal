import os as _os
_HERE=_os.path.dirname(_os.path.abspath(__file__))
_PROJECT=_os.path.abspath(_os.path.join(_HERE,'..','..'))
_WORK=_os.path.join(_HERE,'work')
_os.makedirs(_WORK,exist_ok=True)
import sys, math, os, shutil, subprocess
import numpy as np
from PIL import Image, ImageDraw
import anim, rig, clips
C = clips.build_all()
LEN = {'Mace': 650, 'Spear': 750, 'Whip': 150, 'Sling': 0, 'Sword': 740}
SPR = {'Mace': 'Weapon_Mace', 'Spear': 'Weapon_Spear', 'Whip': 'Weapon_Whip', 'Sling': None}
def draw(name, t, weapon, active=False, stretch=0.0):
    p = anim.pose_at(C[name], t)
    im = rig.render(p, size=(1800, 1500), origin=(150, -150), ground=1245, scale=.4, sprites={'WeaponMount': SPR.get(weapon, 'Weapon')})
    d = ImageDraw.Draw(im)
    M = rig.solve(p)['WeaponMount']
    if weapon == 'Whip':
        a = M[:, :2] @ np.array([150.0, 0]) + M[:, 2]; b = M[:, :2] @ np.array([150.0 + 1100*stretch + 120, 0]) + M[:, 2]
        d.line([((a[0]-150)*.4, (-a[1]+150)*.4), ((b[0]-150)*.4, (-b[1]+150)*.4)], fill=(110, 140, 50, 255), width=4)
    if weapon == 'Sling':
        c = M[:, 2]; d.ellipse(((c[0]-150)*.4-6, (-c[1]+150)*.4-6, (c[0]-150)*.4+6, (-c[1]+150)*.4+6), outline=(220, 200, 120, 255), width=2)
    return im
def sheet(weapon, names, times, out):
    rows = []
    for n in names:
        fr = [draw(n, t, weapon, stretch=max(0, math.sin(min(max((t-.3)/.25, 0), 1)*math.pi*.8))) for t in times]
        w, h = fr[0].size; r = Image.new('RGBA', (w*len(fr), h))
        for i, f in enumerate(fr):
            ImageDraw.Draw(f).text((6, 6), f'{n} {times[i]:.2f}', fill=(255, 255, 255, 255)); r.paste(f, (i*w, 0))
        rows.append(r)
    o = Image.new('RGBA', (rows[0].width, sum(r.height for r in rows)))
    y = 0
    for r in rows: o.paste(r, (0, y)); y += r.height
    o.save(out)
if __name__ == '__main__':
    T = [0, .24, .33, .37, .42, .55, .8]
    sheet('Mace', ['Mace_Front1', 'Mace_Up', 'Mace_Down'], T, _os.path.join(_WORK,'w_mace.png'))
    sheet('Spear', ['Spear_Front1', 'Spear_Up', 'Spear_Down'], T, _os.path.join(_WORK,'w_spear.png'))
    sheet('Whip', ['Whip_Front1', 'Whip_Front2', 'Whip_Front3', 'Whip_Up', 'Whip_Down'], T, _os.path.join(_WORK,'w_whip.png'))
    sheet('Sling', ['Sling_Throw'], T, _os.path.join(_WORK,'w_sling.png'))
