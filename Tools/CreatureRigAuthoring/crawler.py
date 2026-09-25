"""Bramble Crawler cutout rig: fits Codex's separately painted parts onto one skeleton.

Every Codex part is painted on its own canvas at its own scale, so each part is placed by two
landmarks: source-image points (px, y down) that must land on two rig points (world units,
y up, crawler facing right, origin on the ground under the body). That gives one similarity
transform per part (scale + rotation + move), so proportions never distort.

  python crawler.py            -> work/crawler_preview.png (rig next to Codex's concept)
  python crawler.py export     -> Assets/Art/Characters/Crawler/CrawlerRig.json for Unity

Sizes follow request E-01: 0.8 QH long, 0.5 QH tall (QH = 1.65 world units).
"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, '..', '..'))
ART = os.path.join(PROJECT, 'Assets', 'Art', 'Codex', 'Enemies')
WORK = os.path.join(HERE, 'work'); os.makedirs(WORK, exist_ok=True)
OUT = os.path.join(PROJECT, 'Assets', 'Art', 'Characters', 'Crawler', 'CrawlerRig.json')


def cap(name, direction, band=.1):
    """Centroid of the opaque pixels in the outermost `band` of the part along `direction`:
    the centre of a rounded joint cap, or the claw cluster of a foot."""
    a = np.array(Image.open(os.path.join(ART, name + '.png')).convert('RGBA'))[:, :, 3] > 128
    ys, xs = np.nonzero(a)
    d = np.array(direction, float); d /= np.linalg.norm(d)
    pr = xs * d[0] + ys * d[1]
    m = pr >= pr.max() - band * (pr.max() - pr.min())
    return float(xs[m].mean()), float(ys[m].mean())


# name: file, (landmark A px, landmark B px), (rig A, rig B), parent bone, sorting order, tint
# Rig points are the REST pose. The pivot of each bone is its landmark A.
H = .40            # hip / shoulder height
LEG_THICK = 1.45   # Codex's leg parts are long and slim; the concept's legs are chunky
PARTS = {}
def part(bone, file, lm, rig, parent, order, tint=1.0, thick=1.0):
    """thick: widens the art across the bone (A->B) without changing its length."""
    PARTS[bone] = dict(file=file, lm=lm, rig=rig, parent=parent, order=order, tint=tint, thick=thick)

tail, neck = cap('Crawler_Body', (-1, 0)), cap('Crawler_Body', (1, 0))
part('Body', 'Crawler_Body', (tail, neck), ((-.56, .45), (.36, .48)), None, 10)
part('BackThorns', 'Crawler_BackThorns', (cap('Crawler_BackThorns', (-1, .3)), cap('Crawler_BackThorns', (1, .3))),
     ((-.66, .54), (.32, .57)), 'Body', 12)
part('Head', 'Crawler_Head', (cap('Crawler_Head', (-1, 0)), cap('Crawler_Head', (1, .4))),
     ((.28, .50), (.74, .30)), 'Body', 16)
part('Jaw', 'Crawler_Jaw', (cap('Crawler_Jaw', (-1, -.6)), cap('Crawler_Jaw', (1, .3))),
     ((.40, .40), (.68, .23)), 'Head', 15)
for side, x_off, order, tint in (('Far', .16, 0, .7), ('Near', 0., 20, 1.)):
    for leg, hip, knee, foot, upper, lower in (
            ('Hind', (-.30, H), (-.46, .22), (-.50, 0.), 'Crawler_LegHind_Upper', 'Crawler_LegHind_Lower'),
            ('Front', (.12, H), (.30, .22), (.44, 0.), 'Crawler_LegFront_Upper', 'Crawler_LegFront_Lower')):
        up_lm = (cap(upper, (-1, -1.2) if leg == 'Front' else (-1, -.4)), cap(upper, (.4, 1) if leg == 'Front' else (1, .8)))
        lo_lm = (cap(lower, (-.3, -1) if leg == 'Front' else (-.2, -1)), cap(lower, (.5, 1) if leg == 'Front' else (.4, 1)))
        sh = lambda p: (p[0] + x_off, p[1])
        part(f'{leg}{side}Upper', upper, up_lm, (sh(hip), sh(knee)), 'Body', order + 1, tint, LEG_THICK)
        part(f'{leg}{side}Lower', lower, lo_lm, (sh(knee), sh(foot)), f'{leg}{side}Upper', order, tint, LEG_THICK)


def fit(p):
    """Similarity transform for a part: scale (world units per px), rotation (deg, CCW), pivot px."""
    (ax, ay), (bx, by) = p['lm']
    (rax, ray), (rbx, rby) = p['rig']
    src = np.array([bx - ax, -(by - ay)])          # to y-up
    dst = np.array([rbx - rax, rby - ray])
    scale = np.linalg.norm(dst) / np.linalg.norm(src)
    rot = math.degrees(math.atan2(dst[1], dst[0]) - math.atan2(src[1], src[0]))
    return scale, rot


def affine(p):
    """2x3 matrix: image px (x right, y down) -> rig units (y up), and the bone angle."""
    scale, rot = fit(p)
    (ax, ay), _ = p['lm']
    (rax, ray), (rbx, rby) = p['rig']
    theta = math.atan2(rby - ray, rbx - rax)
    def R(t): return np.array([[math.cos(t), -math.sin(t)], [math.sin(t), math.cos(t)]])
    M = R(theta) @ np.diag([1.0, p['thick']]) @ R(math.radians(rot) - theta) @ np.diag([scale, -scale])
    t = np.array([rax, ray]) - M @ np.array([ax, ay])
    return M, t, math.degrees(theta)


def render(ppu=420, size=(760, 460), origin=(300, 400)):
    """Draw the rig in its rest pose."""
    canvas = Image.new('RGBA', size, (90, 100, 110, 255))
    ImageDraw.Draw(canvas).line([(0, origin[1]), (size[0], origin[1])], fill=(60, 50, 40, 255), width=3)
    for name in sorted(PARTS, key=lambda n: PARTS[n]['order']):
        p = PARTS[name]
        img = Image.open(os.path.join(ART, p['file'] + '.png')).convert('RGBA')
        if p['tint'] != 1.0:
            a = np.array(img).astype(float); a[:, :, :3] *= p['tint']; img = Image.fromarray(a.clip(0, 255).astype(np.uint8))
        M, t, _ = affine(p)
        # canvas px = C @ (M @ u + t) + c0, with C flipping y
        C = np.diag([ppu, -ppu]); F = C @ M; f = C @ t + np.array(origin)
        Fi = np.linalg.inv(F); c = -Fi @ f
        layer = img.transform(size, Image.AFFINE, (Fi[0, 0], Fi[0, 1], c[0], Fi[1, 0], Fi[1, 1], c[1]), resample=Image.BICUBIC)
        canvas.alpha_composite(layer)
    return canvas


def export():
    """Bones in hierarchy order. Each bone: local position/rotation under its parent (the rig's
    rest pose), its length (A->B, for IK), and an 'art' transform that places the sprite:
    bone > thick (scale 1 x thick) > art (rotation, uniform scale in px, offset of the sprite
    centre from landmark A in px, y up)."""
    world = {}
    bones = []
    order = []
    def visit(n):
        if n in order: return
        par = PARTS[n]['parent']
        if par: visit(par)
        order.append(n)
    for n in PARTS: visit(n)
    for name in order:
        p = PARTS[name]
        scale, rot = fit(p)
        img = Image.open(os.path.join(ART, p['file'] + '.png'))
        (ax, ay), _ = p['lm']
        (rax, ray), (rbx, rby) = p['rig']
        theta = math.degrees(math.atan2(rby - ray, rbx - rax))
        world[name] = (np.array([rax, ray]), theta)
        pos, prot = np.array([rax, ray]), 0.0
        if p['parent']:
            ppos, prot = world[p['parent']]
            r = math.radians(-prot)
            pos = np.array([[math.cos(r), -math.sin(r)], [math.sin(r), math.cos(r)]]) @ (pos - ppos)
        bones.append(dict(name=name, parent=p['parent'] or '', sprite=p['file'],
                          pos=[float(pos[0]), float(pos[1])], rot=float(theta - prot),
                          length=float(math.hypot(rbx - rax, rby - ray)), thick=float(p['thick']),
                          artRot=float(rot - theta), artScale=float(scale),
                          centreFromPivot=[float(img.width / 2 - ax), float(-(img.height / 2 - ay))],
                          order=int(p['order']), tint=float(p['tint'])))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    json.dump(dict(version=1, bones=bones), open(OUT, 'w'), indent=1)
    print('wrote', OUT, len(bones), 'bones')


if __name__ == '__main__':
    if sys.argv[1:] == ['export']:
        export()
    else:
        rig = render()
        concept = Image.open(os.path.join(ART, 'Crawler_Concept.png')).convert('RGBA')
        bb = concept.getbbox(); concept = concept.crop(bb)
        concept = concept.resize((round(concept.width * 340 / concept.height), 340), Image.LANCZOS)
        sheet = Image.new('RGBA', (rig.width + concept.width + 20, rig.height), (90, 100, 110, 255))
        sheet.alpha_composite(rig, (0, 0)); sheet.alpha_composite(concept, (rig.width + 10, rig.height - concept.height - 60))
        sheet.save(os.path.join(WORK, 'crawler_preview.png'))
