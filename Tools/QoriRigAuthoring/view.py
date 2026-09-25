import os as _os
_HERE=_os.path.dirname(_os.path.abspath(__file__))
_PROJECT=_os.path.abspath(_os.path.join(_HERE,'..','..'))
_WORK=_os.path.join(_HERE,'work')
_os.makedirs(_WORK,exist_ok=True)
import sys
from PIL import Image
import anim, rig, clips
def strip(clip, n=6, crop=(250,0,1300,1330), scale=.36, t0=0, t1=None):
    t1 = clip['length'] if t1 is None else t1
    fr=[]
    for i in range(n):
        t = t0+(t1-t0)*i/(n if clip['loop'] else n-1)
        im = rig.render(anim.pose_at(clip, t), size=(1300,1330), origin=(0,0), ground=1245, scale=1).crop(crop)
        im = im.resize((int(im.width*scale), int(im.height*scale)), Image.LANCZOS)
        fr.append(im)
    out = Image.new('RGBA',(fr[0].width*n, fr[0].height))
    for i,f in enumerate(fr): out.paste(f,(i*f.width,0))
    return out
if __name__=='__main__':
    C=clips.build_all()
    for a in sys.argv[1:]:
        strip(C[a]).save(_os.path.join(_WORK,f'v_{a}.png'))
