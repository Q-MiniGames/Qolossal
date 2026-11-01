"""Cuts the underground creatures' Ludo.ai clips (Tools/IncomingArt/Ludo/Underground/<ID> - <clip>/frames) into the
segments UndergroundCreatureFrames plays, registered like export_enemies.py: every clip of a creature is scaled to its
reference frame (the average of the width and height ratios of each clip's first frame against it) and placed so the
first frames share one pivot (the feet centre for walkers and rooted creatures, the body's box centre for flyers).
Loose bits Ludo adds (sparks, crumbs, a shard in flight) are dropped: opaque pieces under 4% of the frame's largest.
Each creature's frames share one canvas.
Writes Assets/Art/Underground/Creatures/<ID>/<Segment>/<ID>_<Segment>_NN.png and <ID>/<ID>_info.json.
Not art: cutting and registration of generated animations.
Usage: python export_underground.py [IDs]
"""
import json, os, sys
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
SRC = os.path.join(ROOT, 'Tools', 'IncomingArt', 'Ludo', 'Underground')
OUT = os.path.join(ROOT, 'Assets', 'Art', 'Underground', 'Creatures')
ONLY = set(sys.argv[1:])

def r(a, b): return range(a, b)

# id: (pivot, height in world units (the approved concept), reference (clip, frame), {segment: (clip, frames)})
CREATURES = {
    'E-10': ('center', 1.6, ('Hang and drop', 0), {
        'Idle': ('Hang and drop', r(0, 14)), 'Warn': ('Hang and drop', r(14, 22)), 'Attack': ('Hang and drop', r(22, 30)),
        'Walk': ('Crawl', r(0, 36)), 'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'E-11': ('feet', 1.1, ('Scuttle and guard', 0), {
        'Walk': ('Scuttle and guard', r(0, 18)), 'Guard': ('Scuttle and guard', r(22, 36)), 'Warn': ('Claw snap', r(0, 10)),
        'Attack': ('Claw snap', r(10, 20)), 'Recover': ('Claw snap', r(20, 36)),
        'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'E-12': ('center', 1.8, ('Float and swell', 0), {
        'Idle': ('Float and swell', r(0, 14)), 'Warn': ('Float and swell', r(14, 36)), 'Attack': ('Jet and refill', r(0, 14)),
        'Recover': ('Jet and refill', r(14, 36)), 'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'E-13': ('feet', 1.7, ('Sway and gape', 0), {
        'Idle': ('Sway and gape', r(0, 14)), 'Warn': ('Sway and gape', r(14, 36)), 'Attack': ('Snap', r(0, 18)),
        'Recover': ('Snap', r(18, 36)), 'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'E-14': ('center', 1.6, ('Drift and sting', 0), {
        'Idle': ('Drift and sting', r(0, 14)), 'Warn': ('Drift and sting', r(14, 24)), 'Attack': ('Drift and sting', r(24, 36)),
        'Hurt': ('Scatter', r(0, 8)), 'Death': ('Scatter', r(0, 20))}),
    'E-15': ('feet', 1.3, ('Crawl', 0), {
        'Walk': ('Crawl', r(0, 36)), 'Warn': ('Curl and uncurl', r(0, 18)), 'Vulnerable': ('Curl and uncurl', r(18, 36)),
        'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'E-16': ('center', 1.2, ('Glide', 0), {
        'Idle': ('Glide', r(0, 36)), 'Warn': ('Bank and swoop', r(0, 10)), 'Attack': ('Bank and swoop', r(10, 28)),
        'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'E-17': ('center', 1.5, ('Spin and inhale', 0), {
        'Idle': ('Spin and inhale', r(0, 12)), 'Warn': ('Spin and inhale', r(12, 36)), 'Attack': ('Exhale', r(0, 16)),
        'Recover': ('Exhale', r(16, 36)), 'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'E-18': ('feet', 1.9, ('Perch and rear', 0), {
        'Idle': ('Perch and rear', r(0, 16)), 'Warn': ('Perch and rear', r(16, 36)), 'Attack': ('Spit', r(0, 16)),
        'Recover': ('Spit', r(16, 36)), 'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'E-20': ('feet', 1.5, ('Wake', 0), {
        'Dormant': ('Wake', r(0, 1)), 'Warn': ('Wake', r(0, 36)), 'Attack': ('Lunge', r(0, 18)), 'Vulnerable': ('Lunge', r(18, 36)),
        'Hurt': ('Crumble', r(0, 4)), 'Death': ('Crumble', r(0, 36))}),
    'M-01': ('feet', 3.5, ('Idle and roar', 0), {
        'Walk': ('Idle and roar', r(0, 16)), 'Roar': ('Idle and roar', r(16, 36)), 'Warn': ('Swipe', r(0, 12)),
        'Attack': ('Swipe', r(12, 24)), 'Recover': ('Swipe', r(24, 36)), 'Vulnerable': ('Belly open', r(0, 36)),
        'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'M-02': ('feet', 3.5, ('Idle and ring', 0), {
        'Walk': ('Idle and ring', r(0, 12)), 'Guard': ('Idle and ring', r(12, 36)), 'Warn': ('Hammer slam', r(0, 12)),
        'Attack': ('Hammer slam', r(12, 22)), 'Recover': ('Hammer slam', r(22, 36)), 'Vulnerable': ('Bell open', r(0, 36)),
        'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'M-03': ('feet', 4.0, ('Idle and grind', 0), {
        'Walk': ('Idle and grind', r(0, 18)), 'Warn': ('Fist slam', r(0, 12)), 'Attack': ('Fist slam', r(12, 24)),
        'Recover': ('Fist slam', r(24, 36)), 'Vulnerable': ('Core open', r(0, 36)),
        'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'G-03': ('feet', 6.0, ('Idle and roar (redo)', 0), {
        'Idle': ('Idle and roar (redo)', r(0, 14)), 'Roar': ('Idle and roar (redo)', r(14, 36)), 'Warn': ('Claw sweep', r(0, 12)),
        'Attack': ('Claw sweep', r(12, 24)), 'Recover': ('Claw sweep', r(24, 36)), 'Open': ('Valve open', r(0, 12)),
        'Exposed': ('Valve open', r(12, 24)), 'Close': ('Valve open', r(24, 36)),
        'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'G-04': ('feet', 6.5, ('Idle and roar', 0), {
        'Idle': ('Idle and roar', r(0, 16)), 'Roar': ('Idle and roar', r(16, 36)), 'Warn': ('Root lash', r(0, 12)),
        'Attack': ('Root lash', r(12, 24)), 'Recover': ('Root lash', r(24, 36)), 'Open': ('Bloom open', r(0, 12)),
        'Exposed': ('Bloom open', r(12, 24)), 'Close': ('Bloom open', r(24, 36)),
        'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
    'G-05': ('feet', 5.0, ('Idle and screech', 0), {
        'Idle': ('Idle and screech', r(0, 12)), 'Roar': ('Idle and screech', r(12, 36)), 'Warn': ('Wing gust', r(0, 10)),
        'Attack': ('Wing gust', r(10, 20)), 'Recover': ('Wing gust', r(20, 24)), 'DiveWarn': ('Dive wind-up', r(0, 24)), 'Open': ('Eye open', r(0, 12)),
        'Exposed': ('Eye open', r(12, 24)), 'Close': ('Eye open', r(24, 36)),
        'Hurt': ('Hurt and death', r(0, 6)), 'Death': ('Hurt and death', r(6, 36))}),
}


def folder(cid, clip):
    return os.path.join(SRC, f'{cid} - {clip}', 'frames')


def load(cid, clip, i):
    return np.array(Image.open(os.path.join(folder(cid, clip), 'frame_%03d.png' % i)).convert('RGBA'))


def clean(a):
    m = a[:, :, 3] > 40
    lab, n = ndimage.label(m, structure=np.ones((3, 3)))
    if n <= 1: return a
    sizes = ndimage.sum(m, lab, range(1, n + 1))
    keep = np.zeros(n + 1, bool); keep[1:] = sizes >= .04 * sizes.max()
    a = a.copy(); a[~keep[lab] & m] = 0
    return a


def box(a):
    ys, xs = np.where(a[:, :, 3] > 128)
    if len(xs) == 0: return 0, 0, 1, 1
    return xs.min(), ys.min(), xs.max(), ys.max()


for cid, (pivot_mode, height_u, (ref_clip, ref_i), segments) in CREATURES.items():
    if ONLY and cid not in ONLY: continue
    rb = box(clean(load(cid, ref_clip, ref_i))); rw, rh = rb[2] - rb[0], rb[3] - rb[1]
    place = {}
    for clip in {c for c, _ in segments.values()}:
        b = box(clean(load(cid, clip, 0))); w, h = b[2] - b[0], b[3] - b[1]
        s = (rw / w + rh / h) / 2
        px = (b[0] + b[2]) / 2 * s
        py = (b[3] if pivot_mode == 'feet' else (b[1] + b[3]) / 2) * s
        place[clip] = (s, px, py)
    frames, lo, hi = {}, np.array([1e9, 1e9]), np.array([-1e9, -1e9])
    for seg, (clip, rng) in segments.items():
        s, px, py = place[clip]
        for i in rng:
            img = Image.fromarray(clean(load(cid, clip, i)))
            if abs(s - 1) > 1e-3: img = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
            off = np.array([-px, -py])
            frames.setdefault(seg, []).append((img, off))
            b = box(np.array(img)); lo = np.minimum(lo, off + [b[0], b[1]]); hi = np.maximum(hi, off + [b[2], b[3]])
    pad = 8
    CW, CH = int(hi[0] - lo[0]) + 2 * pad, int(hi[1] - lo[1]) + 2 * pad
    PX, PY = -lo[0] + pad, -lo[1] + pad
    for seg, items in frames.items():
        d = os.path.join(OUT, cid, seg); os.makedirs(d, exist_ok=True)
        for k, (img, off) in enumerate(items):
            canvas = Image.new('RGBA', (CW, CH), (0, 0, 0, 0))
            canvas.alpha_composite(img, (int(round(PX + off[0])), int(round(PY + off[1]))))
            arr = np.array(canvas); arr[arr[:, :, 3] == 0, :3] = 0
            Image.fromarray(arr).save(os.path.join(d, '%s_%s_%02d.png' % (cid, seg, k + 1)))
    info = dict(id=cid, pivot=pivot_mode, heightUnits=height_u, canvas=[CW, CH], pivotPx=[round(PX, 1), round(PY, 1)],
                figureHeightPx=float(rh), figureWidthPx=float(rw),
                segments={s: dict(clip=c, frames=[rg.start, rg.stop], scale=round(place[c][0], 4)) for s, (c, rg) in segments.items()})
    json.dump(info, open(os.path.join(OUT, cid, cid + '_info.json'), 'w'), indent=1)
    print(cid, {s: len(v) for s, v in frames.items()}, 'canvas', CW, CH, 'ref', rw, rh)
