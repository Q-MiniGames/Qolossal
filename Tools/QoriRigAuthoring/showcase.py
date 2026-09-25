import os as _os
_HERE=_os.path.dirname(_os.path.abspath(__file__))
_PROJECT=_os.path.abspath(_os.path.join(_HERE,'..','..'))
_WORK=_os.path.join(_HERE,'work')
_os.makedirs(_WORK,exist_ok=True)
from PIL import Image, ImageDraw, ImageFont
import anim, rig, clips
C = clips.build_all()
seq = [('Idle',1.6,.1),('Walk',1.24,.12),('Run',1.6,.12),('AttackFront',.62,.05),('Run',.8,.14),('Rise',.45,.1),('AttackAirFront',.55,.05),('Fall',.5,.1),('Land',.26,.03),('AttackUp',.6,.05),('Idle',.8,.14),('Hang',1.4,.1),('Fall',.4,.1),('Land',.26,.03),('Idle',.6,.12)]
fps=30
def pose(name, t, dur):
    c=C[name]
    if name.startswith('Attack'): return anim.pose_at(c, min(t/dur,1)*c['length'])
    if c['loop']: return anim.pose_at(c, t % c['length'])
    return anim.pose_at(c, min(t, c['length']))
frames=[]; prev=None; font=ImageFont.load_default()
for name,dur,bl in seq:
    n=round(dur*fps)
    for i in range(n):
        t=i/fps; p=pose(name,t,dur)
        if prev is not None and t<bl: p=anim.blend(prev(t),p,t/bl)
        im=rig.render(p,size=(1500,1330),origin=(-50,20),ground=1245,scale=.3,sprites=({'WeaponMount':None} if name=='Hang' else None)).convert('RGB')
        ImageDraw.Draw(im).text((10,8),name,fill=(230,230,210),font=font)
        frames.append(im)
    last_name,last_dur=name,dur
    prev=(lambda nm,d: (lambda t: pose(nm,d+t,d) if not C[nm]['loop'] else pose(nm,d+t,d)))(name,dur)
    if name.startswith('Attack') or not C[name]['loop']: prev=(lambda nm,d:(lambda t: pose(nm,d,d)))(name,dur)
frames[0].save(_os.path.join(_WORK,'qori_rig_preview.gif'),save_all=True,append_images=frames[1:],duration=int(1000/fps),loop=0,optimize=True)
print(len(frames), frames[0].size)
