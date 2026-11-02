# Draws each underground room's current collision (and the planned platforming, if given) to a PNG.
# usage: python render_rooms.py <scene_geometry.json> <out_dir> [platforming_v1.json] [room ids...]
import json, sys, os
from PIL import Image, ImageDraw

PX = 24
geo = json.load(open(sys.argv[1], encoding='utf-8-sig'))
out = sys.argv[2]; os.makedirs(out, exist_ok=True)
plan = json.load(open(sys.argv[3], encoding='utf-8-sig')) if len(sys.argv) > 3 and sys.argv[3].endswith('.json') else None
only = [a for a in sys.argv[3:] if not a.endswith('.json')]
rooms = {r['id']: r for r in geo['rooms']}

def draw_room(r):
    x, y, w, h = r['bounds']; m = 5
    X0, Y0, X1, Y1 = x - m, y - m, x + w + m, y + h + m
    img = Image.new('RGB', (int((X1 - X0) * PX), int((Y1 - Y0) * PX)), (24, 24, 30))
    d = ImageDraw.Draw(img)
    T = lambda p: ((p[0] - X0) * PX, (Y1 - p[1]) * PX)
    for gx in range(int(X0), int(X1) + 1):
        d.line([T((gx, Y0)), T((gx, Y1))], fill=(40, 40, 48) if gx % 5 else (60, 60, 72))
    for gy in range(int(Y0), int(Y1) + 1):
        d.line([T((X0, gy)), T((X1, gy))], fill=(40, 40, 48) if gy % 5 else (60, 60, 72))
    d.rectangle([T((x, y + h)), T((x + w, y))], outline=(90, 90, 140), width=2)
    for c in geo['colliders']:
        for path in c['paths']:
            if not any(X0 <= p[0] <= X1 and Y0 <= p[1] <= Y1 for p in path): continue
            pts = [T(p) for p in path]
            if c['type'] == 'EdgeCollider2D':
                d.line(pts, fill=(200, 200, 200), width=3)
            else:
                d.polygon(pts, fill=(150, 110, 70) if 'Platform' in c['path'] else (120, 120, 160))
    if plan and r['id'] in plan.get('rooms', {}):
        for p in plan['rooms'][r['id']].get('shapes', []):
            k = p['kind']; px, py, pw, ph = p.get('local', p['rect'])
            col = {'deck': (230, 200, 90), 'solid': (110, 200, 120), 'moss': (60, 190, 80), 'water': (60, 120, 220),
                   'thorns': (220, 60, 60), 'crumble': (200, 140, 60), 'pod': (230, 120, 220), 'gust': (120, 200, 240),
                   'anchor': (255, 255, 255)}.get(k, (255, 0, 255))
            d.rectangle([T((x + px, y + py + ph)), T((x + px + pw, y + py))], fill=col if k not in ('gust', 'water') else None,
                        outline=col, width=2)
    d.text((6, 6), f"{r['id']} {r.get('name','')}  {w}x{h}  origin ({x},{y})", fill=(255, 255, 255))
    img.save(os.path.join(out, r['id'] + '.png'))

for rid, r in rooms.items():
    if only and rid not in only: continue
    draw_room(r)
print('ok')
