"""Match existing passage backdrop envelopes to canonical connections; no scene edits."""
from pathlib import Path
import json

task=Path(__file__).resolve().parent
root=task.parents[2]
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
inventory=read(task/'inventory.json')
connections=read(root/'Assets/Development/Layouts/WorldLayout_20261022/layout_geometry.json')['connections']
result=[]
for backdrop in inventory['passages']:
    if backdrop['sortingOrder']!=-105:
        continue
    candidates=[]
    for link in connections:
        # The scene's owner may be either end (not necessarily fromRoom).
        if backdrop['owner'] not in (link['fromRoom'],link['toRoom']):
            continue
        if not all(r in backdrop['intersections'] for r in (link['fromRoom'],link['toRoom'])):
            continue
        xs=[p['x'] for p in link['points']];ys=[p['y'] for p in link['points']]
        envelope=[min(xs)-8,min(ys)-8,max(xs)-min(xs)+16,max(ys)-min(ys)+16]
        error=sum(abs(a-b) for a,b in zip(envelope,backdrop['bounds']))
        candidates.append((error,link))
    candidates.sort(key=lambda pair:pair[0])
    assert candidates and (len(candidates)==1 or candidates[0][0]+.1<candidates[1][0]),backdrop
    error,link=candidates[0]
    assert error<=8.01,(link['id'],error)
    result.append(dict(id=link['id'],fromRoom=link['fromRoom'],toRoom=link['toRoom'],rect=backdrop['bounds'],points=link['points']))
assert len(result)==63 and len({r['id'] for r in result})==63
text=json.dumps({'connections':result},indent=2)
(task/'connections.json').write_text(text)
(root/'Assets/Resources/CavePassageConnections.json').write_text(text)
print('Matched all 63 canonical passages to existing backdrop envelopes.')
