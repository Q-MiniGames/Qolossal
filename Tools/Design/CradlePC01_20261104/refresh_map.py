"""Publish the authorized PC01 local terrain revision; lock all other scene geometry."""
from pathlib import Path
import json, hashlib, subprocess, sys
ROOT=Path(__file__).resolve().parents[3]
TASK=Path(__file__).resolve().parent
BEFORE=TASK/'Before/Map'
MAP=TASK/'Map'
CANON=ROOT/'Tools/Design/MainWorldLayout'
REVIEW=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview')
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):p.write_text(json.dumps(d,indent=2,ensure_ascii=False),encoding='utf-8')
old=read(BEFORE/'inventory.json'); inv=read(MAP/'inventory_fresh.json')
surface=read(REVIEW/'ActualSlopeComparison_20261030/scene_geometry.json')
before_surface=read(BEFORE/'surface_geometry.json')
assert len(inv['rooms'])==89 and len(inv['areas'])==84 and len(inv['caves'])==74
assert inv['rooms']==old['rooms'] and inv['areas']==old['areas']
changed_scenes=[s for s,h in inv['sceneHashes'].items() if h!=old['sceneHashes'][s]]
assert changed_scenes==['MRAtlas_MR01_Cradle'],changed_scenes
for name,h in inv['sceneHashes'].items():assert sha(ROOT/'Assets/Scenes/Game'/f'{name}.unity')==h
assert sha(REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json')==sha(BEFORE/'underground_geometry.json')
assert sha(CANON/'component-placement-lock.json')==sha(BEFORE/'component-placement-lock.json')
key=lambda c:(c['connectionId'],c['undergroundSide'])
oldc={key(c):c for c in old['caves']}; newc={key(c):c for c in inv['caves']}
assert oldc.keys()==newc.keys()
changes=[]
for k,c in newc.items():
    allowed={'entrancePosition','arrivalPosition'} if k==('CAVE_PC01',False) else set()
    for field,value in c.items():
        if field not in allowed:assert value==oldc[k][field],(k,field)
    if c!=oldc[k]:changes.append({'before':oldc[k],'after':c})
assert len(changes)==1
for s in surface['scenes']:
    assert s['sha256']==inv['sceneHashes'][s['scene']]
    previous=next(q for q in before_surface['scenes'] if q['scene']==s['scene'])
    if s['scene']!='MRAtlas_MR01_Cradle':assert s==previous,s['scene']
    else:
        terrain_old={t['path']:t for t in previous['terrains']}
        terrain_changes=[]
        for t in s['terrains']:
            prior=terrain_old[t['path']]
            if t!=prior:
                assert 'PC01' in t['path'] or t['path'].endswith('/Route MR01 piece 00'), t['path']
                terrain_changes.append(t['path'])
                if t['path'].endswith('/Route MR01 piece 00'):
                    outside=lambda row:[p for p in row['surface'] if p[0]<55 or p[0]>81]
                    assert outside(t)==outside(prior),'Terrain outside local repair changed'
top=read(CANON/'topology-source.json')
assert top==read(BEFORE/'topology-source.json')
write(ROOT/'Tools/Design/ConnectedWorldMap_20261030/current_scene_inventory.json',inv)
for script in ('build_layout.py','viewer.py','export_runtime.py','verify.py'):
    subprocess.run([sys.executable,str(CANON/script)],cwd=ROOT,check=True)
after=read(REVIEW/'MainWorldLayout_20261030/world-map-data.json'); before=read(BEFORE/'world-map-data.json')
old_edges={e['id']:e for e in before['edges']}
for e in after['edges']:
    for k,v in e.items():
        if e['id']=='CAVE_PC01' and k in ('points','tunnelSegments','path'):continue
        assert v==old_edges[e['id']][k],(e['id'],k)
assert after['componentPlacements']==before['componentPlacements']
assert after['transforms']==before['transforms']
assert [n for n in after['nodes'] if n['type']=='underground']==[n for n in before['nodes'] if n['type']=='underground']
qa={'passed':True,'changedScenes':changed_scenes,'changedCaveRecords':changes,'localTerrainChanges':terrain_changes,'localXRange':[55,81],'all89UndergroundRoomsUnchanged':True,'allOtherScenesUnchanged':True,'allEdgeConnectivityUnchanged':True,'onlyPC01IllustratedTunnelRegistrationChanged':True,'rigidCartographicTransformsUnchanged':True,'sceneHashes':inv['sceneHashes'],'runtimeAtlasHash':sha(ROOT/'Assets/Resources/QolossalAtlas/AtlasData.json')}
write(MAP/'source_refresh.json',qa)
print(json.dumps({'passed':True,'changedScenes':changed_scenes,'changedCaves':len(changes),'terrainChanges':len(terrain_changes)}))

