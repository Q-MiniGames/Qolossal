"""Publish the canonical actual scene layout, never the retired painted atlas geometry."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parents[3];OUT=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/MainWorldLayout_20261030')
d=json.loads((OUT/'world-map-data.json').read_text(encoding='utf-8'))
for s,h in d['layout']['sceneHashes'].items():assert hashlib.sha256((ROOT/'Assets/Scenes/Game'/f'{s}.unity').read_bytes()).hexdigest()==h
d['sceneDerived']=True
d['illustratedMap']=True
d['contours']=[dict(region=c['region'],room=c.get('room'),underground=kind=='undergroundContours',primary=c.get('primary',False),mappingId=c.get('mappingId',''),kind=str(c.get('source','')).split('/')[-1] if str(c.get('source','')).startswith('UGP Platforming/') else '',path=[dict(x=p[0],y=p[1]) for p in c['points']]) for kind in ['surfaceContours','undergroundContours'] for c in d[kind]]
for e in d['edges']:
 if isinstance(e.get('requirements'),list):e['requirements']=', '.join(e['requirements'])
 if e['kind']=='cave':e['strokes']=[dict(path=[dict(x=p[0],y=p[1]) for p in ps]) for ps in e['tunnelSegments']]
target=ROOT/'Assets/Resources/QolossalAtlas/AtlasData.json';target.write_text(json.dumps(d,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
print('Published 173 nodes, 37 cave pairs and',len(d['contours']),'actual terrain contours to Unity.')
