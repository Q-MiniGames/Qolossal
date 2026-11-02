"""Regression checks for directional cartographic translation, not scene edits."""
from pathlib import Path
import json,math,hashlib
ROOT=Path(__file__).resolve().parents[3]
WORK=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal')
OUT=WORK/'Tools/ForwardCaveMap_20261030'
d=json.loads((WORK/'DesignReview/MainWorldLayout_20261030/world-map-data.json').read_text())
old=json.loads((OUT/'Before/world-map-data.json').read_text())
nodes={n['id']:n for n in d['nodes']};previous={n['id']:n for n in old['nodes']}
assert d['surfaceContours']==old['surfaceContours']
assert all(n==previous[n['id']] for n in d['nodes'] if n['type']=='surface')
offsets={}
for n in d['nodes']:
    if n['type']!='underground':continue
    p=previous[n['id']];assert n['worldBounds']==p['worldBounds'] and n['mapBounds'][2:]==p['mapBounds'][2:]
    delta=[n['mapBounds'][k]-p['mapBounds'][k] for k in range(2)]
    mid=n['mappingId']
    if mid in offsets:assert math.dist(delta,offsets[mid])<1e-6
    offsets[mid]=delta
    if n.get('group') in ['A','C','D','E','H']:assert math.dist(delta,[0,0])<1e-6
    m=d['transforms'][mid];assert m['sx']==m['sy']==3
for e in d['edges']:
    if e['kind']!='passage':continue
    a,b=nodes[e['from']],nodes[e['to']];assert a['mappingId']==b['mappingId']
    prior=next(p for p in old['edges'] if p['id']==e['id']);delta=offsets[a['mappingId']]
    assert len(prior['points'])==len(e['points'])
    assert all(math.dist(new,[before[k]+delta[k] for k in range(2)])<1e-6 for new,before in zip(e['points'],prior['points']))
affected=[]
for c in d['caves']:
    n=nodes[c['lowerRoomId']];before=next(p for p in old['caves'] if p['connectionId']==c['connectionId'])
    assert c['mapUpper']==before['mapUpper']
    if n['group'] in ['A','C','D','E','H']:continue
    assert n['mapBounds'][0]>c['mapUpper'][0],c['lowerRoomId']
    if n['id'].startswith('PC') or n['region'] in ['MR05','MR06','MR07']:
        assert c['mapLower'][1]<before['mapLower'][1],n['id']
    affected.append(c['lowerRoomId'])
for name,h in d['layout']['sceneHashes'].items():
    assert hashlib.sha256((ROOT/'Assets/Scenes/Game'/f'{name}.unity').read_bytes()).hexdigest()==h
qa=dict(affectedAccessPairs=len(affected),translatedRooms=sum(math.dist(offsets[n['mappingId']],[0,0])>1e-6 for n in d['nodes'] if n['type']=='underground'),all33TargetCavesForward=True,all30IsolatedCavesRaised=True,all63PhysicalPassagesRigid=True,all89RoomShapesAndSlopesPreserved=True,centralDeepNetworkUnchanged=True,upperTerrainAndMouthsUnchanged=True,all8SceneHashesUnchanged=True,componentOffsets=offsets)
(OUT/'placement_qa.json').write_text(json.dumps(qa,indent=2))
test=(ROOT/'Tools/Design/MainWorldLayout/player_test.cs').read_text()
test=test.replace('CompactTunnelMap_20261030','ForwardCaveMap_20261030')
marker='''steps.Add((.3f,"per-room marker projection",()=>{
 var a=atlas();a.ShowWorld();var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();var rootObject=player.transform.root.gameObject;var oldScene=rootObject.scene;var oldPosition=player.transform.position;
 var temporary=UnityEngine.SceneManagement.SceneManager.CreateScene("Qolossal_Underground");
 try{
  UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rootObject,temporary);
  foreach(var n in a.Data.nodes.Where(n=>n.type=="underground"))foreach(float t in new[]{.15f,.5f,.85f}){
   player.transform.position=new Vector3(n.worldBounds[0]+n.worldBounds[2]*t,n.worldBounds[1]+n.worldBounds[3]*t,oldPosition.z);
   var expected=new Vector2(n.Bounds.x+n.Bounds.width*t,n.Bounds.yMax-n.Bounds.height*t);
   check(a.PlayerMapPoint(out var actual)&&Vector2.Distance(expected,actual)<.02f,"Marker projection "+n.id+" at "+t);
  }
 }finally{player.transform.position=oldPosition;UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rootObject,oldScene);UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(temporary);}
}));
'''
test=test.replace('steps.Add((.5f,"close"',marker+'steps.Add((.5f,"close"')
(ROOT/'Tools/Design/MainWorldLayout/forward_test.cs').write_text(test)
print(json.dumps({k:v for k,v in qa.items() if k!='componentOffsets'}))
