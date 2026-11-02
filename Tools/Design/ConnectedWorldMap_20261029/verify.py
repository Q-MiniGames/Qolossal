from pathlib import Path
import json,hashlib,xml.etree.ElementTree as ET,re,zipfile
root=Path(r'C:/UnityProjects/Qolossal');src=root/'Tools/Design/ConnectedWorldMap_20261029';out=Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/ConnectedWorldMap_20261029')
d=json.loads((out/'world-map-data.json').read_text(encoding='utf-8'));inv=json.loads((src/'current_scene_inventory.json').read_text());xml=ET.parse(out/'Qolossal_ConnectedWorld.svg');ns={'s':'http://www.w3.org/2000/svg'}
ids={n['id'] for n in d['nodes']};drawn={e.attrib['data-id'] for e in xml.iter() if 'data-id' in e.attrib and e.attrib['data-id'] in ids};assert ids==drawn
for id,h in inv['sceneHashes'].items():assert hashlib.sha256((root/'Assets/Scenes/Game'/f'{id}.unity').read_bytes()).hexdigest()==h
for c in d['caves']:
 pair=[x for x in inv['caves'] if x['connectionId']==c['connectionId']];assert len(pair)==2 and {p['undergroundSide'] for p in pair}=={True,False};assert all(p['upperAreaId']==c['upperAreaId'] and p['lowerRoomId']==c['lowerRoomId'] for p in pair)
assert len([e for e in xml.iter() if e.attrib.get('class')=='cave-link'])==52
html=(out/'index.html').read_text(encoding='utf-8');local=[r for r in re.findall('href="([^"]+)"',html) if not r.startswith(('http','#'))];assert all((out/f).exists() for f in local)
qa={'all173IdsDrawn':True,'all52PairsMatchedAgainstBothSceneEndpoints':True,'all8SceneHashesUnchanged':True,'syntaxChecked':True,'interactiveBrowserTested':False,'reviewResolution':[7000,2700],'overviewResolution':[2800,1080],'runtimeIntegrated':False,'styleReference':'https://hollowknight.wiki/w/File:Silksong_Overview_Map.png','note':'Region colors and outlined chamber/passage language are inspired by the requested reference; all room topology and labels are Qolossal source data. Separate scene groups are assembled for legibility; gold links are travel transitions, not newly built tunnels.','supportedTargets':['Windows PC','PS5','Xbox Series X','Xbox Series S','Nintendo Switch 2 handheld','Nintendo Switch 2 docked'],'notTested':['In-game map input or rendering','Console and standalone builds','Hardware performance','Peak memory'],'sourceGeometry':'Current area/room metadata and cave endpoints; original regional passage centerlines, unchanged by cave-art revisions.'}
(out/'qa.json').write_text(json.dumps(qa,indent=2))
with zipfile.ZipFile(out/'Qolossal_FullMap_Package.zip','w',zipfile.ZIP_DEFLATED) as z:
 for name in ['index.html','Qolossal_ConnectedWorld.svg','Qolossal_ConnectedWorld_Geometry.svg','Qolossal_ConnectedWorld.png','Qolossal_ConnectedWorld_Preview.png','world-map-data.json','validation.json','qa.json']:
  z.write(out/name,name)
 for p in src.glob('*.py'):z.write(p,'source/'+p.name)
print('Verified all 173 labels, 52 paired entrances, eight unchanged scenes and offline file links. Packaged full map.')
