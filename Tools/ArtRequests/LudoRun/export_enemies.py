"""Cuts the Ludo.ai clips of the Thornwing, Pod Spitter, Shellback and Ripple Newt into the animations
EnemyFramesOnCreature plays in the Qori Walk Test room.

Not art: cutting and registration of generated animations. Ludo draws each clip at its own size, so every clip of an
enemy is scaled to that enemy's reference frame (the average of the width and height ratios of each clip's first
frame against it), and placed so the first frames share one pivot: the feet centre for walkers and the rooted plant,
the body's box centre for the flyer. Loose bits Ludo adds (the Pod Spitter's flying seed, hit sparks) are dropped:
every opaque piece smaller than 4% of the frame's largest is removed. Each enemy's frames share one canvas.
Writes Assets/QoriWalkTest/ArtEnemies/<Enemy>/<Segment>/<Enemy>_<Segment>_NN.png and _info.json.
Usage: python export_enemies.py <Ludo dir> <out dir> [enemies]
"""
import json, os, sys
import numpy as np
from PIL import Image
from scipy import ndimage

SRC, OUT = sys.argv[1], sys.argv[2]
ONLY = set(sys.argv[3:])

# enemy: (folder, pivot, reference (clip, frame), {segment: (clip, frames)})
ENEMIES = {
    'Thornwing': ('Thornwing', 'center', ('Thornwing - Hover', 0), {
        'Hover': ('Thornwing - Hover', range(0, 18)),               # one wing beat
        'Warn':  ('Thornwing - Warning and dive', range(12, 22)),   # wings raised high, rearing
        'Dive':  ('Thornwing - Warning and dive', range(24, 32)),   # swept back
        'Hurt':  ('Thornwing - Hurt and death', range(4, 9)),       # the jolt
        'Death': ('Thornwing - Hurt and death', range(5, 18)),      # wings fold (the game lets it fall)
    }),
    'PodSpitter': ('Pod Spitter', 'feet', ('Pod Spitter - Idle sway', 0), {
        'Idle':  ('Pod Spitter - Idle sway', range(0, 36)),
        'Swell': ('Pod Spitter - Swell and spit (redo)', range(4, 18)),    # closes into the swollen pod
        'Spit':  ('Pod Spitter - Swell and spit (redo)', range(18, 31)),   # bursts open, the seed out (Ludo's seed dropped)
        'Hurt':  ('Pod Spitter - Hurt and death', range(1, 6)),
        'Death': ('Pod Spitter - Hurt and death', range(6, 25)),           # droops to the ground
    }),
    'Shellback': ('Shellback', 'feet', ('Shellback - Slow walk', 0), {
        'Walk':  ('Shellback - Slow walk', range(0, 36)),
        'Crack': ('Shellback - Shell cracks', range(0, 27)),   # the glow builds, the plates fly
        'CrackedWalk': ('Shellback - Cracked Shell walk', range(0, 36)),   # walking on with the glow showing
        'Hurt':  ('Shellback - Hurt and death', range(3, 9)),  # (already cracked)
        'Death': ('Shellback - Hurt and death', range(8, 21)),
    }),
    'Grub': ('Grub', 'feet', ('Grub - Erupt and bite', 0), {
        'Erupt': ('Grub - Erupt and bite', range(3, 13)),    # rears up out of the soil (Ludo's dirt clods dropped)
        'Up':    ('Grub - Erupt and bite', range(13, 36)),   # the bite, then swaying upright
        'Sink':  ('Grub - Burrow back', range(22, 34)),      # dives headfirst back down
        'Hurt':  ('Grub - Hurt and death', range(3, 9)),
        'Death': ('Grub - Hurt and death', range(10, 26)),   # flattens out
    }),
    'GustMoth': ('Gust Moth', 'center', ('Gust Moth - Lazy hover', 0), {
        'Hover': ('Gust Moth - Lazy hover', range(0, 35)),   # one slow beat (frame 35 is frame 0 again)
        'Raise': ('Gust Moth - Wind blast', range(0, 26)),   # wings rising and held high (the warning)
        'Beat':  ('Gust Moth - Wind blast', range(26, 36)),  # the hard forward beat, then recovering
        'Death': ('Gust Moth - Defeat', range(0, 36)),       # flutters weakly, drifts down, folds its wings
    }),
    'Sentinel': ('Sentinel', 'feet', ('Sentinel - Pacing walk', 0), {
        'Walk':   ('Sentinel - Pacing walk', range(0, 36)),
        'Guard':  ('Sentinel - Attack', range(0, 9)),                  # the stance, shield up
        'Draw':   ('Sentinel - Attack', range(9, 20)),                 # the spear drawn back (the warning)
        'Thrust': ('Sentinel - Attack', range(20, 27)),                # out past the shield
        'Hurt':   ('Sentinel - Hurt and death', range(1, 9)),
        'Death':  ('Sentinel - Hurt and death (redo)', range(11, 36)), # kneels, topples, still holding shield and spear
    }),
    'Newt': ('Newt', 'feet', ('Newt - Leap and land', 0), {
        'Crouch': ('Newt - Leap and land', range(0, 6)),           # surfacing, the warning
        'Leap':   ('Newt - Leap and land', range(6, 25)),          # airborne, nose up, then down
        'Land':   ('Newt - Leap and land', range(25, 36)),
        'Look':   ('Newt - Wait and jump back', range(0, 13)),     # looks round, tail swishing
        'Spring': ('Newt - Wait and jump back', range(13, 36)),    # crouch, spring, airborne (the jump back)
        'Hurt':   ('Newt - Hurt and death', range(1, 8)),
        'Death':  ('Newt - Hurt and death', range(8, 24)),         # onto its back
    }),
}


# segments whose part below the ground line is cut away (the Grub diving into the soil)
BELOW_GROUND_HIDDEN = {('Grub', 'Sink')}


def load(folder, clip, i):
    return np.array(Image.open(os.path.join(SRC, folder, clip, 'frame_%03d.png' % i)).convert('RGBA'))


def clean(a):
    """Drops opaque pieces smaller than 4% of the largest (loose seeds, sparks, crumbs)."""
    m = a[:, :, 3] > 40
    lab, n = ndimage.label(m, structure=np.ones((3, 3)))
    if n <= 1: return a
    sizes = ndimage.sum(m, lab, range(1, n + 1))
    keep = np.zeros(n + 1, bool); keep[1:] = sizes >= .04 * sizes.max()
    a = a.copy(); a[~keep[lab] & m] = 0
    return a


def box(a):
    ys, xs = np.where(a[:, :, 3] > 128)
    return xs.min(), ys.min(), xs.max(), ys.max()


for enemy, (folder, pivot_mode, (ref_clip, ref_i), segments) in ENEMIES.items():
    if ONLY and enemy not in ONLY: continue
    r = box(clean(load(folder, ref_clip, ref_i))); rw, rh = r[2] - r[0], r[3] - r[1]
    place = {}
    for clip in {c for c, _ in segments.values()}:
        b = box(clean(load(folder, clip, 0))); w, h = b[2] - b[0], b[3] - b[1]
        s = (rw / w + rh / h) / 2
        px = (b[0] + b[2]) / 2 * s
        py = (b[3] if pivot_mode == 'feet' else (b[1] + b[3]) / 2) * s
        place[clip] = (s, px, py)   # the clip's pivot, in its scaled pixels
    # first pass: every frame placed round pivot (0, 0); the union box gives the shared canvas
    frames, lo, hi = {}, np.array([1e9, 1e9]), np.array([-1e9, -1e9])
    for seg, (clip, rng) in segments.items():
        s, px, py = place[clip]
        for i in rng:
            a = clean(load(folder, clip, i))
            img = Image.fromarray(a)
            if abs(s - 1) > 1e-3: img = img.resize((round(img.width * s), round(img.height * s)), Image.LANCZOS)
            off = np.array([-px, -py])
            frames.setdefault(seg, []).append((img, off))
            b = box(np.array(img)); lo = np.minimum(lo, off + [b[0], b[1]]); hi = np.maximum(hi, off + [b[2], b[3]])
    pad = 8
    CW, CH = int(hi[0] - lo[0]) + 2 * pad, int(hi[1] - lo[1]) + 2 * pad
    PX, PY = -lo[0] + pad, -lo[1] + pad   # where the pivot sits on the canvas
    for seg, items in frames.items():
        d = os.path.join(OUT, enemy, seg); os.makedirs(d, exist_ok=True)
        for k, (img, off) in enumerate(items):
            canvas = Image.new('RGBA', (CW, CH), (0, 0, 0, 0))
            canvas.alpha_composite(img, (int(round(PX + off[0])), int(round(PY + off[1]))))
            arr = np.array(canvas); arr[arr[:, :, 3] == 0, :3] = 0
            if (enemy, seg) in BELOW_GROUND_HIDDEN: arr[int(round(PY)) + 2:] = 0   # under the soil: not drawn
            Image.fromarray(arr).save(os.path.join(d, '%s_%s_%02d.png' % (enemy, seg, k + 1)))
        clip, rng = segments[seg]
        info = dict(name=enemy + '_' + seg, source=os.path.join(SRC, folder, clip), frames=list(rng), frameCount=len(items),
                    canvas=[CW, CH], groundRow=int(round(PY)), pivotColumn=int(round(PX)), pivot=pivot_mode,
                    figureHeightPx=float(rh), figureWidthPx=float(rw), scale=round(place[clip][0], 4))
        json.dump(info, open(os.path.join(d, '%s_%s_info.json' % (enemy, seg)), 'w'), indent=1)
    print(enemy, {s: len(v) for s, v in frames.items()}, 'canvas', CW, CH, 'ref', rw, rh, {c: round(v[0], 3) for c, v in place.items()})
