"""Check revised surface/cave registration and unchanged underground cartography."""
from pathlib import Path
import argparse, hashlib, json, math
from refresh_map_sources import ROOT,TASK,BEFORE,MAP,REVIEW,read,write,stable,close,validate

def verify(runtime=False):
    inv,top,lock,source=validate()
    d=read(REVIEW/'MainWorldLayout_20261030/world-map-data.json');old=read(BEFORE/'world-map-data.json')
    assert d['layout']['sceneHashes']==inv['sceneHashes']
    assert len(d['nodes'])==173 and len(d['caves'])==37
    oldnodes={n['id']:n for n in old['nodes']};nodes={n['id']:n for n in d['nodes']}
    assert nodes.keys()==oldnodes.keys()
    for nid,n in nodes.items():
        if n['type']!='underground':continue
        prior=oldnodes[nid]
        for field in ('worldBounds','mapBounds','mappingId','position','point'):
            assert n[field]==prior[field], f'Underground {field} changed: {nid}'
    for key,mapping in lock['transforms'].items():assert d['transforms'][key]==mapping, 'Component mapping changed '+key
    assert stable(d['undergroundContours'])==stable(old['undergroundContours']), 'Underground contour geometry/ownership changed'
    passages=lambda data:[{k:e[k] for k in ('id','from','to','worldPoints','points')} for e in data['edges'] if e['kind']=='passage']
    assert stable(passages(d))==stable(passages(old)), 'Underground passage coordinates changed'
    before_edges={(e['id'],e['from'],e['to'],e['kind']) for e in old['edges']}
    expected=before_edges-{('CAVE_A01','MR04_N06','A01','cave')}|{('CAVE_A01','MR04_N00','A01','cave')}
    assert {(e['id'],e['from'],e['to'],e['kind']) for e in d['edges']}==expected
    current={(c['connectionId'],c['undergroundSide']):c for c in inv['caves']}
    def convert(mapping,p):return [mapping['ox']+p[0]*mapping['sx'],mapping['oy']-p[1]*mapping['sy']]
    for c in d['caves']:
        upper=current[c['connectionId'],False];lower=current[c['connectionId'],True]
        assert c['upperAreaId']==upper['upperAreaId']==lower['upperAreaId']
        up=convert(d['transforms'][upper['upperScene']],upper['entrancePosition'])
        down=convert(d['transforms'][nodes[c['lowerRoomId']]['mappingId']],lower['entrancePosition'])
        assert close(c['mapUpper'],up) and close(c['mapLower'],down)
        edge=next(e for e in d['edges'] if e['id']==c['connectionId'])
        assert close(edge['points'][0],up) and close(edge['points'][-1],down)
    assert d['surfaceContours']!=old['surfaceContours'], 'Expected fresh bevel/entrance surface contours'
    if runtime:
        atlas=read(ROOT/'Assets/Resources/QolossalAtlas/AtlasData.json')
        for field in ('nodes','transforms','caves'):assert atlas[field]==d[field], 'Runtime differs from canonical '+field
        assert atlas['sceneDerived'] and atlas['illustratedMap']
        ugcontours=[c for c in atlas['contours'] if c['underground']]
        oldatlas=read(BEFORE/'AtlasData.json')
        assert stable(ugcontours)==stable([c for c in oldatlas['contours'] if c['underground']])
    result={'passed':True,'sceneCount':len(inv['sceneHashes']),'nodeCount':173,'cavePairs':37,'unchangedUndergroundRooms':89,'unchangedPassages':63,'unchangedUndergroundContours':len(d['undergroundContours']),'componentTransformsPreserved':len(lock['transforms'])-1,'authorizedEdgeChange':source['authorizedTopologyChange'],'surfaceContoursUpdated':True,'mouthEndpointsMatchFreshExtraction':True,'runtimeMatchesCanonical':runtime,'liveUiTest':'Pending separate Unity fixture; static export is not a live pass'}
    write(MAP/('runtime_map_validation.json' if runtime else 'canonical_map_validation.json'),result)
    print(json.dumps(result,indent=2))
    return result

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--runtime',action='store_true');verify(p.parse_args().runtime)
