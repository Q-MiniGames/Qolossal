import os as _os
_HERE=_os.path.dirname(_os.path.abspath(__file__))
_PROJECT=_os.path.abspath(_os.path.join(_HERE,'..','..'))
_WORK=_os.path.join(_HERE,'work')
_os.makedirs(_WORK,exist_ok=True)
import sys
from PIL import Image
import anim, rig, clips
C=clips.build_all()
def at(name, times, crop=(150,150,1400,1330), scale=.3):
    fr=[]
    for t in times:
        im=rig.render(anim.pose_at(C[name],t),size=(1500,1330),origin=(0,0),ground=1245,scale=1).crop(crop)
        fr.append(im.resize((int(im.width*scale),int(im.height*scale)),Image.LANCZOS))
    out=Image.new('RGBA',(fr[0].width*len(fr),fr[0].height))
    for i,f in enumerate(fr): out.paste(f,(i*f.width,0))
    out.save(_os.path.join(_WORK,f'k_{name}.png'))
name=sys.argv[1]; times=[float(x) for x in sys.argv[2:]]
at(name,times)
