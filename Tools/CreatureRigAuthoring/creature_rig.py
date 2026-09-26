"""Cutout rigs for the Codex enemies (Seed Carrier, Thornwing, Pod Spitter, Shellback).

Same method as crawler.py: each separately painted part is placed by two landmarks (source px,
y down) that land on two rig points (world units, y up, facing right). One similarity transform
per part (plus optional widening across the bone), so proportions never distort.

  python creature_rig.py <species>          -> work/<species>_preview.png (rig beside the concept)
  python creature_rig.py <species> export   -> Assets/Art/Characters/<Species>/<Species>Rig.json

Rig origin: flyers are centred on the body; walkers and the spitter stand on y = 0.
"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, '..', '..'))
ART = os.path.join(PROJECT, 'Assets', 'Art', 'Codex', 'Enemies')
WORK = os.path.join(HERE, 'work'); os.makedirs(WORK, exist_ok=True)

_alpha = {}
def alpha(name):
    if name not in _alpha:
        _alpha[name] = np.array(Image.open(os.path.join(ART, name + '.png')).convert('RGBA'))[:, :, 3] > 128
    return _alpha[name]

def cap(name, direction, band=.08):
    """Centroid of the opaque pixels in the outermost `band` of the part along `direction`."""
    a = alpha(name); ys, xs = np.nonzero(a)
    d = np.array(direction, float); d /= np.linalg.norm(d)
    pr = xs * d[0] + ys * d[1]
    m = pr >= pr.max() - band * (pr.max() - pr.min())
    return float(xs[m].mean()), float(ys[m].mean())

def cream_centre(name):
    """Centre of the largest cream (face) region: the Carrier's face disc."""
    import cv2
    im = np.array(Image.open(os.path.join(ART, name + '.png')).convert('RGBA')).astype(int)
    rgb = im[:, :, :3]; cream = (rgb.mean(2) > 205) & ((rgb.max(2) - rgb.min(2)) < 45) & (im[:, :, 3] > 200)
    n, lab, st, cen = cv2.connectedComponentsWithStats(cream.astype(np.uint8))
    i = 1 + int(np.argmax(st[1:, 4]))
    return float(cen[i][0]), float(cen[i][1])


class Rig:
    def __init__(self, name, concept):
        self.name, self.concept, self.parts = name, concept, {}

    def part(self, bone, file, lm, rig, parent=None, order=0, tint=1.0, thick=1.0, extra=None):
        """extra: named points on the part (source px) exported in bone space, e.g. a mouth."""
        self.parts[bone] = dict(file=file, lm=lm, rig=rig, parent=parent, order=order, tint=tint, thick=thick, extra=extra or {})

    def fit(self, p):
        (ax, ay), (bx, by) = p['lm']; (rax, ray), (rbx, rby) = p['rig']
        src = np.array([bx - ax, -(by - ay)]); dst = np.array([rbx - rax, rby - ray])
        return np.linalg.norm(dst) / np.linalg.norm(src), math.degrees(math.atan2(dst[1], dst[0]) - math.atan2(src[1], src[0]))

    def affine(self, p):
        """2x3: source px (y down) -> rig units (y up)."""
        scale, rot = self.fit(p)
        (ax, ay), _ = p['lm']; (rax, ray), (rbx, rby) = p['rig']
        theta = math.atan2(rby - ray, rbx - rax)
        R = lambda t: np.array([[math.cos(t), -math.sin(t)], [math.sin(t), math.cos(t)]])
        M = R(theta) @ np.diag([1.0, p['thick']]) @ R(math.radians(rot) - theta) @ np.diag([scale, -scale])
        return M, np.array([rax, ray]) - M @ np.array([ax, ay])

    def render(self, ppu=300, size=(900, 700), origin=(450, 450), hide=()):
        canvas = Image.new('RGBA', size, (90, 100, 110, 255))
        ImageDraw.Draw(canvas).line([(0, origin[1]), (size[0], origin[1])], fill=(60, 50, 40, 120), width=2)
        for name in sorted(self.parts, key=lambda n: self.parts[n]['order']):
            if name in hide: continue
            p = self.parts[name]
            img = Image.open(os.path.join(ART, p['file'] + '.png')).convert('RGBA')
            if p['tint'] != 1.0:
                a = np.array(img).astype(float); a[:, :, :3] *= p['tint']; img = Image.fromarray(a.clip(0, 255).astype(np.uint8))
            M, t = self.affine(p)
            C = np.diag([ppu, -ppu]); F = C @ M; f = C @ t + np.array(origin)
            Fi = np.linalg.inv(F); c = -Fi @ f
            canvas.alpha_composite(img.transform(size, Image.AFFINE, (Fi[0, 0], Fi[0, 1], c[0], Fi[1, 0], Fi[1, 1], c[1]), resample=Image.BICUBIC))
        return canvas

    def export(self):
        order, world, bones = [], {}, []
        def visit(n):
            if n in order: return
            if self.parts[n]['parent']: visit(self.parts[n]['parent'])
            order.append(n)
        for n in self.parts: visit(n)
        for name in order:
            p = self.parts[name]; scale, rot = self.fit(p)
            img = Image.open(os.path.join(ART, p['file'] + '.png'))
            (ax, ay), _ = p['lm']; (rax, ray), (rbx, rby) = p['rig']
            theta = math.degrees(math.atan2(rby - ray, rbx - rax)); world[name] = (np.array([rax, ray]), theta)
            pos, prot = np.array([rax, ray]), 0.0
            if p['parent']:
                ppos, prot = world[p['parent']]
                r = math.radians(-prot)
                pos = np.array([[math.cos(r), -math.sin(r)], [math.sin(r), math.cos(r)]]) @ (pos - ppos)
            M, t = self.affine(p)
            r = math.radians(-theta); Rinv = np.array([[math.cos(r), -math.sin(r)], [math.sin(r), math.cos(r)]])
            extra = {k: [float(v) for v in Rinv @ (M @ np.array(px) + t - np.array([rax, ray]))] for k, px in p['extra'].items()}
            bones.append(dict(name=name, parent=p['parent'] or '', sprite=p['file'], pos=[float(pos[0]), float(pos[1])],
                              rot=float(theta - prot), length=float(math.hypot(rbx - rax, rby - ray)), thick=float(p['thick']),
                              artRot=float(rot - theta), artScale=float(scale),
                              centreFromPivot=[float(img.width / 2 - ax), float(-(img.height / 2 - ay))],
                              order=int(p['order']), tint=float(p['tint']), points=[dict(name=k, pos=v) for k, v in extra.items()]))
        out = os.path.join(PROJECT, 'Assets', 'Art', 'Characters', self.name, self.name + 'Rig.json')
        os.makedirs(os.path.dirname(out), exist_ok=True)
        json.dump(dict(version=1, bones=bones), open(out, 'w'), indent=1)
        print('wrote', out, len(bones), 'bones')

    def preview(self, **kw):
        rig = self.render(**kw)
        concept = Image.open(os.path.join(ART, self.concept + '.png')).convert('RGBA'); concept = concept.crop(concept.getbbox())
        h = rig.height - 80; concept = concept.resize((round(concept.width * h / concept.height), h), Image.LANCZOS)
        sheet = Image.new('RGBA', (rig.width + concept.width + 20, rig.height), (90, 100, 110, 255))
        sheet.alpha_composite(rig, (0, 0)); sheet.alpha_composite(concept, (rig.width + 10, 40))
        sheet.save(os.path.join(WORK, self.name.lower() + '_preview.png'))


# ------------------------------------------------------------------ Seed Carrier (E-02): 0.6 QH, harmless flyer
def carrier():
    r = Rig('Carrier', 'Carrier_Concept')
    B, H = 'Carrier_Body', 'Carrier_Head'
    r.part('Body', B, (cap(B, (-1, 0)), cap(B, (1, 0))), ((-.44, 0.), (.16, .03)), None, 10)
    face = cream_centre(H)
    r.part('Head', H, (face, cap(H, (1, .15))), ((.24, .07), (.32, .055)), 'Body', 14)
    r.part('WingBack', 'Carrier_WingBack', (cap('Carrier_WingBack', (1, .6)), cap('Carrier_WingBack', (-1, -.5))),
           ((.06, .08), (-.22, .78)), 'Body', 4, .78)
    r.part('WingFront', 'Carrier_WingFront', (cap('Carrier_WingFront', (1, .8)), cap('Carrier_WingFront', (-1, -.8))),
           ((-.02, .08), (-.50, .72)), 'Body', 16)
    P = 'Carrier_SeedPod'
    r.part('SeedPod', P, (cap(P, (0, -1)), cap(P, (.1, 1))), ((.06, -.10), (.09, -.54)), 'Body', 8)
    return r


# ------------------------------------------------------------------ Thornwing (E-03): 0.5 QH, wingspan 0.9 QH, hostile flyer
def thornwing():
    r = Rig('Thornwing', 'Thornwing_Concept')
    B, H, T = 'Thornwing_Body', 'Thornwing_Head', 'Thornwing_Tail'
    r.part('Body', B, (cap(B, (-1, 0)), cap(B, (1, 0))), ((-.48, 0.), (.35, -.03)), None, 10)
    r.part('Head', H, (cap(H, (-1, .1)), cap(H, (.45, 1))), ((.30, .06), (.72, -.26)), 'Body', 14)
    r.part('Tail', T, (cap(T, (1, 0)), cap(T, (-1, 0))), ((-.38, .0), (-1.10, .10)), 'Body', 8)
    r.part('WingBack', 'Thornwing_WingBack', (cap('Thornwing_WingBack', (1, .7)), cap('Thornwing_WingBack', (-1, -.4))),
           ((.10, .12), (.42, .98)), 'Body', 4, .75)
    r.part('WingFront', 'Thornwing_WingFront', (cap('Thornwing_WingFront', (1, .7)), cap('Thornwing_WingFront', (-1, -.4))),
           ((-.02, .10), (-.72, 1.02)), 'Body', 16)
    return r


# ------------------------------------------------------------------ Pod Spitter (E-04): 0.7 QH tall stationary turret
def spitter():
    r = Rig('Spitter', 'Spitter_Concept')
    Ba, St, Hc = 'Spitter_Base', 'Spitter_Stalk', 'Spitter_Head_Closed'
    r.part('Base', Ba, (cap(Ba, (-1, .3)), cap(Ba, (1, .3))), ((-.46, .03), (.46, .03)), None, 12)
    r.part('Stalk', St, (cap(St, (0, 1)), cap(St, (0, -1))), ((0., .16), (-.04, .86)), 'Base', 10)
    mouth = cap(Hc, (1, 0))
    r.part('Head', Hc, ((cap(Hc, (-1, 0))[0], cap(Hc, (1, 0))[1]), mouth), ((-.30, .96), (.52, .96)), 'Stalk', 14,
           extra={'Mouth': mouth})
    return r


# ------------------------------------------------------------------ Shellback (E-05): 0.9 QH long, 0.6 QH tall beetle
def shellback():
    r = Rig('Shellback', 'Shellback_Concept')
    B, H, L, S = 'Shellback_Body', 'Shellback_Head', 'Shellback_Leg', 'Shellback_Shell_Intact'
    r.part('Body', B, (cap(B, (-1, 0)), cap(B, (1, 0))), ((-.62, .40), (.58, .40)), None, 10)
    r.part('Shell', S, (cap(S, (-1, .2)), cap(S, (1, .2))), ((-.72, .52), (.62, .54)), 'Body', 16)
    r.part('Head', H, (cap(H, (-1, 0)), cap(H, (1, .3))), ((.46, .52), (.98, .34)), 'Body', 18)
    hip, foot = cap(L, (-.2, -1)), cap(L, (.2, 1))
    for side, dx, order, tint in (('Far', .08, 2, .7), ('Near', 0., 20, 1.)):
        for i, x in enumerate((-.42, 0., .38)):
            r.part(f'Leg{side}{i}', L, (hip, foot), ((x + dx, .34), (x + dx + .10, 0.)), 'Body', order, tint, 1.4)
    return r


# ------------------------------------------------------------------ Ripple Newt (E-07, A1): 1.2 QH long water lizard
def newt():
    r = Rig('Newt', 'Newt_Concept')
    B, H, L, T1, T2 = 'Newt_Body', 'Newt_Head', 'Newt_Leg', 'Newt_Tail_1', 'Newt_Tail_2'
    # Body: hip socket (lower left) to neck socket (right end).
    r.part('Body', B, ((205, 285), (722, 255)), ((-.45, .30), (.42, .33)), None, 10)
    r.part('Head', H, ((292, 262), (526, 258)), ((.38, .33), (.78, .30)), 'Body', 14, extra={'Mouth': (515, 268)})
    # Tail pieces run leftward; mirrored across the bone (thick -1) so their leaves stay on top.
    r.part('Tail1', T1, (cap(T1, (1, 0)), cap(T1, (-1, 0))), ((-.40, .30), (-.90, .36)), 'Body', 8, 1.0, -1.25)
    r.part('Tail2', T2, (cap(T2, (-1, 0)), cap(T2, (1, 0))), ((-.86, .36), (-1.24, .62)), 'Tail1', 6, 1.0, -1.25)
    hip, foot = cap(L, (0, -1)), cap(L, (0, 1))
    for side, dx, order, tint in (('Far', .06, 2, .72), ('Near', 0., 20, 1.)):
        for i, x in enumerate((.24, -.30)):
            r.part(f'Leg{side}{i}', L, (hip, foot), ((x + dx, .27), (x + dx + .08, 0.)), 'Body', order, tint, 1.5)
    return r


# ------------------------------------------------------------------ Burrow Grub (E-06): 0.6 QH tall segmented grub
def grub():
    r = Rig('Grub', 'Grub_Concept')
    S, H = 'Grub_Body_Segment', 'Grub_Head'
    back, front = cap(S, (-1, 0)), cap(S, (1, 0))
    # Segments from the head backwards, each a little smaller and lower; nearer ones drawn in front.
    # Heavily overlapped, tall segments so they read as one plump, ringed body (as in the concept).
    parent = None
    for i in range(5):
        k = 1.0 - .09 * i                      # size falls off toward the tail
        x1 = .42 - .25 * i; x0 = x1 - .46 * k; y = .30 * k + .02
        r.part(f'Seg{i}', S, (back, front), ((x0, y), (x1, y)), parent, 12 - i, 1.0 - .05 * i, 1.75)
        parent = f'Seg{i}'
    r.part('Head', H, (cap(H, (-1, 0)), cap(H, (1, 0))), ((.18, .36), (.88, .33)), 'Seg0', 16, extra={'Mouth': (565, 450)})
    return r


# ------------------------------------------------------------------ Gust Moth (E-09, A3): wingspan 0.9 QH, harmless hoverer
def gustmoth():
    r = Rig('GustMoth', 'GustMoth_Concept')
    B = 'GustMoth_Body'
    r.part('Body', B, (cap(B, (-1, 0)), cap(B, (1, -.2))), ((-.42, -.02), (.36, .05)), None, 10)
    for name, order, tint, tip in (('WingBack', 4, .8, (-.30, .95)), ('WingFront', 16, 1.0, (-.62, .82))):
        W = 'GustMoth_' + name
        r.part(name, W, (cap(W, (1, 1)), cap(W, (-1, -1))), ((-.06, .12), tip), 'Body', order, tint)
    return r


SPECIES = dict(carrier=carrier, thornwing=thornwing, spitter=spitter, shellback=shellback, newt=newt, grub=grub, gustmoth=gustmoth)

if __name__ == '__main__':
    rig = SPECIES[sys.argv[1]]()
    if sys.argv[2:] == ['export']: rig.export()
    else: rig.preview()
