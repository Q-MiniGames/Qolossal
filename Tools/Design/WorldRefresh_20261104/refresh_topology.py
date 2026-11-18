"""Refresh map room bounds from Unity and verify retained passage source geometry."""
from pathlib import Path
import json,hashlib,math,collections
ROOT=Path(__file__).resolve().parents[3]
TASK=ROOT/'Tools/Design/WorldRefresh_20261104'
REVIEW=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview')
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
ug=read(REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json')
assert ug['sha256']==sha(ROOT/ug['path'])
before=read(TASK/'Before/underground_scene_geometry.json')
old_colliders=collections.defaultdict(list)
for c in before['colliders']:old_colliders[c['path']].append(c)
verified=[]
for c in ug['colliders']:
    if c['layer']!='Ground' or c['dynamic'] or 'Continuous_Collision' not in c['path']:continue
    old=old_colliders.get(c['path'])
    assert old, 'Missing earlier shared passage collider '+c['path']
    assert any(c['paths']==o['paths'] for o in old), 'Shared passage collision changed '+c['path']
    verified.append(c['path'])
assert len(verified)>80
path=ROOT/'Tools/Design/MainWorldLayout/topology-source.json'
top=read(path)
rooms={r['id']:r for r in ug['rooms']}
assert len(rooms)==89
for n in top['nodes']:
    if n['type']=='underground':n['worldBounds']=rooms[n['id']]['bounds']
geometry_path=ROOT/'Assets/Development/Layouts/WorldLayout_20261022/layout_geometry.json'
geometry=read(geometry_path)
connections={c['id']:c for c in geometry['connections']}
count=0
for edge in top['edges']:
    if edge['kind']!='passage':continue
    current=connections[edge['id']]
    assert (edge['from'],edge['to'])==(current['fromRoom'],current['toRoom'])
    assert edge['worldPoints']==current['points'], 'Unexpected passage data movement'
    edge['worldPoints']=current['points'];count+=1
assert count==63
top['sourceUndergroundSceneHash']=ug['sha256']
top['sourceVerification']={'date':'2026-11-04','actualRoomBounds':89,'passages':63,'unchangedSharedCollisionPaths':len(verified),'passageGeometrySource':str(geometry_path),'passageGeometryHash':sha(geometry_path),'actualSceneExtraction':str(REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json'),'actualSceneExtractionHash':sha(REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json')}
path.write_text(json.dumps(top,ensure_ascii=False,indent=2),encoding='utf-8')
(TASK/'topology_refresh.json').write_text(json.dumps(top['sourceVerification'],indent=2),encoding='utf-8')
print(top['sourceVerification'])
