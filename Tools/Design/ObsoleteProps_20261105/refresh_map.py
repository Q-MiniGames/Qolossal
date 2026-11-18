from pathlib import Path
import json, hashlib, subprocess, sys
ROOT=Path(__file__).resolve().parents[3]; TASK=Path(__file__).resolve().parent
REVIEW=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview')
CANON=ROOT/'Tools/Design/MainWorldLayout'
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d): p.write_text(json.dumps(d,indent=2,ensure_ascii=False),encoding='utf-8')
old=read(TASK/'Before/Map/inventory.json'); fresh=read(TASK/'Map/inventory_fresh.json')
for k in ('areas','rooms','caves'): assert old[k]==fresh[k],k
old_ug=read(TASK/'Before/Map/underground.json'); ug=read(REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json')
for k in old_ug:
 if k!='sha256': assert old_ug[k]==ug[k],k
old_s=read(TASK/'Before/Map/surface.json'); surface=read(REVIEW/'ActualSlopeComparison_20261030/scene_geometry.json')
removed=[]; activated=[]
for s in surface['scenes']:
 prior=next(p for p in old_s['scenes'] if p['scene']==s['scene'])
 for k in ('areas','caves','exits','arrivals'): assert prior[k]==s[k],(s['scene'],k)
 oldt={t['path']:t for t in prior['terrains']}
 for t in s['terrains']:
  p=oldt[t['path']]
  for k,v in t.items():
   if k=='active' and 'After the quake (knot:grip)/Fallen stone crossing' in t['path']:
    assert v is True and p[k] is False
   else: assert v==p[k],(t['path'],k)
 oldc={c['path']:c for c in prior['colliders']}; newc={c['path']:c for c in s['colliders']}
 for p,c in newc.items():
  if p not in oldc:
   assert s['scene']=='MRAtlas_MR01_Cradle' and 'After the quake (knot:grip)/Fallen stone crossing' in p,p
   activated.append(p)
  else: assert oldc[p]==c,p
 for p in oldc.keys()-newc.keys():
  assert s['scene']=='MRAtlas_MR01_Cradle' and 'Root Gate (knot:grip)' in p,p
  removed.append(p)
assert len(removed)==1,removed
assert len(activated)==1,activated
assert read(CANON/'component-placement-lock.json')==read(TASK/'Before/Map/component-placement-lock.json')
top=read(CANON/'topology-source.json'); old_top=read(TASK/'Before/Map/topology.json')
for k,v in old_top.items():
 if k!='sourceUndergroundSceneHash': assert top[k]==v,k
top['sourceUndergroundSceneHash']=fresh['sceneHashes']['Qolossal_Underground']
write(CANON/'topology-source.json',top)
write(ROOT/'Tools/Design/ConnectedWorldMap_20261030/current_scene_inventory.json',fresh)
for script in ('build_layout.py','viewer.py','export_runtime.py','verify.py'):
 subprocess.run([sys.executable,str(CANON/script)],cwd=ROOT,check=True)
after=read(REVIEW/'MainWorldLayout_20261030/world-map-data.json'); before=read(TASK/'Before/Map/world-map-data.json')
for k in ('nodes','caves','componentPlacements','transforms'): assert after[k]==before[k],k
old_edges={e['id']:e for e in before['edges']}
for e in after['edges']:
 prior=old_edges[e['id']]
 for k,v in e.items():
  if e['id']=='MR01_E08' and k=='requires': assert v==[]
  else: assert v==prior[k],(e['id'],k)
write(TASK/'Map/source_refresh.json',dict(passed=True,removedGateCollider=removed,existingCrossingNowAlwaysActive=activated,allTerrainVerticesUnchanged=True,all89RoomsAnd37CavePairsUnchanged=True,allCartographicPlacementsUnchanged=True,onlyRequirementChange='MR01_E08 no longer requires knot:grip',sceneHashes=fresh['sceneHashes']))
print('PASS: only obsolete Cradle gate collider and knot requirement removed; terrain and cave geometry unchanged.')
