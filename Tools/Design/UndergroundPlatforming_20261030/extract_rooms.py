# Reads the connected underground's room rectangles, doors and gameplay markers into rooms.json.
# usage: python extract_rooms.py <Qolossal_Underground.unity> <terrain_build_v2.json> <out rooms.json>
import json, re, sys

scene, build, out = sys.argv[1:4]
t = open(scene, encoding='utf-8').read()
docs = re.split(r'\n--- !u!(\d+) &(\d+)', t)
names, tr = {}, {}
for i in range(1, len(docs), 3):
    cls, fid, body = docs[i], docs[i + 1], docs[i + 2]
    if cls == '1':
        m = re.search(r'm_Name: (.*)', body); names[fid] = m.group(1).strip("'") if m else ''
    elif cls == '4':
        g = re.search(r'm_GameObject: \{fileID: (\d+)\}', body).group(1)
        p = re.search(r'm_LocalPosition: \{x: ([-\d.e]+), y: ([-\d.e]+)', body)
        f = re.search(r'm_Father: \{fileID: (\d+)\}', body).group(1)
        tr[fid] = (g, float(p.group(1)), float(p.group(2)), f)

def world(t):
    x = y = 0; chain = []
    while t in tr:
        g, lx, ly, f = tr[t]; x += lx; y += ly; chain.append(names.get(g, '')); t = f
    return x, y, chain

b = json.load(open(build, encoding='utf-8-sig'))
rooms = {r['id']: dict(id=r['id'], name=r['name'], kind=r['kind'], group=r['group'],
                       x=r['x'], y=r['y'], w=r['w'], h=r['h'], doors=[], markers=[]) for r in b['rooms']}

def door(room, pt, width, link, other):
    r = rooms[room]; px, py = pt['x'] - r['x'], pt['y'] - r['y']
    side = 'L' if abs(px) < .01 else 'R' if abs(px - r['w']) < .01 else 'B' if abs(py) < .01 else 'T' if abs(py - r['h']) < .01 else '?'
    d = dict(link=link, to=other, side=side, width=width)
    if side in 'LR': d.update(sill=round(py - width / 2, 3), at=round(px, 3))
    else: d.update(x0=round(px - width / 2, 3), x1=round(px + width / 2, 3), at=round(py, 3))
    r['doors'].append(d)

for c in b['connections']:
    pts = c['points']
    door(c['fromRoom'], pts[0], c['width'], c['id'], c['toRoom'])
    door(c['toRoom'], pts[-1], c['width'], c['id'], c['fromRoom'])

for fid, (g, _, _, _) in tr.items():
    n = names.get(g, '')
    kind = next((k for k in ('Arrival', 'Rest', 'Reward', 'Return to') if n.startswith(k)), None)
    if not kind: continue
    x, y, chain = world(fid)
    room = next((re.search(r'\[([A-Z0-9_]+)\]', c).group(1) for c in chain if re.match(r"\[[A-Z0-9_]+\] ", c)), None)
    if room in rooms:
        r = rooms[room]
        r['markers'].append(dict(kind=kind, name=n, x=round(x - r['x'], 2), y=round(y - r['y'], 2)))

json.dump(dict(source=scene, rooms=rooms), open(out, 'w'), indent=1)
bad = [(r['id'], d['link']) for r in rooms.values() for d in r['doors'] if d['side'] == '?']
print(len(rooms), 'rooms;', sum(len(r['doors']) for r in rooms.values()), 'doors; unplaced doors:', bad)
