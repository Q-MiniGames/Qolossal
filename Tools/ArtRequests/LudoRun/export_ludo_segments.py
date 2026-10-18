"""Cuts Ludo.ai clips (transparent frame exports) into the separate animations the Qori test scene plays.

Not art: cutting and registration of generated animations. Each segment is a frame range of one Ludo clip with an anchor:
- 'feet': Ludo held the feet still (attacks); frames are not shifted.
- 'head': the game moves Qori (air, walls, ledges), so whatever travel Ludo baked into the frames is removed: the face is
  held where it sits, relative to the feet, in that clip's standing reference frame.
- 'center': like 'head', for poses with no face to find (the curled flip): the body's box centre is held instead.
- 'facex': on the ground, but Ludo's sideways drift is removed: the face is held at one column (the body's box centre,
  offset like the reference frame's, where no face shows), the feet stay on the ground row. The game moves Qori instead
  (the combo's step forward), so nothing snaps back when the idle returns. The info's faceColumn lets the pivot be set
  so the face sits where the idle's does.
Every segment of a clip is sized from the clip's standing reference frame (ground to the top of the ears), so the
animations match each other and the idle, walk and run. Writes Assets/QoriWalkTest/ArtLudo/<Segment>/ with an info json.
Usage: python export_ludo_segments.py <Ludo dir> <out dir> [segment names to export; default all]
"""
import json, os, sys
import numpy as np
from PIL import Image

LUDO, OUT = sys.argv[1], sys.argv[2]
ONLY = set(sys.argv[3:])
# segment name: (clip folder, first frame, last frame, loop, anchor, reference standing frame); first may be a list of frames
SEGMENTS = {
    # the jump redo (14 Oct: face steady, no mouth) loops: squat 11-28, push off 24-31, rise 32-35 and 0-3, apex 4-6, fall 7-10
    'JumpTakeoff': ('Ludo-Qori-Jump (Redo)', 26, 31, False, 'feet', 0),   # on the ground: feet held
    'JumpRise':    ('Ludo-Qori-Jump (Redo)', [32, 33, 34, 35, 0, 1, 2, 3], None, True, 'head', 0),
    'JumpFall':    ('Ludo-Qori-Jump (Redo)', 5, 10, True, 'head', 0),
    'JumpLand':    ('Ludo-Qori-Jump (Redo)', 11, 17, False, 'feet', 0),   # on the ground: feet held
    'Dash':        ('Ludo-Qori-Dash & Glide', 5, 16, True, 'head', 0),
    'GlideOpen':   ('Ludo-Qori-Dash & Glide', 17, 23, False, 'head', 0),
    'Glide':       ('Ludo-Qori-Dash & Glide', 24, 35, True, 'head', 0),
    # the wall slide redo (16 Oct): the reference pose held against the wall (Ludo drew no wall); the first clip's
    # 6-19 is superseded
    'WallSlide':   ('Ludo-Qori-Wall slide (redo)', 0, 35, True, 'head', 0),
    'WallJump':    ('Ludo-Qori-Wall', 20, 31, False, 'head', 0),
    # the ledge redo (16 Oct, side-on; the drawn cliff erased into 'Ledge (redo, cliff erased)'): 0-8 still show the
    # reference pose it started from, so the hang is 9-12 (one hand on the edge, body dangling; played back and forth)
    # and the climb, crouch and stand 12-35. The first clip (12-35) is superseded.
    'LedgeHang':   ('Ludo-Qori-Ledge (redo, cliff erased)', 9, 12, True, 'head', 0),   # 0-8 are the reference pose (the user: not a hang); 9-12 hang from the edge
    'LedgeClimb':  ('Ludo-Qori-Ledge (redo, cliff erased)', 12, 35, False, 'head', 0),
    'Slash':       ('Ludo-Qori-Slash', 4, 35, False, 'feet', 0),
    # prompt 7 (14 Oct): staff raised overhead (Ludo holds it for a second; only the raise is kept), swing, follow-through
    'Strike':      ('Ludo-Qori-Forward Strike', [3, 5, 7, 9, 19, 20, 21, 22, 23, 24, 25, 26, 27, 29, 31], None, False, 'feet', 0),
    # attack set A1-A4 (15 Oct). A1: pull back 3-6, sweep 7-11, reach 12-15 | backswing 16-21 | spin finisher 22-29, settle 30-33
    'Combo1':        ('Ludo-Qori-Attack A1', 3, 15, False, 'facex', 0),
    'Combo2':        ('Ludo-Qori-Attack A1', 16, 21, False, 'facex', 0),
    'Combo3':        ('Ludo-Qori-Attack A1', 22, 33, False, 'facex', 0),
    # A2: staff raised straight up 1-12 (its flip, 16-28, is not used: the double jump gets its own Ludo clip)
    # the up attack (15 Oct, the user's second take, 'arc from behind'): staff brought back and low 3-12, the arc from
    # behind over his head 13-21, down to the front 22-25, held 26-35. Every other wind-up frame, so it stays quick.
    'UpAttack':      ('Ludo-Qori-Upper Attack', [10, 12, 13, 15, 17, 18, 19, 20, 21, 22, 24], None, False, 'feet', 0),
    # the double jump (15 Oct). Take 1 ('Ludo-Qori-Double Jump' 12-19) turned to the camera after its flip; take 2, used:
    # ready 0-4, jump 5-9, a forward flip in profile 10-21 (head down at 15-19), upright 22, landing 23-35 (unused)
    'DoubleJump':    ('Ludo-Qori-Double Jump 2', 10, 22, False, 'center', 0),
    # the plunge (15 Oct): stance 0-6, hop 7-8, turning into the dive 9-10, the dive with the staff down 11-18,
    # impact 19, crouch in a burst of leaves 20-27, held 28-35 (unused)
    # the evade (16 Oct): crouch 0-6, the roll 7-18, rising 19-25, standing 26-35; every other frame 6-22 so the whole
    # roll fits the quarter-second evade
    'Evade':         ('Ludo-Qori-Dodge roll', [6, 8, 10, 12, 14, 16, 18, 20, 22], None, False, 'feet', 0),
    # the rope hang (16 Oct): swinging on a vine (the vine above the hands removed: the game draws its own thread)
    'RopeHang':      ('Ludo-Qori-Rope hang (vine removed)', 0, 35, True, 'head', 0),
    # the staff shot (16 Oct, replaces the sling): aim 0-9, the tip glows 10-16, the bolt leaves 16 (cut out as the
    # projectile, Resources/Armory/LeafBolt.png; removed from these frames), recovery 17-35
    'StaffShot':     ('Ludo-Qori-Staff shot (bolt removed)', 0, 35, False, 'feet', 0),
    'PlungeStart':   ('Ludo-Qori-Down Attack', 7, 10, False, 'center', 0),
    'PlungeDive':    ('Ludo-Qori-Down Attack', 11, 18, True, 'center', 0),
    'PlungeLand':    ('Ludo-Qori-Down Attack', 19, 27, False, 'feet', 0),
    # A3: last run step 6, lunge 7, spin 8-11, skid with dust 12-20
    'SprintAttack':  ('Ludo-Qori-Attack A3', 6, 20, False, 'feet', 0),
    # A4: crouch and pull back 0-7 | leaves swirl at the tip 8-18 (held) | release, the leaf ring, scatter 19-31
    'ChargeStart':   ('Ludo-Qori-Attack A4', 0, 7, False, 'feet', 0),
    'ChargeHold':    ('Ludo-Qori-Attack A4', 8, 18, True, 'feet', 0),
    'ChargeRelease': ('Ludo-Qori-Attack A4', 19, 31, False, 'feet', 0),
}


def load(folder, i):
    return np.array(Image.open(os.path.join(LUDO, folder, 'frame_%03d.png' % i)).convert('RGBA'))


def body_mask(a):
    """Alpha of the body only: the piece grown from the opaque pixel nearest the figure's centre (drops loose leaves)."""
    m = a[:, :, 3] > 40
    ys, xs = np.where(a[:, :, 3] > 250)
    k = int(np.argmin((ys - ys.mean()) ** 2 + (xs - xs.mean()) ** 2))
    reg = np.zeros_like(m); reg[ys[k], xs[k]] = True
    while True:
        g = reg.copy(); g[1:] |= reg[:-1]; g[:-1] |= reg[1:]; g[:, 1:] |= reg[:, :-1]; g[:, :-1] |= reg[:, 1:]; g &= m
        if g.sum() == reg.sum(): return reg
        reg = g


def face(a, m):
    r = np.where(m.any(1))[0]; top, bot = r.min(), r.max()
    cream = m & (a[:, :, :3].min(axis=2) > 200); cream[int(top + .45 * (bot - top)):] = False
    ys, xs = np.where(cream)
    return float(np.median(xs)), float(np.median(ys))


def standing_ref(folder):
    """The clip's tallest frame with its feet on the clip's ground row: its standing size (crouches and air poses excluded)."""
    lows, tops = [], []
    for i in range(36):
        m = load(folder, i)[:, :, 3] > 128; r = np.where(m.any(1))[0]; lows.append(r.max()); tops.append(r.min())
    ground = int(np.bincount(lows).argmax())
    cand = [i for i in range(36) if abs(lows[i] - ground) <= 6]
    return max(cand, key=lambda i: lows[i] - tops[i])


summary = {}
for name, (folder, f0, f1, loop, anchor, ref) in SEGMENTS.items():
    if ONLY and name not in ONLY: continue
    ref = standing_ref(folder)
    ra = load(folder, ref); rm = body_mask(ra)
    rr = np.where(rm.any(1))[0]; ref_ground, ref_top = int(rr.max()), int(rr.min())
    xs = np.where(rm[ref_ground - 15:ref_ground + 1].any(0))[0]; ref_pivot = (xs.min() + xs.max()) / 2
    ref_face = face(ra, rm)
    rys, rxs = np.where(rm); ref_box = ((rxs.min() + rxs.max()) / 2, (rys.min() + rys.max()) / 2)
    H, W = ra.shape[:2]; PAD = 160
    CW, CH = W + 2 * PAD, H + 2 * PAD
    ground, pivot = ref_ground + PAD, int(round(ref_pivot)) + PAD
    os.makedirs(os.path.join(OUT, name), exist_ok=True)
    frames = list(f0) if isinstance(f0, list) else list(range(f0, f1 + 1))
    for k, i in enumerate(frames):
        a = load(folder, i)
        dx = dy = 0.0
        if anchor == 'head':
            m = body_mask(a); fx, fy = face(a, m)
            dx, dy = ref_face[0] - fx, ref_face[1] - fy
        elif anchor == 'facex':
            m = body_mask(a); ys, xs = np.where(m)
            fx = face(a, m)[0] if (m & (a[:, :, :3].min(axis=2) > 200)).any() else float('nan')
            box = (xs.min() + xs.max()) / 2 + ref_face[0] - ref_box[0]
            if not np.isfinite(fx) or abs(fx - box) > 70: fx = box   # no face (the spin's back view): the body box
            dx = ref_face[0] - fx
        elif anchor == 'center':
            m = body_mask(a); ys, xs = np.where(m)
            dx, dy = ref_box[0] - (xs.min() + xs.max()) / 2, ref_box[1] - (ys.min() + ys.max()) / 2
        a = a.copy(); a[a[:, :, 3] == 0, :3] = 0
        out = Image.new('RGBA', (CW, CH), (0, 0, 0, 0))
        out.paste(Image.fromarray(a), (int(round(PAD + dx)), int(round(PAD + dy))))
        out.save(os.path.join(OUT, name, 'Qori_%s_%02d.png' % (name, k + 1)))
    info = dict(name=name, source=os.path.join(LUDO, folder), frames=frames, frameCount=len(frames), loop=loop, anchor=anchor,
                canvas=[CW, CH], groundRow=ground, pivotColumn=pivot, faceColumn=round(ref_face[0] + PAD, 1), figureHeightPx=ref_ground - ref_top + 1, referenceFrame=ref, strideCanvasPx=0)
    json.dump(info, open(os.path.join(OUT, name, 'Qori_%s_info.json' % name), 'w'), indent=1)
    summary[name] = (info['frameCount'], info['figureHeightPx'], ref)
print(json.dumps(summary))
