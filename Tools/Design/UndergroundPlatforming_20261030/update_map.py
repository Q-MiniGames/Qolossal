# Puts the greybox platforming on the in-game map (Assets/Resources/QolossalAtlas/AtlasData.json): drops the
# contours of the old generated platforms in every rebuilt room and adds one closed outline per solid piece of
# the new layout, through each room's own map transform. Re-runnable: earlier UGP outlines are replaced.
# Also lists every door (rooms.json, beside the plan) for the cave minimap.
# usage: python update_map.py platforming_v1.json <AtlasData.json>
import json, os, sys

plan = json.load(open(sys.argv[1]))['rooms']
path = sys.argv[2]
d = json.load(open(path, encoding='utf-8-sig'))
nodes = {n['id']: n for n in d['nodes'] if n.get('type') == 'underground'}
SOLID = {'solid', 'rock', 'deck', 'crumble', 'mover', 'gate', 'grille', 'pod'}

def convert(m, p): return [m['ox'] + p[0] * m['sx'], m['oy'] - p[1] * m['sy']]

before = len(d['undergroundContours'])
kept = [c for c in d['undergroundContours']
        if not str(c.get('source', '')).startswith('UGP Platforming/')
        and not (c.get('room') in plan and plan[c['room']]['replace'] and '/Platforms/' in str(c.get('source', '')))]
removed = before - len(kept) - sum(1 for c in d['undergroundContours'] if str(c.get('source', '')).startswith('UGP Platforming/'))
added = 0
for rid, spec in plan.items():
    n = nodes.get(rid)
    if n is None: continue
    m = d['transforms'][n['mappingId']]
    for s in spec['shapes']:
        if s['kind'] not in SOLID: continue
        x, y, w, h = s['rect']
        h = max(h, .3)
        pts = [convert(m, p) for p in [(x, y), (x + w, y), (x + w, y + h), (x, y + h), (x, y)]]
        kept.append(dict(region=n['region'], source='UGP Platforming/' + s['kind'], room=rid, closed=True,
                         mappingId=n['mappingId'], points=pts))
        added += 1
d['undergroundContours'] = kept
# The runtime list is derived from both contour lists, as Tools/Design/MainWorldLayout/export_runtime.py does.
def piece_kind(c):
    source = str(c.get('source', ''))
    return source.split('/')[-1] if source.startswith('UGP Platforming/') else ''
d['contours'] = [dict(region=c['region'], room=c.get('room'), underground=kind == 'undergroundContours', primary=c.get('primary', False),
                      mappingId=c.get('mappingId', ''), kind=piece_kind(c), path=[dict(x=p[0], y=p[1]) for p in c['points']]) for kind in ['surfaceContours', 'undergroundContours'] for c in d[kind]]
# Doors from each room's own corner, so a relocated room's doors move with it.
rooms = json.load(open(os.path.join(os.path.dirname(os.path.abspath(sys.argv[1])), 'rooms.json')))['rooms']
d['caveDoors'] = []
for rid, r in rooms.items():
    n = nodes.get(rid)
    if n is None: continue
    ox, oy = n['worldBounds'][0], n['worldBounds'][1]
    for door in r['doors']:
        if door['side'] in 'LR': x, y, w, h = ox + (0 if door['side'] == 'L' else r['w']), oy + door['sill'], 0, door['width']
        else: x, y, w, h = ox + door['x0'], oy + (0 if door['side'] == 'B' else r['h']), door['x1'] - door['x0'], 0
        d['caveDoors'].append(dict(room=rid, to=door['to'], link=door['link'], x=x, y=y, w=w, h=h))
open(path, 'w', encoding='utf-8').write(json.dumps(d, ensure_ascii=False, separators=(',', ':')))
print(f'removed {removed} old platform outlines, added {added} greybox outlines; {len(d["contours"])} contours in all')
