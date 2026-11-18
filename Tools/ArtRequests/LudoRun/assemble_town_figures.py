"""Draws each Qvale resident's jointed figure as one picture, from the parts and joints saved in the street scene, for
Ludo to animate. The same pieces in the same pose, so the approved look is kept. Writes
Tools/IncomingArt/Ludo/Residents/<Rig>_ForLudo.png on a transparent canvas, with the figure's feet position in feet.json.
Usage: python assemble_town_figures.py [MRAtlas_scene] [Rig ...] (Scribble has its own script: assemble_scribble.py)
"""
import json, math, os, re, sys
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
SCENE = os.path.join(ROOT, 'Assets', 'Scenes', 'Game', 'MRAtlas_MR04_Qvale.unity')
OUT = os.path.join(ROOT, 'Tools', 'IncomingArt', 'Ludo', 'Residents')
SCALE = 600     # pixels per unit on the assembled picture
# Parts drawn in a different order from the jointed rig. Sorrel's near upper arm sat behind the body, leaving the
# forearm floating on her belly.
ORDER = {'Herbalist': {'ArmNearUpper': 7.5}}
# Joints turned from the rig's pose, in degrees (positive: the hanging bone swings toward the way the figure faces).
# Sorrel's forearm swung back from the elbow, which reads as a joint bending the wrong way.
TURN = {'Herbalist': {'ArmNearLower': 24}}
CANVAS = 1024
WORK = 2400    # the assembling canvas, big enough for the tallest figure


def art_index():
    found = {}
    for folder in ('Characters', ):
        for base, _, files in os.walk(os.path.join(ROOT, 'Assets', 'Art', 'Codex', folder)):
            for f in files:
                if f.endswith('.png.meta'):
                    text = open(os.path.join(base, f), encoding='utf8', errors='ignore').read()
                    found[re.search(r'guid: (\w+)', text).group(1)] = (os.path.join(base, f[:-5]), text)
    return found


def main():
    os.makedirs(OUT, exist_ok=True)
    art = art_index()
    # A figure's scene: the homes hold their own residents since the interiors came back.
    only_args = [x for x in sys.argv[1:] if not x.startswith('MRAtlas_')]
    scene = next((x for x in sys.argv[1:] if x.startswith('MRAtlas_')), None)
    text = open(os.path.join(os.path.dirname(SCENE), scene + '.unity') if scene else SCENE, encoding='utf8').read()
    docs = {m.group(2): (m.group(1), m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- |\Z)', text, flags=re.M | re.S)}
    objects, by_go = {}, {}
    for key, (cid, body) in docs.items():
        if cid == '4':
            go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
            if go: by_go[go.group(1)] = key     # a transform without one belongs to a prefab instance
    name = lambda go: re.search(r'm_Name: (.*)', docs[go][1]).group(1).strip("' ")

    def local(t):
        b = docs[t][1]
        px, py = [float(v) for v in re.search(r'm_LocalPosition: \{x: ([-\de.]+), y: ([-\de.]+)', b).groups()]
        qz, qw = [float(v) for v in re.search(r'm_LocalRotation: \{x: [-\de.]+, y: [-\de.]+, z: ([-\de.]+), w: ([-\de.]+)', b).groups()]
        sx, sy = [float(v) for v in re.search(r'm_LocalScale: \{x: ([-\de.]+), y: ([-\de.]+)', b).groups()]
        a = 2 * math.atan2(qz, qw)
        return np.array([[math.cos(a) * sx, -math.sin(a) * sy, px], [math.sin(a) * sx, math.cos(a) * sy, py], [0, 0, 1]])

    def children(t):
        return re.findall(r'- \{fileID: (\d+)\}', re.search(r'm_Children:(.*?)m_Father', docs[t][1], re.S).group(1))

    seen = {}
    for go, t in by_go.items():
        if docs.get(go, ('', ''))[0] == '1' and name(go).startswith('Figure '):
            rig = name(go)[7:]
            if rig in seen: continue
            seen[rig] = t
    print('figures:', sorted(seen))
    feet_path = os.path.join(OUT, 'feet.json')
    feet = json.load(open(feet_path)) if os.path.exists(feet_path) else {}
    only = only_args
    for rig, top in seen.items():
        if only and rig not in only: continue
        pieces = []
        def walk(t, parent):
            m = parent @ local(t) if t != top else np.eye(3)
            go = re.search(r'm_GameObject: \{fileID: (\d+)\}', docs[t][1]).group(1)
            turn = TURN.get(rig, {}).get(name(go))
            if turn is not None:
                a = math.radians(turn)
                m = m.copy(); m[:2, :2] = np.array([[math.cos(a), -math.sin(a)], [math.sin(a), math.cos(a)]])
            active = re.search(r'm_IsActive: (\d)', docs[go][1]).group(1) == '1'
            if not active: return
            for comp in re.findall(r'component: \{fileID: (\d+)\}', docs[go][1]):
                if docs[comp][0] != '212': continue
                b = docs[comp][1]
                guid = re.search(r'm_Sprite: \{fileID: \d+, guid: (\w+)', b)
                if not guid or guid.group(1) not in art: continue
                order = int(re.search(r'm_SortingOrder: (-?\d+)', b).group(1))
                flip = re.search(r'm_FlipX: (\d)', b).group(1) == '1'
                pieces.append((ORDER.get(rig, {}).get(name(go), order), guid.group(1), m, flip))
            for c in children(t): walk(c, m)
        walk(top, np.eye(3))
        if not pieces: print('no parts:', rig); continue
        canvas = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
        # Figure's origin: its feet. Placed low and centred, leaving room to move.
        origin = np.array([WORK / 2, WORK * .8])
        for order, guid, m, flip in sorted(pieces, key=lambda p: p[0]):
            picture, meta = art[guid]
            ppu = float(re.search(r'spritePixelsToUnits: ([\d.]+)', meta).group(1))
            pv = re.search(r'spritePivot: \{x: ([\d.\-e]+), y: ([\d.\-e]+)\}', meta)
            im = Image.open(picture).convert('RGBA')
            w, h = im.size
            pivot = (float(pv.group(1)) * w, (1 - float(pv.group(2))) * h)
            if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT); pivot = (w - pivot[0], pivot[1])
            # Unit-space picture -> canvas: x' = origin + SCALE * M * ((px - pivot) / ppu), y flipped.
            A = m[:2, :2] * SCALE / ppu
            A = np.array([[A[0, 0], -A[0, 1]], [-A[1, 0], A[1, 1]]])
            t = origin + np.array([m[0, 2] * SCALE, -m[1, 2] * SCALE])
            inv = np.linalg.inv(A)
            off = np.array(pivot) - inv @ t
            layer = im.transform((WORK, WORK), Image.AFFINE, (inv[0, 0], inv[0, 1], off[0], inv[1, 0], inv[1, 1], off[1]), resample=Image.BICUBIC)
            canvas.alpha_composite(layer)
        box = canvas.getbbox()
        # Fit to 72% of the canvas so Ludo has room; the feet stay low.
        fit = .72 * CANVAS / max(box[2] - box[0], box[3] - box[1])
        fig = canvas.crop(box).resize((round((box[2] - box[0]) * fit), round((box[3] - box[1]) * fit)), Image.LANCZOS)
        out = Image.new('RGBA', (CANVAS, CANVAS), (0, 0, 0, 0))
        x, y = (CANVAS - fig.width) // 2, CANVAS - fig.height - 110
        out.alpha_composite(fig, (x, y))
        out.save(os.path.join(OUT, rig + '_ForLudo.png'))
        feet[rig] = {'unitsTall': (box[3] - box[1]) / SCALE, 'unitsWide': (box[2] - box[0]) / SCALE, 'originFromBottom': (box[3] - origin[1]) / SCALE, 'originFromLeft': (origin[0] - box[0]) / SCALE, 'parts': len(pieces)}
        print(rig, feet[rig])
    json.dump(feet, open(feet_path, 'w'), indent=1, default=float)
    # A contact sheet for review.
    names = sorted(only or feet)
    sheet = Image.new('RGB', (300 * len(names), 300), (80, 90, 105))
    for i, n in enumerate(names):
        im = Image.open(os.path.join(OUT, n + '_ForLudo.png')); im.thumbnail((300, 300))
        sheet.paste(im, (i * 300, 0), im)
    sheet.save(os.path.join(OUT, '_contact.jpg'), quality=88)


main()
