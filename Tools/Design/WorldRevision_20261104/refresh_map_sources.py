"""Validate fresh Unity extracts, then optionally publish canonical map inputs.

Default is read-only preflight. --apply writes inventory/topology/placement lock
only after every assertion passes. Does not invoke Unity or change scenes.
"""
from pathlib import Path
import argparse, collections, copy, hashlib, json, math, re

ROOT=Path(__file__).resolve().parents[3]
TASK=Path(__file__).resolve().parent
BEFORE=TASK/'Before/Map'
MAP=TASK/'Map'
CANON=ROOT/'Tools/Design/MainWorldLayout'
REVIEW=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview')

def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d): p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
def normalized(v):
    if isinstance(v,float):return round(v,4)
    if isinstance(v,str):return re.sub(r'\\u([0-9a-fA-F]{4})',lambda m:chr(int(m[1],16)),v)
    if isinstance(v,list):return [normalized(x) for x in v]
    if isinstance(v,dict):return {k:normalized(x) for k,x in v.items()}
    return v
def stable(rows): return sorted(json.dumps(normalized(x),sort_keys=True) for x in rows)
def close(a,b,tolerance=.002): return len(a)==len(b) and all(abs(x-y)<=tolerance for x,y in zip(a,b))

def validate():
    manifest=read(BEFORE/'manifest.json')
    for name,item in manifest.items():assert sha(BEFORE/name)==item['sha256'], 'Immutable baseline changed: '+name
    previous=read(BEFORE/'inventory.json');oldmap=read(BEFORE/'world-map-data.json')
    oldug=read(BEFORE/'underground_geometry.json');oldsurface=read(BEFORE/'surface_geometry.json')
    inv=read(MAP/'inventory_fresh.json');present=read(MAP/'presentations_fresh.json')
    surface=read(REVIEW/'ActualSlopeComparison_20261030/scene_geometry.json')
    ug=read(REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json')
    assert len(inv['areas'])==84 and len(inv['rooms'])==89 and len(inv['caves'])==74
    assert len(inv['sceneHashes'])==13 and inv['sceneHashes']==present['sceneHashes']
    for scene,h in inv['sceneHashes'].items():assert sha(ROOT/'Assets/Scenes/Game'/f'{scene}.unity')==h, 'Stale inventory: '+scene
    assert len(surface['scenes'])==7
    for s in surface['scenes']:assert s['sha256']==inv['sceneHashes'][s['scene']]==sha(ROOT/s['path']), 'Stale surface extraction: '+s['scene']
    assert ug['sha256']==inv['sceneHashes']['Qolossal_Underground']==sha(ROOT/ug['path']), 'Stale underground extraction'
    assert stable(ug['rooms'])==stable(oldug['rooms']), 'Underground room bounds/IDs changed'
    ground=lambda d:[c for c in d['colliders'] if c['layer']=='Ground' and not c['dynamic']]
    assert stable(ground(ug))==stable(ground(oldug)), 'Underground static Ground collision changed'
    assert stable(ug['terrain'])==stable(oldug['terrain']), 'Underground terrain changed'
    oldareas={a['id']:a for a in previous['areas']}
    assert {a['id'] for a in inv['areas']}==set(oldareas), 'Surface zone IDs changed'
    for area in inv['areas']:
        oldarea=oldareas[area['id']]
        assert {k:v for k,v in area.items() if k!='bounds'}=={k:v for k,v in oldarea.items() if k!='bounds'}, 'Surface zone metadata changed'
        # The old extractor rebuilt bounds from rounded center/size. Compare
        # actual differences, not decimal rounding across a half-step boundary.
        assert close(area['bounds'],oldarea['bounds'],.00005), 'Surface zone bounds changed: '+area['id']
    assert stable(inv['rooms'])==stable(previous['rooms']), 'Inventory room IDs/bounds changed'
    key=lambda c:(c['connectionId'],c['undergroundSide'])
    oldc={key(c):c for c in previous['caves']};newc={key(c):c for c in inv['caves']}
    assert len(oldc)==len(newc)==74 and oldc.keys()==newc.keys()
    changed=[]
    for k,c in newc.items():
        before=oldc[k];allowed=set()
        if k[0]=='CAVE_A01':allowed|={'upperAreaId','upperAreaName'}
        if k[0] in ('CAVE_A01','CAVE_PC04') and not k[1]:allowed|={'entrancePosition','arrivalPosition','activation'}
        for field,value in before.items():
            if field not in allowed:assert normalized(value)==normalized(c[field]), f'Unexpected cave change {k}: {field}'
        if stable([before])!=stable([c]):changed.append({'connection':k[0],'undergroundSide':k[1],'before':before,'after':c})
    for side in (False,True):assert newc['CAVE_A01',side]['upperAreaId']=='MR04_N00'
    assert newc['CAVE_A01',False]['upperAreaName']==newc['CAVE_A01',True]['upperAreaName']
    assert newc['CAVE_PC04',False]['upperAreaId']=='MR02_N02'
    shows={key(c):c for c in present['presentations']}
    expected={'CAVE_PC04':([88.5,30.0],1.0),'CAVE_A01':([21.178,20.625],None)}
    for cid,(mouth,facing) in expected.items():
        c=shows[cid,False];assert c['presentation'] and close(c['mouth'],mouth,.012), 'Wrong mouth anchor '+cid
        assert abs(c['inward'])==1 and (facing is None or c['inward']==facing), 'Wrong facing '+cid
        assert (c['entrance'][0]-c['mouth'][0])*c['inward']>0, 'Threshold must be inside mouth '+cid
        assert (c['arrival'][0]-c['mouth'][0])*c['inward']<0, 'Arrival must be outside mouth '+cid
        assert c['arrival'][1]>c['mouth'][1], 'Arrival must be above floor '+cid
    # Cross-check TransformPoint inventory against both canonical geometry extracts.
    extracted={(c['id'],False):c for s in surface['scenes'] for c in s['caves']}
    extracted.update({(c['id'],True):c for c in ug['caves']})
    assert extracted.keys()==newc.keys()
    for k,c in newc.items():
        e=extracted[k];assert e['area']==c['upperAreaId'] and e['room']==c['lowerRoomId']
        assert close(e['entrance'],c['entrancePosition']) and close(e['arrival'],c['arrivalPosition']), 'Extraction disagreement '+str(k)
    top=read(BEFORE/'topology-source.json')
    a01=next(e for e in top['edges'] if e['id']=='CAVE_A01')
    assert (a01['kind'],a01['from'],a01['to'])==('cave','MR04_N06','A01')
    a01['from']='MR04_N00'
    # Frozen topology's cave metadata is provenance; keep its endpoint registration current too.
    for c in top.get('caves',[]):
        if c['connectionId']=='CAVE_A01':c['upperAreaId']='MR04_N00';c['upperAreaName']=newc['CAVE_A01',False]['upperAreaName']
    geometry=read(ROOT/'Assets/Development/Layouts/WorldLayout_20261022/layout_geometry.json')
    assert sha(ROOT/'Assets/Development/Layouts/WorldLayout_20261022/layout_geometry.json')==manifest['layout_geometry.json']['sha256']
    links={c['id']:c for c in geometry['connections']};passages=[e for e in top['edges'] if e['kind']=='passage']
    assert len(passages)==63
    for e in passages:
        c=links[e['id']];assert (e['from'],e['to'])==(c['fromRoom'],c['toRoom']) and e['worldPoints']==c['points']
    oldterrain={(s['scene'],t['path']):t for s in oldsurface['scenes'] for t in s['terrains']}
    terrainchanges=[]
    for s in surface['scenes']:
        for t in s['terrains']:
            previous_t=oldterrain.get((s['scene'],t['path']))
            if previous_t is None or normalized(t['surface'])!=normalized(previous_t['surface']):
                terrainchanges.append({'scene':s['scene'],'path':t['path'],'beforePoints':len(previous_t['surface']) if previous_t else None,'afterPoints':len(t['surface'])})
    lock={'reason':'Preserve approved rigid cartographic components during authorized surface bevel and entrance revision, 2026-11-04','baseline':str(BEFORE/'world-map-data.json'),'baselineSha256':manifest['world-map-data.json']['sha256'],'nodes':[copy.deepcopy(n) for n in oldmap['nodes'] if n['type']=='underground'],'transforms':{k:copy.deepcopy(v) for k,v in oldmap['transforms'].items() if k=='Qolossal_Underground' or k.startswith('underground:')},'componentPlacements':copy.deepcopy(oldmap['componentPlacements'])}
    evidence={'date':'2026-11-04','sourceUndergroundSceneHash':ug['sha256'],'verifiedRoomCount':89,'unchangedStaticGroundColliders':len(ground(ug)),'unchangedPassages':63,'authorizedTopologyChange':{'edge':'CAVE_A01','fromBefore':'MR04_N06','fromAfter':'MR04_N00','to':'A01'},'changedCaveRecords':changed,'changedSurfaceTerrain':terrainchanges,'undergroundComponentMappingsFrozen':len(lock['transforms'])-1,'freshInputHashes':{str(p):sha(p) for p in [MAP/'inventory_fresh.json',MAP/'presentations_fresh.json',REVIEW/'ActualSlopeComparison_20261030/scene_geometry.json',REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json']}}
    top['sourceUndergroundSceneHash']=ug['sha256'];top['sourceVerification']=evidence
    return inv,top,lock,evidence

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--apply',action='store_true');args=parser.parse_args()
    inv,top,lock,evidence=validate()
    if args.apply:
        write(ROOT/'Tools/Design/ConnectedWorldMap_20261030/current_scene_inventory.json',inv)
        write(CANON/'topology-source.json',top)
        write(CANON/'component-placement-lock.json',lock)
    evidence['canonicalInputsWritten']=args.apply
    write(MAP/'source_refresh.json',evidence)
    print(json.dumps({'preflight':'PASS','applied':args.apply,'rooms':89,'passages':63,'caves':37,'surfaceChanges':len(evidence['changedSurfaceTerrain']),'frozenMappings':evidence['undergroundComponentMappingsFrozen']},indent=2))
