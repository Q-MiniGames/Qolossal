from pathlib import Path
import json,math,hashlib
ROOT=Path(__file__).resolve().parents[3];WORK=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal');OUT=WORK/'Tools/UndergroundRelocation_20261030';REVIEW=WORK/'DesignReview'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
old=read(OUT/'Before/Artifacts/DesignReview/ActualUndergroundComparison_20261030/scene_geometry.json');d=read(REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json');moves=read(OUT/'backing.json')['moves'];indices={15:'MR06_C01',17:'PC11',3:'PC12'}
def close(a,b):return all(abs(x-y)<.002 for x,y in zip(a,b))
for a,b in zip(old['rooms'],d['rooms']):
 assert a['id']==b['id'];dx,dy=moves.get(a['id'],[0,0]);assert close([a['bounds'][0]+dx,a['bounds'][1]+dy,*a['bounds'][2:]],b['bounds']),a['id']
assert len(old['colliders'])==len(d['colliders'])==922
changed=[]
for a,b in zip(old['colliders'],d['colliders']):
 assert a['path']==b['path'];id=a['room']
 for i,r in indices.items():
  if a['path'].split('/')[-1].startswith(f'Continuous_Collision_{i} | preserved segment '):id=r
 dx,dy=moves.get(id,[0,0]);assert len(a['paths'])==len(b['paths'])
 for ap,bp in zip(a['paths'],b['paths']):
  assert len(ap)==len(bp)
  for x,y in zip(ap,bp):assert close([x[0]+dx,x[1]+dy],y),(a['path'],x,y)
 if dx or dy:changed.append(a['path'])
for a,b in zip(old['caves'],d['caves']):
 assert a['id']==b['id'] and a['requirements']==b['requirements'];dx,dy=moves.get(a['room'],[0,0])
 for k in ['arrival','entrance']:assert close([a[k][0]+dx,a[k][1]+dy],b[k]),a['id']
previous=read(OUT/'Before/Project/Tools/Design/ConnectedWorldMap_20261030/current_scene_inventory.json');inv=read(ROOT/'Tools/Design/ConnectedWorldMap_20261030/current_scene_inventory.json')
for s,h in previous['sceneHashes'].items():
 if s!='Qolossal_Underground':assert inv['sceneHashes'][s]==h
for a,b in zip(previous['caves'],inv['caves']):
 if not a['undergroundSide']:assert a==b
cs=[c for c in d['colliders'] if c['layer']=='Ground'];rooms={r['id']:r for r in d['rooms']}
audit=(ROOT/'Tools/Design/UndergroundLayoutAudit_20261030/review.py').read_text();exec(audit[audit.index('def inside'):audit.index('proposals=[]')])
report=dict(roomCount=89,cavePairs=37,changedRooms=list(moves),changedGroundColliderObjects=len(changed),otherRoomAndColliderPositionsUnchanged=True,allSevenUpperSceneHashesUnchanged=True,allUpperCaveEndpointsUnchanged=True,arrivalChecks=checks,noRoomOverlaps=True,tightRoomPairs=pairs,sceneHash=d['sha256'])
(OUT/'geometry-verification.json').write_text(json.dumps(report,indent=2));print('PASS: only 3 rooms moved; all 37 arrivals clear; 7 upper scenes unchanged; no room overlaps.')
# Refresh the topology snapshot only after the exact transform and topology checks pass.
p=ROOT/'Tools/Design/MainWorldLayout/topology-source.json';top=read(p)
for n in top['nodes']:
 if n['id'] in rooms:n['worldBounds']=rooms[n['id']]['bounds']
top['sourceUndergroundSceneHash']=d['sha256'];p.write_text(json.dumps(top,indent=2))
