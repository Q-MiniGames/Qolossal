"""Ledge-hang / climb preview with the ledge drawn in: python ledge_preview.py [out.png] [t ...]"""
import os as _os, sys
_HERE=_os.path.dirname(_os.path.abspath(__file__))
from PIL import Image, ImageDraw
import anim, rig, clips
C = clips.build_all()
LX, LY = clips.LEDGE          # ledge corner, rig px y-up (while hanging)
REACH_UNTIL = .65   # LedgeClimb progress where QoriAnimator swaps back to the normal arms (keep in sync)
def arm_overrides(name, t):
    reach = name == 'LedgeHang' or (name == 'LedgeClimb' and t < REACH_UNTIL)
    hide = [b for b in rig.BONES if ('Arm' in b or 'Forearm' in b) and (('Reach' in b) != reach)]
    if name == 'LedgeHang' or (name == 'LedgeClimb' and t < .74): hide.append('WeaponMount')
    return {b: None for b in hide}
def frame(name, t, sprites=None):
    sprites = {**arm_overrides(name, t), **(sprites or {})}
    pose = anim.pose_at(C[name], t)
    im = rig.render(pose, size=(1500, 2100), origin=(0, -300), ground=None, scale=1, sprites=sprites, bg=(150, 170, 175))
    d = ImageDraw.Draw(im, 'RGBA')
    rise = 0 if name == 'LedgeHang' else clips.LEDGE_RISE*min(t/.65, 1)
    across = 0 if name == 'LedgeHang' else clips.LEDGE_ACROSS*max(0, (t-.65)/.35)
    x, y = LX - across, -(LY - rise) + 300   # corner in canvas px (y down, origin shifted by 300)
    d.rectangle([x, y, 1500, 2100], fill=(70, 80, 70, 150), outline=(20, 30, 20, 255), width=3)
    return im.crop((250, 0, 1450, 2100)).resize((400, 700), Image.LANCZOS)
if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else _os.path.join(_HERE, 'work', 'ledge_preview.png')
    shots = [('LedgeHang', 0), ('LedgeHang', .9), ('LedgeClimb', .1), ('LedgeClimb', .3), ('LedgeClimb', .5), ('LedgeClimb', .7), ('LedgeClimb', .9)]
    frames = [frame(n, t) for n, t in shots]
    sheet = Image.new("RGBA", (400*len(frames), 700))
    for i, f in enumerate(frames): sheet.paste(f, (i*400, 0))
    sheet.save(out)
