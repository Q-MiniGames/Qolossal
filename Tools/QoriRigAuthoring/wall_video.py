"""Preview video of the wall slide / wall jumps / ledge hang + climb in world space."""
import os, sys, subprocess, math
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import rig, anim, clips
C = clips.build_all()
PX = 1/0.001464     # rig px per world unit
FPS = 30; S = .32; W, H = 900, 640
OUT = sys.argv[1] if len(sys.argv) > 1 else 'qori_wall_ledge_preview.mp4'
tmp = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'work', 'wallvid'); os.makedirs(tmp, exist_ok=True)
for f in os.listdir(tmp): os.remove(os.path.join(tmp, f))
frames = []
def pose(name, t): c = C[name]; return anim.pose_at(c, t % c['length'] if c['loop'] else min(t, c['length']))
def emit(p, wx, wy, facing, scene, spr=None, label=''):
    frames.append((p, wx, wy, facing, scene, spr or {}, label))
# ---------------- scene 1: wall slide, climb the wall with wall jumps, jump off
wall = ('wall', 3.0)          # world x of wall face (to the right)
x, y = 3.0 - .4 - .01, 3.0
SCENE = 'wall'
prev = None
def blend_to(p, q, k): return anim.blend(p, q, k) if p is not None else q
def run(name, dur, dy_fn, facing, fx=None, spr=None, label=''):
    global x, y, prev
    n = int(dur*FPS)
    for i in range(n):
        t = i/FPS
        q = pose(name, t if name != 'LedgeClimb' else t/dur)
        k = min(1, (i+1)/3)
        p = blend_to(prev, q, k) if i < 3 else q
        y += dy_fn(t)/FPS
        if fx: x += fx(t)/FPS
        emit(p, x, y, facing, SCENE, spr, label)
    prev = p
open_hand = {'ForearmFar': 'ForearmOpen', 'WeaponMount': None}
run('WallSlide', .9, lambda t: -2.5, 1, spr=open_hand, label='wall slide')
for j in range(2):
    run('WallJumpUp', .42, lambda t: 11 - 30*t, 1, spr=open_hand, label='wall jump (climb)')
    run('WallSlide', .5, lambda t: -2.5*min(1, t*4), 1, spr=open_hand, label='wall slide')
run('WallJumpOff', .45, lambda t: 11 - 30*t, -1, fx=lambda t: -8, spr={'WeaponMount': None}, label='wall jump away')
run('Fall', .4, lambda t: -3 - 20*t, -1, fx=lambda t: -5, spr={'WeaponMount': None}, label='')
# ---------------- scene 2: ledge hang + climb (climb speed 5 u/s as in PlayerMovement)
prev = None
SCENE = 'ledge'
ledge_c = (6.0, 3.0)       # world ledge corner
x = ledge_c[0] - .4 - .01 - 0.0
# hang position: collider top = corner.y - .06
y = ledge_c[1] - .06 - .6
run('Fall', .25, lambda t: 0, 1, spr={'WeaponMount': None}, label='')
y = ledge_c[1] - .06 - .6
run('LedgeHang', 1.0, lambda t: 0, 1, spr={'WeaponMount': None}, label='ledge hang')
dur = (1.295 + .88)/5
def climb_dx(t): return 0 if t/dur < .65*1.295/2.175 else .88/(dur*.88/2.175)
def climb_dy(t): return 1.295/(dur*1.295/2.175) if t/dur < 1.295/2.175 else 0
# progress in PlayerMovement: .65 at the end of the vertical part
n = int(dur*FPS)+1
x0, y0 = x, y
for i in range(n+1):
    t = min(i/FPS, dur); d = 5*t
    up = min(d, 1.295); ac = max(0, d-1.295)
    prog = .65*up/1.295 + .35*ac/.88
    q = anim.pose_at(C['LedgeClimb'], prog)
    frames.append((q, x0 + ac, y0 + up, 1, 'ledge', {'WeaponMount': None, 'ForearmFar': 'ForearmFar' if prog < .6 else 'ForearmOpen'}, f'ledge climb (progress {prog:.2f})'))
x, y = x0 + .88, y0 + 1.295 - .035
prev = frames[-1][0]
run('Idle', .6, lambda t: 0, 1, spr={'WeaponMount': None}, label='')
# ---------------- render
def world_to_px(wx, wy, cam):
    return (W/2 + (wx-cam[0])*PX*S, H/2 - (wy-cam[1])*PX*S)
for i, (p, wx, wy, facing, scene, spr, label) in enumerate(frames):
    cam = (wx, wy+.3)
    img = rig.render(p, size=(1600, 1700), origin=(0, 0), scale=S, sprites=spr, bg=(0, 0, 0, 0))
    # rig px origin (0,0 y-down) -> world: root rig px (591,665) at player + (-.12,.24)
    canvas = Image.new('RGBA', (W, H), (58, 62, 70, 255)); d = ImageDraw.Draw(canvas)
    if scene == 'wall':
        wx0, _ = world_to_px(3.0, 0, cam)
        if wx0 < W: d.rectangle((max(0, wx0), 0, W, H), fill=(92, 84, 70))
    else:
        cx, cy = world_to_px(*ledge_c, cam)
        if cx < W and cy < H: d.rectangle((max(0, cx), max(0, cy), W, H), fill=(92, 84, 70))
    if facing < 0: img = img.transpose(Image.FLIP_LEFT_RIGHT)
    # where does rig px (0,0) land?  root rig px (591,665) is at player + (-.12*facing, .24)
    rx, ry = world_to_px(wx - .12*facing, wy + .24, cam)
    ox = rx - 591*S if facing > 0 else rx - (1600 - 591)*S
    oy = ry - 665*S
    canvas.alpha_composite(img, (int(round(ox)), int(round(oy)))) if ox > -2000 else None
    d.text((10, 10), label, fill=(255, 255, 255))
    canvas.convert('RGB').save(f'{tmp}/f{i:04d}.png')
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(FPS), '-i', f'{tmp}/f%04d.png', '-vf', 'tpad=stop_mode=clone:stop_duration=0.5',
                '-pix_fmt', 'yuv420p', '-c:v', 'libx264', '-crf', '20', OUT], check=True)
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(FPS//3), '-i', f'{tmp}/f%04d.png',
                '-pix_fmt', 'yuv420p', '-c:v', 'libx264', '-crf', '20', OUT.replace('.mp4', '_slow.mp4')], check=True)
print(len(frames), 'frames ->', OUT)
