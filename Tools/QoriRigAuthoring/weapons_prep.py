"""Prepare Codex's single-weapon paintings for the game (Assets/Resources/Armory/Weapons).

Each PNG is trimmed to its alpha, downscaled to <=1024 px wide, and its grip point
(where Qori's fist closes) written to Grips.txt as a Unity sprite pivot
(x from the left, y from the BOTTOM, both 0..1). The game keeps each weapon's world
length (grip -> blade tip), so hit reach is unchanged.
"""
import os as _os
_HERE=_os.path.dirname(_os.path.abspath(__file__))
_PROJECT=_os.path.abspath(_os.path.join(_HERE,'..','..'))
_WORK=_os.path.join(_HERE,'work')
_os.makedirs(_WORK,exist_ok=True)
import os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import newart

OUT = os.environ.get('QORI_WEAPON_OUT', _os.path.join(_PROJECT,'Assets','Resources','Armory','Weapons'))
os.makedirs(OUT, exist_ok=True)
# name -> (source file, grip x as a fraction of the trimmed width, measured on the handle)
WEAPONS = {
    'LeafSword':   ('Qori_Weapon_LeafSword.png', .17),
    'SeedpodMace': ('Qori_Weapon_SeedpodMace.png', .14),
    'ThornSpear':  ('Qori_Weapon_ThornSpear.png', .10),
    'WhipHandle':  ('Qori_Weapon_WhipHandle.png', .30),
}
MAXW = 1024
def prep():
    rows = []; out = {}
    for name, (f, gx) in WEAPONS.items():
        im = newart.load_new(f)
        bb = im.getchannel('A').point(lambda a: 255 if a > 8 else 0).getbbox()
        im = im.crop((max(bb[0]-4, 0), max(bb[1]-4, 0), min(bb[2]+4, im.width), min(bb[3]+4, im.height)))
        if im.width > MAXW:
            s = MAXW/im.width
            im = im.convert('RGBa').resize((MAXW, round(im.height*s)), Image.LANCZOS).convert('RGBA')
        # handle axis height = centre of the pommel end (hanging leaves don't reach it)
        gy_top = newart.end_point(im, 'left', .03)[1]
        gy = 1 - gy_top/im.height               # Unity pivot y from the bottom
        im.save(f'{OUT}/{name}.png')
        rows.append(f'{name},{gx:.4f},{gy:.4f}')
        out[name] = (im, (gx*im.width, gy_top))
    open(f'{OUT}/Grips.txt', 'w', newline='\r\n').write('\n'.join(rows) + '\n')
    return out
if __name__ == '__main__':
    for n, (im, g) in prep().items(): print(n, im.size, g)
    print(open(f'{OUT}/Grips.txt').read())
