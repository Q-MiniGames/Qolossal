"""Scribble's parts as they were drawn to fit, socket to socket: the jointed figure in the scene leaves them apart (head
floating, one leg). The head's socket meets the body's, three legs carry the low body, and the map roll rides on its
back. Writes Residents/Scribble_ForLudo.png and updates feet.json.
"""
import json, os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
ART = os.path.join(ROOT, 'Assets', 'Art', 'Codex', 'Characters', 'Scribble', 'Scribble_%s.png')
OUT = os.path.join(ROOT, 'Tools', 'IncomingArt', 'Ludo', 'Residents')
PPU, CANVAS = 600, 1024
part = lambda n: Image.open(ART % n).convert('RGBA')

work = Image.new('RGBA', (1400, 1200), (0, 0, 0, 0))
body, head, leg, roll = part('Body'), part('Head'), part('Leg'), part('MapRoll')
bx, by = 350, 330
# Sockets, in each picture's pixels.
# The legs are drawn slanting back; turned about their sockets so the figure stands instead of leaping.
stand = Image.new('RGBA', (700, 700), (0, 0, 0, 0))
stand.alpha_composite(leg, (350 - 48, 350 - 50))
stand = stand.rotate(-38, resample=Image.BICUBIC, center=(350, 350))
for sx, sy in ((165, 262), (305, 278)):
    work.alpha_composite(stand, (bx + sx - 350, by + sy - 350))
work.alpha_composite(body, (bx, by))
work.alpha_composite(stand, (bx + 225 - 350, by + 275 - 350))
work.alpha_composite(head, (bx + 412 - 48, by + 170 - 208))
work.alpha_composite(roll, (bx + 40, by - 62))
box = work.getbbox()
fit = .72 * CANVAS / max(box[2] - box[0], box[3] - box[1])
fig = work.crop(box).resize((round((box[2] - box[0]) * fit), round((box[3] - box[1]) * fit)), Image.LANCZOS)
out = Image.new('RGBA', (CANVAS, CANVAS), (0, 0, 0, 0))
out.alpha_composite(fig, ((CANVAS - fig.width) // 2, CANVAS - fig.height - 110))
out.save(os.path.join(OUT, 'Scribble_ForLudo.png'))
path = os.path.join(OUT, 'feet.json')
feet = json.load(open(path))
w, h = (box[2] - box[0]) / PPU, (box[3] - box[1]) / PPU
feet['Scribble'] = {'unitsTall': h, 'unitsWide': w, 'originFromBottom': 0.0, 'originFromLeft': w * .5, 'parts': 6}
json.dump(feet, open(path, 'w'), indent=1)
print('Scribble', round(w, 2), 'x', round(h, 2), 'u')
bg = Image.new('RGBA', out.size, (80, 90, 105, 255)); bg.alpha_composite(out)
bg.convert('RGB').resize((512, 512)).save(os.path.join(OUT, 'Scribble_check.jpg'), quality=88)
